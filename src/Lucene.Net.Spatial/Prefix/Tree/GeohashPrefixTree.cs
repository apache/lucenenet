using Spatial4n.Core.Context;
using Spatial4n.Core.Shapes;
using Spatial4n.Core.Util;
using System;
using System.Collections.Generic;

namespace Lucene.Net.Spatial.Prefix.Tree
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
    /// A
    /// <see cref="SpatialPrefixTree">SpatialPrefixTree</see>
    /// based on
    /// <a href="http://en.wikipedia.org/wiki/Geohash">Geohashes</a>.
    /// Uses
    /// <see cref="Spatial4n.Core.IO.GeohashUtils">Spatial4n.Core.IO.GeohashUtils
    /// 	</see>
    /// to do all the geohash work.
    /// </summary>
    /// <lucene.experimental></lucene.experimental>
    public class GeohashPrefixTree : SpatialPrefixTree
    {
        #region Nested type: Factory

        /// <summary>
        /// Factory for creating
        /// <see cref="GeohashPrefixTree">GeohashPrefixTree</see>
        /// instances with useful defaults
        /// </summary>
        public class Factory : SpatialPrefixTreeFactory
        {
            protected internal override int GetLevelForDistance(double degrees)
            {
                var grid = new GeohashPrefixTree(ctx, GeohashPrefixTree.MaxLevelsPossible);
                return grid.GetLevelForDistance(degrees);
            }

            protected internal override SpatialPrefixTree NewSPT()
            {
                return new GeohashPrefixTree(ctx, maxLevels.HasValue ? maxLevels.Value : GeohashPrefixTree.MaxLevelsPossible);
            }
        }

        #endregion

        public GeohashPrefixTree(SpatialContext ctx, int maxLevels)
            : base(ctx, maxLevels)
        {
            IRectangle bounds = ctx.WorldBounds;
            if (bounds.MinX != -180)
            {
                throw new ArgumentException("Geohash only supports lat-lon world bounds. Got " + bounds);
            }
            int Maxp = MaxLevelsPossible;
            if (maxLevels <= 0 || maxLevels > Maxp)
            {
                throw new ArgumentException("maxLen must be [1-" + Maxp + "] but got " + maxLevels);
            }
        }

        /// <summary>Any more than this and there's no point (double lat & lon are the same).</summary>
        public static int MaxLevelsPossible
        {
            get { return GeohashUtils.MAX_PRECISION; }
        }

        public override int GetLevelForDistance(double dist)
        {
            if (dist == 0)
            {
                return maxLevels;//short circuit
            }
            
            int level = GeohashUtils.LookupHashLenForWidthHeight(dist, dist);
            return Math.Max(Math.Min(level, maxLevels), 1);
        }

        protected internal override Cell GetCell(IPoint p, int level)
        {
            return new GhCell(this, GeohashUtils.EncodeLatLon(p.Y, p.X, level));
        }

        //args are lat,lon (y,x)
        public override Cell GetCell(string token)
        {
            return new GhCell(this, token);
        }

        public override Cell GetCell(byte[] bytes, int offset, int len)
        {
            return new GhCell(this, bytes, offset, len);
        }

        #region Nested type: GhCell

        internal class GhCell : Cell
        {
            internal GhCell(GeohashPrefixTree outerInstance, string token)
                : base(outerInstance, token)
            {
            }

            internal GhCell(GeohashPrefixTree outerInstance, byte[] bytes, int off, int len)
                : base(outerInstance, bytes, off, len)
            {
            }

            public override void Reset(byte[] bytes, int off, int len)
            {
                base.Reset(bytes, off, len);
                shape = null;
            }

            protected internal override ICollection<Cell> GetSubCells()
            {
                string[] hashes = GeohashUtils.GetSubGeohashes(Geohash);//sorted
                IList<Cell> cells = new List<Cell>(hashes.Length);
                foreach (string hash in hashes)
                {
                    cells.Add(new GhCell((GeohashPrefixTree)outerInstance, hash));
                }
                return cells;
            }

            public override int SubCellsSize
            {
                get { return 32; }//8x4
            }

            public override Cell GetSubCell(IPoint p)
            {
                return outerInstance.GetCell(p, Level + 1);//not performant!
            }

            private IShape shape;//cache

            public override IShape Shape
            {
                get
                {
                    if (shape == null)
                    {
                        shape = GeohashUtils.DecodeBoundary(Geohash, outerInstance.ctx);
                    }
                    return shape;
                }
            }

            public override IPoint Center
            {
                get { return GeohashUtils.Decode(Geohash, outerInstance.ctx); }
            }

            private string Geohash
            {
                get { return TokenString; }
            }

            //class GhCell
        }

        #endregion
    }
}