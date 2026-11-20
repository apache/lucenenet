// LUCENENET specific: Albanian is not supported by Apache Lucene 4.8; this analyzer is a .NET-only addition.
using Lucene.Net.Analysis.Util;
using NUnit.Framework;

namespace Lucene.Net.Analysis.Sq
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
    /// Test the Albanian analyzer
    /// </summary>
    public class TestAlbanianAnalyzer : BaseTokenStreamTestCase
    {
        /// <summary>
        /// This test fails with NPE when the stopwords file is missing in classpath
        /// </summary>
        [Test]
        public virtual void TestResourcesAvailable()
        {
            new AlbanianAnalyzer(TEST_VERSION_CURRENT);
        }

        [Test]
        public virtual void TestStopwords()
        {
            Analyzer a = new AlbanianAnalyzer(TEST_VERSION_CURRENT);
            AssertAnalyzesTo(a, "Çfarë është durimi?", new string[] { "dur" });
            // the stop list matches the spelling without diacritics as well
            AssertAnalyzesTo(a, "Cfare eshte durimi?", new string[] { "dur" });
        }

        [Test]
        public virtual void TestCustomStopwords()
        {
            Analyzer a = new AlbanianAnalyzer(TEST_VERSION_CURRENT, CharArraySet.Empty);
            AssertAnalyzesTo(a, "Çfarë është durimi?", new string[] { "qfar", "esht", "dur" });
        }

        /// <summary>
        /// People type ç as c or q: the three spellings of a word are one term, stop words included, and
        /// the palatal plural (miq) still meets its singular after the fold.
        /// </summary>
        [Test]
        public virtual void TestTypedSpellings()
        {
            Analyzer a = new AlbanianAnalyzer(TEST_VERSION_CURRENT);
            AssertAnalyzesTo(a, "kryeneç kryeneq kryenec", new string[] { "kryenek", "kryenek", "kryenek" });
            AssertAnalyzesTo(a, "çdo qdo cdo çka qka cka", new string[] { });
            AssertAnalyzesTo(a, "mik miq miqtë miqve", new string[] { "mik", "mik", "mik", "mik" });
            AssertAnalyzesTo(a, "Qka ke qdo dite?", new string[] { "dit" });
            // kec (a kid goat) is not keq (bad), which is a stop word: the real c-words keep their c
            AssertAnalyzesTo(a, "kec keca keq", new string[] { "kec", "kec" });
            // çfarë is a stop word; without the list its three spellings are one stem
            Analyzer noStop = new AlbanianAnalyzer(TEST_VERSION_CURRENT, CharArraySet.Empty);
            AssertAnalyzesTo(noStop, "çfarë qfare cfare", new string[] { "qfar", "qfar", "qfar" });
            // cili and cilët are stop words too; without the list their real c shows in the stem
            AssertAnalyzesTo(noStop, "cili cilët secili copë forcë", new string[] { "cil", "cil", "secil", "cop", "forc" });
        }

        [Test]
        public virtual void TestReusableTokenStream()
        {
            Analyzer a = new AlbanianAnalyzer(TEST_VERSION_CURRENT);
            AssertAnalyzesTo(a, "durimtarët", new string[] { "dur" });
            AssertAnalyzesTo(a, "durim", new string[] { "dur" });
        }

        /// <summary>
        /// Normalization runs before stemming: ë/ç folding and apostrophe removal
        /// </summary>
        [Test]
        public virtual void TestNormalization()
        {
            Analyzer a = new AlbanianAnalyzer(TEST_VERSION_CURRENT);
            AssertAnalyzesTo(a, "Kur'ani Kur’anit kuran", new string[] { "kuran", "kuran", "kuran" });
            AssertAnalyzesTo(a, "mëshirë meshire", new string[] { "meshir", "meshir" });
        }

        /// <summary>
        /// Poetry of the Rilindja period (public domain): the opening of Naim Frashëri's
        /// "Bagëti e Bujqësia" (1886) and the first stanza of Asdreni's "Himni i Flamurit"
        /// (1912), the Albanian national anthem. The elided vowels written with apostrophes
        /// (malet', q'u, dit') are handled by the normalizer.
        /// </summary>
        [Test]
        public virtual void TestRilindjaPoetry()
        {
            Analyzer a = new AlbanianAnalyzer(TEST_VERSION_CURRENT);
            AssertAnalyzesTo(a, "O malet' e Shqipërisë e ju o lisat' e gjatë! Fushat e gjera me lule, q'u kam ndër mënt dit' e natë!",
                new string[] { "o", "mal", "shqipr", "o", "lis", "fush", "gjr", "lul", "ment", "dit", "nat" });
            AssertAnalyzesTo(a, "Rreth flamurit të përbashkuar, me një dëshirë e një qëllim, të gjith' atij duke u betuar, të lidhim besën për shpëtim.",
                new string[] { "flam", "perbashk", "deshir", "qell", "bet", "lidh", "bes", "shpet" });
        }

        /// <summary>
        /// Gheg (northern) writing, public domain: the first line of Gjergj Fishta's "Lahuta e
        /// Malcís" (1937) and the best-known sentence of the Kanun of Lekë Dukagjini as
        /// codified by Shtjefën Gjeçovi (1933). The circumflex vowels and the elided clitic
        /// are normalized; the stemmer treats the Gheg endings like the standard ones.
        /// </summary>
        [Test]
        public virtual void TestGhegText()
        {
            Analyzer a = new AlbanianAnalyzer(TEST_VERSION_CURRENT);
            AssertAnalyzesTo(a, "Ndihmo, Zot, si m'ke ndihmue!",
                new string[] { "ndihm", "zot", "ndihm" });
            AssertAnalyzesTo(a, "Shpija e shqyptarit âsht e Zotit dhe e mikut.",
                new string[] { "shp", "shqyp", "zot", "mik" });
        }

        /// <summary>
        /// Everyday Gheg as spoken and written in Kosovo and northern Albania: the Gheg
        /// forms of "to be" and "to have" (âsht, kena), the dropped ë (shum, tash), the
        /// infinitive with "me" and the participle in -ue, and the elided clitics.
        /// </summary>
        [Test]
        public virtual void TestGhegEveryday()
        {
            Analyzer a = new AlbanianAnalyzer(TEST_VERSION_CURRENT);
            AssertAnalyzesTo(a, "A po vjen me shkue n'shpi?", new string[] { "shk", "shp" });
            AssertAnalyzesTo(a, "Kena punue shum sot.", new string[] { "pun", "sot" });
            AssertAnalyzesTo(a, "S'kam kohë me t'ndihmue.", new string[] { "koh", "ndihm" });
            AssertAnalyzesTo(a, "Kjo âsht shpija e nânës.", new string[] { "shp", "nan" });
            AssertAnalyzesTo(a, "Qysh je? Tash po vij.", new string[] { "vij" });
            AssertAnalyzesTo(a, "Çka ke bâ? Hiq, kurrgjâ.", new string[] { "ba", "kurrg" });
        }

        /// <summary>
        /// Legal prose: Article 1 of the Constitution of the Republic of Kosovo (2008) and
        /// Article 1 of the Universal Declaration of Human Rights in Albanian.
        /// </summary>
        [Test]
        public virtual void TestLegalText()
        {
            Analyzer a = new AlbanianAnalyzer(TEST_VERSION_CURRENT);
            AssertAnalyzesTo(a, "Republika e Kosovës është shtet i pavarur, sovran, demokratik, unik dhe i pandashëm.",
                new string[] { "republik", "kos", "shtet", "pavar", "sovran", "demokratik", "unik", "pand" });
            AssertAnalyzesTo(a, "Të gjithë njerëzit lindin të lirë dhe të barabartë në dinjitet dhe në të drejta.",
                new string[] { "njerez", "lind", "lir", "barabart", "dinjitet", "drejt" });
            AssertAnalyzesTo(a, "Ata kanë arsye dhe ndërgjegje dhe duhet të sillen ndaj njëri-tjetrit me frymë vëllazërimi.",
                new string[] { "arsy", "ndergjeg", "duh", "sill", "njr", "tjetr", "frym", "vellazer" });
        }

        /// <summary>
        /// Encyclopedic prose: sentences from the Albanian Wikipedia articles "Gjuha shqipe",
        /// "Shqipëria", "Naim Frashëri" and "Kushtetuta e Shqipërisë" (CC BY-SA).
        /// </summary>
        [Test]
        public virtual void TestEncyclopedicText()
        {
            Analyzer a = new AlbanianAnalyzer(TEST_VERSION_CURRENT);
            AssertAnalyzesTo(a, "Gjuha shqipe është gjuhë dhe degë e veçantë e familjes indo-evropiane.",
                new string[] { "gjuh", "shqip", "gjuh", "deg", "veqant", "famil", "ind", "evropian" });
            AssertAnalyzesTo(a, "Shqipja është gjuha zyrtare e Shqipërisë dhe Kosovës.",
                new string[] { "shqip", "gjuh", "zyr", "shqipr", "kos" });
            AssertAnalyzesTo(a, "Shqipëria është një shtet në Europën Juglindore.",
                new string[] { "shqipr", "shtet", "europ", "juglindor" });
            AssertAnalyzesTo(a, "Naim Frashëri ka qenë poeti më i madh i Rilindjes Kombëtare.",
                new string[] { "naim", "frashr", "poet", "madh", "rilind", "komb" });
            AssertAnalyzesTo(a, "Kjo kushtetutë përkufizon Shqipërinë si republikë parlamentare, shtet unitar e të pandashëm.",
                new string[] { "kushtetut", "perkufiz", "shqipr", "republik", "parlam", "shtet", "uni", "pand" });
            AssertAnalyzesTo(a, "Tradita e shqiptarëve për rregull dhe ligj është e vjetër.",
                new string[] { "trad", "shqip", "rreguj", "lig", "vjetr" });
            AssertAnalyzesTo(a, "Kanuni i Lekë Dukagjinit u ka shërbyer shqiptarëve për ta ruajtur vetëqeverisjen e tyre.",
                new string[] { "kan", "lek", "dukag", "sherb", "shqip", "ruajt", "veteqevr" });
            AssertAnalyzesTo(a, "Sipas Kanunit, vendime të rëndësishme merren nga kuvendet e pleqve.",
                new string[] { "kan", "vend", "rend", "merr", "kuvend", "plek" });
        }

        /// <summary>
        /// Religious prose: phrases from the Albanian Qur'an translation of Sherif Ahmeti.
        /// </summary>
        [Test]
        public virtual void TestReligiousText()
        {
            Analyzer a = new AlbanianAnalyzer(TEST_VERSION_CURRENT);
            AssertAnalyzesTo(a, "Kërkoni ndihmë me durim dhe me namaz",
                new string[] { "kerk", "ndihm", "dur", "namaz" });
            AssertAnalyzesTo(a, "Me emrin e All-llahut, Mëshiruesit, Mëshirëbërësit!",
                new string[] { "emr", "all", "llah", "meshir", "meshirebr" });
            AssertAnalyzesTo(a, "besimtarët besuan", new string[] { "bes", "bes" });
            AssertAnalyzesTo(a, "Zoti i botëve", new string[] { "zot", "bot" });
        }

        [Test]
        public virtual void TestWithStemExclusionSet()
        {
            CharArraySet set = new CharArraySet(TEST_VERSION_CURRENT, 1, true);
            set.add("durimi");
            Analyzer a = new AlbanianAnalyzer(TEST_VERSION_CURRENT, CharArraySet.Empty, set);
            AssertAnalyzesTo(a, "durimi durim", new string[] { "durimi", "dur" });
        }

        /// <summary>
        /// blast some random strings through the analyzer </summary>
        [Test]
        public virtual void TestRandomStrings()
        {
            CheckRandomData(Random, new AlbanianAnalyzer(TEST_VERSION_CURRENT), 1000 * RandomMultiplier);
        }
    }
}
