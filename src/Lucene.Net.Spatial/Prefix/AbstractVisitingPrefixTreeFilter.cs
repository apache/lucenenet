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
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Spatial.Prefix.Tree;
using Lucene.Net.Spatial.Util;
using Lucene.Net.Util;
using Spatial4n.Core.Shapes;

namespace Lucene.Net.Spatial.Prefix
{
    /// <summary>
    /// Traverses a <see cref="SpatialPrefixTree">SpatialPrefixTree</see> indexed field, using the template &
    /// visitor design patterns for subclasses to guide the traversal and collect
    /// matching documents.
    /// <p/>
    /// Subclasses implement <see cref="Filter.GetDocIdSet(AtomicReaderContext, Bits)">Lucene.Search.Filter.GetDocIdSet(AtomicReaderContext, Bits)</see>
    /// by instantiating a custom <see cref="VisitorTemplate">VisitorTemplate</see>
    /// subclass (i.e. an anonymous inner class) and implement the
    /// required methods.
    /// @lucene.internal
    /// </summary>
    public abstract class AbstractVisitingPrefixTreeFilter : AbstractPrefixTreeFilter
    {
        // Historical note: this code resulted from a refactoring of RecursivePrefixTreeFilter,
        // which in turn came out of SOLR-2155

        protected internal readonly int prefixGridScanLevel;

        public AbstractVisitingPrefixTreeFilter(Shape queryShape, string fieldName, SpatialPrefixTree grid, 
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
            
            var that = (AbstractVisitingPrefixTreeFilter)o;
            if (prefixGridScanLevel != that.prefixGridScanLevel)
            {
                return false;
            }
            return true;
        }

        public override int GetHashCode()
        {
            int result = base.GetHashCode();
            result = 31 * result + prefixGridScanLevel;
            return result;
        }

        #region Nested type: VNode

        /// <summary>
        /// A Visitor Cell/Cell found via the query shape for
        /// <see cref="VisitorTemplate">VisitorTemplate</see>
        /// .
        /// Sometimes these are reset(cell). It's like a LinkedList node but forms a
        /// tree.
        /// </summary>
        /// <lucene.internal></lucene.internal>
        public class VNode
        {
            internal readonly VNode parent;

            internal Cell cell;
            internal IEnumerator<VNode> children;

