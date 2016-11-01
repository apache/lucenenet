using Lucene.Net.Analysis.Tokenattributes;
using System;
using System.Collections.Generic;
using System.Text;
using Lucene.Net.Documents;

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

    using Lucene.Net.Analysis;
    using NUnit.Framework;
    using System.IO;
    using AtomicReader = Lucene.Net.Index.AtomicReader;
    using BytesRef = Lucene.Net.Util.BytesRef;
    using Directory = Lucene.Net.Store.Directory;
    using DocsAndPositionsEnum = Lucene.Net.Index.DocsAndPositionsEnum;
    using Document = Documents.Document;
    using Field = Field;
    using IndexReader = Lucene.Net.Index.IndexReader;
    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
    using MultiFields = Lucene.Net.Index.MultiFields;
    using MultiSpansWrapper = Lucene.Net.Search.Spans.MultiSpansWrapper;
    using PayloadSpanUtil = Lucene.Net.Search.Payloads.PayloadSpanUtil;
    using RandomIndexWriter = Lucene.Net.Index.RandomIndexWriter;
    using SlowCompositeReaderWrapper = Lucene.Net.Index.SlowCompositeReaderWrapper;
    using SpanNearQuery = Lucene.Net.Search.Spans.SpanNearQuery;
    using SpanQuery = Lucene.Net.Search.Spans.SpanQuery;
    using SpanTermQuery = Lucene.Net.Search.Spans.SpanTermQuery;
    using Term = Lucene.Net.Index.Term;
    using TextField = TextField;

    /// <summary>
    /// Term position unit test.
    ///
    ///
    /// </summary>
    [TestFixture]
    public class TestPositionIncrement : LuceneTestCase
    {
        // LUCENENET: This existed in Lucene, but do we really want to override the global setting?
        //internal const bool VERBOSE = false;

        [Test]
        public virtual void TestSetPosition()
        {
            Analyzer analyzer = new AnalyzerAnonymousInnerClassHelper(this);
            Directory store = NewDirectory();
            RandomIndexWriter writer = new RandomIndexWriter(Random(), store, analyzer, Similarity, TimeZone);
            Document d = new Document();
            d.Add(NewTextField("field", "bogus", Field.Store.YES));
            writer.AddDocument(d);
            IndexReader reader = writer.Reader;
            writer.Dispose();

            IndexSearcher searcher = NewSearcher(reader);

            DocsAndPositionsEnum pos = MultiFields.GetTermPositionsEnum(searcher.IndexReader, MultiFields.GetLiveDocs(searcher.IndexReader), "field", new BytesRef("1"));
            pos.NextDoc();
            // first token should be at position 0
            Assert.AreEqual(0, pos.NextPosition());

            pos = MultiFields.GetTermPositionsEnum(searcher.IndexReader, MultiFields.GetLiveDocs(searcher.IndexReader), "field", new BytesRef("2"));
            pos.NextDoc();
            // second token should be at position 2
            Assert.AreEqual(2, pos.NextPosition());

            PhraseQuery q;
            ScoreDoc[] hits;

            q = new PhraseQuery();
            q.Add(new Term("field", "1"));
            q.Add(new Term("field", "2"));
            hits = searcher.Search(q, null, 1000).ScoreDocs;
            Assert.AreEqual(0, hits.Length);

            // same as previous, just specify positions explicitely.
            q = new PhraseQuery();
            q.Add(new Term("field", "1"), 0);
            q.Add(new Term("field", "2"), 1);
            hits = searcher.Search(q, null, 1000).ScoreDocs;
            Assert.AreEqual(0, hits.Length);

            // specifying correct positions should find the phrase.
            q = new PhraseQuery();
            q.Add(new Term("field", "1"), 0);
            q.Add(new Term("field", "2"), 2);
            hits = searcher.Search(q, null, 1000).ScoreDocs;
            Assert.AreEqual(1, hits.Length);

            q = new PhraseQuery();
            q.Add(new Term("field", "2"));
            q.Add(new Term("field", "3"));
            hits = searcher.Search(q, null, 1000).ScoreDocs;
            Assert.AreEqual(1, hits.Length);

            q = new PhraseQuery();
            q.Add(new Term("field", "3"));
            q.Add(new Term("field", "4"));
            hits = searcher.Search(q, null, 1000).ScoreDocs;
            Assert.AreEqual(0, hits.Length);

            // phrase query would find it when correct positions are specified.
            q = new PhraseQuery();
            q.Add(new Term("field", "3"), 0);
            q.Add(new Term("field", "4"), 0);
            hits = searcher.Search(q, null, 1000).ScoreDocs;
            Assert.AreEqual(1, hits.Length);

            // phrase query should fail for non existing searched term
            // even if there exist another searched terms in the same searched position.
            q = new PhraseQuery();
            q.Add(new Term("field", "3"), 0);
            q.Add(new Term("field", "9"), 0);
            hits = searcher.Search(q, null, 1000).ScoreDocs;
            Assert.AreEqual(0, hits.Length);

            // multi-phrase query should succed for non existing searched term
            // because there exist another searched terms in the same searched position.
            MultiPhraseQuery mq = new MultiPhraseQuery();
            mq.Add(new Term[] { new Term("field", "3"), new Term("field", "9") }, 0);
            hits = searcher.Search(mq, null, 1000).ScoreDocs;
            Assert.AreEqual(1, hits.Length);

            q = new PhraseQuery();
            q.Add(new Term("field", "2"));
            q.Add(new Term("field", "4"));
            hits = searcher.Search(q, null, 1000).ScoreDocs;
            Assert.AreEqual(1, hits.Length);

            q = new PhraseQuery();
            q.Add(new Term("field", "3"));
            q.Add(new Term("field", "5"));
            hits = searcher.Search(q, null, 1000).ScoreDocs;
            Assert.AreEqual(1, hits.Length);

            q = new PhraseQuery();
            q.Add(new Term("field", "4"));
            q.Add(new Term("field", "5"));
            hits = searcher.Search(q, null, 1000).ScoreDocs;
            Assert.AreEqual(1, hits.Length);

            q = new PhraseQuery();
            q.Add(new Term("field", "2"));
            q.Add(new Term("field", "5"));
            hits = searcher.Search(q, null, 1000).ScoreDocs;
            Assert.AreEqual(0, hits.Length);

            reader.Dispose();
            store.Dispose();
        }

        private class AnalyzerAnonymousInnerClassHelper : Analyzer
        {
            private readonly TestPositionIncrement OuterInstance;

            public AnalyzerAnonymousInnerClassHelper(TestPositionIncrement outerInstance)
            {
                this.OuterInstance = outerInstance;
            }

            public override TokenStreamComponents CreateComponents(string fieldName, TextReader reader)
            {
                return new TokenStreamComponents(new TokenizerAnonymousInnerClassHelper(this, reader));
            }

            private class TokenizerAnonymousInnerClassHelper : Tokenizer
            {
                private readonly AnalyzerAnonymousInnerClassHelper OuterInstance;

                public TokenizerAnonymousInnerClassHelper(AnalyzerAnonymousInnerClassHelper outerInstance, TextReader reader)
                    : base(reader)
                {
                    this.OuterInstance = outerInstance;
                    TOKENS = new string[] { "1", "2", "3", "4", "5" };
                    INCREMENTS = new int[] { 1, 2, 1, 0, 1 };
                    i = 0;
                    posIncrAtt = AddAttribute<IPositionIncrementAttribute>();
                    termAtt = AddAttribute<ICharTermAttribute>();
                    offsetAtt = AddAttribute<IOffsetAttribute>();
                }

                // TODO: use CannedTokenStream
                private readonly string[] TOKENS;

                private readonly int[] INCREMENTS;
                private int i;

                internal IPositionIncrementAttribute posIncrAtt;
                internal ICharTermAttribute termAtt;
                internal IOffsetAttribute offsetAtt;

                public override sealed bool IncrementToken()
                {
                    if (i == TOKENS.Length)
                    {
                        return false;
                    }
                    ClearAttributes();
                    termAtt.Append(TOKENS[i]);
                    offsetAtt.SetOffset(i, i);
                    posIncrAtt.PositionIncrement = INCREMENTS[i];
                    i++;
                    return true;
                }

                public override void Reset()
                {
                    base.Reset();
                    this.i = 0;
                }
            }
        }

        [Test]
        public virtual void TestPayloadsPos0()
        {
            Directory dir = NewDirectory();
            RandomIndexWriter writer = new RandomIndexWriter(Random(), dir, new MockPayloadAnalyzer(), Similarity, TimeZone);
            Document doc = new Document();
            doc.Add(new TextField("content", new StringReader("a a b c d e a f g h i j a b k k")));
            writer.AddDocument(doc);

            IndexReader readerFromWriter = writer.Reader;
            AtomicReader r = SlowCompositeReaderWrapper.Wrap(readerFromWriter);

            DocsAndPositionsEnum tp = r.TermPositionsEnum(new Term("content", "a"));

            int count = 0;
            Assert.IsTrue(tp.NextDoc() != DocIdSetIterator.NO_MORE_DOCS);
            // "a" occurs 4 times
            Assert.AreEqual(4, tp.Freq());
            Assert.AreEqual(0, tp.NextPosition());
            Assert.AreEqual(1, tp.NextPosition());
            Assert.AreEqual(3, tp.NextPosition());
            Assert.AreEqual(6, tp.NextPosition());

            // only one doc has "a"
            Assert.AreEqual(DocIdSetIterator.NO_MORE_DOCS, tp.NextDoc());

            IndexSearcher @is = NewSearcher(readerFromWriter);

            SpanTermQuery stq1 = new SpanTermQuery(new Term("content", "a"));
            SpanTermQuery stq2 = new SpanTermQuery(new Term("content", "k"));
            SpanQuery[] sqs = new SpanQuery[] { stq1, stq2 };
            SpanNearQuery snq = new SpanNearQuery(sqs, 30, false);

            count = 0;
            bool sawZero = false;
            if (VERBOSE)
            {
                Console.WriteLine("\ngetPayloadSpans test");
            }
            Search.Spans.Spans pspans = MultiSpansWrapper.Wrap(@is.TopReaderContext, snq);
            while (pspans.Next())
            {
                if (VERBOSE)
                {
                    Console.WriteLine("doc " + pspans.Doc() + ": span " + pspans.Start() + " to " + pspans.End());
                }
                var payloads = pspans.Payload;
                sawZero |= pspans.Start() == 0;
                foreach (var bytes in payloads)
                {
                    count++;
                    if (VERBOSE)
                    {
                        Console.WriteLine("  payload: " + Encoding.UTF8.GetString((byte[])(Array)bytes));
                    }
                }
            }
            Assert.IsTrue(sawZero);
            Assert.AreEqual(5, count);

            // System.out.println("\ngetSpans test");
            Search.Spans.Spans spans = MultiSpansWrapper.Wrap(@is.TopReaderContext, snq);
            count = 0;
            sawZero = false;
            while (spans.Next())
            {
                count++;
                sawZero |= spans.Start() == 0;
                // System.out.println(spans.Doc() + " - " + spans.Start() + " - " +
                // spans.End());
            }
            Assert.AreEqual(4, count);
            Assert.IsTrue(sawZero);

            // System.out.println("\nPayloadSpanUtil test");

            sawZero = false;
            PayloadSpanUtil psu = new PayloadSpanUtil(@is.TopReaderContext);
            var pls = psu.GetPayloadsForQuery(snq);
            count = pls.Count;
            foreach (var bytes in pls)
            {
                string s = Encoding.UTF8.GetString(bytes);
                //System.out.println(s);
                sawZero |= s.Equals("pos: 0");
            }
            Assert.AreEqual(5, count);
            Assert.IsTrue(sawZero);
            writer.Dispose();
            @is.IndexReader.Dispose();
            dir.Dispose();
        }
    }
}