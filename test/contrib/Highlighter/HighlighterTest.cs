/*
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
 * (the "License"); you may not use this file except in compliance with
 * the License.  You may obtain a copy of the License at
 * 
 * http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using Contrib.Regex;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Analysis.Tokenattributes;
using Lucene.Net.Documents;
using Lucene.Net.QueryParsers;
using Lucene.Net.Search.Spans;
using Lucene.Net.Store;
using Lucene.Net.Support;
using NUnit.Framework;
using Version = Lucene.Net.Util.Version;
using Lucene.Net.Index;
using Lucene.Net.Test.Analysis;
using Lucene.Net.Util;
using Directory = Lucene.Net.Store.Directory;
using Token = Lucene.Net.Analysis.Token;

namespace Lucene.Net.Search.Highlight.Test
{
    /*
     * JUnit Test for Highlighter class.
     *
     */
    public class HighlighterTest : BaseTokenStreamTestCase, IFormatter
    {
        // TODO: change to CURRENT, does not work because posIncr:
        protected internal static readonly Version TEST_VERSION = Version.LUCENE_CURRENT;

        private IndexReader reader;
        protected internal static readonly String FIELD_NAME = "contents";
        private static readonly String NUMERIC_FIELD_NAME = "nfield";
        private Query query;
        private RAMDirectory ramDir;
        public IndexSearcher searcher = null;
        private int numHighlights = 0;
        private readonly Analyzer analyzer = new StandardAnalyzer(TEST_VERSION);
        private TopDocs hits;

        private String[] texts = {
                                     "Hello this is a piece of text that is very long and contains too much preamble and the meat is really here which says kennedy has been shot"
                                     ,
                                     "This piece of text refers to Kennedy at the beginning then has a longer piece of text that is very long in the middle and finally ends with another reference to Kennedy"
                                     ,
                                     "JFK has been shot", "John Kennedy has been shot",
                                     "This text has a typo in referring to Keneddy",
                                     "wordx wordy wordz wordx wordy wordx worda wordb wordy wordc", "y z x y z a b",
                                     "lets is a the lets is a the lets is a the lets"
                                 };

        public HighlighterTest()
        {
            
        }

        /*
         * Constructor for HighlightExtractorTest.
         * 
         * @param arg0
         */
        public HighlighterTest(String arg0)
            : base(arg0)
        {
        }

        [Test]
        public void TestQueryScorerHits()
        {
            Analyzer analyzer = new SimpleAnalyzer();
            QueryParser qp = new QueryParser(TEST_VERSION, FIELD_NAME, analyzer);
            query = qp.Parse("\"very long\"");
            searcher = new IndexSearcher(ramDir, true);
            TopDocs hits = searcher.Search(query, 10);

            QueryScorer scorer = new QueryScorer(query, FIELD_NAME);
            Highlighter highlighter = new Highlighter(scorer);


            for (int i = 0; i < hits.ScoreDocs.Length; i++)
            {
                Document doc = searcher.Doc(hits.ScoreDocs[i].Doc);
                String storedField = doc.Get(FIELD_NAME);

                TokenStream stream = TokenSources.GetAnyTokenStream(searcher.IndexReader, hits.ScoreDocs[i].Doc,
                                                                    FIELD_NAME, doc, analyzer);

                IFragmenter fragmenter = new SimpleSpanFragmenter(scorer);

                highlighter.TextFragmenter = fragmenter;

                String fragment = highlighter.GetBestFragment(stream, storedField);

                Console.WriteLine(fragment);
            }
        }

        [Test]
        public void TestHighlightingWithDefaultField()
        {

            String s1 = "I call our world Flatland, not because we call it so,";

            QueryParser parser = new QueryParser(TEST_VERSION, FIELD_NAME, new StandardAnalyzer(TEST_VERSION));

            // Verify that a query against the default field results in text being
            // highlighted
            // regardless of the field name.
            Query q = parser.Parse("\"world Flatland\"~3");
            String expected = "I call our <B>world</B> <B>Flatland</B>, not because we call it so,";
            String observed = HighlightField(q, "SOME_FIELD_NAME", s1);
            Console.WriteLine("Expected: \"" + expected + "\n" + "Observed: \"" + observed);
            Assert.AreEqual(expected, observed,
                            "Query in the default field results in text for *ANY* field being highlighted");

            // Verify that a query against a named field does not result in any
            // highlighting
            // when the query field name differs from the name of the field being
            // highlighted,
            // which in this example happens to be the default field name.
            q = parser.Parse("text:\"world Flatland\"~3");
            expected = s1;
            observed = HighlightField(q, FIELD_NAME, s1);
            Console.WriteLine("Expected: \"" + expected + "\n" + "Observed: \"" + observed);
            Assert.AreEqual(s1, HighlightField(q, FIELD_NAME, s1),
                            "Query in a named field does not result in highlighting when that field isn't in the query");
        }

        /*
         * This method intended for use with <tt>testHighlightingWithDefaultField()</tt>
       * @throws InvalidTokenOffsetsException 
         */

        private static String HighlightField(Query query, String fieldName, String text)
        {
            TokenStream tokenStream = new StandardAnalyzer(TEST_VERSION).TokenStream(fieldName, new StringReader(text));
            // Assuming "<B>", "</B>" used to highlight
            SimpleHTMLFormatter formatter = new SimpleHTMLFormatter();
            QueryScorer scorer = new QueryScorer(query, fieldName, FIELD_NAME);
            Highlighter highlighter = new Highlighter(formatter, scorer);
            highlighter.TextFragmenter = new SimpleFragmenter(int.MaxValue);

            String rv = highlighter.GetBestFragments(tokenStream, text, 1, "(FIELD TEXT TRUNCATED)");
            return rv.Length == 0 ? text : rv;
        }

        [Test]
        public void TestSimpleSpanHighlighter()
        {
            DoSearching("Kennedy");

            int maxNumFragmentsRequired = 2;

            QueryScorer scorer = new QueryScorer(query, FIELD_NAME);
            Highlighter highlighter = new Highlighter(scorer);

            for (int i = 0; i < hits.TotalHits; i++)
            {
                String text = searcher.Doc(hits.ScoreDocs[i].Doc).Get(FIELD_NAME);
                TokenStream tokenStream = analyzer.TokenStream(FIELD_NAME,
                                                               new StringReader(text));
                highlighter.TextFragmenter = new SimpleFragmenter(40);

                String result = highlighter.GetBestFragments(tokenStream, text, maxNumFragmentsRequired,
                                                             "...");
                Console.WriteLine("\t" + result);
            }

            // Not sure we can assert anything here - just running to check we dont
            // throw any exceptions
        }

        // LUCENE-1752
        [Test]
        public void TestRepeatingTermsInMultBooleans()
        {
            String content = "x y z a b c d e f g b c g";
            String ph1 = "\"a b c d\"";
            String ph2 = "\"b c g\"";
            String f1 = "f1";
            String f2 = "f2";
            String f1c = f1 + ":";
            String f2c = f2 + ":";
            String q = "(" + f1c + ph1 + " OR " + f2c + ph1 + ") AND (" + f1c + ph2
                       + " OR " + f2c + ph2 + ")";
            Analyzer analyzer = new WhitespaceAnalyzer();
            QueryParser qp = new QueryParser(TEST_VERSION, f1, analyzer);
            Query query = qp.Parse(q);

            QueryScorer scorer = new QueryScorer(query, f1);
            scorer.IsExpandMultiTermQuery = false;

            Highlighter h = new Highlighter(this, scorer);

            h.GetBestFragment(analyzer, f1, content);

            Assert.IsTrue(numHighlights == 7, "Failed to find correct number of highlights " + numHighlights + " found");
        }

        [Test]
        public void TestSimpleQueryScorerPhraseHighlighting()
        {
            DoSearching("\"very long and contains\"");

            int maxNumFragmentsRequired = 2;

            QueryScorer scorer = new QueryScorer(query, FIELD_NAME);
            Highlighter highlighter = new Highlighter(this, scorer);

            for (int i = 0; i < hits.TotalHits; i++)
            {
                String text = searcher.Doc(hits.ScoreDocs[i].Doc).Get(FIELD_NAME);
                TokenStream tokenStream = analyzer.TokenStream(FIELD_NAME, new StringReader(text));

                highlighter.TextFragmenter = new SimpleFragmenter(40);

                String result = highlighter.GetBestFragments(tokenStream, text, maxNumFragmentsRequired,
                                                             "...");
                Console.WriteLine("\t" + result);
            }

            Assert.IsTrue(numHighlights == 3, "Failed to find correct number of highlights " + numHighlights + " found");

            numHighlights = 0;
            DoSearching("\"This piece of text refers to Kennedy\"");

            maxNumFragmentsRequired = 2;

            scorer = new QueryScorer(query, FIELD_NAME);
            highlighter = new Highlighter(this, scorer);

            for (int i = 0; i < hits.TotalHits; i++)
            {
                String text = searcher.Doc(hits.ScoreDocs[i].Doc).Get(FIELD_NAME);
                TokenStream tokenStream = analyzer.TokenStream(FIELD_NAME, new StringReader(text));

                highlighter.TextFragmenter = new SimpleFragmenter(40);

                String result = highlighter.GetBestFragments(tokenStream, text, maxNumFragmentsRequired,
                                                             "...");
                Console.WriteLine("\t" + result);
            }

            Assert.IsTrue(numHighlights == 4, "Failed to find correct number of highlights " + numHighlights + " found");

            numHighlights = 0;
            DoSearching("\"lets is a the lets is a the lets is a the lets\"");

            maxNumFragmentsRequired = 2;

            scorer = new QueryScorer(query, FIELD_NAME);
            highlighter = new Highlighter(this, scorer);

            for (int i = 0; i < hits.TotalHits; i++)
            {
                String text = searcher.Doc(hits.ScoreDocs[i].Doc).Get(FIELD_NAME);
                TokenStream tokenStream = analyzer.TokenStream(FIELD_NAME, new StringReader(text));

                highlighter.TextFragmenter = new SimpleFragmenter(40);

                String result = highlighter.GetBestFragments(tokenStream, text, maxNumFragmentsRequired,
                                                             "...");
                Console.WriteLine("\t" + result);
            }

            Assert.IsTrue(numHighlights == 4, "Failed to find correct number of highlights " + numHighlights + " found");

        }

        [Test]
        public void TestSpanRegexQuery()
        {
            const int maxNumFragmentsRequired = 2;

            query = new SpanOrQuery(new SpanQuery[] {new SpanRegexQuery(new Term(FIELD_NAME, "ken.*"))});
            searcher = new IndexSearcher(ramDir, true);
            hits = searcher.Search(query, 100);

            var scorer = new QueryScorer(query, FIELD_NAME);
            var highlighter = new Highlighter(this, scorer);

            for (int i = 0; i < hits.TotalHits; i++)
            {
                String text = searcher.Doc(hits.ScoreDocs[i].Doc).Get(FIELD_NAME);
                TokenStream tokenStream = analyzer.TokenStream(FIELD_NAME, new StringReader(text));

                highlighter.TextFragmenter = new SimpleFragmenter(40);

                String result = highlighter.GetBestFragments(tokenStream, text, maxNumFragmentsRequired,
                                                             "...");
                Console.WriteLine("\t" + result);
            }

            Assert.IsTrue(numHighlights == 5, "Failed to find correct number of highlights " + numHighlights + " found");
        }

        [Test]
        public void TestRegexQuery()
        {
            const int maxNumFragmentsRequired = 2;

            query = new RegexQuery(new Term(FIELD_NAME, "ken.*"));
            searcher = new IndexSearcher(ramDir, true);
            hits = searcher.Search(query, 100);

            var scorer = new QueryScorer(query, FIELD_NAME);
            var highlighter = new Highlighter(this, scorer);

            for (int i = 0; i < hits.TotalHits; i++)
            {
                String text = searcher.Doc(hits.ScoreDocs[i].Doc).Get(FIELD_NAME);
                TokenStream tokenStream = analyzer.TokenStream(FIELD_NAME, new StringReader(text));

                highlighter.TextFragmenter = new SimpleFragmenter(40);

                String result = highlighter.GetBestFragments(tokenStream, text, maxNumFragmentsRequired,
                                                             "...");
                Console.WriteLine("\t" + result);
            }

            Assert.IsTrue(numHighlights == 5, "Failed to find correct number of highlights " + numHighlights + " found");
        }

        [Test]
        public void TestNumericRangeQuery()
        {
            // doesn't currently highlight, but make sure it doesn't cause exception either
            query = NumericRangeQuery.NewIntRange(NUMERIC_FIELD_NAME, 2, 6, true, true);
            searcher = new IndexSearcher(ramDir, true);
            hits = searcher.Search(query, 100);
            int maxNumFragmentsRequired = 2;

            QueryScorer scorer = new QueryScorer(query, FIELD_NAME);
            Highlighter highlighter = new Highlighter(this, scorer);

            for (int i = 0; i < hits.TotalHits; i++)
            {
                String text = searcher.Doc(hits.ScoreDocs[i].Doc).Get(NUMERIC_FIELD_NAME);
                TokenStream tokenStream = analyzer.TokenStream(FIELD_NAME, new StringReader(text));

                highlighter.TextFragmenter = new SimpleFragmenter(40);

                String result = highlighter.GetBestFragments(tokenStream, text, maxNumFragmentsRequired,
                                                             "...");
                //Console.WriteLine("\t" + result);
            }


        }

        [Test]
        public void TestSimpleQueryScorerPhraseHighlighting2()
        {
            DoSearching("\"text piece long\"~5");

            int maxNumFragmentsRequired = 2;

            var scorer = new QueryScorer(query, FIELD_NAME);
            var highlighter = new Highlighter(this, scorer);
            highlighter.TextFragmenter = new SimpleFragmenter(40);

            for (int i = 0; i < hits.TotalHits; i++)
            {
                var text = searcher.Doc(hits.ScoreDocs[i].Doc).Get(FIELD_NAME);
                var tokenStream = analyzer.TokenStream(FIELD_NAME, new StringReader(text));

                var result = highlighter.GetBestFragments(tokenStream, text, maxNumFragmentsRequired,
                                                             "...");
                Console.WriteLine("\t" + result);
            }

            Assert.IsTrue(numHighlights == 6, "Failed to find correct number of highlights " + numHighlights + " found");
        }

        [Test]
        public void TestSimpleQueryScorerPhraseHighlighting3()
        {
            DoSearching("\"x y z\"");

            int maxNumFragmentsRequired = 2;

            for (int i = 0; i < hits.TotalHits; i++)
            {
                var text = searcher.Doc(hits.ScoreDocs[i].Doc).Get(FIELD_NAME);
                var tokenStream = analyzer.TokenStream(FIELD_NAME, new StringReader(text));
                var scorer = new QueryScorer(query, FIELD_NAME);
                var highlighter = new Highlighter(this, scorer) {TextFragmenter = new SimpleFragmenter(40)};

                var result = highlighter.GetBestFragments(tokenStream, text, maxNumFragmentsRequired,
                                                             "...");
                Console.WriteLine("\t" + result);

                Assert.IsTrue(numHighlights == 3,
                              "Failed to find correct number of highlights " + numHighlights + " found");
            }
        }

        [Test]
        public void TestSimpleSpanFragmenter()
        {
            DoSearching("\"piece of text that is very long\"");

            int maxNumFragmentsRequired = 2;

            QueryScorer scorer = new QueryScorer(query, FIELD_NAME);
            Highlighter highlighter = new Highlighter(this, scorer);

            for (int i = 0; i < hits.TotalHits; i++)
            {
                String text = searcher.Doc(hits.ScoreDocs[i].Doc).Get(FIELD_NAME);
                TokenStream tokenStream = analyzer.TokenStream(FIELD_NAME, new StringReader(text));

                highlighter.TextFragmenter = new SimpleSpanFragmenter(scorer, 5);

                String result = highlighter.GetBestFragments(tokenStream, text,
                                                             maxNumFragmentsRequired, "...");
                Console.WriteLine("\t" + result);

            }

            DoSearching("\"been shot\"");

            maxNumFragmentsRequired = 2;

            scorer = new QueryScorer(query, FIELD_NAME);
            highlighter = new Highlighter(this, scorer);

            for (int i = 0; i < hits.TotalHits; i++)
            {
                String text = searcher.Doc(hits.ScoreDocs[i].Doc).Get(FIELD_NAME);
                TokenStream tokenStream = analyzer.TokenStream(FIELD_NAME, new StringReader(text));

                highlighter.TextFragmenter = new SimpleSpanFragmenter(scorer, 20);

                String result = highlighter.GetBestFragments(tokenStream, text,
                                                             maxNumFragmentsRequired, "...");
                Console.WriteLine("\t" + result);

            }
        }

        // position sensitive query added after position insensitive query
        [Test]
        public void TestPosTermStdTerm()
        {
            DoSearching("y \"x y z\"");

            int maxNumFragmentsRequired = 2;

            QueryScorer scorer = new QueryScorer(query, FIELD_NAME);
            Highlighter highlighter = new Highlighter(this, scorer);

            for (int i = 0; i < hits.TotalHits; i++)
            {
                String text = searcher.Doc(hits.ScoreDocs[i].Doc).Get(FIELD_NAME);
                TokenStream tokenStream = analyzer.TokenStream(FIELD_NAME, new StringReader(text));

                highlighter.TextFragmenter = new SimpleFragmenter(40);

                String result = highlighter.GetBestFragments(tokenStream, text, maxNumFragmentsRequired,
                                                             "...");
                Console.WriteLine("\t" + result);

                Assert.IsTrue(numHighlights == 4,
                              "Failed to find correct number of highlights " + numHighlights + " found");
            }
        }

        [Test]
        public void TestQueryScorerMultiPhraseQueryHighlighting()
        {
            MultiPhraseQuery mpq = new MultiPhraseQuery();

            mpq.Add(new Term[] {new Term(FIELD_NAME, "wordx"), new Term(FIELD_NAME, "wordb")});
            mpq.Add(new Term(FIELD_NAME, "wordy"));

            DoSearching(mpq);

            int maxNumFragmentsRequired = 2;
            AssertExpectedHighlightCount(maxNumFragmentsRequired, 6);
        }

        [Test]
        public void TestQueryScorerMultiPhraseQueryHighlightingWithGap()
        {
            MultiPhraseQuery mpq = new MultiPhraseQuery();

            /*
             * The toString of MultiPhraseQuery doesn't work so well with these
             * out-of-order additions, but the Query itself seems to match accurately.
             */

            mpq.Add(new Term[] {new Term(FIELD_NAME, "wordz")}, 2);
            mpq.Add(new Term[] {new Term(FIELD_NAME, "wordx")}, 0);

            DoSearching(mpq);

            int maxNumFragmentsRequired = 1;
            int expectedHighlights = 2;

            AssertExpectedHighlightCount(maxNumFragmentsRequired, expectedHighlights);
        }

        [Test]
        public void TestNearSpanSimpleQuery()
        {
            DoSearching(new SpanNearQuery(new SpanQuery[]
                                              {
                                                  new SpanTermQuery(new Term(FIELD_NAME, "beginning")),
                                                  new SpanTermQuery(new Term(FIELD_NAME, "kennedy"))
                                              }, 3, false));

            var helper = new TestHighlightRunner(TestHighlightRunner.QUERY);
            helper.TestAction = () => helper.DoStandardHighlights(analyzer, searcher, hits, query, this);
            helper.Run();

            Assert.IsTrue(numHighlights == 2, "Failed to find correct number of highlights " + numHighlights + " found");
        }

        [Test]
        public void TestSimpleQueryTermScorerHighlighter()
        {
            DoSearching("Kennedy");
            Highlighter highlighter = new Highlighter(new QueryTermScorer(query));
            highlighter.TextFragmenter = new SimpleFragmenter(40);
            int maxNumFragmentsRequired = 2;
            for (int i = 0; i < hits.TotalHits; i++)
            {
                String text = searcher.Doc(hits.ScoreDocs[i].Doc).Get(FIELD_NAME);
                TokenStream tokenStream = analyzer.TokenStream(FIELD_NAME, new StringReader(text));

                String result = highlighter.GetBestFragments(tokenStream, text, maxNumFragmentsRequired,
                                                             "...");
                Console.WriteLine("\t" + result);
            }
            // Not sure we can assert anything here - just running to check we dont
            // throw any exceptions
        }

        [Test]
        public void TestSpanHighlighting()
        {
            Query query1 = new SpanNearQuery(new SpanQuery[]
                                                 {
                                                     new SpanTermQuery(new Term(FIELD_NAME, "wordx")),
                                                     new SpanTermQuery(new Term(FIELD_NAME, "wordy"))
                                                 }, 1, false);
            Query query2 = new SpanNearQuery(new SpanQuery[]
                                                 {
                                                     new SpanTermQuery(new Term(FIELD_NAME, "wordy")),
                                                     new SpanTermQuery(new Term(FIELD_NAME, "wordc"))
                                                 }, 1, false);
            BooleanQuery bquery = new BooleanQuery();
            bquery.Add(query1, Occur.SHOULD);
            bquery.Add(query2, Occur.SHOULD);
            DoSearching(bquery);
            var helper = new TestHighlightRunner(TestHighlightRunner.QUERY);
            helper.TestAction = () => helper.DoStandardHighlights(analyzer, searcher, hits, query, this);

            helper.Run();
            Assert.IsTrue(numHighlights == 7, "Failed to find correct number of highlights " + numHighlights + " found");
        }

        [Test]
        public void TestNotSpanSimpleQuery()
        {
            DoSearching(new SpanNotQuery(new SpanNearQuery(new SpanQuery[]
                                                               {
                                                                   new SpanTermQuery(new Term(FIELD_NAME, "shot")),
                                                                   new SpanTermQuery(new Term(FIELD_NAME, "kennedy"))
                                                               }, 3, false), new SpanTermQuery(
                                                                                 new Term(FIELD_NAME, "john"))));
            var helper = new TestHighlightRunner(TestHighlightRunner.QUERY);
            helper.TestAction = () => helper.DoStandardHighlights(analyzer, searcher, hits, query, this);

            helper.Run();
            Assert.IsTrue(numHighlights == 4, "Failed to find correct number of highlights " + numHighlights + " found");
        }

        [Test]
        public void TestGetBestFragmentsSimpleQuery()
        {
            var helper = new TestHighlightRunner();
            helper.TestAction = () =>
                                    {
                                        numHighlights = 0;
                                        DoSearching("Kennedy");
                                        helper.DoStandardHighlights(analyzer, searcher, hits, query, this);
                                        Assert.IsTrue(numHighlights == 4,
                                                      "Failed to find correct number of highlights " + numHighlights +
                                                      " found");
                                    };

            helper.Start();
        }

        [Test]
        public void TestGetFuzzyFragments()
        {
            var helper = new TestHighlightRunner();
            helper.TestAction = () =>
                                    {
                                        numHighlights = 0;
                                        DoSearching("Kinnedy~");
                                        helper.DoStandardHighlights(analyzer, searcher, hits, query, this, true);
                                        Assert.IsTrue(numHighlights == 5,
                                                      "Failed to find correct number of highlights " + numHighlights +
                                                      " found");
                                    };

            helper.Start();
        }

        [Test]
        public void TestGetWildCardFragments()
        {
            var helper = new TestHighlightRunner();
            helper.TestAction = () =>
                                    {
                                        numHighlights = 0;
                                        DoSearching("K?nnedy");
                                        helper.DoStandardHighlights(analyzer, searcher, hits, query, this);
                                        Assert.IsTrue(numHighlights == 4,
                                                      "Failed to find correct number of highlights " + numHighlights +
                                                      " found");
                                    };

            helper.Start();
        }

        [Test]
        public void TestGetMidWildCardFragments()
        {
            var helper = new TestHighlightRunner();
            helper.TestAction = () =>
                                    {
                                        numHighlights = 0;
                                        DoSearching("K*dy");
                                        helper.DoStandardHighlights(analyzer, searcher, hits, query, this);
                                        Assert.IsTrue(numHighlights == 5,
                                                      "Failed to find correct number of highlights " + numHighlights +
                                                      " found");
                                    };

            helper.Start();
        }

        [Test]
        public void TestGetRangeFragments()
        {
            var helper = new TestHighlightRunner();

            helper.TestAction = () =>
                                    {
                                        numHighlights = 0;
                                        String queryString = FIELD_NAME + ":[kannedy TO kznnedy]";

                                        // Need to explicitly set the QueryParser property to use TermRangeQuery
                                        // rather
                                        // than RangeFilters
                                        QueryParser parser = new QueryParser(TEST_VERSION, FIELD_NAME, analyzer);
                                        parser.MultiTermRewriteMethod = MultiTermQuery.SCORING_BOOLEAN_QUERY_REWRITE;
                                        query = parser.Parse(queryString);
                                        DoSearching(query);

                                        helper.DoStandardHighlights(analyzer, searcher, hits, query, this);
                                        Assert.IsTrue(numHighlights == 5,
                                                      "Failed to find correct number of highlights " + numHighlights +
                                                      " found");
                                    };

            helper.Start();
        }

        [Test]
        public void TestConstantScoreMultiTermQuery()
        {

            numHighlights = 0;

            query = new WildcardQuery(new Term(FIELD_NAME, "ken*"));
            ((WildcardQuery) query).RewriteMethod = MultiTermQuery.CONSTANT_SCORE_FILTER_REWRITE;
            searcher = new IndexSearcher(ramDir, true);
            // can't rewrite ConstantScore if you want to highlight it -
            // it rewrites to ConstantScoreQuery which cannot be highlighted
            // query = unReWrittenQuery.Rewrite(reader);
            Console.WriteLine("Searching for: " + query.ToString(FIELD_NAME));
            hits = searcher.Search(query, null, 1000);

            for (int i = 0; i < hits.TotalHits; i++)
            {
                String text = searcher.Doc(hits.ScoreDocs[i].Doc).Get(HighlighterTest.FIELD_NAME);
                int maxNumFragmentsRequired = 2;
                String fragmentSeparator = "...";
                QueryScorer scorer = null;
                TokenStream tokenStream = null;

                tokenStream = analyzer.TokenStream(FIELD_NAME, new StringReader(text));

                scorer = new QueryScorer(query, FIELD_NAME);

                Highlighter highlighter = new Highlighter(this, scorer);

                highlighter.TextFragmenter = new SimpleFragmenter(20);

                String result = highlighter.GetBestFragments(tokenStream, text, maxNumFragmentsRequired,
                                                             fragmentSeparator);
                Console.WriteLine("\t" + result);
            }
            Assert.IsTrue(numHighlights == 5, "Failed to find correct number of highlights " + numHighlights + " found");

            // try null field

            hits = searcher.Search(query, null, 1000);

            numHighlights = 0;

            for (int i = 0; i < hits.TotalHits; i++)
            {
                String text = searcher.Doc(hits.ScoreDocs[i].Doc).Get(HighlighterTest.FIELD_NAME);
                int maxNumFragmentsRequired = 2;
                String fragmentSeparator = "...";
                QueryScorer scorer = null;
                TokenStream tokenStream = null;

                tokenStream = analyzer.TokenStream(HighlighterTest.FIELD_NAME, new StringReader(text));

                scorer = new QueryScorer(query, null);

                Highlighter highlighter = new Highlighter(this, scorer);

                highlighter.TextFragmenter = new SimpleFragmenter(20);

                String result = highlighter.GetBestFragments(tokenStream, text, maxNumFragmentsRequired,
                                                             fragmentSeparator);
                Console.WriteLine("\t" + result);
            }
            Assert.IsTrue(numHighlights == 5, "Failed to find correct number of highlights " + numHighlights + " found");

            // try default field

            hits = searcher.Search(query, null, 1000);

            numHighlights = 0;

            for (int i = 0; i < hits.TotalHits; i++)
            {
                String text = searcher.Doc(hits.ScoreDocs[i].Doc).Get(HighlighterTest.FIELD_NAME);
                int maxNumFragmentsRequired = 2;
                String fragmentSeparator = "...";
                QueryScorer scorer = null;
                TokenStream tokenStream = null;

                tokenStream = analyzer.TokenStream(HighlighterTest.FIELD_NAME, new StringReader(text));

                scorer = new QueryScorer(query, "random_field", HighlighterTest.FIELD_NAME);

                Highlighter highlighter = new Highlighter(this, scorer);

                highlighter.TextFragmenter = new SimpleFragmenter(20);

                String result = highlighter.GetBestFragments(tokenStream, text, maxNumFragmentsRequired,
                                                             fragmentSeparator);
                Console.WriteLine("\t" + result);
            }
            Assert.IsTrue(numHighlights == 5, "Failed to find correct number of highlights " + numHighlights + " found");
        }

        [Test]
        public void TestGetBestFragmentsPhrase()
        {
            var helper = new TestHighlightRunner();
            helper.TestAction = () =>
                                    {
                                        numHighlights = 0;
                                        DoSearching("\"John Kennedy\"");
                                        helper.DoStandardHighlights(analyzer, searcher, hits, query, this);
                                        // Currently highlights "John" and "Kennedy" separately
                                        Assert.IsTrue(numHighlights == 2,
                                                      "Failed to find correct number of highlights " + numHighlights +
                                                      " found");
                                    };

            helper.Start();
        }

        [Test]
        public void TestGetBestFragmentsQueryScorer()
        {
            var helper = new TestHighlightRunner();
            helper.TestAction = () =>
                                    {
                                        numHighlights = 0;
                                        SpanQuery[] clauses = new SpanQuery[]
                                                                  {
                                                                      new SpanTermQuery(new Term("contents", "john")),
                                                                      new SpanTermQuery(new Term("contents", "kennedy"))
                                                                      ,
                                                                  };

                                        SpanNearQuery snq = new SpanNearQuery(clauses, 1, true);
                                        DoSearching(snq);
                                        helper.DoStandardHighlights(analyzer, searcher, hits, query, this);
                                        // Currently highlights "John" and "Kennedy" separately
                                        Assert.IsTrue(numHighlights == 2,
                                                      "Failed to find correct number of highlights " + numHighlights +
                                                      " found");
                                    };

            helper.Start();
        }

        [Test]
        public void TestOffByOne()
        {
            var helper = new TestHighlightRunner();
            helper.TestAction = () =>
                                    {
                                        TermQuery query = new TermQuery(new Term("data", "help"));
                                        Highlighter hg = new Highlighter(new SimpleHTMLFormatter(),
                                                                         new QueryTermScorer(query));
                                        hg.TextFragmenter = new NullFragmenter();

                                        String match = null;
                                        match = hg.GetBestFragment(analyzer, "data", "help me [54-65]");
                                        Assert.AreEqual(match, "<B>help</B> me [54-65]");
                                    };

            helper.Start();
        }

        [Test]
        public void TestGetBestFragmentsFilteredQuery()
        {
            var helper = new TestHighlightRunner();
            helper.TestAction = () =>
                                    {
                                        numHighlights = 0;
                                        TermRangeFilter rf = new TermRangeFilter("contents", "john", "john", true, true);
                                        SpanQuery[] clauses = {
                                                                  new SpanTermQuery(new Term("contents", "john")),
                                                                  new SpanTermQuery(new Term("contents", "kennedy"))
                                                              };
                                        SpanNearQuery snq = new SpanNearQuery(clauses, 1, true);
                                        FilteredQuery fq = new FilteredQuery(snq, rf);

                                        DoSearching(fq);
                                        helper.DoStandardHighlights(analyzer, searcher, hits, query, this);
                                        // Currently highlights "John" and "Kennedy" separately
                                        Assert.IsTrue(numHighlights == 2,
                                                      "Failed to find correct number of highlights " + numHighlights +
                                                      " found");
                                    };

            helper.Start();
        }

        [Test]
        public void TestGetBestFragmentsFilteredPhraseQuery()
        {
            var helper = new TestHighlightRunner();
            helper.TestAction = () =>
                                    {
                                        numHighlights = 0;
                                        var rf = new TermRangeFilter("contents", "john", "john", true, true);
                                        var pq = new PhraseQuery();
                                        pq.Add(new Term("contents", "john"));
                                        pq.Add(new Term("contents", "kennedy"));
                                        var fq = new FilteredQuery(pq, rf);

                                        DoSearching(fq);
                                        helper.DoStandardHighlights(analyzer, searcher, hits, query, this);
                                        // Currently highlights "John" and "Kennedy" separately
                                        Assert.IsTrue(numHighlights == 2,
                                                      "Failed to find correct number of highlights " + numHighlights +
                                                      " found");
                                    };

            helper.Start();
        }

        [Test]
        public void TestGetBestFragmentsMultiTerm()
        {
            var helper = new TestHighlightRunner();
            helper.TestAction = () =>
                                    {
                                        numHighlights = 0;
                                        DoSearching("John Kenn*");
                                        helper.DoStandardHighlights(analyzer, searcher, hits, query, this);
                                        Assert.IsTrue(numHighlights == 5,
                                                      "Failed to find correct number of highlights " + numHighlights +
                                                      " found");
                                    };

            helper.Start();
        }

        [Test]
        public void TestGetBestFragmentsWithOr()
        {
            var helper = new TestHighlightRunner();
            helper.TestAction = () =>
                                    {
                                        numHighlights = 0;
                                        DoSearching("JFK OR Kennedy");
                                        helper.DoStandardHighlights(analyzer, searcher, hits, query, this);
                                        Assert.IsTrue(numHighlights == 5,
                                                      "Failed to find correct number of highlights " + numHighlights +
                                                      " found");
                                    };
            helper.Start();
        }

        [Test]
        public void TestGetBestSingleFragment()
        {

            var helper = new TestHighlightRunner();
            helper.TestAction = () =>
                                    {
                                        DoSearching("Kennedy");
                                        numHighlights = 0;
                                        for (int i = 0; i < hits.TotalHits; i++)
                                        {
                                            String text = searcher.Doc(hits.ScoreDocs[i].Doc).Get(FIELD_NAME);
                                            TokenStream tokenStream = analyzer.TokenStream(FIELD_NAME,
                                                                                           new StringReader(text));

                                            Highlighter highlighter = helper.GetHighlighter(query, FIELD_NAME,
                                                                                            tokenStream,
                                                                                            this);
                                            highlighter.TextFragmenter = new SimpleFragmenter(40);
                                            String result = highlighter.GetBestFragment(tokenStream, text);
                                            Console.WriteLine("\t" + result);
                                        }
                                        Assert.IsTrue(numHighlights == 4,
                                                      "Failed to find correct number of highlights " + numHighlights +
                                                      " found");

                                        numHighlights = 0;
                                        for (int i = 0; i < hits.TotalHits; i++)
                                        {
                                            String text = searcher.Doc(hits.ScoreDocs[i].Doc).Get(FIELD_NAME);
                                            TokenStream tokenStream = analyzer.TokenStream(FIELD_NAME,
                                                                                           new StringReader(text));
                                            Highlighter highlighter = helper.GetHighlighter(query, FIELD_NAME,
                                                                                            tokenStream,
                                                                                            this);
                                            highlighter.GetBestFragment(analyzer, FIELD_NAME, text);
                                        }
                                        Assert.IsTrue(numHighlights == 4,
                                                      "Failed to find correct number of highlights " + numHighlights +
                                                      " found");

                                        numHighlights = 0;
                                        for (int i = 0; i < hits.TotalHits; i++)
                                        {
                                            String text = searcher.Doc(hits.ScoreDocs[i].Doc).Get(FIELD_NAME);

                                            TokenStream tokenStream = analyzer.TokenStream(FIELD_NAME,
                                                                                           new StringReader(text));
                                            Highlighter highlighter = helper.GetHighlighter(query, FIELD_NAME,
                                                                                            tokenStream,
                                                                                            this);
                                            highlighter.GetBestFragments(analyzer, FIELD_NAME, text, 10);
                                        }
                                        Assert.IsTrue(numHighlights == 4,
                                                      "Failed to find correct number of highlights " + numHighlights +
                                                      " found");

                                    };

            helper.Start();

        }

        [Test]
        public void TestGetBestSingleFragmentWithWeights()
        {

            var helper = new TestHighlightRunner();
            helper.TestAction = () =>
                                    {
                                        WeightedSpanTerm[] wTerms = new WeightedSpanTerm[2];
                                        wTerms[0] = new WeightedSpanTerm(10f, "hello");

                                        var positionSpans = new List<PositionSpan> {new PositionSpan(0, 0)};
                                        wTerms[0].AddPositionSpans(positionSpans);

                                        wTerms[1] = new WeightedSpanTerm(1f, "kennedy");
                                        positionSpans = new List<PositionSpan> {new PositionSpan(14, 14)};
                                        wTerms[1].AddPositionSpans(positionSpans);

                                        Highlighter highlighter = helper.GetHighlighter(wTerms, this); // new
                                        // Highlighter(new
                                        // QueryTermScorer(wTerms));
                                        TokenStream tokenStream = analyzer.TokenStream(FIELD_NAME,
                                                                                       new StringReader(texts[0]));
                                        highlighter.TextFragmenter = new SimpleFragmenter(2);

                                        String result = highlighter.GetBestFragment(tokenStream, texts[0]).Trim();
                                        Assert.IsTrue("<B>Hello</B>".Equals(result),
                                                      "Failed to find best section using weighted terms. Found: [" +
                                                      result + "]");

                                        // readjust weights
                                        wTerms[1].Weight = 50f;
                                        tokenStream = analyzer.TokenStream(FIELD_NAME, new StringReader(texts[0]));
                                        highlighter = helper.GetHighlighter(wTerms, this);
                                        highlighter.TextFragmenter = new SimpleFragmenter(2);

                                        result = highlighter.GetBestFragment(tokenStream, texts[0]).Trim();
                                        Assert.IsTrue("<B>kennedy</B>".Equals(result),
                                                      "Failed to find best section using weighted terms. Found: " +
                                                      result);
                                    };

            helper.Start();

        }

        // tests a "complex" analyzer that produces multiple
        // overlapping tokens
        [Test]
        public void TestOverlapAnalyzer()
        {
            var helper = new TestHighlightRunner();
            helper.TestAction = () =>
                                    {
                                        var synonyms = new HashMap<string, string>();
                                        synonyms["football"] = "soccer,footie";
                                        var analyzer = new SynonymAnalyzer(synonyms);
                                        var srchkey = "football";

                                        var s = "football-soccer in the euro 2004 footie competition";
                                        var parser = new QueryParser(TEST_VERSION, "bookid", analyzer);
                                        var query = parser.Parse(srchkey);

                                        var tokenStream = analyzer.TokenStream(null, new StringReader(s));

                                        var highlighter = helper.GetHighlighter(query, null, tokenStream, this);

                                        // Get 3 best fragments and seperate with a "..."
                                        tokenStream = analyzer.TokenStream(null, new StringReader(s));

                                        var result = highlighter.GetBestFragments(tokenStream, s, 3, "...");
                                        var expectedResult =
                                            "<B>football</B>-<B>soccer</B> in the euro 2004 <B>footie</B> competition";
                                        Assert.IsTrue(expectedResult.Equals(result),
                                                      "overlapping analyzer should handle highlights OK, expected:" +
                                                      expectedResult + " actual:" + result);
                                    };

            helper.Start();

        }

        [Test]
        public void TestGetSimpleHighlight()
        {
            var helper = new TestHighlightRunner();
            helper.TestAction = () =>
                                    {
                                        numHighlights = 0;
                                        DoSearching("Kennedy");
                                        // new Highlighter(this, new QueryTermScorer(query));

                                        for (int i = 0; i < hits.TotalHits; i++)
                                        {
                                            String text = searcher.Doc(hits.ScoreDocs[i].Doc).Get(FIELD_NAME);
                                            TokenStream tokenStream = analyzer.TokenStream(FIELD_NAME,
                                                                                           new StringReader(text));
                                            Highlighter highlighter = helper.GetHighlighter(query, FIELD_NAME,
                                                                                            tokenStream,
                                                                                            this);
                                            String result = highlighter.GetBestFragment(tokenStream, text);
                                            Console.WriteLine("\t" + result);
                                        }
                                        Assert.IsTrue(numHighlights == 4,
                                                      "Failed to find correct number of highlights " + numHighlights +
                                                      "found");
                                    };
            helper.Start();
        }

        [Test]
        public void TestGetTextFragments()
        {
            var helper = new TestHighlightRunner();
            helper.TestAction = () =>
                                    {
                                        DoSearching("Kennedy");

                                        for (int i = 0; i < hits.TotalHits; i++)
                                        {
                                            var text = searcher.Doc(hits.ScoreDocs[i].Doc).Get(FIELD_NAME);
                                            var tokenStream = analyzer.TokenStream(FIELD_NAME, new StringReader(text));

                                            var highlighter = helper.GetHighlighter(query, FIELD_NAME, tokenStream,
                                                                                    this); // new Highlighter(this, new
                                            // QueryTermScorer(query));
                                            highlighter.TextFragmenter = new SimpleFragmenter(20);
                                            var stringResults = highlighter.GetBestFragments(tokenStream, text, 10);

                                            tokenStream = analyzer.TokenStream(FIELD_NAME, new StringReader(text));
                                            var fragmentResults = highlighter.GetBestTextFragments(tokenStream, text,
                                                                                                   true, 10);

                                            Assert.IsTrue(fragmentResults.Length == stringResults.Length,
                                                          "Failed to find correct number of text Fragments: " +
                                                          fragmentResults.Length + " vs " + stringResults.Length);
                                            for (int j = 0; j < stringResults.Length; j++)
                                            {
                                                Console.WriteLine(fragmentResults[j]);
                                                Assert.IsTrue(fragmentResults[j].ToString().Equals(stringResults[j]),
                                                              "Failed to find same text Fragments: " +
                                                              fragmentResults[j] + " found");

                                            }

                                        }
                                    };
            helper.Start();
        }

        [Test]
        public void TestMaxSizeHighlight()
        {
            var helper = new TestHighlightRunner();
            helper.TestAction = () =>
                                    {
                                        numHighlights = 0;
                                        DoSearching("meat");
                                        TokenStream tokenStream = analyzer.TokenStream(FIELD_NAME,
                                                                                       new StringReader(texts[0]));
                                        Highlighter highlighter = helper.GetHighlighter(query, FIELD_NAME, tokenStream,
                                                                                        this);
                                        // new Highlighter(this, new
                                        // QueryTermScorer(query));
                                        highlighter.MaxDocCharsToAnalyze = 30;

                                        highlighter.GetBestFragment(tokenStream, texts[0]);
                                        Assert.IsTrue(numHighlights == 0,
                                                      "Setting MaxDocBytesToAnalyze should have prevented us from finding matches for this record: "
                                                      + numHighlights + " found");
                                    };

            helper.Start();
        }

        [Test]
        public void TestMaxSizeHighlightTruncates()
        {
            var helper = new TestHighlightRunner();
            helper.TestAction = () =>
                                    {
                                        var goodWord = "goodtoken";
                                        var stopWords = Support.Compatibility.SetFactory.CreateHashSet(new[] { "stoppedtoken" });

                                        var query = new TermQuery(new Term("data", goodWord));

                                        string match;
                                        StringBuilder sb = new StringBuilder();
                                        sb.Append(goodWord);
                                        for (int i = 0; i < 10000; i++)
                                        {
                                            sb.Append(" ");
                                            // only one stopword
                                            sb.Append(stopWords.First());
                                        }
                                        SimpleHTMLFormatter fm = new SimpleHTMLFormatter();
                                        Highlighter hg = helper.GetHighlighter(query, "data",
                                                                               new StandardAnalyzer(TEST_VERSION,
                                                                                                    stopWords).
                                                                                   TokenStream(
                                                                                       "data",
                                                                                       new StringReader(sb.ToString())),
                                                                               fm); // new Highlighter(fm,
                                        // new
                                        // QueryTermScorer(query));
                                        hg.TextFragmenter = new NullFragmenter();
                                        hg.MaxDocCharsToAnalyze = 100;
                                        match = hg.GetBestFragment(new StandardAnalyzer(TEST_VERSION, stopWords), "data",
                                                                   sb.ToString());
                                        Assert.IsTrue(match.Length < hg.MaxDocCharsToAnalyze,
                                                      "Matched text should be no more than 100 chars in length ");

                                        // add another tokenized word to the overrall length - but set way
                                        // beyond
                                        // the length of text under consideration (after a large slug of stop
                                        // words
                                        // + whitespace)
                                        sb.Append(" ");
                                        sb.Append(goodWord);
                                        match = hg.GetBestFragment(new StandardAnalyzer(TEST_VERSION, stopWords), "data",
                                                                   sb.ToString());
                                        Assert.IsTrue(match.Length < hg.MaxDocCharsToAnalyze,
                                                      "Matched text should be no more than 100 chars in length ");
                                    };

            helper.Start();

        }

        [Test]
        public void TestMaxSizeEndHighlight()
        {
            var helper = new TestHighlightRunner();
            helper.TestAction = () =>
                {
                    var stopWords = Support.Compatibility.SetFactory.CreateHashSet(new[] {"in", "it"});
                    TermQuery query = new TermQuery(new Term("text", "searchterm"));

                    String text = "this is a text with searchterm in it";
                    SimpleHTMLFormatter fm = new SimpleHTMLFormatter();
                    Highlighter hg = helper.GetHighlighter(query, "text",
                                                           new StandardAnalyzer(TEST_VERSION,
                                                                                stopWords).
                                                               TokenStream("text",
                                                                           new StringReader(text)),
                                                           fm);
                    hg.TextFragmenter = new NullFragmenter();
                    hg.MaxDocCharsToAnalyze = 36;
                    String match = hg.GetBestFragment(new StandardAnalyzer(TEST_VERSION, stopWords),
                                                      "text", text);
                    Assert.IsTrue(match.EndsWith("in it"),
                                  "Matched text should contain remainder of text after highlighted query ");
                };
            helper.Start();
        }

        [Test]
        public void TestUnRewrittenQuery()
        {
            var helper = new TestHighlightRunner();
            helper.TestAction = () =>
                                    {
                                        numHighlights = 0;
                                        // test to show how rewritten query can still be used
                                        searcher = new IndexSearcher(ramDir, true);
                                        Analyzer analyzer = new StandardAnalyzer(TEST_VERSION);

                                        QueryParser parser = new QueryParser(TEST_VERSION, FIELD_NAME, analyzer);
                                        Query query = parser.Parse("JF? or Kenned*");
                                        Console.WriteLine("Searching with primitive query");
                                        // forget to set this and...
                                        // query=query.Rewrite(reader);
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
                                            TokenStream tokenStream = analyzer.TokenStream(FIELD_NAME,
                                                                                           new StringReader(text));
                                            Highlighter highlighter = helper.GetHighlighter(query, FIELD_NAME,
                                                                                            tokenStream,
                                                                                            this, false);

                                            highlighter.TextFragmenter = new SimpleFragmenter(40);

                                            String highlightedText = highlighter.GetBestFragments(tokenStream, text,
                                                                                                  maxNumFragmentsRequired,
                                                                                                  "...");

                                            Console.WriteLine(highlightedText);
                                        }
                                        // We expect to have zero highlights if the query is multi-terms and is
                                        // not
                                        // rewritten!
                                        Assert.IsTrue(numHighlights == 0,
                                                      "Failed to find correct number of highlights " + numHighlights +
                                                      " found");
                                    };

            helper.Start();
        }

        [Test]
        public void TestNoFragments()
        {
            var helper = new TestHighlightRunner();
            helper.TestAction = () =>
                                    {
                                        DoSearching("AnInvalidQueryWhichShouldYieldNoResults");

                                        foreach (string text in texts)
                                        {
                                            TokenStream tokenStream = analyzer.TokenStream(FIELD_NAME,
                                                                                           new StringReader(text));
                                            Highlighter highlighter = helper.GetHighlighter(query, FIELD_NAME,
                                                                                            tokenStream,
                                                                                            this);
                                            String result = highlighter.GetBestFragment(tokenStream, text);
                                            Assert.IsNull(result,
                                                          "The highlight result should be null for text with no query terms");
                                        }
                                    };

            helper.Start();
        }

        public class MockScorer : IScorer
        {
            public TokenStream Init(TokenStream tokenStream)
            {
                return null;
            }

            public void StartFragment(TextFragment newFragment)
            {
            }

            public float GetTokenScore()
            {
                return 0;
            }

            public float FragmentScore
            {
                get { return 1; }
            }
        }

        /*
         * Demonstrates creation of an XHTML compliant doc using new encoding facilities.
         * 
         * @throws Exception
         */

        [Test]
        public void TestEncoding()
        {

            String rawDocContent = "\"Smith & sons' prices < 3 and >4\" claims article";
            // run the highlighter on the raw content (scorer does not score any tokens
            // for
            // highlighting but scores a single fragment for selection
            Highlighter highlighter = new Highlighter(this, new SimpleHTMLEncoder(), new MockScorer());
            highlighter.TextFragmenter = new SimpleFragmenter(2000);
            TokenStream tokenStream = analyzer.TokenStream(FIELD_NAME, new StringReader(rawDocContent));

            String encodedSnippet = highlighter.GetBestFragments(tokenStream, rawDocContent, 1, "");
            // An ugly bit of XML creation:
            String xhtml = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n"
                           + "<html xmlns=\"http://www.w3.org/1999/xhtml\" xml:lang=\"en\" lang=\"en\">\n"
                           + "<head>\n" + "<title>My Test HTML Document</title>\n" + "</head>\n" + "<body>\n" + "<h2>"
                           + encodedSnippet + "</h2>\n" + "</body>\n" + "</html>";

            // now an ugly built of XML parsing to test the snippet is encoded OK
            var doc = new XmlDocument();
            doc.LoadXml(xhtml);
            var root = doc.DocumentElement;
            var nodes = root.GetElementsByTagName("body");
            var body = (XmlElement) nodes[0];
            nodes = body.GetElementsByTagName("h2");
            var h2 = (XmlElement) nodes[0];

            string decodedSnippet = h2.FirstChild.Value;
            Assert.AreEqual(rawDocContent, decodedSnippet, "XHTML Encoding should have worked:");
        }

        [Test]
        public void TestMultiSearcher()
        {
            // setup index 1
            RAMDirectory ramDir1 = new RAMDirectory();
            IndexWriter writer1 = new IndexWriter(ramDir1, new StandardAnalyzer(TEST_VERSION), true,
                                                  IndexWriter.MaxFieldLength.UNLIMITED);
            Document d = new Document();
            Field f = new Field(FIELD_NAME, "multiOne", Field.Store.YES, Field.Index.ANALYZED);
            d.Add(f);
            writer1.AddDocument(d);
            writer1.Optimize();
            writer1.Close();
            IndexReader reader1 = IndexReader.Open(ramDir1, true);

            // setup index 2
            RAMDirectory ramDir2 = new RAMDirectory();
            IndexWriter writer2 = new IndexWriter(ramDir2, new StandardAnalyzer(TEST_VERSION), true,
                                                  IndexWriter.MaxFieldLength.UNLIMITED);
            d = new Document();
            f = new Field(FIELD_NAME, "multiTwo", Field.Store.YES, Field.Index.ANALYZED);
            d.Add(f);
            writer2.AddDocument(d);
            writer2.Optimize();
            writer2.Close();
            IndexReader reader2 = IndexReader.Open(ramDir2, true);

            var searchers = new IndexSearcher[2];
            searchers[0] = new IndexSearcher(ramDir1, true);
            searchers[1] = new IndexSearcher(ramDir2, true);
            MultiSearcher multiSearcher = new MultiSearcher(searchers);
            QueryParser parser = new QueryParser(TEST_VERSION, FIELD_NAME, new StandardAnalyzer(TEST_VERSION));
            parser.MultiTermRewriteMethod = MultiTermQuery.SCORING_BOOLEAN_QUERY_REWRITE;
            query = parser.Parse("multi*");
            Console.WriteLine("Searching for: " + query.ToString(FIELD_NAME));
            // at this point the multisearcher calls combine(query[])
            hits = multiSearcher.Search(query, null, 1000);

            // query = QueryParser.Parse("multi*", FIELD_NAME, new StandardAnalyzer(TEST_VERSION));
            Query[] expandedQueries = new Query[2];
            expandedQueries[0] = query.Rewrite(reader1);
            expandedQueries[1] = query.Rewrite(reader2);
            query = query.Combine(expandedQueries);

            // create an instance of the highlighter with the tags used to surround
            // highlighted text
            Highlighter highlighter = new Highlighter(this, new QueryTermScorer(query));

            for (int i = 0; i < hits.TotalHits; i++)
            {
                String text = multiSearcher.Doc(hits.ScoreDocs[i].Doc).Get(FIELD_NAME);
                TokenStream tokenStream = analyzer.TokenStream(FIELD_NAME, new StringReader(text));
                String highlightedText = highlighter.GetBestFragment(tokenStream, text);
                Console.WriteLine(highlightedText);
            }
            Assert.IsTrue(numHighlights == 2, "Failed to find correct number of highlights " + numHighlights + " found");

        }

        [Test]
        public void TestFieldSpecificHighlighting()
        {
            var helper = new TestHighlightRunner();
            helper.TestAction = () =>
                                    {
                                        var docMainText = "fred is one of the people";
                                        var parser = new QueryParser(TEST_VERSION, FIELD_NAME, analyzer);
                                        var query = parser.Parse("fred category:people");

                                        // highlighting respects fieldnames used in query

                                        IScorer fieldSpecificScorer = null;
                                        if (helper.Mode == TestHighlightRunner.QUERY)
                                        {
                                            fieldSpecificScorer = new QueryScorer(query, FIELD_NAME);
                                        }
                                        else if (helper.Mode == TestHighlightRunner.QUERY_TERM)
                                        {
                                            fieldSpecificScorer = new QueryTermScorer(query, "contents");
                                        }
                                        var fieldSpecificHighlighter = new Highlighter(new SimpleHTMLFormatter(),
                                                                                       fieldSpecificScorer)
                                                                           {TextFragmenter = new NullFragmenter()};
                                        String result = fieldSpecificHighlighter.GetBestFragment(analyzer, FIELD_NAME,
                                                                                                 docMainText);
                                        Assert.AreEqual(result, "<B>fred</B> is one of the people", "Should match");

                                        // highlighting does not respect fieldnames used in query
                                        IScorer fieldInSpecificScorer = null;
                                        if (helper.Mode == TestHighlightRunner.QUERY)
                                        {
                                            fieldInSpecificScorer = new QueryScorer(query, null);
                                        }
                                        else if (helper.Mode == TestHighlightRunner.QUERY_TERM)
                                        {
                                            fieldInSpecificScorer = new QueryTermScorer(query);
                                        }

                                        var fieldInSpecificHighlighter = new Highlighter(new SimpleHTMLFormatter(),
                                                                                         fieldInSpecificScorer)
                                                                             {TextFragmenter = new NullFragmenter()};
                                        result = fieldInSpecificHighlighter.GetBestFragment(analyzer, FIELD_NAME,
                                                                                            docMainText);
                                        Assert.AreEqual(result, "<B>fred</B> is one of the <B>people</B>",
                                                        "Should match");

                                        reader.Close();
                                    };

            helper.Start();

        }

        private class MockTokenStream : TokenStream
        {
            public Action SetupAction { get; set; }
            public Func<bool> IncrementTokenAction { get; set; }

            public IEnumerator<Token> iter;
            public ITermAttribute termAtt;
            public IPositionIncrementAttribute posIncrAtt;
            public IOffsetAttribute offsetAtt;


            public void RunSetup()
            {
                SetupAction();
            }

            public override bool IncrementToken()
            {
                return IncrementTokenAction();
            }

            protected override void Dispose(bool disposing)
            {
                // do nothing
            }
        }

        protected TokenStream getTS2()
        {
            var ts = new MockTokenStream();

            ts.SetupAction = () =>
                                 {
                                     ts.termAtt = ts.AddAttribute<ITermAttribute>();
                                     ts.posIncrAtt = ts.AddAttribute<IPositionIncrementAttribute>();
                                     ts.offsetAtt = ts.AddAttribute<IOffsetAttribute>();
                                     var lst = new List<Token>();
                                     Token t = CreateToken("hi", 0, 2);
                                     t.PositionIncrement = 1;
                                     lst.Add(t);
                                     t = CreateToken("hispeed", 0, 8);
                                     t.PositionIncrement = 1;
                                     lst.Add(t);
                                     t = CreateToken("speed", 3, 8);
                                     t.PositionIncrement = 0;
                                     lst.Add(t);
                                     t = CreateToken("10", 8, 10);
                                     t.PositionIncrement = 1;
                                     lst.Add(t);
                                     t = CreateToken("foo", 11, 14);
                                     t.PositionIncrement = 1;
                                     lst.Add(t);
                                     ts.iter = lst.GetEnumerator();
                                 };
            ts.IncrementTokenAction = () =>
                                          {
                                              if (ts.iter.MoveNext())
                                              {
                                                  Token token = ts.iter.Current;
                                                  ts.ClearAttributes();
                                                  ts.termAtt.SetTermBuffer(token.Term);
                                                  ts.posIncrAtt.PositionIncrement = token.PositionIncrement;
                                                  ts.offsetAtt.SetOffset(token.StartOffset, token.EndOffset);
                                                  return true;
                                              }
                                              return false;
                                          };
            ts.RunSetup();
            return ts;
        }

        // same token-stream as above, but the bigger token comes first this time
        protected TokenStream GetTS2A()
        {
            var ts = new MockTokenStream();

            ts.SetupAction = () =>
                                 {
                                     ts.termAtt = ts.AddAttribute<ITermAttribute>();
                                     ts.posIncrAtt = ts.AddAttribute<IPositionIncrementAttribute>();
                                     ts.offsetAtt = ts.AddAttribute<IOffsetAttribute>();
                                     var lst = new List<Token>();
                                     Token t = CreateToken("hispeed", 0, 8);
                                     t.PositionIncrement = 1;
                                     lst.Add(t);
                                     t = CreateToken("hi", 0, 2);
                                     t.PositionIncrement = 0;
                                     lst.Add(t);
                                     t = CreateToken("speed", 3, 8);
                                     t.PositionIncrement = 1;
                                     lst.Add(t);
                                     t = CreateToken("10", 8, 10);
                                     t.PositionIncrement = 1;
                                     lst.Add(t);
                                     t = CreateToken("foo", 11, 14);
                                     t.PositionIncrement = 1;
                                     lst.Add(t);
                                     ts.iter = lst.GetEnumerator();

                                 };
            ts.IncrementTokenAction = () =>
                                          {
                                              if (ts.iter.MoveNext())
                                              {
                                                  Token token = ts.iter.Current;
                                                  ts.ClearAttributes();
                                                  ts.termAtt.SetTermBuffer(token.Term);
                                                  ts.posIncrAtt.PositionIncrement = (token.PositionIncrement);
                                                  ts.offsetAtt.SetOffset(token.StartOffset, token.EndOffset);
                                                  return true;
                                              }
                                              return false;
                                          };

            ts.RunSetup();
            return ts;
        }

        [Test]
        public void TestOverlapAnalyzer2()
        {
            var helper = new TestHighlightRunner();
            helper.TestAction = () =>
                                    {
                                        String s = "Hi-Speed10 foo";

                                        Query query;
                                        Highlighter highlighter;
                                        String result;

                                        query =
                                            new QueryParser(TEST_VERSION, "text", new WhitespaceAnalyzer()).Parse("foo");
                                        highlighter = helper.GetHighlighter(query, "text", getTS2(), this);
                                        result = highlighter.GetBestFragments(getTS2(), s, 3, "...");
                                        Assert.AreEqual(result, "Hi-Speed10 <B>foo</B>");

                                        query =
                                            new QueryParser(TEST_VERSION, "text", new WhitespaceAnalyzer()).Parse("10");
                                        highlighter = helper.GetHighlighter(query, "text", getTS2(), this);
                                        result = highlighter.GetBestFragments(getTS2(), s, 3, "...");
                                        Assert.AreEqual(result, "Hi-Speed<B>10</B> foo");

                                        query =
                                            new QueryParser(TEST_VERSION, "text", new WhitespaceAnalyzer()).Parse("hi");
                                        highlighter = helper.GetHighlighter(query, "text", getTS2(), this);
                                        result = highlighter.GetBestFragments(getTS2(), s, 3, "...");
                                        Assert.AreEqual(result, "<B>Hi</B>-Speed10 foo");

                                        query =
                                            new QueryParser(TEST_VERSION, "text", new WhitespaceAnalyzer()).Parse(
                                                "speed");
                                        highlighter = helper.GetHighlighter(query, "text", getTS2(), this);
                                        result = highlighter.GetBestFragments(getTS2(), s, 3, "...");
                                        Assert.AreEqual(result, "Hi-<B>Speed</B>10 foo");

                                        query =
                                            new QueryParser(TEST_VERSION, "text", new WhitespaceAnalyzer()).Parse(
                                                "hispeed");
                                        highlighter = helper.GetHighlighter(query, "text", getTS2(), this);
                                        result = highlighter.GetBestFragments(getTS2(), s, 3, "...");
                                        Assert.AreEqual(result, "<B>Hi-Speed</B>10 foo");

                                        query =
                                            new QueryParser(TEST_VERSION, "text", new WhitespaceAnalyzer()).Parse(
                                                "hi speed");
                                        highlighter = helper.GetHighlighter(query, "text", getTS2(), this);
                                        result = highlighter.GetBestFragments(getTS2(), s, 3, "...");
                                        Assert.AreEqual(result, "<B>Hi-Speed</B>10 foo");

                                        // ///////////////// same tests, just put the bigger overlapping token
                                        // first
                                        query =
                                            new QueryParser(TEST_VERSION, "text", new WhitespaceAnalyzer()).Parse("foo");
                                        highlighter = helper.GetHighlighter(query, "text", GetTS2A(), this);
                                        result = highlighter.GetBestFragments(GetTS2A(), s, 3, "...");
                                        Assert.AreEqual(result, "Hi-Speed10 <B>foo</B>");

                                        query =
                                            new QueryParser(TEST_VERSION, "text", new WhitespaceAnalyzer()).Parse("10");
                                        highlighter = helper.GetHighlighter(query, "text", GetTS2A(), this);
                                        result = highlighter.GetBestFragments(GetTS2A(), s, 3, "...");
                                        Assert.AreEqual(result, "Hi-Speed<B>10</B> foo");

                                        query =
                                            new QueryParser(TEST_VERSION, "text", new WhitespaceAnalyzer()).Parse("hi");
                                        highlighter = helper.GetHighlighter(query, "text", GetTS2A(), this);
                                        result = highlighter.GetBestFragments(GetTS2A(), s, 3, "...");
                                        Assert.AreEqual(result, "<B>Hi</B>-Speed10 foo");

                                        query =
                                            new QueryParser(TEST_VERSION, "text", new WhitespaceAnalyzer()).Parse(
                                                "speed");
                                        highlighter = helper.GetHighlighter(query, "text", GetTS2A(), this);
                                        result = highlighter.GetBestFragments(GetTS2A(), s, 3, "...");
                                        Assert.AreEqual(result, "Hi-<B>Speed</B>10 foo");

                                        query =
                                            new QueryParser(TEST_VERSION, "text", new WhitespaceAnalyzer()).Parse(
                                                "hispeed");
                                        highlighter = helper.GetHighlighter(query, "text", GetTS2A(), this);
                                        result = highlighter.GetBestFragments(GetTS2A(), s, 3, "...");
                                        Assert.AreEqual(result, "<B>Hi-Speed</B>10 foo");

                                        query =
                                            new QueryParser(TEST_VERSION, "text", new WhitespaceAnalyzer()).Parse(
                                                "hi speed");
                                        highlighter = helper.GetHighlighter(query, "text", GetTS2A(), this);
                                        result = highlighter.GetBestFragments(GetTS2A(), s, 3, "...");
                                        Assert.AreEqual(result, "<B>Hi-Speed</B>10 foo");
                                    };

            helper.Start();
        }

        private Directory dir = new RAMDirectory();
        private Analyzer a = new WhitespaceAnalyzer();

        [Test]
        public void TestWeightedTermsWithDeletes()
        {
            MakeIndex();
            DeleteDocument();
            SearchIndex();
        }

        private static Document Doc(String f, String v)
        {
            Document doc = new Document();
            doc.Add(new Field(f, v, Field.Store.YES, Field.Index.ANALYZED));
            return doc;
        }

        private void MakeIndex()
        {
            IndexWriter writer = new IndexWriter(dir, a, IndexWriter.MaxFieldLength.LIMITED);
            writer.AddDocument(Doc("t_text1", "random words for highlighting tests del"));
            writer.AddDocument(Doc("t_text1", "more random words for second field del"));
            writer.AddDocument(Doc("t_text1", "random words for highlighting tests del"));
            writer.AddDocument(Doc("t_text1", "more random words for second field"));
            writer.Optimize();
            writer.Close();
        }

        private void DeleteDocument()
        {
            IndexWriter writer = new IndexWriter(dir, a, false, IndexWriter.MaxFieldLength.LIMITED);
            writer.DeleteDocuments(new Term("t_text1", "del"));
            // To see negative idf, keep comment the following line
            //writer.Optimize();
            writer.Close();
        }

        private void SearchIndex()
        {
            String q = "t_text1:random";
            QueryParser parser = new QueryParser(TEST_VERSION, "t_text1", a);
            Query query = parser.Parse(q);
            IndexSearcher searcher = new IndexSearcher(dir, true);
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
                Console.WriteLine("result:" + result);
                Assert.AreEqual(result, "more <B>random</B> words for second field");
            }
            searcher.Close();
        }

        /*
         * 
         * [Test]
public void testBigramAnalyzer() {
         * //test to ensure analyzers with none-consecutive start/end offsets //dont
         * double-highlight text //setup index 1 RAMDirectory ramDir = new
         * RAMDirectory(); Analyzer bigramAnalyzer=new CJKAnalyzer(); IndexWriter
         * writer = new IndexWriter(ramDir,bigramAnalyzer , true); Document d = new
         * Document(); Field f = new Field(FIELD_NAME, "java abc def", true, true,
         * true); d.Add(f); writer.AddDocument(d); writer.Close(); IndexReader reader =
         * IndexReader.Open(ramDir, true);
         * 
         * IndexSearcher searcher=new IndexSearcher(reader); query =
         * QueryParser.Parse("abc", FIELD_NAME, bigramAnalyzer);
         * Console.WriteLine("Searching for: " + query.ToString(FIELD_NAME)); hits =
         * searcher.Search(query);
         * 
         * Highlighter highlighter = new Highlighter(this,new
         * QueryFragmentScorer(query));
         * 
         * for (int i = 0; i < hits.TotalHits; i++) { String text =
         * searcher.Doc(hits.ScoreDocs[i].Doc).Get(FIELD_NAME); TokenStream
         * tokenStream=bigramAnalyzer.TokenStream(FIELD_NAME,new StringReader(text));
         * String highlightedText = highlighter.GetBestFragment(tokenStream,text);
         * Console.WriteLine(highlightedText); } }
         */

        public String HighlightTerm(String originalText, TokenGroup group)
        {
            if (@group.TotalScore <= 0)
            {
                return originalText;
            }
            numHighlights++; // update stats used in assertions
            return "<B>" + originalText + "</B>";
        }

        public void DoSearching(String queryString)
        {
            QueryParser parser = new QueryParser(TEST_VERSION, FIELD_NAME, analyzer);
            parser.EnablePositionIncrements = true;
            parser.MultiTermRewriteMethod = MultiTermQuery.SCORING_BOOLEAN_QUERY_REWRITE;
            query = parser.Parse(queryString);
            DoSearching(query);
        }

        public void DoSearching(Query unReWrittenQuery)
        {
            searcher = new IndexSearcher(ramDir, true);
            // for any multi-term queries to work (prefix, wildcard, range,fuzzy etc)
            // you must use a rewritten query!
            query = unReWrittenQuery.Rewrite(reader);
            Console.WriteLine("Searching for: " + query.ToString(FIELD_NAME));
            hits = searcher.Search(query, null, 1000);
        }

        public void AssertExpectedHighlightCount(int maxNumFragmentsRequired, int expectedHighlights)
        {
            for (int i = 0; i < hits.TotalHits; i++)
            {
                String text = searcher.Doc(hits.ScoreDocs[i].Doc).Get(FIELD_NAME);
                TokenStream tokenStream = analyzer.TokenStream(FIELD_NAME, new StringReader(text));
                QueryScorer scorer = new QueryScorer(query, FIELD_NAME);
                Highlighter highlighter = new Highlighter(this, scorer);

                highlighter.TextFragmenter = new SimpleFragmenter(40);

                String result = highlighter.GetBestFragments(tokenStream, text, maxNumFragmentsRequired,
                                                             "...");
                Console.WriteLine("\t" + result);

                Assert.IsTrue(numHighlights == expectedHighlights,
                              "Failed to find correct number of highlights " + numHighlights + " found");
            }
        }

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            ramDir = new RAMDirectory();
            IndexWriter writer = new IndexWriter(ramDir, new StandardAnalyzer(TEST_VERSION), true,
                                                 IndexWriter.MaxFieldLength.UNLIMITED);
            for (int i = 0; i < texts.Length; i++)
            {
                AddDoc(writer, texts[i]);
            }
            Document doc = new Document();
            NumericField nfield = new NumericField(NUMERIC_FIELD_NAME, Field.Store.YES, true);
            nfield.SetIntValue(1);
            doc.Add(nfield);
            writer.AddDocument(doc, analyzer);
            nfield = new NumericField(NUMERIC_FIELD_NAME, Field.Store.YES, true);
            nfield.SetIntValue(3);
            doc = new Document();
            doc.Add(nfield);
            writer.AddDocument(doc, analyzer);
            nfield = new NumericField(NUMERIC_FIELD_NAME, Field.Store.YES, true);
            nfield.SetIntValue(5);
            doc = new Document();
            doc.Add(nfield);
            writer.AddDocument(doc, analyzer);
            nfield = new NumericField(NUMERIC_FIELD_NAME, Field.Store.YES, true);
            nfield.SetIntValue(7);
            doc = new Document();
            doc.Add(nfield);
            writer.AddDocument(doc, analyzer);
            writer.Optimize();
            writer.Close();
            reader = IndexReader.Open(ramDir, true);
            numHighlights = 0;
        }

        private void AddDoc(IndexWriter writer, String text)
        {
            Document d = new Document();
            Field f = new Field(FIELD_NAME, text, Field.Store.YES, Field.Index.ANALYZED);
            d.Add(f);
            writer.AddDocument(d);

        }

        [TearDown]
        public override void TearDown()
        {
            base.TearDown();
        }

        private static Token CreateToken(String term, int start, int offset)
        {
            Token token = new Token(start, offset);
            token.SetTermBuffer(term);
            return token;
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

    internal class SynonymAnalyzer : Analyzer
    {
        private IDictionary<string, string> synonyms;

        public SynonymAnalyzer(IDictionary<string, string> synonyms)
        {
            this.synonyms = synonyms;
        }

        /*
         * (non-Javadoc)
         * 
         * @see org.apache.lucene.analysis.Analyzer#tokenStream(java.lang.String,
         *      java.io.Reader)
         */

        public override TokenStream TokenStream(String arg0, System.IO.TextReader arg1)
        {
            LowerCaseTokenizer stream = new LowerCaseTokenizer(arg1);
            stream.AddAttribute<ITermAttribute>();
            stream.AddAttribute<IPositionIncrementAttribute>();
            stream.AddAttribute<IOffsetAttribute>();
            return new SynonymTokenizer(stream, synonyms);
        }
    }

    /*
     * Expands a token stream with synonyms (TODO - make the synonyms analyzed by choice of analyzer)
     *
     */

    internal class SynonymTokenizer : TokenStream
    {
        private TokenStream realStream;
        private Token currentRealToken = null;
        private Token cRealToken = null;
        private IDictionary<string, string> synonyms;
        private Tokenizer st = null;
        private ITermAttribute realTermAtt;
        private IPositionIncrementAttribute realPosIncrAtt;
        private IOffsetAttribute realOffsetAtt;
        private ITermAttribute termAtt;
        private IPositionIncrementAttribute posIncrAtt;
        private IOffsetAttribute offsetAtt;

        public SynonymTokenizer(TokenStream realStream, IDictionary<string, string> synonyms)
        {
            this.realStream = realStream;
            this.synonyms = synonyms;
            realTermAtt = realStream.AddAttribute<ITermAttribute>();
            realPosIncrAtt = realStream.AddAttribute<IPositionIncrementAttribute>();
            realOffsetAtt = realStream.AddAttribute<IOffsetAttribute>();

            termAtt = AddAttribute<ITermAttribute>();
            posIncrAtt = AddAttribute<IPositionIncrementAttribute>();
            offsetAtt = AddAttribute<IOffsetAttribute>();
        }

        public override bool IncrementToken()
        {

            if (currentRealToken == null)
            {
                bool next = realStream.IncrementToken();
                if (!next)
                {
                    return false;
                }
                //Token nextRealToken = new Token(, offsetAtt.startOffset(), offsetAtt.endOffset());
                ClearAttributes();
                termAtt.SetTermBuffer(realTermAtt.Term);
                offsetAtt.SetOffset(realOffsetAtt.StartOffset, realOffsetAtt.EndOffset);
                posIncrAtt.PositionIncrement = realPosIncrAtt.PositionIncrement;

                String expansions = synonyms[realTermAtt.Term];
                if (expansions == null)
                {
                    return true;
                }
                st = new Tokenizer(expansions, ",");
                if (st.HasMoreTokens())
                {
                    currentRealToken = new Token(realOffsetAtt.StartOffset, realOffsetAtt.EndOffset);
                    currentRealToken.SetTermBuffer(realTermAtt.Term);
                }

                return true;
            }
            else
            {
                String tok = st.NextToken();
                ClearAttributes();
                termAtt.SetTermBuffer(tok);
                offsetAtt.SetOffset(currentRealToken.StartOffset, currentRealToken.EndOffset);
                posIncrAtt.PositionIncrement = 0;
                if (!st.HasMoreTokens())
                {
                    currentRealToken = null;
                    st = null;
                }
                return true;
            }
        }

        protected override void Dispose(bool disposing)
        {

        }
    }

    internal class TestHighlightRunner
    {
        public static readonly int QUERY = 0;
        public static readonly int QUERY_TERM = 1;

        public Action TestAction { get; set; }
        public int Mode { get; private set; }

        public TestHighlightRunner()
            : this(QUERY)
        {
        }

        public TestHighlightRunner(int mode)
        {
            Mode = mode;
        }

        public Highlighter GetHighlighter(Query query, String fieldName, TokenStream stream, IFormatter formatter)
        {
            return GetHighlighter(query, fieldName, stream, formatter, true);
        }

        public Highlighter GetHighlighter(Query query, String fieldName, TokenStream stream, IFormatter formatter,
                                          bool expanMultiTerm)
        {
            IScorer scorer = null;
            if (Mode == QUERY)
            {
                scorer = new QueryScorer(query, fieldName);
                if (!expanMultiTerm)
                {
                    ((QueryScorer) scorer).IsExpandMultiTermQuery = false;
                }
            }
            else if (Mode == QUERY_TERM)
            {
                scorer = new QueryTermScorer(query);
            }
            else
            {
                throw new SystemException("Unknown highlight mode");
            }

            return new Highlighter(formatter, scorer);
        }

        public Highlighter GetHighlighter(WeightedTerm[] weightedTerms, IFormatter formatter)
        {
            if (Mode == QUERY)
            {
                return new Highlighter(formatter, new QueryScorer((WeightedSpanTerm[]) weightedTerms));
            }
            else if (Mode == QUERY_TERM)
            {
                return new Highlighter(formatter, new QueryTermScorer(weightedTerms));

            }
            else
            {
                throw new SystemException("Unknown highlight mode");
            }
        }

        public void DoStandardHighlights(Analyzer analyzer, IndexSearcher searcher,
                                         TopDocs hits, Query query, IFormatter formatter)
        {
            DoStandardHighlights(analyzer, searcher, hits, query, formatter, false);
        }

        public void DoStandardHighlights(Analyzer analyzer, IndexSearcher searcher,
                                         TopDocs hits, Query query, IFormatter formatter, bool expandMT)
        {
            IFragmenter frag = new SimpleFragmenter(20);

            for (int i = 0; i < hits.TotalHits; i++)
            {
                String text = searcher.Doc(hits.ScoreDocs[i].Doc).Get(HighlighterTest.FIELD_NAME);
                int maxNumFragmentsRequired = 2;
                String fragmentSeparator = "...";
                IScorer scorer = null;
                TokenStream tokenStream = analyzer.TokenStream(HighlighterTest.FIELD_NAME, new StringReader(text));
                if (Mode == QUERY)
                {
                    scorer = new QueryScorer(query);
                }
                else if (Mode == QUERY_TERM)
                {
                    scorer = new QueryTermScorer(query);
                }
                var highlighter = new Highlighter(formatter, scorer) {TextFragmenter = frag};

                String result = highlighter.GetBestFragments(tokenStream, text, maxNumFragmentsRequired,
                                                             fragmentSeparator);
                Console.WriteLine("\t" + result);
            }
        }

        public void Run()
        {
            if (TestAction == null) throw new InvalidOperationException("Must set TestAction before calling run!");
            TestAction();
        }

        public void Start()
        {
            if (TestAction == null) throw new InvalidOperationException("Must set TestAction before calling start!");
            Console.WriteLine("Run QueryScorer");
            TestAction();
            Console.WriteLine("Run QueryTermScorer");
            Mode = QUERY_TERM;
            TestAction();
        }
    }
}