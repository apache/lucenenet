using J2N.Collections.Generic.Extensions;
using J2N.Threading.Atomic;
using Lucene.Net.Diagnostics;
using Lucene.Net.Index.Extensions;
using Lucene.Net.Support;
using Lucene.Net.Util.Automaton;
using NUnit.Framework;
using RandomizedTesting.Generators;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Assert = Lucene.Net.TestFramework.Assert;
using Console = Lucene.Net.Util.SystemConsole;
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

    using Automaton = Lucene.Net.Util.Automaton.Automaton;
    using BytesReader = Lucene.Net.Util.Fst.FST.BytesReader;
    using CompiledAutomaton = Lucene.Net.Util.Automaton.CompiledAutomaton;
    using Directory = Lucene.Net.Store.Directory;
    using DirectoryReader = Lucene.Net.Index.DirectoryReader;
    using Document = Lucene.Net.Documents.Document;
    using Field = Lucene.Net.Documents.Field;
    using FSDirectory = Lucene.Net.Store.FSDirectory;
    using IndexInput = Lucene.Net.Store.IndexInput;
    using IndexOutput = Lucene.Net.Store.IndexOutput;
    using IndexReader = Lucene.Net.Index.IndexReader;
    using IndexSearcher = Lucene.Net.Search.IndexSearcher;
    using IndexWriter = Lucene.Net.Index.IndexWriter;
    using IndexWriterConfig = Lucene.Net.Index.IndexWriterConfig;
    using IOContext = Lucene.Net.Store.IOContext;
    using MockAnalyzer = Lucene.Net.Analysis.MockAnalyzer;
    using MockDirectoryWrapper = Lucene.Net.Store.MockDirectoryWrapper;
    using MultiFields = Lucene.Net.Index.MultiFields;
    using OpenMode = Lucene.Net.Index.OpenMode;
    using PackedInt32s = Lucene.Net.Util.Packed.PackedInt32s;
    using Pair = Lucene.Net.Util.Fst.PairOutputs<Int64, Int64>.Pair;
    using RandomIndexWriter = Lucene.Net.Index.RandomIndexWriter;
    using RegExp = Lucene.Net.Util.Automaton.RegExp;
    using Term = Lucene.Net.Index.Term;
    using TermQuery = Lucene.Net.Search.TermQuery;
    using Terms = Lucene.Net.Index.Terms;
    using TermsEnum = Lucene.Net.Index.TermsEnum;

    [SuppressCodecs("SimpleText", "Memory", "Direct")]
    [Slow]
    [TestFixture]
    public class TestFSTs : LuceneTestCase
    {

        private MockDirectoryWrapper dir;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            dir = NewMockDirectory();
            dir.PreventDoubleWrite = false;
        }

        [TearDown]
        public override void TearDown()
        {
            // can be null if we force simpletext (funky, some kind of bug in test runner maybe)
            if (dir != null)
            {
                dir.Dispose();
            }
            base.TearDown();
        }

        [Test]
        public virtual void TestBasicFSA()
        {
            string[] strings = new string[] { "station", "commotion", "elation", "elastic", "plastic", "stop", "ftop", "ftation", "stat" };
            string[] strings2 = new string[] { "station", "commotion", "elation", "elastic", "plastic", "stop", "ftop", "ftation" };
            Int32sRef[] terms = new Int32sRef[strings.Length];
            Int32sRef[] terms2 = new Int32sRef[strings2.Length];
            for (int inputMode = 0; inputMode < 2; inputMode++)
            {
                if (Verbose)
                {
                    Console.WriteLine("TEST: inputMode=" + InputModeToString(inputMode));
                }

                for (int idx = 0; idx < strings.Length; idx++)
                {
                    terms[idx] = FSTTester<object>.ToInt32sRef(strings[idx], inputMode);
                }
                for (int idx = 0; idx < strings2.Length; idx++)
                {
                    terms2[idx] = FSTTester<object>.ToInt32sRef(strings2[idx], inputMode);
                }
                Array.Sort(terms2);

                DoTest(inputMode, terms);

                // Test pre-determined FST sizes to make sure we haven't lost minimality (at least on this trivial set of terms):

                // FSA
                {
                    Outputs<object> outputs = NoOutputs.Singleton;
                    object NO_OUTPUT = outputs.NoOutput;
                    IList<InputOutput<object>> pairs = new JCG.List<InputOutput<object>>(terms2.Length);
                    foreach (Int32sRef term in terms2)
                    {
                        pairs.Add(new InputOutput<object>(term, NO_OUTPUT));
                    }
                    FST<object> fst = (new FSTTester<object>(Random, dir, inputMode, pairs, outputs, false)).DoTest(0, 0, false);
                    Assert.IsNotNull(fst);
                    Assert.AreEqual(22, fst.NodeCount);
                    Assert.AreEqual(27, fst.ArcCount);
                }

                // FST ord pos int
                {
                    PositiveInt32Outputs outputs = PositiveInt32Outputs.Singleton;
                    IList<InputOutput<Int64>> pairs = new JCG.List<InputOutput<Int64>>(terms2.Length);
                    for (int idx = 0; idx < terms2.Length; idx++)
                    {
                        pairs.Add(new InputOutput<Int64>(terms2[idx], idx));
                    }
                    FST<Int64> fst = (new FSTTester<Int64>(Random, dir, inputMode, pairs, outputs, true)).DoTest(0, 0, false);
                    Assert.IsNotNull(fst);
                    Assert.AreEqual(22, fst.NodeCount);
                    Assert.AreEqual(27, fst.ArcCount);
                }

                // FST byte sequence ord
                {
                    ByteSequenceOutputs outputs = ByteSequenceOutputs.Singleton;
                    BytesRef NO_OUTPUT = outputs.NoOutput;
                    IList<InputOutput<BytesRef>> pairs = new JCG.List<InputOutput<BytesRef>>(terms2.Length);
                    for (int idx = 0; idx < terms2.Length; idx++)
                    {
                        BytesRef output = Random.Next(30) == 17 ? NO_OUTPUT : new BytesRef(Convert.ToString(idx));
                        pairs.Add(new InputOutput<BytesRef>(terms2[idx], output));
                    }
                    FST<BytesRef> fst = (new FSTTester<BytesRef>(Random, dir, inputMode, pairs, outputs, false)).DoTest(0, 0, false);
                    Assert.IsNotNull(fst);
                    Assert.AreEqual(24, fst.NodeCount);
                    Assert.AreEqual(30, fst.ArcCount);
                }
            }
        }

        // given set of terms, test the different outputs for them
        private void DoTest(int inputMode, Int32sRef[] terms)
        {
            Array.Sort(terms);

            // NoOutputs (simple FSA)
            {
                Outputs<object> outputs = NoOutputs.Singleton;
                object NO_OUTPUT = outputs.NoOutput;
                IList<InputOutput<object>> pairs = new JCG.List<InputOutput<object>>(terms.Length);
                foreach (Int32sRef term in terms)
                {
                    pairs.Add(new InputOutput<object>(term, NO_OUTPUT));
                }
                (new FSTTester<object>(Random, dir, inputMode, pairs, outputs, false)).DoTest(true);
            }

            // PositiveIntOutput (ord)
            {
                PositiveInt32Outputs outputs = PositiveInt32Outputs.Singleton;
                IList<InputOutput<Int64>> pairs = new JCG.List<InputOutput<Int64>>(terms.Length);
                for (int idx = 0; idx < terms.Length; idx++)
                {
                    pairs.Add(new InputOutput<Int64>(terms[idx], idx));
                }
                (new FSTTester<Int64>(Random, dir, inputMode, pairs, outputs, true)).DoTest(true);
            }

            // PositiveIntOutput (random monotonically increasing positive number)
            {
                PositiveInt32Outputs outputs = PositiveInt32Outputs.Singleton;
                IList<InputOutput<Int64>> pairs = new JCG.List<InputOutput<Int64>>(terms.Length);
                long lastOutput = 0;
                for (int idx = 0; idx < terms.Length; idx++)
                {
                    long value = lastOutput + TestUtil.NextInt32(Random, 1, 1000);
                    lastOutput = value;
                    pairs.Add(new InputOutput<Int64>(terms[idx], value));
                }
                (new FSTTester<Int64>(Random, dir, inputMode, pairs, outputs, true)).DoTest(true);
            }

            // PositiveIntOutput (random positive number)
            {
                PositiveInt32Outputs outputs = PositiveInt32Outputs.Singleton;
                IList<InputOutput<Int64>> pairs = new JCG.List<InputOutput<Int64>>(terms.Length);
                for (int idx = 0; idx < terms.Length; idx++)
                {
                    pairs.Add(new InputOutput<Int64>(terms[idx], TestUtil.NextInt64(Random, 0, long.MaxValue)));
                }
                (new FSTTester<Int64>(Random, dir, inputMode, pairs, outputs, false)).DoTest(true);
            }

            // Pair<ord, (random monotonically increasing positive number>
            {
                PositiveInt32Outputs o1 = PositiveInt32Outputs.Singleton;
                PositiveInt32Outputs o2 = PositiveInt32Outputs.Singleton;
                PairOutputs<Int64, Int64> outputs = new PairOutputs<Int64, Int64>(o1, o2);
                IList<InputOutput<Pair>> pairs = new JCG.List<InputOutput<Pair>>(terms.Length);
                long lastOutput = 0;
                for (int idx = 0; idx < terms.Length; idx++)
                {
                    long value = lastOutput + TestUtil.NextInt32(Random, 1, 1000);
                    lastOutput = value;
                    pairs.Add(new InputOutput<Pair>(terms[idx], outputs.NewPair(idx, value)));
                }
                (new FSTTester<Pair>(Random, dir, inputMode, pairs, outputs, false)).DoTest(true);
            }

            // Sequence-of-bytes
            {
                ByteSequenceOutputs outputs = ByteSequenceOutputs.Singleton;
                BytesRef NO_OUTPUT = outputs.NoOutput;
                IList<InputOutput<BytesRef>> pairs = new JCG.List<InputOutput<BytesRef>>(terms.Length);
                for (int idx = 0; idx < terms.Length; idx++)
                {
                    BytesRef output = Random.Next(30) == 17 ? NO_OUTPUT : new BytesRef(Convert.ToString(idx));
                    pairs.Add(new InputOutput<BytesRef>(terms[idx], output));
                }
                (new FSTTester<BytesRef>(Random, dir, inputMode, pairs, outputs, false)).DoTest(true);
            }

            // Sequence-of-ints
            {
                Int32SequenceOutputs outputs = Int32SequenceOutputs.Singleton;
                IList<InputOutput<Int32sRef>> pairs = new JCG.List<InputOutput<Int32sRef>>(terms.Length);
                for (int idx = 0; idx < terms.Length; idx++)
                {
                    string s = Convert.ToString(idx);
                    Int32sRef output = new Int32sRef(s.Length)
                    {
                        Length = s.Length
                    };
                    for (int idx2 = 0; idx2 < output.Length; idx2++)
                    {
                        output.Int32s[idx2] = s[idx2];
                    }
                    pairs.Add(new InputOutput<Int32sRef>(terms[idx], output));
                }
                (new FSTTester<Int32sRef>(Random, dir, inputMode, pairs, outputs, false)).DoTest(true);
            }

        }


        [Test]
        public virtual void TestRandomWords()
        {
            TestRandomWords(1000, AtLeast(2));
            //testRandomWords(100, 1);
        }

        internal virtual string InputModeToString(int mode)
        {
            if (mode == 0)
            {
                return "utf8";
            }
            else
            {
                return "utf32";
            }
        }

        private void TestRandomWords(int maxNumWords, int numIter)
        {
            Random random = new J2N.Randomizer(Random.NextInt64());
            for (int iter = 0; iter < numIter; iter++)
            {
                if (Verbose)
                {
                    Console.WriteLine("\nTEST: iter " + iter);
                }
                for (int inputMode = 0; inputMode < 2; inputMode++)
                {
                    int numWords = random.Next(maxNumWords + 1);
                    ISet<Int32sRef> termsSet = new JCG.HashSet<Int32sRef>();
                    //Int32sRef[] terms = new Int32sRef[numWords]; // LUCENENET: Not Used
                    while (termsSet.Count < numWords)
                    {
                        string term = FSTTester<object>.GetRandomString(random);
                        termsSet.Add(FSTTester<object>.ToInt32sRef(term, inputMode));
                    }
                    DoTest(inputMode, termsSet.ToArray(/*new IntsRef[termsSet.Count]*/));
                }
            }
        }

        [Test]
        [Nightly]
        public virtual void TestBigSet()
        {
            TestRandomWords(TestUtil.NextInt32(Random, 50000, 60000), 1);
        }

        // Build FST for all unique terms in the test line docs
        // file, up until a time limit
        [Test]
        public virtual void TestRealTerms()
        {

            LineFileDocs docs = new LineFileDocs(Random, DefaultCodecSupportsDocValues);
            int RUN_TIME_MSEC = AtLeast(500);
            MockAnalyzer analyzer = new MockAnalyzer(Random);
            analyzer.MaxTokenLength = TestUtil.NextInt32(Random, 1, IndexWriter.MAX_TERM_LENGTH);

            IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer).SetMaxBufferedDocs(-1).SetRAMBufferSizeMB(64);
            DirectoryInfo tempDir = CreateTempDir("fstlines");
            Directory dir = NewFSDirectory(tempDir);
            IndexWriter writer = new IndexWriter(dir, conf);
            long stopTime = (J2N.Time.NanoTime() / J2N.Time.MillisecondsPerNanosecond) + RUN_TIME_MSEC; // LUCENENET: Use NanoTime() rather than CurrentTimeMilliseconds() for more accurate/reliable results
            Document doc;
            int docCount = 0;
            while ((doc = docs.NextDoc()) != null && J2N.Time.NanoTime() / J2N.Time.MillisecondsPerNanosecond < stopTime) // LUCENENET: Use NanoTime() rather than CurrentTimeMilliseconds() for more accurate/reliable results
            {
                writer.AddDocument(doc);
                docCount++;
            }
            IndexReader r = DirectoryReader.Open(writer, true);
            writer.Dispose();
            PositiveInt32Outputs outputs = PositiveInt32Outputs.Singleton;

            bool doRewrite = Random.NextBoolean();

            Builder<Int64> builder = new Builder<Int64>(FST.INPUT_TYPE.BYTE1, 0, 0, true, true, int.MaxValue, outputs, null, doRewrite, PackedInt32s.DEFAULT, true, 15);

            bool storeOrd = Random.NextBoolean();
            if (Verbose)
            {
                if (storeOrd)
                {
                    Console.WriteLine("FST stores ord");
                }
                else
                {
                    Console.WriteLine("FST stores docFreq");
                }
            }
            Terms terms = MultiFields.GetTerms(r, "body");
            if (terms != null)
            {
                Int32sRef scratchIntsRef = new Int32sRef();
                TermsEnum termsEnum = terms.GetEnumerator();
                if (Verbose)
                {
                    Console.WriteLine("TEST: got termsEnum=" + termsEnum);
                }
                BytesRef term;
                int ord = 0;

                Automaton automaton = (new RegExp(".*", RegExpSyntax.NONE)).ToAutomaton();
                TermsEnum termsEnum2 = terms.Intersect(new CompiledAutomaton(automaton, false, false), null);

                while (termsEnum.MoveNext())
                {
                    term = termsEnum.Term;
                    Assert.IsTrue(termsEnum2.MoveNext());
                    BytesRef term2 = termsEnum2.Term;
                    Assert.AreEqual(term, term2);
                    Assert.AreEqual(termsEnum.DocFreq, termsEnum2.DocFreq);
                    Assert.AreEqual(termsEnum.TotalTermFreq, termsEnum2.TotalTermFreq);

                    if (ord == 0)
                    {
                        try
                        {
                            var _ = termsEnum.Ord;
                        }
                        catch (Exception uoe) when (uoe.IsUnsupportedOperationException())
                        {
                            if (Verbose)
                            {
                                Console.WriteLine("TEST: codec doesn't support ord; FST stores docFreq");
                            }
                            storeOrd = false;
                        }
                    }
                    int output;
                    if (storeOrd)
                    {
                        output = ord;
                    }
                    else
                    {
                        output = termsEnum.DocFreq;
                    }
                    builder.Add(Util.ToInt32sRef(term, scratchIntsRef), (long)output);
                    ord++;
                    if (Verbose && ord % 100000 == 0 && LuceneTestCase.TestNightly)
                    {
                        Console.WriteLine(ord + " terms...");
                    }
                }
                FST<Int64> fst = builder.Finish();
                if (Verbose)
                {
                    Console.WriteLine("FST: " + docCount + " docs; " + ord + " terms; " + fst.NodeCount + " nodes; " + fst.ArcCount + " arcs;" + " " + fst.GetSizeInBytes() + " bytes");
                }

                if (ord > 0)
                {
                    Random random = new J2N.Randomizer(Random.NextInt64());
                    // Now confirm BytesRefFSTEnum and TermsEnum act the
                    // same:
                    BytesRefFSTEnum<Int64> fstEnum = new BytesRefFSTEnum<Int64>(fst);
                    int num = AtLeast(1000);
                    for (int iter = 0; iter < num; iter++)
                    {
                        BytesRef randomTerm = new BytesRef(FSTTester<object>.GetRandomString(random));

                        if (Verbose)
                        {
                            Console.WriteLine("TEST: seek non-exist " + randomTerm.Utf8ToString() + " " + randomTerm);
                        }

                        TermsEnum.SeekStatus seekResult = termsEnum.SeekCeil(randomTerm);
                        BytesRefFSTEnum.InputOutput<Int64> fstSeekResult = fstEnum.SeekCeil(randomTerm);

                        if (seekResult == TermsEnum.SeekStatus.END)
                        {
                            Assert.IsNull(fstSeekResult, "got " + (fstSeekResult is null ? "null" : fstSeekResult.Input.Utf8ToString()) + " but expected null");
                        }
                        else
                        {
                            AssertSame(termsEnum, fstEnum, storeOrd);
                            for (int nextIter = 0; nextIter < 10; nextIter++)
                            {
                                if (Verbose)
                                {
                                    Console.WriteLine("TEST: next");
                                    if (storeOrd)
                                    {
                                        Console.WriteLine("  ord=" + termsEnum.Ord);
                                    }
                                }
                                if (termsEnum.MoveNext())
                                {
                                    if (Verbose)
                                    {
                                        Console.WriteLine("  term=" + termsEnum.Term.Utf8ToString());
                                    }
                                    Assert.IsTrue(fstEnum.MoveNext());
                                    AssertSame(termsEnum, fstEnum, storeOrd);
                                }
                                else
                                {
                                    if (Verbose)
                                    {
                                        Console.WriteLine("  end!");
                                    }
                                    BytesRefFSTEnum.InputOutput<Int64> nextResult;
                                    if (fstEnum.MoveNext())
                                    {
                                        nextResult = fstEnum.Current;
                                        Console.WriteLine("expected null but got: input=" + nextResult.Input.Utf8ToString() + " output=" + outputs.OutputToString(nextResult.Output));
                                        Assert.Fail();
                                    }
                                    break;
                                }
                            }
                        }
                    }

                }
            }

            r.Dispose();
            dir.Dispose();
        }

        private void AssertSame(TermsEnum termsEnum, BytesRefFSTEnum<Int64> fstEnum, bool storeOrd)
        {
            if (termsEnum.Term is null)
            {
                Assert.IsNull(fstEnum.Current);
            }
            else
            {
                Assert.IsNotNull(fstEnum.Current);
                Assert.AreEqual(termsEnum.Term, fstEnum.Current.Input, termsEnum.Term.Utf8ToString() + " != " + fstEnum.Current.Input.Utf8ToString());
                if (storeOrd)
                {
                    // fst stored the ord
                    Assert.AreEqual(termsEnum.Ord, fstEnum.Current.Output, "term=" + termsEnum.Term.Utf8ToString() + " " + termsEnum.Term);
                }
                else
                {
                    // fst stored the docFreq
                    Assert.AreEqual(termsEnum.DocFreq, fstEnum.Current.Output, "term=" + termsEnum.Term.Utf8ToString() + " " + termsEnum.Term);
                }
            }
        }

        private abstract class VisitTerms<T> where T : class // LUCENENET specific - added class constraint, since we compare reference equality
        {
            private readonly string dirOut;
            private readonly string wordsFileIn;
            private readonly int inputMode;
            private readonly Outputs<T> outputs;
            private readonly Builder<T> builder;
            //private readonly bool doPack; // LUCENENET: Not used

            public VisitTerms(string dirOut, string wordsFileIn, int inputMode, int prune, Outputs<T> outputs, bool doPack, bool noArcArrays)
            {
                this.dirOut = dirOut;
                this.wordsFileIn = wordsFileIn;
                this.inputMode = inputMode;
                this.outputs = outputs;
                //this.doPack = doPack; // LUCENENET: Not used

                builder = new Builder<T>(inputMode == 0 ? FST.INPUT_TYPE.BYTE1 : FST.INPUT_TYPE.BYTE4, 0, prune, prune == 0, true, int.MaxValue, outputs, null, doPack, PackedInt32s.DEFAULT, !noArcArrays, 15);
            }

            protected internal abstract T GetOutput(Int32sRef input, int ord);

            public virtual void Run(int limit, bool verify, bool verifyByOutput)
            {
                TextReader @is = new StreamReader(new FileStream(wordsFileIn, FileMode.Open), Encoding.UTF8);
                try
                {
                    Int32sRef intsRef = new Int32sRef(10);
                    long tStart = J2N.Time.NanoTime() / J2N.Time.MillisecondsPerNanosecond; // LUCENENET: Use NanoTime() rather than CurrentTimeMilliseconds() for more accurate/reliable results
                    int ord = 0;
                    while (true)
                    {
                        string w = @is.ReadLine();
                        if (w is null)
                        {
                            break;
                        }
                        FSTTester<object>.ToInt32sRef(w, inputMode, intsRef);
                        builder.Add(intsRef, GetOutput(intsRef, ord));

                        ord++;
                        if (ord % 500000 == 0)
                        {
                            Console.WriteLine(string.Format(CultureInfo.InvariantCulture, "{0:000000.000}s: {1:000000000}...", (((J2N.Time.NanoTime() / J2N.Time.MillisecondsPerNanosecond) - tStart) / 1000.0), ord)); // LUCENENET: Use NanoTime() rather than CurrentTimeMilliseconds() for more accurate/reliable results
                        }
                        if (ord >= limit)
                        {
                            break;
                        }
                    }

                    long tMid = J2N.Time.NanoTime() / J2N.Time.MillisecondsPerNanosecond; // LUCENENET: Use NanoTime() rather than CurrentTimeMilliseconds() for more accurate/reliable results
                    Console.WriteLine(((tMid - tStart) / 1000.0) + " sec to add all terms");

                    if (Debugging.AssertsEnabled) Debugging.Assert(builder.TermCount == ord);
                    FST<T> fst = builder.Finish();
                    long tEnd = J2N.Time.NanoTime() / J2N.Time.MillisecondsPerNanosecond; // LUCENENET: Use NanoTime() rather than CurrentTimeMilliseconds() for more accurate/reliable results
                    Console.WriteLine(((tEnd - tMid) / 1000.0) + " sec to finish/pack");
                    if (fst is null)
                    {
                        Console.WriteLine("FST was fully pruned!");
                        Environment.Exit(0);
                    }

                    if (dirOut is null)
                    {
                        return;
                    }

                    Console.WriteLine(ord + " terms; " + fst.NodeCount + " nodes; " + fst.ArcCount + " arcs; " + fst.ArcWithOutputCount + " arcs w/ output; tot size " + fst.GetSizeInBytes());
                    if (fst.NodeCount < 100)
                    {
                        TextWriter w = new StreamWriter(new FileStream("out.dot", FileMode.Create), Encoding.UTF8);
                        Util.ToDot(fst, w, false, false);
                        w.Dispose();
                        Console.WriteLine("Wrote FST to out.dot");
                    }

                    Directory dir = FSDirectory.Open(new DirectoryInfo(dirOut));
                    IndexOutput @out = dir.CreateOutput("fst.bin", IOContext.DEFAULT);
                    fst.Save(@out);
                    @out.Dispose();
                    Console.WriteLine("Saved FST to fst.bin.");

                    if (!verify)
                    {
                        return;
                    }

                    /*
                    IndexInput in = dir.OpenInput("fst.bin", IOContext.DEFAULT);
                    fst = new FST<T>(in, outputs);
                    in.Dispose();
                    */

                    Console.WriteLine("\nNow verify...");

                    while (true)
                    {
                        for (int iter = 0; iter < 2; iter++)
                        {
                            @is.Dispose();
                            @is = new StreamReader(new FileStream(wordsFileIn, FileMode.Open), Encoding.UTF8);

                            ord = 0;
                            tStart = J2N.Time.NanoTime() / J2N.Time.MillisecondsPerNanosecond; // LUCENENET: Use NanoTime() rather than CurrentTimeMilliseconds() for more accurate/reliable results
                            while (true)
                            {
                                string w = @is.ReadLine();
                                if (w is null)
                                {
                                    break;
                                }
                                FSTTester<object>.ToInt32sRef(w, inputMode, intsRef);
                                if (iter == 0)
                                {
                                    T expected = GetOutput(intsRef, ord);
                                    T actual = Util.Get(fst, intsRef);
                                    if (actual is null)
                                    {
                                        throw RuntimeException.Create("unexpected null output on input=" + w);
                                    }
                                    if (!actual.Equals(expected))
                                    {
                                        throw RuntimeException.Create("wrong output (got " + outputs.OutputToString(actual) + " but expected " + outputs.OutputToString(expected) + ") on input=" + w);
                                    }
                                }
                                else
                                {
                                    // Get by output
                                    Int64 output = (Int64)(object)GetOutput(intsRef, ord);
                                    Int32sRef actual = Util.GetByOutput(fst as FST<Int64>, output);
                                    if (actual is null)
                                    {
                                        throw RuntimeException.Create("unexpected null input from output=" + output);
                                    }
                                    if (!actual.Equals(intsRef))
                                    {
                                        throw RuntimeException.Create("wrong input (got " + actual + " but expected " + intsRef + " from output=" + output);
                                    }
                                }

                                ord++;
                                if (ord % 500000 == 0)
                                {
                                    Console.WriteLine((((J2N.Time.NanoTime() / J2N.Time.MillisecondsPerNanosecond) - tStart) / 1000.0) + "s: " + ord + "..."); // LUCENENET: Use NanoTime() rather than CurrentTimeMilliseconds() for more accurate/reliable results
                                }
                                if (ord >= limit)
                                {
                                    break;
                                }
                            }

                            double totSec = (((J2N.Time.NanoTime() / J2N.Time.MillisecondsPerNanosecond) - tStart) / 1000.0); // LUCENENET: Use NanoTime() rather than CurrentTimeMilliseconds() for more accurate/reliable results
                            Console.WriteLine("Verify " + (iter == 1 ? "(by output) " : "") + "took " + totSec + " sec + (" + (int)((totSec * 1000000000 / ord)) + " nsec per lookup)");

                            if (!verifyByOutput)
                            {
                                break;
                            }
                        }

                        // NOTE: comment out to profile lookup...
                        break;
                    }

                }
                finally
                {
                    @is.Dispose();
                }
            }
        }

        // TODO: try experiment: reverse terms before
        // compressing -- how much smaller?

        // TODO: can FST be used to index all internal substrings,
        // mapping to term?

        // java -cp ../build/codecs/classes/java:../test-framework/lib/randomizedtesting-runner-*.jar:../build/core/classes/test:../build/core/classes/test-framework:../build/core/classes/java:../build/test-framework/classes/java:../test-framework/lib/junit-4.10.jar Lucene.Net.Util.Fst.TestFSTs /xold/tmp/allTerms3.txt out
        /*public static void Main(string[] args)
        {
            int prune = 0;
            int limit = int.MaxValue;
            int inputMode = 0; // utf8
            bool storeOrds = false;
            bool storeDocFreqs = false;
            bool verify = true;
            bool doPack = false;
            bool noArcArrays = false;
            string wordsFileIn = null;
            string dirOut = null;

            int idx = 0;
            while (idx < args.Length)
            {
                if (args[idx].Equals("-prune", StringComparison.Ordinal))
                {
                    prune = Convert.ToInt32(args[1 + idx]);
                    idx++;
                }
                else if (args[idx].Equals("-limit", StringComparison.Ordinal))
                {
                    limit = Convert.ToInt32(args[1 + idx]);
                    idx++;
                }
                else if (args[idx].Equals("-utf8", StringComparison.Ordinal))
                {
                    inputMode = 0;
                }
                else if (args[idx].Equals("-utf32", StringComparison.Ordinal))
                {
                    inputMode = 1;
                }
                else if (args[idx].Equals("-docFreq", StringComparison.Ordinal))
                {
                    storeDocFreqs = true;
                }
                else if (args[idx].Equals("-noArcArrays", StringComparison.Ordinal))
                {
                    noArcArrays = true;
                }
                else if (args[idx].Equals("-ords", StringComparison.Ordinal))
                {
                    storeOrds = true;
                }
                else if (args[idx].Equals("-noverify", StringComparison.Ordinal))
                {
                    verify = false;
                }
                else if (args[idx].Equals("-pack", StringComparison.Ordinal))
                {
                    doPack = true;
                }
                else if (args[idx].StartsWith("-", StringComparison.Ordinal))
                {
                    Console.Error.WriteLine("Unrecognized option: " + args[idx]);
                    Environment.Exit(-1);
                }
                else
                {
                    if (wordsFileIn is null)
                    {
                        wordsFileIn = args[idx];
                    }
                    else if (dirOut is null)
                    {
                        dirOut = args[idx];
                    }
                    else
                    {
                        Console.Error.WriteLine("Too many arguments, expected: input [output]");
                        Environment.Exit(-1);
                    }
                }
                idx++;
            }

            if (wordsFileIn is null)
            {
                Console.Error.WriteLine("No input file.");
                Environment.Exit(-1);
            }

            // ord benefits from share, docFreqs don't:

            if (storeOrds && storeDocFreqs)
            {
                // Store both ord & docFreq:
                PositiveIntOutputs o1 = PositiveIntOutputs.Singleton;
                PositiveIntOutputs o2 = PositiveIntOutputs.Singleton;
                PairOutputs<long, long> outputs = new PairOutputs<long, long>(o1, o2);
                new VisitTermsAnonymousClass(dirOut, wordsFileIn, inputMode, prune, outputs, doPack, noArcArrays).Run(limit, verify, false);
            }
            else if (storeOrds)
            {
                // Store only ords
                PositiveIntOutputs outputs = PositiveIntOutputs.Singleton;
                new VisitTermsAnonymousClass2(dirOut, wordsFileIn, inputMode, prune, outputs, doPack, noArcArrays).Run(limit, verify, true);
            }
            else if (storeDocFreqs)
            {
                // Store only docFreq
                PositiveIntOutputs outputs = PositiveIntOutputs.Singleton;
                new VisitTermsAnonymousClass3(dirOut, wordsFileIn, inputMode, prune, outputs, doPack, noArcArrays).Run(limit, verify, false);
            }
            else
            {
                // Store nothing
                NoOutputs outputs = NoOutputs.Singleton;
                object NO_OUTPUT = outputs.NoOutput;
                new VisitTermsAnonymousClass4(dirOut, wordsFileIn, inputMode, prune, outputs, doPack, noArcArrays, NO_OUTPUT).Run(limit, verify, false);
            }
        }*/

        private sealed class VisitTermsAnonymousClass : VisitTerms<Pair>
        {
            private readonly PairOutputs<Int64, Int64> outputs;

            public VisitTermsAnonymousClass(string dirOut, string wordsFileIn, int inputMode, int prune, PairOutputs<Int64, Int64> outputs, bool doPack, bool noArcArrays)
                : base(dirOut, wordsFileIn, inputMode, prune, outputs, doPack, noArcArrays)
            {
                this.outputs = outputs;
            }

            internal Random rand;
            protected internal override Pair GetOutput(Int32sRef input, int ord)
            {
                if (ord == 0)
                {
                    rand = new J2N.Randomizer(17);
                }
                return outputs.NewPair(ord, TestUtil.NextInt32(rand, 1, 5000));
            }
        }

        private sealed class VisitTermsAnonymousClass2 : VisitTerms<Int64>
        {
            public VisitTermsAnonymousClass2(string dirOut, string wordsFileIn, int inputMode, int prune, PositiveInt32Outputs outputs, bool doPack, bool noArcArrays)
                : base(dirOut, wordsFileIn, inputMode, prune, outputs, doPack, noArcArrays)
            {
            }

            protected internal override Int64 GetOutput(Int32sRef input, int ord)
            {
                return ord;
            }
        }

        private sealed class VisitTermsAnonymousClass3 : VisitTerms<Int64>
        {
            public VisitTermsAnonymousClass3(string dirOut, string wordsFileIn, int inputMode, int prune, PositiveInt32Outputs outputs, bool doPack, bool noArcArrays)
                : base(dirOut, wordsFileIn, inputMode, prune, outputs, doPack, noArcArrays)
            {
            }

            internal Random rand;
            protected internal override Int64 GetOutput(Int32sRef input, int ord)
            {
                if (ord == 0)
                {
                    rand = new J2N.Randomizer(17);
                }
                return (long)TestUtil.NextInt32(rand, 1, 5000);
            }
        }

        private sealed class VisitTermsAnonymousClass4 : VisitTerms<object>
        {
            private readonly object NO_OUTPUT;

            public VisitTermsAnonymousClass4(string dirOut, string wordsFileIn, int inputMode, int prune, NoOutputs outputs, bool doPack, bool noArcArrays, object NO_OUTPUT)
                : base(dirOut, wordsFileIn, inputMode, prune, outputs, doPack, noArcArrays)
            {
                this.NO_OUTPUT = NO_OUTPUT;
            }

            protected internal override object GetOutput(Int32sRef input, int ord)
            {
                return NO_OUTPUT;
            }
        }

        [Test]
        public virtual void TestSingleString()
        {
            Outputs<object> outputs = NoOutputs.Singleton;
            Builder<object> b = new Builder<object>(FST.INPUT_TYPE.BYTE1, outputs);
            b.Add(Util.ToInt32sRef(new BytesRef("foobar"), new Int32sRef()), outputs.NoOutput);
            BytesRefFSTEnum<object> fstEnum = new BytesRefFSTEnum<object>(b.Finish());
            Assert.IsNull(fstEnum.SeekFloor(new BytesRef("foo")));
            Assert.IsNull(fstEnum.SeekCeil(new BytesRef("foobaz")));
        }


        [Test]
        public virtual void TestDuplicateFSAString()
        {
            string str = "foobar";
            Outputs<object> outputs = NoOutputs.Singleton;
            Builder<object> b = new Builder<object>(FST.INPUT_TYPE.BYTE1, outputs);
            Int32sRef ints = new Int32sRef();
            for (int i = 0; i < 10; i++)
            {
                b.Add(Util.ToInt32sRef(new BytesRef(str), ints), outputs.NoOutput);
            }
            FST<object> fst = b.Finish();

            // count the input paths
            int count = 0;
            BytesRefFSTEnum<object> fstEnum = new BytesRefFSTEnum<object>(fst);
            while (fstEnum.MoveNext())
            {
                count++;
            }
            Assert.AreEqual(1, count);

            Assert.IsNotNull(Util.Get(fst, new BytesRef(str)));
            Assert.IsNull(Util.Get(fst, new BytesRef("foobaz")));
        }

        /*
        public void testTrivial() throws Exception {
    
          // Get outputs -- passing true means FST will share
          // (delta code) the outputs.  this should result in
          // smaller FST if the outputs grow monotonically.  But
          // if numbers are "random", false should give smaller
          // final size:
          final NoOutputs outputs = NoOutputs.getSingleton();
    
          String[] strings = new String[] {"station", "commotion", "elation", "elastic", "plastic", "stop", "ftop", "ftation", "stat"};
    
          final Builder<Object> builder = new Builder<Object>(FST.INPUT_TYPE.BYTE1,
                                                              0, 0,
                                                              true,
                                                              true,
                                                              Integer.MAX_VALUE,
                                                              outputs,
                                                              null,
                                                              true);
          Array.Sort(strings, StringComparer.Ordinal);
          final IntsRef scratch = new IntsRef();
          for(String s : strings) {
            builder.Add(Util.ToIntsRef(new BytesRef(s), scratch), outputs.getNoOutput());
          }
          final FST<Object> fst = builder.Finish();
          System.out.println("DOT before rewrite");
          Writer w = new OutputStreamWriter(new FileOutputStream("/mnt/scratch/before.dot"));
          Util.toDot(fst, w, false, false);
          w.Dispose();
    
          final FST<Object> rewrite = new FST<Object>(fst, 1, 100);
    
          System.out.println("DOT after rewrite");
          w = new OutputStreamWriter(new FileOutputStream("/mnt/scratch/after.dot"));
          Util.toDot(rewrite, w, false, false);
          w.Dispose();
        }
        */

        [Test]
        public virtual void TestSimple()
        {

            // Get outputs -- passing true means FST will share
            // (delta code) the outputs.  this should result in
            // smaller FST if the outputs grow monotonically.  But
            // if numbers are "random", false should give smaller
            // final size:
            PositiveInt32Outputs outputs = PositiveInt32Outputs.Singleton;

            // Build an FST mapping BytesRef -> Long
            Builder<Int64> builder = new Builder<Int64>(FST.INPUT_TYPE.BYTE1, outputs);

            BytesRef a = new BytesRef("a");
            BytesRef b = new BytesRef("b");
            BytesRef c = new BytesRef("c");

            builder.Add(Util.ToInt32sRef(a, new Int32sRef()), 17L);
            builder.Add(Util.ToInt32sRef(b, new Int32sRef()), 42L);
            builder.Add(Util.ToInt32sRef(c, new Int32sRef()), 13824324872317238L);

            FST<Int64> fst = builder.Finish();

            Assert.AreEqual(13824324872317238L, Util.Get(fst, c));
            Assert.AreEqual(42, Util.Get(fst, b));
            Assert.AreEqual(17, Util.Get(fst, a));

            BytesRefFSTEnum<Int64> fstEnum = new BytesRefFSTEnum<Int64>(fst);
            BytesRefFSTEnum.InputOutput<Int64> seekResult;
            seekResult = fstEnum.SeekFloor(a);
            Assert.IsNotNull(seekResult);
            Assert.AreEqual(17, seekResult.Output);

            // goes to a
            seekResult = fstEnum.SeekFloor(new BytesRef("aa"));
            Assert.IsNotNull(seekResult);
            Assert.AreEqual(17, seekResult.Output);

            // goes to b
            seekResult = fstEnum.SeekCeil(new BytesRef("aa"));
            Assert.IsNotNull(seekResult);
            Assert.AreEqual(b, seekResult.Input);
            Assert.AreEqual(42, seekResult.Output);

            Assert.AreEqual(Util.ToInt32sRef(new BytesRef("c"), new Int32sRef()), Util.GetByOutput(fst, 13824324872317238L));
            Assert.IsNull(Util.GetByOutput(fst, 47));
            Assert.AreEqual(Util.ToInt32sRef(new BytesRef("b"), new Int32sRef()), Util.GetByOutput(fst, 42));
            Assert.AreEqual(Util.ToInt32sRef(new BytesRef("a"), new Int32sRef()), Util.GetByOutput(fst, 17));
        }

        [Test]
        public virtual void TestPrimaryKeys()
        {
            Directory dir = NewDirectory();

            for (int cycle = 0; cycle < 2; cycle++)
            {
                if (Verbose)
                {
                    Console.WriteLine("TEST: cycle=" + cycle);
                }
                RandomIndexWriter w = new RandomIndexWriter(Random, dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)).SetOpenMode(OpenMode.CREATE));
                Document doc = new Document();
                Field idField = NewStringField("id", "", Field.Store.NO);
                doc.Add(idField);

                int NUM_IDS = AtLeast(200);
                //final int NUM_IDS = (int) (377 * (1.0+random.nextDouble()));
                if (Verbose)
                {
                    Console.WriteLine("TEST: NUM_IDS=" + NUM_IDS);
                }
                ISet<string> allIDs = new JCG.HashSet<string>();
                for (int id = 0; id < NUM_IDS; id++)
                {
                    string idString;
                    if (cycle == 0)
                    {
                        // PKs are assigned sequentially
                        idString = string.Format(CultureInfo.InvariantCulture, "{0:0000000}", id);
                    }
                    else
                    {
                        while (true)
                        {
                            string s = Convert.ToString(Random.NextInt64(), CultureInfo.InvariantCulture);
                            if (!allIDs.Contains(s))
                            {
                                idString = s;
                                break;
                            }
                        }
                    }
                    allIDs.Add(idString);
                    idField.SetStringValue(idString);
                    w.AddDocument(doc);
                }

                //w.forceMerge(1);

                // turn writer into reader:
                IndexReader r = w.GetReader();
                IndexSearcher idxS = NewSearcher(r);
                w.Dispose();

                IList<string> allIDsList = new JCG.List<string>(allIDs);
                IList<string> sortedAllIDsList = new JCG.List<string>(allIDsList);
                CollectionUtil.TimSort(sortedAllIDsList);

                // Sprinkle in some non-existent PKs:
                ISet<string> outOfBounds = new JCG.HashSet<string>();
                for (int idx = 0; idx < NUM_IDS / 10; idx++)
                {
                    string idString;
                    if (cycle == 0)
                    {
                        idString = string.Format(CultureInfo.InvariantCulture, "{0:0000000}", (NUM_IDS + idx));
                    }
                    else
                    {
                        while (true)
                        {
                            idString = Convert.ToString(Random.NextInt64(), CultureInfo.InvariantCulture);
                            if (!allIDs.Contains(idString))
                            {
                                break;
                            }
                        }
                    }
                    outOfBounds.Add(idString);
                    allIDsList.Add(idString);
                }

                // Verify w/ TermQuery
                for (int iter = 0; iter < 2 * NUM_IDS; iter++)
                {
                    string id = allIDsList[Random.Next(allIDsList.Count)];
                    bool exists = !outOfBounds.Contains(id);
                    if (Verbose)
                    {
                        Console.WriteLine("TEST: TermQuery " + (exists ? "" : "non-exist ") + " id=" + id);
                    }
                    Assert.AreEqual(exists ? 1 : 0, idxS.Search(new TermQuery(new Term("id", id)), 1).TotalHits, (exists ? "" : "non-exist ") + "id=" + id);
                }

                // Verify w/ MultiTermsEnum
                TermsEnum termsEnum = MultiFields.GetTerms(r, "id").GetEnumerator();
                for (int iter = 0; iter < 2 * NUM_IDS; iter++)
                {
                    string id;
                    string nextID;
                    bool exists;

                    if (Random.NextBoolean())
                    {
                        id = allIDsList[Random.Next(allIDsList.Count)];
                        exists = !outOfBounds.Contains(id);
                        nextID = null;
                        if (Verbose)
                        {
                            Console.WriteLine("TEST: exactOnly " + (exists ? "" : "non-exist ") + "id=" + id);
                        }
                    }
                    else
                    {
                        // Pick ID between two IDs:
                        exists = false;
                        int idv = Random.Next(NUM_IDS - 1);
                        if (cycle == 0)
                        {
                            id = string.Format(CultureInfo.InvariantCulture, "{0:0000000}a", idv);
                            nextID = string.Format(CultureInfo.InvariantCulture, "{0:0000000}", idv + 1);
                        }
                        else
                        {
                            id = sortedAllIDsList[idv] + "a";
                            nextID = sortedAllIDsList[idv + 1];
                        }
                        if (Verbose)
                        {
                            Console.WriteLine("TEST: not exactOnly id=" + id + " nextID=" + nextID);
                        }
                    }

                    TermsEnum.SeekStatus status;
                    if (nextID is null)
                    {
                        if (termsEnum.SeekExact(new BytesRef(id)))
                        {
                            status = TermsEnum.SeekStatus.FOUND;
                        }
                        else
                        {
                            status = TermsEnum.SeekStatus.NOT_FOUND;
                        }
                    }
                    else
                    {
                        status = termsEnum.SeekCeil(new BytesRef(id));
                    }

                    if (nextID != null)
                    {
                        Assert.AreEqual(TermsEnum.SeekStatus.NOT_FOUND, status);
                        Assert.AreEqual(new BytesRef(nextID), termsEnum.Term, "expected=" + nextID + " actual=" + termsEnum.Term.Utf8ToString());
                    }
                    else if (!exists)
                    {
                        Assert.IsTrue(status == TermsEnum.SeekStatus.NOT_FOUND || status == TermsEnum.SeekStatus.END);
                    }
                    else
                    {
                        Assert.AreEqual(TermsEnum.SeekStatus.FOUND, status);
                    }
                }

                r.Dispose();
            }
            dir.Dispose();
        }

        [Test]
        public virtual void TestRandomTermLookup()
        {
            Directory dir = NewDirectory();

            RandomIndexWriter w = new RandomIndexWriter(Random, dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random))
                .SetOpenMode(OpenMode.CREATE));
            Document doc = new Document();
            Field f = NewStringField("field", "", Field.Store.NO);
            doc.Add(f);

            int NUM_TERMS = (int)(1000 * RandomMultiplier * (1 + Random.NextDouble()));
            if (Verbose)
            {
                Console.WriteLine("TEST: NUM_TERMS=" + NUM_TERMS);
            }

            ISet<string> allTerms = new JCG.HashSet<string>();
            while (allTerms.Count < NUM_TERMS)
            {
                allTerms.Add(FSTTester<object>.SimpleRandomString(Random));
            }

            foreach (string term in allTerms)
            {
                f.SetStringValue(term);
                w.AddDocument(doc);
            }

            // turn writer into reader:
            if (Verbose)
            {
                Console.WriteLine("TEST: get reader");
            }
            IndexReader r = w.GetReader();
            if (Verbose)
            {
                Console.WriteLine("TEST: got reader=" + r);
            }
            IndexSearcher s = NewSearcher(r);
            w.Dispose();

            IList<string> allTermsList = new JCG.List<string>(allTerms);
            allTermsList.Shuffle(Random);

            // verify exact lookup
            foreach (string term in allTermsList)
            {
                if (Verbose)
                {
                    Console.WriteLine("TEST: term=" + term);
                }
                Assert.AreEqual(s.Search(new TermQuery(new Term("field", term)), 1).TotalHits, 1, "term=" + term);
            }

            r.Dispose();
            dir.Dispose();
        }


        /// <summary>
        /// Test state expansion (array format) on close-to-root states. Creates
        /// synthetic input that has one expanded state on each level.
        /// </summary>
        /// <seealso cref= "https://issues.apache.org/jira/browse/LUCENE-2933" </seealso>
        [Test]
        public virtual void TestExpandedCloseToRoot()
        {
            // Sanity check.
            Assert.IsTrue(FST.FIXED_ARRAY_NUM_ARCS_SHALLOW < FST.FIXED_ARRAY_NUM_ARCS_DEEP);
            Assert.IsTrue(FST.FIXED_ARRAY_SHALLOW_DISTANCE >= 0);

            SyntheticData s = new SyntheticData();

            IList<string> @out = new JCG.List<string>();
            StringBuilder b = new StringBuilder();
            s.Generate(@out, b, 'a', 'i', 10);
            string[] input = @out.ToArray();
            Array.Sort(input, StringComparer.Ordinal);
            FST<object> fst = s.Compile(input);
            FST.Arc<object> arc = fst.GetFirstArc(new FST.Arc<object>());
            s.VerifyStateAndBelow(fst, arc, 1);
        }

        private class SyntheticData
        {
            public FST<object> Compile(string[] lines)
            {
                NoOutputs outputs = Fst.NoOutputs.Singleton;
                object nothing = outputs.NoOutput;
                Builder<object> b = new Builder<object>(FST.INPUT_TYPE.BYTE1, outputs);

                int line = 0;
                BytesRef term = new BytesRef();
                Int32sRef scratchIntsRef = new Int32sRef();

                while (line < lines.Length)
                {
                    string w = lines[line++];
                    if (w is null)
                    {
                        break;
                    }
                    term.CopyChars(w);
                    b.Add(Util.ToInt32sRef(term, scratchIntsRef), nothing);
                }

                return b.Finish();
            }

            public void Generate(IList<string> @out, StringBuilder b, char from, char to, int depth)
            {
                if (depth == 0 || from == to)
                {
                    string seq = b.ToString() + "_" + @out.Count + "_end";
                    @out.Add(seq);
                }
                else
                {
                    for (char c = from; c <= to; c++)
                    {
                        b.Append(c);
                        Generate(@out, b, from, c == to ? to : from, depth - 1);
                        b.Remove(b.Length - 1, 1);//remove last char
                    }
                }
            }

            public int VerifyStateAndBelow(FST<object> fst, FST.Arc<object> arc, int depth)
            {
                if (FST<object>.TargetHasArcs(arc))
                {
                    int childCount = 0;
                    BytesReader fstReader = fst.GetBytesReader();
                    for (arc = fst.ReadFirstTargetArc(arc, arc, fstReader); ;
                        arc = fst.ReadNextArc(arc, fstReader), childCount++)
                    {
                        bool expanded = fst.IsExpandedTarget(arc, fstReader);
                        int children = VerifyStateAndBelow(fst, new FST.Arc<object>().CopyFrom(arc), depth + 1);

                        Assert.AreEqual(expanded, (depth <= FST.FIXED_ARRAY_SHALLOW_DISTANCE && children >= FST.FIXED_ARRAY_NUM_ARCS_SHALLOW) || children >= FST.FIXED_ARRAY_NUM_ARCS_DEEP);
                        if (arc.IsLast)
                        {
                            break;
                        }
                    }
                    return childCount;
                }
                return 0;
            }
        }

        [Test]
        public virtual void TestFinalOutputOnEndState()
        {
            PositiveInt32Outputs outputs = PositiveInt32Outputs.Singleton;

            Builder<Int64> builder = new Builder<Int64>(FST.INPUT_TYPE.BYTE4, 2, 0, true, true, int.MaxValue, outputs, null, Random.NextBoolean(), PackedInt32s.DEFAULT, true, 15);
            builder.Add(Util.ToUTF32("stat", new Int32sRef()), 17L);
            builder.Add(Util.ToUTF32("station", new Int32sRef()), 10L);
            FST<Int64> fst = builder.Finish();
            //Writer w = new OutputStreamWriter(new FileOutputStream("/x/tmp3/out.dot"));
            StringWriter w = new StringWriter();
            Util.ToDot(fst, w, false, false);
            w.Dispose();
            //System.out.println(w.toString());
            Assert.IsTrue(w.ToString().IndexOf("label=\"t/[7]\"", StringComparison.Ordinal) != -1);
        }

        [Test]
        public virtual void TestInternalFinalState()
        {
            PositiveInt32Outputs outputs = PositiveInt32Outputs.Singleton;
            bool willRewrite = Random.NextBoolean();
            Builder<Int64> builder = new Builder<Int64>(FST.INPUT_TYPE.BYTE1, 0, 0, true, true, int.MaxValue, outputs, null, willRewrite, PackedInt32s.DEFAULT, true, 15);
            builder.Add(Util.ToInt32sRef(new BytesRef("stat"), new Int32sRef()), outputs.NoOutput);
            builder.Add(Util.ToInt32sRef(new BytesRef("station"), new Int32sRef()), outputs.NoOutput);
            FST<Int64> fst = builder.Finish();
            StringWriter w = new StringWriter();
            //Writer w = new OutputStreamWriter(new FileOutputStream("/x/tmp/out.dot"));
            Util.ToDot(fst, w, false, false);
            w.Dispose();
            //System.out.println(w.toString());

            // check for accept state at label t
            Assert.IsTrue(w.ToString().IndexOf("[label=\"t\" style=\"bold\"", StringComparison.Ordinal) != -1);
            // check for accept state at label n
            Assert.IsTrue(w.ToString().IndexOf("[label=\"n\" style=\"bold\"", StringComparison.Ordinal) != -1);
        }

        // Make sure raw FST can differentiate between final vs
        // non-final end nodes
        [Test]
        public virtual void TestNonFinalStopNode()
        {
            PositiveInt32Outputs outputs = PositiveInt32Outputs.Singleton;
            long nothing = outputs.NoOutput;
            Builder<Int64> b = new Builder<Int64>(FST.INPUT_TYPE.BYTE1, outputs);

            FST<Int64> fst = new FST<Int64>(FST.INPUT_TYPE.BYTE1, outputs, false, PackedInt32s.COMPACT, true, 15);

            Builder.UnCompiledNode<Int64> rootNode = new Builder.UnCompiledNode<Int64>(b, 0);

            // Add final stop node
            {
                Builder.UnCompiledNode<Int64> node = new Builder.UnCompiledNode<Int64>(b, 0);
                node.IsFinal = true;
                rootNode.AddArc('a', node);
                Builder.CompiledNode frozen = new Builder.CompiledNode();
                frozen.Node = fst.AddNode(node);
                rootNode.Arcs[0].NextFinalOutput = 17L;
                rootNode.Arcs[0].IsFinal = true;
                rootNode.Arcs[0].Output = nothing;
                rootNode.Arcs[0].Target = frozen;
            }

            // Add non-final stop node
            {
                Builder.UnCompiledNode<Int64> node = new Builder.UnCompiledNode<Int64>(b, 0);
                rootNode.AddArc('b', node);
                Builder.CompiledNode frozen = new Builder.CompiledNode();
                frozen.Node = fst.AddNode(node);
                rootNode.Arcs[1].NextFinalOutput = nothing;
                rootNode.Arcs[1].Output = 42L;
                rootNode.Arcs[1].Target = frozen;
            }

            fst.Finish(fst.AddNode(rootNode));

            StringWriter w = new StringWriter();
            //Writer w = new OutputStreamWriter(new FileOutputStream("/x/tmp3/out.dot"));
            Util.ToDot(fst, w, false, false);
            w.Dispose();

            CheckStopNodes(fst, outputs);

            // Make sure it still works after save/load:
            Directory dir = NewDirectory();
            IndexOutput @out = dir.CreateOutput("fst", IOContext.DEFAULT);
            fst.Save(@out);
            @out.Dispose();

            IndexInput @in = dir.OpenInput("fst", IOContext.DEFAULT);
            FST<Int64> fst2 = new FST<Int64>(@in, outputs);
            CheckStopNodes(fst2, outputs);
            @in.Dispose();
            dir.Dispose();
        }

        private void CheckStopNodes(FST<Int64> fst, PositiveInt32Outputs outputs)
        {
            Int64 nothing = outputs.NoOutput;
            FST.Arc<Int64> startArc = fst.GetFirstArc(new FST.Arc<Int64>());
            Assert.AreEqual(nothing, startArc.Output);
            Assert.AreEqual(nothing, startArc.NextFinalOutput);

            FST.Arc<Int64> arc = fst.ReadFirstTargetArc(startArc, new FST.Arc<Int64>(), fst.GetBytesReader());
            Assert.AreEqual('a', arc.Label);
            Assert.AreEqual(17, arc.NextFinalOutput);
            Assert.IsTrue(arc.IsFinal);

            arc = fst.ReadNextArc(arc, fst.GetBytesReader());
            Assert.AreEqual('b', arc.Label);
            Assert.IsFalse(arc.IsFinal);
            Assert.AreEqual(42, arc.Output);
        }

        internal static readonly IComparer<Int64> minLongComparer = Comparer<Int64>.Create((left, right)=> left.CompareTo(right));
        
        [Test]
        public virtual void TestShortestPaths()
        {
            PositiveInt32Outputs outputs = PositiveInt32Outputs.Singleton;
            Builder<Int64> builder = new Builder<Int64>(FST.INPUT_TYPE.BYTE1, outputs);

            Int32sRef scratch = new Int32sRef();
            builder.Add(Util.ToInt32sRef(new BytesRef("aab"), scratch), 22L);
            builder.Add(Util.ToInt32sRef(new BytesRef("aac"), scratch), 7L);
            builder.Add(Util.ToInt32sRef(new BytesRef("ax"), scratch), 17L);
            FST<Int64> fst = builder.Finish();
            //Writer w = new OutputStreamWriter(new FileOutputStream("out.dot"));
            //Util.toDot(fst, w, false, false);
            //w.Dispose();

            Util.TopResults<Int64> res = Util.ShortestPaths(fst, fst.GetFirstArc(new FST.Arc<Int64>()), outputs.NoOutput, minLongComparer, 3, true);
            Assert.IsTrue(res.IsComplete);
            Assert.AreEqual(3, res.TopN.Count);
            Assert.AreEqual(Util.ToInt32sRef(new BytesRef("aac"), scratch), res.TopN[0].Input);
            Assert.AreEqual(7L, res.TopN[0].Output);

            Assert.AreEqual(Util.ToInt32sRef(new BytesRef("ax"), scratch), res.TopN[1].Input);
            Assert.AreEqual(17L, res.TopN[1].Output);

            Assert.AreEqual(Util.ToInt32sRef(new BytesRef("aab"), scratch), res.TopN[2].Input);
            Assert.AreEqual(22L, res.TopN[2].Output);
        }

        [Test]
        public virtual void TestRejectNoLimits()
        {
            PositiveInt32Outputs outputs = PositiveInt32Outputs.Singleton;
            Builder<Int64> builder = new Builder<Int64>(FST.INPUT_TYPE.BYTE1, outputs);

            Int32sRef scratch = new Int32sRef();
            builder.Add(Util.ToInt32sRef(new BytesRef("aab"), scratch), 22L);
            builder.Add(Util.ToInt32sRef(new BytesRef("aac"), scratch), 7L);
            builder.Add(Util.ToInt32sRef(new BytesRef("adcd"), scratch), 17L);
            builder.Add(Util.ToInt32sRef(new BytesRef("adcde"), scratch), 17L);

            builder.Add(Util.ToInt32sRef(new BytesRef("ax"), scratch), 17L);
            FST<Int64> fst = builder.Finish();
            AtomicInt32 rejectCount = new AtomicInt32();
            Util.TopNSearcher<Int64> searcher = new TopNSearcherAnonymousClass(fst, minLongComparer, rejectCount);

            searcher.AddStartPaths(fst.GetFirstArc(new FST.Arc<Int64>()), outputs.NoOutput, true, new Int32sRef());
            Util.TopResults<Int64> res = searcher.Search();
            Assert.AreEqual(rejectCount, 4);
            Assert.IsTrue(res.IsComplete); // rejected(4) + topN(2) <= maxQueueSize(6)

            Assert.AreEqual(1, res.TopN.Count);
            Assert.AreEqual(Util.ToInt32sRef(new BytesRef("aac"), scratch), res.TopN[0].Input);
            Assert.AreEqual(7L, res.TopN[0].Output);
            rejectCount.Value = (0);
            searcher = new TopNSearcherAnonymousClass2(fst, minLongComparer, rejectCount);

            searcher.AddStartPaths(fst.GetFirstArc(new FST.Arc<Int64>()), outputs.NoOutput, true, new Int32sRef());
            res = searcher.Search();
            Assert.AreEqual(rejectCount, 4);
            Assert.IsFalse(res.IsComplete); // rejected(4) + topN(2) > maxQueueSize(5)
        }

        private sealed class TopNSearcherAnonymousClass : Util.TopNSearcher<Int64>
        {
            private readonly AtomicInt32 rejectCount;

            public TopNSearcherAnonymousClass(FST<Int64> fst, IComparer<Int64> minLongComparer, AtomicInt32 rejectCount)
                : base(fst, 2, 6, minLongComparer)
            {
                this.rejectCount = rejectCount;
            }

            protected override bool AcceptResult(Int32sRef input, Int64 output)
            {
                bool accept = output == 7;
                if (!accept)
                {
                    rejectCount.IncrementAndGet();
                }
                return accept;
            }
        }

        private sealed class TopNSearcherAnonymousClass2 : Util.TopNSearcher<Int64>
        {
            private readonly AtomicInt32 rejectCount;

            public TopNSearcherAnonymousClass2(FST<Int64> fst, IComparer<Int64> minLongComparer, AtomicInt32 rejectCount)
                : base(fst, 2, 5, minLongComparer)
            {
                this.rejectCount = rejectCount;
            }

            protected override bool AcceptResult(Int32sRef input, Int64 output)
            {
                bool accept = output == 7;
                if (!accept)
                {
                    rejectCount.IncrementAndGet();
                }
                return accept;
            }
        }

        // compares just the weight side of the pair
        internal static readonly IComparer<Pair> minPairWeightComparer = Comparer<Pair>.Create((left, right)=> left.Output1.CompareTo(right.Output1));
             
        /// <summary>
        /// like testShortestPaths, but uses pairoutputs so we have both a weight and an output </summary>
        [Test]
        public virtual void TestShortestPathsWFST()
        {

            PairOutputs<Int64, Int64> outputs = new PairOutputs<Int64, Int64>(PositiveInt32Outputs.Singleton, PositiveInt32Outputs.Singleton); // output -  weight

            Builder<Pair> builder = new Builder<Pair>(FST.INPUT_TYPE.BYTE1, outputs);

            Int32sRef scratch = new Int32sRef();
            builder.Add(Util.ToInt32sRef(new BytesRef("aab"), scratch), outputs.NewPair(22L, 57L));
            builder.Add(Util.ToInt32sRef(new BytesRef("aac"), scratch), outputs.NewPair(7L, 36L));
            builder.Add(Util.ToInt32sRef(new BytesRef("ax"), scratch), outputs.NewPair(17L, 85L));
            FST<Pair> fst = builder.Finish();
            //Writer w = new OutputStreamWriter(new FileOutputStream("out.dot"));
            //Util.toDot(fst, w, false, false);
            //w.Dispose();

            Util.TopResults<Pair> res = Util.ShortestPaths(fst, fst.GetFirstArc(new FST.Arc<Pair>()), outputs.NoOutput, minPairWeightComparer, 3, true);
            Assert.IsTrue(res.IsComplete);
            Assert.AreEqual(3, res.TopN.Count);

            Assert.AreEqual(Util.ToInt32sRef(new BytesRef("aac"), scratch), res.TopN[0].Input);
            Assert.AreEqual(7L, res.TopN[0].Output.Output1); // weight
            Assert.AreEqual(36L, res.TopN[0].Output.Output2); // output

            Assert.AreEqual(Util.ToInt32sRef(new BytesRef("ax"), scratch), res.TopN[1].Input);
            Assert.AreEqual(17L, res.TopN[1].Output.Output1); // weight
            Assert.AreEqual(85L, res.TopN[1].Output.Output2); // output

            Assert.AreEqual(Util.ToInt32sRef(new BytesRef("aab"), scratch), res.TopN[2].Input);
            Assert.AreEqual(22L, res.TopN[2].Output.Output1); // weight
            Assert.AreEqual(57L, res.TopN[2].Output.Output2); // output
        }

        [Test]
        public virtual void TestShortestPathsRandom()
        {
            Random random = Random;
            int numWords = AtLeast(1000);

            JCG.SortedDictionary<string, long> slowCompletor = new JCG.SortedDictionary<string, long>(StringComparer.Ordinal);
            JCG.SortedSet<string> allPrefixes = new JCG.SortedSet<string>(StringComparer.Ordinal);

            PositiveInt32Outputs outputs = PositiveInt32Outputs.Singleton;
            Builder<Int64> builder = new Builder<Int64>(FST.INPUT_TYPE.BYTE1, outputs);
            Int32sRef scratch = new Int32sRef();

            for (int i = 0; i < numWords; i++)
            {
                string s;
                while (true)
                {
                    s = TestUtil.RandomSimpleString(random);
                    if (!slowCompletor.ContainsKey(s))
                    {
                        break;
                    }
                }

                for (int j = 1; j < s.Length; j++)
                {
                    allPrefixes.Add(s.Substring(0, j));
                }
                int weight = TestUtil.NextInt32(random, 1, 100); // weights 1..100
                slowCompletor[s] = (long)weight;
            }

            foreach (KeyValuePair<string, long> e in slowCompletor)
            {
                //System.out.println("add: " + e);
                builder.Add(Util.ToInt32sRef(new BytesRef(e.Key), scratch), e.Value);
            }

            FST<Int64> fst = builder.Finish();
            //System.out.println("SAVE out.dot");
            //Writer w = new OutputStreamWriter(new FileOutputStream("out.dot"));
            //Util.toDot(fst, w, false, false);
            //w.Dispose();

            BytesReader reader = fst.GetBytesReader();

            //System.out.println("testing: " + allPrefixes.Size() + " prefixes");
            foreach (string prefix in allPrefixes)
            {
                // 1. run prefix against fst, then complete by value
                //System.out.println("TEST: " + prefix);

                long prefixOutput = 0;
                FST.Arc<Int64> arc = fst.GetFirstArc(new FST.Arc<Int64>());
                for (int idx = 0; idx < prefix.Length; idx++)
                {
                    if (fst.FindTargetArc((int)prefix[idx], arc, arc, reader) is null)
                    {
                        Assert.Fail();
                    }
                    prefixOutput += arc.Output;
                }

                int topN = TestUtil.NextInt32(random, 1, 10);

                Util.TopResults<Int64> r = Util.ShortestPaths(fst, arc, fst.Outputs.NoOutput, minLongComparer, topN, true);
                Assert.IsTrue(r.IsComplete);

                // 2. go thru whole treemap (slowCompletor) and check its actually the best suggestion
                JCG.List<Util.Result<Int64>> matches = new JCG.List<Util.Result<Int64>>();

                // TODO: could be faster... but its slowCompletor for a reason
                foreach (KeyValuePair<string, long> e in slowCompletor)
                {
                    if (e.Key.StartsWith(prefix, StringComparison.Ordinal))
                    {
                        //System.out.println("  consider " + e.getKey());
                        matches.Add(new Util.Result<Int64>(Util.ToInt32sRef(new BytesRef(e.Key.Substring(prefix.Length)), new Int32sRef()), e.Value - prefixOutput));
                    }
                }

                Assert.IsTrue(matches.Count > 0);
                matches.Sort(new TieBreakByInputComparer<Int64>(minLongComparer));
                if (matches.Count > topN)
                {
                    matches.RemoveRange(topN, matches.Count - topN); // LUCENENET: Converted end index to length
                }

                Assert.AreEqual(matches.Count, r.TopN.Count);

                for (int hit = 0; hit < r.TopN.Count; hit++)
                {
                    //System.out.println("  check hit " + hit);
                    Assert.AreEqual(matches[hit].Input, r.TopN[hit].Input);
                    Assert.AreEqual(matches[hit].Output, r.TopN[hit].Output);
                }
            }
        }

        private class TieBreakByInputComparer<T> : IComparer<Util.Result<T>> where T : class // LUCENENET specific - added class constraint, since we compare reference equality
        {
            private readonly IComparer<T> comparer;
            public TieBreakByInputComparer(IComparer<T> comparer)
            {
                this.comparer = comparer;
            }

            public virtual int Compare(Util.Result<T> a, Util.Result<T> b)
            {
                int cmp = comparer.Compare(a.Output, b.Output);
                if (cmp == 0)
                {
                    return a.Input.CompareTo(b.Input);
                }
                else
                {
                    return cmp;
                }
            }
        }

        // used by slowcompletor
        internal class TwoLongs
        {
            internal Int64 a;
            internal Int64 b;

            internal TwoLongs(Int64 a, Int64 b)
            {
                this.a = a;
                this.b = b;
            }
        }

        /// <summary>
        /// like testShortestPathsRandom, but uses pairoutputs so we have both a weight and an output </summary>
        [Test]
        public virtual void TestShortestPathsWFSTRandom()
        {
            int numWords = AtLeast(1000);

            JCG.SortedDictionary<string, TwoLongs> slowCompletor = new JCG.SortedDictionary<string, TwoLongs>(StringComparer.Ordinal);
            JCG.SortedSet<string> allPrefixes = new JCG.SortedSet<string>(StringComparer.Ordinal);

            PairOutputs<Int64, Int64> outputs = new PairOutputs<Int64, Int64>(PositiveInt32Outputs.Singleton, PositiveInt32Outputs.Singleton); // output -  weight
            Builder<Pair> builder = new Builder<Pair>(FST.INPUT_TYPE.BYTE1, outputs);
            Int32sRef scratch = new Int32sRef();

            Random random = Random;
            for (int i = 0; i < numWords; i++)
            {
                string s;
                while (true)
                {
                    s = TestUtil.RandomSimpleString(random);
                    if (!slowCompletor.ContainsKey(s))
                    {
                        break;
                    }
                }

                for (int j = 1; j < s.Length; j++)
                {
                    allPrefixes.Add(s.Substring(0, j));
                }
                int weight = TestUtil.NextInt32(random, 1, 100); // weights 1..100
                int output = TestUtil.NextInt32(random, 0, 500); // outputs 0..500
                slowCompletor[s] = new TwoLongs(weight, output);
            }

            foreach (KeyValuePair<string, TwoLongs> e in slowCompletor)
            {
                //System.out.println("add: " + e);
                long weight = e.Value.a;
                long output = e.Value.b;
                builder.Add(Util.ToInt32sRef(new BytesRef(e.Key), scratch), outputs.NewPair(weight, output));
            }

            FST<Pair> fst = builder.Finish();
            //System.out.println("SAVE out.dot");
            //Writer w = new OutputStreamWriter(new FileOutputStream("out.dot"));
            //Util.toDot(fst, w, false, false);
            //w.Dispose();

            BytesReader reader = fst.GetBytesReader();

            //System.out.println("testing: " + allPrefixes.Size() + " prefixes");
            foreach (string prefix in allPrefixes)
            {
                // 1. run prefix against fst, then complete by value
                //System.out.println("TEST: " + prefix);

                Pair prefixOutput = outputs.NoOutput;
                FST.Arc<Pair> arc = fst.GetFirstArc(new FST.Arc<Pair>());
                for (int idx = 0; idx < prefix.Length; idx++)
                {
                    if (fst.FindTargetArc((int)prefix[idx], arc, arc, reader) is null)
                    {
                        Assert.Fail();
                    }
                    prefixOutput = outputs.Add(prefixOutput, arc.Output);
                }

                int topN = TestUtil.NextInt32(random, 1, 10);

                Util.TopResults<Pair> r = Util.ShortestPaths(fst, arc, fst.Outputs.NoOutput, minPairWeightComparer, topN, true);
                Assert.IsTrue(r.IsComplete);
                // 2. go thru whole treemap (slowCompletor) and check its actually the best suggestion
                JCG.List<Util.Result<Pair>> matches = new JCG.List<Util.Result<Pair>>();

                // TODO: could be faster... but its slowCompletor for a reason
                foreach (KeyValuePair<string, TwoLongs> e in slowCompletor)
                {
                    if (e.Key.StartsWith(prefix, StringComparison.Ordinal))
                    {
                        //System.out.println("  consider " + e.getKey());
                        matches.Add(new Util.Result<Pair>(Util.ToInt32sRef(new BytesRef(e.Key.Substring(prefix.Length)), new Int32sRef()),
                            outputs.NewPair(e.Value.a - prefixOutput.Output1, e.Value.b - prefixOutput.Output2)));
                    }
                }

                Assert.IsTrue(matches.Count > 0);
                matches.Sort(new TieBreakByInputComparer<Pair>(minPairWeightComparer));
                if (matches.Count > topN)
                {
                    matches.RemoveRange(topN, matches.Count - topN);  // LUCENENET: Converted end index to length;
                }

                Assert.AreEqual(matches.Count, r.TopN.Count);

                for (int hit = 0; hit < r.TopN.Count; hit++)
                {
                    //System.out.println("  check hit " + hit);
                    Assert.AreEqual(matches[hit].Input, r.TopN[hit].Input);
                    Assert.AreEqual(matches[hit].Output, r.TopN[hit].Output);
                }
            }
        }

        [Test]
        public virtual void TestLargeOutputsOnArrayArcs()
        {
            ByteSequenceOutputs outputs = ByteSequenceOutputs.Singleton;
            Builder<BytesRef> builder = new Builder<BytesRef>(FST.INPUT_TYPE.BYTE1, outputs);

            byte[] bytes = new byte[300];
            Int32sRef input = new Int32sRef();
            input.Grow(1);
            input.Length = 1;
            BytesRef output = new BytesRef(bytes);
            for (int arc = 0; arc < 6; arc++)
            {
                input.Int32s[0] = arc;
                output.Bytes[0] = (byte)arc;
                builder.Add(input, BytesRef.DeepCopyOf(output));
            }

            FST<BytesRef> fst = builder.Finish();
            for (int arc = 0; arc < 6; arc++)
            {
                input.Int32s[0] = arc;
                BytesRef result = Util.Get(fst, input);
                Assert.IsNotNull(result);
                Assert.AreEqual(300, result.Length);
                Assert.AreEqual(result.Bytes[result.Offset], arc);
                for (int byteIDX = 1; byteIDX < result.Length; byteIDX++)
                {
                    Assert.AreEqual((byte)0, result.Bytes[result.Offset + byteIDX]);
                }
            }
        }
    }
}