using Lucene.Net.Diagnostics;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Spatial.Prefix.Tree;
using Lucene.Net.Util;
using Spatial4n.Shapes;
using System;
using System.Collections.Generic;
using System.IO;

namespace Lucene.Net.Spatial.Prefix
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
    /// Finds docs where its indexed shape <see cref="Queries.SpatialOperation.Contains"/>
    /// the query shape. For use on <see cref="RecursivePrefixTreeStrategy"/>.
    /// 
    /// @lucene.experimental
    /// </summary>
    public class ContainsPrefixTreeFilter : AbstractPrefixTreeFilter
    {
        // Future optimizations:
        //   Instead of seekExact, use seekCeil with some leap-frogging, like Intersects does.

        /// <summary>
        /// If the spatial data for a document is comprised of multiple overlapping or adjacent parts,
        /// it might fail to match a query shape when doing the CONTAINS predicate when the sum of
        /// those shapes contain the query shape but none do individually. Set this to false to
        /// increase performance if you don't care about that circumstance (such as if your indexed
        /// data doesn't even have such conditions).  See LUCENE-5062.
        /// </summary>
        protected readonly bool m_multiOverlappingIndexedShapes;

        public ContainsPrefixTreeFilter(IShape queryShape, string fieldName, SpatialPrefixTree grid, int detailLevel, bool multiOverlappingIndexedShapes)
            : base(queryShape, fieldName, grid, detailLevel)
        {
            this.m_multiOverlappingIndexedShapes = multiOverlappingIndexedShapes;
        }

        public override bool Equals(object? o)
        {
            if (!base.Equals(o))
                return false;
            return m_multiOverlappingIndexedShapes == ((ContainsPrefixTreeFilter)o).m_multiOverlappingIndexedShapes;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode() + (m_multiOverlappingIndexedShapes ? 1 : 0);
        }

        public override DocIdSet? GetDocIdSet(AtomicReaderContext context, IBits acceptDocs)
        {
            return new ContainsVisitor(this, context, acceptDocs).Visit(m_grid.WorldCell, acceptDocs);
        }

        private class ContainsVisitor : BaseTermsEnumTraverser
        {
            public ContainsVisitor(ContainsPrefixTreeFilter outerInstance, AtomicReaderContext context, IBits acceptDocs)
                : base(outerInstance, context, acceptDocs)
            {
            }

            internal BytesRef termBytes = new BytesRef();
            internal Cell? nextCell;//see getLeafDocs

            /// <remarks>This is the primary algorithm; recursive.  Returns null if finds none.</remarks>
            /// <exception cref="IOException"></exception>
            internal SmallDocSet? Visit(Cell cell, IBits acceptContains)
            {
                if (m_termsEnum is null)
                {
                    //signals all done
                    return null;
                }

                ContainsPrefixTreeFilter outerInstance = (ContainsPrefixTreeFilter)base.m_filter;

                //Leaf docs match all query shape
                SmallDocSet? leafDocs = GetLeafDocs(cell, acceptContains);
                // Get the AND of all child results (into combinedSubResults)
                SmallDocSet? combinedSubResults = null;
                //   Optimization: use null subCellsFilter when we know cell is within the query shape.
                IShape? subCellsFilter = outerInstance.m_queryShape;
                if (cell.Level != 0 && ((cell.ShapeRel == SpatialRelation.None || cell.ShapeRel == SpatialRelation.Within)))
                {
                    subCellsFilter = null;
                    if (Debugging.AssertsEnabled) Debugging.Assert(cell.Shape.Relate(outerInstance.m_queryShape) == SpatialRelation.Within);
                }
                ICollection<Cell> subCells = cell.GetSubCells(subCellsFilter);
                foreach (Cell subCell in subCells)
                {
                    if (!SeekExact(subCell))
                    {
                        combinedSubResults = null;
                    }
                    else if (subCell.Level == outerInstance.m_detailLevel)
                    {
                        combinedSubResults = GetDocs(subCell, acceptContains);
                    }
                    else if (!outerInstance.m_multiOverlappingIndexedShapes && 
                        subCell.ShapeRel == SpatialRelation.Within)
                    {
                        combinedSubResults = GetLeafDocs(subCell, acceptContains); //recursion
                    }
                    else
                    {
                        combinedSubResults = Visit(subCell, acceptContains);
                    }
                    
                    if (combinedSubResults is null)
                    {
                        break;
                    }

                    acceptContains = combinedSubResults;//has the 'AND' effect on next iteration
                }
                
                // Result: OR the leaf docs with AND of all child results
                if (combinedSubResults != null)
                {
                    if (leafDocs is null)
                    {
                        return combinedSubResults;
                    }
                    return leafDocs.Union(combinedSubResults);//union is 'or'
                }
                return leafDocs;
            }

            private bool SeekExact(Cell cell)
            {
                if (Debugging.AssertsEnabled) Debugging.Assert(new BytesRef(cell.GetTokenBytes()).CompareTo(termBytes) > 0);
                this.termBytes.Bytes = cell.GetTokenBytes();
                this.termBytes.Length = this.termBytes.Bytes.Length;
                if (m_termsEnum is null)
                    return false;
                return this.m_termsEnum.SeekExact(termBytes);
            }

            private SmallDocSet? GetDocs(Cell cell, IBits acceptContains)
            {
                if (Debugging.AssertsEnabled) Debugging.Assert(new BytesRef(cell.GetTokenBytes()).Equals(termBytes));
                return this.CollectDocs(acceptContains);
            }

            private Cell? lastLeaf = null;//just for assertion

            private SmallDocSet? GetLeafDocs(Cell leafCell, IBits acceptContains)
            {
                if (Debugging.AssertsEnabled)
                {
                    Debugging.Assert(new BytesRef(leafCell.GetTokenBytes()).Equals(termBytes));
                    Debugging.Assert(!leafCell.Equals(lastLeaf));//don't call for same leaf again
                }
                lastLeaf = leafCell;

                if (m_termsEnum is null)
                    return null;
                if (!m_termsEnum.MoveNext())
                {
                    m_termsEnum = null;//signals all done
                    return null;
                }
                BytesRef nextTerm = m_termsEnum.Term;
                nextCell = m_filter.m_grid.GetCell(nextTerm.Bytes, nextTerm.Offset, nextTerm.Length, this.nextCell);
                if (nextCell.Level == leafCell.Level && nextCell.IsLeaf)
                {
                    return CollectDocs(acceptContains);
                }
                else
                {
                    return null;
                }
            }

            private SmallDocSet? CollectDocs(IBits acceptContains)
            {
                // LUCENENET specific - guard against null m_termsEnum
                if (m_termsEnum is null)
                {
                    //signals all done
                    return null;
                }

                SmallDocSet? set = null;

                m_docsEnum = m_termsEnum.Docs(acceptContains, m_docsEnum, DocsFlags.NONE);
                int docid;
                while ((docid = m_docsEnum.NextDoc()) != DocIdSetIterator.NO_MORE_DOCS)
                {
                    if (set is null)
                    {
                        int size = this.m_termsEnum.DocFreq;
                        if (size <= 0)
                        {
                            size = 16;
                        }
                        set = new SmallDocSet(size);
                    }
                    set.Set(docid);
                }
                return set;
            }
        }//class ContainsVisitor

        /// <summary>A hash based mutable set of docIds.</summary>
        /// <remarks>
        /// A hash based mutable set of docIds. If this were Solr code then we might
        /// use a combination of HashDocSet and SortedIntDocSet instead.
        /// </remarks>
        private class SmallDocSet : DocIdSet, IBits
        {
            private readonly SentinelInt32Set intSet;
            private int maxInt = 0;

            public SmallDocSet(int size)
            {
                intSet = new SentinelInt32Set(size, -1);
            }

            public virtual bool Get(int index)
            {
                return intSet.Exists(index);
            }

            public virtual void Set(int index)
            {
                intSet.Put(index);
                if (index > maxInt)
                {
                    maxInt = index;
                }
            }

            /// <summary>Largest docid.</summary>
            public virtual int Length => maxInt;

            /// <summary>
            /// Number of docids.
            /// NOTE: This was size() in Lucene.
            /// </summary>
            public virtual int Count => intSet.Count;

            /// <summary>NOTE: modifies and returns either "this" or "other"</summary>
            /// <exception cref="ArgumentNullException"><paramref name="other"/> is <c>null</c>.</exception>
            public virtual SmallDocSet Union(SmallDocSet other)
            {
                if (other is null)
                    throw new ArgumentNullException(nameof(other));

                SmallDocSet bigger;
                SmallDocSet smaller;
                if (other.intSet.Count > this.intSet.Count)
                {
                    bigger = other;
                    smaller = this;
                }
                else
                {
                    bigger = this;
                    smaller = other;
                }
                //modify bigger
                foreach (int v in smaller.intSet.Keys)
                {
                    if (v == smaller.intSet.EmptyVal)
                    {
                        continue;
                    }
                    bigger.Set(v);
                }
                return bigger;
            }

            public override IBits? Bits =>
                //if the # of docids is super small, return null since iteration is going
                // to be faster
                Count > 4 ? this : null;

            public override DocIdSetIterator? GetIterator()
            {
                if (Count == 0)
                {
                    return null;
                }
                //copy the unsorted values to a new array then sort them
                int d = 0;
                int[] docs = new int[intSet.Count];
                foreach (int v in intSet.Keys)
                {
                    if (v == intSet.EmptyVal)
                    {
                        continue;
                    }
                    docs[d++] = v;
                }
                if (Debugging.AssertsEnabled) Debugging.Assert(d == intSet.Count);
                int size = d;
                //sort them
                Array.Sort(docs, 0, size);
                return new DocIdSetIteratorAnonymousClass(size, docs);
            }

            #region Nested Type: DocIdSetIteratorAnonymousClass

            private sealed class DocIdSetIteratorAnonymousClass : DocIdSetIterator
            {
                private readonly int size;
                private readonly int[] docs;

                public DocIdSetIteratorAnonymousClass(int size, int[] docs)
                {
                    this.size = size;
                    this.docs = docs;
                }

                internal int idx = -1;

                public override int DocID
                {
                    get
                    {
                        if (idx >= 0 && idx < size)
                        {
                            return docs[idx];
                        }
                        else
                        {
                            return -1;
                        }
                    }
                }

                public override int NextDoc()
                {
                    if (++idx < size)
                    {
                        return docs[idx];
                    }
                    return NO_MORE_DOCS;
                }

                public override int Advance(int target)
                {
                    //for this small set this is likely faster vs. a binary search
                    // into the sorted array
                    return SlowAdvance(target);
                }

                public override long GetCost()
                {
                    return size;
                }
            }

            #endregion
        }
    }
}
