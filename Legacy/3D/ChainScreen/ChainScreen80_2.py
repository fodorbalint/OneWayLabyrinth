import math

def r(a, b = 3):
    return f"{a:.3f}".rstrip("0").rstrip(".")

def Sqrt(x):
    return math.sqrt(x)

def Pow(x, y):
    return math.pow(x, y)

def Cos(angle):
    return math.cos(angle * math.pi / 180)

def Sin(angle):
    return math.sin(angle * math.pi / 180)

def Tan(angle):
    return math.tan(angle * math.pi / 180)

def Asin(ratio):
    return math.asin(ratio) * 180 / math.pi

def Atan(ratio):
    return math.atan(ratio) * 180 / math.pi

def M(x, y):
    return " M " + str(r(x)) + " " + str(r(y))

def L(endX, endY):
    return " L " + r(endX) + " " + r(endY)

def A(radius, endX, endY, CW, size = 0): # small or large arc
    if CW == True:
        direction = "1 "
    else:
        direction = "0 "
    return " A " + r(radius) + " " + r(radius) + " 0 " + str(size) + " " + direction + r(endX) + " " + r(endY)

def A1(radius, endX, endY, CW): # large arc
    if CW:
        direction = "1 "
    else:
        direction = "0 "
    return " A " + r(radius) + " " + r(radius) + " 0 1 " + direction + r(endX) + " " + r(endY)

def C(commonControlPointX, commonControlPointY, endX, endY):
    return " C " + r(commonControlPointX) + " " + r(commonControlPointY) + " " + r(commonControlPointX) + " " + r(commonControlPointY) + " " + r(endX) + " " + r(endY)

def C2(controlPoint1X, controlPoint1Y, controlPoint2X, controlPoint2Y, endX, endY):
    return " C " + r(controlPoint1X) + " " + r(controlPoint1Y) + " " + r(controlPoint2X) + " " + r(controlPoint2Y) + " " + r(endX) + " " + r(endY)

def HCW(cx, cy, d): # circle / hole drawn clockwise
    return M(cx, cy - d / 2) + A(d / 2, cx, cy + d / 2, True) + A(d / 2, cx, cy - d / 2, True)

def HCCW(cx, cy, d): # circle / hole drawn counter-clockwise
    return M(cx, cy - d / 2) + A(d / 2, cx, cy + d / 2, False) + A(d / 2, cx, cy - d / 2, False)

def HCCW_cut(cx, cy, d, a1, a2, d2): # circle drawn clockwise with cutout circle
    p1x = cx - Sin(a1) * d / 2
    p1y = cy - Cos(a1) * d / 2
    p2x = cx - Sin(a2) * d / 2
    p2y = cy - Cos(a2) * d / 2
    if a1 > 180:
        size1 = 1
    else:
        size1 = 0
    if a2 < 180:
        size2 = 1
    else:
        size2 = 0
    return M(cx, cy - d / 2) + A(d / 2, p1x, p1y, False, size1) + A(d2 / 2, p2x, p2y, True) + A(d / 2, cx, cy - d / 2, False, size2)    

def PolygonHoles(cx, cy, d1, d2, num, offsetAngle = 0):    
    ret = ""
    for i in range (0, num):
        angle = 360 * i / num + offsetAngle
        px = cx - Sin(angle) * d1 / 2
        py = cy - Cos(angle) * d1 / 2
        ret += HCCW(px, py, d2)
    return ret

def DrawOutlineCCW(points, radiuses, replaceSide = None, sidePath = None):
    result = ""
    result2 = ""
    newPoints = []

    for i in range(len(points)):
        currentX = points[i][0]
        currentY = points[i][1]
        currentR = radiuses[i]

        if replaceSide is not None and replaceSide[0] == i:
            newPoints.append([replaceSide[1], replaceSide[2]])
            newPoints.append([replaceSide[3], replaceSide[4]])
        else:
            nextI = i + 1 if i < len(points) - 1 else 0

            nextX = points[nextI][0]
            nextY = points[nextI][1]
            nextR = radiuses[nextI]

            d12x = nextX - currentX
            d12y = nextY - currentY
            d12 = Sqrt(d12x * d12x + d12y * d12y)

            # rotate direction 90 degrees CW to find start point
            angle = Asin(d12y / d12)
            if d12x < 0 and d12y > 0: angle = 180 - angle
            if d12x < 0 and d12y <= 0: angle = 180 - angle
            if d12x >= 0 and d12y < 0: angle = 360 + angle

            angle2 = Asin((nextR - currentR) / d12)

            s1x = currentX + currentR * Cos(angle + angle2 + 90)
            s1y = currentY + currentR * Sin(angle + angle2 + 90)
            s2x = nextX + nextR * Cos(angle + angle2 + 90)
            s2y = nextY + nextR * Sin(angle + angle2 + 90)
            newPoints.append([s1x, s1y])
            newPoints.append([s2x, s2y])

        result2 += HCCW(currentX, currentY, currentR * 2)

    result += M(newPoints[0][0], newPoints[0][1])
    for i in range(len(points)):
        nextI = i + 1 if i < len(points) - 1 else 0
        AI = 2 * (i + 1) if i < len(points) - 1 else 0

        if replaceSide is not None and replaceSide[0] == i:
            result += sidePath + A(radiuses[nextI], newPoints[AI][0], newPoints[AI][1], False)
        else:
            result += L(newPoints[2 * i + 1][0], newPoints[2 * i + 1][1]) + A(radiuses[nextI], newPoints[AI][0], newPoints[AI][1], False)

    return result
    return [result, result2]

