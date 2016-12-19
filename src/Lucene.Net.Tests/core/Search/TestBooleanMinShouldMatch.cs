using System;
using System.Globalization;
using Lucene.Net.Documents;

namespace Lucene.Net.Search
{
    using Lucene.Net.Randomized.Generators;
    using NUnit.Framework;
    using DefaultSimilarity = Lucene.Net.Search.Similarities.DefaultSimilarity;
    using Directory = Lucene.Net.Store.Directory;
    using Document = Documents.Document;

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

    using Field = Field;
    using IndexReader = Lucene.Net.Index.IndexReader;
    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
    using RandomIndexWriter = Lucene.Net.Index.RandomIndexWriter;
    using Similarity = Lucene.Net.Search.Similarities.Similarity;
    using Term = Lucene.Net.Index.Term;

    /// <summary>
    /// Test that BooleanQuery.setMinimumNumberShouldMatch works.
    /// </summary>
    [TestFixture]
    public class TestBooleanMinShouldMatch : LuceneTestCase
    {
        private static Directory Index;
        private static IndexReader r;
        private static IndexSearcher s;

        /// <summary>
        /// LUCENENET specific
        /// Is non-static because NewStringField is no longer static.
        /// </summary>
        [TestFixtureSetUp]
        public void BeforeClass()
        {
            string[] data = new string[] { "A 1 2 3 4 5 6", "Z       4 5 6", null, "B   2   4 5 6", "Y     3   5 6", null, "C     3     6", "X       4 5 6" };

            Index = NewDirectory();
            RandomIndexWriter w = new RandomIndexWriter(Random(), Index, Similarity, TimeZone);

            for (int i = 0; i < data.Length; i++)
            {
                Document doc = new Document();
                doc.Add(NewStringField("id", Convert.ToString(i), Field.Store.YES)); //Field.Keyword("id",String.valueOf(i)));
                doc.Add(NewStringField("all", "all", Field.Store.YES)); //Field.Keyword("all","all"));
                if (null != data[i])
                {
                    doc.Add(NewTextField("data", data[i], Field.Store.YES)); //Field.Text("data",data[i]));
                }
                w.AddDocument(doc);
            }

            r = w.Reader;
            s = NewSearcher(r);
            w.Dispose();
            //System.out.println("Set up " + getName());
        }

        [OneTimeTearDown]
        public static void AfterClass()
        {
            s = null;
            r.Dispose();
            r = null;
            Index.Dispose();
            Index = null;
        }

        public virtual void VerifyNrHits(Query q, int expected)
        {
            // bs1
            ScoreDoc[] h = s.Search(q, null, 1000).ScoreDocs;
            if (expected != h.Length)
            {
                PrintHits(TestName, h, s);
            }
            Assert.AreEqual(expected, h.Length, "result count");
            //System.out.println("TEST: now check");
            // bs2
            TopScoreDocCollector collector = TopScoreDocCollector.Create(1000, true);
            s.Search(q, collector);
            ScoreDoc[] h2 = collector.TopDocs().ScoreDocs;
            if (expected != h2.Length)
            {
                PrintHits(TestName, h2, s);
            }
            Assert.AreEqual(expected, h2.Length, "result count (bs2)");

            QueryUtils.Check(Random(), q, s, Similarity);
        }

        [Test]
        public virtual void TestAllOptional()
        {
            BooleanQuery q = new BooleanQuery();
            for (int i = 1; i <= 4; i++)
            {
                q.Add(new TermQuery(new Term("data", "" + i)), BooleanClause.Occur.SHOULD); //false, false);
            }
            q.MinimumNumberShouldMatch = 2; // match at least two of 4
            VerifyNrHits(q, 2);
        }

        [Test]
        public virtual void TestOneReqAndSomeOptional()
        {
            /* one required, some optional */
            BooleanQuery q = new BooleanQuery();
            q.Add(new TermQuery(new Term("all", "all")), BooleanClause.Occur.MUST); //true,  false);
            q.Add(new TermQuery(new Term("data", "5")), BooleanClause.Occur.SHOULD); //false, false);
            q.Add(new TermQuery(new Term("data", "4")), BooleanClause.Occur.SHOULD); //false, false);
            q.Add(new TermQuery(new Term("data", "3")), BooleanClause.Occur.SHOULD); //false, false);

            q.MinimumNumberShouldMatch = 2; // 2 of 3 optional

            VerifyNrHits(q, 5);
        }

