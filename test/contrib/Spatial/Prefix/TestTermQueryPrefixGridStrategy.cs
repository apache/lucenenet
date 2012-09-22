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
using Lucene.Net.Spatial.Prefix;
using Lucene.Net.Spatial.Prefix.Tree;
using Lucene.Net.Spatial.Queries;
using NUnit.Framework;
using Spatial4n.Core.Context;
using Spatial4n.Core.Shapes;
using Spatial4n.Core.Shapes.Impl;

namespace Lucene.Net.Contrib.Spatial.Test.Prefix
{
	public class TestTermQueryPrefixGridStrategy : SpatialTestCase
	{
		[Test]
		public void testNGramPrefixGridLosAngeles()
		{
			SpatialContext ctx = SpatialContext.GEO;
			TermQueryPrefixTreeStrategy prefixGridStrategy = new TermQueryPrefixTreeStrategy(new QuadPrefixTree(ctx), "geo");

			Shape point = ctx.MakePoint(-118.243680, 34.052230);

			Document losAngeles = new Document();
			losAngeles.Add(new Field("name", "Los Angeles", Field.Store.YES, Field.Index.NOT_ANALYZED_NO_NORMS));
			foreach (var indexableField in prefixGridStrategy.CreateIndexableFields(point))
			{
				losAngeles.Add(indexableField);
			}
			losAngeles.Add(new Field(prefixGridStrategy.GetFieldName(), ctx.ToString(point), Field.Store.YES, Field.Index.NO));

			addDocumentsAndCommit(new List<Document> { losAngeles });

			// This won't work with simple spatial context...
			SpatialArgsParser spatialArgsParser = new SpatialArgsParser();
			// TODO... use a non polygon query
			//    SpatialArgs spatialArgs = spatialArgsParser.parse(
            //        "Intersects(POLYGON((-127.00390625 39.8125,-112.765625 39.98828125,-111.53515625 31.375,-125.94921875 30.14453125,-127.00390625 39.8125)))",
			//        new SimpleSpatialContext());

			//    Query query = prefixGridStrategy.makeQuery(spatialArgs, fieldInfo);
			//    SearchResults searchResults = executeQuery(query, 1);
			//    assertEquals(1, searchResults.numFound);
		}

	}
}
