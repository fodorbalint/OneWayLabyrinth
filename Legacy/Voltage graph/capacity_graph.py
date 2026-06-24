import spidev
import RPi.GPIO as GPIO
import time
import math
import numpy as np
from PyQt5.QtWidgets import QApplication, QWidget, QVBoxLayout, QHBoxLayout, QLabel, QPushButton, QLineEdit, QListWidget, QAbstractItemView, QTextEdit
from PyQt5.QtCore import QTimer, Qt, QPointF, QRect, QSize
from PyQt5.QtGui import QPixmap, QPainter, QColor, QPolygonF, QFont, QFontMetrics
import re
import os
import board
import busio
import adafruit_ads1x15.ads1115 as ADS
from adafruit_ads1x15.analog_in import AnalogIn

os.environ["QT_QPA_PLATFORM"] = "xcb"
combined_dt = np.dtype([
    ("timestamp_s", "<u4"),
    ("ch1", np.float32),
    ("curr", np.float32),
    ("wh", np.float32)
])

# --- Pin setup ---
PIN_RESET = 17
PIN_CONVST = 18
PIN_BUSY = 25

def float_to_str(f):
    if f.is_integer():
        return str(int(f))
    else:
        return str(f)

def setup_ad():
    global spi

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

def setup_ads():
    global chan1, chan2, chan3, chan4

    # I2C setup
    i2c = busio.I2C(board.SCL, board.SDA)
    ads = ADS.ADS1115(i2c)

    # Set gain (±6.144V allows full-scale input)
    ads.data_rate = 860
    ads.gain = 1

    # Read from channel A0-A3
    chan1 = AnalogIn(ads, 0)
    chan2 = AnalogIn(ads, 1)
    chan3 = AnalogIn(ads, 2)
    chan4 = AnalogIn(ads, 3)

def read_channel(): # 1 channel
    if useAd:
        # Start conversion
        GPIO.output(PIN_CONVST, GPIO.HIGH)
        GPIO.output(PIN_CONVST, GPIO.LOW)

        while GPIO.input(PIN_BUSY):
            pass  # Wait until BUSY goes LOW

        raw = spi.readbytes(16)  # 2 bytes per channel × 8 channels
        readings = []

        i = 0
        word = raw[i] << 8 | raw[i+1]
        # Convert to signed 16-bit
        if word & 0x8000:
            word -= 0x10000
        # Convert to voltage
        voltage = (word / 32768.0) * 5
        return round(voltage, 4)
    else:
        return chan1.voltage * v1multiplier

def read_channel2(): # 2 channels
    readings = []
    if useAd:
        # Start conversion
        GPIO.output(PIN_CONVST, GPIO.HIGH)
        GPIO.output(PIN_CONVST, GPIO.LOW)

        while GPIO.input(PIN_BUSY):
            pass  # Wait until BUSY goes LOW

        raw = spi.readbytes(16)  # 2 bytes per channel × 8 channels

        for i in range(0, 4, 2):
            word = raw[i] << 8 | raw[i+1]
            # Convert to signed 16-bit
            if word & 0x8000:
                word -= 0x10000
            # Convert to voltage
            voltage = (word / 32768.0) * 5
            readings.append(voltage)

        word = raw[8] << 8 | raw[9]
        if word & 0x8000:
            word -= 0x10000
        voltage = (word / 32768.0) * 5
        readings.append(voltage)
        
        return readings
    else:
        readings = [chan1.voltage * v1multiplier, chan2.voltage, 4.2]
        return readings

def get_highest_number(directory):
    max_number = 0
    pattern = re.compile(r'^capacity_log_(\d+)\.txt$')

    for filename in os.listdir(directory):
        match = pattern.match(filename)
        if match:
            number = int(match.group(1))
            if number > max_number:
                max_number = number

    return max_number

def readSettings():
    global settingsArr, settings_trigger, settings_triggerCurrentStart, settings_triggerCurrentEnd, settings_voltageDirection, settings_triggerVoltageStart, settings_triggerVoltageEnd, settings_voltageMultiplier, settings_idleCurrent, settings_multiSelection

    settings = ""
    newSettings = ""

    with open(f"/home/fodorbalint/Documents/capacity settings.txt", "r") as file:
        settings = file.read()
        settingsArr = settings.split("\n")
        for line in settingsArr: 
            pairs = line.strip().split(" = ")

            if pairs[0] == "trigger":
                if pairs[1] == "False" or pairs[1] == "false":
                    settings_trigger = False
                else:
                    settings_trigger = True
                newSettings += line + "\n"
            elif pairs[0] == "triggerCurrentStart":
                settings_triggerCurrentStart = float(pairs[1])
                newSettings += line + "\n"
            elif pairs[0] == "triggerCurrentEnd":
                settings_triggerCurrentEnd = float(pairs[1])
                newSettings += line + "\n"
            elif pairs[0] == "voltageDirection":
                if pairs[1] == "False" or pairs[1] == "false":
                    settings_voltageDirection = False
                else:
                    settings_voltageDirection = True
                newSettings += line + "\n"
            elif pairs[0] == "triggerVoltageStart":
                settings_triggerVoltageStart = float(pairs[1])
                newSettings += line + "\n"
            elif pairs[0] == "triggerVoltageEnd":
                settings_triggerVoltageEnd = float(pairs[1])
                newSettings += line + "\n"
            elif pairs[0] == "voltageMultiplier":
                if pairs[1] == "13":
                    settings_voltageMultiplier = 13
                elif pairs[1] == "7":
                    settings_voltageMultiplier = 7
                else:
                    settings_voltageMultiplier = 1
                newSettings += line + "\n"
            elif pairs[0] == "idleCurrent":
                settings_idleCurrent = float(pairs[1])
                newSettings += line + "\n"
            elif pairs[0] == "multiSelection":
                if pairs[1] == "False" or pairs[1] == "false":
                    settings_multiSelection = False
                else:
                    settings_multiSelection = True
                newSettings += line + "\n"

    newSettings = newSettings[:-1]
    if newSettings != settings:
        print (f"Settings corrected")
        with open(f"/home/fodorbalint/Documents/capacity settings.txt", "w") as file:
            file.write(newSettings)

