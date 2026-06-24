from PyQt5.QtWidgets import QApplication, QWidget, QVBoxLayout, QHBoxLayout, QPushButton, QLabel, QLineEdit, QSizePolicy, QMessageBox, QListWidget, QAbstractItemView, QTextEdit, QScrollArea
from PyQt5.QtCore import QTimer, Qt, QSize, QPoint, QRect, QPointF, QEvent
from PyQt5.QtGui import QPixmap, QImage, QPainter, QPen, QColor, QFont, QCursor, QPolygonF, QFontMetrics
import spidev
import RPi.GPIO as GPIO
import time
import struct
import sys, random
import tty
import termios
import threading
import re
import os
import gc
import math
import numpy as np
import struct, sys
import threading, subprocess, queue

'''
AD7606 - Raspberry Pi connection:
GND - pin 9
%V - pin 2
OS1 - pin 6
OS0 - pin 6
RAGE - pin 6
OS2 - pin 6
CVB - pin 12
CVA - pin 12
R0 - pin 23
RST - pin 11
BUSY - pin 22
CS - pin 24
VIO - pin 1
DB7 - pin 21
DB15 - pin 9

3.7 V battery power consumption:
Pi startup max. 0.83 - 0.84 A
Idle 0.52 A
Samples collecting 0.58 A
Writing to file 0.69 A
Shutdown max 0.79 - 0.85 A
Standby 0.17 A
Switched off 41.6 uA / 2.2 mA (depending on multimeter)
Startup with screen: max 2.04 - 2.10
Idle 1.76
Shutdown 1.89
Power suppliy voltage when screen connected 5.12 / 5V: 4.76
Charging current 2.38 A

3.7 -> 5 V boost converter:
Input 163 mV at 50 mOhm, 3.28V; 3.26A * 3.28V = 10.69 W 
Output: 5.06 V at 2.7 ohm, 5.16V; 1.87A * 5.16V = 9.65 W
Efficiency: 90.3 %
'''

os.environ["QT_QPA_PLATFORM"] = "xcb"
# structured dtype: 8 big-endian int16 entries followed by 1 little-endian uint64
frame_dt = np.dtype([
    ("adc",  ">i2", 4),   # 8 × big-endian int16 -> 16 bytes
    ("ts",   "<u4")       # little-endian unsigned 32-bit -> 4 bytes
])

combined_dt = np.dtype([
    ("timestamp_us", "<u4"),
    ("ch1", np.float32),
    ("ch2", np.float32),
    ("ch3", np.float32),
    ("ch4", np.float32),
])

def formatN(number):
    return f"{number:,}"# .replace(",", ".")

def readSettings():
    global settingsArr, settings_timeLimit, settings_measureChannels1, settings_measureChannels2, settings_measureChannels3, settings_measureChannels4, settings_trigger, settings_triggerChannel, settings_risingEdge, settings_triggerThreshold, settings_offsetPercent, settings_countSwitches, settings_switchChannel, settings_switchLow, settings_switchHigh, settings_liveScaleMin, settings_liveScaleMax, settings_liveScaleWidth, settings_ch1ground, settings_ch2ground, settings_ch3ground, settings_ch4ground    

    settings = ""
    newSettings = ""
    with open(f"/home/fodorbalint/Documents/graph settings.txt", "r") as file:
        settings = file.read()
        settingsArr = settings.split("\n")
        for line in settingsArr:               
            pairs = line.strip().split(" = ")

            if pairs[0] == "timeLimit":
                settings_timeLimit = float(pairs[1])
                if settings_timeLimit > 30:
                    settings_timeLimit = float(30)
                    newSettings += pairs[0] + " = " + float_to_str(settings_timeLimit) + "\n"
                elif settings_timeLimit < 0.1:
                    settings_timeLimit = 0.1
                    newSettings += pairs[0] + " = " + float_to_str(settings_timeLimit) + "\n"
                else:
                    newSettings += line + "\n"
            elif pairs[0] == "measureChannels":
                arr = pairs[1].split(",")
                if arr[0] == "1":
                    settings_measureChannels1 = True
                else:
                    settings_measureChannels1 = False
                if arr[1] == "1":
                    settings_measureChannels2 = True
                else:
                    settings_measureChannels2 = False
                if arr[2] == "1":
                    settings_measureChannels3 = True
                else:
                    settings_measureChannels3 = False
                if arr[3] == "1":
                    settings_measureChannels4 = True
                else:
                    settings_measureChannels4 = False
                newSettings += line + "\n"
            elif pairs[0] == "trigger":
                if pairs[1] == "False" or pairs[1] == "false":
                    settings_trigger = False
                else:
                    settings_trigger = True
                newSettings += line + "\n"
            elif pairs[0] == "triggerChannel":
                settings_triggerChannel = int(pairs[1])
                newSettings += line + "\n"
            elif pairs[0] == "risingEdge":
                if pairs[1] == "False" or pairs[1] == "false":
                    settings_risingEdge = False
                else:
                    settings_risingEdge = True
                newSettings += line + "\n"
            elif pairs[0] == "triggerThreshold":
                settings_triggerThreshold = float(pairs[1])
                newSettings += line + "\n"
            elif pairs[0] == "offsetPercent":
                settings_offsetPercent = int(pairs[1])
                newSettings += line + "\n"
            elif pairs[0] == "countSwitches":
                if pairs[1] == "False" or pairs[1] == "false":
                    settings_countSwitches = False
                else:
                    settings_countSwitches = True
                newSettings += line + "\n"
            elif pairs[0] == "switchChannel":
                settings_switchChannel = int(pairs[1])
                newSettings += line + "\n"
            elif pairs[0] == "switchLow":
                settings_switchLow = float(pairs[1])
                newSettings += line + "\n"
            elif pairs[0] == "switchHigh":
                settings_switchHigh = float(pairs[1])
                newSettings += line + "\n"  
            elif pairs[0] == "liveScaleMin":
                settings_liveScaleMin = float(pairs[1])
                if settings_liveScaleMin < 0:
                    settings_liveScaleMin = float(0)
                    newSettings += pairs[0] + " = " + float_to_str(settings_liveScaleMin) + "\n"
                elif settings_liveScaleMin > 5:
                    settings_liveScaleMin = float(5)
                    newSettings += pairs[0] + " = " + float_to_str(settings_liveScaleMin) + "\n"
                else:
                    newSettings += line + "\n"
            elif pairs[0] == "liveScaleMax":
                settings_liveScaleMax = float(pairs[1])
                if settings_liveScaleMax < 0:
                    settings_liveScaleMax = float(0)
                    newSettings += pairs[0] + " = " + float_to_str(settings_liveScaleMax) + "\n"
                elif settings_liveScaleMax > 5:
                    settings_liveScaleMax = float(5)
                    newSettings += pairs[0] + " = " + float_to_str(settings_liveScaleMax) + "\n"
                else:
                    newSettings += line + "\n"
            elif pairs[0] == "liveScaleWidth":
                settings_liveScaleWidth = float(pairs[1])
                if settings_liveScaleWidth < 0:
                    settings_liveScaleWidth = float(0)
                    newSettings += pairs[0] + " = " + float_to_str(settings_liveScaleWidth) + "\n"
                elif settings_liveScaleWidth > 10:
                    settings_liveScaleWidth = float(5)
                    newSettings += pairs[0] + " = " + float_to_str(settings_liveScaleWidth) + "\n"
                else:
                    newSettings += line + "\n" 
            elif pairs[0] == "ch1ground":
                settings_ch1ground = int(pairs[1])
                newSettings += line + "\n"
            elif pairs[0] == "ch2ground":
                settings_ch2ground = int(pairs[1])
                newSettings += line + "\n"
            elif pairs[0] == "ch3ground":
                settings_ch3ground = int(pairs[1])
                newSettings += line + "\n"
            elif pairs[0] == "ch4ground":
                settings_ch4ground = int(pairs[1])
                newSettings += line + "\n"

    newSettings = newSettings[:-1]
    if newSettings != settings:
        print (f"Settings corrected")
        with open(f"/home/fodorbalint/Documents/graph settings.txt", "w") as file:
            file.write(newSettings)

def saveSettings(name):    
    for i in range (0, len(settingsArr)):
        line = settingsArr[i]
        pairs = line.split(" = ")
        if pairs[0] == name:
            if name == "timeLimit":
                settingsArr[i] = pairs[0] + " = " + float_to_str(settings_timeLimit)
            elif name == "measureChannels":
                settingsArr[i] = pairs[0] + " = " + str(int(settings_measureChannels1)) + "," + str(int(settings_measureChannels2)) + "," + str(int(settings_measureChannels3)) + "," + str(int(settings_measureChannels4))
            elif name == "trigger":
                settingsArr[i] = pairs[0] + " = " + str(settings_trigger)
            elif name == "triggerChannel":
                settingsArr[i] = pairs[0] + " = " + str(settings_triggerChannel)
            elif name == "risingEdge":
                settingsArr[i] = pairs[0] + " = " + str(settings_risingEdge)
            elif name == "triggerThreshold":
                settingsArr[i] = pairs[0] + " = " + float_to_str(settings_triggerThreshold)
            elif name == "offsetPercent":
                settingsArr[i] = pairs[0] + " = " + str(settings_offsetPercent)
            elif name == "countSwitches":
                settingsArr[i] = pairs[0] + " = " + str(settings_countSwitches)
            elif name == "switchChannel":
                settingsArr[i] = pairs[0] + " = " + str(settings_switchChannel)
            elif name == "switchLow":
                settingsArr[i] = pairs[0] + " = " + str(settings_switchLow)
            elif name == "switchHigh":
                settingsArr[i] = pairs[0] + " = " + str(settings_switchHigh)  
            elif name == "liveScaleMin":
                settingsArr[i] = pairs[0] + " = " + float_to_str(settings_liveScaleMin) 
            elif name == "liveScaleMax":
                settingsArr[i] = pairs[0] + " = " + float_to_str(settings_liveScaleMax) 
            elif name == "liveScaleWidth":
                settingsArr[i] = pairs[0] + " = " + float_to_str(settings_liveScaleWidth) 
            elif name == "ch1ground":
                settingsArr[i] = pairs[0] + " = " + str(settings_ch1ground)
            elif name == "ch2ground":
                settingsArr[i] = pairs[0] + " = " + str(settings_ch2ground)
            elif name == "ch3ground":
                settingsArr[i] = pairs[0] + " = " + str(settings_ch3ground)
            elif name == "ch4ground":
                settingsArr[i] = pairs[0] + " = " + str(settings_ch4ground)       

    fileText = "\n".join(settingsArr)
    with open(f"/home/fodorbalint/Documents/graph settings.txt", "w") as file:
        file.write(fileText)

def float_to_str(f):
    if f.is_integer():
        return str(int(f))
    else:
        return str(f)

