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

using Lucene.Net.Search;
using Lucene.Net.Search.Function;
using Lucene.Net.Spatial.Prefix.Tree;
using Lucene.Net.Spatial.Queries;
using Lucene.Net.Spatial.Util;
using Spatial4n.Core.Exceptions;
using Spatial4n.Core.Shapes;

namespace Lucene.Net.Spatial.Prefix
{
	/// <summary>
	/// Based on {@link RecursivePrefixTreeFilter}.
	/// </summary>
	public class RecursivePrefixTreeStrategy : PrefixTreeStrategy
	{
		private int prefixGridScanLevel;//TODO how is this customized?

		public RecursivePrefixTreeStrategy(SpatialPrefixTree grid)
			: base(grid)
		{
			prefixGridScanLevel = grid.GetMaxLevels() - 4;//TODO this default constant is dependent on the prefix grid size
		}

		public void SetPrefixGridScanLevel(int prefixGridScanLevel)
		{
			this.prefixGridScanLevel = prefixGridScanLevel;
		}

		public override Filter MakeFilter(SpatialArgs args, SimpleSpatialFieldInfo fieldInfo)
		{
			var op = args.Operation;
			if (!SpatialOperation.Is(op, SpatialOperation.IsWithin, SpatialOperation.Intersects, SpatialOperation.BBoxWithin, SpatialOperation.BBoxIntersects))
				throw new UnsupportedSpatialOperation(op);

			Shape shape = args.GetShape();

			int detailLevel = grid.GetMaxLevelForPrecision(shape, args.GetDistPrecision());

			return new RecursivePrefixTreeFilter(fieldInfo.GetFieldName(), grid, shape, prefixGridScanLevel, detailLevel);
		}

		public override string ToString()
		{
			return GetType().Name + "(prefixGridScanLevel:" + prefixGridScanLevel + ",SPG:(" + grid + "))";
		}
	}
}
