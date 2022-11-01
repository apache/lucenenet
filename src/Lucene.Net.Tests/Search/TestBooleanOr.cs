using J2N.Threading.Atomic;
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
    using Directory = Lucene.Net.Store.Directory;
    using Document = Documents.Document;
    using FixedBitSet = Lucene.Net.Util.FixedBitSet;
    using IndexReader = Lucene.Net.Index.IndexReader;
    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
    using MockAnalyzer = Lucene.Net.Analysis.MockAnalyzer;
    using RandomIndexWriter = Lucene.Net.Index.RandomIndexWriter;
    using Term = Lucene.Net.Index.Term;
    using TestUtil = Lucene.Net.Util.TestUtil;
    using TextField = TextField;

    [TestFixture]
    public class TestBooleanOr : LuceneTestCase
    {
        private const string FIELD_T = "T";
        private const string FIELD_C = "C";

        private readonly TermQuery t1 = new TermQuery(new Term(FIELD_T, "files"));
        private readonly TermQuery t2 = new TermQuery(new Term(FIELD_T, "deleting"));
        private readonly TermQuery c1 = new TermQuery(new Term(FIELD_C, "production"));
        private readonly TermQuery c2 = new TermQuery(new Term(FIELD_C, "optimize"));

        private IndexSearcher searcher = null;
        private Directory dir;
        private IndexReader reader;

        private int Search(Query q)
        {
            QueryUtils.Check(Random, q, searcher);
            return searcher.Search(q, null, 1000).TotalHits;
        }

        [Test]
        public virtual void TestElements()
        {
            Assert.AreEqual(1, Search(t1));
            Assert.AreEqual(1, Search(t2));
            Assert.AreEqual(1, Search(c1));
            Assert.AreEqual(1, Search(c2));
        }

        /// <summary>
        /// <code>T:files T:deleting C:production C:optimize </code>
        /// it works.
        /// </summary>
        [Test]
        public virtual void TestFlat()
        {
            BooleanQuery q = new BooleanQuery();
            q.Add(new BooleanClause(t1, Occur.SHOULD));
            q.Add(new BooleanClause(t2, Occur.SHOULD));
            q.Add(new BooleanClause(c1, Occur.SHOULD));
            q.Add(new BooleanClause(c2, Occur.SHOULD));
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
            q3.Add(new BooleanClause(t1, Occur.SHOULD));
            q3.Add(new BooleanClause(t2, Occur.SHOULD));
            BooleanQuery q4 = new BooleanQuery();
            q4.Add(new BooleanClause(c1, Occur.MUST));
            q4.Add(new BooleanClause(c2, Occur.MUST));
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
            q3.Add(new BooleanClause(t1, Occur.SHOULD));
            q3.Add(new BooleanClause(t2, Occur.SHOULD));
            BooleanQuery q4 = new BooleanQuery();
            q4.Add(new BooleanClause(c1, Occur.SHOULD));
            q4.Add(new BooleanClause(c2, Occur.SHOULD));
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
            q3.Add(new BooleanClause(t1, Occur.SHOULD));
            q3.Add(new BooleanClause(t2, Occur.SHOULD));
            BooleanQuery q4 = new BooleanQuery();
            q4.Add(new BooleanClause(c1, Occur.SHOULD));
            q4.Add(new BooleanClause(c2, Occur.SHOULD));
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
            dir = NewDirectory();

            //
            RandomIndexWriter writer = new RandomIndexWriter(Random, dir);

            //
            Document d = new Document();
            d.Add(NewField(FIELD_T, "Optimize not deleting all files", TextField.TYPE_STORED));
            d.Add(NewField(FIELD_C, "Deleted When I run an optimize in our production environment.", TextField.TYPE_STORED));

            //
            writer.AddDocument(d);

            reader = writer.GetReader();
            //
            searcher = NewSearcher(reader);
            writer.Dispose();
        }

        [TearDown]
        public override void TearDown()
        {
            reader.Dispose();
            dir.Dispose();
            base.TearDown();
        }

        [Test]
        public virtual void TestBooleanScorerMax()
        {
            Directory dir = NewDirectory();
            RandomIndexWriter riw = new RandomIndexWriter(Random, dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)));

            int docCount = AtLeast(10000);

            for (int i = 0; i < docCount; i++)
            {
                Document doc = new Document();
                doc.Add(NewField("field", "a", TextField.TYPE_NOT_STORED));
                riw.AddDocument(doc);
            }

            riw.ForceMerge(1);
            IndexReader r = riw.GetReader();
            riw.Dispose();

            IndexSearcher s = NewSearcher(r);
            BooleanQuery bq = new BooleanQuery();
            bq.Add(new TermQuery(new Term("field", "a")), Occur.SHOULD);
            bq.Add(new TermQuery(new Term("field", "a")), Occur.SHOULD);

            Weight w = s.CreateNormalizedWeight(bq);

            Assert.AreEqual(1, s.IndexReader.Leaves.Count);
            BulkScorer scorer = w.GetBulkScorer(s.IndexReader.Leaves[0], false, null);

            FixedBitSet hits = new FixedBitSet(docCount);
            AtomicInt32 end = new AtomicInt32();
            ICollector c = new CollectorAnonymousClass(this, scorer, hits, end);

            while (end < docCount)
            {
                int inc = TestUtil.NextInt32(Random, 1, 1000);
                end.AddAndGet(inc);
                scorer.Score(c, end);
            }

            Assert.AreEqual(docCount, hits.Cardinality);
            r.Dispose();
            dir.Dispose();
        }

        private sealed class CollectorAnonymousClass : ICollector
        {
            private readonly TestBooleanOr outerInstance;

            private BulkScorer scorer;
            private readonly FixedBitSet hits;
            private readonly AtomicInt32 end;

            public CollectorAnonymousClass(TestBooleanOr outerInstance, BulkScorer scorer, FixedBitSet hits, AtomicInt32 end)
            {
                this.outerInstance = outerInstance;
                this.scorer = scorer;
                this.hits = hits;
                this.end = end;
            }

            public void SetNextReader(AtomicReaderContext context)
            {
            }

            public void Collect(int doc)
            {
                Assert.IsTrue(doc < end, "collected doc=" + doc + " beyond max=" + end);
                hits.Set(doc);
            }

            public void SetScorer(Scorer scorer)
            {
            }

            public bool AcceptsDocsOutOfOrder => true;
        }
    }
}