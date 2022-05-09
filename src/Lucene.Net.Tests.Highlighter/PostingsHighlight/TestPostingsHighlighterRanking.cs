#if FEATURE_BREAKITERATOR
using J2N;
using Lucene.Net.Analysis;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Index.Extensions;
using Lucene.Net.Store;
using Lucene.Net.Util;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using JCG = J2N.Collections.Generic;

namespace Lucene.Net.Search.PostingsHighlight
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

    /// <summary>
    /// LUCENENET specific - These are the original tests from Lucene. They are only here as proof that we 
    /// can customize the <see cref="ICUPostingsHighlighter"/> to act like the PostingsHighlighter in Lucene,
    /// which has slightly different default behavior than that of ICU because Lucene uses
    /// the RuleBasedBreakIterator from the JDK, not that of ICU4J.
    /// <para/>
    /// These tests use a mock <see cref="PostingsHighlighter"/>, which is backed by an ICU 
    /// <see cref="ICU4N.Text.RuleBasedBreakIterator"/> that is customized a bit to act (sort of)
    /// like the one in the JDK. However, this customized implementation is not a logical default for
    /// the <see cref="ICUPostingsHighlighter"/>.
    /// </summary>
    [SuppressCodecs("MockFixedIntBlock", "MockVariableIntBlock", "MockSep", "MockRandom", "Lucene3x")]
    public class TestPostingsHighlighterRanking : LuceneTestCase
    {
        /// <summary>
        /// indexes a bunch of gibberish, and then highlights top(n).
        /// asserts that top(n) highlights is a subset of top(n+1) up to some max N
        /// </summary>
        // TODO: this only tests single-valued fields. we should also index multiple values per field!
        [Test]
        [Slow]
        public void TestRanking()
        {
            // number of documents: we will check each one
            int numDocs = AtLeast(100);
            // number of top-N snippets, we will check 1 .. N
            int maxTopN = 5;
            // maximum number of elements to put in a sentence.
            int maxSentenceLength = 10;
            // maximum number of sentences in a document
            int maxNumSentences = 20;

            Directory dir = NewDirectory();
            RandomIndexWriter iw = new RandomIndexWriter(Random, dir, new MockAnalyzer(Random, MockTokenizer.SIMPLE, true));
            Document document = new Document();
            Field id = new StringField("id", "", Field.Store.NO);
            FieldType offsetsType = new FieldType(TextField.TYPE_STORED);
            offsetsType.IndexOptions = (IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS);
            Field body = new Field("body", "", offsetsType);
            document.Add(id);
            document.Add(body);

            for (int i = 0; i < numDocs; i++)
            {
                StringBuilder bodyText = new StringBuilder();
                int numSentences = TestUtil.NextInt32(Random, 1, maxNumSentences);
                for (int j = 0; j < numSentences; j++)
                {
                    bodyText.Append(newSentence(Random, maxSentenceLength));
                }
                body.SetStringValue(bodyText.ToString());
                id.SetStringValue(i.ToString(CultureInfo.InvariantCulture));
                iw.AddDocument(document);
            }

            IndexReader ir = iw.GetReader();
            IndexSearcher searcher = NewSearcher(ir);
            for (int i = 0; i < numDocs; i++)
            {
                checkDocument(searcher, i, maxTopN);
            }
            iw.Dispose();
            ir.Dispose();
            dir.Dispose();
        }

        private void checkDocument(IndexSearcher @is, int doc, int maxTopN)
        {
            for (int ch = 'a'; ch <= 'z'; ch++)
            {
                Term term = new Term("body", "" + (char)ch);
                // check a simple term query
                checkQuery(@is, new TermQuery(term), doc, maxTopN);
                // check a boolean query
                BooleanQuery bq = new BooleanQuery();
                bq.Add(new TermQuery(term), Occur.SHOULD);
                Term nextTerm = new Term("body", "" + (char)(ch + 1));
                bq.Add(new TermQuery(nextTerm), Occur.SHOULD);
                checkQuery(@is, bq, doc, maxTopN);
            }
        }

        internal class CheckQueryPostingsHighlighter : PostingsHighlighter
        {
            internal FakePassageFormatter f = new FakePassageFormatter();

            public CheckQueryPostingsHighlighter(int maxLength)
                : base(maxLength)
            {
            }

            protected override PassageFormatter GetFormatter(string field)
            {
                assertEquals("body", field);
                return f;
            }
        }

        private void checkQuery(IndexSearcher @is, Query query, int doc, int maxTopN)
        {
            for (int n = 1; n < maxTopN; n++)
            {
                CheckQueryPostingsHighlighter p1 = new CheckQueryPostingsHighlighter(int.MaxValue - 1);
                CheckQueryPostingsHighlighter p2 = new CheckQueryPostingsHighlighter(int.MaxValue - 1);

                BooleanQuery bq = new BooleanQuery(false);
                bq.Add(query, Occur.MUST);
                bq.Add(new TermQuery(new Term("id", doc.ToString(CultureInfo.InvariantCulture))), Occur.MUST);
                TopDocs td = @is.Search(bq, 1);
                p1.Highlight("body", bq, @is, td, n);
                p2.Highlight("body", bq, @is, td, n + 1);
                assertTrue(p2.f.seen.containsAll(p1.f.seen));
            }
        }

        /** 
         * returns a new random sentence, up to maxSentenceLength "words" in length.
         * each word is a single character (a-z). The first one is capitalized.
         */
        private String newSentence(Random r, int maxSentenceLength)
        {
            StringBuilder sb = new StringBuilder();
            int numElements = TestUtil.NextInt32(r, 1, maxSentenceLength);
            for (int i = 0; i < numElements; i++)
            {
                if (sb.Length > 0)
                {
                    sb.append(' ');
                    sb.append((char)TestUtil.NextInt32(r, 'a', 'z'));
                }
                else
                {
                    // capitalize the first word to help breakiterator
                    sb.append((char)TestUtil.NextInt32(r, 'A', 'Z'));
                }
            }
            sb.append(". "); // finalize sentence
            return sb.toString();
        }

        /** 
         * a fake formatter that doesn't actually format passages.
         * instead it just collects them for asserts!
         */
        internal class FakePassageFormatter : PassageFormatter
        {
            internal ISet<Pair> seen = new JCG.HashSet<Pair>();

            public override object Format(Passage[] passages, String content)
            {
                foreach (Passage p in passages)
                {
                    // verify some basics about the passage
                    assertTrue(p.Score >= 0);
                    assertTrue(p.NumMatches > 0);
                    assertTrue(p.StartOffset >= 0);
                    assertTrue(p.StartOffset <= content.Length);
                    assertTrue(p.EndOffset >= p.StartOffset);
                    assertTrue(p.EndOffset <= content.Length);
                    // we use a very simple analyzer. so we can assert the matches are correct
                    int lastMatchStart = -1;
                    for (int i = 0; i < p.NumMatches; i++)
                    {
                        BytesRef term = p.MatchTerms[i];
                        int matchStart = p.MatchStarts[i];
                        assertTrue(matchStart >= 0);
                        // must at least start within the passage
                        assertTrue(matchStart < p.EndOffset);
                        int matchEnd = p.MatchEnds[i];
                        assertTrue(matchEnd >= 0);
                        // always moving forward
                        assertTrue(matchStart >= lastMatchStart);
                        lastMatchStart = matchStart;
                        // single character terms
                        assertEquals(matchStart + 1, matchEnd);
                        // and the offsets must be correct...
                        assertEquals(1, term.Length);
                        assertEquals((char)term.Bytes[term.Offset], Character.ToLower(content[matchStart], CultureInfo.InvariantCulture));
                    }
                    // record just the start/end offset for simplicity
                    seen.Add(new Pair(p.StartOffset, p.EndOffset));
                }
                return "bogus!!!!!!";
            }
        }

        internal class Pair
        {
            internal readonly int start;
            internal readonly int end;

            internal Pair(int start, int end)
            {
                this.start = start;
                this.end = end;
            }

            public override int GetHashCode()
            {
                int prime = 31;
                int result = 1;
                result = prime * result + end;
                result = prime * result + start;
                return result;
            }

            public override bool Equals(object obj)
            {
                if (this == obj)
                {
                    return true;
                }
                if (obj is null)
                {
                    return false;
                }
                if (GetType() != obj.GetType())
                {
                    return false;
                }
                Pair other = (Pair)obj;
                if (end != other.end)
                {
                    return false;
                }
                if (start != other.start)
                {
                    return false;
                }
                return true;
            }


            public override string ToString()
            {
                return "Pair [start=" + start + ", end=" + end + "]";
            }
        }

        /** sets b=0 to disable passage length normalization */
        [Test]
        public void TestCustomB()
        {
            Directory dir = NewDirectory();
            IndexWriterConfig iwc = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random, MockTokenizer.SIMPLE, true));
            iwc.SetMergePolicy(NewLogMergePolicy());
            RandomIndexWriter iw = new RandomIndexWriter(Random, dir, iwc);

            FieldType offsetsType = new FieldType(TextField.TYPE_STORED);
            offsetsType.IndexOptions = (IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS);
            Field body = new Field("body", "", offsetsType);
            Document doc = new Document();
            doc.Add(body);

            body.SetStringValue("This is a test.  This test is a better test but the sentence is excruiatingly long, " +
                                "you have no idea how painful it was for me to type this long sentence into my IDE.");
            iw.AddDocument(doc);

            IndexReader ir = iw.GetReader();
            iw.Dispose();

            IndexSearcher searcher = NewSearcher(ir);
            PostingsHighlighter highlighter = new CustomBPostingsHighlighter();
            Query query = new TermQuery(new Term("body", "test"));
            TopDocs topDocs = searcher.Search(query, null, 10, Sort.INDEXORDER);
            assertEquals(1, topDocs.TotalHits);
            String[] snippets = highlighter.Highlight("body", query, searcher, topDocs, 1);
            assertEquals(1, snippets.Length);
            assertTrue(snippets[0].StartsWith("This <b>test</b> is a better <b>test</b>", StringComparison.Ordinal));

            ir.Dispose();
            dir.Dispose();
        }

        internal class CustomBPostingsHighlighter : PostingsHighlighter
        {
            protected override PassageScorer GetScorer(string field)
            {
                return new PassageScorer(1.2f, 0, 87);
            }
        }

        /** sets k1=0 for simple coordinate-level match (# of query terms present) */
        [Test]
        public void TestCustomK1()
        {
            Directory dir = NewDirectory();
            IndexWriterConfig iwc = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random, MockTokenizer.SIMPLE, true));
            iwc.SetMergePolicy(NewLogMergePolicy());
            RandomIndexWriter iw = new RandomIndexWriter(Random, dir, iwc);

            FieldType offsetsType = new FieldType(TextField.TYPE_STORED);
            offsetsType.IndexOptions = (IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS);
            Field body = new Field("body", "", offsetsType);
            Document doc = new Document();
            doc.Add(body);

            body.SetStringValue("This has only foo foo. " +
                                "On the other hand this sentence contains both foo and bar. " +
                                "This has only bar bar bar bar bar bar bar bar bar bar bar bar.");
            iw.AddDocument(doc);

            IndexReader ir = iw.GetReader();
            iw.Dispose();

            IndexSearcher searcher = NewSearcher(ir);
            PostingsHighlighter highlighter = new CustomK1PostingsHighlighter();
            BooleanQuery query = new BooleanQuery();
            query.Add(new TermQuery(new Term("body", "foo")), Occur.SHOULD);
            query.Add(new TermQuery(new Term("body", "bar")), Occur.SHOULD);
            TopDocs topDocs = searcher.Search(query, null, 10, Sort.INDEXORDER);
            assertEquals(1, topDocs.TotalHits);
            String[] snippets = highlighter.Highlight("body", query, searcher, topDocs, 1);
            assertEquals(1, snippets.Length);
            assertTrue(snippets[0].StartsWith("On the other hand", StringComparison.Ordinal));

            ir.Dispose();
            dir.Dispose();
        }

        internal class CustomK1PostingsHighlighter : PostingsHighlighter
        {
            public CustomK1PostingsHighlighter()
                : base(10000)
            { }

            protected override PassageScorer GetScorer(string field)
            {
                return new PassageScorer(0, 0.75f, 87);
            }
        }
    }
}
#endif