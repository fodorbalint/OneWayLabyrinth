﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Drawing;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;

namespace OneWayLabyrinth
{
	internal class Path
	{
		MainWindow window;
		int size;
		public List<int[]> path;
		public List<int[]>? path2; //additional forbidden fields
		bool isMain = true;
		public int searchStartIndex = -1;
		public bool searchReverse = false;
		List<int[]> foundPath = new List<int[]>();
		public int count;
		public List<int[]> possible = new List<int[]>(); //field coordinates
		List<int[]> forbidden = new List<int[]>();
		public bool nearField;
		public bool circleDirectionLeft = true;
		public int x, y, x2, y2;
		public int[] s = new int[] { 0, 0 }; //straight, left and right coordinates
        public int[] l = new int[] { 0, 0 };
        public int[] r = new int[] { 0, 0 };
        int[] straightField = new int[] { };
		int[] leftField = new int[] { };
		int[] rightField = new int[] { };
		List<int[]> directions = new List<int[]> { new int[] { 0, 1 }, new int[] { 1, 0 }, new int[] { 0, -1 }, new int[] { -1, 0 } }; //down, right, up, left
		int[] selectedDirection = new int[] { };
		bool CShape;

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

		public void NextStepPossibilities(bool searchReverse, int startIndex, int nearSection, int farSection)
		{
			nearField = false;

			possible = new List<int[]>();
			forbidden = new List<int[]>();

			count = path.Count;
			if (count < 2)
			{
				possible.Add(new int[] { 2, 1 });
				possible.Add(new int[] { 1, 2 });
			}
			else
			{
				int x0, y0;
				this.searchReverse = searchReverse;
				if (startIndex != -1) //for checking future lines
				{
					T("NextSteppossibilities startIndex " + startIndex + " path.Count " + path.Count + " sR " + searchReverse);
					if (!searchReverse)
					{
						//extend far end
						x = path[startIndex][0];
						y = path[startIndex][1];
						x0 = path[startIndex + 1][0];
						y0 = path[startIndex + 1][1];
					}
					else
					{
						//extend near end
						x = path[startIndex][0];
						y = path[startIndex][1];
						x0 = path[startIndex - 1][0];
						y0 = path[startIndex - 1][1];
					}

					searchStartIndex = startIndex;
                }
				else
				{
					T("NextSteppossibilities normal x " + x + " y " + y + " path2.Count " + path2.Count);
					searchStartIndex = count - 1;
					x0 = path[count - 2][0];
					y0 = path[count - 2][1];
				}

				int i;
				for (i = 0; i < 4; i++)
				{
					//last movement: down, right, up, left
					s[0] = directions[i][0];
					s[1] = directions[i][1];

					//this shouldn't be needed. But there is a bug in the framework that changes s when l changes below
					int s0 = s[0];
                    int s1 = s[1];

                    if (x - x0 == s[0] && y - y0 == s[1])
					{
                        selectedDirection = directions[i];

                        if (i == 0)
						{
							l[0] = directions[1][0];
							l[1] = directions[1][1];
							r[0] = directions[3][0];
							r[1] = directions[3][1];
						}
						else if (i == 3)
						{
							l[0] = directions[0][0];
							l[1] = directions[0][1];
							r[0] = directions[2][0];
							r[1] = directions[2][1];
						}
						else
						{
                            l[0] = directions[i + 1][0]; // bug: this line changes s[0] too
                            l[1] = directions[i + 1][1]; // bug: this line changes s[1] too
                            r[0] = directions[i - 1][0];
                            r[1] = directions[i - 1][1];
                        }
						s = new int[] { s0, s1 };

                        straightField = new int[] { x + s[0], y + s[1] };
						leftField = new int[] { x + l[0], y + l[1] };
						rightField = new int[] { x + r[0], y + r[1] };

						T("x " + x + " y " + y + " InTakenAbs(straightField) " + InTakenAbs(straightField) + " InTakenAbs(rightField) " + InTakenAbs(rightField) + " InTakenAbs(leftField) " + InTakenAbs(leftField));

						if (!InTakenAllF(straightField) && !InBorderF(straightField))
						{
							T("possible straight");
							possible.Add(straightField);
						}
						if (!InTakenAllF(rightField) && !InBorderF(rightField))
						{
							T("possible right");
							possible.Add(rightField);
						}
						if (!InTakenAllF(leftField) && !InBorderF(leftField))
						{
							T("possible left");
							possible.Add(leftField);
						}

						// A future line may connect to another section as in 0714_2 when we step up, and a 2x2 line is created on the left
						// For connecting to an older line, see 0730
						// It cannot connect to the end of the same section
						if (!isMain)
						{
							if (!searchReverse && InFutureOwnStartF(straightField, nearSection) || searchReverse && InFutureOwnEndF(straightField, farSection))
							{
								T("possible straight 2");
								possible.Add(straightField);
							}
							// See 0803
							if (!searchReverse && InFutureOwnStartF(rightField, nearSection) || searchReverse && InFutureOwnEndF(rightField, farSection))
							{
								T("possible right 2");
								possible.Add(rightField);
							}
							if (!searchReverse && InFutureOwnStartF(leftField, nearSection) || searchReverse && InFutureOwnEndF(leftField, farSection))
							{
								T("possible left 2");
								possible.Add(leftField);
							}
						}
						//See 0804
						else
						{
							if (InFutureStartAbs(straightField))
							{
								T("possible straight 2 main");
								possible.Add(straightField);
							}
							if (InFutureStartAbs(rightField))
							{
								T("possible right 2 main");
								possible.Add(rightField);
							}
							if (InFutureStartAbs(leftField))
							{
								T("possible left 2 main");
								possible.Add(leftField);
							}
						}

						//if the only possible field is a future field, we don't need to check more. This will prevent unnecessar[1] exits, as in 0804.

						if (isMain && possible.Count == 1 && InFutureStartAbs(possible[0])) break;

						//check for an empty cell next to the position, and a taken one further. In case of a C shape, we need to fill the middle. The shape was formed by the previous 5 steps.
						T("possible.Count " + possible.Count);
											
						CShape = false;
						CheckNearFieldCommon();
						T("forbidden.Count after CheckNearFieldCommon " + forbidden.Count);
						if (!isMain && !searchReverse) // when the far end of the future line extends, it should be checked for border just like the main line. Near end shouldn't be checked, see 0714
						{
							CheckNearBorder();
							T("forbidden.Count after CheckNearBorder " + forbidden.Count);
							if (size >= 21) // 0430_2. Find out the minimum size
							{
                                Check1x3();
                            }
                        }
						else if (isMain)
						{							
                            CheckNearBorder();                            
                            CheckNearCorner(); // 0811_2
                            if (size >= 7)
							{								
                                CheckNearField(); //First case 0901
                                CheckCOnFarBorder();
                                CheckFutureL(); // Live end, future line near end and far end make an L, with one space between.
								CheckMiddleCorner2x2(); // 0909. A 2x2 area would created with one way to go in and out

                            }
                            if (size >= 9)
                            {
                                CheckCOnNearBorder();
                            }
                            if (size >= 11)
                            {
                                Check1x3();
                                Check3x3();
                            }
                            if (size >= 21)
                            {
                                // found on 21x21, may recreate the situation on smaller boards.
                                CheckNearFutureStartEnd();
                                CheckNearFutureSide();
                                // (Will be actual only on 21x21) 0630, but needs to work with 0804_1 too
                                // CheckNearFutureEnd();
                            }    
                        }
                        break;
					}
				}
			}

			List<int[]> newPossible = new List<int[]>();

			foreach (int[] field in possible)
			{
				if (!InForbidden(field))
				{
					newPossible.Add(field);
				}
			}

			possible = newPossible;
		}

