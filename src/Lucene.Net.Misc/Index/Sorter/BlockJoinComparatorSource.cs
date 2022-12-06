using Lucene.Net.Search;
using Lucene.Net.Util;
using System;
using System.IO;

namespace Lucene.Net.Index.Sorter
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
    /// Helper class to sort readers that contain blocks of documents.
    /// <para>
    /// Note that this class is intended to used with <see cref="SortingMergePolicy"/>,
    /// and for other purposes has some limitations:
    /// <list type="bullet">
    ///    <item><description>Cannot yet be used with <see cref="IndexSearcher.SearchAfter(ScoreDoc, Query, Filter, int, Sort)">
    ///    IndexSearcher.SearchAfter</see></description></item>
    ///    <item><description>Filling sort field values is not yet supported.</description></item>
    /// </list>
    /// @lucene.experimental
    /// </para>
    /// </summary>
    // TODO: can/should we clean this thing up (e.g. return a proper sort value)
    // and move to the join/ module?
    public class BlockJoinComparerSource : FieldComparerSource
    {
        internal readonly Filter parentsFilter;
        internal readonly Sort parentSort;
        internal readonly Sort childSort;

        /// <summary>
        /// Create a new BlockJoinComparerSource, sorting only blocks of documents
        /// with <paramref name="parentSort"/> and not reordering children with a block.
        /// </summary>
        /// <param name="parentsFilter"> Filter identifying parent documents </param>
        /// <param name="parentSort"> Sort for parent documents </param>
        public BlockJoinComparerSource(Filter parentsFilter, Sort parentSort)
              : this(parentsFilter, parentSort, new Sort(SortField.FIELD_DOC))
        {
        }

        /// <summary>
        /// Create a new BlockJoinComparerSource, specifying the sort order for both
        /// blocks of documents and children within a block.
        /// </summary>
        /// <param name="parentsFilter"> Filter identifying parent documents </param>
        /// <param name="parentSort"> Sort for parent documents </param>
        /// <param name="childSort"> Sort for child documents in the same block </param>
        public BlockJoinComparerSource(Filter parentsFilter, Sort parentSort, Sort childSort)
        {
            this.parentsFilter = parentsFilter;
            this.parentSort = parentSort;
            this.childSort = childSort;
        }

        public override FieldComparer NewComparer(string fieldname, int numHits, int sortPos, bool reversed)
        {

            // we keep parallel slots: the parent ids and the child ids
            int[] parentSlots = new int[numHits];
            int[] childSlots = new int[numHits];

            SortField[] parentFields = parentSort.GetSort();
            int[] parentReverseMul = new int[parentFields.Length];
            FieldComparer[] parentComparers = new FieldComparer[parentFields.Length];
            for (int i = 0; i < parentFields.Length; i++)
            {
                parentReverseMul[i] = parentFields[i].IsReverse ? -1 : 1;
                parentComparers[i] = parentFields[i].GetComparer(1, i);
            }

            SortField[] childFields = childSort.GetSort();
            int[] childReverseMul = new int[childFields.Length];
            FieldComparer[] childComparers = new FieldComparer[childFields.Length];
            for (int i = 0; i < childFields.Length; i++)
            {
                childReverseMul[i] = childFields[i].IsReverse ? -1 : 1;
                childComparers[i] = childFields[i].GetComparer(1, i);
            }

            // NOTE: we could return parent ID as value but really our sort "value" is more complex...
            // So we throw UOE for now. At the moment you really should only use this at indexing time.
            return new FieldComparerAnonymousClass(this, parentSlots,
                childSlots, parentReverseMul, parentComparers, childReverseMul, childComparers);
        }

        private sealed class FieldComparerAnonymousClass : FieldComparer<J2N.Numerics.Int32>
        {
            private readonly BlockJoinComparerSource outerInstance;

            private readonly int[] parentSlots;
            private readonly int[] childSlots;
            private readonly int[] parentReverseMul;
            private readonly FieldComparer[] parentComparers;
            private readonly int[] childReverseMul;
            private readonly FieldComparer[] childComparers;

            public FieldComparerAnonymousClass(BlockJoinComparerSource outerInstance,
                int[] parentSlots, int[] childSlots, int[] parentReverseMul, FieldComparer[] parentComparers,
                int[] childReverseMul, FieldComparer[] childComparers)
            {
                this.outerInstance = outerInstance;
                this.parentSlots = parentSlots;
                this.childSlots = childSlots;
                this.parentReverseMul = parentReverseMul;
                this.parentComparers = parentComparers;
                this.childReverseMul = childReverseMul;
                this.childComparers = childComparers;
            }

            internal int bottomParent;
            internal int bottomChild;
            internal FixedBitSet parentBits;

            public override int Compare(int slot1, int slot2)
            {
                try
                {
                    return Compare(childSlots[slot1], parentSlots[slot1], childSlots[slot2], parentSlots[slot2]);
                }
                catch (Exception e) when (e.IsIOException())
                {
                    throw RuntimeException.Create(e);
                }
            }

            public override void SetBottom(int slot)
            {
                bottomParent = parentSlots[slot];
                bottomChild = childSlots[slot];
            }

            public override void SetTopValue(J2N.Numerics.Int32 value)
            {
                // we dont have enough information (the docid is needed)
                throw UnsupportedOperationException.Create("this comparer cannot be used with deep paging");
            }

            public override int CompareBottom(int doc)
            {
                return Compare(bottomChild, bottomParent, doc, Parent(doc));
            }

            public override int CompareTop(int doc)
            {
                // we dont have enough information (the docid is needed)
                throw UnsupportedOperationException.Create("this comparer cannot be used with deep paging");
            }

            public override void Copy(int slot, int doc)
            {
                childSlots[slot] = doc;
                parentSlots[slot] = Parent(doc);
            }

            public override FieldComparer SetNextReader(AtomicReaderContext context)
            {

                DocIdSet parents = outerInstance.parentsFilter.GetDocIdSet(context, null);
                if (parents is null)
                {
                    throw IllegalStateException.Create("AtomicReader " + context.AtomicReader + " contains no parents!");
                }
                if (!(parents is FixedBitSet))
                {
                    throw IllegalStateException.Create("parentFilter must return FixedBitSet; got " + parents);
                }
                parentBits = (FixedBitSet)parents;
                for (int i = 0; i < parentComparers.Length; i++)
                {
                    parentComparers[i] = parentComparers[i].SetNextReader(context);
                }
                for (int i = 0; i < childComparers.Length; i++)
                {
                    childComparers[i] = childComparers[i].SetNextReader(context);
                }
                return this;
            }

            // LUCENENET NOTE: This was value(int) in Lucene.
            public override J2N.Numerics.Int32 this[int slot] => throw
                // really our sort "value" is more complex...
                UnsupportedOperationException.Create("filling sort field values is not yet supported");

            public override void SetScorer(Scorer scorer)
            {
                base.SetScorer(scorer);
                foreach (FieldComparer comp in parentComparers)
                {
                    comp.SetScorer(scorer);
                }
                foreach (FieldComparer comp in childComparers)
                {
                    comp.SetScorer(scorer);
                }
            }

            internal int Parent(int doc)
            {
                return parentBits.NextSetBit(doc);
            }

            internal int Compare(int docID1, int parent1, int docID2, int parent2)
            {
                if (parent1 == parent2) // both are in the same block
                {
                    if (docID1 == parent1 || docID2 == parent2)
                    {
                        // keep parents at the end of blocks
                        return docID1 - docID2;
                    }
                    else
                    {
                        return Compare(docID1, docID2, childComparers, childReverseMul);
                    }
                }
                else
                {
                    int cmp = Compare(parent1, parent2, parentComparers, parentReverseMul);
                    if (cmp == 0)
                    {
                        return parent1 - parent2;
                    }
                    else
                    {
                        return cmp;
                    }
                }
            }

            internal static int Compare(int docID1, int docID2, FieldComparer[] comparers, int[] reverseMul) // LUCENENET: CA1822: Mark members as static
            {
                for (int i = 0; i < comparers.Length; i++)
                {
                    // TODO: would be better if copy() didnt cause a term lookup in TermOrdVal & co,
                    // the segments are always the same here...
                    comparers[i].Copy(0, docID1);
                    comparers[i].SetBottom(0);
                    int comp = reverseMul[i] * comparers[i].CompareBottom(docID2);
                    if (comp != 0)
                    {
                        return comp;
                    }
                }
                return 0; // no need to docid tiebreak
            }
        }

        public override string ToString()
        {
            return "blockJoin(parentSort=" + parentSort + ",childSort=" + childSort + ")";
        }
    }
}