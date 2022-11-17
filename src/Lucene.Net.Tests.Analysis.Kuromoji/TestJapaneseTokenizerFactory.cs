using Lucene.Net.Support;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;

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

    /// <summary>
    /// Simple tests for <see cref="JapaneseTokenizerFactory"/>
    /// </summary>
    public class TestJapaneseTokenizerFactory : BaseTokenStreamTestCase
    {
        [Test]
        public void TestSimple()
        {
            JapaneseTokenizerFactory factory = new JapaneseTokenizerFactory(new Dictionary<String, String>());
            factory.Inform(new StringMockResourceLoader(""));
            TokenStream ts = factory.Create(new StringReader("これは本ではない"));
            AssertTokenStreamContents(ts,
                new String[] { "これ", "は", "本", "で", "は", "ない" },
                new int[] { 0, 2, 3, 4, 5, 6 },
                new int[] { 2, 3, 4, 5, 6, 8 }
            );
        }

        /**
         * Test that search mode is enabled and working by default
         */
        [Test]
        public void TestDefaults()
        {
            JapaneseTokenizerFactory factory = new JapaneseTokenizerFactory(new Dictionary<String, String>());
            factory.Inform(new StringMockResourceLoader(""));
            TokenStream ts = factory.Create(new StringReader("シニアソフトウェアエンジニア"));
            AssertTokenStreamContents(ts,
                new String[] { "シニア", "シニアソフトウェアエンジニア", "ソフトウェア", "エンジニア" }
            );
        }

        /**
         * Test mode parameter: specifying normal mode
         */
        [Test]
        public void TestMode()
        {
            IDictionary<String, String> args = new Dictionary<String, String>();
            args["mode"] = "normal";
            JapaneseTokenizerFactory factory = new JapaneseTokenizerFactory(args);
            factory.Inform(new StringMockResourceLoader(""));
            TokenStream ts = factory.Create(new StringReader("シニアソフトウェアエンジニア"));
            AssertTokenStreamContents(ts,
                new String[] { "シニアソフトウェアエンジニア" }
            );
        }

        /**
         * Test user dictionary
         */
        [Test]
        public void TestUserDict()
        {
            String userDict =
                "# Custom segmentation for long entries\n" +
                "日本経済新聞,日本 経済 新聞,ニホン ケイザイ シンブン,カスタム名詞\n" +
                "関西国際空港,関西 国際 空港,カンサイ コクサイ クウコウ,テスト名詞\n" +
                "# Custom reading for sumo wrestler\n" +
                "朝青龍,朝青龍,アサショウリュウ,カスタム人名\n";
            IDictionary<String, String> args = new Dictionary<String, String>();
            args["userDictionary"] = "userdict.txt";
            JapaneseTokenizerFactory factory = new JapaneseTokenizerFactory(args);
            factory.Inform(new StringMockResourceLoader(userDict));
            TokenStream ts = factory.Create(new StringReader("関西国際空港に行った"));
            AssertTokenStreamContents(ts,
                new String[] { "関西", "国際", "空港", "に", "行っ", "た" }
            );
        }

        /**
         * Test preserving punctuation
         */
        [Test]
        public void TestPreservePunctuation()
        {
            IDictionary<String, String> args = new Dictionary<String, String>();
            args["discardPunctuation"] = "false";
            JapaneseTokenizerFactory factory = new JapaneseTokenizerFactory(args);
            factory.Inform(new StringMockResourceLoader(""));
            TokenStream ts = factory.Create(
                new StringReader("今ノルウェーにいますが、来週の頭日本に戻ります。楽しみにしています！お寿司が食べたいな。。。")
            );
            AssertTokenStreamContents(ts,
                new String[] { "今", "ノルウェー", "に", "い", "ます", "が", "、",
                    "来週", "の", "頭", "日本", "に", "戻り", "ます", "。",
                    "楽しみ", "に", "し", "て", "い", "ます", "！",
                    "お", "寿司", "が", "食べ", "たい", "な", "。", "。", "。" }
            );
        }

        /** Test that bogus arguments result in exception */
        [Test]
        public void TestBogusArguments()
        {
            try
            {
                new JapaneseTokenizerFactory(new Dictionary<String, String>() {
                    { "bogusArg", "bogusValue" }
                });
                fail();
            }
            catch (Exception expected) when (expected.IsIllegalArgumentException())
            {
                assertTrue(expected.Message.Contains("Unknown parameters"));
            }
        }
    }
}
