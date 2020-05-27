/*
tyflopodcast.net client
Copyright Dawid Pieper
License: General Public License (GPLv3)
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
return Math.Min(this.Value + this.LargeChange, this.Maximum);
}
public int LargeDown() {
return this.Value = Math.Max(this.Value - this.LargeChange, this.Minimum);
}
}