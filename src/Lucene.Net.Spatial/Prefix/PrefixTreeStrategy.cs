using Lucene.Net.Analysis;
using Lucene.Net.Analysis.TokenAttributes;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Queries.Function;
using Lucene.Net.Spatial.Prefix.Tree;
using Lucene.Net.Spatial.Queries;
using Lucene.Net.Spatial.Util;
using Spatial4n.Shapes;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

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
    /// An abstract SpatialStrategy based on <see cref="SpatialPrefixTree"/>. The two
    /// subclasses are <see cref="RecursivePrefixTreeStrategy">RecursivePrefixTreeStrategy</see> and
    /// <see cref="TermQueryPrefixTreeStrategy">TermQueryPrefixTreeStrategy</see>.  This strategy is most effective as a fast
    /// approximate spatial search filter.
    /// 
    /// <h4>Characteristics:</h4>
    /// <list type="bullet">
    /// <item><description>Can index any shape; however only
    /// <see cref="RecursivePrefixTreeStrategy">RecursivePrefixTreeStrategy</see>
    /// can effectively search non-point shapes.</description></item>
    /// <item><description>Can index a variable number of shapes per field value. This strategy
    /// can do it via multiple calls to <see cref="CreateIndexableFields(IShape)"/>
    /// for a document or by giving it some sort of Shape aggregate (e.g. NTS
    /// WKT MultiPoint).  The shape's boundary is approximated to a grid precision.
    /// </description></item>
    /// <item><description>Can query with any shape.  The shape's boundary is approximated to a grid
    /// precision.</description></item>
    /// <item><description>Only <see cref="SpatialOperation.Intersects"/>
    /// is supported.  If only points are indexed then this is effectively equivalent
    /// to IsWithin.</description></item>
    /// <item><description>The strategy supports <see cref="MakeDistanceValueSource(IPoint, double)"/>
    /// even for multi-valued data, so long as the indexed data is all points; the
    /// behavior is undefined otherwise.  However, <c>it will likely be removed in
    /// the future</c> in lieu of using another strategy with a more scalable
    /// implementation.  Use of this call is the only
    /// circumstance in which a cache is used.  The cache is simple but as such
    /// it doesn't scale to large numbers of points nor is it real-time-search
    /// friendly.</description></item>
    /// </list>
    /// 
    /// <h4>Implementation:</h4>
    /// The <see cref="SpatialPrefixTree"/>
    /// does most of the work, for example returning
    /// a list of terms representing grids of various sizes for a supplied shape.
    /// An important
    /// configuration item is <see cref="DistErrPct"/> which balances
    /// shape precision against scalability.  See those docs.
    /// <para/>
    /// @lucene.internal
    /// </summary>
    public abstract class PrefixTreeStrategy : SpatialStrategy
    {
        protected readonly SpatialPrefixTree m_grid;

        // LUCENENET specific - use Lazy<T> to make the create operation atomic. See #417.
        private readonly ConcurrentDictionary<string, Lazy<PointPrefixTreeFieldCacheProvider>> provider =
            new ConcurrentDictionary<string, Lazy<PointPrefixTreeFieldCacheProvider>>();

        protected readonly bool m_simplifyIndexedCells;
        protected int m_defaultFieldValuesArrayLen = 2;
        protected double m_distErrPct = SpatialArgs.DEFAULT_DISTERRPCT;// [ 0 TO 0.5 ]

        protected PrefixTreeStrategy(SpatialPrefixTree grid, string fieldName, bool simplifyIndexedCells) // LUCENENET: CA1012: Abstract types should not have constructors (marked protected)
            // LUCENENET specific - added guard clause
            : base(grid is null ? throw new ArgumentNullException(nameof(grid)) : grid.SpatialContext, fieldName)
        {
            this.m_grid = grid;
            this.m_simplifyIndexedCells = simplifyIndexedCells;
        }

        /// <summary>
        /// A memory hint used by <see cref="SpatialStrategy.MakeDistanceValueSource(IPoint)"/>
        /// for how big the initial size of each Document's array should be. The
        /// default is 2.  Set this to slightly more than the default expected number
        /// of points per document.
        /// </summary>
        public virtual int DefaultFieldValuesArrayLen
        {
            get => m_defaultFieldValuesArrayLen; // LUCENENET NOTE: Added getter per MSDN guidelines
            set => m_defaultFieldValuesArrayLen = value;
        }

        /// <summary>
        /// The default measure of shape precision affecting shapes at index and query
        /// times.
        /// </summary>
        /// <remarks>
        /// The default measure of shape precision affecting shapes at index and query
        /// times. Points don't use this as they are always indexed at the configured
        /// maximum precision (<see cref="SpatialPrefixTree.MaxLevels"/>);
        /// this applies to all other shapes. Specific shapes at index and query time
        /// can use something different than this default value.  If you don't set a
        /// default then the default is <see cref="SpatialArgs.DEFAULT_DISTERRPCT"/> --
        /// 2.5%.
        /// </remarks>
        /// <seealso cref="SpatialArgs.DistErrPct"/>
        public virtual double DistErrPct
        {
            get => m_distErrPct;
            set => m_distErrPct = value;
        }

        public override Field[] CreateIndexableFields(IShape shape)
        {
            // LUCENENET: Added guard clause
            if (shape is null)
                throw new ArgumentNullException(nameof(shape));

            double distErr = SpatialArgs.CalcDistanceFromErrPct(shape, m_distErrPct, m_ctx);
            return CreateIndexableFields(shape, distErr);
        }

        public virtual Field[] CreateIndexableFields(IShape? shape, double distErr)
        {
            int detailLevel = m_grid.GetLevelForDistance(distErr);
            IList<Cell> cells = m_grid.GetCells(shape, detailLevel, true, m_simplifyIndexedCells);//intermediates cells

            //TODO is CellTokenStream supposed to be re-used somehow? see Uwe's comments:
            //  http://code.google.com/p/lucene-spatial-playground/issues/detail?id=4

            Field field = new Field(FieldName, new CellTokenStream(cells.GetEnumerator()), FIELD_TYPE);
            return new Field[] { field };
        }

        /// <summary>
        /// Indexed, tokenized, not stored.
        /// </summary>
        // LUCENENET: Avoid static constructors (see https://github.com/apache/lucenenet/pull/224#issuecomment-469284006)
        public static readonly FieldType FIELD_TYPE = new FieldType
        {
            IsIndexed = true,
            IsTokenized = true,
            OmitNorms = true,
            IndexOptions = IndexOptions.DOCS_ONLY
        }.Freeze();

        /// <summary>Outputs the tokenString of a cell, and if its a leaf, outputs it again with the leaf byte.</summary>
        internal sealed class CellTokenStream : TokenStream
        {
            private readonly ICharTermAttribute termAtt;

            private readonly IEnumerator<Cell> iter; // LUCENENET specific - marked readonly and got rid of null setting

            public CellTokenStream(IEnumerator<Cell> tokens)
            {
                // LUCENENET specific - added guard clause
                this.iter = tokens ?? throw new ArgumentNullException(nameof(tokens));
                termAtt = AddAttribute<ICharTermAttribute>();
            }

            internal string? nextTokenStringNeedingLeaf = null;

            public override bool IncrementToken()
            {
                ClearAttributes();
                if (nextTokenStringNeedingLeaf != null)
                {
                    termAtt.Append(nextTokenStringNeedingLeaf);
                    termAtt.Append((char)Cell.LEAF_BYTE);
                    nextTokenStringNeedingLeaf = null;
                    return true;
                }
                if (iter.MoveNext())
                {
                    Cell cell = iter.Current;
                    string token = cell.TokenString;
                    termAtt.Append(token);
                    if (cell.IsLeaf)
                    {
                        nextTokenStringNeedingLeaf = token;
                    }
                    return true;
                }
                return false;
            }

            /// <summary>
            /// Releases resources used by the <see cref="CellTokenStream"/> and
            /// if overridden in a derived class, optionally releases unmanaged resources.
            /// </summary>
            /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources;
            /// <c>false</c> to release only unmanaged resources.</param>

            // LUCENENET specific
            protected override void Dispose(bool disposing)
            {
                try
                {
                    if (disposing)
                    {
                        iter.Dispose(); // LUCENENET specific - dispose iter
                    }
                }
                finally
                {
                    base.Dispose(disposing);
                }
            }
        }

        public override ValueSource MakeDistanceValueSource(IPoint queryPoint, double multiplier)
        {
            // LUCENENET specific - added guard clause
            if (queryPoint is null)
                throw new ArgumentNullException(nameof(queryPoint));

            // LUCENENET specific - use Lazy<T> to make the create operation atomic. See #417.
            var p = provider.GetOrAdd(FieldName,
                f => new Lazy<PointPrefixTreeFieldCacheProvider>(
                    () => new PointPrefixTreeFieldCacheProvider(m_grid, FieldName, m_defaultFieldValuesArrayLen)));
            return new ShapeFieldCacheDistanceValueSource(m_ctx, p.Value!, queryPoint, multiplier);
        }

        public virtual SpatialPrefixTree Grid => m_grid;
    }
}
