using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.IO;

namespace Tyflopodcast {

internal static class Mp3Seek {

	internal sealed class Info {
		public long totalFileBytes;
		public int audioStartBytes;
		public long audioBytes;
		public double durationSeconds;
		public byte[] toc; // 100 entries (0-255)
		public int bitrateKbps;
		public int sampleRate;
		public int channels;
	}

	private static readonly HttpClient http = new HttpClient(new HttpClientHandler {
		AutomaticDecompression = DecompressionMethods.None,
		UseProxy = false,
		Proxy = null,
		AllowAutoRedirect = true
	}) {
		Timeout = TimeSpan.FromSeconds(15)
	};

	internal static bool TryFetchInfo(string url, out Info info) {
		info = null;
		if(string.IsNullOrWhiteSpace(url)) return false;

		const int initialRead = 65536;
		byte[] data;
		long totalBytes;
		if(!TryReadRange(url, 0, initialRead-1, out data, out totalBytes)) return false;

		int id3Size = GetId3v2Size(data);
		if(id3Size >= data.Length) {
			const int retryRead = 262144;
			if(!TryReadRange(url, 0, retryRead-1, out data, out totalBytes)) return false;
			id3Size = GetId3v2Size(data);
			if(id3Size >= data.Length) return false;
		}

		if(!TryFindMpegFrame(data, id3Size, out int frameOffset, out Frame frame)) return false;

		Info r = new Info();
		r.totalFileBytes = totalBytes;
		r.audioStartBytes = frameOffset;
		r.sampleRate = frame.sampleRate;
		r.channels = frame.channels;
		r.bitrateKbps = frame.bitrateKbps;

		// Try Xing/Info header (preferred, provides TOC + exact duration/bytes)
		if(TryParseXingHeader(data, frameOffset, frame, out int xingFlags, out int xingFrames, out int xingBytes, out byte[] xingToc)) {
			if((xingFlags & 0x1) != 0 && xingFrames > 0 && frame.samplesPerFrame > 0 && frame.sampleRate > 0)
				r.durationSeconds = (double)xingFrames * (double)frame.samplesPerFrame / (double)frame.sampleRate;
			if((xingFlags & 0x2) != 0 && xingBytes > 0)
				r.audioBytes = xingBytes;
			else if(totalBytes > 0 && frameOffset >= 0 && totalBytes > frameOffset)
				r.audioBytes = totalBytes - frameOffset;
			if((xingFlags & 0x4) != 0 && xingToc != null && xingToc.Length == 100)
				r.toc = xingToc;
		}

		// Fallback duration/bytes if Xing/Info not available
		if(r.audioBytes <= 0 && totalBytes > 0 && frameOffset >= 0 && totalBytes > frameOffset)
			r.audioBytes = totalBytes - frameOffset;
		if(r.durationSeconds <= 0 && r.audioBytes > 0 && r.bitrateKbps > 0)
			r.durationSeconds = (double)r.audioBytes / ((double)r.bitrateKbps * 1000.0 / 8.0);

		if(r.audioBytes <= 0 || r.durationSeconds <= 0) return false;
		info = r;
		return true;
	}

	internal static long GetFileOffsetForTime(Info info, double seconds) {
		if(info == null) return 0;
		if(seconds < 0) seconds = 0;
		if(info.durationSeconds > 0 && seconds > info.durationSeconds) seconds = info.durationSeconds;

		long audioPos = 0;

		if(info.toc != null && info.toc.Length == 100 && info.durationSeconds > 0 && info.audioBytes > 0) {
			double percent = seconds / info.durationSeconds; // 0..1
			double p = percent * 100.0;
			int idx = (int)Math.Floor(p);
			if(idx < 0) idx = 0;
			if(idx > 99) idx = 99;
			double fract = p - idx;
			int a = info.toc[idx];
			int b = (idx < 99) ? info.toc[idx+1] : 256;
			double tocVal = (double)a + ((double)(b - a) * fract);
			audioPos = (long)(tocVal / 256.0 * (double)info.audioBytes);
		}
		else if(info.durationSeconds > 0 && info.audioBytes > 0) {
			audioPos = (long)((seconds / info.durationSeconds) * (double)info.audioBytes);
		}
		else if(info.bitrateKbps > 0) {
			audioPos = (long)(seconds * ((double)info.bitrateKbps * 1000.0 / 8.0));
		}

		if(audioPos < 0) audioPos = 0;
		if(info.audioBytes > 0 && audioPos > info.audioBytes) audioPos = info.audioBytes;
		return (long)info.audioStartBytes + audioPos;
	}

