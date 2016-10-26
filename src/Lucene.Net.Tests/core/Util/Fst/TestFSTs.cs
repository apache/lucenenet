using Lucene.Net.Attributes;
using Lucene.Net.Randomized.Generators;
using Lucene.Net.Support;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

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

    //using Slow = Lucene.Net.Util.LuceneTestCase.Slow;
    using Automaton = Lucene.Net.Util.Automaton.Automaton;
    //using InputOutput = Lucene.Net.Util.Fst.BytesRefFSTEnum.InputOutput;
    //using Arc = Lucene.Net.Util.Fst.FST.Arc;
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
    //using ResultLong = Lucene.Net.Util.Fst.Util.Result<long?>;
    //using ResultPair = Lucene.Net.Util.Fst.Util.Result<long?>;
    using PackedInts = Lucene.Net.Util.Packed.PackedInts;
    using Pair = Lucene.Net.Util.Fst.PairOutputs<long?, long?>.Pair;
    using RandomIndexWriter = Lucene.Net.Index.RandomIndexWriter;
    using RegExp = Lucene.Net.Util.Automaton.RegExp;
    using Term = Lucene.Net.Index.Term;
    using TermQuery = Lucene.Net.Search.TermQuery;
    using Terms = Lucene.Net.Index.Terms;
    using TermsEnum = Lucene.Net.Index.TermsEnum;

    [SuppressCodecs("SimpleText", "Memory", "Direct")]
    [TestFixture]
    public class TestFSTs : LuceneTestCase
    {

        private MockDirectoryWrapper Dir;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            Dir = NewMockDirectory();
            Dir.PreventDoubleWrite = false;
        }

        [TearDown]
        public override void TearDown()
        {
            // can be null if we force simpletext (funky, some kind of bug in test runner maybe)
            if (Dir != null)
            {
                Dir.Dispose();
            }
            base.TearDown();
        }

        [Test]
        public virtual void TestBasicFSA()
        {
            string[] strings = new string[] { "station", "commotion", "elation", "elastic", "plastic", "stop", "ftop", "ftation", "stat" };
            string[] strings2 = new string[] { "station", "commotion", "elation", "elastic", "plastic", "stop", "ftop", "ftation" };
            IntsRef[] terms = new IntsRef[strings.Length];
            IntsRef[] terms2 = new IntsRef[strings2.Length];
            for (int inputMode = 0; inputMode < 2; inputMode++)
            {
                if (VERBOSE)
                {
                    Console.WriteLine("TEST: inputMode=" + InputModeToString(inputMode));
                }

                for (int idx = 0; idx < strings.Length; idx++)
                {
                    terms[idx] = FSTTester<object>.ToIntsRef(strings[idx], inputMode);
                }
                for (int idx = 0; idx < strings2.Length; idx++)
                {
                    terms2[idx] = FSTTester<object>.ToIntsRef(strings2[idx], inputMode);
                }
                Array.Sort(terms2);

                DoTest(inputMode, terms);

                // Test pre-determined FST sizes to make sure we haven't lost minimality (at least on this trivial set of terms):

                // FSA
                {
                    Outputs<object> outputs = NoOutputs.Singleton;
                    object NO_OUTPUT = outputs.NoOutput;
                    List<FSTTester<object>.InputOutput<object>> pairs = new List<FSTTester<object>.InputOutput<object>>(terms2.Length);
                    foreach (IntsRef term in terms2)
                    {
                        pairs.Add(new FSTTester<object>.InputOutput<object>(term, NO_OUTPUT));
                    }
                    FST<object> fst = (new FSTTester<object>(Random(), Dir, inputMode, pairs, outputs, false)).DoTest(0, 0, false);
                    Assert.IsNotNull(fst);
                    Assert.AreEqual(22, fst.NodeCount);
                    Assert.AreEqual(27, fst.ArcCount);
                }

                // FST ord pos int
                {
                    PositiveIntOutputs outputs = PositiveIntOutputs.Singleton;
                    List<FSTTester<long?>.InputOutput<long?>> pairs = new List<FSTTester<long?>.InputOutput<long?>>(terms2.Length);
                    for (int idx = 0; idx < terms2.Length; idx++)
                    {
                        pairs.Add(new FSTTester<long?>.InputOutput<long?>(terms2[idx], (long?)idx));
                    }
                    FST<long?> fst = (new FSTTester<long?>(Random(), Dir, inputMode, pairs, outputs, true)).DoTest(0, 0, false);
                    Assert.IsNotNull(fst);
                    Assert.AreEqual(22, fst.NodeCount);
                    Assert.AreEqual(27, fst.ArcCount);
                }

                // FST byte sequence ord
                {
                    ByteSequenceOutputs outputs = ByteSequenceOutputs.Singleton;
                    BytesRef NO_OUTPUT = outputs.NoOutput;
                    List<FSTTester<BytesRef>.InputOutput<BytesRef>> pairs = new List<FSTTester<BytesRef>.InputOutput<BytesRef>>(terms2.Length);
                    for (int idx = 0; idx < terms2.Length; idx++)
                    {
                        BytesRef output = Random().Next(30) == 17 ? NO_OUTPUT : new BytesRef(Convert.ToString(idx));
                        pairs.Add(new FSTTester<BytesRef>.InputOutput<BytesRef>(terms2[idx], output));
                    }
                    FST<BytesRef> fst = (new FSTTester<BytesRef>(Random(), Dir, inputMode, pairs, outputs, false)).DoTest(0, 0, false);
                    Assert.IsNotNull(fst);
                    Assert.AreEqual(24, fst.NodeCount);
                    Assert.AreEqual(30, fst.ArcCount);
                }
            }
        }

        // given set of terms, test the different outputs for them
        private void DoTest(int inputMode, IntsRef[] terms)
        {
            Array.Sort(terms);

            // NoOutputs (simple FSA)
            {
                Outputs<object> outputs = NoOutputs.Singleton;
                object NO_OUTPUT = outputs.NoOutput;
                List<FSTTester<object>.InputOutput<object>> pairs = new List<FSTTester<object>.InputOutput<object>>(terms.Length);
                foreach (IntsRef term in terms)
                {
                    pairs.Add(new FSTTester<object>.InputOutput<object>(term, NO_OUTPUT));
                }
                (new FSTTester<object>(Random(), Dir, inputMode, pairs, outputs, false)).DoTest(true);
            }

            // PositiveIntOutput (ord)
            {
                PositiveIntOutputs outputs = PositiveIntOutputs.Singleton;
                List<FSTTester<long?>.InputOutput<long?>> pairs = new List<FSTTester<long?>.InputOutput<long?>>(terms.Length);
                for (int idx = 0; idx < terms.Length; idx++)
                {
                    pairs.Add(new FSTTester<long?>.InputOutput<long?>(terms[idx], (long?)idx));
                }
                (new FSTTester<long?>(Random(), Dir, inputMode, pairs, outputs, true)).DoTest(true);
            }

            // PositiveIntOutput (random monotonically increasing positive number)
            {
                PositiveIntOutputs outputs = PositiveIntOutputs.Singleton;
                List<FSTTester<long?>.InputOutput<long?>> pairs = new List<FSTTester<long?>.InputOutput<long?>>(terms.Length);
                long lastOutput = 0;
                for (int idx = 0; idx < terms.Length; idx++)
                {
                    long value = lastOutput + TestUtil.NextInt(Random(), 1, 1000);
                    lastOutput = value;
                    pairs.Add(new FSTTester<long?>.InputOutput<long?>(terms[idx], value));
                }
                (new FSTTester<long?>(Random(), Dir, inputMode, pairs, outputs, true)).DoTest(true);
            }

            // PositiveIntOutput (random positive number)
            {
                PositiveIntOutputs outputs = PositiveIntOutputs.Singleton;
                List<FSTTester<long?>.InputOutput<long?>> pairs = new List<FSTTester<long?>.InputOutput<long?>>(terms.Length);
                for (int idx = 0; idx < terms.Length; idx++)
                {
                    pairs.Add(new FSTTester<long?>.InputOutput<long?>(terms[idx], TestUtil.NextLong(Random(), 0, long.MaxValue)));
                }
                (new FSTTester<long?>(Random(), Dir, inputMode, pairs, outputs, false)).DoTest(true);
            }

            // Pair<ord, (random monotonically increasing positive number>
            {
                PositiveIntOutputs o1 = PositiveIntOutputs.Singleton;
                PositiveIntOutputs o2 = PositiveIntOutputs.Singleton;
                PairOutputs<long?, long?> outputs = new PairOutputs<long?, long?>(o1, o2);
                List<FSTTester<Pair>.InputOutput<Pair>> pairs = new List<FSTTester<Pair>.InputOutput<Pair>>(terms.Length);
                long lastOutput = 0;
                for (int idx = 0; idx < terms.Length; idx++)
                {
                    long value = lastOutput + TestUtil.NextInt(Random(), 1, 1000);
                    lastOutput = value;
                    pairs.Add(new FSTTester<Pair>.InputOutput<Pair>(terms[idx], outputs.NewPair((long?)idx, value)));
                }
                (new FSTTester<Pair>(Random(), Dir, inputMode, pairs, outputs, false)).DoTest(true);
            }

            // Sequence-of-bytes
            {
                ByteSequenceOutputs outputs = ByteSequenceOutputs.Singleton;
                BytesRef NO_OUTPUT = outputs.NoOutput;
                List<FSTTester<BytesRef>.InputOutput<BytesRef>> pairs = new List<FSTTester<BytesRef>.InputOutput<BytesRef>>(terms.Length);
                for (int idx = 0; idx < terms.Length; idx++)
                {
                    BytesRef output = Random().Next(30) == 17 ? NO_OUTPUT : new BytesRef(Convert.ToString(idx));
                    pairs.Add(new FSTTester<BytesRef>.InputOutput<BytesRef>(terms[idx], output));
                }
                (new FSTTester<BytesRef>(Random(), Dir, inputMode, pairs, outputs, false)).DoTest(true);
            }

            // Sequence-of-ints
            {
                IntSequenceOutputs outputs = IntSequenceOutputs.Singleton;
                List<FSTTester<IntsRef>.InputOutput<IntsRef>> pairs = new List<FSTTester<IntsRef>.InputOutput<IntsRef>>(terms.Length);
                for (int idx = 0; idx < terms.Length; idx++)
                {
                    string s = Convert.ToString(idx);
                    IntsRef output = new IntsRef(s.Length);
                    output.Length = s.Length;
                    for (int idx2 = 0; idx2 < output.Length; idx2++)
                    {
                        output.Ints[idx2] = s[idx2];
                    }
                    pairs.Add(new FSTTester<IntsRef>.InputOutput<IntsRef>(terms[idx], output));
                }
                (new FSTTester<IntsRef>(Random(), Dir, inputMode, pairs, outputs, false)).DoTest(true);
            }

        }


        [Test, LongRunningTest, MaxTime(27000000)] // 45 minutes to be on the safe side
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
            Random random = new Random(Random().Next());
            for (int iter = 0; iter < numIter; iter++)
            {
                if (VERBOSE)
                {
                    Console.WriteLine("\nTEST: iter " + iter);
                }
                for (int inputMode = 0; inputMode < 2; inputMode++)
                {
                    int numWords = random.Next(maxNumWords + 1);
                    HashSet<IntsRef> termsSet = new HashSet<IntsRef>();
                    IntsRef[] terms = new IntsRef[numWords];
                    while (termsSet.Count < numWords)
                    {
                        string term = FSTTester<object>.GetRandomString(random);
                        termsSet.Add(FSTTester<object>.ToIntsRef(term, inputMode));
                    }
                    DoTest(inputMode, termsSet.ToArray(/*new IntsRef[termsSet.Count]*/));
                }
            }
        }

        [Test]
        [Ignore("LUCENENET TODO: This test will take around 10-14 hours to finish. It was marked with a Nightly attribute in the original Java source, but we don't currently have a corresponding attribute")]
        public virtual void TestBigSet()
        {
            TestRandomWords(TestUtil.NextInt(Random(), 50000, 60000), 1);
        }

        // Build FST for all unique terms in the test line docs
        // file, up until a time limit
        [Test]
        public virtual void TestRealTerms()
        {

            LineFileDocs docs = new LineFileDocs(Random(), DefaultCodecSupportsDocValues());
            int RUN_TIME_MSEC = AtLeast(500);
            MockAnalyzer analyzer = new MockAnalyzer(Random());
            analyzer.MaxTokenLength = TestUtil.NextInt(Random(), 1, IndexWriter.MAX_TERM_LENGTH);

            IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer).SetMaxBufferedDocs(-1).SetRAMBufferSizeMB(64);
            DirectoryInfo tempDir = CreateTempDir("fstlines");
            Directory dir = NewFSDirectory(tempDir);
            IndexWriter writer = new IndexWriter(dir, conf);
            long stopTime = Environment.TickCount + RUN_TIME_MSEC;
            Document doc;
            int docCount = 0;
            while ((doc = docs.NextDoc()) != null && Environment.TickCount < stopTime)
            {
                writer.AddDocument(doc);
                docCount++;
            }
            IndexReader r = DirectoryReader.Open(writer, true);
            writer.Dispose();
            PositiveIntOutputs outputs = PositiveIntOutputs.Singleton;

            bool doRewrite = Random().NextBoolean();

            Builder<long?> builder = new Builder<long?>(FST.INPUT_TYPE.BYTE1, 0, 0, true, true, int.MaxValue, outputs, null, doRewrite, PackedInts.DEFAULT, true, 15);

            bool storeOrd = Random().NextBoolean();
            if (VERBOSE)
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
                IntsRef scratchIntsRef = new IntsRef();
                TermsEnum termsEnum = terms.Iterator(null);
                if (VERBOSE)
                {
                    Console.WriteLine("TEST: got termsEnum=" + termsEnum);
                }
                BytesRef term;
                int ord = 0;

                Automaton automaton = (new RegExp(".*", RegExp.NONE)).ToAutomaton();
                TermsEnum termsEnum2 = terms.Intersect(new CompiledAutomaton(automaton, false, false), null);

                while ((term = termsEnum.Next()) != null)
                {
                    BytesRef term2 = termsEnum2.Next();
                    Assert.IsNotNull(term2);
                    Assert.AreEqual(term, term2);
                    Assert.AreEqual(termsEnum.DocFreq(), termsEnum2.DocFreq());
                    Assert.AreEqual(termsEnum.TotalTermFreq(), termsEnum2.TotalTermFreq());

                    if (ord == 0)
                    {
                        try
                        {
                            termsEnum.Ord();
                        }
                        catch (System.NotSupportedException uoe)
                        {
                            if (VERBOSE)
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
                        output = termsEnum.DocFreq();
                    }
                    builder.Add(Util.ToIntsRef(term, scratchIntsRef), (long)output);
                    ord++;
                    if (VERBOSE && ord % 100000 == 0 && LuceneTestCase.TEST_NIGHTLY)
                    {
                        Console.WriteLine(ord + " terms...");
                    }
                }
                FST<long?> fst = builder.Finish();
                if (VERBOSE)
                {
                    Console.WriteLine("FST: " + docCount + " docs; " + ord + " terms; " + fst.NodeCount + " nodes; " + fst.ArcCount + " arcs;" + " " + fst.SizeInBytes() + " bytes");
                }

                if (ord > 0)
                {
                    Random random = new Random(Random().Next());
                    // Now confirm BytesRefFSTEnum and TermsEnum act the
                    // same:
                    BytesRefFSTEnum<long?> fstEnum = new BytesRefFSTEnum<long?>(fst);
                    int num = AtLeast(1000);
                    for (int iter = 0; iter < num; iter++)
                    {
                        BytesRef randomTerm = new BytesRef(FSTTester<object>.GetRandomString(random));

                        if (VERBOSE)
                        {
                            Console.WriteLine("TEST: seek non-exist " + randomTerm.Utf8ToString() + " " + randomTerm);
                        }

                        TermsEnum.SeekStatus seekResult = termsEnum.SeekCeil(randomTerm);
                        BytesRefFSTEnum.InputOutput<long?> fstSeekResult = fstEnum.SeekCeil(randomTerm);

                        if (seekResult == TermsEnum.SeekStatus.END)
                        {
                            Assert.IsNull(fstSeekResult, "got " + (fstSeekResult == null ? "null" : fstSeekResult.Input.Utf8ToString()) + " but expected null");
                        }
                        else
                        {
                            AssertSame(termsEnum, fstEnum, storeOrd);
                            for (int nextIter = 0; nextIter < 10; nextIter++)
                            {
                                if (VERBOSE)
                                {
                                    Console.WriteLine("TEST: next");
                                    if (storeOrd)
                                    {
                                        Console.WriteLine("  ord=" + termsEnum.Ord());
                                    }
                                }
                                if (termsEnum.Next() != null)
                                {
                                    if (VERBOSE)
                                    {
                                        Console.WriteLine("  term=" + termsEnum.Term().Utf8ToString());
                                    }
                                    Assert.IsNotNull(fstEnum.Next());
                                    AssertSame(termsEnum, fstEnum, storeOrd);
                                }
                                else
                                {
                                    if (VERBOSE)
                                    {
                                        Console.WriteLine("  end!");
                                    }
                                    BytesRefFSTEnum.InputOutput<long?> nextResult = fstEnum.Next();
                                    if (nextResult != null)
                                    {
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

        private void AssertSame<T1>(TermsEnum termsEnum, BytesRefFSTEnum<T1> fstEnum, bool storeOrd)
        {
            if (termsEnum.Term() == null)
            {
                Assert.IsNull(fstEnum.Current());
            }
            else
            {
                Assert.IsNotNull(fstEnum.Current());
                Assert.AreEqual(termsEnum.Term(), fstEnum.Current().Input, termsEnum.Term().Utf8ToString() + " != " + fstEnum.Current().Input.Utf8ToString());
                if (storeOrd)
                {
                    // fst stored the ord
                    Assert.AreEqual(termsEnum.Ord(), fstEnum.Current().Output, "term=" + termsEnum.Term().Utf8ToString() + " " + termsEnum.Term());
                }
                else
                {
                    // fst stored the docFreq
                    Assert.AreEqual(termsEnum.DocFreq(), fstEnum.Current().Output, "term=" + termsEnum.Term().Utf8ToString() + " " + termsEnum.Term());
                }
            }
        }

        private abstract class VisitTerms<T>
        {
            internal readonly string DirOut;
            internal readonly string WordsFileIn;
            internal int InputMode;
            internal readonly Outputs<T> Outputs;
            internal readonly Builder<T> Builder;
            internal readonly bool DoPack;

            public VisitTerms(string dirOut, string wordsFileIn, int inputMode, int prune, Outputs<T> outputs, bool doPack, bool noArcArrays)
            {
                this.DirOut = dirOut;
                this.WordsFileIn = wordsFileIn;
                this.InputMode = inputMode;
                this.Outputs = outputs;
                this.DoPack = doPack;

                Builder = new Builder<T>(inputMode == 0 ? FST.INPUT_TYPE.BYTE1 : FST.INPUT_TYPE.BYTE4, 0, prune, prune == 0, true, int.MaxValue, outputs, null, doPack, PackedInts.DEFAULT, !noArcArrays, 15);
            }

            protected internal abstract T GetOutput(IntsRef input, int ord);

            public virtual void Run(int limit, bool verify, bool verifyByOutput)
            {
                TextReader @is = new StreamReader(new FileStream(WordsFileIn, FileMode.Open), Encoding.UTF8);
                try
                {
                    IntsRef intsRef = new IntsRef(10);
                    long tStart = Environment.TickCount;
                    int ord = 0;
                    while (true)
                    {
                        string w = @is.ReadLine();
                        if (w == null)
                        {
                            break;
                        }
                        FSTTester<object>.ToIntsRef(w, InputMode, intsRef);
                        Builder.Add(intsRef, GetOutput(intsRef, ord));

                        ord++;
                        if (ord % 500000 == 0)
                        {
                            Console.WriteLine(string.Format(CultureInfo.InvariantCulture, "{0:000000.000}s: {1:000000000}...", ((Environment.TickCount - tStart) / 1000.0), ord));
                        }
                        if (ord >= limit)
                        {
                            break;
                        }
                    }

                    long tMid = Environment.TickCount;
                    Console.WriteLine(((tMid - tStart) / 1000.0) + " sec to add all terms");

                    Debug.Assert(Builder.TermCount == ord);
                    FST<T> fst = Builder.Finish();
                    long tEnd = Environment.TickCount;
                    Console.WriteLine(((tEnd - tMid) / 1000.0) + " sec to finish/pack");
                    if (fst == null)
                    {
                        Console.WriteLine("FST was fully pruned!");
                        Environment.Exit(0);
                    }

                    if (DirOut == null)
                    {
                        return;
                    }

                    Console.WriteLine(ord + " terms; " + fst.NodeCount + " nodes; " + fst.ArcCount + " arcs; " + fst.ArcWithOutputCount + " arcs w/ output; tot size " + fst.SizeInBytes());
                    if (fst.NodeCount < 100)
                    {
                        TextWriter w = new StreamWriter(new FileStream("out.dot", FileMode.Create), Encoding.UTF8);
                        Util.ToDot(fst, w, false, false);
                        w.Dispose();
                        Console.WriteLine("Wrote FST to out.dot");
                    }

                    Directory dir = FSDirectory.Open(new DirectoryInfo(DirOut));
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
                            @is = new StreamReader(new FileStream(WordsFileIn, FileMode.Open), Encoding.UTF8);

                            ord = 0;
                            tStart = Environment.TickCount;
                            while (true)
                            {
                                string w = @is.ReadLine();
                                if (w == null)
                                {
                                    break;
                                }
                                FSTTester<object>.ToIntsRef(w, InputMode, intsRef);
                                if (iter == 0)
                                {
                                    T expected = GetOutput(intsRef, ord);
                                    T actual = Util.Get(fst, intsRef);
                                    if (actual == null)
                                    {
                                        throw new Exception("unexpected null output on input=" + w);
                                    }
                                    if (!actual.Equals(expected))
                                    {
                                        throw new Exception("wrong output (got " + Outputs.OutputToString(actual) + " but expected " + Outputs.OutputToString(expected) + ") on input=" + w);
                                    }
                                }
                                else
                                {
                                    // Get by output
                                    long? output = GetOutput(intsRef, ord) as long?;
                                    IntsRef actual = Util.GetByOutput(fst as FST<long?>, output.GetValueOrDefault());
                                    if (actual == null)
                                    {
                                        throw new Exception("unexpected null input from output=" + output);
                                    }
                                    if (!actual.Equals(intsRef))
                                    {
                                        throw new Exception("wrong input (got " + actual + " but expected " + intsRef + " from output=" + output);
                                    }
                                }

                                ord++;
                                if (ord % 500000 == 0)
                                {
                                    Console.WriteLine(((Environment.TickCount - tStart) / 1000.0) + "s: " + ord + "...");
                                }
                                if (ord >= limit)
                                {
                                    break;
                                }
                            }

                            double totSec = ((Environment.TickCount - tStart) / 1000.0);
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
                if (args[idx].Equals("-prune"))
                {
                    prune = Convert.ToInt32(args[1 + idx]);
                    idx++;
                }
                else if (args[idx].Equals("-limit"))
                {
                    limit = Convert.ToInt32(args[1 + idx]);
                    idx++;
                }
                else if (args[idx].Equals("-utf8"))
                {
                    inputMode = 0;
                }
                else if (args[idx].Equals("-utf32"))
                {
                    inputMode = 1;
                }
                else if (args[idx].Equals("-docFreq"))
                {
                    storeDocFreqs = true;
                }
                else if (args[idx].Equals("-noArcArrays"))
                {
                    noArcArrays = true;
                }
                else if (args[idx].Equals("-ords"))
                {
                    storeOrds = true;
                }
                else if (args[idx].Equals("-noverify"))
                {
                    verify = false;
                }
                else if (args[idx].Equals("-pack"))
                {
                    doPack = true;
                }
                else if (args[idx].StartsWith("-"))
                {
                    Console.Error.WriteLine("Unrecognized option: " + args[idx]);
                    Environment.Exit(-1);
                }
                else
                {
                    if (wordsFileIn == null)
                    {
                        wordsFileIn = args[idx];
                    }
                    else if (dirOut == null)
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

            if (wordsFileIn == null)
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
                new VisitTermsAnonymousInnerClassHelper(dirOut, wordsFileIn, inputMode, prune, outputs, doPack, noArcArrays).Run(limit, verify, false);
            }
            else if (storeOrds)
            {
                // Store only ords
                PositiveIntOutputs outputs = PositiveIntOutputs.Singleton;
                new VisitTermsAnonymousInnerClassHelper2(dirOut, wordsFileIn, inputMode, prune, outputs, doPack, noArcArrays).Run(limit, verify, true);
            }
            else if (storeDocFreqs)
            {
                // Store only docFreq
                PositiveIntOutputs outputs = PositiveIntOutputs.Singleton;
                new VisitTermsAnonymousInnerClassHelper3(dirOut, wordsFileIn, inputMode, prune, outputs, doPack, noArcArrays).Run(limit, verify, false);
            }
            else
            {
                // Store nothing
                NoOutputs outputs = NoOutputs.Singleton;
                object NO_OUTPUT = outputs.NoOutput;
                new VisitTermsAnonymousInnerClassHelper4(dirOut, wordsFileIn, inputMode, prune, outputs, doPack, noArcArrays, NO_OUTPUT).Run(limit, verify, false);
            }
        }*/

        private class VisitTermsAnonymousInnerClassHelper : VisitTerms<Pair>
        {
            private PairOutputs<long?, long?> outputs;

            public VisitTermsAnonymousInnerClassHelper(string dirOut, string wordsFileIn, int inputMode, int prune, PairOutputs<long?, long?> outputs, bool doPack, bool noArcArrays)
                : base(dirOut, wordsFileIn, inputMode, prune, outputs, doPack, noArcArrays)
            {
                this.outputs = outputs;
            }

            internal Random rand;
            protected internal override Pair GetOutput(IntsRef input, int ord)
            {
                if (ord == 0)
                {
                    rand = new Random(17);
                }
                return outputs.NewPair(ord, TestUtil.NextInt(rand, 1, 5000));
            }
        }

        private class VisitTermsAnonymousInnerClassHelper2 : VisitTerms<long?>
        {
            public VisitTermsAnonymousInnerClassHelper2(string dirOut, string wordsFileIn, int inputMode, int prune, PositiveIntOutputs outputs, bool doPack, bool noArcArrays)
                : base(dirOut, wordsFileIn, inputMode, prune, outputs, doPack, noArcArrays)
            {
            }

            protected internal override long? GetOutput(IntsRef input, int ord)
            {
                return ord;
            }
        }

        private class VisitTermsAnonymousInnerClassHelper3 : VisitTerms<long?>
        {
            public VisitTermsAnonymousInnerClassHelper3(string dirOut, string wordsFileIn, int inputMode, int prune, PositiveIntOutputs outputs, bool doPack, bool noArcArrays)
                : base(dirOut, wordsFileIn, inputMode, prune, outputs, doPack, noArcArrays)
            {
            }

            internal Random rand;
            protected internal override long? GetOutput(IntsRef input, int ord)
            {
                if (ord == 0)
                {
                    rand = new Random(17);
                }
                return (long)TestUtil.NextInt(rand, 1, 5000);
            }
        }

        private class VisitTermsAnonymousInnerClassHelper4 : VisitTerms<object>
        {
            private object NO_OUTPUT;

            public VisitTermsAnonymousInnerClassHelper4(string dirOut, string wordsFileIn, int inputMode, int prune, NoOutputs outputs, bool doPack, bool noArcArrays, object NO_OUTPUT)
                : base(dirOut, wordsFileIn, inputMode, prune, outputs, doPack, noArcArrays)
            {
                this.NO_OUTPUT = NO_OUTPUT;
            }

            protected internal override object GetOutput(IntsRef input, int ord)
            {
                return NO_OUTPUT;
            }
        }

        [Test]
        public virtual void TestSingleString()
        {
            Outputs<object> outputs = NoOutputs.Singleton;
            Builder<object> b = new Builder<object>(FST.INPUT_TYPE.BYTE1, outputs);
            b.Add(Util.ToIntsRef(new BytesRef("foobar"), new IntsRef()), outputs.NoOutput);
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
            IntsRef ints = new IntsRef();
            for (int i = 0; i < 10; i++)
            {
                b.Add(Util.ToIntsRef(new BytesRef(str), ints), outputs.NoOutput);
            }
            FST<object> fst = b.Finish();

            // count the input paths
            int count = 0;
            BytesRefFSTEnum<object> fstEnum = new BytesRefFSTEnum<object>(fst);
            while (fstEnum.Next() != null)
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
          Arrays.sort(strings);
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
            PositiveIntOutputs outputs = PositiveIntOutputs.Singleton;

            // Build an FST mapping BytesRef -> Long
            Builder<long?> builder = new Builder<long?>(FST.INPUT_TYPE.BYTE1, outputs);

            BytesRef a = new BytesRef("a");
            BytesRef b = new BytesRef("b");
            BytesRef c = new BytesRef("c");

            builder.Add(Util.ToIntsRef(a, new IntsRef()), 17L);
            builder.Add(Util.ToIntsRef(b, new IntsRef()), 42L);
            builder.Add(Util.ToIntsRef(c, new IntsRef()), 13824324872317238L);

            FST<long?> fst = builder.Finish();

            Assert.AreEqual(13824324872317238L, Util.Get(fst, c));
            Assert.AreEqual(42, Util.Get(fst, b));
            Assert.AreEqual(17, Util.Get(fst, a));

            BytesRefFSTEnum<long?> fstEnum = new BytesRefFSTEnum<long?>(fst);
            BytesRefFSTEnum.InputOutput<long?> seekResult;
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

            Assert.AreEqual(Util.ToIntsRef(new BytesRef("c"), new IntsRef()), Util.GetByOutput(fst, 13824324872317238L));
            Assert.IsNull(Util.GetByOutput(fst, 47));
            Assert.AreEqual(Util.ToIntsRef(new BytesRef("b"), new IntsRef()), Util.GetByOutput(fst, 42));
            Assert.AreEqual(Util.ToIntsRef(new BytesRef("a"), new IntsRef()), Util.GetByOutput(fst, 17));
        }

        [Test]
        public virtual void TestPrimaryKeys()
        {
            Directory dir = NewDirectory();

            for (int cycle = 0; cycle < 2; cycle++)
            {
                if (VERBOSE)
                {
                    Console.WriteLine("TEST: cycle=" + cycle);
                }
                RandomIndexWriter w = new RandomIndexWriter(Random(), dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())).SetOpenMode(IndexWriterConfig.OpenMode_e.CREATE));
                Document doc = new Document();
                Field idField = NewStringField("id", "", Field.Store.NO);
                doc.Add(idField);

                int NUM_IDS = AtLeast(200);
                //final int NUM_IDS = (int) (377 * (1.0+random.nextDouble()));
                if (VERBOSE)
                {
                    Console.WriteLine("TEST: NUM_IDS=" + NUM_IDS);
                }
                HashSet<string> allIDs = new HashSet<string>();
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
                            string s = Convert.ToString(Random().NextLong());
                            if (!allIDs.Contains(s))
                            {
                                idString = s;
                                break;
                            }
                        }
                    }
                    allIDs.Add(idString);
                    idField.StringValue = idString;
                    w.AddDocument(doc);
                }

                //w.forceMerge(1);

                // turn writer into reader:
                IndexReader r = w.Reader;
                IndexSearcher idxS = NewSearcher(r);
                w.Dispose();

                IList<string> allIDsList = new List<string>(allIDs);
                List<string> sortedAllIDsList = new List<string>(allIDsList);
                sortedAllIDsList.Sort();

                // Sprinkle in some non-existent PKs:
                HashSet<string> outOfBounds = new HashSet<string>();
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
                            idString = Convert.ToString(Random().NextLong());
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
                    string id = allIDsList[Random().Next(allIDsList.Count)];
                    bool exists = !outOfBounds.Contains(id);
                    if (VERBOSE)
                    {
                        Console.WriteLine("TEST: TermQuery " + (exists ? "" : "non-exist ") + " id=" + id);
                    }
                    Assert.AreEqual(exists ? 1 : 0, idxS.Search(new TermQuery(new Term("id", id)), 1).TotalHits, (exists ? "" : "non-exist ") + "id=" + id);
                }

                // Verify w/ MultiTermsEnum
                TermsEnum termsEnum = MultiFields.GetTerms(r, "id").Iterator(null);
                for (int iter = 0; iter < 2 * NUM_IDS; iter++)
                {
                    string id;
                    string nextID;
                    bool exists;

                    if (Random().NextBoolean())
                    {
                        id = allIDsList[Random().Next(allIDsList.Count)];
                        exists = !outOfBounds.Contains(id);
                        nextID = null;
                        if (VERBOSE)
                        {
                            Console.WriteLine("TEST: exactOnly " + (exists ? "" : "non-exist ") + "id=" + id);
                        }
                    }
                    else
                    {
                        // Pick ID between two IDs:
                        exists = false;
                        int idv = Random().Next(NUM_IDS - 1);
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
                        if (VERBOSE)
                        {
                            Console.WriteLine("TEST: not exactOnly id=" + id + " nextID=" + nextID);
                        }
                    }

                    TermsEnum.SeekStatus status;
                    if (nextID == null)
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
                        Assert.AreEqual(new BytesRef(nextID), termsEnum.Term(), "expected=" + nextID + " actual=" + termsEnum.Term().Utf8ToString());
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

            RandomIndexWriter w = new RandomIndexWriter(Random(), dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random()))
                .SetOpenMode(IndexWriterConfig.OpenMode_e.CREATE));
            Document doc = new Document();
            Field f = NewStringField("field", "", Field.Store.NO);
            doc.Add(f);

            int NUM_TERMS = (int)(1000 * RANDOM_MULTIPLIER * (1 + Random().NextDouble()));
            if (VERBOSE)
            {
                Console.WriteLine("TEST: NUM_TERMS=" + NUM_TERMS);
            }

            HashSet<string> allTerms = new HashSet<string>();
            while (allTerms.Count < NUM_TERMS)
            {
                allTerms.Add(FSTTester<object>.SimpleRandomString(Random()));
            }

            foreach (string term in allTerms)
            {
                f.StringValue = term;
                w.AddDocument(doc);
            }

            // turn writer into reader:
            if (VERBOSE)
            {
                Console.WriteLine("TEST: get reader");
            }
            IndexReader r = w.Reader;
            if (VERBOSE)
            {
                Console.WriteLine("TEST: got reader=" + r);
            }
            IndexSearcher s = NewSearcher(r);
            w.Dispose();

            IList<string> allTermsList = new List<string>(allTerms);
            allTermsList = CollectionsHelper.Shuffle(allTermsList);

            // verify exact lookup
            foreach (string term in allTermsList)
            {
                if (VERBOSE)
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

            List<string> @out = new List<string>();
            StringBuilder b = new StringBuilder();
            s.Generate(@out, b, 'a', 'i', 10);
            string[] input = @out.ToArray();
            Array.Sort(input);
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
                IntsRef scratchIntsRef = new IntsRef();

                while (line < lines.Length)
                {
                    string w = lines[line++];
                    if (w == null)
                    {
                        break;
                    }
                    term.CopyChars(w);
                    b.Add(Util.ToIntsRef(term, scratchIntsRef), nothing);
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
                    BytesReader fstReader = fst.BytesReader;
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
            PositiveIntOutputs outputs = PositiveIntOutputs.Singleton;

            Builder<long?> builder = new Builder<long?>(FST.INPUT_TYPE.BYTE4, 2, 0, true, true, int.MaxValue, outputs, null, Random().NextBoolean(), PackedInts.DEFAULT, true, 15);
            builder.Add(Util.ToUTF32("stat", new IntsRef()), 17L);
            builder.Add(Util.ToUTF32("station", new IntsRef()), 10L);
            FST<long?> fst = builder.Finish();
            //Writer w = new OutputStreamWriter(new FileOutputStream("/x/tmp3/out.dot"));
            StringWriter w = new StringWriter();
            Util.ToDot(fst, w, false, false);
            w.Dispose();
            //System.out.println(w.toString());
            Assert.IsTrue(w.ToString().IndexOf("label=\"t/[7]\"") != -1);
        }

        [Test]
        public virtual void TestInternalFinalState()
        {
            PositiveIntOutputs outputs = PositiveIntOutputs.Singleton;
            bool willRewrite = Random().NextBoolean();
            Builder<long?> builder = new Builder<long?>(FST.INPUT_TYPE.BYTE1, 0, 0, true, true, int.MaxValue, outputs, null, willRewrite, PackedInts.DEFAULT, true, 15);
            builder.Add(Util.ToIntsRef(new BytesRef("stat"), new IntsRef()), outputs.NoOutput);
            builder.Add(Util.ToIntsRef(new BytesRef("station"), new IntsRef()), outputs.NoOutput);
            FST<long?> fst = builder.Finish();
            StringWriter w = new StringWriter();
            //Writer w = new OutputStreamWriter(new FileOutputStream("/x/tmp/out.dot"));
            Util.ToDot(fst, w, false, false);
            w.Dispose();
            //System.out.println(w.toString());

            // check for accept state at label t
            Assert.IsTrue(w.ToString().IndexOf("[label=\"t\" style=\"bold\"") != -1);
            // check for accept state at label n
            Assert.IsTrue(w.ToString().IndexOf("[label=\"n\" style=\"bold\"") != -1);
        }

        // Make sure raw FST can differentiate between final vs
        // non-final end nodes
        [Test]
        public virtual void TestNonFinalStopNode()
        {
            PositiveIntOutputs outputs = PositiveIntOutputs.Singleton;
            long? nothing = outputs.NoOutput;
            Builder<long?> b = new Builder<long?>(FST.INPUT_TYPE.BYTE1, outputs);

            FST<long?> fst = new FST<long?>(FST.INPUT_TYPE.BYTE1, outputs, false, PackedInts.COMPACT, true, 15);

            Builder<long?>.UnCompiledNode<long?> rootNode = new Builder<long?>.UnCompiledNode<long?>(b, 0);

            // Add final stop node
            {
                Builder<long?>.UnCompiledNode<long?> node = new Builder<long?>.UnCompiledNode<long?>(b, 0);
                node.IsFinal = true;
                rootNode.AddArc('a', node);
                Builder<long?>.CompiledNode frozen = new Builder<long?>.CompiledNode();
                frozen.Node = fst.AddNode(node);
                rootNode.Arcs[0].NextFinalOutput = 17L;
                rootNode.Arcs[0].IsFinal = true;
                rootNode.Arcs[0].Output = nothing;
                rootNode.Arcs[0].Target = frozen;
            }

            // Add non-final stop node
            {
                Builder<long?>.UnCompiledNode<long?> node = new Builder<long?>.UnCompiledNode<long?>(b, 0);
                rootNode.AddArc('b', node);
                Builder<long?>.CompiledNode frozen = new Builder<long?>.CompiledNode();
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
            FST<long?> fst2 = new FST<long?>(@in, outputs);
            CheckStopNodes(fst2, outputs);
            @in.Dispose();
            dir.Dispose();
        }

        private void CheckStopNodes(FST<long?> fst, PositiveIntOutputs outputs)
        {
            long? nothing = outputs.NoOutput;
            FST.Arc<long?> startArc = fst.GetFirstArc(new FST.Arc<long?>());
            Assert.AreEqual(nothing, startArc.Output);
            Assert.AreEqual(nothing, startArc.NextFinalOutput);

            FST.Arc<long?> arc = fst.ReadFirstTargetArc(startArc, new FST.Arc<long?>(), fst.BytesReader);
            Assert.AreEqual('a', arc.Label);
            Assert.AreEqual(17, arc.NextFinalOutput);
            Assert.IsTrue(arc.IsFinal);

            arc = fst.ReadNextArc(arc, fst.BytesReader);
            Assert.AreEqual('b', arc.Label);
            Assert.IsFalse(arc.IsFinal);
            Assert.AreEqual(42, arc.Output);
        }

        internal static readonly IComparer<long?> minLongComparator = new ComparatorAnonymousInnerClassHelper();

        private class ComparatorAnonymousInnerClassHelper : IComparer<long?>
        {
            public ComparatorAnonymousInnerClassHelper()
            {
            }

            public virtual int Compare(long? left, long? right)
            {
                return left.Value.CompareTo(right.Value);
            }
        }

        [Test]
        public virtual void TestShortestPaths()
        {
            PositiveIntOutputs outputs = PositiveIntOutputs.Singleton;
            Builder<long?> builder = new Builder<long?>(FST.INPUT_TYPE.BYTE1, outputs);

            IntsRef scratch = new IntsRef();
            builder.Add(Util.ToIntsRef(new BytesRef("aab"), scratch), 22L);
            builder.Add(Util.ToIntsRef(new BytesRef("aac"), scratch), 7L);
            builder.Add(Util.ToIntsRef(new BytesRef("ax"), scratch), 17L);
            FST<long?> fst = builder.Finish();
            //Writer w = new OutputStreamWriter(new FileOutputStream("out.dot"));
            //Util.toDot(fst, w, false, false);
            //w.Dispose();

            Util.TopResults<long?> res = Util.ShortestPaths(fst, fst.GetFirstArc(new FST.Arc<long?>()), outputs.NoOutput, minLongComparator, 3, true);
            Assert.IsTrue(res.IsComplete);
            Assert.AreEqual(3, res.TopN.Count);
            Assert.AreEqual(Util.ToIntsRef(new BytesRef("aac"), scratch), res.TopN[0].Input);
            Assert.AreEqual(7L, res.TopN[0].Output);

            Assert.AreEqual(Util.ToIntsRef(new BytesRef("ax"), scratch), res.TopN[1].Input);
            Assert.AreEqual(17L, res.TopN[1].Output);

            Assert.AreEqual(Util.ToIntsRef(new BytesRef("aab"), scratch), res.TopN[2].Input);
            Assert.AreEqual(22L, res.TopN[2].Output);
        }

        [Test]
        public virtual void TestRejectNoLimits()
        {
            PositiveIntOutputs outputs = PositiveIntOutputs.Singleton;
            Builder<long?> builder = new Builder<long?>(FST.INPUT_TYPE.BYTE1, outputs);

            IntsRef scratch = new IntsRef();
            builder.Add(Util.ToIntsRef(new BytesRef("aab"), scratch), 22L);
            builder.Add(Util.ToIntsRef(new BytesRef("aac"), scratch), 7L);
            builder.Add(Util.ToIntsRef(new BytesRef("adcd"), scratch), 17L);
            builder.Add(Util.ToIntsRef(new BytesRef("adcde"), scratch), 17L);

            builder.Add(Util.ToIntsRef(new BytesRef("ax"), scratch), 17L);
            FST<long?> fst = builder.Finish();
            AtomicInteger rejectCount = new AtomicInteger();
            Util.TopNSearcher<long?> searcher = new TopNSearcherAnonymousInnerClassHelper(this, fst, minLongComparator, rejectCount);

            searcher.AddStartPaths(fst.GetFirstArc(new FST.Arc<long?>()), outputs.NoOutput, true, new IntsRef());
            Util.TopResults<long?> res = searcher.Search();
            Assert.AreEqual(rejectCount.Get(), 4);
            Assert.IsTrue(res.IsComplete); // rejected(4) + topN(2) <= maxQueueSize(6)

            Assert.AreEqual(1, res.TopN.Count);
            Assert.AreEqual(Util.ToIntsRef(new BytesRef("aac"), scratch), res.TopN[0].Input);
            Assert.AreEqual(7L, res.TopN[0].Output);
            rejectCount.Set(0);
            searcher = new TopNSearcherAnonymousInnerClassHelper2(this, fst, minLongComparator, rejectCount);

            searcher.AddStartPaths(fst.GetFirstArc(new FST.Arc<long?>()), outputs.NoOutput, true, new IntsRef());
            res = searcher.Search();
            Assert.AreEqual(rejectCount.Get(), 4);
            Assert.IsFalse(res.IsComplete); // rejected(4) + topN(2) > maxQueueSize(5)
        }

        private class TopNSearcherAnonymousInnerClassHelper : Util.TopNSearcher<long?>
        {
            private readonly TestFSTs OuterInstance;

            private AtomicInteger RejectCount;

            public TopNSearcherAnonymousInnerClassHelper(TestFSTs outerInstance, FST<long?> fst, IComparer<long?> minLongComparator, AtomicInteger rejectCount)
                : base(fst, 2, 6, minLongComparator)
            {
                this.OuterInstance = outerInstance;
                this.RejectCount = rejectCount;
            }

            protected override bool AcceptResult(IntsRef input, long? output)
            {
                bool accept = (int)output == 7;
                if (!accept)
                {
                    RejectCount.IncrementAndGet();
                }
                return accept;
            }
        }

        private class TopNSearcherAnonymousInnerClassHelper2 : Util.TopNSearcher<long?>
        {
            private readonly TestFSTs OuterInstance;

            private AtomicInteger RejectCount;

            public TopNSearcherAnonymousInnerClassHelper2(TestFSTs outerInstance, FST<long?> fst, IComparer<long?> minLongComparator, AtomicInteger rejectCount)
                : base(fst, 2, 5, minLongComparator)
            {
                this.OuterInstance = outerInstance;
                this.RejectCount = rejectCount;
            }

            protected override bool AcceptResult(IntsRef input, long? output)
            {
                bool accept = (int)output == 7;
                if (!accept)
                {
                    RejectCount.IncrementAndGet();
                }
                return accept;
            }
        }

        // compares just the weight side of the pair
        internal static readonly IComparer<Pair> minPairWeightComparator = new ComparatorAnonymousInnerClassHelper2();

        private class ComparatorAnonymousInnerClassHelper2 : IComparer<Pair>
        {
            public ComparatorAnonymousInnerClassHelper2()
            {
            }

            public virtual int Compare(Pair left, Pair right)
            {
                return left.Output1.GetValueOrDefault().CompareTo(right.Output1.GetValueOrDefault());
            }
        }

        /// <summary>
        /// like testShortestPaths, but uses pairoutputs so we have both a weight and an output </summary>
        [Test]
        public virtual void TestShortestPathsWFST()
        {

            PairOutputs<long?, long?> outputs = new PairOutputs<long?, long?>(PositiveIntOutputs.Singleton, PositiveIntOutputs.Singleton); // output -  weight

            Builder<Pair> builder = new Builder<Pair>(FST.INPUT_TYPE.BYTE1, outputs);

            IntsRef scratch = new IntsRef();
            builder.Add(Util.ToIntsRef(new BytesRef("aab"), scratch), outputs.NewPair(22L, 57L));
            builder.Add(Util.ToIntsRef(new BytesRef("aac"), scratch), outputs.NewPair(7L, 36L));
            builder.Add(Util.ToIntsRef(new BytesRef("ax"), scratch), outputs.NewPair(17L, 85L));
            FST<Pair> fst = builder.Finish();
            //Writer w = new OutputStreamWriter(new FileOutputStream("out.dot"));
            //Util.toDot(fst, w, false, false);
            //w.Dispose();

            Util.TopResults<Pair> res = Util.ShortestPaths(fst, fst.GetFirstArc(new FST.Arc<Pair>()), outputs.NoOutput, minPairWeightComparator, 3, true);
            Assert.IsTrue(res.IsComplete);
            Assert.AreEqual(3, res.TopN.Count);

            Assert.AreEqual(Util.ToIntsRef(new BytesRef("aac"), scratch), res.TopN[0].Input);
            Assert.AreEqual(7L, res.TopN[0].Output.Output1); // weight
            Assert.AreEqual(36L, res.TopN[0].Output.Output2); // output

            Assert.AreEqual(Util.ToIntsRef(new BytesRef("ax"), scratch), res.TopN[1].Input);
            Assert.AreEqual(17L, res.TopN[1].Output.Output1); // weight
            Assert.AreEqual(85L, res.TopN[1].Output.Output2); // output

            Assert.AreEqual(Util.ToIntsRef(new BytesRef("aab"), scratch), res.TopN[2].Input);
            Assert.AreEqual(22L, res.TopN[2].Output.Output1); // weight
            Assert.AreEqual(57L, res.TopN[2].Output.Output2); // output
        }

        [Test]
        public virtual void TestShortestPathsRandom()
        {
            Random random = Random();
            int numWords = AtLeast(1000);

            SortedDictionary<string, long> slowCompletor = new SortedDictionary<string, long>();
            SortedSet<string> allPrefixes = new SortedSet<string>();

            PositiveIntOutputs outputs = PositiveIntOutputs.Singleton;
            Builder<long?> builder = new Builder<long?>(FST.INPUT_TYPE.BYTE1, outputs);
            IntsRef scratch = new IntsRef();

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
                int weight = TestUtil.NextInt(random, 1, 100); // weights 1..100
                slowCompletor[s] = (long)weight;
            }

            foreach (KeyValuePair<string, long> e in slowCompletor)
            {
                //System.out.println("add: " + e);
                builder.Add(Util.ToIntsRef(new BytesRef(e.Key), scratch), e.Value);
            }

            FST<long?> fst = builder.Finish();
            //System.out.println("SAVE out.dot");
            //Writer w = new OutputStreamWriter(new FileOutputStream("out.dot"));
            //Util.toDot(fst, w, false, false);
            //w.Dispose();

            BytesReader reader = fst.BytesReader;

            //System.out.println("testing: " + allPrefixes.Size() + " prefixes");
            foreach (string prefix in allPrefixes)
            {
                // 1. run prefix against fst, then complete by value
                //System.out.println("TEST: " + prefix);

                long? prefixOutput = 0;
                FST.Arc<long?> arc = fst.GetFirstArc(new FST.Arc<long?>());
                for (int idx = 0; idx < prefix.Length; idx++)
                {
                    if (fst.FindTargetArc((int)prefix[idx], arc, arc, reader) == null)
                    {
                        Assert.Fail();
                    }
                    prefixOutput += arc.Output;
                }

                int topN = TestUtil.NextInt(random, 1, 10);

                Util.TopResults<long?> r = Util.ShortestPaths(fst, arc, fst.Outputs.NoOutput, minLongComparator, topN, true);
                Assert.IsTrue(r.IsComplete);

                // 2. go thru whole treemap (slowCompletor) and check its actually the best suggestion
                List<Util.Result<long?>> matches = new List<Util.Result<long?>>();

                // TODO: could be faster... but its slowCompletor for a reason
                foreach (KeyValuePair<string, long> e in slowCompletor)
                {
                    if (e.Key.StartsWith(prefix))
                    {
                        //System.out.println("  consider " + e.getKey());
                        matches.Add(new Util.Result<long?>(Util.ToIntsRef(new BytesRef(e.Key.Substring(prefix.Length)), new IntsRef()), e.Value - prefixOutput));
                    }
                }

                Assert.IsTrue(matches.Count > 0);
                matches.Sort(new TieBreakByInputComparator<long?>(minLongComparator));
                if (matches.Count > topN)
                {
                    matches.SubList(topN, matches.Count).Clear();
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

        private class TieBreakByInputComparator<T> : IComparer<Util.Result<T>>
        {
            internal readonly IComparer<T> Comparator;
            public TieBreakByInputComparator(IComparer<T> comparator)
            {
                this.Comparator = comparator;
            }

            public virtual int Compare(Util.Result<T> a, Util.Result<T> b)
            {
                int cmp = Comparator.Compare(a.Output, b.Output);
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
            private readonly TestFSTs OuterInstance;

            internal long a;
            internal long b;

            internal TwoLongs(TestFSTs outerInstance, long a, long b)
            {
                this.OuterInstance = outerInstance;
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

            SortedDictionary<string, TwoLongs> slowCompletor = new SortedDictionary<string, TwoLongs>();
            SortedSet<string> allPrefixes = new SortedSet<string>();

            PairOutputs<long?, long?> outputs = new PairOutputs<long?, long?>(PositiveIntOutputs.Singleton, PositiveIntOutputs.Singleton); // output -  weight
            Builder<Pair> builder = new Builder<Pair>(FST.INPUT_TYPE.BYTE1, outputs);
            IntsRef scratch = new IntsRef();

            Random random = Random();
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
                int weight = TestUtil.NextInt(random, 1, 100); // weights 1..100
                int output = TestUtil.NextInt(random, 0, 500); // outputs 0..500
                slowCompletor[s] = new TwoLongs(this, weight, output);
            }

            foreach (KeyValuePair<string, TwoLongs> e in slowCompletor)
            {
                //System.out.println("add: " + e);
                long weight = e.Value.a;
                long output = e.Value.b;
                builder.Add(Util.ToIntsRef(new BytesRef(e.Key), scratch), outputs.NewPair(weight, output));
            }

            FST<Pair> fst = builder.Finish();
            //System.out.println("SAVE out.dot");
            //Writer w = new OutputStreamWriter(new FileOutputStream("out.dot"));
            //Util.toDot(fst, w, false, false);
            //w.Dispose();

            BytesReader reader = fst.BytesReader;

            //System.out.println("testing: " + allPrefixes.Size() + " prefixes");
            foreach (string prefix in allPrefixes)
            {
                // 1. run prefix against fst, then complete by value
                //System.out.println("TEST: " + prefix);

                Pair prefixOutput = outputs.NoOutput;
                FST.Arc<Pair> arc = fst.GetFirstArc(new FST.Arc<Pair>());
                for (int idx = 0; idx < prefix.Length; idx++)
                {
                    if (fst.FindTargetArc((int)prefix[idx], arc, arc, reader) == null)
                    {
                        Assert.Fail();
                    }
                    prefixOutput = outputs.Add(prefixOutput, arc.Output);
                }

                int topN = TestUtil.NextInt(random, 1, 10);

                Util.TopResults<Pair> r = Util.ShortestPaths(fst, arc, fst.Outputs.NoOutput, minPairWeightComparator, topN, true);
                Assert.IsTrue(r.IsComplete);
                // 2. go thru whole treemap (slowCompletor) and check its actually the best suggestion
                List<Util.Result<Pair>> matches = new List<Util.Result<Pair>>();

                // TODO: could be faster... but its slowCompletor for a reason
                foreach (KeyValuePair<string, TwoLongs> e in slowCompletor)
                {
                    if (e.Key.StartsWith(prefix))
                    {
                        //System.out.println("  consider " + e.getKey());
                        matches.Add(new Util.Result<Pair>(Util.ToIntsRef(new BytesRef(e.Key.Substring(prefix.Length)), new IntsRef()),
                            outputs.NewPair(e.Value.a - prefixOutput.Output1, e.Value.b - prefixOutput.Output2)));
                    }
                }

                Assert.IsTrue(matches.Count > 0);
                matches.Sort(new TieBreakByInputComparator<Pair>(minPairWeightComparator));
                if (matches.Count > topN)
                {
                    matches.SubList(topN, matches.Count).Clear();
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
            IntsRef input = new IntsRef();
            input.Grow(1);
            input.Length = 1;
            BytesRef output = new BytesRef(bytes);
            for (int arc = 0; arc < 6; arc++)
            {
                input.Ints[0] = arc;
                output.Bytes[0] = (byte)arc;
                builder.Add(input, BytesRef.DeepCopyOf(output));
            }

            FST<BytesRef> fst = builder.Finish();
            for (int arc = 0; arc < 6; arc++)
            {
                input.Ints[0] = arc;
                BytesRef result = Util.Get(fst, input);
                Assert.IsNotNull(result);
                Assert.AreEqual(300, result.Length);
                Assert.AreEqual(result.Bytes[result.Offset], arc);
                for (int byteIDX = 1; byteIDX < result.Length; byteIDX++)
                {
                    Assert.AreEqual(0, result.Bytes[result.Offset + byteIDX]);
                }
            }
        }
    }
}