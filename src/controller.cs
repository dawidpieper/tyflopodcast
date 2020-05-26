/*
tyflopodcast.net client
Copyright Dawid Pieper
License: General Public License (GPLv3)
*/

using System;
using System.Timers;
using System.Threading;
using System.Windows.Forms;
using System.Net;
using System.Text;
using System.IO;
using System.Linq;
using System.Diagnostics;
using Un4seen.Bass;

namespace Tyflopodcast {
public class Controller {

private int stream=0;
private TPWindow wnd=null;
private PlayerWindow wnd_player=null;
private RadioWindow wnd_radio=null;
private CommentsWindow wnd_comments;
private System.Timers.Timer tm_audioposition=null;

public Controller() {
if(!Bass.BASS_IsStarted()) Bass.BASS_Init(-1, 44100, BASSInit.BASS_DEVICE_DEFAULT, IntPtr.Zero);
}

public void SetWindow(TPWindow twnd) {
wnd=twnd;
}

private void SetURL(string url) {
if(stream!=0) FreeStream();
stream = Bass.BASS_StreamCreateURL(url, 0, BASSFlag.BASS_DEFAULT, null, IntPtr.Zero);
}

private void SetRadio() {
SetURL("http://radio.tyflopodcast.net:8000");
}

private void Play() {
if (stream != 0)
Bass.BASS_ChannelPlay(stream, false);
float vol=0;
Bass.BASS_ChannelGetAttribute(stream, BASSAttribute.BASS_ATTRIB_VOL, ref vol);
if(wnd_player!=null) {
long t = Bass.BASS_ChannelGetLength(stream);
double sec = Bass.BASS_ChannelBytes2Seconds(stream, t);
wnd_player.SetDuration(sec);
wnd_player.SetVolume((int)(vol*100));
}
else if(wnd_radio!=null)
wnd_radio.SetVolume((int)(vol*100));
}

private void FreeStream() {
if(stream!=0) Bass.BASS_StreamFree(stream);
}

public void UpdatePosition() {
if(stream==0 || wnd_player==null) return;
long t = Bass.BASS_ChannelGetPosition(stream);
double sec = Bass.BASS_ChannelBytes2Seconds(stream, t);
wnd_player.SetPosition(sec);
}

public void SetPosition(double position) {
if(stream==0) return;
long t = Bass.BASS_ChannelSeconds2Bytes(stream, position);
Bass.BASS_ChannelSetPosition(stream, t);
UpdatePosition();
}

public void SetVolume(int volume) {
float vol = ((float)volume)/100;
Bass.BASS_ChannelSetAttribute(stream, BASSAttribute.BASS_ATTRIB_VOL, vol);
}

public void PodcastSelected(Podcast p) {
wnd_player = new PlayerWindow(p, this);
SetURL("http://tyflopodcast.net/pobierz.php?id="+p.id.ToString()+"&plik=0");
Play();
tm_audioposition = new System.Timers.Timer(250);
tm_audioposition.AutoReset = true;
tm_audioposition.Enabled = true;
tm_audioposition.Elapsed += (sender, e) => {
UpdatePosition();
};
wnd_player.ShowDialog(wnd);
FreeStream();
tm_audioposition.Stop();
tm_audioposition.Dispose();
tm_audioposition=null;
wnd_player=null;
}

public void RadioSelected() {
SetRadio();
wnd_radio = new RadioWindow(this);
Play();
wnd_radio.ShowDialog(wnd);
FreeStream();
wnd_radio=null;
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
var l = new LoadingWindow("Pobieranie podcastu "+podcast.name);
l.SetStatus("Inicjowanie...");
bool cancelled = true;
Thread dwnThread = new Thread(() => {
using (var client = new WebClient ()) {
client.DownloadProgressChanged += (sender, e) => {
l.SetPercentage(e.ProgressPercentage);
l.SetStatus($"Pobieranie: {e.BytesReceived/1048576} / {e.TotalBytesToReceive/1048576} MB");
};
client.DownloadFileCompleted += (sender, e) => {
cancelled=false;
l.Close();
};
client.DownloadFileAsync(new Uri("http://tyflopodcast.net/pobierz.php?id="+podcast.id.ToString()+"&plik=0"), datadir+"\\"+MakeValidFileName(podcast.name)+".mp3");
}
});
dwnThread.Start();
l.ShowDialog(wnd);
if(cancelled) dwnThread.Abort();
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
if(wnd!=null)
wnd.AddCustomCollection("Szukaj: "+term, results, true);
}

public void ShowComments(Podcast podcast) {
Comment[] comments;
if(Podcasts.GetPodcastComments(podcast.id, out comments)) {
wnd_comments = new CommentsWindow(podcast, comments, this);
wnd_comments.ShowDialog(wnd_player);
}
}
}
}