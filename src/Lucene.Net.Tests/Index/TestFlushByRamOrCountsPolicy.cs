using J2N.Threading;
using J2N.Threading.Atomic;
using Lucene.Net.Diagnostics;
using Lucene.Net.Index.Extensions;
using Lucene.Net.Store;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using JCG = J2N.Collections.Generic;
using Assert = Lucene.Net.TestFramework.Assert;
using Console = Lucene.Net.Util.SystemConsole;
using Lucene.Net.Util;

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

    using Directory = Lucene.Net.Store.Directory;
    using Document = Lucene.Net.Documents.Document;
    using LineFileDocs = Lucene.Net.Util.LineFileDocs;
    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
    using MockAnalyzer = Lucene.Net.Analysis.MockAnalyzer;
    using MockDirectoryWrapper = Lucene.Net.Store.MockDirectoryWrapper;
    using TestUtil = Lucene.Net.Util.TestUtil;
    using ThreadState = Lucene.Net.Index.DocumentsWriterPerThreadPool.ThreadState;


    // LUCENENET specific - Specify to unzip the line file docs
    [UseTempLineDocsFile]
    [Timeout(900_000)] // 15 minutes
    public class TestFlushByRamOrCountsPolicy : LuceneTestCase 
    {

        private static LineFileDocs lineDocFile;

        [OneTimeSetUp]
        public override void BeforeClass()
        {
            base.BeforeClass();
            lineDocFile = new LineFileDocs(Random, DefaultCodecSupportsDocValues);
        }

        [OneTimeTearDown]
        public override void AfterClass()
        {
            lineDocFile.Dispose();
            lineDocFile = null;
            base.AfterClass();
        }

        [Test]
        public virtual void TestFlushByRam()
        {
            // LUCENENET specific - disable the test if asserts are not enabled
            AssumeTrue("This test requires asserts to be enabled.", Debugging.AssertsEnabled);

            //double ramBuffer = (TestNightly ? 1 : 10) + AtLeast(2) + Random.NextDouble();
            // LUCENENET specific - increased size of ramBuffer to reduce the amount of
            // time required and offset AtLeast(2).
            double ramBuffer = (TestNightly ? 2 : 10) + AtLeast(2) + Random.NextDouble();
            RunFlushByRam(1 + Random.Next(TestNightly ? 5 : 1), ramBuffer, false);
        }

        [Test]
        public virtual void TestFlushByRamLargeBuffer()
        {
            // LUCENENET specific - disable the test if asserts are not enabled
            AssumeTrue("This test requires asserts to be enabled.", Debugging.AssertsEnabled);

            // with a 256 mb ram buffer we should never stall
            RunFlushByRam(1 + Random.Next(TestNightly ? 5 : 1), 256d, true);
        }

        protected internal virtual void RunFlushByRam(int numThreads, double maxRamMB, bool ensureNotStalled)
        {
            int numDocumentsToIndex = 10 + AtLeast(30);
            AtomicInt32 numDocs = new AtomicInt32(numDocumentsToIndex);
            Directory dir = NewDirectory();
            MockDefaultFlushPolicy flushPolicy = new MockDefaultFlushPolicy();
            MockAnalyzer analyzer = new MockAnalyzer(Random);
            analyzer.MaxTokenLength = TestUtil.NextInt32(Random, 1, IndexWriter.MAX_TERM_LENGTH);

            IndexWriterConfig iwc = NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer).SetFlushPolicy(flushPolicy);
            int numDWPT = 1 + AtLeast(2);
            DocumentsWriterPerThreadPool threadPool = new DocumentsWriterPerThreadPool(numDWPT);
            iwc.SetIndexerThreadPool(threadPool);
            iwc.SetRAMBufferSizeMB(maxRamMB);
            iwc.SetMaxBufferedDocs(IndexWriterConfig.DISABLE_AUTO_FLUSH);
            iwc.SetMaxBufferedDeleteTerms(IndexWriterConfig.DISABLE_AUTO_FLUSH);
            IndexWriter writer = new IndexWriter(dir, iwc);
            flushPolicy = (MockDefaultFlushPolicy)writer.Config.FlushPolicy;
            Assert.IsFalse(flushPolicy.FlushOnDocCount);
            Assert.IsFalse(flushPolicy.FlushOnDeleteTerms);
            Assert.IsTrue(flushPolicy.FlushOnRAM);
            DocumentsWriter docsWriter = writer.DocsWriter;
            Assert.IsNotNull(docsWriter);
            DocumentsWriterFlushControl flushControl = docsWriter.flushControl;
            Assert.AreEqual(0, flushControl.FlushBytes, " bytes must be 0 after init");

            IndexThread[] threads = new IndexThread[numThreads];
            for (int x = 0; x < threads.Length; x++)
            {
                threads[x] = new IndexThread(numDocs, writer, lineDocFile, false);
                threads[x].Start();
            }

            for (int x = 0; x < threads.Length; x++)
            {
                threads[x].Join();
            }
            long maxRAMBytes = (long)(iwc.RAMBufferSizeMB * 1024.0 * 1024.0);
            Assert.AreEqual(0, flushControl.FlushBytes, " all flushes must be due numThreads=" + numThreads);
            Assert.AreEqual(numDocumentsToIndex, writer.NumDocs);
            Assert.AreEqual(numDocumentsToIndex, writer.MaxDoc);
            Assert.IsTrue(flushPolicy.peakBytesWithoutFlush <= maxRAMBytes, "peak bytes without flush exceeded watermark");
            AssertActiveBytesAfter(flushControl);
            if (flushPolicy.hasMarkedPending)
            {
                Assert.IsTrue(maxRAMBytes < flushControl.peakActiveBytes);
            }
            if (ensureNotStalled)
            {
                Assert.IsFalse(docsWriter.flushControl.stallControl.WasStalled);
            }
            writer.Dispose();
            Assert.AreEqual(0, flushControl.ActiveBytes);
            dir.Dispose();
        }

        [Test]
        public virtual void TestFlushDocCount()
        {
            // LUCENENET specific - disable the test if asserts are not enabled
            AssumeTrue("This test requires asserts to be enabled.", Debugging.AssertsEnabled);

            int[] numThreads = new int[] { 2 + AtLeast(1), 1 };
            for (int i = 0; i < numThreads.Length; i++)
            {

                int numDocumentsToIndex = 50 + AtLeast(30);
                AtomicInt32 numDocs = new AtomicInt32(numDocumentsToIndex);
                Directory dir = NewDirectory();
                MockDefaultFlushPolicy flushPolicy = new MockDefaultFlushPolicy();
                IndexWriterConfig iwc = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)).SetFlushPolicy(flushPolicy);

                int numDWPT = 1 + AtLeast(2);
                DocumentsWriterPerThreadPool threadPool = new DocumentsWriterPerThreadPool(numDWPT);
                iwc.SetIndexerThreadPool(threadPool);
                iwc.SetMaxBufferedDocs(2 + AtLeast(10));
                iwc.SetRAMBufferSizeMB(IndexWriterConfig.DISABLE_AUTO_FLUSH);
                iwc.SetMaxBufferedDeleteTerms(IndexWriterConfig.DISABLE_AUTO_FLUSH);
                IndexWriter writer = new IndexWriter(dir, iwc);
                flushPolicy = (MockDefaultFlushPolicy)writer.Config.FlushPolicy;
                Assert.IsTrue(flushPolicy.FlushOnDocCount);
                Assert.IsFalse(flushPolicy.FlushOnDeleteTerms);
                Assert.IsFalse(flushPolicy.FlushOnRAM);
                DocumentsWriter docsWriter = writer.DocsWriter;
                Assert.IsNotNull(docsWriter);
                DocumentsWriterFlushControl flushControl = docsWriter.flushControl;
                Assert.AreEqual(0, flushControl.FlushBytes, " bytes must be 0 after init");

                IndexThread[] threads = new IndexThread[numThreads[i]];
                for (int x = 0; x < threads.Length; x++)
                {
                    threads[x] = new IndexThread(numDocs, writer, lineDocFile, false);
                    threads[x].Start();
                }

                for (int x = 0; x < threads.Length; x++)
                {
                    threads[x].Join();
                }

                Assert.AreEqual(0, flushControl.FlushBytes, " all flushes must be due numThreads=" + numThreads[i]);
                Assert.AreEqual(numDocumentsToIndex, writer.NumDocs);
                Assert.AreEqual(numDocumentsToIndex, writer.MaxDoc);
                Assert.IsTrue(flushPolicy.peakDocCountWithoutFlush <= iwc.MaxBufferedDocs, "peak bytes without flush exceeded watermark");
                AssertActiveBytesAfter(flushControl);
                writer.Dispose();
                Assert.AreEqual(0, flushControl.ActiveBytes);
                dir.Dispose();
            }
        }

        [Test]
        public virtual void TestRandom()
        {
            // LUCENENET specific - disable the test if asserts are not enabled
            AssumeTrue("This test requires asserts to be enabled.", Debugging.AssertsEnabled);

            int numThreads = 1 + Random.Next(8);
            int numDocumentsToIndex = 50 + AtLeast(70);
            AtomicInt32 numDocs = new AtomicInt32(numDocumentsToIndex);
            Directory dir = NewDirectory();
            IndexWriterConfig iwc = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random));
            MockDefaultFlushPolicy flushPolicy = new MockDefaultFlushPolicy();
            iwc.SetFlushPolicy(flushPolicy);

            int numDWPT = 1 + Random.Next(8);
            DocumentsWriterPerThreadPool threadPool = new DocumentsWriterPerThreadPool(numDWPT);
            iwc.SetIndexerThreadPool(threadPool);

            IndexWriter writer = new IndexWriter(dir, iwc);
            flushPolicy = (MockDefaultFlushPolicy)writer.Config.FlushPolicy;
            DocumentsWriter docsWriter = writer.DocsWriter;
            Assert.IsNotNull(docsWriter);
            DocumentsWriterFlushControl flushControl = docsWriter.flushControl;

            Assert.AreEqual(0, flushControl.FlushBytes, " bytes must be 0 after init");

            IndexThread[] threads = new IndexThread[numThreads];
            for (int x = 0; x < threads.Length; x++)
            {
                threads[x] = new IndexThread(numDocs, writer, lineDocFile, true);
                threads[x].Start();
            }

            for (int x = 0; x < threads.Length; x++)
            {
                threads[x].Join();
            }
            Assert.AreEqual(0, flushControl.FlushBytes, " all flushes must be due");
            Assert.AreEqual(numDocumentsToIndex, writer.NumDocs);
            Assert.AreEqual(numDocumentsToIndex, writer.MaxDoc);
            if (flushPolicy.FlushOnRAM && !flushPolicy.FlushOnDocCount && !flushPolicy.FlushOnDeleteTerms)
            {
                long maxRAMBytes = (long)(iwc.RAMBufferSizeMB * 1024.0 * 1024.0);
                Assert.IsTrue(flushPolicy.peakBytesWithoutFlush <= maxRAMBytes, "peak bytes without flush exceeded watermark");
                if (flushPolicy.hasMarkedPending)
                {
                    assertTrue("max: " + maxRAMBytes + " " + flushControl.peakActiveBytes, maxRAMBytes <= flushControl.peakActiveBytes);
                }
            }
            AssertActiveBytesAfter(flushControl);
            writer.Commit();
            Assert.AreEqual(0, flushControl.ActiveBytes);
            IndexReader r = DirectoryReader.Open(dir);
            Assert.AreEqual(numDocumentsToIndex, r.NumDocs);
            Assert.AreEqual(numDocumentsToIndex, r.MaxDoc);
            if (!flushPolicy.FlushOnRAM)
            {
                assertFalse("never stall if we don't flush on RAM", docsWriter.flushControl.stallControl.WasStalled);
                assertFalse("never block if we don't flush on RAM", docsWriter.flushControl.stallControl.HasBlocked);
            }
            r.Dispose();
            writer.Dispose();
            dir.Dispose();
        }

        [Test]
        [Slow] // LUCENENET: occasionally
        public virtual void TestStallControl()
        {
            // LUCENENET specific - disable the test if asserts are not enabled
            AssumeTrue("This test requires asserts to be enabled.", Debugging.AssertsEnabled);

            int[] numThreads = new int[] { 4 + Random.Next(8), 1 };
            int numDocumentsToIndex = 50 + Random.Next(50);
            for (int i = 0; i < numThreads.Length; i++)
            {
                AtomicInt32 numDocs = new AtomicInt32(numDocumentsToIndex);
                MockDirectoryWrapper dir = NewMockDirectory();
                // mock a very slow harddisk sometimes here so that flushing is very slow
                dir.Throttling = Throttling.SOMETIMES;
                IndexWriterConfig iwc = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random));
                iwc.SetMaxBufferedDocs(IndexWriterConfig.DISABLE_AUTO_FLUSH);
                iwc.SetMaxBufferedDeleteTerms(IndexWriterConfig.DISABLE_AUTO_FLUSH);
                FlushPolicy flushPolicy = new FlushByRamOrCountsPolicy();
                iwc.SetFlushPolicy(flushPolicy);

                DocumentsWriterPerThreadPool threadPool = new DocumentsWriterPerThreadPool(numThreads[i] == 1 ? 1 : 2);
                iwc.SetIndexerThreadPool(threadPool);
                // with such a small ram buffer we should be stalled quiet quickly
                iwc.SetRAMBufferSizeMB(0.25);
                IndexWriter writer = new IndexWriter(dir, iwc);
                IndexThread[] threads = new IndexThread[numThreads[i]];
                for (int x = 0; x < threads.Length; x++)
                {
                    threads[x] = new IndexThread(numDocs, writer, lineDocFile, false);
                    threads[x].Start();
                }

                for (int x = 0; x < threads.Length; x++)
                {
                    threads[x].Join();
                }
                DocumentsWriter docsWriter = writer.DocsWriter;
                Assert.IsNotNull(docsWriter);
                DocumentsWriterFlushControl flushControl = docsWriter.flushControl;
                Assert.AreEqual(0, flushControl.FlushBytes, " all flushes must be due");
                Assert.AreEqual(numDocumentsToIndex, writer.NumDocs);
                Assert.AreEqual(numDocumentsToIndex, writer.MaxDoc);
                if (numThreads[i] == 1)
                {
                    assertFalse("single thread must not block numThreads: " + numThreads[i], docsWriter.flushControl.stallControl.HasBlocked);
                }
                if (docsWriter.flushControl.peakNetBytes > (2d * iwc.RAMBufferSizeMB * 1024d * 1024d))
                {
                    Assert.IsTrue(docsWriter.flushControl.stallControl.WasStalled);
                }
                AssertActiveBytesAfter(flushControl);
                writer.Dispose(true);
                dir.Dispose();
            }
        }

        internal virtual void AssertActiveBytesAfter(DocumentsWriterFlushControl flushControl)
        {
            IEnumerator<ThreadState> allActiveThreads = flushControl.AllActiveThreadStates();
            long bytesUsed = 0;
            while (allActiveThreads.MoveNext())
            {
                ThreadState next = allActiveThreads.Current;
                if (next.dwpt != null)
                {
                    bytesUsed += next.dwpt.BytesUsed;
                }
            }
            Assert.AreEqual(bytesUsed, flushControl.ActiveBytes);
        }

        public class IndexThread : ThreadJob
        {
            internal IndexWriter writer;
            internal LiveIndexWriterConfig iwc;
            internal LineFileDocs docs;
            internal AtomicInt32 pendingDocs;
            internal readonly bool doRandomCommit;

            public IndexThread(AtomicInt32 pendingDocs, IndexWriter writer, LineFileDocs docs, bool doRandomCommit)
            {
                this.pendingDocs = pendingDocs;
                this.writer = writer;
                iwc = writer.Config;
                this.docs = docs;
                this.doRandomCommit = doRandomCommit;
            }

            public override void Run()
            {
                try
                {
                    long ramSize = 0;
                    while (pendingDocs.DecrementAndGet() > -1)
                    {
                        Document doc = docs.NextDoc();
                        writer.AddDocument(doc);
                        long newRamSize = writer.RamSizeInBytes();
                        if (newRamSize != ramSize)
                        {
                            ramSize = newRamSize;
                        }
                        if (doRandomCommit)
                        {
                            if (Rarely())
                            {
                                writer.Commit();
                            }
                        }
                    }
                    writer.Commit();
                }
                catch (Exception ex) when (ex.IsThrowable())
                {
                    Console.WriteLine("FAILED exc:");
                    ex.printStackTrace(Console.Out);
                    throw RuntimeException.Create(ex);
                }
            }
        }

        private class MockDefaultFlushPolicy : FlushByRamOrCountsPolicy
        {
            internal long peakBytesWithoutFlush = int.MinValue;
            internal long peakDocCountWithoutFlush = int.MinValue;
            internal bool hasMarkedPending = false;

            public override void OnDelete(DocumentsWriterFlushControl control, ThreadState state)
            {
                IList<ThreadState> pending = new JCG.List<ThreadState>();
                IList<ThreadState> notPending = new JCG.List<ThreadState>();
                FindPending(control, pending, notPending);
                bool flushCurrent = state.IsFlushPending;
                ThreadState toFlush;
                if (state.IsFlushPending)
                {
                    toFlush = state;
                }
                else if (FlushOnDeleteTerms && state.DocumentsWriterPerThread.NumDeleteTerms >= m_indexWriterConfig.MaxBufferedDeleteTerms)
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
                    Assert.IsTrue(toFlush.IsFlushPending);
                    hasMarkedPending = true;
                }

                foreach (ThreadState threadState in notPending)
                {
                    Assert.IsFalse(threadState.IsFlushPending);
                }
            }

            public override void OnInsert(DocumentsWriterFlushControl control, ThreadState state)
            {
                IList<ThreadState> pending = new JCG.List<ThreadState>();
                IList<ThreadState> notPending = new JCG.List<ThreadState>();
                FindPending(control, pending, notPending);
                bool flushCurrent = state.IsFlushPending;
                long activeBytes = control.ActiveBytes;
                ThreadState toFlush;
                if (state.IsFlushPending)
                {
                    toFlush = state;
                }
                else if (FlushOnDocCount && state.DocumentsWriterPerThread.NumDocsInRAM >= m_indexWriterConfig.MaxBufferedDocs)
                {
                    toFlush = state;
                }
                else if (FlushOnRAM && activeBytes >= (long)(m_indexWriterConfig.RAMBufferSizeMB * 1024.0 * 1024.0))
                {
                    toFlush = FindLargestNonPendingWriter(control, state);
                    Assert.IsFalse(toFlush.IsFlushPending);
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
                    Assert.IsTrue(toFlush.IsFlushPending);
                    hasMarkedPending = true;
                }
                else
                {
                    peakBytesWithoutFlush = Math.Max(activeBytes, peakBytesWithoutFlush);
                    peakDocCountWithoutFlush = Math.Max(state.DocumentsWriterPerThread.NumDocsInRAM, peakDocCountWithoutFlush);
                }

                foreach (ThreadState threadState in notPending)
                {
                    Assert.IsFalse(threadState.IsFlushPending);
                }
            }
        }

        internal static void FindPending(DocumentsWriterFlushControl flushControl, IList<ThreadState> pending, IList<ThreadState> notPending)
        {
            IEnumerator<ThreadState> allActiveThreads = flushControl.AllActiveThreadStates();
            while (allActiveThreads.MoveNext())
            {
                ThreadState next = allActiveThreads.Current;
                if (next.IsFlushPending)
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