def DrawOutlineCCWInOut(points, radiuses, mask):
    result = ""
    result2 = ""
    newPoints = []

    for i in range(len(points)):

        currentX = points[i][0]
        currentY = points[i][1]
        currentR = radiuses[i]
        currentM = mask[i]

        nextI = i + 1 if i < len(points) - 1 else 0

        nextX = points[nextI][0]
        nextY = points[nextI][1]
        nextR = radiuses[nextI]
        nextM = mask[nextI]

        d12x = nextX - currentX
        d12y = nextY - currentY
        d12y_ = d12y
        if d12y < 0: d12y_ = currentY - nextY
        d12 = Sqrt(d12x * d12x + d12y * d12y)
        angle1 = Asin(d12y_ / d12)

        if currentM == 0 and nextM == 0: # outside of both points
            # rotate direction 90 degrees CW to find start point
            angle2 = Asin((nextR - currentR) / d12)

            if d12x >= 0 and d12y <= 0:
                s1x = currentX + currentR * Cos(90 - angle1 + angle2)
                s1y = currentY + currentR * Sin(90 - angle1 + angle2)
                s2x = nextX + nextR * Cos(90 - angle1 + angle2)
                s2y = nextY + nextR * Sin(90 - angle1 + angle2)
            elif d12x >= 0 and d12y >= 0:
                s1x = currentX - currentR * Cos(90 - angle1 - angle2)
                s1y = currentY + currentR * Sin(90 - angle1 - angle2)
                s2x = nextX - nextR * Cos(90 - angle1 - angle2)
                s2y = nextY + nextR * Sin(90 - angle1 - angle2)
            elif d12x <= 0 and d12y <= 0:
                s1x = currentX + currentR * Cos(90 - angle1 - angle2)
                s1y = currentY - currentR * Sin(90 - angle1 - angle2)
                s2x = nextX + nextR * Cos(90 - angle1 - angle2)
                s2y = nextY - nextR * Sin(90 - angle1 - angle2)
            else:
                s1x = currentX - currentR * Cos(90 - angle1 + angle2)
                s1y = currentY - currentR * Sin(90 - angle1 + angle2)
                s2x = nextX - nextR * Cos(90 - angle1 + angle2)
                s2y = nextY - nextR * Sin(90 - angle1 + angle2)

        elif currentM == 0 and nextM == 1:
            angle2 = Asin((currentR + nextR) / d12)

            if d12x >= 0 and d12y <= 0: # point 2 is up right to point 1, line goes up right
                s1x = currentX + currentR * Cos(90 - angle1 - angle2)
                s1y = currentY + currentR * Sin(90 - angle1 - angle2)
                s2x = nextX - nextR * Cos(90 - angle1 - angle2)
                s2y = nextY - nextR * Sin(90 - angle1 - angle2)
            elif d12x <= 0 and d12y <= 0: # point 2 is up left to point 1, line goes down left
                s1x = currentX - currentR * Cos(90 + angle1 - angle2)
                s1y = currentY - currentR * Sin(90 + angle1 - angle2)
                s2x = nextX + nextR * Cos(90 + angle1 - angle2)
                s2y = nextY + nextR * Sin(90 + angle1 - angle2)
            elif d12x <= 0 and d12y >= 0: # point 2 is down left to point 1, line goes down left
                s1x = currentX - currentR * Cos(90 - angle1 - angle2)
                s1y = currentY - currentR * Sin(90 - angle1 - angle2)
                s2x = nextX + nextR * Cos(90 - angle1 - angle2)
                s2y = nextY + nextR * Sin(90 - angle1 - angle2)
            elif d12x >= 0 and d12y >= 0: # point 2 is down right to point 1, line goes up right
                s1x = currentX + currentR * Cos(90 + angle1 - angle2)
                s1y = currentY + currentR * Sin(90 + angle1 - angle2)
                s2x = nextX - nextR * Cos(90 + angle1 - angle2)
                s2y = nextY - nextR * Sin(90 + angle1 - angle2)

        elif currentM == 1 and nextM == 1:
            angle2 = Asin((nextR - currentR) / d12)

            if d12x >= 0:
                s1x = currentX - currentR * Sin(angle1 + angle2)
                s1y = currentY - currentR * Cos(angle1 + angle2)
                s2x = nextX - nextR * Sin(angle1 + angle2)
                s2y = nextY - nextR * Cos(angle1 + angle2)
            else:
                s1x = currentX + currentR * Sin(angle1 + angle2)
                s1y = currentY + currentR * Cos(angle1 + angle2)
                s2x = nextX + nextR * Sin(angle1 + angle2)
                s2y = nextY + nextR * Cos(angle1 + angle2)

        else:
            angle2 = Asin((currentR + nextR) / d12)

            if d12x >= 0 and d12y >= 0: # point 2 is down right to point 1, line goes down right
                s1x = currentX + currentR * Cos(90 - angle1 - angle2)
                s1y = currentY - currentR * Sin(90 - angle1 - angle2)
                s2x = nextX - nextR * Cos(90 - angle1 - angle2)
                s2y = nextY + nextR * Sin(90 - angle1 - angle2)
            elif d12x >= 0 and d12y <= 0: # point 2 is up right to point 1, line goes down right
                s1x = currentX + currentR * Cos(90 + angle1 - angle2)
                s1y = currentY - currentR * Sin(90 + angle1 - angle2)
                s2x = nextX - nextR * Cos(90 + angle1 - angle2)
                s2y = nextY + nextR * Sin(90 + angle1 - angle2)
            elif d12x <= 0 and d12y <= 0: # point 2 is up left to point 1, line goes up left
                s1x = currentX - currentR * Cos(90 - angle1 - angle2)
                s1y = currentY + currentR * Sin(90 - angle1 - angle2)
                s2x = nextX + nextR * Cos(90 - angle1 - angle2)
                s2y = nextY - nextR * Sin(90 - angle1 - angle2)
            else: # point 2 is down left to point 1, line goes up left
                s1x = currentX - currentR * Cos(90 + angle1 - angle2)
                s1y = currentY + currentR * Sin(90 + angle1 - angle2)
                s2x = nextX + nextR * Cos(90 + angle1 - angle2)
                s2y = nextY - nextR * Sin(90 + angle1 - angle2)

        newPoints.append([s1x, s1y])
        newPoints.append([s2x, s2y])

        result2 += HCCW(currentX, currentY, currentR * 2)

    result += M(newPoints[0][0], newPoints[0][1])
    for i in range(len(points)):
        nextI = i + 1 if i < len(points) - 1 else 0
        AI = 2 * (i + 1) if i < len(points) - 1 else 0
        direction = False if mask[nextI] == 0 else True

        result += L(newPoints[2 * i + 1][0], newPoints[2 * i + 1][1]) + A(radiuses[nextI], newPoints[AI][0], newPoints[AI][1], direction)
    
    return result
    return [result, result2]

def DrawOutlineCCWInOut2(points, radiuses, mask):
    result = ""
    result2 = ""
    newPoints = []

    for i in range(len(points)):

        currentX = points[i][0]
        currentY = points[i][1]
        currentR = radiuses[i]
        currentM = mask[i]

        nextI = i + 1 if i < len(points) - 1 else 0

        nextX = points[nextI][0]
        nextY = points[nextI][1]
        nextR = radiuses[nextI]
        nextM = mask[nextI]

        d12x = nextX - currentX
        d12y = nextY - currentY
        d12y_ = d12y
        if d12y < 0: d12y_ = currentY - nextY
        d12 = Sqrt(d12x * d12x + d12y * d12y)
        angle1 = Asin(d12y_ / d12)

        if currentM == 0 and nextM == 0: # outside of both points
            # rotate direction 90 degrees CW to find start point
            angle2 = Asin((nextR - currentR) / d12)

            if d12x >= 0 and d12y <= 0:
                s1x = currentX + currentR * Cos(90 - angle1 + angle2)
                s1y = currentY + currentR * Sin(90 - angle1 + angle2)
                s2x = nextX + nextR * Cos(90 - angle1 + angle2)
                s2y = nextY + nextR * Sin(90 - angle1 + angle2)
            elif d12x >= 0 and d12y >= 0:
                s1x = currentX - currentR * Cos(90 - angle1 - angle2)
                s1y = currentY + currentR * Sin(90 - angle1 - angle2)
                s2x = nextX - nextR * Cos(90 - angle1 - angle2)
                s2y = nextY + nextR * Sin(90 - angle1 - angle2)
            elif d12x <= 0 and d12y <= 0:
                s1x = currentX + currentR * Cos(90 - angle1 - angle2)
                s1y = currentY - currentR * Sin(90 - angle1 - angle2)
                s2x = nextX + nextR * Cos(90 - angle1 - angle2)
                s2y = nextY - nextR * Sin(90 - angle1 - angle2)
            else:
                s1x = currentX - currentR * Cos(90 - angle1 + angle2)
                s1y = currentY - currentR * Sin(90 - angle1 + angle2)
                s2x = nextX - nextR * Cos(90 - angle1 + angle2)
                s2y = nextY - nextR * Sin(90 - angle1 + angle2)

        elif currentM == 0 and nextM == 1:
            angle2 = Asin((currentR + nextR) / d12)

            if d12x >= 0 and d12y <= 0: # point 2 is up right to point 1, line goes up right
                s1x = currentX + currentR * Cos(90 - angle1 - angle2)
                s1y = currentY + currentR * Sin(90 - angle1 - angle2)
                s2x = nextX - nextR * Cos(90 - angle1 - angle2)
                s2y = nextY - nextR * Sin(90 - angle1 - angle2)
            elif d12x <= 0 and d12y <= 0: # point 2 is up left to point 1, line goes down left
                s1x = currentX - currentR * Cos(90 + angle1 - angle2)
                s1y = currentY - currentR * Sin(90 + angle1 - angle2)
                s2x = nextX + nextR * Cos(90 + angle1 - angle2)
                s2y = nextY + nextR * Sin(90 + angle1 - angle2)
            elif d12x <= 0 and d12y >= 0: # point 2 is down left to point 1, line goes down left
                s1x = currentX - currentR * Cos(90 - angle1 - angle2)
                s1y = currentY - currentR * Sin(90 - angle1 - angle2)
                s2x = nextX + nextR * Cos(90 - angle1 - angle2)
                s2y = nextY + nextR * Sin(90 - angle1 - angle2)
            elif d12x >= 0 and d12y >= 0: # point 2 is down right to point 1, line goes up right
                s1x = currentX + currentR * Cos(90 + angle1 - angle2)
                s1y = currentY + currentR * Sin(90 + angle1 - angle2)
                s2x = nextX - nextR * Cos(90 + angle1 - angle2)
                s2y = nextY - nextR * Sin(90 + angle1 - angle2)

        elif currentM == 1 and nextM == 1:
            angle2 = Asin((nextR - currentR) / d12)

            if d12x >= 0:
                s1x = currentX - currentR * Sin(angle1 + angle2)
                s1y = currentY - currentR * Cos(angle1 + angle2)
                s2x = nextX - nextR * Sin(angle1 + angle2)
                s2y = nextY - nextR * Cos(angle1 + angle2)
            else:
                s1x = currentX + currentR * Sin(angle1 + angle2)
                s1y = currentY + currentR * Cos(angle1 + angle2)
                s2x = nextX + nextR * Sin(angle1 + angle2)
                s2y = nextY + nextR * Cos(angle1 + angle2)

        else:
            angle2 = Asin((currentR + nextR) / d12)

            if d12x >= 0 and d12y >= 0: # point 2 is down right to point 1, line goes down right
                s1x = currentX + currentR * Cos(90 - angle1 - angle2)
                s1y = currentY - currentR * Sin(90 - angle1 - angle2)
                s2x = nextX - nextR * Cos(90 - angle1 - angle2)
                s2y = nextY + nextR * Sin(90 - angle1 - angle2)
            elif d12x >= 0 and d12y <= 0: # point 2 is up right to point 1, line goes down right
                s1x = currentX + currentR * Cos(90 + angle1 - angle2)
                s1y = currentY - currentR * Sin(90 + angle1 - angle2)
                s2x = nextX - nextR * Cos(90 + angle1 - angle2)
                s2y = nextY + nextR * Sin(90 + angle1 - angle2)
            elif d12x <= 0 and d12y <= 0: # point 2 is up left to point 1, line goes up left
                s1x = currentX - currentR * Cos(90 - angle1 - angle2)
                s1y = currentY + currentR * Sin(90 - angle1 - angle2)
                s2x = nextX + nextR * Cos(90 - angle1 - angle2)
                s2y = nextY - nextR * Sin(90 - angle1 - angle2)
            else: # point 2 is down left to point 1, line goes up left
                s1x = currentX - currentR * Cos(90 + angle1 - angle2)
                s1y = currentY + currentR * Sin(90 + angle1 - angle2)
                s2x = nextX + nextR * Cos(90 + angle1 - angle2)
                s2y = nextY - nextR * Sin(90 + angle1 - angle2)

        newPoints.append([s1x, s1y])
        newPoints.append([s2x, s2y])

        result2 += HCCW(currentX, currentY, currentR * 2)

    result += M(newPoints[0][0], newPoints[0][1])
    for i in range(len(points)):
        nextI = i + 1 if i < len(points) - 1 else 0
        AI = 2 * (i + 1) if i < len(points) - 1 else 0
        direction = False if mask[nextI] == 0 else True

        result += L(newPoints[2 * i + 1][0], newPoints[2 * i + 1][1]) + A(radiuses[nextI], newPoints[AI][0], newPoints[AI][1], direction)
    
    return [result, result2]

