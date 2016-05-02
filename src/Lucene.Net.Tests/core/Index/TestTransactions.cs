using System;
using System.Threading;
using Lucene.Net.Documents;

namespace Lucene.Net.Index
{
    using Lucene.Net.Support;
    using NUnit.Framework;
    using System.IO;
    using Directory = Lucene.Net.Store.Directory;
    using Document = Documents.Document;
    using English = Lucene.Net.Util.English;
    using Field = Field;
    using FieldType = FieldType;
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
    using RAMDirectory = Lucene.Net.Store.RAMDirectory;
    using StringField = StringField;

    [TestFixture]
    public class TestTransactions : LuceneTestCase
    {
        private static volatile bool DoFail;

        private class RandomFailure : MockDirectoryWrapper.Failure
        {
            private readonly TestTransactions OuterInstance;

            public RandomFailure(TestTransactions outerInstance)
            {
                this.OuterInstance = outerInstance;
            }

            public override void Eval(MockDirectoryWrapper dir)
            {
                if (DoFail && Random().Next() % 10 <= 3)
                {
                    throw new IOException("now failing randomly but on purpose");
                }
            }
        }

        private abstract class TimedThread : ThreadClass
        {
            internal volatile bool Failed;
            internal static float RUN_TIME_MSEC = AtLeast(500);
            internal TimedThread[] AllThreads;

            public abstract void DoWork();

            internal TimedThread(TimedThread[] threads)
            {
                this.AllThreads = threads;
            }

            public override void Run()
            {
                long stopTime = Environment.TickCount + (long)(RUN_TIME_MSEC);

                try
                {
                    do
                    {
                        if (AnyErrors())
                        {
                            break;
                        }
                        DoWork();
                    } while (Environment.TickCount < stopTime);
                }
                catch (Exception e)
                {
                    Console.WriteLine(Thread.CurrentThread + ": exc");
                    Console.Error.WriteLine(e.StackTrace);
                    Failed = true;
                }
            }

            internal virtual bool AnyErrors()
            {
                for (int i = 0; i < AllThreads.Length; i++)
                {
                    if (AllThreads[i] != null && AllThreads[i].Failed)
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        private class IndexerThread : TimedThread
        {
            private readonly TestTransactions OuterInstance;
            private IConcurrentMergeScheduler _scheduler1;
            private IConcurrentMergeScheduler _scheduler2;
            internal Directory Dir1;
            internal Directory Dir2;
            internal object @lock;
            internal int NextID;

            public IndexerThread(TestTransactions outerInstance, object @lock, 
                Directory dir1, Directory dir2,
                IConcurrentMergeScheduler scheduler1, IConcurrentMergeScheduler scheduler2, 
                TimedThread[] threads)
                : base(threads)
            {
                _scheduler1 = scheduler1;
                _scheduler2 = scheduler2;
                this.OuterInstance = outerInstance;
                this.@lock = @lock;
                this.Dir1 = dir1;
                this.Dir2 = dir2;
            }

            public override void DoWork()
            {
                var config = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random()))
                                .SetMaxBufferedDocs(3)
                                .SetMergeScheduler(_scheduler1)
                                .SetMergePolicy(NewLogMergePolicy(2));
                IndexWriter writer1 = new IndexWriter(Dir1, config);
                ((IConcurrentMergeScheduler)writer1.Config.MergeScheduler).SetSuppressExceptions();

                // Intentionally use different params so flush/merge
                // happen @ different times
                var config2 = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random()))
                                .SetMaxBufferedDocs(2)
                                .SetMergeScheduler(_scheduler2)
                                .SetMergePolicy(NewLogMergePolicy(3));
                IndexWriter writer2 = new IndexWriter(Dir2, config2);
                ((IConcurrentMergeScheduler)writer2.Config.MergeScheduler).SetSuppressExceptions();

                Update(writer1);
                Update(writer2);

