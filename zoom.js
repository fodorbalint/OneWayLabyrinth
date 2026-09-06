// zoom rectangle with dragging left button

const chart = document.getElementById("chart");
const chartDiv = document.getElementById("chartDiv");
const infoBox = document.getElementById("infoBox");
const zoomLevel = document.getElementById("zoomLevel");
// const logFiles = document.getElementById("logFiles");
const logFilesFull = document.getElementById("logFilesFull");
const resizeColumn = document.getElementById("resizeColumn");
const description = document.getElementById("description");
const updateButton = document.getElementById("updateDesc");
const crosshair = document.getElementById("crosshair");
const zoomRect = document.getElementById("zoomRect");
let zoomFactor = 1.1;
const MAX_ZOOM = 2;
const MIN_V_RANGE = 0.05;
let moveFactor = 40;
/*let showLine1 = true;
let showLine2 = true;
let showLine3 = true;
let showLine4 = true;*/
let settingsFile;
let descriptionFile;
let textContent;
let minV = 0;
let maxV = 0;
let maxH = 0; // last timestamp in microseconds
let scaleMaxH = 0; // number of items in the file -> svg viewBox width
let scaleMaxV = 0; // graph screen height -> svg viewBox height
let mouseX = 0;
let mouseY = 0;
let startMouseX = 0;
let startMouseY = 0;
let mouseLeftDown = false;
let mouseRightDown = false;
let resizeMouseLeftDown = false;
let resizeMouseX = 0;
let timeoutID;
let timeout2ID;
let timeout3ID;
let all_values;
const eventSource = new EventSource('check_updates.php');
let firstLoad = false; // prevents updating settings when scroll state is restored

window.addEventListener('contextmenu', (event) => {
  event.preventDefault(); // Prevents the menu from opening
});

document.body.onload = function () {
  rect = chart.getBoundingClientRect();
  scaleMaxV = rect.height;
  fetch('settings.json?v=' + Date.now())
  .then(response => response.json())
  .then(settings => {
    settingsFile = settings;
    zoomLevel.value = round(settingsFile.zoomState, 3); 
    if (!settings.yellow) {
      document.getElementById("line1").style.display = "none";
      document.getElementById("button1").style.opacity = 0.5;
    }
    if (!settings.green) {
      document.getElementById("line2").style.display = "none";
      document.getElementById("button2").style.opacity = 0.5;
    }
    if (!settings.blue) {
      document.getElementById("line3").style.display = "none";
      document.getElementById("button3").style.opacity = 0.5;
    }
    if (!settings.red) {
      document.getElementById("line4").style.display = "none";
      document.getElementById("button4").style.opacity = 0.5;
    } 
    if (settings.logFile != "") {
      logFilesFull.value = settings.logFile;
      
      fetch('new logs/' + settings.logFile)
      .then(response => response.text())
      .then(data => {
        firstLoad = true;
        updateGraph(data);
      });
      loadDescription();
    } 
    settingsFile.listColumnWidth = settings.listColumnWidth;
    logFilesFull.style.width = settingsFile.listColumnWidth + "px";
  });
}

chart.addEventListener("mousedown", (event) => {
  if (event.button == 0) {
    mouseLeftDown = true;
    startMouseX = mouseX;
    startMouseY = mouseY;
  }
  else if (event.button == 2) {
    mouseRightDown = true;
    startMouseX = mouseX;
    startMouseY = mouseY;
  }
});

