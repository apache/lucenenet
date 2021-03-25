// Lucene version compatibility level 4.8.1
using Lucene.Net.Analysis.Core;
using NUnit.Framework;

namespace Lucene.Net.Analysis.Lv
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
    /// Basic tests for <seealso cref="LatvianStemmer"/>
    /// </summary>
    public class TestLatvianStemmer : BaseTokenStreamTestCase
    {
        private static readonly Analyzer a = Analyzer.NewAnonymous(createComponents: (fieldName, reader) =>
        {
            Tokenizer tokenizer = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
            return new TokenStreamComponents(tokenizer, new LatvianStemFilter(tokenizer));
        });

        [Test]
        public virtual void TestNouns1()
        {
            // decl. I
            CheckOneTerm(a, "tēvs", "tēv"); // nom. sing.
            CheckOneTerm(a, "tēvi", "tēv"); // nom. pl.
            CheckOneTerm(a, "tēva", "tēv"); // gen. sing.
            CheckOneTerm(a, "tēvu", "tēv"); // gen. pl.
            CheckOneTerm(a, "tēvam", "tēv"); // dat. sing.
            CheckOneTerm(a, "tēviem", "tēv"); // dat. pl.
            CheckOneTerm(a, "tēvu", "tēv"); // acc. sing.
            CheckOneTerm(a, "tēvus", "tēv"); // acc. pl.
            CheckOneTerm(a, "tēvā", "tēv"); // loc. sing.
            CheckOneTerm(a, "tēvos", "tēv"); // loc. pl.
            CheckOneTerm(a, "tēvs", "tēv"); // voc. sing.
            CheckOneTerm(a, "tēvi", "tēv"); // voc. pl.
        }

        /// <summary>
        /// decl II nouns with (s,t) -> š and (d,z) -> ž
        /// palatalization will generally conflate to two stems
        /// due to the ambiguity (plural and singular).
        /// </summary>
        [Test]
        public virtual void TestNouns2()
        {
            // decl. II

            // c -> č palatalization
            CheckOneTerm(a, "lācis", "lāc"); // nom. sing.
            CheckOneTerm(a, "lāči", "lāc"); // nom. pl.
            CheckOneTerm(a, "lāča", "lāc"); // gen. sing.
            CheckOneTerm(a, "lāču", "lāc"); // gen. pl.
            CheckOneTerm(a, "lācim", "lāc"); // dat. sing.
            CheckOneTerm(a, "lāčiem", "lāc"); // dat. pl.
            CheckOneTerm(a, "lāci", "lāc"); // acc. sing.
            CheckOneTerm(a, "lāčus", "lāc"); // acc. pl.
            CheckOneTerm(a, "lācī", "lāc"); // loc. sing.
            CheckOneTerm(a, "lāčos", "lāc"); // loc. pl.
            CheckOneTerm(a, "lāci", "lāc"); // voc. sing.
            CheckOneTerm(a, "lāči", "lāc"); // voc. pl.

            // n -> ņ palatalization
            CheckOneTerm(a, "akmens", "akmen"); // nom. sing.
            CheckOneTerm(a, "akmeņi", "akmen"); // nom. pl.
            CheckOneTerm(a, "akmens", "akmen"); // gen. sing.
            CheckOneTerm(a, "akmeņu", "akmen"); // gen. pl.
            CheckOneTerm(a, "akmenim", "akmen"); // dat. sing.
            CheckOneTerm(a, "akmeņiem", "akmen"); // dat. pl.
            CheckOneTerm(a, "akmeni", "akmen"); // acc. sing.
            CheckOneTerm(a, "akmeņus", "akmen"); // acc. pl.
            CheckOneTerm(a, "akmenī", "akmen"); // loc. sing.
            CheckOneTerm(a, "akmeņos", "akmen"); // loc. pl.
            CheckOneTerm(a, "akmens", "akmen"); // voc. sing.
            CheckOneTerm(a, "akmeņi", "akmen"); // voc. pl.

            // no palatalization
            CheckOneTerm(a, "kurmis", "kurm"); // nom. sing.
            CheckOneTerm(a, "kurmji", "kurm"); // nom. pl.
            CheckOneTerm(a, "kurmja", "kurm"); // gen. sing.
            CheckOneTerm(a, "kurmju", "kurm"); // gen. pl.
            CheckOneTerm(a, "kurmim", "kurm"); // dat. sing.
            CheckOneTerm(a, "kurmjiem", "kurm"); // dat. pl.
            CheckOneTerm(a, "kurmi", "kurm"); // acc. sing.
            CheckOneTerm(a, "kurmjus", "kurm"); // acc. pl.
            CheckOneTerm(a, "kurmī", "kurm"); // loc. sing.
            CheckOneTerm(a, "kurmjos", "kurm"); // loc. pl.
            CheckOneTerm(a, "kurmi", "kurm"); // voc. sing.
            CheckOneTerm(a, "kurmji", "kurm"); // voc. pl.
        }

        [Test]
        public virtual void TestNouns3()
        {
            // decl III
            CheckOneTerm(a, "lietus", "liet"); // nom. sing.
            CheckOneTerm(a, "lieti", "liet"); // nom. pl.
            CheckOneTerm(a, "lietus", "liet"); // gen. sing.
            CheckOneTerm(a, "lietu", "liet"); // gen. pl.
            CheckOneTerm(a, "lietum", "liet"); // dat. sing.
            CheckOneTerm(a, "lietiem", "liet"); // dat. pl.
            CheckOneTerm(a, "lietu", "liet"); // acc. sing.
            CheckOneTerm(a, "lietus", "liet"); // acc. pl.
            CheckOneTerm(a, "lietū", "liet"); // loc. sing.
            CheckOneTerm(a, "lietos", "liet"); // loc. pl.
            CheckOneTerm(a, "lietus", "liet"); // voc. sing.
            CheckOneTerm(a, "lieti", "liet"); // voc. pl.
        }

        [Test]
        public virtual void TestNouns4()
        {
            // decl IV
            CheckOneTerm(a, "lapa", "lap"); // nom. sing.
            CheckOneTerm(a, "lapas", "lap"); // nom. pl.
            CheckOneTerm(a, "lapas", "lap"); // gen. sing.
            CheckOneTerm(a, "lapu", "lap"); // gen. pl.
            CheckOneTerm(a, "lapai", "lap"); // dat. sing.
            CheckOneTerm(a, "lapām", "lap"); // dat. pl.
            CheckOneTerm(a, "lapu", "lap"); // acc. sing.
            CheckOneTerm(a, "lapas", "lap"); // acc. pl.
            CheckOneTerm(a, "lapā", "lap"); // loc. sing.
            CheckOneTerm(a, "lapās", "lap"); // loc. pl.
            CheckOneTerm(a, "lapa", "lap"); // voc. sing.
            CheckOneTerm(a, "lapas", "lap"); // voc. pl.

            CheckOneTerm(a, "puika", "puik"); // nom. sing.
            CheckOneTerm(a, "puikas", "puik"); // nom. pl.
            CheckOneTerm(a, "puikas", "puik"); // gen. sing.
            CheckOneTerm(a, "puiku", "puik"); // gen. pl.
            CheckOneTerm(a, "puikam", "puik"); // dat. sing.
            CheckOneTerm(a, "puikām", "puik"); // dat. pl.
            CheckOneTerm(a, "puiku", "puik"); // acc. sing.
            CheckOneTerm(a, "puikas", "puik"); // acc. pl.
            CheckOneTerm(a, "puikā", "puik"); // loc. sing.
            CheckOneTerm(a, "puikās", "puik"); // loc. pl.
            CheckOneTerm(a, "puika", "puik"); // voc. sing.
            CheckOneTerm(a, "puikas", "puik"); // voc. pl.
        }

        /// <summary>
        /// Genitive plural forms with (s,t) -> š and (d,z) -> ž
        /// will not conflate due to ambiguity.
        /// </summary>
        [Test]
        public virtual void TestNouns5()
        {
            // decl V
            // l -> ļ palatalization
            CheckOneTerm(a, "egle", "egl"); // nom. sing.
            CheckOneTerm(a, "egles", "egl"); // nom. pl.
            CheckOneTerm(a, "egles", "egl"); // gen. sing.
            CheckOneTerm(a, "egļu", "egl"); // gen. pl.
            CheckOneTerm(a, "eglei", "egl"); // dat. sing.
            CheckOneTerm(a, "eglēm", "egl"); // dat. pl.
            CheckOneTerm(a, "egli", "egl"); // acc. sing.
            CheckOneTerm(a, "egles", "egl"); // acc. pl.
            CheckOneTerm(a, "eglē", "egl"); // loc. sing.
            CheckOneTerm(a, "eglēs", "egl"); // loc. pl.
            CheckOneTerm(a, "egle", "egl"); // voc. sing.
            CheckOneTerm(a, "egles", "egl"); // voc. pl.
        }

        [Test]
        public virtual void TestNouns6()
        {
            // decl VI

            // no palatalization
            CheckOneTerm(a, "govs", "gov"); // nom. sing.
            CheckOneTerm(a, "govis", "gov"); // nom. pl.
            CheckOneTerm(a, "govs", "gov"); // gen. sing.
            CheckOneTerm(a, "govju", "gov"); // gen. pl.
            CheckOneTerm(a, "govij", "gov"); // dat. sing.
            CheckOneTerm(a, "govīm", "gov"); // dat. pl.
            CheckOneTerm(a, "govi ", "gov"); // acc. sing.
            CheckOneTerm(a, "govis", "gov"); // acc. pl.
            CheckOneTerm(a, "govi ", "gov"); // inst. sing.
            CheckOneTerm(a, "govīm", "gov"); // inst. pl.
            CheckOneTerm(a, "govī", "gov"); // loc. sing.
            CheckOneTerm(a, "govīs", "gov"); // loc. pl.
            CheckOneTerm(a, "govs", "gov"); // voc. sing.
            CheckOneTerm(a, "govis", "gov"); // voc. pl.
        }

        [Test]
        public virtual void TestAdjectives()
        {
            CheckOneTerm(a, "zils", "zil"); // indef. nom. masc. sing.
            CheckOneTerm(a, "zilais", "zil"); // def. nom. masc. sing.
            CheckOneTerm(a, "zili", "zil"); // indef. nom. masc. pl.
            CheckOneTerm(a, "zilie", "zil"); // def. nom. masc. pl.
            CheckOneTerm(a, "zila", "zil"); // indef. nom. fem. sing.
            CheckOneTerm(a, "zilā", "zil"); // def. nom. fem. sing.
            CheckOneTerm(a, "zilas", "zil"); // indef. nom. fem. pl.
            CheckOneTerm(a, "zilās", "zil"); // def. nom. fem. pl.
            CheckOneTerm(a, "zila", "zil"); // indef. gen. masc. sing.
            CheckOneTerm(a, "zilā", "zil"); // def. gen. masc. sing.
            CheckOneTerm(a, "zilu", "zil"); // indef. gen. masc. pl.
            CheckOneTerm(a, "zilo", "zil"); // def. gen. masc. pl.
            CheckOneTerm(a, "zilas", "zil"); // indef. gen. fem. sing.
            CheckOneTerm(a, "zilās", "zil"); // def. gen. fem. sing.
            CheckOneTerm(a, "zilu", "zil"); // indef. gen. fem. pl.
            CheckOneTerm(a, "zilo", "zil"); // def. gen. fem. pl.
            CheckOneTerm(a, "zilam", "zil"); // indef. dat. masc. sing.
            CheckOneTerm(a, "zilajam", "zil"); // def. dat. masc. sing.
            CheckOneTerm(a, "ziliem", "zil"); // indef. dat. masc. pl.
            CheckOneTerm(a, "zilajiem", "zil"); // def. dat. masc. pl.
            CheckOneTerm(a, "zilai", "zil"); // indef. dat. fem. sing.
            CheckOneTerm(a, "zilajai", "zil"); // def. dat. fem. sing.
            CheckOneTerm(a, "zilām", "zil"); // indef. dat. fem. pl.
            CheckOneTerm(a, "zilajām", "zil"); // def. dat. fem. pl.
            CheckOneTerm(a, "zilu", "zil"); // indef. acc. masc. sing.
            CheckOneTerm(a, "zilo", "zil"); // def. acc. masc. sing.
            CheckOneTerm(a, "zilus", "zil"); // indef. acc. masc. pl.
            CheckOneTerm(a, "zilos", "zil"); // def. acc. masc. pl.
            CheckOneTerm(a, "zilu", "zil"); // indef. acc. fem. sing.
            CheckOneTerm(a, "zilo", "zil"); // def. acc. fem. sing.
            CheckOneTerm(a, "zilās", "zil"); // indef. acc. fem. pl.
            CheckOneTerm(a, "zilās", "zil"); // def. acc. fem. pl.
            CheckOneTerm(a, "zilā", "zil"); // indef. loc. masc. sing.
            CheckOneTerm(a, "zilajā", "zil"); // def. loc. masc. sing.
            CheckOneTerm(a, "zilos", "zil"); // indef. loc. masc. pl.
            CheckOneTerm(a, "zilajos", "zil"); // def. loc. masc. pl.
            CheckOneTerm(a, "zilā", "zil"); // indef. loc. fem. sing.
            CheckOneTerm(a, "zilajā", "zil"); // def. loc. fem. sing.
            CheckOneTerm(a, "zilās", "zil"); // indef. loc. fem. pl.
            CheckOneTerm(a, "zilajās", "zil"); // def. loc. fem. pl.
            CheckOneTerm(a, "zilais", "zil"); // voc. masc. sing.
            CheckOneTerm(a, "zilie", "zil"); // voc. masc. pl.
            CheckOneTerm(a, "zilā", "zil"); // voc. fem. sing.
            CheckOneTerm(a, "zilās", "zil"); // voc. fem. pl.
        }

        /// <summary>
        /// Note: we intentionally don't handle the ambiguous
        /// (s,t) -> š and (d,z) -> ž
        /// </summary>
        [Test]
        public virtual void TestPalatalization()
        {
            CheckOneTerm(a, "krāsns", "krāsn"); // nom. sing.
            CheckOneTerm(a, "krāšņu", "krāsn"); // gen. pl.
            CheckOneTerm(a, "zvaigzne", "zvaigzn"); // nom. sing.
            CheckOneTerm(a, "zvaigžņu", "zvaigzn"); // gen. pl.
            CheckOneTerm(a, "kāpslis", "kāpsl"); // nom. sing.
            CheckOneTerm(a, "kāpšļu", "kāpsl"); // gen. pl.
            CheckOneTerm(a, "zizlis", "zizl"); // nom. sing.
            CheckOneTerm(a, "zižļu", "zizl"); // gen. pl.
            CheckOneTerm(a, "vilnis", "viln"); // nom. sing.
            CheckOneTerm(a, "viļņu", "viln"); // gen. pl.
            CheckOneTerm(a, "lelle", "lell"); // nom. sing.
            CheckOneTerm(a, "leļļu", "lell"); // gen. pl.
            CheckOneTerm(a, "pinne", "pinn"); // nom. sing.
            CheckOneTerm(a, "piņņu", "pinn"); // gen. pl.
            CheckOneTerm(a, "rīkste", "rīkst"); // nom. sing.
            CheckOneTerm(a, "rīkšu", "rīkst"); // gen. pl.
        }

        /// <summary>
        /// Test some length restrictions, we require a 3+ char stem,
        /// with at least one vowel.
        /// </summary>
        [Test]
        public virtual void TestLength()
        {
            CheckOneTerm(a, "usa", "usa"); // length
            CheckOneTerm(a, "60ms", "60ms"); // vowel count
        }

        [Test]
        public virtual void TestEmptyTerm()
        {
            Analyzer a = Analyzer.NewAnonymous(createComponents: (fieldName, reader) =>
            {
                Tokenizer tokenizer = new KeywordTokenizer(reader);
                return new TokenStreamComponents(tokenizer, new LatvianStemFilter(tokenizer));
            });
            CheckOneTerm(a, "", "");
        }
    }
}