using Lucene.Net.Analysis;
using Lucene.Net.Analysis.TokenAttributes;
using Lucene.Net.Documents;
using Lucene.Net.Index.Extensions;
using NUnit.Framework;
using System;
using System.IO;
using Assert = Lucene.Net.TestFramework.Assert;

namespace Lucene.Net.Search.Payloads
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

    using BytesRef = Lucene.Net.Util.BytesRef;
    using DefaultSimilarity = Lucene.Net.Search.Similarities.DefaultSimilarity;
    using Directory = Lucene.Net.Store.Directory;
    using DirectoryReader = Lucene.Net.Index.DirectoryReader;
    using Document = Documents.Document;
    using English = Lucene.Net.Util.English;
    using Field = Field;
    using FieldInvertState = Lucene.Net.Index.FieldInvertState;
    using IndexReader = Lucene.Net.Index.IndexReader;
    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
    using MultiSpansWrapper = Lucene.Net.Search.Spans.MultiSpansWrapper;
    using RandomIndexWriter = Lucene.Net.Index.RandomIndexWriter;
    using Similarity = Lucene.Net.Search.Similarities.Similarity;
    using Spans = Lucene.Net.Search.Spans.Spans;
    using SpanTermQuery = Lucene.Net.Search.Spans.SpanTermQuery;
    using Term = Lucene.Net.Index.Term;

    [TestFixture]
    public class TestPayloadTermQuery : LuceneTestCase
    {
        private static IndexSearcher searcher;
        private static IndexReader reader;
        private static readonly Similarity similarity = new BoostingSimilarity();
        private static readonly byte[] payloadField = { 1 };
        private static readonly byte[] payloadMultiField1 = { 2 };
        private static readonly byte[] payloadMultiField2 = { 4 };
        protected internal static Directory directory;

        private class PayloadAnalyzer : Analyzer
        {
            internal PayloadAnalyzer()
                : base(PER_FIELD_REUSE_STRATEGY)
            {
            }

            protected internal override TokenStreamComponents CreateComponents(string fieldName, TextReader reader)
            {
                Tokenizer result = new MockTokenizer(reader, MockTokenizer.SIMPLE, true);
                return new TokenStreamComponents(result, new PayloadFilter(result, fieldName));
            }
        }

        private class PayloadFilter : TokenFilter
        {
            private readonly string fieldName;
            private int numSeen = 0;

            private readonly IPayloadAttribute payloadAtt;

            public PayloadFilter(TokenStream input, string fieldName)
                : base(input)
            {
                this.fieldName = fieldName;
                payloadAtt = AddAttribute<IPayloadAttribute>();
            }

            public sealed override bool IncrementToken()
            {
                bool hasNext = m_input.IncrementToken();
                if (hasNext)
                {
                    if (fieldName.Equals("field", StringComparison.Ordinal))
                    {
                        payloadAtt.Payload = new BytesRef(payloadField);
                    }
                    else if (fieldName.Equals("multiField", StringComparison.Ordinal))
                    {
                        if (numSeen % 2 == 0)
                        {
                            payloadAtt.Payload = new BytesRef(payloadMultiField1);
                        }
                        else
                        {
                            payloadAtt.Payload = new BytesRef(payloadMultiField2);
                        }
                        numSeen++;
                    }
                    return true;
                }
                else
                {
                    return false;
                }
            }

            public override void Reset()
            {
                base.Reset();
                this.numSeen = 0;
            }
        }

        /// <summary>
        /// LUCENENET specific
        /// Is non-static because NewIndexWriterConfig is no longer static.
        /// </summary>
        [OneTimeSetUp]
        public override void BeforeClass()
        {
            base.BeforeClass();

            directory = NewDirectory();
            RandomIndexWriter writer = new RandomIndexWriter(Random, directory, NewIndexWriterConfig(TEST_VERSION_CURRENT, new PayloadAnalyzer()).SetSimilarity(similarity).SetMergePolicy(NewLogMergePolicy()));
            //writer.infoStream = System.out;
            for (int i = 0; i < 1000; i++)
            {
                Document doc = new Document();
                Field noPayloadField = NewTextField(PayloadHelper.NO_PAYLOAD_FIELD, English.Int32ToEnglish(i), Field.Store.YES);
                //noPayloadField.setBoost(0);
                doc.Add(noPayloadField);
                doc.Add(NewTextField("field", English.Int32ToEnglish(i), Field.Store.YES));
                doc.Add(NewTextField("multiField", English.Int32ToEnglish(i) + "  " + English.Int32ToEnglish(i), Field.Store.YES));
                writer.AddDocument(doc);
            }
            reader = writer.GetReader();
            writer.Dispose();

            searcher = NewSearcher(reader);
            searcher.Similarity = similarity;
        }

        [OneTimeTearDown]
        public override void AfterClass()
        {
            searcher = null;
            reader.Dispose();
            reader = null;
            directory.Dispose();
            directory = null;
            base.AfterClass();
        }

        [Test]
        public virtual void Test()
        {
            PayloadTermQuery query = new PayloadTermQuery(new Term("field", "seventy"), new MaxPayloadFunction());
            TopDocs hits = searcher.Search(query, null, 100);
            Assert.IsTrue(hits != null, "hits is null and it shouldn't be");
            Assert.IsTrue(hits.TotalHits == 100, "hits Size: " + hits.TotalHits + " is not: " + 100);

            //they should all have the exact same score, because they all contain seventy once, and we set
            //all the other similarity factors to be 1

            Assert.IsTrue(hits.MaxScore == 1, hits.MaxScore + " does not equal: " + 1);
            for (int i = 0; i < hits.ScoreDocs.Length; i++)
            {
                ScoreDoc doc = hits.ScoreDocs[i];
                Assert.IsTrue(doc.Score == 1, doc.Score + " does not equal: " + 1);
            }
            CheckHits.CheckExplanations(query, PayloadHelper.FIELD, searcher, true);
            Spans spans = MultiSpansWrapper.Wrap(searcher.TopReaderContext, query);
            Assert.IsTrue(spans != null, "spans is null and it shouldn't be");
            /*float score = hits.Score(0);
            for (int i =1; i < hits.Length(); i++)
            {
              Assert.IsTrue(score == hits.Score(i), "scores are not equal and they should be");
            }*/
        }

        [Test]
        public virtual void TestQuery()
        {
            PayloadTermQuery boostingFuncTermQuery = new PayloadTermQuery(new Term(PayloadHelper.MULTI_FIELD, "seventy"), new MaxPayloadFunction());
            QueryUtils.Check(boostingFuncTermQuery);

            SpanTermQuery spanTermQuery = new SpanTermQuery(new Term(PayloadHelper.MULTI_FIELD, "seventy"));

            Assert.IsTrue(boostingFuncTermQuery.Equals(spanTermQuery) == spanTermQuery.Equals(boostingFuncTermQuery));

            PayloadTermQuery boostingFuncTermQuery2 = new PayloadTermQuery(new Term(PayloadHelper.MULTI_FIELD, "seventy"), new AveragePayloadFunction());

            QueryUtils.CheckUnequal(boostingFuncTermQuery, boostingFuncTermQuery2);
        }

        [Test]
        public virtual void TestMultipleMatchesPerDoc()
        {
            PayloadTermQuery query = new PayloadTermQuery(new Term(PayloadHelper.MULTI_FIELD, "seventy"), new MaxPayloadFunction());
            TopDocs hits = searcher.Search(query, null, 100);
            Assert.IsTrue(hits != null, "hits is null and it shouldn't be");
            Assert.IsTrue(hits.TotalHits == 100, "hits Size: " + hits.TotalHits + " is not: " + 100);

            //they should all have the exact same score, because they all contain seventy once, and we set
            //all the other similarity factors to be 1

            //System.out.println("Hash: " + seventyHash + " Twice Hash: " + 2*seventyHash);
            Assert.IsTrue(hits.MaxScore == 4.0, hits.MaxScore + " does not equal: " + 4.0);
            //there should be exactly 10 items that score a 4, all the rest should score a 2
            //The 10 items are: 70 + i*100 where i in [0-9]
            int numTens = 0;
            for (int i = 0; i < hits.ScoreDocs.Length; i++)
            {
                ScoreDoc doc = hits.ScoreDocs[i];
                if (doc.Doc % 10 == 0)
                {
                    numTens++;
                    Assert.IsTrue(doc.Score == 4.0, doc.Score + " does not equal: " + 4.0);
                }
                else
                {
                    Assert.IsTrue(doc.Score == 2, doc.Score + " does not equal: " + 2);
                }
            }
            Assert.IsTrue(numTens == 10, numTens + " does not equal: " + 10);
            CheckHits.CheckExplanations(query, "field", searcher, true);
            Spans spans = MultiSpansWrapper.Wrap(searcher.TopReaderContext, query);
            Assert.IsTrue(spans != null, "spans is null and it shouldn't be");
            //should be two matches per document
            int count = 0;
            //100 hits times 2 matches per hit, we should have 200 in count
            while (spans.MoveNext())
            {
                count++;
            }
            Assert.IsTrue(count == 200, count + " does not equal: " + 200);
        }

        //Set includeSpanScore to false, in which case just the payload score comes through.
        [Test]
        public virtual void TestIgnoreSpanScorer()
        {
            PayloadTermQuery query = new PayloadTermQuery(new Term(PayloadHelper.MULTI_FIELD, "seventy"), new MaxPayloadFunction(), false);

            IndexReader reader = DirectoryReader.Open(directory);
            IndexSearcher theSearcher = NewSearcher(reader);
            theSearcher.Similarity = new FullSimilarity();
            TopDocs hits = searcher.Search(query, null, 100);
            Assert.IsTrue(hits != null, "hits is null and it shouldn't be");
            Assert.IsTrue(hits.TotalHits == 100, "hits Size: " + hits.TotalHits + " is not: " + 100);

            //they should all have the exact same score, because they all contain seventy once, and we set
            //all the other similarity factors to be 1

            //System.out.println("Hash: " + seventyHash + " Twice Hash: " + 2*seventyHash);
            Assert.IsTrue(hits.MaxScore == 4.0, hits.MaxScore + " does not equal: " + 4.0);
            //there should be exactly 10 items that score a 4, all the rest should score a 2
            //The 10 items are: 70 + i*100 where i in [0-9]
            int numTens = 0;
            for (int i = 0; i < hits.ScoreDocs.Length; i++)
            {
                ScoreDoc doc = hits.ScoreDocs[i];
                if (doc.Doc % 10 == 0)
                {
                    numTens++;
                    Assert.IsTrue(doc.Score == 4.0, doc.Score + " does not equal: " + 4.0);
                }
                else
                {
                    Assert.IsTrue(doc.Score == 2, doc.Score + " does not equal: " + 2);
                }
            }
            Assert.IsTrue(numTens == 10, numTens + " does not equal: " + 10);
            CheckHits.CheckExplanations(query, "field", searcher, true);
            Spans spans = MultiSpansWrapper.Wrap(searcher.TopReaderContext, query);
            Assert.IsTrue(spans != null, "spans is null and it shouldn't be");
            //should be two matches per document
            int count = 0;
            //100 hits times 2 matches per hit, we should have 200 in count
            while (spans.MoveNext())
            {
                count++;
            }
            reader.Dispose();
        }

        [Test]
        public virtual void TestNoMatch()
        {
            PayloadTermQuery query = new PayloadTermQuery(new Term(PayloadHelper.FIELD, "junk"), new MaxPayloadFunction());
            TopDocs hits = searcher.Search(query, null, 100);
            Assert.IsTrue(hits != null, "hits is null and it shouldn't be");
            Assert.IsTrue(hits.TotalHits == 0, "hits Size: " + hits.TotalHits + " is not: " + 0);
        }

        [Test]
        public virtual void TestNoPayload()
        {
            PayloadTermQuery q1 = new PayloadTermQuery(new Term(PayloadHelper.NO_PAYLOAD_FIELD, "zero"), new MaxPayloadFunction());
            PayloadTermQuery q2 = new PayloadTermQuery(new Term(PayloadHelper.NO_PAYLOAD_FIELD, "foo"), new MaxPayloadFunction());
            BooleanClause c1 = new BooleanClause(q1, Occur.MUST);
            BooleanClause c2 = new BooleanClause(q2, Occur.MUST_NOT);
            BooleanQuery query = new BooleanQuery();
            query.Add(c1);
            query.Add(c2);
            TopDocs hits = searcher.Search(query, null, 100);
            Assert.IsTrue(hits != null, "hits is null and it shouldn't be");
            Assert.IsTrue(hits.TotalHits == 1, "hits Size: " + hits.TotalHits + " is not: " + 1);
            int[] results = new int[1];
            results[0] = 0; //hits.ScoreDocs[0].Doc;
            CheckHits.CheckHitCollector(Random, query, PayloadHelper.NO_PAYLOAD_FIELD, searcher, results);
        }

        internal class BoostingSimilarity : DefaultSimilarity
        {
            public override float QueryNorm(float sumOfSquaredWeights)
            {
                return 1;
            }

            public override float Coord(int overlap, int maxOverlap)
            {
                return 1;
            }

            // TODO: Remove warning after API has been finalized
            public override float ScorePayload(int docId, int start, int end, BytesRef payload)
            {
                //we know it is size 4 here, so ignore the offset/length
                return payload.Bytes[payload.Offset];
            }

            //!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
            //Make everything else 1 so we see the effect of the payload
            //!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
            public override float LengthNorm(FieldInvertState state)
            {
                return state.Boost;
            }

            public override float SloppyFreq(int distance)
            {
                return 1;
            }

            public override float Idf(long docFreq, long numDocs)
            {
                return 1;
            }

            public override float Tf(float freq)
            {
                return freq == 0 ? 0 : 1;
            }
        }

        internal class FullSimilarity : DefaultSimilarity
        {
            public virtual float ScorePayload(int docId, string fieldName, sbyte[] payload, int offset, int length)
            {
                //we know it is size 4 here, so ignore the offset/length
                return payload[offset];
            }
        }
    }
}