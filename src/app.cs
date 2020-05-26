/*
tyflopodcast.net client
Copyright Dawid Pieper
License: General Public License (GPLv3)
*/

using System;
using System.Globalization;
using System.Text;
using System.Diagnostics;
using System.Windows.Forms;
using System.Threading;
using System.Threading.Tasks;
using Un4seen.Bass;

namespace Tyflopodcast {

public class Program {

private static TPWindow wnd;

public static void loadApp() {
bool cancelled = true;
var l = new LoadingWindow("Pobieranie bazy podcastów");
l.SetStatus("Inicjowanie...");
Podcast[] podcasts=null;
bool localLoaded = Podcasts.GetLocalPodcasts(out podcasts);
bool downloadRemote=true;
if(localLoaded)
if(MessageBox.Show("Czy chcesz zaktualizować listę dostępnych podcastów?", "Tyflopodcast", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.No)
downloadRemote=false;
if(downloadRemote) {
int totalPages=-1, leftPages=-1;
Task.Factory.StartNew(()=> {
podcasts = Podcasts.FetchPodcasts(ref leftPages, ref totalPages);
l.SetStatus("Czyszczenie...");
Podcasts.CleanUp();
cancelled = false;
l.Close();
});
Task.Factory.StartNew(()=> {
l.SetStatus("Łączenie...");
bool s=false;
int p=-1;
for(;;) {
if(!s && totalPages!=-1) {
s=true;
l.SetStatus("Pobieranie informacji o bazie podcastów...");
}
else if(s && p!=leftPages) {
l.SetStatus("Pobieranie strony "+(totalPages-leftPages).ToString()+" z "+totalPages.ToString()+"...");
p=leftPages;
int pr = (int)((double)(totalPages-leftPages)/totalPages*100.0);
l.SetPercentage(pr);
if(leftPages==0) break;
}
}
});
Application.Run(l);
if(cancelled || podcasts==null) Environment.Exit(0);
}
foreach(Category c in Podcasts.categories) wnd.AddCategory(c);
foreach(Podcast p in podcasts) wnd.AddPodcast(p);
wnd.UpdatePodcasts();
}

public static void PrepareLibraries() {
bool suc=false;
if(IntPtr.Size == 8) {
suc=Bass.LoadMe(Application.StartupPath+@"\lib64");
} else {
suc=Bass.LoadMe(Application.StartupPath+@"\lib32");
}
if(!suc) {
MessageBox.Show("Możliwe, że biblioteka nie znajduje się już w poprzedniej lokalizacji. Jeśli program był przenoszony, należy się upewnić czy wraz z nim przeniesiono pozostałe foldery aplikacji. W razie problemów zaleca się ponowne pobranie programu.", "Nie udało się załadować biblioteki Bass.", 0, MessageBoxIcon.Error);
Environment.Exit(1);
}
}

[STAThread]
public static void Main() {
Application.EnableVisualStyles();
PrepareLibraries();
wnd = new TPWindow(new Controller());
loadApp();
Application.Run(wnd);
}
}
}