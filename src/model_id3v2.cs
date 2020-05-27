/*
tyflopodcast.net client
Copyright Dawid Pieper
License: General Public License (GPLv3)
*/

using System;
using System.Text;
using System.Linq;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using Un4seen.Bass;

namespace Tyflopodcast {

public class AudioInfo {
private struct ID3Frame {
public string id;
public int size;
public bool encrypted;
public bool compressed;
public bool grouped;
public int group;
public int numValue;
public string strValue;
public ID3Frame[] subframes;
}

private List<ID3Frame> ReadId3(int stream) {
IntPtr q = Bass.BASS_ChannelGetTags(stream, BASSTag.BASS_TAG_ID3V2);
byte[] header = new byte[10];
Marshal.Copy(q, header, 0, 10);
if(header[3]!=3) return null;
bool unsync=(header[5]&128)>0;
bool extheader=(header[5]&64)>0;
int size = header[9]+header[8]*128+header[7]*16384+header[6]*2097152;
q+=10;
if(extheader) {
byte[] ehsize = new byte[4];
Marshal.Copy(q, ehsize, 0, 4);
q+=4+ehsize[3]+ehsize[2]*256+ehsize[1]*65536*ehsize[0]*16777216;
}
var fr = GetFrames(q, size);
return fr;
}

private List<ID3Frame> GetFrames(IntPtr q, int size) {
var r = new List<ID3Frame>();
int final = (int)q+size;
byte[] header = new byte[10];
while((int)q<final) {
Marshal.Copy(q, header, 0, 10);
if(header[0]==0) {
q+=1;
continue;
}
ID3Frame f = new ID3Frame();
f.id = System.Text.Encoding.ASCII.GetString(header, 0, 4);
f.size = header[7]+header[6]*256+header[5]*65536+header[4]*16777216;
f.compressed = (header[9]&128)>0;
f.encrypted = (header[9]&64)>0;
f.grouped = (header[9]&32)>0;
q+=10;
int left=f.size;
if(f.grouped) {
byte[] t = new byte[1];
Marshal.Copy(q, t, 0, 1);
f.group=t[0];
q+=1;
left-=1;
}
if(!f.compressed && !f.encrypted) {
if(f.id=="CHAP") {
for(;;) {
byte[] t = new byte[1];
Marshal.Copy(q, t, 0, 1);
q+=1;
left-=1;
if(t[0]==0) break;
}
byte[] timings = new byte[16];
Marshal.Copy(q, timings, 0, 16);
q+=16;
left-=16;
if(timings[0]!=0xff || timings[1]!=0xff || timings[2]!=0xff || timings[3]!=0xff)
f.numValue = timings[3]+timings[2]*256+timings[1]*65536+timings[0]*16777216;
else
f.numValue = timings[7]+timings[6]*256+timings[5]*65536+timings[4]*16777216;
f.subframes = GetFrames(q, left).ToArray();
}
else if(f.id[0]=='T' && f.id[1]!='X') {
byte[] t = new byte[1];
Marshal.Copy(q, t, 0, 1);
q+=1;
left-=1;
bool unicoded = (t[0]==1);
bool feff=true;
if(unicoded) {
byte[] u = new byte[2];
Marshal.Copy(q, u, 0, 2);
if(u[0]>=0xfe && u[1]>=0xfe) {
q+=2;
left-=2;
if(u[1]==0xff) feff=false;
}
}
byte[] content = new byte[left];
Marshal.Copy(q, content, 0, left);
if(!unicoded)
f.strValue = System.Text.Encoding.GetEncoding("ISO-8859-1").GetString(content);
else {
if(feff)
f.strValue = System.Text.Encoding.GetEncoding("UTF-16").GetString(content);
else
f.strValue = System.Text.Encoding.GetEncoding("unicodeFFFE").GetString(content);
}
q+=left;
left=0;
}
}
q+=left;
r.Add(f);
}
return r;
}

public struct Chapter {
public string name;
public double time;
}

private List<ID3Frame> frames=null;

public AudioInfo(int stream) {
frames = ReadId3(stream);
}

private string GetFrameString(string id) {
string r="";
foreach(ID3Frame f in frames) {
if(f.id==id && f.strValue!=null) {
r=f.strValue;
break;
}
}
return r;
}

public string title {get {return GetFrameString("TIT2");}}
public string artist {get {return GetFrameString("TPE1");}}

public Chapter[] chapters {get {
List<Chapter> chapters = new List<Chapter>();
foreach(ID3Frame f in frames) {
if(f.id=="CHAP" && f.subframes!=null) {
Chapter c = new Chapter();
c.time=(double)f.numValue/1000.0;
c.name="";
foreach(ID3Frame g in f.subframes)
if(g.id=="TIT2") c.name=g.strValue;
chapters.Add(c);
}
}
return chapters.ToArray();
}}
}
}