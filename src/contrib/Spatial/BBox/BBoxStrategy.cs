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
using Lucene.Net.Documents;
using Lucene.Net.Search;
using Lucene.Net.Search.Function;
using Lucene.Net.Spatial.Queries;
using Spatial4n.Core.Context;
using Spatial4n.Core.Shapes;

namespace Lucene.Net.Spatial.BBox
{
	public class BBoxStrategy : SpatialStrategy<SimpleSpatialFieldInfo>
	{
		public static String SUFFIX_MINX = "__minX";
		public static String SUFFIX_MAXX = "__maxX";
		public static String SUFFIX_MINY = "__minY";
		public static String SUFFIX_MAXY = "__maxY";
		public static String SUFFIX_XDL = "__xdl";

		/*
		 * The Bounding Box gets stored as four fields for x/y min/max and a flag
		 * that says if the box crosses the dateline (xdl).
		 */
		public readonly String field_bbox;
		public readonly String field_minX;
		public readonly String field_minY;
		public readonly String field_maxX;
		public readonly String field_maxY;
		public readonly String field_xdl; // crosses dateline

		public readonly double queryPower = 1.0;
		public readonly double targetPower = 1.0f;
		public int precisionStep = 8; // same as solr default

		public BBoxStrategy(SpatialContext ctx, String fieldNamePrefix)
			: base(ctx/*, fieldNamePrefix*/)
		{
			field_bbox = fieldNamePrefix;
			field_minX = fieldNamePrefix + SUFFIX_MINX;
			field_maxX = fieldNamePrefix + SUFFIX_MAXX;
			field_minY = fieldNamePrefix + SUFFIX_MINY;
			field_maxY = fieldNamePrefix + SUFFIX_MAXY;
			field_xdl = fieldNamePrefix + SUFFIX_XDL;
		}

		public void SetPrecisionStep(int p)
		{
			precisionStep = p;
			if (precisionStep <= 0 || precisionStep >= 64)
				precisionStep = int.MaxValue;
		}

		//---------------------------------
		// Indexing
		//---------------------------------

		public override Field CreateField(SimpleSpatialFieldInfo fieldInfo, Shape shape, bool index, bool store)
		{
			throw new NotImplementedException();
		}

		public override ValueSource MakeValueSource(SpatialArgs args, SimpleSpatialFieldInfo fieldInfo)
		{
			throw new NotImplementedException();
		}

		public override Query MakeQuery(SpatialArgs args, SimpleSpatialFieldInfo fieldInfo)
		{
			throw new NotImplementedException();
		}

		public override Filter MakeFilter(SpatialArgs args, SimpleSpatialFieldInfo fieldInfo)
		{
			throw new NotImplementedException();
		}
	}
}
