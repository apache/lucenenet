// Lucene version compatibility level 4.8.1
using Lucene.Net.Analysis.Core;
using Lucene.Net.Analysis.Miscellaneous;
using Lucene.Net.Analysis.Util;
using NUnit.Framework;
using System.IO;

namespace Lucene.Net.Analysis.Br
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
    /// Test the Brazilian Stem Filter, which only modifies the term text.
    /// 
    /// It is very similar to the snowball portuguese algorithm but not exactly the same.
    /// 
    /// </summary>
    public class TestBrazilianStemmer : BaseTokenStreamTestCase
    {
        [Test]
        public virtual void TestWithSnowballExamples()
        {
            Check("boa", "boa");
            Check("boainain", "boainain");
            Check("boas", "boas");
            Check("bôas", "boas"); // removes diacritic: different from snowball portugese
            Check("boassu", "boassu");
            Check("boataria", "boat");
            Check("boate", "boat");
            Check("boates", "boat");
            Check("boatos", "boat");
            Check("bob", "bob");
            Check("boba", "bob");
            Check("bobagem", "bobag");
            Check("bobagens", "bobagens");
            Check("bobalhões", "bobalho"); // removes diacritic: different from snowball portugese
            Check("bobear", "bob");
            Check("bobeira", "bobeir");
            Check("bobinho", "bobinh");
            Check("bobinhos", "bobinh");
            Check("bobo", "bob");
            Check("bobs", "bobs");
            Check("boca", "boc");
            Check("bocadas", "boc");
            Check("bocadinho", "bocadinh");
            Check("bocado", "boc");
            Check("bocaiúva", "bocaiuv"); // removes diacritic: different from snowball portuguese
            Check("boçal", "bocal"); // removes diacritic: different from snowball portuguese
            Check("bocarra", "bocarr");
            Check("bocas", "boc");
            Check("bode", "bod");
            Check("bodoque", "bodoqu");
            Check("body", "body");
            Check("boeing", "boeing");
            Check("boem", "boem");
            Check("boemia", "boem");
            Check("boêmio", "boemi"); // removes diacritic: different from snowball portuguese
            Check("bogotá", "bogot");
            Check("boi", "boi");
            Check("bóia", "boi"); // removes diacritic: different from snowball portuguese
            Check("boiando", "boi");
            Check("quiabo", "quiab");
            Check("quicaram", "quic");
            Check("quickly", "quickly");
            Check("quieto", "quiet");
            Check("quietos", "quiet");
            Check("quilate", "quilat");
            Check("quilates", "quilat");
            Check("quilinhos", "quilinh");
            Check("quilo", "quil");
            Check("quilombo", "quilomb");
            Check("quilométricas", "quilometr"); // removes diacritic: different from snowball portuguese
            Check("quilométricos", "quilometr"); // removes diacritic: different from snowball portuguese
            Check("quilômetro", "quilometr"); // removes diacritic: different from snowball portoguese
            Check("quilômetros", "quilometr"); // removes diacritic: different from snowball portoguese
            Check("quilos", "quil");
            Check("quimica", "quimic");
            Check("quilos", "quil");
            Check("quimica", "quimic");
            Check("quimicas", "quimic");
            Check("quimico", "quimic");
            Check("quimicos", "quimic");
            Check("quimioterapia", "quimioterap");
            Check("quimioterápicos", "quimioterap"); // removes diacritic: different from snowball portoguese
            Check("quimono", "quimon");
            Check("quincas", "quinc");
            Check("quinhão", "quinha"); // removes diacritic: different from snowball portoguese
            Check("quinhentos", "quinhent");
            Check("quinn", "quinn");
            Check("quino", "quin");
            Check("quinta", "quint");
            Check("quintal", "quintal");
            Check("quintana", "quintan");
            Check("quintanilha", "quintanilh");
            Check("quintão", "quinta"); // removes diacritic: different from snowball portoguese
            Check("quintessência", "quintessente"); // versus snowball portuguese 'quintessent'
            Check("quintino", "quintin");
            Check("quinto", "quint");
            Check("quintos", "quint");
            Check("quintuplicou", "quintuplic");
            Check("quinze", "quinz");
            Check("quinzena", "quinzen");
            Check("quiosque", "quiosqu");
        }

        [Test]
        public virtual void TestNormalization()
        {
            Check("Brasil", "brasil"); // lowercase by default
            Check("Brasília", "brasil"); // remove diacritics
            Check("quimio5terápicos", "quimio5terapicos"); // contains non-letter, diacritic will still be removed
            Check("áá", "áá"); // token is too short: diacritics are not removed
            Check("ááá", "aaa"); // normally, diacritics are removed
        }

        [Test]
        public virtual void TestReusableTokenStream()
        {
            Analyzer a = new BrazilianAnalyzer(TEST_VERSION_CURRENT);
            checkReuse(a, "boa", "boa");
            checkReuse(a, "boainain", "boainain");
            checkReuse(a, "boas", "boas");
            checkReuse(a, "bôas", "boas"); // removes diacritic: different from snowball portugese
        }

        [Test]
        public virtual void TestStemExclusionTable()
        {
            BrazilianAnalyzer a = new BrazilianAnalyzer(TEST_VERSION_CURRENT, CharArraySet.Empty, new CharArraySet(TEST_VERSION_CURRENT, AsSet("quintessência"), false));
            checkReuse(a, "quintessência", "quintessência"); // excluded words will be completely unchanged.
        }

        [Test]
        public virtual void TestWithKeywordAttribute()
        {
            CharArraySet set = new CharArraySet(TEST_VERSION_CURRENT, 1, true);
            set.add("Brasília");
            BrazilianStemFilter filter = new BrazilianStemFilter(new SetKeywordMarkerFilter(new LowerCaseTokenizer(TEST_VERSION_CURRENT, new StringReader("Brasília Brasilia")), set));
            AssertTokenStreamContents(filter, new string[] { "brasília", "brasil" });
        }

        private void Check(string input, string expected)
        {
            CheckOneTerm(new BrazilianAnalyzer(TEST_VERSION_CURRENT), input, expected);
        }

        private void checkReuse(Analyzer a, string input, string expected)
        {
            CheckOneTerm(a, input, expected);
        }

        /// <summary>
        /// blast some random strings through the analyzer </summary>
        [Test]
        public virtual void TestRandomStrings()
        {
            CheckRandomData(Random, new BrazilianAnalyzer(TEST_VERSION_CURRENT), 1000 * RandomMultiplier);
        }

        [Test]
        public virtual void TestEmptyTerm()
        {
            Analyzer a = Analyzer.NewAnonymous(createComponents: (fieldName, reader) =>
            {
                Tokenizer tokenizer = new KeywordTokenizer(reader);
                return new TokenStreamComponents(tokenizer, new BrazilianStemFilter(tokenizer));
            });
            CheckOneTerm(a, "", "");
        }
    }
}