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
using System.Collections.Generic;

namespace Tyflopodcast {
public class SearcherWindow : Form {
private Label lb_term, lb_searchin;
private TextBox edt_term;
private ListBox lst_searchin;
private Button btn_search, btn_cancel;
private Podcast[] podcasts;
private Controller controller;

public SearcherWindow(Podcast[] tpodcasts, Controller tcontroller) {
podcasts=tpodcasts;
controller=tcontroller;

this.FormBorderStyle = FormBorderStyle.FixedDialog ;
this.ShowInTaskbar=false;

this.Size = new Size(535, 240);
this.StartPosition = FormStartPosition.CenterScreen;
this.Text = "Przeszukaj Tyflopodcast";

lb_term = new Label();
lb_term.Text = "Szukaj";
lb_term.Size = new Size(40,30);
lb_term.Location = new Point(20, 20);
this.Controls.Add(lb_term);

edt_term = new TextBox();
edt_term.Size = new Size(440, 30);
edt_term.Location = new Point(60,20);
this.Controls.Add(edt_term);

lb_searchin = new Label();
lb_searchin.Text = "Szukaj w";
lb_searchin.Size = new Size(60,30);
lb_searchin.Location = new Point(20, 60);
this.Controls.Add(lb_searchin);

lst_searchin = new ListBox();
lst_searchin.Size = new Size(420, 50);
lst_searchin.Location = new Point(80, 60);
string[] items = {"Tytuły i opisy", "Tytuły", "Opisy", "Komentarze", "Wszystko"};
foreach(string item in items) lst_searchin.Items.Add(item);
lst_searchin.SelectedIndex=0;
this.Controls.Add(lst_searchin);

btn_search = new Button();
btn_search.Text = "Szukaj";
btn_search.Size = new Size(230, 50);
btn_search.Location = new Point(20, 130);
btn_search.Click += (sender,e) => Search();
this.Controls.Add(btn_search);

btn_cancel = new Button();
btn_cancel.Text = "Anuluj";
btn_cancel.Size = new Size(230, 50);
btn_cancel.Location = new Point(270, 130);
this.Controls.Add(btn_cancel);

this.CancelButton = btn_cancel;
this.AcceptButton=btn_search;
}

public void Search() {
string term = edt_term.Text;
if(term=="") return;
string lterm = term.ToLower();
int searchtype = lst_searchin.SelectedIndex;
var result = new List<Podcast>();
foreach(Podcast p in podcasts)
if((searchtype==0 || searchtype==1 || searchtype==4) && p.name.ToLower().Contains(lterm))
result.Add(p);
else if((searchtype==0 || searchtype==2 || searchtype==4) && p.description.ToLower().Contains(lterm))
result.Add(p);
if(searchtype==3 || searchtype==4) {
try {
Podcast[] pd = null;
Podcasts.LoadPodcastsWithComments(term, out pd);
if(pd!=null) {
foreach(Podcast p in pd)
if(!result.Contains(p)) result.Add(p);
}
}
catch {}
}
controller.SearchResults(result.ToArray(), term);
this.Close();
}
}
}