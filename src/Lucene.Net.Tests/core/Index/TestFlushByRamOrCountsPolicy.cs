using System;
using System.Collections.Generic;

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


    using MockAnalyzer = Lucene.Net.Analysis.MockAnalyzer;
    using Document = Lucene.Net.Documents.Document;
    using ThreadState = Lucene.Net.Index.DocumentsWriterPerThreadPool.ThreadState;
    using Directory = Lucene.Net.Store.Directory;
    using MockDirectoryWrapper = Lucene.Net.Store.MockDirectoryWrapper;
    using LineFileDocs = Lucene.Net.Util.LineFileDocs;
    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
    using TestUtil = Lucene.Net.Util.TestUtil;
    using Lucene.Net.Support;
    using NUnit.Framework;

    [TestFixture]
    public class TestFlushByRamOrCountsPolicy : LuceneTestCase
    {

        private static LineFileDocs LineDocFile;

        [OneTimeSetUp]
        public static void BeforeClass()
        {
            LineDocFile = new LineFileDocs(Random(), DefaultCodecSupportsDocValues());
        }

        [OneTimeTearDown]
        public static void AfterClass()
        {
            LineDocFile.Dispose();
            LineDocFile = null;
        }

        [Test]
        public virtual void TestFlushByRam()
        {
            double ramBuffer = (TEST_NIGHTLY ? 1 : 10) + AtLeast(2) + Random().NextDouble();
            RunFlushByRam(1 + Random().Next(TEST_NIGHTLY ? 5 : 1), ramBuffer, false);
        }

        [Test]
        public virtual void TestFlushByRamLargeBuffer()
        {
            // with a 256 mb ram buffer we should never stall
            RunFlushByRam(1 + Random().Next(TEST_NIGHTLY ? 5 : 1), 256d, true);
        }

        protected internal virtual void RunFlushByRam(int numThreads, double maxRamMB, bool ensureNotStalled)
        {
            int numDocumentsToIndex = 10 + AtLeast(30);
            AtomicInteger numDocs = new AtomicInteger(numDocumentsToIndex);
            Directory dir = NewDirectory();
            MockDefaultFlushPolicy flushPolicy = new MockDefaultFlushPolicy();
            MockAnalyzer analyzer = new MockAnalyzer(Random());
            analyzer.MaxTokenLength = TestUtil.NextInt(Random(), 1, IndexWriter.MAX_TERM_LENGTH);

            IndexWriterConfig iwc = NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer).SetFlushPolicy(flushPolicy);
            int numDWPT = 1 + AtLeast(2);
            DocumentsWriterPerThreadPool threadPool = new ThreadAffinityDocumentsWriterThreadPool(numDWPT);
            iwc.SetIndexerThreadPool(threadPool);
            iwc.SetRAMBufferSizeMB(maxRamMB);
            iwc.SetMaxBufferedDocs(IndexWriterConfig.DISABLE_AUTO_FLUSH);
            iwc.SetMaxBufferedDeleteTerms(IndexWriterConfig.DISABLE_AUTO_FLUSH);
            IndexWriter writer = new IndexWriter(dir, iwc);
            flushPolicy = (MockDefaultFlushPolicy)writer.Config.FlushPolicy;
            Assert.IsFalse(flushPolicy.FlushOnDocCount());
            Assert.IsFalse(flushPolicy.FlushOnDeleteTerms());
            Assert.IsTrue(flushPolicy.FlushOnRAM());
            DocumentsWriter docsWriter = writer.DocsWriter;
            Assert.IsNotNull(docsWriter);
            DocumentsWriterFlushControl flushControl = docsWriter.FlushControl;
            Assert.AreEqual(0, flushControl.FlushBytes(), " bytes must be 0 after init");

            IndexThread[] threads = new IndexThread[numThreads];
            for (int x = 0; x < threads.Length; x++)
            {
                threads[x] = new IndexThread(this, numDocs, numThreads, writer, LineDocFile, false);
                threads[x].Start();
            }

            for (int x = 0; x < threads.Length; x++)
            {
                threads[x].Join();
            }
            long maxRAMBytes = (long)(iwc.RAMBufferSizeMB * 1024.0 * 1024.0);
            Assert.AreEqual(0, flushControl.FlushBytes(), " all flushes must be due numThreads=" + numThreads);
            Assert.AreEqual(numDocumentsToIndex, writer.NumDocs());
            Assert.AreEqual(numDocumentsToIndex, writer.MaxDoc);
            Assert.IsTrue(flushPolicy.PeakBytesWithoutFlush <= maxRAMBytes, "peak bytes without flush exceeded watermark");
            AssertActiveBytesAfter(flushControl);
            if (flushPolicy.HasMarkedPending)
            {
                Assert.IsTrue(maxRAMBytes < flushControl.PeakActiveBytes);
            }
            if (ensureNotStalled)
            {
                Assert.IsFalse(docsWriter.FlushControl.StallControl.WasStalled());
            }
            writer.Dispose();
            Assert.AreEqual(0, flushControl.ActiveBytes());
            dir.Dispose();
        }

        [Test]
        public virtual void TestFlushDocCount()
        {
            int[] numThreads = new int[] { 2 + AtLeast(1), 1 };
            for (int i = 0; i < numThreads.Length; i++)
            {

                int numDocumentsToIndex = 50 + AtLeast(30);
                AtomicInteger numDocs = new AtomicInteger(numDocumentsToIndex);
                Directory dir = NewDirectory();
                MockDefaultFlushPolicy flushPolicy = new MockDefaultFlushPolicy();
                IndexWriterConfig iwc = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())).SetFlushPolicy(flushPolicy);

                int numDWPT = 1 + AtLeast(2);
                DocumentsWriterPerThreadPool threadPool = new ThreadAffinityDocumentsWriterThreadPool(numDWPT);
                iwc.SetIndexerThreadPool(threadPool);
                iwc.SetMaxBufferedDocs(2 + AtLeast(10));
                iwc.SetRAMBufferSizeMB(IndexWriterConfig.DISABLE_AUTO_FLUSH);
                iwc.SetMaxBufferedDeleteTerms(IndexWriterConfig.DISABLE_AUTO_FLUSH);
                IndexWriter writer = new IndexWriter(dir, iwc);
                flushPolicy = (MockDefaultFlushPolicy)writer.Config.FlushPolicy;
                Assert.IsTrue(flushPolicy.FlushOnDocCount());
                Assert.IsFalse(flushPolicy.FlushOnDeleteTerms());
                Assert.IsFalse(flushPolicy.FlushOnRAM());
                DocumentsWriter docsWriter = writer.DocsWriter;
                Assert.IsNotNull(docsWriter);
                DocumentsWriterFlushControl flushControl = docsWriter.FlushControl;
                Assert.AreEqual(0, flushControl.FlushBytes(), " bytes must be 0 after init");

                IndexThread[] threads = new IndexThread[numThreads[i]];
                for (int x = 0; x < threads.Length; x++)
                {
                    threads[x] = new IndexThread(this, numDocs, numThreads[i], writer, LineDocFile, false);
                    threads[x].Start();
                }

                for (int x = 0; x < threads.Length; x++)
                {
                    threads[x].Join();
                }

                Assert.AreEqual(0, flushControl.FlushBytes(), " all flushes must be due numThreads=" + numThreads[i]);
                Assert.AreEqual(numDocumentsToIndex, writer.NumDocs());
                Assert.AreEqual(numDocumentsToIndex, writer.MaxDoc);
                Assert.IsTrue(flushPolicy.PeakDocCountWithoutFlush <= iwc.MaxBufferedDocs, "peak bytes without flush exceeded watermark");
                AssertActiveBytesAfter(flushControl);
                writer.Dispose();
                Assert.AreEqual(0, flushControl.ActiveBytes());
                dir.Dispose();
            }
        }

        [Test]
        public virtual void TestRandom()
        {
            int numThreads = 1 + Random().Next(8);
            int numDocumentsToIndex = 50 + AtLeast(70);
            AtomicInteger numDocs = new AtomicInteger(numDocumentsToIndex);
            Directory dir = NewDirectory();
            IndexWriterConfig iwc = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random()));
            MockDefaultFlushPolicy flushPolicy = new MockDefaultFlushPolicy();
            iwc.SetFlushPolicy(flushPolicy);

            int numDWPT = 1 + Random().Next(8);
            DocumentsWriterPerThreadPool threadPool = new ThreadAffinityDocumentsWriterThreadPool(numDWPT);
            iwc.SetIndexerThreadPool(threadPool);

            IndexWriter writer = new IndexWriter(dir, iwc);
            flushPolicy = (MockDefaultFlushPolicy)writer.Config.FlushPolicy;
            DocumentsWriter docsWriter = writer.DocsWriter;
            Assert.IsNotNull(docsWriter);
            DocumentsWriterFlushControl flushControl = docsWriter.FlushControl;

            Assert.AreEqual(0, flushControl.FlushBytes(), " bytes must be 0 after init");

            IndexThread[] threads = new IndexThread[numThreads];
            for (int x = 0; x < threads.Length; x++)
            {
                threads[x] = new IndexThread(this, numDocs, numThreads, writer, LineDocFile, true);
                threads[x].Start();
            }

            for (int x = 0; x < threads.Length; x++)
            {
                threads[x].Join();
            }
            Assert.AreEqual(0, flushControl.FlushBytes(), " all flushes must be due");
            Assert.AreEqual(numDocumentsToIndex, writer.NumDocs());
            Assert.AreEqual(numDocumentsToIndex, writer.MaxDoc);
            if (flushPolicy.FlushOnRAM() && !flushPolicy.FlushOnDocCount() && !flushPolicy.FlushOnDeleteTerms())
            {
                long maxRAMBytes = (long)(iwc.RAMBufferSizeMB * 1024.0 * 1024.0);
                Assert.IsTrue(flushPolicy.PeakBytesWithoutFlush <= maxRAMBytes, "peak bytes without flush exceeded watermark");
                if (flushPolicy.HasMarkedPending)
                {
                    assertTrue("max: " + maxRAMBytes + " " + flushControl.PeakActiveBytes, maxRAMBytes <= flushControl.PeakActiveBytes);
                }
            }
            AssertActiveBytesAfter(flushControl);
            writer.Commit();
            Assert.AreEqual(0, flushControl.ActiveBytes());
            IndexReader r = DirectoryReader.Open(dir);
            Assert.AreEqual(numDocumentsToIndex, r.NumDocs);
            Assert.AreEqual(numDocumentsToIndex, r.MaxDoc);
            if (!flushPolicy.FlushOnRAM())
            {
                assertFalse("never stall if we don't flush on RAM", docsWriter.FlushControl.StallControl.WasStalled());
                assertFalse("never block if we don't flush on RAM", docsWriter.FlushControl.StallControl.HasBlocked());
            }
            r.Dispose();
            writer.Dispose();
            dir.Dispose();
        }

        [Test]
        public virtual void TestStallControl()
        {

            int[] numThreads = new int[] { 4 + Random().Next(8), 1 };
            int numDocumentsToIndex = 50 + Random().Next(50);
            for (int i = 0; i < numThreads.Length; i++)
            {
                AtomicInteger numDocs = new AtomicInteger(numDocumentsToIndex);
                MockDirectoryWrapper dir = NewMockDirectory();
                // mock a very slow harddisk sometimes here so that flushing is very slow
                dir.Throttling = MockDirectoryWrapper.Throttling_e.SOMETIMES;
                IndexWriterConfig iwc = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random()));
                iwc.SetMaxBufferedDocs(IndexWriterConfig.DISABLE_AUTO_FLUSH);
                iwc.SetMaxBufferedDeleteTerms(IndexWriterConfig.DISABLE_AUTO_FLUSH);
                FlushPolicy flushPolicy = new FlushByRamOrCountsPolicy();
                iwc.SetFlushPolicy(flushPolicy);

                DocumentsWriterPerThreadPool threadPool = new ThreadAffinityDocumentsWriterThreadPool(numThreads[i] == 1 ? 1 : 2);
                iwc.SetIndexerThreadPool(threadPool);
                // with such a small ram buffer we should be stalled quiet quickly
                iwc.SetRAMBufferSizeMB(0.25);
                IndexWriter writer = new IndexWriter(dir, iwc);
                IndexThread[] threads = new IndexThread[numThreads[i]];
                for (int x = 0; x < threads.Length; x++)
                {
                    threads[x] = new IndexThread(this, numDocs, numThreads[i], writer, LineDocFile, false);
                    threads[x].Start();
                }

                for (int x = 0; x < threads.Length; x++)
                {
                    threads[x].Join();
                }
                DocumentsWriter docsWriter = writer.DocsWriter;
                Assert.IsNotNull(docsWriter);
                DocumentsWriterFlushControl flushControl = docsWriter.FlushControl;
                Assert.AreEqual(0, flushControl.FlushBytes(), " all flushes must be due");
                Assert.AreEqual(numDocumentsToIndex, writer.NumDocs());
                Assert.AreEqual(numDocumentsToIndex, writer.MaxDoc);
                if (numThreads[i] == 1)
                {
                    assertFalse("single thread must not block numThreads: " + numThreads[i], docsWriter.FlushControl.StallControl.HasBlocked());
                }
                if (docsWriter.FlushControl.PeakNetBytes > (2d * iwc.RAMBufferSizeMB * 1024d * 1024d))
                {
                    Assert.IsTrue(docsWriter.FlushControl.StallControl.WasStalled());
                }
                AssertActiveBytesAfter(flushControl);
                writer.Dispose(true);
                dir.Dispose();
            }
        }

        protected internal virtual void AssertActiveBytesAfter(DocumentsWriterFlushControl flushControl)
        {
            IEnumerator<ThreadState> allActiveThreads = flushControl.AllActiveThreadStates();
            long bytesUsed = 0;
            while (allActiveThreads.MoveNext())
            {
                ThreadState next = allActiveThreads.Current;
                if (next.DocumentsWriterPerThread != null)
                {
                    bytesUsed += next.DocumentsWriterPerThread.BytesUsed();
                }
            }
            Assert.AreEqual(bytesUsed, flushControl.ActiveBytes());
        }

        public class IndexThread : ThreadClass
        {
            private readonly TestFlushByRamOrCountsPolicy OuterInstance;

            internal IndexWriter Writer;
            internal LiveIndexWriterConfig Iwc;
            internal LineFileDocs Docs;
            internal AtomicInteger PendingDocs;
            internal readonly bool DoRandomCommit;

            public IndexThread(TestFlushByRamOrCountsPolicy outerInstance, AtomicInteger pendingDocs, int numThreads, IndexWriter writer, LineFileDocs docs, bool doRandomCommit)
            {
                this.OuterInstance = outerInstance;
                this.PendingDocs = pendingDocs;
                this.Writer = writer;
                Iwc = writer.Config;
                this.Docs = docs;
                this.DoRandomCommit = doRandomCommit;
            }

            public override void Run()
            {
                try
                {
                    long ramSize = 0;
                    while (PendingDocs.DecrementAndGet() > -1)
                    {
                        Document doc = Docs.NextDoc();
                        Writer.AddDocument(doc);
                        long newRamSize = Writer.RamSizeInBytes();
                        if (newRamSize != ramSize)
                        {
                            ramSize = newRamSize;
                        }
                        if (DoRandomCommit)
                        {
                            if (Rarely())
                            {
                                Writer.Commit();
                            }
                        }
                    }
                    Writer.Commit();
                }
                catch (Exception ex)
                {
                    Console.WriteLine("FAILED exc:");
                    Console.WriteLine(ex.StackTrace);
                    throw new Exception(ex.Message, ex);
                }
            }
        }

        private class MockDefaultFlushPolicy : FlushByRamOrCountsPolicy
        {
            internal long PeakBytesWithoutFlush = int.MinValue;
            internal long PeakDocCountWithoutFlush = int.MinValue;
            internal bool HasMarkedPending = false;

            public override void OnDelete(DocumentsWriterFlushControl control, ThreadState state)
            {
                List<ThreadState> pending = new List<ThreadState>();
                List<ThreadState> notPending = new List<ThreadState>();
                FindPending(control, pending, notPending);
                bool flushCurrent = state.FlushPending;
                ThreadState toFlush;
                if (state.FlushPending)
                {
                    toFlush = state;
                }
                else if (FlushOnDeleteTerms() && state.DocumentsWriterPerThread.NumDeleteTerms() >= IWConfig.MaxBufferedDeleteTerms)
                {
                    toFlush = state;
                }
                else
                {
                    toFlush = null;
                }
                base.OnDelete(control, state);
                if (toFlush != null)
                {
                    if (flushCurrent)
                    {
                        Assert.IsTrue(pending.Remove(toFlush));
                    }
                    else
                    {
                        Assert.IsTrue(notPending.Remove(toFlush));
                    }
                    Assert.IsTrue(toFlush.FlushPending);
                    HasMarkedPending = true;
                }

                foreach (ThreadState threadState in notPending)
                {
                    Assert.IsFalse(threadState.FlushPending);
                }
            }

            public override void OnInsert(DocumentsWriterFlushControl control, ThreadState state)
            {
                List<ThreadState> pending = new List<ThreadState>();
                List<ThreadState> notPending = new List<ThreadState>();
                FindPending(control, pending, notPending);
                bool flushCurrent = state.FlushPending;
                long activeBytes = control.ActiveBytes();
                ThreadState toFlush;
                if (state.FlushPending)
                {
                    toFlush = state;
                }
                else if (FlushOnDocCount() && state.DocumentsWriterPerThread.NumDocsInRAM >= IWConfig.MaxBufferedDocs)
                {
                    toFlush = state;
                }
                else if (FlushOnRAM() && activeBytes >= (long)(IWConfig.RAMBufferSizeMB * 1024.0 * 1024.0))
                {
                    toFlush = FindLargestNonPendingWriter(control, state);
                    Assert.IsFalse(toFlush.FlushPending);
                }
                else
                {
                    toFlush = null;
                }
                base.OnInsert(control, state);
                if (toFlush != null)
                {
                    if (flushCurrent)
                    {
                        Assert.IsTrue(pending.Remove(toFlush));
                    }
                    else
                    {
                        Assert.IsTrue(notPending.Remove(toFlush));
                    }
                    Assert.IsTrue(toFlush.FlushPending);
                    HasMarkedPending = true;
                }
                else
                {
                    PeakBytesWithoutFlush = Math.Max(activeBytes, PeakBytesWithoutFlush);
                    PeakDocCountWithoutFlush = Math.Max(state.DocumentsWriterPerThread.NumDocsInRAM, PeakDocCountWithoutFlush);
                }

                foreach (ThreadState threadState in notPending)
                {
                    Assert.IsFalse(threadState.FlushPending);
                }
            }
        }

        internal static void FindPending(DocumentsWriterFlushControl flushControl, List<ThreadState> pending, List<ThreadState> notPending)
        {
            IEnumerator<ThreadState> allActiveThreads = flushControl.AllActiveThreadStates();
            while (allActiveThreads.MoveNext())
            {
                ThreadState next = allActiveThreads.Current;
                if (next.FlushPending)
                {
                    pending.Add(next);
                }
                else
                {
                    notPending.Add(next);
                }
            }
        }
    }

}