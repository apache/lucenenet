/**
 * Copyright 2004 The Apache Software Foundation
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using NUnit.Framework;

using Analyzer = Lucene.Net.Analysis.Analyzer;
using LowerCaseTokenizer = Lucene.Net.Analysis.LowerCaseTokenizer;
using Token = Lucene.Net.Analysis.Token;
using TokenFilter = Lucene.Net.Analysis.TokenFilter;
using TokenStream = Lucene.Net.Analysis.TokenStream;
using Document = Lucene.Net.Documents.Document;
using Field = Lucene.Net.Documents.Field;
using IndexReader = Lucene.Net.Index.IndexReader;
using IndexWriter = Lucene.Net.Index.IndexWriter;
using Payload = Lucene.Net.Index.Payload;
using Term = Lucene.Net.Index.Term;
using DefaultSimilarity = Lucene.Net.Search.DefaultSimilarity;
using IndexSearcher = Lucene.Net.Search.IndexSearcher;
using Similarity = Lucene.Net.Search.Similarity;
using TermQuery = Lucene.Net.Search.TermQuery;
using PayloadHelper = Lucene.Net.Search.Payloads.PayloadHelper;
using PayloadSpanUtil = Lucene.Net.Search.Payloads.PayloadSpanUtil;
using RAMDirectory = Lucene.Net.Store.RAMDirectory;

namespace Lucene.Net.Search.Spans
{
    [TestFixture]
    public class TestPayloadSpans
    {
        private readonly static bool DEBUG = false;
        private IndexSearcher searcher;
        private Similarity similarity = new DefaultSimilarity();
        protected IndexReader indexReader;

        [SetUp]
        protected void SetUp()
        {
            PayloadHelper helper = new PayloadHelper();
            searcher = helper.SetUp(similarity, 1000);
            indexReader = searcher.GetIndexReader();
        }

        [TearDown]
        protected void TearDown()
        {

        }

        [Test]
        public void TestSpanTermQuery()
        {
            SpanTermQuery stq;
            PayloadSpans spans;
            stq = new SpanTermQuery(new Term(PayloadHelper.FIELD, "seventy"));
            spans = stq.GetPayloadSpans(indexReader);
            Assert.IsTrue(spans != null, "spans is null and it shouldn't be");
            CheckSpans(spans, 100, 1, 1, 1);

            stq = new SpanTermQuery(new Term(PayloadHelper.NO_PAYLOAD_FIELD, "seventy"));
            spans = stq.GetPayloadSpans(indexReader);
            Assert.IsTrue(spans != null, "spans is null and it shouldn't be");
            CheckSpans(spans, 100, 0, 0, 0);
        }
        [Test]
        public void TestSpanFirst()
        {

            SpanQuery match;
            SpanFirstQuery sfq;
            match = new SpanTermQuery(new Term(PayloadHelper.FIELD, "one"));
            sfq = new SpanFirstQuery(match, 2);
            PayloadSpans spans = sfq.GetPayloadSpans(indexReader);
            CheckSpans(spans, 109, 1, 1, 1);
            //Test more complicated subclause
            SpanQuery[] clauses = new SpanQuery[2];
            clauses[0] = new SpanTermQuery(new Term(PayloadHelper.FIELD, "one"));
            clauses[1] = new SpanTermQuery(new Term(PayloadHelper.FIELD, "hundred"));
            match = new SpanNearQuery(clauses, 0, true);
            sfq = new SpanFirstQuery(match, 2);
            CheckSpans(sfq.GetPayloadSpans(indexReader), 100, 2, 1, 1);

            match = new SpanNearQuery(clauses, 0, false);
            sfq = new SpanFirstQuery(match, 2);
            CheckSpans(sfq.GetPayloadSpans(indexReader), 100, 2, 1, 1);

        }

        [Test]
        public void TestNestedSpans()
        {
            SpanTermQuery stq;
            PayloadSpans spans;
            IndexSearcher searcher = GetSearcher();
            stq = new SpanTermQuery(new Term(PayloadHelper.FIELD, "mark"));
            spans = stq.GetPayloadSpans(searcher.GetIndexReader());
            Assert.IsTrue(spans != null, "spans is null and it shouldn't be");
            CheckSpans(spans, 0, null);


            SpanQuery[] clauses = new SpanQuery[3];
            clauses[0] = new SpanTermQuery(new Term(PayloadHelper.FIELD, "rr"));
            clauses[1] = new SpanTermQuery(new Term(PayloadHelper.FIELD, "yy"));
            clauses[2] = new SpanTermQuery(new Term(PayloadHelper.FIELD, "xx"));
            SpanNearQuery spanNearQuery = new SpanNearQuery(clauses, 12, false);

            spans = spanNearQuery.GetPayloadSpans(searcher.GetIndexReader());
            Assert.IsTrue(spans != null, "spans is null and it shouldn't be");
            CheckSpans(spans, 2, new int[] { 3, 3 });


            clauses[0] = new SpanTermQuery(new Term(PayloadHelper.FIELD, "xx"));
            clauses[1] = new SpanTermQuery(new Term(PayloadHelper.FIELD, "rr"));
            clauses[2] = new SpanTermQuery(new Term(PayloadHelper.FIELD, "yy"));

            spanNearQuery = new SpanNearQuery(clauses, 6, true);


            spans = spanNearQuery.GetPayloadSpans(searcher.GetIndexReader());
            Assert.IsTrue(spans != null, "spans is null and it shouldn't be");
            CheckSpans(spans, 1, new int[] { 3 });

            clauses = new SpanQuery[2];

            clauses[0] = new SpanTermQuery(new Term(PayloadHelper.FIELD, "xx"));
            clauses[1] = new SpanTermQuery(new Term(PayloadHelper.FIELD, "rr"));

            spanNearQuery = new SpanNearQuery(clauses, 6, true);


            SpanQuery[] clauses2 = new SpanQuery[2];

            clauses2[0] = new SpanTermQuery(new Term(PayloadHelper.FIELD, "yy"));
            clauses2[1] = spanNearQuery;

            SpanNearQuery nestedSpanNearQuery = new SpanNearQuery(clauses2, 6, false);

            spans = nestedSpanNearQuery.GetPayloadSpans(searcher.GetIndexReader());
            Assert.IsTrue(spans != null, "spans is null and it shouldn't be");
            CheckSpans(spans, 2, new int[] { 3, 3 });
        }

        public void TestFirstClauseWithoutPayload()
        {
            PayloadSpans spans;
            IndexSearcher searcher = GetSearcher();

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

            spans = nestedSpanNearQuery.GetPayloadSpans(searcher.GetIndexReader());
            Assert.IsTrue(spans != null, "spans is null and it shouldn't be");
            CheckSpans(spans, 1, new int[] { 3 });
        }

        public void TestHeavilyNestedSpanQuery()
        {
            PayloadSpans spans;
            IndexSearcher searcher = GetSearcher();

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

            spans = nestedSpanNearQuery.GetPayloadSpans(searcher.GetIndexReader());
            Assert.IsTrue(spans != null, "spans is null and it shouldn't be");
            CheckSpans(spans, 2, new int[] { 8, 8 });
        }

        public void TestPayloadSpanUtil()
        {
            RAMDirectory directory = new RAMDirectory();
            PayloadAnalyzer analyzer = new PayloadAnalyzer();
            string[] docs = new string[] { };
            IndexWriter writer = new IndexWriter(directory, analyzer, true);
            writer.SetSimilarity(similarity);
            Document doc = new Document();
            doc.Add(new Field(PayloadHelper.FIELD, "xx rr yy mm  pp", Field.Store.YES, Field.Index.ANALYZED));
            writer.AddDocument(doc);

            writer.Close();

            IndexSearcher searcher = new IndexSearcher(directory);

            IndexReader reader = searcher.GetIndexReader();
            PayloadSpanUtil psu = new PayloadSpanUtil(reader);

            System.Collections.Generic.ICollection<byte[]> payloads = psu.GetPayloadsForQuery(new TermQuery(new Term(PayloadHelper.FIELD, "rr")));
            if (DEBUG)
                System.Console.WriteLine("Num payloads:" + payloads.Count);
            System.Collections.Generic.IEnumerator<byte[]> it = payloads.GetEnumerator();
            while (it.MoveNext())
            {
                byte[] bytes = it.Current;
                if (DEBUG)
                    System.Console.WriteLine(System.Text.Encoding.Default.GetString(bytes));
            }

        }

        private void CheckSpans(PayloadSpans spans, int expectedNumSpans, int expectedNumPayloads,
                                int expectedPayloadLength, int expectedFirstByte)
        {
            Assert.IsTrue(spans != null, "spans is null and it shouldn't be");
            //each position match should have a span associated with it, since there is just one underlying term query, there should
            //only be one entry in the span
            int seen = 0;
            while (spans.Next() == true)
            {
                //if we expect payloads, then isPayloadAvailable should be true
                if (expectedNumPayloads > 0)
                {
                    Assert.IsTrue(spans.IsPayloadAvailable() == true, "isPayloadAvailable is not returning the correct value: " + spans.IsPayloadAvailable()
                            + " and it should be: " + (expectedNumPayloads > 0));
                }
                else
                {
                    Assert.IsTrue(spans.IsPayloadAvailable() == false, "isPayloadAvailable should be false");
                }
                //See payload helper, for the PayloadHelper.FIELD field, there is a single byte payload at every token
                if (spans.IsPayloadAvailable())
                {
                    System.Collections.Generic.ICollection<byte[]> payload = spans.GetPayload();
                    Assert.IsTrue(payload.Count == expectedNumPayloads, "payload Size: " + payload.Count + " is not: " + expectedNumPayloads);
                    for (System.Collections.Generic.IEnumerator<byte[]> iterator = payload.GetEnumerator(); iterator.MoveNext(); )
                    {
                        byte[] thePayload = iterator.Current;
                        Assert.IsTrue(thePayload.Length == expectedPayloadLength, "payload[0] Size: " + thePayload.Length + " is not: " + expectedPayloadLength);
                        Assert.IsTrue(thePayload[0] == expectedFirstByte, thePayload[0] + " does not equal: " + expectedFirstByte);

                    }

                }
                seen++;
            }
            Assert.IsTrue(seen == expectedNumSpans, seen + " does not equal: " + expectedNumSpans);
        }

        private IndexSearcher GetSearcher()
        {
            RAMDirectory directory = new RAMDirectory();
            PayloadAnalyzer analyzer = new PayloadAnalyzer();
            string[] docs = new string[] { "xx rr yy mm  pp", "xx yy mm rr pp", "nopayload qq ss pp np", "one two three four five six seven eight nine ten eleven", "nine one two three four five six seven eight eleven ten" };
            IndexWriter writer = new IndexWriter(directory, analyzer, true);

            writer.SetSimilarity(similarity);

            Document doc = null;
            for (int i = 0; i < docs.Length; i++)
            {
                doc = new Document();
                string docText = docs[i];
                doc.Add(new Field(PayloadHelper.FIELD, docText, Field.Store.YES, Field.Index.ANALYZED));
                writer.AddDocument(doc);
            }

            writer.Close();

            IndexSearcher searcher = new IndexSearcher(directory);
            return searcher;
        }

        private void CheckSpans(PayloadSpans spans, int numSpans, int[] numPayloads)
        {
            int cnt = 0;

            while (spans.Next() == true)
            {
                if (DEBUG)
                    System.Console.WriteLine("\nSpans Dump --");
                if (spans.IsPayloadAvailable())
                {
                    System.Collections.Generic.ICollection<byte[]> payload = spans.GetPayload();
                    if (DEBUG)
                        System.Console.WriteLine("payloads for span:" + payload.Count);
                    System.Collections.Generic.IEnumerator<byte[]> it = payload.GetEnumerator();
                    while (it.MoveNext())
                    {
                        byte[] bytes = it.Current;
                        if (DEBUG)
                            System.Console.WriteLine("doc:" + spans.Doc() + " s:" + spans.Start() + " e:" + spans.End() + " "
                              + System.Text.Encoding.Default.GetString(bytes));
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

        class PayloadAnalyzer : Analyzer
        {
            override public TokenStream TokenStream(string fieldName, System.IO.TextReader reader)
            {
                TokenStream result = new LowerCaseTokenizer(reader);
                result = new PayloadFilter(result, fieldName);
                return result;
            }
        }

        class PayloadFilter : TokenFilter
        {
            string fieldName;
            //int numSeen = 0;
            System.Collections.Generic.Dictionary<string, string> entities = new System.Collections.Generic.Dictionary<string, string>();
            System.Collections.Generic.Dictionary<string, string> nopayload = new System.Collections.Generic.Dictionary<string, string>();
            int pos;

            public PayloadFilter(TokenStream input, string fieldName)
                : base(input)
            {
                this.fieldName = fieldName;
                pos = 0;
                entities["xx"] = "xx";
                entities["one"] = "one";
                nopayload["nopayload"] = "nopayload";
                nopayload["np"] = "np";

            }

            override public Token Next()
            {
                Token result = input.Next();
                if (result != null)
                {
                    string token = new string(result.TermBuffer(), 0, result.TermLength());

                    if (!nopayload.ContainsKey(token))
                    {
                        if (entities.ContainsKey(token))
                        {
                            result.SetPayload(new Lucene.Net.Index.Payload(System.Text.Encoding.Default.GetBytes(token + ":Entity:" + pos)));
                        }
                        else
                        {
                            result.SetPayload(new Lucene.Net.Index.Payload(System.Text.Encoding.Default.GetBytes(token + ":Noise:" + pos)));
                        }
                    }
                    pos += result.GetPositionIncrement();
                }
                return result;
            }
        }
    }
}