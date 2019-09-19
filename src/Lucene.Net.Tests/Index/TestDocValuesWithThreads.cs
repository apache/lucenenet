using Lucene.Net.Documents;
using Lucene.Net.Randomized.Generators;
using Lucene.Net.Search;
using Lucene.Net.Support;
using Lucene.Net.Support.Threading;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Console = Lucene.Net.Support.SystemConsole;

namespace Lucene.Net.Index
{
    using BinaryDocValuesField = BinaryDocValuesField;
    using BytesRef = Lucene.Net.Util.BytesRef;
    using Directory = Lucene.Net.Store.Directory;
    using Document = Documents.Document;
    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;

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

            IList<long?> numbers = new List<long?>();
            IList<BytesRef> binary = new List<BytesRef>();
            IList<BytesRef> sorted = new List<BytesRef>();
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
            IList<ThreadClass> threads = new List<ThreadClass>();
            CountdownEvent startingGun = new CountdownEvent(1);
            for (int t = 0; t < numThreads; t++)
            {
                Random threadRandom = new Random(Random.Next());
                ThreadClass thread = new ThreadAnonymousInnerClassHelper(this, numbers, binary, sorted, numDocs, ar, startingGun, threadRandom);
                thread.Start();
                threads.Add(thread);
            }

            startingGun.Signal();

            foreach (ThreadClass thread in threads)
            {
                thread.Join();
            }

