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
using System.Collections.Generic;
using Lucene.Net.Documents;
using Lucene.Net.Search;
using Lucene.Net.Spatial;
using Lucene.Net.Spatial.Prefix;
using Lucene.Net.Spatial.Prefix.Tree;
using Lucene.Net.Spatial.Queries;
using NUnit.Framework;
using Spatial4n.Core.Context;
using Spatial4n.Core.Shapes;

namespace Lucene.Net.Contrib.Spatial.Test
{
   /**
	* Based off of Solr 3's SpatialFilterTest.
	*/
	public class PortedSolr3Test : StrategyTestCase
	{
		public class TestValuesProvider
		{
			public List<Param> dataList = new List<Param>();

			public IEnumerable<Param> ParamsProvider()
			{
				var ctorArgs = new List<Param>();

				SpatialContext ctx = SpatialContext.GEO_KM;

				SpatialPrefixTree grid = new GeohashPrefixTree(ctx, 12);
				SpatialStrategy strategy = new RecursivePrefixTreeStrategy(grid, "recursive_geohash");
				ctorArgs.Add(new Param(strategy, "recursive_geohash"));

				grid = new QuadPrefixTree(ctx, 25);
				strategy = new RecursivePrefixTreeStrategy(grid, "recursive_quad");
				ctorArgs.Add(new Param(strategy, "recursive_quad"));

				grid = new GeohashPrefixTree(ctx, 12);
				strategy = new TermQueryPrefixTreeStrategy(grid, "termquery_geohash");
				ctorArgs.Add(new Param(strategy, "termquery_geohash"));

				return ctorArgs;
			}
		}

		public class Param
		{
			public readonly SpatialStrategy strategy;
			public readonly String description;

			public Param(SpatialStrategy strategy, String description)
			{
				this.strategy = strategy;
				this.description = description;
			}

			public override String ToString()
			{
				return description;
			}
		}

		Random random;

		private void setupDocs()
		{
			random = NewRandom();
			deleteAll();
			adoc("1", "32.7693246, -79.9289094");
			adoc("2", "33.7693246, -80.9289094");
			adoc("3", "-32.7693246, 50.9289094");
			adoc("4", "-50.7693246, 60.9289094");
			adoc("5", "0,0");
			adoc("6", "0.1,0.1");
			adoc("7", "-0.1,-0.1");
			adoc("8", "0,179.9");
			adoc("9", "0,-179.9");
			adoc("10", "89.9,50");
			adoc("11", "89.9,-130");
			adoc("12", "-89.9,50");
			adoc("13", "-89.9,-130");
			commit();
		}

		[Test, Sequential]
		public void testIntersections([ValueSourceAttribute(typeof(TestValuesProvider), "ParamsProvider")] Param p)
		{
			this.ctx = p.strategy.GetSpatialContext();
			this.strategy = p.strategy;

			setupDocs();
			//Try some edge cases
			checkHitsCircle("1,1", 175, 3, 5, 6, 7);
			checkHitsCircle("0,179.8", 200, 2, 8, 9);
			checkHitsCircle("89.8, 50", 200, 2, 10, 11); //this goes over the north pole
			checkHitsCircle("-89.8, 50", 200, 2, 12, 13); //this goes over the south pole
			//try some normal cases
			checkHitsCircle("33.0,-80.0", 300, 2);
			//large distance
			checkHitsCircle("1,1", 5000, 3, 5, 6, 7);
			//Because we are generating a box based on the west/east longitudes and the south/north latitudes, which then
			//translates to a range query, which is slightly more inclusive.  Thus, even though 0.0 is 15.725 kms away,
			//it will be included, b/c of the box calculation.
			checkHitsBBox("0.1,0.1", 15, 2, 5, 6);
			//try some more
			deleteAll();
			adoc("14", "0,5");
			adoc("15", "0,15");
			//3000KM from 0,0, see http://www.movable-type.co.uk/scripts/latlong.html
			adoc("16", "18.71111,19.79750");
			adoc("17", "44.043900,-95.436643");
			commit();

			checkHitsCircle("0,0", 1000, 1, 14);
			checkHitsCircle("0,0", 2000, 2, 14, 15);
			checkHitsBBox("0,0", 3000, 3, 14, 15, 16);
			checkHitsCircle("0,0", 3001, 3, 14, 15, 16);
			checkHitsCircle("0,0", 3000.1, 3, 14, 15, 16);

			//really fine grained distance and reflects some of the vagaries of how we are calculating the box
			checkHitsCircle("43.517030,-96.789603", 109, 0);

			// falls outside of the real distance, but inside the bounding box
			checkHitsCircle("43.517030,-96.789603", 110, 0);
			checkHitsBBox("43.517030,-96.789603", 110, 1, 17);
		}

