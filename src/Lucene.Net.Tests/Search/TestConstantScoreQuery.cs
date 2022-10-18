using Lucene.Net.Diagnostics;
using Lucene.Net.Documents;
using NUnit.Framework;
using Assert = Lucene.Net.TestFramework.Assert;

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
            searcher.Search(q, new CollectorAnonymousClass(this, expectedScore, scorerClassName, innerScorerClassName, count));
            Assert.AreEqual(1, count[0], "invalid number of results");
        }

        private sealed class CollectorAnonymousClass : ICollector
        {
            private readonly TestConstantScoreQuery outerInstance;

            private readonly float expectedScore;
            private readonly string scorerClassName;
            private readonly string innerScorerClassName;
            private readonly int[] count;

            public CollectorAnonymousClass(TestConstantScoreQuery outerInstance, float expectedScore, string scorerClassName, string innerScorerClassName, int[] count)
            {
                this.outerInstance = outerInstance;
                this.expectedScore = expectedScore;
                this.scorerClassName = scorerClassName;
                this.innerScorerClassName = innerScorerClassName;
                this.count = count;
            }

            private Scorer scorer;

            public void SetScorer(Scorer scorer)
            {
                this.scorer = scorer;
                Assert.AreEqual(scorerClassName, scorer.GetType().Name, "Scorer is implemented by wrong class");
                if (innerScorerClassName != null && scorer is ConstantScoreQuery.ConstantScorer)
                {
                    ConstantScoreQuery.ConstantScorer innerScorer = (ConstantScoreQuery.ConstantScorer)scorer;
                    Assert.AreEqual(innerScorerClassName, innerScorer.docIdSetIterator.GetType().Name, "inner Scorer is implemented by wrong class");
                }
            }

            public void Collect(int doc)
            {
                Assert.AreEqual(expectedScore, this.scorer.GetScore(), 0, "Score differs from expected");
                count[0]++;
            }

            public void SetNextReader(AtomicReaderContext context)
            {
            }

            public bool AcceptsDocsOutOfOrder => true;
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
                RandomIndexWriter writer = new RandomIndexWriter(Random, directory);

                Document doc = new Document();
                doc.Add(NewStringField("field", "term", Field.Store.NO));
                writer.AddDocument(doc);

                reader = writer.GetReader();
                writer.Dispose();
                // we don't wrap with AssertingIndexSearcher in order to have the original scorer in setScorer.
                searcher = NewSearcher(reader, true, false);

                // set a similarity that does not normalize our boost away
                searcher.Similarity = new DefaultSimilarityAnonymousClass(this);

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

        private sealed class DefaultSimilarityAnonymousClass : DefaultSimilarity
        {
            private readonly TestConstantScoreQuery outerInstance;

            public DefaultSimilarityAnonymousClass(TestConstantScoreQuery outerInstance)
            {
                this.outerInstance = outerInstance;
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
            RandomIndexWriter w = new RandomIndexWriter(Random, d);
            Document doc = new Document();
            doc.Add(NewStringField("field", "a", Field.Store.NO));
            w.AddDocument(doc);
            doc = new Document();
            doc.Add(NewStringField("field", "b", Field.Store.NO));
            w.AddDocument(doc);
            IndexReader r = w.GetReader();
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
            RandomIndexWriter w = new RandomIndexWriter(Random, d);
            Document doc = new Document();
            doc.Add(NewStringField("field", "a", Field.Store.NO));
            w.AddDocument(doc);
            IndexReader r = w.GetReader();
            w.Dispose();

            Filter filter = new QueryWrapperFilter(AssertingQuery.Wrap(Random, new TermQuery(new Term("field", "a"))));
            IndexSearcher s = NewSearcher(r);
            if (Debugging.AssertsEnabled) Debugging.Assert(s is AssertingIndexSearcher);
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