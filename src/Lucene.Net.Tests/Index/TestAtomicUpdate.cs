using J2N.Threading;
using Lucene.Net.Documents;
using Lucene.Net.Index.Extensions;
using Lucene.Net.Store;
using Lucene.Net.Util;
using NUnit.Framework;
using System;
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

    using MockAnalyzer = Lucene.Net.Analysis.MockAnalyzer;

    [TestFixture]
    public class TestAtomicUpdate : LuceneTestCase
    {
        private abstract class TimedThread : ThreadJob
        {
            internal volatile bool failed;
            internal int count;
            internal static float RUN_TIME_MSEC = AtLeast(500);
            internal TimedThread[] allThreads;

            public abstract void DoWork();

            internal TimedThread(TimedThread[] threads)
            {
                this.allThreads = threads;
            }

            public override void Run()
            {
                long stopTime = (J2N.Time.NanoTime() / J2N.Time.MillisecondsPerNanosecond) + (long)RUN_TIME_MSEC; // LUCENENET: Use NanoTime() rather than CurrentTimeMilliseconds() for more accurate/reliable results

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
                    } while ((J2N.Time.NanoTime() / J2N.Time.MillisecondsPerNanosecond) < stopTime); // LUCENENET: Use NanoTime() rather than CurrentTimeMilliseconds() for more accurate/reliable results
                }
                catch (Exception e) when (e.IsThrowable())
                {
                    Console.WriteLine(Thread.CurrentThread.Name + ": exc");
                    Console.WriteLine(e.StackTrace);
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

            public IndexerThread(IndexWriter writer, TimedThread[] threads)
                : base(threads)
            {
                this.writer = writer;
            }

            public override void DoWork()
            {
                // Update all 100 docs...
                for (int i = 0; i < 100; i++)
                {
                    Documents.Document d = new Documents.Document();
                    d.Add(new StringField("id", Convert.ToString(i), Field.Store.YES));
                    d.Add(new TextField("contents", English.Int32ToEnglish(i + 10 * count), Field.Store.NO));
                    writer.UpdateDocument(new Term("id", Convert.ToString(i)), d);
                }
            }
        }

        private class SearcherThread : TimedThread
        {
            internal Directory directory;

            public SearcherThread(Directory directory, TimedThread[] threads)
                : base(threads)
            {
                this.directory = directory;
            }

            public override void DoWork()
            {
                IndexReader r = DirectoryReader.Open(directory);
                Assert.AreEqual(100, r.NumDocs);
                r.Dispose();
            }
        }

        /*
          Run one indexer and 2 searchers against single index as
          stress test.
        */

        public virtual void RunTest(Directory directory)
        {
            TimedThread[] threads = new TimedThread[4];

            IndexWriterConfig conf = (new IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random))).SetMaxBufferedDocs(7);
            ((TieredMergePolicy)conf.MergePolicy).MaxMergeAtOnce = 3;
            IndexWriter writer = RandomIndexWriter.MockIndexWriter(directory, conf, Random);

            // Establish a base index of 100 docs:
            for (int i = 0; i < 100; i++)
            {
                Documents.Document d = new Documents.Document();
                d.Add(NewStringField("id", Convert.ToString(i), Field.Store.YES));
                d.Add(NewTextField("contents", English.Int32ToEnglish(i), Field.Store.NO));
                if ((i - 1) % 7 == 0)
                {
                    writer.Commit();
                }
                writer.AddDocument(d);
            }
            writer.Commit();

            IndexReader r = DirectoryReader.Open(directory);
            Assert.AreEqual(100, r.NumDocs);
            r.Dispose();

            IndexerThread indexerThread = new IndexerThread(writer, threads);
            threads[0] = indexerThread;
            indexerThread.Start();

            IndexerThread indexerThread2 = new IndexerThread(writer, threads);
            threads[1] = indexerThread2;
            indexerThread2.Start();

            SearcherThread searcherThread1 = new SearcherThread(directory, threads);
            threads[2] = searcherThread1;
            searcherThread1.Start();

            SearcherThread searcherThread2 = new SearcherThread(directory, threads);
            threads[3] = searcherThread2;
            searcherThread2.Start();

            indexerThread.Join();
            indexerThread2.Join();
            searcherThread1.Join();
            searcherThread2.Join();

            writer.Dispose();

            Assert.IsTrue(!indexerThread.failed, "hit unexpected exception in indexer");
            Assert.IsTrue(!indexerThread2.failed, "hit unexpected exception in indexer2");
            Assert.IsTrue(!searcherThread1.failed, "hit unexpected exception in search1");
            Assert.IsTrue(!searcherThread2.failed, "hit unexpected exception in search2");
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
        public virtual void TestAtomicUpdates()
        {
            Directory directory;

            // First in a RAM directory:
            using (directory = new MockDirectoryWrapper(Random, new RAMDirectory()))
            {
                RunTest(directory);
            }

            // Second in an FSDirectory:
            System.IO.DirectoryInfo dirPath = CreateTempDir("lucene.test.atomic");
            using (directory = NewFSDirectory(dirPath))
            {
                RunTest(directory);
            }
            System.IO.Directory.Delete(dirPath.FullName, true);
        }
    }
}