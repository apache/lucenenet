using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Index.Extensions;
using NUnit.Framework;
using System.Collections.Generic;
using Assert = Lucene.Net.TestFramework.Assert;

namespace Lucene.Net.Search.Spans
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
    using DirectoryReader = Lucene.Net.Index.DirectoryReader;
    using Document = Documents.Document;
    using Field = Field;
    using IndexReader = Lucene.Net.Index.IndexReader;
    using IndexReaderContext = Lucene.Net.Index.IndexReaderContext;
    using IndexWriter = Lucene.Net.Index.IndexWriter;
    using IndexWriterConfig = Lucene.Net.Index.IndexWriterConfig;
    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
    using MockAnalyzer = Lucene.Net.Analysis.MockAnalyzer;
    using RandomIndexWriter = Lucene.Net.Index.RandomIndexWriter;
    using ReaderUtil = Lucene.Net.Index.ReaderUtil;
    using Similarity = Lucene.Net.Search.Similarities.Similarity;
    using Term = Lucene.Net.Index.Term;

    [TestFixture]
    public class TestSpans : LuceneTestCase
    {
        private IndexSearcher searcher;
        private IndexReader reader;
        private Directory directory;

        public const string field = "field";

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            directory = NewDirectory();
            RandomIndexWriter writer = new RandomIndexWriter(Random, directory, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)).SetMergePolicy(NewLogMergePolicy()));
            for (int i = 0; i < docFields.Length; i++)
            {
                Document doc = new Document();
                doc.Add(NewTextField(field, docFields[i], Field.Store.YES));
                writer.AddDocument(doc);
            }
            reader = writer.GetReader();
            writer.Dispose();
            searcher = NewSearcher(reader);
        }

        [TearDown]
        public override void TearDown()
        {
            reader.Dispose();
            directory.Dispose();
            base.TearDown();
        }

        private readonly string[] docFields = new string[] { "w1 w2 w3 w4 w5", "w1 w3 w2 w3", "w1 xx w2 yy w3", "w1 w3 xx w2 yy w3", "u2 u2 u1", "u2 xx u2 u1", "u2 u2 xx u1", "u2 xx u2 yy u1", "u2 xx u1 u2", "u2 u1 xx u2", "u1 u2 xx u2", "t1 t2 t1 t3 t2 t3", "s2 s1 s1 xx xx s2 xx s2 xx s1 xx xx xx xx xx s2 xx" };

        public virtual SpanTermQuery MakeSpanTermQuery(string text)
        {
            return new SpanTermQuery(new Term(field, text));
        }

        private void CheckHits(Query query, int[] results)
        {
            Search.CheckHits.DoCheckHits(Random, query, field, searcher, results);
        }

        private void OrderedSlopTest3SQ(SpanQuery q1, SpanQuery q2, SpanQuery q3, int slop, int[] expectedDocs)
        {
            bool ordered = true;
            SpanNearQuery snq = new SpanNearQuery(new SpanQuery[] { q1, q2, q3 }, slop, ordered);
            CheckHits(snq, expectedDocs);
        }

        public virtual void OrderedSlopTest3(int slop, int[] expectedDocs)
        {
            OrderedSlopTest3SQ(MakeSpanTermQuery("w1"), MakeSpanTermQuery("w2"), MakeSpanTermQuery("w3"), slop, expectedDocs);
        }

        public virtual void OrderedSlopTest3Equal(int slop, int[] expectedDocs)
        {
            OrderedSlopTest3SQ(MakeSpanTermQuery("w1"), MakeSpanTermQuery("w3"), MakeSpanTermQuery("w3"), slop, expectedDocs);
        }

        public virtual void OrderedSlopTest1Equal(int slop, int[] expectedDocs)
        {
            OrderedSlopTest3SQ(MakeSpanTermQuery("u2"), MakeSpanTermQuery("u2"), MakeSpanTermQuery("u1"), slop, expectedDocs);
        }

        [Test]
        public virtual void TestSpanNearOrdered01()
        {
            OrderedSlopTest3(0, new int[] { 0 });
        }

        [Test]
        public virtual void TestSpanNearOrdered02()
        {
            OrderedSlopTest3(1, new int[] { 0, 1 });
        }

        [Test]
        public virtual void TestSpanNearOrdered03()
        {
            OrderedSlopTest3(2, new int[] { 0, 1, 2 });
        }

        [Test]
        public virtual void TestSpanNearOrdered04()
        {
            OrderedSlopTest3(3, new int[] { 0, 1, 2, 3 });
        }

        [Test]
        public virtual void TestSpanNearOrdered05()
        {
            OrderedSlopTest3(4, new int[] { 0, 1, 2, 3 });
        }

        [Test]
        public virtual void TestSpanNearOrderedEqual01()
        {
            OrderedSlopTest3Equal(0, new int[] { });
        }

        [Test]
        public virtual void TestSpanNearOrderedEqual02()
        {
            OrderedSlopTest3Equal(1, new int[] { 1 });
        }

        [Test]
        public virtual void TestSpanNearOrderedEqual03()
        {
            OrderedSlopTest3Equal(2, new int[] { 1 });
        }

        [Test]
        public virtual void TestSpanNearOrderedEqual04()
        {
            OrderedSlopTest3Equal(3, new int[] { 1, 3 });
        }

        [Test]
        public virtual void TestSpanNearOrderedEqual11()
        {
            OrderedSlopTest1Equal(0, new int[] { 4 });
        }

        [Test]
        public virtual void TestSpanNearOrderedEqual12()
        {
            OrderedSlopTest1Equal(0, new int[] { 4 });
        }

        [Test]
        public virtual void TestSpanNearOrderedEqual13()
        {
            OrderedSlopTest1Equal(1, new int[] { 4, 5, 6 });
        }

        [Test]
        public virtual void TestSpanNearOrderedEqual14()
        {
            OrderedSlopTest1Equal(2, new int[] { 4, 5, 6, 7 });
        }

        [Test]
        public virtual void TestSpanNearOrderedEqual15()
        {
            OrderedSlopTest1Equal(3, new int[] { 4, 5, 6, 7 });
        }

        [Test]
        public virtual void TestSpanNearOrderedOverlap()
        {
            bool ordered = true;
            int slop = 1;
            SpanNearQuery snq = new SpanNearQuery(new SpanQuery[] { MakeSpanTermQuery("t1"), MakeSpanTermQuery("t2"), MakeSpanTermQuery("t3") }, slop, ordered);
            Spans spans = MultiSpansWrapper.Wrap(searcher.TopReaderContext, snq);

            Assert.IsTrue(spans.MoveNext(), "first range");
            Assert.AreEqual(11, spans.Doc, "first doc");
            Assert.AreEqual(0, spans.Start, "first start");
            Assert.AreEqual(4, spans.End, "first end");

            Assert.IsTrue(spans.MoveNext(), "second range");
            Assert.AreEqual(11, spans.Doc, "second doc");
            Assert.AreEqual(2, spans.Start, "second start");
            Assert.AreEqual(6, spans.End, "second end");

            Assert.IsFalse(spans.MoveNext(), "third range");
        }

        [Test]
        public virtual void TestSpanNearUnOrdered()
        {
            //See http://www.gossamer-threads.com/lists/lucene/java-dev/52270 for discussion about this test
            SpanNearQuery snq;
            snq = new SpanNearQuery(new SpanQuery[] { MakeSpanTermQuery("u1"), MakeSpanTermQuery("u2") }, 0, false);
            Spans spans = MultiSpansWrapper.Wrap(searcher.TopReaderContext, snq);
            Assert.IsTrue(spans.MoveNext(), "Does not have next and it should");
            Assert.AreEqual(4, spans.Doc, "doc");
            Assert.AreEqual(1, spans.Start, "start");
            Assert.AreEqual(3, spans.End, "end");

            Assert.IsTrue(spans.MoveNext(), "Does not have next and it should");
            Assert.AreEqual(5, spans.Doc, "doc");
            Assert.AreEqual(2, spans.Start, "start");
            Assert.AreEqual(4, spans.End, "end");

            Assert.IsTrue(spans.MoveNext(), "Does not have next and it should");
            Assert.AreEqual(8, spans.Doc, "doc");
            Assert.AreEqual(2, spans.Start, "start");
            Assert.AreEqual(4, spans.End, "end");

            Assert.IsTrue(spans.MoveNext(), "Does not have next and it should");
            Assert.AreEqual(9, spans.Doc, "doc");
            Assert.AreEqual(0, spans.Start, "start");
            Assert.AreEqual(2, spans.End, "end");

            Assert.IsTrue(spans.MoveNext(), "Does not have next and it should");
            Assert.AreEqual(10, spans.Doc, "doc");
            Assert.AreEqual(0, spans.Start, "start");
            Assert.AreEqual(2, spans.End, "end");
            Assert.IsTrue(spans.MoveNext() == false, "Has next and it shouldn't: " + spans.Doc);

            SpanNearQuery u1u2 = new SpanNearQuery(new SpanQuery[] { MakeSpanTermQuery("u1"), MakeSpanTermQuery("u2") }, 0, false);
            snq = new SpanNearQuery(new SpanQuery[] { u1u2, MakeSpanTermQuery("u2") }, 1, false);
            spans = MultiSpansWrapper.Wrap(searcher.TopReaderContext, snq);
            Assert.IsTrue(spans.MoveNext(), "Does not have next and it should");
            Assert.AreEqual(4, spans.Doc, "doc");
            Assert.AreEqual(0, spans.Start, "start");
            Assert.AreEqual(3, spans.End, "end");

            Assert.IsTrue(spans.MoveNext(), "Does not have next and it should");
            //unordered spans can be subsets
            Assert.AreEqual(4, spans.Doc, "doc");
            Assert.AreEqual(1, spans.Start, "start");
            Assert.AreEqual(3, spans.End, "end");

            Assert.IsTrue(spans.MoveNext(), "Does not have next and it should");
            Assert.AreEqual(5, spans.Doc, "doc");
            Assert.AreEqual(0, spans.Start, "start");
            Assert.AreEqual(4, spans.End, "end");

            Assert.IsTrue(spans.MoveNext(), "Does not have next and it should");
            Assert.AreEqual(5, spans.Doc, "doc");
            Assert.AreEqual(2, spans.Start, "start");
            Assert.AreEqual(4, spans.End, "end");

            Assert.IsTrue(spans.MoveNext(), "Does not have next and it should");
            Assert.AreEqual(8, spans.Doc, "doc");
            Assert.AreEqual(0, spans.Start, "start");
            Assert.AreEqual(4, spans.End, "end");

            Assert.IsTrue(spans.MoveNext(), "Does not have next and it should");
            Assert.AreEqual(8, spans.Doc, "doc");
            Assert.AreEqual(2, spans.Start, "start");
            Assert.AreEqual(4, spans.End, "end");

            Assert.IsTrue(spans.MoveNext(), "Does not have next and it should");
            Assert.AreEqual(9, spans.Doc, "doc");
            Assert.AreEqual(0, spans.Start, "start");
            Assert.AreEqual(2, spans.End, "end");

            Assert.IsTrue(spans.MoveNext(), "Does not have next and it should");
            Assert.AreEqual(9, spans.Doc, "doc");
            Assert.AreEqual(0, spans.Start, "start");
            Assert.AreEqual(4, spans.End, "end");

            Assert.IsTrue(spans.MoveNext(), "Does not have next and it should");
            Assert.AreEqual(10, spans.Doc, "doc");
            Assert.AreEqual(0, spans.Start, "start");
            Assert.AreEqual(2, spans.End, "end");

            Assert.IsTrue(spans.MoveNext() == false, "Has next and it shouldn't");
        }

        private Spans OrSpans(string[] terms)
        {
            SpanQuery[] sqa = new SpanQuery[terms.Length];
            for (int i = 0; i < terms.Length; i++)
            {
                sqa[i] = MakeSpanTermQuery(terms[i]);
            }
            return MultiSpansWrapper.Wrap(searcher.TopReaderContext, new SpanOrQuery(sqa));
        }

        private void TstNextSpans(Spans spans, int doc, int start, int end)
        {
            Assert.IsTrue(spans.MoveNext(), "next");
            Assert.AreEqual(doc, spans.Doc, "doc");
            Assert.AreEqual(start, spans.Start, "start");
            Assert.AreEqual(end, spans.End, "end");
        }

        [Test]
        public virtual void TestSpanOrEmpty()
        {
            Spans spans = OrSpans(new string[0]);
            Assert.IsFalse(spans.MoveNext(), "empty next");

            SpanOrQuery a = new SpanOrQuery();
            SpanOrQuery b = new SpanOrQuery();
            Assert.IsTrue(a.Equals(b), "empty should equal");
        }

        [Test]
        public virtual void TestSpanOrSingle()
        {
            Spans spans = OrSpans(new string[] { "w5" });
            TstNextSpans(spans, 0, 4, 5);
            Assert.IsFalse(spans.MoveNext(), "final next");
        }

        [Test]
        public virtual void TestSpanOrMovesForward()
        {
            Spans spans = OrSpans(new string[] { "w1", "xx" });

            spans.MoveNext();
            int doc = spans.Doc;
            Assert.AreEqual(0, doc);

            spans.SkipTo(0);
            doc = spans.Doc;

            // LUCENE-1583:
            // according to Spans, a skipTo to the same doc or less
            // should still call next() on the underlying Spans
            Assert.AreEqual(1, doc);
        }

        [Test]
        public virtual void TestSpanOrDouble()
        {
            Spans spans = OrSpans(new string[] { "w5", "yy" });
            TstNextSpans(spans, 0, 4, 5);
            TstNextSpans(spans, 2, 3, 4);
            TstNextSpans(spans, 3, 4, 5);
            TstNextSpans(spans, 7, 3, 4);
            Assert.IsFalse(spans.MoveNext(), "final next");
        }

        [Test]
        public virtual void TestSpanOrDoubleSkip()
        {
            Spans spans = OrSpans(new string[] { "w5", "yy" });
            Assert.IsTrue(spans.SkipTo(3), "initial skipTo");
            Assert.AreEqual(3, spans.Doc, "doc");
            Assert.AreEqual(4, spans.Start, "start");
            Assert.AreEqual(5, spans.End, "end");
            TstNextSpans(spans, 7, 3, 4);
            Assert.IsFalse(spans.MoveNext(), "final next");
        }

        [Test]
        public virtual void TestSpanOrUnused()
        {
            Spans spans = OrSpans(new string[] { "w5", "unusedTerm", "yy" });
            TstNextSpans(spans, 0, 4, 5);
            TstNextSpans(spans, 2, 3, 4);
            TstNextSpans(spans, 3, 4, 5);
            TstNextSpans(spans, 7, 3, 4);
            Assert.IsFalse(spans.MoveNext(), "final next");
        }

        [Test]
        public virtual void TestSpanOrTripleSameDoc()
        {
            Spans spans = OrSpans(new string[] { "t1", "t2", "t3" });
            TstNextSpans(spans, 11, 0, 1);
            TstNextSpans(spans, 11, 1, 2);
            TstNextSpans(spans, 11, 2, 3);
            TstNextSpans(spans, 11, 3, 4);
            TstNextSpans(spans, 11, 4, 5);
            TstNextSpans(spans, 11, 5, 6);
            Assert.IsFalse(spans.MoveNext(), "final next");
        }

        [Test]
        public virtual void TestSpanScorerZeroSloppyFreq()
        {
            bool ordered = true;
            int slop = 1;
            IndexReaderContext topReaderContext = searcher.TopReaderContext;
            IList<AtomicReaderContext> leaves = topReaderContext.Leaves;
            int subIndex = ReaderUtil.SubIndex(11, leaves);
            for (int i = 0, c = leaves.Count; i < c; i++)
            {
                AtomicReaderContext ctx = leaves[i];

                Similarity sim = new DefaultSimilarityAnonymousClass(this);

                Similarity oldSim = searcher.Similarity;
                Scorer spanScorer;
                try
                {
                    searcher.Similarity = sim;
                    SpanNearQuery snq = new SpanNearQuery(new SpanQuery[] { MakeSpanTermQuery("t1"), MakeSpanTermQuery("t2") }, slop, ordered);

                    spanScorer = searcher.CreateNormalizedWeight(snq).GetScorer(ctx, ((AtomicReader)ctx.Reader).LiveDocs);
                }
                finally
                {
                    searcher.Similarity = oldSim;
                }
                if (i == subIndex)
                {
                    Assert.IsTrue(spanScorer.NextDoc() != DocIdSetIterator.NO_MORE_DOCS, "first doc");
                    Assert.AreEqual(spanScorer.DocID + ctx.DocBase, 11, "first doc number");
                    float score = spanScorer.GetScore();
                    Assert.IsTrue(score == 0.0f, "first doc score should be zero, " + score);
                }
                else
                {
                    Assert.IsTrue(spanScorer.NextDoc() == DocIdSetIterator.NO_MORE_DOCS, "no second doc");
                }
            }
        }

        private sealed class DefaultSimilarityAnonymousClass : DefaultSimilarity
        {
            private readonly TestSpans outerInstance;

            public DefaultSimilarityAnonymousClass(TestSpans outerInstance)
            {
                this.outerInstance = outerInstance;
            }

            public override float SloppyFreq(int distance)
            {
                return 0.0f;
            }
        }

        // LUCENE-1404
        private void AddDoc(IndexWriter writer, string id, string text)
        {
            Document doc = new Document();
            doc.Add(NewStringField("id", id, Field.Store.YES));
            doc.Add(NewTextField("text", text, Field.Store.YES));
            writer.AddDocument(doc);
        }

        // LUCENE-1404
        private int HitCount(IndexSearcher searcher, string word)
        {
            return searcher.Search(new TermQuery(new Term("text", word)), 10).TotalHits;
        }

        // LUCENE-1404
        private SpanQuery CreateSpan(string value)
        {
            return new SpanTermQuery(new Term("text", value));
        }

        // LUCENE-1404
        private SpanQuery CreateSpan(int slop, bool ordered, SpanQuery[] clauses)
        {
            return new SpanNearQuery(clauses, slop, ordered);
        }

        // LUCENE-1404
        private SpanQuery CreateSpan(int slop, bool ordered, string term1, string term2)
        {
            return CreateSpan(slop, ordered, new SpanQuery[] { CreateSpan(term1), CreateSpan(term2) });
        }

        // LUCENE-1404
        [Test]
        public virtual void TestNPESpanQuery()
        {
            Directory dir = NewDirectory();
            IndexWriter writer = new IndexWriter(dir, new IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)));

            // Add documents
            AddDoc(writer, "1", "the big dogs went running to the market");
            AddDoc(writer, "2", "the cat chased the mouse, then the cat ate the mouse quickly");

            // Commit
            writer.Dispose();

            // Get searcher
            IndexReader reader = DirectoryReader.Open(dir);
            IndexSearcher searcher = NewSearcher(reader);

            // Control (make sure docs indexed)
            Assert.AreEqual(2, HitCount(searcher, "the"));
            Assert.AreEqual(1, HitCount(searcher, "cat"));
            Assert.AreEqual(1, HitCount(searcher, "dogs"));
            Assert.AreEqual(0, HitCount(searcher, "rabbit"));

            // this throws exception (it shouldn't)
            Assert.AreEqual(1, searcher.Search(CreateSpan(0, true, new SpanQuery[] { CreateSpan(4, false, "chased", "cat"), CreateSpan("ate") }), 10).TotalHits);
            reader.Dispose();
            dir.Dispose();
        }

        [Test]
        public virtual void TestSpanNots()
        {
            Assert.AreEqual(0, SpanCount("s2", "s2", 0, 0), 0, "SpanNotIncludeExcludeSame1");
            Assert.AreEqual(0, SpanCount("s2", "s2", 10, 10), 0, "SpanNotIncludeExcludeSame2");

            //focus on behind
            Assert.AreEqual(1, SpanCount("s2", "s1", 6, 0), "SpanNotS2NotS1_6_0");
            Assert.AreEqual(2, SpanCount("s2", "s1", 5, 0), "SpanNotS2NotS1_5_0");
            Assert.AreEqual(3, SpanCount("s2", "s1", 3, 0), "SpanNotS2NotS1_3_0");
            Assert.AreEqual(4, SpanCount("s2", "s1", 2, 0), "SpanNotS2NotS1_2_0");
            Assert.AreEqual(4, SpanCount("s2", "s1", 0, 0), "SpanNotS2NotS1_0_0");

            //focus on both
            Assert.AreEqual(2, SpanCount("s2", "s1", 3, 1), "SpanNotS2NotS1_3_1");
            Assert.AreEqual(3, SpanCount("s2", "s1", 2, 1), "SpanNotS2NotS1_2_1");
            Assert.AreEqual(3, SpanCount("s2", "s1", 1, 1), "SpanNotS2NotS1_1_1");
            Assert.AreEqual(0, SpanCount("s2", "s1", 10, 10), "SpanNotS2NotS1_10_10");

            //focus on ahead
            Assert.AreEqual(0, SpanCount("s1", "s2", 10, 10), "SpanNotS1NotS2_10_10");
            Assert.AreEqual(3, SpanCount("s1", "s2", 0, 1), "SpanNotS1NotS2_0_1");
            Assert.AreEqual(3, SpanCount("s1", "s2", 0, 2), "SpanNotS1NotS2_0_2");
            Assert.AreEqual(2, SpanCount("s1", "s2", 0, 3), "SpanNotS1NotS2_0_3");
            Assert.AreEqual(1, SpanCount("s1", "s2", 0, 4), "SpanNotS1NotS2_0_4");
            Assert.AreEqual(0, SpanCount("s1", "s2", 0, 8), "SpanNotS1NotS2_0_8");

            //exclude doesn't exist
            Assert.AreEqual(3, SpanCount("s1", "s3", 8, 8), "SpanNotS1NotS3_8_8");

            //include doesn't exist
            Assert.AreEqual(0, SpanCount("s3", "s1", 8, 8), "SpanNotS3NotS1_8_8");
        }

        [Test]
        [Description("LUCENENET-597")]
        public void TestToString_LUCENENET_597()
        {
            var clauses = new[]
            {
                new SpanTermQuery(new Term("f", "lucene")),
                new SpanTermQuery(new Term("f", "net")),
                new SpanTermQuery(new Term("f", "solr"))
            };
            var query = new SpanNearQuery(clauses, 0, true);
            var queryString = query.ToString();

            Assert.AreEqual("SpanNear([f:lucene, f:net, f:solr], 0, True)", queryString);
        }

        private int SpanCount(string include, string exclude, int pre, int post)
        {
            SpanTermQuery iq = new SpanTermQuery(new Term(field, include));
            SpanTermQuery eq = new SpanTermQuery(new Term(field, exclude));
            SpanNotQuery snq = new SpanNotQuery(iq, eq, pre, post);
            Spans spans = MultiSpansWrapper.Wrap(searcher.TopReaderContext, snq);

            int i = 0;
            while (spans.MoveNext())
            {
                i++;
            }
            return i;
        }
    }
}