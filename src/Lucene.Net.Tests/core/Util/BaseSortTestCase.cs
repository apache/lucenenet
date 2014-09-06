using Lucene.Net.Support;
using NUnit.Framework;
using System;

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
            public readonly int Value;
            public readonly int Ord;

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

        private readonly bool Stable;

        protected BaseSortTestCase(bool stable)
        {
            this.Stable = stable;
        }

        public abstract Sorter NewSorter(Entry[] arr);

        public virtual void AssertSorted(Entry[] original, Entry[] sorted)
        {
            Assert.AreEqual(original.Length, sorted.Length);
            Entry[] actuallySorted = Arrays.CopyOf(original, original.Length);
            Array.Sort(actuallySorted);
            for (int i = 0; i < original.Length; ++i)
            {
                Assert.AreEqual(actuallySorted[i].Value, sorted[i].Value);
                if (Stable)
                {
                    Assert.AreEqual(actuallySorted[i].Ord, sorted[i].Ord);
                }
            }
        }

        [Test]
        public virtual void Test(Entry[] arr)
        {
            int o = Random().Next(1000);
            var toSort = new Entry[o + arr.Length + Random().Next(3)];
            Array.Copy(arr, 0, toSort, o, arr.Length);
            Sorter sorter = NewSorter(toSort);
            sorter.Sort(o, o + arr.Length);
            AssertSorted(arr, Arrays.CopyOfRange(toSort, o, o + arr.Length));
        }

        internal enum Strategy
        {
            RANDOM,
            RANDOM_LOW_CARDINALITY,
            ASCENDING,
            DESCENDING,
            STRICTLY_DESCENDING,
            ASCENDING_SEQUENCES,
            MOSTLY_ASCENDING
        }

        public abstract void Set(Entry[] arr, int i);
    }
}