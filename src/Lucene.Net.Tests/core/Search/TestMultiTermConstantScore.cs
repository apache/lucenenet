using System;
using Lucene.Net.Documents;

namespace Lucene.Net.Search
{
    using NUnit.Framework;
    using AtomicReaderContext = Lucene.Net.Index.AtomicReaderContext;
    using DefaultSimilarity = Lucene.Net.Search.Similarities.DefaultSimilarity;
    using Directory = Lucene.Net.Store.Directory;
    using Document = Documents.Document;
    using Field = Field;
    using FieldType = FieldType;
    using IndexReader = Lucene.Net.Index.IndexReader;

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
    using RandomIndexWriter = Lucene.Net.Index.RandomIndexWriter;
    using Term = Lucene.Net.Index.Term;
    using TextField = TextField;

    [TestFixture]
    public class TestMultiTermConstantScore : BaseTestRangeFilter
    {
        /// <summary>
        /// threshold for comparing floats </summary>
        public const float SCORE_COMP_THRESH = 1e-6f;

        internal static Directory Small;
        internal static IndexReader Reader;

        public static void AssertEquals(string m, int e, int a)
        {
            Assert.AreEqual(e, a, m);
        }

        /// <summary>
        /// LUCENENET specific
        /// Is non-static because NewIndexWriterConfig is no longer static.
        /// </summary>
        [TestFixtureSetUp]
        public void BeforeClass()
        {
            string[] data = new string[] { "A 1 2 3 4 5 6", "Z       4 5 6", null, "B   2   4 5 6", "Y     3   5 6", null, "C     3     6", "X       4 5 6" };

            Small = NewDirectory();
            RandomIndexWriter writer = new RandomIndexWriter(Random(), Small, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random(), MockTokenizer.WHITESPACE, false)).SetMergePolicy(NewLogMergePolicy()));

            FieldType customType = new FieldType(TextField.TYPE_STORED);
            customType.Tokenized = false;
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

