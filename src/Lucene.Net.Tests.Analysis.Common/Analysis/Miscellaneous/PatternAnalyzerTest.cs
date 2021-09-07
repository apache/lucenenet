// Lucene version compatibility level 4.8.1
using Lucene.Net.Analysis.Core;
using Lucene.Net.Support;
using NUnit.Framework;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace Lucene.Net.Analysis.Miscellaneous
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
    /// Verifies the behavior of PatternAnalyzer.
    /// </summary>
#pragma warning disable 612, 618
    public class PatternAnalyzerTest : BaseTokenStreamTestCase
    {

        /// <summary>
        /// Test PatternAnalyzer when it is configured with a non-word pattern.
        /// Behavior can be similar to SimpleAnalyzer (depending upon options)
        /// </summary>
        [Test]
        public virtual void TestNonWordPattern()
        {
            // Split on non-letter pattern, do not lowercase, no stopwords
            PatternAnalyzer a = new PatternAnalyzer(TEST_VERSION_CURRENT, PatternAnalyzer.NON_WORD_PATTERN, false, null);
            Check(a, "The quick brown Fox,the abcd1234 (56.78) dc.", new string[] { "The", "quick", "brown", "Fox", "the", "abcd", "dc" });

            // split on non-letter pattern, lowercase, english stopwords
            PatternAnalyzer b = new PatternAnalyzer(TEST_VERSION_CURRENT, PatternAnalyzer.NON_WORD_PATTERN, true, StopAnalyzer.ENGLISH_STOP_WORDS_SET);
            Check(b, "The quick brown Fox,the abcd1234 (56.78) dc.", new string[] { "quick", "brown", "fox", "abcd", "dc" });
        }

        /// <summary>
        /// Test PatternAnalyzer when it is configured with a whitespace pattern.
        /// Behavior can be similar to WhitespaceAnalyzer (depending upon options)
        /// </summary>
        [Test]
        public virtual void TestWhitespacePattern()
        {
            // Split on whitespace patterns, do not lowercase, no stopwords
            PatternAnalyzer a = new PatternAnalyzer(TEST_VERSION_CURRENT, PatternAnalyzer.WHITESPACE_PATTERN, false, null);
            Check(a, "The quick brown Fox,the abcd1234 (56.78) dc.", new string[] { "The", "quick", "brown", "Fox,the", "abcd1234", "(56.78)", "dc." });

            // Split on whitespace patterns, lowercase, english stopwords
            PatternAnalyzer b = new PatternAnalyzer(TEST_VERSION_CURRENT, PatternAnalyzer.WHITESPACE_PATTERN, true, StopAnalyzer.ENGLISH_STOP_WORDS_SET);
            Check(b, "The quick brown Fox,the abcd1234 (56.78) dc.", new string[] { "quick", "brown", "fox,the", "abcd1234", "(56.78)", "dc." });
        }

        /// <summary>
        /// Test PatternAnalyzer when it is configured with a custom pattern. In this
        /// case, text is tokenized on the comma ","
        /// </summary>
        [Test]
        public virtual void TestCustomPattern()
        {
            // Split on comma, do not lowercase, no stopwords
            PatternAnalyzer a = new PatternAnalyzer(TEST_VERSION_CURRENT, new Regex(",", RegexOptions.Compiled), false, null);
            Check(a, "Here,Are,some,Comma,separated,words,", new string[] { "Here", "Are", "some", "Comma", "separated", "words" });

            // split on comma, lowercase, english stopwords
            PatternAnalyzer b = new PatternAnalyzer(TEST_VERSION_CURRENT, new Regex(",", RegexOptions.Compiled), true, StopAnalyzer.ENGLISH_STOP_WORDS_SET);
            Check(b, "Here,Are,some,Comma,separated,words,", new string[] { "here", "some", "comma", "separated", "words" });
        }

        /// <summary>
        /// Test PatternAnalyzer against a large document.
        /// </summary>
        [Test]
        public virtual void TestHugeDocument()
        {
            StringBuilder document = new StringBuilder();
            // 5000 a's
            char[] largeWord = new char[5000];
            Arrays.Fill(largeWord, 'a');
            document.Append(largeWord);

            // a space
            document.Append(' ');

            // 2000 b's
            char[] largeWord2 = new char[2000];
            Arrays.Fill(largeWord2, 'b');
            document.Append(largeWord2);

            // Split on whitespace patterns, do not lowercase, no stopwords
            PatternAnalyzer a = new PatternAnalyzer(TEST_VERSION_CURRENT, PatternAnalyzer.WHITESPACE_PATTERN, false, null);
            Check(a, document.ToString(), new string[]
            {
            new string(largeWord),
            new string(largeWord2)
            });
        }

        /// <summary>
        /// Verify the analyzer analyzes to the expected contents. For PatternAnalyzer,
        /// several methods are verified:
        /// <ul>
        /// <li>Analysis with a normal Reader
        /// <li>Analysis with a FastStringReader
        /// <li>Analysis with a String
        /// </ul>
        /// </summary>
        private void Check(PatternAnalyzer analyzer, string document, string[] expected)
        {
            // ordinary analysis of a Reader
            AssertAnalyzesTo(analyzer, document, expected);

            // analysis with a "FastStringReader"
            TokenStream ts = analyzer.GetTokenStream("dummy", new PatternAnalyzer.FastStringReader(document));
            AssertTokenStreamContents(ts, expected);

            // analysis of a String, uses PatternAnalyzer.tokenStream(String, String)
            TokenStream ts2 = analyzer.GetTokenStream("dummy", new StringReader(document));
            AssertTokenStreamContents(ts2, expected);
        }

        /// <summary>
        /// blast some random strings through the analyzer </summary>
        [Test]
        public virtual void TestRandomStrings()
        {
            // LUCENENET: Removed code dealing with buggy JRE

            Analyzer a = new PatternAnalyzer(TEST_VERSION_CURRENT, new Regex(",", RegexOptions.Compiled), true, StopAnalyzer.ENGLISH_STOP_WORDS_SET);

            CheckRandomData(Random, a, 10000 * RandomMultiplier);
        }
    }
#pragma warning restore 612, 618
}