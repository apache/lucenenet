using J2N.Collections.Generic.Extensions;
using J2N.Threading;
using Lucene.Net.Analysis.TokenAttributes;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Support;
using Lucene.Net.Support.Threading;
using Lucene.Net.Util;
using RandomizedTesting.Generators;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using Assert = Lucene.Net.TestFramework.Assert;
using Attribute = Lucene.Net.Util.Attribute;
using AttributeFactory = Lucene.Net.Util.AttributeSource.AttributeFactory;
using Console = Lucene.Net.Util.SystemConsole;
using Directory = Lucene.Net.Store.Directory;
using JCG = J2N.Collections.Generic;

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

    /// <summary>
    /// Attribute that records if it was cleared or not.  this is used
    /// for testing that <see cref="Lucene.Net.Util.AttributeSource.ClearAttributes()"/> was called correctly.
    /// </summary>
    public interface ICheckClearAttributesAttribute : IAttribute
    {
        bool GetAndResetClearCalled();
    }

    /// <summary>
    /// Attribute that records if it was cleared or not.  this is used
    /// for testing that <see cref="Lucene.Net.Util.AttributeSource.ClearAttributes()"/> was called correctly.
    /// </summary>
    public sealed class CheckClearAttributesAttribute : Attribute, ICheckClearAttributesAttribute
    {
        private bool clearCalled = false;

        public bool GetAndResetClearCalled()
        {
            bool old = clearCalled;
            clearCalled = false;
            return old;
        }

        public override void Clear()
        {
            clearCalled = true;
        }

        public override bool Equals(object other)
        {
            return other is CheckClearAttributesAttribute checkClearAttributesAttribute
                && checkClearAttributesAttribute.clearCalled == this.clearCalled;
        }

        public override int GetHashCode()
        {
            return 76137213 ^ clearCalled.GetHashCode();
        }

        public override void CopyTo(IAttribute target) // LUCENENET specific - intentionally expanding target to use IAttribute rather than Attribute
        {
            // LUCENENET: Added guard clauses
            if (target is null)
                throw new ArgumentNullException(nameof(target));
            if (target is not CheckClearAttributesAttribute other)
                throw new ArgumentException($"Argument type {target.GetType().FullName} must subclass {nameof(CheckClearAttributesAttribute)}", nameof(target));
            other.Clear();
        }
    }

    /// <summary>
    /// Base class for all Lucene unit tests that use <see cref="TokenStream"/>s.
    /// <para/>
    /// When writing unit tests for analysis components, its highly recommended
    /// to use the helper methods here (especially in conjunction with <see cref="MockAnalyzer"/> or
    /// <see cref="MockTokenizer"/>), as they contain many assertions and checks to
    /// catch bugs.
    /// </summary>
    /// <seealso cref="MockAnalyzer"/>
    /// <seealso cref="MockTokenizer"/>
    // LUCENENET specific - Specify to unzip the line file docs
    [UseTempLineDocsFile]
    public abstract class BaseTokenStreamTestCase : LuceneTestCase
    {
        // some helpers to test Analyzers and TokenStreams:

        // LUCENENET specific - de-nested ICheckClearAttributesAttribute

        // LUCENENET specific - de-nested CheckClearAttributesAttribute

        // offsetsAreCorrect also validates:
        //   - graph offsets are correct (all tokens leaving from
        //     pos X have the same startOffset; all tokens
        //     arriving to pos Y have the same endOffset)
        //   - offsets only move forwards (startOffset >=
        //     lastStartOffset)
        public static void AssertTokenStreamContents(TokenStream ts, string[] output, int[] startOffsets, int[] endOffsets, string[] types, int[] posIncrements, int[] posLengths, int? finalOffset, int? finalPosInc, bool[] keywordAtts, bool offsetsAreCorrect, byte[][] payloads)
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
                    Assert.IsTrue(ts.HasAttribute<ICharTermAttribute>(), "has no ICharTermAttribute");
                    termAtt = ts.GetAttribute<ICharTermAttribute>();
                }

                IOffsetAttribute offsetAtt = null;
                if (startOffsets != null || endOffsets != null || finalOffset != null)
                {
                    Assert.IsTrue(ts.HasAttribute<IOffsetAttribute>(), "has no IOffsetAttribute");
                    offsetAtt = ts.GetAttribute<IOffsetAttribute>();
                }

                ITypeAttribute typeAtt = null;
                if (types != null)
                {
                    Assert.IsTrue(ts.HasAttribute<ITypeAttribute>(), "has no ITypeAttribute");
                    typeAtt = ts.GetAttribute<ITypeAttribute>();
                }

                IPositionIncrementAttribute posIncrAtt = null;
                if (posIncrements != null || finalPosInc != null)
                {
                    Assert.IsTrue(ts.HasAttribute<IPositionIncrementAttribute>(), "has no IPositionIncrementAttribute");
                    posIncrAtt = ts.GetAttribute<IPositionIncrementAttribute>();
                }

                IPositionLengthAttribute posLengthAtt = null;
                if (posLengths != null)
                {
                    Assert.IsTrue(ts.HasAttribute<IPositionLengthAttribute>(), "has no IPositionLengthAttribute");
                    posLengthAtt = ts.GetAttribute<IPositionLengthAttribute>();
                }

                IKeywordAttribute keywordAtt = null;
                if (keywordAtts != null)
                {
                    Assert.IsTrue(ts.HasAttribute<IKeywordAttribute>(), "has no IKeywordAttribute");
                    keywordAtt = ts.GetAttribute<IKeywordAttribute>();
                }

                // *********** From Lucene 8.2.0 **************

                IPayloadAttribute payloadAtt = null;
                if (payloads != null)
                {
                    Assert.IsTrue(ts.HasAttribute<IPayloadAttribute>(), "has no IPayloadAttribute");
                    payloadAtt = ts.GetAttribute<IPayloadAttribute>();
                }

                // *********** End From Lucene 8.2.0 **************

                // Maps position to the start/end offset:
                IDictionary<int, int> posToStartOffset = new Dictionary<int, int>();
                IDictionary<int, int> posToEndOffset = new Dictionary<int, int>();

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
                        keywordAtt.IsKeyword = (i & 1) == 0;
                    }
                    // *********** From Lucene 8.2.0 **************
                    if (payloadAtt != null)
                    {
                        payloadAtt.Payload = new BytesRef(new byte[] { 0x00, unchecked((byte)-0x21), 0x12, unchecked((byte)-0x43), 0x24 });
                    }
                    // *********** End From Lucene 8.2.0 **************

                    bool reset = checkClearAtt.GetAndResetClearCalled(); // reset it, because we called clearAttribute() before
                    Assert.IsTrue(ts.IncrementToken(), "token " + i + " does not exist");
                    Assert.IsTrue(reset, "ClearAttributes() was not called correctly in TokenStream chain");

                    Assert.AreEqual(output[i], termAtt.ToString(), "term " + i + ", output[i] = " + output[i] + ", termAtt = " + termAtt.ToString());
                    if (startOffsets != null)
                    {
                        Assert.AreEqual(startOffsets[i], offsetAtt.StartOffset, "startOffset " + i);
                    }
                    if (endOffsets != null)
                    {
                        Assert.AreEqual(endOffsets[i], offsetAtt.EndOffset, "endOffset " + i);
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
                        Assert.AreEqual(keywordAtts[i], keywordAtt.IsKeyword, "keywordAtt " + i);
                    }
                    // *********** From Lucene 8.2.0 **************
                    if (payloads != null)
                    {
                        if (payloads[i] != null)
                        {
                            Assert.AreEqual(new BytesRef(payloads[i]), payloadAtt.Payload, "payloads " + i);
                        }
                        else
                        {
                            Assert.IsNull(payloads[i], "payloads " + i);
                        }
                    }
                    // *********** End From Lucene 8.2.0 **************


                    // we can enforce some basic things about a few attributes even if the caller doesn't check:
                    if (offsetAtt != null)
                    {
                        int startOffset = offsetAtt.StartOffset;
                        int endOffset = offsetAtt.EndOffset;
                        if (finalOffset != null)
                        {
                            Assert.IsTrue(startOffset <= (int)finalOffset, "startOffset must be <= finalOffset");
                            Assert.IsTrue(endOffset <= (int)finalOffset, "endOffset must be <= finalOffset: got endOffset=" + endOffset + " vs finalOffset=" + (int)finalOffset);
                        }

                        if (offsetsAreCorrect)
                        {
                            Assert.IsTrue(offsetAtt.StartOffset >= lastStartOffset, "offsets must not go backwards startOffset=" + startOffset + " is < lastStartOffset=" + lastStartOffset);
                            lastStartOffset = offsetAtt.StartOffset;
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

                            if (!posToStartOffset.TryGetValue(pos, out int oldStartOffset))
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
                                Assert.AreEqual(oldStartOffset, startOffset, "pos=" + pos + " posLen=" + posLength + " token=" + termAtt);
                            }

                            int endPos = pos + posLength;

                            if (!posToEndOffset.TryGetValue(endPos, out int oldEndOffset))
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
                                Assert.AreEqual(oldEndOffset, endOffset, "pos=" + pos + " posLen=" + posLength + " token=" + termAtt);
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

                var reset_ = checkClearAtt.GetAndResetClearCalled(); // reset it, because we called clearAttribute() before

                ts.End();
                Assert.IsTrue(checkClearAtt.GetAndResetClearCalled(), "base.End()/ClearAttributes() was not called correctly in End()");

                if (finalOffset != null)
                {
                    Assert.AreEqual((int)finalOffset, offsetAtt.EndOffset, "finalOffset");
                }
                if (offsetAtt != null)
                {
                    Assert.IsTrue(offsetAtt.EndOffset >= 0, "finalOffset must be >= 0");
                }
                if (finalPosInc != null)
                {
                    Assert.AreEqual((int)finalPosInc, posIncrAtt.PositionIncrement, "finalPosInc");
                }

                //ts.Dispose();
            }
            catch (Exception)
            {
                //ts.Reset();
                ts.ClearAttributes();
                ts.End();
                throw; // LUCENENET: CA2200: Rethrow to preserve stack details (https://docs.microsoft.com/en-us/visualstudio/code-quality/ca2200-rethrow-to-preserve-stack-details)
            }
            finally
            {
                ts.Dispose();
            }
        }

