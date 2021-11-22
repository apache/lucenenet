using J2N.Threading;
using J2N.Threading.Atomic;
using Lucene.Net.Attributes;
using Lucene.Net.Index.Extensions;
using Lucene.Net.Support.Threading;
using NUnit.Framework;
using RandomizedTesting.Generators;
using System;
using System.Threading;
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
    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
    using MockAnalyzer = Lucene.Net.Analysis.MockAnalyzer;
    using MockDirectoryWrapper = Lucene.Net.Store.MockDirectoryWrapper;

    [TestFixture]
    public class TestNRTReaderWithThreads : LuceneTestCase
    {
        private readonly AtomicInt32 seq = new AtomicInt32(1);

        [Test]
        [Slow] // (occasionally)
        public virtual void TestIndexing()
        {
            Directory mainDir = NewDirectory();
            var wrapper = mainDir as MockDirectoryWrapper;
            if (wrapper != null)
            {
                wrapper.AssertNoDeleteOpenFile = true;
            }
            var writer = new IndexWriter(mainDir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)).SetMaxBufferedDocs(10).SetMergePolicy(NewLogMergePolicy(false, 2)));
            IndexReader reader = writer.GetReader(); // start pooling readers
            reader.Dispose();
            var indexThreads = new RunThread[4];
            for (int x = 0; x < indexThreads.Length; x++)
            {
                indexThreads[x] = new RunThread(this, x % 2, writer);
                indexThreads[x].Name = "Thread " + x;
                indexThreads[x].Start();
            }
            long startTime = J2N.Time.NanoTime() / J2N.Time.MillisecondsPerNanosecond; // LUCENENET: Use NanoTime() rather than CurrentTimeMilliseconds() for more accurate/reliable results
            long duration = 1000;
            while (((J2N.Time.NanoTime() / J2N.Time.MillisecondsPerNanosecond) - startTime) < duration) // LUCENENET: Use NanoTime() rather than CurrentTimeMilliseconds() for more accurate/reliable results
            {
                Thread.Sleep(100);
            }
            int delCount = 0;
            int addCount = 0;
            for (int x = 0; x < indexThreads.Length; x++)
            {
                indexThreads[x].run = false;
                Assert.IsNull(indexThreads[x].ex, "Exception thrown: " + indexThreads[x].ex);
                addCount += indexThreads[x].addCount;
                delCount += indexThreads[x].delCount;
            }
            for (int x = 0; x < indexThreads.Length; x++)
            {
                indexThreads[x].Join();
            }
            for (int x = 0; x < indexThreads.Length; x++)
            {
                Assert.IsNull(indexThreads[x].ex, "Exception thrown: " + indexThreads[x].ex);
            }
            //System.out.println("addCount:"+addCount);
            //System.out.println("delCount:"+delCount);
            writer.Dispose();
            mainDir.Dispose();
        }

        public class RunThread : ThreadJob
        {
            private readonly TestNRTReaderWithThreads outerInstance;

            internal IndexWriter writer;
            internal volatile bool run = true;
            internal volatile Exception ex;
            internal int delCount = 0;
            internal int addCount = 0;
            internal int type;
            internal readonly Random r = new J2N.Randomizer(Random.NextInt64());

            public RunThread(TestNRTReaderWithThreads outerInstance, int type, IndexWriter writer)
            {
                this.outerInstance = outerInstance;
                this.type = type;
                this.writer = writer;
            }

            public override void Run()
            {
                try
                {
                    while (run)
                    {
                        //int n = random.nextInt(2);
                        if (type == 0)
                        {
                            int i = outerInstance.seq.AddAndGet(1);
                            Document doc = DocHelper.CreateDocument(i, "index1", 10);
                            writer.AddDocument(doc);
                            addCount++;
                        }
                        else if (type == 1)
                        {
                            // we may or may not delete because the term may not exist,
                            // however we're opening and closing the reader rapidly
                            IndexReader reader = writer.GetReader();
                            int id = r.Next(outerInstance.seq);
                            Term term = new Term("id", Convert.ToString(id));
                            int count = TestIndexWriterReader.Count(term, reader);
                            writer.DeleteDocuments(term);
                            reader.Dispose();
                            delCount += count;
                        }
                    }
                }
                catch (Exception ex) when (ex.IsThrowable())
                {
                    Console.WriteLine(ex.StackTrace);
                    this.ex = ex;
                    run = false;
                }
            }
        }
    }
}