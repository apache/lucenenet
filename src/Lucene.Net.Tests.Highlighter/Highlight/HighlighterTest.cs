using J2N.Text;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.TokenAttributes;
using Lucene.Net.Attributes;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Index.Extensions;
using Lucene.Net.Queries;
using Lucene.Net.Search.Spans;
using Lucene.Net.Support;
using Lucene.Net.Util;
using Lucene.Net.Util.Automaton;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Xml;
using JCG = J2N.Collections.Generic;
using Console = Lucene.Net.Util.SystemConsole;

namespace Lucene.Net.Search.Highlight
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
    /// NUnit Test for Highlighter class.
    /// </summary>
    public class HighlighterTest : BaseTokenStreamTestCase, IFormatter
    {
        private IndexReader reader;
        internal static readonly String FIELD_NAME = "contents";
        private static readonly String NUMERIC_FIELD_NAME = "nfield";
        private Query query;
        Store.Directory ramDir;
        public IndexSearcher searcher = null;
        int numHighlights = 0;
        Analyzer analyzer;
        TopDocs hits;

        String[] texts = {
            "Hello this is a piece of text that is very long and contains too much preamble and the meat is really here which says kennedy has been shot",
            "This piece of text refers to Kennedy at the beginning then has a longer piece of text that is very long in the middle and finally ends with another reference to Kennedy",
            "JFK has been shot", "John Kennedy has been shot",
            "This text has a typo in referring to Keneddy",
            "wordx wordy wordz wordx wordy wordx worda wordb wordy wordc", "y z x y z a b", "lets is a the lets is a the lets is a the lets" };

        [Test]
        public void TestQueryScorerHits()
        {
            Analyzer analyzer = new MockAnalyzer(Random, MockTokenizer.SIMPLE, true);

            PhraseQuery phraseQuery = new PhraseQuery();
            phraseQuery.Add(new Term(FIELD_NAME, "very"));
            phraseQuery.Add(new Term(FIELD_NAME, "long"));

            query = phraseQuery;
            searcher = NewSearcher(reader);
            TopDocs hits = searcher.Search(query, 10);

            QueryScorer scorer = new QueryScorer(query, FIELD_NAME);
            Highlighter highlighter = new Highlighter(scorer);


            for (int i = 0; i < hits.ScoreDocs.Length; i++)
            {
                Document doc = searcher.Doc(hits.ScoreDocs[i].Doc);
                String storedField = doc.Get(FIELD_NAME);

                TokenStream stream = TokenSources.GetAnyTokenStream(searcher
                    .IndexReader, hits.ScoreDocs[i].Doc, FIELD_NAME, doc, analyzer);

                IFragmenter fragmenter = new SimpleSpanFragmenter(scorer);

                highlighter.TextFragmenter = (fragmenter);

                String fragment = highlighter.GetBestFragment(stream, storedField);

                if (Verbose) Console.WriteLine(fragment);
            }
        }

        [Test]
        public void TestHighlightingCommonTermsQuery()
        {
            Analyzer analyzer = new MockAnalyzer(Random, MockTokenizer.SIMPLE, true);
            CommonTermsQuery query = new CommonTermsQuery(Occur.MUST, Occur.SHOULD, 3);
            query.Add(new Term(FIELD_NAME, "this"));
            query.Add(new Term(FIELD_NAME, "long"));
            query.Add(new Term(FIELD_NAME, "very"));

            searcher = NewSearcher(reader);
            TopDocs hits = searcher.Search(query, 10);
            assertEquals(2, hits.TotalHits);
            QueryScorer scorer = new QueryScorer(query, FIELD_NAME);
            Highlighter highlighter = new Highlighter(scorer);

            Document doc = searcher.Doc(hits.ScoreDocs[0].Doc);
            String storedField = doc.Get(FIELD_NAME);

            TokenStream stream = TokenSources.GetAnyTokenStream(searcher
                .IndexReader, hits.ScoreDocs[0].Doc, FIELD_NAME, doc, analyzer);
            IFragmenter fragmenter = new SimpleSpanFragmenter(scorer);
            highlighter.TextFragmenter = (fragmenter);
            String fragment = highlighter.GetBestFragment(stream, storedField);
            assertEquals("Hello <B>this</B> is a piece of text that is <B>very</B> <B>long</B> and contains too much preamble and the meat is really here which says kennedy has been shot", fragment);

            doc = searcher.Doc(hits.ScoreDocs[1].Doc);
            storedField = doc.Get(FIELD_NAME);

            stream = TokenSources.GetAnyTokenStream(searcher
                .IndexReader, hits.ScoreDocs[1].Doc, FIELD_NAME, doc, analyzer);
            highlighter.TextFragmenter = (new SimpleSpanFragmenter(scorer));
            fragment = highlighter.GetBestFragment(stream, storedField);
            assertEquals("<B>This</B> piece of text refers to Kennedy at the beginning then has a longer piece of text that is <B>very</B>", fragment);
        }

        private sealed class TestHighlightUnknowQueryAnonymousClass : Query
        {
            public override Query Rewrite(IndexReader reader)
            {
                CommonTermsQuery query = new CommonTermsQuery(Occur.MUST, Occur.SHOULD, 3);
                query.Add(new Term(FIELD_NAME, "this"));
                query.Add(new Term(FIELD_NAME, "long"));
                query.Add(new Term(FIELD_NAME, "very"));
                return query;
            }

            public override string ToString(string field)
            {
                return null;
            }

            public override int GetHashCode()
            {
                return 31 * base.GetHashCode();
            }

            public override bool Equals(object obj)
            {
                return base.Equals(obj);
            }
        }

        [Test]
        public void TestHighlightUnknowQueryAfterRewrite()
        {
            Query query = new TestHighlightUnknowQueryAnonymousClass();

            Analyzer analyzer = new MockAnalyzer(Random, MockTokenizer.SIMPLE, true);

            searcher = NewSearcher(reader);
            TopDocs hits = searcher.Search(query, 10);
            assertEquals(2, hits.TotalHits);
            QueryScorer scorer = new QueryScorer(query, FIELD_NAME);
            Highlighter highlighter = new Highlighter(scorer);

            Document doc = searcher.Doc(hits.ScoreDocs[0].Doc);
            String storedField = doc.Get(FIELD_NAME);

            TokenStream stream = TokenSources.GetAnyTokenStream(searcher
                .IndexReader, hits.ScoreDocs[0].Doc, FIELD_NAME, doc, analyzer);
            IFragmenter fragmenter = new SimpleSpanFragmenter(scorer);
            highlighter.TextFragmenter = (fragmenter);
            String fragment = highlighter.GetBestFragment(stream, storedField);
            assertEquals("Hello <B>this</B> is a piece of text that is <B>very</B> <B>long</B> and contains too much preamble and the meat is really here which says kennedy has been shot", fragment);

            doc = searcher.Doc(hits.ScoreDocs[1].Doc);
            storedField = doc.Get(FIELD_NAME);

            stream = TokenSources.GetAnyTokenStream(searcher
                .IndexReader, hits.ScoreDocs[1].Doc, FIELD_NAME, doc, analyzer);
            highlighter.TextFragmenter = (new SimpleSpanFragmenter(scorer));
            fragment = highlighter.GetBestFragment(stream, storedField);
            assertEquals("<B>This</B> piece of text refers to Kennedy at the beginning then has a longer piece of text that is <B>very</B>", fragment);

        }

        [Test]
        public void TestHighlightingWithDefaultField()
        {

            String s1 = "I call our world Flatland, not because we call it so,";

            // Verify that a query against the default field results in text being
            // highlighted
            // regardless of the field name.

            PhraseQuery q = new PhraseQuery();
            q.Slop = (3);
            q.Add(new Term(FIELD_NAME, "world"));
            q.Add(new Term(FIELD_NAME, "flatland"));

            String expected = "I call our <B>world</B> <B>Flatland</B>, not because we call it so,";
            String observed = highlightField(q, "SOME_FIELD_NAME", s1);
            if (Verbose) Console.WriteLine("Expected: \"" + expected + "\n" + "Observed: \"" + observed);
            assertEquals("Query in the default field results in text for *ANY* field being highlighted",
                expected, observed);

            // Verify that a query against a named field does not result in any
            // highlighting
            // when the query field name differs from the name of the field being
            // highlighted,
            // which in this example happens to be the default field name.
            q = new PhraseQuery();
            q.Slop = (3);
            q.Add(new Term("text", "world"));
            q.Add(new Term("text", "flatland"));

            expected = s1;
            observed = highlightField(q, FIELD_NAME, s1);
            if (Verbose) Console.WriteLine("Expected: \"" + expected + "\n" + "Observed: \"" + observed);
            assertEquals(
                "Query in a named field does not result in highlighting when that field isn't in the query",
                s1, highlightField(q, FIELD_NAME, s1));
        }

        /**
         * This method intended for use with <tt>testHighlightingWithDefaultField()</tt>
         */
        private String highlightField(Query query, String fieldName, String text)
        {
            TokenStream tokenStream = new MockAnalyzer(Random, MockTokenizer.SIMPLE, true, MockTokenFilter.ENGLISH_STOPSET)
                .GetTokenStream(fieldName, text);
            // Assuming "<B>", "</B>" used to highlight
            SimpleHTMLFormatter formatter = new SimpleHTMLFormatter();
            QueryScorer scorer = new QueryScorer(query, fieldName, FIELD_NAME);
            Highlighter highlighter = new Highlighter(formatter, scorer);
            highlighter.TextFragmenter = (new SimpleFragmenter(int.MaxValue));

            String rv = highlighter.GetBestFragments(tokenStream, text, 1, "(FIELD TEXT TRUNCATED)");
            return rv.Length == 0 ? text : rv;
        }

        [Test]
        public void TestSimpleSpanHighlighter()
        {
            doSearching(new TermQuery(new Term(FIELD_NAME, "kennedy")));

            int maxNumFragmentsRequired = 2;

            QueryScorer scorer = new QueryScorer(query, FIELD_NAME);
            Highlighter highlighter = new Highlighter(scorer);

            for (int i = 0; i < hits.TotalHits; i++)
            {
                String text = searcher.Doc(hits.ScoreDocs[i].Doc).Get(FIELD_NAME);
                TokenStream tokenStream = analyzer.GetTokenStream(FIELD_NAME, text);
                highlighter.TextFragmenter = (new SimpleFragmenter(40));

                String result = highlighter.GetBestFragments(tokenStream, text, maxNumFragmentsRequired,
                    "...");
                if (Verbose) Console.WriteLine("\t" + result);
            }

            // Not sure we can assert anything here - just running to check we dont
            // throw any exceptions
        }

        // LUCENE-1752
        [Test]
        public void TestRepeatingTermsInMultBooleans()
        {
            String content = "x y z a b c d e f g b c g";
            String f1 = "f1";
            String f2 = "f2";

            PhraseQuery f1ph1 = new PhraseQuery();
            f1ph1.Add(new Term(f1, "a"));
            f1ph1.Add(new Term(f1, "b"));
            f1ph1.Add(new Term(f1, "c"));
            f1ph1.Add(new Term(f1, "d"));

            PhraseQuery f2ph1 = new PhraseQuery();
            f2ph1.Add(new Term(f2, "a"));
            f2ph1.Add(new Term(f2, "b"));
            f2ph1.Add(new Term(f2, "c"));
            f2ph1.Add(new Term(f2, "d"));

            PhraseQuery f1ph2 = new PhraseQuery();
            f1ph2.Add(new Term(f1, "b"));
            f1ph2.Add(new Term(f1, "c"));
            f1ph2.Add(new Term(f1, "g"));

            PhraseQuery f2ph2 = new PhraseQuery();
            f2ph2.Add(new Term(f2, "b"));
            f2ph2.Add(new Term(f2, "c"));
            f2ph2.Add(new Term(f2, "g"));

            BooleanQuery booleanQuery = new BooleanQuery();
            BooleanQuery leftChild = new BooleanQuery();
            leftChild.Add(f1ph1, Occur.SHOULD);
            leftChild.Add(f2ph1, Occur.SHOULD);
            booleanQuery.Add(leftChild, Occur.MUST);

            BooleanQuery rightChild = new BooleanQuery();
            rightChild.Add(f1ph2, Occur.SHOULD);
            rightChild.Add(f2ph2, Occur.SHOULD);
            booleanQuery.Add(rightChild, Occur.MUST);

            QueryScorer scorer = new QueryScorer(booleanQuery, f1);
            scorer.ExpandMultiTermQuery = (false);

            Highlighter h = new Highlighter(this, scorer);

            Analyzer analyzer = new MockAnalyzer(Random, MockTokenizer.WHITESPACE, false);

            h.GetBestFragment(analyzer, f1, content);

            assertTrue("Failed to find correct number of highlights " + numHighlights + " found",
                numHighlights == 7);
        }

        [Test]
        public void TestSimpleQueryScorerPhraseHighlighting()
        {
            PhraseQuery phraseQuery = new PhraseQuery();
            phraseQuery.Add(new Term(FIELD_NAME, "very"));
            phraseQuery.Add(new Term(FIELD_NAME, "long"));
            phraseQuery.Add(new Term(FIELD_NAME, "contains"), 3);
            doSearching(phraseQuery);

            int maxNumFragmentsRequired = 2;

            QueryScorer scorer = new QueryScorer(query, FIELD_NAME);
            Highlighter highlighter = new Highlighter(this, scorer);

            for (int i = 0; i < hits.TotalHits; i++)
            {
                String text = searcher.Doc(hits.ScoreDocs[i].Doc).Get(FIELD_NAME);
                TokenStream tokenStream = analyzer.GetTokenStream(FIELD_NAME, text);

                highlighter.TextFragmenter = (new SimpleFragmenter(40));

                String result = highlighter.GetBestFragments(tokenStream, text, maxNumFragmentsRequired,
                    "...");
                if (Verbose) Console.WriteLine("\t" + result);
            }

            assertTrue("Failed to find correct number of highlights " + numHighlights + " found",
                numHighlights == 3);

            numHighlights = 0;

            phraseQuery = new PhraseQuery();
            phraseQuery.Add(new Term(FIELD_NAME, "piece"), 1);
            phraseQuery.Add(new Term(FIELD_NAME, "text"), 3);
            phraseQuery.Add(new Term(FIELD_NAME, "refers"), 4);
            phraseQuery.Add(new Term(FIELD_NAME, "kennedy"), 6);

            doSearching(phraseQuery);

            maxNumFragmentsRequired = 2;

            scorer = new QueryScorer(query, FIELD_NAME);
            highlighter = new Highlighter(this, scorer);

            for (int i = 0; i < hits.TotalHits; i++)
            {
                String text = searcher.Doc(hits.ScoreDocs[i].Doc).Get(FIELD_NAME);
                TokenStream tokenStream = analyzer.GetTokenStream(FIELD_NAME, text);

                highlighter.TextFragmenter = (new SimpleFragmenter(40));

                String result = highlighter.GetBestFragments(tokenStream, text, maxNumFragmentsRequired,
                    "...");
                if (Verbose) Console.WriteLine("\t" + result);
            }

            assertTrue("Failed to find correct number of highlights " + numHighlights + " found",
                numHighlights == 4);

            numHighlights = 0;

            phraseQuery = new PhraseQuery();
            phraseQuery.Add(new Term(FIELD_NAME, "lets"));
            phraseQuery.Add(new Term(FIELD_NAME, "lets"), 4);
            phraseQuery.Add(new Term(FIELD_NAME, "lets"), 8);
            phraseQuery.Add(new Term(FIELD_NAME, "lets"), 12);

            doSearching(phraseQuery);

            maxNumFragmentsRequired = 2;

            scorer = new QueryScorer(query, FIELD_NAME);
            highlighter = new Highlighter(this, scorer);

            for (int i = 0; i < hits.TotalHits; i++)
            {
                String text = searcher.Doc(hits.ScoreDocs[i].Doc).Get(FIELD_NAME);
                TokenStream tokenStream = analyzer.GetTokenStream(FIELD_NAME, text);

                highlighter.TextFragmenter = (new SimpleFragmenter(40));

                String result = highlighter.GetBestFragments(tokenStream, text, maxNumFragmentsRequired,
                    "...");
                if (Verbose) Console.WriteLine("\t" + result);
            }

            assertTrue("Failed to find correct number of highlights " + numHighlights + " found",
                numHighlights == 4);

        }

        [Test]
        public void TestSpanRegexQuery()
        {
            query = new SpanOrQuery(new SpanMultiTermQueryWrapper<RegexpQuery>(new RegexpQuery(new Term(FIELD_NAME, "ken.*"))));
            searcher = NewSearcher(reader);
            hits = searcher.Search(query, 100);
            int maxNumFragmentsRequired = 2;

            QueryScorer scorer = new QueryScorer(query, FIELD_NAME);
            Highlighter highlighter = new Highlighter(this, scorer);

            for (int i = 0; i < hits.TotalHits; i++)
            {
                String text = searcher.Doc(hits.ScoreDocs[i].Doc).Get(FIELD_NAME);
                TokenStream tokenStream = analyzer.GetTokenStream(FIELD_NAME, text);

                highlighter.TextFragmenter = (new SimpleFragmenter(40));

                String result = highlighter.GetBestFragments(tokenStream, text, maxNumFragmentsRequired,
                    "...");
                if (Verbose) Console.WriteLine("\t" + result);
            }


            assertTrue("Failed to find correct number of highlights " + numHighlights + " found",
                numHighlights == 5);
        }

        [Test]
        public void TestRegexQuery()
        {
            query = new RegexpQuery(new Term(FIELD_NAME, "ken.*"));
            searcher = NewSearcher(reader);
            hits = searcher.Search(query, 100);
            int maxNumFragmentsRequired = 2;

            QueryScorer scorer = new QueryScorer(query, FIELD_NAME);
            Highlighter highlighter = new Highlighter(this, scorer);

            for (int i = 0; i < hits.TotalHits; i++)
            {
                String text = searcher.Doc(hits.ScoreDocs[i].Doc).Get(FIELD_NAME);
                TokenStream tokenStream = analyzer.GetTokenStream(FIELD_NAME, text);

                highlighter.TextFragmenter = (new SimpleFragmenter(40));

                String result = highlighter.GetBestFragments(tokenStream, text, maxNumFragmentsRequired,
                    "...");
                if (Verbose) Console.WriteLine("\t" + result);
            }


            assertTrue("Failed to find correct number of highlights " + numHighlights + " found",
                numHighlights == 5);
        }

        [Test]
        public void TestExternalReader()
        {
            query = new RegexpQuery(new Term(FIELD_NAME, "ken.*"));
            searcher = NewSearcher(reader);
            hits = searcher.Search(query, 100);
            int maxNumFragmentsRequired = 2;

            QueryScorer scorer = new QueryScorer(query, reader, FIELD_NAME);
            Highlighter highlighter = new Highlighter(this, scorer);

            for (int i = 0; i < hits.TotalHits; i++)
            {
                String text = searcher.Doc(hits.ScoreDocs[i].Doc).Get(FIELD_NAME);
                TokenStream tokenStream = analyzer.GetTokenStream(FIELD_NAME, text);

                highlighter.TextFragmenter = (new SimpleFragmenter(40));

                String result = highlighter.GetBestFragments(tokenStream, text, maxNumFragmentsRequired,
                    "...");
                if (Verbose) Console.WriteLine("\t" + result);
            }


            assertTrue(reader.DocFreq(new Term(FIELD_NAME, "hello")) > 0);
            assertTrue("Failed to find correct number of highlights " + numHighlights + " found",
                numHighlights == 5);
        }

        [Test]
        public void TestNumericRangeQuery()
        {
            // doesn't currently highlight, but make sure it doesn't cause exception either
            query = NumericRangeQuery.NewInt32Range(NUMERIC_FIELD_NAME, 2, 6, true, true);
            searcher = NewSearcher(reader);
            hits = searcher.Search(query, 100);
            int maxNumFragmentsRequired = 2;

            QueryScorer scorer = new QueryScorer(query, FIELD_NAME);
            Highlighter highlighter = new Highlighter(this, scorer);

            for (int i = 0; i < hits.TotalHits; i++)
            {
                String text = searcher.Doc(hits.ScoreDocs[i].Doc).GetField(NUMERIC_FIELD_NAME).GetStringValue(CultureInfo.InvariantCulture);
                TokenStream tokenStream = analyzer.GetTokenStream(FIELD_NAME, text);

                highlighter.TextFragmenter = (new SimpleFragmenter(40));

                //      String result = 
                highlighter.GetBestFragments(tokenStream, text, maxNumFragmentsRequired, "...");
                //if (VERBOSE) Console.WriteLine("\t" + result);
            }
        }

        [Test]
        public void TestSimpleQueryScorerPhraseHighlighting2()
        {
            PhraseQuery phraseQuery = new PhraseQuery();
            phraseQuery.Slop = (5);
            phraseQuery.Add(new Term(FIELD_NAME, "text"));
            phraseQuery.Add(new Term(FIELD_NAME, "piece"));
            phraseQuery.Add(new Term(FIELD_NAME, "long"));
            doSearching(phraseQuery);

            int maxNumFragmentsRequired = 2;

            QueryScorer scorer = new QueryScorer(query, FIELD_NAME);
            Highlighter highlighter = new Highlighter(this, scorer);
            highlighter.TextFragmenter = (new SimpleFragmenter(40));

            for (int i = 0; i < hits.TotalHits; i++)
            {
                String text = searcher.Doc(hits.ScoreDocs[i].Doc).Get(FIELD_NAME);
                TokenStream tokenStream = analyzer.GetTokenStream(FIELD_NAME, text);

                String result = highlighter.GetBestFragments(tokenStream, text, maxNumFragmentsRequired,
                    "...");
                if (Verbose) Console.WriteLine("\t" + result);
            }

            assertTrue("Failed to find correct number of highlights " + numHighlights + " found",
                numHighlights == 6);
        }

        [Test]
        public void TestSimpleQueryScorerPhraseHighlighting3()
        {
            PhraseQuery phraseQuery = new PhraseQuery();
            phraseQuery.Add(new Term(FIELD_NAME, "x"));
            phraseQuery.Add(new Term(FIELD_NAME, "y"));
            phraseQuery.Add(new Term(FIELD_NAME, "z"));
            doSearching(phraseQuery);

            int maxNumFragmentsRequired = 2;

            for (int i = 0; i < hits.TotalHits; i++)
            {
                String text = searcher.Doc(hits.ScoreDocs[i].Doc).Get(FIELD_NAME);
                TokenStream tokenStream = analyzer.GetTokenStream(FIELD_NAME, text);
                QueryScorer scorer = new QueryScorer(query, FIELD_NAME);
                Highlighter highlighter = new Highlighter(this, scorer);

                highlighter.TextFragmenter = (new SimpleFragmenter(40));

                String result = highlighter.GetBestFragments(tokenStream, text, maxNumFragmentsRequired,
                    "...");
                if (Verbose) Console.WriteLine("\t" + result);

                assertTrue("Failed to find correct number of highlights " + numHighlights + " found",
                    numHighlights == 3);
            }
        }

        [Test]
        public void TestSimpleSpanFragmenter()
        {
            PhraseQuery phraseQuery = new PhraseQuery();
            phraseQuery.Add(new Term(FIELD_NAME, "piece"));
            phraseQuery.Add(new Term(FIELD_NAME, "text"), 2);
            phraseQuery.Add(new Term(FIELD_NAME, "very"), 5);
            phraseQuery.Add(new Term(FIELD_NAME, "long"), 6);
            doSearching(phraseQuery);

            int maxNumFragmentsRequired = 2;

            QueryScorer scorer = new QueryScorer(query, FIELD_NAME);
            Highlighter highlighter = new Highlighter(this, scorer);

            for (int i = 0; i < hits.TotalHits; i++)
            {
                String text = searcher.Doc(hits.ScoreDocs[i].Doc).Get(FIELD_NAME);
                TokenStream tokenStream = analyzer.GetTokenStream(FIELD_NAME, text);

                highlighter.TextFragmenter = (new SimpleSpanFragmenter(scorer, 5));

                String result = highlighter.GetBestFragments(tokenStream, text,
                    maxNumFragmentsRequired, "...");
                if (Verbose) Console.WriteLine("\t" + result);

            }

            phraseQuery = new PhraseQuery();
            phraseQuery.Add(new Term(FIELD_NAME, "been"));
            phraseQuery.Add(new Term(FIELD_NAME, "shot"));

            doSearching(query);

            maxNumFragmentsRequired = 2;

            scorer = new QueryScorer(query, FIELD_NAME);
            highlighter = new Highlighter(this, scorer);

            for (int i = 0; i < hits.TotalHits; i++)
            {
                String text = searcher.Doc(hits.ScoreDocs[i].Doc).Get(FIELD_NAME);
                TokenStream tokenStream = analyzer.GetTokenStream(FIELD_NAME, text);

                highlighter.TextFragmenter = (new SimpleSpanFragmenter(scorer, 20));

                String result = highlighter.GetBestFragments(tokenStream, text,
                    maxNumFragmentsRequired, "...");
                if (Verbose) Console.WriteLine("\t" + result);

            }
        }

        // position sensitive query added after position insensitive query
        [Test]
        public void TestPosTermStdTerm()
        {
            BooleanQuery booleanQuery = new BooleanQuery();
            booleanQuery.Add(new TermQuery(new Term(FIELD_NAME, "y")), Occur.SHOULD);

            PhraseQuery phraseQuery = new PhraseQuery();
            phraseQuery.Add(new Term(FIELD_NAME, "x"));
            phraseQuery.Add(new Term(FIELD_NAME, "y"));
            phraseQuery.Add(new Term(FIELD_NAME, "z"));
            booleanQuery.Add(phraseQuery, Occur.SHOULD);

            doSearching(booleanQuery);

            int maxNumFragmentsRequired = 2;

            QueryScorer scorer = new QueryScorer(query, FIELD_NAME);
            Highlighter highlighter = new Highlighter(this, scorer);

            for (int i = 0; i < hits.TotalHits; i++)
            {
                String text = searcher.Doc(hits.ScoreDocs[i].Doc).Get(FIELD_NAME);
                TokenStream tokenStream = analyzer.GetTokenStream(FIELD_NAME, text);

                highlighter.TextFragmenter = (new SimpleFragmenter(40));

                String result = highlighter.GetBestFragments(tokenStream, text, maxNumFragmentsRequired,
                    "...");
                if (Verbose) Console.WriteLine("\t" + result);

                assertTrue("Failed to find correct number of highlights " + numHighlights + " found",
                    numHighlights == 4);
            }
        }

        [Test]
        public void TestQueryScorerMultiPhraseQueryHighlighting()
        {
            MultiPhraseQuery mpq = new MultiPhraseQuery();

            mpq.Add(new Term[] { new Term(FIELD_NAME, "wordx"), new Term(FIELD_NAME, "wordb") });
            mpq.Add(new Term(FIELD_NAME, "wordy"));

            doSearching(mpq);

            int maxNumFragmentsRequired = 2;
            assertExpectedHighlightCount(maxNumFragmentsRequired, 6);
        }

        [Test]
        public void TestQueryScorerMultiPhraseQueryHighlightingWithGap()
        {
            MultiPhraseQuery mpq = new MultiPhraseQuery();

            /*
             * The toString of MultiPhraseQuery doesn't work so well with these
             * out-of-order additions, but the Query itself seems to match accurately.
             */

            mpq.Add(new Term[] { new Term(FIELD_NAME, "wordz") }, 2);
            mpq.Add(new Term[] { new Term(FIELD_NAME, "wordx") }, 0);

            doSearching(mpq);

            int maxNumFragmentsRequired = 1;
            int expectedHighlights = 2;

            assertExpectedHighlightCount(maxNumFragmentsRequired, expectedHighlights);
        }

        [Test]
        public void TestNearSpanSimpleQuery()
        {
            doSearching(new SpanNearQuery(new SpanQuery[] {
                new SpanTermQuery(new Term(FIELD_NAME, "beginning")),
                new SpanTermQuery(new Term(FIELD_NAME, "kennedy")) }, 3, false));

            TestHighlightRunner helper = new TestHighlightRunner((instance) =>
            {
                instance.mode = TestHighlightRunner.QUERY;
                instance.DoStandardHighlights(analyzer, searcher, hits, query, this);
            });

            helper.Run();

            assertTrue("Failed to find correct number of highlights " + numHighlights + " found",
                numHighlights == 2);
        }

        [Test]
        public void TestSimpleQueryTermScorerHighlighter()
        {
            doSearching(new TermQuery(new Term(FIELD_NAME, "kennedy")));
            Highlighter highlighter = new Highlighter(new QueryTermScorer(query));
            highlighter.TextFragmenter = (new SimpleFragmenter(40));
            int maxNumFragmentsRequired = 2;
            for (int i = 0; i < hits.TotalHits; i++)
            {
                String text = searcher.Doc(hits.ScoreDocs[i].Doc).Get(FIELD_NAME);
                TokenStream tokenStream = analyzer.GetTokenStream(FIELD_NAME, text);

                String result = highlighter.GetBestFragments(tokenStream, text, maxNumFragmentsRequired,
                    "...");
                if (Verbose) Console.WriteLine("\t" + result);
            }
            // Not sure we can assert anything here - just running to check we dont
            // throw any exceptions
        }

        [Test]
        public void TestSpanHighlighting()
        {
            Query query1 = new SpanNearQuery(new SpanQuery[] {
                new SpanTermQuery(new Term(FIELD_NAME, "wordx")),
                new SpanTermQuery(new Term(FIELD_NAME, "wordy")) }, 1, false);
            Query query2 = new SpanNearQuery(new SpanQuery[] {
                new SpanTermQuery(new Term(FIELD_NAME, "wordy")),
                new SpanTermQuery(new Term(FIELD_NAME, "wordc")) }, 1, false);
            BooleanQuery bquery = new BooleanQuery();
            bquery.Add(query1, Occur.SHOULD);
            bquery.Add(query2, Occur.SHOULD);
            doSearching(bquery);

            TestHighlightRunner helper = new TestHighlightRunner((instance) =>
            {
                instance.mode = TestHighlightRunner.QUERY;
                instance.DoStandardHighlights(analyzer, searcher, hits, query, this);
            });

            helper.Run();
            assertTrue("Failed to find correct number of highlights " + numHighlights + " found",
                numHighlights == 7);
        }

        [Test]
        public void TestNotSpanSimpleQuery()
        {
            doSearching(new SpanNotQuery(new SpanNearQuery(new SpanQuery[] {
                new SpanTermQuery(new Term(FIELD_NAME, "shot")),
                new SpanTermQuery(new Term(FIELD_NAME, "kennedy")) }, 3, false), new SpanTermQuery(
                new Term(FIELD_NAME, "john"))));

            TestHighlightRunner helper = new TestHighlightRunner((instance) =>
            {
                instance.mode = TestHighlightRunner.QUERY;
                instance.DoStandardHighlights(analyzer, searcher, hits, query, this);
            });

            helper.Run();
            assertTrue("Failed to find correct number of highlights " + numHighlights + " found",
                numHighlights == 4);
        }

        [Test]
        public void TestGetBestFragmentsSimpleQuery()
        {
            TestHighlightRunner helper = new TestHighlightRunner((instance) =>
            {
                numHighlights = 0;
                doSearching(new TermQuery(new Term(FIELD_NAME, "kennedy")));

                instance.DoStandardHighlights(analyzer, searcher, hits, query, this);
                assertTrue("Failed to find correct number of highlights " + numHighlights + " found",
                    numHighlights == 4);
            });

            helper.Start();
        }

        [Test]
        public void TestGetBestFragmentsConstantScore()
        {
            TestHighlightRunner helper = new TestHighlightRunner((instance) =>
            {
                numHighlights = 0;
                if (Random.nextBoolean())
                {
                    BooleanQuery bq = new BooleanQuery();
                    bq.Add(new ConstantScoreQuery(new QueryWrapperFilter(new TermQuery(
                        new Term(FIELD_NAME, "kennedy")))), Occur.MUST);
                    bq.Add(new ConstantScoreQuery(new TermQuery(new Term(FIELD_NAME, "kennedy"))), Occur.MUST);
                    doSearching(bq);
                }
                else
                {
                    doSearching(new ConstantScoreQuery(new TermQuery(new Term(FIELD_NAME,
                        "kennedy"))));
                }
                instance.DoStandardHighlights(analyzer, searcher, hits, query, this);
                assertTrue("Failed to find correct number of highlights " + numHighlights + " found",
                        numHighlights == 4);
            });

            helper.Start();
        }

        [Test]
        public void TestGetFuzzyFragments()
        {
            TestHighlightRunner helper = new TestHighlightRunner((instance) =>
            {
                numHighlights = 0;
                FuzzyQuery fuzzyQuery = new FuzzyQuery(new Term(FIELD_NAME, "kinnedy"), 2);
                fuzzyQuery.MultiTermRewriteMethod = MultiTermQuery.SCORING_BOOLEAN_QUERY_REWRITE;
                doSearching(fuzzyQuery);
                instance.DoStandardHighlights(analyzer, searcher, hits, query, this, true);
                assertTrue("Failed to find correct number of highlights " + numHighlights + " found",
                    numHighlights == 4);
            });

            helper.Start();
        }

        [Test]
        public void TestGetWildCardFragments()
        {
            TestHighlightRunner helper = new TestHighlightRunner((instance) =>
            {
                numHighlights = 0;
                WildcardQuery wildcardQuery = new WildcardQuery(new Term(FIELD_NAME, "k?nnedy"));
                wildcardQuery.MultiTermRewriteMethod = MultiTermQuery.SCORING_BOOLEAN_QUERY_REWRITE;
                doSearching(wildcardQuery);
                instance.DoStandardHighlights(analyzer, searcher, hits, query, this);
                assertTrue("Failed to find correct number of highlights " + numHighlights + " found",
                    numHighlights == 4);
            });

            helper.Start();
        }

        [Test]
        public void TestGetMidWildCardFragments()
        {
            TestHighlightRunner helper = new TestHighlightRunner((instance) =>
            {
                numHighlights = 0;
                WildcardQuery wildcardQuery = new WildcardQuery(new Term(FIELD_NAME, "k*dy"));
                wildcardQuery.MultiTermRewriteMethod = MultiTermQuery.SCORING_BOOLEAN_QUERY_REWRITE;
                doSearching(wildcardQuery);
                instance.DoStandardHighlights(analyzer, searcher, hits, query, this);
                assertTrue("Failed to find correct number of highlights " + numHighlights + " found",
                    numHighlights == 5);
            });

            helper.Start();
        }

        [Test]
        public void TestGetRangeFragments()
        {
            TestHighlightRunner helper = new TestHighlightRunner((instance) =>
            {
                numHighlights = 0;

                // Need to explicitly set the QueryParser property to use TermRangeQuery
                // rather
                // than RangeFilters

                TermRangeQuery rangeQuery = new TermRangeQuery(
                    FIELD_NAME,
                        new BytesRef("kannedy"),
                        new BytesRef("kznnedy"),
                        true, true);
                rangeQuery.MultiTermRewriteMethod = MultiTermQuery.SCORING_BOOLEAN_QUERY_REWRITE;

                query = rangeQuery;
                doSearching(query);

                instance.DoStandardHighlights(analyzer, searcher, hits, query, this);
                assertTrue("Failed to find correct number of highlights " + numHighlights + " found",
                    numHighlights == 5);
            });

            helper.Start();
        }

        [Test]
        public void TestConstantScoreMultiTermQuery()
        {

            numHighlights = 0;

            query = new WildcardQuery(new Term(FIELD_NAME, "ken*"));
            ((WildcardQuery)query).MultiTermRewriteMethod = MultiTermQuery.CONSTANT_SCORE_FILTER_REWRITE;
            searcher = NewSearcher(reader);
            // can't rewrite ConstantScore if you want to highlight it -
            // it rewrites to ConstantScoreQuery which cannot be highlighted
            // query = unReWrittenQuery.rewrite(reader);
            if (Verbose) Console.WriteLine("Searching for: " + query.ToString(FIELD_NAME));
            hits = searcher.Search(query, null, 1000);

            for (int i = 0; i < hits.TotalHits; i++)
            {
                String text = searcher.Doc(hits.ScoreDocs[i].Doc).Get(HighlighterTest.FIELD_NAME);
                int maxNumFragmentsRequired = 2;
                String fragmentSeparator = "...";
                QueryScorer scorer = new QueryScorer(query, HighlighterTest.FIELD_NAME);
                TokenStream tokenStream = analyzer.GetTokenStream(HighlighterTest.FIELD_NAME, text);

                Highlighter highlighter = new Highlighter(this, scorer);

                highlighter.TextFragmenter = (new SimpleFragmenter(20));

                String result = highlighter.GetBestFragments(tokenStream, text, maxNumFragmentsRequired,
                    fragmentSeparator);
                if (Verbose) Console.WriteLine("\t" + result);
            }
            assertTrue("Failed to find correct number of highlights " + numHighlights + " found",
                numHighlights == 5);

            // try null field

            hits = searcher.Search(query, null, 1000);

            numHighlights = 0;

            for (int i = 0; i < hits.TotalHits; i++)
            {
                String text = searcher.Doc(hits.ScoreDocs[i].Doc).Get(HighlighterTest.FIELD_NAME);
                int maxNumFragmentsRequired = 2;
                String fragmentSeparator = "...";
                QueryScorer scorer = new QueryScorer(query, null);
                TokenStream tokenStream = analyzer.GetTokenStream(HighlighterTest.FIELD_NAME, text);

                Highlighter highlighter = new Highlighter(this, scorer);

                highlighter.TextFragmenter = (new SimpleFragmenter(20));

                String result = highlighter.GetBestFragments(tokenStream, text, maxNumFragmentsRequired,
                    fragmentSeparator);
                if (Verbose) Console.WriteLine("\t" + result);
            }
            assertTrue("Failed to find correct number of highlights " + numHighlights + " found",
                numHighlights == 5);

            // try default field

            hits = searcher.Search(query, null, 1000);

            numHighlights = 0;

            for (int i = 0; i < hits.TotalHits; i++)
            {
                String text = searcher.Doc(hits.ScoreDocs[i].Doc).Get(HighlighterTest.FIELD_NAME);
                int maxNumFragmentsRequired = 2;
                String fragmentSeparator = "...";
                QueryScorer scorer = new QueryScorer(query, "random_field", HighlighterTest.FIELD_NAME);
                TokenStream tokenStream = analyzer.GetTokenStream(HighlighterTest.FIELD_NAME, text);

                Highlighter highlighter = new Highlighter(this, scorer);

                highlighter.TextFragmenter = (new SimpleFragmenter(20));

                String result = highlighter.GetBestFragments(tokenStream, text, maxNumFragmentsRequired,
                    fragmentSeparator);
                if (Verbose) Console.WriteLine("\t" + result);
            }
            assertTrue("Failed to find correct number of highlights " + numHighlights + " found",
                numHighlights == 5);
        }

        [Test]
        public void TestGetBestFragmentsPhrase()
        {
            TestHighlightRunner helper = new TestHighlightRunner((instance) =>
            {
                numHighlights = 0;
                PhraseQuery phraseQuery = new PhraseQuery();
                phraseQuery.Add(new Term(FIELD_NAME, "john"));
                phraseQuery.Add(new Term(FIELD_NAME, "kennedy"));
                doSearching(phraseQuery);
                instance.DoStandardHighlights(analyzer, searcher, hits, query, this);
                // Currently highlights "John" and "Kennedy" separately
                assertTrue("Failed to find correct number of highlights " + numHighlights + " found",
                    numHighlights == 2);
            });

            helper.Start();
        }

        [Test]
        public void TestGetBestFragmentsQueryScorer()
        {
            TestHighlightRunner helper = new TestHighlightRunner((instance) =>
            {
                numHighlights = 0;
                SpanQuery[] clauses = {
                        new SpanTermQuery(new Term("contents", "john")),
                            new SpanTermQuery(new Term("contents", "kennedy")), };

                SpanNearQuery snq = new SpanNearQuery(clauses, 1, true);
                doSearching(snq);
                instance.DoStandardHighlights(analyzer, searcher, hits, query, this);
                // Currently highlights "John" and "Kennedy" separately
                assertTrue("Failed to find correct number of highlights " + numHighlights + " found",
                    numHighlights == 2);
            });

            helper.Start();
        }

        [Test]
        public void TestOffByOne()
        {
            TestHighlightRunner helper = new TestHighlightRunner((instance) =>
            {
                TermQuery query = new TermQuery(new Term("data", "help"));
                Highlighter hg = new Highlighter(new SimpleHTMLFormatter(), new QueryTermScorer(query));
                hg.TextFragmenter = (new NullFragmenter());

                String match = hg.GetBestFragment(analyzer, "data", "help me [54-65]");
                assertEquals("<B>help</B> me [54-65]", match);
            });

            helper.Start();
        }

        [Test]
        public void TestGetBestFragmentsFilteredQuery()
        {
            TestHighlightRunner helper = new TestHighlightRunner((instance) =>
            {
                numHighlights = 0;
                TermRangeFilter rf = TermRangeFilter.NewStringRange("contents", "john", "john", true, true);
                SpanQuery[] clauses = {
                        new SpanTermQuery(new Term("contents", "john")),
                            new SpanTermQuery(new Term("contents", "kennedy")), };
                SpanNearQuery snq = new SpanNearQuery(clauses, 1, true);
                FilteredQuery fq = new FilteredQuery(snq, rf);

                doSearching(fq);
                instance.DoStandardHighlights(analyzer, searcher, hits, query, this);
                // Currently highlights "John" and "Kennedy" separately
                assertTrue("Failed to find correct number of highlights " + numHighlights + " found",
                    numHighlights == 2);
            });

            helper.Start();
        }

        [Test]
        public void TestGetBestFragmentsFilteredPhraseQuery()
        {
            TestHighlightRunner helper = new TestHighlightRunner((instance) =>
            {
                numHighlights = 0;
                TermRangeFilter rf = TermRangeFilter.NewStringRange("contents", "john", "john", true, true);
                PhraseQuery pq = new PhraseQuery();
                pq.Add(new Term("contents", "john"));
                pq.Add(new Term("contents", "kennedy"));
                FilteredQuery fq = new FilteredQuery(pq, rf);

                doSearching(fq);
                instance.DoStandardHighlights(analyzer, searcher, hits, query, this);
                // Currently highlights "John" and "Kennedy" separately
                assertTrue("Failed to find correct number of highlights " + numHighlights + " found",
                    numHighlights == 2);
            });

            helper.Start();
        }

        [Test]
        public void TestGetBestFragmentsMultiTerm()
        {
            TestHighlightRunner helper = new TestHighlightRunner((instance) =>
            {
                numHighlights = 0;
                BooleanQuery booleanQuery = new BooleanQuery();
                booleanQuery.Add(new TermQuery(new Term(FIELD_NAME, "john")), Occur.SHOULD);
                PrefixQuery prefixQuery = new PrefixQuery(new Term(FIELD_NAME, "kenn"));
                prefixQuery.MultiTermRewriteMethod = MultiTermQuery.SCORING_BOOLEAN_QUERY_REWRITE;
                booleanQuery.Add(prefixQuery, Occur.SHOULD);

                doSearching(booleanQuery);
                instance.DoStandardHighlights(analyzer, searcher, hits, query, this);
                assertTrue("Failed to find correct number of highlights " + numHighlights + " found",
                    numHighlights == 5);
            });

            helper.Start();
        }

        [Test]
        public void TestGetBestFragmentsWithOr()
        {
            TestHighlightRunner helper = new TestHighlightRunner((instance) =>
            {
                numHighlights = 0;

                BooleanQuery query = new BooleanQuery();
                query.Add(new TermQuery(new Term(FIELD_NAME, "jfk")), Occur.SHOULD);
                query.Add(new TermQuery(new Term(FIELD_NAME, "kennedy")), Occur.SHOULD);

                doSearching(query);
                instance.DoStandardHighlights(analyzer, searcher, hits, query, this);
                assertTrue("Failed to find correct number of highlights " + numHighlights + " found",
                    numHighlights == 5);
            });

            helper.Start();
        }

        [Test]
        public void TestGetBestSingleFragment()
        {
            TestHighlightRunner helper = new TestHighlightRunner((instance) =>
            {
                doSearching(new TermQuery(new Term(FIELD_NAME, "kennedy")));
                numHighlights = 0;
                for (int i = 0; i < hits.TotalHits; i++)
                {
                    String text = searcher.Doc(hits.ScoreDocs[i].Doc).Get(FIELD_NAME);
                    TokenStream tokenStream = analyzer.GetTokenStream(FIELD_NAME, text);

                    Highlighter highlighter = instance.GetHighlighter(query, FIELD_NAME,
                        this);
                    highlighter.TextFragmenter = (new SimpleFragmenter(40));
                    String result = highlighter.GetBestFragment(tokenStream, text);
                    if (Verbose) Console.WriteLine("\t" + result);
                }
                assertTrue("Failed to find correct number of highlights " + numHighlights + " found",
                    numHighlights == 4);

                numHighlights = 0;
                for (int i = 0; i < hits.TotalHits; i++)
                {
                    String text = searcher.Doc(hits.ScoreDocs[i].Doc).Get(FIELD_NAME);
                    Highlighter highlighter = instance.GetHighlighter(query, FIELD_NAME,
                        this);
                    highlighter.GetBestFragment(analyzer, FIELD_NAME, text);
                }
                assertTrue("Failed to find correct number of highlights " + numHighlights + " found",
                    numHighlights == 4);

                numHighlights = 0;
                for (int i = 0; i < hits.TotalHits; i++)
                {
                    String text = searcher.Doc(hits.ScoreDocs[i].Doc).Get(FIELD_NAME);

                    Highlighter highlighter = instance.GetHighlighter(query, FIELD_NAME,
                        this);
                    highlighter.GetBestFragments(analyzer, FIELD_NAME, text, 10);
                }
                assertTrue("Failed to find correct number of highlights " + numHighlights + " found",
                    numHighlights == 4);
            });

            helper.Start();

        }

        [Test]
        public void TestGetBestSingleFragmentWithWeights()
        {
            TestHighlightRunner helper = new TestHighlightRunner((instance) =>
            {
                WeightedSpanTerm[]
                wTerms = new WeightedSpanTerm[2];
                wTerms[0] = new WeightedSpanTerm(10f, "hello");

                IList<PositionSpan> positionSpans = new JCG.List<PositionSpan>();
                positionSpans.Add(new PositionSpan(0, 0));
                wTerms[0].AddPositionSpans(positionSpans);

                wTerms[1] = new WeightedSpanTerm(1f, "kennedy");
                positionSpans = new JCG.List<PositionSpan>();
                positionSpans.Add(new PositionSpan(14, 14));
                wTerms[1].AddPositionSpans(positionSpans);

                Highlighter highlighter = instance.GetHighlighter(wTerms, this);// new
                                                                                // Highlighter(new
                                                                                // QueryTermScorer(wTerms));
                TokenStream tokenStream = analyzer.GetTokenStream(FIELD_NAME, texts[0]);
                highlighter.TextFragmenter = (new SimpleFragmenter(2));

                String result = highlighter.GetBestFragment(tokenStream, texts[0]).Trim();
                assertTrue("Failed to find best section using weighted terms. Found: [" + result + "]",
                    "<B>Hello</B>".Equals(result, StringComparison.Ordinal));

                // readjust weights
                wTerms[1].Weight = (50f);
                tokenStream = analyzer.GetTokenStream(FIELD_NAME, texts[0]);
                highlighter = instance.GetHighlighter(wTerms, this);
                highlighter.TextFragmenter = (new SimpleFragmenter(2));

                result = highlighter.GetBestFragment(tokenStream, texts[0]).Trim();
                assertTrue("Failed to find best section using weighted terms. Found: " + result,
                    "<B>kennedy</B>".Equals(result, StringComparison.Ordinal));
            });

            helper.Start();

        }

        // tests a "complex" analyzer that produces multiple
        // overlapping tokens
        [Test]
        public void TestOverlapAnalyzer()
        {
            TestHighlightRunner helper = new TestHighlightRunner((instance) =>
            {
                IDictionary<String, String> synonyms = new JCG.Dictionary<String, String>();
                synonyms["football"] = "soccer,footie";
                Analyzer analyzer = new SynonymAnalyzer(synonyms);

                String s = "football-soccer in the euro 2004 footie competition";

                BooleanQuery query = new BooleanQuery();
                query.Add(new TermQuery(new Term("bookid", "football")), Occur.SHOULD);
                query.Add(new TermQuery(new Term("bookid", "soccer")), Occur.SHOULD);
                query.Add(new TermQuery(new Term("bookid", "footie")), Occur.SHOULD);

                Highlighter highlighter = instance.GetHighlighter(query, null, this);

                // Get 3 best fragments and separate with a "..."
                TokenStream tokenStream = analyzer.GetTokenStream(null, s);

                String result = highlighter.GetBestFragments(tokenStream, s, 3, "...");
                String expectedResult = "<B>football</B>-<B>soccer</B> in the euro 2004 <B>footie</B> competition";
                assertTrue("overlapping analyzer should handle highlights OK, expected:" + expectedResult
                    + " actual:" + result, expectedResult.Equals(result, StringComparison.Ordinal));
            });

            helper.Start();

        }

        [Test]
        public void TestGetSimpleHighlight()
        {
            TestHighlightRunner helper = new TestHighlightRunner((instance) =>
            {
                numHighlights = 0;
                doSearching(new TermQuery(new Term(FIELD_NAME, "kennedy")));
                // new Highlighter(HighlighterTest.this, new QueryTermScorer(query));

                for (int i = 0; i < hits.TotalHits; i++)
                {
                    String text = searcher.Doc(hits.ScoreDocs[i].Doc).Get(FIELD_NAME);
                    TokenStream tokenStream = analyzer.GetTokenStream(FIELD_NAME, text);
                    Highlighter highlighter = instance.GetHighlighter(query, FIELD_NAME,
                        this);
                    String result = highlighter.GetBestFragment(tokenStream, text);
                    if (Verbose) Console.WriteLine("\t" + result);
                }
                assertTrue("Failed to find correct number of highlights " + numHighlights + " found",
                    numHighlights == 4);
            });

            helper.Start();
        }

        [Test]
        public void TestGetTextFragments()
        {
            TestHighlightRunner helper = new TestHighlightRunner((instance) =>
            {
                doSearching(new TermQuery(new Term(FIELD_NAME, "kennedy")));

                for (int i = 0; i < hits.TotalHits; i++)
                {
                    String text = searcher.Doc(hits.ScoreDocs[i].Doc).Get(FIELD_NAME);
                    TokenStream tokenStream = analyzer.GetTokenStream(FIELD_NAME, text);

                    Highlighter highlighter = instance.GetHighlighter(query, FIELD_NAME,
                        this);// new Highlighter(this, new
                              // QueryTermScorer(query));
                    highlighter.TextFragmenter = (new SimpleFragmenter(20));
                    String[] stringResults = highlighter.GetBestFragments(tokenStream, text, 10);

                    tokenStream = analyzer.GetTokenStream(FIELD_NAME, text);
                    TextFragment[] fragmentResults = highlighter.GetBestTextFragments(tokenStream, text,
                        true, 10);

                    assertTrue("Failed to find correct number of text Fragments: " + fragmentResults.Length
                        + " vs " + stringResults.Length, fragmentResults.Length == stringResults.Length);
                    for (int j = 0; j < stringResults.Length; j++)
                    {
                        if (Verbose) Console.WriteLine(fragmentResults[j]);
                        assertTrue("Failed to find same text Fragments: " + fragmentResults[j] + " found",
                            fragmentResults[j].toString().Equals(stringResults[j], StringComparison.Ordinal));

                    }
                }
            });

            helper.Start();
        }

        [Test]
        public void TestMaxSizeHighlight()
        {
            MockAnalyzer analyzer = new MockAnalyzer(Random, MockTokenizer.SIMPLE, true, MockTokenFilter.ENGLISH_STOPSET);
            // we disable MockTokenizer checks because we will forcefully limit the 
            // tokenstream and call end() before incrementToken() returns false.
            analyzer.EnableChecks = (false);

            TestHighlightRunner helper = new TestHighlightRunner((instance) =>
            {
                numHighlights = 0;
                doSearching(new TermQuery(new Term(FIELD_NAME, "meat")));
                TokenStream tokenStream = analyzer.GetTokenStream(FIELD_NAME, texts[0]);
                Highlighter highlighter = instance.GetHighlighter(query, FIELD_NAME,
                    this);// new Highlighter(this, new
                          // QueryTermScorer(query));
                highlighter.MaxDocCharsToAnalyze = (30);

                highlighter.GetBestFragment(tokenStream, texts[0]);
                assertTrue("Setting MaxDocBytesToAnalyze should have prevented "
                    + "us from finding matches for this record: " + numHighlights + " found",
                    numHighlights == 0);
            });

            helper.Start();
        }

        [Test]
        public void TestMaxSizeHighlightTruncates()
        {
            TestHighlightRunner helper = new TestHighlightRunner((instance) =>
            {
                String goodWord = "goodtoken";
                CharacterRunAutomaton stopWords = new CharacterRunAutomaton(BasicAutomata.MakeString("stoppedtoken"));
                // we disable MockTokenizer checks because we will forcefully limit the 
                // tokenstream and call end() before incrementToken() returns false.
                MockAnalyzer analyzer = new MockAnalyzer(Random, MockTokenizer.SIMPLE, true, stopWords);
                analyzer.EnableChecks = (false);
                TermQuery query = new TermQuery(new Term("data", goodWord));

                String match;
                StringBuilder sb = new StringBuilder();
                sb.append(goodWord);
                for (int i = 0; i < 10000; i++)
                {
                    sb.append(" ");
                    // only one stopword
                    sb.append("stoppedtoken");
                }
                SimpleHTMLFormatter fm = new SimpleHTMLFormatter();
                Highlighter hg = instance.GetHighlighter(query, "data", fm);// new Highlighter(fm,
                                                                            // new
                                                                            // QueryTermScorer(query));
                hg.TextFragmenter = (new NullFragmenter());
                hg.MaxDocCharsToAnalyze = (100);
                match = hg.GetBestFragment(analyzer, "data", sb.toString());
                assertTrue("Matched text should be no more than 100 chars in length ", match.Length < hg
                    .MaxDocCharsToAnalyze);

                // add another tokenized word to the overrall length - but set way
                // beyond
                // the length of text under consideration (after a large slug of stop
                // words
                // + whitespace)
                sb.append(" ");
                sb.append(goodWord);
                match = hg.GetBestFragment(analyzer, "data", sb.toString());
                assertTrue("Matched text should be no more than 100 chars in length ", match.Length < hg
                    .MaxDocCharsToAnalyze);
            });

            helper.Start();

        }

        [Test]
        public void TestMaxSizeEndHighlight()
        {
            TestHighlightRunner helper = new TestHighlightRunner((instance) =>
            {
                CharacterRunAutomaton stopWords = new CharacterRunAutomaton(new RegExp("i[nt]").ToAutomaton());
                TermQuery query = new TermQuery(new Term("text", "searchterm"));

                String text = "this is a text with searchterm in it";
                SimpleHTMLFormatter fm = new SimpleHTMLFormatter();
                Highlighter hg = instance.GetHighlighter(query, "text", fm);
                hg.TextFragmenter = (new NullFragmenter());
                hg.MaxDocCharsToAnalyze = (36);
                String match = hg.GetBestFragment(new MockAnalyzer(Random, MockTokenizer.SIMPLE, true, stopWords), "text", text);
                assertTrue(
                    "Matched text should contain remainder of text after highlighted query ",
                    match.EndsWith("in it", StringComparison.Ordinal));
            });

            helper.Start();
        }

        [Test]
        public void TestUnRewrittenQuery()
        {
            TestHighlightRunner helper = new TestHighlightRunner((instance) =>
            {
                numHighlights = 0;
                // test to show how rewritten query can still be used
                searcher = NewSearcher(reader);
                Analyzer analyzer = new MockAnalyzer(Random, MockTokenizer.SIMPLE, true, MockTokenFilter.ENGLISH_STOPSET);

                BooleanQuery query = new BooleanQuery();
                query.Add(new WildcardQuery(new Term(FIELD_NAME, "jf?")), Occur.SHOULD);
                query.Add(new WildcardQuery(new Term(FIELD_NAME, "kenned*")), Occur.SHOULD);

                if (Verbose) Console.WriteLine("Searching with primitive query");
                // forget to set this and...
                // query=query.rewrite(reader);
                TopDocs hits = searcher.Search(query, null, 1000);

                // create an instance of the highlighter with the tags used to surround
                // highlighted text
                // QueryHighlightExtractor highlighter = new
                // QueryHighlightExtractor(this,
                // query, new StandardAnalyzer(TEST_VERSION));

                int maxNumFragmentsRequired = 3;

                for (int i = 0; i < hits.TotalHits; i++)
                {
                    String text = searcher.Doc(hits.ScoreDocs[i].Doc).Get(FIELD_NAME);
                    TokenStream tokenStream = analyzer.GetTokenStream(FIELD_NAME, text);
                    Highlighter highlighter = instance.GetHighlighter(query, FIELD_NAME, this, false);

                    highlighter.TextFragmenter = (new SimpleFragmenter(40));

                    String highlightedText = highlighter.GetBestFragments(tokenStream, text,
                        maxNumFragmentsRequired, "...");

                    if (Verbose) Console.WriteLine(highlightedText);
                }
                // We expect to have zero highlights if the query is multi-terms and is
                // not
                // rewritten!
                assertTrue("Failed to find correct number of highlights " + numHighlights + " found",
                    numHighlights == 0);
            });

            helper.Start();
        }

        [Test]
        public void TestNoFragments()
        {
            TestHighlightRunner helper = new TestHighlightRunner((instance) =>
            {
                doSearching(new TermQuery(new Term(FIELD_NAME, "aninvalidquerywhichshouldyieldnoresults")));

                foreach (String text in texts)
                {
                    TokenStream tokenStream = analyzer.GetTokenStream(FIELD_NAME, text);
                    Highlighter highlighter = instance.GetHighlighter(query, FIELD_NAME,
                        this);
                    String result = highlighter.GetBestFragment(tokenStream, text);
                    assertNull("The highlight result should be null for text with no query terms", result);
                }
            });

            helper.Start();
        }

        /**
         * Demonstrates creation of an XHTML compliant doc using new encoding facilities.
         */
        [Test]
        public void TestEncoding()
        {

            String rawDocContent = "\"Smith & sons' prices < 3 and >4\" claims article";
            // run the highlighter on the raw content (scorer does not score any tokens
            // for
            // highlighting but scores a single fragment for selection

            Highlighter highlighter = new Highlighter(this, new SimpleHTMLEncoder(), new TestEncodingScorerAnonymousClass(this));

            highlighter.TextFragmenter = (new SimpleFragmenter(2000));
            TokenStream tokenStream = analyzer.GetTokenStream(FIELD_NAME, rawDocContent);

            String encodedSnippet = highlighter.GetBestFragments(tokenStream, rawDocContent, 1, "");
            // An ugly bit of XML creation:
            String xhtml = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n"
                + "<html xmlns=\"http://www.w3.org/1999/xhtml\" xml:lang=\"en\" lang=\"en\">\n"
                + "<head>\n" + "<title>My Test HTML Document</title>\n" + "</head>\n" + "<body>\n" + "<h2>"
                + encodedSnippet + "</h2>\n" + "</body>\n" + "</html>";
            // now an ugly built of XML parsing to test the snippet is encoded OK
            //DocumentBuilderFactory dbf = DocumentBuilderFactory.newInstance();
            //DocumentBuilder db = dbf.newDocumentBuilder();
            //org.w3c.dom.Document doc = db.parse(new ByteArrayInputStream(xhtml.getBytes(StandardCharsets.UTF_8)));
            //Element root = doc.getDocumentElement();
            //NodeList nodes = root.getElementsByTagName("body");
            //Element body = (Element)nodes.item(0);
            //nodes = body.getElementsByTagName("h2");
            //Element h2 = (Element)nodes.item(0);

            XmlDocument doc = new XmlDocument();
            doc.Load(new MemoryStream(xhtml.GetBytes(Encoding.UTF8)));
            XmlElement root = doc.DocumentElement;
            XmlNodeList nodes = root.GetElementsByTagName("body");
            XmlElement body = (XmlElement)nodes.Item(0);
            nodes = body.GetElementsByTagName("h2");
            XmlElement h2 = (XmlElement)nodes.Item(0);

            String decodedSnippet = h2.FirstChild.Value;
            assertEquals("XHTML Encoding should have worked:", rawDocContent, decodedSnippet);
        }

        private sealed class TestEncodingScorerAnonymousClass : IScorer
        {
            private readonly HighlighterTest outerInstance;

            public TestEncodingScorerAnonymousClass(HighlighterTest outerInstance)
            {
                this.outerInstance = outerInstance;
            }

            public void StartFragment(TextFragment newFragment)
            {
            }

            public float GetTokenScore()
            {
                return 0;
            }

            public float FragmentScore => 1;

            public TokenStream Init(TokenStream tokenStream)
            {
                return null;
            }
        }

        [Test]
        public void TestFieldSpecificHighlighting()
        {
            TestHighlightRunner helper = new TestHighlightRunner((instance) =>
            {
                String docMainText = "fred is one of the people";

                BooleanQuery query = new BooleanQuery();
                query.Add(new TermQuery(new Term(FIELD_NAME, "fred")), Occur.SHOULD);
                query.Add(new TermQuery(new Term("category", "people")), Occur.SHOULD);

                // highlighting respects fieldnames used in query

                IScorer fieldSpecificScorer = null;
                if (instance.mode == TestHighlightRunner.QUERY)
                {
                    fieldSpecificScorer = new QueryScorer(query, FIELD_NAME);
                }
                else if (instance.mode == TestHighlightRunner.QUERY_TERM)
                {
                    fieldSpecificScorer = new QueryTermScorer(query, "contents");
                }
                Highlighter fieldSpecificHighlighter = new Highlighter(new SimpleHTMLFormatter(),
                    fieldSpecificScorer);
                fieldSpecificHighlighter.TextFragmenter = (new NullFragmenter());
                String result = fieldSpecificHighlighter.GetBestFragment(analyzer, FIELD_NAME, docMainText);
                assertEquals("Should match", result, "<B>fred</B> is one of the people");

                // highlighting does not respect fieldnames used in query
                IScorer fieldInSpecificScorer = null;
                if (instance.mode == TestHighlightRunner.QUERY)
                {
                    fieldInSpecificScorer = new QueryScorer(query, null);
                }
                else if (instance.mode == TestHighlightRunner.QUERY_TERM)
                {
                    fieldInSpecificScorer = new QueryTermScorer(query);
                }

                Highlighter fieldInSpecificHighlighter = new Highlighter(new SimpleHTMLFormatter(),
                    fieldInSpecificScorer);
                fieldInSpecificHighlighter.TextFragmenter = (new NullFragmenter());
                result = fieldInSpecificHighlighter.GetBestFragment(analyzer, FIELD_NAME, docMainText);
                assertEquals("Should match", result, "<B>fred</B> is one of the <B>people</B>");

                reader.Dispose();
            });

            helper.Start();

        }

        protected TokenStream getTS2()
        {
            // String s = "Hi-Speed10 foo";
            return new TS2TokenStreamAnonymousClass();
        }


        private sealed class TS2TokenStreamAnonymousClass : TokenStream
        {
            public TS2TokenStreamAnonymousClass()
            {
                termAtt = AddAttribute<ICharTermAttribute>();
                posIncrAtt = AddAttribute<IPositionIncrementAttribute>();
                offsetAtt = AddAttribute<IOffsetAttribute>();

                lst = new JCG.List<Token>();
                Token t;
                t = createToken("hi", 0, 2);
                t.PositionIncrement = (1);
                lst.Add(t);
                t = createToken("hispeed", 0, 8);
                t.PositionIncrement = (1);
                lst.Add(t);
                t = createToken("speed", 3, 8);
                t.PositionIncrement = (0);
                lst.Add(t);
                t = createToken("10", 8, 10);
                t.PositionIncrement = (1);
                lst.Add(t);
                t = createToken("foo", 11, 14);
                t.PositionIncrement = (1);
                lst.Add(t);
                iter = lst.GetEnumerator();
            }

            IEnumerator<Token> iter;
            internal IList<Token> lst;
            private readonly ICharTermAttribute termAtt;
            private readonly IPositionIncrementAttribute posIncrAtt;
            private readonly IOffsetAttribute offsetAtt;

            public override bool IncrementToken()
            {
                if (iter.MoveNext())
                {
                    Token token = iter.Current;
                    ClearAttributes();
                    termAtt.SetEmpty().Append(token);
                    posIncrAtt.PositionIncrement = (token.PositionIncrement);
                    offsetAtt.SetOffset(token.StartOffset, token.EndOffset);
                    return true;
                }
                return false;
            }

            public override void Reset()
            {
                base.Reset();
                iter = lst.GetEnumerator();
            }
        }

        // same token-stream as above, but the bigger token comes first this time
        protected TokenStream getTS2a()
        {
            // String s = "Hi-Speed10 foo";
            return new TS2aTokenStreamAnonymousClass();
        }

        private sealed class TS2aTokenStreamAnonymousClass : TokenStream
        {
            public TS2aTokenStreamAnonymousClass()
            {
                termAtt = AddAttribute<ICharTermAttribute>();
                posIncrAtt = AddAttribute<IPositionIncrementAttribute>();
                offsetAtt = AddAttribute<IOffsetAttribute>();

                lst = new JCG.List<Token>();
                Token t;
                t = createToken("hispeed", 0, 8);
                t.PositionIncrement = (1);
                lst.Add(t);
                t = createToken("hi", 0, 2);
                t.PositionIncrement = (0);
                lst.Add(t);
                t = createToken("speed", 3, 8);
                t.PositionIncrement = (1);
                lst.Add(t);
                t = createToken("10", 8, 10);
                t.PositionIncrement = (1);
                lst.Add(t);
                t = createToken("foo", 11, 14);
                t.PositionIncrement = (1);
                lst.Add(t);
                iter = lst.GetEnumerator();
            }

            IEnumerator<Token> iter;
            internal IList<Token> lst;
            private readonly ICharTermAttribute termAtt;
            private readonly IPositionIncrementAttribute posIncrAtt;
            private readonly IOffsetAttribute offsetAtt;

            public override bool IncrementToken()
            {
                if (iter.MoveNext())
                {
                    Token token = iter.Current;
                    ClearAttributes();
                    termAtt.SetEmpty().Append(token);
                    posIncrAtt.PositionIncrement = (token.PositionIncrement);
                    offsetAtt.SetOffset(token.StartOffset, token.EndOffset);
                    return true;
                }
                return false;
            }

            public override void Reset()
            {
                base.Reset();
                iter = lst.GetEnumerator();
            }
        }

        [Test]
        public void TestOverlapAnalyzer2()
        {
            TestHighlightRunner helper = new TestHighlightRunner((instance) =>
            {
                String s = "Hi-Speed10 foo";

                Query query;
                Highlighter highlighter;
                String result;

                query = new TermQuery(new Term("text", "foo"));
                highlighter = instance.GetHighlighter(query, "text", this);
                result = highlighter.GetBestFragments(getTS2(), s, 3, "...");
                assertEquals("Hi-Speed10 <B>foo</B>", result);

                query = new TermQuery(new Term("text", "10"));
                highlighter = instance.GetHighlighter(query, "text", this);
                result = highlighter.GetBestFragments(getTS2(), s, 3, "...");
                assertEquals("Hi-Speed<B>10</B> foo", result);

                query = new TermQuery(new Term("text", "hi"));
                highlighter = instance.GetHighlighter(query, "text", this);
                result = highlighter.GetBestFragments(getTS2(), s, 3, "...");
                assertEquals("<B>Hi</B>-Speed10 foo", result);

                query = new TermQuery(new Term("text", "speed"));
                highlighter = instance.GetHighlighter(query, "text", this);
                result = highlighter.GetBestFragments(getTS2(), s, 3, "...");
                assertEquals("Hi-<B>Speed</B>10 foo", result);

                query = new TermQuery(new Term("text", "hispeed"));
                highlighter = instance.GetHighlighter(query, "text", this);
                result = highlighter.GetBestFragments(getTS2(), s, 3, "...");
                assertEquals("<B>Hi-Speed</B>10 foo", result);

                BooleanQuery booleanQuery = new BooleanQuery();
                booleanQuery.Add(new TermQuery(new Term("text", "hi")), Occur.SHOULD);
                booleanQuery.Add(new TermQuery(new Term("text", "speed")), Occur.SHOULD);

                query = booleanQuery;
                highlighter = instance.GetHighlighter(query, "text", this);
                result = highlighter.GetBestFragments(getTS2(), s, 3, "...");
                assertEquals("<B>Hi-Speed</B>10 foo", result);

                // ///////////////// same tests, just put the bigger overlapping token
                // first
                query = new TermQuery(new Term("text", "foo"));
                highlighter = instance.GetHighlighter(query, "text", this);
                result = highlighter.GetBestFragments(getTS2a(), s, 3, "...");
                assertEquals("Hi-Speed10 <B>foo</B>", result);

                query = new TermQuery(new Term("text", "10"));
                highlighter = instance.GetHighlighter(query, "text", this);
                result = highlighter.GetBestFragments(getTS2a(), s, 3, "...");
                assertEquals("Hi-Speed<B>10</B> foo", result);

                query = new TermQuery(new Term("text", "hi"));
                highlighter = instance.GetHighlighter(query, "text", this);
                result = highlighter.GetBestFragments(getTS2a(), s, 3, "...");
                assertEquals("<B>Hi</B>-Speed10 foo", result);

                query = new TermQuery(new Term("text", "speed"));
                highlighter = instance.GetHighlighter(query, "text", this);
                result = highlighter.GetBestFragments(getTS2a(), s, 3, "...");
                assertEquals("Hi-<B>Speed</B>10 foo", result);

                query = new TermQuery(new Term("text", "hispeed"));
                highlighter = instance.GetHighlighter(query, "text", this);
                result = highlighter.GetBestFragments(getTS2a(), s, 3, "...");
                assertEquals("<B>Hi-Speed</B>10 foo", result);

                query = booleanQuery;
                highlighter = instance.GetHighlighter(query, "text", this);
                result = highlighter.GetBestFragments(getTS2a(), s, 3, "...");
                assertEquals("<B>Hi-Speed</B>10 foo", result);
            });

            helper.Start();
        }

        private Store.Directory dir;
        private Analyzer a;

        [Test]
        public void TestWeightedTermsWithDeletes()
        {
            makeIndex();
            deleteDocument();
            searchIndex();
        }

        private Document doc(String f, String v)
        {
            Document doc = new Document();
            doc.Add(new TextField(f, v, Field.Store.YES));
            return doc;
        }

        private void makeIndex()
        {
            IndexWriter writer = new IndexWriter(dir, new IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random, MockTokenizer.WHITESPACE, false)));
            writer.AddDocument(doc("t_text1", "random words for highlighting tests del"));
            writer.AddDocument(doc("t_text1", "more random words for second field del"));
            writer.AddDocument(doc("t_text1", "random words for highlighting tests del"));
            writer.AddDocument(doc("t_text1", "more random words for second field"));
            writer.ForceMerge(1);
            writer.Dispose();
        }

        private void deleteDocument()
        {
            IndexWriter writer = new IndexWriter(dir, new IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random, MockTokenizer.WHITESPACE, false)).SetOpenMode(OpenMode.APPEND));
            writer.DeleteDocuments(new Term("t_text1", "del"));
            // To see negative idf, keep comment the following line
            //writer.forceMerge(1);
            writer.Dispose();
        }

        private void searchIndex()
        {
            Query query = new TermQuery(new Term("t_text1", "random"));
            IndexReader reader = DirectoryReader.Open(dir);
            IndexSearcher searcher = NewSearcher(reader);
            // This scorer can return negative idf -> null fragment
            IScorer scorer = new QueryTermScorer(query, searcher.IndexReader, "t_text1");
            // This scorer doesn't use idf (patch version)
            //Scorer scorer = new QueryTermScorer( query, "t_text1" );
            Highlighter h = new Highlighter(scorer);

            TopDocs hits = searcher.Search(query, null, 10);
            for (int i = 0; i < hits.TotalHits; i++)
            {
                Document doc = searcher.Doc(hits.ScoreDocs[i].Doc);
                String result = h.GetBestFragment(a, "t_text1", doc.Get("t_text1"));
                if (Verbose) Console.WriteLine("result:" + result);
                assertEquals("more <B>random</B> words for second field", result);
            }
            reader.Dispose();
        }

        /*
         * 
         * public void TestBigramAnalyzer() throws IOException, ParseException {
         * //test to ensure analyzers with none-consecutive start/end offsets //dont
         * double-highlight text //setup index 1 RAMDirectory ramDir = new
         * RAMDirectory(); Analyzer bigramAnalyzer=new CJKAnalyzer(); IndexWriter
         * writer = new IndexWriter(ramDir,bigramAnalyzer , true); Document d = new
         * Document(); Field f = new Field(FIELD_NAME, "java abc def", true, true,
         * true); d.Add(f); writer.addDocument(d); writer.close(); IndexReader reader =
         * DirectoryReader.open(ramDir);
         * 
         * IndexSearcher searcher=new IndexSearcher(reader); query =
         * QueryParser.parse("abc", FIELD_NAME, bigramAnalyzer);
         * Console.WriteLine("Searching for: " + query.toString(FIELD_NAME)); hits =
         * searcher.Search(query);
         * 
         * Highlighter highlighter = new Highlighter(this,new
         * QueryFragmentScorer(query));
         * 
         * for (int i = 0; i < hits.TotalHits; i++) { String text =
         * searcher.doc2(hits.ScoreDocs[i].doc).Get(FIELD_NAME); TokenStream
         * tokenStream=bigramAnalyzer.TokenStream(FIELD_NAME,text);
         * String highlightedText = highlighter.GetBestFragment(tokenStream,text);
         * Console.WriteLine(highlightedText); } }
         */


        public String HighlightTerm(String originalText, TokenGroup group)
        {
            if (group.TotalScore <= 0)
            {
                return originalText;
            }
            numHighlights++; // update stats used in assertions
            return "<B>" + originalText + "</B>";
        }

        public void doSearching(Query unReWrittenQuery)
        {
            searcher = NewSearcher(reader);
            // for any multi-term queries to work (prefix, wildcard, range,fuzzy etc)
            // you must use a rewritten query!
            query = unReWrittenQuery.Rewrite(reader);
            if (Verbose) Console.WriteLine("Searching for: " + query.ToString(FIELD_NAME));
            hits = searcher.Search(query, null, 1000);
        }

        public void assertExpectedHighlightCount(int maxNumFragmentsRequired,
             int expectedHighlights)
        {
            for (int i = 0; i < hits.TotalHits; i++)
            {
                String text = searcher.Doc(hits.ScoreDocs[i].Doc).Get(FIELD_NAME);
                TokenStream tokenStream = analyzer.GetTokenStream(FIELD_NAME, text);
                QueryScorer scorer = new QueryScorer(query, FIELD_NAME);
                Highlighter highlighter = new Highlighter(this, scorer);

                highlighter.TextFragmenter = (new SimpleFragmenter(40));

                String result = highlighter.GetBestFragments(tokenStream, text, maxNumFragmentsRequired,
                    "...");
                if (Verbose) Console.WriteLine("\t" + result);

                assertTrue("Failed to find correct number of highlights " + numHighlights + " found",
                    numHighlights == expectedHighlights);
            }
        }


        public override void SetUp()
        {
            base.SetUp();

            a = new MockAnalyzer(Random, MockTokenizer.WHITESPACE, false);
            analyzer = new MockAnalyzer(Random, MockTokenizer.SIMPLE, true, MockTokenFilter.ENGLISH_STOPSET);
            dir = NewDirectory();
            ramDir = NewDirectory();
            IndexWriter writer = new IndexWriter(ramDir, NewIndexWriterConfig(
                TEST_VERSION_CURRENT, new MockAnalyzer(Random, MockTokenizer.SIMPLE, true, MockTokenFilter.ENGLISH_STOPSET)));
            foreach (String text in texts)
            {
                addDoc(writer, text);
            }
            Document doc = new Document();
            doc.Add(new Int32Field(NUMERIC_FIELD_NAME, 1, Field.Store.NO));
            doc.Add(new StoredField(NUMERIC_FIELD_NAME, 1));
            writer.AddDocument(doc, analyzer);

            doc = new Document();
            doc.Add(new Int32Field(NUMERIC_FIELD_NAME, 3, Field.Store.NO));
            doc.Add(new StoredField(NUMERIC_FIELD_NAME, 3));
            writer.AddDocument(doc, analyzer);

            doc = new Document();
            doc.Add(new Int32Field(NUMERIC_FIELD_NAME, 5, Field.Store.NO));
            doc.Add(new StoredField(NUMERIC_FIELD_NAME, 5));
            writer.AddDocument(doc, analyzer);

            doc = new Document();
            doc.Add(new Int32Field(NUMERIC_FIELD_NAME, 7, Field.Store.NO));
            doc.Add(new StoredField(NUMERIC_FIELD_NAME, 7));
            writer.AddDocument(doc, analyzer);

            writer.ForceMerge(1);
            writer.Dispose();
            reader = DirectoryReader.Open(ramDir);
            numHighlights = 0;
        }


        public override void TearDown()
        {
            reader.Dispose();
            dir.Dispose();
            ramDir.Dispose();
            base.TearDown();
        }
        private void addDoc(IndexWriter writer, String text)
        {
            Document d = new Document();

            Field f = new TextField(FIELD_NAME, text, Field.Store.YES);
            d.Add(f);
            writer.AddDocument(d);

        }

        private static Token createToken(String term, int start, int offset)
        {
            return new Token(term, start, offset);
        }


        /// <summary>
        /// LUCENENET specific test to deterimine if HexToInt is correctly translated
        /// </summary>
        [Test, LuceneNetSpecific]
        public void TestGradientHighlighterHexToInt()
        {
            int result = GradientFormatter.HexToInt32("#CFFFFF".Substring(1, 3 - 1));

            assertEquals(207, result);
        }

    }

    // ===================================================================
    // ========== BEGIN TEST SUPPORTING CLASSES
    // ========== THESE LOOK LIKE, WITH SOME MORE EFFORT THESE COULD BE
    // ========== MADE MORE GENERALLY USEFUL.
    // TODO - make synonyms all interchangeable with each other and produce
    // a version that does hyponyms - the "is a specialised type of ...."
    // so that car = audi, bmw and volkswagen but bmw != audi so different
    // behaviour to synonyms
    // ===================================================================

    internal sealed class SynonymAnalyzer : Analyzer
    {
        private IDictionary<String, String> synonyms;

        public SynonymAnalyzer(IDictionary<String, String> synonyms)
        {
            this.synonyms = synonyms;
        }

        /*
         * (non-Javadoc)
         * 
         * @see org.apache.lucene.analysis.Analyzer#tokenStream(java.lang.String,
         *      java.io.Reader)
         */

        protected internal override TokenStreamComponents CreateComponents(String arg0, TextReader arg1)
        {
            Tokenizer stream = new MockTokenizer(arg1, MockTokenizer.SIMPLE, true);
            stream.AddAttribute<ICharTermAttribute>();
            stream.AddAttribute<IPositionIncrementAttribute>();
            stream.AddAttribute<IOffsetAttribute>();
            return new TokenStreamComponents(stream, new SynonymTokenizer(stream, synonyms));
        }
    }

    /**
     * Expands a token stream with synonyms (TODO - make the synonyms analyzed by choice of analyzer)
     *
     */
    internal sealed class SynonymTokenizer : TokenStream
    {
        private readonly TokenStream realStream;
        private Token currentRealToken = null;
        private readonly IDictionary<String, String> synonyms;
        private J2N.Text.StringTokenizer st = null;
        private readonly ICharTermAttribute realTermAtt;
        private readonly IPositionIncrementAttribute realPosIncrAtt;
        private readonly IOffsetAttribute realOffsetAtt;
        private readonly ICharTermAttribute termAtt;
        private readonly IPositionIncrementAttribute posIncrAtt;
        private readonly IOffsetAttribute offsetAtt;

        public SynonymTokenizer(TokenStream realStream, IDictionary<String, String> synonyms)
        {
            this.realStream = realStream;
            this.synonyms = synonyms;
            realTermAtt = realStream.AddAttribute<ICharTermAttribute>();
            realPosIncrAtt = realStream.AddAttribute<IPositionIncrementAttribute>();
            realOffsetAtt = realStream.AddAttribute<IOffsetAttribute>();

            termAtt = AddAttribute<ICharTermAttribute>();
            posIncrAtt = AddAttribute<IPositionIncrementAttribute>();
            offsetAtt = AddAttribute<IOffsetAttribute>();
        }


        public override bool IncrementToken()
        {

            if (currentRealToken is null)
            {
                bool next = realStream.IncrementToken();
                if (!next)
                {
                    return false;
                }
                //Token nextRealToken = new Token(, offsetAtt.StartOffset, offsetAtt.EndOffset);
                ClearAttributes();
                termAtt.CopyBuffer(realTermAtt.Buffer, 0, realTermAtt.Length);
                offsetAtt.SetOffset(realOffsetAtt.StartOffset, realOffsetAtt.EndOffset);
                posIncrAtt.PositionIncrement = (realPosIncrAtt.PositionIncrement);

                //String expansions = synonyms.Get(realTermAtt.toString());
                //if (expansions is null)
                if (!synonyms.TryGetValue(realTermAtt.ToString(), out string expansions) || expansions is null)
                {
                    return true;
                }
                st = new J2N.Text.StringTokenizer(expansions, ",");
                if (st.MoveNext())
                {
                    currentRealToken = new Token(realOffsetAtt.StartOffset, realOffsetAtt.EndOffset);
                    currentRealToken.CopyBuffer(realTermAtt.Buffer, 0, realTermAtt.Length);
                }

                return true;
            }
            else
            {
                st.MoveNext();
                String tok = st.Current;
                ClearAttributes();
                termAtt.SetEmpty().Append(tok);
                offsetAtt.SetOffset(currentRealToken.StartOffset, currentRealToken.EndOffset);
                posIncrAtt.PositionIncrement = (0);
                if (!st.MoveNext())
                {
                    currentRealToken = null;
                    st = null;
                }
                return true;
            }

        }


        public override void Reset()
        {
            base.Reset();
            this.realStream.Reset();
            this.currentRealToken = null;
            this.st = null;
        }


        public override void End()
        {
            base.End();
            this.realStream.End();
        }


        protected override void Dispose(bool disposing)
        {
            try
            {
                if (disposing)
                {
                    this.realStream.Dispose();
                    this.st?.Dispose();
                    this.st = null;
                }
            }
            finally
            {
                base.Dispose(disposing);
            }
        }
    }

    internal class TestHighlightRunner
    {
        internal static readonly int QUERY = 0;
        internal static readonly int QUERY_TERM = 1;

        internal int mode = QUERY;
        internal IFragmenter frag = new SimpleFragmenter(20);


        // LUCENENET specific - added action to simulate the anonymous class in Java
        private readonly Action<TestHighlightRunner> run;

        public TestHighlightRunner(Action<TestHighlightRunner> run)
        {
            this.run = run;
        }

        public Highlighter GetHighlighter(Query query, String fieldName, IFormatter formatter)
        {
            return GetHighlighter(query, fieldName, formatter, true);
        }

        public Highlighter GetHighlighter(Query query, String fieldName, IFormatter formatter, bool expanMultiTerm)
        {
            IScorer scorer;
            if (mode == QUERY)
            {
                scorer = new QueryScorer(query, fieldName);
                if (!expanMultiTerm)
                {
                    ((QueryScorer)scorer).ExpandMultiTermQuery = (false);
                }
            }
            else if (mode == QUERY_TERM)
            {
                scorer = new QueryTermScorer(query);
            }
            else
            {
                throw RuntimeException.Create("Unknown highlight mode");
            }

            return new Highlighter(formatter, scorer);
        }

        internal Highlighter GetHighlighter(WeightedTerm[] weightedTerms, IFormatter formatter)
        {
            if (mode == QUERY)
            {
                return new Highlighter(formatter, new QueryScorer((WeightedSpanTerm[])weightedTerms));
            }
            else if (mode == QUERY_TERM)
            {
                return new Highlighter(formatter, new QueryTermScorer(weightedTerms));

            }
            else
            {
                throw RuntimeException.Create("Unknown highlight mode");
            }
        }

        internal void DoStandardHighlights(Analyzer analyzer, IndexSearcher searcher, TopDocs hits, Query query, IFormatter formatter)
        {
            DoStandardHighlights(analyzer, searcher, hits, query, formatter, false);
        }

        internal void DoStandardHighlights(Analyzer analyzer, IndexSearcher searcher, TopDocs hits, Query query, IFormatter formatter, bool expandMT)
        {

            for (int i = 0; i < hits.TotalHits; i++)
            {
                String text = searcher.Doc(hits.ScoreDocs[i].Doc).Get(HighlighterTest.FIELD_NAME);
                int maxNumFragmentsRequired = 2;
                String fragmentSeparator = "...";
                IScorer scorer = null;
                TokenStream tokenStream = analyzer.GetTokenStream(HighlighterTest.FIELD_NAME, text);
                if (mode == QUERY)
                {
                    scorer = new QueryScorer(query);
                }
                else if (mode == QUERY_TERM)
                {
                    scorer = new QueryTermScorer(query);
                }
                Highlighter highlighter = new Highlighter(formatter, scorer);
                highlighter.TextFragmenter = (frag);

                String result = highlighter.GetBestFragments(tokenStream, text, maxNumFragmentsRequired,
                    fragmentSeparator);
                if (LuceneTestCase.Verbose) Console.WriteLine("\t" + result);
            }
        }

        //abstract void run();

        internal void Start()
        {
            if (LuceneTestCase.Verbose) Console.WriteLine("Run QueryScorer");
            run(this);
            if (LuceneTestCase.Verbose) Console.WriteLine("Run QueryTermScorer");
            mode = QUERY_TERM;
            run(this);
        }

        public void Run()
        {
            run(this);
        }
    }
}

