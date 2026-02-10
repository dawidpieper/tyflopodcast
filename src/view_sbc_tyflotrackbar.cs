/*
A part of Tyflopodcast - tyflopodcast.net client.
Copyright (C) 2020 Dawid Pieper
This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, version 3. 
This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details. 
You should have received a copy of the GNU General Public License along with this program. If not, see <https://www.gnu.org/licenses/>. 
*/

using System;
using System.Windows.Forms;

public class TyfloTrackBar : TrackBar {

protected override bool ProcessCmdKey(ref Message msg, Keys keyData) {
int oldValue = this.Value;
switch (keyData) {
case Keys.Up:
SmallUp();
break;
case Keys.Down:
SmallDown();
break;
case Keys.Right:
SmallUp();
break;
case Keys.Left:
SmallDown();
break;
case Keys.PageUp:
LargeUp();
break;
case Keys.PageDown:
LargeDown();
break;
default:
return base.ProcessCmdKey(ref msg, keyData);
}

if (Value != oldValue) {
OnScroll(EventArgs.Empty);
OnValueChanged(EventArgs.Empty);
}
return true;
    }
public int SmallUp() {
return this.Value=Math.Min(this.Value + this.SmallChange, this.Maximum);
}
public int SmallDown() {
return this.Value = Math.Max(this.Value - this.SmallChange, this.Minimum);
	}
	public int LargeUp () {
	return this.Value = Math.Min(this.Value + this.LargeChange, this.Maximum);
	}
	public int LargeDown() {
	return this.Value = Math.Max(this.Value - this.LargeChange, this.Minimum);
	}
	}
