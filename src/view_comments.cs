/*
A part of Tyflopodcast - tyflopodcast.net client.
Copyright (C) 2020, 2021 Dawid Pieper
This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, version 3. 
This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details. 
You should have received a copy of the GNU General Public License along with this program. If not, see <https://www.gnu.org/licenses/>. 
*/

using System;
using System.Windows.Forms;
using System.Drawing;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Tyflopodcast {
public class CommentsWindow : Form {
private Podcast podcast;
private Controller controller;
private Comment[] comments;

private Label lb_comments, lb_comment, lb_chapters;
private ListBox lst_comments, lst_chapters;
private TextBox edt_comment;
private Button btn_close;
private Button btn_write;

private AudioInfo.Chapter[] chapters=null;
private bool playing;

public CommentsWindow(Podcast tpodcast, Comment[] tcomments, Controller tcontroller, bool tplaying) {
podcast=tpodcast;
controller=tcontroller;
comments=tcomments;
playing=tplaying;

this.FormBorderStyle = FormBorderStyle.FixedDialog ;
this.ShowInTaskbar=false;
this.AutoScroll=true;

this.Size = new Size(755, 550);
this.StartPosition = FormStartPosition.CenterScreen;
this.Text = "Komentarze do podcastu "+podcast.name+" - Tyflopodcast";

lb_comments = new Label();
lb_comments.Text = "&Komentarze";
lb_comments.Size = new Size(70, 30);
lb_comments.Location = new Point(210, 20);
this.Controls.Add(lb_comments);

lst_comments = new ListBox();
lst_comments.Size = new Size(170, 300);
lst_comments.Location = new Point(20, 40);
lst_comments.HorizontalScrollbar=true;
lst_comments.IntegralHeight=true;
this.Controls.Add(lst_comments);

lb_comment = new Label();
lb_comment.Text = "&Treść komentarza";
lb_comment.Size = new Size(250, 50);
lb_comments.Location = new Point(20, 20);
this.Controls.Add(lb_comments);

edt_comment = new TextBox();
edt_comment.Size = new Size(510, 289);
edt_comment.Location = new Point(210, 40);
edt_comment.ReadOnly = true;
edt_comment.Multiline = true;
this.Controls.Add(edt_comment);

lb_chapters = new Label();
lb_chapters.Text = "&Rozdziały";
lb_chapters.Size = new Size(240, 50);
lb_chapters.Location = new Point(205, 20);
lb_chapters.Visible=false;
this.Controls.Add(lb_chapters);

lst_chapters = new ListBox();
lst_chapters.Size = new Size(700, 80);
lst_chapters.Location = new Point(20, 410);
lst_chapters.Visible=false;
lst_chapters.DoubleClick += (sender, e) => {
GoToChapter();
};
lst_chapters.KeyDown += CommentsKeyDown;
this.Controls.Add(lst_chapters);

btn_close = new Button();
btn_close.Text = "Zamknij";
btn_close.Size = new Size(350, 40);
btn_close.Location = new Point(20, 350);
btn_close.Click += (sender, e) => this.Close();
this.Controls.Add(btn_close);

btn_write = new Button();
btn_write.Text = "&Napisz";
btn_write.Size = new Size(350, 40);
btn_write.Location = new Point(370, 350);
btn_write.Click += (sender, e) => controller.WriteComment(podcast);
this.Controls.Add(btn_write);

lst_comments.SelectedIndexChanged += (sender, e) => {
if(lst_comments.SelectedIndex<comments.Count()) ShowComment(comments[lst_comments.SelectedIndex]);
};

this.CancelButton = btn_close;

SetComments(comments);
}

public void SetComments(Comment[] tcomments) {
comments=tcomments;
lst_comments.Items.Clear();
int largest=0, maxind=-1;
foreach(Comment c in comments) {
StringBuilder sb = new StringBuilder();
sb.Append(c.author);
sb.Append(": ");
string con = c.content.Replace("\n", "");
if(con.Length>100) con=con.Substring(0, 100);
sb.Append(con);
if(c.content.Length>100) sb.Append("...");
string str = sb.ToString();
lst_comments.Items.Add(str);
if(str.Length>largest) {
largest=str.Length;
maxind=lst_comments.Items.Count-1;
}
}
if(maxind>=0) {
Graphics g = lst_comments.CreateGraphics();
int hzSize = (int) g.MeasureString(lst_comments.Items[maxind].ToString(),lst_comments.Font).Width;
lst_comments.HorizontalExtent = hzSize;
lst_comments.HorizontalScrollbar=true;
}
}

private void CommentsKeyDown(Object sender, KeyEventArgs e) {
if(sender==(Object)lst_chapters && e.KeyCode == Keys.Enter) {
GoToChapter();
controller.Play();
}
}

private void GoToChapter() {
if(lst_chapters.SelectedIndex>=0 && lst_chapters.SelectedIndex<chapters.Count())
controller.SetPosition(chapters[lst_chapters.SelectedIndex].time);
}

private void ShowComment(Comment comment) {
edt_comment.Text = comment.content.Replace("\n", "\r\n")+"\r\n\r\n"+comment.time.ToString();
if(playing) {
var l = new List<AudioInfo.Chapter>();
string pattern = @"\d\d\:\d\d\:\d\d";
Regex rgx = new Regex(pattern);
foreach(string line in comment.content.Split(new[] {"\r\n", "\r", "\n"}, StringSplitOptions.None)) {
Match match = rgx.Match(line);
if(match.Success) {
var ch = new AudioInfo.Chapter();
ch.name = line.Replace(match.Value, "");
var t = match.Value.Split(':');
ch.time = int.Parse(t[0])*3600+int.Parse(t[1])*60+int.Parse(t[2]);
l.Add(ch);
}
}
if(l.Count>0) {
chapters=l.ToArray();
lst_chapters.Items.Clear();
foreach(var c in chapters) lst_chapters.Items.Add(c.name);
lb_chapters.Visible=true;
lst_chapters.Visible=true;
} else {
chapters=null;
lb_chapters.Visible=false;
lst_chapters.Visible=false;
}
}
}
}
}