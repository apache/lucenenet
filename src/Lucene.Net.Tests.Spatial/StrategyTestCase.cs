using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Queries.Function;
using Lucene.Net.Search;
using Lucene.Net.Spatial.Queries;
using Lucene.Net.Util;
using Spatial4n.Context;
using Spatial4n.Shapes;
using System;
using System.Collections.Generic;
using System.IO;
using JCG = J2N.Collections.Generic;

namespace Lucene.Net.Spatial
{
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

    public abstract class StrategyTestCase : SpatialTestCase
    {
        public const string RESOURCE_PATH = "Test_Files.";
        public const string DATA_RESOURCE_PATH = RESOURCE_PATH + "Data.";

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
        protected bool storeShape = true;

        protected virtual void executeQueries(SpatialMatchConcern concern, params string[] testQueryFile)
        {
            //log.info("testing queried for strategy "+strategy);
            foreach (String path in testQueryFile)
            {
                IEnumerator<SpatialTestQuery> testQueryIterator = getTestQueries(path, ctx);
                runTestQueries(testQueryIterator, concern);
            }
        }

        protected virtual void getAddAndVerifyIndexedDocuments(String testDataFile)
        {
            IList<Document> testDocuments = getDocuments(testDataFile);
            addDocumentsAndCommit(testDocuments);
            VerifyDocumentsIndexed(testDocuments.size());
        }

        protected virtual IList<Document> getDocuments(String testDataFile)
        {
            return getDocuments(getSampleData(testDataFile));
        }

        protected virtual IList<Document> getDocuments(IEnumerator<SpatialTestData> sampleData)
        {
            IList<Document> documents = new JCG.List<Document>();
            while (sampleData.MoveNext())
            {
                SpatialTestData data = sampleData.Current;
                Document document = new Document();
                document.Add(new StringField("id", data.id, Field.Store.YES));
                document.Add(new StringField("name", data.name, Field.Store.YES));
                IShape shape = data.shape;
                shape = convertShapeFromGetDocuments(shape);
                if (shape != null)
                {
                    foreach (Field f in strategy.CreateIndexableFields(shape))
                    {
                        document.Add(f);
                    }
                    if (storeShape)//just for diagnostics
                        document.Add(new StoredField(strategy.FieldName, shape.toString()));
                }

                documents.Add(document);
            }
            return documents;
        }

        /** Subclasses may override to transform or remove a shape for indexing */
        protected virtual IShape convertShapeFromGetDocuments(IShape shape)
        {
            return shape;
        }

        protected virtual IEnumerator<SpatialTestData> getSampleData(String testDataFile)
        {
            String path = DATA_RESOURCE_PATH + testDataFile;
            Stream stream = GetType().getResourceAsStream(path);
            if (stream is null)
                throw new FileNotFoundException("classpath resource not found: " + path);
            return SpatialTestData.GetTestData(stream, ctx);//closes the InputStream
        }

        protected virtual IEnumerator<SpatialTestQuery> getTestQueries(String testQueryFile, SpatialContext ctx)
        {
            Stream @in = GetType().getResourceAsStream(RESOURCE_PATH + testQueryFile);
            return SpatialTestQuery.GetTestQueries(
                argsParser, ctx, testQueryFile, @in);//closes the InputStream
        }

        public virtual void runTestQueries(
            IEnumerator<SpatialTestQuery> queries,
            SpatialMatchConcern concern)
        {
            while (queries.MoveNext())
            {
                SpatialTestQuery q = queries.Current;
                runTestQuery(concern, q);
            }
        }

