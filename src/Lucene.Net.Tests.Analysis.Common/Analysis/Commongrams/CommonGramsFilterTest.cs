// Lucene version compatibility level 4.8.1
using Lucene.Net.Analysis.Core;
using Lucene.Net.Analysis.TokenAttributes;
using Lucene.Net.Analysis.Util;
using NUnit.Framework;
using System.IO;

namespace Lucene.Net.Analysis.CommonGrams
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
    /// Tests CommonGrams(Query)Filter
    /// </summary>
    public class CommonGramsFilterTest : BaseTokenStreamTestCase
    {
        private static readonly CharArraySet commonWords = new CharArraySet(TEST_VERSION_CURRENT, new string[] { "s", "a", "b", "c", "d", "the", "of" }, false);

        [Test]
        public virtual void TestReset()
        {
            const string input = "How the s a brown s cow d like A B thing?";
            WhitespaceTokenizer wt = new WhitespaceTokenizer(TEST_VERSION_CURRENT, new StringReader(input));
            CommonGramsFilter cgf = new CommonGramsFilter(TEST_VERSION_CURRENT, wt, commonWords);

            ICharTermAttribute term = cgf.AddAttribute<ICharTermAttribute>();
            cgf.Reset();
            assertTrue(cgf.IncrementToken());
            assertEquals("How", term.ToString());
            assertTrue(cgf.IncrementToken());
            assertEquals("How_the", term.ToString());
            assertTrue(cgf.IncrementToken());
            assertEquals("the", term.ToString());
            assertTrue(cgf.IncrementToken());
            assertEquals("the_s", term.ToString());
            cgf.Dispose();

            wt.SetReader(new StringReader(input));
            cgf.Reset();
            assertTrue(cgf.IncrementToken());
            assertEquals("How", term.ToString());
        }

        [Test]
        public virtual void TestQueryReset()
        {
            const string input = "How the s a brown s cow d like A B thing?";
            WhitespaceTokenizer wt = new WhitespaceTokenizer(TEST_VERSION_CURRENT, new StringReader(input));
            CommonGramsFilter cgf = new CommonGramsFilter(TEST_VERSION_CURRENT, wt, commonWords);
            CommonGramsQueryFilter nsf = new CommonGramsQueryFilter(cgf);

            ICharTermAttribute term = wt.AddAttribute<ICharTermAttribute>();
            nsf.Reset();
            assertTrue(nsf.IncrementToken());
            assertEquals("How_the", term.ToString());
            assertTrue(nsf.IncrementToken());
            assertEquals("the_s", term.ToString());
            nsf.Dispose();

            wt.SetReader(new StringReader(input));
            nsf.Reset();
            assertTrue(nsf.IncrementToken());
            assertEquals("How_the", term.ToString());
        }

        /// <summary>
        /// This is for testing CommonGramsQueryFilter which outputs a set of tokens
        /// optimized for querying with only one token at each position, either a
        /// unigram or a bigram It also will not return a token for the final position
        /// if the final word is already in the preceding bigram Example:(three
        /// tokens/positions in)
        /// "foo bar the"=>"foo:1|bar:2,bar-the:2|the:3=> "foo" "bar-the" (2 tokens
        /// out)
        /// 
        /// </summary>
        [Test]
        public virtual void TestCommonGramsQueryFilter()
        {
            Analyzer a = Analyzer.NewAnonymous(createComponents: (fieldName, reader) =>
            {
                Tokenizer tokenizer = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
                return new TokenStreamComponents(tokenizer, new CommonGramsQueryFilter(new CommonGramsFilter(TEST_VERSION_CURRENT, tokenizer, commonWords)));
            });

            // Stop words used below are "of" "the" and "s"

            // two word queries
            AssertAnalyzesTo(a, "brown fox", new string[] { "brown", "fox" });
            AssertAnalyzesTo(a, "the fox", new string[] { "the_fox" });
            AssertAnalyzesTo(a, "fox of", new string[] { "fox_of" });
            AssertAnalyzesTo(a, "of the", new string[] { "of_the" });

            // one word queries
            AssertAnalyzesTo(a, "the", new string[] { "the" });
            AssertAnalyzesTo(a, "foo", new string[] { "foo" });

            // 3 word combinations s=stopword/common word n=not a stop word
            AssertAnalyzesTo(a, "n n n", new string[] { "n", "n", "n" });
            AssertAnalyzesTo(a, "quick brown fox", new string[] { "quick", "brown", "fox" });

            AssertAnalyzesTo(a, "n n s", new string[] { "n", "n_s" });
            AssertAnalyzesTo(a, "quick brown the", new string[] { "quick", "brown_the" });

            AssertAnalyzesTo(a, "n s n", new string[] { "n_s", "s_n" });
            AssertAnalyzesTo(a, "quick the brown", new string[] { "quick_the", "the_brown" });

            AssertAnalyzesTo(a, "n s s", new string[] { "n_s", "s_s" });
            AssertAnalyzesTo(a, "fox of the", new string[] { "fox_of", "of_the" });

            AssertAnalyzesTo(a, "s n n", new string[] { "s_n", "n", "n" });
            AssertAnalyzesTo(a, "the quick brown", new string[] { "the_quick", "quick", "brown" });

            AssertAnalyzesTo(a, "s n s", new string[] { "s_n", "n_s" });
            AssertAnalyzesTo(a, "the fox of", new string[] { "the_fox", "fox_of" });

            AssertAnalyzesTo(a, "s s n", new string[] { "s_s", "s_n" });
            AssertAnalyzesTo(a, "of the fox", new string[] { "of_the", "the_fox" });

            AssertAnalyzesTo(a, "s s s", new string[] { "s_s", "s_s" });
            AssertAnalyzesTo(a, "of the of", new string[] { "of_the", "the_of" });
        }

        [Test]
        public virtual void TestCommonGramsFilter()
        {
            Analyzer a = Analyzer.NewAnonymous(createComponents: (fieldName, reader) =>
            {
                Tokenizer tokenizer = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
                return new TokenStreamComponents(tokenizer, new CommonGramsFilter(TEST_VERSION_CURRENT, tokenizer, commonWords));
            });

            // Stop words used below are "of" "the" and "s"
            // one word queries
            AssertAnalyzesTo(a, "the", new string[] { "the" });
            AssertAnalyzesTo(a, "foo", new string[] { "foo" });

            // two word queries
            AssertAnalyzesTo(a, "brown fox", new string[] { "brown", "fox" }, new int[] { 1, 1 });
            AssertAnalyzesTo(a, "the fox", new string[] { "the", "the_fox", "fox" }, new int[] { 1, 0, 1 });
            AssertAnalyzesTo(a, "fox of", new string[] { "fox", "fox_of", "of" }, new int[] { 1, 0, 1 });
            AssertAnalyzesTo(a, "of the", new string[] { "of", "of_the", "the" }, new int[] { 1, 0, 1 });

            // 3 word combinations s=stopword/common word n=not a stop word
            AssertAnalyzesTo(a, "n n n", new string[] { "n", "n", "n" }, new int[] { 1, 1, 1 });
            AssertAnalyzesTo(a, "quick brown fox", new string[] { "quick", "brown", "fox" }, new int[] { 1, 1, 1 });

            AssertAnalyzesTo(a, "n n s", new string[] { "n", "n", "n_s", "s" }, new int[] { 1, 1, 0, 1 });
            AssertAnalyzesTo(a, "quick brown the", new string[] { "quick", "brown", "brown_the", "the" }, new int[] { 1, 1, 0, 1 });

            AssertAnalyzesTo(a, "n s n", new string[] { "n", "n_s", "s", "s_n", "n" }, new int[] { 1, 0, 1, 0, 1 });
            AssertAnalyzesTo(a, "quick the fox", new string[] { "quick", "quick_the", "the", "the_fox", "fox" }, new int[] { 1, 0, 1, 0, 1 });

            AssertAnalyzesTo(a, "n s s", new string[] { "n", "n_s", "s", "s_s", "s" }, new int[] { 1, 0, 1, 0, 1 });
            AssertAnalyzesTo(a, "fox of the", new string[] { "fox", "fox_of", "of", "of_the", "the" }, new int[] { 1, 0, 1, 0, 1 });

            AssertAnalyzesTo(a, "s n n", new string[] { "s", "s_n", "n", "n" }, new int[] { 1, 0, 1, 1 });
            AssertAnalyzesTo(a, "the quick brown", new string[] { "the", "the_quick", "quick", "brown" }, new int[] { 1, 0, 1, 1 });

            AssertAnalyzesTo(a, "s n s", new string[] { "s", "s_n", "n", "n_s", "s" }, new int[] { 1, 0, 1, 0, 1 });
            AssertAnalyzesTo(a, "the fox of", new string[] { "the", "the_fox", "fox", "fox_of", "of" }, new int[] { 1, 0, 1, 0, 1 });

            AssertAnalyzesTo(a, "s s n", new string[] { "s", "s_s", "s", "s_n", "n" }, new int[] { 1, 0, 1, 0, 1 });
            AssertAnalyzesTo(a, "of the fox", new string[] { "of", "of_the", "the", "the_fox", "fox" }, new int[] { 1, 0, 1, 0, 1 });

            AssertAnalyzesTo(a, "s s s", new string[] { "s", "s_s", "s", "s_s", "s" }, new int[] { 1, 0, 1, 0, 1 });
            AssertAnalyzesTo(a, "of the of", new string[] { "of", "of_the", "the", "the_of", "of" }, new int[] { 1, 0, 1, 0, 1 });
        }

        /// <summary>
        /// Test that CommonGramsFilter works correctly in case-insensitive mode
        /// </summary>
        [Test]
        public virtual void TestCaseSensitive()
        {
            const string input = "How The s a brown s cow d like A B thing?";
            MockTokenizer wt = new MockTokenizer(new StringReader(input), MockTokenizer.WHITESPACE, false);
            TokenFilter cgf = new CommonGramsFilter(TEST_VERSION_CURRENT, wt, commonWords);
            AssertTokenStreamContents(cgf, new string[] { "How", "The", "The_s", "s", "s_a", "a", "a_brown", "brown", "brown_s", "s", "s_cow", "cow", "cow_d", "d", "d_like", "like", "A", "B", "thing?" });
        }

        /// <summary>
        /// Test CommonGramsQueryFilter in the case that the last word is a stopword
        /// </summary>
        [Test]
        public virtual void TestLastWordisStopWord()
        {
            const string input = "dog the";
            MockTokenizer wt = new MockTokenizer(new StringReader(input), MockTokenizer.WHITESPACE, false);
            CommonGramsFilter cgf = new CommonGramsFilter(TEST_VERSION_CURRENT, wt, commonWords);
            TokenFilter nsf = new CommonGramsQueryFilter(cgf);
            AssertTokenStreamContents(nsf, new string[] { "dog_the" });
        }

        /// <summary>
        /// Test CommonGramsQueryFilter in the case that the first word is a stopword
        /// </summary>
        [Test]
        public virtual void TestFirstWordisStopWord()
        {
            const string input = "the dog";
            MockTokenizer wt = new MockTokenizer(new StringReader(input), MockTokenizer.WHITESPACE, false);
            CommonGramsFilter cgf = new CommonGramsFilter(TEST_VERSION_CURRENT, wt, commonWords);
            TokenFilter nsf = new CommonGramsQueryFilter(cgf);
            AssertTokenStreamContents(nsf, new string[] { "the_dog" });
        }

        /// <summary>
        /// Test CommonGramsQueryFilter in the case of a single (stop)word query
        /// </summary>
        [Test]
        public virtual void TestOneWordQueryStopWord()
        {
            const string input = "the";
            MockTokenizer wt = new MockTokenizer(new StringReader(input), MockTokenizer.WHITESPACE, false);
            CommonGramsFilter cgf = new CommonGramsFilter(TEST_VERSION_CURRENT, wt, commonWords);
            TokenFilter nsf = new CommonGramsQueryFilter(cgf);
            AssertTokenStreamContents(nsf, new string[] { "the" });
        }

        /// <summary>
        /// Test CommonGramsQueryFilter in the case of a single word query
        /// </summary>
        [Test]
        public virtual void TestOneWordQuery()
        {
            const string input = "monster";
            MockTokenizer wt = new MockTokenizer(new StringReader(input), MockTokenizer.WHITESPACE, false);
            CommonGramsFilter cgf = new CommonGramsFilter(TEST_VERSION_CURRENT, wt, commonWords);
            TokenFilter nsf = new CommonGramsQueryFilter(cgf);
            AssertTokenStreamContents(nsf, new string[] { "monster" });
        }

        /// <summary>
        /// Test CommonGramsQueryFilter when first and last words are stopwords.
        /// </summary>
        [Test]
        public virtual void TestFirstAndLastStopWord()
        {
            const string input = "the of";
            MockTokenizer wt = new MockTokenizer(new StringReader(input), MockTokenizer.WHITESPACE, false);
            CommonGramsFilter cgf = new CommonGramsFilter(TEST_VERSION_CURRENT, wt, commonWords);
            TokenFilter nsf = new CommonGramsQueryFilter(cgf);
            AssertTokenStreamContents(nsf, new string[] { "the_of" });
        }

        /// <summary>
        /// blast some random strings through the analyzer </summary>
        [Test]
        public virtual void TestRandomStrings()
        {
            Analyzer a = Analyzer.NewAnonymous(createComponents: (fieldName, reader) =>
            {
                Tokenizer t = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
                CommonGramsFilter cgf = new CommonGramsFilter(TEST_VERSION_CURRENT, t, commonWords);
                return new TokenStreamComponents(t, cgf);
            });

            CheckRandomData(Random, a, 1000 * RandomMultiplier);

            Analyzer b = Analyzer.NewAnonymous(createComponents: (fieldName, reader) =>
            {
                Tokenizer t = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
                CommonGramsFilter cgf = new CommonGramsFilter(TEST_VERSION_CURRENT, t, commonWords);
                return new TokenStreamComponents(t, new CommonGramsQueryFilter(cgf));
            });

            CheckRandomData(Random, b, 1000 * RandomMultiplier);
        }
    }
}