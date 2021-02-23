// Lucene version compatibility level 4.8.1
using Lucene.Net.Analysis.Core;
using Lucene.Net.Analysis.Miscellaneous;
using Lucene.Net.Analysis.Util;
using NUnit.Framework;
using System.IO;

namespace Lucene.Net.Analysis.Bg
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
    /// Test the Bulgarian Stemmer
    /// </summary>
    public class TestBulgarianStemmer : BaseTokenStreamTestCase
    {
        /// <summary>
        /// Test showing how masculine noun forms conflate. An example noun for each
        /// common (and some rare) plural pattern is listed.
        /// </summary>
        [Test]
        public virtual void TestMasculineNouns()
        {
            BulgarianAnalyzer a = new BulgarianAnalyzer(TEST_VERSION_CURRENT);

            // -и pattern
            AssertAnalyzesTo(a, "град", new string[] { "град" });
            AssertAnalyzesTo(a, "града", new string[] { "град" });
            AssertAnalyzesTo(a, "градът", new string[] { "град" });
            AssertAnalyzesTo(a, "градове", new string[] { "град" });
            AssertAnalyzesTo(a, "градовете", new string[] { "град" });

            // -ове pattern
            AssertAnalyzesTo(a, "народ", new string[] { "народ" });
            AssertAnalyzesTo(a, "народа", new string[] { "народ" });
            AssertAnalyzesTo(a, "народът", new string[] { "народ" });
            AssertAnalyzesTo(a, "народи", new string[] { "народ" });
            AssertAnalyzesTo(a, "народите", new string[] { "народ" });
            AssertAnalyzesTo(a, "народе", new string[] { "народ" });

            // -ища pattern
            AssertAnalyzesTo(a, "път", new string[] { "път" });
            AssertAnalyzesTo(a, "пътя", new string[] { "път" });
            AssertAnalyzesTo(a, "пътят", new string[] { "път" });
            AssertAnalyzesTo(a, "пътища", new string[] { "път" });
            AssertAnalyzesTo(a, "пътищата", new string[] { "път" });

            // -чета pattern
            AssertAnalyzesTo(a, "градец", new string[] { "градец" });
            AssertAnalyzesTo(a, "градеца", new string[] { "градец" });
            AssertAnalyzesTo(a, "градецът", new string[] { "градец" });
            /* note the below forms conflate with each other, but not the rest */
            AssertAnalyzesTo(a, "градовце", new string[] { "градовц" });
            AssertAnalyzesTo(a, "градовцете", new string[] { "градовц" });

            // -овци pattern
            AssertAnalyzesTo(a, "дядо", new string[] { "дяд" });
            AssertAnalyzesTo(a, "дядото", new string[] { "дяд" });
            AssertAnalyzesTo(a, "дядовци", new string[] { "дяд" });
            AssertAnalyzesTo(a, "дядовците", new string[] { "дяд" });

            // -е pattern
            AssertAnalyzesTo(a, "мъж", new string[] { "мъж" });
            AssertAnalyzesTo(a, "мъжа", new string[] { "мъж" });
            AssertAnalyzesTo(a, "мъже", new string[] { "мъж" });
            AssertAnalyzesTo(a, "мъжете", new string[] { "мъж" });
            AssertAnalyzesTo(a, "мъжо", new string[] { "мъж" });
            /* word is too short, will not remove -ът */
            AssertAnalyzesTo(a, "мъжът", new string[] { "мъжът" });

            // -а pattern
            AssertAnalyzesTo(a, "крак", new string[] { "крак" });
            AssertAnalyzesTo(a, "крака", new string[] { "крак" });
            AssertAnalyzesTo(a, "кракът", new string[] { "крак" });
            AssertAnalyzesTo(a, "краката", new string[] { "крак" });

            // брат
            AssertAnalyzesTo(a, "брат", new string[] { "брат" });
            AssertAnalyzesTo(a, "брата", new string[] { "брат" });
            AssertAnalyzesTo(a, "братът", new string[] { "брат" });
            AssertAnalyzesTo(a, "братя", new string[] { "брат" });
            AssertAnalyzesTo(a, "братята", new string[] { "брат" });
            AssertAnalyzesTo(a, "брате", new string[] { "брат" });
        }

        /// <summary>
        /// Test showing how feminine noun forms conflate
        /// </summary>
        [Test]
        public virtual void TestFeminineNouns()
        {
            BulgarianAnalyzer a = new BulgarianAnalyzer(TEST_VERSION_CURRENT);

            AssertAnalyzesTo(a, "вест", new string[] { "вест" });
            AssertAnalyzesTo(a, "вестта", new string[] { "вест" });
            AssertAnalyzesTo(a, "вести", new string[] { "вест" });
            AssertAnalyzesTo(a, "вестите", new string[] { "вест" });
        }

        /// <summary>
        /// Test showing how neuter noun forms conflate an example noun for each common
        /// plural pattern is listed
        /// </summary>
        [Test]
        public virtual void TestNeuterNouns()
        {
            BulgarianAnalyzer a = new BulgarianAnalyzer(TEST_VERSION_CURRENT);

            // -а pattern
            AssertAnalyzesTo(a, "дърво", new string[] { "дърв" });
            AssertAnalyzesTo(a, "дървото", new string[] { "дърв" });
            AssertAnalyzesTo(a, "дърва", new string[] { "дърв" });
            AssertAnalyzesTo(a, "дървета", new string[] { "дърв" });
            AssertAnalyzesTo(a, "дървата", new string[] { "дърв" });
            AssertAnalyzesTo(a, "дърветата", new string[] { "дърв" });

            // -та pattern
            AssertAnalyzesTo(a, "море", new string[] { "мор" });
            AssertAnalyzesTo(a, "морето", new string[] { "мор" });
            AssertAnalyzesTo(a, "морета", new string[] { "мор" });
            AssertAnalyzesTo(a, "моретата", new string[] { "мор" });

            // -я pattern
            AssertAnalyzesTo(a, "изключение", new string[] { "изключени" });
            AssertAnalyzesTo(a, "изключението", new string[] { "изключени" });
            AssertAnalyzesTo(a, "изключенията", new string[] { "изключени" });
            /* note the below form in this example does not conflate with the rest */
            AssertAnalyzesTo(a, "изключения", new string[] { "изключн" });
        }

        /// <summary>
        /// Test showing how adjectival forms conflate
        /// </summary>
        [Test]
        public virtual void TestAdjectives()
        {
            BulgarianAnalyzer a = new BulgarianAnalyzer(TEST_VERSION_CURRENT);
            AssertAnalyzesTo(a, "красив", new string[] { "красив" });
            AssertAnalyzesTo(a, "красивия", new string[] { "красив" });
            AssertAnalyzesTo(a, "красивият", new string[] { "красив" });
            AssertAnalyzesTo(a, "красива", new string[] { "красив" });
            AssertAnalyzesTo(a, "красивата", new string[] { "красив" });
            AssertAnalyzesTo(a, "красиво", new string[] { "красив" });
            AssertAnalyzesTo(a, "красивото", new string[] { "красив" });
            AssertAnalyzesTo(a, "красиви", new string[] { "красив" });
            AssertAnalyzesTo(a, "красивите", new string[] { "красив" });
        }

        /// <summary>
        /// Test some exceptional rules, implemented as rewrites.
        /// </summary>
        [Test]
        public virtual void TestExceptions()
        {
            BulgarianAnalyzer a = new BulgarianAnalyzer(TEST_VERSION_CURRENT);

            // ци -> к
            AssertAnalyzesTo(a, "собственик", new string[] { "собственик" });
            AssertAnalyzesTo(a, "собственика", new string[] { "собственик" });
            AssertAnalyzesTo(a, "собственикът", new string[] { "собственик" });
            AssertAnalyzesTo(a, "собственици", new string[] { "собственик" });
            AssertAnalyzesTo(a, "собствениците", new string[] { "собственик" });

            // зи -> г
            AssertAnalyzesTo(a, "подлог", new string[] { "подлог" });
            AssertAnalyzesTo(a, "подлога", new string[] { "подлог" });
            AssertAnalyzesTo(a, "подлогът", new string[] { "подлог" });
            AssertAnalyzesTo(a, "подлози", new string[] { "подлог" });
            AssertAnalyzesTo(a, "подлозите", new string[] { "подлог" });

            // си -> х
            AssertAnalyzesTo(a, "кожух", new string[] { "кожух" });
            AssertAnalyzesTo(a, "кожуха", new string[] { "кожух" });
            AssertAnalyzesTo(a, "кожухът", new string[] { "кожух" });
            AssertAnalyzesTo(a, "кожуси", new string[] { "кожух" });
            AssertAnalyzesTo(a, "кожусите", new string[] { "кожух" });

            // ъ deletion
            AssertAnalyzesTo(a, "център", new string[] { "центр" });
            AssertAnalyzesTo(a, "центъра", new string[] { "центр" });
            AssertAnalyzesTo(a, "центърът", new string[] { "центр" });
            AssertAnalyzesTo(a, "центрове", new string[] { "центр" });
            AssertAnalyzesTo(a, "центровете", new string[] { "центр" });

            // е*и -> я*
            AssertAnalyzesTo(a, "промяна", new string[] { "промян" });
            AssertAnalyzesTo(a, "промяната", new string[] { "промян" });
            AssertAnalyzesTo(a, "промени", new string[] { "промян" });
            AssertAnalyzesTo(a, "промените", new string[] { "промян" });

            // ен -> н
            AssertAnalyzesTo(a, "песен", new string[] { "песн" });
            AssertAnalyzesTo(a, "песента", new string[] { "песн" });
            AssertAnalyzesTo(a, "песни", new string[] { "песн" });
            AssertAnalyzesTo(a, "песните", new string[] { "песн" });

            // -еве -> й
            // note: this is the only word i think this rule works for.
            // most -еве pluralized nouns are monosyllabic,
            // and the stemmer requires length > 6...
            AssertAnalyzesTo(a, "строй", new string[] { "строй" });
            AssertAnalyzesTo(a, "строеве", new string[] { "строй" });
            AssertAnalyzesTo(a, "строевете", new string[] { "строй" });
            /* note the below forms conflate with each other, but not the rest */
            AssertAnalyzesTo(a, "строя", new string[] { "стр" });
            AssertAnalyzesTo(a, "строят", new string[] { "стр" });
        }

        [Test]
        public virtual void TestWithKeywordAttribute()
        {
            CharArraySet set = new CharArraySet(TEST_VERSION_CURRENT, 1, true);
            set.add("строеве");
            MockTokenizer tokenStream = new MockTokenizer(new StringReader("строевете строеве"), MockTokenizer.WHITESPACE, false);

            BulgarianStemFilter filter = new BulgarianStemFilter(new SetKeywordMarkerFilter(tokenStream, set));
            AssertTokenStreamContents(filter, new string[] { "строй", "строеве" });
        }

        [Test]
        public virtual void TestEmptyTerm()
        {
            Analyzer a = Analyzer.NewAnonymous(createComponents: (fieldName, reader) =>
            {
                Tokenizer tokenizer = new KeywordTokenizer(reader);
                return new TokenStreamComponents(tokenizer, new BulgarianStemFilter(tokenizer));
            });
            CheckOneTerm(a, "", "");
        }
    }
}