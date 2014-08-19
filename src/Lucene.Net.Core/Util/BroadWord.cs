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

namespace Lucene.Net.Util
{
    using System.Diagnostics;
    using Lucene.Net.Support;

    /// <summary>
    ///     An implementation of BroadWord for rank/select queries.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Methods and constants inspired by the article
    ///         "BroadWord Implementation of Rank/Select Queries" by Sebastiano Vigna, January 30, 2012 which can be
    ///         found in the book:
    ///         <see href="http://www.amazon.com/Experimental-Algorithms-International-Provincetown-Proceedings/dp/3540685480">
    ///             Experimental Algorithms: 7th International Workshop, WEA 2008 Provincetown, MA, USA, May 30 - June 1, 2008
    ///             Proceedings (Lecture Notes in Computer ... Computer Science and General Issues)
    ///         </see>
    ///     </para>
    /// </remarks>
    public static class BroadWord
    {
        // NOTES:
        // DO NOT PORT: smalleru_8, notEquals0_8, smallerUpto15_16, 
        // smalleru8 & notEquals0_8 are used for testing, smallerUpto15_16 is not used at all.

        // DO NOT MAKE CONSTS PUBLIC. ulongs are not CLSCompliant.
        // The constants in the Java Lucene version are only used in the test case. 
        // Thus, there is not a valid reason to make them public.  

        /// <summary>
        ///     The constant for the lowest 8 bit subword.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         This would be the L<sub>8</sub> expression from the paper
        ///         that notates this is the lowest 8 bit subword constant.
        ///     </para>
        /// </remarks>
        private const ulong Lowest8BitSubword = 0x0101010101010101L;

        /// <summary>
        ///     The constant for the lowest 9 bit subword.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         This would be the L<sub>9</sub> expression from the paper
        ///         that notates this is the lowest 9 bit subword constant.
        ///     </para>
        /// </remarks>
        private const ulong Lowest9BitSubword = 0x8040201008040201L;

        /// <summary>
        ///     The constant for the highest 8 bit subword.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         This would be the H<sub>8</sub> expression from the paper
        ///         that notates this is the lowest 8 bit subword constant.
        ///     </para>
        /// </remarks>
        private const ulong Highest8BitSubword = Lowest8BitSubword << 7;

        /* unused
        /// <summary>
        /// The constant for the highest 16 bit subword.  
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         This would be the L<sub>16</sub> expression from the paper
        ///         that notates this is the lowest 16 bit subword constant.  
        ///     </para>
        /// </remarks>
        private static readonly ulong H16_L = L16_L << 15; */

        /// <summary>
        ///     Bit count of a long.
        ///     Only here to compare the implementation with <seealso cref="Select(long,int)" />,
        ///     normally <seealso cref="Lucene.Net.Util.BitUtil.BitCount(long)" /> is preferable.
        /// </summary>
        /// <returns> The total number of 1 bits in x. </returns>
        public static int BitCount(long value)
        {
            var x = (ulong) value;
            // Step 0 leaves in each pair of bits the number of ones originally contained in that pair:
            x = x - ((x & 0xAAAAAAAAAAAAAAAAL) >> 1);
            // Step 1, idem for each nibble:
            x = (x & 0x3333333333333333L) + ((x >> 2) & 0x3333333333333333L);
            // Step 2, idem for each byte:
            x = (x + (x >> 4)) & 0x0F0F0F0F0F0F0F0FL;
            // Multiply to sum them all into the high byte, and return the high byte:
            return (int) ((x*Lowest8BitSubword) >> 56);
        }


        /// <summary>
        ///     Select a 1-bit from a long.
        /// </summary>
        /// <returns> The index of the right most 1 bit in x, or if no such bit exists, 72. </returns>
        public static int Select(long value, int rank)
        {
            var x = (ulong) value;
            var s = x;
            var r = (uint) rank;
            s = s - ((s & 0xAAAAAAAAAAAAAAAAL) >> 1); // Step 0, pairwise bitsums

            // Correct a small mistake in algorithm 2:
            // Use s instead of x the second time in right shift 2, compare to Algorithm 1 in rank9 above.
            s = (s & 0x3333333333333333L) + ((s >> 2) & 0x3333333333333333L); // Step 1, nibblewise bitsums

            s = ((s + (s >> 4)) & 0x0F0F0F0F0F0F0F0FL)*Lowest8BitSubword;
            // Step 2, bytewise bitsums

            // second argument of  << must be an int.  
            var b = ((SmallerUpTo7_8(s, (r*Lowest8BitSubword)) >> 7)*Lowest8BitSubword) >> 53;
            // & (~7L); // Step 3, side ways addition for byte number times 8

            var l = r - (((s << 8) >> (int) b) & 0xFFL);
            // Step 4, byte wise rank, subtract the rank with byte at b-8, or zero for b=0;
            Debug.Assert(0L <= 1);
            //assert l < 8 : l; //fails when bit r is not available.

            // Select bit l from byte (x >>> b):
            var spr = (((x >> (int) b) & 0xFFL)*Lowest8BitSubword) & Lowest9BitSubword;
            // spread the 8 bits of the byte at b over the long at L9 positions

            // long spr_bigger8_zero = smaller8(0L, spr); // inlined smaller8 with 0L argument:
            // FIXME: replace by biggerequal8_one formula from article page 6, line 9. four operators instead of five here.
            var sprBigger8Zero = ((Highest8BitSubword - (spr & (~Highest8BitSubword))) ^ (~spr)) &
                                   Highest8BitSubword;
            s = (sprBigger8Zero >> 7)*Lowest8BitSubword; // Step 5, sideways byte add the 8 bits towards the high byte

            var res = (int) (b + (((SmallerUpTo7_8(s, (l*Lowest8BitSubword)) >> 7)*Lowest8BitSubword) >> 56)); // Step 6
            return res;
        }


        /// <summary>
        ///     A signed byte wise smaller &lt;
        ///     <sub>
        ///         <small>8</small>
        ///     </sub>
        ///     operator, for operands 0L &lt;= x, y &lt;=0x7L.
        ///     this uses the following numbers of basic long operations: 1 or, 2 and, 2 xor, 1 minus, 1 not.
        /// </summary>
        /// <returns>
        ///     A long with bits set in the <seealso cref="Highest8BitSubword" /> positions corresponding to each input signed byte pair
        ///     that compares smaller.
        /// </returns>
        private static ulong SmallerUpTo7_8(ulong x, ulong y)
        {
            // See section 4, page 5, line 14 of the Vigna article:
            return (((x | Highest8BitSubword) - (y & (~Highest8BitSubword))) ^ x ^ ~y) & Highest8BitSubword;
        }


        /// <summary>
        ///     Naive implementation of <seealso cref="Select(long,int)" />.
        ///     Works relatively fast for low ranks.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         Internally uses <see cref="Lucene.Net.Support.NumberExtensionMethods.NumberOfTrailingZeros(long)" /> repetitively.
        ///     </para>
        /// </remarks>
        /// <returns> The index of the r-th 1 bit in x, or if no such bit exists, 72. </returns>
        public static int SelectNaive(long x, int r)
        {
            Debug.Assert(r >= 1);
            var s = -1;
            while ((x != 0L) && (r > 0))
            {
                var ntz = x.NumberOfTrailingZeros();
                x = (long) ((ulong) x >> (ntz + 1));
                s += (ntz + 1);
                r -= 1;
            }
            var res = (r > 0) ? 72 : s;
            return res;
        }
    }
}