class SettingsWindow(QWidget): 
    def __init__(self):
        super().__init__()

        self.setWindowTitle("Settings")
        self.setWindowFlags(Qt.FramelessWindowHint)
        self.setAttribute(Qt.WA_OpaquePaintEvent)
        self.setAttribute(Qt.WA_NoSystemBackground)
        # self.showMaximized()
        QTimer.singleShot(0, self.force_fullscreen_fix)

        layout1 = QHBoxLayout()
        layout1.setContentsMargins(0, 0, 0, 0); 
        layout1.setSpacing(5) 
        layout1.setAlignment(Qt.AlignLeft)

        self.label1 = QLabel("Time limit:")
        self.label1.setStyleSheet("color: white")
        self.label1.setMaximumWidth(140)
        self.label1.setMinimumWidth(140)
        self.textbox1 = QLineEdit()
        self.textbox1.setStyleSheet("font-weight: bold")
        self.textbox1.setText(float_to_str(settings_timeLimit))
        self.textbox1.setMaximumWidth(50)
        self.button11 = QPushButton("--")
        self.button11.setStyleSheet("font-size: 20px; font-weight: bold")
        self.button11.setMaximumSize(QSize(30, 30))
        self.button12 = QPushButton("-")
        self.button12.setStyleSheet("font-size: 20px; font-weight: bold")
        self.button12.setMaximumSize(QSize(30, 30))
        self.button13 = QPushButton("+")
        self.button13.setStyleSheet("font-size: 20px; font-weight: bold")
        self.button13.setMaximumSize(QSize(30, 30))
        self.button14 = QPushButton("++")
        self.button14.setStyleSheet("font-size: 20px; font-weight: bold")
        self.button14.setMaximumSize(QSize(30, 30))

        layout1.addWidget(self.label1)
        layout1.addWidget(self.textbox1)
        layout1.addWidget(self.button11)
        layout1.addWidget(self.button12)
        layout1.addWidget(self.button13)
        layout1.addWidget(self.button14)        

        layout2 = QHBoxLayout()
        layout2.setContentsMargins(0, 0, 0, 0); 
        layout2.setSpacing(5) 
        layout2.setAlignment(Qt.AlignLeft)

        self.label2 = QLabel("Show channels:")
        self.label2.setStyleSheet("color: white")
        self.label2.setMaximumWidth(140)
        self.label2.setMinimumWidth(140)
        self.button21 = QPushButton("1")     
        if settings_measureChannels1: self.button21.setStyleSheet("font-size: 20px; font-weight: bold; background-color: lightgreen")
        else: self.button21.setStyleSheet("font-size: 20px; font-weight: bold; background-color: gray")
        self.button21.setMaximumSize(QSize(30, 30))
        self.button22 = QPushButton("2")
        if settings_measureChannels2: self.button22.setStyleSheet("font-size: 20px; font-weight: bold; background-color: lightgreen")
        else: self.button22.setStyleSheet("font-size: 20px; font-weight: bold; background-color: gray")
        self.button22.setMaximumSize(QSize(30, 30))
        self.button23 = QPushButton("3")
        if settings_measureChannels3: self.button23.setStyleSheet("font-size: 20px; font-weight: bold; background-color: lightgreen")
        else: self.button23.setStyleSheet("font-size: 20px; font-weight: bold; background-color: gray")
        self.button23.setMaximumSize(QSize(30, 30))
        self.button24 = QPushButton("4")
        if settings_measureChannels4: self.button24.setStyleSheet("font-size: 20px; font-weight: bold; background-color: lightgreen")
        else: self.button24.setStyleSheet("font-size: 20px; font-weight: bold; background-color: gray")
        self.button24.setMaximumSize(QSize(30, 30))

        layout2.addWidget(self.label2)
        layout2.addWidget(self.button21)
        layout2.addWidget(self.button22)
        layout2.addWidget(self.button23)
        layout2.addWidget(self.button24)

        layout3 = QHBoxLayout()
        layout3.setContentsMargins(0, 0, 0, 0); 
        layout3.setSpacing(5) 
        layout3.setAlignment(Qt.AlignLeft)

        self.label3 = QLabel("Trigger:")
        self.label3.setStyleSheet("QLabel { color: white; }")
        self.label3.setMaximumWidth(140)
        self.label3.setMinimumWidth(140)
        self.button3 = QPushButton()
        if settings_trigger:
            self.button3.setText("On")
            self.button3.setStyleSheet("font-weight: bold; background-color: lightgreen")
        else:
            self.button3.setText("Off")
            self.button3.setStyleSheet("font-weight: bold; background-color: gray")
        self.button3.setMaximumWidth(50)

        layout3.addWidget(self.label3)
        layout3.addWidget(self.button3)        

        layout4 = QHBoxLayout()
        layout4.setContentsMargins(0, 0, 0, 0); 
        layout4.setSpacing(5) 
        layout4.setAlignment(Qt.AlignLeft)

        self.label4 = QLabel("Channel:")
        self.label4.setStyleSheet("QLabel { color: white; }")
        self.label4.setMaximumWidth(140)
        self.label4.setMinimumWidth(140)
        self.button41 = QPushButton("1")     
        if settings_triggerChannel == 1: self.button41.setStyleSheet("font-size: 20px; font-weight: bold; background-color: lightgreen")
        else: self.button41.setStyleSheet("font-size: 20px; font-weight: bold; background-color: gray")
        self.button41.setMaximumSize(QSize(30, 30))
        self.button42 = QPushButton("2")
        if settings_triggerChannel == 2: self.button42.setStyleSheet("font-size: 20px; font-weight: bold; background-color: lightgreen")
        else: self.button42.setStyleSheet("font-size: 20px; font-weight: bold; background-color: gray")
        self.button42.setMaximumSize(QSize(30, 30))
        self.button43 = QPushButton("3")
        if settings_triggerChannel == 3: self.button43.setStyleSheet("font-size: 20px; font-weight: bold; background-color: lightgreen")
        else: self.button43.setStyleSheet("font-size: 20px; font-weight: bold; background-color: gray")
        self.button43.setMaximumSize(QSize(30, 30))
        self.button44 = QPushButton("4")
        if settings_triggerChannel == 4: self.button44.setStyleSheet("font-size: 20px; font-weight: bold; background-color: lightgreen")
        else: self.button44.setStyleSheet("font-size: 20px; font-weight: bold; background-color: gray")
        self.button44.setMaximumSize(QSize(30, 30))

        layout4.addWidget(self.label4)
        layout4.addWidget(self.button41)
        layout4.addWidget(self.button42)
        layout4.addWidget(self.button43)
        layout4.addWidget(self.button44)

        layout5 = QHBoxLayout()
        layout5.setContentsMargins(0, 0, 0, 0); 
        layout5.setSpacing(5) 
        layout5.setAlignment(Qt.AlignLeft)

        self.button51 = QPushButton("Rising")
        self.button52 = QPushButton("Falling")
        if settings_risingEdge:
            self.button51.setStyleSheet("font-weight: bold; background-color: lightgreen")
            self.button52.setStyleSheet("font-weight: bold; background-color: gray")
        else:
            self.button51.setStyleSheet("font-weight: bold; background-color: gray")
            self.button52.setStyleSheet("font-weight: bold; background-color: lightgreen")
        self.button51.setMaximumWidth(70)
        self.button52.setMaximumWidth(70)                

        layout5.addWidget(self.button51)
        layout5.addWidget(self.button52)

        layout6 = QHBoxLayout()
        layout6.setContentsMargins(0, 0, 0, 0); 
        layout6.setSpacing(5) 
        layout6.setAlignment(Qt.AlignLeft)

        self.label6 = QLabel("Threshold:")
        self.label6.setStyleSheet("color: white")
        self.label6.setMaximumWidth(140)
        self.label6.setMinimumWidth(140)
        self.textbox6 = QLineEdit()
        self.textbox6.setStyleSheet("font-weight: bold")
        self.textbox6.setText(float_to_str(settings_triggerThreshold))
        self.textbox6.setMaximumWidth(50)
        self.button61 = QPushButton("--")
        self.button61.setStyleSheet("font-size: 20px; font-weight: bold")
        self.button61.setMaximumSize(QSize(30, 30))
        self.button62 = QPushButton("-")
        self.button62.setStyleSheet("font-size: 20px; font-weight: bold")
        self.button62.setMaximumSize(QSize(30, 30))
        self.button63 = QPushButton("+")
        self.button63.setStyleSheet("font-size: 20px; font-weight: bold")
        self.button63.setMaximumSize(QSize(30, 30))
        self.button64 = QPushButton("++")
        self.button64.setStyleSheet("font-size: 20px; font-weight: bold")
        self.button64.setMaximumSize(QSize(30, 30))

        layout6.addWidget(self.label6)
        layout6.addWidget(self.textbox6)
        layout6.addWidget(self.button61)
        layout6.addWidget(self.button62)
        layout6.addWidget(self.button63)
        layout6.addWidget(self.button64)  

        layout7 = QHBoxLayout()
        layout7.setContentsMargins(0, 0, 0, 0); 
        layout7.setSpacing(5) 
        layout7.setAlignment(Qt.AlignLeft)

        self.label7 = QLabel("Pre-trigger percent:")
        self.label7.setStyleSheet("color: white")
        self.label7.setMaximumWidth(140)
        self.label7.setMinimumWidth(140)
        self.textbox7 = QLineEdit()
        self.textbox7.setStyleSheet("font-weight: bold")
        self.textbox7.setText(str(settings_offsetPercent))
        self.textbox7.setMaximumWidth(50)
        self.button71 = QPushButton("-")
        self.button71.setStyleSheet("font-size: 20px; font-weight: bold")
        self.button71.setMaximumSize(QSize(30, 30))
        self.button72 = QPushButton("+")
        self.button72.setStyleSheet("font-size: 20px; font-weight: bold")
        self.button72.setMaximumSize(QSize(30, 30))

        layout7.addWidget(self.label7)
        layout7.addWidget(self.textbox7)
        layout7.addWidget(self.button71)
        layout7.addWidget(self.button72)

        layout8 = QHBoxLayout()
        layout8.setContentsMargins(0, 0, 0, 0); 
        layout8.setSpacing(5) 
        layout8.setAlignment(Qt.AlignLeft)

        self.label8 = QLabel("Count switches:")
        self.label8.setStyleSheet("QLabel { color: white; }")
        self.label8.setMaximumWidth(140)
        self.label8.setMinimumWidth(140)
        self.button8 = QPushButton()
        if settings_countSwitches:
            self.button8.setText("On")
            self.button8.setStyleSheet("font-weight: bold; background-color: lightgreen")
        else:
            self.button8.setText("Off")
            self.button8.setStyleSheet("font-weight: bold; background-color: gray")
        self.button8.setMaximumWidth(50)

        layout8.addWidget(self.label8)
        layout8.addWidget(self.button8)

        layout9 = QHBoxLayout()
        layout9.setContentsMargins(0, 0, 0, 0); 
        layout9.setSpacing(5) 
        layout9.setAlignment(Qt.AlignLeft)

        self.label9 = QLabel("Channel:")
        self.label9.setStyleSheet("QLabel { color: white; }")
        self.label9.setMaximumWidth(140)
        self.label9.setMinimumWidth(140)
        self.button91 = QPushButton("1")     
        if settings_switchChannel == 1: self.button91.setStyleSheet("font-size: 20px; font-weight: bold; background-color: lightgreen")
        else: self.button91.setStyleSheet("font-size: 20px; font-weight: bold; background-color: gray")
        self.button91.setMaximumSize(QSize(30, 30))
        self.button92 = QPushButton("2")
        if settings_switchChannel == 2: self.button92.setStyleSheet("font-size: 20px; font-weight: bold; background-color: lightgreen")
        else: self.button92.setStyleSheet("font-size: 20px; font-weight: bold; background-color: gray")
        self.button92.setMaximumSize(QSize(30, 30))
        self.button93 = QPushButton("3")
        if settings_switchChannel == 3: self.button93.setStyleSheet("font-size: 20px; font-weight: bold; background-color: lightgreen")
        else: self.button93.setStyleSheet("font-size: 20px; font-weight: bold; background-color: gray")
        self.button93.setMaximumSize(QSize(30, 30))
        self.button94 = QPushButton("4")
        if settings_switchChannel == 4: self.button94.setStyleSheet("font-size: 20px; font-weight: bold; background-color: lightgreen")
        else: self.button94.setStyleSheet("font-size: 20px; font-weight: bold; background-color: gray")
        self.button94.setMaximumSize(QSize(30, 30))

        layout9.addWidget(self.label9)
        layout9.addWidget(self.button91)
        layout9.addWidget(self.button92)
        layout9.addWidget(self.button93)
        layout9.addWidget(self.button94)

        layoutA = QHBoxLayout()
        layoutA.setContentsMargins(0, 0, 0, 0); 
        layoutA.setSpacing(5) 
        layoutA.setAlignment(Qt.AlignLeft)        

        self.labelA = QLabel("Low threshold:")
        self.labelA.setStyleSheet("color: white")
        self.labelA.setMaximumWidth(140)
        self.labelA.setMinimumWidth(140)
        self.textboxA = QLineEdit()
        self.textboxA.setStyleSheet("font-weight: bold")
        self.textboxA.setText(float_to_str(settings_switchLow))
        self.textboxA.setMaximumWidth(60)
        self.buttonA1 = QPushButton("--")
        self.buttonA1.setStyleSheet("font-size: 20px; font-weight: bold")
        self.buttonA1.setMaximumSize(QSize(30, 30))
        self.buttonA2 = QPushButton("-")
        self.buttonA2.setStyleSheet("font-size: 20px; font-weight: bold")
        self.buttonA2.setMaximumSize(QSize(30, 30))
        self.buttonA3 = QPushButton("+")
        self.buttonA3.setStyleSheet("font-size: 20px; font-weight: bold")
        self.buttonA3.setMaximumSize(QSize(30, 30))
        self.buttonA4 = QPushButton("++")
        self.buttonA4.setStyleSheet("font-size: 20px; font-weight: bold")
        self.buttonA4.setMaximumSize(QSize(30, 30))

        layoutA.addWidget(self.labelA)
        layoutA.addWidget(self.textboxA)
        layoutA.addWidget(self.buttonA1)
        layoutA.addWidget(self.buttonA2)
        layoutA.addWidget(self.buttonA3)
        layoutA.addWidget(self.buttonA4) 

        layoutB = QHBoxLayout()
        layoutB.setContentsMargins(0, 0, 0, 0); 
        layoutB.setSpacing(5) 
        layoutB.setAlignment(Qt.AlignLeft)        

        self.labelB = QLabel("High threshold:")
        self.labelB.setStyleSheet("color: white")
        self.labelB.setMaximumWidth(140)
        self.labelB.setMinimumWidth(140)
        self.textboxB = QLineEdit()
        self.textboxB.setStyleSheet("font-weight: bold")
        self.textboxB.setText(float_to_str(settings_switchHigh))
        self.textboxB.setMaximumWidth(60)
        self.buttonB1 = QPushButton("--")
        self.buttonB1.setStyleSheet("font-size: 20px; font-weight: bold")
        self.buttonB1.setMaximumSize(QSize(30, 30))
        self.buttonB2 = QPushButton("-")
        self.buttonB2.setStyleSheet("font-size: 20px; font-weight: bold")
        self.buttonB2.setMaximumSize(QSize(30, 30))
        self.buttonB3 = QPushButton("+")
        self.buttonB3.setStyleSheet("font-size: 20px; font-weight: bold")
        self.buttonB3.setMaximumSize(QSize(30, 30))
        self.buttonB4 = QPushButton("++")
        self.buttonB4.setStyleSheet("font-size: 20px; font-weight: bold")
        self.buttonB4.setMaximumSize(QSize(30, 30))

        layoutB.addWidget(self.labelB)
        layoutB.addWidget(self.textboxB)
        layoutB.addWidget(self.buttonB1)
        layoutB.addWidget(self.buttonB2)
        layoutB.addWidget(self.buttonB3)
        layoutB.addWidget(self.buttonB4)

        layoutC = QHBoxLayout()
        layoutC.setContentsMargins(0, 0, 0, 0); 
        layoutC.setSpacing(5) 
        layoutC.setAlignment(Qt.AlignLeft)        

        self.labelC = QLabel("Live graph")
        self.labelC.setStyleSheet("color: white")
        self.labelC.setMaximumHeight(30)

        layoutC.addWidget(self.labelC)

        layoutD = QHBoxLayout()
        layoutD.setContentsMargins(0, 0, 0, 0); 
        layoutD.setSpacing(5) 
        layoutD.setAlignment(Qt.AlignLeft)        

        self.labelD = QLabel("Low limit:")
        self.labelD.setStyleSheet("color: white")
        self.labelD.setMaximumWidth(140)
        self.labelD.setMinimumWidth(140)
        self.textboxD = QLineEdit()
        self.textboxD.setStyleSheet("font-weight: bold")
        self.textboxD.setText(float_to_str(settings_liveScaleMin))
        self.textboxD.setMaximumWidth(50)
        self.buttonD1 = QPushButton("--")
        self.buttonD1.setStyleSheet("font-size: 20px; font-weight: bold")
        self.buttonD1.setMaximumSize(QSize(30, 30))
        self.buttonD2 = QPushButton("-")
        self.buttonD2.setStyleSheet("font-size: 20px; font-weight: bold")
        self.buttonD2.setMaximumSize(QSize(30, 30))
        self.buttonD3 = QPushButton("+")
        self.buttonD3.setStyleSheet("font-size: 20px; font-weight: bold")
        self.buttonD3.setMaximumSize(QSize(30, 30))
        self.buttonD4 = QPushButton("++")
        self.buttonD4.setStyleSheet("font-size: 20px; font-weight: bold")
        self.buttonD4.setMaximumSize(QSize(30, 30))

        layoutD.addWidget(self.labelD)
        layoutD.addWidget(self.textboxD)
        layoutD.addWidget(self.buttonD1)
        layoutD.addWidget(self.buttonD2)
        layoutD.addWidget(self.buttonD3)
        layoutD.addWidget(self.buttonD4)

        layoutE = QHBoxLayout()
        layoutE.setContentsMargins(0, 0, 0, 0); 
        layoutE.setSpacing(5) 
        layoutE.setAlignment(Qt.AlignLeft)        

        self.labelE = QLabel("High limit:")
        self.labelE.setStyleSheet("color: white")
        self.labelE.setMaximumWidth(140)
        self.labelE.setMinimumWidth(140)
        self.textboxE = QLineEdit()
        self.textboxE.setStyleSheet("font-weight: bold")
        self.textboxE.setText(float_to_str(settings_liveScaleMax))
        self.textboxE.setMaximumWidth(50)
        self.buttonE1 = QPushButton("--")
        self.buttonE1.setStyleSheet("font-size: 20px; font-weight: bold")
        self.buttonE1.setMaximumSize(QSize(30, 30))
        self.buttonE2 = QPushButton("-")
        self.buttonE2.setStyleSheet("font-size: 20px; font-weight: bold")
        self.buttonE2.setMaximumSize(QSize(30, 30))
        self.buttonE3 = QPushButton("+")
        self.buttonE3.setStyleSheet("font-size: 20px; font-weight: bold")
        self.buttonE3.setMaximumSize(QSize(30, 30))
        self.buttonE4 = QPushButton("++")
        self.buttonE4.setStyleSheet("font-size: 20px; font-weight: bold")
        self.buttonE4.setMaximumSize(QSize(30, 30))

        layoutE.addWidget(self.labelE)
        layoutE.addWidget(self.textboxE)
        layoutE.addWidget(self.buttonE1)
        layoutE.addWidget(self.buttonE2)
        layoutE.addWidget(self.buttonE3)
        layoutE.addWidget(self.buttonE4)

        layoutF = QHBoxLayout()
        layoutF.setContentsMargins(0, 0, 0, 0); 
        layoutF.setSpacing(5) 
        layoutF.setAlignment(Qt.AlignLeft)  

        self.labelF = QLabel("Scale width:")
        self.labelF.setStyleSheet("color: white")
        self.labelF.setMaximumWidth(140)
        self.labelF.setMinimumWidth(140)
        self.textboxF = QLineEdit()
        self.textboxF.setStyleSheet("font-weight: bold")
        self.textboxF.setText(float_to_str(settings_liveScaleWidth))
        self.textboxF.setMaximumWidth(50)
        self.buttonF1 = QPushButton("--")
        self.buttonF1.setStyleSheet("font-size: 20px; font-weight: bold")
        self.buttonF1.setMaximumSize(QSize(30, 30))
        self.buttonF2 = QPushButton("-")
        self.buttonF2.setStyleSheet("font-size: 20px; font-weight: bold")
        self.buttonF2.setMaximumSize(QSize(30, 30))
        self.buttonF3 = QPushButton("+")
        self.buttonF3.setStyleSheet("font-size: 20px; font-weight: bold")
        self.buttonF3.setMaximumSize(QSize(30, 30))
        self.buttonF4 = QPushButton("++")
        self.buttonF4.setStyleSheet("font-size: 20px; font-weight: bold")
        self.buttonF4.setMaximumSize(QSize(30, 30))

        layoutF.addWidget(self.labelF)
        layoutF.addWidget(self.textboxF)
        layoutF.addWidget(self.buttonF1)
        layoutF.addWidget(self.buttonF2)
        layoutF.addWidget(self.buttonF3)
        layoutF.addWidget(self.buttonF4)

        layoutG = QHBoxLayout()
        layoutG.setContentsMargins(0, 0, 0, 0); 
        layoutG.setSpacing(5) 
        layoutG.setAlignment(Qt.AlignLeft)  

        self.labelG = QLabel("Select file:")
        self.labelG.setStyleSheet("color: white")
        self.labelG.setMaximumWidth(140)
        self.labelG.setMinimumWidth(140)
        self.listG = QListWidget()
        self.listG.setStyleSheet("color: white; background-color: black")
        self.refreshList()
        self.listG.setMinimumWidth(170)
        self.listG.setMaximumWidth(170)
        self.listG.setMinimumHeight(150)
        self.listG.setMaximumHeight(150)
        self.listG.setSelectionMode(QAbstractItemView.SingleSelection)

        layoutG1 = QVBoxLayout()
        layoutG1.setContentsMargins(0, 0, 0, 0); 
        layoutG1.setSpacing(15) 
        layoutG1.setAlignment(Qt.AlignBottom)        

        self.buttonG1 = QPushButton("Open")
        self.buttonG1.setStyleSheet("font-weight: bold; background-color: lightgreen")
        self.buttonG1.setMaximumWidth(70)
        self.buttonG2 = QPushButton("Delete")
        self.buttonG2.setStyleSheet("font-weight: bold; background-color: pink")
        self.buttonG2.setMaximumWidth(70)

        layoutG1.addWidget(self.buttonG1)  
        layoutG1.addWidget(self.buttonG2) 

        layoutG.addWidget(self.labelG)
        layoutG.addWidget(self.listG) 
        layoutG.addLayout(layoutG1)

        self.textH = QTextEdit()
        self.textH.setStyleSheet("font-size: 14px; color:white; background-color: black")
        self.textH.setMinimumHeight(83)
        self.textH.setMaximumHeight(83)

        layoutI= QHBoxLayout()
        layoutI.setContentsMargins(0, 0, 0, 0); 
        layoutI.setSpacing(5) 
        layoutI.setAlignment(Qt.AlignLeft)  

        self.buttonI = QPushButton("Open switch count.txt")
        self.buttonI.setStyleSheet("font-weight: bold;")
        self.buttonI.setMinimumWidth(190) 
        self.buttonI.setMaximumWidth(190) 

        layoutI.addWidget(self.buttonI) 

        layoutJ = QHBoxLayout()
        layoutJ.setContentsMargins(0, 0, 0, 0); 
        layoutJ.setSpacing(5) 
        layoutJ.setAlignment(Qt.AlignLeft)

        self.labelJ = QLabel("Channel 1 GND:")
        self.labelJ.setStyleSheet("QLabel { color: white; }")
        self.labelJ.setMaximumWidth(140)
        self.labelJ.setMinimumWidth(140)
        self.buttonJ1 = QPushButton("Off")     
        if settings_ch1ground == 5: self.buttonJ1.setStyleSheet("font-size: 20px; font-weight: bold; background-color: lightgreen")
        else: self.buttonJ1.setStyleSheet("font-size: 20px; font-weight: bold; background-color: gray")
        self.buttonJ1.setMaximumSize(QSize(50, 30))
        self.buttonJ2 = QPushButton("GND")
        if settings_ch1ground == 0: self.buttonJ2.setStyleSheet("font-size: 20px; font-weight: bold; background-color: lightgreen")
        else: self.buttonJ2.setStyleSheet("font-size: 20px; font-weight: bold; background-color: gray")
        self.buttonJ2.setMaximumSize(QSize(50, 30))
        self.buttonJ3 = QPushButton("2")
        if settings_ch1ground == 2: self.buttonJ3.setStyleSheet("font-size: 20px; font-weight: bold; background-color: lightgreen")
        else: self.buttonJ3.setStyleSheet("font-size: 20px; font-weight: bold; background-color: gray")
        self.buttonJ3.setMaximumSize(QSize(30, 30))
        self.buttonJ4 = QPushButton("3")
        if settings_ch1ground == 3: self.buttonJ4.setStyleSheet("font-size: 20px; font-weight: bold; background-color: lightgreen")
        else: self.buttonJ4.setStyleSheet("font-size: 20px; font-weight: bold; background-color: gray")
        self.buttonJ4.setMaximumSize(QSize(30, 30))
        self.buttonJ5 = QPushButton("4")
        if settings_ch1ground == 4: self.buttonJ5.setStyleSheet("font-size: 20px; font-weight: bold; background-color: lightgreen")
        else: self.buttonJ5.setStyleSheet("font-size: 20px; font-weight: bold; background-color: gray")
        self.buttonJ5.setMaximumSize(QSize(30, 30))
        self.buttonJ6 = QPushButton("-GND")
        if settings_ch1ground == 10: self.buttonJ6.setStyleSheet("font-size: 20px; font-weight: bold; background-color: lightgreen")
        else: self.buttonJ6.setStyleSheet("font-size: 20px; font-weight: bold; background-color: gray")
        self.buttonJ6.setMaximumSize(QSize(60, 30))
        self.buttonJ7 = QPushButton("-2")
        if settings_ch1ground == 7: self.buttonJ7.setStyleSheet("font-size: 20px; font-weight: bold; background-color: lightgreen")
        else: self.buttonJ7.setStyleSheet("font-size: 20px; font-weight: bold; background-color: gray")
        self.buttonJ7.setMaximumSize(QSize(30, 30))
        self.buttonJ8 = QPushButton("-3")
        if settings_ch1ground == 8: self.buttonJ8.setStyleSheet("font-size: 20px; font-weight: bold; background-color: lightgreen")
        else: self.buttonJ8.setStyleSheet("font-size: 20px; font-weight: bold; background-color: gray")
        self.buttonJ8.setMaximumSize(QSize(30, 30))
        self.buttonJ9 = QPushButton("-4")
        if settings_ch1ground == 9: self.buttonJ9.setStyleSheet("font-size: 20px; font-weight: bold; background-color: lightgreen")
        else: self.buttonJ9.setStyleSheet("font-size: 20px; font-weight: bold; background-color: gray")
        self.buttonJ9.setMaximumSize(QSize(30, 100))

        layoutJ.addWidget(self.labelJ)
        layoutJ.addWidget(self.buttonJ1)
        layoutJ.addWidget(self.buttonJ2)
        layoutJ.addWidget(self.buttonJ3)
        layoutJ.addWidget(self.buttonJ4)
        layoutJ.addWidget(self.buttonJ5)
        layoutJ.addWidget(self.buttonJ6)
        layoutJ.addWidget(self.buttonJ7)
        layoutJ.addWidget(self.buttonJ8)
        layoutJ.addWidget(self.buttonJ9)

        layoutK = QHBoxLayout()
        layoutK.setContentsMargins(0, 0, 0, 0); 
        layoutK.setSpacing(5) 
        layoutK.setAlignment(Qt.AlignLeft)

        self.labelK = QLabel("Channel 2 GND:")
        self.labelK.setStyleSheet("QLabel { color: white; }")
        self.labelK.setMaximumWidth(140)
        self.labelK.setMinimumWidth(140)
        self.buttonK1 = QPushButton("Off")     
        if settings_ch2ground == 5: self.buttonK1.setStyleSheet("font-size: 20px; font-weight: bold; background-color: lightgreen")
        else: self.buttonK1.setStyleSheet("font-size: 20px; font-weight: bold; background-color: gray")
        self.buttonK1.setMaximumSize(QSize(50, 30))
        self.buttonK2 = QPushButton("GND")
        if settings_ch2ground == 0: self.buttonK2.setStyleSheet("font-size: 20px; font-weight: bold; background-color: lightgreen")
        else: self.buttonK2.setStyleSheet("font-size: 20px; font-weight: bold; background-color: gray")
        self.buttonK2.setMaximumSize(QSize(50, 30))
        self.buttonK3 = QPushButton("1")
        if settings_ch2ground == 1: self.buttonK3.setStyleSheet("font-size: 20px; font-weight: bold; background-color: lightgreen")
        else: self.buttonK3.setStyleSheet("font-size: 20px; font-weight: bold; background-color: gray")
        self.buttonK3.setMaximumSize(QSize(30, 30))
        self.buttonK4 = QPushButton("3")
        if settings_ch2ground == 3: self.buttonK4.setStyleSheet("font-size: 20px; font-weight: bold; background-color: lightgreen")
        else: self.buttonK4.setStyleSheet("font-size: 20px; font-weight: bold; background-color: gray")
        self.buttonK4.setMaximumSize(QSize(30, 30))
        self.buttonK5 = QPushButton("4")
        if settings_ch2ground == 4: self.buttonK5.setStyleSheet("font-size: 20px; font-weight: bold; background-color: lightgreen")
        else: self.buttonK5.setStyleSheet("font-size: 20px; font-weight: bold; background-color: gray")
        self.buttonK5.setMaximumSize(QSize(30, 30))
        self.buttonK6 = QPushButton("-GND")
        if settings_ch2ground == 10: self.buttonK6.setStyleSheet("font-size: 20px; font-weight: bold; background-color: lightgreen")
        else: self.buttonK6.setStyleSheet("font-size: 20px; font-weight: bold; background-color: gray")
        self.buttonK6.setMaximumSize(QSize(60, 30))
        self.buttonK7 = QPushButton("-1")
        if settings_ch2ground == 6: self.buttonK7.setStyleSheet("font-size: 20px; font-weight: bold; background-color: lightgreen")
        else: self.buttonK7.setStyleSheet("font-size: 20px; font-weight: bold; background-color: gray")
        self.buttonK7.setMaximumSize(QSize(30, 30))
        self.buttonK8 = QPushButton("-3")
        if settings_ch2ground == 8: self.buttonK8.setStyleSheet("font-size: 20px; font-weight: bold; background-color: lightgreen")
        else: self.buttonK8.setStyleSheet("font-size: 20px; font-weight: bold; background-color: gray")
        self.buttonK8.setMaximumSize(QSize(30, 30))
        self.buttonK9 = QPushButton("-4")
        if settings_ch2ground == 9: self.buttonK9.setStyleSheet("font-size: 20px; font-weight: bold; background-color: lightgreen")
        else: self.buttonK9.setStyleSheet("font-size: 20px; font-weight: bold; background-color: gray")
        self.buttonK9.setMaximumSize(QSize(30, 100))

        layoutK.addWidget(self.labelK)
        layoutK.addWidget(self.buttonK1)
        layoutK.addWidget(self.buttonK2)
        layoutK.addWidget(self.buttonK3)
        layoutK.addWidget(self.buttonK4)
        layoutK.addWidget(self.buttonK5)
        layoutK.addWidget(self.buttonK6)
        layoutK.addWidget(self.buttonK7)
        layoutK.addWidget(self.buttonK8)
        layoutK.addWidget(self.buttonK9)

        layoutL = QHBoxLayout()
        layoutL.setContentsMargins(0, 0, 0, 0); 
        layoutL.setSpacing(5) 
        layoutL.setAlignment(Qt.AlignLeft)

        self.labelL = QLabel("Channel 3 GND:")
        self.labelL.setStyleSheet("QLabel { color: white; }")
        self.labelL.setMaximumWidth(140)
        self.labelL.setMinimumWidth(140)
        self.buttonL1 = QPushButton("Off")     
        if settings_ch3ground == 5: self.buttonL1.setStyleSheet("font-size: 20px; font-weight: bold; background-color: lightgreen")
        else: self.buttonL1.setStyleSheet("font-size: 20px; font-weight: bold; background-color: gray")
        self.buttonL1.setMaximumSize(QSize(50, 30))
        self.buttonL2 = QPushButton("GND")
        if settings_ch3ground == 0: self.buttonL2.setStyleSheet("font-size: 20px; font-weight: bold; background-color: lightgreen")
        else: self.buttonL2.setStyleSheet("font-size: 20px; font-weight: bold; background-color: gray")
        self.buttonL2.setMaximumSize(QSize(50, 30))
        self.buttonL3 = QPushButton("1")
        if settings_ch3ground == 1: self.buttonL3.setStyleSheet("font-size: 20px; font-weight: bold; background-color: lightgreen")
        else: self.buttonL3.setStyleSheet("font-size: 20px; font-weight: bold; background-color: gray")
        self.buttonL3.setMaximumSize(QSize(30, 30))
        self.buttonL4 = QPushButton("2")
        if settings_ch3ground == 2: self.buttonL4.setStyleSheet("font-size: 20px; font-weight: bold; background-color: lightgreen")
        else: self.buttonL4.setStyleSheet("font-size: 20px; font-weight: bold; background-color: gray")
        self.buttonL4.setMaximumSize(QSize(30, 30))
        self.buttonL5 = QPushButton("4")
        if settings_ch3ground == 4: self.buttonL5.setStyleSheet("font-size: 20px; font-weight: bold; background-color: lightgreen")
        else: self.buttonL5.setStyleSheet("font-size: 20px; font-weight: bold; background-color: gray")
        self.buttonL5.setMaximumSize(QSize(30, 30))
        self.buttonL6 = QPushButton("-GND")
        if settings_ch2ground == 10: self.buttonL6.setStyleSheet("font-size: 20px; font-weight: bold; background-color: lightgreen")
        else: self.buttonL6.setStyleSheet("font-size: 20px; font-weight: bold; background-color: gray")
        self.buttonL6.setMaximumSize(QSize(60, 30))
        self.buttonL7 = QPushButton("-1")
        if settings_ch3ground == 6: self.buttonL7.setStyleSheet("font-size: 20px; font-weight: bold; background-color: lightgreen")
        else: self.buttonL7.setStyleSheet("font-size: 20px; font-weight: bold; background-color: gray")
        self.buttonL7.setMaximumSize(QSize(30, 30))
        self.buttonL8 = QPushButton("-2")
        if settings_ch3ground == 7: self.buttonL8.setStyleSheet("font-size: 20px; font-weight: bold; background-color: lightgreen")
        else: self.buttonL8.setStyleSheet("font-size: 20px; font-weight: bold; background-color: gray")
        self.buttonL8.setMaximumSize(QSize(30, 30))
        self.buttonL9 = QPushButton("-4")
        if settings_ch3ground == 9: self.buttonL9.setStyleSheet("font-size: 20px; font-weight: bold; background-color: lightgreen")
        else: self.buttonL9.setStyleSheet("font-size: 20px; font-weight: bold; background-color: gray")
        self.buttonL9.setMaximumSize(QSize(30, 100))

        layoutL.addWidget(self.labelL)
        layoutL.addWidget(self.buttonL1)
        layoutL.addWidget(self.buttonL2)
        layoutL.addWidget(self.buttonL3)
        layoutL.addWidget(self.buttonL4)
        layoutL.addWidget(self.buttonL5)
        layoutL.addWidget(self.buttonL6)
        layoutL.addWidget(self.buttonL7)
        layoutL.addWidget(self.buttonL8)
        layoutL.addWidget(self.buttonL9)

        layoutM = QHBoxLayout()
        layoutM.setContentsMargins(0, 0, 0, 0); 
        layoutM.setSpacing(5) 
        layoutM.setAlignment(Qt.AlignLeft)

        self.labelM = QLabel("Channel 4 GND:")
        self.labelM.setStyleSheet("QLabel { color: white; }")
        self.labelM.setMaximumWidth(140)
        self.labelM.setMinimumWidth(140)
        self.buttonM1 = QPushButton("Off")     
        if settings_ch4ground == 5: self.buttonM1.setStyleSheet("font-size: 20px; font-weight: bold; background-color: lightgreen")
        else: self.buttonM1.setStyleSheet("font-size: 20px; font-weight: bold; background-color: gray")
        self.buttonM1.setMaximumSize(QSize(50, 30))
        self.buttonM2 = QPushButton("GND")
        if settings_ch4ground == 0: self.buttonM2.setStyleSheet("font-size: 20px; font-weight: bold; background-color: lightgreen")
        else: self.buttonM2.setStyleSheet("font-size: 20px; font-weight: bold; background-color: gray")
        self.buttonM2.setMaximumSize(QSize(50, 30))
        self.buttonM3 = QPushButton("1")
        if settings_ch4ground == 1: self.buttonM3.setStyleSheet("font-size: 20px; font-weight: bold; background-color: lightgreen")
        else: self.buttonM3.setStyleSheet("font-size: 20px; font-weight: bold; background-color: gray")
        self.buttonM3.setMaximumSize(QSize(30, 30))
        self.buttonM4 = QPushButton("2")
        if settings_ch4ground == 2: self.buttonM4.setStyleSheet("font-size: 20px; font-weight: bold; background-color: lightgreen")
        else: self.buttonM4.setStyleSheet("font-size: 20px; font-weight: bold; background-color: gray")
        self.buttonM4.setMaximumSize(QSize(30, 30))
        self.buttonM5 = QPushButton("3")
        if settings_ch4ground == 3: self.buttonM5.setStyleSheet("font-size: 20px; font-weight: bold; background-color: lightgreen")
        else: self.buttonM5.setStyleSheet("font-size: 20px; font-weight: bold; background-color: gray")
        self.buttonM5.setMaximumSize(QSize(30, 30))
        self.buttonM6 = QPushButton("-GND")
        if settings_ch2ground == 10: self.buttonM6.setStyleSheet("font-size: 20px; font-weight: bold; background-color: lightgreen")
        else: self.buttonM6.setStyleSheet("font-size: 20px; font-weight: bold; background-color: gray")
        self.buttonM6.setMaximumSize(QSize(60, 30))
        self.buttonM7 = QPushButton("-1")
        if settings_ch4ground == 7: self.buttonM7.setStyleSheet("font-size: 20px; font-weight: bold; background-color: lightgreen")
        else: self.buttonM7.setStyleSheet("font-size: 20px; font-weight: bold; background-color: gray")
        self.buttonM7.setMaximumSize(QSize(30, 30))
        self.buttonM8 = QPushButton("-2")
        if settings_ch4ground == 8: self.buttonM8.setStyleSheet("font-size: 20px; font-weight: bold; background-color: lightgreen")
        else: self.buttonM8.setStyleSheet("font-size: 20px; font-weight: bold; background-color: gray")
        self.buttonM8.setMaximumSize(QSize(30, 30))
        self.buttonM9 = QPushButton("-3")
        if settings_ch4ground == 9: self.buttonM9.setStyleSheet("font-size: 20px; font-weight: bold; background-color: lightgreen")
        else: self.buttonM9.setStyleSheet("font-size: 20px; font-weight: bold; background-color: gray")
        self.buttonM9.setMaximumSize(QSize(30, 100))

        layoutM.addWidget(self.labelM)
        layoutM.addWidget(self.buttonM1)
        layoutM.addWidget(self.buttonM2)
        layoutM.addWidget(self.buttonM3)
        layoutM.addWidget(self.buttonM4)
        layoutM.addWidget(self.buttonM5)
        layoutM.addWidget(self.buttonM6)
        layoutM.addWidget(self.buttonM7)
        layoutM.addWidget(self.buttonM8)
        layoutM.addWidget(self.buttonM9)

        self.layoutJ = layoutJ
        self.layoutK = layoutK
        self.layoutL = layoutL
        self.layoutM = layoutM      

        layout00 = QVBoxLayout()
        layout00.setAlignment(Qt.AlignTop)
        layout00.addLayout(layout1)
        layout00.addLayout(layout2)
        layout00.addLayout(layout3)
        layout00.addLayout(layout4)
        layout00.addLayout(layout5)
        layout00.addLayout(layout6)
        layout00.addLayout(layout7)
        layout00.addLayout(layout8)
        layout00.addLayout(layout9)
        layout00.addLayout(layoutA)
        layout00.addLayout(layoutB)

        layout01 = QVBoxLayout()
        layout01.setAlignment(Qt.AlignTop)
        layout01.addLayout(layoutC)
        layout01.addLayout(layoutD)
        layout01.addLayout(layoutE)
        layout01.addLayout(layoutF)
        layout01.addLayout(layoutG)
        layout01.addWidget(self.textH)
        layout01.addWidget(self.buttonI)

        layout0 = QHBoxLayout()
        layout0.setAlignment(Qt.AlignLeft)
        layout0.addLayout(layout00)
        layout0.addLayout(layout01) 

        scroll = QScrollArea()
        scroll.setWidgetResizable(True)
        scroll.setStyleSheet("""
            QScrollArea {
                border: none;
            }
            QScrollArea > QWidget > QWidget {
                background: black;
            }
            """)
        main_layout = QVBoxLayout()
        main_layout.setContentsMargins(0, 0, 0, 0)
        main_layout.addWidget(scroll)

        self.setLayout(main_layout)

        container = QWidget()
        scroll.setWidget(container)
        main_layout1 = QVBoxLayout(container)
        main_layout1.setAlignment(Qt.AlignTop)
        main_layout1.addLayout(layout0)
        main_layout1.addLayout(layoutJ) 
        main_layout1.addLayout(layoutK) 
        main_layout1.addLayout(layoutL) 
        main_layout1.addLayout(layoutM) 
        
        self.button11.clicked.connect(lambda: self.setTimeLimit(0,1))
        self.button12.clicked.connect(lambda: self.setTimeLimit(0,0.1))
        self.button13.clicked.connect(lambda: self.setTimeLimit(1,0.1))
        self.button14.clicked.connect(lambda: self.setTimeLimit(1,1))
        self.button21.clicked.connect(lambda: self.setMeasureChannels(1))
        self.button22.clicked.connect(lambda: self.setMeasureChannels(2))
        self.button23.clicked.connect(lambda: self.setMeasureChannels(3))
        self.button24.clicked.connect(lambda: self.setMeasureChannels(4))
        self.button3.clicked.connect(self.setTrigger)
        self.button41.clicked.connect(lambda: self.setTriggerChannel(1))        
        self.button42.clicked.connect(lambda: self.setTriggerChannel(2))
        self.button43.clicked.connect(lambda: self.setTriggerChannel(3))
        self.button44.clicked.connect(lambda: self.setTriggerChannel(4))
        self.button51.clicked.connect(lambda: self.setRisingEdge(True))
        self.button52.clicked.connect(lambda: self.setRisingEdge(False))
        self.button61.clicked.connect(lambda: self.setTriggerThreshold(0,1))
        self.button62.clicked.connect(lambda: self.setTriggerThreshold(0,0.1))
        self.button63.clicked.connect(lambda: self.setTriggerThreshold(1,0.1))
        self.button64.clicked.connect(lambda: self.setTriggerThreshold(1,1))
        self.button71.clicked.connect(lambda: self.setOffsetPercent(0, 5))
        self.button72.clicked.connect(lambda: self.setOffsetPercent(1, 5))
        self.button8.clicked.connect(self.setCountSwitches)
        self.button91.clicked.connect(lambda: self.setSwitchChannel(1))        
        self.button92.clicked.connect(lambda: self.setSwitchChannel(2))
        self.button93.clicked.connect(lambda: self.setSwitchChannel(3))
        self.button94.clicked.connect(lambda: self.setSwitchChannel(4))
        self.buttonA1.clicked.connect(lambda: self.setSwitchLow(0,1))
        self.buttonA2.clicked.connect(lambda: self.setSwitchLow(0,0.1))
        self.buttonA3.clicked.connect(lambda: self.setSwitchLow(1,0.1))
        self.buttonA4.clicked.connect(lambda: self.setSwitchLow(1,1))
        self.buttonB1.clicked.connect(lambda: self.setSwitchHigh(0,1))
        self.buttonB2.clicked.connect(lambda: self.setSwitchHigh(0,0.1))
        self.buttonB3.clicked.connect(lambda: self.setSwitchHigh(1,0.1))
        self.buttonB4.clicked.connect(lambda: self.setSwitchHigh(1,1))
        self.buttonD1.clicked.connect(lambda: self.setScaleLow(0,1))
        self.buttonD2.clicked.connect(lambda: self.setScaleLow(0,0.1))
        self.buttonD3.clicked.connect(lambda: self.setScaleLow(1,0.1))
        self.buttonD4.clicked.connect(lambda: self.setScaleLow(1,1))
        self.buttonE1.clicked.connect(lambda: self.setScaleHigh(0,1))
        self.buttonE2.clicked.connect(lambda: self.setScaleHigh(0,0.1))
        self.buttonE3.clicked.connect(lambda: self.setScaleHigh(1,0.1))
        self.buttonE4.clicked.connect(lambda: self.setScaleHigh(1,1))
        self.buttonF1.clicked.connect(lambda: self.setScaleWide(0,1))
        self.buttonF2.clicked.connect(lambda: self.setScaleWide(0,0.1))
        self.buttonF3.clicked.connect(lambda: self.setScaleWide(1,0.1))
        self.buttonF4.clicked.connect(lambda: self.setScaleWide(1,1))
        self.listG.clicked.connect(self.selectedFile)
        self.buttonG1.clicked.connect(self.openFromFile)
        self.buttonG2.clicked.connect(self.deleteFile)
        self.buttonI.clicked.connect(self.openSwitchCount)
        self.buttonJ1.clicked.connect(lambda: self.setGnd(1,5))
        self.buttonJ2.clicked.connect(lambda: self.setGnd(1,0))
        self.buttonJ3.clicked.connect(lambda: self.setGnd(1,2))
        self.buttonJ4.clicked.connect(lambda: self.setGnd(1,3))
        self.buttonJ5.clicked.connect(lambda: self.setGnd(1,4))
        self.buttonJ6.clicked.connect(lambda: self.setGnd(1,10))
        self.buttonJ7.clicked.connect(lambda: self.setGnd(1,7))
        self.buttonJ8.clicked.connect(lambda: self.setGnd(1,8))
        self.buttonJ9.clicked.connect(lambda: self.setGnd(1,9))
        self.buttonK1.clicked.connect(lambda: self.setGnd(2,5))
        self.buttonK2.clicked.connect(lambda: self.setGnd(2,0))
        self.buttonK3.clicked.connect(lambda: self.setGnd(2,1))
        self.buttonK4.clicked.connect(lambda: self.setGnd(2,3))
        self.buttonK5.clicked.connect(lambda: self.setGnd(2,4))
        self.buttonK6.clicked.connect(lambda: self.setGnd(2,10))
        self.buttonK7.clicked.connect(lambda: self.setGnd(2,6))
        self.buttonK8.clicked.connect(lambda: self.setGnd(2,8))
        self.buttonK9.clicked.connect(lambda: self.setGnd(2,9))
        self.buttonL1.clicked.connect(lambda: self.setGnd(3,5))
        self.buttonL2.clicked.connect(lambda: self.setGnd(3,0))
        self.buttonL3.clicked.connect(lambda: self.setGnd(3,1))
        self.buttonL4.clicked.connect(lambda: self.setGnd(3,2))
        self.buttonL5.clicked.connect(lambda: self.setGnd(3,4))
        self.buttonL6.clicked.connect(lambda: self.setGnd(3,10))
        self.buttonL7.clicked.connect(lambda: self.setGnd(3,6))
        self.buttonL8.clicked.connect(lambda: self.setGnd(3,7))
        self.buttonL9.clicked.connect(lambda: self.setGnd(3,9))
        self.buttonM1.clicked.connect(lambda: self.setGnd(4,5))
        self.buttonM2.clicked.connect(lambda: self.setGnd(4,0))
        self.buttonM3.clicked.connect(lambda: self.setGnd(4,1))
        self.buttonM4.clicked.connect(lambda: self.setGnd(4,2))
        self.buttonM5.clicked.connect(lambda: self.setGnd(4,3))
        self.buttonM6.clicked.connect(lambda: self.setGnd(4,10))
        self.buttonM7.clicked.connect(lambda: self.setGnd(4,6))
        self.buttonM8.clicked.connect(lambda: self.setGnd(4,7))
        self.buttonM9.clicked.connect(lambda: self.setGnd(4,8))

    def force_fullscreen_fix(self):
        rect = QApplication.primaryScreen().availableGeometry()
        self.setGeometry(rect)
        self.setFixedSize(self.width(), self.height())

    def setTimeLimit(self, direction, increment):
        global settings_timeLimit

        if direction == 0:
            settings_timeLimit = round(settings_timeLimit - increment, 1)
        else:
            settings_timeLimit = round(settings_timeLimit + increment, 1)
        if settings_timeLimit < 0.1: settings_timeLimit = 0.1
        if settings_timeLimit > 30: settings_timeLimit = float(30)
        self.textbox1.setText(float_to_str(settings_timeLimit))
        saveSettings("timeLimit")

    def setMeasureChannels(self, num):
        global settings_measureChannels1, settings_measureChannels2, settings_measureChannels3, settings_measureChannels4

        if num == 1:
            if settings_measureChannels1:
                settings_measureChannels1 = False
                self.button21.setStyleSheet("font-size: 20px; font-weight: bold; background-color: gray")
            else:
                settings_measureChannels1 = True
                self.button21.setStyleSheet("font-size: 20px; font-weight: bold; background-color: lightgreen")
        elif num == 2:
            if settings_measureChannels2:
                settings_measureChannels2 = False
                self.button22.setStyleSheet("font-size: 20px; font-weight: bold; background-color: gray")
            else:
                settings_measureChannels2 = True
                self.button22.setStyleSheet("font-size: 20px; font-weight: bold; background-color: lightgreen")
        elif num == 3:
            if settings_measureChannels3:
                settings_measureChannels3 = False
                self.button23.setStyleSheet("font-size: 20px; font-weight: bold; background-color: gray")
            else:
                settings_measureChannels3 = True
                self.button23.setStyleSheet("font-size: 20px; font-weight: bold; background-color: lightgreen")
        elif num == 4:
            if settings_measureChannels4:
                settings_measureChannels4 = False
                self.button24.setStyleSheet("font-size: 20px; font-weight: bold; background-color: gray")
            else:
                settings_measureChannels4 = True
                self.button24.setStyleSheet("font-size: 20px; font-weight: bold; background-color: lightgreen")
        saveSettings("measureChannels")
        if graphWindow.pressed:
            graphWindow.drawValues()


    def setTrigger(self):
        global settings_trigger

        if settings_trigger: settings_trigger = False
        else: settings_trigger = True
        if settings_trigger:
            self.button3.setText("On")
            self.button3.setStyleSheet("font-weight: bold; background-color: lightgreen")
        else:
            self.button3.setText("Off")
            self.button3.setStyleSheet("font-weight: bold; background-color: gray")
        saveSettings("trigger") 

    def setTriggerChannel(self, num):
        global settings_triggerChannel

        settings_triggerChannel = num
        self.button41.setStyleSheet("font-size: 20px; font-weight: bold; background-color: gray")
        self.button42.setStyleSheet("font-size: 20px; font-weight: bold; background-color: gray")
        self.button43.setStyleSheet("font-size: 20px; font-weight: bold; background-color: gray")
        self.button44.setStyleSheet("font-size: 20px; font-weight: bold; background-color: gray")
        if num == 1:
            self.button41.setStyleSheet("font-size: 20px; font-weight: bold; background-color: lightgreen")
        elif num == 2:
            self.button42.setStyleSheet("font-size: 20px; font-weight: bold; background-color: lightgreen")
        elif num == 3:
            self.button43.setStyleSheet("font-size: 20px; font-weight: bold; background-color: lightgreen")
        elif num == 4:
            self.button44.setStyleSheet("font-size: 20px; font-weight: bold; background-color: lightgreen")
        saveSettings("triggerChannel")

    def setRisingEdge(self, rising):
        global settings_risingEdge

        if rising:
            settings_risingEdge = True
            self.button51.setStyleSheet("font-weight: bold; background-color: lightgreen")
            self.button52.setStyleSheet("font-weight: bold; background-color: gray")
        else:
            settings_risingEdge = False
            self.button51.setStyleSheet("font-weight: bold; background-color: gray")
            self.button52.setStyleSheet("font-weight: bold; background-color: lightgreen")
        saveSettings("risingEdge") 

    def setTriggerThreshold(self, direction, increment):
        global settings_triggerThreshold

        if direction == 0:
            settings_triggerThreshold = round(settings_triggerThreshold - increment, 1)
        else:
            settings_triggerThreshold = round(settings_triggerThreshold + increment, 1)
        if settings_triggerThreshold < 0: settings_triggerThreshold = float(0)
        if settings_triggerThreshold > 5: settings_triggerThreshold = float(5)
        self.textbox6.setText(float_to_str(settings_triggerThreshold))
        saveSettings("triggerThreshold")

    def setOffsetPercent(self, direction, increment):
        global settings_offsetPercent

        if direction == 0:
            settings_offsetPercent = round(settings_offsetPercent - increment, 1)
        else:
            settings_offsetPercent = round(settings_offsetPercent + increment, 1)
        if settings_offsetPercent < 0: settings_offsetPercent = 0
        if settings_offsetPercent > 100: settings_offsetPercent = 100
        self.textbox7.setText(str(settings_offsetPercent))
        saveSettings("offsetPercent")

    def setCountSwitches(self):
        global settings_countSwitches

        if settings_countSwitches: settings_countSwitches = False
        else: settings_countSwitches = True
        if settings_countSwitches:
            self.button8.setText("On")
            self.button8.setStyleSheet("font-weight: bold; background-color: lightgreen")
        else:
            self.button8.setText("Off")
            self.button8.setStyleSheet("font-weight: bold; background-color: gray")
        
        graphWindow.setCountSwitches()
        saveSettings("countSwitches") 

    def setSwitchChannel(self, num):
        global settings_switchChannel

        settings_switchChannel = num
        self.button91.setStyleSheet("font-size: 20px; font-weight: bold; background-color: gray")
        self.button92.setStyleSheet("font-size: 20px; font-weight: bold; background-color: gray")
        self.button93.setStyleSheet("font-size: 20px; font-weight: bold; background-color: gray")
        self.button94.setStyleSheet("font-size: 20px; font-weight: bold; background-color: gray")
        if num == 1:
            self.button91.setStyleSheet("font-size: 20px; font-weight: bold; background-color: lightgreen")
        elif num == 2:
            self.button92.setStyleSheet("font-size: 20px; font-weight: bold; background-color: lightgreen")
        elif num == 3:
            self.button93.setStyleSheet("font-size: 20px; font-weight: bold; background-color: lightgreen")
        elif num == 4:
            self.button94.setStyleSheet("font-size: 20px; font-weight: bold; background-color: lightgreen")
        saveSettings("switchChannel")

    def setSwitchLow(self, direction, increment):
        global settings_switchLow

        if direction == 0:
            # for custom value, we round down to nearest tenth
            if math.floor(settings_switchLow * 10) / 10 != settings_switchLow:
                settings_switchLow = math.floor(settings_switchLow * 10) / 10
            else:    
                settings_switchLow = round(settings_switchLow - increment, 1)
                if settings_switchLow < 0:
                    settings_switchLow = float(0)            
        else:
            if math.ceil(settings_switchLow * 10) / 10 != settings_switchLow:
                if math.ceil(settings_switchLow * 10) / 10 < settings_switchHigh:
                    settings_switchLow = math.ceil(settings_switchLow * 10) / 10
            else:
                settings_switchLow = round(settings_switchLow + increment, 1)        
                if settings_switchLow > 5:
                    settings_switchLow = float(5)
                if settings_switchLow >= settings_switchHigh:
                    # high is a custom value, low should be its floor
                    if math.floor(settings_switchHigh * 10) / 10 != settings_switchHigh:
                        settings_switchLow = math.floor(settings_switchHigh * 10) / 10
                    else:
                        settings_switchLow = round(settings_switchHigh - 0.1, 1)

        self.textboxA.setText(float_to_str(settings_switchLow))
        saveSettings("switchLow")
        graphWindow.countSwitchLineY1 = int(443 - (settings_switchLow - graphWindow.minV) / (graphWindow.maxV - graphWindow.minV) * (443 - graphWindow.buttonBarHeight))
        graphWindow.countSwitches()

    def setSwitchHigh(self, direction, increment):
        global settings_switchHigh

        if direction == 0:
            if math.floor(settings_switchHigh * 10) / 10 != settings_switchHigh:
                if math.floor(settings_switchHigh * 10) / 10 > settings_switchLow:
                    settings_switchHigh = math.floor(settings_switchHigh * 10) / 10
            else:
                settings_switchHigh = round(settings_switchHigh - increment, 1)        
                if settings_switchHigh < 0:
                    settings_switchHigh = float(0)
                if settings_switchHigh <= settings_switchLow:
                    # high is a custom value, low should be its floor
                    if math.ceil(settings_switchLow * 10) / 10 != settings_switchLow:
                        settings_switchHigh = math.ceil(settings_switchLow * 10) / 10
                    else:
                        settings_switchHigh = round(settings_switchLow + 0.1, 1)                       
        else:
            # for custom value, we round down to nearest tenth
            if math.ceil(settings_switchHigh * 10) / 10 != settings_switchHigh:
                settings_switchHigh = math.ceil(settings_switchHigh * 10) / 10
            else:    
                settings_switchHigh = round(settings_switchHigh + increment, 1)
                if settings_switchHigh > 5:
                    settings_switchHigh = float(5)

        self.textboxB.setText(float_to_str(settings_switchHigh))
        saveSettings("switchHigh")
        graphWindow.countSwitchLineY2 = int(443 - (settings_switchHigh - graphWindow.minV) / (graphWindow.maxV - graphWindow.minV) * (443 - graphWindow.buttonBarHeight))
        graphWindow.countSwitches()

    def setScaleLow(self, direction, increment):
        global settings_liveScaleMin

        if direction == 0:
            settings_liveScaleMin = round(settings_liveScaleMin - increment, 1)
            if settings_liveScaleMin < 0:
                settings_liveScaleMin = float(0)            
        else:
            settings_liveScaleMin = round(settings_liveScaleMin + increment, 1)        
            if settings_liveScaleMin > 5:
                settings_liveScaleMin = float(5)
            if settings_liveScaleMin >= settings_liveScaleMax:
                settings_liveScaleMin = round(settings_liveScaleMax - 0.1, 1)
        self.textboxD.setText(float_to_str(settings_liveScaleMin))
        saveSettings("liveScaleMin")

    def setScaleHigh(self, direction, increment):
        global settings_liveScaleMax

        if direction == 0:
            settings_liveScaleMax = round(settings_liveScaleMax - increment, 1)
            if settings_liveScaleMax < 0:
                settings_liveScaleMax = float(0)
            if settings_liveScaleMax <= settings_liveScaleMin: 
                settings_liveScaleMax = round(settings_liveScaleMin + 0.1, 1) 
        else:
            settings_liveScaleMax = round(settings_liveScaleMax + increment, 1)
            if settings_liveScaleMax > 5:
                settings_liveScaleMax = float(5)        
        self.textboxE.setText(float_to_str(settings_liveScaleMax))
        saveSettings("liveScaleMax")

    def setScaleWide(self, direction, increment):
        global settings_liveScaleWidth

        if direction == 0:
            settings_liveScaleWidth = round(settings_liveScaleWidth - increment, 1)
            if settings_liveScaleWidth < 0.1:
                settings_liveScaleWidth = float(0.1)
        else:
            settings_liveScaleWidth = round(settings_liveScaleWidth + increment, 1)
            if settings_liveScaleWidth > 10:
                settings_liveScaleWidth = float(10)        
        self.textboxF.setText(float_to_str(settings_liveScaleWidth))
        graphWindow.firstUpdate = True
        saveSettings("liveScaleWidth")

    def focusInEvent(self, event):
        super().focusInEvent(event)
        self.refreshList()

    def refreshList(self):
        self.listG.clear()
        items = []
        pattern = re.compile(r'^voltage4_log_\d+\.txt$')

        for filename in os.listdir("/home/fodorbalint/Documents"):            
            match = pattern.match(filename)
            if match:
                items.append(filename)
                # items.append(match.group(0))
        def natural_key(s):
            return [int(text) if text.isdigit() else text.lower()
                for text in re.split(r'(\d+)', s)]

        items.sort(reverse = True, key = natural_key)
        self.listG.addItems(items)

    def selectedFile(self):
        items = self.listG.selectedItems()
        if len(items) == 1:
            self.textH.setPlainText("")
            with open(f"/home/fodorbalint/Documents/log descriptions.txt", "r") as file:
                descriptions = file.read()
                descriptionsArr = descriptions.split("\n")
                for line in descriptionsArr:
                    pairs = line.strip().split(": ")
                    if "voltage4_log_" + pairs[0] + ".txt" == items[0].text():
                        self.textH.setPlainText(pairs[1])

    def deleteFile(self):        
        items = self.listG.selectedItems()
        if len(items) == 1:
            row = self.listG.currentRow()
            os.remove("/home/fodorbalint/Documents/" + items[0].text())
            self.refreshList()
            self.listG.setCurrentRow(row)
            self.selectedFile()
    
    def openFromFile(self):
        items = self.listG.selectedItems()
        if len(items) == 1:
            graphWindow.openFromFile(items[0].text())

    def openSwitchCount(self):
        editorWindow.open_file()

    def setGnd(self, channel, value):
        global settings_ch1ground, settings_ch2ground, settings_ch3ground, settings_ch4ground 

        if channel == 1:
            settings_ch1ground = value
            saveSettings("ch1ground")
            grayLayoutButtons(self.layoutJ)
            if value == 5:
                self.buttonJ1.setStyleSheet("font-size: 20px; font-weight: bold; background-color: lightgreen")
            elif value == 0:
                self.buttonJ2.setStyleSheet("font-size: 20px; font-weight: bold; background-color: lightgreen")
            elif value == 2:
                self.buttonJ3.setStyleSheet("font-size: 20px; font-weight: bold; background-color: lightgreen")
            elif value == 3:
                self.buttonJ4.setStyleSheet("font-size: 20px; font-weight: bold; background-color: lightgreen")
            elif value == 4:
                self.buttonJ5.setStyleSheet("font-size: 20px; font-weight: bold; background-color: lightgreen")
            elif value == 10:
                self.buttonJ6.setStyleSheet("font-size: 20px; font-weight: bold; background-color: lightgreen")
            elif value == 7:
                self.buttonJ7.setStyleSheet("font-size: 20px; font-weight: bold; background-color: lightgreen")
            elif value == 8:
                self.buttonJ8.setStyleSheet("font-size: 20px; font-weight: bold; background-color: lightgreen")
            elif value == 9:
                self.buttonJ9.setStyleSheet("font-size: 20px; font-weight: bold; background-color: lightgreen")
            
        elif channel == 2:
            settings_ch2ground = value
            saveSettings("ch2ground")
            grayLayoutButtons(self.layoutK)
            if value == 5:
                self.buttonK1.setStyleSheet("font-size: 20px; font-weight: bold; background-color: lightgreen")
            elif value == 0:
                self.buttonK2.setStyleSheet("font-size: 20px; font-weight: bold; background-color: lightgreen")
            elif value == 1:
                self.buttonK3.setStyleSheet("font-size: 20px; font-weight: bold; background-color: lightgreen")
            elif value == 3:
                self.buttonK4.setStyleSheet("font-size: 20px; font-weight: bold; background-color: lightgreen")
            elif value == 4:
                self.buttonK5.setStyleSheet("font-size: 20px; font-weight: bold; background-color: lightgreen")
            elif value == 10:
                self.buttonK6.setStyleSheet("font-size: 20px; font-weight: bold; background-color: lightgreen")
            elif value == 6:
                self.buttonK7.setStyleSheet("font-size: 20px; font-weight: bold; background-color: lightgreen")
            elif value == 8:
                self.buttonK8.setStyleSheet("font-size: 20px; font-weight: bold; background-color: lightgreen")
            elif value == 9:
                self.buttonK9.setStyleSheet("font-size: 20px; font-weight: bold; background-color: lightgreen")
            
        elif channel == 3:
            settings_ch3ground = value
            saveSettings("ch3ground")
            grayLayoutButtons(self.layoutL)
            if value == 5:
                self.buttonL1.setStyleSheet("font-size: 20px; font-weight: bold; background-color: lightgreen")
            elif value == 0:
                self.buttonL2.setStyleSheet("font-size: 20px; font-weight: bold; background-color: lightgreen")
            elif value == 1:
                self.buttonL3.setStyleSheet("font-size: 20px; font-weight: bold; background-color: lightgreen")
            elif value == 2:
                self.buttonL4.setStyleSheet("font-size: 20px; font-weight: bold; background-color: lightgreen")
            elif value == 4:
                self.buttonL5.setStyleSheet("font-size: 20px; font-weight: bold; background-color: lightgreen")
            elif value == 10:
                self.buttonL6.setStyleSheet("font-size: 20px; font-weight: bold; background-color: lightgreen")
            elif value == 6:
                self.buttonL7.setStyleSheet("font-size: 20px; font-weight: bold; background-color: lightgreen")
            elif value == 7:
                self.buttonL8.setStyleSheet("font-size: 20px; font-weight: bold; background-color: lightgreen")
            elif value == 9:
                self.buttonL9.setStyleSheet("font-size: 20px; font-weight: bold; background-color: lightgreen")

        elif channel == 4:
            settings_ch4ground = value
            saveSettings("ch4ground")
            grayLayoutButtons(self.layoutM)
            if value == 5:
                self.buttonM1.setStyleSheet("font-size: 20px; font-weight: bold; background-color: lightgreen")
            elif value == 0:
                self.buttonM2.setStyleSheet("font-size: 20px; font-weight: bold; background-color: lightgreen")
            elif value == 1:
                self.buttonM3.setStyleSheet("font-size: 20px; font-weight: bold; background-color: lightgreen")
            elif value == 2:
                self.buttonM4.setStyleSheet("font-size: 20px; font-weight: bold; background-color: lightgreen")
            elif value == 3:
                self.buttonM5.setStyleSheet("font-size: 20px; font-weight: bold; background-color: lightgreen")
            elif value == 10:
                self.buttonM6.setStyleSheet("font-size: 20px; font-weight: bold; background-color: lightgreen")
            elif value == 6:
                self.buttonM7.setStyleSheet("font-size: 20px; font-weight: bold; background-color: lightgreen")
            elif value == 7:
                self.buttonM8.setStyleSheet("font-size: 20px; font-weight: bold; background-color: lightgreen")
            elif value == 8:
                self.buttonM9.setStyleSheet("font-size: 20px; font-weight: bold; background-color: lightgreen")

