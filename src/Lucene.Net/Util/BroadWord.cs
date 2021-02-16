using J2N.Numerics;
using Lucene.Net.Diagnostics;
using System.Runtime.CompilerServices;

namespace Lucene.Net.Util
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
    /// Methods and constants inspired by the article
    /// "Broadword Implementation of Rank/Select Queries" by Sebastiano Vigna, January 30, 2012:
    /// <list type="bullet">
    ///     <item><description>algorithm 1: <see cref="BitCount(long)"/>, count of set bits in a <see cref="long"/></description></item>
    ///     <item><description>algorithm 2: <see cref="Select(long, int)"/>, selection of a set bit in a <see cref="long"/>,</description></item>
    ///     <item><description>bytewise signed smaller &lt;<sub><small>8</small></sub> operator: <see cref="SmallerUpTo7_8(long,long)"/>.</description></item>
    ///     <item><description>shortwise signed smaller &lt;<sub><small>16</small></sub> operator: <see cref="SmallerUpto15_16(long,long)"/>.</description></item>
    ///     <item><description>some of the Lk and Hk constants that are used by the above:
    ///         L8 <see cref="L8_L"/>, H8 <see cref="H8_L"/>, L9 <see cref="L9_L"/>, L16 <see cref="L16_L"/>and H16 <see cref="H8_L"/>.</description></item>
    /// </list>
    /// @lucene.internal
    /// </summary>
    public static class BroadWord // LUCENENET specific - made static
    {
        // TBD: test smaller8 and smaller16 separately.

        /// <summary>
        /// Bit count of a <see cref="long"/>.
        /// Only here to compare the implementation with <see cref="Select(long, int)"/>,
        /// normally <see cref="BitOperation.PopCount(long)"/> is preferable. </summary>
        /// <returns> The total number of 1 bits in x. </returns>
        internal static int BitCount(long x)
        {
            // Step 0 leaves in each pair of bits the number of ones originally contained in that pair:
            x = x - ((x & unchecked((long)0xAAAAAAAAAAAAAAAAL)).TripleShift(1));
            // Step 1, idem for each nibble:
            x = (x & 0x3333333333333333L) + ((x.TripleShift(2)) & 0x3333333333333333L);
            // Step 2, idem for each byte:
            x = (x + (x.TripleShift(4))) & 0x0F0F0F0F0F0F0F0FL;
            // Multiply to sum them all into the high byte, and return the high byte:
            return (int)((x * L8_L).TripleShift(56));
        }

        /// <summary>
        /// Select a 1-bit from a <see cref="long"/>. </summary>
        /// <returns> The index of the r-th 1 bit in x, or if no such bit exists, 72. </returns>
        public static int Select(long x, int r)
        {
            long s = x - ((x & unchecked((long)0xAAAAAAAAAAAAAAAAL)).TripleShift(1)); // Step 0, pairwise bitsums

            // Correct a small mistake in algorithm 2:
            // Use s instead of x the second time in right shift 2, compare to Algorithm 1 in rank9 above.
            s = (s & 0x3333333333333333L) + ((s.TripleShift(2)) & 0x3333333333333333L); // Step 1, nibblewise bitsums

            s = ((s + (s.TripleShift(4))) & 0x0F0F0F0F0F0F0F0FL) * L8_L; // Step 2, bytewise bitsums

            long b = ((SmallerUpTo7_8(s, (r * L8_L)).TripleShift(7)) * L8_L).TripleShift(53); // & (~7L); // Step 3, side ways addition for byte number times 8

            long l = r - (((s << 8).TripleShift((int)b)) & 0xFFL); // Step 4, byte wise rank, subtract the rank with byte at b-8, or zero for b=0;
            if (Debugging.AssertsEnabled) Debugging.Assert(0L <= 1, "{0}", l);
            //assert l < 8 : l; //fails when bit r is not available.

            // Select bit l from byte (x >>> b):
            long spr = (((x.TripleShift((int)b)) & 0xFFL) * L8_L) & L9_L; // spread the 8 bits of the byte at b over the long at L9 positions

            // long spr_bigger8_zero = smaller8(0L, spr); // inlined smaller8 with 0L argument:
            // FIXME: replace by biggerequal8_one formula from article page 6, line 9. four operators instead of five here.
            long spr_bigger8_zero = ((H8_L - (spr & (~H8_L))) ^ (~spr)) & H8_L;
            s = (spr_bigger8_zero.TripleShift(7)) * L8_L; // Step 5, sideways byte add the 8 bits towards the high byte

            int res = (int)(b + (((SmallerUpTo7_8(s, (l * L8_L)).TripleShift(7)) * L8_L).TripleShift(56))); // Step 6
            return res;
        }

        /// <summary>
        /// A signed bytewise smaller &lt;<sub><small>8</small></sub> operator, for operands 0L&lt;= x, y &lt;=0x7L.
        /// This uses the following numbers of basic <see cref="long"/> operations: 1 or, 2 and, 2 xor, 1 minus, 1 not. </summary>
        /// <returns> A <see cref="long"/> with bits set in the <see cref="H8_L"/> positions corresponding to each input signed byte pair that compares smaller. </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long SmallerUpTo7_8(long x, long y)
        {
            // See section 4, page 5, line 14 of the Vigna article:
            return (((x | H8_L) - (y & (~H8_L))) ^ x ^ ~y) & H8_L;
        }

        /// <summary>
        /// An unsigned bytewise smaller &lt;<sub><small>8</small></sub> operator.
        /// This uses the following numbers of basic <see cref="long"/> operations: 3 or, 2 and, 2 xor, 1 minus, 1 not. </summary>
        /// <returns> A <see cref="long"/> with bits set in the <see cref="H8_L"/> positions corresponding to each input unsigned byte pair that compares smaller. </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long Smalleru_8(long x, long y)
        {
            // See section 4, 8th line from the bottom of the page 5, of the Vigna article:
            return ((((x | H8_L) - (y & ~H8_L)) | x ^ y) ^ (x | ~y)) & H8_L;
        }

        /// <summary>
        /// An unsigned bytewise not equals 0 operator.
        /// This uses the following numbers of basic <see cref="long"/> operations: 2 or, 1 and, 1 minus. </summary>
        /// <returns> A <see cref="long"/> with bits set in the <see cref="H8_L"/> positions corresponding to each unsigned byte that does not equal 0. </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long NotEquals0_8(long x)
        {
            // See section 4, line 6-8 on page 6, of the Vigna article:
            return (((x | H8_L) - L8_L) | x) & H8_L;
        }

        /// <summary>
        /// A bytewise smaller &lt;<sub><small>16</small></sub> operator.
        /// This uses the following numbers of basic <see cref="long"/> operations: 1 or, 2 and, 2 xor, 1 minus, 1 not. </summary>
        /// <returns> A <see cref="long"/> with bits set in the <see cref="H16_L"/> positions corresponding to each input signed short pair that compares smaller. </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long SmallerUpto15_16(long x, long y)
        {
            return (((x | H16_L) - (y & (~H16_L))) ^ x ^ ~y) & H16_L;
        }

        /// <summary>
        /// Lk denotes the constant whose ones are in position 0, k, 2k, . . .
        /// These contain the low bit of each group of k bits.
        /// The suffix _L indicates the <see cref="long"/> implementation.
        /// </summary>
        public const long L8_L = 0x0101010101010101L;

        public const long L9_L = unchecked((long)0x8040201008040201L);
        public const long L16_L = 0x0001000100010001L;

        /// <summary>
        /// Hk = Lk &lt;&lt; (k-1) .
        /// These contain the high bit of each group of k bits.
        /// The suffix _L indicates the <see cref="long"/> implementation.
        /// </summary>
        public static readonly long H8_L = L8_L << 7;

        public static readonly long H16_L = L16_L << 15;

        /// <summary>
        /// Naive implementation of <see cref="Select(long, int)"/>, using <see cref="BitOperation.TrailingZeroCount(long)"/> repetitively.
        /// Works relatively fast for low ranks. </summary>
        /// <returns> The index of the r-th 1 bit in x, or if no such bit exists, 72. </returns>
        public static int SelectNaive(long x, int r)
        {
            if (Debugging.AssertsEnabled) Debugging.Assert(r >= 1);
            int s = -1;
            while ((x != 0L) && (r > 0))
            {
                int ntz = x.TrailingZeroCount();
                x = x.TripleShift(ntz + 1);
                s += (ntz + 1);
                r -= 1;
            }
            int res = (r > 0) ? 72 : s;
            return res;
        }
    }
}