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

using Lucene.Net.Queries.Function;
using Lucene.Net.Search;
using Lucene.Net.Spatial.Prefix.Tree;
using Lucene.Net.Spatial.Queries;
using Spatial4n.Core.Shapes;

namespace Lucene.Net.Spatial.Prefix
{
    /// <summary>
    /// A
    /// <see cref="PrefixTreeStrategy">PrefixTreeStrategy</see>
    /// which uses
    /// <see cref="AbstractVisitingPrefixTreeFilter">AbstractVisitingPrefixTreeFilter</see>
    /// .
    /// This strategy has support for searching non-point shapes (note: not tested).
    /// Even a query shape with distErrPct=0 (fully precise to the grid) should have
    /// good performance for typical data, unless there is a lot of indexed data
    /// coincident with the shape's edge.
    /// </summary>
    /// <lucene.experimental></lucene.experimental>
    public class RecursivePrefixTreeStrategy : PrefixTreeStrategy
    {
        private int prefixGridScanLevel;
        
        /** True if only indexed points shall be supported.  See
        *  {@link IntersectsPrefixTreeFilter#hasIndexedLeaves}. */
        protected bool pointsOnly = false;

        /** See {@link ContainsPrefixTreeFilter#multiOverlappingIndexedShapes}. */
        protected bool multiOverlappingIndexedShapes = true;

        public RecursivePrefixTreeStrategy(SpatialPrefixTree grid, string fieldName)
            : base(grid, fieldName, true) //simplify indexed cells
        {
            prefixGridScanLevel = grid.MaxLevels - 4;//TODO this default constant is dependent on the prefix grid size
        }

        /// <summary>
        /// Sets the grid level [1-maxLevels] at which indexed terms are scanned brute-force
        /// instead of by grid decomposition.By default this is maxLevels - 4.  The
        /// final level, maxLevels, is always scanned.
        /// </summary>
        public virtual int PrefixGridScanLevel
        {
            set
            {
                //TODO if negative then subtract from maxlevels
                prefixGridScanLevel = value;
            }
        }

        public override string ToString()
        {
            return GetType().Name + "(prefixGridScanLevel:" + prefixGridScanLevel + ",SPG:(" + grid + "))";
        }

        public override Filter MakeFilter(SpatialArgs args)
        {
            SpatialOperation op = args.Operation;
            if (op == SpatialOperation.IsDisjointTo)
            {
                return new DisjointSpatialFilter(this, args, FieldName);
            }
            IShape shape = args.Shape;
            int detailLevel = grid.GetLevelForDistance(args.ResolveDistErr(ctx, distErrPct));

        
            if (pointsOnly || op == SpatialOperation.Intersects)
            {
                return new IntersectsPrefixTreeFilter(
                    shape, FieldName, grid, detailLevel, prefixGridScanLevel, !pointsOnly);
            }
            else if (op == SpatialOperation.IsWithin)
            {
                return new WithinPrefixTreeFilter(shape, FieldName, grid, detailLevel, prefixGridScanLevel
                    , -1); //-1 flag is slower but ensures correct results
            }
            else if (op == SpatialOperation.Contains)
            {
                return new ContainsPrefixTreeFilter(shape, FieldName, grid, detailLevel, 
                    multiOverlappingIndexedShapes);
            }
            throw new UnsupportedSpatialOperation(op);
        }
    }
}