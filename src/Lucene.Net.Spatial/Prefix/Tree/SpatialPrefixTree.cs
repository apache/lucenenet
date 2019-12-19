using Spatial4n.Core.Context;
using Spatial4n.Core.Shapes;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;

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
    /// A spatial Prefix Tree, or Trie, which decomposes shapes into prefixed strings
    /// at variable lengths corresponding to variable precision.
    /// </summary>
    /// <remarks>
    /// A spatial Prefix Tree, or Trie, which decomposes shapes into prefixed strings
    /// at variable lengths corresponding to variable precision.   Each string
    /// corresponds to a rectangular spatial region.  This approach is
    /// also referred to "Grids", "Tiles", and "Spatial Tiers".
    /// <p/>
    /// Implementations of this class should be thread-safe and immutable once
    /// initialized.
    /// 
    /// @lucene.experimental
    /// </remarks>
    public abstract class SpatialPrefixTree
    {
        protected readonly int m_maxLevels;

        protected internal readonly SpatialContext m_ctx;

        public SpatialPrefixTree(SpatialContext ctx, int maxLevels)
        {
            Debug.Assert(maxLevels > 0);
            this.m_ctx = ctx;
            this.m_maxLevels = maxLevels;
        }

        public virtual SpatialContext SpatialContext
        {
            get { return m_ctx; }
        }

        public virtual int MaxLevels
        {
            get { return m_maxLevels; }
        }

        public override string ToString()
        {
            return GetType().Name + "(maxLevels:" + m_maxLevels + ",ctx:" + m_ctx + ")";
        }

        /// <summary>
        /// Returns the level of the largest grid in which its longest side is less
        /// than or equal to the provided distance (in degrees).
        /// </summary>
        /// <remarks>
        /// Returns the level of the largest grid in which its longest side is less
        /// than or equal to the provided distance (in degrees). Consequently
        /// <paramref name="dist"/> acts as an error epsilon declaring the amount of detail needed in the
        /// grid, such that you can get a grid with just the right amount of
        /// precision.
        /// </remarks>
        /// <param name="dist">&gt;= 0</param>
        /// <returns>level [1 to maxLevels]</returns>
        public abstract int GetLevelForDistance(double dist);

        /// <summary>
        /// Given a cell having the specified level, returns the distance from opposite
        /// corners.
        /// </summary>
        /// <remarks>
        /// Given a cell having the specified level, returns the distance from opposite
        /// corners. Since this might very depending on where the cell is, this method
        /// may over-estimate.
        /// </remarks>
        /// <param name="level">[1 to maxLevels]</param>
        /// <returns>&gt; 0</returns>
        public virtual double GetDistanceForLevel(int level)
        {
            if (level < 1 || level > MaxLevels)
            {
                throw new ArgumentException("Level must be in 1 to maxLevels range");
            }
            //TODO cache for each level
            Cell cell = GetCell(m_ctx.WorldBounds.Center, level);
            IRectangle bbox = cell.Shape.BoundingBox;
            double width = bbox.Width;
            double height = bbox.Height;
            //Use standard cartesian hypotenuse. For geospatial, this answer is larger
            // than the correct one but it's okay to over-estimate.
            return Math.Sqrt(width * width + height * height);
        }

#if FEATURE_SERIALIZABLE
        [NonSerialized]
#endif
        private Cell worldCell;//cached

        /// <summary>Returns the level 0 cell which encompasses all spatial data.</summary>
        /// <remarks>
        /// Returns the level 0 cell which encompasses all spatial data. Equivalent to
        /// <see cref="GetCell(string)">GetCell(string)</see> with <see cref="string.Empty"/>.
        /// This cell is threadsafe, just like a spatial prefix grid is, although cells aren't
        /// generally threadsafe.
        /// </remarks>
        /// TODO rename to GetTopCell or is this fine?
        public virtual Cell WorldCell
        {
            get
            {
                if (worldCell == null)
                {
                    worldCell = GetCell(string.Empty);
                }
                return worldCell;
            }
        }

        /// <summary>The cell for the specified token.</summary>
        /// <remarks>
        /// The cell for the specified token. The empty string should be equal to
        /// <see cref="WorldCell">WorldCell</see>.
        /// Precondition: Never called when token length &gt; maxLevel.
        /// </remarks>
        public abstract Cell GetCell(string token);

        public abstract Cell GetCell(byte[] bytes, int offset, int len);

        public Cell GetCell(byte[] bytes, int offset, int len, Cell target)
        {
            if (target == null)
            {
                return GetCell(bytes, offset, len);
            }
            target.Reset(bytes, offset, len);
            return target;
        }

        /// <summary>
        /// Returns the cell containing point <paramref name="p"/> at the specified <paramref name="level"/>.
        /// </summary>
        protected internal virtual Cell GetCell(IPoint p, int level)
        {
            return GetCells(p, level, false)[0];
        }

        /// <summary>
        /// Gets the intersecting cells for the specified shape, without exceeding
        /// detail level.
        /// </summary>
        /// <remarks>
        /// Gets the intersecting cells for the specified shape, without exceeding
        /// detail level. If a cell is within the query shape then it's marked as a
        /// leaf and none of its children are added.
        /// <para/>
        /// This implementation checks if shape is a <see cref="IPoint"/> and if so returns
        /// <see cref="GetCells(IPoint, int, bool)"/>.
        /// </remarks>
        /// <param name="shape">the shape; non-null</param>
        /// <param name="detailLevel">the maximum detail level to get cells for</param>
        /// <param name="inclParents">
        /// if true then all parent cells of leaves are returned
        /// too. The top world cell is never returned.
        /// </param>
        /// <param name="simplify">
        /// for non-point shapes, this will simply/aggregate sets of
        /// complete leaves in a cell to its parent, resulting in
        /// ~20-25% fewer cells.
        /// </param>
        /// <returns>a set of cells (no dups), sorted, immutable, non-null</returns>
        public virtual IList<Cell> GetCells(IShape shape, int detailLevel, bool inclParents, 
            bool simplify)
        {
            //TODO consider an on-demand iterator -- it won't build up all cells in memory.
            if (detailLevel > m_maxLevels)
            {
                throw new ArgumentException("detailLevel > maxLevels");
            }
            if (shape is IPoint)
            {
                return GetCells((IPoint)shape, detailLevel, inclParents);
            }
            IList<Cell> cells = new List<Cell>(inclParents ? 4096 : 2048);
            RecursiveGetCells(WorldCell, shape, detailLevel, inclParents, simplify, cells);
            return cells;
        }

        /// <summary>Returns true if cell was added as a leaf.</summary>
        /// <remarks>
        /// Returns true if cell was added as a leaf. If it wasn't it recursively
        /// descends.
        /// </remarks>
        private bool RecursiveGetCells(Cell cell, IShape shape, int detailLevel, 
            bool inclParents, bool simplify, 
            IList<Cell> result)
        {
            if (cell.Level == detailLevel)
            {
                cell.SetLeaf();//FYI might already be a leaf
            }
            if (cell.IsLeaf)
            {
                result.Add(cell);
                return true;
            }
            if (inclParents && cell.Level != 0)
            {
                result.Add(cell);
            }

            ICollection<Cell> subCells = cell.GetSubCells(shape);
            int leaves = 0;
            foreach (Cell subCell in subCells)
            {
                if (RecursiveGetCells(subCell, shape, detailLevel, inclParents, simplify, result))
                {
                    leaves++;
                }
            }
            //can we simplify?
            if (simplify && leaves == cell.SubCellsSize && cell.Level != 0)
            {
                //Optimization: substitute the parent as a leaf instead of adding all
                // children as leaves

                //remove the leaves
                do
                {
                    result.RemoveAt(result.Count - 1);//remove last
                }
                while (--leaves > 0);
                //add cell as the leaf
                cell.SetLeaf();
                if (!inclParents)// otherwise it was already added up above
                {
                    result.Add(cell);
                }
                return true;
            }
            return false;
        }

        /// <summary>
        /// A Point-optimized implementation of
        /// <see cref="GetCells(IShape, int, bool, bool)"/>. That
        /// method in facts calls this for points.
        /// <para/>
        /// This implementation depends on <see cref="GetCell(string)"/> being fast, as its
        /// called repeatedly when incPlarents is true.
        /// </summary>
        public virtual IList<Cell> GetCells(IPoint p, int detailLevel, bool inclParents)
        {
            Cell cell = GetCell(p, detailLevel);
            if (!inclParents)
            {
                return new ReadOnlyCollection<Cell>(new[] { cell });
            }
            string endToken = cell.TokenString;
            Debug.Assert(endToken.Length == detailLevel);
            IList<Cell> cells = new List<Cell>(detailLevel);
            for (int i = 1; i < detailLevel; i++)
            {
                cells.Add(GetCell(endToken.Substring(0, i - 0)));
            }
            cells.Add(cell);
            return cells;
        }

        /// <summary>Will add the trailing leaf byte for leaves. This isn't particularly efficient.</summary>
        [Obsolete("TODO remove; not used and not interesting, don't need collection in & out")]
        public static IList<string> CellsToTokenStrings(ICollection<Cell> cells)
        {
            IList<string> tokens = new List<string>((cells.Count));
            foreach (Cell cell in cells)
            {
                string token = cell.TokenString;
                if (cell.IsLeaf)
                {
                    tokens.Add(token + (char)Cell.LEAF_BYTE);
                }
                else
                {
                    tokens.Add(token);
                }
            }
            return tokens;
        }
    }
}
