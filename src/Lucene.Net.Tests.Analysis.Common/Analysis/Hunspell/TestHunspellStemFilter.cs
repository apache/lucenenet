using Lucene.Net.Analysis.Core;
using Lucene.Net.Analysis.Miscellaneous;
using Lucene.Net.Analysis.Util;
using Lucene.Net.Support;
using Lucene.Net.Util;
using NUnit.Framework;
using System.IO;

namespace Lucene.Net.Analysis.Hunspell
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

    public class TestHunspellStemFilter : BaseTokenStreamTestCase
    {
        private static Dictionary dictionary;

        [TestFixtureSetUp]
        public static void BeforeClass()
        {
            System.IO.Stream affixStream = typeof(TestStemmer).getResourceAsStream("simple.aff");
            System.IO.Stream dictStream = typeof(TestStemmer).getResourceAsStream("simple.dic");
            try
            {
                dictionary = new Dictionary(affixStream, dictStream);
            }
            finally
            {
                IOUtils.CloseWhileHandlingException(affixStream, dictStream);
            }
        }

        [OneTimeTearDown]
        public static void afterClass()
        {
            dictionary = null;
        }

        /// <summary>
        /// Simple test for KeywordAttribute </summary>
        [Test]
        public virtual void TestKeywordAttribute()
        {
            MockTokenizer tokenizer = new MockTokenizer(new StringReader("lucene is awesome"));
            tokenizer.EnableChecks = true;
            HunspellStemFilter filter = new HunspellStemFilter(tokenizer, dictionary);
            AssertTokenStreamContents(filter, new string[] { "lucene", "lucen", "is", "awesome" }, new int[] { 1, 0, 1, 1 });

            // assert with keyword marker
            tokenizer = new MockTokenizer(new StringReader("lucene is awesome"));
            CharArraySet set = new CharArraySet(TEST_VERSION_CURRENT, Arrays.AsList("Lucene"), true);
            filter = new HunspellStemFilter(new SetKeywordMarkerFilter(tokenizer, set), dictionary);
            AssertTokenStreamContents(filter, new string[] { "lucene", "is", "awesome" }, new int[] { 1, 1, 1 });
        }

        /// <summary>
        /// simple test for longestOnly option </summary>
        [Test]
        public virtual void TestLongestOnly()
        {
            MockTokenizer tokenizer = new MockTokenizer(new StringReader("lucene is awesome"));
            tokenizer.EnableChecks = true;
            HunspellStemFilter filter = new HunspellStemFilter(tokenizer, dictionary, true, true);
            AssertTokenStreamContents(filter, new string[] { "lucene", "is", "awesome" }, new int[] { 1, 1, 1 });
        }

        /// <summary>
        /// blast some random strings through the analyzer </summary>
        [Test]
        public virtual void TestRandomStrings()
        {
            Analyzer analyzer = new AnalyzerAnonymousInnerClassHelper(this);
            CheckRandomData(Random(), analyzer, 1000 * RANDOM_MULTIPLIER);
        }

        private class AnalyzerAnonymousInnerClassHelper : Analyzer
        {
            private readonly TestHunspellStemFilter outerInstance;

            public AnalyzerAnonymousInnerClassHelper(TestHunspellStemFilter outerInstance)
            {
                this.outerInstance = outerInstance;
            }

            public override TokenStreamComponents CreateComponents(string fieldName, TextReader reader)
            {
                Tokenizer tokenizer = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
                return new TokenStreamComponents(tokenizer, new HunspellStemFilter(tokenizer, dictionary));
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
            private readonly TestHunspellStemFilter outerInstance;

            public AnalyzerAnonymousInnerClassHelper2(TestHunspellStemFilter outerInstance)
            {
                this.outerInstance = outerInstance;
            }

            public override TokenStreamComponents CreateComponents(string fieldName, TextReader reader)
            {
                Tokenizer tokenizer = new KeywordTokenizer(reader);
                return new TokenStreamComponents(tokenizer, new HunspellStemFilter(tokenizer, dictionary));
            }
        }

        [Test]
        public virtual void TestIgnoreCaseNoSideEffects()
        {
            Dictionary d;
            System.IO.Stream affixStream = typeof(TestStemmer).getResourceAsStream("simple.aff");
            System.IO.Stream dictStream = typeof(TestStemmer).getResourceAsStream("simple.dic");
            try
            {
                d = new Dictionary(affixStream, Arrays.AsList(dictStream), true);
            }
            finally
            {
                IOUtils.CloseWhileHandlingException(affixStream, dictStream);
            }
            Analyzer a = new AnalyzerAnonymousInnerClassHelper3(this, d);
            CheckOneTerm(a, "NoChAnGy", "NoChAnGy");
        }

        private class AnalyzerAnonymousInnerClassHelper3 : Analyzer
        {
            private readonly TestHunspellStemFilter outerInstance;

            private Dictionary d;

            public AnalyzerAnonymousInnerClassHelper3(TestHunspellStemFilter outerInstance, Dictionary d)
            {
                this.outerInstance = outerInstance;
                this.d = d;
            }

            public override TokenStreamComponents CreateComponents(string fieldName, TextReader reader)
            {
                Tokenizer tokenizer = new KeywordTokenizer(reader);
                return new TokenStreamComponents(tokenizer, new HunspellStemFilter(tokenizer, d));
            }
        }
    }
}