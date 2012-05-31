/*
 *
 * Licensed to the Apache Software Foundation (ASF) under one
 * or more contributor license agreements.  See the NOTICE file
 * distributed with this work for additional information
 * regarding copyright ownership.  The ASF licenses this file
 * to you under the Apache License, Version 2.0 (the
 * "License"); you may not use this file except in compliance
 * with the License.  You may obtain a copy of the License at
 *
 *   http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing,
 * software distributed under the License is distributed on an
 * "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
 * KIND, either express or implied.  See the License for the
 * specific language governing permissions and limitations
 * under the License.
 *
*/

using System;
using System.IO;
using System.Linq;
using Lucene.Net.Analysis;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Spatial;
using Lucene.Net.Spatial.Prefix;
using Lucene.Net.Spatial.Prefix.Tree;
using Lucene.Net.Store;
using NUnit.Framework;
using Spatial4n.Core.Context;
using Spatial4n.Core.Distance;
using Spatial4n.Core.Query;
using Spatial4n.Core.Shapes;
using Directory = Lucene.Net.Store.Directory;

namespace Lucene.Net.Contrib.Spatial.Test
{
	[TestFixture]
	public class Various
	{
		private Directory _directory;
		private IndexSearcher _searcher;
		private IndexWriter _writer;
		protected SpatialStrategy<SimpleSpatialFieldInfo> strategy;
		protected SimpleSpatialFieldInfo fieldInfo;
		protected readonly SpatialContext ctx = SpatialContext.GEO_KM;
		protected readonly bool storeShape = true;
		private int maxLength;

		[SetUp]
		protected void SetUp()
		{
			maxLength = GeohashPrefixTree.GetMaxLevelsPossible();
			fieldInfo = new SimpleSpatialFieldInfo(GetType().Name);
			strategy = new RecursivePrefixTreeStrategy(new GeohashPrefixTree(ctx, maxLength));

			_directory = new RAMDirectory();
			_writer = new IndexWriter(_directory, new WhitespaceAnalyzer(), true, IndexWriter.MaxFieldLength.UNLIMITED);
		}

		private void AddPoint(IndexWriter writer, String name, double lat, double lng)
		{
			var doc = new Document();
			doc.Add(new Field("name", name, Field.Store.YES, Field.Index.ANALYZED));
			Shape shape = ctx.MakePoint(lng, lat);
			foreach (var f in strategy.CreateFields(fieldInfo, shape, true, storeShape))
			{
				if (f != null)
				{ // null if incompatibleGeometry && ignore
					doc.Add(f);
				}
			}
			writer.AddDocument(doc);
		}

		[Test]
		public void RadiusOf15Something()
		{
			// Origin
			const double _lat = 45.829507799999988;
			const double _lng = -73.800524699999983;

			//Locations
			AddPoint(_writer, "The location doc we are after", _lat, _lng);

			_writer.Commit();
			_writer.Close();

			_searcher = new IndexSearcher(_directory, true);

			ExecuteSearch(45.831909, -73.810322, ctx.GetUnits().Convert(150000, DistanceUnits.MILES), 1);
			ExecuteSearch(45.831909, -73.810322, ctx.GetUnits().Convert(15000, DistanceUnits.MILES), 1);
			ExecuteSearch(45.831909, -73.810322, ctx.GetUnits().Convert(1500, DistanceUnits.MILES), 1);

			_searcher.Close();
			_directory.Close();
		}

		private void ExecuteSearch(double lat, double lng, double radius, int expectedResults)
		{
			var dq = strategy.MakeQuery(new SpatialArgs(SpatialOperation.IsWithin, ctx.MakeCircle(lng, lat, radius)), fieldInfo);
			Console.WriteLine(dq);

			//var dsort = new DistanceFieldComparatorSource(dq.DistanceFilter);
			//Sort sort = new Sort(new SortField("foo", dsort, false));

			// Perform the search, using the term query, the distance filter, and the
			// distance sort
			TopDocs hits = _searcher.Search(dq, 10);
			int results = hits.TotalHits;
			ScoreDoc[] scoreDocs = hits.ScoreDocs;

			// Get a list of distances
			//Dictionary<int, Double> distances = dq.DistanceFilter.Distances;

			//Console.WriteLine("Distance Filter filtered: " + distances.Count);
			//Console.WriteLine("Results: " + results);

			//Assert.AreEqual(expectedResults, distances.Count); // fixed a store of only needed distances
			Assert.AreEqual(expectedResults, results);
		}

