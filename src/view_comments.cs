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
public class CommentsWindow : Form {
private Podcast podcast;
private Controller controller;
private Comment[] comments;

private Label lb_comments, lb_comment;
private ListBox lst_comments;
private TextBox edt_comment;
private Button btn_close, btn_write;

public CommentsWindow(Podcast tpodcast, Comment[] tcomments, Controller tcontroller) {
podcast=tpodcast;
controller=tcontroller;
comments=tcomments;

this.FormBorderStyle = FormBorderStyle.FixedDialog ;
this.ShowInTaskbar=false;

this.Size = new Size(480, 360);
this.StartPosition = FormStartPosition.CenterScreen;
this.Text = "Komentarze do podcastu "+podcast.name+" - Tyflopodcast";

lb_comments = new Label();
lb_comments.Text = "Komentarze";
lb_comments.Size = new Size(150, 50);
lb_comments.Location = new Point(20, 20);
this.Controls.Add(lb_comments);

lst_comments = new ListBox();
lst_comments.Size = new Size(150, 200);
lst_comments.Location = new Point(20, 80);
this.Controls.Add(lst_comments);

lb_comment = new Label();
lb_comment.Text = "Treść komentarza";
lb_comment.Size = new Size(250, 50);
lb_comments.Location = new Point(190, 20);
this.Controls.Add(lb_comments);

edt_comment = new TextBox();
edt_comment.Size = new Size(250, 200);
edt_comment.Location = new Point(190, 80);
edt_comment.ReadOnly = true;
edt_comment.Multiline = true;
this.Controls.Add(edt_comment);

btn_close = new Button();
btn_close.Text = "Zamknij";
btn_close.Size = new Size(60, 40);
btn_close.Location = new Point(155, 300);
btn_close.Click += (sender, e) => this.Close();
this.Controls.Add(btn_close);

btn_write = new Button();
btn_write.Text = "Napisz";
btn_write.Size = new Size(60, 40);
btn_write.Location = new Point(215, 300);
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
foreach(Comment c in comments) {
StringBuilder sb = new StringBuilder();
sb.Append(c.author);
sb.Append(": ");
sb.Append(c.content.Replace("\n", "").Substring(0, 10));
if(c.content.Length>10) sb.Append("...");
lst_comments.Items.Add(sb.ToString());
}
}

private void ShowComment(Comment comment) {
edt_comment.Text = comment.content.Replace("\n", "\r\n")+"\r\n\r\n"+comment.time.ToString();
}
}
}