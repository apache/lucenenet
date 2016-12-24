using System.Diagnostics;
using Lucene.Net.Documents;

namespace Lucene.Net.Search
{
    using NUnit.Framework;
    using AtomicReaderContext = Lucene.Net.Index.AtomicReaderContext;
    using DefaultSimilarity = Lucene.Net.Search.Similarities.DefaultSimilarity;
    using Directory = Lucene.Net.Store.Directory;

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

    using Document = Documents.Document;
    using Field = Field;
    using IndexReader = Lucene.Net.Index.IndexReader;
    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
    using RandomIndexWriter = Lucene.Net.Index.RandomIndexWriter;
    using Term = Lucene.Net.Index.Term;

    /// <summary>
    /// this class only tests some basic functionality in CSQ, the main parts are mostly
    /// tested by MultiTermQuery tests, explanations seems to be tested in TestExplanations!
    /// </summary>
    [TestFixture]
    public class TestConstantScoreQuery : LuceneTestCase
    {
        [Test]
        public virtual void TestCSQ()
        {
            Query q1 = new ConstantScoreQuery(new TermQuery(new Term("a", "b")));
            Query q2 = new ConstantScoreQuery(new TermQuery(new Term("a", "c")));
            Query q3 = new ConstantScoreQuery(TermRangeFilter.NewStringRange("a", "b", "c", true, true));
            QueryUtils.Check(q1);
            QueryUtils.Check(q2);
            QueryUtils.CheckEqual(q1, q1);
            QueryUtils.CheckEqual(q2, q2);
            QueryUtils.CheckEqual(q3, q3);
            QueryUtils.CheckUnequal(q1, q2);
            QueryUtils.CheckUnequal(q2, q3);
            QueryUtils.CheckUnequal(q1, q3);
            QueryUtils.CheckUnequal(q1, new TermQuery(new Term("a", "b")));
        }

        private void CheckHits(IndexSearcher searcher, Query q, float expectedScore, string scorerClassName, string innerScorerClassName)
        {
            int[] count = new int[1];
            searcher.Search(q, new CollectorAnonymousInnerClassHelper(this, expectedScore, scorerClassName, innerScorerClassName, count));
            Assert.AreEqual(1, count[0], "invalid number of results");
        }

        private class CollectorAnonymousInnerClassHelper : Collector
        {
            private readonly TestConstantScoreQuery OuterInstance;

            private float ExpectedScore;
            private string ScorerClassName;
            private string InnerScorerClassName;
            private int[] Count;

            public CollectorAnonymousInnerClassHelper(TestConstantScoreQuery outerInstance, float expectedScore, string scorerClassName, string innerScorerClassName, int[] count)
            {
                this.OuterInstance = outerInstance;
                this.ExpectedScore = expectedScore;
                this.ScorerClassName = scorerClassName;
                this.InnerScorerClassName = innerScorerClassName;
                this.Count = count;
            }

            private Scorer scorer;

            public override void SetScorer(Scorer scorer)
            {
                this.scorer = scorer;
                Assert.AreEqual(ScorerClassName, scorer.GetType().Name, "Scorer is implemented by wrong class");
                if (InnerScorerClassName != null && scorer is ConstantScoreQuery.ConstantScorer)
                {
                    ConstantScoreQuery.ConstantScorer innerScorer = (ConstantScoreQuery.ConstantScorer)scorer;
                    Assert.AreEqual(InnerScorerClassName, innerScorer.DocIdSetIterator.GetType().Name, "inner Scorer is implemented by wrong class");
                }
            }

            public override void Collect(int doc)
            {
                Assert.AreEqual(ExpectedScore, this.scorer.Score(), 0, "Score differs from expected");
                Count[0]++;
            }

            public override AtomicReaderContext NextReader
            {
                set
                {
                }
            }

            public override bool AcceptsDocsOutOfOrder()
            {
                return true;
            }
        }

