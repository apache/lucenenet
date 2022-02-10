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
    using Directory = Lucene.Net.Store.Directory;
    using Document = Documents.Document;
    using IndexReader = Lucene.Net.Index.IndexReader;
    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
    using RandomIndexWriter = Lucene.Net.Index.RandomIndexWriter;

    [TestFixture]
    public class TestTopDocsCollector : LuceneTestCase
    {
        private sealed class MyTopsDocCollector : TopDocsCollector<ScoreDoc>
        {
            private int idx = 0;
            private int @base = 0;

            public MyTopsDocCollector(int size)
                : base(new HitQueue(size, false))
            {
            }

            protected override TopDocs NewTopDocs(ScoreDoc[] results, int start)
            {
                if (results is null)
                {
                    return EMPTY_TOPDOCS;
                }

                float maxScore = float.NaN;
                if (start == 0)
                {
                    maxScore = results[0].Score;
                }
                else
                {
                    for (int i = m_pq.Count; i > 1; i--)
                    {
                        m_pq.Pop();
                    }
                    maxScore = m_pq.Pop().Score;
                }

                return new TopDocs(TotalHits, results, maxScore);
            }

            public override void Collect(int doc)
            {
                ++TotalHits;
                m_pq.InsertWithOverflow(new ScoreDoc(doc + @base, scores[idx++]));
            }

            public override void SetNextReader(AtomicReaderContext context)
            {
                @base = context.DocBase;
            }

            public override void SetScorer(Scorer scorer)
            {
                // Don't do anything. Assign scores in random
            }

            public override bool AcceptsDocsOutOfOrder => true;
        }

        // Scores array to be used by MyTopDocsCollector. If it is changed, MAX_SCORE
        // must also change.
        private static readonly float[] scores = new float[] { 0.7767749f, 1.7839992f, 8.9925785f, 7.9608946f, 0.07948637f, 2.6356435f, 7.4950366f, 7.1490803f, 8.108544f, 4.961808f, 2.2423935f, 7.285586f, 4.6699767f, 2.9655676f, 6.953706f, 5.383931f, 6.9916306f, 8.365894f, 7.888485f, 8.723962f, 3.1796896f, 0.39971232f, 1.3077754f, 6.8489285f, 9.17561f, 5.060466f, 7.9793315f, 8.601509f, 4.1858315f, 0.28146625f };

        private const float MAX_SCORE = 9.17561f;

        private Directory dir;
        private IndexReader reader;

        private TopDocsCollector<ScoreDoc> DoSearch(int numResults)
        {
            Query q = new MatchAllDocsQuery();
            IndexSearcher searcher = NewSearcher(reader);
            TopDocsCollector<ScoreDoc> tdc = new MyTopsDocCollector(numResults);
            searcher.Search(q, tdc);
            return tdc;
        }

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();

            // populate an index with 30 documents, this should be enough for the test.
            // The documents have no content - the test uses MatchAllDocsQuery().
            dir = NewDirectory();
            RandomIndexWriter writer = new RandomIndexWriter(Random, dir);
            for (int i = 0; i < 30; i++)
            {
                writer.AddDocument(new Document());
            }
            reader = writer.GetReader();
            writer.Dispose();
        }

        [TearDown]
        public override void TearDown()
        {
            reader.Dispose();
            dir.Dispose();
            dir = null;
            base.TearDown();
        }

        [Test]
        public virtual void TestInvalidArguments()
        {
            int numResults = 5;
            TopDocsCollector<ScoreDoc> tdc = DoSearch(numResults);

            // start < 0
            Assert.AreEqual(0, tdc.GetTopDocs(-1).ScoreDocs.Length);

            // start > pq.Size()
            Assert.AreEqual(0, tdc.GetTopDocs(numResults + 1).ScoreDocs.Length);

            // start == pq.Size()
            Assert.AreEqual(0, tdc.GetTopDocs(numResults).ScoreDocs.Length);

            // howMany < 0
            Assert.AreEqual(0, tdc.GetTopDocs(0, -1).ScoreDocs.Length);

            // howMany == 0
            Assert.AreEqual(0, tdc.GetTopDocs(0, 0).ScoreDocs.Length);
        }

        [Test]
        public virtual void TestZeroResults()
        {
            TopDocsCollector<ScoreDoc> tdc = new MyTopsDocCollector(5);
            Assert.AreEqual(0, tdc.GetTopDocs(0, 1).ScoreDocs.Length);
        }

        [Test]
        public virtual void TestFirstResultsPage()
        {
            TopDocsCollector<ScoreDoc> tdc = DoSearch(15);
            Assert.AreEqual(10, tdc.GetTopDocs(0, 10).ScoreDocs.Length);
        }

        [Test]
        public virtual void TestSecondResultsPages()
        {
            TopDocsCollector<ScoreDoc> tdc = DoSearch(15);
            // ask for more results than are available
            Assert.AreEqual(5, tdc.GetTopDocs(10, 10).ScoreDocs.Length);

            // ask for 5 results (exactly what there should be
            tdc = DoSearch(15);
            Assert.AreEqual(5, tdc.GetTopDocs(10, 5).ScoreDocs.Length);

            // ask for less results than there are
            tdc = DoSearch(15);
            Assert.AreEqual(4, tdc.GetTopDocs(10, 4).ScoreDocs.Length);
        }

        [Test]
        public virtual void TestGetAllResults()
        {
            TopDocsCollector<ScoreDoc> tdc = DoSearch(15);
            Assert.AreEqual(15, tdc.GetTopDocs().ScoreDocs.Length);
        }

        [Test]
        public virtual void TestGetResultsFromStart()
        {
            TopDocsCollector<ScoreDoc> tdc = DoSearch(15);
            // should bring all results
            Assert.AreEqual(15, tdc.GetTopDocs(0).ScoreDocs.Length);

            tdc = DoSearch(15);
            // get the last 5 only.
            Assert.AreEqual(5, tdc.GetTopDocs(10).ScoreDocs.Length);
        }

        [Test]
        public virtual void TestMaxScore()
        {
            // ask for all results
            TopDocsCollector<ScoreDoc> tdc = DoSearch(15);
            TopDocs td = tdc.GetTopDocs();
            Assert.AreEqual(MAX_SCORE, td.MaxScore, 0f);

            // ask for 5 last results
            tdc = DoSearch(15);
            td = tdc.GetTopDocs(10);
            Assert.AreEqual(MAX_SCORE, td.MaxScore, 0f);
        }

        // this does not test the PQ's correctness, but whether topDocs()
        // implementations return the results in decreasing score order.
        [Test]
        public virtual void TestResultsOrder()
        {
            TopDocsCollector<ScoreDoc> tdc = DoSearch(15);
            ScoreDoc[] sd = tdc.GetTopDocs().ScoreDocs;

            Assert.AreEqual(MAX_SCORE, sd[0].Score, 0f);
            for (int i = 1; i < sd.Length; i++)
            {
                Assert.IsTrue(sd[i - 1].Score >= sd[i].Score);
            }
        }
    }
}