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
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Search.Function;
using Lucene.Net.Spatial;
using Lucene.Net.Spatial.Queries;
using Lucene.Net.Spatial.Util;
using Lucene.Net.Util;
using NUnit.Framework;
using Spatial4n.Core.Context;
using Spatial4n.Core.Io;
using Spatial4n.Core.Io.Samples;
using Spatial4n.Core.Shapes;

namespace Lucene.Net.Contrib.Spatial.Test
{
    public abstract class StrategyTestCase : SpatialTestCase
    {
        public static readonly String DATA_SIMPLE_BBOX = "simple-bbox.txt";
        public static readonly String DATA_STATES_POLY = "states-poly.txt";
        public static readonly String DATA_STATES_BBOX = "states-bbox.txt";
        public static readonly String DATA_COUNTRIES_POLY = "countries-poly.txt";
        public static readonly String DATA_COUNTRIES_BBOX = "countries-bbox.txt";
        public static readonly String DATA_WORLD_CITIES_POINTS = "world-cities-points.txt";

        public static readonly String QTEST_States_IsWithin_BBox = "states-IsWithin-BBox.txt";
        public static readonly String QTEST_States_Intersects_BBox = "states-Intersects-BBox.txt";
        public static readonly String QTEST_Cities_Intersects_BBox = "cities-Intersects-BBox.txt";
        public static readonly String QTEST_Simple_Queries_BBox = "simple-Queries-BBox.txt";

        //private Logger log = Logger.getLogger(getClass().getName());

        protected readonly SpatialArgsParser argsParser = new SpatialArgsParser();

        protected SpatialStrategy strategy;
        protected SpatialContext ctx;
        protected bool storeShape = true;

        [TearDown]
        public override void TearDown()
        {
            base.TearDown();
            ctx = null;
            strategy = null;
            storeShape = true;
        }

        protected void executeQueries(SpatialMatchConcern concern, params String[] testQueryFile)
        {
            Console.WriteLine("testing queries for strategy " + strategy + ". Executer: " + GetType().Name);
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
                Shape shape = new ShapeReadWriter(ctx).ReadShape(data.shape);
                shape = convertShapeFromGetDocuments(shape);
                if (shape != null)
                {
                    foreach (var f in strategy.CreateIndexableFields(shape))
                    {
                        document.Add(f);
                    }
                    if (storeShape)
                        document.Add(new Field(strategy.GetFieldName(), ctx.ToString(shape), Field.Store.YES,
                                               Field.Index.NOT_ANALYZED_NO_NORMS));
                }

                documents.Add(document);
            }
            return documents;
        }

        /* Subclasses may override to transform or remove a shape for indexing */

        protected virtual Shape convertShapeFromGetDocuments(Shape shape)
        {
            return shape;
        }

        protected IEnumerator<SampleData> getSampleData(String testDataFile)
        {
            var stream =
                File.OpenRead(Path.Combine(Paths.ProjectRootDirectory,
                                           Path.Combine(@"test-files\spatial\data", testDataFile)));
            return new SampleDataReader(stream);
        }

        protected IEnumerator<SpatialTestQuery> getTestQueries(String testQueryFile, SpatialContext ctx)
        {
            var @in =
                File.OpenRead(Path.Combine(Paths.ProjectRootDirectory,
                                           Path.Combine(@"test-files\spatial", testQueryFile)));
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
                    assertNotNull(
                        new ShapeReadWriter(ctx).ReadShape(got.results[0].document.Get(strategy.GetFieldName())));
                }
                if (concern.orderIsImportant)
                {
                    var ids = q.ids.GetEnumerator();
                    foreach (var r in got.results)
                    {
                        String id = r.document.Get("id");
                        if (!ids.MoveNext())
                            Assert.Fail(msg + " :: Did not get enough results.  Expected " + q.ids + ", got: " +
                                        got.toDebugString());
                        Assert.AreEqual(ids.Current, id, "out of order: " + msg);
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

        protected void adoc(String id, String shapeStr)
        {
            Shape shape = shapeStr == null ? null : new ShapeReadWriter(ctx).ReadShape(shapeStr);
            addDocument(newDoc(id, shape));
        }

        protected void adoc(String id, Shape shape)
        {
            addDocument(newDoc(id, shape));
        }

        protected virtual Document newDoc(String id, Shape shape)
        {
            Document doc = new Document();
            doc.Add(new Field("id", id, Field.Store.YES, Field.Index.ANALYZED));
            if (shape != null)
            {
                foreach (var f in strategy.CreateIndexableFields(shape))
                {
                    doc.Add(f);
                }
                if (storeShape)
                    doc.Add(new Field(strategy.GetFieldName(), ctx.ToString(shape), Field.Store.YES,
                                      Field.Index.NOT_ANALYZED_NO_NORMS));
            }
            return doc;
        }

        /* scores[] are in docId order */

        protected void checkValueSource(ValueSource vs, float[] scores, float delta)
        {
            FunctionQuery q = new FunctionQuery(vs);

            //    //TODO is there any point to this check?
            //    int expectedDocs[] = new int[scores.length];//fill with ascending 0....length-1
            //    for (int i = 0; i < expectedDocs.length; i++) {
            //      expectedDocs[i] = i;
            //    }
            //    CheckHits.checkHits(random(), q, "", indexSearcher, expectedDocs);

            TopDocs docs = indexSearcher.Search(q, 1000); //calculates the score
            for (int i = 0; i < docs.ScoreDocs.Length; i++)
            {
                ScoreDoc gotSD = docs.ScoreDocs[i];
                float expectedScore = scores[gotSD.Doc];
                assertEquals("Not equal for doc " + gotSD.Doc, expectedScore, gotSD.Score, delta);
            }

            // CheckHits.checkExplanations(q, "", indexSearcher);
        }
    }
}