class EditorWindow(QWidget):    
    def __init__(self):
        super().__init__()
        self.setWindowTitle("Switch count")
        self.setWindowFlags(Qt.FramelessWindowHint)
        self.setAttribute(Qt.WA_OpaquePaintEvent)
        self.setAttribute(Qt.WA_NoSystemBackground)        

        layout = QVBoxLayout()
        self.text_edit = QTextEdit()
        self.text_edit.setStyleSheet("font-size: 14px; color:white; background-color: black")
        layout.addWidget(self.text_edit)
        self.setLayout(layout)

    def open_file(self):
        try:
            with open("/home/fodorbalint/Documents/switch count.txt", 'r') as f:
                content = f.read()
            self.text_edit.setPlainText(content)
            self.show()
            self.showMaximized()
            self.raise_()  # bring to front
            self.activateWindow()  # give focus
        except Exception as e:
            self.text_edit.setPlainText(f"Failed to open file:\n{e}")
            self.show()
        
class GraphWidget(QWidget):    
    def __init__(self):
        super().__init__()

        # self.convertFiles(1, 1, False)
        
        self.battShutdownThreshold = 3.2

        self.setWindowTitle("Voltage graph")
        self.setWindowFlags(Qt.FramelessWindowHint)
        self.setAttribute(Qt.WA_OpaquePaintEvent)
        self.setAttribute(Qt.WA_NoSystemBackground)
        # Problem: Mouse x coordinate does not register below 9 on startup, only after switching windows
        # self.showMaximized()
        # self.showFullScreen()
        # screen_rect = QApplication.primaryScreen().availableGeometry()
        # self.setGeometry(screen_rect)
        # print(f"Init: {screen_rect}")
        self.setMouseTracking(True)
        self.mouseMoveEnabled = True # no dragging or zooming before the graph has updated. It takes about 250 ms
        self.mouseScrollEnabled = True
        self.prevMouseScrollTime = time.time()
        self.mouseIncrement = 0.3 # need to be at least the drawing time, otherwise movement will freeze. Right now it is 156 - 178 ms
        self.countSwitchLineY1 = -1
        self.countSwitchLineY2 = -1   
        self.moveCountStartY1 = -1
        self.moveCountStartY2 = -1
        self.toUpdateCount = False
        self.zoomRectStartX = -1
        self.zoomRectStartY = -1
        self.dragStartX = -1
        self.dragStartY = -1
        self.mouseLeftDown = False
        self.mouseRightDown = False
        self.setFocusPolicy(Qt.StrongFocus)
        self.setAttribute(Qt.WA_Hover, True)
        # QTimer.singleShot(0, lambda: self.setFocus(Qt.OtherFocusReason))
        # QTimer.singleShot(0, lambda: self.setMouseTracking(True))
        QTimer.singleShot(0, self.force_fullscreen_fix)
        self.mouseX = 0
        self.mouseY = 0        
        
        self.layout1Bg = QPixmap(800, 40)
        self.layout1Bg.fill(Qt.black)
        self.buttonBar = 30
        self.buttonBarSpace = 10
        self.buttonBarHeight = self.buttonBar + self.buttonBarSpace
        self.scaleMaxH = 799
        self.scaleMaxV = 443 - self.buttonBarHeight
        self.overlay_pixmap = QPixmap(self.scaleMaxH + 1, self.scaleMaxV + self.buttonBarHeight + 1)
        self.buffer = QPixmap(self.scaleMaxH + 1, self.scaleMaxV + self.buttonBarHeight + 1)
        self.chart_pixmap = QPixmap(self.scaleMaxH + 1, self.scaleMaxV + 1)
        self.graph_pixmap = QPixmap(self.scaleMaxH + 1, self.scaleMaxV + 1)
        self.minV = 0
        self.maxV = 0
        self.leftLimit = 0
        self.timeRange = 0

        # layout has no background to change
        self.layout1 = QHBoxLayout()
        self.layout1.setContentsMargins(0, 0, 0, 0); 
        self.layout1.setSpacing(5) 
        self.layout1.setAlignment(Qt.AlignTop)

        self.label1 = QLabel("Counts")
        self.label1.setStyleSheet("color: white")
        self.button1 = QPushButton("Reset")
        self.button1.setStyleSheet("font-size: 20px; font-weight: bold")
        self.button1.setMaximumHeight(30)
        self.button2 = QPushButton("Zoom In")
        self.button2.setStyleSheet("font-size: 20px; font-weight: bold")
        self.button2.setMaximumHeight(30)
        self.button3 = QPushButton("Zoom Out")
        self.button3.setStyleSheet("font-size: 20px; font-weight: bold")
        self.button3.setMaximumHeight(30)
        self.button4 = QPushButton("Move Left")
        self.button4.setStyleSheet("font-size: 20px; font-weight: bold")
        self.button4.setMaximumHeight(30)
        self.button5 = QPushButton("Move Right")
        self.button5.setStyleSheet("font-size: 20px; font-weight: bold")
        self.button5.setMaximumHeight(30)
        self.button6 = QPushButton("Save data")
        self.button6.setStyleSheet("font-size: 20px; font-weight: bold")
        self.button6.setMaximumHeight(30)
        self.button7 = QPushButton("Export image")
        self.button7.setStyleSheet("font-size: 20px; font-weight: bold")
        self.button7.setMaximumHeight(30)
        
        self.layout1.addWidget(self.label1)
        self.layout1.addWidget(self.button1)
        self.layout1.addWidget(self.button2)
        self.layout1.addWidget(self.button3)
        self.layout1.addWidget(self.button4)
        self.layout1.addWidget(self.button5)
        self.layout1.addWidget(self.button6)

        self.cancelTimer = False
        self.button1.clicked.connect(self.resetGraph)
        self.button2.pressed.connect(lambda: self.setZoomStart(True))
        self.button2.released.connect(lambda: self.setZoomEnd())
        self.button3.pressed.connect(lambda: self.setZoomStart(False))
        self.button3.released.connect(lambda: self.setZoomEnd())
        self.button4.pressed.connect(lambda: self.setMoveStart(True))
        self.button4.released.connect(lambda: self.setMoveEnd())
        self.button5.pressed.connect(lambda: self.setMoveStart(False))
        self.button5.released.connect(lambda: self.setMoveEnd())    
        self.button6.clicked.connect(self.saveData)
        self.button7.clicked.connect(self.exportImage)

        self.setLayout(self.layout1)

        # intercept mouse movement when it hovers over a button or a label
        for child in self.findChildren(QWidget):
            child.setMouseTracking(True)
            child.installEventFilter(self)
        
        self.buffer.fill(Qt.transparent)
        self.prev_y1 = 0
        self.prev_y2 = 0
        self.prev_y3 = 0
        self.prev_y4 = 0 

        self.old_settings_liveScaleMin = settings_liveScaleMin
        self.old_settings_liveScaleMax = settings_liveScaleMax

        self.overlay_pixmap.fill(Qt.black)

        p = QPainter(self.overlay_pixmap)
        for i in range(math.ceil(settings_liveScaleMin * 10), math.floor(settings_liveScaleMax * 10) + 1): # at 0, h will be 443 and at 25, h is 0. This corresponds to the 444th and 1st row on the screen.
            h = round(443 - (i / 10 - settings_liveScaleMin) / (settings_liveScaleMax - settings_liveScaleMin) * 443)
            if i % 10 == 0:
                p.setPen(QColor(96, 96, 96))
                p.drawLine(0, h, 800, h)
            else:
                p.setPen(QColor(48, 48, 48))
                p.drawLine(0, h, 800, h)
        p.end()

        # self.timer = QTimer(self)
        # QTimer.singleShot(1, self.update_data) 
        self.timer = QTimer()
        self.timer.setTimerType(Qt.PreciseTimer)
        self.timer.timeout.connect(self.update_graph)
        self.timer.start(10) # avg update rate 13 ms
        self.timerStopped = False
        
        self.c = 0
        # self.startTime = time.time()  
        self.ntime = 0 
        self.totalTime = 0 
        self.min = 100
        self.max = 0
        self.minIndex = 0
        self.maxIndex = 0 

        self.pressed = False
        self.press_start_time = time.time()
        self.firstUpdate = True
        self.pauseStartTime = 0
        # self.SIGNAL_PIN = 24  # pin 18
        # GPIO.setup(self.SIGNAL_PIN, GPIO.IN, pull_up_down=GPIO.PUD_DOWN) 

    def setCountSwitches(self):
        if self.pressed and settings_countSwitches:
            self.label1.show()
        else:
            self.label1.hide()

    def convertFiles(self, low, high, overwrite):
        directory = "/home/fodorbalint/Documents"
        
        pattern = re.compile(r"^voltage4_log_(\d+)\.txt$")

        matching_files = []
        for filename in os.listdir(directory):
            match = pattern.match(filename)
            if match:
                num = int(match.group(1))
                if low <= num <= high:
                    matching_files.append((num, os.path.join(directory, filename)))

        matching_files.sort(key=lambda x: x[0])

        print(f"Found {len(matching_files)} matching files:")
        for num, path in matching_files:
            print(f"  {num}: {path}")

        old_dt = np.dtype([
            ("timestamp_s", np.float32),
            ("ch1", np.float32),
            ("ch2", np.float32),
            ("ch3", np.float32),
            ("ch4", np.float32),
        ])

        startTime = time.time()

        for num, path in matching_files:
            print(f"Converting {path} ...")

            old_values = np.loadtxt(path, dtype=old_dt)
            new_values = np.empty(len(old_values), dtype=combined_dt)

            timestamps_f64 = old_values["timestamp_s"].astype(np.float64)   # compute in float64
            timestamps_us = np.rint(timestamps_f64 * 1_000_000.0).astype("<u8")
            new_values["timestamp_us"] = timestamps_us

            for ch in ["ch1", "ch2", "ch3", "ch4"]:
                new_values[ch] = old_values[ch]

            if overwrite:
                fileName = f"voltage4_log_{num}.txt"
            else:
                fileName = f"voltage4_log_{num}_converted.txt"
            new_path = os.path.join(directory, fileName)
            np.savetxt(
                new_path,
                np.column_stack((
                    new_values["timestamp_us"],
                    new_values["ch1"],
                    new_values["ch2"],
                    new_values["ch3"],
                    new_values["ch4"],
                )),
                fmt=["%d", "%.6f", "%.6f", "%.6f", "%.6f"],
            )

            print(f"Saved → {new_path} in {round((time.time() - startTime)*1000)}")
            time.sleep(1) # so that the files will be listed correctly in descending modification date order

    def openFromFile(self, file_name):
        # try:
        startTime = time.time()

        self.all_values = np.loadtxt(
            "/home/fodorbalint/Documents/" + file_name,
            dtype=combined_dt,
            delimiter=" ",
        )

        print(f"File loaded in {round((time.time() - startTime)*1000)}, length {len(self.all_values)} ts {self.all_values[0][0]} {self.all_values[1][0]} ... {self.all_values[len(self.all_values) - 2][0]} {self.all_values[len(self.all_values) - 1][0]}")

        self.analyzeAllValues()   

        self.pressed = True
        self.press_start_time = time.time() + 1000000
        self.buffer.fill(Qt.transparent)
        self.firstUpdate = True

        showLayout(self.layout1)
        self.button6.hide()
        self.setCountSwitches() 

        # does not work
        self.show()
        # self.showMaximized()
        self.raise_()  # bring to front
        self.activateWindow()  # give focus 
        # using this line the window will show, but only once. And if I set to false, the window disappears
        # self.setWindowFlag(Qt.WindowStaysOnTopHint, True)
        # QTimer.singleShot(0, self.force_fullscreen_fix)

        '''
        if not self.isVisible():
        # self.setWindowFlag(Qt.WindowStaysOnTopHint, True)
            self.setWindowFlags(Qt.Window)  # top-level
            self.show()
        # QTimer.singleShot(5000, lambda: self.setWindowFlag(Qt.WindowStaysOnTopHint, False))
        
        QTimer.singleShot(0, lambda: (self.raise_(), self.activateWindow()))
        self.raise_()  # bring to front
        self.activateWindow()  # give focus
        '''       

        '''except Exception as e:
            print(f"{file_name} is not a valid graph.")
            msg = QMessageBox()
            msg.setIcon(QMessageBox.Information)  # Change icon here
            msg.setWindowTitle("Error")
            msg.setText(f"{file_name} is not a valid graph.")
            msg.setStandardButtons(QMessageBox.Ok)
            msg.exec_()'''

    def analyzeAllValues(self):
        # startTime = time.time()

        self.maxH = 0
        self.maxV = 0
        self.minV = 100

        vals = np.column_stack((self.all_values["ch1"], self.all_values["ch2"], self.all_values["ch3"], self.all_values["ch4"]))
        self.minV = np.min(vals)
        self.maxV = math.ceil(np.max(vals)*10000)/10000

        self.maxH = self.all_values[len(self.all_values) - 1][0]
        if self.minV < 0:
            self.minV = 0
        self.origMinV = self.minV
        self.origMaxV = self.maxV

        # print(f"Analyzed in {round((time.time() - startTime)*1000)} minV {self.minV} maxV {self.maxV}")
        
        arrLen = len(self.all_values)
        newArr = self.all_values
        self.incrementArr = []
        while arrLen >= 800 * 2:
            newArr = self.average_pairs(newArr)
            arrLen = len(newArr)
            self.incrementArr.append(newArr)
            # print(f"New increment length {arrLen} new ts {newArr[0][0]} {newArr[1][0]} {newArr[2][0]} {newArr[3][0]}")

        # print(f"Increments created in {round((time.time() - startTime)*1000)}")

        self.graphZoom = 1
        self.graphMiddle = 0.5

        self.timeRange = round(self.maxH / self.graphZoom)
        self.leftLimit = round(self.graphMiddle * self.maxH - self.timeRange / 2)

        self.drawValues()

        # print(f"Drawn in {round((time.time() - startTime)*1000)}")

    def force_fullscreen_fix(self):
        rect = QApplication.primaryScreen().availableGeometry()
        self.setGeometry(rect)
        self.setFixedSize(self.width(), self.height())   

    def eventFilter(self, obj, event):
        if event.type() == QEvent.MouseMove:
            self.mouseMoveEvent(event)
            return False  # let child still handle it if needed
        return super().eventFilter(obj, event)     

    def mousePressEvent(self, event):
        if event.y() < self.buttonBar:
            return
        if self.pressed:
            if event.y() < self.buttonBarHeight:
                evy = self.buttonBarHeight
            else:
                evy = event.y()

            evx = event.x()
            if evx > 799:
                evx = 799

            if event.button() == Qt.LeftButton:
                self.mouseLeftDown = True

                if settings_countSwitches and evy <= self.countSwitchLineY1 + 5 and evy >= self.countSwitchLineY1 - 5:
                    self.moveCountStartY1 = evy
                    self.countSwitchLineY1Start = self.countSwitchLineY1

                elif settings_countSwitches and evy <= self.countSwitchLineY2 + 5 and evy >= self.countSwitchLineY2 - 5:
                    self.moveCountStartY2 = evy
                    self.countSwitchLineY2Start = self.countSwitchLineY2
                
                else:
                    self.zoomRectStartX = evx               
                    self.zoomRectStartY = evy                    
                    self.displayLeftLimit = 0
                    self.displayTimeRange = 0
                    self.displayMinV = 0
                    self.displayMaxV = 0

            elif event.button() == Qt.RightButton:
                self.mouseRightDown = True
                self.dragStartX = evx
                self.dragStartY = evy
                self.dragMoveX = evx
                
        else:
            if event.button() == Qt.LeftButton:
                self.timerStopped = False
            else:
                self.timerStopped = True
                self.pauseStartTime = self.elapsed

    def mouseMoveEvent(self, event): 
        global settings_switchLow, settings_switchHigh

        mouseMoveTime = time.time()
        # global_pos = QCursor.pos()
        # local_pos = self.mapFromGlobal(global_pos)        
        self.mouseX = event.x()
        self.mouseY = event.y()
        if self.mouseX > 799: # at the right column, event.x() is 799, but it becomes 800 when I move the mouse further to right
            self.mouseX = 799
        if self.mouseY > 443:
            self.mouseY = 443
        '''if event.y() < self.buttonBar:
            self.update()
            return'''
        if self.pressed:
            if self.mouseY < self.buttonBarHeight:
                evy = self.buttonBarHeight
            else:
                evy = event.y()
            
            if self.mouseLeftDown:
                if event.y() >= self.buttonBar:
                    if self.moveCountStartY1 != -1:
                        if self.countSwitchLineY1Start + evy - self.moveCountStartY1 > self.countSwitchLineY2:
                            self.countSwitchLineY1 = self.countSwitchLineY1Start + evy - self.moveCountStartY1 
                            if self.countSwitchLineY1 > 443 - 1: # 1 is for accouting for line thickness
                                self.countSwitchLineY1 = 443 - 1
                            settings_switchLow = round((443 - self.countSwitchLineY1) / (443 - self.buttonBarHeight) * (self.maxV - self.minV) + self.minV, 4)                        

                    elif self.moveCountStartY2 != -1:
                        if self.countSwitchLineY2Start + evy - self.moveCountStartY2 < self.countSwitchLineY1:
                            self.countSwitchLineY2 = self.countSwitchLineY2Start + evy - self.moveCountStartY2 
                            if self.countSwitchLineY2 < self.buttonBarHeight + 1:
                                self.countSwitchLineY2 = self.buttonBarHeight + 1
                            settings_switchHigh = round((443 - self.countSwitchLineY2) / (443 - self.buttonBarHeight) * (self.maxV - self.minV) + self.minV, 4)                      
            
                if self.mouseX != self.zoomRectStartX and evy != self.zoomRectStartY:
                    if self.mouseX > self.zoomRectStartX:
                        startX = self.zoomRectStartX
                        endX = self.mouseX
                    else:
                        startX = self.mouseX
                        endX = self.zoomRectStartX
                    if evy > self.zoomRectStartY:
                        startY = self.zoomRectStartY
                        endY = evy
                    else:
                        startY = evy
                        endY = self.zoomRectStartY

                    self.displayLeftLimit = round(self.leftLimit + startX / 799 * self.timeRange)
                    self.displayTimeRange = round((endX - startX) / 799 * self.timeRange)
                    if self.displayTimeRange < MIN_TIME_RANGE:
                        if self.maxH < MIN_TIME_RANGE:
                            # does not currently happen
                            self.displayLeftLimit = 0
                            self.displayTimeRange = self.maxH
                        else:
                            offset = round((MIN_TIME_RANGE - self.displayTimeRange) / 2)
                            self.displayLeftLimit -= offset
                            self.displayTimeRange = MIN_TIME_RANGE
                            if self.displayLeftLimit < 0:
                                self.displayLeftLimit = 0
                            elif self.displayLeftLimit + self.displayTimeRange > self.maxH:
                                self.displayLeftLimit = self.maxH - self.displayTimeRange

                    self.displayMinV = self.minV + (443 - endY) / (443 - self.buttonBarHeight) * (self.maxV - self.minV)
                    self.displayMaxV = self.maxV - (startY - self.buttonBarHeight) / (443 - self.buttonBarHeight) * (self.maxV - self.minV)

                    if (self.displayMaxV - self.displayMinV) < MIN_V_RANGE:
                        if self.origMaxV - self.origMinV < MIN_V_RANGE:
                            self.displayMinV = self.origMinV
                            self.displayMaxV = self.origMaxV
                        else:
                            # put small range in the center of minimum range, but within the boundaries of the original scale
                            offset = (MIN_V_RANGE - (self.displayMaxV - self.displayMinV)) / 2
                            self.displayMinV -= offset
                            self.displayMaxV += offset
                            if self.displayMinV < self.origMinV:
                                self.displayMinV = self.origMinV
                                self.displayMaxV = self.displayMinV + MIN_V_RANGE
                            elif self.displayMaxV > self.origMaxV:
                                self.displayMaxV = self.origMaxV
                                self.displayMinV = self.displayMaxV - MIN_V_RANGE

            elif self.mouseRightDown and self.mouseMoveEnabled:
                self.mouseMoveEnabled = False
                deltaX = self.mouseX - self.dragMoveX
                self.dragMoveX = self.mouseX
                self.leftLimit -= self.timeRange * deltaX / 799
                self.graphMiddle = (self.leftLimit + self.timeRange / 2) / self.maxH

                self.leftLimit = round(self.graphMiddle * self.maxH - self.timeRange / 2)
                if self.leftLimit < 0:
                    self.leftLimit = 0
                    self.graphMiddle = self.timeRange / 2 / self.maxH
                elif self.leftLimit + self.timeRange > self.maxH:
                    self.leftLimit = self.maxH - self.timeRange
                    self.graphMiddle = (self.maxH - self.timeRange / 2) / self.maxH
                
                self.drawValues()                  

        self.update()  

    def mouseReleaseEvent(self, event):
        global settings_switchLow, settings_switchHigh

        if self.pressed:
            if event.y() < self.buttonBarHeight:
                evy = self.buttonBarHeight
            else:
                evy = event.y()

            evx = event.x()
            if evx > 799:
                evx = 799

            if event.button() == Qt.LeftButton:
                if self.moveCountStartY1 != -1:
                    settingsWindow.textboxA.setText(float_to_str(settings_switchLow))
                    saveSettings("switchLow")
                    self.moveCountStartY1 = -1
                    self.toUpdateCount = True

                elif self.moveCountStartY2 != -1:
                    settingsWindow.textboxB.setText(float_to_str(settings_switchHigh))
                    saveSettings("switchHigh")
                    self.moveCountStartY2 = -1
                    self.toUpdateCount = True

                # first condition is needed when we press over a button and release the mouse over the graph                
                elif self.mouseLeftDown and evx != self.zoomRectStartX and evy != self.zoomRectStartY:
                    self.leftLimit = round(self.displayLeftLimit)
                    self.timeRange = round(self.displayTimeRange)
                    self.graphZoom = self.maxH / self.timeRange 
                    self.graphMiddle = (self.leftLimit + self.timeRange / 2) / self.maxH
                    self.minV = self.displayMinV
                    self.maxV = self.displayMaxV
                    self.drawValues()
                elif self.mouseLeftDown:
                    self.update()

                self.zoomRectStartX = -1
                self.zoomRectStartY = -1

                self.mouseLeftDown = False
            elif event.button() == Qt.RightButton:
                self.mouseRightDown = False
                if evx == self.dragStartX and evy == self.dragStartY:                    
                    self.dragStartX = -1
                    self.dragStartY = -1                    
                    self.resetGraph()

    def wheelEvent(self, event): 
        evx = event.x()
        if evx > 799:
            evx = 799

        mouseScrollTime = time.time()       
        # Positive = scroll up, Negative = scroll down
        delta = event.angleDelta().y() # always +-120 with a physical mouse
        # graph under mouse cursor needs to remain at the same place
        if self.pressed:
            if self.mouseScrollEnabled: 
                self.prevMouseScrollTime = time.time()
                self.mouseScrollEnabled = False                  
                timeAtCursor = self.leftLimit + evx / 799 * self.timeRange
                
                if delta > 0:
                    self.graphZoom *= 1.1
                    self.timeRange = round(self.maxH / self.graphZoom)
                    if self.timeRange < MIN_TIME_RANGE:
                        self.timeRange = MIN_TIME_RANGE
                        self.graphZoom = self.maxH / self.timeRange

                    self.leftLimit = round(timeAtCursor - evx / 799 * self.timeRange)
                    # leftLimit will not be less than 0 or greater than maxH

                    self.graphMiddle = (self.leftLimit + self.timeRange / 2) / self.maxH
                else:
                    self.graphZoom /= 1.1
                    if self.graphZoom < 1:
                        self.graphZoom = 1
                    self.timeRange = round(self.maxH / self.graphZoom)
                    self.leftLimit = round(timeAtCursor - evx / 799 * self.timeRange)
                    if self.leftLimit < 0:
                        self.leftLimit = 0
                        self.graphMiddle = self.timeRange / 2 / self.maxH
                    elif self.leftLimit + self.timeRange > self.maxH:
                        self.leftLimit = self.maxH - self.timeRange
                        self.graphMiddle = (self.maxH - self.timeRange / 2) / self.maxH
                    else:
                        self.graphMiddle = (self.leftLimit + self.timeRange / 2) / self.maxH
                
                self.prevMouseScrollTime = mouseScrollTime
                self.drawValues()
                # print(f"End of scroll, values drawn, time {round((time.time() - self.prevMouseScrollTime) * 1000)}")
        else:
            if delta > 0:
                settingsWindow.setScaleWide(0, 0.1)
            else:
                settingsWindow.setScaleWide(1, 0.1)

    def average_pairs(self, arr):
        """Return a new array half the length, averaging pairs of structured entries."""
        n = len(arr)
        if n % 2 != 0:
            arr = arr[:-1]  # drop last element if odd length

        # Create view of pairs (shape: (n/2, 2))
        pairs = arr.reshape(-1, 2)

        # Average all numeric fields
        result = np.empty(pairs.shape[0], dtype=arr.dtype)
        for name in arr.dtype.names:
            result[name] = pairs[name].mean(axis=1)

        return result

    def update_graph(self):

        read_signal()

        if not self.pressed and signal == 1 and time.time() - self.press_start_time > 1: # switched high
            self.timerStopped = False # for the live graph, it will start normally after switching back to it
            self.pressed = True
            self.timer.stop()
            self.press_start_time = time.time() 

            print(f"Measurement starts")            

            self.all_values = np.empty(int(settings_timeLimit * 50000), dtype=combined_dt)
            duration_us = int(settings_timeLimit * 1000000)
            threshold = int(settings_triggerThreshold * 1000)

            if not settings_trigger:  
                cmd = f"FAST{duration_us:08d}\n" # format with 8 digits, leading zeros
            else:
                offsetChannel = 0
                if settings_triggerChannel == 1:
                    offsetChannel = settings_ch1ground
                if settings_triggerChannel == 2:
                    offsetChannel = settings_ch2ground
                if settings_triggerChannel == 3:
                    offsetChannel = settings_ch3ground
                if settings_triggerChannel == 4:
                    offsetChannel = settings_ch4ground
                offsetChannel = hex(offsetChannel)[2:]
                cmd = f"FAST{duration_us:08d}{settings_triggerChannel}{offsetChannel}"
                if settings_risingEdge:
                    cmd += "1"
                else:
                    cmd += "0"
                cmd += f"{threshold:04d}{settings_offsetPercent:02d}\n"

            print (f"Command: {cmd[:-1]}")

            proc.stdin.write(cmd.encode())
            proc.stdin.flush()

            # threading.Thread(target=reader_logs, args=(proc,), daemon=True).start()

            # hangs
            # for line in proc.stderr:
            #    print("C LOG:", line.decode().rstrip())

            for i in range(0):
                print("C LOG: " + proc.stderr.readline().decode().strip()) 

            '''print("C LOG: " + proc.stderr.readline().decode().strip())  
            print("C LOG: " + proc.stderr.readline().decode().strip()) 
            print("C LOG: " + proc.stderr.readline().decode().strip())'''

            startTime = time.time()

            self.all_values = parse_batch(reader_all(proc, q))

            if len(self.all_values) != 0:
                print(f"Data parsed in {round((time.time() - startTime)*1000)}, length {len(self.all_values)}, first {self.all_values[0]}, last {self.all_values[len(self.all_values) - 1]} ts {self.all_values[0][0]} {self.all_values[1][0]} ... {self.all_values[len(self.all_values) - 2][0]} {self.all_values[len(self.all_values) - 1][0]}")

                self.analyzeAllValues()
            else:
                print (f"Wait interrupted")

                time.sleep(0.001) # reading signal can hang the program without a wait
                read_signal()

                if signal == 0:
                    self.pressed = False
                    self.timer.start()
                    self.buffer.fill(Qt.transparent)
                    self.firstUpdate = True
                else:
                    raise ValueError(f"Signal should be 0, got {signal}")

                return


            '''self.all_values = []
            values = read_all_channels()
            startTime = time.time()
            new_time = 0
            values.insert(0, new_time)
            self.all_values.append(values)

            if not settings_trigger:
                counter = 0
                while new_time < settings_timeLimit:
                    values = read_all_channels()
                    new_time = time.time() - startTime
                    values.insert(0, round(new_time, 4))
                    self.all_values.append(values)
                    counter += 1
            else:'''
            '''
                we may need to pop more than one element if the current time intervals are greater than in the past
                In case of
                
                0
                0.003
                0.006
                0.009
                ...
                0.999
                1.007

                we pop
                
                0
                0.003
                0.006
                The whole period will be less than timeLimit, unlike in the caase without eddge detection.
                Reinsertion needed
                '''
            '''
                offsetStart = 0
                periodStart = 0
                firstElementTime = 0

                while GPIO.input(self.SIGNAL_PIN) == GPIO.HIGH and (offsetStart == 0 or (offsetStart != 0 and new_time - offsetStart < settings_timeLimit * (100 - settings_offsetPercent) / 100)):
                    values = read_all_channels()
                    new_time = time.time() - startTime
                    importantValue = values[settings_triggerChannel-1]
                    if offsetStart == 0 and ((settings_risingEdge and importantValue >= settings_triggerThreshold) or (not settings_risingEdge and importantValue <= settings_triggerThreshold)):
                        print (f"Threshold reached at {new_time}, value: {importantValue}")
                        offsetStart = new_time
                    values.insert(0, round(new_time, 4))
                    self.all_values.append(values)
                    if new_time - periodStart >= settings_timeLimit: 
                        firstElementTime = self.all_values[0][0]                       
                        while new_time - firstElementTime >= settings_timeLimit:   
                            v = self.all_values.pop(0)
                            firstElementTime = self.all_values[0][0]
                        # reinsert first element
                        self.all_values.insert(0, v)
                        periodStart = firstElementTime

                # shift all times towards 0
                firstElementTime = self.all_values[0][0]
                new_time = new_time - firstElementTime

                for v in self.all_values:
                    v[0] = round(v[0] - firstElementTime, 4)            
            
            

            '''

            '''time.sleep(0.001) # reading signal can hang the program without a wait
            read_signal()
            if signal == 0:
                print (f"Wait interrupted")

                self.pressed = False
                self.timer.start()
                self.buffer = QPixmap(self.scaleMaxH + 1, self.scaleMaxV + self.buttonBarHeight + 1)
                self.buffer.fill(Qt.transparent)
                self.firstUpdate = True

                return'''

            # print(f"{len(self.all_values)} data points collected in {self.all_values[len(self.all_values) - 1][0]} µs. -1: {self.all_values[len(self.all_values) - 2][0]} -2: {self.all_values[len(self.all_values) - 3][0]}")

            '''self.maxH = 0
            self.maxV = 0
            self.minV = 100         

            for v in self.all_values:
                timestamp = v[0]
                voltage1 = v[1]
                voltage2 = v[2]
                voltage3 = v[3]
                voltage4 = v[4]
                if settings_measureChannels1 and voltage1 > self.maxV:
                    self.maxV = voltage1
                if settings_measureChannels1 and voltage1 < self.minV:
                    self.minV = voltage1
                if settings_measureChannels2 and voltage2 > self.maxV:
                    self.maxV = voltage2
                if settings_measureChannels2 and voltage2 < self.minV:
                    self.minV = voltage2
                if settings_measureChannels3 and voltage3 > self.maxV:
                    self.maxV = voltage3
                if settings_measureChannels3 and voltage3 < self.minV:
                    self.minV = voltage3
                if settings_measureChannels4 and voltage4 > self.maxV:
                    self.maxV = voltage4
                if settings_measureChannels4 and voltage4 < self.minV:
                    self.minV = voltage4
            self.maxH = timestamp
            if self.minV < 0:
                self.minV = 0
            self.origMinV = self.minV
            self.origMaxV = self.maxV

            print (f"Analyzed in {round((time.time() - startTime)*1000)} MinV: {self.minV} MaxV: {self.maxV} Last time: {self.maxH}")

            arrLen = len(self.all_values)
            newArr = self.all_values
            self.incrementArr = []
            while arrLen >= 800 * 2: # at 1601 or 1600 elements, the final array will contain 800. At 1599, it does not shrink further. 
                i = 0
                sumt = 0
                sumv1 = 0
                sumv2 = 0
                sumv3 = 0
                sumv4 = 0
                newArr2 = []
                for v in newArr:
                    i += 1
                    t = v[0]
                    v1 = v[1]
                    v2 = v[2]
                    v3 = v[3]
                    v4 = v[4]
                    if i % 2 == 1:
                        sumt = t
                        sumv1 = v1
                        sumv2 = v2
                        sumv3 = v3
                        sumv4 = v4
                    else:
                        newT = (sumt + t) / 2
                        newv1 = (sumv1 + v1) / 2
                        newv2 = (sumv2 + v2) / 2
                        newv3 = (sumv3 + v3) / 2
                        newv4 = (sumv4 + v4) / 2
                        newArr2.append([newT, v1, v2, v3, v4])
                arrLen = len(newArr2)
                print (f"New increment length {arrLen}")              
                self.incrementArr.append(newArr2)
                newArr = newArr2

            print(f"Increments created in {round((time.time() - startTime)*1000)}")
            '''

            

            '''msg = QMessageBox()
            msg.setIcon(QMessageBox.Information)  # Change icon here
            msg.setWindowTitle("Switches counted")
            msg.setText(f"There are {switchCount} switches in {self.maxH:.4f} seconds from {len(self.all_values)} samples exceeding {settings_switchHigh} V and deceeding {settings_switchLow} V on channel {settings_switchChannel}. Do you want to save this information?")
            msg.setStandardButtons(QMessageBox.Yes | QMessageBox.No)

            reply = msg.exec_()

            if reply == QMessageBox.Yes:
                nowStr = time.strftime("%Y-%m-%d %H:%M:%S")
                with open(f"/home/fodorbalint/Documents/switch count.txt", "a") as file: 
                    file.write(f"{nowStr}: switches {switchCount}, time {new_time:.4f}, samples {len(self.all_values)}, high threshold {settings_switchHigh}, low threshold {settings_switchLow}, channel {settings_switchChannel}\n")'''
            
            self.timer.start()
            self.buffer.fill(Qt.transparent)
            self.firstUpdate = True

            showLayout(self.layout1) 
            self.button6.setText("Save data")
            self.button6.setEnabled(True)
            self.setCountSwitches()

        # after loading a file, switch up
        elif self.pressed and signal == 1:
            if self.press_start_time > time.time():
                self.press_start_time = time.time()
        elif self.pressed and signal == 0 and time.time() - self.press_start_time > 1: # switched low
            self.pressed = False
            self.press_start_time = time.time()
        # QTimer.singleShot(10, self.update_data) # average whithout this 14 ms. Screen refresh rate: 16.6 ms

        # self.ntime = time.time()

        #n1time = round((self.ntime - self.startTime)*1000, 1)
        #print(f"time0 - start {n1time}")

        if not self.pressed:            
            if self.timerStopped:
                return
                  
            hideLayout(self.layout1)
            self.setCountSwitches()

            if self.firstUpdate:
                self.graph_start_time = time.time()
                self.currentSeconds = -1
                self.lastMovement = -1
                self.pauseStartTime = 0

            now = time.time()
            if self.pauseStartTime > 0:              
                # we only need to offset the start time by the amount that exceeds a normal update period, 16 ms on average.               
                self.graph_start_time += now - self.graph_start_time - (self.pauseStartTime + 0.016)
                self.pauseStartTime = 0

            
            self.elapsed = now - self.graph_start_time
            elapsedSeconds = math.floor(self.elapsed)

            pxPerSecond = 799 / settings_liveScaleWidth
            totalMovement = round(self.elapsed * pxPerSecond)

            # print (f"totalmovement {totalMovement} last {self.lastMovement} first update {self.firstUpdate}")

            # scroll by 1 at zero time
            scrollAmount = totalMovement - self.lastMovement
            self.buffer.scroll(-scrollAmount, 0, self.buffer.rect())
            self.lastMovement = totalMovement
            
            painter = QPainter(self.buffer)
            painter.setCompositionMode(QPainter.CompositionMode_Source)

            painter.fillRect(800 - scrollAmount, 0, scrollAmount, 444, Qt.transparent)
            if elapsedSeconds != self.currentSeconds:
                self.currentSeconds = elapsedSeconds
                painter.fillRect(800 - 1, 0, 1, 444, QColor(96, 96, 96))
           
            self.values = read_all_channels()

            if self.values[4] <= self.battShutdownThreshold:
                self.timer.stop()
                os.system("sudo shutdown now")

            new_y1 = round(443 - (self.values[0] - settings_liveScaleMin) / (settings_liveScaleMax - settings_liveScaleMin) * 443)
            new_y2 = round(443 - (self.values[1] - settings_liveScaleMin) / (settings_liveScaleMax - settings_liveScaleMin) * 443)
            new_y3 = round(443 - (self.values[2] - settings_liveScaleMin) / (settings_liveScaleMax - settings_liveScaleMin) * 443)
            new_y4 = round(443 - (self.values[3] - settings_liveScaleMin) / (settings_liveScaleMax - settings_liveScaleMin) * 443)  
            new_y5 = round(443 - (self.values[4] - settings_liveScaleMin) / (settings_liveScaleMax - settings_liveScaleMin) * 443)            
            if not self.firstUpdate:
                if settings_measureChannels1:
                    painter.setPen(QPen(Qt.yellow, 1))
                    painter.drawLine(800 - scrollAmount - 1, self.prev_y1, 800 - 1, new_y1)
                if settings_measureChannels2:
                    painter.setPen(QPen(Qt.green, 1))
                    painter.drawLine(800 - scrollAmount - 1, self.prev_y2, 800 - 1, new_y2)
                if settings_measureChannels3:
                    painter.setPen(QPen(Qt.cyan, 1))
                    painter.drawLine(800 - scrollAmount - 1, self.prev_y3, 800 - 1, new_y3)
                if settings_measureChannels4:
                    painter.setPen(QPen(Qt.red, 1))
                    painter.drawLine(800 - scrollAmount - 1, self.prev_y4, 800 - 1, new_y4)
                painter.setPen(QPen(Qt.gray, 1))
                painter.drawLine(800 - scrollAmount - 1, self.prev_y5, 800 - 1, new_y5)
            else:
                self.firstUpdate = False
            self.prev_y1 = new_y1
            self.prev_y2 = new_y2
            self.prev_y3 = new_y3
            self.prev_y4 = new_y4
            self.prev_y5 = new_y5

            painter.end()

            #n1time = round((time.time() - self.ntime)*1000, 1)
            #print(f"time4 {n1time}") 

            if self.old_settings_liveScaleMin != settings_liveScaleMin or self.old_settings_liveScaleMax != settings_liveScaleMax:
                self.old_settings_liveScaleMin = settings_liveScaleMin
                self.old_settings_liveScaleMax = settings_liveScaleMax

                self.overlay_pixmap.fill(Qt.black)

                p = QPainter(self.overlay_pixmap)
                for i in range(math.ceil(settings_liveScaleMin * 10), math.floor(settings_liveScaleMax * 10) + 1): # at 0, h will be 443 and at 25, h is 0. This corresponds to the 444th and 1st row on the screen.
                    h = round(443 - (i / 10 - settings_liveScaleMin) / (settings_liveScaleMax - settings_liveScaleMin) * 443)
                    if i % 10 == 0:
                        p.setPen(QColor(96, 96, 96))
                        p.drawLine(0, h, 800, h)
                    else:
                        p.setPen(QColor(48, 48, 48))
                        p.drawLine(0, h, 800, h)
                p.end()

                self.buffer.fill(Qt.transparent)
                self.firstUpdate = True

            #n1time = round((time.time() - self.ntime)*1000, 1)
            #print(f"time5 {n1time}") 

            self.update()  # Schedule repaint

            #n1time = round((time.time() - self.ntime)*1000, 1)
            #print(f"time6 {n1time}") 

            '''
            if self.c < 5000:
                now = time.time()
                interval = (now - self.startTime)*1000
                self.startTime = now
                # print (f"Window size {self.height()} x {self.width()}")
                # first measurement is always too much and second is too little
                avg = 0

                if self.c > 1:
                    self.totalTime += interval
                    avg = self.totalTime / (self.c - 1)
                    if interval < self.min:
                        self.min = interval
                        self.minIndex = self.c
                    if interval > self.max:
                        self.max = interval
                        self.maxIndex = self.c

                if self.c % 100 == 0:
                    print (f"C {self.c} interval {interval:.1f} avg {avg:.1f} min {self.min:.1f} i {self.minIndex} max {self.max:.1f} i {self.maxIndex} v1 {values[0]} v2 {values[1]}")

                self.c += 1
            '''

        # if GPIO.input(self.SIGNAL_PIN) == GPIO.HIGH:
        #    print(f"High at {time.time() - self.press_start_time}") 
        # if GPIO.input(self.SIGNAL_PIN) == GPIO.LOW:
        #    print(f"Low at {time.time() - self.press_start_time}")                  

    def paintEvent(self, event):

        '''if not self.mouseScrollEnabled:
            print(f"Start of paintEvent, time {round((time.time() - self.prevMouseScrollTime) * 1000)}")'''

        #n1time = round((time.time() - self.ntime)*1000, 1)
        #print(f"time7 {n1time}")

        painter = QPainter(self)
        font = QFont("Arial", 12)
        painter.setFont(font)
        metrics = QFontMetrics(font)
        text_height = metrics.height() # 19

        if not self.pressed:            
            painter.drawPixmap(0, 0, self.overlay_pixmap)
            painter.drawPixmap(0, 0, self.buffer) 
            painter.setPen(Qt.gray)
            painter.drawLine(0, self.mouseY, 799, self.mouseY)

            textCount = 0
            text1_width = 0
            text2_width = 0
            text3_width = 0
            text4_width = 0
            if settings_measureChannels1:
                textCount += 1
                text1 = f"{round(self.values[0], 4)}"
                text1_width = metrics.horizontalAdvance(text1)
            if settings_measureChannels2:
                textCount += 1
                text2 = f"{round(self.values[1], 4)}"
                text2_width = metrics.horizontalAdvance(text2)
            if settings_measureChannels3:
                textCount += 1
                text3 = f"{round(self.values[2], 4)}"
                text3_width = metrics.horizontalAdvance(text3)
            if settings_measureChannels4:
                textCount += 1
                text4 = f"{round(self.values[3], 4)}"
                text4_width = metrics.horizontalAdvance(text4)
            
            text = f"{round(settings_liveScaleMin + ((443 - self.mouseY)/443) * (settings_liveScaleMax - settings_liveScaleMin), 4)} V"
            text_width = metrics.horizontalAdvance(text)
            text_width = max(text_width, text1_width, text2_width, text3_width, text4_width)
            rectW = text_width + 10
            rectH = 10 + (textCount + 1) * text_height

            rectX = 10
            if self.mouseY < 443 - 9 - rectH:
                rectY = self.mouseY + 10                    
            else:
                rectY = self.mouseY - 10 - rectH
            
            painter.setPen(Qt.gray)
            background_color = QColor(0, 0, 0, 200)
            rect = QRect(rectX, rectY, rectW, rectH)
            painter.fillRect(rect, background_color)
            painter.drawRect(rect)
            painter.drawText(rectX + 5, rectY + text_height + 1, text)

            offset = 0
            if settings_measureChannels1:
                offset += text_height
                painter.setPen(Qt.yellow)
                painter.drawText(rectX + 5, rectY + text_height + 1 + offset, text1)
            if settings_measureChannels2:
                offset += text_height
                painter.setPen(Qt.green)
                painter.drawText(rectX + 5, rectY + text_height + 1 + offset, text2)
            if settings_measureChannels3:
                offset += text_height
                painter.setPen(Qt.cyan)
                painter.drawText(rectX + 5, rectY + text_height + 1 + offset, text3)
            if settings_measureChannels4:
                offset += text_height
                painter.setPen(Qt.red)
                painter.drawText(rectX + 5, rectY + text_height + 1 + offset, text4)            
            painter.end()
        else:
            painter.drawPixmap(0, 0, self.layout1Bg)
            painter.drawPixmap(0, self.buttonBarHeight, self.chart_pixmap)

            if settings_countSwitches:
                if self.countSwitchLineY1 == -1 and self.countSwitchLineY2 == -1:
                    self.countSwitchLineY1 = int(443 - (settings_switchLow - self.minV) / (self.maxV - self.minV) * (443 - self.buttonBarHeight))
                    self.countSwitchLineY2 = int(443 - (settings_switchHigh - self.minV) / (self.maxV - self.minV) * (443 - self.buttonBarHeight))

                # print(f"Swline {y1} {y2} {self.minV} {self.maxV}")

                if self.countSwitchLineY1 >= self.buttonBarHeight and self.countSwitchLineY1 <= 443:
                    if settings_switchChannel == 1:
                        painter.setPen(Qt.darkYellow)
                    if settings_switchChannel == 2:
                        painter.setPen(Qt.darkGreen)
                    if settings_switchChannel == 3:
                        painter.setPen(Qt.darkCyan)
                    if settings_switchChannel == 4:
                        painter.setPen(Qt.darkRed)

                    painter.drawLine(0, self.countSwitchLineY1 - 1, 799, self.countSwitchLineY1 - 1)
                    painter.drawLine(0, self.countSwitchLineY1, 799, self.countSwitchLineY1)
                    painter.drawLine(0, self.countSwitchLineY1 + 1, 799, self.countSwitchLineY1 + 1)

                if self.countSwitchLineY2 >= self.buttonBarHeight and self.countSwitchLineY2 <= 443:
                    if settings_switchChannel == 1:
                        painter.setPen(Qt.darkYellow)
                    if settings_switchChannel == 2:
                        painter.setPen(Qt.darkGreen)
                    if settings_switchChannel == 3:
                        painter.setPen(Qt.darkCyan)
                    if settings_switchChannel == 4:
                        painter.setPen(Qt.darkRed)

                    painter.drawLine(0, self.countSwitchLineY2 - 1, 799, self.countSwitchLineY2 - 1)
                    painter.drawLine(0, self.countSwitchLineY2, 799, self.countSwitchLineY2)
                    painter.drawLine(0, self.countSwitchLineY2 + 1, 799, self.countSwitchLineY2 + 1) 

            # draw graph above count switch lines but below their dragging info box

            painter.drawPixmap(0, self.buttonBarHeight, self.graph_pixmap)

            # applies to: count switch dragging info box, actual position info box and zoom ractangle. The actual position info box should disappear when we go above the button bar, the count switch dragging info box should be only move horizontally, and the zoom rectangle should update horizontally as well.  

            if self.mouseY < self.buttonBarHeight:
                evy = self.buttonBarHeight
            else:
                evy = self.mouseY

            # info box for dragging count switch line

            if settings_countSwitches and self.moveCountStartY1 != -1 or self.moveCountStartY2 != -1:

                painter.setPen(Qt.gray)
                
                if self.moveCountStartY1 != -1:
                    text = f"Low threshold: {settings_switchLow}"
                else:
                    text = f"High threshold: {settings_switchHigh}"
                text_width = metrics.horizontalAdvance(text) 
                rectW = text_width + 10
                rectH = 10 + text_height

                if self.mouseX < 799 - 9 - rectW:
                    rectX = self.mouseX + 10                    
                else:
                    rectX = self.mouseX - 10 - rectW                
                if evy < 443 - 9 - rectH:
                    rectY = evy + 10                    
                else:
                    rectY = evy - 10 - rectH

                background_color = QColor(0, 0, 0, 200)
                rect = QRect(rectX, rectY, rectW, rectH)
                painter.fillRect(rect, background_color)
                painter.drawRect(rect)
                painter.drawText(rectX + 5, rectY + text_height + 1, text)          
                     
            # info box that updates when mouse moves

            textCount = 0 # used in the zoom rectangle too
            if settings_measureChannels1:
                    textCount += 1
            if settings_measureChannels2:
                    textCount += 1
            if settings_measureChannels3:
                    textCount += 1
            if settings_measureChannels4:
                    textCount += 1

            if self.moveCountStartY1 == -1 and self.moveCountStartY2 == -1 and self.mouseY >= self.buttonBar:

                painter.setPen(Qt.gray)
                painter.drawLine(self.mouseX, self.buttonBarHeight, self.mouseX, 443)
                painter.drawLine(0, evy, 799, evy)

                if settings_measureChannels1:
                    value1 = self.displayed_values[self.mouseX][0]
                    if value1 == -1 and self.mouseX != 0:
                        i = self.mouseX - 1
                        while i >= 0 and self.displayed_values[i][0] == -1:
                            i -= 1
                        value1 = self.displayed_values[i][0]

                        '''
                        # calculate incremental value if there is no measurement at this point
                        lowIndex = i                        
                        lowValue = self.displayed_values[i][0]
                        i = self.mouseX + 1
                        while i <= 799 and self.displayed_values[i][0] == -1:
                            i += 1
                        highIndex = i
                        if i == 800:
                            highValue = lowValue
                        else: 
                            highValue = self.displayed_values[i][0]
                        value1 = lowValue + (highValue - lowValue) * (self.mouseX - lowIndex) / (highIndex - lowIndex)'''
                    text3 = f"{round(value1, 4)}"
                if settings_measureChannels2:
                    value2 = self.displayed_values[self.mouseX][1]
                    if value2 == -1 and self.mouseX != 0:
                        i = self.mouseX - 1
                        while i >= 0 and self.displayed_values[i][1] == -1:
                            i -= 1
                        value2 = self.displayed_values[i][1]
                    text4 = f"{round(value2, 4)}"
                if settings_measureChannels3:
                    value3 = self.displayed_values[self.mouseX][2]
                    if value3 == -1 and self.mouseX != 0:
                        i = self.mouseX - 1
                        while i >= 0 and self.displayed_values[i][2] == -1:
                            i -= 1
                        value3 = self.displayed_values[i][2]
                    text5 = f"{round(value3, 4)}"
                if settings_measureChannels4:
                    value4 = self.displayed_values[self.mouseX][3]
                    if value4 == -1 and self.mouseX != 0:
                        i = self.mouseX - 1
                        while i >= 0 and self.displayed_values[i][3] == -1:
                            i -= 1
                        value4 = self.displayed_values[i][3]
                    text6 = f"{round(value4, 4)}"
                
                if len(self.new_values) >= 800 and self.displayed_values[self.mouseX][0] != -1:
                    displayTimestamp = round(self.leftLimit + self.mouseX / 799 * self.timeRange)
                else:               
                    displayTimestamp = -1
                    lastDisplayTimestamp = -1
                    for v in self.new_values:
                        timestamp = float(v[0] - self.leftLimit)
                        x = round(799 * timestamp / self.timeRange)
                        lastDisplayTimestamp = v[0]                            
                        if self.mouseX >= x:
                            displayTimestamp = lastDisplayTimestamp

                    # mouse x from 0 to 799. Index from 0 to length - 1
                    # index = math.floor((len(self.new_values) - 1) * self.mouseX / 799)
                    # displayTimestamp = self.new_values[index][0]

                rectX = 10
                painter.setPen(Qt.gray)

                if displayTimestamp != -1:                    
                    if evy < 443 - 19 - (textCount + 2) * text_height:
                        rectY = evy + 10                    
                    else:
                        rectY = evy - 20 - (textCount + 2) * text_height                    
                    
                    text1 = f"{round(self.minV + ((self.scaleMaxV - (self.mouseY - self.buttonBarHeight))/self.scaleMaxV) * (self.maxV - self.minV), 4)} V"
                    text2 = f"{formatN(displayTimestamp)} µs"
                    text1_width = metrics.horizontalAdvance(text1)          
                    text2_width = metrics.horizontalAdvance(text2)                    
                    rectW = max(text1_width, text2_width) + 10
                    rectH = 10 + (textCount + 2) * text_height

                    background_color = QColor(0, 0, 0, 200)
                    rect = QRect(rectX, rectY, rectW, rectH)
                    painter.fillRect(rect, background_color)
                    painter.drawRect(rect)
                    painter.drawText(rectX + 5, rectY + text_height + 1, text1)
                    painter.drawText(rectX + 5, rectY + 2 * text_height + 1, text2)

                    offset = text_height
                    if settings_measureChannels1:
                        offset += text_height
                        painter.setPen(Qt.yellow)
                        painter.drawText(rectX + 5, rectY + text_height + 1 + offset, text3)
                    if settings_measureChannels2:
                        offset += text_height
                        painter.setPen(Qt.green)
                        painter.drawText(rectX + 5, rectY + text_height + 1 + offset, text4)
                    if settings_measureChannels3:
                        offset += text_height
                        painter.setPen(Qt.cyan)
                        painter.drawText(rectX + 5, rectY + text_height + 1 + offset, text5)
                    if settings_measureChannels4:
                        offset += text_height
                        painter.setPen(Qt.red)
                        painter.drawText(rectX + 5, rectY + text_height + 1 + offset, text6)
                else:
                    rectX = 10
                    if self.mouseY < 443 - 19 - text_height:
                        rectY = self.mouseY + 10                    
                    else:
                        rectY = self.mouseY - 20 - text_height
                    
                    painter.setFont(QFont("Arial", 12))
                    text = f"{round(self.minV + ((self.scaleMaxV - (self.mouseY - self.buttonBarHeight))/self.scaleMaxV) * (self.maxV - self.minV), 4)} V"
                    text_width = metrics.horizontalAdvance(text)
                    rectW = text_width + 10 
                    rectH = 10 + text_height
                    
                    background_color = QColor(0, 0, 0, 200)
                    rect = QRect(rectX, rectY, rectW, rectH)
                    painter.fillRect(rect, background_color)
                    painter.drawRect(rect)
                    painter.drawText(rectX + 5, rectY + text_height, text)

            # zoom rectangle

            if self.mouseLeftDown and self.zoomRectStartX != -1 and self.zoomRectStartY != -1 and self.displayMaxV != 0:

                pen = QPen(Qt.white, 1)
                painter.setPen(pen)

                text1 = f"{self.displayLeftLimit} - {self.displayLeftLimit + self.displayTimeRange} µs"
                text2 = f"{self.displayMinV:.4f} - {self.displayMaxV:.4f} V"
                text1_width = metrics.horizontalAdvance(text1)          
                text2_width = metrics.horizontalAdvance(text2)         
                rectW = max(text1_width, text2_width) + 10
                rectH = 10 + (textCount + 2) * text_height

                if self.mouseX < 799 - 9 - rectW:
                    rectX = self.mouseX + 10                    
                else:
                    rectX = self.mouseX - 10 - rectW
                if evy < 443 - 9 - rectH:
                    rectY = evy + 10                    
                else:
                    rectY = evy - 10 - rectH

                painter.setPen(Qt.gray)
                background_color = QColor(0, 0, 0, 200)
                rect = QRect(rectX, rectY, rectW, rectH)
                painter.fillRect(rect, background_color)
                painter.drawRect(rect)
                painter.drawText(rectX + 5, rectY + text_height + 1, text1)                
                painter.drawText(rectX + 5, rectY + 2 * text_height + 1, text2)

                painter.setPen(Qt.white)
                painter.setBrush(Qt.NoBrush)  # no fill
                painter.drawRect(self.zoomRectStartX, self.zoomRectStartY, self.mouseX - self.zoomRectStartX, evy - self.zoomRectStartY)

                rightLimit = self.displayLeftLimit + self.displayTimeRange
                sum1 = 0
                sum2 = 0
                sum3 = 0
                sum4 = 0
                count = 0
                
                # startTime = time.time()

                # Select elements within timestamp range
                mask = (self.all_values["timestamp_us"] >= self.displayLeftLimit) & (self.all_values["timestamp_us"] <= rightLimit)

                # Count how many elements are included
                count = np.count_nonzero(mask)

                # Apply mask to each numeric field and sum
                result = {
                    name: self.all_values[name][mask].sum(dtype=np.float64)
                    for name in ("ch1", "ch2", "ch3", "ch4")
                }
                
                # up to 10 ms for 42000 samples. If only self.new_values are used, it is 1 ms.
                # print(f"paintEvent: {len(self.new_values)} {round((time.time() - startTime)*1000)}") 
                        
                offset = text_height
                if settings_measureChannels1:
                    offset += text_height
                    painter.setPen(Qt.yellow)
                    painter.drawText(rectX + 5, rectY + text_height + 1 + offset, str(round(result["ch1"] / count, 4)))
                if settings_measureChannels2:
                    offset += text_height
                    painter.setPen(Qt.green)
                    painter.drawText(rectX + 5, rectY + text_height + 1 + offset, str(round(result["ch2"] / count, 4)))
                if settings_measureChannels3:
                    offset += text_height
                    painter.setPen(Qt.cyan)
                    painter.drawText(rectX + 5, rectY + text_height + 1 + offset, str(round(result["ch3"] / count, 4)))
                if settings_measureChannels4:
                    offset += text_height
                    painter.setPen(Qt.red)
                    painter.drawText(rectX + 5, rectY + text_height + 1 + offset, str(round(result["ch4"] / count, 4)))
                        
                painter.end()  
            '''if not self.mouseScrollEnabled:
                print(f"End of paintEvent, time {round((time.time() - self.prevMouseScrollTime) * 1000)}")'''
            self.mouseMoveEnabled = True
            self.mouseScrollEnabled = True 

            if self.toUpdateCount:
                self.toUpdateCount = False
                self.countSwitches()        

    def resetGraph(self):
        self.timeRange = self.maxH
        self.leftLimit = 0
        self.minV = self.origMinV
        self.maxV = self.origMaxV
        self.graphZoom = 1
        self.graphMiddle = 0.5
        self.drawValues()

    def setZoomStart(self, direction):
        if direction:
            self.graphZoom *= 1.05
        else:
            self.graphZoom /= 1.05
        if self.graphZoom < 1:
            self.graphZoom = 1

        self.timeRange = round(self.maxH / self.graphZoom)
        if self.timeRange < MIN_TIME_RANGE:
            self.timeRange = MIN_TIME_RANGE
            self.graphZoom = self.maxH / self.timeRange 
        self.leftLimit = round(self.graphMiddle * self.maxH - self.timeRange / 2)        
        if self.leftLimit < 0:
            self.leftLimit = 0
            self.graphMiddle = self.timeRange / 2 / self.maxH
        elif self.leftLimit + self.timeRange > self.maxH:
            self.leftLimit = self.maxH - self.timeRange
            self.graphMiddle = (self.maxH - self.timeRange / 2) / self.maxH

        self.drawValues()

        if not self.cancelTimer:
            QTimer.singleShot(1, lambda: self.setZoomStart(direction))
        else:
            self.cancelTimer = False

    def setZoomEnd(self):
        self.cancelTimer = True

    def setMoveStart(self, direction):
        if direction:
            self.graphMiddle -= 0.05 * self.maxH / self.graphZoom
        else:
            self.graphMiddle += 0.05 * self.maxH / self.graphZoom

        self.leftLimit = round(self.graphMiddle * self.maxH - self.timeRange / 2)
        if self.leftLimit < 0:
            self.leftLimit = 0
            self.graphMiddle = self.timeRange / 2 / self.maxH
        elif self.leftLimit + self.timeRange > self.maxH:
            self.leftLimit = self.maxH - self.timeRange
            self.graphMiddle = (self.maxH - self.timeRange / 2) / self.maxH

        self.drawValues() 

        if not self.cancelTimer:
            QTimer.singleShot(1, lambda: self.setMoveStart(direction))
        else:
            self.cancelTimer = False

    def setMoveEnd(self):
        self.cancelTimer = True

    def saveData(self):
        ch1ground = hex(settings_ch1ground)[2:]
        ch2ground = hex(settings_ch2ground)[2:]
        ch3ground = hex(settings_ch3ground)[2:]
        ch4ground = hex(settings_ch4ground)[2:]
        cmd = f"SAVE{ch1ground}{ch2ground}{ch3ground}{ch4ground}\n"
        proc.stdin.write(cmd.encode())
        proc.stdin.flush()
        self.button6.setText("Saving...")

        print("C LOG: " + proc.stderr.readline().decode().strip())

        # response_bytes = proc.stdout.read(4)
        # (response,) = struct.unpack("<I", response_bytes)

        # print(f"Saved {response} frames")
        self.button6.setText("Saved")
        self.button6.setEnabled(False)

        '''directory_path = "/home/fodorbalint/Documents"
        highest_number = get_highest_number(directory_path)
        highest_number += 1 

        content = ""
        for v in self.all_values:
            for v2 in v:                    
                content += str(v2) + " "
            content = content[0:len(content)-1] + "\n"
        with open(f"/home/fodorbalint/Documents/voltage4_log_{highest_number}.txt", "w") as file:
            file.write(content)
        print (f"voltage4_log_{highest_number}.txt saved.")'''
        settingsWindow.refreshList()

    def exportImage(self):
        combined = QPixmap(self.scaleMaxH + 1, self.scaleMaxV + 1)
        combined.fill()

        painter = QPainter(combined)
        painter.drawPixmap(0, 0, self.chart_pixmap)
        painter.drawPixmap(0, 0, self.graph_pixmap)
        painter.end()

        directory_path = "/home/fodorbalint/Documents"
        highest_number = get_highest_number2(directory_path)
        highest_number += 1 

        combined.save(directory_path + f"/voltage4_export_{highest_number}.png")

        print(f"voltage4_export_{highest_number}.png saved.")

    def drawValues(self):  
        if self.maxV == 0: return

        voltageGrid = 0.1
        voltage2Grid = 1
        if self.timeRange < 1000000:
            timeGrid = 10000
            time2Grid = 100000
        else:
            timeGrid = 100000
            time2Grid = 1000000

        stime = time.time() # update time 717-752 ms when I zoom in a 5 s chart

        leftLimit = self.leftLimit
        rightLimit = round(self.leftLimit + self.timeRange)

        # print(f"Middle {self.graphMiddle} leftLimit {self.leftLimit} rightLimit {rightLimit} timeRange {self.timeRange} minV {self.minV} maxV {self.maxV}")

        self.chart_pixmap.fill(Qt.black)
        p = QPainter(self.chart_pixmap)

        for i in range(math.ceil(self.minV/voltageGrid), math.floor(self.maxV/voltageGrid) + 1):
            y = round(self.scaleMaxV*(self.maxV-i*voltageGrid)/(self.maxV-self.minV))
            if i*voltageGrid % voltage2Grid == 0:
                p.setPen(QColor(96, 96, 96))                    
            else:
                p.setPen(QColor(48, 48, 48))
            p.drawLine(0, y, self.scaleMaxH, y)

        # time1 = time.time() - stime
        # print(f"1, voltage scale: {round(time1*1000)}")
        
        # example: Range 0.15 to 1.25
        # loop: from 2 to 12  
        # timestamp: 0.2 to 1.2
        # timeRange: 1.1      
        # lines at 0.2 0.3 ... 1.2
        for i in range(math.ceil(leftLimit / timeGrid), math.floor(rightLimit / timeGrid) + 1):  
            timeStamp = i * timeGrid 
            x = round(self.scaleMaxH * (timeStamp - leftLimit) / self.timeRange)
            if i % (time2Grid / timeGrid) == 0:
                p.setPen(QColor(96, 96, 96))                    
            else:
                p.setPen(QColor(48, 48, 48))
            p.drawLine(x, 0, x, self.scaleMaxV)
        p.end()

        # time2 = time.time() - stime
        # print(f"2, time scale: {round((time2 - time1)*1000)}, total {round(time2*1000)}")
        
        '''
        if self.timeRange == self.maxH:
            new_values = self.all_values
        else:
        '''
        new_values = []
        values = self.all_values # 206 -> 190 ms            
        '''
        # Orlginal algorithm takes 180 ms for a 5-second graph
        for v in values: # 190 ms
            # value = values[i] does not make the loop faster
            timestamp = v[0]
            if timestamp >= leftLimit and timestamp <= rightLimit:
                timestamp = round(timestamp - leftLimit, 4)
                new_values.append([timestamp, v[1], v[2], v[3], v[4]])
        '''
        # New algorithm that halves each interval to find the item at the left and right limit takes 0-1 ms
        incrementIndex = 0

        while True:
            lowIndex = 0 
            highIndex = len(values) - 1                     
            newIndex = math.floor(highIndex / 2) # 0 1 2 3 -> 1, 0 1 2 3 4 -> 2 
            timestamp = values[newIndex][0]
            # print(f"leftLimit {leftLimit} highIndex {highIndex}")
            while newIndex != lowIndex:                
                if timestamp < leftLimit: 
                    lowIndex = newIndex                   
                    newIndex = math.floor((newIndex + highIndex) / 2)                    
                    timestamp = values[newIndex][0]                    
                else:
                    highIndex = newIndex
                    newIndex = math.floor((newIndex + lowIndex) / 2)
                    timestamp = values[newIndex][0]                 
            # found timestamp is just below or equal to leftLimit
            if values[newIndex][0] < leftLimit:
                lowLimitIndex = newIndex + 1
            else:
                lowLimitIndex = newIndex
            # print(f"final {newIndex} timestamp {timestamp} next timestamp {values[newIndex + 1][0] }")

            lowIndex = 0 
            highIndex = len(values) - 1                      
            newIndex = math.ceil(highIndex / 2) # 0 1 2 3 -> 1, 0 1 2 3 4 -> 2 
            timestamp = values[newIndex][0]
            # print(f"rightLimit {rightLimit} highIndex {highIndex}")
            while newIndex != highIndex:                
                if timestamp > rightLimit: 
                    highIndex = newIndex                   
                    newIndex = math.ceil((newIndex + lowIndex) / 2)                    
                    timestamp = values[newIndex][0]                    
                else:
                    lowIndex = newIndex
                    newIndex = math.ceil((newIndex + highIndex) / 2)
                    timestamp = values[newIndex][0]                   
            # found timestamp is just above or equal to rightLimit
            if values[newIndex][0] > rightLimit:
                highLimitIndex = newIndex - 1
            else:
                highLimitIndex = newIndex
            # print(f"final {newIndex} timestamp {timestamp} next timestamp {values[newIndex - 1][0] }")

            new_values = values[lowLimitIndex:highLimitIndex+1] # slice array from lowLimitIndex to highLimitIndex
            self.new_values = new_values

            if len(new_values) < 800 * 2:
                break              

            values = self.incrementArr[incrementIndex]
            incrementIndex += 1              

        # print (f"Final length: {len(new_values)} low {new_values[0][0]} high {new_values[len(new_values) - 1][0]}")                  

        # time3 = time.time() - stime
        # print(f"3, found start and end indices: {round((time3 - time2)*1000)}, total {round(time3*1000)}")

        # print(f"New values count {len(new_values)} first time {new_values[0][0]} last time {new_values[len(new_values) - 1][0]}")

        # average measurement points that would appear in the same column
        '''self.displayed_values1 = [-1] * 800
        self.displayed_values2 = [-1] * 800
        self.displayed_values3 = [-1] * 800
        self.displayed_values4 = [-1] * 800
        actual_col = -1
        voltage1sum = 0
        voltage2sum = 0
        voltage3sum = 0
        voltage4sum = 0
        voltage_count = 0

        for i in range(0, len(new_values)):
            timestamp = float(new_values[i][0] - leftLimit)
            voltage1 = float(new_values[i][1])
            voltage2 = float(new_values[i][2])
            voltage3 = float(new_values[i][3])
            voltage4 = float(new_values[i][4])
            x = round(799 * timestamp / self.timeRange)

            if x == actual_col:
                voltage1sum += voltage1
                voltage2sum += voltage2
                voltage3sum += voltage3
                voltage4sum += voltage4
                voltage_count += 1
            elif actual_col != -1:
                self.displayed_values1[actual_col] = voltage1sum/voltage_count
                self.displayed_values2[actual_col] = voltage2sum/voltage_count
                self.displayed_values3[actual_col] = voltage3sum/voltage_count
                self.displayed_values4[actual_col] = voltage4sum/voltage_count
                voltage1sum = voltage1
                voltage2sum = voltage2
                voltage3sum = voltage3
                voltage4sum = voltage4
                voltage_count = 1
                actual_col = x
            else:
                voltage1sum = voltage1
                voltage2sum = voltage2
                voltage3sum = voltage3
                voltage4sum = voltage4
                voltage_count = 1
                actual_col = x
        self.displayed_values1[actual_col] = voltage1sum/voltage_count
        self.displayed_values2[actual_col] = voltage2sum/voltage_count
        self.displayed_values3[actual_col] = voltage3sum/voltage_count
        self.displayed_values4[actual_col] = voltage4sum/voltage_count
        '''

        timestamps = new_values["timestamp_us"].astype(float) # using int would only draw a portion of the graph 
        channels = np.column_stack((new_values["ch1"], new_values["ch2"],
                                    new_values["ch3"], new_values["ch4"]))  # shape (N, 4)

        # Compute bin indices for each timestamp
        x = np.round(799 * (timestamps - leftLimit) / self.timeRange).astype(int)
        x = np.clip(x, 0, 799)

        # Prepare accumulation arrays
        sum_values = np.zeros((800, 4), dtype=float)
        count_values = np.bincount(x, minlength=800)

        # Sum all channels efficiently per bin
        for i in range(4):
            sum_values[:, i] = np.bincount(x, weights=channels[:, i], minlength=800)


        # Avoid division by zero (only average where count > 0)
        mask = count_values > 0
        self.displayed_values = np.full((800, 4), -1.0, dtype=float)
        self.displayed_values[mask] = sum_values[mask] / count_values[mask, None]

        # time4 = time.time() - stime
        # print(f"4, averaging into columns: {round((time4 - time3)*1000)}, total {round(time4*1000)}")

        self.graph_pixmap.fill(Qt.transparent)
        p = QPainter(self.graph_pixmap)

        # print(f"DrawValues {self.displayed_values1[0]} {self.displayed_values1[1]} {self.displayed_values1[2]}, {new_values[0][0]} {new_values[1][0]} {new_values[2][0]}, {self.all_values[0][0]} {self.all_values[1][0]} {self.all_values[2][0]}, {self.incrementArr[incrementIndex - 1][0][0]}")

        '''old_voltage1 = self.displayed_values[0][0]
        old_voltage2 = self.displayed_values[0][1]
        old_voltage3 = self.displayed_values[0][2]
        old_voltage4 = self.displayed_values[0][3]

        startX = 0

        for i in range(1, len(self.displayed_values1)):                   
            voltage1 = self.displayed_values[i][0]
            voltage2 = self.displayed_values[i][1] 
            voltage3 = self.displayed_values[i][2]
            voltage4 = self.displayed_values[i][3]     
            if voltage1 == -1: continue         
            
            if old_voltage1 != -1:
                if settings_measureChannels1:
                    y1 = round(self.scaleMaxV*(self.maxV-old_voltage1)/(self.maxV-self.minV))
                    y2 = round(self.scaleMaxV*(self.maxV-voltage1)/(self.maxV-self.minV))
                    p.setPen(Qt.yellow)
                    p.drawLine(startX, y1, i, y2)
                
                if settings_measureChannels2:
                    y1 = round(self.scaleMaxV*(self.maxV-old_voltage2)/(self.maxV-self.minV))
                    y2 = round(self.scaleMaxV*(self.maxV-voltage2)/(self.maxV-self.minV))
                    p.setPen(Qt.green)
                    p.drawLine(startX, y1, i, y2)

                if settings_measureChannels3:
                    y1 = round(self.scaleMaxV*(self.maxV-old_voltage3)/(self.maxV-self.minV))
                    y2 = round(self.scaleMaxV*(self.maxV-voltage3)/(self.maxV-self.minV))
                    p.setPen(Qt.cyan)
                    p.drawLine(startX, y1, i, y2)

                if settings_measureChannels4:
                    y1 = round(self.scaleMaxV*(self.maxV-old_voltage4)/(self.maxV-self.minV))
                    y2 = round(self.scaleMaxV*(self.maxV-voltage4)/(self.maxV-self.minV))
                    p.setPen(Qt.red)
                    p.drawLine(startX, y1, i, y2)

            old_voltage1 = voltage1
            old_voltage2 = voltage2
            old_voltage3 = voltage3
            old_voltage4 = voltage4
            startX = i'''

        # displayed_values: shape (800, 4)
        v1 = self.displayed_values[:, 0]
        # Mask out -1 (invalid)
        valid = v1 != -1
        indices = np.arange(len(v1))[valid]

        if len(indices) < 2:
            return  # nothing to draw

        if settings_measureChannels1:
            y1= np.round(self.scaleMaxV * (self.maxV - v1[valid]) / (self.maxV - self.minV))
            points1 = [QPointF(i, y1) for i, y1 in zip(indices, y1)]
            p.setPen(Qt.yellow)
            p.drawPolyline(QPolygonF(points1))
        if settings_measureChannels2:
            v2 = self.displayed_values[:, 1]
            y2= np.round(self.scaleMaxV * (self.maxV - v2[valid]) / (self.maxV - self.minV))
            points2 = [QPointF(i, y2) for i, y2 in zip(indices, y2)]
            p.setPen(Qt.green)
            p.drawPolyline(QPolygonF(points2))
        if settings_measureChannels3:
            v3 = self.displayed_values[:, 2]
            y3= np.round(self.scaleMaxV * (self.maxV - v3[valid]) / (self.maxV - self.minV))
            points3 = [QPointF(i, y3) for i, y3 in zip(indices, y3)]
            p.setPen(Qt.cyan)
            p.drawPolyline(QPolygonF(points3))
        if settings_measureChannels4: 
            v4 = self.displayed_values[:, 3]
            y4= np.round(self.scaleMaxV * (self.maxV - v4[valid]) / (self.maxV - self.minV))           
            points4 = [QPointF(i, y4) for i, y4 in zip(indices, y4)]
            p.setPen(Qt.red)
            p.drawPolyline(QPolygonF(points4))

        p.end()

        self.update()

        if settings_countSwitches:
            self.countSwitchLineY1 = int(443 - (settings_switchLow - self.minV) / (self.maxV - self.minV) * (443 - self.buttonBarHeight))
            self.countSwitchLineY2 = int(443 - (settings_switchHigh - self.minV) / (self.maxV - self.minV) * (443 - self.buttonBarHeight))
            self.toUpdateCount = True            

        time5 = time.time() - stime
        # print(f"5, drawn graph: {round((time5 - time4)*1000)}, total {round(time5*1000)}")
        # print(f"Drawn graph in {round(time5*1000)}")
    
    def countSwitches(self):
        startTime = time.time()

        leftLimit = self.leftLimit
        rightLimit = round(self.leftLimit + self.timeRange)  
        values = self.all_values

        # slice the array of all values within the current time range, algorithm copied from drawValues()
        lowIndex = 0 
        highIndex = len(values) - 1                     
        newIndex = math.floor(highIndex / 2) # 0 1 2 3 -> 1, 0 1 2 3 4 -> 2 
        timestamp = values[newIndex][0]
        # print(f"leftLimit {leftLimit} highIndex {highIndex}")
        while newIndex != lowIndex:                
            if timestamp < leftLimit: 
                lowIndex = newIndex                   
                newIndex = math.floor((newIndex + highIndex) / 2)                    
                timestamp = values[newIndex][0]                    
            else:
                highIndex = newIndex
                newIndex = math.floor((newIndex + lowIndex) / 2)
                timestamp = values[newIndex][0]                 
        # found timestamp is just below or equal to leftLimit
        if values[newIndex][0] < leftLimit:
            lowLimitIndex = newIndex + 1
        else:
            lowLimitIndex = newIndex

        lowIndex = 0 
        highIndex = len(values) - 1                      
        newIndex = math.ceil(highIndex / 2) # 0 1 2 3 -> 1, 0 1 2 3 4 -> 2 
        timestamp = values[newIndex][0]
        # print(f"rightLimit {rightLimit} highIndex {highIndex}")
        while newIndex != highIndex:                
            if timestamp > rightLimit: 
                highIndex = newIndex                   
                newIndex = math.ceil((newIndex + lowIndex) / 2)                    
                timestamp = values[newIndex][0]                    
            else:
                lowIndex = newIndex
                newIndex = math.ceil((newIndex + highIndex) / 2)
                timestamp = values[newIndex][0]                   
        # found timestamp is just above or equal to rightLimit
        if values[newIndex][0] > rightLimit:
            highLimitIndex = newIndex - 1
        else:
            highLimitIndex = newIndex

        new_values = values[lowLimitIndex:highLimitIndex+1] # slice array from lowLimitIndex to highLimitIndex

        # 2 ms
        # print(f"Array sliced in {round((time.time() - startTime)*1000)}")

        '''
        isLow = False
        isHigh = False
        switchCount = 0

        for v in new_values:
            voltage = v[settings_switchChannel]
            if voltage < settings_switchLow:
                isLow = True
                if isHigh:
                    isHigh = False
                    switchCount += 1
            elif voltage > settings_switchHigh:
                isHigh = True
                if isLow:
                    isLow = False
                    switchCount += 1

        self.label1.setText(f"Counts: {switchCount}")

        print(f"Counted switches in {round((time.time() - startTime)*1000)}")
        '''

        channelName = ""
        if settings_switchChannel == 1:
            channelName = "ch1"
        elif settings_switchChannel == 2:
            channelName = "ch2"
        elif settings_switchChannel == 3:
            channelName = "ch3"
        elif settings_switchChannel == 4:
            channelName = "ch4"
        # Make sure input is a NumPy array
        v = new_values[channelName]
        n = len(v)

        # Step 1: classify samples into regions
        # -1 = below low, +1 = above high, 0 = middle
        state = np.zeros_like(v, dtype=np.int8)
        state[v < settings_switchLow] = -1
        state[v > settings_switchHigh] = 1

        # Step 2: forward-fill last nonzero state to preserve hysteresis
        # Create mask where state != 0
        nonzero_mask = state != 0

        # Indices of previous nonzero states
        idx = np.maximum.accumulate(nonzero_mask * np.arange(len(v)))
        # Replace zeros with the most recent nonzero state
        state = state[idx]

        # Step 3: count changes between -1 and +1
        diff = np.diff(state)
        switchCount = np.count_nonzero(np.abs(diff) == 2)

        self.label1.setText(f"Counts: {switchCount}, {round(1000000 / self.timeRange * switchCount)}/s")

        # 9 ms
        # print(f"Counted switches in {round((time.time() - startTime)*1000)}")

    def showMsg(self, switchCount): 
        msg = QMessageBox()
        msg.setIcon(QMessageBox.Information)  # Change icon here
        msg.setWindowTitle("Switches counted")
        msg.setText(f"There are {switchCount} switches in {self.maxH} seconds from {len(self.all_values)} samples exceeding {settings_switchHigh} V and deceeding {settings_switchLow} V on channel {settings_switchChannel}.")

        msg.setStandardButtons(QMessageBox.Ok) 
        msg.exec_()

