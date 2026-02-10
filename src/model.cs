/*
A part of Tyflopodcast - tyflopodcast.net client.
Copyright (C) 2020, 2021 Dawid Pieper
This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, version 3. 
This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details. 
You should have received a copy of the GNU General Public License along with this program. If not, see <https://www.gnu.org/licenses/>. 
*/

using System;
using System.Globalization;
using System.Data;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using Newtonsoft.Json;
using HtmlAgilityPack;

namespace Tyflopodcast {

public struct Bookmark {
public int podcast;
public string name;
public float time;
}

public struct Podcast {
public int id;
public DateTime time;
public String name;
public String description;
public int[] categories;
}

public struct Category {
public int id;
public string name;
}

public struct Comment {
public int podcast;
public string author;
public DateTime time;
public string content;
}

public class Podcasts {

private static List<Podcast> localPodcasts=null;

public static List<Category> categories;

public static List<Bookmark> bookmarks;

public static List<int> likes;

private static Dictionary<string, float> resumePositions;

private static readonly object internalDataLock = new object();

public const String url = "http://tyflopodcast.net";
public const String jsonurl = "http://tyflopodcast.net/wp-json/wp/v2";
public const String contacturl = "http://kontakt.tyflopodcast.net/json.php";
public const string versionurl = "https://raw.githubusercontent.com/dawidpieper/tyflopodcast/master/version.json";

private static HttpClient apiClient;

public static void CleanUp() {
if(apiClient!=null) {
apiClient.Dispose();
apiClient=null;
}
}

public static void Init() {
ServicePointManager.DefaultConnectionLimit = 6;
ServicePointManager.Expect100Continue = true;
ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
HttpClientHandler hch = new HttpClientHandler();
hch.Proxy = null;
hch.UseProxy = false;
apiClient = new HttpClient(hch);
apiClient.BaseAddress = new Uri(url);
apiClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("Tyflopodcast", Program.version));
}

private static string HTMLToText(String source) {
Regex htregex = new Regex(@"\<[^\>]*\>");
return WebUtility.HtmlDecode(htregex.Replace(source, String.Empty));
}

private static int fetchPodcastsConverter(int page, HttpResponseMessage response, ref Podcast[] podcasts, bool reset=false) {
var json = response.Content.ReadAsStringAsync().Result;
dynamic j = JsonConvert.DeserializeObject(json);
var headers = response.Headers;
IEnumerable<string> values;
if(podcasts == null)
if(headers.TryGetValues("X-WP-Total", out values))
podcasts = new Podcast[int.Parse(values.First())];
else {
return -1;
}
int maxid=0;
if(localPodcasts!=null) maxid=localPodcasts[0].id;
int index=0;
foreach(dynamic r in j) {
if(page==1 && r.id<=maxid && !reset) {
foreach(Podcast o in localPodcasts) {
podcasts[index]=o;
++index;
}
return 0;
}
Podcast p = new Podcast();
p.name=WebUtility.HtmlDecode((String)r.title.rendered);
p.time = r.date;
p.description=HTMLToText((string)r.content.rendered);
p.id=r.id;
var cs = new List<int>();
if(categories!=null)
foreach(Category c in categories)
foreach(dynamic cid in r.categories)
if(((int)cid)==c.id) cs.Add(c.id);
p.categories=cs.ToArray();
podcasts[(page-1)*100+index]=p;
++index;
}
if(headers.TryGetValues("X-WP-TotalPages", out values))
return int.Parse(values.First());
return 0;
}

private static bool FetchPodcastsPiece(ref Podcast[] podcasts, ref int leftPages, ref int totalPages, int page=1, bool reset=false) {
if(apiClient==null) Init();
Func<int, Task<HttpResponseMessage>> getter = (page) => {
String u=jsonurl+"/posts?per_page=100&page="+page.ToString();
return apiClient.GetAsync(u);
};
HttpResponseMessage response = getter(page).Result;
int pages = fetchPodcastsConverter(page, response, ref podcasts, reset);
if(pages>0) {
totalPages=pages;
if(pages>page && page==1) {
var tasks = new Task<HttpResponseMessage>[pages-1];
for(int i=2; i<=pages; ++i) tasks[i-2] = getter(i);
int waiting=leftPages=pages-1;
while(waiting>0) {
for(int i=2; i<=pages; ++i)
if(tasks[i-2]!=null && tasks[i-2].IsCompleted) {
--leftPages;
HttpResponseMessage m = tasks[i-2].Result;
fetchPodcastsConverter(i, m, ref podcasts, reset);
tasks[i-2].Dispose();
tasks[i-2]=null;
--waiting;
}
}
}
}
return true;
}

public static Podcast[] FetchPodcasts(bool reset=false) {
int a=0,b=0;
return FetchPodcasts(ref a, ref b, reset);
}

private static List<Category> GetCategories(int page=1) {
if(apiClient==null) Init();
String u=jsonurl+"/categories?per_page=100";
if(page>1) u+="&page="+page.ToString();
var response = apiClient.GetAsync(u).Result;
var json = response.Content.ReadAsStringAsync().Result;
dynamic j = JsonConvert.DeserializeObject(json);
var headers = response.Headers;
IEnumerable<string> values;
var cs = new List<Category>();
foreach(dynamic r in j) {
Category c = new Category();
c.name=WebUtility.HtmlDecode((String)r.name);
c.id=r.id;
cs.Add(c);
}
if(headers.TryGetValues("X-WP-TotalPages", out values)) {
int pages = int.Parse(values.First());
if(pages>page)
cs=(List<Category>)cs.Concat(GetCategories(page+1));
}
return cs;
}

public static Podcast[] FetchPodcasts(ref int leftPages, ref int totalPages, bool reset=false) {
categories = GetCategories();
Podcast[] podcasts=null;
FetchPodcastsPiece(ref podcasts, ref leftPages, ref totalPages, 1, reset);
localPodcasts = new List<Podcast>(podcasts);
SaveLocalDB();
return podcasts;
}

private static readonly byte[] MagicNumber = {0xa4, 0x87, 0x42, 0x3f, 0x11, 0x37, 0x99, 0xfa};
private static readonly byte[] MagicInternalNumber = {0xa4, 0x87, 0x42, 0x3f, 0x11, 0x37, 0x99, 0xfb};

private static bool LoadInternal() {
string datadir = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)+"\\tyflopodcast";
System.IO.Directory.CreateDirectory(datadir);
try {
lock(internalDataLock) {
	likes = new List<int>();
	bookmarks = new List<Bookmark>();
	resumePositions = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);
	if(!File.Exists(datadir+"\\internal.dat")) return false;
	using (BinaryReader br = new BinaryReader(File.Open(datadir+"\\internal.dat", FileMode.Open))) {
for(int i=0; i<MagicInternalNumber.Count(); ++i)
if(br.ReadByte() != MagicInternalNumber[i]) return false;
int cnt_likes = br.ReadInt32();
for(int i=0; i<cnt_likes; ++i) likes.Add(br.ReadInt32());
int cnt_bookmarks = br.ReadInt32();
for(int i=0; i<cnt_bookmarks; ++i) {
Bookmark b;
b.podcast = br.ReadInt32();
b.name = br.ReadString();
b.time = br.ReadSingle();
bookmarks.Add(b);
}

if(br.BaseStream.Position < br.BaseStream.Length) {
try {
int cnt_resume = br.ReadInt32();
for(int i=0; i<cnt_resume; ++i) {
string key = br.ReadString();
float position = br.ReadSingle();
if(string.IsNullOrEmpty(key) || position < 0) continue;
resumePositions[key] = position;
}
	} catch(EndOfStreamException) {
	}
	}
	}
	}
	}
