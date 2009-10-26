/**
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

using NUnit.Framework;

using Lucene.Net.Store;
using Lucene.Net.Util;
using Lucene.Net.Analysis;
using Lucene.Net.Documents;

namespace Lucene.Net.Index
{
    [TestFixture]
    public class TestTransactions : LuceneTestCase
    {
        private static readonly System.Random RANDOM = new System.Random();
        private static volatile bool doFail;

        private class RandomFailure : MockRAMDirectory.Failure
        {
            override public void Eval(MockRAMDirectory dir)
            {
                if (TestTransactions.doFail && RANDOM.Next() % 10 <= 3)
                    throw new System.IO.IOException("now failing randomly but on purpose");
            }
        }

        private abstract class TimedThread : SupportClass.ThreadClass
        {
            internal bool failed;
            private static int RUN_TIME_SEC = 6;
            private TimedThread[] allThreads;

            abstract public void DoWork();

            internal TimedThread(TimedThread[] threads)
            {
                this.allThreads = threads;
            }

            public override void Run()
            {
                System.DateTime stopTime = System.DateTime.Now.AddSeconds(RUN_TIME_SEC);

                try
                {
                    while (System.DateTime.Now < stopTime && !AnyErrors())
                        DoWork();
                }
                catch (System.Exception e)
                {
                    System.Console.Out.WriteLine(System.Threading.Thread.CurrentThread + ": exc");
                    System.Console.Out.WriteLine(e.StackTrace);
                    failed = true;
                }
            }

            private bool AnyErrors()
            {
                for (int i = 0; i < allThreads.Length; i++)
                    if (allThreads[i] != null && allThreads[i].failed)
                        return true;
                return false;
            }
        }

        private class IndexerThread : TimedThread
        {
            Directory dir1;
            Directory dir2;
            object lock_Renamed;
            int nextID;

            public IndexerThread(object lock_Renamed, Directory dir1, Directory dir2, TimedThread[] threads)
                : base(threads)
            {
                this.lock_Renamed = lock_Renamed;
                this.dir1 = dir1;
                this.dir2 = dir2;
            }

            override public void DoWork()
            {

                IndexWriter writer1 = new IndexWriter(dir1, new WhitespaceAnalyzer(), IndexWriter.MaxFieldLength.LIMITED);
                writer1.SetMaxBufferedDocs(3);
                writer1.SetMergeFactor(2);
                ((ConcurrentMergeScheduler)writer1.GetMergeScheduler()).SetSuppressExceptions_ForNUnitTest();

                IndexWriter writer2 = new IndexWriter(dir2, new WhitespaceAnalyzer(), IndexWriter.MaxFieldLength.LIMITED);
                // Intentionally use different params so flush/merge
                // happen @ different times
                writer2.SetMaxBufferedDocs(2);
                writer2.SetMergeFactor(3);
                ((ConcurrentMergeScheduler)writer2.GetMergeScheduler()).SetSuppressExceptions_ForNUnitTest();

                Update(writer1);
                Update(writer2);

                TestTransactions.doFail = true;
                try
                {
                    lock (lock_Renamed)
                    {
                        try
                        {
                            writer1.PrepareCommit();
                        }
                        catch (System.Exception)
                        {
                            writer1.Rollback();
                            writer2.Rollback();
                            return;
                        }
                        try
                        {
                            writer2.PrepareCommit();
                        }
                        catch (System.Exception)
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
                    TestTransactions.doFail = false;
                }

                writer1.Close();
                writer2.Close();
            }

            public void Update(IndexWriter writer)
            {
                // Add 10 docs:
                for (int j = 0; j < 10; j++)
                {
                    Document d = new Document();
                    int n = RANDOM.Next();
                    d.Add(new Field("id", "" + nextID++, Field.Store.YES, Field.Index.NOT_ANALYZED));
                    d.Add(new Field("contents", English.IntToEnglish(n), Field.Store.NO, Field.Index.ANALYZED));
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
            Directory dir1;
            Directory dir2;
            object lock_Renamed;

            public SearcherThread(object lock_Renamed, Directory dir1, Directory dir2, TimedThread[] threads)
                : base(threads)
            {
                this.lock_Renamed = lock_Renamed;
                this.dir1 = dir1;
                this.dir2 = dir2;
            }

            override public void DoWork()
            {
                IndexReader r1, r2;
                lock (lock_Renamed)
                {
                    r1 = IndexReader.Open(dir1);
                    r2 = IndexReader.Open(dir2);
                }
                if (r1.NumDocs() != r2.NumDocs())
                    throw new System.Exception("doc counts differ: r1=" + r1.NumDocs() + " r2=" + r2.NumDocs());
                r1.Close();
                r2.Close();
            }
        }

        public void InitIndex(Directory dir)
        {
            IndexWriter writer = new IndexWriter(dir, new WhitespaceAnalyzer(), IndexWriter.MaxFieldLength.LIMITED);
            for (int j = 0; j < 7; j++)
            {
                Document d = new Document();
                int n = RANDOM.Next();
                d.Add(new Field("contents", English.IntToEnglish(n), Field.Store.NO, Field.Index.ANALYZED));
                writer.AddDocument(d);
            }
            writer.Close();
        }

        [Test]
        public void TestTransactions_Renamed()
        {
            MockRAMDirectory dir1 = new MockRAMDirectory();
            MockRAMDirectory dir2 = new MockRAMDirectory();
            dir1.SetPreventDoubleWrite(false);
            dir2.SetPreventDoubleWrite(false);
            dir1.FailOn(new RandomFailure());
            dir2.FailOn(new RandomFailure());

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
                threads[i].Join();

            for (int i = 0; i < numThread; i++)
                Assert.IsTrue(!((TimedThread)threads[i]).failed);
        }
    }
}