#pragma warning disable IDE0060 // Remove unused parameter
        public static void AssertTokenStreamContents(TokenStream ts, string[] output, int[] startOffsets, int[] endOffsets, string[] types, int[] posIncrements, int[] posLengths, int? finalOffset, bool[] keywordAtts, bool offsetsAreCorrect)
#pragma warning restore IDE0060 // Remove unused parameter
        {
            AssertTokenStreamContents(ts, output, startOffsets, endOffsets, types, posIncrements, posLengths, finalOffset, null, null, offsetsAreCorrect, null);
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
            AssertTokenStreamContents(a.GetTokenStream("dummy", new StringReader(input)), output, startOffsets, endOffsets, types, posIncrements, null, input.Length);
        }

        public static void AssertAnalyzesTo(Analyzer a, string input, string[] output, int[] startOffsets, int[] endOffsets, string[] types, int[] posIncrements, int[] posLengths)
        {
            CheckResetException(a, input);
            AssertTokenStreamContents(a.GetTokenStream("dummy", new StringReader(input)), output, startOffsets, endOffsets, types, posIncrements, posLengths, input.Length);
        }

        public static void AssertAnalyzesTo(Analyzer a, string input, string[] output, int[] startOffsets, int[] endOffsets, string[] types, int[] posIncrements, int[] posLengths, bool offsetsAreCorrect)
        {
            CheckResetException(a, input);
            AssertTokenStreamContents(a.GetTokenStream("dummy", new StringReader(input)), output, startOffsets, endOffsets, types, posIncrements, posLengths, input.Length, offsetsAreCorrect);
        }

        // LUCENENET: Overload from Lucene 8.2.0
        public static void AssertAnalyzesTo(Analyzer a, string input, string[] output, int[] startOffsets, int[] endOffsets, string[] types, int[] posIncrements, int[] posLengths, bool graphOffsetsAreCorrect, byte[][] payloads)
        {
            CheckResetException(a, input);
            AssertTokenStreamContents(a.GetTokenStream("dummy", input), output, startOffsets, endOffsets, types, posIncrements, posLengths, input.Length, null, null, graphOffsetsAreCorrect, payloads);
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
            TokenStream ts = a.GetTokenStream("bogus", new StringReader(input));
            try
            {
                if (ts.IncrementToken())
                {
                    ts.ReflectAsString(false);
                    Assert.Fail("didn't get expected exception when reset() not called");
                }
            }
            catch (Exception expected) when (expected.IsIllegalStateException())
            {
                //ok
            }
            catch (Exception expected) when (expected.IsAssertionError())
            {
                // ok: MockTokenizer
                Assert.IsTrue(expected.Message != null && expected.Message.Contains("wrong state"), expected.Message);
            }
            catch (Exception unexpected) when (unexpected.IsException())
            {
                unexpected.printStackTrace(Console.Error);
                Assert.Fail("Got wrong exception when Reset() not called: " + unexpected);
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
            ts = a.GetTokenStream("bogus", new StringReader(input));
            ts.Reset();
            while (ts.IncrementToken())
            {
            }
            ts.End();
            try
            {
                ts = a.GetTokenStream("bogus", new StringReader(input));
                Assert.Fail("Didn't get expected exception when Dispose() not called");
            }
            catch (Exception expected) when (expected.IsIllegalStateException())
            {
                // ok
            }
            finally
            {
                ts.Dispose();
            }
        }

        /// <summary>
        /// Simple utility method for testing stemmers
        /// </summary>
        public static void CheckOneTerm(Analyzer a, string input, string expected)
        {
            AssertAnalyzesTo(a, input, new string[] { expected });
        }

        /// <summary>
        /// Utility method for blasting tokenstreams with data to make sure they don't do anything crazy
        /// </summary>
        public static void CheckRandomData(Random random, Analyzer a, int iterations)
        {
            CheckRandomData(random, a, iterations, 20, false, true);
        }

        /// <summary>
        /// Utility method for blasting tokenstreams with data to make sure they don't do anything crazy
        /// </summary>
        public static void CheckRandomData(Random random, Analyzer a, int iterations, int maxWordLength)
        {
            CheckRandomData(random, a, iterations, maxWordLength, false, true);
        }

        /// <summary>
        /// Utility method for blasting tokenstreams with data to make sure they don't do anything crazy
        /// </summary>
        /// <param name="simple"> true if only ascii strings will be used (try to avoid)</param>
        public static void CheckRandomData(Random random, Analyzer a, int iterations, bool simple)
        {
            CheckRandomData(random, a, iterations, 20, simple, true);
        }

        internal class AnalysisThread : ThreadJob
        {
            internal readonly int iterations;
            internal readonly int maxWordLength;
            internal readonly long seed;
            internal readonly Analyzer a;
            internal readonly bool useCharFilter;
            internal readonly bool simple;
            internal readonly bool offsetsAreCorrect;
            internal readonly RandomIndexWriter iw;
            private readonly CountdownEvent latch;

            // NOTE: not volatile because we don't want the tests to
            // add memory barriers (ie alter how threads
            // interact)... so this is just "best effort":
            public bool Failed { get; set; }
            public Exception FirstException { get; set; } = null;

            internal AnalysisThread(long seed, CountdownEvent latch, Analyzer a, int iterations, int maxWordLength, 
                bool useCharFilter, bool simple, bool offsetsAreCorrect, RandomIndexWriter iw)
            {
                this.seed = seed;
                this.a = a;
                this.iterations = iterations;
                this.maxWordLength = maxWordLength;
                this.useCharFilter = useCharFilter;
                this.simple = simple;
                this.offsetsAreCorrect = offsetsAreCorrect;
                this.iw = iw;
                this.latch = latch;
            }

            public override void Run()
            {
                bool success = false;
                try
                {
                    if (latch != null) latch.Wait();
                    // see the part in checkRandomData where it replays the same text again
                    // to verify reproducability/reuse: hopefully this would catch thread hazards.
                    CheckRandomData(new J2N.Randomizer(seed), a, iterations, maxWordLength, useCharFilter, simple, offsetsAreCorrect, iw);
                    success = true;
                }
                catch (Exception e) when (e.IsException())
                {
                    //Console.WriteLine("Exception in Thread: " + e);
                    //throw; // LUCENENET: CA2200: Rethrow to preserve stack details (https://docs.microsoft.com/en-us/visualstudio/code-quality/ca2200-rethrow-to-preserve-stack-details)
                    // LUCENENET: Throwing an exception on another thread
                    // is pointless, so we set it to a variable so we can read
                    // it from our main thread (for debugging).
                    if (FirstException is null)
                    {
                        FirstException = e;
                    }
                }
                finally
                {
                    Failed = !success;
                }
            }
        }

        public static void CheckRandomData(Random random, Analyzer a, int iterations, int maxWordLength, bool simple)
        {
            CheckRandomData(random, a, iterations, maxWordLength, simple, true);
        }

        public static void CheckRandomData(Random random, Analyzer a, int iterations, int maxWordLength, bool simple, bool offsetsAreCorrect)
        {
            CheckResetException(a, "best effort");
            long seed = random.NextInt64();
            bool useCharFilter = random.NextBoolean();
            Directory dir = null;
            RandomIndexWriter iw = null;
            string postingsFormat = TestUtil.GetPostingsFormat("dummy");
            bool codecOk = iterations * maxWordLength < 100000
                || !(postingsFormat.Equals("Memory", StringComparison.Ordinal) 
                || postingsFormat.Equals("SimpleText", StringComparison.Ordinal));
            if (Rarely(random) && codecOk)
            {
                dir = NewFSDirectory(CreateTempDir("bttc"));
                iw = new RandomIndexWriter(new J2N.Randomizer(seed), dir, a);
            }

            bool success = false;
            try
            {
                CheckRandomData(new J2N.Randomizer(seed), a, iterations, maxWordLength, useCharFilter, simple, offsetsAreCorrect, iw);
                // now test with multiple threads: note we do the EXACT same thing we did before in each thread,
                // so this should only really fail from another thread if its an actual thread problem
                int numThreads = TestUtil.NextInt32(random, 2, 4);
                var startingGun = new CountdownEvent(1);
                var threads = new AnalysisThread[numThreads];
                for (int i = 0; i < threads.Length; i++)
                {
                    threads[i] = new AnalysisThread(seed, startingGun, a, iterations, maxWordLength, useCharFilter, simple, offsetsAreCorrect, iw);
                }

                foreach (AnalysisThread thread in threads)
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

                //if (threads.Any(x => x.Failed))
                //    Fail("Thread threw exception");
                foreach (var t in threads)
                {
                    if (t.Failed)
                    {
                        fail("Thread threw exception: " + t.FirstException.ToString());
                    }
                }

                success = true;
            }
            finally
            {
                if (success)
                {
                    IOUtils.Dispose(iw, dir);
                }
                else
                {
                    IOUtils.DisposeWhileHandlingException(iw, dir); // checkindex
                }
            }
        }

        private static void CheckRandomData(Random random, Analyzer a, int iterations, int maxWordLength, bool useCharFilter, bool simple, bool offsetsAreCorrect, RandomIndexWriter iw)
        {
            LineFileDocs docs = new LineFileDocs(random);
            try
            {
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
                        if (ft.StoreTermVectorPositions && !OldFormatImpersonationIsActive)
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
                            ft.IndexOptions = IndexOptions.DOCS_ONLY;
                            break;

                        case 1:
                            ft.IndexOptions = IndexOptions.DOCS_AND_FREQS;
                            break;

                        case 2:
                            ft.IndexOptions = IndexOptions.DOCS_AND_FREQS_AND_POSITIONS;
                            break;

                        default:
                            if (supportsOffsets && offsetsAreCorrect)
                            {
                                ft.IndexOptions = IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS;
                            }
                            else
                            {
                                ft.IndexOptions = IndexOptions.DOCS_AND_FREQS_AND_POSITIONS;
                            }
                            break;
                    }
                    currentField = field = new Field("dummy", bogus, ft);
                    doc.Add(currentField);
                }

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
                                var ft = field.FieldType;
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
                    catch (Exception t) when (t.IsThrowable())
                    {
                        // TODO: really we should pass a random seed to
                        // checkAnalysisConsistency then print it here too:
                        Console.Error.WriteLine($"TEST FAIL (iteration {i}): useCharFilter=" + useCharFilter + " text='" + Escape(text) + "'");
                        throw; // LUCENENET: CA2200: Rethrow to preserve stack details (https://docs.microsoft.com/en-us/visualstudio/code-quality/ca2200-rethrow-to-preserve-stack-details)
                    }
                }
            }
            finally
            {
                IOUtils.DisposeWhileHandlingException(docs);
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
            if (Verbose)
            {
                Console.WriteLine(Thread.CurrentThread.Name + ": NOTE: baseTokenStreamTestCase: get first token stream now text=" + text);
            }

            ICharTermAttribute termAtt;
            IOffsetAttribute offsetAtt;
            IPositionIncrementAttribute posIncAtt;
            IPositionLengthAttribute posLengthAtt;
            ITypeAttribute typeAtt;

            IList<string> tokens = new JCG.List<string>();
            IList<string> types = new JCG.List<string>();
            IList<int> positions = new JCG.List<int>();
            IList<int> positionLengths = new JCG.List<int>();
            IList<int> startOffsets = new JCG.List<int>();
            IList<int> endOffsets = new JCG.List<int>();

            int remainder = random.Next(10);
            TextReader reader = new StringReader(text);

            TokenStream ts;
            using (ts = a.GetTokenStream("dummy", useCharFilter ? new MockCharFilter(reader, remainder) : reader))
            {
                bool isReset = false;
                try
                {
                    termAtt = ts.HasAttribute<ICharTermAttribute>() ? ts.GetAttribute<ICharTermAttribute>() : null;
                    offsetAtt = ts.HasAttribute<IOffsetAttribute>() ? ts.GetAttribute<IOffsetAttribute>() : null;
                    posIncAtt = ts.HasAttribute<IPositionIncrementAttribute>() ? ts.GetAttribute<IPositionIncrementAttribute>() : null;
                    posLengthAtt = ts.HasAttribute<IPositionLengthAttribute>() ? ts.GetAttribute<IPositionLengthAttribute>() : null;
                    typeAtt = ts.HasAttribute<ITypeAttribute>() ? ts.GetAttribute<ITypeAttribute>() : null;

                    ts.Reset();
                    isReset = true;

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
                            startOffsets.Add(offsetAtt.StartOffset);
                            endOffsets.Add(offsetAtt.EndOffset);
                        }
                    }
                    // LUCENENET: We are doing this in the finally block to ensure it happens
                    // when there are exeptions thrown (such as when the assert fails).
                    //ts.End();
                    //ts.Dispose();
                }
                finally
                {
                    if (!isReset)
                    {
                        try
                        {
                            // consume correctly
                            ts.Reset();
                            while (ts.IncrementToken());
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
            } // ts.close();

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
                        if (Verbose)
                        {
                            Console.WriteLine(Thread.CurrentThread.Name + ": NOTE: baseTokenStreamTestCase: re-run analysis w/ exception");
                        }
                        // Throw an errant exception from the Reader:

                        using MockReaderWrapper evilReader = new MockReaderWrapper(random, new StringReader(text));
                        evilReader.ThrowExcAfterChar(random.Next(text.Length)); // LUCENENET note, Next() is exclusive, so we don't need +1
                        //reader = evilReader; // LUCENENET: IDE0059: Remove unnecessary value assignment

                        try
                        {
                            // NOTE: some Tokenizers go and read characters
                            // when you call .SetReader(TextReader), eg
                            // PatternTokenizer.  this is a bit
                            // iffy... (really, they should only
                            // pull from the TextReader when you call
                            // .IncremenToken(), I think?), but we
                            // currently allow it, so, we must call
                            // a.TokenStream inside the try since we may
                            // hit the exc on init:
                            ts = a.GetTokenStream("dummy", useCharFilter ? (TextReader)new MockCharFilter(evilReader, remainder) : evilReader);
                            ts.Reset();
                            while (ts.IncrementToken()) ;
                            Assert.Fail("did not hit exception");
                        }
                        catch (Exception re) when (re.IsRuntimeException())
                        {
                            Assert.IsTrue(MockReaderWrapper.IsMyEvilException(re));
                        }

                        try
                        {
                            ts.End();
                        }
                        catch (Exception ae) when (ae.IsAssertionError() && ae.Message.Contains("End() called before IncrementToken() returned false!"))
                        {
                            // Catch & ignore MockTokenizer's
                            // anger...
                            // OK
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
                        if (Verbose)
                        {
                            Console.WriteLine(Thread.CurrentThread.Name + ": NOTE: baseTokenStreamTestCase: re-run analysis, only consuming " + numTokensToRead + " of " + tokens.Count + " tokens");
                        }

                        reader = new StringReader(text);
                        ts = a.GetTokenStream("dummy", useCharFilter ? (TextReader)new MockCharFilter(reader, remainder) : reader);
                        ts.Reset();
                        for (int tokenCount = 0; tokenCount < numTokensToRead; tokenCount++)
                        {
                            Assert.IsTrue(ts.IncrementToken());
                        }

                        try
                        {
                            ts.End();
                        }
                        catch (Exception ae) when (ae.IsAssertionError() && ae.Message.Contains("End() called before IncrementToken() returned false!"))
                        {
                            // Catch & ignore MockTokenizer's
                            // anger...
                            // OK
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

            if (Verbose)
            {
                Console.WriteLine(Thread.CurrentThread.Name + ": NOTE: baseTokenStreamTestCase: re-run analysis; " + tokens.Count + " tokens");
            }
            reader = new StringReader(text);

            long seed = random.NextInt64();
            random = new J2N.Randomizer(seed);
            if (random.Next(30) == 7)
            {
                if (Verbose)
                {
                    Console.WriteLine(Thread.CurrentThread.Name + ": NOTE: baseTokenStreamTestCase: using spoon-feed reader");
                }

                reader = new MockReaderWrapper(random, reader);
            }

            ts = a.GetTokenStream("dummy", useCharFilter ? (TextReader)new MockCharFilter(reader, remainder) : reader);
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
                random = new J2N.Randomizer(seed);
                if (random.Next(30) == 7)
                {
                    if (Verbose)
                    {
                        Console.WriteLine(Thread.CurrentThread.Name + ": NOTE: baseTokenStreamTestCase: indexing using spoon-feed reader");
                    }

                    reader = new MockReaderWrapper(random, reader);
                }

                field.SetReaderValue(useCharFilter ? (TextReader)new MockCharFilter(reader, remainder) : reader);
            }
        }

        protected internal virtual string ToDot(Analyzer a, string inputText)
        {
            StringWriter sw = new StringWriter();
            TokenStream ts = a.GetTokenStream("field", new StringReader(inputText));
            ts.Reset();
            (new TokenStreamToDot(inputText, ts, /*new StreamWriter(*/(TextWriter)sw/*)*/)).ToDot();
            return sw.ToString();
        }

        protected internal virtual void ToDotFile(Analyzer a, string inputText, string localFileName)
        {
            using StreamWriter w = new StreamWriter(new FileStream(localFileName, FileMode.Open), Encoding.UTF8);
            TokenStream ts = a.GetTokenStream("field", new StringReader(inputText));
            ts.Reset();
            (new TokenStreamToDot(inputText, ts,/* new PrintWriter(*/w/*)*/)).ToDot();
        }

        [ExceptionToNetNumericConvention] // LUCENENET: Private API, keeping as-is
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

        // *********** From Lucene 8.2.0 **************

        /// <summary>Returns a random <see cref="AttributeFactory"/> impl</summary>
        public static AttributeFactory NewAttributeFactory(Random random)
        {
            switch (random.Next(2))
            {
                case 0:
                    return Token.TOKEN_ATTRIBUTE_FACTORY;
                case 1:
                    return AttributeFactory.DEFAULT_ATTRIBUTE_FACTORY;
                default:
                    throw AssertionError.Create("Please fix the Random.nextInt() call above");
            }

            //switch (random.nextInt(3))
            //{
            //    case 0:
            //        return TokenStream.DEFAULT_TOKEN_ATTRIBUTE_FACTORY;
            //    case 1:
            //        return Token.TOKEN_ATTRIBUTE_FACTORY;
            //    case 2:
            //        return AttributeFactory.DEFAULT_ATTRIBUTE_FACTORY;
            //    default:
            //        throw AssertionError.Create("Please fix the Random.nextInt() call above");
            //}
        }

        /// <summary>Returns a random <see cref="AttributeFactory"/> impl</summary>
        public static AttributeFactory NewAttributeFactory()
        {
            return NewAttributeFactory(Random);
        }

        // *********** End From Lucene 8.2.0 **************
    }
}