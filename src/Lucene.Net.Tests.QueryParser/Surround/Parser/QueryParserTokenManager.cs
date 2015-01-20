/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.IO;
using Org.Apache.Lucene.Queryparser.Surround.Parser;
using Sharpen;

namespace Org.Apache.Lucene.Queryparser.Surround.Parser
{
	/// <summary>Token Manager.</summary>
	/// <remarks>Token Manager.</remarks>
	public class QueryParserTokenManager : QueryParserConstants
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

		private int JjStopStringLiteralDfa_1(int pos, long active0)
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

		private int JjStartNfa_1(int pos, long active0)
		{
			return JjMoveNfa_1(JjStopStringLiteralDfa_1(pos, active0), pos + 1);
		}

		private int JjStopAtPos(int pos, int kind)
		{
			jjmatchedKind = kind;
			jjmatchedPos = pos;
			return pos + 1;
		}

		private int JjMoveStringLiteralDfa0_1()
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

				case 44:
				{
					return JjStopAtPos(0, 15);
				}

				case 58:
				{
					return JjStopAtPos(0, 16);
				}

				case 94:
				{
					return JjStopAtPos(0, 17);
				}

				default:
				{
					return JjMoveNfa_1(0, 0);
					break;
				}
			}
		}

		internal static readonly long[] jjbitVec0 = new long[] { unchecked((long)(0xfffffffffffffffeL
			)), unchecked((long)(0xffffffffffffffffL)), unchecked((long)(0xffffffffffffffffL
			)), unchecked((long)(0xffffffffffffffffL)) };

		internal static readonly long[] jjbitVec2 = new long[] { unchecked((long)(0x0L)), 
			unchecked((long)(0x0L)), unchecked((long)(0xffffffffffffffffL)), unchecked((long
			)(0xffffffffffffffffL)) };

		private int JjMoveNfa_1(int startState, int curPos)
		{
			int startsAt = 0;
			jjnewStateCnt = 38;
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
								if ((unchecked((long)(0x7bffe8faffffd9ffL)) & l) != 0L)
								{
									if (kind > 22)
									{
										kind = 22;
									}
									JjCheckNAddStates(0, 4);
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
										if (curChar == 34)
										{
											JjCheckNAddStates(5, 7);
										}
									}
								}
								if ((unchecked((long)(0x3fc000000000000L)) & l) != 0L)
								{
									JjCheckNAddStates(8, 11);
								}
								else
								{
									if (curChar == 49)
									{
										JjCheckNAddTwoStates(20, 21);
									}
								}
								break;
							}

							case 19:
							{
								if ((unchecked((long)(0x3fc000000000000L)) & l) != 0L)
								{
									JjCheckNAddStates(8, 11);
								}
								break;
							}

							case 20:
							{
								if ((unchecked((long)(0x3ff000000000000L)) & l) != 0L)
								{
									JjCheckNAdd(17);
								}
								break;
							}

							case 21:
							{
								if ((unchecked((long)(0x3ff000000000000L)) & l) != 0L)
								{
									JjCheckNAdd(18);
								}
								break;
							}

							case 22:
							{
								if (curChar == 49)
								{
									JjCheckNAddTwoStates(20, 21);
								}
								break;
							}

							case 23:
							{
								if (curChar == 34)
								{
									JjCheckNAddStates(5, 7);
								}
								break;
							}

							case 24:
							{
								if ((unchecked((long)(0xfffffffbffffffffL)) & l) != 0L)
								{
									JjCheckNAddTwoStates(24, 25);
								}
								break;
							}

							case 25:
							{
								if (curChar == 34)
								{
									jjstateSet[jjnewStateCnt++] = 26;
								}
								break;
							}

							case 26:
							{
								if (curChar == 42 && kind > 18)
								{
									kind = 18;
								}
								break;
							}

							case 27:
							{
								if ((unchecked((long)(0xfffffffbffffffffL)) & l) != 0L)
								{
									JjCheckNAddStates(12, 14);
								}
								break;
							}

							case 29:
							{
								if (curChar == 34)
								{
									JjCheckNAddStates(12, 14);
								}
								break;
							}

							case 30:
							{
								if (curChar == 34 && kind > 19)
								{
									kind = 19;
								}
								break;
							}

							case 31:
							{
								if ((unchecked((long)(0x7bffe8faffffd9ffL)) & l) == 0L)
								{
									break;
								}
								if (kind > 22)
								{
									kind = 22;
								}
								JjCheckNAddStates(0, 4);
								break;
							}

							case 32:
							{
								if ((unchecked((long)(0x7bffe8faffffd9ffL)) & l) != 0L)
								{
									JjCheckNAddTwoStates(32, 33);
								}
								break;
							}

							case 33:
							{
								if (curChar == 42 && kind > 20)
								{
									kind = 20;
								}
								break;
							}

							case 34:
							{
								if ((unchecked((long)(0x7bffe8faffffd9ffL)) & l) != 0L)
								{
									JjCheckNAddTwoStates(34, 35);
								}
								break;
							}

							case 35:
							{
								if ((unchecked((long)(0x8000040000000000L)) & l) == 0L)
								{
									break;
								}
								if (kind > 21)
								{
									kind = 21;
								}
								JjCheckNAddTwoStates(35, 36);
								break;
							}

							case 36:
							{
								if ((unchecked((long)(0xfbffecfaffffd9ffL)) & l) == 0L)
								{
									break;
								}
								if (kind > 21)
								{
									kind = 21;
								}
								JjCheckNAdd(36);
								break;
							}

							case 37:
							{
								if ((unchecked((long)(0x7bffe8faffffd9ffL)) & l) == 0L)
								{
									break;
								}
								if (kind > 22)
								{
									kind = 22;
								}
								JjCheckNAdd(37);
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
									if ((unchecked((long)(0xffffffffbfffffffL)) & l) != 0L)
									{
										if (kind > 22)
										{
											kind = 22;
										}
										JjCheckNAddStates(0, 4);
									}
									if ((unchecked((long)(0x400000004000L)) & l) != 0L)
									{
										if (kind > 12)
										{
											kind = 12;
										}
									}
									else
									{
										if ((unchecked((long)(0x80000000800000L)) & l) != 0L)
										{
											if (kind > 11)
											{
												kind = 11;
											}
										}
										else
										{
											if (curChar == 97)
											{
												jjstateSet[jjnewStateCnt++] = 9;
											}
											else
											{
												if (curChar == 65)
												{
													jjstateSet[jjnewStateCnt++] = 6;
												}
												else
												{
													if (curChar == 111)
													{
														jjstateSet[jjnewStateCnt++] = 3;
													}
													else
													{
														if (curChar == 79)
														{
															jjstateSet[jjnewStateCnt++] = 1;
														}
													}
												}
											}
										}
									}
									if (curChar == 110)
									{
										jjstateSet[jjnewStateCnt++] = 15;
									}
									else
									{
										if (curChar == 78)
										{
											jjstateSet[jjnewStateCnt++] = 12;
										}
									}
									break;
								}

								case 1:
								{
									if (curChar == 82 && kind > 8)
									{
										kind = 8;
									}
									break;
								}

								case 2:
								{
									if (curChar == 79)
									{
										jjstateSet[jjnewStateCnt++] = 1;
									}
									break;
								}

								case 3:
								{
									if (curChar == 114 && kind > 8)
									{
										kind = 8;
									}
									break;
								}

								case 4:
								{
									if (curChar == 111)
									{
										jjstateSet[jjnewStateCnt++] = 3;
									}
									break;
								}

								case 5:
								{
									if (curChar == 68 && kind > 9)
									{
										kind = 9;
									}
									break;
								}

								case 6:
								{
									if (curChar == 78)
									{
										jjstateSet[jjnewStateCnt++] = 5;
									}
									break;
								}

								case 7:
								{
									if (curChar == 65)
									{
										jjstateSet[jjnewStateCnt++] = 6;
									}
									break;
								}

								case 8:
								{
									if (curChar == 100 && kind > 9)
									{
										kind = 9;
									}
									break;
								}

								case 9:
								{
									if (curChar == 110)
									{
										jjstateSet[jjnewStateCnt++] = 8;
									}
									break;
								}

								case 10:
								{
									if (curChar == 97)
									{
										jjstateSet[jjnewStateCnt++] = 9;
									}
									break;
								}

								case 11:
								{
									if (curChar == 84 && kind > 10)
									{
										kind = 10;
									}
									break;
								}

								case 12:
								{
									if (curChar == 79)
									{
										jjstateSet[jjnewStateCnt++] = 11;
									}
									break;
								}

								case 13:
								{
									if (curChar == 78)
									{
										jjstateSet[jjnewStateCnt++] = 12;
									}
									break;
								}

								case 14:
								{
									if (curChar == 116 && kind > 10)
									{
										kind = 10;
									}
									break;
								}

								case 15:
								{
									if (curChar == 111)
									{
										jjstateSet[jjnewStateCnt++] = 14;
									}
									break;
								}

								case 16:
								{
									if (curChar == 110)
									{
										jjstateSet[jjnewStateCnt++] = 15;
									}
									break;
								}

								case 17:
								{
									if ((unchecked((long)(0x80000000800000L)) & l) != 0L && kind > 11)
									{
										kind = 11;
									}
									break;
								}

								case 18:
								{
									if ((unchecked((long)(0x400000004000L)) & l) != 0L && kind > 12)
									{
										kind = 12;
									}
									break;
								}

								case 24:
								{
									JjAddStates(15, 16);
									break;
								}

								case 27:
								{
									if ((unchecked((long)(0xffffffffefffffffL)) & l) != 0L)
									{
										JjCheckNAddStates(12, 14);
									}
									break;
								}

								case 28:
								{
									if (curChar == 92)
									{
										jjstateSet[jjnewStateCnt++] = 29;
									}
									break;
								}

								case 29:
								{
									if (curChar == 92)
									{
										JjCheckNAddStates(12, 14);
									}
									break;
								}

								case 31:
								{
									if ((unchecked((long)(0xffffffffbfffffffL)) & l) == 0L)
									{
										break;
									}
									if (kind > 22)
									{
										kind = 22;
									}
									JjCheckNAddStates(0, 4);
									break;
								}

								case 32:
								{
									if ((unchecked((long)(0xffffffffbfffffffL)) & l) != 0L)
									{
										JjCheckNAddTwoStates(32, 33);
									}
									break;
								}

								case 34:
								{
									if ((unchecked((long)(0xffffffffbfffffffL)) & l) != 0L)
									{
										JjCheckNAddTwoStates(34, 35);
									}
									break;
								}

								case 36:
								{
									if ((unchecked((long)(0xffffffffbfffffffL)) & l) == 0L)
									{
										break;
									}
									if (kind > 21)
									{
										kind = 21;
									}
									jjstateSet[jjnewStateCnt++] = 36;
									break;
								}

								case 37:
								{
									if ((unchecked((long)(0xffffffffbfffffffL)) & l) == 0L)
									{
										break;
									}
									if (kind > 22)
									{
										kind = 22;
									}
									JjCheckNAdd(37);
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
									if (!JjCanMove_0(hiByte, i1, i2, l1, l2))
									{
										break;
									}
									if (kind > 22)
									{
										kind = 22;
									}
									JjCheckNAddStates(0, 4);
									break;
								}

								case 24:
								{
									if (JjCanMove_0(hiByte, i1, i2, l1, l2))
									{
										JjAddStates(15, 16);
									}
									break;
								}

								case 27:
								{
									if (JjCanMove_0(hiByte, i1, i2, l1, l2))
									{
										JjAddStates(12, 14);
									}
									break;
								}

								case 32:
								{
									if (JjCanMove_0(hiByte, i1, i2, l1, l2))
									{
										JjCheckNAddTwoStates(32, 33);
									}
									break;
								}

								case 34:
								{
									if (JjCanMove_0(hiByte, i1, i2, l1, l2))
									{
										JjCheckNAddTwoStates(34, 35);
									}
									break;
								}

								case 36:
								{
									if (!JjCanMove_0(hiByte, i1, i2, l1, l2))
									{
										break;
									}
									if (kind > 21)
									{
										kind = 21;
									}
									jjstateSet[jjnewStateCnt++] = 36;
									break;
								}

								case 37:
								{
									if (!JjCanMove_0(hiByte, i1, i2, l1, l2))
									{
										break;
									}
									if (kind > 22)
									{
										kind = 22;
									}
									JjCheckNAdd(37);
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
				if ((i = jjnewStateCnt) == (startsAt = 38 - (jjnewStateCnt = startsAt)))
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
								if (kind > 23)
								{
									kind = 23;
								}
								JjAddStates(17, 18);
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
								if (kind > 23)
								{
									kind = 23;
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

		internal static readonly int[] jjnextStates = new int[] { 32, 33, 34, 35, 37, 24, 
			27, 28, 20, 17, 21, 18, 27, 28, 30, 24, 25, 0, 1 };

		private static bool JjCanMove_0(int hiByte, int i1, int i2, long l1, long l2)
		{
			switch (hiByte)
			{
				case 0:
				{
					return ((jjbitVec2[i2] & l2) != 0L);
				}

				default:
				{
					if ((jjbitVec0[i1] & l1) != 0L)
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
			null, null, null, null, null, null, null, null, null, null, null, null, "\x32", 
			"\x33", "\x36", "\x48", "\x88", null, null, null, null, null, null };

		/// <summary>Lexer state names.</summary>
		/// <remarks>Lexer state names.</remarks>
		public static readonly string[] lexStateNames = new string[] { "Boost", "DEFAULT"
			 };

		/// <summary>Lex State array.</summary>
		/// <remarks>Lex State array.</remarks>
		public static readonly int[] jjnewLexState = new int[] { -1, -1, -1, -1, -1, -1, 
			-1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 0, -1, -1, -1, -1, -1, 1 };

		internal static readonly long[] jjtoToken = new long[] { unchecked((long)(0xffff01L
			)) };

		internal static readonly long[] jjtoSkip = new long[] { unchecked((long)(0x80L)) };

		protected internal CharStream input_stream;

		private readonly int[] jjrounds = new int[38];

		private readonly int[] jjstateSet = new int[76];

		protected internal char curChar;

		/// <summary>Constructor.</summary>
		/// <remarks>Constructor.</remarks>
		public QueryParserTokenManager(CharStream stream)
		{
			input_stream = stream;
		}

		/// <summary>Constructor.</summary>
		/// <remarks>Constructor.</remarks>
		public QueryParserTokenManager(CharStream stream, int lexState) : this(stream)
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
			for (i = 38; i-- > 0; )
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
			if (lexState >= 2 || lexState < 0)
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

		internal int curLexState = 1;

		internal int defaultLexState = 1;

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
