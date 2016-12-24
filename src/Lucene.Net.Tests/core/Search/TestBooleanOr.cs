using Lucene.Net.Documents;

namespace Lucene.Net.Search
{
    using Lucene.Net.Support;
    using NUnit.Framework;
    using AtomicReaderContext = Lucene.Net.Index.AtomicReaderContext;
    using Directory = Lucene.Net.Store.Directory;
    using Document = Documents.Document;
    using FixedBitSet = Lucene.Net.Util.FixedBitSet;
    using IndexReader = Lucene.Net.Index.IndexReader;
    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;

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
    using RandomIndexWriter = Lucene.Net.Index.RandomIndexWriter;
    using Term = Lucene.Net.Index.Term;
    using TestUtil = Lucene.Net.Util.TestUtil;
    using TextField = TextField;

    [TestFixture]
    public class TestBooleanOr : LuceneTestCase
    {
        private static string FIELD_T = "T";
        private static string FIELD_C = "C";

        private TermQuery T1 = new TermQuery(new Term(FIELD_T, "files"));
        private TermQuery T2 = new TermQuery(new Term(FIELD_T, "deleting"));
        private TermQuery C1 = new TermQuery(new Term(FIELD_C, "production"));
        private TermQuery C2 = new TermQuery(new Term(FIELD_C, "optimize"));

        private IndexSearcher Searcher = null;
        private Directory Dir;
        private IndexReader Reader;

        private int Search(Query q)
        {
            QueryUtils.Check(Random(), q, Searcher, Similarity);
            return Searcher.Search(q, null, 1000).TotalHits;
        }

        [Test]
        public virtual void TestElements()
        {
            Assert.AreEqual(1, Search(T1));
            Assert.AreEqual(1, Search(T2));
            Assert.AreEqual(1, Search(C1));
            Assert.AreEqual(1, Search(C2));
        }

        /// <summary>
        /// <code>T:files T:deleting C:production C:optimize </code>
        /// it works.
        /// </summary>
        [Test]
        public virtual void TestFlat()
        {
            BooleanQuery q = new BooleanQuery();
            q.Add(new BooleanClause(T1, Occur.SHOULD));
            q.Add(new BooleanClause(T2, Occur.SHOULD));
            q.Add(new BooleanClause(C1, Occur.SHOULD));
            q.Add(new BooleanClause(C2, Occur.SHOULD));
            Assert.AreEqual(1, Search(q));
        }

        /// <summary>
        /// <code>(T:files T:deleting) (+C:production +C:optimize)</code>
        /// it works.
        /// </summary>
        [Test]
        public virtual void TestParenthesisMust()
        {
            BooleanQuery q3 = new BooleanQuery();
            q3.Add(new BooleanClause(T1, Occur.SHOULD));
            q3.Add(new BooleanClause(T2, Occur.SHOULD));
            BooleanQuery q4 = new BooleanQuery();
            q4.Add(new BooleanClause(C1, Occur.MUST));
            q4.Add(new BooleanClause(C2, Occur.MUST));
            BooleanQuery q2 = new BooleanQuery();
            q2.Add(q3, Occur.SHOULD);
            q2.Add(q4, Occur.SHOULD);
            Assert.AreEqual(1, Search(q2));
        }

        /// <summary>
        /// <code>(T:files T:deleting) +(C:production C:optimize)</code>
        /// not working. results NO HIT.
        /// </summary>
        [Test]
        public virtual void TestParenthesisMust2()
        {
            BooleanQuery q3 = new BooleanQuery();
            q3.Add(new BooleanClause(T1, Occur.SHOULD));
            q3.Add(new BooleanClause(T2, Occur.SHOULD));
            BooleanQuery q4 = new BooleanQuery();
            q4.Add(new BooleanClause(C1, Occur.SHOULD));
            q4.Add(new BooleanClause(C2, Occur.SHOULD));
            BooleanQuery q2 = new BooleanQuery();
            q2.Add(q3, Occur.SHOULD);
            q2.Add(q4, Occur.MUST);
            Assert.AreEqual(1, Search(q2));
        }

