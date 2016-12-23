using System.Diagnostics;

namespace Lucene.Net.Search
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

    using Lucene.Net.Util;
    using System;

    //Used to hold non-generic nested types
    public static class FieldValueHitQueue
    {
        // had to change from internal to public, due to public accessability of FieldValueHitQueue
        public class Entry : ScoreDoc
        {
            public int Slot; // LUCENENET TODO: make property

            public Entry(int slot, int doc, float score)
                : base(doc, score)
            {
                this.Slot = slot;
            }

            public override string ToString()
            {
                return "slot:" + Slot + " " + base.ToString();
            }
        }

        /// <summary> An implementation of <see cref="FieldValueHitQueue" /> which is optimized in case
        /// there is just one comparator.
        /// </summary>
        internal sealed class OneComparatorFieldValueHitQueue<T> : FieldValueHitQueue<T>
            where T : FieldValueHitQueue.Entry
        {
            private int oneReverseMul;

            public OneComparatorFieldValueHitQueue(SortField[] fields, int size)
                : base(fields, size)
            {
                if (fields.Length == 0)
                {
                    throw new System.ArgumentException("Sort must contain at least one field");
                }

                SortField field = fields[0];
                SetComparator(0, field.GetComparator(size, 0));
                oneReverseMul = field.reverse ? -1 : 1;

                ReverseMul[0] = oneReverseMul;
            }

            /// <summary> Returns whether <c>a</c> is less relevant than <c>b</c>.</summary>
            /// <param name="hitA">ScoreDoc</param>
            /// <param name="hitB">ScoreDoc</param>
            /// <returns><c>true</c> if document <c>a</c> should be sorted after document <c>b</c>.</returns>
            public override bool LessThan(T hitA, T hitB)
            {
                Debug.Assert(hitA != hitB);
                Debug.Assert(hitA.Slot != hitB.Slot);

                int c = oneReverseMul * FirstComparator.Compare(hitA.Slot, hitB.Slot);
                if (c != 0)
                {
                    return c > 0;
                }

                // avoid random sort order that could lead to duplicates (bug #31241):
                return hitA.Doc > hitB.Doc;
            }

            public override bool LessThan(Entry a, Entry b)
            {
                return LessThan(a, b);
            }
        }

        /// <summary> An implementation of <see cref="FieldValueHitQueue" /> which is optimized in case
        /// there is more than one comparator.
        /// </summary>
        internal sealed class MultiComparatorsFieldValueHitQueue<T> : FieldValueHitQueue<T>
            where T : FieldValueHitQueue.Entry
        {
            public MultiComparatorsFieldValueHitQueue(SortField[] fields, int size)
                : base(fields, size)
            {
                int numComparators = comparators.Length;
                for (int i = 0; i < numComparators; ++i)
                {
                    SortField field = fields[i];

                    ReverseMul[i] = field.reverse ? -1 : 1;
                    SetComparator(i, field.GetComparator(size, i));
                }
            }

            public override bool LessThan(T hitA, T hitB)
            {
                Debug.Assert(hitA != hitB);
                Debug.Assert(hitA.Slot != hitB.Slot);

                int numComparators = comparators.Length;
                for (int i = 0; i < numComparators; ++i)
                {
                    int c = ReverseMul[i] * comparators[i].Compare(hitA.Slot, hitB.Slot);
                    if (c != 0)
                    {
                        // Short circuit
                        return c > 0;
                    }
                }

                // avoid random sort order that could lead to duplicates (bug #31241):
                return hitA.Doc > hitB.Doc;
            }

            public override bool LessThan(Entry a, Entry b)
            {
                return LessThan(a, b);
            }
        }

        /// <summary> Creates a hit queue sorted by the given list of fields.
        ///
        /// <p/><b>NOTE</b>: The instances returned by this method
        /// pre-allocate a full array of length <c>numHits</c>.
        ///
        /// </summary>
        /// <param name="fields">SortField array we are sorting by in priority order (highest
        /// priority first); cannot be <c>null</c> or empty
        /// </param>
        /// <param name="size">The number of hits to retain. Must be greater than zero.
        /// </param>
        /// <throws>  IOException </throws>
        public static FieldValueHitQueue<T> Create<T>(SortField[] fields, int size)
            where T : FieldValueHitQueue.Entry
        {
            if (fields.Length == 0)
            {
                throw new ArgumentException("Sort must contain at least one field");
            }

            if (fields.Length == 1)
            {
                return new FieldValueHitQueue.OneComparatorFieldValueHitQueue<T>(fields, size);
            }
            else
            {
                return new FieldValueHitQueue.MultiComparatorsFieldValueHitQueue<T>(fields, size);
            }
        }
    }

    /// <summary>
    /// Expert: A hit queue for sorting by hits by terms in more than one field.
    /// Uses <code>FieldCache.DEFAULT</code> for maintaining
    /// internal term lookup tables.
    ///
    /// @lucene.experimental
    /// @since 2.9 </summary>
    /// <seealso cref= IndexSearcher#search(Query,Filter,int,Sort) </seealso>
    /// <seealso cref= FieldCache </seealso>
    public abstract class FieldValueHitQueue<T> : PriorityQueue<T>
        where T : FieldValueHitQueue.Entry
    {
        // prevent instantiation and extension.
        internal FieldValueHitQueue(SortField[] fields, int size)
            : base(size)
        {
            // When we get here, fields.length is guaranteed to be > 0, therefore no
            // need to check it again.

            // All these are required by this class's API - need to return arrays.
            // Therefore even in the case of a single comparator, create an array
            // anyway.
            this.fields = fields;
            int numComparators = fields.Length;
            comparators = new FieldComparator[numComparators];
            reverseMul = new int[numComparators];
        }

        public virtual FieldComparator[] Comparators // LUCENENET TODO: Make GetComparators() (array)
        {
            get
            {
                return comparators;
            }
        }

        public virtual int[] ReverseMul // LUCENENET TODO: Make GetReverseMul() (array)
        {
            get
            {
                return reverseMul;
            }
        }

        public virtual void SetComparator(int pos, FieldComparator comparator)
        {
            if (pos == 0)
            {
                FirstComparator = comparator;
            }
            comparators[pos] = comparator;
        }

        /// <summary>
        /// Stores the sort criteria being used. </summary>
        protected readonly SortField[] fields;

        protected readonly FieldComparator[] comparators; // use setComparator to change this array // LUCENENET TODO: Rename
        protected internal FieldComparator FirstComparator; // this must always be equal to comparators[0] // LUCENENET TODO: Make property
        protected readonly int[] reverseMul; // LUCENENET TODO: Rename

        public abstract bool LessThan(FieldValueHitQueue.Entry a, FieldValueHitQueue.Entry b);

        /// <summary>
        /// Given a queue Entry, creates a corresponding FieldDoc
        /// that contains the values used to sort the given document.
        /// These values are not the raw values out of the index, but the internal
        /// representation of them. this is so the given search hit can be collated by
        /// a MultiSearcher with other search hits.
        /// </summary>
        /// <param name="entry"> The Entry used to create a FieldDoc </param>
        /// <returns> The newly created FieldDoc </returns>
        /// <seealso cref= IndexSearcher#search(Query,Filter,int,Sort) </seealso>
        internal virtual FieldDoc FillFields(FieldValueHitQueue.Entry entry)
        {
            int n = Comparators.Length;
            IComparable[] fields = new IComparable[n];
            for (int i = 0; i < n; ++i)
            {
                fields[i] = Comparators[i].Value(entry.Slot);
            }
            //if (maxscore > 1.0f) doc.score /= maxscore;   // normalize scores
            return new FieldDoc(entry.Doc, entry.Score, fields);
        }

        /// <summary>
        /// Returns the SortFields being used by this hit queue. </summary>
        internal virtual SortField[] Fields // LUCENENET TODO: Make GetFields() (array)
        {
            get
            {
                return fields;
            }
        }
    }
}