chart.addEventListener("mousemove", (event) => {
  if (resizeMouseLeftDown) {
    return;
  }
  if (mouseLeftDown) {
    endMouseX = event.clientX;
    endMouseY = event.clientY;

    if (!(startMouseX == endMouseX || startMouseY == endMouseY)) {
      drawZoomRect();
    }
  }
  if (mouseRightDown) {
    diffX = event.clientX - mouseX;
    if (diffX < 0) {
      if (chartDiv.scrollLeft < chart.clientWidth - chartDiv.clientWidth) {
        if (diffX < chartDiv.scrollLeft - (chart.clientWidth - chartDiv.clientWidth)) {
          diffX = chartDiv.scrollLeft - (chart.clientWidth - chartDiv.clientWidth);
        }
        settingsFile.midScroll -= diffX / chart.clientWidth;
      }
    }
    else {
      if (chartDiv.scrollLeft > 0) {
        if (diffX > chartDiv.scrollLeft) {
          diffX = chartDiv.scrollLeft;
        }
        settingsFile.midScroll -= diffX / chart.clientWidth;
      }
    }
    updateViewBox();
  }
  else {
    // prevent scrolling when I resize the file list
    event.preventDefault();
  }

  if (!mouseLeftDown) {
    updateCrosshair();
  }
  
  mouseX = event.clientX;
  mouseY = event.clientY;
});

// mousemove will fire once after mouseup, restoring crosshair and info box if a zoom rectangle was dragged. Setting info box display to none is not necessary.
chart.addEventListener("mouseup", (event) => {
  if (event.button == 0 && mouseLeftDown) {
    mouseLeftDown = false;
    zoomRect.setAttribute("d", "");

    // Zoom in max if one point is clicked
    if (mouseX == startMouseX && mouseY == startMouseY) {
      let origZoomState = settingsFile.zoomState;
      settingsFile.zoomState = MAX_ZOOM;
      posAtCursor = (chartDiv.scrollLeft + mouseX) / chart.clientWidth;
      settingsFile.midScroll = posAtCursor + (settingsFile.midScroll - posAtCursor) / settingsFile.zoomState * origZoomState;
      zoomLevel.value = round(settingsFile.zoomState, 3);
      saveSettings();  
      updateViewBox();
    }
    else if (!(mouseX == startMouseX || mouseY == startMouseY)) {
      finishZoomRect();
    }
  }
  if (event.button == 2) {
    mouseRightDown = false;
    event.preventDefault();
    // Reset view
    if (mouseX == startMouseX && mouseY == startMouseY) {
      if (chart.clientHeight == scaleMaxV) { // reset view
        settingsFile.zoomState = chartDiv.clientWidth / scaleMaxH
        zoomLevel.value = round(settingsFile.zoomState, 3);
        settingsFile.midScroll = 0.5;
        saveSettings(); 
        updateViewBox(); 
      }
      else { // zoom out to full height
        chart.style.height = scaleMaxV + "px";
      }
    }
  }
});

chart.addEventListener("mouseleave", (event) => {
  mouseRightDown = false;
  crosshair.setAttribute("d", "");
  event.preventDefault();
});

