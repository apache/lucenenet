using Lucene.Net.Search;
using Lucene.Net.Spatial.Prefix.Tree;
using Lucene.Net.Spatial.Queries;
using Spatial4n.Shapes;
using System;

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
    /// A <see cref="PrefixTreeStrategy"/> which uses <see cref="AbstractVisitingPrefixTreeFilter"/>.
    /// This strategy has support for searching non-point shapes (note: not tested).
    /// Even a query shape with distErrPct=0 (fully precise to the grid) should have
    /// good performance for typical data, unless there is a lot of indexed data
    /// coincident with the shape's edge.
    /// <para/>
    /// @lucene.experimental
    /// </summary>
    public class RecursivePrefixTreeStrategy : PrefixTreeStrategy
    {
        private int prefixGridScanLevel;

        /// <summary>
        /// True if only indexed points shall be supported.  See <see cref="IntersectsPrefixTreeFilter.hasIndexedLeaves"/>.
        /// </summary>
        protected bool m_pointsOnly = false;

        /// <summary>
        /// See <see cref="ContainsPrefixTreeFilter.m_multiOverlappingIndexedShapes"/>.
        /// </summary>
        protected bool m_multiOverlappingIndexedShapes = true;

        public RecursivePrefixTreeStrategy(SpatialPrefixTree grid, string fieldName)
            : base(grid, fieldName, true) //simplify indexed cells
        {
            prefixGridScanLevel = grid.MaxLevels - 4;//TODO this default constant is dependent on the prefix grid size
        }

        /// <summary>
        /// Sets the grid level [1-maxLevels] at which indexed terms are scanned brute-force
        /// instead of by grid decomposition. By default this is maxLevels - 4.  The
        /// final level, maxLevels, is always scanned. Value can be 1 to maxLevels.
        /// </summary>
        public virtual int PrefixGridScanLevel
        {
            get => prefixGridScanLevel; // LUCENENET NOTE: Added getter per MSDN guidelines
            set =>
                //TODO if negative then subtract from maxlevels
                prefixGridScanLevel = value;
        }

        public override string ToString()
        {
            return GetType().Name + "(prefixGridScanLevel:" + prefixGridScanLevel + ",SPG:(" + m_grid + "))";
        }

        public override Filter MakeFilter(SpatialArgs args)
        {
            // LUCENENET specific - added guard clause
            if (args is null)
                throw new ArgumentNullException(nameof(args));

            SpatialOperation op = args.Operation;
            if (op == SpatialOperation.IsDisjointTo)
            {
                return new DisjointSpatialFilter(this, args, FieldName);
            }
            IShape shape = args.Shape;
            int detailLevel = m_grid.GetLevelForDistance(args.ResolveDistErr(m_ctx, m_distErrPct));

        
            if (m_pointsOnly || op == SpatialOperation.Intersects)
            {
                return new IntersectsPrefixTreeFilter(
                    shape, FieldName, m_grid, detailLevel, prefixGridScanLevel, !m_pointsOnly);
            }
            else if (op == SpatialOperation.IsWithin)
            {
                return new WithinPrefixTreeFilter(
                    shape, FieldName, m_grid, detailLevel, prefixGridScanLevel, 
                    -1); //-1 flag is slower but ensures correct results
            }
            else if (op == SpatialOperation.Contains)
            {
                return new ContainsPrefixTreeFilter(shape, FieldName, m_grid, detailLevel, 
                    m_multiOverlappingIndexedShapes);
            }
            throw new UnsupportedSpatialOperationException(op);
        }
    }
}