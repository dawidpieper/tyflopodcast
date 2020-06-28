/*
tyflopodcast.net client
Copyright Dawid Pieper
License: General Public License (GPLv3)
*/

var commentsCount=-1;
function TPDOMFetchedComments(rsp) {
if(rsp['error']!=undefined) return;
var el = document.getElementById("commentsArea");
if(el==null) return;
if(commentsCount==rsp['comments'].length) return;
if(commentsCount!=-1) soundAlert(2);
commentsCount=rsp['comments'].length;
el.innerHTML="";
for(var i=0; i<rsp['comments'].length; ++i) {
var c = rsp['comments'][i];
var p = document.createElement("p");
var h = document.createElement("h2");
var link = document.createElement("a");
h.appendChild(document.createTextNode(c['author']));
p.appendChild(document.createTextNode(c['content']));
p.appendChild(document.createElement("br"));
var date = new Date(c['timestamp'] * 1000);
var hour = date.getHours();
var min = date.getMinutes();
hour = hour<10 ? "0"+hour : hour;
min = min<10 ? "0"+min : min;
p.appendChild(document.createTextNode(hour+":"+min));
link.id="comment:"+i;
link.onclick = function(){ TPDOMDeleteComment(this.id.substring("comment:".length));};
link.appendChild(document.createTextNode("Usuń"));
el.appendChild(h);
el.appendChild(p);
el.appendChild(link);
}
}
function TPDOMFetchComments() {
makeCommand("list", {}, undefined, TPDOMFetchedComments);
}
window.addEventListener('load', function() {
TPDOMFetchComments(false);
window.setInterval(TPDOMFetchComments, 5000);
});

function TPDOMCreate() {
var params = {'title':document.getElementById("title").value};
makeCommand("create", params, "Audycja rozpoczęta.", function(){
location.reload(true);
})
return false;
}

function TPDOMDispose() {
makeCommand("dispose", [], "Audycja zakończona.", function(){
location.reload(true);
})
return false;
}

function TPDOMDeleteComment(id) {
makeCommand("del", {'id':id}, "Komentarz został usunięty.", function(){
TPDOMFetchComments();
});
}

function TPDOMToggleScheduleEditor() {
var el = document.getElementById("scheduleEditor");
var link = document.getElementById("scheduleLink");
if(el.style.display!="") {
el.style.display="";
link.setAttribute('aria-expanded', true);
} else {
el.style.display = "none"
link.setAttribute('aria-expanded', false);
}
}

function TPDOMSetSchedule() {
makeCommand("setschedule", {'timefrom':document.getElementById("sctimefrom").value, 'timeto':document.getElementById("sctimeto").value, 'text':document.getElementById("sctext").value}, "Ramówka została zaktualizowana");
return false;
}