        [Test]
        public virtual void TestSomeReqAndSomeOptional()
        {
            /* two required, some optional */
            BooleanQuery q = new BooleanQuery();
            q.Add(new TermQuery(new Term("all", "all")), BooleanClause.Occur.MUST); //true,  false);
            q.Add(new TermQuery(new Term("data", "6")), BooleanClause.Occur.MUST); //true,  false);
            q.Add(new TermQuery(new Term("data", "5")), BooleanClause.Occur.SHOULD); //false, false);
            q.Add(new TermQuery(new Term("data", "4")), BooleanClause.Occur.SHOULD); //false, false);
            q.Add(new TermQuery(new Term("data", "3")), BooleanClause.Occur.SHOULD); //false, false);

            q.MinimumNumberShouldMatch = 2; // 2 of 3 optional

            VerifyNrHits(q, 5);
        }

        [Test]
        public virtual void TestOneProhibAndSomeOptional()
        {
            /* one prohibited, some optional */
            BooleanQuery q = new BooleanQuery();
            q.Add(new TermQuery(new Term("data", "1")), BooleanClause.Occur.SHOULD); //false, false);
            q.Add(new TermQuery(new Term("data", "2")), BooleanClause.Occur.SHOULD); //false, false);
            q.Add(new TermQuery(new Term("data", "3")), BooleanClause.Occur.MUST_NOT); //false, true );
            q.Add(new TermQuery(new Term("data", "4")), BooleanClause.Occur.SHOULD); //false, false);

            q.MinimumNumberShouldMatch = 2; // 2 of 3 optional

            VerifyNrHits(q, 1);
        }

        [Test]
        public virtual void TestSomeProhibAndSomeOptional()
        {
            /* two prohibited, some optional */
            BooleanQuery q = new BooleanQuery();
            q.Add(new TermQuery(new Term("data", "1")), BooleanClause.Occur.SHOULD); //false, false);
            q.Add(new TermQuery(new Term("data", "2")), BooleanClause.Occur.SHOULD); //false, false);
            q.Add(new TermQuery(new Term("data", "3")), BooleanClause.Occur.MUST_NOT); //false, true );
            q.Add(new TermQuery(new Term("data", "4")), BooleanClause.Occur.SHOULD); //false, false);
            q.Add(new TermQuery(new Term("data", "C")), BooleanClause.Occur.MUST_NOT); //false, true );

            q.MinimumNumberShouldMatch = 2; // 2 of 3 optional

            VerifyNrHits(q, 1);
        }

        [Test]
        public virtual void TestOneReqOneProhibAndSomeOptional()
        {
            /* one required, one prohibited, some optional */
            BooleanQuery q = new BooleanQuery();
            q.Add(new TermQuery(new Term("data", "6")), BooleanClause.Occur.MUST); // true,  false);
            q.Add(new TermQuery(new Term("data", "5")), BooleanClause.Occur.SHOULD); //false, false);
            q.Add(new TermQuery(new Term("data", "4")), BooleanClause.Occur.SHOULD); //false, false);
            q.Add(new TermQuery(new Term("data", "3")), BooleanClause.Occur.MUST_NOT); //false, true );
            q.Add(new TermQuery(new Term("data", "2")), BooleanClause.Occur.SHOULD); //false, false);
            q.Add(new TermQuery(new Term("data", "1")), BooleanClause.Occur.SHOULD); //false, false);

            q.MinimumNumberShouldMatch = 3; // 3 of 4 optional

            VerifyNrHits(q, 1);
        }

        [Test]
        public virtual void TestSomeReqOneProhibAndSomeOptional()
        {
            /* two required, one prohibited, some optional */
            BooleanQuery q = new BooleanQuery();
            q.Add(new TermQuery(new Term("all", "all")), BooleanClause.Occur.MUST); //true,  false);
            q.Add(new TermQuery(new Term("data", "6")), BooleanClause.Occur.MUST); //true,  false);
            q.Add(new TermQuery(new Term("data", "5")), BooleanClause.Occur.SHOULD); //false, false);
            q.Add(new TermQuery(new Term("data", "4")), BooleanClause.Occur.SHOULD); //false, false);
            q.Add(new TermQuery(new Term("data", "3")), BooleanClause.Occur.MUST_NOT); //false, true );
            q.Add(new TermQuery(new Term("data", "2")), BooleanClause.Occur.SHOULD); //false, false);
            q.Add(new TermQuery(new Term("data", "1")), BooleanClause.Occur.SHOULD); //false, false);

            q.MinimumNumberShouldMatch = 3; // 3 of 4 optional

            VerifyNrHits(q, 1);
        }

