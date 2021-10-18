using J2N.Collections.Generic.Extensions;
using J2N.Text;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.TokenAttributes;
using Lucene.Net.Attributes;
using Lucene.Net.Diagnostics;
using Lucene.Net.Documents;
using Lucene.Net.Util;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
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

    public class AnalyzingSuggesterTest : LuceneTestCase
    {
        /** this is basically the WFST test ported to KeywordAnalyzer. so it acts the same */
        [Test]
        public void TestKeyword()
        {
            IEnumerable<Input> keys = Shuffle(
                new Input("foo", 50),
                new Input("bar", 10),
                new Input("barbar", 10),
                new Input("barbar", 12),
                new Input("barbara", 6),
                new Input("bar", 5),
                new Input("barbara", 1)
            );

            AnalyzingSuggester suggester = new AnalyzingSuggester(new MockAnalyzer(Random, MockTokenizer.KEYWORD, false));
            suggester.Build(new InputArrayEnumerator(keys));

            // top N of 2, but only foo is available
            IList<Lookup.LookupResult> results = suggester.DoLookup(TestUtil.StringToCharSequence("f", Random).ToString(), false, 2);
            assertEquals(1, results.size());
            assertEquals("foo", results[0].Key.toString());
            assertEquals(50, results[0].Value, 0.01F);

            // top N of 1 for 'bar': we return this even though
            // barbar is higher because exactFirst is enabled:
            results = suggester.DoLookup(TestUtil.StringToCharSequence("bar", Random).ToString(), false, 1);
            assertEquals(1, results.size());
            assertEquals("bar", results[0].Key.toString());
            assertEquals(10, results[0].Value, 0.01F);

            // top N Of 2 for 'b'
            results = suggester.DoLookup(TestUtil.StringToCharSequence("b", Random).ToString(), false, 2);
            assertEquals(2, results.size());
            assertEquals("barbar", results[0].Key.toString());
            assertEquals(12, results[0].Value, 0.01F);
            assertEquals("bar", results[1].Key.toString());
            assertEquals(10, results[1].Value, 0.01F);

            // top N of 3 for 'ba'
            results = suggester.DoLookup(TestUtil.StringToCharSequence("ba", Random).ToString(), false, 3);
            assertEquals(3, results.size());
            assertEquals("barbar", results[0].Key.toString());
            assertEquals(12, results[0].Value, 0.01F);
            assertEquals("bar", results[1].Key.toString());
            assertEquals(10, results[1].Value, 0.01F);
            assertEquals("barbara", results[2].Key.toString());
            assertEquals(6, results[2].Value, 0.01F);
        }

        [Test]
        public void TestKeywordWithPayloads()
        {
            IEnumerable<Input> keys = Shuffle(
                new Input("foo", 50, new BytesRef("hello")),
                new Input("bar", 10, new BytesRef("goodbye")),
                new Input("barbar", 12, new BytesRef("thank you")),
                new Input("bar", 9, new BytesRef("should be deduplicated")),
                new Input("bar", 8, new BytesRef("should also be deduplicated")),
                new Input("barbara", 6, new BytesRef("for all the fish")));

            AnalyzingSuggester suggester = new AnalyzingSuggester(new MockAnalyzer(Random, MockTokenizer.KEYWORD, false));
            suggester.Build(new InputArrayEnumerator(keys));
            for (int i = 0; i < 2; i++)
            {
                // top N of 2, but only foo is available
                IList<Lookup.LookupResult> results = suggester.DoLookup(TestUtil.StringToCharSequence("f", Random).ToString(), false, 2);
                assertEquals(1, results.size());
                assertEquals("foo", results[0].Key.toString());
                assertEquals(50, results[0].Value, 0.01F);
                assertEquals(new BytesRef("hello"), results[0].Payload);

                // top N of 1 for 'bar': we return this even though
                // barbar is higher because exactFirst is enabled:
                results = suggester.DoLookup(TestUtil.StringToCharSequence("bar", Random).ToString(), false, 1);
                assertEquals(1, results.size());
                assertEquals("bar", results[0].Key.toString());
                assertEquals(10, results[0].Value, 0.01F);
                assertEquals(new BytesRef("goodbye"), results[0].Payload);

                // top N Of 2 for 'b'
                results = suggester.DoLookup(TestUtil.StringToCharSequence("b", Random).ToString(), false, 2);
                assertEquals(2, results.size());
                assertEquals("barbar", results[0].Key.toString());
                assertEquals(12, results[0].Value, 0.01F);
                assertEquals(new BytesRef("thank you"), results[0].Payload);
                assertEquals("bar", results[1].Key.toString());
                assertEquals(10, results[1].Value, 0.01F);
                assertEquals(new BytesRef("goodbye"), results[1].Payload);

                // top N of 3 for 'ba'
                results = suggester.DoLookup(TestUtil.StringToCharSequence("ba", Random).ToString(), false, 3);
                assertEquals(3, results.size());
                assertEquals("barbar", results[0].Key.toString());
                assertEquals(12, results[0].Value, 0.01F);
                assertEquals(new BytesRef("thank you"), results[0].Payload);
                assertEquals("bar", results[1].Key.toString());
                assertEquals(10, results[1].Value, 0.01F);
                assertEquals(new BytesRef("goodbye"), results[1].Payload);
                assertEquals("barbara", results[2].Key.toString());
                assertEquals(6, results[2].Value, 0.01F);
                assertEquals(new BytesRef("for all the fish"), results[2].Payload);
            }
        }

        [Test]
        public void TestRandomRealisticKeys()
        {
            LineFileDocs lineFile = new LineFileDocs(Random);
            IDictionary<string, long> mapping = new JCG.Dictionary<string, long>();
            IList<Input> keys = new JCG.List<Input>();

            int howMany = AtLeast(100); // this might bring up duplicates
            for (int i = 0; i < howMany; i++)
            {
                Document nextDoc = lineFile.NextDoc();
                string title = nextDoc.GetField("title").GetStringValue();
                int randomWeight = Random.nextInt(100);
                keys.Add(new Input(title, randomWeight));
                if (!mapping.TryGetValue(title, out long titleValue) || titleValue < randomWeight)
                {
                    mapping[title] = Convert.ToInt64(randomWeight);
                }
            }
            AnalyzingSuggester analyzingSuggester = new AnalyzingSuggester(new MockAnalyzer(Random), new MockAnalyzer(Random),
                SuggesterOptions.EXACT_FIRST | SuggesterOptions.PRESERVE_SEP, 256, -1, Random.nextBoolean());
            bool doPayloads = Random.nextBoolean();
            if (doPayloads)
            {
                IList<Input> keysAndPayloads = new JCG.List<Input>();
                foreach (Input termFreq in keys)
                {
                    keysAndPayloads.Add(new Input(termFreq.term, termFreq.v, new BytesRef(termFreq.v.ToString())));
                }
                analyzingSuggester.Build(new InputArrayEnumerator(keysAndPayloads));
            }
            else
            {
                analyzingSuggester.Build(new InputArrayEnumerator(keys));
            }

            foreach (Input termFreq in keys)
            {
                IList<Lookup.LookupResult> lookup = analyzingSuggester.DoLookup(termFreq.term.Utf8ToString(), false, keys.size());
                foreach (Lookup.LookupResult lookupResult in lookup)
                {
                    assertEquals(mapping[lookupResult.Key], lookupResult.Value);
                    if (doPayloads)
                    {
                        assertEquals(lookupResult.Payload.Utf8ToString(), lookupResult.Value.ToString());
                    }
                    else
                    {
                        assertNull(lookupResult.Payload);
                    }
                }
            }

            lineFile.Dispose();
        }

        // TODO: more tests
        /**
         * basic "standardanalyzer" test with stopword removal
         */
        [Test]
        public void TestStandard()
        {
            Input[] keys = new Input[] {
                new Input("the ghost of christmas past", 50),
            };

            Analyzer standard = new MockAnalyzer(Random, MockTokenizer.WHITESPACE, true, MockTokenFilter.ENGLISH_STOPSET);
            AnalyzingSuggester suggester = new AnalyzingSuggester(standard, standard,
                SuggesterOptions.EXACT_FIRST | SuggesterOptions.PRESERVE_SEP, 256, -1, false);

            suggester.Build(new InputArrayEnumerator(keys));

            IList<Lookup.LookupResult> results = suggester.DoLookup(TestUtil.StringToCharSequence("the ghost of chris", Random).ToString(), false, 1);
            assertEquals(1, results.size());
            assertEquals("the ghost of christmas past", results[0].Key.toString());
            assertEquals(50, results[0].Value, 0.01F);

            // omit the 'the' since its a stopword, its suggested anyway
            results = suggester.DoLookup(TestUtil.StringToCharSequence("ghost of chris", Random).ToString(), false, 1);
            assertEquals(1, results.size());
            assertEquals("the ghost of christmas past", results[0].Key.toString());
            assertEquals(50, results[0].Value, 0.01F);

            // omit the 'the' and 'of' since they are stopwords, its suggested anyway
            results = suggester.DoLookup(TestUtil.StringToCharSequence("ghost chris", Random).ToString(), false, 1);
            assertEquals(1, results.size());
            assertEquals("the ghost of christmas past", results[0].Key.toString());
            assertEquals(50, results[0].Value, 0.01F);
        }

        [Test]
        public void TestEmpty()
        {
            Analyzer standard = new MockAnalyzer(Random, MockTokenizer.WHITESPACE, true, MockTokenFilter.ENGLISH_STOPSET);
            AnalyzingSuggester suggester = new AnalyzingSuggester(standard);
            suggester.Build(new InputArrayEnumerator(new Input[0]));

            IList<Lookup.LookupResult> result = suggester.DoLookup("a", false, 20);
            assertTrue(result.Count == 0);
        }

        [Test]
        public void TestNoSeps()
        {
            Input[]
            keys = new Input[] {
                new Input("ab cd", 0),
                new Input("abcd", 1),
            };

            SuggesterOptions options = 0;

            Analyzer a = new MockAnalyzer(Random);
            AnalyzingSuggester suggester = new AnalyzingSuggester(a, a, options, 256, -1, true);
            suggester.Build(new InputArrayEnumerator(keys));
            // TODO: would be nice if "ab " would allow the test to
            // pass, and more generally if the analyzer can know
            // that the user's current query has ended at a word, 
            // but, analyzers don't produce SEP tokens!
            IList<Lookup.LookupResult> r = suggester.DoLookup(TestUtil.StringToCharSequence("ab c", Random).ToString(), false, 2);
            assertEquals(2, r.size());

            // With no PRESERVE_SEPS specified, "ab c" should also
            // complete to "abcd", which has higher weight so should
            // appear first:
            assertEquals("abcd", r[0].Key.toString());
        }

        internal class TestGraphDupsTokenStreamComponents : TokenStreamComponents
        {
            private readonly AnalyzingSuggesterTest outerInstance;
            internal int tokenStreamCounter = 0;
            internal readonly TokenStream[] tokenStreams = new TokenStream[] {
            new CannedTokenStream(new Token[] {
                AnalyzingSuggesterTest.NewToken("wifi",1,1),
                AnalyzingSuggesterTest.NewToken("hotspot",0,2),
                AnalyzingSuggesterTest.NewToken("network",1,1),
                AnalyzingSuggesterTest.NewToken("is",1,1),
                AnalyzingSuggesterTest.NewToken("slow",1,1)
              }),
            new CannedTokenStream(new Token[] {
                AnalyzingSuggesterTest.NewToken("wi",1,1),
                AnalyzingSuggesterTest.NewToken("hotspot",0,3),
                AnalyzingSuggesterTest.NewToken("fi",1,1),
                AnalyzingSuggesterTest.NewToken("network",1,1),
                AnalyzingSuggesterTest.NewToken("is",1,1),
                AnalyzingSuggesterTest.NewToken("fast",1,1)

              }),
            new CannedTokenStream(new Token[] {
                AnalyzingSuggesterTest.NewToken("wifi",1,1),
                AnalyzingSuggesterTest.NewToken("hotspot",0,2),
                AnalyzingSuggesterTest.NewToken("network",1,1)
              }),
            };

            public TestGraphDupsTokenStreamComponents(AnalyzingSuggesterTest outerInstance, Tokenizer tokenizer)
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

            protected internal override void SetReader(TextReader reader)
            {
            }
        }

        internal class TestGraphDupsAnalyzer : Analyzer
        {
            private readonly AnalyzingSuggesterTest outerInstance;
            public TestGraphDupsAnalyzer(AnalyzingSuggesterTest outerInstance)
            {
                this.outerInstance = outerInstance;
            }

            protected internal override TokenStreamComponents CreateComponents(string fieldName, TextReader reader)
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
            //AnalyzingSuggester suggester = new AnalyzingSuggester(analyzer, AnalyzingSuggester.EXACT_FIRST, 256, -1);
            AnalyzingSuggester suggester = new AnalyzingSuggester(analyzer);
            suggester.Build(new InputArrayEnumerator(keys));
            IList<Lookup.LookupResult> results = suggester.DoLookup("wifi network", false, 10);
            if (Verbose)
            {
                Console.WriteLine("Results: " + results);
            }
            assertEquals(2, results.size());
            assertEquals("wifi network is slow", results[0].Key);
            assertEquals(50, results[0].Value);
            assertEquals("wi fi network is fast", results[1].Key);
            assertEquals(10, results[1].Value);
        }

        internal class TestInputPathRequiredTokenStreamComponents : TokenStreamComponents
        {
            private readonly AnalyzingSuggesterTest outerInstance;
            internal int tokenStreamCounter = 0;
            internal TokenStream[] tokenStreams = new TokenStream[] {
            new CannedTokenStream(new Token[] {
                AnalyzingSuggesterTest.NewToken("ab",1,1),
                AnalyzingSuggesterTest.NewToken("ba",0,1),
                AnalyzingSuggesterTest.NewToken("xc",1,1)
              }),
            new CannedTokenStream(new Token[] {
                AnalyzingSuggesterTest.NewToken("ba",1,1),
                AnalyzingSuggesterTest.NewToken("xd",1,1)
              }),
            new CannedTokenStream(new Token[] {
                AnalyzingSuggesterTest.NewToken("ab",1,1),
                AnalyzingSuggesterTest.NewToken("ba",0,1),
                AnalyzingSuggesterTest.NewToken("x",1,1)
              })
          };

            public TestInputPathRequiredTokenStreamComponents(AnalyzingSuggesterTest outerInstance, Tokenizer tokenizer)
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

            protected internal override void SetReader(TextReader reader)
            {
            }
        }

        internal class TestInputPathRequiredAnalyzer : Analyzer
        {
            private readonly AnalyzingSuggesterTest outerInstance;

            public TestInputPathRequiredAnalyzer(AnalyzingSuggesterTest outerInstance)
            {
                this.outerInstance = outerInstance;
            }

            protected internal override TokenStreamComponents CreateComponents(string fieldName, TextReader reader)
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
            AnalyzingSuggester suggester = new AnalyzingSuggester(analyzer);
            suggester.Build(new InputArrayEnumerator(keys));
            IList<Lookup.LookupResult> results = suggester.DoLookup("ab x", false, 1);
            assertTrue(results.size() == 1);
        }

        internal static Token NewToken(string term, int posInc, int posLength)
        {
            Token t = new Token(term, 0, 0);
            t.PositionIncrement = (posInc);
            t.PositionLength = (posLength);
            return t;
        }

        internal static BinaryToken NewToken(BytesRef term)
        {
            return new BinaryToken(term);
        }

        /*
        private void printTokens(final Analyzer analyzer, String input) throws IOException {
          System.out.println("Tokens for " + input);
          TokenStream ts = analyzer.tokenStream("", new StringReader(input));
          ts.reset();
          final TermToBytesRefAttribute termBytesAtt = ts.addAttribute(TermToBytesRefAttribute.class);
          final PositionIncrementAttribute posIncAtt = ts.addAttribute(PositionIncrementAttribute.class);
          final PositionLengthAttribute posLengthAtt = ts.addAttribute(PositionLengthAttribute.class);

          while(ts.incrementToken()) {
            termBytesAtt.fillBytesRef();
            System.out.println(String.format("%s,%s,%s", termBytesAtt.getBytesRef().utf8ToString(), posIncAtt.getPositionIncrement(), posLengthAtt.getPositionLength()));      
          }
          ts.end();
          ts.close();
        } 
        */

        internal class UsualTokenStreamComponents : TokenStreamComponents
        {
            private readonly AnalyzingSuggesterTest outerInstance;
            internal int count;

            public UsualTokenStreamComponents(AnalyzingSuggesterTest outerInstance, Tokenizer tokenizer)
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

            protected internal override void SetReader(TextReader reader)
            {
            }
        }
        internal class UsualAnalyzer : Analyzer
        {
            private readonly AnalyzingSuggesterTest outerInstance;
            public UsualAnalyzer(AnalyzingSuggesterTest outerInstance)
            {
                this.outerInstance = outerInstance;
            }

            protected internal override TokenStreamComponents CreateComponents(string fieldName, TextReader reader)
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
            SuggesterOptions options = SuggesterOptions.EXACT_FIRST | SuggesterOptions.PRESERVE_SEP;
            AnalyzingSuggester suggester = new AnalyzingSuggester(a, a, options, 256, -1, true);
            suggester.Build(new InputArrayEnumerator(new Input[] {
                new Input("x y", 1),
                new Input("x y z", 3),
                new Input("x", 2),
                new Input("z z z", 20),
            }));

            //System.out.println("ALL: " + suggester.lookup("x y", false, 6));

            for (int topN = 1; topN < 6; topN++)
            {
                IList<Lookup.LookupResult> results = suggester.DoLookup("x y", false, topN);
                //System.out.println("topN=" + topN + " " + results);

                assertEquals(Math.Min(topN, 4), results.size());

                assertEquals("x y", results[0].Key);
                assertEquals(1, results[0].Value);

                if (topN > 1)
                {
                    assertEquals("z z z", results[1].Key);
                    assertEquals(20, results[1].Value);

                    if (topN > 2)
                    {
                        assertEquals("x y z", results[2].Key);
                        assertEquals(3, results[2].Value);

                        if (topN > 3)
                        {
                            assertEquals("x", results[3].Key);
                            assertEquals(2, results[3].Value);
                        }
                    }
                }
            }
        }

        [Test]
        public void TestNonExactFirst()
        {

            Analyzer a = GetUnusualAnalyzer();
            AnalyzingSuggester suggester = new AnalyzingSuggester(a, a, SuggesterOptions.PRESERVE_SEP, 256, -1, true);

            suggester.Build(new InputArrayEnumerator(new Input[] {
                new Input("x y", 1),
                new Input("x y z", 3),
                new Input("x", 2),
                new Input("z z z", 20),
            }));

            for (int topN = 1; topN < 6; topN++)
            {
                IList<Lookup.LookupResult> results = suggester.DoLookup("p", false, topN);

                assertEquals(Math.Min(topN, 4), results.size());

                assertEquals("z z z", results[0].Key);
                assertEquals(20, results[0].Value);

                if (topN > 1)
                {
                    assertEquals("x y z", results[1].Key);
                    assertEquals(3, results[1].Value);

                    if (topN > 2)
                    {
                        assertEquals("x", results[2].Key);
                        assertEquals(2, results[2].Value);

                        if (topN > 3)
                        {
                            assertEquals("x y", results[3].Key);
                            assertEquals(1, results[3].Value);
                        }
                    }
                }
            }
        }

        // Holds surface form separately:
        internal class TermFreq2 : IComparable<TermFreq2>
        {
            public readonly string surfaceForm;
            public readonly string analyzedForm;
            public readonly long weight;
            public readonly BytesRef payload;

            public TermFreq2(string surfaceForm, string analyzedForm, long weight, BytesRef payload)
            {
                this.surfaceForm = surfaceForm;
                this.analyzedForm = analyzedForm;
                this.weight = weight;
                this.payload = payload;
            }

            public int CompareTo(TermFreq2 other)
            {
                int cmp = analyzedForm.CompareToOrdinal(other.analyzedForm);
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
                    if (Debugging.AssertsEnabled) Debugging.Assert(false);
                    return 0;
                }
            }

            public override string ToString()
            {
                return surfaceForm + "/" + weight;
            }
        }

        internal static bool IsStopChar(char ch, int numStopChars)
        {
            //System.out.println("IS? " + ch + ": " + (ch - 'a') + ": " + ((ch - 'a') < numStopChars));
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

            public override bool IncrementToken()
            {
                int skippedPositions = 0;
                while (m_input.IncrementToken())
                {
                    if (termAtt.Length != 1 || !IsStopChar(termAtt.Buffer[0], numStopChars))
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
                        //System.out.println("RETURN term=" + termAtt + " numStopChars=" + numStopChars);
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

            private readonly MockBytesAttributeFactory factory = new MockBytesAttributeFactory();

            public MockTokenEatingAnalyzer(int numStopChars, bool preserveHoles)
            {
                this.preserveHoles = preserveHoles;
                this.numStopChars = numStopChars;
            }

            protected internal override TokenStreamComponents CreateComponents(string fieldName, TextReader reader)
            {
                MockTokenizer tokenizer = new MockTokenizer(factory, reader, MockTokenizer.WHITESPACE, false, MockTokenizer.DEFAULT_MAX_TOKEN_LENGTH);
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

        private static char SEP = '\u001F';

        internal class TestRandomComparer : IComparer<TermFreq2>
        {
            public int Compare(TermFreq2 left, TermFreq2 right)
            {
                int cmp = ((float)right.weight).CompareTo((float)left.weight);
                if (cmp == 0)
                {
                    return left.analyzedForm.CompareToOrdinal(right.analyzedForm);
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

            int numQueries = AtLeast(1000);

            IList<TermFreq2> slowCompletor = new JCG.List<TermFreq2>();
            ISet<string> allPrefixes = new JCG.SortedSet<string>(StringComparer.Ordinal);
            ISet<string> seen = new JCG.HashSet<string>();

            bool doPayloads = Random.nextBoolean();

            Input[] keys = null;
            Input[] payloadKeys = null;
            if (doPayloads)
            {
                payloadKeys = new Input[numQueries];
            }
            else
            {
                keys = new Input[numQueries];
            }

            bool preserveSep = Random.nextBoolean();

            int numStopChars = Random.nextInt(10);
            bool preserveHoles = Random.nextBoolean();

            if (Verbose)
            {
                Console.WriteLine("TEST: " + numQueries + " words; preserveSep=" + preserveSep + " numStopChars=" + numStopChars + " preserveHoles=" + preserveHoles);
            }

            for (int i = 0; i < numQueries; i++)
            {
                int numTokens = TestUtil.NextInt32(Random, 1, 4);
                string key;
                string analyzedKey;
                while (true)
                {
                    key = "";
                    analyzedKey = "";
                    bool lastRemoved = false;
                    for (int token = 0; token < numTokens; token++)
                    {
                        string s;
                        while (true)
                        {
                            // TODO: would be nice to fix this slowCompletor/comparer to
                            // use full range, but we might lose some coverage too...
                            s = TestUtil.RandomSimpleString(Random);
                            if (s.Length > 0)
                            {
                                if (token > 0)
                                {
                                    key += " ";
                                }
                                if (preserveSep && analyzedKey.Length > 0 && analyzedKey[analyzedKey.Length - 1] != SEP)
                                {
                                    analyzedKey += SEP;
                                }
                                key += s;
                                if (s.Length == 1 && IsStopChar(s[0], numStopChars))
                                {
                                    lastRemoved = true;
                                    if (preserveSep && preserveHoles)
                                    {
                                        analyzedKey += SEP;
                                    }
                                }
                                else
                                {
                                    lastRemoved = false;
                                    analyzedKey += s;
                                }
                                break;
                            }
                        }
                    }

                    analyzedKey = Regex.Replace(analyzedKey, "(^|" + SEP + ")" + SEP + "$", "");

                    if (preserveSep && lastRemoved)
                    {
                        analyzedKey += SEP;
                    }

                    // Don't add same surface form more than once:
                    if (!seen.contains(key))
                    {
                        seen.add(key);
                        break;
                    }
                }

                for (int j = 1; j < key.Length; j++)
                {
                    allPrefixes.add(key.Substring(0, j - 0));
                }
                // we can probably do Integer.MAX_VALUE here, but why worry.
                int weight = Random.nextInt(1 << 24);
                BytesRef payload;
                if (doPayloads)
                {
                    byte[] bytes = new byte[Random.nextInt(10)];
                    Random.NextBytes(bytes);
                    payload = new BytesRef(bytes);
                    payloadKeys[i] = new Input(key, weight, payload);
                }
                else
                {
                    keys[i] = new Input(key, weight);
                    payload = null;
                }

                slowCompletor.Add(new TermFreq2(key, analyzedKey, weight, payload));
            }

            if (Verbose)
            {
                // Don't just sort original list, to avoid VERBOSE
                // altering the test:
                IList<TermFreq2> sorted = new JCG.List<TermFreq2>(slowCompletor);
                // LUCENENET NOTE: Must use TimSort because comparer is not expecting ties
                CollectionUtil.TimSort(sorted);
                foreach (TermFreq2 ent in sorted)
                {
                    Console.WriteLine("  surface='" + ent.surfaceForm + "' analyzed='" + ent.analyzedForm + "' weight=" + ent.weight);
                }
            }

            Analyzer a = new MockTokenEatingAnalyzer(numStopChars, preserveHoles);
            AnalyzingSuggester suggester = new AnalyzingSuggester(a, a,
                                                                  preserveSep ? SuggesterOptions.PRESERVE_SEP : 0, 256, -1, true);
            if (doPayloads)
            {
                suggester.Build(new InputArrayEnumerator(Shuffle(payloadKeys)));
            }
            else
            {
                suggester.Build(new InputArrayEnumerator(Shuffle(keys)));
            }

            foreach (string prefix in allPrefixes)
            {

                if (Verbose)
                {
                    Console.WriteLine("\nTEST: prefix=" + prefix);
                }

                int topN = TestUtil.NextInt32(Random, 1, 10);
                IList<Lookup.LookupResult> r = suggester.DoLookup(TestUtil.StringToCharSequence(prefix, Random).ToString(), false, topN);

                // 2. go thru whole set to find suggestions:
                JCG.List<TermFreq2> matches = new JCG.List<TermFreq2>();

                // "Analyze" the key:
                string[] tokens = prefix.Split(' ').TrimEnd();
                StringBuilder builder = new StringBuilder();
                bool lastRemoved = false;
                for (int i = 0; i < tokens.Length; i++)
                {
                    string token = tokens[i];
                    if (preserveSep && builder.Length > 0 && !builder.ToString().EndsWith("" + SEP, StringComparison.Ordinal))
                    {
                        builder.Append(SEP);
                    }

                    if (token.Length == 1 && IsStopChar(token[0], numStopChars))
                    {
                        if (preserveSep && preserveHoles)
                        {
                            builder.Append(SEP);
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
                    string s = Regex.Replace(analyzedKey, SEP + "$", "");
                    if (s.Equals(analyzedKey, StringComparison.Ordinal))
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

                if (preserveSep && (prefix.EndsWith(" ", StringComparison.Ordinal) || lastRemoved))
                {
                    analyzedKey += SEP;
                }

                if (Verbose)
                {
                    Console.WriteLine("  analyzed: " + analyzedKey);
                }

                // TODO: could be faster... but its slowCompletor for a reason
                foreach (TermFreq2 e in slowCompletor)
                {
                    if (e.analyzedForm.StartsWith(analyzedKey, StringComparison.Ordinal))
                    {
                        matches.Add(e);
                    }
                }

                assertTrue(numStopChars > 0 || matches.size() > 0);

                if (matches.size() > 1)
                {
                    matches.Sort(new TestRandomComparer());
                }

                if (matches.size() > topN)
                {
                    matches = matches.GetView(0, topN); // LUCENENET: Checked length for correctness
                }

                if (Verbose)
                {
                    Console.WriteLine("  expected:");
                    foreach (TermFreq2 lr in matches)
                    {
                        Console.WriteLine("    key=" + lr.surfaceForm + " weight=" + lr.weight);
                    }

                    Console.WriteLine("  actual:");
                    foreach (Lookup.LookupResult lr in r)
                    {
                        Console.WriteLine("    key=" + lr.Key + " weight=" + lr.Value);
                    }
                }

                assertEquals(matches.size(), r.size());

                for (int hit = 0; hit < r.size(); hit++)
                {
                    //System.out.println("  check hit " + hit);
                    assertEquals(matches[hit].surfaceForm, r[hit].Key);
                    assertEquals(matches[hit].weight, r[hit].Value, 0f);
                    if (doPayloads)
                    {
                        assertEquals(matches[hit].payload, r[hit].Payload);
                    }
                }
            }
        }

        /// <summary>
        /// LUCENENET specific test. Added fixed inputs to help with debugging issues found in TestRandom().
        /// Not necessarily required per se, but it may come in handy again if the above test fails.
        /// </summary>
        [Test]
        [LuceneNetSpecific]
        public void TestFixed()
        {
            bool preserveSep = true;
            int numStopChars = 2;
            bool preserveHoles = true;

            string token11 = "foo bar foo bar";
            string token21 = "bar foo orange cat";

            string token12 = "sally sells seashells by the sea shore";
            string token22 = "peter piper picked a pack of pickled peppers";

            string query1 = "ba";
            string query2 = "pet";

            // Query 1
            Analyzer a1 = new MockTokenEatingAnalyzer(numStopChars, preserveHoles);
            AnalyzingSuggester suggester1 = new AnalyzingSuggester(a1, a1,
                preserveSep ? SuggesterOptions.PRESERVE_SEP : 0, 256, -1, true);

            suggester1.Build(new InputArrayEnumerator(new Input[] { new Input(token11, 123456), new Input(token21, 654321) }));

            int topN1 = 4;
            IList<Lookup.LookupResult> r1 = suggester1.DoLookup(query1, false, topN1);

            assertEquals(1, r1.size());

            assertEquals("bar foo orange cat", r1[0].Key);
            assertEquals(654321, r1[0].Value, 0f);

            // Query 2
            Analyzer a2 = new MockTokenEatingAnalyzer(numStopChars, preserveHoles);
            AnalyzingSuggester suggester2 = new AnalyzingSuggester(a2, a2,
                preserveSep ? SuggesterOptions.PRESERVE_SEP : 0, 256, -1, true);

            suggester2.Build(new InputArrayEnumerator(new Input[] { new Input(token12, 1234567), new Input(token22, 7654321) }));

            int topN2 = 4;
            IList<Lookup.LookupResult> r2 = suggester2.DoLookup(query2, false, topN2);

            assertEquals(1, r2.size());

            assertEquals("peter piper picked a pack of pickled peppers", r2[0].Key);
            assertEquals(7654321, r2[0].Value, 0f);
        }


        [Test]
        public void TestMaxSurfaceFormsPerAnalyzedForm()
        {
            Analyzer a = new MockAnalyzer(Random);
            AnalyzingSuggester suggester = new AnalyzingSuggester(a, a, 0, 2, -1, true);
            suggester.Build(new InputArrayEnumerator(Shuffle(new Input("a", 40),
                    new Input("a ", 50), new Input(" a", 60))));

            IList<Lookup.LookupResult> results = suggester.DoLookup("a", false, 5);
            assertEquals(2, results.size());
            assertEquals(" a", results[0].Key);
            assertEquals(60, results[0].Value);
            assertEquals("a ", results[1].Key);
            assertEquals(50, results[1].Value);
        }

        [Test]
        public void TestQueueExhaustion()
        {
            Analyzer a = new MockAnalyzer(Random);
            AnalyzingSuggester suggester = new AnalyzingSuggester(a, a, SuggesterOptions.EXACT_FIRST, 256, -1, true);

            suggester.Build(new InputArrayEnumerator(new Input[] {
                new Input("a", 2),
                new Input("a b c", 3),
                new Input("a c a", 1),
                new Input("a c b", 1),
            }));

            suggester.DoLookup("a", false, 4);
        }

        [Test]
        public void TestExactFirstMissingResult()
        {

            Analyzer a = new MockAnalyzer(Random);

            AnalyzingSuggester suggester = new AnalyzingSuggester(a, a, SuggesterOptions.EXACT_FIRST, 256, -1, true);

            suggester.Build(new InputArrayEnumerator(new Input[] {
                new Input("a", 5),
                new Input("a b", 3),
                new Input("a c", 4),
            }));

            assertEquals(3, suggester.Count);
            IList<Lookup.LookupResult> results = suggester.DoLookup("a", false, 3);
            assertEquals(3, results.size());
            assertEquals("a", results[0].Key);
            assertEquals(5, results[0].Value);
            assertEquals("a c", results[1].Key);
            assertEquals(4, results[1].Value);
            assertEquals("a b", results[2].Key);
            assertEquals(3, results[2].Value);

            // Try again after save/load:
            DirectoryInfo tmpDir = CreateTempDir("AnalyzingSuggesterTest");
            tmpDir.Create();

            FileInfo path = new FileInfo(Path.Combine(tmpDir.FullName, "suggester"));

            Stream os = new FileStream(path.FullName, FileMode.OpenOrCreate);
            suggester.Store(os);
            os.Dispose();

            Stream @is = new FileStream(path.FullName, FileMode.Open);
            suggester.Load(@is);
            @is.Dispose();

            assertEquals(3, suggester.Count);
            results = suggester.DoLookup("a", false, 3);
            assertEquals(3, results.size());
            assertEquals("a", results[0].Key);
            assertEquals(5, results[0].Value);
            assertEquals("a c", results[1].Key);
            assertEquals(4, results[1].Value);
            assertEquals("a b", results[2].Key);
            assertEquals(3, results[2].Value);
        }

        internal class TestDupSurfaceFormsMissingResultsTokenStreamComponents : TokenStreamComponents
        {
            private readonly AnalyzingSuggesterTest outerInstance;
            public TestDupSurfaceFormsMissingResultsTokenStreamComponents(AnalyzingSuggesterTest outerInstance, Tokenizer tokenizer)
                : base(tokenizer)
            {
                this.outerInstance = outerInstance;
            }

            public override TokenStream TokenStream => new CannedTokenStream(new Token[] {
                    NewToken("hairy", 1, 1),
                    NewToken("smelly", 0, 1),
                    NewToken("dog", 1, 1),
                });

            protected internal override void SetReader(TextReader reader)
            {
            }
        }

        internal class TestDupSurfaceFormsMissingResultsAnalyzer : Analyzer
        {
            private readonly AnalyzingSuggesterTest outerInstance;
            public TestDupSurfaceFormsMissingResultsAnalyzer(AnalyzingSuggesterTest outerInstance)
            {
                this.outerInstance = outerInstance;
            }
            protected internal override TokenStreamComponents CreateComponents(string fieldName, TextReader reader)
            {
                Tokenizer tokenizer = new MockTokenizer(reader, MockTokenizer.SIMPLE, true);
                return new TestDupSurfaceFormsMissingResultsTokenStreamComponents(outerInstance, tokenizer);
            }
        }

        [Test]
        public void TestDupSurfaceFormsMissingResults()
        {
            Analyzer a = new TestDupSurfaceFormsMissingResultsAnalyzer(this);

            AnalyzingSuggester suggester = new AnalyzingSuggester(a, a, 0, 256, -1, true);

            suggester.Build(new InputArrayEnumerator(Shuffle(
                      new Input("hambone", 6),
                      new Input("nellie", 5))));

            IList<Lookup.LookupResult> results = suggester.DoLookup("nellie", false, 2);
            assertEquals(2, results.size());
            assertEquals("hambone", results[0].Key);
            assertEquals(6, results[0].Value);
            assertEquals("nellie", results[1].Key);
            assertEquals(5, results[1].Value);

            // Try again after save/load:
            DirectoryInfo tmpDir = CreateTempDir("AnalyzingSuggesterTest");
            tmpDir.Create();

            FileInfo path = new FileInfo(Path.Combine(tmpDir.FullName, "suggester"));

            Stream os = new FileStream(path.FullName, FileMode.OpenOrCreate);
            suggester.Store(os);
            os.Dispose();

            Stream @is = new FileStream(path.FullName, FileMode.Open);
            suggester.Load(@is);
            @is.Dispose();

            results = suggester.DoLookup("nellie", false, 2);
            assertEquals(2, results.size());
            assertEquals("hambone", results[0].Key);
            assertEquals(6, results[0].Value);
            assertEquals("nellie", results[1].Key);
            assertEquals(5, results[1].Value);
        }

        internal class TestDupSurfaceFormsMissingResults2TokenStreamComponents : TokenStreamComponents
        {
            internal int count;
            public TestDupSurfaceFormsMissingResults2TokenStreamComponents(Tokenizer tokenizer)
                : base(tokenizer)
            { }

            public override TokenStream TokenStream
            {
                get
                {
                    if (count == 0)
                    {
                        count++;
                        return new CannedTokenStream(new Token[] {
                            NewToken("p", 1, 1),
                            NewToken("q", 1, 1),
                            NewToken("r", 0, 1),
                            NewToken("s", 0, 1),
                        });
                    }
                    else
                    {
                        return new CannedTokenStream(new Token[] {
                            NewToken("p", 1, 1),
                        });
                    }
                }
            }

            protected internal override void SetReader(TextReader reader)
            {
            }
        }

        internal class TestDupSurfaceFormsMissingResults2Analyzer : Analyzer
        {
            protected internal override TokenStreamComponents CreateComponents(string fieldName, TextReader reader)
            {
                Tokenizer tokenizer = new MockTokenizer(reader, MockTokenizer.SIMPLE, true);
                return new TestDupSurfaceFormsMissingResults2TokenStreamComponents(tokenizer);
            }
        }

        [Test]
        public void TestDupSurfaceFormsMissingResults2()
        {
            Analyzer a = new TestDupSurfaceFormsMissingResults2Analyzer();

            AnalyzingSuggester suggester = new AnalyzingSuggester(a, a, 0, 256, -1, true);

            suggester.Build(new InputArrayEnumerator(new Input[] {
                new Input("a", 6),
                new Input("b", 5),
            }));

            IList<Lookup.LookupResult> results = suggester.DoLookup("a", false, 2);
            assertEquals(2, results.size());
            assertEquals("a", results[0].Key);
            assertEquals(6, results[0].Value);
            assertEquals("b", results[1].Key);
            assertEquals(5, results[1].Value);

            // Try again after save/load:
            DirectoryInfo tmpDir = CreateTempDir("AnalyzingSuggesterTest");
            tmpDir.Create();

            FileInfo path = new FileInfo(Path.Combine(tmpDir.FullName, "suggester"));

            Stream os = new FileStream(path.FullName, FileMode.OpenOrCreate);
            suggester.Store(os);
            os.Dispose();

            Stream @is = new FileStream(path.FullName, FileMode.Open);
            suggester.Load(@is);
            @is.Dispose();

            results = suggester.DoLookup("a", false, 2);
            assertEquals(2, results.size());
            assertEquals("a", results[0].Key);
            assertEquals(6, results[0].Value);
            assertEquals("b", results[1].Key);
            assertEquals(5, results[1].Value);
        }

        internal class Test0ByteKeysTokenStreamComponents : TokenStreamComponents
        {
            internal int tokenStreamCounter = 0;
            internal TokenStream[] tokenStreams = new TokenStream[] {
              new CannedBinaryTokenStream(new BinaryToken[] {
                  NewToken(new BytesRef(new byte[] {0x0, 0x0, 0x0})),
                }),
              new CannedBinaryTokenStream(new BinaryToken[] {
                  NewToken(new BytesRef(new byte[] {0x0, 0x0})),
                }),
              new CannedBinaryTokenStream(new BinaryToken[] {
                  NewToken(new BytesRef(new byte[] {0x0, 0x0, 0x0})),
                }),
              new CannedBinaryTokenStream(new BinaryToken[] {
                  NewToken(new BytesRef(new byte[] {0x0, 0x0})),
                }),
            };

            public Test0ByteKeysTokenStreamComponents(Tokenizer tokenizer)
                : base(tokenizer)
            { }

            public override TokenStream TokenStream
            {
                get
                {
                    TokenStream result = tokenStreams[tokenStreamCounter];
                    tokenStreamCounter++;
                    return result;
                }
            }

            protected internal override void SetReader(TextReader reader)
            {
            }
        }

        internal class Test0ByteKeysAnalyzer : Analyzer
        {
            protected internal override TokenStreamComponents CreateComponents(string fieldName, TextReader reader)
            {
                Tokenizer tokenizer = new MockTokenizer(reader, MockTokenizer.SIMPLE, true);
                return new Test0ByteKeysTokenStreamComponents(tokenizer);
            }
        }

        [Test]
        public void Test0ByteKeys()
        {
            Analyzer a = new Test0ByteKeysAnalyzer();

            AnalyzingSuggester suggester = new AnalyzingSuggester(a, a, 0, 256, -1, true);

            suggester.Build(new InputArrayEnumerator(new Input[] {
                new Input("a a", 50),
                new Input("a b", 50),
            }));
        }

        [Test]
        public void TestDupSurfaceFormsMissingResults3()
        {
            Analyzer a = new MockAnalyzer(Random);
            AnalyzingSuggester suggester = new AnalyzingSuggester(a, a, SuggesterOptions.PRESERVE_SEP, 256, -1, true);
            suggester.Build(new InputArrayEnumerator(new Input[] {
                new Input("a a", 7),
                new Input("a a", 7),
                new Input("a c", 6),
                new Input("a c", 3),
                new Input("a b", 5),
            }));
            assertEquals("[a a/7, a c/6, a b/5]", suggester.DoLookup("a", false, 3).toString());
        }

        [Test]
        public void TestEndingSpace()
        {
            Analyzer a = new MockAnalyzer(Random);
            AnalyzingSuggester suggester = new AnalyzingSuggester(a, a, SuggesterOptions.PRESERVE_SEP, 256, -1, true);
            suggester.Build(new InputArrayEnumerator(new Input[] {
                new Input("i love lucy", 7),
                new Input("isla de muerta", 8),
            }));
            assertEquals("[isla de muerta/8, i love lucy/7]", suggester.DoLookup("i", false, 3).toString());
            assertEquals("[i love lucy/7]", suggester.DoLookup("i ", false, 3).toString());
        }

        internal class TestTooManyExpressionsTokenStreamComponents : TokenStreamComponents
        {
            public TestTooManyExpressionsTokenStreamComponents(Tokenizer tokenizer)
                : base(tokenizer)
            { }

            public override TokenStream TokenStream
            {
                get
                {
                    Token a = new Token("a", 0, 1);
                    a.PositionIncrement = (1);
                    Token b = new Token("b", 0, 1);
                    b.PositionIncrement = (0);
                    return new CannedTokenStream(new Token[] { a, b });
                }
            }

            protected internal override void SetReader(TextReader reader)
            {
            }
        }
        internal class TestTooManyExpressionsAnalyzer : Analyzer
        {
            protected internal override TokenStreamComponents CreateComponents(string fieldName, TextReader reader)
            {
                Tokenizer tokenizer = new MockTokenizer(reader, MockTokenizer.SIMPLE, true);
                return new TestTooManyExpressionsTokenStreamComponents(tokenizer);
            }
        }

        [Test]
        public void TestTooManyExpansions()
        {
            Analyzer a = new TestTooManyExpressionsAnalyzer();

            AnalyzingSuggester suggester = new AnalyzingSuggester(a, a, 0, 256, 1, true);
            suggester.Build(new InputArrayEnumerator(new Input[] { new Input("a", 1) }));

            assertEquals("[a/1]", suggester.DoLookup("a", false, 1).toString());
        }

        [Test]
        public void TestIllegalLookupArgument()
        {
            Analyzer a = new MockAnalyzer(Random);
            AnalyzingSuggester suggester = new AnalyzingSuggester(a, a, 0, 256, -1, true);
            suggester.Build(new InputArrayEnumerator(new Input[] {
                new Input("а где Люси?", 7),
            }));
            try
            {
                suggester.DoLookup("а\u001E", false, 3);
                fail("should throw IllegalArgumentException");
            }
            catch (Exception e) when (e.IsIllegalArgumentException())
            {
                // expected
            }
            try
            {
                suggester.DoLookup("а\u001F", false, 3);
                fail("should throw IllegalArgumentException");
            }
            catch (Exception e) when (e.IsIllegalArgumentException())
            {
                // expected
            }
        }

        internal static IEnumerable<Input> Shuffle(params Input[] values)
        {
            IList<Input> asList = new JCG.List<Input>(values.Length);
            foreach (Input value in values)
            {
                asList.Add(value);
            }
            asList.Shuffle(Random);
            return asList;
        }

        // LUCENENET TODO: This is a test from Lucene 4.8.1 that currently produces a stack overflow
        //// TODO: we need BaseSuggesterTestCase?
        //[Test]
        //public void TestTooLongSuggestion()
        //{
        //    Analyzer a = new MockAnalyzer(Random);
        //    AnalyzingSuggester suggester = new AnalyzingSuggester(a);
        //    String bigString = TestUtil.RandomSimpleString(Random, 60000, 60000);
        //    try
        //    {
        //        suggester.Build(new InputArrayEnumerator(new Input[] {
        //            new Input(bigString, 7)}));
        //        fail("did not hit expected exception");
        //    }
        //    catch (Exception iae) when (iae.IsIllegalArgumentException())
        //    {
        //        // expected
        //    }
        //}
    }
}
