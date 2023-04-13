using Lucene.Net.Support.IO;
using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace Lucene.Net.QueryParsers.Classic
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

    /// <summary>Token Manager. </summary>
    [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "This class is based on generated code")]
    [SuppressMessage("Style", "IDE0059:Unnecessary assignment of a value", Justification = "This class is based on generated code")]
    [SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "This class is based on generated code")]
    public class QueryParserTokenManager //: QueryParserConstants
    {
        private void  InitBlock()
        {
            StreamWriter temp_writer;
            temp_writer = new StreamWriter(Console.OpenStandardOutput(), Console.Out.Encoding);
            temp_writer.AutoFlush = true;
            debugStream = temp_writer;
        }

        /// <summary>Debug output. </summary>
#pragma warning disable IDE0052 // Remove unread private members
        private TextWriter debugStream; // LUCENENET specific - made private, since we already have a setter
#pragma warning restore IDE0052 // Remove unread private members
        /// <summary>Set debug output. </summary>
        public virtual void SetDebugStream(TextWriter ds)
        {
            debugStream = new SafeTextWriterWrapper(ds);
        }
        private int JjStopStringLiteralDfa_2(int pos, long active0)
        {
            switch (pos)
            {

                default:
                    return - 1;

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
            switch (m_curChar)
            {

                case (char) (40):
                    return JjStopAtPos(0, 14);

                case (char) (41):
                    return JjStopAtPos(0, 15);

                case (char) (42):
                    return JjStartNfaWithStates_2(0, 17, 49);

                case (char) (43):
                    return JjStartNfaWithStates_2(0, 11, 15);

                case (char) (45):
                    return JjStartNfaWithStates_2(0, 12, 15);

                case (char) (58):
                    return JjStopAtPos(0, 16);

                case (char) (91):
                    return JjStopAtPos(0, 25);

                case (char) (94):
                    return JjStopAtPos(0, 18);

                case (char) (123):
                    return JjStopAtPos(0, 26);

                default:
                    return JjMoveNfa_2(0, 0);

            }
        }
        private int JjStartNfaWithStates_2(int pos, int kind, int state)
        {
            jjmatchedKind = kind;
            jjmatchedPos = pos;
            try
            {
                m_curChar = m_input_stream.ReadChar();
            }
            catch (Exception e) when (e.IsIOException())
            {
                return pos + 1;
            }
            return JjMoveNfa_2(state, pos + 1);
        }
        internal static readonly ulong[] jjbitVec0 = new ulong[]{0x1L, 0x0L, 0x0L, 0x0L};
        internal static readonly ulong[] jjbitVec1 = new ulong[]{0xfffffffffffffffeL, 0xffffffffffffffffL, 0xffffffffffffffffL, 0xffffffffffffffffL};
        internal static readonly ulong[] jjbitVec3 = new ulong[]{0x0L, 0x0L, 0xffffffffffffffffL, 0xffffffffffffffffL};
        internal static readonly ulong[] jjbitVec4 = new ulong[]{0xfffefffffffffffeL, 0xffffffffffffffffL, 0xffffffffffffffffL, 0xffffffffffffffffL};
        private int JjMoveNfa_2(int startState, int curPos)
        {
            int startsAt = 0;
            jjnewStateCnt = 49;
            int i = 1;
            jjstateSet[0] = startState;
            int kind = 0x7fffffff;
            for (; ; )
            {
                if (++jjround == 0x7fffffff)
                    ReInitRounds();
                if (m_curChar < 64)
                {
                    ulong l = (ulong) (1L << (int) m_curChar);
                    do
                    {
                        switch (jjstateSet[--i])
                        {

                            case 49:
                            case 33:
                                if ((0xfbff7cf8ffffd9ffL & l) == (ulong)0L)
                                    break;
                                if (kind > 23)
                                    kind = 23;
                                JjCheckNAddTwoStates(33, 34);
                                break;

                            case 0:
                                if ((0xfbff54f8ffffd9ffL & l) != (ulong)0L)
                                {
                                    if (kind > 23)
                                        kind = 23;
                                    JjCheckNAddTwoStates(33, 34);
                                }
                                else if ((0x100002600L & l) != 0L)
                                {
                                    if (kind > 7)
                                        kind = 7;
                                }
                                else if ((0x280200000000L & l) != 0L)
                                    jjstateSet[jjnewStateCnt++] = 15;
                                else if (m_curChar == 47)
                                    JjCheckNAddStates(0, 2);
                                else if (m_curChar == 34)
                                    JjCheckNAddStates(3, 5);
                                if ((0x7bff50f8ffffd9ffL & l) != 0L)
                                {
                                    if (kind > 20)
                                        kind = 20;
                                    JjCheckNAddStates(6, 10);
                                }
                                else if (m_curChar == 42)
                                {
                                    if (kind > 22)
                                        kind = 22;
                                }
                                else if (m_curChar == 33)
                                {
                                    if (kind > 10)
                                        kind = 10;
                                }
                                if (m_curChar == 38)
                                    jjstateSet[jjnewStateCnt++] = 4;
                                break;

                            case 4:
                                if (m_curChar == 38 && kind > 8)
                                    kind = 8;
                                break;

                            case 5:
                                if (m_curChar == 38)
                                    jjstateSet[jjnewStateCnt++] = 4;
                                break;

                            case 13:
                                if (m_curChar == 33 && kind > 10)
                                    kind = 10;
                                break;

                            case 14:
                                if ((0x280200000000L & l) != 0L)
                                    jjstateSet[jjnewStateCnt++] = 15;
                                break;
                            case 15:
                                if ((0x100002600L & l) != 0L && kind > 13)
                                    kind = 13;
                                break;
                            case 16:
                                if (m_curChar == 34)
                                    JjCheckNAddStates(3, 5);
                                break;
                            case 17:
                                if ((0xfffffffbffffffffL & l) != (ulong) 0L)
                                    JjCheckNAddStates(3, 5);
                                break;

                            case 19:
                                JjCheckNAddStates(3, 5);
                                break;

                            case 20:
                                if (m_curChar == 34 && kind > 19)
                                    kind = 19;
                                break;

                            case 22:
                                if ((0x3ff000000000000L & l) == 0L)
                                    break;
                                if (kind > 21)
                                    kind = 21;
                                JjCheckNAddStates(11, 14);
                                break;

                            case 23:
                                if (m_curChar == 46)
                                    JjCheckNAdd(24);
                                break;

                            case 24:
                                if ((0x3ff000000000000L & l) == 0L)
                                    break;
                                if (kind > 21)
                                    kind = 21;
                                JjCheckNAddStates(15, 17);
                                break;

                            case 25:
                                if ((0x7bff78f8ffffd9ffL & l) == (ulong)0L)
                                    break;
                                if (kind > 21)
                                    kind = 21;
                                JjCheckNAddTwoStates(25, 26);
                                break;

                            case 27:
                                if (kind > 21)
                                    kind = 21;
                                JjCheckNAddTwoStates(25, 26);
                                break;

                            case 28:
                                if ((0x7bff78f8ffffd9ffL & l) == 0L)
                                    break;
                                if (kind > 21)
                                    kind = 21;
                                JjCheckNAddTwoStates(28, 29);
                                break;

                            case 30:
                                if (kind > 21)
                                    kind = 21;
                                JjCheckNAddTwoStates(28, 29);
                                break;

                            case 31:
                                if (m_curChar == 42 && kind > 22)
                                    kind = 22;
                                break;

                            case 32:
                                if ((0xfbff54f8ffffd9ffL & l) == (ulong)0L)
                                    break;
                                if (kind > 23)
                                    kind = 23;
                                JjCheckNAddTwoStates(33, 34);
                                break;
                            case 35:
                                if (kind > 23)
                                    kind = 23;
                                JjCheckNAddTwoStates(33, 34);
                                break;
                            case 36:
                            case 38:
                                if (m_curChar == 47)
                                    JjCheckNAddStates(0, 2);
                                break;
                            case 37:
                                if ((0xffff7fffffffffffL & l) != (ulong)0L)
                                    JjCheckNAddStates(0, 2);
                                break;
                            case 40:
                                if (m_curChar == 47 && kind > 24)
                                    kind = 24;
                                break;
                            case 41:
                                if ((0x7bff50f8ffffd9ffL & l) == 0L)
                                    break;
                                if (kind > 20)
                                    kind = 20;
                                JjCheckNAddStates(6, 10);
                                break;
                            case 42:
                                if ((0x7bff78f8ffffd9ffL & l) == 0L)
                                    break;
                                if (kind > 20)
                                    kind = 20;
                                JjCheckNAddTwoStates(42, 43);
                                break;
                            case 44:
                                if (kind > 20)
                                    kind = 20;
                                JjCheckNAddTwoStates(42, 43);
                                break;
                            case 45:
                                if ((0x7bff78f8ffffd9ffL & l) != 0L)
                                    JjCheckNAddStates(18, 20);
                                break;
                            case 47:
                                JjCheckNAddStates(18, 20);
                                break;

                            default:  break;

                        }
                    }
                    while (i != startsAt);
                }
                else if (m_curChar < 128)
                {
                    // NOTE: This didn't change in Java from 3.0.1 to 4.8.0, but it is different in .NET.
                    // But changing it back made more tests pass, so I am working under the assumption 63
                    // is the correct value.
                    //ulong l = (ulong)(1L << (curChar & 077));
                    ulong l = (ulong) (1L << (m_curChar & 63));
                    do
                    {
                        switch (jjstateSet[--i])
                        {

                            case 49:
                                if ((0x97ffffff87ffffffL & l) != (ulong) 0L)
                                {
                                    if (kind > 23)
                                        kind = 23;
                                    JjCheckNAddTwoStates(33, 34);
                                }
                                else if (m_curChar == 92)
                                    JjCheckNAddTwoStates(35, 35);
                                break;

                            case 0:
                                if ((0x97ffffff87ffffffL & l) != (ulong) 0L)
                                {
                                    if (kind > 20)
                                        kind = 20;
                                    JjCheckNAddStates(6, 10);
                                }
                                else if (m_curChar == 92)
                                    JjCheckNAddStates(21, 23);
                                else if (m_curChar == 126)
                                {
                                    if (kind > 21)
                                        kind = 21;
                                    JjCheckNAddStates(24, 26);
                                }
                                if ((0x97ffffff87ffffffL & l) != (ulong) 0L)
                                {
                                    if (kind > 23)
                                        kind = 23;
                                    JjCheckNAddTwoStates(33, 34);
                                }
                                if (m_curChar == 78)
                                    jjstateSet[jjnewStateCnt++] = 11;
                                else if (m_curChar == 124)
                                    jjstateSet[jjnewStateCnt++] = 8;
                                else if (m_curChar == 79)
                                    jjstateSet[jjnewStateCnt++] = 6;
                                else if (m_curChar == 65)
                                    jjstateSet[jjnewStateCnt++] = 2;
                                break;

                            case 1:
                                if (m_curChar == 68 && kind > 8)
                                    kind = 8;
                                break;

                            case 2:
                                if (m_curChar == 78)
                                    jjstateSet[jjnewStateCnt++] = 1;
                                break;

                            case 3:
                                if (m_curChar == 65)
                                    jjstateSet[jjnewStateCnt++] = 2;
                                break;

                            case 6:
                                if (m_curChar == 82 && kind > 9)
                                    kind = 9;
                                break;

                            case 7:
                                if (m_curChar == 79)
                                    jjstateSet[jjnewStateCnt++] = 6;
                                break;

                            case 8:
                                if (m_curChar == 124 && kind > 9)
                                    kind = 9;
                                break;

                            case 9:
                                if (m_curChar == 124)
                                    jjstateSet[jjnewStateCnt++] = 8;
                                break;

                            case 10:
                                if (m_curChar == 84 && kind > 10)
                                    kind = 10;
                                break;

                            case 11:
                                if (m_curChar == 79)
                                    jjstateSet[jjnewStateCnt++] = 10;
                                break;

                            case 12:
                                if (m_curChar == 78)
                                    jjstateSet[jjnewStateCnt++] = 11;
                                break;

                            case 17:
                                if ((0xffffffffefffffffL & l) != (ulong) 0L)
                                    JjCheckNAddStates(3, 5);
                                break;

                            case 18:
                                if (m_curChar == 92)
                                    jjstateSet[jjnewStateCnt++] = 19;
                                break;

                            case 19:
                                JjCheckNAddStates(3, 5);
                                break;

                            case 21:
                                if (m_curChar != 126)
                                    break;
                                if (kind > 21)
                                    kind = 21;
                                JjCheckNAddStates(24, 26);
                                break;

                            case 25:
                                if ((0x97ffffff87ffffffL & l) == (ulong) 0L)
                                    break;
                                if (kind > 21)
                                    kind = 21;
                                JjCheckNAddTwoStates(25, 26);
                                break;

                            case 26:
                                if (m_curChar == 92)
                                    JjAddStates(27, 28);
                                break;

                            case 27:
                                if (kind > 21)
                                    kind = 21;
                                JjCheckNAddTwoStates(25, 26);
                                break;

                            case 28:
                                if ((0x97ffffff87ffffffL & l) == (ulong) 0L)
                                    break;
                                if (kind > 21)
                                    kind = 21;
                                JjCheckNAddTwoStates(28, 29);
                                break;

                            case 29:
                                if (m_curChar == 92)
                                    JjAddStates(29, 30);
                                break;
                            case 30:
                                if (kind > 21)
                                    kind = 21;
                                JjCheckNAddTwoStates(28, 29);
                                break;
                            case 32:
                                if ((0x97ffffff87ffffffL & l) == (ulong)0L)
                                    break;
                                if (kind > 23)
                                    kind = 23;
                                JjCheckNAddTwoStates(33, 34);
                                break;
                            case 33:
                                if ((0x97ffffff87ffffffL & l) == (ulong)0L)
                                    break;
                                if (kind > 23)
                                    kind = 23;
                                JjCheckNAddTwoStates(33, 34);
                                break;
                            case 34:
                                if (m_curChar == 92)
                                    JjCheckNAddTwoStates(35, 35);
                                break;
                            case 35:
                                if (kind > 23)
                                    kind = 23;
                                JjCheckNAddTwoStates(33, 34);
                                break;
                            case 37:
                                JjAddStates(0, 2);
                                break;
                            case 39:
                                if (m_curChar == 92)
                                    jjstateSet[jjnewStateCnt++] = 38;
                                break;
                            case 41:
                                if ((0x97ffffff87ffffffL & l) == (ulong)0L)
                                    break;
                                if (kind > 20)
                                    kind = 20;
                                JjCheckNAddStates(6, 10);
                                break;
                            case 42:
                                if ((0x97ffffff87ffffffL & l) == (ulong)0L)
                                    break;
                                if (kind > 20)
                                    kind = 20;
                                JjCheckNAddTwoStates(42, 43);
                                break;
                            case 43:
                                if (m_curChar == 92)
                                    JjCheckNAddTwoStates(44, 44);
                                break;
                            case 44:
                                if (kind > 20)
                                    kind = 20;
                                JjCheckNAddTwoStates(42, 43);
                                break;
                            case 45:
                                if ((0x97ffffff87ffffffL & l) != (ulong)0L)
                                    JjCheckNAddStates(18, 20);
                                break;
                            case 46:
                                if (m_curChar == 92)
                                    JjCheckNAddTwoStates(47, 47);
                                break;
                            case 47:
                                JjCheckNAddStates(18, 20);
                                break;
                            case 48:
                                if (m_curChar == 92)
                                    JjCheckNAddStates(21, 23);
                                break;

                            default: break;

                        }
                    }
                    while (i != startsAt);
                }
                else
                {
                    int hiByte = (int) (m_curChar >> 8);
                    int i1 = hiByte >> 6;
                    ulong l1 = (ulong) (1L << (hiByte & 63));
                    int i2 = (m_curChar & 0xff) >> 6;
                    ulong l2 = (ulong) (1L << (m_curChar & 63));
                    do
                    {
                        switch (jjstateSet[--i])
                        {

                            case 49:
                            case 33:
                                if (!JjCanMove_2(hiByte, i1, i2, l1, l2))
                                    break;
                                if (kind > 23)
                                    kind = 23;
                                JjCheckNAddTwoStates(33, 34);
                                break;

                            case 0:
                                if (JjCanMove_0(hiByte, i1, i2, l1, l2))
                                {
                                    if (kind > 7)
                                        kind = 7;
                                }
                                if (JjCanMove_2(hiByte, i1, i2, l1, l2))
                                {
                                    if (kind > 23)
                                        kind = 23;
                                    JjCheckNAddTwoStates(33, 34);
                                }
                                if (JjCanMove_2(hiByte, i1, i2, l1, l2))
                                {
                                    if (kind > 20)
                                        kind = 20;
                                    JjCheckNAddStates(6, 10);
                                }
                                break;

                            case 15:
                                if (JjCanMove_0(hiByte, i1, i2, l1, l2) && kind > 13)
                                    kind = 13;
                                break;
                            case 17:
                            case 19:
                                if (JjCanMove_1(hiByte, i1, i2, l1, l2))
                                    JjCheckNAddStates(3, 5);
                                break;

                            case 25:
                                if (!JjCanMove_2(hiByte, i1, i2, l1, l2))
                                    break;
                                if (kind > 21)
                                    kind = 21;
                                JjCheckNAddTwoStates(25, 26);
                                break;

                            case 27:
                                if (!JjCanMove_1(hiByte, i1, i2, l1, l2))
                                    break;
                                if (kind > 21)
                                    kind = 21;
                                JjCheckNAddTwoStates(25, 26);
                                break;

                            case 28:
                                if (!JjCanMove_2(hiByte, i1, i2, l1, l2))
                                    break;
                                if (kind > 21)
                                    kind = 21;
                                JjCheckNAddTwoStates(28, 29);
                                break;
                            case 30:
                                if (!JjCanMove_1(hiByte, i1, i2, l1, l2))
                                    break;
                                if (kind > 21)
                                    kind = 21;
                                JjCheckNAddTwoStates(28, 29);
                                break;
                            case 32:
                                if (!JjCanMove_2(hiByte, i1, i2, l1, l2))
                                    break;
                                if (kind > 23)
                                    kind = 23;
                                JjCheckNAddTwoStates(33, 34);
                                break;

                            case 35:
                                if (!JjCanMove_1(hiByte, i1, i2, l1, l2))
                                    break;
                                if (kind > 23)
                                    kind = 23;
                                JjCheckNAddTwoStates(33, 34);
                                break;

                            case 37:
                                if (JjCanMove_1(hiByte, i1, i2, l1, l2))
                                    JjAddStates(0, 2);
                                break;
                            case 41:
                                if (!JjCanMove_2(hiByte, i1, i2, l1, l2))
                                    break;
                                if (kind > 20)
                                    kind = 20;
                                JjCheckNAddStates(6, 10);
                                break;
                            case 42:
                                if (!JjCanMove_2(hiByte, i1, i2, l1, l2))
                                    break;
                                if (kind > 20)
                                    kind = 20;
                                JjCheckNAddTwoStates(42, 43);
                                break;
                            case 44:
                                if (!JjCanMove_1(hiByte, i1, i2, l1, l2))
                                    break;
                                if (kind > 20)
                                    kind = 20;
                                JjCheckNAddTwoStates(42, 43);
                                break;
                            case 45:
                                if (JjCanMove_2(hiByte, i1, i2, l1, l2))
                                    JjCheckNAddStates(18, 20);
                                break;

                            case 47:
                                if (JjCanMove_1(hiByte, i1, i2, l1, l2))
                                    JjCheckNAddStates(18, 20);
                                break;

                            default:  break;

                        }
                    }
                    while (i != startsAt);
                }
                if (kind != 0x7fffffff)
                {
                    jjmatchedKind = kind;
                    jjmatchedPos = curPos;
                    kind = 0x7fffffff;
                }
                ++curPos;
                if ((i = jjnewStateCnt) == (startsAt = 49 - (jjnewStateCnt = startsAt)))
                    return curPos;
                try
                {
                    m_curChar = m_input_stream.ReadChar();
                }
                catch (Exception e) when (e.IsIOException())
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
            int kind = 0x7fffffff;
            for (; ; )
            {
                if (++jjround == 0x7fffffff)
                    ReInitRounds();
                if (m_curChar < 64)
                {
                    ulong l = (ulong) (1L << (int) m_curChar);
                    do
                    {
                        switch (jjstateSet[--i])
                        {

                            case 0:
                                if ((0x3ff000000000000L & l) == 0L)
                                    break;
                                if (kind > 27)
                                    kind = 27;
                                JjAddStates(31, 32);
                                break;

                            case 1:
                                if (m_curChar == 46)
                                    JjCheckNAdd(2);
                                break;

                            case 2:
                                if ((0x3ff000000000000L & l) == 0L)
                                    break;
                                if (kind > 27)
                                    kind = 27;
                                JjCheckNAdd(2);
                                break;

                            default:  break;

                        }
                    }
                    while (i != startsAt);
                }
                else if (m_curChar < 128)
                {
                    ulong l = (ulong) (1L << (m_curChar & 63));
                    do
                    {
                        switch (jjstateSet[--i])
                        {

                            default:  break;

                        }
                    }
                    while (i != startsAt);
                }
                else
                {
                    int hiByte = (int) (m_curChar >> 8);
                    int i1 = hiByte >> 6;
                    long l1 = 1L << (hiByte & 63);
                    int i2 = (m_curChar & 0xff) >> 6;
                    long l2 = 1L << (m_curChar & 63);
                    do
                    {
                        switch (jjstateSet[--i])
                        {

                            default:  break;

                        }
                    }
                    while (i != startsAt);
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
                try
                {
                    m_curChar = m_input_stream.ReadChar();
                }
                catch (Exception e) when (e.IsIOException())
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
                    if ((active0 & 0x10000000L) != 0L)
                    {
                        jjmatchedKind = 32;
                        return 6;
                    }
                    return - 1;

                default:
                    return - 1;

            }
        }
        private int JjStartNfa_1(int pos, long active0)
        {
            return JjMoveNfa_1(JjStopStringLiteralDfa_1(pos, active0), pos + 1);
        }
        private int JjMoveStringLiteralDfa0_1()
        {
            switch (m_curChar)
            {

                case (char)84:
                    return JjMoveStringLiteralDfa1_1(0x10000000L);

                case (char)93:
                    return JjStopAtPos(0, 29);

                case (char)125:
                    return JjStopAtPos(0, 30);

                default:
                    return JjMoveNfa_1(0, 0);

            }
        }
        private int JjMoveStringLiteralDfa1_1(long active0)
        {
            try
            {
                m_curChar = m_input_stream.ReadChar();
            }
            catch (Exception e) when (e.IsIOException())
            {
                JjStopStringLiteralDfa_1(0, active0);
                return 1;
            }
            switch (m_curChar)
            {

                case (char) (79):
                    if ((active0 & 0x10000000L) != 0L)
                        return JjStartNfaWithStates_1(1, 28, 6);
                    break;

                default:
                    break;

            }
            return JjStartNfa_1(0, active0);
        }
        private int JjStartNfaWithStates_1(int pos, int kind, int state)
        {
            jjmatchedKind = kind;
            jjmatchedPos = pos;
            try
            {
                m_curChar = m_input_stream.ReadChar();
            }
            catch (Exception e) when (e.IsIOException())
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
            int kind = 0x7fffffff;
            for (; ; )
            {
                if (++jjround == 0x7fffffff)
                    ReInitRounds();
                if (m_curChar < 64)
                {
                    ulong l = (ulong) (1L << (int) m_curChar);
                    do
                    {
                        switch (jjstateSet[--i])
                        {

                            case 0:
                                if ((0xfffffffeffffffffL & l) != (ulong)0L)
                                {
                                    if (kind > 32)
                                        kind = 32;
                                    JjCheckNAdd(6);
                                }
                                if ((0x100002600L & l) != 0L)
                                {
                                    if (kind > 7)
                                        kind = 7;
                                }
                                else if (m_curChar == 34)
                                    JjCheckNAddTwoStates(2, 4);
                                break;

                            case 1:
                                if (m_curChar == 34)
                                    JjCheckNAddTwoStates(2, 4);
                                break;

                            case 2:
                                if ((0xfffffffbffffffffL & l) != (ulong)0L)
                                    JjCheckNAddStates(33, 35);
                                break;

                            case 3:
                                if (m_curChar == 34)
                                    JjCheckNAddStates(33, 35);
                                break;

                            case 5:
                                if (m_curChar == 34 && kind > 31)
                                    kind = 31;
                                break;

                            case 6:
                                if ((0xfffffffeffffffffL & l) == (ulong)0L)
                                    break;
                                if (kind > 32)
                                    kind = 32;
                                JjCheckNAdd(6);
                                break;

                            default:  break;

                        }
                    }
                    while (i != startsAt);
                }
                else if (m_curChar < 128)
                {
                    ulong l = (ulong) (1L << (m_curChar & 63));
                    do
                    {
                        switch (jjstateSet[--i])
                        {

                            case 0:
                            case 6:
                                if ((0xdfffffffdfffffffL & l) == (ulong)0L)
                                    break;
                                if (kind > 32)
                                    kind = 32;
                                JjCheckNAdd(6);
                                break;

                            case 2:
                                JjAddStates(33, 35);
                                break;

                            case 4:
                                if (m_curChar == 92)
                                    jjstateSet[jjnewStateCnt++] = 3;
                                break;

                            default:  break;

                        }
                    }
                    while (i != startsAt);
                }
                else
                {
                    int hiByte = (int) (m_curChar >> 8);
                    int i1 = hiByte >> 6;
                    ulong l1 = (ulong) (1L << (hiByte & 63));
                    int i2 = (m_curChar & 0xff) >> 6;
                    ulong l2 = (ulong) (1L << (m_curChar & 63));
                    do
                    {
                        switch (jjstateSet[--i])
                        {

                            case 0:
                                if (JjCanMove_0(hiByte, i1, i2, l1, l2))
                                {
                                    if (kind > 7)
                                        kind = 7;
                                }
                                if (JjCanMove_1(hiByte, i1, i2, l1, l2))
                                {
                                    if (kind > 32)
                                        kind = 32;
                                    JjCheckNAdd(6);
                                }
                                break;

                            case 2:
                                if (JjCanMove_1(hiByte, i1, i2, l1, l2))
                                    JjAddStates(33, 35);
                                break;

                            case 6:
                                if (!JjCanMove_1(hiByte, i1, i2, l1, l2))
                                    break;
                                if (kind > 32)
                                    kind = 32;
                                JjCheckNAdd(6);
                                break;

                            default:  break;

                        }
                    }
                    while (i != startsAt);
                }
                if (kind != 0x7fffffff)
                {
                    jjmatchedKind = kind;
                    jjmatchedPos = curPos;
                    kind = 0x7fffffff;
                }
                ++curPos;
                if ((i = jjnewStateCnt) == (startsAt = 7 - (jjnewStateCnt = startsAt)))
                    return curPos;
                try
                {
                    m_curChar = m_input_stream.ReadChar();
                }
                catch (Exception e) when (e.IsIOException())
                {
                    return curPos;
                }
            }
        }
        internal static readonly int[] jjnextStates = new int[]{
           37, 39, 40, 17, 18, 20, 42, 45, 31, 46, 43, 22, 23, 25, 26, 24,
           25, 26, 45, 31, 46, 44, 47, 35, 22, 28, 29, 27, 27, 30, 30, 0,
           1, 2, 4, 5
        };
        private static bool JjCanMove_0(int hiByte, int i1, int i2, ulong l1, ulong l2)
        {
            switch (hiByte)
            {

                case 48:
                    return ((jjbitVec0[i2] & l2) != (ulong) 0L);

                default:
                    return false;

            }
        }
        private static bool JjCanMove_1(int hiByte, int i1, int i2, ulong l1, ulong l2)
        {
            switch (hiByte)
            {

                case 0:
                    return ((jjbitVec3[i2] & l2) != (ulong) 0L);

                default:
                    if ((jjbitVec1[i1] & l1) != (ulong) 0L)
                        return true;
                    return false;

            }
        }
        private static bool JjCanMove_2(int hiByte, int i1, int i2, ulong l1, ulong l2)
        {
            switch (hiByte)
            {

                case 0:
                    return ((jjbitVec3[i2] & l2) != (ulong) 0L);

                case 48:
                    return ((jjbitVec1[i2] & l2) != (ulong) 0L);

                default:
                    if ((jjbitVec4[i1] & l1) != (ulong) 0L)
                        return true;
                    return false;

            }
        }

        ///// <summary>Token literal values. </summary>
        //public static readonly string[] jjstrLiteralImages = new string[] {
        //    "", null, null, null, null, null, null, null, null, null, null, "\x002B", "\x002D",
        //    "\x0028", "\x0029", "\x003A", "\x002A", "\x005E", null, null, null, null, null, "\x005B", "\x007B",
        //    null, "\x0054\x004F", "\x005D", null, null, "\x0054\x004F", "\x007D", null, null };


        /// <summary>Token literal values. </summary>
        public static readonly string[] jjstrLiteralImages = new string[]{
            "", null, null, null, null, null, null, null, null, null, null, "\x002B", "\x002D",
            null, "\x0028", "\x0029", "\x003A", "\x002A", "\x005E", null, null, null, null, null, null,
            "\x005B", "\x007B", null, "\x0054\x004F", "\x005D", "\x007D", null, null };

        /// <summary>Lexer state names. </summary>
        public static readonly string[] lexStateNames = new string[] {
            "Boost",
            "Range",
            "DEFAULT"
        };

        /// <summary>Lex State array. </summary>
        public static readonly int[] jjnewLexState = new int[] {
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 0, -1, -1, -1, -1, -1, -1,
            1, 1, 2, -1, 2, 2, -1, -1
        };
        internal static readonly ulong[] jjtoToken = new ulong[] { 0x1ffffff01L };
        internal static readonly long[] jjtoSkip = new long[] { 0x80L };
        protected ICharStream m_input_stream;
        private readonly uint[] jjrounds = new uint[49]; // LUCENENET: marked readonly
        private readonly int[] jjstateSet = new int[98]; // LUCENENET: marked readonly
        protected char m_curChar;
        /// <summary>Constructor. </summary>
        public QueryParserTokenManager(ICharStream stream)
        {
            InitBlock();
            m_input_stream = stream;
        }

        /// <summary>Constructor. 
        /// <para>Note that this constructor calls a virtual method <see cref="SwitchTo(int)" />. If you
        /// are subclassing this class, use <see cref="QueryParserTokenManager(ICharStream)" /> constructor and
        /// call SwitchTo if needed.</para>
        /// </summary>
        [SuppressMessage("CodeQuality", "IDE0079:Remove unnecessary suppression", Justification = "This is a SonarCloud issue")]
        [SuppressMessage("CodeQuality", "S1699:Constructors should only call non-overridable methods", Justification = "Required for continuity with Lucene's design")]
        public QueryParserTokenManager(ICharStream stream, int lexState):this(stream)
        {
            SwitchTo(lexState);
        }

        /// <summary>Reinitialise parser. </summary>
        public virtual void ReInit(ICharStream stream)
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
            for (i = 49; i-- > 0; )
                jjrounds[i] = 0x80000000;
        }

        /// <summary>Reinitialise parser. </summary>
        public virtual void ReInit(ICharStream stream, int lexState)
        {
            ReInit(stream);
            SwitchTo(lexState);
        }

        /// <summary>Switch to specified lex state. </summary>
        public virtual void SwitchTo(int lexState)
        {
            if (lexState >= 3 || lexState < 0)
                throw new TokenMgrError("Error: Ignoring invalid lexical state : " + lexState + ". State unchanged.", TokenMgrError.INVALID_LEXICAL_STATE);
            else
                curLexState = lexState;
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

        internal int curLexState = 2;
        internal int defaultLexState = 2;
        internal int jjnewStateCnt;
        internal uint jjround;
        internal int jjmatchedPos;
        internal int jjmatchedKind;

        /// <summary>Get the next Token. </summary>
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
                        curPos = JjMoveStringLiteralDfa0_1();
                        break;

                    case 2:
                        jjmatchedKind = 0x7fffffff;
                        jjmatchedPos = 0;
                        curPos = JjMoveStringLiteralDfa0_2();
                        break;
                    }
                if (jjmatchedKind != 0x7fffffff)
                {
                    if (jjmatchedPos + 1 < curPos)
                        m_input_stream.BackUp(curPos - jjmatchedPos - 1);
                    if ((jjtoToken[jjmatchedKind >> 6] & ((ulong) 1L << (jjmatchedKind & 63))) != (ulong) 0L)
                    {
                        matchedToken = JjFillToken();
                        if (jjnewLexState[jjmatchedKind] != - 1)
                            curLexState = jjnewLexState[jjmatchedKind];
                        return matchedToken;
                    }
                    else
                    {
                        if (jjnewLexState[jjmatchedKind] != - 1)
                            curLexState = jjnewLexState[jjmatchedKind];
                        goto EOFLoop;
                    }
                }
                int error_line = m_input_stream.EndLine;
                int error_column = m_input_stream.EndColumn;
                string error_after = null;
                bool EOFSeen = false;
                try
                {
                    m_input_stream.ReadChar(); m_input_stream.BackUp(1);
                }
                catch (Exception e) when (e.IsIOException())
                {
                    EOFSeen = true;
                    error_after = curPos <= 1?"":m_input_stream.Image;
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
                    error_after = curPos <= 1?"":m_input_stream.Image;
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