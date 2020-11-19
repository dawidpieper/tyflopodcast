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
public class ContactRadioPhoneWindow : Form {
private Controller controller;

private Label lb_phone, lb_meeting;
private TextBox edt_phone, edt_meeting;
private Button btn_close;

public ContactRadioPhoneWindow(Controller tcontroller, string meeting) {
controller=tcontroller;

this.FormBorderStyle = FormBorderStyle.FixedToolWindow;

this.Size = new Size(320, 240);
this.StartPosition = FormStartPosition.CenterScreen;
this.Text = "Kontakt telefoniczny z Tyfloradiem - Tyflopodcast";

lb_phone = new Label();
lb_phone.Text = "Zadzwoń pod ten numer";
lb_phone.Size = new Size(100, 100);
lb_phone.Location = new Point(20, 20);
this.Controls.Add(lb_phone);

edt_phone = new TextBox();
edt_phone.Size = new Size(160, 100);
edt_phone.Location = new Point(140, 20);
edt_phone.ReadOnly = true;
edt_phone.Text="+48 22 398 73 56";
this.Controls.Add(edt_phone);

lb_meeting = new Label();
lb_meeting.Text = "I wprowadź ten numer pokoju, potwierdzając wybór krzyżykiem";
lb_meeting.Size = new Size(100, 50);
lb_meeting.Location = new Point(20, 120);
this.Controls.Add(lb_meeting);

edt_meeting = new TextBox();
edt_meeting.Size = new Size(160, 100);
edt_meeting.Location = new Point(140, 120);
edt_meeting.ReadOnly = true;
edt_meeting.Text=meeting;
this.Controls.Add(edt_meeting);

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