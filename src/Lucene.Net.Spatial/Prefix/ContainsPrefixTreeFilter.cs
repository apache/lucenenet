using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Spatial.Prefix.Tree;
using Lucene.Net.Util;
using Spatial4n.Core.Shapes;
using System;
using System.Collections.Generic;
using System.Diagnostics;

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
    /// Finds docs where its indexed shape <see cref="Queries.SpatialOperation.CONTAINS"/>
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
        protected readonly bool multiOverlappingIndexedShapes;

        public ContainsPrefixTreeFilter(IShape queryShape, string fieldName, SpatialPrefixTree grid, int detailLevel, bool multiOverlappingIndexedShapes)
            : base(queryShape, fieldName, grid, detailLevel)
        {
            this.multiOverlappingIndexedShapes = multiOverlappingIndexedShapes;
        }

        public override bool Equals(object o)
        {
            if (!base.Equals(o))
                return false;
            return multiOverlappingIndexedShapes == ((ContainsPrefixTreeFilter)o).multiOverlappingIndexedShapes;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode() + (multiOverlappingIndexedShapes ? 1 : 0);
        }

        public override DocIdSet GetDocIdSet(AtomicReaderContext context, IBits acceptDocs)
        {
            return new ContainsVisitor(this, context, acceptDocs).Visit(grid.WorldCell, acceptDocs);
        }

        private class ContainsVisitor : BaseTermsEnumTraverser
        {
            public ContainsVisitor(ContainsPrefixTreeFilter outerInstance, AtomicReaderContext context, IBits acceptDocs)
                : base(outerInstance, context, acceptDocs)
            {
            }

            internal BytesRef termBytes = new BytesRef();
            internal Cell nextCell;//see getLeafDocs

            /// <remarks>This is the primary algorithm; recursive.  Returns null if finds none.</remarks>
            /// <exception cref="System.IO.IOException"></exception>
            internal SmallDocSet Visit(Cell cell, IBits acceptContains)
            {
                if (termsEnum == null)
                {
                    //signals all done
                    return null;
                }

                ContainsPrefixTreeFilter outerInstance = (ContainsPrefixTreeFilter)base.outerInstance;

                //Leaf docs match all query shape
                SmallDocSet leafDocs = GetLeafDocs(cell, acceptContains);
                // Get the AND of all child results (into combinedSubResults)
                SmallDocSet combinedSubResults = null;
                //   Optimization: use null subCellsFilter when we know cell is within the query shape.
                IShape subCellsFilter = outerInstance.queryShape;
                if (cell.Level != 0 && ((cell.ShapeRel == SpatialRelation.NOT_SET || cell.ShapeRel == SpatialRelation.WITHIN)))
                {
                    subCellsFilter = null;
                    Debug.Assert(cell.Shape.Relate(outerInstance.queryShape) == SpatialRelation.WITHIN);
                }
                ICollection<Cell> subCells = cell.GetSubCells(subCellsFilter);
                foreach (Cell subCell in subCells)
                {
                    if (!SeekExact(subCell))
                    {
                        combinedSubResults = null;
                    }
                    else if (subCell.Level == outerInstance.detailLevel)
                    {
                        combinedSubResults = GetDocs(subCell, acceptContains);
                    }
                    else if (!outerInstance.multiOverlappingIndexedShapes && 
                        subCell.ShapeRel == SpatialRelation.WITHIN)
                    {
                        combinedSubResults = GetLeafDocs(subCell, acceptContains); //recursion
                    }
                    else
                    {
                        combinedSubResults = Visit(subCell, acceptContains);
                    }
                    
                    if (combinedSubResults == null)
                    {
                        break;
                    }

                    acceptContains = combinedSubResults;//has the 'AND' effect on next iteration
                }
                
                // Result: OR the leaf docs with AND of all child results
                if (combinedSubResults != null)
                {
                    if (leafDocs == null)
                    {
                        return combinedSubResults;
                    }
                    return leafDocs.Union(combinedSubResults);//union is 'or'
                }
                return leafDocs;
            }

            private bool SeekExact(Cell cell)
            {
                Debug.Assert(new BytesRef(cell.GetTokenBytes()).CompareTo(termBytes) > 0);
                this.termBytes.Bytes = cell.GetTokenBytes();
                this.termBytes.Length = this.termBytes.Bytes.Length;
                if (termsEnum == null)
                    return false;
                return this.termsEnum.SeekExact(termBytes);
            }

            private SmallDocSet GetDocs(Cell cell, IBits acceptContains)
            {
                Debug.Assert(new BytesRef(cell.GetTokenBytes()).Equals(termBytes));
                return this.CollectDocs(acceptContains);
            }

            private Cell lastLeaf = null;//just for assertion

            private SmallDocSet GetLeafDocs(Cell leafCell, IBits acceptContains)
            {
                Debug.Assert(new BytesRef(leafCell.GetTokenBytes()).Equals(termBytes));
                Debug.Assert(!leafCell.Equals(lastLeaf));//don't call for same leaf again
                lastLeaf = leafCell;

                if (termsEnum == null)
                    return null;
                BytesRef nextTerm = this.termsEnum.Next();
                if (nextTerm == null)
                {
                    termsEnum = null;//signals all done
                    return null;
                }
                nextCell = outerInstance.grid.GetCell(nextTerm.Bytes, nextTerm.Offset, nextTerm.Length, this.nextCell);
                if (nextCell.Level == leafCell.Level && nextCell.IsLeaf)
                {
                    return CollectDocs(acceptContains);
                }
                else
                {
                    return null;
                }
            }

            private SmallDocSet CollectDocs(IBits acceptContains)
            {
                SmallDocSet set = null;

                docsEnum = termsEnum.Docs(acceptContains, docsEnum, DocsEnum.FLAG_NONE);
                int docid;
                while ((docid = docsEnum.NextDoc()) != DocIdSetIterator.NO_MORE_DOCS)
                {
                    if (set == null)
                    {
                        int size = this.termsEnum.DocFreq();
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
            private readonly SentinelIntSet intSet;
            private int maxInt = 0;

            public SmallDocSet(int size)
            {
                intSet = new SentinelIntSet(size, -1);
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
            public virtual int Length()
            {
                return maxInt;
            }

            /// <summary>Number of docids.</summary>
            public virtual int Size
            {
                get { return intSet.Size(); }
            }

            /// <summary>NOTE: modifies and returns either "this" or "other"</summary>
            public virtual SmallDocSet Union(SmallDocSet other)
            {
                SmallDocSet bigger;
                SmallDocSet smaller;
                if (other.intSet.Size() > this.intSet.Size())
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

            public override IBits GetBits()
            {
                //if the # of docids is super small, return null since iteration is going
                // to be faster
                return Size > 4 ? this : null;
            }

            private sealed class _DocIdSetIterator_225 : DocIdSetIterator
            {
                private readonly int size;
                private readonly int[] docs;

                public _DocIdSetIterator_225(int size, int[] docs)
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

                public override long Cost()
                {
                    return size;
                }
            }

            public override DocIdSetIterator GetIterator()
            {
                if (Size == 0)
                {
                    return null;
                }
                //copy the unsorted values to a new array then sort them
                int d = 0;
                int[] docs = new int[intSet.Size()];
                foreach (int v in intSet.Keys)
                {
                    if (v == intSet.EmptyVal)
                    {
                        continue;
                    }
                    docs[d++] = v;
                }
                Debug.Assert(d == intSet.Size());
                int size = d;
                //sort them
                Array.Sort(docs, 0, size);
                return new _DocIdSetIterator_225(size, docs);
            }
        }
    }
}