def saveSettings(name):
    for i in range (0, len(settingsArr)):
        line = settingsArr[i]
        pairs = line.split(" = ")
        if pairs[0] == name:
            if name == "trigger":
                settingsArr[i] = pairs[0] + " = " + str(settings_trigger)
            elif name == "triggerCurrentStart":
                settingsArr[i] = pairs[0] + " = " + float_to_str(settings_triggerCurrentStart)
            elif name == "triggerCurrentEnd":
                settingsArr[i] = pairs[0] + " = " + float_to_str(settings_triggerCurrentEnd)
            elif name == "voltageDirection":
                settingsArr[i] = pairs[0] + " = " + str(settings_voltageDirection)
            elif name == "triggerVoltageStart":
                settingsArr[i] = pairs[0] + " = " + float_to_str(settings_triggerVoltageStart)
            elif name == "triggerVoltageEnd":
                settingsArr[i] = pairs[0] + " = " + float_to_str(settings_triggerVoltageEnd)
            elif name == "voltageMultiplier":
                settingsArr[i] = pairs[0] + " = " + str(settings_voltageMultiplier)
            elif name == "idleCurrent":
                settingsArr[i] = pairs[0] + " = " + float_to_str(settings_idleCurrent)
            elif name == "multiSelection":
                settingsArr[i] = pairs[0] + " = " + str(settings_multiSelection)

    fileText = "\n".join(settingsArr)

    with open(f"/home/fodorbalint/Documents/capacity settings.txt", "w") as file:
        file.write(fileText)

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

        self.label1 = QLabel("Trigger:")
        self.label1.setStyleSheet("QLabel { color: white; }")
        self.label1.setMaximumWidth(160)
        self.label1.setMinimumWidth(160)
        self.button1 = QPushButton()
        if settings_trigger:
            self.button1.setText("On")
            self.button1.setStyleSheet("font-weight: bold; background-color: lightgreen")
        else:
            self.button1.setText("Off")
            self.button1.setStyleSheet("font-weight: bold; background-color: gray")
        self.button1.setMaximumWidth(50)

        layout1.addWidget(self.label1)
        layout1.addWidget(self.button1)

        layout2 = QHBoxLayout()
        layout2.setContentsMargins(0, 0, 0, 0); 
        layout2.setSpacing(5) 
        layout2.setAlignment(Qt.AlignLeft)

        self.label2 = QLabel("Trigger current start:")
        self.label2.setStyleSheet("color: white")
        self.label2.setMinimumWidth(160)
        self.label2.setMaximumWidth(160)
        self.textbox2 = QLineEdit()
        self.textbox2.setStyleSheet("font-weight: bold")
        self.textbox2.setText(float_to_str(settings_triggerCurrentStart))
        self.textbox2.setMaximumWidth(50)
        self.button21 = QPushButton("--")
        self.button21.setStyleSheet("font-size: 20px; font-weight: bold")
        self.button21.setMaximumSize(QSize(30, 30))
        self.button22 = QPushButton("-")
        self.button22.setStyleSheet("font-size: 20px; font-weight: bold")
        self.button22.setMaximumSize(QSize(30, 30))
        self.button23 = QPushButton("+")
        self.button23.setStyleSheet("font-size: 20px; font-weight: bold")
        self.button23.setMaximumSize(QSize(30, 30))
        self.button24 = QPushButton("++")
        self.button24.setStyleSheet("font-size: 20px; font-weight: bold")
        self.button24.setMaximumSize(QSize(30, 30))

        layout2.addWidget(self.label2)
        layout2.addWidget(self.textbox2)
        layout2.addWidget(self.button21)
        layout2.addWidget(self.button22)
        layout2.addWidget(self.button23)
        layout2.addWidget(self.button24) 

        layout3 = QHBoxLayout()
        layout3.setContentsMargins(0, 0, 0, 0); 
        layout3.setSpacing(5) 
        layout3.setAlignment(Qt.AlignLeft)

        self.label3 = QLabel("Trigger current end:")
        self.label3.setStyleSheet("color: white")
        self.label3.setMinimumWidth(160)
        self.label3.setMaximumWidth(160)
        self.textbox3 = QLineEdit()
        self.textbox3.setStyleSheet("font-weight: bold")
        self.textbox3.setText(float_to_str(settings_triggerCurrentEnd))
        self.textbox3.setMaximumWidth(50)
        self.button31 = QPushButton("--")
        self.button31.setStyleSheet("font-size: 20px; font-weight: bold")
        self.button31.setMaximumSize(QSize(30, 30))
        self.button32 = QPushButton("-")
        self.button32.setStyleSheet("font-size: 20px; font-weight: bold")
        self.button32.setMaximumSize(QSize(30, 30))
        self.button33 = QPushButton("+")
        self.button33.setStyleSheet("font-size: 20px; font-weight: bold")
        self.button33.setMaximumSize(QSize(30, 30))
        self.button34 = QPushButton("++")
        self.button34.setStyleSheet("font-size: 20px; font-weight: bold")
        self.button34.setMaximumSize(QSize(30, 30))

        layout3.addWidget(self.label3)
        layout3.addWidget(self.textbox3)
        layout3.addWidget(self.button31)
        layout3.addWidget(self.button32)
        layout3.addWidget(self.button33)
        layout3.addWidget(self.button34)  

        layout4 = QHBoxLayout()
        layout4.setContentsMargins(0, 0, 0, 0); 
        layout4.setSpacing(5) 
        layout4.setAlignment(Qt.AlignLeft)

        self.label4 = QLabel("Voltage direction:")
        self.label4.setStyleSheet("QLabel { color: white; }")
        self.label4.setMaximumWidth(160)
        self.label4.setMinimumWidth(160)

        self.button41 = QPushButton("Rising")
        self.button42 = QPushButton("Falling")
        if settings_voltageDirection:
            self.button41.setStyleSheet("font-weight: bold; background-color: lightgreen")
            self.button42.setStyleSheet("font-weight: bold; background-color: gray")
        else:
            self.button41.setStyleSheet("font-weight: bold; background-color: gray")
            self.button42.setStyleSheet("font-weight: bold; background-color: lightgreen")
        self.button41.setMaximumWidth(70)
        self.button42.setMaximumWidth(70)  

        layout4.addWidget(self.label4)
        layout4.addWidget(self.button41)
        layout4.addWidget(self.button42)

        layout5 = QHBoxLayout()
        layout5.setContentsMargins(0, 0, 0, 0); 
        layout5.setSpacing(5) 
        layout5.setAlignment(Qt.AlignLeft)

        self.label5 = QLabel("Trigger voltage start:")
        self.label5.setStyleSheet("color: white")
        self.label5.setMinimumWidth(160)
        self.label5.setMaximumWidth(160)
        self.textbox5 = QLineEdit()
        self.textbox5.setStyleSheet("font-weight: bold")
        self.textbox5.setText(float_to_str(settings_triggerVoltageStart))
        self.textbox5.setMaximumWidth(50)
        self.button51 = QPushButton("--")
        self.button51.setStyleSheet("font-size: 20px; font-weight: bold")
        self.button51.setMaximumSize(QSize(30, 30))
        self.button52 = QPushButton("-")
        self.button52.setStyleSheet("font-size: 20px; font-weight: bold")
        self.button52.setMaximumSize(QSize(30, 30))
        self.button53 = QPushButton("+")
        self.button53.setStyleSheet("font-size: 20px; font-weight: bold")
        self.button53.setMaximumSize(QSize(30, 30))
        self.button54 = QPushButton("++")
        self.button54.setStyleSheet("font-size: 20px; font-weight: bold")
        self.button54.setMaximumSize(QSize(30, 30))

        layout5.addWidget(self.label5)
        layout5.addWidget(self.textbox5)
        layout5.addWidget(self.button51)
        layout5.addWidget(self.button52)
        layout5.addWidget(self.button53)
        layout5.addWidget(self.button54) 

        layout6 = QHBoxLayout()
        layout6.setContentsMargins(0, 0, 0, 0); 
        layout6.setSpacing(5) 
        layout6.setAlignment(Qt.AlignLeft)

        self.label6 = QLabel("Trigger voltage end:")
        self.label6.setStyleSheet("color: white")
        self.label6.setMinimumWidth(160)
        self.label6.setMaximumWidth(160)
        self.textbox6 = QLineEdit()
        self.textbox6.setStyleSheet("font-weight: bold")
        self.textbox6.setText(float_to_str(settings_triggerVoltageEnd))
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

        self.label7 = QLabel("Voltage multiplier:")
        self.label7.setStyleSheet("QLabel { color: white; }")
        self.label7.setMaximumWidth(160)
        self.label7.setMinimumWidth(160)

        self.button71 = QPushButton("1 - 3.7 V")
        self.button72 = QPushButton("7 - 24 V")
        self.button73 = QPushButton("13 - 48 V")
        if settings_voltageMultiplier == 1:
            self.button71.setStyleSheet("font-weight: bold; background-color: lightgreen")
            self.button72.setStyleSheet("font-weight: bold; background-color: gray")
            self.button73.setStyleSheet("font-weight: bold; background-color: gray")
        elif settings_voltageMultiplier == 7:
            self.button71.setStyleSheet("font-weight: bold; background-color: gray")
            self.button72.setStyleSheet("font-weight: bold; background-color: lightgreen")
            self.button73.setStyleSheet("font-weight: bold; background-color: gray")
        elif settings_voltageMultiplier == 13:
            self.button71.setStyleSheet("font-weight: bold; background-color: gray")
            self.button72.setStyleSheet("font-weight: bold; background-color: gray")
            self.button73.setStyleSheet("font-weight: bold; background-color: lightgreen")
        self.button71.setMaximumWidth(85)
        self.button72.setMaximumWidth(85)
        self.button73.setMaximumWidth(85)  

        layout7.addWidget(self.label7)
        layout7.addWidget(self.button71)
        layout7.addWidget(self.button72)
        layout7.addWidget(self.button73)

        layout8 = QHBoxLayout()
        layout8.setContentsMargins(0, 0, 0, 0); 
        layout8.setSpacing(5) 
        layout8.setAlignment(Qt.AlignLeft)

        self.label8 = QLabel("Idle current (mA):")
        self.label8.setStyleSheet("color: white")
        self.label8.setMinimumWidth(160)
        self.label8.setMaximumWidth(160)
        self.textbox8 = QLineEdit()
        self.textbox8.setStyleSheet("font-weight: bold")
        self.textbox8.setText(float_to_str(settings_idleCurrent))
        self.textbox8.setMaximumWidth(50)
        self.button81 = QPushButton("--")
        self.button81.setStyleSheet("font-size: 20px; font-weight: bold")
        self.button81.setMaximumSize(QSize(30, 30))
        self.button82 = QPushButton("-")
        self.button82.setStyleSheet("font-size: 20px; font-weight: bold")
        self.button82.setMaximumSize(QSize(30, 30))
        self.button83 = QPushButton("+")
        self.button83.setStyleSheet("font-size: 20px; font-weight: bold")
        self.button83.setMaximumSize(QSize(30, 30))
        self.button84 = QPushButton("++")
        self.button84.setStyleSheet("font-size: 20px; font-weight: bold")
        self.button84.setMaximumSize(QSize(30, 30))

        layout8.addWidget(self.label8)
        layout8.addWidget(self.textbox8)
        layout8.addWidget(self.button81)
        layout8.addWidget(self.button82)
        layout8.addWidget(self.button83)
        layout8.addWidget(self.button84)

        layout9 = QHBoxLayout()
        layout9.setContentsMargins(0, 0, 0, 0); 
        layout9.setSpacing(5) 
        layout9.setAlignment(Qt.AlignLeft)

        self.label9 = QLabel("Selection mode:")
        self.label9.setStyleSheet("QLabel { color: white; }")
        self.label9.setMaximumWidth(160)
        self.label9.setMinimumWidth(160)

        self.button91 = QPushButton("Single")
        self.button92 = QPushButton("Multi")
        if not settings_multiSelection:
            self.button91.setStyleSheet("font-weight: bold; background-color: lightgreen")
            self.button92.setStyleSheet("font-weight: bold; background-color: gray")
        else:
            self.button91.setStyleSheet("font-weight: bold; background-color: gray")
            self.button92.setStyleSheet("font-weight: bold; background-color: lightgreen")
        self.button91.setMaximumWidth(70)
        self.button92.setMaximumWidth(70)  

        layout9.addWidget(self.label9)
        layout9.addWidget(self.button91)
        layout9.addWidget(self.button92)

        layoutA = QHBoxLayout()
        layoutA.setContentsMargins(0, 0, 0, 0); 
        layoutA.setSpacing(5) 
        layoutA.setAlignment(Qt.AlignLeft)  

        self.labelA = QLabel("Select file:")
        self.labelA.setStyleSheet("color: white")
        self.labelA.setMaximumWidth(100)
        self.labelA.setMinimumWidth(100)
        self.listA = QListWidget()
        self.listA.setStyleSheet("color: white; background-color: black")
        self.refreshList()
        self.listA.setMinimumWidth(170)
        self.listA.setMaximumWidth(170)
        self.listA.setMinimumHeight(150)
        self.listA.setMaximumHeight(150)
        if settings_multiSelection:
            self.listA.setSelectionMode(QAbstractItemView.MultiSelection)
        else:
            self.listA.setSelectionMode(QAbstractItemView.SingleSelection)

        layoutA1 = QVBoxLayout()
        layoutA1.setContentsMargins(0, 0, 0, 0); 
        layoutA1.setSpacing(15) 
        layoutA1.setAlignment(Qt.AlignBottom)        

        self.buttonA1 = QPushButton("Open")
        self.buttonA1.setStyleSheet("font-weight: bold; background-color: lightgreen")
        self.buttonA1.setMaximumWidth(70)
        self.buttonA2 = QPushButton("Delete")
        self.buttonA2.setStyleSheet("font-weight: bold; background-color: pink")
        self.buttonA2.setMaximumWidth(70)

        layoutA1.addWidget(self.buttonA1)  
        layoutA1.addWidget(self.buttonA2) 

        layoutA.addWidget(self.labelA)
        layoutA.addWidget(self.listA) 
        layoutA.addLayout(layoutA1)  

        self.textB = QTextEdit()
        self.textB.setStyleSheet("font-size: 14px; color:white; background-color: black")
        self.textB.setMinimumHeight(65)
        self.textB.setMaximumHeight(65)      

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

        layout01 = QVBoxLayout()
        layout01.setAlignment(Qt.AlignTop)
        layout01.addLayout(layout9)
        layout01.addLayout(layoutA)
        layout01.addWidget(self.textB)

        layout0 = QHBoxLayout()
        layout0.setAlignment(Qt.AlignLeft)
        layout0.addLayout(layout00)
        layout0.addLayout(layout01) 

        self.setLayout(layout0)

        self.button1.clicked.connect(self.setTrigger)
        self.button21.clicked.connect(lambda: self.setTriggerCurrentStart(0,0.1))
        self.button22.clicked.connect(lambda: self.setTriggerCurrentStart(0,0.01))
        self.button23.clicked.connect(lambda: self.setTriggerCurrentStart(1,0.01))
        self.button24.clicked.connect(lambda: self.setTriggerCurrentStart(1,0.1))
        self.button31.clicked.connect(lambda: self.setTriggerCurrentEnd(0,0.1))
        self.button32.clicked.connect(lambda: self.setTriggerCurrentEnd(0,0.01))
        self.button33.clicked.connect(lambda: self.setTriggerCurrentEnd(1,0.01))
        self.button34.clicked.connect(lambda: self.setTriggerCurrentEnd(1,0.1))
        self.button41.clicked.connect(lambda: self.setVoltageDirection(True))
        self.button42.clicked.connect(lambda: self.setVoltageDirection(False))
        self.button51.clicked.connect(lambda: self.setTriggerVoltageStart(0,0.1))
        self.button52.clicked.connect(lambda: self.setTriggerVoltageStart(0,0.01))
        self.button53.clicked.connect(lambda: self.setTriggerVoltageStart(1,0.01))
        self.button54.clicked.connect(lambda: self.setTriggerVoltageStart(1,0.1))
        self.button61.clicked.connect(lambda: self.setTriggerVoltageEnd(0,0.1))
        self.button62.clicked.connect(lambda: self.setTriggerVoltageEnd(0,0.01))
        self.button63.clicked.connect(lambda: self.setTriggerVoltageEnd(1,0.01))
        self.button64.clicked.connect(lambda: self.setTriggerVoltageEnd(1,0.1))
        self.button71.clicked.connect(lambda: self.setVoltageMultiplier(1))
        self.button72.clicked.connect(lambda: self.setVoltageMultiplier(7))
        self.button73.clicked.connect(lambda: self.setVoltageMultiplier(13))
        self.button81.clicked.connect(lambda: self.setIdleCurrent(0,1))
        self.button82.clicked.connect(lambda: self.setIdleCurrent(0,0.1))
        self.button83.clicked.connect(lambda: self.setIdleCurrent(1,0.1))
        self.button84.clicked.connect(lambda: self.setIdleCurrent(1,1))
        self.button91.clicked.connect(lambda: self.setMultiSelection(False))
        self.button92.clicked.connect(lambda: self.setMultiSelection(True))
        # self.listA.clicked.connect(self.selectedFile)
        self.listA.selectionModel().selectionChanged.connect(self.on_selection_changed)
        self.buttonA1.clicked.connect(self.openFromFile)
        self.buttonA2.clicked.connect(self.deleteFile)

    def force_fullscreen_fix(self):
        rect = QApplication.primaryScreen().availableGeometry()
        self.setGeometry(rect)
        self.setFixedSize(self.width(), self.height())

    def setTrigger(self):
        global settings_trigger

        if settings_trigger: settings_trigger = False
        else: settings_trigger = True
        if settings_trigger:
            self.button1.setText("On")
            self.button1.setStyleSheet("font-weight: bold; background-color: lightgreen")
        else:
            self.button1.setText("Off")
            self.button1.setStyleSheet("font-weight: bold; background-color: gray")
            graphWindow.restartGraph()
        saveSettings("trigger")             

    def setVoltageDirection(self, rising):
        global settings_voltageDirection

        if rising:
            settings_voltageDirection = True
            self.button41.setStyleSheet("font-weight: bold; background-color: lightgreen")
            self.button42.setStyleSheet("font-weight: bold; background-color: gray")
        else:
            settings_voltageDirection = False
            self.button41.setStyleSheet("font-weight: bold; background-color: gray")
            self.button42.setStyleSheet("font-weight: bold; background-color: lightgreen")
        saveSettings("voltageDirection") 

    def setTriggerCurrentStart(self, direction, increment):
        global settings_triggerCurrentStart

        if direction == 0:
            settings_triggerCurrentStart = round(settings_triggerCurrentStart - increment, 2)
        else:
            settings_triggerCurrentStart = round(settings_triggerCurrentStart + increment, 2)
        if settings_triggerCurrentStart < 0: settings_triggerCurrentStart = float(0)
        if settings_triggerCurrentStart > 1: settings_triggerCurrentStart = float(1)

        self.textbox2.setText(float_to_str(settings_triggerCurrentStart))
        saveSettings("triggerCurrentStart")

    def setTriggerCurrentEnd(self, direction, increment):
        global settings_triggerCurrentEnd

        if direction == 0:
            settings_triggerCurrentEnd = round(settings_triggerCurrentEnd - increment, 2)
        else:
            settings_triggerCurrentEnd = round(settings_triggerCurrentEnd + increment, 2)
        if settings_triggerCurrentEnd < 0: settings_triggerCurrentEnd = float(0)
        if settings_triggerCurrentEnd > 1: settings_triggerCurrentEnd = float(1)
        self.textbox3.setText(float_to_str(settings_triggerCurrentEnd))
        saveSettings("triggerCurrentEnd")

    def setTriggerVoltageStart(self, direction, increment):
        global settings_triggerVoltageStart

        if direction == 0:
            settings_triggerVoltageStart = round(settings_triggerVoltageStart - increment, 2)
        else:
            settings_triggerVoltageStart = round(settings_triggerVoltageStart + increment, 2)
        if settings_triggerVoltageStart < 0: settings_triggerVoltageStart = float(0)
        if settings_triggerVoltageStart > 5: settings_triggerVoltageStart = float(5)

        self.textbox5.setText(float_to_str(settings_triggerVoltageStart))
        saveSettings("triggerVoltageStart")

    def setTriggerVoltageEnd(self, direction, increment):
        global settings_triggerVoltageEnd

        if direction == 0:
            settings_triggerVoltageEnd = round(settings_triggerVoltageEnd - increment, 2)
        else:
            settings_triggerVoltageEnd = round(settings_triggerVoltageEnd + increment, 2)
        if settings_triggerVoltageEnd < 0: settings_triggerVoltageEnd = float(0)
        if settings_triggerVoltageEnd > 5: settings_triggerVoltageEnd = float(5)
        self.textbox6.setText(float_to_str(settings_triggerVoltageEnd))
        saveSettings("triggerVoltageEnd")

    def setVoltageMultiplier(self, value):
        global settings_voltageMultiplier

        if value == 13:
            settings_voltageMultiplier = 13
            self.button71.setStyleSheet("font-weight: bold; background-color: gray")
            self.button72.setStyleSheet("font-weight: bold; background-color: gray")
            self.button73.setStyleSheet("font-weight: bold; background-color: lightgreen")
        elif value == 7:
            settings_voltageMultiplier = 7
            self.button71.setStyleSheet("font-weight: bold; background-color: gray")
            self.button72.setStyleSheet("font-weight: bold; background-color: lightgreen")
            self.button73.setStyleSheet("font-weight: bold; background-color: gray")
        else:
            settings_voltageMultiplier = 1
            self.button71.setStyleSheet("font-weight: bold; background-color: lightgreen")
            self.button72.setStyleSheet("font-weight: bold; background-color: gray")
            self.button73.setStyleSheet("font-weight: bold; background-color: gray")
        saveSettings("voltageMultiplier")

    def setIdleCurrent(self, direction, increment):
        global settings_idleCurrent

        if direction == 0:
            settings_idleCurrent = round(settings_idleCurrent - increment, 1)
        else:
            settings_idleCurrent = round(settings_idleCurrent + increment, 1)
        if settings_idleCurrent < 0: settings_idleCurrent = float(0)
        if settings_idleCurrent > 10: settings_idleCurrent = float(10)
        self.textbox8.setText(float_to_str(settings_idleCurrent))
        saveSettings("idleCurrent") 

    def setMultiSelection(self, multi):
        global settings_multiSelection

        if multi:
            settings_multiSelection = True
            self.listA.setSelectionMode(QAbstractItemView.MultiSelection)
            self.button91.setStyleSheet("font-weight: bold; background-color: gray")
            self.button92.setStyleSheet("font-weight: bold; background-color: lightgreen")
        else:
            settings_multiSelection = False
            self.listA.setSelectionMode(QAbstractItemView.SingleSelection)
            self.button91.setStyleSheet("font-weight: bold; background-color: lightgreen")
            self.button92.setStyleSheet("font-weight: bold; background-color: gray")
        saveSettings("multiSelection") 

    def focusInEvent(self, event):
        super().focusInEvent(event)
        self.refreshList()

    def refreshList(self):
        self.listA.clear()
        items = []
        pattern = re.compile(r'^capacity_log_\d+\.txt$')

        for filename in os.listdir("/home/fodorbalint/Documents"):            
            match = pattern.match(filename)
            if match:
                items.append(filename)
                # items.append(match.group(0))
        def natural_key(s):
            return [int(text) if text.isdigit() else text.lower()
                for text in re.split(r'(\d+)', s)]

        items.sort(reverse = True, key = natural_key)
        self.listA.addItems(items)

    def selectedFile(self):
        items = self.listA.selectedItems()
        if len(items) == 1:
            self.textB.setPlainText("")
            with open(f"/home/fodorbalint/Documents/clog descriptions.txt", "r") as file:
                descriptions = file.read()
                descriptionsArr = descriptions.split("\n")
                for line in descriptionsArr:
                    pairs = line.strip().split(": ")
                    if "capacity_log_" + pairs[0] + ".txt" == items[0].text():
                        self.textB.setPlainText(pairs[1])

    def on_selection_changed(self, selected, deselected):
        text = ""
        for index in selected.indexes():
            item = self.listA.itemFromIndex(index)
            text = item.text()

        if settings_multiSelection:
            for index in deselected.indexes():
                item = self.listA.itemFromIndex(index)
                text = item.text()

        self.textB.setPlainText("")
        with open(f"/home/fodorbalint/Documents/clog descriptions.txt", "r") as file:
            descriptions = file.read()
            descriptionsArr = descriptions.split("\n")
            for line in descriptionsArr:
                pairs = line.strip().split(": ")
                if "capacity_log_" + pairs[0] + ".txt" == text:
                    self.textB.setPlainText(pairs[1])

    def deleteFile(self):        
        items = self.listA.selectedItems()
        if len(items) == 1:
            row = self.listA.currentRow()
            os.remove("/home/fodorbalint/Documents/" + items[0].text())
            self.refreshList()
            self.listA.setCurrentRow(row)
            self.selectedFile()
    
    def openFromFile(self):
        items = self.listA.selectedItems()
        if len(items) == 0: return
        names = []
        for i in range(0, len(items)):
            names.append(items[i].text())
        
        graphWindow.openFromFile(names)      

