/*
A part of Tyflopodcast - tyflopodcast.net client.
Copyright (C) 2020 Dawid Pieper
This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, version 3. 
This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details. 
You should have received a copy of the GNU General Public License along with this program. If not, see <https://www.gnu.org/licenses/>. 
*/

using System;
using System.Windows.Forms;
using System.Drawing;
using System.Text;
using System.Linq;

namespace Tyflopodcast {
public class PlayerWindow : Form {
private Podcast podcast;
private Controller controller;

private Label lb_timer, lb_volume, lb_tempo, lb_chapters;
private TyfloTrackBar tb_timer, tb_volume, tb_tempo;
private ListBox lst_chapters;
private Button btn_play, btn_download, btn_comments, btn_close;

private string name, artist;

private AudioInfo.Chapter[] chapters=null;


public PlayerWindow(Podcast tpodcast, Controller tcontroller) {
podcast=tpodcast;
controller=tcontroller;

this.FormBorderStyle = FormBorderStyle.FixedToolWindow;

this.Size = new Size(480, 360);
this.StartPosition = FormStartPosition.CenterScreen;
name=podcast.name;
artist=null;
UpdateWindowCaption();

lb_timer = new Label();
lb_timer.Text = "Pobieranie informacji o strumieniu...";
lb_timer.Size = new Size(50, 50);
lb_timer.Location = new Point(20, 20);
this.Controls.Add(lb_timer);

tb_timer = new TyfloTrackBar();
tb_timer.Size = new Size(130, 50);
tb_timer.Location = new Point(70, 20);
tb_timer.Minimum=0;
tb_timer.Maximum=0;
tb_timer.TickFrequency = 15;
tb_timer.LargeChange=60;
tb_timer.SmallChange=10;
tb_timer.PreviewKeyDown += tb_timer_keyfilter;
tb_timer.Scroll += (sender, e) => {
controller.SetPosition((double)tb_timer.Value);
};
this.Controls.Add(tb_timer);

lb_volume = new Label();
lb_volume.Text = "Głośność";
lb_volume.Size = new Size(50, 50);
lb_volume.Location = new Point(20, 90);
this.Controls.Add(lb_volume);

tb_volume = new TyfloTrackBar();
tb_volume.Size = new Size(130, 50);
tb_volume.Location = new Point(70, 90);
tb_volume.Minimum=0;
tb_volume.Maximum=100;
tb_volume.TickFrequency = 5;
tb_volume.LargeChange=20;
tb_volume.SmallChange=5;
tb_volume.Scroll += (sender, e) => {
controller.SetVolume(tb_volume.Value);
};
this.Controls.Add(tb_volume);

lb_tempo = new Label();
lb_tempo.Text = "Tempo odtwarzania";
lb_tempo.Size = new Size(50, 50);
lb_tempo.Location = new Point(20, 160);
this.Controls.Add(lb_tempo);

tb_tempo = new TyfloTrackBar();
tb_tempo.Size = new Size(130, 50);
tb_tempo.Location = new Point(70, 160);
tb_tempo.Minimum=-50;
tb_tempo.Maximum=50;
tb_tempo.TickFrequency = 5;
tb_tempo.LargeChange=10;
tb_tempo.SmallChange=1;
tb_tempo.Scroll += (sender, e) => {
controller.SetTempo(tb_tempo.Value);
};
this.Controls.Add(tb_tempo);

lb_chapters = new Label();
lb_chapters.Text = "Rozdziały";
lb_chapters.Size = new Size(240, 50);
lb_chapters.Location = new Point(220, 20);
lb_chapters.Visible=false;
this.Controls.Add(lb_chapters);

lst_chapters = new ListBox();
lst_chapters.Size = new Size(240, 140);
lst_chapters.Location = new Point(220, 80);
lst_chapters.Visible=false;
lst_chapters.DoubleClick += (sender, e) => {
GoToChapter();
};
this.Controls.Add(lst_chapters);

tb_timer.KeyDown += TBKeyDown;
tb_volume.KeyDown += TBKeyDown;
tb_tempo.KeyDown += TBKeyDown;
lst_chapters.KeyDown += TBKeyDown;

btn_play = new Button();
btn_play.Text = "Play/Pauza";
btn_play.Size = new Size(70, 100);
btn_play.Location = new Point(20, 240);
btn_play.Click += (sender, e) => controller.TogglePlayback();
this.Controls.Add(btn_play);

btn_download = new Button();
btn_download.Text = "Pobierz";
btn_download.Size = new Size(70, 100);
btn_download.Location = new Point(110, 240);
btn_download.Click += (sender, e) => controller.DownloadPodcast(podcast);
this.Controls.Add(btn_download);

btn_comments = new Button();
btn_comments.Text = "Pokaż komentarze";
btn_comments.Size = new Size(70, 100);
btn_comments.Location = new Point(200, 240);
btn_comments.Click += (sender, e) => controller.ShowComments(podcast);
this.Controls.Add(btn_comments);

btn_close = new Button();
btn_close.Text = "Zamknij";
btn_close.Size = new Size(70, 100);
btn_close.Location = new Point(290, 240);
btn_close.Click += (sender, e) => this.Close();
this.Controls.Add(btn_close);

this.CancelButton = btn_close;;

if(podcast.id==0) {
btn_download.Enabled=false;
btn_comments.Enabled=false;
}
}

void tb_timer_keyfilter(Object sender, PreviewKeyDownEventArgs e) {
switch(e.KeyCode) {
case Keys.Up:
case Keys.Down:
e.IsInputKey = true;
break;
}
}

public void TBKeyDown(Object sender, KeyEventArgs e) {
if (e.KeyCode == Keys.Space)
controller.TogglePlayback();
if(sender==(Object)lst_chapters && e.KeyCode == Keys.Enter) {
GoToChapter();
controller.Play();
}
}

public void SetDuration(double duration) {
tb_timer.Maximum = (int)duration;
tb_timer.Update();
UpdatePosition();
}

public void SetPosition(double position) {
tb_timer.Value = (int)position;
tb_timer.Update();
UpdatePosition();
}

private string FormatTime(int tim) {
int hr, min, sec;
hr = tim/3600;
min = (tim%3600)/60;
sec = tim%60;
StringBuilder sb = new StringBuilder();
if(hr>0) {
sb.Append(hr.ToString("D2"));
sb.Append(" godz. ");
}
if(min>0) {
sb.Append(min.ToString("D2"));
sb.Append(" min. ");
}
sb.Append(sec.ToString("D2"));
sb.Append(" sek.");
return sb.ToString();
}

private void UpdatePosition() {
string text = FormatTime(tb_timer.Value) + " z " + FormatTime(tb_timer.Maximum);
lb_timer.Text = text;
}

public void SetVolume(int volume) {
tb_volume.Value = volume;
tb_volume.Update();
}

private void UpdateWindowCaption() {
StringBuilder sb = new StringBuilder();
if(artist!="" && artist!=null) {
sb.Append(artist);
sb.Append(": ");
}
sb.Append(name);
sb.Append(" - Tyflopodcast");
this.Text=sb.ToString();
}

public void SetName(string tname) {
if(tname==null || tname=="") return;
name=tname;
UpdateWindowCaption();
}

public void SetArtist(string tartist) {
if(tartist==null || tartist=="") return;
artist=tartist;
UpdateWindowCaption();
}

private void GoToChapter() {
if(lst_chapters.SelectedIndex>=0 && lst_chapters.SelectedIndex<chapters.Count())
controller.SetPosition(chapters[lst_chapters.SelectedIndex].time);
}

public void SetChapters(AudioInfo.Chapter[] tchapters) {
if(tchapters==null || tchapters.Count()==0) return;
chapters=tchapters;
lst_chapters.Items.Clear();
foreach(AudioInfo.Chapter c in chapters) lst_chapters.Items.Add(c.name);
lb_chapters.Visible=true;
lst_chapters.Visible=true;
}
}
}