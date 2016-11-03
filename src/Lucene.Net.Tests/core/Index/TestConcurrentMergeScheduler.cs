using System;
using System.Diagnostics;
using System.Threading;
using Lucene.Net.Documents;

namespace Lucene.Net.Index
{
    using Lucene.Net.Randomized.Generators;
    using Lucene.Net.Support;
    using NUnit.Framework;
    using System.IO;
    using Directory = Lucene.Net.Store.Directory;
    using Document = Documents.Document;
    using Field = Field;
    using Lucene41PostingsFormat = Lucene.Net.Codecs.Lucene41.Lucene41PostingsFormat;
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
    using MockDirectoryWrapper = Lucene.Net.Store.MockDirectoryWrapper;
    using OpenMode_e = Lucene.Net.Index.IndexWriterConfig.OpenMode_e;
    using StringField = StringField;
    using TestUtil = Lucene.Net.Util.TestUtil;
    using TextField = TextField;

    [TestFixture]
    public class TestConcurrentMergeScheduler : LuceneTestCase
    {
        private class FailOnlyOnFlush : MockDirectoryWrapper.Failure
        {
            private readonly TestConcurrentMergeScheduler OuterInstance;

            public FailOnlyOnFlush(TestConcurrentMergeScheduler outerInstance)
            {
                this.OuterInstance = outerInstance;
            }

            internal bool DoFail;
            internal bool HitExc;

            public override void SetDoFail()
            {
                this.DoFail = true;
                HitExc = false;
            }

            public override void ClearDoFail()
            {
                this.DoFail = false;
            }

            public override void Eval(MockDirectoryWrapper dir)
            {
                if (DoFail && TestThread() && Random().NextBoolean())
                {
                    bool isDoFlush = Util.StackTraceHelper.DoesStackTraceContainMethod("Flush");
                    bool isClose = Util.StackTraceHelper.DoesStackTraceContainMethod("Close");    

                    if (isDoFlush && !isClose )
                    {
                        HitExc = true;
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

            IndexWriter writer = new IndexWriter(directory, (IndexWriterConfig)NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())).SetMaxBufferedDocs(2));
            Document doc = new Document();
            Field idField = NewStringField("id", "", Field.Store.YES);
            doc.Add(idField);
            int extraCount = 0;

            for (int i = 0; i < 10; i++)
            {
                if (VERBOSE)
                {
                    Console.WriteLine("TEST: iter=" + i);
                }

                for (int j = 0; j < 20; j++)
                {
                    idField.StringValue = Convert.ToString(i * 20 + j);
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
                        if (failure.HitExc)
                        {
                            Assert.Fail("failed to hit IOException");
                        }
                        extraCount++;
                    }
                    catch (IOException ioe)
                    {
                        if (VERBOSE)
                        {
                            Console.WriteLine(ioe.StackTrace);
                        }
                        failure.ClearDoFail();
                        break;
                    }
                }
                Assert.AreEqual(20 * (i + 1) + extraCount, writer.NumDocs());
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
            IndexWriter writer = new IndexWriter(directory, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())).SetMergePolicy(mp));

            Document doc = new Document();
            Field idField = NewStringField("id", "", Field.Store.YES);
            doc.Add(idField);
            for (int i = 0; i < 10; i++)
            {
                if (VERBOSE)
                {
                    Console.WriteLine("\nTEST: cycle");
                }
                for (int j = 0; j < 100; j++)
                {
                    idField.StringValue = Convert.ToString(i * 100 + j);
                    writer.AddDocument(doc);
                }

                int delID = i;
                while (delID < 100 * (1 + i))
                {
                    if (VERBOSE)
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

        [Test, MaxTime(300000)]
        public virtual void TestNoExtraFiles()
        {
            Directory directory = NewDirectory();
            IndexWriter writer = new IndexWriter(directory, (IndexWriterConfig)NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())).SetMaxBufferedDocs(2));

            for (int iter = 0; iter < 7; iter++)
            {
                if (VERBOSE)
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
                writer = new IndexWriter(directory, (IndexWriterConfig)NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())).SetOpenMode(OpenMode_e.APPEND).SetMaxBufferedDocs(2));
            }

            writer.Dispose();

