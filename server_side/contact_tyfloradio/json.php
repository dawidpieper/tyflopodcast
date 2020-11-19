<?php
/*
A part of Tyflopodcast - tyflopodcast.net client.
Copyright (C) 2020 Dawid Pieper
This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, version 3. 
This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details. 
You should have received a copy of the GNU General Public License along with this program. If not, see <https://www.gnu.org/licenses/>. 
*/

require("functions.php");

$contentType = isset($_SERVER["CONTENT_TYPE"]) ? trim($_SERVER["CONTENT_TYPE"]) : '';

if(strcasecmp($contentType, 'application/json') == 0) {
$content = trim(file_get_contents("php://input"));
$decoded = json_decode($content, true);
foreach($decoded as $k=>$v)
$_POST[$k]=$v;
}

$j=array();
switch($_GET['ac']) {
case 'current':
$title = TPGetTitle();
if($title==null) $j['available']=false;
else {
$j['available']=true;
$j['title']=$title;
$j['zoom_meeting_id']=TPGetZoomMeetingId();
}
break;
case 'add':
if(isAdmin() || TPIsIPAllowed($_SERVER['REMOTE_ADDR'])) {
if(TPAddComment($_POST['author'], $_POST['comment'])==false)
$j['error']="Nie udało się dodać komentarza.";
else {
$j['author']=$_POST['author'];
$j['comment']=$_POST['comment'];
}
} else
$j['error']="Umieszczono ostatnio zbyt wiele komentarzy. Spróbuj ponownie za kilkadziesiąt minut.";
break;
case 'del':
if(isAdmin()) {
$comm = TPGetComments()[$_POST['id']];
if(TPDeleteComment($_POST['id'])==false)
$j['error']="Nie udało się dodać komentarza.";
else {
$j['author']=$comm->author;
$j['comment'] = $comm->comment;
}
} else
$j['error'] = "Wymagane jest uwierzytelnienie przed wykonaniem tej operacji.";
break;
case 'create' :
if(isAdmin()) {
if(TPCreateEmpty($_POST['title'])==false)
$j['error']="Nie udało się utworzyć bazy danych.";
else
$j['title']=$_POST['title'];
} else
$j['error'] = "Wymagane jest uwierzytelnienie przed wykonaniem tej operacji.";
break;
case 'dispose':
if(isAdmin()) {
$title = TPGetTitle();
if(TPDelete()==false)
$j['error']="Nie udało się usunąć bazy danych.";
else
$j['title']=$title;
} else
$j['error'] = "Wymagane jest uwierzytelnienie przed wykonaniem tej operacji.";
break;
case 'list':
$j['comments']=array();
foreach(TPGetComments() as $c) array_push($j['comments'], array('author'=>$c->author, 'timestamp'=>$c->timestamp, 'content'=>$c->content));
break;
case 'schedule':
$j['available'] = TPIsScheduleAvailable();
if($j['available']) $j['text']=TPGetScheduleText();
break;
case 'setschedule':
if(isAdmin()) {
if(TPSetSchedule(strtotime($_POST['timefrom']), strtotime($_POST['timeto']), $_POST['text'])==false)
$j['error']="Nie udało się zaktualizować ramówki.";
else {
$j['timefrom'] = strtotime($_POST['timefrom']);
$j['timeto'] = strtotime($_POST['timeto']);
$j['text'] = $_POST['text'];
}
} else
$j['error'] = "Wymagane jest uwierzytelnienie przed wykonaniem tej operacji.";
break;
default:
$j['error'] = "Nie rozpoznano polecenia";
break;
}
header("Content-Type: application/json");
echo json_encode((Object)$j);
?>