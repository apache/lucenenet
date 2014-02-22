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

using System;
#if !NET35
using System.Collections.Concurrent;
#else
using Lucene.Net.Support.Compatibility;
#endif
using System.Collections.Generic;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Tokenattributes;
using Lucene.Net.Documents;
using Lucene.Net.Search.Function;
using Lucene.Net.Spatial.Prefix.Tree;
using Lucene.Net.Spatial.Queries;
using Lucene.Net.Spatial.Util;
using Spatial4n.Core.Shapes;

namespace Lucene.Net.Spatial.Prefix
{
    /// <summary>
    /// Abstract SpatialStrategy which provides common functionality for those 
    /// Strategys which use {@link SpatialPrefixTree}s
    /// </summary>
    public abstract class PrefixTreeStrategy : SpatialStrategy
    {
        protected readonly SpatialPrefixTree grid;

        private readonly IDictionary<String, PointPrefixTreeFieldCacheProvider> provider =
            new ConcurrentDictionary<string, PointPrefixTreeFieldCacheProvider>();

        protected int defaultFieldValuesArrayLen = 2;
        protected double distErrPct = SpatialArgs.DEFAULT_DISTERRPCT; // [ 0 TO 0.5 ]

        protected PrefixTreeStrategy(SpatialPrefixTree grid, String fieldName)
            : base(grid.GetSpatialContext(), fieldName)
        {
            this.grid = grid;
        }

        /* Used in the in-memory ValueSource as a default ArrayList length for this field's array of values, per doc. */

        public void SetDefaultFieldValuesArrayLen(int length)
        {
            defaultFieldValuesArrayLen = length;
        }

        /// <summary>
        /// The default measure of shape precision affecting indexed and query shapes.
        /// Specific shapes at index and query time can use something different.
        /// @see org.apache.lucene.spatial.query.SpatialArgs#getDistErrPct()
        /// </summary>
        public double DistErrPct { get; set; }

		public override Field[] CreateIndexableFields(Shape shape)
		{
		    double distErr = SpatialArgs.CalcDistanceFromErrPct(shape, distErrPct, ctx);
		    return CreateIndexableFields(shape, distErr);
		}

        /* Indexed, tokenized, not stored. */
        public static readonly FieldType FIELD_TYPE = new FieldType();

        public Field[] CreateIndexableFields(Shape shape, double distErr)
        {
            int detailLevel = grid.GetLevelForDistance(distErr);
            var cells = grid.GetNodes(shape, detailLevel, true);//true=intermediates cells

            var field = new Field(GetFieldName(), new CellTokenStream(cells.GetEnumerator()));
            return new Field[] { field };
		}

		/// <summary>
		/// Outputs the tokenString of a cell, and if its a leaf, outputs it again with the leaf byte.
		/// </summary>
		protected class CellTokenStream : TokenStream
		{
			 private CharTermAttribute termAtt;
			private readonly IEnumerator<Node> iter;

			public CellTokenStream(IEnumerator<Node> tokens)
			{
			    this.iter = tokens;
				Init();
			}

			private void Init()
			{
				termAtt = AddAttribute<CharTermAttribute>();
			}

			private string nextTokenStringNeedingLeaf;

			public override bool IncrementToken()
			{
				ClearAttributes();
				if (nextTokenStringNeedingLeaf != null)
				{
					termAtt.Append(nextTokenStringNeedingLeaf);
					termAtt.Append((char)Node.LEAF_BYTE);
					nextTokenStringNeedingLeaf = null;
					return true;
				}
				if (iter.MoveNext())
				{
					Node cell = iter.Current;
					var token = cell.GetTokenString();
					termAtt.Append(token);
					if (cell.IsLeaf())
						nextTokenStringNeedingLeaf = token;
					return true;
				}
				return false;
			}

			protected override void Dispose(bool disposing)
			{
			}
		}

		public ShapeFieldCacheProvider<Point> GetCacheProvider()
		{
			PointPrefixTreeFieldCacheProvider p;
			if (!provider.TryGetValue(GetFieldName(), out p) || p == null)
			{
				lock (this)
				{//double checked locking idiom is okay since provider is threadsafe
					if (!provider.ContainsKey(GetFieldName()))
					{
						p = new PointPrefixTreeFieldCacheProvider(grid, GetFieldName(), defaultFieldValuesArrayLen);
						provider[GetFieldName()] = p;
					}
				}
			}
			return p;
		}

        public override ValueSource MakeDistanceValueSource(Point queryPoint)
		{
			var p = (PointPrefixTreeFieldCacheProvider)GetCacheProvider();
            return new ShapeFieldCacheDistanceValueSource(ctx, p, queryPoint);
		}

		public SpatialPrefixTree GetGrid()
		{
			return grid;
		}
	}
}