function drawZoomRect(startX, startY, endX, endY, rectLeft, rectWidth) {
  infoBox.style.display = "block";
  crosshair.setAttribute("d", "");

  let leftMouseX, rightMouseX, upMouseY, downMouseY;
  let leftX, rightX, upY, downY;

  if (startMouseX < endMouseX) {
    leftMouseX = startMouseX;
    rightMouseX = endMouseX;
  }
  else {
    leftMouseX = endMouseX;
    rightMouseX = startMouseX;
  }
  if (startMouseY < endMouseY) {
    upMouseY = startMouseY;
    downMouseY = endMouseY;
  }
  else {
    upMouseY = endMouseY;
    downMouseY = startMouseY;
  }

  const rect = chart.getBoundingClientRect();
  leftX = (leftMouseX - rect.left) / rect.width * scaleMaxH;
  upY = (upMouseY - rect.top) / rect.height * scaleMaxV;
  rightX = (rightMouseX - rect.left) / rect.width * scaleMaxH;
  downY = (downMouseY - rect.top) / rect.height * scaleMaxV;

  zoomRect.setAttribute("d", "M " + leftX + " " + upY + " L " + rightX + " " + upY + " L " + rightX + " " + downY + " L " + leftX + " " + downY + "z");

  // console.log("leftMouseX " + leftMouseX + " rightMouseX " + rightMouseX + " rect.left " + rect.left + " rect.width " + rect.width + " maxH " + maxH)

  // rightMouseX - rect.left) / rect.width does not go all the way to 1, so the time period end will fall to the last before element.

  timeAtCursorLeft = (leftMouseX - rect.left) / rect.width * maxH;
  timeAtCursorRight = (rightMouseX - rect.left) / rect.width * maxH;

  minTimeRange = chartDiv.clientWidth / scaleMaxH * maxH / MAX_ZOOM;

  if ((timeAtCursorRight - timeAtCursorLeft) < minTimeRange) {
    offset = (minTimeRange - (timeAtCursorRight - timeAtCursorLeft)) / 2;
    timeAtCursorLeft -= offset;
    timeAtCursorRight += offset;
    if (timeAtCursorLeft < 0) {
      timeAtCursorLeft = 0;
      timeAtCursorRight = minTimeRange;
    }
    else if (timeAtCursorRight > maxH) {
      timeAtCursorRight = maxH;
      timeAtCursorLeft = maxH - minTimeRange;
    }
  }

  voltageAtCursorUp = (maxV - (upMouseY - rect.top) / rect.height * (maxV - minV));
  voltageAtCursorDown = (maxV - (downMouseY - rect.top) / rect.height * (maxV - minV));

  if (voltageAtCursorUp - voltageAtCursorDown < MIN_V_RANGE) {
    if (maxV - minV < MIN_V_RANGE) {
      voltageAtCursorDown = minV;
      voltageAtCursorUp = maxV;
    }
    else {
      offset = (MIN_V_RANGE - (voltageAtCursorUp - voltageAtCursorDown)) / 2;
      voltageAtCursorDown -= offset;
      voltageAtCursorUp += offset;
      if (voltageAtCursorDown < minV) {
        voltageAtCursorDown = minV;
        voltageAtCursorUp = minV + MIN_V_RANGE
      }
      else if (voltageAtCursorUp > maxV) {
        voltageAtCursorUp = maxV;
        voltageAtCursorDown = maxV - MIN_V_RANGE;
      }
    }
  }

  // approximate indices, due to the time being uneven between measurements. 
  // = round(([left/right]MouseX - rect.left) / rect.width can be both 0, 1 and in between, so it needs to be multiplied by ([count of elements] - 1), which scaleMaxH is.
  // Example: elements: 0 1 2 3, scaleMaxH 3
  valuesIndexStart = round((leftMouseX - rect.left) / rect.width * scaleMaxH);
  valuesIndexEnd = round((rightMouseX - rect.left) / rect.width * scaleMaxH);
  indexTimeAtCursorLeft = all_values[valuesIndexStart][0]
  indexTimeAtCursorRight = all_values[valuesIndexEnd][0]

  // find the exact index
  i = 0;
  if (timeAtCursorLeft < indexTimeAtCursorLeft) {
    diff = indexTimeAtCursorLeft - timeAtCursorLeft; 
    for (i = valuesIndexStart - 1; i >= 0; i--) {
      newDiff = all_values[i][0] - timeAtCursorLeft;
      if (Math.abs(newDiff) > diff) {
        i++;
        break;
      }
      diff = newDiff;
    }
    // the first element in the array is the closest, loop finished with subtracting -1 from i.
    if (i == -1) i = 0;
    valuesIndexStart = i;
  }
  else if (timeAtCursorLeft > indexTimeAtCursorLeft) {
    diff = timeAtCursorLeft - indexTimeAtCursorLeft; 
    for (i = valuesIndexStart + 1; i <= all_values.length - 1; i++) {
      newDiff = timeAtCursorLeft - all_values[i][0];
      if (Math.abs(newDiff) > diff) {
        i--;
        break;
      }
      diff = newDiff;
    }
    if (i == all_values.length) i--;
    valuesIndexStart = i;
  }

  if (timeAtCursorRight < indexTimeAtCursorRight) {
    diff = indexTimeAtCursorRight - timeAtCursorRight; 
    for (i = valuesIndexEnd - 1; i >= 0; i--) {
      newDiff = all_values[i][0] - timeAtCursorRight;
      if (Math.abs(newDiff) > diff) {
        i++;
        break;
      }
      diff = newDiff;
    }
    if (i == -1) i = 0;
    valuesIndexEnd = i;
  }
  else if (timeAtCursorRight > indexTimeAtCursorRight) {
    diff = timeAtCursorRight - indexTimeAtCursorRight; 
    for (i = valuesIndexEnd + 1; i <= all_values.length - 1; i++) {
      newDiff = timeAtCursorRight - all_values[i][0];
      if (Math.abs(newDiff) > diff) {
        i--;
        break;
      }
      diff = newDiff;
    }
    if (i == all_values.length) i--;
    valuesIndexEnd = i;
  }

  indexTimeAtCursorLeft = all_values[valuesIndexStart][0]
  indexTimeAtCursorRight = all_values[valuesIndexEnd][0]

  ch1sum = 0;
  ch2sum = 0;
  ch3sum = 0;
  ch4sum = 0;

  for(i = valuesIndexStart; i <= valuesIndexEnd; i++) {
    ch1sum += all_values[i][1];
    ch2sum += all_values[i][2];
    ch3sum += all_values[i][3];
    ch4sum += all_values[i][4];
  }

  count = valuesIndexEnd - valuesIndexStart + 1;

  // console.log("timeAtCursorLeft " + round(timeAtCursorLeft, 0).toLocaleString('en-US') + " timeAtCursorRight " + round(timeAtCursorRight, 0).toLocaleString('en-US') + " leftMouseX " + leftMouseX + " rightMouseX " + rightMouseX + " rect.left " + rect.left + " rect.width " + rect.width + " maxH " + maxH + " count " + all_values.length + " indexStart " + valuesIndexStart + " indexEnd " + valuesIndexEnd)

  infoBox.innerHTML = parseInt(timeAtCursorLeft).toLocaleString('en-US') + " - " + parseInt(timeAtCursorRight).toLocaleString('en-US') + " &micro;s<br />" +
  round(voltageAtCursorUp, 3) + " - " + round(voltageAtCursorDown, 3) + " V<br />" +
  "<span class=\"ch1\">" + round(ch1sum / count, 3) + "</span><br />" + 
  "<span class=\"ch2\">" + round(ch2sum / count, 3) + "</span><br />" +
  "<span class=\"ch3\">" + round(ch3sum / count, 3) + "</span><br />" + 
  "<span class=\"ch4\">" + round(ch4sum / count, 3) + "</span>";

  infoBox.style.display = "block";
  if (endMouseX + infoBox.offsetWidth + 10 > chartDiv.clientWidth) {
    infoBox.style.left = (endMouseX - (infoBox.offsetWidth + 10)) + "px";
  }
  else {
    infoBox.style.left = (endMouseX + 10) + "px";
  }

  if (endMouseY - rect.top < infoBox.offsetHeight + 10) {
    infoBox.style.top = (mouseY + 10) + "px";
  }
  else {
    infoBox.style.top = (mouseY - (infoBox.offsetHeight + 10)) + "px";
  }
}

