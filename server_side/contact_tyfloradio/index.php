<?php
require("functions.php");
?>

<!doctype html>
<html lang="pl-PL">
<head>
<meta http-equiv="Content-Type" content="text/html; charset=utf-8">
<title>Napisz do Tyfloradia - Tyflopodcast</title>
<script language=javascript src=main.js></script>
<?php
if(isAdmin())
echo '<script language=javascript src=admin.js></script>';
?>
</head>
<body>
<?php
$title=TPGetTitle();
if($title!=NULL) echo "<h1>Trwająca audycja: ".$title."</h1>";
if(isAdmin()) {
if($title!=null) {
echo "<a onclick=\"TPDOMDispose();\">Zakończ audycję</a>";
echo "<h1>Umieszczone komentarze</h1>";
echo "<div id=commentsArea></div>";
} else {
?>
<h1>Rozpocznij audycję</h1>
<form onsubmit="return TPDOMCreate();">
<p>
<label for=title>Tytuł audycji:</label>
<input id=title type=text>
</p>
<input type=submit value="Rozpocznij audycję">
</form>
<?php } ?>
<a aria-expanded=false id=scheduleLink onclick="TPDOMToggleScheduleEditor();">Pokaż/Ukryj edytor ramówki</a>
<div id=scheduleEditor  style="display: none">
<h2>Edycja ramówki</h2>
<?php
if(TPIsScheduleAvailable()) {
$schedule = TPGetSchedule();
$timefromint = $schedule->timefrom;
$timetoint = $schedule->timeto;
$text = $schedule->text;
} else {
$timefromint = time();
$timetoint = strtotime('next sunday');
$text="";
}
$timefrom = date("Y-m-d", $timefromint);
$timeto = date("Y-m-d", $timetoint);
?>
<form onsubmit="return TPDOMSetSchedule();">
<p>
<label for=sctimefrom>Początek obowiązywania ramówki (RRRR-MM-DD): </label>
<input required id=sctimefrom type=text value="<?php echo $timefrom; ?>">
</p>
<p>
<label for=sctimeto>Koniec obowiązywania ramówki (RRRR-MM-DD): </label>
<input required id=sctimeto type=text value="<?php echo $timeto; ?>">
</p>
<p>
<label for=sctext>Tekst ramówki: </label>
<textarea id=sctext rows=25 cols=100><?php echo htmlspecialchars($text);?></textarea>
</p>
<input type=submit value="Wyślij">
</form>
</div>
<?php } ?>
<?php if($title!=null) { ?>
<h2>Napisz do nas</h2>
<form onsubmit="return TPDOMAddComment();">
<p>
<label for=author>Podpis: </label>
<input required type=text id=author>
</p>
<p>
<label for=comment>Treść komentarza:</label>
<textarea required id=comment rows=20 cols=100></textarea>
</p>
<p>
<input type=submit value="Wyślij">
</p>
</form>
<?php }else{ ?>
<p>W tej chwili nie trwa żadna audycja interaktywna.</p>
<?php } ?>
<a onclick="TPDOMShowSchedule();">Pokaż ramówkę</a>
<div id=schedule></div>
<a href="http://tyflopodcast.net">Wróć do strony głównej</a>
</body>
</html>