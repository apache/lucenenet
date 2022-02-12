using Lucene.Net.Diagnostics;
using Spatial4n.Context;
using Spatial4n.Shapes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using JCG = J2N.Collections.Generic;

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
    /// A <see cref="SpatialPrefixTree"/> which uses a
    /// <a href="http://en.wikipedia.org/wiki/Quadtree">quad tree</a> in which an
    /// indexed term will be generated for each cell, 'A', 'B', 'C', 'D'.
    /// <para/>
    /// @lucene.experimental
    /// </summary>
    public class QuadPrefixTree : SpatialPrefixTree
    {
        // LUCENENET specific - de-nested Factory and renamed QuadPrefixTreeFactory

        public const int MAX_LEVELS_POSSIBLE = 50;//not really sure how big this should be

        public const int DEFAULT_MAX_LEVELS = 12;
        private readonly double xmin;
        private readonly double xmax;
        private readonly double ymin;
        private readonly double ymax;
        private readonly double xmid;
        private readonly double ymid;

        private readonly double gridW;
        public double GridH => gridH;
        private readonly double gridH;
        
        internal readonly double[] levelW;
        internal readonly double[] levelH;
        internal readonly int[] levelS; // side
        internal readonly int[] levelN; // number

        /// <summary>
        /// Initializes a new instance of <see cref="QuadPrefixTree"/> with the specified
        /// spatial context (<paramref name="ctx"/>), <paramref name="bounds"/> and <paramref name="maxLevels"/>.
        /// </summary>
        /// <param name="ctx">The spatial context.</param>
        /// <param name="bounds">The bounded rectangle.</param>
        /// <param name="maxLevels">The maximum number of levels in the tree.</param>
        /// <exception cref="ArgumentNullException"><paramref name="ctx"/> or <paramref name="bounds"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="maxLevels"/> is less than or equal to 0.</exception>
        public QuadPrefixTree(SpatialContext ctx, IRectangle bounds, int maxLevels)
            : base(ctx, maxLevels)
        {
            // LUCENENET specific - added guard clause
            if (bounds is null)
                throw new ArgumentNullException(nameof(bounds));

            xmin = bounds.MinX;
            xmax = bounds.MaxX;
            ymin = bounds.MinY;
            ymax = bounds.MaxY;

            levelW = new double[maxLevels];
            levelH = new double[maxLevels];
            levelS = new int[maxLevels];
            levelN = new int[maxLevels];

            gridW = xmax - xmin;
            gridH = ymax - ymin;
            this.xmid = xmin + gridW / 2.0;
            this.ymid = ymin + gridH / 2.0;
            levelW[0] = gridW / 2.0;
            levelH[0] = gridH / 2.0;
            levelS[0] = 2;
            levelN[0] = 4;

            for (int i = 1; i < levelW.Length; i++)
            {
                levelW[i] = levelW[i - 1] / 2.0;
                levelH[i] = levelH[i - 1] / 2.0;
                levelS[i] = levelS[i - 1] * 2;
                levelN[i] = levelN[i - 1] * 4;
            }
        }

        /// <summary>
        /// Initializes a new instance of <see cref="QuadPrefixTree"/> with the specified
        /// spatial context (<paramref name="ctx"/>).
        /// </summary>
        /// <param name="ctx">The spatial context.</param>
        /// <exception cref="ArgumentNullException"><paramref name="ctx"/> is <c>null</c>.</exception>
        public QuadPrefixTree(SpatialContext ctx)
            : this(ctx, DEFAULT_MAX_LEVELS)
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="QuadPrefixTree"/> with the specified
        /// spatial context (<paramref name="ctx"/>) and <paramref name="maxLevels"/>.
        /// </summary>
        /// <param name="ctx">The spatial context.</param>
        /// <param name="maxLevels">The maximum number of levels in the tree.</param>
        /// <exception cref="ArgumentNullException"><paramref name="ctx"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="maxLevels"/> is less than or equal to 0.</exception>
        public QuadPrefixTree(SpatialContext ctx, int maxLevels)
            : this(ctx, ctx?.WorldBounds!, maxLevels)
        {
        }

        public virtual void PrintInfo(TextWriter output)
        {
            // LUCENENET specific - added guard clause
            if (output is null)
                throw new ArgumentNullException(nameof(output));

            // Format the number to min 3 integer digits and exactly 5 fraction digits
            const string FORMAT_STR = @"000.00000";
            for (int i = 0; i < m_maxLevels; i++)
            {
                output.WriteLine(i + "]\t" + levelW[i].ToString(FORMAT_STR) + "\t" + levelH[i].ToString(FORMAT_STR) + "\t" +
                               levelS[i] + "\t" + (levelS[i] * levelS[i]));
            }
        }

        public override int GetLevelForDistance(double dist)
        {
            if (dist == 0)//short circuit
            {
                return m_maxLevels;
            }
            for (int i = 0; i < m_maxLevels - 1; i++)
            {
                //note: level[i] is actually a lookup for level i+1
                if (dist > levelW[i] && dist > levelH[i])
                {
                    return i + 1;
                }
            }
            return m_maxLevels;
        }

        protected internal override Cell GetCell(IPoint p, int level)
        {
            // LUCENENET specific - added guard clause
            if (p is null)
                throw new ArgumentNullException(nameof(p));

            IList<Cell> cells = new JCG.List<Cell>(1);
            Build(xmid, ymid, 0, cells, new StringBuilder(), m_ctx.MakePoint(p.X, p.Y), level);
            return cells[0];
        }

        //note cells could be longer if p on edge
        public override Cell GetCell(string token)
        {
            return new QuadCell(this, token);
        }

        public override Cell GetCell(byte[] bytes, int offset, int length)
        {
            return new QuadCell(this, bytes, offset, length);
        }

        private void Build(
            double x, 
            double y, 
            int level, 
            IList<Cell> matches, 
            StringBuilder str, 
            IShape shape, 
            int maxLevel)
        {
            if (Debugging.AssertsEnabled) Debugging.Assert(str.Length == level);
            double w = levelW[level] / 2;
            double h = levelH[level] / 2;

            // Z-Order
            // http://en.wikipedia.org/wiki/Z-order_%28curve%29
            CheckBattenberg('A', x - w, y + h, level, matches, str, shape, maxLevel);
            CheckBattenberg('B', x + w, y + h, level, matches, str, shape, maxLevel);
            CheckBattenberg('C', x - w, y - h, level, matches, str, shape, maxLevel);
            CheckBattenberg('D', x + w, y - h, level, matches, str, shape, maxLevel);
        }

        // possibly consider hilbert curve
        // http://en.wikipedia.org/wiki/Hilbert_curve
        // http://blog.notdot.net/2009/11/Damn-Cool-Algorithms-Spatial-indexing-with-Quadtrees-and-Hilbert-Curves
        // if we actually use the range property in the query, this could be useful
        private void CheckBattenberg(
            char c, 
            double cx, 
            double cy, 
            int level, 
            IList<Cell> matches, 
            StringBuilder str,
            IShape shape, 
            int maxLevel)
        {
            if (Debugging.AssertsEnabled) Debugging.Assert(str.Length == level);
            double w = levelW[level] / 2;
            double h = levelH[level] / 2;

            int strlen = str.Length;
            IRectangle rectangle = m_ctx.MakeRectangle(cx - w, cx + w, cy - h, cy + h);
            SpatialRelation v = shape.Relate(rectangle);
            if (SpatialRelation.Contains == v)
            {
                str.Append(c);
                //str.append(SpatialPrefixGrid.COVER);
                matches.Add(new QuadCell(this, str.ToString(), v.Transpose()));
            }
            else if (SpatialRelation.Disjoint == v)
            {
                // nothing
            }
            else // SpatialRelation.WITHIN, SpatialRelation.INTERSECTS
            {
                str.Append(c);

                int nextLevel = level + 1;
                if (nextLevel >= maxLevel)
                {
                    //str.append(SpatialPrefixGrid.INTERSECTS);
                    matches.Add(new QuadCell(this, str.ToString(), v.Transpose()));
                }
                else
                {
                    Build(cx, cy, nextLevel, matches, str, shape, maxLevel);
                }
            }
            str.Length = strlen;
        }

        #region Nested type: QuadCell

        internal class QuadCell : Cell
        {
            public QuadCell(QuadPrefixTree outerInstance, string token)
                : base(outerInstance, token)
            {
            }

            public QuadCell(QuadPrefixTree outerInstance, string token, SpatialRelation shapeRel)
                : base(outerInstance, token)
            {
                this.m_shapeRel = shapeRel;
            }

            internal QuadCell(QuadPrefixTree outerInstance, byte[] bytes, int off, int len)
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
                QuadPrefixTree outerInstance = (QuadPrefixTree)this.m_spatialPrefixTree;
                return new JCG.List<Cell>(4)
                {
                    new QuadCell(outerInstance, TokenString + "A"),
                    new QuadCell(outerInstance, TokenString + "B"),
                    new QuadCell(outerInstance, TokenString + "C"),
                    new QuadCell(outerInstance, TokenString + "D")
                };
            }

            public override int SubCellsSize => 4;

            public override Cell GetSubCell(IPoint p)
            {
                return m_spatialPrefixTree.GetCell(p, Level + 1);//not performant!
            }

            private IShape? shape; //cache

            public override IShape Shape
            {
                get
                {
                    if (shape is null)
                    {
                        shape = MakeShape();
                    }
                    return shape;
                }
            }

            private IRectangle MakeShape()
            {
                QuadPrefixTree outerInstance = (QuadPrefixTree)this.m_spatialPrefixTree;
                string token = TokenString;
                double xmin = outerInstance.xmin;
                double ymin = outerInstance.ymin;
                for (int i = 0; i < token.Length; i++)
                {
                    char c = token[i];
                    if ('A' == c || 'a' == c)
                    {
                        ymin += outerInstance.levelH[i];
                    }
                    else if ('B' == c || 'b' == c)
                    {
                        xmin += outerInstance.levelW[i];
                        ymin += outerInstance.levelH[i];
                    }
                    else if ('C' == c || 'c' == c)
                    {
                        // nothing really
                    }
                    else if ('D' == c || 'd' == c)
                    {
                        xmin += outerInstance.levelW[i];
                    }
                    else
                    {
                        throw RuntimeException.Create("unexpected char: " + c);
                    }
                }
                int len = token.Length;
                double width;
                double height;
                if (len > 0)
                {
                    width = outerInstance.levelW[len - 1];
                    height = outerInstance.levelH[len - 1];
                }
                else
                {
                    width = outerInstance.gridW;
                    height = outerInstance.gridH;
                }
                return outerInstance.m_ctx.MakeRectangle(xmin, xmin + width, ymin, ymin + height);
            }
        }//QuadCell

        #endregion
    }

    /// <summary>
    /// Factory for creating <see cref="QuadPrefixTree"/> instances with useful defaults
    /// </summary>
    public class QuadPrefixTreeFactory : SpatialPrefixTreeFactory
    {
        protected internal override int GetLevelForDistance(double degrees)
        {
            // LUCENENET specific - added guard clause
            if (m_ctx is null)
                throw new InvalidOperationException($"{nameof(m_ctx)} must be set prior to calling GetLevelForDistance(double).");

            var grid = new QuadPrefixTree(m_ctx, QuadPrefixTree.MAX_LEVELS_POSSIBLE);
            return grid.GetLevelForDistance(degrees);
        }

        protected internal override SpatialPrefixTree NewSPT()
        {
            // LUCENENET specific - added guard clause
            if (m_ctx is null)
                throw new InvalidOperationException($"{nameof(m_ctx)} must be set prior to calling NewSPT().");

            return new QuadPrefixTree(m_ctx, m_maxLevels ?? QuadPrefixTree.MAX_LEVELS_POSSIBLE);
        }
    }
}