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
    ///
    /// </summary>
    /// <remarks>
    ///     <para>
    ///      Methods and constants inspired by the article
    ///     "Broadword Implementation of Rank/Select Queries" by Sebastiano Vigna, January 30, 2012 which can be 
    ///     found in the book:
    ///     <see href="http://www.amazon.com/Experimental-Algorithms-International-Provincetown-Proceedings/dp/3540685480">
    ///         Experimental Algorithms: 7th International Workshop, WEA 2008 Provincetown, MA, USA, May 30 - June 1, 2008 Proceedings (Lecture Notes in Computer ... Computer Science and General Issues) 
    ///     </see>
    ///     </para>
    ///     
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
        ///  L8  denotes the constant of 8-byte-counts or 8k.
        ///  _L denotes that the number is an long format. 
        /// </summary>
        private const ulong L8_L = 0x0101010101010101L;

        /// <summary>
        ///  L9  denotes the constant of 8-byte-counts or 9k.
        ///  _L denotes that the number is an long format. 
        /// </summary>
        private const ulong L9_L = 0x8040201008040201L;

        /// <summary>
        ///  L16  denotes the constant of 16-byte-counts or 16k.
        ///  _L denotes that the number is an long format. 
        /// </summary>
        private const ulong L16_L = 0x0001000100010001L;

        /// <summary>
        /// H8 = L8 << (8-1) .
        ///  These contain the high bit of each group of k bits.
        ///  The suffix _L indicates the long implementation.
        /// </summary>
        private static readonly ulong H8_L = L8_L << 7;

        /// H16 = L16 << (16-1) .
        ///  These contain the high bit of each group of k bits.
        ///  The suffix _L indicates the long implementation.
        private static readonly ulong H16_L = L16_L << 15;

        /// <summary>
        /// Bit count of a long.
        /// Only here to compare the implementation with <seealso cref="Select(long,int)"/>,
        /// normally <seealso cref="Lucene.Net.Util.BitUtil.BitCount"/> is preferable. </summary>
        /// <returns> The total number of 1 bits in x. </returns>
        public static int BitCount(long value)
        {
            ulong x = (ulong)value;
            // Step 0 leaves in each pair of bits the number of ones originally contained in that pair:
            x = x - ((x & 0xAAAAAAAAAAAAAAAAL) >> 1);
            // Step 1, idem for each nibble:
            x = (x & 0x3333333333333333L) + ((x >> 2) & 0x3333333333333333L);
            // Step 2, idem for each byte:
            x = (x + (x >> 4)) & 0x0F0F0F0F0F0F0F0FL;
            // Multiply to sum them all into the high byte, and return the high byte:
            return (int)((x * L8_L) >> 56);
        }

       
         /// <summary>
        /// Select a 1-bit from a long. </summary>
        /// <returns> The index of the right most 1 bit in x, or if no such bit exists, 72. </returns>
        public static int Select(long value, int rank)
        {
            
            ulong x = (ulong)value;
            ulong s = x;
            uint r = (uint)rank;
            s =  s - ((s & 0xAAAAAAAAAAAAAAAAL) >> 1); // Step 0, pairwise bitsums

            // Correct a small mistake in algorithm 2:
            // Use s instead of x the second time in right shift 2, compare to Algorithm 1 in rank9 above.
            s =  (s & 0x3333333333333333L) + ((s >> 2) & 0x3333333333333333L); // Step 1, nibblewise bitsums

            s = ((s + (s >> 4)) & 0x0F0F0F0F0F0F0F0FL) * L8_L;; // Step 2, bytewise bitsums

            // second argument of  << must be an int.  
            var b = (ulong)((SmallerUpTo7_8(s, (r * L8_L)) >> 7) * L8_L) >> 53; // & (~7L); // Step 3, side ways addition for byte number times 8

            ulong l = r - (((s << 8) >> (int)b) & 0xFFL); // Step 4, byte wise rank, subtract the rank with byte at b-8, or zero for b=0;
            Debug.Assert(0L <= 1);
            //assert l < 8 : l; //fails when bit r is not available.

            // Select bit l from byte (x >>> b):
            ulong spr = (((x >> (int)b) & 0xFFL) * L8_L) & L9_L; // spread the 8 bits of the byte at b over the long at L9 positions

            // long spr_bigger8_zero = smaller8(0L, spr); // inlined smaller8 with 0L argument:
            // FIXME: replace by biggerequal8_one formula from article page 6, line 9. four operators instead of five here.
            ulong spr_bigger8_zero = ((H8_L - (spr & (~H8_L))) ^ (~spr)) & H8_L;
            s =(spr_bigger8_zero >> 7) * L8_L; // Step 5, sideways byte add the 8 bits towards the high byte

            int res = (int) (b + (((SmallerUpTo7_8(s, (l * L8_L)) >> 7) * L8_L) >> 56)); // Step 6
            return res; 
           
        }

     
        /// <summary>
        /// A signed bytewise smaller &lt;<sub><small>8</small></sub> operator, for operands 0L<= x, y <=0x7L.
        /// this uses the following numbers of basic long operations: 1 or, 2 and, 2 xor, 1 minus, 1 not. </summary>
        /// <returns> A long with bits set in the <seealso cref="#H8_L"/> positions corresponding to each input signed byte pair that compares smaller. </returns>
        private static ulong SmallerUpTo7_8(ulong x, ulong y)
        {
            // See section 4, page 5, line 14 of the Vigna article:
            return (((x | H8_L) - (y & (~H8_L))) ^ x ^ ~y) & H8_L;
        }

     
       



        /// <summary>
        /// Naive implementation of <seealso cref="#select(long,int)"/>, using <seealso cref="Long#numberOfTrailingZeros"/> repetitively.
        /// Works relatively fast for low ranks. </summary>
        /// <returns> The index of the r-th 1 bit in x, or if no such bit exists, 72. </returns>
        public static int SelectNaive(long x, int r)
        {
            Debug.Assert(r >= 1);
            int s = -1;
            while ((x != 0L) && (r > 0))
            {
                int ntz = x.NumberOfTrailingZeros();
                x = (long)((ulong)x >> (ntz + 1));
                s += (ntz + 1);
                r -= 1;
            }
            int res = (r > 0) ? 72 : s;
            return res;
        }
    }
}