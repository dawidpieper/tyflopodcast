/*
A part of Tyflopodcast - tyflopodcast.net client.
Copyright (C) 2020 Dawid Pieper
This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, version 3. 
This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details. 
You should have received a copy of the GNU General Public License along with this program. If not, see <https://www.gnu.org/licenses/>. 
*/

using System;
using System.Timers;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using Microsoft.Win32;
using Un4seen.Bass;
using Un4seen.Bass.AddOn.Fx;

namespace Tyflopodcast {
public class Controller {

private int stream=0;
private TPWindow wnd=null;
private PlayerWindow wnd_player=null;
private RadioWindow wnd_radio=null;
private CommentsWindow wnd_comments;
private CommentWriteWindow wnd_commentwrite;
private ContactRadioWindow wnd_contact;
private ContactRadioPhoneWindow wnd_contactphone;
private RadioProgramWindow wnd_program;
private BookmarksWindow wnd_bookmarks;
	private System.Timers.Timer tm_audioposition=null;
	private AudioInfo.Chapter[] currentId3Chapters=null;

	private const double ResumeCompletionThresholdSeconds = 30.0;
	private const int ResumeSaveIntervalMs = 5000;

	private string currentResumeKey = null;
	private string currentStreamUrl = null;
	private Mp3Seek.Info currentStreamMp3Info = null;
	private double currentStreamBaseSeconds = 0;
	private double currentStreamTotalDurationSeconds = 0;

	private string[] args;

public Controller(string[] targs) {
args=targs;
if(!Bass.BASS_IsStarted()) Bass.BASS_Init(-1, 44100, BASSInit.BASS_DEVICE_DEFAULT, IntPtr.Zero);
}

public void SetWindow(TPWindow twnd) {
wnd=twnd;
}

		private void SetURL(string url) {
		currentStreamUrl = url;
		currentStreamMp3Info = null;
		currentStreamBaseSeconds = 0;
		currentStreamTotalDurationSeconds = 0;
		if(stream!=0) FreeStream();
		int s = Bass.BASS_StreamCreateURL(url, 0, BASSFlag.BASS_DEFAULT | BASSFlag.BASS_STREAM_DECODE, null, IntPtr.Zero);
		stream = BassFx.BASS_FX_TempoCreate(s, BASSFlag.BASS_FX_FREESOURCE);
		}

		private void SetFile(string file) {
		currentStreamUrl = null;
		currentStreamMp3Info = null;
		currentStreamBaseSeconds = 0;
		currentStreamTotalDurationSeconds = 0;
		if(stream!=0) FreeStream();
		int s = Bass.BASS_StreamCreateFile(file, 0, 0, BASSFlag.BASS_DEFAULT | BASSFlag.BASS_STREAM_DECODE);
		stream = BassFx.BASS_FX_TempoCreate(s, BASSFlag.BASS_FX_FREESOURCE);
		}

private void SetRadio() {
SetURL("http://radio.tyflopodcast.net:8000");
}

public void Play() {
if (stream != 0)
Bass.BASS_ChannelPlay(stream, false);
float vol=0;
Bass.BASS_ChannelGetAttribute(stream, BASSAttribute.BASS_ATTRIB_VOL, ref vol);
if(wnd_player!=null) {
wnd_player.SetDuration(GetDuration());
wnd_player.SetVolume((int)(vol*100));
}
else if(wnd_radio!=null)
wnd_radio.SetVolume((int)(vol*100));
}

	public double GetDuration() {
	if(stream==0) return 0;
	if(currentStreamTotalDurationSeconds > 0) return currentStreamTotalDurationSeconds;
	long t = Bass.BASS_ChannelGetLength(stream);
	double sec = Bass.BASS_ChannelBytes2Seconds(stream, t);
	return sec;
	}

private void FreeStream() {
if(stream!=0) {
Bass.BASS_StreamFree(stream);
stream=0;
}
}