		public void CheckNearBorder()
		{
			int directionIndex = Math.Abs(selectedDirection[0]); //0 for vertical, 1 for horizontal

			// going up on left side
			if (x == 2 && x + l[0] == 1 && !InTakenAbs(leftField) && !InTaken(x - 1, y - 1))
			{
				forbidden.Add(leftField);				
			}
			// going left on up side
			else if (y == 2 && y + r[1] == 1 && !InTakenAbs(rightField) && !InTaken(x - 1, y - 1))
			{
				forbidden.Add(rightField);
			}

			if (leftField[directionIndex] == size && !InTakenAbs(leftField))
			{
				if (directionIndex == 0) //going down on right side
				{
					forbidden.Add(straightField);
					forbidden.Add(rightField);
				}
				else //going left on down side
				{
					if (!InTaken(x - 1, y + 1) && !InBorder(x - 1, y + 1)) //InBorder checking is necessar[1] when x=1
					{
						forbidden.Add(leftField);
					}
					else if (!InTaken(x, y + 1)) //bottom may be already filled, and we had an u-turn longer right
					{
						forbidden.Add(straightField);
						forbidden.Add(rightField);
					}
				}
			}
			else if (rightField[directionIndex] == size && !InTakenAbs(rightField))
			{
				if (directionIndex == 0) //going up on right side
				{
					if (!InTaken(x + 1, y - 1) && !InBorder(x + 1, y - 1))
					{
						forbidden.Add(rightField);
					}
					else if (!InTaken(x + 1, y)) //side may be already filled, and we had an u-turn longer down
					{
						forbidden.Add(straightField);
						forbidden.Add(leftField);
					}
				}
				else //going right on down side
				{
					forbidden.Add(straightField);
					forbidden.Add(leftField);
				}
			}

			//going towards an edge, only applies to main line, future line far end may face incompleted fields, see 0829_2
			if (isMain)
			{
				if (x + 2 * s[0] == 0)
				{
					forbidden.Add(leftField);
					if (!InTaken(x - 1, y - 1))
					{
						forbidden.Add(straightField);

						if (size >= 9) // count area is always pair on 7x7. Situations where is would get impair are avoided with CheckFutureL, see 0902
                        {
							AddExit(x - 1, y - 1);
							circleDirectionLeft = false;
						}	
					}
				}
				else if (x + 2 * s[0] == size + 1)
				{
					forbidden.Add(rightField);
					if (!InTaken(x + 1, y - 1) && y != 1)
					{
						forbidden.Add(straightField);

						if (size >= 9)
						{
							AddExit(x + 1, y - 1);
							circleDirectionLeft = true;
						}						
					}
				}
				else if (y + 2 * s[1] == 0)
				{
					forbidden.Add(rightField);
					if (!InTaken(x - 1, y - 1))
					{
						forbidden.Add(straightField);

						if (size >= 9)
						{
							AddExit(x - 1, y - 1);
							circleDirectionLeft = true;
						}						
					}
				}
				else if (y + 2 * s[1] == size + 1)
				{
					forbidden.Add(leftField);
					if (!InTaken(x - 1, y + 1) && x != 1)
					{
						forbidden.Add(straightField);

						if (size >= 9)
						{
							AddExit(x - 1, y + 1);
							circleDirectionLeft = false;
						}						
					}
				}
			}
		}

