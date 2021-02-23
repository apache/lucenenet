// Lucene version compatibility level 4.8.1
using System;
using System.IO;
using NUnit.Framework;
using Lucene.Net.Analysis.Core;
using Lucene.Net.Util;
using Lucene.Net.Analysis.TokenAttributes;

namespace Lucene.Net.Analysis.Cn
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

    /// @deprecated Remove this test when ChineseAnalyzer is removed. 
    [Obsolete("Remove this test when ChineseAnalyzer is removed.")]
    public class TestChineseTokenizer : BaseTokenStreamTestCase
    {
        [Test]
        public virtual void TestOtherLetterOffset()
        {
            string s = "a天b";
            ChineseTokenizer tokenizer = new ChineseTokenizer(new StringReader(s));

            int correctStartOffset = 0;
            int correctEndOffset = 1;
            IOffsetAttribute offsetAtt = tokenizer.GetAttribute<IOffsetAttribute>();
            tokenizer.Reset();
            while (tokenizer.IncrementToken())
            {
                assertEquals(correctStartOffset, offsetAtt.StartOffset);
                assertEquals(correctEndOffset, offsetAtt.EndOffset);
                correctStartOffset++;
                correctEndOffset++;
            }
            tokenizer.End();
            tokenizer.Dispose();
        }

        [Test]
        public virtual void TestReusableTokenStream()
        {
            Analyzer a = new ChineseAnalyzer();
            AssertAnalyzesTo(a, "中华人民共和国", new string[] { "中", "华", "人", "民", "共", "和", "国" }, new int[] { 0, 1, 2, 3, 4, 5, 6 }, new int[] { 1, 2, 3, 4, 5, 6, 7 });
            AssertAnalyzesTo(a, "北京市", new string[] { "北", "京", "市" }, new int[] { 0, 1, 2 }, new int[] { 1, 2, 3 });
        }

        /*
         * Analyzer that just uses ChineseTokenizer, not ChineseFilter.
         * convenience to show the behavior of the tokenizer
         */
        private sealed class JustChineseTokenizerAnalyzer : Analyzer
        {
            protected internal override TokenStreamComponents CreateComponents(string fieldName, TextReader reader)
            {
                return new TokenStreamComponents(new ChineseTokenizer(reader));
            }
        }

        /*
         * Analyzer that just uses ChineseFilter, not ChineseTokenizer.
         * convenience to show the behavior of the filter.
         */
        private sealed class JustChineseFilterAnalyzer : Analyzer
        {
            protected internal override TokenStreamComponents CreateComponents(string fieldName, TextReader reader)
            {
                Tokenizer tokenizer = new WhitespaceTokenizer(LuceneVersion.LUCENE_CURRENT, reader);
                return new TokenStreamComponents(tokenizer, new ChineseFilter(tokenizer));
            }
        }

        /*
         * ChineseTokenizer tokenizes numbers as one token, but they are filtered by ChineseFilter
         */
        [Test]
        public virtual void TestNumerics()
        {
            Analyzer justTokenizer = new JustChineseTokenizerAnalyzer();
            AssertAnalyzesTo(justTokenizer, "中1234", new string[] { "中", "1234" });

            // in this case the ChineseAnalyzer (which applies ChineseFilter) will remove the numeric token.
            Analyzer a = new ChineseAnalyzer();
            AssertAnalyzesTo(a, "中1234", new string[] { "中" });
        }

        /*
         * ChineseTokenizer tokenizes english similar to SimpleAnalyzer.
         * it will lowercase terms automatically.
         * 
         * ChineseFilter has an english stopword list, it also removes any single character tokens.
         * the stopword list is case-sensitive.
         */
        [Test]
        public virtual void TestEnglish()
        {
            Analyzer chinese = new ChineseAnalyzer();
            AssertAnalyzesTo(chinese, "This is a Test. b c d", new string[] { "test" });

            Analyzer justTokenizer = new JustChineseTokenizerAnalyzer();
            AssertAnalyzesTo(justTokenizer, "This is a Test. b c d", new string[] { "this", "is", "a", "test", "b", "c", "d" });

            Analyzer justFilter = new JustChineseFilterAnalyzer();
            AssertAnalyzesTo(justFilter, "This is a Test. b c d", new string[] { "This", "Test." });
        }

        /// <summary>
        /// blast some random strings through the analyzer </summary>
        [Test]
        public virtual void TestRandomStrings()
        {
            CheckRandomData(Random, new ChineseAnalyzer(), 10000 * RandomMultiplier);
        }
    }
}