            directory.Dispose();
        }

        [Test, MaxTime(300000)]
        public virtual void TestNoWaitClose()
        {
            Directory directory = NewDirectory();
            Document doc = new Document();
            Field idField = NewStringField("id", "", Field.Store.YES);
            doc.Add(idField);

            IndexWriter writer = new IndexWriter(directory, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())).SetMaxBufferedDocs(2).SetMergePolicy(NewLogMergePolicy(100)));

            for (int iter = 0; iter < 10; iter++)
            {
                for (int j = 0; j < 201; j++)
                {
                    idField.StringValue = Convert.ToString(iter * 201 + j);
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
                writer = new IndexWriter(directory, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())).SetOpenMode(OpenMode_e.APPEND).SetMergePolicy(NewLogMergePolicy(100)));
            }
            writer.Dispose();

            directory.Dispose();
        }

        // LUCENE-4544
        [Test]
        public virtual void TestMaxMergeCount()
        {
            Directory dir = NewDirectory();
            IndexWriterConfig iwc = new IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random()));

            int maxMergeCount = TestUtil.NextInt(Random(), 1, 5);
            int maxMergeThreads = TestUtil.NextInt(Random(), 1, maxMergeCount);
            CountdownEvent enoughMergesWaiting = new CountdownEvent(maxMergeCount);
            AtomicInteger runningMergeCount = new AtomicInteger(0);
            AtomicBoolean failed = new AtomicBoolean();

            if (VERBOSE)
            {
                Console.WriteLine("TEST: maxMergeCount=" + maxMergeCount + " maxMergeThreads=" + maxMergeThreads);
            }

            ConcurrentMergeScheduler cms = new ConcurrentMergeSchedulerAnonymousInnerClassHelper(this, maxMergeCount, enoughMergesWaiting, runningMergeCount, failed);
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
            while (enoughMergesWaiting.CurrentCount != 0 && !failed.Get())
            {
                for (int i = 0; i < 10; i++)
                {
                    w.AddDocument(doc);
                }
            }
            w.Dispose(false);
            dir.Dispose();
        }

        private class ConcurrentMergeSchedulerAnonymousInnerClassHelper : ConcurrentMergeScheduler
        {
            private readonly TestConcurrentMergeScheduler OuterInstance;

            private int MaxMergeCount;
            private CountdownEvent EnoughMergesWaiting;
            private AtomicInteger RunningMergeCount;
            private AtomicBoolean Failed;

            public ConcurrentMergeSchedulerAnonymousInnerClassHelper(TestConcurrentMergeScheduler outerInstance, int maxMergeCount, CountdownEvent enoughMergesWaiting, AtomicInteger runningMergeCount, AtomicBoolean failed)
            {
                this.OuterInstance = outerInstance;
                this.MaxMergeCount = maxMergeCount;
                this.EnoughMergesWaiting = enoughMergesWaiting;
                this.RunningMergeCount = runningMergeCount;
                this.Failed = failed;
            }

            protected internal override void DoMerge(MergePolicy.OneMerge merge)
            {
                try
                {
                    // Stall all incoming merges until we see
                    // maxMergeCount:
                    int count = RunningMergeCount.IncrementAndGet();
                    try
                    {
                        Assert.IsTrue(count <= MaxMergeCount, "count=" + count + " vs maxMergeCount=" + MaxMergeCount);
                        EnoughMergesWaiting.Signal();

                        // Stall this merge until we see exactly
                        // maxMergeCount merges waiting
                        while (true)
                        {
                            // wait for 10 milliseconds
                            if (EnoughMergesWaiting.Wait(new TimeSpan(0, 0, 0, 0, 10)) || Failed.Get())
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
                        RunningMergeCount.DecrementAndGet();
                    }
                }
                catch (Exception t)
                {
                    Failed.Set(true);
                    Writer.MergeFinish(merge);
                    throw new Exception(t.Message, t);
                }
            }
        }

        private class TrackingCMS : ConcurrentMergeScheduler
        {
            internal long TotMergedBytes;

            public TrackingCMS()
            {
                SetMaxMergesAndThreads(5, 5);
            }

            protected internal override void DoMerge(MergePolicy.OneMerge merge)
            {
                TotMergedBytes += merge.TotalBytesSize();
                base.DoMerge(merge);
            }
        }

        [Test, MaxTime(300000)]
        public virtual void TestTotalBytesSize()
        {
            Directory d = NewDirectory();
            if (d is MockDirectoryWrapper)
            {
                ((MockDirectoryWrapper)d).Throttling = MockDirectoryWrapper.Throttling_e.NEVER;
            }
            IndexWriterConfig iwc = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random()));
            iwc.SetMaxBufferedDocs(5);
            iwc.SetMergeScheduler(new TrackingCMS());
            if (TestUtil.GetPostingsFormat("id").Equals("SimpleText"))
            {
                // no
                iwc.SetCodec(TestUtil.AlwaysPostingsFormat(new Lucene41PostingsFormat()));
            }
            RandomIndexWriter w = new RandomIndexWriter(Random(), d, iwc);
            for (int i = 0; i < 1000; i++)
            {
                Document doc = new Document();
                doc.Add(new StringField("id", "" + i, Field.Store.NO));
                w.AddDocument(doc);

                if (Random().NextBoolean())
                {
                    w.DeleteDocuments(new Term("id", "" + Random().Next(i + 1)));
                }
            }
            Assert.IsTrue(((TrackingCMS)w.w.Config.MergeScheduler).TotMergedBytes != 0);
            w.Dispose();
            d.Dispose();
        }
    }
}