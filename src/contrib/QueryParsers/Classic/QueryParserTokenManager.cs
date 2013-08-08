using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lucene.Net.QueryParsers.Classic
{
    public class QueryParserTokenManager : IQueryParserConstants
    {
        /** Debug output. */
        public TextWriter debugStream = Console.Out;
        /** Set debug output. */
        public void SetDebugStream(TextWriter ds) { debugStream = ds; }

        private int jjStopStringLiteralDfa_2(int pos, long active0)
        {
            switch (pos)
            {
                default:
                    return -1;
            }
        }

        private int jjStartNfa_2(int pos, long active0)
        {
            return jjMoveNfa_2(jjStopStringLiteralDfa_2(pos, active0), pos + 1);
        }

        private int jjStopAtPos(int pos, int kind)
        {
            jjmatchedKind = kind;
            jjmatchedPos = pos;
            return pos + 1;
        }

        private int jjMoveStringLiteralDfa0_2()
        {
            switch (curChar)
            {
                case (char)40:
                    return jjStopAtPos(0, 14);
                case (char)41:
                    return jjStopAtPos(0, 15);
                case (char)42:
                    return jjStartNfaWithStates_2(0, 17, 49);
                case (char)43:
                    return jjStartNfaWithStates_2(0, 11, 15);
                case (char)45:
                    return jjStartNfaWithStates_2(0, 12, 15);
                case (char)58:
                    return jjStopAtPos(0, 16);
                case (char)91:
                    return jjStopAtPos(0, 25);
                case (char)94:
                    return jjStopAtPos(0, 18);
                case (char)123:
                    return jjStopAtPos(0, 26);
                default:
                    return jjMoveNfa_2(0, 0);
            }
        }

        private int jjStartNfaWithStates_2(int pos, int kind, int state)
        {
            jjmatchedKind = kind;
            jjmatchedPos = pos;
            try { curChar = input_stream.ReadChar(); }
            catch (IOException e) { return pos + 1; }
            return jjMoveNfa_2(state, pos + 1);
        }

        internal static readonly long[] jjbitVec0 = {
            0x1L, 0x0L, 0x0L, 0x0L
        };
        internal static readonly long[] jjbitVec1 = {
            unchecked((long)0xfffffffffffffffeL), unchecked((long)0xffffffffffffffffL), unchecked((long)0xffffffffffffffffL), unchecked((long)0xffffffffffffffffL)
        };
        internal static readonly long[] jjbitVec3 = {
            0x0L, 0x0L, unchecked((long)0xffffffffffffffffL), unchecked((long)0xffffffffffffffffL)
        };
        internal static readonly long[] jjbitVec4 = {
            unchecked((long)0xfffefffffffffffeL), unchecked((long)0xffffffffffffffffL), unchecked((long)0xffffffffffffffffL), unchecked((long)0xffffffffffffffffL)
        };

        private int jjMoveNfa_2(int startState, int curPos)
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
                if (curChar < 64)
                {
                    long l = 1L << curChar;
                    do
                    {
                        switch (jjstateSet[--i])
                        {
                            case 49:
                            case 33:
                                if ((unchecked((long)0xfbff7cf8ffffd9ffL) & l) == 0L)
                                    break;
                                if (kind > 23)
                                    kind = 23;
                                jjCheckNAddTwoStates(33, 34);
                                break;
                            case 0:
                                if ((unchecked((long)0xfbff54f8ffffd9ffL) & l) != 0L)
                                {
                                    if (kind > 23)
                                        kind = 23;
                                    jjCheckNAddTwoStates(33, 34);
                                }
                                else if ((0x100002600L & l) != 0L)
                                {
                                    if (kind > 7)
                                        kind = 7;
                                }
                                else if ((0x280200000000L & l) != 0L)
                                    jjstateSet[jjnewStateCnt++] = 15;
                                else if (curChar == 47)
                                    jjCheckNAddStates(0, 2);
                                else if (curChar == 34)
                                    jjCheckNAddStates(3, 5);
                                if ((0x7bff50f8ffffd9ffL & l) != 0L)
                                {
                                    if (kind > 20)
                                        kind = 20;
                                    jjCheckNAddStates(6, 10);
                                }
                                else if (curChar == 42)
                                {
                                    if (kind > 22)
                                        kind = 22;
                                }
                                else if (curChar == 33)
                                {
                                    if (kind > 10)
                                        kind = 10;
                                }
                                if (curChar == 38)
                                    jjstateSet[jjnewStateCnt++] = 4;
                                break;
                            case 4:
                                if (curChar == 38 && kind > 8)
                                    kind = 8;
                                break;
                            case 5:
                                if (curChar == 38)
                                    jjstateSet[jjnewStateCnt++] = 4;
                                break;
                            case 13:
                                if (curChar == 33 && kind > 10)
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
                                if (curChar == 34)
                                    jjCheckNAddStates(3, 5);
                                break;
                            case 17:
                                if ((unchecked((long)0xfffffffbffffffffL) & l) != 0L)
                                    jjCheckNAddStates(3, 5);
                                break;
                            case 19:
                                jjCheckNAddStates(3, 5);
                                break;
                            case 20:
                                if (curChar == 34 && kind > 19)
                                    kind = 19;
                                break;
                            case 22:
                                if ((0x3ff000000000000L & l) == 0L)
                                    break;
                                if (kind > 21)
                                    kind = 21;
                                jjCheckNAddStates(11, 14);
                                break;
                            case 23:
                                if (curChar == 46)
                                    jjCheckNAdd(24);
                                break;
                            case 24:
                                if ((0x3ff000000000000L & l) == 0L)
                                    break;
                                if (kind > 21)
                                    kind = 21;
                                jjCheckNAddStates(15, 17);
                                break;
                            case 25:
                                if ((0x7bff78f8ffffd9ffL & l) == 0L)
                                    break;
                                if (kind > 21)
                                    kind = 21;
                                jjCheckNAddTwoStates(25, 26);
                                break;
                            case 27:
                                if (kind > 21)
                                    kind = 21;
                                jjCheckNAddTwoStates(25, 26);
                                break;
                            case 28:
                                if ((0x7bff78f8ffffd9ffL & l) == 0L)
                                    break;
                                if (kind > 21)
                                    kind = 21;
                                jjCheckNAddTwoStates(28, 29);
                                break;
                            case 30:
                                if (kind > 21)
                                    kind = 21;
                                jjCheckNAddTwoStates(28, 29);
                                break;
                            case 31:
                                if (curChar == 42 && kind > 22)
                                    kind = 22;
                                break;
                            case 32:
                                if ((unchecked((long)0xfbff54f8ffffd9ffL) & l) == 0L)
                                    break;
                                if (kind > 23)
                                    kind = 23;
                                jjCheckNAddTwoStates(33, 34);
                                break;
                            case 35:
                                if (kind > 23)
                                    kind = 23;
                                jjCheckNAddTwoStates(33, 34);
                                break;
                            case 36:
                            case 38:
                                if (curChar == 47)
                                    jjCheckNAddStates(0, 2);
                                break;
                            case 37:
                                if ((unchecked((long)0xffff7fffffffffffL) & l) != 0L)
                                    jjCheckNAddStates(0, 2);
                                break;
                            case 40:
                                if (curChar == 47 && kind > 24)
                                    kind = 24;
                                break;
                            case 41:
                                if ((0x7bff50f8ffffd9ffL & l) == 0L)
                                    break;
                                if (kind > 20)
                                    kind = 20;
                                jjCheckNAddStates(6, 10);
                                break;
                            case 42:
                                if ((0x7bff78f8ffffd9ffL & l) == 0L)
                                    break;
                                if (kind > 20)
                                    kind = 20;
                                jjCheckNAddTwoStates(42, 43);
                                break;
                            case 44:
                                if (kind > 20)
                                    kind = 20;
                                jjCheckNAddTwoStates(42, 43);
                                break;
                            case 45:
                                if ((0x7bff78f8ffffd9ffL & l) != 0L)
                                    jjCheckNAddStates(18, 20);
                                break;
                            case 47:
                                jjCheckNAddStates(18, 20);
                                break;
                            default: break;
                        }
                    } while (i != startsAt);
                }
                else if (curChar < 128)
                {
                    long l = 1L << (curChar & 077);
                    do
                    {
                        switch (jjstateSet[--i])
                        {
                            case 49:
                                if ((unchecked((long)0x97ffffff87ffffffL) & l) != 0L)
                                {
                                    if (kind > 23)
                                        kind = 23;
                                    jjCheckNAddTwoStates(33, 34);
                                }
                                else if (curChar == 92)
                                    jjCheckNAddTwoStates(35, 35);
                                break;
                            case 0:
                                if ((unchecked((long)0x97ffffff87ffffffL) & l) != 0L)
                                {
                                    if (kind > 20)
                                        kind = 20;
                                    jjCheckNAddStates(6, 10);
                                }
                                else if (curChar == 92)
                                    jjCheckNAddStates(21, 23);
                                else if (curChar == 126)
                                {
                                    if (kind > 21)
                                        kind = 21;
                                    jjCheckNAddStates(24, 26);
                                }
                                if ((unchecked((long)0x97ffffff87ffffffL) & l) != 0L)
                                {
                                    if (kind > 23)
                                        kind = 23;
                                    jjCheckNAddTwoStates(33, 34);
                                }
                                if (curChar == 78)
                                    jjstateSet[jjnewStateCnt++] = 11;
                                else if (curChar == 124)
                                    jjstateSet[jjnewStateCnt++] = 8;
                                else if (curChar == 79)
                                    jjstateSet[jjnewStateCnt++] = 6;
                                else if (curChar == 65)
                                    jjstateSet[jjnewStateCnt++] = 2;
                                break;
                            case 1:
                                if (curChar == 68 && kind > 8)
                                    kind = 8;
                                break;
                            case 2:
                                if (curChar == 78)
                                    jjstateSet[jjnewStateCnt++] = 1;
                                break;
                            case 3:
                                if (curChar == 65)
                                    jjstateSet[jjnewStateCnt++] = 2;
                                break;
                            case 6:
                                if (curChar == 82 && kind > 9)
                                    kind = 9;
                                break;
                            case 7:
                                if (curChar == 79)
                                    jjstateSet[jjnewStateCnt++] = 6;
                                break;
                            case 8:
                                if (curChar == 124 && kind > 9)
                                    kind = 9;
                                break;
                            case 9:
                                if (curChar == 124)
                                    jjstateSet[jjnewStateCnt++] = 8;
                                break;
                            case 10:
                                if (curChar == 84 && kind > 10)
                                    kind = 10;
                                break;
                            case 11:
                                if (curChar == 79)
                                    jjstateSet[jjnewStateCnt++] = 10;
                                break;
                            case 12:
                                if (curChar == 78)
                                    jjstateSet[jjnewStateCnt++] = 11;
                                break;
                            case 17:
                                if ((unchecked((long)0xffffffffefffffffL) & l) != 0L)
                                    jjCheckNAddStates(3, 5);
                                break;
                            case 18:
                                if (curChar == 92)
                                    jjstateSet[jjnewStateCnt++] = 19;
                                break;
                            case 19:
                                jjCheckNAddStates(3, 5);
                                break;
                            case 21:
                                if (curChar != 126)
                                    break;
                                if (kind > 21)
                                    kind = 21;
                                jjCheckNAddStates(24, 26);
                                break;
                            case 25:
                                if ((unchecked((long)0x97ffffff87ffffffL) & l) == 0L)
                                    break;
                                if (kind > 21)
                                    kind = 21;
                                jjCheckNAddTwoStates(25, 26);
                                break;
                            case 26:
                                if (curChar == 92)
                                    jjAddStates(27, 28);
                                break;
                            case 27:
                                if (kind > 21)
                                    kind = 21;
                                jjCheckNAddTwoStates(25, 26);
                                break;
                            case 28:
                                if ((unchecked((long)0x97ffffff87ffffffL) & l) == 0L)
                                    break;
                                if (kind > 21)
                                    kind = 21;
                                jjCheckNAddTwoStates(28, 29);
                                break;
                            case 29:
                                if (curChar == 92)
                                    jjAddStates(29, 30);
                                break;
                            case 30:
                                if (kind > 21)
                                    kind = 21;
                                jjCheckNAddTwoStates(28, 29);
                                break;
                            case 32:
                                if ((unchecked((long)0x97ffffff87ffffffL) & l) == 0L)
                                    break;
                                if (kind > 23)
                                    kind = 23;
                                jjCheckNAddTwoStates(33, 34);
                                break;
                            case 33:
                                if ((unchecked((long)0x97ffffff87ffffffL) & l) == 0L)
                                    break;
                                if (kind > 23)
                                    kind = 23;
                                jjCheckNAddTwoStates(33, 34);
                                break;
                            case 34:
                                if (curChar == 92)
                                    jjCheckNAddTwoStates(35, 35);
                                break;
                            case 35:
                                if (kind > 23)
                                    kind = 23;
                                jjCheckNAddTwoStates(33, 34);
                                break;
                            case 37:
                                jjAddStates(0, 2);
                                break;
                            case 39:
                                if (curChar == 92)
                                    jjstateSet[jjnewStateCnt++] = 38;
                                break;
                            case 41:
                                if ((unchecked((long)0x97ffffff87ffffffL) & l) == 0L)
                                    break;
                                if (kind > 20)
                                    kind = 20;
                                jjCheckNAddStates(6, 10);
                                break;
                            case 42:
                                if ((unchecked((long)0x97ffffff87ffffffL) & l) == 0L)
                                    break;
                                if (kind > 20)
                                    kind = 20;
                                jjCheckNAddTwoStates(42, 43);
                                break;
                            case 43:
                                if (curChar == 92)
                                    jjCheckNAddTwoStates(44, 44);
                                break;
                            case 44:
                                if (kind > 20)
                                    kind = 20;
                                jjCheckNAddTwoStates(42, 43);
                                break;
                            case 45:
                                if ((unchecked((long)0x97ffffff87ffffffL) & l) != 0L)
                                    jjCheckNAddStates(18, 20);
                                break;
                            case 46:
                                if (curChar == 92)
                                    jjCheckNAddTwoStates(47, 47);
                                break;
                            case 47:
                                jjCheckNAddStates(18, 20);
                                break;
                            case 48:
                                if (curChar == 92)
                                    jjCheckNAddStates(21, 23);
                                break;
                            default: break;
                        }
                    } while (i != startsAt);
                }
                else
                {
                    int hiByte = (int)(curChar >> 8);
                    int i1 = hiByte >> 6;
                    long l1 = 1L << (hiByte & 077);
                    int i2 = (curChar & 0xff) >> 6;
                    long l2 = 1L << (curChar & 077);
                    do
                    {
                        switch (jjstateSet[--i])
                        {
                            case 49:
                            case 33:
                                if (!jjCanMove_2(hiByte, i1, i2, l1, l2))
                                    break;
                                if (kind > 23)
                                    kind = 23;
                                jjCheckNAddTwoStates(33, 34);
                                break;
                            case 0:
                                if (jjCanMove_0(hiByte, i1, i2, l1, l2))
                                {
                                    if (kind > 7)
                                        kind = 7;
                                }
                                if (jjCanMove_2(hiByte, i1, i2, l1, l2))
                                {
                                    if (kind > 23)
                                        kind = 23;
                                    jjCheckNAddTwoStates(33, 34);
                                }
                                if (jjCanMove_2(hiByte, i1, i2, l1, l2))
                                {
                                    if (kind > 20)
                                        kind = 20;
                                    jjCheckNAddStates(6, 10);
                                }
                                break;
                            case 15:
                                if (jjCanMove_0(hiByte, i1, i2, l1, l2) && kind > 13)
                                    kind = 13;
                                break;
                            case 17:
                            case 19:
                                if (jjCanMove_1(hiByte, i1, i2, l1, l2))
                                    jjCheckNAddStates(3, 5);
                                break;
                            case 25:
                                if (!jjCanMove_2(hiByte, i1, i2, l1, l2))
                                    break;
                                if (kind > 21)
                                    kind = 21;
                                jjCheckNAddTwoStates(25, 26);
                                break;
                            case 27:
                                if (!jjCanMove_1(hiByte, i1, i2, l1, l2))
                                    break;
                                if (kind > 21)
                                    kind = 21;
                                jjCheckNAddTwoStates(25, 26);
                                break;
                            case 28:
                                if (!jjCanMove_2(hiByte, i1, i2, l1, l2))
                                    break;
                                if (kind > 21)
                                    kind = 21;
                                jjCheckNAddTwoStates(28, 29);
                                break;
                            case 30:
                                if (!jjCanMove_1(hiByte, i1, i2, l1, l2))
                                    break;
                                if (kind > 21)
                                    kind = 21;
                                jjCheckNAddTwoStates(28, 29);
                                break;
                            case 32:
                                if (!jjCanMove_2(hiByte, i1, i2, l1, l2))
                                    break;
                                if (kind > 23)
                                    kind = 23;
                                jjCheckNAddTwoStates(33, 34);
                                break;
                            case 35:
                                if (!jjCanMove_1(hiByte, i1, i2, l1, l2))
                                    break;
                                if (kind > 23)
                                    kind = 23;
                                jjCheckNAddTwoStates(33, 34);
                                break;
                            case 37:
                                if (jjCanMove_1(hiByte, i1, i2, l1, l2))
                                    jjAddStates(0, 2);
                                break;
                            case 41:
                                if (!jjCanMove_2(hiByte, i1, i2, l1, l2))
                                    break;
                                if (kind > 20)
                                    kind = 20;
                                jjCheckNAddStates(6, 10);
                                break;
                            case 42:
                                if (!jjCanMove_2(hiByte, i1, i2, l1, l2))
                                    break;
                                if (kind > 20)
                                    kind = 20;
                                jjCheckNAddTwoStates(42, 43);
                                break;
                            case 44:
                                if (!jjCanMove_1(hiByte, i1, i2, l1, l2))
                                    break;
                                if (kind > 20)
                                    kind = 20;
                                jjCheckNAddTwoStates(42, 43);
                                break;
                            case 45:
                                if (jjCanMove_2(hiByte, i1, i2, l1, l2))
                                    jjCheckNAddStates(18, 20);
                                break;
                            case 47:
                                if (jjCanMove_1(hiByte, i1, i2, l1, l2))
                                    jjCheckNAddStates(18, 20);
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
                if ((i = jjnewStateCnt) == (startsAt = 49 - (jjnewStateCnt = startsAt)))
                    return curPos;
                try { curChar = input_stream.ReadChar(); }
                catch (IOException e) { return curPos; }
            }
        }

        private int jjMoveStringLiteralDfa0_0()
        {
            return jjMoveNfa_0(0, 0);
        }

        private int jjMoveNfa_0(int startState, int curPos)
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
                if (curChar < 64)
                {
                    long l = 1L << curChar;
                    do
                    {
                        switch (jjstateSet[--i])
                        {
                            case 0:
                                if ((0x3ff000000000000L & l) == 0L)
                                    break;
                                if (kind > 27)
                                    kind = 27;
                                jjAddStates(31, 32);
                                break;
                            case 1:
                                if (curChar == 46)
                                    jjCheckNAdd(2);
                                break;
                            case 2:
                                if ((0x3ff000000000000L & l) == 0L)
                                    break;
                                if (kind > 27)
                                    kind = 27;
                                jjCheckNAdd(2);
                                break;
                            default: break;
                        }
                    } while (i != startsAt);
                }
                else if (curChar < 128)
                {
                    long l = 1L << (curChar & 077);
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
                    int hiByte = (int)(curChar >> 8);
                    int i1 = hiByte >> 6;
                    long l1 = 1L << (hiByte & 077);
                    int i2 = (curChar & 0xff) >> 6;
                    long l2 = 1L << (curChar & 077);
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
                try { curChar = input_stream.ReadChar(); }
                catch (IOException e) { return curPos; }
            }
        }

        private int jjStopStringLiteralDfa_1(int pos, long active0)
        {
            switch (pos)
            {
                case 0:
                    if ((active0 & 0x10000000L) != 0L)
                    {
                        jjmatchedKind = 32;
                        return 6;
                    }
                    return -1;
                default:
                    return -1;
            }
        }

        private int jjStartNfa_1(int pos, long active0)
        {
            return jjMoveNfa_1(jjStopStringLiteralDfa_1(pos, active0), pos + 1);
        }

        private int jjMoveStringLiteralDfa0_1()
        {
            switch ((int)curChar)
            {
                case 84:
                    return jjMoveStringLiteralDfa1_1(0x10000000L);
                case 93:
                    return jjStopAtPos(0, 29);
                case 125:
                    return jjStopAtPos(0, 30);
                default:
                    return jjMoveNfa_1(0, 0);
            }
        }

        private int jjMoveStringLiteralDfa1_1(long active0)
        {
            try { curChar = input_stream.ReadChar(); }
            catch (IOException e)
            {
                jjStopStringLiteralDfa_1(0, active0);
                return 1;
            }
            switch ((int)curChar)
            {
                case 79:
                    if ((active0 & 0x10000000L) != 0L)
                        return jjStartNfaWithStates_1(1, 28, 6);
                    break;
                default:
                    break;
            }
            return jjStartNfa_1(0, active0);
        }

        private int jjStartNfaWithStates_1(int pos, int kind, int state)
        {
            jjmatchedKind = kind;
            jjmatchedPos = pos;
            try { curChar = input_stream.ReadChar(); }
            catch (IOException e) { return pos + 1; }
            return jjMoveNfa_1(state, pos + 1);
        }

        private int jjMoveNfa_1(int startState, int curPos)
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
                if (curChar < 64)
                {
                    long l = 1L << curChar;
                    do
                    {
                        switch (jjstateSet[--i])
                        {
                            case 0:
                                if ((unchecked((long)0xfffffffeffffffffL) & l) != 0L)
                                {
                                    if (kind > 32)
                                        kind = 32;
                                    jjCheckNAdd(6);
                                }
                                if ((0x100002600L & l) != 0L)
                                {
                                    if (kind > 7)
                                        kind = 7;
                                }
                                else if (curChar == 34)
                                    jjCheckNAddTwoStates(2, 4);
                                break;
                            case 1:
                                if (curChar == 34)
                                    jjCheckNAddTwoStates(2, 4);
                                break;
                            case 2:
                                if ((unchecked((long)0xfffffffbffffffffL) & l) != 0L)
                                    jjCheckNAddStates(33, 35);
                                break;
                            case 3:
                                if (curChar == 34)
                                    jjCheckNAddStates(33, 35);
                                break;
                            case 5:
                                if (curChar == 34 && kind > 31)
                                    kind = 31;
                                break;
                            case 6:
                                if ((unchecked((long)0xfffffffeffffffffL) & l) == 0L)
                                    break;
                                if (kind > 32)
                                    kind = 32;
                                jjCheckNAdd(6);
                                break;
                            default: break;
                        }
                    } while (i != startsAt);
                }
                else if (curChar < 128)
                {
                    long l = 1L << (curChar & 077);
                    do
                    {
                        switch (jjstateSet[--i])
                        {
                            case 0:
                            case 6:
                                if ((unchecked((long)0xdfffffffdfffffffL) & l) == 0L)
                                    break;
                                if (kind > 32)
                                    kind = 32;
                                jjCheckNAdd(6);
                                break;
                            case 2:
                                jjAddStates(33, 35);
                                break;
                            case 4:
                                if (curChar == 92)
                                    jjstateSet[jjnewStateCnt++] = 3;
                                break;
                            default: break;
                        }
                    } while (i != startsAt);
                }
                else
                {
                    int hiByte = (int)(curChar >> 8);
                    int i1 = hiByte >> 6;
                    long l1 = 1L << (hiByte & 077);
                    int i2 = (curChar & 0xff) >> 6;
                    long l2 = 1L << (curChar & 077);
                    do
                    {
                        switch (jjstateSet[--i])
                        {
                            case 0:
                                if (jjCanMove_0(hiByte, i1, i2, l1, l2))
                                {
                                    if (kind > 7)
                                        kind = 7;
                                }
                                if (jjCanMove_1(hiByte, i1, i2, l1, l2))
                                {
                                    if (kind > 32)
                                        kind = 32;
                                    jjCheckNAdd(6);
                                }
                                break;
                            case 2:
                                if (jjCanMove_1(hiByte, i1, i2, l1, l2))
                                    jjAddStates(33, 35);
                                break;
                            case 6:
                                if (!jjCanMove_1(hiByte, i1, i2, l1, l2))
                                    break;
                                if (kind > 32)
                                    kind = 32;
                                jjCheckNAdd(6);
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
                if ((i = jjnewStateCnt) == (startsAt = 7 - (jjnewStateCnt = startsAt)))
                    return curPos;
                try { curChar = input_stream.ReadChar(); }
                catch (IOException e) { return curPos; }
            }
        }

        internal static readonly int[] jjnextStates = {
           37, 39, 40, 17, 18, 20, 42, 45, 31, 46, 43, 22, 23, 25, 26, 24, 
           25, 26, 45, 31, 46, 44, 47, 35, 22, 28, 29, 27, 27, 30, 30, 0, 
           1, 2, 4, 5, 
        };

        private static bool jjCanMove_0(int hiByte, int i1, int i2, long l1, long l2)
        {
            switch (hiByte)
            {
                case 48:
                    return ((jjbitVec0[i2] & l2) != 0L);
                default:
                    return false;
            }
        }

        private static bool jjCanMove_1(int hiByte, int i1, int i2, long l1, long l2)
        {
            switch (hiByte)
            {
                case 0:
                    return ((jjbitVec3[i2] & l2) != 0L);
                default:
                    if ((jjbitVec1[i1] & l1) != 0L)
                        return true;
                    return false;
            }
        }

        private static bool jjCanMove_2(int hiByte, int i1, int i2, long l1, long l2)
        {
            switch (hiByte)
            {
                case 0:
                    return ((jjbitVec3[i2] & l2) != 0L);
                case 48:
                    return ((jjbitVec1[i2] & l2) != 0L);
                default:
                    if ((jjbitVec4[i1] & l1) != 0L)
                        return true;
                    return false;
            }
        }

        /** Token literal values. */
        public static readonly String[] jjstrLiteralImages = {
            "", null, null, null, null, null, null, null, null, null, null, "\u002b", "\u002d", 
            null, "\u0028", "\u0029", "\u003a", "\u002a", "\u005e", null, null, null, null, null, null, 
            "\u005b", "\u007b", null, "\u0054\u004f", "\u005d", "\u007d", null, null, };

        /** Lexer state names. */
        public static readonly String[] lexStateNames = {
           "Boost",
           "Range",
           "DEFAULT",
        };

        /** Lex State array. */
        public static readonly int[] jjnewLexState = {
           -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 0, -1, -1, -1, -1, -1, -1, 
           1, 1, 2, -1, 2, 2, -1, -1, 
        };
        static readonly long[] jjtoToken = {
           0x1ffffff01L, 
        };
        static readonly long[] jjtoSkip = {
           0x80L, 
        };

        protected ICharStream input_stream;
        private readonly int[] jjrounds = new int[49];
        private readonly int[] jjstateSet = new int[98];
        protected char curChar;

        /** Constructor. */
        public QueryParserTokenManager(ICharStream stream)
        {
            input_stream = stream;
        }

        /** Constructor. */
        public QueryParserTokenManager(ICharStream stream, int lexState)
            : this(stream)
        {
            SwitchTo(lexState);
        }

        /** Reinitialise parser. */
        public void ReInit(ICharStream stream)
        {
            jjmatchedPos = jjnewStateCnt = 0;
            curLexState = defaultLexState;
            input_stream = stream;
            ReInitRounds();
        }

        private void ReInitRounds()
        {
            int i;
            jjround = unchecked((int)0x80000001);
            for (i = 49; i-- > 0; )
                jjrounds[i] = unchecked((int)0x80000000);
        }

        /** Reinitialise parser. */
        public void ReInit(ICharStream stream, int lexState)
        {
            ReInit(stream);
            SwitchTo(lexState);
        }

        /** Switch to specified lex state. */
        public void SwitchTo(int lexState)
        {
            if (lexState >= 3 || lexState < 0)
                throw new TokenMgrError("Error: Ignoring invalid lexical state : " + lexState + ". State unchanged.", TokenMgrError.INVALID_LEXICAL_STATE);
            else
                curLexState = lexState;
        }

        protected Token jjFillToken()
        {
            Token t;
            String curTokenImage;
            int beginLine;
            int endLine;
            int beginColumn;
            int endColumn;
            String im = jjstrLiteralImages[jjmatchedKind];
            curTokenImage = (im == null) ? input_stream.GetImage() : im;
            beginLine = input_stream.BeginLine;
            beginColumn = input_stream.BeginColumn;
            endLine = input_stream.EndLine;
            endColumn = input_stream.EndColumn;
            t = Token.NewToken(jjmatchedKind, curTokenImage);

            t.beginLine = beginLine;
            t.endLine = endLine;
            t.beginColumn = beginColumn;
            t.endColumn = endColumn;

            return t;
        }

        int curLexState = 2;
        int defaultLexState = 2;
        int jjnewStateCnt;
        int jjround;
        int jjmatchedPos;
        int jjmatchedKind;

        /** Get the next Token. */
        public Token GetNextToken()
        {
            Token matchedToken;
            int curPos = 0;

            for (; ; )
            {
                try
                {
                    curChar = input_stream.BeginToken();
                }
                catch (IOException e)
                {
                    jjmatchedKind = 0;
                    matchedToken = jjFillToken();
                    return matchedToken;
                }

                switch (curLexState)
                {
                    case 0:
                        jjmatchedKind = 0x7fffffff;
                        jjmatchedPos = 0;
                        curPos = jjMoveStringLiteralDfa0_0();
                        break;
                    case 1:
                        jjmatchedKind = 0x7fffffff;
                        jjmatchedPos = 0;
                        curPos = jjMoveStringLiteralDfa0_1();
                        break;
                    case 2:
                        jjmatchedKind = 0x7fffffff;
                        jjmatchedPos = 0;
                        curPos = jjMoveStringLiteralDfa0_2();
                        break;
                }
                if (jjmatchedKind != 0x7fffffff)
                {
                    if (jjmatchedPos + 1 < curPos)
                        input_stream.Backup(curPos - jjmatchedPos - 1);
                    if ((jjtoToken[jjmatchedKind >> 6] & (1L << (jjmatchedKind & 077))) != 0L)
                    {
                        matchedToken = jjFillToken();
                        if (jjnewLexState[jjmatchedKind] != -1)
                            curLexState = jjnewLexState[jjmatchedKind];
                        return matchedToken;
                    }
                    else
                    {
                        if (jjnewLexState[jjmatchedKind] != -1)
                            curLexState = jjnewLexState[jjmatchedKind];
                        continue;
                    }
                }
                int error_line = input_stream.EndLine;
                int error_column = input_stream.EndColumn;
                String error_after = null;
                bool EOFSeen = false;
                try { input_stream.ReadChar(); input_stream.Backup(1); }
                catch (IOException e1)
                {
                    EOFSeen = true;
                    error_after = curPos <= 1 ? "" : input_stream.GetImage();
                    if (curChar == '\n' || curChar == '\r')
                    {
                        error_line++;
                        error_column = 0;
                    }
                    else
                        error_column++;
                }
                if (!EOFSeen)
                {
                    input_stream.Backup(1);
                    error_after = curPos <= 1 ? "" : input_stream.GetImage();
                }
                throw new TokenMgrError(EOFSeen, curLexState, error_line, error_column, error_after, curChar, TokenMgrError.LEXICAL_ERROR);
            }
        }

        private void jjCheckNAdd(int state)
        {
            if (jjrounds[state] != jjround)
            {
                jjstateSet[jjnewStateCnt++] = state;
                jjrounds[state] = jjround;
            }
        }

        private void jjAddStates(int start, int end)
        {
            do
            {
                jjstateSet[jjnewStateCnt++] = jjnextStates[start];
            } while (start++ != end);
        }

        private void jjCheckNAddTwoStates(int state1, int state2)
        {
            jjCheckNAdd(state1);
            jjCheckNAdd(state2);
        }

        private void jjCheckNAddStates(int start, int end)
        {
            do
            {
                jjCheckNAdd(jjnextStates[start]);
            } while (start++ != end);
        }
    }
}
