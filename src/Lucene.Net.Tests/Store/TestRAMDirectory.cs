using J2N.Threading;
using Lucene.Net.Documents;
using Lucene.Net.Index.Extensions;
using Lucene.Net.Support.IO;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using Assert = Lucene.Net.TestFramework.Assert;

namespace Lucene.Net.Store
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

    using DirectoryReader = Lucene.Net.Index.DirectoryReader;
    using Document = Documents.Document;
    using English = Lucene.Net.Util.English;
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
        private DirectoryInfo indexDir = null;

        // add enough document so that the index will be larger than RAMDirectory.READ_BUFFER_SIZE
        private readonly int docsToAdd = 500;

        // setup the index
        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            //IndexDir = CreateTempDir("RAMDirIndex");
            string tempDir = Path.GetTempPath();
            if (tempDir is null)
                throw new IOException("java.io.tmpdir undefined, cannot run test");
            indexDir = new DirectoryInfo(Path.Combine(tempDir, "RAMDirIndex"));

            Directory dir = NewFSDirectory(indexDir);
            IndexWriter writer = new IndexWriter(dir, (new IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random))).SetOpenMode(OpenMode.CREATE));
            // add some documents
            Document doc = null;
            for (int i = 0; i < docsToAdd; i++)
            {
                doc = new Document();
                doc.Add(NewStringField("content", English.Int32ToEnglish(i).Trim(), Field.Store.YES));
                writer.AddDocument(doc);
            }
            Assert.AreEqual(docsToAdd, writer.MaxDoc);
            writer.Dispose();
            dir.Dispose();
        }

        [Test]
        public virtual void TestRAMDirectoryMem()
        {
            Directory dir = NewFSDirectory(indexDir);
            MockDirectoryWrapper ramDir = new MockDirectoryWrapper(Random, new RAMDirectory(dir, NewIOContext(Random)));

            // close the underlaying directory
            dir.Dispose();

            // Check size
            Assert.AreEqual(ramDir.GetSizeInBytes(), ramDir.GetRecomputedSizeInBytes());

            // open reader to test document count
            IndexReader reader = DirectoryReader.Open(ramDir);
            Assert.AreEqual(docsToAdd, reader.NumDocs);

            // open search zo check if all doc's are there
            IndexSearcher searcher = NewSearcher(reader);

            // search for all documents
            for (int i = 0; i < docsToAdd; i++)
            {
                Document doc = searcher.Doc(i);
                Assert.IsTrue(doc.GetField("content") != null);
            }

            // cleanup
            reader.Dispose();
        }

        private readonly int numThreads = 10;
        private readonly int docsPerThread = 40;

        [Test]
        public virtual void TestRAMDirectorySize()
        {
            Directory dir = NewFSDirectory(indexDir);
            MockDirectoryWrapper ramDir = new MockDirectoryWrapper(Random, new RAMDirectory(dir, NewIOContext(Random)));
            dir.Dispose();

            IndexWriter writer = new IndexWriter(ramDir, (new IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random))).SetOpenMode(OpenMode.APPEND));
            writer.ForceMerge(1);

            Assert.AreEqual(ramDir.GetSizeInBytes(), ramDir.GetRecomputedSizeInBytes());

            ThreadJob[] threads = new ThreadJob[numThreads];
            for (int i = 0; i < numThreads; i++)
            {
                int num = i;
                threads[i] = new ThreadAnonymousClass(this, writer, num);
            }
            for (int i = 0; i < numThreads; i++)
            {
                threads[i].Start();
            }
            for (int i = 0; i < numThreads; i++)
            {
                threads[i].Join();
            }

            writer.ForceMerge(1);
            Assert.AreEqual(ramDir.GetSizeInBytes(), ramDir.GetRecomputedSizeInBytes());

            writer.Dispose();
        }

        private sealed class ThreadAnonymousClass : ThreadJob
        {
            private readonly TestRAMDirectory outerInstance;

            private readonly IndexWriter writer;
            private readonly int num;

            public ThreadAnonymousClass(TestRAMDirectory outerInstance, IndexWriter writer, int num)
            {
                this.outerInstance = outerInstance;
                this.writer = writer;
                this.num = num;
            }

            public override void Run()
            {
                for (int j = 1; j < outerInstance.docsPerThread; j++)
                {
                    Document doc = new Document();
                    doc.Add(NewStringField("sizeContent", English.Int32ToEnglish(num * outerInstance.docsPerThread + j).Trim(), Field.Store.YES));
                    try
                    {
                        writer.AddDocument(doc);
                    }
                    catch (Exception e) when (e.IsIOException())
                    {
                        throw RuntimeException.Create(e);
                    }
                }
            }
        }

        [TearDown]
        public override void TearDown()
        {
            // cleanup
            if (indexDir != null && indexDir.Exists)
            {
                RmDir(indexDir);
            }
            base.TearDown();
        }

        // LUCENE-1196
        [Test]
        public virtual void TestIllegalEOF()
        {
            RAMDirectory dir = new RAMDirectory();
            IndexOutput o = dir.CreateOutput("out", NewIOContext(Random));
            var b = new byte[1024];
            o.WriteBytes(b, 0, 1024);
            o.Dispose();
            IndexInput i = dir.OpenInput("out", NewIOContext(Random));
            i.Seek(1024);
            i.Dispose();
            dir.Dispose();
        }

        private void RmDir(DirectoryInfo dir)
        {
            FileInfo[] files = dir.GetFiles();
            List<FileInfo> retryFiles = null;
            for (int i = 0; i < files.Length; i++)
            {
                try
                {
                    files[i].Delete();
                }
                catch (IOException)
                {
                    // LUCENENET specific - we can get here if Windows stil has a lock on the file. We will put it into a list to retry.
                    if (retryFiles is null) retryFiles = new List<FileInfo>();
                    retryFiles.Add(files[i]);
                }
            }
            // LUCENENET specific - retry the deletion if it failed on the first pass
            if (retryFiles is not null)
            {
                // Second pass - if this attempt doesn't work, just give up.
                foreach (var file in retryFiles)
                {
                    try
                    {
                        file.Delete();
                    }
                    catch { /* ignore */ }
                }
                if (retryFiles.Count == 0)
                {
                    dir.Delete();
                }
                return;
            }
            dir.Delete();
        }

        // LUCENE-2852
        [Test]
        public virtual void TestSeekToEOFThenBack()
        {
            RAMDirectory dir = new RAMDirectory();

            IndexOutput o = dir.CreateOutput("out", NewIOContext(Random));
            var bytes = new byte[3 * RAMInputStream.BUFFER_SIZE];
            o.WriteBytes(bytes, 0, bytes.Length);
            o.Dispose();

            IndexInput i = dir.OpenInput("out", NewIOContext(Random));
            i.Seek(2 * RAMInputStream.BUFFER_SIZE - 1);
            i.Seek(3 * RAMInputStream.BUFFER_SIZE);
            i.Seek(RAMInputStream.BUFFER_SIZE);
            i.ReadBytes(bytes, 0, 2 * RAMInputStream.BUFFER_SIZE);
            i.Dispose();
            dir.Dispose();
        }
    }
}