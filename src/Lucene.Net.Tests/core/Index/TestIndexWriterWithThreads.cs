using System;
using System.Diagnostics;
using System.Threading;
using Lucene.Net.Documents;

namespace Lucene.Net.Index
{
    //using Slow = Lucene.Net.Util.LuceneTestCase.Slow;
    using Lucene.Net.Randomized.Generators;
    using Lucene.Net.Support;
    using NUnit.Framework;
    using System.IO;
    using Util;
    using AlreadyClosedException = Lucene.Net.Store.AlreadyClosedException;
    using BaseDirectoryWrapper = Lucene.Net.Store.BaseDirectoryWrapper;
    using Bits = Lucene.Net.Util.Bits;
    using BytesRef = Lucene.Net.Util.BytesRef;
    using Directory = Lucene.Net.Store.Directory;
    using DocIdSetIterator = Lucene.Net.Search.DocIdSetIterator;
    using Document = Documents.Document;
    using Field = Field;
    using FieldType = FieldType;
    using LineFileDocs = Lucene.Net.Util.LineFileDocs;
    using LockObtainFailedException = Lucene.Net.Store.LockObtainFailedException;
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
    using NumericDocValuesField = NumericDocValuesField;
    using TestUtil = Lucene.Net.Util.TestUtil;
    using TextField = TextField;
    using Util;

    /// <summary>
    /// MultiThreaded IndexWriter tests
    /// </summary>
    [SuppressCodecs("Lucene3x")]
    [TestFixture]
    public class TestIndexWriterWithThreads : LuceneTestCase
    {
        // Used by test cases below
        private class IndexerThread : ThreadClass
        {
            private readonly Func<string, string, FieldType, Field> NewField;

            internal bool DiskFull;
            internal Exception Error;
            internal AlreadyClosedException Ace;
            internal IndexWriter Writer;
            internal bool NoErrors;
            internal volatile int AddCount;

            /// <param name="newField">
            /// LUCENENET specific
            /// Passed in because <see cref="LuceneTestCase.NewField(string, string, FieldType)"/>
            /// is no longer static.
            /// </param>
            public IndexerThread(IndexWriter writer, bool noErrors, Func<string, string, FieldType, Field> newField)
            {
                this.Writer = writer;
                this.NoErrors = noErrors;
                NewField = newField;
            }

            public override void Run()
            {
                Document doc = new Document();
                FieldType customType = new FieldType(TextField.TYPE_STORED);
                customType.StoreTermVectors = true;
                customType.StoreTermVectorPositions = true;
                customType.StoreTermVectorOffsets = true;

                doc.Add(NewField("field", "aaa bbb ccc ddd eee fff ggg hhh iii jjj", customType));
                doc.Add(new NumericDocValuesField("dv", 5));

                int idUpto = 0;
                int fullCount = 0;
                long stopTime = Environment.TickCount + 200;

                do
                {
                    try
                    {
                        Writer.UpdateDocument(new Term("id", "" + (idUpto++)), doc);
                        AddCount++;
                    }
                    catch (IOException ioe)
                    {
                        if (VERBOSE)
                        {
                            Console.WriteLine("TEST: expected exc:");
                            Console.WriteLine(ioe.StackTrace);
                        }
                        //System.out.println(Thread.currentThread().getName() + ": hit exc");
                        //ioConsole.WriteLine(e.StackTrace);
                        if (ioe.Message.StartsWith("fake disk full at") || ioe.Message.Equals("now failing on purpose"))
                        {
                            DiskFull = true;
#if !NETSTANDARD
                            try
                            {
#endif
                            Thread.Sleep(1);
#if !NETSTANDARD
                            }
                            catch (ThreadInterruptedException ie)
                            {
                                throw new ThreadInterruptedException("Thread Interrupted Exception", ie);
                            }
#endif
                            if (fullCount++ >= 5)
                            {
                                break;
                            }
                        }
                        else
                        {
                            if (NoErrors)
                            {
                                Console.WriteLine(Thread.CurrentThread.Name + ": ERROR: unexpected IOException:");
                                Console.WriteLine(ioe.StackTrace);
                                Error = ioe;
                            }
                            break;
                        }
                    }
                    catch (Exception t)
                    {
                        //Console.WriteLine(t.StackTrace);
                        if (NoErrors)
                        {
                            Console.WriteLine(Thread.CurrentThread.Name + ": ERROR: unexpected Throwable:");
                            Console.WriteLine(t.StackTrace);
                            Error = t;
                        }
                        break;
                    }
                } while (Environment.TickCount < stopTime);
            }
        }

