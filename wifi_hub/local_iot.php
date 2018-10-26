<html>
<body>
<?php
$lat=$_GET['lat'];
$lon=$_GET['lon'];
echo shell_exec('node /home/pi/Documents/d2/hello.js '.$lat.' '.$lon);
?>
</body>
</html>