		private double GetCurrentPositionSeconds() {
		if(stream==0) return 0;
		long t = Bass.BASS_ChannelGetPosition(stream);
		return Bass.BASS_ChannelBytes2Seconds(stream, t);
		}

		private double GetLogicalPositionSeconds() {
		return currentStreamBaseSeconds + GetCurrentPositionSeconds();
		}

		private bool TryRangeSeek(double absoluteSeconds, bool shouldResume) {
		if(stream==0) return false;
		if(string.IsNullOrEmpty(currentStreamUrl) || currentStreamMp3Info==null) return false;

		if(absoluteSeconds < 0) absoluteSeconds = 0;
		double total = GetDuration();
		if(total > 0 && absoluteSeconds > total) absoluteSeconds = total;

		long offset = Mp3Seek.GetFileOffsetForTime(currentStreamMp3Info, absoluteSeconds);
		if(offset < 0) offset = 0;
		int offset32 = offset > int.MaxValue ? int.MaxValue : (int)offset;

		float vol = 0, tempo = 0;
		Bass.BASS_ChannelGetAttribute(stream, BASSAttribute.BASS_ATTRIB_VOL, ref vol);
		Bass.BASS_ChannelGetAttribute(stream, BASSAttribute.BASS_ATTRIB_TEMPO, ref tempo);

		int source = Bass.BASS_StreamCreateURL(currentStreamUrl, offset32, BASSFlag.BASS_DEFAULT | BASSFlag.BASS_STREAM_DECODE, null, IntPtr.Zero);
		if(source==0) return false;
		int newStream = BassFx.BASS_FX_TempoCreate(source, BASSFlag.BASS_FX_FREESOURCE);
		if(newStream==0) {
		Bass.BASS_StreamFree(source);
		return false;
		}

		Bass.BASS_StreamFree(stream);
		stream = newStream;

		Bass.BASS_ChannelSetAttribute(stream, BASSAttribute.BASS_ATTRIB_VOL, vol);
		Bass.BASS_ChannelSetAttribute(stream, BASSAttribute.BASS_ATTRIB_TEMPO, tempo);

		currentStreamBaseSeconds = absoluteSeconds;

		if(wnd_player!=null) {
		wnd_player.SetDuration(GetDuration());
		wnd_player.SetVolume((int)(vol*100));
		}

		if(shouldResume) Bass.BASS_ChannelPlay(stream, false);
		return true;
		}

	public void UpdatePosition() {
	if(stream==0 || wnd_player==null) return;
	double sec = GetLogicalPositionSeconds();
	wnd_player.SetPosition(sec);
	}

