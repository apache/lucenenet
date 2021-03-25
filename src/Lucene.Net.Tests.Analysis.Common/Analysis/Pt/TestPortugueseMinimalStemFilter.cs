// Lucene version compatibility level 4.8.1
using Lucene.Net.Analysis.Core;
using Lucene.Net.Analysis.Miscellaneous;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Analysis.Util;
using NUnit.Framework;

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
    /// Simple tests for <seealso cref="PortugueseMinimalStemFilter"/>
    /// </summary>
    public class TestPortugueseMinimalStemFilter : BaseTokenStreamTestCase
    {
        private static readonly Analyzer analyzer = Analyzer.NewAnonymous(createComponents: (fieldName, reader) =>
        {
            Tokenizer source = new StandardTokenizer(TEST_VERSION_CURRENT, reader);
            TokenStream result = new LowerCaseFilter(TEST_VERSION_CURRENT, source);
            return new TokenStreamComponents(source, new PortugueseMinimalStemFilter(result));
        });

        /// <summary>
        /// Test the example from the paper "Assessing the impact of stemming accuracy
        /// on information retrieval"
        /// </summary>
        [Test]
        public virtual void TestExamples()
        {
            AssertAnalyzesTo(analyzer, "O debate político, pelo menos o que vem a público, parece, de modo nada " + "surpreendente, restrito a temas menores. Mas há, evidentemente, " + "grandes questões em jogo nas eleições que se aproximam.", new string[] { "o", "debate", "político", "pelo", "menos", "o", "que", "vem", "a", "público", "parece", "de", "modo", "nada", "surpreendente", "restrito", "a", "tema", "menor", "mas", "há", "evidentemente", "grande", "questão", "em", "jogo", "na", "eleição", "que", "se", "aproximam" });
        }

        /// <summary>
        /// Test against a vocabulary from the reference impl </summary>
        [Test]
        public virtual void TestVocabulary()
        {
            VocabularyAssert.AssertVocabulary(analyzer, GetDataFile("ptminimaltestdata.zip"), "ptminimal.txt");
        }

        [Test]
        public virtual void TestKeyword()
        {
            CharArraySet exclusionSet = new CharArraySet(TEST_VERSION_CURRENT, AsSet("quilométricas"), false);
            Analyzer a = Analyzer.NewAnonymous(createComponents: (fieldName, reader) =>
            {
                Tokenizer source = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
                TokenStream sink = new SetKeywordMarkerFilter(source, exclusionSet);
                return new TokenStreamComponents(source, new PortugueseMinimalStemFilter(sink));
            });
            CheckOneTerm(a, "quilométricas", "quilométricas");
        }

        /// <summary>
        /// blast some random strings through the analyzer </summary>
        [Test]
        public virtual void TestRandomStrings()
        {
            CheckRandomData(Random, analyzer, 1000 * RandomMultiplier);
        }

        [Test]
        public virtual void TestEmptyTerm()
        {
            Analyzer a = Analyzer.NewAnonymous(createComponents: (fieldName, reader) =>
            {
                Tokenizer tokenizer = new KeywordTokenizer(reader);
                return new TokenStreamComponents(tokenizer, new PortugueseMinimalStemFilter(tokenizer));
            });
            CheckOneTerm(a, "", "");
        }
    }
}