/*
tyflopodcast.net client
Copyright Dawid Pieper
License: General Public License (GPLv3)
*/

using System;
using System.Windows.Forms;
using System.Drawing;
using System.Text;
using System.Collections.Generic;

namespace Tyflopodcast {
public class ContactRadioWindow : Form {
private Label lb_name, lb_message;
private TextBox edt_name, edt_message;
private Button btn_send, btn_cancel;
private Controller controller;

public ContactRadioWindow(Controller tcontroller, string title) {
controller=tcontroller;

this.FormBorderStyle = FormBorderStyle.FixedToolWindow;

this.Size = new Size(320, 240);
this.StartPosition = FormStartPosition.CenterScreen;
this.Text = title+" - Napisz do Tyfloradia";

lb_name = new Label();
lb_name.Text = "Podpis";
lb_name.Size = new Size(40,50);
lb_name.Location = new Point(20, 20);
this.Controls.Add(lb_name);

edt_name = new TextBox();
edt_name.Size = new Size(240, 50);
edt_name.Location = new Point(60, 20);
this.Controls.Add(edt_name);

lb_message = new Label();
lb_message.Text = "Wiadomość";
lb_message.Size = new Size(40,100);
lb_message.Location = new Point(20, 80);
this.Controls.Add(lb_message);

edt_message = new TextBox();
edt_message.Size = new Size(240, 100);
edt_message.Location = new Point(60, 80);
edt_message.Multiline = true;
this.Controls.Add(edt_message);

btn_send = new Button();
btn_send.Text = "Wyślij";
btn_send.Size = new Size(100, 30);
btn_send.Location = new Point(20, 190);
btn_send.Click += (sender,e) => Send();
this.Controls.Add(btn_send);

btn_cancel = new Button();
btn_cancel.Text = "Anuluj";
btn_cancel.Size = new Size(100, 30);
btn_cancel.Location = new Point(150, 190);
this.Controls.Add(btn_cancel);

this.CancelButton = btn_cancel;
//this.AcceptButton=btn_send;
}

public void Send() {
string name = edt_name.Text;
string message = edt_message.Text;
if(name=="" || message=="") return;
controller.SendRadioContact(name, message);
}
}
}