		public void SetPosition(double position) {
		if(stream==0) return;
		if(position<0) position=0;
		var active = Bass.BASS_ChannelIsActive(stream);
		bool shouldResume = active == BASSActive.BASS_ACTIVE_PLAYING || active == BASSActive.BASS_ACTIVE_STALLED;
		double relativeSeconds = position - currentStreamBaseSeconds;
		if(relativeSeconds < 0) {
		if(TryRangeSeek(position, shouldResume)) {
		UpdatePosition();
		return;
		}
		return;
		}

		long t = Bass.BASS_ChannelSeconds2Bytes(stream, relativeSeconds);
		if(t<0) return;
		if(Bass.BASS_ChannelSetPosition(stream, t)==false) {
		var err = Bass.BASS_ErrorGetCode();
		if(err == BASSError.BASS_ERROR_POSITION) {
		if(TryRangeSeek(position, shouldResume)) {
		UpdatePosition();
		return;
		}
		}
		if(wnd_player!=null) {
		Bass.BASS_ChannelPause(stream);
		var l = new LoadingWindow("Buforowanie...");
		l.SetStatus("Buforowanie...");
		bool cancelled = false;
		l.FormClosed += (s, e) => cancelled = true;
	var _ = l.Handle;
	long length = Bass.BASS_StreamGetFilePosition(stream, BASSStreamFilePosition.BASS_FILEPOS_SIZE);
	long prebuffered = Bass.BASS_StreamGetFilePosition(stream, BASSStreamFilePosition.BASS_FILEPOS_DOWNLOAD);
		double duration = GetDuration();
		long needed = (length > 0 && duration > 0) ? (long)(length*position/duration) : -1;
		Task.Factory.StartNew(async ()=> {
		do {
		if(cancelled) break;
		await Task.Delay(250);
	long downloaded = Bass.BASS_StreamGetFilePosition(stream, BASSStreamFilePosition.BASS_FILEPOS_DOWNLOAD);
	if(needed > 0 && needed > prebuffered) {
	int p = (int)((100*(downloaded-prebuffered)/(needed-prebuffered)));
	if(p<0) p=0;
	if(p>100) p=100;
	if(p<100) l.SetPercentage(p);
	}
	} while(Bass.BASS_ChannelSetPosition(stream, t)==false && !cancelled);
	if(!l.IsDisposed) l.BeginInvoke(new Action(()=> { if(!l.IsDisposed) l.Close(); }));
		});
		l.ShowDialog(wnd_player);
		if(shouldResume) Bass.BASS_ChannelPlay(stream, false);
		}
		}
		UpdatePosition();
		}

public void RestartFromBeginning() {
if(stream==0) return;
bool wasPlaying = Bass.BASS_ChannelIsActive(stream) == BASSActive.BASS_ACTIVE_PLAYING;
if(!string.IsNullOrEmpty(currentResumeKey))
Podcasts.ClearResumePosition(currentResumeKey);
SetPosition(0);
if(!string.IsNullOrEmpty(currentResumeKey))
Podcasts.ClearResumePosition(currentResumeKey);
if(wasPlaying) Play();
}

public void SetVolume(int volume) {
float vol = ((float)volume)/100;
Bass.BASS_ChannelSetAttribute(stream, BASSAttribute.BASS_ATTRIB_VOL, vol);
}

public void SetTempo(int tempo) {
float t=0;
if(tempo<0) t=tempo;
else t=tempo*3;
Bass.BASS_ChannelSetAttribute(stream, BASSAttribute.BASS_ATTRIB_TEMPO, t);
}