def SlotCCW(c1x, c1y, c2x, c2y, d):
    points = [[c1x, c1y], [c2x, c2y]]
    radiuses = [d / 2, d / 2]

    result = DrawOutlineCCW(points, radiuses)
    return result

def CalculateSegment(sx, sy, cx, cy, ex, ey, t, reverse):
    global newPointX, newPointY

    q0x = (1 - t) * sx + t * cx
    q0y = (1 - t) * sy + t * cy
    q2x = (1 - t) * cx + t * ex
    q2y = (1 - t) * cy + t * ey

    '''file = file.Replace("[q0x]", r(q0x).ToString())
    file = file.Replace("[q0y]", r(q0y).ToString())
    file = file.Replace("[q2x]", r(q2x).ToString())
    file = file.Replace("[q2y]", r(q2y).ToString())'''

    r0x = (1 - t) * q0x + t * cx
    r0y = (1 - t) * q0y + t * cy
    r1x = (1 - t) * cx + t * q2x
    r1y = (1 - t) * cy + t * q2y

    '''file = file.Replace("[r0x]", r(r0x).ToString())
    file = file.Replace("[r0y]", r(r0y).ToString())
    file = file.Replace("[r1x]", r(r1x).ToString())
    file = file.Replace("[r1y]", r(r1y).ToString())'''

    s0x = (1 - t) * r0x + t * r1x
    s0y = (1 - t) * r0y + t * r1y

    '''file = file.Replace("[s0x]", r(s0x).ToString())
    file = file.Replace("[s0y]", r(s0y).ToString())'''

    n0x = (1 - t) * cx + t * q2x
    n0y = (1 - t) * cy + t * q2y
    n1x = (1 - t) * cx + t * ex
    n1y = (1 - t) * cy + t * ey

    '''file = file.Replace("[n0x]", r(n0x).ToString())
    file = file.Replace("[n0y]", r(n0y).ToString())
    file = file.Replace("[n1x]", r(n1x).ToString())
    file = file.Replace("[n1y]", r(n1y).ToString())'''

    newPointX = s0x
    newPointY = s0y

    if not reverse:
        return " C " + r(n0x) + " " + r(n0y) + " " + r(n1x) + " " + r(n1y) + " " + r(ex) + " " + r(ey)
    else:
        return " C " + r(n1x) + " " + r(n1y) + " " + r(n0x) + " " + r(n0y) + " " + r(s0x) + " " + r(s0y)

def MovePointWithAngle(x, y, xDiff, yDiff, angle):
    x += xDiff * Cos(angle) + yDiff * Sin(angle)
    y += -xDiff * Sin(angle) + yDiff * Cos(angle)
    return [x, y]

# -----------------------------------------------


laserCut = True
background = True

print(f"Laser cut: {laserCut}\n")

adjust = 0
pathClass = "l2" # 0.08 black unfilled
pathClass2 = "l2"
if laserCut:
    adjust = 0.2
    pathClass = "l2"
    pathClass2 = "l1" # 0.02 black unfilled

bgString = '' # Inkscape's DXF R14 exporter cannot handle this commented section.
if background:
    bgString = '<rect width="100%" height="100%" fill="#dddddd" />'


wheelMountInside = 51.6 - adjust
screwHole = 6 - adjust
spacerSize = 18
washerSize = 12
wheelMountOutside = wheelMountInside + adjust + 2 * spacerSize + adjust
sprocketMountInside = 34 - adjust
sprocketMountOutside = 108 + adjust
wheelCircleDiameter = (wheelMountInside + wheelMountOutside) / 2
# adjustment
wheelMountInside += 0.2
sprocketCircleDiameter = sprocketMountOutside - adjust - washerSize
valveHoleSize = 40
w = 376
h = 188
c1x = wheelMountOutside / 2
c1y = wheelMountOutside / 2
# c1x = sprocketMountOutside / 2
# c1y = sprocketMountOutside / 2
c2x = sprocketMountOutside / 2
c2y = sprocketMountOutside / 2

print (f"wheelMountInside {r(wheelMountInside)}\nwheelMountOutside {wheelMountOutside}\nsprocketMountInside {sprocketMountInside}\nsprocketMountOutside {sprocketMountOutside}\nwheelCircleDiameter {wheelCircleDiameter}\nsprocketCircleDiameter {sprocketCircleDiameter}\n")

# wheel mount

path1 = HCCW(c1x, c1y, wheelMountOutside) + HCCW(c1x, c1x, wheelMountInside) + PolygonHoles(c1x, c1y, wheelCircleDiameter, screwHole, 5, -9)

# sprocket mount

path2 = HCCW(c2x, c2y, sprocketMountInside) + HCCW_cut(c2x, c2y, sprocketMountOutside, 295, 335, valveHoleSize) + PolygonHoles(c2x, c2y, wheelCircleDiameter, screwHole, 5, -9) + PolygonHoles(c2x, c2y, sprocketCircleDiameter, screwHole, 4)

# sprocket guides

path3 = HCCW(c2x, c2y, sprocketMountInside) + HCCW_cut(c2x, c2y, sprocketMountOutside, 295, 335, valveHoleSize) + PolygonHoles(c2x, c2y, wheelCircleDiameter, screwHole, 5, -9) + PolygonHoles(c2x, c2y, wheelCircleDiameter, spacerSize, 5, -9) + PolygonHoles(c2x, c2y, sprocketCircleDiameter, screwHole, 4) + PolygonHoles(c2x, c2y, sprocketCircleDiameter, washerSize, 4)

# rear fork extension

bigScrewHole = 10 - adjust
screwHole5mm = 5 - adjust
screwHole8mm = 8 - adjust
slotLength = 3
sx = 15
sy = 15
points = [[sx, sy], [sx + 49 - slotLength - 12, sy + 24], [sx + 49, sy + 15], [sx + 20, sy]]
radiuses = [15, 6, 15, 15]

