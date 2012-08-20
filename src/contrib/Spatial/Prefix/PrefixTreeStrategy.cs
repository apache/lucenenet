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
using Spatial4n.Core.Distance;
using Spatial4n.Core.Shapes;

namespace Lucene.Net.Spatial.Prefix
{
	public abstract class PrefixTreeStrategy : SpatialStrategy
	{
		protected readonly SpatialPrefixTree grid;
		private readonly string fieldName;
		private readonly IDictionary<String, PointPrefixTreeFieldCacheProvider> provider = new ConcurrentDictionary<string, PointPrefixTreeFieldCacheProvider>();
		protected int defaultFieldValuesArrayLen = 2;
		protected double distErrPct = SpatialArgs.DEFAULT_DIST_PRECISION;

		protected PrefixTreeStrategy(SpatialPrefixTree grid, String fieldName)
			: base(grid.GetSpatialContext(), fieldName)
		{
			this.grid = grid;
			this.fieldName = fieldName;
		}

		/** Used in the in-memory ValueSource as a default ArrayList length for this field's array of values, per doc. */
		public void SetDefaultFieldValuesArrayLen(int defaultFieldValuesArrayLen)
		{
			this.defaultFieldValuesArrayLen = defaultFieldValuesArrayLen;
		}

		/** See {@link SpatialPrefixTree#getMaxLevelForPrecision(com.spatial4j.core.shape.Shape, double)}. */
		public void SetDistErrPct(double distErrPct)
		{
			this.distErrPct = distErrPct;
		}

		public override AbstractField[] CreateIndexableFields(Shape shape)
		{
			int detailLevel = grid.GetMaxLevelForPrecision(shape, distErrPct);
			var cells = grid.GetNodes(shape, detailLevel, true);//true=intermediates cells
			//If shape isn't a point, add a full-resolution center-point so that
			// PrefixFieldCacheProvider has the center-points.
			// TODO index each center of a multi-point? Yes/no?
			if (!(shape is Point))
			{
				Point ctr = shape.GetCenter();
				//TODO should be smarter; don't index 2 tokens for this in CellTokenizer. Harmless though.
				cells.Add(grid.GetNodes(ctr, grid.GetMaxLevels(), false)[0]);
			}

			//TODO is CellTokenStream supposed to be re-used somehow? see Uwe's comments:
			//  http://code.google.com/p/lucene-spatial-playground/issues/detail?id=4

			return new AbstractField[] {new Field(GetFieldName(), new CellTokenStream(cells.GetEnumerator())) {OmitNorms = true}};
		}

		/// <summary>
		/// Outputs the tokenString of a cell, and if its a leaf, outputs it again with the leaf byte.
		/// </summary>
		protected class CellTokenStream : TokenStream
		{
			private ITermAttribute termAtt;
			private readonly IEnumerator<Node> iter;

			public CellTokenStream(IEnumerator<Node> tokens)
			{
				this.iter = tokens;
				Init();
			}

			private void Init()
			{
				termAtt = AddAttribute<ITermAttribute>();
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

		public override ValueSource MakeValueSource(SpatialArgs args)
		{
			var calc = grid.GetSpatialContext().GetDistCalc();
			return MakeValueSource(args, calc);
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

		public ValueSource MakeValueSource(SpatialArgs args, DistanceCalculator calc)
		{
			PointPrefixTreeFieldCacheProvider p = (PointPrefixTreeFieldCacheProvider)GetCacheProvider();
			Point point = args.GetShape().GetCenter();
			return new ShapeFieldCacheDistanceValueSource(point, calc, p);
		}

		public SpatialPrefixTree GetGrid()
		{
			return grid;
		}
	}
}
