import math
import sys

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
outline = False

print(f"Laser cut: {laserCut}\n")

adjust = 0
pathClass = "l2" # 0.2 black unfilled
if laserCut:
    adjust = 0.2
    pathClass = "l2" # 0.02 black unfilled

bgString = '' # Inkscape's DXF R14 exporter cannot handle this commented section.
outlineStr = ''

if background:
    bgString = '<rect width="100%" height="100%" fill="#dddddd" />'
    
if outline:
    outlineStr = '<rect width="100%" height="100%" fill="none" stroke="black" stroke-width="0.2" />'

# bearing plates

squareSize = 110
margin = 3
w = 300
h = 200
c1x = squareSize / 2 + margin
c1y = squareSize / 2 + margin
centerHoleSmall = 6 - adjust
mountingHole = 6 - adjust
centerHoleBig = 14 - adjust
plateConnectorHole = 4 - adjust
smallBearingInner = 35
smallBearingInnerHousing = 37.3
smallBearingOuter = 52
bigBearingInner = 80
bigBearingInnerHousing = 82.3
sealWidth = 2
c2x = bigBearingInner / 2 + margin
c2y = bigBearingInner / 2 + margin
c3x = smallBearingOuter / 2 + margin
c3y = smallBearingOuter / 2 + margin
c4x = smallBearingInner / 2 + margin
c4y = smallBearingInner / 2 + margin
hole1dist = (smallBearingOuter + bigBearingInner) / (4 * Sqrt(2))
hole2dist = 85 / 2
hole3dist = (centerHoleBig + adjust + smallBearingInner) / 4
hole4dist = (centerHoleSmall + adjust + smallBearingInner) / 4
# at 24 degree rotation, the rounded rectangle extends to 65 mm from the center. Radius calculated as: ...
cornerRadius = 47.555 / 2
cornerCenterDist = squareSize / 2 - cornerRadius 
print(f"cornerCenterDist {cornerCenterDist}")
points1 = [[c1x - cornerCenterDist, c1y - cornerCenterDist], [c1x - cornerCenterDist, c1y + cornerCenterDist],[c1x + cornerCenterDist, c1y + cornerCenterDist],[c1x + cornerCenterDist, c1y - cornerCenterDist]]
radiuses1 = [cornerRadius + adjust / 2] * 4

path1 = HCW(c1x, c1y, centerHoleSmall) + \
    HCW(c1x - hole1dist, c1y - hole1dist, plateConnectorHole) + \
    HCW(c1x - hole1dist, c1y + hole1dist, plateConnectorHole) + \
    HCW(c1x + hole1dist, c1y - hole1dist, plateConnectorHole) + \
    HCW(c1x + hole1dist, c1y + hole1dist, plateConnectorHole) + \
    HCW(c1x - hole2dist, c1y - hole2dist, mountingHole) + \
    HCW(c1x - hole2dist, c1y + hole2dist, mountingHole) + \
    HCW(c1x + hole2dist, c1y - hole2dist, mountingHole) + \
    HCW(c1x + hole2dist, c1y + hole2dist, mountingHole) + \
    DrawOutlineCCW(points1, radiuses1)

path2 = HCW(c2x, c2y, bigBearingInner + adjust) + \
    HCW(c2x, c2y, smallBearingOuter - adjust) + \
    HCW(c2x - hole1dist, c2y - hole1dist, plateConnectorHole) + \
    HCW(c2x - hole1dist, c2y + hole1dist, plateConnectorHole) + \
    HCW(c2x + hole1dist, c2y - hole1dist, plateConnectorHole) + \
    HCW(c2x + hole1dist, c2y + hole1dist, plateConnectorHole)

path3 = HCW(c2x, c2y, bigBearingInnerHousing + adjust) + \
    HCW(c2x, c2y, smallBearingOuter - adjust) + \
    HCW(c2x - hole1dist, c2y - hole1dist, plateConnectorHole) + \
    HCW(c2x - hole1dist, c2y + hole1dist, plateConnectorHole) + \
    HCW(c2x + hole1dist, c2y - hole1dist, plateConnectorHole) + \
    HCW(c2x + hole1dist, c2y + hole1dist, plateConnectorHole)

