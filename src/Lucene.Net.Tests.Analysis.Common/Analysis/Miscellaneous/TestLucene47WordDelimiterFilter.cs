// Lucene version compatibility level 4.8.1
using Lucene.Net.Analysis.Core;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Analysis.TokenAttributes;
using Lucene.Net.Analysis.Util;
using NUnit.Framework;
using System;
using System.IO;

namespace Lucene.Net.Analysis.Miscellaneous
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
    /// New WordDelimiterFilter tests... most of the tests are in ConvertedLegacyTest
    /// TODO: should explicitly test things like protWords and not rely on
    /// the factory tests in Solr.
    /// </summary>
    [Obsolete]
    public class TestLucene47WordDelimiterFilter : BaseTokenStreamTestCase


    // public void TestPerformance() throws IOException
    // {
    //  String s = "now is the time-for all good men to come to-the aid of their country.";
    //  Token tok = new Token();
    //  long start = J2N.Time.NanoTime() / J2N.Time.MillisecondsPerNanosecond; // LUCENENET: Use NanoTime() rather than CurrentTimeMilliseconds() for more accurate/reliable results
    //  int ret = 0;
    //  for (int i = 0; i<1000000; i++) {
    //    StringReader r = new StringReader(s);
    //    TokenStream ts = new WhitespaceTokenizer(r);
    //    ts = new WordDelimiterFilter(ts, 1,1,1,1,0);
    // 
    //    while (ts.next(tok) != null) ret++;
    //  }
    // 
    //  System.out.println("ret="+ret+" time="+(J2N.Time.NanoTime() / J2N.Time.MillisecondsPerNanosecond-start)); // LUCENENET: Use NanoTime() rather than CurrentTimeMilliseconds() for more accurate/reliable results
    // }


    {
        [Test]
        public virtual void TestOffsets()
        {
            WordDelimiterFlags flags = WordDelimiterFlags.GENERATE_WORD_PARTS 
                | WordDelimiterFlags.GENERATE_NUMBER_PARTS 
                | WordDelimiterFlags.CATENATE_ALL 
                | WordDelimiterFlags.SPLIT_ON_CASE_CHANGE 
                | WordDelimiterFlags.SPLIT_ON_NUMERICS 
                | WordDelimiterFlags.STEM_ENGLISH_POSSESSIVE;
            // test that subwords and catenated subwords have
            // the correct offsets.
            TokenFilter wdf = new Lucene47WordDelimiterFilter(new SingleTokenTokenStream(new Token("foo-bar", 5, 12)), WordDelimiterIterator.DEFAULT_WORD_DELIM_TABLE, flags, null);

            AssertTokenStreamContents(wdf, new string[] { "foo", "bar", "foobar" }, new int[] { 5, 9, 5 }, new int[] { 8, 12, 12 }, null, null, null, null, false);

            wdf = new Lucene47WordDelimiterFilter(new SingleTokenTokenStream(new Token("foo-bar", 5, 6)), WordDelimiterIterator.DEFAULT_WORD_DELIM_TABLE, flags, null);

            AssertTokenStreamContents(wdf, new string[] { "foo", "bar", "foobar" }, new int[] { 5, 5, 5 }, new int[] { 6, 6, 6 }, null, null, null, null, false);
        }

        [Test]
        public virtual void TestOffsetChange()
        {
            WordDelimiterFlags flags = WordDelimiterFlags.GENERATE_WORD_PARTS 
                | WordDelimiterFlags.GENERATE_NUMBER_PARTS 
                | WordDelimiterFlags.CATENATE_ALL 
                | WordDelimiterFlags.SPLIT_ON_CASE_CHANGE 
                | WordDelimiterFlags.SPLIT_ON_NUMERICS 
                | WordDelimiterFlags.STEM_ENGLISH_POSSESSIVE;
            TokenFilter wdf = new Lucene47WordDelimiterFilter(new SingleTokenTokenStream(new Token("übelkeit)", 7, 16)), WordDelimiterIterator.DEFAULT_WORD_DELIM_TABLE, flags, null);

            AssertTokenStreamContents(wdf, new string[] { "übelkeit" }, new int[] { 7 }, new int[] { 15 });
        }

        [Test]
        public virtual void TestOffsetChange2()
        {
            WordDelimiterFlags flags = WordDelimiterFlags.GENERATE_WORD_PARTS 
                | WordDelimiterFlags.GENERATE_NUMBER_PARTS 
                | WordDelimiterFlags.CATENATE_ALL 
                | WordDelimiterFlags.SPLIT_ON_CASE_CHANGE 
                | WordDelimiterFlags.SPLIT_ON_NUMERICS 
                | WordDelimiterFlags.STEM_ENGLISH_POSSESSIVE;
            TokenFilter wdf = new Lucene47WordDelimiterFilter(new SingleTokenTokenStream(new Token("(übelkeit", 7, 17)), WordDelimiterIterator.DEFAULT_WORD_DELIM_TABLE, flags, null);

            AssertTokenStreamContents(wdf, new string[] { "übelkeit" }, new int[] { 8 }, new int[] { 17 });
        }

        [Test]
        public virtual void TestOffsetChange3()
        {
            WordDelimiterFlags flags = WordDelimiterFlags.GENERATE_WORD_PARTS 
                | WordDelimiterFlags.GENERATE_NUMBER_PARTS 
                | WordDelimiterFlags.CATENATE_ALL 
                | WordDelimiterFlags.SPLIT_ON_CASE_CHANGE 
                | WordDelimiterFlags.SPLIT_ON_NUMERICS 
                | WordDelimiterFlags.STEM_ENGLISH_POSSESSIVE;
            TokenFilter wdf = new Lucene47WordDelimiterFilter(new SingleTokenTokenStream(new Token("(übelkeit", 7, 16)), WordDelimiterIterator.DEFAULT_WORD_DELIM_TABLE, flags, null);

            AssertTokenStreamContents(wdf, new string[] { "übelkeit" }, new int[] { 8 }, new int[] { 16 });
        }

        [Test]
        public virtual void TestOffsetChange4()
        {
            WordDelimiterFlags flags = WordDelimiterFlags.GENERATE_WORD_PARTS 
                | WordDelimiterFlags.GENERATE_NUMBER_PARTS 
                | WordDelimiterFlags.CATENATE_ALL 
                | WordDelimiterFlags.SPLIT_ON_CASE_CHANGE 
                | WordDelimiterFlags.SPLIT_ON_NUMERICS 
                | WordDelimiterFlags.STEM_ENGLISH_POSSESSIVE;
            TokenFilter wdf = new Lucene47WordDelimiterFilter(new SingleTokenTokenStream(new Token("(foo,bar)", 7, 16)), WordDelimiterIterator.DEFAULT_WORD_DELIM_TABLE, flags, null);

            AssertTokenStreamContents(wdf, new string[] { "foo", "bar", "foobar" }, new int[] { 8, 12, 8 }, new int[] { 11, 15, 15 }, null, null, null, null, false);
        }

        public virtual void DoSplit(string input, params string[] output)
        {
            WordDelimiterFlags flags = WordDelimiterFlags.GENERATE_WORD_PARTS 
                | WordDelimiterFlags.GENERATE_NUMBER_PARTS 
                | WordDelimiterFlags.SPLIT_ON_CASE_CHANGE 
                | WordDelimiterFlags.SPLIT_ON_NUMERICS 
                | WordDelimiterFlags.STEM_ENGLISH_POSSESSIVE;
            MockTokenizer tokenizer = new MockTokenizer(new StringReader(input), MockTokenizer.KEYWORD, false);
            TokenFilter wdf = new Lucene47WordDelimiterFilter(tokenizer, WordDelimiterIterator.DEFAULT_WORD_DELIM_TABLE, flags, null);

            AssertTokenStreamContents(wdf, output);
        }

        [Test]
        public virtual void TestSplits()
        {
            DoSplit("basic-split", "basic", "split");
            DoSplit("camelCase", "camel", "Case");

            // non-space marking symbol shouldn't cause split
            // this is an example in Thai    
            DoSplit("\u0e1a\u0e49\u0e32\u0e19", "\u0e1a\u0e49\u0e32\u0e19");
            // possessive followed by delimiter
            DoSplit("test's'", "test");

            // some russian upper and lowercase
            DoSplit("Роберт", "Роберт");
            // now cause a split (russian camelCase)
            DoSplit("РобЕрт", "Роб", "Ерт");

            // a composed titlecase character, don't split
            DoSplit("aǅungla", "aǅungla");

            // a modifier letter, don't split
            DoSplit("ســـــــــــــــــلام", "ســـــــــــــــــلام");

            // enclosing mark, don't split
            DoSplit("test⃝", "test⃝");

            // combining spacing mark (the virama), don't split
            DoSplit("हिन्दी", "हिन्दी");

            // don't split non-ascii digits
            DoSplit("١٢٣٤", "١٢٣٤");

            // don't split supplementaries into unpaired surrogates
            DoSplit("𠀀𠀀", "𠀀𠀀");
        }

        public virtual void DoSplitPossessive(int stemPossessive, string input, params string[] output)
        {
            WordDelimiterFlags flags = WordDelimiterFlags.GENERATE_WORD_PARTS 
                | WordDelimiterFlags.GENERATE_NUMBER_PARTS 
                | WordDelimiterFlags.SPLIT_ON_CASE_CHANGE 
                | WordDelimiterFlags.SPLIT_ON_NUMERICS;
            flags |= (stemPossessive == 1) ? WordDelimiterFlags.STEM_ENGLISH_POSSESSIVE : 0;
            MockTokenizer tokenizer = new MockTokenizer(new StringReader(input), MockTokenizer.KEYWORD, false);
            TokenFilter wdf = new Lucene47WordDelimiterFilter(tokenizer, flags, null);

            AssertTokenStreamContents(wdf, output);
        }

        /*
         * Test option that allows disabling the special "'s" stemming, instead treating the single quote like other delimiters. 
         */
        [Test]
        public virtual void TestPossessives()
        {
            DoSplitPossessive(1, "ra's", "ra");
            DoSplitPossessive(0, "ra's", "ra", "s");
        }

        /*
         * Set a large position increment gap of 10 if the token is "largegap" or "/"
         */
        private sealed class LargePosIncTokenFilter : TokenFilter
        {
            internal ICharTermAttribute termAtt;
            internal IPositionIncrementAttribute posIncAtt;

            public LargePosIncTokenFilter(TokenStream input) : base(input)
            {
                this.termAtt = AddAttribute<ICharTermAttribute>();
                this.posIncAtt = AddAttribute<IPositionIncrementAttribute>();
            }

            public override sealed bool IncrementToken()
            {
                if (m_input.IncrementToken())
                {
                    if (termAtt.ToString().Equals("largegap", StringComparison.Ordinal) || termAtt.ToString().Equals("/", StringComparison.Ordinal))
                    {
                        posIncAtt.PositionIncrement = 10;
                    }
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        [Test]
        public virtual void TestPositionIncrements()
        {
            WordDelimiterFlags flags = WordDelimiterFlags.GENERATE_WORD_PARTS 
                | WordDelimiterFlags.GENERATE_NUMBER_PARTS 
                | WordDelimiterFlags.CATENATE_ALL 
                | WordDelimiterFlags.SPLIT_ON_CASE_CHANGE 
                | WordDelimiterFlags.SPLIT_ON_NUMERICS 
                | WordDelimiterFlags.STEM_ENGLISH_POSSESSIVE;
            CharArraySet protWords = new CharArraySet(TEST_VERSION_CURRENT, new string[] { "NUTCH" }, false);

            /* analyzer that uses whitespace + wdf */
            Analyzer a = Analyzer.NewAnonymous(createComponents: (fieldName, reader) =>
            {
                Tokenizer tokenizer = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
                return new TokenStreamComponents(tokenizer, new Lucene47WordDelimiterFilter(tokenizer, flags, protWords));
            });

            /* in this case, works as expected. */
            AssertAnalyzesTo(a, "LUCENE / SOLR", new string[] { "LUCENE", "SOLR" }, new int[] { 0, 9 }, new int[] { 6, 13 }, null, new int[] { 1, 1 }, null, false);

            /* only in this case, posInc of 2 ?! */
            AssertAnalyzesTo(a, "LUCENE / solR", new string[] { "LUCENE", "sol", "R", "solR" }, new int[] { 0, 9, 12, 9 }, new int[] { 6, 12, 13, 13 }, null, new int[] { 1, 1, 1, 0 }, null, false);

            AssertAnalyzesTo(a, "LUCENE / NUTCH SOLR", new string[] { "LUCENE", "NUTCH", "SOLR" }, new int[] { 0, 9, 15 }, new int[] { 6, 14, 19 }, null, new int[] { 1, 1, 1 }, null, false);

            /* analyzer that will consume tokens with large position increments */
            Analyzer a2 = Analyzer.NewAnonymous(createComponents: (fieldName, reader) =>
            {
                Tokenizer tokenizer = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
                return new TokenStreamComponents(tokenizer, new Lucene47WordDelimiterFilter(new LargePosIncTokenFilter(tokenizer), flags, protWords));
            });

            /* increment of "largegap" is preserved */
            AssertAnalyzesTo(a2, "LUCENE largegap SOLR", new string[] { "LUCENE", "largegap", "SOLR" }, new int[] { 0, 7, 16 }, new int[] { 6, 15, 20 }, null, new int[] { 1, 10, 1 }, null, false);

            /* the "/" had a position increment of 10, where did it go?!?!! */
            AssertAnalyzesTo(a2, "LUCENE / SOLR", new string[] { "LUCENE", "SOLR" }, new int[] { 0, 9 }, new int[] { 6, 13 }, null, new int[] { 1, 11 }, null, false);

            /* in this case, the increment of 10 from the "/" is carried over */
            AssertAnalyzesTo(a2, "LUCENE / solR", new string[] { "LUCENE", "sol", "R", "solR" }, new int[] { 0, 9, 12, 9 }, new int[] { 6, 12, 13, 13 }, null, new int[] { 1, 11, 1, 0 }, null, false);

            AssertAnalyzesTo(a2, "LUCENE / NUTCH SOLR", new string[] { "LUCENE", "NUTCH", "SOLR" }, new int[] { 0, 9, 15 }, new int[] { 6, 14, 19 }, null, new int[] { 1, 11, 1 }, null, false);

            Analyzer a3 = Analyzer.NewAnonymous(createComponents: (fieldName, reader) =>
            {
                Tokenizer tokenizer = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
                StopFilter filter = new StopFilter(TEST_VERSION_CURRENT, tokenizer, StandardAnalyzer.STOP_WORDS_SET);
                return new TokenStreamComponents(tokenizer, new Lucene47WordDelimiterFilter(filter, flags, protWords));
            });

            AssertAnalyzesTo(a3, "lucene.solr", new string[] { "lucene", "solr", "lucenesolr" }, new int[] { 0, 7, 0 }, new int[] { 6, 11, 11 }, null, new int[] { 1, 1, 0 }, null, false);

            /* the stopword should add a gap here */
            AssertAnalyzesTo(a3, "the lucene.solr", new string[] { "lucene", "solr", "lucenesolr" }, new int[] { 4, 11, 4 }, new int[] { 10, 15, 15 }, null, new int[] { 2, 1, 0 }, null, false);
        }

        /// <summary>
        /// blast some random strings through the analyzer </summary>
        [Test]
        public virtual void TestRandomStrings()
        {
            int numIterations = AtLeast(5);
            for (int i = 0; i < numIterations; i++)
            {
                WordDelimiterFlags flags = (WordDelimiterFlags)Random.Next(512);
                CharArraySet protectedWords;
                if (Random.nextBoolean())
                {
                    protectedWords = new CharArraySet(TEST_VERSION_CURRENT, new string[] { "a", "b", "cd" }, false);
                }
                else
                {
                    protectedWords = null;
                }

                Analyzer a = Analyzer.NewAnonymous(createComponents: (fieldName, reader) =>
                {
                    Tokenizer tokenizer = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
                    return new TokenStreamComponents(tokenizer, new Lucene47WordDelimiterFilter(tokenizer, flags, protectedWords));
                });
                CheckRandomData(Random, a, 200, 20, false, false);
            }
        }

        [Test]
        public virtual void TestEmptyTerm()
        {
            Random random = Random;
            for (int i = 0; i < 512; i++)
            {
                WordDelimiterFlags flags = (WordDelimiterFlags)i;
                CharArraySet protectedWords;
                if (random.nextBoolean())
                {
                    protectedWords = new CharArraySet(TEST_VERSION_CURRENT, new string[] { "a", "b", "cd" }, false);
                }
                else
                {
                    protectedWords = null;
                }

                Analyzer a = Analyzer.NewAnonymous(createComponents: (fieldName, reader) =>
                {
                    Tokenizer tokenizer = new KeywordTokenizer(reader);
                    return new TokenStreamComponents(tokenizer, new Lucene47WordDelimiterFilter(tokenizer, flags, protectedWords));
                });
                // depending upon options, this thing may or may not preserve the empty term
                CheckAnalysisConsistency(random, a, random.nextBoolean(), "");
            }
        }
    }
}