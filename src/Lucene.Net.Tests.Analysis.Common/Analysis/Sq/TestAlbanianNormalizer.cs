// LUCENENET specific: Albanian is not supported by Apache Lucene 4.8; this analyzer is a .NET-only addition.
using Lucene.Net.Analysis.Core;
using NUnit.Framework;
using System.IO;

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
    /// Test <see cref="AlbanianNormalizer"/>
    /// </summary>
    public class TestAlbanianNormalizer : BaseTokenStreamTestCase
    {
        [Test]
        public virtual void TestDiacritics()
        {
            Check("është", "eshte");
            Check("çfarë", "qfare");
            Check("mëshirëbërës", "meshireberes");
            Check("ÇËSHTJE", "QESHTJE");
        }

        /// <summary>
        /// ë and ç written as a base letter plus a combining mark, as text copied from PDFs
        /// often is, fold the same way as the precomposed letters.
        /// </summary>
        [Test]
        public virtual void TestCombiningMarks()
        {
            Check("është", "eshte");
            Check("çfarë", "qfare");
            Check("âsht", "asht");
        }

        /// <summary>
        /// Gheg writing marks nasal vowels with a circumflex (âsht, nânë, Malcís).
        /// </summary>
        [Test]
        public virtual void TestGhegVowels()
        {
            Check("âsht", "asht");
            Check("nânë", "nane");
            Check("shqypnîs", "shqypnis");
            Check("kênë", "kene");
            Check("hûni", "huni");
            Check("ÂSHT", "ASHT");
            // acute accents mark long vowels in the same tradition (Lahuta e Malcís)
            Check("malcís", "malqis");
            Check("lirís", "liris");
            Check("kángë", "kange");
        }

        /// <summary>
        /// ç is typed as c (without the cedilla) and as q (the same sound to most speakers): the three
        /// spellings land on one term. Every row is a spelling seen in the search logs of an Albanian site.
        /// </summary>
        [Test]
        public virtual void TestKeyboardSpellings()
        {
            Check("çdo", "qdo");
            Check("cdo", "qdo");
            Check("qdo", "qdo");
            Check("kryeneç", "kryeneq");
            Check("kryenec", "kryeneq");
            Check("kryeneq", "kryeneq");
            Check("çështje", "qeshtje");
            Check("ceshtje", "qeshtje");
            Check("qeshtje", "qeshtje");
            Check("mençur", "menqur");
        }

        /// <summary>
        /// Words that really have a c keep it (cwords.txt): cili, copë, forcë, kec, and the whole word
        /// ca; a c typed for ç still folds, also in a word that looks like one of them (care for çarë).
        /// </summary>
        [Test]
        public virtual void TestRealCIsKept()
        {
            Check("kec", "kec");
            Check("kecat", "kecat");
            Check("Kec", "Kec");
            Check("cili", "cili");
            Check("cilët", "cilet");
            Check("secili", "secili");
            Check("copë", "cope");
            Check("forcë", "force");
            Check("caktuar", "caktuar");
            Check("shumica", "shumica");
            Check("ca", "ca");
            Check("care", "qare");
            Check("cdo", "qdo");
            Check("kryenec", "kryeneq");
            // xh is not folded to gj: xhenet (paradise) would stem into gjen (to find)
            Check("xhenet", "xhenet");
            Check("gjenet", "gjenet");
        }

        [Test]
        public virtual void TestApostrophes()
        {
            Check("kur'an", "kuran");
            Check("kur’an", "kuran");
            Check("kurʼan", "kuran");
            Check("pa'pa'pa", "papapa");
        }

        /// <summary>
        /// A one-letter clitic elided with an apostrophe is dropped, so that the word it was
        /// attached to can still be recognized (ç'është → është, which is a stop word).
        /// </summary>
        [Test]
        public virtual void TestElidedClitics()
        {
            Check("ç'është", "eshte");
            Check("s'ka", "ka");
            Check("t'i", "i");
            Check("m'u", "u");
            Check("q'u", "u");
            Check("n'atë", "ate");
            Check("s'a", "a");
            // nothing after the apostrophe: just drop it
            Check("s'", "s");
        }

        [Test]
        public virtual void TestUnchanged()
        {
            Check("durim", "durim");
            Check("shqiperi", "shqiperi");
        }

        private void Check(string input, string output)
        {
            Tokenizer tokenizer = new MockTokenizer(new StringReader(input), MockTokenizer.WHITESPACE, false);
            TokenFilter tf = new AlbanianNormalizationFilter(tokenizer);
            AssertTokenStreamContents(tf, new string[] { output });
        }

        [Test]
        public virtual void TestEmptyTerm()
        {
            Analyzer a = Analyzer.NewAnonymous(createComponents: (fieldName, reader) =>
            {
                Tokenizer tokenizer = new KeywordTokenizer(reader);
                return new TokenStreamComponents(tokenizer, new AlbanianNormalizationFilter(tokenizer));
            });
            CheckOneTerm(a, "", "");
        }
    }
}
