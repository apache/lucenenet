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
using System.Diagnostics;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Spatial.Prefix.Tree;
using Lucene.Net.Spatial.Queries;
using Lucene.Net.Util;
using Spatial4n.Core.Context;
using Spatial4n.Core.Distance;
using Spatial4n.Core.Shapes;

namespace Lucene.Net.Spatial.Prefix
{
    /// <summary>
    /// Finds docs where its indexed shape is
    /// <see cref="SpatialOperation.IsWithin">WITHIN</see>
    /// the query shape.  It works by looking at cells outside of the query
    /// shape to ensure documents there are excluded. By default, it will
    /// examine all cells, and it's fairly slow.  If you know that the indexed shapes
    /// are never comprised of multiple disjoint parts (which also means it is not multi-valued),
    /// then you can pass
    /// <code>SpatialPrefixTree.getDistanceForLevel(maxLevels)</code>
    /// as
    /// the
    /// <code>queryBuffer</code>
    /// constructor parameter to minimally look this distance
    /// beyond the query shape's edge.  Even if the indexed shapes are sometimes
    /// comprised of multiple disjoint parts, you might want to use this option with
    /// a large buffer as a faster approximation with minimal false-positives.
    /// </summary>
    /// <lucene.experimental></lucene.experimental>
    public class WithinPrefixTreeFilter : AbstractVisitingPrefixTreeFilter
    {
        private readonly Shape bufferedQueryShape;

        /// <summary>
        /// See
        /// <see cref="AbstractVisitingPrefixTreeFilter">AbstractVisitingPrefixTreeFilter.AbstractVisitingPrefixTreeFilter(Shape, string, Lucene.Net.Spatial.Prefix.Tree.SpatialPrefixTree, int, int)
        /// 	</see>
        /// .
        /// <code>queryBuffer</code>
        /// is the (minimum) distance beyond the query shape edge
        /// where non-matching documents are looked for so they can be excluded. If
        /// -1 is used then the whole world is examined (a good default for correctness).
        /// </summary>
        public WithinPrefixTreeFilter(Shape queryShape, string fieldName
                                      , SpatialPrefixTree grid, int detailLevel, int prefixGridScanLevel,
                                      double queryBuffer
            )
            : base(queryShape, fieldName, grid, detailLevel, prefixGridScanLevel)
        {
            //TODO LUCENE-4869: implement faster algorithm based on filtering out false-positives of a
            //  minimal query buffer by looking in a DocValues cache holding a representative
            //  point of each disjoint component of a document's shape(s).
            //if null then the whole world
            if (queryBuffer == -1)
            {
                bufferedQueryShape = null;
            }
            else
            {
                bufferedQueryShape = BufferShape(queryShape, queryBuffer);
            }
        }

        /// <summary>Returns a new shape that is larger than shape by at distErr.</summary>
        /// <remarks>Returns a new shape that is larger than shape by at distErr.</remarks>
        protected internal virtual Shape BufferShape(Shape
                                                         shape, double distErr)
        {
            //TODO move this generic code elsewhere?  Spatial4j?
            if (distErr <= 0)
            {
                throw new ArgumentException("distErr must be > 0");
            }
            SpatialContext ctx = grid.SpatialContext;
            if (shape is Point)
            {
                return ctx.MakeCircle((Point)shape, distErr);
            }
            else
            {
                if (shape is Circle)
                {
                    var circle = (Circle)shape;
                    double newDist = circle.GetRadius() + distErr;
                    if (ctx.IsGeo() && newDist > 180)
                    {
                        newDist = 180;
                    }
                    return ctx.MakeCircle(circle.GetCenter(), newDist);
                }
                else
                {
                    Rectangle bbox = shape.GetBoundingBox();
                    double newMinX = bbox.GetMinX() - distErr;
                    double newMaxX = bbox.GetMaxX() + distErr;
                    double newMinY = bbox.GetMinY() - distErr;
                    double newMaxY = bbox.GetMaxY() + distErr;
                    if (ctx.IsGeo())
                    {
                        if (newMinY < -90)
                        {
                            newMinY = -90;
                        }
                        if (newMaxY > 90)
                        {
                            newMaxY = 90;
                        }
                        if (newMinY == -90 || newMaxY == 90 || bbox.GetWidth() + 2 * distErr > 360)
                        {
                            newMinX = -180;
                            newMaxX = 180;
                        }
                        else
                        {
                            newMinX = DistanceUtils.NormLonDEG(newMinX);
                            newMaxX = DistanceUtils.NormLonDEG(newMaxX);
                        }
                    }
                    else
                    {
                        //restrict to world bounds
                        newMinX = Math.Max(newMinX, ctx.GetWorldBounds().GetMinX());
                        newMaxX = Math.Min(newMaxX, ctx.GetWorldBounds().GetMaxX());
                        newMinY = Math.Max(newMinY, ctx.GetWorldBounds().GetMinY());
                        newMaxY = Math.Min(newMaxY, ctx.GetWorldBounds().GetMaxY());
                    }
                    return ctx.MakeRectangle(newMinX, newMaxX, newMinY, newMaxY);
                }
            }
        }