                DoFail = true;
                try
                {
                    lock (@lock)
                    {
                        try
                        {
                            writer1.PrepareCommit();
                        }
                        catch (Exception)
                        {
                            writer1.Rollback();
                            writer2.Rollback();
                            return;
                        }
                        try
                        {
                            writer2.PrepareCommit();
                        }
                        catch (Exception)
                        {
                            writer1.Rollback();
                            writer2.Rollback();
                            return;
                        }

                        writer1.Commit();
                        writer2.Commit();
                    }
                }
                finally
                {
                    DoFail = false;
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
                    int n = Random().Next();
                    d.Add(NewField("id", Convert.ToString(NextID++), customType));
                    d.Add(NewTextField("contents", English.IntToEnglish(n), Field.Store.NO));
                    writer.AddDocument(d);
                }

                // Delete 5 docs:
                int deleteID = NextID - 1;
                for (int j = 0; j < 5; j++)
                {
                    writer.DeleteDocuments(new Term("id", "" + deleteID));
                    deleteID -= 2;
                }
            }
        }

        private class SearcherThread : TimedThread
        {
            internal Directory Dir1;
            internal Directory Dir2;
            internal object @lock;

            public SearcherThread(object @lock, Directory dir1, Directory dir2, TimedThread[] threads)
                : base(threads)
            {
                this.@lock = @lock;
                this.Dir1 = dir1;
                this.Dir2 = dir2;
            }

            public override void DoWork()
            {
                IndexReader r1 = null, r2 = null;
                lock (@lock)
                {
                    try
                    {
                        r1 = DirectoryReader.Open(Dir1);
                        r2 = DirectoryReader.Open(Dir2);
                    }
                    catch (IOException e)
                    {
                        if (!e.Message.Contains("on purpose"))
                        {
                            throw e;
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
                if (r1.NumDocs != r2.NumDocs)
                {
                    throw new Exception("doc counts differ: r1=" + r1.NumDocs + " r2=" + r2.NumDocs);
                }
                r1.Dispose();
                r2.Dispose();
            }
        }

        public virtual void InitIndex(Directory dir)
        {
            IndexWriter writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())));
            for (int j = 0; j < 7; j++)
            {
                Document d = new Document();
                int n = Random().Next();
                d.Add(NewTextField("contents", English.IntToEnglish(n), Field.Store.NO));
                writer.AddDocument(d);
            }
            writer.Dispose();
        }

        [Test, Sequential]
        public virtual void TestTransactions_Mem(
            [ValueSource(typeof(ConcurrentMergeSchedulers), "Values")]IConcurrentMergeScheduler scheduler1, 
            [ValueSource(typeof(ConcurrentMergeSchedulers), "Values")]IConcurrentMergeScheduler scheduler2)
        {
            Console.WriteLine("Start test");
            // we cant use non-ramdir on windows, because this test needs to double-write.
            MockDirectoryWrapper dir1 = new MockDirectoryWrapper(Random(), new RAMDirectory());
            MockDirectoryWrapper dir2 = new MockDirectoryWrapper(Random(), new RAMDirectory());
            dir1.PreventDoubleWrite = false;
            dir2.PreventDoubleWrite = false;
            dir1.FailOn(new RandomFailure(this));
            dir2.FailOn(new RandomFailure(this));
            dir1.FailOnOpenInput = false;
            dir2.FailOnOpenInput = false;

            // We throw exceptions in deleteFile, which creates
            // leftover files:
            dir1.AssertNoUnrefencedFilesOnClose = false;
            dir2.AssertNoUnrefencedFilesOnClose = false;

            InitIndex(dir1);
            InitIndex(dir2);

            TimedThread[] threads = new TimedThread[3];
            int numThread = 0;

            IndexerThread indexerThread = new IndexerThread(this, this, dir1, dir2, scheduler1, scheduler2, threads);
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
                Assert.IsTrue(!threads[i].Failed);
            }
            dir1.Dispose();
            dir2.Dispose();

            Console.WriteLine("End test");
        }
    }
}