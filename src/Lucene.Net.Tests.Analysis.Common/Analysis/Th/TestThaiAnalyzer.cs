// Lucene version compatibility level 4.8.1
#if FEATURE_BREAKITERATOR
using J2N.Threading;
using Lucene.Net.Analysis.Core;
using Lucene.Net.Analysis.TokenAttributes;
using Lucene.Net.Analysis.Util;
using Lucene.Net.Attributes;
using Lucene.Net.Support.Threading;
using Lucene.Net.Util;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using JCG = J2N.Collections.Generic;

namespace Lucene.Net.Analysis.Th
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
    /// Test case for ThaiAnalyzer, modified from TestFrenchAnalyzer
    /// 
    /// </summary>

    public class TestThaiAnalyzer : BaseTokenStreamTestCase
    {
        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            // LUCENENET specific - ICU always has a dictionary-based BreakIterator in .NET.
            //AssumeTrue("JRE does not support Thai dictionary-based BreakIterator", ThaiTokenizer.DBBI_AVAILABLE);
        }
        /*
         * testcase for offsets
         */
        [Test]
        public virtual void TestOffsets()
        {
            AssertAnalyzesTo(new ThaiAnalyzer(TEST_VERSION_CURRENT, CharArraySet.Empty), "การที่ได้ต้องแสดงว่างานดี", new string[] { "การ", "ที่", "ได้", "ต้อง", "แสดง", "ว่า", "งาน", "ดี" }, new int[] { 0, 3, 6, 9, 13, 17, 20, 23 }, new int[] { 3, 6, 9, 13, 17, 20, 23, 25 });
        }

        [Test]
        public virtual void TestStopWords()
        {
            AssertAnalyzesTo(new ThaiAnalyzer(TEST_VERSION_CURRENT), "การที่ได้ต้องแสดงว่างานดี", new string[] { "แสดง", "งาน", "ดี" }, new int[] { 13, 20, 23 }, new int[] { 17, 23, 25 }, new int[] { 5, 2, 1 });
        }

        [Test]
        public virtual void TestBackwardsStopWords()
        {
            AssertAnalyzesTo(
#pragma warning disable 612, 618
                new ThaiAnalyzer(LuceneVersion.LUCENE_35),
#pragma warning restore 612, 618
                "การที่ได้ต้องแสดงว่างานดี", new string[] { "การ", "ที่", "ได้", "ต้อง", "แสดง", "ว่า", "งาน", "ดี" }, new int[] { 0, 3, 6, 9, 13, 17, 20, 23 }, new int[] { 3, 6, 9, 13, 17, 20, 23, 25 });
        }

        /// <summary>
        /// Thai numeric tokens were typed as <ALPHANUM> instead of <NUM>. </summary>
        /// @deprecated (3.1) testing backwards behavior 
        [Test]
        [Obsolete("(3.1) testing backwards behavior")]
        public virtual void TestBuggyTokenType30()
        {
            AssertAnalyzesTo(new ThaiAnalyzer(LuceneVersion.LUCENE_30), "การที่ได้ต้องแสดงว่างานดี ๑๒๓", new string[] { "การ", "ที่", "ได้", "ต้อง", "แสดง", "ว่า", "งาน", "ดี", "๑๒๓" }, new string[] { "<ALPHANUM>", "<ALPHANUM>", "<ALPHANUM>", "<ALPHANUM>", "<ALPHANUM>", "<ALPHANUM>", "<ALPHANUM>", "<ALPHANUM>", "<ALPHANUM>" });
        }

        /// @deprecated (3.1) testing backwards behavior 
        [Test]
        [Obsolete("(3.1) testing backwards behavior")]
        public virtual void TestAnalyzer30()
        {
            ThaiAnalyzer analyzer = new ThaiAnalyzer(LuceneVersion.LUCENE_30);

            AssertAnalyzesTo(analyzer, "", new string[] { });

            AssertAnalyzesTo(analyzer, "การที่ได้ต้องแสดงว่างานดี", new string[] { "การ", "ที่", "ได้", "ต้อง", "แสดง", "ว่า", "งาน", "ดี" });

            AssertAnalyzesTo(analyzer, "บริษัทชื่อ XY&Z - คุยกับ xyz@demo.com", new string[] { "บริษัท", "ชื่อ", "xy&z", "คุย", "กับ", "xyz@demo.com" });

            // English stop words
            AssertAnalyzesTo(analyzer, "ประโยคว่า The quick brown fox jumped over the lazy dogs", new string[] { "ประโยค", "ว่า", "quick", "brown", "fox", "jumped", "over", "lazy", "dogs" });
        }

        // Ellision character
        private static readonly char THAI_PAIYANNOI = (char)0x0E2F;
        // Repeat character
        private static readonly char THAI_MAIYAMOK = (char)0x0E46;

        [Test]
        [LuceneNetSpecific]
        public virtual void TestThaiBreakEngineInitializerCode()
        {
            // Initialize UnicodeSets
            var fThaiWordSet = new ICU4N.Text.UnicodeSet();
            var fMarkSet = new ICU4N.Text.UnicodeSet();
            var fBeginWordSet = new ICU4N.Text.UnicodeSet();
            var fSuffixSet = new ICU4N.Text.UnicodeSet();

            fThaiWordSet.ApplyPattern("[[:Thai:]&[:LineBreak=SA:]]");
            fThaiWordSet.Compact();

            fMarkSet.ApplyPattern("[[:Thai:]&[:LineBreak=SA:]&[:M:]]");
            fMarkSet.Add(0x0020);
            var fEndWordSet = new ICU4N.Text.UnicodeSet(fThaiWordSet);
            fEndWordSet.Remove(0x0E31); // MAI HAN-AKAT
            fEndWordSet.Remove(0x0E40, 0x0E44); // SARA E through SARA AI MAIMALAI
            fBeginWordSet.Add(0x0E01, 0x0E2E); //KO KAI through HO NOKHUK
            fBeginWordSet.Add(0x0E40, 0x0E44); // SARA E through SARA AI MAIMALAI
            fSuffixSet.Add(THAI_PAIYANNOI);
            fSuffixSet.Add(THAI_MAIYAMOK);

            // Compact for caching
            fMarkSet.Compact();
            fEndWordSet.Compact();
            fBeginWordSet.Compact();
            fSuffixSet.Compact();

            // Freeze the static UnicodeSet
            fThaiWordSet.Freeze();
            fMarkSet.Freeze();
            fEndWordSet.Freeze();
            fBeginWordSet.Freeze();
            fSuffixSet.Freeze();
        }

        /*
         * Test that position increments are adjusted correctly for stopwords.
         */
        // note this test uses stopfilter's stopset
        [Test]
        public virtual void TestPositionIncrements()
        {
            ThaiAnalyzer analyzer = new ThaiAnalyzer(TEST_VERSION_CURRENT, StopAnalyzer.ENGLISH_STOP_WORDS_SET);
            AssertAnalyzesTo(analyzer, "การที่ได้ต้อง the แสดงว่างานดี", new string[] { "การ", "ที่", "ได้", "ต้อง", "แสดง", "ว่า", "งาน", "ดี" }, new int[] { 0, 3, 6, 9, 18, 22, 25, 28 }, new int[] { 3, 6, 9, 13, 22, 25, 28, 30 }, new int[] { 1, 1, 1, 1, 2, 1, 1, 1 });

            // case that a stopword is adjacent to thai text, with no whitespace
            AssertAnalyzesTo(analyzer, "การที่ได้ต้องthe แสดงว่างานดี", new string[] { "การ", "ที่", "ได้", "ต้อง", "แสดง", "ว่า", "งาน", "ดี" }, new int[] { 0, 3, 6, 9, 17, 21, 24, 27 }, new int[] { 3, 6, 9, 13, 21, 24, 27, 29 }, new int[] { 1, 1, 1, 1, 2, 1, 1, 1 });
        }

        [Test]
        public virtual void TestReusableTokenStream()
        {
            ThaiAnalyzer analyzer = new ThaiAnalyzer(TEST_VERSION_CURRENT, CharArraySet.Empty);
            AssertAnalyzesTo(analyzer, "", new string[] { });

            AssertAnalyzesTo(analyzer, "การที่ได้ต้องแสดงว่างานดี", new string[] { "การ", "ที่", "ได้", "ต้อง", "แสดง", "ว่า", "งาน", "ดี" });

            AssertAnalyzesTo(analyzer, "บริษัทชื่อ XY&Z - คุยกับ xyz@demo.com", new string[] { "บริษัท", "ชื่อ", "xy", "z", "คุย", "กับ", "xyz", "demo.com" });
        }

        /// @deprecated (3.1) for version back compat 
        [Test]
        [Obsolete("(3.1) for version back compat")]
        public virtual void TestReusableTokenStream30()
        {
            ThaiAnalyzer analyzer = new ThaiAnalyzer(LuceneVersion.LUCENE_30);
            AssertAnalyzesTo(analyzer, "", new string[] { });

            AssertAnalyzesTo(analyzer, "การที่ได้ต้องแสดงว่างานดี", new string[] { "การ", "ที่", "ได้", "ต้อง", "แสดง", "ว่า", "งาน", "ดี" });

            AssertAnalyzesTo(analyzer, "บริษัทชื่อ XY&Z - คุยกับ xyz@demo.com", new string[] { "บริษัท", "ชื่อ", "xy&z", "คุย", "กับ", "xyz@demo.com" });
        }

        [Test]
        [LuceneNetSpecific]
        public virtual void TestConcurrency()
        {
            ThaiAnalyzer analyzer = new ThaiAnalyzer(TEST_VERSION_CURRENT);

            char[] chars = new char[] {
                (char)4160,
                (char)4124,
                (char)4097,
                (char)4177,
                (char)4113,
                (char)32,
                (char)10671,
            };
            string contents = new string(chars);
            AssertAnalyzer(analyzer, contents);

            int numThreads = 4;
            var startingGun = new CountdownEvent(1);
            var threads = new ThaiAnalyzerThread[numThreads];
            for (int i = 0; i < threads.Length; i++)
            {
                threads[i] = new ThaiAnalyzerThread(startingGun, analyzer, contents);
            }

            foreach (var thread in threads)
            {
                thread.Start();
            }

            startingGun.Signal();
            foreach (var t in threads)
            {
                try
                {
                    t.Join();
                }
                catch (Exception e) when (e.IsInterruptedException())
                {
                    fail("Thread interrupted");
                }
            }
        }

        private class ThaiAnalyzerThread : ThreadJob
        {
            private readonly CountdownEvent latch;
            private readonly Analyzer analyzer;
            private readonly string text;

            public ThaiAnalyzerThread(CountdownEvent latch, Analyzer analyzer, string text)
            {
                this.latch = latch;
                this.analyzer = analyzer;
                this.text = text;
            }

            public override void Run()
            {
                latch.Wait();

                for (int i = 0; i < 1000; i++)
                {
                    AssertAnalyzer(analyzer, text);
                }
            }
        }

        private static void AssertAnalyzer(Analyzer analyzer, string text)
        {
            ICharTermAttribute termAtt;
            IOffsetAttribute offsetAtt;
            IPositionIncrementAttribute posIncAtt;

            JCG.List<string> tokens = new JCG.List<string>();
            JCG.List<int> positions = new JCG.List<int>();
            JCG.List<int> startOffsets = new JCG.List<int>();
            JCG.List<int> endOffsets = new JCG.List<int>();

            TokenStream ts;
            TextReader reader = new StringReader(text);

            using (ts = analyzer.GetTokenStream("dummy", reader))
            {
                bool isReset = false;
                try
                {

                    termAtt = ts.GetAttribute<ICharTermAttribute>();
                    offsetAtt = ts.GetAttribute<IOffsetAttribute>();
                    posIncAtt = ts.GetAttribute<IPositionIncrementAttribute>();

                    ts.Reset();
                    isReset = true;

                    while (ts.IncrementToken())
                    {
                        Assert.IsNotNull(termAtt, "has no CharTermAttribute");
                        tokens.Add(termAtt.ToString());
                        positions.Add(posIncAtt.PositionIncrement);
                        startOffsets.Add(offsetAtt.StartOffset);
                        endOffsets.Add(offsetAtt.EndOffset);
                    }
                }
                finally
                {
                    if (!isReset)
                    {
                        try
                        {
                            // consume correctly
                            ts.Reset();
                            while (ts.IncrementToken()) ;
                            //ts.End();
                            //ts.Dispose();
                        }
#pragma warning disable 168
                        catch (Exception ex)
#pragma warning restore 168
                        {
                            // ignore
                        }
                    }
                    ts.End(); // ts.end();
                }
            } // ts.Dispose()

            reader = new StringReader(text);
            using (ts = analyzer.GetTokenStream("dummy", reader))
            {
                bool isReset = false;
                try
                {

                    // offset + pos
                    AssertTokenStreamContents(ts,
                        output: tokens.ToArray(),
                        startOffsets: ToIntArray(startOffsets),
                        endOffsets: ToIntArray(endOffsets),
                        types: null,
                        posIncrements: ToIntArray(positions),
                        posLengths: null,
                        finalOffset: text.Length,
                        offsetsAreCorrect: true);

                    isReset = true;
                }
                finally
                {
                    if (!isReset)
                    {
                        try
                        {
                            // consume correctly
                            ts.Reset();
                            while (ts.IncrementToken()) ;
                            //ts.End();
                            //ts.Dispose();
                        }
#pragma warning disable 168
                        catch (Exception ex)
#pragma warning restore 168
                        {
                            // ignore
                        }
                    }
                    ts.End(); // ts.end();
                }
            }

        } // ts.Dispose()


        /// <summary>
        /// blast some random strings through the analyzer </summary>
        [Test]
        [AwaitsFix(BugUrl = "https://github.com/apache/lucenenet/issues/269")] // LUCENENET TODO: this test occasionally fails
        public virtual void TestRandomStrings()
        {
            CheckRandomData(Random, new ThaiAnalyzer(TEST_VERSION_CURRENT), 1000 * RandomMultiplier);
        }

        /// <summary>
        /// blast some random large strings through the analyzer </summary>
        /// 
        [Test]
        [AwaitsFix(BugUrl = "https://github.com/apache/lucenenet/issues/269")] // LUCENENET TODO: this test occasionally fails
        public virtual void TestRandomHugeStrings()
        {
            Random random = Random;
            CheckRandomData(random, new ThaiAnalyzer(TEST_VERSION_CURRENT), 100 * RandomMultiplier, 8192);
        }

        // LUCENE-3044
        [Test]
        public virtual void TestAttributeReuse()
        {
#pragma warning disable 612, 618
            ThaiAnalyzer analyzer = new ThaiAnalyzer(LuceneVersion.LUCENE_30);
#pragma warning restore 612, 618
            // just consume
            TokenStream ts = analyzer.GetTokenStream("dummy", "ภาษาไทย");
            AssertTokenStreamContents(ts, new string[] { "ภาษา", "ไทย" });
            // this consumer adds flagsAtt, which this analyzer does not use. 
            ts = analyzer.GetTokenStream("dummy", "ภาษาไทย");
            ts.AddAttribute<IFlagsAttribute>();
            AssertTokenStreamContents(ts, new string[] { "ภาษา", "ไทย" });
        }

        [Test]
        public virtual void TestTwoSentences()
        {
            AssertAnalyzesTo(new ThaiAnalyzer(TEST_VERSION_CURRENT, CharArraySet.Empty), "This is a test. การที่ได้ต้องแสดงว่างานดี", new string[] { "this", "is", "a", "test", "การ", "ที่", "ได้", "ต้อง", "แสดง", "ว่า", "งาน", "ดี" }, new int[] { 0, 5, 8, 10, 16, 19, 22, 25, 29, 33, 36, 39 }, new int[] { 4, 7, 9, 14, 19, 22, 25, 29, 33, 36, 39, 41 });
        }

        /// <summary>
        /// LUCENENET: Tests scenario outlined in <see cref="ThaiWordBreaker"/>
        /// </summary>
        [Test][LuceneNetSpecific]
        public virtual void TestNumeralBreaking()
        {
            ThaiAnalyzer analyzer = new ThaiAnalyzer(TEST_VERSION_CURRENT, CharArraySet.Empty);
            AssertAnalyzesTo(analyzer, "๑๒๓456", new String[] { "๑๒๓456" });
        }
    }
}
#endif