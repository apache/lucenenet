using J2N.Threading;
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

    using BaseDirectoryWrapper = Lucene.Net.Store.BaseDirectoryWrapper;
    using BytesRef = Lucene.Net.Util.BytesRef;
    using Directory = Lucene.Net.Store.Directory;
    using DocIdSetIterator = Lucene.Net.Search.DocIdSetIterator;
    using Document = Documents.Document;
    using Field = Field;
    using FieldType = FieldType;
    using IBits = Lucene.Net.Util.IBits;
    using LineFileDocs = Lucene.Net.Util.LineFileDocs;
    using LockObtainFailedException = Lucene.Net.Store.LockObtainFailedException;
    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
    using MockAnalyzer = Lucene.Net.Analysis.MockAnalyzer;
    using MockDirectoryWrapper = Lucene.Net.Store.MockDirectoryWrapper;
    using NumericDocValuesField = NumericDocValuesField;
    using TestUtil = Lucene.Net.Util.TestUtil;
    using TextField = TextField;

    /// <summary>
    /// MultiThreaded IndexWriter tests
    /// </summary>
    [SuppressCodecs("Lucene3x")]
    [Slow]
    [TestFixture]
    public class TestIndexWriterWithThreads : LuceneTestCase
    {
        // Used by test cases below
        private class IndexerThread : ThreadJob
        {
            private readonly Func<string, string, FieldType, Field> newField;

            internal bool diskFull;
            internal Exception error;
            //internal ObjectDisposedException ace; // LUCENENET: Not used
            internal IndexWriter writer;
            internal bool noErrors;
            internal volatile int addCount;
            internal int timeToRunInMilliseconds = 200;

            /// <param name="newField">
            /// LUCENENET specific
            /// Passed in because <see cref="LuceneTestCase.NewField(string, string, FieldType)"/>
            /// is no longer static.
            /// </param>
            public IndexerThread(IndexWriter writer, bool noErrors, Func<string, string, FieldType, Field> newField)
            {
                this.writer = writer;
                this.noErrors = noErrors;
                this.newField = newField;
            }

            public override void Run()
            {
                Document doc = new Document();
                FieldType customType = new FieldType(TextField.TYPE_STORED);
                customType.StoreTermVectors = true;
                customType.StoreTermVectorPositions = true;
                customType.StoreTermVectorOffsets = true;

                doc.Add(newField("field", "aaa bbb ccc ddd eee fff ggg hhh iii jjj", customType));
                doc.Add(new NumericDocValuesField("dv", 5));

                int idUpto = 0;
                int fullCount = 0;
                // LUCENENET: Use NanoTime() rather than CurrentTimeMilliseconds() for more accurate/reliable results
                long stopTime = (J2N.Time.NanoTime() / J2N.Time.MillisecondsPerNanosecond) + timeToRunInMilliseconds; // LUCENENET specific: added the ability to change how much time to alot

                do
                {
                    try
                    {
                        writer.UpdateDocument(new Term("id", "" + (idUpto++)), doc);
                        addCount++;
                    }
                    catch (Exception ioe) when (ioe.IsIOException())
                    {
                        if (Verbose)
                        {
                            Console.WriteLine("TEST: expected exc:");
                            Console.WriteLine(ioe.StackTrace);
                        }
                        //System.out.println(Thread.currentThread().getName() + ": hit exc");
                        //ioConsole.WriteLine(e.StackTrace);
                        if (ioe.Message.StartsWith("fake disk full at", StringComparison.Ordinal) || ioe.Message.Equals("now failing on purpose", StringComparison.Ordinal))
                        {
                            diskFull = true;

                            try
                            {
                                Thread.Sleep(1);
                            }
                            catch (Exception ie) when (ie.IsInterruptedException())
                            {
                                throw new Util.ThreadInterruptedException(ie);
                            }

                            if (fullCount++ >= 5)
                            {
                                break;
                            }
                        }
                        else
                        {
                            if (noErrors)
                            {
                                Console.WriteLine(Thread.CurrentThread.Name + ": ERROR: unexpected IOException:");
                                Console.WriteLine(ioe.StackTrace);
                                error = ioe;
                            }
                            break;
                        }
                    }
                    catch (Exception t) when (t.IsThrowable())
                    {
                        //Console.WriteLine(t.StackTrace);
                        if (noErrors)
                        {
                            Console.WriteLine(Thread.CurrentThread.Name + ": ERROR: unexpected Throwable:");
                            Console.WriteLine(t.StackTrace);
                            error = t;
                        }
                        break;
                    }
                } while (J2N.Time.NanoTime() / J2N.Time.MillisecondsPerNanosecond < stopTime); // LUCENENET: Use NanoTime() rather than CurrentTimeMilliseconds() for more accurate/reliable results
            }
        }

        // LUCENE-1130: make sure immediate disk full on creating
        // an IndexWriter (hit during DW.ThreadState.Init()), with
        // multiple threads, is OK:
        [Test]
        public virtual void TestImmediateDiskFullWithThreads()
        {
            int NUM_THREADS = 3;
            int numIterations = TestNightly ? 10 : 3;
            for (int iter = 0; iter < numIterations; iter++)
            {
                if (Verbose)
                {
                    Console.WriteLine("\nTEST: iter=" + iter);
                }
                MockDirectoryWrapper dir = NewMockDirectory();
                IndexWriter writer = new IndexWriter(
                    dir,
                    NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random))
                        .SetMaxBufferedDocs(2)
                        .SetMergeScheduler(new ConcurrentMergeScheduler())
                        .SetMergePolicy(NewLogMergePolicy(4)));
                ((IConcurrentMergeScheduler)writer.Config.MergeScheduler).SetSuppressExceptions();

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
                    Assert.IsTrue(threads[i].error is null, "hit unexpected Throwable");
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
        public virtual void TestCloseWithThreads()
        {
            int NUM_THREADS = 3;
            int numIterations = TestNightly ? 7 : 3;
            for (int iter = 0; iter < numIterations; iter++)
            {
                if (Verbose)
                {
                    Console.WriteLine("\nTEST: iter=" + iter);
                }
                Directory dir = NewDirectory();
                IndexWriter writer = new IndexWriter(
                    dir,
                    NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random))
                        .SetMaxBufferedDocs(10)
                        .SetMergeScheduler(new ConcurrentMergeScheduler())
                        .SetMergePolicy(NewLogMergePolicy(4)));
                ((IConcurrentMergeScheduler)writer.Config.MergeScheduler).SetSuppressExceptions();

                IndexerThread[] threads = new IndexerThread[NUM_THREADS];

                for (int i = 0; i < NUM_THREADS; i++)
                {
                    threads[i] = new IndexerThread(writer, false, NewField)

                        // LUCENENET NOTE - ConcurrentMergeScheduler 
                        // used to take too long for this test to index a single document
                        // so, increased the time from 200 to 300 ms. 
                        // But it has now been restored to 200 ms like Lucene.
                        { timeToRunInMilliseconds = 200 };
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
                        if (threads[i].addCount > 0)
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

                if (Verbose)
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
                DocsEnum tdocs = TestUtil.Docs(Random, reader, "field", new BytesRef("aaa"), MultiFields.GetLiveDocs(reader), null, 0);
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
        public virtual void TestMultipleThreadsFailure(Failure failure)
        {
            int NUM_THREADS = 3;

            for (int iter = 0; iter < 2; iter++)
            {
                if (Verbose)
                {
                    Console.WriteLine("TEST: iter=" + iter);
                }
                MockDirectoryWrapper dir = NewMockDirectory();
                IndexWriter writer = new IndexWriter(
                    dir,
                    NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random))
                        .SetMaxBufferedDocs(2)
                        .SetMergeScheduler(new ConcurrentMergeScheduler())
                        .SetMergePolicy(NewLogMergePolicy(4)));
                ((IConcurrentMergeScheduler)writer.Config.MergeScheduler).SetSuppressExceptions();

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
                    Assert.IsTrue(threads[i].error is null, "hit unexpected Throwable");
                }

                bool success = false;
                try
                {
                    writer.Dispose(false);
                    success = true;
                }
                catch (Exception ioe) when (ioe.IsIOException())
                {
                    failure.ClearDoFail();
                    writer.Dispose(false);
                }
                if (Verbose)
                {
                    Console.WriteLine("TEST: success=" + success);
                }

                if (success)
                {
                    IndexReader reader = DirectoryReader.Open(dir);
                    IBits delDocs = MultiFields.GetLiveDocs(reader);
                    for (int j = 0; j < reader.MaxDoc; j++)
                    {
                        if (delDocs is null || !delDocs.Get(j))
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
        public virtual void TestSingleThreadFailure(Failure failure)
        {
            MockDirectoryWrapper dir = NewMockDirectory();

            IndexWriter writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)).SetMaxBufferedDocs(2).SetMergeScheduler(new ConcurrentMergeScheduler()));
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
            catch (Exception ioe) when (ioe.IsIOException())
            {
            }
            failure.ClearDoFail();
            writer.AddDocument(doc);
            writer.Dispose(false);
            dir.Dispose();
        }

        // Throws IOException during FieldsWriter.flushDocument and during DocumentsWriter.abort
        private class FailOnlyOnAbortOrFlush : Failure
        {
            internal bool onlyOnce;

            public FailOnlyOnAbortOrFlush(bool onlyOnce)
            {
                this.onlyOnce = onlyOnce;
            }

            public override void Eval(MockDirectoryWrapper dir)
            {
                // Since we throw exc during abort, eg when IW is
                // attempting to delete files, we will leave
                // leftovers:
                dir.AssertNoUnreferencedFilesOnDispose = false;

                if (m_doFail)
                {
                    // LUCENENET specific: for these to work in release mode, we have added [MethodImpl(MethodImplOptions.NoInlining)]
                    // to each possible target of the StackTraceHelper. If these change, so must the attribute on the target methods.
                    bool sawAbortOrFlushDoc = StackTraceHelper.DoesStackTraceContainMethod("Abort")
                        || StackTraceHelper.DoesStackTraceContainMethod("FinishDocument");
                    bool sawClose = StackTraceHelper.DoesStackTraceContainMethod("Close")
                        || StackTraceHelper.DoesStackTraceContainMethod("Dispose");
                    bool sawMerge = StackTraceHelper.DoesStackTraceContainMethod("Merge");

                    if (sawAbortOrFlushDoc && !sawClose && !sawMerge)
                    {
                        if (onlyOnce)
                        {
                            m_doFail = false;
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
        public virtual void TestIOExceptionDuringAbort()
        {
            TestSingleThreadFailure(new FailOnlyOnAbortOrFlush(false));
        }

        // LUCENE-1130: make sure initial IOException, and then 2nd
        // IOException during rollback(), is OK:
        [Test]
        public virtual void TestIOExceptionDuringAbortOnlyOnce()
        {
            TestSingleThreadFailure(new FailOnlyOnAbortOrFlush(true));
        }

        // LUCENE-1130: make sure initial IOException, and then 2nd
        // IOException during rollback(), with multiple threads, is OK:
        [Test]
        public virtual void TestIOExceptionDuringAbortWithThreads()
        {
            TestMultipleThreadsFailure(new FailOnlyOnAbortOrFlush(false));
        }

        // LUCENE-1130: make sure initial IOException, and then 2nd
        // IOException during rollback(), with multiple threads, is OK:
        [Test]
        public virtual void TestIOExceptionDuringAbortWithThreadsOnlyOnce()
        {
            TestMultipleThreadsFailure( new FailOnlyOnAbortOrFlush(true));
        }

        // Throws IOException during DocumentsWriter.writeSegment
        private class FailOnlyInWriteSegment : Failure
        {
            internal bool onlyOnce;

            public FailOnlyInWriteSegment(bool onlyOnce)
            {
                this.onlyOnce = onlyOnce;
            }

            public override void Eval(MockDirectoryWrapper dir)
            {
                if (m_doFail)
                {
                    // LUCENENET specific: for these to work in release mode, we have added [MethodImpl(MethodImplOptions.NoInlining)]
                    // to each possible target of the StackTraceHelper. If these change, so must the attribute on the target methods.
                    if (StackTraceHelper.DoesStackTraceContainMethod(typeof(DocFieldProcessor).Name, "Flush"))
                    {
                        if (onlyOnce)
                        {
                            m_doFail = false;
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
        public virtual void TestIOExceptionDuringWriteSegment()
        {
            TestSingleThreadFailure(new FailOnlyInWriteSegment(false));
        }

        // LUCENE-1130: test IOException in writeSegment
        [Test]
        public virtual void TestIOExceptionDuringWriteSegmentOnlyOnce()
        {
            TestSingleThreadFailure(new FailOnlyInWriteSegment(true));
        }

        // LUCENE-1130: test IOException in writeSegment, with threads
        [Test]
        public virtual void TestIOExceptionDuringWriteSegmentWithThreads()
        {
            TestMultipleThreadsFailure(new FailOnlyInWriteSegment(false));
        }

        // LUCENE-1130: test IOException in writeSegment, with threads
        [Test]
        public virtual void TestIOExceptionDuringWriteSegmentWithThreadsOnlyOnce()
        {
            TestMultipleThreadsFailure(new FailOnlyInWriteSegment(true));
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

            DelayedIndexAndCloseRunnable thread1 = new DelayedIndexAndCloseRunnable(dir, oneIWConstructed);
            DelayedIndexAndCloseRunnable thread2 = new DelayedIndexAndCloseRunnable(dir, oneIWConstructed);


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
                AssumeFalse("aborting test: timeout obtaining lock", thread1.failure is LockObtainFailedException);
                AssumeFalse("aborting test: timeout obtaining lock", thread2.failure is LockObtainFailedException);

                Assert.IsFalse(thread1.failed, "Failed due to: " + thread1.failure);
                Assert.IsFalse(thread2.failed, "Failed due to: " + thread2.failure);
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

        internal class DelayedIndexAndCloseRunnable : ThreadJob
        {
            internal readonly Directory dir;
            internal bool failed = false;
            internal Exception failure = null;
            internal readonly CountdownEvent startIndexing = new CountdownEvent(1);
            internal CountdownEvent iwConstructed;

            public DelayedIndexAndCloseRunnable(Directory dir, CountdownEvent iwConstructed)
            {
                this.dir = dir;
                this.iwConstructed = iwConstructed;
            }

            public virtual void StartIndexing()
            {
                this.startIndexing.Signal();
            }

            public override void Run()
            {
                try
                {
                    Document doc = new Document();
                    Field field = NewTextField("field", "testData", Field.Store.YES);
                    doc.Add(field);
                    using IndexWriter writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)));
                    if (iwConstructed.CurrentCount > 0)
                    {
                        iwConstructed.Signal();
                    }
                    startIndexing.Wait();
                    writer.AddDocument(doc);
                }
                catch (Exception e) when (e.IsThrowable())
                {
                    failed = true;
                    failure = e;
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

            int threadCount = TestUtil.NextInt32(Random, 2, 6);

            MockAnalyzer analyzer = new MockAnalyzer(Random);
            analyzer.MaxTokenLength = TestUtil.NextInt32(Random, 1, IndexWriter.MAX_TERM_LENGTH);
            AtomicReference<IndexWriter> writerRef =
                new AtomicReference<IndexWriter>(new IndexWriter(d, NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer)));

            LineFileDocs docs = new LineFileDocs(Random);
            ThreadJob[] threads = new ThreadJob[threadCount];
            int iters = AtLeast(100);
            AtomicBoolean failed = new AtomicBoolean();
            ReentrantLock rollbackLock = new ReentrantLock();
            ReentrantLock commitLock = new ReentrantLock();
            for (int threadID = 0; threadID < threadCount; threadID++)
            {
                threads[threadID] = new ThreadAnonymousClass(d, writerRef, docs, iters, failed, rollbackLock, commitLock);
                threads[threadID].Start();
            }

            for (int threadID = 0; threadID < threadCount; threadID++)
            {
                try
                {
                    threads[threadID].Join();
                } 
                catch (Exception e)
                {
                    Console.WriteLine("EXCEPTION in ThreadAnonymousClass: " + Environment.NewLine + e);
                }
            }

            Assert.IsTrue(!failed.Value);
            writerRef.Value.Dispose();
            d.Dispose();
        }

        private sealed class ThreadAnonymousClass : ThreadJob
        {
            private readonly BaseDirectoryWrapper d;
            private readonly AtomicReference<IndexWriter> writerRef;
            private readonly LineFileDocs docs;
            private readonly int iters;
            private readonly AtomicBoolean failed;
            private readonly ReentrantLock rollbackLock;
            private readonly ReentrantLock commitLock;

            public ThreadAnonymousClass(BaseDirectoryWrapper d, AtomicReference<IndexWriter> writerRef, LineFileDocs docs, int iters, AtomicBoolean failed, ReentrantLock rollbackLock, ReentrantLock commitLock)
            {
                this.d = d;
                this.writerRef = writerRef;
                this.docs = docs;
                this.iters = iters;
                this.failed = failed;
                this.rollbackLock = rollbackLock;
                this.commitLock = commitLock;
            }

            public override void Run()
            {
                for (int iter = 0; iter < iters && !failed.Value; iter++)
                {
                    //final int x = Random().nextInt(5);
                    int x = Random.Next(3);
                    try
                    {
                        switch (x)
                        {
                            case 0:
                                rollbackLock.@Lock();
                                if (Verbose)
                                {
                                    Console.WriteLine("\nTEST: " + Thread.CurrentThread.Name + ": now rollback");
                                }
                                try
                                {
                                    writerRef.Value.Rollback();
                                    if (Verbose)
                                    {
                                        Console.WriteLine("TEST: " + Thread.CurrentThread.Name + ": rollback done; now open new writer");
                                    }
                                    writerRef.Value = 
                                        new IndexWriter(d, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)));
                                }
                                finally
                                {
                                    rollbackLock.Unlock();
                                }
                                break;

                            case 1:
                                commitLock.@Lock();
                                if (Verbose)
                                {
                                    Console.WriteLine("\nTEST: " + Thread.CurrentThread.Name + ": now commit");
                                }
                                try
                                {
                                    if (Random.NextBoolean())
                                    {
                                        writerRef.Value.PrepareCommit();
                                    }
                                    writerRef.Value.Commit();
                                }
                                catch (Exception ace) when (ace.IsAlreadyClosedException())
                                {
                                    // ok
                                }
                                catch (NullReferenceException) // LUCENENET TODO: - NullReferenceException must be allowed to propagate so we can defensively avoid it in .NET
                                {
                                    // ok
                                }
                                finally
                                {
                                    commitLock.Unlock();
                                }
                                break;

                            case 2:
                                if (Verbose)
                                {
                                    Console.WriteLine("\nTEST: " + Thread.CurrentThread.Name + ": now add");
                                }
                                try
                                {
                                    writerRef.Value.AddDocument(docs.NextDoc());
                                }
                                catch (Exception ace) when (ace.IsAlreadyClosedException())
                                {
                                    // ok
                                }
                                catch (NullReferenceException) // LUCENENET TODO: - NullReferenceException must be allowed to propagate so we can defensively avoid it in .NET
                                {
                                    // ok
                                }
                                catch (Exception ae) when (ae.IsAssertionError())
                                {
                                    // ok
                                }
                                break;
                        }
                    }
                    catch (Exception t) when (t.IsThrowable())
                    {
                        failed.Value = (true);
                        throw RuntimeException.Create(t);
                    }
                }
            }
        }
    }
}