            /// <summary>call reset(cell) after to set the cell.</summary>
            /// <remarks>call reset(cell) after to set the cell.</remarks>
            internal VNode(VNode parent)
            {
                //Note: The VNode tree adds more code to debug/maintain v.s. a flattened
                // LinkedList that we used to have. There is more opportunity here for
                // custom behavior (see preSiblings & postSiblings) but that's not
                // leveraged yet. Maybe this is slightly more GC friendly.
                //only null at the root
                //null, then sometimes set, then null
                //not null (except initially before reset())
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

        #region Nested type: VisitorTemplate

        /// <summary>
        /// An abstract class designed to make it easy to implement predicates or
        /// other operations on a
        /// <see cref="Lucene.Net.Spatial.Prefix.Tree.SpatialPrefixTree">Lucene.Net.Spatial.Prefix.Tree.SpatialPrefixTree
        /// 	</see>
        /// indexed field. An instance
        /// of this class is not designed to be re-used across AtomicReaderContext
        /// instances so simply create a new one for each call to, say a
        /// <see cref="Lucene.Net.Search.Filter.GetDocIdSet(Lucene.Net.Index.AtomicReaderContext, Lucene.Net.Util.Bits)
        /// 	">Lucene.Net.Search.Filter.GetDocIdSet(Lucene.Net.Index.AtomicReaderContext, Lucene.Net.Util.Bits)
        /// 	</see>
        /// .
        /// The
        /// <see cref="GetDocIdSet()">GetDocIdSet()</see>
        /// method here starts the work. It first checks
        /// that there are indexed terms; if not it quickly returns null. Then it calls
        /// <see cref="Start()">Start()</see>
        /// so a subclass can set up a return value, like an
        /// <see cref="Lucene.Net.Util.OpenBitSet">Lucene.Net.Util.OpenBitSet</see>
        /// . Then it starts the traversal
        /// process, calling
        /// <see cref="FindSubCellsToVisit(Lucene.Net.Spatial.Prefix.Tree.Cell)">FindSubCellsToVisit(Lucene.Net.Spatial.Prefix.Tree.Cell)
        /// 	</see>
        /// which by default finds the top cells that intersect
        /// <code>queryShape</code>
        /// . If
        /// there isn't an indexed cell for a corresponding cell returned for this
        /// method then it's short-circuited until it finds one, at which point
        /// <see cref="Visit(Lucene.Net.Spatial.Prefix.Tree.Cell)">Visit(Lucene.Net.Spatial.Prefix.Tree.Cell)
        /// 	</see>
        /// is called. At
        /// some depths, of the tree, the algorithm switches to a scanning mode that
        /// finds calls
        /// <see cref="VisitScanned(Lucene.Net.Spatial.Prefix.Tree.Cell)">VisitScanned(Lucene.Net.Spatial.Prefix.Tree.Cell)
        /// 	</see>
        /// for each leaf cell found.
        /// </summary>
        /// <lucene.internal></lucene.internal>
        public abstract class VisitorTemplate : BaseTermsEnumTraverser
        {
            private readonly AbstractVisitingPrefixTreeFilter outerInstance;
            private readonly BytesRef curVNodeTerm = new BytesRef();
            protected internal readonly bool hasIndexedLeaves;//if false then we can skip looking for them

            private VNode curVNode;//current pointer, derived from query shape
            private BytesRef thisTerm; //the result of termsEnum.term()
            private Cell scanCell;//curVNode.cell's term.

            /// <exception cref="System.IO.IOException"></exception>
            public VisitorTemplate(AbstractVisitingPrefixTreeFilter outerInstance, AtomicReaderContext context, Bits acceptDocs,
                                   bool hasIndexedLeaves)
                : base(outerInstance, context, acceptDocs)
            {
                this.outerInstance = outerInstance;
                this.hasIndexedLeaves = hasIndexedLeaves;
            }

            /// <exception cref="System.IO.IOException"></exception>
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
                    return null;
                }
                // all done
                curVNode = new VNode(null);
                curVNode.Reset(outerInstance.grid.WorldCell);
                Start();
                AddIntersectingChildren();
                while (thisTerm != null)
                {
                    //terminates for other reasons too!
                    //Advance curVNode pointer
                    if (curVNode.children != null)
                    {
                        //-- HAVE CHILDREN: DESCEND
                        Debug.Assert(curVNode.children.MoveNext());
                        //if we put it there then it has something
                        PreSiblings(curVNode);
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
                                goto main_break;
                            }
                            // all done
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
                    int compare = termsEnum.Comparator.Compare(thisTerm, curVNodeTerm
                        );
                    if (compare > 0)
                    {
                        // leap frog (termsEnum is beyond where we would otherwise seek)
                        Debug.Assert(!((AtomicReader)context.Reader).Terms(outerInstance.fieldName).Iterator(null).SeekExact(curVNodeTerm), "should be absent");
                    }
                    else
                    {
                        if (compare < 0)
                        {
                            // Seek !
                            TermsEnum.SeekStatus seekStatus = termsEnum.SeekCeil(curVNodeTerm);
                            if (seekStatus == TermsEnum.SeekStatus.END)
                            {
                                break;
                            }
                            // all done
                            thisTerm = termsEnum.Term();
                            if (seekStatus == TermsEnum.SeekStatus.NOT_FOUND)
                            {
                                continue;
                            }
                        }
                        // leap frog
                        // Visit!
                        bool descend = Visit(curVNode.cell);
                        //advance
                        if ((thisTerm = termsEnum.Next()) == null)
                        {
                            break;
                        }
                        // all done
                        if (descend)
                        {
                            AddIntersectingChildren();
                        }
                    }
                    ;
                }
            main_break:
                ;
                //main loop
                return Finish();
            }

            /// <summary>
            /// Called initially, and whenever
            /// <see cref="Visit(Lucene.Net.Spatial.Prefix.Tree.Cell)">Visit(Lucene.Net.Spatial.Prefix.Tree.Cell)
            /// 	</see>
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
                    Debug.Assert(StringHelper.StartsWith(thisTerm, curVNodeTerm
                                     ));
                    scanCell = outerInstance.grid.GetCell(thisTerm.Bytes, thisTerm.Offset
                                                       , thisTerm.Length, scanCell);
                    if (scanCell.Level == cell.Level && scanCell.IsLeaf())
                    {
                        VisitLeaf(scanCell);
                        //advance
                        if ((thisTerm = termsEnum.Next()) == null)
                        {
                            return;
                        }
                    }
                }
                // all done
                //Decide whether to continue to divide & conquer, or whether it's time to
                // scan through terms beneath this cell.
                // Scanning is a performance optimization trade-off.
                //TODO use termsEnum.docFreq() as heuristic
                bool scan = cell.Level >= outerInstance.prefixGridScanLevel;
                //simple heuristic
                if (!scan)
                {
                    //Divide & conquer (ultimately termsEnum.seek())
                    IEnumerator<Cell> subCellsIter = FindSubCellsToVisit(cell);
                    if (!subCellsIter.MoveNext())
                    {
                        //not expected
                        return;
                    }
                    curVNode.children = new VNodeCellIterator
                        (this, subCellsIter, new VNode(curVNode));
                }
                else
                {
                    //Scan (loop of termsEnum.next())
                    Scan(outerInstance.detailLevel);
                }
            }