catch {return false;}
return true;
}

private static bool SaveInternalLocked(string datadir) {
try {
using (BinaryWriter bw = new BinaryWriter(File.Open(datadir+"\\internal.dat", FileMode.Create))) {
for(int i=0; i<MagicInternalNumber.Count(); ++i)
bw.Write(MagicInternalNumber[i]);
bw.Write(likes.Count());
foreach(int l in likes) bw.Write(l);
bw.Write(bookmarks.Count());
foreach(Bookmark b in bookmarks) {
bw.Write(b.podcast);
bw.Write(b.name);
bw.Write(b.time);
}
	if(resumePositions==null) resumePositions = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);
	bw.Write(resumePositions.Count());
	foreach(var kv in resumePositions) {
	bw.Write(kv.Key);
	bw.Write(kv.Value);
	}
	}
	}
catch {return false;}
return true;
}

private static bool SaveInternal() {
string datadir = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)+"\\tyflopodcast";
System.IO.Directory.CreateDirectory(datadir);
	lock(internalDataLock) {
	if(likes==null) likes = new List<int>();
	if(bookmarks==null) bookmarks = new List<Bookmark>();
	if(resumePositions==null) resumePositions = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);
	return SaveInternalLocked(datadir);
	}
	}

private static bool LoadLocalDB() {
LoadInternal();
string datadir = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)+"\\tyflopodcast";
System.IO.Directory.CreateDirectory(datadir);
if(!File.Exists(datadir+"\\db.dat")) return false;
try {
using (BinaryReader br = new BinaryReader(File.Open(datadir+"\\db.dat", FileMode.Open))) {
for(int i=0; i<MagicNumber.Count(); ++i)
if(br.ReadByte() != MagicNumber[i]) return false;
categories = new List<Category>();
int cnt_categories = br.ReadInt32();
for(int i=0; i<cnt_categories; ++i) {
Category c;
c.id = br.ReadInt32();
c.name = br.ReadString();
categories.Add(c);
}
localPodcasts = new List<Podcast>();
int cnt_podcasts = br.ReadInt32();
for(int i=0; i<cnt_podcasts; ++i) {
Podcast p = new Podcast();
p.id = br.ReadInt32();
p.name = br.ReadString();
p.time = DateTime.FromBinary(br.ReadInt64());
p.description = br.ReadString();
int cnt_podcastcategories = br.ReadInt32();
var cs = new List<int>();
for(int j=0; j<cnt_podcastcategories; ++j) cs.Add(br.ReadInt32());
p.categories=cs.ToArray();
localPodcasts.Add(p);
}
}
}
catch {
return false;
}
return true;
}

