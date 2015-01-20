/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.IO;
using Org.Apache.Lucene.Queryparser.Flexible.Standard.Parser;
using Sharpen;

namespace Org.Apache.Lucene.Queryparser.Flexible.Standard.Parser
{
	/// <summary>Token Manager.</summary>
	/// <remarks>Token Manager.</remarks>
	public class StandardSyntaxParserTokenManager : StandardSyntaxParserConstants
	{
		/// <summary>Debug output.</summary>
		/// <remarks>Debug output.</remarks>
		public TextWriter debugStream = System.Console.Out;

		/// <summary>Set debug output.</summary>
		/// <remarks>Set debug output.</remarks>
		public virtual void SetDebugStream(TextWriter ds)
		{
			debugStream = ds;
		}

		private int JjStopStringLiteralDfa_2(int pos, long active0)
		{
			switch (pos)
			{
				default:
				{
					return -1;
					break;
				}
			}
		}

		private int JjStartNfa_2(int pos, long active0)
		{
			return JjMoveNfa_2(JjStopStringLiteralDfa_2(pos, active0), pos + 1);
		}

		private int JjStopAtPos(int pos, int kind)
		{
			jjmatchedKind = kind;
			jjmatchedPos = pos;
			return pos + 1;
		}

		private int JjMoveStringLiteralDfa0_2()
		{
			switch (curChar)
			{
				case 40:
				{
					return JjStopAtPos(0, 13);
				}

				case 41:
				{
					return JjStopAtPos(0, 14);
				}

				case 43:
				{
					return JjStopAtPos(0, 11);
				}

				case 45:
				{
					return JjStopAtPos(0, 12);
				}

				case 58:
				{
					return JjStopAtPos(0, 15);
				}

				case 60:
				{
					jjmatchedKind = 17;
					return JjMoveStringLiteralDfa1_2(unchecked((long)(0x40000L)));
				}

				case 61:
				{
					return JjStopAtPos(0, 16);
				}

				case 62:
				{
					jjmatchedKind = 19;
					return JjMoveStringLiteralDfa1_2(unchecked((long)(0x100000L)));
				}

				case 91:
				{
					return JjStopAtPos(0, 26);
				}

				case 94:
				{
					return JjStopAtPos(0, 21);
				}

				case 123:
				{
					return JjStopAtPos(0, 27);
				}

				default:
				{
					return JjMoveNfa_2(0, 0);
					break;
				}
			}
		}

		private int JjMoveStringLiteralDfa1_2(long active0)
		{
			try
			{
				curChar = input_stream.ReadChar();
			}
			catch (IOException)
			{
				JjStopStringLiteralDfa_2(0, active0);
				return 1;
			}
			switch (curChar)
			{
				case 61:
				{
					if ((active0 & unchecked((long)(0x40000L))) != 0L)
					{
						return JjStopAtPos(1, 18);
					}
					else
					{
						if ((active0 & unchecked((long)(0x100000L))) != 0L)
						{
							return JjStopAtPos(1, 20);
						}
					}
					break;
				}

				default:
				{
					break;
					break;
				}
			}
			return JjStartNfa_2(0, active0);
		}

		internal static readonly long[] jjbitVec0 = new long[] { unchecked((long)(0x1L)), 
			unchecked((long)(0x0L)), unchecked((long)(0x0L)), unchecked((long)(0x0L)) };

		internal static readonly long[] jjbitVec1 = new long[] { unchecked((long)(0xfffffffffffffffeL
			)), unchecked((long)(0xffffffffffffffffL)), unchecked((long)(0xffffffffffffffffL
			)), unchecked((long)(0xffffffffffffffffL)) };

		internal static readonly long[] jjbitVec3 = new long[] { unchecked((long)(0x0L)), 
			unchecked((long)(0x0L)), unchecked((long)(0xffffffffffffffffL)), unchecked((long
			)(0xffffffffffffffffL)) };

		internal static readonly long[] jjbitVec4 = new long[] { unchecked((long)(0xfffefffffffffffeL
			)), unchecked((long)(0xffffffffffffffffL)), unchecked((long)(0xffffffffffffffffL
			)), unchecked((long)(0xffffffffffffffffL)) };