'''def listen_for_keypress():
    global stop_requested
    try:
        tty.setcbreak(fd)
        while True:
            ch = sys.stdin.read(1)
            if ch == '\x1b' or ch == '\n' or ch == ' ':
                stop_requested = True
                break
    finally:
        termios.tcsetattr(fd, termios.TCSADRAIN, old_settings)'''

def hideLayout(layout):
    for i in range(layout.count()):
        widget = layout.itemAt(i).widget()
        if widget is not None:
            widget.hide()

def showLayout(layout):
    for i in range(layout.count()):
        widget = layout.itemAt(i).widget()
        if widget is not None:
            widget.show() 

def grayLayoutButtons(layout): 
    for i in range(layout.count()):
        widget = layout.itemAt(i).widget()
        if isinstance(widget, QPushButton):
            widget.setStyleSheet("font-size: 20px; font-weight: bold; background-color: gray")

def get_highest_number(directory):
    max_number = 0
    pattern = re.compile(r'^voltage4_log_(\d+)\.txt$')

    for filename in os.listdir(directory):
        match = pattern.match(filename)
        if match:
            number = int(match.group(1))
            if number > max_number:
                max_number = number

    return max_number

def get_highest_number2(directory):
    max_number = 0
    pattern = re.compile(r'^voltage4_export_(\d+)\.png$')

    for filename in os.listdir(directory):
        match = pattern.match(filename)
        if match:
            number = int(match.group(1))
            if number > max_number:
                max_number = number

    return max_number

