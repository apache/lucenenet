using Lucene.Net.Documents;

namespace Lucene.Net.Search.Spans
{
    using Lucene.Net.Index;
    using NUnit.Framework;
    using AtomicReaderContext = Lucene.Net.Index.AtomicReaderContext;
    using Directory = Lucene.Net.Store.Directory;
    using Document = Documents.Document;
    using Field = Field;
    using IndexReader = Lucene.Net.Index.IndexReader;
    using IndexReaderContext = Lucene.Net.Index.IndexReaderContext;
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

    [TestFixture]
    public class TestNearSpansOrdered : LuceneTestCase
    {
        protected internal IndexSearcher Searcher;
        protected internal Directory Directory;
        protected internal IndexReader Reader;

        public const string FIELD = "field";

        [TearDown]
        public override void TearDown()
        {
            Reader.Dispose();
            Directory.Dispose();
            base.TearDown();
        }

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            Directory = NewDirectory();
            RandomIndexWriter writer = new RandomIndexWriter(Random(), Directory, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())).SetMergePolicy(NewLogMergePolicy()));
            for (int i = 0; i < DocFields.Length; i++)
            {
                Document doc = new Document();
                doc.Add(NewTextField(FIELD, DocFields[i], Field.Store.NO));
                writer.AddDocument(doc);
            }
            Reader = writer.Reader;
            writer.Dispose();
            Searcher = NewSearcher(Reader);
        }

        protected internal string[] DocFields = new string[] { "w1 w2 w3 w4 w5", "w1 w3 w2 w3 zz", "w1 xx w2 yy w3", "w1 w3 xx w2 yy w3 zz" };

        protected internal virtual SpanNearQuery MakeQuery(string s1, string s2, string s3, int slop, bool inOrder)
        {
            return new SpanNearQuery(new SpanQuery[] { new SpanTermQuery(new Term(FIELD, s1)), new SpanTermQuery(new Term(FIELD, s2)), new SpanTermQuery(new Term(FIELD, s3)) }, slop, inOrder);
        }

        protected internal virtual SpanNearQuery MakeQuery()
        {
            return MakeQuery("w1", "w2", "w3", 1, true);
        }

        [Test]
        public virtual void TestSpanNearQuery()
        {
            SpanNearQuery q = MakeQuery();
            CheckHits.DoCheckHits(Random(), q, FIELD, Searcher, new int[] { 0, 1 }, Similarity);
        }

        public virtual string s(Spans span)
        {
            return s(span.Doc, span.Start, span.End);
        }

        public virtual string s(int doc, int start, int end)
        {
            return "s(" + doc + "," + start + "," + end + ")";
        }

        [Test]
        public virtual void TestNearSpansNext()
        {
            SpanNearQuery q = MakeQuery();
            Spans span = MultiSpansWrapper.Wrap(Searcher.TopReaderContext, q);
            Assert.AreEqual(true, span.Next());
            Assert.AreEqual(s(0, 0, 3), s(span));
            Assert.AreEqual(true, span.Next());
            Assert.AreEqual(s(1, 0, 4), s(span));
            Assert.AreEqual(false, span.Next());
        }

        /// <summary>
        /// test does not imply that skipTo(doc+1) should work exactly the
        /// same as next -- it's only applicable in this case since we know doc
        /// does not contain more than one span
        /// </summary>
        [Test]
        public virtual void TestNearSpansSkipToLikeNext()
        {
            SpanNearQuery q = MakeQuery();
            Spans span = MultiSpansWrapper.Wrap(Searcher.TopReaderContext, q);
            Assert.AreEqual(true, span.SkipTo(0));
            Assert.AreEqual(s(0, 0, 3), s(span));
            Assert.AreEqual(true, span.SkipTo(1));
            Assert.AreEqual(s(1, 0, 4), s(span));
            Assert.AreEqual(false, span.SkipTo(2));
        }

        [Test]
        public virtual void TestNearSpansNextThenSkipTo()
        {
            SpanNearQuery q = MakeQuery();
            Spans span = MultiSpansWrapper.Wrap(Searcher.TopReaderContext, q);
            Assert.AreEqual(true, span.Next());
            Assert.AreEqual(s(0, 0, 3), s(span));
            Assert.AreEqual(true, span.SkipTo(1));
            Assert.AreEqual(s(1, 0, 4), s(span));
            Assert.AreEqual(false, span.Next());
        }

        [Test]
        public virtual void TestNearSpansNextThenSkipPast()
        {
            SpanNearQuery q = MakeQuery();
            Spans span = MultiSpansWrapper.Wrap(Searcher.TopReaderContext, q);
            Assert.AreEqual(true, span.Next());
            Assert.AreEqual(s(0, 0, 3), s(span));
            Assert.AreEqual(false, span.SkipTo(2));
        }

        [Test]
        public virtual void TestNearSpansSkipPast()
        {
            SpanNearQuery q = MakeQuery();
            Spans span = MultiSpansWrapper.Wrap(Searcher.TopReaderContext, q);
            Assert.AreEqual(false, span.SkipTo(2));
        }

        [Test]
        public virtual void TestNearSpansSkipTo0()
        {
            SpanNearQuery q = MakeQuery();
            Spans span = MultiSpansWrapper.Wrap(Searcher.TopReaderContext, q);
            Assert.AreEqual(true, span.SkipTo(0));
            Assert.AreEqual(s(0, 0, 3), s(span));
        }

        [Test]
        public virtual void TestNearSpansSkipTo1()
        {
            SpanNearQuery q = MakeQuery();
            Spans span = MultiSpansWrapper.Wrap(Searcher.TopReaderContext, q);
            Assert.AreEqual(true, span.SkipTo(1));
            Assert.AreEqual(s(1, 0, 4), s(span));
        }

        /// <summary>
        /// not a direct test of NearSpans, but a demonstration of how/when
        /// this causes problems
        /// </summary>
        [Test]
        public virtual void TestSpanNearScorerSkipTo1()
        {
            SpanNearQuery q = MakeQuery();
            Weight w = Searcher.CreateNormalizedWeight(q);
            IndexReaderContext topReaderContext = Searcher.TopReaderContext;
            AtomicReaderContext leave = topReaderContext.Leaves[0];
            Scorer s = w.Scorer(leave, ((AtomicReader)leave.Reader).LiveDocs);
            Assert.AreEqual(1, s.Advance(1));
        }

        /// <summary>
        /// not a direct test of NearSpans, but a demonstration of how/when
        /// this causes problems
        /// </summary>
        [Test]
        public virtual void TestSpanNearScorerExplain()
        {
            SpanNearQuery q = MakeQuery();
            Explanation e = Searcher.Explain(q, 1);
            Assert.IsTrue(0.0f < e.Value, "Scorer explanation value for doc#1 isn't positive: " + e.ToString());
        }
    }
}