		private int JjMoveNfa_2(int startState, int curPos)
		{
			int startsAt = 0;
			jjnewStateCnt = 33;
			int i = 1;
			jjstateSet[0] = startState;
			int kind = unchecked((int)(0x7fffffff));
			for (; ; )
			{
				if (++jjround == unchecked((int)(0x7fffffff)))
				{
					ReInitRounds();
				}
				if (curChar < 64)
				{
					long l = 1L << curChar;
					do
					{
						switch (jjstateSet[--i])
						{
							case 0:
							{
								if ((unchecked((long)(0x8bff54f8ffffd9ffL)) & l) != 0L)
								{
									if (kind > 23)
									{
										kind = 23;
									}
									JjCheckNAddTwoStates(20, 21);
								}
								else
								{
									if ((unchecked((long)(0x100002600L)) & l) != 0L)
									{
										if (kind > 7)
										{
											kind = 7;
										}
									}
									else
									{
										if (curChar == 47)
										{
											JjCheckNAddStates(0, 2);
										}
										else
										{
											if (curChar == 34)
											{
												JjCheckNAddStates(3, 5);
											}
											else
											{
												if (curChar == 33)
												{
													if (kind > 10)
													{
														kind = 10;
													}
												}
											}
										}
									}
								}
								if (curChar == 38)
								{
									jjstateSet[jjnewStateCnt++] = 4;
								}
								break;
							}

							case 4:
							{
								if (curChar == 38 && kind > 8)
								{
									kind = 8;
								}
								break;
							}

							case 5:
							{
								if (curChar == 38)
								{
									jjstateSet[jjnewStateCnt++] = 4;
								}
								break;
							}

							case 13:
							{
								if (curChar == 33 && kind > 10)
								{
									kind = 10;
								}
								break;
							}

							case 14:
							{
								if (curChar == 34)
								{
									JjCheckNAddStates(3, 5);
								}
								break;
							}

							case 15:
							{
								if ((unchecked((long)(0xfffffffbffffffffL)) & l) != 0L)
								{
									JjCheckNAddStates(3, 5);
								}
								break;
							}

							case 17:
							{
								JjCheckNAddStates(3, 5);
								break;
							}

							case 18:
							{
								if (curChar == 34 && kind > 22)
								{
									kind = 22;
								}
								break;
							}

							case 19:
							{
								if ((unchecked((long)(0x8bff54f8ffffd9ffL)) & l) == 0L)
								{
									break;
								}
								if (kind > 23)
								{
									kind = 23;
								}
								JjCheckNAddTwoStates(20, 21);
								break;
							}

							case 20:
							{
								if ((unchecked((long)(0x8bff7cf8ffffd9ffL)) & l) == 0L)
								{
									break;
								}
								if (kind > 23)
								{
									kind = 23;
								}
								JjCheckNAddTwoStates(20, 21);
								break;
							}

							case 22:
							{
								if (kind > 23)
								{
									kind = 23;
								}
								JjCheckNAddTwoStates(20, 21);
								break;
							}

							case 25:
							{
								if ((unchecked((long)(0x3ff000000000000L)) & l) == 0L)
								{
									break;
								}
								if (kind > 24)
								{
									kind = 24;
								}
								JjAddStates(6, 7);
								break;
							}

							case 26:
							{
								if (curChar == 46)
								{
									JjCheckNAdd(27);
								}
								break;
							}

							case 27:
							{
								if ((unchecked((long)(0x3ff000000000000L)) & l) == 0L)
								{
									break;
								}
								if (kind > 24)
								{
									kind = 24;
								}
								JjCheckNAdd(27);
								break;
							}

							case 28:
							case 30:
							{
								if (curChar == 47)
								{
									JjCheckNAddStates(0, 2);
								}
								break;
							}

							case 29:
							{
								if ((unchecked((long)(0xffff7fffffffffffL)) & l) != 0L)
								{
									JjCheckNAddStates(0, 2);
								}
								break;
							}

							case 32:
							{
								if (curChar == 47 && kind > 25)
								{
									kind = 25;
								}
								break;
							}

							default:
							{
								break;
								break;
							}
						}
					}
					while (i != startsAt);
				}
				else
				{
					if (curChar < 128)
					{
						long l = 1L << (curChar & 0x3f);
						do
						{
							switch (jjstateSet[--i])
							{
								case 0:
								{
									if ((unchecked((long)(0x97ffffff87ffffffL)) & l) != 0L)
									{
										if (kind > 23)
										{
											kind = 23;
										}
										JjCheckNAddTwoStates(20, 21);
									}
									else
									{
										if (curChar == 126)
										{
											if (kind > 24)
											{
												kind = 24;
											}
											jjstateSet[jjnewStateCnt++] = 25;
										}
										else
										{
											if (curChar == 92)
											{
												JjCheckNAdd(22);
											}
										}
									}
									if (curChar == 78)
									{
										jjstateSet[jjnewStateCnt++] = 11;
									}
									else
									{
										if (curChar == 124)
										{
											jjstateSet[jjnewStateCnt++] = 8;
										}
										else
										{
											if (curChar == 79)
											{
												jjstateSet[jjnewStateCnt++] = 6;
											}
											else
											{
												if (curChar == 65)
												{
													jjstateSet[jjnewStateCnt++] = 2;
												}
											}
										}
									}
									break;
								}

								case 1:
								{
									if (curChar == 68 && kind > 8)
									{
										kind = 8;
									}
									break;
								}

								case 2:
								{
									if (curChar == 78)
									{
										jjstateSet[jjnewStateCnt++] = 1;
									}
									break;
								}

								case 3:
								{
									if (curChar == 65)
									{
										jjstateSet[jjnewStateCnt++] = 2;
									}
									break;
								}

								case 6:
								{
									if (curChar == 82 && kind > 9)
									{
										kind = 9;
									}
									break;
								}

								case 7:
								{
									if (curChar == 79)
									{
										jjstateSet[jjnewStateCnt++] = 6;
									}
									break;
								}

								case 8:
								{
									if (curChar == 124 && kind > 9)
									{
										kind = 9;
									}
									break;
								}

								case 9:
								{
									if (curChar == 124)
									{
										jjstateSet[jjnewStateCnt++] = 8;
									}
									break;
								}

								case 10:
								{
									if (curChar == 84 && kind > 10)
									{
										kind = 10;
									}
									break;
								}

								case 11:
								{
									if (curChar == 79)
									{
										jjstateSet[jjnewStateCnt++] = 10;
									}
									break;
								}

								case 12:
								{
									if (curChar == 78)
									{
										jjstateSet[jjnewStateCnt++] = 11;
									}
									break;
								}

								case 15:
								{
									if ((unchecked((long)(0xffffffffefffffffL)) & l) != 0L)
									{
										JjCheckNAddStates(3, 5);
									}
									break;
								}

								case 16:
								{
									if (curChar == 92)
									{
										jjstateSet[jjnewStateCnt++] = 17;
									}
									break;
								}

								case 17:
								{
									JjCheckNAddStates(3, 5);
									break;
								}

								case 19:
								case 20:
								{
									if ((unchecked((long)(0x97ffffff87ffffffL)) & l) == 0L)
									{
										break;
									}
									if (kind > 23)
									{
										kind = 23;
									}
									JjCheckNAddTwoStates(20, 21);
									break;
								}

								case 21:
								{
									if (curChar == 92)
									{
										JjCheckNAddTwoStates(22, 22);
									}
									break;
								}

								case 22:
								{
									if (kind > 23)
									{
										kind = 23;
									}
									JjCheckNAddTwoStates(20, 21);
									break;
								}

								case 23:
								{
									if (curChar == 92)
									{
										JjCheckNAdd(22);
									}
									break;
								}

								case 24:
								{
									if (curChar != 126)
									{
										break;
									}
									if (kind > 24)
									{
										kind = 24;
									}
									jjstateSet[jjnewStateCnt++] = 25;
									break;
								}

								case 29:
								{
									JjAddStates(0, 2);
									break;
								}

								case 31:
								{
									if (curChar == 92)
									{
										jjstateSet[jjnewStateCnt++] = 30;
									}
									break;
								}

								default:
								{
									break;
									break;
								}
							}
						}
						while (i != startsAt);
					}
					else
					{
						int hiByte = (int)(curChar >> 8);
						int i1 = hiByte >> 6;
						long l1 = 1L << (hiByte & 0x3f);
						int i2 = (curChar & unchecked((int)(0xff))) >> 6;
						long l2 = 1L << (curChar & 0x3f);
						do
						{
							switch (jjstateSet[--i])
							{
								case 0:
								{
									if (JjCanMove_0(hiByte, i1, i2, l1, l2))
									{
										if (kind > 7)
										{
											kind = 7;
										}
									}
									if (JjCanMove_2(hiByte, i1, i2, l1, l2))
									{
										if (kind > 23)
										{
											kind = 23;
										}
										JjCheckNAddTwoStates(20, 21);
									}
									break;
								}

								case 15:
								case 17:
								{
									if (JjCanMove_1(hiByte, i1, i2, l1, l2))
									{
										JjCheckNAddStates(3, 5);
									}
									break;
								}

								case 19:
								case 20:
								{
									if (!JjCanMove_2(hiByte, i1, i2, l1, l2))
									{
										break;
									}
									if (kind > 23)
									{
										kind = 23;
									}
									JjCheckNAddTwoStates(20, 21);
									break;
								}

								case 22:
								{
									if (!JjCanMove_1(hiByte, i1, i2, l1, l2))
									{
										break;
									}
									if (kind > 23)
									{
										kind = 23;
									}
									JjCheckNAddTwoStates(20, 21);
									break;
								}

								case 29:
								{
									if (JjCanMove_1(hiByte, i1, i2, l1, l2))
									{
										JjAddStates(0, 2);
									}
									break;
								}

								default:
								{
									break;
									break;
								}
							}
						}
						while (i != startsAt);
					}
				}
				if (kind != unchecked((int)(0x7fffffff)))
				{
					jjmatchedKind = kind;
					jjmatchedPos = curPos;
					kind = unchecked((int)(0x7fffffff));
				}
				++curPos;
				if ((i = jjnewStateCnt) == (startsAt = 33 - (jjnewStateCnt = startsAt)))
				{
					return curPos;
				}
				try
				{
					curChar = input_stream.ReadChar();
				}
				catch (IOException)
				{
					return curPos;
				}
			}
		}

