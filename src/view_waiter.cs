/*
tyflopodcast.net client
Copyright Dawid Pieper
License: General Public License (GPLv3)
*/

using System;
using System.Text;
using System.Windows.Forms;
using System.Drawing;

namespace Tyflopodcast {
public class LoadingWindow : Form {
private Label lb_status;
private ProgressBar pb_percentage;
private Button btn_cancel;
public LoadingWindow(string text="Ładowanie...") {
this.FormBorderStyle = FormBorderStyle.FixedToolWindow;

this.Size = new Size(240,320);
this.StartPosition = FormStartPosition.CenterScreen;
this.Text = text;

lb_status = new Label();
lb_status.Size = new Size(300, 90);
lb_status.Location = new Point(10, 10);
this.Controls.Add(lb_status);

pb_percentage = new ProgressBar();
pb_percentage.Size = new Size(50, 90);
pb_percentage.Location = new Point(10, 115);
pb_percentage.Minimum=0;
pb_percentage.Step=1;
pb_percentage.Maximum=100;
this.Controls.Add(pb_percentage);

btn_cancel = new Button();
btn_cancel.Text = "Anuluj";
btn_cancel.Size = new Size(200, 90);
btn_cancel.Location = new Point(10, 220);
btn_cancel.Click += (Object, e) => {this.Close();};
this.Controls.Add(btn_cancel);
this.CancelButton = btn_cancel;
}

public void SetStatus(String t) {
lb_status.Text=t;
lb_status.Update();
}

public void SetPercentage(int p) {
pb_percentage.Value=p;
pb_percentage.Update();
}
}
}