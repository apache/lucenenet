using Lucene.Net.Spatial.Prefix.Tree;
using Lucene.Net.Spatial.Util;
using Lucene.Net.Util;
using Spatial4n.Shapes;
using System;
using System.Diagnostics.CodeAnalysis;

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
    /// Implementation of <see cref="ShapeFieldCacheProvider{T}"/>
    /// designed for <see cref="PrefixTreeStrategy">PrefixTreeStrategy</see>s.
    /// 
    /// Note, due to the fragmented representation of Shapes in these Strategies, this implementation
    /// can only retrieve the central <see cref="IPoint">Point</see> of the original Shapes.
    /// 
    /// @lucene.internal
    /// </summary>
    public class PointPrefixTreeFieldCacheProvider : ShapeFieldCacheProvider<IPoint>
    {
        private readonly SpatialPrefixTree grid; //

        public PointPrefixTreeFieldCacheProvider(SpatialPrefixTree grid, string shapeField, int defaultSize)
            : base(shapeField, defaultSize)
        {
            // LUCENENT specific - added guard clause
            this.grid = grid ?? throw new ArgumentNullException(nameof(grid));
        }

        private Cell? scanCell = null;//re-used in readShape to save GC

        [return: MaybeNull]
        protected override IPoint ReadShape(BytesRef term)
        {
            scanCell = grid.GetCell(term.Bytes, term.Offset, term.Length, scanCell);
            if (scanCell.Level == grid.MaxLevels && !scanCell.IsLeaf)
            {
                return scanCell.Center;
            }
            return null;
        }
    }
}
