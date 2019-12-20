using NUnit.Framework;
using Lucene.Net.Attributes;
using Lucene.Net.Support;
using Lucene.Net.Support.Threading;
using System;
using System.Threading;
using AtomicInt32 = J2N.Threading.Atomic.AtomicInt32;
using Console = Lucene.Net.Support.SystemConsole;

namespace Lucene.Net.Index
{
    using Directory = Lucene.Net.Store.Directory;
    using Document = Documents.Document;
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

    [TestFixture]
    public class TestNRTReaderWithThreads : LuceneTestCase
    {
        internal AtomicInt32 Seq = new AtomicInt32(1);

        [Test, LongRunningTest]
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
            long startTime = Environment.TickCount;
            long duration = 1000;
            while ((Environment.TickCount - startTime) < duration)
            {
                Thread.Sleep(100);
            }
            int delCount = 0;
            int addCount = 0;
            for (int x = 0; x < indexThreads.Length; x++)
            {
                indexThreads[x].Run_Renamed = false;
                Assert.IsNull(indexThreads[x].Ex, "Exception thrown: " + indexThreads[x].Ex);
                addCount += indexThreads[x].AddCount;
                delCount += indexThreads[x].DelCount;
            }
            for (int x = 0; x < indexThreads.Length; x++)
            {
                indexThreads[x].Join();
            }
            for (int x = 0; x < indexThreads.Length; x++)
            {
                Assert.IsNull(indexThreads[x].Ex, "Exception thrown: " + indexThreads[x].Ex);
            }
            //System.out.println("addCount:"+addCount);
            //System.out.println("delCount:"+delCount);
            writer.Dispose();
            mainDir.Dispose();
        }

        public class RunThread : ThreadClass
        {
            private readonly TestNRTReaderWithThreads OuterInstance;

            internal IndexWriter Writer;
            internal volatile bool Run_Renamed = true;
            internal volatile Exception Ex;
            internal int DelCount = 0;
            internal int AddCount = 0;
            internal int Type;
            internal readonly Random r = new Random(Random.Next());

            public RunThread(TestNRTReaderWithThreads outerInstance, int type, IndexWriter writer)
            {
                this.OuterInstance = outerInstance;
                this.Type = type;
                this.Writer = writer;
            }

            public override void Run()
            {
                try
                {
                    while (Run_Renamed)
                    {
                        //int n = random.nextInt(2);
                        if (Type == 0)
                        {
                            int i = OuterInstance.Seq.AddAndGet(1);
                            Document doc = DocHelper.CreateDocument(i, "index1", 10);
                            Writer.AddDocument(doc);
                            AddCount++;
                        }
                        else if (Type == 1)
                        {
                            // we may or may not delete because the term may not exist,
                            // however we're opening and closing the reader rapidly
                            IndexReader reader = Writer.GetReader();
                            int id = r.Next(OuterInstance.Seq);
                            Term term = new Term("id", Convert.ToString(id));
                            int count = TestIndexWriterReader.Count(term, reader);
                            Writer.DeleteDocuments(term);
                            reader.Dispose();
                            DelCount += count;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.StackTrace);
                    this.Ex = ex;
                    Run_Renamed = false;
                }
            }
        }
    }
}