		public void CheckNearFieldCommon()
		{
			if ((InTakenAll(x + 2 * l[0], y + 2 * l[1]) || InBorder(x + 2 * l[0], y + 2 * l[1])) && InTakenAll(x + l[0] - s[0], y + l[1] - s[1]) && !InTakenAllF(leftField)) //2 to left
			{
				//C shape that needs to be filled, unless there is a live end nearby that can fill it				
				if (isMain && !InFutureStartRel(1, -1) && !InFutureStartRel(2, 0) || !isMain && !InFutureOwnStartRel(1, -1) && !InFutureOwnEndRel(1, -1) && !InFutureOwnStartRel(2, 0) && !InFutureOwnEndRel(2, 0))
				{	
					// The future line does not create a C shape if the end of the main line is the empty field to the left. 
					// 0620_2, one step forward, the near end extends. 
					if (!(path2.Count != 0 && leftField[0] == path2[path2.Count - 1][0] && leftField[1] == path2[path2.Count - 1][1]))
					{
						T("C shape left");
						forbidden.Add(straightField);
						forbidden.Add(rightField);
						CShape = true; // left/right across checking will be disabled, no exits needed
					}
				}
				
				/*else if (!InTaken(x + l[0] + s[0], y + l[1] + s[1])) //if not upside down C
				{
					//lower left corner
					if (!(x == 1 && (y + l[1]) == size))
					{
						forbidden.Add(leftField);
					}
				}*/
			}

			if ((InTakenAll(x + 2 * r[0], y + 2 * r[1]) || InBorder(x + 2 * r[0], y + 2 * r[1])) && InTakenAll(x + r[0] - s[0], y + r[1] - s[1]) && !InTakenAllF(rightField)) //2 to right
			{
				if (isMain && !InFutureStartRel(-1, -1) && !InFutureStartRel(-2, 0) || !isMain && !InFutureOwnStartRel(-1, -1) && !InFutureOwnEndRel(-1, -1) && !InFutureOwnStartRel(-2, 0) && !InFutureOwnEndRel(-2, 0))
				{
					if (!(path2.Count != 0 && rightField[0] == path2[path2.Count - 1][0] && rightField[1] == path2[path2.Count - 1][1]))
					{
						T("C shape right");
						forbidden.Add(straightField);
						forbidden.Add(leftField);
						CShape = true;
					}
				}
				/*else if (!InTaken(x + r[0] + s[0], y + r[1] + s[1]))
				{
					//upper right corner
					if (!(y == 1 && (x + r[0]) == size))
					{
						T("forbidden.Count " + y);
						forbidden.Add(rightField);
					}
				}*/
			}

			// C shape straight
			// Even future line can make this C shape, see 0727_1
			if (!inPossible(leftField) && (InTakenAll(x + l[0] + s[0], y + l[1] + s[1]) || InBorder(x + l[0] + s[0], y + l[1] + s[1])) && (InTakenAll(x + 2 * s[0], y + 2 * s[1]) || InBorder(x + 2 * s[0], y + 2 * s[1])) && !InTakenAllF(straightField) && !(straightField[0] == size && straightField[1] == size)) 
			{
				if (isMain && !InFutureStartRel(1, 1) && !InFutureStartRel(0, 2) || !isMain && !InFutureOwnStartRel(1, 1) && !InFutureOwnEndRel(1, 1) && !InFutureOwnStartRel(0, 2) && !InFutureOwnEndRel(0, 2))
				{
					T("C shape straight left");
					forbidden.Add(rightField);
					CShape = true;
				}
			}
			else if (!inPossible(rightField) && (InTakenAll(x + r[0] + s[0], y + r[1] + s[1]) || InBorder(x + r[0] + s[0], y + r[1] + s[1])) && (InTakenAll(x + 2 * s[0], y + 2 * s[1]) || InBorder(x + 2 * s[0], y + 2 * s[1])) && !InTakenAllF(straightField) && !(straightField[0] == size && straightField[1] == size))
			{
				if (isMain && !InFutureStartRel(-1, 1) && !InFutureStartRel(0, 2) || !isMain && !InFutureOwnStartRel(-1, 1) && !InFutureOwnEndRel(-1, 1) && !InFutureOwnStartRel(0, 2) && !InFutureOwnEndRel(0, 2))
				{
					T("C shape straight right");
					forbidden.Add(leftField);
					CShape = true;
				}
			}
		}

