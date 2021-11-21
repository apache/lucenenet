// Lucene version compatibility level 4.8.1
using J2N;
using Lucene.Net.Util;
using NUnit.Framework;
using RandomizedTesting.Generators;
using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Console = Lucene.Net.Util.SystemConsole;

namespace Lucene.Net.Analysis.Pattern
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
    /// Tests <seealso cref="PatternReplaceCharFilter"/>
    /// </summary>
    public class TestPatternReplaceCharFilter : BaseTokenStreamTestCase
    {
        [Test]
        public virtual void TestFailingDot()
        {
            checkOutput("A. .B.", "\\.[\\s]*", ".", "A..B.", "A..B.");
        }

        [Test]
        public virtual void TestLongerReplacement()
        {
            checkOutput("XXabcZZabcYY", "abc", "abcde", "XXabcdeZZabcdeYY", "XXabcccZZabcccYY");
            checkOutput("XXabcabcYY", "abc", "abcde", "XXabcdeabcdeYY", "XXabcccabcccYY");
            checkOutput("abcabcYY", "abc", "abcde", "abcdeabcdeYY", "abcccabcccYY");
            checkOutput("YY", "^", "abcde", "abcdeYY", "YYYYYYY");
            // Should be: "-----YY" but we're enforcing non-negative offsets.
            checkOutput("YY", "$", "abcde", "YYabcde", "YYYYYYY");
            checkOutput("XYZ", ".", "abc", "abcabcabc", "XXXYYYZZZ");
            checkOutput("XYZ", ".", "$0abc", "XabcYabcZabc", "XXXXYYYYZZZZ");
        }

        [Test]
        public virtual void TestShorterReplacement()
        {
            checkOutput("XXabcZZabcYY", "abc", "xy", "XXxyZZxyYY", "XXabZZabYY");
            checkOutput("XXabcabcYY", "abc", "xy", "XXxyxyYY", "XXababYY");
            checkOutput("abcabcYY", "abc", "xy", "xyxyYY", "ababYY");
            checkOutput("abcabcYY", "abc", "", "YY", "YY");
            checkOutput("YYabcabc", "abc", "", "YY", "YY");
        }

        private void checkOutput(string input, string pattern, string replacement, string expectedOutput, string expectedIndexMatchedOutput)
        {
            CharFilter cs = new PatternReplaceCharFilter(new Regex(pattern, RegexOptions.Compiled), replacement, new StringReader(input));

            StringBuilder output = new StringBuilder();
            for (int chr = cs.Read(); chr > 0; chr = cs.Read())
            {
                output.Append((char)chr);
            }

            StringBuilder indexMatched = new StringBuilder();
            for (int i = 0; i < output.Length; i++)
            {
                if (cs.CorrectOffset(i) < input.Length)
                {
                    indexMatched.Append((cs.CorrectOffset(i) < 0 ? '-' : input[cs.CorrectOffset(i)]));
                }
            }

            bool outputGood = expectedOutput.Equals(output.ToString(), StringComparison.Ordinal);
            bool indexMatchedGood = expectedIndexMatchedOutput.Equals(indexMatched.ToString(), StringComparison.Ordinal);

            if (!outputGood || !indexMatchedGood || false)
            {
                Console.WriteLine("Pattern : " + pattern);
                Console.WriteLine("Replac. : " + replacement);
                Console.WriteLine("Input   : " + input);
                Console.WriteLine("Output  : " + output);
                Console.WriteLine("Expected: " + expectedOutput);
                Console.WriteLine("Output/i: " + indexMatched);
                Console.WriteLine("Expected: " + expectedIndexMatchedOutput);
                Console.WriteLine();
            }

            assertTrue("Output doesn't match.", outputGood);
            assertTrue("Index-matched output doesn't match.", indexMatchedGood);
        }

        //           1111
        // 01234567890123
        // this is test.
        [Test]
        public virtual void TestNothingChange()
        {
            const string BLOCK = "this is test.";
            CharFilter cs = new PatternReplaceCharFilter(pattern("(aa)\\s+(bb)\\s+(cc)"), "$1$2$3", new StringReader(BLOCK));
            TokenStream ts = new MockTokenizer(cs, MockTokenizer.WHITESPACE, false);
            AssertTokenStreamContents(ts, new string[] { "this", "is", "test." }, new int[] { 0, 5, 8 }, new int[] { 4, 7, 13 }, BLOCK.Length);
        }

        // 012345678
        // aa bb cc
        [Test]
        public virtual void TestReplaceByEmpty()
        {
            const string BLOCK = "aa bb cc";
            CharFilter cs = new PatternReplaceCharFilter(pattern("(aa)\\s+(bb)\\s+(cc)"), "", new StringReader(BLOCK));
            TokenStream ts = new MockTokenizer(cs, MockTokenizer.WHITESPACE, false);
            AssertTokenStreamContents(ts, new string[] { });
        }

        // 012345678
        // aa bb cc
        // aa#bb#cc
        [Test]
        public virtual void Test1block1matchSameLength()
        {
            const string BLOCK = "aa bb cc";
            CharFilter cs = new PatternReplaceCharFilter(pattern("(aa)\\s+(bb)\\s+(cc)"), "$1#$2#$3", new StringReader(BLOCK));
            TokenStream ts = new MockTokenizer(cs, MockTokenizer.WHITESPACE, false);
            AssertTokenStreamContents(ts, new string[] { "aa#bb#cc" }, new int[] { 0 }, new int[] { 8 }, BLOCK.Length);
        }

        //           11111
        // 012345678901234
        // aa bb cc dd
        // aa##bb###cc dd
        [Test]
        public virtual void Test1block1matchLonger()
        {
            const string BLOCK = "aa bb cc dd";
            CharFilter cs = new PatternReplaceCharFilter(pattern("(aa)\\s+(bb)\\s+(cc)"), "$1##$2###$3", new StringReader(BLOCK));
            TokenStream ts = new MockTokenizer(cs, MockTokenizer.WHITESPACE, false);
            AssertTokenStreamContents(ts, new string[] { "aa##bb###cc", "dd" }, new int[] { 0, 9 }, new int[] { 8, 11 }, BLOCK.Length);
        }

        // 01234567
        //  a  a
        //  aa  aa
        [Test]
        public virtual void Test1block2matchLonger()
        {
            const string BLOCK = " a  a";
            CharFilter cs = new PatternReplaceCharFilter(pattern("a"), "aa", new StringReader(BLOCK));
            TokenStream ts = new MockTokenizer(cs, MockTokenizer.WHITESPACE, false);
            AssertTokenStreamContents(ts, new string[] { "aa", "aa" }, new int[] { 1, 4 }, new int[] { 2, 5 }, BLOCK.Length);
        }

        //           11111
        // 012345678901234
        // aa  bb   cc dd
        // aa#bb dd
        [Test]
        public virtual void Test1block1matchShorter()
        {
            const string BLOCK = "aa  bb   cc dd";
            CharFilter cs = new PatternReplaceCharFilter(pattern("(aa)\\s+(bb)\\s+(cc)"), "$1#$2", new StringReader(BLOCK));
            TokenStream ts = new MockTokenizer(cs, MockTokenizer.WHITESPACE, false);
            AssertTokenStreamContents(ts, new string[] { "aa#bb", "dd" }, new int[] { 0, 12 }, new int[] { 11, 14 }, BLOCK.Length);
        }

        //           111111111122222222223333
        // 0123456789012345678901234567890123
        //   aa bb cc --- aa bb aa   bb   cc
        //   aa  bb  cc --- aa bb aa  bb  cc
        [Test]
        public virtual void Test1blockMultiMatches()
        {
            const string BLOCK = "  aa bb cc --- aa bb aa   bb   cc";
            CharFilter cs = new PatternReplaceCharFilter(pattern("(aa)\\s+(bb)\\s+(cc)"), "$1  $2  $3", new StringReader(BLOCK));
            TokenStream ts = new MockTokenizer(cs, MockTokenizer.WHITESPACE, false);
            AssertTokenStreamContents(ts, new string[] { "aa", "bb", "cc", "---", "aa", "bb", "aa", "bb", "cc" }, new int[] { 2, 6, 9, 11, 15, 18, 21, 25, 29 }, new int[] { 4, 8, 10, 14, 17, 20, 23, 27, 33 }, BLOCK.Length);
        }

        //           11111111112222222222333333333
        // 012345678901234567890123456789012345678
        //   aa bb cc --- aa bb aa. bb aa   bb cc
        //   aa##bb cc --- aa##bb aa. bb aa##bb cc

        //   aa bb cc --- aa bbbaa. bb aa   b cc

        [Test]
        public virtual void Test2blocksMultiMatches()
        {
            const string BLOCK = "  aa bb cc --- aa bb aa. bb aa   bb cc";

            CharFilter cs = new PatternReplaceCharFilter(pattern("(aa)\\s+(bb)"), "$1##$2", new StringReader(BLOCK));
            TokenStream ts = new MockTokenizer(cs, MockTokenizer.WHITESPACE, false);
            AssertTokenStreamContents(ts, new string[] { "aa##bb", "cc", "---", "aa##bb", "aa.", "bb", "aa##bb", "cc" }, new int[] { 2, 8, 11, 15, 21, 25, 28, 36 }, new int[] { 7, 10, 14, 20, 24, 27, 35, 38 }, BLOCK.Length);
        }

        //           11111111112222222222333333333
        // 012345678901234567890123456789012345678
        //  a bb - ccc . --- bb a . ccc ccc bb
        //  aa b - c . --- b aa . c c b
        [Test]
        public virtual void TestChain()
        {
            const string BLOCK = " a bb - ccc . --- bb a . ccc ccc bb";
            CharFilter cs = new PatternReplaceCharFilter(pattern("a"), "aa", new StringReader(BLOCK));
            cs = new PatternReplaceCharFilter(pattern("bb"), "b", cs);
            cs = new PatternReplaceCharFilter(pattern("ccc"), "c", cs);
            TokenStream ts = new MockTokenizer(cs, MockTokenizer.WHITESPACE, false);
            AssertTokenStreamContents(ts, new string[] { "aa", "b", "-", "c", ".", "---", "b", "aa", ".", "c", "c", "b" }, new int[] { 1, 3, 6, 8, 12, 14, 18, 21, 23, 25, 29, 33 }, new int[] { 2, 5, 7, 11, 13, 17, 20, 22, 24, 28, 32, 35 }, BLOCK.Length);
        }

        private Regex pattern(string p)
        {
            return new Regex(p, RegexOptions.Compiled);
        }

        /// <summary>
        /// A demonstration of how backtracking regular expressions can lead to relatively 
        /// easy DoS attacks.
        /// </summary>
        /// <seealso cref= "http://swtch.com/~rsc/regexp/regexp1.html" </seealso>
        [Test]
        [Ignore("Ignored in Lucene")]
        public virtual void TestNastyPattern()
        {
            Regex p = new Regex("(c.+)*xy", RegexOptions.Compiled);
            string input = "[;<!--aecbbaa--><    febcfdc fbb = \"fbeeebff\" fc = dd   >\\';<eefceceaa e= babae\" eacbaff =\"fcfaccacd\" = bcced>>><  bccaafe edb = ecfccdff\"   <?</script><    edbd ebbcd=\"faacfcc\" aeca= bedbc ceeaac =adeafde aadccdaf = \"afcc ffda=aafbe &#x16921ed5\"1843785582']";
            for (int i = 0; i < input.Length; i++)
            {
                Match matcher = p.Match(input.Substring(0, i));
                long t = Time.NanoTime() / Time.MillisecondsPerNanosecond; // LUCENENET: Use NanoTime() rather than CurrentTimeMilliseconds() for more accurate/reliable results
                if (matcher.Success)
                {
                    Console.WriteLine(matcher.Groups[1]);
                }
                Console.WriteLine(i + " > " + (Time.NanoTime() / Time.MillisecondsPerNanosecond - t) / 1000.0); // LUCENENET: Use NanoTime() rather than CurrentTimeMilliseconds() for more accurate/reliable results
            }
        }

        /// <summary>
        /// blast some random strings through the analyzer </summary>
        [Test]
        [Slow]
        public virtual void TestRandomStrings()
        {
            int numPatterns = 10 + Random.Next(20);
            Random random = new J2N.Randomizer(Random.NextInt64());
            for (int i = 0; i < numPatterns; i++)
            {
                Regex p = TestUtil.RandomRegex(Random);

                string replacement = TestUtil.RandomSimpleString(random);
                Analyzer a = Analyzer.NewAnonymous(createComponents: (fieldName, reader) =>
                {
                    Tokenizer tokenizer = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
                    return new TokenStreamComponents(tokenizer, tokenizer);
                }, initReader: (fieldName, reader) => new PatternReplaceCharFilter(p, replacement, reader));

                /* max input length. don't make it longer -- exponential processing
                 * time for certain patterns. */
                const int maxInputLength = 30;
                /* ASCII only input?: */
                const bool asciiOnly = true;
                CheckRandomData(random, a, 250 * RandomMultiplier, maxInputLength, asciiOnly);
            }
        }
    }
}