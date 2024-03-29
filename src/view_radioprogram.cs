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
public class RadioProgramWindow : Form {
private Controller controller;

private Label lb_program;
private TextBox edt_program;
private Button btn_close;

public RadioProgramWindow(Controller tcontroller, string program) {
controller=tcontroller;

this.FormBorderStyle = FormBorderStyle.FixedDialog ;
this.ShowInTaskbar=false;

this.Size = new Size(635, 800);
this.StartPosition = FormStartPosition.CenterScreen;
this.Text = "Aktualna ramówka Tyfloradia - Tyflopodcast";

lb_program = new Label();
lb_program.Text = "Ramówka";
lb_program.Size = new Size(500, 20);
lb_program.Location = new Point(20, 5);
this.Controls.Add(lb_program);

edt_program = new TextBox();
edt_program.Size = new Size(580, 660);
edt_program.Location = new Point(20, 35);
edt_program.ReadOnly = true;
edt_program.Multiline = true;
edt_program.Text=program.Replace("\n", "\r\n");
this.Controls.Add(edt_program);

btn_close = new Button();
btn_close.Text = "Zamknij";
btn_close.Size = new Size(420, 40);
btn_close.Location = new Point(100, 700);
btn_close.Click += (sender, e) => this.Close();
this.Controls.Add(btn_close);

this.CancelButton = btn_close;

}
}
}