private static bool SaveLocalDB() {
if(categories==null || localPodcasts==null) return false;
string datadir = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)+"\\tyflopodcast";
System.IO.Directory.CreateDirectory(datadir);
try {
using (BinaryWriter bw = new BinaryWriter(File.Open(datadir+"\\db.dat", FileMode.Create))) {
for(int i=0; i<MagicNumber.Count(); ++i)
bw.Write(MagicNumber[i]);
bw.Write((Int32)categories.Count());
foreach(Category c in categories) {
bw.Write((Int32)c.id);
bw.Write(c.name);
}
bw.Write((Int32)localPodcasts.Count());
foreach(Podcast p in localPodcasts) {
bw.Write((Int32)p.id);
bw.Write(p.name);
bw.Write((Int64)p.time.ToBinary());
bw.Write(p.description);
bw.Write((Int32)p.categories.Count());
foreach(int c in p.categories)
bw.Write((Int32)c);
}
}
}
catch {
return false;
}
return true;
}

public static bool GetLocalPodcasts(out Podcast[] podcasts) {
podcasts=null;
if(!LoadLocalDB()) return false;
podcasts=localPodcasts.ToArray();
return true;
}

private static List<Comment> GetComments(string phrase=null, int podcast=0, int page=1) {
if(apiClient==null) Init();
String u=jsonurl+"/comments?order=asc&per_page=100";
if(phrase!=null) u+="&search="+phrase;
if(podcast!=0) u+="&post="+podcast.ToString();
if(page>1) u+="&page="+page.ToString();
var response = apiClient.GetAsync(u).Result;
var json = response.Content.ReadAsStringAsync().Result;
dynamic j = JsonConvert.DeserializeObject(json);
var headers = response.Headers;
IEnumerable<string> values;
var cs = new List<Comment>();
foreach(dynamic r in j) {
Comment c = new Comment();
c.author=WebUtility.HtmlDecode((String)r.author_name);
c.time = r.date;
c.podcast=r.post;
c.content = HTMLToText((string)r.content.rendered);
cs.Add(c);
}
if(headers.TryGetValues("X-WP-TotalPages", out values)) {
int pages = int.Parse(values.First());
if(pages>page)
cs=(List<Comment>)cs.Concat(GetComments(phrase, podcast, page+1));
}
return cs;
}