	private static bool TryReadRange(string url, long from, long to, out byte[] data, out long totalBytes) {
		data = null;
		totalBytes = -1;

		try {
			using(var req = new HttpRequestMessage(HttpMethod.Get, url)) {
				req.Headers.Range = new RangeHeaderValue(from, to);
				req.Headers.AcceptEncoding.Clear();
				req.Headers.AcceptEncoding.Add(new StringWithQualityHeaderValue("identity"));
				using(var resp = http.Send(req, HttpCompletionOption.ResponseHeadersRead)) {
					totalBytes = resp.Content.Headers.ContentRange?.Length ?? resp.Content.Headers.ContentLength ?? -1;
					using(Stream s = resp.Content.ReadAsStream()) {
						int max = (int)(to - from + 1);
						byte[] buf = new byte[max];
						int read = 0;
						while(read < max) {
							int n = s.Read(buf, read, max - read);
							if(n <= 0) break;
							read += n;
						}
						if(read <= 0) return false;
						if(read != buf.Length) {
							byte[] tmp = new byte[read];
							Array.Copy(buf, tmp, read);
							buf = tmp;
						}
						data = buf;
						return true;
					}
				}
			}
		} catch {
			return false;
		}
	}

	private static int GetId3v2Size(byte[] data) {
		if(data == null || data.Length < 10) return 0;
		if(data[0] != (byte)'I' || data[1] != (byte)'D' || data[2] != (byte)'3') return 0;
		int ver = data[3];
		byte flags = data[5];
		int size = SynchsafeToInt(data, 6);
		int total = 10 + size;
		if(ver >= 4 && (flags & 0x10) != 0) total += 10; // footer
		if(total < 0) total = 0;
		if(total > data.Length) return total; // may require more data
		return total;
	}

	private static int SynchsafeToInt(byte[] data, int offset) {
		if(data == null || offset < 0 || offset + 4 > data.Length) return 0;
		return ((data[offset] & 0x7F) << 21) | ((data[offset+1] & 0x7F) << 14) | ((data[offset+2] & 0x7F) << 7) | (data[offset+3] & 0x7F);
	}

	private struct Frame {
		public int mpegVersion; // 1,2,25
		public int layer; // 1,2,3
		public bool hasCrc;
		public int bitrateKbps;
		public int sampleRate;
		public int channels;
		public int samplesPerFrame;
	}

	private static bool TryFindMpegFrame(byte[] data, int start, out int frameOffset, out Frame frame) {
		frameOffset = -1;
		frame = new Frame();
		if(data == null || data.Length < 4) return false;
		if(start < 0) start = 0;
		if(start > data.Length - 4) return false;

		for(int i=start; i <= data.Length - 4; ++i) {
			if(data[i] != 0xFF || (data[i+1] & 0xE0) != 0xE0) continue;
			if(!TryParseFrameHeader(data, i, out Frame f)) continue;
			frameOffset = i;
			frame = f;
			return true;
		}
		return false;
	}

	private static bool TryParseFrameHeader(byte[] data, int offset, out Frame f) {
		f = new Frame();
		if(data == null || offset < 0 || offset + 4 > data.Length) return false;

		byte b1 = data[offset+1];
		int versionId = (b1 >> 3) & 0x03;
		int layerId = (b1 >> 1) & 0x03;
		if(versionId == 1 || layerId == 0) return false; // reserved

		int mpegVersion = versionId == 3 ? 1 : (versionId == 2 ? 2 : 25);
		int layer = layerId == 3 ? 1 : (layerId == 2 ? 2 : 3);
		bool hasCrc = (b1 & 0x01) == 0;

		byte b2 = data[offset+2];
		int bitrateIndex = (b2 >> 4) & 0x0F;
		int sampleIndex = (b2 >> 2) & 0x03;
		if(sampleIndex == 3) return false;

		int sampleRate = GetSampleRate(mpegVersion, sampleIndex);
		int bitrateKbps = GetBitrateKbps(mpegVersion, layer, bitrateIndex);
		if(sampleRate <= 0 || bitrateKbps <= 0) return false;

		byte b3 = data[offset+3];
		int channelMode = (b3 >> 6) & 0x03;
		int channels = channelMode == 3 ? 1 : 2;

		int samplesPerFrame = GetSamplesPerFrame(mpegVersion, layer);
		if(samplesPerFrame <= 0) return false;

		f.mpegVersion = mpegVersion;
		f.layer = layer;
		f.hasCrc = hasCrc;
		f.bitrateKbps = bitrateKbps;
		f.sampleRate = sampleRate;
		f.channels = channels;
		f.samplesPerFrame = samplesPerFrame;
		return true;
	}

