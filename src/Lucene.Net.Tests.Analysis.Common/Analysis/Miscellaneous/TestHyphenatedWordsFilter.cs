// Lucene version compatibility level 4.8.1
using Lucene.Net.Analysis.Core;
using NUnit.Framework;
using System.IO;

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
    /// HyphenatedWordsFilter test
    /// </summary>
    public class TestHyphenatedWordsFilter : BaseTokenStreamTestCase
    {
        [Test]
        public virtual void TestHyphenatedWords()
        {
            string input = "ecologi-\r\ncal devel-\r\n\r\nop compre-\u0009hensive-hands-on and ecologi-\ncal";
            // first test
            TokenStream ts = new MockTokenizer(new StringReader(input), MockTokenizer.WHITESPACE, false);
            ts = new HyphenatedWordsFilter(ts);
            AssertTokenStreamContents(ts, new string[] { "ecological", "develop", "comprehensive-hands-on", "and", "ecological" });
        }

        /// <summary>
        /// Test that HyphenatedWordsFilter behaves correctly with a final hyphen
        /// </summary>
        [Test]
        public virtual void TestHyphenAtEnd()
        {
            string input = "ecologi-\r\ncal devel-\r\n\r\nop compre-\u0009hensive-hands-on and ecology-";
            // first test
            TokenStream ts = new MockTokenizer(new StringReader(input), MockTokenizer.WHITESPACE, false);
            ts = new HyphenatedWordsFilter(ts);
            AssertTokenStreamContents(ts, new string[] { "ecological", "develop", "comprehensive-hands-on", "and", "ecology-" });
        }

        [Test]
        public virtual void TestOffsets()
        {
            string input = "abc- def geh 1234- 5678-";
            TokenStream ts = new MockTokenizer(new StringReader(input), MockTokenizer.WHITESPACE, false);
            ts = new HyphenatedWordsFilter(ts);
            AssertTokenStreamContents(ts, new string[] { "abcdef", "geh", "12345678-" }, new int[] { 0, 9, 13 }, new int[] { 8, 12, 24 });
        }

        /// <summary>
        /// blast some random strings through the analyzer </summary>
        [Test]
        public virtual void TestRandomString()
        {
            Analyzer a = Analyzer.NewAnonymous(createComponents: (fieldName, reader) =>
            {
                Tokenizer tokenizer = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
                return new TokenStreamComponents(tokenizer, new HyphenatedWordsFilter(tokenizer));
            });

            CheckRandomData(Random, a, 1000 * RandomMultiplier);
        }

        [Test]
        public virtual void TestEmptyTerm()
        {
            Analyzer a = Analyzer.NewAnonymous(createComponents: (fieldName, reader) =>
            {
                Tokenizer tokenizer = new KeywordTokenizer(reader);
                return new TokenStreamComponents(tokenizer, new HyphenatedWordsFilter(tokenizer));
            });
            CheckOneTerm(a, "", "");
        }
    }
}