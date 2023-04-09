using Lucene.Net.Diagnostics;
using Spatial4n.Shapes;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
    /// Represents a grid cell. These are not necessarily thread-safe, although new
    /// Cell("") (world cell) must be.
    /// 
    /// @lucene.experimental
    /// </summary>
    public abstract class Cell : IComparable<Cell>
    {
        /// <summary>
        /// LUCENENET specific - we need to set the SpatialPrefixTree before calling overridden 
        /// members of this class, just in case those overridden members require it. This is
        /// not possible from the subclass because the constructor of the base class runs first.
        /// So we need to move the reference here and also set it before running the normal constructor
        /// logic.
        /// </summary>
        protected readonly SpatialPrefixTree m_spatialPrefixTree;


        public const byte LEAF_BYTE = (byte)('+');//NOTE: must sort before letters & numbers

        /// <summary>
        /// Holds a byte[] and/or String representation of the cell. Both are lazy constructed from the other.
        /// Neither contains the trailing leaf byte.
        /// </summary>
        private byte[]? bytes;
        private int b_off;
        private int b_len;

        private string? token;//this is the only part of equality

        /// <summary>
        /// When set via <see cref="GetSubCells(IShape)">GetSubCells(filter)</see>, it is the relationship between this cell
        /// and the given shape filter.
        /// </summary>
        protected SpatialRelation m_shapeRel;//set in GetSubCells(filter), and via SetLeaf().

        /// <summary>Always false for points.</summary>
        /// <remarks>
        /// Always false for points. Otherwise, indicate no further sub-cells are going
        /// to be provided because shapeRel is WITHIN or maxLevels or a detailLevel is
        /// hit.
        /// </remarks>
        protected bool m_leaf;

        /// <summary>
        /// Initializes a new instance of <see cref="Cell"/> for the
        /// <paramref name="spatialPrefixTree"/> with the specified <paramref name="token"/>.
        /// </summary>
        /// <param name="spatialPrefixTree">The <see cref="SpatialPrefixTree"/> this instance belongs to.</param>
        /// <param name="token">The string representation of this <see cref="Cell"/>.</param>
        /// <exception cref="ArgumentNullException"><paramref name="spatialPrefixTree"/> or <paramref name="token"/> is <c>null</c>.</exception>
        protected Cell(SpatialPrefixTree spatialPrefixTree, string token)
        {
            // LUCENENET specific - set the outer instance here
            // because overrides of Shape may require it
            // LUCENENET specific - added guard clauses
            this.m_spatialPrefixTree = spatialPrefixTree ?? throw new ArgumentNullException(nameof(spatialPrefixTree));

            //NOTE: must sort before letters & numbers
            //this is the only part of equality
            this.token = token ?? throw new ArgumentNullException(nameof(token));
            if (token.Length > 0 && token[token.Length - 1] == (char)LEAF_BYTE)
            {
                this.token = token.Substring(0, (token.Length - 1) - 0);
                // LUCENENET specific - calling private instead of virtual to avoid initialization issues
                SetLeafInternal(); 
            }
            if (Level == 0)
            {
                var _ = Shape;//ensure any lazy instantiation completes to make this threadsafe
            }
        }

        /// <summary>
        /// Initializes a new instance of <see cref="Cell"/> for the
        /// <paramref name="spatialPrefixTree"/> with the specified <paramref name="bytes"/>, <paramref name="offset"/> and <paramref name="length"/>.
        /// </summary>
        /// <param name="spatialPrefixTree">The <see cref="SpatialPrefixTree"/> this instance belongs to.</param>
        /// <param name="bytes">The representation of this <see cref="Cell"/>.</param>
        /// <param name="offset">The offset into <paramref name="bytes"/> to use.</param>
        /// <param name="length">The count of <paramref name="bytes"/> to use.</param>
        /// <exception cref="ArgumentNullException"><paramref name="spatialPrefixTree"/> or <paramref name="bytes"/> is <c>null</c>.</exception>
        protected Cell(SpatialPrefixTree spatialPrefixTree, byte[] bytes, int offset, int length)
        {
            // LUCENENET specific - set the outer instance here
            // because overrides of Shape may require it
            this.m_spatialPrefixTree = spatialPrefixTree ?? throw new ArgumentNullException(nameof(spatialPrefixTree));

            //ensure any lazy instantiation completes to make this threadsafe
            this.bytes = bytes ?? throw new ArgumentNullException(nameof(bytes));

            // LUCENENET specific - check that the offset and length are in bounds
            int len = bytes.Length;
            if (offset < 0)
                throw new ArgumentOutOfRangeException(nameof(offset), $"{nameof(offset)} must not be negative.");
            if (length < 0)
                throw new ArgumentOutOfRangeException(nameof(length), $"{nameof(length)} must not be negative.");
            if (offset > len - length) // Checks for int overflow
                throw new ArgumentOutOfRangeException(nameof(length), $"{nameof(offset)} and {nameof(length)} must refer to a location within the array.");

            b_off = offset;
            b_len = length;
            B_fixLeaf();
        }

        public virtual void Reset(byte[] bytes, int offset, int length)
        {
            // LUCENENET specific - added guard clauses
            if (bytes is null)
                throw new ArgumentNullException(nameof(bytes));
            int len = bytes.Length;
            if (offset < 0)
                throw new ArgumentOutOfRangeException(nameof(offset));
            if (length < 0)
                throw new ArgumentOutOfRangeException(nameof(length));
            if (offset > len - length) // Checks for int overflow
                throw new ArgumentOutOfRangeException(nameof(length), "Offset and length must refer to a location within the array.");

            if (Debugging.AssertsEnabled) Debugging.Assert(Level != 0);
            token = null;
            m_shapeRel = SpatialRelation.None;
            this.bytes = bytes;
            b_off = offset;
            b_len = length;
            B_fixLeaf();
        }

        private void B_fixLeaf()
        {
            //note that non-point shapes always have the maxLevels cell set with setLeaf
            if (bytes![b_off + b_len - 1] == LEAF_BYTE)
            {
                b_len--;
                // LUCENENET specific - calling internal vs virtual
                // to avoid issues when this is called from constructor
                SetLeafInternal();
            }
            else
            {
                m_leaf = false;
            }
        }

        public virtual SpatialRelation ShapeRel => m_shapeRel;

        /// <summary>For points, this is always false.</summary>
        /// <remarks>
        /// For points, this is always false.  Otherwise this is true if there are no
        /// further cells with this prefix for the shape (always true at maxLevels).
        /// </remarks>
        public virtual bool IsLeaf => m_leaf;

        /// <summary>Note: not supported at level 0.
        /// 
        /// NOTE: When overriding this method, be aware that the constructor of this class calls 
        /// a private method and not this virtual method. So if you need to override
        /// the behavior during the initialization, call your own private method from the constructor
        /// with whatever custom behavior you need.
        /// </summary>
        public virtual void SetLeaf() => SetLeafInternal();

        // LUCENENET specific - S1699 - private method that can be called
        // from constructor without causing initialization issues
        private void SetLeafInternal()
        {
            if (Debugging.AssertsEnabled) Debugging.Assert(Level != 0);
            m_leaf = true;
        }

        /// <summary>
        /// Note: doesn't contain a trailing leaf byte.
        /// </summary>
        public virtual string TokenString
        {
            get
            {
                if (token is null)
                    token = Encoding.UTF8.GetString(bytes!, b_off, b_len);
                return token;
            }
        }

        /// <summary>Note: doesn't contain a trailing leaf byte.</summary>
        public virtual byte[] GetTokenBytes()
        {
            if (bytes != null)
            {
                if (b_off != 0 || b_len != bytes.Length)
                {
                    throw IllegalStateException.Create("Not supported if byte[] needs to be recreated.");
                }
            }
            else
            {
                bytes = Encoding.UTF8.GetBytes(token!);
                b_off = 0;
                b_len = bytes.Length;
            }
            return bytes;
        }

        public virtual int Level => token != null ? token.Length : b_len;

        //TODO add getParent() and update some algorithms to use this?
        //public Cell getParent();
        /// <summary>
        /// Like <see cref="GetSubCells()">GetSubCells()</see> but with the results filtered by a shape. If
        /// that shape is a <see cref="IPoint"/> then it must call 
        /// <see cref="GetSubCell(IPoint)"/>. The returned cells
        /// should have <see cref="ShapeRel">ShapeRel</see> set to their relation with
        /// <paramref name="shapeFilter"/>. In addition, <see cref="IsLeaf"/>
        /// must be true when that relation is <see cref="SpatialRelation.WITHIN"/>.
        /// <para/>
        /// Precondition: Never called when Level == maxLevel.
        /// </summary>
        /// <param name="shapeFilter">an optional filter for the returned cells.</param>
        /// <returns>A set of cells (no dups), sorted. Not Modifiable.</returns>
        public virtual ICollection<Cell> GetSubCells(IShape? shapeFilter)
        {
            //Note: Higher-performing subclasses might override to consider the shape filter to generate fewer cells.
            if (shapeFilter is IPoint point)
            {
                Cell subCell = GetSubCell(point);
                subCell.m_shapeRel = SpatialRelation.Contains;
                return new ReadOnlyCollection<Cell>(new[] { subCell });
            }

            ICollection<Cell> cells = GetSubCells();
            if (shapeFilter is null)
            {
                return cells;
            }
            //TODO change API to return a filtering iterator
            IList<Cell> copy = new JCG.List<Cell>(cells.Count);
            foreach (Cell cell in cells)
            {
                SpatialRelation rel = cell.Shape.Relate(shapeFilter);
                if (rel == SpatialRelation.Disjoint)
                {
                    continue;
                }
                cell.m_shapeRel = rel;
                if (rel == SpatialRelation.Within)
                {
                    cell.SetLeaf();
                }
                copy.Add(cell);
            }
            return copy;
        }

        /// <summary>
        /// Performant implementations are expected to implement this efficiently by
        /// considering the current cell's boundary.
        /// </summary>
        /// <remarks>
        /// Performant implementations are expected to implement this efficiently by
        /// considering the current cell's boundary. Precondition: Never called when
        /// Level == maxLevel.
        /// <p/>
        /// Precondition: this.Shape.Relate(p) != SpatialRelation.DISJOINT.
        /// </remarks>
        public abstract Cell GetSubCell(IPoint p);

        //TODO Cell getSubCell(byte b)
        /// <summary>Gets the cells at the next grid cell level that cover this cell.</summary>
        /// <remarks>
        /// Gets the cells at the next grid cell level that cover this cell.
        /// Precondition: Never called when Level == maxLevel.
        /// </remarks>
        /// <returns>A set of cells (no dups), sorted, modifiable, not empty, not null.</returns>
        protected internal abstract ICollection<Cell> GetSubCells();

        /// <summary>
        /// <see cref="GetSubCells()"/>.Count -- usually a constant. Should be &gt;=2
        /// </summary>
        public abstract int SubCellsSize { get; }

        public abstract IShape Shape { get; }

        public virtual IPoint Center => Shape.Center;

        #region IComparable<Cell> Members

        public virtual int CompareTo(Cell? other)
        {
            if (other is null) return 1; // LUCENENET specific - match other comparers
            return string.CompareOrdinal(TokenString, other.TokenString);
        }

        #endregion

        #region Equality overrides

        public override bool Equals(object? obj)
        {
            return !(obj is null || !(obj is Cell cell)) &&
                TokenString.Equals(cell.TokenString, StringComparison.Ordinal);
        }

        public override int GetHashCode()
        {
            return TokenString.GetHashCode();
        }

        public override string ToString()
        {
            return TokenString + (IsLeaf ? ((char)LEAF_BYTE).ToString() : string.Empty);
        }

        #endregion

    }
}