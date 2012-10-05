/*
 *
 * Licensed to the Apache Software Foundation (ASF) under one
 * or more contributor license agreements.  See the NOTICE file
 * distributed with this work for additional information
 * regarding copyright ownership.  The ASF licenses this file
 * to you under the Apache License, Version 2.0 (the
 * "License"); you may not use this file except in compliance
 * with the License.  You may obtain a copy of the License at
 *
 *   http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing,
 * software distributed under the License is distributed on an
 * "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
 * KIND, either express or implied.  See the License for the
 * specific language governing permissions and limitations
 * under the License.
 *
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.BR;
using Lucene.Net.Test.Analysis;
using NUnit.Framework;
using Version = Lucene.Net.Util.Version;

namespace Lucene.Net.Analyzers.Br
{
    /*
     * Test the Brazilian Stem Filter, which only modifies the term text.
     * 
     * It is very similar to the snowball portuguese algorithm but not exactly the same.
     *
     */
    [TestFixture]
    public class TestBrazilianStemmer : BaseTokenStreamTestCase
    {
        [Test]
        public void TestWithSnowballExamples()
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

        public void TestNormalization()
        {
            Check("Brasil", "brasil"); // lowercase by default
            Check("Brasília", "brasil"); // remove diacritics
            Check("quimio5terápicos", "quimio5terapicos"); // contains non-letter, diacritic will still be removed
            Check("áá", "áá"); // token is too short: diacritics are not removed
            Check("ááá", "aaa"); // normally, diacritics are removed
        }

        [Test]
        public void TestReusableTokenStream()
        {
            Analyzer a = new BrazilianAnalyzer(Version.LUCENE_CURRENT);
            CheckReuse(a, "boa", "boa");
            CheckReuse(a, "boainain", "boainain");
            CheckReuse(a, "boas", "boas");
            CheckReuse(a, "bôas", "boas"); // removes diacritic: different from snowball portugese
        }

        [Test]
        public void TestStemExclusionTable()
        {
            BrazilianAnalyzer a = new BrazilianAnalyzer(Version.LUCENE_CURRENT);
            a.SetStemExclusionTable(new String[] { "quintessência" });
            CheckReuse(a, "quintessência", "quintessência"); // excluded words will be completely unchanged.
        }

        /* 
         * Test that changes to the exclusion table are applied immediately
         * when using reusable token streams.
         */
        [Test]
        public void TestExclusionTableReuse()
        {
            BrazilianAnalyzer a = new BrazilianAnalyzer(Version.LUCENE_CURRENT);
            CheckReuse(a, "quintessência", "quintessente");
            a.SetStemExclusionTable(new String[] { "quintessência" });
            CheckReuse(a, "quintessência", "quintessência");
        }

        private void Check(String input, String expected)
        {
            CheckOneTerm(new BrazilianAnalyzer(Version.LUCENE_CURRENT), input, expected);
        }

        private void CheckReuse(Analyzer a, String input, String expected)
        {
            CheckOneTermReuse(a, input, expected);
        }
    }
}
