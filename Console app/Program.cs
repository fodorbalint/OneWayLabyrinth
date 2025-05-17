using System.Diagnostics;

string loadFile;
int size = 0;
List<int[]> path;
List<int[]> possibleDirections;
List<int[]> possible = new(); //field coordinates
List<int[]> forbidden = new();
int x = 0, y = 0;
List<int[]> directions = new List<int[]> { new int[] { 0, 1 }, new int[] { 1, 0 }, new int[] { 0, -1 }, new int[] { -1, 0 } }; //down, right, up, left
int nextDirection = -1;
int lastDirection = -1;
long completedCount = 0;
long fileCompletedCount;
bool lineFinished = false;
long startTimerValue = 0;
long lastTimerValue = 0;
string errorString = "";
string savePath = "";
int saveFrequency;
bool makeStats;
Random rand = new Random();
int numberOfRuns = 0;
long numberOfCompleted = 0;
int statsRuns = 0;
int count = 0;
int sx = 0; //straight, left and right coordinates
int sy = 0;
int lx = 0;
int ly = 0;
int rx = 0;
int ry = 0;
int thisSx = 0; // remain constant in one step, while the above variables change for the InTakenRel calls.
int thisSy = 0;
int thisLx = 0;
int thisLy = 0;
int[] straightField;
int[] leftField;
int[] rightField;
bool CShape = false;
bool closeStraightSmall, closeMidAcrossSmall, closeAcrossSmall, closeStraightLarge, closeMidAcrossLarge, closeAcrossLarge = false;
List<object> info = new();

bool DirectionalArea, DoubleArea1, DoubleArea2, DoubleArea3, DoubleArea4, DoubleArea1Rotated, DownStairClose, DownStair = false;
bool DoubleAreaFirstCaseRotatedNext, DownStairNext = false;

int[] newExitField0 = new int[] { 0, 0 };
int[] newExitField = new int[] { 0, 0 };
bool newDirectionRotated = false; // if rotated, it is CW on left side

List<string> activeRules;
List<List<int[]>> activeRulesForbiddenFields;
List<int[]> activeRuleSizes;

int nextStepEnterLeft = -1;
int nextStepEnterRight = -1;

string baseDir = AppDomain.CurrentDomain.BaseDirectory;

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

if (File.Exists(baseDir + "settings.txt"))
{
    string[] lines = File.ReadAllLines(baseDir + "settings.txt");
    string[] arr = lines[0].Split(": ");
    size = int.Parse(arr[1]);
    arr = lines[1].Split(": ");
    saveFrequency = int.Parse(arr[1]);
    arr = lines[2].Split(": ");
    makeStats = bool.Parse(arr[1]);
    arr = lines[3].Split(": ");
    statsRuns = int.Parse(arr[1]);
}
else
{
    size = 9;
    saveFrequency = 1000000;
    makeStats = false;
    statsRuns = 100;
    string[] lines = new string[] { "size: " + size, "saveFrequency: " + saveFrequency, "makeStats: " + makeStats, "statsRuns: " + statsRuns };
    File.WriteAllLines(baseDir + "settings.txt", lines);
}

L("Size setting: " + size, "save frequency: " + saveFrequency, "make stats: " + makeStats);

ReadDir();

if (loadFile != "" && !makeStats)
{
    LoadFromFile();
}
else
{
    InitializeList();
}
count = path.Count;

bool errorInWalkthrough = false;
bool criticalError = false;

if (path != null && possibleDirections.Count == count) //null checking is only needed for removing warning
{
    if (!lineFinished)
    {
        NextStepPossibilities();

        if (errorInWalkthrough)
        {
            L("Error: " + errorString);
            Console.Read();
            return;
        }
    }
    else
    {
        possibleDirections.Add(new int[] { });
    }
}
else if (path != null && possibleDirections.Count != count + 1)
{
    L("Error in file.");
    Console.Read();
    return;
}

bool completedWalkthrough = false;
bool halfwayWalkthrough = false;

if (makeStats)
{
    numberOfRuns = 0;
    numberOfCompleted = 0;
    completedCount = 0;
    File.WriteAllText(baseDir + "log_stats.txt", "");
    string[] files = Directory.GetFiles(baseDir, "*.txt");
    foreach (string file in files)
    {
        string fileName = System.IO.Path.GetFileName(file);
        if (fileName.IndexOf("error case") != -1)
        {
            File.Delete(file);
        }
    }

}
else
{
    if (fileCompletedCount == 0 || !File.Exists(baseDir + "log_performance.txt"))
    {
        File.WriteAllText(baseDir + "log_performance.txt", "");
        File.WriteAllText(baseDir + "log_rules.txt", "");
        startTimerValue = 0;
    }
    else // continue where we left off
    {
        List<string> arr = File.ReadAllLines(baseDir + "log_performance.txt").ToList();
        List<string> newArr = new();

        foreach (string line in arr)
        {
            string[] parts = line.Split(" ");
            if (long.Parse(parts[0]) <= fileCompletedCount)
            {
                newArr.Add(line);
                startTimerValue = (long)(float.Parse(parts[1]));
            }
            else
            {
                break;
            }
        }

        if (newArr.Count > 0)
        {
            File.WriteAllLines(baseDir + "log_performance.txt", newArr);
        }
    }
    completedCount = fileCompletedCount;
    lastTimerValue = startTimerValue;
}

Stopwatch watch = Stopwatch.StartNew();

DoThread();

void DoThread()
{
    do
    {
        NextClick();
    }
    while (!completedWalkthrough && !halfwayWalkthrough && !errorInWalkthrough);

    if (completedWalkthrough)
    {
        Console.Write("\rThe number of walkthroughs are " + completedCount + ".");
        Console.Read();
    }
    else if (halfwayWalkthrough)
    {
        Console.WriteLine("\r" + completedCount + " walkthroughs are completed halfway.");
        halfwayWalkthrough = false;
        DoThread();
    }
    else
    {
        if (makeStats && !criticalError)
        {
            numberOfRuns++;
            numberOfCompleted += completedCount;
            errorInWalkthrough = false;
            Console.Write("\rIn " + numberOfRuns + " runs, average " + Math.Round((float)numberOfCompleted / numberOfRuns, 1) + " per run.                  ");
            
            // Save at every cycle for further study
            SavePath(false);

            Log("Current run: " + completedCount + ", " + numberOfCompleted + " in " + numberOfRuns + " runs. " + Math.Round((float)numberOfCompleted / numberOfRuns, 1) + " per run. " + errorString);

            if (numberOfRuns < statsRuns)
            {
                InitializeList();
                completedCount = 0;
                DoThread();
            } 
            else
            {
                Console.Read();
            }
        }
        else
        {
            Console.Write("\r\nError at " + completedCount + ": " + errorString);
            SavePath(false);
            Console.Read();
        }
    }
}

void NextClick()
{
    if (x == size && y == size)
    {
        if (makeStats)
        {
            InitializeList();
        }
        else
        {
            // step back until there is an option to move right of the step that had been taken.

            bool rightFound = false;
            nextDirection = -1;
            bool oppositeFound;
            bool leftFound;

            do
            {
                PreviousStep();

                int[] prevField;
                if (count > 2)
                {
                    prevField = path[count - 3];
                }
                else
                {
                    prevField = new int[] { 0, 1 };
                }

                int[] startField = path[count - 2];
                int[] newField = path[count - 1];
                int prevX = prevField[0];
                int prevY = prevField[1];
                int startX = startField[0];
                int startY = startField[1];
                int newX = newField[0];
                int newY = newField[1];

                int firstDir = FindDirection(startX - prevX, startY - prevY);
                int secondDir = FindDirection(newX - startX, newY - startY);

                oppositeFound = false;

                foreach (int direction in possibleDirections[count - 1]) // last but one element of possible directions
                {
                    if (direction != secondDir && direction % 2 == secondDir % 2) // two difference
                    {
                        // opposite is found, but we don't know yet if it is on the left or right side. First example 19802714.

                        if (secondDir == firstDir + 1 || firstDir == 3 && secondDir == 0)
                        { // line turned to left. The opposite direction is on the right side
                            oppositeFound = true;
                        }
                        // else line turned to right.
                    }
                    if (direction == secondDir - 1 || secondDir == 0 && direction == 3)
                    {
                        rightFound = true;
                        break;
                    }
                }

                if (rightFound) // right and maybe opposite directions exist
                {
                    nextDirection = secondDir == 0 ? 3 : secondDir - 1;
                    PreviousStep();
                }
                else if (oppositeFound) // only opposite direction
                {
                    nextDirection = secondDir < 2 ? secondDir + 2 : secondDir - 2;
                    rightFound = true;
                    PreviousStep();
                }

            } while (!rightFound && count > 2);

            if (!rightFound)
            {
                PreviousStep(); // c = 2. We reached the end, step back to the start position
                                // Reset nextDirection, so that we can start again
                completedWalkthrough = true;
                nextDirection = -1;
            }
            else if (count == 1)
            {
                halfwayWalkthrough = true;
            }
        }

        return;
    }

    if (NextStep())
    {
        if (x == size && y == size)
        {
            if (count != size * size)
            {
                possibleDirections.Add(new int[] { });

                errorInWalkthrough = true;
                errorString = "The number of steps were only " + count + ".";
                criticalError = true;
                
            }
            else
            {
                possibleDirections.Add(new int[] { });
                lineFinished = true;
                completedCount++;

                if (makeStats)
                {
                    Console.Write("\rIn " + numberOfRuns + " runs, average " + Math.Round((float)numberOfCompleted / (numberOfRuns > 0 ? numberOfRuns : 1), 1) + " per run. Current run: " + completedCount + "      ");
                }
                else
                {
                    if(completedCount % 1000 == 0)
                Console.Write("\r{0} completed.", completedCount);

                    if (completedCount % saveFrequency == 0)
                    {
                        SavePath();

                        long elapsed = watch.ElapsedMilliseconds + startTimerValue;
                        long periodValue = elapsed - lastTimerValue;
                        File.AppendAllText(baseDir + "log_performance.txt", completedCount + " " + (elapsed - elapsed % 1000) / 1000 + "." + elapsed % 1000 + " " + (periodValue - periodValue % 1000) / 1000 + "." + periodValue % 1000 + "\n");

                        lastTimerValue = elapsed;
                    }
                }
            }

            return;
        }

        NextStepPossibilities();
    }
}

bool NextStep()
{
    // L("NextStep", x, y);
    if (possible.Count == 0)
    {
        errorInWalkthrough = true;
        return false;
    }

    int[] newField = new int[] { };

    if (nextDirection != -1) // found direction after stepping back repeatedly on completion
    {
        newField = new int[] { x + directions[nextDirection][0], y + directions[nextDirection][1] };
        lastDirection = nextDirection;
        nextDirection = -1;
    }
    else
    {
        if (makeStats)
        {
            newField = possible[rand.Next(0, possible.Count)];
        }
        else // Find the most left field. It is possible to have the left and right field but not straight.
        {
            int[] newDirections = possibleDirections[possibleDirections.Count - 1];

            bool foundLeft = false;
            bool foundStraight = false;
            int i = 0;
            for (i = 0; i < newDirections.Length; i++)
            {
                int leftDirection = lastDirection == 3 ? 0 : lastDirection + 1;
                if (newDirections[i] == leftDirection)
                {
                    foundLeft = true;
                    break;
                }
                else if (newDirections[i] == lastDirection)
                {
                    foundStraight = true;
                    //no break, left may be found later
                }
            }

            int newDirectionTemp = -1;
            if (foundLeft)
            {
                newDirectionTemp = newDirections[i];
                newField = new int[] { x + directions[newDirectionTemp][0], y + directions[newDirectionTemp][1] };
            }
            else if (foundStraight)
            {
                newDirectionTemp = lastDirection;
                newField = new int[] { x + directions[newDirectionTemp][0], y + directions[newDirectionTemp][1] };
            }
            else //only right is possible
            {
                newDirectionTemp = newDirections[0];
                newField = new int[] { x + directions[newDirectionTemp][0], y + directions[newDirectionTemp][1] };
            }
            lastDirection = newDirectionTemp;
        }
    }

    x = newField[0];
    y = newField[1];
    path.Add(new int[] { x, y });
    count = path.Count;

    return true;
}

void NextStepPossibilities()
{
    try
    {
        NextStepPossibilities2();

        List<int> possibleFields = new List<int>();
        List<int[]> newPossible = new List<int[]>();

        if (errorInWalkthrough) // countarea errors
        {
            possible = newPossible;
            possibleDirections.Add(possibleFields.ToArray());

            return;
        }

        foreach (int[] field in possible)
        {
            int fx = field[0];
            int fy = field[1];

            newPossible.Add(field);

            for (int i = 0; i < 4; i++)
            {
                //last movement: down, right, up, left
                int dx = directions[i][0];
                int dy = directions[i][1];

                if (fx - x == dx && fy - y == dy)
                {
                    possibleFields.Add(i);
                }
            }
        }

        possible = newPossible;
        possibleDirections.Add(possibleFields.ToArray()); //array containing possible fields for all steps

        if (possible.Count == 0)
        {
            errorInWalkthrough = true;
            errorString = "No option to move.";
        }

        //Stop at pattern here

        /*if (!path.countAreaImpair && path.FutureL)
        {
            M("Future L: " + completedCount + " walkthroughs are completed.", 3);
            if (isTaskRunning)
            {
                Dispatcher.Invoke(() =>
                {
                    DrawPath();
                });
            }
        }*/
    }
    catch (Exception ex)
    {
        errorInWalkthrough = true;
        errorString = ex.Message + " " + ex.StackTrace;
        criticalError = true;
    }
}

