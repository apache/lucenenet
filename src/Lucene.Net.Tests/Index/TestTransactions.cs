using J2N.Threading;
using Lucene.Net.Documents;
using Lucene.Net.Index.Extensions;
using Lucene.Net.Store;
using Lucene.Net.Support.Threading;
using NUnit.Framework;
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
    using English = Lucene.Net.Util.English;
    using Field = Field;
    using FieldType = FieldType;
    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
    using MockAnalyzer = Lucene.Net.Analysis.MockAnalyzer;
    using MockDirectoryWrapper = Lucene.Net.Store.MockDirectoryWrapper;
    using RAMDirectory = Lucene.Net.Store.RAMDirectory;
    using StringField = StringField;

    [TestFixture]
    public class TestTransactions : LuceneTestCase
    {
        private static volatile bool doFail;

        private class RandomFailure : Failure
        {
            private readonly TestTransactions outerInstance;

            public RandomFailure(TestTransactions outerInstance)
            {
                this.outerInstance = outerInstance;
            }

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
                    Console.Error.WriteLine(e.StackTrace);
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
            private readonly TestTransactions outerInstance;
            internal Directory dir1;
            internal Directory dir2;
            internal object @lock;
            internal int nextID;

            public IndexerThread(TestTransactions outerInstance, object @lock, 
                Directory dir1, Directory dir2,
                TimedThread[] threads)
                : base(threads)
            {
                this.outerInstance = outerInstance;
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
                    UninterruptableMonitor.Enter(@lock);
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

                        writer1.Commit();
                        writer2.Commit();
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
                UninterruptableMonitor.Enter(@lock);
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
            Console.WriteLine("start test");
            // we cant use non-ramdir on windows, because this test needs to double-write.
            MockDirectoryWrapper dir1 = new MockDirectoryWrapper(Random, new RAMDirectory());
            MockDirectoryWrapper dir2 = new MockDirectoryWrapper(Random, new RAMDirectory());
            dir1.PreventDoubleWrite = false;
            dir2.PreventDoubleWrite = false;
            dir1.FailOn(new RandomFailure(this));
            dir2.FailOn(new RandomFailure(this));
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

            IndexerThread indexerThread = new IndexerThread(this, this, dir1, dir2, threads);

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