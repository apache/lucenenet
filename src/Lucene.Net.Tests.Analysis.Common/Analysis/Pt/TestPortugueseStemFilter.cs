using System.IO;
using NUnit.Framework;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Analysis.Core;
using Lucene.Net.Analysis.Util;
using Lucene.Net.Analysis.Miscellaneous;

namespace Lucene.Net.Analysis.Pt
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
    /// Simple tests for <seealso cref="PortugueseStemFilter"/>
    /// </summary>
    public class TestPortugueseStemFilter : BaseTokenStreamTestCase
    {
        private Analyzer analyzer = new AnalyzerAnonymousInnerClassHelper();

        private class AnalyzerAnonymousInnerClassHelper : Analyzer
        {
            public AnalyzerAnonymousInnerClassHelper()
            {
            }

            protected internal override TokenStreamComponents CreateComponents(string fieldName, TextReader reader)
            {
                Tokenizer source = new StandardTokenizer(TEST_VERSION_CURRENT, reader);
                TokenStream result = new LowerCaseFilter(TEST_VERSION_CURRENT, source);
                return new TokenStreamComponents(source, new PortugueseStemFilter(result));
            }
        }

        /// <summary>
        /// Test the example from the paper "Assessing the impact of stemming accuracy
        /// on information retrieval"
        /// </summary>
        [Test]
        public virtual void TestExamples()
        {
            AssertAnalyzesTo(analyzer, "O debate político, pelo menos o que vem a público, parece, de modo nada " + "surpreendente, restrito a temas menores. Mas há, evidentemente, " + "grandes questões em jogo nas eleições que se aproximam.", new string[] { "o", "debat", "politic", "pel", "menos", "o", "que", "vem", "a", "public", "parec", "de", "mod", "nad", "surpreend", "restrit", "a", "tem", "men", "mas", "ha", "evid", "grand", "quest", "em", "jog", "na", "eleic", "que", "se", "aproxim" });
        }

        /// <summary>
        /// Test against a vocabulary from the reference impl </summary>
        [Test]
        public virtual void TestVocabulary()
        {
            VocabularyAssert.AssertVocabulary(analyzer, GetDataFile("ptrslptestdata.zip"), "ptrslp.txt");
        }

        [Test]
        public virtual void TestKeyword()
        {
            CharArraySet exclusionSet = new CharArraySet(TEST_VERSION_CURRENT, AsSet("quilométricas"), false);
            Analyzer a = new AnalyzerAnonymousInnerClassHelper2(this, exclusionSet);
            CheckOneTerm(a, "quilométricas", "quilométricas");
        }

        private class AnalyzerAnonymousInnerClassHelper2 : Analyzer
        {
            private readonly TestPortugueseStemFilter outerInstance;

            private CharArraySet exclusionSet;

            public AnalyzerAnonymousInnerClassHelper2(TestPortugueseStemFilter outerInstance, CharArraySet exclusionSet)
            {
                this.outerInstance = outerInstance;
                this.exclusionSet = exclusionSet;
            }

            protected internal override TokenStreamComponents CreateComponents(string fieldName, TextReader reader)
            {
                Tokenizer source = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
                TokenStream sink = new SetKeywordMarkerFilter(source, exclusionSet);
                return new TokenStreamComponents(source, new PortugueseStemFilter(sink));
            }
        }

        /// <summary>
        /// blast some random strings through the analyzer </summary>
        [Test]
        public virtual void TestRandomStrings()
        {
            CheckRandomData(Random(), analyzer, 1000 * RANDOM_MULTIPLIER);
        }

        [Test]
        public virtual void TestEmptyTerm()
        {
            Analyzer a = new AnalyzerAnonymousInnerClassHelper3(this);
            CheckOneTerm(a, "", "");
        }

        private class AnalyzerAnonymousInnerClassHelper3 : Analyzer
        {
            private readonly TestPortugueseStemFilter outerInstance;

            public AnalyzerAnonymousInnerClassHelper3(TestPortugueseStemFilter outerInstance)
            {
                this.outerInstance = outerInstance;
            }

            protected internal override TokenStreamComponents CreateComponents(string fieldName, TextReader reader)
            {
                Tokenizer tokenizer = new KeywordTokenizer(reader);
                return new TokenStreamComponents(tokenizer, new PortugueseStemFilter(tokenizer));
            }
        }
    }
}