path4 = HCW(c1x, c1y, centerHoleBig) + \
    HCW(c1x - hole1dist, c1y - hole1dist, plateConnectorHole) + \
    HCW(c1x - hole1dist, c1y + hole1dist, plateConnectorHole) + \
    HCW(c1x + hole1dist, c1y - hole1dist, plateConnectorHole) + \
    HCW(c1x + hole1dist, c1y + hole1dist, plateConnectorHole) + \
    HCW(c1x - hole2dist, c1y - hole2dist, mountingHole) + \
    HCW(c1x - hole2dist, c1y + hole2dist, mountingHole) + \
    HCW(c1x + hole2dist, c1y - hole2dist, mountingHole) + \
    HCW(c1x + hole2dist, c1y + hole2dist, mountingHole) + \
    HCW(c1x - hole3dist, c1y, plateConnectorHole) + \
    HCW(c1x, c1y + hole3dist, plateConnectorHole) + \
    HCW(c1x + hole3dist, c1y, plateConnectorHole) + \
    HCW(c1x, c1y - hole3dist, plateConnectorHole) + \
    DrawOutlineCCW(points1, radiuses1)

path5 = HCW(c4x, c4y, centerHoleBig) + \
    HCW(c4x, c4y, smallBearingInnerHousing + adjust) + \
    HCW(c4x - hole3dist, c4y, plateConnectorHole) + \
    HCW(c4x, c4y + hole3dist, plateConnectorHole) + \
    HCW(c4x + hole3dist, c4y, plateConnectorHole) + \
    HCW(c4x, c4y - hole3dist, plateConnectorHole)

path6 = HCW(c4x, c4y, centerHoleSmall) + \
    HCW(c4x, c4y, smallBearingInner + adjust) + \
    HCW(c4x - hole4dist, c4y, plateConnectorHole) + \
    HCW(c4x, c4y + hole4dist, plateConnectorHole) + \
    HCW(c4x + hole4dist, c4y, plateConnectorHole) + \
    HCW(c4x, c4y - hole4dist, plateConnectorHole)

path7 = HCW(c3x, c3y, centerHoleSmall) + \
    HCW(c3x, c3y, smallBearingOuter + 2 * sealWidth + adjust) + \
    HCW(c3x - hole4dist, c3y, plateConnectorHole) + \
    HCW(c3x, c3y + hole4dist, plateConnectorHole) + \
    HCW(c3x + hole4dist, c3y, plateConnectorHole) + \
    HCW(c3x, c3y - hole4dist, plateConnectorHole)

# crankbox attachment

crankW = 50
crankH1 = 52
crankH2 = 15 + 53
c5x = crankW / 2 + margin
c5y = crankW / 2 + 2 + margin
c6x = crankW / 2 + margin
c6y = crankW / 2 + 3 + 15 + margin
crankSmallHole = 24 - adjust
crankBigHole = 30 - adjust
crankPlateConnectorHole = 5 - adjust
crankPilotHole = 4
crankCornerRadius = 9
hole5dist = 16
hole6dist = 20
points2 = [[crankCornerRadius + margin, crankCornerRadius + margin], [crankCornerRadius + margin, crankH2 - crankCornerRadius + margin], [crankW - crankCornerRadius + margin, crankH2 - crankCornerRadius + margin], [crankW - crankCornerRadius + margin, crankCornerRadius + margin]]
radiuses2 = [crankCornerRadius + adjust / 2] * 4

path8 = HCW(c5x, c5y, crankBigHole) + \
    HCW(c5x - hole5dist, c5y - hole5dist, mountingHole) + \
    HCW(c5x - hole5dist, c5y + hole5dist, mountingHole) + \
    HCW(c5x + hole5dist, c5y - hole5dist, mountingHole) + \
    HCW(c5x + hole5dist, c5y + hole5dist, mountingHole) + \
    HCW(c5x - hole6dist, c5y, crankPlateConnectorHole) + \
    HCW(c5x, c5y + hole6dist, crankPlateConnectorHole) + \
    HCW(c5x + hole6dist, c5y, crankPlateConnectorHole) + \
    HCW(c5x, c5y - hole6dist, crankPlateConnectorHole) + \
    M(margin, margin + 6) + L(margin + 15, margin + 6) + \
    A(10.05, margin + 17.79, margin, True) + \
    L(margin + crankW - 17.79, margin) + \
    A(10.05, margin + crankW - 15, margin + 6, True) + \
    L(margin + crankW, margin + 6) + \
    L(margin + crankW, margin + crankH1 - crankCornerRadius) + \
    A(crankCornerRadius, margin + crankW - crankCornerRadius, margin + crankH1, True) + \
    L(margin + crankCornerRadius, margin + crankH1) + \
    A(crankCornerRadius, margin, margin + crankH1 - crankCornerRadius, True) + \
    L(margin, margin + 6)

