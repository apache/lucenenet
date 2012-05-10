/*
 *
 * Licensed to the Apache Software Foundation (ASF) under one
 * or more contributor license agreements.  See the NOTICE file
 * distributed with this work for additional information
 * regarding copyright ownership.  The ASF licenses this file
 * to you under the Apache License, Version 2.0 (the
 * "License"); you may not use this file except in compliance
 * with the License.  You may obtain a copy of the License at
 *
 *   http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing,
 * software distributed under the License is distributed on an
 * "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
 * KIND, either express or implied.  See the License for the
 * specific language governing permissions and limitations
 * under the License.
 *
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Cn;
using Lucene.Net.Analysis.Tokenattributes;
using Lucene.Net.Test.Analysis;
using NUnit.Framework;

namespace Lucene.Net.Analyzers.Cn
{
    [TestFixture]
    public class TestChineseTokenizer : BaseTokenStreamTestCase
    {
        [Test]
        public void TestOtherLetterOffset()
        {
            String s = "a天b";
            ChineseTokenizer tokenizer = new ChineseTokenizer(new StringReader(s));

            int correctStartOffset = 0;
            int correctEndOffset = 1;
            IOffsetAttribute offsetAtt = tokenizer.GetAttribute<IOffsetAttribute>();
            while (tokenizer.IncrementToken())
            {
                Assert.AreEqual(correctStartOffset, offsetAtt.StartOffset);
                Assert.AreEqual(correctEndOffset, offsetAtt.EndOffset);
                correctStartOffset++;
                correctEndOffset++;
            }
        }

        [Test]
        public void TestReusableTokenStream()
        {
            Analyzer a = new ChineseAnalyzer();
            AssertAnalyzesToReuse(a, "中华人民共和国",
                                  new String[] { "中", "华", "人", "民", "共", "和", "国" },
                                  new int[] { 0, 1, 2, 3, 4, 5, 6 },
                                  new int[] { 1, 2, 3, 4, 5, 6, 7 });
            AssertAnalyzesToReuse(a, "北京市",
                                  new String[] { "北", "京", "市" },
                                  new int[] { 0, 1, 2 },
                                  new int[] { 1, 2, 3 });
        }

        /*
         * Analyzer that just uses ChineseTokenizer, not ChineseFilter.
         * convenience to show the behavior of the tokenizer
         */
        private class JustChineseTokenizerAnalyzer : Analyzer
        {
            public override TokenStream TokenStream(String fieldName, TextReader reader)
            {
                return new ChineseTokenizer(reader);
            }
        }

        /*
         * Analyzer that just uses ChineseFilter, not ChineseTokenizer.
         * convenience to show the behavior of the filter.
         */
        private class JustChineseFilterAnalyzer : Analyzer
        {
            public override TokenStream TokenStream(String fieldName, TextReader reader)
            {
                return new ChineseFilter(new WhitespaceTokenizer(reader));
            }
        }

        /*
         * ChineseTokenizer tokenizes numbers as one token, but they are filtered by ChineseFilter
         */
        [Test]
        public void TestNumerics()
        {
            Analyzer justTokenizer = new JustChineseTokenizerAnalyzer();
            AssertAnalyzesTo(justTokenizer, "中1234", new String[] {"中", "1234"});

            // in this case the ChineseAnalyzer (which applies ChineseFilter) will remove the numeric token.
            Analyzer a = new ChineseAnalyzer();
            AssertAnalyzesTo(a, "中1234", new String[] {"中"});
        }

        /*
         * ChineseTokenizer tokenizes english similar to SimpleAnalyzer.
         * it will lowercase terms automatically.
         * 
         * ChineseFilter has an english stopword list, it also removes any single character tokens.
         * the stopword list is case-sensitive.
         */
        [Test]
        public void TestEnglish()
        {
            Analyzer chinese = new ChineseAnalyzer();
            AssertAnalyzesTo(chinese, "This is a Test. b c d",
                             new String[] {"test"});

            Analyzer justTokenizer = new JustChineseTokenizerAnalyzer();
            AssertAnalyzesTo(justTokenizer, "This is a Test. b c d",
                             new String[] {"this", "is", "a", "test", "b", "c", "d"});

            Analyzer justFilter = new JustChineseFilterAnalyzer();
            AssertAnalyzesTo(justFilter, "This is a Test. b c d",
                             new String[] {"This", "Test."});
        }
    }
}