function finishZoomRect() {
  settingsFile.zoomState = maxH / (timeAtCursorRight - timeAtCursorLeft) * chartDiv.clientWidth / scaleMaxH;
  settingsFile.midScroll = (timeAtCursorLeft + timeAtCursorRight) / 2 / maxH;
  zoomLevel.value = round(settingsFile.zoomState, 3);

  newHeight = scaleMaxV * (maxV - minV) / (voltageAtCursorUp - voltageAtCursorDown);
  chart.style.height = newHeight + "px";
  chartDiv.scrollTop = newHeight * (maxV - voltageAtCursorUp) / (maxV - minV);

  zoomLevel.value = round(settingsFile.zoomState, 3);
  saveSettings();  
  updateViewBox();
}

function updateCrosshair() {
  const rect = chart.getBoundingClientRect();
  const x = mouseX - rect.left;
  const y = mouseY - rect.top;

  if (mouseX == 0 && mouseY == 0) return; // at startup when the page automatically scrolls to saved position

  lineX = round(x / rect.width * scaleMaxH);
  lineY = round(y / rect.height * scaleMaxV); 

  crosshair.setAttribute("d", "M 0 " + lineY + " h "+ scaleMaxH + " M " + lineX + " 0 v " + scaleMaxV);

  timeAtCursor = x / rect.width * maxH;
  valuesIndex = lineX;
  indexTimeAtCursor = all_values[valuesIndex][0]; 

  // find the exact index
  i = 0;
  if (timeAtCursor < indexTimeAtCursor) {
    diff = indexTimeAtCursor - timeAtCursor; 
    for (i = valuesIndex - 1; i >= 0; i--) {
      newDiff = all_values[i][0] - timeAtCursor;
      if (Math.abs(newDiff) > diff) {
        i++;
        break;
      }
      diff = newDiff;
    }
    // the first element in the array is the closest, loop finished with subtracting -1 from i.
    if (i == -1) i = 0;
    valuesIndex = i
  }
  else if (timeAtCursor > indexTimeAtCursor) {
    diff = timeAtCursor - indexTimeAtCursor; 
    for (i = valuesIndex + 1; i <= all_values.length - 1; i++) {
      newDiff = timeAtCursor - all_values[i][0];
      if (Math.abs(newDiff) > diff) {
        i--;
        break;
      }
      diff = newDiff;
    }
    valuesIndex = i
  }

  indexTimeAtCursor = all_values[valuesIndex][0]

  voltageAtCursor = (maxV - y / rect.height * (maxV - minV));
  
  infoBox.innerHTML = voltageAtCursor.toFixed(3) + " V<br />" + indexTimeAtCursor.toLocaleString('en-US') + " &micro;s<br />" +
  "<span class=\"ch1\">" + round(all_values[valuesIndex][1], 3) + "</span><br />" + 
  "<span class=\"ch2\">" + round(all_values[valuesIndex][2], 3) + "</span><br />" +
  "<span class=\"ch3\">" + round(all_values[valuesIndex][3], 3) + "</span><br />" + 
  "<span class=\"ch4\">" + round(all_values[valuesIndex][4], 3) + "</span>";

  infoBox.style.display = "block";
  if (mouseX < infoBox.offsetWidth + 10) {
    infoBox.style.left = (mouseX + 10) + "px";
  }
  else {
    infoBox.style.left = (mouseX - (infoBox.offsetWidth + 10)) + "px";
  }

  if (y < infoBox.offsetHeight + 10) {
    infoBox.style.top = (mouseY + 10) + "px";
  }
  else {
    infoBox.style.top = (mouseY - (infoBox.offsetHeight + 10)) + "px";
  }
}

