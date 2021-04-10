// Lucene version compatibility level 4.8.1
using J2N;
using Lucene.Net.Analysis.Core;
using Lucene.Net.Analysis.Miscellaneous;
using Lucene.Net.Analysis.TokenAttributes;
using Lucene.Net.Util;
using NUnit.Framework;
using System;
using System.IO;

namespace Lucene.Net.Analysis.NGram
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
    /// Tests <seealso cref="NGramTokenFilter"/> for correctness.
    /// </summary>
    public class NGramTokenFilterTest : BaseTokenStreamTestCase
    {
        private TokenStream input;

        public override void SetUp()
        {
            base.SetUp();
            input = new MockTokenizer(new StringReader("abcde"), MockTokenizer.WHITESPACE, false);
        }

        [Test]
        public virtual void TestInvalidInput()
        {
            bool gotException = false;
            try
            {
                new NGramTokenFilter(TEST_VERSION_CURRENT, input, 2, 1);
            }
            catch (Exception e) when (e.IsIllegalArgumentException())
            {
                gotException = true;
            }
            assertTrue(gotException);
        }

        [Test]
        public virtual void TestInvalidInput2()
        {
            bool gotException = false;
            try
            {
                new NGramTokenFilter(TEST_VERSION_CURRENT, input, 0, 1);
            }
            catch (ArgumentOutOfRangeException) // LUCENENET specific - changed from IllegalArgumentException to ArgumentOutOfRangeException (.NET convention)
            {
                gotException = true;
            }
            assertTrue(gotException);
        }

        [Test]
        public virtual void TestUnigrams()
        {
            NGramTokenFilter filter = new NGramTokenFilter(TEST_VERSION_CURRENT, input, 1, 1);
            AssertTokenStreamContents(filter, new string[] { "a", "b", "c", "d", "e" }, new int[] { 0, 0, 0, 0, 0 }, new int[] { 5, 5, 5, 5, 5 }, new int[] { 1, 0, 0, 0, 0 });
        }

        [Test]
        public virtual void TestBigrams()
        {
            NGramTokenFilter filter = new NGramTokenFilter(TEST_VERSION_CURRENT, input, 2, 2);
            AssertTokenStreamContents(filter, new string[] { "ab", "bc", "cd", "de" }, new int[] { 0, 0, 0, 0 }, new int[] { 5, 5, 5, 5 }, new int[] { 1, 0, 0, 0 });
        }

        [Test]
        public virtual void TestNgrams()
        {
            NGramTokenFilter filter = new NGramTokenFilter(TEST_VERSION_CURRENT, input, 1, 3);
            AssertTokenStreamContents(filter, new string[] { "a", "ab", "abc", "b", "bc", "bcd", "c", "cd", "cde", "d", "de", "e" }, new int[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 }, new int[] { 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5 }, null, new int[] { 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 }, null, null, false);
        }

        [Test]
        public virtual void TestNgramsNoIncrement()
        {
            NGramTokenFilter filter = new NGramTokenFilter(TEST_VERSION_CURRENT, input, 1, 3);
            AssertTokenStreamContents(filter, new string[] { "a", "ab", "abc", "b", "bc", "bcd", "c", "cd", "cde", "d", "de", "e" }, new int[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 }, new int[] { 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5 }, null, new int[] { 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 }, null, null, false);
        }

        [Test]
        public virtual void TestOversizedNgrams()
        {
            NGramTokenFilter filter = new NGramTokenFilter(TEST_VERSION_CURRENT, input, 6, 7);
            AssertTokenStreamContents(filter, new string[0], new int[0], new int[0]);
        }

        [Test]
        public virtual void TestSmallTokenInStream()
        {
            input = new MockTokenizer(new StringReader("abc de fgh"), MockTokenizer.WHITESPACE, false);
            NGramTokenFilter filter = new NGramTokenFilter(TEST_VERSION_CURRENT, input, 3, 3);
            AssertTokenStreamContents(filter, new string[] { "abc", "fgh" }, new int[] { 0, 7 }, new int[] { 3, 10 }, new int[] { 1, 2 });
        }

        [Test]
        public virtual void TestReset()
        {
            WhitespaceTokenizer tokenizer = new WhitespaceTokenizer(TEST_VERSION_CURRENT, new StringReader("abcde"));
            NGramTokenFilter filter = new NGramTokenFilter(TEST_VERSION_CURRENT, tokenizer, 1, 1);
            AssertTokenStreamContents(filter, new string[] { "a", "b", "c", "d", "e" }, new int[] { 0, 0, 0, 0, 0 }, new int[] { 5, 5, 5, 5, 5 }, new int[] { 1, 0, 0, 0, 0 });
            tokenizer.SetReader(new StringReader("abcde"));
            AssertTokenStreamContents(filter, new string[] { "a", "b", "c", "d", "e" }, new int[] { 0, 0, 0, 0, 0 }, new int[] { 5, 5, 5, 5, 5 }, new int[] { 1, 0, 0, 0, 0 });
        }

        // LUCENE-3642
        // EdgeNgram blindly adds term length to offset, but this can take things out of bounds
        // wrt original text if a previous filter increases the length of the word (in this case æ -> ae)
        // so in this case we behave like WDF, and preserve any modified offsets
        [Test]
        public virtual void TestInvalidOffsets()
        {
            Analyzer analyzer = Analyzer.NewAnonymous(createComponents: (fieldName, reader) =>
            {
                Tokenizer tokenizer = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
                TokenFilter filters = new ASCIIFoldingFilter(tokenizer);
                filters = new NGramTokenFilter(TEST_VERSION_CURRENT, filters, 2, 2);
                return new TokenStreamComponents(tokenizer, filters);
            });
            AssertAnalyzesTo(analyzer, "mosfellsbær", new string[] { "mo", "os", "sf", "fe", "el", "ll", "ls", "sb", "ba", "ae", "er" }, new int[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 }, new int[] { 11, 11, 11, 11, 11, 11, 11, 11, 11, 11, 11 }, new int[] { 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 });
        }

        /// <summary>
        /// blast some random strings through the analyzer </summary>
        [Test]
        public virtual void TestRandomStrings()
        {
            for (int i = 0; i < 10; i++)
            {
                int min = TestUtil.NextInt32(Random, 2, 10);
                int max = TestUtil.NextInt32(Random, min, 20);
                Analyzer a = Analyzer.NewAnonymous(createComponents: (fieldName, reader) =>
                {
                    Tokenizer tokenizer = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
                    return new TokenStreamComponents(tokenizer, new NGramTokenFilter(TEST_VERSION_CURRENT, tokenizer, min, max));
                });
                CheckRandomData(Random, a, 200 * RandomMultiplier, 20);
            }
        }

        [Test]
        public virtual void TestEmptyTerm()
        {
            Random random = Random;
            Analyzer a = Analyzer.NewAnonymous(createComponents: (fieldName, reader) =>
            {
                Tokenizer tokenizer = new KeywordTokenizer(reader);
                return new TokenStreamComponents(tokenizer, new NGramTokenFilter(TEST_VERSION_CURRENT, tokenizer, 2, 15));
            });
            CheckAnalysisConsistency(random, a, random.nextBoolean(), "");
        }

        [Test]
        public virtual void TestLucene43()
        {
#pragma warning disable 612, 618
            NGramTokenFilter filter = new NGramTokenFilter(LuceneVersion.LUCENE_43, input, 2, 3);
#pragma warning restore 612, 618
            AssertTokenStreamContents(filter, new string[] { "ab", "bc", "cd", "de", "abc", "bcd", "cde" }, new int[] { 0, 1, 2, 3, 0, 1, 2 }, new int[] { 2, 3, 4, 5, 3, 4, 5 }, null, new int[] { 1, 1, 1, 1, 1, 1, 1 }, null, null, false);
        }

        [Test]
        public virtual void TestSupplementaryCharacters()
        {
            string s = TestUtil.RandomUnicodeString(Random, 10);
            int codePointCount = s.CodePointCount(0, s.Length);
            int minGram = TestUtil.NextInt32(Random, 1, 3);
            int maxGram = TestUtil.NextInt32(Random, minGram, 10);
            TokenStream tk = new KeywordTokenizer(new StringReader(s));
            tk = new NGramTokenFilter(TEST_VERSION_CURRENT, tk, minGram, maxGram);
            ICharTermAttribute termAtt = tk.AddAttribute<ICharTermAttribute>();
            IOffsetAttribute offsetAtt = tk.AddAttribute<IOffsetAttribute>();
            tk.Reset();
            for (int start = 0; start < codePointCount; ++start)
            {
                for (int end = start + minGram; end <= Math.Min(codePointCount, start + maxGram); ++end)
                {
                    assertTrue(tk.IncrementToken());
                    assertEquals(0, offsetAtt.StartOffset);
                    assertEquals(s.Length, offsetAtt.EndOffset);
                    int startIndex = Character.OffsetByCodePoints(s, 0, start);
                    int endIndex = Character.OffsetByCodePoints(s, 0, end);
                    assertEquals(s.Substring(startIndex, endIndex - startIndex), termAtt.ToString());
                }
            }
            assertFalse(tk.IncrementToken());
        }
    }
}