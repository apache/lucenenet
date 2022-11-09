// Lucene version compatibility level 4.8.1
using J2N.Text;
using Lucene.Net.Analysis.Core;
using Lucene.Net.Analysis.TokenAttributes;
using Lucene.Net.Diagnostics;
using Lucene.Net.Support;
using Lucene.Net.Util;
using NUnit.Framework;
using RandomizedTesting.Generators;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Console = Lucene.Net.Util.SystemConsole;
using JCG = J2N.Collections.Generic;

namespace Lucene.Net.Analysis.Synonym
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

    public class TestSynonymMapFilter : BaseTokenStreamTestCase
    {

        private SynonymMap.Builder b;
        private Tokenizer tokensIn;
        private SynonymFilter tokensOut;
        private ICharTermAttribute termAtt;
        private IPositionIncrementAttribute posIncrAtt;
        private IPositionLengthAttribute posLenAtt;
        private IOffsetAttribute offsetAtt;

        private static readonly Regex space = new Regex(" +", RegexOptions.Compiled);

        private void Add(string input, string output, bool keepOrig)
        {
            if (Verbose)
            {
                Console.WriteLine("  add input=" + input + " output=" + output + " keepOrig=" + keepOrig);
            }

            CharsRef inputCharsRef = new CharsRef();
            SynonymMap.Builder.Join(space.Split(input).TrimEnd(), inputCharsRef);

            CharsRef outputCharsRef = new CharsRef();
            SynonymMap.Builder.Join(space.Split(output).TrimEnd(), outputCharsRef);

            b.Add(inputCharsRef, outputCharsRef, keepOrig);
        }

        private void AssertEquals(ICharTermAttribute term, string expected)
        {
            assertEquals(expected.Length, term.Length);
            char[] buffer = term.Buffer;
            for (int chIDX = 0; chIDX < expected.Length; chIDX++)
            {
                assertEquals(expected[chIDX], buffer[chIDX]);
            }
        }

        // For the output string: separate positions with a space,
        // and separate multiple tokens at each position with a
        // /.  If a token should have end offset != the input
        // token's end offset then add :X to it:

        // TODO: we should probably refactor this guy to use/take analyzer,
        // the tests are a little messy
        private void Verify(string input, string output)
        {
            if (Verbose)
            {
                Console.WriteLine("TEST: verify input=" + input + " expectedOutput=" + output);
            }

            tokensIn.SetReader(new StringReader(input));
            tokensOut.Reset();
            string[] expected = output.Split(' ').TrimEnd();
            int expectedUpto = 0;
            while (tokensOut.IncrementToken())
            {

                if (Verbose)
                {
                    Console.WriteLine("  incr token=" + termAtt.ToString() + " posIncr=" + posIncrAtt.PositionIncrement + " startOff=" + offsetAtt.StartOffset + " endOff=" + offsetAtt.EndOffset);
                }

                assertTrue(expectedUpto < expected.Length);
                int startOffset = offsetAtt.StartOffset;
                int endOffset = offsetAtt.EndOffset;

                string[] expectedAtPos = expected[expectedUpto++].Split('/').TrimEnd();
                for (int atPos = 0; atPos < expectedAtPos.Length; atPos++)
                {
                    if (atPos > 0)
                    {
                        assertTrue(tokensOut.IncrementToken());
                        if (Verbose)
                        {
                            Console.WriteLine("  incr token=" + termAtt.ToString() + " posIncr=" + posIncrAtt.PositionIncrement + " startOff=" + offsetAtt.StartOffset + " endOff=" + offsetAtt.EndOffset);
                        }
                    }
                    int colonIndex = expectedAtPos[atPos].IndexOf(':');
                    int underbarIndex = expectedAtPos[atPos].IndexOf('_');
                    string expectedToken;
                    int expectedEndOffset;
                    int expectedPosLen;
                    if (colonIndex != -1)
                    {
                        expectedToken = expectedAtPos[atPos].Substring(0, colonIndex - 0);
                        if (underbarIndex != -1)
                        {
                            expectedEndOffset = int.Parse(expectedAtPos[atPos].Substring(1 + colonIndex, underbarIndex - (1 + colonIndex)), CultureInfo.InvariantCulture);
                            expectedPosLen = int.Parse(expectedAtPos[atPos].Substring(1 + underbarIndex), CultureInfo.InvariantCulture);
                        }
                        else
                        {
                            expectedEndOffset = int.Parse(expectedAtPos[atPos].Substring(1 + colonIndex), CultureInfo.InvariantCulture);
                            expectedPosLen = 1;
                        }
                    }
                    else
                    {
                        expectedToken = expectedAtPos[atPos];
                        expectedEndOffset = endOffset;
                        expectedPosLen = 1;
                    }
                    assertEquals(expectedToken, termAtt.ToString());
                    assertEquals(atPos == 0 ? 1 : 0, posIncrAtt.PositionIncrement);
                    // start/end offset of all tokens at same pos should
                    // be the same:
                    assertEquals(startOffset, offsetAtt.StartOffset);
                    assertEquals(expectedEndOffset, offsetAtt.EndOffset);
                    assertEquals(expectedPosLen, posLenAtt.PositionLength);
                }
            }
            tokensOut.End();
            tokensOut.Dispose();
            if (Verbose)
            {
                Console.WriteLine("  incr: END");
            }
            assertEquals(expectedUpto, expected.Length);
        }

        [Test]
        public virtual void TestDontKeepOrig()
        {
            b = new SynonymMap.Builder(true);
            Add("a b", "foo", false);

            SynonymMap map = b.Build();
            Analyzer analyzer = Analyzer.NewAnonymous(createComponents: (fieldName, reader) =>
            {
                Tokenizer tokenizer = new MockTokenizer(reader, MockTokenizer.SIMPLE, true);
                return new TokenStreamComponents(tokenizer, new SynonymFilter(tokenizer, map, false));
            });

            AssertAnalyzesTo(analyzer, "a b c", 
                            new string[] { "foo", "c" }, 
                            new int[] { 0, 4 }, 
                            new int[] { 3, 5 }, 
                            null, 
                            new int[] { 1, 1 }, 
                            new int[] { 1, 1 }, 
                            true);
            CheckAnalysisConsistency(Random, analyzer, false, "a b c");
        }

        [Test]
        public virtual void TestDoKeepOrig()
        {
            b = new SynonymMap.Builder(true);
            Add("a b", "foo", true);

            SynonymMap map = b.Build();

            Analyzer analyzer = Analyzer.NewAnonymous(createComponents: (fieldName, reader) =>
            {
                Tokenizer tokenizer = new MockTokenizer(reader, MockTokenizer.SIMPLE, true);
                return new TokenStreamComponents(tokenizer, new SynonymFilter(tokenizer, map, false));
            });

            AssertAnalyzesTo(analyzer, "a b c", 
                            new string[] { "a", "foo", "b", "c" }, 
                            new int[] { 0, 0, 2, 4 }, 
                            new int[] { 1, 3, 3, 5 }, 
                            null, 
                            new int[] { 1, 0, 1, 1 }, 
                            new int[] { 1, 2, 1, 1 }, 
                            true);
            CheckAnalysisConsistency(Random, analyzer, false, "a b c");
        }

        [Test]
        public virtual void TestBasic()
        {
            b = new SynonymMap.Builder(true);
            Add("a", "foo", true);
            Add("a b", "bar fee", true);
            Add("b c", "dog collar", true);
            Add("c d", "dog harness holder extras", true);
            Add("m c e", "dog barks loudly", false);
            Add("i j k", "feep", true);

            Add("e f", "foo bar", false);
            Add("e f", "baz bee", false);

            Add("z", "boo", false);
            Add("y", "bee", true);

            tokensIn = new MockTokenizer(new StringReader("a"), MockTokenizer.WHITESPACE, true);
            tokensIn.Reset();
            assertTrue(tokensIn.IncrementToken());
            assertFalse(tokensIn.IncrementToken());
            tokensIn.End();
            tokensIn.Dispose();

            tokensOut = new SynonymFilter(tokensIn, b.Build(), true);
            termAtt = tokensOut.AddAttribute<ICharTermAttribute>();
            posIncrAtt = tokensOut.AddAttribute<IPositionIncrementAttribute>();
            posLenAtt = tokensOut.AddAttribute<IPositionLengthAttribute>();
            offsetAtt = tokensOut.AddAttribute<IOffsetAttribute>();

            Verify("a b c", "a/bar b/fee c");

            // syn output extends beyond input tokens
            Verify("x a b c d", "x a/bar b/fee c/dog d/harness holder extras");

            Verify("a b a", "a/bar b/fee a/foo");

            // outputs that add to one another:
            Verify("c d c d", "c/dog d/harness c/holder/dog d/extras/harness holder extras");

            // two outputs for same input
            Verify("e f", "foo/baz bar/bee");

            // verify multi-word / single-output offsets:
            Verify("g i j k g", "g i/feep:7_3 j k g");

            // mixed keepOrig true/false:
            Verify("a m c e x", "a/foo dog barks loudly x");
            Verify("c d m c e x", "c/dog d/harness holder/dog extras/barks loudly x");
            assertTrue(tokensOut.CaptureCount > 0);

            // no captureStates when no syns matched
            Verify("p q r s t", "p q r s t");
            assertEquals(0, tokensOut.CaptureCount);

            // no captureStates when only single-input syns, w/ no
            // lookahead needed, matched
            Verify("p q z y t", "p q boo y/bee t");
            assertEquals(0, tokensOut.CaptureCount);
        }

        private string GetRandomString(char start, int alphabetSize, int length)
        {
            if (Debugging.AssertsEnabled) Debugging.Assert(alphabetSize <= 26);
            char[] s = new char[2 * length];
            for (int charIDX = 0; charIDX < length; charIDX++)
            {
                s[2 * charIDX] = (char)(start + Random.Next(alphabetSize));
                s[2 * charIDX + 1] = ' ';
            }
            return new string(s);
        }

        protected class OneSyn
        {
            internal string @in;
            internal IList<string> @out;
            internal bool keepOrig;
        }

        protected virtual string SlowSynMatcher(string doc, IList<OneSyn> syns, int maxOutputLength)
        {
            assertTrue(doc.Length % 2 == 0);
            int numInputs = doc.Length / 2;
            bool[] keepOrigs = new bool[numInputs];
            bool[] hasMatch = new bool[numInputs];
            Arrays.Fill(keepOrigs, false);
            string[] outputs = new string[numInputs + maxOutputLength];
            OneSyn[] matches = new OneSyn[numInputs];
            foreach (OneSyn syn in syns)
            {
                int idx = -1;
                while (true)
                {
                    idx = doc.IndexOf(syn.@in, 1 + idx, StringComparison.Ordinal);
                    if (idx == -1)
                    {
                        break;
                    }
                    assertTrue(idx % 2 == 0);
                    int matchIDX = idx / 2;
                    assertTrue(syn.@in.Length % 2 == 1);
                    if (matches[matchIDX] is null)
                    {
                        matches[matchIDX] = syn;
                    }
                    else if (syn.@in.Length > matches[matchIDX].@in.Length)
                    {
                        // Greedy conflict resolution: longer match wins:
                        matches[matchIDX] = syn;
                    }
                    else
                    {
                        assertTrue(syn.@in.Length < matches[matchIDX].@in.Length);
                    }
                }
            }

            // Greedy conflict resolution: if syn matches a range of inputs,
            // it prevents other syns from matching that range
            for (int inputIDX = 0; inputIDX < numInputs; inputIDX++)
            {
                OneSyn match = matches[inputIDX];
                if (match != null)
                {
                    int synInLength = (1 + match.@in.Length) / 2;
                    for (int nextInputIDX = inputIDX + 1; nextInputIDX < numInputs && nextInputIDX < (inputIDX + synInLength); nextInputIDX++)
                    {
                        matches[nextInputIDX] = null;
                    }
                }
            }

            // Fill overlapping outputs:
            for (int inputIDX = 0; inputIDX < numInputs; inputIDX++)
            {
                OneSyn syn = matches[inputIDX];
                if (syn is null)
                {
                    continue;
                }
                for (int idx = 0; idx < (1 + syn.@in.Length) / 2; idx++)
                {
                    hasMatch[inputIDX + idx] = true;
                    keepOrigs[inputIDX + idx] |= syn.keepOrig;
                }
                foreach (string synOut in syn.@out)
                {
                    string[] synOutputs = synOut.Split(' ').TrimEnd();
                    assertEquals(synOutputs.Length, (1 + synOut.Length) / 2);
                    int matchEnd = inputIDX + synOutputs.Length;
                    int synUpto = 0;
                    for (int matchIDX = inputIDX; matchIDX < matchEnd; matchIDX++)
                    {
                        if (outputs[matchIDX] is null)
                        {
                            outputs[matchIDX] = synOutputs[synUpto++];
                        }
                        else
                        {
                            outputs[matchIDX] = outputs[matchIDX] + "/" + synOutputs[synUpto++];
                        }
                        int endOffset;
                        if (matchIDX < numInputs)
                        {
                            int posLen;
                            if (synOutputs.Length == 1)
                            {
                                // Add full endOffset
                                endOffset = (inputIDX * 2) + syn.@in.Length;
                                posLen = syn.keepOrig ? (1 + syn.@in.Length) / 2 : 1;
                            }
                            else
                            {
                                // Add endOffset matching input token's
                                endOffset = (matchIDX * 2) + 1;
                                posLen = 1;
                            }
                            outputs[matchIDX] = outputs[matchIDX] + ":" + endOffset + "_" + posLen;
                        }
                    }
                }
            }

            StringBuilder sb = new StringBuilder();
            string[] inputTokens = doc.Split(' ').TrimEnd();
            int limit = inputTokens.Length + maxOutputLength;
            for (int inputIDX = 0; inputIDX < limit; inputIDX++)
            {
                bool posHasOutput = false;
                if (inputIDX >= numInputs && outputs[inputIDX] is null)
                {
                    break;
                }
                if (inputIDX < numInputs && (!hasMatch[inputIDX] || keepOrigs[inputIDX]))
                {
                    assertTrue(inputTokens[inputIDX].Length != 0);
                    sb.Append(inputTokens[inputIDX]);
                    posHasOutput = true;
                }

                if (outputs[inputIDX] != null)
                {
                    if (posHasOutput)
                    {
                        sb.Append('/');
                    }
                    sb.Append(outputs[inputIDX]);
                }
                else if (!posHasOutput)
                {
                    continue;
                }
                if (inputIDX < limit - 1)
                {
                    sb.Append(' ');
                }
            }

            return sb.ToString();
        }

        [Test]
        public virtual void TestRandom()
        {

            int alphabetSize = TestUtil.NextInt32(Random, 2, 7);

            int docLen = AtLeast(3000);
            //final int docLen = 50;

            string document = GetRandomString('a', alphabetSize, docLen);

            if (Verbose)
            {
                Console.WriteLine("TEST: doc=" + document);
            }

            int numSyn = AtLeast(5);
            //final int numSyn = 2;

            IDictionary<string, OneSyn> synMap = new Dictionary<string, OneSyn>();
            IList<OneSyn> syns = new JCG.List<OneSyn>();
            bool dedup = Random.nextBoolean();
            if (Verbose)
            {
                Console.WriteLine("  dedup=" + dedup);
            }
            b = new SynonymMap.Builder(dedup);
            for (int synIDX = 0; synIDX < numSyn; synIDX++)
            {
                string synIn = GetRandomString('a', alphabetSize, TestUtil.NextInt32(Random, 1, 5)).Trim();
                if (!synMap.TryGetValue(synIn, out OneSyn s) || s is null)
                {
                    s = new OneSyn();
                    s.@in = synIn;
                    syns.Add(s);
                    s.@out = new JCG.List<string>();
                    synMap[synIn] = s;
                    s.keepOrig = Random.nextBoolean();
                }
                string synOut = GetRandomString('0', 10, TestUtil.NextInt32(Random, 1, 5)).Trim();
                s.@out.Add(synOut);
                Add(synIn, synOut, s.keepOrig);
                if (Verbose)
                {
                    Console.WriteLine("  syns[" + synIDX + "] = " + s.@in + " -> " + s.@out + " keepOrig=" + s.keepOrig);
                }
            }

            tokensIn = new MockTokenizer(new StringReader("a"), MockTokenizer.WHITESPACE, true);
            tokensIn.Reset();
            assertTrue(tokensIn.IncrementToken());
            assertFalse(tokensIn.IncrementToken());
            tokensIn.End();
            tokensIn.Dispose();

            tokensOut = new SynonymFilter(tokensIn, b.Build(), true);
            termAtt = tokensOut.AddAttribute<ICharTermAttribute>();
            posIncrAtt = tokensOut.AddAttribute<IPositionIncrementAttribute>();
            posLenAtt = tokensOut.AddAttribute<IPositionLengthAttribute>();
            offsetAtt = tokensOut.AddAttribute<IOffsetAttribute>();

            if (dedup)
            {
                PruneDups(syns);
            }

            string expected = SlowSynMatcher(document, syns, 5);

            if (Verbose)
            {
                Console.WriteLine("TEST: expected=" + expected);
            }

            Verify(document, expected);
        }

        private void PruneDups(IList<OneSyn> syns)
        {
            ISet<string> seen = new JCG.HashSet<string>();
            foreach (OneSyn syn in syns)
            {
                int idx = 0;
                while (idx < syn.@out.Count)
                {
                    string @out = syn.@out[idx];
                    if (!seen.Contains(@out))
                    {
                        seen.Add(@out);
                        idx++;
                    }
                    else
                    {
                        syn.@out.RemoveAt(idx);
                    }
                }
                seen.Clear();
            }
        }

        private string RandomNonEmptyString()
        {
            while (true)
            {
                string s = TestUtil.RandomUnicodeString(Random).Trim();
                if (s.Length != 0 && s.IndexOf('\u0000') == -1)
                {
                    return s;
                }
            }
        }

        /// <summary>
        /// simple random test, doesn't verify correctness.
        ///  does verify it doesnt throw exceptions, or that the stream doesn't misbehave
        /// </summary>
        [Test]
        public virtual void TestRandom2()
        {
            int numIters = AtLeast(3);
            for (int i = 0; i < numIters; i++)
            {
                b = new SynonymMap.Builder(Random.nextBoolean());
                int numEntries = AtLeast(10);
                for (int j = 0; j < numEntries; j++)
                {
                    Add(RandomNonEmptyString(), RandomNonEmptyString(), Random.nextBoolean());
                }
                SynonymMap map = b.Build();
                bool ignoreCase = Random.nextBoolean();

                Analyzer analyzer = Analyzer.NewAnonymous(createComponents: (fieldName, reader) =>
                {
                    Tokenizer tokenizer = new MockTokenizer(reader, MockTokenizer.SIMPLE, true);
                    return new TokenStreamComponents(tokenizer, new SynonymFilter(tokenizer, map, ignoreCase));
                });

                CheckRandomData(Random, analyzer, 100);
            }
        }

        // NOTE: this is an invalid test... SynFilter today can't
        // properly consume a graph... we can re-enable this once
        // we fix that...
        /*
        // Adds MockGraphTokenFilter before SynFilter:
        public void TestRandom2GraphBefore() throws Exception {
          final int numIters = AtLeast(10);
          Random random = Random;
          for (int i = 0; i < numIters; i++) {
            b = new SynonymMap.Builder(random.nextBoolean());
            final int numEntries = AtLeast(10);
            for (int j = 0; j < numEntries; j++) {
              add(randomNonEmptyString(), randomNonEmptyString(), random.nextBoolean());
            }
            final SynonymMap map = b.Build();
            final boolean ignoreCase = random.nextBoolean();

            final Analyzer analyzer = new Analyzer() {
              @Override
              protected TokenStreamComponents createComponents(String fieldName, Reader reader) {
                Tokenizer tokenizer = new MockTokenizer(reader, MockTokenizer.SIMPLE, true);
                TokenStream graph = new MockGraphTokenFilter(Random, tokenizer);
                return new TokenStreamComponents(tokenizer, new SynonymFilter(graph, map, ignoreCase));
              }
            };

            checkRandomData(random, analyzer, 1000*RANDOM_MULTIPLIER);
          }
        }
        */

        // Adds MockGraphTokenFilter after SynFilter:
        [Test]
        public virtual void TestRandom2GraphAfter()
        {
            int numIters = AtLeast(3);
            Random random = Random;
            for (int i = 0; i < numIters; i++)
            {
                b = new SynonymMap.Builder(random.nextBoolean());
                int numEntries = AtLeast(10);
                for (int j = 0; j < numEntries; j++)
                {
                    Add(RandomNonEmptyString(), RandomNonEmptyString(), random.nextBoolean());
                }
                SynonymMap map = b.Build();
                bool ignoreCase = random.nextBoolean();

                Analyzer analyzer = Analyzer.NewAnonymous(createComponents: (fieldName, reader) =>
                {
                    Tokenizer tokenizer = new MockTokenizer(reader, MockTokenizer.SIMPLE, true);
                    TokenStream syns = new SynonymFilter(tokenizer, map, ignoreCase);
                    TokenStream graph = new MockGraphTokenFilter(Random, syns);
                    return new TokenStreamComponents(tokenizer, graph);
                });

                CheckRandomData(random, analyzer, 100);
            }
        }

        [Test]
        public virtual void TestEmptyTerm()
        {
            Random random = Random;
            int numIters = AtLeast(10);
            for (int i = 0; i < numIters; i++)
            {
                b = new SynonymMap.Builder(random.nextBoolean());
                int numEntries = AtLeast(10);
                for (int j = 0; j < numEntries; j++)
                {
                    Add(RandomNonEmptyString(), RandomNonEmptyString(), random.NextBoolean());
                }
                SynonymMap map = b.Build();
                bool ignoreCase = random.nextBoolean();

                Analyzer analyzer = Analyzer.NewAnonymous(createComponents: (fieldName, reader) =>
                {
                    Tokenizer tokenizer = new KeywordTokenizer(reader);
                    return new TokenStreamComponents(tokenizer, new SynonymFilter(tokenizer, map, ignoreCase));
                });

                CheckAnalysisConsistency(random, analyzer, random.NextBoolean(), "");
            }
        }

        /// <summary>
        /// simple random test like testRandom2, but for larger docs
        /// </summary>
        [Test]
        public virtual void TestRandomHuge()
        {
            Random random = Random;
            int numIters = AtLeast(3);
            for (int i = 0; i < numIters; i++)
            {
                b = new SynonymMap.Builder(random.NextBoolean());
                int numEntries = AtLeast(10);
                if (Verbose)
                {
                    Console.WriteLine("TEST: iter=" + i + " numEntries=" + numEntries);
                }
                for (int j = 0; j < numEntries; j++)
                {
                    Add(RandomNonEmptyString(), RandomNonEmptyString(), random.NextBoolean());
                }
                SynonymMap map = b.Build();
                bool ignoreCase = random.NextBoolean();

                Analyzer analyzer = Analyzer.NewAnonymous(createComponents: (fieldName, reader) =>
                {
                    Tokenizer tokenizer = new MockTokenizer(reader, MockTokenizer.SIMPLE, true);
                    return new TokenStreamComponents(tokenizer, new SynonymFilter(tokenizer, map, ignoreCase));
                });

                CheckRandomData(random, analyzer, 100, 1024);
            }
        }

        // LUCENE-3375
        [Test]
        public virtual void TestVanishingTerms()
        {
            string testFile = "aaa => aaaa1 aaaa2 aaaa3\n" + "bbb => bbbb1 bbbb2\n";

            SolrSynonymParser parser = new SolrSynonymParser(true, true, new MockAnalyzer(Random));
            parser.Parse(new StringReader(testFile));
            SynonymMap map = parser.Build();

            Analyzer analyzer = Analyzer.NewAnonymous(createComponents: (fieldName, reader) =>
            {
                Tokenizer tokenizer = new MockTokenizer(reader, MockTokenizer.WHITESPACE, true);
                return new TokenStreamComponents(tokenizer, new SynonymFilter(tokenizer, map, true));
            });

            // where did my pot go?!
            AssertAnalyzesTo(analyzer, "xyzzy bbb pot of gold", new string[] { "xyzzy", "bbbb1", "pot", "bbbb2", "of", "gold" });

            // this one nukes 'pot' and 'of'
            // xyzzy aaa pot of gold -> xyzzy aaaa1 aaaa2 aaaa3 gold
            AssertAnalyzesTo(analyzer, "xyzzy aaa pot of gold", new string[] { "xyzzy", "aaaa1", "pot", "aaaa2", "of", "aaaa3", "gold" });
        }

        [Test]
        public virtual void TestBasic2()
        {
            b = new SynonymMap.Builder(true);
            const bool keepOrig = false;
            Add("aaa", "aaaa1 aaaa2 aaaa3", keepOrig);
            Add("bbb", "bbbb1 bbbb2", keepOrig);
            tokensIn = new MockTokenizer(new StringReader("a"), MockTokenizer.WHITESPACE, true);
            tokensIn.Reset();
            assertTrue(tokensIn.IncrementToken());
            assertFalse(tokensIn.IncrementToken());
            tokensIn.End();
            tokensIn.Dispose();

            tokensOut = new SynonymFilter(tokensIn, b.Build(), true);
            termAtt = tokensOut.AddAttribute<ICharTermAttribute>();
            posIncrAtt = tokensOut.AddAttribute<IPositionIncrementAttribute>();
            posLenAtt = tokensOut.AddAttribute<IPositionLengthAttribute>();
            offsetAtt = tokensOut.AddAttribute<IOffsetAttribute>();

#pragma warning disable 162
            if (keepOrig)
            {
                Verify("xyzzy bbb pot of gold", "xyzzy bbb/bbbb1 pot/bbbb2 of gold");
                Verify("xyzzy aaa pot of gold", "xyzzy aaa/aaaa1 pot/aaaa2 of/aaaa3 gold");
            }
            else
            {
                Verify("xyzzy bbb pot of gold", "xyzzy bbbb1 pot/bbbb2 of gold");
                Verify("xyzzy aaa pot of gold", "xyzzy aaaa1 pot/aaaa2 of/aaaa3 gold");
            }
#pragma warning restore 612, 618
        }

        [Test]
        public virtual void TestMatching()
        {
            b = new SynonymMap.Builder(true);
            const bool keepOrig = false;
            Add("a b", "ab", keepOrig);
            Add("a c", "ac", keepOrig);
            Add("a", "aa", keepOrig);
            Add("b", "bb", keepOrig);
            Add("z x c v", "zxcv", keepOrig);
            Add("x c", "xc", keepOrig);
            SynonymMap map = b.Build();
            Analyzer a = Analyzer.NewAnonymous(createComponents: (fieldName, reader) =>
            {
                Tokenizer tokenizer = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
                return new TokenStreamComponents(tokenizer, new SynonymFilter(tokenizer, map, true));
            });

            CheckOneTerm(a, "$", "$");
            CheckOneTerm(a, "a", "aa");
            CheckOneTerm(a, "b", "bb");

            AssertAnalyzesTo(a, "a $", new string[] { "aa", "$" }, new int[] { 1, 1 });

            AssertAnalyzesTo(a, "$ a", new string[] { "$", "aa" }, new int[] { 1, 1 });

            AssertAnalyzesTo(a, "a a", new string[] { "aa", "aa" }, new int[] { 1, 1 });

            AssertAnalyzesTo(a, "z x c v", new string[] { "zxcv" }, new int[] { 1 });

            AssertAnalyzesTo(a, "z x c $", new string[] { "z", "xc", "$" }, new int[] { 1, 1, 1 });
        }

        [Test]
        public virtual void TestRepeatsOff()
        {
            b = new SynonymMap.Builder(true);
            const bool keepOrig = false;
            Add("a b", "ab", keepOrig);
            Add("a b", "ab", keepOrig);
            Add("a b", "ab", keepOrig);
            SynonymMap map = b.Build();
            Analyzer a = Analyzer.NewAnonymous(createComponents: (fieldName, reader) =>
            {
                Tokenizer tokenizer = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
                return new TokenStreamComponents(tokenizer, new SynonymFilter(tokenizer, map, true));
            });

            AssertAnalyzesTo(a, "a b", new string[] { "ab" }, new int[] { 1 });
        }

        [Test]
        public virtual void TestRepeatsOn()
        {
            b = new SynonymMap.Builder(false);
            const bool keepOrig = false;
            Add("a b", "ab", keepOrig);
            Add("a b", "ab", keepOrig);
            Add("a b", "ab", keepOrig);
            SynonymMap map = b.Build();
            Analyzer a = Analyzer.NewAnonymous(createComponents: (fieldName, reader) =>
            {
                Tokenizer tokenizer = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
                return new TokenStreamComponents(tokenizer, new SynonymFilter(tokenizer, map, true));
            });

            AssertAnalyzesTo(a, "a b", new string[] { "ab", "ab", "ab" }, new int[] { 1, 0, 0 });
        }

        [Test]
        public virtual void TestRecursion()
        {
            b = new SynonymMap.Builder(true);
            const bool keepOrig = false;
            Add("zoo", "zoo", keepOrig);
            SynonymMap map = b.Build();
            Analyzer a = Analyzer.NewAnonymous(createComponents: (fieldName, reader) =>
            {
                Tokenizer tokenizer = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
                return new TokenStreamComponents(tokenizer, new SynonymFilter(tokenizer, map, true));
            });

            AssertAnalyzesTo(a, "zoo zoo $ zoo", new string[] { "zoo", "zoo", "$", "zoo" }, new int[] { 1, 1, 1, 1 });
        }

        [Test]
        public virtual void TestRecursion2()
        {
            b = new SynonymMap.Builder(true);
            const bool keepOrig = false;
            Add("zoo", "zoo", keepOrig);
            Add("zoo", "zoo zoo", keepOrig);
            SynonymMap map = b.Build();
            Analyzer a = Analyzer.NewAnonymous(createComponents: (fieldName, reader) =>
            {
                Tokenizer tokenizer = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
                return new TokenStreamComponents(tokenizer, new SynonymFilter(tokenizer, map, true));
            });

            // verify("zoo zoo $ zoo", "zoo/zoo zoo/zoo/zoo $/zoo zoo/zoo zoo");
            AssertAnalyzesTo(a, "zoo zoo $ zoo", new string[] { "zoo", "zoo", "zoo", "zoo", "zoo", "$", "zoo", "zoo", "zoo", "zoo" }, new int[] { 1, 0, 1, 0, 0, 1, 0, 1, 0, 1 });
        }

        [Test]
        public virtual void TestOutputHangsOffEnd()
        {
            b = new SynonymMap.Builder(true);
            const bool keepOrig = false;
            // b hangs off the end (no input token under it):
            Add("a", "a b", keepOrig);
            tokensIn = new MockTokenizer(new StringReader("a"), MockTokenizer.WHITESPACE, true);
            tokensIn.Reset();
            assertTrue(tokensIn.IncrementToken());
            assertFalse(tokensIn.IncrementToken());
            tokensIn.End();
            tokensIn.Dispose();

            tokensOut = new SynonymFilter(tokensIn, b.Build(), true);
            termAtt = tokensOut.AddAttribute<ICharTermAttribute>();
            posIncrAtt = tokensOut.AddAttribute<IPositionIncrementAttribute>();
            offsetAtt = tokensOut.AddAttribute<IOffsetAttribute>();
            posLenAtt = tokensOut.AddAttribute<IPositionLengthAttribute>();

            // Make sure endOffset inherits from previous input token:
            Verify("a", "a b:1");
        }

        [Test]
        public virtual void TestIncludeOrig()
        {
            b = new SynonymMap.Builder(true);
            const bool keepOrig = true;
            Add("a b", "ab", keepOrig);
            Add("a c", "ac", keepOrig);
            Add("a", "aa", keepOrig);
            Add("b", "bb", keepOrig);
            Add("z x c v", "zxcv", keepOrig);
            Add("x c", "xc", keepOrig);
            SynonymMap map = b.Build();
            Analyzer a = Analyzer.NewAnonymous(createComponents: (fieldName, reader) =>
            {
                Tokenizer tokenizer = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
                return new TokenStreamComponents(tokenizer, new SynonymFilter(tokenizer, map, true));
            });

            AssertAnalyzesTo(a, "$", new string[] { "$" }, new int[] { 1 });
            AssertAnalyzesTo(a, "a", new string[] { "a", "aa" }, new int[] { 1, 0 });
            AssertAnalyzesTo(a, "a", new string[] { "a", "aa" }, new int[] { 1, 0 });
            AssertAnalyzesTo(a, "$ a", new string[] { "$", "a", "aa" }, new int[] { 1, 1, 0 });
            AssertAnalyzesTo(a, "a $", new string[] { "a", "aa", "$" }, new int[] { 1, 0, 1 });
            AssertAnalyzesTo(a, "$ a !", new string[] { "$", "a", "aa", "!" }, new int[] { 1, 1, 0, 1 });
            AssertAnalyzesTo(a, "a a", new string[] { "a", "aa", "a", "aa" }, new int[] { 1, 0, 1, 0 });
            AssertAnalyzesTo(a, "b", new string[] { "b", "bb" }, new int[] { 1, 0 });
            AssertAnalyzesTo(a, "z x c v", new string[] { "z", "zxcv", "x", "c", "v" }, new int[] { 1, 0, 1, 1, 1 });
            AssertAnalyzesTo(a, "z x c $", new string[] { "z", "x", "xc", "c", "$" }, new int[] { 1, 1, 0, 1, 1 });
        }

        [Test]
        public virtual void TestRecursion3()
        {
            b = new SynonymMap.Builder(true);
            const bool keepOrig = true;
            Add("zoo zoo", "zoo", keepOrig);
            SynonymMap map = b.Build();
            Analyzer a = Analyzer.NewAnonymous(createComponents: (fieldName, reader) =>
            {
                Tokenizer tokenizer = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
                return new TokenStreamComponents(tokenizer, new SynonymFilter(tokenizer, map, true));
            });

            AssertAnalyzesTo(a, "zoo zoo $ zoo", new string[] { "zoo", "zoo", "zoo", "$", "zoo" }, new int[] { 1, 0, 1, 1, 1 });
        }

        [Test]
        public virtual void TestRecursion4()
        {
            b = new SynonymMap.Builder(true);
            const bool keepOrig = true;
            Add("zoo zoo", "zoo", keepOrig);
            Add("zoo", "zoo zoo", keepOrig);
            SynonymMap map = b.Build();
            Analyzer a = Analyzer.NewAnonymous(createComponents: (fieldName, reader) =>
            {
                Tokenizer tokenizer = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
                return new TokenStreamComponents(tokenizer, new SynonymFilter(tokenizer, map, true));
            });

            AssertAnalyzesTo(a, "zoo zoo $ zoo", new string[] { "zoo", "zoo", "zoo", "$", "zoo", "zoo", "zoo" }, new int[] { 1, 0, 1, 1, 1, 0, 1 });
        }

        [Test]
        public virtual void TestMultiwordOffsets()
        {
            b = new SynonymMap.Builder(true);
            const bool keepOrig = true;
            Add("national hockey league", "nhl", keepOrig);
            SynonymMap map = b.Build();
            Analyzer a = Analyzer.NewAnonymous(createComponents: (fieldName, reader) =>
            {
                Tokenizer tokenizer = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
                return new TokenStreamComponents(tokenizer, new SynonymFilter(tokenizer, map, true));
            });

            AssertAnalyzesTo(a, "national hockey league", new string[] { "national", "nhl", "hockey", "league" }, new int[] { 0, 0, 9, 16 }, new int[] { 8, 22, 15, 22 }, new int[] { 1, 0, 1, 1 });
        }

        [Test]
        public virtual void TestEmpty()
        {
            Tokenizer tokenizer = new MockTokenizer(new StringReader("aa bb"));
            try
            {
                new SynonymFilter(tokenizer, (new SynonymMap.Builder(true)).Build(), true);
                fail("did not hit expected exception");
            }
            catch (ArgumentNullException iae) // LUCENENET specific - changed from IllegalArgumentException to ArgumentNullException (.NET convention)
            {
                // expected
                assertTrue(iae.Message.Contains("fst must be non-null")); // LUCENENET: .NET Adds the parameter name to the message
            }
        }
    }
}