chartDiv.addEventListener("scroll", (event) => {
  if (resizeMouseLeftDown) {
    return;
  }
  clearTimeout(timeoutID);
  if (firstLoad) {
    firstLoad = false;
  }
  else {
    timeoutID = setTimeout(updateScroll, 200);
  }
});

function updateScroll() {
  updateCrosshair()
  settingsFile.midScroll = (chartDiv.scrollLeft + chartDiv.clientWidth / 2) / chart.clientWidth;
  saveSettings();
}

/*logFiles.addEventListener('change', (event) => {
  settingsFile.logFile = event.target.value;
  saveSettings();

  fetch('new logs/' + settingsFile.logFile)
  .then(response => response.text())
  .then(data => updateGraph(data));
  loadDescription();
  updateButton.className = "activeButton";
  updateButton.disabled = false;
});*/

document.body.addEventListener('mousedown', (event) => {
  if (event.target == resizeColumn && event.button == 0) {
    resizeMouseLeftDown = true;
    // hide crosshair, so it does not get misplaced
    document.getElementById("crosshair").setAttribute("d", "");
    infoBox.style.display = "none";
    // disabled the drag-and-drop behaviour
    event.preventDefault();
  }
});

document.body.addEventListener('mousemove', (event) => {
  if (resizeMouseLeftDown) {
    diffX = event.clientX - resizeMouseX;
    settingsFile.listColumnWidth -= diffX;
    if (settingsFile.listColumnWidth < 100) {
      settingsFile.listColumnWidth = 100;
    }
    logFilesFull.style.width = settingsFile.listColumnWidth + "px";
    
    clearTimeout(timeout3ID);
    timeout3ID = setTimeout(saveSettings, 200);

    // blocks listbox selected item change
    event.preventDefault();
  }
  resizeMouseX = event.clientX;
});