public static bool GetPodcastComments(int podcast, out Comment[] comments) {
comments=null;
List<Comment> cs = GetComments(null, podcast);
if(cs==null) return false;
comments = cs.ToArray();
return true;
}

public static bool LoadPodcastsWithComments(string phrase, out Podcast[] podcasts) {
var ids = new List<int>();
var comments = GetComments(phrase);
foreach(Comment c in comments)
if(!ids.Contains(c.podcast)) ids.Add(c.podcast);
var pd = new List<Podcast>();
foreach(Podcast p in localPodcasts)
if(ids.Contains(p.id)) pd.Add(p);
podcasts = pd.ToArray();
return true;
}

public static (bool, string, string) GetRadioContactInfo() {
try {
if(apiClient==null) Init();
String u=contacturl+"?ac=current";
var response = apiClient.GetAsync(u).Result;
var json = response.Content.ReadAsStringAsync().Result;
dynamic j = JsonConvert.DeserializeObject(json);
if(j.available==true) return (true, j.title, j.zoom_meeting_id);
else return (false, null, null);
} catch {
return (false, null, null);
}
}

public static (bool, string) SendRadioContact(string name, string message) {
//try {
if(apiClient==null) Init();
String u=contacturl+"?ac=add";
var jd = new Dictionary<string, string> { {"author", name}, {"comment", message} };
var jdata = JsonConvert.SerializeObject(jd);
var data = new StringContent(jdata, Encoding.UTF8, "application/json");
data.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
var response = apiClient.PostAsync(u, data).Result;
var json = response.Content.ReadAsStringAsync().Result;
dynamic j = JsonConvert.DeserializeObject(json);
if(j.ContainsKey("error"))
return (false, j.error);
else
return (true, null);
//} catch {
//return (false, "Nie udało się połączyć z serwerem");
//}
}

public static (bool, string) GetRadioProgram() {
try {
if(apiClient==null) Init();
String u=contacturl+"?ac=schedule";
var response = apiClient.GetAsync(u).Result;
var json = response.Content.ReadAsStringAsync().Result;
dynamic j = JsonConvert.DeserializeObject(json);
if(j.available==true) return (true, j.text);
else return (false, null);
} catch {
return (false, null);
}
}

public static (bool, string) CheckForUpdates() {
try {
if(apiClient==null) Init();
String u=versionurl;
var response = apiClient.GetAsync(u).Result;
var json = response.Content.ReadAsStringAsync().Result;
dynamic j = JsonConvert.DeserializeObject(json);
return (j.lastVersion!=Program.version, j.lastVersion);
} catch {
return (false, null);
}
}

public static bool WriteComment(string action, Dictionary<string, string> fields, string name, string mail, string url, string comment) {
if(apiClient==null) Init();
var dict = new Dictionary<string,string>();
dict["comment"]=comment;
dict["author"]=name;
dict["email"]=mail;
dict["url"]=url;
foreach(var field in fields)
dict.Add(field.Key,field.Value);
var response = apiClient.PostAsync(action, new FormUrlEncodedContent(dict)).Result;
var body = response.Content.ReadAsStringAsync().Result;
return true;
}

