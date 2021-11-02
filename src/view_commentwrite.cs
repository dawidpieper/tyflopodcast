/*
A part of Tyflopodcast - tyflopodcast.net client.
Copyright (C) 2020, 2021 Dawid Pieper
This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, version 3. 
This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details. 
You should have received a copy of the GNU General Public License along with this program. If not, see <https://www.gnu.org/licenses/>. 
*/

using System;
using System.Windows.Forms;
using System.Drawing;
using System.Text;
using System.Collections.Generic;

namespace Tyflopodcast {
public class CommentWriteWindow : Form {
private Label lb_name, lb_mail, lb_url, lb_message;
private TextBox edt_name, edt_mail, edt_url, edt_message;
private Button btn_send, btn_cancel;
private Controller controller;
private Podcast podcast;
private string action;
private Dictionary<string,string> fields;

public CommentWriteWindow(Controller tcontroller, Podcast tpodcast, string taction, Dictionary<string,string> tfields) {
controller=tcontroller;
podcast=tpodcast;
action=taction;
fields=tfields;

this.FormBorderStyle = FormBorderStyle.FixedDialog ;
this.ShowInTaskbar=false;

this.Size = new Size(655, 500);
this.StartPosition = FormStartPosition.CenterScreen;
this.Text = "Skomentuj podcast "+podcast.name;

lb_name = new Label();
lb_name.Text = "Podpis";
lb_name.Size = new Size(50,30);
lb_name.Location = new Point(20, 20);
this.Controls.Add(lb_name);

edt_name = new TextBox();
edt_name.Size = new Size(160, 50);
edt_name.Location = new Point(140, 20);
this.Controls.Add(edt_name);

lb_mail = new Label();
lb_mail.Text = "Adres E-mail (nie zostanie opublikowany)";
lb_mail.Size = new Size(90,50);
lb_mail.Location = new Point(20, 70);
this.Controls.Add(lb_mail);

edt_mail = new TextBox();
edt_mail.Size = new Size(480, 50);
edt_mail.Location = new Point(140, 70);
this.Controls.Add(edt_mail);

lb_url = new Label();
lb_url.Text = "Witryna Internetowa";
lb_url.Size = new Size(100,50);
lb_url.Location = new Point(20, 137);
this.Controls.Add(lb_url);

edt_url = new TextBox();
edt_url.Size = new Size(480, 50);
edt_url.Location = new Point(140, 140);
this.Controls.Add(edt_url);

lb_message = new Label();
lb_message.Text = "Komentarz";
lb_message.Size = new Size(100,50);
lb_message.Location = new Point(20, 210);
this.Controls.Add(lb_message);

edt_message = new TextBox();
edt_message.Size = new Size(480, 160);
edt_message.Location = new Point(140, 210);
edt_message.Multiline = true;
this.Controls.Add(edt_message);

btn_send = new Button();
btn_send.Text = "Wyślij";
btn_send.Size = new Size(300, 50);
btn_send.Location = new Point(20, 390);
btn_send.Click += (sender,e) => Send();
this.Controls.Add(btn_send);

btn_cancel = new Button();
btn_cancel.Text = "Anuluj";
btn_cancel.Size = new Size(300, 50);
btn_cancel.Location = new Point(320, 390);
this.Controls.Add(btn_cancel);

this.CancelButton = btn_cancel;
//this.AcceptButton=btn_send;
}

public void Send() {
string name = edt_name.Text;
string url = edt_url.Text;
string mail = edt_mail.Text;
string message = edt_message.Text;
if(name=="" || mail=="" || message=="") return;
controller.PublishComment(podcast, action, fields, name, mail, url, message);
}
}
}