// Lucene version compatibility level < 7.1.0
using Lucene.Net.Analysis.Core;
using NUnit.Framework;

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
    /// Tests <see cref="ICUFoldingFilter"/>
    /// </summary>
    public class TestICUFoldingFilter : BaseTokenStreamTestCase
    {
        Analyzer a;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            a = Analyzer.NewAnonymous(createComponents: (fieldName, reader) =>
            {
                Tokenizer tokenizer = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
                return new TokenStreamComponents(tokenizer, new ICUFoldingFilter(tokenizer));
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
            AssertAnalyzesTo(a, "This is a test", new string[] { "this", "is", "a", "test" });

            // case folding
            AssertAnalyzesTo(a, "Ruß", new string[] { "russ" });

            // case folding with accent removal
            AssertAnalyzesTo(a, "ΜΆΪΟΣ", new string[] { "μαιοσ" });
            AssertAnalyzesTo(a, "Μάϊος", new string[] { "μαιοσ" });

            // supplementary case folding
            AssertAnalyzesTo(a, "𐐖", new string[] { "𐐾" });

            // normalization
            AssertAnalyzesTo(a, "ﴳﴺﰧ", new string[] { "طمطمطم" });

            // removal of default ignorables
            AssertAnalyzesTo(a, "क्‍ष", new string[] { "कष" });

            // removal of latin accents (composed)
            AssertAnalyzesTo(a, "résumé", new string[] { "resume" });

            // removal of latin accents (decomposed)
            AssertAnalyzesTo(a, "re\u0301sume\u0301", new string[] { "resume" });

            // fold native digits
            AssertAnalyzesTo(a, "৭০৬", new string[] { "706" });

            // ascii-folding-filter type stuff
            AssertAnalyzesTo(a, "đis is cræzy", new string[] { "dis", "is", "craezy" });

            // proper downcasing of Turkish dotted-capital I
            // (according to default case folding rules)
            AssertAnalyzesTo(a, "ELİF", new string[] { "elif" });

            // handling of decomposed combining-dot-above
            AssertAnalyzesTo(a, "eli\u0307f", new string[] { "elif" });
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
                return new TokenStreamComponents(tokenizer, new ICUFoldingFilter(tokenizer));
            });
            CheckOneTerm(a, "", "");
        }
    }
}