		/**
		 * This test is similar to a Solr 3 spatial test.
		 */
		[Test, Sequential]
		public void testDistanceOrder([ValueSourceAttribute(typeof(TestValuesProvider), "ParamsProvider")] Param p)
		{
			this.ctx = p.strategy.GetSpatialContext();
			this.strategy = p.strategy;

			adoc("100", "1,2");
			adoc("101", "4,-1");
			commit();

			//query closer to #100
			checkHitsOrdered("Intersects(Circle(3,4 d=1000))", "101", "100");
			//query closer to #101
			checkHitsOrdered("Intersects(Circle(4,0 d=1000))", "100", "101");
		}

		private void checkHitsOrdered(String spatialQ, params String[] ids)
		{
			SpatialArgs args = this.argsParser.Parse(spatialQ, ctx);
			Query query = strategy.MakeQuery(args);
			SearchResults results = executeQuery(query, 100);
			var resultIds = new String[results.numFound];
			int i = 0;
			foreach (SearchResult result in results.results)
			{
				resultIds[i++] = result.document.Get("id");
			}

			Assert.AreEqual(ids, resultIds, "order matters");
		}

		//---- these are similar to Solr test methods

		private void adoc(String idStr, String shapeStr)
		{
			Shape shape = ctx.ReadShape(shapeStr);
			addDocument(newDoc(idStr, shape));
		}

		//@SuppressWarnings("unchecked")
		private Document newDoc(String id, Shape shape)
		{
			Document doc = new Document();
			doc.Add(new Field("id", id, Field.Store.YES, Field.Index.ANALYZED));
			foreach (var f in strategy.CreateIndexableFields(shape))
			{
				doc.Add(f);
			}
			if (storeShape)
				doc.Add(new Field(strategy.GetFieldName(), ctx.ToString(shape), Field.Store.YES, Field.Index.NO));
			return doc;
		}

		private void checkHitsCircle(String ptStr, double dist, int assertNumFound, params int[] assertIds)
		{
			_checkHits(SpatialOperation.Intersects, ptStr, dist, assertNumFound, assertIds);
		}

		private void checkHitsBBox(String ptStr, double dist, int assertNumFound, params int[] assertIds)
		{
			_checkHits(SpatialOperation.BBoxIntersects, ptStr, dist, assertNumFound, assertIds);
		}

		//@SuppressWarnings("unchecked")
		private void _checkHits(SpatialOperation op, String ptStr, double dist, int assertNumFound, params int[] assertIds)
		{
			Point pt = (Point) ctx.ReadShape(ptStr);
			Shape shape = ctx.MakeCircle(pt, dist);

			SpatialArgs args = new SpatialArgs(op, shape);
			//args.setDistPrecision(0.025);
			Query query;
			if (random.NextDouble() > 0.5)
			{
				query = strategy.MakeQuery(args);
			}
			else
			{
				query = new FilteredQuery(new MatchAllDocsQuery(), strategy.MakeFilter(args));
			}
			SearchResults results = executeQuery(query, 100);
			Assert.AreEqual(assertNumFound, results.numFound, "" + shape);
			if (assertIds != null)
			{
				var resultIds = new HashSet<int>();
				foreach (SearchResult result in results.results)
				{
					resultIds.Add(int.Parse(result.document.Get("id")));
				}
				foreach (int assertId in assertIds)
				{
					Assert.True(resultIds.Contains(assertId), "has " + assertId);
				}
			}
		}
	}
}