        /// <exception cref="System.IO.IOException"></exception>
        public override DocIdSet GetDocIdSet(AtomicReaderContext context, Bits acceptDocs
            )
        {
            return new _VisitorTemplate_121(this, context, acceptDocs, true).GetDocIdSet();
        }

        #region Nested type: _VisitorTemplate_121

        private sealed class _VisitorTemplate_121 : VisitorTemplate
        {
            private readonly WithinPrefixTreeFilter _enclosing;
            private FixedBitSet inside;

            private FixedBitSet outside;

            private SpatialRelation visitRelation;

            public _VisitorTemplate_121(WithinPrefixTreeFilter _enclosing, AtomicReaderContext
                                                                               baseArg1, Bits baseArg2, bool baseArg3)
                : base(_enclosing, baseArg1, baseArg2, baseArg3)
            {
                this._enclosing = _enclosing;
            }

            protected internal override void Start()
            {
                inside = new FixedBitSet(maxDoc);
                outside = new FixedBitSet(maxDoc);
            }

            protected internal override DocIdSet Finish()
            {
                inside.AndNot(outside);
                return inside;
            }

            protected internal override IEnumerator<Cell> FindSubCellsToVisit(Cell cell)
            {
                //use buffered query shape instead of orig.  Works with null too.
                return cell.GetSubCells(_enclosing.bufferedQueryShape).GetEnumerator();
            }

            /// <exception cref="System.IO.IOException"></exception>
            protected internal override bool Visit(Cell cell)
            {
                //cell.relate is based on the bufferedQueryShape; we need to examine what
                // the relation is against the queryShape
                visitRelation = cell.GetShape().Relate(_enclosing.queryShape);
                if (visitRelation == SpatialRelation.WITHIN)
                {
                    CollectDocs(inside);
                    return false;
                }
                else
                {
                    if (visitRelation == SpatialRelation.DISJOINT)
                    {
                        CollectDocs(outside);
                        return false;
                    }
                    else
                    {
                        if (cell.Level == _enclosing.detailLevel)
                        {
                            CollectDocs(inside);
                            return false;
                        }
                    }
                }
                return true;
            }

            /// <exception cref="System.IO.IOException"></exception>
            protected internal override void VisitLeaf(Cell cell)
            {
                //visitRelation is declared as a field, populated by visit() so we don't recompute it
                Debug.Assert(_enclosing.detailLevel != cell.Level);
                Debug.Assert(visitRelation == cell.GetShape().Relate(_enclosing.queryShape));
                if (AllCellsIntersectQuery(cell, visitRelation))
                {
                    CollectDocs(inside);
                }
                else
                {
                    CollectDocs(outside);
                }
            }

            /// <summary>
            /// Returns true if the provided cell, and all its sub-cells down to
            /// detailLevel all intersect the queryShape.
            /// </summary>
            /// <remarks>
            /// Returns true if the provided cell, and all its sub-cells down to
            /// detailLevel all intersect the queryShape.
            /// </remarks>
            private bool AllCellsIntersectQuery(Cell cell, SpatialRelation relate)
            {
                if (relate == SpatialRelation.NULL_VALUE)
                {
                    relate = cell.GetShape().Relate(_enclosing.queryShape);
                }
                if (cell.Level == _enclosing.detailLevel)
                {
                    return relate.Intersects();
                }
                if (relate == SpatialRelation.WITHIN)
                {
                    return true;
                }
                if (relate == SpatialRelation.DISJOINT)
                {
                    return false;
                }
                // Note: Generating all these cells just to determine intersection is not ideal.
                // It was easy to implement but could be optimized. For example if the docs
                // in question are already marked in the 'outside' bitset then it can be avoided.
                ICollection<Cell> subCells = cell.GetSubCells(null);
                foreach (Cell subCell in subCells)
                {
                    if (!AllCellsIntersectQuery(subCell, SpatialRelation.NULL_VALUE))
                    {
                        //recursion
                        return false;
                    }
                }
                return true;
            }

            /// <exception cref="System.IO.IOException"></exception>
            protected internal override void VisitScanned(Cell cell)
            {
                if (AllCellsIntersectQuery(cell, SpatialRelation.NULL_VALUE))
                {
                    CollectDocs(inside);
                }
                else
                {
                    CollectDocs(outside);
                }
            }
        }

        #endregion
    }
}