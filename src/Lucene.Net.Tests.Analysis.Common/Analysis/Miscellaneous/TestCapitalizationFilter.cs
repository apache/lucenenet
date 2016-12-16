using Lucene.Net.Analysis.Core;
using Lucene.Net.Analysis.Util;
using Lucene.Net.Support;
using NUnit.Framework;
using System;
using System.Collections.Generic;
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
    /// Tests <seealso cref="CapitalizationFilter"/> </summary>
    public class TestCapitalizationFilter : BaseTokenStreamTestCase
    {
        [Test]
        public virtual void TestCapitalization()
        {
            CharArraySet keep = new CharArraySet(TEST_VERSION_CURRENT, Arrays.AsList("and", "the", "it", "BIG"), false);

            AssertCapitalizesTo("kiTTEN", new string[] { "Kitten" }, true, keep, true, null, 0, CapitalizationFilter.DEFAULT_MAX_WORD_COUNT, CapitalizationFilter.DEFAULT_MAX_TOKEN_LENGTH);

            AssertCapitalizesTo("and", new string[] { "And" }, true, keep, true, null, 0, CapitalizationFilter.DEFAULT_MAX_WORD_COUNT, CapitalizationFilter.DEFAULT_MAX_TOKEN_LENGTH);

            AssertCapitalizesTo("AnD", new string[] { "And" }, true, keep, true, null, 0, CapitalizationFilter.DEFAULT_MAX_WORD_COUNT, CapitalizationFilter.DEFAULT_MAX_TOKEN_LENGTH);

            //first is not forced, but it's not a keep word, either
            AssertCapitalizesTo("AnD", new string[] { "And" }, true, keep, false, null, 0, CapitalizationFilter.DEFAULT_MAX_WORD_COUNT, CapitalizationFilter.DEFAULT_MAX_TOKEN_LENGTH);

            AssertCapitalizesTo("big", new string[] { "Big" }, true, keep, true, null, 0, CapitalizationFilter.DEFAULT_MAX_WORD_COUNT, CapitalizationFilter.DEFAULT_MAX_TOKEN_LENGTH);

            AssertCapitalizesTo("BIG", new string[] { "BIG" }, true, keep, true, null, 0, CapitalizationFilter.DEFAULT_MAX_WORD_COUNT, CapitalizationFilter.DEFAULT_MAX_TOKEN_LENGTH);

            AssertCapitalizesToKeyword("Hello thEre my Name is Ryan", "Hello there my name is ryan", true, keep, true, null, 0, CapitalizationFilter.DEFAULT_MAX_WORD_COUNT, CapitalizationFilter.DEFAULT_MAX_TOKEN_LENGTH);

            // now each token
            AssertCapitalizesTo("Hello thEre my Name is Ryan", new string[] { "Hello", "There", "My", "Name", "Is", "Ryan" }, false, keep, true, null, 0, CapitalizationFilter.DEFAULT_MAX_WORD_COUNT, CapitalizationFilter.DEFAULT_MAX_TOKEN_LENGTH);

            // now only the long words
            AssertCapitalizesTo("Hello thEre my Name is Ryan", new string[] { "Hello", "There", "my", "Name", "is", "Ryan" }, false, keep, true, null, 3, CapitalizationFilter.DEFAULT_MAX_WORD_COUNT, CapitalizationFilter.DEFAULT_MAX_TOKEN_LENGTH);

            // without prefix
            AssertCapitalizesTo("McKinley", new string[] { "Mckinley" }, true, keep, true, null, 0, CapitalizationFilter.DEFAULT_MAX_WORD_COUNT, CapitalizationFilter.DEFAULT_MAX_TOKEN_LENGTH);

            // Now try some prefixes
            IList<char[]> okPrefix = new List<char[]>();
            okPrefix.Add("McK".ToCharArray());

            AssertCapitalizesTo("McKinley", new string[] { "McKinley" }, true, keep, true, okPrefix, 0, CapitalizationFilter.DEFAULT_MAX_WORD_COUNT, CapitalizationFilter.DEFAULT_MAX_TOKEN_LENGTH);

            // now try some stuff with numbers
            AssertCapitalizesTo("1st 2nd third", new string[] { "1st", "2nd", "Third" }, false, keep, false, null, 0, CapitalizationFilter.DEFAULT_MAX_WORD_COUNT, CapitalizationFilter.DEFAULT_MAX_TOKEN_LENGTH);

            AssertCapitalizesToKeyword("the The the", "The The the", false, keep, true, null, 0, CapitalizationFilter.DEFAULT_MAX_WORD_COUNT, CapitalizationFilter.DEFAULT_MAX_TOKEN_LENGTH);
        }

        internal static void AssertCapitalizesTo(Tokenizer tokenizer, string[] expected, bool onlyFirstWord, CharArraySet keep, bool forceFirstLetter, ICollection<char[]> okPrefix, int minWordLength, int maxWordCount, int maxTokenLength)
        {
            CapitalizationFilter filter = new CapitalizationFilter(tokenizer, onlyFirstWord, keep, forceFirstLetter, okPrefix, minWordLength, maxWordCount, maxTokenLength);
            AssertTokenStreamContents(filter, expected);
        }

        internal static void AssertCapitalizesTo(string input, string[] expected, bool onlyFirstWord, CharArraySet keep, bool forceFirstLetter, ICollection<char[]> okPrefix, int minWordLength, int maxWordCount, int maxTokenLength)
        {
            AssertCapitalizesTo(new MockTokenizer(new StringReader(input), MockTokenizer.WHITESPACE, false), expected, onlyFirstWord, keep, forceFirstLetter, okPrefix, minWordLength, maxWordCount, maxTokenLength);
        }

        internal static void AssertCapitalizesToKeyword(string input, string expected, bool onlyFirstWord, CharArraySet keep, bool forceFirstLetter, ICollection<char[]> okPrefix, int minWordLength, int maxWordCount, int maxTokenLength)
        {
            AssertCapitalizesTo(new MockTokenizer(new StringReader(input), MockTokenizer.KEYWORD, false), new string[] { expected }, onlyFirstWord, keep, forceFirstLetter, okPrefix, minWordLength, maxWordCount, maxTokenLength);
        }

        /// <summary>
        /// blast some random strings through the analyzer </summary>
        [Test]
        public virtual void TestRandomString()
        {
            Analyzer a = new AnalyzerAnonymousInnerClassHelper(this);

            CheckRandomData(Random(), a, 1000 * RANDOM_MULTIPLIER);
        }

        private class AnalyzerAnonymousInnerClassHelper : Analyzer
        {
            private readonly TestCapitalizationFilter outerInstance;

            public AnalyzerAnonymousInnerClassHelper(TestCapitalizationFilter outerInstance)
            {
                this.outerInstance = outerInstance;
            }

            public override TokenStreamComponents CreateComponents(string fieldName, TextReader reader)
            {
                Tokenizer tokenizer = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
                return new TokenStreamComponents(tokenizer, new CapitalizationFilter(tokenizer));
            }
        }

        [Test]
        public virtual void TestEmptyTerm()
        {
            Analyzer a = new AnalyzerAnonymousInnerClassHelper2(this);
            CheckOneTerm(a, "", "");
        }

        private class AnalyzerAnonymousInnerClassHelper2 : Analyzer
        {
            private readonly TestCapitalizationFilter outerInstance;

            public AnalyzerAnonymousInnerClassHelper2(TestCapitalizationFilter outerInstance)
            {
                this.outerInstance = outerInstance;
            }

            public override TokenStreamComponents CreateComponents(string fieldName, TextReader reader)
            {
                Tokenizer tokenizer = new KeywordTokenizer(reader);
                return new TokenStreamComponents(tokenizer, new CapitalizationFilter(tokenizer));
            }
        }

        /// <summary>
        /// checking the validity of constructor arguments
        /// </summary>
        [Test]
        public virtual void TestIllegalArguments()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new CapitalizationFilter(new MockTokenizer(new StringReader("accept only valid arguments"), MockTokenizer.WHITESPACE, false), true, null, true, null, -1, CapitalizationFilter.DEFAULT_MAX_WORD_COUNT, CapitalizationFilter.DEFAULT_MAX_TOKEN_LENGTH));
        }

        [Test]
        public virtual void TestIllegalArguments1()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new CapitalizationFilter(new MockTokenizer(new StringReader("accept only valid arguments"), MockTokenizer.WHITESPACE, false), true, null, true, null, 0, -10, CapitalizationFilter.DEFAULT_MAX_TOKEN_LENGTH));
        }

        [Test]
        public virtual void TestIllegalArguments2()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new CapitalizationFilter(new MockTokenizer(new StringReader("accept only valid arguments"), MockTokenizer.WHITESPACE, false), true, null, true, null, 0, CapitalizationFilter.DEFAULT_MAX_WORD_COUNT, -50));
        }
    }
}