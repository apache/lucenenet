using J2N.Threading;
using Lucene.Net.Documents;
using Lucene.Net.Index.Extensions;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Util;
using NUnit.Framework;
using System;
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

    using MockAnalyzer = Lucene.Net.Analysis.MockAnalyzer;

    [TestFixture]
    public class TestStressIndexing : LuceneTestCase
    {
        private abstract class TimedThread : ThreadJob
        {
            internal volatile bool failed;
            internal int count;
            private static int RUN_TIME_MSEC = AtLeast(1000);
            private TimedThread[] allThreads;

            public abstract void DoWork();

            internal TimedThread(TimedThread[] threads)
            {
                this.allThreads = threads;
            }

            public override void Run()
            {
                long stopTime = (J2N.Time.NanoTime() / J2N.Time.MillisecondsPerNanosecond) + RUN_TIME_MSEC; // LUCENENET: Use NanoTime() rather than CurrentTimeMilliseconds() for more accurate/reliable results

                count = 0;

                try
                {
                    do
                    {
                        if (AnyErrors())
                        {
                            break;
                        }
                        DoWork();
                        count++;
                    } while (J2N.Time.NanoTime() / J2N.Time.MillisecondsPerNanosecond < stopTime); // LUCENENET: Use NanoTime() rather than CurrentTimeMilliseconds() for more accurate/reliable results
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
            internal IndexWriter writer;
            internal int nextID;

            public IndexerThread(IndexWriter writer, TimedThread[] threads)
                : base(threads)
            {
                this.writer = writer;
            }

            public override void DoWork()
            {
                // Add 10 docs:
                for (int j = 0; j < 10; j++)
                {
                    Document d = new Document();
                    int n = Random.Next();
                    d.Add(NewStringField("id", Convert.ToString(nextID++), Field.Store.YES));
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
            private Directory directory;

            public SearcherThread(Directory directory, TimedThread[] threads)
                : base(threads)
            {
                this.directory = directory;
            }

            public override void DoWork()
            {
                for (int i = 0; i < 100; i++)
                {
                    IndexReader ir = DirectoryReader.Open(directory);
                    IndexSearcher _ = NewSearcher(ir); // LUCENENET: discarding unused `is` variable
                    ir.Dispose();
                }
                count += 100;
            }
        }

        /*
          Run one indexer and 2 searchers against single index as
          stress test.
        */

        public virtual void RunStressTest(Directory directory, IConcurrentMergeScheduler mergeScheduler)
        {
            IndexWriter modifier = new IndexWriter(directory, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)).SetOpenMode(OpenMode.CREATE).SetMaxBufferedDocs(10).SetMergeScheduler(mergeScheduler));
            modifier.Commit();

            TimedThread[] threads = new TimedThread[4];
            int numThread = 0;

            // One modifier that writes 10 docs then removes 5, over
            // and over:
            IndexerThread indexerThread = new IndexerThread(modifier, threads);
            threads[numThread++] = indexerThread;
            indexerThread.Start();

            IndexerThread indexerThread2 = new IndexerThread(modifier, threads);
            threads[numThread++] = indexerThread2;
            indexerThread2.Start();

            // Two searchers that constantly just re-instantiate the
            // searcher:
            SearcherThread searcherThread1 = new SearcherThread(directory, threads);
            threads[numThread++] = searcherThread1;
            searcherThread1.Start();

            SearcherThread searcherThread2 = new SearcherThread(directory, threads);
            threads[numThread++] = searcherThread2;
            searcherThread2.Start();

            for (int i = 0; i < numThread; i++)
            {
                threads[i].Join();
            }

            modifier.Dispose();

            for (int i = 0; i < numThread; i++)
            {
                Assert.IsTrue(!threads[i].failed);
            }

            //System.out.println("    Writer: " + indexerThread.count + " iterations");
            //System.out.println("Searcher 1: " + searcherThread1.count + " searchers created");
            //System.out.println("Searcher 2: " + searcherThread2.count + " searchers created");
        }

        /*
          Run above stress test against RAMDirectory and then
          FSDirectory.
        */

        [Test]
        [Slow]
        public virtual void TestStressIndexAndSearching()
        {
            Directory directory = NewDirectory();
            MockDirectoryWrapper wrapper = directory as MockDirectoryWrapper;
            if (wrapper != null)
            {
                wrapper.AssertNoUnreferencedFilesOnDispose = true;
            }

            RunStressTest(directory, new ConcurrentMergeScheduler());
            directory.Dispose();
        }
    }
}
