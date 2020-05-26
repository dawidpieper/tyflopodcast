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
public class CommentsWindow : Form {
private Podcast podcast;
private Controller controller;
private Comment[] comments;

private Label lb_comments;
private TextBox edt_comments;
private Button btn_close;

public CommentsWindow(Podcast tpodcast, Comment[] tcomments, Controller tcontroller) {
podcast=tpodcast;
controller=tcontroller;
comments=tcomments;

this.FormBorderStyle = FormBorderStyle.FixedToolWindow;

this.Size = new Size(240,320);
this.StartPosition = FormStartPosition.CenterScreen;
this.Text = "Komentarze do podcastu "+podcast.name+" - Tyflopodcast";

lb_comments = new Label();
lb_comments.Text = "Komentarze";
lb_comments.Size = new Size(120,50);
lb_comments.Location = new Point(20, 20);
this.Controls.Add(lb_comments);

edt_comments = new TextBox();
edt_comments.Size = new Size(240, 120);
edt_comments.Location = new Point(60,20);
edt_comments.ReadOnly = true;
edt_comments.Multiline = true;
this.Controls.Add(edt_comments);

SetComments(comments);

btn_close = new Button();
btn_close.Text = "Zamknij";
btn_close.Size = new Size(50, 50);
btn_close.Location = new Point(180, 170);
btn_close.Click += (sender, e) => this.Close();
this.Controls.Add(btn_close);

this.CancelButton = btn_close;
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

public void SetComments(Comment[] tcomments) {
comments=tcomments;
var sb = new StringBuilder();
foreach(Comment c in comments) {
sb.Append(c.author);
sb.Append("\r\n");
sb.Append(c.time.ToString());
sb.Append("\r\n");
sb.Append(c.content).Replace("\n", "\r\n");
sb.Append("\r\n\r\n");
}
edt_comments.Text = sb.ToString();
}
}
}