		public void CheckNearField()
		{
			// o
			//xo
			//xox

			if (!CShape)
			{
				if (InTakenRel(2, -1) && !InTakenAbs(leftField) && !InTakenRel(1, -1) && !InTakenRel(1, 1))
				{
					forbidden.Add(leftField);
				}
				if (InTakenRel(-2, -1) && !InTakenAbs(rightField) && !InTakenRel(-1, -1)! && !InTakenRel(-1, 1))
				{
					forbidden.Add(rightField);
				}

				if (InTakenRel(2, 1) && !InTakenAbs(leftField) && !InTakenRel(1, 1) && !(path2 != null && path2.Count > 0 && leftField[0] == path2[path2.Count - 1][0] && leftField[1] == path2[path2.Count - 1][1]))
				{
					if (InTakenRel(1, -1) && InTakenRel(2, 0))
					{
						forbidden.Add(straightField);
						forbidden.Add(rightField);
					}
					else
					{
						forbidden.Add(leftField);
						T("Adding left to forbidden");
					}
				}
				if (InTakenRel(-2, 1) && !InTakenAbs(rightField) && !InTakenRel(-1, 1) && !(path2 != null && path2.Count > 0 && rightField[0] == path2[path2.Count - 1][0] && rightField[1] == path2[path2.Count - 1][1]))
				{
					if (InTakenRel(-1, -1) && InTakenRel(-2, 0))
					{
						forbidden.Add(straightField);
						forbidden.Add(leftField);
					}
					else
					{
						forbidden.Add(rightField);
					}
				}
			}			

			foreach (int[] field in forbidden)
			{
				T("CheckNearField forbidden " + field[0] + " " + field[1]);
			}

			if (!CShape && size >= 7) // See 0901
			{
				int directionIndex = Math.Abs(selectedDirection[0]); //0 for vertical, 1 for horizontal

				if (InTakenRel(1, 2) &&
					!InTakenAbs(straightField) &&
					!InTakenRel(1, 1) && !(x + l[0] + 2 * s[0] == 1 && y + l[1] + 2 * s[1] == 1))
				{ //inTakenIndex would be negative when getting to the upper left corner, rigtt mid across check will be true instead.
					int index = InTakenIndex(x + l[0] + 2 * s[0], y + l[1] + 2 * s[1]);
					T("Left mid across field: x " + (x + l[0] + 2 * s[0]) + " y " + (y + l[1] + 2 * s[1]) + " x " + x + " y " + y);

					int[] nextField;
					int[] prevField;
					if (isMain)
					{
						if (foundPath == path2)
						{
							nextField = foundPath[index - 1];
							prevField = foundPath[index + 1];
						}
						else
						{
							nextField = foundPath[index + 1];
							prevField = foundPath[index - 1];
						}
					}
					else
					{
						nextField = foundPath[index + 1];
						prevField = foundPath[index - 1];
					}

					forbidden.Add(straightField);

					//In the 7x7 example, the area is pair, and there does not seem to be another scenario
					if (size >= 9) AddExit(x + l[0] + s[0], y + l[1] + s[1]);

					int firstConditionValue, secondConditionValue;
					if (directionIndex == 0)
					{
						firstConditionValue = x + 2 * l[0];
						secondConditionValue = x;
					}
					else
					{
						firstConditionValue = y + 2 * l[1];
						secondConditionValue = y;
					}

					if (nextField[directionIndex] == firstConditionValue) //left
					{
						forbidden.Add(rightField);
						circleDirectionLeft = true;
					}
					else if (nextField[directionIndex] == secondConditionValue) //right
					{
						forbidden.Add(leftField);
						circleDirectionLeft = false;
					}
					else //up
					{
						T("Left mid across, next field up");

						if (prevField[directionIndex] == firstConditionValue) //from left
						{
							T("Left mid across, next field up from left");
							forbidden.Add(leftField);
							circleDirectionLeft = false;
						}
						else //from right
						{
							T("Left mid across, next field up from right");
							forbidden.Add(rightField);
							circleDirectionLeft = true;
						}
					}
				}
				else if (InTakenRel(-1, 2) &&
					!InTakenAbs(straightField) &&
					!InTakenRel(-1, 1) && !(x + r[0] + 2 * s[0] == 1 && y + r[1] + 2 * s[1] == 1))
				{
					int index = InTakenIndex(x + r[0] + 2 * s[0], y + r[1] + 2 * s[1]);
					T("Right mid across field: x " + (x + r[0] + 2 * s[0]) + " y " + (y + r[1] + 2 * s[1]) + " x " + x + " y " + y);

					int[] nextField;
					int[] prevField;
					if (isMain)
					{
						if (foundPath == path2)
						{
							nextField = foundPath[index - 1];
							prevField = foundPath[index + 1];
						}
						else
						{
							nextField = foundPath[index + 1];
							prevField = foundPath[index - 1];
						}
					}
					else
					{
						nextField = foundPath[index + 1];
						prevField = foundPath[index - 1];
					}

					forbidden.Add(straightField);

                    if (size >= 9) AddExit(x + r[0] + s[0], y + r[1] + s[1]);

					int firstConditionValue, secondConditionValue;
					if (directionIndex == 0)
					{
						firstConditionValue = x + 2 * r[0];
						secondConditionValue = x;
					}
					else
					{
						firstConditionValue = y + 2 * r[1];
						secondConditionValue = y;
					}

					if (nextField[directionIndex] == firstConditionValue) //right
					{
						forbidden.Add(leftField);
						circleDirectionLeft = false;
					}
					else if (nextField[directionIndex] == secondConditionValue) //left
					{
						forbidden.Add(rightField);
						circleDirectionLeft = true;
					}
					else //up
					{
						if (prevField[directionIndex] == firstConditionValue) //from right
						{
							forbidden.Add(rightField);
							circleDirectionLeft = true;
						}
						else //from left
						{
							//check C shape
							forbidden.Add(leftField);
							circleDirectionLeft = false;
						}
					}
				}
				else
				{
					int firstConditionValue, secondConditionValue;
					if (directionIndex == 0)
					{
						firstConditionValue = y + 3 * s[1];
						secondConditionValue = y + s[1];
					}
					else
					{
						firstConditionValue = x + 3 * s[0];
						secondConditionValue = x + s[0];
					}

					if (InTakenRel(2, 2) &&
						!InTakenAbs(straightField) &&
						!InTakenRel(1, 1) &&
						!InTakenAbs(leftField))
					{
						int index = InTakenIndex(x + 2 * l[0] + 2 * s[0], y + 2 * l[1] + 2 * s[1]);

						T("Left across field: x " + (x + 2 * l[0] + 2 * s[0]) + " y " + (y + 2 * l[1] + 2 * s[1]) + " x " + x + " y " + y);

						int[]? nextField = null;
						int[] prevField;
						if (isMain)
						{
							if (foundPath == path2)
							{
								if (index != 0) //not the far end of the future line
								{
									nextField = foundPath[index - 1];
								}								
								prevField = foundPath[index + 1];
							}
							else
							{
								nextField = foundPath[index + 1];
								prevField = foundPath[index - 1];
							}
						}
						else
						{
							nextField = foundPath[index + 1];
							prevField = foundPath[index - 1];
						}

                        if (size >= 9) AddExit(x + l[0] + s[0], y + l[1] + s[1]);

						if (nextField != null && nextField[1 - directionIndex] == firstConditionValue) //up
						{
							if (isMain)
							{
								forbidden.Add(leftField);
								circleDirectionLeft = false;
							}
						}
						else //right
						{
							if (index != 0) //not the start field
							{
								if (prevField[1 - directionIndex] == secondConditionValue) // from down
								{
									forbidden.Add(leftField);
									circleDirectionLeft = false;
								}
								else
								{
									forbidden.Add(straightField);
									forbidden.Add(rightField);
									circleDirectionLeft = true;
								}
							}
							else
							{
								forbidden.Add(straightField);
								forbidden.Add(rightField);
								circleDirectionLeft = true;
							}
						}
					}

					if (InTakenRel(-2, 2) &&
					!InTakenAbs(straightField) &&
					!InTakenRel(-1, 1) &&
					!InTakenAbs(rightField))
					{
						int index = InTakenIndex(x + 2 * r[0] + 2 * s[0], y + 2 * r[1] + 2 * s[1]);

						T("Right across field: x " + (x + 2 * r[0] + 2 * s[0]) + " y " + (y + 2 * r[1] + 2 * s[1]) + " x " + x + " y " + y);

						int[]? nextField = null;
						int[] prevField;
						if (isMain)
						{
							if (foundPath == path2)
							{
								if (index != 0) //not the far end of the future line
								{
									nextField = foundPath[index - 1];
								}
								prevField = foundPath[index + 1];
							}
							else
							{
								nextField = foundPath[index + 1];
								prevField = foundPath[index - 1];
							}
						}
						else
						{
							nextField = foundPath[index + 1];
							prevField = foundPath[index - 1];
						}

                        if (size >= 9) AddExit(x + r[0] + s[0], y + r[1] + s[1]);

						if (nextField != null && nextField[1 - directionIndex] == firstConditionValue) //up
						{
							if (isMain) //Restriction only valid for real line, because it has to come out of the enclosed area. Future line is the line coming out. See 0415_1.
							{
								forbidden.Add(rightField);
								circleDirectionLeft = true;
							}
						}
						else //left
						{
							if (index != 0) //not the start field
							{
								if (prevField[1 - directionIndex] == secondConditionValue) // from down
								{
									forbidden.Add(rightField);
									circleDirectionLeft = true;
								}
								else
								{
									forbidden.Add(straightField); //cannot go up or left
									forbidden.Add(leftField);
									circleDirectionLeft = false;
								}
							}
							else
							{
								forbidden.Add(straightField); //cannot go up or left
								forbidden.Add(leftField);
								circleDirectionLeft = false;
							}
						}
					}
				}
			}
		}

