using System;
using NUnit.Framework;
using System.IO;
using Lucene.Net.Analysis.Util;
using Lucene.Net.Analysis.Miscellaneous;
using Lucene.Net.Analysis.Core;

namespace Lucene.Net.Analysis.No
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
    /// Simple tests for <seealso cref="NorwegianMinimalStemFilter"/>
    /// </summary>
    public class TestNorwegianMinimalStemFilter : BaseTokenStreamTestCase
    {
        private Analyzer analyzer = new AnalyzerAnonymousInnerClassHelper();

        private class AnalyzerAnonymousInnerClassHelper : Analyzer
        {
            public AnalyzerAnonymousInnerClassHelper()
            {
            }

            protected override TokenStreamComponents CreateComponents(string fieldName, TextReader reader)
            {
                Tokenizer source = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
                return new TokenStreamComponents(source, new NorwegianMinimalStemFilter(source, NorwegianLightStemmer.BOKMAAL));
            }
        }

        /// <summary>
        /// Test against a Bokmål vocabulary file </summary>
        [Test]
        public virtual void TestVocabulary()
        {
            VocabularyAssert.AssertVocabulary(analyzer, GetDataFile("nb_minimal.txt"));
        }

        /// <summary>
        /// Test against a Nynorsk vocabulary file </summary>
        [Test]
        public virtual void TestNynorskVocabulary()
        {
            Analyzer analyzer = new AnalyzerAnonymousInnerClassHelper2(this);
            VocabularyAssert.AssertVocabulary(analyzer, GetDataFile("nn_minimal.txt"));
        }

        private class AnalyzerAnonymousInnerClassHelper2 : Analyzer
        {
            private readonly TestNorwegianMinimalStemFilter outerInstance;

            public AnalyzerAnonymousInnerClassHelper2(TestNorwegianMinimalStemFilter outerInstance)
            {
                this.outerInstance = outerInstance;
            }

            protected override TokenStreamComponents CreateComponents(string fieldName, TextReader reader)
            {
                Tokenizer source = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
                return new TokenStreamComponents(source, new NorwegianMinimalStemFilter(source, NorwegianLightStemmer.NYNORSK));
            }
        }

        [Test]
        public virtual void TestKeyword()
        {
            CharArraySet exclusionSet = new CharArraySet(TEST_VERSION_CURRENT, AsSet("sekretæren"), false);
            Analyzer a = new AnalyzerAnonymousInnerClassHelper3(this, exclusionSet);
            CheckOneTerm(a, "sekretæren", "sekretæren");
        }

        private class AnalyzerAnonymousInnerClassHelper3 : Analyzer
        {
            private readonly TestNorwegianMinimalStemFilter outerInstance;

            private CharArraySet exclusionSet;

            public AnalyzerAnonymousInnerClassHelper3(TestNorwegianMinimalStemFilter outerInstance, CharArraySet exclusionSet)
            {
                this.outerInstance = outerInstance;
                this.exclusionSet = exclusionSet;
            }

            protected override TokenStreamComponents CreateComponents(string fieldName, TextReader reader)
            {
                Tokenizer source = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
                TokenStream sink = new SetKeywordMarkerFilter(source, exclusionSet);
                return new TokenStreamComponents(source, new NorwegianMinimalStemFilter(sink));
            }
        }

        /// <summary>
        /// blast some random strings through the analyzer </summary>
        [Test]
        public virtual void TestRandomStrings()
        {
            Random random = Random();
            CheckRandomData(random, analyzer, 1000 * RANDOM_MULTIPLIER);
        }

        [Test]
        public virtual void TestEmptyTerm()
        {
            Analyzer a = new AnalyzerAnonymousInnerClassHelper4(this);
            CheckOneTerm(a, "", "");
        }

        private class AnalyzerAnonymousInnerClassHelper4 : Analyzer
        {
            private readonly TestNorwegianMinimalStemFilter outerInstance;

            public AnalyzerAnonymousInnerClassHelper4(TestNorwegianMinimalStemFilter outerInstance)
            {
                this.outerInstance = outerInstance;
            }

            protected override TokenStreamComponents CreateComponents(string fieldName, TextReader reader)
            {
                Tokenizer tokenizer = new KeywordTokenizer(reader);
                return new TokenStreamComponents(tokenizer, new NorwegianMinimalStemFilter(tokenizer));
            }
        }
    }
}