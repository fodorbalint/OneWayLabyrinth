using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.Diagnostics.Metrics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Security.Cryptography;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Text;
using System.Windows.Documents;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace OneWayLabyrinth
{
    public partial class Path
    {
        MainWindow window;
        bool debug, debug2;
        int size;
        public List<int[]> path;
        public List<int[]> path2 = new List<int[]>(); //future line uses the main line to check forbidden fields
        public List<int[]> nextStepPath = new List<int[]>();
        bool isMain = true;
        public bool isNearEnd = false;
        public int count;
        public List<int[]> possible = new List<int[]>(); //field coordinates
        public List<int[]> nextStepPossible = new List<int[]>();
        List<int[]> forbidden = new List<int[]>();
        public int x, y, x3, y3;
        public int sx = 0; //straight, left and right coordinates
        public int sy = 0;
        public int lx = 0;
        public int ly = 0;
        public int rx = 0;
        public int ry = 0;
        public int thisSx = 0; // remain constant in one step, while the above variables change for the InTakenRel calls.
        public int thisSy = 0;
        public int thisLx = 0;
        public int thisLy = 0;
        int[] straightField = new int[] { };
        int[] leftField = new int[] { };
        int[] rightField = new int[] { };
        List<int[]> directions = new List<int[]> { new int[] { 0, 1 }, new int[] { 1, 0 }, new int[] { 0, -1 }, new int[] { -1, 0 } }; //down, right, up, left
        int foundSectionStart, foundSectionEnd;
        bool CShape = false;
        //bool CShapeLeft = false;
        //bool CShapeRight = false;

        //bool closeStraight = false;
        //bool closeMidAcross = false;
        //bool closeAcross = false;

        // rotation at which if we step straight, an area is created on both sides that we need to enter.
        // 9_234212, 9_522267
        int nextStepEnterLeft = -1;
        int nextStepEnterRight = -1;

        public List<List<int[]>> examAreaLines = new();
        public List<int> examAreaLineTypes = new();
        public List<bool> examAreaLineDirections = new();
        public List<List<int[]>> examAreaPairFields = new();
        List<int[]> examAreaLine2 = new();
        int examAreaLineType2 = 0;
        bool examAreaLineDirection2 = false;
        List<int[]> examAreaPairField2 = new();

        //used only for displaying area
        public List<List<int[]>> areaLines = new();
        public List<int> areaLineTypes = new();
        public List<bool> areaLineDirections = new();
        public List<List<int[]>> areaPairFields = new();
        public List<bool> areaLineSecondary = new();

        List<object> info;

        // defined in PathRules.cs
        /*List<string> activeRules;
        List<List<int[]>> activeRulesForbiddenFields;
        List<int[]> activeRuleSizes;*/

        // used for double area cases
        int x2 = 0;
        int y2 = 0;
        int sx2 = 0;
        int sy2 = 0;
        int lx2 = 0;
        int ly2 = 0;

        int counterrec = 0;
        int sequenceLeftObstacleIndex = -1;

        List<int[]>[] closedCorners = new List<int[]>[4];
        List<int[]>[] openCWCorners = new List<int[]>[4];
        List<int[]>[] openCCWCorners = new List<int[]>[4];
        // quarters examined at left and right side for CW rotation
        int[][] quarters = new int[][] { new int[] { 0, 1, 2, 3 }, new int[] { 1, 0, 3, 2 } };
        List<int[]> quarterMultipliers = new List<int[]>() { new int[] { 1, 1 }, new int[] { -1, 1 }, new int[] { -1, -1 }, new int[] { 1, -1 } };
        public bool suppressLogs = false; // used when loading from file

        public Path(MainWindow window, int size, List<int[]> path, List<int[]>? path2, bool isMain)
        {
            this.window = window;
            this.size = size;

            count = path.Count;
            if (count > 0)
            {
                x = path[count - 1][0];
                y = path[count - 1][1];
            }
            else
            {
                x = 1;
                y = 1;
            }
            this.path = path;
            this.path2 = path2;
            this.isMain = isMain;
        }

        public void NextStepPossibilities(bool isNearEnd, int index, int nearSection, int farSection)
        {
            try
            {
                possible = new List<int[]>();

                count = path.Count;
                if (count < 2)
                {
                    possible.Add(new int[] { 2, 1 });
                    possible.Add(new int[] { 1, 2 });
                }
                else
                {
                    int x0, y0;
                    this.isNearEnd = isNearEnd;
                    if (index != -1) //for checking future lines
                    {
                        if (!isNearEnd)
                        {
                            //extend far end
                            x = path[index][0];
                            y = path[index][1];
                            x0 = path[index + 1][0];
                            y0 = path[index + 1][1];
                        }
                        else
                        {
                            //extend near end
                            x = path[index][0];
                            y = path[index][1];
                            x0 = path[index - 1][0];
                            y0 = path[index - 1][1];
                        }
                        T("NextSteppossibilities future, x " + x + " y " + y + " isNearEnd " + isNearEnd + " index " + index + " length " + path.Count);
                    }
                    else
                    {
                        x0 = path[count - 2][0];
                        y0 = path[count - 2][1];
                    }

                    int i;
                    for (i = 0; i < 4; i++)
                    {
                        //last movement: down, right, up, left
                        thisSx = sx = directions[i][0];
                        thisSy = sy = directions[i][1];

                        if (x - x0 == sx && y - y0 == sy)
                        {
                            if (i == 0)
                            {
                                thisLx = lx = directions[1][0];
                                thisLy = ly = directions[1][1];
                                rx = directions[3][0];
                                ry = directions[3][1];
                            }
                            else if (i == 3)
                            {
                                thisLx = lx = directions[0][0];
                                thisLy = ly = directions[0][1];
                                rx = directions[2][0];
                                ry = directions[2][1];
                            }
                            else
                            {
                                thisLx = lx = directions[i + 1][0];
                                thisLy = ly = directions[i + 1][1];
                                rx = directions[i - 1][0];
                                ry = directions[i - 1][1];
                            }

                            straightField = new int[] { x + sx, y + sy };
                            leftField = new int[] { x + lx, y + ly };
                            rightField = new int[] { x + rx, y + ry };

                            if (!window.calculateFuture)
                            {
                                if (!InTakenAbs(straightField) && !InBorderAbs(straightField))
                                {
                                    possible.Add(straightField);
                                }
                                if (!InTakenAbs(rightField) && !InBorderAbs(rightField))
                                {
                                    possible.Add(rightField);
                                }
                                if (!InTakenAbs(leftField) && !InBorderAbs(leftField))
                                {
                                    possible.Add(leftField);
                                }
                            }
                            else
                            {
                                if (!InTakenAbs(straightField) && !InBorderAbs(straightField) && !InFutureAbs(straightField))
                                {
                                    possible.Add(straightField);
                                }
                                if (!InTakenAbs(rightField) && !InBorderAbs(rightField) && !InFutureAbs(rightField))
                                {
                                    possible.Add(rightField);
                                }
                                if (!InTakenAbs(leftField) && !InBorderAbs(leftField) && !InFutureAbs(leftField))
                                {
                                    possible.Add(leftField);
                                }
                            }


                            // A future line may connect to another section as in 2023_0714_1 when we step straight, and a 2x2 line is created on the left
                            // For connecting to an older line, see 2023_0730
                            // It cannot connect to the end of the same section
                            if (!isMain)
                            {

                                if (isNearEnd && !window.inFuture) // main line can be connected to if it is not already connected to another future line
                                {
                                    int c2 = path2.Count;
                                    if (path2[c2 - 1][0] == straightField[0] && path2[c2 - 1][1] == straightField[1]) possible.Add(straightField);
                                    if (path2[c2 - 1][0] == rightField[0] && path2[c2 - 1][1] == rightField[1]) possible.Add(rightField);
                                    if (path2[c2 - 1][0] == leftField[0] && path2[c2 - 1][1] == leftField[1]) possible.Add(leftField);
                                }

                                if (!isNearEnd && InFutureStartAbs(straightField, nearSection) || isNearEnd && InFutureEndAbs(straightField, farSection))
                                {
                                    T("possible future connection straight");
                                    possible.Add(straightField);
                                }
                                // See 2023_0803
                                if (!isNearEnd && InFutureStartAbs(rightField, nearSection) || isNearEnd && InFutureEndAbs(rightField, farSection))
                                {
                                    T("possible future connection right");
                                    possible.Add(rightField);
                                }
                                if (!isNearEnd && InFutureStartAbs(leftField, nearSection) || isNearEnd && InFutureEndAbs(leftField, farSection))
                                {
                                    T("possible future connection left");
                                    possible.Add(leftField);
                                }
                            }
                            else if (window.calculateFuture)
                            {
                                if (InFutureStartAbs(straightField))
                                {
                                    T("possible future start straight");
                                    possible.Add(straightField);
                                }
                                if (InFutureStartAbs(rightField))
                                {
                                    T("possible future start right");
                                    possible.Add(rightField);
                                }
                                if (InFutureStartAbs(leftField))
                                {
                                    T("possible future start left");
                                    possible.Add(leftField);
                                }
                            }

                            /*if the only possible field is a future field, we don't need to check more. This will prevent unnecessary exits, as in 2023_0804.

                            if (isMain && possible.Count == 1 && InFutureStartAbs(possible[0])) break;*/

                            if (isMain && possible.Count == 1) break;

                            //CShape = false;

                            if (!isMain)
                            {
                                CheckFutureCShape();

                                if (!isNearEnd) // when the far end of the future line extends, it should be checked for border as in 2023_0714. Find out the minimum size for when it is needed.
                                {
                                }
                            }
                            else
                            {
                                if (MainWindow.rulesDisabled) break;

                                // ----- copy start -----
                                bool rules = true;
                                bool rules_control = false;

                                if (rules && rules_control)
                                {
                                    forbidden = new List<int[]>();
                                    ApplyRules();

                                    List<int[]> newPossible1 = new();
                                    foreach (int[] field in possible)
                                    {
                                        if (!InForbidden(field))
                                        {
                                            newPossible1.Add(field);
                                        }
                                    }

                                    forbidden = new List<int[]>();
                                    ApplyRules_control();

                                    List<int[]> newPossible2 = new();
                                    foreach (int[] field in possible)
                                    {
                                        if (!InForbidden(field))
                                        {
                                            newPossible2.Add(field);
                                        }
                                    }

                                    bool different = false;
                                    if (newPossible1.Count != newPossible2.Count) different = true;
                                    else
                                    {
                                        for (int j = 0; j < newPossible1.Count; j++)
                                        {
                                            if (!(newPossible1[j][0] == newPossible2[j][0] && newPossible1[j][1] == newPossible2[j][1])) different = true;
                                        }
                                    }

                                    if (different)
                                    {
                                        window.errorInWalkthrough = true;
                                        window.criticalError = true;
                                        window.errorString = "Results different.";
                                    }
                                    else
                                    {
                                        possible = newPossible1;
                                    }
                                }
                                else
                                {
                                    forbidden = new List<int[]>();
                                    if (rules) ApplyRules();
                                    else ApplyRules_control();

                                    List<int[]> newPossible = new();
                                    foreach (int[] field in possible)
                                    {
                                        if (!InForbidden(field))
                                        {
                                            newPossible.Add(field);
                                        }
                                    }
                                    possible = newPossible;
                                }
                                // ----- copy end -----
                            }
                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                T(ex.Message);
                T(ex.StackTrace);
            }
        }

        // ----- copy start -----
        void ApplyRules()
        {
            debug = false;
            debug2 = false;

            nextStepEnterLeft = -1;
            nextStepEnterRight = -1;

            closedCorners = new List<int[]>[] { new List<int[]>(), new List<int[]>(), new List<int[]>(), new List<int[]>() };
            openCWCorners = new List<int[]>[] { new List<int[]>(), new List<int[]>(), new List<int[]>(), new List<int[]>() };
            openCCWCorners = new List<int[]>[] { new List<int[]>(), new List<int[]>(), new List<int[]>(), new List<int[]>() };

            // needs to be checked before AreaUp, it can overwrite it as in 9_802973
            CornerDiscoveryAll();

            T("CheckCShapeNext");
            CheckCShapeNext();
            T("CheckStraight " + ShowForbidden());
            CheckStraight();
            T("CheckLeftRightAreaUp " + ShowForbidden());
            CheckLeftRightAreaUp();
            T("CheckLeftRightCorner " + ShowForbidden());
            CheckLeftRightCorner();
            T("Forbidden: " + ShowForbidden());

            T("NextStepEnter " + nextStepEnterLeft + " " + nextStepEnterRight);

            // 2024_0611_3, 2024_0611_4, 2024_0611_5, 2024_0611_6, 2024_0611_7, 2024_0611_8, 9_234212, 9_522267
            // 0 and 0 or 1 and 3. Beware of 1 and -1.
            // Overwrite order: 3, 0, 1 (See 9_802973 and 9_2020799)
            if (nextStepEnterLeft == 0 && nextStepEnterRight == 0 || nextStepEnterLeft + nextStepEnterRight == 4 && Math.Abs(nextStepEnterLeft - nextStepEnterRight) == 2)
            {
                switch (nextStepEnterLeft)
                {
                    case 0:
                        T("Next step double area, cannot step straight");
                        AddForbidden(0, 1);
                        break;
                    case 1:
                        T("Next step double area, cannot step right");
                        AddForbidden(-1, 0);
                        break;
                    case 3:
                        T("Next step double area, cannot step left");
                        AddForbidden(1, 0);
                        break;
                }
            }

            T("StairAtStartConvexIn2 " + ShowForbidden());
            StairAtStartConvexIn2();
            T("StairAtStartConvexIn3 " + ShowForbidden());
            StairAtStartConvexIn3();
            T("StairAtStartConvexIn4 " + ShowForbidden());
            StairAtStartConvexIn4();
            T("StairAtStartConvexStraight3 " + ShowForbidden());
            StairAtStartConvexStraight3();
            T("StairAtStartConvexStraight4 " + ShowForbidden());
            StairAtStartConvexStraight4();
            T("StairAtStartConvexStraight5 " + ShowForbidden());
            StairAtStartConvexStraight5();

            T("StairAtEndConvexIn2 " + ShowForbidden());
            StairAtEndConvexIn2();
            T("StairAtEndConvexIn3 " + ShowForbidden());
            StairAtEndConvexIn3();
            T("StairAtEndConvexIn4 " + ShowForbidden());
            StairAtEndConvexIn4();
            T("StairAtEndConvexStraight3 " + ShowForbidden());
            StairAtEndConvexStraight3();
            T("StairAtEndConvexStraight4 " + ShowForbidden());
            StairAtEndConvexStraight4();
            T("StairAtEndConvexOut4 " + ShowForbidden());
            StairAtEndConvexOut4(); // 2025_0525_1

            T("StairAtEndConcaveIn2 " + ShowForbidden());
            StairAtEndConcaveIn2();
            T("StairAtEndConcaveIn3 " + ShowForbidden());
            StairAtEndConcaveIn3();
            T("StairAtEndConcaveIn4 " + ShowForbidden());
            StairAtEndConcaveIn4(); // 2024_0814, ...
            T("StairAtEndConcaveIn5 " + ShowForbidden());
            StairAtEndConcaveIn5(); // 2024_0714
            T("StairAtEndConcaveStraight3 " + ShowForbidden());
            StairAtEndConcaveStraight3(); // 2025_0527, 2025_0527_1 open corner
            T("StairAtEndConcaveStraight4 " + ShowForbidden());
            StairAtEndConcaveStraight4();
            T("StairAtEndConcaveStraight5 " + ShowForbidden());
            StairAtEndConcaveStraight5(); // 2025_0525
            T("StairAtEndConcaveStraight6 " + ShowForbidden());
            StairAtEndConcaveStraight6(); // 2025_0525
            T("StairAtEndConcaveOut3 " + ShowForbidden());
            StairAtEndConcaveOut3(); // 2024_0811
            T("StairAtEndConcaveOut5 " + ShowForbidden());
            StairAtEndConcaveOut5(); // 2026_0304_2, 2026_0304_6

            T("DoubleStair " + ShowForbidden());
            DoubleStair();
            T("DoubleStairReversed " + ShowForbidden());
            DoubleStairReversed();
            T("StairAtEnd3Obtacles1 " + ShowForbidden());
            StairAtEnd3Obtacles1(); // 2024_0725_4, 2024_0731_1
            T("Stair3x3 " + ShowForbidden());
            Stair3x3();
            T("RemoteStair " + ShowForbidden());
            RemoteStair();
            T("Sequence " + ShowForbidden());
            Sequence();
        }

        void CheckCShapeNext() // 2024_0611_5, 2024_0611_6, 2024_0611_7, 2024_0611_8
        {
            for (int i = 0; i < 2; i++)
            {
                for (int j = 0; j < 4; j++)
                {
                    if (j != 2 && !InTakenRel(1, 1) && (InTakenRel(2, 1) || InBorderRel(2, 1)) && InTakenRel(1, 0))
                    {
                        if (i == 0)
                        {
                            if (nextStepEnterLeft == -1)
                            {
                                nextStepEnterLeft = j;
                            }
                            else if (nextStepEnterLeft == 3 && (j == 0 || j == 1))
                            {
                                nextStepEnterLeft = j;
                            }
                            else if (nextStepEnterLeft == 0 && j == 1)
                            {
                                nextStepEnterLeft = j;
                            }
                        }
                        else
                        {
                            if (nextStepEnterRight == -1)
                            {
                                nextStepEnterRight = j;
                            }
                            else if (nextStepEnterRight == 3 && (j == 0 || j == 1))
                            {
                                nextStepEnterRight = j;
                            }
                            else if (nextStepEnterRight == 0 && j == 1)
                            {
                                nextStepEnterRight = j;
                            }
                        }
                    }

                    // rotate CW
                    int s0 = sx;
                    int s1 = sy;
                    sx = -lx;
                    sy = -ly;
                    lx = s0;
                    ly = s1;
                }
                sx = thisSx;
                sy = thisSy;
                lx = -thisLx;
                ly = -thisLy;
            }
            sx = thisSx;
            sy = thisSy;
            lx = thisLx;
            ly = thisLy;
        }

        void CheckStraight()
        // 7_266: Close straight 
        {
            for (int i = 0; i < 2; i++)
            {
                bool circleDirectionLeft = (i == 0) ? true : false;

                for (int j = 0; j < 3; j++) // j = 1: small area, j = 2: big area
                {
                    bool circleValid = false;
                    int dist = 1;
                    List<int[]> borderFields = new();

                    while (!InTakenRel(0, dist) && !InBorderRel(0, dist))
                    {
                        dist++;
                    }

                    int ex = dist - 1;

                    // if (-1, dist - 1) was border, all the remaining fields will be in the area. 
                    // If that field is taken, a big corner case will be true
                    // But we need circleValid, for 2 distance, in case CheckNearField is not called first.

                    if (dist == 2 || dist > 2 && !InBorderRel(-1, dist - 1))
                    {
                        if (InBorderRel(0, dist))
                        {
                            int i1 = InBorderIndexRel(0, dist);
                            int i2 = InBorderIndexRel(1, dist);

                            if (i1 > i2)
                            {
                                circleValid = true;
                            }
                        }
                        else
                        {
                            int i1 = InTakenIndexRel(0, dist);
                            int i2 = InTakenIndexRel(1, dist);

                            if (i2 != -1)
                            {
                                if (i2 > i1)
                                {
                                    circleValid = true;

                                }
                            }
                            else
                            {
                                i2 = InTakenIndexRel(-1, dist);
                                if (i1 > i2)
                                {
                                    circleValid = true;
                                }
                            }
                        }

                        if (circleValid)
                        {
                            // Not actual with CheckNearField being applied at first.
                            if (ex == 1) // close straight or C-shape up
                            {
                                T("Close straight", i, j);
                                AddForbidden(-1, 0);
                                // not a C-shape
                                if (!(InTakenRel(1, 1) || InBorderRel(1, 1)))
                                {
                                    AddForbidden(0, 1);
                                }
                                else
                                {
                                    // C-shape left
                                    if (j == 1)
                                    {
                                        AddForbidden(0, -1);
                                    }

                                }

                                // only one option remains
                                sx = thisSx;
                                sy = thisSy;
                                lx = thisLx;
                                ly = thisLy;
                                return;
                            }
                            else
                            {
                                if (ex > 2)
                                {
                                    for (int k = ex - 1; k >= 2; k--)
                                    {
                                        borderFields.Add(new int[] { 0, k });
                                    }
                                }

                                ResetExamAreas();

                                if (CountAreaRel(0, 1, 0, ex, borderFields, circleDirectionLeft, 3, true))
                                {
                                    int black = (int)info[1];
                                    int white = (int)info[2];

                                    int whiteDiff = white - black;
                                    int nowWCount = 0;
                                    int nowWCountLeft = 0;
                                    int nowBCount = 0;
                                    int nowBCountLeft = 0;
                                    int laterWCount = 0;
                                    int laterBCount = 0;

                                    bool ruleTrue = false;

                                    switch (ex % 4)
                                    {
                                        case 0:
                                            nowWCountLeft = nowWCount = ex / 4;
                                            nowBCountLeft = nowBCount = ex / 4 - 1;
                                            laterWCount = ex / 4;
                                            laterBCount = ex / 4;
                                            break;
                                        case 1:
                                            nowWCountLeft = nowWCount = (ex + 3) / 4;
                                            nowBCountLeft = nowBCount = (ex - 5) / 4;
                                            laterWCount = (ex - 1) / 4;
                                            laterBCount = (ex - 5) / 4; // At 5 distance, there are 3 white and 2 black fields on the border. A black to black line is not possible.

                                            // In rotation 1, the rule is not getting activated, because close straight in rotation 0 returns.
                                            if (j < 2 && whiteDiff == nowWCount) // 2024_0715
                                            {
                                                if (CheckNearFieldSmallRel0(0, 2, 1, 1, false))
                                                {
                                                    ruleTrue = true;
                                                    T("CheckStraight % 4 = 1 start obstacle: Cannot step straight");
                                                    AddForbidden(0, 1);
                                                }
                                            }
                                            break;
                                        case 2:
                                            nowWCount = (ex - 2) / 4; // At 6 distance, if we step straight and exit, the 5 distance situation remain with 3 black and 2 white fields. Another white to white line is not possible. 2024_0610_6
                                            nowWCountLeft = (ex + 2) / 4;
                                            nowBCountLeft = nowBCount = (ex - 2) / 4;
                                            laterWCount = (ex - 2) / 4;
                                            laterBCount = (ex - 2) / 4;
                                            break;
                                        case 3:
                                            nowWCountLeft = nowWCount = (ex + 1) / 4;
                                            nowBCountLeft = (ex - 7) / 4;
                                            nowBCount = (ex - 3) / 4;
                                            laterWCount = (ex + 1) / 4;
                                            laterBCount = (ex - 3) / 4;
                                            break;
                                    }

                                    // T(black, white, nowWCount, nowBCount, nowWCountLeft, nowBCountLeft, laterWCount, laterBCount);

                                    if (!(whiteDiff <= nowWCount && whiteDiff >= -nowBCount))
                                    {
                                        ruleTrue = true;
                                        T("Straight " + i + " " + j + ": Cannot enter now up");
                                        AddForbidden(0, 1);
                                    }
                                    if (!(whiteDiff <= nowWCountLeft && whiteDiff >= -nowBCountLeft) && j != 1)  // for left rotation, lx, ly is the down field
                                    {
                                        ruleTrue = true;
                                        T("Straight " + i + " " + j + ": Cannot enter now left");
                                        AddForbidden(1, 0);
                                        if (j == 2)
                                        {
                                            AddForbidden(0, -1);
                                        }
                                    }
                                    if (!(whiteDiff <= laterWCount && whiteDiff >= -laterBCount) && j != 2)
                                    {
                                        ruleTrue = true;
                                        T("Straight " + i + " " + j + ": Cannot enter later");
                                        AddForbidden(-1, 0);
                                        if (j == 1)
                                        {
                                            AddForbidden(0, -1);
                                        }
                                    }

                                    if (ruleTrue)
                                    {
                                        AddExamAreas();
                                    }
                                }
                            }
                        }
                    }

                    if (j == 0) // rotate down (CCW): small area
                    {
                        int l0 = lx;
                        int l1 = ly;
                        lx = -sx;
                        ly = -sy;
                        sx = l0;
                        sy = l1;
                    }
                    else if (j == 1) // rotate up (CW): big area
                    {
                        lx = -lx;
                        ly = -ly;
                        sx = -sx;
                        sy = -sy;
                    }
                }
                sx = thisSx;
                sy = thisSy;
                lx = -thisLx;
                ly = -thisLy;
            }
            sx = thisSx;
            sy = thisSy;
            lx = thisLx;
            ly = thisLy;
        }

        void CheckLeftRightAreaUp()
        // 7_3627: Close mid across
        // 7_2558: Close mid across big
        {
            for (int i = 0; i < 2; i++)
            {
                for (int j = 0; j < 4; j++) // rotate CW, j = 1: big area, j = 3: small area
                {
                    if (j != 2)
                    {
                        int dist = size;
                        int quarter = quarters[i][j];

                        foreach (int[] corner in closedCorners[quarter])
                        {
                            // find closest areaUp corner
                            if (j == 0 && corner[0] == 1)
                            {
                                if (corner[1] < dist) dist = corner[1];
                            }
                            else if (j % 2 == 1 && corner[1] == 1)
                            {
                                if (corner[0] < dist) dist = corner[0];
                            }
                        }

                        if (dist < size)
                        {
                            T("AreaUp distance " + (dist - 1), "side " + i, "rotation " + j);

                            bool distanceEmpty = true;
                            for (int k = 1; k <= dist - 1; k++)
                            {
                                if (InTakenRel(0, k) || InTakenRel(1, k)) distanceEmpty = false;
                            }

                            if (distanceEmpty)
                            {
                                int i1 = InTakenIndexRel(1, dist);
                                int i2 = InTakenIndexRel(2, dist);

                                if (i2 > i1) // small area
                                {
                                    bool circleDirectionLeft = (i == 0) ? true : false;
                                    List<int[]> borderFields = new();
                                    int ex = dist - 1;

                                    // Not actual with CheckNearField being applied at first.
                                    if (ex == 1) // close mid across
                                    {
                                        T("Close mid across", i, j);
                                        AddForbidden(0, 1);
                                        if (j == 0)
                                        {
                                            AddForbidden(-1, 0);
                                        }

                                        // only one option remains, but we do not return in case of 2024_0623 where the area would close, and at the end, the number of steps are less than size * size.
                                        /*sx = thisSx;
                                        sy = thisSy;
                                        lx = thisLx;
                                        ly = thisLy;
                                        return;*/
                                    }
                                    else
                                    {
                                        if (ex > 2)
                                        {
                                            for (int k = ex - 1; k >= 2; k--)
                                            {
                                                borderFields.Add(new int[] { 1, k });
                                            }
                                        }

                                        ResetExamAreas();

                                        if (CountAreaRel(1, 1, 1, ex, borderFields, circleDirectionLeft, 2, true))
                                        {
                                            int black = (int)info[1];
                                            int white = (int)info[2];

                                            int whiteDiff = white - black;
                                            int nowWCount = 0;
                                            int nowBCount = 0;
                                            int laterWCount = 0;
                                            int laterBCount = 0;

                                            bool ruleTrue = false;

                                            switch (ex % 4)
                                            {
                                                case 0:
                                                    nowWCount = ex / 4;
                                                    nowBCount = ex / 4 - 1;
                                                    laterWCount = ex / 4;
                                                    laterBCount = ex / 4;
                                                    break;
                                                case 1:
                                                    nowWCount = (ex - 1) / 4;
                                                    nowBCount = (ex - 1) / 4;
                                                    laterWCount = (ex - 1) / 4;
                                                    laterBCount = (ex - 1) / 4;
                                                    break;
                                                case 2:
                                                    nowWCount = (ex + 2) / 4;
                                                    nowBCount = (ex - 2) / 4;
                                                    laterWCount = (ex - 2) / 4;
                                                    laterBCount = (ex - 2) / 4;
                                                    break;
                                                case 3:
                                                    nowWCount = (ex + 1) / 4;
                                                    nowBCount = (ex - 3) / 4;
                                                    laterWCount = (ex - 3) / 4;
                                                    laterBCount = (ex + 1) / 4;
                                                    break;
                                            }

                                            if (!(whiteDiff <= nowWCount && whiteDiff >= -nowBCount))
                                            {
                                                if (j != 3) // no small small area
                                                {
                                                    if (AddForbidden(1, 0))
                                                    {
                                                        ruleTrue = true;
                                                        T("LeftRightAreaUp: Cannot enter now left");
                                                        if (j == 1)
                                                        {
                                                            T("LeftRightAreaUp: Cannot enter now down");
                                                            AddForbidden(0, -1);
                                                        }
                                                    }
                                                }
                                            }
                                            if (!(whiteDiff <= laterWCount && whiteDiff >= -laterBCount))
                                            {
                                                ruleTrue = true;
                                                T("LeftRightAreaUp: Cannot enter later");
                                                AddForbidden(0, 1);
                                                AddForbidden(-1, 0);
                                            }
                                            else if (j != 2) // We can enter later, check for start C on the opposite side (if the obstacle is up on the left, we check the straight field for next step C, not the right field.) 
                                            // 9_466
                                            {
                                                if (ex == 2)
                                                {
                                                    if (i == 0)
                                                    {
                                                        if (nextStepEnterLeft == -1)
                                                        {
                                                            nextStepEnterLeft = j;
                                                        }
                                                        else if (nextStepEnterLeft == 3 && (j == 0 || j == 1))
                                                        {
                                                            nextStepEnterLeft = j;
                                                        }
                                                        else if (nextStepEnterLeft == 0 && j == 1)
                                                        {
                                                            nextStepEnterLeft = j;
                                                        }
                                                    }
                                                    else
                                                    {
                                                        if (nextStepEnterRight == -1)
                                                        {
                                                            nextStepEnterRight = j;
                                                        }
                                                        else if (nextStepEnterRight == 3 && (j == 0 || j == 1))
                                                        {
                                                            nextStepEnterRight = j;
                                                        }
                                                        else if (nextStepEnterRight == 0 && j == 1)
                                                        {
                                                            nextStepEnterRight = j;
                                                        }
                                                    }
                                                }
                                            }

                                            if (ruleTrue)
                                            {
                                                AddExamAreas();
                                            }
                                        }
                                    }
                                }
                                else // big area
                                {
                                    bool circleDirectionLeft = (i == 0) ? false : true;
                                    List<int[]> borderFields = new();
                                    int ex = dist - 1;

                                    // Not actual with CheckNearField being applied at first.
                                    if (ex == 1) // close mid across big
                                    {
                                        T("Close mid across big", i, j);
                                        AddForbidden(0, 1);
                                        if (j == 0)
                                        {
                                            AddForbidden(1, 0);
                                        }

                                        // only one option remains
                                        /*sx = thisSx;
                                        sy = thisSy;
                                        lx = thisLx;
                                        ly = thisLy;
                                        return;*/
                                    }
                                    else
                                    {
                                        if (ex > 2)
                                        {
                                            for (int k = ex - 1; k >= 2; k--)
                                            {
                                                borderFields.Add(new int[] { 0, k });
                                            }
                                        }

                                        ResetExamAreas();

                                        if (CountAreaRel(0, 1, 0, ex, borderFields, circleDirectionLeft, 3, true))
                                        {
                                            int black = (int)info[1];
                                            int white = (int)info[2];

                                            int whiteDiff = white - black;
                                            int nowWCount = 0;
                                            int nowWCountRight = 0;
                                            int nowBCount = 0;
                                            int laterWCount = 0;
                                            int laterBCount = 0;

                                            switch (ex % 4)
                                            {
                                                case 0:
                                                    nowWCountRight = nowWCount = ex / 4;
                                                    nowBCount = ex / 4 - 1;
                                                    laterWCount = ex / 4;
                                                    laterBCount = ex / 4;
                                                    break;
                                                case 1:
                                                    nowWCountRight = nowWCount = (ex + 3) / 4;
                                                    nowBCount = (ex - 5) / 4;
                                                    laterWCount = (ex - 1) / 4;
                                                    laterBCount = (ex - 1) / 4;
                                                    break;
                                                case 2:
                                                    if (ex == 2)
                                                    {
                                                        nowWCountRight = 1;
                                                        nowWCount = 0;
                                                    }
                                                    else
                                                    {
                                                        nowWCountRight = nowWCount = (ex + 2) / 4;
                                                        nowBCount = (ex - 2) / 4;
                                                        laterWCount = (ex - 2) / 4;
                                                        laterBCount = (ex - 2) / 4;
                                                    }
                                                    break;
                                                case 3:
                                                    nowWCountRight = nowWCount = (ex + 1) / 4;
                                                    nowBCount = (ex - 3) / 4;
                                                    laterWCount = (ex + 1) / 4;
                                                    laterBCount = (ex - 3) / 4;
                                                    break;
                                            }

                                            bool ruleTrue = false;

                                            if (!(whiteDiff <= nowWCount && whiteDiff >= -nowBCount)) // not in range
                                            {
                                                ruleTrue = true;
                                                T("LeftRightAreaUpBig: Cannot enter now up");
                                                AddForbidden(0, 1);
                                            }
                                            if (!(whiteDiff <= nowWCountRight && whiteDiff >= -nowBCount)) // not in range
                                            {
                                                ruleTrue = true;
                                                T("LeftRightAreaUpBig: Cannot enter now right");
                                                AddForbidden(-1, 0);
                                            }
                                            if (!(whiteDiff <= laterWCount && whiteDiff >= -laterBCount))
                                            {
                                                ruleTrue = true;
                                                T("LeftRightAreaUpBig: Cannot enter later");
                                                AddForbidden(1, 0);
                                            }

                                            if (ruleTrue)
                                            {
                                                AddExamAreas();
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }

                    // rotate CW
                    int s0 = sx;
                    int s1 = sy;
                    sx = -lx;
                    sy = -ly;
                    lx = s0;
                    ly = s1;
                }
                sx = thisSx;
                sy = thisSy;
                lx = -thisLx;
                ly = -thisLy;
            }
            sx = thisSx;
            sy = thisSy;
            lx = thisLx;
            ly = thisLy;
        }

        void CheckLeftRightCorner()
        {
            for (int i = 0; i < 2; i++)
            {
                bool circleDirectionLeft = (i == 0) ? true : false;

                for (int j = 0; j < 4; j++) // rotate CW
                {
                    int quarter = quarters[i][j];
                    foreach (int[] corner in closedCorners[quarter])
                    {
                        int hori = j % 2 == 0 ? corner[0] : corner[1];
                        int vert = j % 2 == 0 ? corner[1] : corner[0];

                        if (!(hori == 1 || vert == 1)) // this case is handled in AreaUp
                        {
                            T("Corner at " + hori, vert, "side " + i, "rotation " + j);

                            int i1 = InTakenIndexRel(hori, vert);
                            int i2 = InTakenIndexRel(hori + 1, vert);

                            if (i2 > i1)
                            {
                                if (hori == 2 && vert == 2) // close across, small if j = 0, big if j = 1
                                {
                                    AddForbidden(0, 1);
                                    if (j == 0) // close across small
                                    {
                                        T("Close across small", i);
                                        AddForbidden(-1, 0);

                                        // only one option remains
                                        sx = thisSx;
                                        sy = thisSy;
                                        lx = thisLx;
                                        ly = thisLy;
                                        return;
                                    }
                                    else if (j == 1)
                                    {
                                        T("Close across big", i);
                                    }
                                }
                                else
                                {
                                    bool takenFound = false;
                                    int left1 = 1;
                                    int straight1 = 1;
                                    int left2 = hori - 1;
                                    int straight2 = vert - 1;
                                    List<int[]> borderFields = new();

                                    int nowWCount, nowWCountDown, nowBCount, laterWCount, laterBCount;
                                    int a, n;

                                    //check if all fields on the border line is free
                                    if (vert == hori)
                                    {
                                        a = hori - 1;
                                        nowWCountDown = nowWCount = 0;
                                        nowBCount = a - 1;
                                        laterWCount = -1;// means B = 1
                                        laterBCount = a - 1;

                                        for (int k = 1; k < hori; k++)
                                        {
                                            if (k < hori - 1)
                                            {
                                                if (InTakenRel(k, k) || InTakenRel(k + 1, k))
                                                {
                                                    takenFound = true;
                                                    break;
                                                }
                                            }
                                            else
                                            {
                                                if (InTakenRel(k, k))
                                                {
                                                    takenFound = true;
                                                    break;
                                                }
                                            }

                                            if (k == 1)
                                            {
                                                borderFields.Add(new int[] { 2, 1 });
                                            }
                                            else if (k < hori - 1)
                                            {
                                                borderFields.Add(new int[] { k, k });
                                                borderFields.Add(new int[] { k + 1, k });
                                            }
                                        }
                                    }
                                    else if (hori > vert)
                                    {
                                        a = vert - 1;
                                        n = (hori - vert - (hori - vert) % 2) / 2;

                                        if ((hori - vert) % 2 == 0)
                                        {
                                            if (n > 1)
                                            {
                                                nowWCountDown = nowWCount = (n + 1 - (n + 1) % 2) / 2;
                                            }
                                            else
                                            {
                                                nowWCount = 0;
                                                nowWCountDown = 1;
                                            }
                                            nowBCount = a + (n - 1 - (n - 1) % 2) / 2;
                                            laterWCount = (n - n % 2) / 2;
                                            laterBCount = a + (n - n % 2) / 2;
                                        }
                                        else
                                        {
                                            if (n > 0)
                                            {
                                                nowWCountDown = nowWCount = a + (n - n % 2) / 2;
                                                laterBCount = (n + 2 - (n + 2) % 2) / 2;
                                            }
                                            else
                                            {
                                                nowWCount = a - 1;
                                                nowWCountDown = a;
                                                laterBCount = 0;
                                            }
                                            nowBCount = (n + 1 - (n + 1) % 2) / 2;
                                            laterWCount = a - 1 + (n + 1 - (n + 1) % 2) / 2;

                                        }

                                        for (int k = 1; k <= hori - vert; k++)
                                        {
                                            if (InTakenRel(k, 1))
                                            {
                                                takenFound = true;
                                                break;
                                            }

                                            if (k > 1)
                                            {
                                                borderFields.Add(new int[] { k, 1 });
                                            }
                                        }

                                        for (int k = 1; k < vert; k++)
                                        {
                                            if (k < vert - 1)
                                            {
                                                if (InTakenRel(hori - vert + k, k) || InTakenRel(hori - vert + k + 1, k))
                                                {
                                                    takenFound = true;
                                                    break;
                                                }
                                            }
                                            else
                                            {
                                                if (InTakenRel(hori - vert + k, k))
                                                {
                                                    takenFound = true;
                                                    break;
                                                }
                                            }

                                            if (k < vert - 1)
                                            {
                                                borderFields.Add(new int[] { hori - vert + k, k });
                                                borderFields.Add(new int[] { hori - vert + k + 1, k });
                                            }
                                        }
                                    }
                                    else // vert > hori
                                    {
                                        a = hori - 1;
                                        n = (vert - hori - (vert - hori) % 2) / 2;

                                        if ((vert - hori) % 2 == 0)
                                        {
                                            nowWCountDown = nowWCount = (n + 1 - (n + 1) % 2) / 2;
                                            nowBCount = a + (n - 1 - (n - 1) % 2) / 2;
                                            laterWCount = (n - n % 2) / 2;
                                            laterBCount = a + (n - n % 2) / 2;
                                        }
                                        else
                                        {
                                            nowWCountDown = nowWCount = 1 + (n + 1 - (n + 1) % 2) / 2;
                                            nowBCount = a - 1 + (n - n % 2) / 2;
                                            if (n > 0)
                                            {
                                                laterWCount = 1 + (n - n % 2) / 2;
                                            }
                                            else
                                            {
                                                laterWCount = 0;
                                            }
                                            laterBCount = a - 1 + (n + 1 - (n + 1) % 2) / 2;
                                        }

                                        for (int k = 1; k < hori; k++)
                                        {
                                            if (k < hori - 1 && hori > 2)
                                            {
                                                if (InTakenRel(k, k) || InTakenRel(k + 1, k))
                                                {
                                                    takenFound = true;
                                                    break;
                                                }
                                            }
                                            else
                                            {
                                                if (InTakenRel(k, k))
                                                {
                                                    takenFound = true;
                                                    break;
                                                }
                                            }

                                            if (hori > 2) // there is no stair if corner is at 1 distance, only one field which is the start field.
                                            {
                                                if (k == 1)
                                                {
                                                    borderFields.Add(new int[] { 2, 1 });
                                                }
                                                else if (k < hori - 1)
                                                {
                                                    borderFields.Add(new int[] { k, k });
                                                    borderFields.Add(new int[] { k + 1, k });
                                                }
                                                else
                                                {
                                                    borderFields.Add(new int[] { k, k });
                                                }
                                            }
                                        }

                                        for (int k = 1; k <= vert - hori; k++)
                                        {
                                            if (InTakenRel(hori - 1, hori - 1 + k))
                                            {
                                                takenFound = true;
                                                break;
                                            }

                                            if (k < vert - hori)
                                            {
                                                borderFields.Add(new int[] { hori - 1, hori - 1 + k });
                                            }
                                        }
                                    }

                                    if (!takenFound)
                                    {
                                        // reverse order
                                        List<int[]> newBorderFields = new();
                                        for (int k = borderFields.Count - 1; k >= 0; k--)
                                        {
                                            newBorderFields.Add(borderFields[k]);
                                        }

                                        ResetExamAreas();

                                        // here, true means that count area succeeds, does not run into an error
                                        if (CountAreaRel(left1, straight1, left2, straight2, newBorderFields, circleDirectionLeft, 2, true))
                                        {
                                            int black = (int)info[1];
                                            int white = (int)info[2];

                                            int whiteDiff = white - black;

                                            bool ruleTrue = false;

                                            // need to be generalized for larger than 1 vertical distance
                                            if (hori == 2)
                                            {
                                                if (vert % 4 == 3 && j < 2) // 2024_0610, 2024_0610_1, 2024_0625_1, 2024_0611_2 (21 cutout)
                                                {
                                                    if (-whiteDiff == (vert - 3) / 4)
                                                    {
                                                        if (CheckCorner(1, 2, 0, 2, circleDirectionLeft, true))
                                                        {
                                                            ruleTrue = true;
                                                            T("LeftRightCorner closed corner 2, 3: Cannot step left");
                                                            AddForbidden(1, 0);
                                                            if (j == 1) // big area
                                                            {
                                                                T("LeftRightCorner closed corner 2, 3: Cannot step down");
                                                                AddForbidden(0, -1);
                                                            }
                                                        }
                                                    }
                                                }

                                                else if (vert % 4 == 0 && j <= 1)  // 9_743059_1, 2024_0610_2, 2024_0610_3, 2024_0625
                                                // These above cases are solved by the y = x + 2 return stair pattern too. But this algorithm can be applied to a straight extension as well.
                                                {
                                                    if (-whiteDiff == vert / 4)
                                                    {
                                                        // Add field so that a second circle can be drawn
                                                        path.Add(new int[] { x + 2 * lx + (vert - 1) * sx, y + 2 * ly + (vert - 1) * sy });

                                                        if (CheckCorner(1, vert - 2, 0, 2, circleDirectionLeft, true))
                                                        {
                                                            path.RemoveAt(path.Count - 1);
                                                            ruleTrue = true;
                                                            T("LeftRightCorner open corner 2, 4: Cannot step left");
                                                            AddForbidden(1, 0);
                                                            if (j == 1)
                                                            {
                                                                T("LeftRightCorner open corner 2, 4: Cannot step down");
                                                                AddForbidden(0, -1);
                                                            }
                                                        }
                                                        else
                                                        {
                                                            path.RemoveAt(path.Count - 1);
                                                        }

                                                        /*
                                                        // 2024_0726, sequence on right side

                                                        ResetExamAreas();

                                                        counterrec = 0;

                                                        lx2 = -lx2;
                                                        ly2 = -ly2;
                                                        if (CheckSequenceRecursive(1 - i))
                                                        {
                                                            AddExamAreas(true);

                                                            T("Corner 2 4 Sequence at " + x + " " + y + ", stop at " + x2 + " " + y2 + ": Cannot step left");
                                                            AddForbidden(1, 0);
                                                            if (j == 1)
                                                            {
                                                                T("Corner 2 4 Sequence: Cannot step down");
                                                                AddForbidden(0, -1);
                                                            }
                                                        }*/
                                                    }
                                                }
                                            }
                                            else if (vert == 2)
                                            {
                                                if (hori % 4 == 0 && j < 2 && -whiteDiff == hori / 4)
                                                {
                                                    // 2024_0720_3: mid across, 2024_0725_1: across
                                                    // Find example for area
                                                    if (CheckNearFieldSmallRel1(hori - 2, 1, 1, 0, true))
                                                    {
                                                        ruleTrue = true;
                                                        T("LeftRightCorner 4 2 1B: Cannot step left");
                                                        AddForbidden(1, 0);
                                                        if (j == 1)
                                                        {
                                                            T("LeftRightCorner 4 2 1B: Cannot step down");
                                                            AddForbidden(0, -1);
                                                        }
                                                    }

                                                    /*
                                                    // 2024_0711, sequence on left side
                                                    path.Add(new int[] { x + (hori - 1) * lx + sx, y + (hori - 1) * ly + sy });

                                                    x2 = x + (hori - 1) * lx + sx;
                                                    y2 = y + (hori - 1) * ly + sy;

                                                    lx2 = lx;
                                                    ly2 = ly;
                                                    sx2 = sx;
                                                    sy2 = sy;

                                                    ResetExamAreas();

                                                    counterrec = 0;

                                                    if (CheckSequenceRecursive(i)) // 2024_0711
                                                    {
                                                        path.RemoveAt(path.Count - 1);

                                                        AddExamAreas(true);

                                                        T("Corner 4 2 Sequence at " + x + " " + y + ", stop at " + x2 + " " + y2 + ": Cannot step left");
                                                        AddForbidden(1, 0);
                                                        if (j == 1)
                                                        {
                                                            T("Corner 4 2 Sequence: Cannot step down");
                                                            AddForbidden(0, -1);
                                                        }
                                                    }
                                                    else
                                                    {
                                                        path.RemoveAt(path.Count - 1);
                                                    }
                                                    */
                                                }

                                                // 2024_0727_1: mid across
                                                if (hori % 4 == 2 && j < 2 && whiteDiff == (hori - 2) / 4 && CheckNearFieldSmallRel0(2, 2, 1, 0, false))
                                                {
                                                    ruleTrue = true;
                                                    T("Corner hori % 4 = 2 vert 2 start obstacle: Cannot step straight");
                                                    AddForbidden(0, 1);
                                                    if (j == 0)
                                                    {
                                                        T("Corner hori % 4 = 2 vert 2 start obstacle: Cannot step right");
                                                        AddForbidden(-1, 0);
                                                    }
                                                }
                                            }


                                            // Stair extensions: 2, 3 or 4 fields on the top near the live end
                                            if (vert == hori + 1 && -whiteDiff == hori - 2 && j <= 1) // 2024_0712
                                            {
                                                int m;
                                                for (m = hori - 1; m >= 2; m--)
                                                {
                                                    path.Add(new int[] { x + m * lx + (m + 1) * sx, y + m * ly + (m + 1) * sy });
                                                }

                                                if (CheckCorner(1, 2, 0, 2, circleDirectionLeft, true))
                                                {
                                                    for (m = hori - 1; m >= 2; m--)
                                                    {
                                                        path.RemoveAt(path.Count - 1);
                                                    }

                                                    AddExamAreas();
                                                    T("Corner y = x + 1 return stair close obstacle: Cannot step left");
                                                    AddForbidden(1, 0);
                                                    if (j == 1)
                                                    {
                                                        T("Corner y = x + 1 return stair close obstacle: Cannot step down");
                                                        AddForbidden(0, -1);
                                                    }
                                                }
                                                else
                                                {
                                                    for (m = hori - 1; m >= 2; m--)
                                                    {
                                                        path.RemoveAt(path.Count - 1);
                                                    }
                                                }
                                            }

                                            if (vert == hori + 2 && -whiteDiff == hori - 1 && j <= 1) // Close mid across: 9_743059_1, 2024_0610_2, 2024_0610_3; Close across: 2024_0716_1, Area: 2024_0625, 2024_0720_1
                                            // stair entered from side
                                            // obstacle at any point of the return step?
                                            {
                                                int m;
                                                for (m = hori; m >= 2; m--)
                                                {
                                                    path.Add(new int[] { x + m * lx + (m + 1) * sx, y + m * ly + (m + 1) * sy });
                                                }

                                                if (CheckCorner(1, 2, 0, 2, circleDirectionLeft, true))
                                                {
                                                    for (m = hori; m >= 2; m--)
                                                    {
                                                        path.RemoveAt(path.Count - 1);
                                                    }

                                                    AddExamAreas();
                                                    T("Corner y = x + 2 return stair close obstacle: Cannot step left");
                                                    AddForbidden(1, 0);
                                                    if (j == 1)
                                                    {
                                                        T("Corner y = x + 2 return stair close obstacle: Cannot step down");
                                                        AddForbidden(0, -1);
                                                    }
                                                }
                                                else
                                                {
                                                    for (m = hori; m >= 2; m--)
                                                    {
                                                        path.RemoveAt(path.Count - 1);
                                                    }
                                                }
                                            }

                                            if (vert == hori + 3 && -whiteDiff == hori - 1 && j == 3) // 2024_0717, 2024_0717_3 (far obstacle)
                                                                                                      // stair entered from below
                                            {
                                                int m;
                                                for (m = hori - 1; m >= 1; m--)
                                                {
                                                    path.Add(new int[] { x + m * lx + (m + 3) * sx, y + m * ly + (m + 3) * sy });
                                                }

                                                if (CheckCorner(0, 3, 0, 2, circleDirectionLeft, true))
                                                {
                                                    for (m = hori - 1; m >= 1; m--)
                                                    {
                                                        path.RemoveAt(path.Count - 1);
                                                    }

                                                    ruleTrue = true;
                                                    T("Corner y = x + 3 return stair second obstacle: Cannot step straight");
                                                    AddForbidden(0, 1);
                                                }
                                                else
                                                {
                                                    for (m = hori - 1; m >= 1; m--)
                                                    {
                                                        path.RemoveAt(path.Count - 1);
                                                    }
                                                }
                                            }

                                            // Stair extensions: flat top far away
                                            // Does example with 2 or 3 fields on top exist? It does not look like it, because then the area could not be filled.
                                            if (hori == vert + 3 && -whiteDiff == 1) // 2024_0725_7, corner 2 5 stair (shows large area)
                                            {
                                                int m;
                                                for (m = 1; m <= vert; m++)
                                                {
                                                    path.Add(new int[] { x + m * lx + (m - 1) * sx, y + m * ly + (m - 1) * sy });
                                                }
                                                m--;

                                                if (CheckNearFieldSmallRel1(hori - 2, vert, 1, 0, true))
                                                // Example needed
                                                // if (CheckCorner(hori - 2, vert, 1, 0, circleDirectionLeft, true))
                                                {
                                                    for (m = 1; m <= vert; m++)
                                                    {
                                                        path.RemoveAt(path.Count - 1);
                                                    }

                                                    ruleTrue = true;
                                                    T("Corner x = y + 3 up left stair second obstacle: Cannot step left");
                                                    AddForbidden(1, 0);
                                                    if (j == 1)
                                                    {
                                                        T("Corner x = y + 3 up left stair second obstacle: Cannot step down");
                                                        AddForbidden(0, -1);
                                                    }
                                                }
                                                else
                                                {
                                                    for (m = 1; m <= vert; m++)
                                                    {
                                                        path.RemoveAt(path.Count - 1);
                                                    }
                                                }
                                            }

                                            // 2025_0522: Area has a maximum allowed of white fields. There is an across obstacle at the bottom of the stair edge
                                            if ((hori - vert) % 4 == 3 && whiteDiff == laterWCount && CheckNearFieldSmallRel1(hori - vert + 1, 1, 0, 1, true))
                                            {
                                                T("LeftRightCorner close obstacle inside " + i + " " + j + ": Cannot enter later");

                                                // AddExamAreas();

                                                AddForbidden(0, 1);
                                                // for small area
                                                if (j == 0)
                                                {
                                                    AddForbidden(-1, 0);
                                                }
                                            }

                                            // 2025_0527_2: close obstacles inside or outside
                                            // j = 2 rotation might be possible, but we need an example of it
                                            if (hori == vert && hori >= 4 && j == 1 && whiteDiff == 0 && CheckNearFieldSmallRel0(1, 0, 0, 1, true) && CheckNearFieldSmallRel0(2, 2, 0, 2, true))
                                            {
                                                T("LeftRightCorner close obstacle inside outside " + i + " " + j + ": Cannot step left");

                                                AddForbidden(1, 0);
                                            }

                                            if (!(whiteDiff <= nowWCount && whiteDiff >= -nowBCount) && j != 3) // for left rotation, lx, ly is the down field
                                            {
                                                ruleTrue = true;
                                                T("LeftRightCorner " + i + " " + j + ": Cannot enter now left");
                                                AddForbidden(1, 0);
                                            }
                                            if (!(whiteDiff <= nowWCountDown && whiteDiff >= -nowBCount) && j != 3)
                                            {
                                                ruleTrue = true;
                                                T("LeftRightCorner " + i + " " + j + ": Cannot enter now down");
                                                AddForbidden(0, -1);
                                            }
                                            if (!(whiteDiff <= laterWCount && whiteDiff >= -laterBCount))
                                            {
                                                ruleTrue = true;
                                                T("LeftRightCorner " + i + " " + j + ": Cannot enter later");
                                                AddForbidden(0, 1);
                                                // for small area
                                                if (j == 0)
                                                {
                                                    AddForbidden(-1, 0);
                                                }
                                            }
                                            else
                                            {
                                                if (j != 2) // We can enter later, but if we step straight, we have to enter afterwards. Check for pattern on the opposite side (if the obstacle is up on the left, we check the straight field for next step C, not the right field.) 
                                                            // When j = 2, the enter later field is the field behind.

                                                {
                                                    // 2024_0611_7, 2024_0611_8
                                                    // If we can enter later at the hori 2, vert 3 case, the area must be W = B
                                                    if (
                                                        (hori == 2 && vert == 3) ||
                                                        (hori == 2 && vert == 4 && -whiteDiff == 1) ||
                                                        (hori == 3 && vert == 4 && -whiteDiff == 1)) // 2024_0726_3
                                                    {
                                                        if (i == 0)
                                                        {
                                                            if (nextStepEnterLeft == -1)
                                                            {
                                                                nextStepEnterLeft = j;
                                                            }
                                                            else if (nextStepEnterLeft == 3 && (j == 0 || j == 1))
                                                            {
                                                                nextStepEnterLeft = j;
                                                            }
                                                            else if (nextStepEnterLeft == 0 && j == 1)
                                                            {
                                                                nextStepEnterLeft = j;
                                                            }
                                                        }
                                                        else
                                                        {
                                                            if (nextStepEnterRight == -1)
                                                            {
                                                                nextStepEnterRight = j;
                                                            }
                                                            else if (nextStepEnterRight == 3 && (j == 0 || j == 1))
                                                            {
                                                                nextStepEnterRight = j;
                                                            }
                                                            else if (nextStepEnterRight == 0 && j == 1)
                                                            {
                                                                nextStepEnterRight = j;
                                                            }
                                                        }
                                                    }
                                                }
                                            }

                                            if (ruleTrue)
                                            {
                                                AddExamAreas();
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }

                    // rotate CW
                    int s0 = sx;
                    int s1 = sy;
                    sx = -lx;
                    sy = -ly;
                    lx = s0;
                    ly = s1;
                }
                sx = thisSx;
                sy = thisSy;
                lx = -thisLx;
                ly = -thisLy;
            }
            sx = thisSx;
            sy = thisSy;
            lx = thisLx;
            ly = thisLy;
        }

        void StairAtStartConvexIn2()
        // 2024_0727_3: mid across left, across right
        // 2024_0724: across up, mid across down
        // 2024_0725_2:area up, mid across down
        {
            for (int i = 0; i < 2; i++)
            {
                bool circleDirectionLeft = (i == 0) ? true : false;

                for (int j = 0; j < 2; j++) // j = 0: straight area, j = 1: left (small) area
                {
                    int dist = size; // vertical distance
                    int j2 = (j == 0) ? 0 : 3;
                    int quarter = quarters[i][j2];

                    foreach (int[] corner in closedCorners[quarter])
                    {
                        if (j == 0 && corner[1] == corner[0] + 3)
                        {
                            if (corner[1] < dist) dist = corner[1];
                        }
                        else if (j == 1 && corner[0] == corner[1] + 3)
                        {
                            if (corner[0] < dist) dist = corner[0];
                        }
                    }

                    if (dist > 3 && dist < size)
                    {
                        T("StairAtStartConvexIn2 distance " + (dist - 1), "side " + i, "rotation " + j);

                        bool distanceEmpty = true;
                        for (int k = 1; k <= dist - 1; k++)
                        {
                            if (k < 2)
                            {
                                if (InTakenRel(-1, k)) distanceEmpty = false;
                            }
                            else
                            {
                                if (InTakenRel(k - 3, k)) distanceEmpty = false;
                            }
                        }

                        if (distanceEmpty)
                        {
                            int hori = dist - 3;
                            int vert = dist;

                            int i1 = InTakenIndexRel(hori, vert);
                            int i2 = InTakenIndexRel(hori + 1, vert);

                            if (i2 > i1)
                            {
                                List<int[]> borderFields = new();
                                for (int k = 2; k <= vert - 2; k++)
                                {
                                    borderFields.Add(new int[] { k - 3, k });
                                    borderFields.Add(new int[] { k - 2, k });
                                }

                                List<int[]> newBorderFields = new();
                                for (int k = borderFields.Count - 1; k >= 0; k--)
                                {
                                    T("border field " + borderFields[k][0] + " " + borderFields[k][1]);
                                    newBorderFields.Add(borderFields[k]);
                                }

                                ResetExamAreas();

                                if (CountAreaRel(-1, 1, hori - 1, vert - 1, newBorderFields, circleDirectionLeft, 2, true))
                                {
                                    int black = (int)info[1];
                                    int white = (int)info[2];

                                    if (black == white)
                                    {
                                        int counter = hori + 1;
                                        AddEndFar(counter, hori - 1, vert - 1);

                                        if (CheckCorner(-1, 2, 0, 2, circleDirectionLeft, true) && CheckNearFieldSmallRel1(-1, 2, 1, 1, true))
                                        {
                                            AddExamAreas();
                                            T("StairAtStartConvexIn2 at " + hori + " " + vert + ": Cannot step straight");
                                            RemoveEnd(counter);
                                            AddForbidden(0, 1);

                                            if (j == 0)
                                            {
                                                T("StairAtStartConvexIn2 at " + hori + " " + vert + ": Cannot step left");
                                                AddForbidden(1, 0);
                                            }
                                        }
                                        else
                                        {
                                            RemoveEnd(counter);
                                        }
                                    }
                                }

                            }
                        }
                    }

                    // rotate CCW
                    int l0 = lx;
                    int l1 = ly;
                    lx = -sx;
                    ly = -sy;
                    sx = l0;
                    sy = l1;
                }
                sx = thisSx;
                sy = thisSy;
                lx = -thisLx;
                ly = -thisLy;
            }
            sx = thisSx;
            sy = thisSy;
            lx = thisLx;
            ly = thisLy;
        }

        void StairAtStartConvexIn3()
        // (h+1)B
        // 2024_0725_6: mid across down, mid across up
        // 2024_0726_1: across, mid across
        // 2024_0726_2: mid across, area

        // E
        // 2026_0410: mid across, mid across


        {
            for (int i = 0; i < 2; i++)
            {
                bool circleDirectionLeft = (i == 0) ? true : false;

                for (int j = 0; j < 2; j++) // j = 0: straight area, j = 1: left (small) area
                {
                    int dist = size; // vertical distance
                    int j2 = (j == 0) ? 0 : 3;
                    int quarter = quarters[i][j2];

                    foreach (int[] corner in closedCorners[quarter])
                    {
                        if (j == 0 && corner[1] == corner[0] + 4)
                        {
                            if (corner[1] < dist) dist = corner[1];
                        }
                        else if (j == 1 && corner[0] == corner[1] + 4)
                        {
                            if (corner[0] < dist) dist = corner[0];
                        }
                    }

                    if (dist > 3 && dist < size)
                    {
                        T("StairAtStartConvexIn3 distance " + (dist - 1), "side " + i, "rotation " + j);

                        bool distanceEmpty = true;
                        for (int k = 1; k <= dist - 1; k++)
                        {
                            if (k < 3)
                            {
                                if (InTakenRel(-1, k)) distanceEmpty = false;
                            }
                            else
                            {
                                if (InTakenRel(k - 4, k)) distanceEmpty = false;
                            }
                        }

                        if (distanceEmpty)
                        {
                            int hori = dist - 4;
                            int vert = dist;

                            int i1 = InTakenIndexRel(hori, vert);
                            int i2 = InTakenIndexRel(hori + 1, vert);

                            if (i2 > i1)
                            {
                                List<int[]> borderFields = new();
                                for (int k = 2; k <= vert - 1; k++)
                                {
                                    if (k <= 2)
                                    {
                                        borderFields.Add(new int[] { -1, k });
                                    }
                                    else if (hori >= 1 && k <= vert - 2)
                                    {
                                        borderFields.Add(new int[] { k - 4, k });
                                        borderFields.Add(new int[] { k - 3, k });
                                    }
                                }

                                List<int[]> newBorderFields = new();
                                for (int k = borderFields.Count - 1; k >= 0; k--)
                                {
                                    T("border field " + borderFields[k][0] + " " + borderFields[k][1]);
                                    newBorderFields.Add(borderFields[k]);
                                }

                                ResetExamAreas();

                                if (CountAreaRel(-1, 1, hori - 1, vert - 1, newBorderFields, circleDirectionLeft, 2, true))
                                {
                                    int black = (int)info[1];
                                    int white = (int)info[2];

                                    T("b " + black + " w " + white);

                                    if (black == white + hori + 1)
                                    {
                                        for (int k = vert - 1; k >= 3; k--)
                                        {
                                            path.Add(new int[] { x + (k - 3) * lx + k * sx, y + (k - 3) * ly + k * sy });
                                        }

                                        if (CheckCorner(-1, 2, 0, 2, circleDirectionLeft, true) && CheckNearFieldSmallRel1(-1, 2, 1, 1, true))
                                        {
                                            for (int k = vert - 1; k >= 3; k--)
                                            {
                                                path.RemoveAt(path.Count - 1);
                                            }

                                            AddExamAreas();
                                            T("StairAtStartConvexIn3 at " + hori + " " + vert + ": Cannot step straight");
                                            AddForbidden(0, 1);

                                            if (j == 0)
                                            {
                                                T("StairAtStartConvexIn3 at " + hori + " " + vert + ": Cannot step left");
                                                AddForbidden(1, 0);
                                            }
                                        }
                                        else
                                        {
                                            for (int k = vert - 1; k >= 3; k--)
                                            {
                                                path.RemoveAt(path.Count - 1);
                                            }
                                        }
                                    }
                                    else if (black == white && CheckNearFieldSmallRel0(-1, 3, 0, 2, true) && CheckNearFieldSmallRel0(-1, 1, 1, 1, false))
                                    {
                                        AddExamAreas();
                                        T("StairAtStartConvexIn3 at " + hori + " " + vert + ": Cannot step right");
                                        AddForbidden(-1, 0);

                                        if (j == 1)
                                        {
                                            T("StairAtStartConvexIn3 at " + hori + " " + vert + ": Cannot step down");
                                            AddForbidden(0, -1);
                                        }
                                    }
                                }
                            }
                        }
                    }

                    // rotate CCW
                    int l0 = lx;
                    int l1 = ly;
                    lx = -sx;
                    ly = -sy;
                    sx = l0;
                    sy = l1;
                }
                sx = thisSx;
                sy = thisSy;
                lx = -thisLx;
                ly = -thisLy;
            }
            sx = thisSx;
            sy = thisSy;
            lx = thisLx;
            ly = thisLy;
        }

        void StairAtStartConvexIn4()
        // Currently, all cases are applied StairAtEndConvexIn4 to, because there is no stair.
        {
            for (int i = 0; i < 2; i++)
            {
                bool circleDirectionLeft = (i == 0) ? true : false;

                for (int j = 0; j < 2; j++) // j = 0: straight area, j = 1: left (small) area
                {

                    // rotate CCW
                    int l0 = lx;
                    int l1 = ly;
                    lx = -sx;
                    ly = -sy;
                    sx = l0;
                    sy = l1;
                }
                sx = thisSx;
                sy = thisSy;
                lx = -thisLx;
                ly = -thisLy;
            }
            sx = thisSx;
            sy = thisSy;
            lx = thisLx;
            ly = thisLy;
        }

        void StairAtStartConvexStraight3()
        // 2024_0710, 2024_0710_1: area
        // 2026_0301_1: area
        // 2024_0611: [needs extension] close mid across
        {
            for (int i = 0; i < 2; i++)
            {
                bool circleDirectionLeft = (i == 0) ? true : false;

                for (int j = 0; j < 2; j++) // j = 0: straight area, j = 1: left (small) area
                {
                    int dist = size; // vertical distance
                    int j2 = (j == 0) ? 0 : 3;
                    int quarter = quarters[i][j2];

                    foreach (int[] corner in closedCorners[quarter])
                    {
                        if (j == 0 && corner[1] == corner[0] + 3)
                        {
                            if (corner[1] < dist) dist = corner[1];
                        }
                        else if (j == 1 && corner[0] == corner[1] + 3)
                        {
                            if (corner[0] < dist) dist = corner[0];
                        }
                    }

                    if (dist > 3 && dist < size)
                    {
                        T("StairAtStartConvexStraight3 distance " + (dist - 1), "side " + i, "rotation " + j);

                        bool distanceEmpty = true;
                        for (int k = 1; k <= dist - 1; k++)
                        {
                            if (k < 3)
                            {
                                if (InTakenRel(0, k)) distanceEmpty = false;
                            }
                            else
                            {
                                if (InTakenRel(k - 3, k)) distanceEmpty = false;
                            }
                        }

                        if (distanceEmpty)
                        {
                            int hori = dist - 3;
                            int vert = dist;

                            int i1 = InTakenIndexRel(hori, vert);
                            int i2 = InTakenIndexRel(hori + 1, vert);

                            if (i2 > i1)
                            {
                                List<int[]> borderFields = new();
                                for (int k = 2; k <= vert - 2; k++)
                                {
                                    if (k <= 2)
                                    {
                                        borderFields.Add(new int[] { 0, k });
                                    }
                                    else if (hori >= 2 && k <= vert - 2)
                                    {
                                        borderFields.Add(new int[] { k - 3, k });
                                        borderFields.Add(new int[] { k - 2, k });
                                    }
                                }

                                List<int[]> newBorderFields = new();
                                for (int k = borderFields.Count - 1; k >= 0; k--)
                                {
                                    T("border field " + borderFields[k][0] + " " + borderFields[k][1]);
                                    newBorderFields.Add(borderFields[k]);
                                }

                                ResetExamAreas();

                                if (CountAreaRel(0, 1, hori - 1, vert - 1, newBorderFields, circleDirectionLeft, 3, true))
                                {
                                    int black = (int)info[1];
                                    int white = (int)info[2];

                                    if (white == black)
                                    {
                                        for (int k = hori - 1; k >= 0; k--)
                                        {
                                            path.Add(new int[] { x + k * lx + (k + 3) * sx, y + k * ly + (k + 3) * sy });
                                        }
                                        int counter = hori;

                                        if (CheckCorner(0, 3, 0, 2, circleDirectionLeft, true))
                                        {
                                            AddExamAreas();
                                            T("StairAtStartConvexStraight3 at " + hori + " " + vert + ": Cannot step straight");

                                            for (int k = 1; k <= counter; k++)
                                            {
                                                path.RemoveAt(path.Count - 1);
                                            }
                                            AddForbidden(0, 1);
                                        }
                                        else
                                        {
                                            for (int k = 1; k <= counter; k++)
                                            {
                                                path.RemoveAt(path.Count - 1);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }

                    // rotate CCW
                    int l0 = lx;
                    int l1 = ly;
                    lx = -sx;
                    ly = -sy;
                    sx = l0;
                    sy = l1;
                }
                sx = thisSx;
                sy = thisSy;
                lx = -thisLx;
                ly = -thisLy;
            }
            sx = thisSx;
            sy = thisSy;
            lx = thisLx;
            ly = thisLy;
        }

        void StairAtStartConvexStraight4()
        // 2024_0618_2, 2026_0302_6, 2026_0304_1, 2026_0304_5: 1W
        // 2024_0610_4, 2024_0610_5, 9_121670752, 2024_0627: 1B
        // 2024_0725, 2024_0727_5: start obstacle as well
        {
            for (int i = 0; i < 2; i++)
            {
                bool circleDirectionLeft = (i == 0) ? true : false;

                for (int j = 0; j < 2; j++) // j = 0: straight area, j = 1: left (small) area
                {
                    int dist = size; // vertical distance
                    int j2 = (j == 0) ? 0 : 3;
                    int quarter = quarters[i][j2];

                    foreach (int[] corner in closedCorners[quarter])
                    {
                        if (j == 0 && corner[1] == corner[0] + 4)
                        {
                            if (corner[1] < dist) dist = corner[1];
                        }
                        else if (j == 1 && corner[0] == corner[1] + 4)
                        {
                            if (corner[0] < dist) dist = corner[0];
                        }
                    }

                    if (dist < size)
                    {
                        T("StairAtStartConvexStraight4 distance " + (dist - 1), "side " + i, "rotation " + j);

                        bool distanceEmpty = true;
                        for (int k = 1; k <= dist - 1; k++)
                        {
                            if (k < 4)
                            {
                                if (InTakenRel(0, k)) distanceEmpty = false;
                            }
                            else
                            {
                                if (InTakenRel(k - 4, k)) distanceEmpty = false;
                            }
                        }

                        if (distanceEmpty)
                        {
                            int hori = dist - 4;
                            int vert = dist;

                            int i1 = InTakenIndexRel(hori, vert);
                            int i2 = InTakenIndexRel(hori + 1, vert);

                            if (i2 > i1)
                            {
                                List<int[]> borderFields = new();
                                for (int k = 2; k <= vert - 2; k++)
                                {
                                    if (k <= 3)
                                    {
                                        borderFields.Add(new int[] { 0, k });
                                    }
                                    else if (hori >= 2 && k <= vert - 2)
                                    {
                                        borderFields.Add(new int[] { k - 4, k });
                                        borderFields.Add(new int[] { k - 3, k });
                                    }
                                }

                                List<int[]> newBorderFields = new();
                                for (int k = borderFields.Count - 1; k >= 0; k--)
                                {
                                    // T("border field " + borderFields[k][0] + " " + borderFields[k][1]);
                                    newBorderFields.Add(borderFields[k]);
                                }

                                ResetExamAreas();

                                if (CountAreaRel(0, 1, hori - 1, vert - 1, newBorderFields, circleDirectionLeft, 3, true))
                                {
                                    int black = (int)info[1];
                                    int white = (int)info[2];

                                    // we cannot enter later
                                    if (white - black == 1)
                                    {
                                        for (int k = vert - 1; k >= 4; k--)
                                        {
                                            path.Add(new int[] { x + (k - 4) * lx + k * sx, y + (k - 4) * ly + k * sy });
                                            T("Adding " + (x + (k - 4) * lx + k * sx) + " " + (y + (k - 4) * ly + k * sy));
                                        }
                                        int counter = hori;

                                        if (CheckCorner(0, 4, 0, 2, circleDirectionLeft, true))
                                        {
                                            // we cannot enter now straight
                                            if (CheckNearFieldSmallRel1(0, 2, 1, 1, false))
                                            {
                                                AddExamAreas();
                                                T("StairAtStartConvexStraight4 1W start obstacle at " + hori + " " + vert + ": Cannot step straight");
                                                RemoveEnd(counter);
                                                AddForbidden(0, 1);
                                            }
                                            else
                                            {
                                                AddExamAreas();
                                                T("StairAtStartConvexStraight4 1W at " + hori + " " + vert + ": Cannot step right and down");
                                                RemoveEnd(counter);
                                                AddForbidden(-1, 0);
                                                AddForbidden(0, -1);
                                            }
                                        }
                                        else
                                        {
                                            RemoveEnd(counter);
                                        }
                                    }
                                    // we cannot enter now straight
                                    else if (black - white == hori)
                                    {
                                        for (int k = vert - 1; k >= 3; k--)
                                        {
                                            path.Add(new int[] { x + (k - 3) * lx + k * sx, y + (k - 3) * ly + k * sy });
                                        }
                                        int counter = hori + 1;

                                        if (CheckCorner(0, 3, 0, 2, circleDirectionLeft, true))
                                        {
                                            AddExamAreas();
                                            T("StairAtStartConvexStraight4 1B at " + hori + " " + vert + ": Cannot step straight");
                                            RemoveEnd(counter);
                                            AddForbidden(0, 1);
                                        }
                                        else
                                        {
                                            RemoveEnd(counter);
                                        }
                                    }
                                }
                            }
                        }
                    }

                    // rotate CCW
                    int l0 = lx;
                    int l1 = ly;
                    lx = -sx;
                    ly = -sy;
                    sx = l0;
                    sy = l1;
                }
                sx = thisSx;
                sy = thisSy;
                lx = -thisLx;
                ly = -thisLy;
            }
            sx = thisSx;
            sy = thisSy;
            lx = thisLx;
            ly = thisLy;
        }

        void StairAtStartConvexStraight5()
        // 2024_0626; Stair at start convex 5
        // 2024_0727_4; start obstacle outside
        {
            for (int i = 0; i < 2; i++)
            {
                bool circleDirectionLeft = (i == 0) ? true : false;

                for (int j = 0; j < 2; j++) // j = 0: straight area, j = 1: left (small) area
                {
                    int dist = size; // vertical distance
                    int j2 = (j == 0) ? 0 : 3;
                    int quarter = quarters[i][j2];

                    foreach (int[] corner in closedCorners[quarter])
                    {
                        if (j == 0 && corner[1] == corner[0] + 5)
                        {
                            if (corner[1] < dist) dist = corner[1];
                        }
                        else if (j == 1 && corner[0] == corner[1] + 5)
                        {
                            if (corner[0] < dist) dist = corner[0];
                        }
                    }

                    if (dist < size)
                    {
                        T("StairAtStartConvexStraight5 distance " + (dist - 1), "side " + i, "rotation " + j);

                        bool distanceEmpty = true;
                        for (int k = 1; k <= dist - 1; k++)
                        {
                            if (k < 5)
                            {
                                if (InTakenRel(0, k)) distanceEmpty = false;
                            }
                            else
                            {
                                if (InTakenRel(k - 5, k)) distanceEmpty = false;
                            }
                        }

                        if (distanceEmpty)
                        {
                            int hori = dist - 5;
                            int vert = dist;

                            int i1 = InTakenIndexRel(hori, vert);
                            int i2 = InTakenIndexRel(hori + 1, vert);

                            if (i2 > i1)
                            {
                                List<int[]> borderFields = new();
                                for (int k = 2; k <= vert - 2; k++)
                                {
                                    if (k <= 4)
                                    {
                                        borderFields.Add(new int[] { 0, k });
                                    }
                                    else if (hori >= 2 && k <= vert - 2)
                                    {
                                        borderFields.Add(new int[] { k - 5, k });
                                        borderFields.Add(new int[] { k - 4, k });
                                    }
                                }

                                List<int[]> newBorderFields = new();
                                for (int k = borderFields.Count - 1; k >= 0; k--)
                                {
                                    T("border field " + borderFields[k][0] + " " + borderFields[k][1]);
                                    newBorderFields.Add(borderFields[k]);
                                }

                                ResetExamAreas();

                                if (CountAreaRel(0, 1, hori - 1, vert - 1, newBorderFields, circleDirectionLeft, 3, true))
                                {
                                    int black = (int)info[1];
                                    int white = (int)info[2];

                                    T("b " + black + " w " + white);

                                    // we cannot enter later
                                    if (white - black == hori + 1)
                                    {
                                        for (int k = hori; k >= 0; k--)
                                        {
                                            path.Add(new int[] { x + k * lx + (k + 5) * sx, y + k * ly + (k + 5) * sy });
                                        }
                                        int counter = hori + 1;

                                        if (CheckCorner(0, 4, 0, 2, circleDirectionLeft, true))
                                        {
                                            if (CheckNearFieldSmallRel1(0, 2, 1, 1, false))
                                            {
                                                AddExamAreas();
                                                T("StairAtStartConvexStraight5 start obstacle at " + hori + " " + vert + ": Cannot step straight");
                                                RemoveEnd(counter);
                                                AddForbidden(0, 1);
                                            }
                                            else
                                            {
                                                AddExamAreas();
                                                T("StairAtStartConvexStraight5 at " + hori + " " + vert + ": Cannot step right and down");
                                                RemoveEnd(counter);
                                                AddForbidden(-1, 0);
                                                AddForbidden(0, -1);
                                            }
                                        }
                                        else
                                        {
                                            RemoveEnd(counter);
                                        }
                                    }
                                }
                            }
                        }
                    }

                    // rotate CCW
                    int l0 = lx;
                    int l1 = ly;
                    lx = -sx;
                    ly = -sy;
                    sx = l0;
                    sy = l1;
                }
                sx = thisSx;
                sy = thisSy;
                lx = -thisLx;
                ly = -thisLy;
            }
            sx = thisSx;
            sy = thisSy;
            lx = thisLx;
            ly = thisLy;
        }

        void StairAtEndConvexIn2()
        // case 1:
        // 2026_0302_3 mid across left, mid across right
        // 2026_0404 across left, mid across right
        // 2026_0408_1 C-shape left, across right
        // 2026_0408_9 area left, mid across right
        // Sequence:
        // 2024_0516: mid across left, across right
        // 2024_0704, 2024_1014: area left, mid across right
        //
        // CW, cannot enter now

        // case 2: 2024_0805, 2024_0808

        {
            for (int i = 0; i < 2; i++)
            {
                bool circleDirectionLeft = (i == 0) ? true : false;

                for (int j = 0; j < 2; j++) // j = 0: straight area, j = 1: big area
                {
                    int dist = size; // horizontal distance
                    int quarter = quarters[i][j];

                    List<int[]> corners = i == 0 ? openCWCorners[quarter] : openCCWCorners[quarter];

                    // Find closest step
                    foreach (int[] corner in corners)
                    {
                        if (j == 0 && corner[0] == corner[1] + 3)
                        {
                            if (corner[0] < dist) dist = corner[0];
                        }
                        else if (j == 1 && corner[1] == corner[0] + 3)
                        {
                            if (corner[1] < dist) dist = corner[1];
                        }
                    }

                    // Find continuous steps until the furthest one
                    bool found = true;
                    while (found)
                    {
                        found = false;
                        foreach (int[] corner in corners)
                        {
                            if (j == 0 && corner[0] == dist + 1 && corner[0] == corner[1] + 3)
                            {
                                found = true;
                                dist++;
                            }
                            else if (j == 1 && corner[1] == dist + 1 && corner[1] == corner[0] + 3)
                            {
                                found = true;
                                dist++;
                            }
                        }
                    }

                    if (dist < size)
                    {
                        T("StairAtEndConvexIn2 distance " + (dist - 1), "side " + i, "rotation " + j);

                        bool distanceEmpty = true;
                        for (int k = 1; k <= dist - 1; k++)
                        {
                            if (k < dist - 1)
                            {
                                if (InTakenRel(k, k)) distanceEmpty = false;
                            }
                            else
                            {
                                if (InTakenRel(k, dist - 2)) distanceEmpty = false;
                            }
                        }

                        if (distanceEmpty)
                        {
                            int hori = dist;
                            int vert = dist - 3;

                            int i1 = InTakenIndexRel(hori, vert);
                            int i2 = InTakenIndexRel(hori + 1, vert);

                            if (i1 > i2)
                            {
                                List<int[]> borderFields = new();
                                for (int k = 2; k <= hori - 2; k++)
                                {
                                    borderFields.Add(new int[] { k, k - 1 });
                                    borderFields.Add(new int[] { k, k });
                                }

                                bool takenFound = false;
                                foreach (int[] field in borderFields)
                                {
                                    if (InTakenRel(field[0], field[1]))
                                    {
                                        takenFound = true;
                                        break;
                                    }
                                }

                                if (!takenFound)
                                {
                                    // reverse order
                                    List<int[]> newBorderFields = new();
                                    for (int k = borderFields.Count - 1; k >= 0; k--)
                                    {
                                        // T("border field " + borderFields[k][0] + " " + borderFields[k][1]);
                                        newBorderFields.Add(borderFields[k]);
                                    }

                                    ResetExamAreas();

                                    if (CountAreaRel(1, 1, hori - 1, vert + 1, newBorderFields, circleDirectionLeft, 2, true))
                                    {
                                        int black = (int)info[1];
                                        int white = (int)info[2];

                                        T("b " + black + " w " + white);

                                        if (black - white == vert && (CheckNearFieldSmallRel(hori - 1, vert + 1, 0, 0, true) || CheckCorner(hori - 1, vert + 1, 0, 0, circleDirectionLeft, true)))
                                        {
                                            if (CheckNearFieldSmallRel1(hori - 1, vert + 1, 1, 0, true))
                                            {
                                                AddExamAreas();
                                                T("StairAtEndConvexIn2 vB at " + hori + " " + vert + ": Cannot step left");
                                                AddForbidden(1, 0);

                                                if (j == 1)
                                                {
                                                    T("StairAtEndConvexIn2 vB case 1 at " + hori + " " + vert + ": Cannot step down");
                                                    AddForbidden(0, -1);
                                                }
                                            }
                                            // 9_22325 shows that dist must be greater than 3
                                            else if (dist > 3 && InTakenRel(-1, 0) && (InTakenRel(-2, 1) || InBorderRel(-2, 1)) && (InTakenRel(-2, 2) || InBorderRel(-2, 2)) && (InTakenRel(-2, 3) || InBorderRel(-2, 3)) && !InTakenRel(-1, 1) && !InTakenRel(-1, 3) && CheckNearFieldSmallRel0(0, 3, 0, 0, true))
                                            {
                                                AddExamAreas();
                                                T("StairAtEndConvexIn2 vB case 2 at " + hori + " " + vert + ": Cannot step straight");
                                                AddForbidden(0, 1);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }

                    // rotate CW
                    int s0 = sx;
                    int s1 = sy;
                    sx = -lx;
                    sy = -ly;
                    lx = s0;
                    ly = s1;
                }
                sx = thisSx;
                sy = thisSy;
                lx = -thisLx;
                ly = -thisLy;
            }
            sx = thisSx;
            sy = thisSy;
            lx = thisLx;
            ly = thisLy;
        }

        void StairAtEndConvexIn3()
        // W = B, cannot enter later
        // 2024_0718: mid across left, across right
        // 2024_0720_2: mid across x 2
        // 2024_0709: C-shape left, mid across right, no stair
        // 2024_0727: X-shape left, mid across right
        // 2026_0410_4: corner left, mid across right, no stair

        // with 3 obstacles:
        // 2024_0731, 2024_0811_3

        // (v+1)B
        // 2024_0516_2: across up, mid across down
        // 2024_1012: mid across up, across down

        {
            for (int i = 0; i < 2; i++)
            {
                bool circleDirectionLeft = (i == 0) ? true : false;

                for (int j = 0; j < 2; j++) // j = 0: straight area, j = 1: big area
                {
                    int dist = size; // horizontal distance
                    int quarter = quarters[i][j];

                    List<int[]> corners = i == 0 ? openCWCorners[quarter] : openCCWCorners[quarter];

                    // Find closest step
                    // No condition to have at least two steps: Will work as StraightSmall
                    foreach (int[] corner in corners)
                    {
                        if (j == 0 && corner[0] == corner[1] + 4)
                        {
                            if (corner[0] < dist) dist = corner[0];
                        }
                        else if (j == 1 && corner[1] == corner[0] + 4)
                        {
                            if (corner[1] < dist) dist = corner[1];
                        }
                    }

                    // Find continuous steps until the furthest one
                    bool found = true;
                    while (found)
                    {
                        found = false;
                        foreach (int[] corner in corners)
                        {
                            if (j == 0 && corner[0] == dist + 1 && corner[0] == corner[1] + 4)
                            {
                                found = true;
                                dist++;
                            }
                            else if (j == 1 && corner[1] == dist + 1 && corner[1] == corner[0] + 4)
                            {
                                found = true;
                                dist++;
                            }
                        }
                    }

                    if (dist < size)
                    {
                        T("StairAtEndConvexIn3 distance " + (dist - 1), "side " + i, "rotation " + j);

                        bool distanceEmpty = true;
                        for (int k = 1; k <= dist - 1; k++)
                        {
                            if (k < dist - 2)
                            {
                                if (InTakenRel(k, k)) distanceEmpty = false;
                            }
                            else
                            {
                                if (InTakenRel(k, dist - 3)) distanceEmpty = false;
                            }
                        }

                        if (distanceEmpty)
                        {
                            int hori = dist;
                            int vert = dist - 4;

                            int i1 = InTakenIndexRel(hori, vert);
                            int i2 = InTakenIndexRel(hori + 1, vert);

                            if (i1 > i2)
                            {
                                List<int[]> borderFields = new();
                                for (int k = 2; k <= hori - 2; k++)
                                {
                                    if (vert >= 1 && k <= hori - 3)
                                    {
                                        borderFields.Add(new int[] { k, k - 1 });
                                        borderFields.Add(new int[] { k, k });
                                    }
                                    else if (k > hori - 3)
                                    {
                                        borderFields.Add(new int[] { k, hori - 3 });
                                    }
                                }

                                bool takenFound = false;
                                foreach (int[] field in borderFields)
                                {
                                    if (InTakenRel(field[0], field[1]))
                                    {
                                        takenFound = true;
                                        break;
                                    }
                                }

                                if (!takenFound)
                                {
                                    // reverse order
                                    List<int[]> newBorderFields = new();
                                    for (int k = borderFields.Count - 1; k >= 0; k--)
                                    {
                                        // T("border field " + borderFields[k][0] + " " + borderFields[k][1]);
                                        newBorderFields.Add(borderFields[k]);
                                    }

                                    ResetExamAreas();

                                    if (CountAreaRel(1, 1, hori - 1, vert + 1, newBorderFields, circleDirectionLeft, 2, true))
                                    {
                                        int black = (int)info[1];
                                        int white = (int)info[2];

                                        // T("b " + black + " w " + white + ", " + CheckNearFieldSmallRel(hori - 1, vert + 1, 0, 0, true) + " " + CheckNearFieldSmallRel1(hori - 3, vert + 1, 1, 0, false));

                                        if (white == black)
                                        {
                                            AddEndFar(1, hori - 1, vert + 1);

                                            if (CheckCorner(hori - 1, vert + 1, 0, 0, circleDirectionLeft, true) || CheckNearFieldSmallRel(hori - 1, vert + 1, 0, 0, true))
                                            {
                                                if (CheckNearFieldSmallRel1(hori - 3, vert + 1, 1, 0, false))
                                                {
                                                    AddExamAreas();
                                                    T("StairAtEndConvexIn3 E at " + hori + " " + vert + ": Cannot step straight");
                                                    RemoveEnd(1);
                                                    AddForbidden(0, 1);

                                                    if (j == 0)
                                                    {
                                                        T("StairAtEndConvexIn3 E at " + hori + " " + vert + ": Cannot step right");
                                                        AddForbidden(-1, 0);
                                                    }
                                                }
                                                // for first condition, see 9_25691
                                                // for last condition, see 9_22325
                                                else if (dist > 4 && CheckNearFieldSmallRel0(hori - 4, vert + 2, 0, 0, true) && (InTakenRel(hori - 6, vert + 2) || InBorderRel(hori - 6, vert + 2)) && !InTakenRel(hori - 5, vert + 2) && !InBorderRel(hori - 5, vert + 2))
                                                {
                                                    i1 = InTakenIndexRel(hori - 6, vert + 2);
                                                    i2 = InTakenIndexRel(hori - 7, vert + 2);

                                                    if (i1 == -1)
                                                    {
                                                        i1 = InBorderIndexRel(hori - 6, vert + 2);
                                                        i2 = InBorderIndexRel(hori - 6, vert + 3);
                                                    }

                                                    if (i2 > i1)
                                                    {
                                                        AddExamAreas();
                                                        T("StairAtEndConvexIn3 E 3 obstacles at " + hori + " " + vert + ": Cannot step straight");
                                                        RemoveEnd(1);
                                                        AddForbidden(0, 1);

                                                        if (j == 0)
                                                        {
                                                            T("StairAtEndConvexIn3 E 3 obstacles at " + hori + " " + vert + ": Cannot step right");
                                                            AddForbidden(-1, 0);
                                                        }
                                                    }
                                                    else
                                                    {
                                                        RemoveEnd(1);
                                                    }
                                                }
                                                else
                                                {
                                                    RemoveEnd(1);
                                                }
                                            }
                                            else
                                            {
                                                RemoveEnd(1);
                                            }
                                        }
                                        else if (black == white + vert + 1 && CheckNearFieldSmallRel1(hori - 2, vert + 1, 0, 0, true) && CheckNearFieldSmallRel1(hori - 2, vert + 1, 1, 0, true))
                                        {
                                            AddExamAreas();
                                            T("StairAtEndConvexIn3 b = w + vert + 1 at " + hori + " " + vert + ": Cannot step left");
                                            AddForbidden(1, 0);

                                            if (j == 1)
                                            {
                                                T("StairAtEndConvexIn3 b = w + vert + 1 at " + hori + " " + vert + ": Cannot step down");
                                                AddForbidden(0, -1);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }

                    // rotate CW
                    int s0 = sx;
                    int s1 = sy;
                    sx = -lx;
                    sy = -ly;
                    lx = s0;
                    ly = s1;
                }
                sx = thisSx;
                sy = thisSy;
                lx = -thisLx;
                ly = -thisLy;
            }
            sx = thisSx;
            sy = thisSy;
            lx = thisLx;
            ly = thisLy;
        }

        void StairAtEndConvexIn4()
        // CW, cannot enter later
        // 2024_0626_1, 2026_0404_1 mid across, mic across
        // 2024_0730: mid across left, across right
        // 2024_0729_3: across left, mid across right
        // 2026_0404_3 area left, mid across right
        {
            for (int i = 0; i < 2; i++)
            {
                bool circleDirectionLeft = (i == 0) ? true : false;

                for (int j = 0; j < 2; j++) // j = 0: straight area, j = 1: big area
                {
                    int dist = size; // horizontal distance
                    int quarter = quarters[i][j];

                    List<int[]> corners = i == 0 ? openCWCorners[quarter] : openCCWCorners[quarter];

                    // Find closest step
                    foreach (int[] corner in corners)
                    {
                        if (j == 0 && corner[0] == corner[1] + 5)
                        {
                            if (corner[0] < dist) dist = corner[0];
                        }
                        else if (j == 1 && corner[1] == corner[0] + 5)
                        {
                            if (corner[1] < dist) dist = corner[1];
                        }
                    }

                    // Find continuous steps until the furthest one
                    bool found = true;
                    while (found)
                    {
                        found = false;
                        foreach (int[] corner in corners)
                        {
                            if (j == 0 && corner[0] == dist + 1 && corner[0] == corner[1] + 5)
                            {
                                found = true;
                                dist++;
                            }
                            else if (j == 1 && corner[1] == dist + 1 && corner[1] == corner[0] + 5)
                            {
                                found = true;
                                dist++;
                            }
                        }
                    }

                    if (dist < size)
                    {
                        T("StairAtEndConvexIn4 distance " + (dist - 1), "side " + i, "rotation " + j);

                        bool distanceEmpty = true;
                        for (int k = 1; k <= dist - 1; k++)
                        {
                            if (k < dist - 3)
                            {
                                if (InTakenRel(k, k)) distanceEmpty = false;
                            }
                            else
                            {
                                if (InTakenRel(k, dist - 4)) distanceEmpty = false;
                            }
                        }

                        if (distanceEmpty)
                        {
                            int hori = dist;
                            int vert = dist - 5;

                            int i1 = InTakenIndexRel(hori, vert);
                            int i2 = InTakenIndexRel(hori + 1, vert);

                            if (i1 > i2)
                            {
                                List<int[]> borderFields = new();
                                for (int k = 2; k <= hori - 2; k++)
                                {
                                    if (vert >= 1 && k <= hori - 4)
                                    {
                                        borderFields.Add(new int[] { k, k - 1 });
                                        borderFields.Add(new int[] { k, k });
                                    }
                                    else if (k > hori - 4)
                                    {
                                        borderFields.Add(new int[] { k, hori - 4 });
                                    }
                                }

                                bool takenFound = false;
                                foreach (int[] field in borderFields)
                                {
                                    if (InTakenRel(field[0], field[1]))
                                    {
                                        takenFound = true;
                                        break;
                                    }
                                }

                                if (!takenFound)
                                {
                                    // reverse order
                                    List<int[]> newBorderFields = new();
                                    for (int k = borderFields.Count - 1; k >= 0; k--)
                                    {
                                        T("border field " + borderFields[k][0] + " " + borderFields[k][1]);
                                        newBorderFields.Add(borderFields[k]);
                                    }

                                    ResetExamAreas();

                                    if (CountAreaRel(1, 1, hori - 1, vert + 1, newBorderFields, circleDirectionLeft, 2, true))
                                    {
                                        int black = (int)info[1];
                                        int white = (int)info[2];

                                        if (white - black == 1)
                                        {
                                            // direction does not matter at one step
                                            AddEndFar(1, hori - 1, vert);

                                            if (CheckCorner(hori - 2, vert + 1, 0, 0, circleDirectionLeft, true) && CheckNearFieldSmallRel1(hori - 4, vert + 1, 1, 0, false))
                                            {
                                                AddExamAreas();
                                                T("StairAtEndConvexIn4 1W at " + hori + " " + vert + ": Cannot step straight");
                                                RemoveEnd(1);
                                                AddForbidden(0, 1);

                                                if (j == 0)
                                                {
                                                    T("StairAtEndConvexIn4 1W at " + hori + " " + vert + ": Cannot step right");
                                                    AddForbidden(-1, 0);
                                                }
                                            }
                                            else
                                            {
                                                RemoveEnd(1);
                                            }
                                        }
                                    }
                                }
                            }

                        }
                    }

                    // rotate CW
                    int s0 = sx;
                    int s1 = sy;
                    sx = -lx;
                    sy = -ly;
                    lx = s0;
                    ly = s1;
                }
                sx = thisSx;
                sy = thisSy;
                lx = -thisLx;
                ly = -thisLy;
            }
            sx = thisSx;
            sy = thisSy;
            lx = thisLx;
            ly = thisLy;
        }

        void StairAtEndConvexStraight3()
        // Straight wall:
        // 2024_0905 mid across
        // 2024_0706, 2024_1001 (also Sequence2), 2024_1008 corner

        // Closed corner:
        // 2024_0916 across
        // 9_665575 mid across
        {
            for (int i = 0; i < 2; i++)
            {
                bool circleDirectionLeft = (i == 0) ? true : false;

                for (int j = 0; j < 2; j++) // j = 0: straight area, j = 1: big area
                {
                    int hori = 1;
                    int vert = 1;

                    bool found = false;

                    while (!InTakenRel(hori, vert) && !InBorderRel(hori, vert))
                    {
                        while (!InTakenRel(hori, vert) && !InBorderRel(hori, vert))
                        {
                            hori++;
                        }

                        if (hori == vert + 3)
                        {
                            found = true;
                            break;
                        }
                        else if (hori < vert + 3) break;

                        vert++;
                        hori = 1;
                    }

                    if (found && !InCornerRel(hori - 1, vert))
                    {
                        int dist = hori;

                        T("StairAtEndConvexStraight3 distance " + (dist - 1), "side " + i, "rotation " + j);

                        bool distanceEmpty = true;
                        for (int k = 1; k <= dist - 1; k++)
                        {
                            if (k < dist - 2)
                            {
                                if (InTakenRel(k, k)) distanceEmpty = false;
                            }
                            else
                            {
                                if (InTakenRel(k, dist - 3)) distanceEmpty = false;
                            }
                        }

                        if (distanceEmpty)
                        {
                            hori = dist;
                            vert = dist - 3;

                            int i1 = InTakenIndexRel(hori, vert);
                            int i2 = -1;
                            int i3 = -1;
                            // straight obstacle can be border
                            if (i1 == -1)
                            {
                                i2 = InBorderIndexRel(hori, vert + 1);
                                i3 = InBorderIndexRel(hori, vert - 1);
                            }
                            else
                            {
                                i2 = InTakenIndexRel(hori, vert + 1);
                                i3 = InTakenIndexRel(hori, vert - 1);
                            }

                            if (i1 != -1 && (i2 != -1 && i1 > i2 || i3 != -1 && i3 > i1) || i1 == -1 && i2 > i3)
                            {
                                List<int[]> borderFields = new();
                                for (int k = 2; k <= hori - 2; k++)
                                {
                                    if (vert >= 2 && k <= hori - 3)
                                    {
                                        borderFields.Add(new int[] { k, k - 1 });
                                        borderFields.Add(new int[] { k, k });
                                    }
                                    else if (k > hori - 3)
                                    {
                                        borderFields.Add(new int[] { k, vert });
                                    }
                                }

                                List<int[]> newBorderFields = new();
                                for (int k = borderFields.Count - 1; k >= 0; k--)
                                {
                                    T("border field " + borderFields[k][0] + " " + borderFields[k][1]);
                                    newBorderFields.Add(borderFields[k]);
                                }

                                ResetExamAreas();

                                if (CountAreaRel(1, 1, hori - 1, vert, newBorderFields, circleDirectionLeft, 2, true))
                                {
                                    int black = (int)info[1];
                                    int white = (int)info[2];

                                    T("b " + black + " w " + white);

                                    if (black - white == vert)
                                    {
                                        AddEndClose(vert, 1, 0);
                                        int counter = vert;

                                        if (CheckCorner(hori - 2, vert, 1, 0, circleDirectionLeft, true))
                                        {
                                            AddExamAreas();
                                            T("StairAtEndConvexStraight3 at " + hori + " " + vert + ": Cannot step left and down");
                                            RemoveEnd(counter);
                                            AddForbidden(1, 0);
                                            AddForbidden(0, -1);
                                        }
                                        else
                                        {
                                            RemoveEnd(counter);
                                        }
                                    }
                                }
                            }
                        }
                    }

                    // rotate CW
                    int s0 = sx;
                    int s1 = sy;
                    sx = -lx;
                    sy = -ly;
                    lx = s0;
                    ly = s1;
                }
                sx = thisSx;
                sy = thisSy;
                lx = -thisLx;
                ly = -thisLy;
            }
            sx = thisSx;
            sy = thisSy;
            lx = thisLx;
            ly = thisLy;
        }

        void StairAtEndConvexStraight4()
        // 2024_0624
        {
            for (int i = 0; i < 2; i++)
            {
                bool circleDirectionLeft = (i == 0) ? true : false;

                for (int j = 0; j < 2; j++) // j = 0: straight area, j = 1: big area
                {
                    int hori = 1;
                    int vert = 1;

                    bool found = false;

                    while (!InTakenRel(hori, vert) && !InBorderRel(hori, vert))
                    {
                        while (!InTakenRel(hori, vert) && !InBorderRel(hori, vert))
                        {
                            hori++;
                        }

                        if (hori == vert + 4)
                        {
                            found = true;
                            break;
                        }
                        else if (hori < vert + 4) break;

                        vert++;
                        hori = 1;
                    }

                    if (found && !InCornerRel(hori - 1, vert))
                    {
                        int dist = hori;

                        T("StairAtEndConvexStraight4 distance " + (dist - 1), "side " + i, "rotation " + j);

                        bool distanceEmpty = true;
                        for (int k = 1; k <= dist - 1; k++)
                        {
                            if (k < dist - 3)
                            {
                                if (InTakenRel(k, k)) distanceEmpty = false;
                            }
                            else
                            {
                                if (InTakenRel(k, dist - 4)) distanceEmpty = false;
                            }
                        }

                        if (distanceEmpty)
                        {
                            hori = dist;
                            vert = dist - 4;

                            int i1 = InTakenIndexRel(hori, vert);
                            int i2 = -1;
                            int i3 = -1;
                            // straight obstacle can be border
                            if (i1 == -1)
                            {
                                i2 = InBorderIndexRel(hori, vert + 1);
                                i3 = InBorderIndexRel(hori, vert - 1);
                            }
                            else
                            {
                                i2 = InTakenIndexRel(hori, vert + 1);
                                i3 = InTakenIndexRel(hori, vert - 1);
                            }

                            if (i1 != -1 && (i2 != -1 && i1 > i2 || i3 != -1 && i3 > i1) || i1 == -1 && i2 > i3)
                            {
                                List<int[]> borderFields = new();
                                for (int k = 2; k <= hori - 2; k++)
                                {
                                    if (vert >= 2 && k <= hori - 4)
                                    {
                                        borderFields.Add(new int[] { k, k - 1 });
                                        borderFields.Add(new int[] { k, k });
                                    }
                                    else if (k > hori - 4)
                                    {
                                        borderFields.Add(new int[] { k, vert });
                                    }
                                }

                                List<int[]> newBorderFields = new();
                                for (int k = borderFields.Count - 1; k >= 0; k--)
                                {
                                    T("border field " + borderFields[k][0] + " " + borderFields[k][1]);
                                    newBorderFields.Add(borderFields[k]);
                                }

                                ResetExamAreas();

                                if (CountAreaRel(1, 1, hori - 1, vert, newBorderFields, circleDirectionLeft, 2, true))
                                {
                                    int black = (int)info[1];
                                    int white = (int)info[2];

                                    T("b " + black + " w " + white);

                                    if (white - black == 1 && CheckNearFieldSmallRel1(vert, vert, 1, 0, false))
                                    {
                                        AddExamAreas();
                                        T("StairAtEndConvexStraight4 at " + hori + " " + vert + ": Cannot step straight and right");
                                        AddForbidden(0, 1);
                                        AddForbidden(-1, 0);
                                    }
                                }
                            }
                        }
                    }

                    // rotate CW
                    int s0 = sx;
                    int s1 = sy;
                    sx = -lx;
                    sy = -ly;
                    lx = s0;
                    ly = s1;
                }
                sx = thisSx;
                sy = thisSy;
                lx = -thisLx;
                ly = -thisLy;
            }
            sx = thisSx;
            sy = thisSy;
            lx = thisLx;
            ly = thisLy;
        }

        void StairAtEndConvexOut4()
        // 2025_0525_1
        {
            for (int i = 0; i < 2; i++)
            {
                bool circleDirectionLeft = (i == 0) ? true : false;

                for (int j = 0; j < 2; j++) // j = 0: straight area, j = 1: big area
                {
                    int dist = size; // horizontal distance
                    int quarter = quarters[i][j];

                    List<int[]> corners = closedCorners[quarter];

                    // Find closest step
                    foreach (int[] corner in corners)
                    {
                        if (j == 0 && corner[0] == corner[1] + 3)
                        {
                            if (corner[0] < dist) dist = corner[0];
                        }
                        else if (j == 1 && corner[1] == corner[0] + 3)
                        {
                            if (corner[1] < dist) dist = corner[1];
                        }
                    }

                    if (dist >= 5 && dist < size)
                    {
                        T("StairAtEndConvexOut4 distance " + (dist - 1), "side " + i, "rotation " + j);

                        bool distanceEmpty = true;
                        for (int k = 1; k <= dist - 1; k++)
                        {
                            if (k < dist - 3)
                            {
                                if (InTakenRel(k, k)) distanceEmpty = false;
                            }
                            else
                            {
                                if (InTakenRel(k, dist - 4)) distanceEmpty = false;
                            }
                        }

                        if (distanceEmpty)
                        {
                            int hori = dist;
                            int vert = dist - 3;

                            int i1 = InTakenIndexRel(hori, vert);
                            int i2 = InTakenIndexRel(hori + 1, vert);

                            if (i2 > i1)
                            {
                                List<int[]> borderFields = new();
                                for (int k = 2; k <= hori - 2; k++)
                                {
                                    if (vert == 2)
                                    {
                                        borderFields.Add(new int[] { k, vert - 1 });
                                    }
                                    else
                                    {
                                        if (k < hori - 3)
                                        {
                                            borderFields.Add(new int[] { k, k - 1 });
                                            borderFields.Add(new int[] { k, k });
                                        }
                                        else
                                        {
                                            borderFields.Add(new int[] { k, vert - 1 });
                                        }
                                    }
                                }

                                bool takenFound = false;
                                foreach (int[] field in borderFields)
                                {
                                    if (InTakenRel(field[0], field[1]))
                                    {
                                        takenFound = true;
                                        break;
                                    }
                                }

                                if (!takenFound)
                                {
                                    // reverse order
                                    List<int[]> newBorderFields = new();
                                    for (int k = borderFields.Count - 1; k >= 0; k--)
                                    {
                                        newBorderFields.Add(borderFields[k]);
                                    }

                                    ResetExamAreas();

                                    if (CountAreaRel(1, 1, hori - 1, vert - 1, newBorderFields, circleDirectionLeft, 2, true))
                                    {
                                        int black = (int)info[1];
                                        int white = (int)info[2];

                                        if (white - black == 1 && CheckNearFieldSmallRel1(vert - 1, vert - 1, 1, 0, false))
                                        {
                                            AddExamAreas();

                                            T("StairAtEndConvexOut4 at " + hori + " " + vert + ": Cannot step straight");
                                            AddForbidden(0, 1);

                                            if (j == 0)
                                            {
                                                T("StairAtEndConvexOut4 at " + hori + " " + vert + ": Cannot step right");
                                                AddForbidden(-1, 0);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }

                    // rotate CW
                    int s0 = sx;
                    int s1 = sy;
                    sx = -lx;
                    sy = -ly;
                    lx = s0;
                    ly = s1;
                }
                sx = thisSx;
                sy = thisSy;
                lx = -thisLx;
                ly = -thisLy;
            }
            sx = thisSx;
            sy = thisSy;
            lx = thisLx;
            ly = thisLy;
        }

        void StairAtEndConcaveIn2()
        // 2024_0717_4, 2024_0729_2
        {
            for (int i = 0; i < 2; i++)
            {
                bool circleDirectionLeft = (i == 0) ? false : true;

                for (int j = 0; j < 2; j++) // j = 0: straight area, j = 1: left (small) area
                {
                    int dist = size; // horizontal distance
                    int j2 = (j == 0) ? 0 : 3;
                    int quarter = quarters[i][j2];

                    List<int[]> corners = i == 0 ? openCWCorners[quarter] : openCCWCorners[quarter];

                    // Find closest step
                    foreach (int[] corner in corners)
                    {
                        if (j == 0 && corner[0] == corner[1] + 3)
                        {
                            if (corner[0] < dist) dist = corner[0];
                        }
                        else if (j == 1 && corner[1] == corner[0] + 3)
                        {
                            if (corner[1] < dist) dist = corner[1];
                        }
                    }

                    // Find continuous steps until the furthest one
                    bool found = true;
                    while (found)
                    {
                        found = false;
                        foreach (int[] corner in corners)
                        {
                            if (j == 0 && corner[0] == dist + 1 && corner[0] == corner[1] + 3)
                            {
                                found = true;
                                dist++;
                            }
                            else if (j == 1 && corner[1] == dist + 1 && corner[1] == corner[0] + 3)
                            {
                                found = true;
                                dist++;
                            }
                        }
                    }

                    if (dist < size)
                    {
                        T("StairAtEndConcaveIn2 distance " + (dist - 1), "side " + i, "rotation " + j);

                        bool distanceEmpty = true;
                        for (int k = 1; k <= dist - 1; k++)
                        {
                            if (k < dist - 1)
                            {
                                if (InTakenRel(k, k)) distanceEmpty = false;
                            }
                            else
                            {
                                if (InTakenRel(k, dist - 2)) distanceEmpty = false;
                            }
                        }

                        if (distanceEmpty)
                        {
                            int hori = dist;
                            int vert = dist - 3;

                            int i1 = InTakenIndexRel(hori, vert);
                            int i2 = InTakenIndexRel(hori + 1, vert);

                            if (i2 > i1)
                            {
                                List<int[]> borderFields = new();
                                for (int k = 1; k <= hori - 2; k++)
                                {
                                    if (k == 1 && vert >= 1)
                                    {
                                        borderFields.Add(new int[] { 1, 2 });
                                    }
                                    else if (k < hori - 2)
                                    {
                                        borderFields.Add(new int[] { k, k });
                                        borderFields.Add(new int[] { k, k + 1 });
                                    }
                                    else
                                    {
                                        if (vert > 0)
                                        {
                                            borderFields.Add(new int[] { k, vert + 1 });
                                        }
                                        else if (k > 1)
                                        {
                                            borderFields.Add(new int[] { k, 1 });
                                        }
                                    }
                                }

                                bool takenFound = false;
                                foreach (int[] field in borderFields)
                                {
                                    if (InTakenRel(field[0], field[1]))
                                    {
                                        takenFound = true;
                                        break;
                                    }
                                }

                                if (!takenFound)
                                {
                                    // reverse order
                                    List<int[]> newBorderFields = new();
                                    for (int k = borderFields.Count - 1; k >= 0; k--)
                                    {
                                        T("border field " + borderFields[k][0] + " " + borderFields[k][1]);
                                        newBorderFields.Add(borderFields[k]);
                                    }

                                    ResetExamAreas();

                                    if (CountAreaRel(1, 1, hori - 1, vert + 1, newBorderFields, circleDirectionLeft, 2, true))
                                    {
                                        int black = (int)info[1];
                                        int white = (int)info[2];

                                        T("b " + black + " w " + white);

                                        if (white == black + 1 && CheckNearFieldSmallRel1(hori - 3, hori - 2, 1, 0, true) && CheckNearFieldSmallRel1(hori - 1, hori - 2, 0, 0, false))
                                        {
                                            AddExamAreas();
                                            T("StairAtEndConcaveIn2 at " + hori + " " + vert + ": Cannot step straight");
                                            AddForbidden(0, 1);
                                        }
                                    }
                                }
                            }
                        }
                    }

                    // rotate CCW
                    int l0 = lx;
                    int l1 = ly;
                    lx = -sx;
                    ly = -sy;
                    sx = l0;
                    sy = l1;
                }
                sx = thisSx;
                sy = thisSy;
                lx = -thisLx;
                ly = -thisLy;
            }
            sx = thisSx;
            sy = thisSy;
            lx = thisLx;
            ly = thisLy;
        }

        void StairAtEndConcaveIn3()
        // 2024_0716
        {
            for (int i = 0; i < 2; i++)
            {
                bool circleDirectionLeft = (i == 0) ? false : true;

                for (int j = 0; j < 2; j++) // j = 0: straight area, j = 1: left (small) area
                {
                    int dist = size; // horizontal distance
                    int j2 = (j == 0) ? 0 : 3;
                    int quarter = quarters[i][j2];

                    List<int[]> corners = i == 0 ? openCWCorners[quarter] : openCCWCorners[quarter];

                    // Find closest step
                    foreach (int[] corner in corners)
                    {
                        if (j == 0 && corner[0] == corner[1] + 4)
                        {
                            if (corner[0] < dist) dist = corner[0];
                        }
                        else if (j == 1 && corner[1] == corner[0] + 4)
                        {
                            if (corner[1] < dist) dist = corner[1];
                        }
                    }

                    // Find continuous steps until the furthest one
                    bool found = true;
                    while (found)
                    {
                        found = false;
                        foreach (int[] corner in corners)
                        {
                            if (j == 0 && corner[0] == dist + 1 && corner[0] == corner[1] + 4)
                            {
                                found = true;
                                dist++;
                            }
                            else if (j == 1 && corner[1] == dist + 1 && corner[1] == corner[0] + 4)
                            {
                                found = true;
                                dist++;
                            }
                        }
                    }

                    if (dist < size)
                    {
                        T("StairAtEndConcaveIn3 distance " + (dist - 1), "side " + i, "rotation " + j);

                        bool distanceEmpty = true;
                        for (int k = 1; k <= dist - 1; k++)
                        {
                            if (k < dist - 2)
                            {
                                if (InTakenRel(k, k)) distanceEmpty = false;
                            }
                            else
                            {
                                if (InTakenRel(k, dist - 3)) distanceEmpty = false;
                            }
                        }

                        if (distanceEmpty)
                        {
                            int hori = dist;
                            int vert = dist - 4;

                            int i1 = InTakenIndexRel(hori, vert);
                            int i2 = InTakenIndexRel(hori + 1, vert);

                            if (i2 > i1)
                            {
                                List<int[]> borderFields = new();
                                for (int k = 1; k <= hori - 2; k++)
                                {
                                    if (k == 1 && vert >= 1)
                                    {
                                        borderFields.Add(new int[] { 1, 2 });
                                    }
                                    else if (k < hori - 3)
                                    {
                                        borderFields.Add(new int[] { k, k });
                                        borderFields.Add(new int[] { k, k + 1 });
                                    }
                                    else
                                    {
                                        if (vert > 0)
                                        {
                                            borderFields.Add(new int[] { k, vert + 1 });
                                        }
                                        else if (k > 1)
                                        {
                                            borderFields.Add(new int[] { k, 1 });
                                        }
                                    }
                                }

                                bool takenFound = false;
                                foreach (int[] field in borderFields)
                                {
                                    if (InTakenRel(field[0], field[1]))
                                    {
                                        takenFound = true;
                                        break;
                                    }
                                }

                                if (!takenFound)
                                {
                                    // reverse order
                                    List<int[]> newBorderFields = new();
                                    for (int k = borderFields.Count - 1; k >= 0; k--)
                                    {
                                        T("border field " + borderFields[k][0] + " " + borderFields[k][1]);
                                        newBorderFields.Add(borderFields[k]);
                                    }

                                    ResetExamAreas();

                                    if (CountAreaRel(1, 1, hori - 1, vert + 1, newBorderFields, circleDirectionLeft, 2, true))
                                    {
                                        int black = (int)info[1];
                                        int white = (int)info[2];

                                        T("b " + black + " w " + white);

                                        if (white == black + 1 && CheckNearFieldSmallRel1(hori - 3, hori - 3, 1, 0, true) && CheckNearFieldSmallRel1(hori - 2, hori - 3, 0, 0, false))
                                        {
                                            AddExamAreas();
                                            T("StairAtEndConcaveIn3 at " + hori + " " + vert + ": Cannot step straight");
                                            AddForbidden(0, 1);
                                        }
                                    }
                                }
                            }
                        }
                    }

                    // rotate CCW
                    int l0 = lx;
                    int l1 = ly;
                    lx = -sx;
                    ly = -sy;
                    sx = l0;
                    sy = l1;
                }
                sx = thisSx;
                sy = thisSy;
                lx = -thisLx;
                ly = -thisLy;
            }
            sx = thisSx;
            sy = thisSy;
            lx = thisLx;
            ly = thisLy;
        }

        void StairAtEndConcaveIn4()
        // obstacles inside, open corner: hori = vert + 5
        // 2024_0814: stair, mid across x 2
        // 2024_0619_1: mid across x 2
        // 2024_0729_1: across down, mid across up
        // 2024_0729_4: mid across down, across up
        // 2024_0820: mid across down, C-shape up
        {
            for (int i = 0; i < 2; i++)
            {
                bool circleDirectionLeft = (i == 0) ? false : true;

                for (int j = 0; j < 2; j++) // j = 0: straight area, j = 1: big area
                {
                    int dist = size; // horizontal distance
                    int quarter = quarters[i][j];

                    List<int[]> corners = i == 0 ? openCWCorners[quarter] : openCCWCorners[quarter];

                    // Find closest step
                    foreach (int[] corner in corners)
                    {
                        if (j == 0 && corner[0] == corner[1] + 5)
                        {
                            if (corner[0] < dist) dist = corner[0];
                        }
                        else if (j == 1 && corner[1] == corner[0] + 5)
                        {
                            if (corner[1] < dist) dist = corner[1];
                        }
                    }

                    // Find continuous steps until the furthest one
                    bool found = true;
                    while (found)
                    {
                        found = false;
                        foreach (int[] corner in corners)
                        {
                            if (j == 0 && corner[0] == dist + 1 && corner[0] == corner[1] + 5)
                            {
                                found = true;
                                dist++;
                            }
                            else if (j == 1 && corner[1] == dist + 1 && corner[1] == corner[0] + 5)
                            {
                                found = true;
                                dist++;
                            }
                        }
                    }

                    if (dist < size)
                    {
                        T("StairAtEndConcaveIn4 distance " + (dist - 1), "side " + i, "rotation " + j);

                        bool distanceEmpty = true;
                        for (int k = 1; k <= dist - 1; k++)
                        {
                            if (k < dist - 3)
                            {
                                if (InTakenRel(k, k)) distanceEmpty = false;
                            }
                            else
                            {
                                if (InTakenRel(k, dist - 4)) distanceEmpty = false;
                            }
                        }

                        if (distanceEmpty)
                        {
                            int hori = dist;
                            int vert = dist - 5;

                            int i1 = InTakenIndexRel(hori, vert);
                            int i2 = InTakenIndexRel(hori + 1, vert);

                            if (i2 > i1)
                            {
                                List<int[]> borderFields = new();
                                for (int k = 1; k <= hori - 2; k++)
                                {
                                    if (k == 1 && vert >= 1)
                                    {
                                        borderFields.Add(new int[] { 1, 2 });
                                    }
                                    else if (k < hori - 4)
                                    {
                                        borderFields.Add(new int[] { k, k });
                                        borderFields.Add(new int[] { k, k + 1 });
                                    }
                                    else
                                    {
                                        if (vert > 0)
                                        {
                                            borderFields.Add(new int[] { k, vert + 1 });
                                        }
                                        else if (k > 1)
                                        {
                                            borderFields.Add(new int[] { k, 1 });
                                        }
                                    }
                                }

                                bool takenFound = false;
                                foreach (int[] field in borderFields)
                                {
                                    if (InTakenRel(field[0], field[1]))
                                    {
                                        takenFound = true;
                                        break;
                                    }
                                }

                                if (!takenFound)
                                {
                                    // reverse order
                                    List<int[]> newBorderFields = new();
                                    for (int k = borderFields.Count - 1; k >= 0; k--)
                                    {
                                        newBorderFields.Add(borderFields[k]);
                                    }

                                    ResetExamAreas();

                                    if (CountAreaRel(1, 1, hori - 1, vert + 1, newBorderFields, circleDirectionLeft, 2, true))
                                    {
                                        int black = (int)info[1];
                                        int white = (int)info[2];

                                        if (white == black + 1 && CheckNearFieldSmallRel(hori - 1, vert + 1, 0, 0, false) && CheckNearFieldSmallRel1(hori - 3, vert + 1, 1, 0, true))
                                        {
                                            AddExamAreas();
                                            T("StairAtEndConcaveIn4 at " + hori + " " + vert + ": Cannot step left");
                                            AddForbidden(1, 0);

                                            if (j == 1)
                                            {
                                                T("StairAtEndConcaveIn4 at " + hori + " " + vert + ": Cannot step down");
                                                AddForbidden(0, -1);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }

                    // rotate CW
                    int s0 = sx;
                    int s1 = sy;
                    sx = -lx;
                    sy = -ly;
                    lx = s0;
                    ly = s1;
                }
                sx = thisSx;
                sy = thisSy;
                lx = -thisLx;
                ly = -thisLy;
            }
            sx = thisSx;
            sy = thisSy;
            lx = thisLx;
            ly = thisLy;
        }

        void StairAtEndConcaveIn5()
        // 1W
        // obstacles inside, hori = vert + 6
        // 2024_0714: mid across x 2
        // 2026_0410_6: left mid across, right across

        // (v+1)B, 3 obstacles
        // 2026_0408_8

        // CheckNearField minimal


        {
            for (int i = 0; i < 2; i++)
            {
                bool circleDirectionLeft = (i == 0) ? false : true;

                for (int j = 0; j < 2; j++) // j = 0: straight area, j = 1: big area
                {
                    int dist = size; // horizontal distance
                    int quarter = quarters[i][j];

                    List<int[]> corners = i == 0 ? openCWCorners[quarter] : openCCWCorners[quarter];

                    // Find closest step
                    // No condition to have at least two steps: Will work as StraightSmalll
                    foreach (int[] corner in corners)
                    {
                        if (j == 0 && corner[0] == corner[1] + 6)
                        {
                            if (corner[0] < dist) dist = corner[0];
                        }
                        else if (j == 1 && corner[1] == corner[0] + 6)
                        {
                            if (corner[1] < dist) dist = corner[1];
                        }
                    }

                    // Find continuous steps until the furthest one
                    bool found = true;
                    while (found)
                    {
                        found = false;
                        foreach (int[] corner in corners)
                        {
                            if (j == 0 && corner[0] == dist + 1 && corner[0] == corner[1] + 6)
                            {
                                found = true;
                                dist++;
                            }
                            else if (j == 1 && corner[1] == dist + 1 && corner[1] == corner[0] + 6)
                            {
                                found = true;
                                dist++;
                            }
                        }
                    }

                    if (dist < size)
                    {
                        T("StairAtEndConcaveIn5 distance " + (dist - 1), "side " + i, "rotation " + j);

                        bool distanceEmpty = true;
                        for (int k = 1; k <= dist - 1; k++)
                        {
                            if (k < dist - 4)
                            {
                                if (InTakenRel(k, k)) distanceEmpty = false;
                            }
                            else
                            {
                                if (InTakenRel(k, dist - 5)) distanceEmpty = false;
                            }
                        }

                        if (distanceEmpty)
                        {
                            int hori = dist;
                            int vert = dist - 6;

                            int i1 = InTakenIndexRel(hori, vert);
                            int i2 = InTakenIndexRel(hori + 1, vert);

                            if (i2 > i1)
                            {
                                List<int[]> borderFields = new();
                                for (int k = 1; k <= vert + 1; k++)
                                {
                                    if (k == 1 && k < vert + 1)
                                    {
                                        borderFields.Add(new int[] { 1, 2 });
                                    }
                                    else if (k < vert + 1)
                                    {
                                        borderFields.Add(new int[] { k, k });
                                        borderFields.Add(new int[] { k, k + 1 });
                                    }
                                    else
                                    {
                                        if (vert > 0)
                                        {
                                            for (int m = 0; m < hori - vert - 2; m++)
                                            {
                                                borderFields.Add(new int[] { k + m, k });
                                            }
                                        }
                                        else
                                        {
                                            for (int m = 1; m < hori - vert - 2; m++)
                                            {
                                                borderFields.Add(new int[] { k + m, k });
                                            }
                                        }
                                    }
                                }

                                bool takenFound = false;
                                foreach (int[] field in borderFields)
                                {
                                    if (InTakenRel(field[0], field[1]))
                                    {
                                        takenFound = true;
                                        break;
                                    }
                                }

                                if (!takenFound)
                                {
                                    // reverse order
                                    List<int[]> newBorderFields = new();
                                    for (int k = borderFields.Count - 1; k >= 0; k--)
                                    {
                                        newBorderFields.Add(borderFields[k]);
                                    }

                                    ResetExamAreas();

                                    if (CountAreaRel(1, 1, hori - 1, vert + 1, newBorderFields, circleDirectionLeft, 2, true))
                                    {
                                        int black = (int)info[1];
                                        int white = (int)info[2];

                                        T("b " + black + " w " + white);

                                        if (white == black + 1 && CheckNearFieldSmallRel0(hori - 2, vert + 1, 0, 0, false) && CheckNearFieldSmallRel1(hori - 4, vert + 1, 1, 0, true))
                                        {
                                            AddExamAreas();
                                            T("StairAtEndConcaveIn5 1W at " + hori + " " + vert + ": Cannot step left");
                                            AddForbidden(1, 0);

                                            if (j == 1)
                                            {
                                                T("StairAtEndConcaveIn5 1W at " + hori + " " + vert + ": Cannot step down");
                                                AddForbidden(0, -1);
                                            }
                                        }
                                        else if (black == white + vert + 1 && CheckNearFieldSmallRel0(hori - 1, vert + 1, 0, 0, false) && CheckNearFieldSmallRel0(hori - 3, vert + 1, 1, 0, true) && CheckNearFieldSmallRel1(hori - 4, vert + 1, 1, 3, true))
                                        {
                                            AddExamAreas();
                                            T("StairAtEndConcaveIn5 (v+1)B at " + hori + " " + vert + ": Cannot step straight");
                                            AddForbidden(0, 1);

                                            if (j == 0)
                                            {
                                                T("StairAtEndConcaveIn5 (v+1)B at " + hori + " " + vert + ": Cannot step right");
                                                AddForbidden(-1, 0);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }

                    // rotate CW
                    int s0 = sx;
                    int s1 = sy;
                    sx = -lx;
                    sy = -ly;
                    lx = s0;
                    ly = s1;
                }
                sx = thisSx;
                sy = thisSy;
                lx = -thisLx;
                ly = -thisLy;
            }
            sx = thisSx;
            sy = thisSy;
            lx = thisLx;
            ly = thisLy;
        }

        void StairAtEndConcaveStraight3()
        // 2024_0619, 2024_0717_2, 2025_0527, 2025_0527_1, 2026_0302_4, 2026_0302_5
        {
            for (int i = 0; i < 2; i++)
            {
                bool circleDirectionLeft = (i == 0) ? false : true;

                for (int j = 0; j < 2; j++) // j = 0: straight area, j = 1: left (small) area
                {
                    int hori = 1;
                    int vert = 1;

                    bool found = false;

                    while (!InTakenRel(hori, vert) && !InBorderRel(hori, vert))
                    {
                        while (!InTakenRel(hori, vert) && !InBorderRel(hori, vert))
                        {
                            hori++;
                        }

                        if (hori == vert + 3)
                        {
                            found = true;
                            break;
                        }
                        else if (hori < vert + 3) break;

                        vert++;
                        hori = 1;
                    }

                    if (found)
                    {
                        int dist = hori;

                        T("StairAtEndConcaveStraight3 distance " + (dist - 1), "side " + i, "rotation " + j);

                        bool distanceEmpty = true;
                        for (int k = 1; k <= dist - 1; k++)
                        {
                            if (k < dist - 2)
                            {
                                if (InTakenRel(k, k)) distanceEmpty = false;
                            }
                            else
                            {
                                if (InTakenRel(k, dist - 3)) distanceEmpty = false;
                            }
                        }

                        if (distanceEmpty)
                        {
                            hori = dist;
                            vert = dist - 3;

                            int i1 = InTakenIndexRel(hori, vert);
                            int i2 = -1;
                            int i3 = -1;
                            // straight obstacle can be border
                            if (i1 == -1)
                            {
                                i2 = InBorderIndexRel(hori, vert + 1);
                                i3 = InBorderIndexRel(hori, vert - 1);
                            }
                            else
                            {
                                i2 = InTakenIndexRel(hori, vert + 1);
                                i3 = InTakenIndexRel(hori, vert - 1);
                            }

                            if (i1 != -1 && (i2 != -1 && i2 > i1 || i3 != -1 && i1 > i3) || i1 == -1 && i3 > i2)
                            {
                                List<int[]> borderFields = new();
                                for (int k = 1; k <= hori - 2; k++)
                                {
                                    if (k == 1 && vert >= 2)
                                    {
                                        borderFields.Add(new int[] { 1, 2 });
                                    }
                                    else if (k < hori - 3)
                                    {
                                        borderFields.Add(new int[] { k, k });
                                        borderFields.Add(new int[] { k, k + 1 });
                                    }
                                    else
                                    {
                                        if (vert > 1)
                                        {
                                            borderFields.Add(new int[] { k, vert });
                                        }
                                        else if (k > 1)
                                        {
                                            borderFields.Add(new int[] { k, 1 });
                                        }
                                    }
                                }

                                bool takenFound = false;
                                /*foreach (int[] field in borderFields)
                                {
                                    if (InTakenRel(field[0], field[1]))
                                    {
                                        takenFound = true;
                                        break;
                                    }
                                }*/

                                if (!takenFound)
                                {
                                    // reverse order
                                    List<int[]> newBorderFields = new();
                                    for (int k = borderFields.Count - 1; k >= 0; k--)
                                    {
                                        newBorderFields.Add(borderFields[k]);
                                    }

                                    ResetExamAreas();

                                    if (CountAreaRel(1, 1, hori - 1, vert, newBorderFields, circleDirectionLeft, 2, true))
                                    {
                                        int black = (int)info[1];
                                        int white = (int)info[2];

                                        // T("b " + black + " w " + white);

                                        if (white - black == 1)
                                        {
                                            // this will close the area when walking it around
                                            path.Add(new int[] { x + sx, y + sy });

                                            if (CheckCorner(0, 1, 1, 0, !circleDirectionLeft, true))
                                            {
                                                AddExamAreas();
                                                T("StairAtEndConcaveStraight3 at " + hori + " " + vert + ": Cannot step straight");
                                                path.RemoveAt(path.Count - 1);
                                                AddForbidden(0, 1);
                                            }
                                            else
                                            {
                                                path.RemoveAt(path.Count - 1);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }

                    // rotate CCW
                    int l0 = lx;
                    int l1 = ly;
                    lx = -sx;
                    ly = -sy;
                    sx = l0;
                    sy = l1;
                }
                sx = thisSx;
                sy = thisSy;
                lx = -thisLx;
                ly = -thisLy;
            }
            sx = thisSx;
            sy = thisSy;
            lx = thisLx;
            ly = thisLy;
        }

        void StairAtEndConcaveStraight4()
        {
            // 2025_0522_1
            // in the rule, the obstacle is a straight wall, but in the example it is a corner

            for (int i = 0; i < 2; i++)
            {
                bool circleDirectionLeft = (i == 0) ? false : true;

                for (int j = 0; j < 2; j++) // j = 0: straight area, j = 1: left (small) area
                {
                    int dist = size; // horizontal distance
                    int j2 = (j == 0) ? 0 : 3;
                    int quarter = quarters[i][j2];

                    List<int[]> corners = closedCorners[quarter];

                    // Find closest step
                    foreach (int[] corner in corners)
                    {
                        if (j == 0 && corner[0] == corner[1] + 3)
                        {
                            if (corner[0] < dist) dist = corner[0];
                        }
                        else if (j == 1 && corner[1] == corner[0] + 3)
                        {
                            if (corner[1] < dist) dist = corner[1];
                        }
                    }

                    if (dist < size)
                    {
                        T("StairAtEndConcaveStraight4 distance " + (dist - 1), "side " + i, "rotation " + j);

                        bool distanceEmpty = true;
                        for (int k = 0; k <= dist - 1; k++)
                        {
                            if (k < dist - 3)
                            {
                                if (InTakenRel(k, k + 1)) distanceEmpty = false;
                            }
                            else
                            {
                                if (InTakenRel(k, dist - 3)) distanceEmpty = false;
                            }
                        }

                        if (distanceEmpty)
                        {
                            int hori = dist;
                            int vert = dist - 3;

                            int i1 = InTakenIndexRel(hori, vert);
                            int i2 = InTakenIndexRel(hori + 1, vert);

                            if (i1 > i2)
                            {
                                List<int[]> borderFields = new();
                                for (int k = 0; k <= hori - 2; k++)
                                {
                                    if (k == 0 && vert >= 2)
                                    {
                                        borderFields.Add(new int[] { 0, 2 });
                                    }
                                    else if (k < hori - 4)
                                    {
                                        borderFields.Add(new int[] { k, k + 1 });
                                        borderFields.Add(new int[] { k, k + 2 });
                                    }
                                    else
                                    {
                                        if (vert > 1)
                                        {
                                            borderFields.Add(new int[] { k, vert });
                                        }
                                        else if (k > 0) // 0, 1 is the start field of the area
                                        {
                                            borderFields.Add(new int[] { k, 1 });
                                        }
                                    }
                                }

                                bool takenFound = false;
                                foreach (int[] field in borderFields)
                                {
                                    if (InTakenRel(field[0], field[1]))
                                    {
                                        takenFound = true;
                                        break;
                                    }
                                }

                                if (!takenFound)
                                {
                                    // reverse order
                                    List<int[]> newBorderFields = new();
                                    for (int k = borderFields.Count - 1; k >= 0; k--)
                                    {
                                        newBorderFields.Add(borderFields[k]);
                                    }

                                    ResetExamAreas();

                                    if (CountAreaRel(0, 1, hori - 1, vert, newBorderFields, circleDirectionLeft, 3, true))
                                    {
                                        int black = (int)info[1];
                                        int white = (int)info[2];

                                        if (white - black == vert && CheckNearFieldSmallRel1(vert - 1, vert, 1, 0, true))
                                        {
                                            AddExamAreas();
                                            T("StairAtEndConcaveStraight4 at " + hori + " " + vert + ": Cannot step straight");
                                            AddForbidden(0, 1);
                                        }
                                    }
                                }

                            }
                        }
                    }

                    // rotate CCW
                    int l0 = lx;
                    int l1 = ly;
                    lx = -sx;
                    ly = -sy;
                    sx = l0;
                    sy = l1;
                }
                sx = thisSx;
                sy = thisSy;
                lx = -thisLx;
                ly = -thisLy;
            }
            sx = thisSx;
            sy = thisSy;
            lx = thisLx;
            ly = thisLy;
        }

        void StairAtEndConcaveStraight5()
        {
            // 2024_0618, 2024_0717_1, 2025_0525, 2026_0304_4: obstacle is closed corner
            // 2024_0818, 2025_0720, 2026_0301, 2026_0302_1, 2026_0304_7: obstacle is straight wall
            // Second obstacle is inside and seen at the first white field where we enter the area if we should enter it later.

            for (int i = 0; i < 2; i++)
            {
                bool circleDirectionLeft = (i == 0) ? false : true;

                for (int j = 0; j < 2; j++) // j = 0: upper-left quarter, j = 1: upper-right quarter
                {
                    int hori = 1;
                    int vert = 1;

                    bool found = false;

                    while (!InTakenRel(hori, vert) && !InBorderRel(hori, vert))
                    {
                        while (!InTakenRel(hori, vert) && !InBorderRel(hori, vert))
                        {
                            hori++;
                        }

                        if (hori == vert + 5)
                        {
                            found = true;
                            break;
                        }
                        else if (hori < vert + 5) break;

                        vert++;
                        hori = 1;
                    }

                    if (found)
                    {
                        int dist = hori;

                        T("StairAtEndConcaveStraight5 distance " + (dist - 1), "side " + i, "rotation " + j);

                        bool distanceEmpty = true;
                        for (int k = 1; k <= dist - 1; k++)
                        {
                            if (k < dist - 4)
                            {
                                if (InTakenRel(k, k)) distanceEmpty = false;
                            }
                            else
                            {
                                if (InTakenRel(k, dist - 5)) distanceEmpty = false;
                            }
                        }

                        if (distanceEmpty)
                        {
                            hori = dist;
                            vert = dist - 5;

                            int i1 = InTakenIndexRel(hori, vert);
                            int i2 = -1;
                            int i3 = -1;
                            // straight obstacle can be border
                            if (i1 == -1)
                            {
                                i2 = InBorderIndexRel(hori, vert + 1);
                                i3 = InBorderIndexRel(hori, vert - 1);
                            }
                            else
                            {
                                i2 = InTakenIndexRel(hori, vert + 1);
                                i3 = InTakenIndexRel(hori, vert - 1);
                            }

                            if (i1 != -1 && (i2 != -1 && i2 > i1 || i3 != -1 && i1 > i3) || i1 == -1 && i3 > i2)
                            {
                                List<int[]> borderFields = new();
                                for (int k = 1; k <= hori - 2; k++)
                                {
                                    if (k == 1 && vert >= 2)
                                    {
                                        borderFields.Add(new int[] { 1, 2 });
                                    }
                                    else if (k < hori - 5)
                                    {
                                        borderFields.Add(new int[] { k, k });
                                        borderFields.Add(new int[] { k, k + 1 });
                                    }
                                    else
                                    {
                                        if (vert > 1)
                                        {
                                            borderFields.Add(new int[] { k, vert });
                                        }
                                        else if (k > 1) // 1, 1 is the start field of the area
                                        {
                                            borderFields.Add(new int[] { k, 1 });
                                        }
                                    }
                                }

                                bool takenFound = false;
                                foreach (int[] field in borderFields)
                                {
                                    if (InTakenRel(field[0], field[1]))
                                    {
                                        takenFound = true;
                                        break;
                                    }
                                }

                                if (!takenFound)
                                {
                                    // reverse order
                                    List<int[]> newBorderFields = new();
                                    for (int k = borderFields.Count - 1; k >= 0; k--)
                                    {
                                        // T("Border field " + borderFields[k][0] + " " + borderFields[k][1]);
                                        newBorderFields.Add(borderFields[k]);
                                    }

                                    ResetExamAreas();

                                    if (CountAreaRel(1, 1, hori - 1, vert, newBorderFields, circleDirectionLeft, 2, true))
                                    {
                                        int black = (int)info[1];
                                        int white = (int)info[2];

                                        if (white - black == 1)
                                        {
                                            // this will close the area when walking it around
                                            for (int k = 0; k < vert; k++)
                                            {
                                                path.Add(new int[] { x + (k + 1) * lx + k * sx, y + (k + 1) * ly + k * sy });
                                            }
                                            int counter = vert;

                                            if (CheckCorner(vert + 1, vert, 1, 0, !circleDirectionLeft, true))
                                            {
                                                AddExamAreas();
                                                T("StairAtEndConcaveStraight5 at " + hori + " " + vert + ": Cannot step left and down");

                                                for (int k = 1; k <= counter; k++)
                                                {
                                                    path.RemoveAt(path.Count - 1);
                                                }
                                                AddForbidden(1, 0);
                                                AddForbidden(0, -1);
                                            }
                                            else
                                            {
                                                for (int k = 1; k <= counter; k++)
                                                {
                                                    path.RemoveAt(path.Count - 1);
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }

                    // rotate CW
                    int s0 = sx;
                    int s1 = sy;
                    sx = -lx;
                    sy = -ly;
                    lx = s0;
                    ly = s1;
                }
                sx = thisSx;
                sy = thisSy;
                lx = -thisLx;
                ly = -thisLy;
            }
            sx = thisSx;
            sy = thisSy;
            lx = thisLx;
            ly = thisLy;
        }

        void StairAtEndConcaveStraight6()
        {
            // 2026_0302
            // Second obstacle is inside and seen at the first white field where we enter the area if we should enter it later.

            for (int i = 0; i < 2; i++)
            {
                bool circleDirectionLeft = (i == 0) ? false : true;

                for (int j = 0; j < 2; j++) // j = 0: upper-left quarter, j = 1: upper-right quarter
                {
                    int hori = 1;
                    int vert = 1;

                    bool found = false;

                    while (!InTakenRel(hori, vert) && !InBorderRel(hori, vert))
                    {
                        while (!InTakenRel(hori, vert) && !InBorderRel(hori, vert))
                        {
                            hori++;
                        }

                        if (hori == vert + 6)
                        {
                            found = true;
                            break;
                        }
                        else if (hori < vert + 6) break;

                        vert++;
                        hori = 1;
                    }

                    if (found)
                    {
                        int dist = hori;

                        T("StairAtEndConcaveStraight6 distance " + (dist - 1), "side " + i, "rotation " + j);

                        bool distanceEmpty = true;
                        for (int k = 1; k <= dist - 1; k++)
                        {
                            if (k < dist - 5)
                            {
                                if (InTakenRel(k, k)) distanceEmpty = false;
                            }
                            else
                            {
                                if (InTakenRel(k, dist - 6)) distanceEmpty = false;
                            }
                        }

                        if (distanceEmpty)
                        {
                            hori = dist;
                            vert = dist - 6;

                            int i1 = InTakenIndexRel(hori, vert);
                            int i2 = -1;
                            int i3 = -1;
                            // straight obstacle can be border
                            if (i1 == -1)
                            {
                                i2 = InBorderIndexRel(hori, vert + 1);
                                i3 = InBorderIndexRel(hori, vert - 1);
                            }
                            else
                            {
                                i2 = InTakenIndexRel(hori, vert + 1);
                                i3 = InTakenIndexRel(hori, vert - 1);
                            }

                            if (i1 != -1 && (i2 != -1 && i2 > i1 || i3 != -1 && i1 > i3) || i1 == -1 && i3 > i2)
                            {
                                List<int[]> borderFields = new();
                                for (int k = 1; k <= hori - 2; k++)
                                {
                                    if (k == 1 && vert >= 2)
                                    {
                                        borderFields.Add(new int[] { 1, 2 });
                                    }
                                    else if (k < hori - 6)
                                    {
                                        borderFields.Add(new int[] { k, k });
                                        borderFields.Add(new int[] { k, k + 1 });
                                    }
                                    else
                                    {
                                        if (vert > 1)
                                        {
                                            borderFields.Add(new int[] { k, vert });
                                        }
                                        else if (k > 1) // 1, 1 is the start field of the area
                                        {
                                            borderFields.Add(new int[] { k, 1 });
                                        }
                                    }
                                }

                                bool takenFound = false;
                                foreach (int[] field in borderFields)
                                {
                                    if (InTakenRel(field[0], field[1]))
                                    {
                                        takenFound = true;
                                        break;
                                    }
                                }

                                if (!takenFound)
                                {
                                    // reverse order
                                    List<int[]> newBorderFields = new();
                                    for (int k = borderFields.Count - 1; k >= 0; k--)
                                    {
                                        T("border field " + borderFields[k][0] + " " + borderFields[k][1]);
                                        newBorderFields.Add(borderFields[k]);
                                    }

                                    ResetExamAreas();

                                    if (CountAreaRel(1, 1, hori - 1, vert, newBorderFields, circleDirectionLeft, 2, true))
                                    {
                                        int black = (int)info[1];
                                        int white = (int)info[2];

                                        T("b " + black + " w " + white);
                                        T(CheckNearFieldSmallRel0(vert + 2, vert, 1, 0, true));

                                        if (black - white == vert && CheckNearFieldSmallRel0(vert + 2, vert, 1, 0, true))
                                        {
                                            path.Add(new int[] { x + (vert + 1) * lx + (vert + 1) * sx, y + (vert + 1) * ly + (vert + 1) * sy });
                                            path.Add(new int[] { x + (vert + 1) * lx + vert * sx, y + (vert + 1) * ly + vert * sy });
                                            int counter = 2;

                                            if (CheckCorner(vert + 1, vert, 1, 3, !circleDirectionLeft, true))
                                            {
                                                AddExamAreas();
                                                T("StairAtEndConcaveStraight6 at " + hori + " " + vert + ": Cannot step straight");

                                                for (int k = 1; k <= counter; k++)
                                                {
                                                    path.RemoveAt(path.Count - 1);
                                                }
                                                AddForbidden(0, 1);
                                            }
                                            else
                                            {
                                                for (int k = 1; k <= counter; k++)
                                                {
                                                    path.RemoveAt(path.Count - 1);
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }

                    // rotate CW
                    int s0 = sx;
                    int s1 = sy;
                    sx = -lx;
                    sy = -ly;
                    lx = s0;
                    ly = s1;
                }
                sx = thisSx;
                sy = thisSy;
                lx = -thisLx;
                ly = -thisLy;
            }
            sx = thisSx;
            sy = thisSy;
            lx = thisLx;
            ly = thisLy;
        }

        void StairAtEndConcaveOut3()
        // 2024_0811
        {
            for (int i = 0; i < 2; i++)
            {
                bool circleDirectionLeft = (i == 0) ? false : true;

                for (int j = 0; j < 2; j++) // j = 0: straight area, j = 1: small area
                {
                    int dist = size; // vertical distance
                    int j2 = (j == 0) ? 0 : 3;
                    int quarter = quarters[i][j2];

                    List<int[]> corners = closedCorners[quarter];

                    // Find closest step
                    foreach (int[] corner in corners)
                    {
                        if (j == 0 && corner[0] == corner[1] + 2)
                        {
                            if (corner[0] < dist) dist = corner[0];
                        }
                        else if (j == 1 && corner[1] == corner[0] + 2)
                        {
                            if (corner[1] < dist) dist = corner[1];
                        }
                    }

                    if (dist >= 4 && dist < size)
                    {
                        T("StairAtEndConcaveOut3 distance " + (dist - 1), "side " + i, "rotation " + j);

                        bool distanceEmpty = true;
                        for (int k = 1; k <= dist - 1; k++)
                        {
                            if (k < dist - 2)
                            {
                                if (InTakenRel(k, k)) distanceEmpty = false;
                            }
                            else
                            {
                                if (InTakenRel(k, dist - 3)) distanceEmpty = false;
                            }
                        }

                        if (distanceEmpty)
                        {
                            int hori = dist;
                            int vert = dist - 2;

                            int i1 = InTakenIndexRel(hori, vert);
                            int i2 = InTakenIndexRel(hori + 1, vert);

                            if (i1 > i2)
                            {
                                List<int[]> borderFields = new();
                                for (int k = 1; k <= hori - 2; k++)
                                {
                                    if (k == 1 && vert > 2)
                                    {
                                        borderFields.Add(new int[] { 1, 2 });
                                    }
                                    else if (k < hori - 3)
                                    {
                                        borderFields.Add(new int[] { k, k });
                                        borderFields.Add(new int[] { k, k + 1 });
                                    }
                                    else
                                    {
                                        if (vert > 2)
                                        {
                                            borderFields.Add(new int[] { k, vert - 1 });
                                        }
                                        else if (k > 1) // 1, 1 is the start field of the area
                                        {
                                            borderFields.Add(new int[] { k, 1 });
                                        }
                                    }
                                }

                                List<int[]> newBorderFields = new();
                                for (int k = borderFields.Count - 1; k >= 0; k--)
                                {
                                    // T("border field " + borderFields[k][0] + " " + borderFields[k][1]);
                                    newBorderFields.Add(borderFields[k]);
                                }

                                ResetExamAreas();

                                if (CountAreaRel(1, 1, hori - 1, vert - 1, newBorderFields, circleDirectionLeft, 2, true))
                                {
                                    int black = (int)info[1];
                                    int white = (int)info[2];

                                    // T("b " + black + " w " + white);

                                    if (white - black == 1)
                                    {
                                        // this will close the area when walking it around
                                        AddEndClose(1, 0, 1);

                                        if (CheckNearFieldSmallRel0(0, 1, 1, 0, true))
                                        {
                                            AddExamAreas();
                                            T("StairAtEndConcaveOut3 at " + hori + " " + vert + ": Cannot step straight");
                                            RemoveEnd(1);
                                            AddForbidden(0, 1);
                                        }
                                        else
                                        {
                                            RemoveEnd(1);
                                        }
                                    }
                                }
                            }
                        }
                    }

                    // rotate CCW
                    int l0 = lx;
                    int l1 = ly;
                    lx = -sx;
                    ly = -sy;
                    sx = l0;
                    sy = l1;
                }
                sx = thisSx;
                sy = thisSy;
                lx = -thisLx;
                ly = -thisLy;
            }
            sx = thisSx;
            sy = thisSy;
            lx = thisLx;
            ly = thisLy;
        }

        void StairAtEndConcaveOut5()
        // start obstalce inside: 2024_0817 mid across, 2026_0408 across
        // end obstacle outside: 2026_0304_2, 2026_0304_6
        // CheckNearFieldSmallRel minimal
        {
            for (int i = 0; i < 2; i++)
            {
                bool circleDirectionLeft = (i == 0) ? false : true;

                for (int j = 0; j < 2; j++) // j = 0: straight area, j = 1: big area
                {
                    int dist = size; // vertical distance
                    int quarter = quarters[i][j];

                    List<int[]> corners = closedCorners[quarter];

                    foreach (int[] corner in corners)
                    {
                        if (j == 0 && corner[0] == corner[1] + 4)
                        {
                            if (corner[0] < dist) dist = corner[0];
                        }
                        else if (j == 1 && corner[1] == corner[0] + 4)
                        {
                            if (corner[1] < dist) dist = corner[1];
                        }
                    }

                    T("StairAtEndConcaveOut5 dist " + dist);

                    if (dist >= 6 && dist < size)
                    {
                        T("StairAtEndConcaveOut5 distance " + (dist - 1), "side " + i, "rotation " + j);

                        bool distanceEmpty = true;
                        // dist = 6, loop from 1 to 5
                        for (int k = 1; k <= dist - 1; k++)
                        {
                            if (k < dist - 5)
                            {
                                if (InTakenRel(k, k)) distanceEmpty = false;
                            }
                            else
                            {
                                if (InTakenRel(k, dist - 5)) distanceEmpty = false;
                            }
                        }

                        if (distanceEmpty)
                        {
                            int hori = dist;
                            int vert = dist - 4;

                            int i1 = InTakenIndexRel(hori, vert);
                            int i2 = InTakenIndexRel(hori, vert + 1);

                            if (i2 > i1)
                            {
                                List<int[]> borderFields = new();
                                // hori = 2, vert = 6, loop from 1 to 1
                                for (int k = 1; k <= hori - 2; k++)
                                {
                                    if (k == 1 && vert > 2)
                                    {
                                        borderFields.Add(new int[] { 1, 2 });
                                    }
                                    else if (k < hori - 5)
                                    {
                                        borderFields.Add(new int[] { k, k });
                                        borderFields.Add(new int[] { k, k + 1 });
                                    }
                                    else
                                    {
                                        if (vert > 2)
                                        {
                                            borderFields.Add(new int[] { k, vert - 1 });
                                        }
                                        else if (k > 1)
                                        {
                                            borderFields.Add(new int[] { k, 1 });
                                        }
                                    }
                                }

                                // reverse order
                                List<int[]> newBorderFields = new();
                                for (int k = borderFields.Count - 1; k >= 0; k--)
                                {
                                    T("border field " + borderFields[k][0] + " " + borderFields[k][1]);
                                    newBorderFields.Add(borderFields[k]);
                                }

                                ResetExamAreas();

                                if (CountAreaRel(1, 1, hori - 1, vert - 1, newBorderFields, circleDirectionLeft, 2, true))
                                {
                                    int black = (int)info[1];
                                    int white = (int)info[2];

                                    T("b w " + black + " " + white);

                                    if (white == black + 1 && (CheckNearFieldSmallRel0(hori - 1, vert - 1, 1, 3, true) || CheckNearFieldSmallRel1(hori - 4, vert - 1, 1, 0, true)))
                                    {
                                        AddExamAreas();
                                        T("StairAtEndConcaveOut5 at " + hori + " " + vert + ": Cannot step left and down");
                                        AddForbidden(1, 0);
                                        AddForbidden(0, -1);
                                    }
                                }
                            }
                        }
                    }

                    // rotate CW
                    int s0 = sx;
                    int s1 = sy;
                    sx = -lx;
                    sy = -ly;
                    lx = s0;
                    ly = s1;
                }
                sx = thisSx;
                sy = thisSy;
                lx = -thisLx;
                ly = -thisLy;
            }
            sx = thisSx;
            sy = thisSy;
            lx = thisLx;
            ly = thisLy;
        }

        void DoubleStair()
        // 2024_0706_1, 2024_1006_1, 2024_0711
        // Also Sequence2: 2024_0516_4, 2024_0516_5, 2024_0727_6
        {
            for (int i = 0; i < 2; i++)
            {
                for (int j = 0; j < 2; j++) // normal or big area
                {
                    // 2024_1006_1 shows that (1, -1) is not necessarily a corner 
                    if (InTakenRel(1, -1) && !InTakenRel(1, 0))
                    {
                        int quarter = quarters[i][j];
                        bool corner1Found = false;
                        bool corner2Found = false;

                        foreach (int[] corner in closedCorners[quarter])
                        {
                            if (corner[0] == 3 && corner[1] == 3)
                            {
                                corner1Found = true;
                            }
                            else if (j == 0 && corner[0] == 4 && corner[1] == 2 ||
                                j == 1 && corner[0] == 2 && corner[1] == 4)
                            {
                                corner2Found = true;
                            }
                        }

                        if (corner1Found && corner2Found)
                        {
                            int i1 = InTakenIndexRel(3, 3);
                            int i2 = InTakenIndexRel(4, 3);

                            if (i1 > i2)
                            {
                                T("Double stair corners found at", i, j);

                                // either stair on both sides of the two corners (2024_0706_1) or close obstacle (2024_0516_4) or area (2024_0727_6)
                                AddEndFar(1, 2, 2);
                                bool circleDirectionLeft = (i == 0) ? true : false;

                                if ((CheckNearFieldSmallRel(2, 2, 0, 2, false) || CheckCorner(2, 2, 0, 2, circleDirectionLeft, false)) && CheckNearFieldSmallRel(3, 1, 1, 3, true))
                                {
                                    T("DoubleStair: Cannot step straight");
                                    RemoveEnd(1);
                                    AddForbidden(0, 1);

                                    if (j == 0)
                                    {
                                        T("DoubleStair: Cannot step right");
                                        AddForbidden(-1, 0);
                                    }
                                }
                                else
                                {
                                    RemoveEnd(1);
                                }
                            }
                        }
                    }

                    // rotate CW
                    int s0 = sx;
                    int s1 = sy;
                    sx = -lx;
                    sy = -ly;
                    lx = s0;
                    ly = s1;
                }
                sx = thisSx;
                sy = thisSy;
                lx = -thisLx;
                ly = -thisLy;
            }
            sx = thisSx;
            sy = thisSy;
            lx = thisLx;
            ly = thisLy;
        }

        void DoubleStairReversed()
        // 2026_0124, 2026_0304
        // 2024_0726, 2024_0713: obstacles are not all corners
        // close obstacle at 3, -1 theoretically possible
        {
            for (int i = 0; i < 2; i++)
            {
                for (int j = 0; j < 2; j++) // normal or big area
                {
                    if (InTakenRel(5, 1) && InTakenRel(4, 2) && !InTakenRel(4, 1))
                    {
                        int i1 = InTakenIndexRel(4, 2);
                        int i2 = InTakenIndexRel(5, 2);

                        if (i1 > i2)
                        {
                            T("Double stair reversed corners found at", i, j);

                            if (InTakenRel(1, -1) && InTakenRel(2, -2) && !InTakenRel(1, 0) && !InTakenRel(2, -1) && !InTakenRel(3, -2) && CheckNearFieldSmallRel(3, -1, 0, 1, true))
                            {
                                T("DoubleStairReversed: Cannot step straight");
                                AddForbidden(0, 1);

                                if (j == 0)
                                {
                                    T("DoubleStairReversed: Cannot step right");
                                    AddForbidden(-1, 0);
                                }
                            }
                        }
                    }

                    // rotate CW
                    int s0 = sx;
                    int s1 = sy;
                    sx = -lx;
                    sy = -ly;
                    lx = s0;
                    ly = s1;
                }
                sx = thisSx;
                sy = thisSy;
                lx = -thisLx;
                ly = -thisLy;
            }
            sx = thisSx;
            sy = thisSy;
            lx = thisLx;
            ly = thisLy;
        }

        void StairAtEnd3Obtacles1()
        // 2024_0731_1 straight area
        // 2024_0725_4 small area
        {
            for (int i = 0; i < 2; i++)
            {
                bool circleDirectionLeft = (i == 0) ? true : false;

                for (int j = 0; j < 4; j++) // j = 0: straight area, j = 3: small area
                {
                    if (j == 0 || j == 3)
                    {
                        int dist = size; // horizontal distance
                        int quarter = quarters[i][j];

                        List<int[]> corners = i == 0 ? openCWCorners[quarter] : openCCWCorners[quarter];

                        // Find closest step
                        foreach (int[] corner in corners)
                        {
                            if (j == 0 && corner[0] == corner[1] + 1)
                            {
                                if (corner[0] < dist) dist = corner[0];
                            }
                            else if (j == 3 && corner[1] == corner[0] + 1)
                            {
                                if (corner[1] < dist) dist = corner[1];
                            }
                        }

                        // Find continuous steps until the furthest one
                        bool found = true;
                        while (found)
                        {
                            found = false;
                            foreach (int[] corner in corners)
                            {
                                if (j == 0 && corner[0] == dist + 1 && corner[0] == corner[1] + 1)
                                {
                                    found = true;
                                    dist++;
                                }
                                else if (j == 3 && corner[1] == dist + 1 && corner[1] == corner[0] + 1)
                                {
                                    found = true;
                                    dist++;
                                }
                            }
                        }

                        if (dist < size)
                        {
                            T("StairAtEnd3Obtacles1 distance " + (dist - 1), "side " + i, "rotation " + j);

                            bool distanceEmpty = true;
                            for (int k = -1; k <= dist - 1; k++)
                            {
                                if (k < dist - 1)
                                {
                                    if (InTakenRel(k, k + 2)) distanceEmpty = false;
                                }
                                else
                                {
                                    if (InTakenRel(k, dist - 1)) distanceEmpty = false;
                                }
                            }

                            if (distanceEmpty)
                            {
                                int hori = dist;
                                int vert = dist - 1;

                                int i1 = InTakenIndexRel(hori, vert);
                                int i2 = InTakenIndexRel(hori + 1, vert);

                                if (i2 < i1)
                                {
                                    List<int[]> borderFields = new();
                                    for (int k = 1; k <= vert + 1; k++)
                                    {
                                        if (k == 1)
                                        {
                                            borderFields.Add(new int[] { 0, 1 });
                                        }
                                        else if (k < vert + 1)
                                        {
                                            borderFields.Add(new int[] { k - 2, k });
                                            borderFields.Add(new int[] { k - 1, k });
                                        }
                                        else
                                        {
                                            borderFields.Add(new int[] { k - 2, k });
                                        }
                                    }

                                    bool takenFound = false;
                                    foreach (int[] field in borderFields)
                                    {
                                        if (InTakenRel(field[0], field[1]))
                                        {
                                            takenFound = true;
                                            break;
                                        }
                                    }

                                    if (!takenFound)
                                    {
                                        // reverse order
                                        List<int[]> newBorderFields = new();
                                        for (int k = borderFields.Count - 1; k >= 0; k--)
                                        {
                                            newBorderFields.Add(borderFields[k]);
                                        }

                                        //in order to be able to walk through the area, the field to the left has to be added and current position set to 2 left. CountAreaRel must be implemented here.
                                        int left1 = -1;
                                        int straight1 = 1;
                                        int left2 = hori - 1;
                                        int straight2 = vert + 1;

                                        int x1 = x + left1 * lx + straight1 * sx;
                                        int y1 = y + left1 * ly + straight1 * sy;
                                        int x2 = x + left2 * lx + straight2 * sx;
                                        int y2 = y + left2 * ly + straight2 * sy;

                                        List<int[]> absBorderFields = new();
                                        foreach (int[] field2 in newBorderFields)
                                        {
                                            absBorderFields.Add(new int[] { x + field2[0] * lx + field2[1] * sx, y + field2[0] * ly + field2[1] * sy });
                                        }

                                        path.Add(new int[] { x - lx, y - ly });
                                        path.Add(new int[] { x - 2 * lx, y - 2 * ly });
                                        x = x - 2 * lx;
                                        y = y - 2 * ly;

                                        ResetExamAreas();

                                        if (CountArea(x1, y1, x2, y2, absBorderFields, circleDirectionLeft, 2, true))
                                        {
                                            path.RemoveAt(path.Count - 1);
                                            path.RemoveAt(path.Count - 1);
                                            x = x + 2 * lx;
                                            y = y + 2 * ly;

                                            int black = (int)info[1];
                                            int white = (int)info[2];

                                            if (black == white + vert && CheckNearFieldSmallRel0(hori - 1, vert + 1, 0, 0, true))
                                            {
                                                // Find straight obstacle on the left at 3 distance
                                                dist = 1;

                                                while (!InTakenRel(-dist, 0) && !InBorderRel(-dist, 0))
                                                {
                                                    dist++;
                                                }

                                                if (dist == 4)
                                                {
                                                    bool circleValid = false;

                                                    if (InBorderRel(-dist, 0))
                                                    {
                                                        i1 = InBorderIndexRel(-dist, 0);
                                                        i2 = InBorderIndexRel(-dist, -1);

                                                        if (i1 > i2)
                                                        {
                                                            circleValid = true;
                                                        }
                                                    }
                                                    else
                                                    {
                                                        i1 = InTakenIndexRel(-dist, 0);
                                                        i2 = InTakenIndexRel(-dist, -1);

                                                        if (i2 != -1)
                                                        {
                                                            if (i2 > i1)
                                                            {
                                                                circleValid = true;

                                                            }
                                                        }
                                                        else
                                                        {
                                                            i2 = InTakenIndexRel(-dist, 1);
                                                            if (i1 > i2)
                                                            {
                                                                circleValid = true;
                                                            }
                                                        }
                                                    }

                                                    if (circleValid)
                                                    {
                                                        if (CountAreaRel(-1, 0, -3, 0, new List<int[]> { new int[] { -2, 0 } }, !circleDirectionLeft, 3, true))
                                                        {
                                                            black = (int)info[1];
                                                            white = (int)info[2];

                                                            if (black == white && CheckNearFieldSmallRel(-2, 1, 1, 0, true) && CheckNearFieldSmallRel(hori - 4, vert + 2, 1, 0, true) && CheckNearFieldSmallRel0(hori - 4, vert + 2, 0, 0, true))
                                                            {
                                                                AddExamAreas();
                                                                T("Reverse stair 3 obstacles case 1 at " + hori + " " + vert + ": Cannot step right");
                                                                AddForbidden(-1, 0);
                                                                if (hori - 1 > 1) // example needs to be saved
                                                                {
                                                                    window.errorInWalkthrough = true;
                                                                    window.criticalError = true;
                                                                    window.errorString = "Reverse stair 3 obstacles nextX > 3";
                                                                    return;
                                                                }
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                        else
                                        {
                                            path.RemoveAt(path.Count - 1);
                                            path.RemoveAt(path.Count - 1);
                                            x = x + 2 * lx;
                                            y = y + 2 * ly;
                                        }
                                    }
                                }
                            }
                        }
                    }

                    // rotate CW
                    int s0 = sx;
                    int s1 = sy;
                    sx = -lx;
                    sy = -ly;
                    lx = s0;
                    ly = s1;
                }
                sx = thisSx;
                sy = thisSy;
                lx = -thisLx;
                ly = -thisLy;
            }
            sx = thisSx;
            sy = thisSy;
            lx = thisLx;
            ly = thisLy;
        }

        void Stair3x3() // 2024_0722 / Stair3x3. It is not a nested 3x3 area sequence. 2024_1111 shows, even if we step down, there will be two-way choice later.
        {
            for (int i = 0; i < 2; i++)
            {
                for (int j = 0; j < 2; j++)
                {
                    if (InTakenRel(3, 0) && InTakenRel(5, 1) && InTakenRel(1, 3) && InTakenRel(2, 4) && InTakenRel(3, 5) &&
                        !InTakenRel(2, 0) && !InTakenRel(3, 1) && !InTakenRel(4, 1) && !InTakenRel(5, 2) && !InTakenRel(1, 2) && !InTakenRel(2, 3) && !InTakenRel(3, 4))
                    {
                        int i1 = InTakenIndexRel(3, 0);
                        int i2 = InTakenIndexRel(3, -1);

                        if (i2 > i1)
                        {
                            i1 = InTakenIndexRel(5, 1);
                            i2 = InTakenIndexRel(5, 0);

                            if (i2 > i1)
                            {
                                i1 = InTakenIndexRel(1, 3);
                                i2 = InTakenIndexRel(0, 3);

                                if (i2 > i1)
                                {
                                    T("Stair3x3 at side " + i + " rotation " + j + ": Cannot step left");
                                    AddForbidden(1, 0);
                                }
                            }
                        }
                    }

                    // rotate CW
                    int s0 = sx;
                    int s1 = sy;
                    sx = -lx;
                    sy = -ly;
                    lx = s0;
                    ly = s1;
                }
                sx = thisSx;
                sy = thisSy;
                lx = -thisLx;
                ly = -thisLy;
            }
            sx = thisSx;
            sy = thisSy;
            lx = thisLx;
            ly = thisLy;
        }

        void RemoteStair()
        // 2024_0818_1
        // Find big area corner in the first quarter, mirrored of remote stair.svg. Rotate CCW.
        {
            for (int i = 0; i < 2; i++)
            {
                bool circleDirectionLeft = (i == 0) ? true : false;

                for (int j = 0; j < 4; j++) // normal or small area
                {
                    if (j == 0 || j == 3)
                    {
                        int dist = size;
                        int quarter = quarters[i][j];

                        foreach (int[] corner in closedCorners[quarter])
                        {
                            if (j == 0 && corner[1] == corner[0] + 3)
                            {
                                if (corner[1] < dist) dist = corner[1];
                            }
                            else if (j == 3 && corner[0] == corner[1] + 3)
                            {
                                if (corner[0] < dist) dist = corner[0];
                            }
                        }

                        if (dist < size)
                        {
                            T("RemoteStair distance " + (dist - 1), "side " + i, "rotation " + j);

                            // check line straight up and stair after 3 distance for being empty
                            bool distanceEmpty = true;
                            for (int k = 1; k <= dist - 1; k++)
                            {
                                if (k <= 3)
                                {
                                    if (InTakenRel(0, k)) distanceEmpty = false;
                                }
                                else
                                {
                                    if (InTakenRel(k - 3, k)) distanceEmpty = false;
                                }
                            }

                            if (distanceEmpty)
                            {
                                int hori = dist - 3;
                                int vert = dist;

                                int i1 = InTakenIndexRel(hori, vert);
                                int i2 = InTakenIndexRel(hori + 1, vert);

                                if (i1 > i2) // large area
                                {
                                    int nextX = hori - 1;
                                    int nextY = vert - 1;
                                    bool rightWallFound = false;
                                    bool liveEndPassed = false;

                                    List<int[]> borderFields = new();
                                    int wallX = 0;

                                    int counter = 0;
                                    while (true)
                                    {
                                        counter++;
                                        if (counter == size)
                                        {
                                            T("RemoteStair discovery error.");

                                            window.errorInWalkthrough = true;
                                            window.errorString = "RemoteStair discovery error.";
                                            window.criticalError = true;
                                            return;
                                        }

                                        borderFields.Add(new int[] { nextX, nextY });
                                        borderFields.Add(new int[] { nextX - 1, nextY });

                                        if (InTakenRel(nextX, nextY)) break;

                                        wallX = nextX - 1;
                                        while (!InTakenRel(wallX, nextY))
                                        {
                                            wallX--;
                                        }

                                        if (vert == hori + 3 && nextX == -2 && nextY == 1)
                                        // for live end making a mid across obstacle
                                        {
                                            liveEndPassed = true;
                                        }
                                        /*if (vert == hori + 4 && nextX == -3 && nextY == 1)
                                        // for live end making an across obstacle
                                        {
                                            liveEndPassed = true;
                                        }*/

                                        if (nextX - wallX == 3)
                                        {
                                            rightWallFound = true;
                                            break;
                                        }
                                        else if (nextX - wallX < 3) break;

                                        nextX--;
                                        nextY--;
                                    }

                                    bool takenFound = false;
                                    foreach (int[] field in borderFields)
                                    {
                                        if (InTakenRel(field[0], field[1]))
                                        {
                                            takenFound = true;
                                            break;
                                        }
                                    }

                                    if (rightWallFound && liveEndPassed && !takenFound)
                                    {
                                        // reverse order
                                        List<int[]> newBorderFields = new();
                                        for (int k = borderFields.Count - 1; k >= 0; k--)
                                        {
                                            newBorderFields.Add(borderFields[k]);
                                        }

                                        ResetExamAreas();

                                        if (CountAreaRel(hori - 1, vert, wallX + 1, nextY, newBorderFields, circleDirectionLeft, 2, true))
                                        {
                                            int black = (int)info[1];
                                            int white = (int)info[2];

                                            if (black == white)
                                            {
                                                /*
                                                New function needed?

                                                if (vert == hori + 4)
                                                {
                                                    window.errorInWalkthrough = true;
                                                    window.errorString = "RemoteStair across found.";
                                                    window.criticalError = true;
                                                    return;
                                                }*/

                                                AddExamAreas();

                                                T("RemoteStair mid across: Cannot step straight");
                                                AddForbidden(0, 1);

                                                if (j == 0)
                                                {
                                                    T("RemoteStair mid across: Cannot step left");
                                                    AddForbidden(1, 0);
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }

                    // rotate CW
                    int s0 = sx;
                    int s1 = sy;
                    sx = -lx;
                    sy = -ly;
                    lx = s0;
                    ly = s1;
                }
                sx = thisSx;
                sy = thisSy;
                lx = -thisLx;
                ly = -thisLy;
            }
            sx = thisSx;
            sy = thisSy;
            lx = thisLx;
            ly = thisLy;
        }

        void Sequence()
        // Start at 3, 0. Any combination of stairs and 2-distance straight obstacles are possible. 2 rotations possible.
        // 2024_0706, 2024_1001: corner -> StairAtEndConvexStraight3
        // [no stair] 2024_1005: mid across
        // 2024_0516, 2024_0516_1: one step across -> StairAtEndConvexIn2
        // 2024_0516_4, 2024_0516_5: multiple step across -> DoubleStair
        // 2024_1006: across on the left at the end of the sequence -> DobuleStair
        // 2024_0704, 2024_1014: area on left, close mid across on right -> StairAtEndConvexIn2

        // Start at 3, -1. 3 rotations possible.
        // [no stair] 2024_0516_6, 2024_0516_7, 2024_0516_8: across
        // 2026_0405 -> Next step double area

        // Start at 4, 0
        // 2024_1115: mid across -> DoubleStair   

        // Start at 4, -1
        // 2024_0727_6: across, horizontal distance to the first obstacle % 4 = 3 -> DoubleStair
        // 2024_0724: left across, right mid across -> StairAtStartConvexIn2
        // 2024_0725_2: left area, right mid across -> StairAtStartConvexIn2
        // 2024_0727_3: left mid across, right across -> StairAtStartConvexIn2

        // Start at stair:
        // [no stair] 2024_0630: area
        // [no stair] 2024_0720: across left, mid across right
        // [no stair] 2024_0723, 2024_0723_1: across

        // Obstacle at 3, 0 and 1, -1: Next step to front will create a stair start
        // [no stair] 2024_0724_1: across

        {
            for (int i = 0; i < 2; i++)
            {
                bool circleDirectionLeft = (i == 0) ? true : false;

                for (int j = 0; j < 4; j++)
                {
                    bool startObstacleValid = false;
                    bool vertLow = false;
                    bool stairStart = false;
                    bool stairStart2 = false;
                    int hori = 0;
                    int vert = 0;
                    int i1, i2;

                    if (j < 2)
                    {
                        hori = 1;
                        vert = 0;

                        while (!InTakenRel(hori, vert) && !InBorderRel(hori, vert))
                        {
                            hori++;
                        }

                        if (hori == 3 && !InTakenRel(hori, vert + 1))
                        {
                            i1 = InTakenIndexRel(hori, vert);
                            i2 = InTakenIndexRel(hori, vert - 1);

                            if (i2 != -1 && i2 > i1)
                            {
                                T("Sequence 3 0, side", i, "rotation", j);
                                startObstacleValid = true;
                            }
                        }
                    }

                    if (!startObstacleValid && j < 3)
                    {
                        hori = 1;
                        vert = -1;

                        while (!InTakenRel(hori, vert) && !InBorderRel(hori, vert))
                        {
                            hori++;
                        }

                        if (hori == 3 && !InTakenRel(hori, vert + 1))
                        {
                            i1 = InTakenIndexRel(hori, vert);
                            i2 = InTakenIndexRel(hori, vert - 1);

                            if (i2 != -1 && i2 > i1)
                            {
                                T("Sequence 3 -1, side", i, "rotation", j);
                                startObstacleValid = true;
                                vertLow = true;
                            }
                        }
                        else if (hori == 4 && !InTakenRel(hori, vert + 1))
                        {
                            i1 = InTakenIndexRel(hori, vert);
                            i2 = InTakenIndexRel(hori, vert - 1);

                            if (i2 != -1 && i2 > i1)
                            {
                                T("Sequence 4 -1, side", i, "rotation", j);
                                startObstacleValid = true;
                                vertLow = true;
                            }
                        }

                        if (!startObstacleValid)
                        {
                            hori = 1;
                            vert = 0;

                            while (!InTakenRel(hori, vert) && !InBorderRel(hori, vert))
                            {
                                hori++;
                            }

                            if (hori == 4 && !InTakenRel(hori, vert + 1))
                            {
                                i1 = InTakenIndexRel(hori, vert);
                                i2 = InTakenIndexRel(hori, vert - 1);

                                if (i2 != -1 && i2 > i1)
                                {
                                    T("Sequence 4 0, side", i, "rotation", j);
                                    startObstacleValid = true;
                                    vertLow = true;
                                }
                            }
                        }
                    }

                    // stair start, 2024_0630, 2024_0720, 2024_0723, 2024_0723_1
                    if (!startObstacleValid && j == 0 || j == 3)
                    {
                        if (InTakenRel(1, 0) && InTakenRel(2, 1) && !InTakenRel(1, 1))
                        {
                            T("Sequence stair, side", i, "rotation", j);
                            startObstacleValid = true;
                            stairStart = true;
                            hori = 2;
                            vert = 1;
                        }
                    }

                    // 2024_0724_1
                    if (!startObstacleValid && j < 2)
                    {
                        if (InTakenRel(0, 3) && !InTakenRel(1, 3) && !InTakenRel(0, 2) && !InTakenRel(1, 0) && InTakenRel(1, -1))
                        {
                            i1 = InTakenIndexRel(0, 3);
                            i2 = InTakenIndexRel(-1, 3);

                            if (i1 > i2)
                            {
                                T("Sequence stair 2, side", i, "rotation", j);
                                startObstacleValid = true;
                                stairStart2 = true;
                                hori = 1;
                                vert = -1;
                            }
                        }
                    }

                    if (startObstacleValid)
                    {
                        bool sequenceValid = false;

                        if (!stairStart && !stairStart2)
                        {
                            if (hori == 4 && (vert == -1 || vert == 0)) // 2024_0727_6. We need to think about a general UpExtended start area where distance to the obstacle % 4 = 3.
                            {
                                List<int[]> borderFields = new();
                                borderFields.Add(new int[] { 2, 0 });

                                if (CountAreaRel(1, 0, 3, 0, borderFields, circleDirectionLeft, 2, true))
                                {
                                    int black = (int)info[1];
                                    int white = (int)info[2];

                                    // the area needs to be B = W in order to exit at 2, 0 after entry left or down
                                    if (black == white)
                                    {
                                        T("areaUp area counted, black = white");
                                        sequenceValid = true;
                                    }
                                }
                            }
                            else
                            {
                                if (CountAreaRel(1, vert, 2, vert, null, circleDirectionLeft, vert == 0 ? 2 : 3, true))
                                {
                                    int black = (int)info[1];
                                    int white = (int)info[2];

                                    // the area needs to be B = W in order to exit at 2, 0 after entry left or down
                                    if (black == white)
                                    {
                                        T("straight area counted, black = white");
                                        sequenceValid = true;
                                    }
                                }
                            }
                        }
                        else
                        {
                            sequenceValid = true;
                        }

                        if (sequenceValid)
                        {
                            bool stepFound = true;
                            bool farStraightFound = true;
                            List<int[]> rotations = new List<int[]> { new int[] { 1, 1 }, new int[] { -1, 1 }, new int[] { -1, -1 }, new int[] { 1, -1 } };
                            int rotationIndex = 0;
                            // Extensions by rotation:
                            // 1, 1
                            // -1, 1
                            // -1, -1
                            // 1, -1

                            int counter = 1;
                            if (!stairStart && !stairStart2)
                            {
                                if (hori == 4 && (vert == -1 || vert == 0)) // 2024_0727_6
                                {
                                    path.Add(new int[] { x + lx, y + ly });
                                    path.Add(new int[] { x + 3 * lx, y + 3 * ly });
                                    path.Add(new int[] { x + 2 * lx + sx, y + 2 * ly + sy });
                                    counter = 3;
                                    hori = 3;
                                    vert = 0;
                                }
                                else // 2024_1006
                                {
                                    path.Add(new int[] { x + lx, y + ly });
                                }
                            }
                            else if (stairStart)
                            {
                                // Add straight field and left up (the second field needs to be added to establish found corner direction later in the sequence: 2024_0723, 2024_0723_1
                                path.Add(new int[] { x + sx, y + sy });
                                path.Add(new int[] { x + lx + sx, y + ly + sy });
                                counter++;
                            }
                            else
                            { // 2024_0724_1                                
                                path.Add(new int[] { x + lx + sx, y + ly + sy });
                                T("Added start 0", path[path.Count - 1][0], path[path.Count - 1][1]);
                                path.Add(new int[] { x + lx, y + ly });
                                counter++;
                                rotationIndex = 3;
                            }

                            T("Added start", path[path.Count - 1][0], path[path.Count - 1][1], "at", counter, "hori " + hori, "vert " + vert);

                            int limitCounter = 0;
                            // start at hori 3, vert 0
                            while (stepFound || farStraightFound)
                            {
                                limitCounter++;
                                if (limitCounter == size * size)
                                {
                                    T("Sequence limit.");

                                    window.errorInWalkthrough = true;
                                    window.criticalError = true;
                                    window.errorString = "Sequence limit";
                                    return;
                                }

                                stepFound = false;
                                farStraightFound = false;

                                // new imaginary step (obstacle placement of a stair)
                                hori += rotations[rotationIndex][0]; // 4
                                vert += rotations[rotationIndex][1]; // 1

                                // 4, 1 should be taken. 3, 1 should be free
                                // OR
                                // 3, 3 should be taken. 3, 2 and 3, 1 should be free
                                // both stair and far straight can be true at the same time, but far straight sets the new direction

                                int hx = 0;
                                int hy = 0;
                                int vx = 0;
                                int vy = 0;
                                switch (rotationIndex)
                                {
                                    case 0:
                                        hx = 1;
                                        hy = 0;
                                        vx = 0;
                                        vy = 1;
                                        break;
                                    case 1:
                                        hx = 0;
                                        hy = 1;
                                        vx = -1;
                                        vy = 0;
                                        break;
                                    case 2:
                                        hx = -1;
                                        hy = 0;
                                        vx = 0;
                                        vy = -1;
                                        break;
                                    case 3:
                                        hx = 0;
                                        hy = -1;
                                        vx = 1;
                                        vy = 0;
                                        break;
                                }

                                if (InTakenRel(hori - hx + 2 * vx, vert - hy + 2 * vy) && !InTakenRel(hori - hx + vx, vert - hy + vy) && !InTakenRel(hori - hx, vert - hy))
                                {
                                    i1 = InTakenIndexRel(hori - hx + 2 * vx, vert - hy + 2 * vy);
                                    i2 = InTakenIndexRel(hori + 2 * vx, vert + 2 * vy);

                                    if (i2 != -1 && i2 > i1)
                                    {
                                        farStraightFound = true;
                                    }
                                }

                                if (!farStraightFound && InTakenRel(hori, vert) && !InTakenRel(hori - hx, vert - hy))
                                {
                                    stepFound = true;
                                }

                                ResetExamAreas();

                                // 2024_0704, 2024_1014, 2024_0724, 2024_0725_2: double area at first step. For subsequent steps, rotation has to be changed from 0 to its actual value.
                                // 2024_1006: double area after many steps

                                int rotationIndex2 = 0;
                                int rotationIndex3 = 0; // for use in the following functions only. Their rotation order is upper left, lower left, upper right and lower right.
                                switch (rotationIndex)
                                {
                                    case 0:
                                        rotationIndex2 = 0;
                                        rotationIndex3 = 0;
                                        break;
                                    case 1:
                                        rotationIndex2 = 2;
                                        rotationIndex3 = 1;
                                        break;
                                    case 2:
                                        rotationIndex2 = 3;
                                        rotationIndex3 = 3;
                                        break;
                                    case 3:
                                        rotationIndex2 = 1;
                                        rotationIndex3 = 2;
                                        break;
                                }

                                if (CheckCorner(hori - 2 * hx, vert - 2 * hy, 0, rotationIndex2, circleDirectionLeft, true) && CheckNearFieldSmallRel0(hori - 2 * hx, vert - 2 * hy, 1, rotationIndex3, true))
                                {
                                    AddExamAreas(true);

                                    for (int m = 1; m <= counter; m++)
                                    {
                                        path.RemoveAt(path.Count - 1);
                                    }
                                    counter = 0;

                                    if (!stairStart) // 2024_0704, 2024_1014, 2024_0724, 2024_0725_2, 2024_1006, 2026_0404, 2026_0405
                                    {
                                        T("Sequence double area at relative " + (hori - 2 * hx) + " " + (vert - 2 * hy) + ": Cannot step left");
                                        AddForbidden(1, 0);

                                        // down direction should not be disabled: 2026_0405
                                    }
                                    else // 2024_0720
                                    {
                                        T("Sequence double area stair start at relative " + (hori - 2 * hx) + " " + (vert - 2 * hy) + ": Cannot step straight");
                                        AddForbidden(0, 1);
                                    }

                                    break;
                                }

                                T("hori", hori, "vert", vert, "straightFound", farStraightFound, "stepFound", stepFound);

                                // 2025_0516, C-shape ahead on the right
                                // Checking for empty field ahead is probably not necessary
                                // Only apply for stairStart2 for now.
                                if (stepFound && InTakenRel(hori - 2 * hx + 2 * vx, vert - 2 * hy + 2 * vy) && InTakenRel(hori - 3 * hx + vx, vert - 3 * hy + vy) && stairStart2)
                                {
                                    T("Sequence double C-shape at relative " + (hori - 2 * hx) + " " + (vert - 2 * hy) + " stairStart2: Cannot step straight");
                                    AddForbidden(0, 1);

                                    break;
                                }

                                if (farStraightFound || stepFound)
                                {
                                    switch (rotationIndex)
                                    {
                                        case 0:
                                            // Add 2, 1
                                            path.Add(new int[] { x + (hori - 2) * lx + vert * sx, y + (hori - 2) * ly + vert * sy });
                                            break;
                                        case 1:
                                            path.Add(new int[] { x + hori * lx + (vert - 2) * sx, y + hori * ly + (vert - 2) * sy });
                                            break;
                                        case 2:
                                            path.Add(new int[] { x + (hori + 2) * lx + vert * sx, y + (hori + 2) * ly + vert * sy });
                                            break;
                                        case 3:
                                            path.Add(new int[] { x + hori * lx + (vert + 2) * sx, y + hori * ly + (vert + 2) * sy });
                                            break;
                                    }
                                    counter++;

                                    int nearFieldRotation = 0;
                                    switch (rotationIndex)
                                    {
                                        case 0:
                                            nearFieldRotation = 0;
                                            break;
                                        case 1:
                                            nearFieldRotation = 1;
                                            break;
                                        case 2:
                                            nearFieldRotation = 3;
                                            break;
                                        case 3:
                                            nearFieldRotation = 2;
                                            break;
                                    }

                                    T("Added", path[path.Count - 1][0], path[path.Count - 1][1], "at", counter);

                                    ResetExamAreas();

                                    if (CheckCorner(hori - 2 * hx, vert - 2 * hy, 1, nearFieldRotation, circleDirectionLeft, true))
                                    {
                                        AddExamAreas(true);

                                        for (int m = 1; m <= counter; m++)
                                        {
                                            path.RemoveAt(path.Count - 1);
                                        }
                                        counter = 0;

                                        if (!stairStart && !stairStart2)
                                        {
                                            T("Sequence at relative " + (hori - 2 * hx) + " " + (vert - 2 * hy) + ": Cannot step left");
                                            AddForbidden(1, 0);

                                            if (j == 1 && !vertLow)
                                            {
                                                T("Sequence at relative " + (hori - 2 * hx) + " " + (vert - 2 * hy) + ": Cannot step down");
                                                AddForbidden(0, -1);
                                            }
                                        }
                                        else if (stairStart)
                                        {
                                            T("Sequence at relative " + (hori - 2 * hx) + " " + (vert - 2 * hy) + " stairStart: Cannot step straight");
                                            AddForbidden(0, 1);
                                        }
                                        else
                                        {
                                            T("Sequence at relative " + (hori - 2 * hx) + " " + (vert - 2 * hy) + " stairStart2: Cannot step straight");
                                            AddForbidden(0, 1);
                                        }

                                        break;
                                    }
                                }

                                if (farStraightFound)
                                {
                                    switch (rotationIndex)
                                    {
                                        case 0:
                                            hori = hori - 1;
                                            vert = vert + 2;
                                            break;
                                        case 1:
                                            hori = hori - 2;
                                            vert = vert - 1;
                                            break;
                                        case 2:
                                            hori = hori + 1;
                                            vert = vert - 2;
                                            break;
                                        case 3:
                                            hori = hori + 2;
                                            vert = vert + 1;
                                            break;
                                    }
                                    rotationIndex = rotationIndex < 3 ? rotationIndex + 1 : 0;
                                }

                                T("New rotationIndex", rotationIndex, "hori", hori, "vert", vert);
                            }

                            for (int m = 1; m <= counter; m++)
                            {
                                path.RemoveAt(path.Count - 1);
                            }
                        }
                    }

                    // rotate CW
                    int s0 = sx;
                    int s1 = sy;
                    sx = -lx;
                    sy = -ly;
                    lx = s0;
                    ly = s1;
                }
                sx = thisSx;
                sy = thisSy;
                lx = -thisLx;
                ly = -thisLy;
            }
            sx = thisSx;
            sy = thisSy;
            lx = thisLx;
            ly = thisLy;
        }

        void CornerDiscoveryAll()
        {
            int coordStart;
            bool cornerReached = false;
            bool liveEndReached = false; // It is not enough to reach the corner before getting back to the walkthrough start. See 2024_0823
            coordStart = 2;
            int liveNearX;
            int liveNearY;
            int startX;
            int startY;
            int nextX;
            int nextY;
            int currentDirection;

            if (!InTakenRel(1, 0) && !InBorderRel(1, 0)) // left
            {
                liveNearX = 1;
                liveNearY = 0;

                while (!InTakenRel(coordStart, 0) && !InBorderRel(coordStart, 0))
                {
                    coordStart++;
                }

                currentDirection = 0;
                nextX = coordStart - 1;
                nextY = 0;
            }
            else if (!InTakenRel(0, 1) && !InBorderRel(0, 1)) // up
            {
                liveNearX = 0;
                liveNearY = 1;

                while (!InTakenRel(0, coordStart) && !InBorderRel(0, coordStart))
                {
                    coordStart++;
                }

                currentDirection = 3;
                nextX = 0;
                nextY = coordStart - 1;
            }
            else // right
            {
                liveNearX = -1;
                liveNearY = 0;

                while (!InTakenRel(-coordStart, 0) && !InBorderRel(-coordStart, 0))
                {
                    coordStart++;
                }

                currentDirection = 2;
                nextX = coordStart - 1;
                nextY = 0;
            }

            startX = nextX;
            startY = nextY;
            int counter = 0;

            while (!(cornerReached && liveEndReached && nextX == startX && nextY == startY))
            {
                //T("nextX " + nextX, "nextY " + nextY);

                counter++;
                if (counter == size * size)
                {
                    T("Corner discovery error.");

                    window.errorInWalkthrough = true;
                    window.errorString = "Corner discovery error.";
                    window.criticalError = true;
                    return;
                }

                // left direction
                int leftDirection = (currentDirection == 3) ? 0 : currentDirection + 1;
                currentDirection = leftDirection;
                int possibleNextX = nextX + directions[leftDirection][0];
                int possibleNextY = nextY + directions[leftDirection][1];

                int counter2 = 0;
                // turn right until a field is empty 
                while (InBorderRel(possibleNextX, possibleNextY) || InTakenRel(possibleNextX, possibleNextY))
                {
                    counter2++;
                    if (counter2 == 4)
                    {
                        T("Corner discovery error 2.");

                        window.errorInWalkthrough = true;
                        window.errorString = "Corner discovery error 2.";
                        window.criticalError = true;
                        return;
                    }

                    leftDirection = (leftDirection == 0) ? 3 : leftDirection - 1;
                    possibleNextX = nextX + directions[leftDirection][0];
                    possibleNextY = nextY + directions[leftDirection][1];
                }

                // first quarter
                if (nextX >= 0 && nextY >= 0 && currentDirection == 0 && leftDirection == 0)
                {
                    closedCorners[0].Add(new int[] { nextX + 1, nextY + 1 });
                }
                else if (nextX >= 0 && nextY >= 1 && currentDirection == 1 && leftDirection == 1)
                {
                    openCWCorners[0].Add(new int[] { nextX + 1, nextY - 1 });
                }
                else if (nextX >= 1 && nextY >= 0 && currentDirection == 3 && leftDirection == 3)
                {
                    openCCWCorners[0].Add(new int[] { nextX - 1, nextY + 1 });
                }

                // second quarter
                if (nextX <= 0 && nextY >= 0 && currentDirection == 3 && leftDirection == 3)
                {

                    closedCorners[1].Add(new int[] { quarterMultipliers[1][0] * (nextX - 1), quarterMultipliers[1][1] * (nextY + 1) });
                }
                else if (nextX <= -1 && nextY >= 0 && currentDirection == 0 && leftDirection == 0)
                {
                    openCWCorners[1].Add(new int[] { quarterMultipliers[1][0] * (nextX + 1), quarterMultipliers[1][1] * (nextY + 1) });
                }
                else if (nextX <= 0 && nextY >= 1 && currentDirection == 2 && leftDirection == 2)
                {
                    openCCWCorners[1].Add(new int[] { quarterMultipliers[1][0] * (nextX - 1), quarterMultipliers[1][1] * (nextY - 1) });
                }

                // third quarter
                if (nextX <= 0 && nextY <= 0 && currentDirection == 2 && leftDirection == 2)
                {
                    closedCorners[2].Add(new int[] { quarterMultipliers[2][0] * (nextX - 1), quarterMultipliers[2][1] * (nextY - 1) });
                }
                else if (nextX <= 0 && nextY <= -1 && currentDirection == 3 && leftDirection == 3)
                {
                    openCWCorners[2].Add(new int[] { quarterMultipliers[2][0] * (nextX - 1), quarterMultipliers[2][1] * (nextY + 1) });
                }
                else if (nextX <= -1 && nextY <= 0 && currentDirection == 1 && leftDirection == 1)
                {
                    openCCWCorners[2].Add(new int[] { quarterMultipliers[2][0] * (nextX + 1), quarterMultipliers[2][1] * (nextY - 1) });
                }

                // fourth quarter
                if (nextX >= 0 && nextY <= 0 && currentDirection == 1 && leftDirection == 1)
                {
                    closedCorners[3].Add(new int[] { quarterMultipliers[3][0] * (nextX + 1), quarterMultipliers[3][1] * (nextY - 1) });
                }
                else if (nextX >= 1 && nextY <= 0 && currentDirection == 2 && leftDirection == 2)
                {
                    openCWCorners[3].Add(new int[] { quarterMultipliers[3][0] * (nextX - 1), quarterMultipliers[3][1] * (nextY - 1) });
                }
                else if (nextX >= 0 && nextY <= -1 && currentDirection == 0 && leftDirection == 0)
                {
                    openCCWCorners[3].Add(new int[] { quarterMultipliers[3][0] * (nextX + 1), quarterMultipliers[3][1] * (nextY + 1) });
                }

                currentDirection = leftDirection;

                nextX = possibleNextX;
                nextY = possibleNextY;

                if (InCornerRel(nextX, nextY)) cornerReached = true;
                if (nextX == liveNearX && nextY == liveNearY) liveEndReached = true;
            }

            T("Closed corners:");
            foreach (int[] corner in closedCorners[0])
            {
                T("0: " + corner[0] + " " + corner[1]);
            }
            foreach (int[] corner in closedCorners[1])
            {
                T("1: " + corner[0] + " " + corner[1]);
            }
            foreach (int[] corner in closedCorners[2])
            {
                T("2: " + corner[0] + " " + corner[1]);
            }
            foreach (int[] corner in closedCorners[3])
            {
                T("3: " + corner[0] + " " + corner[1]);
            }
            T("Open CW corners:");
            foreach (int[] corner in openCWCorners[0])
            {
                T("0: " + corner[0] + " " + corner[1]);
            }
            foreach (int[] corner in openCWCorners[1])
            {
                T("1: " + corner[0] + " " + corner[1]);
            }
            foreach (int[] corner in openCWCorners[2])
            {
                T("2: " + corner[0] + " " + corner[1]);
            }
            foreach (int[] corner in openCWCorners[3])
            {
                T("3: " + corner[0] + " " + corner[1]);
            }
            T("Open CCW corners:");
            foreach (int[] corner in openCCWCorners[0])
            {
                T("0: " + corner[0] + " " + corner[1]);
            }
            foreach (int[] corner in openCCWCorners[1])
            {
                T("1: " + corner[0] + " " + corner[1]);
            }
            foreach (int[] corner in openCCWCorners[2])
            {
                T("2: " + corner[0] + " " + corner[1]);
            }
            foreach (int[] corner in openCCWCorners[3])
            {
                T("3: " + corner[0] + " " + corner[1]);
            }
        }

        bool CheckCorner(int left, int straight, int side, int rotation, bool circleDirectionLeft, bool smallArea)
        {
            x2 = x + left * lx + straight * sx;
            y2 = y + left * ly + straight * sy;
            path.Add(new int[] { x2, y2 });

            if (side == 0)
            {
                switch (rotation)
                {
                    case 0: // straight
                        lx2 = lx;
                        ly2 = ly;
                        sx2 = sx;
                        sy2 = sy;
                        break;
                    case 1: // small area
                        lx2 = -sx;
                        ly2 = -sy;
                        sx2 = lx;
                        sy2 = ly;
                        break;
                    case 2: // big area
                        lx2 = sx;
                        ly2 = sy;
                        sx2 = -lx;
                        sy2 = -ly;
                        break;
                    case 3: // big big area
                        lx2 = -lx;
                        ly2 = -ly;
                        sx2 = -sx;
                        sy2 = -sy;
                        break;
                }
            }
            else
            {
                switch (rotation)
                {
                    case 0: // straight
                        lx2 = -lx;
                        ly2 = -ly;
                        sx2 = sx;
                        sy2 = sy;
                        break;
                    case 1: // small area
                        lx2 = -sx;
                        ly2 = -sy;
                        sx2 = -lx;
                        sy2 = -ly;
                        break;
                    case 2: // big area
                        lx2 = sx;
                        ly2 = sy;
                        sx2 = lx;
                        sy2 = ly;
                        break;
                    case 3: // big big area
                        lx2 = lx;
                        ly2 = ly;
                        sx2 = -sx;
                        sy2 = -sy;
                        break;
                }
            }

            circleDirectionLeft = (side == 0) ? circleDirectionLeft : !circleDirectionLeft;

            // 1, 1 relative field cannot be taken
            int horiStart = 1;

            int counter0 = 0;

            while (!InTakenRel2(horiStart, 1) && !InBorderRel2(horiStart, 1))
            {
                counter0++;
                if (counter0 == size)
                {
                    T("Corner2 0 discovery error.");

                    window.errorInWalkthrough = true;
                    window.errorString = "Corner2 0 discovery error.";
                    window.criticalError = true;
                    return false;
                }

                horiStart++;
            }

            if (horiStart >= 2) // at least left field has to be empty
            {
                int currentDirection = 0;

                int nextX = horiStart - 1;
                int nextY = 1;

                int counter = 0;
                //T("nextX", nextX, nextY, circleDirectionLeft, x2, y2, lx2, ly2);
                while (!(nextX < 0 && nextY >= 1) && !InCornerRel2(nextX, nextY) && !(counter > 0 && nextX == horiStart - 1 && nextY == 1))
                { // First condition: Includes AreaUp. The closed area might go below and to -1 horizontal position.
                  // Second condition: 2024_0708_1: Finish corner is reached, there cannot be small area from there.
                  // Third condition: 2024_0708_2: We never get to -1 horizontal position, the area is closed. When we get to the first square again, break the cycle.

                    //T("nextX", nextX, nextY);
                    counter++;
                    if (counter == size * size)
                    {
                        T("Corner2 discovery error.");

                        window.errorInWalkthrough = true;
                        window.errorString = "Corner2 discovery error.";
                        window.criticalError = true;
                        return false;
                    }

                    // left direction
                    currentDirection = (currentDirection == 3) ? 0 : currentDirection + 1;
                    int l = currentDirection;
                    int possibleNextX = nextX + directions[currentDirection][0];
                    int possibleNextY = nextY + directions[currentDirection][1];

                    int counter1 = 0;

                    // turn right until a field is empty 
                    while (InBorderRel2(possibleNextX, possibleNextY) || InTakenRel2(possibleNextX, possibleNextY))
                    {
                        counter1++;
                        if (counter1 == size * size)
                        {
                            T("Corner2 1 discovery error.");

                            window.errorInWalkthrough = true;
                            window.errorString = "Corner2 1 discovery error.";
                            window.criticalError = true;
                            return false;
                        }

                        l = (l == 0) ? 3 : l - 1;
                        possibleNextX = nextX + directions[l][0];
                        possibleNextY = nextY + directions[l][1];
                    }

                    if (currentDirection == 0 && l == 0 && nextY >= 1) // 2024_0708: Corner can be found beneath
                    {
                        int hori = nextX + 1;
                        int vert = nextY + 1;

                        T("Corner at", hori, vert, "x2", x2, "y2", y2, "lx2", lx2, "ly2", ly2, "circleDirectionLeft", circleDirectionLeft);

                        bool circleValid = false;
                        List<int[]> borderFields = new();

                        int i1, i2;

                        i1 = InTakenIndexRel2(hori, vert);
                        i2 = InTakenIndexRel2(hori + 1, vert);

                        if (smallArea && i2 > i1 || !smallArea && i2 < i1)
                        {
                            if (sequenceLeftObstacleIndex != -1)
                            {
                                if (i1 < sequenceLeftObstacleIndex) circleValid = true;
                            }
                            else
                            {
                                circleValid = true;
                            }
                        }

                        if (circleValid)
                        {
                            if (hori == 1 && vert == 2) // close mid across
                            {
                                path.RemoveAt(path.Count - 1);
                                return true;

                            }
                            else if (hori == 2 && vert == 2) // close across
                            {
                                path.RemoveAt(path.Count - 1);
                                return true;
                            }
                            else if (hori == 1) // AreaUp
                            {
                                /* Example needed
                                int ex = vert - 1;

                                if (ex > 2)
                                {
                                    for (int k = ex - 1; k >= 2; k--)
                                    {
                                        borderFields.Add(new int[] { 1, k });
                                    }
                                }

                                ResetExamAreas();

                                if (CountAreaRel2(1, 1, 1, vert - 1, borderFields, circleDirectionLeft, 2, true))
                                {
                                    int black = (int)info[1];
                                    int white = (int)info[2];

                                    int whiteDiff = white - black;
                                    int nowWCount = 0;
                                    int nowBCount = 0;
                                    int laterWCount = 0;
                                    int laterBCount = 0;

                                    switch (ex % 4)
                                    {
                                        case 0:
                                            nowWCount = ex / 4;
                                            nowBCount = ex / 4 - 1;
                                            laterWCount = ex / 4;
                                            laterBCount = ex / 4;
                                            break;
                                        case 1:
                                            nowWCount = (ex - 1) / 4;
                                            nowBCount = (ex - 1) / 4;
                                            laterWCount = (ex - 1) / 4;
                                            laterBCount = (ex - 1) / 4;
                                            break;
                                        case 2:
                                            nowWCount = (ex + 2) / 4;
                                            nowBCount = (ex - 2) / 4;
                                            laterWCount = (ex - 2) / 4;
                                            laterBCount = (ex - 2) / 4;
                                            break;
                                        case 3:
                                            nowWCount = (ex + 1) / 4;
                                            nowBCount = (ex - 3) / 4;
                                            laterWCount = (ex - 3) / 4;
                                            laterBCount = (ex + 1) / 4;
                                            break;
                                    }

                                    if (!(whiteDiff <= laterWCount && whiteDiff >= -laterBCount))
                                    {
                                        T("Corner2: Cannot enter later");
                                        return true;
                                    }
                                }*/
                            }
                            else // Corner 2024_0627
                            {
                                bool takenFound = false;
                                int left1 = 1;
                                int straight1 = 1;
                                int left2 = hori - 1;
                                int straight2 = vert - 1;

                                int nowWCount, nowWCountDown, nowBCount, laterWCount, laterBCount;
                                int a, n;

                                //check if all fields on the border line is free
                                if (vert == hori)
                                {
                                    a = hori - 1;
                                    nowWCountDown = nowWCount = 0;
                                    nowBCount = a - 1;
                                    laterWCount = -1;// means B = 1
                                    laterBCount = a - 1;

                                    for (int k = 1; k < hori; k++)
                                    {
                                        if (k < hori - 1)
                                        {
                                            if (InTakenRel2(k, k) || InTakenRel2(k + 1, k))
                                            {
                                                takenFound = true;
                                                break;
                                            }
                                        }
                                        else
                                        {
                                            if (InTakenRel2(k, k))
                                            {
                                                takenFound = true;
                                                break;
                                            }
                                        }

                                        if (k == 1)
                                        {
                                            borderFields.Add(new int[] { 2, 1 });
                                        }
                                        else if (k < hori - 1)
                                        {
                                            borderFields.Add(new int[] { k, k });
                                            borderFields.Add(new int[] { k + 1, k });
                                        }
                                    }
                                }
                                else if (hori > vert)
                                {
                                    a = vert - 1;
                                    n = (hori - vert - (hori - vert) % 2) / 2;

                                    if ((hori - vert) % 2 == 0)
                                    {
                                        if (n > 1)
                                        {
                                            nowWCountDown = nowWCount = (n + 1 - (n + 1) % 2) / 2;
                                        }
                                        else
                                        {
                                            nowWCount = 0;
                                            nowWCountDown = 1;
                                        }
                                        nowBCount = a + (n - 1 - (n - 1) % 2) / 2;
                                        laterWCount = (n - n % 2) / 2;
                                        laterBCount = a + (n - n % 2) / 2;
                                    }
                                    else
                                    {
                                        if (n > 0)
                                        {
                                            nowWCountDown = nowWCount = a + (n - n % 2) / 2;
                                            laterBCount = (n + 2 - (n + 2) % 2) / 2;
                                        }
                                        else
                                        {
                                            nowWCount = a - 1;
                                            nowWCountDown = a;
                                            laterBCount = 0;
                                        }
                                        nowBCount = (n + 1 - (n + 1) % 2) / 2;
                                        laterWCount = a - 1 + (n + 1 - (n + 1) % 2) / 2;

                                    }

                                    for (int k = 1; k <= hori - vert; k++)
                                    {
                                        if (InTakenRel2(k, 1))
                                        {
                                            takenFound = true;
                                            break;
                                        }

                                        if (k > 1)
                                        {
                                            borderFields.Add(new int[] { k, 1 });
                                        }
                                    }

                                    for (int k = 1; k < vert; k++)
                                    {
                                        if (k < vert - 1)
                                        {
                                            if (InTakenRel2(hori - vert + k, k) || InTakenRel2(hori - vert + k + 1, k))
                                            {
                                                takenFound = true;
                                                break;
                                            }
                                        }
                                        else
                                        {
                                            if (InTakenRel2(hori - vert + k, k))
                                            {
                                                takenFound = true;
                                                break;
                                            }
                                        }

                                        if (k < vert - 1)
                                        {
                                            borderFields.Add(new int[] { hori - vert + k, k });
                                            borderFields.Add(new int[] { hori - vert + k + 1, k });
                                        }
                                    }
                                }
                                else // vert > hori
                                {
                                    a = hori - 1;
                                    n = (vert - hori - (vert - hori) % 2) / 2;

                                    if ((vert - hori) % 2 == 0)
                                    {
                                        nowWCountDown = nowWCount = (n + 1 - (n + 1) % 2) / 2;
                                        nowBCount = a + (n - 1 - (n - 1) % 2) / 2;
                                        laterWCount = (n - n % 2) / 2;
                                        laterBCount = a + (n - n % 2) / 2;
                                    }
                                    else
                                    {
                                        nowWCountDown = nowWCount = 1 + (n + 1 - (n + 1) % 2) / 2;
                                        nowBCount = a - 1 + (n - n % 2) / 2;
                                        if (n > 0)
                                        {
                                            laterWCount = 1 + (n - n % 2) / 2;
                                        }
                                        else
                                        {
                                            laterWCount = 0;
                                        }
                                        laterBCount = a - 1 + (n + 1 - (n + 1) % 2) / 2;
                                    }

                                    for (int k = 1; k < hori; k++)
                                    {
                                        if (k < hori - 1 && hori > 2)
                                        {
                                            if (InTakenRel2(k, k) || InTakenRel2(k + 1, k))
                                            {
                                                takenFound = true;
                                                break;
                                            }
                                        }
                                        else
                                        {
                                            if (InTakenRel2(k, k))
                                            {
                                                takenFound = true;
                                                break;
                                            }
                                        }

                                        if (hori > 2) // there is no stair if corner is at 1 distance, only one field which is the start field.
                                        {
                                            if (k == 1)
                                            {
                                                borderFields.Add(new int[] { 2, 1 });
                                            }
                                            else if (k < hori - 1)
                                            {
                                                borderFields.Add(new int[] { k, k });
                                                borderFields.Add(new int[] { k + 1, k });
                                            }
                                            else
                                            {
                                                borderFields.Add(new int[] { k, k });
                                            }
                                        }
                                    }

                                    for (int k = 1; k <= vert - hori; k++)
                                    {
                                        if (InTakenRel2(hori - 1, hori - 1 + k))
                                        {
                                            takenFound = true;
                                            break;
                                        }

                                        if (k < vert - hori)
                                        {
                                            borderFields.Add(new int[] { hori - 1, hori - 1 + k });
                                        }
                                    }
                                }

                                if (!takenFound)
                                {
                                    // reverse order
                                    List<int[]> newBorderFields = new();
                                    for (int k = borderFields.Count - 1; k >= 0; k--)
                                    {
                                        newBorderFields.Add(borderFields[k]);
                                    }

                                    ResetExamAreas2();

                                    if (CountAreaRel2(left1, straight1, left2, straight2, newBorderFields, circleDirectionLeft, 2, true))
                                    {
                                        int black = (int)info[1];
                                        int white = (int)info[2];

                                        int whiteDiff = white - black;

                                        if (!(whiteDiff <= laterWCount && whiteDiff >= -laterBCount))
                                        {
                                            T("Corner1: Cannot enter later");
                                            path.RemoveAt(path.Count - 1);
                                            return true;
                                        }
                                    }
                                }
                            }
                        }
                    }

                    currentDirection = l;

                    nextX = possibleNextX;
                    nextY = possibleNextY;
                }
            }

            path.RemoveAt(path.Count - 1);
            return false;
        }

        bool CheckNearFieldSmallRel0(int x, int y, int side, int rotation, bool smallArea)
        { // close mid across only
          // obstacle right side of the field in question, area up
          // mid across and across fields
          // used for LeftRightAreaUp and LeftRightCorner
            int lx = 1;
            int ly = 0;
            int sx = 0;
            int sy = 1;

            // Mid across obstacle:
            // left side:
            // 1, 2
            // 2, -1
            // -2, 1

            // right side:
            // -1, 2
            // -2, -1
            // 2, 1

            // direction checkng is not necessary when the close obstacle is inside the area, but it is when the obstacle is at the exit point of the area. 9_1023055626

            for (int i = 0; i < 2; i++)
            {
                for (int j = 0; j < 4; j++) // j = 0: middle, j = 1: small area, j = 2: big area, j = 3: big (right down) area
                {
                    if (i == side && j == rotation)
                    {
                        // close mid across
                        if (InTakenRel(x + lx + 2 * sx, y + ly + 2 * sy) && !InTakenRel(x + 2 * sx, y + 2 * sy) && !InTakenRel(x + lx + sx, y + ly + sy))
                        {

                            int i1 = InTakenIndexRel(x + 1 * lx + 2 * sx, y + 1 * ly + 2 * sy);
                            int i2 = InTakenIndexRel(x + 2 * lx + 2 * sx, y + 2 * ly + 2 * sy);

                            if (smallArea && i2 > i1 || !smallArea && i2 < i1) return true;
                        }
                    }

                    if (j == 0) // rotate down (CCW): small area
                    {
                        int l0 = lx;
                        int l1 = ly;
                        lx = -sx;
                        ly = -sy;
                        sx = l0;
                        sy = l1;
                    }
                    else if (j == 1) // rotate up (CW): big area
                    {
                        lx = -lx;
                        ly = -ly;
                        sx = -sx;
                        sy = -sy;
                    }
                    else // rotate CW again
                    {
                        int s0 = sx;
                        int s1 = sy;
                        sx = -lx;
                        sy = -ly;
                        lx = s0;
                        ly = s1;
                    }
                }
                lx = -1;
                ly = 0;
                sx = 0;
                sy = 1;
            }
            return false;
        }

        bool CheckNearFieldSmallRel1(int x, int y, int side, int rotation, bool smallArea)
        { // close mid across and across only
          // obstacle right side of the field in question, area up
          // mid across and across fields
          // used for LeftRightAreaUp and LeftRightCorner
            int lx = 1;
            int ly = 0;
            int sx = 0;
            int sy = 1;

            // Mid across obstacle:
            // left side:
            // 1, 2
            // 2, -1
            // -2, 1

            // right side:
            // -1, 2
            // -2, -1
            // 2, 1

            // direction checkng is not necessary when the close obstacle is inside the area, but it is when the obstacle is at the exit point of the area. 9_1023055626

            for (int i = 0; i < 2; i++)
            {
                for (int j = 0; j < 4; j++) // j = 0: middle, j = 1: small area, j = 2: big area, j = 3: big (right down) area
                {
                    if (i == side && j == rotation)
                    {
                        // close mid across
                        if (InTakenRel(x + lx + 2 * sx, y + ly + 2 * sy) && !InTakenRel(x + 2 * sx, y + 2 * sy) && !InTakenRel(x + lx + sx, y + ly + sy))
                        {

                            int i1 = InTakenIndexRel(x + 1 * lx + 2 * sx, y + 1 * ly + 2 * sy);
                            int i2 = InTakenIndexRel(x + 2 * lx + 2 * sx, y + 2 * ly + 2 * sy);

                            if (smallArea && i2 > i1 || !smallArea && i2 < i1) return true;
                        }

                        // close across
                        if (InTakenRel(x + 2 * lx + 2 * sx, y + 2 * ly + 2 * sy) && !InTakenRel(x + lx + 2 * sx, y + ly + 2 * sy) && !InTakenRel(x + 2 * lx + sx, y + 2 * ly + sy))
                        {

                            int i1 = InTakenIndexRel(x + 2 * lx + 2 * sx, y + 2 * ly + 2 * sy);
                            int i2 = InTakenIndexRel(x + 3 * lx + 2 * sx, y + 3 * ly + 2 * sy);

                            if (smallArea && i2 > i1 || !smallArea && i2 < i1) return true;
                        }
                    }

                    if (j == 0) // rotate down (CCW): small area
                    {
                        int l0 = lx;
                        int l1 = ly;
                        lx = -sx;
                        ly = -sy;
                        sx = l0;
                        sy = l1;
                    }
                    else if (j == 1) // rotate up (CW): big area
                    {
                        lx = -lx;
                        ly = -ly;
                        sx = -sx;
                        sy = -sy;
                    }
                    else // rotate CW again
                    {
                        int s0 = sx;
                        int s1 = sy;
                        sx = -lx;
                        sy = -ly;
                        lx = s0;
                        ly = s1;
                    }
                }
                lx = -1;
                ly = 0;
                sx = 0;
                sy = 1;
            }
            return false;
        }

        bool CheckNearFieldSmallRel(int x, int y, int side, int rotation, bool smallArea)
        { // obstacle right side of the field in question, area up
          // mid across and across fields
          // used for LeftRightAreaUp and LeftRightCorner
            int lx = 1;
            int ly = 0;
            int sx = 0;
            int sy = 1;

            // Mid across obstacle:
            // left side:
            // 1, 2
            // 2, -1
            // -2, 1

            // right side:
            // -1, 2
            // -2, -1
            // 2, 1

            // direction checkng is not necessary when the close obstacle is inside the area, but it is when the obstacle is at the exit point of the area. 9_1023055626

            for (int i = 0; i < 2; i++)
            {
                for (int j = 0; j < 4; j++) // j = 0: middle, j = 1: small area, j = 2: big area, j = 3: big (right down) area
                {
                    if (i == side && j == rotation)
                    {
                        // C-shape left
                        // if (InTakenRel(x + 2 * lx, y + 2 * ly) && InTakenRel(x + lx - sx, y + ly - sy) && !InTakenRel(x + lx, y + ly))
                        // For 2024_0808, border checking is needed too.
                        if ((InTakenRel(x + 2 * lx, y + 2 * ly) || InBorderRel(x + 2 * lx, y + 2 * ly)) && !InTakenRel(x + lx, y + ly))
                        {
                            if (InTakenRel(x + 2 * lx, y + 2 * ly))
                            {
                                int i1 = InTakenIndexRel(x + 2 * lx, y + 2 * ly);
                                int i2 = InTakenIndexRel(x + 2 * lx - sx, y + 2 * ly - sy);

                                if (i2 != -1)
                                {
                                    if (smallArea && i2 > i1 || !smallArea && i1 > i2) return true;
                                }
                                else
                                {
                                    i2 = InTakenIndexRel(x + 2 * lx + sx, y + 2 * ly + sy);
                                    if (smallArea && i1 > i2 || !smallArea && i2 > i1) return true;
                                }
                            }
                            else
                            {
                                int i1 = InBorderIndexRel(x + 2 * lx, y + 2 * ly);
                                int i2 = InBorderIndexRel(x + 2 * lx - sx, y + 2 * ly - sy);
                                T(i1 + " " + i2);
                                if (smallArea && i1 > i2 || !smallArea && i2 > i1) return true;
                            }
                        }

                        // close mid across
                        if (InTakenRel(x + lx + 2 * sx, y + ly + 2 * sy) && !InTakenRel(x + 2 * sx, y + 2 * sy) && !InTakenRel(x + lx + sx, y + ly + sy))
                        {

                            int i1 = InTakenIndexRel(x + 1 * lx + 2 * sx, y + 1 * ly + 2 * sy);
                            int i2 = InTakenIndexRel(x + 2 * lx + 2 * sx, y + 2 * ly + 2 * sy);

                            if (smallArea && i2 > i1 || !smallArea && i2 < i1) return true;
                        }

                        // close across
                        if (InTakenRel(x + 2 * lx + 2 * sx, y + 2 * ly + 2 * sy) && !InTakenRel(x + lx + 2 * sx, y + ly + 2 * sy) && !InTakenRel(x + 2 * lx + sx, y + 2 * ly + sy))
                        {

                            int i1 = InTakenIndexRel(x + 2 * lx + 2 * sx, y + 2 * ly + 2 * sy);
                            int i2 = InTakenIndexRel(x + 3 * lx + 2 * sx, y + 3 * ly + 2 * sy);

                            if (smallArea && i2 > i1 || !smallArea && i2 < i1) return true;
                        }
                    }

                    if (j == 0) // rotate down (CCW): small area
                    {
                        int l0 = lx;
                        int l1 = ly;
                        lx = -sx;
                        ly = -sy;
                        sx = l0;
                        sy = l1;
                    }
                    else if (j == 1) // rotate up (CW): big area
                    {
                        lx = -lx;
                        ly = -ly;
                        sx = -sx;
                        sy = -sy;
                    }
                    else // rotate CW again
                    {
                        int s0 = sx;
                        int s1 = sy;
                        sx = -lx;
                        sy = -ly;
                        lx = s0;
                        ly = s1;
                    }
                }
                lx = -1;
                ly = 0;
                sx = 0;
                sy = 1;
            }
            return false;
        }
        // ----- copy end -----

        void CheckFutureCShape() // Even future line can make a straight C-shape, see 2023_0727
        {
            //T("CheckFutureCShape " + sx + " " + sy + " " + lx + " " + ly + " " + path.Count + " " + path[path.Count - 1][0] + " " + path[path.Count - 1][1]);
            for (int i = 0; i < 2; i++)
            {
                for (int j = 0; j < 2; j++)
                {
                    int[] liveEnd = path2[path2.Count - 1];
                    if (!(x + lx == size && y + ly == size) && (InTakenRel(1, -1) || InBorderRel(1, -1)) && (InTakenRel(2, 0) || InBorderRel(2, 0)) && !InTakenRel(1, 0)
                        && !InFutureStartRel(1, -1) && !InFutureEndRel(1, -1)
                        && !InFutureStartRel(2, 0) && !InFutureEndRel(2, 0)
                        && !(isNearEnd && !window.inFuture && liveEnd[0] == x + lx - sx && liveEnd[1] == y + ly - sy)
                         && !(isNearEnd && !window.inFuture && liveEnd[0] == x + 2 * lx && liveEnd[1] == y + 2 * ly))
                    {
                        T("Future C Shape");
                        //CShape = true;
                        AddForbidden(-1, 0); //right
                        AddForbidden(0, 1); //straight				
                    }
                    //turn right, pattern goes upwards
                    int s0 = sx;
                    int s1 = sy;
                    sx = -lx;
                    sy = -ly;
                    lx = s0;
                    ly = s1;
                }
                //mirror directions
                sx = thisSx;
                sy = thisSy;
                lx = -thisLx;
                ly = -thisLy;
            }
            sx = thisSx;
            sy = thisSy;
            lx = thisLx;
            ly = thisLy;
        }

        public void ResetExamAreas()
        {
            examAreaLines = new();
            examAreaLineTypes = new();
            examAreaLineDirections = new();
            examAreaPairFields = new();

            examAreaLine2 = new();
            examAreaLineType2 = 0;
            examAreaLineDirection2 = false;
            examAreaPairField2 = new();
        }

        public void ResetExamAreas2()
        {
            examAreaLine2 = new();
            examAreaLineType2 = new();
            examAreaLineDirection2 = new();
            examAreaPairField2 = new();
        }

        public void AddExamAreas(bool secondaryArea = false) // if a rule is true, we display all examined circles, but only add the checkerboard from the last one.
        {
            for (int i = 0; i < examAreaLines.Count; i++)
            {
                areaLines.Add(examAreaLines[i]);
                areaLineTypes.Add(examAreaLineTypes[i]);
                areaLineDirections.Add(examAreaLineDirections[i]);
                areaPairFields.Add(examAreaPairFields[i]);
                if (secondaryArea)
                {
                    areaLineSecondary.Add(true);
                }
                else
                {
                    areaLineSecondary.Add(false);
                }
            }

            if (examAreaLine2.Count != 0)
            {
                areaLines.Add(examAreaLine2);
                areaLineTypes.Add(examAreaLineType2);
                areaLineDirections.Add(examAreaLineDirection2);
                areaPairFields.Add(examAreaPairField2);
                areaLineSecondary.Add(true);
            }
        }

        // ---- Count Area -----

        public bool CountAreaRel(int left1, int straight1, int left2, int straight2, List<int[]>? borderFields, bool circleDirectionLeft, int circleType, bool getInfo = false)
        {
            //T("CountAreaRel " + left1 + " " + straight1 + " " + left2 + " " + straight2);
            int x1 = x + left1 * lx + straight1 * sx;
            int y1 = y + left1 * ly + straight1 * sy;
            int x2 = x + left2 * lx + straight2 * sx;
            int y2 = y + left2 * ly + straight2 * sy;

            List<int[]> absBorderFields = new();
            if (!(borderFields is null))
            {
                foreach (int[] field in borderFields)
                {
                    absBorderFields.Add(new int[] { x + field[0] * lx + field[1] * sx, y + field[0] * ly + field[1] * sy });
                }
            }

            return CountArea(x1, y1, x2, y2, absBorderFields, circleDirectionLeft, circleType, getInfo, false);
        }

        public bool CountAreaRel2(int left1, int straight1, int left2, int straight2, List<int[]>? borderFields, bool circleDirectionLeft, int circleType, bool getInfo = false)
        {
            //T("CountAreaRel2 " + left1 + " " + straight1 + " " + left2 + " " + straight2, circleDirectionLeft);
            int x_1 = x2 + left1 * lx2 + straight1 * sx2;
            int y_1 = y2 + left1 * ly2 + straight1 * sy2;
            int x_2 = x2 + left2 * lx2 + straight2 * sx2;
            int y_2 = y2 + left2 * ly2 + straight2 * sy2;

            List<int[]> absBorderFields = new();
            if (!(borderFields is null))
            {
                foreach (int[] field in borderFields)
                {
                    absBorderFields.Add(new int[] { x2 + field[0] * lx2 + field[1] * sx2, y2 + field[0] * ly2 + field[1] * sy2 });
                }
            }

            return CountArea(x_1, y_1, x_2, y_2, absBorderFields, circleDirectionLeft, circleType, getInfo, true);
        }

        // Due to 2024_0618_1, new algorithm is now used.
        private bool CountArea(int startX, int startY, int endX, int endY, List<int[]>? borderFields, bool circleDirectionLeft, int circleType, bool getInfo = false, bool secondaryArea = false)
        // compareColors is for the starting situation of 2023_1119, where we mark an impair area and know the entry and the exit field. We count the number of white and black cells of a checkered pattern, the color of the entry and exit should be one more than the other color.
        {
            // find coordinates of the top left (circleDirection = right) or top right corner (circleDirection = left)
            int minY = startY;
            int limitX = startX;
            int startIndex;

            int xDiff, yDiff;
            List<int[]> areaLine = new();

            if (debug) T("Count area startX " + startX + " startY " + startY + " endX " + endX + " endY " + endY);

            if (borderFields == null || borderFields.Count == 0)
            {
                if (Math.Abs(endX - startX) == 2 || Math.Abs(endY - startY) == 2)
                {
                    int middleX = (endX + startX) / 2;
                    int middleY = (endY + startY) / 2;
                    xDiff = startX - middleX;
                    yDiff = startY - middleY;
                    areaLine.Add(new int[] { middleX, middleY });
                    if (debug) T("Adding border " + middleX + " " + middleY);

                }
                else
                {
                    xDiff = startX - endX;
                    yDiff = startY - endY;
                }
            }
            else
            {
                areaLine = new();
                foreach (int[] field in borderFields)
                {
                    areaLine.Add(new int[] { field[0], field[1] });
                    if (debug) T("Adding border " + field[0] + " " + field[1]);
                }
                xDiff = startX - borderFields[borderFields.Count - 1][0];
                yDiff = startY - borderFields[borderFields.Count - 1][1];
            }

            areaLine.Add(new int[] { startX, startY });
            if (debug) T("Adding start " + startX + " " + startY);

            List<int[]> directions;

            if (circleDirectionLeft)
            {
                directions = new List<int[]> { new int[] { 0, 1 }, new int[] { 1, 0 }, new int[] { 0, -1 }, new int[] { -1, 0 } };
            }
            else
            {
                directions = new List<int[]> { new int[] { 0, 1 }, new int[] { -1, 0 }, new int[] { 0, -1 }, new int[] { 1, 0 } };
            }

            int currentDirection = -1;
            foreach (int[] direction in directions)
            {
                currentDirection++;
                if (direction[0] == xDiff && direction[1] == yDiff)
                {
                    break;
                }
            }

            int nextX = startX;
            int nextY = startY;

            startIndex = areaLine.Count - 1;

            // if the field in straight direction is the live end, we need to turn (right if the circle direction is left). Similarly, if the live end is across on the same side the direction is going.
            int turnedDirection = currentDirection == 0 ? 3 : currentDirection - 1;

            int x = path[path.Count - 1][0];
            int y = path[path.Count - 1][1];

            if (x == nextX + xDiff && y == nextY + yDiff || InTaken(nextX + xDiff, nextY + yDiff) || x == nextX + xDiff + directions[turnedDirection][0] && y == nextY + yDiff + directions[turnedDirection][1])
            {
                currentDirection = turnedDirection;
            }

            // T("currentDirection: " + currentDirection + " turnedDirection " + turnedDirection + " nextX + directions[turnedDirection][0] " + (nextX + directions[turnedDirection][0]) + " nextY + directions[turnedDirection][1] " + (nextY + directions[turnedDirection][1]) + " nextX " + nextX + " nextY " + nextY + " x " + x + " y " + y);

            // In case of an area of 2, 3 or a longer column
            if (InTaken(nextX + directions[currentDirection][0], nextY + directions[currentDirection][1]) || InBorder(nextX + directions[currentDirection][0], nextY + directions[currentDirection][1]))
            {
                currentDirection = currentDirection == 0 ? 3 : currentDirection - 1;
            }

            nextX += directions[currentDirection][0];
            nextY += directions[currentDirection][1];

            areaLine.Add(new int[] { nextX, nextY });
            if (debug) T("Adding continued " + nextX + " " + nextY);

            if (nextY < minY)
            {
                minY = nextY;
                limitX = nextX;
                startIndex = areaLine.Count - 1;
            }
            else if (nextY == minY)
            {
                if (circleDirectionLeft) //top right corner
                {
                    if (nextX > limitX)
                    {
                        limitX = nextX;
                        startIndex = areaLine.Count - 1;
                    }
                }
                else //top left corner
                {
                    if (nextX < limitX)
                    {
                        limitX = nextX;
                        startIndex = areaLine.Count - 1;
                    }
                }
            }

            while (!(nextX == endX && nextY == endY))
            {
                // int startDirection = currentDirection;
                currentDirection = currentDirection == 3 ? 0 : currentDirection + 1;
                int i = currentDirection;
                int possibleNextX = nextX + directions[currentDirection][0];
                int possibleNextY = nextY + directions[currentDirection][1];

                int counter = 0;
                while (InBorder(possibleNextX, possibleNextY) || InTaken(possibleNextX, possibleNextY))
                {
                    counter++;
                    i = (i == 0) ? 3 : i - 1;
                    possibleNextX = nextX + directions[i][0];
                    possibleNextY = nextY + directions[i][1];

                    if (counter == 4)
                    {
                        T("Countarea error.");

                        window.errorInWalkthrough = true;
                        window.errorString = "Countarea error.";
                        window.criticalError = true;
                        debug = false;
                        return false;
                    }
                }

                // not actual with C-shape allowed when checking other rules
                /*if (i != startDirection && (i - startDirection) % 2 == 0) // opposite direction. Can happen in 2023_1006
                {
                    T("Error at " + startX + " " + startY + " " + endX + " " + endY + " " + possibleNextX + " " + possibleNextY);
                    window.errorInWalkthrough = true;
                    T("Single field in arealine.");
                    foreach (int[] field in areaLine)
                    {
                        T(field[0] + " " + field[1]);
                    }
                    window.M("Single field in arealine.", 1);

                    return false;
                }*/

                currentDirection = i;

                nextX = possibleNextX;
                nextY = possibleNextY;

                // when getting info about area
                if (nextX == size && nextY == size)
                {
                    T("Corner is reached.");

                    window.errorInWalkthrough = true;
                    window.errorString = "Corner is reached.";
                    window.criticalError = true;
                    debug = false;
                    return false;
                }

                // not actual with C-shape allowed when checking other rules
                // We may go through the same field twice as in 2023_1208 side across down checking, but that field is a count area border field.
                /*foreach (int[] field in areaLine)
                {
                    if (field[0] == nextX && field[1] == nextY)
                    {
                        bool found = false;
                        if (borderFields != null)
                        {
                            foreach (int[] field2 in borderFields)
                            {                                
                                if (field2[0] == nextX && field2[1] == nextY)
                                {
                                    found = true;
                                    break;
                                }
                            }
                        }

                        if (!found)
                        {
                            T("Error at sx " + startX + " sy " + startY + " ex " + endX + " ey " + endY + " x " + nextX + " y " + nextY);
                            window.errorInWalkthrough = true;
                            T("Field exists in arealine.");
                            window.M("Field exists in arealine.", 1);
                            return false;
                        }
                    }
                }*/

                areaLine.Add(new int[] { nextX, nextY });

                if (areaLine.Count == size * size)
                {
                    T("Area walkthrough error.");

                    window.errorInWalkthrough = true;
                    window.errorString = "Area walkthrough error.";
                    window.criticalError = true;
                    debug = false;
                    return false;
                }

                if (debug) T("Adding " + nextX + " " + nextY + " count " + areaLine.Count);

                if (nextY < minY)
                {
                    minY = nextY;
                    limitX = nextX;
                    startIndex = areaLine.Count - 1;
                }
                else if (nextY == minY)
                {
                    if (circleDirectionLeft) //top right corner
                    {
                        if (nextX > limitX)
                        {
                            limitX = nextX;
                            startIndex = areaLine.Count - 1;
                        }
                    }
                    else //top left corner
                    {
                        if (nextX < limitX)
                        {
                            limitX = nextX;
                            startIndex = areaLine.Count - 1;
                        }
                    }
                }
            }

            if (debug)
            {
                T("minY " + minY + " limitX " + limitX + " startIndex " + startIndex);
                foreach (int[] a in areaLine)
                {
                    T(a[0] + " " + a[1]);
                }
            }

            //Special cases are not yet programmed in here as in MainWindow.xaml.cs. We take a gradual approach, starting from the cases that can happen on 7 x 7.

            if (!secondaryArea)
            {
                examAreaLines.Add(areaLine);
                examAreaLineTypes.Add(circleType);
                examAreaLineDirections.Add(circleDirectionLeft);
            }
            else
            {
                examAreaLine2 = Copy(areaLine);
                examAreaLineType2 = circleType;
                examAreaLineDirection2 = circleDirectionLeft;
            }

            int area = 0;
            List<int[]> startSquares = new();
            List<int[]> endSquares = new();

            if (areaLine.Count > 2)
            {
                int[] startCandidate = new int[] { limitX, minY };
                int[] endCandidate = new int[] { limitX, minY };

                if (debug2) T("arealine start " + startCandidate[0] + " " + startCandidate[1]);

                int currentY = minY;

                bool singleField = false;
                // check if there is a one square row on the top
                if (startIndex > 0)
                {
                    if (areaLine[startIndex][1] != areaLine[startIndex - 1][1])
                    {
                        singleField = true;
                    }
                }
                else
                {
                    if (areaLine[0][1] != areaLine[areaLine.Count - 1][1])
                    {
                        singleField = true;
                    }
                }

                // chech if the arealine is one row (column is not a problem for the algorithm)

                int otherX = limitX;
                bool oneRow = true;

                foreach (int[] field in areaLine)
                {
                    x = field[0];
                    y = field[1];

                    if (circleDirectionLeft && x < otherX)
                    {
                        otherX = x;
                    }
                    else if (!circleDirectionLeft && x > otherX)
                    {
                        otherX = x;
                    }

                    if (y != minY)
                    {
                        oneRow = false;
                        break;
                    }
                }

                if (oneRow)
                {
                    if (otherX < limitX)
                    {
                        startSquares.Add(new int[] { otherX, minY });
                        endSquares.Add(new int[] { limitX, minY });
                    }
                    else
                    {
                        startSquares.Add(new int[] { limitX, minY });
                        endSquares.Add(new int[] { otherX, minY });
                    }
                }
                else
                {
                    for (int i = 1; i < areaLine.Count; i++)
                    {
                        int index = startIndex + i;
                        if (index >= areaLine.Count)
                        {
                            index -= areaLine.Count;
                        }
                        int[] field = areaLine[index];
                        int fieldX = field[0];
                        int fieldY = field[1];

                        if (debug2) T("field x " + field[0] + " y " + field[1] + " currentY " + currentY + " startCandidate " + startCandidate[0] + " " + startCandidate[1] + " endCandidate " + endCandidate[0] + " " + endCandidate[1]);

                        if (fieldY > currentY)
                        {
                            if (circleDirectionLeft)
                            {
                                //in the case where where the previous row was a closed peak, but an open dip was preceding it: the previous end field should have the same y and lower x
                                if (endSquares.Count > 0)
                                {
                                    int[] square = endSquares[endSquares.Count - 1];
                                    x = square[0];
                                    y = square[1];

                                    if (y == fieldY - 1 && x <= fieldX)
                                    {
                                        startSquares.Add(startCandidate);
                                        endSquares.Add(endCandidate);
                                        startCandidate = endCandidate = field;
                                        currentY = fieldY;
                                        continue;
                                    }
                                }

                                if (startSquares.Count > 0)
                                {
                                    int[] square = startSquares[startSquares.Count - 1];
                                    x = square[0];
                                    y = square[1];

                                    if (y == fieldY)
                                    {
                                        //the previous row was a closed peak
                                        if (x <= fieldX)
                                        {
                                            endSquares.Add(endCandidate);
                                            startSquares.Add(startCandidate);
                                        }
                                        // else: open peak, no start and end should be marked
                                    }
                                    else // stair down right or left, any possible start field is higher up
                                    {
                                        endSquares.Add(endCandidate);
                                    }
                                }
                                else // stair, no start fields exist
                                {
                                    if (singleField)
                                    {
                                        startSquares.Add(startCandidate);
                                    }
                                    endSquares.Add(endCandidate);
                                }
                            }
                            else
                            {
                                if (startSquares.Count > 0)
                                {
                                    int[] square = startSquares[startSquares.Count - 1];
                                    x = square[0];
                                    y = square[1];

                                    if (y == fieldY - 1 && x >= fieldX)
                                    {
                                        startSquares.Add(startCandidate);
                                        endSquares.Add(endCandidate);
                                        startCandidate = endCandidate = field;
                                        currentY = fieldY;
                                        continue;
                                    }
                                }

                                if (endSquares.Count > 0)
                                {
                                    int[] square = endSquares[endSquares.Count - 1];
                                    x = square[0];
                                    y = square[1];

                                    if (y == fieldY)
                                    {
                                        //the previous row was a closed peak
                                        if (x >= fieldX)
                                        {
                                            startSquares.Add(startCandidate);
                                            endSquares.Add(endCandidate);
                                        }
                                        // else: open peak, no start and end should be marked
                                    }
                                    else
                                    {
                                        startSquares.Add(startCandidate);
                                    }
                                }
                                else
                                {
                                    if (singleField)
                                    {
                                        endSquares.Add(endCandidate);
                                    }
                                    startSquares.Add(startCandidate);
                                }
                            }
                            startCandidate = endCandidate = field;
                        }
                        else if (fieldY == currentY)
                        {
                            if (fieldX < startCandidate[0])
                            {
                                startCandidate = field;
                            }
                            else if (fieldX > endCandidate[0])
                            {
                                endCandidate = field;
                            }
                        }
                        else
                        {
                            if (circleDirectionLeft)
                            {
                                if (startSquares.Count > 0)
                                {
                                    int[] square = startSquares[startSquares.Count - 1];
                                    x = square[0];
                                    y = square[1];

                                    if (y == fieldY + 1 && x >= fieldX)
                                    {
                                        startSquares.Add(startCandidate);
                                        endSquares.Add(endCandidate);
                                        startCandidate = endCandidate = field;
                                        currentY = fieldY;
                                        continue;
                                    }
                                }

                                if (endSquares.Count > 0)
                                {
                                    int[] square = endSquares[endSquares.Count - 1];
                                    x = square[0];
                                    y = square[1];

                                    if (y == fieldY)
                                    {
                                        //the previous row was a closed peak
                                        if (x >= fieldX)
                                        {
                                            startSquares.Add(startCandidate);
                                            endSquares.Add(endCandidate);
                                        }
                                        // else: open peak, no start and end should be marked
                                    }
                                    else
                                    {
                                        startSquares.Add(startCandidate);
                                    }
                                }
                                else
                                {
                                    startSquares.Add(startCandidate);
                                }
                            }
                            else
                            {
                                if (endSquares.Count > 0)
                                {
                                    int[] square = endSquares[endSquares.Count - 1];
                                    x = square[0];
                                    y = square[1];

                                    if (y == fieldY + 1 && x <= fieldX)
                                    {
                                        startSquares.Add(startCandidate);
                                        endSquares.Add(endCandidate);
                                        startCandidate = endCandidate = field;
                                        currentY = fieldY;
                                        continue;
                                    }
                                }

                                if (startSquares.Count > 0)
                                {
                                    int[] square = startSquares[startSquares.Count - 1];
                                    x = square[0];
                                    y = square[1];

                                    if (y == fieldY)
                                    {
                                        //the previous row was a closed peak
                                        if (x <= fieldX)
                                        {
                                            endSquares.Add(endCandidate);
                                            startSquares.Add(startCandidate);
                                        }
                                        // else: open peak, no start and end should be marked
                                    }
                                    else
                                    {
                                        endSquares.Add(endCandidate);
                                    }
                                }
                                else
                                {
                                    endSquares.Add(endCandidate);
                                }
                            }
                            startCandidate = endCandidate = field;
                        }
                        currentY = fieldY;
                    }

                    //add last field
                    if (circleDirectionLeft)
                    {
                        if (singleField)
                        {
                            // L-shape
                            if (endSquares.Count == 1)
                            {
                                endSquares.Add(endCandidate);
                                startSquares.Add(startCandidate);
                            }
                            // add startCandidate, unless the last row is an open dip
                            else
                            {
                                int[] square = endSquares[endSquares.Count - 1];
                                y = square[1];

                                if (y != currentY - 1)
                                {
                                    startSquares.Add(startCandidate);
                                }
                            }

                        }
                        else
                        {
                            startSquares.Add(startCandidate);
                        }
                    }
                    else
                    {
                        if (singleField)
                        {
                            // L-shape
                            if (startSquares.Count == 1)
                            {
                                startSquares.Add(startCandidate);
                                endSquares.Add(endCandidate);
                            }
                            // add startCandidate, unless the last row is an open dip
                            else
                            {
                                int[] square = startSquares[startSquares.Count - 1];
                                y = square[1];

                                if (y != currentY - 1)
                                {
                                    endSquares.Add(endCandidate);
                                }
                            }

                        }
                        else
                        {
                            endSquares.Add(endCandidate);
                        }
                    }
                }

                /* T("circleDirectionLeft " + circleDirectionLeft + " singleField " + singleField);
                foreach (int[] sfield in startSquares)
                {
                    T("startsquare: " + sfield[0] + " " + sfield[1]);
                }
                foreach (int[] efield in endSquares)
                {
                    T("endsquare: " + efield[0] + " " + efield[1]);
                } */

                int eCount = endSquares.Count;

                // it should never happen if the above algorithm is bug-free.
                if (startSquares.Count != eCount)
                {
                    foreach (int[] f in startSquares)
                    {
                        T("startSquares " + f[0] + " " + f[1]);
                    }
                    foreach (int[] f in endSquares)
                    {
                        T("endSquares " + f[0] + " " + f[1]);
                    }

                    T("Count of start and end squares are inequal: " + startSquares.Count + " " + eCount);
                    window.errorInWalkthrough = true;
                    window.criticalError = true;
                    window.errorString = "Count of start and end squares are inequal: " + startSquares.Count + " " + eCount;
                    return false;
                }

                for (int i = 0; i < eCount; i++)
                {
                    area += endSquares[i][0] - startSquares[i][0] + 1;
                }
            }
            else // area is 2. No rule will be applies, but the black and white field counts have to be right.
            {
                area = areaLine.Count;

                if (startY == endY)
                {
                    if (startX < endX)
                    {
                        startSquares.Add(new int[] { startX, startY });
                        endSquares.Add(new int[] { endX, endY });
                    }
                    else
                    {
                        startSquares.Add(new int[] { endX, endY });
                        endSquares.Add(new int[] { startX, startY });
                    }
                }
                else
                {
                    startSquares.Add(new int[] { startX, startY });
                    startSquares.Add(new int[] { endX, endY });
                    endSquares.Add(new int[] { startX, startY });
                    endSquares.Add(new int[] { endX, endY });
                }
            }

            if (debug) T("Count area: " + area);

            switch (circleType)
            {
                case 0:
                    if (area % 2 == 0)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                    break;
                case 1:
                    if (area % 2 == 0)
                    {
                        return false;
                    }
                    else
                    {
                        return true;
                    }
                    break;
                case 2:
                case 3:
                    if (!getInfo && area % 2 == 0)
                    {
                        return false;
                    }
                    else
                    {
                        //Check that the number of black cells are one more than the number of white ones in a checkered pattern.The black color is where we enter and exit the area.

                        int pairCount = 0, impairCount = 0;

                        List<int[]> pairFields = new();
                        List<int[]> impairFields = new();

                        foreach (int[] field in startSquares)
                        {
                            x = field[0];
                            y = field[1];
                            int minX = size;

                            //without having open peaks, the first start square should match the last end square. Otherwise, we need to find the ending that is closest to the start field in the row.
                            for (int i = endSquares.Count - 1; i >= 0; i--)
                            {
                                if (endSquares[i][1] == y && endSquares[i][0] >= x)
                                {
                                    if (endSquares[i][0] < minX)
                                    {
                                        minX = endSquares[i][0];
                                    }
                                }
                            }

                            int span = minX - x + 1;

                            if (getInfo)
                            {
                                for (int i = x; i <= minX; i++)
                                {
                                    if ((i + y) % 2 == 0)
                                    {
                                        pairFields.Add(new int[] { i, y });
                                    }
                                    else
                                    {
                                        impairFields.Add(new int[] { i, y });
                                    }

                                }
                            }

                            if ((x + y) % 2 == 0)
                            {
                                pairCount += (span + span % 2) / 2;
                                impairCount += (span - span % 2) / 2;
                            }
                            else
                            {
                                impairCount += (span + span % 2) / 2;
                                pairCount += (span - span % 2) / 2;
                            }
                        }

                        if (getInfo)
                        {
                            if (circleType == 2)
                            {
                                if ((startX + startY) % 2 == 0)
                                {
                                    info = new List<object> { area % 2, pairCount, impairCount };
                                    if (!secondaryArea)
                                    {
                                        examAreaPairFields.Add(pairFields);
                                    }
                                    else
                                    {
                                        examAreaPairField2 = Copy(pairFields);
                                    }
                                }
                                else
                                {
                                    info = new List<object> { area % 2, impairCount, pairCount };
                                    if (!secondaryArea)
                                    {
                                        examAreaPairFields.Add(impairFields);
                                    }
                                    else
                                    {
                                        examAreaPairField2 = Copy(impairFields);
                                    }
                                }
                            }
                            else
                            {
                                if ((startX + startY) % 2 == 1)
                                {
                                    info = new List<object> { area % 2, pairCount, impairCount };
                                    if (!secondaryArea)
                                    {
                                        examAreaPairFields.Add(pairFields);
                                    }
                                    else
                                    {
                                        examAreaPairField2 = Copy(pairFields);
                                    }
                                }
                                else
                                {
                                    info = new List<object> { area % 2, impairCount, pairCount };
                                    if (!secondaryArea)
                                    {
                                        examAreaPairFields.Add(impairFields);
                                    }
                                    else
                                    {
                                        examAreaPairField2 = Copy(impairFields);
                                    }
                                }
                            }

                            return true;
                        }

                        T("pair " + pairCount + ", impair " + impairCount + " circleType " + circleType);
                        if (circleType == 2 && ((startX + startY) % 2 == 0 && pairCount != impairCount + 1 || (startX + startY) % 2 == 1 && impairCount != pairCount + 1) || circleType == 3 && ((startX + startY) % 2 == 0 && pairCount + 1 != impairCount || (startX + startY) % 2 == 1 && impairCount + 1 != pairCount))
                        {
                            // imbalance in colors, forbidden fields in the rule apply
                            return true;
                        }
                        else
                        {
                            return false;
                        }
                    }

            }
            return false;
        }

        public void AddEndClose(int counter, int stx, int sty)
        // North-west direction
        {
            for (int i = 0; i < counter; i++)
            {
                path.Add(new int[] { x + (stx + i) * lx + (sty + i) * sx, y + (stx + i) * ly + (sty + i) * sy });
            }
        }

        public void AddEndFar(int counter, int stx, int sty)
        // South-east direction
        {
            for (int i = 0; i < counter; i++)
            {
                path.Add(new int[] { x + (stx - i) * lx + (sty - i) * sx, y + (stx - i) * ly + (sty - i) * sy });
            }
        }

        public void RemoveEnd(int counter)
        {
            for (int i = 1; i <= counter; i++)
            {
                path.RemoveAt(path.Count - 1);
            }
        }

        // ----- Field checking -----

        public bool AddForbidden(int left, int straight)
        {
            if (!InTakenRel(left, straight))
            {
                forbidden.Add(new int[] { x + left * lx + straight * sx, y + left * ly + straight * sy });
                return true;
            }
            else return false;
        }

        public bool InBorderAbs(int[] field)
        {
            int x = field[0];
            int y = field[1];
            return InBorder(x, y);
        }

        public bool InBorderRel(int left, int straight)
        {
            int x = this.x + left * lx + straight * sx;
            int y = this.y + left * ly + straight * sy;
            return InBorder(x, y);
        }

        public bool InBorderRel2(int left, int straight)
        {
            int x = x2 + left * lx2 + straight * sx2;
            int y = y2 + left * ly2 + straight * sy2;
            return InBorder(x, y);
        }

        public bool InBorder(int x, int y) // allowing negative values could cause an error in AddFutureLines 2x2 checking, but it is necessary in CheckLeftRightCorner due to possibility checking
        {
            if (x <= 0 || x >= size + 1 || y <= 0 || y >= size + 1) return true;
            return false;
        }

        public bool InBorderRelExact(int left, int straight)
        {
            int x = this.x + left * lx + straight * sx;
            int y = this.y + left * ly + straight * sy;
            return InBorderExact(x, y);
        }

        bool InBorderRelExact2(int left, int straight)
        {
            int x0 = x2 + left * lx2 + straight * sx2;
            int y0 = y2 + left * ly2 + straight * sy2;
            return InBorderExact(x0, y0);
        }

        public bool InBorderExact(int x, int y) // strict mode
        {
            if (x == 0 || x == size + 1 || y == 0 || y == size + 1) return true;
            return false;
        }

        public bool InTakenAbs(int[] field0)
        {
            int x = field0[0];
            int y = field0[1];

            return InTaken(x, y);
        }

        public bool InTakenRel(int left, int straight)
        {
            int x = this.x + left * lx + straight * sx;
            int y = this.y + left * ly + straight * sy;

            // T("InTakenRel " + x + " " + y);
            return InTaken(x, y);
        }

        public bool InTakenRel2(int left, int straight)
        {
            int x = x2 + left * lx2 + straight * sx2;
            int y = y2 + left * ly2 + straight * sy2;

            return InTaken(x, y);
        }

        public bool InTaken(int x, int y) //more recent fields are more probable to encounter, so this way processing time is optimized
        {
            if (!isMain)
            {
                if (path2 != null)
                {
                    int c2 = path2.Count;
                    for (int i = c2 - 1; i >= 0; i--)
                    {
                        int[] field = path2[i];
                        if (x == field[0] && y == field[1])
                        {
                            return true;
                        }
                    }
                }

                int c1 = path.Count;
                for (int i = c1 - 1; i >= 0; i--)
                {
                    int[] field = path[i];
                    if (x == field[0] && y == field[1]) // In 2023_0919 (step right, the near end of the bottom line extends), even if the near end being stepped on is now inactive, it is not an option (Near end cannot connect to near end.) Active checking is unnecessary.
                    {
                        return true;
                    }
                }
            }
            else
            {
                int c1 = path.Count;
                for (int i = c1 - 1; i >= 0; i--)
                {
                    int[] field = path[i];
                    if (x == field[0] && y == field[1])
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public bool InCornerRel(int left, int straight)
        {
            int x0 = x + left * lx + straight * sx;
            int y0 = y + left * ly + straight * sy;
            if (x0 == size && y0 == size) return true;
            return false;
        }

        public bool InCornerRel2(int left, int straight)
        {
            int x0 = x2 + left * lx2 + straight * sx2;
            int y0 = y2 + left * ly2 + straight * sy2;
            if (x0 == size && y0 == size) return true;
            return false;
        }

        public bool InFutureAbs(int[] f)
        {
            return InFuture(f[0], f[1]);
        }

        public bool InFutureRel(int left, int straight)
        {
            int x = this.x + left * lx + straight * sx;
            int y = this.y + left * ly + straight * sy;
            return InFuture(x, y);
        }

        public bool InFuture(int x, int y)
        {
            List<int[]> searchPath = isMain ? window.future.path : path;
            int c = searchPath.Count;
            if (c == 0) return false;

            for (int i = c - 1; i >= 0; i--)
            {
                int[] field = searchPath[i];
                if (x == field[0] && y == field[1])
                {
                    return true;
                }
            }
            return false;
        }

        public bool InFutureStartAbs(int[] f, int nearSection = -1) // absulute position
        {
            return InFutureStart(f[0], f[1], nearSection);
        }

        public bool InFutureStartRel(int left, int straight) // relative position
        {
            int x = this.x + left * lx + straight * sx;
            int y = this.y + left * ly + straight * sy;
            return InFutureStart(x, y);
        }

        public bool InFutureStart(int x, int y, int nearSection = -1)
        {
            List<int[]> searchPath = isMain ? window.future.path : path;
            int c = searchPath.Count;
            if (c == 0) return false;

            int foundIndex = -1;

            int i;
            for (i = c - 1; i >= 0; i--)
            {
                int[] field = searchPath[i];

                if (field[0] == x && field[1] == y && MainWindow.futureActive[i])
                {
                    foundIndex = i;
                }
            }

            if (foundIndex == -1) return false;

            i = -1;
            foreach (int[] section in MainWindow.futureSections)
            {
                i++;
                if (section[0] == foundIndex)
                {
                    // for checking possible steps, all near ends from the current section/merge cannot be stepped on. For checking C-shape, the section can be whatever
                    if (nearSection == -1 || nearSection != -1 && i != nearSection)
                    {
                        // We examine all mearges. The start of them can be stepped on, but all the others cannot.
                        foreach (int[] merge in MainWindow.futureSectionMerges)
                        {
                            for (int j = 1; j < merge.Length; j++)
                            {
                                if (merge[j] == i) return false;
                            }
                        }
                        foundSectionStart = i;
                        return true;
                    }
                    // else i == nearSection: Found field is not a start that can be stepped on. 
                }
            }

            return false;
        }

        public bool InFutureEndAbs(int[] f, int farSection = -1) // absulute position
        {
            return InFutureEnd(f[0], f[1], farSection);
        }

        public bool InFutureEndRel(int left, int straight) // relative position
        {
            int x = this.x + left * lx + straight * sx;
            int y = this.y + left * ly + straight * sy;
            return InFutureEnd(x, y);
        }

        public bool InFutureEnd(int x, int y, int farSection = -1)
        {
            List<int[]> searchPath = isMain ? window.future.path : path;
            int c = searchPath.Count;
            if (c == 0) return false;

            if (x == size && y == size) return false; // a far end that reached the corner is not considered live.

            int foundIndex = -1;

            int i;
            for (i = c - 1; i >= 0; i--)
            {
                int[] field = searchPath[i];
                if (field[0] == x && field[1] == y && MainWindow.futureActive[i])
                {
                    foundIndex = i;
                }
            }

            if (foundIndex == -1) return false;

            i = -1;
            foreach (int[] section in MainWindow.futureSections)
            {
                i++;
                if (section[1] == foundIndex)
                {
                    // for checking possible steps, all far ends from the current section/merge cannot be stepped on. For checking C-shape, the section can be whatever
                    if (farSection == -1 || farSection != -1 && i != farSection)
                    {
                        // We examine all mearges. The end of them can be stepped on, but all the others cannot.
                        foreach (int[] merge in MainWindow.futureSectionMerges)
                        {
                            for (int j = 0; j < merge.Length - 1; j++)
                            {
                                if (merge[j] == i) return false;
                            }
                        }

                        foundSectionEnd = i;
                        return true;
                    }
                }
            }

            return false;
        }

        public int InBorderIndexRel(int left, int straight)
        {
            int x = this.x + left * lx + straight * sx;
            int y = this.y + left * ly + straight * sy;
            return x + y;
        }


        public int InTakenIndexRel(int left, int straight) // relative position
        {
            int x = this.x + left * lx + straight * sx;
            int y = this.y + left * ly + straight * sy;
            return InTakenIndex(x, y);
        }

        public int InTakenIndexRel2(int left, int straight) // relative position
        {
            int x = x2 + left * lx2 + straight * sx2;
            int y = y2 + left * ly2 + straight * sy2;
            return InTakenIndex(x, y);
        }

        public int InTakenIndex(int x, int y)
        {
            int c = path.Count;
            for (int i = 0; i < c; i++)
            {
                int[] field = path[i];
                if (x == field[0] && y == field[1])
                {
                    return i;
                }
            }
            return -1;
        }

        public int InFutureIndex(int x, int y)
        {
            int c2 = path2.Count;

            for (int i = c2 - 1; i >= 0; i--)
            {
                int[] field = path2[i];
                //without checking active state, CheckNearFutureSide can come true
                if (x == field[0] && y == field[1] && MainWindow.futureActive[i])
                {
                    return i;
                }
            }
            return -1;
        }

        public bool InForbidden(int[] value)
        {
            bool found = false;
            foreach (int[] field in forbidden)
            {
                if (value[0] == field[0] && value[1] == field[1])
                {
                    found = true;
                }
            }
            return found;
        }

        // -----  functions end -----

        void T(params object[] o)
        {
            if (!suppressLogs)
            {
                string result = "";
                if (o.Length > 0)
                {
                    result += o[0];
                }

                for (int i = 1; i < o.Length; i++)
                {
                    result += ", " + o[i];
                }
                Trace.WriteLine(result);
                MainWindow.logger.LogDebug("----------------------------- " + result);
            }
        }

        string ShowForbidden()
        {
            string s = "";
            foreach (int[] field in forbidden)
            {
                s += field[0] + "," + field[1] + "; ";
            }
            if (s.Length > 2)
            {
                return s.Substring(0, s.Length - 2);
            }
            else
            {
                return "";
            }
        }

        List<int[]> Copy(List<int[]> obj)
        {
            List<int[]> newObj = new();
            foreach (int[] element in obj)
            {
                newObj.Add(element);
            }
            return newObj;
        }
    }
}
