using NUnit.Framework;
using System;
using System.IO;
using System.Text;

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

    public class TestJapaneseIterationMarkCharFilter : BaseTokenStreamTestCase
    {
        private Analyzer keywordAnalyzer = Analyzer.NewAnonymous(createComponents: (fieldName, reader) =>
        {
            Tokenizer tokenizer = new MockTokenizer(reader, MockTokenizer.KEYWORD, false);
            return new TokenStreamComponents(tokenizer, tokenizer);
        },
            initReader: (fieldName, reader) =>
            {
                return new JapaneseIterationMarkCharFilter(reader);
            });


        private Analyzer japaneseAnalyzer = Analyzer.NewAnonymous(createComponents: (fieldName, reader) =>
        {
            Tokenizer tokenizer = new JapaneseTokenizer(reader, null, false, JapaneseTokenizerMode.SEARCH);
            return new TokenStreamComponents(tokenizer, tokenizer);
        },
            initReader: (fieldName, reader) =>
            {
                return new JapaneseIterationMarkCharFilter(reader);
            });

        [Test]
        public void TestKanji()
        {
            // Test single repetition
            AssertAnalyzesTo(keywordAnalyzer, "時々", new String[] { "時時" });
            AssertAnalyzesTo(japaneseAnalyzer, "時々", new String[] { "時時" });

            // Test multiple repetitions
            AssertAnalyzesTo(keywordAnalyzer, "馬鹿々々しい", new String[] { "馬鹿馬鹿しい" });
            AssertAnalyzesTo(japaneseAnalyzer, "馬鹿々々しい", new String[] { "馬鹿馬鹿しい" });
        }

        [Test]
        public void TestKatakana()
        {
            // Test single repetition
            AssertAnalyzesTo(keywordAnalyzer, "ミスヾ", new String[] { "ミスズ" });
            AssertAnalyzesTo(japaneseAnalyzer, "ミスヾ", new String[] { "ミ", "スズ" }); // Side effect
        }

        [Test]
        public void testHiragana()
        {
            // Test single unvoiced iteration
            AssertAnalyzesTo(keywordAnalyzer, "おゝの", new String[] { "おおの" });
            AssertAnalyzesTo(japaneseAnalyzer, "おゝの", new String[] { "お", "おの" }); // Side effect

            // Test single voiced iteration
            AssertAnalyzesTo(keywordAnalyzer, "みすゞ", new String[] { "みすず" });
            AssertAnalyzesTo(japaneseAnalyzer, "みすゞ", new String[] { "みすず" });

            // Test single voiced iteration
            AssertAnalyzesTo(keywordAnalyzer, "じゞ", new String[] { "じじ" });
            AssertAnalyzesTo(japaneseAnalyzer, "じゞ", new String[] { "じじ" });

            // Test single unvoiced iteration with voiced iteration
            AssertAnalyzesTo(keywordAnalyzer, "じゝ", new String[] { "じし" });
            AssertAnalyzesTo(japaneseAnalyzer, "じゝ", new String[] { "じし" });

            // Test multiple repetitions with voiced iteration
            AssertAnalyzesTo(keywordAnalyzer, "ところゞゝゝ", new String[] { "ところどころ" });
            AssertAnalyzesTo(japaneseAnalyzer, "ところゞゝゝ", new String[] { "ところどころ" });
        }

        [Test]
        public void TestMalformed()
        {
            // We can't iterate c here, so emit as it is
            AssertAnalyzesTo(keywordAnalyzer, "abcところゝゝゝゝ", new String[] { "abcところcところ" });

            // We can't iterate c (with dakuten change) here, so emit it as-is
            AssertAnalyzesTo(keywordAnalyzer, "abcところゞゝゝゝ", new String[] { "abcところcところ" });

            // We can't iterate before beginning of stream, so emit characters as-is
            AssertAnalyzesTo(keywordAnalyzer, "ところゞゝゝゞゝゞ", new String[] { "ところどころゞゝゞ" });

            // We can't iterate an iteration mark only, so emit as-is
            AssertAnalyzesTo(keywordAnalyzer, "々", new String[] { "々" });
            AssertAnalyzesTo(keywordAnalyzer, "ゞ", new String[] { "ゞ" });
            AssertAnalyzesTo(keywordAnalyzer, "ゞゝ", new String[] { "ゞゝ" });

            // We can't iterate a full stop punctuation mark (because we use it as a flush marker)
            AssertAnalyzesTo(keywordAnalyzer, "。ゝ", new String[] { "。ゝ" });
            AssertAnalyzesTo(keywordAnalyzer, "。。ゝゝ", new String[] { "。。ゝゝ" });

            // We can iterate other punctuation marks
            AssertAnalyzesTo(keywordAnalyzer, "？ゝ", new String[] { "？？" });

            // We can not get a dakuten variant of ぽ -- this is also a corner case test for inside()
            AssertAnalyzesTo(keywordAnalyzer, "ねやぽゞつむぴ", new String[] { "ねやぽぽつむぴ" });
            AssertAnalyzesTo(keywordAnalyzer, "ねやぽゝつむぴ", new String[] { "ねやぽぽつむぴ" });
        }

        [Test]
        public void TestEmpty()
        {
            // Empty input stays empty
            AssertAnalyzesTo(keywordAnalyzer, "", new String[0]);
            AssertAnalyzesTo(japaneseAnalyzer, "", new String[0]);
        }

        [Test]
        public void TestFullStop()
        {
            // Test full stops   
            AssertAnalyzesTo(keywordAnalyzer, "。", new String[] { "。" });
            AssertAnalyzesTo(keywordAnalyzer, "。。", new String[] { "。。" });
            AssertAnalyzesTo(keywordAnalyzer, "。。。", new String[] { "。。。" });
        }

        [Test]
        public void TestKanjiOnly()
        {
            // Test kanji only repetition marks
            CharFilter filter = new JapaneseIterationMarkCharFilter(
                new StringReader("時々、おゝのさんと一緒にお寿司が食べたいです。abcところゞゝゝ。"),
                true, // kanji
                false // no kana
            );
            assertCharFilterEquals(filter, "時時、おゝのさんと一緒にお寿司が食べたいです。abcところゞゝゝ。");
        }

        [Test]
        public void TestKanaOnly()
        {
            // Test kana only repetition marks
            CharFilter filter = new JapaneseIterationMarkCharFilter(
                new StringReader("時々、おゝのさんと一緒にお寿司が食べたいです。abcところゞゝゝ。"),
                false, // no kanji
                true   // kana
            );
            assertCharFilterEquals(filter, "時々、おおのさんと一緒にお寿司が食べたいです。abcところどころ。");
        }

        [Test]
        public void TestNone()
        {
            // Test no repetition marks
            CharFilter filter = new JapaneseIterationMarkCharFilter(
                new StringReader("時々、おゝのさんと一緒にお寿司が食べたいです。abcところゞゝゝ。"),
                false, // no kanji
                false  // no kana
            );
            assertCharFilterEquals(filter, "時々、おゝのさんと一緒にお寿司が食べたいです。abcところゞゝゝ。");
        }

        [Test]
        public void TestCombinations()
        {
            AssertAnalyzesTo(keywordAnalyzer, "時々、おゝのさんと一緒にお寿司を食べに行きます。",
                new String[] { "時時、おおのさんと一緒にお寿司を食べに行きます。" }
            );
        }

        [Test]
        public void TestHiraganaCoverage()
        {
            // Test all hiragana iteration variants
            String source = "かゝがゝきゝぎゝくゝぐゝけゝげゝこゝごゝさゝざゝしゝじゝすゝずゝせゝぜゝそゝぞゝたゝだゝちゝぢゝつゝづゝてゝでゝとゝどゝはゝばゝひゝびゝふゝぶゝへゝべゝほゝぼゝ";
            String target = "かかがかききぎきくくぐくけけげけここごこささざさししじしすすずすせせぜせそそぞそたただたちちぢちつつづつててでてととどとははばはひひびひふふぶふへへべへほほぼほ";
            AssertAnalyzesTo(keywordAnalyzer, source, new String[] { target });

            // Test all hiragana iteration variants with dakuten
            source = "かゞがゞきゞぎゞくゞぐゞけゞげゞこゞごゞさゞざゞしゞじゞすゞずゞせゞぜゞそゞぞゞたゞだゞちゞぢゞつゞづゞてゞでゞとゞどゞはゞばゞひゞびゞふゞぶゞへゞべゞほゞぼゞ";
            target = "かがががきぎぎぎくぐぐぐけげげげこごごごさざざざしじじじすずずずせぜぜぜそぞぞぞただだだちぢぢぢつづづづてでででとどどどはばばばひびびびふぶぶぶへべべべほぼぼぼ";
            AssertAnalyzesTo(keywordAnalyzer, source, new String[] { target });
        }

        [Test]
        public void TestKatakanaCoverage()
        {
            // Test all katakana iteration variants
            String source = "カヽガヽキヽギヽクヽグヽケヽゲヽコヽゴヽサヽザヽシヽジヽスヽズヽセヽゼヽソヽゾヽタヽダヽチヽヂヽツヽヅヽテヽデヽトヽドヽハヽバヽヒヽビヽフヽブヽヘヽベヽホヽボヽ";
            String target = "カカガカキキギキククグクケケゲケココゴコササザサシシジシススズスセセゼセソソゾソタタダタチチヂチツツヅツテテデテトトドトハハバハヒヒビヒフフブフヘヘベヘホホボホ";
            AssertAnalyzesTo(keywordAnalyzer, source, new String[] { target });

            // Test all katakana iteration variants with dakuten
            source = "カヾガヾキヾギヾクヾグヾケヾゲヾコヾゴヾサヾザヾシヾジヾスヾズヾセヾゼヾソヾゾヾタヾダヾチヾヂヾツヾヅヾテヾデヾトヾドヾハヾバヾヒヾビヾフヾブヾヘヾベヾホヾボヾ";
            target = "カガガガキギギギクグググケゲゲゲコゴゴゴサザザザシジジジスズズズセゼゼゼソゾゾゾタダダダチヂヂヂツヅヅヅテデデデトドドドハバババヒビビビフブブブヘベベベホボボボ";
            AssertAnalyzesTo(keywordAnalyzer, source, new String[] { target });
        }

        [Test]
        public void TestRandomStrings()
        {
            // Blast some random strings through
            CheckRandomData(Random, keywordAnalyzer, 1000 * RandomMultiplier);
        }

        [Test]
        public void TestRandomHugeStrings()
        {
            // Blast some random strings through
            CheckRandomData(Random, keywordAnalyzer, 100 * RandomMultiplier, 8192);
        }

        private void assertCharFilterEquals(CharFilter filter, String expected)
        {
            String actual = readFully(filter);
            assertEquals(expected, actual);
        }

        private String readFully(TextReader stream)
        {
            StringBuilder buffer = new StringBuilder();
            int ch;
            while ((ch = stream.Read()) != -1)
            {
                buffer.append((char)ch);
            }
            return buffer.toString();
        }
    }
}