		private int JjMoveStringLiteralDfa0_0()
		{
			return JjMoveNfa_0(0, 0);
		}

		private int JjMoveNfa_0(int startState, int curPos)
		{
			int startsAt = 0;
			jjnewStateCnt = 3;
			int i = 1;
			jjstateSet[0] = startState;
			int kind = unchecked((int)(0x7fffffff));
			for (; ; )
			{
				if (++jjround == unchecked((int)(0x7fffffff)))
				{
					ReInitRounds();
				}
				if (curChar < 64)
				{
					long l = 1L << curChar;
					do
					{
						switch (jjstateSet[--i])
						{
							case 0:
							{
								if ((unchecked((long)(0x3ff000000000000L)) & l) == 0L)
								{
									break;
								}
								if (kind > 28)
								{
									kind = 28;
								}
								JjAddStates(8, 9);
								break;
							}

							case 1:
							{
								if (curChar == 46)
								{
									JjCheckNAdd(2);
								}
								break;
							}

							case 2:
							{
								if ((unchecked((long)(0x3ff000000000000L)) & l) == 0L)
								{
									break;
								}
								if (kind > 28)
								{
									kind = 28;
								}
								JjCheckNAdd(2);
								break;
							}

							default:
							{
								break;
								break;
							}
						}
					}
					while (i != startsAt);
				}
				else
				{
					if (curChar < 128)
					{
						long l = 1L << (curChar & 0x3f);
						do
						{
							switch (jjstateSet[--i])
							{
								default:
								{
									break;
									break;
								}
							}
						}
						while (i != startsAt);
					}
					else
					{
						int hiByte = (int)(curChar >> 8);
						int i1 = hiByte >> 6;
						long l1 = 1L << (hiByte & 0x3f);
						int i2 = (curChar & unchecked((int)(0xff))) >> 6;
						long l2 = 1L << (curChar & 0x3f);
						do
						{
							switch (jjstateSet[--i])
							{
								default:
								{
									break;
									break;
								}
							}
						}
						while (i != startsAt);
					}
				}
				if (kind != unchecked((int)(0x7fffffff)))
				{
					jjmatchedKind = kind;
					jjmatchedPos = curPos;
					kind = unchecked((int)(0x7fffffff));
				}
				++curPos;
				if ((i = jjnewStateCnt) == (startsAt = 3 - (jjnewStateCnt = startsAt)))
				{
					return curPos;
				}
				try
				{
					curChar = input_stream.ReadChar();
				}
				catch (IOException)
				{
					return curPos;
				}
			}
		}

