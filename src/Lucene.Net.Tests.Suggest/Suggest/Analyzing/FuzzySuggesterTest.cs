using Lucene.Net.Analysis;
using Lucene.Net.Analysis.TokenAttributes;
using Lucene.Net.Support;
using Lucene.Net.Util;
using Lucene.Net.Util.Automaton;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

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

    public class FuzzySuggesterTest : LuceneTestCase
    {
        [Test]
        public void TestRandomEdits()
        {
            List<Input> keys = new List<Input>();
            int numTerms = AtLeast(100);
            for (int i = 0; i < numTerms; i++)
            {
                keys.Add(new Input("boo" + TestUtil.RandomSimpleString(Random()), 1 + Random().Next(100)));
            }
            keys.Add(new Input("foo bar boo far", 12));
            MockAnalyzer analyzer = new MockAnalyzer(Random(), MockTokenizer.KEYWORD, false);
            FuzzySuggester suggester = new FuzzySuggester(analyzer, analyzer, SuggesterOptions.EXACT_FIRST | SuggesterOptions.PRESERVE_SEP, 256, -1, true, FuzzySuggester.DEFAULT_MAX_EDITS, FuzzySuggester.DEFAULT_TRANSPOSITIONS,
                                                          0, FuzzySuggester.DEFAULT_MIN_FUZZY_LENGTH, FuzzySuggester.DEFAULT_UNICODE_AWARE);
            suggester.Build(new InputArrayIterator(keys));
            int numIters = AtLeast(10);
            for (int i = 0; i < numIters; i++)
            {
                string addRandomEdit = AddRandomEdit("foo bar boo", FuzzySuggester.DEFAULT_NON_FUZZY_PREFIX);
                IList<Lookup.LookupResult> results = suggester.DoLookup(TestUtil.StringToCharSequence(addRandomEdit, Random()).ToString(), false, 2);
                assertEquals(addRandomEdit, 1, results.size());
                assertEquals("foo bar boo far", results.ElementAt(0).key.toString());
                assertEquals(12, results.ElementAt(0).value, 0.01F);
            }
        }

        [Test]
        public void TestNonLatinRandomEdits()
        {
            List<Input> keys = new List<Input>();
            int numTerms = AtLeast(100);
            for (int i = 0; i < numTerms; i++)
            {
                keys.Add(new Input("буу" + TestUtil.RandomSimpleString(Random()), 1 + Random().nextInt(100)));
            }
            keys.Add(new Input("фуу бар буу фар", 12));
            MockAnalyzer analyzer = new MockAnalyzer(Random(), MockTokenizer.KEYWORD, false);
            FuzzySuggester suggester = new FuzzySuggester(analyzer, analyzer, SuggesterOptions.EXACT_FIRST | SuggesterOptions.PRESERVE_SEP, 256, -1, true, FuzzySuggester.DEFAULT_MAX_EDITS, FuzzySuggester.DEFAULT_TRANSPOSITIONS,
                0, FuzzySuggester.DEFAULT_MIN_FUZZY_LENGTH, true);
            suggester.Build(new InputArrayIterator(keys));
            int numIters = AtLeast(10);
            for (int i = 0; i < numIters; i++)
            {
                string addRandomEdit = AddRandomEdit("фуу бар буу", 0);
                IList<Lookup.LookupResult> results = suggester.DoLookup(TestUtil.StringToCharSequence(addRandomEdit, Random()).ToString(), false, 2);
                assertEquals(addRandomEdit, 1, results.size());
                assertEquals("фуу бар буу фар", results.ElementAt(0).key.toString());
                assertEquals(12, results.ElementAt(0).value, 0.01F);
            }
        }

        /** this is basically the WFST test ported to KeywordAnalyzer. so it acts the same */
        [Test]
        public void TestKeyword()
        {
            Input[] keys = new Input[] {
                new Input("foo", 50),
                new Input("bar", 10),
                new Input("barbar", 12),
                new Input("barbara", 6)
            };

            FuzzySuggester suggester = new FuzzySuggester(new MockAnalyzer(Random(), MockTokenizer.KEYWORD, false));
            suggester.Build(new InputArrayIterator(keys));

            IList<Lookup.LookupResult> results = suggester.DoLookup(TestUtil.StringToCharSequence("bariar", Random()).ToString(), false, 2);
            assertEquals(2, results.size());
            assertEquals("barbar", results.ElementAt(0).key.toString());
            assertEquals(12, results.ElementAt(0).value, 0.01F);

            results = suggester.DoLookup(TestUtil.StringToCharSequence("barbr", Random()).ToString(), false, 2);
            assertEquals(2, results.size());
            assertEquals("barbar", results.ElementAt(0).key.toString());
            assertEquals(12, results.ElementAt(0).value, 0.01F);

            results = suggester.DoLookup(TestUtil.StringToCharSequence("barbara", Random()).ToString(), false, 2);
            assertEquals(2, results.size());
            assertEquals("barbara", results.ElementAt(0).key.toString());
            assertEquals(6, results.ElementAt(0).value, 0.01F);

            results = suggester.DoLookup(TestUtil.StringToCharSequence("barbar", Random()).ToString(), false, 2);
            assertEquals(2, results.size());
            assertEquals("barbar", results.ElementAt(0).key.toString());
            assertEquals(12, results.ElementAt(0).value, 0.01F);
            assertEquals("barbara", results.ElementAt(1).key.toString());
            assertEquals(6, results.ElementAt(1).value, 0.01F);

            results = suggester.DoLookup(TestUtil.StringToCharSequence("barbaa", Random()).ToString(), false, 2);
            assertEquals(2, results.size());
            assertEquals("barbar", results.ElementAt(0).key.toString());
            assertEquals(12, results.ElementAt(0).value, 0.01F);
            assertEquals("barbara", results.ElementAt(1).key.toString());
            assertEquals(6, results.ElementAt(1).value, 0.01F);

            // top N of 2, but only foo is available
            results = suggester.DoLookup(TestUtil.StringToCharSequence("f", Random()).ToString(), false, 2);
            assertEquals(1, results.size());
            assertEquals("foo", results.ElementAt(0).key.toString());
            assertEquals(50, results.ElementAt(0).value, 0.01F);

            // top N of 1 for 'bar': we return this even though
            // barbar is higher because exactFirst is enabled:
            results = suggester.DoLookup(TestUtil.StringToCharSequence("bar", Random()).ToString(), false, 1);
            assertEquals(1, results.size());
            assertEquals("bar", results.ElementAt(0).key.toString());
            assertEquals(10, results.ElementAt(0).value, 0.01F);

            // top N Of 2 for 'b'
            results = suggester.DoLookup(TestUtil.StringToCharSequence("b", Random()).ToString(), false, 2);
            assertEquals(2, results.size());
            assertEquals("barbar", results.ElementAt(0).key.toString());
            assertEquals(12, results.ElementAt(0).value, 0.01F);
            assertEquals("bar", results.ElementAt(1).key.toString());
            assertEquals(10, results.ElementAt(1).value, 0.01F);

            // top N of 3 for 'ba'
            results = suggester.DoLookup(TestUtil.StringToCharSequence("ba", Random()).ToString(), false, 3);
            assertEquals(3, results.size());
            assertEquals("barbar", results.ElementAt(0).key.toString());
            assertEquals(12, results.ElementAt(0).value, 0.01F);
            assertEquals("bar", results.ElementAt(1).key.toString());
            assertEquals(10, results.ElementAt(1).value, 0.01F);
            assertEquals("barbara", results.ElementAt(2).key.toString());
            assertEquals(6, results.ElementAt(2).value, 0.01F);
        }

        /**
         * basic "standardanalyzer" test with stopword removal
         */
        [Test]
        public void TestStandard()
        {
            Input[] keys = new Input[] {
                new Input("the ghost of christmas past", 50),
            };

            Analyzer standard = new MockAnalyzer(Random(), MockTokenizer.WHITESPACE, true, MockTokenFilter.ENGLISH_STOPSET);
            FuzzySuggester suggester = new FuzzySuggester(standard, standard, SuggesterOptions.EXACT_FIRST | SuggesterOptions.PRESERVE_SEP, 256, -1, false, FuzzySuggester.DEFAULT_MAX_EDITS, FuzzySuggester.DEFAULT_TRANSPOSITIONS,
                FuzzySuggester.DEFAULT_NON_FUZZY_PREFIX, FuzzySuggester.DEFAULT_MIN_FUZZY_LENGTH, FuzzySuggester.DEFAULT_UNICODE_AWARE);
            suggester.Build(new InputArrayIterator(keys));

            IList<Lookup.LookupResult> results = suggester.DoLookup(TestUtil.StringToCharSequence("the ghost of chris", Random()).ToString(), false, 1);
            assertEquals(1, results.size());
            assertEquals("the ghost of christmas past", results.ElementAt(0).key.toString());
            assertEquals(50, results.ElementAt(0).value, 0.01F);

            // omit the 'the' since its a stopword, its suggested anyway
            results = suggester.DoLookup(TestUtil.StringToCharSequence("ghost of chris", Random()).ToString(), false, 1);
            assertEquals(1, results.size());
            assertEquals("the ghost of christmas past", results.ElementAt(0).key.toString());
            assertEquals(50, results.ElementAt(0).value, 0.01F);

            // omit the 'the' and 'of' since they are stopwords, its suggested anyway
            results = suggester.DoLookup(TestUtil.StringToCharSequence("ghost chris", Random()).ToString(), false, 1);
            assertEquals(1, results.size());
            assertEquals("the ghost of christmas past", results.ElementAt(0).key.toString());
            assertEquals(50, results.ElementAt(0).value, 0.01F);
        }

        [Test]
        public void TestNoSeps()
        {
            Input[] keys = new Input[] {
                new Input("ab cd", 0),
                new Input("abcd", 1),
            };

            SuggesterOptions options = 0;

            Analyzer a = new MockAnalyzer(Random());
            FuzzySuggester suggester = new FuzzySuggester(a, a, options, 256, -1, true, 1, true, 1, 3, false);
            suggester.Build(new InputArrayIterator(keys));
            // TODO: would be nice if "ab " would allow the test to
            // pass, and more generally if the analyzer can know
            // that the user's current query has ended at a word, 
            // but, analyzers don't produce SEP tokens!
            IList<Lookup.LookupResult> r = suggester.DoLookup(TestUtil.StringToCharSequence("ab c", Random()).ToString(), false, 2);
            assertEquals(2, r.size());

            // With no PRESERVE_SEPS specified, "ab c" should also
            // complete to "abcd", which has higher weight so should
            // appear first:
            assertEquals("abcd", r.ElementAt(0).key.toString());
        }
        internal class TestGraphDupsTokenStreamComponents : Analyzer.TokenStreamComponents
        {
            private readonly FuzzySuggesterTest outerInstance;
            internal int tokenStreamCounter = 0;
            internal readonly TokenStream[] tokenStreams = new TokenStream[] {
            new CannedTokenStream(new Token[] {
                NewToken("wifi",1,1),
                NewToken("hotspot",0,2),
                NewToken("network",1,1),
                NewToken("is",1,1),
                NewToken("slow",1,1)
              }),
            new CannedTokenStream(new Token[] {
                NewToken("wi",1,1),
                NewToken("hotspot",0,3),
                NewToken("fi",1,1),
                NewToken("network",1,1),
                NewToken("is",1,1),
                NewToken("fast",1,1)

              }),
            new CannedTokenStream(new Token[] {
                NewToken("wifi",1,1),
                NewToken("hotspot",0,2),
                NewToken("network",1,1)
              }),
          };

            public TestGraphDupsTokenStreamComponents(FuzzySuggesterTest outerInstance, Tokenizer tokenizer)
                : base(tokenizer)
            {
                this.outerInstance = outerInstance;
            }

            public override TokenStream TokenStream
            {
                get
                {
                    TokenStream result = tokenStreams[tokenStreamCounter];
                    tokenStreamCounter++;
                    return result;
                }
            }

            protected override void SetReader(TextReader reader)
            {
            }
        }

        internal class TestGraphDupsAnalyzer : Analyzer
        {
            private readonly FuzzySuggesterTest outerInstance;
            public TestGraphDupsAnalyzer(FuzzySuggesterTest outerInstance)
            {
                this.outerInstance = outerInstance;
            }

            protected override TokenStreamComponents CreateComponents(string fieldName, TextReader reader)
            {
                Tokenizer tokenizer = new MockTokenizer(reader, MockTokenizer.SIMPLE, true);
                return new TestGraphDupsTokenStreamComponents(outerInstance, tokenizer);
            }
        }

        [Test]
        public void TestGraphDups()
        {
            Analyzer analyzer = new TestGraphDupsAnalyzer(this);

            Input[] keys = new Input[] {
                new Input("wifi network is slow", 50),
                new Input("wi fi network is fast", 10),
            };
            FuzzySuggester suggester = new FuzzySuggester(analyzer);
            suggester.Build(new InputArrayIterator(keys));

            IList<Lookup.LookupResult> results = suggester.DoLookup("wifi network", false, 10);
            if (VERBOSE)
            {
                Console.WriteLine("Results: " + results);
            }
            assertEquals(2, results.size());
            assertEquals("wifi network is slow", results.ElementAt(0).key);
            assertEquals(50, results.ElementAt(0).value);
            assertEquals("wi fi network is fast", results.ElementAt(1).key);
            assertEquals(10, results.ElementAt(1).value);
        }

        [Test]
        public void TestEmpty()
        {
            FuzzySuggester suggester = new FuzzySuggester(new MockAnalyzer(Random(), MockTokenizer.KEYWORD, false));
            suggester.Build(new InputArrayIterator(new Input[0]));

            IList<Lookup.LookupResult> result = suggester.DoLookup("a", false, 20);
            assertTrue(!result.Any());
        }

        internal class TestInputPathRequiredTokenStreamComponents : Analyzer.TokenStreamComponents
        {
            private readonly FuzzySuggesterTest outerInstance;
            internal int tokenStreamCounter = 0;
            internal readonly TokenStream[] tokenStreams = new TokenStream[] {
            new CannedTokenStream(new Token[] {
                FuzzySuggesterTest.NewToken("ab",1,1),
                FuzzySuggesterTest.NewToken("ba",0,1),
                FuzzySuggesterTest.NewToken("xc",1,1)
              }),
            new CannedTokenStream(new Token[] {
                FuzzySuggesterTest.NewToken("ba",1,1),
                FuzzySuggesterTest.NewToken("xd",1,1)
              }),
            new CannedTokenStream(new Token[] {
                FuzzySuggesterTest.NewToken("ab",1,1),
                FuzzySuggesterTest.NewToken("ba",0,1),
                FuzzySuggesterTest.NewToken("x",1,1)
              })
            };

            public TestInputPathRequiredTokenStreamComponents(FuzzySuggesterTest outerInstance, Tokenizer tokenizer)
                : base(tokenizer)
            {
                this.outerInstance = outerInstance;
            }

            public override TokenStream TokenStream
            {
                get
                {
                    TokenStream result = tokenStreams[tokenStreamCounter];
                    tokenStreamCounter++;
                    return result;
                }
            }

            protected override void SetReader(TextReader reader)
            {
            }
        }

        internal class TestInputPathRequiredAnalyzer : Analyzer
        {
            private readonly FuzzySuggesterTest outerInstance;

            public TestInputPathRequiredAnalyzer(FuzzySuggesterTest outerInstance)
            {
                this.outerInstance = outerInstance;
            }

            protected override TokenStreamComponents CreateComponents(string fieldName, TextReader reader)
            {
                Tokenizer tokenizer = new MockTokenizer(reader, MockTokenizer.SIMPLE, true);
                return new TestInputPathRequiredTokenStreamComponents(outerInstance, tokenizer);
            }
        }

        [Test]
        public void TestInputPathRequired()
        {

            //  SynonymMap.Builder b = new SynonymMap.Builder(false);
            //  b.add(new CharsRef("ab"), new CharsRef("ba"), true);
            //  final SynonymMap map = b.build();

            //  The Analyzer below mimics the functionality of the SynonymAnalyzer
            //  using the above map, so that the suggest module does not need a dependency on the 
            //  synonym module 

            Analyzer analyzer = new TestInputPathRequiredAnalyzer(this);

            Input[] keys = new Input[] {
                new Input("ab xc", 50),
                new Input("ba xd", 50),
            };
            FuzzySuggester suggester = new FuzzySuggester(analyzer);
            suggester.Build(new InputArrayIterator(keys));
            IList<Lookup.LookupResult> results = suggester.DoLookup("ab x", false, 1);
            assertTrue(results.size() == 1);
        }

        private static Token NewToken(string term, int posInc, int posLength)
        {
            Token t = new Token(term, 0, 0);
            t.PositionIncrement = (posInc);
            t.PositionLength = (posLength);
            return t;
        }

        /*
        private void printTokens(final Analyzer analyzer, String input) throws IOException {
          Console.WriteLine("Tokens for " + input);
          TokenStream ts = analyzer.tokenStream("", new StringReader(input));
          ts.reset();
          final TermToBytesRefAttribute termBytesAtt = ts.addAttribute(TermToBytesRefAttribute.class);
          final PositionIncrementAttribute posIncAtt = ts.addAttribute(PositionIncrementAttribute.class);
          final PositionLengthAttribute posLengthAtt = ts.addAttribute(PositionLengthAttribute.class);

          while(ts.incrementToken()) {
            termBytesAtt.fillBytesRef();
            Console.WriteLine(String.format("%s,%s,%s", termBytesAtt.getBytesRef().utf8ToString(), posIncAtt.getPositionIncrement(), posLengthAtt.getPositionLength()));      
          }
          ts.end();
          ts.close();
        } 
        */

        internal class UsualTokenStreamComponents : Analyzer.TokenStreamComponents
        {
            private readonly FuzzySuggesterTest outerInstance;
            internal int count;

            public UsualTokenStreamComponents(FuzzySuggesterTest outerInstance, Tokenizer tokenizer)
                : base(tokenizer)
            {
                this.outerInstance = outerInstance;
            }

            public override TokenStream TokenStream
            {
                get
                {
                    // 4th time we are called, return tokens a b,
                    // else just a:
                    if (count++ != 3)
                    {
                        return new CannedTokenStream(new Token[] {
                            NewToken("a", 1, 1),
                        });
                    }
                    else
                    {
                        // After that "a b":
                        return new CannedTokenStream(new Token[] {
                            NewToken("a", 1, 1),
                            NewToken("b", 1, 1),
                        });
                    }
                }
            }

            protected override void SetReader(TextReader reader)
            {
            }
        }
        internal class UsualAnalyzer : Analyzer
        {
            private readonly FuzzySuggesterTest outerInstance;
            public UsualAnalyzer(FuzzySuggesterTest outerInstance)
            {
                this.outerInstance = outerInstance;
            }

            protected override TokenStreamComponents CreateComponents(string fieldName, TextReader reader)
            {
                Tokenizer tokenizer = new MockTokenizer(reader, MockTokenizer.SIMPLE, true);
                return new UsualTokenStreamComponents(outerInstance, tokenizer);
            }
        }

        private Analyzer GetUnusualAnalyzer()
        {
            return new UsualAnalyzer(this);
        }

        [Test]
        public void TestExactFirst()
        {

            Analyzer a = GetUnusualAnalyzer();
            FuzzySuggester suggester = new FuzzySuggester(a, a, SuggesterOptions.EXACT_FIRST | SuggesterOptions.PRESERVE_SEP, 256, -1, true, 1, true, 1, 3, false);
            suggester.Build(new InputArrayIterator(new Input[] {
                new Input("x y", 1),
                new Input("x y z", 3),
                new Input("x", 2),
                new Input("z z z", 20),
            }));

            //Console.WriteLine("ALL: " + suggester.DoLookup("x y", false, 6));

            for (int topN = 1; topN < 6; topN++)
            {
                IList<Lookup.LookupResult> results = suggester.DoLookup("x y", false, topN);
                //Console.WriteLine("topN=" + topN + " " + results);

                assertEquals(Math.Min(topN, 4), results.size());

                assertEquals("x y", results.ElementAt(0).key);
                assertEquals(1, results.ElementAt(0).value);

                if (topN > 1)
                {
                    assertEquals("z z z", results.ElementAt(1).key);
                    assertEquals(20, results.ElementAt(1).value);

                    if (topN > 2)
                    {
                        assertEquals("x y z", results.ElementAt(2).key);
                        assertEquals(3, results.ElementAt(2).value);

                        if (topN > 3)
                        {
                            assertEquals("x", results.ElementAt(3).key);
                            assertEquals(2, results.ElementAt(3).value);
                        }
                    }
                }
            }
        }

        [Test]
        public void TestNonExactFirst()
        {

            Analyzer a = GetUnusualAnalyzer();
            FuzzySuggester suggester = new FuzzySuggester(a, a, SuggesterOptions.PRESERVE_SEP, 256, -1, true, 1, true, 1, 3, false);

            suggester.Build(new InputArrayIterator(new Input[] {
                new Input("x y", 1),
                new Input("x y z", 3),
                new Input("x", 2),
                new Input("z z z", 20),
            }));

            for (int topN = 1; topN < 6; topN++)
            {
                IList<Lookup.LookupResult> results = suggester.DoLookup("p", false, topN);

                assertEquals(Math.Min(topN, 4), results.size());

                assertEquals("z z z", results.ElementAt(0).key);
                assertEquals(20, results.ElementAt(0).value);

                if (topN > 1)
                {
                    assertEquals("x y z", results.ElementAt(1).key);
                    assertEquals(3, results.ElementAt(1).value);

                    if (topN > 2)
                    {
                        assertEquals("x", results.ElementAt(2).key);
                        assertEquals(2, results.ElementAt(2).value);

                        if (topN > 3)
                        {
                            assertEquals("x y", results.ElementAt(3).key);
                            assertEquals(1, results.ElementAt(3).value);
                        }
                    }
                }
            }
        }

        // Holds surface form separately:
        internal class TermFreqPayload2 : IComparable<TermFreqPayload2>
        {
            public readonly string surfaceForm;
            public readonly string analyzedForm;
            public readonly long weight;

            public TermFreqPayload2(string surfaceForm, string analyzedForm, long weight)
            {
                this.surfaceForm = surfaceForm;
                this.analyzedForm = analyzedForm;
                this.weight = weight;
            }

            public int CompareTo(TermFreqPayload2 other)
            {
                int cmp = analyzedForm.CompareTo(other.analyzedForm);
                if (cmp != 0)
                {
                    return cmp;
                }
                else if (weight > other.weight)
                {
                    return -1;
                }
                else if (weight < other.weight)
                {
                    return 1;
                }
                else
                {
                    Debug.Assert(false);
                    return 0;
                }
            }
        }

        private static bool IsStopChar(char ch, int numStopChars)
        {
            //Console.WriteLine("IS? " + ch + ": " + (ch - 'a') + ": " + ((ch - 'a') < numStopChars));
            return (ch - 'a') < numStopChars;
        }

        // Like StopFilter:
        internal sealed class TokenEater : TokenFilter
        {
            private readonly IPositionIncrementAttribute posIncrAtt;
            private readonly ICharTermAttribute termAtt;
            private readonly int numStopChars;
            private readonly bool preserveHoles;
            private bool first;

            public TokenEater(bool preserveHoles, TokenStream @in, int numStopChars)
            : base(@in)
            {

                this.preserveHoles = preserveHoles;
                this.numStopChars = numStopChars;
                this.posIncrAtt = AddAttribute<IPositionIncrementAttribute>();
                this.termAtt = AddAttribute<ICharTermAttribute>();
            }

            public override void Reset()
            {
                base.Reset();
                first = true;
            }

            public override sealed bool IncrementToken()
            {
                int skippedPositions = 0;
                while (input.IncrementToken())
                {
                    if (termAtt.Length != 1 || !IsStopChar(termAtt.Buffer()[0], numStopChars))
                    {
                        int posInc = posIncrAtt.PositionIncrement + skippedPositions;
                        if (first)
                        {
                            if (posInc == 0)
                            {
                                // first token having posinc=0 is illegal.
                                posInc = 1;
                            }
                            first = false;
                        }
                        posIncrAtt.PositionIncrement = (posInc);
                        //Console.WriteLine("RETURN term=" + termAtt + " numStopChars=" + numStopChars);
                        return true;
                    }
                    if (preserveHoles)
                    {
                        skippedPositions += posIncrAtt.PositionIncrement;
                    }
                }

                return false;
            }
        }

        internal class MockTokenEatingAnalyzer : Analyzer
        {
            private int numStopChars;
            private bool preserveHoles;

            public MockTokenEatingAnalyzer(int numStopChars, bool preserveHoles)
            {
                this.preserveHoles = preserveHoles;
                this.numStopChars = numStopChars;
            }

            protected override TokenStreamComponents CreateComponents(string fieldName, TextReader reader)
            {
                MockTokenizer tokenizer = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false, MockTokenizer.DEFAULT_MAX_TOKEN_LENGTH);
                tokenizer.EnableChecks = (true);
                TokenStream next;
                if (numStopChars != 0)
                {
                    next = new TokenEater(preserveHoles, tokenizer, numStopChars);
                }
                else
                {
                    next = tokenizer;
                }
                return new TokenStreamComponents(tokenizer, next);
            }
        }

        internal class TestRandomComparator : IComparer<Lookup.LookupResult>
        {
            public int Compare(Lookup.LookupResult left, Lookup.LookupResult right)
            {
                int cmp = ((float)right.value).CompareTo((float)left.value);
                if (cmp == 0)
                {
                    return left.CompareTo(right);
                }
                else
                {
                    return cmp;
                }
            }
        }

        [Test]
        public void TestRandom()
        {

            int numQueries = AtLeast(100);

            List<TermFreqPayload2> slowCompletor = new List<TermFreqPayload2>();
            SortedSet<string> allPrefixes = new SortedSet<string>();
            ISet<string> seen = new HashSet<string>();

            Input[] keys = new Input[numQueries];

            bool preserveSep = Random().nextBoolean();
            bool unicodeAware = Random().nextBoolean();

            int numStopChars = Random().nextInt(10);
            bool preserveHoles = Random().nextBoolean();

            if (VERBOSE)
            {
                Console.WriteLine("TEST: " + numQueries + " words; preserveSep=" + preserveSep + " ; unicodeAware=" + unicodeAware + " numStopChars=" + numStopChars + " preserveHoles=" + preserveHoles);
            }

            for (int i = 0; i < numQueries; i++)
            {
                int numTokens = TestUtil.NextInt(Random(), 1, 4);
                string key;
                string analyzedKey;
                while (true)
                {
                    key = "";
                    analyzedKey = "";
                    bool lastRemoved = false;
                    for (int token = 0; token < numTokens; token++)
                    {
                        String s;
                        while (true)
                        {
                            // TODO: would be nice to fix this slowCompletor/comparator to
                            // use full range, but we might lose some coverage too...
                            s = TestUtil.RandomSimpleString(Random());
                            if (s.Length > 0)
                            {
                                if (token > 0)
                                {
                                    key += " ";
                                }
                                if (preserveSep && analyzedKey.Length > 0 && (unicodeAware ? analyzedKey.CodePointAt(analyzedKey.CodePointCount(0, analyzedKey.Length) - 1) != ' ' : analyzedKey[analyzedKey.Length - 1] != ' '))
                                {
                                    analyzedKey += " ";
                                }
                                key += s;
                                if (s.Length == 1 && IsStopChar(s[0], numStopChars))
                                {
                                    if (preserveSep && preserveHoles)
                                    {
                                        analyzedKey += '\u0000';
                                    }
                                    lastRemoved = true;
                                }
                                else
                                {
                                    analyzedKey += s;
                                    lastRemoved = false;
                                }
                                break;
                            }
                        }
                    }

                    analyzedKey = Regex.Replace(analyzedKey, "(^| )\u0000$", "");

                    if (preserveSep && lastRemoved)
                    {
                        analyzedKey += " ";
                    }

                    // Don't add same surface form more than once:
                    if (!seen.Contains(key))
                    {
                        seen.Add(key);
                        break;
                    }
                }

                for (int j = 1; j < key.Length; j++)
                {
                    allPrefixes.Add(key.Substring(0, j - 0));
                }
                // we can probably do Integer.MAX_VALUE here, but why worry.
                int weight = Random().Next(1 << 24);
                keys[i] = new Input(key, weight);

                slowCompletor.Add(new TermFreqPayload2(key, analyzedKey, weight));
            }

            if (VERBOSE)
            {
                // Don't just sort original list, to avoid VERBOSE
                // altering the test:
                List<TermFreqPayload2> sorted = new List<TermFreqPayload2>(slowCompletor);
                // LUCENENET NOTE: Must use TimSort because comparer is not expecting ties
                CollectionUtil.TimSort(sorted); 
                foreach (TermFreqPayload2 ent in sorted)
                {
                    Console.WriteLine("  surface='" + ent.surfaceForm + " analyzed='" + ent.analyzedForm + "' weight=" + ent.weight);
                }
            }

            Analyzer a = new MockTokenEatingAnalyzer(numStopChars, preserveHoles);
            FuzzySuggester suggester = new FuzzySuggester(a, a,
                                                          preserveSep ? SuggesterOptions.PRESERVE_SEP : 0, 256, -1, true, 1, false, 1, 3, unicodeAware);
            suggester.Build(new InputArrayIterator(keys));

            foreach (string prefix in allPrefixes)
            {

                if (VERBOSE)
                {
                    Console.WriteLine("\nTEST: prefix=" + prefix);
                }

                int topN = TestUtil.NextInt(Random(), 1, 10);
                IList<Lookup.LookupResult> r = suggester.DoLookup(TestUtil.StringToCharSequence(prefix, Random()).ToString(), false, topN);

                // 2. go thru whole set to find suggestions:
                List<Lookup.LookupResult> matches = new List<Lookup.LookupResult>();

                // "Analyze" the key:
                string[] tokens = prefix.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                StringBuilder builder = new StringBuilder();
                bool lastRemoved = false;
                for (int i = 0; i < tokens.Length; i++)
                {
                    string token = tokens[i];
                    if (preserveSep && builder.Length > 0 && !builder.ToString().EndsWith(" "))
                    {
                        builder.Append(' ');
                    }

                    if (token.Length == 1 && IsStopChar(token[0], numStopChars))
                    {
                        if (preserveSep && preserveHoles)
                        {
                            builder.Append("\u0000");
                        }
                        lastRemoved = true;
                    }
                    else
                    {
                        builder.Append(token);
                        lastRemoved = false;
                    }
                }

                string analyzedKey = builder.ToString();

                // Remove trailing sep/holes (TokenStream.end() does
                // not tell us any trailing holes, yet ... there is an
                // issue open for this):
                while (true)
                {
                    string s = Regex.Replace(analyzedKey, "(^| )\u0000$", "");
                    s = Regex.Replace(s, "\\s+$", "");
                    if (s.Equals(analyzedKey))
                    {
                        break;
                    }
                    analyzedKey = s;
                }

                if (analyzedKey.Length == 0)
                {
                    // Currently suggester can't suggest from the empty
                    // string!  You get no results, not all results...
                    continue;
                }

                if (preserveSep && (prefix.EndsWith(" ") || lastRemoved))
                {
                    analyzedKey += " ";
                }

                if (VERBOSE)
                {
                    Console.WriteLine("  analyzed: " + analyzedKey);
                }
                TokenStreamToAutomaton tokenStreamToAutomaton = suggester.TokenStreamToAutomaton;

                // NOTE: not great that we ask the suggester to give
                // us the "answer key" (ie maybe we have a bug in
                // suggester.toLevA ...) ... but testRandom2() fixes
                // this:
                Automaton automaton = suggester.ConvertAutomaton(suggester.ToLevenshteinAutomata(suggester.ToLookupAutomaton(analyzedKey)));
                assertTrue(automaton.IsDeterministic); // LUCENENET TODO: Rename back to IsDeterministic
                                                     // TODO: could be faster... but its slowCompletor for a reason
                BytesRef spare = new BytesRef();
                foreach (TermFreqPayload2 e in slowCompletor)
                {
                    spare.CopyChars(e.analyzedForm);
                    ISet<IntsRef> finiteStrings = suggester.ToFiniteStrings(spare, tokenStreamToAutomaton);
                    foreach (IntsRef intsRef in finiteStrings)
                    {
                        State p = automaton.GetInitialState();
                        BytesRef @ref = Lucene.Net.Util.Fst.Util.ToBytesRef(intsRef, spare);
                        bool added = false;
                        for (int i = @ref.Offset; i < @ref.Length; i++)
                        {
                            State q = p.Step(@ref.Bytes[i] & 0xff);
                            if (q == null)
                            {
                                break;
                            }
                            else if (q.Accept)
                            {
                                matches.Add(new Lookup.LookupResult(e.surfaceForm, e.weight));
                                added = true;
                                break;
                            }
                            p = q;
                        }
                        if (!added && p.Accept)
                        {
                            matches.Add(new Lookup.LookupResult(e.surfaceForm, e.weight));
                        }
                    }
                }

                assertTrue(numStopChars > 0 || matches.size() > 0);

                if (matches.size() > 1)
                {
                    matches.Sort(new TestRandomComparator());
                }

                if (matches.size() > topN)
                {
                    matches = new List<Lookup.LookupResult>(matches.SubList(0, topN));
                }

                if (VERBOSE)
                {
                    Console.WriteLine("  expected:");
                    foreach (Lookup.LookupResult lr in matches)
                    {
                        Console.WriteLine("    key=" + lr.key + " weight=" + lr.value);
                    }

                    Console.WriteLine("  actual:");
                    foreach (Lookup.LookupResult lr in r)
                    {
                        Console.WriteLine("    key=" + lr.key + " weight=" + lr.value);
                    }
                }


                assertEquals(prefix + "  " + topN, matches.size(), r.size());
                for (int hit = 0; hit < r.size(); hit++)
                {
                    //Console.WriteLine("  check hit " + hit);
                    assertEquals(prefix + "  " + topN, matches.ElementAt(hit).key.toString(), r.ElementAt(hit).key.toString());
                    assertEquals(matches.ElementAt(hit).value, r.ElementAt(hit).value, 0f);
                }
            }
        }

        [Test]
        public void TestMaxSurfaceFormsPerAnalyzedForm()
        {
            Analyzer a = new MockAnalyzer(Random());
            FuzzySuggester suggester = new FuzzySuggester(a, a, 0, 2, -1, true, 1, true, 1, 3, false);

            IList<Input> keys = Arrays.AsList(new Input[] {
                new Input("a", 40),
                new Input("a ", 50),
                new Input(" a", 60),
            });

            keys = CollectionsHelper.Shuffle(keys);
            suggester.Build(new InputArrayIterator(keys));

            IList<Lookup.LookupResult> results = suggester.DoLookup("a", false, 5);
            assertEquals(2, results.size());
            assertEquals(" a", results.ElementAt(0).key);
            assertEquals(60, results.ElementAt(0).value);
            assertEquals("a ", results.ElementAt(1).key);
            assertEquals(50, results.ElementAt(1).value);
        }

        [Test]
        public void TestEditSeps()
        {
            Analyzer a = new MockAnalyzer(Random());
            FuzzySuggester suggester = new FuzzySuggester(a, a, SuggesterOptions.PRESERVE_SEP, 2, -1, true, 2, true, 1, 3, false);

            IList<Input> keys = Arrays.AsList(new Input[] {
                new Input("foo bar", 40),
                new Input("foo bar baz", 50),
                new Input("barbaz", 60),
                new Input("barbazfoo", 10),
            });

            keys = CollectionsHelper.Shuffle(keys);
            suggester.Build(new InputArrayIterator(keys));

            assertEquals("[foo bar baz/50, foo bar/40]", suggester.DoLookup("foobar", false, 5).toString());
            assertEquals("[foo bar baz/50]", suggester.DoLookup("foobarbaz", false, 5).toString());
            assertEquals("[barbaz/60, barbazfoo/10]", suggester.DoLookup("bar baz", false, 5).toString());
            assertEquals("[barbazfoo/10]", suggester.DoLookup("bar baz foo", false, 5).toString());
        }


        private static string AddRandomEdit(string @string, int prefixLength)
        {
            char[] input = @string.ToCharArray();
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < input.Length; i++)
            {
                if (i >= prefixLength && Random().nextBoolean() && i < input.Length - 1)
                {
                    switch (Random().nextInt(4))
                    {
                        case 3:
                            if (i < input.Length - 1)
                            {
                                // Transpose input[i] and input[1+i]:
                                builder.Append(input[i + 1]);
                                builder.Append(input[i]);
                                for (int j = i + 2; j < input.Length; j++)
                                {
                                    builder.Append(input[j]);
                                }
                                return builder.ToString();
                            }
                            // NOTE: fall through to delete:
                            goto case 2;
                        case 2:
                            // Delete input[i]
                            for (int j = i + 1; j < input.Length; j++)
                            {
                                builder.Append(input[j]);
                            }
                            return builder.ToString();
                        case 1:
                            // Insert input[i+1] twice
                            if (i + 1 < input.Length)
                            {
                                builder.Append(input[i + 1]);
                                builder.Append(input[i++]);
                                i++;
                            }
                            for (int j = i; j < input.Length; j++)
                            {
                                builder.Append(input[j]);
                            }
                            return builder.ToString();
                        case 0:
                            // Insert random byte.
                            // NOTE: can only use ascii here so that, in
                            // UTF8 byte space it's still a single
                            // insertion:
                            // bytes 0x1e and 0x1f are reserved
                            int x = Random().nextBoolean() ? Random().nextInt(30) : 32 + Random().nextInt(128 - 32);
                            builder.Append((char)x);
                            for (int j = i; j < input.Length; j++)
                            {
                                builder.Append(input[j]);
                            }
                            return builder.ToString();
                    }
                }

                builder.Append(input[i]);
            }

            return builder.ToString();
        }

        private string RandomSimpleString(int maxLen)
        {
            int len = TestUtil.NextInt(Random(), 1, maxLen);
            char[] chars = new char[len];
            for (int j = 0; j < len; j++)
            {
                chars[j] = (char)('a' + Random().nextInt(4));
            }
            return new string(chars);
        }

        internal class TestRandom2Comparator : IComparer<Input>
        {
            public int Compare(Input a, Input b)
            {
                return a.term.CompareTo(b.term);
            }
        }

        [Test]
        public void TestRandom2()
        {
            int NUM = AtLeast(200);
            List<Input> answers = new List<Input>();
            ISet<string> seen = new HashSet<string>();
            for (int i = 0; i < NUM; i++)
            {
                string s = RandomSimpleString(8);
                if (!seen.Contains(s))
                {
                    answers.Add(new Input(s, Random().nextInt(1000)));
                    seen.Add(s);
                }
            }

            answers.Sort(new TestRandom2Comparator());

            if (VERBOSE)
            {
                Console.WriteLine("\nTEST: targets");
                foreach (Input tf in answers)
                {
                    Console.WriteLine("  " + tf.term.Utf8ToString() + " freq=" + tf.v);
                }
            }

            Analyzer a = new MockAnalyzer(Random(), MockTokenizer.KEYWORD, false);
            int maxEdits = Random().nextBoolean() ? 1 : 2;
            int prefixLen = Random().nextInt(4);
            bool transpositions = Random().nextBoolean();
            // TODO: test graph analyzers
            // TODO: test exactFirst / preserveSep permutations
            FuzzySuggester suggest = new FuzzySuggester(a, a, 0, 256, -1, true, maxEdits, transpositions, prefixLen, prefixLen, false);

            if (VERBOSE)
            {
                Console.WriteLine("TEST: maxEdits=" + maxEdits + " prefixLen=" + prefixLen + " transpositions=" + transpositions + " num=" + NUM);
            }

            answers = new List<Input>(CollectionsHelper.Shuffle(answers));
            suggest.Build(new InputArrayIterator(answers.ToArray()));

            int ITERS = AtLeast(100);
            for (int iter = 0; iter < ITERS; iter++)
            {
                string frag = RandomSimpleString(6);
                if (VERBOSE)
                {
                    Console.WriteLine("\nTEST: iter frag=" + frag);
                }
                List<Lookup.LookupResult> expected = SlowFuzzyMatch(prefixLen, maxEdits, transpositions, answers, frag);
                if (VERBOSE)
                {
                    Console.WriteLine("  expected: " + expected.size());
                    foreach (Lookup.LookupResult c in expected)
                    {
                        Console.WriteLine("    " + c);
                    }
                }
                List<Lookup.LookupResult> actual = new List<Lookup.LookupResult>(suggest.DoLookup(frag, false, NUM));
                if (VERBOSE)
                {
                    Console.WriteLine("  actual: " + actual.size());
                    foreach (Lookup.LookupResult c in actual)
                    {
                        Console.WriteLine("    " + c);
                    }
                }

                actual.Sort(new CompareByCostThenAlpha());

                int limit = Math.Min(expected.size(), actual.size());
                for (int ans = 0; ans < limit; ans++)
                {
                    Lookup.LookupResult c0 = expected.ElementAt(ans);
                    Lookup.LookupResult c1 = actual.ElementAt(ans);
                    assertEquals("expected " + c0.key +
                                 " but got " + c1.key,
                                 0,
                                 CHARSEQUENCE_COMPARATOR.Compare(c0.key, c1.key));
                    assertEquals(c0.value, c1.value);
                }
                assertEquals(expected.size(), actual.size());
            }
        }

        private List<Lookup.LookupResult> SlowFuzzyMatch(int prefixLen, int maxEdits, bool allowTransposition, List<Input> answers, string frag)
        {
            List<Lookup.LookupResult> results = new List<Lookup.LookupResult>();
            int fragLen = frag.Length;
            foreach (Input tf in answers)
            {
                //Console.WriteLine("  check s=" + tf.term.utf8ToString());
                bool prefixMatches = true;
                for (int i = 0; i < prefixLen; i++)
                {
                    if (i == fragLen)
                    {
                        // Prefix still matches:
                        break;
                    }
                    if (i == tf.term.Length || tf.term.Bytes[i] != (byte)frag[i])
                    {
                        prefixMatches = false;
                        break;
                    }
                }
                //Console.WriteLine("    prefixMatches=" + prefixMatches);

                if (prefixMatches)
                {
                    int len = tf.term.Length;
                    if (len >= fragLen - maxEdits)
                    {
                        // OK it's possible:
                        //Console.WriteLine("    possible");
                        int d;
                        string s = tf.term.Utf8ToString();
                        if (fragLen == prefixLen)
                        {
                            d = 0;
                        }
                        else if (false && len < fragLen)
                        {
                            d = GetDistance(frag, s, allowTransposition);
                        }
                        else
                        {
                            //Console.WriteLine("    try loop");
                            d = maxEdits + 1;
                            //for(int ed=-maxEdits;ed<=maxEdits;ed++) {
                            for (int ed = -maxEdits; ed <= maxEdits; ed++)
                            {
                                if (s.Length < fragLen - ed)
                                {
                                    continue;
                                }
                                string check = s.Substring(0, (fragLen - ed) - 0);
                                d = GetDistance(frag, check, allowTransposition);
                                //Console.WriteLine("    sub check s=" + check + " d=" + d);
                                if (d <= maxEdits)
                                {
                                    break;
                                }
                            }
                        }
                        if (d <= maxEdits)
                        {
                            results.Add(new Lookup.LookupResult(tf.term.Utf8ToString(), tf.v));
                        }
                    }
                }

                results.Sort(new CompareByCostThenAlpha());
            }

            return results;
        }

        internal class CharSequenceComparator : IComparer<string>
        {

            public int Compare(string o1, string o2)
            {
                int l1 = o1.Length;
                int l2 = o2.Length;

                int aStop = Math.Min(l1, l2);
                for (int i = 0; i < aStop; i++)
                {
                    int diff = o1[i] - o2[i];
                    if (diff != 0)
                    {
                        return diff;
                    }
                }
                // One is a prefix of the other, or, they are equal:
                return l1 - l2;
            }
        }

        private static readonly IComparer<string> CHARSEQUENCE_COMPARATOR = new CharSequenceComparator();

        public class CompareByCostThenAlpha : IComparer<Lookup.LookupResult>
        {

            public int Compare(Lookup.LookupResult a, Lookup.LookupResult b)
            {
                if (a.value > b.value)
                {
                    return -1;
                }
                else if (a.value < b.value)
                {
                    return 1;
                }
                else
                {
                    int c = CHARSEQUENCE_COMPARATOR.Compare(a.key, b.key);
                    Debug.Assert(c != 0, "term=" + a.key);
                    return c;
                }
            }
        }

        // NOTE: copied from
        // modules/suggest/src/java/org/apache/lucene/search/spell/LuceneLevenshteinDistance.java
        // and tweaked to return the edit distance not the float
        // lucene measure

        /* Finds unicode (code point) Levenstein (edit) distance
         * between two strings, including transpositions. */
        public int GetDistance(string target, string other, bool allowTransposition)
        {
            IntsRef targetPoints;
            IntsRef otherPoints;
            int n;
            int[][] d; // cost array

            // NOTE: if we cared, we could 3*m space instead of m*n space, similar to 
            // what LevenshteinDistance does, except cycling thru a ring of three 
            // horizontal cost arrays... but this comparator is never actually used by 
            // DirectSpellChecker, its only used for merging results from multiple shards 
            // in "distributed spellcheck", and its inefficient in other ways too...

            // cheaper to do this up front once
            targetPoints = ToIntsRef(target);
            otherPoints = ToIntsRef(other);
            n = targetPoints.Length;
            int m = otherPoints.Length;

            d = ReturnRectangularIntArray(n + 1, m + 1);


            if (n == 0 || m == 0)
            {
                if (n == m)
                {
                    return 0;
                }
                else
                {
                    return Math.Max(n, m);
                }
            }

            // indexes into strings s and t
            int i; // iterates through s
            int j; // iterates through t

            int t_j; // jth character of t

            int cost; // cost

            for (i = 0; i <= n; i++)
            {
                d[i][0] = i;
            }

            for (j = 0; j <= m; j++)
            {
                d[0][j] = j;
            }

            for (j = 1; j <= m; j++)
            {
                t_j = otherPoints.Ints[j - 1];

                for (i = 1; i <= n; i++)
                {
                    cost = targetPoints.Ints[i - 1] == t_j ? 0 : 1;
                    // minimum of cell to the left+1, to the top+1, diagonally left and up +cost
                    d[i][j] = Math.Min(Math.Min(d[i - 1][j] + 1, d[i][j - 1] + 1), d[i - 1][j - 1] + cost);
                    // transposition
                    if (allowTransposition && i > 1 && j > 1 && targetPoints.Ints[i - 1] == otherPoints.Ints[j - 2] && targetPoints.Ints[i - 2] == otherPoints.Ints[j - 1])
                    {
                        d[i][j] = Math.Min(d[i][j], d[i - 2][j - 2] + cost);
                    }
                }
            }

            return d[n][m];
        }

        internal static int[][] ReturnRectangularIntArray(int size1, int size2)
        {
            int[][] array;
            if (size1 > -1)
            {
                array = new int[size1][];
                if (size2 > -1)
                {
                    for (int Array1 = 0; Array1 < size1; Array1++)
                    {
                        array[Array1] = new int[size2];
                    }
                }
            }
            else
                array = null;

            return array;
        }

        private static IntsRef ToIntsRef(string s)
        {
            IntsRef @ref = new IntsRef(s.Length); // worst case
            int utf16Len = s.Length;
            for (int i = 0, cp = 0; i < utf16Len; i += Character.CharCount(cp))
            {
                cp = @ref.Ints[@ref.Length++] = Character.CodePointAt(s, i);
            }
            return @ref;
        }
    }
}
