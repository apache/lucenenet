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
using System.Collections.Concurrent;
using System.Collections.Generic;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Tokenattributes;
using Lucene.Net.Documents;
using Lucene.Net.Search.Function;
using Lucene.Net.Spatial.Prefix.Tree;
using Lucene.Net.Spatial.Util;
using Spatial4n.Core.Distance;
using Spatial4n.Core.Query;
using Spatial4n.Core.Shapes;

namespace Lucene.Net.Spatial.Prefix
{
	public abstract class PrefixTreeStrategy : SpatialStrategy<SimpleSpatialFieldInfo>
	{
		protected readonly SpatialPrefixTree grid;
		private readonly IDictionary<String, PointPrefixTreeFieldCacheProvider> provider = new ConcurrentDictionary<string, PointPrefixTreeFieldCacheProvider>();
		protected int defaultFieldValuesArrayLen = 2;
		protected double distErrPct = SpatialArgs.DEFAULT_DIST_PRECISION;


		public PrefixTreeStrategy(SpatialPrefixTree grid)
			: base(grid.GetSpatialContext())
		{
			this.grid = grid;
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

		public override Field CreateField(SimpleSpatialFieldInfo fieldInfo, Shape shape, bool index, bool store)
		{
			int detailLevel = grid.GetMaxLevelForPrecision(shape, distErrPct);
			List<Node> cells = grid.GetNodes(shape, detailLevel, true);//true=intermediates cells
			//If shape isn't a point, add a full-resolution center-point so that
			// PrefixFieldCacheProvider has the center-points.
			// TODO index each center of a multi-point? Yes/no?
			if (!(shape is Point))
			{
				Point ctr = shape.GetCenter();
				//TODO should be smarter; don't index 2 tokens for this in CellTokenizer. Harmless though.
				cells.Add(grid.GetNodes(ctr, grid.GetMaxLevels(), false).Get(0));
			}

			String fname = fieldInfo.GetFieldName();
			if (store)
			{
				//TODO figure out how to re-use original string instead of reconstituting it.
				String wkt = grid.GetSpatialContext().toString(shape);
				if (index)
				{
					Field f = new Field(fname, wkt, TYPE_STORED);
					f.SetTokenStream(new CellTokenStream(cells.iterator()));
					return f;
				}
				return new StoredField(fname, wkt);
			}

			if (index)
			{
				return new Field(fname, new CellTokenStream(cells.iterator()), TYPE_UNSTORED);
			}

			throw new InvalidOperationException("Fields need to be indexed or store [" + fname + "]");
		}

		///* Indexed, tokenized, not stored. */
		//public static final FieldType TYPE_UNSTORED = new FieldType();

		///* Indexed, tokenized, stored. */
		//public static final FieldType TYPE_STORED = new FieldType();

		//static {
		//  TYPE_UNSTORED.setIndexed(true);
		//  TYPE_UNSTORED.setTokenized(true);
		//  TYPE_UNSTORED.setOmitNorms(true);
		//  TYPE_UNSTORED.freeze();

		//  TYPE_STORED.setStored(true);
		//  TYPE_STORED.setIndexed(true);
		//  TYPE_STORED.setTokenized(true);
		//  TYPE_STORED.setOmitNorms(true);
		//  TYPE_STORED.freeze();
		//}


		/// <summary>
		/// Outputs the tokenString of a cell, and if its a leaf, outputs it again with the leaf byte.
		/// </summary>
		protected class CellTokenStream : TokenStream
		{
			private readonly TermAttribute termAtt;

			private Iterator<Node> iter = null;

			public CellTokenStream(Iterator<Node> tokens)
			{
				this.iter = tokens;
				termAtt = (TermAttribute)AddAttribute(typeof(TermAttribute));
			}

			private CharSequence nextTokenStringNeedingLeaf = null;

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
				if (iter.HasNext())
				{
					Node cell = iter.Next();
					CharSequence token = cell.GetTokenString();
					termAtt.Append(token);
					if (cell.IsLeaf())
						nextTokenStringNeedingLeaf = token;
					return true;
				}
				return false;
			}
		}

		public override ValueSource MakeValueSource(SpatialArgs args, SimpleSpatialFieldInfo fieldInfo)
		{
			var calc = grid.GetSpatialContext().GetDistCalc();
			return MakeValueSource(args, fieldInfo, calc);
		}

		public ValueSource MakeValueSource(SpatialArgs args, SimpleSpatialFieldInfo fieldInfo, DistanceCalculator calc)
		{
			PointPrefixTreeFieldCacheProvider p = provider.get(fieldInfo.GetFieldName());
			if (p == null)
			{
				lock (this)
				{//double checked locking idiom is okay since provider is threadsafe
					p = provider.Get(fieldInfo.GetFieldName());
					if (p == null)
					{
						p = new PointPrefixTreeFieldCacheProvider(grid, fieldInfo.GetFieldName(), defaultFieldValuesArrayLen);
						provider.Put(fieldInfo.GetFieldName(), p);
					}
				}
			}
			Point point = args.GetShape().GetCenter();
			return new CachedDistanceValueSource(point, calc, p);
		}

		public SpatialPrefixTree GetGrid()
		{
			return grid;
		}
	}
}
