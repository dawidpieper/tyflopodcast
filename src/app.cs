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
using Un4seen.Bass.AddOn.Fx;

namespace Tyflopodcast {

public class Program {
public static string version = "1.0";

private static TPWindow wnd;

public static void PrepareLibraries() {
bool suc=false;
if(IntPtr.Size == 8) {
suc=Bass.LoadMe(Application.StartupPath+@"\lib64");
if(suc) suc=BassFx.LoadMe(Application.StartupPath+@"\lib64");
} else {
suc=Bass.LoadMe(Application.StartupPath+@"\lib32");
if(suc) suc=BassFx.LoadMe(Application.StartupPath+@"\lib32");
}
if(!suc) {
MessageBox.Show("Możliwe, że biblioteka nie znajduje się już w poprzedniej lokalizacji. Jeśli program był przenoszony, należy się upewnić czy wraz z nim przeniesiono pozostałe foldery aplikacji. W razie problemów zaleca się ponowne pobranie programu.", "Nie udało się załadować biblioteki Bass.", 0, MessageBoxIcon.Error);
Environment.Exit(1);
}
}

[STAThread]
public static void Main(string[] args) {
Application.EnableVisualStyles();
PrepareLibraries();
wnd = new TPWindow(new Controller(args));
Application.Run(wnd);
}
}
}