		private void Check1x3()
		{
            //Certain edges are impossible to fill.

            //xo
            // xoo?x
            //  xxxx

            //next step has to be left

			//if I give value to int[] thisS = s, it will change when s changes.
            int thisS0 = s[0];
            int thisS1 = s[1];
            int thisL0 = l[0];
            int thisL1 = l[1];

            for (int i = 0; i < 2; i++)
			{
				for (int j = 0; j < 2; j++)
				{
                    if (InTakenRel(1, -1) && InTakenRel(2, -1) && InTakenRel(3, -1)
					&& InTakenRel(4, 0) && InTakenRel(5, 1)
					&& !InTakenRel(1, 0) && !InTakenRel(2, 0) && !InTakenRel(3, 0)
					&& !InTakenRel(4, 1))
					{
						T("1x3 valid at x " + x + " y " + y);
						forbidden.Add(new int[] { x + s[0], y + s[1] }); //straight field
						forbidden.Add(new int[] { x - l[0], y - l[1] }); //right field (per the start direction)
					}

                    //turn right, pattern goes upwards
                    int temps0 = s[0];
					int temps1 = s[1];
					s[0] = -l[0];
					s[1] = -l[1];                    
                    l[0] = temps0;
					l[1] = temps1;
                }

				//mirror directions
				s[0] = thisS0;
                s[1] = thisS1;
                l[0] = -thisL0;
				l[1] = -thisL1;
            }

            s[0] = thisS0;
            s[1] = thisS1;
            l[0] = thisL0;
            l[1] = thisL1;
        }

		private void Check3x3()
		{
            // x
            //xo
            //xo
            //xoo?x
            // xxxx

            //next step has to be left

            int thisS0 = s[0];
            int thisS1 = s[1];
            int thisL0 = l[0];
            int thisL1 = l[1];

            for (int i = 0; i < 2; i++)
			{
				for (int j = 0; j < 2; j++)
				{
					if (InTakenRel(1, -1) && InTakenRel(2, -1) && InTakenRel(3, -1)
					 && InTakenRel(4, 0) && InTakenRel(4, 1) && InTakenRel(4, 2)
					 && InTakenRel(3,3)
					 && !InTakenRel(1, 0) && !InTakenRel(2, 0) && !InTakenRel(3, 0)
					 && !InTakenRel(3, 1) && !InTakenRel(3, 2))
					{
						T("3x3 valid at x " + x + " y " + y);
						forbidden.Add(new int[] { x + s[0], y + s[1] }); //straight field
						forbidden.Add(new int[] { x - l[0], y - l[1] }); //right field (per the start direction)
					}

                    //turn right, pattern goes upwards
                    int temps0 = s[0];
                    int temps1 = s[1];
                    s[0] = -l[0];
                    s[1] = -l[1];
                    l[0] = temps0;
                    l[1] = temps1;
                }

                //mirror directions
                s[0] = thisS0;
                s[1] = thisS1;
                l[0] = -thisL0;
                l[1] = -thisL1;
            }

            s[0] = thisS0;
            s[1] = thisS1;
            l[0] = thisL0;
            l[1] = thisL1;
        }

		public void CheckFutureL()
		{
            // conditions are true already on 5x5 at 0831_2, but it is handled in CheckNearCorner
            if (InFutureStart(x + 2 * l[0], y + 2 * l[1]) && InFutureEnd(x + 2 * l[0] + 2 * s[0], y + 2 * l[1] + 2 * s[1]))
			{
                T("CheckFutureL left");
                forbidden.Add(rightField);
                forbidden.Add(straightField);
            }
            if (InFutureStart(x + 2 * r[0], y + 2 * r[1]) && InFutureEnd(x + 2 * r[0] + 2 * s[0], y + 2 * r[1] + 2 * s[1]))
            {
                T("CheckFutureL right");
                forbidden.Add(leftField);
                forbidden.Add(straightField);
            }
            if (InFutureStart(x + 2 * s[0], y + 2 * s[1]) && InFutureEnd(x + 2 * l[0] + 2 * s[0], y + 2 * l[1] + 2 * s[1]))
            {
                T("CheckFutureL straight left");
                forbidden.Add(leftField);
            }
            if (InFutureStart(x + 2 * s[0], y + 2 * s[1]) && InFutureEnd(x + 2 * r[0] + 2 * s[0], y + 2 * r[1] + 2 * s[1]))
            {
                T("CheckFutureL straight right");
                forbidden.Add(rightField);
            }
        }

