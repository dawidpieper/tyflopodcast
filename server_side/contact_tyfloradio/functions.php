<?php
/*
tyflopodcast.net client
Copyright Dawid Pieper
License: General Public License (GPLv3)
*/

define("TP_FILENAME", "._tp.dat");
define("TP_SCHEDULE_FILENAME", "._tp_schedule.dat");
define("PASSWORD", "SuperSecretAdminPassword");
define("TP_COOKIENAME", "tp_session");
define("TP_COMMENTS_PER_IP_HOURLY", 45);

$db = NULL;
$schedule = NULL;

session_name(TP_COOKIENAME);
session_set_cookie_params(18000, '/', $_SERVER['host'], false, false);

if(isset($_COOKIE[TP_COOKIENAME])) {
session_start();
if(isset($_SESSION['lastPostData'])) {
$_POST = $_SESSION['lastPostData'];
unset($_SESSION['lastPostData']);
}
}

class TPData {
public $title=null;
public $comments=array();
};
class TPComment {
public $author;
public $ip;
public $content;
public $timestamp;
}
class TPSchedule {
public $timefrom;
public $timeto;
public $text;
}

function TPGetData($useLocks = true, $fpPrev=null) {
global $db;
if($fpPrev==null && !file_exists(TP_FILENAME)) return new TPData();
if($fpPrev==null) $fp = fopen(TP_FILENAME, "rb");
else {
$fp = $fpPrev;
fseek($fp, 0);
}
if($useLocks) flock($fp, LOCK_SH);
$content="";
while(!feof($fp)) $content .= fread($fp, 8192);
if($useLocks) flock($fp, LOCK_UN);
if($fpPrev==null) fclose($fp);
try {
$un = unserialize($content);
if(get_class($un)!="TPData") throw(new UnexpectedValueException());
$db = $un;
return $un;
} catch(Exception $e) {return new TPData();}
}

function TPCreateEmpty($title) {
global $db;
$db = new TPData;
$db->title = $title;
$fp=fopen(TP_FILENAME, "wb");
fwrite($fp, serialize($db));
fclose($fp);
return true;
}

function TPDelete() {
global $db;
$db = new TPData;
$db->title = null;
unlink(TP_FILENAME);
return true;
}

function TpAddComment($author, $content) {
global $db;
$c = new TPComment();
$c->author = $author;
$c->content = $content;
$c->timestamp = time();
$c->ip = $_SERVER['REMOTE_ADDR'];
$fp = fopen(TP_FILENAME, "rb+");
flock($fp, LOCK_EX);
$db = TPGetData(false, $fp);
array_push($db->comments, $c);
fseek($fp, 0);
ftruncate($fp, 0);
fwrite($fp, serialize($db));
flock($fp, LOCK_UN);
fclose($fp);
return true;
}

function TPDeleteComment($id) {
global $db;
$fp = fopen(TP_FILENAME, "rb+");
flock($fp, LOCK_EX);
$db = TPGetData(false, $fp);
array_splice($db->comments, $id, 1);
fseek($fp, 0);
ftruncate($fp, 0);
fwrite($fp, serialize($db));
flock($fp, LOCK_UN);
fclose($fp);
return true;
}

if($_GET['pwd']==PASSWORD || $_POST['pwd']==PASSWORD) {
session_start();
$_SESSION['lastPostData']=$_POST;
$_SESSION['isAdmin']=true;
$url="?";
foreach($_GET as $k=>$v) {
if(strtolower($k)=="pwd") continue;
if($url=="?") $url.="&";
$url .= urlencode($k)."=".urlencode($v);
}
header("Location: ".$url);
die;
}

function isAdmin() {
return isset($_SESSION) && $_SESSION['isAdmin']==true;
}

function TPGetTitle() {
global $db;
if($db==NULL) $db = TPGetData();
return $db->title;
}

function TPGetComments() {
global $db;
if($db==NULL) $db = TPGetData();
return $db->comments;
}

function TPGetSchedule() {
global $schedule;
if(!file_exists(TP_SCHEDULE_FILENAME)) return new TPSchedule();
$fp = fopen(TP_SCHEDULE_FILENAME, "rb");
flock($fp, LOCK_SH);
$content="";
while(!feof($fp)) $content .= fread($fp, 8192);
flock($fp, LOCK_UN);
fclose($fp);
try {
$un = unserialize($content);
if(get_class($un)!="TPSchedule") throw(new UnexpectedValueException());
$schedule = $un;
return $un;
} catch(Exception $e) {return new TPSchedule();}
}

function TPIsScheduleAvailable() {
global $schedule;
if(!file_exists(TP_SCHEDULE_FILENAME)) return false;
if($schedule==NULL) $schedule = TPGetSchedule();
if($schedule->timeto<time()) return false;
return true;
}

function TPGetScheduleText() {
global $schedule;
if(!file_exists(TP_SCHEDULE_FILENAME)) return "";
if($schedule==NULL) $schedule = TPGetSchedule();
return $schedule->text;
}

function TPSetSchedule($timefrom, $timeto, $text) {
$sc = new TPSchedule();
$sc->timefrom = $timefrom;
$sc->timeto = $timeto;
$sc->text = $text;
$fp = fopen(TP_SCHEDULE_FILENAME, "wb");
flock($fp, LOCK_EX);
fwrite($fp, serialize($sc));
flock($fp, LOCK_UN);
fclose($fp);
return true;
}

function TPIsIPAllowed($ip) {
global $db;
if($db==NULL) $db = TPGetData();
$cnt=0;
foreach($db->comments as $c)
if($c->timestamp>time()-3600 && $c->ip == $ip) ++$cnt;
if($cnt>=TP_COMMENTS_PER_IP_HOURLY) return false;
else return true;
}