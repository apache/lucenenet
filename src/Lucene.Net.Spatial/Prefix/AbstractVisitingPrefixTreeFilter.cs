using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Spatial.Prefix.Tree;
using Lucene.Net.Util;
using Spatial4n.Core.Shapes;
using System;
using System.Collections;
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
    /// Traverses a <see cref="SpatialPrefixTree">SpatialPrefixTree</see> indexed field, using the template &
    /// visitor design patterns for subclasses to guide the traversal and collect
    /// matching documents.
    /// <para/>
    /// Subclasses implement <see cref="Filter.GetDocIdSet(AtomicReaderContext, IBits)"/>
    /// by instantiating a custom <see cref="VisitorTemplate"/> subclass (i.e. an anonymous inner class) and implement the
    /// required methods.
    /// 
    /// @lucene.internal
    /// </summary>
    public abstract class AbstractVisitingPrefixTreeFilter : AbstractPrefixTreeFilter
    {
        // Historical note: this code resulted from a refactoring of RecursivePrefixTreeFilter,
        // which in turn came out of SOLR-2155

        protected internal readonly int prefixGridScanLevel;//at least one less than grid.getMaxLevels()

        public AbstractVisitingPrefixTreeFilter(IShape queryShape, string fieldName, SpatialPrefixTree grid, 
                                                int detailLevel, int prefixGridScanLevel)
            : base(queryShape, fieldName, grid, detailLevel)
        {
            this.prefixGridScanLevel = Math.Max(0, Math.Min(prefixGridScanLevel, grid.MaxLevels - 1));
            Debug.Assert(detailLevel <= grid.MaxLevels);
        }

        public override bool Equals(object o)
        {
            if (!base.Equals(o))
            {
                return false;//checks getClass == o.getClass & instanceof
            }

            //Ignore prefixGridScanLevel as it is merely a tuning parameter.

            return true;
        }

        public override int GetHashCode()
        {
            int result = base.GetHashCode();
            return result;
        }

        #region Nested type: VisitorTemplate

        /// <summary>
        /// An abstract class designed to make it easy to implement predicates or
        /// other operations on a <see cref="SpatialPrefixTree"/> indexed field. An instance
        /// of this class is not designed to be re-used across AtomicReaderContext
        /// instances so simply create a new one for each call to, say a
        /// <see cref="Lucene.Net.Search.Filter.GetDocIdSet(Lucene.Net.Index.AtomicReaderContext, Lucene.Net.Util.IBits)"/>.
        /// The <see cref="GetDocIdSet()"/> method here starts the work. It first checks
        /// that there are indexed terms; if not it quickly returns null. Then it calls
        /// <see cref="Start()">Start()</see> so a subclass can set up a return value, like an
        /// <see cref="Lucene.Net.Util.FixedBitSet"/>. Then it starts the traversal
        /// process, calling <see cref="FindSubCellsToVisit(Lucene.Net.Spatial.Prefix.Tree.Cell)"/>
        /// which by default finds the top cells that intersect <c>queryShape</c>. If
        /// there isn't an indexed cell for a corresponding cell returned for this
        /// method then it's short-circuited until it finds one, at which point
        /// <see cref="Visit(Lucene.Net.Spatial.Prefix.Tree.Cell)"/> is called. At
        /// some depths, of the tree, the algorithm switches to a scanning mode that
        /// calls <see cref="VisitScanned(Lucene.Net.Spatial.Prefix.Tree.Cell)"/>
        /// for each leaf cell found.
        /// 
        /// @lucene.internal
        /// </summary>
        public abstract class VisitorTemplate : BaseTermsEnumTraverser
        {
            /* Future potential optimizations:

            * Can a polygon query shape be optimized / made-simpler at recursive depths
              (e.g. intersection of shape + cell box)

            * RE "scan" vs divide & conquer performance decision:
              We should use termsEnum.docFreq() as an estimate on the number of places at
              this depth.  It would be nice if termsEnum knew how many terms
              start with the current term without having to repeatedly next() & test to find out.

            * Perhaps don't do intermediate seek()'s to cells above detailLevel that have Intersects
              relation because we won't be collecting those docs any way.  However seeking
              does act as a short-circuit.  So maybe do some percent of the time or when the level
              is above some threshold.

            * Each shape.relate(otherShape) result could be cached since much of the same relations
              will be invoked when multiple segments are involved.

            */

            protected internal readonly bool hasIndexedLeaves;//if false then we can skip looking for them

            private VNode curVNode;//current pointer, derived from query shape
            private BytesRef curVNodeTerm = new BytesRef();//curVNode.cell's term.
            private Cell scanCell;

            private BytesRef thisTerm; //the result of termsEnum.term()

            public VisitorTemplate(AbstractVisitingPrefixTreeFilter outerInstance, AtomicReaderContext context, IBits acceptDocs,
                                   bool hasIndexedLeaves)
                : base(outerInstance, context, acceptDocs)
            {
                this.hasIndexedLeaves = hasIndexedLeaves;
            }

            public virtual DocIdSet GetDocIdSet()
            {
                Debug.Assert(curVNode == null, "Called more than once?");
                if (termsEnum == null)
                {
                    return null;
                }
                //advance
                if ((thisTerm = termsEnum.Next()) == null)
                {
                    return null;// all done
                }
                
                curVNode = new VNode(null);
                curVNode.Reset(outerInstance.grid.WorldCell);

                Start();

                AddIntersectingChildren();

                while (thisTerm != null)//terminates for other reasons too!
                {
                    //Advance curVNode pointer
                    if (curVNode.children != null)
                    {
                        //-- HAVE CHILDREN: DESCEND

                        // LUCENENET NOTE: Must call this line before calling MoveNext()
                        // on the enumerator.

                        //if we put it there then it has something
                        PreSiblings(curVNode);

                        // LUCENENET IMPORTANT: Must not call this inline with Debug.Assert
                        // because the compiler removes Debug.Assert statements in release mode!!
                        bool hasNext = curVNode.children.MoveNext();
                        Debug.Assert(hasNext);

                        curVNode = curVNode.children.Current;
                    }
                    else
                    {
                        //-- NO CHILDREN: ADVANCE TO NEXT SIBLING
                        VNode parentVNode = curVNode.parent;
                        while (true)
                        {
                            if (parentVNode == null)
                            {
                                goto main_break;// all done
                            }
                            if (parentVNode.children.MoveNext())
                            {
                                //advance next sibling
                                curVNode = parentVNode.children.Current;
                                break;
                            }
                            else
                            {
                                //reached end of siblings; pop up
                                PostSiblings(parentVNode);
                                parentVNode.children = null;
                                //GC
                                parentVNode = parentVNode.parent;
                            }
                        }
                    }
                    //Seek to curVNode's cell (or skip if termsEnum has moved beyond)
                    curVNodeTerm.Bytes = curVNode.cell.GetTokenBytes();
                    curVNodeTerm.Length = curVNodeTerm.Bytes.Length;
                    int compare = termsEnum.Comparator.Compare(thisTerm, curVNodeTerm);
                    if (compare > 0)
                    {
                        // leap frog (termsEnum is beyond where we would otherwise seek)
                        Debug.Assert(!context.AtomicReader.Terms(outerInstance.fieldName).Iterator(null).SeekExact(curVNodeTerm), "should be absent");
                    }
                    else
                    {
                        if (compare < 0)
                        {
                            // Seek !
                            TermsEnum.SeekStatus seekStatus = termsEnum.SeekCeil(curVNodeTerm);
                            if (seekStatus == TermsEnum.SeekStatus.END)
                            {
                                break;// all done
                            }
                            thisTerm = termsEnum.Term;
                            if (seekStatus == TermsEnum.SeekStatus.NOT_FOUND)
                            {
                                continue; // leap frog
                            }
                        }
                        // Visit!
                        bool descend = Visit(curVNode.cell);
                        //advance
                        if ((thisTerm = termsEnum.Next()) == null)
                        {
                            break;// all done
                        }
                        if (descend)
                        {
                            AddIntersectingChildren();
                        }
                    }
                    ;
                }//main loop
                main_break: { }
                
                return Finish();
            }

            /// <summary>
            /// Called initially, and whenever <see cref="Visit(Lucene.Net.Spatial.Prefix.Tree.Cell)"/>
            /// returns true.
            /// </summary>
            /// <exception cref="System.IO.IOException"></exception>
            private void AddIntersectingChildren()
            {
                Debug.Assert(thisTerm != null);
                Cell cell = curVNode.cell;
                if (cell.Level >= outerInstance.detailLevel)
                {
                    throw new InvalidOperationException("Spatial logic error");
                }
                //Check for adjacent leaf (happens for indexed non-point shapes)
                if (hasIndexedLeaves && cell.Level != 0)
                {
                    //If the next indexed term just adds a leaf marker ('+') to cell,
                    // then add all of those docs
                    Debug.Assert(StringHelper.StartsWith(thisTerm, curVNodeTerm));//TODO refactor to use method on curVNode.cell
                    scanCell = outerInstance.grid.GetCell(thisTerm.Bytes, thisTerm.Offset, thisTerm.Length, scanCell);
                    if (scanCell.Level == cell.Level && scanCell.IsLeaf)
                    {
                        VisitLeaf(scanCell);
                        //advance
                        if ((thisTerm = termsEnum.Next()) == null)
                        {
                            return;// all done
                        }
                    }
                }
                
                //Decide whether to continue to divide & conquer, or whether it's time to
                // scan through terms beneath this cell.
                // Scanning is a performance optimization trade-off.

                //TODO use termsEnum.docFreq() as heuristic
                bool scan = cell.Level >= ((AbstractVisitingPrefixTreeFilter)outerInstance).prefixGridScanLevel;//simple heuristic

                if (!scan)
                {
                    //Divide & conquer (ultimately termsEnum.seek())

                    IEnumerator<Cell> subCellsIter = FindSubCellsToVisit(cell);
                    if (!subCellsIter.MoveNext())
                    {
                        return;//not expected
                    }
                    curVNode.children = new VNodeCellIterator(this, subCellsIter, new VNode(curVNode));
                }
                else
                {
                    //Scan (loop of termsEnum.next())

                    Scan(outerInstance.detailLevel);
                }
            }

            /// <summary>
            /// Called when doing a divide & conquer to find the next intersecting cells
            /// of the query shape that are beneath <paramref name="cell"/>. <paramref name="cell"/> is
            /// guaranteed to have an intersection and thus this must return some number
            /// of nodes.
            /// </summary>
            protected internal virtual IEnumerator<Cell> FindSubCellsToVisit(Cell cell)
            {
                return cell.GetSubCells(outerInstance.queryShape).GetEnumerator();
            }

            /// <summary>
            /// Scans (<c>termsEnum.Next()</c>) terms until a term is found that does
            /// not start with curVNode's cell. If it finds a leaf cell or a cell at
            /// level <paramref name="scanDetailLevel"/> then it calls
            /// <see cref="VisitScanned(Lucene.Net.Spatial.Prefix.Tree.Cell)"/>.
            /// </summary>
            /// <exception cref="System.IO.IOException"></exception>
            protected internal virtual void Scan(int scanDetailLevel)
            {
                for (;
                    thisTerm != null && StringHelper.StartsWith(thisTerm, curVNodeTerm);//TODO refactor to use method on curVNode.cell
                    thisTerm = termsEnum.Next())
                {
                    scanCell = outerInstance.grid.GetCell(thisTerm.Bytes, thisTerm.Offset, thisTerm.Length, scanCell);

                    int termLevel = scanCell.Level;
                    if (termLevel < scanDetailLevel)
                    {
                        if (scanCell.IsLeaf)
                            VisitScanned(scanCell);
                    }
                    else if (termLevel == scanDetailLevel)
                    {
                        if (!scanCell.IsLeaf)//LUCENE-5529
                            VisitScanned(scanCell);
                    }
                }//term loop
            }

            #region Nested type: VNodeCellIterator

            /// <summary>
            /// Used for <see cref="VNode.children"/>.
            /// </summary>
            private class VNodeCellIterator : IEnumerator<VNode>
            {
                private readonly VisitorTemplate outerInstance;

                internal readonly IEnumerator<Cell> cellIter;
                private readonly VNode vNode;
                private bool first = true;

                internal VNodeCellIterator(VisitorTemplate outerInstance, IEnumerator<Cell> cellIter, VNode vNode)
                {
                    this.outerInstance = outerInstance;
                    //term loop
                    this.cellIter = cellIter;
                    this.vNode = vNode;
                }

                //it always removes

                #region IEnumerator<VNode> Members

                public void Dispose()
                {
                    cellIter.Dispose();
                }

                public bool MoveNext()
                {
                    //Debug.Assert(cellIter.Current != null);

                    // LUCENENET NOTE: The consumer of this class calls
                    // cellIter.MoveNext() before it is instantiated.
                    // So, the first call here
                    // to MoveNext() must not move the cursor.
                    bool result;
                    if (!first)
                    {
                        result = cellIter.MoveNext();
                    }
                    else
                    {
                        result = true;
                        first = false;
                    }

                    // LUCENENET NOTE: Need to skip this call
                    // if there are no more results because null
                    // is not allowed
                    if (result == true)
                    {
                        vNode.Reset(cellIter.Current);
                    }
                    return result;
                }

                public void Reset()
                {
                    cellIter.Reset();
                }

                public VNode Current
                {
                    get
                    {
                        return vNode;
                    }
                }

                object IEnumerator.Current
                {
                    get { return Current; }
                }

                #endregion
            }

            #endregion

            /// <summary>Called first to setup things.</summary>
            /// <exception cref="System.IO.IOException"></exception>
            protected internal abstract void Start();

            /// <summary>Called last to return the result.</summary>
            /// <exception cref="System.IO.IOException"></exception>
            protected internal abstract DocIdSet Finish();

            /// <summary>
            /// Visit an indexed cell returned from
            /// <see cref="FindSubCellsToVisit(Lucene.Net.Spatial.Prefix.Tree.Cell)"/>.
            /// </summary>
            /// <param name="cell">An intersecting cell.</param>
            /// <returns>
            /// true to descend to more levels. It is an error to return true
            /// if cell.Level == detailLevel
            /// </returns>
            /// <exception cref="System.IO.IOException"></exception>
            protected internal abstract bool Visit(Cell cell);

            /// <summary>Called after visit() returns true and an indexed leaf cell is found.</summary>
            /// <remarks>
            /// Called after Visit() returns true and an indexed leaf cell is found. An
            /// indexed leaf cell means associated documents generally won't be found at
            /// further detail levels.
            /// </remarks>
            /// <exception cref="System.IO.IOException"></exception>
            protected internal abstract void VisitLeaf(Cell cell);

            /// <summary>
            /// The cell is either indexed as a leaf or is the last level of detail. It
            /// might not even intersect the query shape, so be sure to check for that.
            /// </summary>
            /// <exception cref="System.IO.IOException"></exception>
            protected internal abstract void VisitScanned(Cell cell);

            protected internal virtual void PreSiblings(VNode vNode)
            {
            }

            protected internal virtual void PostSiblings(VNode vNode)
            {
            }
            //class VisitorTemplate
        }

        #endregion

        #region Nested type: VNode

        /// <summary>
        /// A Visitor node/cell found via the query shape for <see cref="VisitorTemplate"/>.
        /// Sometimes these are reset(cell). It's like a LinkedList node but forms a
        /// tree.
        /// 
        /// @lucene.internal
        /// </summary>
        public class VNode
        {
            //Note: The VNode tree adds more code to debug/maintain v.s. a flattened
            // LinkedList that we used to have. There is more opportunity here for
            // custom behavior (see preSiblings & postSiblings) but that's not
            // leveraged yet. Maybe this is slightly more GC friendly.

            internal readonly VNode parent;//only null at the root
            internal IEnumerator<VNode> children;//null, then sometimes set, then null
            internal Cell cell;//not null (except initially before reset())

            /// <summary>Call <see cref="Reset(Cell)"/> after to set the cell.</summary>
            internal VNode(VNode parent)
            {
                // remember to call reset(cell) after
                this.parent = parent;
            }

            internal virtual void Reset(Cell cell)
            {
                Debug.Assert(cell != null);
                this.cell = cell;
                Debug.Assert(children == null);
            }
        }

        #endregion
    }
}