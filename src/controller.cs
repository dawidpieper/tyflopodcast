/*
tyflopodcast.net client
Copyright Dawid Pieper
License: General Public License (GPLv3)
*/

using System;
using System.Timers;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using Un4seen.Bass;
using Un4seen.Bass.AddOn.Fx;

namespace Tyflopodcast {
public class Controller {

private int stream=0;
private TPWindow wnd=null;
private PlayerWindow wnd_player=null;
private RadioWindow wnd_radio=null;
private CommentsWindow wnd_comments;
private System.Timers.Timer tm_audioposition=null;

private string[] args;

public Controller(string[] targs) {
args=targs;
if(!Bass.BASS_IsStarted()) Bass.BASS_Init(-1, 44100, BASSInit.BASS_DEVICE_DEFAULT, IntPtr.Zero);
}

public void SetWindow(TPWindow twnd) {
wnd=twnd;
}

private void SetURL(string url) {
if(stream!=0) FreeStream();
int s = Bass.BASS_StreamCreateURL(url, 0, BASSFlag.BASS_DEFAULT | BASSFlag.BASS_STREAM_DECODE, null, IntPtr.Zero);
stream = BassFx.BASS_FX_TempoCreate(s, BASSFlag.BASS_FX_FREESOURCE);
}

private void SetFile(string file) {
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
long t = Bass.BASS_ChannelGetLength(stream);
double sec = Bass.BASS_ChannelBytes2Seconds(stream, t);
return sec;
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
if(wnd_player!=null && Bass.BASS_ChannelSetPosition(stream, t)==false) {
Bass.BASS_ChannelPause(stream);
var l = new LoadingWindow("Buforowanie...");
l.SetStatus("Buforowanie...");
long length = Bass.BASS_StreamGetFilePosition(stream, BASSStreamFilePosition.BASS_FILEPOS_SIZE);
long prebuffered = Bass.BASS_StreamGetFilePosition(stream, BASSStreamFilePosition.BASS_FILEPOS_DOWNLOAD);
long needed = (long)(length*position/GetDuration());
Task.Factory.StartNew(()=> {
do {
long downloaded = Bass.BASS_StreamGetFilePosition(stream, BASSStreamFilePosition.BASS_FILEPOS_DOWNLOAD);
int p = (int)((100*(downloaded-prebuffered)/(needed-prebuffered)));
if(p<100) l.SetPercentage(p);
} while(Bass.BASS_ChannelSetPosition(stream, t)==false);
if(!l.IsDisposed) l.Close();
});
l.ShowDialog(wnd_player);
}
UpdatePosition();
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
if(location==null)
SetURL("http://tyflopodcast.net/pobierz.php?id="+p.id.ToString()+"&plik=0");
else
SetFile(location);
AudioInfo ai = new AudioInfo(stream);
wnd_player.SetName(ai.title);
wnd_player.SetArtist(ai.artist);
wnd_player.SetChapters(ai.chapters);
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

public void ShowComments(Podcast podcast) {
Comment[] comments;
if(Podcasts.GetPodcastComments(podcast.id, out comments)) {
wnd_comments = new CommentsWindow(podcast, comments, this);
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
Task.Factory.StartNew(()=> {
podcasts = Podcasts.FetchPodcasts(ref leftPages, ref totalPages, reset);
l.SetStatus("Czyszczenie...");
Podcasts.CleanUp();
cancelled = false;
l.Close();
});
Task.Factory.StartNew(()=> {
l.SetStatus("Łączenie...");
bool s=false;
int p=-1;
for(;;) {
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
}
});
l.ShowDialog(wnd);
if(cancelled || podcasts==null) return;//Environment.Exit(0);
}
wnd.Clear();
foreach(Category c in Podcasts.categories) wnd.AddCategory(c);
foreach(Podcast p in podcasts) wnd.AddPodcast(p);
wnd.UpdatePodcasts();

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
System.Diagnostics.Process.Start(url);
}
}
}