        [Test]
        public virtual void TestOneReqSomeProhibAndSomeOptional()
        {
            /* one required, two prohibited, some optional */
            BooleanQuery q = new BooleanQuery();
            q.Add(new TermQuery(new Term("data", "6")), BooleanClause.Occur.MUST); //true,  false);
            q.Add(new TermQuery(new Term("data", "5")), BooleanClause.Occur.SHOULD); //false, false);
            q.Add(new TermQuery(new Term("data", "4")), BooleanClause.Occur.SHOULD); //false, false);
            q.Add(new TermQuery(new Term("data", "3")), BooleanClause.Occur.MUST_NOT); //false, true );
            q.Add(new TermQuery(new Term("data", "2")), BooleanClause.Occur.SHOULD); //false, false);
            q.Add(new TermQuery(new Term("data", "1")), BooleanClause.Occur.SHOULD); //false, false);
            q.Add(new TermQuery(new Term("data", "C")), BooleanClause.Occur.MUST_NOT); //false, true );

            q.MinimumNumberShouldMatch = 3; // 3 of 4 optional

            VerifyNrHits(q, 1);
        }

        [Test]
        public virtual void TestSomeReqSomeProhibAndSomeOptional()
        {
            /* two required, two prohibited, some optional */
            BooleanQuery q = new BooleanQuery();
            q.Add(new TermQuery(new Term("all", "all")), BooleanClause.Occur.MUST); //true,  false);
            q.Add(new TermQuery(new Term("data", "6")), BooleanClause.Occur.MUST); //true,  false);
            q.Add(new TermQuery(new Term("data", "5")), BooleanClause.Occur.SHOULD); //false, false);
            q.Add(new TermQuery(new Term("data", "4")), BooleanClause.Occur.SHOULD); //false, false);
            q.Add(new TermQuery(new Term("data", "3")), BooleanClause.Occur.MUST_NOT); //false, true );
            q.Add(new TermQuery(new Term("data", "2")), BooleanClause.Occur.SHOULD); //false, false);
            q.Add(new TermQuery(new Term("data", "1")), BooleanClause.Occur.SHOULD); //false, false);
            q.Add(new TermQuery(new Term("data", "C")), BooleanClause.Occur.MUST_NOT); //false, true );

            q.MinimumNumberShouldMatch = 3; // 3 of 4 optional

            VerifyNrHits(q, 1);
        }

        [Test]
        public virtual void TestMinHigherThenNumOptional()
        {
            /* two required, two prohibited, some optional */
            BooleanQuery q = new BooleanQuery();
            q.Add(new TermQuery(new Term("all", "all")), BooleanClause.Occur.MUST); //true,  false);
            q.Add(new TermQuery(new Term("data", "6")), BooleanClause.Occur.MUST); //true,  false);
            q.Add(new TermQuery(new Term("data", "5")), BooleanClause.Occur.SHOULD); //false, false);
            q.Add(new TermQuery(new Term("data", "4")), BooleanClause.Occur.SHOULD); //false, false);
            q.Add(new TermQuery(new Term("data", "3")), BooleanClause.Occur.MUST_NOT); //false, true );
            q.Add(new TermQuery(new Term("data", "2")), BooleanClause.Occur.SHOULD); //false, false);
            q.Add(new TermQuery(new Term("data", "1")), BooleanClause.Occur.SHOULD); //false, false);
            q.Add(new TermQuery(new Term("data", "C")), BooleanClause.Occur.MUST_NOT); //false, true );

            q.MinimumNumberShouldMatch = 90; // 90 of 4 optional ?!?!?!

            VerifyNrHits(q, 0);
        }

        [Test]
        public virtual void TestMinEqualToNumOptional()
        {
            /* two required, two optional */
            BooleanQuery q = new BooleanQuery();
            q.Add(new TermQuery(new Term("all", "all")), BooleanClause.Occur.SHOULD); //false, false);
            q.Add(new TermQuery(new Term("data", "6")), BooleanClause.Occur.MUST); //true,  false);
            q.Add(new TermQuery(new Term("data", "3")), BooleanClause.Occur.MUST); //true,  false);
            q.Add(new TermQuery(new Term("data", "2")), BooleanClause.Occur.SHOULD); //false, false);

            q.MinimumNumberShouldMatch = 2; // 2 of 2 optional

            VerifyNrHits(q, 1);
        }