        public virtual void runTestQuery(SpatialMatchConcern concern, SpatialTestQuery q)
        {
            String msg = q.toString(); //"Query: " + q.args.toString(ctx);
            SearchResults got = executeQuery(makeQuery(q), Math.Max(100, q.ids.size() + 1));
            if (storeShape && got.numFound > 0)
            {
                //check stored value is there
                assertNotNull(got.results[0].document.Get(strategy.FieldName));
            }
            if (concern.orderIsImportant)
            {
                IEnumerator<String> ids = q.ids.GetEnumerator();
                foreach (SearchResult r in got.results)
                {
                    String id = r.document.Get("id");
                    if (!ids.MoveNext())
                    {
                        fail(msg + " :: Did not get enough results.  Expect" + q.ids + ", got: " + got.toDebugString());
                    }
                    assertEquals("out of order: " + msg, ids.Current, id);
                }

                if (ids.MoveNext())
                {
                    fail(msg + " :: expect more results then we got: " + ids.Current);
                }
            }
            else
            {
                // We are looking at how the results overlap
                if (concern.resultsAreSuperset)
                {
                    ISet<string> found = new JCG.HashSet<string>();
                    foreach (SearchResult r in got.results)
                    {
                        found.add(r.document.Get("id"));
                    }
                    foreach (String s in q.ids)
                    {
                        if (!found.contains(s))
                        {
                            fail("Results are mising id: " + s + " :: " + found);
                        }
                    }
                }
                else
                {
                    IList<string> found = new JCG.List<string>();
                    foreach (SearchResult r in got.results)
                    {
                        found.Add(r.document.Get("id"));
                    }

                    // sort both so that the order is not important
                    CollectionUtil.TimSort(q.ids);
                    CollectionUtil.TimSort(found);
                    assertEquals(msg, q.ids.ToString(), found.ToString());
                }
            }
        }

        protected virtual Query makeQuery(SpatialTestQuery q)
        {
            return strategy.MakeQuery(q.args);
        }

        protected virtual void adoc(String id, String shapeStr)
        {
            IShape shape = shapeStr is null ? null : ctx.ReadShapeFromWkt(shapeStr);
            AddDocument(newDoc(id, shape));
        }
        protected virtual void adoc(String id, IShape shape)
        {
            AddDocument(newDoc(id, shape));
        }

        protected virtual Document newDoc(String id, IShape shape)
        {
            Document doc = new Document();
            doc.Add(new StringField("id", id, Field.Store.YES));
            if (shape != null)
            {
                foreach (Field f in strategy.CreateIndexableFields(shape))
                {
                    doc.Add(f);
                }
                if (storeShape)
                    doc.Add(new StoredField(strategy.FieldName, shape.toString()));//not to be parsed; just for debug
            }
            return doc;
        }

        protected virtual void DeleteDoc(String id)
        {
            indexWriter.DeleteDocuments(new TermQuery(new Term("id", id)));
        }

        /** scores[] are in docId order */
        protected virtual void CheckValueSource(ValueSource vs, float[] scores, float delta)
        {
            FunctionQuery q = new FunctionQuery(vs);

            //    //TODO is there any point to this check?
            //    int expectedDocs[] = new int[scores.length];//fill with ascending 0....length-1
            //    for (int i = 0; i < expectedDocs.length; i++) {
            //      expectedDocs[i] = i;
            //    }
            //    CheckHits.checkHits(Random, q, "", indexSearcher, expectedDocs);

            TopDocs docs = indexSearcher.Search(q, 1000);//calculates the score
            for (int i = 0; i < docs.ScoreDocs.Length; i++)
            {
                ScoreDoc gotSD = docs.ScoreDocs[i];
                float expectedScore = scores[gotSD.Doc];
                assertEquals("Not equal for doc " + gotSD.Doc, expectedScore, gotSD.Score, delta);
            }

            CheckHits.CheckExplanations(q, "", indexSearcher);
        }

        protected virtual void AssertOperation(IDictionary<String, IShape> indexedDocs,
                                       SpatialOperation operation, IShape queryShape)
        {
            //Generate truth via brute force
            ISet<string> expectedIds = new JCG.HashSet<string>();
            foreach (var stringShapeEntry in indexedDocs)
            {
                if (operation.Evaluate(stringShapeEntry.Value, queryShape))
                    expectedIds.add(stringShapeEntry.Key);
            }

            SpatialTestQuery testQuery = new SpatialTestQuery();
            testQuery.args = new SpatialArgs(operation, queryShape);
            testQuery.ids = new JCG.List<string>(expectedIds);
            runTestQuery(SpatialMatchConcern.FILTER, testQuery);
        }
    }
}
