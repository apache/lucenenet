using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Spatial.Prefix.Tree;
using Lucene.Net.Util;
using Spatial4n.Core.Context;
using Spatial4n.Core.Distance;
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
    /// Finds docs where its indexed shape is
    /// <see cref="Queries.SpatialOperation.IsWithin">WITHIN</see>
    /// the query shape.  It works by looking at cells outside of the query
    /// shape to ensure documents there are excluded. By default, it will
    /// examine all cells, and it's fairly slow.  If you know that the indexed shapes
    /// are never comprised of multiple disjoint parts (which also means it is not multi-valued),
    /// then you can pass <c>SpatialPrefixTree.GetDistanceForLevel(maxLevels)</c> as
    /// the <c>queryBuffer</c> constructor parameter to minimally look this distance
    /// beyond the query shape's edge.  Even if the indexed shapes are sometimes
    /// comprised of multiple disjoint parts, you might want to use this option with
    /// a large buffer as a faster approximation with minimal false-positives.
    /// 
    /// @lucene.experimental
    /// </summary>
    public class WithinPrefixTreeFilter : AbstractVisitingPrefixTreeFilter
    {
        /// TODO LUCENE-4869: implement faster algorithm based on filtering out false-positives of a
        //  minimal query buffer by looking in a DocValues cache holding a representative
        //  point of each disjoint component of a document's shape(s).

        private readonly IShape bufferedQueryShape;//if null then the whole world

        /// <summary>
        /// See <see cref="AbstractVisitingPrefixTreeFilter.AbstractVisitingPrefixTreeFilter(IShape, string, SpatialPrefixTree, int, int)"/>.
        /// <c>queryBuffer</c> is the (minimum) distance beyond the query shape edge
        /// where non-matching documents are looked for so they can be excluded. If
        /// -1 is used then the whole world is examined (a good default for correctness).
        /// </summary>
        public WithinPrefixTreeFilter(IShape queryShape, string fieldName, SpatialPrefixTree grid, 
                                      int detailLevel, int prefixGridScanLevel, double queryBuffer)
            : base(queryShape, fieldName, grid, detailLevel, prefixGridScanLevel)
        {
            if (queryBuffer == -1)
            {
                bufferedQueryShape = null;
            }
            else
            {
                bufferedQueryShape = BufferShape(queryShape, queryBuffer);
            }
        }

        /// <summary>
        /// Returns a new shape that is larger than shape by at distErr.
        /// </summary>
        protected virtual IShape BufferShape(IShape shape, double distErr)
        {
            //TODO move this generic code elsewhere?  Spatial4j?
            if (distErr <= 0)
            {
                throw new ArgumentException("distErr must be > 0");
            }
            SpatialContext ctx = grid.SpatialContext;
            if (shape is IPoint)
            {
                return ctx.MakeCircle((IPoint)shape, distErr);
            }
            else if (shape is ICircle)
            {
                var circle = (ICircle)shape;
                double newDist = circle.Radius + distErr;
                if (ctx.IsGeo && newDist > 180)
                {
                    newDist = 180;
                }
                return ctx.MakeCircle(circle.Center, newDist);
            }
            else
            {
                IRectangle bbox = shape.BoundingBox;
                double newMinX = bbox.MinX - distErr;
                double newMaxX = bbox.MaxX + distErr;
                double newMinY = bbox.MinY - distErr;
                double newMaxY = bbox.MaxY + distErr;
                if (ctx.IsGeo)
                {
                    if (newMinY < -90)
                    {
                        newMinY = -90;
                    }
                    if (newMaxY > 90)
                    {
                        newMaxY = 90;
                    }
                    if (newMinY == -90 || newMaxY == 90 || bbox.Width + 2 * distErr > 360)
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
                    newMinX = Math.Max(newMinX, ctx.WorldBounds.MinX);
                    newMaxX = Math.Min(newMaxX, ctx.WorldBounds.MaxX);
                    newMinY = Math.Max(newMinY, ctx.WorldBounds.MinY);
                    newMaxY = Math.Min(newMaxY, ctx.WorldBounds.MaxY);
                }
                return ctx.MakeRectangle(newMinX, newMaxX, newMinY, newMaxY);
            }
        }

        /// <exception cref="System.IO.IOException"></exception>
        public override DocIdSet GetDocIdSet(AtomicReaderContext context, IBits acceptDocs)
        {
            return new _VisitorTemplate_121(this, context, acceptDocs, true).GetDocIdSet();
        }

        #region Nested type: _VisitorTemplate_121

        private sealed class _VisitorTemplate_121 : VisitorTemplate
        {
            private FixedBitSet inside;
            private FixedBitSet outside;
            private SpatialRelation visitRelation;

            public _VisitorTemplate_121(WithinPrefixTreeFilter outerInstance, AtomicReaderContext context, 
                IBits acceptDocs, bool hasIndexedLeaves)
                : base(outerInstance, context, acceptDocs, hasIndexedLeaves)
            {
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
                return cell.GetSubCells(((WithinPrefixTreeFilter)outerInstance).bufferedQueryShape).GetEnumerator();
            }

            protected internal override bool Visit(Cell cell)
            {
                //cell.relate is based on the bufferedQueryShape; we need to examine what
                // the relation is against the queryShape
                visitRelation = cell.Shape.Relate(outerInstance.queryShape);
                if (visitRelation == SpatialRelation.WITHIN)
                {
                    CollectDocs(inside);
                    return false;
                }
                else if (visitRelation == SpatialRelation.DISJOINT)
                {
                    CollectDocs(outside);
                    return false;
                }
                else if (cell.Level == outerInstance.detailLevel)
                {
                    CollectDocs(inside);
                    return false;
                }
                return true;
            }

            /// <exception cref="System.IO.IOException"></exception>
            protected internal override void VisitLeaf(Cell cell)
            {
                //visitRelation is declared as a field, populated by visit() so we don't recompute it
                Debug.Assert(outerInstance.detailLevel != cell.Level);
                Debug.Assert(visitRelation == cell.Shape.Relate(outerInstance.queryShape));
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
            private bool AllCellsIntersectQuery(Cell cell, SpatialRelation relate/*cell to query*/)
            {
                if (relate == SpatialRelation.NOT_SET)
                {
                    relate = cell.Shape.Relate(outerInstance.queryShape);
                }
                if (cell.Level == outerInstance.detailLevel)
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
                    if (!AllCellsIntersectQuery(subCell, SpatialRelation.NOT_SET))
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
                if (AllCellsIntersectQuery(cell, SpatialRelation.NOT_SET))
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