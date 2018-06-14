// Lucene version compatibility level < 7.1.0
using NUnit.Framework;

namespace Lucene.Net.Analysis.Icu.Segmentation
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
    /// Test tokenizing Myanmar text into syllables
    /// </summary>
    public class TestMyanmarSyllable : BaseTokenStreamTestCase
    {
        Analyzer a;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            a = Analyzer.NewAnonymous(createComponents: (fieldName, reader) =>
            {
                Tokenizer tokenizer = new ICUTokenizer(reader, new DefaultICUTokenizerConfig(false, false));
                return new TokenStreamComponents(tokenizer);
            });
        }

        [TearDown]
        public override void TearDown()
        {
            if (a != null) a.Dispose();
            base.TearDown();
        }

        /** as opposed to dictionary break of သက်ဝင်|လှုပ်ရှား|စေ|ပြီး */
        [Test]
        public void TestBasics()
        {
            AssertAnalyzesTo(a, "သက်ဝင်လှုပ်ရှားစေပြီး", new string[] { "သက်", "ဝင်", "လှုပ်", "ရှား", "စေ", "ပြီး" });
        }

        // simple tests from "A Rule-based Syllable Segmentation of Myanmar Text" 
        // * http://www.aclweb.org/anthology/I08-3010
        // (see also the presentation: http://gii2.nagaokaut.ac.jp/gii/media/share/20080901-ZMM%20Presentation.pdf)
        // The words are fake, we just test the categories.
        // note that currently our algorithm is not sophisticated enough to handle some of the special cases!

        /** constant */
        [Test]
        public void TestC()
        {
            AssertAnalyzesTo(a, "ကက", new string[] { "က", "က" });
        }

        /** consonant + sign */
        [Test]
        public void TestCF()
        {
            AssertAnalyzesTo(a, "ကံကံ", new string[] { "ကံ", "ကံ" });
        }

        /** consonant + consonant + asat */
        [Test]
        public void TestCCA()
        {
            AssertAnalyzesTo(a, "ကင်ကင်", new string[] { "ကင်", "ကင်" });
        }

        /** consonant + consonant + asat + sign */
        [Test]
        public void TestCCAF()
        {
            AssertAnalyzesTo(a, "ကင်းကင်း", new string[] { "ကင်း", "ကင်း" });
        }

        /** consonant + vowel */
        [Test]
        public void TestCV()
        {
            AssertAnalyzesTo(a, "ကာကာ", new string[] { "ကာ", "ကာ" });
        }

        /** consonant + vowel + sign */
        [Test]
        public void TestCVF()
        {
            AssertAnalyzesTo(a, "ကားကား", new string[] { "ကား", "ကား" });
        }

        /** consonant + vowel + vowel + asat */
        [Test]
        public void TestCVVA()
        {
            AssertAnalyzesTo(a, "ကော်ကော်", new string[] { "ကော်", "ကော်" });
        }

        /** consonant + vowel + vowel + consonant + asat */
        [Test]
        public void TestCVVCA()
        {
            AssertAnalyzesTo(a, "ကောင်ကောင်", new string[] { "ကောင်", "ကောင်" });
        }

        /** consonant + vowel + vowel + consonant + asat + sign */
        [Test]
        public void TestCVVCAF()
        {
            AssertAnalyzesTo(a, "ကောင်းကောင်း", new string[] { "ကောင်း", "ကောင်း" });
        }

        /** consonant + medial */
        [Test]
        public void TestCM()
        {
            AssertAnalyzesTo(a, "ကျကျ", new string[] { "ကျ", "ကျ" });
        }

        /** consonant + medial + sign */
        [Test]
        public void TestCMF()
        {
            AssertAnalyzesTo(a, "ကျံကျံ", new string[] { "ကျံ", "ကျံ" });
        }

        /** consonant + medial + consonant + asat */
        [Test]
        public void TestCMCA()
        {
            AssertAnalyzesTo(a, "ကျင်ကျင်", new string[] { "ကျင်", "ကျင်" });
        }

        /** consonant + medial + consonant + asat + sign */
        [Test]
        public void TestCMCAF()
        {
            AssertAnalyzesTo(a, "ကျင်းကျင်း", new string[] { "ကျင်း", "ကျင်း" });
        }

        /** consonant + medial + vowel */
        [Test]
        public void TestCMV()
        {
            AssertAnalyzesTo(a, "ကျာကျာ", new string[] { "ကျာ", "ကျာ" });
        }

        /** consonant + medial + vowel + sign */
        [Test]
        public void TestCMVF()
        {
            AssertAnalyzesTo(a, "ကျားကျား", new string[] { "ကျား", "ကျား" });
        }

        /** consonant + medial + vowel + vowel + asat */
        [Test]
        public void TestCMVVA()
        {
            AssertAnalyzesTo(a, "ကျော်ကျော်", new string[] { "ကျော်", "ကျော်" });
        }

        /** consonant + medial + vowel + vowel + consonant + asat */
        [Test]
        public void TestCMVVCA()
        {
            AssertAnalyzesTo(a, "ကြောင်ကြောင်", new string[] { "ကြောင်", "ကြောင်" });
        }

        /** consonant + medial + vowel + vowel + consonant + asat + sign */
        [Test]
        public void TestCMVVCAF()
        {
            AssertAnalyzesTo(a, "ကြောင်းကြောင်း", new string[] { "ကြောင်း", "ကြောင်း" });
        }

        /** independent vowel */
        [Test]
        public void TestI()
        {
            AssertAnalyzesTo(a, "ဪဪ", new string[] { "ဪ", "ဪ" });
        }

        /** independent vowel */
        [Test]
        public void TestE()
        {
            AssertAnalyzesTo(a, "ဣဣ", new string[] { "ဣ", "ဣ" });
        }
    }
}