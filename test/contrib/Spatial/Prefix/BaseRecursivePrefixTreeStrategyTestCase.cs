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

using System.Collections.Generic;
using Lucene.Net.Documents;
using Lucene.Net.Spatial;
using Lucene.Net.Spatial.Prefix;
using Lucene.Net.Spatial.Prefix.Tree;
using Spatial4n.Core.Context;
using NUnit.Framework;
using Spatial4n.Core.Io.Samples;
using Spatial4n.Core.Query;
using Spatial4n.Core.Shapes;

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

		[Test]
		public void minifiedTest()
		{
			var list = new List<SampleData> { new SampleData("G5391959	San Francisco	-122.419420 37.774930") };

			var documents = new List<Document>();
			foreach (var data in list)
			{
				var document = new Document();
				document.Add(new Field("id", data.id, Field.Store.YES, Field.Index.ANALYZED));
				document.Add(new Field("name", data.name, Field.Store.YES, Field.Index.ANALYZED));
				Shape shape = ctx.ReadShape(data.shape);
				foreach (var f in strategy.CreateFields(fieldInfo, shape, true, storeShape))
				{
					if (f != null)
					{ // null if incompatibleGeometry && ignore
						document.Add(f);
					}
				}
				documents.Add(document);
			}
			addDocumentsAndCommit(documents);
			verifyDocumentsIndexed(documents.Count);

			((RecursivePrefixTreeStrategy)strategy).SetPrefixGridScanLevel(0);

			const string line = "[San Francisco] G5391959 @ IsWithin(-122.524918 37.674973 -122.360123 37.817108)";
			var argsParser = new SpatialArgsParser();
			var queries = new List<SpatialTestQuery>
			              	{
			              		new SpatialTestQuery
			              			{
										args = new SpatialArgs(SpatialOperation.IsWithin, ctx.ReadShape("-122.524918 37.674973 -122.360123 37.817108")),
										line = line,
										lineNumber = 0,
										//testname = ,
			              				ids = new List<string> {"G5391959"},
			              			}
			              	};

			runTestQueries(queries.GetEnumerator(), SpatialMatchConcern.FILTER);
		}
	}
}
