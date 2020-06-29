/*
tyflopodcast.net client
Copyright Dawid Pieper
License: General Public License (GPLv3)
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

this.FormBorderStyle = FormBorderStyle.FixedToolWindow;

this.Size = new Size(320, 240);
this.StartPosition = FormStartPosition.CenterScreen;
this.Text = "Aktualna ramówka Tyfloradia - Tyflopodcast";

lb_program = new Label();
lb_program.Text = "Ramówka";
lb_program.Size = new Size(200, 50);
lb_program.Location = new Point(60, 20);
this.Controls.Add(lb_program);

edt_program = new TextBox();
edt_program.Size = new Size(280, 100);
edt_program.Location = new Point(20, 70);
edt_program.ReadOnly = true;
edt_program.Multiline = true;
edt_program.Text=program.Replace("\n", "\r\n");
this.Controls.Add(edt_program);

btn_close = new Button();
btn_close.Text = "Zamknij";
btn_close.Size = new Size(120, 40);
btn_close.Location = new Point(100, 180);
btn_close.Click += (sender, e) => this.Close();
this.Controls.Add(btn_close);

this.CancelButton = btn_close;

}
}
}