        [Test]
        public virtual void TestOneOptionalEqualToMin()
        {
            /* two required, one optional */
            BooleanQuery q = new BooleanQuery();
            q.Add(new TermQuery(new Term("all", "all")), BooleanClause.Occur.MUST); //true,  false);
            q.Add(new TermQuery(new Term("data", "3")), BooleanClause.Occur.SHOULD); //false, false);
            q.Add(new TermQuery(new Term("data", "2")), BooleanClause.Occur.MUST); //true,  false);

            q.MinimumNumberShouldMatch = 1; // 1 of 1 optional

            VerifyNrHits(q, 1);
        }

        [Test]
        public virtual void TestNoOptionalButMin()
        {
            /* two required, no optional */
            BooleanQuery q = new BooleanQuery();
            q.Add(new TermQuery(new Term("all", "all")), BooleanClause.Occur.MUST); //true,  false);
            q.Add(new TermQuery(new Term("data", "2")), BooleanClause.Occur.MUST); //true,  false);

            q.MinimumNumberShouldMatch = 1; // 1 of 0 optional

            VerifyNrHits(q, 0);
        }

        [Test]
        public virtual void TestNoOptionalButMin2()
        {
            /* one required, no optional */
            BooleanQuery q = new BooleanQuery();
            q.Add(new TermQuery(new Term("all", "all")), BooleanClause.Occur.MUST); //true,  false);

            q.MinimumNumberShouldMatch = 1; // 1 of 0 optional

            VerifyNrHits(q, 0);
        }

        [Test]
        public virtual void TestRandomQueries()
        {
            const string field = "data";
            string[] vals = new string[] { "1", "2", "3", "4", "5", "6", "A", "Z", "B", "Y", "Z", "X", "foo" };
            int maxLev = 4;

            // callback object to set a random setMinimumNumberShouldMatch
            TestBoolean2.Callback minNrCB = new CallbackAnonymousInnerClassHelper(this, field, vals);

            // increase number of iterations for more complete testing
            int num = AtLeast(20);
            for (int i = 0; i < num; i++)
            {
                int lev = Random().Next(maxLev);
                int seed = Random().Next();
                BooleanQuery q1 = TestBoolean2.RandBoolQuery(new Random(seed), true, lev, field, vals, null);
                // BooleanQuery q2 = TestBoolean2.randBoolQuery(new Random(seed), lev, field, vals, minNrCB);
                BooleanQuery q2 = TestBoolean2.RandBoolQuery(new Random(seed), true, lev, field, vals, null);
                // only set minimumNumberShouldMatch on the top level query since setting
                // at a lower level can change the score.
                minNrCB.PostCreate(q2);

                // Can't use Hits because normalized scores will mess things
                // up.  The non-sorting version of search() that returns TopDocs
                // will not normalize scores.
                TopDocs top1 = s.Search(q1, null, 100);
                TopDocs top2 = s.Search(q2, null, 100);
                if (i < 100)
                {
                    QueryUtils.Check(Random(), q1, s, Similarity);
                    QueryUtils.Check(Random(), q2, s, Similarity);
                }
                AssertSubsetOfSameScores(q2, top1, top2);
            }
            // System.out.println("Total hits:"+tot);
        }

        private class CallbackAnonymousInnerClassHelper : TestBoolean2.Callback
        {
            private readonly TestBooleanMinShouldMatch OuterInstance;

            private string Field;
            private string[] Vals;

            public CallbackAnonymousInnerClassHelper(TestBooleanMinShouldMatch outerInstance, string field, string[] vals)
            {
                this.OuterInstance = outerInstance;
                this.Field = field;
                this.Vals = vals;
            }

            public virtual void PostCreate(BooleanQuery q)
            {
                BooleanClause[] c = q.Clauses;
                int opt = 0;
                for (int i = 0; i < c.Length; i++)
                {
                    if (c[i].Occur_ == BooleanClause.Occur.SHOULD)
                    {
                        opt++;
                    }
                }
                q.MinimumNumberShouldMatch = Random().Next(opt + 2);
                if (Random().NextBoolean())
                {
                    // also add a random negation
                    Term randomTerm = new Term(Field, Vals[Random().Next(Vals.Length)]);
                    q.Add(new TermQuery(randomTerm), BooleanClause.Occur.MUST_NOT);
                }
            }
        }

