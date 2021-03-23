// Lucene version compatibility level 4.8.1
using Lucene.Net.Analysis.Core;
using Lucene.Net.Util;
using NUnit.Framework;
using System;
using System.IO;

namespace Lucene.Net.Analysis.Reverse
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

    public class TestReverseStringFilter : BaseTokenStreamTestCase
    {
        [Test]
        public virtual void TestFilter()
        {
            TokenStream stream = new MockTokenizer(new StringReader("Do have a nice day"), MockTokenizer.WHITESPACE, false); // 1-4 length string
            ReverseStringFilter filter = new ReverseStringFilter(TEST_VERSION_CURRENT, stream);
            AssertTokenStreamContents(filter, new string[] { "oD", "evah", "a", "ecin", "yad" });
        }

        [Test]
        public virtual void TestFilterWithMark()
        {
            TokenStream stream = new MockTokenizer(new StringReader("Do have a nice day"), MockTokenizer.WHITESPACE, false); // 1-4 length string
            ReverseStringFilter filter = new ReverseStringFilter(TEST_VERSION_CURRENT, stream, '\u0001');
            AssertTokenStreamContents(filter, new string[] { "\u0001oD", "\u0001evah", "\u0001a", "\u0001ecin", "\u0001yad" });
        }

        [Test]
        public virtual void TestReverseString()
        {
            assertEquals("A", ReverseStringFilter.Reverse(TEST_VERSION_CURRENT, "A"));
            assertEquals("BA", ReverseStringFilter.Reverse(TEST_VERSION_CURRENT, "AB"));
            assertEquals("CBA", ReverseStringFilter.Reverse(TEST_VERSION_CURRENT, "ABC"));
        }

        [Test]
        public virtual void TestReverseChar()
        {
            char[] buffer = new char[] { 'A', 'B', 'C', 'D', 'E', 'F' };
            ReverseStringFilter.Reverse(TEST_VERSION_CURRENT, buffer, 2, 3);
            assertEquals("ABEDCF", new string(buffer));
        }

        /// <summary>
        /// Test the broken 3.0 behavior, for back compat </summary>
        /// @deprecated (3.1) Remove in Lucene 5.0 
        [Test]
        [Obsolete("(3.1) Remove in Lucene 5.0")]
        public virtual void TestBackCompat()
        {
            assertEquals("\uDF05\uD866\uDF05\uD866", ReverseStringFilter.Reverse(LuceneVersion.LUCENE_30, "𩬅𩬅"));
        }

        [Test]
        public virtual void TestReverseSupplementary()
        {
            // supplementary at end
            assertEquals("𩬅艱鍟䇹愯瀛", ReverseStringFilter.Reverse(TEST_VERSION_CURRENT, "瀛愯䇹鍟艱𩬅"));
            // supplementary at end - 1
            assertEquals("a𩬅艱鍟䇹愯瀛", ReverseStringFilter.Reverse(TEST_VERSION_CURRENT, "瀛愯䇹鍟艱𩬅a"));
            // supplementary at start
            assertEquals("fedcba𩬅", ReverseStringFilter.Reverse(TEST_VERSION_CURRENT, "𩬅abcdef"));
            // supplementary at start + 1
            assertEquals("fedcba𩬅z", ReverseStringFilter.Reverse(TEST_VERSION_CURRENT, "z𩬅abcdef"));
            // supplementary medial
            assertEquals("gfe𩬅dcba", ReverseStringFilter.Reverse(TEST_VERSION_CURRENT, "abcd𩬅efg"));
        }

        [Test]
        public virtual void TestReverseSupplementaryChar()
        {
            // supplementary at end
            char[] buffer = "abc瀛愯䇹鍟艱𩬅".ToCharArray();
            ReverseStringFilter.Reverse(TEST_VERSION_CURRENT, buffer, 3, 7);
            assertEquals("abc𩬅艱鍟䇹愯瀛", new string(buffer));
            // supplementary at end - 1
            buffer = "abc瀛愯䇹鍟艱𩬅d".ToCharArray();
            ReverseStringFilter.Reverse(TEST_VERSION_CURRENT, buffer, 3, 8);
            assertEquals("abcd𩬅艱鍟䇹愯瀛", new string(buffer));
            // supplementary at start
            buffer = "abc𩬅瀛愯䇹鍟艱".ToCharArray();
            ReverseStringFilter.Reverse(TEST_VERSION_CURRENT, buffer, 3, 7);
            assertEquals("abc艱鍟䇹愯瀛𩬅", new string(buffer));
            // supplementary at start + 1
            buffer = "abcd𩬅瀛愯䇹鍟艱".ToCharArray();
            ReverseStringFilter.Reverse(TEST_VERSION_CURRENT, buffer, 3, 8);
            assertEquals("abc艱鍟䇹愯瀛𩬅d", new string(buffer));
            // supplementary medial
            buffer = "abc瀛愯𩬅def".ToCharArray();
            ReverseStringFilter.Reverse(TEST_VERSION_CURRENT, buffer, 3, 7);
            assertEquals("abcfed𩬅愯瀛", new string(buffer));
        }

        /// <summary>
        /// blast some random strings through the analyzer </summary>
        [Test]
        public virtual void TestRandomStrings()
        {
            Analyzer a = Analyzer.NewAnonymous(createComponents: (fieldName, reader) =>
            {
                Tokenizer tokenizer = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
                return new TokenStreamComponents(tokenizer, new ReverseStringFilter(TEST_VERSION_CURRENT, tokenizer));
            });
            CheckRandomData(Random, a, 1000 * RandomMultiplier);
        }

        [Test]
        public virtual void TestEmptyTerm()
        {
            Analyzer a = Analyzer.NewAnonymous(createComponents: (fieldName, reader) =>
            {
                Tokenizer tokenizer = new KeywordTokenizer(reader);
                return new TokenStreamComponents(tokenizer, new ReverseStringFilter(TEST_VERSION_CURRENT, tokenizer));
            });
            CheckOneTerm(a, "", "");
        }
    }
}