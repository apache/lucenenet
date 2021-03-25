// Lucene version compatibility level 4.8.1
using Lucene.Net.Analysis.Core;
using Lucene.Net.Analysis.Miscellaneous;
using Lucene.Net.Analysis.Util;
using NUnit.Framework;

namespace Lucene.Net.Analysis.Fr
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
    /// Simple tests for <seealso cref="FrenchLightStemFilter"/>
    /// </summary>
    public class TestFrenchLightStemFilter : BaseTokenStreamTestCase
    {
        private static readonly Analyzer analyzer = Analyzer.NewAnonymous(createComponents: (fieldName, reader) =>
        {
            Tokenizer source = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
            return new TokenStreamComponents(source, new FrenchLightStemFilter(source));
        });

        /// <summary>
        /// Test some examples from the paper </summary>
        [Test]
        public virtual void TestExamples()
        {
            CheckOneTerm(analyzer, "chevaux", "cheval");
            CheckOneTerm(analyzer, "cheval", "cheval");

            CheckOneTerm(analyzer, "hiboux", "hibou");
            CheckOneTerm(analyzer, "hibou", "hibou");

            CheckOneTerm(analyzer, "chantés", "chant");
            CheckOneTerm(analyzer, "chanter", "chant");
            CheckOneTerm(analyzer, "chante", "chant");
            CheckOneTerm(analyzer, "chant", "chant");

            CheckOneTerm(analyzer, "baronnes", "baron");
            CheckOneTerm(analyzer, "barons", "baron");
            CheckOneTerm(analyzer, "baron", "baron");

            CheckOneTerm(analyzer, "peaux", "peau");
            CheckOneTerm(analyzer, "peau", "peau");

            CheckOneTerm(analyzer, "anneaux", "aneau");
            CheckOneTerm(analyzer, "anneau", "aneau");

            CheckOneTerm(analyzer, "neveux", "neveu");
            CheckOneTerm(analyzer, "neveu", "neveu");

            CheckOneTerm(analyzer, "affreux", "afreu");
            CheckOneTerm(analyzer, "affreuse", "afreu");

            CheckOneTerm(analyzer, "investissement", "investi");
            CheckOneTerm(analyzer, "investir", "investi");

            CheckOneTerm(analyzer, "assourdissant", "asourdi");
            CheckOneTerm(analyzer, "assourdir", "asourdi");

            CheckOneTerm(analyzer, "pratiquement", "pratiqu");
            CheckOneTerm(analyzer, "pratique", "pratiqu");

            CheckOneTerm(analyzer, "administrativement", "administratif");
            CheckOneTerm(analyzer, "administratif", "administratif");

            CheckOneTerm(analyzer, "justificatrice", "justifi");
            CheckOneTerm(analyzer, "justificateur", "justifi");
            CheckOneTerm(analyzer, "justifier", "justifi");

            CheckOneTerm(analyzer, "educatrice", "eduqu");
            CheckOneTerm(analyzer, "eduquer", "eduqu");

            CheckOneTerm(analyzer, "communicateur", "comuniqu");
            CheckOneTerm(analyzer, "communiquer", "comuniqu");

            CheckOneTerm(analyzer, "accompagnatrice", "acompagn");
            CheckOneTerm(analyzer, "accompagnateur", "acompagn");

            CheckOneTerm(analyzer, "administrateur", "administr");
            CheckOneTerm(analyzer, "administrer", "administr");

            CheckOneTerm(analyzer, "productrice", "product");
            CheckOneTerm(analyzer, "producteur", "product");

            CheckOneTerm(analyzer, "acheteuse", "achet");
            CheckOneTerm(analyzer, "acheteur", "achet");

            CheckOneTerm(analyzer, "planteur", "plant");
            CheckOneTerm(analyzer, "plante", "plant");

            CheckOneTerm(analyzer, "poreuse", "poreu");
            CheckOneTerm(analyzer, "poreux", "poreu");

            CheckOneTerm(analyzer, "plieuse", "plieu");

            CheckOneTerm(analyzer, "bijoutière", "bijouti");
            CheckOneTerm(analyzer, "bijoutier", "bijouti");

            CheckOneTerm(analyzer, "caissière", "caisi");
            CheckOneTerm(analyzer, "caissier", "caisi");

            CheckOneTerm(analyzer, "abrasive", "abrasif");
            CheckOneTerm(analyzer, "abrasif", "abrasif");

            CheckOneTerm(analyzer, "folle", "fou");
            CheckOneTerm(analyzer, "fou", "fou");

            CheckOneTerm(analyzer, "personnelle", "person");
            CheckOneTerm(analyzer, "personne", "person");

            // algo bug: too short length
            //CheckOneTerm(analyzer, "personnel", "person");

            CheckOneTerm(analyzer, "complète", "complet");
            CheckOneTerm(analyzer, "complet", "complet");

            CheckOneTerm(analyzer, "aromatique", "aromat");

            CheckOneTerm(analyzer, "faiblesse", "faibl");
            CheckOneTerm(analyzer, "faible", "faibl");

            CheckOneTerm(analyzer, "patinage", "patin");
            CheckOneTerm(analyzer, "patin", "patin");

            CheckOneTerm(analyzer, "sonorisation", "sono");

            CheckOneTerm(analyzer, "ritualisation", "rituel");
            CheckOneTerm(analyzer, "rituel", "rituel");

            // algo bug: masked by rules above
            //CheckOneTerm(analyzer, "colonisateur", "colon");

            CheckOneTerm(analyzer, "nomination", "nomin");

            CheckOneTerm(analyzer, "disposition", "dispos");
            CheckOneTerm(analyzer, "dispose", "dispos");

            // SOLR-3463 : abusive compression of repeated characters in numbers
            // Trailing repeated char elision :
            CheckOneTerm(analyzer, "1234555", "1234555");
            // Repeated char within numbers with more than 4 characters :
            CheckOneTerm(analyzer, "12333345", "12333345");
            // Short numbers weren't affected already:
            CheckOneTerm(analyzer, "1234", "1234");
            // Ensure behaviour is preserved for words!
            // Trailing repeated char elision :
            CheckOneTerm(analyzer, "abcdeff", "abcdef");
            // Repeated char within words with more than 4 characters :
            CheckOneTerm(analyzer, "abcccddeef", "abcdef");
            CheckOneTerm(analyzer, "créées", "cre");
            // Combined letter and digit repetition
            CheckOneTerm(analyzer, "22hh00", "22h00"); // 10:00pm
        }

        /// <summary>
        /// Test against a vocabulary from the reference impl </summary>
        [Test]
        public virtual void TestVocabulary()
        {
            VocabularyAssert.AssertVocabulary(analyzer, GetDataFile("frlighttestdata.zip"), "frlight.txt");
        }

        [Test]
        public virtual void TestKeyword()
        {
            CharArraySet exclusionSet = new CharArraySet(TEST_VERSION_CURRENT, AsSet("chevaux"), false);
            Analyzer a = Analyzer.NewAnonymous(createComponents: (fieldName, reader) =>
            {
                Tokenizer source = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
                TokenStream sink = new SetKeywordMarkerFilter(source, exclusionSet);
                return new TokenStreamComponents(source, new FrenchLightStemFilter(sink));
            });

            CheckOneTerm(a, "chevaux", "chevaux");
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
                return new TokenStreamComponents(tokenizer, new FrenchLightStemFilter(tokenizer));
            });
            CheckOneTerm(a, "", "");
        }
    }
}