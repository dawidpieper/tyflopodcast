/*
A part of Tyflopodcast - tyflopodcast.net client.
Copyright (C) 2020 Dawid Pieper
This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, version 3. 
This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details. 
You should have received a copy of the GNU General Public License along with this program. If not, see <https://www.gnu.org/licenses/>. 
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

this.Size = new Size(320, 240);
this.StartPosition = FormStartPosition.CenterScreen;
this.Text = text;

lb_status = new Label();
lb_status.Size = new Size(280, 80);
lb_status.Location = new Point(20, 20);
this.Controls.Add(lb_status);

pb_percentage = new ProgressBar();
pb_percentage.Size = new Size(120, 30);
pb_percentage.Location = new Point(100, 115);
pb_percentage.Minimum=0;
pb_percentage.Step=1;
pb_percentage.Maximum=100;
this.Controls.Add(pb_percentage);

btn_cancel = new Button();
btn_cancel.Text = "Anuluj";
btn_cancel.Size = new Size(100, 60);
btn_cancel.Location = new Point(120, 160);
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