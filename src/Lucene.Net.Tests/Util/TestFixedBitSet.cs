using Lucene.Net.Attributes;
using NUnit.Framework;
using RandomizedTesting.Generators;
using System;
using Assert = Lucene.Net.TestFramework.Assert;
using BitSet = J2N.Collections.BitSet;

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

    using DocIdSetIterator = Lucene.Net.Search.DocIdSetIterator;

    [TestFixture]
    public class TestFixedBitSet : BaseDocIdSetTestCase<FixedBitSet>
    {
        public override FixedBitSet CopyOf(BitSet bs, int length)
        {
            FixedBitSet set = new FixedBitSet(length);
            for (int doc = bs.NextSetBit(0); doc != -1; doc = bs.NextSetBit(doc + 1))
            {
                set.Set(doc);
            }
            return set;
        }

        internal virtual void DoGet(BitSet a, FixedBitSet b)
        {
            int max = b.Length;
            for (int i = 0; i < max; i++)
            {
                if (a.Get(i) != b.Get(i))
                {
                    Assert.Fail("mismatch: BitSet=[" + i + "]=" + a.Get(i));
                }
            }
        }

        internal virtual void DoNextSetBit(BitSet a, FixedBitSet b)
        {
            int aa = -1, bb = -1;
            do
            {
                aa = a.NextSetBit(aa + 1);
                bb = bb < b.Length - 1 ? b.NextSetBit(bb + 1) : -1;
                Assert.AreEqual(aa, bb);
            } while (aa >= 0);
        }

        internal virtual void DoPrevSetBit(BitSet a, FixedBitSet b)
        {
            int aa = a.Length + Random.Next(100);
            int bb = aa;
            do
            {
                // aa = a.PrevSetBit(aa-1);
                aa--;
                while ((aa >= 0) && (aa >= a.Length || !a.Get(aa)))
                {
                    aa--;
                }
                if (b.Length == 0)
                {
                    bb = -1;
                }
                else if (bb > b.Length - 1)
                {
                    bb = b.PrevSetBit(b.Length - 1);
                }
                else if (bb < 1)
                {
                    bb = -1;
                }
                else
                {
                    bb = bb >= 1 ? b.PrevSetBit(bb - 1) : -1;
                }
                Assert.AreEqual(aa, bb);
            } while (aa >= 0);
        }

        // test interleaving different FixedBitSetIterator.Next()/skipTo()
        internal virtual void DoIterate(BitSet a, FixedBitSet b, int mode)
        {
            if (mode == 1)
            {
                DoIterate1(a, b);
            }
            if (mode == 2)
            {
                DoIterate2(a, b);
            }
        }

        internal virtual void DoIterate1(BitSet a, FixedBitSet b)
        {
            int aa = -1, bb = -1;
            DocIdSetIterator iterator = b.GetIterator();
            do
            {
                aa = a.NextSetBit(aa + 1);
                bb = (bb < b.Length && Random.NextBoolean()) ? iterator.NextDoc() : iterator.Advance(bb + 1);
                Assert.AreEqual(aa == -1 ? DocIdSetIterator.NO_MORE_DOCS : aa, bb);
            } while (aa >= 0);
        }

        internal virtual void DoIterate2(BitSet a, FixedBitSet b)
        {
            int aa = -1, bb = -1;
            DocIdSetIterator iterator = b.GetIterator();
            do
            {
                aa = a.NextSetBit(aa + 1);
                bb = Random.NextBoolean() ? iterator.NextDoc() : iterator.Advance(bb + 1);
                Assert.AreEqual(aa == -1 ? DocIdSetIterator.NO_MORE_DOCS : aa, bb);
            } while (aa >= 0);
        }

        internal virtual void DoRandomSets(int maxSize, int iter, int mode)
        {
            BitSet a0 = null;
            FixedBitSet b0 = null;

            for (int i = 0; i < iter; i++)
            {
                int sz = TestUtil.NextInt32(Random, 2, maxSize);
                BitSet a = new BitSet(sz);
                FixedBitSet b = new FixedBitSet(sz);

                // test the various ways of setting bits
                if (sz > 0)
                {
                    int nOper = Random.Next(sz);
                    for (int j = 0; j < nOper; j++)
                    {
                        int idx;

                        idx = Random.Next(sz);
                        a.Set(idx);
                        b.Set(idx);

                        idx = Random.Next(sz);
                        a.Clear(idx);
                        b.Clear(idx);

                        idx = Random.Next(sz);
                        a.Flip(idx, idx + 1);
                        b.Flip(idx, idx + 1);

                        idx = Random.Next(sz);
                        a.Flip(idx, idx + 1);
                        b.Flip(idx, idx + 1);

                        bool val2 = b.Get(idx);
                        bool val = b.GetAndSet(idx);
                        Assert.IsTrue(val2 == val);
                        Assert.IsTrue(b.Get(idx));

                        if (!val)
                        {
                            b.Clear(idx);
                        }
                        Assert.IsTrue(b.Get(idx) == val);
                    }
                }

                // test that the various ways of accessing the bits are equivalent
                DoGet(a, b);

                // test ranges, including possible extension
                int fromIndex, toIndex;
                fromIndex = Random.Next(sz / 2);
                toIndex = fromIndex + Random.Next(sz - fromIndex);
                BitSet aa = (BitSet)a.Clone();
                aa.Flip(fromIndex, toIndex);
                FixedBitSet bb = b.Clone();
                bb.Flip(fromIndex, toIndex);

                DoIterate(aa, bb, mode); // a problem here is from flip or doIterate

                fromIndex = Random.Next(sz / 2);
                toIndex = fromIndex + Random.Next(sz - fromIndex);
                aa = (BitSet)a.Clone();
                aa.Clear(fromIndex, toIndex);
                bb = b.Clone();
                bb.Clear(fromIndex, toIndex);

                DoNextSetBit(aa, bb); // a problem here is from clear() or nextSetBit

                DoPrevSetBit(aa, bb);

                fromIndex = Random.Next(sz / 2);
                toIndex = fromIndex + Random.Next(sz - fromIndex);
                aa = (BitSet)a.Clone();
                aa.Set(fromIndex, toIndex);
                bb = b.Clone();
                bb.Set(fromIndex, toIndex);

                DoNextSetBit(aa, bb); // a problem here is from set() or nextSetBit

                DoPrevSetBit(aa, bb);

                if (b0 != null && b0.Length <= b.Length)
                {
                    Assert.AreEqual(a.Cardinality, b.Cardinality);
                    
                    BitSet a_and = (BitSet)a.Clone();
                    a_and.And(a0);
                    BitSet a_or = (BitSet)a.Clone();
                    a_or.Or(a0);
                    BitSet a_xor = (BitSet)a.Clone();
                    a_xor.Xor(a0);
                    BitSet a_andn = (BitSet)a.Clone();
                    a_andn.AndNot(a0);

                    FixedBitSet b_and = b.Clone();
                    Assert.AreEqual(b, b_and);
                    b_and.And(b0);
                    FixedBitSet b_or = b.Clone();
                    b_or.Or(b0);
                    FixedBitSet b_xor = b.Clone();
                    b_xor.Xor(b0);
                    FixedBitSet b_andn = b.Clone();
                    b_andn.AndNot(b0);

                    Assert.AreEqual(a0.Cardinality, b0.Cardinality);
                    Assert.AreEqual(a_or.Cardinality, b_or.Cardinality);

                    DoIterate(a_and, b_and, mode);
                    DoIterate(a_or, b_or, mode);
                    DoIterate(a_andn, b_andn, mode);
                    DoIterate(a_xor, b_xor, mode);

                    Assert.AreEqual(a_and.Cardinality, b_and.Cardinality);
                    Assert.AreEqual(a_or.Cardinality, b_or.Cardinality);
                    Assert.AreEqual(a_xor.Cardinality, b_xor.Cardinality);
                    Assert.AreEqual(a_andn.Cardinality, b_andn.Cardinality);
                }

                a0 = a;
                b0 = b;
            }
        }

        // large enough to flush obvious bugs, small enough to run in <.5 sec as part of a
        // larger testsuite.
        [Test]
        public virtual void TestSmall()
        {
            DoRandomSets(AtLeast(1200), AtLeast(1000), 1);
            DoRandomSets(AtLeast(1200), AtLeast(1000), 2);
        }

        [Test, LuceneNetSpecific]
        public void TestClearSmall()
        {
            FixedBitSet a = new FixedBitSet(30);   // 0110010111001000101101001001110...0
            int[] onesA = { 1, 2, 5, 7, 8, 9, 12, 16, 18, 19, 21, 24, 27, 28, 29 };

            for (int i = 0; i < onesA.size(); i++)
            {
                a.Set(onesA[i]);
            }

            FixedBitSet b = new FixedBitSet(30);   // 0110000001001000101101001001110...0
            int[] onesB = { 1, 2, 9, 12, 16, 18, 19, 21, 24, 27, 28, 29 };

            for (int i = 0; i < onesB.size(); i++)
            {
                b.Set(onesB[i]);
            }

            a.Clear(5, 9);
            Assert.True(a.Equals(b));

            a.Clear(9, 10);
            Assert.False(a.Equals(b));

            a.Set(9);
            Assert.True(a.Equals(b));
        }

        [Test, LuceneNetSpecific]
        public void TestClearLarge()
        {
            Random random = Random;
            int iters = AtLeast(1000);
            for (int it = 0; it < iters; it++)
            {
                int sz = AtLeast(1200);
                FixedBitSet a = new FixedBitSet(sz);
                FixedBitSet b = new FixedBitSet(sz);
                int from = random.Next(sz - 1);
                int to = random.Next(from, sz);

                for (int i = 0; i < sz / 2; i++)
                {
                    int index = random.Next(sz - 1);
                    a.Set(index);

                    if (index < from || index >= to)
                    {
                        b.Set(index);
                    }
                }

                a.Clear(from, to);
                Assert.True(a.Equals(b));
            }
        }

        // uncomment to run a bigger test (~2 minutes).
        /*
        public void testBig() {
          doRandomSets(2000,200000, 1);
          doRandomSets(2000,200000, 2);
        }
        */

        [Test]
        public virtual void TestEquals()
        {
            // this test can't handle numBits==0:
            int numBits = Random.Next(2000) + 1;
            FixedBitSet b1 = new FixedBitSet(numBits);
            FixedBitSet b2 = new FixedBitSet(numBits);
            Assert.IsTrue(b1.Equals(b2));
            Assert.IsTrue(b2.Equals(b1));
            for (int iter = 0; iter < 10 * RandomMultiplier; iter++)
            {
                int idx = Random.Next(numBits);
                if (!b1.Get(idx))
                {
                    b1.Set(idx);
                    Assert.IsFalse(b1.Equals(b2));
                    Assert.IsFalse(b2.Equals(b1));
                    b2.Set(idx);
                    Assert.IsTrue(b1.Equals(b2));
                    Assert.IsTrue(b2.Equals(b1));
                }
            }

            // try different type of object
            Assert.IsFalse(b1.Equals(new object()));
        }

        [Test]
        public virtual void TestHashCodeEquals()
        {
            // this test can't handle numBits==0:
            int numBits = Random.Next(2000) + 1;
            FixedBitSet b1 = new FixedBitSet(numBits);
            FixedBitSet b2 = new FixedBitSet(numBits);
            Assert.IsTrue(b1.Equals(b2));
            Assert.IsTrue(b2.Equals(b1));
            for (int iter = 0; iter < 10 * RandomMultiplier; iter++)
            {
                int idx = Random.Next(numBits);
                if (!b1.Get(idx))
                {
                    b1.Set(idx);
                    Assert.IsFalse(b1.Equals(b2));
                    Assert.IsFalse(b1.GetHashCode() == b2.GetHashCode());
                    b2.Set(idx);
                    Assert.AreEqual(b1, b2);
                    Assert.AreEqual(b1.GetHashCode(), b2.GetHashCode());
                }
            }
        }

        [Test]
        public virtual void TestSmallBitSets()
        {
            // Make sure size 0-10 bit sets are OK:
            for (int numBits = 0; numBits < 10; numBits++)
            {
                FixedBitSet b1 = new FixedBitSet(numBits);
                FixedBitSet b2 = new FixedBitSet(numBits);
                Assert.IsTrue(b1.Equals(b2));
                Assert.AreEqual(b1.GetHashCode(), b2.GetHashCode());
                Assert.AreEqual(0, b1.Cardinality);
                if (numBits > 0)
                {
                    b1.Set(0, numBits);
                    Assert.AreEqual(numBits, b1.Cardinality);
                    b1.Flip(0, numBits);
                    Assert.AreEqual(0, b1.Cardinality);
                }
            }
        }

        private FixedBitSet MakeFixedBitSet(int[] a, int numBits)
        {
            FixedBitSet bs;
            if (Random.NextBoolean())
            {
                int bits2words = FixedBitSet.Bits2words(numBits);
                long[] words = new long[bits2words + Random.Next(100)];
                for (int i = bits2words; i < words.Length; i++)
                {
                    words[i] = Random.NextInt64();
                }
                bs = new FixedBitSet(words, numBits);
            }
            else
            {
                bs = new FixedBitSet(numBits);
            }
            foreach (int e in a)
            {
                bs.Set(e);
            }
            return bs;
        }

        private BitSet MakeBitSet(int[] a)
        {
            BitSet bs = new BitSet();
            foreach (int e in a)
            {
                bs.Set(e);
            }
            return bs;
        }

        private void CheckPrevSetBitArray(int[] a, int numBits)
        {
            FixedBitSet obs = MakeFixedBitSet(a, numBits);
            BitSet bs = MakeBitSet(a);
            DoPrevSetBit(bs, obs);
        }

        [Test]
        public virtual void TestPrevSetBit()
        {
            CheckPrevSetBitArray(new int[] { }, 0);
            CheckPrevSetBitArray(new int[] { 0 }, 1);
            CheckPrevSetBitArray(new int[] { 0, 2 }, 3);
        }

        private void CheckNextSetBitArray(int[] a, int numBits)
        {
            FixedBitSet obs = MakeFixedBitSet(a, numBits);
            BitSet bs = MakeBitSet(a);
            DoNextSetBit(bs, obs);
        }

        [Test]
        public virtual void TestNextBitSet()
        {
            int[] setBits = new int[0 + Random.Next(1000)];
            for (int i = 0; i < setBits.Length; i++)
            {
                setBits[i] = Random.Next(setBits.Length);
            }
            CheckNextSetBitArray(setBits, setBits.Length + Random.Next(10));

            CheckNextSetBitArray(new int[0], setBits.Length + Random.Next(10));
        }

        [Test]
        public virtual void TestEnsureCapacity()
        {
            FixedBitSet bits = new FixedBitSet(5);
            bits.Set(1);
            bits.Set(4);

            FixedBitSet newBits = FixedBitSet.EnsureCapacity(bits, 8); // grow within the word
            Assert.IsTrue(newBits.Get(1));
            Assert.IsTrue(newBits.Get(4));
            newBits.Clear(1);
            // we align to 64-bits, so even though it shouldn't have, it re-allocated a long[1]
            Assert.IsTrue(bits.Get(1));
            Assert.IsFalse(newBits.Get(1));

            newBits.Set(1);
            newBits = FixedBitSet.EnsureCapacity(newBits, newBits.Length - 2); // reuse
            Assert.IsTrue(newBits.Get(1));

            bits.Set(1);
            newBits = FixedBitSet.EnsureCapacity(bits, 72); // grow beyond one word
            Assert.IsTrue(newBits.Get(1));
            Assert.IsTrue(newBits.Get(4));
            newBits.Clear(1);
            // we grew the long[], so it's not shared
            Assert.IsTrue(bits.Get(1));
            Assert.IsFalse(newBits.Get(1));
        }
    }
}