/*
tyflopodcast.net client
Copyright Dawid Pieper
License: General Public License (GPLv3)
*/

using System;
using System.Globalization;
using System.Data;
using System.Net;
using System.Net.Http;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using Newtonsoft.Json;

namespace Tyflopodcast {

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

public const String url = "http://tyflopodcast.net";
public const String jsonurl = "http://tyflopodcast.net/wp-json/wp/v2";

private static HttpClient apiClient;

public static void CleanUp() {
if(apiClient!=null) {
apiClient.Dispose();
apiClient=null;
}
}

public static void Init() {
ServicePointManager.DefaultConnectionLimit = 6;
HttpClientHandler hch = new HttpClientHandler();
hch.Proxy = null;
hch.UseProxy = false;
apiClient = new HttpClient(hch);
apiClient.BaseAddress = new Uri(url);
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

private static bool LoadLocalDB() {
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
}
}