/*
A part of Tyflopodcast - tyflopodcast.net client.
Copyright (C) 2020, 2021 Dawid Pieper
This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, version 3. 
This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details. 
You should have received a copy of the GNU General Public License along with this program. If not, see <https://www.gnu.org/licenses/>. 
*/

using System;
using System.Text;
using System.Windows.Forms;
using System.Drawing;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Tyflopodcast {
public class TPWindow : Form {

private struct CustomCollection {
public string name;
public Podcast[] podcasts;
}

private Label lb_categories, lb_podcasts, lb_description;
private ListBox lst_categories, lst_podcasts;
private TextBox edt_description;

private List<Category> categories;
private List<Podcast> podcasts, currentPodcasts;
private List<CustomCollection> collections;
private List<int> likes;

private Task updateWorker = null;
private CancellationTokenSource updateWorkerCTS = null;
private CancellationToken updateWorkerCT;

private Controller controller;

private ToolStripMenuItem item_likepodcast, item_ctxlikepodcast;

public TPWindow(Controller tcontroller) {
controller=tcontroller;
controller.SetWindow(this);
categories = new List<Category>();
podcasts = new List<Podcast>();
currentPodcasts = new List<Podcast>();
collections = new List<CustomCollection>();
likes = new List<int>();

this.Shown += (sender, e) => controller.Initiate();

this.Size = new Size(790,500);
this.Text = "Tyflopodcast";
this.AutoScroll=true;

lb_categories = new Label();
lb_categories.Size = new Size(140, 17);
lb_categories.Location = new Point(18,40);
lb_categories.Text = "Kategorie";
this.Controls.Add(lb_categories);
lst_categories = new ListBox();
lst_categories.Size = new Size(149, 380);
lst_categories.Location = new Point(20,60);
this.Controls.Add(lst_categories);
lst_categories.SelectedIndexChanged += (sender, e) => {
UpdatePodcasts();
UpdateLike();
};

lb_podcasts = new Label();
lb_podcasts.Size = new Size(250, 17);
lb_podcasts.Location = new Point(164, 40);
lb_podcasts.Text = "Podcasty";
this.Controls.Add(lb_podcasts);
lst_podcasts = new ListBox();
lst_podcasts.Size = new Size(400, 380);
lst_podcasts.Location = new Point(165, 60);
this.Controls.Add(lst_podcasts);
lst_podcasts.SelectedIndexChanged += (sender, e) => UpdateLike();
lst_podcasts.DoubleClick += (sender, e) => {
if(lst_podcasts.SelectedIndex>=0 && lst_podcasts.SelectedIndex<currentPodcasts.Count)
controller.PodcastSelected(currentPodcasts[lst_podcasts.SelectedIndex]);
};
lst_podcasts.KeyDown += (sender, e) => {
if (e.KeyCode == Keys.Enter) {
if(lst_podcasts.SelectedIndex>=0 && lst_podcasts.SelectedIndex<currentPodcasts.Count)
controller.PodcastSelected(currentPodcasts[lst_podcasts.SelectedIndex]);
}
};
lst_podcasts.SelectedIndexChanged += (sender, e) => UpdateDescription();

lb_description = new Label();
lb_description.Size = new Size(190, 17);
lb_description.Location = new Point(564, 40);
lb_description.Text = "Opis";
this.Controls.Add(lb_description);
edt_description = new TextBox();
edt_description.Size = new Size(190, 379);
edt_description.Location = new Point(564, 60);
edt_description.ReadOnly = true;
edt_description.Multiline = true;
this.Controls.Add(edt_description);

this.MainMenuStrip = new MenuStrip();
this.MainMenuStrip.Parent = this;
ToolStripMenuItem item_podcast = new ToolStripMenuItem("&Podcast");
this.MainMenuStrip.Items.Add(item_podcast);
item_podcast.DropDownItems.Add(new ToolStripMenuItem("&Otwórz", null, (sender, e) => {
if(lst_podcasts.SelectedIndex>=0 && lst_podcasts.SelectedIndex<currentPodcasts.Count)
controller.PodcastSelected(currentPodcasts[lst_podcasts.SelectedIndex]);
}));
item_podcast.DropDownItems.Add(new ToolStripMenuItem("&Pobierz", null, (sender, e) => {
if(lst_podcasts.SelectedIndex>=0 && lst_podcasts.SelectedIndex<currentPodcasts.Count)
controller.DownloadPodcast(currentPodcasts[lst_podcasts.SelectedIndex]);
}));
item_podcast.DropDownItems.Add(new ToolStripMenuItem("Pokaż &komentarze", null, (sender, e) => {
if(lst_podcasts.SelectedIndex>=0 && lst_podcasts.SelectedIndex<currentPodcasts.Count)
controller.ShowComments(currentPodcasts[lst_podcasts.SelectedIndex]);
}, Keys.Control|Keys.K));
item_likepodcast = new ToolStripMenuItem("Po&lub", null, (sender, e) => {
if(lst_podcasts.SelectedIndex>=0 && lst_podcasts.SelectedIndex<currentPodcasts.Count)
controller.SetLikedPodcast(currentPodcasts[lst_podcasts.SelectedIndex], !likes.Contains(currentPodcasts[lst_podcasts.SelectedIndex].id));
UpdateLike();
}, Keys.Control|Keys.L);
item_podcast.DropDownItems.Add(item_likepodcast);
item_podcast.DropDownItems.Add(new ToolStripMenuItem("Pok&aż pobrane", null, (sender, e) => controller.ShowDownloads()));
ToolStripMenuItem item_tyflopodcast = new ToolStripMenuItem("&tyflopodcast.net");
this.MainMenuStrip.Items.Add(item_tyflopodcast);
item_tyflopodcast.DropDownItems.Add(new ToolStripMenuItem("Tyflo&radio", null, (sender, e) => controller.RadioSelected(), Keys.Control|Keys.D));
item_tyflopodcast.DropDownItems.Add(new ToolStripMenuItem("Pokaż r&amówke Tyfloradia", null, (sender, e) => controller.ShowRadioProgram(), Keys.Control|Keys.M));
item_tyflopodcast.DropDownItems.Add(new ToolStripMenuItem("&Szukaj", null, (sender, e) => controller.SearchPodcasts(podcasts.ToArray()), Keys.Control|Keys.F));
item_tyflopodcast.DropDownItems.Add(new ToolStripMenuItem("&Odbuduj bazę podcastów", null, (sender, e) => controller.UpdateDatabase(true)));
ToolStripMenuItem item_help = new ToolStripMenuItem("P&omoc");
this.MainMenuStrip.Items.Add(item_help);
item_help.DropDownItems.Add(new ToolStripMenuItem("Strona Internetowa &tyflopodcast.net", null, (sender, e) => controller.ShowURL("http://tyflopodcast.net")));
item_help.DropDownItems.Add(new ToolStripMenuItem("Strona Internetowa tej &aplikacji", null, (sender, e) => controller.ShowURL("https://github.com/dawidpieper/tyflopodcast")));
item_help.DropDownItems.Add(new ToolStripMenuItem("&O programie", null, (sender, e) => AppAbout()));
item_help.DropDownItems.Add(new ToolStripMenuItem("Sprawdź dostępność a&ktualizacji", null, (sender, e) => {controller.CheckForUpdates(true);}));

var ctx_podcasts = new ContextMenuStrip();
foreach(ToolStripMenuItem mi in item_podcast.DropDownItems) {
ToolStripMenuItem cl = new ToolStripMenuItem(mi.Text, null, (sender, e) => {mi.PerformClick();});
ctx_podcasts.Items.Add(cl);
if(mi==item_likepodcast) item_ctxlikepodcast=cl;
}
lst_podcasts.ContextMenuStrip = ctx_podcasts;

}

public void AddPodcast(Podcast podcast) {
podcasts.Add(podcast);
}

public void AddCategory(Category category) {
categories.Add(category);
lst_categories.Items.Add(category.name);
}

public void UpdatePodcasts() {
lst_categories.Update();
if(updateWorker!=null && !updateWorker.IsCompleted) {
updateWorkerCTS.Cancel();
try {
updateWorker.Wait();
} catch{}
updateWorker=null;
}
updateWorkerCTS = new CancellationTokenSource();
updateWorkerCT = updateWorkerCTS.Token;
updateWorker = Task.Factory.StartNew(() => {
lst_podcasts.Items.Clear();
currentPodcasts.Clear();
int c=-2;
Podcast[] source;
if(lst_categories.SelectedIndex <= categories.Count()+1) {
if(lst_categories.SelectedIndex>1) c = categories[lst_categories.SelectedIndex-2].id;
else c = lst_categories.SelectedIndex-2;
source = podcasts.ToArray();
} else source=collections[lst_categories.SelectedIndex-categories.Count()-2].podcasts;
foreach(Podcast p in source) {
if(c==-2 || (c==-1 && likes.Contains(p.id)) || (c>=0 && p.categories.Contains(c))) {
currentPodcasts.Add(p);
lst_podcasts.Items.Add(p.name+" ("+p.time.ToString()+")");
}
if (updateWorkerCT.IsCancellationRequested) break;
}
if (updateWorkerCT.IsCancellationRequested) return;
lst_podcasts.Update();
UpdateDescription();
updateWorker=null;
}, updateWorkerCT);
}

public void AddCustomCollection(string name, Podcast[] podcasts, bool jump=false) {
var cc = new CustomCollection();
cc.name=name;
cc.podcasts=podcasts;
collections.Add(cc);
lst_categories.Items.Add(cc.name);
if(jump) {
lst_categories.SelectedIndex = categories.Count()+collections.Count()+1;
UpdatePodcasts();
}
}

public void UpdateDescription() {
if(lst_podcasts.SelectedIndex<0 || lst_podcasts.SelectedIndex>=currentPodcasts.Count()) {
edt_description.Text="";
return;
}
Podcast p = currentPodcasts[lst_podcasts.SelectedIndex];
if(p.description!=null)
edt_description.Text = p.description.Replace("\n", "\r\n");
}

public void Clear() {
categories.Clear();
podcasts.Clear();
currentPodcasts.Clear();
lst_categories.Items.Clear();
lst_podcasts.Items.Clear();
lst_categories.Items.Add("Wszystkie podcasty");
lst_categories.Items.Add("Polubione podcasty");
lst_categories.SelectedIndex=0;
}

private void AppAbout() {
string title = "Tyflopodcast wersja "+Program.version;
string message = @"
Klient portalu tyflopodcast.net.
Copyright (©) 2020, 2021 Dawid Pieper

Niniejszy program jest wolnym oprogramowaniem.
Dozwala się jego dalsze rozprowadzanie lub modyfikację  na warunkach licencji GNU General Public License V3, wydanej przez Free Software Foundation.
Kod źródłowy aplikacji znajduje się na jej stronie w serwisie Github.
";
MessageBox.Show(this, message, title);
}

public void SetLikedPodcasts(Podcast[] podcasts) {
likes.Clear();
foreach(Podcast p in podcasts) likes.Add(p.id);
}

private void UpdateLike() {
if(lst_podcasts.SelectedIndex>=0 && lst_podcasts.SelectedIndex<currentPodcasts.Count)
item_likepodcast.Checked = likes.Contains(currentPodcasts[lst_podcasts.SelectedIndex].id);
else item_likepodcast.Checked=false;
item_ctxlikepodcast.Checked = item_likepodcast.Checked;
}
}
}