        // LUCENE-1130: make sure immediate disk full on creating
        // an IndexWriter (hit during DW.ThreadState.Init()), with
        // multiple threads, is OK:
        [Test]
        public virtual void TestImmediateDiskFullWithThreads([ValueSource(typeof(ConcurrentMergeSchedulers), "Values")]IConcurrentMergeScheduler scheduler)
        {
            int NUM_THREADS = 3;
            int numIterations = TEST_NIGHTLY ? 10 : 3;
            for (int iter = 0; iter < numIterations; iter++)
            {
                if (VERBOSE)
                {
                    Console.WriteLine("\nTEST: iter=" + iter);
                }
                MockDirectoryWrapper dir = NewMockDirectory();
                var config = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random()))
                                .SetMaxBufferedDocs(2)
                                .SetMergeScheduler(scheduler)
                                .SetMergePolicy(NewLogMergePolicy(4));
                IndexWriter writer = new IndexWriter(dir, config);
                scheduler.SetSuppressExceptions();
                dir.MaxSizeInBytes = 4 * 1024 + 20 * iter;

                IndexerThread[] threads = new IndexerThread[NUM_THREADS];

                for (int i = 0; i < NUM_THREADS; i++)
                {
                    threads[i] = new IndexerThread(writer, true, NewField);
                }

                for (int i = 0; i < NUM_THREADS; i++)
                {
                    threads[i].Start();
                }

                for (int i = 0; i < NUM_THREADS; i++)
                {
                    // Without fix for LUCENE-1130: one of the
                    // threads will hang
                    threads[i].Join();
                    Assert.IsTrue(threads[i].Error == null, "hit unexpected Throwable");
                }

