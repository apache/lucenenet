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
    using System.Linq;
    using System.Collections.Generic;
    using Lucene.Net.TestFramework;
    using Lucene.Net.TestFramework.Random;

    public abstract class BaseSorterTestCase : LuceneTestCase
    {
        private readonly Boolean Stable;

        public BaseSorterTestCase(bool stable)
        {
            this.Stable = stable;
        }

        internal abstract Sorter CreateSorter(Entry[] array);


        [Test]
        public virtual void TestEmptyArray()
        {
            this.TestGeneratedEntries(new Entry[0]);
        }

        [Test]
        public virtual void TestOneValue()
        {
            this.RunStrategy(Strategy.RANDOM, 1);
        }


        [Test]
        public virtual void TestTwoValues()
        {
            this.RunStrategy(Strategy.RANDOM_LOW_CARDINALITY, 2);
        }

        [Test]
        public virtual void TestRandom()
        {
            this.RunStrategy(Strategy.RANDOM);
        }

        [Test]
        public void TestRandomLowCardinality()
        {
            this.RunStrategy(Strategy.RANDOM_LOW_CARDINALITY);
        }

        [Test]
        public void TestAscending()
        {
            this.RunStrategy(Strategy.ASCENDING);
        }

        [Test]
        public void TestAscendingSequences()
        {
            this.RunStrategy(Strategy.ASCENDING_SEQUENCES);
        }


        [Test]
        public void TestDescending()
        {
            this.RunStrategy(Strategy.DESCENDING);
        }

        [Test]
        public void TestStrictlyDescending()
        {
            this.RunStrategy(Strategy.STRICTLY_DESCENDING);
        }

        protected void RunStrategy(Strategy strategy, int length = -1)
        {
            if (length < 0)
                length = Random.Next(20000);

            var entries = new Entry[length];
            for (var i = 0; i < entries.Length; i++)
            {
                strategy.SetValue(this.Random, entries, i);
            }

            this.TestGeneratedEntries(entries);
        }

        protected void TestGeneratedEntries(Entry[] entries)
        {
            int o = this.Random.Next(1000);
            var actual = new Entry[o + entries.Length + this.Random.Next(3)];
            Array.Copy(entries, 0, actual, o, entries.Length);
            var sorter = this.CreateSorter(actual);
            sorter.SortSlice(o, o + entries.Length);

            VerifySorted(entries, actual);
        }

        protected void VerifySorted(IEnumerable<Entry> original, IEnumerable<Entry> sorted)
        {
            var originalCount = original.Count();

            Equal(originalCount, sorted.Count());
            var copy = original.ToList();
            copy.Sort();

            for (var i = 0; i < originalCount; i++)
            {
                var actual = copy.ElementAt(i);
                var expected = copy.ElementAt(i);

                Equal(actual.Value, expected.Value);

                if (this.Stable)
                {
                    Equal(actual.Ordinal, expected.Ordinal);
                }
            }
        }

        internal protected class Strategy
        {
            public Strategy(Action<System.Random, IList<Entry>, int> setValue)
            {
                this.SetValue = setValue;
            }

            public Action<System.Random, IList<Entry>, int> SetValue { get; private set; }


            public static readonly Strategy RANDOM = new Strategy((random, col, index) =>
            {
                col[index] = new Entry(random.Next(), index);
            });

            public static readonly Strategy RANDOM_LOW_CARDINALITY = new Strategy((random, col, index) =>
            {
                col[index] = new Entry(random.Next(6), index);
            });


            public static readonly Strategy ASCENDING = new Strategy((random, col, index) =>
            {
                Entry value = null;

                if (index == 0)
                    value = new Entry(random.Next(6), 0);
                else
                    value = new Entry(col[index - 1].Value + random.Next(6), index);

                col[index] = value;
            });


            public static readonly Strategy DESCENDING = new Strategy((random, col, index) =>
            {
                Entry value = null;

                if (index == 0)
                    value = new Entry(random.Next(6), 0);
                else
                    value = new Entry(col[index - 1].Value + random.Next(6), index);

                col[index] = value;
            });

            public static readonly Strategy STRICTLY_DESCENDING = new Strategy((random, col, index) =>
            {
                Entry value = null;

                if (index == 0)
                    value = new Entry(random.Next(6), 0);
                else
                    value = new Entry(col[index - 1].Value - random.NextBetween(1, 5), index);

                col[index] = value;
            });

            public static readonly Strategy ASCENDING_SEQUENCES = new Strategy((random, col, index) =>
            {
                Entry value = null;

                if (index == 0)
                    value = new Entry(random.Next(6), 0);
                else
                    value = new Entry(random.Rarely() ? random.Next(1000) : col[index - 1].Value + random.Next(6), index);

                col[index] = value;
            });

            public static readonly Strategy MOSTLY_ASCENDING = new Strategy((random, col, index) =>
            {
                Entry value = null;

                if (index == 0)
                    value = new Entry(random.Next(6), 0);
                else
                    value = new Entry(col[index - 1].Value + random.NextBetween(-8, 10), index);

                col[index] = value;
            });
        }


        public class Entry : IComparable<Entry>
        {
            public int Value { get; private set; }
            public int Ordinal { get; private set; }

            public Entry(int value, int ordinal)
            {
                this.Value = value;
                this.Ordinal = ordinal;
            }

            public int CompareTo(Entry other)
            {
                return this.Value < other.Value ? -1 : Value == other.Value ? 0 : 1;
            }
        }

    }
}
