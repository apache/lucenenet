using Lucene.Net.Diagnostics;
using Lucene.Net.Search;
using System;
using Assert = Lucene.Net.TestFramework.Assert;
using BitSet = J2N.Collections.BitSet;


#if TESTFRAMEWORK_MSTEST
using Test = Microsoft.VisualStudio.TestTools.UnitTesting.TestMethodAttribute;
#elif TESTFRAMEWORK_NUNIT
using Test = NUnit.Framework.TestAttribute;
#elif TESTFRAMEWORK_XUNIT
using Test = Lucene.Net.TestFramework.SkippableFactAttribute;
#endif

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
    /// Base test class for <see cref="DocIdSet"/>s. </summary>
    public abstract class BaseDocIdSetTestCase<T> : LuceneTestCase
        
#if TESTFRAMEWORK_XUNIT
        , Xunit.IClassFixture<BeforeAfterClass>
        where T : Lucene.Net.Search.DocIdSet
    {
        public BaseDocIdSetTestCase(BeforeAfterClass beforeAfter)
            : base(beforeAfter)
        {
        }
#else
        where T : Lucene.Net.Search.DocIdSet
    {
#endif
        /// <summary>
        /// Create a copy of the given <see cref="BitSet"/> which has <paramref name="length"/> bits. </summary>
        public abstract T CopyOf(BitSet bs, int length);

        /// <summary>
        /// Create a random set which has <paramref name="numBitsSet"/> of its <paramref name="numBits"/> bits set. </summary>
        protected static BitSet RandomSet(int numBits, int numBitsSet)
        {
            if (Debugging.ShouldAssert(numBitsSet <= numBits)) Debugging.ThrowAssert();
            BitSet set = new BitSet(numBits);
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
        protected static BitSet RandomSet(int numBits, float percentSet)
        {
            return RandomSet(numBits, (int)(percentSet * numBits));
        }

        /// <summary>
        /// Test length=0.
        /// </summary>
        [Test]
        public virtual void TestNoBit()
        {
            BitSet bs = new BitSet(1);
            T copy = CopyOf(bs, 0);
            AssertEquals(0, bs, copy);
        }

        /// <summary>
        /// Test length=1.
        /// </summary>
        [Test]
        public virtual void Test1Bit()
        {
            BitSet bs = new BitSet(1);
            if (Random.NextBoolean())
            {
                bs.Set(0);
            }
            T copy = CopyOf(bs, 1);
            AssertEquals(1, bs, copy);
        }

        /// <summary>
        /// Test length=2.
        /// </summary>
        [Test]
        public virtual void Test2Bits()
        {
            BitSet bs = new BitSet(2);
            if (Random.NextBoolean())
            {
                bs.Set(0);
            }
            if (Random.NextBoolean())
            {
                bs.Set(1);
            }
            T copy = CopyOf(bs, 2);
            AssertEquals(2, bs, copy);
        }

        /// <summary>
        /// Compare the content of the set against a <see cref="BitSet"/>.
        /// </summary>
        [Test]
        public virtual void TestAgainstBitSet()
        {
            int numBits = TestUtil.NextInt32(Random, 100, 1 << 20);
            // test various random sets with various load factors
            foreach (float percentSet in new float[] { 0f, 0.0001f, (float)Random.NextDouble() / 2, 0.9f, 1f })
            {
                BitSet set = RandomSet(numBits, percentSet);
                T copy = CopyOf(set, numBits);
                AssertEquals(numBits, set, copy);
            }
            // test one doc
            BitSet set_ = new BitSet(numBits);
            set_.Set(0); // 0 first
            T copy_ = CopyOf(set_, numBits);
            AssertEquals(numBits, set_, copy_);
            set_.Clear(0);
            set_.Set(Random.Next(numBits));
            copy_ = CopyOf(set_, numBits); // then random index
            AssertEquals(numBits, set_, copy_);
            // test regular increments
            for (int inc = 2; inc < 1000; inc += TestUtil.NextInt32(Random, 1, 100))
            {
                set_ = new BitSet(numBits);
                for (int d = Random.Next(10); d < numBits; d += inc)
                {
                    set_.Set(d);
                }
                copy_ = CopyOf(set_, numBits);
                AssertEquals(numBits, set_, copy_);
            }
        }

        /// <summary>
        /// Assert that the content of the <see cref="DocIdSet"/> is the same as the content of the <see cref="BitSet"/>.
        /// </summary>
#pragma warning disable xUnit1013
        public virtual void AssertEquals(int numBits, BitSet ds1, T ds2)
#pragma warning restore xUnit1013
        {
            // nextDoc
            DocIdSetIterator it2 = ds2.GetIterator();
            if (it2 == null)
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
            if (it2 == null)
            {
                Assert.AreEqual(-1, ds1.NextSetBit(0));
            }
            else
            {
                for (int doc = -1; doc != DocIdSetIterator.NO_MORE_DOCS; )
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
        }
    }
}