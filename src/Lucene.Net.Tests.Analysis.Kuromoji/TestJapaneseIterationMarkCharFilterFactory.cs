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
    /// Simple tests for <see cref="JapaneseIterationMarkCharFilterFactory"/>
    /// </summary>
    public class TestJapaneseIterationMarkCharFilterFactory : BaseTokenStreamTestCase
    {
        [Test]
        public void TestIterationMarksWithKeywordTokenizer()
        {
            String text = "時々馬鹿々々しいところゞゝゝミスヾ";
            JapaneseIterationMarkCharFilterFactory filterFactory = new JapaneseIterationMarkCharFilterFactory(new Dictionary<String, String>());
            TextReader filter = filterFactory.Create(new StringReader(text));
            TokenStream tokenStream = new MockTokenizer(filter, MockTokenizer.KEYWORD, false);
            AssertTokenStreamContents(tokenStream, new String[] { "時時馬鹿馬鹿しいところどころミスズ" });
        }

        [Test]
        public void TestIterationMarksWithJapaneseTokenizer()
        {
            JapaneseTokenizerFactory tokenizerFactory = new JapaneseTokenizerFactory(new Dictionary<String, String>());
            tokenizerFactory.Inform(new StringMockResourceLoader(""));

            JapaneseIterationMarkCharFilterFactory filterFactory = new JapaneseIterationMarkCharFilterFactory(new Dictionary<String, String>());
            TextReader filter = filterFactory.Create(
                new StringReader("時々馬鹿々々しいところゞゝゝミスヾ")
            );
            TokenStream tokenStream = tokenizerFactory.Create(filter);
            AssertTokenStreamContents(tokenStream, new String[] { "時時", "馬鹿馬鹿しい", "ところどころ", "ミ", "スズ" });
        }

        [Test]
        public void TestKanjiOnlyIterationMarksWithJapaneseTokenizer()
        {
            JapaneseTokenizerFactory tokenizerFactory = new JapaneseTokenizerFactory(new Dictionary<String, String>());
            tokenizerFactory.Inform(new StringMockResourceLoader(""));

            IDictionary<String, String> filterArgs = new Dictionary<String, String>();
            filterArgs["normalizeKanji"] = "true";
            filterArgs["normalizeKana"] = "false";
            JapaneseIterationMarkCharFilterFactory filterFactory = new JapaneseIterationMarkCharFilterFactory(filterArgs);

            TextReader filter = filterFactory.Create(
                new StringReader("時々馬鹿々々しいところゞゝゝミスヾ")
            );
            TokenStream tokenStream = tokenizerFactory.Create(filter);
            AssertTokenStreamContents(tokenStream, new String[] { "時時", "馬鹿馬鹿しい", "ところ", "ゞ", "ゝ", "ゝ", "ミス", "ヾ" });
        }

        [Test]
        public void TestKanaOnlyIterationMarksWithJapaneseTokenizer()
        {
            JapaneseTokenizerFactory tokenizerFactory = new JapaneseTokenizerFactory(new Dictionary<String, String>());
            tokenizerFactory.Inform(new StringMockResourceLoader(""));

            IDictionary<String, String> filterArgs = new Dictionary<String, String>();
            filterArgs["normalizeKanji"] = "false";
            filterArgs["normalizeKana"] = "true";
            JapaneseIterationMarkCharFilterFactory filterFactory = new JapaneseIterationMarkCharFilterFactory(filterArgs);

            TextReader filter = filterFactory.Create(
                new StringReader("時々馬鹿々々しいところゞゝゝミスヾ")
            );
            TokenStream tokenStream = tokenizerFactory.Create(filter);
            AssertTokenStreamContents(tokenStream, new String[] { "時々", "馬鹿", "々", "々", "しい", "ところどころ", "ミ", "スズ" });
        }

        /** Test that bogus arguments result in exception */
        [Test]
        public void TestBogusArguments()
        {
            try
            {
                new JapaneseIterationMarkCharFilterFactory(new Dictionary<String, String>() {
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