		private int JjStopStringLiteralDfa_1(int pos, long active0)
		{
			switch (pos)
			{
				case 0:
				{
					if ((active0 & unchecked((long)(0x20000000L))) != 0L)
					{
						jjmatchedKind = 33;
						return 6;
					}
					return -1;
				}

				default:
				{
					return -1;
					break;
				}
			}
		}

		private int JjStartNfa_1(int pos, long active0)
		{
			return JjMoveNfa_1(JjStopStringLiteralDfa_1(pos, active0), pos + 1);
		}

		private int JjMoveStringLiteralDfa0_1()
		{
			switch (curChar)
			{
				case 84:
				{
					return JjMoveStringLiteralDfa1_1(unchecked((long)(0x20000000L)));
				}

				case 93:
				{
					return JjStopAtPos(0, 30);
				}

				case 125:
				{
					return JjStopAtPos(0, 31);
				}

				default:
				{
					return JjMoveNfa_1(0, 0);
					break;
				}
			}
		}

		private int JjMoveStringLiteralDfa1_1(long active0)
		{
			try
			{
				curChar = input_stream.ReadChar();
			}
			catch (IOException)
			{
				JjStopStringLiteralDfa_1(0, active0);
				return 1;
			}
			switch (curChar)
			{
				case 79:
				{
					if ((active0 & unchecked((long)(0x20000000L))) != 0L)
					{
						return JjStartNfaWithStates_1(1, 29, 6);
					}
					break;
				}

				default:
				{
					break;
					break;
				}
			}
			return JjStartNfa_1(0, active0);
		}

