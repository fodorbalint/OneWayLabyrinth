<?php
if ($_SERVER['REQUEST_METHOD'] === 'POST') {
  if ($_GET["a"] == 1) {
    $data = json_decode(file_get_contents('php://input'), true);
    file_put_contents('settings.json', json_encode($data, JSON_PRETTY_PRINT));
    echo "Settings saved.";
  }
  else if ($_GET["a"] == 2) {
    $num = 0;
    $found = false;
    $data = json_decode(file_get_contents('php://input'), true);
    foreach ($data as $key => $value) {
      $content = file("new logs/log descriptions.txt");
      for ($i = 0; $i < count($content); $i++) {
        $line = $content[$i];
        $pos = strpos($line, ":");
        if ($pos != -1) {
          $num = substr($line, 0, $pos);
          if ($key == "voltage4_log_".$num.".txt") {
            $content[$i] = $num.": ".$value."\n";
            $found = true;
            print("OK");
            break;
          }
        }
      }
      if (!$found) {
        $num = str_replace(".txt", "", str_replace("voltage4_log_", "", $key));
        $content[] = "$num: $value\n";
        print("OK");
      }

      file_put_contents("new logs/log descriptions.txt", $content);
    } 
  }  
  else if ($_GET["a"] == 3) {
    $data = json_decode(file_get_contents('php://input'), true);
    foreach ($data as $key => $value) {
      $num0 = str_replace(".txt", "", str_replace("voltage4_log_", "", $value));

      $found = false;
      $content = file("new logs/log descriptions.txt");
      for ($i = 0; $i < count($content); $i++) {
        $line = $content[$i];
        $pos = strpos($line, ":");
        if ($pos != -1) {
          $num = substr($line, 0, $pos);
          if ($num0 == $num) {
            $found = true;
            unset($content[$i]);
            break;
          }
        }
      }
      if ($found) {
        file_put_contents("new logs/log descriptions.txt", $content);
      }
      unlink("new logs/".$value);
      print("OK");
    }
  }
  die();
}
?>
<!DOCTYPE html>
<html lang="en">
<head>
  <meta charset="UTF-8">
  <title>Show chart</title>
  <style>
    body {
      margin: 0;
      overflow-x: hidden;
      overflow-y: hidden;
      font-size: 20px;
      position: fixed;
      width: 100%;
      height: 100%;
    }
    div {
      color: white;
      background-color: black;
      font-family: inherit;
      font-size: inherit;
    }
    #mainDiv {
      display: flex;
      flex-direction: row;
      width: 100%;
      height: 100%;
    }
    #chartDiv {
      flex-grow: 1;
      margin-top: 100px;
      overflow-x: scroll;
      overflow-y: scroll;
    }
    #chart {
      height: 100%;
      display: block;
    }
    .ch1 {
      color: yellow;
    }
    .ch2 {
      color: lime;
    }
    .ch3 {
      color: cyan;
    }
    .ch4 {
      color: #ff8080;
    }
    #logFilesFullContainer {
      display: flex;
      flex-direction: row;
    }
    #resizeColumn {
      position: absolute;
      width: 10px;
      height: 100%;
      cursor: ew-resize;
    }
    #logFilesFull {
      width: 0px;
      user-select: none;
    }
    #logFiles {
      margin-right: 20px;
    }
    select[size] option, select[multiple] option {
      padding-left: 10px;
      padding-right: 20px;
      user-select: none;
    }
    #infoBox {
      position: fixed;
      left: 0px;
      top: 10000px;
      padding: 10px;
      background: rgba(0, 0, 0, 0.75);
      border: 1px solid white;
      pointer-events: none;
      user-select: none;
    }
    #zoomLevelDiv {
      position: fixed;
      left:20px;
      top:20px;
    }
    #logDiv {
      position: fixed;
      left:160px;
      top:20px;
      display: flex;
      align-items: flex-start;
    }  
    #zoomLevel {
      color: white;
      background-color: black;
      border: 1px solid gray;
      font-family: inherit;
      font-size: inherit;
    }
    .activeButton {
      color: white;
      background-color: black;
      border: 1px solid gray;
      font-family: inherit;
      font-size: inherit;
    }
    .activeButton:hover {
      background-color: #303030;
    }
    .activeButton:active {
      box-shadow: 2px 2px 5px #808080;
    }
    .passiveButton {
      color: white;
      background-color: black;
      border: 1px solid gray;
      font-family: inherit;
      font-size: inherit;
      opacity: 0.7;
    }
    textarea {
      color: white;
      background-color: black;
      border: 1px solid gray;
      font-family: inherit;
      font-size: inherit;
      width: 500px;
      margin-right: 10px;
    }
    select {
      color: white;
      background-color: black;
      font-family: inherit;
      font-size: inherit;
    }
    svg {
      width: 0vh;
      height: calc(100vh - 136px);
      transition: transform 0.1s ease;
    }
    .small {
        font: {fontSize}px sans-serif;
    }
  </style>
