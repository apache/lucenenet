using Lucene.Net.Diagnostics;
using Lucene.Net.Search;
using NUnit.Framework;
using RandomizedTesting.Generators;
using System;
using System.Collections.Generic;
using JCG = J2N.Collections.Generic;
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

    [TestFixture]
    public class TestWAH8DocIdSet : BaseDocIdSetTestCase<WAH8DocIdSet>
    {
        public override WAH8DocIdSet CopyOf(BitSet bs, int length)
        {
            int indexInterval = TestUtil.NextInt32(Random, 8, 256);
            WAH8DocIdSet.Builder builder = (WAH8DocIdSet.Builder)(new WAH8DocIdSet.Builder()).SetIndexInterval(indexInterval);
            for (int i = bs.NextSetBit(0); i != -1; i = bs.NextSetBit(i + 1))
            {
                builder.Add(i);
            }
            return builder.Build();
        }

        public WAH8DocIdSet CopyOf(OpenBitSet bs, int length)
        {
            int indexInterval = TestUtil.NextInt32(Random, 8, 256);
            WAH8DocIdSet.Builder builder = (WAH8DocIdSet.Builder)(new WAH8DocIdSet.Builder()).SetIndexInterval(indexInterval);
            for (int i = bs.NextSetBit(0); i != -1; i = bs.NextSetBit(i + 1))
            {
                builder.Add(i);
            }
            return builder.Build();
        }

        public override void AssertEquals(int numBits, BitSet ds1, WAH8DocIdSet ds2)
        {
            base.AssertEquals(numBits, ds1, ds2);
            Assert.AreEqual(ds1.Cardinality, ds2.Cardinality);
        }

        //public override void AssertEquals(int numBits, OpenBitSet ds1, WAH8DocIdSet ds2)
        //{
        //    base.AssertEquals(numBits, ds1, ds2);
        //    Assert.AreEqual(ds1.Cardinality, ds2.Cardinality);
        //}

        [Test]
        public virtual void TestUnion()
        {
            int numBits = TestUtil.NextInt32(Random, 100, 1 << 20);
            int numDocIdSets = TestUtil.NextInt32(Random, 0, 4);
            IList<BitSet> fixedSets = new JCG.List<BitSet>(numDocIdSets);
            for (int i = 0; i < numDocIdSets; ++i)
            {
                fixedSets.Add(RandomSet(numBits, Random.NextSingle() / 16));
            }
            IList<WAH8DocIdSet> compressedSets = new JCG.List<WAH8DocIdSet>(numDocIdSets);
            foreach (BitSet set in fixedSets)
            {
                compressedSets.Add(CopyOf(set, numBits));
            }

            WAH8DocIdSet union = WAH8DocIdSet.Union(compressedSets);
            BitSet expected = new BitSet(numBits);
            foreach (BitSet set in fixedSets)
            {
                for (int doc = set.NextSetBit(0); doc != -1; doc = set.NextSetBit(doc + 1))
                {
                    expected.Set(doc);
                }
            }
            AssertEquals(numBits, expected, union);
        }

        /// <summary>
        /// Create a random set which has <paramref name="numBitsSet"/> of its <paramref name="numBits"/> bits set. </summary>
        protected static OpenBitSet RandomOpenSet(int numBits, int numBitsSet)
        {
            if (Debugging.AssertsEnabled) Debugging.Assert(numBitsSet <= numBits);
            OpenBitSet set = new OpenBitSet(numBits);
            Random random = Random;
            if (numBitsSet == numBits)
            {
                set.Set(0, numBits);
            }
            else
            {
                for (int i = 0; i < numBitsSet; ++i)
                {
                    while (true)
                    {
                        int o = random.Next(numBits);
                        if (!set.Get(o))
                        {
                            set.Set(o);
                            break;
                        }
                    }
                }
            }
            return set;
        }

        /// <summary>
        /// Same as <see cref="RandomSet(int, int)"/> but given a load factor. </summary>
        protected static OpenBitSet RandomOpenSet(int numBits, float percentSet)
        {
            return RandomOpenSet(numBits, (int)(percentSet * numBits));
        }

        /// <summary>
        /// Assert that the content of the <see cref="DocIdSet"/> is the same as the content of the <see cref="OpenBitSet"/>.
        /// </summary>
        public virtual void AssertEquals(int numBits, OpenBitSet ds1, WAH8DocIdSet ds2)
        {
            // nextDoc
            DocIdSetIterator it2 = ds2.GetIterator();
            if (it2 is null)
            {
                Assert.AreEqual(-1, ds1.NextSetBit(0));
            }
            else
            {
                Assert.AreEqual(-1, it2.DocID);
                for (int doc = ds1.NextSetBit(0); doc != -1; doc = ds1.NextSetBit(doc + 1))
                {
                    Assert.AreEqual(doc, it2.NextDoc());
                    Assert.AreEqual(doc, it2.DocID);
                }
                Assert.AreEqual(DocIdSetIterator.NO_MORE_DOCS, it2.NextDoc());
                Assert.AreEqual(DocIdSetIterator.NO_MORE_DOCS, it2.DocID);
            }

            // nextDoc / advance
            it2 = ds2.GetIterator();
            if (it2 is null)
            {
                Assert.AreEqual(-1, ds1.NextSetBit(0));
            }
            else
            {
                for (int doc = -1; doc != DocIdSetIterator.NO_MORE_DOCS;)
                {
                    if (Random.NextBoolean())
                    {
                        doc = ds1.NextSetBit(doc + 1);
                        if (doc == -1)
                        {
                            doc = DocIdSetIterator.NO_MORE_DOCS;
                        }
                        Assert.AreEqual(doc, it2.NextDoc());
                        Assert.AreEqual(doc, it2.DocID);
                    }
                    else
                    {
                        int target = doc + 1 + Random.Next(Random.NextBoolean() ? 64 : Math.Max(numBits / 8, 1));
                        doc = ds1.NextSetBit(target);
                        if (doc == -1)
                        {
                            doc = DocIdSetIterator.NO_MORE_DOCS;
                        }
                        Assert.AreEqual(doc, it2.Advance(target));
                        Assert.AreEqual(doc, it2.DocID);
                    }
                }
            }

            // bits()
            IBits bits = ds2.Bits;
            if (bits != null)
            {
                // test consistency between bits and iterator
                it2 = ds2.GetIterator();
                for (int previousDoc = -1, doc = it2.NextDoc(); ; previousDoc = doc, doc = it2.NextDoc())
                {
                    int max = doc == DocIdSetIterator.NO_MORE_DOCS ? bits.Length : doc;
                    for (int i = previousDoc + 1; i < max; ++i)
                    {
                        Assert.AreEqual(false, bits.Get(i));
                    }
                    if (doc == DocIdSetIterator.NO_MORE_DOCS)
                    {
                        break;
                    }
                    Assert.AreEqual(true, bits.Get(doc));
                }
            }

            Assert.AreEqual(ds1.Cardinality, ds2.Cardinality);
        }

        [Test]
        public virtual void TestIntersection()
        {
            int numBits = TestUtil.NextInt32(Random, 100, 1 << 20);
            int numDocIdSets = TestUtil.NextInt32(Random, 1, 4);
            IList<OpenBitSet> fixedSets = new JCG.List<OpenBitSet>(numDocIdSets);
            for (int i = 0; i < numDocIdSets; ++i)
            {
                fixedSets.Add(RandomOpenSet(numBits, Random.NextSingle()));
            }
            IList<WAH8DocIdSet> compressedSets = new JCG.List<WAH8DocIdSet>(numDocIdSets);
            foreach (OpenBitSet set in fixedSets)
            {
                compressedSets.Add(CopyOf(set, numBits));
            }

            WAH8DocIdSet union = WAH8DocIdSet.Intersect(compressedSets);
            OpenBitSet expected = new OpenBitSet(numBits);
            expected.Set(0, expected.Length);
            foreach (OpenBitSet set in fixedSets)
            {
                for (int previousDoc = -1, doc = set.NextSetBit(0); ; previousDoc = doc, doc = set.NextSetBit(doc + 1))
                {
                    if (doc == -1)
                    {
                        expected.Clear(previousDoc + 1, set.Length);
                        break;
                    }
                    else
                    {
                        expected.Clear(previousDoc + 1, doc);
                    }
                }
            }
            AssertEquals(numBits, expected, union);
        }
    }
}