# Laser cutter settings: 40 % speed, 50 % power

import math, sys

def r(a, b = 3):
    return f"{a:.3f}".rstrip("0").rstrip(".")

def L(endX, endY):
    return " L " + r(endX) + " " + r(endY)

def M(x, y):
    return " M " + r(x) + " " + r(y)

series = int(sys.argv[1])
if sys.argv[2] == "1":
    startHigh = True
else:
    startHigh = False

w = 140.5 # sys.argv[3]
h = 127.5 # sys.argv[4]

laserCut = True

pathClass = "l1"
if laserCut:
    pathClass = "l"

pair = True
if series % 2 == 0:
    sideLength = w / (series * 1.5 + 0.5)
else:
    pair = False
    sideLength = w / ((series - 1) * 1.5 + 2)

d1 = sideLength * math.sqrt(3) / 2

print (f"Side length: {sideLength}")

if startHigh:
    path = M(0, d1)
    upperStartX = 0
else:    
    path = M(0, 2 * d1) + L(sideLength / 2, d1)
    upperStartX = sideLength / 2

for i in range (0, int((series - series % 2) / 2)):
    startX = upperStartX + i * sideLength * 3
    if startHigh:
        path += L(startX + sideLength / 2, 0) + L(startX + sideLength * 1.5, 0) + L(startX + sideLength * 2, d1) + L(startX + sideLength * 3, d1)
    else:
        path += L(startX + sideLength, d1) + L(startX + sideLength * 1.5, 0) + L(startX + sideLength * 2.5, 0) + L(startX + sideLength * 3, d1)

startX = upperStartX + (i + 1) * sideLength * 3

if not startHigh and not pair:
    path += L(w - sideLength / 2, d1)
elif startHigh and not pair:
    path += L(startX + sideLength / 2, 0) + L(startX + sideLength * 1.5, 0) + L(w, d1)
    
if (startHigh and pair) or (not startHigh and not pair):
    path+= L(w, 2 * d1) + L(w, h - d1)
    lowerStartX = 0
else:
    path += L(w, h - 2 * d1) + L(w - sideLength / 2, h - d1)
    # path+= L(startX + sideLength / 2, 0) + L(startX + sideLength * 1.5, 0) + L(startX + sideLength * 2, d1) + L(startX + sideLength * 2, h - 2 * d1) + L(startX + sideLength * 1.5, h - d1) + L(startX + sideLength / 2, h - d1)
    lowerStartX = sideLength / 2
for i in range (0, int((series - series % 2) / 2)):
    startX = w - lowerStartX - i * sideLength * 3
    if (startHigh and pair) or (not startHigh and not pair):
        path += L(startX - sideLength / 2, h) + L(startX - sideLength * 1.5, h) + L(startX - sideLength * 2, h - d1) + L(startX - sideLength * 3, h - d1)
    else:
        path += L(startX - sideLength, h - d1) + L(startX - sideLength * 1.5, h) + L(startX - sideLength * 2.5, h) + L(startX - sideLength * 3, h - d1)
    
startX = w - lowerStartX - (i + 1) * sideLength * 3

if not startHigh and not pair:
    path += L(startX - sideLength / 2, h) + L(startX - sideLength * 1.5, h) + L(0, h - d1)
elif startHigh and not pair:
    path += L(startX - sideLength, h - d1) + L(0, h - 2 * d1)

if startHigh:
    path+= L(0, h - 2 * d1) + L(0, d1)
else:
    path+= L(0, 2 * d1)


with open(f"battery insulator orig.svg", "r") as file:
    orig = file.read()
    orig = orig.replace("[w]", str(w))
    orig = orig.replace("[h]", str(h))
    orig = orig.replace("[class]", pathClass)
    orig1 = orig.replace("[path]", path)
    orig2 = orig.replace("[path]", M(sideLength / 2, 2 * d1) + L(w - sideLength / 2, 2 * d1) + L(w - sideLength/2, h - 2 * d1) + L(sideLength / 2, h - 2 * d1) + L(sideLength / 2, 2 * d1))

if startHigh:
    fileName = "battery insulator " + str(series) + " high.svg"
else:
    fileName = "battery insulator " + str(series) + " low.svg" 
with open(fileName, "w") as file:
        file.write(orig1)
with open("battery insulator weight.svg", "w") as file:
        file.write(orig2)

print (fileName)