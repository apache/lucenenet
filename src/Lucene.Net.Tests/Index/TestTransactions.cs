using J2N.Threading;
using Lucene.Net.Attributes;
using Lucene.Net.Documents;
using Lucene.Net.Index.Extensions;
using Lucene.Net.Store;
using Lucene.Net.Support.Threading;
using NUnit.Framework;
using System;
using System.IO;
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

    using Directory = Lucene.Net.Store.Directory;
    using Document = Documents.Document;
    using English = Lucene.Net.Util.English;
    using Field = Field;
    using FieldType = FieldType;
    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
    using MockAnalyzer = Lucene.Net.Analysis.MockAnalyzer;
    using MockDirectoryWrapper = Lucene.Net.Store.MockDirectoryWrapper;
    using RAMDirectory = Lucene.Net.Store.RAMDirectory;
    using StackTraceHelper = Lucene.Net.Util.StackTraceHelper;
    using StringField = StringField;

    [TestFixture]
    public class TestTransactions : LuceneTestCase
    {
        private static volatile bool doFail;

        private class RandomFailure : Failure
        {
            public override void Eval(MockDirectoryWrapper dir)
            {
                if (TestTransactions.doFail && Random.Next() % 10 <= 3)
                {
                    throw new IOException("now failing randomly but on purpose");
                }
            }
        }

        private abstract class TimedThread : ThreadJob
        {
            internal volatile bool failed;
            internal static float RUN_TIME_MSEC = AtLeast(500);
            internal TimedThread[] allThreads;

            public abstract void DoWork();

            internal TimedThread(TimedThread[] threads)
            {
                this.allThreads = threads;
            }

            public override void Run()
            {
                long stopTime = (J2N.Time.NanoTime() / J2N.Time.MillisecondsPerNanosecond) + (long)(RUN_TIME_MSEC);

                try
                {
                    do
                    {
                        if (AnyErrors())
                        {
                            break;
                        }
                        DoWork();
                    } while (J2N.Time.NanoTime() / J2N.Time.MillisecondsPerNanosecond < stopTime);
                }
                catch (Exception e) when (e.IsThrowable())
                {
                    Console.WriteLine(Thread.CurrentThread + ": exc");
                    e.PrintStackTrace(Console.Out);
                    failed = true;
                }
            }

            internal virtual bool AnyErrors()
            {
                for (int i = 0; i < allThreads.Length; i++)
                {
                    if (allThreads[i] != null && allThreads[i].failed)
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        private class IndexerThread : TimedThread
        {
            internal Directory dir1;
            internal Directory dir2;
            internal object @lock;
            internal int nextID;

            public IndexerThread(object @lock,
                Directory dir1, Directory dir2,
                TimedThread[] threads)
                : base(threads)
            {
                this.@lock = @lock;
                this.dir1 = dir1;
                this.dir2 = dir2;
            }

            public override void DoWork()
            {
                var config = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random))
                        .SetMaxBufferedDocs(3)
                        .SetMergeScheduler(new ConcurrentMergeScheduler())
                        .SetMergePolicy(NewLogMergePolicy(2));
                IndexWriter writer1 = new IndexWriter(dir1, config);
                ((IConcurrentMergeScheduler)writer1.Config.MergeScheduler).SetSuppressExceptions();

                // Intentionally use different params so flush/merge
                // happen @ different times
                var config2 = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random))
                        .SetMaxBufferedDocs(2)
                        .SetMergeScheduler(new ConcurrentMergeScheduler())
                        .SetMergePolicy(NewLogMergePolicy(3));
                IndexWriter writer2 = new IndexWriter(dir2, config2);
                ((IConcurrentMergeScheduler)writer2.Config.MergeScheduler).SetSuppressExceptions();

                Update(writer1);
                Update(writer2);

                doFail = true;
                try
                {
                    UninterruptableMonitor.Enter(@lock); // LUCENENET: Using UninterruptableMonitor instead of lock/synchronized, see docs for type
                    try
                    {
                        try
                        {
                            writer1.PrepareCommit();
                        }
                        catch (Exception t) when (t.IsThrowable())
                        {
                            writer1.Rollback();
                            writer2.Rollback();
                            return;
                        }
                        try
                        {
                            writer2.PrepareCommit();
                        }
                        catch (Exception t) when (t.IsThrowable())
                        {
                            writer1.Rollback();
                            writer2.Rollback();
                            return;
                        }

                        // LUCENENET specific: deviates from upstream Java, which leaves
                        // these Commit() calls unguarded. The test injects random I/O
                        // failures (RandomFailure) for the whole prepare+commit region,
                        // and Commit() writes the segments file footer via
                        // SegmentInfos.FinishCommit, so an injected IOException can escape
                        // Commit() into TimedThread.Run's catch and trip failed=true. That
                        // is not the behavior under test: the test verifies the
                        // transactional protocol holds *in the presence of* random I/O
                        // failures, so a Commit() failure should be handled the same way as
                        // a PrepareCommit() failure (roll back and abort the cycle). The
                        // same limitation exists in upstream Java; it just surfaces rarely.
                        // See https://github.com/apache/lucenenet/issues/1298.
                        try
                        {
                            writer1.Commit();
                            writer2.Commit();
                        }
                        catch (Exception t) when (t.IsThrowable())
                        {
                            writer1.Rollback();
                            writer2.Rollback();
                            return;
                        }
                    }
                    finally
                    {
                        UninterruptableMonitor.Exit(@lock);
                    }
                }
                finally
                {
                    doFail = false;
                }

                writer1.Dispose();
                writer2.Dispose();
            }

            public virtual void Update(IndexWriter writer)
            {
                // Add 10 docs:
                FieldType customType = new FieldType(StringField.TYPE_NOT_STORED);
                customType.StoreTermVectors = true;
                for (int j = 0; j < 10; j++)
                {
                    Document d = new Document();
                    int n = Random.Next();
                    d.Add(NewField("id", Convert.ToString(nextID++), customType));
                    d.Add(NewTextField("contents", English.Int32ToEnglish(n), Field.Store.NO));
                    writer.AddDocument(d);
                }

                // Delete 5 docs:
                int deleteID = nextID - 1;
                for (int j = 0; j < 5; j++)
                {
                    writer.DeleteDocuments(new Term("id", "" + deleteID));
                    deleteID -= 2;
                }
            }
        }

        private class SearcherThread : TimedThread
        {
            internal Directory dir1;
            internal Directory dir2;
            internal object @lock;

            public SearcherThread(object @lock, Directory dir1, Directory dir2, TimedThread[] threads)
                : base(threads)
            {
                this.@lock = @lock;
                this.dir1 = dir1;
                this.dir2 = dir2;
            }

            public override void DoWork()
            {
                IndexReader r1 = null, r2 = null;
                UninterruptableMonitor.Enter(@lock); // LUCENENET: Using UninterruptableMonitor instead of lock/synchronized, see docs for type
                try
                {
                    try
                    {
                        r1 = DirectoryReader.Open(dir1);
                        r2 = DirectoryReader.Open(dir2);
                    }
                    catch (Exception e) when (e.IsIOException())
                    {
                        if (!e.Message.Contains("on purpose"))
                        {
                            throw; // LUCENENET: CA2200: Rethrow to preserve stack details (https://docs.microsoft.com/en-us/visualstudio/code-quality/ca2200-rethrow-to-preserve-stack-details)
                        }
                        if (r1 != null)
                        {
                            r1.Dispose();
                        }
                        if (r2 != null)
                        {
                            r2.Dispose();
                        }
                        return;
                    }
                }
                finally
                {
                    UninterruptableMonitor.Exit(@lock);
                }
                if (r1.NumDocs != r2.NumDocs)
                {
                    throw RuntimeException.Create("doc counts differ: r1=" + r1.NumDocs + " r2=" + r2.NumDocs);
                }
                r1.Dispose();
                r2.Dispose();
            }
        }

        public virtual void InitIndex(Directory dir)
        {
            IndexWriter writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)));
            for (int j = 0; j < 7; j++)
            {
                Document d = new Document();
                int n = Random.Next();
                d.Add(NewTextField("contents", English.Int32ToEnglish(n), Field.Store.NO));
                writer.AddDocument(d);
            }
            writer.Dispose();
        }

        [Test]
        public virtual void TestTransactions_Mem()
        {
            DoTestTransactions(new RandomFailure(), new RandomFailure());
        }

        // LUCENENET specific: deterministic regression test for the race surfaced
        // intermittently in https://github.com/apache/lucenenet/issues/1298
        // (TestTransactions_Mem reporting "Expected: True Actual: False"). The
        // standard test relies on RandomFailure, which throws during PrepareCommit
        // ~100% of the time, so it almost never reaches Commit, making the
        // Commit-phase failure nearly impossible to reproduce. This variant injects
        // an exception that fires *only* inside SegmentInfos.FinishCommit, which is
        // on the call stack of writer.Commit() (it writes the segments file footer)
        // but never of writer.PrepareCommit(). That makes Commit() throw
        // deterministically while PrepareCommit() stays clean, exercising the
        // previously-unguarded Commit() path in IndexerThread.DoWork. Without the
        // Commit() try/catch added for #1298 this fails with "Expected: True
        // Actual: False"; with it, the Commit failure is treated as a recoverable
        // cycle abort and the test passes.
        [Test, LuceneNetSpecific]
        public virtual void TestTransactions_CommitFailure_IsHandled()
        {
            DoTestTransactions(new FailOnlyInFinishCommit(), new FailOnlyInFinishCommit());
        }

        // Fires "now failing on purpose" only when the call stack is inside
        // SegmentInfos.FinishCommit (which is marked NoInlining so the stack frame
        // remains observable). That method is reachable from writer.Commit() but
        // not writer.PrepareCommit(), so PrepareCommit completes cleanly and the
        // test reaches the Commit() calls in IndexerThread.DoWork.
        private class FailOnlyInFinishCommit : Failure
        {
            public override void Eval(MockDirectoryWrapper dir)
            {
                if (TestTransactions.doFail &&
                    StackTraceHelper.DoesStackTraceContainMethod(nameof(SegmentInfos), nameof(SegmentInfos.FinishCommit)))
                {
                    throw new IOException("now failing on purpose");
                }
            }
        }

        private void DoTestTransactions(Failure failure1, Failure failure2)
        {
            Console.WriteLine("start test");
            // we can't use non-ramdir on windows, because this test needs to double-write.
            MockDirectoryWrapper dir1 = new MockDirectoryWrapper(Random, new RAMDirectory());
            MockDirectoryWrapper dir2 = new MockDirectoryWrapper(Random, new RAMDirectory());
            dir1.PreventDoubleWrite = false;
            dir2.PreventDoubleWrite = false;
            dir1.FailOn(failure1);
            dir2.FailOn(failure2);
            dir1.FailOnOpenInput = false;
            dir2.FailOnOpenInput = false;

            // We throw exceptions in deleteFile, which creates
            // leftover files:
            dir1.AssertNoUnreferencedFilesOnDispose = false;
            dir2.AssertNoUnreferencedFilesOnDispose = false;

            InitIndex(dir1);
            InitIndex(dir2);

            TimedThread[] threads = new TimedThread[3];
            int numThread = 0;

            IndexerThread indexerThread = new IndexerThread(this, dir1, dir2, threads);

            threads[numThread++] = indexerThread;
            indexerThread.Start();

            SearcherThread searcherThread1 = new SearcherThread(this, dir1, dir2, threads);
            threads[numThread++] = searcherThread1;
            searcherThread1.Start();

            SearcherThread searcherThread2 = new SearcherThread(this, dir1, dir2, threads);
            threads[numThread++] = searcherThread2;
            searcherThread2.Start();

            for (int i = 0; i < numThread; i++)
            {
                threads[i].Join();
            }

            for (int i = 0; i < numThread; i++)
            {
                Assert.IsTrue(!threads[i].failed);
            }
            dir1.Dispose();
            dir2.Dispose();

            Console.WriteLine("End test");
        }
    }
}
