using Lucene.Net.Support;
using Lucene.Net.Util;
using NUnit.Framework;
using System.IO;
using Reader = System.IO.TextReader;
using Version = Lucene.Net.Util.LuceneVersion;

namespace Lucene.Net.Analysis.Ngram
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
    /// Tests <seealso cref="EdgeNGramTokenizer"/> for correctness.
    /// </summary>
    public class EdgeNGramTokenizerTest : BaseTokenStreamTestCase
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
                new EdgeNGramTokenizer(TEST_VERSION_CURRENT, input, 0, 0);
            }
            catch (System.ArgumentException)
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
                new EdgeNGramTokenizer(TEST_VERSION_CURRENT, input, 2, 1);
            }
            catch (System.ArgumentException)
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
                new EdgeNGramTokenizer(TEST_VERSION_CURRENT, input, -1, 2);
            }
            catch (System.ArgumentException)
            {
                gotException = true;
            }
            assertTrue(gotException);
        }

        [Test]
        public virtual void TestFrontUnigram()
        {
            EdgeNGramTokenizer tokenizer = new EdgeNGramTokenizer(TEST_VERSION_CURRENT, input, 1, 1);
            AssertTokenStreamContents(tokenizer, new string[] { "a" }, new int[] { 0 }, new int[] { 1 }, 5); // abcde
        }

        [Test]
        public virtual void TestBackUnigram()
        {
#pragma warning disable 612, 618
            Tokenizer tokenizer = new Lucene43EdgeNGramTokenizer(Version.LUCENE_43, input, Lucene43EdgeNGramTokenizer.Side.BACK, 1, 1);
#pragma warning restore 612, 618
            AssertTokenStreamContents(tokenizer, new string[] { "e" }, new int[] { 4 }, new int[] { 5 }, 5); // abcde
        }

        [Test]
        public virtual void TestOversizedNgrams()
        {
            EdgeNGramTokenizer tokenizer = new EdgeNGramTokenizer(TEST_VERSION_CURRENT, input, 6, 6);
            AssertTokenStreamContents(tokenizer, new string[0], new int[0], new int[0], 5); // abcde
        }

        [Test]
        public virtual void TestFrontRangeOfNgrams()
        {
            EdgeNGramTokenizer tokenizer = new EdgeNGramTokenizer(TEST_VERSION_CURRENT, input, 1, 3);
            AssertTokenStreamContents(tokenizer, new string[] { "a", "ab", "abc" }, new int[] { 0, 0, 0 }, new int[] { 1, 2, 3 }, 5); // abcde
        }

        [Test]
        public virtual void TestBackRangeOfNgrams()
        {
#pragma warning disable 612, 618
            Tokenizer tokenizer = new Lucene43EdgeNGramTokenizer(Version.LUCENE_43, input, Lucene43EdgeNGramTokenizer.Side.BACK, 1, 3);
#pragma warning restore 612, 618
            AssertTokenStreamContents(tokenizer, new string[] { "e", "de", "cde" }, new int[] { 4, 3, 2 }, new int[] { 5, 5, 5 }, null, null, null, 5, false); // abcde
        }

        [Test]
        public virtual void TestReset()
        {
            EdgeNGramTokenizer tokenizer = new EdgeNGramTokenizer(TEST_VERSION_CURRENT, input, 1, 3);
            AssertTokenStreamContents(tokenizer, new string[] { "a", "ab", "abc" }, new int[] { 0, 0, 0 }, new int[] { 1, 2, 3 }, 5); // abcde
            tokenizer.Reader = new StringReader("abcde");
            AssertTokenStreamContents(tokenizer, new string[] { "a", "ab", "abc" }, new int[] { 0, 0, 0 }, new int[] { 1, 2, 3 }, 5); // abcde
        }

        /// <summary>
        /// blast some random strings through the analyzer </summary>
        [Test]
        public virtual void TestRandomStrings()
        {
            for (int i = 0; i < 10; i++)
            {
                int min = TestUtil.NextInt(Random(), 2, 10);
                int max = TestUtil.NextInt(Random(), min, 20);

                Analyzer a = new AnalyzerAnonymousInnerClassHelper(this, min, max);
                CheckRandomData(Random(), a, 100 * RANDOM_MULTIPLIER, 20);
                CheckRandomData(Random(), a, 10 * RANDOM_MULTIPLIER, 8192);
            }

            Analyzer b = new AnalyzerAnonymousInnerClassHelper2(this);
            CheckRandomData(Random(), b, 1000 * RANDOM_MULTIPLIER, 20, false, false);
            CheckRandomData(Random(), b, 100 * RANDOM_MULTIPLIER, 8192, false, false);
        }

        private class AnalyzerAnonymousInnerClassHelper : Analyzer
        {
            private readonly EdgeNGramTokenizerTest outerInstance;

            private int min;
            private int max;

            public AnalyzerAnonymousInnerClassHelper(EdgeNGramTokenizerTest outerInstance, int min, int max)
            {
                this.outerInstance = outerInstance;
                this.min = min;
                this.max = max;
            }

            protected override TokenStreamComponents CreateComponents(string fieldName, Reader reader)
            {
                Tokenizer tokenizer = new EdgeNGramTokenizer(TEST_VERSION_CURRENT, reader, min, max);
                return new TokenStreamComponents(tokenizer, tokenizer);
            }
        }

        private class AnalyzerAnonymousInnerClassHelper2 : Analyzer
        {
            private readonly EdgeNGramTokenizerTest outerInstance;

            public AnalyzerAnonymousInnerClassHelper2(EdgeNGramTokenizerTest outerInstance)
            {
                this.outerInstance = outerInstance;
            }

            protected override TokenStreamComponents CreateComponents(string fieldName, Reader reader)
            {
#pragma warning disable 612, 618
                Tokenizer tokenizer = new Lucene43EdgeNGramTokenizer(Version.LUCENE_43, reader, Lucene43EdgeNGramTokenizer.Side.BACK, 2, 4);
#pragma warning restore 612, 618
                return new TokenStreamComponents(tokenizer, tokenizer);
            }
        }

        [Test]
        public virtual void TestTokenizerPositions()
        {
#pragma warning disable 612, 618
            Tokenizer tokenizer = new Lucene43EdgeNGramTokenizer(Version.LUCENE_43, input, Lucene43EdgeNGramTokenizer.Side.FRONT, 1, 3);
#pragma warning restore 612, 618
            AssertTokenStreamContents(tokenizer, new string[] { "a", "ab", "abc" }, new int[] { 0, 0, 0 }, new int[] { 1, 2, 3 }, null, new int[] { 1, 0, 0 }, null, null, false);

            tokenizer = new EdgeNGramTokenizer(TEST_VERSION_CURRENT, new StringReader("abcde"), 1, 3);
            AssertTokenStreamContents(tokenizer, new string[] { "a", "ab", "abc" }, new int[] { 0, 0, 0 }, new int[] { 1, 2, 3 }, null, new int[] { 1, 1, 1 }, null, null, false);
        }

        private static void TestNGrams(int minGram, int maxGram, int length, string nonTokenChars)
        {
            // LUCENENET TODO: Changed randomizing strategy - not sure if this is right...
            //string s = RandomStrings.randomAsciiOfLength(Random(), length);
            string s = TestUtil.RandomAnalysisString(Random(), length, true);
            TestNGrams(minGram, maxGram, s, nonTokenChars);
        }

        private static void TestNGrams(int minGram, int maxGram, string s, string nonTokenChars)
        {
            NGramTokenizerTest.TestNGrams(minGram, maxGram, s, nonTokenChars, true);
        }

        [Test]
        public virtual void TestLargeInput()
        {
            // test sliding
            int minGram = TestUtil.NextInt(Random(), 1, 100);
            int maxGram = TestUtil.NextInt(Random(), minGram, 100);
            TestNGrams(minGram, maxGram, TestUtil.NextInt(Random(), 3 * 1024, 4 * 1024), "");
        }

        [Test]
        public virtual void TestLargeMaxGram()
        {
            // test sliding with maxGram > 1024
            int minGram = TestUtil.NextInt(Random(), 1290, 1300);
            int maxGram = TestUtil.NextInt(Random(), minGram, 1300);
            TestNGrams(minGram, maxGram, TestUtil.NextInt(Random(), 3 * 1024, 4 * 1024), "");
        }

        [Test]
        public virtual void TestPreTokenization()
        {
            int minGram = TestUtil.NextInt(Random(), 1, 100);
            int maxGram = TestUtil.NextInt(Random(), minGram, 100);
            TestNGrams(minGram, maxGram, TestUtil.NextInt(Random(), 0, 4 * 1024), "a");
        }

        [Test]
        public virtual void TestHeavyPreTokenization()
        {
            int minGram = TestUtil.NextInt(Random(), 1, 100);
            int maxGram = TestUtil.NextInt(Random(), minGram, 100);
            TestNGrams(minGram, maxGram, TestUtil.NextInt(Random(), 0, 4 * 1024), "abcdef");
        }

        [Test]
        public virtual void TestFewTokenChars()
        {
            char[] chrs = new char[TestUtil.NextInt(Random(), 4000, 5000)];
            Arrays.Fill(chrs, ' ');
            for (int i = 0; i < chrs.Length; ++i)
            {
                if (Random().NextDouble() < 0.1)
                {
                    chrs[i] = 'a';
                }
            }
            int minGram = TestUtil.NextInt(Random(), 1, 2);
            int maxGram = TestUtil.NextInt(Random(), minGram, 2);
            TestNGrams(minGram, maxGram, new string(chrs), " ");
        }

        [Test]
        public virtual void TestFullUTF8Range()
        {
            int minGram = TestUtil.NextInt(Random(), 1, 100);
            int maxGram = TestUtil.NextInt(Random(), minGram, 100);
            string s = TestUtil.RandomUnicodeString(Random(), 4 * 1024);
            TestNGrams(minGram, maxGram, s, "");
            TestNGrams(minGram, maxGram, s, "abcdef");
        }
    }
}