                // Make sure once disk space is avail again, we can
                // cleanly close:
                dir.MaxSizeInBytes = 0;
                writer.Dispose(false);
                dir.Dispose();
            }
        }

        // LUCENE-1130: make sure we can close() even while
        // threads are trying to add documents.  Strictly
        // speaking, this isn't valid us of Lucene's APIs, but we
        // still want to be robust to this case:
        [Test]
        public virtual void TestCloseWithThreads([ValueSource(typeof(ConcurrentMergeSchedulers), "Values")]IConcurrentMergeScheduler scheduler)
        {
            int NUM_THREADS = 3;
            int numIterations = TEST_NIGHTLY ? 7 : 3;
            for (int iter = 0; iter < numIterations; iter++)
            {
                if (VERBOSE)
                {
                    Console.WriteLine("\nTEST: iter=" + iter);
                }
                Directory dir = NewDirectory();
                var config = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random()))
                                .SetMaxBufferedDocs(10)
                                .SetMergeScheduler(scheduler)
                                .SetMergePolicy(NewLogMergePolicy(4));
                IndexWriter writer = new IndexWriter(dir, config);
                scheduler.SetSuppressExceptions();

                IndexerThread[] threads = new IndexerThread[NUM_THREADS];

                for (int i = 0; i < NUM_THREADS; i++)
                {
                    threads[i] = new IndexerThread(writer, false, NewField);
                }

                for (int i = 0; i < NUM_THREADS; i++)
                {
                    threads[i].Start();
                }

                bool done = false;
                while (!done)
                {
                    Thread.Sleep(100);
                    for (int i = 0; i < NUM_THREADS; i++)
                    // only stop when at least one thread has added a doc
                    {
                        if (threads[i].AddCount > 0)
                        {
                            done = true;
                            break;
                        }
                        else if (!threads[i].IsAlive)
                        {
                            Assert.Fail("thread failed before indexing a single document");
                        }
                    }
                }

                if (VERBOSE)
                {
                    Console.WriteLine("\nTEST: now close");
                }
                writer.Dispose(false);

                // Make sure threads that are adding docs are not hung:
                for (int i = 0; i < NUM_THREADS; i++)
                {
                    // Without fix for LUCENE-1130: one of the
                    // threads will hang
                    threads[i].Join();
                    if (threads[i].IsAlive)
                    {
                        Assert.Fail("thread seems to be hung");
                    }
                }

                // Quick test to make sure index is not corrupt:
                IndexReader reader = DirectoryReader.Open(dir);
                DocsEnum tdocs = TestUtil.Docs(Random(), reader, "field", new BytesRef("aaa"), MultiFields.GetLiveDocs(reader), null, 0);
                int count = 0;
                while (tdocs.NextDoc() != DocIdSetIterator.NO_MORE_DOCS)
                {
                    count++;
                }
                Assert.IsTrue(count > 0);
                reader.Dispose();

                dir.Dispose();
            }
        }

        // Runs test, with multiple threads, using the specific
        // failure to trigger an IOException
        public virtual void TestMultipleThreadsFailure(IConcurrentMergeScheduler scheduler, MockDirectoryWrapper.Failure failure)
        {
            int NUM_THREADS = 3;

            for (int iter = 0; iter < 2; iter++)
            {
                if (VERBOSE)
                {
                    Console.WriteLine("TEST: iter=" + iter);
                }
                MockDirectoryWrapper dir = NewMockDirectory();
                var config = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random()))
                                .SetMaxBufferedDocs(2)
                                .SetMergeScheduler(scheduler)
                                .SetMergePolicy(NewLogMergePolicy(4));
                IndexWriter writer = new IndexWriter(dir, config);
                scheduler.SetSuppressExceptions();

                IndexerThread[] threads = new IndexerThread[NUM_THREADS];

                for (int i = 0; i < NUM_THREADS; i++)
                {
                    threads[i] = new IndexerThread(writer, true, NewField);
                }

                for (int i = 0; i < NUM_THREADS; i++)
                {
                    threads[i].Start();
                }

                Thread.Sleep(10);

                dir.FailOn(failure);
                failure.SetDoFail();

                for (int i = 0; i < NUM_THREADS; i++)
                {
                    threads[i].Join();
                    Assert.IsTrue(threads[i].Error == null, "hit unexpected Throwable");
                }

                bool success = false;
                try
                {
                    writer.Dispose(false);
                    success = true;
                }
                catch (IOException)
                {
                    failure.ClearDoFail();
                    writer.Dispose(false);
                }
                if (VERBOSE)
                {
                    Console.WriteLine("TEST: success=" + success);
                }

                if (success)
                {
                    IndexReader reader = DirectoryReader.Open(dir);
                    Bits delDocs = MultiFields.GetLiveDocs(reader);
                    for (int j = 0; j < reader.MaxDoc; j++)
                    {
                        if (delDocs == null || !delDocs.Get(j))
                        {
                            reader.Document(j);
                            reader.GetTermVectors(j);
                        }
                    }
                    reader.Dispose();
                }

                dir.Dispose();
            }
        }

        // Runs test, with one thread, using the specific failure
        // to trigger an IOException
        public virtual void TestSingleThreadFailure(IConcurrentMergeScheduler scheduler, MockDirectoryWrapper.Failure failure)
        {
            MockDirectoryWrapper dir = NewMockDirectory();

            IndexWriter writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())).SetMaxBufferedDocs(2).SetMergeScheduler(scheduler));
            Document doc = new Document();
            FieldType customType = new FieldType(TextField.TYPE_STORED);
            customType.StoreTermVectors = true;
            customType.StoreTermVectorPositions = true;
            customType.StoreTermVectorOffsets = true;
            doc.Add(NewField("field", "aaa bbb ccc ddd eee fff ggg hhh iii jjj", customType));

            for (int i = 0; i < 6; i++)
            {
                writer.AddDocument(doc);
            }

            dir.FailOn(failure);
            failure.SetDoFail();
            try
            {
                writer.AddDocument(doc);
                writer.AddDocument(doc);
                writer.Commit();
                Assert.Fail("did not hit exception");
            }
            catch (IOException)
            {
            }
            failure.ClearDoFail();
            writer.AddDocument(doc);
            writer.Dispose(false);
            dir.Dispose();
        }

        // Throws IOException during FieldsWriter.flushDocument and during DocumentsWriter.abort
        private class FailOnlyOnAbortOrFlush : MockDirectoryWrapper.Failure
        {
            internal bool OnlyOnce;

            public FailOnlyOnAbortOrFlush(bool onlyOnce)
            {
                this.OnlyOnce = onlyOnce;
            }

            public override void Eval(MockDirectoryWrapper dir)
            {
                // Since we throw exc during abort, eg when IW is
                // attempting to delete files, we will leave
                // leftovers:
                dir.AssertNoUnrefencedFilesOnClose = false;

                if (DoFail)
                {
                    bool sawAbortOrFlushDoc = StackTraceHelper.DoesStackTraceContainMethod("Abort")
                        || StackTraceHelper.DoesStackTraceContainMethod("FinishDocument");
                    bool sawClose = StackTraceHelper.DoesStackTraceContainMethod("Close")
                        || StackTraceHelper.DoesStackTraceContainMethod("Dispose");
                    bool sawMerge = StackTraceHelper.DoesStackTraceContainMethod("Merge");

                    if (sawAbortOrFlushDoc && !sawClose && !sawMerge)
                    {
                        if (OnlyOnce)
                        {
                            DoFail = false;
                        }
                        //System.out.println(Thread.currentThread().getName() + ": now fail");
                        //new Throwable(Console.WriteLine().StackTrace);
                        throw new IOException("now failing on purpose");
                    }
                }
            }
        }

        // LUCENE-1130: make sure initial IOException, and then 2nd
        // IOException during rollback(), is OK:
        [Test]
        public virtual void TestIOExceptionDuringAbort([ValueSource(typeof(ConcurrentMergeSchedulers), "Values")]IConcurrentMergeScheduler scheduler)
        {
            TestSingleThreadFailure(scheduler, new FailOnlyOnAbortOrFlush(false));
        }

        // LUCENE-1130: make sure initial IOException, and then 2nd
        // IOException during rollback(), is OK:
        [Test]
        public virtual void TestIOExceptionDuringAbortOnlyOnce([ValueSource(typeof(ConcurrentMergeSchedulers), "Values")]IConcurrentMergeScheduler scheduler)
        {
            TestSingleThreadFailure(scheduler, new FailOnlyOnAbortOrFlush(true));
        }

        // LUCENE-1130: make sure initial IOException, and then 2nd
        // IOException during rollback(), with multiple threads, is OK:
        [Test]
        public virtual void TestIOExceptionDuringAbortWithThreads([ValueSource(typeof(ConcurrentMergeSchedulers), "Values")]IConcurrentMergeScheduler scheduler)
        {
            TestMultipleThreadsFailure(scheduler, new FailOnlyOnAbortOrFlush(false));
        }

        // LUCENE-1130: make sure initial IOException, and then 2nd
        // IOException during rollback(), with multiple threads, is OK:
        [Test]
        public virtual void TestIOExceptionDuringAbortWithThreadsOnlyOnce([ValueSource(typeof(ConcurrentMergeSchedulers), "Values")]IConcurrentMergeScheduler scheduler)
        {
            TestMultipleThreadsFailure(scheduler, new FailOnlyOnAbortOrFlush(true));
        }

        // Throws IOException during DocumentsWriter.writeSegment
        private class FailOnlyInWriteSegment : MockDirectoryWrapper.Failure
        {
            internal bool OnlyOnce;

            public FailOnlyInWriteSegment(bool onlyOnce)
            {
                this.OnlyOnce = onlyOnce;
            }

            public override void Eval(MockDirectoryWrapper dir)
            {
                if (DoFail)
                {
                    if (StackTraceHelper.DoesStackTraceContainMethod("Flush") /*&& "Lucene.Net.Index.DocFieldProcessor".Equals(frame.GetType().Name)*/)
                    {
                        if (OnlyOnce)
                        {
                            DoFail = false;
                        }
                        //System.out.println(Thread.currentThread().getName() + ": NOW FAIL: onlyOnce=" + onlyOnce);
                        //new Throwable(Console.WriteLine().StackTrace);
                        throw new IOException("now failing on purpose");
                    }
                }
            }
        }

        // LUCENE-1130: test IOException in writeSegment
        [Test]
        public virtual void TestIOExceptionDuringWriteSegment([ValueSource(typeof(ConcurrentMergeSchedulers), "Values")]IConcurrentMergeScheduler scheduler)
        {
            TestSingleThreadFailure(scheduler, new FailOnlyInWriteSegment(false));
        }

        // LUCENE-1130: test IOException in writeSegment
        [Test]
        public virtual void TestIOExceptionDuringWriteSegmentOnlyOnce([ValueSource(typeof(ConcurrentMergeSchedulers), "Values")]IConcurrentMergeScheduler scheduler)
        {
            TestSingleThreadFailure(scheduler, new FailOnlyInWriteSegment(true));
        }

        // LUCENE-1130: test IOException in writeSegment, with threads
        [Test]
        public virtual void TestIOExceptionDuringWriteSegmentWithThreads([ValueSource(typeof(ConcurrentMergeSchedulers), "Values")]IConcurrentMergeScheduler scheduler)
        {
            TestMultipleThreadsFailure(scheduler, new FailOnlyInWriteSegment(false));
        }

        // LUCENE-1130: test IOException in writeSegment, with threads
        [Test]
        public virtual void TestIOExceptionDuringWriteSegmentWithThreadsOnlyOnce([ValueSource(typeof(ConcurrentMergeSchedulers), "Values")]IConcurrentMergeScheduler scheduler)
        {
            TestMultipleThreadsFailure(scheduler, new FailOnlyInWriteSegment(true));
        }

        //  LUCENE-3365: Test adding two documents with the same field from two different IndexWriters
        //  that we attempt to open at the same time.  As long as the first IndexWriter completes
        //  and closes before the second IndexWriter time's out trying to get the Lock,
        //  we should see both documents
        [Test]
        public virtual void TestOpenTwoIndexWritersOnDifferentThreads()
        {
            Directory dir = NewDirectory();
            CountdownEvent oneIWConstructed = new CountdownEvent(1);
            DelayedIndexAndCloseRunnable thread1 = new DelayedIndexAndCloseRunnable(dir, oneIWConstructed, this);
            DelayedIndexAndCloseRunnable thread2 = new DelayedIndexAndCloseRunnable(dir, oneIWConstructed, this);

            thread1.Start();
            thread2.Start();
            oneIWConstructed.Wait();

            thread1.StartIndexing();
            thread2.StartIndexing();

            thread1.Join();
            thread2.Join();

            // ensure the directory is closed if we hit the timeout and throw assume
            // TODO: can we improve this in LuceneTestCase? I dont know what the logic would be...
            try
            {
                AssumeFalse("aborting test: timeout obtaining lock", thread1.Failure is LockObtainFailedException);
                AssumeFalse("aborting test: timeout obtaining lock", thread2.Failure is LockObtainFailedException);

                Assert.IsFalse(thread1.Failed, "Failed due to: " + thread1.Failure);
                Assert.IsFalse(thread2.Failed, "Failed due to: " + thread2.Failure);
                // now verify that we have two documents in the index
                IndexReader reader = DirectoryReader.Open(dir);
                Assert.AreEqual(2, reader.NumDocs, "IndexReader should have one document per thread running");

                reader.Dispose();
            }
            finally
            {
                dir.Dispose();
            }
        }

        internal class DelayedIndexAndCloseRunnable : ThreadClass
        {
            internal readonly Directory Dir;
            internal bool Failed = false;
            internal Exception Failure = null;
            internal readonly CountdownEvent StartIndexing_Renamed = new CountdownEvent(1);
            internal CountdownEvent IwConstructed;
            private readonly LuceneTestCase OuterInstance;

            /// <param name="outerInstance">
            /// LUCENENET specific
            /// Passed in because this class acceses non-static methods,
            /// NewTextField and NewIndexWriterConfig
            /// </param>
            public DelayedIndexAndCloseRunnable(Directory dir, CountdownEvent iwConstructed, LuceneTestCase outerInstance)
            {
                this.Dir = dir;
                this.IwConstructed = iwConstructed;
                OuterInstance = outerInstance;
            }

            public virtual void StartIndexing()
            {
                this.StartIndexing_Renamed.Signal();
            }

            public override void Run()
            {
                try
                {
                    Document doc = new Document();
                    Field field = OuterInstance.NewTextField("field", "testData", Field.Store.YES);
                    doc.Add(field);
                    using (IndexWriter writer = new IndexWriter(Dir, OuterInstance.NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random()))))
                    {
                        if (IwConstructed.CurrentCount > 0)
                        {
                            IwConstructed.Signal();
                        }
                        StartIndexing_Renamed.Wait();
                        writer.AddDocument(doc);
                    }
                }
                catch (Exception e)
                {
                    Failed = true;
                    Failure = e;
                    Console.WriteLine(e.ToString());
                    return;
                }
            }
        }

        // LUCENE-4147
        [Test]
        public virtual void TestRollbackAndCommitWithThreads()
        {
            BaseDirectoryWrapper d = NewDirectory();
            if (d is MockDirectoryWrapper)
            {
                ((MockDirectoryWrapper)d).PreventDoubleWrite = false;
            }

            int threadCount = TestUtil.NextInt(Random(), 2, 6);

            MockAnalyzer analyzer = new MockAnalyzer(Random());
            analyzer.MaxTokenLength = TestUtil.NextInt(Random(), 1, IndexWriter.MAX_TERM_LENGTH);
            AtomicObject<IndexWriter> writerRef =
                new AtomicObject<IndexWriter>(new IndexWriter(d, NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer)));

            LineFileDocs docs = new LineFileDocs(Random());
            ThreadClass[] threads = new ThreadClass[threadCount];
            int iters = AtLeast(100);
            AtomicBoolean failed = new AtomicBoolean();
            ReentrantLock rollbackLock = new ReentrantLock();
            ReentrantLock commitLock = new ReentrantLock();
            for (int threadID = 0; threadID < threadCount; threadID++)
            {
                threads[threadID] = new ThreadAnonymousInnerClassHelper(this, d, writerRef, docs, iters, failed, rollbackLock, commitLock);
                threads[threadID].Start();
            }

            for (int threadID = 0; threadID < threadCount; threadID++)
            {
                threads[threadID].Join();
            }

            Assert.IsTrue(!failed.Get());
            writerRef.Value.Dispose();
            d.Dispose();
        }

        private class ThreadAnonymousInnerClassHelper : ThreadClass
        {
            private readonly TestIndexWriterWithThreads OuterInstance;

            private BaseDirectoryWrapper d;
            private AtomicObject<IndexWriter> WriterRef;
            private LineFileDocs Docs;
            private int Iters;
            private AtomicBoolean Failed;
            private ReentrantLock RollbackLock;
            private ReentrantLock CommitLock;

            public ThreadAnonymousInnerClassHelper(TestIndexWriterWithThreads outerInstance, BaseDirectoryWrapper d, AtomicObject<IndexWriter> writerRef, LineFileDocs docs, int iters, AtomicBoolean failed, ReentrantLock rollbackLock, ReentrantLock commitLock)
            {
                this.OuterInstance = outerInstance;
                this.d = d;
                this.WriterRef = writerRef;
                this.Docs = docs;
                this.Iters = iters;
                this.Failed = failed;
                this.RollbackLock = rollbackLock;
                this.CommitLock = commitLock;
            }

            public override void Run()
            {
                for (int iter = 0; iter < Iters && !Failed.Get(); iter++)
                {
                    //final int x = Random().nextInt(5);
                    int x = Random().Next(3);
                    try
                    {
                        switch (x)
                        {
                            case 0:
                                RollbackLock.@Lock();
                                if (VERBOSE)
                                {
                                    Console.WriteLine("\nTEST: " + Thread.CurrentThread.Name + ": now rollback");
                                }
                                try
                                {
                                    WriterRef.Value.Rollback();
                                    if (VERBOSE)
                                    {
                                        Console.WriteLine("TEST: " + Thread.CurrentThread.Name + ": rollback done; now open new writer");
                                    }
                                    WriterRef.Value = 
                                        new IndexWriter(d, OuterInstance.NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())));
                                }
                                finally
                                {
                                    RollbackLock.Unlock();
                                }
                                break;

                            case 1:
                                CommitLock.@Lock();
                                if (VERBOSE)
                                {
                                    Console.WriteLine("\nTEST: " + Thread.CurrentThread.Name + ": now commit");
                                }
                                try
                                {
                                    if (Random().NextBoolean())
                                    {
                                        WriterRef.Value.PrepareCommit();
                                    }
                                    WriterRef.Value.Commit();
                                }
                                catch (AlreadyClosedException)
                                {
                                    // ok
                                }
                                catch (NullReferenceException)
                                {
                                    // ok
                                }
                                finally
                                {
                                    CommitLock.Unlock();
                                }
                                break;

                            case 2:
                                if (VERBOSE)
                                {
                                    Console.WriteLine("\nTEST: " + Thread.CurrentThread.Name + ": now add");
                                }
                                try
                                {
                                    WriterRef.Value.AddDocument(Docs.NextDoc());
                                }
                                catch (AlreadyClosedException)
                                {
                                    // ok
                                }
                                catch (System.NullReferenceException)
                                {
                                    // ok
                                }
                                catch (InvalidOperationException)
                                {
                                    // ok
                                }
                                break;
                        }
                    }
                    catch (Exception t)
                    {
                        Failed.Set(true);
                        throw new Exception(t.Message, t);
                    }
                }
            }
        }
    }
}