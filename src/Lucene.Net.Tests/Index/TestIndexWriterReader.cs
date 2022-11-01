using J2N.Threading;
using J2N.Threading.Atomic;
using Lucene.Net.Attributes;
using Lucene.Net.Documents;
using Lucene.Net.Index.Extensions;
using Lucene.Net.Store;
using Lucene.Net.Support.Threading;
using Lucene.Net.Util;
using NUnit.Framework;
using RandomizedTesting.Generators;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using JCG = J2N.Collections.Generic;
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

    using BytesRef = Lucene.Net.Util.BytesRef;
    using Codec = Lucene.Net.Codecs.Codec;
    using Directory = Lucene.Net.Store.Directory;
    using DocIdSetIterator = Lucene.Net.Search.DocIdSetIterator;
    using Document = Documents.Document;
    using FakeIOException = Lucene.Net.Store.FakeIOException;
    using Field = Field;
    using IndexSearcher = Lucene.Net.Search.IndexSearcher;
    using InfoStream = Lucene.Net.Util.InfoStream;
    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
    using MockAnalyzer = Lucene.Net.Analysis.MockAnalyzer;
    using MockDirectoryWrapper = Lucene.Net.Store.MockDirectoryWrapper;
    using Query = Lucene.Net.Search.Query;
    using RAMDirectory = Lucene.Net.Store.RAMDirectory;
    using TermQuery = Lucene.Net.Search.TermQuery;
    using TestUtil = Lucene.Net.Util.TestUtil;
    using TextField = TextField;
    using TopDocs = Lucene.Net.Search.TopDocs;

    [TestFixture]
    public class TestIndexWriterReader : LuceneTestCase
    {
        private readonly int numThreads = TestNightly ? 5 : 3;

        public static int Count(Term t, IndexReader r)
        {
            int count = 0;
            DocsEnum td = TestUtil.Docs(Random, r, t.Field, new BytesRef(t.Text), MultiFields.GetLiveDocs(r), null, 0);

            if (td != null)
            {
                while (td.NextDoc() != DocIdSetIterator.NO_MORE_DOCS)
                {
                    var _ = td.DocID;
                    count++;
                }
            }
            return count;
        }

#if FEATURE_INDEXWRITER_TESTS

        [Test]
        public virtual void TestAddCloseOpen()
        {
            // Can't use assertNoDeletes: this test pulls a non-NRT
            // reader in the end:
            Directory dir1 = NewDirectory();
            IndexWriterConfig iwc = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random));

            IndexWriter writer = new IndexWriter(dir1, iwc);
            for (int i = 0; i < 97; i++)
            {
                DirectoryReader reader = writer.GetReader();
                if (i == 0)
                {
                    writer.AddDocument(DocHelper.CreateDocument(i, "x", 1 + Random.Next(5)));
                }
                else
                {
                    int previous = Random.Next(i);
                    // a check if the reader is current here could fail since there might be
                    // merges going on.
                    switch (Random.Next(5))
                    {
                        case 0:
                        case 1:
                        case 2:
                            writer.AddDocument(DocHelper.CreateDocument(i, "x", 1 + Random.Next(5)));
                            break;

                        case 3:
                            writer.UpdateDocument(new Term("id", "" + previous), DocHelper.CreateDocument(previous, "x", 1 + Random.Next(5)));
                            break;

                        case 4:
                            writer.DeleteDocuments(new Term("id", "" + previous));
                            break;
                    }
                }
                Assert.IsFalse(reader.IsCurrent());
                reader.Dispose();
            }
            writer.ForceMerge(1); // make sure all merging is done etc.
            DirectoryReader dirReader = writer.GetReader();
            writer.Commit(); // no changes that are not visible to the reader
            Assert.IsTrue(dirReader.IsCurrent());
            writer.Dispose();
            Assert.IsTrue(dirReader.IsCurrent()); // all changes are visible to the reader
            iwc = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random));
            writer = new IndexWriter(dir1, iwc);
            Assert.IsTrue(dirReader.IsCurrent());
            writer.AddDocument(DocHelper.CreateDocument(1, "x", 1 + Random.Next(5)));
            Assert.IsTrue(dirReader.IsCurrent()); // segments in ram but IW is different to the readers one
            writer.Dispose();
            Assert.IsFalse(dirReader.IsCurrent()); // segments written
            dirReader.Dispose();
            dir1.Dispose();
        }

        [Test]
        public virtual void TestUpdateDocument()
        {
            bool doFullMerge = true;

            Directory dir1 = NewDirectory();
            IndexWriterConfig iwc = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random));
            if (iwc.MaxBufferedDocs < 20)
            {
                iwc.SetMaxBufferedDocs(20);
            }
            // no merging
            if (Random.NextBoolean())
            {
                iwc.SetMergePolicy(NoMergePolicy.NO_COMPOUND_FILES);
            }
            else
            {
                iwc.SetMergePolicy(NoMergePolicy.COMPOUND_FILES);
            }
            if (Verbose)
            {
                Console.WriteLine("TEST: make index");
            }
            IndexWriter writer = new IndexWriter(dir1, iwc);

            // create the index
            CreateIndexNoClose(!doFullMerge, "index1", writer);

            // writer.Flush(false, true, true);

            // get a reader
            DirectoryReader r1 = writer.GetReader();
            Assert.IsTrue(r1.IsCurrent());

            string id10 = r1.Document(10).GetField("id").GetStringValue();

            Document newDoc = r1.Document(10);
            newDoc.RemoveField("id");
            newDoc.Add(NewStringField("id", Convert.ToString(8000), Field.Store.YES));
            writer.UpdateDocument(new Term("id", id10), newDoc);
            Assert.IsFalse(r1.IsCurrent());

            DirectoryReader r2 = writer.GetReader();
            Assert.IsTrue(r2.IsCurrent());
            Assert.AreEqual(0, Count(new Term("id", id10), r2));
            if (Verbose)
            {
                Console.WriteLine("TEST: verify id");
            }
            Assert.AreEqual(1, Count(new Term("id", Convert.ToString(8000)), r2));

            r1.Dispose();
            Assert.IsTrue(r2.IsCurrent());
            writer.Dispose();
            Assert.IsTrue(r2.IsCurrent());

            DirectoryReader r3 = DirectoryReader.Open(dir1);
            Assert.IsTrue(r3.IsCurrent());
            Assert.IsTrue(r2.IsCurrent());
            Assert.AreEqual(0, Count(new Term("id", id10), r3));
            Assert.AreEqual(1, Count(new Term("id", Convert.ToString(8000)), r3));

            writer = new IndexWriter(dir1, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)));
            Document doc = new Document();
            doc.Add(NewTextField("field", "a b c", Field.Store.NO));
            writer.AddDocument(doc);
            Assert.IsTrue(r2.IsCurrent());
            Assert.IsTrue(r3.IsCurrent());

            writer.Dispose();

            Assert.IsFalse(r2.IsCurrent());
            Assert.IsTrue(!r3.IsCurrent());

            r2.Dispose();
            r3.Dispose();

            dir1.Dispose();
        }

        [Test]
        public virtual void TestIsCurrent()
        {
            Directory dir = NewDirectory();
            IndexWriterConfig iwc = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random));

            IndexWriter writer = new IndexWriter(dir, iwc);
            Document doc = new Document();
            doc.Add(NewTextField("field", "a b c", Field.Store.NO));
            writer.AddDocument(doc);
            writer.Dispose();

            iwc = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random));
            writer = new IndexWriter(dir, iwc);
            doc = new Document();
            doc.Add(NewTextField("field", "a b c", Field.Store.NO));
            DirectoryReader nrtReader = writer.GetReader();
            Assert.IsTrue(nrtReader.IsCurrent());
            writer.AddDocument(doc);
            Assert.IsFalse(nrtReader.IsCurrent()); // should see the changes
            writer.ForceMerge(1); // make sure we don't have a merge going on
            Assert.IsFalse(nrtReader.IsCurrent());
            nrtReader.Dispose();

            DirectoryReader dirReader = DirectoryReader.Open(dir);
            nrtReader = writer.GetReader();

            Assert.IsTrue(dirReader.IsCurrent());
            Assert.IsTrue(nrtReader.IsCurrent()); // nothing was committed yet so we are still current
            Assert.AreEqual(2, nrtReader.MaxDoc); // sees the actual document added
            Assert.AreEqual(1, dirReader.MaxDoc);
            writer.Dispose(); // close is actually a commit both should see the changes
            Assert.IsTrue(nrtReader.IsCurrent());
            Assert.IsFalse(dirReader.IsCurrent()); // this reader has been opened before the writer was closed / committed

            dirReader.Dispose();
            nrtReader.Dispose();
            dir.Dispose();
        }

        /// <summary>
        /// Test using IW.addIndexes
        /// </summary>
        [Test]
        public virtual void TestAddIndexes()
        {
            bool doFullMerge = false;

            Directory dir1 = GetAssertNoDeletesDirectory(NewDirectory());
            IndexWriterConfig iwc = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random));
            if (iwc.MaxBufferedDocs < 20)
            {
                iwc.SetMaxBufferedDocs(20);
            }
            // no merging
            if (Random.NextBoolean())
            {
                iwc.SetMergePolicy(NoMergePolicy.NO_COMPOUND_FILES);
            }
            else
            {
                iwc.SetMergePolicy(NoMergePolicy.COMPOUND_FILES);
            }
            IndexWriter writer = new IndexWriter(dir1, iwc);

            // create the index
            CreateIndexNoClose(!doFullMerge, "index1", writer);
            writer.Flush(false, true);

            // create a 2nd index
            Directory dir2 = NewDirectory();
            IndexWriter writer2 = new IndexWriter(dir2, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)));
            CreateIndexNoClose(!doFullMerge, "index2", writer2);
            writer2.Dispose();

            DirectoryReader r0 = writer.GetReader();
            Assert.IsTrue(r0.IsCurrent());
            writer.AddIndexes(dir2);
            Assert.IsFalse(r0.IsCurrent());
            r0.Dispose();

            DirectoryReader r1 = writer.GetReader();
            Assert.IsTrue(r1.IsCurrent());

            writer.Commit();
            Assert.IsTrue(r1.IsCurrent()); // we have seen all changes - no change after opening the NRT reader

            Assert.AreEqual(200, r1.MaxDoc);

            int index2df = r1.DocFreq(new Term("indexname", "index2"));

            Assert.AreEqual(100, index2df);

            // verify the docs are from different indexes
            Document doc5 = r1.Document(5);
            Assert.AreEqual("index1", doc5.Get("indexname"));
            Document doc150 = r1.Document(150);
            Assert.AreEqual("index2", doc150.Get("indexname"));
            r1.Dispose();
            writer.Dispose();
            dir1.Dispose();
            dir2.Dispose();
        }

        [Test]
        public virtual void ExposeCompTermVR()
        {
            bool doFullMerge = false;
            Directory dir1 = GetAssertNoDeletesDirectory(NewDirectory());
            IndexWriterConfig iwc = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random));
            if (iwc.MaxBufferedDocs < 20)
            {
                iwc.SetMaxBufferedDocs(20);
            }
            // no merging
            if (Random.NextBoolean())
            {
                iwc.SetMergePolicy(NoMergePolicy.NO_COMPOUND_FILES);
            }
            else
            {
                iwc.SetMergePolicy(NoMergePolicy.COMPOUND_FILES);
            }
            IndexWriter writer = new IndexWriter(dir1, iwc);
            CreateIndexNoClose(!doFullMerge, "index1", writer);
            writer.Dispose();
            dir1.Dispose();
        }

        [Test]
        public virtual void TestAddIndexes2()
        {
            bool doFullMerge = false;

            Directory dir1 = GetAssertNoDeletesDirectory(NewDirectory());
            IndexWriter writer = new IndexWriter(dir1, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)));

            // create a 2nd index
            Directory dir2 = NewDirectory();
            IndexWriter writer2 = new IndexWriter(dir2, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)));
            CreateIndexNoClose(!doFullMerge, "index2", writer2);
            writer2.Dispose();

            writer.AddIndexes(dir2);
            writer.AddIndexes(dir2);
            writer.AddIndexes(dir2);
            writer.AddIndexes(dir2);
            writer.AddIndexes(dir2);

            IndexReader r1 = writer.GetReader();
            Assert.AreEqual(500, r1.MaxDoc);

            r1.Dispose();
            writer.Dispose();
            dir1.Dispose();
            dir2.Dispose();
        }

        /// <summary>
        /// Deletes using IW.deleteDocuments
        /// </summary>
        [Test]
        public virtual void TestDeleteFromIndexWriter()
        {
            bool doFullMerge = true;

            Directory dir1 = GetAssertNoDeletesDirectory(NewDirectory());
            IndexWriter writer = new IndexWriter(dir1, (IndexWriterConfig)NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)).SetReaderTermsIndexDivisor(2));
            // create the index
            CreateIndexNoClose(!doFullMerge, "index1", writer);
            writer.Flush(false, true);
            // get a reader
            IndexReader r1 = writer.GetReader();

            string id10 = r1.Document(10).GetField("id").GetStringValue();

            // deleted IW docs should not show up in the next getReader
            writer.DeleteDocuments(new Term("id", id10));
            IndexReader r2 = writer.GetReader();
            Assert.AreEqual(1, Count(new Term("id", id10), r1));
            Assert.AreEqual(0, Count(new Term("id", id10), r2));

            string id50 = r1.Document(50).GetField("id").GetStringValue();
            Assert.AreEqual(1, Count(new Term("id", id50), r1));

            writer.DeleteDocuments(new Term("id", id50));

            IndexReader r3 = writer.GetReader();
            Assert.AreEqual(0, Count(new Term("id", id10), r3));
            Assert.AreEqual(0, Count(new Term("id", id50), r3));

            string id75 = r1.Document(75).GetField("id").GetStringValue();
            writer.DeleteDocuments(new TermQuery(new Term("id", id75)));
            IndexReader r4 = writer.GetReader();
            Assert.AreEqual(1, Count(new Term("id", id75), r3));
            Assert.AreEqual(0, Count(new Term("id", id75), r4));

            r1.Dispose();
            r2.Dispose();
            r3.Dispose();
            r4.Dispose();
            writer.Dispose();

            // reopen the writer to verify the delete made it to the directory
            writer = new IndexWriter(dir1, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)));
            IndexReader w2r1 = writer.GetReader();
            Assert.AreEqual(0, Count(new Term("id", id10), w2r1));
            w2r1.Dispose();
            writer.Dispose();
            dir1.Dispose();
        }

        [Test]
        [Slow]
        public virtual void TestAddIndexesAndDoDeletesThreads()
        {
            const int numIter = 2;
            int numDirs = 3;

            Directory mainDir = GetAssertNoDeletesDirectory(NewDirectory());

            IndexWriter mainWriter = new IndexWriter(mainDir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)).SetMergePolicy(NewLogMergePolicy()));
            TestUtil.ReduceOpenFiles(mainWriter);

            AddDirectoriesThreads addDirThreads = new AddDirectoriesThreads(this, numIter, mainWriter);
            addDirThreads.LaunchThreads(numDirs);
            addDirThreads.JoinThreads();

            //Assert.AreEqual(100 + numDirs * (3 * numIter / 4) * addDirThreads.numThreads
            //    * addDirThreads.NUM_INIT_DOCS, addDirThreads.mainWriter.NumDocs);
            Assert.AreEqual(addDirThreads.count, addDirThreads.mainWriter.NumDocs);

            addDirThreads.Close(true);

            Assert.IsTrue(addDirThreads.failures.Count == 0);

            TestUtil.CheckIndex(mainDir);

            IndexReader reader = DirectoryReader.Open(mainDir);
            Assert.AreEqual(addDirThreads.count, reader.NumDocs);
            //Assert.AreEqual(100 + numDirs * (3 * numIter / 4) * addDirThreads.numThreads
            //    * addDirThreads.NUM_INIT_DOCS, reader.NumDocs);
            reader.Dispose();

            addDirThreads.CloseDir();
            mainDir.Dispose();
        }

        private class AddDirectoriesThreads
        {
            private readonly TestIndexWriterReader outerInstance;

            internal Directory addDir;
            internal const int NUM_INIT_DOCS = 100;
            internal int numDirs;
            internal ThreadJob[] threads;
            internal IndexWriter mainWriter;
            internal readonly IList<Exception> failures = new JCG.List<Exception>();
            internal IndexReader[] readers;
            internal bool didClose = false;
            internal AtomicInt32 count = new AtomicInt32(0);
            internal AtomicInt32 numaddIndexes = new AtomicInt32(0);

            public AddDirectoriesThreads(TestIndexWriterReader outerInstance, int numDirs, IndexWriter mainWriter)
            {
                this.outerInstance = outerInstance;
                threads = new ThreadJob[outerInstance.numThreads];
                this.numDirs = numDirs;
                this.mainWriter = mainWriter;
                addDir = NewDirectory();
                IndexWriter writer = new IndexWriter(addDir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)).SetMaxBufferedDocs(2));
                TestUtil.ReduceOpenFiles(writer);
                for (int i = 0; i < NUM_INIT_DOCS; i++)
                {
                    Document doc = DocHelper.CreateDocument(i, "addindex", 4);
                    writer.AddDocument(doc);
                }

                writer.Dispose();

                readers = new IndexReader[numDirs];
                for (int i = 0; i < numDirs; i++)
                {
                    readers[i] = DirectoryReader.Open(addDir);
                }
            }

            internal virtual void JoinThreads()
            {
                for (int i = 0; i < outerInstance.numThreads; i++)
                {
                    try
                    {
                        threads[i].Join();
                    }
                    catch (Exception ie) when (ie.IsInterruptedException())
                    {
#pragma warning disable IDE0001 // Simplify name
                        throw new Util.ThreadInterruptedException(ie);
#pragma warning restore IDE0001 // Simplify name
                    }
                }
            }


            internal virtual void Close(bool doWait)
            {
                didClose = true;
                if (doWait)
                {
                    mainWriter.WaitForMerges();
                }
                mainWriter.Dispose(doWait);
            }

            internal virtual void CloseDir()
            {
                for (int i = 0; i < numDirs; i++)
                {
                    readers[i].Dispose();
                }
                addDir.Dispose();
            }

            internal virtual void Handle(Exception t)
            {
                Console.WriteLine(t.StackTrace);
                UninterruptableMonitor.Enter(failures);
                try
                {
                    failures.Add(t);
                }
                finally
                {
                    UninterruptableMonitor.Exit(failures);
                }
            }

            internal virtual void LaunchThreads(int numIter)
            {
                for (int i = 0; i < outerInstance.numThreads; i++)
                {
                    threads[i] = new ThreadAnonymousClass(this, numIter);
                }
                for (int i = 0; i < outerInstance.numThreads; i++)
                {
                    threads[i].Start();
                }
            }

            private sealed class ThreadAnonymousClass : ThreadJob
            {
                private readonly AddDirectoriesThreads outerInstance;

                private readonly int numIter;

                public ThreadAnonymousClass(AddDirectoriesThreads outerInstance, int numIter)
                {
                    this.outerInstance = outerInstance;
                    this.numIter = numIter;
                }

                public override void Run()
                {
                    try
                    {
                        Directory[] dirs = new Directory[outerInstance.numDirs];
                        for (int k = 0; k < outerInstance.numDirs; k++)
                        {
                            dirs[k] = new MockDirectoryWrapper(Random, new RAMDirectory(outerInstance.addDir, NewIOContext(Random)));
                        }
                        //int j = 0;
                        //while (true) {
                        // System.out.println(Thread.currentThread().getName() + ": iter
                        // j=" + j);
                        for (int x = 0; x < numIter; x++)
                        {
                            // only do addIndexes
                            outerInstance.DoBody(x, dirs);
                        }
                        //if (numIter > 0 && j == numIter)
                        //  break;
                        //doBody(j++, dirs);
                        //doBody(5, dirs);
                        //}
                    }
                    catch (Exception t) when (t.IsThrowable())
                    {
                        outerInstance.Handle(t);
                    }
                }
            }

            internal virtual void DoBody(int j, Directory[] dirs)
            {
                switch (j % 4)
                {
                    case 0:
                        mainWriter.AddIndexes(dirs);
                        mainWriter.ForceMerge(1);
                        break;

                    case 1:
                        mainWriter.AddIndexes(dirs);
                        numaddIndexes.IncrementAndGet();
                        break;

                    case 2:
                        mainWriter.AddIndexes(readers);
                        break;

                    case 3:
                        mainWriter.Commit();
                        break;
                }
                count.AddAndGet(dirs.Length * NUM_INIT_DOCS);
            }
        }

        [Test]
        public virtual void TestIndexWriterReopenSegmentFullMerge()
        {
            DoTestIndexWriterReopenSegment(true);
        }

        [Test]
        public virtual void TestIndexWriterReopenSegment()
        {
            DoTestIndexWriterReopenSegment(false);
        }

        /// <summary>
        /// Tests creating a segment, then check to insure the segment can be seen via
        /// IW.getReader
        /// </summary>
        public virtual void DoTestIndexWriterReopenSegment(bool doFullMerge)
        {
            Directory dir1 = GetAssertNoDeletesDirectory(NewDirectory());
            IndexWriter writer = new IndexWriter(dir1, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)));
            IndexReader r1 = writer.GetReader();
            Assert.AreEqual(0, r1.MaxDoc);
            CreateIndexNoClose(false, "index1", writer);
            writer.Flush(!doFullMerge, true);

            IndexReader iwr1 = writer.GetReader();
            Assert.AreEqual(100, iwr1.MaxDoc);

            IndexReader r2 = writer.GetReader();
            Assert.AreEqual(r2.MaxDoc, 100);
            // add 100 documents
            for (int x = 10000; x < 10000 + 100; x++)
            {
                Document d = DocHelper.CreateDocument(x, "index1", 5);
                writer.AddDocument(d);
            }
            writer.Flush(false, true);
            // verify the reader was reopened internally
            IndexReader iwr2 = writer.GetReader();
            Assert.IsTrue(iwr2 != r1);
            Assert.AreEqual(200, iwr2.MaxDoc);
            // should have flushed out a segment
            IndexReader r3 = writer.GetReader();
            Assert.IsTrue(r2 != r3);
            Assert.AreEqual(200, r3.MaxDoc);

            // dec ref the readers rather than close them because
            // closing flushes changes to the writer
            r1.Dispose();
            iwr1.Dispose();
            r2.Dispose();
            r3.Dispose();
            iwr2.Dispose();
            writer.Dispose();

            // test whether the changes made it to the directory
            writer = new IndexWriter(dir1, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)));
            IndexReader w2r1 = writer.GetReader();
            // insure the deletes were actually flushed to the directory
            Assert.AreEqual(200, w2r1.MaxDoc);
            w2r1.Dispose();
            writer.Dispose();

            dir1.Dispose();
        }

