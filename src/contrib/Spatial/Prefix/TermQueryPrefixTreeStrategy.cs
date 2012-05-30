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

using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Spatial.Prefix.Tree;
using Spatial4n.Core.Exceptions;
using Spatial4n.Core.Query;

namespace Lucene.Net.Spatial.Prefix
{
	public class TermQueryPrefixTreeStrategy : PrefixTreeStrategy
	{
		public TermQueryPrefixTreeStrategy(SpatialPrefixTree grid)
			: base(grid)
		{
		}

		public override Query MakeQuery(SpatialArgs args, SimpleSpatialFieldInfo fieldInfo)
		{
			if (args.Operation != SpatialOperation.Intersects &&
				args.Operation != SpatialOperation.IsWithin &&
				args.Operation != SpatialOperation.Overlaps)
			{
				// TODO -- can translate these other query types
				throw new UnsupportedSpatialOperation(args.Operation);
			}
			var qshape = args.GetShape();
			int detailLevel = grid.GetMaxLevelForPrecision(qshape, args.GetDistPrecision());
			var cells = grid.GetNodes(qshape, detailLevel, false);

			var booleanQuery = new BooleanQuery();
			foreach (var cell in cells)
			{
				booleanQuery.Add(new TermQuery(new Term(fieldInfo.GetFieldName(), cell.GetTokenString())), Occur.SHOULD);
			}
			return booleanQuery;
		}

		public override Filter MakeFilter(SpatialArgs args, SimpleSpatialFieldInfo fieldInfo)
		{
			return new QueryWrapperFilter(MakeQuery(args, fieldInfo));
		}
	}
}
