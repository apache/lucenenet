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
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Miscellaneous;
using Lucene.Net.Test.Analysis;
using NUnit.Framework;
using Version = Lucene.Net.Util.Version;

namespace Lucene.Net.Analyzers.Miscellaneous
{
    /*
     * Verifies the behavior of PatternAnalyzer.
     */
    [TestFixture]
    public class PatternAnalyzerTest : BaseTokenStreamTestCase
    {
        /*
         * Test PatternAnalyzer when it is configured with a non-word pattern.
         * Behavior can be similar to SimpleAnalyzer (depending upon options)
         */
        [Test]
        public void TestNonWordPattern()
        {
            // Split on non-letter pattern, do not lowercase, no stopwords
            PatternAnalyzer a = new PatternAnalyzer(Version.LUCENE_CURRENT, PatternAnalyzer.NON_WORD_PATTERN,
                false, null);
            Check(a, "The quick brown Fox,the abcd1234 (56.78) dc.", new String[]
                                                                         {
                                                                             "The", "quick", "brown", "Fox", "the",
                                                                             "abcd", "dc"
                                                                         });

            // split on non-letter pattern, lowercase, english stopwords
            PatternAnalyzer b = new PatternAnalyzer(Version.LUCENE_CURRENT, PatternAnalyzer.NON_WORD_PATTERN,
                true, StopAnalyzer.ENGLISH_STOP_WORDS_SET);
            Check(b, "The quick brown Fox,the abcd1234 (56.78) dc.", new String[]
                                                                         {
                                                                             "quick", "brown", "fox", "abcd", "dc"
                                                                         });
        }

        /*
         * Test PatternAnalyzer when it is configured with a whitespace pattern.
         * Behavior can be similar to WhitespaceAnalyzer (depending upon options)
         */
        [Test]
        public void TestWhitespacePattern()
        {
            // Split on whitespace patterns, do not lowercase, no stopwords
            PatternAnalyzer a = new PatternAnalyzer(Version.LUCENE_CURRENT, PatternAnalyzer.WHITESPACE_PATTERN,
                false, null);
            Check(a, "The quick brown Fox,the abcd1234 (56.78) dc.", new String[]
                                                                         {
                                                                             "The", "quick", "brown", "Fox,the",
                                                                             "abcd1234", "(56.78)", "dc."
                                                                         });

            // Split on whitespace patterns, lowercase, english stopwords
            PatternAnalyzer b = new PatternAnalyzer(Version.LUCENE_CURRENT, PatternAnalyzer.WHITESPACE_PATTERN,
                true, StopAnalyzer.ENGLISH_STOP_WORDS_SET);
            Check(b, "The quick brown Fox,the abcd1234 (56.78) dc.", new String[]
                                                                         {
                                                                             "quick", "brown", "fox,the", "abcd1234",
                                                                             "(56.78)", "dc."
                                                                         });
        }

        /*
         * Test PatternAnalyzer when it is configured with a custom pattern. In this
         * case, text is tokenized on the comma ","
         */
        [Test]
        public void TestCustomPattern()
        {
            // Split on comma, do not lowercase, no stopwords
            PatternAnalyzer a = new PatternAnalyzer(Version.LUCENE_CURRENT, new Regex(",", RegexOptions.Compiled), false, null);
            Check(a, "Here,Are,some,Comma,separated,words,", new String[]
                                                                 {
                                                                     "Here",
                                                                     "Are", "some", "Comma", "separated", "words"
                                                                 });

            // split on comma, lowercase, english stopwords
            PatternAnalyzer b = new PatternAnalyzer(Version.LUCENE_CURRENT, new Regex(",", RegexOptions.Compiled), true,
                StopAnalyzer.ENGLISH_STOP_WORDS_SET);
            Check(b, "Here,Are,some,Comma,separated,words,", new String[]
                                                                 {
                                                                     "here",
                                                                     "some", "comma", "separated", "words"
                                                                 });
        }

        /*
         * Test PatternAnalyzer against a large document.
         */
        [Test]
        public void TestHugeDocument()
        {
            StringBuilder document = new StringBuilder();
            // 5000 a's
            char[] largeWord;
            largeWord = Enumerable.Repeat('a', 5000).ToArray();
            document.Append(largeWord);

            // a space
            document.Append(' ');

            // 2000 b's
            char[] largeWord2;
            largeWord2 = Enumerable.Repeat('b', 2000).ToArray();
            document.Append(largeWord2);

            // Split on whitespace patterns, do not lowercase, no stopwords
            PatternAnalyzer a = new PatternAnalyzer(Version.LUCENE_CURRENT, PatternAnalyzer.WHITESPACE_PATTERN,
                false, null);
            Check(a, document.ToString(), new String[]
                                              {
                                                  new String(largeWord),
                                                  new String(largeWord2)
                                              });
        }

        /*
         * Verify the analyzer analyzes to the expected contents. For PatternAnalyzer,
         * several methods are verified:
         * <ul>
         * <li>Analysis with a normal Reader
         * <li>Analysis with a FastStringReader
         * <li>Analysis with a String
         * </ul>
         */
        private void Check(PatternAnalyzer analyzer, String document,
            String[] expected)
        {
            // ordinary analysis of a Reader
            AssertAnalyzesTo(analyzer, document, expected);

            // analysis with a "FastStringReader"
            TokenStream ts = analyzer.TokenStream("dummy",
                new PatternAnalyzer.FastStringReader(document));
            AssertTokenStreamContents(ts, expected);

            // analysis of a String, uses PatternAnalyzer.tokenStream(String, String)
            TokenStream ts2 = analyzer.TokenStream("dummy", document);
            AssertTokenStreamContents(ts2, expected);
        }
    }
}
