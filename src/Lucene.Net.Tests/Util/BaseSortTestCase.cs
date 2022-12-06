using System.Collections.Generic;
using System.Linq;
using Lucene.Net.Support;
using NUnit.Framework;
using System;
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

    public abstract class BaseSortTestCase : LuceneTestCase
    {
        public class Entry : IComparable<Entry>
        {
            public int Value { get; }
            public int Ord { get; }

            public Entry(int value, int ord)
            {
                this.Value = value;
                this.Ord = ord;
            }

            public virtual int CompareTo(Entry other)
            {
                return Value < other.Value ? -1 : Value == other.Value ? 0 : 1;
            }
        }

        private readonly bool stable;
        private readonly Random random;

        protected BaseSortTestCase(bool stable)
        {
            this.stable = stable;
            this.random = Random;
        }

        public abstract Sorter NewSorter(Entry[] arr);

        public class StableEntryComparer : IComparer<Entry>
        {
            public int Compare(Entry a, Entry b)
            {
                if (a.Value < b.Value) return -1;
                else if (a.Value > b.Value) return 1;

                //required for stable sorting
                return a.Ord < b.Ord ? -1 : a.Ord == b.Ord ? 0 : 1;
            }
        }

        public virtual void AssertSorted(Entry[] original, Entry[] sorted)
        {
            Assert.AreEqual(original.Length, sorted.Length);
            Entry[] stableSorted = original.OrderBy(e => e, new StableEntryComparer()).ToArray();
            for (int i = 0; i < original.Length; ++i)
            {
                Assert.AreEqual(stableSorted[i].Value, sorted[i].Value);
                if (stable)
                {
                    Assert.AreEqual(stableSorted[i].Ord, sorted[i].Ord);
                }
            }
        }

        public virtual void SortTest(Entry[] arr)
        {
            int o = random.Next(1000);
            var toSort = new Entry[o + arr.Length + random.Next(3)];
            Arrays.Copy(arr, 0, toSort, o, arr.Length);
            Sorter sorter = NewSorter(toSort);
            sorter.Sort(o, o + arr.Length);
            AssertSorted(arr, Arrays.CopyOfRange(toSort, o, o + arr.Length));
        }

        private delegate void Strategy(Entry[] arr, int i);

        private void RandomStrategy(Entry[] arr, int i)
        {
            arr[i] = new Entry(random.Next(), i);
        }

        private void RandomLowCardinalityStrategy(Entry[] arr, int i)
        {
            arr[i] = new Entry(random.nextInt(6), i);
        }

        private void AscendingStrategy(Entry[] arr, int i)
        {
            arr[i] = i == 0
            ? new Entry(random.nextInt(6), 0)
            : new Entry(arr[i - 1].Value + random.nextInt(6), i);
        }

        private void DescendingStrategy(Entry[] arr, int i)
        {
            arr[i] = i == 0
            ? new Entry(random.nextInt(6), 0)
            : new Entry(arr[i - 1].Value - random.nextInt(6), i);
        }
        
        private void StrictlyDescendingStrategy(Entry[] arr, int i)
        {
            arr[i] = i == 0
            ? new Entry(random.nextInt(6), 0)
            : new Entry(arr[i - 1].Value - TestUtil.NextInt32(random, 1, 5), i);
            
        }

        private void AscendingSequencesStrategy(Entry[] arr, int i)
        {
            arr[i] = i == 0
            ? new Entry(random.nextInt(6), 0)
            : new Entry(Rarely(random) ? random.nextInt(1000) : arr[i - 1].Value + random.nextInt(6), i);
            
        }
        
        private void MostlyAscendingStrategy(Entry[] arr, int i)
        {
            arr[i] = i == 0
            ? new Entry(random.nextInt(6), 0)
            : new Entry(arr[i - 1].Value + TestUtil.NextInt32(random, -8, 10), i);
            
        }

        private void DoTest(Strategy strategy, int length)
        {
            Entry[] arr = new Entry[length];
            for (int i = 0; i < arr.Length; ++i) {
                strategy(arr, i);
            }
            SortTest(arr);
        }

        private void DoTest(Strategy strategy)
        {
            DoTest(strategy, Random.Next(20000));
        }

        [Test]
        public virtual void TestEmpty()
        {
            SortTest(new Entry[0]);
        }

        [Test]
        public virtual void TestOne()
        {
            DoTest(RandomStrategy, 1);
        }

        [Test]
        public virtual void TestTwo()
        {
            DoTest(RandomStrategy, 2);
        }

        [Test]
        public virtual void TestRandom()
        {
            DoTest(RandomStrategy);
        }

        [Test]
        public virtual void TestRandomLowCardinality()
        {
            DoTest(RandomLowCardinalityStrategy, 2);
        }

        [Test]
        public virtual void TestAscending()
        {
            DoTest(AscendingStrategy, 2);
        }

        [Test]
        public virtual void TestAscendingSequences()
        {
            DoTest(AscendingSequencesStrategy, 2);
        }

        [Test]
        public virtual void TestDescending()
        {
            DoTest(DescendingStrategy, 2);
        }

        [Test]
        public virtual void TestStrictlyDescendingStrategy()
        {
            DoTest(StrictlyDescendingStrategy, 2);
        }
    }
}