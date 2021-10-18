using J2N.Text;
using J2N.Threading;
using J2N.Threading.Atomic;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Core;
using Lucene.Net.Analysis.TokenAttributes;
using Lucene.Net.Analysis.Util;
using Lucene.Net.Support.Threading;
using Lucene.Net.Util;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using static Lucene.Net.Search.Suggest.Lookup;
using Console = Lucene.Net.Util.SystemConsole;
using JCG = J2N.Collections.Generic;

namespace Lucene.Net.Search.Suggest.Analyzing
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


    // Test requires postings offsets:
    [SuppressCodecs("Lucene3x", "MockFixedIntBlock", "MockVariableIntBlock", "MockSep", "MockRandom")]
    public class AnalyzingInfixSuggesterTest : LuceneTestCase
    {
        [Test]
        public void TestBasic()
        {
            Input[] keys = new Input[] {
                new Input("lend me your ear", 8, new BytesRef("foobar")),
                new Input("a penny saved is a penny earned", 10, new BytesRef("foobaz")),
            };

            Analyzer a = new MockAnalyzer(Random, MockTokenizer.WHITESPACE, false);
            using AnalyzingInfixSuggester suggester = new AnalyzingInfixSuggester(TEST_VERSION_CURRENT, NewDirectory(), a, a, 3);     //LUCENENET TODO: add extra false param at version 4.11.0
            suggester.Build(new InputArrayEnumerator(keys));

            IList<Lookup.LookupResult> results = suggester.DoLookup(TestUtil.StringToCharSequence("ear", Random).ToString(), 10, true, true);
            assertEquals(2, results.size());
            assertEquals("a penny saved is a penny <b>ear</b>ned", results[0].Key);
            assertEquals(10, results[0].Value);
            assertEquals(new BytesRef("foobaz"), results[0].Payload);

            assertEquals("lend me your <b>ear</b>", results[1].Key);
            assertEquals(8, results[1].Value);
            assertEquals(new BytesRef("foobar"), results[1].Payload);

            results = suggester.DoLookup(TestUtil.StringToCharSequence("ear ", Random).ToString(), 10, true, true);
            assertEquals(1, results.size());
            assertEquals("lend me your <b>ear</b>", results[0].Key);
            assertEquals(8, results[0].Value);
            assertEquals(new BytesRef("foobar"), results[0].Payload);

            results = suggester.DoLookup(TestUtil.StringToCharSequence("pen", Random).ToString(), 10, true, true);
            assertEquals(1, results.size());
            assertEquals("a <b>pen</b>ny saved is a <b>pen</b>ny earned", results[0].Key);
            assertEquals(10, results[0].Value);
            assertEquals(new BytesRef("foobaz"), results[0].Payload);

            results = suggester.DoLookup(TestUtil.StringToCharSequence("p", Random).ToString(), 10, true, true);
            assertEquals(1, results.size());
            assertEquals("a <b>p</b>enny saved is a <b>p</b>enny earned", results[0].Key);
            assertEquals(10, results[0].Value);
            assertEquals(new BytesRef("foobaz"), results[0].Payload);
        }

        [Test]
        public void TestAfterLoad()
        {
            Input[] keys = new Input[] {
                new Input("lend me your ear", 8, new BytesRef("foobar")),
                new Input("a penny saved is a penny earned", 10, new BytesRef("foobaz")),
            };

            DirectoryInfo tempDir = CreateTempDir("AnalyzingInfixSuggesterTest");

            Analyzer a = new MockAnalyzer(Random, MockTokenizer.WHITESPACE, false);
            AnalyzingInfixSuggester suggester = new AnalyzingInfixSuggester(TEST_VERSION_CURRENT, NewFSDirectory(tempDir), a, a, 3);     //LUCENENET TODO: add extra false param at version 4.11.0
            try
            {
                suggester.Build(new InputArrayEnumerator(keys));
                assertEquals(2, suggester.Count);
                suggester.Dispose();

                suggester = new AnalyzingInfixSuggester(TEST_VERSION_CURRENT, NewFSDirectory(tempDir), a, a, 3);                            //LUCENENET TODO: add extra false param at version 4.11.0
                IList<Lookup.LookupResult> results = suggester.DoLookup(TestUtil.StringToCharSequence("ear", Random).ToString(), 10, true, true);
                assertEquals(2, results.size());
                assertEquals("a penny saved is a penny <b>ear</b>ned", results[0].Key);
                assertEquals(10, results[0].Value);
                assertEquals(new BytesRef("foobaz"), results[0].Payload);
                assertEquals(2, suggester.Count);
            }
            finally
            {
                suggester.Dispose();
            }
        }

        /// <summary>
        /// Used to return highlighted result; see 
        /// <see cref = "Lookup.LookupResult.HighlightKey" />
        /// </summary>
        private sealed class LookupHighlightFragment
        {
            /// <summary>Portion of text for this fragment.</summary>
            public readonly string text;

            /** True if this text matched a part of the user's
             *  query. */
            public readonly bool isHit;

            /** Sole constructor. */
            public LookupHighlightFragment(string text, bool isHit)
            {
                this.text = text;
                this.isHit = isHit;
            }

            public override string ToString()
            {
                return "LookupHighlightFragment(text=" + text + " isHit=" + isHit + ")";
            }
        }

        internal class TestHighlightAnalyzingInfixSuggester : AnalyzingInfixSuggester
        {
            public TestHighlightAnalyzingInfixSuggester(AnalyzingInfixSuggesterTest outerInstance, Analyzer a)
                : base(TEST_VERSION_CURRENT, NewDirectory(), a, a, 3)                                                               //LUCENENET TODO: add extra false param at version 4.11.0
            {
            }

            protected internal override object Highlight(string text, ICollection<string> matchedTokens, string prefixToken)
            {
                TokenStream ts = m_queryAnalyzer.GetTokenStream("text", new StringReader(text));
                try
                {
                    ICharTermAttribute termAtt = ts.AddAttribute<ICharTermAttribute>();
                    IOffsetAttribute offsetAtt = ts.AddAttribute<IOffsetAttribute>();
                    ts.Reset();
                    IList<LookupHighlightFragment> fragments = new JCG.List<LookupHighlightFragment>();
                    int upto = 0;
                    while (ts.IncrementToken())
                    {
                        string token = termAtt.toString();
                        int startOffset = offsetAtt.StartOffset;
                        int endOffset = offsetAtt.EndOffset;
                        if (upto < startOffset)
                        {
                            fragments.Add(new LookupHighlightFragment(text.Substring(upto, startOffset - upto), false));
                            upto = startOffset;
                        }
                        else if (upto > startOffset)
                        {
                            continue;
                        }

                        if (matchedTokens.Contains(token))
                        {
                            // Token matches.
                            fragments.Add(new LookupHighlightFragment(text.Substring(startOffset, endOffset - startOffset), true));
                            upto = endOffset;
                        }
                        else if (prefixToken != null && token.StartsWith(prefixToken, StringComparison.Ordinal))
                        {
                            fragments.Add(new LookupHighlightFragment(text.Substring(startOffset, prefixToken.Length), true));
                            if (prefixToken.Length < token.Length)
                            {
                                fragments.Add(new LookupHighlightFragment(text.Substring(startOffset + prefixToken.Length, (startOffset + token.Length) - (startOffset + prefixToken.Length)), false));
                            }
                            upto = endOffset;
                        }
                    }
                    ts.End();
                    int endOffset2 = offsetAtt.EndOffset;
                    if (upto < endOffset2)
                    {
                        fragments.Add(new LookupHighlightFragment(text.Substring(upto), false));
                    }

                    return fragments;
                }
                finally
                {
                    IOUtils.DisposeWhileHandlingException(ts);
                }
            }
        }

        [Test]
        public void TestHighlightAsObject()
        {
            Input[] keys = new Input[] {
                new Input("a penny saved is a penny earned", 10, new BytesRef("foobaz")),
            };

            Analyzer a = new MockAnalyzer(Random, MockTokenizer.WHITESPACE, false);
            using AnalyzingInfixSuggester suggester = new TestHighlightAnalyzingInfixSuggester(this, a);
            suggester.Build(new InputArrayEnumerator(keys));

            IList<Lookup.LookupResult> results = suggester.DoLookup(TestUtil.StringToCharSequence("ear", Random).ToString(), 10, true, true);
            assertEquals(1, results.size());
            assertEquals("a penny saved is a penny <b>ear</b>ned", ToString((IList<LookupHighlightFragment>)results[0].HighlightKey));
            assertEquals(10, results[0].Value);
            assertEquals(new BytesRef("foobaz"), results[0].Payload);
        }

        private string ToString(IEnumerable<LookupHighlightFragment> fragments)
        {
            StringBuilder sb = new StringBuilder();
            foreach (LookupHighlightFragment fragment in fragments)
            {
                if (fragment.isHit)
                {
                    sb.append("<b>");
                }
                sb.append(fragment.text);
                if (fragment.isHit)
                {
                    sb.append("</b>");
                }
            }

            return sb.toString();
        }

        [Test]
        public void TestRandomMinPrefixLength()
        {
            Input[] keys = new Input[] {
                new Input("lend me your ear", 8, new BytesRef("foobar")),
                new Input("a penny saved is a penny earned", 10, new BytesRef("foobaz")),
            };
            DirectoryInfo tempDir = CreateTempDir("AnalyzingInfixSuggesterTest");

            Analyzer a = new MockAnalyzer(Random, MockTokenizer.WHITESPACE, false);
            int minPrefixLength = Random.nextInt(10);
            AnalyzingInfixSuggester suggester = new AnalyzingInfixSuggester(TEST_VERSION_CURRENT, NewFSDirectory(tempDir), a, a, minPrefixLength);     //LUCENENET TODO: add extra false param at version 4.11.0
            try
            {
                suggester.Build(new InputArrayEnumerator(keys));

                for (int i = 0; i < 2; i++)
                {
                    for (int j = 0; j < 2; j++)
                    {
                        bool doHighlight = j == 0;

                        IList<Lookup.LookupResult> results = suggester.DoLookup(TestUtil.StringToCharSequence("ear", Random).ToString(), 10, true, doHighlight);
                        assertEquals(2, results.size());
                        if (doHighlight)
                        {
                            assertEquals("a penny saved is a penny <b>ear</b>ned", results[0].Key);
                        }
                        else
                        {
                            assertEquals("a penny saved is a penny earned", results[0].Key);
                        }
                        assertEquals(10, results[0].Value);
                        if (doHighlight)
                        {
                            assertEquals("lend me your <b>ear</b>", results[1].Key);
                        }
                        else
                        {
                            assertEquals("lend me your ear", results[1].Key);
                        }
                        assertEquals(new BytesRef("foobaz"), results[0].Payload);
                        assertEquals(8, results[1].Value);
                        assertEquals(new BytesRef("foobar"), results[1].Payload);

                        results = suggester.DoLookup(TestUtil.StringToCharSequence("ear ", Random).ToString(), 10, true, doHighlight);
                        assertEquals(1, results.size());
                        if (doHighlight)
                        {
                            assertEquals("lend me your <b>ear</b>", results[0].Key);
                        }
                        else
                        {
                            assertEquals("lend me your ear", results[0].Key);
                        }
                        assertEquals(8, results[0].Value);
                        assertEquals(new BytesRef("foobar"), results[0].Payload);

                        results = suggester.DoLookup(TestUtil.StringToCharSequence("pen", Random).ToString(), 10, true, doHighlight);
                        assertEquals(1, results.size());
                        if (doHighlight)
                        {
                            assertEquals("a <b>pen</b>ny saved is a <b>pen</b>ny earned", results[0].Key);
                        }
                        else
                        {
                            assertEquals("a penny saved is a penny earned", results[0].Key);
                        }
                        assertEquals(10, results[0].Value);
                        assertEquals(new BytesRef("foobaz"), results[0].Payload);

                        results = suggester.DoLookup(TestUtil.StringToCharSequence("p", Random).ToString(), 10, true, doHighlight);
                        assertEquals(1, results.size());
                        if (doHighlight)
                        {
                            assertEquals("a <b>p</b>enny saved is a <b>p</b>enny earned", results[0].Key);
                        }
                        else
                        {
                            assertEquals("a penny saved is a penny earned", results[0].Key);
                        }
                        assertEquals(10, results[0].Value);
                        assertEquals(new BytesRef("foobaz"), results[0].Payload);
                    }

                    // Make sure things still work after close and reopen:
                    suggester.Dispose();
                    suggester = new AnalyzingInfixSuggester(TEST_VERSION_CURRENT, NewFSDirectory(tempDir), a, a, minPrefixLength);     //LUCENENET TODO: add extra false param at version 4.11.0
                }
            }
            finally
            {
                suggester.Dispose();
            }

        }

        [Test]
        public void TestHighlight()
        {
            Input[] keys = new Input[] {
                new Input("a penny saved is a penny earned", 10, new BytesRef("foobaz")),
            };

            Analyzer a = new MockAnalyzer(Random, MockTokenizer.WHITESPACE, false);
            using AnalyzingInfixSuggester suggester = new AnalyzingInfixSuggester(TEST_VERSION_CURRENT, NewDirectory(), a, a, 3);     //LUCENENET TODO: add extra false param at version 4.11.0
            suggester.Build(new InputArrayEnumerator(keys));
            IList<Lookup.LookupResult> results = suggester.DoLookup(TestUtil.StringToCharSequence("penn", Random).ToString(), 10, true, true);
            assertEquals(1, results.size());
            assertEquals("a <b>penn</b>y saved is a <b>penn</b>y earned", results[0].Key);
        }

        internal class TestHighlightChangeCaseAnalyzingInfixSuggester : AnalyzingInfixSuggester
        {
            private readonly AnalyzingInfixSuggesterTest outerInstance;
            public TestHighlightChangeCaseAnalyzingInfixSuggester(AnalyzingInfixSuggesterTest outerInstance, Analyzer a)
                : base(TEST_VERSION_CURRENT, NewDirectory(), a, a, 3)                                                               //LUCENENET TODO: add extra false param at version 4.11.0
            {
                this.outerInstance = outerInstance;
            }

            protected internal override void AddPrefixMatch(StringBuilder sb, string surface, string analyzed, string prefixToken)
            {
                sb.append("<b>");
                sb.append(surface);
                sb.append("</b>");
            }
        }

        [Test]
        public void TestHighlightCaseChange()
        {
            Input[] keys = new Input[] {
                new Input("a Penny saved is a penny earned", 10, new BytesRef("foobaz")),
            };

            Analyzer a = new MockAnalyzer(Random, MockTokenizer.WHITESPACE, true);
            IList<Lookup.LookupResult> results;
            using (AnalyzingInfixSuggester suggester = new AnalyzingInfixSuggester(TEST_VERSION_CURRENT, NewDirectory(), a, a, 3))                  //LUCENENET TODO: add extra false param at version 4.11.0
            {
                suggester.Build(new InputArrayEnumerator(keys));
                results = suggester.DoLookup(TestUtil.StringToCharSequence("penn", Random).ToString(), 10, true, true);
                assertEquals(1, results.size());
                assertEquals("a <b>Penn</b>y saved is a <b>penn</b>y earned", results[0].Key);
            }

            // Try again, but overriding addPrefixMatch to highlight
            // the entire hit:
            using (var suggester = new TestHighlightChangeCaseAnalyzingInfixSuggester(this, a))
            {
                suggester.Build(new InputArrayEnumerator(keys));
                results = suggester.DoLookup(TestUtil.StringToCharSequence("penn", Random).ToString(), 10, true, true);
                assertEquals(1, results.size());
                assertEquals("a <b>Penny</b> saved is a <b>penny</b> earned", results[0].Key);
            }
        }

        [Test]
        public void TestDoubleClose()
        {
            Input[] keys = new Input[] {
                new Input("a penny saved is a penny earned", 10, new BytesRef("foobaz")),
            };

            Analyzer a = new MockAnalyzer(Random, MockTokenizer.WHITESPACE, false);
            using AnalyzingInfixSuggester suggester = new AnalyzingInfixSuggester(TEST_VERSION_CURRENT, NewDirectory(), a, a, 3);
            suggester.Build(new InputArrayEnumerator(keys));
            suggester.Dispose();
        }

        [Test]
        public void TestSuggestStopFilter()
        {
            CharArraySet stopWords = StopFilter.MakeStopSet(TEST_VERSION_CURRENT, "a");
            Analyzer indexAnalyzer = Analyzer.NewAnonymous(createComponents: (fieldName, reader) =>
            {
                MockTokenizer tokens = new MockTokenizer(reader);
                return new TokenStreamComponents(tokens,
                    new StopFilter(TEST_VERSION_CURRENT, tokens, stopWords));
            });
            Analyzer queryAnalyzer = Analyzer.NewAnonymous(createComponents: (fieldName, reader) =>
            {
                MockTokenizer tokens = new MockTokenizer(reader);
                return new TokenStreamComponents(tokens,
                    new SuggestStopFilter(tokens, stopWords));
            });

            using AnalyzingInfixSuggester suggester = new AnalyzingInfixSuggester(TEST_VERSION_CURRENT, NewDirectory(), indexAnalyzer, queryAnalyzer, 3);
            Input[] keys = new Input[] {
                    new Input("a bob for apples", 10, new BytesRef("foobaz")),
                };

            suggester.Build(new InputArrayEnumerator(keys));
            IList<Lookup.LookupResult> results = suggester.DoLookup(TestUtil.StringToCharSequence("a", Random).ToString(), 10, true, true);
            assertEquals(1, results.size());
            assertEquals("a bob for <b>a</b>pples", results[0].Key);
        }

        [Test]
        public void TestEmptyAtStart()
        {
            Analyzer a = new MockAnalyzer(Random, MockTokenizer.WHITESPACE, false);
            using AnalyzingInfixSuggester suggester = new AnalyzingInfixSuggester(TEST_VERSION_CURRENT, NewDirectory(), a, a, 3);                       //LUCENENET TODO: add extra false param at version 4.11.0              
            suggester.Build(new InputArrayEnumerator(new Input[0]));
            suggester.Add(new BytesRef("a penny saved is a penny earned"), null, 10, new BytesRef("foobaz"));
            suggester.Add(new BytesRef("lend me your ear"), null, 8, new BytesRef("foobar"));
            suggester.Refresh();
            IList<Lookup.LookupResult> results = suggester.DoLookup(TestUtil.StringToCharSequence("ear", Random).ToString(), 10, true, true);
            assertEquals(2, results.size());
            assertEquals("a penny saved is a penny <b>ear</b>ned", results[0].Key);
            assertEquals(10, results[0].Value);
            assertEquals(new BytesRef("foobaz"), results[0].Payload);

            assertEquals("lend me your <b>ear</b>", results[1].Key);
            assertEquals(8, results[1].Value);
            assertEquals(new BytesRef("foobar"), results[1].Payload);

            results = suggester.DoLookup(TestUtil.StringToCharSequence("ear ", Random).ToString(), 10, true, true);
            assertEquals(1, results.size());
            assertEquals("lend me your <b>ear</b>", results[0].Key);
            assertEquals(8, results[0].Value);
            assertEquals(new BytesRef("foobar"), results[0].Payload);

            results = suggester.DoLookup(TestUtil.StringToCharSequence("pen", Random).ToString(), 10, true, true);
            assertEquals(1, results.size());
            assertEquals("a <b>pen</b>ny saved is a <b>pen</b>ny earned", results[0].Key);
            assertEquals(10, results[0].Value);
            assertEquals(new BytesRef("foobaz"), results[0].Payload);

            results = suggester.DoLookup(TestUtil.StringToCharSequence("p", Random).ToString(), 10, true, true);
            assertEquals(1, results.size());
            assertEquals("a <b>p</b>enny saved is a <b>p</b>enny earned", results[0].Key);
            assertEquals(10, results[0].Value);
            assertEquals(new BytesRef("foobaz"), results[0].Payload);
        }

        [Test]
        public void TestBothExactAndPrefix()
        {
            Analyzer a = new MockAnalyzer(Random, MockTokenizer.WHITESPACE, false);
            using AnalyzingInfixSuggester suggester = new AnalyzingInfixSuggester(TEST_VERSION_CURRENT, NewDirectory(), a, a, 3);     //LUCENENET TODO: add extra false param at version 4.11.0
            suggester.Build(new InputArrayEnumerator(new Input[0]));
            suggester.Add(new BytesRef("the pen is pretty"), null, 10, new BytesRef("foobaz"));
            suggester.Refresh();

            IList<Lookup.LookupResult> results = suggester.DoLookup(TestUtil.StringToCharSequence("pen p", Random).ToString(), 10, true, true);
            assertEquals(1, results.size());
            assertEquals("the <b>pen</b> is <b>p</b>retty", results[0].Key);
            assertEquals(10, results[0].Value);
            assertEquals(new BytesRef("foobaz"), results[0].Payload);
        }

        private static string RandomText()
        {
            int numWords = TestUtil.NextInt32(Random, 1, 4);

            StringBuilder b = new StringBuilder();
            for (int i = 0; i < numWords; i++)
            {
                if (i > 0)
                {
                    b.append(' ');
                }
                b.append(TestUtil.RandomSimpleString(Random, 1, 10));
            }

            return b.toString();
        }

        private class Update
        {
            internal long weight;
            internal int index;
        }

        private class LookupThread : ThreadJob
        {
            private readonly AnalyzingInfixSuggesterTest outerInstance;

            private readonly AnalyzingInfixSuggester suggester;
            private readonly AtomicBoolean stop;

            public LookupThread(AnalyzingInfixSuggesterTest outerInstance, AnalyzingInfixSuggester suggester)
            {
                this.outerInstance = outerInstance;
                this.suggester = suggester;
                this.stop = new AtomicBoolean(false);
            }

            public virtual void Finish()
            {
                stop.Value = true;
                this.Join();
            }

            public override void Run()
            {
                Priority += 1;
                while (!stop)
                {
                    string query = RandomText();
                    int topN = TestUtil.NextInt32(Random, 1, 100);
                    bool allTermsRequired = Random.nextBoolean();
                    bool doHilite = Random.nextBoolean();
                    // We don't verify the results; just doing
                    // simultaneous lookups while adding/updating to
                    // see if there are any thread hazards:
                    try
                    {
                        suggester.DoLookup(TestUtil.StringToCharSequence(query, Random).ToString(),
                                         topN, allTermsRequired, doHilite);
                        Thread.Sleep(10);// don't starve refresh()'s CPU, which sleeps every 50 bytes for 1 ms
                    }
                    catch (Exception e) when (e.IsIOException())
                    {
                        throw RuntimeException.Create(e);
                    }
                }
            }
        }

        internal class TestRandomNRTComparer : IComparer<Input>
        {
            public int Compare(Input a, Input b)
            {
                if (a.v > b.v)
                {
                    return -1;
                }
                else if (a.v < b.v)
                {
                    return 1;
                }
                else
                {
                    return 0;
                }
            }
        }

        [Test]
        [Slow]
        public void TestRandomNRT()
        {
            DirectoryInfo tempDir = CreateTempDir("AnalyzingInfixSuggesterTest");
            Analyzer a = new MockAnalyzer(Random, MockTokenizer.WHITESPACE, false);
            int minPrefixChars = Random.nextInt(7);
            if (Verbose)
            {
                Console.WriteLine("  minPrefixChars=" + minPrefixChars);
            }

            AnalyzingInfixSuggester suggester = new AnalyzingInfixSuggester(TEST_VERSION_CURRENT, NewFSDirectory(tempDir), a, a, minPrefixChars);     //LUCENENET TODO: add extra false param at version 4.11.0
            try
            {

                // Initial suggester built with nothing:
                suggester.Build(new InputArrayEnumerator(new Input[0]));

                LookupThread lookupThread = new LookupThread(this, suggester);
                lookupThread.Start();

                int iters = AtLeast(1000);
                int visibleUpto = 0;

                ISet<long> usedWeights = new JCG.HashSet<long>();
                ISet<string> usedKeys = new JCG.HashSet<string>();

                IList<Input> inputs = new JCG.List<Input>();
                IList<Update> pendingUpdates = new JCG.List<Update>();

                for (int iter = 0; iter < iters; iter++)
                {
                    string text;
                    while (true)
                    {
                        text = RandomText();
                        if (usedKeys.contains(text) == false)
                        {
                            usedKeys.add(text);
                            break;
                        }
                    }

                    // Carefully pick a weight we never used, to sidestep
                    // tie-break problems:
                    long weight;
                    while (true)
                    {
                        weight = Random.nextInt(10 * iters);
                        if (usedWeights.contains(weight) == false)
                        {
                            usedWeights.add(weight);
                            break;
                        }
                    }

                    if (inputs.size() > 0 && Random.nextInt(4) == 1)
                    {
                        // Update an existing suggestion
                        Update update = new Update();
                        update.index = Random.nextInt(inputs.size());
                        update.weight = weight;
                        Input input = inputs[update.index];
                        pendingUpdates.Add(update);
                        if (Verbose)
                        {
                            Console.WriteLine("TEST: iter=" + iter + " update input=" + input.term.Utf8ToString() + "/" + weight);
                        }
                        suggester.Update(input.term, null, weight, input.term);

                    }
                    else
                    {
                        // Add a new suggestion
                        inputs.Add(new Input(text, weight, new BytesRef(text)));
                        if (Verbose)
                        {
                            Console.WriteLine("TEST: iter=" + iter + " add input=" + text + "/" + weight);
                        }
                        BytesRef br = new BytesRef(text);
                        suggester.Add(br, null, weight, br);
                    }

                    if (Random.nextInt(15) == 7)
                    {
                        if (Verbose)
                        {
                            Console.WriteLine("TEST: now refresh suggester");
                        }
                        suggester.Refresh();
                        visibleUpto = inputs.size();
                        foreach (Update update in pendingUpdates)
                        {
                            Input oldInput = inputs[update.index];
                            Input newInput = new Input(oldInput.term, update.weight, oldInput.payload);
                            inputs[update.index] = newInput;
                        }
                        pendingUpdates.Clear();
                    }

                    if (Random.nextInt(50) == 7)
                    {
                        if (Verbose)
                        {
                            Console.WriteLine("TEST: now close/reopen suggester");
                        }
                        lookupThread.Finish();
                        suggester.Dispose();
                        suggester = new AnalyzingInfixSuggester(TEST_VERSION_CURRENT, NewFSDirectory(tempDir), a, a, minPrefixChars);     //LUCENENET TODO: add extra false param at version 4.11.0
                        lookupThread = new LookupThread(this, suggester);
                        lookupThread.Start();

                        visibleUpto = inputs.size();
                        foreach (Update update in pendingUpdates)
                        {
                            Input oldInput = inputs[update.index];
                            Input newInput = new Input(oldInput.term, update.weight, oldInput.payload);
                            inputs[update.index] = newInput;
                        }
                        pendingUpdates.Clear();
                    }

                    if (visibleUpto > 0)
                    {
                        string query = RandomText();
                        bool lastPrefix = Random.nextInt(5) != 1;
                        if (lastPrefix == false)
                        {
                            query += " ";
                        }

                        string[] queryTerms = Regex.Split(query, "\\s", RegexOptions.Compiled).TrimEnd();
                        bool allTermsRequired = Random.nextInt(10) == 7;
                        bool doHilite = Random.nextBoolean();

                        if (Verbose)
                        {
                            Console.WriteLine("TEST: lookup \"" + query + "\" allTermsRequired=" + allTermsRequired + " doHilite=" + doHilite);
                        }

                        // Stupid slow but hopefully correct matching:
                        IList<Input> expected = new JCG.List<Input>();
                        for (int i = 0; i < visibleUpto; i++)
                        {
                            Input input = inputs[i];
                            string[] inputTerms = Regex.Split(input.term.Utf8ToString(), "\\s", RegexOptions.Compiled).TrimEnd();
                            bool match = false;
                            for (int j = 0; j < queryTerms.Length; j++)
                            {
                                if (j < queryTerms.Length - 1 || lastPrefix == false)
                                {
                                    // Exact match
                                    for (int k = 0; k < inputTerms.Length; k++)
                                    {
                                        if (inputTerms[k].Equals(queryTerms[j], StringComparison.Ordinal))
                                        {
                                            match = true;
                                            break;
                                        }
                                    }
                                }
                                else
                                {
                                    // Prefix match
                                    for (int k = 0; k < inputTerms.Length; k++)
                                    {
                                        if (inputTerms[k].StartsWith(queryTerms[j], StringComparison.Ordinal))
                                        {
                                            match = true;
                                            break;
                                        }
                                    }
                                }
                                if (match)
                                {
                                    if (allTermsRequired == false)
                                    {
                                        // At least one query term does match:
                                        break;
                                    }
                                    match = false;
                                }
                                else if (allTermsRequired)
                                {
                                    // At least one query term does not match:
                                    break;
                                }
                            }

                            if (match)
                            {
                                if (doHilite)
                                {
                                    expected.Add(new Input(Hilite(lastPrefix, inputTerms, queryTerms), input.v, input.term));
                                }
                                else
                                {
                                    expected.Add(input);
                                }
                            }
                        }

                        expected.Sort(new TestRandomNRTComparer());

                        if (expected.Count > 0)
                        {

                            int topN = TestUtil.NextInt32(Random, 1, expected.size());

                            IList<Lookup.LookupResult> actual = suggester.DoLookup(TestUtil.StringToCharSequence(query, Random).ToString(), topN, allTermsRequired, doHilite);

                            int expectedCount = Math.Min(topN, expected.size());

                            if (Verbose)
                            {
                                Console.WriteLine("  expected:");
                                for (int i = 0; i < expectedCount; i++)
                                {
                                    Input x = expected[i];
                                    Console.WriteLine("    " + x.term.Utf8ToString() + "/" + x.v);
                                }
                                Console.WriteLine("  actual:");
                                foreach (Lookup.LookupResult result in actual)
                                {
                                    Console.WriteLine("    " + result);
                                }
                            }

                            assertEquals(expectedCount, actual.size());
                            for (int i = 0; i < expectedCount; i++)
                            {
                                assertEquals(expected[i].term.Utf8ToString(), actual[i].Key.toString());
                                assertEquals(expected[i].v, actual[i].Value);
                                assertEquals(expected[i].payload, actual[i].Payload);
                            }
                        }
                        else
                        {
                            if (Verbose)
                            {
                                Console.WriteLine("  no expected matches");
                            }
                        }
                    }
                }

                lookupThread.Finish();
            }
            finally
            {
                suggester.Dispose();
            }
        }

        private static string Hilite(bool lastPrefix, string[] inputTerms, string[] queryTerms)
        {
            // Stupid slow but hopefully correct highlighter:
            //System.out.println("hilite: lastPrefix=" + lastPrefix + " inputTerms=" + Arrays.toString(inputTerms) + " queryTerms=" + Arrays.toString(queryTerms));
            StringBuilder b = new StringBuilder();
            for (int i = 0; i < inputTerms.Length; i++)
            {
                if (i > 0)
                {
                    b.Append(' ');
                }
                string inputTerm = inputTerms[i];
                //System.out.println("  inputTerm=" + inputTerm);
                bool matched = false;
                for (int j = 0; j < queryTerms.Length; j++)
                {
                    string queryTerm = queryTerms[j];
                    //System.out.println("    queryTerm=" + queryTerm);
                    if (j < queryTerms.Length - 1 || lastPrefix == false)
                    {
                        //System.out.println("      check exact");
                        if (inputTerm.Equals(queryTerm, StringComparison.Ordinal))
                        {
                            b.Append("<b>");
                            b.Append(inputTerm);
                            b.Append("</b>");
                            matched = true;
                            break;
                        }
                    }
                    else if (inputTerm.StartsWith(queryTerm, StringComparison.Ordinal))
                    {
                        b.Append("<b>");
                        b.Append(queryTerm);
                        b.Append("</b>");
                        b.Append(inputTerm.Substring(queryTerm.Length, inputTerm.Length - queryTerm.Length));
                        matched = true;
                        break;
                    }
                }

                if (matched == false)
                {
                    b.Append(inputTerm);
                }
            }

            return b.ToString();
        }

        [Test]
        public void TestBasicNRT()
        {
            Input[] keys = new Input[] {
                new Input("lend me your ear", 8, new BytesRef("foobar")),
            };

            Analyzer a = new MockAnalyzer(Random, MockTokenizer.WHITESPACE, false);
            using AnalyzingInfixSuggester suggester = new AnalyzingInfixSuggester(TEST_VERSION_CURRENT, NewDirectory(), a, a, 3);     //LUCENENET TODO: add extra false param at version 4.11.0
            suggester.Build(new InputArrayEnumerator(keys));

            IList<Lookup.LookupResult> results = suggester.DoLookup(TestUtil.StringToCharSequence("ear", Random).ToString(), 10, true, true);
            assertEquals(1, results.size());
            assertEquals("lend me your <b>ear</b>", results[0].Key);
            assertEquals(8, results[0].Value);
            assertEquals(new BytesRef("foobar"), results[0].Payload);

            // Add a new suggestion:
            suggester.Add(new BytesRef("a penny saved is a penny earned"), null, 10, new BytesRef("foobaz"));

            // Must refresh to see any newly added suggestions:
            suggester.Refresh();

            results = suggester.DoLookup(TestUtil.StringToCharSequence("ear", Random).ToString(), 10, true, true);
            assertEquals(2, results.size());
            assertEquals("a penny saved is a penny <b>ear</b>ned", results[0].Key);
            assertEquals(10, results[0].Value);
            assertEquals(new BytesRef("foobaz"), results[0].Payload);

            assertEquals("lend me your <b>ear</b>", results[1].Key);
            assertEquals(8, results[1].Value);
            assertEquals(new BytesRef("foobar"), results[1].Payload);

            results = suggester.DoLookup(TestUtil.StringToCharSequence("ear ", Random).ToString(), 10, true, true);
            assertEquals(1, results.size());
            assertEquals("lend me your <b>ear</b>", results[0].Key);
            assertEquals(8, results[0].Value);
            assertEquals(new BytesRef("foobar"), results[0].Payload);

            results = suggester.DoLookup(TestUtil.StringToCharSequence("pen", Random).ToString(), 10, true, true);
            assertEquals(1, results.size());
            assertEquals("a <b>pen</b>ny saved is a <b>pen</b>ny earned", results[0].Key);
            assertEquals(10, results[0].Value);
            assertEquals(new BytesRef("foobaz"), results[0].Payload);

            results = suggester.DoLookup(TestUtil.StringToCharSequence("p", Random).ToString(), 10, true, true);
            assertEquals(1, results.size());
            assertEquals("a <b>p</b>enny saved is a <b>p</b>enny earned", results[0].Key);
            assertEquals(10, results[0].Value);
            assertEquals(new BytesRef("foobaz"), results[0].Payload);

            // Change the weight:
            suggester.Update(new BytesRef("lend me your ear"), null, 12, new BytesRef("foobox"));

            // Must refresh to see any newly added suggestions:
            suggester.Refresh();

            results = suggester.DoLookup(TestUtil.StringToCharSequence("ear", Random).ToString(), 10, true, true);
            assertEquals(2, results.size());
            assertEquals("lend me your <b>ear</b>", results[0].Key);
            assertEquals(12, results[0].Value);
            assertEquals(new BytesRef("foobox"), results[0].Payload);
            assertEquals("a penny saved is a penny <b>ear</b>ned", results[1].Key);
            assertEquals(10, results[1].Value);
            assertEquals(new BytesRef("foobaz"), results[1].Payload);
        }

        // LUCENENET specific - LUCENE-5889, a 4.11.0 feature.
        [Test]
        public void TestNRTWithParallelAdds()
        {
            String[] keys = new String[] { "python", "java", "c", "scala", "ruby", "clojure", "erlang", "go", "swift", "lisp" };
            Analyzer a = new MockAnalyzer(Random, MockTokenizer.WHITESPACE, false);
            DirectoryInfo tempDir = CreateTempDir("AIS_NRT_PERSIST_TEST");
            AnalyzingInfixSuggester suggester = new AnalyzingInfixSuggester(TEST_VERSION_CURRENT, NewFSDirectory(tempDir), a, a, 3, false);
            Thread[] multiAddThreads = new Thread[10];

            try
            {
                suggester.Refresh();
                fail("Cannot call refresh on an suggester when no docs are added to the index");
            }
            catch (Exception e) when (e.IsIllegalStateException())
            {
                //Expected
            }

            for (int i = 0; i < 10; i++)
            {
                string key = keys[i];
                multiAddThreads[i] = new Thread(() => IndexDocument(suggester, key));   // LUCENENET specific: use of closure rather than object.
            }

            for (int i = 0; i < 10; i++)
            {
                multiAddThreads[i].Start();
            }

            //Make sure all threads have completed indexing
            for (int i = 0; i < 10; i++)
            {
                multiAddThreads[i].Join();
            }

            suggester.Refresh();
            IList<LookupResult> results = suggester.DoLookup(TestUtil.StringToCharSequence("python", Random).ToString(), 10, true, false);
            assertEquals(1, results.size());
            assertEquals("python", results[0].Key);

            //Test if the index is getting persisted correctly and can be reopened.
            suggester.Commit();
            suggester.Dispose();

            suggester = new AnalyzingInfixSuggester(TEST_VERSION_CURRENT, NewFSDirectory(tempDir), a, a, 3, false);
            results = suggester.DoLookup(TestUtil.StringToCharSequence("python", Random).ToString(), 10, true, false);
            assertEquals(1, results.size());
            assertEquals("python", results[0].Key);

            suggester.Dispose();
        }

        // LUCENENET specific - LUCENE-5889, a 4.11.0 feature. Use of method not class due to Thread parameter needs in .Net.
        public void IndexDocument(AnalyzingInfixSuggester suggester, String key)
        {
            try
            {
                suggester.Add(new BytesRef(key), null, 10, null);
            }
            catch (Exception e) when (e.IsIOException())
            {
                fail("Could not build suggest dictionary correctly");
            }
        }


        private ISet<BytesRef> AsSet(params string[] values)
        {
            ISet<BytesRef> result = new JCG.HashSet<BytesRef>();
            foreach (string value in values)
            {
                result.add(new BytesRef(value));
            }

            return result;
        }

        // LUCENE-5528
        [Test]
        public void TestBasicContext()
        {
            Input[] keys = new Input[] {
                new Input("lend me your ear", 8, new BytesRef("foobar"), AsSet("foo", "bar")),
                new Input("a penny saved is a penny earned", 10, new BytesRef("foobaz"), AsSet("foo", "baz"))
            };

            DirectoryInfo tempDir = CreateTempDir("analyzingInfixContext");

            for (int iter = 0; iter < 2; iter++)
            {
                AnalyzingInfixSuggester suggester = null;
                try
                {
                    Analyzer a = new MockAnalyzer(Random, MockTokenizer.WHITESPACE, false);
                    if (iter == 0)
                    {
                        suggester = new AnalyzingInfixSuggester(TEST_VERSION_CURRENT, NewFSDirectory(tempDir), a, a, 3);     //LUCENENET TODO: add extra false param at version 4.11.0
                        suggester.Build(new InputArrayEnumerator(keys));
                    }
                    else
                    {
                        // Test again, after close/reopen:
                        suggester = new AnalyzingInfixSuggester(TEST_VERSION_CURRENT, NewFSDirectory(tempDir), a, a, 3);     //LUCENENET TODO: add extra false param at version 4.11.0
                    }

                    // No context provided, all results returned
                    IList<Lookup.LookupResult> results = suggester.DoLookup(TestUtil.StringToCharSequence("ear", Random).ToString(), 10, true, true);
                    assertEquals(2, results.size());
                    Lookup.LookupResult result = results[0];
                    assertEquals("a penny saved is a penny <b>ear</b>ned", result.Key);
                    assertEquals(10, result.Value);
                    assertEquals(new BytesRef("foobaz"), result.Payload);
                    assertNotNull(result.Contexts);
                    assertEquals(2, result.Contexts.Count());
                    assertTrue(result.Contexts.Contains(new BytesRef("foo")));
                    assertTrue(result.Contexts.Contains(new BytesRef("baz")));

                    result = results[1];
                    assertEquals("lend me your <b>ear</b>", result.Key);
                    assertEquals(8, result.Value);
                    assertEquals(new BytesRef("foobar"), result.Payload);
                    assertNotNull(result.Contexts);
                    assertEquals(2, result.Contexts.Count());
                    assertTrue(result.Contexts.Contains(new BytesRef("foo")));
                    assertTrue(result.Contexts.Contains(new BytesRef("bar")));

                    // Both suggestions have "foo" context:
                    results = suggester.DoLookup(TestUtil.StringToCharSequence("ear", Random).ToString(), AsSet("foo"), 10, true, true);
                    assertEquals(2, results.size());

                    result = results[0];
                    assertEquals("a penny saved is a penny <b>ear</b>ned", result.Key);
                    assertEquals(10, result.Value);
                    assertEquals(new BytesRef("foobaz"), result.Payload);
                    assertNotNull(result.Contexts);
                    assertEquals(2, result.Contexts.Count());
                    assertTrue(result.Contexts.Contains(new BytesRef("foo")));
                    assertTrue(result.Contexts.Contains(new BytesRef("baz")));

                    result = results[1];
                    assertEquals("lend me your <b>ear</b>", result.Key);
                    assertEquals(8, result.Value);
                    assertEquals(new BytesRef("foobar"), result.Payload);
                    assertNotNull(result.Contexts);
                    assertEquals(2, result.Contexts.Count());
                    assertTrue(result.Contexts.Contains(new BytesRef("foo")));
                    assertTrue(result.Contexts.Contains(new BytesRef("bar")));

                    // Only one has "bar" context:
                    results = suggester.DoLookup(TestUtil.StringToCharSequence("ear", Random).ToString(), AsSet("bar"), 10, true, true);
                    assertEquals(1, results.size());

                    result = results[0];
                    assertEquals("lend me your <b>ear</b>", result.Key);
                    assertEquals(8, result.Value);
                    assertEquals(new BytesRef("foobar"), result.Payload);
                    assertNotNull(result.Contexts);
                    assertEquals(2, result.Contexts.Count());
                    assertTrue(result.Contexts.Contains(new BytesRef("foo")));
                    assertTrue(result.Contexts.Contains(new BytesRef("bar")));

                    // Only one has "baz" context:
                    results = suggester.DoLookup(TestUtil.StringToCharSequence("ear", Random).ToString(), AsSet("baz"), 10, true, true);
                    assertEquals(1, results.size());

                    result = results[0];
                    assertEquals("a penny saved is a penny <b>ear</b>ned", result.Key);
                    assertEquals(10, result.Value);
                    assertEquals(new BytesRef("foobaz"), result.Payload);
                    assertNotNull(result.Contexts);
                    assertEquals(2, result.Contexts.Count());
                    assertTrue(result.Contexts.Contains(new BytesRef("foo")));
                    assertTrue(result.Contexts.Contains(new BytesRef("baz")));

                    // Both have foo or bar:
                    results = suggester.DoLookup(TestUtil.StringToCharSequence("ear", Random).ToString(), AsSet("foo", "bar"), 10, true, true);
                    assertEquals(2, results.size());

                    result = results[0];
                    assertEquals("a penny saved is a penny <b>ear</b>ned", result.Key);
                    assertEquals(10, result.Value);
                    assertEquals(new BytesRef("foobaz"), result.Payload);
                    assertNotNull(result.Contexts);
                    assertEquals(2, result.Contexts.Count());
                    assertTrue(result.Contexts.Contains(new BytesRef("foo")));
                    assertTrue(result.Contexts.Contains(new BytesRef("baz")));

                    result = results[1];
                    assertEquals("lend me your <b>ear</b>", result.Key);
                    assertEquals(8, result.Value);
                    assertEquals(new BytesRef("foobar"), result.Payload);
                    assertNotNull(result.Contexts);
                    assertEquals(2, result.Contexts.Count());
                    assertTrue(result.Contexts.Contains(new BytesRef("foo")));
                    assertTrue(result.Contexts.Contains(new BytesRef("bar")));
                }
                finally
                {
                    if (suggester != null)
                        suggester.Dispose();
                }
            }
        }
    }
}
