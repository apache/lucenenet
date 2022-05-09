// Lucene version compatibility level 4.8.1
using Lucene.Net.Diagnostics;
using Lucene.Net.Util;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Console = Lucene.Net.Util.SystemConsole;
using JCG = J2N.Collections.Generic;

namespace Lucene.Net.Analysis.CharFilters
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

    public class TestMappingCharFilter : BaseTokenStreamTestCase
    {

        internal NormalizeCharMap normMap;

        public override void SetUp()
        {
            base.SetUp();
            NormalizeCharMap.Builder builder = new NormalizeCharMap.Builder();

            builder.Add("aa", "a");
            builder.Add("bbb", "b");
            builder.Add("cccc", "cc");

            builder.Add("h", "i");
            builder.Add("j", "jj");
            builder.Add("k", "kkk");
            builder.Add("ll", "llll");

            builder.Add("empty", "");

            // BMP (surrogate pair):
            builder.Add(UnicodeUtil.NewString(new int[] { 0x1D122 }, 0, 1), "fclef");

            builder.Add("\uff01", "full-width-exclamation");

            normMap = builder.Build();
        }

        [Test]
        public virtual void TestReaderReset()
        {
            CharFilter cs = new MappingCharFilter(normMap, new StringReader("x"));
            char[] buf = new char[10];
            int len = cs.Read(buf, 0, 10);
            assertEquals(1, len);
            assertEquals('x', buf[0]);
            len = cs.Read(buf, 0, 10);
            assertEquals(-1, len);

            // rewind
            cs.Reset();
            len = cs.Read(buf, 0, 10);
            assertEquals(1, len);
            assertEquals('x', buf[0]);
        }

        [Test]
        public virtual void TestNothingChange()
        {
            CharFilter cs = new MappingCharFilter(normMap, new StringReader("x"));
            TokenStream ts = new MockTokenizer(cs, MockTokenizer.WHITESPACE, false);
            AssertTokenStreamContents(ts, new string[] { "x" }, new int[] { 0 }, new int[] { 1 }, 1);
        }

        [Test]
        public virtual void Test1to1()
        {
            CharFilter cs = new MappingCharFilter(normMap, new StringReader("h"));
            TokenStream ts = new MockTokenizer(cs, MockTokenizer.WHITESPACE, false);
            AssertTokenStreamContents(ts, new string[] { "i" }, new int[] { 0 }, new int[] { 1 }, 1);
        }

        [Test]
        public virtual void Test1to2()
        {
            CharFilter cs = new MappingCharFilter(normMap, new StringReader("j"));
            TokenStream ts = new MockTokenizer(cs, MockTokenizer.WHITESPACE, false);
            AssertTokenStreamContents(ts, new string[] { "jj" }, new int[] { 0 }, new int[] { 1 }, 1);
        }

        [Test]
        public virtual void Test1to3()
        {
            CharFilter cs = new MappingCharFilter(normMap, new StringReader("k"));
            TokenStream ts = new MockTokenizer(cs, MockTokenizer.WHITESPACE, false);
            AssertTokenStreamContents(ts, new string[] { "kkk" }, new int[] { 0 }, new int[] { 1 }, 1);
        }

        [Test]
        public virtual void Test2to4()
        {
            CharFilter cs = new MappingCharFilter(normMap, new StringReader("ll"));
            TokenStream ts = new MockTokenizer(cs, MockTokenizer.WHITESPACE, false);
            AssertTokenStreamContents(ts, new string[] { "llll" }, new int[] { 0 }, new int[] { 2 }, 2);
        }

        [Test]
        public virtual void Test2to1()
        {
            CharFilter cs = new MappingCharFilter(normMap, new StringReader("aa"));
            TokenStream ts = new MockTokenizer(cs, MockTokenizer.WHITESPACE, false);
            AssertTokenStreamContents(ts, new string[] { "a" }, new int[] { 0 }, new int[] { 2 }, 2);
        }

        [Test]
        public virtual void Test3to1()
        {
            CharFilter cs = new MappingCharFilter(normMap, new StringReader("bbb"));
            TokenStream ts = new MockTokenizer(cs, MockTokenizer.WHITESPACE, false);
            AssertTokenStreamContents(ts, new string[] { "b" }, new int[] { 0 }, new int[] { 3 }, 3);
        }

        [Test]
        public virtual void Test4to2()
        {
            CharFilter cs = new MappingCharFilter(normMap, new StringReader("cccc"));
            TokenStream ts = new MockTokenizer(cs, MockTokenizer.WHITESPACE, false);
            AssertTokenStreamContents(ts, new string[] { "cc" }, new int[] { 0 }, new int[] { 4 }, 4);
        }

        [Test]
        public virtual void Test5to0()
        {
            CharFilter cs = new MappingCharFilter(normMap, new StringReader("empty"));
            TokenStream ts = new MockTokenizer(cs, MockTokenizer.WHITESPACE, false);
            AssertTokenStreamContents(ts, new string[0], new int[] { }, new int[] { }, 5);
        }

        [Test]
        public virtual void TestNonBMPChar()
        {
            CharFilter cs = new MappingCharFilter(normMap, new StringReader(UnicodeUtil.NewString(new int[] { 0x1D122 }, 0, 1)));
            TokenStream ts = new MockTokenizer(cs, MockTokenizer.WHITESPACE, false);
            AssertTokenStreamContents(ts, new string[] { "fclef" }, new int[] { 0 }, new int[] { 2 }, 2);
        }

        [Test]
        public virtual void TestFullWidthChar()
        {
            CharFilter cs = new MappingCharFilter(normMap, new StringReader("\uff01"));
            TokenStream ts = new MockTokenizer(cs, MockTokenizer.WHITESPACE, false);
            AssertTokenStreamContents(ts, new string[] { "full-width-exclamation" }, new int[] { 0 }, new int[] { 1 }, 1);
        }

        //
        //                1111111111222
        //      01234567890123456789012
        //(in)  h i j k ll cccc bbb aa
        //
        //                1111111111222
        //      01234567890123456789012
        //(out) i i jj kkk llll cc b a
        //
        //    h, 0, 1 =>    i, 0, 1
        //    i, 2, 3 =>    i, 2, 3
        //    j, 4, 5 =>   jj, 4, 5
        //    k, 6, 7 =>  kkk, 6, 7
        //   ll, 8,10 => llll, 8,10
        // cccc,11,15 =>   cc,11,15
        //  bbb,16,19 =>    b,16,19
        //   aa,20,22 =>    a,20,22
        //
        [Test]
        public virtual void TestTokenStream()
        {
            string testString = "h i j k ll cccc bbb aa";
            CharFilter cs = new MappingCharFilter(normMap, new StringReader(testString));
            TokenStream ts = new MockTokenizer(cs, MockTokenizer.WHITESPACE, false);
            AssertTokenStreamContents(ts, new string[] { "i", "i", "jj", "kkk", "llll", "cc", "b", "a" }, new int[] { 0, 2, 4, 6, 8, 11, 16, 20 }, new int[] { 1, 3, 5, 7, 10, 15, 19, 22 }, testString.Length);
        }

        //
        //
        //        0123456789
        //(in)    aaaa ll h
        //(out-1) aa llll i
        //(out-2) a llllllll i
        //
        // aaaa,0,4 => a,0,4
        //   ll,5,7 => llllllll,5,7
        //    h,8,9 => i,8,9
        [Test]
        public virtual void TestChained()
        {
            string testString = "aaaa ll h";
            CharFilter cs = new MappingCharFilter(normMap, new MappingCharFilter(normMap, new StringReader(testString)));
            TokenStream ts = new MockTokenizer(cs, MockTokenizer.WHITESPACE, false);
            AssertTokenStreamContents(ts, new string[] { "a", "llllllll", "i" }, new int[] { 0, 5, 8 }, new int[] { 4, 7, 9 }, testString.Length);
        }

        [Test]
        public virtual void TestRandom()
        {
            Analyzer analyzer = Analyzer.NewAnonymous(createComponents: (fieldName, reader) =>
            {
                Tokenizer tokenizer = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
                return new TokenStreamComponents(tokenizer, tokenizer);
            }, initReader: (fieldName, reader) =>
            { 
                return new MappingCharFilter(normMap, reader);
            });

            int numRounds = RandomMultiplier * 10000;
            CheckRandomData(Random, analyzer, numRounds);
        }

        //[Ignore("wrong finalOffset: https://issues.apache.org/jira/browse/LUCENE-3971")] // LUCENENET: This was commented in Lucene
        [Test]
        public virtual void TestFinalOffsetSpecialCase()
        {
            NormalizeCharMap.Builder builder = new NormalizeCharMap.Builder();
            builder.Add("t", "");
            // even though this below rule has no effect, the test passes if you remove it!!
            builder.Add("tmakdbl", "c");

            NormalizeCharMap map = builder.Build();

            Analyzer analyzer = Analyzer.NewAnonymous(createComponents: (fieldName, reader) =>
            {
                Tokenizer tokenizer = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
                return new TokenStreamComponents(tokenizer, tokenizer);
            }, initReader: (fieldName, reader) => new MappingCharFilter(map, reader));

            string text = "gzw f quaxot";
            CheckAnalysisConsistency(Random, analyzer, false, text);
        }

        //[Ignore("wrong finalOffset: https://issues.apache.org/jira/browse/LUCENE-3971")] // LUCENENET: This was commented in Lucene
        [Test]
        public virtual void TestRandomMaps()
        {
            int numIterations = AtLeast(3);
            for (int i = 0; i < numIterations; i++)
            {
                NormalizeCharMap map = RandomMap();
                Analyzer analyzer = Analyzer.NewAnonymous(createComponents: (fieldName, reader) =>
                {
                    Tokenizer tokenizer = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
                    return new TokenStreamComponents(tokenizer, tokenizer);
                }, initReader: (fieldName, reader) => new MappingCharFilter(map, reader));
                int numRounds = 100;
                CheckRandomData(Random, analyzer, numRounds);
            }
        }

        private NormalizeCharMap RandomMap()
        {
            Random random = Random;
            NormalizeCharMap.Builder builder = new NormalizeCharMap.Builder();
            // we can't add duplicate keys, or NormalizeCharMap gets angry
            ISet<string> keys = new JCG.HashSet<string>();
            int num = random.Next(5);
            //System.out.println("NormalizeCharMap=");
            for (int i = 0; i < num; i++)
            {
                string key = TestUtil.RandomSimpleString(random);
                if (!keys.Contains(key) && key.Length != 0)
                {
                    string value = TestUtil.RandomSimpleString(random);
                    builder.Add(key, value);
                    keys.Add(key);
                    //System.out.println("mapping: '" + key + "' => '" + value + "'");
                }
            }
            return builder.Build();
        }

        [Test]
        public virtual void TestRandomMaps2()
        {
            Random random = Random;
            int numIterations = AtLeast(3);
            for (int iter = 0; iter < numIterations; iter++)
            {

                if (Verbose)
                {
                    Console.WriteLine("\nTEST iter=" + iter);
                }

                char endLetter = (char)TestUtil.NextInt32(random, 'b', 'z');
                IDictionary<string, string> map = new Dictionary<string, string>();
                NormalizeCharMap.Builder builder = new NormalizeCharMap.Builder();
                int numMappings = AtLeast(5);
                if (Verbose)
                {
                    Console.WriteLine("  mappings:");
                }
                while (map.Count < numMappings)
                {
                    string key = TestUtil.RandomSimpleStringRange(random, 'a', endLetter, 7);
                    if (key.Length != 0 && !map.ContainsKey(key))
                    {
                        string value = TestUtil.RandomSimpleString(random);
                        map[key] = value;
                        builder.Add(key, value);
                        if (Verbose)
                        {
                            Console.WriteLine("    " + key + " -> " + value);
                        }
                    }
                }

                NormalizeCharMap charMap = builder.Build();

                if (Verbose)
                {
                    Console.WriteLine("  test random documents...");
                }

                for (int iter2 = 0; iter2 < 100; iter2++)
                {
                    string content = TestUtil.RandomSimpleStringRange(random, 'a', endLetter, AtLeast(1000));

                    if (Verbose)
                    {
                        Console.WriteLine("  content=" + content);
                    }

                    // Do stupid dog-slow mapping:

                    // Output string:
                    StringBuilder output = new StringBuilder();

                    // Maps output offset to input offset:
                    IList<int> inputOffsets = new JCG.List<int>();

                    int cumDiff = 0;
                    int charIdx = 0;
                    while (charIdx < content.Length)
                    {

                        int matchLen = -1;
                        string matchRepl = null;

                        foreach (KeyValuePair<string, string> ent in map)
                        {
                            string match = ent.Key;
                            if (charIdx + match.Length <= content.Length)
                            {
                                int limit = charIdx + match.Length;
                                bool matches = true;
                                for (int charIdx2 = charIdx; charIdx2 < limit; charIdx2++)
                                {
                                    if (match[charIdx2 - charIdx] != content[charIdx2])
                                    {
                                        matches = false;
                                        break;
                                    }
                                }

                                if (matches)
                                {
                                    string repl = ent.Value;
                                    if (match.Length > matchLen)
                                    {
                                        // Greedy: longer match wins
                                        matchLen = match.Length;
                                        matchRepl = repl;
                                    }
                                }
                            }
                        }

                        if (matchLen != -1)
                        {
                            // We found a match here!
                            if (Verbose)
                            {
                                Console.WriteLine("    match=" + content.Substring(charIdx, matchLen) + " @ off=" + charIdx + " repl=" + matchRepl);
                            }
                            output.Append(matchRepl);
                            int minLen = Math.Min(matchLen, matchRepl.Length);

                            // Common part, directly maps back to input
                            // offset:
                            for (int outIdx = 0; outIdx < minLen; outIdx++)
                            {
                                inputOffsets.Add(output.Length - matchRepl.Length + outIdx + cumDiff);
                            }

                            cumDiff += matchLen - matchRepl.Length;
                            charIdx += matchLen;

                            if (matchRepl.Length < matchLen)
                            {
                                // Replacement string is shorter than matched
                                // input: nothing to do
                            }
                            else if (matchRepl.Length > matchLen)
                            {
                                // Replacement string is longer than matched
                                // input: for all the "extra" chars we map
                                // back to a single input offset:
                                for (int outIdx = matchLen; outIdx < matchRepl.Length; outIdx++)
                                {
                                    inputOffsets.Add(output.Length + cumDiff - 1);
                                }
                            }
                            else
                            {
                                // Same length: no change to offset
                            }

                            if (Debugging.AssertsEnabled) Debugging.Assert(inputOffsets.Count == output.Length,"inputOffsets.Count={0} vs output.Length={1}", inputOffsets.Count, output.Length);
                        }
                        else
                        {
                            inputOffsets.Add(output.Length + cumDiff);
                            output.Append(content[charIdx]);
                            charIdx++;
                        }
                    }

                    string expected = output.ToString();
                    if (Verbose)
                    {
                        Console.Write("    expected:");
                        for (int charIdx2 = 0; charIdx2 < expected.Length; charIdx2++)
                        {
                            Console.Write(" " + expected[charIdx2] + "/" + inputOffsets[charIdx2]);
                        }
                        Console.WriteLine();
                    }

                    MappingCharFilter mapFilter = new MappingCharFilter(charMap, new StringReader(content));
                    StringBuilder actualBuilder = new StringBuilder();
                    IList<int> actualInputOffsets = new JCG.List<int>();

                    // Now consume the actual mapFilter, somewhat randomly:
                    while (true)
                    {
                        if (random.Next(0, 1) == 1)
                        {
                            int ch = mapFilter.Read();
                            if (ch == -1)
                            {
                                break;
                            }
                            actualBuilder.Append((char)ch);
                        }
                        else
                        {
                            char[] buffer = new char[TestUtil.NextInt32(random, 1, 100)];
                            int off = buffer.Length == 1 ? 0 : random.Next(buffer.Length - 1);
                            int count = mapFilter.Read(buffer, off, buffer.Length - off);
                            if (count == -1)
                            {
                                break;
                            }
                            else
                            {
                                actualBuilder.Append(buffer, off, count);
                            }
                        }

                        if (random.Next(10) == 7)
                        {
                            // Map offsets
                            while (actualInputOffsets.Count < actualBuilder.Length)
                            {
                                actualInputOffsets.Add(mapFilter.CorrectOffset(actualInputOffsets.Count));
                            }
                        }
                    }

                    // Finish mappping offsets
                    while (actualInputOffsets.Count < actualBuilder.Length)
                    {
                        actualInputOffsets.Add(mapFilter.CorrectOffset(actualInputOffsets.Count));
                    }

                    string actual = actualBuilder.ToString();

                    // Verify:
                    assertEquals(expected, actual);
                    assertEquals(inputOffsets, actualInputOffsets, aggressive: false);
                }
            }
        }
    }
}