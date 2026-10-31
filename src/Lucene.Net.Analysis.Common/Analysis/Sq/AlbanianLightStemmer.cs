// LUCENENET specific: Albanian is not supported by Apache Lucene 4.8; this analyzer is a .NET-only addition.
using Lucene.Net.Analysis.Util;

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
    /// Light stemmer for Albanian.
    /// <para>
    /// A suffix-stripping stemmer in the style of the UniNE light stemmers
    /// (see <see cref="Hu.HungarianLightStemmer"/>), built on the longest-match
    /// suffix removal approach that the published Albanian stemmers use:
    /// <list type="bullet">
    ///     <item><description>Nikitas N. Karanikolas, <c>Bootstrapping the Albanian Information Retrieval</c>,
    ///     4th Balkan Conference in Informatics, 2009 (longest-match suffix removal with a
    ///     minimum remaining stem length, repeated so that a second suffix can be removed).</description></item>
    ///     <item><description>Jetmir Sadiku and Marenglen Biba, <c>A Novel Stemming Algorithm for Albanian
    ///     in a Data Mining Approach for Document Classification</c>, 2012 (JStem: rule based
    ///     suffix and prefix removal; its rules do not cover plural and gender formation).</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// The suffix inventory is the regular inflection of Standard Albanian as described in
    /// reference grammars (Newmark, Hubbard and Prifti, <c>Standard Albanian: A Reference
    /// Grammar for Students</c>, 1982) and as tabulated for several hundred lemmas in the
    /// English Wiktionary: the definite article and case endings of nouns
    /// (<c>-i -u -a -in -un -ën -it -ut -ës -së -në -t -ve -sh</c>), the regular plural endings
    /// (<c>-ë -e -a -ra</c>), the feminine adjective ending, and the endings of the regular verb
    /// classes in the active and the passive voice (present, imperfect, aorist, subjunctive,
    /// optative, participle). The Gheg (northern) forms of the same categories are covered
    /// where they differ: the participle in <c>-ue / -u / -un</c> (<c>punue</c>, <c>hapun</c>),
    /// the imperfect in <c>-ojsha</c> and the genitive in <c>-s</c> (<c>shpis</c>).
    /// Derivational suffixes that are very productive in retrieval
    /// (<c>-tar -tor -ist -ues -shëm -shme -isht -im -je</c>) are removed in a second step, so that
    /// for example <c>durim</c> (patience), <c>duroj</c> (I endure), <c>durimtar</c> (patient
    /// one) and <c>durueshëm</c> (enduring) conflate.
    /// </para>
    /// <para>
    /// Input is expected to be lower-cased and normalized by <see cref="AlbanianNormalizer"/>
    /// (<c>ë</c>→<c>e</c>, <c>ç</c>→<c>c</c>), so every suffix is listed in its folded form.
    /// Suffixes are only removed while at least three characters of stem remain. Removal is
    /// repeated until no suffix matches, which is what makes the inflected forms of a paradigm
    /// conflate even when more than one ending is stacked (<c>shtëpi / shtëpia / shtëpinë /
    /// shtëpive</c>). A final step undoes two regular stem alternations: the <c>ë</c> that is
    /// dropped before an ending (<c>libër / libri</c>, <c>i mesëm / e mesme</c>,
    /// <c>socializëm / socializmi</c>) and the palatalized plural (<c>mik / miq</c>,
    /// <c>zog / zogj</c>, <c>popull / popuj</c>). Irregular plurals with vowel changes
    /// (<c>njeri / njerëz</c>, <c>natë / net</c>), suppletive verbs (<c>shoh / pashë</c>,
    /// <c>them / tha</c>) and the admirative of consonant-stem verbs (<c>hapka</c>) are not # codespell:ignore tha
    /// handled; this is a light stemmer. Like every suffix stripper it also conflates some
    /// unrelated words whose forms differ only in a suffix or in <c>ë</c>: <c>qendër / qëndroj</c>,
    /// <c>botë / botoj</c>, <c>mes / mësoj</c>, <c>drekë / dreq</c>, <c>hekur / heq</c>,
    /// <c>beton / betoj</c>. Those are pinned in the tests. The stemmer is idempotent.
    /// </para>
    /// </summary>
    public class AlbanianLightStemmer
    {
        /// <summary>
        /// Suffixes removed by the stemmer are only removed when at least this many
        /// characters remain (Karanikolas' "at least remain letters" condition).
        /// </summary>
        private const int MINIMUM_STEM_LENGTH = 3;

        /// <summary>
        /// Inflectional endings, longest first. Nominal endings (definite article, case,
        /// number) and verbal endings (person, tense, mood, voice, participle) share one table
        /// because after folding they overlap heavily and the stemmer cannot tell parts
        /// of speech apart. Some entries need context; see <see cref="IsInflection"/>.
        /// </summary>
        private static readonly string[] INFLECTIONS =
        {
            // class 1 admirative imperfect plural (punuakëshim)
            "uakeshim", "uakeshin", "uakeshit",
            // passive imperfect plural of class 1 verbs (punoheshim); admirative imperfect singular
            "oheshim", "oheshin", "oheshit", "uakesha", "uakeshe",
            // optative plural; passive imperfect singular; passive imperative plural (punohuni);
            // admirative 2nd singular (punuakësh)
            "ofshim", "ofshin", "ofshit", "ohesha", "oheshe", "ohuni", "uakesh",
            // class 1 admirative plural (punuakemi); Gheg imperfect plural (punojshim)
            "uakemi", "uakeni", "ojshim", "ojshit", "ojshin",
            // optative singular; Gheg imperfect singular (punojsha); -uaj verbs 1st/3rd plural
            // (shkruajmë); passive present plural; -je nouns plural dative; class 2 passive
            // imperfect plural; admirative; -enj plural
            "ofsha", "ofshe", "ofshi", "ojsha", "ojshe", "uajme", "uajne", "ohemi", "oheni", "jeve", "uani",
            "uakam", "uakan", "enjve", "enjsh",
            "eshim", "eshin", "eshit", "ohesh",
            // plurals in -inj (arabaxhinj), -llarë (baballarë), -ëra (baltëra) and -ajë nouns
            // with their plural endings
            "injve", "injsh", "erave", "erash", "llare", "jave", "jash", // codespell:ignore jave
            // class 1 imperfect and 1st/3rd plural present; -ej verbs 1st/3rd plural (kthejmë);
            // optative base; passive present; class 2 passive imperfect; -je nouns plural ablative
            "onim", "onin", "onit", "onte", "ojme", "ojne", "ejme", "ejne", "shim", "shin", "shit", "ofte", "ofsh",
            "ohem", "ohet", "ohen", "ohej", "esha", "eshe", "jesh",
            // plural dative/ablative; accusative of -i nouns (shtëpinë); plural in -ëra; -ej verbs
            // aorist (ktheva) and participle (kthyer); -je nouns; -uaj verbs; class 1 aorist,
            // participle, imperfect, 2nd plural, subjunctive, optative; class 2 imperfect plural;
            // optative; participle
            "injt", "llar", "erat", "uaka", "uake", "enjt",
            "eve", "ave", "ive", "ine", "esh", "ash", "ish", "era", "eva", "yer",
            "jes", "jet", "jen", "jat", "inj", "enj",
            "uaj", "uam", "uat", "uan", "uar", "ova", "ove", "oje", "oja", "oni", "osh", "ohu",
            "nim", "nin", "nit", "oft", "sha", "she", "shi",
            // definite singular and plural, accusative, genitive/dative, plural dative;
            // class 1 present and aorist (incl. the -ov- aorist stem); participle; -je nouns;
            // passive; 2nd plural -ni and 2nd singular subjunctive -sh of consonant stems;
            // palatal plurals' definite -të; Gheg genitive -s (shpis)
            "et", "it", "ut", "at", "in", "un", "en", "es", "ve",
            "on", "oj", "oi", "ov", "ur", "je", "ja", "em", "ej", "se", "te", "sh", "ni", "t", "s",
            // indefinite plural, definite nominative, feminine, imperative of class 1 (puno)
            "i", "u", "a", "e", "o",
        };

        /// <summary>
        /// Productive derivational suffixes, longest first. Removed one at a time, each
        /// followed by another round of inflection removal.
        /// </summary>
        private static readonly string[] DERIVATIONS =
        {
            // "shm" is what is left of the feminine "-shme" once the inflection step has
            // taken its final vowel. "-ist" (artist, komunist) also covers the feminine "-iste"
            // and the imperfect of the -is verbs (arratiste), once "-e" is gone.
            "shme", "shem", "isht", "shm", "ues", "tar", "tor", "ist", "im", "je", // codespell:ignore ues
        };

        /// <summary>
        /// Stem an input buffer of Albanian text.
        /// </summary>
        /// <param name="s"> input buffer </param>
        /// <param name="len"> length of input buffer </param>
        /// <returns> length of input buffer after normalization </returns>
        public virtual int Stem(char[] s, int len)
        {
            if (len < 3) // do not touch
            {
                return len;
            }

            // Normalizing can expose another ending (mësonjëtore → mësonj → mëson → mës), so the
            // whole thing repeats until nothing changes; that also makes the stemmer idempotent.
            int before;
            do
            {
                before = len;
                if (len >= 4) // three-letter words carry no removable ending, but "miq" still normalizes
                {
                    int unstripped;
                    do
                    {
                        unstripped = len;
                        len = RemoveInflections(s, len);
                        len = RemoveLongest(s, len, DERIVATIONS, inflection: false);
                    } while (len < unstripped);
                }
                len = Normalize(s, len);
            } while (len < before);

            return len;
        }

        /// <summary>
        /// Removes inflectional endings until none matches.
        /// </summary>
        private static int RemoveInflections(char[] s, int len)
        {
            int before;
            do
            {
                before = len;
                len = RemoveLongest(s, len, INFLECTIONS, inflection: true);
            } while (len < before);
            return len;
        }

        /// <summary>
        /// Removes the first (longest) suffix of <paramref name="suffixes"/> that ends the
        /// buffer and leaves at least <see cref="MINIMUM_STEM_LENGTH"/> characters.
        /// </summary>
        private static int RemoveLongest(char[] s, int len, string[] suffixes, bool inflection)
        {
            foreach (string suffix in suffixes)
            {
                if (len - suffix.Length >= MINIMUM_STEM_LENGTH
                    && StemmerUtil.EndsWith(s, len, suffix)
                    && (inflection ? IsInflection(s, len, suffix) : IsDerivation(s, len, suffix)))
                {
                    return len - suffix.Length;
                }
            }
            return len;
        }

        /// <summary>
        /// <c>-ist</c> is stripped wherever it occurs; it is the agent suffix of loanwords
        /// (<c>artist</c>, <c>komunist</c>, <c>kapitalist</c>), and in the <c>-is</c> verbs' imperfect
        /// (<c>arratiste</c>) it is the stem plus <c>-te</c>, which the inflection step cannot separate
        /// because <c>-ste</c> is also the feminine of those nouns. The agent suffix is <c>-ëtor</c> (<c>punëtor</c>, <c>luftëtor</c>); a <c>-tor</c>
        /// after any other letter is a loanword's own ending (<c>sektor</c>, <c>motor</c>,
        /// <c>doktor</c>, <c>histori</c> → <c>histor</c>) and stays. # codespell:ignore histori
        /// </summary>
        private static bool IsDerivation(char[] s, int len, string suffix)
        {
            return suffix != "tor" || s[len - 4] == 'e';
        }

        /// <summary>
        /// Some endings are only an inflection in a given context:
        /// <list type="bullet">
        ///     <item><description><c>-et</c> after <c>-itet</c> in a long word is part of the
        ///     word (<c>dinjitet</c>, <c>universitet</c>), unless the word ends in
        ///     <c>-itetet</c>, the definite plural of such a noun.</description></item>
        ///     <item><description><c>-ve</c> after a vowel is the end of a stem
        ///     (<c>Kosovë</c>); the plural dative after a vowel is covered by
        ///     <c>-eve -ave -ive</c>.</description></item>
        ///     <item><description><c>-së</c>, and its Gheg form <c>-s</c>, is a genitive only
        ///     after <c>i</c> (<c>shtëpisë</c>, <c>shpis</c>, <c>Shqypnís</c>); elsewhere it is
        ///     stem + <c>-e</c> (<c>autobusë</c>, <c>baldosë</c>) or the word itself. In a
        ///     four-letter word the <c>s</c> is the root (<c>rris</c>, <c>abis</c>, <c>kris</c>).</description></item>
        ///     <item><description><c>-et</c>, <c>-at</c> and <c>-it</c> are not taken off a word whose
        ///     remainder would be a bare consonant cluster (<c>shtet</c>, <c>shtatë</c>, <c>shpatë</c>,
        ///     <c>zgjat</c>, <c>ngjit</c>, <c>dhjetë</c>): three characters that spell two consonants
        ///     are not a stem, and the <c>t</c> belongs to the root. Without this <c>shtet</c> (state),
        ///     <c>shtatë</c> (seven) and <c>shtoj</c> (add) share a key, as do <c>zgjat</c> (stretch)
        ///     and <c>zgjoj</c> (wake).</description></item>
        ///     <item><description><c>-ive</c> after <c>t</c>, or after <c>s</c> that does not
        ///     follow <c>e</c>, is the feminine of an <c>-iv</c> adjective (<c>aktive</c>, # codespell:ignore aktive
        ///     <c>masive</c>), not the plural dative of an <c>-i</c> noun (<c>shtëpive</c>,
        ///     <c>bashkësive</c>).</description></item>
        ///     <item><description>The class 2 imperfect plural <c>-nim -nit -nin</c>, the 2nd
        ///     person <c>-ni</c> and <c>-sh</c>, and the optative <c>-sha -she -shi -shim -shin
        ///     -shit</c> follow a consonant (<c>hapnim</c>, <c>hapni</c>, <c>hapsh</c>,
        ///     <c>hapsha</c>); after a vowel they are part of the word (<c>Kuranit</c>,
        ///     <c>zakoni</c>, <c>afishe</c>). After <c>f</c> they are either the optative's own
        ///     <c>-fsh-</c> (<c>punofsh</c>, <c>pafsha</c>) or part of a root (<c>kafshë</c>, <c>kofshë</c>,
        ///     <c>rrafsh</c>), so <c>kafshë</c> (animal) stays off <c>kafe</c>.</description></item>
        ///     <item><description><c>-ut</c> is the dative of the <c>-u</c> declension, which is limited
        ///     to stems in <c>k g h</c> and vowels (<c>mikut</c>, <c>zogut</c>, <c>krahut</c>, <c>dheut</c>);
        ///     elsewhere it is the root (<c>institut</c>, <c>kushtetutë</c>, <c>valutë</c>, <c>këput</c>,
        ///     <c>minutë</c>).</description></item>
        ///     <item><description><c>-em</c> is not removed from <c>-izëm</c> nouns; the
        ///     final step normalizes them instead.</description></item>
        ///     <item><description><c>-të</c> and <c>-t</c> are plural endings only after a
        ///     palatalized plural stem (<c>miqtë</c>, <c>zogjtë</c>, <c>popujt</c>); elsewhere
        ///     they belong to the word (<c>e drejtë</c>, <c>i shenjtë</c>).</description></item>
        ///     <item><description><c>-enj</c> is a plural after <c>m ll rr dh</c> or a vowel
        ///     (<c>lumenj</c>, <c>budallenj</c>, <c>përrenj</c>, <c>mëdhenj</c>, <c>thonj</c>);
        ///     after a single <c>r</c> it is the end of an adjective stem (<c>i shtrenjtë</c>),
        ///     which would otherwise fall together with <c>shtrat</c> and <c>shtroj</c>.</description></item>
        /// </list>
        /// </summary>
        private static bool IsInflection(char[] s, int len, string suffix)
        {
            int stemLen = len - suffix.Length;
            char before = s[stemLen - 1];

            switch (suffix)
            {
                case "et":
                    if (IsConsonantCluster(s, stemLen))
                    {
                        return false;
                    }
                    return !(len >= 8 && StemmerUtil.EndsWith(s, len, "itet")) || StemmerUtil.EndsWith(s, len, "itetet");
                case "at":
                case "it":
                    return !IsConsonantCluster(s, stemLen);
                case "ve":
                    return !IsVowel(before);
                case "se":
                    return before == 'i';
                case "s":
                    return before == 'i' && len >= 5;
                case "ive":
                    return before != 't' && !(before == 's' && s[stemLen - 2] != 'e');
                case "nim":
                case "nin":
                case "nit":
                case "ni":
                    return !IsVowel(before);
                case "sh":
                case "sha":
                case "she":
                case "shi":
                case "shim":
                case "shin":
                case "shit":
                    return !IsVowel(before) && before != 'f';
                case "ut":
                    return before == 'k' || before == 'g' || before == 'h' || IsVowel(before);
                case "em":
                    return !StemmerUtil.EndsWith(s, len, "izem");
                case "te":
                case "t":
                    return before == 'q'
                        || StemmerUtil.EndsWith(s, stemLen, "gj")
                        || StemmerUtil.EndsWith(s, stemLen, "uj");
                case "enj":
                case "enjt":
                case "enjve":
                case "enjsh":
                    return !(before == 'r' && s[stemLen - 2] != 'r');
                default:
                    return true;
            }
        }

        /// <summary>
        /// Undoes the regular stem alternations so that the uninflected form matches the
        /// stem of the inflected forms:
        /// <list type="bullet">
        ///     <item><description><c>ë</c> syncope before the article: <c>libër / libri</c>,
        ///     <c>emër / emri</c>, <c>ajër / ajri</c>, <c>arkël / arkli</c> → <c>libr emr ajr arkl</c>.
        ///     (Loanwords with a real <c>-er</c>, like <c>lider</c> or <c>hotel</c>, lose their
        ///     <c>e</c> too, consistently across their forms.)</description></item>
        ///     <item><description><c>-ëm / -me</c> adjectives and <c>-ëm</c> nouns: <c>i mesëm / e mesme</c> → <c>mes</c>,
        ///     <c>bagëm / bagmi</c> → <c>bag</c>; <c>-izëm / -izmi</c> nouns: <c>socializëm</c> → <c>socializm</c>.</description></item>
        ///     <item><description>palatalized plurals: <c>miq</c> → <c>mik</c>, <c>zogj</c> → <c>zog</c>,
        ///     <c>arbunj</c> → <c>arbun</c>; for <c>-ull</c> nouns the singular is mapped onto the
        ///     plural stem instead (<c>popull</c> → <c>popuj</c>) because the buffer cannot grow.
        ///     A bare consonant cluster ending in <c>q</c> (<c>shq-</c>, left of <c>shqisë</c> and
        ///     <c>shquar</c>) is a truncated root, not a plural, and keeps its <c>q</c>; mapping it
        ///     to <c>shk</c> would merge those words with <c>shkoj</c>.</description></item>
        /// </list>
        /// </summary>
        private static int Normalize(char[] s, int len)
        {
            // -ëm first, so that the -ër it may uncover (i afërm → afër) is syncopated too
            if (len >= 4 && s[len - 1] == 'm' && DropsFinalM(s[len - 2]))
            {
                len--;
            }

            if (len >= 4 && (StemmerUtil.EndsWith(s, len, "er") || StemmerUtil.EndsWith(s, len, "el")) && TakesSyncope(s[len - 3]))
            {
                s[len - 2] = s[len - 1];
                len--;
            }
            else if (len >= 5 && StemmerUtil.EndsWith(s, len, "err") && TakesSyncope(s[len - 4]))
            {
                s[len - 3] = 'r';
                len--;
            }

            if (len >= 6 && StemmerUtil.EndsWith(s, len, "izem"))
            {
                s[len - 2] = 'm';
                len--;
            }

            if (len >= 4 && (StemmerUtil.EndsWith(s, len, "gj") || StemmerUtil.EndsWith(s, len, "nj")))
            {
                len--;
            }
            else if (len >= 3 && s[len - 1] == 'q' && !IsConsonantCluster(s, len))
            {
                s[len - 1] = 'k';
            }
            else if (len >= 5 && StemmerUtil.EndsWith(s, len, "ull"))
            {
                s[len - 3] = 'u';
                s[len - 2] = 'j';
                len--;
            }

            return len;
        }

        /// <summary>
        /// The consonants after which a final <c>-ër</c> / <c>-ël</c> drops its <c>ë</c> before
        /// an ending (<c>libër, qendër, gjarpër, emër, motër, ajër, arkël, vegjël</c>). After
        /// <c>z r l n s</c> a final <c>-er</c> is a root (<c>vëllazëri, sheqer</c>) and is left
        /// alone.
        /// </summary>
        private static bool TakesSyncope(char c)
        {
            switch (c)
            {
                case 'b':
                case 'd':
                case 'f':
                case 'g':
                case 'h':
                case 'j':
                case 'k':
                case 'm':
                case 'p':
                case 't':
                case 'v':
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// The consonants after which a final <c>m</c> is the <c>-ëm</c> of an adjective or
        /// noun whose <c>ë</c> has been dropped (<c>mesëm/mesme, sotëm, bagëm/bagmi, film/filmi</c>).
        /// <c>z</c> is excluded so that <c>-izëm</c> nouns keep their <c>m</c>.
        /// </summary>
        private static bool DropsFinalM(char c)
        {
            switch (c)
            {
                case 'b':
                case 'd':
                case 'g':
                case 'k':
                case 'l':
                case 'n':
                case 'p':
                case 'r':
                case 's':
                case 't':
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Three characters that spell a digraph and a consonant (<c>sht shp shq dhj</c>) or a consonant
        /// and <c>gj</c> (<c>zgj ngj</c>) are a consonant cluster, not a stem. A vowel and a digraph
        /// (<c>udh</c>, <c>arr</c>, <c>eth</c>) is a real root, and so is a consonant and <c>sh</c>
        /// (<c>fsh-</c>), which goes on in derivations (<c>fshat</c>, <c>fshatar</c>).
        /// </summary>
        private static bool IsConsonantCluster(char[] s, int len)
        {
            if (len != 3)
            {
                return false;
            }
            if (IsDigraph(s[0], s[1]))
            {
                return !IsVowel(s[2]);
            }
            return s[1] == 'g' && s[2] == 'j' && !IsVowel(s[0]);
        }

        /// <summary>
        /// The consonant digraphs of the Albanian alphabet, which count as one letter.
        /// </summary>
        private static bool IsDigraph(char a, char b)
        {
            switch (b)
            {
                case 'h':
                    return a == 'd' || a == 's' || a == 't' || a == 'x' || a == 'z';
                case 'j':
                    return a == 'g' || a == 'n';
                case 'l':
                    return a == 'l';
                case 'r':
                    return a == 'r';
                default:
                    return false;
            }
        }

        private static bool IsVowel(char c)
        {
            switch (c)
            {
                case 'a':
                case 'e':
                case 'i':
                case 'o':
                case 'u':
                case 'y':
                    return true;
                default:
                    return false;
            }
        }
    }
}
