using Lucene.Net.Documents;
using Lucene.Net.Index.Extensions;
using NUnit.Framework;
using System;
using Assert = Lucene.Net.TestFramework.Assert;
using Console = Lucene.Net.Util.SystemConsole;

namespace Lucene.Net.Search
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

    using AtomicReaderContext = Lucene.Net.Index.AtomicReaderContext;
    using DefaultSimilarity = Lucene.Net.Search.Similarities.DefaultSimilarity;
    using Directory = Lucene.Net.Store.Directory;
    using Document = Documents.Document;
    using Field = Field;
    using FieldType = FieldType;
    using IndexReader = Lucene.Net.Index.IndexReader;
    using MockAnalyzer = Lucene.Net.Analysis.MockAnalyzer;
    using MockTokenizer = Lucene.Net.Analysis.MockTokenizer;
    using RandomIndexWriter = Lucene.Net.Index.RandomIndexWriter;
    using Term = Lucene.Net.Index.Term;
    using TextField = TextField;

    [TestFixture]
    public class TestMultiTermConstantScore : BaseTestRangeFilter
    {
        /// <summary>
        /// threshold for comparing floats </summary>
        public const float SCORE_COMP_THRESH = 1e-6f;

        // LUCENENET specific - made these instance variables
        // since our BeforeClass() and AfterClass() are instance
        // methods and not doing so makes them cross runner threads.
        internal /*static*/ Directory small;
        internal /*static*/ IndexReader reader;

        /// <summary>
        /// LUCENENET specific
        /// Is non-static because NewIndexWriterConfig is no longer static.
        /// </summary>
        [OneTimeSetUp]
        public override void BeforeClass()
        {
            base.BeforeClass();

            string[] data = new string[] { "A 1 2 3 4 5 6", "Z       4 5 6", null, "B   2   4 5 6", "Y     3   5 6", null, "C     3     6", "X       4 5 6" };

            small = NewDirectory();
            using RandomIndexWriter writer = new RandomIndexWriter(Random, small, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random, MockTokenizer.WHITESPACE, false)).SetMergePolicy(NewLogMergePolicy()));
            FieldType customType = new FieldType(TextField.TYPE_STORED);
            customType.IsTokenized = false;
            for (int i = 0; i < data.Length; i++)
            {
                Document doc = new Document();
                doc.Add(NewField("id", Convert.ToString(i), customType)); // Field.Keyword("id",String.valueOf(i)));
                doc.Add(NewField("all", "all", customType)); // Field.Keyword("all","all"));
                if (null != data[i])
                {
                    doc.Add(NewTextField("data", data[i], Field.Store.YES)); // Field.Text("data",data[i]));
                }
                writer.AddDocument(doc);
            }

            reader = writer.GetReader();
        }

        [OneTimeTearDown]
        public override void AfterClass()
        {
            reader?.Dispose();
            small?.Dispose();
            reader = null;
            small = null;
            base.AfterClass();
        }

        /// <summary>
        /// macro for readability </summary>
        public static Query Csrq(string f, string l, string h, bool il, bool ih)
        {
            TermRangeQuery query = TermRangeQuery.NewStringRange(f, l, h, il, ih);
            query.MultiTermRewriteMethod = (MultiTermQuery.CONSTANT_SCORE_FILTER_REWRITE);
            if (Verbose)
            {
                Console.WriteLine("TEST: query=" + query);
            }
            return query;
        }

        public static Query Csrq(string f, string l, string h, bool il, bool ih, MultiTermQuery.RewriteMethod method)
        {
            TermRangeQuery query = TermRangeQuery.NewStringRange(f, l, h, il, ih);
            query.MultiTermRewriteMethod = (method);
            if (Verbose)
            {
                Console.WriteLine("TEST: query=" + query + " method=" + method);
            }
            return query;
        }

        /// <summary>
        /// macro for readability </summary>
        public static Query Cspq(Term prefix)
        {
            PrefixQuery query = new PrefixQuery(prefix);
            query.MultiTermRewriteMethod = (MultiTermQuery.CONSTANT_SCORE_FILTER_REWRITE);
            return query;
        }

        /// <summary>
        /// macro for readability </summary>
        public static Query Cswcq(Term wild)
        {
            WildcardQuery query = new WildcardQuery(wild);
            query.MultiTermRewriteMethod = (MultiTermQuery.CONSTANT_SCORE_FILTER_REWRITE);
            return query;
        }

        [Test]
        public virtual void TestBasics()
        {
            QueryUtils.Check(Csrq("data", "1", "6", T, T));
            QueryUtils.Check(Csrq("data", "A", "Z", T, T));
            QueryUtils.CheckUnequal(Csrq("data", "1", "6", T, T), Csrq("data", "A", "Z", T, T));

            QueryUtils.Check(Cspq(new Term("data", "p*u?")));
            QueryUtils.CheckUnequal(Cspq(new Term("data", "pre*")), Cspq(new Term("data", "pres*")));

            QueryUtils.Check(Cswcq(new Term("data", "p")));
            QueryUtils.CheckUnequal(Cswcq(new Term("data", "pre*n?t")), Cswcq(new Term("data", "pr*t?j")));
        }

        [Test]
        public virtual void TestEqualScores()
        {
            // NOTE: uses index build in *this* setUp

            IndexSearcher search = NewSearcher(reader);

            ScoreDoc[] result;

            // some hits match more terms then others, score should be the same

            result = search.Search(Csrq("data", "1", "6", T, T), null, 1000).ScoreDocs;
            int numHits = result.Length;
            assertEquals("wrong number of results", 6, numHits);
            float score = result[0].Score;
            for (int i = 1; i < numHits; i++)
            {
                Assert.AreEqual(score, result[i].Score, SCORE_COMP_THRESH, "score for " + i + " was not the same");
            }

            result = search.Search(Csrq("data", "1", "6", T, T, MultiTermQuery.CONSTANT_SCORE_BOOLEAN_QUERY_REWRITE), null, 1000).ScoreDocs;
            numHits = result.Length;
            assertEquals("wrong number of results", 6, numHits);
            for (int i = 0; i < numHits; i++)
            {
                Assert.AreEqual(score, result[i].Score, SCORE_COMP_THRESH, "score for " + i + " was not the same");
            }

            result = search.Search(Csrq("data", "1", "6", T, T, MultiTermQuery.CONSTANT_SCORE_AUTO_REWRITE_DEFAULT), null, 1000).ScoreDocs;
            numHits = result.Length;
            assertEquals("wrong number of results", 6, numHits);
            for (int i = 0; i < numHits; i++)
            {
                Assert.AreEqual(score, result[i].Score, SCORE_COMP_THRESH, "score for " + i + " was not the same");
            }
        }

        [Test]
        public virtual void TestEqualScoresWhenNoHits() // Test for LUCENE-5245: Empty MTQ rewrites should have a consistent norm, so always need to return a CSQ!
        {
            // NOTE: uses index build in *this* setUp

            IndexSearcher search = NewSearcher(reader);

            ScoreDoc[] result;

            TermQuery dummyTerm = new TermQuery(new Term("data", "1"));

            BooleanQuery bq = new BooleanQuery();
            bq.Add(dummyTerm, Occur.SHOULD); // hits one doc
            bq.Add(Csrq("data", "#", "#", T, T), Occur.SHOULD); // hits no docs
            result = search.Search(bq, null, 1000).ScoreDocs;
            int numHits = result.Length;
            assertEquals("wrong number of results", 1, numHits);
            float score = result[0].Score;
            for (int i = 1; i < numHits; i++)
            {
                Assert.AreEqual(score, result[i].Score, SCORE_COMP_THRESH, "score for " + i + " was not the same");
            }

            bq = new BooleanQuery();
            bq.Add(dummyTerm, Occur.SHOULD); // hits one doc
            bq.Add(Csrq("data", "#", "#", T, T, MultiTermQuery.CONSTANT_SCORE_BOOLEAN_QUERY_REWRITE), Occur.SHOULD); // hits no docs
            result = search.Search(bq, null, 1000).ScoreDocs;
            numHits = result.Length;
            assertEquals("wrong number of results", 1, numHits);
            for (int i = 0; i < numHits; i++)
            {
                Assert.AreEqual(score, result[i].Score, SCORE_COMP_THRESH, "score for " + i + " was not the same");
            }

            bq = new BooleanQuery();
            bq.Add(dummyTerm, Occur.SHOULD); // hits one doc
            bq.Add(Csrq("data", "#", "#", T, T, MultiTermQuery.CONSTANT_SCORE_AUTO_REWRITE_DEFAULT), Occur.SHOULD); // hits no docs
            result = search.Search(bq, null, 1000).ScoreDocs;
            numHits = result.Length;
            assertEquals("wrong number of results", 1, numHits);
            for (int i = 0; i < numHits; i++)
            {
                Assert.AreEqual(score, result[i].Score, SCORE_COMP_THRESH, "score for " + i + " was not the same");
            }
        }

        [Test]
        public virtual void TestBoost()
        {
            // NOTE: uses index build in *this* setUp

            IndexSearcher search = NewSearcher(reader);

            // test for correct application of query normalization
            // must use a non score normalizing method for this.

            search.Similarity = new DefaultSimilarity();
            Query q = Csrq("data", "1", "6", T, T);
            q.Boost = 100;
            search.Search(q, null, new CollectorAnonymousClass(this));

            //
            // Ensure that boosting works to score one clause of a query higher
            // than another.
            //
            Query q1 = Csrq("data", "A", "A", T, T); // matches document #0
            q1.Boost = .1f;
            Query q2 = Csrq("data", "Z", "Z", T, T); // matches document #1
            BooleanQuery bq = new BooleanQuery(true);
            bq.Add(q1, Occur.SHOULD);
            bq.Add(q2, Occur.SHOULD);

            ScoreDoc[] hits = search.Search(bq, null, 1000).ScoreDocs;
            Assert.AreEqual(1, hits[0].Doc);
            Assert.AreEqual(0, hits[1].Doc);
            Assert.IsTrue(hits[0].Score > hits[1].Score);

            q1 = Csrq("data", "A", "A", T, T, MultiTermQuery.CONSTANT_SCORE_BOOLEAN_QUERY_REWRITE); // matches document #0
            q1.Boost = .1f;
            q2 = Csrq("data", "Z", "Z", T, T, MultiTermQuery.CONSTANT_SCORE_BOOLEAN_QUERY_REWRITE); // matches document #1
            bq = new BooleanQuery(true);
            bq.Add(q1, Occur.SHOULD);
            bq.Add(q2, Occur.SHOULD);

            hits = search.Search(bq, null, 1000).ScoreDocs;
            Assert.AreEqual(1, hits[0].Doc);
            Assert.AreEqual(0, hits[1].Doc);
            Assert.IsTrue(hits[0].Score > hits[1].Score);

            q1 = Csrq("data", "A", "A", T, T); // matches document #0
            q1.Boost = 10f;
            q2 = Csrq("data", "Z", "Z", T, T); // matches document #1
            bq = new BooleanQuery(true);
            bq.Add(q1, Occur.SHOULD);
            bq.Add(q2, Occur.SHOULD);

            hits = search.Search(bq, null, 1000).ScoreDocs;
            Assert.AreEqual(0, hits[0].Doc);
            Assert.AreEqual(1, hits[1].Doc);
            Assert.IsTrue(hits[0].Score > hits[1].Score);
        }

        private sealed class CollectorAnonymousClass : ICollector
        {
            private readonly TestMultiTermConstantScore outerInstance;

            public CollectorAnonymousClass(TestMultiTermConstantScore outerInstance)
            {
                this.outerInstance = outerInstance;
                @base = 0;
            }

            private int @base;
            private Scorer scorer;

            public void SetScorer(Scorer scorer)
            {
                this.scorer = scorer;
            }

            public void Collect(int doc)
            {
                Assert.AreEqual(1.0f, scorer.GetScore(), SCORE_COMP_THRESH, "score for doc " + (doc + @base) + " was not correct");
            }

            public void SetNextReader(AtomicReaderContext context)
            {
                @base = context.DocBase;
            }

            public bool AcceptsDocsOutOfOrder => true;
        }

        [Test]
        public virtual void TestBooleanOrderUnAffected()
        {
            // NOTE: uses index build in *this* setUp

            IndexSearcher search = NewSearcher(reader);

            // first do a regular TermRangeQuery which uses term expansion so
            // docs with more terms in range get higher scores

            Query rq = TermRangeQuery.NewStringRange("data", "1", "4", T, T);

            ScoreDoc[] expected = search.Search(rq, null, 1000).ScoreDocs;
            int numHits = expected.Length;

            // now do a boolean where which also contains a
            // ConstantScoreRangeQuery and make sure hte order is the same

            BooleanQuery q = new BooleanQuery();
            q.Add(rq, Occur.MUST); // T, F);
            q.Add(Csrq("data", "1", "6", T, T), Occur.MUST); // T, F);

            ScoreDoc[] actual = search.Search(q, null, 1000).ScoreDocs;

            assertEquals("wrong numebr of hits", numHits, actual.Length);
            for (int i = 0; i < numHits; i++)
            {
                assertEquals("mismatch in docid for hit#" + i, expected[i].Doc, actual[i].Doc);
            }
        }

        [Test]
        public virtual void TestRangeQueryId()
        {
            // NOTE: uses index build in *super* setUp

            IndexReader reader = signedIndexReader;
            IndexSearcher search = NewSearcher(reader);

            if (Verbose)
            {
                Console.WriteLine("TEST: reader=" + reader);
            }

            int medId = ((maxId - minId) / 2);

            string minIP = Pad(minId);
            string maxIP = Pad(maxId);
            string medIP = Pad(medId);

            int numDocs = reader.NumDocs;

            assertEquals("num of docs", numDocs, 1 + maxId - minId);

            ScoreDoc[] result;

            // test id, bounded on both ends

            result = search.Search(Csrq("id", minIP, maxIP, T, T), null, numDocs).ScoreDocs;
            assertEquals("find all", numDocs, result.Length);

            result = search.Search(Csrq("id", minIP, maxIP, T, T, MultiTermQuery.CONSTANT_SCORE_AUTO_REWRITE_DEFAULT), null, numDocs).ScoreDocs;
            assertEquals("find all", numDocs, result.Length);

            result = search.Search(Csrq("id", minIP, maxIP, T, F), null, numDocs).ScoreDocs;
            assertEquals("all but last", numDocs - 1, result.Length);

            result = search.Search(Csrq("id", minIP, maxIP, T, F, MultiTermQuery.CONSTANT_SCORE_AUTO_REWRITE_DEFAULT), null, numDocs).ScoreDocs;
            assertEquals("all but last", numDocs - 1, result.Length);

            result = search.Search(Csrq("id", minIP, maxIP, F, T), null, numDocs).ScoreDocs;
            assertEquals("all but first", numDocs - 1, result.Length);

            result = search.Search(Csrq("id", minIP, maxIP, F, T, MultiTermQuery.CONSTANT_SCORE_AUTO_REWRITE_DEFAULT), null, numDocs).ScoreDocs;
            assertEquals("all but first", numDocs - 1, result.Length);

            result = search.Search(Csrq("id", minIP, maxIP, F, F), null, numDocs).ScoreDocs;
            assertEquals("all but ends", numDocs - 2, result.Length);

            result = search.Search(Csrq("id", minIP, maxIP, F, F, MultiTermQuery.CONSTANT_SCORE_AUTO_REWRITE_DEFAULT), null, numDocs).ScoreDocs;
            assertEquals("all but ends", numDocs - 2, result.Length);

            result = search.Search(Csrq("id", medIP, maxIP, T, T), null, numDocs).ScoreDocs;
            assertEquals("med and up", 1 + maxId - medId, result.Length);

            result = search.Search(Csrq("id", medIP, maxIP, T, T, MultiTermQuery.CONSTANT_SCORE_AUTO_REWRITE_DEFAULT), null, numDocs).ScoreDocs;
            assertEquals("med and up", 1 + maxId - medId, result.Length);

            result = search.Search(Csrq("id", minIP, medIP, T, T), null, numDocs).ScoreDocs;
            assertEquals("up to med", 1 + medId - minId, result.Length);

            result = search.Search(Csrq("id", minIP, medIP, T, T, MultiTermQuery.CONSTANT_SCORE_AUTO_REWRITE_DEFAULT), null, numDocs).ScoreDocs;
            assertEquals("up to med", 1 + medId - minId, result.Length);

            // unbounded id

            result = search.Search(Csrq("id", minIP, null, T, F), null, numDocs).ScoreDocs;
            assertEquals("min and up", numDocs, result.Length);

            result = search.Search(Csrq("id", null, maxIP, F, T), null, numDocs).ScoreDocs;
            assertEquals("max and down", numDocs, result.Length);

            result = search.Search(Csrq("id", minIP, null, F, F), null, numDocs).ScoreDocs;
            assertEquals("not min, but up", numDocs - 1, result.Length);

            result = search.Search(Csrq("id", null, maxIP, F, F), null, numDocs).ScoreDocs;
            assertEquals("not max, but down", numDocs - 1, result.Length);

            result = search.Search(Csrq("id", medIP, maxIP, T, F), null, numDocs).ScoreDocs;
            assertEquals("med and up, not max", maxId - medId, result.Length);

            result = search.Search(Csrq("id", minIP, medIP, F, T), null, numDocs).ScoreDocs;
            assertEquals("not min, up to med", medId - minId, result.Length);

            // very small sets

            result = search.Search(Csrq("id", minIP, minIP, F, F), null, numDocs).ScoreDocs;
            assertEquals("min,min,F,F", 0, result.Length);

            result = search.Search(Csrq("id", minIP, minIP, F, F, MultiTermQuery.CONSTANT_SCORE_AUTO_REWRITE_DEFAULT), null, numDocs).ScoreDocs;
            assertEquals("min,min,F,F", 0, result.Length);

            result = search.Search(Csrq("id", medIP, medIP, F, F), null, numDocs).ScoreDocs;
            assertEquals("med,med,F,F", 0, result.Length);

            result = search.Search(Csrq("id", medIP, medIP, F, F, MultiTermQuery.CONSTANT_SCORE_AUTO_REWRITE_DEFAULT), null, numDocs).ScoreDocs;
            assertEquals("med,med,F,F", 0, result.Length);

            result = search.Search(Csrq("id", maxIP, maxIP, F, F), null, numDocs).ScoreDocs;
            assertEquals("max,max,F,F", 0, result.Length);

            result = search.Search(Csrq("id", maxIP, maxIP, F, F, MultiTermQuery.CONSTANT_SCORE_AUTO_REWRITE_DEFAULT), null, numDocs).ScoreDocs;
            assertEquals("max,max,F,F", 0, result.Length);

            result = search.Search(Csrq("id", minIP, minIP, T, T), null, numDocs).ScoreDocs;
            assertEquals("min,min,T,T", 1, result.Length);

            result = search.Search(Csrq("id", minIP, minIP, T, T, MultiTermQuery.CONSTANT_SCORE_AUTO_REWRITE_DEFAULT), null, numDocs).ScoreDocs;
            assertEquals("min,min,T,T", 1, result.Length);

            result = search.Search(Csrq("id", null, minIP, F, T), null, numDocs).ScoreDocs;
            assertEquals("nul,min,F,T", 1, result.Length);

            result = search.Search(Csrq("id", null, minIP, F, T, MultiTermQuery.CONSTANT_SCORE_AUTO_REWRITE_DEFAULT), null, numDocs).ScoreDocs;
            assertEquals("nul,min,F,T", 1, result.Length);

            result = search.Search(Csrq("id", maxIP, maxIP, T, T), null, numDocs).ScoreDocs;
            assertEquals("max,max,T,T", 1, result.Length);

            result = search.Search(Csrq("id", maxIP, maxIP, T, T, MultiTermQuery.CONSTANT_SCORE_AUTO_REWRITE_DEFAULT), null, numDocs).ScoreDocs;
            assertEquals("max,max,T,T", 1, result.Length);

            result = search.Search(Csrq("id", maxIP, null, T, F), null, numDocs).ScoreDocs;
            assertEquals("max,nul,T,T", 1, result.Length);

            result = search.Search(Csrq("id", maxIP, null, T, F, MultiTermQuery.CONSTANT_SCORE_AUTO_REWRITE_DEFAULT), null, numDocs).ScoreDocs;
            assertEquals("max,nul,T,T", 1, result.Length);

            result = search.Search(Csrq("id", medIP, medIP, T, T), null, numDocs).ScoreDocs;
            assertEquals("med,med,T,T", 1, result.Length);

            result = search.Search(Csrq("id", medIP, medIP, T, T, MultiTermQuery.CONSTANT_SCORE_AUTO_REWRITE_DEFAULT), null, numDocs).ScoreDocs;
            assertEquals("med,med,T,T", 1, result.Length);
        }

        [Test]
        public virtual void TestRangeQueryRand()
        {
            // NOTE: uses index build in *super* setUp

            IndexReader reader = signedIndexReader;
            IndexSearcher search = NewSearcher(reader);

            string minRP = Pad(signedIndexDir.minR);
            string maxRP = Pad(signedIndexDir.maxR);

            int numDocs = reader.NumDocs;

            assertEquals("num of docs", numDocs, 1 + maxId - minId);

            ScoreDoc[] result;

            // test extremes, bounded on both ends

            result = search.Search(Csrq("rand", minRP, maxRP, T, T), null, numDocs).ScoreDocs;
            assertEquals("find all", numDocs, result.Length);

            result = search.Search(Csrq("rand", minRP, maxRP, T, F), null, numDocs).ScoreDocs;
            assertEquals("all but biggest", numDocs - 1, result.Length);

            result = search.Search(Csrq("rand", minRP, maxRP, F, T), null, numDocs).ScoreDocs;
            assertEquals("all but smallest", numDocs - 1, result.Length);

            result = search.Search(Csrq("rand", minRP, maxRP, F, F), null, numDocs).ScoreDocs;
            assertEquals("all but extremes", numDocs - 2, result.Length);

            // unbounded

            result = search.Search(Csrq("rand", minRP, null, T, F), null, numDocs).ScoreDocs;
            assertEquals("smallest and up", numDocs, result.Length);

            result = search.Search(Csrq("rand", null, maxRP, F, T), null, numDocs).ScoreDocs;
            assertEquals("biggest and down", numDocs, result.Length);

            result = search.Search(Csrq("rand", minRP, null, F, F), null, numDocs).ScoreDocs;
            assertEquals("not smallest, but up", numDocs - 1, result.Length);

            result = search.Search(Csrq("rand", null, maxRP, F, F), null, numDocs).ScoreDocs;
            assertEquals("not biggest, but down", numDocs - 1, result.Length);

            // very small sets

            result = search.Search(Csrq("rand", minRP, minRP, F, F), null, numDocs).ScoreDocs;
            assertEquals("min,min,F,F", 0, result.Length);
            result = search.Search(Csrq("rand", maxRP, maxRP, F, F), null, numDocs).ScoreDocs;
            assertEquals("max,max,F,F", 0, result.Length);

            result = search.Search(Csrq("rand", minRP, minRP, T, T), null, numDocs).ScoreDocs;
            assertEquals("min,min,T,T", 1, result.Length);
            result = search.Search(Csrq("rand", null, minRP, F, T), null, numDocs).ScoreDocs;
            assertEquals("nul,min,F,T", 1, result.Length);

            result = search.Search(Csrq("rand", maxRP, maxRP, T, T), null, numDocs).ScoreDocs;
            assertEquals("max,max,T,T", 1, result.Length);
            result = search.Search(Csrq("rand", maxRP, null, T, F), null, numDocs).ScoreDocs;
            assertEquals("max,nul,T,T", 1, result.Length);
        }
    }
}