            /// <summary>
            /// Called when doing a divide & conquer to find the next intersecting cells
            /// of the query shape that are beneath
            /// <code>cell</code>
            /// .
            /// <code>cell</code>
            /// is
            /// guaranteed to have an intersection and thus this must return some number
            /// of nodes.
            /// </summary>
            protected internal virtual IEnumerator<Cell> FindSubCellsToVisit(Cell cell)
            {
                return cell.GetSubCells(outerInstance.queryShape).GetEnumerator();
            }

            /// <summary>
            /// Scans (
            /// <code>termsEnum.next()</code>
            /// ) terms until a term is found that does
            /// not start with curVNode's cell. If it finds a leaf cell or a cell at
            /// level
            /// <code>scanDetailLevel</code>
            /// then it calls
            /// <see cref="VisitScanned(Lucene.Net.Spatial.Prefix.Tree.Cell)">VisitScanned(Lucene.Net.Spatial.Prefix.Tree.Cell)
            /// 	</see>
            /// .
            /// </summary>
            /// <exception cref="System.IO.IOException"></exception>
            protected internal virtual void Scan(int scanDetailLevel)
            {
                for (;
                    thisTerm != null && StringHelper.StartsWith(thisTerm, curVNodeTerm
                                            );
                    thisTerm = termsEnum.Next())
                {
                    scanCell = outerInstance.grid.GetCell(thisTerm.Bytes, thisTerm.Offset
                                                       , thisTerm.Length, scanCell);
                    int termLevel = scanCell.Level;
                    if (termLevel > scanDetailLevel)
                    {
                        continue;
                    }
                    if (termLevel == scanDetailLevel || scanCell.IsLeaf())
                    {
                        VisitScanned(scanCell);
                    }
                }
            }

            /// <summary>Called first to setup things.</summary>
            /// <remarks>Called first to setup things.</remarks>
            /// <exception cref="System.IO.IOException"></exception>
            protected internal abstract void Start();

            /// <summary>Called last to return the result.</summary>
            /// <remarks>Called last to return the result.</remarks>
            /// <exception cref="System.IO.IOException"></exception>
            protected internal abstract DocIdSet Finish();

            /// <summary>
            /// Visit an indexed cell returned from
            /// <see cref="FindSubCellsToVisit(Lucene.Net.Spatial.Prefix.Tree.Cell)">FindSubCellsToVisit(Lucene.Net.Spatial.Prefix.Tree.Cell)
            /// 	</see>
            /// .
            /// </summary>
            /// <param name="cell">An intersecting cell.</param>
            /// <returns>
            /// true to descend to more levels. It is an error to return true
            /// if cell.level == detailLevel
            /// </returns>
            /// <exception cref="System.IO.IOException"></exception>
            protected internal abstract bool Visit(Cell cell);

            /// <summary>Called after visit() returns true and an indexed leaf cell is found.</summary>
            /// <remarks>
            /// Called after visit() returns true and an indexed leaf cell is found. An
            /// indexed leaf cell means associated documents generally won't be found at
            /// further detail levels.
            /// </remarks>
            /// <exception cref="System.IO.IOException"></exception>
            protected internal abstract void VisitLeaf(Cell cell);

            /// <summary>The cell is either indexed as a leaf or is the last level of detail.</summary>
            /// <remarks>
            /// The cell is either indexed as a leaf or is the last level of detail. It
            /// might not even intersect the query shape, so be sure to check for that.
            /// </remarks>
            /// <exception cref="System.IO.IOException"></exception>
            protected internal abstract void VisitScanned(Cell cell);

            /// <exception cref="System.IO.IOException"></exception>
            protected internal virtual void PreSiblings(VNode vNode)
            {
            }

            /// <exception cref="System.IO.IOException"></exception>
            protected internal virtual void PostSiblings(VNode vNode)
            {
            }

            #region Nested type: VNodeCellIterator

            /// <summary>
            /// Used for
            /// <see cref="VNode.children">VNode.children</see>
            /// .
            /// </summary>
            private class VNodeCellIterator : IEnumerator<VNode>
            {
                private readonly VisitorTemplate _enclosing;
                internal readonly IEnumerator<Cell> cellIter;

                private readonly VNode vNode;

                internal VNodeCellIterator(VisitorTemplate _enclosing, IEnumerator<Cell> cellIter, VNode vNode)
                {
                    this._enclosing = _enclosing;
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
                    return cellIter.MoveNext();
                }

                public void Reset()
                {
                    cellIter.Reset();
                }

                public VNode Current
                {
                    get
                    {
                        Debug.Assert(cellIter.Current != null);
                        vNode.Reset(cellIter.Current);
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

            //class VisitorTemplate
        }

        #endregion
    }
}