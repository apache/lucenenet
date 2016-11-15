/* 
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
 * (the "License"); you may not use this file except in compliance with
 * the License.  You may obtain a copy of the License at
 * 
 * http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */
using System;
using System.Collections.Generic;
using Lucene.Net.Spatial.Util;
using Spatial4n.Core.Shapes;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Spatial.Prefix;
using Lucene.Net.Spatial.Prefix.Tree;
using Lucene.Net.Spatial.Query;
using Lucene.Net.Util;

namespace Lucene.Net.Spatial.Prefix
{
    /// <summary>
    /// Finds docs where its indexed shape
    /// <see cref="SpatialOperation.Contains">CONTAINS</see>
    /// the query shape. For use on
    /// <see cref="RecursivePrefixTreeStrategy">RecursivePrefixTreeStrategy</see>
    /// .
    /// </summary>
    /// <lucene.experimental></lucene.experimental>
    public class ContainsPrefixTreeFilter : AbstractPrefixTreeFilter
    {
        protected readonly bool multiOverlappingIndexedShapes;

        public ContainsPrefixTreeFilter(IShape queryShape, string fieldName, SpatialPrefixTree grid, int detailLevel, bool multiOverlappingIndexedShapes)
            : base(queryShape, fieldName, grid, detailLevel)
        {
            this.multiOverlappingIndexedShapes = multiOverlappingIndexedShapes;
        }

        /// <exception cref="System.IO.IOException"></exception>
        public override DocIdSet GetDocIdSet(AtomicReaderContext context, Bits acceptDocs)
        {
            return new ContainsVisitor(this, context, acceptDocs).Visit(grid.WorldCell, acceptDocs);
        }

        private class ContainsVisitor : BaseTermsEnumTraverser
        {
            private readonly IShape queryShape;
            private readonly int detailLevel;
            private readonly bool multiOverlappingIndexedShapes;
            private SpatialPrefixTree grid;

            /// <exception cref="System.IO.IOException"></exception>
            public ContainsVisitor(ContainsPrefixTreeFilter outerInstance, AtomicReaderContext context, Bits acceptDocs)
                : base(outerInstance, context, acceptDocs)
            {
                this.queryShape = outerInstance.queryShape;
                this.detailLevel = outerInstance.detailLevel;
                this.grid = outerInstance.grid;
                this.multiOverlappingIndexedShapes = outerInstance.multiOverlappingIndexedShapes;
            }

            internal BytesRef termBytes = new BytesRef();

            internal Cell nextCell;//see getLeafDocs

            /// <remarks>This is the primary algorithm; recursive.  Returns null if finds none.</remarks>
            /// <exception cref="System.IO.IOException"></exception>
            internal SmallDocSet Visit(Cell cell, Bits acceptContains)
            {
                if (termsEnum == null)
                {
                    //signals all done
                    return null;
                }
                //Leaf docs match all query shape
                SmallDocSet leafDocs = GetLeafDocs(cell, acceptContains);
                // Get the AND of all child results
                SmallDocSet combinedSubResults = null;

                //   Optimization: use null subCellsFilter when we know cell is within the query shape.
                IShape subCellsFilter = queryShape;
                if (cell.Level != 0 && ((cell.GetShapeRel() == null || cell.GetShapeRel() == SpatialRelation.WITHIN)))
                {
                    subCellsFilter = null;
                    System.Diagnostics.Debug.Assert(cell.GetShape().Relate(queryShape) == SpatialRelation.WITHIN);
                }
                ICollection<Cell> subCells = cell.GetSubCells(subCellsFilter);
                foreach (Cell subCell in subCells)
                {
                    if (!SeekExact(subCell))
                    {
                        combinedSubResults = null;
                    }
                    else if (subCell.Level == detailLevel)
                    {
                        combinedSubResults = GetDocs(subCell, acceptContains);
                    }
                    else if (!multiOverlappingIndexedShapes && 
                        subCell.GetShapeRel() == SpatialRelation.WITHIN)
                    {
                        combinedSubResults = GetLeafDocs(subCell, acceptContains);
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
                    return leafDocs.Union(combinedSubResults);
                }
                return leafDocs;
            }

            /// <exception cref="System.IO.IOException"></exception>
            private bool SeekExact(Cell cell)
            {
                System.Diagnostics.Debug.Assert(new BytesRef(cell.GetTokenBytes()).CompareTo(termBytes) > 0);
                this.termBytes.Bytes = cell.GetTokenBytes();
                this.termBytes.Length = this.termBytes.Bytes.Length;
                if (termsEnum == null)
                    return false;
                return this.termsEnum.SeekExact(termBytes);
            }

            /// <exception cref="System.IO.IOException"></exception>
            private SmallDocSet GetDocs(Cell cell, Bits acceptContains)
            {
                System.Diagnostics.Debug.Assert(new BytesRef(cell.GetTokenBytes()).Equals(termBytes));
                return this.CollectDocs(acceptContains);
            }

            private Cell lastLeaf = null;//just for assertion

            /// <exception cref="System.IO.IOException"></exception>
            private SmallDocSet GetLeafDocs(Cell leafCell, Bits acceptContains)
            {
                System.Diagnostics.Debug.Assert(new BytesRef(leafCell.GetTokenBytes()).Equals(termBytes));
                System.Diagnostics.Debug.Assert(leafCell.Equals(lastLeaf));//don't call for same leaf again
                lastLeaf = leafCell;

                if (termsEnum == null)
                    return null;
                BytesRef nextTerm = this.termsEnum.Next();
                if (nextTerm == null)
                {
                    termsEnum = null;
                    //signals all done
                    return null;
                }
                nextCell = grid.GetCell(nextTerm.Bytes, nextTerm.Offset, nextTerm.Length, this.nextCell);
                if (nextCell.Level == leafCell.Level && nextCell.IsLeaf())
                {
                    return CollectDocs(acceptContains);
                }
                else
                {
                    return null;
                }
            }

            /// <exception cref="System.IO.IOException"></exception>
            private SmallDocSet CollectDocs(Bits acceptContains)
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
        private class SmallDocSet : DocIdSet, Bits
        {
            private readonly SentinelIntSet intSet;

            private int maxInt = 0;

            public SmallDocSet(int size)
            {
                intSet = new SentinelIntSet(size, -1);
            }

            public bool Get(int index)
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

            int Bits.Length()
            {
                return maxInt;
            }

            /// <summary>Number of docids.</summary>
            /// <remarks>Number of docids.</remarks>
            public virtual int Size()
            {
                return intSet.Size();
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

            public override Bits GetBits()
            {
                //if the # of docids is super small, return null since iteration is going
                // to be faster
                return Size() > 4 ? this : null;
            }

            private sealed class _DocIdSetIterator_225 : DocIdSetIterator
            {
                private readonly int size;
                private readonly int[] docs;

                public _DocIdSetIterator_225(int size, int[] docs)
                {
                    this.size = size;
                    this.docs = docs;
                    this.idx = -1;
                }

                internal int idx;

                public override int DocID()
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

                /// <exception cref="System.IO.IOException"></exception>
                public override int NextDoc()
                {
                    if (++idx < size)
                    {
                        return docs[idx];
                    }
                    return NO_MORE_DOCS;
                }

                /// <exception cref="System.IO.IOException"></exception>
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
                if (Size() == 0)
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
                System.Diagnostics.Debug.Assert(d == intSet.Size());
                int size = d;
                //sort them
                Array.Sort(docs, 0, size);
                return new _DocIdSetIterator_225(size, docs);
            }
        }
    }
}