class GraphWidget(QWidget):    
    def __init__(self):
        super().__init__()

        self.resistorValue = 0.1014 # 0.1029 calibrated by Ferrex (active) and Parkside multimeters.
        # Voltage measured 51.3 V at resistor divider ends, Ferrex shows 51 V, Parkside 51.2 V.

        self.currentCounter = 0
        self.currentSum = 0
        self.totalWh = 0
        
        self.minV = 100
        self.maxV = -100
        self.new_minV = 100
        self.new_maxV = -100
        self.minCurr = 10
        self.maxCurr = -10
        self.new_minCurr = 10
        self.new_maxCurr = -10
        self.minWh = 0
        self.maxWh = 0
        self.new_minWh = 0
        self.new_maxWh = 0

        self.MIN_TIME_RANGE = 60
        self.MIN_V_RANGE = 0.05
        self.battShutdownThreshold = 3.2
    
        self.setWindowTitle("Capacity graph")
        self.setWindowFlags(Qt.FramelessWindowHint)
        self.setAttribute(Qt.WA_OpaquePaintEvent)
        self.setAttribute(Qt.WA_NoSystemBackground)
        QTimer.singleShot(0, self.force_fullscreen_fix)
        
        self.setMouseTracking(True)
        self.mouseMoveEnabled = True # no dragging or zooming before the graph has updated.
        self.mouseScrollEnabled = True
        self.zoomRectStartX = -1
        self.zoomRectStartY = -1
        self.dragStartX = -1
        self.dragStartY = -1
        self.mouseLeftDown = False
        self.mouseRightDown = False
        self.mouseX = -1
        self.mouseY = -1
        self.graphZoom = 1
        self.graphMiddle = 0.5
        self.timeRange = 1
        self.leftLimit = 0

        self.buttonBar = 30
        self.buttonBarSpace = 10
        self.buttonBarHeight = self.buttonBar + self.buttonBarSpace
        self.screenH = 799
        self.screenV = 443
        self.scaleMaxH = self.screenH
        self.scaleMaxV = self.screenV - self.buttonBarHeight

        self.chart_pixmap = QPixmap(self.scaleMaxH + 1, self.scaleMaxV + 1)
        self.graph_pixmap = QPixmap(self.scaleMaxH + 1, self.scaleMaxV + 1)
        self.chart_pixmap.fill(Qt.black)
        self.graph_pixmap.fill(Qt.black)
        self.buttonbar_pixmap = QPixmap(self.scaleMaxH + 1, 40) # used when the info box is too tall. The 10 px space has to be filled again.
        self.buttonbar_pixmap.fill(Qt.black)
        
        self.timer = QTimer()
        self.timer.setTimerType(Qt.PreciseTimer)
        self.timer.timeout.connect(self.update_graph)
        self.timer.start(1000)
        self.timerRunning = True
        self.stoppedTime = 0
        self.stopButtonEnabled = True

        self.timeRange = 0      
        self.all_values_full = np.empty(36000, dtype=combined_dt) # Enough for 10 hours using a 1-second interval
        self.all_values_multi = []
        self.new_values = np.empty(0, dtype=combined_dt)
        self.prevVoltage = 0
        self.prevCurrent = 0
        self.prevTime = 0
        self.all_values_count = 0
        self.temp_values = [] # measurements added while graph is stopped  
        self.displayed_v = np.full(self.scaleMaxH + 1, -1.0, dtype=float)
        self.displayed_curr = np.full(self.scaleMaxH + 1, -1.0, dtype=float)
        self.displayed_wh = np.full(self.scaleMaxH + 1, -1.0, dtype=float)
        self.stopped = False  
        self.stoppedNow = False

        self.layout1 = QHBoxLayout()
        self.layout1.setContentsMargins(0, 0, 0, 0); 
        self.layout1.setSpacing(5) 
        self.layout1.setAlignment(Qt.AlignTop)

        self.label1 = QLabel("")
        # setting background color is necessary to update the text without layering over the old one when we use os.environ["QT_QPA_PLATFORM"] = "xcb"
        self.label1.setStyleSheet("background-color: black; font-size: 20px; font-weight: bold; color: white")
        self.button1 = QPushButton("Stop")
        self.button1.setStyleSheet("background-color: red; font-size: 20px; font-weight: bold")
        self.button1.setMinimumSize(QSize(80, 30))
        self.button1.setMaximumSize(QSize(80, 30))
        self.button2 = QPushButton("Restart")
        self.button2.setStyleSheet("background-color: gray; font-size: 20px; font-weight: bold")
        self.button2.setMinimumSize(QSize(80, 30))
        self.button2.setMaximumSize(QSize(80, 30))
        self.button3 = QPushButton("Reset view")
        self.button3.setStyleSheet("background-color: gray; font-size: 20px; font-weight: bold")
        self.button3.setMinimumSize(QSize(110, 30))
        self.button3.setMaximumSize(QSize(110, 30))
        self.button4 = QPushButton("Save data")
        self.button4.setStyleSheet("background-color: gray; font-size: 20px; font-weight: bold")
        self.button4.setMinimumSize(QSize(110, 30))
        self.button4.setMaximumSize(QSize(110, 30))

        self.layout1.addWidget(self.label1)
        self.layout1.addWidget(self.button1)
        self.layout1.addWidget(self.button2)
        self.layout1.addWidget(self.button3)
        self.layout1.addWidget(self.button4)      

        self.button1.clicked.connect(self.startStopGraph)
        self.button2.clicked.connect(self.restartGraph)
        self.button3.clicked.connect(self.resetGraph)
        self.button4.clicked.connect(self.saveData)
        self.setLayout(self.layout1)

        self.startTime = time.time()
        self.currentTime = -1
        self.currentTime_temp = -1
        self.preciseTime = 0
        self.update_graph()

    def force_fullscreen_fix(self):
        rect = QApplication.primaryScreen().availableGeometry()
        self.setGeometry(rect)
        self.setFixedSize(self.width(), self.height())

    def openFromFile(self, file_names):
        self.stopped = True
        self.stoppedNow = True
        self.currentTime_temp = -1

        self.timer.stop()
        self.timerRunning = False
        self.button1.setText("Start")
        self.button1.setStyleSheet("background-color: green; font-size: 20px; font-weight: bold")
        self.stoppedTime = 0

        startTime = time.time()

        self.currentTime = -1
        self.maxV = 0
        self.minV = 100

        if len(file_names) == 1:
            self.all_values_multi = []
            self.all_values = np.loadtxt(
                "/home/fodorbalint/Documents/" + file_names[0],
                dtype=combined_dt,
                delimiter=" ",
            )

            print(f"File loaded in {round((time.time() - startTime)*1000)}, length {len(self.all_values)} first element {self.all_values[0]} ts {self.all_values[0][0]} {self.all_values[1][0]} ... {self.all_values[len(self.all_values) - 2][0]} {self.all_values[len(self.all_values) - 1][0]}")

            self.analyzeAllValues()
        else:
            self.all_values_multi = []
            self.incrementArr_multi = []
            
            for i in range(0, len(file_names)):                
                self.all_values_multi.append(np.loadtxt(
                    "/home/fodorbalint/Documents/" + file_names[i],
                    dtype=combined_dt,
                    delimiter=" ",
                ))

                self.analyzeAllValues_multi(i)   

        self.origMinV = self.minV
        self.origMaxV = self.maxV 

        self.graphZoom = 1
        self.graphMiddle = 0.5

        self.timeRange = round(self.currentTime / self.graphZoom)
        self.leftLimit = round(self.graphMiddle * self.currentTime - self.timeRange / 2)

        startTime = time.time()

        self.drawValues() 

        print(f"Drawn in {time.time() - startTime} currentTime {self.currentTime} minV {self.minV} maxV {self.maxV}")      
        
        self.raise_() 
        self.activateWindow()

    def analyzeAllValues(self):
        startTime = time.time()

        vals = np.column_stack(self.all_values["ch1"])
        self.minV = np.min(vals)
        self.maxV = np.max(vals)

        vals = np.column_stack(self.all_values["curr"])
        self.minCurr = np.min(vals)
        self.maxCurr = np.max(vals)

        vals = np.column_stack(self.all_values["wh"])
        self.minWh = np.min(vals)
        self.maxWh = np.max(vals)

        self.currentTime = self.all_values[len(self.all_values) - 1][0]

        arrLen = len(self.all_values)
        newArr = self.all_values
        self.incrementArr = []
        while arrLen >= 800 * 2:
            newArr = self.average_pairs(newArr)
            arrLen = len(newArr)
            self.incrementArr.append(newArr)        

        print(f"Analyzed in {time.time() - startTime}")        

    def analyzeAllValues_multi(self, i):
        vals = np.column_stack(self.all_values_multi[i]["ch1"])

        currentTime = self.all_values_multi[i][len(self.all_values_multi[i]) - 1][0]
        minV = np.min(vals)
        maxV = np.max(vals)
        
        if currentTime > self.currentTime:
            self.currentTime = currentTime
        if minV < self.minV:
            self.minV = minV
        if maxV > self.maxV:
            self.maxV = maxV

        arrLen = len(self.all_values_multi[i])
        newArr = self.all_values_multi[i]
        self.incrementArr_multi.append([])
        
        while arrLen >= 800 * 2:
            newArr = self.average_pairs(newArr)
            arrLen = len(newArr)
            self.incrementArr_multi[i].append(newArr)

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

    def drawValues(self):
        voltageGrid = 10 ** math.floor(math.log10(self.maxV - self.minV) - 1)
        voltage2Grid = voltageGrid * 10
        timeGrid = 10 ** int(math.log10(self.currentTime) - 1)
        time2Grid = timeGrid * 10

        leftLimit = self.leftLimit
        rightLimit = round(self.leftLimit + self.timeRange)

        self.chart_pixmap.fill(Qt.black)
        p = QPainter(self.chart_pixmap)

        for i in range(math.ceil(self.minV/voltageGrid), math.floor(self.maxV/voltageGrid) + 1):
            y = round(self.scaleMaxV * (self.maxV-i*voltageGrid)/(self.maxV-self.minV))
            if i * 0.1 == int(i * 0.1):
                p.setPen(QColor(96, 96, 96))                    
            else:
                p.setPen(QColor(48, 48, 48))
            p.drawLine(0, y, self.scaleMaxH, y)

        for i in range(math.ceil(leftLimit / timeGrid), math.floor(rightLimit / timeGrid) + 1):  
            timeStamp = i * timeGrid 
            x = round(self.scaleMaxH * (timeStamp - leftLimit) / self.timeRange)
            if i % (time2Grid / timeGrid) == 0:
                p.setPen(QColor(96, 96, 96))                    
            else:
                p.setPen(QColor(48, 48, 48))
            p.drawLine(x, 0, x, self.scaleMaxV)
        p.end()

        self.graph_pixmap.fill(Qt.transparent)
        p = QPainter(self.graph_pixmap)

        if len(self.all_values_multi) == 0:
            new_values = []
            values = self.all_values

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
                            
                if len(new_values) < (self.scaleMaxH + 1) * 2:
                    self.new_values = new_values
                    break              

                values = self.incrementArr[incrementIndex]
                incrementIndex += 1
            
            # va = new_values["timestamp_s"][len(new_values) - 1]
            # print(f"Last timestamp {va}")

            timestamps = new_values["timestamp_s"]
            channel_v = new_values["ch1"] # shape (N, 1)
            channel_curr = new_values["curr"] # shape (N, 1)
            channel_wh = new_values["wh"] # shape (N, 1)

            # Compute bin indices for each timestamp
            x = np.round(self.scaleMaxH * (timestamps - leftLimit) / self.timeRange).astype(int)
            # x = np.clip(x, 0, self.scaleMaxH)

            '''error_log = ""
            for i in range (154, 159):
                error_log += f"{i}: {channel_v[i]} {x[i]}\n"
                print (f"{i}: {channel_v[i]} {x[i]}")

            for col in [381, 384, 386]:
                idx = np.where(x == col)[0]
                print(col, idx, channel_v[idx])'''
          
            # Prepare accumulation arrays
            sum_values_v = np.zeros(self.scaleMaxH + 1, dtype=float)
            sum_values_curr = np.zeros(self.scaleMaxH + 1, dtype=float)
            sum_values_wh = np.zeros(self.scaleMaxH + 1, dtype=float)
            count_values = np.bincount(x, minlength=self.scaleMaxH + 1)

            sum_values_v = np.bincount(x, weights=channel_v, minlength=self.scaleMaxH + 1)
            sum_values_curr = np.bincount(x, weights=channel_curr, minlength=self.scaleMaxH + 1)
            sum_values_wh = np.bincount(x, weights=channel_wh, minlength=self.scaleMaxH + 1)

            '''mean_values_v = np.divide(sum_values_v, count_values, out=np.zeros_like(sum_values_v), where=count_values != 0)
            mean_values_curr = np.divide(sum_values_curr, count_values, out=np.zeros_like(sum_values_curr), where=count_values != 0)
            mean_values_wh = np.divide(sum_values_wh, count_values, out=np.zeros_like(sum_values_wh), where=count_values != 0)'''
            
            mask = count_values > 0
            self.displayed_v = np.full(self.scaleMaxH + 1, -1.0, dtype=float)
            self.displayed_curr = np.full(self.scaleMaxH + 1, -1.0, dtype=float)
            self.displayed_wh = np.full(self.scaleMaxH + 1, -1.0, dtype=float)
            self.displayed_v[mask] = sum_values_v[mask] / count_values[mask]

            '''for i in range (379, 391):
                print (f"{i}: {self.displayed_v[i]} {sum_values_v[i]} {count_values[i]}")
                error_log += f"{i}: {self.displayed_v[i]} {sum_values_v[i]} {count_values[i]}\n"

            with open(f"/home/fodorbalint/Documents/error_log.txt", "w") as file:
                file.write(error_log)'''
            
            self.displayed_curr[mask] = sum_values_curr[mask] / count_values[mask]
            self.displayed_wh[mask] = sum_values_wh[mask] / count_values[mask]

            # Mask out -1 (invalid)
            valid = self.displayed_v != -1
            indices = np.arange(len(self.displayed_v))[valid]
            v1 = self.displayed_v[valid]            
            curr1 = self.displayed_curr[valid]
            wh1 = self.displayed_wh[valid]

            y1 = np.round(self.scaleMaxV * (self.maxV - v1) / (self.maxV - self.minV))
            points1 = [QPointF(i, y1) for i, y1 in zip(indices, y1)]
            p.setPen(Qt.yellow)
            p.drawPolyline(QPolygonF(points1))

            if self.minCurr != self.maxCurr:
                # fill only the lower half of the graph
                y2 = np.round(self.scaleMaxV / 2 + self.scaleMaxV / 2 * (self.maxCurr - curr1) / (self.maxCurr - self.minCurr))
            else:
                y2 = self.scaleMaxV + curr1 * 0 # ensure y2 is an array

            points2 = [QPointF(i, y2) for i, y2 in zip(indices, y2)]
            p.setPen(Qt.green)
            p.drawPolyline(QPolygonF(points2))

            if self.minWh != self.maxWh:
                y3 = np.round(self.scaleMaxV * (self.maxWh - wh1) / (self.maxWh - self.minWh))
            else:
                y3 = wh1 * 0 + self.scaleMaxV
            points3 = [QPointF(i, y3) for i, y3 in zip(indices, y3)]
            p.setPen(Qt.cyan)
            p.drawPolyline(QPolygonF(points3))

        else:
            self.new_values_multi = []

            for i in range(0, len(self.all_values_multi)):
                new_values = []
                values = self.all_values_multi[i]
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

                    if len(new_values) < (self.scaleMaxH + 1) * 2:         
                        self.new_values_multi.append(new_values)
                        break              

                    values = self.incrementArr_multi[i][incrementIndex]
                    incrementIndex += 1

            self.displayed_v_multi = []

            for i in range(0, len(self.all_values_multi)):
                timestamps = self.new_values_multi[i]["timestamp_s"]
                channel_v = self.new_values_multi[i]["ch1"] # shape (N, 1)
                # channel_curr = new_values_multi[i]["curr"] # shape (N, 1)
                # channel_wh = new_values_multi[i]["wh"] # shape (N, 1)
            
                # Compute bin indices for each timestamp
                x = np.round(self.scaleMaxH * (timestamps - leftLimit) / self.timeRange).astype(int)
                x = np.clip(x, 0, self.scaleMaxH)

                # Prepare accumulation arrays
                sum_values_v = np.zeros(self.scaleMaxH + 1, dtype=float)
                # sum_values_curr = np.zeros(self.scaleMaxH + 1, dtype=float)
                # sum_values_wh = np.zeros(self.scaleMaxH + 1, dtype=float)
                count_values = np.bincount(x, minlength=self.scaleMaxH + 1)

                sum_values_v = np.bincount(x, weights=channel_v, minlength=self.scaleMaxH + 1)
                # sum_values_curr = np.bincount(x, weights=channel_curr, minlength=self.scaleMaxH + 1)
                # sum_values_wh = np.bincount(x, weights=channel_wh, minlength=self.scaleMaxH + 1)

                mask = count_values > 0
                self.displayed_v = np.full(self.scaleMaxH + 1, -1.0, dtype=float)
                # self.displayed_curr = np.full(self.scaleMaxH + 1, -1.0, dtype=float)
                # self.displayed_wh = np.full(self.scaleMaxH + 1, -1.0, dtype=float)
                self.displayed_v[mask] = sum_values_v[mask] / count_values[mask]
                # self.displayed_curr[mask] = sum_values_curr[mask] / count_values[mask]
                # self.displayed_wh[mask] = sum_values_wh[mask] / count_values[mask]
                self.displayed_v_multi.append(self.displayed_v)

                # Mask out -1 (invalid)
                valid = self.displayed_v != -1
                indices = np.arange(len(self.displayed_v))[valid]
                v1 = self.displayed_v[valid]
                curr1 = self.displayed_curr[valid]
                wh1 = self.displayed_wh[valid]

                y1 = np.round(self.scaleMaxV * (self.maxV - v1) / (self.maxV - self.minV))
                points1 = [QPointF(i, y1) for i, y1 in zip(indices, y1)]
                p.setPen(QColor(255 - round(i * 255 / (len(self.all_values_multi) - 1)), 255, round(i * 255 / (len(self.all_values_multi) - 1))))
                p.drawPolyline(QPolygonF(points1))

                '''if self.minCurr != self.maxCurr:
                    # fill only the lower half of the graph
                    y2 = np.round(self.scaleMaxV / 2 + self.scaleMaxV / 2 * (self.maxCurr - curr1) / (self.maxCurr - self.minCurr))
                else:
                    y2 = self.scaleMaxV + curr1 * 0 # ensure y2 is an array

                points2 = [QPointF(i, y2) for i, y2 in zip(indices, y2)]
                p.setPen(Qt.green)
                p.drawPolyline(QPolygonF(points2))

                if self.minWh != self.maxWh:
                    y3 = np.round(self.scaleMaxV * (self.maxWh - wh1) / (self.maxWh - self.minWh))
                else:
                    y3 = wh1 * 0 + self.scaleMaxV
                points3 = [QPointF(i, y3) for i, y3 in zip(indices, y3)]
                p.setPen(Qt.cyan)
                p.drawPolyline(QPolygonF(points3))'''

            p.end()

        self.update()

    def restartGraph(self):
        self.all_values_full = np.empty(36000, dtype=combined_dt)
        self.all_values_multi = []
        self.displayed_v = np.full(self.scaleMaxH + 1, -1.0, dtype=float)
        self.displayed_curr = np.full(self.scaleMaxH + 1, -1.0, dtype=float)
        self.displayed_wh = np.full(self.scaleMaxH + 1, -1.0, dtype=float)
        self.all_values_count = 0
        self.minV = 100
        self.maxV = -100
        self.minCurr = 10
        self.maxCurr = -10
        self.minWh = 0
        self.maxWh = 0
        self.totalWh = 0
        self.timeRange = 1
        self.leftLimit = 0
        self.graphMiddle = 0.5
        self.graphZoom = 1
        self.startTime = time.time()
        self.currentTime = -1
        self.currentTime_temp = -1
        self.preciseTime = 0
        self.prevVoltage = 0
        self.prevCurrent = 0
        self.prevTime = 0
        self.timer.start() # will fire 1 s from now.
        self.timerRunning = True
        self.button1.setText("Stop")
        self.button1.setStyleSheet("background-color: red; font-size: 20px; font-weight: bold")
        self.stopped = False
        self.chart_pixmap.fill(Qt.black)
        self.graph_pixmap.fill(Qt.black)
        self.update()
        self.update_graph()

    def startStopGraph(self):
        if self.stopButtonEnabled:
            if self.timerRunning:
                self.timer.stop()
                self.timerRunning = False
                self.button1.setText("Start")
                self.button1.setStyleSheet("background-color: green; font-size: 20px; font-weight: bold")
                self.stoppedTime = time.time() - self.startTime
                self.timeFraction = (self.stoppedTime - self.preciseTime) * 1000
                if self.timeFraction > 1000: self.timeFraction = 1000
                
            else:
                if self.stoppedTime != 0:
                    self.stopButtonEnabled = False
                    pauseTime = time.time() - self.startTime - self.stoppedTime
                    self.startTime += pauseTime
                    # will ensure we keep exactly 1s intervals in the data set
                    QTimer.singleShot(round(1000 - self.timeFraction), self.startGraph)
                else: # exit loaded file, start anew
                    self.restartGraph()

    def startGraph(self):
        self.timer.start() # will fire 1 s from now.
        self.timerRunning = True
        self.button1.setText("Stop")
        self.button1.setStyleSheet("background-color: red; font-size: 20px; font-weight: bold")
        self.update_graph()
        self.stopButtonEnabled = True

    def resetGraph(self):
        self.timeRange = self.currentTime
        self.leftLimit = 0
        self.minV = self.origMinV
        self.maxV = self.origMaxV
        self.graphZoom = 1
        self.graphMiddle = 0.5
        self.drawValues()

    def mousePressEvent(self, event):
        global evy

        if event.y() < self.buttonBar:
            return
        if event.y() < self.buttonBarHeight:
            evy = self.buttonBarHeight
        else:
            evy = event.y()

        evx = event.x()
        if evx > 799:
            evx = 799

        # No action in a waiting state or without scale
        if self.currentTime == -1 or self.minV == self.maxV:
            return

        if event.button() == Qt.RightButton:
            self.dragStartX = evx
            self.dragStartY = evy 
            # drag a stopped graph or reset if mouse is not dragged 
            if self.stopped or not self.timerRunning:
                self.dragMoveX = evx
                self.mouseRightDown = True
            # else stop graph if mouse is not dragged
        elif event.button() == Qt.LeftButton:
            # reset running graph if mouse is not dragged
            if not self.stopped and self.timerRunning:
                self.dragStartX = evx
                self.dragStartY = evy
            # drag a zoom rectangle when graph is stopped or restart graph
            else:
                self.mouseLeftDown = True
                self.zoomRectStartX = evx               
                self.zoomRectStartY = evy                    
                self.displayLeftLimit = 0
                self.displayTimeRange = 0
                self.displayMinV = 0
                self.displayMaxV = 0                

    def mouseMoveEvent(self, event):
        global evy

        self.mouseX = event.x()
        self.mouseY = event.y()
        
        if self.mouseY < self.buttonBarHeight:
            evy = self.buttonBarHeight
        else:
            evy = event.y()

        if self.mouseX > self.scaleMaxH:
            self.mouseX = self.scaleMaxH
        if self.mouseY > self.screenV:
            self.mouseY = self.screenV
        if self.mouseY < self.buttonBarHeight:
            self.mouseY = self.buttonBarHeight

        if self.stopped or not self.timerRunning:
        # print(f"moveEvent {self.mouseX} {self.mouseY}")
            if self.mouseLeftDown and self.mouseX != self.zoomRectStartX and evy != self.zoomRectStartY:
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
                if self.displayTimeRange < self.MIN_TIME_RANGE:
                    if self.currentTime < self.MIN_TIME_RANGE:
                        # does not currently happen
                        self.displayLeftLimit = 0
                        self.displayTimeRange = self.currentTime
                    else:
                        offset = round((self.MIN_TIME_RANGE - self.displayTimeRange) / 2)
                        self.displayLeftLimit -= offset
                        self.displayTimeRange = self.MIN_TIME_RANGE
                        if self.displayLeftLimit < 0:
                            self.displayLeftLimit = 0
                        elif self.displayLeftLimit + self.displayTimeRange > self.currentTime:
                            self.displayLeftLimit = self.currentTime - self.displayTimeRange

                self.displayMinV = self.minV + (443 - endY) / (443 - self.buttonBarHeight) * (self.maxV - self.minV)
                self.displayMaxV = self.maxV - (startY - self.buttonBarHeight) / (443 - self.buttonBarHeight) * (self.maxV - self.minV)

                if (self.displayMaxV - self.displayMinV) < self.MIN_V_RANGE:
                    if self.origMaxV - self.origMinV < self.MIN_V_RANGE:
                        self.displayMinV = self.origMinV
                        self.displayMaxV = self.origMaxV
                    else:
                        # put small range in the center of minimum range, but within the boundaries of the original scale
                        offset = (self.MIN_V_RANGE - (self.displayMaxV - self.displayMinV)) / 2
                        self.displayMinV -= offset
                        self.displayMaxV += offset
                        if self.displayMinV < self.origMinV:
                            self.displayMinV = self.origMinV
                            self.displayMaxV = self.displayMinV + self.MIN_V_RANGE
                        elif self.displayMaxV > self.origMaxV:
                            self.displayMaxV = self.origMaxV
                
            elif self.mouseRightDown and self.mouseMoveEnabled:
                self.mouseMoveEnabled = False
                deltaX = self.mouseX - self.dragMoveX
                self.dragMoveX = self.mouseX
                self.leftLimit -= self.timeRange * deltaX / 799
                self.graphMiddle = (self.leftLimit + self.timeRange / 2) / self.currentTime

                self.leftLimit = round(self.graphMiddle * self.currentTime - self.timeRange / 2)
                if self.leftLimit < 0:
                    self.leftLimit = 0
                    self.graphMiddle = self.timeRange / 2 / self.currentTime
                elif self.leftLimit + self.timeRange > self.currentTime:
                    self.leftLimit = self.currentTime - self.timeRange
                    self.graphMiddle = (self.currentTime - self.timeRange / 2) / self.currentTime
                
                self.drawValues()
        
        self.update()

    def mouseReleaseEvent(self, event):
        if event.y() < self.buttonBarHeight:
            evy = self.buttonBarHeight
        else:
            evy = event.y()

        evx = event.x()
        if evx > 799:
            evx = 799

        # No action in a waiting state or without scale
        if self.currentTime == -1 or self.minV == self.maxV:
            return

        if event.button() == Qt.RightButton:
            # stop graph if mouse is not dragged
            if not self.stopped and self.timerRunning:
                if evx == self.dragStartX and evy == self.dragStartY:
                    self.new_minV = self.minV
                    self.new_maxV = self.maxV
                    self.new_minCurr = self.minCurr
                    self.new_maxCurr = self.maxCurr
                    self.new_minWh = self.minWh
                    self.new_maxWh = self.maxWh

                    # if we zoomed in on the live graph, it should stay
                    self.graphZoom = self.currentTime / (self.currentTime - self.leftLimit)
                    self.stopped = True
                    # if we stop and start the view within a second, currentTime_temp will not get a new, correct value.
                    self.currentTime_temp = self.currentTime
                    print(f"Live graph stopped at {self.currentTime}")
            else:
                # finish dragging a stopped graph
                self.mouseRightDown = False
                # reset if mouse was not dragged
                if evx == self.dragStartX and evy == self.dragStartY:                    
                    self.resetGraph()
        else:
            if not self.stopped and self.timerRunning:
                # reset running graph if mouse was not dragged
                if evx == self.dragStartX and evy == self.dragStartY:                    
                    self.resetGraph()
            else:
                # restart graph
                # last condition is to prevent firing when a file is loaded.
                if evx == self.zoomRectStartX and evy == self.zoomRectStartY and self.timerRunning:
                    temp_array = np.array(self.temp_values, dtype=combined_dt)
                    new_count = len(temp_array)

                    print(f"Live graph started. Existing data count: {self.all_values_count}, new data count: {new_count}")

                    # if, after adding the temporary elements, all_values_full grows larger than its preallocated size, we double the size or set it higher if there are more elements now. 
                    if self.all_values_count + new_count > len(self.all_values_full):
                        new_size = max(len(self.all_values_full) * 2, self.all_values_count + new_count)
                        self.all_values_full = np.resize(self.all_values_full, new_size)
                        print(f"Array size doubled to {new_size} after restarting")

                    self.all_values_full[self.all_values_count:self.all_values_count + new_count] = temp_array
                    self.all_values_count += new_count
                    self.temp_values.clear()            
                    self.all_values = self.all_values_full[:self.all_values_count]
                    
                    self.minCurr = self.new_minCurr
                    self.maxCurr = self.new_maxCurr            
                    self.minWh = self.new_minWh
                    self.maxWh = self.new_maxWh
                    
                    # only zoom out if we are not at the right side of the graph at full scale height, or if we are viewing the full scale to start with.

                    if not (self.leftLimit > 0 and self.currentTime == self.leftLimit + self.timeRange and self.minV == self.origMinV and self.maxV == self.origMaxV):
                        self.timeRange = self.currentTime_temp
                        self.leftLimit = 0
                        self.graphZoom = 1
                        self.graphMiddle = 0.5
                    else:
                        self.leftLimit = self.currentTime_temp - self.timeRange 

                    self.currentTime = self.currentTime_temp

                    if self.all_values_count != self.currentTime + 1:
                        raise ValueError(f"Wrong timestamp, all_values_count: {self.all_values_count}, currentTime: {self.currentTime}")

                    self.minV = self.new_minV
                    self.maxV = self.new_maxV
                    self.stopped = False
                    
                    self.drawValues()
                
                # finish dragging a zoom rectangle
                elif evx != self.zoomRectStartX and evy != self.zoomRectStartY:
                    self.leftLimit = round(self.displayLeftLimit)
                    self.timeRange = round(self.displayTimeRange)
                    self.graphZoom = self.currentTime / self.timeRange 
                    self.graphMiddle = (self.leftLimit + self.timeRange / 2) / self.currentTime
                    self.minV = self.displayMinV
                    self.maxV = self.displayMaxV
                    self.drawValues()
                
                # remove rectangle lines if horizontal or vertical drag is 0
                else:
                    self.update()

                self.mouseLeftDown = False

    def leaveEvent(self, event):
        self.mouseY = self.buttonBarHeight
        self.update()