path4 = HCCW(points[0][0], points[0][1], bigScrewHole) + HCCW(points[3][0], points[3][1], bigScrewHole) + HCCW(points[1][0], points[1][1], screwHole) + SlotCCW(points[2][0] - slotLength, points[2][1], points[2][0], points[2][1], screwHole) + DrawOutlineCCW(points, radiuses)

# rear fork extension brake

# original brake mount dimensions. First hole is 30 mm from center horizontally. Second hole x-coordinate is 30 + x, -y relative to center.
# First and second hole distance is 51 mm.
# Center and second hole distance is 70 mm
slotLength = 3
side1 = 30
side2 = 51
side3 = 70 
sx = 15
sy = 62
x = (side3 * side3 - side1 * side1 - side2 * side2) / (2 * side1)
y = Sqrt(side2 * side2 - x * x)


rotate = 52
sideAngle = Asin(y/51) + rotate - 90 # angle tilting left at the upper end from vertical

print(f"Brake mount x: {x:.3f}, relative angle {Asin(y/51):.3f}, absolute side angle {sideAngle:.3f}") # 23.3, 62.8, 11.8

p1x = sx + 49 + side1 * Cos(rotate)
p1y = sy + 15 - side1 * Sin(rotate)
p2x = sx + 49 + (side1 + x) * Cos(rotate) - y * Sin(rotate)
p2y = sy + 15 - (side1 + x) * Sin(rotate) - y * Cos(rotate)

print(f"Brake mounting holes {p1x:.3f} x {p1y:.3f}, {p2x:.3f} x {p2y:.3f}")
print(f"Upper brake hole to the right of mounting hole by {p2x - (sx + 24)}, up by {sy - 40 - p2y}, slope 4 mm to right by 40 mm up, calculated distance: {p2x - (sx + 24) - (sy - 40 - p2y) / 10}") # 20.6 mm at 52 degree angle

holes  = [[sx, sy], [sx + 20, sy], [sx + 49, sy + 15], [p1x, p1y], [p2x, p2y], [sx + 25, sy - 40]]
points = [[sx, sy], [sx + 49 - slotLength, sy + 15], [sx + 49, sy + 15], [p1x, p1y], [p2x, p2y], [p2x - slotLength, p2y], [sx + 25, sy - 40]]
radiuses = [15, 15, 15, 7, 7, 7, 10]

# cutout side

# define ellipse center and rotation angle
# center is positioned right and up as if side1 was horizontal.
ecx = p1x + (x + 0) * Cos(rotate) - 10 * Sin(rotate)
ecy = p1y - (x + 0) * Sin(rotate) - 10 * Cos(rotate)
bigEllR = 20
smallEllR = 15
angleEllipse = sideAngle + 45

# ends of cutout side
p1x = p1x + radiuses[3] * Cos(sideAngle)
p1y = p1y - radiuses[3] * Sin(sideAngle) 
p2x = p2x + radiuses[3] * Cos(sideAngle)
p2y = p2y - radiuses[3] * Sin(sideAngle)

guidePoints = HCCW(ecx, ecy, 1) + HCCW(p1x, p1y, 1) + HCCW(p2x, p2y, 1)

# Find intersection points:
# 1) Rotate line ends around ellipse center
d1c = Sqrt(Pow(p1y - ecy, 2) + Pow(p1x - ecx, 2))
d2c = Sqrt(Pow(p2y - ecy, 2) + Pow(p2x - ecx, 2))

# Angle of upper point with the vertical line at the ellipse center, point is on the left

distAngle = Asin((ecx - p2x)/d2c) - sideAngle
dist = Sin(distAngle)*d2c

print(f"Distance of ellipse center to side2: {dist:.3f}, upper triangle angle: {distAngle:.3f}")

a1 = Asin((ecx - p1x) / d1c) # angle at ellipse center with vertical down line, p1 is to left
a2 = Asin((ecy - p2y) / d2c) # angle at ellipse center with horizontal left line, p2 is above
print(f"Point angles at ellipse center: {a1:.3f} {a2:.3f}")
# a1 0.3, a2 48.9

# Rotate lines by the ellipse angle, so we get them as if the ellipse was upright
np1x = ecx - Sin(a1 + angleEllipse) * d1c # angle on left from vertical down line
np1y = ecy + Cos(a1 + angleEllipse) * d1c
np2x = ecx + Sin(a2 + angleEllipse - 90) * d2c  # angle on right from vertical up line
np2y = ecy - Cos(a2 + angleEllipse - 90) * d2c

guidePoints = HCCW(ecx, ecy, 1) + HCCW(p1x, p1y, 1) + HCCW(p2x, p2y, 1) + HCCW(np1x, np1y, 1) + HCCW(np2x, np2y, 1)

# 2) Offset, so the ellipse center is the origo
np1x -= ecx
np1y -= ecy
np2x -= ecx
np2y -= ecy
a = (np1y - np2y) / (np1x - np2x)
c = np1y - a * np1x

# 3) Calculate intersection points, ellipse is tall and narrow.
'''
Line: y = ax + c
Ellipse: x^2 / smallR^2 + y^2 / bigR^2 = 1
 
(a^2x^2 + 2acx + c^2)bigR^2 + smallR^2x^2 = smallR^2bigR^2
a^2bigR^2x^2 + 2acbigR^2x + c^2bigR^2 + smallR^2x^2 = smallR^2bigR^2
(a^2bigR^2 + smallR^2)x^2 + (2acbigR^2)x + c^2bigR^2 - smallR^2bigR^2 = 0
'''

a_ = a * a * smallEllR * smallEllR + bigEllR * bigEllR
b_ = 2 * a * c * smallEllR * smallEllR
c_ = c * c * smallEllR * smallEllR - smallEllR * smallEllR * bigEllR * bigEllR

p3x = (-b_ - Sqrt(b_ * b_ - 4 * a_ * c_)) / (2 * a_)
p3y = a * p3x + c
p4x = (-b_ + Sqrt(b_ * b_ - 4 * a_ * c_)) / (2 * a_)
p4y = a * p4x + c

# 4) Offset and rotate found points back. p3 is upper point, p4 is lower 

p3x += ecx
p3y += ecy
p4x += ecx
p4y += ecy

guidePoints += HCCW(p3x, p3y, 1) + HCCW(p4x, p4y, 1)

d1c = Sqrt(Pow(p3y - ecy, 2) + Pow(p3x - ecx, 2))
d2c = Sqrt(Pow(p4y - ecy, 2) + Pow(p4x - ecx, 2))
a1 = Asin((p3y - ecy) / d1c) # the lower point is now below the horizontal axis
a2 = Asin((p4x - ecx) / d2c) # the upper point is still on right side of the vertical axis
print(f"Intersection point angles at ellipse center: {a1:.3f} {a2:.3f}")
# a1 7.5, a2 16.8

p3x = ecx - Sin(90 - a1 - angleEllipse) * d1c # angle left down with to vertical line
p3y = ecy + Cos(90 - a1 - angleEllipse) * d1c
p4x = ecx - Cos(90 + a2 - angleEllipse) * d2c # angle left above horizontal line
p4y = ecy - Sin(90 + a2 - angleEllipse) * d2c

guidePoints += HCCW(p3x, p3y, 1) + HCCW(p4x, p4y, 1)
guidePoints = ""

replaceSide = 3
sidePath = L(p3x, p3y) + " A " + str(smallEllR) + " " + str(bigEllR) + f" {r(-angleEllipse)} 0 1 " + r(p4x) + " " + r(p4y) + L(p2x, p2y)
 
path5 = guidePoints + HCCW(holes[0][0], holes[0][1], bigScrewHole) + HCCW(holes[1][0], holes[1][1], bigScrewHole) + SlotCCW(holes[2][0] - slotLength, holes[2][1], holes[2][0], holes[2][1], screwHole) + SlotCCW(holes[3][0] - slotLength, holes[3][1], holes[3][0], holes[3][1], screwHole) + SlotCCW(holes[4][0] - slotLength, holes[4][1], holes[4][0], holes[4][1], screwHole) + HCCW(holes[5][0], holes[5][1], screwHole5mm) + DrawOutlineCCW(points, radiuses, [replaceSide, p1x, p1y, p2x, p2y], sidePath)

# front fork extension