            Reader = writer.Reader;
            writer.Dispose();
        }

        [OneTimeTearDown]
        public static void AfterClass()
        {
            Reader.Dispose();
            Small.Dispose();
            Reader = null;
            Small = null;
        }

        /// <summary>
        /// macro for readability </summary>
        public static Query Csrq(string f, string l, string h, bool il, bool ih)
        {
            TermRangeQuery query = TermRangeQuery.NewStringRange(f, l, h, il, ih);
            query.SetRewriteMethod(MultiTermQuery.CONSTANT_SCORE_FILTER_REWRITE);
            if (VERBOSE)
            {
                Console.WriteLine("TEST: query=" + query);
            }
            return query;
        }

        public static Query Csrq(string f, string l, string h, bool il, bool ih, MultiTermQuery.RewriteMethod method)
        {
            TermRangeQuery query = TermRangeQuery.NewStringRange(f, l, h, il, ih);
            query.SetRewriteMethod(method);
            if (VERBOSE)
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
            query.SetRewriteMethod(MultiTermQuery.CONSTANT_SCORE_FILTER_REWRITE);
            return query;
        }

        /// <summary>
        /// macro for readability </summary>
        public static Query Cswcq(Term wild)
        {
            WildcardQuery query = new WildcardQuery(wild);
            query.SetRewriteMethod(MultiTermQuery.CONSTANT_SCORE_FILTER_REWRITE);
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

            IndexSearcher search = NewSearcher(Reader);

            ScoreDoc[] result;

            // some hits match more terms then others, score should be the same

            result = search.Search(Csrq("data", "1", "6", T, T), null, 1000).ScoreDocs;
            int numHits = result.Length;
            AssertEquals("wrong number of results", 6, numHits);
            float score = result[0].Score;
            for (int i = 1; i < numHits; i++)
            {
                Assert.AreEqual(score, result[i].Score, SCORE_COMP_THRESH, "score for " + i + " was not the same");
            }

            result = search.Search(Csrq("data", "1", "6", T, T, MultiTermQuery.CONSTANT_SCORE_BOOLEAN_QUERY_REWRITE), null, 1000).ScoreDocs;
            numHits = result.Length;
            AssertEquals("wrong number of results", 6, numHits);
            for (int i = 0; i < numHits; i++)
            {
                Assert.AreEqual(score, result[i].Score, SCORE_COMP_THRESH, "score for " + i + " was not the same");
            }

            result = search.Search(Csrq("data", "1", "6", T, T, MultiTermQuery.CONSTANT_SCORE_AUTO_REWRITE_DEFAULT), null, 1000).ScoreDocs;
            numHits = result.Length;
            AssertEquals("wrong number of results", 6, numHits);
            for (int i = 0; i < numHits; i++)
            {
                Assert.AreEqual(score, result[i].Score, SCORE_COMP_THRESH, "score for " + i + " was not the same");
            }
        }

        [Test]
        public virtual void TestEqualScoresWhenNoHits() // Test for LUCENE-5245: Empty MTQ rewrites should have a consistent norm, so always need to return a CSQ!
        {
            // NOTE: uses index build in *this* setUp

            IndexSearcher search = NewSearcher(Reader);

            ScoreDoc[] result;

            TermQuery dummyTerm = new TermQuery(new Term("data", "1"));

            BooleanQuery bq = new BooleanQuery();
            bq.Add(dummyTerm, BooleanClause.Occur.SHOULD); // hits one doc
            bq.Add(Csrq("data", "#", "#", T, T), BooleanClause.Occur.SHOULD); // hits no docs
            result = search.Search(bq, null, 1000).ScoreDocs;
            int numHits = result.Length;
            AssertEquals("wrong number of results", 1, numHits);
            float score = result[0].Score;
            for (int i = 1; i < numHits; i++)
            {
                Assert.AreEqual(score, result[i].Score, SCORE_COMP_THRESH, "score for " + i + " was not the same");
            }

            bq = new BooleanQuery();
            bq.Add(dummyTerm, BooleanClause.Occur.SHOULD); // hits one doc
            bq.Add(Csrq("data", "#", "#", T, T, MultiTermQuery.CONSTANT_SCORE_BOOLEAN_QUERY_REWRITE), BooleanClause.Occur.SHOULD); // hits no docs
            result = search.Search(bq, null, 1000).ScoreDocs;
            numHits = result.Length;
            AssertEquals("wrong number of results", 1, numHits);
            for (int i = 0; i < numHits; i++)
            {
                Assert.AreEqual(score, result[i].Score, SCORE_COMP_THRESH, "score for " + i + " was not the same");
            }

            bq = new BooleanQuery();
            bq.Add(dummyTerm, BooleanClause.Occur.SHOULD); // hits one doc
            bq.Add(Csrq("data", "#", "#", T, T, MultiTermQuery.CONSTANT_SCORE_AUTO_REWRITE_DEFAULT), BooleanClause.Occur.SHOULD); // hits no docs
            result = search.Search(bq, null, 1000).ScoreDocs;
            numHits = result.Length;
            AssertEquals("wrong number of results", 1, numHits);
            for (int i = 0; i < numHits; i++)
            {
                Assert.AreEqual(score, result[i].Score, SCORE_COMP_THRESH, "score for " + i + " was not the same");
            }
        }

        [Test]
        public virtual void TestBoost()
        {
            // NOTE: uses index build in *this* setUp

            IndexSearcher search = NewSearcher(Reader);

            // test for correct application of query normalization
            // must use a non score normalizing method for this.

            search.Similarity = new DefaultSimilarity();
            Query q = Csrq("data", "1", "6", T, T);
            q.Boost = 100;
            search.Search(q, null, new CollectorAnonymousInnerClassHelper(this));

            //
            // Ensure that boosting works to score one clause of a query higher
            // than another.
            //
            Query q1 = Csrq("data", "A", "A", T, T); // matches document #0
            q1.Boost = .1f;
            Query q2 = Csrq("data", "Z", "Z", T, T); // matches document #1
            BooleanQuery bq = new BooleanQuery(true);
            bq.Add(q1, BooleanClause.Occur.SHOULD);
            bq.Add(q2, BooleanClause.Occur.SHOULD);

            ScoreDoc[] hits = search.Search(bq, null, 1000).ScoreDocs;
            Assert.AreEqual(1, hits[0].Doc);
            Assert.AreEqual(0, hits[1].Doc);
            Assert.IsTrue(hits[0].Score > hits[1].Score);

            q1 = Csrq("data", "A", "A", T, T, MultiTermQuery.CONSTANT_SCORE_BOOLEAN_QUERY_REWRITE); // matches document #0
            q1.Boost = .1f;
            q2 = Csrq("data", "Z", "Z", T, T, MultiTermQuery.CONSTANT_SCORE_BOOLEAN_QUERY_REWRITE); // matches document #1
            bq = new BooleanQuery(true);
            bq.Add(q1, BooleanClause.Occur.SHOULD);
            bq.Add(q2, BooleanClause.Occur.SHOULD);

            hits = search.Search(bq, null, 1000).ScoreDocs;
            Assert.AreEqual(1, hits[0].Doc);
            Assert.AreEqual(0, hits[1].Doc);
            Assert.IsTrue(hits[0].Score > hits[1].Score);

            q1 = Csrq("data", "A", "A", T, T); // matches document #0
            q1.Boost = 10f;
            q2 = Csrq("data", "Z", "Z", T, T); // matches document #1
            bq = new BooleanQuery(true);
            bq.Add(q1, BooleanClause.Occur.SHOULD);
            bq.Add(q2, BooleanClause.Occur.SHOULD);

            hits = search.Search(bq, null, 1000).ScoreDocs;
            Assert.AreEqual(0, hits[0].Doc);
            Assert.AreEqual(1, hits[1].Doc);
            Assert.IsTrue(hits[0].Score > hits[1].Score);
        }

        private class CollectorAnonymousInnerClassHelper : Collector
        {
            private readonly TestMultiTermConstantScore OuterInstance;

            public CollectorAnonymousInnerClassHelper(TestMultiTermConstantScore outerInstance)
            {
                this.OuterInstance = outerInstance;
                @base = 0;
            }

            private int @base;
            private Scorer scorer;

            public override Scorer Scorer
            {
                set
                {
                    this.scorer = value;
                }
            }

            public override void Collect(int doc)
            {
                Assert.AreEqual(1.0f, scorer.Score(), SCORE_COMP_THRESH, "score for doc " + (doc + @base) + " was not correct");
            }

            public override AtomicReaderContext NextReader
            {
                set
                {
                    @base = value.DocBase;
                }
            }

            public override bool AcceptsDocsOutOfOrder()
            {
                return true;
            }
        }

        [Test]
        public virtual void TestBooleanOrderUnAffected()
        {
            // NOTE: uses index build in *this* setUp

            IndexSearcher search = NewSearcher(Reader);

            // first do a regular TermRangeQuery which uses term expansion so
            // docs with more terms in range get higher scores

            Query rq = TermRangeQuery.NewStringRange("data", "1", "4", T, T);

            ScoreDoc[] expected = search.Search(rq, null, 1000).ScoreDocs;
            int numHits = expected.Length;

            // now do a boolean where which also contains a
            // ConstantScoreRangeQuery and make sure hte order is the same

            BooleanQuery q = new BooleanQuery();
            q.Add(rq, BooleanClause.Occur.MUST); // T, F);
            q.Add(Csrq("data", "1", "6", T, T), BooleanClause.Occur.MUST); // T, F);

            ScoreDoc[] actual = search.Search(q, null, 1000).ScoreDocs;

            AssertEquals("wrong numebr of hits", numHits, actual.Length);
            for (int i = 0; i < numHits; i++)
            {
                AssertEquals("mismatch in docid for hit#" + i, expected[i].Doc, actual[i].Doc);
            }
        }

        [Test]
        public virtual void TestRangeQueryId()
        {
            // NOTE: uses index build in *super* setUp

            IndexReader reader = SignedIndexReader;
            IndexSearcher search = NewSearcher(reader);

            if (VERBOSE)
            {
                Console.WriteLine("TEST: reader=" + reader);
            }

            int medId = ((MaxId - MinId) / 2);

            string minIP = Pad(MinId);
            string maxIP = Pad(MaxId);
            string medIP = Pad(medId);

            int numDocs = reader.NumDocs;

            AssertEquals("num of docs", numDocs, 1 + MaxId - MinId);

            ScoreDoc[] result;

            // test id, bounded on both ends

            result = search.Search(Csrq("id", minIP, maxIP, T, T), null, numDocs).ScoreDocs;
            AssertEquals("find all", numDocs, result.Length);

            result = search.Search(Csrq("id", minIP, maxIP, T, T, MultiTermQuery.CONSTANT_SCORE_AUTO_REWRITE_DEFAULT), null, numDocs).ScoreDocs;
            AssertEquals("find all", numDocs, result.Length);

            result = search.Search(Csrq("id", minIP, maxIP, T, F), null, numDocs).ScoreDocs;
            AssertEquals("all but last", numDocs - 1, result.Length);

            result = search.Search(Csrq("id", minIP, maxIP, T, F, MultiTermQuery.CONSTANT_SCORE_AUTO_REWRITE_DEFAULT), null, numDocs).ScoreDocs;
            AssertEquals("all but last", numDocs - 1, result.Length);

            result = search.Search(Csrq("id", minIP, maxIP, F, T), null, numDocs).ScoreDocs;
            AssertEquals("all but first", numDocs - 1, result.Length);

            result = search.Search(Csrq("id", minIP, maxIP, F, T, MultiTermQuery.CONSTANT_SCORE_AUTO_REWRITE_DEFAULT), null, numDocs).ScoreDocs;
            AssertEquals("all but first", numDocs - 1, result.Length);

            result = search.Search(Csrq("id", minIP, maxIP, F, F), null, numDocs).ScoreDocs;
            AssertEquals("all but ends", numDocs - 2, result.Length);

            result = search.Search(Csrq("id", minIP, maxIP, F, F, MultiTermQuery.CONSTANT_SCORE_AUTO_REWRITE_DEFAULT), null, numDocs).ScoreDocs;
            AssertEquals("all but ends", numDocs - 2, result.Length);

            result = search.Search(Csrq("id", medIP, maxIP, T, T), null, numDocs).ScoreDocs;
            AssertEquals("med and up", 1 + MaxId - medId, result.Length);

            result = search.Search(Csrq("id", medIP, maxIP, T, T, MultiTermQuery.CONSTANT_SCORE_AUTO_REWRITE_DEFAULT), null, numDocs).ScoreDocs;
            AssertEquals("med and up", 1 + MaxId - medId, result.Length);

            result = search.Search(Csrq("id", minIP, medIP, T, T), null, numDocs).ScoreDocs;
            AssertEquals("up to med", 1 + medId - MinId, result.Length);

            result = search.Search(Csrq("id", minIP, medIP, T, T, MultiTermQuery.CONSTANT_SCORE_AUTO_REWRITE_DEFAULT), null, numDocs).ScoreDocs;
            AssertEquals("up to med", 1 + medId - MinId, result.Length);

            // unbounded id

            result = search.Search(Csrq("id", minIP, null, T, F), null, numDocs).ScoreDocs;
            AssertEquals("min and up", numDocs, result.Length);

            result = search.Search(Csrq("id", null, maxIP, F, T), null, numDocs).ScoreDocs;
            AssertEquals("max and down", numDocs, result.Length);

            result = search.Search(Csrq("id", minIP, null, F, F), null, numDocs).ScoreDocs;
            AssertEquals("not min, but up", numDocs - 1, result.Length);

            result = search.Search(Csrq("id", null, maxIP, F, F), null, numDocs).ScoreDocs;
            AssertEquals("not max, but down", numDocs - 1, result.Length);

            result = search.Search(Csrq("id", medIP, maxIP, T, F), null, numDocs).ScoreDocs;
            AssertEquals("med and up, not max", MaxId - medId, result.Length);

            result = search.Search(Csrq("id", minIP, medIP, F, T), null, numDocs).ScoreDocs;
            AssertEquals("not min, up to med", medId - MinId, result.Length);

            // very small sets

            result = search.Search(Csrq("id", minIP, minIP, F, F), null, numDocs).ScoreDocs;
            AssertEquals("min,min,F,F", 0, result.Length);

            result = search.Search(Csrq("id", minIP, minIP, F, F, MultiTermQuery.CONSTANT_SCORE_AUTO_REWRITE_DEFAULT), null, numDocs).ScoreDocs;
            AssertEquals("min,min,F,F", 0, result.Length);

            result = search.Search(Csrq("id", medIP, medIP, F, F), null, numDocs).ScoreDocs;
            AssertEquals("med,med,F,F", 0, result.Length);

            result = search.Search(Csrq("id", medIP, medIP, F, F, MultiTermQuery.CONSTANT_SCORE_AUTO_REWRITE_DEFAULT), null, numDocs).ScoreDocs;
            AssertEquals("med,med,F,F", 0, result.Length);

            result = search.Search(Csrq("id", maxIP, maxIP, F, F), null, numDocs).ScoreDocs;
            AssertEquals("max,max,F,F", 0, result.Length);

            result = search.Search(Csrq("id", maxIP, maxIP, F, F, MultiTermQuery.CONSTANT_SCORE_AUTO_REWRITE_DEFAULT), null, numDocs).ScoreDocs;
            AssertEquals("max,max,F,F", 0, result.Length);

            result = search.Search(Csrq("id", minIP, minIP, T, T), null, numDocs).ScoreDocs;
            AssertEquals("min,min,T,T", 1, result.Length);

            result = search.Search(Csrq("id", minIP, minIP, T, T, MultiTermQuery.CONSTANT_SCORE_AUTO_REWRITE_DEFAULT), null, numDocs).ScoreDocs;
            AssertEquals("min,min,T,T", 1, result.Length);

            result = search.Search(Csrq("id", null, minIP, F, T), null, numDocs).ScoreDocs;
            AssertEquals("nul,min,F,T", 1, result.Length);

            result = search.Search(Csrq("id", null, minIP, F, T, MultiTermQuery.CONSTANT_SCORE_AUTO_REWRITE_DEFAULT), null, numDocs).ScoreDocs;
            AssertEquals("nul,min,F,T", 1, result.Length);

            result = search.Search(Csrq("id", maxIP, maxIP, T, T), null, numDocs).ScoreDocs;
            AssertEquals("max,max,T,T", 1, result.Length);

            result = search.Search(Csrq("id", maxIP, maxIP, T, T, MultiTermQuery.CONSTANT_SCORE_AUTO_REWRITE_DEFAULT), null, numDocs).ScoreDocs;
            AssertEquals("max,max,T,T", 1, result.Length);

            result = search.Search(Csrq("id", maxIP, null, T, F), null, numDocs).ScoreDocs;
            AssertEquals("max,nul,T,T", 1, result.Length);

            result = search.Search(Csrq("id", maxIP, null, T, F, MultiTermQuery.CONSTANT_SCORE_AUTO_REWRITE_DEFAULT), null, numDocs).ScoreDocs;
            AssertEquals("max,nul,T,T", 1, result.Length);

            result = search.Search(Csrq("id", medIP, medIP, T, T), null, numDocs).ScoreDocs;
            AssertEquals("med,med,T,T", 1, result.Length);

            result = search.Search(Csrq("id", medIP, medIP, T, T, MultiTermQuery.CONSTANT_SCORE_AUTO_REWRITE_DEFAULT), null, numDocs).ScoreDocs;
            AssertEquals("med,med,T,T", 1, result.Length);
        }

        [Test]
        public virtual void TestRangeQueryRand()
        {
            // NOTE: uses index build in *super* setUp

            IndexReader reader = SignedIndexReader;
            IndexSearcher search = NewSearcher(reader);

            string minRP = Pad(SignedIndexDir.MinR);
            string maxRP = Pad(SignedIndexDir.MaxR);

            int numDocs = reader.NumDocs;

            AssertEquals("num of docs", numDocs, 1 + MaxId - MinId);

            ScoreDoc[] result;

            // test extremes, bounded on both ends

            result = search.Search(Csrq("rand", minRP, maxRP, T, T), null, numDocs).ScoreDocs;
            AssertEquals("find all", numDocs, result.Length);

            result = search.Search(Csrq("rand", minRP, maxRP, T, F), null, numDocs).ScoreDocs;
            AssertEquals("all but biggest", numDocs - 1, result.Length);

            result = search.Search(Csrq("rand", minRP, maxRP, F, T), null, numDocs).ScoreDocs;
            AssertEquals("all but smallest", numDocs - 1, result.Length);

            result = search.Search(Csrq("rand", minRP, maxRP, F, F), null, numDocs).ScoreDocs;
            AssertEquals("all but extremes", numDocs - 2, result.Length);

            // unbounded

            result = search.Search(Csrq("rand", minRP, null, T, F), null, numDocs).ScoreDocs;
            AssertEquals("smallest and up", numDocs, result.Length);

            result = search.Search(Csrq("rand", null, maxRP, F, T), null, numDocs).ScoreDocs;
            AssertEquals("biggest and down", numDocs, result.Length);

            result = search.Search(Csrq("rand", minRP, null, F, F), null, numDocs).ScoreDocs;
            AssertEquals("not smallest, but up", numDocs - 1, result.Length);

            result = search.Search(Csrq("rand", null, maxRP, F, F), null, numDocs).ScoreDocs;
            AssertEquals("not biggest, but down", numDocs - 1, result.Length);

            // very small sets

            result = search.Search(Csrq("rand", minRP, minRP, F, F), null, numDocs).ScoreDocs;
            AssertEquals("min,min,F,F", 0, result.Length);
            result = search.Search(Csrq("rand", maxRP, maxRP, F, F), null, numDocs).ScoreDocs;
            AssertEquals("max,max,F,F", 0, result.Length);

            result = search.Search(Csrq("rand", minRP, minRP, T, T), null, numDocs).ScoreDocs;
            AssertEquals("min,min,T,T", 1, result.Length);
            result = search.Search(Csrq("rand", null, minRP, F, T), null, numDocs).ScoreDocs;
            AssertEquals("nul,min,F,T", 1, result.Length);

            result = search.Search(Csrq("rand", maxRP, maxRP, T, T), null, numDocs).ScoreDocs;
            AssertEquals("max,max,T,T", 1, result.Length);
            result = search.Search(Csrq("rand", maxRP, null, T, F), null, numDocs).ScoreDocs;
            AssertEquals("max,nul,T,T", 1, result.Length);
        }


        #region SorterTestBase
        // LUCENENET NOTE: Tests in a base class are not pulled into the correct
        // context in Visual Studio. This fixes that with the minimum amount of code necessary
        // to run them in the correct context without duplicating all of the tests.

        [Test]
        public override void TestPad()
        {
            base.TestPad();
        }

        #endregion
    }
}