		[Test]
		public void CheckIfSortingCorrectly()
		{
			// Origin
			const double lat = 38.96939;
			const double lng = -77.386398;
			var radius = ctx.GetUnits().Convert(6.0, DistanceUnits.MILES);


			AddPoint(_writer, "c/1", 38.9579000, -77.3572000); // 1.76 Miles away
			AddPoint(_writer, "a/2", 38.9690000, -77.3862000); // 0.03 Miles away
			AddPoint(_writer, "b/3", 38.9510000, -77.4107000); // 1.82 Miles away

			_writer.Commit();
			_writer.Close();

			_searcher = new IndexSearcher(_directory, true);

			// create a distance query
			var args = new SpatialArgs(SpatialOperation.IsWithin, ctx.MakeCircle(lng, lat, radius));

			var vs = strategy.MakeValueSource(args, fieldInfo);
			var vals = vs.GetValues(_searcher.IndexReader);

			args.SetDistPrecision(0.0);
			var dq = strategy.MakeQuery(args, fieldInfo);
			Console.WriteLine(dq);

			TopDocs hits = _searcher.Search(dq, null, 1000, new Sort(new SortField("distance", SortField.SCORE, true)));
			var results = hits.TotalHits;
			Assert.AreEqual(3, results);

			var expectedOrder = new[] {"a/2", "c/1", "b/3"};
			for (int i = 0; i < hits.TotalHits; i++)
			{
				Assert.AreEqual(expectedOrder[i], _searcher.Doc(hits.ScoreDocs[i].Doc).GetField("name").StringValue);
			}
		}

		[Test]
		public void LUCENENET462()
		{
			Console.WriteLine("LUCENENET462");

			// Origin
			const double _lat = 51.508129;
			const double _lng = -0.128005;

			// Locations
			AddPoint(_writer, "Location 1", 51.5073802128877, -0.124669075012207);
			AddPoint(_writer, "Location 2", 51.5091, -0.1235);
			AddPoint(_writer, "Location 3", 51.5093, -0.1232);
			AddPoint(_writer, "Location 4", 51.5112531582845, -0.12509822845459);
			AddPoint(_writer, "Location 5", 51.5107, -0.123);
			AddPoint(_writer, "Location 6", 51.512, -0.1246);
			AddPoint(_writer, "Location 8", 51.5088760101322, -0.143165588378906);
			AddPoint(_writer, "Location 9", 51.5087958793819, -0.143508911132813);

			_writer.Commit();
			_writer.Close();

			_searcher = new IndexSearcher(_directory, true);

			// create a distance query
			var radius = ctx.GetUnits().Convert(2.0, DistanceUnits.MILES);
			var dq = strategy.MakeQuery(new SpatialArgs(SpatialOperation.IsWithin, ctx.MakeCircle(_lng, _lat, radius)), fieldInfo);
			Console.WriteLine(dq);

			//var dsort = new DistanceFieldComparatorSource(dq.DistanceFilter);
			//Sort sort = new Sort(new SortField("foo", dsort, false));

			// Perform the search, using the term query, the distance filter, and the
			// distance sort
			TopDocs hits = _searcher.Search(dq, 1000);
			var results = hits.TotalHits;
			Assert.AreEqual(8, results);

			radius = ctx.GetUnits().Convert(1.0, DistanceUnits.MILES);
			var spatialArgs = new SpatialArgs(SpatialOperation.IsWithin, ctx.MakeCircle(_lng, _lat, radius));
			dq = strategy.MakeQuery(spatialArgs, fieldInfo);
			Console.WriteLine(dq);

			//var dsort = new DistanceFieldComparatorSource(dq.DistanceFilter);
			//Sort sort = new Sort(new SortField("foo", dsort, false));

			// Perform the search, using the term query, the distance filter, and the
			// distance sort
			hits = _searcher.Search(dq, 1000);
			results = hits.TotalHits;

			Assert.AreEqual(8, results);

			_searcher.Close();
			_directory.Close();
		}

		[Test]
		public void LUCENENET483()
		{
			Console.WriteLine("LUCENENET483");

			// Origin
			const double _lat = 42.350153;
			const double _lng = -71.061667;

			//Locations            
			AddPoint(_writer, "Location 1", 42.0, -71.0); //24 miles away from origin
			AddPoint(_writer, "Location 2", 42.35, -71.06); //less than a mile

			_writer.Commit();
			_writer.Close();

			_searcher = new IndexSearcher(_directory, true);

			// create a distance query
			var radius = ctx.GetUnits().Convert(52.0, DistanceUnits.MILES);
			var dq = strategy.MakeQuery(new SpatialArgs(SpatialOperation.IsWithin, ctx.MakeCircle(_lng, _lat, radius)), fieldInfo);
			Console.WriteLine(dq);

			TopDocs hits = _searcher.Search(dq, 1000);
			var results = hits.TotalHits;
			Assert.AreEqual(2, results);

			_searcher.Close();
			_directory.Close();
		}
	}
}