// Finish zoom rectangle. When the mouse leaves the browser, it just disappears without zooming. Letting the zoom rectangle remain when the mouse leaves the chart but still in the browser allows for zooming to minimum/maximum voltage.
document.body.addEventListener('mouseup', (event) => {
  resizeMouseLeftDown = false;
  if (event.button == 0 && mouseLeftDown) {
    mouseLeftDown = false;
    zoomRect.setAttribute("d", "");
    infoBox.style.display = "none";

    if (!(mouseX == startMouseX || mouseY == startMouseY)) {
      finishZoomRect();
    }
  }
});

document.body.addEventListener('mouseleave', (event) => {
  resizeMouseLeftDown = false;
  mouseLeftDown = false;
  zoomRect.setAttribute("d", "");
  // infoBox.style.display = "none";
});

logFilesFull.addEventListener('change', (event) => {
  settingsFile.logFile = event.target.value;
  clearTimeout(timeout2ID);
  timeout2ID = setTimeout(getData, 300);
});

function getData() {
  saveSettings();

  fetch('new logs/' + settingsFile.logFile)
  .then(response => response.text())
  .then(data => updateGraph(data));
  loadDescription();
  updateButton.className = "activeButton";
  updateButton.disabled = false;
}

description.addEventListener('input', (event) => {
  updateButton.className = "activeButton";
  updateButton.disabled = false;
});

description.addEventListener("keydown", (e) => {
  if (e.key === "Enter") {
    updateDescription();
    e.preventDefault();
  }
});

updateButton.addEventListener('click', (event) => {
  updateDescription();
});

function loadDescription() {
  found = false
  fetch('new logs/log descriptions.txt?v=' + Date.now())
  .then(response => response.text())
  .then(data => {
    descriptionFile = data;
    lines = data.split("\n");
    lines.forEach((line) => {
      pos = line.indexOf(":");
      if (pos != -1) {
        num = line.substring(0, pos);
        if (settingsFile.logFile == "voltage4_log_" + num + ".txt") {
          info = line.substring(pos + 1).trim();
          description.value = info;
          found = true;
        }
      }
    });
  });
  if (!found) {
    description.value = "";
  }
}

function updateDescription() {
  fetch('ChartViewer.php?a=2', {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json'
    },
    body: JSON.stringify({[settingsFile.logFile]: description.value})
  })
  .then(response => response.text())
  .then(result => {
    if (result == "OK") {
      console.log("Updated.");
      updateButton.className = "passiveButton";
      updateButton.disabled = true;
      num = logFilesFull.options[logFilesFull.selectedIndex].text.split(":")[0]
      logFilesFull.options[logFilesFull.selectedIndex].text = num + ": " + description.value;
    }
  })
  .catch(error => console.error('Error:', error));
}

