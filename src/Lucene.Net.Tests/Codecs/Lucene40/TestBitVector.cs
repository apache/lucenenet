using NUnit.Framework;
using Assert = Lucene.Net.TestFramework.Assert;

namespace Lucene.Net.Codecs.Lucene40
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

    using Directory = Lucene.Net.Store.Directory;
    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
    using MockDirectoryWrapper = Lucene.Net.Store.MockDirectoryWrapper;
    using RAMDirectory = Lucene.Net.Store.RAMDirectory;
    using TestUtil = Lucene.Net.Util.TestUtil;

    /// <summary>
    /// <code>TestBitVector</code> tests the <code>BitVector</code>, obviously.
    /// </summary>
    [TestFixture]
    public class TestBitVector : LuceneTestCase
    {
        /// <summary>
        /// Test the default constructor on BitVectors of various sizes.
        /// </summary>
        [Test]
        public virtual void TestConstructSize()
        {
            DoTestConstructOfSize(8);
            DoTestConstructOfSize(20);
            DoTestConstructOfSize(100);
            DoTestConstructOfSize(1000);
        }

        private void DoTestConstructOfSize(int n)
        {
            BitVector bv = new BitVector(n);
            Assert.AreEqual(n, bv.Length); // LUCENENET NOTE: Length is the equivalent of size()
        }

        /// <summary>
        /// Test the get() and set() methods on BitVectors of various sizes.
        /// </summary>
        [Test]
        public virtual void TestGetSet()
        {
            DoTestGetSetVectorOfSize(8);
            DoTestGetSetVectorOfSize(20);
            DoTestGetSetVectorOfSize(100);
            DoTestGetSetVectorOfSize(1000);
        }

        private void DoTestGetSetVectorOfSize(int n)
        {
            BitVector bv = new BitVector(n);
            for (int i = 0; i < bv.Length; i++) // LUCENENET NOTE: Length is the equivalent of size()
            {
                // ensure a set bit can be git'
                Assert.IsFalse(bv.Get(i));
                bv.Set(i);
                Assert.IsTrue(bv.Get(i));
            }
        }

        /// <summary>
        /// Test the clear() method on BitVectors of various sizes.
        /// </summary>
        [Test]
        public virtual void TestClear()
        {
            DoTestClearVectorOfSize(8);
            DoTestClearVectorOfSize(20);
            DoTestClearVectorOfSize(100);
            DoTestClearVectorOfSize(1000);
        }

        private void DoTestClearVectorOfSize(int n)
        {
            BitVector bv = new BitVector(n);
            for (int i = 0; i < bv.Length; i++) // LUCENENET NOTE: Length is the equivalent of size()
            {
                // ensure a set bit is cleared
                Assert.IsFalse(bv.Get(i));
                bv.Set(i);
                Assert.IsTrue(bv.Get(i));
                bv.Clear(i);
                Assert.IsFalse(bv.Get(i));
            }
        }

        /// <summary>
        /// Test the count() method on BitVectors of various sizes.
        /// </summary>
        [Test]
        public virtual void TestCount()
        {
            DoTestCountVectorOfSize(8);
            DoTestCountVectorOfSize(20);
            DoTestCountVectorOfSize(100);
            DoTestCountVectorOfSize(1000);
        }

        private void DoTestCountVectorOfSize(int n)
        {
            BitVector bv = new BitVector(n);
            // test count when incrementally setting bits
            for (int i = 0; i < bv.Length; i++) // LUCENENET NOTE: Length is the equivalent of size()
            {
                Assert.IsFalse(bv.Get(i));
                Assert.AreEqual(i, bv.Count());
                bv.Set(i);
                Assert.IsTrue(bv.Get(i));
                Assert.AreEqual(i + 1, bv.Count());
            }

            bv = new BitVector(n);
            // test count when setting then clearing bits
            for (int i = 0; i < bv.Length; i++) // LUCENENET NOTE: Length is the equivalent of size()
            {
                Assert.IsFalse(bv.Get(i));
                Assert.AreEqual(0, bv.Count());
                bv.Set(i);
                Assert.IsTrue(bv.Get(i));
                Assert.AreEqual(1, bv.Count());
                bv.Clear(i);
                Assert.IsFalse(bv.Get(i));
                Assert.AreEqual(0, bv.Count());
            }
        }

        /// <summary>
        /// Test writing and construction to/from Directory.
        /// </summary>
        [Test]
        public virtual void TestWriteRead()
        {
            DoTestWriteRead(8);
            DoTestWriteRead(20);
            DoTestWriteRead(100);
            DoTestWriteRead(1000);
        }

        private void DoTestWriteRead(int n)
        {
            MockDirectoryWrapper d = new MockDirectoryWrapper(Random, new RAMDirectory());
            d.PreventDoubleWrite = false;
            BitVector bv = new BitVector(n);
            // test count when incrementally setting bits
            for (int i = 0; i < bv.Length; i++) // LUCENENET NOTE: Length is the equivalent of size()
            {
                Assert.IsFalse(bv.Get(i));
                Assert.AreEqual(i, bv.Count());
                bv.Set(i);
                Assert.IsTrue(bv.Get(i));
                Assert.AreEqual(i + 1, bv.Count());
                bv.Write(d, "TESTBV", NewIOContext(Random));
                BitVector compare = new BitVector(d, "TESTBV", NewIOContext(Random));
                // compare bit vectors with bits set incrementally
                Assert.IsTrue(DoCompare(bv, compare));
            }
        }

        /// <summary>
        /// Test r/w when size/count cause switching between bit-set and d-gaps file formats.
        /// </summary>
        [Test]
        public virtual void TestDgaps()
        {
            DoTestDgaps(1, 0, 1);
            DoTestDgaps(10, 0, 1);
            DoTestDgaps(100, 0, 1);
            DoTestDgaps(1000, 4, 7);
            DoTestDgaps(10000, 40, 43);
            DoTestDgaps(100000, 415, 418);
            DoTestDgaps(1000000, 3123, 3126);
            // now exercise skipping of fully populated byte in the bitset (they are omitted if bitset is sparse)
            MockDirectoryWrapper d = new MockDirectoryWrapper(Random, new RAMDirectory());
            d.PreventDoubleWrite = false;
            BitVector bv = new BitVector(10000);
            bv.Set(0);
            for (int i = 8; i < 16; i++)
            {
                bv.Set(i);
            } // make sure we have once byte full of set bits
            for (int i = 32; i < 40; i++)
            {
                bv.Set(i);
            } // get a second byte full of set bits
            // add some more bits here
            for (int i = 40; i < 10000; i++)
            {
                if (Random.Next(1000) == 0)
                {
                    bv.Set(i);
                }
            }
            bv.Write(d, "TESTBV", NewIOContext(Random));
            BitVector compare = new BitVector(d, "TESTBV", NewIOContext(Random));
            Assert.IsTrue(DoCompare(bv, compare));
        }

        private void DoTestDgaps(int size, int count1, int count2)
        {
            MockDirectoryWrapper d = new MockDirectoryWrapper(Random, new RAMDirectory());
            d.PreventDoubleWrite = false;
            BitVector bv = new BitVector(size);
            bv.InvertAll();
            for (int i = 0; i < count1; i++)
            {
                bv.Clear(i);
                Assert.AreEqual(i + 1, size - bv.Count());
            }
            bv.Write(d, "TESTBV", NewIOContext(Random));
            // gradually increase number of set bits
            for (int i = count1; i < count2; i++)
            {
                BitVector bv2 = new BitVector(d, "TESTBV", NewIOContext(Random));
                Assert.IsTrue(DoCompare(bv, bv2));
                bv = bv2;
                bv.Clear(i);
                Assert.AreEqual(i + 1, size - bv.Count());
                bv.Write(d, "TESTBV", NewIOContext(Random));
            }
            // now start decreasing number of set bits
            for (int i = count2 - 1; i >= count1; i--)
            {
                BitVector bv2 = new BitVector(d, "TESTBV", NewIOContext(Random));
                Assert.IsTrue(DoCompare(bv, bv2));
                bv = bv2;
                bv.Set(i);
                Assert.AreEqual(i, size - bv.Count());
                bv.Write(d, "TESTBV", NewIOContext(Random));
            }
        }

        [Test]
        public virtual void TestSparseWrite()
        {
            Directory d = NewDirectory();
            const int numBits = 10240;
            BitVector bv = new BitVector(numBits);
            bv.InvertAll();
            int numToClear = Random.Next(5);
            for (int i = 0; i < numToClear; i++)
            {
                bv.Clear(Random.Next(numBits));
            }
            bv.Write(d, "test", NewIOContext(Random));
            long size = d.FileLength("test");
            Assert.IsTrue(size < 100, "size=" + size);
            d.Dispose();
        }

        [Test]
        public virtual void TestClearedBitNearEnd()
        {
            Directory d = NewDirectory();
            int numBits = TestUtil.NextInt32(Random, 7, 1000);
            BitVector bv = new BitVector(numBits);
            bv.InvertAll();
            bv.Clear(numBits - TestUtil.NextInt32(Random, 1, 7));
            bv.Write(d, "test", NewIOContext(Random));
            Assert.AreEqual(numBits - 1, bv.Count());
            d.Dispose();
        }

        [Test]
        public virtual void TestMostlySet()
        {
            Directory d = NewDirectory();
            int numBits = TestUtil.NextInt32(Random, 30, 1000);
            for (int numClear = 0; numClear < 20; numClear++)
            {
                BitVector bv = new BitVector(numBits);
                bv.InvertAll();
                int count = 0;
                while (count < numClear)
                {
                    int bit = Random.Next(numBits);
                    // Don't use getAndClear, so that count is recomputed
                    if (bv.Get(bit))
                    {
                        bv.Clear(bit);
                        count++;
                        Assert.AreEqual(numBits - count, bv.Count());
                    }
                }
            }

            d.Dispose();
        }

        /// <summary>
        /// Compare two BitVectors.
        /// this should really be an equals method on the BitVector itself. </summary>
        /// <param name="bv"> One bit vector </param>
        /// <param name="compare"> The second to compare </param>
        private bool DoCompare(BitVector bv, BitVector compare)
        {
            bool equal = true;
            for (int i = 0; i < bv.Length; i++) // LUCENENET NOTE: Length is the equivalent of size()
            {
                // bits must be equal
                if (bv.Get(i) != compare.Get(i))
                {
                    equal = false;
                    break;
                }
            }
            Assert.AreEqual(bv.Count(), compare.Count());
            return equal;
        }
    }
}