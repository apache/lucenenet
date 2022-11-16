using J2N;
using J2N.Collections;
using J2N.Collections.Generic.Extensions;
using Lucene.Net.Diagnostics;
using Lucene.Net.Store;
using Lucene.Net.Support;
using Lucene.Net.Util.Packed;
using RandomizedTesting.Generators;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Assert = Lucene.Net.TestFramework.Assert;
using Console = Lucene.Net.Util.SystemConsole;
using Directory = Lucene.Net.Store.Directory;
using JCG = J2N.Collections.Generic;
using Int64 = J2N.Numerics.Int64;

namespace Lucene.Net.Util.Fst
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
    /// Holds one input/output pair. </summary>
    public class InputOutput<T> : IComparable<InputOutput<T>> where T : class // LUCENENET specific - added class constraint because we compare reference equality
    {
        public Int32sRef Input { get; private set; }
        public T Output { get; private set; }

        public InputOutput(Int32sRef input, T output)
        {
            this.Input = input;
            this.Output = output;
        }

        public virtual int CompareTo(InputOutput<T> other)
        {
            return this.Input.CompareTo(other.Input);
        }
    }

    /// <summary>
    /// Helper class to test FSTs. </summary>
    public class FSTTester<T> where T : class // LUCENENET specific - added class constraint because we compare reference equality
    {
        internal readonly Random random;
        internal readonly IList<InputOutput<T>> pairs;
        internal readonly int inputMode;
        internal readonly Outputs<T> outputs;
        internal readonly Directory dir;
        internal readonly bool doReverseLookup;

        public FSTTester(Random random, Directory dir, int inputMode, IList<InputOutput<T>> pairs, Outputs<T> outputs, bool doReverseLookup)
        {
            this.random = random;
            this.dir = dir;
            this.inputMode = inputMode;
            this.pairs = pairs;
            this.outputs = outputs;
            this.doReverseLookup = doReverseLookup;
        }

        internal static string InputToString(int inputMode, Int32sRef term)
        {
            return InputToString(inputMode, term, true);
        }

        internal static string InputToString(int inputMode, Int32sRef term, bool isValidUnicode)
        {
            if (!isValidUnicode)
            {
                return term.ToString();
            }
            else if (inputMode == 0)
            {
                // utf8
                return ToBytesRef(term).Utf8ToString() + " " + term;
            }
            else
            {
                // utf32
                return UnicodeUtil.NewString(term.Int32s, term.Offset, term.Length) + " " + term;
            }
        }

        private static BytesRef ToBytesRef(Int32sRef ir)
        {
            BytesRef br = new BytesRef(ir.Length);
            for (int i = 0; i < ir.Length; i++)
            {
                int x = ir.Int32s[ir.Offset + i];
                if (Debugging.AssertsEnabled) Debugging.Assert(x >= 0 && x <= 255);
                br.Bytes[i] = (byte)x;
            }
            br.Length = ir.Length;
            return br;
        }

        internal static string GetRandomString(Random random)
        {
            string term;
            if (random.NextBoolean())
            {
                term = TestUtil.RandomRealisticUnicodeString(random);
            }
            else
            {
                // we want to mix in limited-alphabet symbols so
                // we get more sharing of the nodes given how few
                // terms we are testing...
                term = SimpleRandomString(random);
            }
            return term;
        }

        internal static string SimpleRandomString(Random r)
        {
            int end = r.Next(10);
            if (end == 0)
            {
                // allow 0 length
                return "";
            }
            char[] buffer = new char[end];
            for (int i = 0; i < end; i++)
            {
                buffer[i] = (char)TestUtil.NextInt32(r, 97, 102);
            }
            return new string(buffer, 0, end);
        }

        internal static Int32sRef ToInt32sRef(string s, int inputMode)
        {
            return ToInt32sRef(s, inputMode, new Int32sRef(10));
        }

        internal static Int32sRef ToInt32sRef(string s, int inputMode, Int32sRef ir)
        {
            if (inputMode == 0)
            {
                // utf8
                return ToInt32sRef(new BytesRef(s), ir);
            }
            else
            {
                // utf32
                return ToInt32sRefUTF32(s, ir);
            }
        }

        internal static Int32sRef ToInt32sRefUTF32(string s, Int32sRef ir)
        {
            int charLength = s.Length;
            int charIdx = 0;
            int intIdx = 0;
            while (charIdx < charLength)
            {
                if (intIdx == ir.Int32s.Length)
                {
                    ir.Grow(intIdx + 1);
                }
                int utf32 = Character.CodePointAt(s, charIdx);
                ir.Int32s[intIdx] = utf32;
                charIdx += Character.CharCount(utf32);
                intIdx++;
            }
            ir.Length = intIdx;
            return ir;
        }

        internal static Int32sRef ToInt32sRef(BytesRef br, Int32sRef ir)
        {
            if (br.Length > ir.Int32s.Length)
            {
                ir.Grow(br.Length);
            }
            for (int i = 0; i < br.Length; i++)
            {
                ir.Int32s[i] = br.Bytes[br.Offset + i] & 0xFF;
            }
            ir.Length = br.Length;
            return ir;
        }

        // LUCENENET specific - de-nested InputOutput<T>

        public virtual void DoTest(bool testPruning)
        {
            // no pruning
            DoTest(0, 0, true);

            if (testPruning)
            {
                // simple pruning
                DoTest(TestUtil.NextInt32(random, 1, 1 + pairs.Count), 0, true);

                // leafy pruning
                DoTest(0, TestUtil.NextInt32(random, 1, 1 + pairs.Count), true);
            }
        }

        // runs the term, returning the output, or null if term
        // isn't accepted.  if prefixLength is non-null it must be
        // length 1 int array; prefixLength[0] is set to the length
        // of the term prefix that matches
        private static T Run(FST<T> fst, Int32sRef term, int[] prefixLength) // LUCENENET: CA1822: Mark members as static
        {
            if (Debugging.AssertsEnabled) Debugging.Assert(prefixLength is null || prefixLength.Length == 1);
            FST.Arc<T> arc = fst.GetFirstArc(new FST.Arc<T>());
            T NO_OUTPUT = fst.Outputs.NoOutput;
            T output = NO_OUTPUT;
            FST.BytesReader fstReader = fst.GetBytesReader();

            for (int i = 0; i <= term.Length; i++)
            {
                int label;
                if (i == term.Length)
                {
                    label = FST.END_LABEL;
                }
                else
                {
                    label = term.Int32s[term.Offset + i];
                }
                // System.out.println("   loop i=" + i + " label=" + label + " output=" + fst.Outputs.outputToString(output) + " curArc: target=" + arc.target + " isFinal?=" + arc.isFinal());
                if (fst.FindTargetArc(label, arc, arc, fstReader) is null)
                {
                    // System.out.println("    not found");
                    if (prefixLength != null)
                    {
                        prefixLength[0] = i;
                        return output;
                    }
                    else
                    {
                        return default;
                    }
                }
                output = fst.Outputs.Add(output, arc.Output);
            }

            if (prefixLength != null)
            {
                prefixLength[0] = term.Length;
            }

            return output;
        }

        private T RandomAcceptedWord(FST<T> fst, Int32sRef @in)
        {
            FST.Arc<T> arc = fst.GetFirstArc(new FST.Arc<T>());

            IList<FST.Arc<T>> arcs = new JCG.List<FST.Arc<T>>();
            @in.Length = 0;
            @in.Offset = 0;
            T NO_OUTPUT = fst.Outputs.NoOutput;
            T output = NO_OUTPUT;
            FST.BytesReader fstReader = fst.GetBytesReader();

            while (true)
            {
                // read all arcs:
                fst.ReadFirstTargetArc(arc, arc, fstReader);
                arcs.Add((new FST.Arc<T>()).CopyFrom(arc));
                while (!arc.IsLast)
                {
                    fst.ReadNextArc(arc, fstReader);
                    arcs.Add((new FST.Arc<T>()).CopyFrom(arc));
                }

                // pick one
                arc = arcs[random.Next(arcs.Count)];
                arcs.Clear();

                // accumulate output
                output = fst.Outputs.Add(output, arc.Output);

                // append label
                if (arc.Label == FST.END_LABEL)
                {
                    break;
                }

                if (@in.Int32s.Length == @in.Length)
                {
                    @in.Grow(1 + @in.Length);
                }
                @in.Int32s[@in.Length++] = arc.Label;
            }

            return output;
        }

        internal virtual FST<T> DoTest(int prune1, int prune2, bool allowRandomSuffixSharing)
        {
            if (LuceneTestCase.Verbose)
            {
                Console.WriteLine("\nTEST: prune1=" + prune1 + " prune2=" + prune2);
            }

            bool willRewrite = random.NextBoolean();

            Builder<T> builder = new Builder<T>(inputMode == 0 ? FST.INPUT_TYPE.BYTE1 : FST.INPUT_TYPE.BYTE4, 
                                                prune1, prune2, 
                                                prune1 == 0 && prune2 == 0, 
                                                allowRandomSuffixSharing ? random.NextBoolean() : true, 
                                                allowRandomSuffixSharing ? TestUtil.NextInt32(random, 1, 10) : int.MaxValue, 
                                                outputs, 
                                                null, 
                                                willRewrite, 
                                                PackedInt32s.DEFAULT, 
                                                true, 
                                                15);
            if (LuceneTestCase.Verbose)
            {
                if (willRewrite)
                {
                    Console.WriteLine("TEST: packed FST");
                }
                else
                {
                    Console.WriteLine("TEST: non-packed FST");
                }
            }

            foreach (InputOutput<T> pair in pairs)
            {
                if (pair.Output is IEnumerable<T> values)
                {
                    foreach (T value in values)
                    {
                        builder.Add(pair.Input, value);
                    }
                }
                else if (pair.Output is IEnumerable objectValues)
                {
                    Builder<object> builderObject = builder as Builder<object>;
                    foreach (object value in objectValues)
                    {
                        builderObject.Add(pair.Input, value);
                    }
                }
                else
                {
                    builder.Add(pair.Input, pair.Output);
                }
            }
            FST<T> fst = builder.Finish();

            if (random.NextBoolean() && fst != null && !willRewrite)
            {
                IOContext context = LuceneTestCase.NewIOContext(random);
                using (IndexOutput @out = dir.CreateOutput("fst.bin", context))
                {
                    fst.Save(@out);
                }
                IndexInput @in = dir.OpenInput("fst.bin", context);
                try
                {
                    fst = new FST<T>(@in, outputs);
                }
                finally
                {
                    @in.Dispose();
                    dir.DeleteFile("fst.bin");
                }
            }

            if (LuceneTestCase.Verbose && pairs.Count <= 20 && fst != null)
            {
                using (TextWriter w = new StreamWriter(new FileStream("out.dot", FileMode.OpenOrCreate), Encoding.UTF8))
                {
                    Util.ToDot(fst, w, false, false);
                }
                Console.WriteLine("SAVED out.dot");
            }

            if (LuceneTestCase.Verbose)
            {
                if (fst is null)
                {
                    Console.WriteLine("  fst has 0 nodes (fully pruned)");
                }
                else
                {
                    Console.WriteLine("  fst has " + fst.NodeCount + " nodes and " + fst.ArcCount + " arcs");
                }
            }

            if (prune1 == 0 && prune2 == 0)
            {
                VerifyUnPruned(inputMode, fst);
            }
            else
            {
                VerifyPruned(inputMode, fst, prune1, prune2);
            }

            return fst;
        }

        protected virtual bool OutputsEqual(T a, T b)
        {
            // LUCENENET: In .NET, IEnumerables do not automatically test to ensure
            // their values are equal, so we need to do that manually.
            // Note that we are testing the values without regard to whether
            // the enumerable type is nullable.
            return StructuralEqualityComparer.Default.Equals(a, b);
        }

        // FST is complete
        private void VerifyUnPruned(int inputMode, FST<T> fst)
        {
            FST<Int64> fstLong;
            ISet<Int64> validOutputs;
            long minLong = long.MaxValue;
            long maxLong = long.MinValue;

            if (doReverseLookup)
            {
                FST<Int64> fstLong0 = fst as FST<Int64>;
                fstLong = fstLong0;
                validOutputs = new JCG.HashSet<Int64>();
                foreach (InputOutput<T> pair in pairs)
                {
                    Int64 output = (Int64)(object)pair.Output;
                    maxLong = Math.Max(maxLong, output);
                    minLong = Math.Min(minLong, output);
                    validOutputs.Add(output);
                }
            }
            else
            {
                fstLong = null;
                validOutputs = null;
            }

            if (pairs.Count == 0)
            {
                Assert.IsNull(fst);
                return;
            }

            if (LuceneTestCase.Verbose)
            {
                Console.WriteLine("TEST: now verify " + pairs.Count + " terms");
                foreach (InputOutput<T> pair in pairs)
                {
                    Assert.IsNotNull(pair);
                    Assert.IsNotNull(pair.Input);
                    Assert.IsNotNull(pair.Output);
                    Console.WriteLine("  " + InputToString(inputMode, pair.Input) + ": " + outputs.OutputToString(pair.Output));
                }
            }

            Assert.IsNotNull(fst);

            // visit valid pairs in order -- make sure all words
            // are accepted, and FSTEnum's next() steps through
            // them correctly
            if (LuceneTestCase.Verbose)
            {
                Console.WriteLine("TEST: check valid terms/next()");
            }
            {
                Int32sRefFSTEnum<T> fstEnum = new Int32sRefFSTEnum<T>(fst);
                foreach (InputOutput<T> pair in pairs)
                {
                    Int32sRef term = pair.Input;
                    if (LuceneTestCase.Verbose)
                    {
                        Console.WriteLine("TEST: check term=" + InputToString(inputMode, term) + " output=" + fst.Outputs.OutputToString(pair.Output));
                    }
                    T output = Run(fst, term, null);
                    Assert.IsNotNull(output, "term " + InputToString(inputMode, term) + " is not accepted");
                    Assert.IsTrue(OutputsEqual(pair.Output, output));

                    // verify enum's next
                    Assert.IsTrue(fstEnum.MoveNext());
                    Int32sRefFSTEnum.InputOutput<T> t = fstEnum.Current;
                    Assert.IsNotNull(t);
                    Assert.AreEqual(term, t.Input, "expected input=" + InputToString(inputMode, term) + " but fstEnum returned " + InputToString(inputMode, t.Input));
                    Assert.IsTrue(OutputsEqual(pair.Output, t.Output));
                }
                Assert.IsFalse(fstEnum.MoveNext());
            }

            IDictionary<Int32sRef, T> termsMap = new Dictionary<Int32sRef, T>();
            foreach (InputOutput<T> pair in pairs)
            {
                termsMap[pair.Input] = pair.Output;
            }

            if (doReverseLookup && maxLong > minLong)
            {
                // Do random lookups so we test null (output doesn't
                // exist) case:
                Assert.IsNull(Util.GetByOutput(fstLong, minLong - 7));
                Assert.IsNull(Util.GetByOutput(fstLong, maxLong + 7));

                int num = LuceneTestCase.AtLeast(random, 100);
                for (int iter = 0; iter < num; iter++)
                {
                    long v = TestUtil.NextInt64(random, minLong, maxLong);
                    Int32sRef input = Util.GetByOutput(fstLong, v);
                    Assert.IsTrue(validOutputs.Contains(v) || input is null);
                }
            }

            // find random matching word and make sure it's valid
            if (LuceneTestCase.Verbose)
            {
                Console.WriteLine("TEST: verify random accepted terms");
            }
            Int32sRef scratch = new Int32sRef(10);
            int num_ = LuceneTestCase.AtLeast(random, 500);
            for (int iter = 0; iter < num_; iter++)
            {
                T output = RandomAcceptedWord(fst, scratch);
                Assert.IsTrue(termsMap.ContainsKey(scratch), "accepted word " + InputToString(inputMode, scratch) + " is not valid");
                Assert.IsTrue(OutputsEqual(termsMap[scratch], output));

                if (doReverseLookup)
                {
                    //System.out.println("lookup output=" + output + " outs=" + fst.Outputs);
                    Int32sRef input = Util.GetByOutput(fstLong, (Int64)(object)output);
                    Assert.IsNotNull(input);
                    //System.out.println("  got " + Util.toBytesRef(input, new BytesRef()).utf8ToString());
                    Assert.AreEqual(scratch, input);
                }
            }

            // test IntsRefFSTEnum.Seek:
            if (LuceneTestCase.Verbose)
            {
                Console.WriteLine("TEST: verify seek");
            }
            Int32sRefFSTEnum<T> fstEnum_ = new Int32sRefFSTEnum<T>(fst);
            num_ = LuceneTestCase.AtLeast(random, 100);
            for (int iter = 0; iter < num_; iter++)
            {
                if (LuceneTestCase.Verbose)
                {
                    Console.WriteLine("  iter=" + iter);
                }
                if (random.NextBoolean())
                {
                    // seek to term that doesn't exist:
                    while (true)
                    {
                        Int32sRef term = ToInt32sRef(GetRandomString(random), inputMode);
                        int pos = pairs.BinarySearch(new InputOutput<T>(term, default));
                        if (pos < 0)
                        {
                            pos = -(pos + 1);
                            // ok doesn't exist
                            //System.out.println("  seek " + inputToString(inputMode, term));
                            Int32sRefFSTEnum.InputOutput<T> seekResult;
                            if (random.Next(3) == 0)
                            {
                                if (LuceneTestCase.Verbose)
                                {
                                    Console.WriteLine("  do non-exist seekExact term=" + InputToString(inputMode, term));
                                }
                                seekResult = fstEnum_.SeekExact(term);
                                pos = -1;
                            }
                            else if (random.NextBoolean())
                            {
                                if (LuceneTestCase.Verbose)
                                {
                                    Console.WriteLine("  do non-exist seekFloor term=" + InputToString(inputMode, term));
                                }
                                seekResult = fstEnum_.SeekFloor(term);
                                pos--;
                            }
                            else
                            {
                                if (LuceneTestCase.Verbose)
                                {
                                    Console.WriteLine("  do non-exist seekCeil term=" + InputToString(inputMode, term));
                                }
                                seekResult = fstEnum_.SeekCeil(term);
                            }

                            if (pos != -1 && pos < pairs.Count)
                            {
                                //System.out.println("    got " + inputToString(inputMode,seekResult.input) + " output=" + fst.Outputs.outputToString(seekResult.Output));
                                Assert.IsNotNull(seekResult, "got null but expected term=" + InputToString(inputMode, pairs[pos].Input));
                                if (LuceneTestCase.Verbose)
                                {
                                    Console.WriteLine("    got " + InputToString(inputMode, seekResult.Input));
                                }
                                Assert.AreEqual(pairs[pos].Input, seekResult.Input, "expected " + InputToString(inputMode, pairs[pos].Input) + " but got " + InputToString(inputMode, seekResult.Input));
                                Assert.IsTrue(OutputsEqual(pairs[pos].Output, seekResult.Output));
                            }
                            else
                            {
                                // seeked before start or beyond end
                                //System.out.println("seek=" + seekTerm);
                                Assert.IsNull(seekResult, "expected null but got " + (seekResult is null ? "null" : InputToString(inputMode, seekResult.Input)));
                                if (LuceneTestCase.Verbose)
                                {
                                    Console.WriteLine("    got null");
                                }
                            }

                            break;
                        }
                    }
                }
                else
                {
                    // seek to term that does exist:
                    InputOutput<T> pair = pairs[random.Next(pairs.Count)];
                    Int32sRefFSTEnum.InputOutput<T> seekResult;
                    if (random.Next(3) == 2)
                    {
                        if (LuceneTestCase.Verbose)
                        {
                            Console.WriteLine("  do exists seekExact term=" + InputToString(inputMode, pair.Input));
                        }
                        seekResult = fstEnum_.SeekExact(pair.Input);
                    }
                    else if (random.NextBoolean())
                    {
                        if (LuceneTestCase.Verbose)
                        {
                            Console.WriteLine("  do exists seekFloor " + InputToString(inputMode, pair.Input));
                        }
                        seekResult = fstEnum_.SeekFloor(pair.Input);
                    }
                    else
                    {
                        if (LuceneTestCase.Verbose)
                        {
                            Console.WriteLine("  do exists seekCeil " + InputToString(inputMode, pair.Input));
                        }
                        seekResult = fstEnum_.SeekCeil(pair.Input);
                    }
                    Assert.IsNotNull(seekResult);
                    Assert.AreEqual(pair.Input, seekResult.Input, "got " + InputToString(inputMode, seekResult.Input) + " but expected " + InputToString(inputMode, pair.Input));
                    Assert.IsTrue(OutputsEqual(pair.Output, seekResult.Output));
                }
            }

            if (LuceneTestCase.Verbose)
            {
                Console.WriteLine("TEST: mixed next/seek");
            }

            // test mixed next/seek
            num_ = LuceneTestCase.AtLeast(random, 100);
            for (int iter = 0; iter < num_; iter++)
            {
                if (LuceneTestCase.Verbose)
                {
                    Console.WriteLine("TEST: iter " + iter);
                }
                // reset:
                fstEnum_ = new Int32sRefFSTEnum<T>(fst);
                int upto = -1;
                while (true)
                {
                    bool isDone = false;
                    if (upto == pairs.Count - 1 || random.NextBoolean())
                    {
                        // next
                        upto++;
                        if (LuceneTestCase.Verbose)
                        {
                            Console.WriteLine("  do next");
                        }
                        isDone = fstEnum_.MoveNext() == false;
                    }
                    else if (upto != -1 && upto < 0.75 * pairs.Count && random.NextBoolean())
                    {
                        int attempt = 0;
                        for (; attempt < 10; attempt++)
                        {
                            Int32sRef term = ToInt32sRef(GetRandomString(random), inputMode);
                            if (!termsMap.ContainsKey(term) && term.CompareTo(pairs[upto].Input) > 0)
                            {
                                int pos = pairs.BinarySearch(new InputOutput<T>(term, default));
                                if (Debugging.AssertsEnabled) Debugging.Assert(pos < 0);
                                upto = -(pos + 1);

                                if (random.NextBoolean())
                                {
                                    upto--;
                                    Assert.IsTrue(upto != -1);
                                    if (LuceneTestCase.Verbose)
                                    {
                                        Console.WriteLine("  do non-exist seekFloor(" + InputToString(inputMode, term) + ")");
                                    }
                                    isDone = fstEnum_.SeekFloor(term) is null;
                                }
                                else
                                {
                                    if (LuceneTestCase.Verbose)
                                    {
                                        Console.WriteLine("  do non-exist seekCeil(" + InputToString(inputMode, term) + ")");
                                    }
                                    isDone = fstEnum_.SeekCeil(term) is null;
                                }

                                break;
                            }
                        }
                        if (attempt == 10)
                        {
                            continue;
                        }
                    }
                    else
                    {
                        int inc = random.Next(pairs.Count - upto - 1);
                        upto += inc;
                        if (upto == -1)
                        {
                            upto = 0;
                        }

                        if (random.NextBoolean())
                        {
                            if (LuceneTestCase.Verbose)
                            {
                                Console.WriteLine("  do seekCeil(" + InputToString(inputMode, pairs[upto].Input) + ")");
                            }
                            isDone = fstEnum_.SeekCeil(pairs[upto].Input) is null;
                        }
                        else
                        {
                            if (LuceneTestCase.Verbose)
                            {
                                Console.WriteLine("  do seekFloor(" + InputToString(inputMode, pairs[upto].Input) + ")");
                            }
                            isDone = fstEnum_.SeekFloor(pairs[upto].Input) is null;
                        }
                    }
                    if (LuceneTestCase.Verbose)
                    {
                        if (!isDone)
                        {
                            Console.WriteLine("    got " + InputToString(inputMode, fstEnum_.Current.Input));
                        }
                        else
                        {
                            Console.WriteLine("    got null");
                        }
                    }

                    if (upto == pairs.Count)
                    {
                        Assert.IsTrue(isDone);
                        break;
                    }
                    else
                    {
                        Assert.IsFalse(isDone);
                        Assert.AreEqual(pairs[upto].Input, fstEnum_.Current.Input);
                        Assert.IsTrue(OutputsEqual(pairs[upto].Output, fstEnum_.Current.Output));

                        /*
                          if (upto < pairs.size()-1) {
                          int tryCount = 0;
                          while(tryCount < 10) {
                          final IntsRef t = toIntsRef(getRandomString(), inputMode);
                          if (pairs.get(upto).input.compareTo(t) < 0) {
                          final boolean expected = t.compareTo(pairs.get(upto+1).input) < 0;
                          if (LuceneTestCase.VERBOSE) {
                          System.out.println("TEST: call beforeNext(" + inputToString(inputMode, t) + "); current=" + inputToString(inputMode, pairs.get(upto).input) + " next=" + inputToString(inputMode, pairs.get(upto+1).input) + " expected=" + expected);
                          }
                          Assert.AreEqual(expected, fstEnum.beforeNext(t));
                          break;
                          }
                          tryCount++;
                          }
                          }
                        */
                    }
                }
            }
        }

        private class CountMinOutput<S>
        {
            internal int Count { get; set; }
            internal S Output { get; set; }
            internal S FinalOutput { get; set; }
            internal bool IsLeaf { get; set; } = true;
            internal bool IsFinal { get; set; }
        }

        // FST is pruned
        private void VerifyPruned(int inputMode, FST<T> fst, int prune1, int prune2)
        {
            if (LuceneTestCase.Verbose)
            {
                Console.WriteLine("TEST: now verify pruned " + pairs.Count + " terms; outputs=" + outputs);
                foreach (InputOutput<T> pair in pairs)
                {
                    Console.WriteLine("  " + InputToString(inputMode, pair.Input) + ": " + outputs.OutputToString(pair.Output));
                }
            }

            // To validate the FST, we brute-force compute all prefixes
            // in the terms, matched to their "common" outputs, prune that
            // set according to the prune thresholds, then assert the FST
            // matches that same set.

            // NOTE: Crazy RAM intensive!!

            //System.out.println("TEST: tally prefixes");

            // build all prefixes

#if FEATURE_DICTIONARY_REMOVE_CONTINUEENUMERATION
            IDictionary<Int32sRef, CountMinOutput<T>> prefixes = new Dictionary<Int32sRef, CountMinOutput<T>>();
#else
            // LUCENENET: We use ConcurrentDictionary<TKey, TValue> because Dictionary<TKey, TValue> doesn't support
            // deletion while iterating, but ConcurrentDictionary does.
            IDictionary<Int32sRef, CountMinOutput<T>> prefixes = new ConcurrentDictionary<Int32sRef, CountMinOutput<T>>();
#endif
            Int32sRef scratch = new Int32sRef(10);
            foreach (InputOutput<T> pair in pairs)
            {
                scratch.CopyInt32s(pair.Input);
                for (int idx = 0; idx <= pair.Input.Length; idx++)
                {
                    scratch.Length = idx;
                    if (!prefixes.TryGetValue(scratch, out CountMinOutput<T> cmo) || cmo is null)
                    {
                        cmo = new CountMinOutput<T>();
                        cmo.Count = 1;
                        cmo.Output = pair.Output;
                        prefixes[Int32sRef.DeepCopyOf(scratch)] = cmo;
                    }
                    else
                    {
                        cmo.Count++;
                        T output1 = cmo.Output;
                        if (output1.Equals(outputs.NoOutput))
                        {
                            output1 = outputs.NoOutput;
                        }
                        T output2 = pair.Output;
                        if (output2.Equals(outputs.NoOutput))
                        {
                            output2 = outputs.NoOutput;
                        }
                        cmo.Output = outputs.Common(output1, output2);
                    }
                    if (idx == pair.Input.Length)
                    {
                        cmo.IsFinal = true;
                        cmo.FinalOutput = cmo.Output;
                    }
                }
            }

            if (LuceneTestCase.Verbose)
            {
                Console.WriteLine("TEST: now prune");
            }

            // prune 'em
            using (var it = prefixes.GetEnumerator())
            {
                while (it.MoveNext())
                {
                    var ent = it.Current;
                    Int32sRef prefix = ent.Key;
                    CountMinOutput<T> cmo = ent.Value;
                    if (LuceneTestCase.Verbose)
                    {
                        Console.WriteLine("  term prefix=" + InputToString(inputMode, prefix, false) + " count=" + cmo.Count + " isLeaf=" + cmo.IsLeaf + " output=" + outputs.OutputToString(cmo.Output) + " isFinal=" + cmo.IsFinal);
                    }
                    bool keep;
                    if (prune1 > 0)
                    {
                        keep = cmo.Count >= prune1;
                    }
                    else
                    {
                        if (Debugging.AssertsEnabled) Debugging.Assert(prune2 > 0);
                        if (prune2 > 1 && cmo.Count >= prune2)
                        {
                            keep = true;
                        }
                        else if (prefix.Length > 0)
                        {
                            // consult our parent
                            scratch.Length = prefix.Length - 1;
                            Arrays.Copy(prefix.Int32s, prefix.Offset, scratch.Int32s, 0, scratch.Length);
                            keep = prefixes.TryGetValue(scratch, out CountMinOutput<T> cmo2) && cmo2 != null && ((prune2 > 1 && cmo2.Count >= prune2) || (prune2 == 1 && (cmo2.Count >= 2 || prefix.Length <= 1)));
                            //System.out.println("    parent count = " + (cmo2 is null ? -1 : cmo2.count));
                        }
                        else if (cmo.Count >= prune2)
                        {
                            keep = true;
                        }
                        else
                        {
                            keep = false;
                        }
                    }

                    if (!keep)
                    {
                        //it.remove();
                        prefixes.Remove(ent);
                        //System.out.println("    remove");
                    }
                    else
                    {
                        // clear isLeaf for all ancestors
                        //System.out.println("    keep");
                        scratch.CopyInt32s(prefix);
                        scratch.Length--;
                        while (scratch.Length >= 0)
                        {
                            if (prefixes.TryGetValue(scratch, out CountMinOutput<T> cmo2) && cmo2 != null)
                            {
                                //System.out.println("    clear isLeaf " + inputToString(inputMode, scratch));
                                cmo2.IsLeaf = false;
                            }
                            scratch.Length--;
                        }
                    }
                }
            }

            if (LuceneTestCase.Verbose)
            {
                Console.WriteLine("TEST: after prune");
                foreach (KeyValuePair<Int32sRef, CountMinOutput<T>> ent in prefixes)
                {
                    Console.WriteLine("  " + InputToString(inputMode, ent.Key, false) + ": isLeaf=" + ent.Value.IsLeaf + " isFinal=" + ent.Value.IsFinal);
                    if (ent.Value.IsFinal)
                    {
                        Console.WriteLine("    finalOutput=" + outputs.OutputToString(ent.Value.FinalOutput));
                    }
                }
            }

            if (prefixes.Count <= 1)
            {
                Assert.IsNull(fst);
                return;
            }

            Assert.IsNotNull(fst);

            // make sure FST only enums valid prefixes
            if (LuceneTestCase.Verbose)
            {
                Console.WriteLine("TEST: check pruned enum");
            }
            Int32sRefFSTEnum<T> fstEnum = new Int32sRefFSTEnum<T>(fst);
            Int32sRefFSTEnum.InputOutput<T> current;
            while (fstEnum.MoveNext())
            {
                current = fstEnum.Current;
                if (LuceneTestCase.Verbose)
                {
                    Console.WriteLine("  fstEnum.next prefix=" + InputToString(inputMode, current.Input, false) + " output=" + outputs.OutputToString(current.Output));
                }
                prefixes.TryGetValue(current.Input, out CountMinOutput<T> cmo);
                Assert.IsNotNull(cmo);
                Assert.IsTrue(cmo.IsLeaf || cmo.IsFinal);
                //if (cmo.isFinal && !cmo.isLeaf) {
                if (cmo.IsFinal)
                {
                    Assert.AreEqual(cmo.FinalOutput, current.Output);
                }
                else
                {
                    Assert.AreEqual(cmo.Output, current.Output);
                }
            }

            // make sure all non-pruned prefixes are present in the FST
            if (LuceneTestCase.Verbose)
            {
                Console.WriteLine("TEST: verify all prefixes");
            }
            int[] stopNode = new int[1];
            foreach (KeyValuePair<Int32sRef, CountMinOutput<T>> ent in prefixes)
            {
                if (ent.Key.Length > 0)
                {
                    CountMinOutput<T> cmo = ent.Value;
                    T output = Run(fst, ent.Key, stopNode);
                    if (LuceneTestCase.Verbose)
                    {
                        Console.WriteLine("TEST: verify prefix=" + InputToString(inputMode, ent.Key, false) + " output=" + outputs.OutputToString(cmo.Output));
                    }
                    // if (cmo.isFinal && !cmo.isLeaf) {
                    if (cmo.IsFinal)
                    {
                        Assert.AreEqual(cmo.FinalOutput, output);
                    }
                    else
                    {
                        Assert.AreEqual(cmo.Output, output);
                    }
                    Assert.AreEqual(ent.Key.Length, stopNode[0]);
                }
            }
        }
    }
}