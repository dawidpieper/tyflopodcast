/*
tyflopodcast.net client
Copyright Dawid Pieper
License: General Public License (GPLv3)
*/

function notify(text) {
var notification = document.createElement('p');
notification.setAttribute('role', 'alert');
notification.appendChild(document.createTextNode(text));
document.body.appendChild(notification);
setTimeout(function () {
document.body.removeChild(notification);
}, 5000);
}

audioCtx = new(window.AudioContext || window.webkitAudioContext)();
function soundAlert(times=1, frequency=440, callback) {
var oscillator = audioCtx.createOscillator();
var gainNode = audioCtx.createGain();
gainNode.gain.value = 0.1;
oscillator.connect(gainNode);
gainNode.connect(audioCtx.destination);
oscillator.frequency.value = frequency;

function onended() {
if(times>1) soundAlert(times-1, frequency*2);
}
oscillator.onended = onended;
oscillator.start(audioCtx.currentTime);
oscillator.stop(audioCtx.currentTime+0.1);
};

function makeCommand(ac, params, sucinfo=undefined, callback=undefined) {
var url = "json.php?ac="+ac;
var req = new XMLHttpRequest();
req.onload = function() {
rsp =  JSON.parse(this.responseText);
if(rsp['error']!=undefined)
alert("Wystąpił błąd podczas wykonywania ostatniej operacji: "+rsp['error']+".");
else
if(sucinfo!=undefined)
notify(sucinfo);
if(callback!=undefined)
callback(rsp);
};
req.open("POST", url, true);
req.setRequestHeader("Content-Type", "application/json");
req.send(JSON.stringify(params));
}

function TPDOMAddComment() {
var params = {'author':document.getElementById("author").value, 'comment':document.getElementById("comment").value};
makeCommand("add", params, "Komentarz został dodany, dziękujemy.", function(){
document.getElementById("comment").value="";
})
return false;
}

function TPDOMShowSchedule() {
makeCommand("schedule", {}, undefined, function(rsp) {
var el = document.getElementById("schedule");
el.innerHTML="";
if(rsp['available']==false)
notify("Aktualna ramówka nie jest jeszcze dostępna.");
else {
var p = document.createElement("p");
var h = document.createElement("h2");
h.appendChild(document.createTextNode("Aktualna ramówka"));
p.appendChild(document.createTextNode(rsp['text']));
p.style="white-space: pre;";
el.appendChild(h);
el.appendChild(p);
}
});
}