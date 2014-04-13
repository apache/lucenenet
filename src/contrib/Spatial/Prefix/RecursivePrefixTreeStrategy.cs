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

        public RecursivePrefixTreeStrategy(SpatialPrefixTree grid, string fieldName)
            : base(grid, fieldName, true)
        {
            //simplify indexed cells
            prefixGridScanLevel = grid.GetMaxLevels() - 4;
        }

        //TODO this default constant is dependent on the prefix grid size
        /// <summary>
        /// Sets the grid level [1-maxLevels] at which indexed terms are scanned brute-force
        /// instead of by grid decomposition.
        /// </summary>
        /// <remarks>
        /// Sets the grid level [1-maxLevels] at which indexed terms are scanned brute-force
        /// instead of by grid decomposition.  By default this is maxLevels - 4.  The
        /// final level, maxLevels, is always scanned.
        /// </remarks>
        /// <param name="prefixGridScanLevel">1 to maxLevels</param>
        public virtual void SetPrefixGridScanLevel(int prefixGridScanLevel)
        {
            //TODO if negative then subtract from maxlevels
            this.prefixGridScanLevel = prefixGridScanLevel;
        }

        public override string ToString()
        {
            return GetType().Name + "(prefixGridScanLevel:" + prefixGridScanLevel + ",SPG:("
                   + grid + "))";
        }

        public override Filter MakeFilter(SpatialArgs args)
        {
            SpatialOperation op = args.Operation;
            if (op == SpatialOperation.IsDisjointTo)
            {
                return new DisjointSpatialFilter(this, args, GetFieldName());
            }
            Shape shape = args.Shape;
            int detailLevel = grid.GetLevelForDistance(args.ResolveDistErr(ctx, distErrPct));
            bool hasIndexedLeaves = true;
            if (op == SpatialOperation.Intersects)
            {
                return new IntersectsPrefixTreeFilter(shape, GetFieldName(), grid, detailLevel, prefixGridScanLevel
                                                      , hasIndexedLeaves);
            }
            else
            {
                if (op == SpatialOperation.IsWithin)
                {
                    return new WithinPrefixTreeFilter(shape, GetFieldName(), grid, detailLevel, prefixGridScanLevel
                                                      , -1);
                }
                else
                {
                    //-1 flag is slower but ensures correct results
                    if (op == SpatialOperation.Contains)
                    {
                        return new ContainsPrefixTreeFilter(shape, GetFieldName(), grid, detailLevel);
                    }
                }
            }
            throw new UnsupportedSpatialOperation(op);
        }
    }
}