def read_all_channels():
    proc.stdin.write(b"SLOW\n")
    proc.stdin.flush()    

    reader_frame(proc, q)
    buf = q.get()  # wait for next frame
    frame_bytes = buf[0:FRAME_SIZE + 2]
    readings = parse_frame(frame_bytes)                
    return readings

def reader_batch(proc, q): # works with batches that contain less than 8 frames in the end. (when triggering)
    counter = 0
    while True:               
        buf = bytearray(BATCH_SIZE)
        n = proc.stdout.readinto(buf)
        if n < BATCH_SIZE:
            if n == FRAME_SIZE: # remainer from slow mode
                print("Continuing")
                continue
            print(f"N {n}, end frame count {n/FRAME_SIZE_TS} after {counter} batches of {FRAMES_PER_BATCH}")
            if n == 1:
                q.put(None)
                break
            else:
                # both can happen        
                if n % FRAME_SIZE_TS == 1:
                    buf = buf[0:n - 1]
                elif n % FRAME_SIZE_TS == 0:
                    buf = buf[0:n]
                else:
                    print(f"Frame incomplete: {n % FRAME_SIZE_TS}") # = 16, remainder from slow mode at the start?
                    if n % FRAME_SIZE_TS == FRAME_SIZE:
                        buf = buf[16:n]
                        q.put(buf)
                        counter += 1
                        continue
                    else:
                        raise ValueError(f"Error in buffer length")
                q.put(buf)
                q.put(None)
                break
        else:      
            q.put(buf)
        counter += 1 