</head>
<body>
  <div id="mainDiv">
    <div id="chartDiv"><svg preserveAspectRatio="none" id="chart" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 0 0">
        <path d="" id="line1" stroke="yellow" stroke-width="1" fill="none" vector-effect="non-scaling-stroke" />
        <path d="" id="line2" stroke="lime" stroke-width="1" fill="none" vector-effect="non-scaling-stroke" />
        <path d="" id="line3" stroke="cyan" stroke-width="1" fill="none" vector-effect="non-scaling-stroke" />
        <path d="" id="line4" stroke="#ff8080" stroke-width="1" fill="none" vector-effect="non-scaling-stroke" />
        <path d="" id="crosshair" stroke="white" stroke-width="1" fill="none" vector-effect="non-scaling-stroke" />
        <path d="" id="zoomRect" stroke="white" stroke-width="1" fill="none" vector-effect="non-scaling-stroke" />
      </svg></div>
    <div id="logFilesFullContainer">
      <select name="logFilesFull" id="logFilesFull" size="2">
  <?php
          $dir = 'c:/Users/Balint/OneDrive/Documents/BatteryTest/new logs/';
          $dir = "c:/Users/Balint/OneWayLabyrinth/new logs/";
          $dir = "new logs/";
          $handler = opendir($dir);
          $results = array();
          while ($file = readdir($handler)) {
              if ($file != "." && $file != "..") {
                  if(preg_match('/voltage4_log_(\d+).txt/', $file)) {
                      $results[] = $file;
                  }
              }
          }

          rsort($results);
          foreach ($results as $file) {
              $num0 = str_replace(".txt", "", str_replace("voltage4_log_", "", $file));

              $text = "";
              $found = false;
              $content = file("new logs/log descriptions.txt");
              for ($i = 0; $i < count($content); $i++) {
                $line = $content[$i];
                $pos = strpos($line, ":");
                if ($pos != -1) {
                  $num = substr($line, 0, $pos);
                  $text = trim(substr($line, $pos + 1));
                  if ($num0 == $num) {
                    $found = true;
                    break;
                  }
                }
              }
              if (!$found) {
                $text = "";
              }
              print "\t\t<option value='$file'>$num0: $text</option>\n";
          }
      ?>
      </select>
      <div id="resizeColumn"></div>
    </div>
  </div>
  <div id="infoBox"></div>
  <div id="zoomLevelDiv">
    Zoom: 
    <input id="zoomLevel" type="text" style="width:60px; height:25px; margin-bottom: 5px;" /><br />
    <button id="button1" onclick="showHideLine(1)" style="width:25px; height:25px; background-color: yellow; border: 1px solid gray"></button>
    <button id="button2" onclick="showHideLine(2)" style="width:25px; height:25px; background-color:lime; display:inline; border:1px solid gray"></button>
    <button id="button3" onclick="showHideLine(3)" style="width:25px; height:25px; background-color: cyan; display:inline; border:1px solid gray"></button>
    <button id="button4" onclick="showHideLine(4)" style="width:25px; height:25px; background-color: #ff8080; display:inline; border:1px solid gray"></button>
  </div>
  <div id="logDiv">
    <!--<select name="logFiles" id="logFiles">
        <option value="">-- Choose an option --</option>
  <?php
            foreach ($results as $file) {
                print "\t\t<option value='$file'>$file</option>\n";
            }
        ?>
    </select>-->
    <textarea id="description" rows="3"></textarea>
    <input type="button" class="activeButton" id="updateDesc" value="Update" />
  </div>
  <script src="zoom.js?v=<?=time()?>"></script>
  <script>
    //962 in my browser window. In order to lay one data point per pixel, if there are 3500 samples per second, one second must be 3500/962 * 100vh wide      
    //document.body.onload = function() {alert(document.getElementById("measureDiv").offsetHeight)};
  </script>
</body>
</html>