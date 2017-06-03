using Lucene.Net.Support;
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

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

    //Used to hold non-generic nested types
    public static class FieldValueHitQueue
    {
        // had to change from internal to public, due to public accessability of FieldValueHitQueue
#if FEATURE_SERIALIZABLE
        [Serializable]
#endif
        public class Entry : ScoreDoc
        {
            public int Slot { get; set; } // LUCENENET NOTE: For some reason, this was not made readonly in the original

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
        /// there is just one comparer.
        /// </summary>
#if FEATURE_SERIALIZABLE
        [Serializable]
#endif
        internal sealed class OneComparerFieldValueHitQueue<T> : FieldValueHitQueue<T>
            where T : FieldValueHitQueue.Entry
        {
            private int oneReverseMul;

            public OneComparerFieldValueHitQueue(SortField[] fields, int size)
                : base(fields, size)
            {
                if (fields.Length == 0)
                {
                    throw new System.ArgumentException("Sort must contain at least one field");
                }

                SortField field = fields[0];
                SetComparer(0, field.GetComparer(size, 0));
                oneReverseMul = field.reverse ? -1 : 1;

                ReverseMul[0] = oneReverseMul;
            }

            /// <summary> Returns whether <c>a</c> is less relevant than <c>b</c>.</summary>
            /// <param name="hitA">ScoreDoc</param>
            /// <param name="hitB">ScoreDoc</param>
            /// <returns><c>true</c> if document <c>a</c> should be sorted after document <c>b</c>.</returns>
            protected internal override bool LessThan(T hitA, T hitB)
            {
                Debug.Assert(hitA != hitB);
                Debug.Assert(hitA.Slot != hitB.Slot);

                int c = oneReverseMul * m_firstComparer.Compare(hitA.Slot, hitB.Slot);
                if (c != 0)
                {
                    return c > 0;
                }

                // avoid random sort order that could lead to duplicates (bug #31241):
                return hitA.Doc > hitB.Doc;
            }
        }

        /// <summary> An implementation of <see cref="FieldValueHitQueue" /> which is optimized in case
        /// there is more than one comparer.
        /// </summary>
#if FEATURE_SERIALIZABLE
        [Serializable]
#endif
        internal sealed class MultiComparersFieldValueHitQueue<T> : FieldValueHitQueue<T>
            where T : FieldValueHitQueue.Entry
        {
            public MultiComparersFieldValueHitQueue(SortField[] fields, int size)
                : base(fields, size)
            {
                int numComparers = m_comparers.Length;
                for (int i = 0; i < numComparers; ++i)
                {
                    SortField field = fields[i];

                    m_reverseMul[i] = field.reverse ? -1 : 1;
                    SetComparer(i, field.GetComparer(size, i));
                }
            }

            protected internal override bool LessThan(T hitA, T hitB)
            {
                Debug.Assert(hitA != hitB);
                Debug.Assert(hitA.Slot != hitB.Slot);

                int numComparers = m_comparers.Length;
                for (int i = 0; i < numComparers; ++i)
                {
                    int c = m_reverseMul[i] * m_comparers[i].Compare(hitA.Slot, hitB.Slot);
                    if (c != 0)
                    {
                        // Short circuit
                        return c > 0;
                    }
                }

                // avoid random sort order that could lead to duplicates (bug #31241):
                return hitA.Doc > hitB.Doc;
            }
        }

        /// <summary> Creates a hit queue sorted by the given list of fields.
        /// <para/><b>NOTE</b>: The instances returned by this method
        /// pre-allocate a full array of length <c>numHits</c>.
        /// </summary>
        /// <param name="fields"><see cref="SortField"/> array we are sorting by in priority order (highest
        /// priority first); cannot be <c>null</c> or empty
        /// </param>
        /// <param name="size">The number of hits to retain. Must be greater than zero.
        /// </param>
        /// <exception cref="System.IO.IOException">If there is a low-level IO error</exception>
        public static FieldValueHitQueue<T> Create<T>(SortField[] fields, int size)
            where T : FieldValueHitQueue.Entry
        {
            if (fields.Length == 0)
            {
                throw new ArgumentException("Sort must contain at least one field");
            }

            if (fields.Length == 1)
            {
                return new FieldValueHitQueue.OneComparerFieldValueHitQueue<T>(fields, size);
            }
            else
            {
                return new FieldValueHitQueue.MultiComparersFieldValueHitQueue<T>(fields, size);
            }
        }
    }

    /// <summary>
    /// Expert: A hit queue for sorting by hits by terms in more than one field.
    /// Uses <c>FieldCache.DEFAULT</c> for maintaining
    /// internal term lookup tables.
    /// <para/>
    /// @lucene.experimental
    /// @since 2.9 </summary>
    /// <seealso cref="IndexSearcher.Search(Query,Filter,int,Sort)"/>
    /// <seealso cref="FieldCache"/>
#if FEATURE_SERIALIZABLE
    [Serializable]
#endif
    public abstract class FieldValueHitQueue<T> : Util.PriorityQueue<T>
        where T : FieldValueHitQueue.Entry
    {
        // prevent instantiation and extension.
        internal FieldValueHitQueue(SortField[] fields, int size)
            : base(size)
        {
            // When we get here, fields.length is guaranteed to be > 0, therefore no
            // need to check it again.

            // All these are required by this class's API - need to return arrays.
            // Therefore even in the case of a single comparer, create an array
            // anyway.
            this.m_fields = fields;
            int numComparers = fields.Length;
            m_comparers = new FieldComparer[numComparers];
            m_reverseMul = new int[numComparers];
        }

        [WritableArray]
        [SuppressMessage("Microsoft.Performance", "CA1819", Justification = "Lucene's design requires some writable array properties")]
        public virtual FieldComparer[] Comparers
        {
            get { return m_comparers; }
        }

        [WritableArray]
        [SuppressMessage("Microsoft.Performance", "CA1819", Justification = "Lucene's design requires some writable array properties")]
        public virtual int[] ReverseMul
        {
            get { return m_reverseMul; }
        }

        public virtual void SetComparer(int pos, FieldComparer comparer)
        {
            if (pos == 0)
            {
                m_firstComparer = comparer;
            }
            m_comparers[pos] = comparer;
        }

        /// <summary>
        /// Stores the sort criteria being used. </summary>
        protected readonly SortField[] m_fields;

        protected readonly FieldComparer[] m_comparers; // use setComparer to change this array
        protected FieldComparer m_firstComparer; // this must always be equal to comparers[0]
        protected readonly int[] m_reverseMul;

        internal FieldComparer FirstComparer
        {
            get { return this.m_firstComparer; }
        }

        // LUCENENET NOTE: We don't need this declaration because we are using
        // a generic constraint on T
        //public abstract bool LessThan(FieldValueHitQueue.Entry a, FieldValueHitQueue.Entry b);

        /// <summary>
        /// Given a queue <see cref="FieldValueHitQueue.Entry"/>, creates a corresponding <see cref="FieldDoc"/>
        /// that contains the values used to sort the given document.
        /// These values are not the raw values out of the index, but the internal
        /// representation of them. This is so the given search hit can be collated by
        /// a MultiSearcher with other search hits.
        /// </summary>
        /// <param name="entry"> The <see cref="FieldValueHitQueue.Entry"/> used to create a <see cref="FieldDoc"/> </param>
        /// <returns> The newly created <see cref="FieldDoc"/> </returns>
        /// <seealso cref="IndexSearcher.Search(Query,Filter,int,Sort)"/>
        internal virtual FieldDoc FillFields(FieldValueHitQueue.Entry entry)
        {
            int n = m_comparers.Length;
            IComparable[] fields = new IComparable[n];
            for (int i = 0; i < n; ++i)
            {
                fields[i] = m_comparers[i][entry.Slot];
            }
            //if (maxscore > 1.0f) doc.score /= maxscore;   // normalize scores
            return new FieldDoc(entry.Doc, entry.Score, fields);
        }

        /// <summary>
        /// Returns the <see cref="SortField"/>s being used by this hit queue. </summary>
        [WritableArray]
        [SuppressMessage("Microsoft.Performance", "CA1819", Justification = "Lucene's design requires some writable array properties")]
        internal virtual SortField[] Fields
        {
            get { return m_fields; }
        }
    }
}