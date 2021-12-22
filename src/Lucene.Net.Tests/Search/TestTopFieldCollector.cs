using System;
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

    using Directory = Lucene.Net.Store.Directory;
    using Document = Documents.Document;
    using Entry = Lucene.Net.Search.FieldValueHitQueue.Entry;
    using IndexReader = Lucene.Net.Index.IndexReader;
    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
    using RandomIndexWriter = Lucene.Net.Index.RandomIndexWriter;

    [TestFixture]
    public class TestTopFieldCollector : LuceneTestCase
    {
        private IndexSearcher @is;
        private IndexReader ir;
        private Directory dir;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            dir = NewDirectory();
            RandomIndexWriter iw = new RandomIndexWriter(Random, dir);
            int numDocs = AtLeast(100);
            for (int i = 0; i < numDocs; i++)
            {
                Document doc = new Document();
                iw.AddDocument(doc);
            }
            ir = iw.GetReader();
            iw.Dispose();
            @is = NewSearcher(ir);
        }

        [TearDown]
        public override void TearDown()
        {
            ir.Dispose();
            dir.Dispose();
            base.TearDown();
        }

        [Test]
        public virtual void TestSortWithoutFillFields()
        {
            // There was previously a bug in TopFieldCollector when fillFields was set
            // to false - the same doc and score was set in ScoreDoc[] array. this test
            // asserts that if fillFields is false, the documents are set properly. It
            // does not use Searcher's default search methods (with Sort) since all set
            // fillFields to true.
            Sort[] sort = new Sort[] { new Sort(SortField.FIELD_DOC), new Sort() };
            for (int i = 0; i < sort.Length; i++)
            {
                Query q = new MatchAllDocsQuery();
                TopDocsCollector<Entry> tdc = TopFieldCollector.Create(sort[i], 10, false, false, false, true);

                @is.Search(q, tdc);

                ScoreDoc[] sd = tdc.GetTopDocs().ScoreDocs;
                for (int j = 1; j < sd.Length; j++)
                {
                    Assert.IsTrue(sd[j].Doc != sd[j - 1].Doc);
                }
            }
        }

        [Test]
        public virtual void TestSortWithoutScoreTracking()
        {
            // Two Sort criteria to instantiate the multi/single comparers.
            Sort[] sort = new Sort[] { new Sort(SortField.FIELD_DOC), new Sort() };
            for (int i = 0; i < sort.Length; i++)
            {
                Query q = new MatchAllDocsQuery();
                TopDocsCollector<Entry> tdc = TopFieldCollector.Create(sort[i], 10, true, false, false, true);

                @is.Search(q, tdc);

                TopDocs td = tdc.GetTopDocs();
                ScoreDoc[] sd = td.ScoreDocs;
                for (int j = 0; j < sd.Length; j++)
                {
                    Assert.IsTrue(float.IsNaN(sd[j].Score));
                }
                Assert.IsTrue(float.IsNaN(td.MaxScore));
            }
        }

        [Test]
        public virtual void TestSortWithScoreNoMaxScoreTracking()
        {
            // Two Sort criteria to instantiate the multi/single comparers.
            Sort[] sort = new Sort[] { new Sort(SortField.FIELD_DOC), new Sort() };
            for (int i = 0; i < sort.Length; i++)
            {
                Query q = new MatchAllDocsQuery();
                TopDocsCollector<Entry> tdc = TopFieldCollector.Create(sort[i], 10, true, true, false, true);

                @is.Search(q, tdc);

                TopDocs td = tdc.GetTopDocs();
                ScoreDoc[] sd = td.ScoreDocs;
                for (int j = 0; j < sd.Length; j++)
                {
                    Assert.IsTrue(!float.IsNaN(sd[j].Score));
                }
                Assert.IsTrue(float.IsNaN(td.MaxScore));
            }
        }

        // MultiComparerScoringNoMaxScoreCollector
        [Test]
        public virtual void TestSortWithScoreNoMaxScoreTrackingMulti()
        {
            // Two Sort criteria to instantiate the multi/single comparers.
            Sort[] sort = new Sort[] { new Sort(SortField.FIELD_DOC, SortField.FIELD_SCORE) };
            for (int i = 0; i < sort.Length; i++)
            {
                Query q = new MatchAllDocsQuery();
                TopDocsCollector<Entry> tdc = TopFieldCollector.Create(sort[i], 10, true, true, false, true);

                @is.Search(q, tdc);

                TopDocs td = tdc.GetTopDocs();
                ScoreDoc[] sd = td.ScoreDocs;
                for (int j = 0; j < sd.Length; j++)
                {
                    Assert.IsTrue(!float.IsNaN(sd[j].Score));
                }
                Assert.IsTrue(float.IsNaN(td.MaxScore));
            }
        }

        [Test]
        public virtual void TestSortWithScoreAndMaxScoreTracking()
        {
            // Two Sort criteria to instantiate the multi/single comparers.
            Sort[] sort = new Sort[] { new Sort(SortField.FIELD_DOC), new Sort() };
            for (int i = 0; i < sort.Length; i++)
            {
                Query q = new MatchAllDocsQuery();
                TopDocsCollector<Entry> tdc = TopFieldCollector.Create(sort[i], 10, true, true, true, true);

                @is.Search(q, tdc);

                TopDocs td = tdc.GetTopDocs();
                ScoreDoc[] sd = td.ScoreDocs;
                for (int j = 0; j < sd.Length; j++)
                {
                    Assert.IsTrue(!float.IsNaN(sd[j].Score));
                }
                Assert.IsTrue(!float.IsNaN(td.MaxScore));
            }
        }

        [Test]
        public virtual void TestOutOfOrderDocsScoringSort()
        {
            // Two Sort criteria to instantiate the multi/single comparers.
            Sort[] sort = new Sort[] { new Sort(SortField.FIELD_DOC), new Sort() };
            bool[][] tfcOptions = new bool[][] { new bool[] { false, false, false }, new bool[] { false, false, true }, new bool[] { false, true, false }, new bool[] { false, true, true }, new bool[] { true, false, false }, new bool[] { true, false, true }, new bool[] { true, true, false }, new bool[] { true, true, true } };
            string[] actualTFCClasses = new string[] { "OutOfOrderOneComparerNonScoringCollector", "OutOfOrderOneComparerScoringMaxScoreCollector", "OutOfOrderOneComparerScoringNoMaxScoreCollector", "OutOfOrderOneComparerScoringMaxScoreCollector", "OutOfOrderOneComparerNonScoringCollector", "OutOfOrderOneComparerScoringMaxScoreCollector", "OutOfOrderOneComparerScoringNoMaxScoreCollector", "OutOfOrderOneComparerScoringMaxScoreCollector" };

            BooleanQuery bq = new BooleanQuery();
            // Add a Query with SHOULD, since bw.Scorer() returns BooleanScorer2
            // which delegates to BS if there are no mandatory clauses.
            bq.Add(new MatchAllDocsQuery(), Occur.SHOULD);
            // Set minNrShouldMatch to 1 so that BQ will not optimize rewrite to return
            // the clause instead of BQ.
            bq.MinimumNumberShouldMatch = 1;
            for (int i = 0; i < sort.Length; i++)
            {
                for (int j = 0; j < tfcOptions.Length; j++)
                {
                    TopDocsCollector<Entry> tdc = TopFieldCollector.Create(sort[i], 10, tfcOptions[j][0], tfcOptions[j][1], tfcOptions[j][2], false);

                    Assert.IsTrue(tdc.GetType().Name.EndsWith(actualTFCClasses[j], StringComparison.Ordinal));

                    @is.Search(bq, tdc);

                    TopDocs td = tdc.GetTopDocs();
                    ScoreDoc[] sd = td.ScoreDocs;
                    Assert.AreEqual(10, sd.Length);
                }
            }
        }

        // OutOfOrderMulti*Collector
        [Test]
        public virtual void TestOutOfOrderDocsScoringSortMulti()
        {
            // Two Sort criteria to instantiate the multi/single comparers.
            Sort[] sort = new Sort[] { new Sort(SortField.FIELD_DOC, SortField.FIELD_SCORE) };
            bool[][] tfcOptions = new bool[][] { new bool[] { false, false, false }, new bool[] { false, false, true }, new bool[] { false, true, false }, new bool[] { false, true, true }, new bool[] { true, false, false }, new bool[] { true, false, true }, new bool[] { true, true, false }, new bool[] { true, true, true } };
            string[] actualTFCClasses = new string[] { "OutOfOrderMultiComparerNonScoringCollector", "OutOfOrderMultiComparerScoringMaxScoreCollector", "OutOfOrderMultiComparerScoringNoMaxScoreCollector", "OutOfOrderMultiComparerScoringMaxScoreCollector", "OutOfOrderMultiComparerNonScoringCollector", "OutOfOrderMultiComparerScoringMaxScoreCollector", "OutOfOrderMultiComparerScoringNoMaxScoreCollector", "OutOfOrderMultiComparerScoringMaxScoreCollector" };

            BooleanQuery bq = new BooleanQuery();
            // Add a Query with SHOULD, since bw.Scorer() returns BooleanScorer2
            // which delegates to BS if there are no mandatory clauses.
            bq.Add(new MatchAllDocsQuery(), Occur.SHOULD);
            // Set minNrShouldMatch to 1 so that BQ will not optimize rewrite to return
            // the clause instead of BQ.
            bq.MinimumNumberShouldMatch = 1;
            for (int i = 0; i < sort.Length; i++)
            {
                for (int j = 0; j < tfcOptions.Length; j++)
                {
                    TopDocsCollector<Entry> tdc = TopFieldCollector.Create(sort[i], 10, tfcOptions[j][0], tfcOptions[j][1], tfcOptions[j][2], false);

                    Assert.IsTrue(tdc.GetType().Name.EndsWith(actualTFCClasses[j], StringComparison.Ordinal));

                    @is.Search(bq, tdc);

                    TopDocs td = tdc.GetTopDocs();
                    ScoreDoc[] sd = td.ScoreDocs;
                    Assert.AreEqual(10, sd.Length);
                }
            }
        }

        [Test]
        public virtual void TestSortWithScoreAndMaxScoreTrackingNoResults()
        {
            // Two Sort criteria to instantiate the multi/single comparers.
            Sort[] sort = new Sort[] { new Sort(SortField.FIELD_DOC), new Sort() };
            for (int i = 0; i < sort.Length; i++)
            {
                TopDocsCollector<Entry> tdc = TopFieldCollector.Create(sort[i], 10, true, true, true, true);
                TopDocs td = tdc.GetTopDocs();
                Assert.AreEqual(0, td.TotalHits);
                Assert.IsTrue(float.IsNaN(td.MaxScore));
            }
        }
    }
}