		public void CheckMiddleCorner2x2()
		{
			if (x == 3 && y == size - 2 && leftField[0] == 2)
			{
                T("CheckMiddleCorner2x2 to left");
                forbidden.Add(leftField);
            }
			else if (y == 3 && x == size - 2 && rightField[1] == 2)
			{
                T("CheckMiddleCorner2x2 to right");
                forbidden.Add(rightField);
            }
		}
        //Example: 0305
        public void CheckNearFutureSide() //suppose the side is of stair form
		{
			//mid cases are used when approaching an end, example: 0415_2
			//but in case of 0620, this is an error
			if (InFuture(x + 2 * s[0], y + 2 * s[1]) && InFuture(x + r[0] + 2 * s[0], y + r[1] + 2 * s[1]) && InFutureStart(x + r[0] + s[0], y + r[1] + s[1]) && !InTakenAbs(leftField) && !InTakenAbs(straightField) && !InTakenAbs(rightField) && !InFutureF(leftField) && !InFutureF(straightField) && !InFutureF(rightField))
			{
				//touching the end at the straight right corner. Can the future line go in other direction?
				T("CheckNearFutureSide mid right");
				forbidden.Add(rightField);
				forbidden.Add(straightField);
			}
			else if (InFuture(x + 2 * s[0], y + 2 * s[1]) && InFuture(x + l[0] + 2 * s[0], y + l[1] + 2 * s[1]) && InFutureStart(x + l[0] + s[0], y + l[1] + s[1]) && !InTakenAbs(leftField) && !InTakenAbs(straightField) && !InTakenAbs(rightField) && !InFutureF(leftField) && !InFutureF(straightField) && !InFutureF(rightField))
			{
				T("CheckNearFutureSide mid left");
				forbidden.Add(leftField);
				forbidden.Add(straightField);
			}
			else //check possible across fields. 2 across is in future, 1 across is free, plug 1 forward (0316 can otherwise go wrong). 1 to the side is also free, but we don't need to check it. Both sides can be true simultaneousl[1], example: 0413
			//Condition has to hold right even in situations like 0425_1
			{
				/*if (InFutureStart(x + 2 * r[0] + 2 * s[0], y + 2 * r[1] + 2 * s[1]) && !InFuture(x + r[0] + s[0], y + r[1] + s[1]) && !InFuture(x + s[0], y + s[1]) && !InFutureF(rightField)
					&& !InTaken(x + r[0] + s[0], y + r[1] + s[1]) && !InTaken(x + s[0], y + s[1]) )
				{
					//2 to right and 1 forward, plus 1 to right and 2 forward is also free
					if (!InFuture(x + 2 * r[0] + s[0], y + 2 * r[1] + s[1]) && !InFuture(x + r[0] + 2 * s[0], y + r[1] + 2 * s[1]))
					{
						T("CheckNearFutureSide across right");

						if (!InFutureStartIndex(i) && !InFutureEndIndex(i))
						{
							T("Not start of section");
							int[] nextField = path2[i - 1];
							//goes to right further
							if (nextField[0] == x + 3 * r[0] + 2 * s[0] &&
								nextField[1] == y + 3 * r[1] + 2 * s[1])
							{
								T("Goes right");
								forbidden.Add(leftField);
								forbidden.Add(straightField);
							}
							//goes forward further
							else
							{
								T("Goes straight");
								forbidden.Add(rightField);
							}
						}
						// In case the across field is the start of a section, we can still go in all directions, it is possible to connect to it.
					}
					// we entered the stair from the start, as in 0305. The across field is going forward.
					// But in 0729, we cannot disable the straight field, because the left field is not free.
					else if (!(InTakenAbs(leftField) || InFutureF(leftField) || InBorderF(leftField)))
					{
						T("CheckNearFutureSide across right, entered at start");
						forbidden.Add(rightField);
						forbidden.Add(straightField);
					}
				}

				//left across
				if (InFutureStart(x + 2 * l[0] + 2 * s[0], y + 2 * l[1] + 2 * s[1]) && !InFuture(x + l[0] + s[0], y + l[1] + s[1]) && !InFuture(x + s[0], y + s[1]) && !InTaken(x + l[0] + s[0], y + l[1] + s[1]) && !InTaken(x + s[0], y + s[1]) && !InFutureF(leftField))
				{
					if (!InFuture(x + 2 * l[0] + s[0], y + 2 * l[1] + s[1]) && !InFuture(x + l[0] + 2 * s[0], y + l[1] + 2 * s[1]))
					{
						T("CheckNearFutureSide across left");

						if (!InFutureStartIndex(i) && !InFutureEndIndex(i))
						{
							T("Not start of section");
							int[] nextField = path2[i - 1];
							//goes to right further
							if (nextField[0] == x + 3 * l[0] + 2 * s[0] &&
								nextField[1] == y + 3 * l[1] + 2 * s[1])
							{
								T("Goes left");
								forbidden.Add(rightField);
								forbidden.Add(straightField);
							}
							//goes forward further
							else
							{
								T("Goes straight");
								forbidden.Add(leftField);
							}
						}
					}
					else if (!(InTakenAbs(rightField) || InFutureF(rightField) || InBorderF(rightField)))
					{
						T("CheckNearFutureSide across left, entered at start");
						forbidden.Add(leftField);
						forbidden.Add(straightField);
					}
				}*/
			}			
		}

		public void CheckNearFutureStartEnd()
		{
			T(" x " + x + " " + y + " " + l[0] + " " + l[1] + " " + s[0] + " " + s[1]);
			T("CheckNearFutureStartEnd x + l[0] + 2 * s[0] " + (x + l[0] + 2 * s[0]) + " y + l[1] + 2 * s[1] " + (y + l[1] + 2 * s[1]));
			//meeting start and end ahead. Ends are 2 apart from each other at same forward distance.
			if (InFutureStart(x + 2 * s[0], y + 2 * s[1]) && InFutureEnd(x + 2 * r[0] + 2 * s[0], y + 2 * r[1] + 2 * s[1]) && !InTakenAbs(leftField) && !InTakenAbs(straightField) && !InTakenAbs(rightField))
			{
				T("CheckNearFutureStartEnd to right");
				forbidden.Add(rightField);
			}
			else if (InFutureStart(x + 2 * s[0], y + 2 * s[1]) && InFutureEnd(x + 2 * l[0] + 2 * s[0], y + 2 * l[1] + 2 * s[1]) && !InTakenAbs(leftField) && !InTakenAbs(straightField) && !InTakenAbs(rightField))
			{
				T("CheckNearFutureStartEnd to left");
				forbidden.Add(leftField);
			}
			else if (InFutureStart(x + l[0] + 2 * s[0], y + l[1] + 2 * s[1]) && InFutureEnd(x + r[0] + 2 * s[0], y + r[1] + 2 * s[1]) && !InTakenAbs(leftField) && !InTakenAbs(straightField) && !InTakenAbs(rightField))
			{
				T("CheckNearFutureStartEnd to middle, start on left");
				forbidden.Add(rightField);
				forbidden.Add(straightField);
			}
			else if (InFutureStart(x + r[0] + 2 * s[0], y + r[1] + 2 * s[1]) && InFutureEnd(x + l[0] + 2 * s[0], y + l[1] + 2 * s[1]) && !InTakenAbs(leftField) && !InTakenAbs(straightField) && !InTakenAbs(rightField))
			{
				T("CheckNearFutureStartEnd to middle, start on right");
				forbidden.Add(leftField);
				forbidden.Add(straightField);
			}
		}