def reader_all(proc, q): # works with batches that contain less than 8 frames in the end. (when triggering) 
    size_bytes = proc.stdout.read(4)
    (size,) = struct.unpack("<I", size_bytes)

    buf = bytearray()  # dynamically grow if needed

    print(f"Reading {size} bytes")
    if size == 0:
        return bytes(buf)

    chunk = bytearray(65536)  # 64 KiB chunk size
    view = memoryview(chunk)

    while True:
        n = proc.stdout.readinto(view)
        if n is None or n == 0:
            break  # EOF reached
        buf.extend(view[:n])
        if len(buf) >= size:
            break

    return bytes(buf)

def reader_frame(proc, q):
    buf = bytearray(FRAME_SIZE + 2)
    n = proc.stdout.readinto(buf)
    if n < FRAME_SIZE:
        q.put(None)
        print(f"Short read/end of stream: {n}")
    q.put(buf)   # no parsing here

def reader_frame_ts(proc, q):
    while True:  
        buf = bytearray(FRAME_SIZE_TS)
        n = proc.stdout.readinto(buf)
        if n < FRAME_SIZE_TS:
            q.put(None)
            break;
        q.put(buf)   # no parsing here

def reader_logs(proc):
    for line in proc.stderr:
        print("C LOG:", line.decode().rstrip())

