// Lucene version compatibility level 4.8.1 + LUCENE-10059 (https://github.com/apache/lucene/pull/254 only)

using J2N;
using J2N.Collections.Generic.Extensions;
using J2N.Text;
using Lucene.Net.Analysis.Ja.Dict;
using Lucene.Net.Analysis.Ja.TokenAttributes;
using Lucene.Net.Analysis.TokenAttributes;
using Lucene.Net.Util;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Console = Lucene.Net.Util.SystemConsole;

namespace Lucene.Net.Analysis.Ja
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

    [Slow]
    public class TestJapaneseTokenizer : BaseTokenStreamTestCase
    {
        public static UserDictionary ReadDict()
        {
            Stream @is = typeof(TestJapaneseTokenizer).getResourceAsStream("userdict.txt");
            if (@is is null)
            {
                throw RuntimeException.Create("Cannot find userdict.txt in test classpath!");
            }
            try
            {
                try
                {
                    TextReader reader = new StreamReader(@is, Encoding.UTF8);
                    return new UserDictionary(reader);
                }
                finally
                {
                    @is.Dispose();
                }
            }
            catch (Exception ioe) when (ioe.IsIOException())
            {
                throw RuntimeException.Create(ioe);
            }
        }

        private readonly Analyzer analyzer = Analyzer.NewAnonymous(createComponents: (fieldName, reader) =>
        {
            Tokenizer tokenizer = new JapaneseTokenizer(reader, ReadDict(), false, JapaneseTokenizerMode.SEARCH);
            return new TokenStreamComponents(tokenizer, tokenizer);
        });


        private readonly Analyzer analyzerNormal = Analyzer.NewAnonymous(createComponents: (fieldName, reader) =>
        {
            Tokenizer tokenizer = new JapaneseTokenizer(reader, ReadDict(), false, JapaneseTokenizerMode.NORMAL);
            return new TokenStreamComponents(tokenizer, tokenizer);
        });

        private readonly Analyzer analyzerNoPunct = Analyzer.NewAnonymous(createComponents: (fieldName, reader) =>
        {
            Tokenizer tokenizer = new JapaneseTokenizer(reader, ReadDict(), true, JapaneseTokenizerMode.SEARCH);
            return new TokenStreamComponents(tokenizer, tokenizer);
        });


        private readonly Analyzer extendedModeAnalyzerNoPunct = Analyzer.NewAnonymous(createComponents: (fieldName, reader) =>
        {
            Tokenizer tokenizer = new JapaneseTokenizer(reader, ReadDict(), true, JapaneseTokenizerMode.EXTENDED);
            return new TokenStreamComponents(tokenizer, tokenizer);
        });


        [Test]
        public void TestNormalMode()
        {
            AssertAnalyzesTo(analyzerNormal,
                             "シニアソフトウェアエンジニア",
                             new String[] { "シニアソフトウェアエンジニア" });
        }

        [Test]
        public void TestDecomposition1()
        {
            AssertAnalyzesTo(analyzerNoPunct, "本来は、貧困層の女性や子供に医療保護を提供するために創設された制度である、" +
                                 "アメリカ低所得者医療援助制度が、今日では、その予算の約３分の１を老人に費やしている。",
             new String[] { "本来", "は",  "貧困", "層", "の", "女性", "や", "子供", "に", "医療", "保護", "を",
                    "提供", "する", "ため", "に", "創設", "さ", "れ", "た", "制度", "で", "ある",  "アメリカ",
                    "低", "所得", "者", "医療", "援助", "制度", "が",  "今日", "で", "は",  "その",
                    "予算", "の", "約", "３", "分の", "１", "を", "老人", "に", "費やし", "て", "いる" },
             new int[] { 0, 2, 4, 6, 7,  8, 10, 11, 13, 14, 16, 18, 19, 21, 23, 25, 26, 28, 29, 30,
                 31, 33, 34, 37, 41, 42, 44, 45, 47, 49, 51, 53, 55, 56, 58, 60,
                 62, 63, 64, 65, 67, 68, 69, 71, 72, 75, 76 },
             new int[] { 2, 3, 6, 7, 8, 10, 11, 13, 14, 16, 18, 19, 21, 23, 25, 26, 28, 29, 30, 31,
                 33, 34, 36, 41, 42, 44, 45, 47, 49, 51, 52, 55, 56, 57, 60, 62,
                 63, 64, 65, 67, 68, 69, 71, 72, 75, 76, 78 }
            );
        }

        [Test]
        public void TestDecomposition2()
        {
            AssertAnalyzesTo(analyzerNoPunct, "麻薬の密売は根こそぎ絶やさなければならない",
              new String[] { "麻薬", "の", "密売", "は", "根こそぎ", "絶やさ", "なけれ", "ば", "なら", "ない" },
              new int[] { 0, 2, 3, 5, 6, 10, 13, 16, 17, 19 },
              new int[] { 2, 3, 5, 6, 10, 13, 16, 17, 19, 21 }
            );
        }

        [Test]
        public void TestDecomposition3()
        {
            AssertAnalyzesTo(analyzerNoPunct, "魔女狩大将マシュー・ホプキンス。",
              new String[] { "魔女", "狩", "大将", "マシュー", "ホプキンス" },
              new int[] { 0, 2, 3, 5, 10 },
              new int[] { 2, 3, 5, 9, 15 }
            );
        }

        [Test]
        public void TestDecomposition4()
        {
            AssertAnalyzesTo(analyzer, "これは本ではない",
              new String[] { "これ", "は", "本", "で", "は", "ない" },
              new int[] { 0, 2, 3, 4, 5, 6 },
              new int[] { 2, 3, 4, 5, 6, 8 }
            );
        }

        /* Note this is really a stupid test just to see if things arent horribly slow.
         * ideally the test would actually fail instead of hanging...
         */
        [Test]
        public void TestDecomposition5()
        {
            TokenStream ts = analyzer.GetTokenStream("bogus", "くよくよくよくよくよくよくよくよくよくよくよくよくよくよくよくよくよくよくよくよ");
            try
            {
                ts.Reset();
                while (ts.IncrementToken())
                {

                }
                ts.End();
            }
            finally
            {
                IOUtils.DisposeWhileHandlingException(ts);
            }
        }

        /*
          // NOTE: intentionally fails!  Just trying to debug this
          // one input...
        public void testDecomposition6() throws Exception {
          assertAnalyzesTo(analyzer, "奈良先端科学技術大学院大学",
            new String[] { "これ", "は", "本", "で", "は", "ない" },
            new int[] { 0, 2, 3, 4, 5, 6 },
            new int[] { 2, 3, 4, 5, 6, 8 }
                           );
        }
        */

        /** Tests that sentence offset is incorporated into the resulting offsets */
        [Test]
        public void TestTwoSentences()
        {
            /*
            //TokenStream ts = a.tokenStream("foo", "妹の咲子です。俺と年子で、今受験生です。");
            TokenStream ts = analyzer.tokenStream("foo", "&#x250cdf66<!--\"<!--#<!--;?><!--#<!--#><!---->?>-->;");
            ts.reset();
            CharTermAttribute termAtt = ts.addAttribute(CharTermAttribute.class);
            while(ts.incrementToken()) {
              System.out.println("  " + termAtt.toString());
            }
            System.out.println("DONE PARSE\n\n");
            */

            AssertAnalyzesTo(analyzerNoPunct, "魔女狩大将マシュー・ホプキンス。 魔女狩大将マシュー・ホプキンス。",
              new String[] { "魔女", "狩", "大将", "マシュー", "ホプキンス", "魔女", "狩", "大将", "マシュー", "ホプキンス" },
              new int[] { 0, 2, 3, 5, 10, 17, 19, 20, 22, 27 },
              new int[] { 2, 3, 5, 9, 15, 19, 20, 22, 26, 32 }
            );
        }

        /** blast some random strings through the analyzer */
        [Test]
        public void TestRandomStrings()
        {
            CheckRandomData(Random, analyzer, 1000 * RandomMultiplier);
            CheckRandomData(Random, analyzerNoPunct, 1000 * RandomMultiplier);
        }

        /** blast some random large strings through the analyzer */
        [Test]
        [Slow]
        public void TestRandomHugeStrings()
        {
            Random random = Random;
            CheckRandomData(random, analyzer, 100 * RandomMultiplier, 8192);
            CheckRandomData(random, analyzerNoPunct, 100 * RandomMultiplier, 8192);
        }

        [Test]
        public void TestRandomHugeStringsMockGraphAfter()
        {
            // Randomly inject graph tokens after JapaneseTokenizer:
            Random random = Random;
            CheckRandomData(random,
                            Analyzer.NewAnonymous(createComponents: (fieldName, reader) =>
                            {
                                Tokenizer tokenizer = new JapaneseTokenizer(reader, ReadDict(), false, JapaneseTokenizerMode.SEARCH);
                                TokenStream graph = new MockGraphTokenFilter(Random, tokenizer);
                                return new TokenStreamComponents(tokenizer, graph);
                            }),
                    100 * RandomMultiplier, 8192);
        }

        [Test]
        public void TestLargeDocReliability()
        {
            for (int i = 0; i < 100; i++)
            {
                String s = TestUtil.RandomUnicodeString(Random, 10000);
                TokenStream ts = analyzer.GetTokenStream("foo", s);
                try
                {
                    ts.Reset();
                    while (ts.IncrementToken())
                    {
                    }
                    ts.End();
                }
                finally
                {
                    IOUtils.DisposeWhileHandlingException(ts);
                }
            }
        }

        /** simple test for supplementary characters */
        [Test]
        public void TestSurrogates()
        {
            AssertAnalyzesTo(analyzer, "𩬅艱鍟䇹愯瀛",
              new String[] { "𩬅", "艱", "鍟", "䇹", "愯", "瀛" });
        }

        /** random test ensuring we don't ever split supplementaries */
        [Test]
        public void TestSurrogates2()
        {
            int numIterations = AtLeast(10000);
            for (int i = 0; i < numIterations; i++)
            {
                if (Verbose)
                {
                    Console.WriteLine("\nTEST: iter=" + i);
                }
                String s = TestUtil.RandomUnicodeString(Random, 100);
                TokenStream ts = analyzer.GetTokenStream("foo", s);
                try
                {
                    ICharTermAttribute termAtt = ts.AddAttribute<ICharTermAttribute>();
                    ts.Reset();
                    while (ts.IncrementToken())
                    {
                        assertTrue(UnicodeUtil.ValidUTF16String(termAtt));
                    }
                    ts.End();
                }
                finally
                {
                    IOUtils.DisposeWhileHandlingException(ts);
                }
            }
        }

        [Test]
        public void TestOnlyPunctuation()
        {
            TokenStream ts = analyzerNoPunct.GetTokenStream("foo", "。、。。");
            try
            {
                ts.Reset();
                assertFalse(ts.IncrementToken());
                ts.End();
            }
            finally
            {
                IOUtils.DisposeWhileHandlingException(ts);
            }
        }

        [Test]
        public void TestOnlyPunctuationExtended()
        {
            TokenStream ts = extendedModeAnalyzerNoPunct.GetTokenStream("foo", "......");
            try
            {
                ts.Reset();
                assertFalse(ts.IncrementToken());
                ts.End();
            }
            finally
            {
                IOUtils.DisposeWhileHandlingException(ts);
            }
        }

        // note: test is kinda silly since kuromoji emits punctuation tokens.
        // but, when/if we filter these out it will be useful.
        [Test]
        public void TestEnd()
        {
            AssertTokenStreamContents(analyzerNoPunct.GetTokenStream("foo", "これは本ではない"),
                new String[] { "これ", "は", "本", "で", "は", "ない" },
                new int[] { 0, 2, 3, 4, 5, 6 },
                new int[] { 2, 3, 4, 5, 6, 8 },
                new int?(8)
            );

            AssertTokenStreamContents(analyzerNoPunct.GetTokenStream("foo", "これは本ではない    "),
                new String[] { "これ", "は", "本", "で", "は", "ない" },
                new int[] { 0, 2, 3, 4, 5, 6, 8 },
                new int[] { 2, 3, 4, 5, 6, 8, 9 },
                new int?(12)
            );
        }

        [Test]
        public void TestUserDict()
        {
            // Not a great test because w/o userdict.txt the
            // segmentation is the same:
            AssertTokenStreamContents(analyzer.GetTokenStream("foo", "関西国際空港に行った"),
                                      new String[] { "関西", "国際", "空港", "に", "行っ", "た" },
                                      new int[] { 0, 2, 4, 6, 7, 9 },
                                      new int[] { 2, 4, 6, 7, 9, 10 },
                                      new int?(10)
            );
        }

        [Test]
        public void TestUserDict2()
        {
            // Better test: w/o userdict the segmentation is different:
            AssertTokenStreamContents(analyzer.GetTokenStream("foo", "朝青龍"),
                                      new String[] { "朝青龍" },
                                      new int[] { 0 },
                                      new int[] { 3 },
                                      new int?(3)
            );
        }

        [Test]
        public void TestUserDict3()
        {
            // Test entry that breaks into multiple tokens:
            AssertTokenStreamContents(analyzer.GetTokenStream("foo", "abcd"),
                                      new String[] { "a", "b", "cd" },
                                      new int[] { 0, 1, 2 },
                                      new int[] { 1, 2, 4 },
                                      new int?(4)
            );
        }

        // HMM: fails (segments as a/b/cd/efghij)... because the
        // two paths have exactly equal paths (1 KNOWN + 1
        // UNKNOWN) and we don't seem to favor longer KNOWN /
        // shorter UNKNOWN matches:

        /*
        public void testUserDict4()  {
          // Test entry that has another entry as prefix
          assertTokenStreamContents(analyzer.tokenStream("foo", "abcdefghij"),
                                    new String[] { "ab", "cd", "efg", "hij"  },
                                    new int[] { 0, 2, 4, 7 },
                                    new int[] { 2, 4, 7, 10 },
                                    new int?(10)
          );
        }
        */

        [Test]
        public void TestSegmentation()
        {
            // Skip tests for Michelle Kwan -- UniDic segments Kwan as ク ワン
            //   String input = "ミシェル・クワンが優勝しました。スペースステーションに行きます。うたがわしい。";
            //   String[] surfaceForms = {
            //        "ミシェル", "・", "クワン", "が", "優勝", "し", "まし", "た", "。",
            //        "スペース", "ステーション", "に", "行き", "ます", "。",
            //        "うたがわしい", "。"
            //   };
            String input = "スペースステーションに行きます。うたがわしい。";
            String[]
            surfaceForms = {
                "スペース", "ステーション", "に", "行き", "ます", "。",
                "うたがわしい", "。"
            };
            AssertAnalyzesTo(analyzer,
                             input,
                             surfaceForms);
        }

        [Test]
        public void TestLatticeToDot()
        {
            GraphvizFormatter gv2 = new GraphvizFormatter(ConnectionCosts.Instance);
            Analyzer analyzer = Analyzer.NewAnonymous(createComponents: (fieldName, reader) =>
            {
                JapaneseTokenizer tokenizer = new JapaneseTokenizer(reader, ReadDict(), false, JapaneseTokenizerMode.SEARCH)
                {
                    GraphvizFormatter = gv2
                };
                return new TokenStreamComponents(tokenizer, tokenizer);
            });


            String input = "スペースステーションに行きます。うたがわしい。";
            String[] surfaceForms = {
                "スペース", "ステーション", "に", "行き", "ます", "。",
                "うたがわしい", "。"
            };
            AssertAnalyzesTo(analyzer,
                             input,
                             surfaceForms);


            assertTrue(gv2.Finish().IndexOf("22.0", StringComparison.Ordinal) != -1);
        }

        private void assertReadings(String input, params String[] readings)
        {
            TokenStream ts = analyzer.GetTokenStream("ignored", input);
            try
            {
                IReadingAttribute readingAtt = ts.AddAttribute<IReadingAttribute>();
                ts.Reset();
                foreach (String reading in readings)
                {
                    assertTrue(ts.IncrementToken());
                    assertEquals(reading, readingAtt.GetReading());
                }
                assertFalse(ts.IncrementToken());
                ts.End();
            }
            finally
            {
                IOUtils.DisposeWhileHandlingException(ts);
            }
        }

        private void assertPronunciations(String input, params String[] pronunciations)
        {
            TokenStream ts = analyzer.GetTokenStream("ignored", input);
            try
            {
                IReadingAttribute readingAtt = ts.AddAttribute<IReadingAttribute>();
                ts.Reset();
                foreach (String pronunciation in pronunciations)
                {
                    assertTrue(ts.IncrementToken());
                    assertEquals(pronunciation, readingAtt.GetPronunciation());
                }
                assertFalse(ts.IncrementToken());
                ts.End();
            }
            finally
            {
                IOUtils.DisposeWhileHandlingException(ts);
            }
        }

        private void assertBaseForms(String input, params String[] baseForms)
        {
            TokenStream ts = analyzer.GetTokenStream("ignored", input);
            try
            {
                IBaseFormAttribute baseFormAtt = ts.AddAttribute<IBaseFormAttribute>();
                ts.Reset();
                foreach (String baseForm in baseForms)
                {
                    assertTrue(ts.IncrementToken());
                    assertEquals(baseForm, baseFormAtt.GetBaseForm());
                }
                assertFalse(ts.IncrementToken());
                ts.End();
            }
            finally
            {
                IOUtils.DisposeWhileHandlingException(ts);
            }
        }

        private void assertInflectionTypes(String input, params String[] inflectionTypes)
        {
            TokenStream ts = analyzer.GetTokenStream("ignored", input);
            try
            {
                IInflectionAttribute inflectionAtt = ts.AddAttribute<IInflectionAttribute>();
                ts.Reset();
                foreach (String inflectionType in inflectionTypes)
                {
                    assertTrue(ts.IncrementToken());
                    assertEquals(inflectionType, inflectionAtt.GetInflectionType());
                }
                assertFalse(ts.IncrementToken());
                ts.End();
            }
            finally
            {
                IOUtils.DisposeWhileHandlingException(ts);
            }
        }

        private void assertInflectionForms(String input, params String[] inflectionForms)
        {
            TokenStream ts = analyzer.GetTokenStream("ignored", input);
            try
            {
                IInflectionAttribute inflectionAtt = ts.AddAttribute<IInflectionAttribute>();
                ts.Reset();
                foreach (String inflectionForm in inflectionForms)
                {
                    assertTrue(ts.IncrementToken());
                    assertEquals(inflectionForm, inflectionAtt.GetInflectionForm());
                }
                assertFalse(ts.IncrementToken());
                ts.End();
            }
            finally
            {
                IOUtils.DisposeWhileHandlingException(ts);
            }
        }

        private void assertPartsOfSpeech(String input, params String[] partsOfSpeech)
        {
            TokenStream ts = analyzer.GetTokenStream("ignored", input);
            try
            {
                IPartOfSpeechAttribute partOfSpeechAtt = ts.AddAttribute<IPartOfSpeechAttribute>();
                ts.Reset();
                foreach (String partOfSpeech in partsOfSpeech)
                {
                    assertTrue(ts.IncrementToken());
                    assertEquals(partOfSpeech, partOfSpeechAtt.GetPartOfSpeech());
                }
                assertFalse(ts.IncrementToken());
                ts.End();
            }
            finally
            {
                IOUtils.DisposeWhileHandlingException(ts);
            }
        }

        [Test]
        public void TestReadings()
        {
            assertReadings("寿司が食べたいです。",
                           "スシ",
                           "ガ",
                           "タベ",
                           "タイ",
                           "デス",
                           "。");
        }

        [Test]
        public void TestReadings2()
        {
            assertReadings("多くの学生が試験に落ちた。",
                           "オオク",
                           "ノ",
                           "ガクセイ",
                           "ガ",
                           "シケン",
                           "ニ",
                           "オチ",
                           "タ",
                           "。");
        }

        [Test]
        public void TestPronunciations()
        {
            assertPronunciations("寿司が食べたいです。",
                                 "スシ",
                                 "ガ",
                                 "タベ",
                                 "タイ",
                                 "デス",
                                 "。");
        }

        [Test]
        public void TestPronunciations2()
        {
            // pronunciation differs from reading here
            assertPronunciations("多くの学生が試験に落ちた。",
                                 "オーク",
                                 "ノ",
                                 "ガクセイ",
                                 "ガ",
                                 "シケン",
                                 "ニ",
                                 "オチ",
                                 "タ",
                                 "。");
        }

        [Test]
        public void TestBasicForms()
        {
            assertBaseForms("それはまだ実験段階にあります。",
                            null,
                            null,
                            null,
                            null,
                            null,
                            null,
                            "ある",
                            null,
                            null);
        }

        [Test]
        public void TestInflectionTypes()
        {
            assertInflectionTypes("それはまだ実験段階にあります。",
                                  null,
                                  null,
                                  null,
                                  null,
                                  null,
                                  null,
                                  "五段・ラ行",
                                  "特殊・マス",
                                  null);
        }

        [Test]
        public void TestInflectionForms()
        {
            assertInflectionForms("それはまだ実験段階にあります。",
                                  null,
                                  null,
                                  null,
                                  null,
                                  null,
                                  null,
                                  "連用形",
                                  "基本形",
                                  null);
        }

        [Test]
        public void TestPartOfSpeech()
        {
            assertPartsOfSpeech("それはまだ実験段階にあります。",
                                "名詞-代名詞-一般",
                                "助詞-係助詞",
                                "副詞-助詞類接続",
                                "名詞-サ変接続",
                                "名詞-一般",
                                "助詞-格助詞-一般",
                                "動詞-自立",
                                "助動詞",
                                "記号-句点");
        }

        // TODO: the next 2 tests are no longer using the first/last word ids, maybe lookup the words and fix?
        // do we have a possibility to actually lookup the first and last word from dictionary?
        [Test]
        public void TestYabottai()
        {
            AssertAnalyzesTo(analyzer, "やぼったい",
                             new String[] { "やぼったい" });
        }

        [Test]
        public void TestTsukitosha()
        {
            AssertAnalyzesTo(analyzer, "突き通しゃ",
                             new String[] { "突き通しゃ" });
        }

        [Test]
        public void TestBocchan()
        {
            doTestBocchan(1);
        }

        [Test]
        [Nightly]
        public void TestBocchanBig()
        {
            doTestBocchan(100);
        }

        /*
        public void testWikipedia()  {
          final FileInputStream fis = new FileInputStream("/q/lucene/jawiki-20120220-pages-articles.xml");
          final Reader r = new BufferedReader(new InputStreamReader(fis, StandardCharsets.UTF_8));

          final long startTimeNS = Time.NanoTime();
          boolean done = false;
          long compoundCount = 0;
          long nonCompoundCount = 0;
          long netOffset = 0;
          while (!done) {
            final TokenStream ts = analyzer.tokenStream("ignored", r);
            ts.reset();
            final PositionIncrementAttribute posIncAtt = ts.addAttribute(PositionIncrementAttribute.class);
            final OffsetAttribute offsetAtt = ts.addAttribute(OffsetAttribute.class);
            int count = 0;
            while (true) {
              if (!ts.incrementToken()) {
                done = true;
                break;
              }
              count++;
              if (posIncAtt.getPositionIncrement() == 0) {
                compoundCount++;
              } else {
                nonCompoundCount++;
                if (nonCompoundCount % 1000000 == 0) {
                  System.out.println(String.format("%.2f msec [pos=%d, %d, %d]",
                                                   (Time.NanoTime()-startTimeNS)/1000000.0,
                                                   netOffset + offsetAtt.startOffset(),
                                                   nonCompoundCount,
                                                   compoundCount));
                }
              }
              if (count == 100000000) {
                System.out.println("  again...");
                break;
              }
            }
            ts.end();
            netOffset += offsetAtt.endOffset();
          }
          System.out.println("compoundCount=" + compoundCount + " nonCompoundCount=" + nonCompoundCount);
          r.close();
        }
        */


        private void doTestBocchan(int numIterations)
        {
            TextReader reader = new StreamReader(
                this.GetType().getResourceAsStream("bocchan.utf-8"), Encoding.UTF8);
            String line = reader.ReadLine();
            reader.Dispose();

            if (Verbose)
            {
                Console.WriteLine("Test for Bocchan without pre-splitting sentences");
            }

            /*
            if (numIterations > 1) {
              // warmup
              for (int i = 0; i < numIterations; i++) {
                final TokenStream ts = analyzer.tokenStream("ignored", line);
                ts.reset();
                while(ts.incrementToken());
              }
            }
            */

            long totalStart = Time.NanoTime() / Time.MillisecondsPerNanosecond; // LUCENENET: Use NanoTime() rather than CurrentTimeMilliseconds() for more accurate/reliable results
            for (int i = 0; i < numIterations; i++)
            {
                TokenStream ts = analyzer.GetTokenStream("ignored", line);
                try
                {
                    ts.Reset();
                    while (ts.IncrementToken()) ;
                    ts.End();
                }
                finally
                {
                    IOUtils.DisposeWhileHandlingException(ts);
                }
            }
            String[] sentences = Regex.Split(line, "、|。").TrimEnd();
            if (Verbose)
            {
                Console.WriteLine("Total time : " + ((Time.NanoTime() / Time.MillisecondsPerNanosecond) - totalStart)); // LUCENENET: Use NanoTime() rather than CurrentTimeMilliseconds() for more accurate/reliable results
                Console.WriteLine("Test for Bocchan with pre-splitting sentences (" + sentences.Length + " sentences)");
            }
            totalStart = Time.NanoTime() / Time.MillisecondsPerNanosecond; // LUCENENET: Use NanoTime() rather than CurrentTimeMilliseconds() for more accurate/reliable results
            for (int i = 0; i < numIterations; i++)
            {
                foreach (String sentence in sentences)
                {
                    TokenStream ts = analyzer.GetTokenStream("ignored", sentence);
                    try
                    {
                        ts.Reset();
                        while (ts.IncrementToken()) ;
                        ts.End();
                    }
                    finally
                    {
                        IOUtils.DisposeWhileHandlingException(ts);
                    }
                }
            }
            if (Verbose)
            {
                Console.WriteLine("Total time : " + ((Time.NanoTime() / Time.MillisecondsPerNanosecond) - totalStart)); // LUCENENET: Use NanoTime() rather than CurrentTimeMilliseconds() for more accurate/reliable results
            }
        }

        [Test]
        public void TestWithPunctuation()
        {
            AssertAnalyzesTo(analyzerNoPunct, "羽田。空港",
                             new String[] { "羽田", "空港" },
                             new int[] { 1, 1 });
        }

        [Test]
        public void TestCompoundOverPunctuation()
        {
            AssertAnalyzesToPositions(analyzerNoPunct, "dεε϶ϢϏΎϷΞͺ羽田",
                                      new String[] { "d", "ε", "ε", "ϢϏΎϷΞͺ", "羽田" },
                                      new int[] { 1, 1, 1, 1, 1 },
                                      new int[] { 1, 1, 1, 1, 1 });
        }

        // LUCENENET: ported from LUCENE-10059
        // Note that these are only the changes from https://github.com/apache/lucene/pull/254.
        // The NBest feature doesn't yet exist in Lucene 4.8.0, so the changes from
        // https://github.com/apache/lucene/pull/284 will need to be added here when that feature is ported.
        [Test]
        public void TestEmptyBacktrace()
        {
            String text = "";

            // since the max backtrace gap ({@link JapaneseTokenizer#MAX_BACKTRACE_GAP)
            // is set to 1024, we want the first 1023 characters to generate multiple paths
            // so that the regular backtrace is not executed.
            for (int i = 0; i < 1023; i++)
            {
                text += "あ";
            }

            // and the last 2 characters to be a valid word so that they
            // will end-up together
            text += "手紙";

            IList<String> outputs = new List<String>();
            for (int i = 0; i < 511; i++)
            {
                outputs.Add("ああ");
            }
            outputs.Add("あ");
            outputs.Add("手紙");

            AssertAnalyzesTo(analyzer, text, outputs.ToArray());
        }
    }
}