		private int JjStartNfaWithStates_1(int pos, int kind, int state)
		{
			jjmatchedKind = kind;
			jjmatchedPos = pos;
			try
			{
				curChar = input_stream.ReadChar();
			}
			catch (IOException)
			{
				return pos + 1;
			}
			return JjMoveNfa_1(state, pos + 1);
		}

		private int JjMoveNfa_1(int startState, int curPos)
		{
			int startsAt = 0;
			jjnewStateCnt = 7;
			int i = 1;
			jjstateSet[0] = startState;
			int kind = unchecked((int)(0x7fffffff));
			for (; ; )
			{
				if (++jjround == unchecked((int)(0x7fffffff)))
				{
					ReInitRounds();
				}
				if (curChar < 64)
				{
					long l = 1L << curChar;
					do
					{
						switch (jjstateSet[--i])
						{
							case 0:
							{
								if ((unchecked((long)(0xfffffffeffffffffL)) & l) != 0L)
								{
									if (kind > 33)
									{
										kind = 33;
									}
									JjCheckNAdd(6);
								}
								if ((unchecked((long)(0x100002600L)) & l) != 0L)
								{
									if (kind > 7)
									{
										kind = 7;
									}
								}
								else
								{
									if (curChar == 34)
									{
										JjCheckNAddTwoStates(2, 4);
									}
								}
								break;
							}

							case 1:
							{
								if (curChar == 34)
								{
									JjCheckNAddTwoStates(2, 4);
								}
								break;
							}

							case 2:
							{
								if ((unchecked((long)(0xfffffffbffffffffL)) & l) != 0L)
								{
									JjCheckNAddStates(10, 12);
								}
								break;
							}

							case 3:
							{
								if (curChar == 34)
								{
									JjCheckNAddStates(10, 12);
								}
								break;
							}

							case 5:
							{
								if (curChar == 34 && kind > 32)
								{
									kind = 32;
								}
								break;
							}

							case 6:
							{
								if ((unchecked((long)(0xfffffffeffffffffL)) & l) == 0L)
								{
									break;
								}
								if (kind > 33)
								{
									kind = 33;
								}
								JjCheckNAdd(6);
								break;
							}

							default:
							{
								break;
								break;
							}
						}
					}
					while (i != startsAt);
				}
				else
				{
					if (curChar < 128)
					{
						long l = 1L << (curChar & 0x3f);
						do
						{
							switch (jjstateSet[--i])
							{
								case 0:
								case 6:
								{
									if ((unchecked((long)(0xdfffffffdfffffffL)) & l) == 0L)
									{
										break;
									}
									if (kind > 33)
									{
										kind = 33;
									}
									JjCheckNAdd(6);
									break;
								}

								case 2:
								{
									JjAddStates(10, 12);
									break;
								}

								case 4:
								{
									if (curChar == 92)
									{
										jjstateSet[jjnewStateCnt++] = 3;
									}
									break;
								}

								default:
								{
									break;
									break;
								}
							}
						}
						while (i != startsAt);
					}
					else
					{
						int hiByte = (int)(curChar >> 8);
						int i1 = hiByte >> 6;
						long l1 = 1L << (hiByte & 0x3f);
						int i2 = (curChar & unchecked((int)(0xff))) >> 6;
						long l2 = 1L << (curChar & 0x3f);
						do
						{
							switch (jjstateSet[--i])
							{
								case 0:
								{
									if (JjCanMove_0(hiByte, i1, i2, l1, l2))
									{
										if (kind > 7)
										{
											kind = 7;
										}
									}
									if (JjCanMove_1(hiByte, i1, i2, l1, l2))
									{
										if (kind > 33)
										{
											kind = 33;
										}
										JjCheckNAdd(6);
									}
									break;
								}

								case 2:
								{
									if (JjCanMove_1(hiByte, i1, i2, l1, l2))
									{
										JjAddStates(10, 12);
									}
									break;
								}

								case 6:
								{
									if (!JjCanMove_1(hiByte, i1, i2, l1, l2))
									{
										break;
									}
									if (kind > 33)
									{
										kind = 33;
									}
									JjCheckNAdd(6);
									break;
								}

								default:
								{
									break;
									break;
								}
							}
						}
						while (i != startsAt);
					}
				}
				if (kind != unchecked((int)(0x7fffffff)))
				{
					jjmatchedKind = kind;
					jjmatchedPos = curPos;
					kind = unchecked((int)(0x7fffffff));
				}
				++curPos;
				if ((i = jjnewStateCnt) == (startsAt = 7 - (jjnewStateCnt = startsAt)))
				{
					return curPos;
				}
				try
				{
					curChar = input_stream.ReadChar();
				}
				catch (IOException)
				{
					return curPos;
				}
			}
		}

