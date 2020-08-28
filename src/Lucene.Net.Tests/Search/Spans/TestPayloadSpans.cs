using Lucene.Net.Analysis;
using Lucene.Net.Analysis.TokenAttributes;
using Lucene.Net.Index.Extensions;
using System;
using System.Collections.Generic;
using System.Text;
using Lucene.Net.Documents;
using NUnit.Framework;
using System.IO;
using JCG = J2N.Collections.Generic;
using Assert = Lucene.Net.TestFramework.Assert;
using Console = Lucene.Net.Util.SystemConsole;

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

    using Analyzer = Lucene.Net.Analysis.Analyzer;
    using BytesRef = Lucene.Net.Util.BytesRef;
    using DefaultSimilarity = Lucene.Net.Search.Similarities.DefaultSimilarity;
    using Directory = Lucene.Net.Store.Directory;
    using Document = Documents.Document;
    using Field = Field;
    using IndexReader = Lucene.Net.Index.IndexReader;
    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
    using MockTokenizer = Lucene.Net.Analysis.MockTokenizer;
    using PayloadHelper = Lucene.Net.Search.Payloads.PayloadHelper;
    using PayloadSpanUtil = Lucene.Net.Search.Payloads.PayloadSpanUtil;
    using RandomIndexWriter = Lucene.Net.Index.RandomIndexWriter;
    using Similarity = Lucene.Net.Search.Similarities.Similarity;
    using Term = Lucene.Net.Index.Term;
    using TextField = TextField;
    using TokenFilter = Lucene.Net.Analysis.TokenFilter;
    using Tokenizer = Lucene.Net.Analysis.Tokenizer;
    using TokenStream = Lucene.Net.Analysis.TokenStream;

    [TestFixture]
    public class TestPayloadSpans : LuceneTestCase
    {
        private IndexSearcher searcher;
        private Similarity similarity = new DefaultSimilarity();
        protected internal IndexReader indexReader;
        private IndexReader closeIndexReader;
        private Directory directory;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            PayloadHelper helper = new PayloadHelper();
            searcher = helper.SetUp(Random, similarity, 1000);
            indexReader = searcher.IndexReader;
        }

        [Test]
        public virtual void TestSpanTermQuery()
        {
            SpanTermQuery stq;
            Spans spans;
            stq = new SpanTermQuery(new Term(PayloadHelper.FIELD, "seventy"));
            spans = MultiSpansWrapper.Wrap(indexReader.Context, stq);
            Assert.IsTrue(spans != null, "spans is null and it shouldn't be");
            CheckSpans(spans, 100, 1, 1, 1);

            stq = new SpanTermQuery(new Term(PayloadHelper.NO_PAYLOAD_FIELD, "seventy"));
            spans = MultiSpansWrapper.Wrap(indexReader.Context, stq);
            Assert.IsTrue(spans != null, "spans is null and it shouldn't be");
            CheckSpans(spans, 100, 0, 0, 0);
        }

        [Test]
        public virtual void TestSpanFirst()
        {
            SpanQuery match;
            SpanFirstQuery sfq;
            match = new SpanTermQuery(new Term(PayloadHelper.FIELD, "one"));
            sfq = new SpanFirstQuery(match, 2);
            Spans spans = MultiSpansWrapper.Wrap(indexReader.Context, sfq);
            CheckSpans(spans, 109, 1, 1, 1);
            //Test more complicated subclause
            SpanQuery[] clauses = new SpanQuery[2];
            clauses[0] = new SpanTermQuery(new Term(PayloadHelper.FIELD, "one"));
            clauses[1] = new SpanTermQuery(new Term(PayloadHelper.FIELD, "hundred"));
            match = new SpanNearQuery(clauses, 0, true);
            sfq = new SpanFirstQuery(match, 2);
            CheckSpans(MultiSpansWrapper.Wrap(indexReader.Context, sfq), 100, 2, 1, 1);

            match = new SpanNearQuery(clauses, 0, false);
            sfq = new SpanFirstQuery(match, 2);
            CheckSpans(MultiSpansWrapper.Wrap(indexReader.Context, sfq), 100, 2, 1, 1);
        }

        [Test]
        public virtual void TestSpanNot()
        {
            SpanQuery[] clauses = new SpanQuery[2];
            clauses[0] = new SpanTermQuery(new Term(PayloadHelper.FIELD, "one"));
            clauses[1] = new SpanTermQuery(new Term(PayloadHelper.FIELD, "three"));
            SpanQuery spq = new SpanNearQuery(clauses, 5, true);
            SpanNotQuery snq = new SpanNotQuery(spq, new SpanTermQuery(new Term(PayloadHelper.FIELD, "two")));

            Directory directory = NewDirectory();
            RandomIndexWriter writer = new RandomIndexWriter(Random, directory, NewIndexWriterConfig(TEST_VERSION_CURRENT, new PayloadAnalyzer(this)).SetSimilarity(similarity));

            Document doc = new Document();
            doc.Add(NewTextField(PayloadHelper.FIELD, "one two three one four three", Field.Store.YES));
            writer.AddDocument(doc);
            IndexReader reader = writer.GetReader();
            writer.Dispose();

            CheckSpans(MultiSpansWrapper.Wrap(reader.Context, snq), 1, new int[] { 2 });
            reader.Dispose();
            directory.Dispose();
        }

        [Test]
        public virtual void TestNestedSpans()
        {
            SpanTermQuery stq;
            Spans spans;
            IndexSearcher searcher = Searcher;
            stq = new SpanTermQuery(new Term(PayloadHelper.FIELD, "mark"));
            spans = MultiSpansWrapper.Wrap(searcher.TopReaderContext, stq);
            Assert.IsTrue(spans != null, "spans is null and it shouldn't be");
            CheckSpans(spans, 0, null);

            SpanQuery[] clauses = new SpanQuery[3];
            clauses[0] = new SpanTermQuery(new Term(PayloadHelper.FIELD, "rr"));
            clauses[1] = new SpanTermQuery(new Term(PayloadHelper.FIELD, "yy"));
            clauses[2] = new SpanTermQuery(new Term(PayloadHelper.FIELD, "xx"));
            SpanNearQuery spanNearQuery = new SpanNearQuery(clauses, 12, false);

            spans = MultiSpansWrapper.Wrap(searcher.TopReaderContext, spanNearQuery);
            Assert.IsTrue(spans != null, "spans is null and it shouldn't be");
            CheckSpans(spans, 2, new int[] { 3, 3 });

            clauses[0] = new SpanTermQuery(new Term(PayloadHelper.FIELD, "xx"));
            clauses[1] = new SpanTermQuery(new Term(PayloadHelper.FIELD, "rr"));
            clauses[2] = new SpanTermQuery(new Term(PayloadHelper.FIELD, "yy"));

            spanNearQuery = new SpanNearQuery(clauses, 6, true);

            spans = MultiSpansWrapper.Wrap(searcher.TopReaderContext, spanNearQuery);

            Assert.IsTrue(spans != null, "spans is null and it shouldn't be");
            CheckSpans(spans, 1, new int[] { 3 });

            clauses = new SpanQuery[2];

            clauses[0] = new SpanTermQuery(new Term(PayloadHelper.FIELD, "xx"));
            clauses[1] = new SpanTermQuery(new Term(PayloadHelper.FIELD, "rr"));

            spanNearQuery = new SpanNearQuery(clauses, 6, true);

            // xx within 6 of rr

            SpanQuery[] clauses2 = new SpanQuery[2];

            clauses2[0] = new SpanTermQuery(new Term(PayloadHelper.FIELD, "yy"));
            clauses2[1] = spanNearQuery;

            SpanNearQuery nestedSpanNearQuery = new SpanNearQuery(clauses2, 6, false);

            // yy within 6 of xx within 6 of rr

            spans = MultiSpansWrapper.Wrap(searcher.TopReaderContext, nestedSpanNearQuery);
            Assert.IsTrue(spans != null, "spans is null and it shouldn't be");
            CheckSpans(spans, 2, new int[] { 3, 3 });
            closeIndexReader.Dispose();
            directory.Dispose();
        }

        [Test]
        public virtual void TestFirstClauseWithoutPayload()
        {
            Spans spans;
            IndexSearcher searcher = Searcher;

            SpanQuery[] clauses = new SpanQuery[3];
            clauses[0] = new SpanTermQuery(new Term(PayloadHelper.FIELD, "nopayload"));
            clauses[1] = new SpanTermQuery(new Term(PayloadHelper.FIELD, "qq"));
            clauses[2] = new SpanTermQuery(new Term(PayloadHelper.FIELD, "ss"));

            SpanNearQuery spanNearQuery = new SpanNearQuery(clauses, 6, true);

            SpanQuery[] clauses2 = new SpanQuery[2];

            clauses2[0] = new SpanTermQuery(new Term(PayloadHelper.FIELD, "pp"));
            clauses2[1] = spanNearQuery;

            SpanNearQuery snq = new SpanNearQuery(clauses2, 6, false);

            SpanQuery[] clauses3 = new SpanQuery[2];

            clauses3[0] = new SpanTermQuery(new Term(PayloadHelper.FIELD, "np"));
            clauses3[1] = snq;

            SpanNearQuery nestedSpanNearQuery = new SpanNearQuery(clauses3, 6, false);
            spans = MultiSpansWrapper.Wrap(searcher.TopReaderContext, nestedSpanNearQuery);

            Assert.IsTrue(spans != null, "spans is null and it shouldn't be");
            CheckSpans(spans, 1, new int[] { 3 });
            closeIndexReader.Dispose();
            directory.Dispose();
        }

        [Test]
        public virtual void TestHeavilyNestedSpanQuery()
        {
            Spans spans;
            IndexSearcher searcher = Searcher;

            SpanQuery[] clauses = new SpanQuery[3];
            clauses[0] = new SpanTermQuery(new Term(PayloadHelper.FIELD, "one"));
            clauses[1] = new SpanTermQuery(new Term(PayloadHelper.FIELD, "two"));
            clauses[2] = new SpanTermQuery(new Term(PayloadHelper.FIELD, "three"));

            SpanNearQuery spanNearQuery = new SpanNearQuery(clauses, 5, true);

            clauses = new SpanQuery[3];
            clauses[0] = spanNearQuery;
            clauses[1] = new SpanTermQuery(new Term(PayloadHelper.FIELD, "five"));
            clauses[2] = new SpanTermQuery(new Term(PayloadHelper.FIELD, "six"));

            SpanNearQuery spanNearQuery2 = new SpanNearQuery(clauses, 6, true);

            SpanQuery[] clauses2 = new SpanQuery[2];
            clauses2[0] = new SpanTermQuery(new Term(PayloadHelper.FIELD, "eleven"));
            clauses2[1] = new SpanTermQuery(new Term(PayloadHelper.FIELD, "ten"));
            SpanNearQuery spanNearQuery3 = new SpanNearQuery(clauses2, 2, false);

            SpanQuery[] clauses3 = new SpanQuery[3];
            clauses3[0] = new SpanTermQuery(new Term(PayloadHelper.FIELD, "nine"));
            clauses3[1] = spanNearQuery2;
            clauses3[2] = spanNearQuery3;

            SpanNearQuery nestedSpanNearQuery = new SpanNearQuery(clauses3, 6, false);

            spans = MultiSpansWrapper.Wrap(searcher.TopReaderContext, nestedSpanNearQuery);
            Assert.IsTrue(spans != null, "spans is null and it shouldn't be");
            CheckSpans(spans, 2, new int[] { 8, 8 });
            closeIndexReader.Dispose();
            directory.Dispose();
        }

        [Test]
        public virtual void TestShrinkToAfterShortestMatch()
        {
            Directory directory = NewDirectory();
            RandomIndexWriter writer = new RandomIndexWriter(Random, directory, NewIndexWriterConfig(TEST_VERSION_CURRENT, new TestPayloadAnalyzer(this)));

            Document doc = new Document();
            doc.Add(new TextField("content", new StringReader("a b c d e f g h i j a k")));
            writer.AddDocument(doc);

            IndexReader reader = writer.GetReader();
            IndexSearcher @is = NewSearcher(reader);
            writer.Dispose();

            SpanTermQuery stq1 = new SpanTermQuery(new Term("content", "a"));
            SpanTermQuery stq2 = new SpanTermQuery(new Term("content", "k"));
            SpanQuery[] sqs = new SpanQuery[] { stq1, stq2 };
            SpanNearQuery snq = new SpanNearQuery(sqs, 1, true);
            Spans spans = MultiSpansWrapper.Wrap(@is.TopReaderContext, snq);

            TopDocs topDocs = @is.Search(snq, 1);
            ISet<string> payloadSet = new JCG.HashSet<string>();
            for (int i = 0; i < topDocs.ScoreDocs.Length; i++)
            {
                while (spans.MoveNext())
                {
                    var payloads = spans.GetPayload();
                    foreach (var payload in payloads)
                    {
                        payloadSet.Add(Encoding.UTF8.GetString(payload));
                    }
                }
            }
            Assert.AreEqual(2, payloadSet.Count);
            Assert.IsTrue(payloadSet.Contains("a:Noise:10"));
            Assert.IsTrue(payloadSet.Contains("k:Noise:11"));
            reader.Dispose();
            directory.Dispose();
        }

        [Test]
        public virtual void TestShrinkToAfterShortestMatch2()
        {
            Directory directory = NewDirectory();
            RandomIndexWriter writer = new RandomIndexWriter(Random, directory, NewIndexWriterConfig(TEST_VERSION_CURRENT, new TestPayloadAnalyzer(this)));

            Document doc = new Document();
            doc.Add(new TextField("content", new StringReader("a b a d k f a h i k a k")));
            writer.AddDocument(doc);
            IndexReader reader = writer.GetReader();
            IndexSearcher @is = NewSearcher(reader);
            writer.Dispose();

            SpanTermQuery stq1 = new SpanTermQuery(new Term("content", "a"));
            SpanTermQuery stq2 = new SpanTermQuery(new Term("content", "k"));
            SpanQuery[] sqs = { stq1, stq2 };
            SpanNearQuery snq = new SpanNearQuery(sqs, 0, true);
            Spans spans = MultiSpansWrapper.Wrap(@is.TopReaderContext, snq);

            TopDocs topDocs = @is.Search(snq, 1);
            ISet<string> payloadSet = new JCG.HashSet<string>();
            for (int i = 0; i < topDocs.ScoreDocs.Length; i++)
            {
                while (spans.MoveNext())
                {
                    var payloads = spans.GetPayload();
                    foreach (var payload in payloads)
                    {
                        payloadSet.Add(Encoding.UTF8.GetString(payload));
                    }
                }
            }
            Assert.AreEqual(2, payloadSet.Count);
            Assert.IsTrue(payloadSet.Contains("a:Noise:10"));
            Assert.IsTrue(payloadSet.Contains("k:Noise:11"));
            reader.Dispose();
            directory.Dispose();
        }

        [Test]
        public virtual void TestShrinkToAfterShortestMatch3()
        {
            Directory directory = NewDirectory();
            RandomIndexWriter writer = new RandomIndexWriter(Random, directory, NewIndexWriterConfig(TEST_VERSION_CURRENT, new TestPayloadAnalyzer(this)));

            Document doc = new Document();
            doc.Add(new TextField("content", new StringReader("j k a l f k k p a t a k l k t a")));
            writer.AddDocument(doc);
            IndexReader reader = writer.GetReader();
            IndexSearcher @is = NewSearcher(reader);
            writer.Dispose();

            SpanTermQuery stq1 = new SpanTermQuery(new Term("content", "a"));
            SpanTermQuery stq2 = new SpanTermQuery(new Term("content", "k"));
            SpanQuery[] sqs = new SpanQuery[] { stq1, stq2 };
            SpanNearQuery snq = new SpanNearQuery(sqs, 0, true);
            Spans spans = MultiSpansWrapper.Wrap(@is.TopReaderContext, snq);

            TopDocs topDocs = @is.Search(snq, 1);
            ISet<string> payloadSet = new JCG.HashSet<string>();
            for (int i = 0; i < topDocs.ScoreDocs.Length; i++)
            {
                while (spans.MoveNext())
                {
                    var payloads = spans.GetPayload();
                    foreach (var payload in payloads)
                    {
                        payloadSet.Add(Encoding.UTF8.GetString(payload));
                    }
                }
            }
            Assert.AreEqual(2, payloadSet.Count);
            if (Verbose)
            {
                foreach (String payload in payloadSet)
                {
                    Console.WriteLine("match:" + payload);
                }
            }
            Assert.IsTrue(payloadSet.Contains("a:Noise:10"));
            Assert.IsTrue(payloadSet.Contains("k:Noise:11"));
            reader.Dispose();
            directory.Dispose();
        }

        [Test]
        public virtual void TestPayloadSpanUtil()
        {
            Directory directory = NewDirectory();
            RandomIndexWriter writer = new RandomIndexWriter(Random, directory, NewIndexWriterConfig(TEST_VERSION_CURRENT, new PayloadAnalyzer(this)).SetSimilarity(similarity));

            Document doc = new Document();
            doc.Add(NewTextField(PayloadHelper.FIELD, "xx rr yy mm  pp", Field.Store.YES));
            writer.AddDocument(doc);

            IndexReader reader = writer.GetReader();
            writer.Dispose();
            IndexSearcher searcher = NewSearcher(reader);

            PayloadSpanUtil psu = new PayloadSpanUtil(searcher.TopReaderContext);

            var payloads = psu.GetPayloadsForQuery(new TermQuery(new Term(PayloadHelper.FIELD, "rr")));
            if (Verbose)
            {
                Console.WriteLine("Num payloads:" + payloads.Count);
                foreach (var bytes in payloads)
                {
                    Console.WriteLine(Encoding.UTF8.GetString(bytes));
                }
            }
            reader.Dispose();
            directory.Dispose();
        }

        private void CheckSpans(Spans spans, int expectedNumSpans, int expectedNumPayloads, int expectedPayloadLength, int expectedFirstByte)
        {
            Assert.IsTrue(spans != null, "spans is null and it shouldn't be");
            //each position match should have a span associated with it, since there is just one underlying term query, there should
            //only be one entry in the span
            int seen = 0;
            while (spans.MoveNext() == true)
            {
                //if we expect payloads, then isPayloadAvailable should be true
                if (expectedNumPayloads > 0)
                {
                    Assert.IsTrue(spans.IsPayloadAvailable == true, "isPayloadAvailable is not returning the correct value: " + spans.IsPayloadAvailable + " and it should be: " + (expectedNumPayloads > 0));
                }
                else
                {
                    Assert.IsTrue(spans.IsPayloadAvailable == false, "isPayloadAvailable should be false");
                }
                //See payload helper, for the PayloadHelper.FIELD field, there is a single byte payload at every token
                if (spans.IsPayloadAvailable)
                {
                    var payload = spans.GetPayload();
                    Assert.IsTrue(payload.Count == expectedNumPayloads, "payload Size: " + payload.Count + " is not: " + expectedNumPayloads);
                    foreach (var thePayload in payload)
                    {
                        Assert.IsTrue(thePayload.Length == expectedPayloadLength, "payload[0] Size: " + thePayload.Length + " is not: " + expectedPayloadLength);
                        Assert.IsTrue(thePayload[0] == expectedFirstByte, thePayload[0] + " does not equal: " + expectedFirstByte);
                    }
                }
                seen++;
            }
            Assert.IsTrue(seen == expectedNumSpans, seen + " does not equal: " + expectedNumSpans);
        }

        private IndexSearcher Searcher
        {
            get
            {
                directory = NewDirectory();
                string[] docs = new string[] { "xx rr yy mm  pp", "xx yy mm rr pp", "nopayload qq ss pp np", "one two three four five six seven eight nine ten eleven", "nine one two three four five six seven eight eleven ten" };
                RandomIndexWriter writer = new RandomIndexWriter(Random, directory, NewIndexWriterConfig(TEST_VERSION_CURRENT, new PayloadAnalyzer(this)).SetSimilarity(similarity));

                Document doc = null;
                for (int i = 0; i < docs.Length; i++)
                {
                    doc = new Document();
                    string docText = docs[i];
                    doc.Add(NewTextField(PayloadHelper.FIELD, docText, Field.Store.YES));
                    writer.AddDocument(doc);
                }

                closeIndexReader = writer.GetReader();
                writer.Dispose();

                IndexSearcher searcher = NewSearcher(closeIndexReader);
                return searcher;
            }
        }

        private void CheckSpans(Spans spans, int numSpans, int[] numPayloads)
        {
            int cnt = 0;

            while (spans.MoveNext() == true)
            {
                if (Verbose)
                {
                    Console.WriteLine("\nSpans Dump --");
                }
                if (spans.IsPayloadAvailable)
                {
                    var payload = spans.GetPayload();
                    if (Verbose)
                    {
                        Console.WriteLine("payloads for span:" + payload.Count);
                        foreach (var bytes in payload)
                        {
                            Console.WriteLine("doc:" + spans.Doc + " s:" + spans.Start + " e:" + spans.End + " " + Encoding.UTF8.GetString(bytes));
                        }
                    }

                    Assert.AreEqual(numPayloads[cnt], payload.Count);
                }
                else
                {
                    Assert.IsFalse(numPayloads.Length > 0 && numPayloads[cnt] > 0, "Expected spans:" + numPayloads[cnt] + " found: 0");
                }
                cnt++;
            }

            Assert.AreEqual(numSpans, cnt);
        }

        internal sealed class PayloadAnalyzer : Analyzer
        {
            private readonly TestPayloadSpans outerInstance;

            public PayloadAnalyzer(TestPayloadSpans outerInstance)
            {
                this.outerInstance = outerInstance;
            }

            protected internal override TokenStreamComponents CreateComponents(string fieldName, TextReader reader)
            {
                Tokenizer result = new MockTokenizer(reader, MockTokenizer.SIMPLE, true);
                return new TokenStreamComponents(result, new PayloadFilter(outerInstance, result));
            }
        }

        internal sealed class PayloadFilter : TokenFilter
        {
            private readonly TestPayloadSpans outerInstance;

            internal ISet<string> entities = new JCG.HashSet<string>();
            internal ISet<string> nopayload = new JCG.HashSet<string>();
            internal int pos;
            internal IPayloadAttribute payloadAtt;
            internal ICharTermAttribute termAtt;
            internal IPositionIncrementAttribute posIncrAtt;

            public PayloadFilter(TestPayloadSpans outerInstance, TokenStream input)
                : base(input)
            {
                this.outerInstance = outerInstance;
                pos = 0;
                entities.Add("xx");
                entities.Add("one");
                nopayload.Add("nopayload");
                nopayload.Add("np");
                termAtt = AddAttribute<ICharTermAttribute>();
                posIncrAtt = AddAttribute<IPositionIncrementAttribute>();
                payloadAtt = AddAttribute<IPayloadAttribute>();
            }

            public override bool IncrementToken()
            {
                if (m_input.IncrementToken())
                {
                    string token = termAtt.ToString();

                    if (!nopayload.Contains(token))
                    {
                        if (entities.Contains(token))
                        {
                            payloadAtt.Payload = new BytesRef(token + ":Entity:" + pos);
                        }
                        else
                        {
                            payloadAtt.Payload = new BytesRef(token + ":Noise:" + pos);
                        }
                    }
                    pos += posIncrAtt.PositionIncrement;
                    return true;
                }
                return false;
            }

            public override void Reset()
            {
                base.Reset();
                this.pos = 0;
            }
        }

        public sealed class TestPayloadAnalyzer : Analyzer
        {
            private readonly TestPayloadSpans outerInstance;

            public TestPayloadAnalyzer(TestPayloadSpans outerInstance)
            {
                this.outerInstance = outerInstance;
            }

            protected internal override TokenStreamComponents CreateComponents(string fieldName, TextReader reader)
            {
                Tokenizer result = new MockTokenizer(reader, MockTokenizer.SIMPLE, true);
                return new TokenStreamComponents(result, new PayloadFilter(outerInstance, result));
            }
        }
    }
}