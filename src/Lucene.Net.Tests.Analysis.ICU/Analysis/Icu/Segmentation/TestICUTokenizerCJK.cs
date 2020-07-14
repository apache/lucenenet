// Lucene version compatibility level < 7.1.0
using NUnit.Framework;
using System;

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
    /// test ICUTokenizer with dictionary-based CJ segmentation
    /// </summary>
    public class TestICUTokenizerCJK : BaseTokenStreamTestCase
    {
        Analyzer a;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            a = Analyzer.NewAnonymous(createComponents: (fieldName, reader) =>
            {
                return new TokenStreamComponents(new ICUTokenizer(reader));
            });
        }

        [TearDown]
        public override void TearDown()
        {
            if (a != null) a.Dispose();
            base.TearDown();
        }

        /**
         * test stolen from smartcn
         */
        [Test]
        public void TestSimpleChinese()
        {
            AssertAnalyzesTo(a, "我购买了道具和服装。",
                new string[] { "我", "购买", "了", "道具", "和", "服装" }
            );
        }

        [Test]
        public void TestTraditionalChinese() 
        {
            AssertAnalyzesTo(a, "我購買了道具和服裝。",
                new string[] { "我", "購買", "了", "道具", "和", "服裝"});
            AssertAnalyzesTo(a, "定義切分字串的基本單位是訂定分詞標準的首要工作", // From http://godel.iis.sinica.edu.tw/CKIP/paper/wordsegment_standard.pdf
                new string[] { "定義", "切", "分", "字串", "的", "基本", "單位", "是", "訂定", "分詞", "標準", "的", "首要", "工作" });
        }

        [Test]
        public void TestChineseNumerics()
        {
            AssertAnalyzesTo(a, "９４８３", new string[] { "９４８３" });
            AssertAnalyzesTo(a, "院內分機９４８３。",
                new string[] { "院", "內", "分機", "９４８３" });
            AssertAnalyzesTo(a, "院內分機9483。",
                new string[] { "院", "內", "分機", "9483" });
        }

        /**
         * test stolen from kuromoji
         */
        [Test]
        public void TestSimpleJapanese()
        {
            AssertAnalyzesTo(a, "それはまだ実験段階にあります",
                new string[] { "それ", "は", "まだ", "実験", "段階", "に", "あり", "ます" }
            );
        }

        [Test]
        public void TestJapaneseTypes()
        {
            AssertAnalyzesTo(a, "仮名遣い カタカナ",
                new string[] { "仮名遣い", "カタカナ" },
                new string[] { "<IDEOGRAPHIC>", "<IDEOGRAPHIC>" });
            }

        [Test]
        public void TestKorean()
        {
            // Korean words
            AssertAnalyzesTo(a, "안녕하세요 한글입니다", new string[] { "안녕하세요", "한글입니다" });
        }

        /** make sure that we still tag korean as HANGUL (for further decomposition/ngram/whatever) */
        [Test]
        public void TestKoreanTypes()
        {
            AssertAnalyzesTo(a, "훈민정음",
                new string[] { "훈민정음" },
                new string[] { "<HANGUL>" });
        }

        /** blast some random strings through the analyzer */
        [Test]
        public void TestRandomStrings()
        {
            CheckRandomData(Random, a, 10000 * RandomMultiplier);
        }

        /** blast some random large strings through the analyzer */
        [Test]
        public void TestRandomHugeStrings()
        {
            Random random = Random;
            CheckRandomData(random, a, 100 * RandomMultiplier, 8192);
        }
    }
}