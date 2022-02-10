// Lucene version compatibility level 4.8.1
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Util;
using System;

namespace Lucene.Net.Join
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
    /// A field comparer that allows parent documents to be sorted by fields
    /// from the nested / child documents.
    /// 
    /// @lucene.experimental
    /// </summary>
    [Obsolete("Use Lucene.Net.Search.Join.ToParentBlockJoinFieldComparer instead. This class will be removed in 4.8.0 release candidate."), System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public abstract class ToParentBlockJoinFieldComparer : FieldComparer<object>
    {
        private readonly Filter _parentFilter;
        private readonly Filter _childFilter;
        private readonly int _spareSlot;

        private FieldComparer _wrappedComparer;
        private FixedBitSet _parentDocuments;
        private FixedBitSet _childDocuments;

        private ToParentBlockJoinFieldComparer(FieldComparer wrappedComparer, Filter parentFilter, Filter childFilter, int spareSlot)
        {
            _wrappedComparer = wrappedComparer;
            _parentFilter = parentFilter;
            _childFilter = childFilter;
            _spareSlot = spareSlot;
        }

        public override int Compare(int slot1, int slot2)
        {
            return _wrappedComparer.Compare(slot1, slot2);
        }

        public override void SetBottom(int slot)
        {
            _wrappedComparer.SetBottom(slot);
        }

        public override void SetTopValue(object value)
        {
            _wrappedComparer.SetTopValue(value);
        }

        public override FieldComparer SetNextReader(AtomicReaderContext context)
        {
            DocIdSet innerDocuments = _childFilter.GetDocIdSet(context, null);
            if (IsEmpty(innerDocuments))
            {
                _childDocuments = null;
            }
            else if (innerDocuments is FixedBitSet fixedBitSet)
            {
                _childDocuments = fixedBitSet;
            }
            else
            {
                DocIdSetIterator iterator = innerDocuments.GetIterator();
                _childDocuments = iterator != null ? ToFixedBitSet(iterator, context.AtomicReader.MaxDoc) : null;
            }
            DocIdSet rootDocuments = _parentFilter.GetDocIdSet(context, null);
            if (IsEmpty(rootDocuments))
            {
                _parentDocuments = null;
            }
            else if (rootDocuments is FixedBitSet fixedBitSet)
            {
                _parentDocuments = fixedBitSet;
            }
            else
            {
                DocIdSetIterator iterator = rootDocuments.GetIterator();
                _parentDocuments = iterator != null ? ToFixedBitSet(iterator, context.AtomicReader.MaxDoc) : null;
            }

            _wrappedComparer = _wrappedComparer.SetNextReader(context);
            return this;
        }

        private static bool IsEmpty(DocIdSet set)
        {
            return set is null;
        }

        private static FixedBitSet ToFixedBitSet(DocIdSetIterator iterator, int numBits)
        {
            var set = new FixedBitSet(numBits);
            int doc;
            while ((doc = iterator.NextDoc()) != DocIdSetIterator.NO_MORE_DOCS)
            {
                set.Set(doc);
            }
            return set;
        }

        // LUCENENET NOTE: This was value(int) in Lucene.
        public override object this[int slot] => _wrappedComparer.GetValue(slot);

        /// <summary>
        /// Concrete implementation of <see cref="ToParentBlockJoinSortField"/> to sorts the parent docs with the lowest values
        /// in the child / nested docs first.
        /// </summary>
        public sealed class Lowest : ToParentBlockJoinFieldComparer
        {
            /// <summary>
            /// Create <see cref="ToParentBlockJoinFieldComparer.Lowest"/>
            /// </summary>
            /// <param name="wrappedComparer">The <see cref="FieldComparer"/> on the child / nested level. </param>
            /// <param name="parentFilter"><see cref="Filter"/> (must produce <see cref="FixedBitSet"/> per-segment) that identifies the parent documents. </param>
            /// <param name="childFilter"><see cref="Filter"/> that defines which child / nested documents participates in sorting. </param>
            /// <param name="spareSlot">The extra slot inside the wrapped comparer that is used to compare which nested document
            ///                  inside the parent document scope is most competitive. </param>
            public Lowest(FieldComparer wrappedComparer, Filter parentFilter, Filter childFilter, int spareSlot)
                : base(wrappedComparer, parentFilter, childFilter, spareSlot)
            {
            }

            public override int CompareBottom(int parentDoc)
            {
                if (parentDoc == 0 || _parentDocuments is null || _childDocuments is null)
                {
                    return 0;
                }

                // We need to copy the lowest value from all child docs into slot.
                int prevParentDoc = _parentDocuments.PrevSetBit(parentDoc - 1);
                int childDoc = _childDocuments.NextSetBit(prevParentDoc + 1);
                if (childDoc >= parentDoc || childDoc == -1)
                {
                    return 0;
                }

                // We only need to emit a single cmp value for any matching child doc
                int cmp = _wrappedComparer.CompareBottom(childDoc);
                if (cmp > 0)
                {
                    return cmp;
                }

                while (true)
                {
                    childDoc = _childDocuments.NextSetBit(childDoc + 1);
                    if (childDoc >= parentDoc || childDoc == -1)
                    {
                        return cmp;
                    }
                    int cmp1 = _wrappedComparer.CompareBottom(childDoc);
                    if (cmp1 > 0)
                    {
                        return cmp1;
                    }
                    if (cmp1 == 0)
                    {
                        cmp = 0;
                    }
                }
            }

            public override void Copy(int slot, int parentDoc)
            {
                if (parentDoc == 0 || _parentDocuments is null || _childDocuments is null)
                {
                    return;
                }

                // We need to copy the lowest value from all child docs into slot.
                int prevParentDoc = _parentDocuments.PrevSetBit(parentDoc - 1);
                int childDoc = _childDocuments.NextSetBit(prevParentDoc + 1);
                if (childDoc >= parentDoc || childDoc == -1)
                {
                    return;
                }
                _wrappedComparer.Copy(_spareSlot, childDoc);
                _wrappedComparer.Copy(slot, childDoc);

                while (true)
                {
                    childDoc = _childDocuments.NextSetBit(childDoc + 1);
                    if (childDoc >= parentDoc || childDoc == -1)
                    {
                        return;
                    }
                    _wrappedComparer.Copy(_spareSlot, childDoc);
                    if (_wrappedComparer.Compare(_spareSlot, slot) < 0)
                    {
                        _wrappedComparer.Copy(slot, childDoc);
                    }
                }
            }

            public override int CompareTop(int parentDoc)
            {
                if (parentDoc == 0 || _parentDocuments is null || _childDocuments is null)
                {
                    return 0;
                }

                // We need to copy the lowest value from all nested docs into slot.
                int prevParentDoc = _parentDocuments.PrevSetBit(parentDoc - 1);
                int childDoc = _childDocuments.NextSetBit(prevParentDoc + 1);
                if (childDoc >= parentDoc || childDoc == -1)
                {
                    return 0;
                }

                // We only need to emit a single cmp value for any matching child doc
                int cmp = _wrappedComparer.CompareBottom(childDoc);
                if (cmp > 0)
                {
                    return cmp;
                }

                while (true)
                {
                    childDoc = _childDocuments.NextSetBit(childDoc + 1);
                    if (childDoc >= parentDoc || childDoc == -1)
                    {
                        return cmp;
                    }
                    int cmp1 = _wrappedComparer.CompareTop(childDoc);
                    if (cmp1 > 0)
                    {
                        return cmp1;
                    }
                    if (cmp1 == 0)
                    {
                        cmp = 0;
                    }
                }
            }

        }

        /// <summary>
        /// Concrete implementation of <see cref="ToParentBlockJoinSortField"/> to sorts the parent docs with the highest values
        /// in the child / nested docs first.
        /// </summary>
        public sealed class Highest : ToParentBlockJoinFieldComparer
        {
            /// <summary>
            /// Create <see cref="ToParentBlockJoinFieldComparer.Highest"/>
            /// </summary>
            /// <param name="wrappedComparer">The <see cref="FieldComparer"/> on the child / nested level. </param>
            /// <param name="parentFilter"><see cref="Filter"/> (must produce <see cref="FixedBitSet"/> per-segment) that identifies the parent documents. </param>
            /// <param name="childFilter"><see cref="Filter"/> that defines which child / nested documents participates in sorting. </param>
            /// <param name="spareSlot">The extra slot inside the wrapped comparer that is used to compare which nested document
            ///                  inside the parent document scope is most competitive. </param>
            public Highest(FieldComparer wrappedComparer, Filter parentFilter, Filter childFilter, int spareSlot)
                : base(wrappedComparer, parentFilter, childFilter, spareSlot)
            {
            }

            public override int CompareBottom(int parentDoc)
            {
                if (parentDoc == 0 || _parentDocuments is null || _childDocuments is null)
                {
                    return 0;
                }

                int prevParentDoc = _parentDocuments.PrevSetBit(parentDoc - 1);
                int childDoc = _childDocuments.NextSetBit(prevParentDoc + 1);
                if (childDoc >= parentDoc || childDoc == -1)
                {
                    return 0;
                }

                int cmp = _wrappedComparer.CompareBottom(childDoc);
                if (cmp < 0)
                {
                    return cmp;
                }

                while (true)
                {
                    childDoc = _childDocuments.NextSetBit(childDoc + 1);
                    if (childDoc >= parentDoc || childDoc == -1)
                    {
                        return cmp;
                    }
                    int cmp1 = _wrappedComparer.CompareBottom(childDoc);
                    if (cmp1 < 0)
                    {
                        return cmp1;
                    }
                    else
                    {
                        if (cmp1 == 0)
                        {
                            cmp = 0;
                        }
                    }
                }
            }

            public override void Copy(int slot, int parentDoc)
            {
                if (parentDoc == 0 || _parentDocuments is null || _childDocuments is null)
                {
                    return;
                }

                int prevParentDoc = _parentDocuments.PrevSetBit(parentDoc - 1);
                int childDoc = _childDocuments.NextSetBit(prevParentDoc + 1);
                if (childDoc >= parentDoc || childDoc == -1)
                {
                    return;
                }
                _wrappedComparer.Copy(_spareSlot, childDoc);
                _wrappedComparer.Copy(slot, childDoc);

                while (true)
                {
                    childDoc = _childDocuments.NextSetBit(childDoc + 1);
                    if (childDoc >= parentDoc || childDoc == -1)
                    {
                        return;
                    }
                    _wrappedComparer.Copy(_spareSlot, childDoc);
                    if (_wrappedComparer.Compare(_spareSlot, slot) > 0)
                    {
                        _wrappedComparer.Copy(slot, childDoc);
                    }
                }
            }

            public override int CompareTop(int parentDoc)
            {
                if (parentDoc == 0 || _parentDocuments is null || _childDocuments is null)
                {
                    return 0;
                }

                int prevParentDoc = _parentDocuments.PrevSetBit(parentDoc - 1);
                int childDoc = _childDocuments.NextSetBit(prevParentDoc + 1);
                if (childDoc >= parentDoc || childDoc == -1)
                {
                    return 0;
                }

                int cmp = _wrappedComparer.CompareBottom(childDoc);
                if (cmp < 0)
                {
                    return cmp;
                }

                while (true)
                {
                    childDoc = _childDocuments.NextSetBit(childDoc + 1);
                    if (childDoc >= parentDoc || childDoc == -1)
                    {
                        return cmp;
                    }
                    int cmp1 = _wrappedComparer.CompareTop(childDoc);
                    if (cmp1 < 0)
                    {
                        return cmp1;
                    }
                    if (cmp1 == 0)
                    {
                        cmp = 0;
                    }
                }
            }
        }
    }
}