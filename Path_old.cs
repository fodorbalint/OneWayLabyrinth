// obsolete functions and notes

namespace OneWayLabyrinth
{
    public partial class Path
    {
        int directionFieldIndex = 0;
        public bool Future2x2StartEnd = false;
        public bool Future2x3StartEnd = false;
        public bool Future3x3StartEnd = false;
        public bool FutureL = false;
        public bool TripleAreaExitDown = false;

        bool closeStraightSmall = false;
        bool closeMidAcrossSmall = false;
        bool closeAcrossSmall = false;
        bool closeStraightLarge = false;
        bool closeMidAcrossLarge = false;
        bool closeAcrossLarge = false;

        int Straight3I = -1; // used for checking Down Stair and Double Area first case rotated at the next step
        int Straight3J = -1;
        bool DirectionalArea, DoubleArea1, DoubleArea2, DoubleArea3, DoubleArea4, DoubleArea1Rotated, DownStairClose, DownStair = false;
        bool DoubleAreaFirstCaseRotatedNext, DownStairNext = false;

        int[] newExitField0 = new int[] { 0, 0 };
        int[] newExitField = new int[] { 0, 0 };
        bool newDirectionRotated = false; // if rotated, it is CW on left side

        // Notes for NextStepPossibilities()

        // To speed up execution, we check for a C-Shape and close obstacle first. If only one possible field remains, we don't check more. If it is an error, we will see it later.

        /*activeRules = new();
        activeRulesForbiddenFields = new();
        activeRuleSizes = new();*/

        /* ---- uncomment to enable basic rule checking first ---- */

        /*CShape = false;

        CheckCShape();

        if (CShape) break;

        closeStraightSmall = false;
        closeMidAcrossSmall = false;
        closeAcrossSmall = false;
        closeStraightLarge = false;
        closeMidAcrossLarge = false;

        // needed for far left and right case 9_234320
        //CheckNearField();

        if (closeStraightSmall || closeMidAcrossSmall || closeAcrossSmall || closeStraightLarge || closeMidAcrossLarge) break;

        CheckNearBorder();
        CheckAreaNearBorder();

        newPossible = new();
        foreach (int[] field in possible)
        {
            if (!InForbidden(field))
            {
                newPossible.Add(field);
            }
        }
        possible = newPossible;

        if (possible.Count == 1) break;*/

        /* ---- uncomment to disable advanced rules ---- */
        // break;

        /* DirectionalArea = DoubleArea1 = DoubleArea2 = DoubleArea3 = DoubleArea4 = DoubleArea1Rotated = DownStairClose = DownStair = false;
        DoubleAreaFirstCaseRotatedNext = DownStairNext = false; */

        /*List<int[]> startForbiddenFields = Copy(forbidden);

        window.ShowActiveRules(activeRules, activeRulesForbiddenFields, startForbiddenFields, activeRuleSizes);*/

        /*T("DirectionalArea: " + DirectionalArea + "\n" + "DoubleArea1: " + DoubleArea1 + "\n" + "DoubleArea2: " + DoubleArea2 + "\n" + "DoubleArea3: " + DoubleArea3 + "\n" + "DoubleArea4: " + DoubleArea4 + "\n" + "DoubleArea1Rotated: " + DoubleArea1Rotated + "\n" + "Sequence1: " + Sequence1 + "\n" + "Sequence2: " + Sequence2 + "\n" + "Sequence3: " + Sequence3 + "\n" + "DownStairClose: " + DownStairClose + "\n" + "DownStair: " + DownStair + "\n" + "DoubleAreaFirstCaseRotatedNext: " + DoubleAreaFirstCaseRotatedNext + "\n" + "DownStairNext: " + DownStairNext);*/

        /* 5 x 5: CheckNearCorner: 2023_0811_2

        Used rules for 7 x 7:
            * Side back
            * Side front
            * Side front L
            * Future L
            2023_0827, 2023_0827_1
            the start and end fields have to be in the same section, otherwise they can connect, like in 2023_0913
            conditions are true already on 5x5 at 2023_0831_1, but it is handled in CheckNearCorner

            * Future 2 x 2 Start End
            * 2023_0909_1, 2023_0909_2
            * On boards larger than 7 x 7, is it possible to apply the rule in straight left/right directions? It means that in the original example, the line is coming downwards instead of heading right.

            * Future 2 x 3 Start End
                2023_0915
                Is there a situation where the start and end fields are not part of one future line?
                On boards larger than 7 x 7, is it possible to apply the rule in straight left/right directions? It means that in the original example, the line is coming downwards instead of heading right.

            * Future 3 x 3 Start End
                2023_0916

        * Notes for 9 x 9:

        * if there is a close across large obstacle leading to a large area, there can be valid rules on the other side, see 9_2707632
        * CheckAreaNearBorder() uses countarea, see 2023_0909. A 2x2 area would be created with one way to go in and out
        * With the exception of closeAcross large area, all near field rules disable two fields, leaving only one option. Running further rules are not necessary. 
        * Example of interference: 2023_1031_1
        * CountArea3x3 2,2: 2023_1021_1

        Check1x3: 2023_0430_2 */