path9 = HCW(c6x, c6y, crankSmallHole) + \
    HCW(c6x - hole5dist, c6y - hole5dist, mountingHole) + \
    HCW(c6x - hole5dist, c6y + hole5dist, mountingHole) + \
    HCW(c6x + hole5dist, c6y - hole5dist, mountingHole) + \
    HCW(c6x + hole5dist, c6y + hole5dist, mountingHole) + \
    HCW(c6x - hole6dist, c6y, crankPlateConnectorHole) + \
    HCW(c6x, c6y + hole6dist, crankPlateConnectorHole) + \
    HCW(c6x + hole6dist, c6y, crankPlateConnectorHole) + \
    HCW(c6x, c6y - hole6dist, crankPlateConnectorHole) + \
    HCW(c6x - hole5dist, crankCornerRadius + margin, crankPilotHole) + \
    HCW(c6x + hole5dist, crankCornerRadius + margin, crankPilotHole) + \
    DrawOutlineCCW(points2, radiuses2)

# sawblade gasket

path10 = HCW(5.1, 5.1, 10.2) + HCW(20.1, 5.1, 10.2) + HCW(5.1, 5.1, 5.8) + HCW(20.1, 5.1, 5.8)

sectionNames = ["bearing 1", "bearing 2", "bearing 3", "bearing 4", "bearing 5", "bearing 6", "bearing 7", "crankbox 1", "crankbox 2", "sawblade gasket"]
fileNames = ["bearing 1", "bearing 2", "bearing 3", "bearing 4", "bearing 5", "bearing 6", "bearing 7", "crankbox 1", "crankbox 2", "sawblade gasket"]
sizesX = [squareSize + 2 * margin, bigBearingInner + 2 * margin, bigBearingInner + 2 * margin, squareSize + 2 * margin, smallBearingInner + 2 * margin, smallBearingInner + 2 * margin, smallBearingOuter + 2 * margin, crankW + 2 * margin, crankW + 2 * margin, 25.2]
sizesY = [squareSize + 2 * margin, bigBearingInner + 2 * margin, bigBearingInner + 2 * margin, squareSize + 2 * margin, smallBearingInner + 2 * margin, smallBearingInner + 2 * margin, smallBearingOuter + 2 * margin, crankH1 + 2 * margin, crankH2 + 2 * margin, 10.2]

with open(f"turntable orig.svg", "r") as file:
    orig = file.read()    
    orig = orig.replace("[bgString]", bgString) 
    orig = orig.replace("[class]", pathClass)
    orig = orig.replace("[path1]", path1)    
    orig = orig.replace("[path2]", path2)
    orig = orig.replace("[path3]", path3)
    orig = orig.replace("[path4]", path4) 
    orig = orig.replace("[path5]", path5)
    orig = orig.replace("[path6]", path6)
    orig = orig.replace("[path7]", path7)
    orig = orig.replace("[path8]", path8)
    orig = orig.replace("[path9]", path9)
    orig = orig.replace("[path10]", path10)      

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
        content = content.replace(f"[t{counter + 1}x]", "0")
        content = content.replace(f"[t{counter + 1}y]", "0")
        content = content.replace("[outline]", "")

        with open(f"{fileNames[counter]}.svg", "w") as file:
            file.write(content.replace("[w]", str(sizesX[counter])).replace("[h]", str(sizesY[counter])))
            print(f"{fileNames[counter]}.svg") 

        pos = nextPos
        name = nextName
        counter += 1

    # tightest fit
    trxs = [0, 0, 83, 113, 22.5, 105.5, 166, 226, 226]
    trys = [0, 113, 113, 0, 135.5, 135.5, 116, 0, 55]
    # more clearance (bearing 1, 4, 7, crankbox 1, 2: cut)
    trxs = [6, 78.5, 193.5, 121, 101, 216, 237, 237, 9]
    trys = [2, 113, 113, 2, 135.5, 135.5, 62, 5, 120]
    # bearing 2, 3, 5, 6, bearing 7 remade:  cut; crankbox 2 remade: not cut; sawblade gasket
    trxs = [6, 64, 209, 121, 86.5, 231.5, 150, 237, 9, 150]
    trys = [2, 114.5, 110.5, 2, 137, 133, 122, 5, 122, 182]

    orig = orig.replace("[outline]", outlineStr)

    for i in range (0, len(trxs)):
        orig = orig.replace(f"[t{i + 1}x]", str(trxs[i]))
        orig = orig.replace(f"[t{i + 1}y]", str(trys[i]))

    with open("bearing all.svg", "w") as file:
        file.write(orig.replace("[w]", str(w)).replace("[h]", str(h)))