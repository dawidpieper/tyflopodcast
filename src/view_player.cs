/*
tyflopodcast.net client
Copyright Dawid Pieper
License: General Public License (GPLv3)
*/

using System;
using System.Windows.Forms;
using System.Drawing;
using System.Text;

namespace Tyflopodcast {
public class PlayerWindow : Form {
private Podcast podcast;
private Controller controller;

private Label lb_timer, lb_volume;
private TrackBar tb_timer, tb_volume;
private Button btn_play, btn_download, btn_comments, btn_close;

public PlayerWindow(Podcast tpodcast, Controller tcontroller) {
podcast=tpodcast;
controller=tcontroller;

this.FormBorderStyle = FormBorderStyle.FixedToolWindow;

this.Size = new Size(240,320);
this.StartPosition = FormStartPosition.CenterScreen;
this.Text = podcast.name+" - Tyflopodcast";

lb_timer = new Label();
lb_timer.Text = "Pobieranie informacji o strumieniu...";
lb_timer.Size = new Size(40,50);
lb_timer.Location = new Point(20, 20);
this.Controls.Add(lb_timer);

tb_timer = new TrackBar();
tb_timer.Size = new Size(240, 50);
tb_timer.Location = new Point(60,20);
tb_timer.Minimum=0;
tb_timer.Maximum=0;
tb_timer.TickFrequency = 15;
tb_timer.LargeChange=60;
tb_timer.SmallChange=10;
tb_timer.Scroll += (sender, e) => {
controller.SetPosition((double)tb_timer.Value);
};
this.Controls.Add(tb_timer);

lb_volume = new Label();
lb_volume.Text = "Głośność";
lb_volume.Size = new Size(40,50);
lb_volume.Location = new Point(20, 90);
this.Controls.Add(lb_volume);

tb_volume = new TrackBar();
tb_volume.Size = new Size(240, 50);
tb_volume.Location = new Point(60, 90);
tb_volume.Minimum=0;
tb_volume.Maximum=100;
tb_volume.TickFrequency = 5;
tb_volume.LargeChange=20;
tb_volume.SmallChange=5;
tb_volume.Scroll += (sender, e) => {
controller.SetVolume(tb_volume.Value);
};
this.Controls.Add(tb_volume);

tb_timer.KeyDown += TBKeyDown;
tb_volume.KeyDown += TBKeyDown;

btn_play = new Button();
btn_play.Text = "Play/Pauza";
btn_play.Size = new Size(50, 50);
btn_play.Location = new Point(20, 170);
btn_play.Click += (sender, e) => controller.TogglePlayback();
this.Controls.Add(btn_play);

btn_download = new Button();
btn_download.Text = "Pobierz";
btn_download.Size = new Size(50, 50);
btn_download.Location = new Point(100, 170);
btn_download.Click += (sender, e) => controller.DownloadPodcast(podcast);
this.Controls.Add(btn_download);

btn_comments = new Button();
btn_comments.Text = "Pokaż komentarze";
btn_comments.Size = new Size(50, 50);
btn_comments.Location = new Point(180, 170);
btn_comments.Click += (sender, e) => controller.ShowComments(podcast);
this.Controls.Add(btn_comments);

btn_close = new Button();
btn_close.Text = "Zamknij";
btn_close.Size = new Size(50, 50);
btn_close.Location = new Point(260, 170);
btn_close.Click += (sender, e) => this.Close();
this.Controls.Add(btn_close);
}

public void TBKeyDown(Object sender, KeyEventArgs e) {
if (e.KeyCode == Keys.Space)
controller.TogglePlayback();
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
}
}