sx = 16.5
sy = 16.5
path5_1 = HCCW(sx, sy, screwHole5mm) + HCCW(sx, sy + 18, bigScrewHole) + HCCW(sx, sy + 34, screwHole) + SlotCCW(sx, sy, sx, sy + 34, 33) 

# chain screen

smallC1 = 88.5 # 80T rear sprocket is 165 mm in diameter. 6 mm clearance
smallC2 = 19
axleDist = 260.5 # measured distance, with 1.5 mm stretch out of 3
drop = 5
angle = 3.01
thickness = 4
bigC1 = smallC1 + thickness
bigC2 = smallC2 + thickness
miniC1 = 65
miniC2 = 13
c1x = bigC1
c1y = bigC1
upperHoleDist = 26
lowerHoleDist = 26
hole1Dist = 32.5 # upper edge
hole2Dist = 88 # upper edge
hole3Dist = 91.5 # lower edge (the L-bracket tilts down left compared to the vertical line of the chain screen)
diff = 0.15
supportThickness = 3
supportC1 = bigC1 + supportThickness
supportElevation = lowerHoleDist - 20
supportHoleTopDistance = 10 # center point of the hole to the top line of the support
supportHoleElevation = supportElevation + supportHoleTopDistance
supportHoleMove = 46
supportHole1 = 61 + supportHoleMove
supportHole2 = 86 + supportHoleMove
supportHole3 = 111 + supportHoleMove
# match support bottom curve to its end
t1 = 0.6496
t2 = t1 + 0.0002
supportVerticalCorrection = 0

print(f"\nChain screen drop angle: {r(angle)} Support curve ratio: {r(t1, 5)} {r(t2, 5)}")

# angle of straight line connecting the two circles
a1 = Asin((smallC1 - smallC2) / axleDist)
p1x = c1x + Sin(a1 - angle) * bigC1
p1y = c1y + Cos(a1 - angle) * bigC1
p2x = c1x + axleDist + Sin(a1 + angle) * bigC2
p2y = c1y + Cos(a1 + angle) * bigC2
# control point perpendicular to the middle of the p1p2 line. Distance is tan(angle)*half(p1p2dist), its x and y offset has the same ratio as x and y components of the p1p2 distance. 
p3x = (p1x + p2x) / 2 + Tan(angle) * (p1y - p2y) / 2
p3y = (p1y + p2y) / 2 + Tan(angle) * (p2x - p1x) / 2
p4x = c1x + axleDist + Sin(a1) * bigC2
p4y = c1y - Cos(a1) * bigC2
p5x = c1x + Sin(a1) * bigC1
p5y = c1y - Cos(a1) * bigC1

path6 = M(c1x, c1y + bigC1) + A(bigC1, p1x, p1y, False) + C(p3x, p3y, p2x, p2y) + A(bigC2, p4x, p4y, False) + L(p5x, p5y) + A(bigC1, c1x, c1y - bigC1, False)

r0 = (smallC1 - miniC1 - bigC1 + smallC1) / (Sqrt(3) * 2 + 2)

origR = r0
bigR = r0
smallR = r0

# big base connecting section from up to down
path7 = "v " + str(bigC1 - smallC1) + \
    " a " + r(smallR) + " " + r(smallR) + " 0 1 0 " + r(origR / 2) + " " + r((1 + Sqrt(3) / 2) * origR) + \
    " a " + r(bigR) + " " + r(bigR) + " 0 1 1 0 " + r(origR * Sqrt(3)) + \
    " A " + r(smallR) + " " + r(smallR) + " 0 1 0 " + r(c1x) + " " + r(c1y - miniC1 - thickness) + \
    " L " + r(c1x) + " " + r(c1y - miniC1) + \
    " a " + r(miniC1) + " " + r(miniC1) + " 0 0 1 0 " + r(2 * miniC1) + \
    " L " + r(c1x) + " " + r(c1y + miniC1 + bigC1 - smallC1) + \
    " a " + r(smallR) + " " + r(smallR) + " 0 1 0 " + r(origR / 2) + " " + r((1 + Sqrt(3) / 2) * origR) + \
    " a " + r(bigR) + " " + r(bigR) + " 0 1 1 0 " + r(origR * Sqrt(3)) + \
    " A " + r(smallR) + " " + r(smallR) + " 0 1 0 " + r(c1x) + " " + r(c1y + smallC1) + "z"

# mounting holes, 5 mm diamater

p4x = c1x + (bigC1 - upperHoleDist) / Sin(a1)
p_1x = c1x + axleDist - miniC2 - hole1Dist
p_1y = c1y - (p4x - p_1x) * Tan(a1)
p_2x = c1x + axleDist - miniC2 - hole2Dist
p_2y = c1y - (p4x - p_2x) * Tan(a1)

p5x = c1x + (bigC1 - lowerHoleDist) / Sin(a1)
p_3x = c1x + axleDist - miniC2 - hole3Dist
p_3y = c1y + (p5x - p_3x) * Tan(a1)

print (f"3 holes: {p_1x:.3f} x {p_1y:.3f}, {p_2x:.3f} x {p_2y:.3f}, {p_3x:.3f} x {p_3y:.3f}")

# mounting holes incl. protector, calculated from FreeCAD.

p_4x = 204.19
p_4y = 130.58

p0_x = c1x + (bigC1 - lowerHoleDist) / Sin(a1)
p_x = c1x + axleDist - miniC2 - hole3Dist
p_y = c1y + (p0_x - p_x) * Tan(a1)
p_5x = MovePointWithAngle(p_x, p_y, -supportHoleMove, 0, a1)[0]
p_5y = MovePointWithAngle(p_x, p_y, -supportHoleMove, 0, a1)[1]
path9 = HCW(p_1x, p_1y, 5) + HCW(p_2x, p_2y, 5) + HCW(p_3x, p_3y, 5) + HCW(p_4x, p_4y, 5) + HCW(p_5x, p_5y, 5)+ HCW(c1x + axleDist - 26.25, c1y, 5)

# 3 holes strengthening piece

points = [[p_1x, p_1y], [p_2x, p_2y], [p_3x, p_3y]]
radiuses = [8, 8, 8]
path14 = HCW(p_1x, p_1y, screwHole5mm) + HCW(p_2x, p_2y, screwHole5mm) + HCW(p_3x, p_3y, screwHole5mm) + DrawOutlineCCW(points, radiuses)

tr5x = 0
tr5y = 0
Holes3W = w
Holes3H = h
if laserCut:
    tr5x = -(p_3x - 8)
    tr5y = -(p_2y - 8)
    Holes3W = r(p_1x - p_3x + 16)
    Holes3H = r(p_3y - p_2y + 16)

# will be used at L-plate
d23 = Sqrt(Pow(p_2x - p_3x, 2) + Pow(p_2y - p_3y, 2)) 

# mounting hole aligner

holeLines = M(c1x, c1y) + L(p4x, c1y) + L(c1x + (bigC1 - upperHoleDist) * Sin(a1), c1y - (bigC1 - upperHoleDist) * Cos(a1)) + L(c1x, c1y) + L(c1x + (bigC1 - lowerHoleDist) * Sin(a1), c1y + (bigC1 - lowerHoleDist) * Cos(a1)) + L(p5x, c1y) + " z"

print(f"Chain screen angle: {a1}, old {Asin((74 - 19) / 217)}")

# chain slack aligner

aligner = M(c1x + Sin(a1) * smallC1, c1y + Cos(a1) * smallC1) + L(c1x + axleDist + Sin(a1) * smallC2, c1y + Cos(a1) * smallC2) + \
    M(c1x + Sin(a1) * bigC1, c1y + Cos(a1) * bigC1) + L(c1x + axleDist + Sin(a1) * bigC2, c1y + Cos(a1) * bigC2) + \
    M(c1x + Sin(a1) * (smallC1 + drop), c1y + Cos(a1) * (smallC1 + drop)) + L(c1x + axleDist + Sin(a1) * (smallC2 + drop), c1y + Cos(a1) * (smallC2 + drop))

#  border inner

