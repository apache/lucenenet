// Lucene version compatibility level 4.8.1
using Lucene.Net.Analysis.Core;
using Lucene.Net.Analysis.Miscellaneous;
using Lucene.Net.Analysis.Util;
using NUnit.Framework;
using System.IO;

namespace Lucene.Net.Analysis.Cz
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
    /// Test the Czech Stemmer.
    /// 
    /// Note: its algorithmic, so some stems are nonsense
    /// 
    /// </summary>
    public class TestCzechStemmer : BaseTokenStreamTestCase
    {

        /// <summary>
        /// Test showing how masculine noun forms conflate
        /// </summary>
        [Test]
        public virtual void TestMasculineNouns()
        {
            CzechAnalyzer cz = new CzechAnalyzer(TEST_VERSION_CURRENT);

            /* animate ending with a hard consonant */
            AssertAnalyzesTo(cz, "pán", new string[] { "pán" });
            AssertAnalyzesTo(cz, "páni", new string[] { "pán" });
            AssertAnalyzesTo(cz, "pánové", new string[] { "pán" });
            AssertAnalyzesTo(cz, "pána", new string[] { "pán" });
            AssertAnalyzesTo(cz, "pánů", new string[] { "pán" });
            AssertAnalyzesTo(cz, "pánovi", new string[] { "pán" });
            AssertAnalyzesTo(cz, "pánům", new string[] { "pán" });
            AssertAnalyzesTo(cz, "pány", new string[] { "pán" });
            AssertAnalyzesTo(cz, "páne", new string[] { "pán" });
            AssertAnalyzesTo(cz, "pánech", new string[] { "pán" });
            AssertAnalyzesTo(cz, "pánem", new string[] { "pán" });

            /* inanimate ending with hard consonant */
            AssertAnalyzesTo(cz, "hrad", new string[] { "hrad" });
            AssertAnalyzesTo(cz, "hradu", new string[] { "hrad" });
            AssertAnalyzesTo(cz, "hrade", new string[] { "hrad" });
            AssertAnalyzesTo(cz, "hradem", new string[] { "hrad" });
            AssertAnalyzesTo(cz, "hrady", new string[] { "hrad" });
            AssertAnalyzesTo(cz, "hradech", new string[] { "hrad" });
            AssertAnalyzesTo(cz, "hradům", new string[] { "hrad" });
            AssertAnalyzesTo(cz, "hradů", new string[] { "hrad" });

            /* animate ending with a soft consonant */
            AssertAnalyzesTo(cz, "muž", new string[] { "muh" });
            AssertAnalyzesTo(cz, "muži", new string[] { "muh" });
            AssertAnalyzesTo(cz, "muže", new string[] { "muh" });
            AssertAnalyzesTo(cz, "mužů", new string[] { "muh" });
            AssertAnalyzesTo(cz, "mužům", new string[] { "muh" });
            AssertAnalyzesTo(cz, "mužích", new string[] { "muh" });
            AssertAnalyzesTo(cz, "mužem", new string[] { "muh" });

            /* inanimate ending with a soft consonant */
            AssertAnalyzesTo(cz, "stroj", new string[] { "stroj" });
            AssertAnalyzesTo(cz, "stroje", new string[] { "stroj" });
            AssertAnalyzesTo(cz, "strojů", new string[] { "stroj" });
            AssertAnalyzesTo(cz, "stroji", new string[] { "stroj" });
            AssertAnalyzesTo(cz, "strojům", new string[] { "stroj" });
            AssertAnalyzesTo(cz, "strojích", new string[] { "stroj" });
            AssertAnalyzesTo(cz, "strojem", new string[] { "stroj" });

            /* ending with a */
            AssertAnalyzesTo(cz, "předseda", new string[] { "předsd" });
            AssertAnalyzesTo(cz, "předsedové", new string[] { "předsd" });
            AssertAnalyzesTo(cz, "předsedy", new string[] { "předsd" });
            AssertAnalyzesTo(cz, "předsedů", new string[] { "předsd" });
            AssertAnalyzesTo(cz, "předsedovi", new string[] { "předsd" });
            AssertAnalyzesTo(cz, "předsedům", new string[] { "předsd" });
            AssertAnalyzesTo(cz, "předsedu", new string[] { "předsd" });
            AssertAnalyzesTo(cz, "předsedo", new string[] { "předsd" });
            AssertAnalyzesTo(cz, "předsedech", new string[] { "předsd" });
            AssertAnalyzesTo(cz, "předsedou", new string[] { "předsd" });

            /* ending with e */
            AssertAnalyzesTo(cz, "soudce", new string[] { "soudk" });
            AssertAnalyzesTo(cz, "soudci", new string[] { "soudk" });
            AssertAnalyzesTo(cz, "soudců", new string[] { "soudk" });
            AssertAnalyzesTo(cz, "soudcům", new string[] { "soudk" });
            AssertAnalyzesTo(cz, "soudcích", new string[] { "soudk" });
            AssertAnalyzesTo(cz, "soudcem", new string[] { "soudk" });
        }

        /// <summary>
        /// Test showing how feminine noun forms conflate
        /// </summary>
        [Test]
        public virtual void TestFeminineNouns()
        {
            CzechAnalyzer cz = new CzechAnalyzer(TEST_VERSION_CURRENT);

            /* ending with hard consonant */
            AssertAnalyzesTo(cz, "kost", new string[] { "kost" });
            AssertAnalyzesTo(cz, "kosti", new string[] { "kost" });
            AssertAnalyzesTo(cz, "kostí", new string[] { "kost" });
            AssertAnalyzesTo(cz, "kostem", new string[] { "kost" });
            AssertAnalyzesTo(cz, "kostech", new string[] { "kost" });
            AssertAnalyzesTo(cz, "kostmi", new string[] { "kost" });

            /* ending with a soft consonant */
            // note: in this example sing nom. and sing acc. don't conflate w/ the rest
            AssertAnalyzesTo(cz, "píseň", new string[] { "písň" });
            AssertAnalyzesTo(cz, "písně", new string[] { "písn" });
            AssertAnalyzesTo(cz, "písni", new string[] { "písn" });
            AssertAnalyzesTo(cz, "písněmi", new string[] { "písn" });
            AssertAnalyzesTo(cz, "písních", new string[] { "písn" });
            AssertAnalyzesTo(cz, "písním", new string[] { "písn" });

            /* ending with e */
            AssertAnalyzesTo(cz, "růže", new string[] { "růh" });
            AssertAnalyzesTo(cz, "růží", new string[] { "růh" });
            AssertAnalyzesTo(cz, "růžím", new string[] { "růh" });
            AssertAnalyzesTo(cz, "růžích", new string[] { "růh" });
            AssertAnalyzesTo(cz, "růžemi", new string[] { "růh" });
            AssertAnalyzesTo(cz, "růži", new string[] { "růh" });

            /* ending with a */
            AssertAnalyzesTo(cz, "žena", new string[] { "žn" });
            AssertAnalyzesTo(cz, "ženy", new string[] { "žn" });
            AssertAnalyzesTo(cz, "žen", new string[] { "žn" });
            AssertAnalyzesTo(cz, "ženě", new string[] { "žn" });
            AssertAnalyzesTo(cz, "ženám", new string[] { "žn" });
            AssertAnalyzesTo(cz, "ženu", new string[] { "žn" });
            AssertAnalyzesTo(cz, "ženo", new string[] { "žn" });
            AssertAnalyzesTo(cz, "ženách", new string[] { "žn" });
            AssertAnalyzesTo(cz, "ženou", new string[] { "žn" });
            AssertAnalyzesTo(cz, "ženami", new string[] { "žn" });
        }

        /// <summary>
        /// Test showing how neuter noun forms conflate
        /// </summary>
        [Test]
        public virtual void TestNeuterNouns()
        {
            CzechAnalyzer cz = new CzechAnalyzer(TEST_VERSION_CURRENT);

            /* ending with o */
            AssertAnalyzesTo(cz, "město", new string[] { "měst" });
            AssertAnalyzesTo(cz, "města", new string[] { "měst" });
            AssertAnalyzesTo(cz, "měst", new string[] { "měst" });
            AssertAnalyzesTo(cz, "městu", new string[] { "měst" });
            AssertAnalyzesTo(cz, "městům", new string[] { "měst" });
            AssertAnalyzesTo(cz, "městě", new string[] { "měst" });
            AssertAnalyzesTo(cz, "městech", new string[] { "měst" });
            AssertAnalyzesTo(cz, "městem", new string[] { "měst" });
            AssertAnalyzesTo(cz, "městy", new string[] { "měst" });

            /* ending with e */
            AssertAnalyzesTo(cz, "moře", new string[] { "moř" });
            AssertAnalyzesTo(cz, "moří", new string[] { "moř" });
            AssertAnalyzesTo(cz, "mořím", new string[] { "moř" });
            AssertAnalyzesTo(cz, "moři", new string[] { "moř" });
            AssertAnalyzesTo(cz, "mořích", new string[] { "moř" });
            AssertAnalyzesTo(cz, "mořem", new string[] { "moř" });

            /* ending with ě */
            AssertAnalyzesTo(cz, "kuře", new string[] { "kuř" });
            AssertAnalyzesTo(cz, "kuřata", new string[] { "kuř" });
            AssertAnalyzesTo(cz, "kuřete", new string[] { "kuř" });
            AssertAnalyzesTo(cz, "kuřat", new string[] { "kuř" });
            AssertAnalyzesTo(cz, "kuřeti", new string[] { "kuř" });
            AssertAnalyzesTo(cz, "kuřatům", new string[] { "kuř" });
            AssertAnalyzesTo(cz, "kuřatech", new string[] { "kuř" });
            AssertAnalyzesTo(cz, "kuřetem", new string[] { "kuř" });
            AssertAnalyzesTo(cz, "kuřaty", new string[] { "kuř" });

            /* ending with í */
            AssertAnalyzesTo(cz, "stavení", new string[] { "stavn" });
            AssertAnalyzesTo(cz, "stavením", new string[] { "stavn" });
            AssertAnalyzesTo(cz, "staveních", new string[] { "stavn" });
            AssertAnalyzesTo(cz, "staveními", new string[] { "stavn" });
        }

        /// <summary>
        /// Test showing how adjectival forms conflate
        /// </summary>
        [Test]
        public virtual void TestAdjectives()
        {
            CzechAnalyzer cz = new CzechAnalyzer(TEST_VERSION_CURRENT);

            /* ending with ý/á/é */
            AssertAnalyzesTo(cz, "mladý", new string[] { "mlad" });
            AssertAnalyzesTo(cz, "mladí", new string[] { "mlad" });
            AssertAnalyzesTo(cz, "mladého", new string[] { "mlad" });
            AssertAnalyzesTo(cz, "mladých", new string[] { "mlad" });
            AssertAnalyzesTo(cz, "mladému", new string[] { "mlad" });
            AssertAnalyzesTo(cz, "mladým", new string[] { "mlad" });
            AssertAnalyzesTo(cz, "mladé", new string[] { "mlad" });
            AssertAnalyzesTo(cz, "mladém", new string[] { "mlad" });
            AssertAnalyzesTo(cz, "mladými", new string[] { "mlad" });
            AssertAnalyzesTo(cz, "mladá", new string[] { "mlad" });
            AssertAnalyzesTo(cz, "mladou", new string[] { "mlad" });

            /* ending with í */
            AssertAnalyzesTo(cz, "jarní", new string[] { "jarn" });
            AssertAnalyzesTo(cz, "jarního", new string[] { "jarn" });
            AssertAnalyzesTo(cz, "jarních", new string[] { "jarn" });
            AssertAnalyzesTo(cz, "jarnímu", new string[] { "jarn" });
            AssertAnalyzesTo(cz, "jarním", new string[] { "jarn" });
            AssertAnalyzesTo(cz, "jarními", new string[] { "jarn" });
        }

        /// <summary>
        /// Test some possessive suffixes
        /// </summary>
        [Test]
        public virtual void TestPossessive()
        {
            CzechAnalyzer cz = new CzechAnalyzer(TEST_VERSION_CURRENT);
            AssertAnalyzesTo(cz, "Karlův", new string[] { "karl" });
            AssertAnalyzesTo(cz, "jazykový", new string[] { "jazyk" });
        }

        /// <summary>
        /// Test some exceptional rules, implemented as rewrites.
        /// </summary>
        [Test]
        public virtual void TestExceptions()
        {
            CzechAnalyzer cz = new CzechAnalyzer(TEST_VERSION_CURRENT);

            /* rewrite of št -> sk */
            AssertAnalyzesTo(cz, "český", new string[] { "česk" });
            AssertAnalyzesTo(cz, "čeští", new string[] { "česk" });

            /* rewrite of čt -> ck */
            AssertAnalyzesTo(cz, "anglický", new string[] { "anglick" });
            AssertAnalyzesTo(cz, "angličtí", new string[] { "anglick" });

            /* rewrite of z -> h */
            AssertAnalyzesTo(cz, "kniha", new string[] { "knih" });
            AssertAnalyzesTo(cz, "knize", new string[] { "knih" });

            /* rewrite of ž -> h */
            AssertAnalyzesTo(cz, "mazat", new string[] { "mah" });
            AssertAnalyzesTo(cz, "mažu", new string[] { "mah" });

            /* rewrite of c -> k */
            AssertAnalyzesTo(cz, "kluk", new string[] { "kluk" });
            AssertAnalyzesTo(cz, "kluci", new string[] { "kluk" });
            AssertAnalyzesTo(cz, "klucích", new string[] { "kluk" });

            /* rewrite of č -> k */
            AssertAnalyzesTo(cz, "hezký", new string[] { "hezk" });
            AssertAnalyzesTo(cz, "hezčí", new string[] { "hezk" });

            /* rewrite of *ů* -> *o* */
            AssertAnalyzesTo(cz, "hůl", new string[] { "hol" });
            AssertAnalyzesTo(cz, "hole", new string[] { "hol" });

            /* rewrite of e* -> * */
            AssertAnalyzesTo(cz, "deska", new string[] { "desk" });
            AssertAnalyzesTo(cz, "desek", new string[] { "desk" });
        }

        /// <summary>
        /// Test that very short words are not stemmed.
        /// </summary>
        [Test]
        public virtual void TestDontStem()
        {
            CzechAnalyzer cz = new CzechAnalyzer(TEST_VERSION_CURRENT);
            AssertAnalyzesTo(cz, "e", new string[] { "e" });
            AssertAnalyzesTo(cz, "zi", new string[] { "zi" });
        }

        [Test]
        public virtual void TestWithKeywordAttribute()
        {
            CharArraySet set = new CharArraySet(TEST_VERSION_CURRENT, 1, true);
            set.add("hole");
            CzechStemFilter filter = new CzechStemFilter(new SetKeywordMarkerFilter(new MockTokenizer(new StringReader("hole desek"), MockTokenizer.WHITESPACE, false), set));
            AssertTokenStreamContents(filter, new string[] { "hole", "desk" });
        }

        [Test]
        public virtual void TestEmptyTerm()
        {
            Analyzer a = Analyzer.NewAnonymous(createComponents: (fieldName, reader) =>
            {
                Tokenizer tokenizer = new KeywordTokenizer(reader);
                return new TokenStreamComponents(tokenizer, new CzechStemFilter(tokenizer));
            });
            CheckOneTerm(a, "", "");
        }
    }
}