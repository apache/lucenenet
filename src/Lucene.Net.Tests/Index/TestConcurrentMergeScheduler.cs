using J2N.Threading.Atomic;
using Lucene.Net.Attributes;
using Lucene.Net.Documents;
using Lucene.Net.Index.Extensions;
using Lucene.Net.Store;
using Lucene.Net.Support.Threading;
using Lucene.Net.Util;
using NUnit.Framework;
using RandomizedTesting.Generators;
using System;
using System.IO;
using System.Threading;
using Assert = Lucene.Net.TestFramework.Assert;
using Console = Lucene.Net.Util.SystemConsole;

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
    using Document = Documents.Document;
    using Field = Field;
    using Lucene41PostingsFormat = Lucene.Net.Codecs.Lucene41.Lucene41PostingsFormat;
    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
    using MockAnalyzer = Lucene.Net.Analysis.MockAnalyzer;
    using MockDirectoryWrapper = Lucene.Net.Store.MockDirectoryWrapper;
    using StringField = StringField;
    using TestUtil = Lucene.Net.Util.TestUtil;
    using TextField = TextField;

    [TestFixture]
    public class TestConcurrentMergeScheduler : LuceneTestCase
    {
        private class FailOnlyOnFlush : Failure
        {
            private readonly TestConcurrentMergeScheduler outerInstance;

            public FailOnlyOnFlush(TestConcurrentMergeScheduler outerInstance)
            {
                this.outerInstance = outerInstance;
            }

            internal bool doFail;
            internal bool hitExc;

            public override void SetDoFail()
            {
                this.doFail = true;
                hitExc = false;
            }

            public override void ClearDoFail()
            {
                this.doFail = false;
            }

            public override void Eval(MockDirectoryWrapper dir)
            {
                if (doFail && IsTestThread)
                {
                    // LUCENENET specific: for these to work in release mode, we have added [MethodImpl(MethodImplOptions.NoInlining)]
                    // to each possible target of the StackTraceHelper. If these change, so must the attribute on the target methods.
                    bool isDoFlush = Util.StackTraceHelper.DoesStackTraceContainMethod("Flush");
                    bool isClose = Util.StackTraceHelper.DoesStackTraceContainMethod("Close") ||
                        Util.StackTraceHelper.DoesStackTraceContainMethod("Dispose");

                    if (isDoFlush && !isClose && Random.NextBoolean())
                    {
                        hitExc = true;
                        throw new IOException(Thread.CurrentThread.Name + ": now failing during flush");
                    }
                }
            }
        }

        // Make sure running BG merges still work fine even when
        // we are hitting exceptions during flushing.
        [Test]
        public virtual void TestFlushExceptions()
        {
            MockDirectoryWrapper directory = NewMockDirectory();
            FailOnlyOnFlush failure = new FailOnlyOnFlush(this);
            directory.FailOn(failure);

            IndexWriter writer = new IndexWriter(directory, (IndexWriterConfig)NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)).SetMaxBufferedDocs(2));
            Document doc = new Document();
            Field idField = NewStringField("id", "", Field.Store.YES);
            doc.Add(idField);
            int extraCount = 0;

            for (int i = 0; i < 10; i++)
            {
                if (Verbose)
                {
                    Console.WriteLine("TEST: iter=" + i);
                }

                for (int j = 0; j < 20; j++)
                {
                    idField.SetStringValue(Convert.ToString(i * 20 + j));
                    writer.AddDocument(doc);
                }

                // must cycle here because sometimes the merge flushes
                // the doc we just added and so there's nothing to
                // flush, and we don't hit the exception
                while (true)
                {
                    writer.AddDocument(doc);
                    failure.SetDoFail();
                    try
                    {
                        writer.Flush(true, true);
                        if (failure.hitExc)
                        {
                            Assert.Fail("failed to hit IOException");
                        }
                        extraCount++;
                    }
                    catch (Exception ioe) when (ioe.IsIOException())
                    {
                        if (Verbose)
                        {
                            Console.WriteLine(ioe.StackTrace);
                        }
                        failure.ClearDoFail();
                        break;
                    }
                }
                Assert.AreEqual(20 * (i + 1) + extraCount, writer.NumDocs);
            }

            writer.Dispose();
            IndexReader reader = DirectoryReader.Open(directory);
            Assert.AreEqual(200 + extraCount, reader.NumDocs);
            reader.Dispose();
            directory.Dispose();
        }

        // Test that deletes committed after a merge started and
        // before it finishes, are correctly merged back:
        [Test]
        public virtual void TestDeleteMerging()
        {
            Directory directory = NewDirectory();

            LogDocMergePolicy mp = new LogDocMergePolicy();
            // Force degenerate merging so we can get a mix of
            // merging of segments with and without deletes at the
            // start:
            mp.MinMergeDocs = 1000;
            IndexWriter writer = new IndexWriter(directory, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)).SetMergePolicy(mp));

            Document doc = new Document();
            Field idField = NewStringField("id", "", Field.Store.YES);
            doc.Add(idField);
            for (int i = 0; i < 10; i++)
            {
                if (Verbose)
                {
                    Console.WriteLine("\nTEST: cycle");
                }
                for (int j = 0; j < 100; j++)
                {
                    idField.SetStringValue(Convert.ToString(i * 100 + j));
                    writer.AddDocument(doc);
                }

                int delID = i;
                while (delID < 100 * (1 + i))
                {
                    if (Verbose)
                    {
                        Console.WriteLine("TEST: del " + delID);
                    }
                    writer.DeleteDocuments(new Term("id", "" + delID));
                    delID += 10;
                }

                writer.Commit();
            }

            writer.Dispose();
            IndexReader reader = DirectoryReader.Open(directory);
            // Verify that we did not lose any deletes...
            Assert.AreEqual(450, reader.NumDocs);
            reader.Dispose();
            directory.Dispose();
        }

        [Test]
        public virtual void TestNoExtraFiles()
        {
            Directory directory = NewDirectory();
            IndexWriter writer = new IndexWriter(directory, (IndexWriterConfig)NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)).SetMaxBufferedDocs(2));

            for (int iter = 0; iter < 7; iter++)
            {
                if (Verbose)
                {
                    Console.WriteLine("TEST: iter=" + iter);
                }

                for (int j = 0; j < 21; j++)
                {
                    Document doc = new Document();
                    doc.Add(NewTextField("content", "a b c", Field.Store.NO));
                    writer.AddDocument(doc);
                }

                writer.Dispose();
                TestIndexWriter.AssertNoUnreferencedFiles(directory, "testNoExtraFiles");

                // Reopen
                writer = new IndexWriter(directory, (IndexWriterConfig)NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)).SetOpenMode(OpenMode.APPEND).SetMaxBufferedDocs(2));
            }

            writer.Dispose();

            directory.Dispose();
        }

        [Test]
        public virtual void TestNoWaitClose()
        {
            Directory directory = NewDirectory();
            Document doc = new Document();
            Field idField = NewStringField("id", "", Field.Store.YES);
            doc.Add(idField);

            IndexWriter writer = new IndexWriter(directory, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)).SetMaxBufferedDocs(2).SetMergePolicy(NewLogMergePolicy(100)));

            for (int iter = 0; iter < 10; iter++)
            {
                for (int j = 0; j < 201; j++)
                {
                    idField.SetStringValue(Convert.ToString(iter * 201 + j));
                    writer.AddDocument(doc);
                }

                int delID = iter * 201;
                for (int j = 0; j < 20; j++)
                {
                    writer.DeleteDocuments(new Term("id", Convert.ToString(delID)));
                    delID += 5;
                }

                // Force a bunch of merge threads to kick off so we
                // stress out aborting them on close:
                ((LogMergePolicy)writer.Config.MergePolicy).MergeFactor = 3;
                writer.AddDocument(doc);
                writer.Commit();

                writer.Dispose(false);

                IndexReader reader = DirectoryReader.Open(directory);
                Assert.AreEqual((1 + iter) * 182, reader.NumDocs);
                reader.Dispose();

                // Reopen
                writer = new IndexWriter(directory, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)).SetOpenMode(OpenMode.APPEND).SetMergePolicy(NewLogMergePolicy(100)));
            }
            writer.Dispose();

            directory.Dispose();
        }

        // LUCENE-4544
        [Test]
        public virtual void TestMaxMergeCount()
        {
            Directory dir = NewDirectory();
            IndexWriterConfig iwc = new IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random));

            int maxMergeCount = TestUtil.NextInt32(Random, 1, 5);
            int maxMergeThreads = TestUtil.NextInt32(Random, 1, maxMergeCount);
            CountdownEvent enoughMergesWaiting = new CountdownEvent(maxMergeCount);
            AtomicInt32 runningMergeCount = new AtomicInt32(0);
            AtomicBoolean failed = new AtomicBoolean();

            if (Verbose)
            {
                Console.WriteLine("TEST: maxMergeCount=" + maxMergeCount + " maxMergeThreads=" + maxMergeThreads);
            }

            ConcurrentMergeScheduler cms = new ConcurrentMergeSchedulerAnonymousClass(this, maxMergeCount, enoughMergesWaiting, runningMergeCount, failed);
            cms.SetMaxMergesAndThreads(maxMergeCount, maxMergeThreads);
            iwc.SetMergeScheduler(cms);
            iwc.SetMaxBufferedDocs(2);

            TieredMergePolicy tmp = new TieredMergePolicy();
            iwc.SetMergePolicy(tmp);
            tmp.MaxMergeAtOnce = 2;
            tmp.SegmentsPerTier = 2;

            IndexWriter w = new IndexWriter(dir, iwc);
            Document doc = new Document();
            doc.Add(NewField("field", "field", TextField.TYPE_NOT_STORED));
            while (enoughMergesWaiting.CurrentCount != 0 && !failed)
            {
                for (int i = 0; i < 10; i++)
                {
                    w.AddDocument(doc);
                }
            }
            w.Dispose(false);
            dir.Dispose();
        }

        private sealed class ConcurrentMergeSchedulerAnonymousClass : ConcurrentMergeScheduler
        {
            private readonly TestConcurrentMergeScheduler outerInstance;

            private readonly int maxMergeCount;
            private readonly CountdownEvent enoughMergesWaiting;
            private readonly AtomicInt32 runningMergeCount;
            private readonly AtomicBoolean failed;

            public ConcurrentMergeSchedulerAnonymousClass(TestConcurrentMergeScheduler outerInstance, int maxMergeCount, CountdownEvent enoughMergesWaiting, AtomicInt32 runningMergeCount, AtomicBoolean failed)
            {
                this.outerInstance = outerInstance;
                this.maxMergeCount = maxMergeCount;
                this.enoughMergesWaiting = enoughMergesWaiting;
                this.runningMergeCount = runningMergeCount;
                this.failed = failed;
            }

            protected override void DoMerge(MergePolicy.OneMerge merge)
            {
                try
                {
                    // Stall all incoming merges until we see
                    // maxMergeCount:
                    int count = runningMergeCount.IncrementAndGet();
                    try
                    {
                        Assert.IsTrue(count <= maxMergeCount, "count=" + count + " vs maxMergeCount=" + maxMergeCount);
                        enoughMergesWaiting.Signal();

                        // Stall this merge until we see exactly
                        // maxMergeCount merges waiting
                        while (true)
                        {
                            // wait for 10 milliseconds
                            if (enoughMergesWaiting.Wait(new TimeSpan(0, 0, 0, 0, 10)) || failed)
                            {
                                break;
                            }
                        }
                        // Then sleep a bit to give a chance for the bug
                        // (too many pending merges) to appear:
                        Thread.Sleep(20);
                        base.DoMerge(merge);
                    }
                    finally
                    {
                        runningMergeCount.DecrementAndGet();
                    }
                }
                catch (Exception t) when (t.IsThrowable())
                {
                    failed.Value = (true);
                    m_writer.MergeFinish(merge);
                    // LUCENENET NOTE: ThreadJob takes care of propagating the exception to the calling thread
                    throw RuntimeException.Create(t);
                }
            }
        }

        private class TrackingCMS : ConcurrentMergeScheduler
        {
            internal long totMergedBytes;

            public TrackingCMS()
            {
                SetMaxMergesAndThreads(5, 5);
            }

            protected override void DoMerge(MergePolicy.OneMerge merge)
            {
                totMergedBytes += merge.TotalBytesSize;
                base.DoMerge(merge);
            }
        }

        [Test]
        public virtual void TestTotalBytesSize()
        {
            Directory d = NewDirectory();
            if (d is MockDirectoryWrapper)
            {
                ((MockDirectoryWrapper)d).Throttling = Throttling.NEVER;
            }
            IndexWriterConfig iwc = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random));
            iwc.SetMaxBufferedDocs(5);
            iwc.SetMergeScheduler(new TrackingCMS());
            if (TestUtil.GetPostingsFormat("id").Equals("SimpleText", StringComparison.Ordinal))
            {
                // no
                iwc.SetCodec(TestUtil.AlwaysPostingsFormat(new Lucene41PostingsFormat()));
            }
            RandomIndexWriter w = new RandomIndexWriter(Random, d, iwc);
            for (int i = 0; i < 1000; i++)
            {
                Document doc = new Document();
                doc.Add(new StringField("id", "" + i, Field.Store.NO));
                w.AddDocument(doc);

                if (Random.NextBoolean())
                {
                    w.DeleteDocuments(new Term("id", "" + Random.Next(i + 1)));
                }
            }
            Assert.IsTrue(((TrackingCMS)w.IndexWriter.Config.MergeScheduler).totMergedBytes != 0);
            w.Dispose();
            d.Dispose();
        }


        // LUCENENET specific
        private class FailOnlyOnMerge : Failure
        {
            public override void Eval(MockDirectoryWrapper dir)
            {
                // LUCENENET specific: for these to work in release mode, we have added [MethodImpl(MethodImplOptions.NoInlining)]
                // to each possible target of the StackTraceHelper. If these change, so must the attribute on the target methods.
                if (StackTraceHelper.DoesStackTraceContainMethod("DoMerge"))
                {
                    throw new IOException("now failing during merge");
                }
            }
        }

        // LUCENENET-603
        [Test, LuceneNetSpecific]
        public void TestExceptionOnBackgroundThreadIsPropagatedToCallingThread()
        {
            using MockDirectoryWrapper dir = NewMockDirectory();
            dir.FailOn(new FailOnlyOnMerge());

            Document doc = new Document();
            Field idField = NewStringField("id", "", Field.Store.YES);
            doc.Add(idField);

            var mergeScheduler = new ConcurrentMergeScheduler();
            using IndexWriter writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)).SetMergeScheduler(mergeScheduler).SetMaxBufferedDocs(2).SetRAMBufferSizeMB(IndexWriterConfig.DISABLE_AUTO_FLUSH).SetMergePolicy(NewLogMergePolicy()));
            LogMergePolicy logMP = (LogMergePolicy)writer.Config.MergePolicy;
            logMP.MergeFactor = 10;
            for (int i = 0; i < 20; i++)
            {
                writer.AddDocument(doc);
            }

            bool exceptionHit = false;
            try
            {
                mergeScheduler.Sync();
            }
            catch (MergePolicy.MergeException)
            {
                exceptionHit = true;
            }

            assertTrue(exceptionHit);
        }
    }
}