using J2N.Threading;
using Lucene.Net.Codecs;
using Lucene.Net.Documents;
using Lucene.Net.Index.Extensions;
using Lucene.Net.Support;
using Lucene.Net.Support.Threading;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Assert = Lucene.Net.TestFramework.Assert;
using Console = Lucene.Net.Util.SystemConsole;
using JCG = J2N.Collections.Generic;

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

    using BaseDirectoryWrapper = Lucene.Net.Store.BaseDirectoryWrapper;
    using Codec = Lucene.Net.Codecs.Codec;
    using Directory = Lucene.Net.Store.Directory;
    using DocIdSetIterator = Lucene.Net.Search.DocIdSetIterator;
    using Document = Documents.Document;
    using Field = Field;
    using FieldType = FieldType;
    using FilterCodec = Lucene.Net.Codecs.FilterCodec;
    using IOUtils = Lucene.Net.Util.IOUtils;
    using LockObtainFailedException = Lucene.Net.Store.LockObtainFailedException;
    using Lucene46Codec = Lucene.Net.Codecs.Lucene46.Lucene46Codec;
    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
    using MockAnalyzer = Lucene.Net.Analysis.MockAnalyzer;
    using MockDirectoryWrapper = Lucene.Net.Store.MockDirectoryWrapper;
    using PhraseQuery = Lucene.Net.Search.PhraseQuery;
    using PostingsFormat = Lucene.Net.Codecs.PostingsFormat;
    using Pulsing41PostingsFormat = Lucene.Net.Codecs.Pulsing.Pulsing41PostingsFormat;
    using RAMDirectory = Lucene.Net.Store.RAMDirectory;
    using StringField = StringField;
    using TestUtil = Lucene.Net.Util.TestUtil;
    using TextField = TextField;

    [TestFixture]
    public class TestAddIndexes : LuceneTestCase
    {
        [Test]
        public virtual void TestSimpleCase()
        {
            // main directory
            Directory dir = NewDirectory();
            // two auxiliary directories
            Directory aux = NewDirectory();
            Directory aux2 = NewDirectory();

            IndexWriter writer = null;

            writer = NewWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)).SetOpenMode(OpenMode.CREATE));
            // add 100 documents
            AddDocs(writer, 100);
            Assert.AreEqual(100, writer.MaxDoc);
            writer.Dispose();
            TestUtil.CheckIndex(dir);

            writer = NewWriter(aux, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)).SetOpenMode(OpenMode.CREATE).SetMergePolicy(NewLogMergePolicy(false)));
            // add 40 documents in separate files
            AddDocs(writer, 40);
            Assert.AreEqual(40, writer.MaxDoc);
            writer.Dispose();

            writer = NewWriter(aux2, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)).SetOpenMode(OpenMode.CREATE));
            // add 50 documents in compound files
            AddDocs2(writer, 50);
            Assert.AreEqual(50, writer.MaxDoc);
            writer.Dispose();

            // test doc count before segments are merged
            writer = NewWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)).SetOpenMode(OpenMode.APPEND));
            Assert.AreEqual(100, writer.MaxDoc);
            writer.AddIndexes(aux, aux2);
            Assert.AreEqual(190, writer.MaxDoc);
            writer.Dispose();
            TestUtil.CheckIndex(dir);

            // make sure the old index is correct
            VerifyNumDocs(aux, 40);

            // make sure the new index is correct
            VerifyNumDocs(dir, 190);

            // now add another set in.
            Directory aux3 = NewDirectory();
            writer = NewWriter(aux3, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)));
            // add 40 documents
            AddDocs(writer, 40);
            Assert.AreEqual(40, writer.MaxDoc);
            writer.Dispose();

            // test doc count before segments are merged
            writer = NewWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)).SetOpenMode(OpenMode.APPEND));
            Assert.AreEqual(190, writer.MaxDoc);
            writer.AddIndexes(aux3);
            Assert.AreEqual(230, writer.MaxDoc);
            writer.Dispose();

            // make sure the new index is correct
            VerifyNumDocs(dir, 230);

            VerifyTermDocs(dir, new Term("content", "aaa"), 180);

            VerifyTermDocs(dir, new Term("content", "bbb"), 50);

            // now fully merge it.
            writer = NewWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)).SetOpenMode(OpenMode.APPEND));
            writer.ForceMerge(1);
            writer.Dispose();

            // make sure the new index is correct
            VerifyNumDocs(dir, 230);

            VerifyTermDocs(dir, new Term("content", "aaa"), 180);

            VerifyTermDocs(dir, new Term("content", "bbb"), 50);

            // now add a single document
            Directory aux4 = NewDirectory();
            writer = NewWriter(aux4, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)));
            AddDocs2(writer, 1);
            writer.Dispose();

            writer = NewWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)).SetOpenMode(OpenMode.APPEND));
            Assert.AreEqual(230, writer.MaxDoc);
            writer.AddIndexes(aux4);
            Assert.AreEqual(231, writer.MaxDoc);
            writer.Dispose();

            VerifyNumDocs(dir, 231);

            VerifyTermDocs(dir, new Term("content", "bbb"), 51);
            dir.Dispose();
            aux.Dispose();
            aux2.Dispose();
            aux3.Dispose();
            aux4.Dispose();
        }

        [Test]
        public virtual void TestWithPendingDeletes()
        {
            // main directory
            Directory dir = NewDirectory();
            // auxiliary directory
            Directory aux = NewDirectory();

            SetUpDirs(dir, aux);
            IndexWriter writer = NewWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)).SetOpenMode(OpenMode.APPEND));
            writer.AddIndexes(aux);

            // Adds 10 docs, then replaces them with another 10
            // docs, so 10 pending deletes:
            for (int i = 0; i < 20; i++)
            {
                Document doc = new Document();
                doc.Add(NewStringField("id", "" + (i % 10), Field.Store.NO));
                doc.Add(NewTextField("content", "bbb " + i, Field.Store.NO));
                writer.UpdateDocument(new Term("id", "" + (i % 10)), doc);
            }
            // Deletes one of the 10 added docs, leaving 9:
            PhraseQuery q = new PhraseQuery();
            q.Add(new Term("content", "bbb"));
            q.Add(new Term("content", "14"));
            writer.DeleteDocuments(q);

            writer.ForceMerge(1);
            writer.Commit();

            VerifyNumDocs(dir, 1039);
            VerifyTermDocs(dir, new Term("content", "aaa"), 1030);
            VerifyTermDocs(dir, new Term("content", "bbb"), 9);

            writer.Dispose();
            dir.Dispose();
            aux.Dispose();
        }

        [Test]
        public virtual void TestWithPendingDeletes2()
        {
            // main directory
            Directory dir = NewDirectory();
            // auxiliary directory
            Directory aux = NewDirectory();

            SetUpDirs(dir, aux);
            IndexWriter writer = NewWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)).SetOpenMode(OpenMode.APPEND));

            // Adds 10 docs, then replaces them with another 10
            // docs, so 10 pending deletes:
            for (int i = 0; i < 20; i++)
            {
                Document doc = new Document();
                doc.Add(NewStringField("id", "" + (i % 10), Field.Store.NO));
                doc.Add(NewTextField("content", "bbb " + i, Field.Store.NO));
                writer.UpdateDocument(new Term("id", "" + (i % 10)), doc);
            }

            writer.AddIndexes(aux);

            // Deletes one of the 10 added docs, leaving 9:
            PhraseQuery q = new PhraseQuery();
            q.Add(new Term("content", "bbb"));
            q.Add(new Term("content", "14"));
            writer.DeleteDocuments(q);

            writer.ForceMerge(1);
            writer.Commit();

            VerifyNumDocs(dir, 1039);
            VerifyTermDocs(dir, new Term("content", "aaa"), 1030);
            VerifyTermDocs(dir, new Term("content", "bbb"), 9);

            writer.Dispose();
            dir.Dispose();
            aux.Dispose();
        }

        [Test]
        public virtual void TestWithPendingDeletes3()
        {
            // main directory
            Directory dir = NewDirectory();
            // auxiliary directory
            Directory aux = NewDirectory();

            SetUpDirs(dir, aux);
            IndexWriter writer = NewWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)).SetOpenMode(OpenMode.APPEND));

            // Adds 10 docs, then replaces them with another 10
            // docs, so 10 pending deletes:
            for (int i = 0; i < 20; i++)
            {
                Document doc = new Document();
                doc.Add(NewStringField("id", "" + (i % 10), Field.Store.NO));
                doc.Add(NewTextField("content", "bbb " + i, Field.Store.NO));
                writer.UpdateDocument(new Term("id", "" + (i % 10)), doc);
            }

            // Deletes one of the 10 added docs, leaving 9:
            PhraseQuery q = new PhraseQuery();
            q.Add(new Term("content", "bbb"));
            q.Add(new Term("content", "14"));
            writer.DeleteDocuments(q);

            writer.AddIndexes(aux);

            writer.ForceMerge(1);
            writer.Commit();

            VerifyNumDocs(dir, 1039);
            VerifyTermDocs(dir, new Term("content", "aaa"), 1030);
            VerifyTermDocs(dir, new Term("content", "bbb"), 9);

            writer.Dispose();
            dir.Dispose();
            aux.Dispose();
        }

        // case 0: add self or exceed maxMergeDocs, expect exception
        [Test]
        public virtual void TestAddSelf()
        {
            // main directory
            Directory dir = NewDirectory();
            // auxiliary directory
            Directory aux = NewDirectory();

            IndexWriter writer = null;

            writer = NewWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)));
            // add 100 documents
            AddDocs(writer, 100);
            Assert.AreEqual(100, writer.MaxDoc);
            writer.Dispose();

            writer = NewWriter(aux, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)).SetOpenMode(OpenMode.CREATE).SetMaxBufferedDocs(1000).SetMergePolicy(NewLogMergePolicy(false)));
            // add 140 documents in separate files
            AddDocs(writer, 40);
            writer.Dispose();
            writer = NewWriter(aux, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)).SetOpenMode(OpenMode.CREATE).SetMaxBufferedDocs(1000).SetMergePolicy(NewLogMergePolicy(false)));
            AddDocs(writer, 100);
            writer.Dispose();

            writer = NewWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)).SetOpenMode(OpenMode.APPEND));
            try
            {
                // cannot add self
                writer.AddIndexes(aux, dir);
                Assert.IsTrue(false);
            }
            catch (Exception e) when (e.IsIllegalArgumentException())
            {
                Assert.AreEqual(100, writer.MaxDoc);
            }
            writer.Dispose();

            // make sure the index is correct
            VerifyNumDocs(dir, 100);
            dir.Dispose();
            aux.Dispose();
        }

        // in all the remaining tests, make the doc count of the oldest segment
        // in dir large so that it is never merged in addIndexes()
        // case 1: no tail segments
        [Test]
        public virtual void TestNoTailSegments()
        {
            // main directory
            Directory dir = NewDirectory();
            // auxiliary directory
            Directory aux = NewDirectory();

            SetUpDirs(dir, aux);

            IndexWriter writer = NewWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)).SetOpenMode(OpenMode.APPEND).SetMaxBufferedDocs(10).SetMergePolicy(NewLogMergePolicy(4)));
            AddDocs(writer, 10);

            writer.AddIndexes(aux);
            Assert.AreEqual(1040, writer.MaxDoc);
            Assert.AreEqual(1000, writer.GetDocCount(0));
            writer.Dispose();

            // make sure the index is correct
            VerifyNumDocs(dir, 1040);
            dir.Dispose();
            aux.Dispose();
        }

        // case 2: tail segments, invariants hold, no copy
        [Test]
        public virtual void TestNoCopySegments()
        {
            // main directory
            Directory dir = NewDirectory();
            // auxiliary directory
            Directory aux = NewDirectory();

            SetUpDirs(dir, aux);

            IndexWriter writer = NewWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)).SetOpenMode(OpenMode.APPEND).SetMaxBufferedDocs(9).SetMergePolicy(NewLogMergePolicy(4)));
            AddDocs(writer, 2);

            writer.AddIndexes(aux);
            Assert.AreEqual(1032, writer.MaxDoc);
            Assert.AreEqual(1000, writer.GetDocCount(0));
            writer.Dispose();

            // make sure the index is correct
            VerifyNumDocs(dir, 1032);
            dir.Dispose();
            aux.Dispose();
        }

        // case 3: tail segments, invariants hold, copy, invariants hold
        [Test]
        public virtual void TestNoMergeAfterCopy()
        {
            // main directory
            Directory dir = NewDirectory();
            // auxiliary directory
            Directory aux = NewDirectory();

            SetUpDirs(dir, aux);

            IndexWriter writer = NewWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)).SetOpenMode(OpenMode.APPEND).SetMaxBufferedDocs(10).SetMergePolicy(NewLogMergePolicy(4)));

            writer.AddIndexes(aux, new MockDirectoryWrapper(Random, new RAMDirectory(aux, NewIOContext(Random))));
            Assert.AreEqual(1060, writer.MaxDoc);
            Assert.AreEqual(1000, writer.GetDocCount(0));
            writer.Dispose();

            // make sure the index is correct
            VerifyNumDocs(dir, 1060);
            dir.Dispose();
            aux.Dispose();
        }

        // case 4: tail segments, invariants hold, copy, invariants not hold
        [Test]
        public virtual void TestMergeAfterCopy()
        {
            // main directory
            Directory dir = NewDirectory();
            // auxiliary directory
            Directory aux = NewDirectory();

            SetUpDirs(dir, aux, true);

            IndexWriterConfig dontMergeConfig = (new IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random))).SetMergePolicy(NoMergePolicy.COMPOUND_FILES);
            IndexWriter writer = new IndexWriter(aux, dontMergeConfig);
            for (int i = 0; i < 20; i++)
            {
                writer.DeleteDocuments(new Term("id", "" + i));
            }
            writer.Dispose();
            IndexReader reader = DirectoryReader.Open(aux);
            Assert.AreEqual(10, reader.NumDocs);
            reader.Dispose();

            writer = NewWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)).SetOpenMode(OpenMode.APPEND).SetMaxBufferedDocs(4).SetMergePolicy(NewLogMergePolicy(4)));

            if (Verbose)
            {
                Console.WriteLine("\nTEST: now addIndexes");
            }
            writer.AddIndexes(aux, new MockDirectoryWrapper(Random, new RAMDirectory(aux, NewIOContext(Random))));
            Assert.AreEqual(1020, writer.MaxDoc);
            Assert.AreEqual(1000, writer.GetDocCount(0));
            writer.Dispose();
            dir.Dispose();
            aux.Dispose();
        }

        // case 5: tail segments, invariants not hold
        [Test]
        public virtual void TestMoreMerges()
        {
            // main directory
            Directory dir = NewDirectory();
            // auxiliary directory
            Directory aux = NewDirectory();
            Directory aux2 = NewDirectory();

            SetUpDirs(dir, aux, true);

            IndexWriter writer = NewWriter(aux2, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)).SetOpenMode(OpenMode.CREATE).SetMaxBufferedDocs(100).SetMergePolicy(NewLogMergePolicy(10)));
            writer.AddIndexes(aux);
            Assert.AreEqual(30, writer.MaxDoc);
            Assert.AreEqual(3, writer.SegmentCount);
            writer.Dispose();

            IndexWriterConfig dontMergeConfig = (new IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random))).SetMergePolicy(NoMergePolicy.COMPOUND_FILES);
            writer = new IndexWriter(aux, dontMergeConfig);
            for (int i = 0; i < 27; i++)
            {
                writer.DeleteDocuments(new Term("id", "" + i));
            }
            writer.Dispose();
            IndexReader reader = DirectoryReader.Open(aux);
            Assert.AreEqual(3, reader.NumDocs);
            reader.Dispose();

            dontMergeConfig = (new IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random))).SetMergePolicy(NoMergePolicy.COMPOUND_FILES);
            writer = new IndexWriter(aux2, dontMergeConfig);
            for (int i = 0; i < 8; i++)
            {
                writer.DeleteDocuments(new Term("id", "" + i));
            }
            writer.Dispose();
            reader = DirectoryReader.Open(aux2);
            Assert.AreEqual(22, reader.NumDocs);
            reader.Dispose();

            writer = NewWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)).SetOpenMode(OpenMode.APPEND).SetMaxBufferedDocs(6).SetMergePolicy(NewLogMergePolicy(4)));

            writer.AddIndexes(aux, aux2);
            Assert.AreEqual(1040, writer.MaxDoc);
            Assert.AreEqual(1000, writer.GetDocCount(0));
            writer.Dispose();
            dir.Dispose();
            aux.Dispose();
            aux2.Dispose();
        }

        private IndexWriter NewWriter(Directory dir, IndexWriterConfig conf)
        {
            conf.SetMergePolicy(new LogDocMergePolicy());
            IndexWriter writer = new IndexWriter(dir, conf);
            return writer;
        }

        private void AddDocs(IndexWriter writer, int numDocs)
        {
            for (int i = 0; i < numDocs; i++)
            {
                Document doc = new Document();
                doc.Add(NewTextField("content", "aaa", Field.Store.NO));
                writer.AddDocument(doc);
            }
        }

        private void AddDocs2(IndexWriter writer, int numDocs)
        {
            for (int i = 0; i < numDocs; i++)
            {
                Document doc = new Document();
                doc.Add(NewTextField("content", "bbb", Field.Store.NO));
                writer.AddDocument(doc);
            }
        }

        private void VerifyNumDocs(Directory dir, int numDocs)
        {
            IndexReader reader = DirectoryReader.Open(dir);
            Assert.AreEqual(numDocs, reader.MaxDoc);
            Assert.AreEqual(numDocs, reader.NumDocs);
            reader.Dispose();
        }

        private void VerifyTermDocs(Directory dir, Term term, int numDocs)
        {
            IndexReader reader = DirectoryReader.Open(dir);
            DocsEnum docsEnum = TestUtil.Docs(Random, reader, term.Field, term.Bytes, null, null, DocsFlags.NONE);
            int count = 0;
            while (docsEnum.NextDoc() != DocIdSetIterator.NO_MORE_DOCS)
            {
                count++;
            }
            Assert.AreEqual(numDocs, count);
            reader.Dispose();
        }

        private void SetUpDirs(Directory dir, Directory aux)
        {
            SetUpDirs(dir, aux, false);
        }

        private void SetUpDirs(Directory dir, Directory aux, bool withID)
        {
            IndexWriter writer = null;

            writer = NewWriter(dir, (IndexWriterConfig)NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)).SetOpenMode(OpenMode.CREATE).SetMaxBufferedDocs(1000));
            // add 1000 documents in 1 segment
            if (withID)
            {
                AddDocsWithID(writer, 1000, 0);
            }
            else
            {
                AddDocs(writer, 1000);
            }
            Assert.AreEqual(1000, writer.MaxDoc);
            Assert.AreEqual(1, writer.SegmentCount);
            writer.Dispose();

            writer = NewWriter(aux, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)).SetOpenMode(OpenMode.CREATE).SetMaxBufferedDocs(1000).SetMergePolicy(NewLogMergePolicy(false, 10)));
            // add 30 documents in 3 segments
            for (int i = 0; i < 3; i++)
            {
                if (withID)
                {
                    AddDocsWithID(writer, 10, 10 * i);
                }
                else
                {
                    AddDocs(writer, 10);
                }
                writer.Dispose();
                writer = NewWriter(aux, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)).SetOpenMode(OpenMode.APPEND).SetMaxBufferedDocs(1000).SetMergePolicy(NewLogMergePolicy(false, 10)));
            }
            Assert.AreEqual(30, writer.MaxDoc);
            Assert.AreEqual(3, writer.SegmentCount);
            writer.Dispose();
        }

        // LUCENE-1270
        [Test]
        public virtual void TestHangOnClose()
        {
            Directory dir = NewDirectory();
            LogByteSizeMergePolicy lmp = new LogByteSizeMergePolicy();
            lmp.NoCFSRatio = 0.0;
            lmp.MergeFactor = 100;
            IndexWriter writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)).SetMaxBufferedDocs(5).SetMergePolicy(lmp));

            Document doc = new Document();
            FieldType customType = new FieldType(TextField.TYPE_STORED);
            customType.StoreTermVectors = true;
            customType.StoreTermVectorPositions = true;
            customType.StoreTermVectorOffsets = true;
            doc.Add(NewField("content", "aaa bbb ccc ddd eee fff ggg hhh iii", customType));
            for (int i = 0; i < 60; i++)
            {
                writer.AddDocument(doc);
            }

            Document doc2 = new Document();
            FieldType customType2 = new FieldType();
            customType2.IsStored = true;
            doc2.Add(NewField("content", "aaa bbb ccc ddd eee fff ggg hhh iii", customType2));
            doc2.Add(NewField("content", "aaa bbb ccc ddd eee fff ggg hhh iii", customType2));
            doc2.Add(NewField("content", "aaa bbb ccc ddd eee fff ggg hhh iii", customType2));
            doc2.Add(NewField("content", "aaa bbb ccc ddd eee fff ggg hhh iii", customType2));
            for (int i = 0; i < 10; i++)
            {
                writer.AddDocument(doc2);
            }
            writer.Dispose();

            Directory dir2 = NewDirectory();
            lmp = new LogByteSizeMergePolicy();
            lmp.MinMergeMB = 0.0001;
            lmp.NoCFSRatio = 0.0;
            lmp.MergeFactor = 4;
            writer = new IndexWriter(dir2, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)).SetMergeScheduler(new SerialMergeScheduler()).SetMergePolicy(lmp));
            writer.AddIndexes(dir);
            writer.Dispose();
            dir.Dispose();
            dir2.Dispose();
        }

        // TODO: these are also in TestIndexWriter... add a simple doc-writing method
        // like this to LuceneTestCase?
        private void AddDoc(IndexWriter writer)
        {
            Document doc = new Document();
            doc.Add(NewTextField("content", "aaa", Field.Store.NO));
            writer.AddDocument(doc);
        }

        private abstract class RunAddIndexesThreads
        {
            internal Directory dir, dir2;
            internal const int NUM_INIT_DOCS = 17;
            internal IndexWriter writer2;
            internal readonly IList<Exception> failures = new JCG.List<Exception>();
            internal volatile bool didClose;
            internal readonly IndexReader[] readers;
            internal readonly int NUM_COPY;
            internal const int NUM_THREADS = 5;
            internal readonly ThreadJob[] threads = new ThreadJob[NUM_THREADS];

            public RunAddIndexesThreads(TestAddIndexes outerInstance, int numCopy)
            {
                NUM_COPY = numCopy;
                dir = new MockDirectoryWrapper(Random, new RAMDirectory());
                IndexWriter writer = new IndexWriter(dir, (IndexWriterConfig)new IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)).SetMaxBufferedDocs(2));
                for (int i = 0; i < NUM_INIT_DOCS; i++)
                {
                    outerInstance.AddDoc(writer);
                }
                writer.Dispose();

                dir2 = NewDirectory();
                writer2 = new IndexWriter(dir2, new IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)));
                writer2.Commit();

                readers = new IndexReader[NUM_COPY];
                for (int i = 0; i < NUM_COPY; i++)
                {
                    readers[i] = DirectoryReader.Open(dir);
                }
            }

            internal virtual void LaunchThreads(int numIter)
            {
                for (int i = 0; i < NUM_THREADS; i++)
                {
                    threads[i] = new ThreadAnonymousClass(this, numIter);
                }

                for (int i = 0; i < NUM_THREADS; i++)
                {
                    threads[i].Start();
                }
            }

            private sealed class ThreadAnonymousClass : ThreadJob
            {
                private readonly RunAddIndexesThreads outerInstance;
                private readonly int numIter;

                public ThreadAnonymousClass(RunAddIndexesThreads outerInstance, int numIter)
                {
                    this.outerInstance = outerInstance;
                    this.numIter = numIter;
                }

                public override void Run()
                {
                    try
                    {
                        Directory[] dirs = new Directory[outerInstance.NUM_COPY];
                        for (int k = 0; k < outerInstance.NUM_COPY; k++)
                        {
                            dirs[k] = new MockDirectoryWrapper(Random, new RAMDirectory(outerInstance.dir, NewIOContext(Random)));
                        }

                        int j = 0;

                        while (true)
                        {
                            // System.out.println(Thread.currentThread().getName() + ": iter j=" + j);
                            if (numIter > 0 && j == numIter)
                            {
                                break;
                            }
                            outerInstance.DoBody(j++, dirs);
                        }
                    }
                    catch (Exception t) when (t.IsThrowable())
                    {
                        outerInstance.Handle(t);
                    }
                }
            }

            internal virtual void JoinThreads()
            {
                for (int i = 0; i < NUM_THREADS; i++)
                {
                    threads[i].Join();
                }
            }

            internal virtual void Close(bool doWait)
            {
                didClose = true;
                writer2.Dispose(doWait);
            }

            internal virtual void CloseDir()
            {
                for (int i = 0; i < NUM_COPY; i++)
                {
                    readers[i].Dispose();
                }
                dir2.Dispose();
            }

            internal abstract void DoBody(int j, Directory[] dirs);

            internal abstract void Handle(Exception t);
        }

        private class CommitAndAddIndexes : RunAddIndexesThreads
        {
            public CommitAndAddIndexes(TestAddIndexes outerInstance, int numCopy)
                : base(outerInstance, numCopy)
            {
            }

            internal override void Handle(Exception t)
            {
                t.printStackTrace(Console.Out);
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

            internal override void DoBody(int j, Directory[] dirs)
            {
                switch (j % 5)
                {
                    case 0:
                        if (Verbose)
                        {
                            Console.WriteLine(Thread.CurrentThread.Name + ": TEST: addIndexes(Dir[]) then full merge");
                        }
                        writer2.AddIndexes(dirs);
                        writer2.ForceMerge(1);
                        break;

                    case 1:
                        if (Verbose)
                        {
                            Console.WriteLine(Thread.CurrentThread.Name + ": TEST: addIndexes(Dir[])");
                        }
                        writer2.AddIndexes(dirs);
                        break;

                    case 2:
                        if (Verbose)
                        {
                            Console.WriteLine(Thread.CurrentThread.Name + ": TEST: addIndexes(IndexReader[])");
                        }
                        writer2.AddIndexes(readers);
                        break;

                    case 3:
                        if (Verbose)
                        {
                            Console.WriteLine(Thread.CurrentThread.Name + ": TEST: addIndexes(Dir[]) then maybeMerge");
                        }
                        writer2.AddIndexes(dirs);
                        writer2.MaybeMerge();
                        break;

                    case 4:
                        if (Verbose)
                        {
                            Console.WriteLine(Thread.CurrentThread.Name + ": TEST: commit");
                        }
                        writer2.Commit();
                        break;
                }
            }
        }

        // LUCENE-1335: test simultaneous addIndexes & commits
        // from multiple threads
        [Test]
        [Timeout(300_000)] // 5 minutes
        public virtual void TestAddIndexesWithThreads()
        {
            int NUM_ITER = TestNightly ? 15 : 5;
            const int NUM_COPY = 3;
            CommitAndAddIndexes c = new CommitAndAddIndexes(this, NUM_COPY);
            c.LaunchThreads(NUM_ITER);

            for (int i = 0; i < 100; i++)
            {
                AddDoc(c.writer2);
            }

            c.JoinThreads();

            int expectedNumDocs = 100 + NUM_COPY * (4 * NUM_ITER / 5) * RunAddIndexesThreads.NUM_THREADS * RunAddIndexesThreads.NUM_INIT_DOCS;
            Assert.AreEqual(expectedNumDocs, c.writer2.NumDocs, "expected num docs don't match - failures: " + Environment.NewLine
                + string.Join(Environment.NewLine, c.failures.Select(x => x.ToString())));

            c.Close(true);

            Assert.IsTrue(c.failures.Count == 0, "found unexpected failures: " + c.failures);

            IndexReader reader = DirectoryReader.Open(c.dir2);
            Assert.AreEqual(expectedNumDocs, reader.NumDocs);
            reader.Dispose();

            c.CloseDir();
        }

        private class CommitAndAddIndexes2 : CommitAndAddIndexes
        {
            public CommitAndAddIndexes2(TestAddIndexes outerInstance, int numCopy)
                : base(outerInstance, numCopy)
            {
            }

            internal override void Handle(Exception t)
            {
                if (!t.IsAlreadyClosedException() && !t.IsNullPointerException())
                {
                    t.printStackTrace(Console.Out);
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
            }
        }

        // LUCENE-1335: test simultaneous addIndexes & close
        [Test]
        public virtual void TestAddIndexesWithClose()
        {
            const int NUM_COPY = 3;
            CommitAndAddIndexes2 c = new CommitAndAddIndexes2(this, NUM_COPY);
            //c.writer2.setInfoStream(System.out);
            c.LaunchThreads(-1);

            // Close w/o first stopping/joining the threads
            c.Close(true);
            //c.writer2.Dispose();

            c.JoinThreads();

            c.CloseDir();

            Assert.IsTrue(c.failures.Count == 0);
        }

        private class CommitAndAddIndexes3 : RunAddIndexesThreads
        {
            public CommitAndAddIndexes3(TestAddIndexes outerInstance, int numCopy)
                : base(outerInstance, numCopy)
            {
            }

            internal override void DoBody(int j, Directory[] dirs)
            {
                switch (j % 5)
                {
                    case 0:
                        if (Verbose)
                        {
                            Console.WriteLine("TEST: " + Thread.CurrentThread.Name + ": addIndexes + full merge");
                        }
                        writer2.AddIndexes(dirs);
                        writer2.ForceMerge(1);
                        break;

                    case 1:
                        if (Verbose)
                        {
                            Console.WriteLine("TEST: " + Thread.CurrentThread.Name + ": addIndexes");
                        }
                        writer2.AddIndexes(dirs);
                        break;

                    case 2:
                        if (Verbose)
                        {
                            Console.WriteLine("TEST: " + Thread.CurrentThread.Name + ": addIndexes(IR[])");
                        }
                        writer2.AddIndexes(readers);
                        break;

                    case 3:
                        if (Verbose)
                        {
                            Console.WriteLine("TEST: " + Thread.CurrentThread.Name + ": full merge");
                        }
                        writer2.ForceMerge(1);
                        break;

                    case 4:
                        if (Verbose)
                        {
                            Console.WriteLine("TEST: " + Thread.CurrentThread.Name + ": commit");
                        }
                        writer2.Commit();
                        break;
                }
            }

            internal override void Handle(Exception t)
            {
                bool report = true;

                if (t.IsAlreadyClosedException() || t is MergePolicy.MergeAbortedException || t.IsNullPointerException())
                {
                    report = !didClose;
                }
                else if (t.IsNoSuchFileExceptionOrFileNotFoundException())
                {
                    report = !didClose;
                }
                else if (t.IsIOException())
                {
                    Exception t2 = t.InnerException;
                    if (t2 is MergePolicy.MergeAbortedException)
                    {
                        report = !didClose;
                    }
                }
                if (report)
                {
                    t.printStackTrace(Console.Out);
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
            }
        }

        // LUCENE-1335: test simultaneous addIndexes & close
        [Test]
        [Slow]
        public virtual void TestAddIndexesWithCloseNoWait()
        {
            const int NUM_COPY = 50;
            CommitAndAddIndexes3 c = new CommitAndAddIndexes3(this, NUM_COPY);
            c.LaunchThreads(-1);

            Thread.Sleep(TestUtil.NextInt32(Random, 10, 500));

            // Close w/o first stopping/joining the threads
            if (Verbose)
            {
                Console.WriteLine("TEST: now close(false)");
            }
            c.Close(false);

            c.JoinThreads();

            if (Verbose)
            {
                Console.WriteLine("TEST: done join threads");
            }
            c.CloseDir();

            Assert.IsTrue(c.failures.Count == 0);
        }

        // LUCENE-1335: test simultaneous addIndexes & close
        [Test]
        [Timeout(300_000)] // 5 minutes
        public virtual void TestAddIndexesWithRollback()
        {
            int NUM_COPY = TestNightly ? 50 : 5;
            CommitAndAddIndexes3 c = new CommitAndAddIndexes3(this, NUM_COPY);
            c.LaunchThreads(-1);

            Thread.Sleep(TestUtil.NextInt32(Random, 10, 500));

            // Close w/o first stopping/joining the threads
            if (Verbose)
            {
                Console.WriteLine("TEST: now force rollback");
            }
            c.didClose = true;
            c.writer2.Rollback();

            c.JoinThreads();

            c.CloseDir();

            Assert.IsTrue(c.failures.Count == 0);
        }

        // LUCENE-2996: tests that addIndexes(IndexReader) applies existing deletes correctly.
        [Test]
        public virtual void TestExistingDeletes()
        {
            Directory[] dirs = new Directory[2];
            for (int i = 0; i < dirs.Length; i++)
            {
                dirs[i] = NewDirectory();
                IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random));
                IndexWriter writer = new IndexWriter(dirs[i], conf);
                Document doc = new Document();
                doc.Add(new StringField("id", "myid", Field.Store.NO));
                writer.AddDocument(doc);
                writer.Dispose();
            }

            IndexWriterConfig conf_ = new IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random));
            IndexWriter writer_ = new IndexWriter(dirs[0], conf_);

            // Now delete the document
            writer_.DeleteDocuments(new Term("id", "myid"));
            IndexReader r = DirectoryReader.Open(dirs[1]);
            try
            {
                writer_.AddIndexes(r);
            }
            finally
            {
                r.Dispose();
            }
            writer_.Commit();
            Assert.AreEqual(1, writer_.NumDocs, "Documents from the incoming index should not have been deleted");
            writer_.Dispose();

            foreach (Directory dir in dirs)
            {
                dir.Dispose();
            }
        }

        // just like addDocs but with ID, starting from docStart
        private void AddDocsWithID(IndexWriter writer, int numDocs, int docStart)
        {
            for (int i = 0; i < numDocs; i++)
            {
                Document doc = new Document();
                doc.Add(NewTextField("content", "aaa", Field.Store.NO));
                doc.Add(NewTextField("id", "" + (docStart + i), Field.Store.YES));
                writer.AddDocument(doc);
            }
        }

        [Test]
        public virtual void TestSimpleCaseCustomCodec()
        {
            // main directory
            Directory dir = NewDirectory();
            // two auxiliary directories
            Directory aux = NewDirectory();
            Directory aux2 = NewDirectory();
            Codec codec = new CustomPerFieldCodec();
            IndexWriter writer = null;

            writer = NewWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)).SetOpenMode(OpenMode.CREATE).SetCodec(codec));
            // add 100 documents
            AddDocsWithID(writer, 100, 0);
            Assert.AreEqual(100, writer.MaxDoc);
            writer.Commit();
            writer.Dispose();
            TestUtil.CheckIndex(dir);

            writer = NewWriter(aux, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)).SetOpenMode(OpenMode.CREATE).SetCodec(codec).SetMaxBufferedDocs(10).SetMergePolicy(NewLogMergePolicy(false)));
            // add 40 documents in separate files
            AddDocs(writer, 40);
            Assert.AreEqual(40, writer.MaxDoc);
            writer.Commit();
            writer.Dispose();

            writer = NewWriter(aux2, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)).SetOpenMode(OpenMode.CREATE).SetCodec(codec));
            // add 40 documents in compound files
            AddDocs2(writer, 50);
            Assert.AreEqual(50, writer.MaxDoc);
            writer.Commit();
            writer.Dispose();

            // test doc count before segments are merged
            writer = NewWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)).SetOpenMode(OpenMode.APPEND).SetCodec(codec));
            Assert.AreEqual(100, writer.MaxDoc);
            writer.AddIndexes(aux, aux2);
            Assert.AreEqual(190, writer.MaxDoc);
            writer.Dispose();

            dir.Dispose();
            aux.Dispose();
            aux2.Dispose();
        }

        private sealed class CustomPerFieldCodec : Lucene46Codec
        {
            internal readonly PostingsFormat simpleTextFormat;
            internal readonly PostingsFormat defaultFormat;
            internal readonly PostingsFormat mockSepFormat;

            public CustomPerFieldCodec()
            {
                simpleTextFormat = Codecs.PostingsFormat.ForName("SimpleText");
                defaultFormat = Codecs.PostingsFormat.ForName("Lucene41");
                mockSepFormat = Codecs.PostingsFormat.ForName("MockSep");
            }

            public override PostingsFormat GetPostingsFormatForField(string field)
            {
                if (field.Equals("id", StringComparison.Ordinal))
                {
                    return simpleTextFormat;
                }
                else if (field.Equals("content", StringComparison.Ordinal))
                {
                    return mockSepFormat;
                }
                else
                {
                    return defaultFormat;
                }
            }
        }

        // LUCENE-2790: tests that the non CFS files were deleted by addIndexes
        [Test]
        public virtual void TestNonCFSLeftovers()
        {
            Directory[] dirs = new Directory[2];
            for (int i = 0; i < dirs.Length; i++)
            {
                dirs[i] = new RAMDirectory();
                IndexWriter w = new IndexWriter(dirs[i], new IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)));
                Document d = new Document();
                FieldType customType = new FieldType(TextField.TYPE_STORED);
                customType.StoreTermVectors = true;
                d.Add(new Field("c", "v", customType));
                w.AddDocument(d);
                w.Dispose();
            }

            IndexReader[] readers = new IndexReader[] { DirectoryReader.Open(dirs[0]), DirectoryReader.Open(dirs[1]) };

            Directory dir = new MockDirectoryWrapper(Random, new RAMDirectory());
            IndexWriterConfig conf = (new IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random))).SetMergePolicy(NewLogMergePolicy(true));
            MergePolicy lmp = conf.MergePolicy;
            // Force creation of CFS:
            lmp.NoCFSRatio = 1.0;
            lmp.MaxCFSSegmentSizeMB = double.PositiveInfinity;
            IndexWriter w3 = new IndexWriter(dir, conf);
            w3.AddIndexes(readers);
            w3.Dispose();
            // we should now see segments_X,
            // segments.gen,_Y.cfs,_Y.cfe, _Z.si
            Assert.AreEqual(5, dir.ListAll().Length, "Only one compound segment should exist, but got: " + Arrays.ToString(dir.ListAll()));
            dir.Dispose();
        }

        [CodecName("NotRegistered")]
        private sealed class UnRegisteredCodec : FilterCodec
        {
            public UnRegisteredCodec()
                : base(new Lucene46Codec())
            {
            }
        }

        /*
         * simple test that ensures we getting expected exceptions
         */
        [Test]
        public virtual void TestAddIndexMissingCodec()
        {
            BaseDirectoryWrapper toAdd = NewDirectory();
            // Disable checkIndex, else we get an exception because
            // of the unregistered codec:
            toAdd.CheckIndexOnDispose = false;
            {
                IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random));
                conf.SetCodec(new UnRegisteredCodec());
                using var w = new IndexWriter(toAdd, conf);
                Document doc = new Document();
                FieldType customType = new FieldType();
                customType.IsIndexed = true;
                doc.Add(NewField("foo", "bar", customType));
                w.AddDocument(doc);
            }

            {
                using Directory dir = NewDirectory();
                IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random));
                conf.SetCodec(TestUtil.AlwaysPostingsFormat(new Pulsing41PostingsFormat(1 + Random.Next(20))));
                IndexWriter w = new IndexWriter(dir, conf);
                try
                {
                    w.AddIndexes(toAdd);
                    Assert.Fail("no such codec");
                }
                catch (Exception ex) when (ex.IsIllegalArgumentException())
                {
                    // expected
                }
                finally
                {
                    w.Dispose();
                }
                using IndexReader open = DirectoryReader.Open(dir);
                Assert.AreEqual(0, open.NumDocs);
            }

            try
            {
                DirectoryReader.Open(toAdd);
                Assert.Fail("no such codec");
            }
            catch (Exception ex) when (ex.IsIllegalArgumentException())
            {
                // expected
            }
            toAdd.Dispose();
        }

        // LUCENE-3575
        [Test]
        public virtual void TestFieldNamesChanged()
        {
            Directory d1 = NewDirectory();
            RandomIndexWriter w = new RandomIndexWriter(Random, d1);
            Document doc = new Document();
            doc.Add(NewStringField("f1", "doc1 field1", Field.Store.YES));
            doc.Add(NewStringField("id", "1", Field.Store.YES));
            w.AddDocument(doc);
            IndexReader r1 = w.GetReader();
            w.Dispose();

            Directory d2 = NewDirectory();
            w = new RandomIndexWriter(Random, d2);
            doc = new Document();
            doc.Add(NewStringField("f2", "doc2 field2", Field.Store.YES));
            doc.Add(NewStringField("id", "2", Field.Store.YES));
            w.AddDocument(doc);
            IndexReader r2 = w.GetReader();
            w.Dispose();

            Directory d3 = NewDirectory();
            w = new RandomIndexWriter(Random, d3);
            w.AddIndexes(r1, r2);
            r1.Dispose();
            d1.Dispose();
            r2.Dispose();
            d2.Dispose();

            IndexReader r3 = w.GetReader();
            w.Dispose();
            Assert.AreEqual(2, r3.NumDocs);
            for (int docID = 0; docID < 2; docID++)
            {
                Document d = r3.Document(docID);
                if (d.Get("id").Equals("1", StringComparison.Ordinal))
                {
                    Assert.AreEqual("doc1 field1", d.Get("f1"));
                }
                else
                {
                    Assert.AreEqual("doc2 field2", d.Get("f2"));
                }
            }
            r3.Dispose();
            d3.Dispose();
        }

        [Test]
        public virtual void TestAddEmpty()
        {
            Directory d1 = NewDirectory();
            RandomIndexWriter w = new RandomIndexWriter(Random, d1);
            MultiReader empty = new MultiReader();
            w.AddIndexes(empty);
            w.Dispose();
            DirectoryReader dr = DirectoryReader.Open(d1);
            foreach (AtomicReaderContext ctx in dr.Leaves)
            {
                Assert.IsTrue(ctx.Reader.MaxDoc > 0, "empty segments should be dropped by addIndexes");
            }
            dr.Dispose();
            d1.Dispose();
        }

        // Currently it's impossible to end up with a segment with all documents
        // deleted, as such segments are dropped. Still, to validate that addIndexes
        // works with such segments, or readers that end up in such state, we fake an
        // all deleted segment.
        [Test]
        public virtual void TestFakeAllDeleted()
        {
            Directory src = NewDirectory(), dest = NewDirectory();
            RandomIndexWriter w = new RandomIndexWriter(Random, src);
            w.AddDocument(new Document());
            IndexReader allDeletedReader = new AllDeletedFilterReader((AtomicReader)w.GetReader().Leaves[0].Reader);
            w.Dispose();

            w = new RandomIndexWriter(Random, dest);
            w.AddIndexes(allDeletedReader);
            w.Dispose();
            DirectoryReader dr = DirectoryReader.Open(src);
            foreach (AtomicReaderContext ctx in dr.Leaves)
            {
                Assert.IsTrue(ctx.Reader.MaxDoc > 0, "empty segments should be dropped by addIndexes");
            }
            dr.Dispose();
            allDeletedReader.Dispose();
            src.Dispose();
            dest.Dispose();
        }

        /// <summary>
        /// Make sure an open IndexWriter on an incoming Directory
        ///  causes a LockObtainFailedException
        /// </summary>
        [Test]
        public virtual void TestLocksBlock()
        {
            Directory src = NewDirectory();
            RandomIndexWriter w1 = new RandomIndexWriter(Random, src);
            w1.AddDocument(new Document());
            w1.Commit();

            Directory dest = NewDirectory();

            IndexWriterConfig iwc = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random));
            iwc.SetWriteLockTimeout(1);
            RandomIndexWriter w2 = new RandomIndexWriter(Random, dest, iwc);

            try
            {
                w2.AddIndexes(src);
                Assert.Fail("did not hit expected exception");
            }
#pragma warning disable 168
            catch (LockObtainFailedException lofe)
#pragma warning restore 168
            {
                // expected
            }

            IOUtils.Dispose(w1, w2, src, dest);
        }
    }
}