		private void CheckNearFutureEnd()
		{
			if (InFutureEnd(x + l[0] + s[0], y + l[1] + s[1]))
			{
				T("CheckNearFutureEnd to left");
				forbidden.Add(rightField);
				//0804_1 makes this untrue: forbidden.Add(straightField);
			}
			else if (InFutureEnd(x + r[0] + s[0], y + r[1] + s[1]))
			{
				T("CheckNearFutureEnd to right");
				forbidden.Add(leftField);
			}
		}

		private void CheckNearCorner()
		{
			//First condition is needed for when we are at 4,5 and the left field is 4,4 on a 5x5 field
			if (y != size && leftField[0] == size - 1 && leftField[1] == size - 1)
			{
				T("Left field near corner");
				forbidden.Add(leftField);
			}
			else if (x != size && rightField[0] == size - 1 && rightField[1] == size - 1)
			{
				T("Right field near corner");
				forbidden.Add(rightField);
			}
		}

        private void CheckCOnFarBorder()
        {
            // Applies from 7x7, see 0831_1. Similar to CheckNearCorner, it is just not at the corner.
            if (x == size - 2 && rightField[0] == size - 1 && !InTakenRel(-1, -1) && InTakenRel(-1, -2))
            {
                T("Right field C on far border");
                forbidden.Add(rightField);
            }
            else if (y == size - 2 && leftField[1] == size - 1 && !InTakenRel(1, -1) && InTakenRel(1, -2))
            {
                T("Left field C on far border");
                forbidden.Add(leftField);
            }
        }

        private void CheckCOnNearBorder()
        {
            // Applies from 9x9, see 0901_1.
            if (x == 3 && leftField[0] == 2 && !InTakenRel(1, -1) && InTakenRel(1, -2))
            {
                T("Left field C on near border");
                forbidden.Add(leftField);
            }
            else if (y == 3 && rightField[1] == 2 && !InTakenRel(-1, -1) && InTakenRel(-1, -2))
            {
                T("Right field C on near border");
                forbidden.Add(rightField);
            }
        }

        private void AddExit(int x, int y)
		{
			if (!isMain) return; //future path does not need exit

			T("AddExit x " + x + " y " + y);
			if (window.exitIndex.Count > 0)
			{
				int lastIndex = window.exits.Count - 1;
				int[] lastExitField = window.exits[lastIndex]; //the new exit may have the same coordinates as the last one if the path came back from a U-turn. In this case the new exit index is at least 2 steps after the old one.
				if (x == lastExitField[0] && y == lastExitField[1])
				{
					window.exits.RemoveAt(lastIndex);
					window.exitIndex.RemoveAt(lastIndex);
				}

				int lastExitIndex = -1;
				if (window.exits.Count > 0)
				{
					lastIndex = window.exits.Count - 1;
					lastExitIndex = window.exitIndex[lastIndex];
				}

				if (count - 1 >= lastExitIndex + 2) //exits right after each other are unnecessar[1], there should be at least 2 steps between them 
				{
					window.exits.Add(new int[] { x, y });
					window.exitIndex.Add(count - 1);
					circleDirectionLeft = false;
					nearField = true;
				}
			}
			else
			{
				window.exits.Add(new int[] { x, y });
				window.exitIndex.Add(count - 1);
				circleDirectionLeft = false;
				nearField = true;
			}
		}

		public bool inPossible(int[] field)
		{
			foreach (int[] f in possible)
			{
				if (f[0] == field[0] && f[1] == field[1]) return true;
			}
			return false;
		}

        public bool InBorderF(int[] field)
        {
            int x = field[0];
            int y = field[1];
            return InBorder(x, y);
        }

        public bool InBorderRel(int left, int straight)
        {
            int x = this.x + left * l[0] + straight * s[0];
            int y = this.y + left * l[1] + straight * s[1];
            return InBorder(x, y);
        }

        public bool InBorderRel2(int left, int straight)
        {
            int x = this.x2 + left * l[0] + straight * s[0];
            int y = this.y2 + left * l[1] + straight * s[1];
            return InBorder(x, y);
        }

        public bool InBorder(int x, int y) // allowing negative values could cause an error in AddFutureLines 2x2 checking
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
            int x = this.x + left * l[0] + straight * s[0];
            int y = this.y + left * l[1] + straight * s[1];
            return InTaken(x, y);
        }

        public bool InTakenRel2(int left, int straight)
        {
            int x = x2 + left * l[0] + straight * s[0];
            int y = y2 + left * l[1] + straight * s[1];
            return InTaken(x, y);
        }

