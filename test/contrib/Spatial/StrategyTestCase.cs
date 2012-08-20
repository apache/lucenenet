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
using System.IO;
using Lucene.Net.Documents;
using Lucene.Net.Spatial;
using Lucene.Net.Spatial.Queries;
using Lucene.Net.Util;
using NUnit.Framework;
using Spatial4n.Core.Context;
using Spatial4n.Core.Io.Samples;
using Spatial4n.Core.Shapes;

namespace Lucene.Net.Contrib.Spatial.Test
{
	public abstract class StrategyTestCase : SpatialTestCase
	{
		public static readonly String DATA_STATES_POLY = "states-poly.txt";
		public static readonly String DATA_STATES_BBOX = "states-bbox.txt";
		public static readonly String DATA_COUNTRIES_POLY = "countries-poly.txt";
		public static readonly String DATA_COUNTRIES_BBOX = "countries-bbox.txt";
		public static readonly String DATA_WORLD_CITIES_POINTS = "world-cities-points.txt";

		public static readonly String QTEST_States_IsWithin_BBox = "states-IsWithin-BBox.txt";
		public static readonly String QTEST_States_Intersects_BBox = "states-Intersects-BBox.txt";

		public static readonly String QTEST_Cities_IsWithin_BBox = "cities-IsWithin-BBox.txt";

		//private Logger log = Logger.getLogger(getClass().getName());

		protected readonly SpatialArgsParser argsParser = new SpatialArgsParser();

		protected SpatialStrategy strategy;
		protected SpatialContext ctx;
		protected bool storeShape = true;

		protected void executeQueries(SpatialMatchConcern concern, params String[] testQueryFile)
		{
			Console.WriteLine("testing queried for strategy " + strategy);
			foreach (String path in testQueryFile)
			{
				IEnumerator<SpatialTestQuery> testQueryIterator = getTestQueries(path, ctx);
				runTestQueries(testQueryIterator, concern);
			}
		}

		protected void getAddAndVerifyIndexedDocuments(String testDataFile)
		{
			List<Document> testDocuments = getDocuments(testDataFile);
			addDocumentsAndCommit(testDocuments);
			verifyDocumentsIndexed(testDocuments.Count);
		}

		protected List<Document> getDocuments(String testDataFile)
		{
			IEnumerator<SampleData> sampleData = getSampleData(testDataFile);
			var documents = new List<Document>();
			while (sampleData.MoveNext())
			{
				SampleData data = sampleData.Current;
				var document = new Document();
				document.Add(new Field("id", data.id, Field.Store.YES, Field.Index.ANALYZED));
				document.Add(new Field("name", data.name, Field.Store.YES, Field.Index.ANALYZED));
				Shape shape = ctx.ReadShape(data.shape);
				foreach (var f in strategy.CreateFields(shape))
				{
					if (f != null)
					{ // null if incompatibleGeometry && ignore
						document.Add(f);
					}
				}
				if (storeShape)
					document.Add(strategy.CreateStoredField(shape));

				documents.Add(document);
			}
			return documents;
		}

		protected IEnumerator<SampleData> getSampleData(String testDataFile)
		{
            var stream = File.OpenRead(Path.Combine(Paths.ProjectRootDirectory, Path.Combine(@"test-files\spatial\data", testDataFile)));
			return new SampleDataReader(stream);
		}

		protected IEnumerator<SpatialTestQuery> getTestQueries(String testQueryFile, SpatialContext ctx)
		{
			var @in = File.OpenRead(Path.Combine(Paths.ProjectRootDirectory, Path.Combine(@"test-files\spatial", testQueryFile)));
			return SpatialTestQuery.getTestQueries(argsParser, ctx, testQueryFile, @in);
		}

		public void runTestQueries(
			IEnumerator<SpatialTestQuery> queries,
			SpatialMatchConcern concern)
		{
			while (queries.MoveNext())
			{
				SpatialTestQuery q = queries.Current;

				String msg = q.line; //"Query: " + q.args.toString(ctx);
				SearchResults got = executeQuery(strategy.MakeQuery(q.args), 100);
				if (storeShape && got.numFound > 0)
				{
					//check stored value is there & parses
					Assert.NotNull(ctx.ReadShape(got.results.Get(0).document.get(strategy.GetFieldName())));
				}
				if (concern.orderIsImportant)
				{
					var ids = q.ids.GetEnumerator();
					foreach (var r in got.results)
					{
						String id = r.document.Get("id");
						ids.MoveNext();
						Assert.AreEqual("out of order: " + msg, ids.Current, id);
					}
					if (ids.MoveNext())
					{
						Assert.Fail(msg + " :: expect more results then we got: " + ids.Current);
					}
				}
				else
				{
					// We are looking at how the results overlap
					if (concern.resultsAreSuperset)
					{
						var found = new HashSet<String>();
						foreach (var r in got.results)
						{
							found.Add(r.document.Get("id"));
						}
						foreach (String s in q.ids)
						{
							if (!found.Contains(s))
							{
								Assert.Fail("Results are mising id: " + s + " :: " + found);
							}
						}
					}
					else
					{
						var found = new List<String>();
						foreach (SearchResult r in got.results)
						{
							found.Add(r.document.Get("id"));
						}

						// sort both so that the order is not important
						q.ids.Sort();
						found.Sort();
						Assert.AreEqual(q.ids.Count, found.Count);
						for (var i = 0; i < found.Count; i++)
						{
							Assert.AreEqual(q.ids[i], found[i], msg);
						}
					}
				}
			}
		}

	}
}
