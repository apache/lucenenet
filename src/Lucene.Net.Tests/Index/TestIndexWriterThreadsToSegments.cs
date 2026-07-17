using J2N.Threading;
using J2N.Threading.Atomic;
using Lucene.Net.Analysis;
using Lucene.Net.Codecs;
using Lucene.Net.Codecs.Lucene46;
using Lucene.Net.Documents;
using Lucene.Net.Store;
using Lucene.Net.Util;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading;
using JCG = J2N.Collections.Generic;

// ReSharper disable AccessToDisposedClosure - thread is always joined or otherwise the lambda doesn't outlive the test method

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

    [TestFixture]
    public class TestIndexWriterThreadsToSegments : LuceneTestCase
    {
        [Test]
        public void TestSegmentCountOnFlushBasic()
        {
            using Directory dir = NewDirectory();
            using IndexWriter w = new IndexWriter(dir, new IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)));
            using CountdownLatch startingGun = new CountdownLatch(1);
            using CountdownLatch startDone = new CountdownLatch(2);
            using CountdownLatch middleGun = new CountdownLatch(1);
            using CountdownLatch finalGun = new CountdownLatch(1);
            Thread[] threads = new Thread[2];
            for (int i = 0; i < threads.Length; i++)
            {
                int threadID = i;
                threads[i] = new Thread(() =>
                {
                    try
                    {
                        startingGun.Wait();
                        Document doc = new Document();
                        doc.Add(NewTextField("field", "here is some text", Field.Store.NO));
                        w.AddDocument(doc);
                        startDone.Signal();

                        middleGun.Wait();
                        if (threadID == 0)
                        {
                            w.AddDocument(doc);
                        }
                        else
                        {
                            finalGun.Wait();
                            w.AddDocument(doc);
                        }
                    }
                    catch (Exception e)
                    {
                        throw RuntimeException.Create(e);
                    }
                });
                threads[i].Start();
            }

            startingGun.Signal();
            startDone.Wait();

            // LUCENENET: split declarations for outer scope reuse below
            IndexReader r;
            int numSegments;
            using (r = DirectoryReader.Open(w, true))
            {
                Assert.AreEqual(2, r.NumDocs);
                numSegments = r.Leaves.Count;
                // 1 segment if the threads ran sequentially, else 2:
                Assert.IsTrue(numSegments <= 2);
                // r.Dispose(); // LUCENENET: disposed by using statement
            }

            middleGun.Signal();
            threads[0].Join();

            finalGun.Signal();
            threads[1].Join();

            using (r = DirectoryReader.Open(w, true))
            {
                Assert.AreEqual(4, r.NumDocs);
                // Both threads should have shared a single thread state since they did not try to index concurrently:
                Assert.AreEqual(1 + numSegments, r.Leaves.Count);
                // r.Dispose(); // LUCENENET: disposed by using statement
            }

            // LUCENENET: disposed by using statement
            // w.Dispose();
            // dir.Dispose();
        }

        /// <summary>
        /// Maximum number of simultaneous threads to use for each iteration.
        /// </summary>
        private const int MAX_THREADS_AT_ONCE = 10;

        private class CheckSegmentCount : ThreadJob, IDisposable
        {
            private readonly IndexWriter w;
            private readonly AtomicInt32 maxThreadCountPerIter;
            private readonly AtomicInt32 indexingCount;
            private DirectoryReader r;

            public CheckSegmentCount(IndexWriter w, AtomicInt32 maxThreadCountPerIter, AtomicInt32 indexingCount)
            {
                this.w = w;
                this.maxThreadCountPerIter = maxThreadCountPerIter;
                this.indexingCount = indexingCount;
                r = DirectoryReader.Open(w, true);
                Assert.AreEqual(0, r.Leaves.Count);
                SetNextIterThreadCount();
            }

            public override void Run()
            {
                try
                {
                    int oldSegmentCount = r.Leaves.Count;
                    DirectoryReader r2 = DirectoryReader.OpenIfChanged(r);
                    Assert.IsNotNull(r2);
                    r.Dispose();
                    r = r2;
                    int maxThreadStates = w.Config.MaxThreadStates;
                    int maxExpectedSegments = oldSegmentCount + Math.Min(maxThreadStates, maxThreadCountPerIter.Value);
                    if (Verbose)
                    {
                        Console.WriteLine($"TEST: iter done; now verify oldSegCount={oldSegmentCount} newSegCount={r2.Leaves.Count} maxExpected={maxExpectedSegments}");
                    }

                    // NOTE: it won't necessarily be ==, in case some threads were strangely scheduled and never conflicted with one another (should be uncommon...?):
                    Assert.IsTrue(r.Leaves.Count <= maxExpectedSegments);
                    SetNextIterThreadCount();
                }
                catch (Exception e)
                {
                    throw RuntimeException.Create(e);
                }
            }

            private void SetNextIterThreadCount()
            {
                indexingCount.Value = 0;
                maxThreadCountPerIter.Value = TestUtil.NextInt32(Random, 1, MAX_THREADS_AT_ONCE);
                if (Verbose)
                {
                    Console.WriteLine($"TEST: iter set maxThreadCount={maxThreadCountPerIter.Value}");
                }
            }

            public void Dispose()
            {
                r.Dispose();
                r = null;
            }
        }

        // LUCENE-5644: index docs w/ multiple threads but in between flushes we limit how many threads can index concurrently in the next
        // iteration, and then verify that no more segments were flushed than number of threads:
        [Test]
        public void TestSegmentCountOnFlushRandom()
        {
            using Directory dir = NewFSDirectory(CreateTempDir());
            IndexWriterConfig iwc = new IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random));

            int maxThreadStates = TestUtil.NextInt32(Random, 1, 12);

            if (Verbose)
            {
                Console.WriteLine($"TEST: maxThreadStates={maxThreadStates}");
            }

            // Never trigger flushes (so we only flush on getReader):
            iwc.MaxBufferedDocs = 100000000;
            iwc.RAMBufferSizeMB = -1;
            iwc.MaxThreadStates = maxThreadStates;

            // Never trigger merges (so we can simplistically count flushed segments):
            iwc.MergePolicy = NoMergePolicy.NO_COMPOUND_FILES;

            using IndexWriter w = new IndexWriter(dir, iwc);

            // How many threads are indexing in the current cycle:
            AtomicInt32 indexingCount = new AtomicInt32();

            // How many threads we will use on each cycle:
            AtomicInt32 maxThreadCount = new AtomicInt32();

            using CheckSegmentCount checker = new CheckSegmentCount(w, maxThreadCount, indexingCount);

            // We spin up 10 threads up front, but then in between flushes we limit how many can run on each iteration
            const int ITERATIONS = 100;
            Thread[] threads = new Thread[MAX_THREADS_AT_ONCE];

            // We use this to stop all threads once they've indexed their docs in the current iter, and pull a new NRT reader, and verify the
            // segment count:
            // ReSharper disable once AccessToDisposedClosure - lifetime of barrier is this method
            using Barrier barrier = new Barrier(MAX_THREADS_AT_ONCE, _ => checker.Run());

            for (int i = 0; i < threads.Length; i++)
            {
                threads[i] = new Thread(() =>
                {
                    try
                    {
                        for (int iter = 0; iter < ITERATIONS; iter++)
                        {
                            if (indexingCount.IncrementAndGet() <= maxThreadCount.Value)
                            {
                                if (Verbose)
                                {
                                    Console.WriteLine($"TEST: {Thread.CurrentThread.Name}: do index");
                                }

                                // We get to index on this cycle:
                                Document doc = new Document();
                                doc.Add(new TextField("field", "here is some text that is a bit longer than normal trivial text", Field.Store.NO));
                                for (int j = 0; j < 200; j++)
                                {
                                    w.AddDocument(doc);
                                }
                            }
                            else
                            {
                                // We lose: no indexing for us on this cycle
                                if (Verbose)
                                {
                                    Console.WriteLine($"TEST: {Thread.CurrentThread.Name}: don't index");
                                }
                            }

                            barrier.SignalAndWait();
                        }
                    }
                    catch (Exception e)
                    {
                        throw RuntimeException.Create(e);
                    }
                });

                threads[i].Start();
            }

            foreach (Thread t in threads)
            {
                t.Join();
            }
        }

        [Test]
        public void TestManyThreadsClose()
        {
            using Directory dir = NewDirectory();
            Thread[] threads; // LUCENENET: moved declaration to outer scope
            using (RandomIndexWriter w = new RandomIndexWriter(Random, dir))
            {
                w.DoRandomForceMerge = false;
                threads = new Thread[TestUtil.NextInt32(Random, 4, 30)];
                using CountdownLatch startingGun = new CountdownLatch(1);
                for (int i = 0; i < threads.Length; i++)
                {
                    threads[i] = new Thread(() =>
                    {
                        try
                        {
                            startingGun.Wait();
                            Document doc = new Document();
                            doc.Add(new TextField("field", "here is some text that is a bit longer than normal trivial text", Field.Store.NO));
                            while (true)
                            {
                                w.AddDocument(doc);
                            }
                        }
                        catch (Exception ace) when (ace.IsAlreadyClosedException())
                        {
                            // ok
                        }
                        catch (Exception e)
                        {
                            throw RuntimeException.Create(e);
                        }
                    });
                    threads[i].Start();
                }

                startingGun.Signal();

                Thread.Sleep(100);
                // w.Dispose(); // LUCENENET - disposed by using statement
            }

            foreach (Thread t in threads)
            {
                t.Join();
            }

            // dir.Dispose(); // LUCENENET - disposed by using statement
        }

        [Test]
        public void TestDocsStuckInRAMForever()
        {
            using Directory dir = NewDirectory();
            IndexWriterConfig iwc = new IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random));
            iwc.RAMBufferSizeMB = .2;
            Codec codec = Codec.ForName("Lucene46");
            iwc.Codec = codec;
            iwc.MergePolicy = NoMergePolicy.NO_COMPOUND_FILES;
            using IndexWriter w = new IndexWriter(dir, iwc);
            using CountdownLatch startingGun = new CountdownLatch(1);
            Thread[] threads = new Thread[2];
            for (int i = 0; i < threads.Length; i++)
            {
                int threadID = i;
                threads[i] = new Thread(() =>
                {
                    try
                    {
                        startingGun.Wait();
                        for (int j = 0; j < 1000; j++)
                        {
                            Document doc = new Document();
                            doc.Add(NewStringField("field", "threadID" + threadID, Field.Store.NO));
                            w.AddDocument(doc);
                        }
                    }
                    catch (Exception e)
                    {
                        throw RuntimeException.Create(e);
                    }
                });
                threads[i].Start();
            }

            startingGun.Signal();
            foreach (Thread t in threads)
            {
                t.Join();
            }

            ISet<string> segSeen = new JCG.HashSet<string>();
            int thread0Count = 0;
            int thread1Count = 0;

            // At this point the writer should have 2 thread states w/ docs; now we index with only 1 thread until we see all 1000 thread0 & thread1
            // docs flushed.  If the writer incorrectly holds onto previously indexed docs forever then this will run forever:
            while (thread0Count < 1000 || thread1Count < 1000)
            {
                Document doc = new Document();
                doc.Add(NewStringField("field", "threadIDmain", Field.Store.NO));
                w.AddDocument(doc);

                foreach (string fileName in dir.ListAll())
                {
                    if (fileName.EndsWith(".si", StringComparison.Ordinal))
                    {
                        string segName = IndexFileNames.ParseSegmentName(fileName);
                        if (!segSeen.Contains(segName))
                        {
                            segSeen.Add(segName);
                            SegmentInfo si = new Lucene46SegmentInfoFormat().SegmentInfoReader.Read(dir, segName, IOContext.DEFAULT);
                            si.Codec = codec;
                            SegmentCommitInfo sci = new SegmentCommitInfo(si, 0, -1, -1);
                            // LUCENENET: try/finally with close changed to using statement
                            using SegmentReader sr = new SegmentReader(sci, 1, IOContext.DEFAULT);
                            thread0Count += sr.DocFreq(new Term("field", "threadID0"));
                            thread1Count += sr.DocFreq(new Term("field", "threadID1"));
                        }
                    }
                }
            }

            // LUCENENET: disposed via using statements
            // w.Dispose();
            // dir.Dispose();
        }
    }
}
