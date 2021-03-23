// Lucene version compatibility level 4.8.1
using Lucene.Net.Analysis.Core;
using NUnit.Framework;
using System.IO;
using System.Text.RegularExpressions;

namespace Lucene.Net.Analysis.Pattern
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

    public class TestPatternReplaceFilter : BaseTokenStreamTestCase
    {

        [Test]
        public virtual void TestReplaceAll()
        {
            string input = "aabfooaabfooabfoob ab caaaaaaaaab";
            TokenStream ts = new PatternReplaceFilter(new MockTokenizer(new StringReader(input), MockTokenizer.WHITESPACE, false), new Regex("a*b", RegexOptions.Compiled), "-", true);
            AssertTokenStreamContents(ts, new string[] { "-foo-foo-foo-", "-", "c-" });
        }

        [Test]
        public virtual void TestReplaceFirst()
        {
            string input = "aabfooaabfooabfoob ab caaaaaaaaab";
            TokenStream ts = new PatternReplaceFilter(new MockTokenizer(new StringReader(input), MockTokenizer.WHITESPACE, false), new Regex("a*b", RegexOptions.Compiled), "-", false);
            AssertTokenStreamContents(ts, new string[] { "-fooaabfooabfoob", "-", "c-" });
        }

        [Test]
        public virtual void TestStripFirst()
        {
            string input = "aabfooaabfooabfoob ab caaaaaaaaab";
            TokenStream ts = new PatternReplaceFilter(new MockTokenizer(new StringReader(input), MockTokenizer.WHITESPACE, false), new Regex("a*b", RegexOptions.Compiled), null, false);
            AssertTokenStreamContents(ts, new string[] { "fooaabfooabfoob", "", "c" });
        }

        [Test]
        public virtual void TestStripAll()
        {
            string input = "aabfooaabfooabfoob ab caaaaaaaaab";
            TokenStream ts = new PatternReplaceFilter(new MockTokenizer(new StringReader(input), MockTokenizer.WHITESPACE, false), new Regex("a*b", RegexOptions.Compiled), null, true);
            AssertTokenStreamContents(ts, new string[] { "foofoofoo", "", "c" });
        }

        [Test]
        public virtual void TestReplaceAllWithBackRef()
        {
            string input = "aabfooaabfooabfoob ab caaaaaaaaab";
            // LUCENENET NOTE: In .NET we don't need to escape $ like \\$$ (Java), it is like $$
            TokenStream ts = new PatternReplaceFilter(new MockTokenizer(new StringReader(input), MockTokenizer.WHITESPACE, false), new Regex("(a*)b", RegexOptions.Compiled), /*"$1\\$$"*/ "$1$$", true);
            AssertTokenStreamContents(ts, new string[] { "aa$fooaa$fooa$foo$", "a$", "caaaaaaaaa$" });
        }

        /// <summary>
        /// blast some random strings through the analyzer </summary>
        [Test]
        public virtual void TestRandomStrings()
        {
            Analyzer a = Analyzer.NewAnonymous(createComponents: (fieldName, reader) =>
            {
                Tokenizer tokenizer = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
                TokenStream filter = new PatternReplaceFilter(tokenizer, new Regex("a", RegexOptions.Compiled), "b", false);
                return new TokenStreamComponents(tokenizer, filter);
            });
            CheckRandomData(Random, a, 1000 * RandomMultiplier);

            Analyzer b = Analyzer.NewAnonymous(createComponents: (fieldName, reader) =>
            {
                Tokenizer tokenizer = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
                TokenStream filter = new PatternReplaceFilter(tokenizer, new Regex("a", RegexOptions.Compiled), "b", true);
                return new TokenStreamComponents(tokenizer, filter);
            });
            CheckRandomData(Random, b, 1000 * RandomMultiplier);
        }

        [Test]
        public virtual void TestEmptyTerm()
        {
            Analyzer a = Analyzer.NewAnonymous(createComponents: (fieldName, reader) =>
            {
                Tokenizer tokenizer = new KeywordTokenizer(reader);
                return new TokenStreamComponents(tokenizer, new PatternReplaceFilter(tokenizer, new Regex("a", RegexOptions.Compiled), "b", true));
            });
            CheckOneTerm(a, "", "");
        }
    }
}