        [Test]
        public virtual void TestWrapped2Times()
        {
            Directory directory = null;
            IndexReader reader = null;
            IndexSearcher searcher = null;
            try
            {
                directory = NewDirectory();
                RandomIndexWriter writer = new RandomIndexWriter(Random(), directory, Similarity, TimeZone);

                Document doc = new Document();
                doc.Add(NewStringField("field", "term", Field.Store.NO));
                writer.AddDocument(doc);

                reader = writer.Reader;
                writer.Dispose();
                // we don't wrap with AssertingIndexSearcher in order to have the original scorer in setScorer.
                searcher = NewSearcher(reader, true, false);

                // set a similarity that does not normalize our boost away
                searcher.Similarity = new DefaultSimilarityAnonymousInnerClassHelper(this);

                Query csq1 = new ConstantScoreQuery(new TermQuery(new Term("field", "term")));
                csq1.Boost = 2.0f;
                Query csq2 = new ConstantScoreQuery(csq1);
                csq2.Boost = 5.0f;

                BooleanQuery bq = new BooleanQuery();
                bq.Add(csq1, Occur.SHOULD);
                bq.Add(csq2, Occur.SHOULD);

                Query csqbq = new ConstantScoreQuery(bq);
                csqbq.Boost = 17.0f;

                CheckHits(searcher, csq1, csq1.Boost, typeof(ConstantScoreQuery.ConstantScorer).Name, null);
                CheckHits(searcher, csq2, csq2.Boost, typeof(ConstantScoreQuery.ConstantScorer).Name, typeof(ConstantScoreQuery.ConstantScorer).Name);

                // for the combined BQ, the scorer should always be BooleanScorer's BucketScorer, because our scorer supports out-of order collection!
                string bucketScorerClass = typeof(FakeScorer).Name;
                CheckHits(searcher, bq, csq1.Boost + csq2.Boost, bucketScorerClass, null);
                CheckHits(searcher, csqbq, csqbq.Boost, typeof(ConstantScoreQuery.ConstantScorer).Name, bucketScorerClass);
            }
            finally
            {
                if (reader != null)
                {
                    reader.Dispose();
                }
                if (directory != null)
                {
                    directory.Dispose();
                }
            }
        }

        private class DefaultSimilarityAnonymousInnerClassHelper : DefaultSimilarity
        {
            private readonly TestConstantScoreQuery OuterInstance;

            public DefaultSimilarityAnonymousInnerClassHelper(TestConstantScoreQuery outerInstance)
            {
                this.OuterInstance = outerInstance;
            }

            public override float QueryNorm(float sumOfSquaredWeights)
            {
                return 1.0f;
            }
        }

        [Test]
        public virtual void TestConstantScoreQueryAndFilter()
        {
            Directory d = NewDirectory();
            RandomIndexWriter w = new RandomIndexWriter(Random(), d, Similarity, TimeZone);
            Document doc = new Document();
            doc.Add(NewStringField("field", "a", Field.Store.NO));
            w.AddDocument(doc);
            doc = new Document();
            doc.Add(NewStringField("field", "b", Field.Store.NO));
            w.AddDocument(doc);
            IndexReader r = w.Reader;
            w.Dispose();

            Filter filterB = new CachingWrapperFilter(new QueryWrapperFilter(new TermQuery(new Term("field", "b"))));
            Query query = new ConstantScoreQuery(filterB);

            IndexSearcher s = NewSearcher(r);
            Assert.AreEqual(1, s.Search(query, filterB, 1).TotalHits); // Query for field:b, Filter field:b

            Filter filterA = new CachingWrapperFilter(new QueryWrapperFilter(new TermQuery(new Term("field", "a"))));
            query = new ConstantScoreQuery(filterA);

            Assert.AreEqual(0, s.Search(query, filterB, 1).TotalHits); // Query field:b, Filter field:a

            r.Dispose();
            d.Dispose();
        }

        // LUCENE-5307
        // don't reuse the scorer of filters since they have been created with bulkScorer=false
        [Test]
        public virtual void TestQueryWrapperFilter()
        {
            Directory d = NewDirectory();
            RandomIndexWriter w = new RandomIndexWriter(Random(), d, Similarity, TimeZone);
            Document doc = new Document();
            doc.Add(NewStringField("field", "a", Field.Store.NO));
            w.AddDocument(doc);
            IndexReader r = w.Reader;
            w.Dispose();

            Filter filter = new QueryWrapperFilter(AssertingQuery.Wrap(Random(), new TermQuery(new Term("field", "a"))));
            IndexSearcher s = NewSearcher(r);
            Debug.Assert(s is AssertingIndexSearcher);
            // this used to fail
            s.Search(new ConstantScoreQuery(filter), new TotalHitCountCollector());

            // check the rewrite
            Query rewritten = (new ConstantScoreQuery(filter)).Rewrite(r);
            Assert.IsTrue(rewritten is ConstantScoreQuery);
            Assert.IsTrue(((ConstantScoreQuery)rewritten).Query is AssertingQuery);

            r.Dispose();
            d.Dispose();
        }
    }
}