p_1x = c1x + Sin(a1 - angle) * smallC1
p_1y = c1y + Cos(a1 - angle) * smallC1
p_2x = c1x + axleDist + Sin(a1 + angle) * smallC2
p_2y = c1y + Cos(a1 + angle) * smallC2
p_3x = (p_1x + p_2x) / 2 + Tan(angle) * (p_1y - p_2y) / 2
p_3y = (p_1y + p_2y) / 2 + Tan(angle) * (p_2x - p_1x) / 2
p4x = c1x + axleDist + Sin(a1) * smallC2
p4y = c1y - Cos(a1) * smallC2
p5x = c1x + Sin(a1) * smallC1
p5y = c1y - Cos(a1) * smallC1
path10 = L(c1x, c1y - smallC1) + A(smallC1, p5x, p5y, True) + L(p4x, p4y) + A(smallC2, p_2x, p_2y, True) + C(p_3x, p_3y, p_1x, p_1y) + A(smallC1, c1x, c1y + smallC1, True) + " z"

# small base and border outer clockwise

# path11 = M(c1x, c1y + bigC1) + A(bigC1, c1x, c1y - bigC1, True)
# with mounting hole, calculated from FreeCAD
path11 = M(c1x, c1y + bigC1) + A(bigC1, 0.65, c1y + 10.91, True) + A(4.32 / 2, -1.5, c1y + 9, False) + A(9, -1.5, c1y - 9, True) + A(4.32 / 2, 0.65, c1y - 10.91, False) + A(bigC1, c1x, c1y - bigC1, True)

# small base connection section from up to down

bigR = r0 + diff
smallR = r0 - diff

path12 = "h " + r(diff) +  " v " + r(bigC1 - smallC1 + diff) + " h -" + r(diff) + \
    " a " + r(smallR) + " " + r(smallR) + " 0 1 0 " + r(smallR / 2) + " " + r((1 + Sqrt(3) / 2) * smallR) + \
    " a " + r(bigR) + " " + r(bigR) + " 0 1 1 0 " + r(bigR * Sqrt(3)) + \
    " A " + r(smallR) + " " + r(smallR) + " 0 1 0 " + r(c1x) + " " + r(c1y - miniC1 - thickness - diff) + \
    " h " + r(diff) + " v " + r(bigC1 - smallC1 + diff) + " h -" + r(diff) + \
    " a " + r(miniC1) + " " + r(miniC1) + " 0 0 0 0 " + r(2 * miniC1) + \
    " h " + r(diff) + " v " + r(bigC1 - smallC1 + diff) + " h -" + r(diff) + \
    " a " + r(smallR) + " " + r(smallR) + " 0 1 0 " + r(smallR / 2) + " " + r((1 + Sqrt(3) / 2) * smallR) + \
    " a " + r(bigR) + " " + r(bigR) + " 0 1 1 0 " + r(bigR * Sqrt(3)) + \
    " A " + r(smallR) + " " + r(smallR) + " 0 1 0 " + r(c1x) + " " + r(c1y + smallC1 - diff) + \
    " h " + r(diff) + " v " + r(bigC1 - smallC1 + diff) + " h -" + r(diff)


tr1x = 0
tr2x = 0
plateBigW = w
plateBigH = h
plateSmallW = w
plateSmallH = h
if laserCut:
    tr1x = -(c1x - origR) 
    tr2x = 10.5
    plateBigW = r(w + tr1x)
    plateBigH = 2 * bigC1
    plateSmallW = r(c1x + 2 * origR + tr2x + diff)
    plateSmallH = 2 * bigC1

path8 = HCW(c1x + axleDist, c1y, 2 * miniC2)

# small border inner counter-clockwise

path13 = "h " + r(diff) + " v " + r(bigC1 - smallC1) + " h -" + r(diff) + A(smallC1, c1x, c1y + smallC1, False) + " h " + r(diff) + " v " + r(bigC1 - smallC1) + " z"

# Support

s1x = c1x + Sin(a1 - angle) * supportC1
s1y = c1y + Cos(a1 - angle) * supportC1
s2x = c1x + axleDist + Sin(a1 + angle) * (bigC2 + supportThickness)
s2y = c1y + Cos(a1 + angle) * (bigC2 + supportThickness)
s3x = (s1x + s2x) / 2 + Tan(angle) * (s1y - s2y) / 2
s3y = (s1y + s2y) / 2 + Tan(angle) * (s2x - s1x) / 2

newPointX = 0
newPointY = 0

segment1 = CalculateSegment(s2x, s2y, s3x, s3y, s1x, s1y, 1 - t1, False)

newPointX0 = newPointX
newPointY0 = newPointY

print (f"newpointx {newPointX, newPointY}")

segment2 = CalculateSegment(p2x, p2y, p3x, p3y, p1x, p1y, 1 - t2, True)

newPointX1 = newPointX
newPointY1 = newPointY

print (f"newpointx {newPointX, newPointY}")

# relative to mounting hole

p4x = c1x + (bigC1 - lowerHoleDist) / Sin(a1)
p5x = c1x + axleDist - miniC2 - hole3Dist
p5y = c1y + (p4x - p5x) * Tan(a1)

a0x = MovePointWithAngle(p5x, p5y, 10, 0, a1)[0]
a0y = MovePointWithAngle(p5x, p5y, 10, 0, a1)[1]
# 0.5 is for correction due to the curve.
a1x = MovePointWithAngle(p5x, p5y, 10, lowerHoleDist + drop + supportThickness - 0.45, a1)[0]
a1y = MovePointWithAngle(p5x, p5y, 10, lowerHoleDist + drop + supportThickness - 0.45, a1)[1]
a2x = c1x - 7.5
a2y = c1y + Sqrt(Pow(supportC1, 2) - 7.5 * 7.5)
a3x = a2x
a3y = c1y + Sqrt(Pow(bigC1, 2) - 7.5 * 7.5)
a4x = a2x
a4y = p5y + supportElevation + (p5x - a2x) * Tan(a1)
a7x = MovePointWithAngle(p5x, p5y, -10 / Sqrt(2), -10 / Sqrt(2), a1)[0]
a7y = MovePointWithAngle(p5x, p5y, -10 / Sqrt(2), -10 / Sqrt(2), a1)[1]
a6x = MovePointWithAngle(a7x, a7y, -supportElevation - 10 / Sqrt(2), supportElevation + 10 / Sqrt(2), a1)[0]
a6y = MovePointWithAngle(a7x, a7y, -supportElevation - 10 / Sqrt(2), supportElevation + 10 / Sqrt(2), a1)[1]
d67 = Sqrt(Pow(a7x - a6x, 2) + Pow(a7y - a6y, 2))
a5x = MovePointWithAngle(a6x, a6y, -d67, 0, a1)[0]
a5y = MovePointWithAngle(a6x, a6y, -d67, 0, a1)[1]
 
b1x = MovePointWithAngle(p5x, p5y, -supportHole1, supportElevation, a1)[0]
b1y = MovePointWithAngle(p5x, p5y, -supportHole1, supportElevation, a1)[1]
b2x = MovePointWithAngle(p5x, p5y, -supportHole1 + supportHoleTopDistance / Sqrt(2), supportHoleElevation - supportHoleTopDistance / Sqrt(2), a1)[0]
b2y = MovePointWithAngle(p5x, p5y, -supportHole1 + supportHoleTopDistance / Sqrt(2), supportHoleElevation - supportHoleTopDistance / Sqrt(2), a1)[1]
b3x = MovePointWithAngle(p5x, p5y, -supportHole1 + 2 * supportHoleTopDistance / Sqrt(2) + lowerHoleDist - supportHoleElevation, lowerHoleDist, a1)[0]
b3y = MovePointWithAngle(p5x, p5y, -supportHole1 + 2 * supportHoleTopDistance / Sqrt(2) + lowerHoleDist - supportHoleElevation, lowerHoleDist, a1)[1]
b4x = MovePointWithAngle(a1x, a1y, -(thickness + supportThickness), -(thickness + supportThickness), a1)[0]
b4y = MovePointWithAngle(a1x, a1y, -(thickness + supportThickness), -(thickness + supportThickness), a1)[1]

# holes 6 mm above straight line of inside border, ca. 8.3 mm above actual curve
b5x = MovePointWithAngle(p5x, p5y, -supportHole1, supportHoleElevation, a1)[0]
b5y = MovePointWithAngle(p5x, p5y, -supportHole1, supportHoleElevation, a1)[1]
b6x = MovePointWithAngle(p5x, p5y, -supportHole2, supportHoleElevation, a1)[0]
b6y = MovePointWithAngle(p5x, p5y, -supportHole2, supportHoleElevation, a1)[1]
b7x = MovePointWithAngle(p5x, p5y, -supportHole3, supportHoleElevation, a1)[0]
b7y = MovePointWithAngle(p5x, p5y, -supportHole3, supportHoleElevation, a1)[1]