        public bool InTaken(int x, int y) //more recent fields are more probable to encounter, so this way processing time is optimized
		{
			if (!isMain)
			{
				//In AddFutureLines, path2 of future gets null
				if (path2 != null)
				{
					int c2 = path2.Count;

					//when we extend the near end, it can connect to the main line. The far end cannot.
					int searchStart = searchReverse ? c2 - 2: c2 - 1;
					for (int i = searchStart; i >= 0; i--)
					{
						int[] field = path2[i];
						if (x == field[0] && y == field[1])
						{
							return true;
						}
					}
				}				

				int c1 = path.Count;

				if (!searchReverse)
				{
					for (int i = searchStartIndex + 2; i < c1; i++) // In 0618_2 when the far end of the upper line extends, it encounters the side of the previous future section, which is not checked in this funtion.					
					{
						int[] field = path[i];
						if (x == field[0] && y == field[1])
						{
							return true;
						}
					}
				}
				else
				{
					for (int i = searchStartIndex - 2; i >= 0; i--) //the first area that is checked (in checknearfield) is one back and one to left/right side, 2 steps from the current
					{
						int[] field = path[i];
						if (x == field[0] && y == field[1])
						{
							return true;
						}
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

        public bool InTakenAllF(int[] field0)
        {
            int x = field0[0];
            int y = field0[1];

            return InTakenAll(x, y);
        }

        public bool InTakenAllRel(int left, int straight)
        {
            int x = this.x + left * l[0] + straight * s[0];
            int y = this.y + left * l[1] + straight * s[1];
            return InTakenAll(x, y);
        }

        public bool InTakenAll(int x, int y)
		{
			if (!isMain)
			{
				//In AddFutureLines, path2 of future gets null
				if (path2 != null)
				{
					int c2 = path2.Count;

					//when we extend the near end, it can connect to the main line. The far end cannot.
					int searchStart = searchReverse ? c2 - 2 : c2 - 1;
					for (int i = searchStart; i >= 0; i--)
					{
						int[] field = path2[i];
						if (x == field[0] && y == field[1])
						{
							return true;
						}
					}
				}

				int c1 = path.Count;

				for (int i = 0; i < c1; i++)				
				{
					int[] field = path[i];
					if (x == field[0] && y == field[1])
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

				if (path2 != null)
				{
					c1 = path2.Count;

					for (int i = 0; i < c1; i++)
					{
						int[] field = path2[i];
						if (x == field[0] && y == field[1])
						{
							return true;
						}
					}
				}				
			}

			return false;
		}

		public bool InFutureOwnStartRel(int left, int straight) // relative position
        {
                int x = this.x + left * l[0] + straight * s[0];
                int y = this.y + left * l[1] + straight * s[1];
                return InFutureOwnStartF(new int[] { x, y });
		}

		public bool InFutureOwnEndRel(int left, int straight)
		{
            int x = this.x + left * l[0] + straight * s[0];
            int y = this.y + left * l[1] + straight * s[1];
            return InFutureOwnEndF(new int[] { x, y });
		}

		public bool InFutureOwnStartF(int[] f, int nearSection = -1)
		{
			int c = path.Count;
			if (c == 0) return false;

			int x = f[0];
			int y = f[1];

			int foundIndex = -1;

			int i;
			for (i = c - 1; i >= 0; i--)
			{
				int[] field = path[i];

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
							for (int j = 2; j < merge.Length; j++)
							{
								if (merge[j] == i) return false;
							}
						}

						return true;
					}
					// else i == nearSection: Found field is not a start that can be stepped on. 
				}
			}

			return false;
		}

		public bool InFutureOwnEndF(int[] f, int farSection = -1)
		{
			int c = path.Count;
			if (c == 0) return false;

			int x = f[0];
			int y = f[1];

			if (x == size && y == size) return false; // a far end that reached the corner is not considered live.

			int foundIndex = -1;

			int i;
			for (i = c - 1; i >= 0; i--)
			{
				int[] field = path[i];

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
							for (int j = 1; j < merge.Length - 1; j++)
							{
								if (merge[j] == i) return false;
							}
						}

						return true;
					}
				}
			}

			return false;
		}

		public bool InFutureStartAbs(int[] f) // absulute position
		{
			return InFutureStart(f[0], f[1]);
		}

        public bool InFutureStartRel(int left, int straight) // relative position
        {
                int x = this.x + left * l[0] + straight * s[0];
                int y = this.y + left * l[1] + straight * s[1];
                return InFutureStart(x, y);
        }


        public bool InFutureStart(int x, int y)
		{
            int c = path2.Count;
			if (c == 0) return false;

			int foundIndex = -1;

			int i;
			for (i = c - 1; i >= 0; i--)
			{
				int[] field = path2[i];

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
					// if the start is in the middle of a merged section, it doesn't count
					// See 0806
					foreach (int[] merge in MainWindow.futureSectionMerges)
					{
						// examine all but the first merged section
						for (int j = 2; j < merge.Length; j++)
						{
							if (merge[j] == i) return false;
						}
					}
					return true;
				}
			}

			return false;
		}

		public bool InFutureEnd(int x, int y)
		{
			int c = path2.Count;
			if (c == 0) return false;

			if (x == size && y == size) return false; // a far end that reached the corner is not considered live.

			int foundIndex = -1;

			int i;
			for (i = c - 1; i >= 0; i--)
			{
				int[] field = path2[i];
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
					// if the end is in the middle of a merged section, it doesn't count
					// See 0806
					foreach (int[] merge in MainWindow.futureSectionMerges)
					{
						// examine all but the last merged section
						for (int j = 1; j < merge.Length - 1; j++)
						{
							if (merge[j] == i) return false;
						}
					}
					return true;
				}
			}

			return false;
		}

		private bool InFutureStartIndex(int i)
		{
			foreach (int[] section in MainWindow.futureSections)
			{
				if (section[0] == i)
				{
					return true;
				}
			}

			return false;
		}

		private bool InFutureEndIndex(int i)
		{
			foreach (int[] section in MainWindow.futureSections)
			{
				if (section[1] == i)
				{
					return true;
				}
			}

			return false;
		}

		public bool InFuture(int x, int y)
		{
			int c2 = path2.Count;
			if (c2 == 0) return false;

			for (int i = c2 - 1; i >= 0; i--)
			{
				int[] field = path2[i];
				if (x == field[0] && y == field[1])
				{
					return true;
				}
			}
			return false;
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

		public bool InFutureF(int[] field0)
		{
			int x = field0[0];
			int y = field0[1];

			return InFuture(x, y);
		}

		public int InTakenIndex(int x, int y)
		{
			int foundIndex = -1;
			
			if (path2 != null && path2.Count > 0)
			{
				int c2 = path2.Count;
				//first field (near end) doesn't count as taken, it can be connected to. Also ever[1] open ends.foundpath
				int prevX = path2[c2 - 1][0];
				int prevY = path2[c2 - 1][1];
				for (int i = c2 - 2; i >= 0; i--)
				{
					int[] field = path2[i];
					int searchX = field[0];
					int searchY = field[1];
					if (x == searchX && y == searchY && (searchX == prevX && Math.Abs(searchY - prevY) == 1 || searchY == prevY && Math.Abs(searchX - prevX) == 1))
					{
						foundIndex = i;
						foundPath = path2;
					}
					prevX = searchX;
					prevY = searchY;
				}
			}

			int c1 = path.Count;
			for (int i = 0; i < c1; i++)
			{
				int[] field = path[i];
				if (x == field[0] && y == field[1])
				{
					foundIndex = i;
					foundPath = path;
				}
			}
			return foundIndex;
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

		private void T(object o)
		{
			Trace.WriteLine(o.ToString());
		}
	}
}