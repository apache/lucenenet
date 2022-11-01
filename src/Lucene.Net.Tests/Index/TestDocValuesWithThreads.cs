using J2N.Threading;
using Lucene.Net.Documents;
using Lucene.Net.Index.Extensions;
using Lucene.Net.Search;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using JCG = J2N.Collections.Generic;
using Assert = Lucene.Net.TestFramework.Assert;
using Console = Lucene.Net.Util.SystemConsole;
using RandomizedTesting.Generators;

namespace Lucene.Net.Index
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

    using BinaryDocValuesField = BinaryDocValuesField;
    using BytesRef = Lucene.Net.Util.BytesRef;
    using Directory = Lucene.Net.Store.Directory;
    using Document = Documents.Document;
    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
    using MockAnalyzer = Lucene.Net.Analysis.MockAnalyzer;
    using NumericDocValuesField = NumericDocValuesField;
    using SortedDocValuesField = SortedDocValuesField;
    using TestUtil = Lucene.Net.Util.TestUtil;

    [SuppressCodecs("Lucene3x")]
    [TestFixture]
    public class TestDocValuesWithThreads : LuceneTestCase
    {
        [Test]
        public virtual void Test()
        {
            Directory dir = NewDirectory();
            IndexWriter w = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)).SetMergePolicy(NewLogMergePolicy()));

            IList<long> numbers = new JCG.List<long>();
            IList<BytesRef> binary = new JCG.List<BytesRef>();
            IList<BytesRef> sorted = new JCG.List<BytesRef>();
            int numDocs = AtLeast(100);
            for (int i = 0; i < numDocs; i++)
            {
                Document d = new Document();
                long number = Random.NextInt64();
                d.Add(new NumericDocValuesField("number", number));
                BytesRef bytes = new BytesRef(TestUtil.RandomRealisticUnicodeString(Random));
                d.Add(new BinaryDocValuesField("bytes", bytes));
                binary.Add(bytes);
                bytes = new BytesRef(TestUtil.RandomRealisticUnicodeString(Random));
                d.Add(new SortedDocValuesField("sorted", bytes));
                sorted.Add(bytes);
                w.AddDocument(d);
                numbers.Add(number);
            }

            w.ForceMerge(1);
            IndexReader r = w.GetReader();
            w.Dispose();

            Assert.AreEqual(1, r.Leaves.Count);
            AtomicReader ar = (AtomicReader)r.Leaves[0].Reader;

            int numThreads = TestUtil.NextInt32(Random, 2, 5);
            IList<ThreadJob> threads = new JCG.List<ThreadJob>();
            CountdownEvent startingGun = new CountdownEvent(1);
            for (int t = 0; t < numThreads; t++)
            {
                Random threadRandom = new J2N.Randomizer(Random.NextInt64());
                ThreadJob thread = new ThreadAnonymousClass(this, numbers, binary, sorted, numDocs, ar, startingGun, threadRandom);
                thread.Start();
                threads.Add(thread);
            }

            startingGun.Signal();

            foreach (ThreadJob thread in threads)
            {
                thread.Join();
            }

            r.Dispose();
            dir.Dispose();
        }

        private sealed class ThreadAnonymousClass : ThreadJob
        {
            private readonly TestDocValuesWithThreads outerInstance;

            private readonly IList<long> numbers;
            private readonly IList<BytesRef> binary;
            private readonly IList<BytesRef> sorted;
            private readonly int numDocs;
            private readonly AtomicReader ar;
            private readonly CountdownEvent startingGun;
            private readonly Random threadRandom;

            public ThreadAnonymousClass(TestDocValuesWithThreads outerInstance, IList<long> numbers, IList<BytesRef> binary, IList<BytesRef> sorted, int numDocs, AtomicReader ar, CountdownEvent startingGun, Random threadRandom)
            {
                this.outerInstance = outerInstance;
                this.numbers = numbers;
                this.binary = binary;
                this.sorted = sorted;
                this.numDocs = numDocs;
                this.ar = ar;
                this.startingGun = startingGun;
                this.threadRandom = threadRandom;
            }

            public override void Run()
            {
                try
                {
                    //NumericDocValues ndv = ar.GetNumericDocValues("number");
                    FieldCache.Int64s ndv = FieldCache.DEFAULT.GetInt64s(ar, "number", false);
                    //BinaryDocValues bdv = ar.GetBinaryDocValues("bytes");
                    BinaryDocValues bdv = FieldCache.DEFAULT.GetTerms(ar, "bytes", false);
                    SortedDocValues sdv = FieldCache.DEFAULT.GetTermsIndex(ar, "sorted");
                    startingGun.Wait();
                    int iters = AtLeast(1000);
                    BytesRef scratch = new BytesRef();
                    BytesRef scratch2 = new BytesRef();
                    for (int iter = 0; iter < iters; iter++)
                    {
                        int docID = threadRandom.Next(numDocs);
                        switch (threadRandom.Next(6))
                        {
#pragma warning disable 612, 618
                            case 0:
                                Assert.AreEqual((sbyte)numbers[docID], (sbyte)FieldCache.DEFAULT.GetBytes(ar, "number", false).Get(docID));
                                break;

                            case 1:
                                Assert.AreEqual((short)numbers[docID], FieldCache.DEFAULT.GetInt16s(ar, "number", false).Get(docID));
                                break;
#pragma warning restore 612, 618

                            case 2:
                                Assert.AreEqual((int)numbers[docID], FieldCache.DEFAULT.GetInt32s(ar, "number", false).Get(docID));
                                break;

                            case 3:
                                Assert.AreEqual((long)numbers[docID], FieldCache.DEFAULT.GetInt64s(ar, "number", false).Get(docID));
                                break;

                            case 4:
                                Assert.AreEqual(J2N.BitConversion.Int32BitsToSingle((int)numbers[docID]), FieldCache.DEFAULT.GetSingles(ar, "number", false).Get(docID), 0.0f);
                                break;

                            case 5:
                                Assert.AreEqual(J2N.BitConversion.Int64BitsToDouble((long)numbers[docID]), FieldCache.DEFAULT.GetDoubles(ar, "number", false).Get(docID), 0.0);
                                break;
                        }
                        bdv.Get(docID, scratch);
                        Assert.AreEqual(binary[docID], scratch);
                        // Cannot share a single scratch against two "sources":
                        sdv.Get(docID, scratch2);
                        Assert.AreEqual(sorted[docID], scratch2);
                    }
                }
                catch (Exception e) when (e.IsException())
                {
                    throw RuntimeException.Create(e);
                }
            }
        }

        [Test]
        [Timeout(600_000)] // 10 minutes
        public virtual void Test2()
        {
            Random random = Random;
            int NUM_DOCS = AtLeast(100);
            Directory dir = NewDirectory();
            RandomIndexWriter writer = new RandomIndexWriter(random, dir);
            bool allowDups = random.NextBoolean();
            ISet<string> seen = new JCG.HashSet<string>();
            if (Verbose)
            {
                Console.WriteLine("TEST: NUM_DOCS=" + NUM_DOCS + " allowDups=" + allowDups);
            }
            int numDocs = 0;
            IList<BytesRef> docValues = new JCG.List<BytesRef>();

            // TODO: deletions
            while (numDocs < NUM_DOCS)
            {
                string s;
                if (random.NextBoolean())
                {
                    s = TestUtil.RandomSimpleString(random);
                }
                else
                {
                    s = TestUtil.RandomUnicodeString(random);
                }
                BytesRef br = new BytesRef(s);

                if (!allowDups)
                {
                    if (seen.Contains(s))
                    {
                        continue;
                    }
                    seen.Add(s);
                }

                if (Verbose)
                {
                    Console.WriteLine("  " + numDocs + ": s=" + s);
                }

                Document doc = new Document();
                doc.Add(new SortedDocValuesField("stringdv", br));
                doc.Add(new NumericDocValuesField("id", numDocs));
                docValues.Add(br);
                writer.AddDocument(doc);
                numDocs++;

                if (random.Next(40) == 17)
                {
                    // force flush
                    writer.GetReader().Dispose();
                }
            }

            writer.ForceMerge(1);
            DirectoryReader r = writer.GetReader();
            writer.Dispose();

            AtomicReader sr = GetOnlySegmentReader(r);

            long END_TIME = (J2N.Time.NanoTime() / J2N.Time.MillisecondsPerNanosecond) + (TestNightly ? 30 : 1); // LUCENENET: Use NanoTime() rather than CurrentTimeMilliseconds() for more accurate/reliable results

            int NUM_THREADS = TestUtil.NextInt32(LuceneTestCase.Random, 1, 10);
            ThreadJob[] threads = new ThreadJob[NUM_THREADS];
            for (int thread = 0; thread < NUM_THREADS; thread++)
            {
                threads[thread] = new ThreadAnonymousClass2(random, docValues, sr, END_TIME);
                threads[thread].Start();
            }

            foreach (ThreadJob thread in threads)
            {
                thread.Join();
            }

            r.Dispose();
            dir.Dispose();
        }

        private sealed class ThreadAnonymousClass2 : ThreadJob
        {
            private readonly Random random;
            private readonly IList<BytesRef> docValues;
            private readonly AtomicReader sr;
            private readonly long endTime;

            public ThreadAnonymousClass2(Random random, IList<BytesRef> docValues, AtomicReader sr, long endTime)
            {
                this.random = random;
                this.docValues = docValues;
                this.sr = sr;
                this.endTime = endTime;
            }

            public override void Run()
            {
                SortedDocValues stringDVDirect;
                NumericDocValues docIDToID;
                try
                {
                    stringDVDirect = sr.GetSortedDocValues("stringdv");
                    docIDToID = sr.GetNumericDocValues("id");
                    Assert.IsNotNull(stringDVDirect);
                }
                catch (Exception ioe) when (ioe.IsIOException())
                {
                    throw RuntimeException.Create(ioe);
                }
                while (J2N.Time.NanoTime() / J2N.Time.MillisecondsPerNanosecond < endTime) // LUCENENET: Use NanoTime() rather than CurrentTimeMilliseconds() for more accurate/reliable results
                {
                    SortedDocValues source;
                    source = stringDVDirect;
                    BytesRef scratch = new BytesRef();

                    for (int iter = 0; iter < 100; iter++)
                    {
                        int docID = random.Next(sr.MaxDoc);
                        source.Get(docID, scratch);
                        Assert.AreEqual(docValues[(int)docIDToID.Get(docID)], scratch);
                    }
                }
            }
        }
    }
}