def parse_frame(buf): # 4 channels without timestamp
    global firstReading

    readings = []
    
    for i in range(0, 10, 2):
        (val,) = struct.unpack('>h', buf[i:i+2])  # '>h' = big-endian signed 16-bit
        voltage = val * 5 / 32768.0
        readings.append(voltage)

    '''
    0: groud normal
    1 - 4: subtract that channel
    5: measurement zero
    6 - 9: subtract from that channel
    '''
    newreadings = [0] * 4
    # option 2 is negative value in relation to another channel 
    if settings_ch1ground > 0 and settings_ch1ground < 5:
        newreadings[0] = round(readings[0] - readings[settings_ch1ground - 1], 4)        
    elif settings_ch1ground > 5 and settings_ch1ground < 10:
        newreadings[0] = round(readings[settings_ch1ground - 6] - readings[0], 4)        
    elif settings_ch1ground == 5:
        newreadings[0] = 0
    elif settings_ch1ground == 10:
        newreadings[0] = -round(readings[0], 4)
    else: 
        newreadings[0] = round(readings[0], 4)

    if settings_ch2ground > 0 and settings_ch2ground < 5:
        newreadings[1] = round(readings[1] - readings[settings_ch2ground - 1], 4)        
    elif settings_ch2ground > 5 and settings_ch2ground < 10:
        newreadings[1] = round(readings[settings_ch2ground - 6] - readings[1], 4)        
    elif settings_ch2ground == 5:
        newreadings[1] = 0
    elif settings_ch2ground == 10:
        newreadings[1] = -round(readings[1], 4)
    else: 
        newreadings[1] = round(readings[1], 4)

    if settings_ch3ground > 0 and settings_ch3ground < 5:
        newreadings[2] = round(readings[2] - readings[settings_ch3ground - 1], 4)
    elif settings_ch3ground > 5 and settings_ch3ground < 10:
        newreadings[2] = round(readings[settings_ch3ground - 6] - readings[2], 4)        
    elif settings_ch3ground == 5:
        newreadings[2] = 0
    elif settings_ch3ground == 10:
        newreadings[2] = -round(readings[2], 4)
    else: 
        newreadings[2] = round(readings[2], 4)

    if settings_ch4ground > 0 and settings_ch4ground < 5:
        newreadings[3] = round(readings[3] - readings[settings_ch4ground - 1], 4)        
    elif settings_ch4ground > 5 and settings_ch4ground < 10:
        newreadings[3] = round(readings[settings_ch4ground - 6] - readings[3], 4)        
    elif settings_ch4ground == 5:
        newreadings[3] = 0
    elif settings_ch4ground == 10:
        newreadings[3] = -round(readings[3], 4)
    else: 
        newreadings[3] = round(readings[3], 4)

    newreadings.append(round(readings[4], 4))
    
    return newreadings

