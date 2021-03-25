// Lucene version compatibility level 4.8.1
using Lucene.Net.Analysis.Util;
using Lucene.Net.Util;
using NUnit.Framework;
using System.Collections.Generic;
using System.IO;
using JCG = J2N.Collections.Generic;

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
    /// Test <seealso cref="KeepWordFilter"/> </summary>
    public class TestKeepWordFilter : BaseTokenStreamTestCase
    {

        [Test]
        public virtual void TestStopAndGo()
        {
            ISet<string> words = new JCG.HashSet<string>();
            words.Add("aaa");
            words.Add("bbb");

            string input = "xxx yyy aaa zzz BBB ccc ddd EEE";

            // Test Stopwords
            TokenStream stream = new MockTokenizer(new StringReader(input), MockTokenizer.WHITESPACE, false);
            stream = new KeepWordFilter(TEST_VERSION_CURRENT, stream, new CharArraySet(TEST_VERSION_CURRENT, words, true));
            AssertTokenStreamContents(stream, new string[] { "aaa", "BBB" }, new int[] { 3, 2 });

            // Now force case
            stream = new MockTokenizer(new StringReader(input), MockTokenizer.WHITESPACE, false);
            stream = new KeepWordFilter(TEST_VERSION_CURRENT, stream, new CharArraySet(TEST_VERSION_CURRENT, words, false));
            AssertTokenStreamContents(stream, new string[] { "aaa" }, new int[] { 3 });

            // Test Stopwords
            stream = new MockTokenizer(new StringReader(input), MockTokenizer.WHITESPACE, false);
#pragma warning disable 612, 618
            stream = new KeepWordFilter(LuceneVersion.LUCENE_43, false, stream, new CharArraySet(TEST_VERSION_CURRENT, words, true));
            AssertTokenStreamContents(stream, new string[] { "aaa", "BBB" }, new int[] { 1, 1 });

            // Now force case
            stream = new MockTokenizer(new StringReader(input), MockTokenizer.WHITESPACE, false);
            stream = new KeepWordFilter(LuceneVersion.LUCENE_43, false, stream, new CharArraySet(TEST_VERSION_CURRENT, words, false));
#pragma warning restore 612, 618
            AssertTokenStreamContents(stream, new string[] { "aaa" }, new int[] { 1 });
        }

        /// <summary>
        /// blast some random strings through the analyzer </summary>
        [Test]
        public virtual void TestRandomStrings()
        {
            ISet<string> words = new JCG.HashSet<string>();
            words.Add("a");
            words.Add("b");

            Analyzer a = Analyzer.NewAnonymous(createComponents: (fieldName, reader) =>
            {
                Tokenizer tokenizer = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
                TokenStream stream = new KeepWordFilter(TEST_VERSION_CURRENT, tokenizer, new CharArraySet(TEST_VERSION_CURRENT, words, true));
                return new TokenStreamComponents(tokenizer, stream);
            });

            CheckRandomData(Random, a, 1000 * RandomMultiplier);
        }
    }
}