		internal static readonly int[] jjnextStates = new int[] { 29, 31, 32, 15, 16, 18, 
			25, 26, 0, 1, 2, 4, 5 };

		private static bool JjCanMove_0(int hiByte, int i1, int i2, long l1, long l2)
		{
			switch (hiByte)
			{
				case 48:
				{
					return ((jjbitVec0[i2] & l2) != 0L);
				}

				default:
				{
					return false;
					break;
				}
			}
		}

		private static bool JjCanMove_1(int hiByte, int i1, int i2, long l1, long l2)
		{
			switch (hiByte)
			{
				case 0:
				{
					return ((jjbitVec3[i2] & l2) != 0L);
				}

				default:
				{
					if ((jjbitVec1[i1] & l1) != 0L)
					{
						return true;
					}
					return false;
					break;
				}
			}
		}

		private static bool JjCanMove_2(int hiByte, int i1, int i2, long l1, long l2)
		{
			switch (hiByte)
			{
				case 0:
				{
					return ((jjbitVec3[i2] & l2) != 0L);
				}

				case 48:
				{
					return ((jjbitVec1[i2] & l2) != 0L);
				}

				default:
				{
					if ((jjbitVec4[i1] & l1) != 0L)
					{
						return true;
					}
					return false;
					break;
				}
			}
		}

