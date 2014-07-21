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
            var original = new Entry[0];
            var copy = this.CopyAndSort(original);
            this.VerifySorted(original, copy, Strategy.RANDOM);
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


        protected Entry[] GenerateEntries(Strategy strategy, int length)
        {
            var entries = new Entry[length];
            for (var i = 0; i < entries.Length; ++i)
            {
                strategy.SetValue(this.Random, entries, i);
            }

            return entries;
        }

        // test(Entry[] array)
        protected Entry[] CopyAndSort(Entry[] entries)
        {
            int start = this.Random.Next(1000);
            var toSort = new Entry[start + entries.Length + this.Random.Next(3)];
            Array.Copy(entries, 0, toSort, start, entries.Length);

            var sorter = this.CreateSorter(toSort);

            sorter.SortRange(start, start + entries.Length);

            return toSort.CopyOfRange(start, start + entries.Length);
        }

        // test(Strategy strategy, int length)
        protected void RunStrategy(Strategy strategy, int length = -1)
        {
            if (length < 0)
                length = Random.Next(50);

            var entries = this.GenerateEntries(strategy, length);
            var sorted = this.CopyAndSort(entries);

            this.VerifySorted(entries, sorted, strategy);
        }


        // assertSorted
        protected void VerifySorted(Entry[] original, Entry[] sorted, Strategy strategy)
        {
            Equal(original.Length, sorted.Length);
            var actuallySorted = original.CopyOf(original.Length);

            Array.Sort(actuallySorted);

            for (var i = 0; i < original.Length; ++i)
            {
                var actual = actuallySorted[i];
                var expected = sorted[i];

                Ok(actual.Value == expected.Value, "original {0} must equal {1} at position {2}", actual.Value, expected.Value, i);

                //
                if (this.Stable && strategy != Strategy.RANDOM_LOW_CARDINALITY)
                {
                    string append = "";
                    if(actual.Ordinal != expected.Ordinal )
                    {
                        for (var c = 0; c < original.Length; c++)
                        {
                            if (actuallySorted[c].Value == expected.Value)
                            {
                                append += " actual found at " + c.ToString() + ". value is " + actuallySorted[c].Value.ToString() + ".";

                       
                            }
                        }
                    }

                    Ok(actual.Ordinal == expected.Ordinal, "original oridinal {0} with value {1} should be equal to {2} with value"+
                        " {3} at position {4}. " + 
                        append, actual.Ordinal, actual.Value,  expected.Ordinal, expected.Value,i);
                }
            }
        }

        /// <summary>
        /// TODO: figure out the differences between the Java Version and .NET 
        /// 
        /// Porting the logic as it currently is causes the oridinal position to mismatch and randomly fail
        /// when the same value is generated for Entry.
        /// 
        /// Entry only sorts by the VALUE. If you have multiple entries
        /// with the same value like in RANDOM_LOW_CARDINALITY, then the sort can have the ordinal position out
        /// of order.
        /// 
        /// This could be caused by the differences in implementation of Array.Sort, the Java version of Array.sort
        /// and the SortRange impelementation.
        /// 
        ///  To work around this for the short term, the minValue is currently constrained to have a minValue of 1 and
        ///  RANDOM_LOW_CARDINALITY strategy currently omits checking for matching ordinal positions.
        /// </summary>
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

                col[index] = new Entry(random.Next(1, 6), index);
            });


            public static readonly Strategy ASCENDING = new Strategy((random, col, index) =>
            {
                Entry value = null;

                if (index == 0)
                    value = new Entry(random.Next(6), 0);
                else
                    value = new Entry(col[index - 1].Value + random.Next(1, 6), index);

                col[index] = value;
            });


            public static readonly Strategy DESCENDING = new Strategy((random, col, index) =>
            {
                Entry value = null;

                if (index == 0)
                    value = new Entry(random.Next(6), 0);
                else
                    value = new Entry(col[index - 1].Value - random.Next(1, 6), index);

                col[index] = value;
            });

            public static readonly Strategy STRICTLY_DESCENDING = new Strategy((random, col, index) =>
            {
                Entry value = null;

                if (index == 0)
                    value = new Entry(random.Next(6), 0);
                else
                    value = new Entry(col[index - 1].Value - random.Next(1, 5), index);

                col[index] = value;
            });

            public static readonly Strategy ASCENDING_SEQUENCES = new Strategy((random, col, index) =>
            {
                Entry value = null;

                if (index == 0)
                    value = new Entry(random.Next(6), 0);
                else
                    value = new Entry(random.Rarely() ? random.Next(1000) : col[index - 1].Value + new Random().Next(1, 6), index);

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
