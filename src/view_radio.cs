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

namespace Tyflopodcast {
public class RadioWindow : Form {
private Controller controller;

private Label lb_volume;
private TyfloTrackBar tb_volume;
private Button btn_play, btn_contact, btn_close;

private string name=null;

public RadioWindow(Controller tcontroller) {
controller=tcontroller;

this.FormBorderStyle = FormBorderStyle.FixedDialog ;
this.ShowInTaskbar=false;

this.Size = new Size(396, 205);
this.StartPosition = FormStartPosition.CenterScreen;
UpdateWindowCaption();

lb_volume = new Label();
lb_volume.Text = "Głośność";
lb_volume.Size = new Size(80, 50);
lb_volume.Location = new Point(20, 20);
this.Controls.Add(lb_volume);

tb_volume = new TyfloTrackBar();
tb_volume.Size = new Size(250, 50);
tb_volume.Location = new Point(100, 20);
tb_volume.Minimum=0;
tb_volume.Maximum=100;
tb_volume.TickFrequency = 5;
tb_volume.LargeChange=20;
tb_volume.SmallChange=5;
tb_volume.Scroll += (sender, e) => {
controller.SetVolume(tb_volume.Value);
};
this.Controls.Add(tb_volume);

tb_volume.KeyDown += TBKeyDown;

btn_play = new Button();
btn_play.Text = "P&lay/Pauza";
btn_play.Size = new Size(100, 40);
btn_play.Location = new Point(20, 70);
btn_play.Click += (sender, e) => controller.TogglePlayback();
this.Controls.Add(btn_play);

btn_contact = new Button();
btn_contact.Text = "&Napisz lub zadzwoń do Tyfloradia";
btn_contact.Size = new Size(100, 80);
btn_contact.Location = new Point(140, 70);
btn_contact.Click += (sender, e) => controller.ContactRadio();
this.Controls.Add(btn_contact);

btn_close = new Button();
btn_close.Text = "Zamknij";
btn_close.Size = new Size(100, 40);
btn_close.Location = new Point(260, 70);
btn_close.Click += (sender, e) => this.Close();
this.Controls.Add(btn_close);

this.CancelButton = btn_close;;
}

public void TBKeyDown(Object sender, KeyEventArgs e) {
if (e.KeyCode == Keys.Space)
controller.TogglePlayback();
}

public void SetVolume(int vol) {
tb_volume.Value = vol;
}

public void SetName(string tname) {
this.name=tname;
UpdateWindowCaption();
}

public void UpdateWindowCaption() {
StringBuilder sb = new StringBuilder();
if(this.name!=null && this.name!="") {
sb.Append(this.name);
sb.Append(" - ");
}
sb.Append("Tyfloradio - Tyflopodcast");
this.Text=sb.ToString();
}

public void ShowContactRadioContext(bool callAvailable) {
ContextMenuStrip cm = new ContextMenuStrip();
cm.Items.Add("&Napisz", null, (sender, e) => {controller.ContactRadioText();});
if(callAvailable) {
var item_call = new ToolStripMenuItem("&Zadzwoń");
cm.Items.Add(item_call);
item_call.DropDownItems.Add("Przez &aplikację Zoom", null, (sender, e) => {
controller.ContactRadioZoom();
});
item_call.DropDownItems.Add("Przez przeglądarkę &Internetową", null, (sender, e) => {
controller.ContactRadioBrowser();
});
item_call.DropDownItems.Add("Przez &telefon", null, (sender, e) => {
controller.ContactRadioPhone();
});
}
cm.Show(this, new Point(0,0));
}

}
}