#endif

        /*
        * Delete a document by term and return the doc id
        *
        * public static int deleteDocument(Term term, IndexWriter writer) throws
        * IOException { IndexReader reader = writer.GetReader(); TermDocs td =
        * reader.termDocs(term); int doc = -1; //if (td.Next()) { // doc = td.Doc();
        * //} //writer.DeleteDocuments(term); td.Dispose(); return doc; }
        */

        public void CreateIndex(Random random, Directory dir1, string indexName, bool multiSegment)
        {
            IndexWriter w = new IndexWriter(dir1, NewIndexWriterConfig(random, TEST_VERSION_CURRENT, new MockAnalyzer(random)).SetMergePolicy(new LogDocMergePolicy()));
            for (int i = 0; i < 100; i++)
            {
                w.AddDocument(DocHelper.CreateDocument(i, indexName, 4));
            }
            if (!multiSegment)
            {
                w.ForceMerge(1);
            }
            w.Dispose();
        }

        public static void CreateIndexNoClose(bool multiSegment, string indexName, IndexWriter w)
        {
            for (int i = 0; i < 100; i++)
            {
                w.AddDocument(DocHelper.CreateDocument(i, indexName, 4));
            }
            if (!multiSegment)
            {
                w.ForceMerge(1);
            }
        }