function updateGraph(data) {
  limit = 100000;
  scaleMaxH = 0; // chart pixel size at 1x zoom, the number of records in a file
  scaleMaxV = chartDiv.clientHeight; // height excluding scrollbar
  maxH = 0;
  minV = 100;
  maxV = 0;

  all_values = [];
  arr = data.split("\n");
  arr.pop();
  arr.forEach((line) => {
    values = line.split(" ");
    all_values.push([parseInt(values[0]), parseFloat(values[1]), parseFloat(values[2]), parseFloat(values[3]), parseFloat(values[4])]);
    timestamp = values[0]
    voltage1 = values[1]
    voltage2 = values[2]
    voltage3 = values[3]
    voltage4 = values[4]
    if (voltage1 > maxV)
        maxV = voltage1
    if (voltage1 < minV)
        minV = voltage1
    if (voltage2 > maxV)
        maxV = voltage2
    if (voltage2 < minV)
        minV = voltage2
    if (voltage3 > maxV)
        maxV = voltage3
    if (voltage3 < minV)
        minV = voltage3
    if (voltage4 > maxV)
        maxV = voltage4
    if (voltage4 < minV)
        minV = voltage4
  });

  maxH = parseInt(timestamp);
  scaleMaxH = arr.length - 1;

  console.log("Time: " + maxH + " MinV: " + minV + " MaxV: " + maxV + " count: " + (arr.length - 1));

  path4 = path3 = path2 = path1 = "";

  c = 0;
  found = false;
  arr.forEach((line) => {
    c++;
    if (c < limit) {
      values = line.split(" ");
      timestamp = values[0];
      voltage1 = values[1];
      voltage2 = values[2];
      voltage3 = values[3];
      voltage4 = values[4];

      m1 = round(scaleMaxH*timestamp/maxH, 3);
      m2 = round(scaleMaxV*(maxV-voltage1)/(maxV-minV), 3);
      m3 = round(scaleMaxV*(maxV-voltage2)/(maxV-minV), 3);
      m4 = round(scaleMaxV*(maxV-voltage3)/(maxV-minV), 3);
      m5 = round(scaleMaxV*(maxV-voltage4)/(maxV-minV), 3);

      path1 += "L " + m1 + " " + m2 + " ";
      path2 += "L " + m1 + " " + m3 + " ";
      path3 += "L " + m1 + " " + m4 + " ";
      path4 += "L " + m1 + " " + m5 + " ";
    }
  });

  path1 = "M" + path1.substring(1);
  path2 = "M" + path2.substring(1);
  path3 = "M" + path3.substring(1);
  path4 = "M" + path4.substring(1);

  chart.setAttribute("viewBox", "0 0 " + scaleMaxH + " " + scaleMaxV);
  chart.style.width = scaleMaxH + "px";
  chart.style.height = scaleMaxV + "px";
  document.getElementById("line1").setAttribute("d", path1);
  document.getElementById("line2").setAttribute("d", path2);
  document.getElementById("line3").setAttribute("d", path3);
  document.getElementById("line4").setAttribute("d", path4);

  updateViewBox();
}

function updateViewBox() {
  if (scaleMaxH * settingsFile.zoomState < chartDiv.clientWidth) {
    settingsFile.zoomState = chartDiv.clientWidth / scaleMaxH
    zoomLevel.value = round(settingsFile.zoomState, 3);
    settingsFile.midScroll = 0.5;
  }
  chart.style.width = scaleMaxH * settingsFile.zoomState + "px";  
  chartDiv.scrollLeft = settingsFile.midScroll * chart.clientWidth - chartDiv.clientWidth / 2;

  // scroll event will not fire
  if (firstLoad && chartDiv.scrollLeft == 0) {
    firstLoad = false;
  }
}

document.addEventListener("keydown", (e) => {
  if (e.target != zoomLevel && e.target != logFilesFull && e.target != description) {
    if (e.key === "ArrowDown") {
      if (scaleMaxH * settingsFile.zoomState / zoomFactor >= chartDiv.clientWidth)  {
        settingsFile.zoomState /= zoomFactor;
      }
      else {
        settingsFile.zoomState = chartDiv.clientWidth / scaleMaxH
        settingsFile.midScroll = 0.5;
      }
      saveSettings();
      updateViewBox();          
      e.preventDefault();
    }
    else if (e.key === "ArrowUp") {
      settingsFile.zoomState *= zoomFactor;
      if (settingsFile.zoomState > MAX_ZOOM) {
        settingsFile.zoomState = MAX_ZOOM;
      }
      saveSettings();  
      updateViewBox();
      e.preventDefault();
    }
    else if (e.key == "ArrowLeft") {
      chartDiv.scrollLeft -= moveFactor;
    }
    else if (e.key == "ArrowRight") {
      chartDiv.scrollLeft += moveFactor;
    }
    zoomLevel.value = round(settingsFile.zoomState, 3);; 
  }
  else {
    if (e.key === "Escape") {
      e.target.blur();
    }
    if (e.key === "Delete" && e.target == logFilesFull) {
      console.log("Delete pressed " + logFilesFull.value)
      fetch('ChartViewer.php?a=3', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json'
        },
        body: JSON.stringify({File: settingsFile.logFile})
      })
      .then(response => response.text())
      .then(result => {
        if (result == "OK") {
          console.log("Deleted.");
          oldIndex = logFilesFull.selectedIndex;
          logFilesFull.options[logFilesFull.selectedIndex].remove(); // index becomes -1
          if (logFilesFull.options.length > oldIndex) {
            logFilesFull.options.selectedIndex = oldIndex;
          }
          else {
            logFilesFull.options.selectedIndex = 0;
          }
          settingsFile.logFile = logFilesFull.options[logFilesFull.selectedIndex].value;
          getData();
        }
      })
      .catch(error => console.error('Error:', error));
    }
  }
});  

