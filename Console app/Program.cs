using System.Diagnostics;

bool debug = false, debug2 = false;
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
List<object> info = new();

// control rules declaration -->
bool closeStraightSmall, closeMidAcrossSmall, closeAcrossSmall, closeStraightLarge, closeMidAcrossLarge, closeAcrossLarge = false;
int Straight3I = -1; // used for checking Down Stair and Double Area first case rotated at the next step.
int Straight3J = -1;

bool DoubleArea1, DoubleArea2, DoubleArea3, DoubleArea4, DoubleArea1Rotated, Sequence1, Sequence2, Sequence3, DownStairClose, DownStair, DoubleAreaFirstCaseRotatedNext, DownStairNext = false;

int[] newExitField = new int[] { 0, 0 };
bool newDirectionRotated = false; // if rotated, it is CW on left side
List<int[]> startForbiddenFields = new();
List<string> activeRules = new();
List<List<int[]>> activeRulesForbiddenFields = new();
List<int[]> activeRuleSizes = new();
// <-- control rules declaration

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
                    if (completedCount % 1000 == 0)
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
                            errorInWalkthrough = true;
                            criticalError = true;
                            errorString = "Results different.";
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

                    if (!makeStats)
                    {
                        ShowActiveRules(activeRules, activeRulesForbiddenFields, startForbiddenFields, activeRuleSizes);
                    }
                    break;
                }
            }
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
void ApplyRules()
{
    debug = false;
    debug2 = false;

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

    // T("StairAtStartConvexIn2 " + ShowForbidden());
    StairAtStartConvexIn2();
    // T("StairAtStartConvexIn3 " + ShowForbidden());
    StairAtStartConvexIn3();
    // T("StairAtStartConvexIn4 " + ShowForbidden());
    StairAtStartConvexIn4();
    // T("StairAtStartConvexStraight3 " + ShowForbidden());
    StairAtStartConvexStraight3();
    // T("StairAtStartConvexStraight4 " + ShowForbidden());
    StairAtStartConvexStraight4();
    // T("StairAtStartConvexStraight5 " + ShowForbidden());
    StairAtStartConvexStraight5();

    //T("StairAtStartConcaveStraight3 " + ShowForbidden());
    //StairAtStartConcaveStraight3();

    // T("StairAtEndConvexIn2 " + ShowForbidden());
    StairAtEndConvexIn2();
    // T("StairAtEndConvexIn3 " + ShowForbidden());
    StairAtEndConvexIn3();
    // T("StairAtEndConvexIn4 " + ShowForbidden());
    StairAtEndConvexIn4();
    // T("StairAtEndConvexStraight3 " + ShowForbidden());
    StairAtEndConvexStraight3();
    // T("StairAtEndConvexStraight4 " + ShowForbidden());
    StairAtEndConvexStraight4();
    // T("StairAtEndConvexOut4 " + ShowForbidden());
    StairAtEndConvexOut4(); // 2025_0525_1

    // T("StairAtEndConcaveIn2 " + ShowForbidden());
    StairAtEndConcaveIn2();
    // T("StairAtEndConcaveIn3 " + ShowForbidden());
    StairAtEndConcaveIn3();
    // T("StairAtEndConcaveIn4 " + ShowForbidden());
    StairAtEndConcaveIn4(); // 0814, ...
    // T("StairAtEndConcaveIn5 " + ShowForbidden());
    StairAtEndConcaveIn5(); // 2024_0714
    // T("StairAtEndConcaveStraight3 " + ShowForbidden());
    StairAtEndConcaveStraight3(); // 2025_0527 open corner
    // T("StairAtEndConcaveStraight4 " + ShowForbidden());
    StairAtEndConcaveStraight4();
    // T("StairAtEndConcaveStraight5 " + ShowForbidden());
    StairAtEndConcaveStraight5(); // 2025_0525
    // T("StairAtEndConcaveStraight6 " + ShowForbidden());
    StairAtEndConcaveStraight6(); // 2025_0525
    // T("StairAtEndConcaveOut3 " + ShowForbidden());
    StairAtEndConcaveOut3(); // 2024_0811
    // T("StairAtEndConcaveOut5 " + ShowForbidden());
    StairAtEndConcaveOut5(); // 2026_0304_2, 2026_0304_6

    // T("DoubleStair " + ShowForbidden());
    DoubleStair();
    // T("DoubleStairReversed " + ShowForbidden());
    DoubleStairReversed();
    // T("StairAtEnd3Obtacles1 " + ShowForbidden());
    StairAtEnd3Obtacles1(); // 0725_4, 0731_1
    // T("Stair3x3 " + ShowForbidden());
    Stair3x3();
    // T("RemoteStair " + ShowForbidden());
    RemoteStair();
    // T("Sequence " + ShowForbidden());
    Sequence();
}

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

        for (int j = 0; j < 4; j++) // rotate CW
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
                                                if (CheckCorner(1, 2, 0, 2, circleDirectionLeft, true))
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

                                                if (CheckCorner(1, vert - 2, 0, 2, circleDirectionLeft, true))
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

                                        if (CheckCorner(1, 2, 0, 2, circleDirectionLeft, true))
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

                                        if (CheckCorner(1, 2, 0, 2, circleDirectionLeft, true))
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

                                        if (CheckCorner(0, 3, 0, 2, circleDirectionLeft, true))
                                        {
                                            for (m = hori - 1; m >= 1; m--)
                                            {
                                                path.RemoveAt(path.Count - 1);
                                            }

                                            ruleTrue = true;
                                            // T("Corner y = x + 3 return stair second obstacle: Cannot step straight");
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
                                        // if (CheckCorner(hori - 2, vert, 1, 0, circleDirectionLeft, true))
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

                                    // 2025_0522: Area has a maximum allowed of white fields. There is an across obstacle at the bottom of the stair edge
                                    if ((hori - vert) % 4 == 3 && whiteDiff == laterWCount && CheckNearFieldSmallRel1(hori - vert + 1, 1, 0, 1, true))
                                    {
                                        // T("LeftRightCorner close obstacle inside " + i + " " + j + ": Cannot enter later");

                                        // AddExamAreas();

                                        AddForbidden(0, 1);
                                        // for small area
                                        if (j == 0)
                                        {
                                            AddForbidden(-1, 0);
                                        }
                                    }

                                    // 2025_0527_1: close obstacles inside or outside
                                    // j = 2 rotation might be possible, but we need an example of it
                                    if (hori == vert && hori >= 4 && j == 1 && whiteDiff == 0 && CheckNearFieldSmallRel0(1, 0, 0, 1, true) && CheckNearFieldSmallRel0(2, 2, 0, 2, true))
                                    {
                                        // T("LeftRightCorner close obstacle inside outside " + i + " " + j + ": Cannot step left");

                                        AddForbidden(1, 0);
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

void StairAtStartConvexIn2()
// 2024_0727_2: mid across left, across right
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
                // T("StairAtStartConvexIn2 distance " + (dist - 1), "side " + i, "rotation " + j);

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
                            // T("border field " + borderFields[k][0] + " " + borderFields[k][1]);
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
                                    // T("StairAtStartConvexIn2 at " + hori + " " + vert + ": Cannot step straight");
                                    RemoveEnd(counter);
                                    AddForbidden(0, 1);

                                    if (j == 0)
                                    {
                                        // T("StairAtStartConvexIn2 at " + hori + " " + vert + ": Cannot step left");
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
// 0725_5: mid across down, mid across up
// 0726_1: across, mid across
// 0726_2: mid across, area

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
                // T("StairAtStartConvexIn3 distance " + (dist - 1), "side " + i, "rotation " + j);

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
                            // T("border field " + borderFields[k][0] + " " + borderFields[k][1]);
                            newBorderFields.Add(borderFields[k]);
                        }

                        ResetExamAreas();

                        if (CountAreaRel(-1, 1, hori - 1, vert - 1, newBorderFields, circleDirectionLeft, 2, true))
                        {
                            int black = (int)info[1];
                            int white = (int)info[2];

                            // T("b " + black + " w " + white);

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
                                    // T("StairAtStartConvexIn3 at " + hori + " " + vert + ": Cannot step straight");
                                    AddForbidden(0, 1);

                                    if (j == 0)
                                    {
                                        // T("StairAtStartConvexIn3 at " + hori + " " + vert + ": Cannot step left");
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
                                // T("StairAtStartConvexIn3 at " + hori + " " + vert + ": Cannot step right");
                                AddForbidden(-1, 0);

                                if (j == 1)
                                {
                                    // T("StairAtStartConvexIn3 at " + hori + " " + vert + ": Cannot step down");
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
// 2024_0710: area
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
                // T("StairAtStartConvexStraight3 distance " + (dist - 1), "side " + i, "rotation " + j);

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
                            // T("border field " + borderFields[k][0] + " " + borderFields[k][1]);
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
                                    // T("StairAtStartConvexStraight3 at " + hori + " " + vert + ": Cannot step straight");

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
// 2024_0610_4, 2024_0610_5, 121670752, 0627: 1B
// 2024_0725, 2024_0727_4: start obstacle as well
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
                // T("StairAtStartConvexStraight4 distance " + (dist - 1), "side " + i, "rotation " + j);

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
                                    // T("Adding " + (x + (k - 4) * lx + k * sx) + " " + (y + (k - 4) * ly + k * sy));
                                }
                                int counter = hori;

                                if (CheckCorner(0, 4, 0, 2, circleDirectionLeft, true))
                                {
                                    // we cannot enter now straight
                                    if (CheckNearFieldSmallRel1(0, 2, 1, 1, false))
                                    {
                                        AddExamAreas();
                                        // T("StairAtStartConvexStraight4 1W start obstacle at " + hori + " " + vert + ": Cannot step straight");
                                        RemoveEnd(counter);
                                        AddForbidden(0, 1);
                                    }
                                    else
                                    {
                                        AddExamAreas();
                                        // T("StairAtStartConvexStraight4 1W at " + hori + " " + vert + ": Cannot step right and down");
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
                                    // T("StairAtStartConvexStraight4 1B at " + hori + " " + vert + ": Cannot step straight");
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
// 2024_0727_3; start obstacle outside
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
                // T("StairAtStartConvexStraight5 distance " + (dist - 1), "side " + i, "rotation " + j);

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
                            // T("border field " + borderFields[k][0] + " " + borderFields[k][1]);
                            newBorderFields.Add(borderFields[k]);
                        }

                        ResetExamAreas();

                        if (CountAreaRel(0, 1, hori - 1, vert - 1, newBorderFields, circleDirectionLeft, 3, true))
                        {
                            int black = (int)info[1];
                            int white = (int)info[2];

                            // T("b " + black + " w " + white);

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
                                        // T("StairAtStartConvexStraight5 start obstacle at " + hori + " " + vert + ": Cannot step straight");
                                        RemoveEnd(counter);
                                        AddForbidden(0, 1);
                                    }
                                    else
                                    {
                                        AddExamAreas();
                                        // T("StairAtStartConvexStraight5 at " + hori + " " + vert + ": Cannot step right and down");
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

void StairAtStartConcaveStraight3()
// 2026_0302_7: C-shape inside at stair end
// CCW, cannot enter later or now up
{
    for (int i = 0; i < 2; i++)
    {
        bool circleDirectionLeft = (i == 0) ? false : true;

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

            if (dist > 4 && dist < size)
            {
                // T("StairAtStartConcaveStraight3 distance " + (dist - 1), "side " + i, "rotation " + j);

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

                    if (i1 > i2)
                    {
                        List<int[]> borderFields = new();
                        for (int k = 2; k <= vert - 1; k++)
                        {
                            if (k <= 3)
                            {
                                borderFields.Add(new int[] { 0, k });
                            }
                            else if (k <= vert - 2)
                            {
                                borderFields.Add(new int[] { k - 4, k });
                                borderFields.Add(new int[] { k - 3, k });
                            }
                            else if (hori > 1)
                            {
                                borderFields.Add(new int[] { k - 4, k });
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

                            // T("b " + black + " w " + white);

                            if (white - black == hori && CheckNearFieldSmallRel(hori - 1, vert - 1, 0, 2, false))
                            {
                                AddExamAreas();
                                // T("StairAtStartConcaveStraight3 at " + hori + " " + vert + ": Cannot step straight");
                                AddForbidden(0, 1);

                                if (j == 0)
                                {
                                    // T("StairAtStartConcaveStraight3 at " + hori + " " + vert + ": Cannot step left");
                                    AddForbidden(1, 0);
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
                // T("StairAtEndConvexIn2 distance " + (dist - 1), "side " + i, "rotation " + j);

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

                                // T("b " + black + " w " + white);

                                if (black - white == vert && (CheckNearFieldSmallRel(hori - 1, vert + 1, 0, 0, true) || CheckCorner(hori - 1, vert + 1, 0, 0, circleDirectionLeft, true)))
                                {
                                    if (CheckNearFieldSmallRel1(hori - 1, vert + 1, 1, 0, true))
                                    {
                                        AddExamAreas();
                                        // T("StairAtEndConvexIn2 vB at " + hori + " " + vert + ": Cannot step left");
                                        AddForbidden(1, 0);

                                        if (j == 1)
                                        {
                                            // T("StairAtEndConvexIn2 vB case 1 at " + hori + " " + vert + ": Cannot step down");
                                            AddForbidden(0, -1);
                                        }
                                    }
                                    // 9_22325 shows that dist must be greater than 3
                                    else if (dist > 3 && InTakenRel(-1, 0) && (InTakenRel(-2, 1) || InBorderRel(-2, 1)) && (InTakenRel(-2, 2) || InBorderRel(-2, 2)) && (InTakenRel(-2, 3) || InBorderRel(-2, 3)) && !InTakenRel(-1, 1) && !InTakenRel(-1, 3) && CheckNearFieldSmallRel0(0, 3, 0, 0, true))
                                    {
                                        AddExamAreas();
                                        // T("StairAtEndConvexIn2 vB case 2 at " + hori + " " + vert + ": Cannot step straight");
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
                // T("StairAtEndConvexIn3 distance " + (dist - 1), "side " + i, "rotation " + j);

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
                                            // T("StairAtEndConvexIn3 E at " + hori + " " + vert + ": Cannot step straight");
                                            RemoveEnd(1);
                                            AddForbidden(0, 1);

                                            if (j == 0)
                                            {
                                                // T("StairAtEndConvexIn3 E at " + hori + " " + vert + ": Cannot step right");
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
                                                // T("StairAtEndConvexIn3 E 3 obstacles at " + hori + " " + vert + ": Cannot step straight");
                                                RemoveEnd(1);
                                                AddForbidden(0, 1);

                                                if (j == 0)
                                                {
                                                    // T("StairAtEndConvexIn3 E 3 obstacles at " + hori + " " + vert + ": Cannot step right");
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
                                    // T("StairAtEndConvexIn3 b = w + vert + 1 at " + hori + " " + vert + ": Cannot step left");
                                    AddForbidden(1, 0);

                                    if (j == 1)
                                    {
                                        // T("StairAtEndConvexIn3 b = w + vert + 1 at " + hori + " " + vert + ": Cannot step down");
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
                // T("StairAtEndConvexIn4 distance " + (dist - 1), "side " + i, "rotation " + j);

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
                                // T("border field " + borderFields[k][0] + " " + borderFields[k][1]);
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
                                        // T("StairAtEndConvexIn4 1W at " + hori + " " + vert + ": Cannot step straight");
                                        RemoveEnd(1);
                                        AddForbidden(0, 1);

                                        if (j == 0)
                                        {
                                            // T("StairAtEndConvexIn4 1W at " + hori + " " + vert + ": Cannot step right");
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
// 0916 across
// 665575 mid across
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

                // T("StairAtEndConvexStraight3 distance " + (dist - 1), "side " + i, "rotation " + j);

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
                            // T("border field " + borderFields[k][0] + " " + borderFields[k][1]);
                            newBorderFields.Add(borderFields[k]);
                        }

                        ResetExamAreas();

                        if (CountAreaRel(1, 1, hori - 1, vert, newBorderFields, circleDirectionLeft, 2, true))
                        {
                            int black = (int)info[1];
                            int white = (int)info[2];

                            // T("b " + black + " w " + white);

                            if (black - white == vert)
                            {
                                AddEndClose(vert, 1, 0);
                                int counter = vert;

                                if (CheckCorner(hori - 2, vert, 1, 0, circleDirectionLeft, true))
                                {
                                    AddExamAreas();
                                    // T("StairAtEndConvexStraight3 at " + hori + " " + vert + ": Cannot step left and down");
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

                // T("StairAtEndConvexStraight4 distance " + (dist - 1), "side " + i, "rotation " + j);

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
                            // T("border field " + borderFields[k][0] + " " + borderFields[k][1]);
                            newBorderFields.Add(borderFields[k]);
                        }

                        ResetExamAreas();

                        if (CountAreaRel(1, 1, hori - 1, vert, newBorderFields, circleDirectionLeft, 2, true))
                        {
                            int black = (int)info[1];
                            int white = (int)info[2];

                            // T("b " + black + " w " + white);

                            if (white - black == 1 && CheckNearFieldSmallRel1(vert, vert, 1, 0, false))
                            {
                                AddExamAreas();
                                // T("StairAtEndConvexStraight4 at " + hori + " " + vert + ": Cannot step straight and right");
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
                // T("StairAtEndConvexOut4 distance " + (dist - 1), "side " + i, "rotation " + j);

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

                                    // T("StairAtEndConvexOut4 at " + hori + " " + vert + ": Cannot step straight");
                                    AddForbidden(0, 1);

                                    if (j == 0)
                                    {
                                        // T("StairAtEndConvexOut4 at " + hori + " " + vert + ": Cannot step right");
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
// 2024_0717_4, 0729_2
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
                // T("StairAtEndConcaveIn2 distance " + (dist - 1), "side " + i, "rotation " + j);

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
                                // T("border field " + borderFields[k][0] + " " + borderFields[k][1]);
                                newBorderFields.Add(borderFields[k]);
                            }

                            ResetExamAreas();

                            if (CountAreaRel(1, 1, hori - 1, vert + 1, newBorderFields, circleDirectionLeft, 2, true))
                            {
                                int black = (int)info[1];
                                int white = (int)info[2];

                                // T("b " + black + " w " + white);

                                if (white == black + 1 && CheckNearFieldSmallRel1(hori - 3, hori - 2, 1, 0, true) && CheckNearFieldSmallRel1(hori - 1, hori - 2, 0, 0, false))
                                {
                                    AddExamAreas();
                                    // T("StairAtEndConcaveIn2 at " + hori + " " + vert + ": Cannot step straight");
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
                // T("StairAtEndConcaveIn3 distance " + (dist - 1), "side " + i, "rotation " + j);

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
                                // T("border field " + borderFields[k][0] + " " + borderFields[k][1]);
                                newBorderFields.Add(borderFields[k]);
                            }

                            ResetExamAreas();

                            if (CountAreaRel(1, 1, hori - 1, vert + 1, newBorderFields, circleDirectionLeft, 2, true))
                            {
                                int black = (int)info[1];
                                int white = (int)info[2];

                                // T("b " + black + " w " + white);

                                if (white == black + 1 && CheckNearFieldSmallRel1(hori - 3, hori - 3, 1, 0, true) && CheckNearFieldSmallRel1(hori - 2, hori - 3, 0, 0, false))
                                {
                                    AddExamAreas();
                                    // T("StairAtEndConcaveIn3 at " + hori + " " + vert + ": Cannot step straight");
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
                // T("StairAtEndConcaveIn4 distance " + (dist - 1), "side " + i, "rotation " + j);

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
                                    // T("StairAtEndConcaveIn4 at " + hori + " " + vert + ": Cannot step left");
                                    AddForbidden(1, 0);

                                    if (j == 1)
                                    {
                                        // T("StairAtEndConcaveIn4 at " + hori + " " + vert + ": Cannot step down");
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
                // T("StairAtEndConcaveIn5 distance " + (dist - 1), "side " + i, "rotation " + j);

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

                                // T("b " + black + " w " + white);

                                if (white == black + 1 && CheckNearFieldSmallRel0(hori - 2, vert + 1, 0, 0, false) && CheckNearFieldSmallRel1(hori - 4, vert + 1, 1, 0, true))
                                {
                                    AddExamAreas();
                                    // T("StairAtEndConcaveIn5 1W at " + hori + " " + vert + ": Cannot step left");
                                    AddForbidden(1, 0);

                                    if (j == 1)
                                    {
                                        // T("StairAtEndConcaveIn5 1W at " + hori + " " + vert + ": Cannot step down");
                                        AddForbidden(0, -1);
                                    }
                                }
                                else if (black == white + vert + 1 && CheckNearFieldSmallRel0(hori - 1, vert + 1, 0, 0, false) && CheckNearFieldSmallRel0(hori - 3, vert + 1, 1, 0, true) && CheckNearFieldSmallRel1(hori - 4, vert + 1, 1, 3, true))
                                {
                                    AddExamAreas();
                                    // T("StairAtEndConcaveIn5 (v+1)B at " + hori + " " + vert + ": Cannot step straight");
                                    AddForbidden(0, 1);

                                    if (j == 0)
                                    {
                                        // T("StairAtEndConcaveIn5 (v+1)B at " + hori + " " + vert + ": Cannot step right");
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
// 2024_0619, 2024_0717_2, 2025_0527, 2026_0302_4, 2026_0302_5
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

                // T("StairAtEndConcaveStraight3 distance " + (dist - 1), "side " + i, "rotation " + j);

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
                                        // T("StairAtEndConcaveStraight3 at " + hori + " " + vert + ": Cannot step straight");
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
                // T("StairAtEndConcaveStraight4 distance " + (dist - 1), "side " + i, "rotation " + j);

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
                                    // T("StairAtEndConcaveStraight4 at " + hori + " " + vert + ": Cannot step straight");
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
    // 2024_0818, 2025_0720_1, 2026_0301, 2026_0302_1, 2026_0304_7: obstacle is straight wall
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

                // T("StairAtEndConcaveStraight5 distance " + (dist - 1), "side " + i, "rotation " + j);

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
                                        // T("StairAtEndConcaveStraight5 at " + hori + " " + vert + ": Cannot step left and down");

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

                // T("StairAtEndConcaveStraight6 distance " + (dist - 1), "side " + i, "rotation " + j);

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
                                // T("border field " + borderFields[k][0] + " " + borderFields[k][1]);
                                newBorderFields.Add(borderFields[k]);
                            }

                            ResetExamAreas();

                            if (CountAreaRel(1, 1, hori - 1, vert, newBorderFields, circleDirectionLeft, 2, true))
                            {
                                int black = (int)info[1];
                                int white = (int)info[2];

                                // T("b " + black + " w " + white);
                                T(CheckNearFieldSmallRel0(vert + 2, vert, 1, 0, true));

                                if (black - white == vert && CheckNearFieldSmallRel0(vert + 2, vert, 1, 0, true))
                                {
                                    path.Add(new int[] { x + (vert + 1) * lx + (vert + 1) * sx, y + (vert + 1) * ly + (vert + 1) * sy });
                                    path.Add(new int[] { x + (vert + 1) * lx + vert * sx, y + (vert + 1) * ly + vert * sy });
                                    int counter = 2;

                                    if (CheckCorner(vert + 1, vert, 1, 3, !circleDirectionLeft, true))
                                    {
                                        AddExamAreas();
                                        // T("StairAtEndConcaveStraight6 at " + hori + " " + vert + ": Cannot step straight");

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
                // T("StairAtEndConcaveOut3 distance " + (dist - 1), "side " + i, "rotation " + j);

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
                                    // T("StairAtEndConcaveOut3 at " + hori + " " + vert + ": Cannot step straight");
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

            // T("StairAtEndConcaveOut5 dist " + dist);

            if (dist >= 6 && dist < size)
            {
                // T("StairAtEndConcaveOut5 distance " + (dist - 1), "side " + i, "rotation " + j);

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
                            // T("border field " + borderFields[k][0] + " " + borderFields[k][1]);
                            newBorderFields.Add(borderFields[k]);
                        }

                        ResetExamAreas();

                        if (CountAreaRel(1, 1, hori - 1, vert - 1, newBorderFields, circleDirectionLeft, 2, true))
                        {
                            int black = (int)info[1];
                            int white = (int)info[2];

                            // T("b w " + black + " " + white);

                            if (white == black + 1 && (CheckNearFieldSmallRel0(hori - 1, vert - 1, 1, 3, true) || CheckNearFieldSmallRel1(hori - 4, vert - 1, 1, 0, true)))
                            {
                                AddExamAreas();
                                // T("StairAtEndConcaveOut5 at " + hori + " " + vert + ": Cannot step left and down");
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
// Also Sequence2: 2024_0516_4, 2024_0516_5, 2024_0727_5
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
                        // T("Double stair corners found at", i, j);

                        // either stair on both sides of the two corners (2024_0706_1) or close obstacle (2024_0516_4) or area (2024_0727_5)
                        AddEndFar(1, 2, 2);
                        bool circleDirectionLeft = (i == 0) ? true : false;

                        if ((CheckNearFieldSmallRel(2, 2, 0, 2, false) || CheckCorner(2, 2, 0, 2, circleDirectionLeft, false)) && CheckNearFieldSmallRel(3, 1, 1, 3, true))
                        {
                            // T("DoubleStair: Cannot step straight");
                            RemoveEnd(1);
                            AddForbidden(0, 1);

                            if (j == 0)
                            {
                                // T("DoubleStair: Cannot step right");
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
                    // T("Double stair reversed corners found at", i, j);

                    if (InTakenRel(1, -1) && InTakenRel(2, -2) && !InTakenRel(1, 0) && !InTakenRel(2, -1) && !InTakenRel(3, -2) && CheckNearFieldSmallRel(3, -1, 0, 1, true))
                    {
                        // T("DoubleStairReversed: Cannot step straight");
                        AddForbidden(0, 1);

                        if (j == 0)
                        {
                            // T("DoubleStairReversed: Cannot step right");
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
                    // T("StairAtEnd3Obtacles1 distance " + (dist - 1), "side " + i, "rotation " + j);

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

void Stair3x3() // 0722 / Stair3x3. It is not a nested 3x3 area sequence. 1111 shows, even if we step down, there will be two-way choice later.
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
                            // T("Stair3x3 at side " + i + " rotation " + j + ": Cannot step left");
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

                                        // T("RemoteStair mid across: Cannot step straight");
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

void Sequence()
// Start at 3, 0. Any combination of stairs and 2-distance straight obstacles are possible. 2 rotations possible.
// 0706, 1001: corner -> StairAtEndConvexStraight3
// [no stair] 1005: mid across
// 2024_0516, 2024_0516_1: one step across -> StairAtEndConvexIn2
// 0516_4, 0516_5: multiple step across -> DoubleStair
// 1006: across on the left at the end of the sequence -> DobuleStair
// 0704, 1014: area on left, close mid across on right -> StairAtEndConvexIn2

// Start at 3, -1. 3 rotations possible.
// [no stair] 0516_6, 0516_7, 0516_8: across
// 2026_0405 -> Next step double area

// Start at 4, 0
// 2024_1115: mid across -> DoubleStair   

// Start at 4, -1
// 0727_5: across, horizontal distance to the first obstacle % 4 = 3 -> DoubleStair
// 2024_0724: left across, right mid across -> StairAtStartConvexIn2
// 2024_0725_2: left area, right mid across -> StairAtStartConvexIn2
// 0727_2: left mid across, right across -> StairAtStartConvexIn2

// Start at stair:
// [no stair] 0630: area
// [no stair] 0720: across left, mid across right
// [no stair] 0723: across

// Obstacle at 3, 0 and 1, -1: Next step to front will create a stair start
// [no stair] 0724_1: across

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
                        // T("Sequence 3 0, side", i, "rotation", j);
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
                        // T("Sequence 3 -1, side", i, "rotation", j);
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
                        // T("Sequence 4 -1, side", i, "rotation", j);
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
                            // T("Sequence 4 0, side", i, "rotation", j);
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
                    // T("Sequence stair, side", i, "rotation", j);
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
                        // T("Sequence stair 2, side", i, "rotation", j);
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

                        if (CheckCorner(hori - 2 * hx, vert - 2 * hy, 0, rotationIndex2, circleDirectionLeft, true) && CheckNearFieldSmallRel0(hori - 2 * hx, vert - 2 * hy, 1, rotationIndex3, true))
                        {
                            AddExamAreas(true);

                            for (int m = 1; m <= counter; m++)
                            {
                                path.RemoveAt(path.Count - 1);
                            }
                            counter = 0;

                            if (!stairStart) // 0704, 1014, 0724, 0725_2, 1006, 2026_0404, 2026_0405
                            {
                                // T("Sequence double area at relative " + (hori - 2 * hx) + " " + (vert - 2 * hy) + ": Cannot step left");
                                AddForbidden(1, 0);

                                // down direction should not be disabled: 2026_0405
                            }
                            else // 0720
                            {
                                // T("Sequence double area stair start at relative " + (hori - 2 * hx) + " " + (vert - 2 * hy) + ": Cannot step straight");
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
                            // T("Sequence double C-shape at relative " + (hori - 2 * hx) + " " + (vert - 2 * hy) + " stairStart2: Cannot step straight");
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
                                    // T("Sequence at relative " + (hori - 2 * hx) + " " + (vert - 2 * hy) + ": Cannot step left");
                                    AddForbidden(1, 0);

                                    if (j == 1 && !vertLow)
                                    {
                                        // T("Sequence at relative " + (hori - 2 * hx) + " " + (vert - 2 * hy) + ": Cannot step down");
                                        AddForbidden(0, -1);
                                    }
                                }
                                else if (stairStart)
                                {
                                    // T("Sequence at relative " + (hori - 2 * hx) + " " + (vert - 2 * hy) + " stairStart: Cannot step straight");
                                    AddForbidden(0, 1);
                                }
                                else
                                {
                                    // T("Sequence at relative " + (hori - 2 * hx) + " " + (vert - 2 * hy) + " stairStart2: Cannot step straight");
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

    // needed for far left and right case 234320
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

    // Far rules shouldn't be checked until close rules are checked on both sides, see 305112. Here, close straight is only true on the right side, but left side far rules get checked before.
    // A close rule may be true on one side, but on the other side there can be a far rule, like in 1307639. The close rule has to be large in this case.

    // A large close mid across on one side can have a small far across on the other side.
    // A large close across on one side can have a small far mid across / across on the other side.
    // Only the last case needs to be examined. All the other close rules have two fields disabled.

    if (!closeStraightSmall && !closeMidAcrossSmall && !closeAcrossSmall && !closeStraightLarge && !closeMidAcrossLarge)
    {
        for (int i = 0; i < 2; i++)
        {
            bool farStraight = false;
            bool farMidAcross = false;

            if (InTakenRel(0, 3) && InTakenRel(1, 3) && !InTakenRel(0, 2) && !InTakenRel(0, 1)) // 0, 2: 1225; 0, 1: 1226
            {
                farStraight = true;

                int middleIndex = InTakenIndexRel(0, 3);
                int sideIndex = InTakenIndexRel(1, 3);
                if (sideIndex > middleIndex) // area on left
                {
                    if (!InTakenRel(1, 2) && !InTakenRel(2, 2)) // 1,2: 1019_4, 2,2: 1019_5
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
                    if (!InTakenRel(-1, 2) && !InTakenRel(-2, 2)) // -1, 2: 1019_6, -2, 2: 1019_7
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
                if (InTakenRel(1, 3) && InTakenRel(2, 3) && !InTakenRel(0, 2) && !InTakenRel(1, 2) && !InTakenRel(0, 1)) // 0, 2; 1, 2: 1019_3
                {
                    farMidAcross = true;

                    int middleIndex = InTakenIndexRel(1, 3);
                    int sideIndex = InTakenIndexRel(2, 3);
                    if (sideIndex > middleIndex) // area on left
                    {
                        if (!InTakenRel(2, 2)) // 2, 2: 1019
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
                        if (!InTakenRel(-1, 2)) // -1, 2: 1019_1
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

    if (farStraightLeft && farStraightRight) // 9:234256
    {
        forbidden.Add(new int[] { x + sx, y + sy });
    }

    // left/right side rules
    // When any of the close rules are present, even close across large, examining side rules is not necessary. Example: 1019_8
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

                    if (InTakenRel(3, -1) && InTakenRel(3, -2) && !InTakenRel(1, -1) && !InTakenRel(1, 0) && !InTakenRel(1, 1)) // mid across down, 1,1: 1021_8
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
                if (!farSideStraightUp && !farSideMidAcrossUp && InTakenRel(3, 2) && InTakenRel(3, 3) && !InTakenRel(1, 0) && !InTakenRel(1, 1) && !InTakenRel(1, 2)) // 1,2: 1021
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

                if (!farSideStraightDown && !farSideMidAcrossDown && InTakenRel(3, -2) && InTakenRel(3, -3) && !InTakenRel(1, -1) && !InTakenRel(1, 0) && !InTakenRel(1, 1) && !InTakenRel(2, -2)) // 2,-2: 630259
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

            if (farSideUp && farSideDown) // 9:234256
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

void CheckAreaNearBorder() // 0909. Check both straight approach and side.
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
                             // 466
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

void CheckLeftRightCornerBig() // rotate down (CCW): 59438645 for behind and up for small area 
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

void Check3DoubleArea() // the distance to the obstacle is maximum 3. Line cannot finish at the far corner, but at the field below. There is a second area created sith an obstacle on the right side.

// has to be rotated ccw in first area case
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
                                case 1: // 0601
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
    // for now, we only take the right side C-shape into account as it happens in 740 293. Other close obstacles we don't check.
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

                // Both: 18677343

                // Double Area only: 59434452
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

bool CheckNearFieldSmall1() // for use only with Double Area case 1, 2, 3 and 1 rotated, and Down Stair. Across is needed at 53144883
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
        // But when it comes to the right side (if it was checked), it is necessary, otherwise we can detect a C-shape with the live end as in 213.
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

    // close across. Checking empty fields necessary, see 29558469
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

    // T("currentDirection: " + currentDirection + " turnedDirection " + turnedDirection + " nextX + directions[turnedDirection][0] " + (nextX + directions[turnedDirection][0]) + " nextY + directions[turnedDirection][1] " + (nextY + directions[turnedDirection][1]) + " nextX " + nextX + " nextY " + nextY + " x " + x + " y " + y);

    // In case of an area of 2, 3 or a longer column
    if (InTaken(nextX + directions[currentDirection][0], nextY + directions[currentDirection][1]) || InBorder(nextX + directions[currentDirection][0], nextY + directions[currentDirection][1]))
    {
        currentDirection = currentDirection == 0 ? 3 : currentDirection - 1;
    }

    // In case of StairAtEndConvexIn2_1, where we need to change to reverse direction. In case of 2024_0731, both the above and below cunditions are true
    if (x == nextX + directions[turnedDirection][0] && y == nextY + directions[turnedDirection][1])
    {
        currentDirection = (turnedDirection == 0) ? 3 : turnedDirection - 1;
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

void AddEndClose(int counter, int stx, int sty)
// North-west direction
{
    for (int i = 0; i < counter; i++)
    {
        path.Add(new int[] { x + (stx + i) * lx + (sty + i) * sx, y + (stx + i) * ly + (sty + i) * sy });
    }
}

void AddEndFar(int counter, int stx, int sty)
// South-east direction
{
    for (int i = 0; i < counter; i++)
    {
        path.Add(new int[] { x + (stx - i) * lx + (sty - i) * sx, y + (stx - i) * ly + (sty - i) * sy });
    }
}

void RemoveEnd(int counter)
{
    for (int i = 1; i <= counter; i++)
    {
        path.RemoveAt(path.Count - 1);
    }
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