#if FEATURE_INDEXWRITER_TESTS

        private class MyWarmer : IndexWriter.IndexReaderWarmer
        {
            internal int warmCount;

            public override void Warm(AtomicReader reader)
            {
                warmCount++;
            }
        }

        [Test]
        [Slow]
        public virtual void TestMergeWarmer()
        {
            Directory dir1 = GetAssertNoDeletesDirectory(NewDirectory());
            // Enroll warmer
            MyWarmer warmer = new MyWarmer();
            var config = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random))
                            .SetMaxBufferedDocs(2)
                            .SetMergedSegmentWarmer(warmer)
                            .SetMergeScheduler(new ConcurrentMergeScheduler())
                            .SetMergePolicy(NewLogMergePolicy());
            IndexWriter writer = new IndexWriter(dir1, config);

            // create the index
            CreateIndexNoClose(false, "test", writer);

            // get a reader to put writer into near real-time mode
            IndexReader r1 = writer.GetReader();

            ((LogMergePolicy)writer.Config.MergePolicy).MergeFactor = 2;

            //int num = AtLeast(100);
            int num = 101;
            for (int i = 0; i < num; i++)
            {
                writer.AddDocument(DocHelper.CreateDocument(i, "test", 4));
            }
            ((IConcurrentMergeScheduler)writer.Config.MergeScheduler).Sync();

            Assert.IsTrue(warmer.warmCount > 0);
            Console.WriteLine("Count {0}", warmer.warmCount);
            int count = warmer.warmCount;

            var newDocument = DocHelper.CreateDocument(17, "test", 4);
            writer.AddDocument(newDocument);
            writer.ForceMerge(1);
            Assert.IsTrue(warmer.warmCount > count);

            writer.Dispose();
            r1.Dispose();
            dir1.Dispose();
        }

        [Test]
        public virtual void TestAfterCommit()
        {
            Directory dir1 = GetAssertNoDeletesDirectory(NewDirectory());
            var config = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)).SetMergeScheduler(new ConcurrentMergeScheduler());
            IndexWriter writer = new IndexWriter(dir1, config);
            writer.Commit();

            // create the index
            CreateIndexNoClose(false, "test", writer);

            // get a reader to put writer into near real-time mode
            DirectoryReader r1 = writer.GetReader();
            TestUtil.CheckIndex(dir1);
            writer.Commit();
            TestUtil.CheckIndex(dir1);
            Assert.AreEqual(100, r1.NumDocs);

            for (int i = 0; i < 10; i++)
            {
                writer.AddDocument(DocHelper.CreateDocument(i, "test", 4));
            }
            ((IConcurrentMergeScheduler)writer.Config.MergeScheduler).Sync();

            DirectoryReader r2 = DirectoryReader.OpenIfChanged(r1);
            if (r2 != null)
            {
                r1.Dispose();
                r1 = r2;
            }
            Assert.AreEqual(110, r1.NumDocs);
            writer.Dispose();
            r1.Dispose();
            dir1.Dispose();
        }

        // Make sure reader remains usable even if IndexWriter closes
        [Test]
        public virtual void TestAfterClose()
        {
            Directory dir1 = GetAssertNoDeletesDirectory(NewDirectory());
            IndexWriter writer = new IndexWriter(dir1, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)));

            // create the index
            CreateIndexNoClose(false, "test", writer);

            DirectoryReader r = writer.GetReader();
            writer.Dispose();

            TestUtil.CheckIndex(dir1);

            // reader should remain usable even after IndexWriter is closed:
            Assert.AreEqual(100, r.NumDocs);
            Query q = new TermQuery(new Term("indexname", "test"));
            IndexSearcher searcher = NewSearcher(r);
            Assert.AreEqual(100, searcher.Search(q, 10).TotalHits);
            try
            {
                DirectoryReader.OpenIfChanged(r);
                Assert.Fail("failed to hit ObjectDisposedException");
            }
            catch (Exception ace) when (ace.IsAlreadyClosedException())
            {
                // expected
            }
            r.Dispose();
            dir1.Dispose();
        }

        // Stress test reopen during addIndexes
        [Test]
        [Slow]
        public virtual void TestDuringAddIndexes()
        {
            // LUCENENET specific - log the current locking strategy used and HResult values
            // for assistance troubleshooting problems on Linux/macOS
            LogNativeFSFactoryDebugInfo();

            Directory dir1 = GetAssertNoDeletesDirectory(NewDirectory());
            IndexWriter writer = new IndexWriter(
                dir1, 
                NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random))
                    .SetMergePolicy(NewLogMergePolicy(2)));

            // create the index
            CreateIndexNoClose(false, "test", writer);
            writer.Commit();

            Directory[] dirs = new Directory[10];
            for (int i = 0; i < 10; i++)
            {
                dirs[i] = new MockDirectoryWrapper(Random, new RAMDirectory(dir1, NewIOContext(Random)));
            }

            DirectoryReader r = writer.GetReader();

            const float SECONDS = 0.5f;

            long endTime = (long)((J2N.Time.NanoTime() / J2N.Time.MillisecondsPerNanosecond) + 1000.0 * SECONDS); // LUCENENET: Use NanoTime() rather than CurrentTimeMilliseconds() for more accurate/reliable results
            ConcurrentQueue<Exception> excs = new ConcurrentQueue<Exception>();

            // Only one thread can addIndexes at a time, because
            // IndexWriter acquires a write lock in each directory:
            var threads = new ThreadJob[1];
            for (int i = 0; i < threads.Length; i++)
            {
                threads[i] = new ThreadAnonymousClass(writer, dirs, endTime, excs);
                threads[i].IsBackground = (true);
                threads[i].Start();
            }

            int lastCount = 0;
            while (J2N.Time.NanoTime() / J2N.Time.MillisecondsPerNanosecond < endTime) // LUCENENET: Use NanoTime() rather than CurrentTimeMilliseconds() for more accurate/reliable results
            {
                DirectoryReader r2 = DirectoryReader.OpenIfChanged(r);
                if (r2 != null)
                {
                    r.Dispose();
                    r = r2;
                }
                Query q = new TermQuery(new Term("indexname", "test"));
                IndexSearcher searcher = NewSearcher(r);
                int count = searcher.Search(q, 10).TotalHits;
                Assert.IsTrue(count >= lastCount);
                lastCount = count;
            }

            for (int i = 0; i < threads.Length; i++)
            {
                threads[i].Join();
            }
            // final check
            DirectoryReader dr2 = DirectoryReader.OpenIfChanged(r);
            if (dr2 != null)
            {
                r.Dispose();
                r = dr2;
            }
            Query q2 = new TermQuery(new Term("indexname", "test"));
            IndexSearcher searcher_ = NewSearcher(r);
            int count_ = searcher_.Search(q2, 10).TotalHits;
            Assert.IsTrue(count_ >= lastCount);

            Assert.AreEqual(0, excs.Count);
            r.Dispose();
            if (dir1 is MockDirectoryWrapper)
            {
                ICollection<string> openDeletedFiles = ((MockDirectoryWrapper)dir1).GetOpenDeletedFiles();
                Assert.AreEqual(0, openDeletedFiles.Count, "openDeleted=" + openDeletedFiles);
            }

            writer.Dispose();

            dir1.Dispose();
        }

        private sealed class ThreadAnonymousClass : ThreadJob
        {
            private readonly IndexWriter writer;
            private readonly Directory[] dirs;
            private readonly long endTime;
            private readonly ConcurrentQueue<Exception> excs;

            public ThreadAnonymousClass(IndexWriter writer, Directory[] dirs, long endTime, ConcurrentQueue<Exception> excs)
            {
                this.writer = writer;
                this.dirs = dirs;
                this.endTime = endTime;
                this.excs = excs;
            }

            public override void Run()
            {
                do
                {
                    try
                    {
                        writer.AddIndexes(dirs);
                        writer.MaybeMerge();
                    }
                    catch (Exception t) when (t.IsThrowable())
                    {
                        excs.Enqueue(t);
                        throw RuntimeException.Create(t);
                    }
                } while (J2N.Time.NanoTime() / J2N.Time.MillisecondsPerNanosecond < endTime); // LUCENENET: Use NanoTime() rather than CurrentTimeMilliseconds() for more accurate/reliable results
            }
        }

        private Directory GetAssertNoDeletesDirectory(Directory directory)
        {
            if (directory is MockDirectoryWrapper)
            {
                ((MockDirectoryWrapper)directory).AssertNoDeleteOpenFile = true;
            }
            return directory;
        }

        // Stress test reopen during add/delete
        [Test]
        [Slow]
        public virtual void TestDuringAddDelete()
        {
            Directory dir1 = NewDirectory();
            var writer = new IndexWriter(dir1, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)).SetMergePolicy(NewLogMergePolicy(2)));

            // create the index
            CreateIndexNoClose(false, "test", writer);
            writer.Commit();

            DirectoryReader r = writer.GetReader();

            const float SECONDS = 0.5f;

            long endTime = (long)((J2N.Time.NanoTime() / J2N.Time.MillisecondsPerNanosecond) + 1000.0 * SECONDS); // LUCENENET: Use NanoTime() rather than CurrentTimeMilliseconds() for more accurate/reliable results
            ConcurrentQueue<Exception> excs = new ConcurrentQueue<Exception>();

            var threads = new ThreadJob[numThreads];
            for (int i = 0; i < numThreads; i++)
            {
                threads[i] = new ThreadAnonymousClass2(writer, endTime, excs);
                threads[i].IsBackground = (true);
                threads[i].Start();
            }

            int sum = 0;
            while (J2N.Time.NanoTime() / J2N.Time.MillisecondsPerNanosecond < endTime) // LUCENENET: Use NanoTime() rather than CurrentTimeMilliseconds() for more accurate/reliable results
            {
                DirectoryReader r2 = DirectoryReader.OpenIfChanged(r);
                if (r2 != null)
                {
                    r.Dispose();
                    r = r2;
                }
                Query q = new TermQuery(new Term("indexname", "test"));
                IndexSearcher searcher = NewSearcher(r);
                sum += searcher.Search(q, 10).TotalHits;
            }

            for (int i = 0; i < numThreads; i++)
            {
                threads[i].Join();
            }
            // at least search once
            DirectoryReader dr2 = DirectoryReader.OpenIfChanged(r);
            if (dr2 != null)
            {
                r.Dispose();
                r = dr2;
            }
            Query q2 = new TermQuery(new Term("indexname", "test"));
            IndexSearcher indSearcher = NewSearcher(r);
            sum += indSearcher.Search(q2, 10).TotalHits;
            Assert.IsTrue(sum > 0, "no documents found at all");

            Assert.AreEqual(0, excs.Count);
            writer.Dispose();

            r.Dispose();
            dir1.Dispose();
        }

        private sealed class ThreadAnonymousClass2 : ThreadJob
        {
            private readonly IndexWriter writer;
            private readonly long endTime;
            private readonly ConcurrentQueue<Exception> excs;

            public ThreadAnonymousClass2(IndexWriter writer, long endTime, ConcurrentQueue<Exception> excs)
            {
                this.writer = writer;
                this.endTime = endTime;
                this.excs = excs;
                rand = new J2N.Randomizer(Random.NextInt64());
            }

            internal readonly Random rand;

            public override void Run()
            {
                int count = 0;
                do
                {
                    try
                    {
                        for (int docUpto = 0; docUpto < 10; docUpto++)
                        {
                            writer.AddDocument(DocHelper.CreateDocument(10 * count + docUpto, "test", 4));
                        }
                        count++;
                        int limit = count * 10;
                        for (int delUpto = 0; delUpto < 5; delUpto++)
                        {
                            int x = rand.Next(limit);
                            writer.DeleteDocuments(new Term("field3", "b" + x));
                        }
                    }
                    catch (Exception t) when (t.IsThrowable())
                    {
                        excs.Enqueue(t);
                        throw RuntimeException.Create(t);
                    }
                } while (J2N.Time.NanoTime() / J2N.Time.MillisecondsPerNanosecond < endTime); // LUCENENET: Use NanoTime() rather than CurrentTimeMilliseconds() for more accurate/reliable results
            }
        }

        [Test]
        public virtual void TestForceMergeDeletes()
        {
            Directory dir = NewDirectory();
            IndexWriter w = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)).SetMergePolicy(NewLogMergePolicy()));
            Document doc = new Document();
            doc.Add(NewTextField("field", "a b c", Field.Store.NO));
            Field id = NewStringField("id", "", Field.Store.NO);
            doc.Add(id);
            id.SetStringValue("0");
            w.AddDocument(doc);
            id.SetStringValue("1");
            w.AddDocument(doc);
            w.DeleteDocuments(new Term("id", "0"));

            IndexReader r = w.GetReader();
            w.ForceMergeDeletes();
            w.Dispose();
            r.Dispose();
            r = DirectoryReader.Open(dir);
            Assert.AreEqual(1, r.NumDocs);
            Assert.IsFalse(r.HasDeletions);
            r.Dispose();
            dir.Dispose();
        }

        [Test]
        public virtual void TestDeletesNumDocs()
        {
            Directory dir = NewDirectory();
            IndexWriter w = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)));
            Document doc = new Document();
            doc.Add(NewTextField("field", "a b c", Field.Store.NO));
            Field id = NewStringField("id", "", Field.Store.NO);
            doc.Add(id);
            id.SetStringValue("0");
            w.AddDocument(doc);
            id.SetStringValue("1");
            w.AddDocument(doc);
            IndexReader r = w.GetReader();
            Assert.AreEqual(2, r.NumDocs);
            r.Dispose();

            w.DeleteDocuments(new Term("id", "0"));
            r = w.GetReader();
            Assert.AreEqual(1, r.NumDocs);
            r.Dispose();

            w.DeleteDocuments(new Term("id", "1"));
            r = w.GetReader();
            Assert.AreEqual(0, r.NumDocs);
            r.Dispose();

            w.Dispose();
            dir.Dispose();
        }

        [Test]
        public virtual void TestEmptyIndex()
        {
            // Ensures that getReader works on an empty index, which hasn't been committed yet.
            Directory dir = NewDirectory();
            IndexWriter w = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)));
            IndexReader r = w.GetReader();
            Assert.AreEqual(0, r.NumDocs);
            r.Dispose();
            w.Dispose();
            dir.Dispose();
        }

        [Test]
        public virtual void TestSegmentWarmer()
        {
            Directory dir = NewDirectory();
            AtomicBoolean didWarm = new AtomicBoolean();
            IndexWriter w = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)).SetMaxBufferedDocs(2).SetReaderPooling(true).SetMergedSegmentWarmer(new IndexReaderWarmerAnonymousClass(didWarm)).
                    SetMergePolicy(NewLogMergePolicy(10)));

            Document doc = new Document();
            doc.Add(NewStringField("foo", "bar", Field.Store.NO));
            for (int i = 0; i < 20; i++)
            {
                w.AddDocument(doc);
            }
            w.WaitForMerges();
            w.Dispose();
            dir.Dispose();
            Assert.IsTrue(didWarm);
        }

        private sealed class IndexReaderWarmerAnonymousClass : IndexWriter.IndexReaderWarmer
        {
            private readonly AtomicBoolean didWarm;

            public IndexReaderWarmerAnonymousClass(AtomicBoolean didWarm)
            {
                this.didWarm = didWarm;
            }

            public override void Warm(AtomicReader r)
            {
                IndexSearcher s = NewSearcher(r);
                TopDocs hits = s.Search(new TermQuery(new Term("foo", "bar")), 10);
                Assert.AreEqual(20, hits.TotalHits);
                didWarm.Value = (true);
            }
        }

        [Test]
        public virtual void TestSimpleMergedSegmentWramer()
        {
            Directory dir = NewDirectory();
            AtomicBoolean didWarm = new AtomicBoolean();
            InfoStream infoStream = new InfoStreamAnonymousClass(didWarm);
            IndexWriter w = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)).SetMaxBufferedDocs(2).SetReaderPooling(true).SetInfoStream(infoStream).SetMergedSegmentWarmer(new SimpleMergedSegmentWarmer(infoStream)).SetMergePolicy(NewLogMergePolicy(10)));

            Document doc = new Document();
            doc.Add(NewStringField("foo", "bar", Field.Store.NO));
            for (int i = 0; i < 20; i++)
            {
                w.AddDocument(doc);
            }
            w.WaitForMerges();
            w.Dispose();
            dir.Dispose();
            Assert.IsTrue(didWarm);
        }

        private sealed class InfoStreamAnonymousClass : InfoStream
        {
            private readonly AtomicBoolean didWarm;

            public InfoStreamAnonymousClass(AtomicBoolean didWarm)
            {
                this.didWarm = didWarm;
            }

            protected override void Dispose(bool disposing)
            {
            }

            public override void Message(string component, string message)
            {
                if ("SMSW".Equals(component, StringComparison.Ordinal))
                {
                    didWarm.Value = (true);
                }
            }

            public override bool IsEnabled(string component)
            {
                return true;
            }
        }

        [Test]
        public virtual void TestNoTermsIndex()
        {
            // Some Codecs don't honor the ReaderTermsIndexDivisor, so skip the test if
            // they're picked.
            AssumeFalse("PreFlex codec does not support ReaderTermsIndexDivisor!", "Lucene3x".Equals(Codec.Default.Name, StringComparison.Ordinal));

            IndexWriterConfig conf = (IndexWriterConfig)NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)).SetReaderTermsIndexDivisor(-1);

            // Don't proceed if picked Codec is in the list of illegal ones.
            string format = TestUtil.GetPostingsFormat("f");
            AssumeFalse("Format: " + format + " does not support ReaderTermsIndexDivisor!", 
                (format.Equals("FSTPulsing41", StringComparison.Ordinal) || 
                format.Equals("FSTOrdPulsing41", StringComparison.Ordinal) || 
                format.Equals("FST41", StringComparison.Ordinal) || 
                format.Equals("FSTOrd41", StringComparison.Ordinal) || 
                format.Equals("SimpleText", StringComparison.Ordinal) || 
                format.Equals("Memory", StringComparison.Ordinal) || 
                format.Equals("MockRandom", StringComparison.Ordinal) || 
                format.Equals("Direct", StringComparison.Ordinal)));

            Directory dir = NewDirectory();
            IndexWriter w = new IndexWriter(dir, conf);
            Document doc = new Document();
            doc.Add(new TextField("f", "val", Field.Store.NO));
            w.AddDocument(doc);
            SegmentReader r = GetOnlySegmentReader(DirectoryReader.Open(w, true));
            try
            {
                TestUtil.Docs(Random, r, "f", new BytesRef("val"), null, null, DocsFlags.NONE);
                Assert.Fail("should have failed to seek since terms index was not loaded.");
            }
            catch (Exception e) when (e.IsIllegalStateException())
            {
                // expected - we didn't load the term index
            }
            finally
            {
                r.Dispose();
                w.Dispose();
                dir.Dispose();
            }
        }

        [Test]
        public virtual void TestReopenAfterNoRealChange()
        {
            Directory d = GetAssertNoDeletesDirectory(NewDirectory());
            IndexWriter w = new IndexWriter(d, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)));

            DirectoryReader r = w.GetReader(); // start pooling readers

            DirectoryReader r2 = DirectoryReader.OpenIfChanged(r);
            Assert.IsNull(r2);

            w.AddDocument(new Document());
            DirectoryReader r3 = DirectoryReader.OpenIfChanged(r);
            Assert.IsNotNull(r3);
            Assert.IsTrue(r3.Version != r.Version);
            Assert.IsTrue(r3.IsCurrent());

            // Deletes nothing in reality...:
            w.DeleteDocuments(new Term("foo", "bar"));

            // ... but IW marks this as not current:
            Assert.IsFalse(r3.IsCurrent());
            DirectoryReader r4 = DirectoryReader.OpenIfChanged(r3);
            Assert.IsNull(r4);

            // Deletes nothing in reality...:
            w.DeleteDocuments(new Term("foo", "bar"));
            DirectoryReader r5 = DirectoryReader.OpenIfChanged(r3, w, true);
            Assert.IsNull(r5);

            r3.Dispose();

            w.Dispose();
            d.Dispose();
        }

        [Test]
        public virtual void TestNRTOpenExceptions()
        {
            // LUCENE-5262: test that several failed attempts to obtain an NRT reader
            // don't leak file handles.
            MockDirectoryWrapper dir = (MockDirectoryWrapper)GetAssertNoDeletesDirectory(NewMockDirectory());
            AtomicBoolean shouldFail = new AtomicBoolean();
            dir.FailOn(new FailureAnonymousClass(shouldFail));

            IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random));
            conf.SetMergePolicy(NoMergePolicy.COMPOUND_FILES); // prevent merges from getting in the way
            IndexWriter writer = new IndexWriter(dir, conf);

            // create a segment and open an NRT reader
            writer.AddDocument(new Document());
            writer.GetReader().Dispose();

            // add a new document so a new NRT reader is required
            writer.AddDocument(new Document());

            // try to obtain an NRT reader twice: first time it fails and closes all the
            // other NRT readers. second time it fails, but also fails to close the
            // other NRT reader, since it is already marked closed!
            for (int i = 0; i < 2; i++)
            {
                shouldFail.Value = (true);
                try
                {
                    writer.GetReader().Dispose();
                }
#pragma warning disable 168
                catch (FakeIOException e)
#pragma warning restore 168
                {
                    // expected
                    if (Verbose)
                    {
                        Console.WriteLine("hit expected fake IOE");
                    }
                }
            }

            writer.Dispose();
            dir.Dispose();
        }

        private sealed class FailureAnonymousClass : Failure
        {
            private readonly AtomicBoolean shouldFail;

            public FailureAnonymousClass(AtomicBoolean shouldFail)
            {
                this.shouldFail = shouldFail;
            }

            public override void Eval(MockDirectoryWrapper dir)
            {
                // LUCENENET specific: for these to work in release mode, we have added [MethodImpl(MethodImplOptions.NoInlining)]
                // to each possible target of the StackTraceHelper. If these change, so must the attribute on the target methods.
                if (shouldFail && StackTraceHelper.DoesStackTraceContainMethod("GetReadOnlyClone"))
                {
                    if (Verbose)
                    {
                        Console.WriteLine("TEST: now fail; exc:");
                        Console.WriteLine((new Exception()).StackTrace);
                    }
                    shouldFail.Value = (false);
                    throw new FakeIOException();
                }
            }
        }

        /// <summary>
        /// Make sure if all we do is open NRT reader against
        ///  writer, we don't see merge starvation.
        /// </summary>
        [Test]
        [Slow]
        public virtual void TestTooManySegments()
        {
            Directory dir = GetAssertNoDeletesDirectory(NewDirectory());
            // Don't use newIndexWriterConfig, because we need a
            // "sane" mergePolicy:
            IndexWriterConfig iwc = new IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random));
            IndexWriter w = new IndexWriter(dir, iwc);
            // Create 500 segments:
            for (int i = 0; i < 500; i++)
            {
                Document doc = new Document();
                doc.Add(NewStringField("id", "" + i, Field.Store.NO));
                w.AddDocument(doc);
                IndexReader r = DirectoryReader.Open(w, true);
                // Make sure segment count never exceeds 100:
                Assert.IsTrue(r.Leaves.Count < 100);
                r.Dispose();
            }
            w.Dispose();
            dir.Dispose();
        }
#endif
    }
}