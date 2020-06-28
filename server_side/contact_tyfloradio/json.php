<?php
/*
tyflopodcast.net client
Copyright Dawid Pieper
License: General Public License (GPLv3)
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
case 'add':
if(isAdmin() || TPIsIPAllowed($_SERVER['REMOTE_ADDR'])) {
if(TPAddComment($_POST['author'], $_POST['comment'])==false)
$j['error']="Nie udało się dodać komentarza.";
} else
$j['error']="Umieściłeś ostatnio zbyt wiele komentarzy. Spróbuj ponownie za kilkadziesiąt minut.";
break;
case 'del':
if(isAdmin()) {
if(TPDeleteComment($_POST['id'])==false)
$j['error']="Nie udało się dodać komentarza.";
} else
$j['error'] = "Wymagane jest uwierzytelnienie przed wykonaniem tej operacji.";
break;
case 'create' :
if(isAdmin()) {
if(TPCreateEmpty($_POST['title'])==false)
$j['error']="Nie udało się utworzyć bazy danych.";
} else
$j['error'] = "Wymagane jest uwierzytelnienie przed wykonaniem tej operacji.";
break;
case 'dispose':
if(isAdmin()) {
if(TPDelete()==false)
$j['error']="Nie udało się usunąć bazy danych.";
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
} else
$j['error'] = "Wymagane jest uwierzytelnienie przed wykonaniem tej operacji.";
break;
default:
$j['error'] = "Nie rozpoznano polecenia";
break;
}
header("Content-Type: application/json");
http_response_code($j['status']);
echo json_encode($j);
?>