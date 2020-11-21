// Lucene version compatibility level < 7.1.0
using ICU4N.Text;
using Lucene.Net.Analysis.Core;
using NUnit.Framework;
using System;

namespace Lucene.Net.Analysis.Icu
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
    /// Tests the ICUNormalizer2Filter
    /// </summary>
    public class TestICUNormalizer2Filter : BaseTokenStreamTestCase
    {
        private Analyzer a;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            a = Analyzer.NewAnonymous(createComponents: (fieldName, reader) =>
            {
                Tokenizer tokenizer = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
                return new TokenStreamComponents(tokenizer, new ICUNormalizer2Filter(tokenizer));
            });
        }

        [TearDown]
        public override void TearDown()
        {
            if (a != null) a.Dispose();
            base.TearDown();
        }

        [Test]
        public void TestDefaults()
        {
            // case folding
            AssertAnalyzesTo(a, "This is a test", new String[] { "this", "is", "a", "test" });

            // case folding
            AssertAnalyzesTo(a, "Ruß", new String[] { "russ" });

            // case folding
            AssertAnalyzesTo(a, "ΜΆΪΟΣ", new String[] { "μάϊοσ" });
            AssertAnalyzesTo(a, "Μάϊος", new String[] { "μάϊοσ" });

            // supplementary case folding
            AssertAnalyzesTo(a, "𐐖", new String[] { "𐐾" });

            // normalization
            AssertAnalyzesTo(a, "ﴳﴺﰧ", new String[] { "طمطمطم" });

            // removal of default ignorables
            AssertAnalyzesTo(a, "क्‍ष", new String[] { "क्ष" });
        }

        [Test]
        public void TestAlternate()
        {
            using Analyzer a = Analyzer.NewAnonymous(createComponents: (fieldName, reader) =>
            {
                Tokenizer tokenizer = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
                return new TokenStreamComponents(tokenizer, new ICUNormalizer2Filter(
                    tokenizer,
                    /* specify nfc with decompose to get nfd */
                    Normalizer2.GetInstance(null, "nfc", Normalizer2Mode.Decompose)));
            });
            // decompose EAcute into E + combining Acute
            AssertAnalyzesTo(a, "\u00E9", new String[] { "\u0065\u0301" });
        }

        /** blast some random strings through the analyzer */
        [Test]
        public void TestRandomStrings()
        {
            CheckRandomData(Random, a, 1000 * RandomMultiplier);
        }

        [Test]
        public void TestEmptyTerm()
        {
            using Analyzer a = Analyzer.NewAnonymous(createComponents: (fieldName, reader) =>
            {
                Tokenizer tokenizer = new KeywordTokenizer(reader);
                return new TokenStreamComponents(tokenizer, new ICUNormalizer2Filter(tokenizer));
            });
            CheckOneTerm(a, "", "");
        }
    }
}