void NextStepPossibilities2()
{
    try
    {
        // L("NextStepPossibilities2", x, y);
        possible = new List<int[]>();
        forbidden = new List<int[]>();
        List<int[]> newPossible;

        if (count < 2)
        {
            possible.Add(new int[] { 2, 1 });
            possible.Add(new int[] { 1, 2 });
        }
        else
        {
            int x0 = path[count - 2][0];
            int y0 = path[count - 2][1];

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

                    if (possible.Count == 1) break;

                    activeRules = new();
                    activeRulesForbiddenFields = new();
                    activeRuleSizes = new();

                    // ----- copy start -----
                    nextStepEnterLeft = -1;
                    nextStepEnterRight = -1;

                    closedCorners = new List<int[]>[] { new List<int[]>(), new List<int[]>(), new List<int[]>(), new List<int[]>() };
                    openCWCorners = new List<int[]>[] { new List<int[]>(), new List<int[]>(), new List<int[]>(), new List<int[]>() };
                    openCCWCorners = new List<int[]>[] { new List<int[]>(), new List<int[]>(), new List<int[]>(), new List<int[]>() };

                    // needs to be checked before AreaUp, it can overwrite it as in 802973
                    CornerDiscoveryAll();

                    // T("CheckCShapeNext");
                    CheckCShapeNext();
                    // T("CheckStraight " + ShowForbidden());
                    CheckStraight();
                    // T("CheckLeftRightAreaUp " + ShowForbidden());
                    CheckLeftRightAreaUp();
                    // T("CheckLeftRightCorner " + ShowForbidden());
                    CheckLeftRightCorner();
                    // T("Forbidden: " + ShowForbidden());

                    // T("NextStepEnter " + nextStepEnterLeft + " " + nextStepEnterRight);

                    // 0611_4, 0611_5, 0611_6, 234212, 522267
                    // 0 and 0 or 1 and 3. Beware of 1 and -1.
                    // Overwrite order: 3, 0, 1 (See 802973 and 2020799)
                    if (nextStepEnterLeft == 0 && nextStepEnterRight == 0 || nextStepEnterLeft + nextStepEnterRight == 4 && Math.Abs(nextStepEnterLeft - nextStepEnterRight) == 2)
                    {
                        switch (nextStepEnterLeft)
                        {
                            case 0:
                                // T("Next step double area, cannot step straight");
                                AddForbidden(0, 1);
                                break;
                            case 1:
                                // T("Next step double area, cannot step right");
                                AddForbidden(-1, 0);
                                break;
                            case 3:
                                // T("Next step double area, cannot step left");
                                AddForbidden(1, 0);
                                break;
                        }
                    }

                    // T("CheckLeftRightAreaUpExtended " + ShowForbidden());
                    CheckLeftRightAreaUpExtended(); // #1 close obstacle is at the end of the area, outside.
                    //T("CheckStairArea " + ShowForbidden());
                    /*CheckStairArea();
                    // T("CheckStairAtStart " + ShowForbidden());*/
                    CheckStairAtStart();
                    // T("CheckStairAtStartEqual " + ShowForbidden());
                    CheckStairAtStartEqual();
                    // T("CheckStairAtEndConvex " + ShowForbidden());
                    CheckStairAtEndConvex(); // 0718, reverse stair 1/2, 0720_2, 0731: 3 obstacles 
                    // T("CheckStairAtEndConvexStraight3 " + ShowForbidden());
                    CheckStairAtEndConvexStraight3();
                    // T("CheckStairAtEndConcave5 " + ShowForbidden());
                    CheckStairAtEndConcave5(); // 0814
                    // T("CheckStairAtEndConcave6 " + ShowForbidden());
                    CheckStairAtEndConcave6(); // 0714
                    // T("CheckStairAtEnd3Obtacles1 " + ShowForbidden());
                    CheckStairAtEnd3Obtacles1(); // 0725_4, 0731 - 0808
                    // T("CheckStairAtEnd3Obtacles2 " + ShowForbidden());
                    CheckStairAtEnd3Obtacles2(); // 0805, 0808

                    // T("CheckStartObstacleInside " + ShowForbidden());
                    CheckStartObstacleInside();
                    // T("CheckStraightSmall " + ShowForbidden());
                    CheckStraightSmall(); // #3 close obstacle is at the start and end of the area, inside. 4 distance only.
                    // T("CheckLeftRightAreaUpBigExtended " + ShowForbidden());
                    CheckLeftRightAreaUpBigExtended(); // #4 when entering at the first white field, we have to step down to the first black and then left to enter as in 0624
                                                       //T("CheckStraightBig " + ShowForbidden());
                                                       //CheckStraightBig(); // #7 close obstacle is at the start and end of the area, outside. 4 distance only.                                

                    // T("Forbidden: " + ShowForbidden());

                    List<int[]> startForbiddenFields = Copy(forbidden);
                    // If distance is over 3, single area rules seem to disable the needed directions. For 3 distance, we use Sequence first case.

                    /** //T("CheckDirectionalArea");
                    CheckDirectionalArea();
                    // T("Check3DoubleArea");
                    Check3DoubleArea(); **/

                    // T("CheckSequence " + ShowForbidden());
                    CheckSequence();
                    // T("CheckNearStair " + ShowForbidden());
                    CheckNearStair();
                    //T("CheckDoubleStair " + ShowForbidden());
                    //CheckDoubleStair();
                    //T("CheckSideStair " + ShowForbidden());
                    //CheckSideStair();
                    // T("CheckSideStairStraight " + ShowForbidden());
                    // CheckSideStairStraight(); -> Sequence 2
                    // T("Check3x3Stair " + ShowForbidden());
                    Check3x3Stair();
                    // T("CheckSequence2 " + ShowForbidden());
                    CheckSequence2();
                    // T("CheckRemoteStair " + ShowForbidden());
                    CheckRemoteStair();

                    // T("Forbidden: " + ShowForbidden());
                    // ----- copy end -----

                    if (!makeStats)
                    {
                        ShowActiveRules(activeRules, activeRulesForbiddenFields, startForbiddenFields, activeRuleSizes);
                    }
                    break;
                }
            }

            newPossible = new();

            foreach (int[] field in possible)
            {
                if (!InForbidden(field))
                {
                    newPossible.Add(field);
                }
            }
            possible = newPossible;
        }
    }
    catch (Exception ex)
    {
        errorInWalkthrough = true;
        errorString = ex.Message + " " + ex.StackTrace;
        criticalError = true;
    }
}

void PreviousStep()
{
    if (count < 2) return;

    int removeX = path[count - 1][0];
    int removeY = path[count - 1][1];

    errorInWalkthrough = false;
    lineFinished = false;

    x = path[count - 2][0];
    y = path[count - 2][1];

    path.RemoveAt(count - 1);
    possibleDirections.RemoveAt(count);

    count = path.Count;

    possible = new List<int[]>();
    List<int> dirs = possibleDirections[possibleDirections.Count - 1].ToList<int>();

    foreach (int dir in dirs)
    {
        int newX = x + directions[dir][0];
        int newY = y + directions[dir][1];

        possible.Add(new int[] { newX, newY });
    }
}

void SavePath(bool isCompleted = true) // used in fast run mode
{
    int startX = 1;
    int startY = 1;
    string completedPathCode = "";
    int lastDrawnDirection = 0;
    savePath = size + "|1-" + startX + "," + startY + ";";

    for (int i = 1; i < count; i++)
    {
        int[] field = path[i];
        int newX = field[0];
        int newY = field[1];

        foreach (int direction in possibleDirections[i])
        {
            savePath += direction + ",";
        }
        savePath = savePath.Substring(0, savePath.Length - 1);
        savePath += "-" + newX + "," + newY + ";";

        if (isCompleted)
        {
            for (int j = 0; j < 4; j++)
            {
                if (directions[j][0] == newX - startX && directions[j][1] == newY - startY)
                {
                    if (possibleDirections[i].Length > 1)
                    {
                        if (j == lastDrawnDirection) //stepped straight. We need to check if there is a left or right field in the possibilities
                        {
                            bool leftFound = false;
                            bool rightFound = false;
                            foreach (int direction in possibleDirections[i])
                            {
                                if (direction == j + 1 || j == 3 && direction == 0) leftFound = true;
                                if (direction == j - 1 || j == 0 && direction == 3) rightFound = true;
                            }

                            if (leftFound && rightFound) // straight
                            {
                                completedPathCode += "b";
                            }
                            else if (leftFound) // right
                            {
                                completedPathCode += "c";
                            }
                            else // left
                            {
                                completedPathCode += "a";
                            }
                        }
                        else if (j == lastDrawnDirection + 1 || lastDrawnDirection == 3 && j == 0) completedPathCode += "a"; // left
                        else completedPathCode += "c"; // right
                    }

                    lastDrawnDirection = j;
                }
            }
        }

        startX = newX;
        startY = newY;
    }

    if (possibleDirections.Count > count)
    {
        foreach (int direction in possibleDirections[possibleDirections.Count - 1])
        {
            savePath += direction + ",";
        }
    }
    savePath = savePath.Substring(0, savePath.Length - 1);

    ReadDir();

    if (isCompleted)
    {
        File.WriteAllText(baseDir + "completed/" + completedCount + "_" + completedPathCode + ".txt", savePath);
    }
    else
    {
        if (!makeStats && !errorInWalkthrough)
        {
            string path = "incomplete/";

            if (File.Exists(baseDir + path + completedCount + ".txt"))
            {
                int i = 1;
                while (File.Exists(baseDir + path + completedCount + "_" + i + ".txt"))
                {
                    i++;
                }
                File.WriteAllText(baseDir + path + completedCount + "_" + i + ".txt", savePath);
            }
            else
            {
                File.WriteAllText(baseDir + path + completedCount + ".txt", savePath);
            }
        }
        else if (!makeStats)
        {
            File.WriteAllText(baseDir + completedCount + ".txt", savePath);
        }
        else
        {
            if (File.Exists(baseDir + "error case 1.txt"))
            {
                int i = 2;
                while (File.Exists(baseDir + "error case " + i + ".txt"))
                {
                    i++;
                }
                File.WriteAllText(baseDir + "error case " + i + ".txt", savePath);
            }
            else
            {
                File.WriteAllText(baseDir + "error case 1.txt", savePath);
            }
        }
    }
}

void ReadDir()
{
    loadFile = "";
    string[] files = Directory.GetFiles(baseDir, "*.txt");
    foreach (string file in files)
    {
        string fileName = System.IO.Path.GetFileName(file);
        if (fileName != "settings.txt" && fileName != "log_stats.txt" && fileName != "log_rules.txt" && fileName != "log_performance.txt" && fileName != "completedPaths.txt" && fileName.IndexOf("_temp") == -1 && fileName.IndexOf("_error") == -1)
        {
            loadFile = fileName;
            return;
        }
    }
}

void LoadFromFile()
{
    string content = File.ReadAllText(baseDir + loadFile);
    string[] loadPath;

    if (content.IndexOf("|") != -1)
    {
        string[] arr = content.Split("|");
        size = int.Parse(arr[0]);
        CheckSize();
        content = arr[1];
    }

    loadPath = content.Split(";");

    path = new();
    possibleDirections = new();

    if (content.IndexOf("-") != -1) // normal mode, with possibilities
    {
        foreach (string coords in loadPath)
        {
            string[] sections = coords.Split("-");
            int[] possibles = Array.ConvertAll(sections[0].Split(","), s => int.Parse(s));
            possibleDirections.Add(possibles);
            if (sections.Length == 2)
            {
                int[] field = Array.ConvertAll(sections[1].Split(","), s => int.Parse(s));
                path.Add(field);
                x = field[0];
                y = field[1];
            }
        }
    }
    else // only coordinates
    {
        int startX = 0;
        int startY = 1;

        foreach (string coords in loadPath)
        {
            int[] field = Array.ConvertAll(coords.Split(","), s => int.Parse(s));
            path.Add(field);
            x = field[0];
            y = field[1];

            possibleDirections.Add(new int[] { FindDirection(x - startX, y - startY) });
            startX = x;
            startY = y;
        }
        possibleDirections.Add(new int[] { });
    }

    L("Loading", loadFile, "path count: " + path.Count, "possible count: " + possibleDirections.Count);

    nextDirection = -1;

    if (path.Count > 1)
    {
        int[] prevField = path[path.Count - 2];
        int prevX = prevField[0];
        int prevY = prevField[1];
        for (int i = 0; i < 4; i++)
        {
            //last movement: down, right, up, left
            int dx = directions[i][0];
            int dy = directions[i][1];

            if (x - prevX == dx && y - prevY == dy)
            {
                lastDirection = i;
            }
        }
    }
    else lastDirection = 0;

    fileCompletedCount = 0;
    if (loadFile.IndexOf("_") > 0)
    {
        string[] arr = loadFile.Split("_");
        arr[1] = arr[1].Replace(".txt", "");
        if (!int.TryParse(arr[1], out int result))
        {
            fileCompletedCount = long.Parse(arr[0]);
        }
    }

    if (x == size && y == size)
    {
        lineFinished = true;
    }
    else
    {
        lineFinished = false;

        possible = new();
        foreach (int direction in possibleDirections[possibleDirections.Count - 1])
        {
            possible.Add(new int[] { x + directions[direction][0], y + directions[direction][1] });
        }
    }
}