	private static int GetSamplesPerFrame(int mpegVersion, int layer) {
		if(layer == 1) return 384;
		if(layer == 2) return 1152;
		// layer 3
		return mpegVersion == 1 ? 1152 : 576;
	}

	private static int GetSampleRate(int mpegVersion, int index) {
		switch(mpegVersion) {
			case 1:
				return index == 0 ? 44100 : (index == 1 ? 48000 : 32000);
			case 2:
				return index == 0 ? 22050 : (index == 1 ? 24000 : 16000);
			case 25:
				return index == 0 ? 11025 : (index == 1 ? 12000 : 8000);
			default:
				return 0;
		}
	}

	private static int GetBitrateKbps(int mpegVersion, int layer, int index) {
		if(index <= 0 || index >= 15) return 0;

		// Tables are in kbps.
		if(layer == 1) {
			int[] v1 = {0,32,64,96,128,160,192,224,256,288,320,352,384,416,448,0};
			int[] v2 = {0,32,48,56,64,80,96,112,128,144,160,176,192,224,256,0};
			return mpegVersion == 1 ? v1[index] : v2[index];
		}
		if(layer == 2) {
			int[] v1 = {0,32,48,56,64,80,96,112,128,160,192,224,256,320,384,0};
			int[] v2 = {0,8,16,24,32,40,48,56,64,80,96,112,128,144,160,0};
			return mpegVersion == 1 ? v1[index] : v2[index];
		}
		// layer 3
		int[] l3v1 = {0,32,40,48,56,64,80,96,112,128,160,192,224,256,320,0};
		int[] l3v2 = {0,8,16,24,32,40,48,56,64,80,96,112,128,144,160,0};
		return mpegVersion == 1 ? l3v1[index] : l3v2[index];
	}

	private static bool TryParseXingHeader(byte[] data, int frameOffset, Frame frame, out int flags, out int frames, out int bytes, out byte[] toc) {
		flags = 0;
		frames = 0;
		bytes = 0;
		toc = null;

		if(frame.layer != 3) return false;

		int sideInfoSize;
		if(frame.mpegVersion == 1)
			sideInfoSize = frame.channels == 1 ? 17 : 32;
		else
			sideInfoSize = frame.channels == 1 ? 9 : 17;

		int xingOffset = frameOffset + 4 + (frame.hasCrc ? 2 : 0) + sideInfoSize;
		if(xingOffset < 0 || xingOffset + 8 > data.Length) return false;

		string tag = "" + (char)data[xingOffset] + (char)data[xingOffset+1] + (char)data[xingOffset+2] + (char)data[xingOffset+3];
		if(tag != "Xing" && tag != "Info") return false;

		int p = xingOffset + 4;
		flags = ReadBE32(data, p); p += 4;
		if((flags & 0x1) != 0) { frames = ReadBE32(data, p); p += 4; }
		if((flags & 0x2) != 0) { bytes = ReadBE32(data, p); p += 4; }
		if((flags & 0x4) != 0) {
			if(p + 100 > data.Length) return false;
			toc = new byte[100];
			Array.Copy(data, p, toc, 0, 100);
			p += 100;
		}

		return true;
	}

	private static int ReadBE32(byte[] data, int offset) {
		if(data == null || offset < 0 || offset + 4 > data.Length) return 0;
		return (data[offset] << 24) | (data[offset+1] << 16) | (data[offset+2] << 8) | data[offset+3];
	}
}
}
