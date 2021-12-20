using NUnit.Framework;
using System;
using System.Collections.Generic;
using JCG = J2N.Collections.Generic;
using Assert = Lucene.Net.TestFramework.Assert;
using RandomizedTesting.Generators;

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
    public class TestMergedIterator : LuceneTestCase
    {
        private const int REPEATS = 2;
        private const int VALS_TO_MERGE = 15000;

        [Test]
        public virtual void TestMergeEmpty()
        {
            IEnumerator<int> merged = new MergedEnumerator<int>();
            Assert.IsFalse(merged.MoveNext());

            merged = new MergedEnumerator<int>((new JCG.List<int>()).GetEnumerator());
            Assert.IsFalse(merged.MoveNext());

            IEnumerator<int>[] itrs = new IEnumerator<int>[Random.Next(100)];
            for (int i = 0; i < itrs.Length; i++)
            {
                itrs[i] = (new JCG.List<int>()).GetEnumerator();
            }
            merged = new MergedEnumerator<int>(itrs);
            Assert.IsFalse(merged.MoveNext());
        }

        [Test]
        public virtual void TestNoDupsRemoveDups()
        {
            TestCase(1, 1, true);
        }

        [Test]
        public virtual void TestOffItrDupsRemoveDups()
        {
            TestCase(3, 1, true);
        }

        [Test]
        public virtual void TestOnItrDupsRemoveDups()
        {
            TestCase(1, 3, true);
        }

        [Test]
        public virtual void TestOnItrRandomDupsRemoveDups()
        {
            TestCase(1, -3, true);
        }

        [Test]
        public virtual void TestBothDupsRemoveDups()
        {
            TestCase(3, 3, true);
        }

        [Test]
        public virtual void TestBothDupsWithRandomDupsRemoveDups()
        {
            TestCase(3, -3, true);
        }

        [Test]
        public virtual void TestNoDupsKeepDups()
        {
            TestCase(1, 1, false);
        }

        [Test]
        public virtual void TestOffItrDupsKeepDups()
        {
            TestCase(3, 1, false);
        }

        [Test]
        public virtual void TestOnItrDupsKeepDups()
        {
            TestCase(1, 3, false);
        }

        [Test]
        public virtual void TestOnItrRandomDupsKeepDups()
        {
            TestCase(1, -3, false);
        }

        [Test]
        public virtual void TestBothDupsKeepDups()
        {
            TestCase(3, 3, false);
        }

        [Test]
        public virtual void TestBothDupsWithRandomDupsKeepDups()
        {
            TestCase(3, -3, false);
        }

        private void TestCase(int itrsWithVal, int specifiedValsOnItr, bool removeDups)
        {
            // Build a random number of lists
            IList<int> expected = new JCG.List<int>();
            Random random = new J2N.Randomizer(Random.NextInt64());
            int numLists = itrsWithVal + random.Next(1000 - itrsWithVal);
            IList<int>[] lists = new IList<int>[numLists];
            for (int i = 0; i < numLists; i++)
            {
                lists[i] = new JCG.List<int>();
            }
            int start = random.Next(1000000);
            int end = start + VALS_TO_MERGE / itrsWithVal / Math.Abs(specifiedValsOnItr);
            for (int i = start; i < end; i++)
            {
                int maxList = lists.Length;
                int maxValsOnItr = 0;
                int sumValsOnItr = 0;
                for (int itrWithVal = 0; itrWithVal < itrsWithVal; itrWithVal++)
                {
                    int list = random.Next(maxList);
                    int valsOnItr = specifiedValsOnItr < 0 ? (1 + random.Next(-specifiedValsOnItr)) : specifiedValsOnItr;
                    maxValsOnItr = Math.Max(maxValsOnItr, valsOnItr);
                    sumValsOnItr += valsOnItr;
                    for (int valOnItr = 0; valOnItr < valsOnItr; valOnItr++)
                    {
                        lists[list].Add(i);
                    }
                    maxList = maxList - 1;
                    ArrayUtil.Swap(lists, list, maxList);
                }
                int maxCount = removeDups ? maxValsOnItr : sumValsOnItr;
                for (int count = 0; count < maxCount; count++)
                {
                    expected.Add(i);
                }
            }
            // Now check that they get merged cleanly
            IEnumerator<int>[] itrs = new IEnumerator<int>[numLists];
            for (int i = 0; i < numLists; i++)
            {
                itrs[i] = lists[i].GetEnumerator();
            }
            try
            {
                MergedEnumerator<int> mergedItr = new MergedEnumerator<int>(removeDups, itrs);
                using IEnumerator<int> expectedItr = expected.GetEnumerator();
                while (expectedItr.MoveNext())
                {
                    Assert.IsTrue(mergedItr.MoveNext());
                    Assert.AreEqual(expectedItr.Current, mergedItr.Current);
                }
                Assert.IsFalse(mergedItr.MoveNext());
            }
            finally
            {
                IOUtils.Dispose(itrs);
            }
        }






        [Test]
        [Obsolete("This method will be removed in 4.8.0 release candidate.")]
        public virtual void TestMergeEmptyIterator()
        {
            IEnumerator<int> merged = new MergedIterator<int>();
            Assert.IsFalse(merged.MoveNext());

            merged = new MergedIterator<int>((new JCG.List<int>()).GetEnumerator());
            Assert.IsFalse(merged.MoveNext());

            IEnumerator<int>[] itrs = new IEnumerator<int>[Random.Next(100)];
            for (int i = 0; i < itrs.Length; i++)
            {
                itrs[i] = (new JCG.List<int>()).GetEnumerator();
            }
            merged = new MergedIterator<int>(itrs);
            Assert.IsFalse(merged.MoveNext());
        }

        [Test]
        [Obsolete("This method will be removed in 4.8.0 release candidate.")]
        public virtual void TestNoDupsRemoveDupsIterator()
        {
            TestCaseIterator(1, 1, true);
        }

        [Test]
        [Obsolete("This method will be removed in 4.8.0 release candidate.")]
        public virtual void TestOffItrDupsRemoveDupsIterator()
        {
            TestCaseIterator(3, 1, true);
        }

        [Test]
        [Obsolete("This method will be removed in 4.8.0 release candidate.")]
        public virtual void TestOnItrDupsRemoveDupsIterator()
        {
            TestCaseIterator(1, 3, true);
        }

        [Test]
        [Obsolete("This method will be removed in 4.8.0 release candidate.")]
        public virtual void TestOnItrRandomDupsRemoveDupsIterator()
        {
            TestCaseIterator(1, -3, true);
        }

        [Test]
        [Obsolete("This method will be removed in 4.8.0 release candidate.")]
        public virtual void TestBothDupsRemoveDupsIterator()
        {
            TestCaseIterator(3, 3, true);
        }

        [Test]
        [Obsolete("This method will be removed in 4.8.0 release candidate.")]
        public virtual void TestBothDupsWithRandomDupsRemoveDupsIterator()
        {
            TestCaseIterator(3, -3, true);
        }

        [Test]
        [Obsolete("This method will be removed in 4.8.0 release candidate.")]
        public virtual void TestNoDupsKeepDupsIterator()
        {
            TestCaseIterator(1, 1, false);
        }

        [Test]
        [Obsolete("This method will be removed in 4.8.0 release candidate.")]
        public virtual void TestOffItrDupsKeepDupsIterator()
        {
            TestCaseIterator(3, 1, false);
        }

        [Test]
        [Obsolete("This method will be removed in 4.8.0 release candidate.")]
        public virtual void TestOnItrDupsKeepDupsIterator()
        {
            TestCaseIterator(1, 3, false);
        }

        [Test]
        [Obsolete("This method will be removed in 4.8.0 release candidate.")]
        public virtual void TestOnItrRandomDupsKeepDupsIterator()
        {
            TestCaseIterator(1, -3, false);
        }

        [Test]
        [Obsolete("This method will be removed in 4.8.0 release candidate.")]
        public virtual void TestBothDupsKeepDupsIterator()
        {
            TestCaseIterator(3, 3, false);
        }

        [Test]
        [Obsolete("This method will be removed in 4.8.0 release candidate.")]
        public virtual void TestBothDupsWithRandomDupsKeepDupsIterator()
        {
            TestCaseIterator(3, -3, false);
        }

        [Obsolete("This method will be removed in 4.8.0 release candidate.")]
        private void TestCaseIterator(int itrsWithVal, int specifiedValsOnItr, bool removeDups)
        {
            // Build a random number of lists
            IList<int> expected = new JCG.List<int>();
            Random random = new J2N.Randomizer(Random.NextInt64());
            int numLists = itrsWithVal + random.Next(1000 - itrsWithVal);
            IList<int>[] lists = new IList<int>[numLists];
            for (int i = 0; i < numLists; i++)
            {
                lists[i] = new JCG.List<int>();
            }
            int start = random.Next(1000000);
            int end = start + VALS_TO_MERGE / itrsWithVal / Math.Abs(specifiedValsOnItr);
            for (int i = start; i < end; i++)
            {
                int maxList = lists.Length;
                int maxValsOnItr = 0;
                int sumValsOnItr = 0;
                for (int itrWithVal = 0; itrWithVal < itrsWithVal; itrWithVal++)
                {
                    int list = random.Next(maxList);
                    int valsOnItr = specifiedValsOnItr < 0 ? (1 + random.Next(-specifiedValsOnItr)) : specifiedValsOnItr;
                    maxValsOnItr = Math.Max(maxValsOnItr, valsOnItr);
                    sumValsOnItr += valsOnItr;
                    for (int valOnItr = 0; valOnItr < valsOnItr; valOnItr++)
                    {
                        lists[list].Add(i);
                    }
                    maxList = maxList - 1;
                    ArrayUtil.Swap(lists, list, maxList);
                }
                int maxCount = removeDups ? maxValsOnItr : sumValsOnItr;
                for (int count = 0; count < maxCount; count++)
                {
                    expected.Add(i);
                }
            }
            // Now check that they get merged cleanly
            IEnumerator<int>[] itrs = new IEnumerator<int>[numLists];
            for (int i = 0; i < numLists; i++)
            {
                itrs[i] = lists[i].GetEnumerator();
            }

            MergedIterator<int> mergedItr = new MergedIterator<int>(removeDups, itrs);
            using IEnumerator<int> expectedItr = expected.GetEnumerator();
            while (expectedItr.MoveNext())
            {
                Assert.IsTrue(mergedItr.MoveNext());
                Assert.AreEqual(expectedItr.Current, mergedItr.Current);
            }
            Assert.IsFalse(mergedItr.MoveNext());
        }
    }
}