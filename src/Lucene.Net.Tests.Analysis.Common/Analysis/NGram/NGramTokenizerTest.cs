// Lucene version compatibility level 4.8.1
using J2N;
using J2N.Text;
using Lucene.Net.Analysis.TokenAttributes;
using Lucene.Net.Support;
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
    /// Tests <seealso cref="NGramTokenizer"/> for correctness.
    /// </summary>
    public class NGramTokenizerTest : BaseTokenStreamTestCase
    {
        private StringReader input;

        public override void SetUp()
        {
            base.SetUp();
            input = new StringReader("abcde");
        }

        [Test]
        public virtual void TestInvalidInput()
        {
            bool gotException = false;
            try
            {
                new NGramTokenizer(TEST_VERSION_CURRENT, input, 2, 1);
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
                new NGramTokenizer(TEST_VERSION_CURRENT, input, 0, 1);
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
            NGramTokenizer tokenizer = new NGramTokenizer(TEST_VERSION_CURRENT, input, 1, 1);
            AssertTokenStreamContents(tokenizer, new string[] { "a", "b", "c", "d", "e" }, new int[] { 0, 1, 2, 3, 4 }, new int[] { 1, 2, 3, 4, 5 }, 5); // abcde
        }

        [Test]
        public virtual void TestBigrams()
        {
            NGramTokenizer tokenizer = new NGramTokenizer(TEST_VERSION_CURRENT, input, 2, 2);
            AssertTokenStreamContents(tokenizer, new string[] { "ab", "bc", "cd", "de" }, new int[] { 0, 1, 2, 3 }, new int[] { 2, 3, 4, 5 }, 5); // abcde
        }

        [Test]
        public virtual void TestNgrams()
        {
            NGramTokenizer tokenizer = new NGramTokenizer(TEST_VERSION_CURRENT, input, 1, 3);
            AssertTokenStreamContents(tokenizer, new string[] { "a", "ab", "abc", "b", "bc", "bcd", "c", "cd", "cde", "d", "de", "e" }, new int[] { 0, 0, 0, 1, 1, 1, 2, 2, 2, 3, 3, 4 }, new int[] { 1, 2, 3, 2, 3, 4, 3, 4, 5, 4, 5, 5 }, null, null, null, 5, false); // abcde
        }

        [Test]
        public virtual void TestOversizedNgrams()
        {
            NGramTokenizer tokenizer = new NGramTokenizer(TEST_VERSION_CURRENT, input, 6, 7);
            AssertTokenStreamContents(tokenizer, new string[0], new int[0], new int[0], 5); // abcde
        }

        [Test]
        public virtual void TestReset()
        {
            NGramTokenizer tokenizer = new NGramTokenizer(TEST_VERSION_CURRENT, input, 1, 1);
            AssertTokenStreamContents(tokenizer, new string[] { "a", "b", "c", "d", "e" }, new int[] { 0, 1, 2, 3, 4 }, new int[] { 1, 2, 3, 4, 5 }, 5); // abcde
            tokenizer.SetReader(new StringReader("abcde"));
            AssertTokenStreamContents(tokenizer, new string[] { "a", "b", "c", "d", "e" }, new int[] { 0, 1, 2, 3, 4 }, new int[] { 1, 2, 3, 4, 5 }, 5); // abcde
        }

        /// <summary>
        /// blast some random strings through the analyzer </summary>
        [Test]
        [Slow]
        public virtual void TestRandomStrings()
        {
            for (int i = 0; i < 10; i++)
            {
                int min = TestUtil.NextInt32(Random, 2, 10);
                int max = TestUtil.NextInt32(Random, min, 20);
                Analyzer a = Analyzer.NewAnonymous(createComponents: (fieldName, reader) =>
                {
                    Tokenizer tokenizer = new NGramTokenizer(TEST_VERSION_CURRENT, reader, min, max);
                    return new TokenStreamComponents(tokenizer, tokenizer);
                });
                CheckRandomData(Random, a, 200 * RandomMultiplier, 20);
                CheckRandomData(Random, a, 10 * RandomMultiplier, 1027);
            }
        }

        private static void TestNGrams(int minGram, int maxGram, int length, string nonTokenChars)
        {
            //string s = RandomStrings.randomAsciiOfLength(Random(), length);
            string s = TestUtil.RandomAnalysisString(Random, length, true);
            TestNGrams(minGram, maxGram, s, nonTokenChars);
        }

        private static void TestNGrams(int minGram, int maxGram, string s, string nonTokenChars)
        {
            TestNGrams(minGram, maxGram, s, nonTokenChars, false);
        }

        internal static int[] toCodePoints(string s)
        {
            int[] codePoints = new int[Character.CodePointCount(s, 0, s.Length)];
            for (int i = 0, j = 0; i < s.Length; ++j)
            {
                codePoints[j] = Character.CodePointAt(s, i);
                i += Character.CharCount(codePoints[j]);
            }
            return codePoints;
        }

        internal static int[] toCodePoints(ICharSequence s)
        {
            int[] codePoints = new int[Character.CodePointCount(s, 0, s.Length)];
            for (int i = 0, j = 0; i < s.Length; ++j)
            {
                codePoints[j] = Character.CodePointAt(s, i);
                i += Character.CharCount(codePoints[j]);
            }
            return codePoints;
        }

        internal static bool isTokenChar(string nonTokenChars, int codePoint)
        {
            for (int i = 0; i < nonTokenChars.Length;)
            {
                int cp = nonTokenChars.CodePointAt(i);
                if (cp == codePoint)
                {
                    return false;
                }
                i += Character.CharCount(cp);
            }
            return true;
        }

        internal static void TestNGrams(int minGram, int maxGram, string s, string nonTokenChars, bool edgesOnly)
        {
            // convert the string to code points
            int[] codePoints = toCodePoints(s);
            int[] offsets = new int[codePoints.Length + 1];
            for (int i = 0; i < codePoints.Length; ++i)
            {
                offsets[i + 1] = offsets[i] + Character.CharCount(codePoints[i]);
            }
            TokenStream grams = new NGramTokenizerAnonymousClass(TEST_VERSION_CURRENT, new StringReader(s), minGram, maxGram, edgesOnly, nonTokenChars);
            ICharTermAttribute termAtt = grams.AddAttribute<ICharTermAttribute>();
            IPositionIncrementAttribute posIncAtt = grams.AddAttribute<IPositionIncrementAttribute>();
            IPositionLengthAttribute posLenAtt = grams.AddAttribute<IPositionLengthAttribute>();
            IOffsetAttribute offsetAtt = grams.AddAttribute<IOffsetAttribute>();
            grams.Reset();
            for (int start = 0; start < codePoints.Length; ++start)
            {
                for (int end = start + minGram; end <= start + maxGram && end <= codePoints.Length; ++end)
                {
                    if (edgesOnly && start > 0 && isTokenChar(nonTokenChars, codePoints[start - 1]))
                    {
                        // not on an edge
                        goto nextGramContinue;
                    }
                    for (int j = start; j < end; ++j)
                    {
                        if (!isTokenChar(nonTokenChars, codePoints[j]))
                        {
                            goto nextGramContinue;
                        }
                    }
                    assertTrue(grams.IncrementToken());
                    assertArrayEquals(Arrays.CopyOfRange(codePoints, start, end), toCodePoints(termAtt));
                    assertEquals(1, posIncAtt.PositionIncrement);
                    assertEquals(1, posLenAtt.PositionLength);
                    assertEquals(offsets[start], offsetAtt.StartOffset);
                    assertEquals(offsets[end], offsetAtt.EndOffset);
                    nextGramContinue:;
                }
                //nextGramBreak:;
            }
            assertFalse(grams.IncrementToken());
            grams.End();
            assertEquals(s.Length, offsetAtt.StartOffset);
            assertEquals(s.Length, offsetAtt.EndOffset);
        }

        private sealed class NGramTokenizerAnonymousClass : NGramTokenizer
        {
            private readonly string nonTokenChars;

            public NGramTokenizerAnonymousClass(LuceneVersion TEST_VERSION_CURRENT, StringReader java, int minGram, int maxGram, bool edgesOnly, string nonTokenChars)
                  : base(TEST_VERSION_CURRENT, java, minGram, maxGram, edgesOnly)
            {
                this.nonTokenChars = nonTokenChars;
            }

            protected override bool IsTokenChar(int chr)
            {
                return nonTokenChars.IndexOf(chr) < 0;
            }
        }

        [Test]
        public virtual void TestLargeInput()
        {
            // test sliding
            int minGram = TestUtil.NextInt32(Random, 1, 100);
            int maxGram = TestUtil.NextInt32(Random, minGram, 100);
            TestNGrams(minGram, maxGram, TestUtil.NextInt32(Random, 3 * 1024, 4 * 1024), "");
        }

        [Test]
        public virtual void TestLargeMaxGram()
        {
            // test sliding with maxGram > 1024
            int minGram = TestUtil.NextInt32(Random, 1290, 1300);
            int maxGram = TestUtil.NextInt32(Random, minGram, 1300);
            TestNGrams(minGram, maxGram, TestUtil.NextInt32(Random, 3 * 1024, 4 * 1024), "");
        }

        [Test]
        public virtual void TestPreTokenization()
        {
            int minGram = TestUtil.NextInt32(Random, 1, 100);
            int maxGram = TestUtil.NextInt32(Random, minGram, 100);
            TestNGrams(minGram, maxGram, TestUtil.NextInt32(Random, 0, 4 * 1024), "a");
        }

        [Test]
        public virtual void TestHeavyPreTokenization()
        {
            int minGram = TestUtil.NextInt32(Random, 1, 100);
            int maxGram = TestUtil.NextInt32(Random, minGram, 100);
            TestNGrams(minGram, maxGram, TestUtil.NextInt32(Random, 0, 4 * 1024), "abcdef");
        }

        [Test]
        public virtual void TestFewTokenChars()
        {
            char[] chrs = new char[TestUtil.NextInt32(Random, 4000, 5000)];
            Arrays.Fill(chrs, ' ');
            for (int i = 0; i < chrs.Length; ++i)
            {
                if (Random.NextDouble() < 0.1)
                {
                    chrs[i] = 'a';
                }
            }
            int minGram = TestUtil.NextInt32(Random, 1, 2);
            int maxGram = TestUtil.NextInt32(Random, minGram, 2);
            TestNGrams(minGram, maxGram, new string(chrs), " ");
        }

        [Test]
        public virtual void TestFullUTF8Range()
        {
            int minGram = TestUtil.NextInt32(Random, 1, 100);
            int maxGram = TestUtil.NextInt32(Random, minGram, 100);
            string s = TestUtil.RandomUnicodeString(Random, 4 * 1024);
            TestNGrams(minGram, maxGram, s, "");
            TestNGrams(minGram, maxGram, s, "abcdef");
        }
    }
}