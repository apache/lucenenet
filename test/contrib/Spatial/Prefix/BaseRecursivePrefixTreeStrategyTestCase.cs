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

using Lucene.Net.Spatial;
using Lucene.Net.Spatial.Prefix;
using Lucene.Net.Spatial.Prefix.Tree;
using Spatial4n.Core.Context;
using NUnit.Framework;

namespace Lucene.Net.Contrib.Spatial.Test.Prefix
{
	public abstract class BaseRecursivePrefixTreeStrategyTestCase : StrategyTestCase<SimpleSpatialFieldInfo>
	{
		private int maxLength;

		protected abstract SpatialContext getSpatialContext();

		[SetUp]
		public override void SetUp()
		{
			base.SetUp();
			maxLength = GeohashPrefixTree.GetMaxLevelsPossible();
			// SimpleIO
			this.ctx = getSpatialContext();
			this.strategy = new RecursivePrefixTreeStrategy(new GeohashPrefixTree(
				ctx, maxLength));
			this.fieldInfo = new SimpleSpatialFieldInfo(GetType().Name);
		}

		[Test]
		public void testFilterWithVariableScanLevel()
		{
			getAddAndVerifyIndexedDocuments(DATA_WORLD_CITIES_POINTS);

			//execute queries for each prefix grid scan level
			for (int i = 0; i <= maxLength; i++)
			{
				((RecursivePrefixTreeStrategy)strategy).SetPrefixGridScanLevel(i);
				executeQueries(SpatialMatchConcern.FILTER, QTEST_Cities_IsWithin_BBox);
			}
		}
	}
}
