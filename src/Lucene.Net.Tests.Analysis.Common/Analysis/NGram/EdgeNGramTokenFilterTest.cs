// Lucene version compatibility level 4.8.1
using J2N;
using Lucene.Net.Analysis.Core;
using Lucene.Net.Analysis.Miscellaneous;
using Lucene.Net.Analysis.Shingle;
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
    /// Tests <seealso cref="EdgeNGramTokenFilter"/> for correctness.
    /// </summary>
    public class EdgeNGramTokenFilterTest : BaseTokenStreamTestCase
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
#pragma warning disable 612, 618
                new EdgeNGramTokenFilter(TEST_VERSION_CURRENT, input, EdgeNGramTokenFilter.Side.FRONT, 0, 0);
#pragma warning restore 612, 618
            }
            catch (ArgumentOutOfRangeException) // LUCENENET specific - changed from IllegalArgumentException to ArgumentOutOfRangeException (.NET convention)
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
#pragma warning disable 612, 618
                new EdgeNGramTokenFilter(TEST_VERSION_CURRENT, input, EdgeNGramTokenFilter.Side.FRONT, 2, 1);
#pragma warning restore 612, 618
            }
            catch (Exception e) when (e.IsIllegalArgumentException())
            {
                gotException = true;
            }
            assertTrue(gotException);
        }

        [Test]
        public virtual void TestInvalidInput3()
        {
            bool gotException = false;
            try
            {
#pragma warning disable 612, 618
                new EdgeNGramTokenFilter(TEST_VERSION_CURRENT, input, EdgeNGramTokenFilter.Side.FRONT, -1, 2);
#pragma warning restore 612, 618
            }
            catch (ArgumentOutOfRangeException) // LUCENENET specific - changed from IllegalArgumentException to ArgumentOutOfRangeException (.NET convention)
            {
                gotException = true;
            }
            assertTrue(gotException);
        }

        [Test]
        public virtual void TestFrontUnigram()
        {
#pragma warning disable 612, 618
            EdgeNGramTokenFilter tokenizer = new EdgeNGramTokenFilter(TEST_VERSION_CURRENT, input, EdgeNGramTokenFilter.Side.FRONT, 1, 1);
#pragma warning restore 612, 618
            AssertTokenStreamContents(tokenizer, new string[] { "a" }, new int[] { 0 }, new int[] { 5 });
        }

        [Test]
        public virtual void TestBackUnigram()
        {
#pragma warning disable 612, 618
            EdgeNGramTokenFilter tokenizer = new EdgeNGramTokenFilter(LuceneVersion.LUCENE_43, input, EdgeNGramTokenFilter.Side.BACK, 1, 1);
#pragma warning restore 612, 618
            AssertTokenStreamContents(tokenizer, new string[] { "e" }, new int[] { 4 }, new int[] { 5 });
        }

        [Test]
        public virtual void TestOversizedNgrams()
        {
#pragma warning disable 612, 618
            EdgeNGramTokenFilter tokenizer = new EdgeNGramTokenFilter(TEST_VERSION_CURRENT, input, EdgeNGramTokenFilter.Side.FRONT, 6, 6);
#pragma warning restore 612, 618
            AssertTokenStreamContents(tokenizer, new string[0], new int[0], new int[0]);
        }

        [Test]
        public virtual void TestFrontRangeOfNgrams()
        {
#pragma warning disable 612, 618
            EdgeNGramTokenFilter tokenizer = new EdgeNGramTokenFilter(TEST_VERSION_CURRENT, input, EdgeNGramTokenFilter.Side.FRONT, 1, 3);
#pragma warning restore 612, 618
            AssertTokenStreamContents(tokenizer, new string[] { "a", "ab", "abc" }, new int[] { 0, 0, 0 }, new int[] { 5, 5, 5 });
        }

        [Test]
        public virtual void TestBackRangeOfNgrams()
        {
#pragma warning disable 612, 618
            EdgeNGramTokenFilter tokenizer = new EdgeNGramTokenFilter(LuceneVersion.LUCENE_43, input, EdgeNGramTokenFilter.Side.BACK, 1, 3);
#pragma warning restore 612, 618
            AssertTokenStreamContents(tokenizer, new string[] { "e", "de", "cde" }, new int[] { 4, 3, 2 }, new int[] { 5, 5, 5 }, null, null, null, null, false);
        }

        [Test]
        public virtual void TestFilterPositions()
        {
            TokenStream ts = new MockTokenizer(new StringReader("abcde vwxyz"), MockTokenizer.WHITESPACE, false);
#pragma warning disable 612, 618
            EdgeNGramTokenFilter tokenizer = new EdgeNGramTokenFilter(TEST_VERSION_CURRENT, ts, EdgeNGramTokenFilter.Side.FRONT, 1, 3);
#pragma warning restore 612, 618
            AssertTokenStreamContents(tokenizer, new string[] { "a", "ab", "abc", "v", "vw", "vwx" }, new int[] { 0, 0, 0, 6, 6, 6 }, new int[] { 5, 5, 5, 11, 11, 11 }, null, new int[] { 1, 0, 0, 1, 0, 0 }, null, null, false);
        }

        private sealed class PositionFilter : TokenFilter
        {

            internal readonly IPositionIncrementAttribute posIncrAtt;
            internal bool started;

            internal PositionFilter(TokenStream input) : base(input)
            {
                posIncrAtt = AddAttribute<IPositionIncrementAttribute>();
            }

            public override sealed bool IncrementToken()
            {
                if (m_input.IncrementToken())
                {
                    if (started)
                    {
                        posIncrAtt.PositionIncrement = 0;
                    }
                    else
                    {
                        started = true;
                    }
                    return true;
                }
                else
                {
                    return false;
                }
            }

            public override void Reset()
            {
                base.Reset();
                started = false;
            }
        }

        [Test]
        public virtual void TestFirstTokenPositionIncrement()
        {
            TokenStream ts = new MockTokenizer(new StringReader("a abc"), MockTokenizer.WHITESPACE, false);
            ts = new PositionFilter(ts); // All but first token will get 0 position increment
#pragma warning disable 612, 618
            EdgeNGramTokenFilter filter = new EdgeNGramTokenFilter(TEST_VERSION_CURRENT, ts, EdgeNGramTokenFilter.Side.FRONT, 2, 3);
#pragma warning restore 612, 618
            // The first token "a" will not be output, since it's smaller than the mingram size of 2.
            // The second token on input to EdgeNGramTokenFilter will have position increment of 0,
            // which should be increased to 1, since this is the first output token in the stream.
            AssertTokenStreamContents(filter, new string[] { "ab", "abc" }, new int[] { 2, 2 }, new int[] { 5, 5 }, new int[] { 1, 0 });
        }

        [Test]
        public virtual void TestSmallTokenInStream()
        {
            input = new MockTokenizer(new StringReader("abc de fgh"), MockTokenizer.WHITESPACE, false);
#pragma warning disable 612, 618
            EdgeNGramTokenFilter tokenizer = new EdgeNGramTokenFilter(TEST_VERSION_CURRENT, input, EdgeNGramTokenFilter.Side.FRONT, 3, 3);
#pragma warning restore 612, 618
            AssertTokenStreamContents(tokenizer, new string[] { "abc", "fgh" }, new int[] { 0, 7 }, new int[] { 3, 10 });
        }

        [Test]
        public virtual void TestReset()
        {
            WhitespaceTokenizer tokenizer = new WhitespaceTokenizer(TEST_VERSION_CURRENT, new StringReader("abcde"));
#pragma warning disable 612, 618
            EdgeNGramTokenFilter filter = new EdgeNGramTokenFilter(TEST_VERSION_CURRENT, tokenizer, EdgeNGramTokenFilter.Side.FRONT, 1, 3);
#pragma warning restore 612, 618
            AssertTokenStreamContents(filter, new string[] { "a", "ab", "abc" }, new int[] { 0, 0, 0 }, new int[] { 5, 5, 5 });
            tokenizer.SetReader(new StringReader("abcde"));
            AssertTokenStreamContents(filter, new string[] { "a", "ab", "abc" }, new int[] { 0, 0, 0 }, new int[] { 5, 5, 5 });
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
#pragma warning disable 612, 618
                filters = new EdgeNGramTokenFilter(LuceneVersion.LUCENE_43, filters, EdgeNGramTokenFilter.Side.FRONT, 2, 15);
#pragma warning restore 612, 618
                return new TokenStreamComponents(tokenizer, filters);
            });
            AssertAnalyzesTo(analyzer, "mosfellsbær", new string[] { "mo", "mos", "mosf", "mosfe", "mosfel", "mosfell", "mosfells", "mosfellsb", "mosfellsba", "mosfellsbae", "mosfellsbaer" }, new int[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 }, new int[] { 11, 11, 11, 11, 11, 11, 11, 11, 11, 11, 11 });
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
                    return new TokenStreamComponents(tokenizer, new EdgeNGramTokenFilter(TEST_VERSION_CURRENT, tokenizer, min, max));
                });
                CheckRandomData(Random, a, 100 * RandomMultiplier);
            }

            Analyzer b = Analyzer.NewAnonymous(createComponents: (fieldName, reader) =>
            {
                Tokenizer tokenizer = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
#pragma warning disable 612, 618
                return new TokenStreamComponents(tokenizer, new EdgeNGramTokenFilter(LuceneVersion.LUCENE_43, tokenizer, EdgeNGramTokenFilter.Side.BACK, 2, 4));
#pragma warning restore 612, 618 
            });
            CheckRandomData(Random, b, 1000 * RandomMultiplier, 20, false, false);
        }

        [Test]
        public virtual void TestEmptyTerm()
        {
            Random random = Random;
            Analyzer a = Analyzer.NewAnonymous(createComponents: (fieldName, reader) =>
            {
                Tokenizer tokenizer = new KeywordTokenizer(reader);
#pragma warning disable 612, 618
                return new TokenStreamComponents(tokenizer, new EdgeNGramTokenFilter(TEST_VERSION_CURRENT, tokenizer, EdgeNGramTokenFilter.Side.FRONT, 2, 15));
#pragma warning restore 612, 618
            });
            CheckAnalysisConsistency(random, a, random.nextBoolean(), "");

            Analyzer b = Analyzer.NewAnonymous(createComponents: (fieldName, reader) =>
            {
                Tokenizer tokenizer = new KeywordTokenizer(reader);
#pragma warning disable 612, 618
                return new TokenStreamComponents(tokenizer, new EdgeNGramTokenFilter(LuceneVersion.LUCENE_43, tokenizer, EdgeNGramTokenFilter.Side.BACK, 2, 15));
#pragma warning restore 612, 618
            });
            CheckAnalysisConsistency(random, b, random.nextBoolean(), "");
        }

        [Test]
        public virtual void TestGraphs()
        {
            TokenStream tk = new LetterTokenizer(TEST_VERSION_CURRENT, new StringReader("abc d efgh ij klmno p q"));
            tk = new ShingleFilter(tk);
            tk = new EdgeNGramTokenFilter(TEST_VERSION_CURRENT, tk, 7, 10);
            AssertTokenStreamContents(tk, new string[] { "efgh ij", "ij klmn", "ij klmno", "klmno p" }, new int[] { 6, 11, 11, 14 }, new int[] { 13, 19, 19, 21 }, new int[] { 3, 1, 0, 1 }, new int[] { 2, 2, 2, 2 }, 23);
        }

        [Test]
        public virtual void TestSupplementaryCharacters()
        {
            string s = TestUtil.RandomUnicodeString(Random, 10);
            int codePointCount = s.CodePointCount(0, s.Length);
            int minGram = TestUtil.NextInt32(Random, 1, 3);
            int maxGram = TestUtil.NextInt32(Random, minGram, 10);
            TokenStream tk = new KeywordTokenizer(new StringReader(s));
            tk = new EdgeNGramTokenFilter(TEST_VERSION_CURRENT, tk, minGram, maxGram);
            ICharTermAttribute termAtt = tk.AddAttribute<ICharTermAttribute>();
            IOffsetAttribute offsetAtt = tk.AddAttribute<IOffsetAttribute>();
            tk.Reset();
            for (int i = minGram; i <= Math.Min(codePointCount, maxGram); ++i)
            {
                assertTrue(tk.IncrementToken());
                assertEquals(0, offsetAtt.StartOffset);
                assertEquals(s.Length, offsetAtt.EndOffset);
                int end = Character.OffsetByCodePoints(s, 0, i);
                assertEquals(s.Substring(0, end), termAtt.ToString());
            }
            assertFalse(tk.IncrementToken());
        }
    }
}