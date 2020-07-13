using J2N.Numerics;
using NUnit.Framework;
using Assert = Lucene.Net.TestFramework.Assert;

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

    [TestFixture]
    public class TestBroadWord : LuceneTestCase
    {
        private void TstRank(long x)
        {
            Assert.AreEqual(x.PopCount(), BroadWord.BitCount(x), "rank(" + x + ")");
        }

        [Test]
        public virtual void TestRank1()
        {
            TstRank(0L);
            TstRank(1L);
            TstRank(3L);
            TstRank(0x100L);
            TstRank(0x300L);
            TstRank(unchecked((long)0x8000000000000001L));
        }

        private void TstSelect(long x, int r, int exp)
        {
            Assert.AreEqual(exp, BroadWord.SelectNaive(x, r), "selectNaive(" + x + "," + r + ")");
            Assert.AreEqual(exp, BroadWord.Select(x, r), "select(" + x + "," + r + ")");
        }

        [Test]
        public virtual void TestSelectFromZero()
        {
            TstSelect(0L, 1, 72);
        }

        [Test]
        public virtual void TestSelectSingleBit()
        {
            for (int i = 0; i < 64; i++)
            {
                TstSelect((1L << i), 1, i);
            }
        }

        [Test]
        public virtual void TestSelectTwoBits()
        {
            for (int i = 0; i < 64; i++)
            {
                for (int j = i + 1; j < 64; j++)
                {
                    long x = (1L << i) | (1L << j);
                    //System.out.println(getName() + " i: " + i + " j: " + j);
                    TstSelect(x, 1, i);
                    TstSelect(x, 2, j);
                    TstSelect(x, 3, 72);
                }
            }
        }

        [Test]
        public virtual void TestSelectThreeBits()
        {
            for (int i = 0; i < 64; i++)
            {
                for (int j = i + 1; j < 64; j++)
                {
                    for (int k = j + 1; k < 64; k++)
                    {
                        long x = (1L << i) | (1L << j) | (1L << k);
                        TstSelect(x, 1, i);
                        TstSelect(x, 2, j);
                        TstSelect(x, 3, k);
                        TstSelect(x, 4, 72);
                    }
                }
            }
        }

        [Test]
        public virtual void TestSelectAllBits()
        {
            for (int i = 0; i < 64; i++)
            {
                TstSelect(unchecked((long)0xFFFFFFFFFFFFFFFFL), i + 1, i);
            }
        }

        [Test]
        public virtual void TestPerfSelectAllBitsBroad()
        {
            for (int j = 0; j < 100000; j++) // 1000000 for real perf test
            {
                for (int i = 0; i < 64; i++)
                {
                    Assert.AreEqual(i, BroadWord.Select(unchecked((long)0xFFFFFFFFFFFFFFFFL), i + 1));
                }
            }
        }

        [Test]
        public virtual void TestPerfSelectAllBitsNaive()
        {
            for (int j = 0; j < 10000; j++) // real perftest: 1000000
            {
                for (int i = 0; i < 64; i++)
                {
                    Assert.AreEqual(i, BroadWord.SelectNaive(unchecked((long)0xFFFFFFFFFFFFFFFFL), i + 1));
                }
            }
        }

        [Test]
        public virtual void TestSmalleru_87_01()
        {
            // 0 <= arguments < 2 ** (k-1), k=8, see paper
            for (long i = 0x0L; i <= 0x7FL; i++)
            {
                for (long j = 0x0L; i <= 0x7FL; i++)
                {
                    long ii = i * BroadWord.L8_L;
                    long jj = j * BroadWord.L8_L;
                    Assert.AreEqual(ToStringUtils.Int64Hex((i < j) ? unchecked(0x80L * BroadWord.L8_L) : 0x0L), ToStringUtils.Int64Hex(BroadWord.SmallerUpTo7_8(ii, jj)), ToStringUtils.Int64Hex(ii) + " < " + ToStringUtils.Int64Hex(jj));
                }
            }
        }

        [Test]
        public virtual void TestSmalleru_8_01()
        {
            // 0 <= arguments < 2 ** k, k=8, see paper
            for (long i = 0x0L; i <= 0xFFL; i++)
            {
                for (long j = 0x0L; i <= 0xFFL; i++)
                {
                    long ii = i * BroadWord.L8_L;
                    long jj = j * BroadWord.L8_L;
                    Assert.AreEqual(ToStringUtils.Int64Hex((i < j) ? unchecked(0x80L * BroadWord.L8_L) : 0x0L), ToStringUtils.Int64Hex(BroadWord.Smalleru_8(ii, jj)), ToStringUtils.Int64Hex(ii) + " < " + ToStringUtils.Int64Hex(jj));
                }
            }
        }

        [Test]
        public virtual void TestNotEquals0_8()
        {
            // 0 <= arguments < 2 ** k, k=8, see paper
            for (long i = 0x0L; i <= 0xFFL; i++)
            {
                long ii = i * BroadWord.L8_L;
                Assert.AreEqual(ToStringUtils.Int64Hex((i != 0L) ? unchecked(0x80L * BroadWord.L8_L) : 0x0L), ToStringUtils.Int64Hex(BroadWord.NotEquals0_8(ii)), ToStringUtils.Int64Hex(ii) + " <> 0");
            }
        }
    }
}