namespace OneWayLabyrinth
{
	using System.Collections.Generic;

	public partial class Path
	{
		int directionFieldIndex = 0;
		List<string> activeRules;
		List<List<int[]>> activeRulesForbiddenFields;
		List<int[]> activeRuleSizes;
		List<int[]> startForbiddenFields;
		public bool Future2x2StartEnd = false;
		public bool Future2x3StartEnd = false;
		public bool Future3x3StartEnd = false;
		public bool FutureL = false;
		public bool DoubleAreaCShape = false;
		public bool DoubleAreaStair2 = false;
		public bool DoubleAreaStairArea = false;
		public bool DoubleAreaStair = false;
		public bool DoubleCShapeStartC = false;
		public bool Square4x2Area = false;
		public bool Square4x2CShape = false;
		public bool StraightAcross3EndArea = false;
		public bool StraightAcrossEndArea = false;
		public bool StraightAcrossEndC = false;
		public bool StraightMidAcross3EndArea = false;
		public bool StraightMidAcross3EndC = false;
		public bool TripleAreaExitDown = false;
		public bool TripleAreaStair = false;
		public bool TripleArea = false;
		public bool StraightMidAcross3EndArea2 = false;

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
			DoubleAreaCShape = false;
			DoubleAreaStair2 = false;
			DoubleAreaStairArea = false;
			DoubleAreaStair = false;
			DoubleCShapeStartC = false;
			Square4x2Area = false;
			Square4x2CShape = false;
			StraightAcross3EndArea = false;
			StraightAcrossEndArea = false;
			StraightAcrossEndC = false;
			StraightMidAcross3EndArea = false;
			StraightMidAcross3EndC = false;
			TripleAreaExitDown = false;
			TripleAreaStair = false;
			TripleArea = false;
			StraightMidAcross3EndArea2 = false;

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
					if ((InTakenRel(4,1) || InBorderRel(4,1)) && InFutureStartRel(1,0) && InFutureEndRel(3,0) && InTakenRel(0,3) && InTakenRel(-1,2) && InTakenRel(-1,1) && !InTakenRel(0,2) && !InBorderRel(0,2) && foundSectionStart == foundSectionEnd)
					{
						Future2x2StartEnd = true;
						activeRules.Add("Future 2 x 2 Start End");
						activeRulesForbiddenFields.Add(new List<int[]> {new int[] { x + lx, y + ly }});
						activeRuleSizes.Add(new int[] {6,4});
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
					if ((InTakenRel(1,-2) || InBorderRel(1,-2)) && !InTakenRel(1,-1) && !InBorderRel(1,-1) && InFutureStartRel(0,1) && InFutureEndRel(2,1) && foundSectionStart == foundSectionEnd)
					{
						Future2x3StartEnd = true;
						activeRules.Add("Future 2 x 3 Start End");
						activeRulesForbiddenFields.Add(new List<int[]> {new int[] { x + lx, y + ly }});
						activeRuleSizes.Add(new int[] {3,4});
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
						if (!InTakenRel(3,3) && !InBorderRel(3,3) && !InTakenRel(3,1) && !InBorderRel(3,1) && (InTakenRel(3,4) || InBorderRel(3,4)) && (InTakenRel(2,4) || InBorderRel(2,4)) && (InTakenRel(1,4) || InBorderRel(1,4)) && (InTakenRel(4,3) || InBorderRel(4,3)) && (InTakenRel(4,2) || InBorderRel(4,2)) && (InTakenRel(4,1) || InBorderRel(4,1)) && InFutureStartRel(0,1) && InFutureEndRel(0,3) && !InCornerRel(3,3) && foundSectionStart == foundSectionEnd)
						{
							Future3x3StartEnd = true;
							activeRules.Add("Future 3 x 3 Start End");
							activeRulesForbiddenFields.Add(new List<int[]> {new int[] { x + sx, y + sy }});
							activeRuleSizes.Add(new int[] {5,5});
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
						if (InFutureStartRel(2,0) && InFutureEndRel(2,2) && foundSectionStart == foundSectionEnd)
						{
							FutureL = true;
							activeRules.Add("Future L");
							activeRulesForbiddenFields.Add(new List<int[]> {new int[] { x + sx, y + sy }, new int[] { x - lx, y - ly }});
							activeRuleSizes.Add(new int[] {4,3});
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
				// Double Area C-Shape
				for (int i = 0; i < 2; i++)
				{
					for (int j = 0; j < 2; j++)
					{
						if ((InTakenRel(-1,4) || InBorderRel(-1,4)) && (InTakenRel(-3,0) || InBorderRel(-3,0)) && !InTakenRel(0,3) && !InBorderRel(0,3) && !InTakenRel(0,2) && !InBorderRel(0,2) && !InTakenRel(-1,3) && !InBorderRel(-1,3) && !InTakenRel(1,3) && !InBorderRel(1,3) && !InTakenRel(1,1) && !InBorderRel(1,1) && !InTakenRel(-2,0) && !InBorderRel(-2,0))
						{
							bool DoubleAreaCShape_circle1 = false;
							directionFieldIndex = InTakenIndexRel(-1,4);
							if (directionFieldIndex != -1)
							{
								if (InTakenRel(0,4))
								{
									int leftIndex = InTakenIndexRel(0,4);
									if (leftIndex > directionFieldIndex)
									{
										DoubleAreaCShape_circle1 = true;
									}
								}
								else
								{
									int rightIndex = InTakenIndexRel(-2,4);
									if (rightIndex < directionFieldIndex)
									{
										DoubleAreaCShape_circle1 = true;
									}
								}
							}
							else
							{
								directionFieldIndex = InBorderIndexRel(-1,4);
								int farSideIndex = InBorderIndexRel(-2,4);
								if (farSideIndex > directionFieldIndex)
								{
									DoubleAreaCShape_circle1 = true;
								}
							}
							
							bool DoubleAreaCShape_circle2 = false;
							directionFieldIndex = InTakenIndexRel(-3,0);
							if (directionFieldIndex != -1)
							{
								if (InTakenRel(-3,1))
								{
									int leftIndex = InTakenIndexRel(-3,1);
									if (leftIndex < directionFieldIndex)
									{
										DoubleAreaCShape_circle2 = true;
									}
								}
								else
								{
									int rightIndex = InTakenIndexRel(-3,-1);
									if (rightIndex > directionFieldIndex)
									{
										DoubleAreaCShape_circle2 = true;
									}
								}
							}
							else
							{
								directionFieldIndex = InBorderIndexRel(-3,0);
								int farSideIndex = InBorderIndexRel(-3,1);
								if (farSideIndex > directionFieldIndex)
								{
									DoubleAreaCShape_circle2 = true;
								}
							}
							
							ResetExamAreas();
							if (DoubleAreaCShape_circle1 && DoubleAreaCShape_circle2 && CountAreaRel(0,1,0,3,new List<int[]> {new int[] {0,2}},i==0?true:!true,0) && CountAreaRel(-1,0,-2,0,null,i==0?false:!false,0))
							{
								DoubleAreaCShape = true;
								activeRules.Add("Double Area C-Shape");
								activeRulesForbiddenFields.Add(new List<int[]> {new int[] { x + sx, y + sy }, new int[] { x + lx, y + ly }});
								activeRuleSizes.Add(new int[] {5,5});
								AddExamAreas();
								forbidden.Add(new int[] { x + sx, y + sy });
								forbidden.Add(new int[] { x + lx, y + ly });
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

				// Double Area Stair 2
				for (int i = 0; i < 2; i++)
				{
					for (int j = 0; j < 2; j++)
					{
						if ((InTakenRel(-3,1) || InBorderRel(-3,1)) && (InTakenRel(1,3) || InBorderRel(1,3)) && !InTakenRel(-2,1) && !InBorderRel(-2,1) && InTakenRel(0,4) && InTakenRel(-1,5) && !InTakenRel(0,3) && !InBorderRel(0,3) && !InTakenRel(-1,4) && !InBorderRel(-1,4) && !InTakenRel(1,2) && !InBorderRel(1,2))
						{
							bool DoubleAreaStair2_circle1 = false;
							directionFieldIndex = InTakenIndexRel(1,3);
							if (directionFieldIndex != -1)
							{
								if (InTakenRel(2,3))
								{
									int leftIndex = InTakenIndexRel(2,3);
									if (leftIndex > directionFieldIndex)
									{
										DoubleAreaStair2_circle1 = true;
									}
								}
								else
								{
									int rightIndex = InTakenIndexRel(0,3);
									if (rightIndex < directionFieldIndex)
									{
										DoubleAreaStair2_circle1 = true;
									}
								}
							}
							else
							{
								directionFieldIndex = InBorderIndexRel(1,3);
								int farSideIndex = InBorderIndexRel(0,3);
								if (farSideIndex > directionFieldIndex)
								{
									DoubleAreaStair2_circle1 = true;
								}
							}
							
							bool DoubleAreaStair2_circle2 = false;
							directionFieldIndex = InTakenIndexRel(-3,1);
							if (directionFieldIndex != -1)
							{
								if (InTakenRel(-3,2))
								{
									int leftIndex = InTakenIndexRel(-3,2);
									if (leftIndex < directionFieldIndex)
									{
										DoubleAreaStair2_circle2 = true;
									}
								}
								else
								{
									int rightIndex = InTakenIndexRel(-3,0);
									if (rightIndex > directionFieldIndex)
									{
										DoubleAreaStair2_circle2 = true;
									}
								}
							}
							else
							{
								directionFieldIndex = InBorderIndexRel(-3,1);
								int farSideIndex = InBorderIndexRel(-3,2);
								if (farSideIndex > directionFieldIndex)
								{
									DoubleAreaStair2_circle2 = true;
								}
							}
							
							ResetExamAreas();
							if (DoubleAreaStair2_circle1 && DoubleAreaStair2_circle2 && CountAreaRel(1,1,1,2,null,i==0?true:!true,0) && CountAreaRel(-1,1,-2,1,null,i==0?false:!false,0))
							{
								DoubleAreaStair2 = true;
								activeRules.Add("Double Area Stair 2");
								activeRulesForbiddenFields.Add(new List<int[]> {new int[] { x + sx, y + sy }, new int[] { x - lx, y - ly }});
								activeRuleSizes.Add(new int[] {5,6});
								AddExamAreas();
								forbidden.Add(new int[] { x + sx, y + sy });
								forbidden.Add(new int[] { x - lx, y - ly });
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

				// Double Area Stair Area
				for (int i = 0; i < 2; i++)
				{
					for (int j = 0; j < 2; j++)
					{
						if ((InTakenRel(1,3) || InBorderRel(1,3)) && InTakenRel(0,4) && (InTakenRel(-3,5) || InBorderRel(-3,5)) && (InTakenRel(-3,1) || InBorderRel(-3,1)) && !InTakenRel(-1,4) && !InBorderRel(-1,4) && !InTakenRel(-2,4) && !InBorderRel(-2,4) && !InTakenRel(-1,5) && !InBorderRel(-1,5) && !InTakenRel(-2,5) && !InBorderRel(-2,5) && !InTakenRel(0,3) && !InBorderRel(0,3) && !InTakenRel(1,2) && !InBorderRel(1,2) && !InTakenRel(-2,1) && !InBorderRel(-2,1))
						{
							bool DoubleAreaStairArea_circle1 = false;
							directionFieldIndex = InTakenIndexRel(1,3);
							if (directionFieldIndex != -1)
							{
								if (InTakenRel(2,3))
								{
									int leftIndex = InTakenIndexRel(2,3);
									if (leftIndex > directionFieldIndex)
									{
										DoubleAreaStairArea_circle1 = true;
									}
								}
								else
								{
									int rightIndex = InTakenIndexRel(0,3);
									if (rightIndex < directionFieldIndex)
									{
										DoubleAreaStairArea_circle1 = true;
									}
								}
							}
							else
							{
								directionFieldIndex = InBorderIndexRel(1,3);
								int farSideIndex = InBorderIndexRel(0,3);
								if (farSideIndex > directionFieldIndex)
								{
									DoubleAreaStairArea_circle1 = true;
								}
							}
							
							bool DoubleAreaStairArea_circle2 = false;
							directionFieldIndex = InTakenIndexRel(-3,5);
							if (directionFieldIndex != -1)
							{
								if (InTakenRel(-3,6))
								{
									int leftIndex = InTakenIndexRel(-3,6);
									if (leftIndex > directionFieldIndex)
									{
										DoubleAreaStairArea_circle2 = true;
									}
								}
								else
								{
									int rightIndex = InTakenIndexRel(-3,4);
									if (rightIndex < directionFieldIndex)
									{
										DoubleAreaStairArea_circle2 = true;
									}
								}
							}
							else
							{
								directionFieldIndex = InBorderIndexRel(-3,5);
								int farSideIndex = InBorderIndexRel(-3,4);
								if (farSideIndex > directionFieldIndex)
								{
									DoubleAreaStairArea_circle2 = true;
								}
							}
							
							bool DoubleAreaStairArea_circle3 = false;
							directionFieldIndex = InTakenIndexRel(-3,1);
							if (directionFieldIndex != -1)
							{
								if (InTakenRel(-3,2))
								{
									int leftIndex = InTakenIndexRel(-3,2);
									if (leftIndex < directionFieldIndex)
									{
										DoubleAreaStairArea_circle3 = true;
									}
								}
								else
								{
									int rightIndex = InTakenIndexRel(-3,0);
									if (rightIndex > directionFieldIndex)
									{
										DoubleAreaStairArea_circle3 = true;
									}
								}
							}
							else
							{
								directionFieldIndex = InBorderIndexRel(-3,1);
								int farSideIndex = InBorderIndexRel(-3,2);
								if (farSideIndex > directionFieldIndex)
								{
									DoubleAreaStairArea_circle3 = true;
								}
							}
							
							ResetExamAreas();
							if (DoubleAreaStairArea_circle1 && DoubleAreaStairArea_circle2 && DoubleAreaStairArea_circle3 && CountAreaRel(1,1,1,2,null,i==0?true:!true,0) && CountAreaRel(-1,1,-2,1,null,i==0?false:!false,0))
							{
								DoubleAreaStairArea = true;
								activeRules.Add("Double Area Stair Area");
								activeRulesForbiddenFields.Add(new List<int[]> {new int[] { x - lx, y - ly }, new int[] { x + sx, y + sy }});
								activeRuleSizes.Add(new int[] {5,6});
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

				// Double Area Stair
				for (int i = 0; i < 2; i++)
				{
					for (int j = 0; j < 2; j++)
					{
						if (InTakenRel(-1,-1) && (InTakenRel(0,3) || InBorderRel(0,3)) && (InTakenRel(-3,3) || InBorderRel(-3,3)) && InTakenRel(-4,2) && InTakenRel(-5,1) && !InTakenRel(-3,2) && !InBorderRel(-3,2) && !InTakenRel(-4,1) && !InBorderRel(-4,1) && !InTakenRel(0,2) && !InBorderRel(0,2) && !InTakenRel(-1,3) && !InBorderRel(-1,3) && !InTakenRel(-2,3) && !InBorderRel(-2,3) && !InTakenRel(-1,0) && !InBorderRel(-1,0))
						{
							bool DoubleAreaStair_circle1 = false;
							directionFieldIndex = InTakenIndexRel(0,3);
							if (directionFieldIndex != -1)
							{
								if (InTakenRel(1,3))
								{
									int leftIndex = InTakenIndexRel(1,3);
									if (leftIndex > directionFieldIndex)
									{
										DoubleAreaStair_circle1 = true;
									}
								}
								else
								{
									int rightIndex = InTakenIndexRel(-1,3);
									if (rightIndex < directionFieldIndex)
									{
										DoubleAreaStair_circle1 = true;
									}
								}
							}
							else
							{
								directionFieldIndex = InBorderIndexRel(0,3);
								int farSideIndex = InBorderIndexRel(-1,3);
								if (farSideIndex > directionFieldIndex)
								{
									DoubleAreaStair_circle1 = true;
								}
							}
							
							bool DoubleAreaStair_circle2 = false;
							directionFieldIndex = InTakenIndexRel(-3,3);
							if (directionFieldIndex != -1)
							{
								if (InTakenRel(-3,4))
								{
									int leftIndex = InTakenIndexRel(-3,4);
									if (leftIndex > directionFieldIndex)
									{
										DoubleAreaStair_circle2 = true;
									}
								}
								else
								{
									int rightIndex = InTakenIndexRel(-3,2);
									if (rightIndex < directionFieldIndex)
									{
										DoubleAreaStair_circle2 = true;
									}
								}
							}
							else
							{
								directionFieldIndex = InBorderIndexRel(-3,3);
								int farSideIndex = InBorderIndexRel(-3,2);
								if (farSideIndex > directionFieldIndex)
								{
									DoubleAreaStair_circle2 = true;
								}
							}
							
							ResetExamAreas();
							if (DoubleAreaStair_circle1 && DoubleAreaStair_circle2 && CountAreaRel(0,1,0,2,null,i==0?true:!true,0))
							{
								DoubleAreaStair = true;
								activeRules.Add("Double Area Stair");
								activeRulesForbiddenFields.Add(new List<int[]> {new int[] { x + sx, y + sy }});
								activeRuleSizes.Add(new int[] {6,5});
								AddExamAreas();
								forbidden.Add(new int[] { x + sx, y + sy });
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

				// Double C-Shape Start C
				for (int i = 0; i < 2; i++)
				{
					for (int j = 0; j < 2; j++)
					{
						if (InTakenRel(1,0) && InTakenRel(2,1) && (InTakenRel(0,5) || InBorderRel(0,5)) && !InTakenRel(0,4) && !InBorderRel(0,4) && !InTakenRel(2,4) && !InBorderRel(2,4) && !InTakenRel(1,4) && !InBorderRel(1,4) && !InTakenRel(1,3) && !InBorderRel(1,3) && !InTakenRel(1,2) && !InBorderRel(1,2) && !InTakenRel(0,2) && !InBorderRel(0,2) && !InTakenRel(2,2) && !InBorderRel(2,2) && !InTakenRel(1,1) && !InBorderRel(1,1))
						{
							bool DoubleCShapeStartC_circle1 = false;
							directionFieldIndex = InTakenIndexRel(0,5);
							if (directionFieldIndex != -1)
							{
								if (InTakenRel(1,5))
								{
									int leftIndex = InTakenIndexRel(1,5);
									if (leftIndex > directionFieldIndex)
									{
										DoubleCShapeStartC_circle1 = true;
									}
								}
								else
								{
									int rightIndex = InTakenIndexRel(-1,5);
									if (rightIndex < directionFieldIndex)
									{
										DoubleCShapeStartC_circle1 = true;
									}
								}
							}
							else
							{
								directionFieldIndex = InBorderIndexRel(0,5);
								int farSideIndex = InBorderIndexRel(-1,5);
								if (farSideIndex > directionFieldIndex)
								{
									DoubleCShapeStartC_circle1 = true;
								}
							}
							
							ResetExamAreas();
							if (DoubleCShapeStartC_circle1 && CountAreaRel(1,2,1,4,new List<int[]> {new int[] {1,3}},i==0?true:!true,0))
							{
								DoubleCShapeStartC = true;
								activeRules.Add("Double C-Shape Start C");
								activeRulesForbiddenFields.Add(new List<int[]> {new int[] { x + sx, y + sy }});
								activeRuleSizes.Add(new int[] {3,6});
								AddExamAreas();
								forbidden.Add(new int[] { x + sx, y + sy });
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

				// Square 4 x 2 Area
				for (int i = 0; i < 2; i++)
				{
					for (int j = 0; j < 2; j++)
					{
						if ((InTakenRel(0,3) || InBorderRel(0,3)) && (InTakenRel(-3,4) || InBorderRel(-3,4)) && (InTakenRel(-3,0) || InBorderRel(-3,0)) && !InTakenRel(-2,4) && !InBorderRel(-2,4) && !InTakenRel(-1,4) && !InBorderRel(-1,4) && !InTakenRel(-2,3) && !InBorderRel(-2,3) && !InTakenRel(-1,3) && !InBorderRel(-1,3) && !InTakenRel(-2,0) && !InBorderRel(-2,0) && !InTakenRel(0,2) && !InBorderRel(0,2))
						{
							bool Square4x2Area_circle1 = false;
							directionFieldIndex = InTakenIndexRel(-3,0);
							if (directionFieldIndex != -1)
							{
								if (InTakenRel(-3,1))
								{
									int leftIndex = InTakenIndexRel(-3,1);
									if (leftIndex < directionFieldIndex)
									{
										Square4x2Area_circle1 = true;
									}
								}
								else
								{
									int rightIndex = InTakenIndexRel(-3,-1);
									if (rightIndex > directionFieldIndex)
									{
										Square4x2Area_circle1 = true;
									}
								}
							}
							else
							{
								directionFieldIndex = InBorderIndexRel(-3,0);
								int farSideIndex = InBorderIndexRel(-3,1);
								if (farSideIndex > directionFieldIndex)
								{
									Square4x2Area_circle1 = true;
								}
							}
							
							bool Square4x2Area_circle2 = false;
							directionFieldIndex = InTakenIndexRel(0,3);
							if (directionFieldIndex != -1)
							{
								if (InTakenRel(1,3))
								{
									int leftIndex = InTakenIndexRel(1,3);
									if (leftIndex > directionFieldIndex)
									{
										Square4x2Area_circle2 = true;
									}
								}
								else
								{
									int rightIndex = InTakenIndexRel(-1,3);
									if (rightIndex < directionFieldIndex)
									{
										Square4x2Area_circle2 = true;
									}
								}
							}
							else
							{
								directionFieldIndex = InBorderIndexRel(0,3);
								int farSideIndex = InBorderIndexRel(-1,3);
								if (farSideIndex > directionFieldIndex)
								{
									Square4x2Area_circle2 = true;
								}
							}
							
							bool Square4x2Area_circle3 = false;
							directionFieldIndex = InTakenIndexRel(-3,4);
							if (directionFieldIndex != -1)
							{
								if (InTakenRel(-3,5))
								{
									int leftIndex = InTakenIndexRel(-3,5);
									if (leftIndex > directionFieldIndex)
									{
										Square4x2Area_circle3 = true;
									}
								}
								else
								{
									int rightIndex = InTakenIndexRel(-3,3);
									if (rightIndex < directionFieldIndex)
									{
										Square4x2Area_circle3 = true;
									}
								}
							}
							else
							{
								directionFieldIndex = InBorderIndexRel(-3,4);
								int farSideIndex = InBorderIndexRel(-3,3);
								if (farSideIndex > directionFieldIndex)
								{
									Square4x2Area_circle3 = true;
								}
							}
							
							ResetExamAreas();
							if (Square4x2Area_circle1 && Square4x2Area_circle2 && Square4x2Area_circle3 && CountAreaRel(-1,0,-2,0,null,i==0?false:!false,0) && CountAreaRel(0,1,0,2,null,i==0?true:!true,0))
							{
								Square4x2Area = true;
								activeRules.Add("Square 4 x 2 Area");
								activeRulesForbiddenFields.Add(new List<int[]> {new int[] { x + sx, y + sy }, new int[] { x + lx, y + ly }});
								activeRuleSizes.Add(new int[] {5,5});
								AddExamAreas();
								forbidden.Add(new int[] { x + sx, y + sy });
								forbidden.Add(new int[] { x + lx, y + ly });
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

				// Square 4 x 2 C-Shape
				for (int i = 0; i < 2; i++)
				{
					for (int j = 0; j < 2; j++)
					{
						if ((InTakenRel(0,3) || InBorderRel(0,3)) && (InTakenRel(-3,0) || InBorderRel(-3,0)) && InTakenRel(-1,4) && !InTakenRel(-1,3) && !InBorderRel(-1,3) && !InTakenRel(0,2) && !InBorderRel(0,2) && !InTakenRel(-2,0) && !InBorderRel(-2,0))
						{
							bool Square4x2CShape_circle1 = false;
							directionFieldIndex = InTakenIndexRel(0,3);
							if (directionFieldIndex != -1)
							{
								if (InTakenRel(1,3))
								{
									int leftIndex = InTakenIndexRel(1,3);
									if (leftIndex > directionFieldIndex)
									{
										Square4x2CShape_circle1 = true;
									}
								}
								else
								{
									int rightIndex = InTakenIndexRel(-1,3);
									if (rightIndex < directionFieldIndex)
									{
										Square4x2CShape_circle1 = true;
									}
								}
							}
							else
							{
								directionFieldIndex = InBorderIndexRel(0,3);
								int farSideIndex = InBorderIndexRel(-1,3);
								if (farSideIndex > directionFieldIndex)
								{
									Square4x2CShape_circle1 = true;
								}
							}
							
							bool Square4x2CShape_circle2 = false;
							directionFieldIndex = InTakenIndexRel(-3,0);
							if (directionFieldIndex != -1)
							{
								if (InTakenRel(-3,1))
								{
									int leftIndex = InTakenIndexRel(-3,1);
									if (leftIndex < directionFieldIndex)
									{
										Square4x2CShape_circle2 = true;
									}
								}
								else
								{
									int rightIndex = InTakenIndexRel(-3,-1);
									if (rightIndex > directionFieldIndex)
									{
										Square4x2CShape_circle2 = true;
									}
								}
							}
							else
							{
								directionFieldIndex = InBorderIndexRel(-3,0);
								int farSideIndex = InBorderIndexRel(-3,1);
								if (farSideIndex > directionFieldIndex)
								{
									Square4x2CShape_circle2 = true;
								}
							}
							
							ResetExamAreas();
							if (Square4x2CShape_circle1 && Square4x2CShape_circle2 && CountAreaRel(0,1,0,2,null,i==0?true:!true,0) && CountAreaRel(-1,0,-2,0,null,i==0?false:!false,0))
							{
								Square4x2CShape = true;
								activeRules.Add("Square 4 x 2 C-Shape");
								activeRulesForbiddenFields.Add(new List<int[]> {new int[] { x + sx, y + sy }, new int[] { x + lx, y + ly }});
								activeRuleSizes.Add(new int[] {5,5});
								AddExamAreas();
								forbidden.Add(new int[] { x + sx, y + sy });
								forbidden.Add(new int[] { x + lx, y + ly });
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

				// Straight Across 3 End Area
				for (int i = 0; i < 2; i++)
				{
					for (int j = 0; j < 2; j++)
					{
						if ((InTakenRel(2,4) || InBorderRel(2,4)) && (InTakenRel(-1,4) || InBorderRel(-1,4)) && !InTakenRel(1,3) && !InBorderRel(1,3) && !InTakenRel(1,2) && !InBorderRel(1,2) && !InTakenRel(1,1) && !InBorderRel(1,1) && !InTakenRel(2,3) && !InBorderRel(2,3) && !InTakenRel(2,1) && !InBorderRel(2,1) && !InTakenRel(1,4) && !InBorderRel(1,4) && !InTakenRel(0,4) && !InBorderRel(0,4) && !InTakenRel(1,0) && !InBorderRel(1,0))
						{
							bool StraightAcross3EndArea_circle1 = false;
							directionFieldIndex = InTakenIndexRel(-1,4);
							if (directionFieldIndex != -1)
							{
								if (InTakenRel(-1,5))
								{
									int leftIndex = InTakenIndexRel(-1,5);
									if (leftIndex > directionFieldIndex)
									{
										StraightAcross3EndArea_circle1 = true;
									}
								}
								else
								{
									int rightIndex = InTakenIndexRel(-1,3);
									if (rightIndex < directionFieldIndex)
									{
										StraightAcross3EndArea_circle1 = true;
									}
								}
							}
							else
							{
								directionFieldIndex = InBorderIndexRel(-1,4);
								int farSideIndex = InBorderIndexRel(-1,3);
								if (farSideIndex > directionFieldIndex)
								{
									StraightAcross3EndArea_circle1 = true;
								}
							}
							
							bool StraightAcross3EndArea_circle2 = false;
							directionFieldIndex = InTakenIndexRel(2,4);
							if (directionFieldIndex != -1)
							{
								if (InTakenRel(3,4))
								{
									int leftIndex = InTakenIndexRel(3,4);
									if (leftIndex > directionFieldIndex)
									{
										StraightAcross3EndArea_circle2 = true;
									}
								}
								else
								{
									int rightIndex = InTakenIndexRel(1,4);
									if (rightIndex < directionFieldIndex)
									{
										StraightAcross3EndArea_circle2 = true;
									}
								}
							}
							else
							{
								directionFieldIndex = InBorderIndexRel(2,4);
								int farSideIndex = InBorderIndexRel(1,4);
								if (farSideIndex > directionFieldIndex)
								{
									StraightAcross3EndArea_circle2 = true;
								}
							}
							
							ResetExamAreas();
							if (StraightAcross3EndArea_circle1 && StraightAcross3EndArea_circle2 && CountAreaRel(1,1,1,3,new List<int[]> {new int[] {1,2}},i==0?true:!true,1))
							{
								StraightAcross3EndArea = true;
								activeRules.Add("Straight Across 3 End Area");
								activeRulesForbiddenFields.Add(new List<int[]> {new int[] { x + lx, y + ly }});
								activeRuleSizes.Add(new int[] {4,5});
								AddExamAreas();
								forbidden.Add(new int[] { x + lx, y + ly });
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

				// Straight Across End Area
				for (int i = 0; i < 2; i++)
				{
					for (int j = 0; j < 2; j++)
					{
						if ((InTakenRel(2,3) || InBorderRel(2,3)) && (InTakenRel(-1,4) || InBorderRel(-1,4)) && !InTakenRel(1,3) && !InBorderRel(1,3) && !InTakenRel(2,2) && !InBorderRel(2,2) && !InTakenRel(2,1) && !InBorderRel(2,1) && !InTakenRel(0,3) && !InBorderRel(0,3) && !InTakenRel(0,1) && !InBorderRel(0,1) && !InTakenRel(1,0) && !InBorderRel(1,0) && !InTakenRel(1,4) && !InBorderRel(1,4) && !InTakenRel(0,4) && !InBorderRel(0,4))
						{
							bool StraightAcrossEndArea_circle1 = false;
							directionFieldIndex = InTakenIndexRel(-1,4);
							if (directionFieldIndex != -1)
							{
								if (InTakenRel(-1,5))
								{
									int leftIndex = InTakenIndexRel(-1,5);
									if (leftIndex > directionFieldIndex)
									{
										StraightAcrossEndArea_circle1 = true;
									}
								}
								else
								{
									int rightIndex = InTakenIndexRel(-1,3);
									if (rightIndex < directionFieldIndex)
									{
										StraightAcrossEndArea_circle1 = true;
									}
								}
							}
							else
							{
								directionFieldIndex = InBorderIndexRel(-1,4);
								int farSideIndex = InBorderIndexRel(-1,3);
								if (farSideIndex > directionFieldIndex)
								{
									StraightAcrossEndArea_circle1 = true;
								}
							}
							
							bool StraightAcrossEndArea_circle2 = false;
							directionFieldIndex = InTakenIndexRel(2,3);
							if (directionFieldIndex != -1)
							{
								if (InTakenRel(3,3))
								{
									int leftIndex = InTakenIndexRel(3,3);
									if (leftIndex > directionFieldIndex)
									{
										StraightAcrossEndArea_circle2 = true;
									}
								}
								else
								{
									int rightIndex = InTakenIndexRel(1,3);
									if (rightIndex < directionFieldIndex)
									{
										StraightAcrossEndArea_circle2 = true;
									}
								}
							}
							else
							{
								directionFieldIndex = InBorderIndexRel(2,3);
								int farSideIndex = InBorderIndexRel(1,3);
								if (farSideIndex > directionFieldIndex)
								{
									StraightAcrossEndArea_circle2 = true;
								}
							}
							
							ResetExamAreas();
							if (StraightAcrossEndArea_circle1 && StraightAcrossEndArea_circle2 && CountAreaRel(1,1,1,2,null,i==0?true:!true,0))
							{
								StraightAcrossEndArea = true;
								activeRules.Add("Straight Across End Area");
								activeRulesForbiddenFields.Add(new List<int[]> {new int[] { x + lx, y + ly }});
								activeRuleSizes.Add(new int[] {4,5});
								AddExamAreas();
								forbidden.Add(new int[] { x + lx, y + ly });
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

				// Straight Across End C
				for (int i = 0; i < 2; i++)
				{
					for (int j = 0; j < 2; j++)
					{
						if ((InTakenRel(2,3) || InBorderRel(2,3)) && !InTakenRel(1,3) && !InBorderRel(1,3) && InTakenRel(1,4) && !InTakenRel(0,1) && !InBorderRel(0,1) && !InTakenRel(2,2) && !InBorderRel(2,2) && !InTakenRel(2,1) && !InBorderRel(2,1) && !InTakenRel(1,0) && !InBorderRel(1,0))
						{
							bool StraightAcrossEndC_circle1 = false;
							directionFieldIndex = InTakenIndexRel(2,3);
							if (directionFieldIndex != -1)
							{
								if (InTakenRel(3,3))
								{
									int leftIndex = InTakenIndexRel(3,3);
									if (leftIndex > directionFieldIndex)
									{
										StraightAcrossEndC_circle1 = true;
									}
								}
								else
								{
									int rightIndex = InTakenIndexRel(1,3);
									if (rightIndex < directionFieldIndex)
									{
										StraightAcrossEndC_circle1 = true;
									}
								}
							}
							else
							{
								directionFieldIndex = InBorderIndexRel(2,3);
								int farSideIndex = InBorderIndexRel(1,3);
								if (farSideIndex > directionFieldIndex)
								{
									StraightAcrossEndC_circle1 = true;
								}
							}
							
							ResetExamAreas();
							if (StraightAcrossEndC_circle1 && CountAreaRel(1,1,1,2,null,i==0?true:!true,0))
							{
								StraightAcrossEndC = true;
								activeRules.Add("Straight Across End C");
								activeRulesForbiddenFields.Add(new List<int[]> {new int[] { x + lx, y + ly }});
								activeRuleSizes.Add(new int[] {3,5});
								AddExamAreas();
								forbidden.Add(new int[] { x + lx, y + ly });
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

				// Straight Mid Across 3 End Area
				for (int i = 0; i < 2; i++)
				{
					for (int j = 0; j < 2; j++)
					{
						if ((InTakenRel(1,4) || InBorderRel(1,4)) && (InTakenRel(-2,5) || InBorderRel(-2,5)) && !InTakenRel(0,4) && !InBorderRel(0,4) && !InTakenRel(-1,4) && !InBorderRel(-1,4) && !InTakenRel(-1,5) && !InBorderRel(-1,5) && !InTakenRel(0,5) && !InBorderRel(0,5) && !InTakenRel(0,3) && !InBorderRel(0,3) && !InTakenRel(0,2) && !InBorderRel(0,2) && !InTakenRel(1,1) && !InBorderRel(1,1) && !InTakenRel(1,3) && !InBorderRel(1,3))
						{
							bool StraightMidAcross3EndArea_circle1 = false;
							directionFieldIndex = InTakenIndexRel(-2,5);
							if (directionFieldIndex != -1)
							{
								if (InTakenRel(-2,6))
								{
									int leftIndex = InTakenIndexRel(-2,6);
									if (leftIndex > directionFieldIndex)
									{
										StraightMidAcross3EndArea_circle1 = true;
									}
								}
								else
								{
									int rightIndex = InTakenIndexRel(-2,4);
									if (rightIndex < directionFieldIndex)
									{
										StraightMidAcross3EndArea_circle1 = true;
									}
								}
							}
							else
							{
								directionFieldIndex = InBorderIndexRel(-2,5);
								int farSideIndex = InBorderIndexRel(-2,4);
								if (farSideIndex > directionFieldIndex)
								{
									StraightMidAcross3EndArea_circle1 = true;
								}
							}
							
							bool StraightMidAcross3EndArea_circle2 = false;
							directionFieldIndex = InTakenIndexRel(1,4);
							if (directionFieldIndex != -1)
							{
								if (InTakenRel(2,4))
								{
									int leftIndex = InTakenIndexRel(2,4);
									if (leftIndex > directionFieldIndex)
									{
										StraightMidAcross3EndArea_circle2 = true;
									}
								}
								else
								{
									int rightIndex = InTakenIndexRel(0,4);
									if (rightIndex < directionFieldIndex)
									{
										StraightMidAcross3EndArea_circle2 = true;
									}
								}
							}
							else
							{
								directionFieldIndex = InBorderIndexRel(1,4);
								int farSideIndex = InBorderIndexRel(0,4);
								if (farSideIndex > directionFieldIndex)
								{
									StraightMidAcross3EndArea_circle2 = true;
								}
							}
							
							ResetExamAreas();
							if (StraightMidAcross3EndArea_circle1 && StraightMidAcross3EndArea_circle2 && CountAreaRel(0,1,0,3,new List<int[]> {new int[] {0,2}},i==0?true:!true,0))
							{
								StraightMidAcross3EndArea = true;
								activeRules.Add("Straight Mid Across 3 End Area");
								activeRulesForbiddenFields.Add(new List<int[]> {new int[] { x + sx, y + sy }});
								activeRuleSizes.Add(new int[] {4,6});
								AddExamAreas();
								forbidden.Add(new int[] { x + sx, y + sy });
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

				// Straight Mid Across 3 End C
				for (int i = 0; i < 2; i++)
				{
					for (int j = 0; j < 2; j++)
					{
						if ((InTakenRel(1,4) || InBorderRel(1,4)) && InTakenRel(0,5) && !InTakenRel(0,3) && !InBorderRel(0,3) && !InTakenRel(0,2) && !InBorderRel(0,2) && !InTakenRel(1,3) && !InBorderRel(1,3) && !InTakenRel(1,1) && !InBorderRel(1,1) && !InTakenRel(0,4) && !InBorderRel(0,4))
						{
							bool StraightMidAcross3EndC_circle1 = false;
							directionFieldIndex = InTakenIndexRel(1,4);
							if (directionFieldIndex != -1)
							{
								if (InTakenRel(2,4))
								{
									int leftIndex = InTakenIndexRel(2,4);
									if (leftIndex > directionFieldIndex)
									{
										StraightMidAcross3EndC_circle1 = true;
									}
								}
								else
								{
									int rightIndex = InTakenIndexRel(0,4);
									if (rightIndex < directionFieldIndex)
									{
										StraightMidAcross3EndC_circle1 = true;
									}
								}
							}
							else
							{
								directionFieldIndex = InBorderIndexRel(1,4);
								int farSideIndex = InBorderIndexRel(0,4);
								if (farSideIndex > directionFieldIndex)
								{
									StraightMidAcross3EndC_circle1 = true;
								}
							}
							
							ResetExamAreas();
							if (StraightMidAcross3EndC_circle1 && CountAreaRel(0,1,0,3,new List<int[]> {new int[] {0,2}},i==0?true:!true,0))
							{
								StraightMidAcross3EndC = true;
								activeRules.Add("Straight Mid Across 3 End C");
								activeRulesForbiddenFields.Add(new List<int[]> {new int[] { x + sx, y + sy }});
								activeRuleSizes.Add(new int[] {2,6});
								AddExamAreas();
								forbidden.Add(new int[] { x + sx, y + sy });
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

				// Triple Area Exit Down
				for (int i = 0; i < 2; i++)
				{
					for (int j = 0; j < 2; j++)
					{
						if ((InTakenRel(4,0) || InBorderRel(4,0)) && (InTakenRel(4,3) || InBorderRel(4,3)) && (InTakenRel(0,3) || InBorderRel(0,3)) && !InTakenRel(2,0) && !InBorderRel(2,0) && !InTakenRel(3,0) && !InBorderRel(3,0) && !InTakenRel(4,1) && !InBorderRel(4,1) && !InTakenRel(4,2) && !InBorderRel(4,2) && !InTakenRel(3,3) && !InBorderRel(3,3) && !InTakenRel(2,3) && !InBorderRel(2,3) && !InTakenRel(1,3) && !InBorderRel(1,3) && !InTakenRel(1,2) && !InBorderRel(1,2) && !InTakenRel(1,1) && !InBorderRel(1,1))
						{
							bool TripleAreaExitDown_circle1 = false;
							directionFieldIndex = InTakenIndexRel(4,0);
							if (directionFieldIndex != -1)
							{
								if (InTakenRel(4,-1))
								{
									int leftIndex = InTakenIndexRel(4,-1);
									if (leftIndex > directionFieldIndex)
									{
										TripleAreaExitDown_circle1 = true;
									}
								}
								else
								{
									int rightIndex = InTakenIndexRel(4,1);
									if (rightIndex < directionFieldIndex)
									{
										TripleAreaExitDown_circle1 = true;
									}
								}
							}
							else
							{
								directionFieldIndex = InBorderIndexRel(4,0);
								int farSideIndex = InBorderIndexRel(4,1);
								if (farSideIndex > directionFieldIndex)
								{
									TripleAreaExitDown_circle1 = true;
								}
							}
							
							bool TripleAreaExitDown_circle2 = false;
							directionFieldIndex = InTakenIndexRel(4,3);
							if (directionFieldIndex != -1)
							{
								if (InTakenRel(5,3))
								{
									int leftIndex = InTakenIndexRel(5,3);
									if (leftIndex > directionFieldIndex)
									{
										TripleAreaExitDown_circle2 = true;
									}
								}
								else
								{
									int rightIndex = InTakenIndexRel(3,3);
									if (rightIndex < directionFieldIndex)
									{
										TripleAreaExitDown_circle2 = true;
									}
								}
							}
							else
							{
								directionFieldIndex = InBorderIndexRel(4,3);
								int farSideIndex = InBorderIndexRel(3,3);
								if (farSideIndex > directionFieldIndex)
								{
									TripleAreaExitDown_circle2 = true;
								}
							}
							
							bool TripleAreaExitDown_circle3 = false;
							directionFieldIndex = InTakenIndexRel(0,3);
							if (directionFieldIndex != -1)
							{
								if (InTakenRel(0,4))
								{
									int leftIndex = InTakenIndexRel(0,4);
									if (leftIndex > directionFieldIndex)
									{
										TripleAreaExitDown_circle3 = true;
									}
								}
								else
								{
									int rightIndex = InTakenIndexRel(0,2);
									if (rightIndex < directionFieldIndex)
									{
										TripleAreaExitDown_circle3 = true;
									}
								}
							}
							else
							{
								directionFieldIndex = InBorderIndexRel(0,3);
								int farSideIndex = InBorderIndexRel(0,2);
								if (farSideIndex > directionFieldIndex)
								{
									TripleAreaExitDown_circle3 = true;
								}
							}
							
							ResetExamAreas();
							if (TripleAreaExitDown_circle1 && TripleAreaExitDown_circle2 && TripleAreaExitDown_circle3 && CountAreaRel(1,0,3,0,new List<int[]> {new int[] {2,0}},i==0?true:!true,1) && CountAreaRel(3,3,1,3,new List<int[]> {new int[] {2,3}},i==0?true:!true,1))
							{
								TripleAreaExitDown = true;
								activeRules.Add("Triple Area Exit Down");
								activeRulesForbiddenFields.Add(new List<int[]> {new int[] { x - lx, y - ly }, new int[] { x + sx, y + sy }});
								activeRuleSizes.Add(new int[] {6,4});
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

				// Triple Area Stair
				for (int i = 0; i < 2; i++)
				{
					for (int j = 0; j < 2; j++)
					{
						if ((InTakenRel(-5,-1) || InBorderRel(-5,-1)) && InTakenRel(-4,2) && InTakenRel(-1,-1) && !InTakenRel(-3,2) && !InBorderRel(-3,2) && (InTakenRel(-3,3) || InBorderRel(-3,3)) && (InTakenRel(0,3) || InBorderRel(0,3)) && !InTakenRel(-2,3) && !InBorderRel(-2,3) && !InTakenRel(-1,0) && !InBorderRel(-1,0) && !InTakenRel(-1,3) && !InBorderRel(-1,3) && !InTakenRel(0,2) && !InBorderRel(0,2) && !InTakenRel(-4,1) && !InBorderRel(-4,1) && !InTakenRel(-4,0) && !InBorderRel(-4,0))
						{
							bool TripleAreaStair_circle1 = false;
							directionFieldIndex = InTakenIndexRel(0,3);
							if (directionFieldIndex != -1)
							{
								if (InTakenRel(1,3))
								{
									int leftIndex = InTakenIndexRel(1,3);
									if (leftIndex > directionFieldIndex)
									{
										TripleAreaStair_circle1 = true;
									}
								}
								else
								{
									int rightIndex = InTakenIndexRel(-1,3);
									if (rightIndex < directionFieldIndex)
									{
										TripleAreaStair_circle1 = true;
									}
								}
							}
							else
							{
								directionFieldIndex = InBorderIndexRel(0,3);
								int farSideIndex = InBorderIndexRel(-1,3);
								if (farSideIndex > directionFieldIndex)
								{
									TripleAreaStair_circle1 = true;
								}
							}
							
							bool TripleAreaStair_circle2 = false;
							directionFieldIndex = InTakenIndexRel(-3,3);
							if (directionFieldIndex != -1)
							{
								if (InTakenRel(-3,4))
								{
									int leftIndex = InTakenIndexRel(-3,4);
									if (leftIndex > directionFieldIndex)
									{
										TripleAreaStair_circle2 = true;
									}
								}
								else
								{
									int rightIndex = InTakenIndexRel(-3,2);
									if (rightIndex < directionFieldIndex)
									{
										TripleAreaStair_circle2 = true;
									}
								}
							}
							else
							{
								directionFieldIndex = InBorderIndexRel(-3,3);
								int farSideIndex = InBorderIndexRel(-3,2);
								if (farSideIndex > directionFieldIndex)
								{
									TripleAreaStair_circle2 = true;
								}
							}
							
							bool TripleAreaStair_circle3 = false;
							directionFieldIndex = InTakenIndexRel(-5,-1);
							if (directionFieldIndex != -1)
							{
								if (InTakenRel(-6,-1))
								{
									int leftIndex = InTakenIndexRel(-6,-1);
									if (leftIndex > directionFieldIndex)
									{
										TripleAreaStair_circle3 = true;
									}
								}
								else
								{
									int rightIndex = InTakenIndexRel(-4,-1);
									if (rightIndex < directionFieldIndex)
									{
										TripleAreaStair_circle3 = true;
									}
								}
							}
							else
							{
								directionFieldIndex = InBorderIndexRel(-5,-1);
								int farSideIndex = InBorderIndexRel(-4,-1);
								if (farSideIndex > directionFieldIndex)
								{
									TripleAreaStair_circle3 = true;
								}
							}
							
							ResetExamAreas();
							if (TripleAreaStair_circle1 && TripleAreaStair_circle2 && TripleAreaStair_circle3 && CountAreaRel(0,1,0,2,null,i==0?true:!true,0))
							{
								TripleAreaStair = true;
								activeRules.Add("Triple Area Stair");
								activeRulesForbiddenFields.Add(new List<int[]> {new int[] { x + sx, y + sy }, new int[] { x + lx, y + ly }});
								activeRuleSizes.Add(new int[] {7,5});
								AddExamAreas();
								forbidden.Add(new int[] { x + sx, y + sy });
								forbidden.Add(new int[] { x + lx, y + ly });
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

				// Triple Area
				for (int i = 0; i < 2; i++)
				{
					for (int j = 0; j < 2; j++)
					{
						if (!InTakenRel(0,3) && !InBorderRel(0,3) && !InTakenRel(0,2) && !InBorderRel(0,2) && !InTakenRel(1,3) && !InBorderRel(1,3) && !InTakenRel(1,1) && !InBorderRel(1,1) && (InTakenRel(0,4) || InBorderRel(0,4)) && (InTakenRel(-3,0) || InBorderRel(-3,0)) && !InTakenRel(-2,0) && !InBorderRel(-2,0) && (InTakenRel(-3,4) || InBorderRel(-3,4)) && !InTakenRel(-1,4) && !InBorderRel(-1,4) && !InTakenRel(-2,4) && !InBorderRel(-2,4))
						{
							bool TripleArea_circle1 = false;
							directionFieldIndex = InTakenIndexRel(0,4);
							if (directionFieldIndex != -1)
							{
								if (InTakenRel(1,4))
								{
									int leftIndex = InTakenIndexRel(1,4);
									if (leftIndex > directionFieldIndex)
									{
										TripleArea_circle1 = true;
									}
								}
								else
								{
									int rightIndex = InTakenIndexRel(-1,4);
									if (rightIndex < directionFieldIndex)
									{
										TripleArea_circle1 = true;
									}
								}
							}
							else
							{
								directionFieldIndex = InBorderIndexRel(0,4);
								int farSideIndex = InBorderIndexRel(-1,4);
								if (farSideIndex > directionFieldIndex)
								{
									TripleArea_circle1 = true;
								}
							}
							
							bool TripleArea_circle2 = false;
							directionFieldIndex = InTakenIndexRel(-3,0);
							if (directionFieldIndex != -1)
							{
								if (InTakenRel(-3,1))
								{
									int leftIndex = InTakenIndexRel(-3,1);
									if (leftIndex < directionFieldIndex)
									{
										TripleArea_circle2 = true;
									}
								}
								else
								{
									int rightIndex = InTakenIndexRel(-3,-1);
									if (rightIndex > directionFieldIndex)
									{
										TripleArea_circle2 = true;
									}
								}
							}
							else
							{
								directionFieldIndex = InBorderIndexRel(-3,0);
								int farSideIndex = InBorderIndexRel(-3,1);
								if (farSideIndex > directionFieldIndex)
								{
									TripleArea_circle2 = true;
								}
							}
							
							bool TripleArea_circle3 = false;
							directionFieldIndex = InTakenIndexRel(-3,4);
							if (directionFieldIndex != -1)
							{
								if (InTakenRel(-3,5))
								{
									int leftIndex = InTakenIndexRel(-3,5);
									if (leftIndex > directionFieldIndex)
									{
										TripleArea_circle3 = true;
									}
								}
								else
								{
									int rightIndex = InTakenIndexRel(-3,3);
									if (rightIndex < directionFieldIndex)
									{
										TripleArea_circle3 = true;
									}
								}
							}
							else
							{
								directionFieldIndex = InBorderIndexRel(-3,4);
								int farSideIndex = InBorderIndexRel(-3,3);
								if (farSideIndex > directionFieldIndex)
								{
									TripleArea_circle3 = true;
								}
							}
							
							ResetExamAreas();
							if (TripleArea_circle1 && TripleArea_circle2 && TripleArea_circle3 && CountAreaRel(0,1,0,3,new List<int[]> {new int[] {0,2}},i==0?true:!true,0) && CountAreaRel(-1,0,-2,0,null,i==0?false:!false,0))
							{
								TripleArea = true;
								activeRules.Add("Triple Area");
								activeRulesForbiddenFields.Add(new List<int[]> {new int[] { x + sx, y + sy }});
								activeRuleSizes.Add(new int[] {5,5});
								AddExamAreas();
								forbidden.Add(new int[] { x + sx, y + sy });
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
			}

			if (size >= 13)
			{
				// Straight Mid Across 3 End Area 2
				for (int i = 0; i < 2; i++)
				{
					for (int j = 0; j < 2; j++)
					{
						if ((InTakenRel(1,4) || InBorderRel(1,4)) && (InTakenRel(-2,4) || InBorderRel(-2,4)) && !InTakenRel(0,4) && !InBorderRel(0,4) && !InTakenRel(0,5) && !InBorderRel(0,5) && !InTakenRel(-1,5) && !InBorderRel(-1,5) && !InTakenRel(-1,4) && !InBorderRel(-1,4) && !InTakenRel(0,3) && !InBorderRel(0,3) && !InTakenRel(0,1) && !InBorderRel(0,1) && !InTakenRel(0,2) && !InBorderRel(0,2) && !InTakenRel(1,3) && !InBorderRel(1,3) && !InTakenRel(1,1) && !InBorderRel(1,1))
						{
							bool StraightMidAcross3EndArea2_circle1 = false;
							directionFieldIndex = InTakenIndexRel(1,4);
							if (directionFieldIndex != -1)
							{
								if (InTakenRel(2,4))
								{
									int leftIndex = InTakenIndexRel(2,4);
									if (leftIndex > directionFieldIndex)
									{
										StraightMidAcross3EndArea2_circle1 = true;
									}
								}
								else
								{
									int rightIndex = InTakenIndexRel(0,4);
									if (rightIndex < directionFieldIndex)
									{
										StraightMidAcross3EndArea2_circle1 = true;
									}
								}
							}
							else
							{
								directionFieldIndex = InBorderIndexRel(1,4);
								int farSideIndex = InBorderIndexRel(0,4);
								if (farSideIndex > directionFieldIndex)
								{
									StraightMidAcross3EndArea2_circle1 = true;
								}
							}
							
							bool StraightMidAcross3EndArea2_circle2 = false;
							directionFieldIndex = InTakenIndexRel(-2,4);
							if (directionFieldIndex != -1)
							{
								if (InTakenRel(-2,5))
								{
									int leftIndex = InTakenIndexRel(-2,5);
									if (leftIndex > directionFieldIndex)
									{
										StraightMidAcross3EndArea2_circle2 = true;
									}
								}
								else
								{
									int rightIndex = InTakenIndexRel(-2,3);
									if (rightIndex < directionFieldIndex)
									{
										StraightMidAcross3EndArea2_circle2 = true;
									}
								}
							}
							else
							{
								directionFieldIndex = InBorderIndexRel(-2,4);
								int farSideIndex = InBorderIndexRel(-2,3);
								if (farSideIndex > directionFieldIndex)
								{
									StraightMidAcross3EndArea2_circle2 = true;
								}
							}
							
							ResetExamAreas();
							if (StraightMidAcross3EndArea2_circle1 && StraightMidAcross3EndArea2_circle2 && CountAreaRel(0,1,0,3,new List<int[]> {new int[] {0,2}},i==0?true:!true,0))
							{
								StraightMidAcross3EndArea2 = true;
								activeRules.Add("Straight Mid Across 3 End Area 2");
								activeRulesForbiddenFields.Add(new List<int[]> {new int[] { x + sx, y + sy }, new int[] { x + lx, y + ly }});
								activeRuleSizes.Add(new int[] {4,6});
								AddExamAreas();
								forbidden.Add(new int[] { x + sx, y + sy });
								forbidden.Add(new int[] { x + lx, y + ly });
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
			T("Future2x2StartEnd: " + Future2x2StartEnd + "\n" + "Future2x3StartEnd: " + Future2x3StartEnd + "\n" + "Future3x3StartEnd: " + Future3x3StartEnd + "\n" + "FutureL: " + FutureL + "\n" + "DoubleAreaCShape: " + DoubleAreaCShape + "\n" + "DoubleAreaStair2: " + DoubleAreaStair2 + "\n" + "DoubleAreaStairArea: " + DoubleAreaStairArea + "\n" + "DoubleAreaStair: " + DoubleAreaStair + "\n" + "DoubleCShapeStartC: " + DoubleCShapeStartC + "\n" + "Square4x2Area: " + Square4x2Area + "\n" + "Square4x2CShape: " + Square4x2CShape + "\n" + "StraightAcross3EndArea: " + StraightAcross3EndArea + "\n" + "StraightAcrossEndArea: " + StraightAcrossEndArea + "\n" + "StraightAcrossEndC: " + StraightAcrossEndC + "\n" + "StraightMidAcross3EndArea: " + StraightMidAcross3EndArea + "\n" + "StraightMidAcross3EndC: " + StraightMidAcross3EndC + "\n" + "TripleAreaExitDown: " + TripleAreaExitDown + "\n" + "TripleAreaStair: " + TripleAreaStair + "\n" + "TripleArea: " + TripleArea + "\n" + "StraightMidAcross3EndArea2: " + StraightMidAcross3EndArea2);
			window.ShowActiveRules(activeRules,activeRulesForbiddenFields,startForbiddenFields,activeRuleSizes);
		}
	}
}