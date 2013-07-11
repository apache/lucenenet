/* 
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
 * (the "License"); you may not use this file except in compliance with
 * the License.  You may obtain a copy of the License at
 * 
 * http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using Lucene.Net.Analysis;
using Lucene.Net.Util;
using NUnit.Framework;
using Lucene.Net.Analysis.Tokenattributes;
using System.Collections.Generic;
using Lucene.Net.Support;
using Lucene.Net.Store;
using System.Threading;

namespace Lucene.Net.TestFramework.Analysis
{

    /// <summary>Base class for all Lucene unit tests that use TokenStreams.</summary>
    public abstract class BaseTokenStreamTestCase : LuceneTestCase
    {
        public BaseTokenStreamTestCase()
        { }

        public BaseTokenStreamTestCase(string name)
            : base(name)
        { }

        // some helpers to test Analyzers and TokenStreams:
        public interface ICheckClearAttributesAttribute : Util.IAttribute
        {
            bool GetAndResetClearCalled();
        }

        public class CheckClearAttributesAttribute : Util.Attribute, ICheckClearAttributesAttribute
        {
            private bool clearCalled = false;

            public bool GetAndResetClearCalled()
            {
                try
                {
                    return clearCalled;
                }
                finally
                {
                    clearCalled = false;
                }
            }

            public override void Clear()
            {
                clearCalled = true;
            }

            public override bool Equals(Object other)
            {
                return (
                other is CheckClearAttributesAttribute &&
                ((CheckClearAttributesAttribute)other).clearCalled == this.clearCalled
                );
            }

            public override int GetHashCode()
            {
                //Java: return 76137213 ^ Boolean.valueOf(clearCalled).hashCode();
                return 76137213 ^ clearCalled.GetHashCode();
            }

            public override void CopyTo(Util.Attribute target)
            {
                target.Clear();
            }
        }

        public static void AssertTokenStreamContents(TokenStream ts, System.String[] output, int[] startOffsets, int[] endOffsets, string[] types, int[] posIncrements, int[] posLengths, int? finalOffset, bool offsetsAreCorrect)
        {
            assertNotNull(output);
            ICheckClearAttributesAttribute checkClearAtt = ts.AddAttribute<ICheckClearAttributesAttribute>();

            ICharTermAttribute termAtt = null;
            if (output.Length > 0)
            {
                assertTrue("has no CharTermAttribute", ts.HasAttribute<ICharTermAttribute>());
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
            if (posIncrements != null)
            {
                Assert.IsTrue(ts.HasAttribute<IPositionIncrementAttribute>(), "has no PositionIncrementAttribute");
                posIncrAtt = ts.GetAttribute<IPositionIncrementAttribute>();
            }

            IPositionLengthAttribute posLengthAtt = null;
            if (posLengths != null)
            {
                assertTrue("has no PositionLengthAttribute", ts.HasAttribute<IPositionLengthAttribute>());
                posLengthAtt = ts.GetAttribute<IPositionLengthAttribute>();
            }

            // Maps position to the start/end offset:
            IDictionary<int, int> posToStartOffset = new HashMap<int, int>();
            IDictionary<int, int> posToEndOffset = new HashMap<int, int>();

            ts.Reset();
            int pos = -1;
            int lastStartOffset = 0;
            for (int i = 0; i < output.Length; i++)
            {
                // extra safety to enforce, that the state is not preserved and also assign bogus values
                ts.ClearAttributes();
                termAtt.SetEmpty().Append("bogusTerm");
                if (offsetAtt != null) offsetAtt.SetOffset(14584724, 24683243);
                if (typeAtt != null) typeAtt.Type = "bogusType";
                if (posIncrAtt != null) posIncrAtt.PositionIncrement = 45987657;
                if (posLengthAtt != null) posLengthAtt.PositionLength = 45987653;

                checkClearAtt.GetAndResetClearCalled(); // reset it, because we called clearAttribute() before
                Assert.IsTrue(ts.IncrementToken(), "token " + i + " does not exist");
                Assert.IsTrue(checkClearAtt.GetAndResetClearCalled(), "clearAttributes() was not called correctly in TokenStream chain");

                Assert.AreEqual(output[i], termAtt.ToString(), "term " + i);
                if (startOffsets != null)
                    Assert.AreEqual(startOffsets[i], offsetAtt.StartOffset, "startOffset " + i);
                if (endOffsets != null)
                    Assert.AreEqual(endOffsets[i], offsetAtt.EndOffset, "endOffset " + i);
                if (types != null)
                    Assert.AreEqual(types[i], typeAtt.Type, "type " + i);
                if (posIncrements != null)
                    Assert.AreEqual(posIncrements[i], posIncrAtt.PositionIncrement, "posIncrement " + i);
                if (posLengths != null)
                    assertEquals("posLength " + i, posLengths[i], posLengthAtt.PositionLength);

                // we can enforce some basic things about a few attributes even if the caller doesn't check:
                if (offsetAtt != null)
                {
                    int startOffset = offsetAtt.StartOffset;
                    int endOffset = offsetAtt.EndOffset;
                    if (finalOffset != null)
                    {
                        assertTrue("startOffset must be <= finalOffset", startOffset <= finalOffset.GetValueOrDefault());
                        assertTrue("endOffset must be <= finalOffset: got endOffset=" + endOffset + " vs finalOffset=" + finalOffset.GetValueOrDefault(),
                                   endOffset <= finalOffset.GetValueOrDefault());
                    }

                    if (offsetsAreCorrect)
                    {
                        assertTrue("offsets must not go backwards startOffset=" + startOffset + " is < lastStartOffset=" + lastStartOffset, offsetAtt.StartOffset >= lastStartOffset);
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
                            assertEquals("pos=" + pos + " posLen=" + posLength + " token=" + termAtt, posToStartOffset[pos], startOffset);
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
                            assertEquals("pos=" + pos + " posLen=" + posLength + " token=" + termAtt, posToEndOffset[endPos], endOffset);
                        }
                    }
                }
                if (posIncrAtt != null)
                {
                    if (i == 0)
                    {
                        assertTrue("first posIncrement must be >= 1", posIncrAtt.PositionIncrement >= 1);
                    }
                    else
                    {
                        assertTrue("posIncrement must be >= 0", posIncrAtt.PositionIncrement >= 0);
                    }
                }
                if (posLengthAtt != null)
                {
                    assertTrue("posLength must be >= 1", posLengthAtt.PositionLength >= 1);
                }
            }
            Assert.IsFalse(ts.IncrementToken(), "end of stream");
            ts.End();
            if (finalOffset.HasValue)
                Assert.AreEqual(finalOffset, offsetAtt.EndOffset, "finalOffset ");
            if (offsetAtt != null)
            {
                assertTrue("finalOffset must be >= 0", offsetAtt.EndOffset >= 0);
            }
            ts.Dispose();
        }

        public static void AssertTokenStreamContents(TokenStream ts, String[] output, int[] startOffsets, int[] endOffsets, String[] types, int[] posIncrements, int[] posLengths, int? finalOffset)
        {
            AssertTokenStreamContents(ts, output, startOffsets, endOffsets, types, posIncrements, posLengths, finalOffset, true);
        }

        public static void AssertTokenStreamContents(TokenStream ts, String[] output, int[] startOffsets, int[] endOffsets, String[] types, int[] posIncrements, int? finalOffset)
        {
            AssertTokenStreamContents(ts, output, startOffsets, endOffsets, types, posIncrements, null, finalOffset);
        }

        public static void AssertTokenStreamContents(TokenStream ts, String[] output, int[] startOffsets, int[] endOffsets, String[] types, int[] posIncrements)
        {
            AssertTokenStreamContents(ts, output, startOffsets, endOffsets, types, posIncrements, null, null);
        }

        public static void AssertTokenStreamContents(TokenStream ts, String[] output)
        {
            AssertTokenStreamContents(ts, output, null, null, null, null, null, null);
        }

        public static void AssertTokenStreamContents(TokenStream ts, String[] output, String[] types)
        {
            AssertTokenStreamContents(ts, output, null, null, types, null, null, null);
        }

        public static void AssertTokenStreamContents(TokenStream ts, String[] output, int[] posIncrements)
        {
            AssertTokenStreamContents(ts, output, null, null, null, posIncrements, null, null);
        }

        public static void AssertTokenStreamContents(TokenStream ts, String[] output, int[] startOffsets, int[] endOffsets)
        {
            AssertTokenStreamContents(ts, output, startOffsets, endOffsets, null, null, null, null);
        }

        public static void AssertTokenStreamContents(TokenStream ts, String[] output, int[] startOffsets, int[] endOffsets, int? finalOffset)
        {
            AssertTokenStreamContents(ts, output, startOffsets, endOffsets, null, null, null, finalOffset);
        }

        public static void AssertTokenStreamContents(TokenStream ts, String[] output, int[] startOffsets, int[] endOffsets, int[] posIncrements)
        {
            AssertTokenStreamContents(ts, output, startOffsets, endOffsets, null, posIncrements, null, null);
        }

        public static void AssertTokenStreamContents(TokenStream ts, String[] output, int[] startOffsets, int[] endOffsets, int[] posIncrements, int? finalOffset)
        {
            AssertTokenStreamContents(ts, output, startOffsets, endOffsets, null, posIncrements, null, finalOffset);
        }

        public static void AssertTokenStreamContents(TokenStream ts, String[] output, int[] startOffsets, int[] endOffsets, int[] posIncrements, int[] posLengths, int? finalOffset)
        {
            AssertTokenStreamContents(ts, output, startOffsets, endOffsets, null, posIncrements, posLengths, finalOffset);
        }

        public static void AssertAnalyzesTo(Analyzer a, String input, String[] output, int[] startOffsets, int[] endOffsets, string[] types, int[] posIncrements)
        {
            AssertTokenStreamContents(a.TokenStream("dummy", new System.IO.StringReader(input)), output, startOffsets, endOffsets, types, posIncrements, null, input.Length);
        }

        public static void AssertAnalyzesTo(Analyzer a, String input, String[] output, int[] startOffsets, int[] endOffsets, string[] types, int[] posIncrements, int[] posLengths)
        {
            AssertTokenStreamContents(a.TokenStream("dummy", new System.IO.StringReader(input)), output, startOffsets, endOffsets, types, posIncrements, posLengths, input.Length);
        }

        public static void AssertAnalyzesTo(Analyzer a, String input, String[] output, int[] startOffsets, int[] endOffsets, string[] types, int[] posIncrements, int[] posLengths, bool offsetsAreCorrect)
        {
            AssertTokenStreamContents(a.TokenStream("dummy", new System.IO.StringReader(input)), output, startOffsets, endOffsets, types, posIncrements, posLengths, input.Length, offsetsAreCorrect);
        }

        public static void AssertAnalyzesTo(Analyzer a, String input, String[] output)
        {
            AssertAnalyzesTo(a, input, output, null, null, null, null, null);
        }

        public static void AssertAnalyzesTo(Analyzer a, String input, String[] output, String[] types)
        {
            AssertAnalyzesTo(a, input, output, null, null, types, null, null);
        }

        public static void AssertAnalyzesTo(Analyzer a, String input, String[] output, int[] posIncrements)
        {
            AssertAnalyzesTo(a, input, output, null, null, null, posIncrements, null);
        }

        public static void AssertAnalyzesToPositions(Analyzer a, String input, String[] output, int[] posIncrements, int[] posLengths)
        {
            AssertAnalyzesTo(a, input, output, null, null, null, posIncrements, posLengths);
        }

        public static void AssertAnalyzesTo(Analyzer a, String input, String[] output, int[] startOffsets, int[] endOffsets)
        {
            AssertAnalyzesTo(a, input, output, startOffsets, endOffsets, null, null, null);
        }

        public static void AssertAnalyzesTo(Analyzer a, String input, String[] output, int[] startOffsets, int[] endOffsets, int[] posIncrements)
        {
            AssertAnalyzesTo(a, input, output, startOffsets, endOffsets, null, posIncrements, null);
        }


        public static void AssertAnalyzesToReuse(Analyzer a, String input, String[] output, int[] startOffsets, int[] endOffsets, string[] types, int[] posIncrements)
        {
            AssertTokenStreamContents(a.TokenStream("dummy", new System.IO.StringReader(input)), output, startOffsets, endOffsets, types, posIncrements, null, input.Length);
        }

        public static void AssertAnalyzesToReuse(Analyzer a, String input, String[] output)
        {
            AssertAnalyzesToReuse(a, input, output, null, null, null, null);
        }

        public static void AssertAnalyzesToReuse(Analyzer a, String input, String[] output, String[] types)
        {
            AssertAnalyzesToReuse(a, input, output, null, null, types, null);
        }

        public static void AssertAnalyzesToReuse(Analyzer a, String input, String[] output, int[] posIncrements)
        {
            AssertAnalyzesToReuse(a, input, output, null, null, null, posIncrements);
        }

        public static void AssertAnalyzesToReuse(Analyzer a, String input, String[] output, int[] startOffsets, int[] endOffsets)
        {
            AssertAnalyzesToReuse(a, input, output, startOffsets, endOffsets, null, null);
        }

        public static void AssertAnalyzesToReuse(Analyzer a, String input, String[] output, int[] startOffsets, int[] endOffsets, int[] posIncrements)
        {
            AssertAnalyzesToReuse(a, input, output, startOffsets, endOffsets, null, posIncrements);
        }

        // simple utility method for testing stemmers

        public static void CheckOneTerm(Analyzer a, String input, String expected)
        {
            AssertAnalyzesTo(a, input, new String[] { expected });
        }

        public static void CheckOneTermReuse(Analyzer a, String input, String expected)
        {
            AssertAnalyzesToReuse(a, input, new String[] { expected });
        }

        /** utility method for blasting tokenstreams with data to make sure they don't do anything crazy */
        public static void CheckRandomData(Random random, Analyzer a, int iterations)
        {
            CheckRandomData(random, a, iterations, 20, false, true);
        }

        /** utility method for blasting tokenstreams with data to make sure they don't do anything crazy */
        public static void CheckRandomData(Random random, Analyzer a, int iterations, int maxWordLength)
        {
            CheckRandomData(random, a, iterations, maxWordLength, false, true);
        }

        /** 
         * utility method for blasting tokenstreams with data to make sure they don't do anything crazy 
         * @param simple true if only ascii strings will be used (try to avoid)
         */
        public static void CheckRandomData(Random random, Analyzer a, int iterations, bool simple)
        {
            CheckRandomData(random, a, iterations, 20, simple, true);
        }

        internal class AnalysisThread : ThreadClass
        {
            internal readonly int iterations;
            internal readonly int maxWordLength;
            internal readonly long seed;
            internal readonly Analyzer a;
            internal readonly bool useCharFilter;
            internal readonly bool simple;
            internal readonly bool offsetsAreCorrect;
            internal readonly RandomIndexWriter iw;

            // NOTE: not volatile because we don't want the tests to
            // add memory barriers (ie alter how threads
            // interact)... so this is just "best effort":
            public bool failed;

            public AnalysisThread(long seed, Analyzer a, int iterations, int maxWordLength, bool useCharFilter,
                bool simple, bool offsetsAreCorrect, RandomIndexWriter iw)
            {
                this.seed = seed;
                this.a = a;
                this.iterations = iterations;
                this.maxWordLength = maxWordLength;
                this.useCharFilter = useCharFilter;
                this.simple = simple;
                this.offsetsAreCorrect = offsetsAreCorrect;
                this.iw = iw;
            }

            public override void Run()
            {
                bool success = false;
                try
                {
                    // see the part in checkRandomData where it replays the same text again
                    // to verify reproducability/reuse: hopefully this would catch thread hazards.
                    CheckRandomData(new Random((int)seed), a, iterations, maxWordLength, useCharFilter, simple, offsetsAreCorrect, iw);
                    success = true;
                }
                catch (System.IO.IOException)
                {
                    throw;
                }
                finally
                {
                    failed = !success;
                }
            }
        }

        public static void CheckRandomData(Random random, Analyzer a, int iterations, int maxWordLength, bool simple)
        {
            CheckRandomData(random, a, iterations, maxWordLength, simple, true);
        }

        public static void CheckRandomData(Random random, Analyzer a, int iterations, int maxWordLength,
            bool simple, bool offsetsAreCorrect)
        {
            long seed = random.Next();
            bool useCharFilter = random.nextBoolean();
            Directory dir = null;
            RandomIndexWriter iw = null;
            String postingsFormat = _TestUtil.GetPostingsFormat("dummy");
            bool codecOk = iterations * maxWordLength < 100000 ||
                !(postingsFormat.Equals("Memory") ||
                    postingsFormat.Equals("SimpleText"));
            if (rarely(random) && codecOk)
            {
                dir = NewFSDirectory(_TestUtil.GetTempDir("bttc"));
                iw = new RandomIndexWriter(new Random((int)seed), dir, a);
            }
            bool success = false;
            try
            {
                CheckRandomData(new Random(seed), a, iterations, maxWordLength, useCharFilter, simple, offsetsAreCorrect, iw);
                // now test with multiple threads: note we do the EXACT same thing we did before in each thread,
                // so this should only really fail from another thread if its an actual thread problem
                int numThreads = _TestUtil.nextInt(random, 2, 4);
                AnalysisThread[] threads = new AnalysisThread[numThreads];
                for (int i = 0; i < threads.Length; i++)
                {
                    threads[i] = new AnalysisThread(seed, a, iterations, maxWordLength, useCharFilter, simple, offsetsAreCorrect, iw);
                }
                for (int i = 0; i < threads.Length; i++)
                {
                    threads[i].Start();
                }
                for (int i = 0; i < threads.Length; i++)
                {
                    try
                    {
                        threads[i].Join();
                    }
                    catch (ThreadInterruptedException e)
                    {
                        throw;
                    }
                }
                for (int i = 0; i < threads.Length; i++)
                {
                    if (threads[i].failed)
                    {
                        throw new SystemException("some thread(s) failed");
                    }
                }
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
    }
}