            r.Dispose();
            dir.Dispose();
        }

        private class ThreadAnonymousInnerClassHelper : ThreadClass
        {
            private readonly TestDocValuesWithThreads OuterInstance;

            private IList<long?> Numbers;
            private IList<BytesRef> Binary;
            private IList<BytesRef> Sorted;
            private int NumDocs;
            private AtomicReader Ar;
            private CountdownEvent StartingGun;
            private Random ThreadRandom;

            public ThreadAnonymousInnerClassHelper(TestDocValuesWithThreads outerInstance, IList<long?> numbers, IList<BytesRef> binary, IList<BytesRef> sorted, int numDocs, AtomicReader ar, CountdownEvent startingGun, Random threadRandom)
            {
                this.OuterInstance = outerInstance;
                this.Numbers = numbers;
                this.Binary = binary;
                this.Sorted = sorted;
                this.NumDocs = numDocs;
                this.Ar = ar;
                this.StartingGun = startingGun;
                this.ThreadRandom = threadRandom;
            }

            public override void Run()
            {
                try
                {
                    //NumericDocValues ndv = ar.GetNumericDocValues("number");
                    FieldCache.Int64s ndv = FieldCache.DEFAULT.GetInt64s(Ar, "number", false);
                    //BinaryDocValues bdv = ar.GetBinaryDocValues("bytes");
                    BinaryDocValues bdv = FieldCache.DEFAULT.GetTerms(Ar, "bytes", false);
                    SortedDocValues sdv = FieldCache.DEFAULT.GetTermsIndex(Ar, "sorted");
                    StartingGun.Wait();
                    int iters = AtLeast(1000);
                    BytesRef scratch = new BytesRef();
                    BytesRef scratch2 = new BytesRef();
                    for (int iter = 0; iter < iters; iter++)
                    {
                        int docID = ThreadRandom.Next(NumDocs);
                        switch (ThreadRandom.Next(6))
                        {
#pragma warning disable 612, 618
                            case 0:
                                Assert.AreEqual((long)(sbyte)Numbers[docID], (sbyte)FieldCache.DEFAULT.GetBytes(Ar, "number", false).Get(docID));
                                break;

                            case 1:
                                Assert.AreEqual((long)(short)Numbers[docID], FieldCache.DEFAULT.GetInt16s(Ar, "number", false).Get(docID));
                                break;
#pragma warning restore 612, 618

                            case 2:
                                Assert.AreEqual((long)(int)Numbers[docID], FieldCache.DEFAULT.GetInt32s(Ar, "number", false).Get(docID));
                                break;

                            case 3:
                                Assert.AreEqual((long)Numbers[docID], FieldCache.DEFAULT.GetInt64s(Ar, "number", false).Get(docID));
                                break;

                            case 4:
                                Assert.AreEqual(Number.Int32BitsToSingle((int)Numbers[docID]), FieldCache.DEFAULT.GetSingles(Ar, "number", false).Get(docID), 0.0f);
                                break;

                            case 5:
                                Assert.AreEqual(BitConverter.Int64BitsToDouble((long)Numbers[docID]), FieldCache.DEFAULT.GetDoubles(Ar, "number", false).Get(docID), 0.0);
                                break;
                        }
                        bdv.Get(docID, scratch);
                        Assert.AreEqual(Binary[docID], scratch);
                        // Cannot share a single scratch against two "sources":
                        sdv.Get(docID, scratch2);
                        Assert.AreEqual(Sorted[docID], scratch2);
                    }
                }
                catch (Exception e)
                {
                    throw new Exception(e.Message, e);
                }
            }
        }

        [Test]
        public virtual void Test2()
        {
            Random random = Random;
            int NUM_DOCS = AtLeast(100);
            Directory dir = NewDirectory();
            RandomIndexWriter writer = new RandomIndexWriter(
#if FEATURE_INSTANCE_TESTDATA_INITIALIZATION
                this,
#endif
                random, dir);
            bool allowDups = random.NextBoolean();
            HashSet<string> seen = new HashSet<string>();
            if (VERBOSE)
            {
                Console.WriteLine("TEST: NUM_DOCS=" + NUM_DOCS + " allowDups=" + allowDups);
            }
            int numDocs = 0;
            IList<BytesRef> docValues = new List<BytesRef>();

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

                if (VERBOSE)
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

            long END_TIME = Environment.TickCount + (TEST_NIGHTLY ? 30 : 1);

            int NUM_THREADS = TestUtil.NextInt32(LuceneTestCase.Random, 1, 10);
            ThreadClass[] threads = new ThreadClass[NUM_THREADS];
            for (int thread = 0; thread < NUM_THREADS; thread++)
            {
                threads[thread] = new ThreadAnonymousInnerClassHelper2(random, docValues, sr, END_TIME);
                threads[thread].Start();
            }

            foreach (ThreadClass thread in threads)
            {
                thread.Join();
            }

            r.Dispose();
            dir.Dispose();
        }

        private class ThreadAnonymousInnerClassHelper2 : ThreadClass
        {
            private Random Random;
            private IList<BytesRef> DocValues;
            private AtomicReader Sr;
            private long END_TIME;

            public ThreadAnonymousInnerClassHelper2(Random random, IList<BytesRef> docValues, AtomicReader sr, long END_TIME)
            {
                this.Random = random;
                this.DocValues = docValues;
                this.Sr = sr;
                this.END_TIME = END_TIME;
            }

            public override void Run()
            {
                Random random = Random;
                SortedDocValues stringDVDirect;
                NumericDocValues docIDToID;
                try
                {
                    stringDVDirect = Sr.GetSortedDocValues("stringdv");
                    docIDToID = Sr.GetNumericDocValues("id");
                    Assert.IsNotNull(stringDVDirect);
                }
                catch (IOException ioe)
                {
                    throw new Exception(ioe.ToString(), ioe);
                }
                while (Environment.TickCount < END_TIME)
                {
                    SortedDocValues source;
                    source = stringDVDirect;
                    BytesRef scratch = new BytesRef();

                    for (int iter = 0; iter < 100; iter++)
                    {
                        int docID = random.Next(Sr.MaxDoc);
                        source.Get(docID, scratch);
                        Assert.AreEqual(DocValues[(int)docIDToID.Get(docID)], scratch);
                    }
                }
            }
        }
    }
}