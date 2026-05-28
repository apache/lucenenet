using J2N.Threading;
using J2N.Threading.Atomic;
using Lucene.Net.Analysis;
using Lucene.Net.Codecs;
using Lucene.Net.Codecs.Lucene46;
using Lucene.Net.Documents;
using Lucene.Net.Index.Extensions;
using Lucene.Net.Store;
using Lucene.Net.Util;
using NUnit.Framework;
using RandomizedTesting.Generators;
using System;
using System.Collections.Generic;
using System.Threading;
using Assert = Lucene.Net.TestFramework.Assert;

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
        // LUCENE-5644: for first segment, two threads each indexed one doc (likely concurrently), but for second segment, each thread indexed the
        // doc NOT at the same time, and should have shared the same thread state / segment
        [Test]
        public virtual void TestSegmentCountOnFlushBasic()
        {
            Directory dir = NewDirectory();
            IndexWriter w = new IndexWriter(dir, new IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)));
            CountdownEvent startingGun = new CountdownEvent(1);
            CountdownEvent startDone = new CountdownEvent(2);
            CountdownEvent middleGun = new CountdownEvent(1);
            CountdownEvent finalGun = new CountdownEvent(1);
            ThreadJob[] threads = new ThreadJob[2];
            for (int i = 0; i < threads.Length; i++)
            {
                int threadID = i;
                threads[i] = new SegmentCountOnFlushBasicThread(w, threadID, startingGun, startDone, middleGun, finalGun);
                threads[i].Start();
            }

            startingGun.Signal();
            startDone.Wait();

            IndexReader r = DirectoryReader.Open(w, true);
            Assert.AreEqual(2, r.NumDocs);
            int numSegments = r.Leaves.Count;
            // 1 segment if the threads ran sequentially, else 2:
            Assert.IsTrue(numSegments <= 2);
            r.Dispose();

            middleGun.Signal();
            threads[0].Join();

            finalGun.Signal();
            threads[1].Join();

            r = DirectoryReader.Open(w, true);
            Assert.AreEqual(4, r.NumDocs);
            // Both threads should have shared a single thread state since they did not try to index concurrently:
            Assert.AreEqual(1 + numSegments, r.Leaves.Count);
            r.Dispose();

            w.Dispose();
            dir.Dispose();
        }

        private sealed class SegmentCountOnFlushBasicThread : ThreadJob
        {
            private readonly IndexWriter w;
            private readonly int threadID;
            private readonly CountdownEvent startingGun;
            private readonly CountdownEvent startDone;
            private readonly CountdownEvent middleGun;
            private readonly CountdownEvent finalGun;

            public SegmentCountOnFlushBasicThread(IndexWriter w, int threadID, CountdownEvent startingGun, CountdownEvent startDone, CountdownEvent middleGun, CountdownEvent finalGun)
            {
                this.w = w;
                this.threadID = threadID;
                this.startingGun = startingGun;
                this.startDone = startDone;
                this.middleGun = middleGun;
                this.finalGun = finalGun;
            }

            public override void Run()
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
                catch (Exception e) when (e.IsException())
                {
                    throw RuntimeException.Create(e);
                }
            }
        }

        /// <summary>
        /// Maximum number of simultaneous threads to use for each iteration.
        /// </summary>
        private const int MAX_THREADS_AT_ONCE = 10;

        private sealed class CheckSegmentCount : IDisposable
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

            public void Run()
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
                        Console.WriteLine("TEST: iter done; now verify oldSegCount=" + oldSegmentCount + " newSegCount=" + r2.Leaves.Count + " maxExpected=" + maxExpectedSegments);
                    }
                    // NOTE: it won't necessarily be ==, in case some threads were strangely scheduled and never conflicted with one another (should be uncommon...?):
                    Assert.IsTrue(r.Leaves.Count <= maxExpectedSegments);
                    SetNextIterThreadCount();
                }
                catch (Exception e) when (e.IsException())
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
                    Console.WriteLine("TEST: iter set maxThreadCount=" + maxThreadCountPerIter);
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
        public virtual void TestSegmentCountOnFlushRandom()
        {
            Directory dir = NewFSDirectory(CreateTempDir());
            IndexWriterConfig iwc = new IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random));

            int maxThreadStates = TestUtil.NextInt32(Random, 1, 12);

            if (Verbose)
            {
                Console.WriteLine("TEST: maxThreadStates=" + maxThreadStates);
            }

            // Never trigger flushes (so we only flush on getReader):
            iwc.SetMaxBufferedDocs(100000000);
            iwc.SetRAMBufferSizeMB(-1);
            iwc.MaxThreadStates = maxThreadStates;

            // Never trigger merges (so we can simplistically count flushed segments):
            iwc.SetMergePolicy(NoMergePolicy.NO_COMPOUND_FILES);

            IndexWriter w = new IndexWriter(dir, iwc);

            // How many threads are indexing in the current cycle:
            AtomicInt32 indexingCount = new AtomicInt32();

            // How many threads we will use on each cycle:
            AtomicInt32 maxThreadCount = new AtomicInt32();

            CheckSegmentCount checker = new CheckSegmentCount(w, maxThreadCount, indexingCount);

            // We spin up 10 threads up front, but then in between flushes we limit how many can run on each iteration
            const int ITERS = 100;
            ThreadJob[] threads = new ThreadJob[MAX_THREADS_AT_ONCE];

            // We use this to stop all threads once they've indexed their docs in the current iter, and pull a new NRT reader, and verify the
            // segment count:
            Barrier barrier = new Barrier(MAX_THREADS_AT_ONCE, _ => checker.Run());

            for (int i = 0; i < threads.Length; i++)
            {
                threads[i] = new SegmentCountOnFlushRandomThread(w, indexingCount, maxThreadCount, barrier, ITERS);
                threads[i].Start();
            }

            foreach (ThreadJob t in threads)
            {
                t.Join();
            }

            IOUtils.Dispose(checker, w, dir);
        }

        private sealed class SegmentCountOnFlushRandomThread : ThreadJob
        {
            private readonly IndexWriter w;
            private readonly AtomicInt32 indexingCount;
            private readonly AtomicInt32 maxThreadCount;
            private readonly Barrier barrier;
            private readonly int iters;

            public SegmentCountOnFlushRandomThread(IndexWriter w, AtomicInt32 indexingCount, AtomicInt32 maxThreadCount, Barrier barrier, int iters)
            {
                this.w = w;
                this.indexingCount = indexingCount;
                this.maxThreadCount = maxThreadCount;
                this.barrier = barrier;
                this.iters = iters;
            }

            public override void Run()
            {
                try
                {
                    for (int iter = 0; iter < iters; iter++)
                    {
                        if (indexingCount.IncrementAndGet() <= maxThreadCount.Value)
                        {
                            if (Verbose)
                            {
                                Console.WriteLine("TEST: " + Thread.CurrentThread.Name + ": do index");
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
                                Console.WriteLine("TEST: " + Thread.CurrentThread.Name + ": don't index");
                            }
                        }
                        barrier.SignalAndWait();
                    }
                }
                catch (Exception e) when (e.IsException())
                {
                    throw RuntimeException.Create(e);
                }
            }
        }

        [Test]
        public virtual void TestManyThreadsClose()
        {
            Directory dir = NewDirectory();
            RandomIndexWriter w = new RandomIndexWriter(Random, dir);
            w.DoRandomForceMerge = false;
            ThreadJob[] threads = new ThreadJob[TestUtil.NextInt32(Random, 4, 30)];
            CountdownEvent startingGun = new CountdownEvent(1);
            for (int i = 0; i < threads.Length; i++)
            {
                threads[i] = new ManyThreadsCloseThread(w, startingGun);
                threads[i].Start();
            }

            startingGun.Signal();

            Thread.Sleep(100);
            // LUCENENET: ported from upstream LUCENE-5871 (commit 2cfcdcc, first released in
            // 4.10.0), pulled in alongside the LUCENE-5871 IndexWriter close fix (#1284).
            // The new Shutdown contract can now throw IllegalStateException if a concurrent
            // thread called prepareCommit; the close is still required to retry afterwards.
            try
            {
                w.Dispose();
            }
            catch (Exception ise) when (ise.IsIllegalStateException())
            {
                // OK but not required
            }
            foreach (ThreadJob t in threads)
            {
                t.Join();
            }
            w.Dispose();
            dir.Dispose();
        }

        private sealed class ManyThreadsCloseThread : ThreadJob
        {
            private readonly RandomIndexWriter w;
            private readonly CountdownEvent startingGun;

            public ManyThreadsCloseThread(RandomIndexWriter w, CountdownEvent startingGun)
            {
                this.w = w;
                this.startingGun = startingGun;
            }

            public override void Run()
            {
                try
                {
                    startingGun.Wait();
                    Document doc = new Document();
                    doc.Add(new TextField("field", "here is some text that is a bit longer than normal trivial text", Field.Store.NO));
                    // LUCENENET: ported from upstream LUCENE-5871 (commit 2cfcdcc, first released
                    // in 4.10.0), pulled in alongside the LUCENE-5871 IndexWriter close fix
                    // (#1284). Bounded loop instead of while(true) so threads exit even if
                    // AlreadyClosedException never fires.
                    for (int i = 0; i < 10000; i++)
                    {
                        w.AddDocument(doc);
                    }
                }
                catch (Exception ace) when (ace.IsAlreadyClosedException())
                {
                    // ok
                }
                catch (Exception e) when (e.IsException())
                {
                    throw RuntimeException.Create(e);
                }
            }
        }

        [Test]
        public virtual void TestDocsStuckInRAMForever()
        {
            Directory dir = NewDirectory();
            IndexWriterConfig iwc = new IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random));
            iwc.SetRAMBufferSizeMB(.2);
            Codec codec = Codec.ForName("Lucene46");
            iwc.SetCodec(codec);
            iwc.SetMergePolicy(NoMergePolicy.NO_COMPOUND_FILES);
            IndexWriter w = new IndexWriter(dir, iwc);
            CountdownEvent startingGun = new CountdownEvent(1);
            ThreadJob[] threads = new ThreadJob[2];
            for (int i = 0; i < threads.Length; i++)
            {
                int threadID = i;
                threads[i] = new DocsStuckInRAMForeverThread(w, threadID, startingGun);
                threads[i].Start();
            }

            startingGun.Signal();
            foreach (ThreadJob t in threads)
            {
                t.Join();
            }

            ISet<string> segSeen = new HashSet<string>();
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
                        if (segSeen.Contains(segName) == false)
                        {
                            segSeen.Add(segName);
                            SegmentInfo si = new Lucene46SegmentInfoFormat().SegmentInfoReader.Read(dir, segName, IOContext.DEFAULT);
                            si.Codec = codec;
                            SegmentCommitInfo sci = new SegmentCommitInfo(si, 0, -1, -1);
                            SegmentReader sr = new SegmentReader(sci, 1, IOContext.DEFAULT);
                            try
                            {
                                thread0Count += sr.DocFreq(new Term("field", "threadID0"));
                                thread1Count += sr.DocFreq(new Term("field", "threadID1"));
                            }
                            finally
                            {
                                sr.Dispose();
                            }
                        }
                    }
                }
            }

            w.Dispose();
            dir.Dispose();
        }

        private sealed class DocsStuckInRAMForeverThread : ThreadJob
        {
            private readonly IndexWriter w;
            private readonly int threadID;
            private readonly CountdownEvent startingGun;

            public DocsStuckInRAMForeverThread(IndexWriter w, int threadID, CountdownEvent startingGun)
            {
                this.w = w;
                this.threadID = threadID;
                this.startingGun = startingGun;
            }

            public override void Run()
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
                catch (Exception e) when (e.IsException())
                {
                    throw RuntimeException.Create(e);
                }
            }
        }
    }
}