        /// <summary>
        /// <code>(T:files T:deleting) (C:production C:optimize)</code>
        /// not working. results NO HIT.
        /// </summary>
        [Test]
        public virtual void TestParenthesisShould()
        {
            BooleanQuery q3 = new BooleanQuery();
            q3.Add(new BooleanClause(T1, Occur.SHOULD));
            q3.Add(new BooleanClause(T2, Occur.SHOULD));
            BooleanQuery q4 = new BooleanQuery();
            q4.Add(new BooleanClause(C1, Occur.SHOULD));
            q4.Add(new BooleanClause(C2, Occur.SHOULD));
            BooleanQuery q2 = new BooleanQuery();
            q2.Add(q3, Occur.SHOULD);
            q2.Add(q4, Occur.SHOULD);
            Assert.AreEqual(1, Search(q2));
        }

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();

            //
            Dir = NewDirectory();

            //
            RandomIndexWriter writer = new RandomIndexWriter(Random(), Dir, Similarity, TimeZone);

            //
            Document d = new Document();
            d.Add(NewField(FIELD_T, "Optimize not deleting all files", TextField.TYPE_STORED));
            d.Add(NewField(FIELD_C, "Deleted When I run an optimize in our production environment.", TextField.TYPE_STORED));

            //
            writer.AddDocument(d);

            Reader = writer.Reader;
            //
            Searcher = NewSearcher(Reader);
            writer.Dispose();
        }

        [TearDown]
        public override void TearDown()
        {
            Reader.Dispose();
            Dir.Dispose();
            base.TearDown();
        }

        [Test]
        public virtual void TestBooleanScorerMax()
        {
            Directory dir = NewDirectory();
            RandomIndexWriter riw = new RandomIndexWriter(Random(), dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())));

            int docCount = AtLeast(10000);

            for (int i = 0; i < docCount; i++)
            {
                Document doc = new Document();
                doc.Add(NewField("field", "a", TextField.TYPE_NOT_STORED));
                riw.AddDocument(doc);
            }

            riw.ForceMerge(1);
            IndexReader r = riw.Reader;
            riw.Dispose();

            IndexSearcher s = NewSearcher(r);
            BooleanQuery bq = new BooleanQuery();
            bq.Add(new TermQuery(new Term("field", "a")), Occur.SHOULD);
            bq.Add(new TermQuery(new Term("field", "a")), Occur.SHOULD);

            Weight w = s.CreateNormalizedWeight(bq);

            Assert.AreEqual(1, s.IndexReader.Leaves.Count);
            BulkScorer scorer = w.BulkScorer(s.IndexReader.Leaves[0], false, null);

            FixedBitSet hits = new FixedBitSet(docCount);
            AtomicInteger end = new AtomicInteger();
            Collector c = new CollectorAnonymousInnerClassHelper(this, scorer, hits, end);

            while (end.Get() < docCount)
            {
                int inc = TestUtil.NextInt(Random(), 1, 1000);
                end.AddAndGet(inc);
                scorer.Score(c, end.Get());
            }

            Assert.AreEqual(docCount, hits.Cardinality());
            r.Dispose();
            dir.Dispose();
        }

        private class CollectorAnonymousInnerClassHelper : Collector
        {
            private readonly TestBooleanOr OuterInstance;

            private BulkScorer scorer;
            private FixedBitSet Hits;
            private AtomicInteger End;

            public CollectorAnonymousInnerClassHelper(TestBooleanOr outerInstance, BulkScorer scorer, FixedBitSet hits, AtomicInteger end)
            {
                this.OuterInstance = outerInstance;
                this.scorer = scorer;
                this.Hits = hits;
                this.End = end;
            }

            public override AtomicReaderContext NextReader
            {
                set
                {
                }
            }

            public override void Collect(int doc)
            {
                Assert.IsTrue(doc < End.Get(), "collected doc=" + doc + " beyond max=" + End);
                Hits.Set(doc);
            }

            public override void SetScorer(Scorer scorer)
            {
            }

            public override bool AcceptsDocsOutOfOrder()
            {
                return true;
            }
        }
    }
}