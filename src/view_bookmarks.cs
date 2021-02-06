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

namespace Tyflopodcast {
public class BookmarksWindow : Form {
private Podcast podcast;
private Controller controller;
private double time;

private Label lb_bookmarks, lb_name;
private ListBox lst_bookmarks;
private TextBox edt_name;
private Button btn_delete, btn_add, btn_close;

private Bookmark[] bookmarks=null;

public BookmarksWindow(Podcast tpodcast, double ttime, Controller tcontroller) {
podcast=tpodcast;
controller=tcontroller;
time=ttime;

this.FormBorderStyle = FormBorderStyle.FixedDialog ;
this.ShowInTaskbar=false;

this.Size = new Size(320, 240);
this.StartPosition = FormStartPosition.CenterScreen;
this.Text = "Zakładki podcastu "+podcast.name+" - Tyflopodcast";

lb_bookmarks = new Label();
lb_bookmarks.Text = "Zakładki";
lb_bookmarks.Size = new Size(50, 100);
lb_bookmarks.Location = new Point(20, 20);
this.Controls.Add(lb_bookmarks);

lst_bookmarks = new ListBox();
lst_bookmarks.Size = new Size(150, 100);
lst_bookmarks.Location = new Point(20, 70);
this.Controls.Add(lst_bookmarks);

btn_delete = new Button();
btn_delete.Text = "Usuń";
btn_delete.Size = new Size(150, 50);
btn_delete.Location = new Point(20, 170);
btn_delete.Click += (sender, e) => DeleteBookmark();
this.Controls.Add(btn_delete);

lb_name = new Label();
lb_name.Text = "Nazwa zakładki";
lb_name.Size = new Size(150, 50);
lb_name.Location = new Point(190, 20);
this.Controls.Add(lb_name);

edt_name = new TextBox();
edt_name.Size = new Size(150, 100);
edt_name.Location = new Point(190, 70);
this.Controls.Add(edt_name);

btn_add = new Button();
btn_add.Text = "Dodaj tutaj";
btn_add.Size = new Size(150, 50);
btn_add.Location = new Point(190, 170);
btn_add.Click += (sender, e) => AddBookmark();
this.Controls.Add(btn_add);

btn_close = new Button();
btn_close.Text = "Zamknij";
btn_close.Size = new Size(50, 50);
btn_close.Location = new Point(250, 170);
btn_close.Click += (sender, e) => this.Close();
this.Controls.Add(btn_close);
this.CancelButton = btn_close;

UpdateBookmarks();
}

public void UpdateBookmarks() {
bookmarks = Podcasts.GetPodcastBookmarks(podcast);
lst_bookmarks.Items.Clear();
foreach(Bookmark b in bookmarks) {
lst_bookmarks.Items.Add(b.name);
}
edt_name.Text="";
}

private void AddBookmark() {
if(edt_name.Text=="") return;
controller.AddBookmark(podcast, edt_name.Text, time);
}

private void DeleteBookmark() {
if(bookmarks==null || lst_bookmarks.SelectedIndex<0) return;
controller.DeleteBookmark(podcast, bookmarks[lst_bookmarks.SelectedIndex]);
}
}
}