/*
tyflopodcast.net client
Copyright Dawid Pieper
License: General Public License (GPLv3)
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

private Task updateWorker = null;
private CancellationTokenSource updateWorkerCTS = null;
private CancellationToken updateWorkerCT;

private Controller controller;

public TPWindow(Controller tcontroller) {
controller=tcontroller;
controller.SetWindow(this);
categories = new List<Category>();
podcasts = new List<Podcast>();
currentPodcasts = new List<Podcast>();
collections = new List<CustomCollection>();

this.Shown += (sender, e) => controller.Initiate();

this.Size = new Size(640,480);
this.Text = "Tyflopodcast";

lb_categories = new Label();
lb_categories.Size = new Size(100, 50);
lb_categories.Location = new Point(20,20);
lb_categories.Text = "Kategorie";
this.Controls.Add(lb_categories);
lst_categories = new ListBox();
lst_categories.Size = new Size(100, 380);
lst_categories.Location = new Point(20,80);
this.Controls.Add(lst_categories);
lst_categories.SelectedIndexChanged += (sender, e) => UpdatePodcasts();

lb_podcasts = new Label();
lb_podcasts.Size = new Size(250, 50);
lb_podcasts.Location = new Point(160, 20);
lb_podcasts.Text = "Podcasty";
this.Controls.Add(lb_podcasts);
lst_podcasts = new ListBox();
lst_podcasts.Size = new Size(250, 380);
lst_podcasts.Location = new Point(160, 80);
this.Controls.Add(lst_podcasts);
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
lb_description.Size = new Size(190, 50);
lb_description.Location = new Point(530, 20);
lb_description.Text = "Opis";
this.Controls.Add(lb_description);
edt_description = new TextBox();
edt_description.Size = new Size(190, 380);
edt_description.Location = new Point(530, 80);
edt_description.ReadOnly = true;
edt_description.Multiline = true;
this.Controls.Add(edt_description);

this.Menu = new MainMenu();
MenuItem item_podcast = new MenuItem("&Podcast");
this.Menu.MenuItems.Add(item_podcast);
item_podcast.MenuItems.Add("&Otwórz", (sender, e) => {
if(lst_podcasts.SelectedIndex>=0 && lst_podcasts.SelectedIndex<currentPodcasts.Count)
controller.PodcastSelected(currentPodcasts[lst_podcasts.SelectedIndex]);
});
item_podcast.MenuItems.Add("&Pobierz", (sender, e) => {
if(lst_podcasts.SelectedIndex>=0 && lst_podcasts.SelectedIndex<currentPodcasts.Count)
controller.DownloadPodcast(currentPodcasts[lst_podcasts.SelectedIndex]);
});
item_podcast.MenuItems.Add("Pokaż &komentarze", (sender, e) => {
if(lst_podcasts.SelectedIndex>=0 && lst_podcasts.SelectedIndex<currentPodcasts.Count)
controller.ShowComments(currentPodcasts[lst_podcasts.SelectedIndex]);
});
item_podcast.MenuItems.Add("Pok&aż pobrane", (sender, e) => controller.ShowDownloads());
MenuItem item_tyflopodcast = new MenuItem("&tyflopodcast.net");
this.Menu.MenuItems.Add(item_tyflopodcast);
item_tyflopodcast.MenuItems.Add("Tyflo&radio", (sender, e) => controller.RadioSelected());
item_tyflopodcast.MenuItems.Add("Pokaż r&amówke Tyfloradia", (sender, e) => controller.ShowRadioProgram());
item_tyflopodcast.MenuItems.Add("&Szukaj", (sender, e) => controller.SearchPodcasts(podcasts.ToArray()));
item_tyflopodcast.MenuItems.Add("&Odbuduj bazę podcastów", (sender, e) => controller.UpdateDatabase(true));
MenuItem item_help = new MenuItem("P&omoc");
this.Menu.MenuItems.Add(item_help);
item_help.MenuItems.Add("Strona Internetowa &tyflopodcast.net", (sender, e) => controller.ShowURL("http://tyflopodcast.net"));
item_help.MenuItems.Add("Strona Internetowa tej &aplikacji", (sender, e) => controller.ShowURL("https://github.com/dawidpieper/tyflopodcast"));
item_help.MenuItems.Add("&O programie", (sender, e) => AppAbout());
item_help.MenuItems.Add("Sprawdź dostępność a&ktualizacji", (sender, e) => {controller.CheckForUpdates(true);});

ContextMenu ctx_podcasts = new ContextMenu();
foreach(MenuItem mi in item_podcast.MenuItems)
ctx_podcasts.MenuItems.Add(mi.CloneMenu());
lst_podcasts.ContextMenu = ctx_podcasts;
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
int c=-1;
Podcast[] source;
if(lst_categories.SelectedIndex <= categories.Count()) {
if(lst_categories.SelectedIndex>0) c = categories[lst_categories.SelectedIndex-1].id;
source = podcasts.ToArray();
} else source=collections[lst_categories.SelectedIndex-categories.Count()-1].podcasts;
foreach(Podcast p in source) {
if(c==-1 || p.categories.Contains(c)) {
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
lst_categories.SelectedIndex = categories.Count()+collections.Count();
UpdatePodcasts();
}
}

public void UpdateDescription() {
if(lst_podcasts.SelectedIndex<0 || lst_podcasts.SelectedIndex>=currentPodcasts.Count()) {
edt_description.Text="";
return;
}
Podcast p = currentPodcasts[lst_podcasts.SelectedIndex];
edt_description.Text = p.description.Replace("\n", "\r\n");
}

public void Clear() {
categories.Clear();
podcasts.Clear();
currentPodcasts.Clear();
lst_categories.Items.Clear();
lst_podcasts.Items.Clear();
lst_categories.Items.Add("Wszystkie podcasty");
lst_categories.SelectedIndex=0;
}

private void AppAbout() {
string title = "Tyflopodcast wersja "+Program.version;
string message = @"
Klient portalu tyflopodcast.net.
Copyright (©) 2020 Dawid Pieper

Niniejszy program jest wolnym oprogramowaniem.
Dozwala się jego dalsze rozprowadzanie lub modyfikację  na warunkach licencji GNU General Public License V3, wydanej przez Free Software Foundation.
Kod źródłowy aplikacji znajduje się na jej stronie w serwisie Github.
";
MessageBox.Show(this, message, title);
}
}
}