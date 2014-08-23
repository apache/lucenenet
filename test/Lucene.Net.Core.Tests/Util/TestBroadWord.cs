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
    using System;
    using Lucene.Net.Support;


    public class TestBroadWord : LuceneTestCase
    {
  
        private const long delta = unchecked((long)0xFFFFFFFFFFFFFFFFL);

        [Test]
        public void TestRank()
        {
            AssertRank(0L);
            AssertRank(1L);
            AssertRank(3L);
            AssertRank(0x100L);
            AssertRank(0x300L);
            AssertRank(unchecked((long)0x8000000000000001L));
        }

        [Test]
        public void TestSelectFromZero()
        {
            AssertSelect(72, 0L, 1);
        }

        [Test]
        public void TestSelectSingleBit()
        {
            for(var i = 0; i < 64; i++)
            {
                AssertSelect(i, (1L << i), 1);
            }
        }

        [Test]
        public void TestSelectTwoBits()
        {
            for (int i = 0; i < 64; i++)
            {
                for (int j = i + 1; j < 64; j++)
                {
                    long x = (1L << i) | (1L << j);
                    //System.out.println(getName() + " i: " + i + " j: " + j);
                    AssertSelect(i, x, 1);
                    AssertSelect(j, x, 2);
                    AssertSelect(72, x, 3);
                }
            }
        }

        [Test]
        public void TestSelectThreeBits()
        {
            for (int i = 0; i < 64; i++)
            {
                for (int j = i + 1; j < 64; j++)
                {
                    for (int k = j + 1; k < 64; k++)
                    {
                        long x = (1L << i) | (1L << j) | (1L << k);
                        AssertSelect(i, x, 1);
                        AssertSelect(j, x, 2);
                        AssertSelect(k, x, 3);
                        AssertSelect(72, x, 4);
                    }
                }
            }
        }

        [Test]
        public void TestSelectAllBits()
        {
            for (int i = 0; i < 64; i++)
            {
                var l = delta;
                AssertSelect(i, l, i + 1);
            }
        }

        [Test]
        [Performance] // TODO: implement a real performance test.
        public void TestPerfSelectAllBitsBroad()
        {
            var length = AtLeast(5000);
            for (int j = 0; j < length; j++)
            { // 1000000 for real perf test
                for (int i = 0; i < 64; i++)
                {
                    var l = delta;
                    Equal(i, BroadWord.Select(l, i + 1));
                }
            }
        }

        [Test]
        [Performance]
        public void TestPerfSelectAllBitsNaive()
        {
            var length = AtLeast(5000);
            for (int j = 0; j < length; j++)
            { // real perftest: 1000000
                for (int i = 0; i < 64; i++)
                {
                    var l = delta;
                    Equal(i, BroadWord.SelectNaive(l, i + 1));
                }
            }
        }

        private void AssertSelect(int expected, long value, int rank)
        { 
            var actual = BroadWord.Select(value, rank);
            Ok(expected == actual, "Expected {0} does not equal {3} = Select({1}, {2})", expected, value, rank, actual);
        }

        private void AssertRank(long value)
        {
            Equal(BitUtil.BitCount(value), BroadWord.BitCount(value));
        }

        #region these methods exist, but are not used in the Lucene Code Base

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
        /// An unsigned bytewise smaller &lt;<sub><small>8</small></sub> operator.
        /// this uses the following numbers of basic long operations: 3 or, 2 and, 2 xor, 1 minus, 1 not. </summary>
        /// <returns> A long with bits set in the <seealso cref="#H8_L"/> positions corresponding to each input unsigned byte pair that compares smaller. </returns>
        private static ulong Smalleru_8(ulong x, ulong y)
        {
            // See section 4, 8th line from the bottom of the page 5, of the Vigna article:
            return ((((x | H8_L) - (y & ~H8_L)) | x ^ y) ^ (x | ~y)) & H8_L;
        }

        /// <summary>
        /// An unsigned bytewise not equals 0 operator.
        /// this uses the following numbers of basic long operations: 2 or, 1 and, 1 minus. </summary>
        /// <returns> A long with bits set in the <seealso cref="#H8_L"/> positions corresponding to each unsigned byte that does not equal 0. </returns>
        private static ulong NotEquals0_8(ulong x)
        {
            // See section 4, line 6-8 on page 6, of the Vigna article:
            return (((x | H8_L) - L8_L) | x) & H8_L;
        }

        /// <summary>
        /// A bytewise smaller &lt;<sub><small>16</small></sub> operator.
        /// this uses the following numbers of basic long operations: 1 or, 2 and, 2 xor, 1 minus, 1 not. </summary>
        /// <returns> A long with bits set in the <seealso cref="#H16_L"/> positions corresponding to each input signed short pair that compares smaller. </returns>
        private static ulong SmallerUpto15_16(ulong x, ulong y)
        {
            return (((x | H16_L) - (y & (~H16_L))) ^ x ^ ~y) & H16_L;
        }
        #endregion
    }
}