public static (string,Dictionary<string,string>) GetCommentsNonce(Podcast podcast) {
try {
if(apiClient==null) Init();
String u=jsonurl+"/posts/"+podcast.id.ToString();
var response = apiClient.GetAsync(u).Result;
var json = response.Content.ReadAsStringAsync().Result;
dynamic j = JsonConvert.DeserializeObject(json);
string link=j.link;
var pageResponse = apiClient.GetAsync(link).Result;
var page = pageResponse.Content.ReadAsStringAsync().Result;
var doc = new HtmlAgilityPack.HtmlDocument();
doc.LoadHtml(page);
var cmt = doc.GetElementbyId("commentform");
string action=cmt.GetAttributeValue("action","");
var fields = new Dictionary<string,string>();
if(cmt==null) return (null,null);
var nodes = cmt.Descendants("input");
foreach(var node in nodes) {
if(node.GetAttributeValue("type","").ToLower()=="hidden") fields[node.GetAttributeValue("name", "")]=node.GetAttributeValue("value","");
}
return (action,fields);
} catch {
return (null,null);
}
}

public static void LikePodcast(Podcast podcast) {
string datadir = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)+"\\tyflopodcast";
System.IO.Directory.CreateDirectory(datadir);
lock(internalDataLock) {
if(likes==null) likes = new List<int>();
if(!likes.Contains(podcast.id)) {
likes.Add(podcast.id);
SaveInternalLocked(datadir);
}
}
}

public static void DislikePodcast(Podcast podcast) {
string datadir = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)+"\\tyflopodcast";
System.IO.Directory.CreateDirectory(datadir);
lock(internalDataLock) {
if(likes==null) likes = new List<int>();
if(likes.Contains(podcast.id)) {
likes.Remove(podcast.id);
SaveInternalLocked(datadir);
}
}
}

public static Podcast[] GetLikedPodcasts() {
var ret = new List<Podcast>();
foreach(Podcast p in localPodcasts)
if(likes!=null && likes.Contains(p.id)) ret.Add(p);
return ret.ToArray();
}

public static Bookmark[] GetPodcastBookmarks(Podcast podcast) {
var ret = new List<Bookmark>();
foreach(Bookmark b in bookmarks)
if(b.podcast==podcast.id) ret.Add(b);
return ret.ToArray();
}

public static void AddBookmark(Podcast podcast, string name, float time) {
var b = new Bookmark();
b.podcast=podcast.id;
b.time=time;
b.name=name;
string datadir = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)+"\\tyflopodcast";
System.IO.Directory.CreateDirectory(datadir);
lock(internalDataLock) {
if(bookmarks==null) bookmarks = new List<Bookmark>();
bookmarks.Add(b);
SaveInternalLocked(datadir);
}
}

public static void DeleteBookmark(Bookmark bookmark) {
string datadir = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)+"\\tyflopodcast";
System.IO.Directory.CreateDirectory(datadir);
lock(internalDataLock) {
if(bookmarks==null) bookmarks = new List<Bookmark>();
bookmarks.Remove(bookmark);
SaveInternalLocked(datadir);
}
}

public static string GetResumeKeyForPodcast(int podcastId) {
if(podcastId<=0) return null;
return "p:"+podcastId.ToString();
}

public static string GetResumeKeyForFile(string filePath) {
if(string.IsNullOrWhiteSpace(filePath)) return null;
try {
filePath = Path.GetFullPath(filePath);
} catch {
}
return "f:"+filePath.Trim();
}

public static float GetResumePosition(string key) {
if(string.IsNullOrWhiteSpace(key)) return 0;
lock(internalDataLock) {
if(resumePositions==null) resumePositions = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);
if(resumePositions.TryGetValue(key, out float position)) return position;
return 0;
}
}

public static void SetResumePosition(string key, float position) {
if(string.IsNullOrWhiteSpace(key) || position<0) return;
string datadir = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)+"\\tyflopodcast";
System.IO.Directory.CreateDirectory(datadir);
lock(internalDataLock) {
if(resumePositions==null) resumePositions = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);
resumePositions[key]=position;
SaveInternalLocked(datadir);
}
}

public static void ClearResumePosition(string key) {
if(string.IsNullOrWhiteSpace(key)) return;
string datadir = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)+"\\tyflopodcast";
System.IO.Directory.CreateDirectory(datadir);
lock(internalDataLock) {
if(resumePositions==null) resumePositions = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);
if(resumePositions.Remove(key)) SaveInternalLocked(datadir);
}
}
}
}
