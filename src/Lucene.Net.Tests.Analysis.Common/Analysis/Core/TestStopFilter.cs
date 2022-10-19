// Lucene version compatibility level 4.8.1
using Lucene.Net.Analysis.TokenAttributes;
using Lucene.Net.Analysis.Util;
using Lucene.Net.Util;
using NUnit.Framework;
using System;
using System.IO;
using System.Text;
using Console = Lucene.Net.Util.SystemConsole;
using JCG = J2N.Collections.Generic;
using Version = Lucene.Net.Util.LuceneVersion;

namespace Lucene.Net.Analysis.Core
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

    public class TestStopFilter : BaseTokenStreamTestCase
    {

        // other StopFilter functionality is already tested by TestStopAnalyzer

        [Test]
        public virtual void TestExactCase()
        {
            StringReader reader = new StringReader("Now is The Time");
            CharArraySet stopWords = new CharArraySet(TEST_VERSION_CURRENT, new string[] { "is", "the", "Time" }, false);
            TokenStream stream = new StopFilter(TEST_VERSION_CURRENT, new MockTokenizer(reader, MockTokenizer.WHITESPACE, false), stopWords);
            AssertTokenStreamContents(stream, new string[] { "Now", "The" });
        }

        [Test]
        public virtual void TestStopFilt()
        {
            StringReader reader = new StringReader("Now is The Time");
            string[] stopWords = new string[] { "is", "the", "Time" };
            CharArraySet stopSet = StopFilter.MakeStopSet(TEST_VERSION_CURRENT, stopWords);
            TokenStream stream = new StopFilter(TEST_VERSION_CURRENT, new MockTokenizer(reader, MockTokenizer.WHITESPACE, false), stopSet);
            AssertTokenStreamContents(stream, new string[] { "Now", "The" });
        }

        /// <summary>
        /// Test Position increments applied by StopFilter with and without enabling this option.
        /// </summary>
        [Test]
        public virtual void TestStopPositons()
        {
            StringBuilder sb = new StringBuilder();
            JCG.List<string> a = new JCG.List<string>();
            for (int i = 0; i < 20; i++)
            {
                string w = English.Int32ToEnglish(i).Trim();
                sb.Append(w).Append(' ');
                if (i % 3 != 0)
                {
                    a.Add(w);
                }
            }
            log(sb.ToString());
            string[] stopWords = a.ToArray();
            for (int i = 0; i < a.Count; i++)
            {
                log("Stop: " + stopWords[i]);
            }
            CharArraySet stopSet = StopFilter.MakeStopSet(TEST_VERSION_CURRENT, stopWords);
            // with increments
            StringReader reader = new StringReader(sb.ToString());
#pragma warning disable 612, 618
            StopFilter stpf = new StopFilter(Version.LUCENE_40, new MockTokenizer(reader, MockTokenizer.WHITESPACE, false), stopSet);
            DoTestStopPositons(stpf, true);
            // without increments
            reader = new StringReader(sb.ToString());
            stpf = new StopFilter(Version.LUCENE_43, new MockTokenizer(reader, MockTokenizer.WHITESPACE, false), stopSet);
#pragma warning restore 612, 618
            DoTestStopPositons(stpf, false);
            // with increments, concatenating two stop filters
            JCG.List<string> a0 = new JCG.List<string>();
            JCG.List<string> a1 = new JCG.List<string>();
            for (int i = 0; i < a.Count; i++)
            {
                if (i % 2 == 0)
                {
                    a0.Add(a[i]);
                }
                else
                {
                    a1.Add(a[i]);
                }
            }
            string[] stopWords0 = a0.ToArray();
            for (int i = 0; i < a0.Count; i++)
            {
                log("Stop0: " + stopWords0[i]);
            }
            string[] stopWords1 = a1.ToArray();
            for (int i = 0; i < a1.Count; i++)
            {
                log("Stop1: " + stopWords1[i]);
            }
            CharArraySet stopSet0 = StopFilter.MakeStopSet(TEST_VERSION_CURRENT, stopWords0);
            CharArraySet stopSet1 = StopFilter.MakeStopSet(TEST_VERSION_CURRENT, stopWords1);
            reader = new StringReader(sb.ToString());
            StopFilter stpf0 = new StopFilter(TEST_VERSION_CURRENT, new MockTokenizer(reader, MockTokenizer.WHITESPACE, false), stopSet0); // first part of the set
#pragma warning disable 612, 618
            stpf0.SetEnablePositionIncrements(true);
#pragma warning restore 612, 618
            StopFilter stpf01 = new StopFilter(TEST_VERSION_CURRENT, stpf0, stopSet1); // two stop filters concatenated!
            DoTestStopPositons(stpf01, true);
        }

        // LUCENE-3849: make sure after .end() we see the "ending" posInc
        [Test]
        public virtual void TestEndStopword()
        {
            CharArraySet stopSet = StopFilter.MakeStopSet(TEST_VERSION_CURRENT, "of");
            StopFilter stpf = new StopFilter(TEST_VERSION_CURRENT, new MockTokenizer(new StringReader("test of"), MockTokenizer.WHITESPACE, false), stopSet);
            AssertTokenStreamContents(stpf, new string[] { "test" }, new int[] { 0 }, new int[] { 4 }, null, new int[] { 1 }, null, 7, 1, null, true, null);
        }

        private void DoTestStopPositons(StopFilter stpf, bool enableIcrements)
        {
            log("---> test with enable-increments-" + (enableIcrements ? "enabled" : "disabled"));
#pragma warning disable 612, 618
            stpf.SetEnablePositionIncrements(enableIcrements);
#pragma warning restore 612, 618
            ICharTermAttribute termAtt = stpf.GetAttribute<ICharTermAttribute>();
            IPositionIncrementAttribute posIncrAtt = stpf.GetAttribute<IPositionIncrementAttribute>();
            stpf.Reset();
            for (int i = 0; i < 20; i += 3)
            {
                assertTrue(stpf.IncrementToken());
                log("Token " + i + ": " + stpf);
                string w = English.Int32ToEnglish(i).Trim();
                assertEquals("expecting token " + i + " to be " + w, w, termAtt.ToString());
                assertEquals("all but first token must have position increment of 3", enableIcrements ? (i == 0 ? 1 : 3) : 1, posIncrAtt.PositionIncrement);
            }
            assertFalse(stpf.IncrementToken());
            stpf.End();
            stpf.Dispose();
        }

        // print debug info depending on VERBOSE
        private static void log(string s)
        {
            if (Verbose)
            {
                Console.WriteLine(s);
            }
        }

        // stupid filter that inserts synonym of 'hte' for 'the'
        private sealed class MockSynonymFilter : TokenFilter
        {
            internal State bufferedState;
            internal ICharTermAttribute termAtt;
            internal IPositionIncrementAttribute posIncAtt;

            internal MockSynonymFilter(TokenStream input)
                : base(input)
            {
                termAtt = AddAttribute<ICharTermAttribute>();
                posIncAtt = AddAttribute<IPositionIncrementAttribute>();
            }

            public override sealed bool IncrementToken()
            {
                if (bufferedState != null)
                {
                    RestoreState(bufferedState);
                    posIncAtt.PositionIncrement = 0;
                    termAtt.SetEmpty().Append("hte");
                    bufferedState = null;
                    return true;
                }
                else if (m_input.IncrementToken())
                {
                    if (termAtt.ToString().Equals("the", StringComparison.Ordinal))
                    {
                        bufferedState = CaptureState();
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
                bufferedState = null;
            }
        }

        [Test]
        public virtual void TestFirstPosInc()
        {
            Analyzer analyzer = Analyzer.NewAnonymous(createComponents: (fieldName, reader) =>
            {
                Tokenizer tokenizer = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
                TokenFilter filter = new MockSynonymFilter(tokenizer);
#pragma warning disable 612, 618
                StopFilter stopfilter = new StopFilter(Version.LUCENE_43, filter, StopAnalyzer.ENGLISH_STOP_WORDS_SET);
                stopfilter.SetEnablePositionIncrements(false);
#pragma warning restore 612, 618
                return new TokenStreamComponents(tokenizer, stopfilter);
            });

            AssertAnalyzesTo(analyzer, "the quick brown fox", new string[] { "hte", "quick", "brown", "fox" }, new int[] { 1, 1, 1, 1 });
        }
    }
}