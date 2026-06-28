using Lucene.Net.Analysis.Core;
using Lucene.Net.Analysis.Miscellaneous;
using Lucene.Net.Analysis.Util;
using Lucene.Net.Attributes;
using NUnit.Framework;
using System;

namespace Lucene.Net.Analysis.Ja
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
    /// Tests for <see cref="JapaneseKatakanaStemFilter"/>
    /// </summary>
    public class TestJapaneseKatakanaStemFilter : BaseTokenStreamTestCase
    {
        private Analyzer analyzer = Analyzer.NewAnonymous(createComponents: (fieldName, reader) =>
        {
            // Use a MockTokenizer here since this filter doesn't really depend on Kuromoji
            Tokenizer source = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
            return new TokenStreamComponents(source, new JapaneseKatakanaStemFilter(source));
        });

        /**
         * Test a few common katakana spelling variations.
         * <p>
         * English translations are as follows:
         * <ul>
         *   <li>copy</li>
         *   <li>coffee</li>
         *   <li>taxi</li>
         *   <li>party</li>
         *   <li>party (without long sound)</li>
         *   <li>center</li>
         * </ul>
         * Note that we remove a long sound in the case of "coffee" that is required.
         * </p>
         */
        [Test]
        public void TestStemVariants()
        {
            AssertAnalyzesTo(analyzer, "コピー コーヒー タクシー パーティー パーティ センター",
          new String[] { "コピー", "コーヒ", "タクシ", "パーティ", "パーティ", "センタ" },
          new int[] { 0, 4, 9, 14, 20, 25 },
          new int[] { 3, 8, 13, 19, 24, 29 });
        }

        [Test]
        public void TestKeyword()
        {
            CharArraySet exclusionSet = new CharArraySet(TEST_VERSION_CURRENT, AsSet("コーヒー"), false);
            Analyzer a = Analyzer.NewAnonymous(createComponents: (fieldName, reader) =>
            {
                Tokenizer source = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
                TokenStream sink = new SetKeywordMarkerFilter(source, exclusionSet);
                return new TokenStreamComponents(source, new JapaneseKatakanaStemFilter(sink));
            });
            CheckOneTerm(a, "コーヒー", "コーヒー");
        }

        [Test]
        public void TestUnsupportedHalfWidthVariants()
        {
            // The below result is expected since only full-width katakana is supported
            AssertAnalyzesTo(analyzer, "ﾀｸｼｰ", new String[] { "ﾀｸｼｰ" });
        }

        [Test]
        public void TestRandomData()
        {
            CheckRandomData(Random, analyzer, 1000 * RandomMultiplier);
        }

        [Test]
        public void TestEmptyTerm()
        {
            Analyzer a = Analyzer.NewAnonymous(createComponents: (fieldName, reader) =>
            {
                Tokenizer tokenizer = new KeywordTokenizer(reader);
                return new TokenStreamComponents(tokenizer, new JapaneseKatakanaStemFilter(tokenizer));
            });

            CheckOneTerm(a, "", "");
        }

        /// <summary>
        /// LUCENENET-specific regression test for <a href="https://github.com/apache/lucenenet/issues/1072">#1072</a>.
        /// <para/>
        /// Verifies the LUCENE-10352 guard: the constructor must reject a <c>minimumLength</c> less than
        /// <c>1</c>. A <c>minimumLength</c> of <c>0</c> would let a zero-length token (such as the single empty
        /// token a <see cref="KeywordTokenizer"/> emits for empty input) skip the length check in <c>Stem()</c>,
        /// after which the <c>term[length - 1]</c> access reads <c>term[-1]</c> and throws. This surfaced as a
        /// residual <c>TestRandomChains</c> failure reported on PR #1348 (e.g. seed
        /// <c>0x951afd480395f9b7:0x63fc16274945f1a3</c>).
        /// </summary>
        [Test]
        [LuceneNetSpecific] // Issue #1072
        public void TestMinimumLengthLessThanOneThrows()
        {
            Tokenizer tokenizer = new KeywordTokenizer(new System.IO.StringReader(""));

            Assert.Throws<ArgumentOutOfRangeException>(() => new JapaneseKatakanaStemFilter(tokenizer, minimumLength: 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new JapaneseKatakanaStemFilter(tokenizer, minimumLength: -1));
        }
    }
}