	public void PodcastSelected(Podcast p, string location=null) {
	wnd_player = new PlayerWindow(p, this);
	currentId3Chapters=null;
	string resumeKey = (location==null) ? Podcasts.GetResumeKeyForPodcast(p.id) : Podcasts.GetResumeKeyForFile(location);
	currentResumeKey = resumeKey;
	double resumeSeconds = Podcasts.GetResumePosition(resumeKey);
	double lastSavedPosition = -1;
	System.Timers.Timer tm_resumeSave = null;
		if(location==null)
		SetURL("http://tyflopodcast.net/pobierz.php?id="+p.id.ToString()+"&plik=0");
		else
		SetFile(location);
		UpdateAudioInfo(p);
		if(location==null && !string.IsNullOrEmpty(currentStreamUrl)) {
		if(Mp3Seek.TryFetchInfo(currentStreamUrl, out Mp3Seek.Info info)) {
		currentStreamMp3Info = info;
		currentStreamTotalDurationSeconds = info.durationSeconds;
		}
		}
		if(wnd_player!=null) wnd_player.SetDuration(GetDuration());

		bool doResume = !string.IsNullOrEmpty(resumeKey) && resumeSeconds > 0;
		double initialDuration = GetDuration();
		if(doResume && initialDuration > 0 && (initialDuration - resumeSeconds) <= ResumeCompletionThresholdSeconds) {
		Podcasts.ClearResumePosition(resumeKey);
	doResume = false;
	}

	if(doResume) {
	SetPosition(resumeSeconds);
	}

	Play();

	if(!string.IsNullOrEmpty(resumeKey)) {
	tm_resumeSave = new System.Timers.Timer(ResumeSaveIntervalMs);
	tm_resumeSave.AutoReset = true;
	tm_resumeSave.Enabled = true;
		tm_resumeSave.Elapsed += (sender, e) => {
		try {
		if(stream==0) return;
		double positionSeconds = GetLogicalPositionSeconds();
		if(positionSeconds < 1.0) return;
		double duration = GetDuration();
		if(duration>0 && (duration - positionSeconds) <= ResumeCompletionThresholdSeconds) {
		Podcasts.ClearResumePosition(resumeKey);
	return;
	}
	if(lastSavedPosition>=0 && Math.Abs(positionSeconds-lastSavedPosition) < 1.0) return;
	Podcasts.SetResumePosition(resumeKey, (float)positionSeconds);
	lastSavedPosition = positionSeconds;
	} catch {
	}
	};
	tm_resumeSave.Start();
	}

tm_audioposition = new System.Timers.Timer(250);
tm_audioposition.AutoReset = true;
tm_audioposition.Enabled = true;
tm_audioposition.Elapsed += (sender, e) => {
UpdatePosition();
};
wnd_player.ShowDialog(wnd);

if(tm_resumeSave!=null) {
tm_resumeSave.Stop();
tm_resumeSave.Dispose();
tm_resumeSave=null;
}
		if(!string.IsNullOrEmpty(resumeKey) && stream!=0) {
		try {
		double positionSeconds = GetLogicalPositionSeconds();
		if(positionSeconds < 1.0) {
		Podcasts.ClearResumePosition(resumeKey);
		} else {
		double duration = GetDuration();
	if(duration>0 && (duration - positionSeconds) <= ResumeCompletionThresholdSeconds)
	Podcasts.ClearResumePosition(resumeKey);
	else
	Podcasts.SetResumePosition(resumeKey, (float)positionSeconds);
	}
	} catch {
	}
	}

FreeStream();
tm_audioposition.Stop();
tm_audioposition.Dispose();
		tm_audioposition=null;
		currentResumeKey=null;
		currentStreamUrl=null;
		currentStreamMp3Info=null;
		currentStreamBaseSeconds=0;
		currentStreamTotalDurationSeconds=0;
		wnd_player=null;
		}

private SYNCPROC metaSync;

public void RadioSelected() {
SetRadio();
wnd_radio = new RadioWindow(this);
metaSync = new SYNCPROC(onMetaReceive);
Bass.BASS_ChannelSetSync(stream, BASSSync.BASS_SYNC_META, 0, metaSync, IntPtr.Zero);
Play();
wnd_radio.ShowDialog(wnd);
FreeStream();
wnd_radio=null;
}

private void onMetaReceive(int handle, int channel, int data, IntPtr user) {
string[] tags = Bass.BASS_ChannelGetTagsMETA(channel);
Regex regex = new Regex(@"([^\=]+)\=\'([^\']+)*\'");
string name;
foreach (string tag in tags) {
Match m = regex.Match(tag);
if(m.Success) {
TextInfo ti = new CultureInfo("pl-PL",false).TextInfo;
name = m.Groups[2].Captures[0].Value.Replace("_", " ");
name = ti.ToTitleCase(name);
wnd_radio.SetName(name);
}
}
}



public void TogglePlayback() {
if(stream==0) return;
if(Bass.BASS_ChannelIsActive(stream) == BASSActive.BASS_ACTIVE_PLAYING)
Bass.BASS_ChannelPause(stream);
else
Bass.BASS_ChannelPlay(stream, !(Bass.BASS_ChannelIsActive(stream) == BASSActive.BASS_ACTIVE_PAUSED));
}

private string MakeValidFileName(string text) {
StringBuilder sb = new StringBuilder(text.Length);
var invalids = Path.GetInvalidFileNameChars();
for (int i = 0; i < text.Length; i++) {
char c = text[i];
if(invalids.Contains(c))
sb.Append("_");
else
sb.Append(c);
}
return sb.ToString();
}

public void DownloadPodcast(Podcast podcast) {
string datadir = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)+"\\tyflopodcast\\downloads";
System.IO.Directory.CreateDirectory(datadir);
OpenDownloader("http://tyflopodcast.net/pobierz.php?id="+podcast.id.ToString()+"&plik=0", datadir+"\\"+MakeValidFileName(podcast.name)+".mp3", "Pobieranie podcastu "+podcast.name);
}

public void ShowDownloads() {
string datadir = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)+"\\tyflopodcast\\downloads";
System.IO.Directory.CreateDirectory(datadir);
Process.Start("explorer.exe", datadir);
}