correctedC1y = c1y - supportVerticalCorrection
d_b5c1 = Sqrt(Pow(b5x - c1x, 2) + Pow(b5y - correctedC1y, 2))
d_b7c1 = Sqrt(Pow(b7x - c1x, 2) + Pow(b7y - correctedC1y, 2))
topHoleCurve = 15
bottomHoleCurve = 7.5
midHoleCurve = (25 - bottomHoleCurve * Sqrt(3)) / Sqrt(3)
translate1X = c1x - topHoleCurve
translate1Y = c1y - supportVerticalCorrection - topHoleCurve
d1x = c1x + topHoleCurve * (b5y - correctedC1y) / d_b5c1
d1y = correctedC1y - topHoleCurve * (b5x - c1x) / d_b5c1
d4x = b5x + bottomHoleCurve * (b5y - correctedC1y) / d_b5c1
d4y = b5y - bottomHoleCurve * (b5x - c1x) / d_b5c1
d2x = d1x + (b5x - c1x) / 4
d2y = d1y + (b5y - correctedC1y) / 4
d3x = d4x - (b5x - c1x) / 4
d3y = d4y - (b5y - correctedC1y) / 4
d5x = MovePointWithAngle(b5x, b5y, -bottomHoleCurve * Sqrt(3) / 2, bottomHoleCurve / 2, a1)[0]
d5y = MovePointWithAngle(b5x, b5y, -bottomHoleCurve * Sqrt(3) / 2, bottomHoleCurve / 2, a1)[1]
d6x = MovePointWithAngle(b6x, b6y, bottomHoleCurve * Sqrt(3) / 2, bottomHoleCurve / 2, a1)[0]
d6y = MovePointWithAngle(b6x, b6y, bottomHoleCurve * Sqrt(3) / 2, bottomHoleCurve / 2, a1)[1]
d7x = MovePointWithAngle(b6x, b6y, -bottomHoleCurve * Sqrt(3) / 2, bottomHoleCurve / 2, a1)[0]
d7y = MovePointWithAngle(b6x, b6y, -bottomHoleCurve * Sqrt(3) / 2, bottomHoleCurve / 2, a1)[1]
d8x = MovePointWithAngle(b7x, b7y, bottomHoleCurve * Sqrt(3) / 2, bottomHoleCurve / 2, a1)[0]
d8y = MovePointWithAngle(b7x, b7y, bottomHoleCurve * Sqrt(3) / 2, bottomHoleCurve / 2, a1)[1]
d9x = b7x - bottomHoleCurve * (b7y - correctedC1y) / d_b7c1
d9y = b7y + bottomHoleCurve * (b7x - c1x) / d_b7c1
d12x = c1x - topHoleCurve * (b7y - correctedC1y) / d_b7c1
d12y = correctedC1y + topHoleCurve * (b7x - c1x) / d_b7c1 
d10x = d9x - (b7x - c1x) / 4
d10y = d9y - (b7y - correctedC1y) / 4
d11x = d12x + (b7x - c1x) / 4
d11y = d12y + (b7y - correctedC1y) / 4

# check if they are close

print("\nSupport, outer segment point: line " + r(a1x) + " " + r(a1y) + ", curve " + r(newPointX0) + " " + r(newPointY0) + ", diff " + r(newPointX0 - a1x) + " " + r(newPointY0 - a1y))
print("Bottom attachment hole 1: " + r(b5x) + " " + r(b5y) + ", wheel middle " + r(c1x) + " " + r(c1y) + ", distance " + str(Sqrt(Pow(b5x - c1x, 2) + Pow(b5y - c1y, 2))) + "\n")

support1 = M(a0x, a0y) + L(a1x, a1y) + segment1 + A(supportC1, a2x, a2y, True) + L(a3x, a3y) + C(a4x, a4y, b1x, b1y) + L(a5x, a5y) + C(a6x, a6y, a7x, a7y) + A(10, a0x, a0y, True) + M(p5x, p5y - 2.5) + HCCW(p5x, p5y, 5)

support2 = M(newPointX0, newPointY0) + segment1 + A(supportC1, a2x, a2y, True) + L(a3x, a3y) + A(bigC1, p1x, p1y, False) + segment2 + " z"

support3 = M(a1x, a1y) + segment1 + A(supportC1, a2x, a2y, True) + L(a3x, a3y) + C(a4x, a4y, b1x, b1y) + A(supportHoleTopDistance, b2x, b2y, True) + C(b3x, b3y, b4x, b4y) + A(thickness + supportThickness, a1x, a1y, True) + M(b5x, b5y - 2.1) + HCCW(b5x, b5y, 4.2) + HCCW(b6x, b6y, 4.2) + HCCW(b7x, b7y, 4.2)

angle1 = 3.5
if not laserCut:
    sx = 106.94
    sy = 86.59
    ex = 118.743
    ey = 103.354
    line1Length = 4.6
    line2Length = 5.3
else:
    sx = 106.94
    sy = 86.59
    ex = 118.743
    ey = 103.354
    line1Length = 4.6
    line2Length = 5.3

slot1x = c1x - slotLength / 2 * Cos(angle1)
slot1y = correctedC1y - slotLength / 2 * Sin(angle1)
slot2x = c1x + slotLength / 2 * Cos(angle1)
slot2y = correctedC1y + slotLength / 2 * Sin(angle1)
d = Sqrt(13.5 * 13.5 + 9 * 9) # 16.225
angle2 = Atan(9/13) + angle1
px = c1x + d * Cos(angle2)
py = correctedC1y + d * Sin(angle2)

support4 = M(d1x, d1y) + C2(d2x, d2y, d3x, d3y, d4x, d4y) + A(bottomHoleCurve, d5x, d5y, True) + A(midHoleCurve, d6x, d6y, False) + A(bottomHoleCurve, d7x, d7y, True) + A(midHoleCurve, d8x, d8y, False) + A(bottomHoleCurve, d9x, d9y, True) + C2(d10x, d10y, d11x, d11y, d12x, d12y) + A(topHoleCurve, d1x, d1y, True) + " z" + SlotCCW(slot1x, slot1y, slot2x, slot2y, screwHole) + HCCW(px, py, screwHole) + HCCW(b5x, b5y, screwHole5mm) + HCCW(b6x, b6y, screwHole5mm) + HCCW(b7x, b7y, screwHole5mm)
# frame cutout. Bottom matches mounting hole bottom
support4 += M(sx, sy) + L(sx - Sin(angle1) * line1Length, sy + Cos(angle1) * line1Length) + C(sx - Sin(angle1) * (line1Length + line2Length), sy + Cos(angle1) * (line1Length + line2Length), ex, ey)

tr3x = 0
tr3y = 0
support4W = w
support4H = h
if laserCut:
    tr3x = -translate1X
    tr3y = -translate1Y
    support4W = r(b5x + bottomHoleCurve - translate1X)
    support4H = r(b7y + bottomHoleCurve - translate1Y)


support5 = SlotCCW(-1.5, c1y, c1x, c1y, 18) + HCCW(-1.5, c1y, screwHole5mm) + SlotCCW(c1x - slotLength / 2, c1y, c1x + slotLength / 2, c1y, screwHole)

tr4x = 0
tr4y = 0
support5W = w
support5H = h
if laserCut:
    tr4x = 10.5
    tr4y = -(c1y - 9)
    support5W = r(1.5 + c1x + 18)
    support5H = 18

# rear washer

path15 = HCCW(15, 15, bigScrewHole) + HCCW(35, 15, bigScrewHole) + SlotCCW(15, 15, 35, 15, 30)

# battery lock
# Battery box: 128 mm wide, handle: 8 x 106, slot 10 x 110, thickness beyond 10
# eyelets: 6 x 28, slot 8 x 30, thickness beyond 8
# Holder width: 166 (130 space for battery), with eyelets: 170, with lock 174
# eyelet distance: 150

sx = 10 + adjust / 2
sy = 10 + adjust / 2
r1 = 10 + adjust / 2
r2 = 10 - adjust / 2
d = 23.365 # guide circles touch each other; x-distance from start hole
slot1 = 8 - adjust
slot2 = 10 - adjust