        public void RunRules()
        {
            activeRules = new();
            activeRulesForbiddenFields = new();
            activeRuleSizes = new();
            startForbiddenFields = Copy(forbidden);
            Future2x2StartEnd = false;
            Future2x3StartEnd = false;
            Future3x3StartEnd = false;
            FutureL = false;
            TripleAreaExitDown = false;

            if (size == 5)
            {
                // C-Shape
                // Embedded in Path.cs as the absolute checking functions need it.
            }

            if (size == 7)
            {
                // Future 2 x 2 Start End
                for (int i = 0; i < 2; i++)
                {
                    if ((InTakenRel(4, 1) || InBorderRel(4, 1)) && InFutureStartRel(1, 0) && InFutureEndRel(3, 0) && InTakenRel(0, 3) && InTakenRel(-1, 2) && InTakenRel(-1, 1) && !InTakenRel(0, 2) && !InBorderRel(0, 2) && foundSectionStart == foundSectionEnd)
                    {
                        Future2x2StartEnd = true;
                        activeRules.Add("Future 2 x 2 Start End");
                        activeRulesForbiddenFields.Add(new List<int[]> { new int[] { x + lx, y + ly } });
                        activeRuleSizes.Add(new int[] { 6, 4 });
                        forbidden.Add(new int[] { x + lx, y + ly });
                    }
                    lx = -lx;
                    ly = -ly;
                }
                lx = thisLx;
                ly = thisLy;

                // Future 2 x 3 Start End
                for (int i = 0; i < 2; i++)
                {
                    if ((InTakenRel(1, -2) || InBorderRel(1, -2)) && !InTakenRel(1, -1) && !InBorderRel(1, -1) && InFutureStartRel(0, 1) && InFutureEndRel(2, 1) && foundSectionStart == foundSectionEnd)
                    {
                        Future2x3StartEnd = true;
                        activeRules.Add("Future 2 x 3 Start End");
                        activeRulesForbiddenFields.Add(new List<int[]> { new int[] { x + lx, y + ly } });
                        activeRuleSizes.Add(new int[] { 3, 4 });
                        forbidden.Add(new int[] { x + lx, y + ly });
                    }
                    lx = -lx;
                    ly = -ly;
                }
                lx = thisLx;
                ly = thisLy;

                // Future 3 x 3 Start End
                for (int i = 0; i < 2; i++)
                {
                    for (int j = 0; j < 2; j++)
                    {
                        if (!InTakenRel(3, 3) && !InBorderRel(3, 3) && !InTakenRel(3, 1) && !InBorderRel(3, 1) && (InTakenRel(3, 4) || InBorderRel(3, 4)) && (InTakenRel(2, 4) || InBorderRel(2, 4)) && (InTakenRel(1, 4) || InBorderRel(1, 4)) && (InTakenRel(4, 3) || InBorderRel(4, 3)) && (InTakenRel(4, 2) || InBorderRel(4, 2)) && (InTakenRel(4, 1) || InBorderRel(4, 1)) && InFutureStartRel(0, 1) && InFutureEndRel(0, 3) && !InCornerRel(3, 3) && foundSectionStart == foundSectionEnd)
                        {
                            Future3x3StartEnd = true;
                            activeRules.Add("Future 3 x 3 Start End");
                            activeRulesForbiddenFields.Add(new List<int[]> { new int[] { x + sx, y + sy } });
                            activeRuleSizes.Add(new int[] { 5, 5 });
                            forbidden.Add(new int[] { x + sx, y + sy });
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

                // Future L
                for (int i = 0; i < 2; i++)
                {
                    for (int j = 0; j < 2; j++)
                    {
                        if (InFutureStartRel(2, 0) && InFutureEndRel(2, 2) && foundSectionStart == foundSectionEnd)
                        {
                            FutureL = true;
                            activeRules.Add("Future L");
                            activeRulesForbiddenFields.Add(new List<int[]> { new int[] { x + sx, y + sy }, new int[] { x - lx, y - ly } });
                            activeRuleSizes.Add(new int[] { 4, 3 });
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

            if (size >= 9)
            {
                // Triple Area Exit Down
                for (int i = 0; i < 2; i++)
                {
                    for (int j = 0; j < 2; j++)
                    {
                        if ((InTakenRel(4, 0) || InBorderRel(4, 0)) && (InTakenRel(4, 3) || InBorderRel(4, 3)) && (InTakenRel(0, 3) || InBorderRel(0, 3)) && !InTakenRel(2, 0) && !InBorderRel(2, 0) && !InTakenRel(3, 0) && !InBorderRel(3, 0) && !InTakenRel(4, 1) && !InBorderRel(4, 1) && !InTakenRel(4, 2) && !InBorderRel(4, 2) && !InTakenRel(3, 3) && !InBorderRel(3, 3) && !InTakenRel(2, 3) && !InBorderRel(2, 3) && !InTakenRel(1, 3) && !InBorderRel(1, 3) && !InTakenRel(1, 2) && !InBorderRel(1, 2) && !InTakenRel(1, 1) && !InBorderRel(1, 1))
                        {
                            bool TripleAreaExitDown_circle1 = false;
                            directionFieldIndex = InTakenIndexRel(4, 0);
                            if (directionFieldIndex != -1)
                            {
                                if (InTakenRel(4, -1))
                                {
                                    int leftIndex = InTakenIndexRel(4, -1);
                                    if (leftIndex > directionFieldIndex)
                                    {
                                        TripleAreaExitDown_circle1 = true;
                                    }
                                }
                                else
                                {
                                    int rightIndex = InTakenIndexRel(4, 1);
                                    if (rightIndex < directionFieldIndex)
                                    {
                                        TripleAreaExitDown_circle1 = true;
                                    }
                                }
                            }
                            else
                            {
                                directionFieldIndex = InBorderIndexRel(4, 0);
                                int farSideIndex = InBorderIndexRel(4, 1);
                                if (farSideIndex > directionFieldIndex)
                                {
                                    TripleAreaExitDown_circle1 = true;
                                }
                            }

                            bool TripleAreaExitDown_circle2 = false;
                            directionFieldIndex = InTakenIndexRel(4, 3);
                            if (directionFieldIndex != -1)
                            {
                                if (InTakenRel(5, 3))
                                {
                                    int leftIndex = InTakenIndexRel(5, 3);
                                    if (leftIndex > directionFieldIndex)
                                    {
                                        TripleAreaExitDown_circle2 = true;
                                    }
                                }
                                else
                                {
                                    int rightIndex = InTakenIndexRel(3, 3);
                                    if (rightIndex < directionFieldIndex)
                                    {
                                        TripleAreaExitDown_circle2 = true;
                                    }
                                }
                            }
                            else
                            {
                                directionFieldIndex = InBorderIndexRel(4, 3);
                                int farSideIndex = InBorderIndexRel(3, 3);
                                if (farSideIndex > directionFieldIndex)
                                {
                                    TripleAreaExitDown_circle2 = true;
                                }
                            }

                            bool TripleAreaExitDown_circle3 = false;
                            directionFieldIndex = InTakenIndexRel(0, 3);
                            if (directionFieldIndex != -1)
                            {
                                if (InTakenRel(0, 4))
                                {
                                    int leftIndex = InTakenIndexRel(0, 4);
                                    if (leftIndex > directionFieldIndex)
                                    {
                                        TripleAreaExitDown_circle3 = true;
                                    }
                                }
                                else
                                {
                                    int rightIndex = InTakenIndexRel(0, 2);
                                    if (rightIndex < directionFieldIndex)
                                    {
                                        TripleAreaExitDown_circle3 = true;
                                    }
                                }
                            }
                            else
                            {
                                directionFieldIndex = InBorderIndexRel(0, 3);
                                int farSideIndex = InBorderIndexRel(0, 2);
                                if (farSideIndex > directionFieldIndex)
                                {
                                    TripleAreaExitDown_circle3 = true;
                                }
                            }

                            ResetExamAreas();
                            if (TripleAreaExitDown_circle1 && TripleAreaExitDown_circle2 && TripleAreaExitDown_circle3 && CountAreaRel(1, 0, 3, 0, new List<int[]> { new int[] { 2, 0 } }, i == 0 ? true : !true, 1) && CountAreaRel(3, 3, 1, 3, new List<int[]> { new int[] { 2, 3 } }, i == 0 ? true : !true, 1))
                            {
                                TripleAreaExitDown = true;
                                activeRules.Add("Triple Area Exit Down");
                                activeRulesForbiddenFields.Add(new List<int[]> { new int[] { x - lx, y - ly }, new int[] { x + sx, y + sy } });
                                activeRuleSizes.Add(new int[] { 6, 4 });
                                AddExamAreas();
                                forbidden.Add(new int[] { x - lx, y - ly });
                                forbidden.Add(new int[] { x + sx, y + sy });
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
            }

            if (size >= 13)
            { }
            T("Future2x2StartEnd: " + Future2x2StartEnd + "\n" + "Future2x3StartEnd: " + Future2x3StartEnd + "\n" + "Future3x3StartEnd: " + Future3x3StartEnd + "\n" + "FutureL: " + FutureL + "\n" + "TripleAreaExitDown: " + TripleAreaExitDown);
            window.ShowActiveRules(activeRules, activeRulesForbiddenFields, startForbiddenFields, activeRuleSizes);
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
                        /*if (j == 0)
                        {
                            if (i == 0)
                            {
                                CShapeLeft = true;
                            }
                            else
                            {
                                CShapeRight = true;
                            }
                        }*/
                        AddForbidden(0, 1);
                        AddForbidden(-1, 0);
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
                        AddForbidden(0, 1);

                        int middleIndex = InTakenIndexRel(0, 2);
                        int sideIndex = InTakenIndexRel(1, 2);
                        if (sideIndex > middleIndex) // area on left
                        {
                            closeStraightSmall = true;
                            AddForbidden(-1, 0);
                        }
                        else
                        {
                            closeStraightLarge = true;
                            AddForbidden(1, 0);
                        }
                    }
                }

                if (!closeStraight)
                {
                    if (InTakenRel(1, 2) && !InTakenRel(0, 1) && !InTakenRel(1, 1))
                    {
                        closeMidAcross = true;

                        AddForbidden(0, 1);

                        int middleIndex = InTakenIndexRel(1, 2);
                        int sideIndex = InTakenIndexRel(2, 2);
                        if (sideIndex > middleIndex)
                        {
                            closeMidAcrossSmall = true;
                            AddForbidden(-1, 0);
                        }
                        else
                        {
                            closeMidAcrossLarge = true;
                            AddForbidden(1, 0);
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
                            AddForbidden(0, 1);
                            AddForbidden(-1, 0);
                        }
                        else
                        {
                            closeAcrossLarge = true;
                            AddForbidden(1, 0);
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
                        T("Far straight");
                        farStraight = true;

                        int middleIndex = InTakenIndexRel(0, 3);
                        int sideIndex = InTakenIndexRel(1, 3);
                        if (sideIndex > middleIndex) // area on left
                        {
                            if (!InTakenRel(1, 2) && !InTakenRel(2, 2)) // 1,2: 2023_1019_3, 2,2: 2023_1019_4
                            {
                                T("Far straight small");
                                if (i == 0) farStraightLeft = true; else farStraightRight = true;

                                bool circleDirectionLeft = i == 0 ? true : false;
                                if (CountAreaRel(1, 1, 1, 2, null, circleDirectionLeft, 1))
                                {
                                    AddForbidden(0, 1);
                                    AddForbidden(-1, 0);
                                }
                                else if (InTakenRel(-2, 1) && InTakenRel(-1, 0) && !InTakenRel(-1, 1))
                                {
                                    AddForbidden(0, 1);
                                }
                            }
                        }
                        else // area on right
                        {
                            if (!InTakenRel(-1, 2) && !InTakenRel(-2, 2)) // -1, 2: 2023_1019_5, -2, 2: 2023_1019_6
                            {
                                T("Far straight large");
                                bool circleDirectionLeft = i == 0 ? false : true;
                                if (CountAreaRel(-1, 1, -1, 2, null, circleDirectionLeft, 1))
                                {
                                    AddForbidden(0, 1);
                                    AddForbidden(1, 0);
                                }
                                else if (InTakenRel(2, 1) && InTakenRel(1, 0) && !InTakenRel(1, 1))
                                {
                                    AddForbidden(0, 1);
                                }
                            }
                        }
                    }

                    if (!farStraight)
                    {
                        if (InTakenRel(1, 3) && InTakenRel(2, 3) && !InTakenRel(0, 2) && !InTakenRel(1, 2) && !InTakenRel(0, 1)) // 0, 2; 1, 2: 2023_1019_2
                        {
                            T("Far mid across");
                            farMidAcross = true;

                            int middleIndex = InTakenIndexRel(1, 3);
                            int sideIndex = InTakenIndexRel(2, 3);
                            if (sideIndex > middleIndex) // area on left
                            {
                                if (!InTakenRel(2, 2)) // 2, 2: 2023_1019
                                {
                                    T("Far mid across small");
                                    if (i == 0) farStraightLeft = true; else farStraightRight = true;
                                    bool circleDirectionLeft = i == 0 ? true : false;
                                    if (CountAreaRel(1, 1, 1, 2, null, circleDirectionLeft, 1))
                                    {
                                        AddForbidden(0, 1);
                                        AddForbidden(-1, 0);
                                    }
                                    else if (InTakenRel(-2, 1) && InTakenRel(-1, 0) && !InTakenRel(-1, 1))
                                    {
                                        AddForbidden(0, 1);
                                    }
                                }
                            }
                            else // area on right
                            {
                                if (!InTakenRel(-1, 2)) // -1, 2: 2023_1019_1
                                {
                                    T("Far mid across large");
                                    bool circleDirectionLeft = i == 0 ? false : true;
                                    if (CountAreaRel(0, 1, 0, 2, null, circleDirectionLeft, 1))
                                    {
                                        AddForbidden(0, 1);
                                        AddForbidden(1, 0);
                                    }
                                    else if (InTakenRel(2, 1) && InTakenRel(1, 0) && !InTakenRel(1, 1))
                                    {
                                        AddForbidden(0, 1);
                                    }
                                }
                            }
                        }

                        if (!farMidAcross)
                        {
                            if (InTakenRel(2, 3) && InTakenRel(3, 3) && !InTakenRel(0, 2) && !InTakenRel(1, 2) && !InTakenRel(2, 2) && !InTakenRel(0, 1))
                            {
                                T("Far across");
                                int middleIndex = InTakenIndexRel(2, 3);
                                int sideIndex = InTakenIndexRel(3, 3);
                                if (sideIndex > middleIndex) // area on left
                                {
                                    T("Far across small");
                                    if (i == 0) farStraightLeft = true; else farStraightRight = true;
                                    bool circleDirectionLeft = i == 0 ? true : false;
                                    if (CountAreaRel(1, 1, 1, 2, null, circleDirectionLeft, 1))
                                    {
                                        AddForbidden(0, 1);
                                        AddForbidden(-1, 0);
                                    }
                                    else
                                    {
                                        if (InTakenRel(-2, 1) && InTakenRel(-1, 0) && !InTakenRel(-1, 1))
                                        {
                                            AddForbidden(0, 1);
                                        }
                                        /*if (InTakenRel(1, 4) && !InTakenRel(1, 3)) // end C, there is a separate rule for that now
                                        {
                                            AddForbidden(1, 0);
                                        }*/
                                    }
                                }
                                else // area on right
                                {
                                    T("Far across large");
                                    if (!InTakenRel(-1, 2))
                                    {
                                        bool circleDirectionLeft = i == 0 ? false : true;
                                        if (CountAreaRel(0, 1, 1, 2, new List<int[]> { new int[] { 0, 2 } }, circleDirectionLeft, 0))
                                        {
                                            AddForbidden(0, 1);
                                            AddForbidden(1, 0);
                                        }
                                        else if (InTakenRel(2, 1) && InTakenRel(1, 0) && !InTakenRel(1, 1))
                                        {
                                            AddForbidden(0, 1);
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
                T("farStraightLeft and farStraightRight true");
                AddForbidden(0, 1);
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
                            AddForbidden(1, 0);
                        }
                    }

                    if (!closeSideStraight)
                    {
                        if (!InTakenRel(1, 0) && !InTakenRel(1, 1) && (InTakenRel(2, 1) || InTakenRel(2, -1) && !InTakenRel(1, -1)))
                        {
                            closeSideMidAcross = true;
                            AddForbidden(1, 0);
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
                                T("farSideStraight up");
                                farSideStraightUp = true;

                                int sideIndex = InTakenIndexRel(3, 1);
                                if (sideIndex > middleIndex) // area up
                                {
                                    farSideUp = true;

                                    if (CountAreaRel(1, 1, 2, 1, null, circleDirectionLeft, 1))
                                    {
                                        AddForbidden(1, 0);
                                    }
                                    else if ((InTakenRel(1, -2) || InBorderRel(1, -2)) && !InTakenRel(1, -1))
                                    {
                                        AddForbidden(1, 0);
                                    }
                                }
                            }
                            if (InTakenRel(3, -1)) // down side taken, we need to check sepearately from up side, in order to establish farSideStraightDown
                            {
                                T("farSideStraight down");
                                farSideStraightDown = true;

                                int sideIndex = InTakenIndexRel(3, -1);
                                if (sideIndex < middleIndex) // area up
                                {
                                    if (CountAreaRel(1, 1, 2, 1, null, circleDirectionLeft, 1))
                                    {
                                        AddForbidden(1, 0);
                                    }
                                    else if ((InTakenRel(1, -2) || InBorderRel(1, -2)) && !InTakenRel(1, -1))
                                    {
                                        AddForbidden(1, 0);
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
                                T("farSideMidAcross up");
                                farSideMidAcrossUp = true;

                                int middleIndex = InTakenIndexRel(3, 1);
                                int sideIndex = InTakenIndexRel(3, 2);
                                if (sideIndex > middleIndex) // area up
                                {
                                    farSideUp = true;

                                    if (CountAreaRel(1, 1, 2, 1, null, circleDirectionLeft, 1))
                                    {
                                        AddForbidden(1, 0);
                                    }
                                    else if ((InTakenRel(1, -2) || InBorderRel(1, -2)) && !InTakenRel(1, -1))
                                    {
                                        AddForbidden(1, 0);
                                    }
                                }
                            }

                            if (InTakenRel(3, -1) && InTakenRel(3, -2) && !InTakenRel(1, -1) && !InTakenRel(1, 0) && !InTakenRel(1, 1)) // mid across down, 1,1: 2023_1021_7
                            {
                                T("farSideMidAcross down");
                                farSideMidAcrossDown = true;

                                int middleIndex = InTakenIndexRel(3, -1);
                                int sideIndex = InTakenIndexRel(3, -2);
                                if (sideIndex < middleIndex) // area up
                                {
                                    if (CountAreaRel(1, 0, 2, 0, null, circleDirectionLeft, 1))
                                    {
                                        AddForbidden(1, 0);
                                    }
                                    else if (InTakenRel(1, -2))
                                    {
                                        AddForbidden(1, 0);
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
                            T("farSideAcross up");

                            int middleIndex = InTakenIndexRel(3, 2);
                            int sideIndex = InTakenIndexRel(3, 3);
                            if (sideIndex > middleIndex) // area up
                            {
                                farSideUp = true;

                                if (CountAreaRel(1, 1, 2, 1, null, circleDirectionLeft, 1))
                                {
                                    AddForbidden(1, 0);
                                }
                                else
                                {
                                    if ((InTakenRel(1, -2) || InBorderRel(1, -2)) && !InTakenRel(1, -1))
                                    {
                                        AddForbidden(1, 0);
                                    }
                                    /*if (InTakenRel(4, 1) && !InTakenRel(3, 1)) // end C
                                    {
                                        AddForbidden(0, 1);
                                    }*/
                                }
                            }
                        }

                        if (!farSideStraightDown && !farSideMidAcrossDown && InTakenRel(3, -2) && InTakenRel(3, -3) && !InTakenRel(1, -1) && !InTakenRel(1, 0) && !InTakenRel(1, 1) && !InTakenRel(2, -2)) // 2,-2: 9_630259
                        {
                            T("farSideAcross down");

                            int middleIndex = InTakenIndexRel(3, -2);
                            int sideIndex = InTakenIndexRel(3, -3);

                            if (sideIndex < middleIndex) // area up
                            {
                                if (CountAreaRel(1, 0, 2, -1, new List<int[]> { new int[] { 2, 0 } }, circleDirectionLeft, 0))
                                {
                                    AddForbidden(1, 0);
                                }
                                else if (InTakenRel(1, -2))
                                {
                                    AddForbidden(1, 0);
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
                        T("farSideUp and farSideDown true");
                        AddForbidden(1, 0);
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

        // May not be needed, countarea on the border takes care of it
        void CheckCOnFarBorder() // 2023_0831
        {
            // Applies from 7x7, see 2023_0831. Similar to CheckNearCorner, it is just not at the corner.
            if (x == size - 2 && rightField[0] == size - 1 && !InTakenRel(-1, -1) && InTakenRel(-1, -2))
            {
                T("COnFarBorder horizontal");
                forbidden.Add(rightField);
            }
            else if (y == size - 2 && leftField[1] == size - 1 && !InTakenRel(1, -1) && InTakenRel(1, -2))
            {
                T("COnFarBorder vertical");
                forbidden.Add(leftField);
            }
        }

        void CheckAreaNearBorder() // 2023_0909. Check both straight approach and side.
        {
            if (x == 3 && straightField[0] == 2 && !InTakenAbs(straightField) && !InTakenAbs(rightField) && !InTaken(1, y))
            {
                T("CheckArea left");
                if (CountArea(2, y - 1, 1, y - 1, null, false, 1))
                {
                    forbidden.Add(straightField);
                    forbidden.Add(leftField);
                }
            }
            else if (y == 3 && straightField[1] == 2 && !InTakenAbs(straightField) && !InTakenAbs(leftField) && !InTaken(x, 1))
            {
                T("CheckArea up");
                if (CountArea(x - 1, 2, x - 1, 1, null, true, 1))
                {
                    forbidden.Add(straightField);
                    forbidden.Add(rightField);
                }
            }
            else if (x == size - 2 && y >= 2 && straightField[0] == size - 1 && !InTakenAbs(straightField) && !InTakenAbs(leftField) && !InTaken(size, y))
            {
                T("CheckArea right");
                if (CountArea(size - 1, y - 1, size, y - 1, null, true, 1))
                {
                    forbidden.Add(straightField);
                    forbidden.Add(rightField);
                }
            }
            else if (y == size - 2 && x >= 2 && straightField[1] == size - 1 && !InTakenAbs(straightField) && !InTakenAbs(rightField) && !InTaken(x, size))
            {
                T("CheckArea down");
                if (CountArea(x - 1, size - 1, x - 1, size, null, false, 1))
                {
                    forbidden.Add(straightField);
                    forbidden.Add(leftField);
                }
            }
            else if (x == 3 && y >= 4 && leftField[0] == 2 && !InTaken(3, y - 1) && !InTaken(1, y) && !InTaken(2, y - 2)) //straight and left field cannot be taken, but it is enough we check the most left field on border. Also, 1 left and 2 up, 2 left and 2 up cannot be taken in order to draw an arealine. Checking 1 left and 2 up is enough.
            {
                T("CheckArea left side");
                if (CountArea(2, y - 1, 1, y - 1, null, false, 1))
                {
                    forbidden.Add(leftField);
                }
            }
            else if (y == 3 && x >= 4 && rightField[1] == 2 && !InTaken(x - 1, 3) && !InTaken(x, 1) && !InTaken(x - 2, 2))
            {
                T("CheckArea up side");
                if (CountArea(x - 1, 2, x - 1, 1, null, true, 1))
                {
                    forbidden.Add(rightField);
                }
            }
            else if (x == size - 2 && y >= 3 && rightField[0] == size - 1 && !InTaken(size - 2, y - 1) && !InTaken(size, y) && !InTaken(size - 1, y - 2))
            {
                T("CheckArea right side");
                if (CountArea(size - 1, y - 1, size, y - 1, null, true, 1))
                {
                    forbidden.Add(rightField);
                }
            }
            else if (y == size - 2 && x >= 3 && leftField[1] == size - 1 && !InTaken(x - 1, size - 2) && !InTaken(x, size) && !InTaken(x - 2, size - 1))
            {
                T("CheckArea down side");
                if (CountArea(x - 1, size - 1, x - 1, size, null, false, 1))
                {
                    forbidden.Add(leftField);
                }
            }
        }

        void CheckDirectionalArea()
        {
            for (int i = 0; i < 2; i++)
            {
                for (int j = 0; j < 2; j++)
                {
                    if (!(InTakenRel(1, 0) || InBorderRel(1, 0) || InTakenRel(2, 0) || InBorderRel(2, 0)))
                    {
                        bool circleDirectionLeft = (i == 0) ? true : false;
                        int hori = 0;
                        int vert = 1;

                        while (!InTakenRel(hori, vert) && !InBorderRel(hori, vert))
                        {
                            while (hori <= vert + 3 && !InTakenRel(hori, vert) && !InBorderRel(hori, vert))
                            {
                                hori++;
                            }
                            // After stepping to side, we need to step down if the area contains more than the border line
                            // Check if the top row is empty, so we can exit the area.
                            if (hori == vert + 3 && !InTakenRel(hori - 1, vert + 1) && !InBorderRel(hori - 1, vert + 1) && !InTakenRel(hori - 2, vert + 1) && !InTakenRel(hori - 3, vert + 1))
                            {
                                bool circleValid = false;

                                if (InBorderRel(hori, vert))
                                {
                                    int i1 = InBorderIndexRel(hori, vert);
                                    int i2 = InBorderIndexRel(hori, vert + 1);

                                    if (i2 > i1)
                                    {
                                        circleValid = true;
                                    }
                                }
                                else
                                {
                                    int i1 = InTakenIndexRel(hori, vert);
                                    int i2 = InTakenIndexRel(hori, vert + 1);

                                    if (i2 != -1)
                                    {
                                        if (i2 < i1)
                                        {
                                            circleValid = true;
                                        }
                                    }
                                    else
                                    {
                                        i2 = InTakenIndexRel(hori, vert - 1);
                                        if (i2 > i1)
                                        {
                                            circleValid = true;
                                        }
                                    }
                                }

                                if (circleValid)
                                {
                                    bool takenFound = false;
                                    List<int[]> borderFields = new();

                                    for (int k = hori - 2; k >= 2; k--)
                                    {
                                        borderFields.Add(new int[] { k, k - 1 });
                                        borderFields.Add(new int[] { k, k - 2 });
                                    }

                                    // bottom and top row already checked for emptiness
                                    for (int k = hori - 2; k >= 3; k--)
                                    {
                                        if (InTakenRel(k, k - 2) || InTakenRel(k - 1, k - 2))
                                        {
                                            takenFound = true;
                                            break;
                                        }
                                    }

                                    if (!takenFound)
                                    {
                                        if (CountAreaRel(1, 0, hori - 1, hori - 3, borderFields, circleDirectionLeft, 3, true))
                                        {
                                            int black = (int)info[1];
                                            int white = (int)info[2];

                                            if (black == white)
                                            {
                                                int thisX = x;
                                                int thisY = y;
                                                int thisLx = lx;
                                                int thisLy = ly;

                                                // opposite side of the relation of the area to the live end
                                                lx = -lx;
                                                ly = -ly;

                                                // check all the corners if there is a close obstacle that would disable the horizontal far direction
                                                for (int k = 2; k <= hori - 2; k++)
                                                {
                                                    x = thisX + thisLx * k + sx * (k - 1);
                                                    y = thisY + thisLy * k + sy * (k - 1);

                                                    if (CheckNearFieldSmall1())
                                                    {
                                                        T("CheckDirectionalArea at " + thisX + " " + thisY + " obstacle encountered at " + x + " " + y);

                                                        DirectionalArea = true;
                                                        activeRules.Add("Directional Area");
                                                        activeRuleSizes.Add(new int[] { 7, 7 });
                                                        activeRulesForbiddenFields.Add(new List<int[]> { new int[] { thisX + thisLx, thisY + thisLy } });

                                                        forbidden.Add(new int[] { thisX + thisLx, thisY + thisLy });
                                                        // field behind, only relevant when rotated up
                                                        if (j == 1)
                                                        {
                                                            forbidden.Add(new int[] { thisX - sx, thisY - sy });
                                                        }
                                                    }
                                                }

                                                x = thisX;
                                                y = thisY;
                                                lx = thisLx;
                                                ly = thisLy;
                                            }
                                        }
                                    }
                                    else
                                    {
                                        T("arealine taken");
                                    }
                                }
                            }

                            vert++;
                            hori = vert - 1;
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
        }

        bool CheckDownStair(int side = -1, int nLx = 0, int nLy = 0, int nSx = 0, int nSy = 0) // 9_51015231, 9_53144883
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

                                    T("CheckDownStair far, cannot step left", i, j);

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
                            T("Check3DoubleAreaRotated true", i, j);

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
                            T("CheckDownStair true");

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

        bool Check3DoubleAreaRotated(int side = -1) // Take only the first case and rotate it. Next step checking will need it, otherwise it is built into AreaUp.
        {
            for (int i = 0; i < 2; i++)
            {
                if (side != -1 && side != i) continue;

                bool circleValid = false;
                bool circleDirectionLeft = (i == 0) ? true : false;
                int startX = 0, startY = 0, endX = 0, endY = 0;

                List<int[]> borderFields = new();

                if (InTakenRel(4, -1) && !InTakenRel(2, 0) && !InTakenRel(3, 0) && !InTakenRel(4, 0) && !InTakenRel(1, -1) && !InTakenRel(3, -1))
                {
                    int i1 = InTakenIndexRel(4, -1);
                    int i2 = InTakenIndexRel(4, -2);

                    if (i2 > i1)
                    {
                        circleValid = true;

                        startX = 1;
                        startY = 0;
                        endX = 3;
                        endY = 0;
                        borderFields.Add(new int[] { 2, 0 });
                    }
                }

                if (circleValid && CountAreaRel(startX, startY, endX, endY, borderFields, circleDirectionLeft, 2, true))
                {
                    int black = (int)info[1];
                    int white = (int)info[2];

                    if (black == white)
                    {
                        int thisX = x;
                        int thisY = y;

                        x = x + endX * lx + endY * thisSx;
                        y = y + endX * ly + endY * thisSy;

                        // Checking C-Shape not necessary, side straight will take care of it, because area is 1B.
                        if (CheckNearFieldSmall1())
                        {
                            if (side != -1)
                            {
                                return true; // We are only interested in the side the straight obstacle is going to. Both sides cannot be true at the same time.
                            }

                            T("Check3DoubleAreaRotated at " + thisX + " " + thisY);

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

        bool CheckNearFieldSmall1() // for use only with Double Area case 1, 2, 3 and 1 rotated, and Down Stair. Across is needed at 9_53144883
                                    // Sequence case 1 right side
        {
            // close mid across. In DirectionalArea, the empty fields are already checked.
            if (InTakenRel2(1, 2) && !InTakenRel2(0, 2) && !InTakenRel2(1, 1))
            {
                int middleIndex = InTakenIndexRel2(1, 2);
                int sideIndex = InTakenIndexRel2(2, 2);

                if (sideIndex > middleIndex)
                {

                    return true;
                }
            }

            // close across
            if (InTakenRel2(2, 2) && !InTakenRel2(1, 2) && !InTakenRel2(2, 1))
            {
                int middleIndex = InTakenIndexRel2(2, 2);
                int sideIndex = InTakenIndexRel2(3, 2);

                if (sideIndex > middleIndex)
                {
                    return true;
                }
            }

            return false;
        }

        bool CheckNearFieldSmall1_5() // for use only with Double Area case 1, 2, 3 and 1 rotated
                                      // Sequence case 1 left side
        {
            // C-shape (left)
            if ((InTakenRel2(2, 0) || InBorderRel2(2, 0)) && !InTakenRel2(1, 0))
            {
                return true;
            }

            // close mid across. In DirectionalArea, the empty fields are already checked.
            if (InTakenRel2(1, 2) && !InTakenRel2(0, 2) && !InTakenRel2(1, 1))
            {
                int middleIndex = InTakenIndexRel2(1, 2);
                int sideIndex = InTakenIndexRel2(2, 2);

                if (sideIndex > middleIndex)
                {

                    return true;
                }
            }

            // close across
            if (InTakenRel2(2, 2) && !InTakenRel2(1, 2) && !InTakenRel2(2, 1))
            {
                int middleIndex = InTakenIndexRel2(2, 2);
                int sideIndex = InTakenIndexRel2(3, 2);

                if (sideIndex > middleIndex)
                {
                    return true;
                }
            }

            return false;
        }

        void StairArea()
        // Solved by Sequence2
        // 2024_0630: Stair on one side, and one of the steps creates an area where we can only enter now.
        // 2024_0720: Double close obstacle at the exit point
        // Also solved by sequence case 4, but this is redundant.
        {
            for (int i = 0; i < 2; i++)
            {
                bool circleDirectionLeft = (i == 0) ? true : false;

                for (int j = 0; j < 2; j++) // j = 0: straight area, j = 1: left (small) area
                {
                    int dist = 1;
                    while (InTakenRel(dist, dist - 1) && InTakenRel(dist + 1, dist) && !InTakenRel(dist, dist))
                    {
                        dist++;
                    }
                    dist--;

                    int k;
                    for (k = 1; k < dist; k++)
                    {
                        path.Add(new int[] { x + (k - 1) * lx + k * sx, y + (k - 1) * ly + k * sy });

                        ResetExamAreas();

                        if (CheckCorner1(k, k + 1, 1, 0, circleDirectionLeft, true))
                        {
                            for (int l = 1; l <= k; l++)
                            {
                                path.RemoveAt(path.Count - 1);
                            }

                            AddExamAreas(true);
                            T("StairArea " + dist + " dist: Cannot step straight");
                            AddForbidden(0, 1);

                            sx = thisSx;
                            sy = thisSy;
                            lx = thisLx;
                            ly = thisLy;
                            return;
                        }
                    }

                    for (k = 1; k < dist; k++)
                    {
                        path.RemoveAt(path.Count - 1);
                    }

                    // double area at the exit point of the stair, 2024_0720
                    if (dist >= 1)
                    {
                        if (CheckNearFieldSmallRel1(k, k + 1, 0, 0, true) && CheckNearFieldSmallRel0(k, k + 1, 1, 0, true))
                        {
                            T("StairArea end " + dist + " dist: Cannot step straight");
                            AddForbidden(0, 1);
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

        void StairAtStart()

        // StairAtStart 3 1 / 3 2 / 4 1 / 4 2

        // 3 distance on top:
        // 2024_0725_6: mid across down, mid across up
        // 2024_0726_1: across, mid across
        // 2024_0726_2: mid across, area

        // 4 distance on top:
        // 2024_0626_1: mid across down, mid across up, no stair
        // 2024_0729_3: across, mid across
        // 2024_0730: across down, mid across up
        {
            for (int i = 0; i < 2; i++)
            {
                bool circleDirectionLeft = (i == 0) ? true : false;

                for (int j = 0; j < 4; j++)
                {
                    if (j != 1 && j != 2) // rotate to small area
                    {
                        int topDist = 4;

                        int dist = size; // vertical distance
                        int quarter = quarters[i][j];
                        foreach (int[] corner in closedCorners[quarter])
                        {
                            if (j == 0 && corner[1] == corner[0] + 4)
                            {
                                if (corner[1] < dist) dist = corner[1];
                            }
                            else if (j == 3 && corner[0] == corner[1] + 4)
                            {
                                if (corner[0] < dist) dist = corner[0];
                            }
                        }

                        // if a corner was found for a 3-distance on top stair in a rotation, a 4-distance top cannot co-exist.
                        if (dist == size)
                        {
                            int nextQuarter;
                            List<int[]>[] corners;

                            if (i == 0)
                            {
                                nextQuarter = quarter == 3 ? 0 : quarter + 1;
                                corners = openCWCorners;
                            }
                            else
                            {
                                nextQuarter = quarter == 0 ? 3 : quarter - 1;
                                corners = openCCWCorners;
                            }

                            foreach (int[] corner in corners[nextQuarter])
                            {
                                if (j == 0 && corner[0] == 0 && corner[1] == 5)
                                {
                                    if (corner[1] < dist) dist = corner[1];
                                }
                                else if (j == 3 && corner[0] == 5 && corner[1] == 0)
                                {
                                    if (corner[0] < dist) dist = corner[0];
                                }
                            }
                        }
                        else
                        {
                            topDist = 3;
                        }

                        if (dist < size)
                        {
                            T("StairAtStart distance " + (dist - 1), "side " + i, "rotation " + j, "topDist " + topDist);

                            bool distanceEmpty = true;
                            for (int k = 1; k <= dist - 1; k++)
                            {
                                if (k <= topDist)
                                {
                                    if (InTakenRel(-1, k)) distanceEmpty = false;
                                }
                                else
                                {
                                    if (InTakenRel(k - (topDist + 1), k)) distanceEmpty = false;
                                }
                            }

                            if (distanceEmpty)
                            {
                                int i1, i2;
                                i1 = InTakenIndexRel(dist - (topDist + 1), dist);
                                i2 = InTakenIndexRel(dist - topDist, dist);

                                if (i2 > i1)
                                {
                                    List<int[]> borderFields = new();
                                    int ex = dist - 1;

                                    for (int k = ex - 1; k >= 2; k--)
                                    {
                                        if (k >= topDist)
                                        {
                                            borderFields.Add(new int[] { k - topDist, k });
                                            borderFields.Add(new int[] { k - (topDist + 1), k });
                                        }
                                        else
                                        {
                                            borderFields.Add(new int[] { -1, k });
                                        }
                                    }

                                    ResetExamAreas();

                                    if (CountAreaRel(-1, 1, ex - (topDist + 1), ex, borderFields, circleDirectionLeft, 2, true))
                                    {
                                        int black = (int)info[1];
                                        int white = (int)info[2];

                                        // 2024_0725_6: mid across down, mid across up
                                        // 2024_0726_1: across, mid across
                                        // 2024_0726_2: mid across, area

                                        if (topDist == 3 && black == white + ex - 2)
                                        {
                                            // Add future fields in order to be able to draw the second area
                                            for (int k = ex; k >= 3; k--)
                                            {
                                                path.Add(new int[] { x + (k - 3) * lx + k * sx, y + (k - 3) * ly + k * sy });
                                            }

                                            if (CheckNearFieldSmallRel1(-1, 2, 1, 1, true) && CheckCorner1(-1, 2, 0, 2, circleDirectionLeft, true))
                                            {
                                                for (int k = ex; k >= 3; k--)
                                                {
                                                    path.RemoveAt(path.Count - 1);
                                                }

                                                AddExamAreas();

                                                T("StairAtStart 3: cannot step straight");
                                                AddForbidden(0, 1);

                                                if (j == 0)
                                                {
                                                    T("StairAtStart 3: cannot step left");
                                                    AddForbidden(1, 0);
                                                }
                                            }
                                            else
                                            {
                                                for (int k = ex; k >= 3; k--)
                                                {
                                                    path.RemoveAt(path.Count - 1);
                                                }
                                            }
                                        }
                                        // 2024_0626_1: mid across down, mid across up, no stair
                                        // 2024_0729_3: across, mid across
                                        // 2024_0730: across down, mid across up
                                        else if (topDist == 4 && white == black + ex - 3)
                                        {
                                            if (CheckNearFieldSmallRel1(-1, 1, 1, 1, false) && CheckNearFieldSmallRel1(-1, 3, 0, 2, true))
                                            {
                                                AddExamAreas();

                                                T("StairAtStart 4: cannot step right");
                                                AddForbidden(-1, 0);

                                                if (j == 3)
                                                {
                                                    T("StairAtStart 4: cannot step down");
                                                    AddForbidden(0, -1);
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

        void CheckLeftRightAreaUpExtended() // End obstacle
        {
            for (int i = 0; i < 2; i++)
            {
                bool circleDirectionLeft = (i == 0) ? true : false;

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

                        // no close mid across checking here, distance needs to be at least 2
                        if (dist >= 3 && dist < size)
                        {
                            T("AreaUpExtended distance " + (dist - 1), "side " + i, "rotation " + j);

                            bool distanceEmpty = true;
                            for (int k = 1; k <= dist - 1; k++)
                            {
                                if (k == 1) // As 2024_0618_2 shows, 1,1 can be taken
                                {
                                    if (InTakenRel(0, k)) distanceEmpty = false;
                                }
                                else
                                {
                                    if (InTakenRel(0, k) || InTakenRel(1, k)) distanceEmpty = false;
                                }
                            }

                            if (distanceEmpty)
                            {
                                int i1 = InTakenIndexRel(1, dist);
                                int i2 = InTakenIndexRel(2, dist);

                                if (i2 > i1)
                                {
                                    List<int[]> borderFields = new();
                                    int ex = dist - 1;

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

                                        bool ruleTrue = false;

                                        switch (ex % 4)
                                        {
                                            case 0:
                                                // 2024_0610_4, 2024_0610_5: across
                                                // 9_121670752: mid across
                                                // 2024_0627: area
                                                if (-whiteDiff == ex / 4 && (j == 0 || j == 3))
                                                {
                                                    // Add field so that a second circle can be drawn
                                                    path.Add(new int[] { x + lx + ex * sx, y + ly + ex * sy });

                                                    if (CheckCorner1(0, ex - 1, 0, 2, circleDirectionLeft, true))
                                                    {
                                                        path.RemoveAt(path.Count - 1);

                                                        ruleTrue = true;
                                                        T("LeftRightAreaUpExtended open corner 4: Cannot step straight");
                                                        AddForbidden(0, 1);
                                                        // stepping left is already disabled
                                                    }
                                                    else
                                                    {
                                                        path.RemoveAt(path.Count - 1);
                                                    }
                                                }
                                                // 2024_0618_2: end obstacle (across)
                                                // 2024_0725: double obstacle outside 
                                                else if (whiteDiff == ex / 4)
                                                {
                                                    if (CheckNearFieldSmallRel1(0, ex, 0, 2, true))
                                                    {
                                                        ruleTrue = true;
                                                        T("LeftRightAreaUpExtended closed corner 4: Cannot step right");
                                                        AddForbidden(-1, 0);
                                                        if (j == 3)
                                                        {
                                                            T("LeftRightAreaUpExtended closed corner 4: Cannot step down");
                                                            AddForbidden(0, -1);
                                                        }

                                                        // 2024_0725: double obstacle outside, 2 x mid across
                                                        // 2024_0727_5: up mid across, down across
                                                        if (CheckNearFieldSmallRel1(0, 2, 1, 1, false))
                                                        {
                                                            T("LeftRightAreaUpExtended 4 dist double obstacle outside: Cannot step straight");
                                                            AddForbidden(0, 1);
                                                        }
                                                    }
                                                }
                                                break;
                                            case 1:
                                                // 2024_0626, across
                                                if (whiteDiff == (ex + 3) / 4 && CheckNearFieldSmallRel1(0, ex - 1, 0, 2, true))
                                                {
                                                    ruleTrue = true;
                                                    T("LeftRightAreaUpExtended open corner 5: Cannot step right");
                                                    AddForbidden(-1, 0);

                                                    if (j == 3)
                                                    {
                                                        T("LeftRightAreaUpExtended open corner 5: Cannot step down");
                                                        AddForbidden(0, -1);
                                                    }

                                                    // 2024_0727_4: double obstacle outside: mid across x 2 
                                                    if (CheckNearFieldSmallRel0(0, 2, 1, 1, false))
                                                    {
                                                        T("LeftRightAreaUpExtended 5 dist double obstacle outside: Cannot step straight");
                                                        AddForbidden(0, 1);
                                                    }
                                                }
                                                break;
                                            case 2:
                                                // We cannot get to the 2- or 6-distance case if the other rules are applied. 2024_0611_1
                                                break;
                                            case 3:
                                                // Can we get here?
                                                /*if (whiteDiff == (ex + 1) / 4 + 1 && CheckNearFieldSmallRel(0, ex - 1, 0, 2, true))
                                                {
                                                    ruleTrue = true;
                                                    T("LeftRightAreaUpExtended open corner 3: Cannot step left");
                                                    AddForbidden(1, 0);
                                                }*/
                                                // 2024_0611, 2024_0710, 2024_0710_1
                                                if (-whiteDiff == (ex + 1) / 4 - 1 && (j == 0 || j == 3))
                                                {
                                                    if (CheckCorner1(0, ex, 0, 2, circleDirectionLeft, true))
                                                    {
                                                        ruleTrue = true;
                                                        T("LeftRightAreaUpExtended closed corner 3: Cannot step straight");
                                                        AddForbidden(0, 1);
                                                        // stepping left is already disabled
                                                    }
                                                }

                                                // Sequence sixth case
                                                // Sequence can only exist at a short distance (max 3) where the line cannot exit and enter again.
                                                // 2024_0724: up across, down mid across
                                                // 2024_0725_2: up area, down mid across
                                                // 2024_0727_3: up mid across, down across
                                                // 2024_0727_6: sequence up

                                                /*if (ex == 3 && (j == 0 || j == 3) && white == black)
                                                {
                                                    path.Add(new int[] { x + sx, y + sy });
                                                    path.Add(new int[] { x + 3 * sx, y + 3 * sy });
                                                    path.Add(new int[] { x - lx + 2 * sx, y - ly + 2 * sy });
                                                    x2 = x - lx + 2 * sx;
                                                    y2 = y - ly + 2 * sy;

                                                    int[] rotatedDir = RotateDir(lx, ly, i);
                                                    lx2 = rotatedDir[0];
                                                    ly2 = rotatedDir[1];
                                                    rotatedDir = RotateDir(sx, sy, i);
                                                    sx2 = rotatedDir[0];
                                                    sy2 = rotatedDir[1];

                                                    counterrec = 0;
                                                    if (CheckSequenceRecursive(i))
                                                    {
                                                        path.RemoveAt(path.Count - 1);
                                                        path.RemoveAt(path.Count - 1);
                                                        path.RemoveAt(path.Count - 1);

                                                        AddExamAreas();

                                                        T("CheckSequence case 6 at " + x + " " + y + ", stop at " + x2 + " " + y2 + ": Cannot step straight");
                                                        AddForbidden(0, 1);
                                                    }
                                                    else
                                                    {
                                                        path.RemoveAt(path.Count - 1);
                                                        path.RemoveAt(path.Count - 1);
                                                        path.RemoveAt(path.Count - 1);
                                                    }
                                                }
                                                */
                                                break;
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

        void StairAtStartEqual()
        {
            // B = W area, corner obstacle at return stair
            // 2024_1012_1
            // Find case where first obstacle is a corner relative to the area

            for (int i = 0; i < 2; i++)
            {
                bool circleDirectionLeft = (i == 0) ? true : false;

                for (int j = 0; j < 4; j++)
                {
                    if (j != 1 && j != 2) // rotate to small area
                    {
                        int dist = size; // vertical distance
                        int quarter = quarters[i][j];
                        foreach (int[] corner in closedCorners[quarter])
                        {
                            if (j == 0 && corner[1] == corner[0] + 4)
                            {
                                if (corner[1] < dist) dist = corner[1];
                            }
                            else if (j == 3 && corner[0] == corner[1] + 4)
                            {
                                if (corner[0] < dist) dist = corner[0];
                            }
                        }

                        if (dist < size)
                        {
                            T("StairAtStartEqual distance " + (dist - 1), "side " + i, "rotation " + j);

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
                                int i1, i2;
                                i1 = InTakenIndexRel(dist - 4, dist);
                                i2 = InTakenIndexRel(dist - 3, dist);

                                if (i2 > i1)
                                {
                                    List<int[]> borderFields = new();
                                    int ex = dist - 1;

                                    for (int k = ex - 1; k >= 2; k--)
                                    {
                                        if (k >= 3)
                                        {
                                            borderFields.Add(new int[] { k - 2, k });
                                            borderFields.Add(new int[] { k - 3, k });
                                        }
                                        else
                                        {
                                            borderFields.Add(new int[] { 0, k });
                                        }
                                    }

                                    ResetExamAreas();

                                    if (CountAreaRel(0, 1, ex - 3, ex, borderFields, circleDirectionLeft, 2, true))
                                    {
                                        int black = (int)info[1];
                                        int white = (int)info[2];

                                        if (black == white)
                                        {
                                            int counter = 0;

                                            for (int k = ex - 1; k >= 3; k--)
                                            {
                                                path.Add(new int[] { x + (k - 2) * lx + (k + 1) * sx, y + (k - 2) * ly + (k + 1) * sy });
                                                counter++;

                                                if (CheckCorner1(k - 3, k, 0, 2, circleDirectionLeft, true))
                                                {
                                                    for (k = 0; k < counter; k++)
                                                    {
                                                        path.RemoveAt(path.Count - 1);
                                                    }
                                                    counter = 0;

                                                    AddExamAreas();

                                                    T("StairAtStartEqual: Cannot step straight");
                                                    AddForbidden(0, 1);

                                                    break;
                                                }
                                            }

                                            for (int k = 0; k < counter; k++)
                                            {
                                                path.RemoveAt(path.Count - 1);
                                            }
                                            counter = 0;
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

        void StairAtEndConvex()

        // Enter later, 0B area:
        // StairAtEndConvex 3 1 / 3 2 later
        // 2024_0718: across down, mid across up
        // 9_18677343, 9_59434452: mid across x 2, no stair
        // 2024_0720_2: mid across x 2
        // 2024_0709: mid across down, C-shape up, no stair
        // 2024_0727: mid across down, C-shape up
        // 2024_0731: 3 obstacles

        // Enter now, 1B -> xB area:
        // StairAtEndConvex 3 1 / 3 2 now nostair
        // 2024_0516_2: across up, mid across down
        // 2024_1012: mid across up, across down

        // Corner for convex and concave area can both exist at the same time (2024_0831), so they need two separate functions
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
                    // No condition to have at least two steps: Will work as StraightSmalll
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
                        T("StairAtEndConvex distance " + (dist - 1), "side " + i, "rotation " + j);

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

                            if (i2 < i1)
                            {
                                List<int[]> borderFields = new();
                                for (int k = 1; k <= vert + 1; k++)
                                {
                                    if (k == 1 && k < vert + 1)
                                    {
                                        borderFields.Add(new int[] { 2, 1 });
                                    }
                                    else if (k < vert + 1)
                                    {
                                        borderFields.Add(new int[] { k, k });
                                        borderFields.Add(new int[] { k + 1, k });
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

                                        // 2024_0718: across down, mid across up
                                        // 9_18677343, 9_59434452: mid across x 2, no stair
                                        // 2024_0720_2: mid across x 2
                                        // 2024_0709: mid across down, C-shape up, no stair
                                        // 2024_0727: mid across down, C-shape up
                                        // 2024_0731: 3 obstacles
                                        T(CheckNearFieldSmallRel(hori - 1, vert + 1, 0, 0, true)); // ma, a, c
                                        T(CheckNearFieldSmallRel1(hori - 3, vert + 1, 1, 0, false)); // ma, a
                                        T(CheckNearFieldSmallRel(hori - 4, vert + 2, 1, 0, true));
                                        T(CheckNearFieldSmallRel0(hori - 4, vert + 2, 0, 0, true));
                                        // if (black == white && true && (false || (true && true)))
                                        if (black == white && CheckNearFieldSmallRel(hori - 1, vert + 1, 0, 0, true) && (CheckNearFieldSmallRel1(hori - 3, vert + 1, 1, 0, false) || (CheckNearFieldSmallRel(hori - 4, vert + 2, 1, 0, true) && CheckNearFieldSmallRel0(hori - 4, vert + 2, 0, 0, true))))
                                        {
                                            AddExamAreas();
                                            T("StairAtEndConvex 0B at " + hori + " " + vert + ": Cannot step straight");
                                            AddForbidden(0, 1);

                                            if (j == 0)
                                            {
                                                T("StairAtEndConvex 0B at " + hori + " " + vert + ": Cannot step right");
                                                AddForbidden(-1, 0);
                                            }
                                        }
                                        // 2024_0516_2: across up, mid across down
                                        // 2024_1012: mid across up, across down
                                        else if (black == white + vert + 1)
                                        {
                                            if (CheckNearFieldSmallRel1(hori - 2, vert + 1, 0, 0, true) && CheckNearFieldSmallRel1(hori - 2, vert + 1, 1, 0, true))
                                            {
                                                AddExamAreas();
                                                T("StairAtEndConvex 1B at " + hori + " " + vert + ": Cannot step left");
                                                AddForbidden(1, 0);

                                                if (j == 1)
                                                {
                                                    T("StairAtEndConvex 1B at " + hori + " " + vert + ": Cannot step down");
                                                    AddForbidden(0, -1);
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

        void StairAtEnd3Obtacles2()
        // 2024_0805: start 2 dist, 2024_0808: start 3 dist, 2024_0811_3: nextX = 4
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
                        T("StairAtEnd3Obtacles2 distance " + (dist - 1), "side " + i, "rotation " + j);

                        bool distanceEmpty = true;
                        for (int k = 1; k <= dist - 1; k++)
                        {
                            if (k < dist - 1)
                            {
                                if (InTakenRel(k, k)) distanceEmpty = false;
                            }
                            else
                            {
                                if (InTakenRel(k, dist - 1)) distanceEmpty = false;
                            }
                        }

                        if (distanceEmpty)
                        {
                            int hori = dist;
                            int vert = dist - 3;

                            int i1 = InTakenIndexRel(hori, vert);
                            int i2 = InTakenIndexRel(hori + 1, vert);

                            if (i2 < i1)
                            {
                                List<int[]> borderFields = new();
                                for (int k = 1; k <= vert + 1; k++)
                                {
                                    if (k == 1)
                                    {
                                        borderFields.Add(new int[] { 2, 1 });
                                    }
                                    else if (k < vert + 1)
                                    {
                                        borderFields.Add(new int[] { k, k });
                                        borderFields.Add(new int[] { k + 1, k });
                                    }
                                    else
                                    {
                                        borderFields.Add(new int[] { k, k });
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

                                        if (black == white + vert && CheckNearFieldSmallRel0(hori - 1, vert + 1, 0, 0, true) && CheckNearFieldSmallRel(0, 1, 1, 0, true) && CheckNearFieldSmallRel(hori - 4, vert + 2, 1, 0, true) && CheckNearFieldSmallRel0(hori - 4, vert + 2, 0, 0, true))
                                        {
                                            AddExamAreas();
                                            T("StairAtEnd3Obtacles2 at " + hori + " " + vert + ": Cannot step straight");
                                            AddForbidden(0, 1);
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

        void CheckAreaUpStartObstacleInside() // 2024_0618, 2024_0619: When we enter the area, we need to step straight. There is a close obstacle the other way inside the area.
        {
            for (int i = 0; i < 2; i++)
            {
                bool circleDirectionLeft = (i == 0) ? true : false;

                for (int j = 0; j < 3; j++) // small area, big area, behind right
                {
                    bool circleValid = false;
                    int dist = 1;
                    List<int[]> borderFields = new();

                    while (!InTakenRel(1, dist) && !InBorderRel(1, dist))
                    {
                        dist++;
                    }

                    int ex = dist - 1;

                    if (ex >= 3 && ex % 2 == 1)
                    {
                        if (InBorderRel(1, dist))
                        {
                            int i1 = InBorderIndexRel(1, dist);
                            int i2 = InBorderIndexRel(2, dist);

                            if (i1 > i2)
                            {
                                circleValid = true;
                            }
                        }
                        else
                        {
                            int i1 = InTakenIndexRel(1, dist);
                            int i2 = InTakenIndexRel(2, dist);

                            if (i2 > i1)
                            {
                                circleValid = true;
                            }
                        }
                    }

                    if (circleValid)
                    {
                        for (int k = ex - 1; k >= 2; k--)
                        {
                            borderFields.Add(new int[] { 1, k });
                        }

                        ResetExamAreas();

                        if (CountAreaRel(1, 1, 1, ex, borderFields, circleDirectionLeft, 2, true))
                        {
                            int black = (int)info[1];
                            int white = (int)info[2];

                            int whiteDiff = white - black;

                            bool ruleTrue = false;

                            switch (ex % 4)
                            {
                                case 1:
                                    if (j <= 1 && whiteDiff == (ex - 1) / 4 && CheckNearFieldSmallRel1(1, 2, 0, 1, true)) // Mid across: 2024_0618, Across: 2024_0717_1
                                    {
                                        ruleTrue = true;
                                        T("AreaUpStartObstacleInside % 4 = 1: Cannot step straight and right");
                                        AddForbidden(0, 1);
                                        if (j != 2) // the right field relative to the area (left of the main line) is now inside the area.
                                        {
                                            AddForbidden(-1, 0);
                                        }
                                    }
                                    break;
                                case 3:
                                    if (j >= 1 && whiteDiff == (ex + 1) / 4 && CheckNearFieldSmallRel1(1, 0, 0, 1, true)) // Mid across: 2024_0619, Across: 2024_0717_2, area up 3 start obstacle
                                    {
                                        ruleTrue = true;
                                        T("AreaUpStartObstacleInside % 4 = 3: Cannot step left");
                                        AddForbidden(1, 0);
                                    }
                                    break;
                            }

                            if (ruleTrue)
                            {
                                AddExamAreas();
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

        void CheckStartObstacleInside()

        // When we enter the area, we need to step straight. There is a close obstacle the other way inside the area.
        // 2024_0619, 2024_0818: straight
        // 2024_0618, 2024_0717_1, 2024_0717_2: area up
        // 2024_0811, 2024_0817: corner
        // Example needed for corner (y - x) % 4 = 2 (2024_0619 extension)
        {
            for (int i = 0; i < 2; i++)
            {
                bool circleDirectionLeft = (i == 0) ? true : false;

                for (int j = 0; j < 3; j++) // small area, big area, behind right
                {
                    if (!InTakenRel(1, 1) && !InBorderRel(1, 1))
                    {
                        int dist = 2;
                        while (!InTakenRel(dist, 1) && !InBorderRel(dist, 1))
                        {
                            dist++;
                        }
                        dist--;

                        int nextX = dist;
                        int nextY = 1;
                        int currentDirection = 0;
                        int counter = 0;

                        // The corner discovery head can be in any of the 4 quarters and the area is still closed at the right position. Only stop when reaching the corner or passing by the live end.
                        while (!InCornerRel(nextX, nextY) && !(nextX == 1 && nextY == 1))
                        {
                            counter++;
                            if (counter == size * size)
                            {
                                T("StartObstacleInside corner discovery error.");

                                window.errorInWalkthrough = true;
                                window.errorString = "StartObstacleInside corner discovery error.";
                                window.criticalError = true;
                                return;
                            }

                            // left direction
                            currentDirection = (currentDirection == 3) ? 0 : currentDirection + 1;
                            int l = currentDirection;
                            int possibleNextX = nextX + directions[currentDirection][0];
                            int possibleNextY = nextY + directions[currentDirection][1];

                            // turn right until a field is empty 
                            while (InBorderRel(possibleNextX, possibleNextY) || InTakenRel(possibleNextX, possibleNextY))
                            {
                                l = (l == 0) ? 3 : l - 1;
                                possibleNextX = nextX + directions[l][0];
                                possibleNextY = nextY + directions[l][1];
                            }

                            // At a corner, the obstacle is 1 5 distance away. At an areaUp, it is 0 5 or 0 3.
                            // Straight obstacle is allowed at 3 distance as in 2024_0619
                            if (currentDirection == 0 && nextX >= 0 && nextY > nextX &&
                                (l == 0 &&
                                (
                                nextX >= 1 && ((nextY - nextX) % 4 == 0 || (nextY - nextX) % 4 == 2) ||
                                nextX == 0 && ((nextY - nextX) % 4 == 1 || (nextY - nextX) % 4 == 3)
                                )
                                ||
                                (l == 3 && nextX == 0 && ((nextY - nextX) % 4 == 1 || (nextY - nextX) % 4 == 3))
                                ))
                            {
                                int hori = nextX + 1;
                                int vert = nextY + 1;

                                //T("Corner found at " + hori, vert, "side " + i, "rotation " + j);

                                bool circleValid = false;

                                if (InBorderRel(hori, vert))
                                {
                                    int i1 = InBorderIndexRel(hori, vert);
                                    int i2 = InBorderIndexRel(hori + 1, vert);

                                    if (i1 > i2)
                                    {
                                        circleValid = true;
                                    }
                                }
                                else
                                {
                                    int i1 = InTakenIndexRel(hori, vert);
                                    int i2 = InTakenIndexRel(hori + 1, vert);

                                    if (i2 > i1)
                                    {
                                        circleValid = true;
                                    }
                                }

                                if (circleValid)
                                {
                                    bool takenFound = false;
                                    List<int[]> borderFields = new();

                                    if (hori > 1)
                                    {
                                        for (int k = 1; k < hori; k++)
                                        {
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
                                            if (k < vert - hori)
                                            {
                                                borderFields.Add(new int[] { hori - 1, hori - 1 + k });
                                            }
                                        }
                                    }
                                    else
                                    {
                                        for (int k = 2; k <= vert - 2; k++)
                                        {
                                            borderFields.Add(new int[] { 1, k });
                                        }
                                        hori++; // count the neightboring obstacle as the corner
                                    }

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
                                            int whiteDiff = white - black;
                                            bool ruleTrue = false;

                                            switch ((vert - hori) % 4)
                                            {
                                                case 0:
                                                    if (j <= 1 && whiteDiff == (vert - hori) / 4 && CheckNearFieldSmallRel1(1, 2, 0, 1, true))
                                                    // Mid across: 2024_0618, 2024_0817, 2024_0818
                                                    // Across: 2024_0717_1
                                                    {
                                                        ruleTrue = true;
                                                        T("StartObstacleInside % 4 = 0: Cannot step straight");
                                                        AddForbidden(0, 1);

                                                        if (j == 0)
                                                        {
                                                            T("StartObstacleInside % 4 = 0: Cannot step right");
                                                            AddForbidden(-1, 0);
                                                        }
                                                    }
                                                    break;
                                                case 2:
                                                    if (j >= 1 && whiteDiff == (vert - hori + 2) / 4 && CheckNearFieldSmallRel1(1, 0, 0, 1, true))
                                                    // Mid across: 2024_0619
                                                    // Across: 2024_0717_2, area up 3 start obstacle
                                                    {
                                                        ruleTrue = true;
                                                        T("StartObstacleInside % 4 = 2: Cannot step left");
                                                        // straight direction is disabled already due to single area rule                                                        
                                                        if (hori > 2 && !InForbidden(new int[] { x + lx, y + ly }))
                                                        {
                                                            T("StartObstacleInside corner (y - x) % 4 = 2");

                                                            window.errorInWalkthrough = true;
                                                            window.errorString = "StartObstacleInside corner (y - x) % 4 = 2";
                                                            window.criticalError = true;
                                                            return;
                                                        }

                                                        AddForbidden(1, 0);
                                                    }
                                                    break;
                                            }

                                            if (ruleTrue)
                                            {
                                                AddExamAreas();
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

        void CheckStraightSmall() // 2024_0619_1, 2024_0714, 2024_0716, 2024_0717_4, 2024_0729_2
                                  // double obstacle inside
                                  // Two columns are checked for being empty, but at the end the straight field must be taken, and the left field must be empty.
        {
            for (int i = 0; i < 2; i++)
            {
                bool circleDirectionLeft = (i == 0) ? true : false;

                for (int j = 0; j < 3; j++) // j = 0: straight area, j = 1: right (big) area, j = 2: behind on right side (2024_0716)
                {
                    bool circleValid = false;
                    int dist = 1;
                    List<int[]> borderFields = new();

                    // stop when both 0 and 1 horizontal distance is empty or when border is encountered
                    while (InTakenRel(0, dist) && !InTakenRel(1, dist))
                    {
                        dist++;
                    }

                    if (!InTakenRel(0, dist) && !InTakenRel(1, dist))
                    {
                        // if border hasn't been encountered, continue and stop when 0 is taken, 1 is empty
                        while (!InTakenRel(0, dist) && !InTakenRel(1, dist) && !InBorderRel(0, dist))
                        {
                            dist++;
                        }

                        int ex = dist - 1;

                        if (ex >= 2 && ex <= 5 && InTakenRel(0, dist) && !InTakenRel(1, dist))
                        {
                            int i1 = InTakenIndexRel(0, dist);
                            int i2 = InTakenIndexRel(-1, dist);

                            if (i1 > i2)
                            {
                                circleValid = true;
                            }
                        }

                        if (circleValid)
                        {
                            for (int k = ex - 1; k >= 2; k--)
                            {
                                borderFields.Add(new int[] { 1, k });
                            }

                            ResetExamAreas();

                            if (CountAreaRel(1, 1, 1, ex, borderFields, circleDirectionLeft, 2, true))
                            {
                                int black = (int)info[1];
                                int white = (int)info[2];

                                if (j >= 1 && ex == 2 && white == black + 1)
                                // 2024_0717_4: across down, mid across up
                                // 2024_0729_2: mid across down, across up
                                {
                                    if (CheckNearFieldSmallRel1(1, 0, 0, 1, true) && CheckNearFieldSmallRel1(1, 2, 1, 2, false))
                                    {
                                        T("CheckStraightSmall 2 double close obstacle inside: Cannot step left");
                                        AddForbidden(1, 0);

                                        AddExamAreas();
                                    }
                                }

                                if (j >= 1 && ex == 3 && white == black + 1)
                                // 2024_0716: mid across x 2
                                // 2024_0729: mid across down, across up
                                // 2024_0730_1: across down, mid across up
                                {
                                    if (CheckNearFieldSmallRel1(1, 0, 0, 1, true) && CheckNearFieldSmallRel1(1, 2, 1, 2, false))
                                    {
                                        T("CheckStraightSmall 3 double close obstacle inside: Cannot step left");
                                        AddForbidden(1, 0);

                                        AddExamAreas();
                                    }
                                }

                                if (j <= 1 && ex == 4 && white == black + 1)
                                // 2024_0619_1: mid across x 2
                                // 2024_0729_1: across down, mid across up
                                // 2024_0729_4: mid across down, across up
                                // 2024_0820: mid across down, C-shape up
                                {
                                    if (CheckNearFieldSmallRel1(1, 2, 0, 1, true) && CheckNearFieldSmallRel(1, 4, 1, 2, false))
                                    {
                                        T("CheckStraightSmall 4 double close obstacle inside: Cannot step straight");
                                        AddForbidden(0, 1);

                                        if (j == 0)
                                        {
                                            T("CheckStraightSmall 4 double close obstacle inside: Cannot step right");
                                            AddForbidden(-1, 0);
                                        }
                                        AddExamAreas();
                                    }
                                }

                                if (j <= 1 && ex == 5 && white == black + 1) // 2024_0714: mid across x 2
                                {
                                    if (CheckNearFieldSmallRel1(1, 2, 0, 1, true) && CheckNearFieldSmallRel1(1, 4, 1, 2, false))
                                    {
                                        T("CheckStraightSmall 5 double close obstacle inside: Cannot step straight");
                                        AddForbidden(0, 1);

                                        if (j == 0)
                                        {
                                            T("CheckStraightSmall 5 double close obstacle inside: Cannot step right");
                                            AddForbidden(-1, 0);
                                        }
                                        AddExamAreas();
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

        void CheckLeftRightAreaUpBigExtended() // Area as in the first area case of documentation. That area is taken care of in UpBig and Striaght. This is about a border movement close obstacle: 2024_0624
        {
            for (int i = 0; i < 2; i++)
            {
                bool circleDirectionLeft = (i == 0) ? false : true;

                for (int j = 0; j < 3; j++) // j = 1: small area, j = 2: big area
                {
                    bool circleValid = false;
                    int dist = 1;
                    List<int[]> borderFields = new();

                    while (!InTakenRel(1, dist) && !InBorderRel(1, dist) && !InTakenRel(0, dist))
                    {
                        dist++;
                    }

                    int ex = dist - 1;

                    if (ex > 1 && InTakenRel(1, dist))
                    {
                        int i1 = InTakenIndexRel(1, dist);

                        if (InTakenRel(2, dist))
                        {
                            int i2 = InTakenIndexRel(2, dist);
                            if (i1 > i2)
                            {
                                circleValid = true;
                            }
                        }
                        else
                        {
                            int i2 = InTakenIndexRel(0, dist);
                            if (i2 > i1)
                            {
                                circleValid = true;
                            }
                        }
                    }

                    if (circleValid)
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
                            int nowWCountRight = 0;
                            int nowBCount = 0;
                            int laterWCount = 0;
                            int laterBCount = 0;

                            bool ruleTrue = false;

                            switch (ex % 4)
                            {
                                case 0:
                                    nowWCountRight = nowWCount = ex / 4;
                                    nowBCount = ex / 4 - 1;
                                    laterWCount = ex / 4;
                                    laterBCount = ex / 4;

                                    if (whiteDiff == laterWCount && CheckNearFieldSmallRel1(1, 1, 0, 1, false))
                                    // 2024_0624: mid across
                                    // 2024_0730_2: across
                                    // When entering at the first white field, we have to step down to the first black and then left to enter
                                    {
                                        ruleTrue = true;
                                        T("CheckLeftRightAreaUpBigExtended start obstacle: Cannot step left");
                                        AddForbidden(1, 0);
                                        if (j == 2)
                                        {
                                            AddForbidden(0, -1);
                                        }
                                    }

                                    break;
                                case 1:
                                    nowWCountRight = nowWCount = (ex - 1) / 4;
                                    nowBCount = (ex - 1) / 4;
                                    laterWCount = (ex - 1) / 4;
                                    laterBCount = (ex + 3) / 4;
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
                                        laterBCount = (ex + 2) / 4;
                                    }
                                    break;
                                case 3:
                                    nowWCountRight = nowWCount = (ex - 3) / 4;
                                    nowBCount = (ex + 1) / 4;
                                    laterWCount = (ex - 3) / 4;
                                    laterBCount = (ex + 1) / 4;
                                    break;
                            }

                            if (ruleTrue)
                            {
                                AddExamAreas();
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

        void CheckStraightBig() // 9_18677343 -> StairAtEndConvex, 9_59434452 -> StairAtEndConvex, 2024_0626_1 -> StairAtStart 4, 2024_0516_2 -> StairAtEndConvex
        {
            for (int i = 0; i < 2; i++)
            {
                bool circleDirectionLeft = (i == 0) ? true : false;

                for (int j = 0; j < 2; j++) // j = 0: straight area, j = 1: left (small) area
                {
                    bool circleValid = false;
                    int dist = 1;
                    List<int[]> borderFields = new();

                    while (!InTakenRel(0, dist) && !InBorderRel(0, dist) && !InTakenRel(-1, dist) && !InBorderRel(-1, dist))
                    {
                        dist++;
                    }

                    int ex = dist - 1;

                    if (InTakenRel(0, dist) && !InTakenRel(-1, dist))
                    {
                        int i1 = InTakenIndexRel(0, dist);
                        int i2 = InTakenIndexRel(1, dist);

                        if (i2 > i1)
                        {
                            circleValid = true;
                        }

                        if (circleValid)
                        {
                            for (int k = ex - 1; k >= 2; k--)
                            {
                                borderFields.Add(new int[] { -1, k });
                            }

                            ResetExamAreas();
                            if (ex == 3)
                            {
                                if (CountAreaRel(-1, 1, -1, ex, borderFields, circleDirectionLeft, 2, true))
                                {
                                    int black = (int)info[1];
                                    int white = (int)info[2];

                                    /*// 9_18677343, 9_59434452: mid across x 2
                                    // 2024_0709: mid across down, C-shape up 
                                    if (white == black)
                                    {
                                        if (CheckNearFieldSmallRel0(-1, 1, 1, 1, false) && CheckNearFieldSmallRel(-1, 3, 0, 2, true))
                                        {
                                            T("CheckStraightBig double close obstacle outside 3 dist 0W: Cannot step right and down");
                                            AddForbidden(0, -1);
                                            AddForbidden(-1, 0);

                                            AddExamAreas();
                                        }
                                    }*/
                                    // 2024_0516_2
                                    /*else if (black == white + 1)
                                    {
                                        if (CheckNearFieldSmallRel1(-1, 2, 0, 2, true) && CheckNearFieldSmallRel0(-1, 2, 1, 1, true))
                                        {
                                            T("CheckStraightBig double close obstacle outside 3 dist 1B: Cannot step straight and left");
                                            AddForbidden(0, 1);
                                            AddForbidden(1, 0);

                                            AddExamAreas();
                                        }
                                    }*/
                                }
                            }
                            /*// 2024_0626_1: mid across x 2
                            // 2024_0729_3: mid across down, across up
                            // 2024_0730: across down, mid across up
                            else if (ex == 4)
                            {
                                if (CountAreaRel(-1, 1, -1, ex, borderFields, circleDirectionLeft, 2, true))
                                {
                                    int black = (int)info[1];
                                    int white = (int)info[2];

                                    if (white == black + 1)
                                    {
                                        if (CheckNearFieldSmallRel1(-1, 1, 1, 1, false) && CheckNearFieldSmallRel1(-1, 3, 0, 2, true))
                                        {
                                            T("CheckStraightBig double close obstacle outside 4 dist: Cannot step right and down");
                                            AddForbidden(0, -1);
                                            AddForbidden(-1, 0);

                                            AddExamAreas();
                                        }
                                    }
                                }
                            }*/
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
                                    path.Add(new int[] { x + sx, y + sy }); // right side area checking needs it
                                    path.Add(new int[] { x + 3 * sx, y + 3 * sy }); // left side area checking needs it
                                    path.Add(new int[] { x - lx + 2 * sx, y - ly + 2 * sy });

                                    // step after exiting area:
                                    x2 = x - lx + 2 * sx;
                                    y2 = y - ly + 2 * sy;

                                    int[] rotatedDir = RotateDir(lx, ly, i);
                                    lx2 = rotatedDir[0];
                                    ly2 = rotatedDir[1];
                                    rotatedDir = RotateDir(sx, sy, i);
                                    sx2 = rotatedDir[0];
                                    sy2 = rotatedDir[1];

                                    ResetExamAreas();

                                    counterrec = 0;

                                    if (CheckSequenceRecursive(i))
                                    {
                                        path.RemoveAt(path.Count - 1);
                                        path.RemoveAt(path.Count - 1);
                                        path.RemoveAt(path.Count - 1);

                                        AddExamAreas(true);

                                        activeRules.Add("Sequence first case");
                                        activeRuleSizes.Add(new int[] { 5, 5 });
                                        activeRulesForbiddenFields.Add(new List<int[]> { new int[] { x + lx, y + ly }, new int[] { x + sx, y + sy } });

                                        T("CheckSequence case 1 at " + x + " " + y + ", stop at " + x2 + " " + y2 + ": Cannot step straight");
                                        AddForbidden(0, 1);

                                        if (j == 0)
                                        {
                                            // Due to CheckStraight, stepping left is already disabled when the obstacle is straight ahead. When it is one to the right, we need the left field to be disabled.
                                            T("CheckSequence case 1 at " + x + " " + y + ", stop at " + x2 + " " + y2 + ": Cannot step left");
                                            AddForbidden(1, 0);
                                        }

                                        AddForbidden(1, 0);
                                        AddForbidden(0, 1);
                                    }
                                    else
                                    {
                                        path.RemoveAt(path.Count - 1);
                                        path.RemoveAt(path.Count - 1);
                                        path.RemoveAt(path.Count - 1);
                                    }
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


                    if (InTakenRel(0, 3) && !InTakenRel(0, 1) && !InTakenRel(0, 2) && !InTakenRel(-1, 3) && !InTakenRel(-2, 2)) // Field in front of exit should also be empty
                    {
                        int directionFieldIndex = InTakenIndexRel(0, 3);
                        int leftIndex = InTakenIndexRel(1, 3);

                        if (leftIndex > directionFieldIndex)
                        {
                            ResetExamAreas();

                            if (CountAreaRel(0, 1, 0, 2, null, circleDirectionLeft, 2, true))
                            {
                                int black = (int)info[1];
                                int white = (int)info[2];

                                if (black == white)
                                {
                                    path.Add(new int[] { x + sx, y + sy }); // right side area checking needs it
                                    path.Add(new int[] { x - lx + 2 * sx, y - ly + 2 * sy });

                                    x2 = x - lx + 2 * sx;
                                    y2 = y - ly + 2 * sy;

                                    int[] rotatedDir = RotateDir(lx, ly, i);
                                    lx2 = rotatedDir[0];
                                    ly2 = rotatedDir[1];
                                    rotatedDir = RotateDir(sx, sy, i);
                                    sx2 = rotatedDir[0];
                                    sy2 = rotatedDir[1];

                                    counterrec = 0;

                                    if (CheckSequenceRecursive(i))
                                    {
                                        path.RemoveAt(path.Count - 1);
                                        path.RemoveAt(path.Count - 1);

                                        AddExamAreas(true);

                                        activeRules.Add("Sequence second case");
                                        activeRuleSizes.Add(new int[] { 5, 5 });
                                        activeRulesForbiddenFields.Add(new List<int[]> { new int[] { x + lx, y + ly }, new int[] { x + sx, y + sy } });

                                        T("CheckSequence case 2 at " + x + " " + y + ", stop at " + x2 + " " + y2 + ": Cannot step straight");
                                        AddForbidden(0, 1);

                                        if (j == 0)
                                        {
                                            T("CheckSequence case 2 at " + x + " " + y + ", stop at " + x2 + " " + y2 + ": Cannot step left");
                                            AddForbidden(1, 0);
                                        }
                                    }
                                    else
                                    {
                                        path.RemoveAt(path.Count - 1);
                                        path.RemoveAt(path.Count - 1);
                                    }
                                }
                            }
                        }
                    }

                    int l0 = lx; // rotate down (CCW)
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
                    if (InTakenRel(1, 3) && !InTakenRel(1, 2) && !InTakenRel(0, 3) && !InTakenRel(0, 1) && !InTakenRel(-1, 2))
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
                                    path.Add(new int[] { x + sx, y + sy }); // right side area checking needs it
                                    path.Add(new int[] { x + 2 * sx, y + 2 * sy });

                                    x2 = x + 2 * sx;
                                    y2 = y + 2 * sy;

                                    int[] rotatedDir = RotateDir(lx, ly, i);
                                    lx2 = rotatedDir[0];
                                    ly2 = rotatedDir[1];
                                    rotatedDir = RotateDir(sx, sy, i);
                                    sx2 = rotatedDir[0];
                                    sy2 = rotatedDir[1];

                                    ResetExamAreas();

                                    counterrec = 0;

                                    if (CheckSequenceRecursive(i))
                                    {
                                        path.RemoveAt(path.Count - 1);
                                        path.RemoveAt(path.Count - 1);

                                        AddExamAreas(true);

                                        activeRules.Add("Sequence third case");
                                        activeRuleSizes.Add(new int[] { 4, 5 });
                                        activeRulesForbiddenFields.Add(new List<int[]> { new int[] { x + sx, y + sy } });

                                        T("CheckSequence case 3 at " + x + " " + y + ", stop at " + x2 + " " + y2 + ": Cannot step straight");
                                        AddForbidden(0, 1);
                                    }
                                    else
                                    {
                                        path.RemoveAt(path.Count - 1);
                                        path.RemoveAt(path.Count - 1);
                                    }
                                }
                            }
                        }
                    }

                    if (j == 0) // rotate down (CCW)
                    {
                        int l0 = lx;
                        int l1 = ly;
                        lx = -sx;
                        ly = -sy;
                        sx = l0;
                        sy = l1;
                    }
                    else if (j == 1) // rotate up (CW)
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

            // Fourth case, next step C-shape
            // 2024_0630, 2024_0720: Solved by StairArea
            // 2024_0723, 2024_0723_1 -> Sequence 2 stair start
            // Sequence has to begin already at the next step, not at the exit point of the first C-shape: 2024_0725_3 -> Next step double area
            // Rotated CCW

            for (int i = 0; i < 2; i++)
            {
                for (int j = 0; j < 2; j++)
                {
                    if (InTakenRel(2, 1) && InTakenRel(1, 0) && !InTakenRel(1, 1))
                    {
                        path.Add(new int[] { x + sx, y + sy });

                        x2 = x + sx;
                        y2 = y + sy;
                        lx2 = lx;
                        ly2 = ly;
                        sx2 = sx;
                        sy2 = sy;

                        ResetExamAreas();

                        counterrec = 0;

                        if (CheckSequenceRecursive(i))
                        {
                            path.RemoveAt(path.Count - 1);

                            AddExamAreas(true);

                            T("CheckSequence case 4 at " + x + " " + y + ", stop at " + x2 + " " + y2 + ": Cannot step straight");
                            AddForbidden(0, 1);
                        }
                        else path.RemoveAt(path.Count - 1);
                    }

                    // rotate down (CCW)
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

            // Fifth case, 2024_0724_1: Step right next step C-shape. There is an obstacle 2 distance to the right to start with.

            /*for (int i = 0; i < 2; i++)
            {
                for (int j = 0; j < 2; j++)
                {
                    if (InTakenRel(1, 1) && !InTakenRel(0, 1) && InTakenRel(-3, 0) && !InTakenRel(-2, 0) && !InTakenRel(-1, 0) && !InTakenRel(-3, 1))
                    {
                        int directionFieldIndex = InTakenIndexRel(-3, 0);
                        int sideIndex = InTakenIndexRel(-3, -1);

                        if (directionFieldIndex > sideIndex)
                        {
                            path.Add(new int[] { x - lx, y - ly });
                            path.Add(new int[] { x - lx + sx, y - ly + sy });
                            path.Add(new int[] { x + sx, y + sy });
                            path.Add(new int[] { x + 2 * sx, y + 2 * sy });

                            x2 = x + 2 * sx;
                            y2 = y + 2 * sy;
                            lx2 = lx;
                            ly2 = ly;
                            sx2 = sx;
                            sy2 = sy;

                            ResetExamAreas();

                            counterrec = 0;

                            if (CheckSequenceRecursive(i))
                            {
                                path.RemoveAt(path.Count - 1);
                                path.RemoveAt(path.Count - 1);
                                path.RemoveAt(path.Count - 1);
                                path.RemoveAt(path.Count - 1);

                                AddExamAreas(true);

                                T("CheckSequence case 5 at " + x + " " + y + ", stop at " + x2 + " " + y2 + ": Cannot step right");
                                AddForbidden(-1, 0);

                                if (j == 1)
                                {
                                    T("CheckSequence case 5 at " + x + " " + y + ", stop at " + x2 + " " + y2 + ": Cannot step down");
                                    AddForbidden(0, -1);
                                }
                            }
                            else
                            {
                                path.RemoveAt(path.Count - 1);
                                path.RemoveAt(path.Count - 1);
                                path.RemoveAt(path.Count - 1);
                                path.RemoveAt(path.Count - 1);
                            }
                        }
                    }

                    // rotate down (CCW)
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
            ly = thisLy;*/


            // Sixth case: 2024_0727_6, implemented in UpExtended
        }

        void CheckNearStair() // 2024_0726, 2024_0713, nearStair 1/2/3
        {
            for (int i = 0; i < 2; i++)
            {
                for (int j = 0; j < 2; j++) // normal or big area
                {
                    int dist = 1;
                    while (!InTakenRel(-dist + 1, dist) && InTakenRel(-dist, dist))
                    {
                        dist++;
                    }
                    dist--;

                    if (dist >= 3)
                    {
                        if (CheckNearFieldSmallRel0(-dist + 2, dist, 0, 0, true) && CheckNearFieldSmallRel1(-dist + 3, dist - 1, 0, 0, true) || CheckNearFieldSmallRel1(-dist + 2, dist, 0, 0, true) && CheckNearFieldSmallRel0(-dist + 4, dist - 2, 0, 0, true))
                        {
                            if (AddForbidden(1, 0))
                            {
                                T("NearStair: Cannot enter now left");
                                if (j == 1)
                                {
                                    T("NearStair: Cannot enter now down");
                                    AddForbidden(0, -1);
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

        void CheckSideStair()
        // Start at -1 vertical. 2024_0516_6, 2024_0516_7, 2024_0516_8 -> Sequence2
        {
            for (int i = 0; i < 2; i++)
            {
                bool circleDirectionLeft = (i == 0) ? true : false;

                for (int j = 0; j < 3; j++)
                {
                    int hori = 1;
                    int vert = -1;

                    while (!InTakenRel(hori, vert) && !InBorderRel(hori, vert))
                    {
                        hori++;
                    }

                    if (hori == 3)
                    {
                        int i1 = InTakenIndexRel(hori, vert);
                        int i2 = InTakenIndexRel(hori, vert - 1);

                        if (i2 != -1 && i2 > i1)
                        {
                            bool stepFound = true;
                            while (stepFound)
                            {
                                hori++;
                                vert++;
                                if (!((InTakenRel(hori, vert) || InBorderRelExact(hori, vert)) && !InTakenRel(hori - 1, vert)))
                                {
                                    stepFound = false;
                                }
                                else if (CheckNearFieldSmallRel1(hori - 2, vert, 1, 0, true))
                                {
                                    T("CheckSideStair at " + hori + " " + vert + ": Cannot step left");
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

        void CheckSideStairStraight()
        // -> Sequence 2
        // Start at 0 vertical. 2024_1001
        {
            for (int i = 0; i < 2; i++)
            {
                bool circleDirectionLeft = (i == 0) ? true : false;

                for (int j = 0; j < 3; j++)
                {
                    int hori = 1;
                    int vert = 0;

                    while (!InTakenRel(hori, vert) && !InBorderRel(hori, vert))
                    {
                        hori++;
                    }

                    if (hori == 3)
                    {
                        T("SideStairStraight", i, j);
                        int i1 = InTakenIndexRel(hori, vert);
                        int i2 = InTakenIndexRel(hori, vert - 1);

                        if (i2 != -1 && i2 > i1)
                        {
                            bool stepFound = true;
                            int counter = 0;
                            while (stepFound)
                            {
                                hori++;
                                vert++;
                                counter++;

                                path.Add(new int[] { x + (hori - 3) * lx + (vert - 1) * sx, y + (hori - 3) * ly + (vert - 1) * sy });

                                if (!((InTakenRel(hori, vert) || InBorderRelExact(hori, vert)) && !InTakenRel(hori - 1, vert)))
                                {
                                    stepFound = false;
                                }
                                else if (CheckCorner1(hori - 2, vert, 1, 0, circleDirectionLeft, true))
                                {
                                    T("CheckSideStairStraight at " + hori + " " + vert + ": Cannot step left");

                                    for (int m = 1; m <= counter; m++)
                                    {
                                        path.RemoveAt(path.Count - 1);
                                    }
                                    counter = 0;
                                    AddForbidden(1, 0);
                                    break;
                                }
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

        /* This function is incomplete as the found corners can be separated from the live end. In 2024_0901, if called in the j = 3 rotation, CornerDiscovery(0, 1, false, true, 3) will return the 7, 3 corner. That will result in an infinite loop in RemoteStair where a wall should be found at each descend.  */

        bool CheckSequenceRecursive(int side)
        {
            T("Recursive start side: " + side, "x2 y2 lx2 ly2: " + x2, y2, lx2, ly2);

            counterrec++;
            if (counterrec == size * size)
            {
                window.errorInWalkthrough = true;
                window.errorString = "Recursive overflow.";
                window.criticalError = true;
                return false;
            }

            newExitField0 = new int[] { 0, 0 };
            newExitField = new int[] { 0, 0 };

            ResetExamAreas2(); // prevents showing an area from previous cycle

            bool leftSideEnterNow = CheckCorner2(side, true);
            sequenceLeftObstacleIndex = -1; // needed for 2024_0811_1 and 2024_0811_2 where live end would be inside the area
            bool leftSideClose = CheckNearFieldSmall2(); // contains exit points for next call but only works for c-shapes and close obstacles.
            lx2 = -lx2;
            ly2 = -ly2;
            bool rightSideClose = CheckNearFieldSmall3(); // 2024_0722, 2024_0811
            int tempSequenceLeftIndex = sequenceLeftObstacleIndex;
            sequenceLeftObstacleIndex = -1;
            bool rightSideEnterNow = CheckCorner2(1 - side, true);
            sequenceLeftObstacleIndex = tempSequenceLeftIndex;
            lx2 = -lx2;
            ly2 = -ly2;

            T("Recursive checked", leftSideClose, leftSideEnterNow, rightSideClose, rightSideEnterNow, sequenceLeftObstacleIndex);

            if ((leftSideClose || leftSideEnterNow) && (rightSideClose || rightSideEnterNow))
            {
                return true;
            }
            // right side close can happen with the future line
            // for now, we only take the right side C-shape into account as it happens in 9_740293. Other close obstacles we don't check.
            else if (leftSideClose)
            //else if ((leftSideClose || rightSideClose) && newExitField[0] != 0)
            {
                T("CheckSequenceRecursive left side only x2 " + newExitField[0] + " y2 " + newExitField[1] + " direction rotated " + newDirectionRotated);

                // at 2024_0723, 2024_0723_1, it is important that both fields are added, because the sequence relies on the first when determining the direction of the obstacle.
                bool firstAdded = false;
                x2 = newExitField0[0];
                y2 = newExitField0[1];
                if (x2 != 0 && y2 != 0)
                {
                    firstAdded = true;
                    path.Add(new int[] { x2, y2 });
                }
                x2 = newExitField[0];
                y2 = newExitField[1];
                path.Add(new int[] { x2, y2 });

                if (newDirectionRotated)
                {
                    int[] rotatedDir = RotateDir(lx2, ly2, side);
                    lx2 = rotatedDir[0];
                    ly2 = rotatedDir[1];
                    rotatedDir = RotateDir(sx2, sy2, side);
                    sx2 = rotatedDir[0];
                    sy2 = rotatedDir[1];
                }

                if (InTakenRel2(0, 1)) // 2023_0708 Field in front of exit should be empty
                {
                    if (firstAdded)
                    {
                        path.RemoveAt(path.Count - 1);
                    }
                    path.RemoveAt(path.Count - 1);

                    sequenceLeftObstacleIndex = -1;
                    return false;
                }

                bool ret = CheckSequenceRecursive(side);

                if (firstAdded)
                {
                    path.RemoveAt(path.Count - 1);
                }
                path.RemoveAt(path.Count - 1);

                sequenceLeftObstacleIndex = -1;
                return ret;

            }
            else if (rightSideClose)
            {
                T("CheckSequenceRecursive right side only x2 " + newExitField[0] + " y2 " + newExitField[1] + " direction rotated " + newDirectionRotated);

                x2 = newExitField[0];
                y2 = newExitField[1];
                path.Add(new int[] { x2, y2 });
                lx2 = -lx2;
                ly2 = -ly2;

                if (newDirectionRotated)
                {
                    int[] rotatedDir = RotateDir(lx2, ly2, 1 - side);
                    lx2 = rotatedDir[0];
                    ly2 = rotatedDir[1];
                    rotatedDir = RotateDir(sx2, sy2, 1 - side);
                    sx2 = rotatedDir[0];
                    sy2 = rotatedDir[1];
                }

                if (InTakenRel2(0, 1)) // 2023_0708: Field in front of exit should be empty
                {
                    path.RemoveAt(path.Count - 1);

                    sequenceLeftObstacleIndex = -1;
                    return false;
                }

                bool ret = CheckSequenceRecursive(1 - side);
                path.RemoveAt(path.Count - 1);

                sequenceLeftObstacleIndex = -1;
                return ret;
            }
            else
            {
                sequenceLeftObstacleIndex = -1;
                return false;
            }
        }

        bool CheckNearFieldSmall2() // for use with Sequence
                                    // Case 2 and 3, used in recursive function
        {
            bool ret = false;

            // C-Shape, only left side should have it
            // Checking for InTakenRel2(1, -1) is not possible, because in Sequence first case, we are exiting the area at the middle border field.
            // But when it comes to the right side (if it was checked), it is necessary, otherwise we can detect a C-shape with the live end as in 9_213.
            if ((InTakenRel2(2, 0) || InBorderRelExact2(2, 0)) && !InTakenRel2(1, 0) && !InBorderRelExact2(1, 0))
            {
                T("CheckNearFieldSmall2 C-Shape, left side");
                ret = true;

                newExitField0 = new int[] { x2 + lx2, y2 + ly2 };
                newExitField = new int[] { x2 + lx2 + sx2, y2 + ly2 + sy2 };
                newDirectionRotated = false;
                //sequenceLeftObstacleIndex = InTakenIndexRel2(2, 0); example needed
            }

            //C-Shape up
            if (InTakenRel2(0, 2) && InTakenRel2(1, 1) && !InTakenRel2(0, 1))
            {
                T("CheckNearFieldSmall2 C-Shape up");
                ret = true;

                newExitField = new int[] { x2 - lx2 + sx2, y2 - ly2 + sy2 };
                newDirectionRotated = true;
                //sequenceLeftObstacleIndex = InTakenIndexRel2(0, 2); example needed
            }

            // close mid across
            if (InTakenRel2(1, 2) && !InTakenRel2(0, 2) && !InTakenRel2(1, 1))
            {
                int middleIndex = InTakenIndexRel2(1, 2);
                int sideIndex = InTakenIndexRel2(2, 2);

                if (sideIndex > middleIndex)
                {
                    T("CheckNearFieldSmall2 close mid across");
                    ret = true;

                    // mid across overwrites C-shape
                    newExitField = new int[] { x2 + sx2, y2 + sy2 };
                    newDirectionRotated = true;
                    sequenceLeftObstacleIndex = middleIndex; // 2024_0811_2
                }
            }

            // close across. Checking empty fields necessary, see 9_29558469
            if (InTakenRel2(2, 2) && !InTakenRel2(1, 2) && !InTakenRel2(2, 1))
            {
                int middleIndex = InTakenIndexRel2(2, 2);
                int sideIndex = InTakenIndexRel2(3, 2);

                if (sideIndex > middleIndex)
                {
                    T("CheckNearFieldSmall2 close across");
                    ret = true;

                    newExitField = new int[] { x2 + lx2 + sx2, y2 + ly2 + sy2 };
                    newDirectionRotated = true;
                    sequenceLeftObstacleIndex = middleIndex; // 2024_0811_1
                }
            }

            return ret;
        }

        bool CheckNearFieldSmall3()
        {
            bool ret = false;

            // 2024_0811
            // C-shape
            if ((InTakenRel2(2, 0) || InBorderRelExact2(2, 0)) && InTakenRel2(1, -1) && !InTakenRel2(1, 0) && !InBorderRelExact2(1, 0))
            {
                T("CheckNearFieldSmall3 C-Shape");
                ret = true;

                // newexitfield not necessary for now, Sequence will be true.
            }

            // 2024_0722
            // close mid across
            if (InTakenRel2(1, 2) && !InTakenRel2(0, 2) && !InTakenRel2(1, 1))
            {
                int middleIndex = InTakenIndexRel2(1, 2);
                int sideIndex = InTakenIndexRel2(2, 2);

                if (sideIndex > middleIndex)
                {
                    T("CheckNearFieldSmall3 close mid across");
                    ret = true;

                    // mid across overwrites C-shape
                    newExitField = new int[] { x2 + sx2, y2 + sy2 };
                    newDirectionRotated = true;
                }
            }

            return ret;
        }

        bool CheckCorner2(int side, bool smallArea) // #8
        {
            bool circleDirectionLeft = (side == 0) ? true : false;

            // 1, 1 relative field cannot be taken
            int horiStart = 1;
            while (!InTakenRel2(horiStart, 1) && !InBorderRel2(horiStart, 1))
            {
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

                    // turn right until a field is empty 
                    while (InBorderRel2(possibleNextX, possibleNextY) || InTakenRel2(possibleNextX, possibleNextY))
                    {
                        l = (l == 0) ? 3 : l - 1;
                        possibleNextX = nextX + directions[l][0];
                        possibleNextY = nextY + directions[l][1];
                    }

                    if (currentDirection == 0 && l == 0 && nextY >= 1) // 2024_0708: Corner can be found beneath
                    {
                        int hori = nextX + 1;
                        int vert = nextY + 1;

                        T("Corner at", hori, vert, "x2", x2, "y2", y2, "lx2", lx2, "ly2", ly2, circleDirectionLeft);

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
                                return true;

                            }
                            else if (hori == 2 && vert == 2) // close across
                            {
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
                                            T("Corner2: Cannot enter later");
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

            return false;
        }

        List<int[]>? CornerDiscovery(int startX, int startY, bool toLeft, bool closedCorner, int minEndCoord)
        {
            List<int[]> foundCorners = new();

            // Can we have an area with a corner if this field is taken? It isn't in the border line.
            if (!InTakenRel(startX, startY) && !InBorderRel(startX, startY))
            {
                // checking taken fields from the middle to side is incomplete: 9_17699719
                // instead, we check fields in the first row until an obstacle is found, then we walk around the first (top-left) quarter.
                if (toLeft)
                {
                    int coordStart = startX + 1;
                    while (!InTakenRel(coordStart, startY) && !InBorderRel(coordStart, startY))
                    {
                        coordStart++;
                    }

                    // relative directions and coordinates. Since the relative x and y is expanding towards the upper left corner, the current direction is what we use for downwards motion in the absolute coordinate system.
                    int currentDirection = 0;

                    int nextX = coordStart - 1;
                    int nextY = startY;

                    int counter = 0;

                    // area walls can be found in any of the four quarters. Therefore, we only stop when we have reached the corner or passed by the live end.
                    // If we terminate the walkthrough at startX and startY, a close across obstacle will might be missed (2024_0822). Therefore, we shift the x coordinate. 
                    while (!InCornerRel(nextX, nextY) && !(nextX == startX - 1 && nextY == startY))
                    {
                        //T("Corner discovery nextX " + nextX, "nextY " + nextY);

                        counter++;
                        if (counter == size * size)
                        {
                            T("Corner discovery error.");

                            window.errorInWalkthrough = true;
                            window.errorString = "Corner discovery error.";
                            window.criticalError = true;
                            return null;
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
                                return null;
                            }

                            leftDirection = (leftDirection == 0) ? 3 : leftDirection - 1;
                            possibleNextX = nextX + directions[leftDirection][0];
                            possibleNextY = nextY + directions[leftDirection][1];
                        }

                        // if we have turned left from a right direction (to upwards), a corner is found
                        // It has to be left and up. In 2024_0619_2 the walking edge goes below the current position.
                        // minEndCoord is 2 for across corner, 1 for up left, 0 for corner straight ahead

                        if (nextX >= minEndCoord - 1 && nextY >= 0)
                        {
                            if (closedCorner && currentDirection == 0 && leftDirection == 0)
                            {
                                foundCorners.Add(new int[] { nextX + 1, nextY + 1 });
                            }
                            else if (!closedCorner && currentDirection == 1 && leftDirection == 1)
                            {
                                foundCorners.Add(new int[] { nextX + 1, nextY - 1 });
                            }
                        }

                        currentDirection = leftDirection;

                        nextX = possibleNextX;
                        nextY = possibleNextY;
                    }
                }
                else // up
                {
                    int coordStart = startY + 1;
                    while (!InTakenRel(startX, coordStart) && !InBorderRel(startX, coordStart))
                    {
                        coordStart++;
                    }

                    T("coordStart: " + coordStart);
                    int currentDirection = 1;

                    int nextX = startX;
                    int nextY = coordStart - 1;

                    T("nextX0", nextX, nextY);
                    int counter = 0;

                    // area walls can be found in any of the four quarters. Therefore, we only stop when we have reached the corner or passed by the live end.
                    while (!InCornerRel(nextX, nextY) && !(nextX == startX && nextY == startY - 1))
                    {
                        T("nextX1", nextX, nextY);
                        counter++;
                        if (counter == size * size)
                        {
                            T("Corner discovery error.");

                            window.errorInWalkthrough = true;
                            window.errorString = "Corner discovery error.";
                            window.criticalError = true;
                            return null;
                        }

                        // up direction
                        int upDirection = (currentDirection == 0) ? 3 : currentDirection - 1;
                        currentDirection = upDirection;
                        int possibleNextX = nextX + directions[upDirection][0];
                        int possibleNextY = nextY + directions[upDirection][1];

                        int counter2 = 0;
                        // turn left until a field is empty 
                        while (InBorderRel(possibleNextX, possibleNextY) || InTakenRel(possibleNextX, possibleNextY))
                        {
                            counter2++;
                            if (counter2 == 4)
                            {
                                T("Corner discovery error 2.");

                                window.errorInWalkthrough = true;
                                window.errorString = "Corner discovery error 2.";
                                window.criticalError = true;
                                return null;
                            }

                            upDirection = (upDirection == 3) ? 0 : upDirection + 1;
                            possibleNextX = nextX + directions[upDirection][0];
                            possibleNextY = nextY + directions[upDirection][1];
                        }

                        if (nextX >= 0 && nextY >= minEndCoord - 1)
                        {
                            if (closedCorner && currentDirection == 1 && upDirection == 1)
                            {
                                foundCorners.Add(new int[] { nextX + 1, nextY + 1 });
                            }
                            else if (!closedCorner && currentDirection == 0 && upDirection == 0)
                            {
                                foundCorners.Add(new int[] { nextX - 1, nextY + 1 });
                            }
                        }

                        currentDirection = upDirection;

                        nextX = possibleNextX;
                        nextY = possibleNextY;
                    }
                }

                return foundCorners;
            }
            else
            {
                return null;
            }
        }

        bool CountAreaNew(int startX, int startY, int endX, int endY, List<int[]>? borderFields, bool circleDirectionLeft, int circleType, bool getInfo = false)
        // compareColors is for the starting situation of 2023_1119, where we mark an impair area and know the entry and the exit field. We count the number of white and black cells of a checkered pattern, the color of the entry and exit should be one more than the other color.
        {
            bool debug = false;
            bool debug2 = false;

            // find coordinates of the top left (circleDirection = right) or top right corner (circleDirection = left)
            int minY = startY;
            int limitX = startX;
            int startIndex;

            int xDiff, yDiff;
            List<int[]> areaLine = new();

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

            if (x == nextX + xDiff && y == nextY + yDiff || InTaken(nextX + xDiff, nextY + yDiff) || x == nextX + xDiff + directions[turnedDirection][0] && y == nextY + yDiff + directions[turnedDirection][1])
            {
                currentDirection = turnedDirection;
            }

            // T("currentDirection: " + currentDirection + ", " + (nextX + directions[currentDirection][0]) + " " + (nextY + directions[currentDirection][1]) + " taken: " + InTaken(nextX + directions[currentDirection][0], nextY + directions[currentDirection][1]));

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

                while (InBorder(possibleNextX, possibleNextY) || InTaken(possibleNextX, possibleNextY))
                {
                    i = (i == 0) ? 3 : i - 1;
                    possibleNextX = nextX + directions[i][0];
                    possibleNextY = nextY + directions[i][1];
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

                //T(nextX + " " + nextY);
                // when getting info about area
                if (nextX == size && nextY == size)
                {
                    T("Corner is reached.");

                    window.errorInWalkthrough = true;
                    window.criticalError = true;
                    window.errorString = "Corner is reached.";
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

            examAreaLines.Add(areaLine);
            examAreaLineTypes.Add(circleType);
            examAreaLineDirections.Add(circleDirectionLeft);

            int area = 0;
            List<int[]> startSquares = new();
            List<int[]> endSquares = new();

            if (areaLine.Count > 2)
            {
                int thisStartX = startX;
                int thisStartY = startY;

                int[] startCandidate = new int[] { limitX, minY };
                int[] endCandidate = new int[] { limitX, minY };

                if (debug2) T("arealine start " + startCandidate[0] + " " + startCandidate[1]);

                int currentY = minY;

                bool singleField = false;
                // check if there is a one square row on the top

                int prevIndex = (startIndex > 0) ? startIndex - 1 : areaLine.Count - 1;
                int nextIndex = (startIndex < areaLine.Count - 1) ? startIndex + 1 : 0;

                if (areaLine[startIndex][1] != areaLine[prevIndex][1] && areaLine[startIndex][1] != areaLine[nextIndex][1])
                {
                    singleField = true;
                    if (debug2) T("Single field on top");
                }

                // check if the arealine is one row (column is not a problem for the algorithm)

                int otherX = limitX;
                bool oneRow = true;
                bool startRepeat = false;
                bool startRepeat1 = false;
                bool startRepeat2 = false;

                int i = 0;
                foreach (int[] field in areaLine)
                {
                    int x = field[0];
                    int y = field[1];

                    if (y == minY)
                    {
                        if (circleDirectionLeft && x < otherX)
                        {
                            otherX = x;
                        }
                        else if (!circleDirectionLeft && x > otherX)
                        {
                            otherX = x;
                        }
                    }

                    if (y != minY)
                    {
                        oneRow = false;
                    }

                    if (x == limitX && y == minY && i != startIndex)
                    {
                        startRepeat = true;
                    }
                    i++;
                }

                // 9_214111, first row should not be added to start and end squares
                // crossing over the live end is only a problem if we return. An L-shape like 9_31817 is no problem.
                if (startRepeat && (!circleDirectionLeft && limitX < this.x && otherX > this.x || circleDirectionLeft && limitX > this.x && otherX < this.x) && minY == this.y - 1 && !InTakenAbs(leftField) && !InTakenAbs(rightField))
                {
                    startRepeat1 = true;
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
                    /* Test algorithm with all cases in References/countarea folder:
                     * 9_6
                     * 9_198
                     * 9_348
                     * 9_22323
                     * 2024_0611_9
                     * 2024_0611_10
                     */

                    int x = 0;
                    int y = 0;

                    for (i = 1; i < areaLine.Count; i++)
                    {
                        int index = startIndex + i;
                        if (index >= areaLine.Count)
                        {
                            index -= areaLine.Count;
                        }
                        int[] field = areaLine[index];
                        x = field[0];
                        y = field[1];
                        int[] square;

                        if (debug2) T("field x " + x + " y " + y + " currentY " + currentY + " startCandidate " + startCandidate[0] + " " + startCandidate[1] + " endCandidate " + endCandidate[0] + " " + endCandidate[1]);

                        if (y > currentY)
                        {
                            if (circleDirectionLeft)
                            {
                                // we descend first
                                if (startSquares.Count == 0 && endSquares.Count == 0)
                                {
                                    // In case of 9_214111, we add the first field to the start square. It will be removed in the end to prevent a duplicate, but an ascension needs a start field to be present.
                                    // Second condition is to prevent 9_22495 where the after the start, we step down.
                                    if (!startRepeat1 || startRepeat1 && x == limitX)
                                    {
                                        if (singleField)
                                        {
                                            startSquares.Add(startCandidate);
                                        }
                                        // 9_348, first row is walked through when we descend. But 9_198 should not add end square.
                                        else if (startCandidate[0] != endCandidate[0] && x == limitX)
                                        {
                                            startSquares.Add(startCandidate);
                                        }
                                        endSquares.Add(endCandidate);
                                    }
                                    else
                                    {
                                        startRepeat2 = true;
                                        endSquares.Add(endCandidate);
                                    }
                                }
                                else
                                {
                                    // no open peak on the bottom without having startsquares and endsquares -> descending
                                    // no closed peak on the bottom without having endsquares -> ascending
                                    // no open peak on the top without having endsquares -> ascending
                                    // no closed peak on the top without having endsquares -> descending

                                    // we have a startsquare after descending from the top row

                                    if (startSquares.Count > 0)
                                    {
                                        square = startSquares[startSquares.Count - 1];
                                        startX = square[0];
                                        startY = square[1];

                                        if (y == startY)
                                        {
                                            // open peak on the bottom
                                            if (x < startX)
                                            {
                                                startCandidate = endCandidate = field;
                                                currentY = y;
                                                continue;
                                            }
                                            else
                                            {
                                                // closed peak on the top
                                                startSquares.Add(startCandidate);
                                                endSquares.Add(endCandidate);
                                                startCandidate = endCandidate = field;
                                                currentY = y;
                                                continue;
                                            }
                                        }
                                        else if (y == startY + 1)
                                        {
                                            // first row after open peak on the bottom: stair left down or straight down
                                            if (x < startX)
                                            {
                                                endSquares.Add(endCandidate);
                                                startCandidate = endCandidate = field;
                                                currentY = y;
                                                continue;
                                            }
                                        }
                                    }

                                    square = endSquares[endSquares.Count - 1];
                                    endX = square[0];
                                    endY = square[1];

                                    if (y == endY + 1)
                                    {
                                        // closed peak on the top, preceded by and open peak
                                        if (x > endX)
                                        {
                                            startSquares.Add(startCandidate);
                                            endSquares.Add(endCandidate);
                                        }
                                    }
                                    // stair left down or straight down
                                    else
                                    {
                                        endSquares.Add(endCandidate);
                                    }
                                }
                            }
                            else
                            {
                                if (startSquares.Count == 0 && endSquares.Count == 0)
                                {
                                    // In case of 9_214111, we add the first field to the start square. It will be removed in the end to prevent a duplicate, but an ascension needs a start field to be present.
                                    // Second condition is to prevent 9_22495 where the after the start, we step down.
                                    if (!startRepeat1 || startRepeat1 && x == limitX)
                                    {
                                        if (singleField)
                                        {
                                            endSquares.Add(endCandidate);
                                        }
                                        // 9_348, first row is walked through when we descend. But 9_198 should not add end square.
                                        else if (startCandidate[0] != endCandidate[0] && x == limitX)
                                        {
                                            endSquares.Add(endCandidate);
                                        }
                                        startSquares.Add(startCandidate);
                                    }
                                    else
                                    {
                                        startRepeat2 = true;
                                        startSquares.Add(startCandidate);
                                    }
                                }
                                else
                                {
                                    // no open peak on the bottom without having endsquares and startsquares -> descending
                                    // no closed peak on the bottom without having startsquares -> ascending
                                    // no open peak on the top without having startsquares -> ascending
                                    // no closed peak on the top without having startsquares -> descending

                                    // we have a startsquare after descending from the top row

                                    if (endSquares.Count > 0)
                                    {
                                        square = endSquares[endSquares.Count - 1];
                                        endX = square[0];
                                        endY = square[1];

                                        if (y == endY)
                                        {
                                            // open peak on the bottom
                                            if (x > endX)
                                            {
                                                startCandidate = endCandidate = field;
                                                currentY = y;
                                                continue;
                                            }
                                            else
                                            {
                                                // closed peak on the top
                                                startSquares.Add(startCandidate);
                                                endSquares.Add(endCandidate);
                                                startCandidate = endCandidate = field;
                                                currentY = y;
                                                continue;
                                            }
                                        }
                                        else if (y == endY + 1)
                                        {
                                            // first row after open peak on the bottom: stair right down or straight down
                                            if (x > endX)
                                            {
                                                startSquares.Add(startCandidate);
                                                startCandidate = endCandidate = field;
                                                currentY = y;
                                                continue;
                                            }
                                        }
                                    }

                                    square = startSquares[startSquares.Count - 1];
                                    startX = square[0];
                                    startY = square[1];

                                    if (y == startY + 1)
                                    {
                                        // closed peak on the top, preceded by an open peak
                                        if (x < startX)
                                        {
                                            startSquares.Add(startCandidate);
                                            endSquares.Add(endCandidate);
                                        }
                                    }
                                    // stair right down or straight down
                                    else
                                    {
                                        startSquares.Add(startCandidate);
                                    }
                                }
                            }
                            startCandidate = endCandidate = field;
                        }
                        else if (y == currentY)
                        {
                            if (x < startCandidate[0])
                            {
                                startCandidate = field;
                            }
                            else if (x > endCandidate[0])
                            {
                                endCandidate = field;
                            }
                        }
                        else
                        {
                            if (circleDirectionLeft)
                            {
                                square = endSquares[endSquares.Count - 1];
                                endX = square[0];
                                endY = square[1];

                                if (y == endY)
                                {
                                    // open peak on the top
                                    if (x > endX)
                                    {
                                        startCandidate = endCandidate = field;
                                        currentY = y;
                                        continue;
                                    }
                                    else
                                    {
                                        // closed peak on the bottom
                                        startSquares.Add(startCandidate);
                                        endSquares.Add(endCandidate);
                                        startCandidate = endCandidate = field;
                                        currentY = y;
                                        continue;
                                    }
                                }
                                else if (y == endY - 1)
                                {
                                    // first row after an open peak on the top: stair right up or straight up
                                    if (x > endX)
                                    {
                                        startSquares.Add(startCandidate);
                                        startCandidate = endCandidate = field;
                                        currentY = y;
                                        continue;
                                    }
                                }

                                square = startSquares[startSquares.Count - 1];
                                startX = square[0];
                                startY = square[1];

                                if (y == startY - 1)
                                {
                                    // closed peak on bottom, preceded by an open peak
                                    if (x < startX)
                                    {
                                        startSquares.Add(startCandidate);
                                        endSquares.Add(endCandidate);
                                    }
                                    // first row after an open peak on the top: stair right up or straight up
                                    else
                                    {
                                        startSquares.Add(startCandidate);
                                    }
                                }
                                // stair right up or straight up
                                else
                                {
                                    startSquares.Add(startCandidate);
                                }
                            }
                            else
                            {
                                square = startSquares[startSquares.Count - 1];
                                startX = square[0];
                                startY = square[1];

                                if (y == startY)
                                {
                                    // open peak on the top
                                    if (x < startX)
                                    {
                                        startCandidate = endCandidate = field;
                                        currentY = y;
                                        continue;
                                    }
                                    else
                                    {
                                        // closed peak on the bottom
                                        startSquares.Add(startCandidate);
                                        endSquares.Add(endCandidate);
                                        startCandidate = endCandidate = field;
                                        currentY = y;
                                        continue;
                                    }
                                }
                                else if (y == startY - 1)
                                {
                                    // first row after an open peak on the top: stair left up or straight up
                                    if (x < startX)
                                    {
                                        endSquares.Add(endCandidate);
                                        startCandidate = endCandidate = field;
                                        currentY = y;
                                        continue;
                                    }
                                }

                                square = endSquares[endSquares.Count - 1];
                                endX = square[0];
                                endY = square[1];

                                if (y == endY - 1)
                                {
                                    // closed peak on bottom, preceded by an open peak
                                    if (x > endX)
                                    {
                                        startSquares.Add(startCandidate);
                                        endSquares.Add(endCandidate);
                                    }
                                }
                                // stair left up or straight up
                                else
                                {
                                    endSquares.Add(endCandidate);
                                }
                            }
                            startCandidate = endCandidate = field;
                        }
                        currentY = y;
                    }

                    //add last field
                    if (circleDirectionLeft)
                    {
                        // 2024_0611_10, checkstraight straight: we finish on the left side of the top row. End should not be added as it is the same as the first end field.

                        // 9_350: checkstraight straight: end row is a bottom row, it acts as a closed peak, so end square should be added.
                        if (x < endCandidate[0] && y == minY + 1)
                        {
                            int[] square = endSquares[endSquares.Count - 1];
                            endX = square[0];
                            endY = square[1];

                            if (y > endY)
                            {
                                endSquares.Add(endCandidate);
                            }
                        }

                        // 9_1273: L-shape, end row finishes on the right, below minY. We need to add a closed bottom peak. The last end square should be above and at the same x position or right.
                        if (x == endCandidate[0] && y == minY + 1)
                        {
                            int[] square = endSquares[endSquares.Count - 1];
                            endX = square[0];
                            endY = square[1];

                            if (y > endY && x <= endX)
                            {
                                endSquares.Add(endCandidate);
                            }
                        }

                        startSquares.Add(startCandidate);

                        if (startRepeat2)
                        {
                            endSquares.RemoveAt(0);
                        }
                    }
                    else
                    {

                        // 2024_0611_10, checkstraight straight: we finish on the left side of the top row. End should not be added as it is the same as the first end field.

                        // 9_350: checkstraight straight: end row is a bottom row, it acts as a closed peak, so end square should be added.
                        if (x > startCandidate[0] && y == minY + 1)
                        {
                            int[] square = startSquares[startSquares.Count - 1];
                            startX = square[0];
                            startY = square[1];

                            if (y > startY)
                            {
                                startSquares.Add(startCandidate);
                            }
                        }

                        // 9_1273: L-shape, end row finishes on the left, below minY. We need to add a closed bottom peak. The last end square should be above and at the same x position or left.
                        if (x == startCandidate[0] && y == minY + 1)
                        {
                            int[] square = startSquares[startSquares.Count - 1];
                            startX = square[0];
                            startY = square[1];

                            if (y > startY && x <= startX)
                            {
                                startSquares.Add(startCandidate);
                            }
                        }

                        endSquares.Add(endCandidate);

                        if (startRepeat2)
                        {
                            startSquares.RemoveAt(0);
                        }
                    }

                    startX = thisStartX;
                    startY = thisStartY;
                }

                if (debug2)
                {
                    T("circleDirectionLeft " + circleDirectionLeft + " singleField " + singleField);
                    foreach (int[] sfield in startSquares)
                    {
                        T("startsquare: " + sfield[0] + " " + sfield[1]);
                    }
                    foreach (int[] efield in endSquares)
                    {
                        T("endsquare: " + efield[0] + " " + efield[1]);
                    }
                }

                int eCount = endSquares.Count;

                // it should never happen if the above algorithm is bug-free.
                if (startSquares.Count != eCount)
                {
                    T("Count of start and end squares are inequal: " + startSquares.Count + " " + eCount);
                    foreach (int[] f in startSquares)
                    {
                        T("startSquares " + f[0] + " " + f[1]);
                    }
                    foreach (int[] f in endSquares)
                    {
                        T("endSquares " + f[0] + " " + f[1]);
                    }

                    window.errorInWalkthrough = true;
                    window.criticalError = true;
                    window.errorString = "Count of start and end squares are inequal: " + startSquares.Count + " " + eCount;
                    return false;
                }

                for (i = 0; i < eCount; i++)
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
                            int x = field[0];
                            int y = field[1];
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
                                    info = new List<object> { area % 2, pairCount, impairCount, pairFields };
                                }
                                else
                                {
                                    info = new List<object> { area % 2, impairCount, pairCount, impairFields };
                                }
                            }
                            else
                            {
                                if ((startX + startY) % 2 == 1)
                                {
                                    info = new List<object> { area % 2, pairCount, impairCount, pairFields };
                                }
                                else
                                {
                                    info = new List<object> { area % 2, impairCount, pairCount, impairFields };
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

        int[] RotateDir(int xDiff, int yDiff, int ccw)
        { // For double area, check if we can only step left
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

    }
}