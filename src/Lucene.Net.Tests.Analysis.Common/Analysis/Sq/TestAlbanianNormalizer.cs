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
            Check("çfarë", "cfare");
            Check("mëshirëbërës", "meshireberes");
            Check("ÇËSHTJE", "CESHTJE");
        }

        /// <summary>
        /// ë and ç written as a base letter plus a combining mark, as text copied from PDFs
        /// often is, fold the same way as the precomposed letters.
        /// </summary>
        [Test]
        public virtual void TestCombiningMarks()
        {
            Check("është", "eshte");
            Check("çfarë", "cfare");
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
            Check("malcís", "malcis");
            Check("lirís", "liris");
            Check("kángë", "kange");
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
