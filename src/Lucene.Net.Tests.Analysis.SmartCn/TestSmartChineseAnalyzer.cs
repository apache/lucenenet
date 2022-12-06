using Lucene.Net.Analysis.Core;
using Lucene.Net.Analysis.Miscellaneous;
using Lucene.Net.Util;
using NUnit.Framework;
using System;
using System.Text;

namespace Lucene.Net.Analysis.Cn.Smart
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

    public class TestSmartChineseAnalyzer : BaseTokenStreamTestCase
    {
        [Test]
        public void TestChineseStopWordsDefault()
        {
#pragma warning disable 612, 618
            Analyzer ca = new SmartChineseAnalyzer(LuceneVersion.LUCENE_CURRENT); /* will load stopwords */
#pragma warning restore 612, 618
            String sentence = "我购买了道具和服装。";
            String[] result = { "我", "购买", "了", "道具", "和", "服装" };
            AssertAnalyzesTo(ca, sentence, result);
            // set stop-words from the outer world - must yield same behavior
            ca = new SmartChineseAnalyzer(
#pragma warning disable 612, 618
                LuceneVersion.LUCENE_CURRENT,
#pragma warning restore 612, 618
                SmartChineseAnalyzer.DefaultStopSet);
            AssertAnalyzesTo(ca, sentence, result);
        }

        /*
         * This test is the same as the above, except with two phrases.
         * This tests to ensure the SentenceTokenizer->WordTokenFilter chain works correctly.
         */
        [Test]
        public void TestChineseStopWordsDefaultTwoPhrases()
        {
#pragma warning disable 612, 618
            Analyzer ca = new SmartChineseAnalyzer(LuceneVersion.LUCENE_CURRENT); /* will load stopwords */
#pragma warning restore 612, 618
            String sentence = "我购买了道具和服装。 我购买了道具和服装。";
            String[] result = { "我", "购买", "了", "道具", "和", "服装", "我", "购买", "了", "道具", "和", "服装" };
            AssertAnalyzesTo(ca, sentence, result);
        }

        /*
         * This test is the same as the above, except using an ideographic space as a separator.
         * This tests to ensure the stopwords are working correctly.
         */
        [Test]
        public void TestChineseStopWordsDefaultTwoPhrasesIdeoSpace()
        {
#pragma warning disable 612, 618
            Analyzer ca = new SmartChineseAnalyzer(LuceneVersion.LUCENE_CURRENT); /* will load stopwords */
#pragma warning restore 612, 618
            String sentence = "我购买了道具和服装　我购买了道具和服装。";
            String[] result = { "我", "购买", "了", "道具", "和", "服装", "我", "购买", "了", "道具", "和", "服装" };
            AssertAnalyzesTo(ca, sentence, result);
        }

        /*
         * Punctuation is handled in a strange way if you disable stopwords
         * In this example the IDEOGRAPHIC FULL STOP is converted into a comma.
         * if you don't supply (true) to the constructor, or use a different stopwords list,
         * then punctuation is indexed.
         */
        [Test]
        public void TestChineseStopWordsOff()
        {
            Analyzer[]
            analyzers = new Analyzer[] {
#pragma warning disable 612, 618
                new SmartChineseAnalyzer(LuceneVersion.LUCENE_CURRENT, false),/* doesn't load stopwords */
                new SmartChineseAnalyzer(LuceneVersion.LUCENE_CURRENT, null) /* sets stopwords to empty set */};
#pragma warning restore 612, 618
            String sentence = "我购买了道具和服装。";
            String[] result = { "我", "购买", "了", "道具", "和", "服装", "," };
            foreach (Analyzer analyzer in analyzers)
            {
                AssertAnalyzesTo(analyzer, sentence, result);
                AssertAnalyzesTo(analyzer, sentence, result);
            }
        }

        /*
         * Check that position increments after stopwords are correct,
         * when stopfilter is configured with enablePositionIncrements
         */
        [Test]
        public void TestChineseStopWords2()
        {
#pragma warning disable 612, 618
            Analyzer ca = new SmartChineseAnalyzer(LuceneVersion.LUCENE_CURRENT); /* will load stopwords */
#pragma warning restore 612, 618
            String sentence = "Title:San"; // : is a stopword
            String[] result = { "titl", "san" };
            int[] startOffsets = { 0, 6 };
            int[] endOffsets = { 5, 9 };
            int[] posIncr = { 1, 2 };
            AssertAnalyzesTo(ca, sentence, result, startOffsets, endOffsets, posIncr);
        }

        [Test]
        public void TestChineseAnalyzer()
        {
#pragma warning disable 612, 618
            Analyzer ca = new SmartChineseAnalyzer(LuceneVersion.LUCENE_CURRENT, true);
#pragma warning restore 612, 618
            String sentence = "我购买了道具和服装。";
            String[] result = { "我", "购买", "了", "道具", "和", "服装" };
            AssertAnalyzesTo(ca, sentence, result);
        }

        /*
         * English words are lowercased and porter-stemmed.
         */
        [Test]
        public void TestMixedLatinChinese()
        {
            AssertAnalyzesTo(new SmartChineseAnalyzer(
#pragma warning disable 612, 618
                LuceneVersion.LUCENE_CURRENT
#pragma warning restore 612, 618
                , true), "我购买 Tests 了道具和服装",
                new String[] { "我", "购买", "test", "了", "道具", "和", "服装" });
        }

        /*
         * Numerics are parsed as their own tokens
         */
        [Test]
        public void TestNumerics()
        {
            AssertAnalyzesTo(new SmartChineseAnalyzer(
#pragma warning disable 612, 618
                LuceneVersion.LUCENE_CURRENT
#pragma warning restore 612, 618
                , true), "我购买 Tests 了道具和服装1234",
              new String[] { "我", "购买", "test", "了", "道具", "和", "服装", "1234" });
        }

        /*
         * Full width alphas and numerics are folded to half-width
         */
        [Test]
        public void TestFullWidth()
        {
            AssertAnalyzesTo(new SmartChineseAnalyzer(
#pragma warning disable 612, 618
                LuceneVersion.LUCENE_CURRENT
#pragma warning restore 612, 618
                , true), "我购买 Ｔｅｓｔｓ 了道具和服装１２３４",
                new String[] { "我", "购买", "test", "了", "道具", "和", "服装", "1234" });
        }

        /*
         * Presentation form delimiters are removed
         */
        [Test]
        public void TestDelimiters()
        {
            AssertAnalyzesTo(new SmartChineseAnalyzer(
#pragma warning disable 612, 618
                LuceneVersion.LUCENE_CURRENT
#pragma warning restore 612, 618
                , true), "我购买︱ Tests 了道具和服装",
                new String[] { "我", "购买", "test", "了", "道具", "和", "服装" });
        }

        /*
         * Text from writing systems other than Chinese and Latin are parsed as individual characters.
         * (regardless of Unicode category)
         */
        [Test]
        public void TestNonChinese()
        {
            AssertAnalyzesTo(new SmartChineseAnalyzer(
#pragma warning disable 612, 618
                LuceneVersion.LUCENE_CURRENT
#pragma warning restore 612, 618
                , true), "我购买 روبرتTests 了道具和服装",
                new String[] { "我", "购买", "ر", "و", "ب", "ر", "ت", "test", "了", "道具", "和", "服装" });
        }

        /*
         * Test what the analyzer does with out-of-vocabulary words.
         * In this case the name is Yousaf Raza Gillani.
         * Currently it is being analyzed into single characters...
         */
        [Test]
        public void TestOOV()
        {
            AssertAnalyzesTo(new SmartChineseAnalyzer(
#pragma warning disable 612, 618
                LuceneVersion.LUCENE_CURRENT
#pragma warning restore 612, 618
                , true), "优素福·拉扎·吉拉尼",
              new String[] { "优", "素", "福", "拉", "扎", "吉", "拉", "尼" });


            AssertAnalyzesTo(new SmartChineseAnalyzer(
#pragma warning disable 612, 618
                LuceneVersion.LUCENE_CURRENT
#pragma warning restore 612, 618
                , true), "优素福拉扎吉拉尼",
              new String[] { "优", "素", "福", "拉", "扎", "吉", "拉", "尼" });
        }

        [Test]
        public void TestOffsets()
        {
            AssertAnalyzesTo(new SmartChineseAnalyzer(
#pragma warning disable 612, 618
                LuceneVersion.LUCENE_CURRENT
#pragma warning restore 612, 618
                , true), "我购买了道具和服装",
                new String[] { "我", "购买", "了", "道具", "和", "服装" },
                new int[] { 0, 1, 3, 4, 6, 7 },
                new int[] { 1, 3, 4, 6, 7, 9 });
        }

        [Test]
        public void TestReusableTokenStream()
        {
            Analyzer a = new SmartChineseAnalyzer(
#pragma warning disable 612, 618
                LuceneVersion.LUCENE_CURRENT);
#pragma warning restore 612, 618
            AssertAnalyzesTo(a, "我购买 Tests 了道具和服装",
                new String[] { "我", "购买", "test", "了", "道具", "和", "服装" },
                new int[] { 0, 1, 4, 10, 11, 13, 14 },
                new int[] { 1, 3, 9, 11, 13, 14, 16 });
            AssertAnalyzesTo(a, "我购买了道具和服装。",
                new String[] { "我", "购买", "了", "道具", "和", "服装" },
                new int[] { 0, 1, 3, 4, 6, 7 },
                new int[] { 1, 3, 4, 6, 7, 9 });
        }

        // LUCENE-3026
        [Test]
        public void TestLargeDocument()
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < 5000; i++)
            {
                sb.append("我购买了道具和服装。");
            }
            Analyzer analyzer = new SmartChineseAnalyzer(TEST_VERSION_CURRENT);
            TokenStream stream = analyzer.GetTokenStream("", sb.toString());
            try
            {
                stream.Reset();
                while (stream.IncrementToken())
                {
                }
                stream.End();
            }
            finally
            {
                IOUtils.DisposeWhileHandlingException(stream);
            }
        }

        // LUCENE-3026
        [Test]
        public void TestLargeSentence()
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < 5000; i++)
            {
                sb.append("我购买了道具和服装");
            }
            Analyzer analyzer = new SmartChineseAnalyzer(TEST_VERSION_CURRENT);
            TokenStream stream = analyzer.GetTokenStream("", sb.toString());
            try
            {
                stream.Reset();
                while (stream.IncrementToken())
                {
                }
                stream.End();
            }
            finally
            {
                IOUtils.DisposeWhileHandlingException(stream);
            }
        }

        // LUCENE-3642
        [Test]
        public void TestInvalidOffset()
        {
            Analyzer analyzer = Analyzer.NewAnonymous(createComponents: (fieldName, reader) =>
            {
                Tokenizer tokenizer = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
                TokenFilter filters = new ASCIIFoldingFilter(tokenizer);
#pragma warning disable 612, 618
                filters = new WordTokenFilter(filters);
#pragma warning restore 612, 618
                return new TokenStreamComponents(tokenizer, filters);
            });

            AssertAnalyzesTo(analyzer, "mosfellsbær",
                new string[] { "mosfellsbaer" },
                new int[] { 0 },
                new int[] { 11 });
        }

        /** blast some random strings through the analyzer */
        [Test]
        public void TestRandomStrings()
        {
            CheckRandomData(Random, new SmartChineseAnalyzer(TEST_VERSION_CURRENT), 1000 * RandomMultiplier);
        }

        /** blast some random large strings through the analyzer */
        [Test]
        [Slow]
        public void TestRandomHugeStrings()
        {
            Random random = Random;
            CheckRandomData(random, new SmartChineseAnalyzer(TEST_VERSION_CURRENT), 100 * RandomMultiplier, 8192);
        }

        [Test]
        public void TestEmptyTerm()
        {
            Random random = Random;
            Analyzer a = Analyzer.NewAnonymous(createComponents: (fieldName, reader) =>
            {
                Tokenizer tokenizer = new KeywordTokenizer(reader);
#pragma warning disable 612, 618
                return new TokenStreamComponents(tokenizer, new WordTokenFilter(tokenizer));
#pragma warning restore 612, 618
            });
            CheckAnalysisConsistency(random, a, random.nextBoolean(), "");
        }
    }
}
