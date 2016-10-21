using Lucene.Net.Documents;
using Lucene.Net.Index;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Lucene.Net.Analysis
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

    using Lucene.Net.Analysis.Tokenattributes;
    using Lucene.Net.Randomized.Generators;
    using Lucene.Net.Support;
    using System.Globalization;
    using System.IO;
    using Attribute = Lucene.Net.Util.Attribute;
    using Directory = Lucene.Net.Store.Directory;
    using Document = Documents.Document;
    using Field = Field;
    using FieldType = FieldType;
    using IAttribute = Lucene.Net.Util.IAttribute;
    using IOUtils = Lucene.Net.Util.IOUtils;
    using LineFileDocs = Lucene.Net.Util.LineFileDocs;
    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
    using RandomIndexWriter = Lucene.Net.Index.RandomIndexWriter;
    using TestUtil = Lucene.Net.Util.TestUtil;
    using TextField = TextField;

    /// <summary>
    /// base class for all Lucene unit tests that use TokenStreams.
    /// <p>
    /// When writing unit tests for analysis components, its highly recommended
    /// to use the helper methods here (especially in conjunction with <seealso cref="MockAnalyzer"/> or
    /// <seealso cref="MockTokenizer"/>), as they contain many assertions and checks to
    /// catch bugs.
    /// </summary>
    /// <seealso cref= MockAnalyzer </seealso>
    /// <seealso cref= MockTokenizer </seealso>
    public abstract class BaseTokenStreamTestCase : LuceneTestCase
    {
        // some helpers to test Analyzers and TokenStreams:

        /// <summary>
        /// Attribute that records if it was cleared or not.  this is used
        /// for testing that ClearAttributes() was called correctly.
        /// </summary>
        public interface ICheckClearAttributesAttribute : IAttribute
        {
            bool AndResetClearCalled { get; }
        }

        /// <summary>
        /// Attribute that records if it was cleared or not.  this is used
        /// for testing that ClearAttributes() was called correctly.
        /// </summary>
        public sealed class CheckClearAttributesAttribute : Attribute, ICheckClearAttributesAttribute
        {
            internal bool ClearCalled = false;

            public bool AndResetClearCalled
            {
                get
                {
                    bool old = ClearCalled;
                    ClearCalled = false;
                    return old;
                }
            }

            public override void Clear()
            {
                ClearCalled = true;
            }

            public override bool Equals(object other)
            {
                return (other is CheckClearAttributesAttribute && ((CheckClearAttributesAttribute)other).ClearCalled == this.ClearCalled);
            }

            public override int GetHashCode()
            {
                return 76137213 ^ Convert.ToBoolean(ClearCalled).GetHashCode();
            }

            public override void CopyTo(Attribute target)
            {
                ((CheckClearAttributesAttribute)target).Clear();
            }
        }

        // offsetsAreCorrect also validates:
        //   - graph offsets are correct (all tokens leaving from
        //     pos X have the same startOffset; all tokens
        //     arriving to pos Y have the same endOffset)
        //   - offsets only move forwards (startOffset >=
        //     lastStartOffset)
        public static void AssertTokenStreamContents(TokenStream ts, string[] output, int[] startOffsets, int[] endOffsets, string[] types, int[] posIncrements, int[] posLengths, int? finalOffset, int? finalPosInc, bool[] keywordAtts, bool offsetsAreCorrect)
        {
            // LUCENENET: Bug fix: NUnit throws an exception when something fails. 
            // This causes Dispose() to be skipped and it pollutes other tests indicating false negatives.
            // Added this try-finally block to fix this.
            try
            {

                Assert.IsNotNull(output);
                var checkClearAtt = ts.AddAttribute<ICheckClearAttributesAttribute>();

                ICharTermAttribute termAtt = null;
                if (output.Length > 0)
                {
                    Assert.IsTrue(ts.HasAttribute<ICharTermAttribute>(), "has no CharTermAttribute");
                    termAtt = ts.GetAttribute<ICharTermAttribute>();
                }

                IOffsetAttribute offsetAtt = null;
                if (startOffsets != null || endOffsets != null || finalOffset != null)
                {
                    Assert.IsTrue(ts.HasAttribute<IOffsetAttribute>(), "has no OffsetAttribute");
                    offsetAtt = ts.GetAttribute<IOffsetAttribute>();
                }

                ITypeAttribute typeAtt = null;
                if (types != null)
                {
                    Assert.IsTrue(ts.HasAttribute<ITypeAttribute>(), "has no TypeAttribute");
                    typeAtt = ts.GetAttribute<ITypeAttribute>();
                }

                IPositionIncrementAttribute posIncrAtt = null;
                if (posIncrements != null || finalPosInc != null)
                {
                    Assert.IsTrue(ts.HasAttribute<IPositionIncrementAttribute>(), "has no PositionIncrementAttribute");
                    posIncrAtt = ts.GetAttribute<IPositionIncrementAttribute>();
                }

                IPositionLengthAttribute posLengthAtt = null;
                if (posLengths != null)
                {
                    Assert.IsTrue(ts.HasAttribute<IPositionLengthAttribute>(), "has no PositionLengthAttribute");
                    posLengthAtt = ts.GetAttribute<IPositionLengthAttribute>();
                }

                IKeywordAttribute keywordAtt = null;
                if (keywordAtts != null)
                {
                    Assert.IsTrue(ts.HasAttribute<IKeywordAttribute>(), "has no KeywordAttribute");
                    keywordAtt = ts.GetAttribute<IKeywordAttribute>();
                }

                // Maps position to the start/end offset:
                IDictionary<int?, int?> posToStartOffset = new Dictionary<int?, int?>();
                IDictionary<int?, int?> posToEndOffset = new Dictionary<int?, int?>();

                ts.Reset();
                int pos = -1;
                int lastStartOffset = 0;
                for (int i = 0; i < output.Length; i++)
                {
                    // extra safety to enforce, that the state is not preserved and also assign bogus values
                    ts.ClearAttributes();
                    termAtt.SetEmpty().Append("bogusTerm");
                    if (offsetAtt != null)
                    {
                        offsetAtt.SetOffset(14584724, 24683243);
                    }
                    if (typeAtt != null)
                    {
                        typeAtt.Type = "bogusType";
                    }
                    if (posIncrAtt != null)
                    {
                        posIncrAtt.PositionIncrement = 45987657;
                    }
                    if (posLengthAtt != null)
                    {
                        posLengthAtt.PositionLength = 45987653;
                    }
                    if (keywordAtt != null)
                    {
                        keywordAtt.Keyword = (i & 1) == 0;
                    }

                    bool reset = checkClearAtt.AndResetClearCalled; // reset it, because we called clearAttribute() before
                    Assert.IsTrue(ts.IncrementToken(), "token " + i + " does not exist");
                    Assert.IsTrue(reset, "ClearAttributes() was not called correctly in TokenStream chain");

                    Assert.AreEqual(output[i], termAtt.ToString(), "term " + i + ", output[i] = " + output[i] + ", termAtt = " + termAtt.ToString());
                    if (startOffsets != null)
                    {
                        Assert.AreEqual(startOffsets[i], offsetAtt.StartOffset(), "startOffset " + i);
                    }
                    if (endOffsets != null)
                    {
                        Assert.AreEqual(endOffsets[i], offsetAtt.EndOffset(), "endOffset " + i);
                    }
                    if (types != null)
                    {
                        Assert.AreEqual(types[i], typeAtt.Type, "type " + i);
                    }
                    if (posIncrements != null)
                    {
                        Assert.AreEqual(posIncrements[i], posIncrAtt.PositionIncrement, "posIncrement " + i);
                    }
                    if (posLengths != null)
                    {
                        Assert.AreEqual(posLengths[i], posLengthAtt.PositionLength, "posLength " + i);
                    }
                    if (keywordAtts != null)
                    {
                        Assert.AreEqual(keywordAtts[i], keywordAtt.Keyword, "keywordAtt " + i);
                    }

                    // we can enforce some basic things about a few attributes even if the caller doesn't check:
                    if (offsetAtt != null)
                    {
                        int startOffset = offsetAtt.StartOffset();
                        int endOffset = offsetAtt.EndOffset();
                        if (finalOffset != null)
                        {
                            Assert.IsTrue(startOffset <= (int)finalOffset, "startOffset must be <= finalOffset");
                            Assert.IsTrue(endOffset <= (int)finalOffset, "endOffset must be <= finalOffset: got endOffset=" + endOffset + " vs finalOffset=" + (int)finalOffset);
                        }

                        if (offsetsAreCorrect)
                        {
                            Assert.IsTrue(offsetAtt.StartOffset() >= lastStartOffset, "offsets must not go backwards startOffset=" + startOffset + " is < lastStartOffset=" + lastStartOffset);
                            lastStartOffset = offsetAtt.StartOffset();
                        }

                        if (offsetsAreCorrect && posLengthAtt != null && posIncrAtt != null)
                        {
                            // Validate offset consistency in the graph, ie
                            // all tokens leaving from a certain pos have the
                            // same startOffset, and all tokens arriving to a
                            // certain pos have the same endOffset:
                            int posInc = posIncrAtt.PositionIncrement;
                            pos += posInc;

                            int posLength = posLengthAtt.PositionLength;

                            if (!posToStartOffset.ContainsKey(pos))
                            {
                                // First time we've seen a token leaving from this position:
                                posToStartOffset[pos] = startOffset;
                                //System.out.println("  + s " + pos + " -> " + startOffset);
                            }
                            else
                            {
                                // We've seen a token leaving from this position
                                // before; verify the startOffset is the same:
                                //System.out.println("  + vs " + pos + " -> " + startOffset);
                                Assert.AreEqual((int)posToStartOffset[pos], startOffset, "pos=" + pos + " posLen=" + posLength + " token=" + termAtt);
                            }

                            int endPos = pos + posLength;

                            if (!posToEndOffset.ContainsKey(endPos))
                            {
                                // First time we've seen a token arriving to this position:
                                posToEndOffset[endPos] = endOffset;
                                //System.out.println("  + e " + endPos + " -> " + endOffset);
                            }
                            else
                            {
                                // We've seen a token arriving to this position
                                // before; verify the endOffset is the same:
                                //System.out.println("  + ve " + endPos + " -> " + endOffset);
                                Assert.AreEqual((int)posToEndOffset[endPos], endOffset, "pos=" + pos + " posLen=" + posLength + " token=" + termAtt);
                            }
                        }
                    }
                    if (posIncrAtt != null)
                    {
                        if (i == 0)
                        {
                            Assert.IsTrue(posIncrAtt.PositionIncrement >= 1, "first posIncrement must be >= 1");
                        }
                        else
                        {
                            Assert.IsTrue(posIncrAtt.PositionIncrement >= 0, "posIncrement must be >= 0");
                        }
                    }
                    if (posLengthAtt != null)
                    {
                        Assert.IsTrue(posLengthAtt.PositionLength >= 1, "posLength must be >= 1");
                    }
                }

                if (ts.IncrementToken())
                {
                    Assert.Fail("TokenStream has more tokens than expected (expected count=" + output.Length + "); extra token=" + termAtt);
                }

                // repeat our extra safety checks for End()
                ts.ClearAttributes();
                if (termAtt != null)
                {
                    termAtt.SetEmpty().Append("bogusTerm");
                }
                if (offsetAtt != null)
                {
                    offsetAtt.SetOffset(14584724, 24683243);
                }
                if (typeAtt != null)
                {
                    typeAtt.Type = "bogusType";
                }
                if (posIncrAtt != null)
                {
                    posIncrAtt.PositionIncrement = 45987657;
                }
                if (posLengthAtt != null)
                {
                    posLengthAtt.PositionLength = 45987653;
                }

                var reset_ = checkClearAtt.AndResetClearCalled; // reset it, because we called clearAttribute() before

                ts.End();
                Assert.IsTrue(checkClearAtt.AndResetClearCalled, "super.End()/ClearAttributes() was not called correctly in End()");

                if (finalOffset != null)
                {
                    Assert.AreEqual((int)finalOffset, offsetAtt.EndOffset(), "finalOffset");
                }
                if (offsetAtt != null)
                {
                    Assert.IsTrue(offsetAtt.EndOffset() >= 0, "finalOffset must be >= 0");
                }
                if (finalPosInc != null)
                {
                    Assert.AreEqual((int)finalPosInc, posIncrAtt.PositionIncrement, "finalPosInc");
                }

                ts.Dispose();
            }
            catch (Exception)
            {
                //ts.Reset();
                ts.ClearAttributes();
                ts.End();
                ts.Dispose();
                throw;
            }
        }

        public static void AssertTokenStreamContents(TokenStream ts, string[] output, int[] startOffsets, int[] endOffsets, string[] types, int[] posIncrements, int[] posLengths, int? finalOffset, bool[] keywordAtts, bool offsetsAreCorrect)
        {
            AssertTokenStreamContents(ts, output, startOffsets, endOffsets, types, posIncrements, posLengths, finalOffset, null, null, offsetsAreCorrect);
        }

        public static void AssertTokenStreamContents(TokenStream ts, string[] output, int[] startOffsets, int[] endOffsets, string[] types, int[] posIncrements, int[] posLengths, int? finalOffset, bool offsetsAreCorrect)
        {
            AssertTokenStreamContents(ts, output, startOffsets, endOffsets, types, posIncrements, posLengths, finalOffset, null, offsetsAreCorrect);
        }

        public static void AssertTokenStreamContents(TokenStream ts, string[] output, int[] startOffsets, int[] endOffsets, string[] types, int[] posIncrements, int[] posLengths, int? finalOffset)
        {
            AssertTokenStreamContents(ts, output, startOffsets, endOffsets, types, posIncrements, posLengths, finalOffset, true);
        }

        public static void AssertTokenStreamContents(TokenStream ts, string[] output, int[] startOffsets, int[] endOffsets, string[] types, int[] posIncrements, int? finalOffset)
        {
            AssertTokenStreamContents(ts, output, startOffsets, endOffsets, types, posIncrements, null, finalOffset);
        }

        public static void AssertTokenStreamContents(TokenStream ts, string[] output, int[] startOffsets, int[] endOffsets, string[] types, int[] posIncrements)
        {
            AssertTokenStreamContents(ts, output, startOffsets, endOffsets, types, posIncrements, null, null);
        }

        public static void AssertTokenStreamContents(TokenStream ts, string[] output)
        {
            AssertTokenStreamContents(ts, output, null, null, null, null, null, null);
        }

        public static void AssertTokenStreamContents(TokenStream ts, string[] output, string[] types)
        {
            AssertTokenStreamContents(ts, output, null, null, types, null, null, null);
        }

        public static void AssertTokenStreamContents(TokenStream ts, string[] output, int[] posIncrements)
        {
            AssertTokenStreamContents(ts, output, null, null, null, posIncrements, null, null);
        }

        public static void AssertTokenStreamContents(TokenStream ts, string[] output, int[] startOffsets, int[] endOffsets)
        {
            AssertTokenStreamContents(ts, output, startOffsets, endOffsets, null, null, null, null);
        }

        public static void AssertTokenStreamContents(TokenStream ts, string[] output, int[] startOffsets, int[] endOffsets, int? finalOffset)
        {
            AssertTokenStreamContents(ts, output, startOffsets, endOffsets, null, null, null, finalOffset);
        }

        public static void AssertTokenStreamContents(TokenStream ts, string[] output, int[] startOffsets, int[] endOffsets, int[] posIncrements)
        {
            AssertTokenStreamContents(ts, output, startOffsets, endOffsets, null, posIncrements, null, null);
        }

        public static void AssertTokenStreamContents(TokenStream ts, string[] output, int[] startOffsets, int[] endOffsets, int[] posIncrements, int? finalOffset)
        {
            AssertTokenStreamContents(ts, output, startOffsets, endOffsets, null, posIncrements, null, finalOffset);
        }

        public static void AssertTokenStreamContents(TokenStream ts, string[] output, int[] startOffsets, int[] endOffsets, int[] posIncrements, int[] posLengths, int? finalOffset)
        {
            AssertTokenStreamContents(ts, output, startOffsets, endOffsets, null, posIncrements, posLengths, finalOffset);
        }

        public static void AssertAnalyzesTo(Analyzer a, string input, string[] output, int[] startOffsets, int[] endOffsets, string[] types, int[] posIncrements)
        {
            CheckResetException(a, input);
            AssertTokenStreamContents(a.TokenStream("dummy", new StringReader(input)), output, startOffsets, endOffsets, types, posIncrements, null, input.Length);
        }

        public static void AssertAnalyzesTo(Analyzer a, string input, string[] output, int[] startOffsets, int[] endOffsets, string[] types, int[] posIncrements, int[] posLengths)
        {
            CheckResetException(a, input);
            AssertTokenStreamContents(a.TokenStream("dummy", new StringReader(input)), output, startOffsets, endOffsets, types, posIncrements, posLengths, input.Length);
        }

        public static void AssertAnalyzesTo(Analyzer a, string input, string[] output, int[] startOffsets, int[] endOffsets, string[] types, int[] posIncrements, int[] posLengths, bool offsetsAreCorrect)
        {
            CheckResetException(a, input);
            AssertTokenStreamContents(a.TokenStream("dummy", new StringReader(input)), output, startOffsets, endOffsets, types, posIncrements, posLengths, input.Length, offsetsAreCorrect);
        }

        public static void AssertAnalyzesTo(Analyzer a, string input, string[] output)
        {
            AssertAnalyzesTo(a, input, output, null, null, null, null, null);
        }

        public static void AssertAnalyzesTo(Analyzer a, string input, string[] output, string[] types)
        {
            AssertAnalyzesTo(a, input, output, null, null, types, null, null);
        }

        public static void AssertAnalyzesTo(Analyzer a, string input, string[] output, int[] posIncrements)
        {
            AssertAnalyzesTo(a, input, output, null, null, null, posIncrements, null);
        }

        public static void AssertAnalyzesToPositions(Analyzer a, string input, string[] output, int[] posIncrements, int[] posLengths)
        {
            AssertAnalyzesTo(a, input, output, null, null, null, posIncrements, posLengths);
        }

        public static void AssertAnalyzesTo(Analyzer a, string input, string[] output, int[] startOffsets, int[] endOffsets)
        {
            AssertAnalyzesTo(a, input, output, startOffsets, endOffsets, null, null, null);
        }

        public static void AssertAnalyzesTo(Analyzer a, string input, string[] output, int[] startOffsets, int[] endOffsets, int[] posIncrements)
        {
            AssertAnalyzesTo(a, input, output, startOffsets, endOffsets, null, posIncrements, null);
        }

        internal static void CheckResetException(Analyzer a, string input)
        {
            TokenStream ts = a.TokenStream("bogus", new StringReader(input));
            try
            {
                if (ts.IncrementToken())
                {
                    ts.ReflectAsString(false);
                    Assert.Fail("didn't get expected exception when reset() not called");
                }
            }
            catch (InvalidOperationException expected)
            {
                //ok
            }
            catch (AssertionException expected)
            {
                // ok: MockTokenizer
                Assert.IsTrue(expected.Message != null && expected.Message.Contains("wrong state"), expected.Message);
            }
            catch (Exception unexpected)
            {
                //unexpected.printStackTrace(System.err);
                Console.Error.WriteLine(unexpected.StackTrace);
                Assert.Fail("got wrong exception when reset() not called: " + unexpected);
            }
            finally
            {
                // consume correctly
                ts.Reset();
                while (ts.IncrementToken())
                {
                }
                ts.End();
                ts.Dispose();
            }

            // check for a missing Close()
            ts = a.TokenStream("bogus", new StringReader(input));
            ts.Reset();
            while (ts.IncrementToken())
            {
            }
            ts.End();
            try
            {
                ts = a.TokenStream("bogus", new StringReader(input));
                Assert.Fail("didn't get expected exception when Close() not called");
            }
            catch (Exception)
            {
                // ok
            }
            finally
            {
                ts.Dispose();
            }
        }

        // simple utility method for testing stemmers

        public static void CheckOneTerm(Analyzer a, string input, string expected)
        {
            AssertAnalyzesTo(a, input, new string[] { expected });
        }

        /// <summary>
        /// utility method for blasting tokenstreams with data to make sure they don't do anything crazy
        ///
        /// LUCENENET specific
        /// Non-static to reduce the inter-class dependencies due to use of
        /// static variables
        /// </summary>
        public void CheckRandomData(Random random, Analyzer a, int iterations)
        {
            CheckRandomData(random, a, iterations, 20, false, true);
        }

        /// <summary>
        /// utility method for blasting tokenstreams with data to make sure they don't do anything crazy
        ///
        /// LUCENENET specific
        /// Non-static to reduce the inter-class dependencies due to use of
        /// static variables
        /// </summary>
        public void CheckRandomData(Random random, Analyzer a, int iterations, int maxWordLength)
        {
            CheckRandomData(random, a, iterations, maxWordLength, false, true);
        }

        /// <summary>
        /// utility method for blasting tokenstreams with data to make sure they don't do anything crazy
        /// 
        /// LUCENENET specific
        /// Non-static to reduce the inter-class dependencies due to use of
        /// static variables
        /// </summary>
        /// <param name="simple"> true if only ascii strings will be used (try to avoid)</param>
        public void CheckRandomData(Random random, Analyzer a, int iterations, bool simple)
        {
            CheckRandomData(random, a, iterations, 20, simple, true);
        }

        internal class AnalysisThread : ThreadClass
        {
            private readonly BaseTokenStreamTestCase OuterInstance;
            internal readonly int Iterations;
            internal readonly int MaxWordLength;
            internal readonly long Seed;
            internal readonly Analyzer a;
            internal readonly bool UseCharFilter;
            internal readonly bool Simple;
            internal readonly bool OffsetsAreCorrect;
            internal readonly RandomIndexWriter Iw;
            private readonly CountdownEvent _latch;

            // NOTE: not volatile because we don't want the tests to
            // add memory barriers (ie alter how threads
            // interact)... so this is just "best effort":
            public bool Failed;

            /// <summary>
            /// <param name="outerInstance">
            /// LUCENENET specific
            /// Added to remove a call to the then-static BaseTokenStreamTestCase methods</param>
            /// </summary>
            internal AnalysisThread(long seed, /*CountdownEvent latch,*/ Analyzer a, int iterations, int maxWordLength, 
                bool useCharFilter, bool simple, bool offsetsAreCorrect, RandomIndexWriter iw, BaseTokenStreamTestCase outerInstance)
            {
                this.Seed = seed;
                this.a = a;
                this.Iterations = iterations;
                this.MaxWordLength = maxWordLength;
                this.UseCharFilter = useCharFilter;
                this.Simple = simple;
                this.OffsetsAreCorrect = offsetsAreCorrect;
                this.Iw = iw;
                this._latch = null;
                this.OuterInstance = outerInstance;
            }

            public override void Run()
            {
                bool success = false;
                try
                {
                    if (_latch != null) _latch.Wait();
                    // see the part in checkRandomData where it replays the same text again
                    // to verify reproducability/reuse: hopefully this would catch thread hazards.
                    OuterInstance.CheckRandomData(new Random((int)Seed), a, Iterations, MaxWordLength, UseCharFilter, Simple, OffsetsAreCorrect, Iw);
                    success = true;
                }
                catch (Exception e)
                {
                    Console.WriteLine("Exception in Thread: " + e);
                    throw;
                }
                finally
                {
                    Failed = !success;
                }
            }
        }

        public void CheckRandomData(Random random, Analyzer a, int iterations, int maxWordLength, bool simple)
        {
            CheckRandomData(random, a, iterations, maxWordLength, simple, true);
        }

        public void CheckRandomData(Random random, Analyzer a, int iterations, int maxWordLength, bool simple, bool offsetsAreCorrect)
        {
            CheckResetException(a, "best effort");
            long seed = random.Next();
            bool useCharFilter = random.NextBoolean();
            Directory dir = null;
            RandomIndexWriter iw = null;
            string postingsFormat = TestUtil.GetPostingsFormat("dummy");
            bool codecOk = iterations * maxWordLength < 100000
                || !(postingsFormat.Equals("Memory") || postingsFormat.Equals("SimpleText"));
            if (Rarely(random) && codecOk)
            {
                dir = NewFSDirectory(CreateTempDir("bttc"));
                iw = new RandomIndexWriter(new Random((int)seed), dir, a, ClassEnvRule.Similarity, ClassEnvRule.TimeZone);
            }

            bool success = false;
            try
            {
                CheckRandomData(new Random((int)seed), a, iterations, maxWordLength, useCharFilter, simple, offsetsAreCorrect, iw);
                // now test with multiple threads: note we do the EXACT same thing we did before in each thread,
                // so this should only really fail from another thread if its an actual thread problem
                int numThreads = TestUtil.NextInt(random, 2, 4);
                var startingGun = new CountdownEvent(1);
                var threads = new AnalysisThread[numThreads];
                for (int i = 0; i < threads.Length; i++)
                {
                    threads[i] = new AnalysisThread(seed, /*startingGun,*/ a, iterations, maxWordLength, useCharFilter, simple, offsetsAreCorrect, iw, this);
                }
                
                foreach (AnalysisThread thread in threads)
                {
                    thread.Start();
                }

                startingGun.Signal();
                foreach (var t in threads)
                {
#if !NETSTANDARD
                    try
                    {
#endif
                        t.Join();
#if !NETSTANDARD
                    }
                    catch (ThreadInterruptedException e)
                    {
                        Fail("Thread interrupted");
                    }
#endif
                }

                if (threads.Any(x => x.Failed))
                    Fail("Thread threw exception");

                success = true;
            }
            finally
            {
                if (success)
                {
                    IOUtils.Close(iw, dir);
                }
                else
                {
                    IOUtils.CloseWhileHandlingException(iw, dir); // checkindex
                }
            }
        }

        private void CheckRandomData(Random random, Analyzer a, int iterations, int maxWordLength, bool useCharFilter, bool simple, bool offsetsAreCorrect, RandomIndexWriter iw)
        {
            LineFileDocs docs = new LineFileDocs(random);
            Document doc = null;
            Field field = null, currentField = null;
            StringReader bogus = new StringReader("");
            if (iw != null)
            {
                doc = new Document();
                FieldType ft = new FieldType(TextField.TYPE_NOT_STORED);
                if (random.NextBoolean())
                {
                    ft.StoreTermVectors = true;
                    ft.StoreTermVectorOffsets = random.NextBoolean();
                    ft.StoreTermVectorPositions = random.NextBoolean();
                    if (ft.StoreTermVectorPositions && !OLD_FORMAT_IMPERSONATION_IS_ACTIVE)
                    {
                        ft.StoreTermVectorPayloads = random.NextBoolean();
                    }
                }
                if (random.NextBoolean())
                {
                    ft.OmitNorms = true;
                }
                string pf = TestUtil.GetPostingsFormat("dummy");
                bool supportsOffsets = !DoesntSupportOffsets.Contains(pf);
                switch (random.Next(4))
                {
                    case 0:
                        ft.IndexOptions = FieldInfo.IndexOptions.DOCS_ONLY;
                        break;

                    case 1:
                        ft.IndexOptions = FieldInfo.IndexOptions.DOCS_AND_FREQS;
                        break;

                    case 2:
                        ft.IndexOptions = FieldInfo.IndexOptions.DOCS_AND_FREQS_AND_POSITIONS;
                        break;

                    default:
                        if (supportsOffsets && offsetsAreCorrect)
                        {
                            ft.IndexOptions = FieldInfo.IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS;
                        }
                        else
                        {
                            ft.IndexOptions = FieldInfo.IndexOptions.DOCS_AND_FREQS_AND_POSITIONS;
                        }
                        break;
                }
                currentField = field = new Field("dummy", bogus, ft);
                doc.Add(currentField);
            }

            try
            {
                for (int i = 0; i < iterations; i++)
                {
                    string text;

                    if (random.Next(10) == 7)
                    {
                        // real data from linedocs
                        text = docs.NextDoc().Get("body");
                        if (text.Length > maxWordLength)
                        {
                            // Take a random slice from the text...:
                            int startPos = random.Next(text.Length - maxWordLength);
                            if (startPos > 0 && char.IsLowSurrogate(text[startPos]))
                            {
                                // Take care not to split up a surrogate pair:
                                startPos--;
                                Assert.True(char.IsHighSurrogate(text[startPos]));
                            }
                            int endPos = startPos + maxWordLength - 1;
                            if (char.IsHighSurrogate(text[endPos]))
                            {
                                // Take care not to split up a surrogate pair:
                                endPos--;
                            }
                            text = text.Substring(startPos, 1 + endPos - startPos);
                        }
                    }
                    else
                    {
                        // synthetic
                        text = TestUtil.RandomAnalysisString(random, maxWordLength, simple);
                    }

                    try
                    {
                        CheckAnalysisConsistency(random, a, useCharFilter, text, offsetsAreCorrect, currentField);
                        if (iw != null)
                        {
                            if (random.Next(7) == 0)
                            {
                                // pile up a multivalued field
                                var ft = (FieldType)field.FieldType;
                                currentField = new Field("dummy", bogus, ft);
                                doc.Add(currentField);
                            }
                            else
                            {
                                iw.AddDocument(doc);
                                if (doc.Fields.Count > 1)
                                {
                                    // back to 1 field
                                    currentField = field;
                                    doc.RemoveFields("dummy");
                                    doc.Add(currentField);
                                }
                            }
                        }
                    }
                    catch (Exception t)
                    {
                        // TODO: really we should pass a random seed to
                        // checkAnalysisConsistency then print it here too:
                        Console.Error.WriteLine("TEST FAIL: useCharFilter=" + useCharFilter + " text='" + Escape(text) + "'");
                        throw;
                    }
                }
            }
            finally
            {
                IOUtils.CloseWhileHandlingException(docs);
            }
        }

        public static string Escape(string s)
        {
            int charUpto = 0;
            StringBuilder sb = new StringBuilder();
            while (charUpto < s.Length)
            {
                int c = s[charUpto];
                if (c == 0xa)
                {
                    // Strangely, you cannot put \ u000A into Java
                    // sources (not in a comment nor a string
                    // constant)...:
                    sb.Append("\\n");
                }
                else if (c == 0xd)
                {
                    // ... nor \ u000D:
                    sb.Append("\\r");
                }
                else if (c == '"')
                {
                    sb.Append("\\\"");
                }
                else if (c == '\\')
                {
                    sb.Append("\\\\");
                }
                else if (c >= 0x20 && c < 0x80)
                {
                    sb.Append((char)c);
                }
                else
                {
                    // TODO: we can make ascii easier to read if we
                    // don't escape...
                    sb.AppendFormat(CultureInfo.InvariantCulture, "\\u{0:x4}", c);
                }
                charUpto++;
            }
            return sb.ToString();
        }

        public static void CheckAnalysisConsistency(Random random, Analyzer a, bool useCharFilter, string text)
        {
            CheckAnalysisConsistency(random, a, useCharFilter, text, true);
        }

        public static void CheckAnalysisConsistency(Random random, Analyzer a, bool useCharFilter, string text, bool offsetsAreCorrect)
        {
            CheckAnalysisConsistency(random, a, useCharFilter, text, offsetsAreCorrect, null);
        }

        private static void CheckAnalysisConsistency(Random random, Analyzer a, bool useCharFilter, string text, bool offsetsAreCorrect, Field field)
        {
            if (VERBOSE)
            {
                Console.WriteLine(Thread.CurrentThread.Name + ": NOTE: baseTokenStreamTestCase: get first token stream now text=" + text);
            }

            ICharTermAttribute termAtt;
            IOffsetAttribute offsetAtt;
            IPositionIncrementAttribute posIncAtt;
            IPositionLengthAttribute posLengthAtt;
            ITypeAttribute typeAtt;

            IList<string> tokens = new List<string>();
            IList<string> types = new List<string>();
            IList<int> positions = new List<int>();
            IList<int> positionLengths = new List<int>();
            IList<int> startOffsets = new List<int>();
            IList<int> endOffsets = new List<int>();

            int remainder = random.Next(10);
            StringReader reader = new StringReader(text);

            TokenStream ts;
            using (ts = a.TokenStream("dummy", useCharFilter ? (TextReader) new MockCharFilter(reader, remainder) : reader))
            {
                 termAtt = ts.HasAttribute<ICharTermAttribute>()
                    ? ts.GetAttribute<ICharTermAttribute>()
                    : null;
                offsetAtt = ts.HasAttribute<IOffsetAttribute>()
                    ? ts.GetAttribute<IOffsetAttribute>()
                    : null;
                posIncAtt = ts.HasAttribute<IPositionIncrementAttribute>()
                    ? ts.GetAttribute<IPositionIncrementAttribute>()
                    : null;
                posLengthAtt = ts.HasAttribute<IPositionLengthAttribute>()
                    ? ts.GetAttribute<IPositionLengthAttribute>()
                    : null;
                typeAtt = ts.HasAttribute<ITypeAttribute>() ? ts.GetAttribute<ITypeAttribute>() : null;

                ts.Reset();

                // First pass: save away "correct" tokens
                while (ts.IncrementToken())
                {
                    Assert.IsNotNull(termAtt, "has no CharTermAttribute");
                    tokens.Add(termAtt.ToString());
                    if (typeAtt != null)
                    {
                        types.Add(typeAtt.Type);
                    }
                    if (posIncAtt != null)
                    {
                        positions.Add(posIncAtt.PositionIncrement);
                    }
                    if (posLengthAtt != null)
                    {
                        positionLengths.Add(posLengthAtt.PositionLength);
                    }
                    if (offsetAtt != null)
                    {
                        startOffsets.Add(offsetAtt.StartOffset());
                        endOffsets.Add(offsetAtt.EndOffset());
                    }
                }
                ts.End();
            }

            // verify reusing is "reproducable" and also get the normal tokenstream sanity checks
            if (tokens.Count > 0)
            {
                // KWTokenizer (for example) can produce a token
                // even when input is length 0:
                if (text.Length != 0)
                {
                    // (Optional) second pass: do something evil:
                    int evilness = random.Next(50);
                    if (evilness == 17)
                    {
                        if (VERBOSE)
                        {
                            Console.WriteLine(Thread.CurrentThread.Name + ": NOTE: baseTokenStreamTestCase: re-run analysis w/ exception");
                        }
                        // Throw an errant exception from the Reader:

                        MockReaderWrapper evilReader = new MockReaderWrapper(random, text);
                        evilReader.ThrowExcAfterChar(random.Next(text.Length));
                        reader = evilReader;

                        try
                        {
                            // NOTE: some Tokenizers go and read characters
                            // when you call .setReader(Reader), eg
                            // PatternTokenizer.  this is a bit
                            // iffy... (really, they should only
                            // pull from the Reader when you call
                            // .incremenToken(), I think?), but we
                            // currently allow it, so, we must call
                            // a.TokenStream inside the try since we may
                            // hit the exc on init:
                            ts = a.TokenStream("dummy", useCharFilter ? (TextReader)new MockCharFilter(evilReader, remainder) : evilReader);
                            ts.Reset();
                            while (ts.IncrementToken()) ;
                            Assert.Fail("did not hit exception");
                        }
                        catch (Exception re)
                        {
                            Assert.IsTrue(MockReaderWrapper.IsMyEvilException(re));
                        }

                        try
                        {
                            ts.End();
                        }
                        catch (InvalidOperationException ae)
                        {
                            // Catch & ignore MockTokenizer's
                            // anger...
                            if ("End() called before IncrementToken() returned false!".Equals(ae.Message))
                            {
                                // OK
                            }
                            else
                            {
                                throw ae;
                            }
                        }
                        finally
                        {
                            ts.Dispose();
                        }
                    }
                    else if (evilness == 7)
                    {
                        // Only consume a subset of the tokens:
                        int numTokensToRead = random.Next(tokens.Count);
                        if (VERBOSE)
                        {
                            Console.WriteLine(Thread.CurrentThread.Name + ": NOTE: baseTokenStreamTestCase: re-run analysis, only consuming " + numTokensToRead + " of " + tokens.Count + " tokens");
                        }

                        reader = new StringReader(text);
                        ts = a.TokenStream("dummy", useCharFilter ? (TextReader)new MockCharFilter(reader, remainder) : reader);
                        ts.Reset();
                        for (int tokenCount = 0; tokenCount < numTokensToRead; tokenCount++)
                        {
                            Assert.IsTrue(ts.IncrementToken());
                        }

                        try
                        {
                            ts.End();
                        }
                        catch (InvalidOperationException ae)
                        {
                            // Catch & ignore MockTokenizer's
                            // anger...
                            if ("End() called before IncrementToken() returned false!".Equals(ae.Message))
                            {
                                // OK
                            }
                            else
                            {
                                throw ae;
                            }
                        }
                        finally
                        {
                            ts.Dispose();
                        }
                    }
                }
            }

            // Final pass: verify clean tokenization matches
            // results from first pass:

            if (VERBOSE)
            {
                Console.WriteLine(Thread.CurrentThread.Name + ": NOTE: baseTokenStreamTestCase: re-run analysis; " + tokens.Count + " tokens");
            }
            reader = new StringReader(text);

            long seed = random.Next();
            random = new Random((int)seed);
            if (random.Next(30) == 7)
            {
                if (VERBOSE)
                {
                    Console.WriteLine(Thread.CurrentThread.Name + ": NOTE: baseTokenStreamTestCase: using spoon-feed reader");
                }

                reader = new MockReaderWrapper(random, text);
            }

            ts = a.TokenStream("dummy", useCharFilter ? (TextReader)new MockCharFilter(reader, remainder) : reader);
            if (typeAtt != null && posIncAtt != null && posLengthAtt != null && offsetAtt != null)
            {
                // offset + pos + posLength + type
                AssertTokenStreamContents(ts, tokens.ToArray(), ToIntArray(startOffsets), ToIntArray(endOffsets), types.ToArray(), ToIntArray(positions), ToIntArray(positionLengths), text.Length, offsetsAreCorrect);
            }
            else if (typeAtt != null && posIncAtt != null && offsetAtt != null)
            {
                // offset + pos + type
                AssertTokenStreamContents(ts, tokens.ToArray(), ToIntArray(startOffsets), ToIntArray(endOffsets), types.ToArray(), ToIntArray(positions), null, text.Length, offsetsAreCorrect);
            }
            else if (posIncAtt != null && posLengthAtt != null && offsetAtt != null)
            {
                // offset + pos + posLength
                AssertTokenStreamContents(ts, tokens.ToArray(), ToIntArray(startOffsets), ToIntArray(endOffsets), null, ToIntArray(positions), ToIntArray(positionLengths), text.Length, offsetsAreCorrect);
            }
            else if (posIncAtt != null && offsetAtt != null)
            {
                // offset + pos
                AssertTokenStreamContents(ts, tokens.ToArray(), ToIntArray(startOffsets), ToIntArray(endOffsets), null, ToIntArray(positions), null, text.Length, offsetsAreCorrect);
            }
            else if (offsetAtt != null)
            {
                // offset
                AssertTokenStreamContents(ts, tokens.ToArray(), ToIntArray(startOffsets), ToIntArray(endOffsets), null, null, null, text.Length, offsetsAreCorrect);
            }
            else
            {
                // terms only
                AssertTokenStreamContents(ts, tokens.ToArray());
            }

            if (field != null)
            {
                reader = new StringReader(text);
                random = new Random((int)seed);
                if (random.Next(30) == 7)
                {
                    if (VERBOSE)
                    {
                        Console.WriteLine(Thread.CurrentThread.Name + ": NOTE: baseTokenStreamTestCase: indexing using spoon-feed reader");
                    }

                    reader = new MockReaderWrapper(random, text);
                }

                field.ReaderValue = useCharFilter ? (TextReader)new MockCharFilter(reader, remainder) : reader;
            }
        }

        protected internal virtual string ToDot(Analyzer a, string inputText)
        {
            StringWriter sw = new StringWriter();
            TokenStream ts = a.TokenStream("field", new StringReader(inputText));
            ts.Reset();
            (new TokenStreamToDot(inputText, ts, /*new StreamWriter(*/(TextWriter)sw/*)*/)).ToDot();
            return sw.ToString();
        }

        protected internal virtual void ToDotFile(Analyzer a, string inputText, string localFileName)
        {
            using (StreamWriter w = new StreamWriter(new FileStream(localFileName, FileMode.Open), IOUtils.CHARSET_UTF_8))
            {
                TokenStream ts = a.TokenStream("field", new StreamReader(inputText));
                ts.Reset();
                (new TokenStreamToDot(inputText, ts,/* new PrintWriter(*/w/*)*/)).ToDot();    
            }
        }

        internal static int[] ToIntArray(IList<int> list)
        {
            int[] ret = new int[list.Count];
            int offset = 0;
            foreach (int i in list)
            {
                ret[offset++] = i;
            }
            return ret;
        }
    }
}