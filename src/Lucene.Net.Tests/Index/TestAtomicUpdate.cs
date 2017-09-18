using Lucene.Net.Attributes;
using Lucene.Net.Documents;
using Lucene.Net.Store;
using Lucene.Net.Support;
using Lucene.Net.Support.Threading;
using Lucene.Net.Util;
using NUnit.Framework;
using System;
using System.Threading;
using Console = Lucene.Net.Support.SystemConsole;

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
        private abstract class TimedThread : ThreadClass
        {
            internal volatile bool Failed;
            internal int Count;
            internal static float RUN_TIME_MSEC = AtLeast(500);
            internal TimedThread[] AllThreads;

            public abstract void DoWork();

            internal TimedThread(TimedThread[] threads)
            {
                this.AllThreads = threads;
            }

            public override void Run()
            {
                long stopTime = Environment.TickCount + (long)RUN_TIME_MSEC;

                Count = 0;

                try
                {
                    do
                    {
                        if (AnyErrors())
                        {
                            break;
                        }
                        DoWork();
                        Count++;
                    } while (Environment.TickCount < stopTime);
                }
                catch (Exception e)
                {
                    Console.WriteLine(Thread.CurrentThread.Name + ": exc");
                    Console.WriteLine(e.StackTrace);
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
            internal IndexWriter Writer;

            public IndexerThread(IndexWriter writer, TimedThread[] threads)
                : base(threads)
            {
                this.Writer = writer;
            }

            public override void DoWork()
            {
                // Update all 100 docs...
                for (int i = 0; i < 100; i++)
                {
                    Documents.Document d = new Documents.Document();
                    d.Add(new StringField("id", Convert.ToString(i), Field.Store.YES));
                    d.Add(new TextField("contents", English.IntToEnglish(i + 10 * Count), Field.Store.NO));
                    Writer.UpdateDocument(new Term("id", Convert.ToString(i)), d);
                }
            }
        }

        private class SearcherThread : TimedThread
        {
            internal Directory Directory;

            public SearcherThread(Directory directory, TimedThread[] threads)
                : base(threads)
            {
                this.Directory = directory;
            }

            public override void DoWork()
            {
                IndexReader r = DirectoryReader.Open(Directory);
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

            IndexWriterConfig conf = (new IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random()))).SetMaxBufferedDocs(7);
            ((TieredMergePolicy)conf.MergePolicy).MaxMergeAtOnce = 3;
            IndexWriter writer = RandomIndexWriter.MockIndexWriter(directory, conf, Random());

            // Establish a base index of 100 docs:
            for (int i = 0; i < 100; i++)
            {
                Documents.Document d = new Documents.Document();
                d.Add(NewStringField("id", Convert.ToString(i), Field.Store.YES));
                d.Add(NewTextField("contents", English.IntToEnglish(i), Field.Store.NO));
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

            Assert.IsTrue(!indexerThread.Failed, "hit unexpected exception in indexer");
            Assert.IsTrue(!indexerThread2.Failed, "hit unexpected exception in indexer2");
            Assert.IsTrue(!searcherThread1.Failed, "hit unexpected exception in search1");
            Assert.IsTrue(!searcherThread2.Failed, "hit unexpected exception in search2");
            //System.out.println("    Writer: " + indexerThread.count + " iterations");
            //System.out.println("Searcher 1: " + searcherThread1.count + " searchers created");
            //System.out.println("Searcher 2: " + searcherThread2.count + " searchers created");
        }

        /*
          Run above stress test against RAMDirectory and then
          FSDirectory.
        */

#if !NETSTANDARD
        // LUCENENET: There is no Timeout on NUnit for .NET Core.
        [Timeout(60000)]
#endif
        [Test, HasTimeout]
        public virtual void TestAtomicUpdates()
        {
            Directory directory;

            // First in a RAM directory:
            using (directory = new MockDirectoryWrapper(Random(), new RAMDirectory()))
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