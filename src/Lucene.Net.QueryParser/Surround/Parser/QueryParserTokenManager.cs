using Lucene.Net.Support.IO;
using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace Lucene.Net.QueryParsers.Surround.Parser
{
    /*
     * Licensed to the Apache Software Foundation (ASF) under one or more
     * contributor license agreements.  See the NOTICE file distributed with
     * this work for additional information regarding copyright ownership.
     * The ASF licenses this file to You under the Apache License, Version 2.0
     * (the "License"); you may not use this file except in compliance with
     * the License.  You may obtain a copy of the License at
     *
     *     http://www.apache.org/licenses/LICENSE-2.0
     *
     * Unless required by applicable law or agreed to in writing, software
     * distributed under the License is distributed on an "AS IS" BASIS,
     * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
     * See the License for the specific language governing permissions and
     * limitations under the License.
     */

    /// <summary>
    /// Token Manager.
    /// </summary>
    [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "This class is based on generated code")]
    [SuppressMessage("Style", "IDE0059:Unnecessary assignment of a value", Justification = "This class is based on generated code")]
    [SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "This class is based on generated code")]
    [SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "This class is based on generated code")]
    public class QueryParserTokenManager //: QueryParserConstants
    {
        /// <summary>Debug output. </summary>
#pragma warning disable IDE0052 // Remove unread private members
        private TextWriter debugStream; // LUCENENET specific - made private, since we already have a setter
#pragma warning restore IDE0052 // Remove unread private members
        /// <summary>Set debug output. </summary>
        public virtual void SetDebugStream(TextWriter ds)
        {
            debugStream = new SafeTextWriterWrapper(ds);
        }
        private int JjStopStringLiteralDfa_1(int pos, long active0)
        {
            switch (pos)
            {
                default:
                    return -1;
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

        private int jjMoveStringLiteralDfa0_1()
        {
            switch (m_curChar)
            {
                case (char)40:
                    return JjStopAtPos(0, 13);
                case (char)41:
                    return JjStopAtPos(0, 14);
                case (char)44:
                    return JjStopAtPos(0, 15);
                case (char)58:
                    return JjStopAtPos(0, 16);
                case (char)94:
                    return JjStopAtPos(0, 17);
                default:
                    return JjMoveNfa_1(0, 0);
            }
        }
        internal static readonly ulong[] jjbitVec0 = {
            0xfffffffffffffffeL, 0xffffffffffffffffL, 0xffffffffffffffffL, 0xffffffffffffffffL
        };
        internal static readonly ulong[] jjbitVec2 = {
            0x0L, 0x0L, 0xffffffffffffffffL, 0xffffffffffffffffL
        };
        private int JjMoveNfa_1(int startState, int curPos)
        {
            int startsAt = 0;
            jjnewStateCnt = 38;
            int i = 1;
            jjstateSet[0] = startState;
            int kind = 0x7fffffff;
            for (; ; )
            {
                if (++jjround == 0x7fffffff)
                    ReInitRounds();
                if (m_curChar < 64)
                {
                    ulong l = (ulong)(1L << (int)m_curChar);
                    do
                    {
                        switch (jjstateSet[--i])
                        {
                            case 0:
                                if ((0x7bffe8faffffd9ffL & l) != 0L)
                                {
                                    if (kind > 22)
                                        kind = 22;
                                    JjCheckNAddStates(0, 4);
                                }
                                else if ((0x100002600L & l) != 0L)
                                {
                                    if (kind > 7)
                                        kind = 7;
                                }
                                else if (m_curChar == 34)
                                    JjCheckNAddStates(5, 7);
                                if ((0x3fc000000000000L & l) != 0L)
                                    JjCheckNAddStates(8, 11);
                                else if (m_curChar == 49)
                                    JjCheckNAddTwoStates(20, 21);
                                break;
                            case 19:
                                if ((0x3fc000000000000L & l) != 0L)
                                    JjCheckNAddStates(8, 11);
                                break;
                            case 20:
                                if ((0x3ff000000000000L & l) != 0L)
                                    JjCheckNAdd(17);
                                break;
                            case 21:
                                if ((0x3ff000000000000L & l) != 0L)
                                    JjCheckNAdd(18);
                                break;
                            case 22:
                                if (m_curChar == 49)
                                    JjCheckNAddTwoStates(20, 21);
                                break;
                            case 23:
                                if (m_curChar == 34)
                                    JjCheckNAddStates(5, 7);
                                break;
                            case 24:
                                if ((0xfffffffbffffffffL & l) != (ulong)0L)
                                    JjCheckNAddTwoStates(24, 25);
                                break;
                            case 25:
                                if (m_curChar == 34)
                                    jjstateSet[jjnewStateCnt++] = 26;
                                break;
                            case 26:
                                if (m_curChar == 42 && kind > 18)
                                    kind = 18;
                                break;
                            case 27:
                                if ((0xfffffffbffffffffL & l) != (ulong)0L)
                                    JjCheckNAddStates(12, 14);
                                break;
                            case 29:
                                if (m_curChar == 34)
                                    JjCheckNAddStates(12, 14);
                                break;
                            case 30:
                                if (m_curChar == 34 && kind > 19)
                                    kind = 19;
                                break;
                            case 31:
                                if ((0x7bffe8faffffd9ffL & l) == 0L)
                                    break;
                                if (kind > 22)
                                    kind = 22;
                                JjCheckNAddStates(0, 4);
                                break;
                            case 32:
                                if ((0x7bffe8faffffd9ffL & l) != 0L)
                                    JjCheckNAddTwoStates(32, 33);
                                break;
                            case 33:
                                if (m_curChar == 42 && kind > 20)
                                    kind = 20;
                                break;
                            case 34:
                                if ((0x7bffe8faffffd9ffL & l) != 0L)
                                    JjCheckNAddTwoStates(34, 35);
                                break;
                            case 35:
                                if ((0x8000040000000000L & l) == (ulong)0L)
                                    break;
                                if (kind > 21)
                                    kind = 21;
                                JjCheckNAddTwoStates(35, 36);
                                break;
                            case 36:
                                if ((0xfbffecfaffffd9ffL & l) == (ulong)0L)
                                    break;
                                if (kind > 21)
                                    kind = 21;
                                JjCheckNAdd(36);
                                break;
                            case 37:
                                if ((0x7bffe8faffffd9ffL & l) == 0L)
                                    break;
                                if (kind > 22)
                                    kind = 22;
                                JjCheckNAdd(37);
                                break;
                            default: break;
                        }
                    } while (i != startsAt);
                }
                else if (m_curChar < 128)
                {
                    // NOTE: See the note in the Classic.QueryParserTokenManager.cs file.
                    // I am working under the assumption 63 is the correct value, since it
                    // made the tests pass there.
                    ulong l = (ulong)(1L << (m_curChar & 63));
                    //long l = 1L << (curChar & 077);
                    do
                    {
                        switch (jjstateSet[--i])
                        {
                            case 0:
                                if ((0xffffffffbfffffffL & l) != (ulong)0L)
                                {
                                    if (kind > 22)
                                        kind = 22;
                                    JjCheckNAddStates(0, 4);
                                }
                                if ((0x400000004000L & l) != 0L)
                                {
                                    if (kind > 12)
                                        kind = 12;
                                }
                                else if ((0x80000000800000L & l) != 0L)
                                {
                                    if (kind > 11)
                                        kind = 11;
                                }
                                else if (m_curChar == 97)
                                    jjstateSet[jjnewStateCnt++] = 9;
                                else if (m_curChar == 65)
                                    jjstateSet[jjnewStateCnt++] = 6;
                                else if (m_curChar == 111)
                                    jjstateSet[jjnewStateCnt++] = 3;
                                else if (m_curChar == 79)
                                    jjstateSet[jjnewStateCnt++] = 1;
                                if (m_curChar == 110)
                                    jjstateSet[jjnewStateCnt++] = 15;
                                else if (m_curChar == 78)
                                    jjstateSet[jjnewStateCnt++] = 12;
                                break;
                            case 1:
                                if (m_curChar == 82 && kind > 8)
                                    kind = 8;
                                break;
                            case 2:
                                if (m_curChar == 79)
                                    jjstateSet[jjnewStateCnt++] = 1;
                                break;
                            case 3:
                                if (m_curChar == 114 && kind > 8)
                                    kind = 8;
                                break;
                            case 4:
                                if (m_curChar == 111)
                                    jjstateSet[jjnewStateCnt++] = 3;
                                break;
                            case 5:
                                if (m_curChar == 68 && kind > 9)
                                    kind = 9;
                                break;
                            case 6:
                                if (m_curChar == 78)
                                    jjstateSet[jjnewStateCnt++] = 5;
                                break;
                            case 7:
                                if (m_curChar == 65)
                                    jjstateSet[jjnewStateCnt++] = 6;
                                break;
                            case 8:
                                if (m_curChar == 100 && kind > 9)
                                    kind = 9;
                                break;
                            case 9:
                                if (m_curChar == 110)
                                    jjstateSet[jjnewStateCnt++] = 8;
                                break;
                            case 10:
                                if (m_curChar == 97)
                                    jjstateSet[jjnewStateCnt++] = 9;
                                break;
                            case 11:
                                if (m_curChar == 84 && kind > 10)
                                    kind = 10;
                                break;
                            case 12:
                                if (m_curChar == 79)
                                    jjstateSet[jjnewStateCnt++] = 11;
                                break;
                            case 13:
                                if (m_curChar == 78)
                                    jjstateSet[jjnewStateCnt++] = 12;
                                break;
                            case 14:
                                if (m_curChar == 116 && kind > 10)
                                    kind = 10;
                                break;
                            case 15:
                                if (m_curChar == 111)
                                    jjstateSet[jjnewStateCnt++] = 14;
                                break;
                            case 16:
                                if (m_curChar == 110)
                                    jjstateSet[jjnewStateCnt++] = 15;
                                break;
                            case 17:
                                if ((0x80000000800000L & l) != 0L && kind > 11)
                                    kind = 11;
                                break;
                            case 18:
                                if ((0x400000004000L & l) != 0L && kind > 12)
                                    kind = 12;
                                break;
                            case 24:
                                JjAddStates(15, 16);
                                break;
                            case 27:
                                if ((0xffffffffefffffffL & l) != (ulong)0L)
                                    JjCheckNAddStates(12, 14);
                                break;
                            case 28:
                                if (m_curChar == 92)
                                    jjstateSet[jjnewStateCnt++] = 29;
                                break;
                            case 29:
                                if (m_curChar == 92)
                                    JjCheckNAddStates(12, 14);
                                break;
                            case 31:
                                if ((0xffffffffbfffffffL & l) == (ulong)0L)
                                    break;
                                if (kind > 22)
                                    kind = 22;
                                JjCheckNAddStates(0, 4);
                                break;
                            case 32:
                                if ((0xffffffffbfffffffL & l) != (ulong)0L)
                                    JjCheckNAddTwoStates(32, 33);
                                break;
                            case 34:
                                if ((0xffffffffbfffffffL & l) != (ulong)0L)
                                    JjCheckNAddTwoStates(34, 35);
                                break;
                            case 36:
                                if ((0xffffffffbfffffffL & l) == (ulong)0L)
                                    break;
                                if (kind > 21)
                                    kind = 21;
                                jjstateSet[jjnewStateCnt++] = 36;
                                break;
                            case 37:
                                if ((0xffffffffbfffffffL & l) == (ulong)0L)
                                    break;
                                if (kind > 22)
                                    kind = 22;
                                JjCheckNAdd(37);
                                break;
                            default: break;
                        }
                    } while (i != startsAt);
                }
                else
                {
                    int hiByte = (int)(m_curChar >> 8);
                    int i1 = hiByte >> 6;
                    //long l1 = 1L << (hiByte & 077);
                    ulong l1 = (ulong)(1L << (hiByte & 63));
                    int i2 = (m_curChar & 0xff) >> 6;
                    //long l2 = 1L << (curChar & 077);
                    ulong l2 = (ulong)(1L << (m_curChar & 63));
                    do
                    {
                        switch (jjstateSet[--i])
                        {
                            case 0:
                                if (!JjCanMove_0(hiByte, i1, i2, l1, l2))
                                    break;
                                if (kind > 22)
                                    kind = 22;
                                JjCheckNAddStates(0, 4);
                                break;
                            case 24:
                                if (JjCanMove_0(hiByte, i1, i2, l1, l2))
                                    JjAddStates(15, 16);
                                break;
                            case 27:
                                if (JjCanMove_0(hiByte, i1, i2, l1, l2))
                                    JjAddStates(12, 14);
                                break;
                            case 32:
                                if (JjCanMove_0(hiByte, i1, i2, l1, l2))
                                    JjCheckNAddTwoStates(32, 33);
                                break;
                            case 34:
                                if (JjCanMove_0(hiByte, i1, i2, l1, l2))
                                    JjCheckNAddTwoStates(34, 35);
                                break;
                            case 36:
                                if (!JjCanMove_0(hiByte, i1, i2, l1, l2))
                                    break;
                                if (kind > 21)
                                    kind = 21;
                                jjstateSet[jjnewStateCnt++] = 36;
                                break;
                            case 37:
                                if (!JjCanMove_0(hiByte, i1, i2, l1, l2))
                                    break;
                                if (kind > 22)
                                    kind = 22;
                                JjCheckNAdd(37);
                                break;
                            default: break;
                        }
                    } while (i != startsAt);
                }
                if (kind != 0x7fffffff)
                {
                    jjmatchedKind = kind;
                    jjmatchedPos = curPos;
                    kind = 0x7fffffff;
                }
                ++curPos;
                if ((i = jjnewStateCnt) == (startsAt = 38 - (jjnewStateCnt = startsAt)))
                    return curPos;
                try { m_curChar = m_input_stream.ReadChar(); }
                catch (Exception e) when (e.IsIOException()) { return curPos; }
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
            int kind = 0x7fffffff;
            for (; ; )
            {
                if (++jjround == 0x7fffffff)
                    ReInitRounds();
                if (m_curChar < 64)
                {
                    long l = 1L << m_curChar;
                    do
                    {
                        switch (jjstateSet[--i])
                        {
                            case 0:
                                if ((0x3ff000000000000L & l) == 0L)
                                    break;
                                if (kind > 23)
                                    kind = 23;
                                JjAddStates(17, 18);
                                break;
                            case 1:
                                if (m_curChar == 46)
                                    JjCheckNAdd(2);
                                break;
                            case 2:
                                if ((0x3ff000000000000L & l) == 0L)
                                    break;
                                if (kind > 23)
                                    kind = 23;
                                JjCheckNAdd(2);
                                break;
                            default: break;
                        }
                    } while (i != startsAt);
                }
                else if (m_curChar < 128)
                {
                    //long l = 1L << (curChar & 077);
                    ulong l = (ulong)(1L << (m_curChar & 63));
                    do
                    {
                        switch (jjstateSet[--i])
                        {
                            default: break;
                        }
                    } while (i != startsAt);
                }
                else
                {
                    int hiByte = (int)(m_curChar >> 8);
                    int i1 = hiByte >> 6;
                    //long l1 = 1L << (hiByte & 077);
                    ulong l1 = (ulong)(1L << (hiByte & 63));
                    int i2 = (m_curChar & 0xff) >> 6;
                    //long l2 = 1L << (curChar & 077);
                    ulong l2 = (ulong)(1L << (m_curChar & 63));
                    do
                    {
                        switch (jjstateSet[--i])
                        {
                            default: break;
                        }
                    } while (i != startsAt);
                }
                if (kind != 0x7fffffff)
                {
                    jjmatchedKind = kind;
                    jjmatchedPos = curPos;
                    kind = 0x7fffffff;
                }
                ++curPos;
                if ((i = jjnewStateCnt) == (startsAt = 3 - (jjnewStateCnt = startsAt)))
                    return curPos;
                try { m_curChar = m_input_stream.ReadChar(); }
                catch (Exception e) when (e.IsIOException()) { return curPos; }
            }
        }
        internal static readonly int[] jjnextStates = {
            32, 33, 34, 35, 37, 24, 27, 28, 20, 17, 21, 18, 27, 28, 30, 24,
            25, 0, 1,
        };
        private static bool JjCanMove_0(int hiByte, int i1, int i2, ulong l1, ulong l2)
        {
            switch (hiByte)
            {
                case 0:
                    return ((jjbitVec2[i2] & l2) != 0L);
                default:
                    if ((jjbitVec0[i1] & l1) != 0L)
                        return true;
                    return false;
            }
        }

        ////** Token literal values. */
        ////public static readonly string[] jjstrLiteralImages = {
        ////    "", null, null, null, null, null, null, null, null, null, null, null, null,
        ////    "\50", "\51", "\54", "\72", "\136", null, null, null, null, null, null
        ////};

        /// <summary>Token literal values.</summary>
        public static readonly string[] jjstrLiteralImages = {
            "", null, null, null, null, null, null, null, null, null, null, null, null,
            "\x0028" /*"\50"*/, "\x0029" /*"\51"*/, "\x002C" /*"\54"*/, "\x003A" /*"\72"*/, "\x005E" /*"\136"*/, null, null, null, null, null, null
        };

        /// <summary>Lexer state names.</summary>
        public static readonly string[] lexStateNames = {
           "Boost",
           "DEFAULT"
        };

        /// <summary>Lex State array.</summary>
        public static readonly int[] jjnewLexState = {
           -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 0, -1, -1, -1, -1, -1, 1,
        };
        internal static readonly long[] jjtoToken = {
           0xffff01L,
        };
        internal static readonly long[] jjtoSkip = {
           0x80L,
        };
        protected ICharStream m_input_stream;
        private readonly uint[] jjrounds = new uint[38];
        private readonly int[] jjstateSet = new int[76];
        protected char m_curChar;

        /// <summary>Constructor.</summary>
        public QueryParserTokenManager(ICharStream stream)
        {
            m_input_stream = stream;
        }

        /// <summary>Constructor.</summary>
        public QueryParserTokenManager(ICharStream stream, int lexState)
            : this(stream)
        {
            SwitchTo(lexState);
        }

        /// <summary>Reinitialize parser.</summary>
        public void ReInit(ICharStream stream)
        {
            jjmatchedPos = jjnewStateCnt = 0;
            curLexState = defaultLexState;
            m_input_stream = stream;
            ReInitRounds();
        }
        private void ReInitRounds()
        {
            int i;
            jjround = 0x80000001;
            for (i = 38; i-- > 0; )
                jjrounds[i] = 0x80000000;
        }

        /// <summary>Reinitialize parser.</summary>
        public void ReInit(ICharStream stream, int lexState)
        {
            ReInit(stream);
            SwitchTo(lexState);
        }

        /// <summary>Switch to specified lex state.</summary>
        public void SwitchTo(int lexState)
        {
            if (lexState >= 2 || lexState < 0)
                throw new TokenMgrError("Error: Ignoring invalid lexical state : " + lexState + ". State unchanged.", TokenMgrError.INVALID_LEXICAL_STATE);
            else
                curLexState = lexState;
        }

        protected Token JjFillToken()
        {
            Token t;
            string curTokenImage;
            int beginLine;
            int endLine;
            int beginColumn;
            int endColumn;
            string im = jjstrLiteralImages[jjmatchedKind];
            curTokenImage = im ?? m_input_stream.Image;
            beginLine = m_input_stream.BeginLine;
            beginColumn = m_input_stream.BeginColumn;
            endLine = m_input_stream.EndLine;
            endColumn = m_input_stream.EndColumn;
            t = Token.NewToken(jjmatchedKind, curTokenImage);

            t.BeginLine = beginLine;
            t.EndLine = endLine;
            t.BeginColumn = beginColumn;
            t.EndColumn = endColumn;

            return t;
        }

        internal int curLexState = 1;
        internal int defaultLexState = 1;
        internal int jjnewStateCnt;
        internal uint jjround;
        internal int jjmatchedPos;
        internal int jjmatchedKind;

        /// <summary>Get the next Token.</summary>
        [SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate")]
        public virtual Token GetNextToken()
        {
            Token matchedToken;
            int curPos = 0;

            for (; ; )
            {
                try
                {
                    m_curChar = m_input_stream.BeginToken();
                }
                catch (Exception e) when (e.IsIOException())
                {
                    jjmatchedKind = 0;
                    matchedToken = JjFillToken();
                    return matchedToken;
                }

                switch (curLexState)
                {
                    case 0:
                        jjmatchedKind = 0x7fffffff;
                        jjmatchedPos = 0;
                        curPos = JjMoveStringLiteralDfa0_0();
                        break;
                    case 1:
                        jjmatchedKind = 0x7fffffff;
                        jjmatchedPos = 0;
                        curPos = jjMoveStringLiteralDfa0_1();
                        break;
                }
                if (jjmatchedKind != 0x7fffffff)
                {
                    if (jjmatchedPos + 1 < curPos)
                        m_input_stream.BackUp(curPos - jjmatchedPos - 1);
                    if ((jjtoToken[jjmatchedKind >> 6] & (1L << (jjmatchedKind & 63 /*077*/))) != 0L)
                    {
                        matchedToken = JjFillToken();
                        if (jjnewLexState[jjmatchedKind] != -1)
                            curLexState = jjnewLexState[jjmatchedKind];
                        return matchedToken;
                    }
                    else
                    {
                        if (jjnewLexState[jjmatchedKind] != -1)
                            curLexState = jjnewLexState[jjmatchedKind];
                        goto EOFLoop;
                    }
                }
                int error_line = m_input_stream.EndLine;
                int error_column = m_input_stream.EndColumn;
                string error_after = null;
                bool EOFSeen = false;
                try { m_input_stream.ReadChar(); m_input_stream.BackUp(1); }
                catch (Exception e1) when (e1.IsIOException())
                {
                    EOFSeen = true;
                    error_after = curPos <= 1 ? "" : m_input_stream.Image;
                    if (m_curChar == '\n' || m_curChar == '\r')
                    {
                        error_line++;
                        error_column = 0;
                    }
                    else
                        error_column++;
                }
                if (!EOFSeen)
                {
                    m_input_stream.BackUp(1);
                    error_after = curPos <= 1 ? "" : m_input_stream.Image;
                }
                throw new TokenMgrError(EOFSeen, curLexState, error_line, error_column, error_after, m_curChar, TokenMgrError.LEXICAL_ERROR);
            EOFLoop: {/* LUCENENET: intentionally blank */}
            }
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
            } while (start++ != end);
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
            } while (start++ != end);
        }
    }
}
