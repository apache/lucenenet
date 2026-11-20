// LUCENENET specific: Albanian is not supported by Apache Lucene 4.8; this analyzer is a .NET-only addition.
using Lucene.Net.Analysis.Core;
using Lucene.Net.Analysis.Miscellaneous;
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
    /// Tests for <see cref="AlbanianLightStemFilter"/>. Inputs are given the way the filter
    /// sees them inside <see cref="AlbanianAnalyzer"/>: lower-cased and with ë/ç already
    /// folded. The paradigms follow the declension and conjugation tables of the English
    /// Wiktionary for the words in question.
    /// </summary>
    public class TestAlbanianLightStemFilter : BaseTokenStreamTestCase
    {
        private static readonly Analyzer analyzer = Analyzer.NewAnonymous(createComponents: (fieldName, reader) =>
        {
            Tokenizer source = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
            return new TokenStreamComponents(source, new AlbanianLightStemFilter(source));
        });

        private static void Check(string stem, params string[] forms)
        {
            foreach (string form in forms)
            {
                CheckOneTerm(analyzer, form, stem);
            }
        }

        /// <summary>
        /// Regression vocabulary: every inflected form of the Albanian lemmas that carry a
        /// declension or conjugation table in the English Wiktionary, plus the vocabulary of
        /// a set of Albanian Wikipedia articles and of a Qur'an translation, each with the
        /// stem this stemmer produced when the file was generated. It pins the behaviour of
        /// the rules on tens of thousands of real words.
        /// </summary>
        [Test]
        public virtual void TestVocabulary()
        {
            VocabularyAssert.AssertVocabulary(analyzer, GetDataFile("sqlighttestdata.zip"), "sqlight.txt");
        }

        /// <summary>
        /// Masculine nouns: indefinite, definite nominative, accusative, dative,
        /// indefinite plural, definite plural, plural dative, plural ablative.
        /// </summary>
        [Test]
        public virtual void TestMasculineNouns()
        {
            Check("mal", "mal", "mali", "malin", "malit", "male", "malet", "maleve", "malesh");
            Check("det", "det", "deti", "detin", "detit", "dete", "detet", "deteve", "detesh");
            Check("vend", "vend", "vendi", "vendin", "vendit", "vende", "vendet", "vendeve", "vendesh");
            // "u" type (stem ends in h, k, g or a stressed vowel)
            Check("allah", "allah", "allahu", "allahun", "allahut");
            Check("zot", "zot", "zoti", "zotit", "zotin");
            // nouns in -im
            Check("bes", "besim", "besimi", "besimin", "besimit", "besime", "besimet", "besimeve", "besimesh");
        }

        /// <summary>
        /// Feminine nouns in -ë, -e, -i and -je.
        /// </summary>
        [Test]
        public virtual void TestFeminineNouns()
        {
            Check("vajz", "vajze", "vajza", "vajzen", "vajzes", "vajzat", "vajzave", "vajzash");
            Check("pun", "pune", "puna", "punen", "punes", "punet", "puneve", "punesh");
            Check("gjuh", "gjuhe", "gjuha", "gjuhen", "gjuhes", "gjuhet", "gjuheve", "gjuhesh");
            Check("shkoll", "shkolle", "shkolla", "shkollen", "shkolles", "shkollat", "shkollave", "shkollash");
            Check("rrug", "rruge", "rruga", "rrugen", "rruges", "rruget", "rrugeve");
            Check("dit", "dite", "dita", "diten", "dites", "ditet", "diteve", "ditesh");
            Check("lul", "lule", "lulja", "lulen", "lules", "lulet", "luleve", "lulesh");
            // -i nouns: the stacked endings all conflate
            Check("shtep", "shtepi", "shtepia", "shtepine", "shtepise", "shtepite", "shtepive", "shtepish");
            Check("lir", "liri", "liria", "lirine", "lirise", "lirite", "lirive", "lirish");
            Check("shoqer", "shoqeri", "shoqeria", "shoqerine", "shoqerise", "shoqerite", "shoqerive");
            // -je nouns
            Check("lut", "lutje", "lutja", "lutjen", "lutjes", "lutjet", "lutjeve", "lutjesh");
            Check("fem", "femije", "femija", "femijen", "femijes", "femijet", "femijeve", "femijesh");
        }

        /// <summary>
        /// Nouns whose final ë drops before an ending (libër / libri) get the same stem in
        /// both shapes.
        /// </summary>
        [Test]
        public virtual void TestSyncope()
        {
            Check("libr", "liber", "libri", "librin", "librit", "libra", "librat", "librave", "librash");
            Check("emr", "emer", "emri", "emrin", "emrit", "emra", "emrat", "emrave");
            Check("zemr", "zemer", "zemra", "zemren", "zemres", "zemrat", "zemrave");
            Check("qendr", "qender", "qendra", "qendren", "qendres", "qendrat");
            Check("motr", "moter", "motra", "motren", "motres", "motrat");
            Check("dimr", "dimer", "dimri", "dimrin", "dimrit");
            Check("numr", "numer", "numri", "numrin", "numrat");
            Check("vjetr", "vjeter", "vjetra", "vjetrit", "vjetrat");
            Check("teatr", "teater", "teatri", "teatrin", "teatrit");
            Check("ministr", "minister", "ministri", "ministrin", "ministrat");
            Check("ajr", "ajer", "ajri", "ajrin", "ajrit");
            Check("afr", "afer", "aferm", "aferme", "afermit");
            Check("cakrr", "cakerr", "cakrri", "cakrrin");
            Check("arkl", "arkel", "arkli", "arklin", "arkla");
            Check("vegjl", "vegjel", "vegjlit", "vegjla");
            // roots in -er after z, r, l, n, s are left alone
            Check("sheqer", "sheqer", "sheqeri", "sheqerin");
            Check("vellazer", "vellazeri", "vellazerim", "vellazerimi");
        }

        /// <summary>
        /// The less common plural classes: -inj (arabaxhi / arabaxhinj), -llarë (baba /
        /// baballarë), -ëra (baltë / baltëra), and nouns in -ajë / -onjë whose j is part of
        /// the stem.
        /// </summary>
        [Test]
        public virtual void TestOtherPlurals()
        {
            Check("arabaxh", "arabaxhi", "arabaxhinj", "arabaxhinjt", "arabaxhinjve", "arabaxhinjsh");
            Check("bab", "baba", "babai", "baballare", "baballaret", "baballareve");
            Check("balt", "balte", "balta", "balten", "baltera", "balterat", "balterave");
            Check("zon", "zonje", "zonja", "zonjen", "zonjes", "zonjat", "zonjave");
            Check("gjun", "gjunje", "gjunjet");
            // -enj plural (lumë / lumenj)
            Check("lum", "lume", "lumi", "lumin", "lumenj", "lumenjte", "lumenjve", "lumenjsh");
        }

        /// <summary>
        /// Loanwords in -tor keep it: only the native agent suffix -ëtor is derivational.
        /// </summary>
        [Test]
        public virtual void TestTorLoanwords()
        {
            Check("histor", "histori", "historia", "historine", "historise");
            Check("historik", "historik", "historike", "historiket");
            Check("sektor", "sektor", "sektori", "sektorin", "sektoret");
            Check("motor", "motor", "motori", "motoret");
            Check("doktor", "doktor", "doktori", "doktoret");
            Check("pun", "punetor", "punetore", "punetoret");
            Check("luft", "luftetor", "luftetoret", "lufta", "luften");
        }

        /// <summary>
        /// Stemming twice gives the same result as stemming once.
        /// </summary>
        [Test]
        public virtual void TestIdempotent()
        {
            foreach (string word in new[] { "mesonjetoret", "afermit", "siperme", "lumenjve", "arbunjve", "medhenj", "biznesmeneve", "nepermjet", "popujve", "miqte" })
            {
                string once = Stem(word);
                CheckOneTerm(analyzer, once, once);
            }
        }

        private static string Stem(string word)
        {
            char[] buffer = word.ToCharArray();
            int len = new AlbanianLightStemmer().Stem(buffer, buffer.Length);
            return new string(buffer, 0, len);
        }

        /// <summary>
        /// Adjectives in -iv keep their stem in the feminine (aktiv / aktive); -i nouns
        /// still lose their plural dative -ive (bashkësi / bashkësive).
        /// </summary>
        [Test]
        public virtual void TestIvAdjectives()
        {
            Check("aktiv", "aktiv", "aktive", "aktivet", "aktivit");
            Check("masiv", "masiv", "masive");
            Check("pasiv", "pasiv", "pasive");
            Check("intensiv", "intensiv", "intensive");
            Check("bashk", "bashkesi", "bashkesia", "bashkesive");
            Check("njes", "njesi", "njesia", "njesive");
            Check("ushtr", "ushtri", "ushtria", "ushtrive");
        }

        /// <summary>
        /// Nouns in -ëm (bagëm / bagmi) and loanwords in consonant + m (film / filmi).
        /// </summary>
        [Test]
        public virtual void TestEmNouns()
        {
            Check("bag", "bagem", "bagmi", "bagmit");
            Check("fil", "film", "filmi", "filmat");
            Check("rit", "ritem", "ritmi", "ritmin");
        }

        /// <summary>
        /// -së is a genitive only after i; after a consonant it is stem + ë.
        /// </summary>
        [Test]
        public virtual void TestSeEnding()
        {
            Check("shtep", "shtepise");
            Check("lir", "lirise");
            Check("autobus", "autobus", "autobusi", "autobuse", "autobusat");
            Check("baldos", "baldose", "baldosa");
            Check("ananas", "ananas", "ananasi", "ananase");
            // the Gheg genitive -s needs a word of five letters; in rris and abis the s is the root
            Check("rris", "rris");
            Check("abis", "abis", "abisi");
            Check("lir", "liris", "liri");
        }

        /// <summary>
        /// Plurals formed by palatalization (k → q, g → gj, ll → j) conflate with the
        /// singular, including their definite -të / -t and ablative -sh.
        /// </summary>
        [Test]
        public virtual void TestPalatalPlurals()
        {
            Check("mik", "mik", "miku", "mikun", "mikut", "miq", "miqte", "miqt", "miqve", "miqsh");
            Check("zog", "zog", "zogu", "zogun", "zogut", "zogj", "zogjte", "zogjve", "zogjsh");
            Check("popuj", "popull", "populli", "popullin", "popullit", "popuj", "popujt", "popujve", "popujsh");
            Check("shok", "shok", "shoku", "shokun", "shoqe", "shoqja", "shoqen");
            // the endings -të -t -sh are not touched elsewhere
            Check("drejt", "drejt", "drejte", "drejta");
            Check("shenjt", "shenjte", "shenjta");
            Check("lart", "lart", "larte", "larta");
            // consonant + q is palatalized too (ujk / ujq, peshk / peshq)
            Check("ujk", "ujk", "ujku", "ujq", "ujqit", "ujqve");
            Check("peshk", "peshk", "peshku", "peshq", "peshqit");
        }

        /// <summary>
        /// Three characters that spell a digraph and a consonant (sht, shp, shq, zgj, ngj) are a bare
        /// consonant cluster, not a stem: -et, -at and -it are not taken down to one, and a final q in
        /// one is not a palatal plural. This keeps shtet (state), shtatë (seven) and shtoj (add) apart,
        /// shpatë (sword) and shpëtim (rescue) off the Gheg shpi (house), zgjat (stretch) off zgjoj
        /// (wake), ngjit (climb) off ngjaj (resemble), and shqetësoj (worry) off shkoj (go).
        /// </summary>
        [Test]
        public virtual void TestConsonantClusters()
        {
            Check("shtet", "shtet", "shteti", "shtetin", "shtetet", "shteteve");
            Check("shtat", "shtate", "shtatit");
            Check("sht", "shtoj", "shtohet");
            Check("shpat", "shpate", "shpata");
            Check("shpet", "shpetim", "shpetoj", "shpetuar");
            Check("shp", "shpi", "shpija");
            Check("zgjat", "zgjat", "zgjatet", "zgjatur", "zgjatje");
            Check("zgj", "zgjoj", "zgjohem", "zgjim");
            Check("ngjit", "ngjit", "ngjitet", "ngjitur", "ngjitje");
            Check("ngj", "ngjashem");
            Check("dhjet", "dhjete", "dhjeta");
            Check("shqet", "shqetesim", "shqetesimi", "shqetesoj", "shqetesuar", "shqetesohet");
            Check("shq", "shquar", "shqisa");
            Check("shk", "shkoj", "shkoi", "shkuar");
            // a vowel and a digraph is a root, and keeps its endings coming off
            Check("udh", "udhe", "udha", "udhat");
            Check("arr", "arre", "arrat");
            // so is a consonant and sh, which goes on in derivations
            Check("fsh", "fshat", "fshati", "fshatar", "fshatare");
        }

        /// <summary>
        /// -ut belongs to the -u declension (stems in k, g, h or a vowel); elsewhere it is the root, so
        /// kushtetutë (constitution) stays off kusht (condition), valutë (currency) off valë (wave), and
        /// këput (tear off) off qep (sew).
        /// </summary>
        [Test]
        public virtual void TestUtEndings()
        {
            Check("mik", "mik", "miku", "mikut");
            Check("zog", "zog", "zogu", "zogut");
            Check("allah", "allah", "allahu", "allahut");
            Check("dhe", "dhe", "dheu", "dheut");
            Check("kushtetut", "kushtetute", "kushtetuta", "kushtetutes");
            Check("kusht", "kusht", "kushti", "kushtet");
            Check("institut", "institut", "instituti", "institutin");
            Check("valut", "valute", "valuta", "valutes");
            Check("val", "vale", "vala", "valet");
            Check("keput", "keput", "keputur", "keputje");
            Check("minut", "minute", "minuta", "minutat");
            Check("hajdut", "hajdut", "hajduti", "hajdutet");
        }

        /// <summary>
        /// After f the -sh endings are the optative's own -fsh- (punofsh, pafsha) or part of a root, so
        /// kafshë (animal) keeps its sh and stays off kafe (coffee); kofshë (thigh) and rrafsh (plain)
        /// likewise.
        /// </summary>
        [Test]
        public virtual void TestShAfterF()
        {
            Check("kafsh", "kafshe", "kafsha", "kafshet", "kafshoj", "kafshim");
            Check("kaf", "kafe", "kafeja");
            Check("kofsh", "kofshe", "kofsha", "kofshet");
            Check("rrafsh", "rrafsh", "rrafshi", "rrafshoj");
            Check("pun", "punofsh", "punofsha", "punoj");
            Check("hap", "hapsh", "hapsha", "hapshim", "hapim");
        }

        /// <summary>
        /// Verbs in -ej (kthej, shërbej, përkthej): present plural, aorist, participle and the nouns
        /// derived from them conflate.
        /// </summary>
        [Test]
        public virtual void TestEjVerbs()
        {
            Check("kth", "kthej", "kthen", "kthejme", "kthejne", "ktheva", "ktheu", "kthyer", "kthim");
            Check("sherb", "sherbej", "sherben", "sherbejme", "sherbeva", "sherbyer", "sherbim", "sherbetor");
            Check("perkth", "perkthej", "perkthyer", "perkthim");
        }

        /// <summary>
        /// -ist is derivational: the agent noun joins its base (artist / art, kapitalist / kapital,
        /// aktivist / aktiv), the feminine -iste joins the masculine, and the -is verbs' imperfect
        /// (braktiste) joins their paradigm.
        /// </summary>
        [Test]
        public virtual void TestIstNouns()
        {
            Check("art", "art", "arti", "artist", "artiste", "artistet");
            Check("kapital", "kapital", "kapitali", "kapitalist", "kapitalistet");
            Check("aktiv", "aktiv", "aktive", "aktivist", "aktiviste");
            Check("brakt", "braktis", "braktiste", "braktisur", "braktisje");
        }

        /// <summary>
        /// -enj is a plural only after m, ll, rr, dh or a vowel; after a single r it is part of an
        /// adjective stem, so i shtrenjtë (expensive) stays apart from shtrat (bed) and shtroj (lay).
        /// </summary>
        [Test]
        public virtual void TestEnjPlurals()
        {
            Check("shtrenjt", "shtrenjte", "shtrenjta", "shtrenjtit");
            Check("shtr", "shtrat", "shtrati", "shtroj", "shtrese");
            Check("perr", "perrua", "perrenj", "perrenjte");
        }

        /// <summary>
        /// Conflations of unrelated words that a suffix stripper cannot avoid, pinned so that a
        /// change in the rules shows up here: the pairs differ only in ë, which the normalizer folds
        /// (qendër / qëndroj, mes / mësoj, derë / dërmoj), or one word ends like an inflection of the
        /// other (botë / botoj, hekur / heq, beton / betoj, veri / verë), or the palatal plural rule
        /// meets an original q (drekë / dreq).
        /// </summary>
        [Test]
        public virtual void TestKnownConflations()
        {
            Check("qendr", "qender", "qendra", "qendroj", "qendrim");
            Check("mes", "mes", "mesoj", "mesim");
            Check("der", "dere", "dermoj");
            Check("bot", "bote", "botoj", "botim");
            Check("hek", "hekur", "heq");
            Check("bet", "beton", "betoj", "beteje");
            Check("ver", "veri", "vere");
            Check("drek", "dreke", "dreq");
        }

        /// <summary>
        /// Nouns whose stem ends in -itet (dinjitet, universitet) keep it; only the definite
        /// plural -itetet loses an ending. Shorter words in -tet are stemmed like any other
        /// (qytet → qyt), which keeps their paradigm together.
        /// </summary>
        [Test]
        public virtual void TestItetNouns()
        {
            Check("dinjitet", "dinjitet", "dinjiteti", "dinjitetin", "dinjitetit");
            Check("universitet", "universitet", "universiteti", "universitetin", "universitetet", "universiteteve");
            // the universe keeps a key of its own; the adjective universitar loses its -tar and joins it
            Check("univers", "univers", "universi", "universin", "universitar");
            Check("qyt", "qytet", "qyteti", "qytetin", "qytetit", "qytete", "qytetet", "qyteteve");
            Check("shtet", "shtet", "shteti", "shtetin", "shtetet", "shteteve");
            Check("vit", "vit", "viti", "vitin", "vitit", "vite", "vitet", "viteve");
            Check("prit", "pritet", "pritur", "prita");
        }

        /// <summary>
        /// Place names in -ë/-a and -i/-ia conflate across their cases, including Kosovë /
        /// Kosova whose stem happens to end like an aorist.
        /// </summary>
        [Test]
        public virtual void TestPlaceNames()
        {
            Check("kos", "kosove", "kosova", "kosoven", "kosoves");
            Check("kosovar", "kosovar", "kosovare", "kosovaret", "kosovareve");
            Check("shqipr", "shqiperi", "shqiperia", "shqiperine", "shqiperise");
            Check("tiran", "tirane", "tirana", "tiranen", "tiranes");
            Check("prisht", "prishtine", "prishtina", "prishtinen", "prishtines");
        }

        /// <summary>
        /// Adjectives: -an/-ane, -antë, -ëm/-me, -shëm/-shme, -ur/-ura.
        /// </summary>
        [Test]
        public virtual void TestAdjectives()
        {
            Check("evropian", "evropian", "evropiane", "evropianet");
            Check("vecant", "vecante", "vecanta");
            Check("mes", "mesem", "mesme", "mesmja");
            Check("sot", "sot", "sotem", "sotme");
            Check("lasht", "lashte", "lashtem", "lashta");
            Check("dur", "durueshem", "durueshme");
            Check("buk", "bukur", "bukura", "bukurat");
        }

        /// <summary>
        /// Nouns in -izëm drop the ë before the article (socializëm / socializmi).
        /// </summary>
        [Test]
        public virtual void TestIzemNouns()
        {
            Check("socializm", "socializem", "socializmi", "socializmin", "socializmit");
            Check("organizm", "organizem", "organizmi", "organizmat", "organizmave");
        }

        /// <summary>
        /// Class 1 verbs (-oj): present, imperfect, aorist, participle, imperative,
        /// subjunctive and optative.
        /// </summary>
        [Test]
        public virtual void TestClassOneVerbs()
        {
            Check("pun", "punoj", "punon", "punojme", "punoni", "punojne",
                "punoja", "punoje", "punonte", "punonim", "punonit", "punonin",
                "punova", "punove", "punoi", "punuam", "punuat", "punuan",
                "punuar", "puno", "punosh", "punofsha", "punofte", "punofshim",
                "punuaka", "punuakam", "punuakan", "punuake", "punuakemi", "punuakeni",
                "punuakesh", "punuakesha", "punuakeshe", "punuakeshim", "punuakeshin", "punuakeshit");
            Check("mes", "mesoj", "meson", "mesojme", "mesuan", "mesuar", "meso", "mesosh");
            Check("shk", "shkoj", "shkon", "shkova", "shkoi", "shkuan", "shkuar");
        }

        /// <summary>
        /// Class 1 verbs in -uaj (shkruaj, write) and the nouns derived from them.
        /// </summary>
        [Test]
        public virtual void TestUajVerbs()
        {
            Check("shkr", "shkruaj", "shkruan", "shkruajme", "shkruani", "shkruajne",
                "shkrova", "shkroi", "shkruar", "shkrim", "shkrimi", "shkrimet");
        }

        /// <summary>
        /// Class 2 verbs (consonant stems): present, imperfect plural, aorist, participle,
        /// passive present and imperfect, optative.
        /// </summary>
        [Test]
        public virtual void TestClassTwoVerbs()
        {
            Check("hap", "hap", "hapim", "hapin", "hapni", "hapsh", "hapnim", "hapnit", "hapnin",
                "hapa", "hape", "hapi", "hapur",
                "hapem", "hapesh", "hapet", "hapemi", "hapeni", "hapen",
                "hapej", "hapesha", "hapeshe", "hapeshim", "hapeshin", "hapeshit",
                "hapja", "hapje", "hapsha", "hapshim");
        }

        /// <summary>
        /// Passive voice of class 1 verbs (gëzohem, I rejoice) conflates with the active.
        /// </summary>
        [Test]
        public virtual void TestPassive()
        {
            Check("gez", "gezoj", "gezon", "gezuar",
                "gezohem", "gezohesh", "gezohet", "gezohemi", "gezoheni", "gezohen",
                "gezohej", "gezohesha", "gezoheshe", "gezoheshim", "gezoheshin", "gezoheshit",
                "gezohu", "gezohuni");
            Check("pun", "punohet", "punohen", "punohej");
            Check("lex", "lexoj", "lexo", "lexohet", "lexohen");
        }

        /// <summary>
        /// Derivation: the noun, the verb, the agent noun and the adjective of one root
        /// conflate.
        /// </summary>
        [Test]
        public virtual void TestDerivation()
        {
            // durim (patience), duroj (endure), durimtar (patient one), durueshëm (enduring)
            Check("dur", "durim", "durimi", "durimin", "durimtar", "durimtaret", "durimtareve",
                "duroj", "duroni", "duruan", "durueshem", "durueshme");
            // besim (faith), besoj (believe), besimtar (believer)
            Check("bes", "besim", "besimi", "besimtar", "besimtaret", "beson", "besoj", "besuan", "besuar");
            // punë (work), punëtor (worker), punoj (work)
            Check("pun", "pune", "punet", "punetor", "punetori", "punetoret", "punoj");
            // mëshirë (mercy), mëshirues (merciful), mëshiron (has mercy)
            Check("meshir", "meshire", "meshira", "meshirues", "meshiruesit", "meshiron");
            // mësoj (learn/teach), mësim (lesson), mësues (teacher)
            Check("mes", "mesim", "mesimi", "mesimet", "mesues", "mesuesi", "mesuesit", "mesuesve");
            // shqip (Albanian language), shqiptar (Albanian person)
            Check("shqip", "shqip", "shqipe", "shqipja", "shqiptar", "shqiptari", "shqiptare", "shqiptaret", "shqiptareve");
            // drejt (straight), e drejta (the right), drejtësi (justice)
            Check("drejt", "drejt", "drejte", "drejta", "drejtesi", "drejtesia");
            // lirë (free), liri (freedom), lirisht (freely)
            Check("lir", "lire", "liri", "liria", "lirisht");
        }

        /// <summary>
        /// Gheg verb forms: the participle in -ue / -u / -un / -uem, the imperfect in -ojsha,
        /// and consonant stems in -un, all conflate with the standard forms of the verb.
        /// </summary>
        [Test]
        public virtual void TestGhegVerbs()
        {
            Check("pun", "punue", "punu", "punun", "punuem",
                "punojsha", "punojshe", "punojshim", "punojshit", "punojshin",
                "punoj", "punuar", "punova");
            Check("shk", "shkue", "shkoj", "shkova", "shkuar");
            Check("hap", "hapun", "hapur", "hapsha", "hapshe");
            Check("hangr", "hanger", "hangrun", "hangri");
            Check("kqyr", "kqyr", "kqyrun", "kqyri");
            Check("ardh", "ardhe", "ardhun", "ardhur");
            Check("dal", "dale", "dalun", "dalur");
        }

        /// <summary>
        /// Gheg nouns: the genitive in -s after i (shpis, Shqypnís) joins the paradigm.
        /// </summary>
        [Test]
        public virtual void TestGhegNouns()
        {
            Check("shp", "shpi", "shpija", "shpin", "shpis", "shpive");
            Check("shqyp", "shqypni", "shqypnia", "shqypnis");
            Check("nan", "nane", "nana", "nanes", "nanen");
            Check("shqyp", "shqyptar", "shqyptari", "shqyptarit", "shqyptarve");
            Check("lir", "liri", "liris", "liria", "lirise");
        }

        /// <summary>
        /// Loanwords in -o behave like class 1 imperatives and conflate with their
        /// inflected forms.
        /// </summary>
        [Test]
        public virtual void TestWordsInO()
        {
            Check("rad", "radio", "radioja", "radion");
            Check("fot", "foto", "fotoja", "foton");
        }

        /// <summary>
        /// Words of three letters or fewer, and words that would be cut below three
        /// letters, are left alone (apart from a final q, which is a palatal plural).
        /// </summary>
        [Test]
        public virtual void TestShortWords()
        {
            Check("dhe", "dhe");
            Check("uje", "uje");
            Check("ata", "ata");
            Check("ne", "ne");
            Check("mik", "miq");
        }

        [Test]
        public virtual void TestKeyword()
        {
            CharArraySet exclusionSet = new CharArraySet(TEST_VERSION_CURRENT, AsSet("durimi"), false);
            Analyzer a = Analyzer.NewAnonymous(createComponents: (fieldName, reader) =>
            {
                Tokenizer source = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
                TokenStream sink = new SetKeywordMarkerFilter(source, exclusionSet);
                return new TokenStreamComponents(source, new AlbanianLightStemFilter(sink));
            });
            CheckOneTerm(a, "durimi", "durimi");
        }

        [Test]
        public virtual void TestEmptyTerm()
        {
            Analyzer a = Analyzer.NewAnonymous(createComponents: (fieldName, reader) =>
            {
                Tokenizer tokenizer = new KeywordTokenizer(reader);
                return new TokenStreamComponents(tokenizer, new AlbanianLightStemFilter(tokenizer));
            });
            CheckOneTerm(a, "", "");
        }
    }
}