#    def mouseReleaseEvent(self, event):
#        if event.button() == Qt.LeftButton:
    
    def wheelEvent(self, event):
        evx = event.x()
        if evx > self.scaleMaxH:
            evx = self.scaleMaxH

        delta = event.angleDelta().y()

        if self.stopped:
            if self.mouseScrollEnabled: 
                self.mouseScrollEnabled = False
                timeAtCursor = self.leftLimit + evx / self.scaleMaxH * self.timeRange

                if delta > 0:
                    if self.currentTime > self.MIN_TIME_RANGE:
                        self.graphZoom *= 1.1
                        self.timeRange = round(self.currentTime / self.graphZoom)

                        if self.timeRange < self.MIN_TIME_RANGE:
                            self.timeRange = self.MIN_TIME_RANGE
                            self.graphZoom = self.currentTime / self.timeRange

                        self.leftLimit = round(timeAtCursor - evx / self.scaleMaxH * self.timeRange)
                        self.graphMiddle = (self.leftLimit + self.timeRange / 2) / self.currentTime
                else:
                    self.graphZoom /= 1.1
                    if self.graphZoom < 1:
                        self.graphZoom = 1
                    self.timeRange = round(self.currentTime / self.graphZoom)
                    self.leftLimit = round(timeAtCursor - evx / self.scaleMaxH * self.timeRange)
                    if self.leftLimit < 0:
                        self.leftLimit = 0
                        self.graphMiddle = self.timeRange / 2 / self.currentTime
                    elif self.leftLimit + self.timeRange > self.currentTime:
                        self.leftLimit = self.currentTime - self.timeRange
                        self.graphMiddle = (self.currentTime - self.timeRange / 2) / self.currentTime
                    else:
                        self.graphMiddle = (self.leftLimit + self.timeRange / 2) / self.currentTime

                self.drawValues()
        else: # zoom in on the right side only, able to receive new updates
            if self.mouseScrollEnabled: 
                self.mouseScrollEnabled = False

                if delta > 0:
                    if self.currentTime > self.MIN_TIME_RANGE:
                        self.graphZoom *= 1.1
                        self.timeRange = round(self.currentTime / self.graphZoom)
                        if self.timeRange < self.MIN_TIME_RANGE:
                            self.timeRange = self.MIN_TIME_RANGE
                            self.graphZoom = self.currentTime / self.timeRange
                else:
                    self.graphZoom /= 1.1
                    if self.graphZoom < 1:
                        self.graphZoom = 1
                    self.timeRange = round(self.currentTime / self.graphZoom)
                self.leftLimit = round(self.currentTime - self.timeRange)

                self.drawValues()

    def saveData(self):
        if self.all_values_count == 0: return

        self.button3.setEnabled(False)

        directory_path = "/home/fodorbalint/Documents"
        highest_number = get_highest_number(directory_path)
        highest_number += 1 

        '''content = ""
        for v in self.all_values:
            for v2 in v:                    
                content += str(v2) + " "
            content = content[0:len(content)-1] + "\n"
        with open(f"/home/fodorbalint/Documents/capacity_log_{highest_number}.txt", "w") as file:
            file.write(content)'''
        startTime = time.time()
        np.savetxt(
            f"/home/fodorbalint/Documents/capacity_log_{highest_number}.txt",
            self.all_values,
            fmt = ("%d", "%g", "%g", "%g"),
            delimiter=" "
        )
        
        print (f"capacity_log_{highest_number}.txt saved in {time.time() - startTime:.3f} s.")

        self.button4.setText("Saved")
        self.button3.setEnabled(True)
        settingsWindow.refreshList()

        QTimer.singleShot(1000, self.resetButton)
    
    def resetButton(self):
        self.button4.setText("Save data")

    def update_graph(self):

        voltage, voltage2, battVoltage = read_channel2()
        voltage *= settings_voltageMultiplier
        current = voltage2 / self.resistorValue + settings_idleCurrent / 1000

        if battVoltage <= self.battShutdownThreshold:
            self.timer.stop()
            os.system("sudo shutdown now")

        # updates stop if either the current or the voltage thresholds are reached
        # if, in the condition we use self.prevCurrent instead of current, the triggering point will be saved as well.
        if self.currentTime > 0 and settings_trigger and (current < settings_triggerCurrentEnd or (settings_voltageDirection and self.prevVoltage > settings_triggerVoltageEnd * settings_voltageMultiplier) or (not settings_voltageDirection and self.prevVoltage < settings_triggerVoltageEnd * settings_voltageMultiplier)):
            self.label1.setText(f"Finished: {voltage:.4f} V {current:.4f} A {battVoltage:.3f} V ")
            if not self.stopped:
                self.stopped = True
                self.stoppedNow = True
                self.currentTime_temp = -1
            return

        # updates start if both the current and voltage requirements are met
        if self.currentTime == -1 and settings_trigger and (current < settings_triggerCurrentStart or (settings_voltageDirection and voltage < settings_triggerVoltageStart * settings_voltageMultiplier) or (not settings_voltageDirection and voltage > settings_triggerVoltageStart * settings_voltageMultiplier)):
            self.prevVoltage = voltage
            self.prevCurrent = current
            self.label1.setText(f"Waiting: {voltage:.4f} V {current:.4f} A {battVoltage:.3f} V ")
            return
        elif self.currentTime == -1 and settings_trigger:
            # save last measurement point before trigger
            '''self.startTime = time.time() - 1
            self.totalWh = (0 + self.prevVoltage) / 2 * self.prevCurrent * 1 / 3600
            self.all_values_full[0] = (0, self.prevVoltage, self.prevCurrent, self.totalWh)
            self.all_values_count = 1
            self.minV = min(self.minV, self.prevVoltage)
            self.maxV = max(self.maxV, self.prevVoltage)
            self.minCurr = min(self.minCurr, self.prevCurrent)
            self.maxCurr = max(self.maxCurr, self.prevCurrent)
            self.minWh = min(self.minWh, self.totalWh)
            self.maxWh = max(self.maxWh, self.totalWh)
            self.currentCounter = 1
            self.currentSum = self.prevCurrent
            '''
            # do not save
            self.startTime = time.time()
        
        self.currentCounter += 1
        self.currentSum += current

        if (self.currentCounter < 1000 and self.currentCounter % 100 == 0) or self.currentCounter % 1000 == 0:
            print(f"Counter: {self.currentCounter}, V1 {voltage:.4f} V2 {voltage2:.4f} I {current:.4f} Iavg {self.currentSum / self.currentCounter:.4f}")

        if not self.stopped:
            self.preciseTime = time.time() - self.startTime
            self.currentTime = round(self.preciseTime)
            self.totalWh += (self.prevVoltage + voltage) / 2 * current * (self.currentTime - self.prevTime) / 3600
            # self.totalWh += pow((self.prevVoltage + voltage) / 2, 2) / self.resistorValue * (self.currentTime - self.prevTime) / 3600
            self.all_values_full[self.all_values_count] = (self.currentTime, voltage, current, self.totalWh)
            self.all_values_count += 1

            # double the size if it reaches the limit
            if self.all_values_count >= len(self.all_values_full):
                new_size = len(self.all_values_full) * 2
                self.all_values_full = np.resize(self.all_values_full, new_size)
                print(f"Array size doubled to {new_size} ")

            self.label1.setText(f"{self.currentTime} s {voltage:.4f} V {current:.3f} A {self.totalWh:.3f} Wh {battVoltage:.3f} V")
            
        else:
            self.preciseTime = time.time() - self.startTime
            self.currentTime_temp = round(self.preciseTime)
            self.totalWh += (self.prevVoltage + voltage) / 2 * current * (self.currentTime_temp - self.prevTime) / 3600
            # self.totalWh += pow((self.prevVoltage + voltage) / 2, 2) / self.resistorValue * (self.currentTime_temp - self.prevTime) / 3600

            self.temp_values.append((self.currentTime_temp, voltage, current, self.totalWh))
            
            self.prevVoltage = voltage
            self.prevCurrent = current
            self.prevTime = self.currentTime_temp
            
            self.new_minV = min(self.new_minV, voltage)
            self.new_maxV = max(self.new_maxV, voltage)
            self.new_minCurr = min(self.new_minCurr, current)
            self.new_maxCurr = max(self.new_maxCurr, current)
            self.new_minWh = min(self.new_minWh, self.totalWh)
            self.new_maxWh = max(self.new_maxWh, self.totalWh)

            self.label1.setText(f"{self.currentTime_temp} s {voltage:.4f} V {current:.3f} A {self.totalWh:.3f} Wh {battVoltage:.3f} V")
            
            return

        self.prevVoltage = voltage
        self.prevCurrent = current
        self.prevTime = self.currentTime

        self.minV = min(self.minV, voltage)
        self.maxV = max(self.maxV, voltage)
        self.minCurr = min(self.minCurr, current)
        self.maxCurr = max(self.maxCurr, current)
        self.minWh = min(self.minWh, self.totalWh)
        self.maxWh = max(self.maxWh, self.totalWh)
        self.origMinV = self.minV
        self.origMaxV = self.maxV

        if self.all_values_count == 1:
            return

        # slice to actual size
        self.all_values = self.all_values_full[:self.all_values_count]

        if len(self.all_values) == 1 or self.minV == self.maxV: return

        arrLen = len(self.all_values)
        newArr = self.all_values
        self.incrementArr = []
        while arrLen >= (self.scaleMaxH + 1) * 2:
            newArr = self.average_pairs(newArr)
            arrLen = len(newArr)
            self.incrementArr.append(newArr)

        self.timeRange = self.currentTime - self.leftLimit

        self.drawValues()

    def paintEvent(self, event):
        painter = QPainter(self)
        painter.drawPixmap(0, self.buttonBarHeight, self.chart_pixmap)
        painter.drawPixmap(0, self.buttonBarHeight, self.graph_pixmap)

        font = QFont("Arial", 12)
        painter.setFont(font)
        metrics = QFontMetrics(font)
        text_height = metrics.height() # 19

        # zoom rectangle

        if self.mouseLeftDown and self.zoomRectStartX != -1 and self.zoomRectStartY != -1 and self.displayMaxV != 0:            
            painter.setPen(Qt.white)

            text1 = f"{self.displayLeftLimit} - {self.displayLeftLimit + self.displayTimeRange} s"
            text2 = f"{self.displayMinV:.4f} - {self.displayMaxV:.4f} V"
            text1_width = metrics.horizontalAdvance(text1)          
            text2_width = metrics.horizontalAdvance(text2)         
            rectW = max(text1_width, text2_width) + 10

            if len(self.all_values_multi) == 0:
                rectH = 10 + 4 * text_height
            else:
                rectH = 10 + 2 * text_height
                
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

            if len(self.all_values_multi) == 0:
                rightLimit = self.displayLeftLimit + self.displayTimeRange
                sum1 = 0
                sum2 = 0
                count = 0
                
                # startTime = time.time()

                # Select elements within timestamp range
                mask = (self.all_values["timestamp_s"] >= self.displayLeftLimit) & (self.all_values["timestamp_s"] <= rightLimit)

                # Count how many elements are included
                count = np.count_nonzero(mask)

                # Apply mask to each numeric field and sum
                result = {
                    name: self.all_values[name][mask].sum(dtype=np.float64)
                    for name in ("ch1", "curr")
                }
                
                offset = text_height
                offset += text_height
                painter.setPen(Qt.yellow)
                painter.drawText(rectX + 5, rectY + text_height + 1 + offset, str(round(result["ch1"] / count, 4)) + " V avg")
                offset += text_height
                painter.setPen(Qt.green)
                painter.drawText(rectX + 5, rectY + text_height + 1 + offset, str(round(result["curr"] / count, 4)) + " A avg")

        # info box

        elif not (self.mouseX == -1 and self.mouseY == -1):
            painter.setPen(Qt.gray)
            painter.drawLine(self.mouseX, self.buttonBarHeight, self.mouseX, self.scaleMaxV + self.buttonBarHeight)
            painter.drawLine(0, self.mouseY, self.scaleMaxH, self.mouseY)

            if len(self.all_values_multi) == 0:
                value_v = self.displayed_v[self.mouseX]
                value_curr = self.displayed_curr[self.mouseX]
                value_wh = self.displayed_wh[self.mouseX]
                text_count = 7
                if value_v == -1 and self.mouseX != 0:
                    i = self.mouseX - 1
                    while i >= 0 and self.displayed_v[i] == -1:
                        i -= 1
                    value_v = self.displayed_v[i]
                    value_curr = self.displayed_curr[i]
                    value_wh = self.displayed_wh[i]

                if len(self.new_values) >= 800 and self.displayed_v[self.mouseX] != -1:
                    displayTimestamp = round(self.leftLimit + self.mouseX / self.scaleMaxH * self.timeRange)
                else:
                    displayTimestamp = -1
                    lastDisplayTimestamp = -1
                    for v in self.new_values:
                        timestamp = float(v[0] - self.leftLimit)
                        x = round(self.scaleMaxH * timestamp / self.timeRange)
                        lastDisplayTimestamp = v[0]                            
                        if self.mouseX >= x:
                            displayTimestamp = lastDisplayTimestamp
            else:
                text_count = 2 + len(self.all_values_multi)
                text_v = []
                for i in range (0, len(self.all_values_multi)):
                    value_v = self.displayed_v_multi[i][self.mouseX]
                    if value_v == -1 and self.mouseX != 0:
                        j = self.mouseX - 1
                        while j >= 0 and self.displayed_v_multi[i][j] == -1:
                            j -= 1
                        value_v = self.displayed_v_multi[i][j]                        
                    text_v.append(f"{round(value_v, 4)} V")
                
                displayTimestamp = round(self.leftLimit + self.mouseX / self.scaleMaxH * self.timeRange)            

            text1 = f"{round(displayTimestamp)} s"
            text2 = f"{round(self.minV + (self.scaleMaxV - (self.mouseY - self.buttonBarHeight)) / self.scaleMaxV * (self.maxV - self.minV), 4)} V"
            # Example: mouse is at lower 3/4 or screen
            # mouse Y - buttonBarHeight = 300 (out of 0 to 403 scale) 
            # self.scaleMaxV - (self.mouseY - self.buttonBarHeight) = 103
            # 103 / (self.scaleMaxV / 2) = 0.51
            
            text1_width = metrics.horizontalAdvance(text1)
            text2_width = metrics.horizontalAdvance(text2)

            if len(self.all_values_multi) == 0:
                if self.minCurr != self.maxCurr:
                    text3 = f"{round(self.minCurr + (self.scaleMaxV - (self.mouseY - self.buttonBarHeight)) / (self.scaleMaxV / 2) * (self.maxCurr - self.minCurr), 4)} A"
                else:
                    text3 = "0 A"
                if self.minWh != self.maxWh:
                    text4 = f"{round(((self.scaleMaxV - (self.mouseY - self.buttonBarHeight)) / self.scaleMaxV) * (self.maxWh - self.minWh), 4)} Wh"
                else:
                    text4 = f"0 Wh"
                text3_width = metrics.horizontalAdvance(text3)
                text4_width = metrics.horizontalAdvance(text4)

                text5 = f"{round(value_v, 4)} V"
                text6 = f"{round(value_curr, 4)} A"
                text7 = f"{round(value_wh, 3)} Wh"
                text5_width = metrics.horizontalAdvance(text5)
                text6_width = metrics.horizontalAdvance(text6)
                text7_width = metrics.horizontalAdvance(text7)
                if value_v == -1:
                    text_count -= 3

                rectW = max(text1_width, text2_width, text3_width, text4_width, text5_width, text6_width, text7_width) + 10
            else:
                rectW = max(text1_width, text2_width) + 10
                for t in text_v:
                    w = metrics.horizontalAdvance(t) + 10
                    if w > rectW:
                        rectW = w

            rectH = 10 + text_count * text_height

            if self.mouseX < self.screenH - 9 - rectW:
                rectX = self.mouseX + 10                    
            else:
                rectX = self.mouseX - 10 - rectW
            if self.mouseY < self.screenV - 9 - rectH:
                rectY = self.mouseY + 10                    
            else:
                rectY = self.mouseY - 10 - rectH

            painter.setPen(Qt.gray)
            background_color = QColor(0, 0, 0, 200)
            rect = QRect(rectX, rectY, rectW, rectH)
            painter.fillRect(rect, background_color)
            painter.drawRect(rect)
            painter.drawText(rectX + 5, rectY + text_height + 1, text1)
            painter.drawText(rectX + 5, rectY + 2 * text_height + 1, text2)
            
            if len(self.all_values_multi) == 0:
                painter.drawText(rectX + 5, rectY + 3 * text_height + 1, text3)
                painter.drawText(rectX + 5, rectY + 4 * text_height + 1, text4)

                if value_v != -1: # should always be true
                    painter.setPen(Qt.yellow)
                    painter.drawText(rectX + 5, rectY + 5 * text_height + 1, text5)
                    painter.setPen(Qt.green)
                    painter.drawText(rectX + 5, rectY + 6 * text_height + 1, text6)
                    painter.setPen(Qt.cyan)
                    painter.drawText(rectX + 5, rectY + 7 * text_height + 1, text7)
            
            else:
                for i in range(0, len(text_v)):
                    t = text_v[i]
                    painter.setPen(QColor(255 - round(i * 255 / (len(self.all_values_multi) - 1)), 255, round(i * 255 / (len(self.all_values_multi) - 1))))
                    painter.drawText(rectX + 5, rectY + (3 + i) * text_height + 1, t)
                
                painter.drawPixmap(0, 0, self.buttonbar_pixmap)
        
        painter.end()

        self.mouseMoveEnabled = True
        self.mouseScrollEnabled = True 


    
useAd = True
v1multiplier = 16.287

if useAd:
    setup_ad()
else:
    setup_ads()

readSettings()
app = QApplication([])  
app.setStyle("Fusion")
settingsWindow = SettingsWindow()
graphWindow = GraphWidget()
settingsWindow.show()
graphWindow.show()        
app.exec_() 