		/// <summary>Token literal values.</summary>
		/// <remarks>Token literal values.</remarks>
		public static readonly string[] jjstrLiteralImages = new string[] { string.Empty, 
			null, null, null, null, null, null, null, null, null, null, "\x35", "\x37", "\x32"
			, "\x33", "\x48", "\x4b", "\x4a", "\x4a\x4b", "\x4c", "\x4c\x4b", "\x88", null, 
			null, null, null, "\x85", "\xad", null, "\x7c\x75", "\x87", "\xaf", null, null };

		/// <summary>Lexer state names.</summary>
		/// <remarks>Lexer state names.</remarks>
		public static readonly string[] lexStateNames = new string[] { "Boost", "Range", 
			"DEFAULT" };

		/// <summary>Lex State array.</summary>
		/// <remarks>Lex State array.</remarks>
		public static readonly int[] jjnewLexState = new int[] { -1, -1, -1, -1, -1, -1, 
			-1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 0, -1, -1, -1, -1, 1
			, 1, 2, -1, 2, 2, -1, -1 };

		internal static readonly long[] jjtoToken = new long[] { unchecked((long)(0x3ffffff01L
			)) };

		internal static readonly long[] jjtoSkip = new long[] { unchecked((long)(0x80L)) };

		protected internal CharStream input_stream;

		private readonly int[] jjrounds = new int[33];

		private readonly int[] jjstateSet = new int[66];

		protected internal char curChar;

		/// <summary>Constructor.</summary>
		/// <remarks>Constructor.</remarks>
		public StandardSyntaxParserTokenManager(CharStream stream)
		{
			input_stream = stream;
		}

		/// <summary>Constructor.</summary>
		/// <remarks>Constructor.</remarks>
		public StandardSyntaxParserTokenManager(CharStream stream, int lexState) : this(stream
			)
		{
			SwitchTo(lexState);
		}

		/// <summary>Reinitialise parser.</summary>
		/// <remarks>Reinitialise parser.</remarks>
		public virtual void ReInit(CharStream stream)
		{
			jjmatchedPos = jjnewStateCnt = 0;
			curLexState = defaultLexState;
			input_stream = stream;
			ReInitRounds();
		}

		private void ReInitRounds()
		{
			int i;
			jjround = unchecked((int)(0x80000001));
			for (i = 33; i-- > 0; )
			{
				jjrounds[i] = unchecked((int)(0x80000000));
			}
		}

		/// <summary>Reinitialise parser.</summary>
		/// <remarks>Reinitialise parser.</remarks>
		public virtual void ReInit(CharStream stream, int lexState)
		{
			ReInit(stream);
			SwitchTo(lexState);
		}

		/// <summary>Switch to specified lex state.</summary>
		/// <remarks>Switch to specified lex state.</remarks>
		public virtual void SwitchTo(int lexState)
		{
			if (lexState >= 3 || lexState < 0)
			{
				throw new TokenMgrError("Error: Ignoring invalid lexical state : " + lexState + ". State unchanged."
					, TokenMgrError.INVALID_LEXICAL_STATE);
			}
			else
			{
				curLexState = lexState;
			}
		}

		protected internal virtual Token JjFillToken()
		{
			Token t;
			string curTokenImage;
			int beginLine;
			int endLine;
			int beginColumn;
			int endColumn;
			string im = jjstrLiteralImages[jjmatchedKind];
			curTokenImage = (im == null) ? input_stream.GetImage() : im;
			beginLine = input_stream.GetBeginLine();
			beginColumn = input_stream.GetBeginColumn();
			endLine = input_stream.GetEndLine();
			endColumn = input_stream.GetEndColumn();
			t = Token.NewToken(jjmatchedKind, curTokenImage);
			t.beginLine = beginLine;
			t.endLine = endLine;
			t.beginColumn = beginColumn;
			t.endColumn = endColumn;
			return t;
		}