def parse_frame2(buf): # 8 channels with timestamp
    # buf must be 16 bytes = 8 channels * 2 bytes
    readings = []
    for i in range(0, 16, 2):
        (val,) = struct.unpack('>h', buf[i:i+2])  # '>h' = big-endian signed 16-bit
        voltage = val * 5 / 32768.0
        readings.append(round(voltage, 4))

    # timestamp (little endian unsigned 32-bit)
    (ts,) = struct.unpack('<Q', buf[16:20])

    return readings, ts

def parse_frame3(buf): # 8 channels without timestamp
    readings = []
    for i in range(0, 16, 2):
        (val,) = struct.unpack('>h', buf[i:i+2])  # '>h' = big-endian signed 16-bit
        voltage = val * 5 / 32768.0
        readings.append(round(voltage, 4))

    return readings

def parse_frame4(buf): # 4 channels with timestamp in one array
    readings = []
    for i in range(0, 8, 2):
        (val,) = struct.unpack('>h', buf[i:i+2])  # '>h' = big-endian signed 16-bit
        voltage = val * 5 / 32768.0
        readings.append(round(voltage, 4))

    # timestamp (little endian unsigned 32-bit)
    (ts,) = struct.unpack('<Q', buf[16:20])
    readings.insert(0, ts / 1000000) # first value can be at 10 e-8

    return readings

def parse_frame_np(buf):
    if len(buf) % 12 != 0:
        raise ValueError(f"Buffer length must be multiple of 12, got {len(buf)}")
    frames = np.frombuffer(buf, dtype=frame_dt)
    volts = frames["adc"].astype(np.float32) * (5.0 / 32768.0)  # shape (nframes, 8)
    ts = frames["ts"]                                            # shape (nframes,)
    return volts, ts

 # returns one array. Channels 2 to 5 only.
def parse_batch(buf):
    nframes = len(buf) // FRAME_SIZE_TS
    if len(buf) % FRAME_SIZE_TS != 0:
        raise ValueError(f"Buffer length must be multiple of {FRAME_SIZE_TS}, got {len(buf)}")

    frames = np.frombuffer(buf, dtype=frame_dt, count=nframes)
    combined = np.empty(nframes, dtype=combined_dt)
    scaled = frames["adc"] * (5.0 / 32768.0)
    combined["ch1"] = scaled[:, 0]
    combined["ch2"] = scaled[:, 1]
    combined["ch3"] = scaled[:, 2]
    combined["ch4"] = scaled[:, 3]

    newcombined = np.empty(nframes, dtype=combined_dt)
    newcombined["timestamp_us"] = frames["ts"]
    if settings_ch1ground > 0 and settings_ch1ground < 5:
        newcombined["ch1"] = combined["ch1"] - combined["ch" + str(settings_ch1ground)]
    elif settings_ch1ground > 5 and settings_ch1ground < 10:
        newcombined["ch1"] = combined["ch" + str(settings_ch1ground - 5)] - combined["ch1"]
    elif settings_ch1ground == 5:
        newcombined["ch1"] = 0
    elif settings_ch1ground == 10:
        newcombined["ch1"] = -combined["ch1"]
    else:
        newcombined["ch1"] = combined["ch1"]
            
    if settings_ch2ground > 0 and settings_ch2ground < 5:
        newcombined["ch2"] = combined["ch2"] - combined["ch" + str(settings_ch2ground)]
    elif settings_ch2ground > 5 and settings_ch2ground < 10:
        newcombined["ch2"] = combined["ch" + str(settings_ch2ground - 5)] - combined["ch2"]
    elif settings_ch2ground == 5:
        newcombined["ch2"] = 0
    elif settings_ch2ground == 10:
        newcombined["ch2"] = -combined["ch2"]
    else:
        newcombined["ch2"] = combined["ch2"]

    if settings_ch3ground > 0 and settings_ch3ground < 5:
        newcombined["ch3"] = combined["ch3"] - combined["ch" + str(settings_ch3ground)]
    elif settings_ch3ground > 5 and settings_ch3ground < 10:
        newcombined["ch3"] = combined["ch" + str(settings_ch3ground - 5)] - combined["ch3"]
    elif settings_ch3ground == 5:
        newcombined["ch3"] = 0
    elif settings_ch3ground == 10:
        newcombined["ch3"] = -combined["ch3"]
    else:
        newcombined["ch3"] = combined["ch3"]

    if settings_ch4ground > 0 and settings_ch4ground < 5:
        newcombined["ch4"] = combined["ch4"] - combined["ch" + str(settings_ch4ground)]
    elif settings_ch4ground > 5 and settings_ch4ground < 10:
        newcombined["ch4"] = combined["ch" + str(settings_ch4ground - 5)] - combined["ch4"]
    elif settings_ch4ground == 5:
        newcombined["ch4"] = 0
    elif settings_ch4ground == 10:
        newcombined["ch4"] = -combined["ch4"]
    else:
        newcombined["ch4"] = combined["ch4"]

    return newcombined

# slower, 28 000 sps
def parse_batch2(buf):
    """Parse multiple frames (N × FRAME_SIZE_TS bytes)"""
    # if len(buf) % FRAME_SIZE_TS != 0:
    #    raise ValueError("Buffer length must be multiple of 20")

    nbytes = len(buf)
    nframes = nbytes // FRAME_SIZE_TS

    # Extract all ADC data
    adc_raw = np.frombuffer(buf, dtype='>i2').reshape(nframes, 12)[:, :4]  # 4 channels
    volts = adc_raw.astype(np.float32) * (5.0 / 32768.0)

    # Extract timestamps (last 8 bytes of each frame)
    # Step through the buffer at 20-byte stride
    # ts = np.frombuffer(buf, dtype='>u8', offset=16).reshape(nframes)
    # ts = np.frombuffer(buf, dtype='>u8', offset=16, count=n, like=np).reshape(n)

    ts = np.empty(nframes, dtype=np.uint64)
    for f in range(nframes):
        start = f * FRAME_SIZE_TS + FRAME_SIZE
        ts[f] = int.from_bytes(buf[start:start+4], byteorder="little", signed=False)

    return volts, ts

def read_signal():
    global signal

    proc.stdin.write(b"SIGN\n")
    proc.stdin.flush()

    line = proc.stderr.readline()

    # signal = 1
    # return

    try:
        signal = int(line.strip())            
    except ValueError:
        pass  # skip malformed lines

'''def read_all_channels():
    # Start conversion
    GPIO.output(PIN_CONVST, GPIO.HIGH)
    GPIO.output(PIN_CONVST, GPIO.LOW)

    while GPIO.input(PIN_BUSY):
        pass  # Wait until BUSY goes LOW

    raw = spi.readbytes(16)  # 2 bytes per channel × 8 channels
    readings = []

    for i in range(0, 8, 2):
        word = raw[i] << 8 | raw[i+1]
        # Convert to signed 16-bit
        if word & 0x8000:
            word -= 0x10000
        # Convert to voltage
        voltage = (word / 32768.0) * 5
        readings.append(round(voltage, 4))

    return readings
'''

analyze = False
fastMode = True
useThread = False

try:
    gc.disable()

    # stop_requested = False
    
    # fd = sys.stdin.fileno()
    # old_settings = termios.tcgetattr(fd)

    # Start keypress listener in a separate thread
    # keypress_thread = threading.Thread(target=listen_for_keypress)
    # keypress_thread.daemon = True
    # keypress_thread.start()    
    FRAME_SIZE = 8
    FRAME_SIZE_TS = 12
    # FRAMES_PER_BATCH = 1000 # at 10000, I frequently get errors
    # BATCH_SIZE = FRAMES_PER_BATCH * FRAME_SIZE_TS
    QUEUE_MAX = 2 

    proc = subprocess.Popen(
            ["/home/fodorbalint/Documents/ad/ad7606_reader"],
            stdin=subprocess.PIPE,
            stdout=subprocess.PIPE,
            stderr=subprocess.PIPE,  # logs
            bufsize=0  # disable buffering on Python side too
        )  

    q = queue.Queue(maxsize=QUEUE_MAX) 
       
    print(f"Program starts")

    if analyze:
        if fastMode: 
            if useThread:
                t = threading.Thread(target=reader_batch, args=(proc, q), daemon=True)
                t.start()

                counter = 0
                while True:                    
                    buf = q.get()
                    if buf is None:   # sentinel
                        break                    
                    
                    '''for f in range(FRAMES_PER_BATCH):
                        frame_bytes = buf[f*FRAME_SIZE_TS:(f+1)*FRAME_SIZE_TS]
                        readings, ts = parse_frame(frame_bytes)
                        if counter < 3:
                            print(counter, ts, readings)'''
                   
                    volts_batch, ts_batch = parse_frame_np(buf)                             
                    if counter < 3:
                        print(counter, volts_batch[0], ts_batch[0])

                    counter += 1                    
                
                print(f"Got {counter*FRAMES_PER_BATCH} readings.")
            else:
                readSettings()

                duration_us = int(settings_timeLimit * 1000000)
                threshold = int(settings_triggerThreshold * 1000)

                cmd = f"FAST{duration_us:08d}{settings_triggerChannel}"
                if settings_risingEdge:
                    cmd += "1"
                else:
                    cmd += "0"
                cmd += f"{threshold:04d}{settings_offsetPercent:02d}\n"

                print(cmd)
                
                proc.stdin.write(cmd.encode())
                proc.stdin.flush()

                # print(proc.stderr.readline().decode().strip())
                # print(proc.stderr.readline().decode().strip())

                threading.Thread(target=reader_frame_ts, args=(proc, q), daemon=True).start()
                # threading.Thread(target=reader_logs, args=(proc,), daemon=True).start()

                # reader_logs(proc)
                # reader_frame_ts(proc, q)

                print(f"Start reading.")

                counter = 0
                while True:
                    buf = q.get()  # wait for next batch
                    if buf is None:   # sentinel
                        break

                    if counter % 1000 == 0:
                        print (f"{counter}")

                    # frame_bytes = buf[0:FRAME_SIZE_TS]
                    readings, ts = parse_frame(buf)
                    if counter < 3:
                        print(counter, ts, readings)
                # Parse in bulk with NumPy
                # samples = np.frombuffer(buf, dtype="<i2").reshape(-1, 8)
                # Now samples.shape == (frames_per_batch, 8)
                # Do something with samples...
                # print(samples[0])  # print first frame as test
                    counter += 1

                print(counter, ts, readings)
                print(f"Got {counter} readings.")                                

                '''
                reader_batch(proc, q)

                counter = 0
                while True:
                    buf = q.get()  # wait for next batch
                    if buf is None:   # sentinel
                        break

                    if counter % 1000 == 0:
                        print (f"{counter}")

                    for f in range(FRAMES_PER_BATCH):
                        frame_bytes = buf[f*FRAME_SIZE_TS:(f+1)*FRAME_SIZE_TS]
                        readings, ts = parse_frame(frame_bytes)
                        if counter < 3:
                            print(counter, ts, readings)
                    # Parse in bulk with NumPy
                    # samples = np.frombuffer(buf, dtype="<i2").reshape(-1, 8)
                    # Now samples.shape == (frames_per_batch, 8)
                    # Do something with samples...
                    # print(samples[0])  # print first frame as test
                    counter += 1

                print(f"Got {counter*FRAMES_PER_BATCH} readings.")
                '''

        else:            
            proc.stdin.write(b"SLOW\n")
            proc.stdin.flush()

            QUEUE_MAX = 10000
            q = queue.Queue(maxsize=QUEUE_MAX)

            reader_frame(proc, q)
            buf = q.get()  # wait for next frame

            frame_bytes = buf[0:FRAME_SIZE + 2]
            readings = parse_frame2(frame_bytes)                   
            print(readings)

            print ("Read ends")
        
    else:
        '''
        # --- Pin setup ---
        PIN_RESET = 17
        PIN_CONVST = 18
        PIN_BUSY = 25

        # --- GPIO setup ---
        GPIO.setmode(GPIO.BCM)
        GPIO.setup(PIN_RESET, GPIO.OUT)
        GPIO.setup(PIN_CONVST, GPIO.OUT)
        GPIO.setup(PIN_BUSY, GPIO.IN)

        # --- SPI setup ---
        spi = spidev.SpiDev()
        spi.open(0, 0)  # SPI0, CE0
        spi.max_speed_hz = 1000000  # 1 MHz is safe to start with
        spi.mode = 2  # CPOL=1, CPHA=0 (SPI Mode 2 is required by AD7606)

        # --- Reset AD7606 ---
        GPIO.output(PIN_RESET, GPIO.HIGH)
        time.sleep(0.01)
        GPIO.output(PIN_RESET, GPIO.LOW)
        time.sleep(0.01)
        '''

        GPIO.setmode(GPIO.BCM)   
        MIN_TIME_RANGE = 10000 # 1 / 55000 * 800 = 0.0145
        # 1 / 42700 * 800 = 0.0187
        MIN_V_RANGE = 0.05 # 5 / 32768 * 408 = 0.0623
        readSettings()
        values = read_all_channels()
        print(f"Start values {values[0]} {values[1]} {values[2]} {values[3]}, battery voltage {values[4]}")

        # Run the application
        
        app = QApplication([])  
        app.setStyle("Fusion")      
        settingsWindow = SettingsWindow()
        graphWindow = GraphWidget()
        editorWindow = EditorWindow()
        settingsWindow.show()
        graphWindow.show()        
        app.exec_()

    '''buf = bytearray(BATCH_SIZE)

    counter = 0
    while True:
        n = proc.stdout.readinto(buf)
        if n < BATCH_SIZE:
            print(f"Short read / end of stream: {n}")
            break
        
        for f in range(FRAMES_PER_BATCH):
            frame_bytes = buf[f*FRAME_SIZE:(f+1)*FRAME_SIZE]
            # readings, ts = parse_frame(frame_bytes)
            # readings = [struct.unpack('>h', frame_bytes[i:i+2])[0] for i in range(0, FRAME_SIZE, 2)]
            if counter < 10:            
                readings, ts = parse_frame(frame_bytes)
                print(ts, readings)
        
        counter += 1
    '''

    # print(f"Got {counter*FRAMES_PER_BATCH} readings.")
    

    '''
    while not stop_requested and chan2Value < chan2Threshold:
        values = read_all_channels()
        chan2Value = values[1]

    if not stop_requested:
        print(f"Measurement starts")

        all_values = []
        start = time.time()
        new_time = 0

        while not stop_requested and new_time < timeLimit:
            values = read_all_channels()
            new_time = time.time()-start
            values.insert(0, round(new_time,4))
            all_values.append(values)

            # print(["{:.3f} V".format(v) for v in voltages])
            # print(values)  # Raw ADC values (-32768 to 32767)
            # time.sleep(0.0001)  # 10000 samples/sec

        print(f"{len(all_values)} data points collected. Writing to file...")
        content = ""
        for v in all_values:
            for v2 in v:
                content += str(v2) + " "
            content = content[0:len(content)-1] + "\n"
        with open(f"voltage4_log_{highest_number}.txt", "w") as file:
            file.write(content)
        print(f"Finished")  
    '''  
finally:
    termios.tcsetattr(fd, termios.TCSADRAIN, old_settings)
    GPIO.cleanup()
    '''if not analyze:
        spi.close()'''