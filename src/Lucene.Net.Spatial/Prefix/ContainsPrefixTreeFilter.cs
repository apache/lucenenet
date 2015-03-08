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
using Lucene.Net.Spatial.Queries;
using Lucene.Net.Spatial.Util;
using Spatial4n.Core.Shapes;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Spatial.Prefix;
using Lucene.Net.Spatial.Prefix.Tree;
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
        public ContainsPrefixTreeFilter(Shape queryShape, string
             fieldName, SpatialPrefixTree grid, int detailLevel)
            : base(queryShape, fieldName, grid, detailLevel)
        {
        }

        /// <exception cref="System.IO.IOException"></exception>
        public override DocIdSet GetDocIdSet(AtomicReaderContext context, IBits acceptDocs
            )
        {
            return new ContainsVisitor(this, context, acceptDocs).Visit(grid.WorldCell, acceptDocs);
        }

        private class ContainsVisitor : BaseTermsEnumTraverser
        {
            /// <exception cref="System.IO.IOException"></exception>
            public ContainsVisitor(ContainsPrefixTreeFilter _enclosing, AtomicReaderContext context
                , IBits acceptDocs)
                : base(_enclosing, context, acceptDocs)
            {
                this._enclosing = _enclosing;
            }

            internal BytesRef termBytes = new BytesRef();

            internal Cell nextCell;

            //see getLeafDocs
            /// <summary>This is the primary algorithm; recursive.</summary>
            /// <remarks>This is the primary algorithm; recursive.  Returns null if finds none.</remarks>
            /// <exception cref="System.IO.IOException"></exception>
            internal SmallDocSet Visit(Cell cell, IBits acceptContains
                )
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
                ICollection<Cell> subCells = cell.GetSubCells(_enclosing.queryShape);
                foreach (Cell subCell in subCells)
                {
                    if (!SeekExact(subCell))
                    {
                        combinedSubResults = null;
                    }
                    else
                    {
                        if (subCell.Level == _enclosing.detailLevel)
                        {
                            combinedSubResults = GetDocs(subCell, acceptContains);
                        }
                        else
                        {
                            if (subCell.GetShapeRel() == SpatialRelation.WITHIN)
                            {
                                combinedSubResults = GetLeafDocs(subCell, acceptContains);
                            }
                            else
                            {
                                combinedSubResults = Visit(subCell, acceptContains);
                            }
                        }
                    }
                    //recursion
                    if (combinedSubResults == null)
                    {
                        break;
                    }
                    acceptContains = combinedSubResults;
                }
                //has the 'AND' effect on next iteration
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
                System.Diagnostics.Debug.Assert(new BytesRef(cell.GetTokenBytes().ToSByteArray()).CompareTo(this
                    .termBytes) > 0);
                this.termBytes.bytes = cell.GetTokenBytes().ToSByteArray();
                this.termBytes.length = this.termBytes.bytes.Length;
                return this.termsEnum.SeekExact(this.termBytes, cell.Level <= 2);
            }

            /// <exception cref="System.IO.IOException"></exception>
            private ContainsPrefixTreeFilter.SmallDocSet GetDocs(Cell cell, IBits acceptContains
                )
            {
                System.Diagnostics.Debug.Assert(new BytesRef(cell.GetTokenBytes().ToSByteArray()).Equals(this.termBytes
                    ));
                return this.CollectDocs(acceptContains);
            }

            /// <exception cref="System.IO.IOException"></exception>
            private ContainsPrefixTreeFilter.SmallDocSet GetLeafDocs(Cell leafCell, IBits acceptContains)
            {
                System.Diagnostics.Debug.Assert(new BytesRef(leafCell.GetTokenBytes().ToSByteArray()).Equals(this
                    .termBytes));
                BytesRef nextTerm = this.termsEnum.Next();
                if (nextTerm == null)
                {
                    this.termsEnum = null;
                    //signals all done
                    return null;
                }
                this.nextCell = this._enclosing.grid.GetCell(nextTerm.bytes.ToByteArray(), nextTerm.offset, nextTerm
                    .length, this.nextCell);
                if (this.nextCell.Level == leafCell.Level && this.nextCell.IsLeaf())
                {
                    return this.CollectDocs(acceptContains);
                }
                else
                {
                    return null;
                }
            }

            /// <exception cref="System.IO.IOException"></exception>
            private ContainsPrefixTreeFilter.SmallDocSet CollectDocs(IBits acceptContains)
            {
                ContainsPrefixTreeFilter.SmallDocSet set = null;
                this.docsEnum = this.termsEnum.Docs(acceptContains, this.docsEnum, DocsEnum.FLAG_NONE
                    );
                int docid;
                while ((docid = this.docsEnum.NextDoc()) != DocIdSetIterator.NO_MORE_DOCS)
                {
                    if (set == null)
                    {
                        int size = this.termsEnum.DocFreq;
                        if (size <= 0)
                        {
                            size = 16;
                        }
                        set = new ContainsPrefixTreeFilter.SmallDocSet(size);
                    }
                    set.Set(docid);
                }
                return set;
            }

            private readonly ContainsPrefixTreeFilter _enclosing;
            //class ContainsVisitor
        }

        /// <summary>A hash based mutable set of docIds.</summary>
        /// <remarks>
        /// A hash based mutable set of docIds. If this were Solr code then we might
        /// use a combination of HashDocSet and SortedIntDocSet instead.
        /// </remarks>
        private class SmallDocSet : DocIdSet, Lucene.Net.Util.IBits
        {
            private readonly SentinelIntSet intSet;

            private int maxInt = 0;

            public SmallDocSet(int size)
            {
                intSet = new SentinelIntSet(size, -1);
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
            /// <remarks>Largest docid.</remarks>
            public int Length
            {
                get
                {
                    return maxInt;
                }
            }

            /// <summary>Number of docids.</summary>
            /// <remarks>Number of docids.</remarks>
            public virtual int Size()
            {
                return intSet.Size;
            }

            /// <summary>NOTE: modifies and returns either "this" or "other"</summary>
            public virtual ContainsPrefixTreeFilter.SmallDocSet Union(ContainsPrefixTreeFilter.SmallDocSet
                 other)
            {
                ContainsPrefixTreeFilter.SmallDocSet bigger;
                ContainsPrefixTreeFilter.SmallDocSet smaller;
                if (other.intSet.Size > this.intSet.Size)
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
                foreach (int v in smaller.intSet.keys)
                {
                    if (v == smaller.intSet.emptyVal)
                    {
                        continue;
                    }
                    bigger.Set(v);
                }
                return bigger;
            }

            /// <exception cref="System.IO.IOException"></exception>
            public override Lucene.Net.Util.IBits Bits
            {
                get
                {
                    //if the # of docids is super small, return null since iteration is going
                    // to be faster
                    return Size() > 4 ? this : null;
                }
            }

            /// <exception cref="System.IO.IOException"></exception>
            public override DocIdSetIterator Iterator()
            {
                if (Size() == 0)
                {
                    return null;
                }
                //copy the unsorted values to a new array then sort them
                int d = 0;
                int[] docs = new int[intSet.Size];
                foreach (int v in intSet.keys)
                {
                    if (v == intSet.emptyVal)
                    {
                        continue;
                    }
                    docs[d++] = v;
                }
                System.Diagnostics.Debug.Assert(d == intSet.Size);
                int size = d;
                //sort them
                Array.Sort(docs, 0, size);
                return new _DocIdSetIterator_225(size, docs);
            }

            private sealed class _DocIdSetIterator_225 : DocIdSetIterator
            {
                public _DocIdSetIterator_225(int size, int[] docs)
                {
                    this.size = size;
                    this.docs = docs;
                    this.idx = -1;
                }

                internal int idx;

                public override int DocID
                {
                    get
                    {
                        if (this.idx >= 0 && this.idx < size)
                        {
                            return docs[this.idx];
                        }
                        else
                        {
                            return -1;
                        }
                    }
                }

                /// <exception cref="System.IO.IOException"></exception>
                public override int NextDoc()
                {
                    if (++this.idx < size)
                    {
                        return docs[this.idx];
                    }
                    return DocIdSetIterator.NO_MORE_DOCS;
                }

                /// <exception cref="System.IO.IOException"></exception>
                public override int Advance(int target)
                {
                    //for this small set this is likely faster vs. a binary search
                    // into the sorted array
                    return this.SlowAdvance(target);
                }

                public override long Cost
                {
                    get { return size; }
                }

                private readonly int size;

                private readonly int[] docs;
            }
            //class SmallDocSet
            public bool this[int index]
            {
                get { return intSet.Exists(index); }
            }
        }
    }
}