public void SearchPodcasts(Podcast[] podcasts) {
var l = new SearcherWindow(podcasts, this);
l.ShowDialog(wnd);
}

public void SearchResults(Podcast[] results, string term="") {
if(results.Count()==0) return;
var lresults = new List<Podcast>();
var ids = new List<int>();
Podcast l = new Podcast();
l.id=0;
foreach(Podcast p in results) {
if(ids.Contains(p.id)) continue;
int n=lresults.Count();
if(l.id!=0) {
for(; n>0; --n)
if(lresults[n-1].time>p.time) break;
}
lresults.Insert(n, p);
ids.Add(p.id);
l=p;
}
if(wnd!=null)
wnd.AddCustomCollection("Szukaj: "+term, lresults.ToArray(), true);
}

public void ShowComments(Podcast podcast, bool playing=false) {
Comment[] comments;
if(Podcasts.GetPodcastComments(podcast.id, out comments)) {
wnd_comments = new CommentsWindow(podcast, comments, this, playing);
wnd_comments.ShowDialog(wnd_player);
}
}

public void UpdateDatabase(bool reset=false) {
if(wnd==null) return;
bool cancelled = true;
var l = new LoadingWindow("Pobieranie bazy podcastów");
l.SetStatus("Inicjowanie...");
Podcast[] podcasts=null;
bool localLoaded = Podcasts.GetLocalPodcasts(out podcasts);
bool downloadRemote=true;
if(localLoaded && !reset)
if(MessageBox.Show("Czy chcesz zaktualizować listę dostępnych podcastów?", "Tyflopodcast", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.No)
downloadRemote=false;
if(downloadRemote) {
int totalPages=-1, leftPages=-1;
CancellationTokenSource cts=new CancellationTokenSource();
CancellationToken ct = cts.Token;
Task.Factory.StartNew(async ()=> {
l.SetStatus("Łączenie...");
bool s=false;
int p=-1;
for(;;) {
await Task.Delay(250);
if(!s && totalPages!=-1) {
s=true;
l.SetStatus("Pobieranie informacji o bazie podcastów...");
}
else if(s && p!=leftPages) {
l.SetStatus("Pobieranie strony "+(totalPages-leftPages).ToString()+" z "+totalPages.ToString()+"...");
p=leftPages;
int pr = (int)((double)(totalPages-leftPages)/totalPages*100.0);
l.SetPercentage(pr);
if(leftPages==0) break;
}
if(ct.IsCancellationRequested) return;
}
}, ct);
Task.Factory.StartNew(()=> {
podcasts = Podcasts.FetchPodcasts(ref leftPages, ref totalPages, reset);
l.SetStatus("Czyszczenie...");
Podcasts.CleanUp();
cancelled = false;
l.Close();
});
l.ShowDialog(wnd);
cts.Cancel();
if(cancelled || podcasts==null) return;//Environment.Exit(0);
}
wnd.Clear();
foreach(Category c in Podcasts.categories) wnd.AddCategory(c);
foreach(Podcast p in podcasts) wnd.AddPodcast(p);
wnd.UpdatePodcasts();
wnd.SetLikedPodcasts(Podcasts.GetLikedPodcasts());

}

public void Initiate() {
bool reset=false;
Podcast p;
string location=null;
for(int i=0; i<args.Count(); ++i) {
if(args[i].ToLower()=="-b") reset=true;
else if(args[i].ToLower()=="-f" && i<args.Count()-1) {
location=args[i+1];
++i;
}
}
UpdateDatabase(reset);
if(location!=null) {
p = new Podcast();
p.name=location;
PodcastSelected(p, location);
}
}

public void ShowURL(string url) {
System.Diagnostics.Process.Start("explorer", url);
}

public void ContactRadio() {
(bool available, string title, string meeting) = Podcasts.GetRadioContactInfo();
if(available) {
wnd_radio.ShowContactRadioContext(meeting!=null);
} else
MessageBox.Show("W tej chwili możliwość kontaktu jest wyłączona, możliwe, że nie trwa teraz żadna audycja interaktywna lub prowadzący nie umożliwił jeszcze komunikacji.", "Kontakt niemożliwy", MessageBoxButtons.OK, MessageBoxIcon.Error);
}

public void ContactRadioText() {
(bool available, string title, string meeting) = Podcasts.GetRadioContactInfo();
if(available) {
wnd_contact = new ContactRadioWindow(this, title);
wnd_contact.ShowDialog(wnd_radio!=null ? wnd_radio : wnd);
} else
MessageBox.Show("W tej chwili możliwość kontaktu jest wyłączona, możliwe, że nie trwa teraz żadna audycja interaktywna lub prowadzący nie umożliwił jeszcze komunikacji.", "Kontakt niemożliwy", MessageBoxButtons.OK, MessageBoxIcon.Error);
}

public void ContactRadioZoom() {
(bool available, string title, string meeting) = Podcasts.GetRadioContactInfo();
if(available && meeting!=null) {
RegistryKey key = Registry.ClassesRoot.OpenSubKey("zoommtg");
if(key==null) {
if(MessageBox.Show("Aplikacja Zoom nie została zainstalowana na tym komputerze. Czy chcesz teraz przejść do jej strony pobierania?", "Nie znaleziono aplikacji Zoom", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
ShowURL("https://zoom.us/download");
}
else {
ShowURL("zoommtg://zoom.us/join?action=join&confno="+meeting.ToString());
}
} else
MessageBox.Show("W tej chwili opcja kontaktu głosowego nie jest dostępna.", "Kontakt niemożliwy", MessageBoxButtons.OK, MessageBoxIcon.Error);
}

public void ContactRadioBrowser() {
(bool available, string title, string meeting) = Podcasts.GetRadioContactInfo();
if(available && meeting!=null)
ShowURL("https://zoom.us/wc/join/"+meeting.ToString());
else
MessageBox.Show("W tej chwili opcja kontaktu głosowego nie jest dostępna.", "Kontakt niemożliwy", MessageBoxButtons.OK, MessageBoxIcon.Error);
}

public void ContactRadioPhone() {
(bool available, string title, string meeting) = Podcasts.GetRadioContactInfo();
if(available && meeting!=null) {
wnd_contactphone = new ContactRadioPhoneWindow(this, meeting);
wnd_contactphone.ShowDialog(wnd_radio);
}
else
MessageBox.Show("W tej chwili opcja kontaktu głosowego nie jest dostępna.", "Kontakt niemożliwy", MessageBoxButtons.OK, MessageBoxIcon.Error);
}

public void SendRadioContact(string name, string message) {
(bool suc, string error) = Podcasts.SendRadioContact(name, message);
if(suc) {
wnd_contact.Close();
wnd_contact=null;
} else
MessageBox.Show(error, "Wysłanie wiadomości nie powiodło się", MessageBoxButtons.OK, MessageBoxIcon.Error);
}

public void ShowRadioProgram() {
(bool available, string text) = Podcasts.GetRadioProgram();
if(available) {
wnd_program = new RadioProgramWindow(this, text);
wnd_program.ShowDialog(wnd);
} else
MessageBox.Show("W tej chwili ramówka nie jest dostępna.", "Ramówka niedostępna", MessageBoxButtons.OK, MessageBoxIcon.Error);
}

public void CheckForUpdates(bool confirm=false) {
(bool available, string text) = Podcasts.CheckForUpdates();
if(available) {
if(MessageBox.Show("Dostępna jest nowa wersja programu. Czy chcesz przejść teraz do strony pobierania?", "Dostępna aktualizacja", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
ShowURL("https://github.com/dawidpieper/tyflopodcast/releases");
} else if(confirm)
MessageBox.Show("Używasz najnowszej wersji programu.", "Nie znaleziono dostępnych aktualizacji", MessageBoxButtons.OK, MessageBoxIcon.Information);
}

private bool OpenDownloader(string source, string destination, string label="Pobieranie") {
var l = new LoadingWindow(label);
l.SetStatus("Inicjowanie...");
bool cancelled = true;
using (var client = new WebClient ()) {
client.DownloadProgressChanged += (sender, e) => {
l.SetPercentage(e.ProgressPercentage);
l.SetStatus($"Pobieranie: {e.BytesReceived/1048576} / {e.TotalBytesToReceive/1048576} MB");
};
client.DownloadFileCompleted += (sender, e) => {
cancelled=false;
l.Close();
};
client.DownloadFileAsync(new Uri(source), destination);
l.ShowDialog(wnd);
if(cancelled) client.CancelAsync();
}
return !cancelled;
}

public void WriteComment(Podcast podcast) {
(string action, Dictionary<string,string> fields) = Podcasts.GetCommentsNonce(podcast);
if(action==null) return;
wnd_commentwrite = new CommentWriteWindow(this, podcast, action, fields);
wnd_commentwrite.ShowDialog(wnd_comments);
}

public void PublishComment(Podcast podcast, string action, Dictionary<string,string> fields, string name, string mail, string url, string comment) {
if(Podcasts.WriteComment(action, fields, name, mail, url, comment))
wnd_commentwrite.Close();
Comment[] comments;
if(Podcasts.GetPodcastComments(podcast.id, out comments) && wnd_comments!=null) {
wnd_comments.SetComments(comments);
}
}

public void SetLikedPodcast(Podcast podcast, bool liked) {
if(liked) Podcasts.LikePodcast(podcast);
else Podcasts.DislikePodcast(podcast);
wnd.SetLikedPodcasts(Podcasts.GetLikedPodcasts());
}

public void Bookmarks(Podcast podcast, double time) {
wnd_bookmarks = new BookmarksWindow(podcast, time, this);
wnd_bookmarks.ShowDialog(wnd_player);
}

public void AddBookmark(Podcast podcast, string name, double time) {
Podcasts.AddBookmark(podcast, name, (float)time);
wnd_bookmarks.UpdateBookmarks();
UpdateAudioInfo(podcast);
}

public void DeleteBookmark(Podcast podcast, Bookmark bookmark) {
if(bookmark.podcast!=podcast.id) return;
Podcasts.DeleteBookmark(bookmark);
wnd_bookmarks.UpdateBookmarks();
UpdateAudioInfo(podcast);
}

private void UpdateAudioInfo(Podcast p) {
if(stream==0) return;
AudioInfo ai;
//try {
ai = new AudioInfo(stream);
//} catch(Exception) {return;}
if(ai==null) return;
wnd_player.SetName(ai.title);
wnd_player.SetArtist(ai.artist);
var chapters = new List<AudioInfo.Chapter>();
AudioInfo.Chapter[] id3Chapters = ai.chapters;
if(id3Chapters!=null && id3Chapters.Length>0) currentId3Chapters=id3Chapters;
else if(currentId3Chapters==null && id3Chapters!=null) currentId3Chapters=id3Chapters;
if(currentId3Chapters!=null)
foreach(AudioInfo.Chapter ch in currentId3Chapters) chapters.Add(ch);
foreach(Bookmark b in Podcasts.GetPodcastBookmarks(p)) {
var ch = new AudioInfo.Chapter();
ch.name="Zakładka: "+b.name;
ch.time=b.time;
ch.userDefined=true;
chapters.Add(ch);
}
wnd_player.SetChapters(chapters.ToArray());
}
}
}