        private void AssertSubsetOfSameScores(Query q, TopDocs top1, TopDocs top2)
        {
            // The constrained query
            // should be a subset to the unconstrained query.
            if (top2.TotalHits > top1.TotalHits)
            {
                Assert.Fail("Constrained results not a subset:\n" + CheckHits.TopdocsString(top1, 0, 0) + CheckHits.TopdocsString(top2, 0, 0) + "for query:" + q.ToString());
            }

            for (int hit = 0; hit < top2.TotalHits; hit++)
            {
                int id = top2.ScoreDocs[hit].Doc;
                float score = top2.ScoreDocs[hit].Score;
                bool found = false;
                // find this doc in other hits
                for (int other = 0; other < top1.TotalHits; other++)
                {
                    if (top1.ScoreDocs[other].Doc == id)
                    {
                        found = true;
                        float otherScore = top1.ScoreDocs[other].Score;
                        // check if scores match
                        Assert.AreEqual(score, otherScore, CheckHits.ExplainToleranceDelta(score, otherScore), "Doc " + id + " scores don't match\n" + CheckHits.TopdocsString(top1, 0, 0) + CheckHits.TopdocsString(top2, 0, 0) + "for query:" + q.ToString());
                    }
                }

                // check if subset
                if (!found)
                {
                    Assert.Fail("Doc " + id + " not found\n" + CheckHits.TopdocsString(top1, 0, 0) + CheckHits.TopdocsString(top2, 0, 0) + "for query:" + q.ToString());
                }
            }
        }

        [Test]
        public virtual void TestRewriteCoord1()
        {
            Similarity oldSimilarity = s.Similarity;
            try
            {
                s.Similarity = new DefaultSimilarityAnonymousInnerClassHelper(this);
                BooleanQuery q1 = new BooleanQuery();
                q1.Add(new TermQuery(new Term("data", "1")), BooleanClause.Occur.SHOULD);
                BooleanQuery q2 = new BooleanQuery();
                q2.Add(new TermQuery(new Term("data", "1")), BooleanClause.Occur.SHOULD);
                q2.MinimumNumberShouldMatch = 1;
                TopDocs top1 = s.Search(q1, null, 100);
                TopDocs top2 = s.Search(q2, null, 100);
                AssertSubsetOfSameScores(q2, top1, top2);
            }
            finally
            {
                s.Similarity = oldSimilarity;
            }
        }

        private class DefaultSimilarityAnonymousInnerClassHelper : DefaultSimilarity
        {
            private readonly TestBooleanMinShouldMatch OuterInstance;

            public DefaultSimilarityAnonymousInnerClassHelper(TestBooleanMinShouldMatch outerInstance)
            {
                this.OuterInstance = outerInstance;
            }

            public override float Coord(int overlap, int maxOverlap)
            {
                return overlap / ((float)maxOverlap + 1);
            }
        }

        [Test]
        public virtual void TestRewriteNegate()
        {
            Similarity oldSimilarity = s.Similarity;
            try
            {
                s.Similarity = new DefaultSimilarityAnonymousInnerClassHelper2(this);
                BooleanQuery q1 = new BooleanQuery();
                q1.Add(new TermQuery(new Term("data", "1")), BooleanClause.Occur.SHOULD);
                BooleanQuery q2 = new BooleanQuery();
                q2.Add(new TermQuery(new Term("data", "1")), BooleanClause.Occur.SHOULD);
                q2.Add(new TermQuery(new Term("data", "Z")), BooleanClause.Occur.MUST_NOT);
                TopDocs top1 = s.Search(q1, null, 100);
                TopDocs top2 = s.Search(q2, null, 100);
                AssertSubsetOfSameScores(q2, top1, top2);
            }
            finally
            {
                s.Similarity = oldSimilarity;
            }
        }

        private class DefaultSimilarityAnonymousInnerClassHelper2 : DefaultSimilarity
        {
            private readonly TestBooleanMinShouldMatch OuterInstance;

            public DefaultSimilarityAnonymousInnerClassHelper2(TestBooleanMinShouldMatch outerInstance)
            {
                this.OuterInstance = outerInstance;
            }

            public override float Coord(int overlap, int maxOverlap)
            {
                return overlap / ((float)maxOverlap + 1);
            }
        }

        protected internal virtual void PrintHits(string test, ScoreDoc[] h, IndexSearcher searcher)
        {
            Console.Error.WriteLine("------- " + test + " -------");

            NumberFormatInfo f = new NumberFormatInfo();
            f.NumberDecimalSeparator = ".";

            //DecimalFormat f = new DecimalFormat("0.000000", DecimalFormatSymbols.getInstance(Locale.ROOT));

            for (int i = 0; i < h.Length; i++)
            {
                Document d = searcher.Doc(h[i].Doc);
                decimal score = (decimal)h[i].Score;
                Console.Error.WriteLine("#" + i + ": " + score.ToString(f) + " - " + d.Get("id") + " - " + d.Get("data"));
            }
        }
    }
}