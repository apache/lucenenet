using Lucene.Net.Documents;
using Lucene.Net.Support;
using NUnit.Framework;
using System;
using System.IO;

namespace Lucene.Net.Store
{
    using DirectoryReader = Lucene.Net.Index.DirectoryReader;
    using Document = Documents.Document;
    using English = Lucene.Net.Util.English;

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

    using Field = Field;
    using IndexReader = Lucene.Net.Index.IndexReader;
    using IndexSearcher = Lucene.Net.Search.IndexSearcher;
    using IndexWriter = Lucene.Net.Index.IndexWriter;
    using IndexWriterConfig = Lucene.Net.Index.IndexWriterConfig;
    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
    using MockAnalyzer = Lucene.Net.Analysis.MockAnalyzer;
    using OpenMode = Lucene.Net.Index.OpenMode;

    /// <summary>
    /// JUnit testcase to test RAMDirectory. RAMDirectory itself is used in many testcases,
    /// but not one of them uses an different constructor other than the default constructor.
    /// </summary>
    [TestFixture]
    public class TestRAMDirectory : LuceneTestCase
    {
        private DirectoryInfo IndexDir = null;

        // add enough document so that the index will be larger than RAMDirectory.READ_BUFFER_SIZE
        private readonly int DocsToAdd = 500;

        // setup the index
        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            //IndexDir = CreateTempDir("RAMDirIndex");
            string tempDir = Path.GetTempPath();
            if (tempDir == null)
                throw new IOException("java.io.tmpdir undefined, cannot run test");
            IndexDir = new DirectoryInfo(Path.Combine(tempDir, "RAMDirIndex"));

            Directory dir = NewFSDirectory(IndexDir);
            IndexWriter writer = new IndexWriter(dir, (new IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random()))).SetOpenMode(OpenMode.CREATE));
            // add some documents
            Document doc = null;
            for (int i = 0; i < DocsToAdd; i++)
            {
                doc = new Document();
                doc.Add(NewStringField("content", English.IntToEnglish(i).Trim(), Field.Store.YES));
                writer.AddDocument(doc);
            }
            Assert.AreEqual(DocsToAdd, writer.MaxDoc);
            writer.Dispose();
            dir.Dispose();
        }

        [Test]
        public virtual void TestRAMDirectoryMem()
        {
            Directory dir = NewFSDirectory(IndexDir);
            MockDirectoryWrapper ramDir = new MockDirectoryWrapper(Random(), new RAMDirectory(dir, NewIOContext(Random())));

            // close the underlaying directory
            dir.Dispose();

            // Check size
            Assert.AreEqual(ramDir.SizeInBytes(), ramDir.RecomputedSizeInBytes);

            // open reader to test document count
            IndexReader reader = DirectoryReader.Open(ramDir);
            Assert.AreEqual(DocsToAdd, reader.NumDocs);

            // open search zo check if all doc's are there
            IndexSearcher searcher = NewSearcher(reader);

            // search for all documents
            for (int i = 0; i < DocsToAdd; i++)
            {
                Document doc = searcher.Doc(i);
                Assert.IsTrue(doc.GetField("content") != null);
            }

            // cleanup
            reader.Dispose();
        }

        private readonly int NumThreads = 10;
        private readonly int DocsPerThread = 40;

        [Test]
        public virtual void TestRAMDirectorySize()
        {
            Directory dir = NewFSDirectory(IndexDir);
            MockDirectoryWrapper ramDir = new MockDirectoryWrapper(Random(), new RAMDirectory(dir, NewIOContext(Random())));
            dir.Dispose();

            IndexWriter writer = new IndexWriter(ramDir, (new IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random()))).SetOpenMode(OpenMode.APPEND));
            writer.ForceMerge(1);

            Assert.AreEqual(ramDir.SizeInBytes(), ramDir.RecomputedSizeInBytes);

            ThreadClass[] threads = new ThreadClass[NumThreads];
            for (int i = 0; i < NumThreads; i++)
            {
                int num = i;
                threads[i] = new ThreadAnonymousInnerClassHelper(this, writer, num);
            }
            for (int i = 0; i < NumThreads; i++)
            {
                threads[i].Start();
            }
            for (int i = 0; i < NumThreads; i++)
            {
                threads[i].Join();
            }

            writer.ForceMerge(1);
            Assert.AreEqual(ramDir.SizeInBytes(), ramDir.RecomputedSizeInBytes);

            writer.Dispose();
        }

        private class ThreadAnonymousInnerClassHelper : ThreadClass
        {
            private readonly TestRAMDirectory OuterInstance;

            private IndexWriter Writer;
            private int Num;

            public ThreadAnonymousInnerClassHelper(TestRAMDirectory outerInstance, IndexWriter writer, int num)
            {
                this.OuterInstance = outerInstance;
                this.Writer = writer;
                this.Num = num;
            }

            public override void Run()
            {
                for (int j = 1; j < OuterInstance.DocsPerThread; j++)
                {
                    Document doc = new Document();
                    doc.Add(OuterInstance.NewStringField("sizeContent", English.IntToEnglish(Num * OuterInstance.DocsPerThread + j).Trim(), Field.Store.YES));
                    try
                    {
                        Writer.AddDocument(doc);
                    }
                    catch (IOException e)
                    {
                        throw new Exception(e.Message, e);
                    }
                }
            }
        }

        [TearDown]
        public override void TearDown()
        {
            // cleanup
            if (IndexDir != null && IndexDir.Exists)
            {
                RmDir(IndexDir);
            }
            base.TearDown();
        }

        // LUCENE-1196
        [Test]
        public virtual void TestIllegalEOF()
        {
            RAMDirectory dir = new RAMDirectory();
            IndexOutput o = dir.CreateOutput("out", NewIOContext(Random()));
            var b = new byte[1024];
            o.WriteBytes(b, 0, 1024);
            o.Dispose();
            IndexInput i = dir.OpenInput("out", NewIOContext(Random()));
            i.Seek(1024);
            i.Dispose();
            dir.Dispose();
        }

        private void RmDir(DirectoryInfo dir)
        {
            FileInfo[] files = dir.GetFiles();
            for (int i = 0; i < files.Length; i++)
            {
                files[i].Delete();
            }
            dir.Delete();
        }

        // LUCENE-2852
        [Test]
        public virtual void TestSeekToEOFThenBack()
        {
            RAMDirectory dir = new RAMDirectory();

            IndexOutput o = dir.CreateOutput("out", NewIOContext(Random()));
            var bytes = new byte[3 * RAMInputStream.BUFFER_SIZE];
            o.WriteBytes(bytes, 0, bytes.Length);
            o.Dispose();

            IndexInput i = dir.OpenInput("out", NewIOContext(Random()));
            i.Seek(2 * RAMInputStream.BUFFER_SIZE - 1);
            i.Seek(3 * RAMInputStream.BUFFER_SIZE);
            i.Seek(RAMInputStream.BUFFER_SIZE);
            i.ReadBytes(bytes, 0, 2 * RAMInputStream.BUFFER_SIZE);
            i.Dispose();
            dir.Dispose();
        }
    }
}