void InitializeList()
{
    path = new List<int[]> { new int[] { 1, 1 } };
    x = 1;
    y = 1;
    possibleDirections = new List<int[]> { new int[] { 1 }, new int[] { 0, 1 } };
    nextDirection = -1;
    lastDirection = 0;
    fileCompletedCount = 0;

    possible = new();
    foreach (int direction in possibleDirections[possibleDirections.Count - 1])
    {
        possible.Add(new int[] { x + directions[direction][0], y + directions[direction][1] });
    }

    lineFinished = false;
}   

int FindDirection(int xDiff, int yDiff)
{
    for (int i = 0; i < 4; i++)
    {
        if (directions[i][0] == xDiff && directions[i][1] == yDiff)
        {
            return i;
        }
    }
    return 0;
}

void CheckSize()
{
    if (size > 99)
    {
        L("Size should be between 3 and 99.");
        size = 99;
    }
    else if (size < 3)
    {
        L("Size should be between 3 and 99.");
        size = 3;
    }
    else if (size % 2 == 0)
    {
        L("Size cannot be pair.");
        size = size - 1;
    }
}

void T(params object[] o)
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
    Debug.WriteLine(result);
}

void L(params object[] o)
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
    Console.WriteLine(result);
}

void Log(string line)
{
    File.AppendAllText(baseDir + "log_stats.txt", line + "\n");
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

void ShowActiveRules(List<string> rules, List<List<int[]>> forbiddenFields, List<int[]> startForbiddenFields, List<int[]> sizes)
{
    if (rules.Count == 0) return;

    int i;

    // only record new rule when its forbidden fields were not created by other rules. More complicated rules can be true together with simpler rules that already added the necessary forbidden fields, like in 349170
    int ruleNo = 0;

    if (File.Exists(baseDir + "log_rules.txt"))
    {
        List<string> arr = File.ReadAllLines(baseDir + "log_rules.txt").ToList();
        int startCount = arr.Count;

        foreach (string rule in rules)
        {
            bool found = false;

            foreach (string line in arr)
            {
                string[] split = line.Split(": ");
                if (split[1] == rule) found = true;
            }

            if (!found)
            {
                List<int[]> newRuleForbiddenFields = forbiddenFields[ruleNo];

                // only remove the same fields as what is contained in startforbiddenFields.
                // other rules can be true simultaneously.

                foreach (int[] field in startForbiddenFields)
                {
                    for (int j = newRuleForbiddenFields.Count - 1; j >= 0; j--)
                    {
                        int[] newField = newRuleForbiddenFields[j];
                        if (field[0] == newField[0] && field[1] == newField[1])
                        {
                            newRuleForbiddenFields.RemoveAt(j);
                        }
                    }
                }

                // the unique forbidden field(s) has to be empty
                if (newRuleForbiddenFields.Count != 0)
                {
                    foreach (int[] field in newRuleForbiddenFields)
                    {
                        if (!InTaken(field[0], field[1]))
                        {
                            arr.Add((numberOfCompleted + completedCount) + ": " + rule);
                            // if two different positions of the same path number are saved, there will be appended _1, _2 etc.
                            SavePath(false);
                            break;
                        }
                    }
                }
            }

            ruleNo++;
        }

        if (arr.Count > startCount) File.WriteAllLines(baseDir + "log_rules.txt", arr);
    }
    else
    {
        List<string> arr = new();
        foreach (string rule in rules)
        {
            List<int[]> newRuleForbiddenFields = forbiddenFields[ruleNo]; // does not copy, it is a reference assignment

            foreach (int[] field in startForbiddenFields)
            {
                for (int j = newRuleForbiddenFields.Count - 1; j >= 0; j--)
                {
                    int[] newField = newRuleForbiddenFields[j];
                    if (field[0] == newField[0] && field[1] == newField[1])
                    {
                        newRuleForbiddenFields.RemoveAt(j);
                    }
                }
            }

            if (newRuleForbiddenFields.Count != 0)
            {
                foreach (int[] field in newRuleForbiddenFields)
                {
                    if (!InTaken(field[0], field[1]))
                    {
                        arr.Add((numberOfCompleted + completedCount) + ": " + rule);
                        SavePath(false);
                        break;
                    }
                }
            }

            ruleNo++;
        }
        File.WriteAllLines(baseDir + "log_rules.txt", arr);
    }
}

/* ----- Rules ----- */

// ----- copy start -----
void CheckCShapeNext() // 0611_5, 0611_6
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
                        // T("Close straight", i, j);
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
                                    if (j < 2 && whiteDiff == nowWCount) // 0715
                                    {
                                        if (CheckNearFieldSmallRel0(0, 2, 1, 1, false))
                                        {
                                            ruleTrue = true;
                                            // T("CheckStraight % 4 = 1 start obstacle: Cannot step straight");
                                            AddForbidden(0, 1);
                                        }
                                    }
                                    break;
                                case 2:
                                    nowWCount = (ex - 2) / 4; // At 6 distance, if we step straight and exit, the 5 distance situation remain with 3 black and 2 white fields. Another white to white line is not possible. 0610_6
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
                                // T("Straight " + i + " " + j + ": Cannot enter now up");
                                AddForbidden(0, 1);
                            }
                            if (!(whiteDiff <= nowWCountLeft && whiteDiff >= -nowBCountLeft) && j != 1)  // for left rotation, lx, ly is the down field
                            {
                                ruleTrue = true;
                                // T("Straight " + i + " " + j + ": Cannot enter now left");
                                AddForbidden(1, 0);
                                if (j == 2)
                                {
                                    AddForbidden(0, -1);
                                }
                            }
                            if (!(whiteDiff <= laterWCount && whiteDiff >= -laterBCount) && j != 2)
                            {
                                ruleTrue = true;
                                // T("Straight " + i + " " + j + ": Cannot enter later");
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
                    // T("AreaUp distance " + (dist - 1), "side " + i, "rotation " + j);

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
                                // T("Close mid across", i, j);
                                AddForbidden(0, 1);
                                if (j == 0)
                                {
                                    AddForbidden(-1, 0);
                                }

                                // only one option remains, but we do not return in case of 0623 where the area would close, and at the end, the number of steps are less than size * size.
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
                                                // T("LeftRightAreaUp: Cannot enter now left");
                                                if (j == 1)
                                                {
                                                    // T("LeftRightAreaUp: Cannot enter now down");
                                                    AddForbidden(0, -1);
                                                }
                                            }
                                        }
                                    }
                                    if (!(whiteDiff <= laterWCount && whiteDiff >= -laterBCount))
                                    {
                                        ruleTrue = true;
                                        // T("LeftRightAreaUp: Cannot enter later");
                                        AddForbidden(0, 1);
                                        AddForbidden(-1, 0);
                                    }
                                    else if (j != 2) // We can enter later, check for start C on the opposite side (if the obstacle is up on the left, we check the straight field for next step C, not the right field.) 
                                                     // 466
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
                                // T("Close mid across big", i, j);
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
                                        // T("LeftRightAreaUpBig: Cannot enter now up");
                                        AddForbidden(0, 1);
                                    }
                                    if (!(whiteDiff <= nowWCountRight && whiteDiff >= -nowBCount)) // not in range
                                    {
                                        ruleTrue = true;
                                        // T("LeftRightAreaUpBig: Cannot enter now right");
                                        AddForbidden(-1, 0);
                                    }
                                    if (!(whiteDiff <= laterWCount && whiteDiff >= -laterBCount))
                                    {
                                        ruleTrue = true;
                                        // T("LeftRightAreaUpBig: Cannot enter later");
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

        for (int j = 0; j < 4; j++)
        {
            int quarter = quarters[i][j];
            foreach (int[] corner in closedCorners[quarter])
            {
                int hori = j % 2 == 0 ? corner[0] : corner[1];
                int vert = j % 2 == 0 ? corner[1] : corner[0];

                if (!(hori == 1 || vert == 1)) // this case is handled in AreaUp
                {
                    // T("Corner at " + hori, vert, "side " + i, "rotation " + j);

                    int i1 = InTakenIndexRel(hori, vert);
                    int i2 = InTakenIndexRel(hori + 1, vert);

                    if (i2 > i1)
                    {
                        if (hori == 2 && vert == 2) // close across, small if j = 0, big if j = 1
                        {
                            AddForbidden(0, 1);
                            if (j == 0) // close across small
                            {
                                // T("Close across small", i);
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
                                // T("Close across big", i);
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
                                        if (vert % 4 == 3 && j < 2) // 0610, 0610_1, #6 0625_1, 0611_3 (21 cutout)
                                        {
                                            if (-whiteDiff == (vert - 3) / 4)
                                            {
                                                if (CheckCorner1(1, 2, 0, 2, circleDirectionLeft, true))
                                                {
                                                    ruleTrue = true;
                                                    // T("LeftRightCorner closed corner 2, 3: Cannot step left");
                                                    AddForbidden(1, 0);
                                                    if (j == 1) // big area
                                                    {
                                                        // T("LeftRightCorner closed corner 2, 3: Cannot step down");
                                                        AddForbidden(0, -1);
                                                    }
                                                }
                                            }
                                        }

                                        else if (vert % 4 == 0 && j <= 1)  // 743059_1, 0610_2, 0610_3, #5 0625
                                        // These above cases are solved by the y = x + 2 return stair pattern too. But this algorithm can be applied to a straight extension as well.
                                        {
                                            if (-whiteDiff == vert / 4)
                                            {
                                                // Add field so that a second circle can be drawn
                                                path.Add(new int[] { x + 2 * lx + (vert - 1) * sx, y + 2 * ly + (vert - 1) * sy });

                                                if (CheckCorner1(1, vert - 2, 0, 2, circleDirectionLeft, true))
                                                {
                                                    path.RemoveAt(path.Count - 1);
                                                    ruleTrue = true;
                                                    // T("LeftRightCorner open corner 2, 4: Cannot step left");
                                                    AddForbidden(1, 0);
                                                    if (j == 1)
                                                    {
                                                        // T("LeftRightCorner open corner 2, 4: Cannot step down");
                                                        AddForbidden(0, -1);
                                                    }
                                                }
                                                else
                                                {
                                                    path.RemoveAt(path.Count - 1);
                                                }

                                                /*
                                                // 0726, sequence on right side

                                                ResetExamAreas();

                                                counterrec = 0;

                                                lx2 = -lx2;
                                                ly2 = -ly2;
                                                if (CheckSequenceRecursive(1 - i))
                                                {
                                                    AddExamAreas(true);

                                                    // T("Corner 2 4 Sequence at " + x + " " + y + ", stop at " + x2 + " " + y2 + ": Cannot step left");
                                                    AddForbidden(1, 0);
                                                    if (j == 1)
                                                    {
                                                        // T("Corner 2 4 Sequence: Cannot step down");
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
                                            // 0720_3: mid across, 0725_1: across
                                            // Find example for area
                                            if (CheckNearFieldSmallRel1(hori - 2, 1, 1, 0, true))
                                            {
                                                ruleTrue = true;
                                                // T("LeftRightCorner 4 2 1B: Cannot step left");
                                                AddForbidden(1, 0);
                                                if (j == 1)
                                                {
                                                    // T("LeftRightCorner 4 2 1B: Cannot step down");
                                                    AddForbidden(0, -1);
                                                }
                                            }

                                            /*
                                            // 0711, sequence on left side
                                            path.Add(new int[] { x + (hori - 1) * lx + sx, y + (hori - 1) * ly + sy });

                                            x2 = x + (hori - 1) * lx + sx;
                                            y2 = y + (hori - 1) * ly + sy;

                                            lx2 = lx;
                                            ly2 = ly;
                                            sx2 = sx;
                                            sy2 = sy;

                                            ResetExamAreas();

                                            counterrec = 0;

                                            if (CheckSequenceRecursive(i)) // 0711
                                            {
                                                path.RemoveAt(path.Count - 1);

                                                AddExamAreas(true);

                                                // T("Corner 4 2 Sequence at " + x + " " + y + ", stop at " + x2 + " " + y2 + ": Cannot step left");
                                                AddForbidden(1, 0);
                                                if (j == 1)
                                                {
                                                    // T("Corner 4 2 Sequence: Cannot step down");
                                                    AddForbidden(0, -1);
                                                }
                                            }
                                            else
                                            {
                                                path.RemoveAt(path.Count - 1);
                                            }
                                            */
                                        }

                                        // 0727_1: mid across
                                        if (hori % 4 == 2 && j < 2 && whiteDiff == (hori - 2) / 4 && CheckNearFieldSmallRel0(2, 2, 1, 0, false))
                                        {
                                            ruleTrue = true;
                                            // T("Corner hori % 4 = 2 vert 2 start obstacle: Cannot step straight");
                                            AddForbidden(0, 1);
                                            if (j == 0)
                                            {
                                                // T("Corner hori % 4 = 2 vert 2 start obstacle: Cannot step right");
                                                AddForbidden(-1, 0);
                                            }
                                        }
                                    }


                                    // Stair extensions: 2, 3 or 4 fields on the top near the live end
                                    if (vert == hori + 1 && -whiteDiff == hori - 2 && j <= 1) // 0712
                                    {
                                        int m;
                                        for (m = hori - 1; m >= 2; m--)
                                        {
                                            path.Add(new int[] { x + m * lx + (m + 1) * sx, y + m * ly + (m + 1) * sy });
                                        }

                                        if (CheckCorner1(1, 2, 0, 2, circleDirectionLeft, true))
                                        {
                                            for (m = hori - 1; m >= 2; m--)
                                            {
                                                path.RemoveAt(path.Count - 1);
                                            }

                                            AddExamAreas();
                                            // T("Corner y = x + 1 return stair close obstacle: Cannot step left");
                                            AddForbidden(1, 0);
                                            if (j == 1)
                                            {
                                                // T("Corner y = x + 1 return stair close obstacle: Cannot step down");
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

                                    if (vert == hori + 2 && -whiteDiff == hori - 1 && j <= 1) // Close mid across: 743059_1, 0610_2, 0610_3; Close across: 0716_1, Area: 0625, 0720_1
                                    // stair entered from side
                                    // obstacle at any point of the return step?
                                    {
                                        int m;
                                        for (m = hori; m >= 2; m--)
                                        {
                                            path.Add(new int[] { x + m * lx + (m + 1) * sx, y + m * ly + (m + 1) * sy });
                                        }

                                        if (CheckCorner1(1, 2, 0, 2, circleDirectionLeft, true))
                                        {
                                            for (m = hori; m >= 2; m--)
                                            {
                                                path.RemoveAt(path.Count - 1);
                                            }

                                            AddExamAreas();
                                            // T("Corner y = x + 2 return stair close obstacle: Cannot step left");
                                            AddForbidden(1, 0);
                                            if (j == 1)
                                            {
                                                // T("Corner y = x + 2 return stair close obstacle: Cannot step down");
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

                                    if (vert == hori + 3 && -whiteDiff == hori - 1 && j == 3) // 0717, 0717_3 (far obstacle)
                                                                                              // stair entered from below
                                    {
                                        int m;
                                        for (m = hori - 1; m >= 1; m--)
                                        {
                                            path.Add(new int[] { x + m * lx + (m + 3) * sx, y + m * ly + (m + 3) * sy });
                                        }

                                        if (CheckCorner1(0, 3, 0, 2, circleDirectionLeft, true))
                                        {
                                            for (m = hori - 1; m >= 1; m--)
                                            {
                                                path.RemoveAt(path.Count - 1);
                                            }

                                            ruleTrue = true;
                                            // T("Corner y = x + 3 return stair second obstacle: Cannot step up");
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
                                    if (hori == vert + 3 && -whiteDiff == 1) // 0725_6, corner 2 5 stair (shows large area)
                                    {
                                        int m;
                                        for (m = 1; m <= vert; m++)
                                        {
                                            path.Add(new int[] { x + m * lx + (m - 1) * sx, y + m * ly + (m - 1) * sy });
                                        }
                                        m--;

                                        if (CheckNearFieldSmallRel1(hori - 2, vert, 1, 0, true))
                                        // Example needed
                                        // if (CheckCorner1(hori - 2, vert, 1, 0, circleDirectionLeft, true))
                                        {
                                            for (m = 1; m <= vert; m++)
                                            {
                                                path.RemoveAt(path.Count - 1);
                                            }

                                            ruleTrue = true;
                                            // T("Corner x = y + 3 up left stair second obstacle: Cannot step left");
                                            AddForbidden(1, 0);
                                            if (j == 1)
                                            {
                                                // T("Corner x = y + 3 up left stair second obstacle: Cannot step down");
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

                                    if (!(whiteDiff <= nowWCount && whiteDiff >= -nowBCount) && j != 3) // for left rotation, lx, ly is the down field
                                    {
                                        ruleTrue = true;
                                        // T("LeftRightCorner " + i + " " + j + ": Cannot enter now left");
                                        AddForbidden(1, 0);
                                    }
                                    if (!(whiteDiff <= nowWCountDown && whiteDiff >= -nowBCount) && j != 3)
                                    {
                                        ruleTrue = true;
                                        // T("LeftRightCorner " + i + " " + j + ": Cannot enter now down");
                                        AddForbidden(0, -1);
                                    }
                                    if (!(whiteDiff <= laterWCount && whiteDiff >= -laterBCount))
                                    {
                                        ruleTrue = true;
                                        // T("LeftRightCorner " + i + " " + j + ": Cannot enter later");
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
                                            // 0611_6
                                            // If we can enter later at the hori 2, vert 3 case, the area must be W = B
                                            if (
                                                (hori == 2 && vert == 3) ||
                                                (hori == 2 && vert == 4 && -whiteDiff == 1) ||
                                                (hori == 3 && vert == 4 && -whiteDiff == 1)) // 0726_3
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
                    // T("AreaUpExtended distance " + (dist - 1), "side " + i, "rotation " + j);

                    bool distanceEmpty = true;
                    for (int k = 1; k <= dist - 1; k++)
                    {
                        if (k == 1) // As 0618_2 shows, 1,1 can be taken
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
                                        // 0610_4, 0610_5: across
                                        // 121670752: mid across
                                        // 0627: area
                                        if (-whiteDiff == ex / 4 && (j == 0 || j == 3))
                                        {
                                            // Add field so that a second circle can be drawn
                                            path.Add(new int[] { x + lx + ex * sx, y + ly + ex * sy });

                                            if (CheckCorner1(0, ex - 1, 0, 2, circleDirectionLeft, true))
                                            {
                                                path.RemoveAt(path.Count - 1);

                                                ruleTrue = true;
                                                // T("LeftRightAreaUpExtended open corner 4: Cannot step straight");
                                                AddForbidden(0, 1);
                                                // stepping left is already disabled
                                            }
                                            else
                                            {
                                                path.RemoveAt(path.Count - 1);
                                            }
                                        }
                                        // 0618_2: end obstacle (across)
                                        // 0725: double obstacle outside 
                                        else if (whiteDiff == ex / 4)
                                        {
                                            if (CheckNearFieldSmallRel1(0, ex, 0, 2, true))
                                            {
                                                ruleTrue = true;
                                                // T("LeftRightAreaUpExtended closed corner 4: Cannot step right");
                                                AddForbidden(-1, 0);
                                                if (j == 3)
                                                {
                                                    // T("LeftRightAreaUpExtended closed corner 4: Cannot step down");
                                                    AddForbidden(0, -1);
                                                }

                                                // 0725: double obstacle outside, 2 x mid across
                                                // 0727_4: up mid across, down across
                                                if (CheckNearFieldSmallRel1(0, 2, 1, 1, false))
                                                {
                                                    // T("LeftRightAreaUpExtended 4 dist double obstacle outside: Cannot step straight");
                                                    AddForbidden(0, 1);
                                                }
                                            }
                                        }
                                        break;
                                    case 1:
                                        // 0626, across
                                        if (whiteDiff == (ex + 3) / 4 && CheckNearFieldSmallRel1(0, ex - 1, 0, 2, true))
                                        {
                                            ruleTrue = true;
                                            // T("LeftRightAreaUpExtended open corner 5: Cannot step right");
                                            AddForbidden(-1, 0);

                                            if (j == 3)
                                            {
                                                // T("LeftRightAreaUpExtended open corner 5: Cannot step down");
                                                AddForbidden(0, -1);
                                            }

                                            // 0727_3: double obstacle outside: mid across x 2 
                                            if (CheckNearFieldSmallRel0(0, 2, 1, 1, false))
                                            {
                                                // T("LeftRightAreaUpExtended 5 dist double obstacle outside: Cannot step straight");
                                                AddForbidden(0, 1);
                                            }
                                        }
                                        break;
                                    case 2:
                                        // We cannot get to the 2- or 6-distance case if the other rules are applied. 0611_1
                                        break;
                                    case 3:
                                        // Can we get here?
                                        /*if (whiteDiff == (ex + 1) / 4 + 1 && CheckNearFieldSmallRel(0, ex - 1, 0, 2, true))
                                        {
                                            ruleTrue = true;
                                            // T("LeftRightAreaUpExtended open corner 3: Cannot step left");
                                            AddForbidden(1, 0);
                                        }*/
                                        // 0611, 0710
                                        if (-whiteDiff == (ex + 1) / 4 - 1 && (j == 0 || j == 3))
                                        {
                                            if (CheckCorner1(0, ex, 0, 2, circleDirectionLeft, true))
                                            {
                                                ruleTrue = true;
                                                // T("LeftRightAreaUpExtended closed corner 3: Cannot step straight");
                                                AddForbidden(0, 1);
                                                // stepping left is already disabled
                                            }
                                        }

                                        // Sequence sixth case
                                        // Sequence can only exist at a short distance (max 3) where the line cannot exit and enter again.
                                        // 0724: up across, down mid across
                                        // 0725_2: up area, down mid across
                                        // 0727_2: up mid across, down across
                                        // 0727_5: sequence up

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

                                                // T("CheckSequence case 6 at " + x + " " + y + ", stop at " + x2 + " " + y2 + ": Cannot step straight");
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

void CheckStairArea()
// Solved by Sequence2
// 0630: Stair on one side, and one of the steps creates an area where we can only enter now.
// 0720: Double close obstacle at the exit point
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
                    // T("StairArea " + dist + " dist: Cannot step straight");
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

            // double area at the exit point of the stair, 0720
            if (dist >= 1)
            {
                if (CheckNearFieldSmallRel1(k, k + 1, 0, 0, true) && CheckNearFieldSmallRel0(k, k + 1, 1, 0, true))
                {
                    // T("StairArea end " + dist + " dist: Cannot step straight");
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

void CheckStairAtStart()

// StairAtStart 3 1 / 3 2 / 4 1 / 4 2

// 3 distance on top:
// 0725_5: mid across down, mid across up
// 0726_1: across, mid across
// 0726_2: mid across, area

// 4 distance on top:
// 0626_1: mid across down, mid across up, no stair
// 0729_3: across, mid across
// 0730: across down, mid across up
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
                    // T("CheckStairAtStart distance " + (dist - 1), "side " + i, "rotation " + j, "topDist " + topDist);

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

                                // 0725_5: mid across down, mid across up
                                // 0726_1: across, mid across
                                // 0726_2: mid across, area

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

                                        // T("CheckStairAtStart 3: cannot step straight");
                                        AddForbidden(0, 1);

                                        if (j == 0)
                                        {
                                            // T("CheckStairAtStart 3: cannot step left");
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
                                // 0626_1: mid across down, mid across up, no stair
                                // 0729_3: across, mid across
                                // 0730: across down, mid across up
                                else if (topDist == 4 && white == black + ex - 3)
                                {
                                    if (CheckNearFieldSmallRel1(-1, 1, 1, 1, false) && CheckNearFieldSmallRel1(-1, 3, 0, 2, true))
                                    {
                                        AddExamAreas();

                                        // T("CheckStairAtStart 4: cannot step right");
                                        AddForbidden(-1, 0);

                                        if (j == 3)
                                        {
                                            // T("CheckStairAtStart 4: cannot step down");
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

void CheckStairAtStartEqual()
{
    // B = W area, corner obstacle at return stair
    // 1012_1
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
                    // T("StairAtStartEqual distance " + (dist - 1), "side " + i, "rotation " + j);

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

                                            // T("StairAtStartEqual: Cannot step up");
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

void CheckStairAtEndConvex()

// Enter later, 0B area:
// StairAtEndConvex 3 1 / 3 2 later
// 0718: across down, mid across up
// 18677343, 59434452: mid across x 2, no stair
// 0720_2: mid across x 2
// 0709: mid across down, C-shape up, no stair
// 0727: mid across down, C-shape up
// 0731: 3 obstacles

// Enter now, 1B -> xB area:
// StairAtEndConvex 3 1 / 3 2 now nostair
// 0516_2: across up, mid across down
// 1012: mid across up, across down

// Corner for convex and concave area can both exist at the same time (0831), so they need two separate functions
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
                // T("CheckStairAtEndConvex distance " + (dist - 1), "side " + i, "rotation " + j);

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

                                // 0718: across down, mid across up
                                // 18677343, 59434452: mid across x 2, no stair
                                // 0720_2: mid across x 2
                                // 0709: mid across down, C-shape up, no stair
                                // 0727: mid across down, C-shape up
                                // 0731: 3 obstacles
                                if (black == white && CheckNearFieldSmallRel(hori - 1, vert + 1, 0, 0, true) && (CheckNearFieldSmallRel1(hori - 3, vert + 1, 1, 0, false) || (CheckNearFieldSmallRel(hori - 4, vert + 2, 1, 0, true) && CheckNearFieldSmallRel0(hori - 4, vert + 2, 0, 0, true))))
                                {
                                    AddExamAreas();
                                    // T("CheckStairAtEndConvex 0B at " + hori + " " + vert + ": Cannot step straight");
                                    AddForbidden(0, 1);

                                    if (j == 0)
                                    {
                                        // T("CheckStairAtEndConvex 0B at " + hori + " " + vert + ": Cannot step right");
                                        AddForbidden(-1, 0);
                                    }
                                }
                                // 0516_2: across up, mid across down
                                // 1012: mid across up, across down
                                else if (black == white + vert + 1)
                                {
                                    if (CheckNearFieldSmallRel1(hori - 2, vert + 1, 0, 0, true) && CheckNearFieldSmallRel1(hori - 2, vert + 1, 1, 0, true))
                                    {
                                        AddExamAreas();
                                        // T("CheckStairAtEndConvex 1B at " + hori + " " + vert + ": Cannot step left");
                                        AddForbidden(1, 0);

                                        if (j == 1)
                                        {
                                            // T("CheckStairAtEndConvex 1B at " + hori + " " + vert + ": Cannot step down");
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

void CheckStairAtEndConvexStraight3()

// Straight:
// 0905 mid across
// 0706 (also Sequence2), 1008 corner

// AreaUp:
// 0916 across
// 665575 mid across

// Stair addition:
// Start obstacle 4, 1: 0706_1, 1006_1
// Start obstacle 4, 2: 0711 (See rules also) First two rotations apply

{
    for (int i = 0; i < 2; i++)
    {
        bool circleDirectionLeft = (i == 0) ? true : false;

        for (int j = 0; j < 2; j++) // j = 0: straight area, j = 1: big area
        {
            int hori = 1;
            int vert = 1;

            while (!InTakenRel(hori, vert) && !InBorderRel(hori, vert))
            {
                // look for a wall 4 left at 1 vertical distance, 5 left at 2 etc.
                while (!InTakenRel(hori, vert) && !InBorderRel(hori, vert))
                {
                    hori++;
                }

                if (hori == vert + 3)
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
                        List<int[]> borderFields = new();

                        borderFields.Add(new int[] { hori - 2, vert });

                        if (vert >= 2)
                        {
                            for (int k = vert; k >= 2; k--)
                            {
                                borderFields.Add(new int[] { k, k });
                                borderFields.Add(new int[] { k, k - 1 });
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
                            // T("CheckStairAtEndConvexStraight3 hori " + hori, "vert " + vert, "side " + i, "rotation " + j);

                            ResetExamAreas();

                            if (!InCornerRel(hori - 1, vert) && CountAreaRel(1, 1, hori - 1, vert, borderFields, circleDirectionLeft, 2, true))
                            {
                                int black = (int)info[1];
                                int white = (int)info[2];

                                if (black - white == vert)
                                {
                                    int counter = 0;
                                    for (int k = 0; k < vert; k++)
                                    {
                                        // corners of the stair will be filled later
                                        path.Add(new int[] { x + (k + 1) * lx + k * sx, y + (k + 1) * ly + k * sy });
                                        counter++;
                                    }

                                    if (CheckCorner1(hori - 2, vert, 1, 0, circleDirectionLeft, true))
                                    {
                                        for (int k = 0; k < counter; k++)
                                        {
                                            path.RemoveAt(path.Count - 1);
                                        }
                                        counter = 0;

                                        AddExamAreas();
                                        // T("CheckStairAtEndConvexStraight3 start obstacle: Cannot step left");
                                        AddForbidden(1, 0);

                                        if (j == 1)
                                        {
                                            // T("CheckStairAtEndConvexStraight3 start obstacle: Cannot step down");
                                            AddForbidden(0, -1);
                                        }
                                    }
                                    else // 0706_1, 1006_1: stair on left after exiting area
                                    {
                                        int h = hori - 1;
                                        int v = vert + 1;
                                        if (InTakenRel(h, v + 1) && !InTakenRel(h, v))
                                        {
                                            bool stairFound = true;

                                            path.Add(new int[] { x + v * lx + (v - 1) * sx, y + v * ly + (v - 1) * sy });
                                            counter++;

                                            while (stairFound)
                                            {
                                                h--;
                                                v++;

                                                if (!(InTakenRel(h, v + 1) && !InTakenRel(h, v)))
                                                {
                                                    stairFound = false;
                                                }
                                                // Add field to taken for countarea, except at the first stair step
                                                else if (v != vert + 2)
                                                {
                                                    path.Add(new int[] { x + (h + 1) * lx + (v - 2) * sx, y + (h + 1) * ly + (v - 2) * sy });
                                                    counter++;
                                                }

                                                if (stairFound)
                                                {
                                                    ResetExamAreas();

                                                    if (CheckCorner1(h, v - 1, 1, 1, circleDirectionLeft, true))
                                                    {
                                                        for (int k = 0; k < counter; k++)
                                                        {
                                                            path.RemoveAt(path.Count - 1);
                                                        }
                                                        counter = 0;

                                                        AddExamAreas();
                                                        // 0706_1, 1006_1
                                                        // T("CheckStairAtEndConvexStraight3 Stair: Cannot step left");
                                                        AddForbidden(1, 0);
                                                        // Would the down field ever be free?
                                                        /*if (j == 1) // big area
                                                        {
                                                            // 1006_1
                                                            // T("CheckStairAtEndConvexStraight3 Stair: Cannot step down");
                                                            AddForbidden(0, -1);
                                                        }*/
                                                        break;
                                                    }
                                                }
                                            }

                                            h++;
                                            v--;

                                            for (int k = 0; k < counter; k++)
                                            {
                                                path.RemoveAt(path.Count - 1);
                                            }
                                            counter = 0;
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

                vert++;
                hori = vert;
            }

            // 0711
            hori = 1;
            vert = 2;

            while (!InTakenRel(hori, vert) && !InBorderRel(hori, vert))
            {
                // look for a wall 4 left at 2 vertical distance, 5 left at 3 etc.
                while (!InTakenRel(hori, vert) && !InBorderRel(hori, vert) && !InTakenRel(hori, vert - 1) && !InBorderRel(hori, vert - 1))
                {
                    hori++;
                }

                // the loop could have ended with the lower line getting to a taken field, but the upper line could be empty
                if (hori == vert + 2 && InTakenRel(hori, vert))
                {
                    bool circleValid = false;

                    //if (!InBorderRel(hori, vert))
                    //{

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

                    //}

                    if (circleValid)
                    {
                        List<int[]> borderFields = new();

                        borderFields.Add(new int[] { hori - 2, vert - 1 });

                        if (vert >= 3)
                        {
                            for (int k = vert - 1; k >= 2; k--)
                            {
                                borderFields.Add(new int[] { k, k });
                                borderFields.Add(new int[] { k, k - 1 });
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
                            // T("CheckStairAtEndConvexStraight3 hori " + hori, "vert " + vert, "side " + i, "rotation " + j);

                            ResetExamAreas();

                            if (!InCornerRel(hori - 1, vert - 1) && CountAreaRel(1, 1, hori - 1, vert - 1, borderFields, circleDirectionLeft, 2, true))
                            {
                                int black = (int)info[1];
                                int white = (int)info[2];

                                if (black - white == vert - 1)
                                {
                                    int counter = 0;
                                    for (int k = 0; k < vert - 1; k++)
                                    {
                                        // corners of the stair will be filled later
                                        path.Add(new int[] { x + (k + 1) * lx + k * sx, y + (k + 1) * ly + k * sy });
                                        counter++;
                                    }

                                    // CheckCorner here if example is found

                                    int h = hori - 1;
                                    int v = vert;
                                    if (InTakenRel(h, v + 1) && !InTakenRel(h, v))
                                    {
                                        bool stairFound = true;

                                        path.Add(new int[] { x + v * lx + (v - 1) * sx, y + v * ly + (v - 1) * sy });
                                        counter++;

                                        while (stairFound)
                                        {
                                            h--;
                                            v++;

                                            if (!(InTakenRel(h, v + 1) && !InTakenRel(h, v)))
                                            {
                                                stairFound = false;
                                            }
                                            // Add field to taken for countarea, except at the first stair step
                                            else if (v != vert + 2)
                                            {
                                                path.Add(new int[] { x + (h + 1) * lx + (v - 2) * sx, y + (h + 1) * ly + (v - 2) * sy });
                                                counter++;
                                            }

                                            if (stairFound)
                                            {
                                                ResetExamAreas();

                                                if (CheckCorner1(h, v - 1, 1, 1, circleDirectionLeft, true))
                                                {
                                                    for (int k = 0; k < counter; k++)
                                                    {
                                                        path.RemoveAt(path.Count - 1);
                                                    }
                                                    counter = 0;

                                                    AddExamAreas();
                                                    // 0711
                                                    // T("CheckStairAtEndConvexStraight 4 2 Stair: Cannot step left");
                                                    AddForbidden(1, 0);

                                                    break;
                                                }
                                            }
                                        }

                                        h++;
                                        v--;

                                        for (int k = 0; k < counter; k++)
                                        {
                                            path.RemoveAt(path.Count - 1);
                                        }
                                        counter = 0;
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

                vert++;
                hori = vert;
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

void CheckStairAtEndConcave5()
// obstacles inside, hori = vert + 5
// 0814: stair, mid across x 2
// 0619_1: mid across x 2
// 0729_1: across down, mid across up
// 0729_4: mid across down, across up
// 0820: mid across down, C-shape up
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
            // No condition to have at least two steps: Will work as StraightSmall
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
                // T("CheckStairAtEndConcave5 distance " + (dist - 1), "side " + i, "rotation " + j);

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

                                if (white == black + 1 && CheckNearFieldSmallRel(hori - 1, vert + 1, 0, 0, false) && CheckNearFieldSmallRel1(hori - 3, vert + 1, 1, 0, true))
                                {
                                    AddExamAreas();
                                    // T("CheckStairAtEndConcave5 at " + hori + " " + vert + ": Cannot step left");
                                    AddForbidden(1, 0);

                                    if (j == 1)
                                    {
                                        // T("CheckStairAtEndConcave5 at " + hori + " " + vert + ": Cannot step down");
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

void CheckStairAtEndConcave6()
// obstacles inside, hori = vert + 6
// 0714: mid across x 2
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
                // T("CheckStairAtEndConcave6 distance " + (dist - 1), "side " + i, "rotation " + j);

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

                                // 0814                                        
                                if (white == black + 1 && CheckNearFieldSmallRel1(hori - 2, vert + 1, 0, 0, false) && CheckNearFieldSmallRel1(hori - 4, vert + 1, 1, 0, true))
                                {
                                    AddExamAreas();
                                    // T("CheckStairAtEndConcave at " + hori + " " + vert + ": Cannot step left");
                                    AddForbidden(1, 0);

                                    if (j == 1)
                                    {
                                        // T("CheckStairAtEndConcave at " + hori + " " + vert + ": Cannot step down");
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

void CheckStairAtEnd3Obtacles1() // 0731_1 straight area, 0725_4 small area
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
                    // T("CheckStairAtEnd3Obtacles1 distance " + (dist - 1), "side " + i, "rotation " + j);

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
                                                        // T("Reverse stair 3 obstacles case 1 at " + hori + " " + vert + ": Cannot step right");
                                                        AddForbidden(-1, 0);
                                                        if (hori - 1 > 1) // example needs to be saved
                                                        {
                                                            errorInWalkthrough = true;
                                                            criticalError = true;
                                                            errorString = "Reverse stair 3 obstacles nextX > 3";
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

void CheckStairAtEnd3Obtacles2() // 0805: start 2 dist, 0808: start 3 dist, 0811_3: nextX = 4
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
                // T("CheckStairAtEnd3Obtacles2 distance " + (dist - 1), "side " + i, "rotation " + j);

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

                                // T("CheckNearFieldSmallRel(0, 1, 1, 0, true) " + CheckNearFieldSmallRel(0, 1, 1, 0, true));
                                if (black == white + vert && CheckNearFieldSmallRel0(hori - 1, vert + 1, 0, 0, true) && CheckNearFieldSmallRel(0, 1, 1, 0, true) && CheckNearFieldSmallRel(hori - 4, vert + 2, 1, 0, true) && CheckNearFieldSmallRel0(hori - 4, vert + 2, 0, 0, true))
                                {
                                    AddExamAreas();
                                    // T("Reverse stair 3 obstacles case 2 at " + hori + " " + vert + ": Cannot step straight");
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

/*void CheckAreaUpStartObstacleInside() // 0618, 0619: When we enter the area, we need to step up. There is a close obstacle the other way inside the area.
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
                            if (j <= 1 && whiteDiff == (ex - 1) / 4 && CheckNearFieldSmallRel1(1, 2, 0, 1, true)) // Mid across: 0618, Across: 0717_1
                            {
                                ruleTrue = true;
                                // T("AreaUpStartObstacleInside % 4 = 1: Cannot step straight and right");
                                AddForbidden(0, 1);
                                if (j != 2) // the right field relative to the area (left of the main line) is now inside the area.
                                {
                                    AddForbidden(-1, 0);
                                }
                            }
                            break;
                        case 3:
                            if (j >= 1 && whiteDiff == (ex + 1) / 4 && CheckNearFieldSmallRel1(1, 0, 0, 1, true)) // Mid across: 0619, Across: 0717_2, area up 3 start obstacle
                            {
                                ruleTrue = true;
                                // T("AreaUpStartObstacleInside % 4 = 3: Cannot step left");
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
}*/

void CheckStartObstacleInside()
// When we enter the area, we need to step up. There is a close obstacle the other way inside the area.
// 0619, 0818: straight
// 0618, 0717_1, 0717_2: area up
// 0811, 0817: corner
// Example needed for corner (y - x) % 4 = 2 (0619 extension)
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
                        // T("StartObstacleInside corner discovery error.");

                        errorInWalkthrough = true;
                        errorString = "StartObstacleInside corner discovery error.";
                        criticalError = true;
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
                    // Straight obstacle is allowed at 3 distance as in 0619
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
                                            // Mid across: 0618, 0817, 0818
                                            // Across: 0717_1
                                            {
                                                ruleTrue = true;
                                                // T("StartObstacleInside % 4 = 0: Cannot step straight");
                                                AddForbidden(0, 1);

                                                if (j == 0)
                                                {
                                                    // T("StartObstacleInside % 4 = 0: Cannot step right");
                                                    AddForbidden(-1, 0);
                                                }
                                            }
                                            break;
                                        case 2:
                                            if (j >= 1 && whiteDiff == (vert - hori + 2) / 4 && CheckNearFieldSmallRel1(1, 0, 0, 1, true))
                                            // Mid across: 0619
                                            // Across: 0717_2, area up 3 start obstacle
                                            {
                                                ruleTrue = true;
                                                // T("StartObstacleInside % 4 = 2: Cannot step left");
                                                // straight direction is disabled already due to single area rule                                                        
                                                if (hori > 2 && !InForbidden(new int[] { x + lx, y + ly }))
                                                {
                                                    // T("StartObstacleInside corner (y - x) % 4 = 2");

                                                    errorInWalkthrough = true;
                                                    errorString = "StartObstacleInside corner (y - x) % 4 = 2";
                                                    criticalError = true;
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

void CheckStraightSmall() // 0619_1, 0714, 0716, 0717_4
// double obstacle inside
// Two columns are checked for being empty, but at the end the straight field must be taken, and the left field must be empty.
{
    for (int i = 0; i < 2; i++)
    {
        bool circleDirectionLeft = (i == 0) ? true : false;

        for (int j = 0; j < 3; j++) // j = 0: straight area, j = 1: right (big) area, j = 2: behind on right side (0716)
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
                        // 0717_4: across down, mid across up
                        // 0729_2: mid across down, across up
                        {
                            if (CheckNearFieldSmallRel1(1, 0, 0, 1, true) && CheckNearFieldSmallRel1(1, 2, 1, 2, false))
                            {
                                // T("CheckStraightSmall 2 double close obstacle inside: Cannot step left");
                                AddForbidden(1, 0);

                                AddExamAreas();
                            }
                        }

                        if (j >= 1 && ex == 3 && white == black + 1)
                        // 0716: mid across x 2
                        // 0729: mid across down, across up
                        // 0730_1: across down, mid across up
                        {
                            if (CheckNearFieldSmallRel1(1, 0, 0, 1, true) && CheckNearFieldSmallRel1(1, 2, 1, 2, false))
                            {
                                // T("CheckStraightSmall 3 double close obstacle inside: Cannot step left");
                                AddForbidden(1, 0);

                                AddExamAreas();
                            }
                        }

                        /* use CheckStairAtEndConcave5 and CheckStairAtEndConcave6
                        
                        if (j <= 1 && ex == 4 && white == black + 1)
                        // 0619_1: mid across x 2
                        // 0729_1: across down, mid across up
                        // 0729_4: mid across down, across up
                        // 0820: mid across down, C-shape up
                        {
                            if (CheckNearFieldSmallRel1(1, 2, 0, 1, true) && CheckNearFieldSmallRel(1, 4, 1, 2, false))
                            {
                                // T("CheckStraightSmall 4 double close obstacle inside: Cannot step straight");
                                AddForbidden(0, 1);

                                if (j == 0)
                                {
                                    // T("CheckStraightSmall 4 double close obstacle inside: Cannot step right");
                                    AddForbidden(-1, 0);
                                }
                                AddExamAreas();
                            }
                        }

                        if (j <= 1 && ex == 5 && white == black + 1) // 0714: mid across x 2
                        {
                            if (CheckNearFieldSmallRel1(1, 2, 0, 1, true) && CheckNearFieldSmallRel1(1, 4, 1, 2, false))
                            {
                                // T("CheckStraightSmall 5 double close obstacle inside: Cannot step straight and right");
                                AddForbidden(0, 1);

                                if (j == 0)
                                {
                                    // T("CheckStraightSmall 5 double close obstacle inside: Cannot step right");
                                    AddForbidden(-1, 0);
                                }
                                AddExamAreas();
                            }
                        }*/
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

void CheckLeftRightAreaUpBigExtended() // Area as in the first area case of documentation. That area is taken care of in UpBig and Striaght. This is about a border movement close obstacle: 0624
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
                            // 0624: mid across
                            // 0730_2: across
                            // When entering at the first white field, we have to step down to the first black and then left to enter
                            {
                                ruleTrue = true;
                                // T("CheckLeftRightAreaUpBigExtended start obstacle: Cannot step left");
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

void CheckStraightBig() // 18677343 -> CheckStairAtEndConvex, 59434452 -> CheckStairAtEndConvex, 0626_1 -> StairAtStart 4, 0516_2 -> CheckStairAtEndConvex
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

                            /*// 18677343, 59434452: mid across x 2
                            // 0709: mid across down, C-shape up 
                            if (white == black)
                            {
                                if (CheckNearFieldSmallRel0(-1, 1, 1, 1, false) && CheckNearFieldSmallRel(-1, 3, 0, 2, true))
                                {
                                    // T("CheckStraightBig double close obstacle outside 3 dist 0W: Cannot step right and down");
                                    AddForbidden(0, -1);
                                    AddForbidden(-1, 0);

                                    AddExamAreas();
                                }
                            }*/
                            // 0516_2
                            /*else if (black == white + 1)
                            {
                                if (CheckNearFieldSmallRel1(-1, 2, 0, 2, true) && CheckNearFieldSmallRel0(-1, 2, 1, 1, true))
                                {
                                    // T("CheckStraightBig double close obstacle outside 3 dist 1B: Cannot step up and left");
                                    AddForbidden(0, 1);
                                    AddForbidden(1, 0);

                                    AddExamAreas();
                                }
                            }*/
                        }
                    }
                    /*// 0626_1: mid across x 2
                    // 0729_3: mid across down, across up
                    // 0730: across down, mid across up
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
                                    // T("CheckStraightBig double close obstacle outside 4 dist: Cannot step right and down");
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
    /*
    for (int i = 0; i < 2; i++)
    {
        bool circleDirectionLeft = (i == 0) ? true : false;

        for (int j = 0; j < 2; j++)
        {
            // First case

            // Triple Area
            // 2024_0516_2
            // Rotated: 2024_0516_3

            // See also 665575 for alternative start obstacle placement
            
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

                                // T("CheckSequence case 1 at " + x + " " + y + ", stop at " + x2 + " " + y2 + ": Cannot step straight");
                                AddForbidden(0, 1);

                                if (j == 0)
                                {
                                    // Due to CheckStraight, stepping left is already disabled when the obstacle is straight ahead. When it is one to the right, we need the left field to be disabled.
                                    // T("CheckSequence case 1 at " + x + " " + y + ", stop at " + x2 + " " + y2 + ": Cannot step left");
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

                                // T("CheckSequence case 2 at " + x + " " + y + ", stop at " + x2 + " " + y2 + ": Cannot step straight");
                                AddForbidden(0, 1);

                                if (j == 0)
                                {
                                    // T("CheckSequence case 2 at " + x + " " + y + ", stop at " + x2 + " " + y2 + ": Cannot step left");
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

                                // T("CheckSequence case 3 at " + x + " " + y + ", stop at " + x2 + " " + y2 + ": Cannot step straight");
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
    // 2024_0723 -> Sequence 2 stair start
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

                    // T("CheckSequence case 4 at " + x + " " + y + ", stop at " + x2 + " " + y2 + ": Cannot step straight");
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
    */

    // Fifth case, 0724_1: Step right next step C-shape. There is an obstacle 2 distance to the right to start with.

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

                        // T("CheckSequence case 5 at " + x + " " + y + ", stop at " + x2 + " " + y2 + ": Cannot step right");
                        AddForbidden(-1, 0);

                        if (j == 1)
                        {
                            // T("CheckSequence case 5 at " + x + " " + y + ", stop at " + x2 + " " + y2 + ": Cannot step down");
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


    // Sixth case: 0727_5, implemented in UpExtended
}

void CheckNearStair() // 0726, 0713, nearStair 1/2/3
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
                        // T("NearStair: Cannot enter now left");
                        if (j == 1)
                        {
                            // T("NearStair: Cannot enter now down");
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

void CheckDoubleStair() // 0706_1 -> StairAtEndConvex Stair, Also Sequence2: 0516_4, 0516_5
{
    for (int i = 0; i < 2; i++)
    {
        for (int j = 0; j < 2; j++) // normal or big area
        {
            int dist = size;
            int quarter = quarters[i][j];
            int nearQuarter;
            List<int[]>[] corners;

            if (i == 0)
            {
                nearQuarter = quarter == 0 ? 3 : quarter - 1;
                corners = openCWCorners;
            }
            else
            {
                nearQuarter = quarter == 3 ? 0 : quarter + 1;
                corners = openCCWCorners;
            }

            bool corner0Found = false;

            foreach (int[] corner in corners[nearQuarter])
            {
                if (corner[0] == 1 && corner[1] == 1)
                {
                    corner0Found = true;
                }
            }

            if (corner0Found)
            {
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

                // T("Double stair corner1Found", corner1Found, corner2Found, i, j);
                if (corner1Found && corner2Found)
                {
                    T(CheckNearFieldSmallRel(2, 2, 0, 2, false), CheckNearFieldSmallRel(3, 1, 1, 3, true));
                    // either stair on both sides of the two corners (0706_1) or close obstacle (0516_4)
                    if (CheckNearFieldSmallRel(2, 2, 0, 2, false) && CheckNearFieldSmallRel(3, 1, 1, 3, true))
                    {
                        // T("DoubleStair: Cannot step up");
                        AddForbidden(0, 1);

                        if (j == 0)
                        {
                            // T("DoubleStair: Cannot step right");
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

/*
void CheckSideStair()
// Start at -1 vertical. 0516_6, 0516_7, 0516_8 -> Sequence2
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
                            // T("CheckSideStair at " + hori + " " + vert + ": Cannot step left");
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
*/

/* void CheckSideStairStraight() -> Sequence 2
// Start at 0 vertical. 1001
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
                // T("SideStairStraight", i, j);
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
                            // T("CheckSideStairStraight at " + hori + " " + vert + ": Cannot step left");

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
}*/

void CheckSequence2()
// Start at 3, 0. Any combination of stairs and 2-distance straight obstacles are possible. 2 rotations possible.
// 0706, 1001: corner
// 1005: mid across
// 0516, 0516_1: one step across
// 0516_4, 0516_5: multiple step across
// 1006. Across obstacle encountered on the left. It appears at the end of the sequence, does not take part in adding a step to it.
// (0723 starts with next step C-shape)
// Double area at first step:
// 1014
// 0704, 1014: area on left, close mid across on right

// Start at 3, -1
// 0516_6, 0516_7, 0516_8: across, 3 rotations        

// Start at stair: 0630, 0720, 0723

// Start at 4, -1: 0727_5 (Start area is UpExtended, distance to the first obstacle % 4 = 3)
// 0724: up across, down mid across
// 0725_2: up area, down mid across
// 0727_2: up mid across, down across

// Start at 4, 0: 1115 (area is straight)

// Obstacle at 3, 0 and 1, -1: Next step to front will create a stair start: 0724_1

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
                        // T("CheckSequence2 3 0, side", i, "rotation", j);
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
                        // T("CheckSequence2 3 -1, side", i, "rotation", j);
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
                        // T("CheckSequence2 4 -1, side", i, "rotation", j);
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
                            // T("CheckSequence2 4 0, side", i, "rotation", j);
                            startObstacleValid = true;
                            vertLow = true;
                        }
                    }
                }
            }

            // stair start, 0630, 0720, 0723
            if (!startObstacleValid && j == 0 || j == 3)
            {
                if (InTakenRel(1, 0) && InTakenRel(2, 1) && !InTakenRel(1, 1))
                {
                    // T("CheckSequence2 stair, side", i, "rotation", j);
                    startObstacleValid = true;
                    stairStart = true;
                    hori = 2;
                    vert = 1;
                }
            }

            // 0724_1
            if (!startObstacleValid && j < 2)
            {
                if (InTakenRel(0, 3) && !InTakenRel(1, 3) && !InTakenRel(0, 2) && !InTakenRel(1, 0) && InTakenRel(1, -1))
                {
                    i1 = InTakenIndexRel(0, 3);
                    i2 = InTakenIndexRel(-1, 3);

                    if (i1 > i2)
                    {
                        // T("CheckSequence2 stair 2, side", i, "rotation", j);
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
                    if (hori == 4 && (vert == -1 || vert == 0)) // 0727_5. We need to think about a general UpExtended start area where distance to the obstacle % 4 = 3.
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
                                // T("areaUp area counted, black = white");
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
                                // T("straight area counted, black = white");
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
                        if (hori == 4 && (vert == -1 || vert == 0)) // 0727_5
                        {
                            path.Add(new int[] { x + lx, y + ly });
                            path.Add(new int[] { x + 3 * lx, y + 3 * ly });
                            path.Add(new int[] { x + 2 * lx + sx, y + 2 * ly + sy });
                            counter = 3;
                            hori = 3;
                            vert = 0;
                        }
                        else // 1006
                        {
                            path.Add(new int[] { x + lx, y + ly });
                        }
                    }
                    else if (stairStart)
                    {
                        // Add straight field and left up (the second field needs to be added to establish found corner direction later in the sequence: 0723
                        path.Add(new int[] { x + sx, y + sy });
                        path.Add(new int[] { x + lx + sx, y + ly + sy });
                        counter++;
                    }
                    else
                    { // 0724_1                                
                        path.Add(new int[] { x + lx + sx, y + ly + sy });
                        // T("Added start 0", path[path.Count - 1][0], path[path.Count - 1][1]);
                        path.Add(new int[] { x + lx, y + ly });
                        counter++;
                        rotationIndex = 3;
                    }

                    // T("Added start", path[path.Count - 1][0], path[path.Count - 1][1], "at", counter, "hori " + hori, "vert " + vert);

                    int limitCounter = 0;
                    // start at hori 3, vert 0
                    while (stepFound || farStraightFound)
                    {
                        limitCounter++;
                        if (limitCounter == size * size)
                        {
                            // T("Sequence limit.");

                            errorInWalkthrough = true;
                            criticalError = true;
                            errorString = "Sequence limit";
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

                        // 0704, 1014, 0724, 0725_2: double area at first step. For subsequent steps, rotation has to be changed from 0 to its actual value.
                        // 1006: double area after many steps

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

                        if (CheckCorner1(hori - 2 * hx, vert - 2 * hy, 0, rotationIndex2, circleDirectionLeft, true) && CheckNearFieldSmallRel0(hori - 2 * hx, vert - 2 * hy, 1, rotationIndex3, true))
                        {
                            AddExamAreas(true);

                            for (int m = 1; m <= counter; m++)
                            {
                                path.RemoveAt(path.Count - 1);
                            }
                            counter = 0;

                            if (!stairStart) // 0704, 1014, 0724, 0725_2, 1006
                            {
                                // T("CheckSequence2 double area at relative " + (hori - 2 * hx) + " " + (vert - 2 * hy) + ": Cannot step left");
                                AddForbidden(1, 0);
                            }
                            else // 0720
                            {
                                // T("CheckSequence2 double area stair start at relative " + (hori - 2 * hx) + " " + (vert - 2 * hy) + ": Cannot step straight");
                                AddForbidden(0, 1);
                            }

                            break;
                        }

                        // T("hori", hori, "vert", vert, "straightFound", farStraightFound, "stepFound", stepFound);

                        // 2025_0516, C-shape ahead on the right
                        // Checking for empty field ahead is probably not necessary
                        // Only apply for stairStart2 for now.
                        if (stepFound && InTakenRel(hori - 2 * hx + 2 * vx, vert - 2 * hy + 2 * vy) && InTakenRel(hori - 3 * hx + vx, vert - 3 * hy + vy) && stairStart2)
                        {
                            // T("CheckSequence2 double C-shape at relative " + (hori - 2 * hx) + " " + (vert - 2 * hy) + " stairStart2: Cannot step straight");
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

                            // T("Added", path[path.Count - 1][0], path[path.Count - 1][1], "at", counter);

                            ResetExamAreas();

                            if (CheckCorner1(hori - 2 * hx, vert - 2 * hy, 1, nearFieldRotation, circleDirectionLeft, true))
                            {
                                AddExamAreas(true);

                                for (int m = 1; m <= counter; m++)
                                {
                                    path.RemoveAt(path.Count - 1);
                                }
                                counter = 0;

                                if (!stairStart && !stairStart2)
                                {
                                    // T("CheckSequence2 at relative " + (hori - 2 * hx) + " " + (vert - 2 * hy) + ": Cannot step left");
                                    AddForbidden(1, 0);

                                    if (j == 1 && !vertLow)
                                    {
                                        // T("CheckSequence2 at relative " + (hori - 2 * hx) + " " + (vert - 2 * hy) + ": Cannot step down");
                                        AddForbidden(0, -1);
                                    }
                                }
                                else if (stairStart)
                                {
                                    // T("CheckSequence2 at relative " + (hori - 2 * hx) + " " + (vert - 2 * hy) + " stairStart: Cannot step straight");
                                    AddForbidden(0, 1);
                                }
                                else
                                {
                                    // T("CheckSequence2 at relative " + (hori - 2 * hx) + " " + (vert - 2 * hy) + " stairStart2: Cannot step straight");
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

                        // T("New rotationIndex", rotationIndex, "hori", hori, "vert", vert);
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

void Check3x3Stair() // 0722 / 3x3Stair. It is not a nested 3x3 area sequence. 1111 shows, even if we step down, there will be two-way choice later.
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
                            // T("Check3x3Stair at side " + i + " rotation " + j + ": Cannot step left");
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

void CheckRemoteStair()
// 0818_1
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
                    // T("RemoteStair distance " + (dist - 1), "side " + i, "rotation " + j);

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
                                    // T("RemoteStair discovery error.");

                                    errorInWalkthrough = true;
                                    errorString = "RemoteStair discovery error.";
                                    criticalError = true;
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
                                            errorInWalkthrough = true;
                                            errorString = "RemoteStair across found.";
                                            criticalError = true;
                                            return;
                                        }*/

                                        AddExamAreas();

                                        // T("RemoteStair mid across: Cannot step up");
                                        AddForbidden(0, 1);

                                        if (j == 0)
                                        {
                                            // T("RemoteStair mid across: Cannot step left");
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

/* This function is incomplete as the found corners can be separated from the live end. In 0901, if called in the j = 3 rotation, CornerDiscovery(0, 1, false, true, 3) will return the 7, 3 corner. That will result in an infinite loop in RemoteStair where a wall should be found at each descend.  */
/*List<int[]>? CornerDiscovery(int startX, int startY, bool toLeft, bool closedCorner, int minEndCoord)
{
    List<int[]> foundCorners = new();

    // Can we have an area with a corner if this field is taken? It isn't in the border line.
    if (!InTakenRel(startX, startY) && !InBorderRel(startX, startY))
    {
        // checking taken fields from the middle to side is incomplete: 17699719
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
            // If we terminate the walkthrough at startX and startY, a close across obstacle will might be missed (0822). Therefore, we shift the x coordinate. 
            while (!InCornerRel(nextX, nextY) && !(nextX == startX - 1 && nextY == startY))
            {
                //T("Corner discovery nextX " + nextX, "nextY " + nextY);

                counter++;
                if (counter == size * size)
                {
                    // T("Corner discovery error.");

                    errorInWalkthrough = true;
                    errorString = "Corner discovery error.";
                    criticalError = true;
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
                        // T("Corner discovery error 2.");

                        errorInWalkthrough = true;
                        errorString = "Corner discovery error 2.";
                        criticalError = true;
                        return null;
                    }

                    leftDirection = (leftDirection == 0) ? 3 : leftDirection - 1;
                    possibleNextX = nextX + directions[leftDirection][0];
                    possibleNextY = nextY + directions[leftDirection][1];
                }

                // if we have turned left from a right direction (to upwards), a corner is found
                // It has to be left and up. In 0619_2 the walking edge goes below the current position.
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

            // T("coordStart: " + coordStart);
            int currentDirection = 1;

            int nextX = startX;
            int nextY = coordStart - 1;

            // T("nextX0", nextX, nextY);
            int counter = 0;

            // area walls can be found in any of the four quarters. Therefore, we only stop when we have reached the corner or passed by the live end.
            while (!InCornerRel(nextX, nextY) && !(nextX == startX && nextY == startY - 1))
            {
                // T("nextX1", nextX, nextY);
                counter++;
                if (counter == size * size)
                {
                    // T("Corner discovery error.");

                    errorInWalkthrough = true;
                    errorString = "Corner discovery error.";
                    criticalError = true;
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
                        // T("Corner discovery error 2.");

                        errorInWalkthrough = true;
                        errorString = "Corner discovery error 2.";
                        criticalError = true;
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
}*/

void CornerDiscoveryAll()
{
    int coordStart;
    bool cornerReached = false;
    bool liveEndReached = false; // It is not enough to reach the corner before getting back to the walkthrough start. See 0823
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
            // T("Corner discovery error.");

            errorInWalkthrough = true;
            errorString = "Corner discovery error.";
            criticalError = true;
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
                // T("Corner discovery error 2.");

                errorInWalkthrough = true;
                errorString = "Corner discovery error 2.";
                criticalError = true;
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

    // T("Closed corners:");
    foreach (int[] corner in closedCorners[0])
    {
        // T("0: " + corner[0] + " " + corner[1]);
    }
    foreach (int[] corner in closedCorners[1])
    {
        // T("1: " + corner[0] + " " + corner[1]);
    }
    foreach (int[] corner in closedCorners[2])
    {
        // T("2: " + corner[0] + " " + corner[1]);
    }
    foreach (int[] corner in closedCorners[3])
    {
        // T("3: " + corner[0] + " " + corner[1]);
    }
    // T("Open CW corners:");
    foreach (int[] corner in openCWCorners[0])
    {
        // T("0: " + corner[0] + " " + corner[1]);
    }
    foreach (int[] corner in openCWCorners[1])
    {
        // T("1: " + corner[0] + " " + corner[1]);
    }
    foreach (int[] corner in openCWCorners[2])
    {
        // T("2: " + corner[0] + " " + corner[1]);
    }
    foreach (int[] corner in openCWCorners[3])
    {
        // T("3: " + corner[0] + " " + corner[1]);
    }
    // T("Open CCW corners:");
    foreach (int[] corner in openCCWCorners[0])
    {
        // T("0: " + corner[0] + " " + corner[1]);
    }
    foreach (int[] corner in openCCWCorners[1])
    {
        // T("1: " + corner[0] + " " + corner[1]);
    }
    foreach (int[] corner in openCCWCorners[2])
    {
        // T("2: " + corner[0] + " " + corner[1]);
    }
    foreach (int[] corner in openCCWCorners[3])
    {
        // T("3: " + corner[0] + " " + corner[1]);
    }
}

bool CheckCorner1(int left, int straight, int side, int rotation, bool circleDirectionLeft, bool smallArea)
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
            // T("Corner2 0 discovery error.");

            errorInWalkthrough = true;
            errorString = "Corner2 0 discovery error.";
            criticalError = true;
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
          // Second condition: 0708_1: Finish corner is reached, there cannot be small area from there.
          // Third condition: 0708_2: We never get to -1 horizontal position, the area is closed. When we get to the first square again, break the cycle.

            //T("nextX", nextX, nextY);
            counter++;
            if (counter == size * size)
            {
                // T("Corner2 discovery error.");

                errorInWalkthrough = true;
                errorString = "Corner2 discovery error.";
                criticalError = true;
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
                    // T("Corner2 1 discovery error.");

                    errorInWalkthrough = true;
                    errorString = "Corner2 1 discovery error.";
                    criticalError = true;
                    return false;
                }

                l = (l == 0) ? 3 : l - 1;
                possibleNextX = nextX + directions[l][0];
                possibleNextY = nextY + directions[l][1];
            }

            if (currentDirection == 0 && l == 0 && nextY >= 1) // 0708: Corner can be found beneath
            {
                int hori = nextX + 1;
                int vert = nextY + 1;

                // T("Corner at", hori, vert, "x2", x2, "y2", y2, "lx2", lx2, "ly2", ly2, "circleDirectionLeft", circleDirectionLeft);

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
                                // T("Corner2: Cannot enter later");
                                return true;
                            }
                        }*/
                    }
                    else // Corner 0627, 0627_1
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
                                    // T("Corner1: Cannot enter later");
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
          // Second condition: 0708_1: Finish corner is reached, there cannot be small area from there.
          // Third condition: 0708_2: We never get to -1 horizontal position, the area is closed. When we get to the first square again, break the cycle.

            //T("nextX", nextX, nextY);
            counter++;
            if (counter == size * size)
            {
                // T("Corner2 discovery error.");

                errorInWalkthrough = true;
                errorString = "Corner2 discovery error.";
                criticalError = true;
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

            if (currentDirection == 0 && l == 0 && nextY >= 1) // 0708: Corner can be found beneath
            {
                int hori = nextX + 1;
                int vert = nextY + 1;

                // T("Corner at", hori, vert, "x2", x2, "y2", y2, "lx2", lx2, "ly2", ly2, circleDirectionLeft);

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
                                // T("Corner2: Cannot enter later");
                                return true;
                            }
                        }*/
                    }
                    else // Corner 0627, 0627_1
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
                                    // T("Corner2: Cannot enter later");
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

bool CheckSequenceRecursive(int side)
{
    // T("Recursive start side: " + side, "x2 y2 lx2 ly2: " + x2, y2, lx2, ly2);

    counterrec++;
    if (counterrec == size * size)
    {
        errorInWalkthrough = true;
        errorString = "Recursive overflow.";
        criticalError = true;
        return false;
    }

    newExitField0 = new int[] { 0, 0 };
    newExitField = new int[] { 0, 0 };

    ResetExamAreas2(); // prevents showing an area from previous cycle

    bool leftSideEnterNow = CheckCorner2(side, true);
    sequenceLeftObstacleIndex = -1; // needed for 0811_1 and 0811_2 where live end would be inside the area
    bool leftSideClose = CheckNearFieldSmall2(); // contains exit points for next call but only works for c-shapes and close obstacles.
    lx2 = -lx2;
    ly2 = -ly2;
    bool rightSideClose = CheckNearFieldSmall3(); // 0722, 0811
    int tempSequenceLeftIndex = sequenceLeftObstacleIndex;
    sequenceLeftObstacleIndex = -1;
    bool rightSideEnterNow = CheckCorner2(1 - side, true);
    sequenceLeftObstacleIndex = tempSequenceLeftIndex;
    lx2 = -lx2;
    ly2 = -ly2;

    // T("Recursive checked", leftSideClose, leftSideEnterNow, rightSideClose, rightSideEnterNow, sequenceLeftObstacleIndex);

    if ((leftSideClose || leftSideEnterNow) && (rightSideClose || rightSideEnterNow))
    {
        return true;
    }
    // right side close can happen with the future line
    // for now, we only take the right side C-shape into account as it happens in 740 293. Other close obstacles we don't check.
    else if (leftSideClose)
    //else if ((leftSideClose || rightSideClose) && newExitField[0] != 0)
    {
        // T("CheckSequenceRecursive left side only x2 " + newExitField[0] + " y2 " + newExitField[1] + " direction rotated " + newDirectionRotated);

        // at 0723, it is important that both fields are added, because the sequence relies on the first when determining the direction of the obstacle.
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

        if (InTakenRel2(0, 1)) // 0708 Field in front of exit should be empty
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
        // T("CheckSequenceRecursive right side only x2 " + newExitField[0] + " y2 " + newExitField[1] + " direction rotated " + newDirectionRotated);

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

        if (InTakenRel2(0, 1)) // 0708: Field in front of exit should be empty
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

    // direction checkng is not necessary when the close obstacle is inside the area, but it is when the obstacle is at the exit point of the area. 1023055626

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

    // direction checkng is not necessary when the close obstacle is inside the area, but it is when the obstacle is at the exit point of the area. 1023055626

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

    // direction checkng is not necessary when the close obstacle is inside the area, but it is when the obstacle is at the exit point of the area. 1023055626

    for (int i = 0; i < 2; i++)
    {
        for (int j = 0; j < 4; j++) // j = 0: middle, j = 1: small area, j = 2: big area, j = 3: big (right down) area
        {
            if (i == side && j == rotation)
            {
                // C-shape left
                // if (InTakenRel(x + 2 * lx, y + 2 * ly) && InTakenRel(x + lx - sx, y + ly - sy) && !InTakenRel(x + lx, y + ly))
                // For 0808, border checking is needed too.
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

bool CheckNearFieldSmall2() // for use with Sequence
                            // Case 2 and 3, used in recursive function
{
    bool ret = false;

    // C-Shape, only left side should have it
    // Checking for InTakenRel2(1, -1) is not possible, because in Sequence first case, we are exiting the area at the middle border field.
    // But when it comes to the right side (if it was checked), it is necessary, otherwise we can detect a C-shape with the live end as in 213.
    if ((InTakenRel2(2, 0) || InBorderRelExact2(2, 0)) && !InTakenRel2(1, 0) && !InBorderRelExact2(1, 0))
    {
        // T("CheckNearFieldSmall2 C-Shape, left side");
        ret = true;

        newExitField0 = new int[] { x2 + lx2, y2 + ly2 };
        newExitField = new int[] { x2 + lx2 + sx2, y2 + ly2 + sy2 };
        newDirectionRotated = false;
        //sequenceLeftObstacleIndex = InTakenIndexRel2(2, 0); example needed
    }

    //C-Shape up
    if (InTakenRel2(0, 2) && InTakenRel2(1, 1) && !InTakenRel2(0, 1))
    {
        // T("CheckNearFieldSmall2 C-Shape up");
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
            // T("CheckNearFieldSmall2 close mid across");
            ret = true;

            // mid across overwrites C-shape
            newExitField = new int[] { x2 + sx2, y2 + sy2 };
            newDirectionRotated = true;
            sequenceLeftObstacleIndex = middleIndex; // 0811_2
        }
    }

    // close across. Checking empty fields necessary, see 29558469
    if (InTakenRel2(2, 2) && !InTakenRel2(1, 2) && !InTakenRel2(2, 1))
    {
        int middleIndex = InTakenIndexRel2(2, 2);
        int sideIndex = InTakenIndexRel2(3, 2);

        if (sideIndex > middleIndex)
        {
            // T("CheckNearFieldSmall2 close across");
            ret = true;

            newExitField = new int[] { x2 + lx2 + sx2, y2 + ly2 + sy2 };
            newDirectionRotated = true;
            sequenceLeftObstacleIndex = middleIndex; // 0811_1
        }
    }

    return ret;
}

bool CheckNearFieldSmall3()
{
    bool ret = false;

    // 0811
    // C-shape
    if ((InTakenRel2(2, 0) || InBorderRelExact2(2, 0)) && InTakenRel2(1, -1) && !InTakenRel2(1, 0) && !InBorderRelExact2(1, 0))
    {
        // T("CheckNearFieldSmall3 C-Shape");
        ret = true;

        // newexitfield not necessary for now, Sequence will be true.
    }

    // 0722
    // close mid across
    if (InTakenRel2(1, 2) && !InTakenRel2(0, 2) && !InTakenRel2(1, 1))
    {
        int middleIndex = InTakenIndexRel2(1, 2);
        int sideIndex = InTakenIndexRel2(2, 2);

        if (sideIndex > middleIndex)
        {
            // T("CheckNearFieldSmall3 close mid across");
            ret = true;

            // mid across overwrites C-shape
            newExitField = new int[] { x2 + sx2, y2 + sy2 };
            newDirectionRotated = true;
        }
    }

    return ret;
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
// ----- copy end -----

void ResetExamAreas() { }

void ResetExamAreas2() { }

void AddExamAreas(bool secondaryArea = false) { }

/* ----- Count Area ----- */

bool CountAreaRel(int left1, int straight1, int left2, int straight2, List<int[]>? borderFields, bool circleDirectionLeft, int circleType, bool getInfo = false)
{
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

    return CountArea(x1, y1, x2, y2, absBorderFields, circleDirectionLeft, circleType, getInfo);
}

bool CountAreaRel2(int left1, int straight1, int left2, int straight2, List<int[]>? borderFields, bool circleDirectionLeft, int circleType, bool getInfo = false)
{
    // T("CountAreaRel2 " + left1 + " " + straight1 + " " + left2 + " " + straight2);
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

    return CountArea(x_1, y_1, x_2, y_2, absBorderFields, circleDirectionLeft, circleType, getInfo);
}

bool CountArea(int startX, int startY, int endX, int endY, List<int[]>? borderFields, bool circleDirectionLeft, int circleType, bool getInfo = false)
// compareColors is for the starting situation of 1119, where we mark an impair area and know the entry and the exit field. We count the number of white and black cells of a checkered pattern, the color of the entry and exit should be one more tchan the other color.
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
            if (debug) L("Adding border " + middleX + " " + middleY);

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
            if (debug) L("Adding border " + field[0] + " " + field[1]);
        }
        xDiff = startX - borderFields[borderFields.Count - 1][0];
        yDiff = startY - borderFields[borderFields.Count - 1][1];
    }

    areaLine.Add(new int[] { startX, startY });
    if (debug) L("Adding start " + startX + " " + startY);

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
    // second condition is needed in case of 2024_0411_1 where future possibility creates a 2-field area
    if (x == nextX + xDiff && y == nextY + yDiff || InTaken(nextX + xDiff, nextY + yDiff) || x == nextX + xDiff + directions[turnedDirection][0] && y == nextY + yDiff + directions[turnedDirection][1])
    {
        currentDirection = turnedDirection;
    }

    // T("currentDirection: " + currentDirection + ", " + (nextX + directions[currentDirection][0]) + " " + (nextY + directions[currentDirection][1]) + " path: " + InTaken(nextX + directions[currentDirection][0], nextY + directions[currentDirection][1]));

    // In case of an area of 2, 3 or a longer column
    if (InTaken(nextX + directions[currentDirection][0], nextY + directions[currentDirection][1]) || InBorder(nextX + directions[currentDirection][0], nextY + directions[currentDirection][1]))
    {
        currentDirection = currentDirection == 0 ? 3 : currentDirection - 1;
    }
    nextX += directions[currentDirection][0];
    nextY += directions[currentDirection][1];

    areaLine.Add(new int[] { nextX, nextY });
    if (debug) L("Adding continued " + nextX + " " + nextY);

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
                errorInWalkthrough = true;
                errorString = "Countarea error.";
                criticalError = true;
                return false;
            }
        }

        currentDirection = i;

        nextX = possibleNextX;
        nextY = possibleNextY;

        // when getting info about area
        if (nextX == size && nextY == size)
        {
            errorInWalkthrough = true;
            errorString = "Corner is reached."; 
            criticalError = true;            
            return false;
        }

        areaLine.Add(new int[] { nextX, nextY });

        if (areaLine.Count == size * size)
        {
            errorInWalkthrough = true;
            errorString = "Area walkthrough error.";
            criticalError = true;            
            return false;
        }

        if (debug) L("Adding " + nextX + " " + nextY + " count " + areaLine.Count);

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
        L("minY " + minY + " limitX " + limitX + " startIndex " + startIndex);
        foreach (int[] a in areaLine)
        {
            T(a[0] + " " + a[1]);
        }
    }

    int area = 0;
    List<int[]> startSquares = new List<int[]>();
    List<int[]> endSquares = new List<int[]>();

    if (areaLine.Count > 2)
    {
        int[] startCandidate = new int[] { limitX, minY };
        int[] endCandidate = new int[] { limitX, minY };

        if (debug2) L("arealine start " + startCandidate[0] + " " + startCandidate[1]);

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

                if (debug2) L("field x " + field[0] + " y " + field[1] + " currentY " + currentY + " startCandidate " + startCandidate[0] + " " + startCandidate[1] + " endCandidate " + endCandidate[0] + " " + endCandidate[1]);

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

        int eCount = endSquares.Count;

        // it should never happen if the above algorithm is bug-free.
        if (startSquares.Count != eCount)
        {
            foreach (int[] f in startSquares)
            {
                L("startSquares " + f[0] + " " + f[1]);
            }
            foreach (int[] f in endSquares)
            {
                L("endSquares " + f[0] + " " + f[1]);
            }

            errorInWalkthrough = true;
            errorString = "Count of start and end squares are inequal: " + startSquares.Count + " " + eCount;
            criticalError = true;
            
            return false;
        }

        for (int i = 0; i < eCount; i++)
        {
            area += endSquares[i][0] - startSquares[i][0] + 1;
        }
    }
    else area = areaLine.Count;

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

/* ----- Field checking ----- */

bool AddForbidden(int left, int straight)
{
    if (!InTakenRel(left, straight))
    {
        forbidden.Add(new int[] { x + left * lx + straight * sx, y + left * ly + straight * sy });
        return true;
    }
    else return false;
}

bool InBorderAbs(int[] field)
{
    int x = field[0];
    int y = field[1];
    return InBorder(x, y);
}

bool InBorderRel(int left, int straight)
{
    int x0 = x + left * lx + straight * sx;
    int y0 = y + left * ly + straight * sy;
    return InBorder(x0, y0);
}

bool InBorderRel2(int left, int straight)
{
    int x = x2 + left * lx2 + straight * sx2;
    int y = y2 + left * ly2 + straight * sy2;
    return InBorder(x, y);
}

bool InBorder(int x, int y) // allowing negative values could cause an error in AddFutureLines 2x2 checking, but it is necessary in CheckLeftRightCorner due to possibility checking
{
    if (x <= 0 || x >= size + 1 || y <= 0 || y >= size + 1) return true;
    return false;
}

bool InBorderRelExact(int left, int straight)
{
    int x0 = x + left * lx + straight * sx;
    int y0 = y + left * ly + straight * sy;
    return InBorderExact(x0, y0);
}

bool InBorderRelExact2(int left, int straight)
{
    int x0 = x2 + left * lx2 + straight * sx2;
    int y0 = y2 + left * ly2 + straight * sy2;
    return InBorderExact(x0, y0);
}

bool InBorderExact(int x, int y) // strict mode
{
    if (x == 0 || x == size + 1 || y == 0 || y == size + 1) return true;
    return false;
}

bool InTakenAbs(int[] field0)
{
    int x0 = field0[0];
    int y0 = field0[1];

    return InTaken(x0, y0);
}

bool InTakenRel(int left, int straight)
{
    int x0 = x + left * lx + straight * sx;
    int y0 = y + left * ly + straight * sy;

    return InTaken(x0, y0);
}

bool InTakenRel2(int left, int straight)
{
    int x = x2 + left * lx2 + straight * sx2;
    int y = y2 + left * ly2 + straight * sy2;

    return InTaken(x, y);
}

bool InTaken(int x, int y) //more recent fields are more probable to encounter, so this way processing time is optimized
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

    return false;
}

bool InCornerRel(int left, int straight)
{
    int x0 = x + left * lx + straight * sx;
    int y0 = y + left * ly + straight * sy;
    if (x0 == size && y0 == size) return true;
    return false;
}

bool InCornerRel2(int left, int straight)
{
    int x0 = x2 + left * lx2 + straight * sx2;
    int y0 = y2 + left * ly2 + straight * sy2;
    if (x0 == size && y0 == size) return true;
    return false;
}

int InBorderIndexRel(int left, int straight)
{
    int x0 = x + left * lx + straight * sx;
    int y0 = y + left * ly + straight * sy;
    return x0 + y0;
}


int InTakenIndexRel(int left, int straight) // relative position
{
    int x0 = x + left * lx + straight * sx;
    int y0 = y + left * ly + straight * sy;
    return InTakenIndex(x0, y0);
}

int InTakenIndexRel2(int left, int straight) // relative position
{
    int x = x2 + left * lx2 + straight * sx2;
    int y = y2 + left * ly2 + straight * sy2;
    return InTakenIndex(x, y);
}

int InTakenIndex(int x, int y)
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

bool InForbidden(int[] value)
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