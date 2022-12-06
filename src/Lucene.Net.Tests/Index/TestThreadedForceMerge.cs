using J2N.Threading;
using Lucene.Net.Documents;
using Lucene.Net.Index.Extensions;
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

    using Analyzer = Lucene.Net.Analysis.Analyzer;
    using Directory = Lucene.Net.Store.Directory;
    using Document = Documents.Document;
    using English = Lucene.Net.Util.English;
    using FieldType = FieldType;
    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
    using MockAnalyzer = Lucene.Net.Analysis.MockAnalyzer;
    using MockTokenizer = Lucene.Net.Analysis.MockTokenizer;
    using StringField = StringField;

    [TestFixture]
    public class TestThreadedForceMerge : LuceneTestCase
    {
        private static Analyzer ANALYZER;

        private const int NUM_THREADS = 3;
        //private final static int NUM_THREADS = 5;

        private const int NUM_ITER = 1;

        private const int NUM_ITER2 = 1;

        private volatile bool failed;

        [SetUp]
        public static void Setup()
        {
            ANALYZER = new MockAnalyzer(Random, MockTokenizer.SIMPLE, true);
        }

        private void SetFailed()
        {
            failed = true;
        }

        public virtual void RunTest(Random random, Directory directory)
        {
            IndexWriter writer = new IndexWriter(directory, ((IndexWriterConfig)NewIndexWriterConfig(TEST_VERSION_CURRENT, ANALYZER).SetOpenMode(OpenMode.CREATE).SetMaxBufferedDocs(2)).SetMergePolicy(NewLogMergePolicy()));

            for (int iter = 0; iter < NUM_ITER; iter++)
            {
                int iterFinal = iter;

                ((LogMergePolicy)writer.Config.MergePolicy).MergeFactor = 1000;

                FieldType customType = new FieldType(StringField.TYPE_STORED);
                customType.OmitNorms = true;

                for (int i = 0; i < 200; i++)
                {
                    Document d = new Document();
                    d.Add(NewField("id", Convert.ToString(i), customType));
                    d.Add(NewField("contents", English.Int32ToEnglish(i), customType));
                    writer.AddDocument(d);
                }

                ((LogMergePolicy)writer.Config.MergePolicy).MergeFactor = 4;

                ThreadJob[] threads = new ThreadJob[NUM_THREADS];

                for (int i = 0; i < NUM_THREADS; i++)
                {
                    int iFinal = i;
                    IndexWriter writerFinal = writer;
                    threads[i] = new ThreadAnonymousClass(this, iterFinal, customType, iFinal, writerFinal);
                }

                for (int i = 0; i < NUM_THREADS; i++)
                {
                    threads[i].Start();
                }

                for (int i = 0; i < NUM_THREADS; i++)
                {
                    threads[i].Join();
                }

                Assert.IsTrue(!failed);

                int expectedDocCount = (int)((1 + iter) * (200 + 8 * NUM_ITER2 * (NUM_THREADS / 2.0) * (1 + NUM_THREADS)));

                Assert.AreEqual(expectedDocCount, writer.NumDocs, "index=" + writer.SegString() + " numDocs=" + writer.NumDocs + " maxDoc=" + writer.MaxDoc + " config=" + writer.Config);
                Assert.AreEqual(expectedDocCount, writer.MaxDoc, "index=" + writer.SegString() + " numDocs=" + writer.NumDocs + " maxDoc=" + writer.MaxDoc + " config=" + writer.Config);

                writer.Dispose();
                writer = new IndexWriter(directory, (IndexWriterConfig)NewIndexWriterConfig(TEST_VERSION_CURRENT, ANALYZER).SetOpenMode(OpenMode.APPEND).SetMaxBufferedDocs(2));

                DirectoryReader reader = DirectoryReader.Open(directory);
                Assert.AreEqual(1, reader.Leaves.Count, "reader=" + reader);
                Assert.AreEqual(expectedDocCount, reader.NumDocs);
                reader.Dispose();
            }
            writer.Dispose();
        }

        private sealed class ThreadAnonymousClass : ThreadJob
        {
            private readonly TestThreadedForceMerge outerInstance;

            private readonly int iterFinal;
            private readonly FieldType customType;
            private readonly int iFinal;
            private readonly IndexWriter writerFinal;

            public ThreadAnonymousClass(TestThreadedForceMerge outerInstance, int iterFinal, FieldType customType, int iFinal, IndexWriter writerFinal)
            {
                this.outerInstance = outerInstance;
                this.iterFinal = iterFinal;
                this.customType = customType;
                this.iFinal = iFinal;
                this.writerFinal = writerFinal;
            }

            public override void Run()
            {
                try
                {
                    for (int j = 0; j < NUM_ITER2; j++)
                    {
                        writerFinal.ForceMerge(1, false);
                        for (int k = 0; k < 17 * (1 + iFinal); k++)
                        {
                            Document d = new Document();
                            d.Add(NewField("id", iterFinal + "_" + iFinal + "_" + j + "_" + k, customType));
                            d.Add(NewField("contents", English.Int32ToEnglish(iFinal + k), customType));
                            writerFinal.AddDocument(d);
                        }
                        for (int k = 0; k < 9 * (1 + iFinal); k++)
                        {
                            writerFinal.DeleteDocuments(new Term("id", iterFinal + "_" + iFinal + "_" + j + "_" + k));
                        }
                        writerFinal.ForceMerge(1);
                    }
                }
                catch (Exception t) when (t.IsThrowable())
                {
                    outerInstance.SetFailed();
                    Console.WriteLine(Thread.CurrentThread.Name + ": hit exception");
                    Console.WriteLine(t.StackTrace);
                }
            }
        }

        /*
          Run above stress test against RAMDirectory and then
          FSDirectory.
        */

        [Test]
        public virtual void TestThreadedForceMerge_Mem()
        {
            Directory directory = NewDirectory();
            RunTest(Random, directory);
            directory.Dispose();
        }
    }
}