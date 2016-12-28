using Lucene.Net.Randomized.Generators;
using Lucene.Net.Support;
using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Lucene.Net.Util.Fst
{
    /*
         * Licensed to the Apache Software Foundation (ASF) under one or more
         * contributor license agreements.  See the NOTICE file distributed with
         * this work for additional information regarding copyright ownership.
         * The ASF licenses this file to You under the Apache License, Version 2.0
         * (the "License"); you may not use this file except in compliance with
         * the License.  You may obtain a copy of the License at
         * <p/>
         * http://www.apache.org/licenses/LICENSE-2.0
         * <p/>
         * Unless required by applicable law or agreed to in writing, software
         * distributed under the License is distributed on an "AS IS" BASIS,
         * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
         * See the License for the specific language governing permissions and
         * limitations under the License.
         */

    using Directory = Lucene.Net.Store.Directory;
    using IndexInput = Lucene.Net.Store.IndexInput;
    using IndexOutput = Lucene.Net.Store.IndexOutput;
    using IOContext = Lucene.Net.Store.IOContext;
    using PackedInts = Lucene.Net.Util.Packed.PackedInts;

    /// <summary>
    /// Helper class to test FSTs. </summary>
    public class FSTTester<T>
    {
        internal readonly Random Random;
        internal readonly List<InputOutput<T>> Pairs;
        internal readonly int InputMode;
        internal readonly Outputs<T> Outputs;
        internal readonly Directory Dir;
        internal readonly bool DoReverseLookup;

        public FSTTester(Random random, Directory dir, int inputMode, List<InputOutput<T>> pairs, Outputs<T> outputs, bool doReverseLookup)
        {
            this.Random = random;
            this.Dir = dir;
            this.InputMode = inputMode;
            this.Pairs = pairs;
            this.Outputs = outputs;
            this.DoReverseLookup = doReverseLookup;
        }

        internal static string InputToString(int inputMode, IntsRef term)
        {
            return InputToString(inputMode, term, true);
        }

        internal static string InputToString(int inputMode, IntsRef term, bool isValidUnicode)
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
                return UnicodeUtil.NewString(term.Ints, term.Offset, term.Length) + " " + term;
            }
        }

        private static BytesRef ToBytesRef(IntsRef ir)
        {
            BytesRef br = new BytesRef(ir.Length);
            for (int i = 0; i < ir.Length; i++)
            {
                int x = ir.Ints[ir.Offset + i];
                Debug.Assert(x >= 0 && x <= 255);
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
                buffer[i] = (char)TestUtil.NextInt(r, 97, 102);
            }
            return new string(buffer, 0, end);
        }

        internal static IntsRef ToIntsRef(string s, int inputMode)
        {
            return ToIntsRef(s, inputMode, new IntsRef(10));
        }

        internal static IntsRef ToIntsRef(string s, int inputMode, IntsRef ir)
        {
            if (inputMode == 0)
            {
                // utf8
                return ToIntsRef(new BytesRef(s), ir);
            }
            else
            {
                // utf32
                return ToIntsRefUTF32(s, ir);
            }
        }

        internal static IntsRef ToIntsRefUTF32(string s, IntsRef ir)
        {
            int charLength = s.Length;
            int charIdx = 0;
            int intIdx = 0;
            while (charIdx < charLength)
            {
                if (intIdx == ir.Ints.Length)
                {
                    ir.Grow(intIdx + 1);
                }
                int utf32 = Character.CodePointAt(s, charIdx);
                ir.Ints[intIdx] = utf32;
                charIdx += Character.CharCount(utf32);
                intIdx++;
            }
            ir.Length = intIdx;
            return ir;
        }

        internal static IntsRef ToIntsRef(BytesRef br, IntsRef ir)
        {
            if (br.Length > ir.Ints.Length)
            {
                ir.Grow(br.Length);
            }
            for (int i = 0; i < br.Length; i++)
            {
                ir.Ints[i] = br.Bytes[br.Offset + i] & 0xFF;
            }
            ir.Length = br.Length;
            return ir;
        }

        /// <summary>
        /// Holds one input/output pair. </summary>
        public class InputOutput<T1> : IComparable<InputOutput<T1>>
        {
            public readonly IntsRef Input;
            public readonly T1 Output;

            public InputOutput(IntsRef input, T1 output)
            {
                this.Input = input;
                this.Output = output;
            }

            public virtual int CompareTo(InputOutput<T1> other)
            {
                return this.Input.CompareTo(other.Input);
            }
        }

        public virtual void DoTest(bool testPruning)
        {
            // no pruning
            DoTest(0, 0, true);

            if (testPruning)
            {
                // simple pruning
                DoTest(TestUtil.NextInt(Random, 1, 1 + Pairs.Count), 0, true);

                // leafy pruning
                DoTest(0, TestUtil.NextInt(Random, 1, 1 + Pairs.Count), true);
            }
        }

        // runs the term, returning the output, or null if term
        // isn't accepted.  if prefixLength is non-null it must be
        // length 1 int array; prefixLength[0] is set to the length
        // of the term prefix that matches
        private T Run(FST<T> fst, IntsRef term, int[] prefixLength)
        {
            Debug.Assert(prefixLength == null || prefixLength.Length == 1);
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
                    label = term.Ints[term.Offset + i];
                }
                // System.out.println("   loop i=" + i + " label=" + label + " output=" + fst.Outputs.outputToString(output) + " curArc: target=" + arc.target + " isFinal?=" + arc.isFinal());
                if (fst.FindTargetArc(label, arc, arc, fstReader) == null)
                {
                    // System.out.println("    not found");
                    if (prefixLength != null)
                    {
                        prefixLength[0] = i;
                        return output;
                    }
                    else
                    {
                        return default(T);
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

        private T RandomAcceptedWord(FST<T> fst, IntsRef @in)
        {
            FST.Arc<T> arc = fst.GetFirstArc(new FST.Arc<T>());

            IList<FST.Arc<T>> arcs = new List<FST.Arc<T>>();
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
                arc = arcs[Random.Next(arcs.Count)];
                arcs.Clear();

                // accumulate output
                output = fst.Outputs.Add(output, arc.Output);

                // append label
                if (arc.Label == FST.END_LABEL)
                {
                    break;
                }

                if (@in.Ints.Length == @in.Length)
                {
                    @in.Grow(1 + @in.Length);
                }
                @in.Ints[@in.Length++] = arc.Label;
            }

            return output;
        }

        internal virtual FST<T> DoTest(int prune1, int prune2, bool allowRandomSuffixSharing)
        {
            if (LuceneTestCase.VERBOSE)
            {
                Console.WriteLine("\nTEST: prune1=" + prune1 + " prune2=" + prune2);
            }

            bool willRewrite = Random.NextBoolean();

            Builder<T> builder = new Builder<T>(InputMode == 0 ? FST.INPUT_TYPE.BYTE1 : FST.INPUT_TYPE.BYTE4, 
                                                prune1, prune2, 
                                                prune1 == 0 && prune2 == 0, 
                                                allowRandomSuffixSharing ? Random.NextBoolean() : true, 
                                                allowRandomSuffixSharing ? TestUtil.NextInt(Random, 1, 10) : int.MaxValue, 
                                                Outputs, 
                                                null, 
                                                willRewrite, 
                                                PackedInts.DEFAULT, 
                                                true, 
                                                15);
            if (LuceneTestCase.VERBOSE)
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

            foreach (InputOutput<T> pair in Pairs)
            {
                if (pair.Output is IEnumerable)
                {
                    Builder<object> builderObject = builder as Builder<object>;
                    var values = pair.Output as IEnumerable;
                    foreach (object value in values)
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

            if (Random.NextBoolean() && fst != null && !willRewrite)
            {
                IOContext context = LuceneTestCase.NewIOContext(Random);
                using (IndexOutput @out = Dir.CreateOutput("fst.bin", context))
                {
                    fst.Save(@out);
                }
                IndexInput @in = Dir.OpenInput("fst.bin", context);
                try
                {
                    fst = new FST<T>(@in, Outputs);
                }
                finally
                {
                    @in.Dispose();
                    Dir.DeleteFile("fst.bin");
                }
            }

            if (LuceneTestCase.VERBOSE && Pairs.Count <= 20 && fst != null)
            {
                using (TextWriter w = new StreamWriter(new FileStream("out.dot", FileMode.OpenOrCreate), IOUtils.CHARSET_UTF_8))
                {
                    Util.ToDot(fst, w, false, false);
                }
                Console.WriteLine("SAVED out.dot");
            }

            if (LuceneTestCase.VERBOSE)
            {
                if (fst == null)
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
                VerifyUnPruned(InputMode, fst);
            }
            else
            {
                VerifyPruned(InputMode, fst, prune1, prune2);
            }

            return fst;
        }

        protected internal virtual bool OutputsEqual(T a, T b)
        {
            // LUCENENET: In .NET, IEnumerables do not automatically test to ensure
            // their values are equal, so we need to do that manually.
            // Note that we are testing the values without regard to whether
            // the enumerable type is nullable.
            return a.ValueEquals(b);
        }

        // FST is complete
        private void VerifyUnPruned(int inputMode, FST<T> fst)
        {
            FST<long?> fstLong;
            ISet<long?> validOutputs;
            long minLong = long.MaxValue;
            long maxLong = long.MinValue;

            if (DoReverseLookup)
            {
                FST<long?> fstLong0 = fst as FST<long?>;
                fstLong = fstLong0;
                validOutputs = new HashSet<long?>();
                foreach (InputOutput<T> pair in Pairs)
                {
                    long? output = pair.Output as long?;
                    maxLong = Math.Max(maxLong, output.Value);
                    minLong = Math.Min(minLong, output.Value);
                    validOutputs.Add(output.Value);
                }
            }
            else
            {
                fstLong = null;
                validOutputs = null;
            }

            if (Pairs.Count == 0)
            {
                Assert.IsNull(fst);
                return;
            }

            if (LuceneTestCase.VERBOSE)
            {
                Console.WriteLine("TEST: now verify " + Pairs.Count + " terms");
                foreach (InputOutput<T> pair in Pairs)
                {
                    Assert.IsNotNull(pair);
                    Assert.IsNotNull(pair.Input);
                    Assert.IsNotNull(pair.Output);
                    Console.WriteLine("  " + InputToString(inputMode, pair.Input) + ": " + Outputs.OutputToString(pair.Output));
                }
            }

            Assert.IsNotNull(fst);

            // visit valid pairs in order -- make sure all words
            // are accepted, and FSTEnum's next() steps through
            // them correctly
            if (LuceneTestCase.VERBOSE)
            {
                Console.WriteLine("TEST: check valid terms/next()");
            }
            {
                IntsRefFSTEnum<T> fstEnum = new IntsRefFSTEnum<T>(fst);
                foreach (InputOutput<T> pair in Pairs)
                {
                    IntsRef term = pair.Input;
                    if (LuceneTestCase.VERBOSE)
                    {
                        Console.WriteLine("TEST: check term=" + InputToString(inputMode, term) + " output=" + fst.Outputs.OutputToString(pair.Output));
                    }
                    T output = Run(fst, term, null);
                    Assert.IsNotNull(output, "term " + InputToString(inputMode, term) + " is not accepted");
                    Assert.IsTrue(OutputsEqual(pair.Output, output));

                    // verify enum's next
                    IntsRefFSTEnum.InputOutput<T> t = fstEnum.Next();
                    Assert.IsNotNull(t);
                    Assert.AreEqual(term, t.Input, "expected input=" + InputToString(inputMode, term) + " but fstEnum returned " + InputToString(inputMode, t.Input));
                    Assert.IsTrue(OutputsEqual(pair.Output, t.Output));
                }
                Assert.IsNull(fstEnum.Next());
            }

            IDictionary<IntsRef, T> termsMap = new Dictionary<IntsRef, T>();
            foreach (InputOutput<T> pair in Pairs)
            {
                termsMap[pair.Input] = pair.Output;
            }

            if (DoReverseLookup && maxLong > minLong)
            {
                // Do random lookups so we test null (output doesn't
                // exist) case:
                Assert.IsNull(Util.GetByOutput(fstLong, minLong - 7));
                Assert.IsNull(Util.GetByOutput(fstLong, maxLong + 7));

                int num = LuceneTestCase.AtLeast(Random, 100);
                for (int iter = 0; iter < num; iter++)
                {
                    long v = TestUtil.NextLong(Random, minLong, maxLong);
                    IntsRef input = Util.GetByOutput(fstLong, v);
                    Assert.IsTrue(validOutputs.Contains(v) || input == null);
                }
            }

            // find random matching word and make sure it's valid
            if (LuceneTestCase.VERBOSE)
            {
                Console.WriteLine("TEST: verify random accepted terms");
            }
            IntsRef scratch = new IntsRef(10);
            int num_ = LuceneTestCase.AtLeast(Random, 500);
            for (int iter = 0; iter < num_; iter++)
            {
                T output = RandomAcceptedWord(fst, scratch);
                Assert.IsTrue(termsMap.ContainsKey(scratch), "accepted word " + InputToString(inputMode, scratch) + " is not valid");
                Assert.IsTrue(OutputsEqual(termsMap[scratch], output));

                if (DoReverseLookup)
                {
                    //System.out.println("lookup output=" + output + " outs=" + fst.Outputs);
                    IntsRef input = Util.GetByOutput(fstLong, (output as long?).Value);
                    Assert.IsNotNull(input);
                    //System.out.println("  got " + Util.toBytesRef(input, new BytesRef()).utf8ToString());
                    Assert.AreEqual(scratch, input);
                }
            }

            // test IntsRefFSTEnum.Seek:
            if (LuceneTestCase.VERBOSE)
            {
                Console.WriteLine("TEST: verify seek");
            }
            IntsRefFSTEnum<T> fstEnum_ = new IntsRefFSTEnum<T>(fst);
            num_ = LuceneTestCase.AtLeast(Random, 100);
            for (int iter = 0; iter < num_; iter++)
            {
                if (LuceneTestCase.VERBOSE)
                {
                    Console.WriteLine("  iter=" + iter);
                }
                if (Random.NextBoolean())
                {
                    // seek to term that doesn't exist:
                    while (true)
                    {
                        IntsRef term = ToIntsRef(GetRandomString(Random), inputMode);
                        int pos = Pairs.BinarySearch(new InputOutput<T>(term, default(T)));
                        if (pos < 0)
                        {
                            pos = -(pos + 1);
                            // ok doesn't exist
                            //System.out.println("  seek " + inputToString(inputMode, term));
                            IntsRefFSTEnum.InputOutput<T> seekResult;
                            if (Random.Next(3) == 0)
                            {
                                if (LuceneTestCase.VERBOSE)
                                {
                                    Console.WriteLine("  do non-exist seekExact term=" + InputToString(inputMode, term));
                                }
                                seekResult = fstEnum_.SeekExact(term);
                                pos = -1;
                            }
                            else if (Random.NextBoolean())
                            {
                                if (LuceneTestCase.VERBOSE)
                                {
                                    Console.WriteLine("  do non-exist seekFloor term=" + InputToString(inputMode, term));
                                }
                                seekResult = fstEnum_.SeekFloor(term);
                                pos--;
                            }
                            else
                            {
                                if (LuceneTestCase.VERBOSE)
                                {
                                    Console.WriteLine("  do non-exist seekCeil term=" + InputToString(inputMode, term));
                                }
                                seekResult = fstEnum_.SeekCeil(term);
                            }

                            if (pos != -1 && pos < Pairs.Count)
                            {
                                //System.out.println("    got " + inputToString(inputMode,seekResult.input) + " output=" + fst.Outputs.outputToString(seekResult.Output));
                                Assert.IsNotNull(seekResult, "got null but expected term=" + InputToString(inputMode, Pairs[pos].Input));
                                if (LuceneTestCase.VERBOSE)
                                {
                                    Console.WriteLine("    got " + InputToString(inputMode, seekResult.Input));
                                }
                                Assert.AreEqual(Pairs[pos].Input, seekResult.Input, "expected " + InputToString(inputMode, Pairs[pos].Input) + " but got " + InputToString(inputMode, seekResult.Input));
                                Assert.IsTrue(OutputsEqual(Pairs[pos].Output, seekResult.Output));
                            }
                            else
                            {
                                // seeked before start or beyond end
                                //System.out.println("seek=" + seekTerm);
                                Assert.IsNull(seekResult, "expected null but got " + (seekResult == null ? "null" : InputToString(inputMode, seekResult.Input)));
                                if (LuceneTestCase.VERBOSE)
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
                    InputOutput<T> pair = Pairs[Random.Next(Pairs.Count)];
                    IntsRefFSTEnum.InputOutput<T> seekResult;
                    if (Random.Next(3) == 2)
                    {
                        if (LuceneTestCase.VERBOSE)
                        {
                            Console.WriteLine("  do exists seekExact term=" + InputToString(inputMode, pair.Input));
                        }
                        seekResult = fstEnum_.SeekExact(pair.Input);
                    }
                    else if (Random.NextBoolean())
                    {
                        if (LuceneTestCase.VERBOSE)
                        {
                            Console.WriteLine("  do exists seekFloor " + InputToString(inputMode, pair.Input));
                        }
                        seekResult = fstEnum_.SeekFloor(pair.Input);
                    }
                    else
                    {
                        if (LuceneTestCase.VERBOSE)
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

            if (LuceneTestCase.VERBOSE)
            {
                Console.WriteLine("TEST: mixed next/seek");
            }

            // test mixed next/seek
            num_ = LuceneTestCase.AtLeast(Random, 100);
            for (int iter = 0; iter < num_; iter++)
            {
                if (LuceneTestCase.VERBOSE)
                {
                    Console.WriteLine("TEST: iter " + iter);
                }
                // reset:
                fstEnum_ = new IntsRefFSTEnum<T>(fst);
                int upto = -1;
                while (true)
                {
                    bool isDone = false;
                    if (upto == Pairs.Count - 1 || Random.NextBoolean())
                    {
                        // next
                        upto++;
                        if (LuceneTestCase.VERBOSE)
                        {
                            Console.WriteLine("  do next");
                        }
                        isDone = fstEnum_.Next() == null;
                    }
                    else if (upto != -1 && upto < 0.75 * Pairs.Count && Random.NextBoolean())
                    {
                        int attempt = 0;
                        for (; attempt < 10; attempt++)
                        {
                            IntsRef term = ToIntsRef(GetRandomString(Random), inputMode);
                            if (!termsMap.ContainsKey(term) && term.CompareTo(Pairs[upto].Input) > 0)
                            {
                                int pos = Pairs.BinarySearch(new InputOutput<T>(term, default(T)));
                                Debug.Assert(pos < 0);
                                upto = -(pos + 1);

                                if (Random.NextBoolean())
                                {
                                    upto--;
                                    Assert.IsTrue(upto != -1);
                                    if (LuceneTestCase.VERBOSE)
                                    {
                                        Console.WriteLine("  do non-exist seekFloor(" + InputToString(inputMode, term) + ")");
                                    }
                                    isDone = fstEnum_.SeekFloor(term) == null;
                                }
                                else
                                {
                                    if (LuceneTestCase.VERBOSE)
                                    {
                                        Console.WriteLine("  do non-exist seekCeil(" + InputToString(inputMode, term) + ")");
                                    }
                                    isDone = fstEnum_.SeekCeil(term) == null;
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
                        int inc = Random.Next(Pairs.Count - upto - 1);
                        upto += inc;
                        if (upto == -1)
                        {
                            upto = 0;
                        }

                        if (Random.NextBoolean())
                        {
                            if (LuceneTestCase.VERBOSE)
                            {
                                Console.WriteLine("  do seekCeil(" + InputToString(inputMode, Pairs[upto].Input) + ")");
                            }
                            isDone = fstEnum_.SeekCeil(Pairs[upto].Input) == null;
                        }
                        else
                        {
                            if (LuceneTestCase.VERBOSE)
                            {
                                Console.WriteLine("  do seekFloor(" + InputToString(inputMode, Pairs[upto].Input) + ")");
                            }
                            isDone = fstEnum_.SeekFloor(Pairs[upto].Input) == null;
                        }
                    }
                    if (LuceneTestCase.VERBOSE)
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

                    if (upto == Pairs.Count)
                    {
                        Assert.IsTrue(isDone);
                        break;
                    }
                    else
                    {
                        Assert.IsFalse(isDone);
                        Assert.AreEqual(Pairs[upto].Input, fstEnum_.Current.Input);
                        Assert.IsTrue(OutputsEqual(Pairs[upto].Output, fstEnum_.Current.Output));

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
            internal int Count;
            internal S Output;
            internal S FinalOutput;
            internal bool IsLeaf = true;
            internal bool IsFinal;
        }

        // FST is pruned
        private void VerifyPruned(int inputMode, FST<T> fst, int prune1, int prune2)
        {
            if (LuceneTestCase.VERBOSE)
            {
                Console.WriteLine("TEST: now verify pruned " + Pairs.Count + " terms; outputs=" + Outputs);
                foreach (InputOutput<T> pair in Pairs)
                {
                    Console.WriteLine("  " + InputToString(inputMode, pair.Input) + ": " + Outputs.OutputToString(pair.Output));
                }
            }

            // To validate the FST, we brute-force compute all prefixes
            // in the terms, matched to their "common" outputs, prune that
            // set according to the prune thresholds, then assert the FST
            // matches that same set.

            // NOTE: Crazy RAM intensive!!

            //System.out.println("TEST: tally prefixes");

            // build all prefixes
            IDictionary<IntsRef, CountMinOutput<T>> prefixes = new HashMap<IntsRef, CountMinOutput<T>>();
            IntsRef scratch = new IntsRef(10);
            foreach (InputOutput<T> pair in Pairs)
            {
                scratch.CopyInts(pair.Input);
                for (int idx = 0; idx <= pair.Input.Length; idx++)
                {
                    scratch.Length = idx;
                    CountMinOutput<T> cmo = prefixes.ContainsKey(scratch) ? prefixes[scratch] : null;
                    if (cmo == null)
                    {
                        cmo = new CountMinOutput<T>();
                        cmo.Count = 1;
                        cmo.Output = pair.Output;
                        prefixes[IntsRef.DeepCopyOf(scratch)] = cmo;
                    }
                    else
                    {
                        cmo.Count++;
                        T output1 = cmo.Output;
                        if (output1.Equals(Outputs.NoOutput))
                        {
                            output1 = Outputs.NoOutput;
                        }
                        T output2 = pair.Output;
                        if (output2.Equals(Outputs.NoOutput))
                        {
                            output2 = Outputs.NoOutput;
                        }
                        cmo.Output = Outputs.Common(output1, output2);
                    }
                    if (idx == pair.Input.Length)
                    {
                        cmo.IsFinal = true;
                        cmo.FinalOutput = cmo.Output;
                    }
                }
            }

            if (LuceneTestCase.VERBOSE)
            {
                Console.WriteLine("TEST: now prune");
            }


            // prune 'em
            // LUCENENET NOTE: Altered this a bit to go in reverse rather than use an enumerator since
            // in .NET you cannot delete records while enumerating forward through a dictionary.
            for (int i = prefixes.Count - 1; i >= 0; i--)
            {
                KeyValuePair<IntsRef, CountMinOutput<T>> ent = prefixes.ElementAt(i);
                IntsRef prefix = ent.Key;
                CountMinOutput<T> cmo = ent.Value;
                if (LuceneTestCase.VERBOSE)
                {
                    Console.WriteLine("  term prefix=" + InputToString(inputMode, prefix, false) + " count=" + cmo.Count + " isLeaf=" + cmo.IsLeaf + " output=" + Outputs.OutputToString(cmo.Output) + " isFinal=" + cmo.IsFinal);
                }
                bool keep;
                if (prune1 > 0)
                {
                    keep = cmo.Count >= prune1;
                }
                else
                {
                    Debug.Assert(prune2 > 0);
                    if (prune2 > 1 && cmo.Count >= prune2)
                    {
                        keep = true;
                    }
                    else if (prefix.Length > 0)
                    {
                        // consult our parent
                        scratch.Length = prefix.Length - 1;
                        Array.Copy(prefix.Ints, prefix.Offset, scratch.Ints, 0, scratch.Length);
                        CountMinOutput<T> cmo2 = prefixes.ContainsKey(scratch) ? prefixes[scratch] : null;
                        //System.out.println("    parent count = " + (cmo2 == null ? -1 : cmo2.count));
                        keep = cmo2 != null && ((prune2 > 1 && cmo2.Count >= prune2) || (prune2 == 1 && (cmo2.Count >= 2 || prefix.Length <= 1)));
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
                    prefixes.Remove(prefix);
                    //System.out.println("    remove");
                }
                else
                {
                    // clear isLeaf for all ancestors
                    //System.out.println("    keep");
                    scratch.CopyInts(prefix);
                    scratch.Length--;
                    while (scratch.Length >= 0)
                    {
                        CountMinOutput<T> cmo2 = prefixes.ContainsKey(scratch) ? prefixes[scratch] : null;
                        if (cmo2 != null)
                        {
                            //System.out.println("    clear isLeaf " + inputToString(inputMode, scratch));
                            cmo2.IsLeaf = false;
                        }
                        scratch.Length--;
                    }
                }
            }

            if (LuceneTestCase.VERBOSE)
            {
                Console.WriteLine("TEST: after prune");
                foreach (KeyValuePair<IntsRef, CountMinOutput<T>> ent in prefixes)
                {
                    Console.WriteLine("  " + InputToString(inputMode, ent.Key, false) + ": isLeaf=" + ent.Value.IsLeaf + " isFinal=" + ent.Value.IsFinal);
                    if (ent.Value.IsFinal)
                    {
                        Console.WriteLine("    finalOutput=" + Outputs.OutputToString(ent.Value.FinalOutput));
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
            if (LuceneTestCase.VERBOSE)
            {
                Console.WriteLine("TEST: check pruned enum");
            }
            IntsRefFSTEnum<T> fstEnum = new IntsRefFSTEnum<T>(fst);
            IntsRefFSTEnum.InputOutput<T> current;
            while ((current = fstEnum.Next()) != null)
            {
                if (LuceneTestCase.VERBOSE)
                {
                    Console.WriteLine("  fstEnum.next prefix=" + InputToString(inputMode, current.Input, false) + " output=" + Outputs.OutputToString(current.Output));
                }
                CountMinOutput<T> cmo = prefixes.ContainsKey(current.Input) ? prefixes[current.Input] : null;
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
            if (LuceneTestCase.VERBOSE)
            {
                Console.WriteLine("TEST: verify all prefixes");
            }
            int[] stopNode = new int[1];
            foreach (KeyValuePair<IntsRef, CountMinOutput<T>> ent in prefixes)
            {
                if (ent.Key.Length > 0)
                {
                    CountMinOutput<T> cmo = ent.Value;
                    T output = Run(fst, ent.Key, stopNode);
                    if (LuceneTestCase.VERBOSE)
                    {
                        Console.WriteLine("TEST: verify prefix=" + InputToString(inputMode, ent.Key, false) + " output=" + Outputs.OutputToString(cmo.Output));
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