		internal int curLexState = 2;

		internal int defaultLexState = 2;

		internal int jjnewStateCnt;

		internal int jjround;

		internal int jjmatchedPos;

		internal int jjmatchedKind;

		/// <summary>Get the next Token.</summary>
		/// <remarks>Get the next Token.</remarks>
		public virtual Token GetNextToken()
		{
			Token matchedToken;
			int curPos = 0;
			for (; ; )
			{
				try
				{
					curChar = input_stream.BeginToken();
				}
				catch (IOException)
				{
					jjmatchedKind = 0;
					matchedToken = JjFillToken();
					return matchedToken;
				}
				switch (curLexState)
				{
					case 0:
					{
						jjmatchedKind = unchecked((int)(0x7fffffff));
						jjmatchedPos = 0;
						curPos = JjMoveStringLiteralDfa0_0();
						break;
					}

					case 1:
					{
						jjmatchedKind = unchecked((int)(0x7fffffff));
						jjmatchedPos = 0;
						curPos = JjMoveStringLiteralDfa0_1();
						break;
					}

					case 2:
					{
						jjmatchedKind = unchecked((int)(0x7fffffff));
						jjmatchedPos = 0;
						curPos = JjMoveStringLiteralDfa0_2();
						break;
					}
				}
				if (jjmatchedKind != unchecked((int)(0x7fffffff)))
				{
					if (jjmatchedPos + 1 < curPos)
					{
						input_stream.Backup(curPos - jjmatchedPos - 1);
					}
					if ((jjtoToken[jjmatchedKind >> 6] & (1L << (jjmatchedKind & 0x3f))) != 0L)
					{
						matchedToken = JjFillToken();
						if (jjnewLexState[jjmatchedKind] != -1)
						{
							curLexState = jjnewLexState[jjmatchedKind];
						}
						return matchedToken;
					}
					else
					{
						if (jjnewLexState[jjmatchedKind] != -1)
						{
							curLexState = jjnewLexState[jjmatchedKind];
						}
						goto EOFLoop_continue;
					}
				}
				int error_line = input_stream.GetEndLine();
				int error_column = input_stream.GetEndColumn();
				string error_after = null;
				bool EOFSeen = false;
				try
				{
					input_stream.ReadChar();
					input_stream.Backup(1);
				}
				catch (IOException)
				{
					EOFSeen = true;
					error_after = curPos <= 1 ? string.Empty : input_stream.GetImage();
					if (curChar == '\n' || curChar == '\r')
					{
						error_line++;
						error_column = 0;
					}
					else
					{
						error_column++;
					}
				}
				if (!EOFSeen)
				{
					input_stream.Backup(1);
					error_after = curPos <= 1 ? string.Empty : input_stream.GetImage();
				}
				throw new TokenMgrError(EOFSeen, curLexState, error_line, error_column, error_after
					, curChar, TokenMgrError.LEXICAL_ERROR);
EOFLoop_continue: ;
			}
EOFLoop_break: ;
		}

		private void JjCheckNAdd(int state)
		{
			if (jjrounds[state] != jjround)
			{
				jjstateSet[jjnewStateCnt++] = state;
				jjrounds[state] = jjround;
			}
		}

		private void JjAddStates(int start, int end)
		{
			do
			{
				jjstateSet[jjnewStateCnt++] = jjnextStates[start];
			}
			while (start++ != end);
		}

		private void JjCheckNAddTwoStates(int state1, int state2)
		{
			JjCheckNAdd(state1);
			JjCheckNAdd(state2);
		}

		private void JjCheckNAddStates(int start, int end)
		{
			do
			{
				JjCheckNAdd(jjnextStates[start]);
			}
			while (start++ != end);
		}
	}
}