points = [[sx, sy], [sx, sy + 40], [sx + 4, sy + 40], [sx + d, sy + 45], [sx + 154 - d, sy + 45], [sx + 150, sy + 40], [sx + 154, sy + 40], [sx + 154, sy], [sx + 150, sy], [sx + 154 - d, sy - 5], [sx + d, sy - 5], [sx + 4, sy]]
radiuses = [r1, r1, r1, r2, r2, r1, r1, r1, r1, r2, r2, r1]
mask = [0, 0, 0, 1, 1, 0, 0, 0, 0, 1, 1, 0]

path16 = DrawOutlineCCWInOut(points, radiuses, mask) + SlotCCW(12, 19, 12, 41, slot1) + SlotCCW(162, 19, 162, 41, slot1) + SlotCCW(37, 30, 137, 30, slot2)

# rear fender mount
# mount distance 49 mm
# 16, 6 mm
# middle at x 36.5
# bend at 76 mm total, 66 mm down from mounting hole
# lights at 82 total, 72 down
# height 88 mm
# width 92 mm
# offset 2 mm

points = [[21.5, 10], [5.5, 70], [6, 82], [86, 82], [86.5, 70], [70.5, 10]]
radiuses = [10, 6, 6, 6, 6, 10]
mask = [0, 1, 0, 0, 1, 0]

path17 = DrawOutlineCCWInOut(points, radiuses, mask) + HCCW(19.5, 10, screwHole) + HCCW(68.5, 10, screwHole) + HCCW(46, 41, screwHole) + HCCW(6, 82, screwHole5mm) + HCCW(46, 82, screwHole8mm) + HCCW(86, 82, screwHole5mm)


# L-plate
# Holes at top, from bend: 16, 58, upper length: 68
# at sides: 51.5; 39, 89.783
# Length: -4 mm due to bend

print (f"L-plate hole 2 3 distance: {d23}, 3rd hole x-position: {104.5 + d23}\n")

h1 = 51.5
h2 = 39
h3 = h2 + d23

path18 = SlotCCW(10, 10, 68 - 4 + h1, 10, 20) + HCCW(10, 10, screwHole) + HCCW(52, 10, screwHole) + HCCW(68 - 4 + h1, 10, screwHole5mm)
L1W = 68 - 4 + h1 + 10
path19 = SlotCCW(10, 10, 68 - 4 + h3, 10, 20) + HCCW(10, 10, screwHole) + HCCW(52, 10, screwHole) + HCCW(68 - 4 + h2, 10, screwHole5mm) + HCCW(68 - 4 + h3, 10, screwHole5mm)
L2W = 68 - 4 + h3 + 10

# pi cover, 165 x 116

points = [[4, 4], [4, 112], [161, 112], [161, 4]]
radiuses = [4, 4, 4, 4]

path20 = DrawOutlineCCW(points, radiuses) + HCCW(4, 4, 3) + HCCW(4, 112, 3) + HCCW(161, 4, 3) + HCCW(161, 112, 3) + HCCW(11.65, 39.10, 3) + HCCW(60.65, 39.10, 3) + HCCW(11.65, 97.10, 3) + HCCW(60.65, 97.10, 3) + HCCW(108.5, 62.45, 3) + HCCW(159.5, 62.45, 3) + HCCW(108.5, 99.25, 3) + HCCW(159.5, 99.25, 3) + HCCW(159.5, 16.75, 3) + HCCW(36.15 + 40.55 + 3.5, 57 - 10.32, 3) + HCCW(36.15 + 40.55 + 3.5, 57 - 33.48, 3) + HCCW(36.15 + 111.45 - 3.5, 57 - 10.32, 3) + HCCW(36.15 + 111.45 - 3.5, 57 - 33.48, 3)

sectionNames = ["wheel mount", "sprocket mount", "sprocket guides", "rear fork extension", "rear fork extension brake", "front fork extension", "plate big", "border big", "plate small", "border small", "support1", "support2", "support3", "support4", "support5", "aligner", "3 holes", "rear washer", "battery lock", "rear fender mount", "L-plate1", "L-plate2", "pi cover"]
fileNames = ["wheel mount 80T", "sprocket mount 80T", "sprocket guides 80T", "rear fork extension", "rear fork extension brake", "front fork extension", "plate big 80T", "border big 80T", "plate small 80T", "border small 80T", "support1 80T", "support2 80T", "support3 80T", "support4 80T", "support5 80T", "aligner 80T", "3 holes 80T", "rear washer", "battery lock", "rear fender mount", "L-plate1", "L-plate2", "pi cover"]
sizesX = [wheelMountOutside, sprocketMountOutside, sprocketMountOutside, 79, 90, 33, plateBigW, plateBigW, plateSmallW, plateSmallW, w, w, w, support4W, support5W, w, Holes3W, 50, 174 + adjust, 92, L1W, L2W, 165]
sizesY = [wheelMountOutside, sprocketMountOutside, sprocketMountOutside, 45, 92, 67, plateBigH, plateBigH, plateSmallH, plateSmallH, h, h, h, support4H, support5H, h, Holes3H, 30, 60 + adjust, 88, 20, 20, 116]

with open(f"chain screen 80_2 orig.svg", "r") as file:
    orig = file.read()    
    orig = orig.replace("[bgString]", bgString)  
    orig = orig.replace("[class]", pathClass)
    orig = orig.replace("[class2]", pathClass2)  
    orig = orig.replace("[path1]", path1)    
    orig = orig.replace("[path2]", path2)
    # with guides
    orig = orig.replace("[path3]", path3)
    orig = orig.replace("[path4]", path4) 
    orig = orig.replace("[path5]", path5)
    orig = orig.replace("[path5_1]", path5_1) 
    orig = orig.replace("[ecx]", str(ecx))
    orig = orig.replace("[ecy]", str(ecy)) 
    orig = orig.replace("[smallR]", str(smallEllR))
    orig = orig.replace("[bigR]", str(bigEllR))
    orig = orig.replace("[rotateAngle]", str(-angleEllipse))
    orig = orig.replace("[path6]", path6)
    orig = orig.replace("[path7]", path7) 
    orig = orig.replace("[path8]", path8) 
    orig = orig.replace("[path9]", path9) 
    orig = orig.replace("[tr1x]", r(tr1x))
    orig = orig.replace("[path10]", path10) 
    orig = orig.replace("[path11]", path11)
    orig = orig.replace("[tr2x]", r(tr2x))
    orig = orig.replace("[path12]", path12) 
    orig = orig.replace("[path13]", path13)
    orig = orig.replace("[support1]", support1)
    orig = orig.replace("[support2]", support2)
    orig = orig.replace("[support3]", support3)
    orig = orig.replace("[support4]", support4) 
    orig = orig.replace("[support5]", support5)
    orig = orig.replace("[tr3x]", r(tr3x))
    orig = orig.replace("[tr3y]", r(tr3y)) 
    orig = orig.replace("[tr4x]", r(tr4x))
    orig = orig.replace("[tr4y]", r(tr4y))     
    orig = orig.replace("[holeLines]", holeLines) 
    orig = orig.replace("[aligner]", aligner)
    orig = orig.replace("[path14]", path14)
    orig = orig.replace("[tr5x]", r(tr5x))
    orig = orig.replace("[tr5y]", r(tr5y)) 
    orig = orig.replace("[path15]", path15)  
    orig = orig.replace("[path16]", path16)
    orig = orig.replace("[path17]", path17)
    orig = orig.replace("[path18]", path18)   
    orig = orig.replace("[path19]", path19) 
    orig = orig.replace("[path20]", path20)      

    with open("chain screen 80_2 all.svg", "w") as file:
        file.write(orig.replace("[w]", str(w)).replace("[h]", str(h)))

    counter = 0
    name = sectionNames[counter]    
    startPos = orig.find("<!-- " + name + " -->")
    pos = startPos
    lastPos = orig.find("<!---->")

    while pos != lastPos:
        if counter < len(sectionNames) - 1:
            nextName = sectionNames[counter + 1]
            nextPos = orig.find("<!-- " + nextName + " -->")
        else:
            nextPos = lastPos
        l = len(name) + 10
        content = orig[0:startPos] + orig[pos+l:nextPos] + "</svg>"

        with open(f"{fileNames[counter]}.svg", "w") as file:
            file.write(content.replace("[w]", str(sizesX[counter])).replace("[h]", str(sizesY[counter])))
            print(f"{fileNames[counter]}.svg") 

        pos = nextPos
        name = nextName
        counter += 1