chart.addEventListener("wheel", (event) => {
  let origZoomState = settingsFile.zoomState;
  if (event.deltaY < 0) {
    settingsFile.zoomState *= zoomFactor;
    if (settingsFile.zoomState > MAX_ZOOM) {
      settingsFile.zoomState = MAX_ZOOM;
    }
  }
  else {
    if (scaleMaxH * settingsFile.zoomState / zoomFactor >= chartDiv.clientWidth)  {
        settingsFile.zoomState /= zoomFactor;
    }
    else {
      settingsFile.zoomState = chartDiv.clientWidth / scaleMaxH;
      settingsFile.midScroll = 0.5;
    }
  }
  posAtCursor = (chartDiv.scrollLeft + mouseX) / chart.clientWidth;
  // graph under the cursor needs to remain at the same place. The distance between midScroll and cursor position needs to be increased or decreased proportionally with the zoom change.
  settingsFile.midScroll = posAtCursor + (settingsFile.midScroll - posAtCursor) / settingsFile.zoomState * origZoomState;
  zoomLevel.value = round(settingsFile.zoomState, 3);
  saveSettings();  
  updateViewBox();
  event.preventDefault(); // prevent vertical scrolling
});

zoomLevel.addEventListener("keydown", (e) => {
  if (e.key === "Enter") {
    settingsFile.zoomState = zoomLevel.value;
    saveSettings();  
    updateViewBox();
  }
  if (e.key === "Escape") {
    zoomLevel.blur();
  }
});

eventSource.onmessage = function(event) {
    const data = JSON.parse(event.data);

    if (data.status === 'new_data_available') {
        console.log("Received: " + data.filePath);
        num = data.filePath.replace("voltage4_log_", "").replace(".txt", "")
        const newOption = new Option(num + ": ", data.filePath);
        logFilesFull.insertBefore(newOption, logFilesFull.firstChild);
        logFilesFull.selectedIndex = 0;
        settingsFile.logFile = logFilesFull.options[logFilesFull.selectedIndex].value;
        getData();
    }
    else if (data.status === 'idle') {
        // console.log("Checked for updates: No new data yet.");
    }
};

function showHideLine(num, event) {
  var line = document.getElementById("line" + num);
  var button = document.getElementById("button" + num);
  if (line.style.display === 'none') {
    line.style.display = 'inline'
    button.style.opacity = 1;
  }
  else {
    line.style.display = 'none'
    button.style.opacity = 0.5;
  }

  switch (num) {
    case 1:
      settingsFile.yellow = !settingsFile.yellow;
      break;
    case 2:
      settingsFile.green = !settingsFile.green;
      break;
    case 3:
      settingsFile.blue = !settingsFile.blue;
      break;
    case 4:
      settingsFile.red = !settingsFile.red;
      break;
  }

  saveSettings();
}

function saveSettings() {
  fetch('ChartViewer.php?a=1', {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json'
    },
    body: JSON.stringify(settingsFile)
  })
  .then(response => response.text())
  .then(result => {
    // console.log(result)
  })
  .catch(error => console.error('Error:', error));
}

function round(num, precision = 0) {
  return Math.round(num * Math.pow(10, precision)) / Math.pow(10, precision);
}