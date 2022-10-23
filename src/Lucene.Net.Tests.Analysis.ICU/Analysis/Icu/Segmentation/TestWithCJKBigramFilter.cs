// Lucene version compatibility level < 7.1.0
using Lucene.Net.Analysis.Cjk;
using Lucene.Net.Analysis.Core;
using Lucene.Net.Analysis.Util;
using Lucene.Net.Util;
using NUnit.Framework;

namespace Lucene.Net.Analysis.Icu.Segmentation
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
    /// Tests ICUTokenizer's ability to work with CJKBigramFilter.
    /// Most tests adopted from TestCJKTokenizer
    /// </summary>
    public class TestWithCJKBigramFilter : BaseTokenStreamTestCase
    {
        private Analyzer analyzer, analyzer2;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            /**
            * ICUTokenizer+CJKBigramFilter
            */
            analyzer = Analyzer.NewAnonymous(createComponents: (fieldName, reader) =>
            {
                Tokenizer source = new ICUTokenizer(reader, new DefaultICUTokenizerConfig(false, true));
                TokenStream result = new CJKBigramFilter(source);
                return new TokenStreamComponents(source, new StopFilter(TEST_VERSION_CURRENT, result, CharArraySet.Empty));
            });

            /**
             * ICUTokenizer+ICUNormalizer2Filter+CJKBigramFilter.
             * 
             * ICUNormalizer2Filter uses nfkc_casefold by default, so this is a language-independent
             * superset of CJKWidthFilter's foldings.
             */
            analyzer2 = Analyzer.NewAnonymous(createComponents: (fieldName, reader) =>
            {
                Tokenizer source = new ICUTokenizer(reader, new DefaultICUTokenizerConfig(false, true));
                // we put this before the CJKBigramFilter, because the normalization might combine
                // some halfwidth katakana forms, which will affect the bigramming.
                TokenStream result = new ICUNormalizer2Filter(source);
                result = new CJKBigramFilter(source);
                return new TokenStreamComponents(source, new StopFilter(TEST_VERSION_CURRENT, result, CharArraySet.Empty));
            });
        }

        [TearDown]
        public override void TearDown()
        {
            IOUtils.Dispose(analyzer, analyzer2);
            base.TearDown();
        }

        [Test]
        public void TestJa1()
        {
            AssertAnalyzesTo(analyzer, "一二三四五六七八九十",
                new string[] { "一二", "二三", "三四", "四五", "五六", "六七", "七八", "八九", "九十" },
                new int[] { 0, 1, 2, 3, 4, 5, 6, 7, 8 },
                new int[] { 2, 3, 4, 5, 6, 7, 8, 9, 10 },
                new string[] { "<DOUBLE>", "<DOUBLE>", "<DOUBLE>", "<DOUBLE>", "<DOUBLE>", "<DOUBLE>", "<DOUBLE>", "<DOUBLE>", "<DOUBLE>" },
                new int[] { 1, 1, 1, 1, 1, 1, 1, 1, 1 });
        }

        [Test]
        public void TestJa2()
        {
            AssertAnalyzesTo(analyzer, "一 二三四 五六七八九 十",
                new string[] { "一", "二三", "三四", "五六", "六七", "七八", "八九", "十" },
                new int[] { 0, 2, 3, 6, 7, 8, 9, 12 },
                new int[] { 1, 4, 5, 8, 9, 10, 11, 13 },
                new string[] { "<SINGLE>", "<DOUBLE>", "<DOUBLE>", "<DOUBLE>", "<DOUBLE>", "<DOUBLE>", "<DOUBLE>", "<SINGLE>" },
                new int[] { 1, 1, 1, 1, 1, 1, 1, 1 });
        }

        [Test]
        public void TestC()
        {
            AssertAnalyzesTo(analyzer, "abc defgh ijklmn opqrstu vwxy z",
                new string[] { "abc", "defgh", "ijklmn", "opqrstu", "vwxy", "z" },
                new int[] { 0, 4, 10, 17, 25, 30 },
                new int[] { 3, 9, 16, 24, 29, 31 },
                new string[] { "<ALPHANUM>", "<ALPHANUM>", "<ALPHANUM>", "<ALPHANUM>", "<ALPHANUM>", "<ALPHANUM>" },
                new int[] { 1, 1, 1, 1, 1, 1 });
        }

        /**
         * LUCENE-2207: wrong offset calculated by end() 
         */
        [Test]
        public void TestFinalOffset()
        {
            AssertAnalyzesTo(analyzer, "あい",
                new string[] { "あい" },
                new int[] { 0 },
                new int[] { 2 },
                new string[] { "<DOUBLE>" },
                new int[] { 1 });


            AssertAnalyzesTo(analyzer, "あい   ",
                new string[] { "あい" },
                new int[] { 0 },
                new int[] { 2 },
                new string[] { "<DOUBLE>" },
                new int[] { 1 });

            AssertAnalyzesTo(analyzer, "test",
                new string[] { "test" },
                new int[] { 0 },
                new int[] { 4 },
                new string[] { "<ALPHANUM>" },
                new int[] { 1 });


            AssertAnalyzesTo(analyzer, "test   ",
                new string[] { "test" },
                new int[] { 0 },
                new int[] { 4 },
                new string[] { "<ALPHANUM>" },
                new int[] { 1 });


            AssertAnalyzesTo(analyzer, "あいtest",
                new string[] { "あい", "test" },
                new int[] { 0, 2 },
                new int[] { 2, 6 },
                new string[] { "<DOUBLE>", "<ALPHANUM>" },
                new int[] { 1, 1 });


            AssertAnalyzesTo(analyzer, "testあい    ",
                new string[] { "test", "あい" },
                new int[] { 0, 4 },
                new int[] { 4, 6 },
                new string[] { "<ALPHANUM>", "<DOUBLE>" },
                new int[] { 1, 1 });
        }

        [Test]
        public void TestMix()
        {
            AssertAnalyzesTo(analyzer, "あいうえおabcかきくけこ",
                new string[] { "あい", "いう", "うえ", "えお", "abc", "かき", "きく", "くけ", "けこ" },
                new int[] { 0, 1, 2, 3, 5, 8, 9, 10, 11 },
                new int[] { 2, 3, 4, 5, 8, 10, 11, 12, 13 },
                new string[] { "<DOUBLE>", "<DOUBLE>", "<DOUBLE>", "<DOUBLE>", "<ALPHANUM>", "<DOUBLE>", "<DOUBLE>", "<DOUBLE>", "<DOUBLE>" },
                new int[] { 1, 1, 1, 1, 1, 1, 1, 1, 1 });
        }

        [Test]
        public void TestMix2()
        {
            AssertAnalyzesTo(analyzer, "あいうえおabんcかきくけ こ",
                new string[] { "あい", "いう", "うえ", "えお", "ab", "ん", "c", "かき", "きく", "くけ", "こ" },
                new int[] { 0, 1, 2, 3, 5, 7, 8, 9, 10, 11, 14 },
                new int[] { 2, 3, 4, 5, 7, 8, 9, 11, 12, 13, 15 },
                new string[] { "<DOUBLE>", "<DOUBLE>", "<DOUBLE>", "<DOUBLE>", "<ALPHANUM>", "<SINGLE>", "<ALPHANUM>", "<DOUBLE>", "<DOUBLE>", "<DOUBLE>", "<SINGLE>" },
                new int[] { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 });
        }

        /**
         * Non-english text (outside of CJK) is treated normally, according to unicode rules 
         */
        [Test]
        public void TestNonIdeographic()
        {
            AssertAnalyzesTo(analyzer, "一 روبرت موير",
                new string[] { "一", "روبرت", "موير" },
                new int[] { 0, 2, 8 },
                new int[] { 1, 7, 12 },
                new string[] { "<SINGLE>", "<ALPHANUM>", "<ALPHANUM>" },
                new int[] { 1, 1, 1 });
        }

        /**
         * Same as the above, except with a nonspacing mark to show correctness.
         */
        [Test]
        public void TestNonIdeographicNonLetter()
        {
            AssertAnalyzesTo(analyzer, "一 رُوبرت موير",
                new string[] { "一", "رُوبرت", "موير" },
                new int[] { 0, 2, 9 },
                new int[] { 1, 8, 13 },
                new string[] { "<SINGLE>", "<ALPHANUM>", "<ALPHANUM>" },
                new int[] { 1, 1, 1 });
        }

        [Test]
        public void TestSurrogates()
        {
            AssertAnalyzesTo(analyzer, "𩬅艱鍟䇹愯瀛",
                new string[] { "𩬅艱", "艱鍟", "鍟䇹", "䇹愯", "愯瀛" },
                new int[] { 0, 2, 3, 4, 5 },
                new int[] { 3, 4, 5, 6, 7 },
                new string[] { "<DOUBLE>", "<DOUBLE>", "<DOUBLE>", "<DOUBLE>", "<DOUBLE>" },
                new int[] { 1, 1, 1, 1, 1 });
        }

        [Test]
        public void TestReusableTokenStream()
        {
            AssertAnalyzesTo(analyzer, "あいうえおabcかきくけこ",
                new string[] { "あい", "いう", "うえ", "えお", "abc", "かき", "きく", "くけ", "けこ" },
                new int[] { 0, 1, 2, 3, 5, 8, 9, 10, 11 },
                new int[] { 2, 3, 4, 5, 8, 10, 11, 12, 13 },
                new string[] { "<DOUBLE>", "<DOUBLE>", "<DOUBLE>", "<DOUBLE>", "<ALPHANUM>", "<DOUBLE>", "<DOUBLE>", "<DOUBLE>", "<DOUBLE>" },
                new int[] { 1, 1, 1, 1, 1, 1, 1, 1, 1 });


            AssertAnalyzesTo(analyzer, "あいうえおabんcかきくけ こ",
                new string[] { "あい", "いう", "うえ", "えお", "ab", "ん", "c", "かき", "きく", "くけ", "こ" },
                new int[] { 0, 1, 2, 3, 5, 7, 8, 9, 10, 11, 14 },
                new int[] { 2, 3, 4, 5, 7, 8, 9, 11, 12, 13, 15 },
                new string[] { "<DOUBLE>", "<DOUBLE>", "<DOUBLE>", "<DOUBLE>", "<ALPHANUM>", "<SINGLE>", "<ALPHANUM>", "<DOUBLE>", "<DOUBLE>", "<DOUBLE>", "<SINGLE>" },
                new int[] { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 });
        }

        [Test]
        public void TestSingleChar()
        {
            AssertAnalyzesTo(analyzer, "一",
                new string[] { "一" },
                new int[] { 0 },
                new int[] { 1 },
                new string[] { "<SINGLE>" },
                new int[] { 1 });
        }

        [Test]
        public void TestTokenStream()
        {
            AssertAnalyzesTo(analyzer, "一丁丂",
              new string[] { "一丁", "丁丂" },
              new int[] { 0, 1 },
              new int[] { 2, 3 },
              new string[] { "<DOUBLE>", "<DOUBLE>" },
              new int[] { 1, 1 });
        }
    }
}