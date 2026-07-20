// 9 x 9 walkthrough complete

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
        bool closeStraightSmall, closeMidAcrossSmall, closeAcrossSmall, closeStraightLarge, closeMidAcrossLarge, closeAcrossLarge = false;
        int Straight3I = -1; // used for checking Down Stair and Double Area first case rotated at the next step.
        int Straight3J = -1;

        bool DoubleArea1, DoubleArea2, DoubleArea3, DoubleArea4, DoubleArea1Rotated, Sequence1, Sequence2, Sequence3, DownStairClose, DownStair, DoubleAreaFirstCaseRotatedNext, DownStairNext = false;

        int[] newExitField = new int[] { 0, 0 };
        bool newDirectionRotated = false; // if rotated, it is CW on left side
        List<int[]> startForbiddenFields;
        List<string> activeRules;
        List<List<int[]>> activeRulesForbiddenFields;
        List<int[]> activeRuleSizes;

        // ----- copy start -----
        void ApplyRules_control()
        {
            activeRules = new();
            activeRulesForbiddenFields = new();
            activeRuleSizes = new();

            CShape = false;

            CheckCShape();

            if (CShape) return;

            closeStraightSmall = false;
            closeMidAcrossSmall = false;
            closeAcrossSmall = false;
            closeStraightLarge = false;
            closeMidAcrossLarge = false;

            // needed for far left and right case 9_234320
            CheckNearField();

            if (closeStraightSmall || closeMidAcrossSmall || closeAcrossSmall || closeStraightLarge || closeMidAcrossLarge) return;

            CheckNearBorder();
            CheckAreaNearBorder();

            List<int[]> newPossible = new();
            foreach (int[] field in possible)
            {
                if (!InForbidden(field))
                {
                    newPossible.Add(field);
                }
            }
            possible = newPossible;

            if (possible.Count == 1) return;

            Straight3I = -1;
            Straight3J = -1;

            //L("CheckStraight", forbidden.Count);
            CheckStraight_control();
            //L("CheckLeftRightAreaUp", forbidden.Count);
            CheckLeftRightAreaUp_control();
            //L("CheckLeftRightAreaUpBig", forbidden.Count);
            CheckLeftRightAreaUpBig();
            //L("CheckLeftRightCornerBig", forbidden.Count);
            CheckLeftRightCornerBig();

            //L("CheckLeftRightCornerBig end", forbidden.Count);

            startForbiddenFields = Copy(forbidden);

            //T("Check3DoubleArea");
            Check3DoubleArea();
            //T("Check3DoubleAreaRotated");
            Check3DoubleAreaRotated();
            //T("CheckSequence");
            CheckSequence();
            //T("CheckDownStair");
            CheckDownStair();
            //T("Check3DistNextStep");
            Check3DistNextStep();
        }

        void CheckCShape()
        {
            for (int i = 0; i < 2; i++)
            {
                for (int j = 0; j < 2; j++)
                {
                    if ((InTakenRel(2, 0) || InBorderRel(2, 0)) &&
                        (InTakenRel(1, -1) || InBorderRel(1, -1)) &&
                        !InTakenRel(1, 0) && !InBorderRel(1, 0) && !InCornerRel(1, 0))
                    {
                        CShape = true;

                        forbidden.Add(new int[] { x + sx, y + sy });
                        forbidden.Add(new int[] { x - lx, y - ly });
                    }

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

        void CheckNearField()
        {
            bool farSideStraightUp = false;
            bool farSideStraightDown = false;
            bool farSideMidAcrossUp = false;
            bool farSideMidAcrossDown = false;

            // for 2-distance simultaneous rules
            bool farStraightLeft = false;
            bool farStraightRight = false;
            bool farSideUp;
            bool farSideDown;

            // used also for determining if custom rules have to run
            closeStraightSmall = false;
            closeMidAcrossSmall = false;
            closeAcrossSmall = false;
            closeStraightLarge = false;
            closeMidAcrossLarge = false;
            closeAcrossLarge = false;

            for (int i = 0; i < 2; i++)
            {
                bool closeStraight = false;
                bool closeMidAcross = false;

                if (InTakenRel(0, 2) && InTakenRel(1, 2) && !InTakenRel(0, 1))
                {
                    closeStraight = true;

                    // needed if C-shape precondition is disabled
                    if (!InTakenRel(1, 1) && !InTakenRel(-1, 1))
                    {
                        forbidden.Add(new int[] { x + sx, y + sy });

                        int middleIndex = InTakenIndexRel(0, 2);
                        int sideIndex = InTakenIndexRel(1, 2);
                        if (sideIndex > middleIndex) // area on left
                        {
                            closeStraightSmall = true;
                            forbidden.Add(new int[] { x - lx, y - ly });
                        }
                        else
                        {
                            closeStraightLarge = true;
                            forbidden.Add(new int[] { x + lx, y + ly });
                        }
                    }
                }

                if (!closeStraight)
                {
                    if (InTakenRel(1, 2) && !InTakenRel(0, 1) && !InTakenRel(1, 1))
                    {
                        closeMidAcross = true;

                        forbidden.Add(new int[] { x + sx, y + sy });

                        int middleIndex = InTakenIndexRel(1, 2);
                        int sideIndex = InTakenIndexRel(2, 2);
                        if (sideIndex > middleIndex)
                        {
                            closeMidAcrossSmall = true;
                            forbidden.Add(new int[] { x - lx, y - ly });
                        }
                        else
                        {
                            closeMidAcrossLarge = true;
                            forbidden.Add(new int[] { x + lx, y + ly });
                        }
                    }
                }

                if (!closeStraight && !closeMidAcross)
                {
                    if (InTakenRel(2, 2) && !InTakenRel(0, 1) && !InTakenRel(1, 1) && !InTakenRel(2, 1))
                    {
                        int middleIndex = InTakenIndexRel(2, 2);
                        int sideIndex = InTakenIndexRel(3, 2);
                        if (sideIndex > middleIndex)
                        {
                            closeAcrossSmall = true;
                            forbidden.Add(new int[] { x + sx, y + sy });
                            forbidden.Add(new int[] { x - lx, y - ly });
                        }
                        else
                        {
                            closeAcrossLarge = true;
                            forbidden.Add(new int[] { x + lx, y + ly });
                        }
                    }
                }

                lx = -lx;
                ly = -ly;
            }
            lx = thisLx;
            ly = thisLy;

            // Far rules shouldn't be checked until close rules are checked on both sides, see 9_305112. Here, close straight is only true on the right side, but left side far rules get checked before.
            // A close rule may be true on one side, but on the other side there can be a far rule, like in 9_1307639. The close rule has to be large in this case.

            // A large close mid across on one side can have a small far across on the other side.
            // A large close across on one side can have a small far mid across / across on the other side.
            // Only the last case needs to be examined. All the other close rules have two fields disabled.

            if (!closeStraightSmall && !closeMidAcrossSmall && !closeAcrossSmall && !closeStraightLarge && !closeMidAcrossLarge)
            {
                for (int i = 0; i < 2; i++)
                {
                    bool farStraight = false;
                    bool farMidAcross = false;

                    if (InTakenRel(0, 3) && InTakenRel(1, 3) && !InTakenRel(0, 2) && !InTakenRel(0, 1)) // 0, 2: 2023_1225; 0, 1: 2023_1226
                    {
                        farStraight = true;

                        int middleIndex = InTakenIndexRel(0, 3);
                        int sideIndex = InTakenIndexRel(1, 3);
                        if (sideIndex > middleIndex) // area on left
                        {
                            if (!InTakenRel(1, 2) && !InTakenRel(2, 2)) // 1,2: 2023_1019_3, 2,2: 2023_1019_4
                            {
                                if (i == 0) farStraightLeft = true; else farStraightRight = true;

                                bool circleDirectionLeft = i == 0 ? true : false;
                                if (CountAreaRel(1, 1, 1, 2, null, circleDirectionLeft, 1))
                                {
                                    forbidden.Add(new int[] { x + sx, y + sy });
                                    forbidden.Add(new int[] { x - lx, y - ly });
                                }
                                else if (InTakenRel(-2, 1) && InTakenRel(-1, 0) && !InTakenRel(-1, 1))
                                {
                                    forbidden.Add(new int[] { x + sx, y + sy });
                                }
                            }
                        }
                        else // area on right
                        {
                            if (!InTakenRel(-1, 2) && !InTakenRel(-2, 2)) // -1, 2: 2023_1019_5, -2, 2: 2023_1019_6
                            {
                                bool circleDirectionLeft = i == 0 ? false : true;
                                if (CountAreaRel(-1, 1, -1, 2, null, circleDirectionLeft, 1))
                                {
                                    forbidden.Add(new int[] { x + sx, y + sy });
                                    forbidden.Add(new int[] { x + lx, y + ly });
                                }
                                else if (InTakenRel(2, 1) && InTakenRel(1, 0) && !InTakenRel(1, 1))
                                {
                                    forbidden.Add(new int[] { x + sx, y + sy });
                                }
                            }
                        }
                    }

                    if (!farStraight)
                    {
                        if (InTakenRel(1, 3) && InTakenRel(2, 3) && !InTakenRel(0, 2) && !InTakenRel(1, 2) && !InTakenRel(0, 1)) // 0, 2; 1, 2: 2023_1019_2
                        {
                            farMidAcross = true;

                            int middleIndex = InTakenIndexRel(1, 3);
                            int sideIndex = InTakenIndexRel(2, 3);
                            if (sideIndex > middleIndex) // area on left
                            {
                                if (!InTakenRel(2, 2)) // 2, 2: 2023_1019
                                {
                                    if (i == 0) farStraightLeft = true; else farStraightRight = true;
                                    bool circleDirectionLeft = i == 0 ? true : false;
                                    if (CountAreaRel(1, 1, 1, 2, null, circleDirectionLeft, 1))
                                    {
                                        forbidden.Add(new int[] { x + sx, y + sy });
                                        forbidden.Add(new int[] { x - lx, y - ly });
                                    }
                                    else if (InTakenRel(-2, 1) && InTakenRel(-1, 0) && !InTakenRel(-1, 1))
                                    {
                                        forbidden.Add(new int[] { x + sx, y + sy });
                                    }
                                }
                            }
                            else // area on right
                            {
                                if (!InTakenRel(-1, 2)) // -1, 2: 2023_1019_1
                                {
                                    bool circleDirectionLeft = i == 0 ? false : true;
                                    if (CountAreaRel(0, 1, 0, 2, null, circleDirectionLeft, 1))
                                    {
                                        forbidden.Add(new int[] { x + sx, y + sy });
                                        forbidden.Add(new int[] { x + lx, y + ly });
                                    }
                                    else if (InTakenRel(2, 1) && InTakenRel(1, 0) && !InTakenRel(1, 1))
                                    {
                                        forbidden.Add(new int[] { x + sx, y + sy });
                                    }
                                }
                            }
                        }

                        if (!farMidAcross)
                        {
                            if (InTakenRel(2, 3) && InTakenRel(3, 3) && !InTakenRel(0, 2) && !InTakenRel(1, 2) && !InTakenRel(2, 2) && !InTakenRel(0, 1))
                            {
                                int middleIndex = InTakenIndexRel(2, 3);
                                int sideIndex = InTakenIndexRel(3, 3);
                                if (sideIndex > middleIndex) // area on left
                                {
                                    if (i == 0) farStraightLeft = true; else farStraightRight = true;
                                    bool circleDirectionLeft = i == 0 ? true : false;
                                    if (CountAreaRel(1, 1, 1, 2, null, circleDirectionLeft, 1))
                                    {
                                        forbidden.Add(new int[] { x + sx, y + sy });
                                        forbidden.Add(new int[] { x - lx, y - ly });
                                    }
                                    else
                                    {
                                        if (InTakenRel(-2, 1) && InTakenRel(-1, 0) && !InTakenRel(-1, 1))
                                        {
                                            forbidden.Add(new int[] { x + sx, y + sy });
                                        }
                                        /*if (InTakenRel(1, 4) && !InTakenRel(1, 3)) // end C, there is a separate rule for that now
                                        {
                                            forbidden.Add(new int[] { x + lx, y + ly });
                                        }*/
                                    }
                                }
                                else // area on right
                                {
                                    if (!InTakenRel(-1, 2))
                                    {
                                        bool circleDirectionLeft = i == 0 ? false : true;
                                        if (CountAreaRel(0, 1, 1, 2, new List<int[]> { new int[] { 0, 2 } }, circleDirectionLeft, 0))
                                        {
                                            forbidden.Add(new int[] { x + sx, y + sy });
                                            forbidden.Add(new int[] { x + lx, y + ly });
                                        }
                                        else if (InTakenRel(2, 1) && InTakenRel(1, 0) && !InTakenRel(1, 1))
                                        {
                                            forbidden.Add(new int[] { x + sx, y + sy });
                                        }
                                    }
                                }
                            }
                        }
                    }
                    lx = -lx;
                    ly = -ly;
                }
                lx = thisLx;
                ly = thisLy;
            }

            if (farStraightLeft && farStraightRight) // 9_234256
            {
                forbidden.Add(new int[] { x + sx, y + sy });
            }

            // left/right side rules
            // When any of the close rules are present, even close across large, examining side rules is not necessary. Example: 2023_1019_7
            if (!closeStraightSmall && !closeMidAcrossSmall && !closeAcrossSmall && !closeStraightLarge && !closeMidAcrossLarge && !closeAcrossLarge)
            {
                for (int i = 0; i < 2; i++)
                {
                    bool closeSideStraight = false;
                    bool closeSideMidAcross = false;
                    bool closeSideAcross = false;
                    farSideUp = false;
                    farSideDown = false;
                    farSideStraightUp = false;
                    farSideStraightDown = false;
                    farSideMidAcrossUp = false;
                    farSideMidAcrossDown = false;
                    bool circleDirectionLeft = i == 0 ? false : true;

                    if (InTakenRel(2, 0) && !InTakenRel(1, 0) && !InTakenRel(1, 1))
                    {
                        closeSideStraight = true;

                        // needed if C-Shape precondition is disabled
                        if (!InTakenRel(1, -1))
                        {
                            forbidden.Add(new int[] { x + lx, y + ly });
                        }
                    }

                    if (!closeSideStraight)
                    {
                        if (!InTakenRel(1, 0) && !InTakenRel(1, 1) && (InTakenRel(2, 1) || InTakenRel(2, -1) && !InTakenRel(1, -1)))
                        {
                            closeSideMidAcross = true;
                            forbidden.Add(new int[] { x + lx, y + ly });
                        }
                    }

                    if (!closeSideStraight && !closeSideMidAcross)
                    {
                        if (InTakenRel(2, 2) && !InTakenRel(1, 0) && !InTakenRel(1, 1) && !InTakenRel(1, 2))
                        {
                            closeSideAcross = true;
                            // fields forbidden in straight rules
                        }
                    }

                    if (!closeSideStraight && !closeSideMidAcross && !closeSideAcross)
                    {
                        if (InTakenRel(3, 0) && !InTakenRel(1, 0) && !InTakenRel(1, 1) && !InTakenRel(1, 2))
                        {
                            int middleIndex = InTakenIndexRel(3, 0);
                            if (InTakenRel(3, 1)) // up side taken
                            {
                                farSideStraightUp = true;

                                int sideIndex = InTakenIndexRel(3, 1);
                                if (sideIndex > middleIndex) // area up
                                {
                                    farSideUp = true;

                                    if (CountAreaRel(1, 1, 2, 1, null, circleDirectionLeft, 1))
                                    {
                                        forbidden.Add(new int[] { x + lx, y + ly });
                                    }
                                    else if ((InTakenRel(1, -2) || InBorderRel(1, -2)) && !InTakenRel(1, -1))
                                    {
                                        forbidden.Add(new int[] { x + lx, y + ly });
                                    }
                                }
                            }
                            if (InTakenRel(3, -1)) // down side taken, we need to check sepearately from up side, in order to establish farSideStraightDown
                            {
                                farSideStraightDown = true;

                                int sideIndex = InTakenIndexRel(3, -1);
                                if (sideIndex < middleIndex) // area up
                                {
                                    if (CountAreaRel(1, 1, 2, 1, null, circleDirectionLeft, 1))
                                    {
                                        forbidden.Add(new int[] { x + lx, y + ly });
                                    }
                                    else if ((InTakenRel(1, -2) || InBorderRel(1, -2)) && !InTakenRel(1, -1))
                                    {
                                        forbidden.Add(new int[] { x + lx, y + ly });
                                    }
                                }
                                else
                                {
                                    farSideDown = true;
                                }
                            }
                        }

                        if (!farSideStraightUp && !farSideStraightDown)
                        {
                            if (InTakenRel(3, 1) && InTakenRel(3, 2) && !InTakenRel(1, 0) && !InTakenRel(1, 1) && !InTakenRel(1, 2)) // mid across up
                            {
                                farSideMidAcrossUp = true;

                                int middleIndex = InTakenIndexRel(3, 1);
                                int sideIndex = InTakenIndexRel(3, 2);
                                if (sideIndex > middleIndex) // area up
                                {
                                    farSideUp = true;

                                    if (CountAreaRel(1, 1, 2, 1, null, circleDirectionLeft, 1))
                                    {
                                        forbidden.Add(new int[] { x + lx, y + ly });
                                    }
                                    else if ((InTakenRel(1, -2) || InBorderRel(1, -2)) && !InTakenRel(1, -1))
                                    {
                                        forbidden.Add(new int[] { x + lx, y + ly });
                                    }
                                }
                            }

                            if (InTakenRel(3, -1) && InTakenRel(3, -2) && !InTakenRel(1, -1) && !InTakenRel(1, 0) && !InTakenRel(1, 1)) // mid across down, 1,1: 2023_1021_6
                            {
                                farSideMidAcrossDown = true;

                                int middleIndex = InTakenIndexRel(3, -1);
                                int sideIndex = InTakenIndexRel(3, -2);
                                if (sideIndex < middleIndex) // area up
                                {
                                    if (CountAreaRel(1, 0, 2, 0, null, circleDirectionLeft, 1))
                                    {
                                        forbidden.Add(new int[] { x + lx, y + ly });
                                    }
                                    else if (InTakenRel(1, -2))
                                    {
                                        forbidden.Add(new int[] { x + lx, y + ly });
                                    }
                                }
                                else
                                {
                                    farSideDown = true;
                                }
                            }
                        }

                        // there can be a far side across in the opposite direction of a far side straight or mid across situation
                        if (!farSideStraightUp && !farSideMidAcrossUp && InTakenRel(3, 2) && InTakenRel(3, 3) && !InTakenRel(1, 0) && !InTakenRel(1, 1) && !InTakenRel(1, 2)) // 1,2: 2023_1021
                        {
                            int middleIndex = InTakenIndexRel(3, 2);
                            int sideIndex = InTakenIndexRel(3, 3);
                            if (sideIndex > middleIndex) // area up
                            {
                                farSideUp = true;

                                if (CountAreaRel(1, 1, 2, 1, null, circleDirectionLeft, 1))
                                {
                                    forbidden.Add(new int[] { x + lx, y + ly });
                                }
                                else
                                {
                                    if ((InTakenRel(1, -2) || InBorderRel(1, -2)) && !InTakenRel(1, -1))
                                    {
                                        forbidden.Add(new int[] { x + lx, y + ly });
                                    }
                                    /*if (InTakenRel(4, 1) && !InTakenRel(3, 1)) // end C
                                    {
                                        forbidden.Add(new int[] { x + sx, y + sy });
                                    }*/
                                }
                            }
                        }

                        if (!farSideStraightDown && !farSideMidAcrossDown && InTakenRel(3, -2) && InTakenRel(3, -3) && !InTakenRel(1, -1) && !InTakenRel(1, 0) && !InTakenRel(1, 1) && !InTakenRel(2, -2)) // 2,-2: 9_630259
                        {
                            int middleIndex = InTakenIndexRel(3, -2);
                            int sideIndex = InTakenIndexRel(3, -3);

                            if (sideIndex < middleIndex) // area up
                            {
                                if (CountAreaRel(1, 0, 2, -1, new List<int[]> { new int[] { 2, 0 } }, circleDirectionLeft, 0))
                                {
                                    forbidden.Add(new int[] { x + lx, y + ly });
                                }
                                else if (InTakenRel(1, -2))
                                {
                                    forbidden.Add(new int[] { x + lx, y + ly });
                                }
                            }
                            else
                            {
                                farSideDown = true;
                            }
                        }
                    }

                    if (farSideUp && farSideDown) // 9_234256
                    {
                        forbidden.Add(new int[] { x + lx, y + ly });
                    }

                    lx = -lx;
                    ly = -ly;
                }
                lx = thisLx;
                ly = thisLy;
            }

            // we cannot have close side across down going down when farSideUp is true, because the area down has only one entrance.
        }

        void CheckNearBorder()
        {
            // going right on down side and going down on right side cases are checked by the CShape rule.

            // going up on left side
            if (x == 2 && leftField[0] == 1 && !InTakenAbs(leftField) && !InTaken(x - 1, y - 1))
            {
                forbidden.Add(leftField);
            }
            // going left on up side
            else if (y == 2 && rightField[1] == 1 && !InTakenAbs(rightField) && !InTaken(x - 1, y - 1))
            {
                forbidden.Add(rightField);
            }

            if (y == size - 1 && leftField[1] == size && !InTakenAbs(leftField) && !InTaken(x - 1, y + 1) && !InBorder(x - 1, y + 1)) // going left on down side
            {
                forbidden.Add(leftField);
            }
            else if (x == size - 1 && rightField[0] == size && !InTakenAbs(rightField) && !InTaken(x + 1, y - 1) && !InBorder(x + 1, y - 1)) //going up on right side
            {
                forbidden.Add(rightField);
            }

            //going towards an edge
            if (x + 2 * sx == 0)
            {
                forbidden.Add(leftField);
                if (!InTaken(x - 1, y - 1))
                {
                    forbidden.Add(straightField);
                }
            }
            else if (x + 2 * sx == size + 1)
            {
                forbidden.Add(rightField);
                if (!InTaken(x + 1, y - 1) && y != 1)
                {
                    forbidden.Add(straightField);
                }
            }
            else if (y + 2 * sy == 0)
            {
                forbidden.Add(rightField);
                if (!InTaken(x - 1, y - 1))
                {
                    forbidden.Add(straightField);
                }
            }
            else if (y + 2 * sy == size + 1)
            {
                forbidden.Add(leftField);
                if (!InTaken(x - 1, y + 1) && x != 1)
                {
                    forbidden.Add(straightField);
                }
            }
        }

        void CheckAreaNearBorder() // 2023_0909. Check both straight approach and side.
        {
            if (x == 3 && straightField[0] == 2 && !InTakenAbs(straightField) && !InTakenAbs(rightField) && !InTaken(1, y))
            {
                if (CountArea(2, y - 1, 1, y - 1, null, false, 1))
                {
                    forbidden.Add(straightField);
                    forbidden.Add(leftField);
                }
            }
            else if (y == 3 && straightField[1] == 2 && !InTakenAbs(straightField) && !InTakenAbs(leftField) && !InTaken(x, 1))
            {
                if (CountArea(x - 1, 2, x - 1, 1, null, true, 1))
                {
                    forbidden.Add(straightField);
                    forbidden.Add(rightField);
                }
            }
            else if (x == size - 2 && y >= 2 && straightField[0] == size - 1 && !InTakenAbs(straightField) && !InTakenAbs(leftField) && !InTaken(size, y))
            {
                if (CountArea(size - 1, y - 1, size, y - 1, null, true, 1))
                {
                    forbidden.Add(straightField);
                    forbidden.Add(rightField);
                }
            }
            else if (y == size - 2 && x >= 2 && straightField[1] == size - 1 && !InTakenAbs(straightField) && !InTakenAbs(rightField) && !InTaken(x, size))
            {
                if (CountArea(x - 1, size - 1, x - 1, size, null, false, 1))
                {
                    forbidden.Add(straightField);
                    forbidden.Add(leftField);
                }
            }
            else if (x == 3 && y >= 4 && leftField[0] == 2 && !InTaken(3, y - 1) && !InTaken(1, y) && !InTaken(2, y - 2)) //straight and left field cannot be taken, but it is enough we check the most left field on border. Also, 1 left and 2 up, 2 left and 2 up cannot be taken in order to draw an arealine. Checking 1 left and 2 up is enough.
            {
                if (CountArea(2, y - 1, 1, y - 1, null, false, 1))
                {
                    forbidden.Add(leftField);
                }
            }
            else if (y == 3 && x >= 4 && rightField[1] == 2 && !InTaken(x - 1, 3) && !InTaken(x, 1) && !InTaken(x - 2, 2))
            {
                if (CountArea(x - 1, 2, x - 1, 1, null, true, 1))
                {
                    forbidden.Add(rightField);
                }
            }
            else if (x == size - 2 && y >= 3 && rightField[0] == size - 1 && !InTaken(size - 2, y - 1) && !InTaken(size, y) && !InTaken(size - 1, y - 2))
            {
                if (CountArea(size - 1, y - 1, size, y - 1, null, true, 1))
                {
                    forbidden.Add(rightField);
                }
            }
            else if (y == size - 2 && x >= 3 && leftField[1] == size - 1 && !InTaken(x - 1, size - 2) && !InTaken(x, size) && !InTaken(x - 2, size - 1))
            {
                if (CountArea(x - 1, size - 1, x - 1, size, null, false, 1))
                {
                    forbidden.Add(leftField);
                }
            }
        }

        void CheckStraight_control()
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
                                forbidden.Add(new int[] { x - lx, y - ly });
                                if (!(InTakenRel(1, 1) || InBorderRel(1, 1)))
                                {
                                    forbidden.Add(new int[] { x + sx, y + sy });
                                }
                                else // C-shape
                                {
                                    forbidden.Add(new int[] { x - sx, y - sy });
                                }
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
                                else
                                {
                                    Straight3I = i;
                                    if (j == 0)
                                    {
                                        Straight3J = 0;
                                    }
                                    else if (j == 2)
                                    {
                                        Straight3J = 1;
                                    }
                                }

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
                                            break;
                                        case 2:
                                            if (ex == 2)
                                            {
                                                nowWCount = 0;
                                                nowWCountLeft = 1;
                                            }
                                            else
                                            {
                                                nowWCountLeft = nowWCount = (ex + 2) / 4;
                                                nowBCountLeft = nowBCount = (ex - 2) / 4;
                                                laterWCount = (ex - 2) / 4;
                                                laterBCount = (ex - 2) / 4;
                                            }
                                            break;
                                        case 3:
                                            nowWCountLeft = nowWCount = (ex + 1) / 4;
                                            nowBCountLeft = (ex - 7) / 4;
                                            nowBCount = (ex - 3) / 4;
                                            laterWCount = (ex + 1) / 4;
                                            laterBCount = (ex - 3) / 4;
                                            break;
                                    }

                                    if (!(whiteDiff <= nowWCount && whiteDiff >= -nowBCount))
                                    {
                                        forbidden.Add(new int[] { x + sx, y + sy });
                                    }
                                    if (!(whiteDiff <= nowWCountLeft && whiteDiff >= -nowBCountLeft))
                                    {
                                        forbidden.Add(new int[] { x + lx, y + ly });
                                        if (j == 2)
                                        {
                                            forbidden.Add(new int[] { x - sx, y - sy });
                                        }
                                    }
                                    if (!(whiteDiff <= laterWCount && whiteDiff >= -laterBCount))
                                    {
                                        forbidden.Add(new int[] { x - lx, y - ly });
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

        void CheckLeftRightAreaUp_control()
        {
            for (int i = 0; i < 2; i++)
            {
                bool circleDirectionLeft = (i == 0) ? true : false;

                for (int j = 0; j < 3; j++) // j = 1: small area, j = 2: big area
                {

                    bool circleValid = false;
                    int dist = 1;
                    List<int[]> borderFields = new();

                    while (!InTakenRel(1, dist) && !InBorderRel(1, dist))
                    {
                        dist++;
                    }

                    int ex = dist - 1;

                    if (ex != 0 && !InTakenRel(0, dist))
                    {
                        int i1 = InTakenIndexRel(1, dist);
                        int i2 = InTakenIndexRel(2, dist);

                        if (i2 > i1)
                        {
                            circleValid = true;
                        }
                    }

                    // double area addition
                    bool found = false;
                    for (int k = 1; k < dist; k++)
                    {
                        if (InTaken(0, k))
                        {
                            found = true;
                            break;
                        }
                    }

                    if (circleValid)
                    {
                        // Not actual with CheckNearField being applied at first.
                        if (ex == 1) // close mid across
                        {
                            forbidden.Add(new int[] { x + sx, y + sy });
                            forbidden.Add(new int[] { x - lx, y - ly });
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

                            if (CountAreaRel(1, 1, 1, ex, borderFields, circleDirectionLeft, 2, true))
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

                                        if (!found && (
                                            (InTakenRel(-2, dist) && !InTakenRel(-1, dist) && !InTakenRel(-2, dist - 1))
                                            ||
                                            (InTakenRel(-2, dist - 1) && !InTakenRel(-1, dist - 1) && !InTakenRel(-2, dist - 2))
                                            ) && -whiteDiff == ex / 4)
                                        {
                                            forbidden.Add(new int[] { x + lx, y + ly });
                                            forbidden.Add(new int[] { x + sx, y + sy });
                                        }
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

                                        if (!found && (
                                            (InTakenRel(-2, dist) && !InTakenRel(-1, dist) && !InTakenRel(-2, dist - 1))
                                            ||
                                            (InTakenRel(-2, dist - 1) && !InTakenRel(-1, dist - 1) && !InTakenRel(-2, dist - 2))
                                            ) && whiteDiff == (ex + 1) / 4)
                                        {
                                            forbidden.Add(new int[] { x + lx, y + ly });
                                        }
                                        break;
                                }

                                if (!(whiteDiff <= nowWCount && whiteDiff >= -nowBCount))
                                {
                                    forbidden.Add(new int[] { x + lx, y + ly });
                                }
                                if (!(whiteDiff <= laterWCount && whiteDiff >= -laterBCount))
                                {
                                    forbidden.Add(new int[] { x + sx, y + sy });
                                    forbidden.Add(new int[] { x - lx, y - ly });
                                }
                                else // We can enter later, check for start C on the opposite side (if the obstacle is up on the left, we check the straight field for next step C, not the right field.) 
                                // 9_465
                                {
                                    if (ex == 2 && !InTakenRel(-1, 1) && (InTakenRel(-2, 1) || InBorderRel(-2, 1)) && InTakenRel(-1, 0))
                                    {
                                        forbidden.Add(new int[] { x + sx, y + sy });
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

        void CheckLeftRightAreaUpBig()
        {
            for (int i = 0; i < 2; i++)
            {
                bool circleDirectionLeft = (i == 0) ? false : true;

                for (int j = 0; j < 3; j++) // j = 1: small area, j = 2: big area
                {
                    bool circleValid = false;
                    int dist = 1;
                    List<int[]> borderFields = new();

                    while (!InTakenRel(1, dist) && !InBorderRel(1, dist))
                    {
                        dist++;
                    }

                    int ex = dist - 1;
                    // if the obstacle is a border, we will also have the Straight rule.
                    if (ex != 0 && !InBorderRel(1, dist) && !InTakenRel(0, dist))
                    {
                        int i1 = InTakenIndexRel(1, dist);
                        int i2 = InTakenIndexRel(2, dist);

                        if (i1 > i2)
                        {
                            circleValid = true;
                        }
                    }

                    if (circleValid)
                    {
                        // Not actual with CheckNearField being applied at first.
                        if (ex == 1) // close mid across big
                        {
                            forbidden.Add(new int[] { x + sx, y + sy });
                            forbidden.Add(new int[] { x + lx, y + ly });
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

                                if (!(whiteDiff <= nowWCount && whiteDiff >= -nowBCount)) // not in range
                                {
                                    forbidden.Add(new int[] { x + sx, y + sy });
                                }
                                if (!(whiteDiff <= nowWCountRight && whiteDiff >= -nowBCount)) // not in range
                                {
                                    forbidden.Add(new int[] { x - lx, y - ly });
                                }
                                if (!(whiteDiff <= laterWCount && whiteDiff >= -laterBCount))
                                {
                                    forbidden.Add(new int[] { x + lx, y + ly });
                                }
                            }
                        }
                    }

                    if (j == 0) // rotate down (CCW): behind obstacle
                    {
                        int l0 = lx;
                        int l1 = ly;
                        lx = -sx;
                        ly = -sy;
                        sx = l0;
                        sy = l1;
                    }
                    else if (j == 1) // rotate up (CW): small area
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

        void CheckLeftRightCornerBig() // rotate down (CCW): 9_59438645 for behind and up for small area 
        {
            for (int i = 0; i < 2; i++)
            {
                bool circleDirectionLeft = (i == 0) ? false : true;

                for (int j = 0; j < 3; j++)
                {

                    int hori = 1;
                    int vert = 2;

                    while (!InTakenRel(hori, vert) && !InBorderRel(hori, vert))
                    {
                        bool circleValid = false;
                        hori++;

                        while (!InTakenRel(hori, vert) && !InBorderRel(hori, vert))
                        {
                            hori++;
                        }

                        if (InBorderRel(hori, vert))
                        {
                            vert++;
                            hori = 1;
                            continue;
                        }

                        // check field below to make sure we are at a corner, not a side wall
                        if (!InTakenRel(hori, vert - 1))
                        {
                            int i1 = InTakenIndexRel(hori, vert);
                            int i2 = InTakenIndexRel(hori, vert + 1);

                            if (i2 > i1)
                            {
                                circleValid = true;
                            }
                        }

                        if (circleValid)
                        {
                            if (hori == 2 && vert == 2) // close across, big if j = 0
                            {
                                forbidden.Add(new int[] { x + lx, y + ly });
                                if (j == 2) // close across small
                                {
                                    forbidden.Add(new int[] { x - sx, y - sy });
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

                                int nowWCount, nowWCountRight, nowBCount, laterWCount, laterBCount;
                                int a, n;

                                //check if all fields on the border line is free
                                if (vert == hori)
                                {
                                    a = hori - 1;
                                    nowWCountRight = nowWCount = 0;
                                    nowBCount = a - 1;
                                    laterWCount = -1;// means B = 1
                                    laterBCount = a - 1;

                                    for (int k = 1; k < hori; k++)
                                    {
                                        if (k < hori - 1)
                                        {
                                            if (InTakenRel(k, k) || InTakenRel(k, k + 1))
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
                                            borderFields.Add(new int[] { 1, 2 });
                                        }
                                        else if (k < hori - 1)
                                        {
                                            borderFields.Add(new int[] { k, k });
                                            borderFields.Add(new int[] { k, k + 1 });
                                        }
                                    }
                                }
                                else if (hori > vert)
                                {
                                    a = vert - 1;
                                    n = (hori - vert - (hori - vert) % 2) / 2;

                                    if ((hori - vert) % 2 == 0)
                                    {
                                        nowWCountRight = nowWCount = (n + 1 - (n + 1) % 2) / 2;
                                        nowBCount = a + (n - 1 - (n - 1) % 2) / 2;
                                        laterWCount = (n - n % 2) / 2;
                                        laterBCount = a + (n - n % 2) / 2;
                                    }
                                    else
                                    {
                                        nowWCountRight = nowWCount = 1 + (n + 1 - (n + 1) % 2) / 2;
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

                                    for (int k = 1; k < vert; k++)
                                    {
                                        if (k < vert - 1 && vert > 2)
                                        {
                                            if (InTakenRel(k, k) || InTakenRel(k, k + 1))
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

                                        if (vert > 2) // there is no stair if corner is at 1 distance, only one field which is the start field.
                                        {
                                            if (k == 1)
                                            {
                                                borderFields.Add(new int[] { 1, 2 });
                                            }
                                            else if (k < vert - 1)
                                            {
                                                borderFields.Add(new int[] { k, k });
                                                borderFields.Add(new int[] { k, k + 1 });
                                            }
                                            else
                                            {
                                                borderFields.Add(new int[] { k, k });
                                            }
                                        }
                                    }

                                    for (int k = 1; k <= hori - vert; k++)
                                    {
                                        if (InTakenRel(vert - 1 + k, vert - 1))
                                        {
                                            takenFound = true;
                                            break;
                                        }

                                        if (k < hori - vert)
                                        {
                                            borderFields.Add(new int[] { vert - 1 + k, vert - 1 });
                                        }
                                    }
                                }
                                else // vert > hori
                                {
                                    a = hori - 1;
                                    n = (vert - hori - (vert - hori) % 2) / 2;

                                    if ((vert - hori) % 2 == 0)
                                    {
                                        if (n > 1)
                                        {
                                            nowWCountRight = nowWCount = (n + 1 - (n + 1) % 2) / 2;
                                        }
                                        else
                                        {
                                            nowWCount = 0;
                                            nowWCountRight = 1;
                                        }
                                        nowBCount = a + (n - 1 - (n - 1) % 2) / 2;
                                        laterWCount = (n - n % 2) / 2;
                                        laterBCount = a + (n - n % 2) / 2;
                                    }
                                    else
                                    {
                                        if (n > 0)
                                        {
                                            nowWCountRight = nowWCount = a + (n - n % 2) / 2;
                                            laterBCount = (n + 2 - (n + 2) % 2) / 2;
                                        }
                                        else
                                        {
                                            nowWCount = a - 1;
                                            nowWCountRight = a;
                                            laterBCount = 0;
                                        }
                                        nowBCount = (n + 1 - (n + 1) % 2) / 2;
                                        laterWCount = a - 1 + (n + 1 - (n + 1) % 2) / 2;

                                    }

                                    for (int k = 1; k <= vert - hori; k++)
                                    {
                                        if (InTakenRel(1, k))
                                        {
                                            takenFound = true;
                                            break;
                                        }

                                        if (k > 1)
                                        {
                                            borderFields.Add(new int[] { 1, k });
                                        }
                                    }

                                    for (int k = 1; k < hori; k++)
                                    {
                                        if (k < hori - 1)
                                        {
                                            if (InTakenRel(k, vert - hori + k) || InTakenRel(k, vert - hori + k + 1))
                                            {
                                                takenFound = true;
                                                break;
                                            }
                                        }
                                        else
                                        {
                                            if (InTakenRel(k, vert - hori + k))
                                            {
                                                takenFound = true;
                                                break;
                                            }
                                        }

                                        if (k < hori - 1)
                                        {
                                            borderFields.Add(new int[] { k, vert - hori + k });
                                            borderFields.Add(new int[] { k, vert - hori + k + 1 });
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

                                    // here, true means that count area succeeds, does not run into an error
                                    if (CountAreaRel(left1, straight1, left2, straight2, newBorderFields, circleDirectionLeft, 2, true))
                                    {
                                        int black = (int)info[1];
                                        int white = (int)info[2];

                                        int whiteDiff = white - black;

                                        if (!(whiteDiff <= nowWCount && whiteDiff >= -nowBCount)) // not in range
                                        {
                                            forbidden.Add(new int[] { x + sx, y + sy });
                                        }
                                        if (!(whiteDiff <= nowWCountRight && whiteDiff >= -nowBCount)) // not in range
                                        {
                                            forbidden.Add(new int[] { x - lx, y - ly });
                                        }
                                        if (!(whiteDiff <= laterWCount && whiteDiff >= -laterBCount))
                                        {
                                            forbidden.Add(new int[] { x + lx, y + ly });
                                            // for small area
                                            if (j == 2)
                                            {
                                                forbidden.Add(new int[] { x - sx, y - sy });
                                            }
                                        }
                                    }
                                }
                            }
                        }

                        vert++;
                        hori = 1;
                    }

                    if (j == 0) // rotate down (CCW): behind obstacle
                    {
                        int l0 = lx;
                        int l1 = ly;
                        lx = -sx;
                        ly = -sy;
                        sx = l0;
                        sy = l1;
                    }
                    else if (j == 1) // rotate up (CW): small area
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

        void Check3DoubleArea() // the distance to the obstacle is maximum 3. Line cannot finish at the far corner, but at the field below. There is a second area created with an obstacle on the right side.

        // First case: 2024_0529_4 across
        // Second case: 2024_0529_5 across
        // Third case: 2024_0529_3 across

        // C-shape checking is not necessary for first case: 2024_0529, 2024_0529_2 is solved by the straight obstacle single area rule
        // C-shape checking is not necessary for second case: 2024_0529_1 is solved by the straight obstacle single area rule

        // has to be rotated CCW in first area case
        {
            for (int i = 0; i < 2; i++)
            {
                for (int j = 0; j < 2; j++)
                {
                    bool circleValid = false;
                    bool circleDirectionLeft = (i == 0) ? true : false;
                    int startX = 0, startY = 0, endX = 0, endY = 0;
                    int forbidX = 0, forbidY = 0;
                    int circleParity = 0;
                    int caseNumber = 0;

                    List<int[]> borderFields = new();

                    // First two cases can be simultaneously true, as in 2024_0505. If the bigger area is pair, so is the smaller, which would cause a C-shape with the obstacle in (1, 4). So we can just examine the smaller area. borderFields needs to be reset in that case.

                    if (InTakenRel(1, 4) && !InTakenRel(0, 2) && !InTakenRel(0, 3) && !InTakenRel(0, 4) && !InTakenRel(1, 1) && !InTakenRel(1, 3))
                    {
                        int i1 = InTakenIndexRel(1, 4);
                        int i2 = InTakenIndexRel(2, 4);

                        if (i2 > i1)
                        {
                            circleValid = true;

                            startX = 0;
                            startY = 1;
                            endX = 0;
                            endY = 3;
                            forbidX = 0;
                            forbidY = 1;
                            borderFields.Add(new int[] { 0, 2 });
                            circleParity = 0;
                            caseNumber = 1;
                        }
                    }

                    if (InTakenRel(2, 3) && !InTakenRel(1, 0) && !InTakenRel(1, 1) && !InTakenRel(1, 2) && !InTakenRel(1, 3) && !InTakenRel(0, 2) && !InTakenRel(2, 2))
                    {
                        int i1 = InTakenIndexRel(2, 3);
                        int i2 = InTakenIndexRel(3, 3);

                        if (i2 > i1)
                        {
                            circleValid = true;

                            startX = 1;
                            startY = 1;
                            endX = 1;
                            endY = 2;
                            forbidX = 1;
                            forbidY = 0;
                            borderFields = new();
                            circleParity = 0;
                            caseNumber = 2;
                        }
                    }

                    if (InTakenRel(2, 4) && !InTakenRel(1, 0) && !InTakenRel(1, 1) && !InTakenRel(1, 2) && !InTakenRel(1, 3) && !InTakenRel(1, 4) && !InTakenRel(2, 1) && !InTakenRel(2, 3))
                    {
                        int i1 = InTakenIndexRel(2, 4);
                        int i2 = InTakenIndexRel(3, 4);

                        if (i2 > i1)
                        {
                            circleValid = true;

                            startX = 1;
                            startY = 1;
                            endX = 1;
                            endY = 3;
                            forbidX = 1;
                            forbidY = 0;
                            borderFields.Add(new int[] { 1, 2 });
                            circleParity = 1;
                            caseNumber = 3;
                        }
                    }

                    if (circleValid && CountAreaRel(startX, startY, endX, endY, borderFields, circleDirectionLeft, 2, true))
                    {
                        int black = (int)info[1];
                        int white = (int)info[2];

                        if (circleParity == 0 && black == white || circleParity == 1 && black == white + 1)
                        {
                            int thisX = x;
                            int thisY = y;
                            int thisSx = sx;
                            int thisSy = sy;
                            int thisLx = lx;
                            int thisLy = ly;

                            int[] rotatedDir = RotateDir(lx, ly, i);
                            lx = rotatedDir[0];
                            ly = rotatedDir[1];
                            rotatedDir = RotateDir(sx, sy, i);
                            sx = rotatedDir[0];
                            sy = rotatedDir[1];

                            if (caseNumber < 3)
                            {
                                x = x + endX * thisLx + endY * thisSx;
                                y = y + endX * thisLy + endY * thisSy;

                                if (CheckNearFieldSmall1()) // check only mid across
                                {
                                    switch (caseNumber)
                                    {
                                        case 1:
                                            DoubleArea1 = true;
                                            activeRules.Add("Double Area first case");
                                            activeRuleSizes.Add(new int[] { 4, 6 });
                                            break;
                                        case 2:
                                            DoubleArea2 = true;
                                            activeRules.Add("Double Area second case");
                                            activeRuleSizes.Add(new int[] { 4, 5 });
                                            break;
                                    }

                                    activeRulesForbiddenFields.Add(new List<int[]> { new int[] { thisX + forbidX * thisLx + forbidY * thisSx, thisY + forbidX * thisLy + forbidY * thisSy } });

                                    forbidden.Add(new int[] { thisX + forbidX * thisLx + forbidY * thisSx, thisY + forbidX * thisLy + forbidY * thisSy });
                                }
                            }
                            else
                            {
                                x = x + endX * thisLx + (endY - 1) * thisSx;
                                y = y + endX * thisLy + (endY - 1) * thisSy;

                                if (CheckNearFieldSmall1()) // check mid across or across
                                {
                                    DoubleArea3 = true;
                                    activeRules.Add("Double Area third case");
                                    activeRuleSizes.Add(new int[] { 4, 5 });

                                    activeRulesForbiddenFields.Add(new List<int[]> { new int[] { thisX + forbidX * thisLx + forbidY * thisSx, thisY + forbidX * thisLy + forbidY * thisSy } });

                                    forbidden.Add(new int[] { thisX + forbidX * thisLx + forbidY * thisSx, thisY + forbidX * thisLy + forbidY * thisSy });
                                }
                            }

                            x = thisX;
                            y = thisY;
                            lx = thisLx;
                            ly = thisLy;
                            sx = thisSx;
                            sy = thisSy;
                        }
                    }
                    // rotate up. Clockwise on left side and counter-clockwise on right side.
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

        bool Check3DoubleAreaRotated(int side = -1) // Take only the first case and rotate it.
        {
            for (int i = 0; i < 2; i++)
            {
                if (side != -1 && side != i) continue;

                bool circleValid = false;
                bool circleDirectionLeft = (i == 0) ? true : false;
                int sx = 0, sy = 0, ex = 0, ey = 0;

                List<int[]> borderFields = new();

                if (InTakenRel(4, -1) && !InTakenRel(2, 0) && !InTakenRel(3, 0) && !InTakenRel(4, 0) && !InTakenRel(1, -1) && !InTakenRel(3, -1))
                {
                    int i1 = InTakenIndexRel(4, -1);
                    int i2 = InTakenIndexRel(4, -2);

                    if (i2 > i1)
                    {
                        circleValid = true;

                        sx = 1;
                        sy = 0;
                        ex = 3;
                        ey = 0;
                        borderFields.Add(new int[] { 2, 0 });
                    }
                }

                if (circleValid && CountAreaRel(sx, sy, ex, ey, borderFields, circleDirectionLeft, 2, true))
                {
                    int black = (int)info[1];
                    int white = (int)info[2];

                    if (black == white)
                    {
                        int thisX = x;
                        int thisY = y;

                        x = x + ex * lx + ey * thisSx;
                        y = y + ex * ly + ey * thisSy;

                        // Checking C-Shape not necessary, side straight will take care of it, because area is 1B.
                        if (CheckNearFieldSmall1())
                        {
                            if (side != -1)
                            {
                                return true; // We are only interested in the side the straight obstacle is going to. Both sides cannot be true at the same time.
                            }

                            DoubleArea1Rotated = true;
                            activeRules.Add("Double Area first case rotated");
                            activeRuleSizes.Add(new int[] { 6, 4 });
                            activeRulesForbiddenFields.Add(new List<int[]> { new int[] { thisX + lx, thisY + ly } });

                            forbidden.Add(new int[] { thisX + lx, thisY + ly });
                        }

                        x = thisX;
                        y = thisY;
                    }
                }

                lx = -lx;
                ly = -ly;
            }
            lx = thisLx;
            ly = thisLy;

            return false;
        }

        void CheckSequence()
        {
            for (int i = 0; i < 2; i++)
            {
                bool circleDirectionLeft = (i == 0) ? true : false;

                for (int j = 0; j < 2; j++)
                {
                    // First case

                    // Triple Area
                    // 2024_0516_2
                    // Rotated: 2024_0516_3

                    // See also 9_665575 for alternative start obstacle placement
                    bool circleValid = false;

                    if (((InBorderRelExact(0, 4) && !InCornerRel(0, 3)) || InTakenRel(0, 4) || InTakenRel(-1, 4)) && !InTakenRel(0, 1) && !InTakenRel(0, 2) && !InTakenRel(0, 3) && !InTakenRel(1, 1) && !InTakenRel(1, 3) && !InTakenRel(-1, 3))
                    {
                        if (InBorderRelExact(0, 4))
                        {
                            int directionFieldIndex = InBorderIndexRel(0, 4);
                            int sideIndex = InBorderIndexRel(1, 4);

                            if (sideIndex < directionFieldIndex)
                            {
                                circleValid = true;
                            }
                        }
                        else if (InTakenRel(0, 4))
                        {
                            int directionFieldIndex = InTakenIndexRel(0, 4);
                            int sideIndex = InTakenIndexRel(1, 4);

                            if (sideIndex != -1)
                            {
                                if (sideIndex > directionFieldIndex)
                                {
                                    circleValid = true;
                                }

                            }
                            else
                            {
                                sideIndex = InTakenIndexRel(-1, 4);
                                if (directionFieldIndex > sideIndex)
                                {
                                    circleValid = true;
                                }
                            }
                        }
                        else
                        {
                            int directionFieldIndex = InTakenIndexRel(-1, 4);
                            int sideIndex = InTakenIndexRel(-2, 4);
                            if (directionFieldIndex > sideIndex)
                            {
                                circleValid = true;
                            }
                        }

                        if (circleValid)
                        {
                            if (CountAreaRel(0, 1, 0, 3, new List<int[]> { new int[] { 0, 2 } }, circleDirectionLeft, 2, true))
                            {
                                int black = (int)info[1];
                                int white = (int)info[2];

                                if (black == white)
                                {
                                    int thisX = x;
                                    int thisY = y;
                                    int thisSx = sx;
                                    int thisSy = sy;
                                    int thisLx = lx;
                                    int thisLy = ly;

                                    //List<int[]> thisPath = Copy(path);
                                    // necessary for checking C-shape on the left side
                                    //path.Add(new int[] { x + 3 * sx, y + 3 * sy });

                                    // step after exiting area:
                                    x = x - lx + 2 * sx;
                                    y = y - ly + 2 * sy;

                                    //only necessary if recursive function will be used: path.Add(new int[] { x, y });

                                    int[] rotatedDir = RotateDir(lx, ly, i);
                                    lx = rotatedDir[0];
                                    ly = rotatedDir[1];
                                    rotatedDir = RotateDir(sx, sy, i);
                                    sx = rotatedDir[0];
                                    sy = rotatedDir[1];

                                    // does not use C-shape up, only left
                                    bool leftSideClose = CheckNearFieldSmall1_5();

                                    lx = -lx;
                                    ly = -ly;

                                    bool rightSideClose = CheckNearFieldSmall1();

                                    lx = -lx;
                                    ly = -ly;

                                    if (leftSideClose && rightSideClose)
                                    {
                                        Sequence1 = true;
                                        activeRules.Add("Sequence first case");
                                        activeRuleSizes.Add(new int[] { 5, 5 });
                                        activeRulesForbiddenFields.Add(new List<int[]> { new int[] { thisX + thisSx, thisY + thisSy } });

                                        // Due to CheckStraight, stepping left is already disabled when the obstacle is straight ahead. When it is one to the right, we need the left field to be disabled.
                                        forbidden.Add(new int[] { thisX + thisLx, thisY + thisLy });
                                        forbidden.Add(new int[] { thisX + thisSx, thisY + thisSy });
                                    }

                                    x = thisX;
                                    y = thisY;
                                    lx = thisLx;
                                    ly = thisLy;
                                    sx = thisSx;
                                    sy = thisSy;
                                    //path = thisPath;
                                }
                            }
                        }
                    }

                    // Second case

                    // Square 4 x 2 C-Shape / Square 4 x 2 Area
                    // 2024_0516
                    // Rotated: 2024_0516_1

                    // Double Area Stair
                    // 2024_0516_4
                    // Rotated: 2024_0516_5
                    if (InTakenRel(0, 3) && !InTakenRel(0, 1) && !InTakenRel(0, 2) && !InTakenRel(-1, 3))
                    {
                        int directionFieldIndex = InTakenIndexRel(0, 3);
                        int leftIndex = InTakenIndexRel(1, 3);

                        if (leftIndex > directionFieldIndex)
                        {
                            if (CountAreaRel(0, 1, 0, 2, null, circleDirectionLeft, 2, true))
                            {
                                int black = (int)info[1];
                                int white = (int)info[2];

                                if (black == white)
                                {
                                    int thisX = x;
                                    int thisY = y;
                                    int thisSx = sx;
                                    int thisSy = sy;
                                    int thisLx = lx;
                                    int thisLy = ly;

                                    //List<int[]> thisPath = Copy(path);
                                    //path.Add(new int[] { x + sx, y + sy });
                                    // step after exiting area:
                                    x = x - lx + 2 * sx;
                                    y = y - ly + 2 * sy;

                                    //path.Add(new int[] { x, y });

                                    int[] rotatedDir = RotateDir(lx, ly, i);
                                    lx = rotatedDir[0];
                                    ly = rotatedDir[1];
                                    rotatedDir = RotateDir(sx, sy, i);
                                    sx = rotatedDir[0];
                                    sy = rotatedDir[1];

                                    if (CheckSequenceRecursive(i))
                                    {
                                        Sequence2 = true;
                                        activeRules.Add("Sequence second case");
                                        activeRuleSizes.Add(new int[] { 1, 1 });
                                        activeRulesForbiddenFields.Add(new List<int[]> { new int[] { thisX + thisLx, thisY + thisLy }, new int[] { thisX + thisSx, thisY + thisSy } });

                                        forbidden.Add(new int[] { thisX + thisLx, thisY + thisLy });
                                        forbidden.Add(new int[] { thisX + thisSx, thisY + thisSy });
                                    }

                                    x = thisX;
                                    y = thisY;
                                    lx = thisLx;
                                    ly = thisLy;
                                    sx = thisSx;
                                    sy = thisSy;
                                    //path = thisPath;
                                }
                            }
                        }
                    }

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

            // Third case, Double Area Stair 2
            // 2024_0516_6
            // Rotated both ways: 2024_0516_7, 2024_0516_8

            for (int i = 0; i < 2; i++)
            {
                bool circleDirectionLeft = (i == 0) ? true : false;

                for (int j = 0; j < 3; j++)
                {
                    if (InTakenRel(1, 3) && !InTakenRel(1, 2) && !InTakenRel(0, 3))
                    {
                        int directionFieldIndex = InTakenIndexRel(1, 3);
                        int leftIndex = InTakenIndexRel(2, 3);

                        if (leftIndex > directionFieldIndex)
                        {
                            if (CountAreaRel(1, 1, 1, 2, null, circleDirectionLeft, 2, true))
                            {
                                int black = (int)info[1];
                                int white = (int)info[2];

                                if (black == white)
                                {
                                    // first circle true

                                    int thisX = x;
                                    int thisY = y;
                                    int thisSx = sx;
                                    int thisSy = sy;
                                    int thisLx = lx;
                                    int thisLy = ly;

                                    //List<int[]> thisPath = Copy(path);
                                    //path.Add(new int[] { x + sx, y + sy });
                                    // step after exiting area:
                                    x = x + 2 * sx;
                                    y = y + 2 * sy;

                                    //path.Add(new int[] { x, y });

                                    int[] rotatedDir = RotateDir(lx, ly, i);
                                    lx = rotatedDir[0];
                                    ly = rotatedDir[1];
                                    rotatedDir = RotateDir(sx, sy, i);
                                    sx = rotatedDir[0];
                                    sy = rotatedDir[1];

                                    if (CheckSequenceRecursive(i))
                                    {
                                        Sequence3 = true;
                                        activeRules.Add("Sequence third case");
                                        activeRuleSizes.Add(new int[] { 1, 1 });
                                        activeRulesForbiddenFields.Add(new List<int[]> { new int[] { thisX + thisSx, thisY + thisSy } });

                                        forbidden.Add(new int[] { thisX + thisSx, thisY + thisSy });
                                    }

                                    x = thisX;
                                    y = thisY;
                                    lx = thisLx;
                                    ly = thisLy;
                                    sx = thisSx;
                                    sy = thisSy;
                                    //path = thisPath;
                                }
                            }
                        }
                    }

                    if (j == 0)
                    {
                        int l0 = lx;
                        int l1 = ly;
                        lx = -sx;
                        ly = -sy;
                        sx = l0;
                        sy = l1;
                    }
                    else if (j == 1)
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

        bool CheckSequenceRecursive(int j)
        {
            newExitField = new int[] { 0, 0 };

            bool leftSideClose = CheckNearFieldSmall2(true);
            bool rightSideClose = CheckNearFieldSmall2(false);

            if (leftSideClose && rightSideClose)
            {
                return true;
            }
            // right side close can happen with the future line
            // for now, we only take the right side C-shape into account as it happens in 9_740293. Other close obstacles we don't check.
            else if (leftSideClose)
            //else if ((leftSideClose || rightSideClose) && newExitField[0] != 0)
            {
                x = newExitField[0];
                y = newExitField[1];
                //path.Add(new int[] { x, y });

                if (newDirectionRotated)
                {
                    int[] rotatedDir = RotateDir(lx, ly, j);
                    lx = rotatedDir[0];
                    ly = rotatedDir[1];
                    rotatedDir = RotateDir(sx, sy, j);
                    sx = rotatedDir[0];
                    sy = rotatedDir[1];
                }

                return CheckSequenceRecursive(j);
            }
            else
            {
                return false;
            }
        }

        bool CheckDownStair(int side = -1, int nLx = 0, int nLy = 0, int nSx = 0, int nSy = 0)
        {
            for (int i = 0; i < 2; i++)
            {
                for (int j = 0; j < 2; j++)
                {
                    if (side != -1 && !(side == i && j == 0)) continue; // if it is a next step checking, the pattern will not be rotated.

                    if (side != -1) // if it is right side, cycling through left side would change the values.
                    {
                        lx = nLx;
                        ly = nLy;
                        sx = nSx;
                        sy = nSy;
                    }

                    if (!InTakenRel(1, 0))
                    {
                        int hori = 1;
                        int vert = -1;


                        while (!InTakenRel(hori, vert) && InTakenRel(hori, vert - 1))
                        {
                            hori++;
                            vert--;
                        }

                        if (InTakenRel(hori, vert) && !InTakenRel(hori + 1, vert + 1) && InTakenRel(hori + 2, vert + 1))
                        {
                            hori++;
                            vert += 2;

                            while (!InTakenRel(hori, vert) && vert <= 0)
                            {
                                int thisX = x;
                                int thisY = y;

                                x = thisX + lx * hori + sx * vert;
                                y = thisY + ly * hori + sy * vert;

                                if (CheckNearFieldSmall1())
                                {
                                    if (side != -1)
                                    {
                                        return true; // For next step checking
                                    }

                                    DownStair = true;
                                    activeRules.Add("Down Stair");
                                    activeRuleSizes.Add(new int[] { 7, 8 });
                                    activeRulesForbiddenFields.Add(new List<int[]> { new int[] { thisX + lx, thisY + ly } });

                                    forbidden.Add(new int[] { thisX + lx, thisY + ly });

                                }

                                x = thisX;
                                y = thisY;

                                hori--;
                                vert++;
                            }
                        }
                    }

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

            return false;
        }

        void Check3DistNextStep()
        {
            for (int i = 0; i < 2; i++)
            {
                for (int j = 0; j < 2; j++)
                {
                    if (Straight3I == i && Straight3J == j) // same side and rotation
                    {
                        int thisX = x;
                        int thisY = y;

                        x = thisX + sx;
                        y = thisY + sy;

                        // Check3DoubleAreaRotated will change x, y, lx and ly
                        // CheckDownStair may change sx and sy
                        int tempX = x;
                        int tempY = y;
                        int tempLx = lx;
                        int tempLy = ly;
                        int tempSx = sx;
                        int tempSy = sy;

                        // Both: 9_18677343

                        // Double Area only: 9_59434452
                        if (Check3DoubleAreaRotated(i))
                        {
                            DoubleAreaFirstCaseRotatedNext = true;
                            activeRules.Add("Double Area first case rotated next");
                            activeRuleSizes.Add(new int[] { 6, 4 });
                            activeRulesForbiddenFields.Add(new List<int[]> { new int[] { thisX + sx, thisY + sy }, new int[] { thisX - lx, thisY - ly } });

                            forbidden.Add(new int[] { thisX + sx, thisY + sy });
                            forbidden.Add(new int[] { thisX - lx, thisY - ly });
                        }

                        x = tempX;
                        y = tempY;
                        lx = tempLx;
                        ly = tempLy;

                        // Stair only: 2024_0604
                        if (CheckDownStair(i, lx, ly, sx, sy))
                        {
                            DownStairNext = true;
                            activeRules.Add("Down Stair next");
                            activeRuleSizes.Add(new int[] { 8, 8 });
                            activeRulesForbiddenFields.Add(new List<int[]> { new int[] { thisX + sx, thisY + sy }, new int[] { thisX - lx, thisY - ly } });

                            forbidden.Add(new int[] { thisX + sx, thisY + sy });
                            forbidden.Add(new int[] { thisX - lx, thisY - ly });
                        }

                        x = thisX;
                        y = thisY;
                        lx = tempLx;
                        ly = tempLy;
                        sx = tempSx;
                        sy = tempSy;
                    }

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

        bool CheckNearFieldSmall1() // for use only with Double Area case 1, 2, 3 and 1 rotated, and Down Stair. Across is needed at 9_53144883
        {
            // close mid across. In DirectionalArea, the empty fields are already checked.
            if (InTakenRel(1, 2) && !InTakenRel(0, 2) && !InTakenRel(1, 1))
            {
                int middleIndex = InTakenIndexRel(1, 2);
                int sideIndex = InTakenIndexRel(2, 2);

                if (sideIndex > middleIndex)
                {

                    return true;
                }
            }

            // close across
            if (InTakenRel(2, 2) && !InTakenRel(1, 2) && !InTakenRel(2, 1))
            {
                int middleIndex = InTakenIndexRel(2, 2);
                int sideIndex = InTakenIndexRel(3, 2);

                if (sideIndex > middleIndex)
                {
                    return true;
                }
            }

            return false;
        }

        bool CheckNearFieldSmall1_5() // for use only with Double Area case 1, 2, 3 and 1 rotated
        {
            // C-shape (left)
            if ((InTakenRel(2, 0) || InBorderRel(2, 0)) && !InTakenRel(1, 0))
            {
                return true;
            }

            // close mid across. In DirectionalArea, the empty fields are already checked.
            if (InTakenRel(1, 2) && !InTakenRel(0, 2) && !InTakenRel(1, 1))
            {
                int middleIndex = InTakenIndexRel(1, 2);
                int sideIndex = InTakenIndexRel(2, 2);

                if (sideIndex > middleIndex)
                {

                    return true;
                }
            }

            // close across
            if (InTakenRel(2, 2) && !InTakenRel(1, 2) && !InTakenRel(2, 1))
            {
                int middleIndex = InTakenIndexRel(2, 2);
                int sideIndex = InTakenIndexRel(3, 2);

                if (sideIndex > middleIndex)
                {
                    return true;
                }
            }

            return false;
        }

        bool CheckNearFieldSmall2(bool leftSide = true) // for use with Sequence
        {
            bool ret = false;

            if (!leftSide)
            {
                lx = -lx;
                ly = -ly;
            }
            else
            {
                // C-Shape, only left side should have it
                // Checking for InTakenRel(1, -1) is not possible, because in Sequence first case, we are exiting the area at the middle border field.
                // But when it comes to the right side (if it was checked), it is necessary, otherwise we can detect a C-shape with the live end as in 9_213.
                if (InTakenRel(2, 0) && !InTakenRel(1, 0))
                {
                    ret = true;

                    newExitField = new int[] { x + lx + sx, y + ly + sy };
                    newDirectionRotated = false;
                }

                //C-Shape up
                if (InTakenRel(0, 2) && InTakenRel(1, 1) && !InTakenRel(0, 1))
                {
                    ret = true;

                    newExitField = new int[] { x - lx + sx, y - ly + sy };
                    newDirectionRotated = true;
                }
            }

            // close mid across
            if (InTakenRel(1, 2) && !InTakenRel(0, 2) && !InTakenRel(1, 1))
            {
                int middleIndex = InTakenIndexRel(1, 2);
                int sideIndex = InTakenIndexRel(2, 2);

                if (sideIndex > middleIndex)
                {
                    ret = true;

                    if (leftSide)
                    {
                        // mid across overwrites C-shape
                        newExitField = new int[] { x + sx, y + sy };
                        newDirectionRotated = true;
                    }
                }
            }

            // close across. Checking empty fields necessary, see 9_29558469
            if (InTakenRel(2, 2) && !InTakenRel(1, 2) && !InTakenRel(2, 1))
            {
                int middleIndex = InTakenIndexRel(2, 2);
                int sideIndex = InTakenIndexRel(3, 2);

                if (sideIndex > middleIndex)
                {
                    ret = true;

                    if (leftSide)
                    {
                        newExitField = new int[] { x + lx + sx, y + ly + sy };
                        newDirectionRotated = true;
                    }
                }
            }

            if (!leftSide)
            {
                lx = -lx;
                ly = -ly;
            }

            return ret;
        }

        int[] RotateDir(int xDiff, int yDiff, int ccw)
        {
            List<int[]> directions;

            if (ccw == 0) // clockwise
            {
                directions = new List<int[]> { new int[] { 0, 1 }, new int[] { -1, 0 }, new int[] { 0, -1 }, new int[] { 1, 0 } };
            }
            else // counter-clockwise
            {
                directions = new List<int[]> { new int[] { 0, 1 }, new int[] { 1, 0 }, new int[] { 0, -1 }, new int[] { -1, 0 } };
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

            int turnedDirection = currentDirection == 3 ? 0 : currentDirection + 1;

            return directions[turnedDirection];
        }
        // ----- copy end -----
    }
}