/*
tyflopodcast.net client
Copyright Dawid Pieper
License: General Public License (GPLv3)
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

this.FormBorderStyle = FormBorderStyle.FixedToolWindow;

this.Size = new Size(240,320);
this.StartPosition = FormStartPosition.CenterScreen;
this.Text = "Przeszukaj Tyflopodcast";

lb_term = new Label();
lb_term.Text = "Szukaj";
lb_term.Size = new Size(40,50);
lb_term.Location = new Point(20, 20);
this.Controls.Add(lb_term);

edt_term = new TextBox();
edt_term.Size = new Size(240, 50);
edt_term.Location = new Point(60,20);
this.Controls.Add(edt_term);

lb_searchin = new Label();
lb_searchin.Text = "Szukaj w";
lb_searchin.Size = new Size(40,50);
lb_searchin.Location = new Point(20, 90);
this.Controls.Add(lb_searchin);

lst_searchin = new ListBox();
lst_searchin.Size = new Size(240, 50);
lst_searchin.Location = new Point(60, 90);
string[] items = {"Tytuły i opisy", "Tytuły", "Opisy", "Komentarze", "Wszystko"};
foreach(string item in items) lst_searchin.Items.Add(item);
lst_searchin.SelectedIndex=0;
this.Controls.Add(lst_searchin);

btn_search = new Button();
btn_search.Text = "Szukaj";
btn_search.Size = new Size(100, 50);
btn_search.Location = new Point(20, 170);
btn_search.Click += (sender,e) => Search();
this.Controls.Add(btn_search);

btn_cancel = new Button();
btn_cancel.Text = "Anuluj";
btn_cancel.Size = new Size(100, 50);
btn_cancel.Location = new Point(150, 170);
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