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
using Lucene.Net.Spatial.Util;
using Lucene.Net.Spatial.Vector;
using NUnit.Framework;
using Spatial4n.Core.Context;

namespace Lucene.Net.Contrib.Spatial.Test.Vector
{
	public abstract class BaseTwoDoublesStrategyTestCase : StrategyTestCase
	{
		protected abstract SpatialContext getSpatialContext();

		[SetUp]
		public override void SetUp()
		{
			base.SetUp();
			this.ctx = getSpatialContext();
			this.strategy = new TwoDoublesStrategy(ctx, 
				new NumericFieldInfo(), FieldCache_Fields.NUMERIC_UTILS_DOUBLE_PARSER);
		}

		[Test]
		public void testCitiesWithinBBox()
		{
			getAddAndVerifyIndexedDocuments(DATA_WORLD_CITIES_POINTS);
			executeQueries(SpatialMatchConcern.FILTER, QTEST_Cities_IsWithin_BBox);
		}
	}
}
