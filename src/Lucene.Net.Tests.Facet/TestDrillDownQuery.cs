using System;
using System.Diagnostics;
using Lucene.Net.Randomized.Generators;
using NUnit.Framework;

namespace Lucene.Net.Facet
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


    using MockAnalyzer = Lucene.Net.Analysis.MockAnalyzer;
    using MockTokenizer = Lucene.Net.Analysis.MockTokenizer;
    using Document = Lucene.Net.Documents.Document;
    using Field = Lucene.Net.Documents.Field;
    using TextField = Lucene.Net.Documents.TextField;
    using ITaxonomyWriter = Lucene.Net.Facet.Taxonomy.ITaxonomyWriter;
    using DirectoryTaxonomyReader = Lucene.Net.Facet.Taxonomy.Directory.DirectoryTaxonomyReader;
    using DirectoryTaxonomyWriter = Lucene.Net.Facet.Taxonomy.Directory.DirectoryTaxonomyWriter;
    using IndexReader = Lucene.Net.Index.IndexReader;
    using RandomIndexWriter = Lucene.Net.Index.RandomIndexWriter;
    using Term = Lucene.Net.Index.Term;
    using IndexSearcher = Lucene.Net.Search.IndexSearcher;
    using MatchAllDocsQuery = Lucene.Net.Search.MatchAllDocsQuery;
    using Query = Lucene.Net.Search.Query;
    using QueryUtils = Lucene.Net.Search.QueryUtils;
    using ScoreDoc = Lucene.Net.Search.ScoreDoc;
    using TermQuery = Lucene.Net.Search.TermQuery;
    using TopDocs = Lucene.Net.Search.TopDocs;
    using Directory = Lucene.Net.Store.Directory;
    using IOUtils = Lucene.Net.Util.IOUtils;

    [TestFixture]
    public class TestDrillDownQuery : FacetTestCase
    {

        private static IndexReader reader;
        private static DirectoryTaxonomyReader taxo;
        private static Directory dir;
        private static Directory taxoDir;
        private static FacetsConfig config;

      
        [OneTimeTearDown]
        public static void AfterClassDrillDownQueryTest()
        {
            IOUtils.Close(reader, taxo, dir, taxoDir);
            reader = null;
            taxo = null;
            dir = null;
            taxoDir = null;
            config = null;
        }

        [OneTimeSetUp]
        public void BeforeClassDrillDownQueryTest()
        {
            dir = NewDirectory();
            Random r = Random();
            RandomIndexWriter writer = new RandomIndexWriter(r, dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(r, MockTokenizer.KEYWORD, false)));

            taxoDir = NewDirectory();
            ITaxonomyWriter taxoWriter = new DirectoryTaxonomyWriter(taxoDir);
            config = new FacetsConfig();

            // Randomize the per-dim config:
            config.SetHierarchical("a", Random().NextBoolean());
            config.SetMultiValued("a", Random().NextBoolean());
            if (Random().NextBoolean())
            {
                config.SetIndexFieldName("a", "$a");
            }
            config.SetRequireDimCount("a", true);

            config.SetHierarchical("b", Random().NextBoolean());
            config.SetMultiValued("b", Random().NextBoolean());
            if (Random().NextBoolean())
            {
                config.SetIndexFieldName("b", "$b");
            }
            config.SetRequireDimCount("b", true);

            for (int i = 0; i < 100; i++)
            {
                Document doc = new Document();
                if (i % 2 == 0) // 50
                {
                    doc.Add(new TextField("content", "foo", Field.Store.NO));
                }
                if (i % 3 == 0) // 33
                {
                    doc.Add(new TextField("content", "bar", Field.Store.NO));
                }
                if (i % 4 == 0) // 25
                {
                    if (r.NextBoolean())
                    {
                        doc.Add(new FacetField("a", "1"));
                    }
                    else
                    {
                        doc.Add(new FacetField("a", "2"));
                    }
                }
                if (i % 5 == 0) // 20
                {
                    doc.Add(new FacetField("b", "1"));
                }
                writer.AddDocument(config.Build(taxoWriter, doc));
            }

            taxoWriter.Dispose();
            reader = writer.Reader;
            writer.Dispose();

            taxo = new DirectoryTaxonomyReader(taxoDir);
        }

        [Test]
        public virtual void TestAndOrs()
        {
            IndexSearcher searcher = NewSearcher(reader);

            // test (a/1 OR a/2) AND b/1
            DrillDownQuery q = new DrillDownQuery(config);
            q.Add("a", "1");
            q.Add("a", "2");
            q.Add("b", "1");
            TopDocs docs = searcher.Search(q, 100);
            Assert.AreEqual(5, docs.TotalHits);
        }

        [Test]
        public virtual void TestQuery()
        {
            IndexSearcher searcher = NewSearcher(reader);

            // Making sure the query yields 25 documents with the facet "a"
            DrillDownQuery q = new DrillDownQuery(config);
            q.Add("a");
            QueryUtils.Check(q);
            TopDocs docs = searcher.Search(q, 100);
            Assert.AreEqual(25, docs.TotalHits);

            // Making sure the query yields 5 documents with the facet "b" and the
            // previous (facet "a") query as a base query
            DrillDownQuery q2 = new DrillDownQuery(config, q);
            q2.Add("b");
            docs = searcher.Search(q2, 100);
            Assert.AreEqual(5, docs.TotalHits);

            // Making sure that a query of both facet "a" and facet "b" yields 5 results
            DrillDownQuery q3 = new DrillDownQuery(config);
            q3.Add("a");
            q3.Add("b");
            docs = searcher.Search(q3, 100);

            Assert.AreEqual(5, docs.TotalHits);
            // Check that content:foo (which yields 50% results) and facet/b (which yields 20%)
            // would gather together 10 results (10%..) 
            Query fooQuery = new TermQuery(new Term("content", "foo"));
            DrillDownQuery q4 = new DrillDownQuery(config, fooQuery);
            q4.Add("b");
            docs = searcher.Search(q4, 100);
            Assert.AreEqual(10, docs.TotalHits);
        }

        [Test]
        public virtual void TestQueryImplicitDefaultParams()
        {
            IndexSearcher searcher = NewSearcher(reader);

            // Create the base query to start with
            DrillDownQuery q = new DrillDownQuery(config);
            q.Add("a");

            // Making sure the query yields 5 documents with the facet "b" and the
            // previous (facet "a") query as a base query
            DrillDownQuery q2 = new DrillDownQuery(config, q);
            q2.Add("b");
            TopDocs docs = searcher.Search(q2, 100);
            Assert.AreEqual(5, docs.TotalHits);

            // Check that content:foo (which yields 50% results) and facet/b (which yields 20%)
            // would gather together 10 results (10%..) 
            Query fooQuery = new TermQuery(new Term("content", "foo"));
            DrillDownQuery q4 = new DrillDownQuery(config, fooQuery);
            q4.Add("b");
            docs = searcher.Search(q4, 100);
            Assert.AreEqual(10, docs.TotalHits);
        }

        [Test]
        public virtual void TestScoring()
        {
            // verify that drill-down queries do not modify scores
            IndexSearcher searcher = NewSearcher(reader);

            float[] scores = new float[reader.MaxDoc];

            Query q = new TermQuery(new Term("content", "foo"));
            TopDocs docs = searcher.Search(q, reader.MaxDoc); // fetch all available docs to this query
            foreach (ScoreDoc sd in docs.ScoreDocs)
            {
                scores[sd.Doc] = sd.Score;
            }

            // create a drill-down query with category "a", scores should not change
            DrillDownQuery q2 = new DrillDownQuery(config, q);
            q2.Add("a");
            docs = searcher.Search(q2, reader.MaxDoc); // fetch all available docs to this query
            foreach (ScoreDoc sd in docs.ScoreDocs)
            {
                Assert.AreEqual(scores[sd.Doc], sd.Score, 0f, "score of doc=" + sd.Doc + " modified");
            }
        }

        [Test]
        public virtual void TestScoringNoBaseQuery()
        {
            // verify that drill-down queries (with no base query) returns 0.0 score
            IndexSearcher searcher = NewSearcher(reader);

            DrillDownQuery q = new DrillDownQuery(config);
            q.Add("a");
            TopDocs docs = searcher.Search(q, reader.MaxDoc); // fetch all available docs to this query
            foreach (ScoreDoc sd in docs.ScoreDocs)
            {
                Assert.AreEqual(0f, sd.Score, 0f);
            }
        }

        [Test]
        public virtual void TestTermNonDefault()
        {
            string aField = config.GetDimConfig("a").IndexFieldName;
            Term termA = DrillDownQuery.Term(aField, "a");
            Assert.AreEqual(new Term(aField, "a"), termA);

            string bField = config.GetDimConfig("b").IndexFieldName;
            Term termB = DrillDownQuery.Term(bField, "b");
            Assert.AreEqual(new Term(bField, "b"), termB);
        }

        [Test]
        public virtual void TestClone()
        {
            var q = new DrillDownQuery(config, new MatchAllDocsQuery());
            q.Add("a");

            var clone = q.Clone() as DrillDownQuery;
            Assert.NotNull(clone);
            clone.Add("b");
            Assert.False(q.ToString().Equals(clone.ToString()), "query wasn't cloned: source=" + q + " clone=" + clone);
        }

        [Test]
        public virtual void TestNoDrillDown()
        {
            Query @base = new MatchAllDocsQuery();
            DrillDownQuery q = new DrillDownQuery(config, @base);
            Query rewrite = q.Rewrite(reader).Rewrite(reader);
            Assert.AreSame(@base, rewrite);
        }
    }

}