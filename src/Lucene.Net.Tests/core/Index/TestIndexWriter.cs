using Lucene.Net.Analysis.Tokenattributes;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Lucene.Net.Documents;
using Lucene.Net.Search;

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

    using Lucene.Net.Analysis;
    using Lucene.Net.Randomized.Generators;
    using Lucene.Net.Support;
    using Lucene.Net.Util;
    using NUnit.Framework;
    using System.Diagnostics;
    using System.IO;
    using AlreadyClosedException = Lucene.Net.Store.AlreadyClosedException;
    using Automaton = Lucene.Net.Util.Automaton.Automaton;
    using BaseDirectoryWrapper = Lucene.Net.Store.BaseDirectoryWrapper;
    using BasicAutomata = Lucene.Net.Util.Automaton.BasicAutomata;

    //using SimpleTextCodec = Lucene.Net.Codecs.simpletext.SimpleTextCodec;
    using BinaryDocValuesField = BinaryDocValuesField;
    using Bits = Lucene.Net.Util.Bits;
    using BytesRef = Lucene.Net.Util.BytesRef;
    using CharacterRunAutomaton = Lucene.Net.Util.Automaton.CharacterRunAutomaton;
    using CharTermAttribute = Lucene.Net.Analysis.Tokenattributes.CharTermAttribute;
    using Constants = Lucene.Net.Util.Constants;
    using Directory = Lucene.Net.Store.Directory;
    using DocIdSetIterator = Lucene.Net.Search.DocIdSetIterator;
    using Document = Documents.Document;
    using Field = Field;
    using FieldType = FieldType;
    using IndexOutput = Lucene.Net.Store.IndexOutput;
    using IndexSearcher = Lucene.Net.Search.IndexSearcher;
    using IOContext = Lucene.Net.Store.IOContext;
    using IOUtils = Lucene.Net.Util.IOUtils;
    using Lock = Lucene.Net.Store.Lock;
    using LockFactory = Lucene.Net.Store.LockFactory;
    using LockObtainFailedException = Lucene.Net.Store.LockObtainFailedException;
    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
    using MatchAllDocsQuery = Lucene.Net.Search.MatchAllDocsQuery;
    using MockDirectoryWrapper = Lucene.Net.Store.MockDirectoryWrapper;
    using NoLockFactory = Lucene.Net.Store.NoLockFactory;
    using NumericDocValuesField = NumericDocValuesField;
    using OpenMode_e = Lucene.Net.Index.IndexWriterConfig.OpenMode_e;
    using PackedInts = Lucene.Net.Util.Packed.PackedInts;
    using PhraseQuery = Lucene.Net.Search.PhraseQuery;
    using RAMDirectory = Lucene.Net.Store.RAMDirectory;
    using ScoreDoc = Lucene.Net.Search.ScoreDoc;
    using SimpleFSLockFactory = Lucene.Net.Store.SimpleFSLockFactory;
    using SingleInstanceLockFactory = Lucene.Net.Store.SingleInstanceLockFactory;
    using SortedDocValuesField = SortedDocValuesField;
    using SortedSetDocValuesField = SortedSetDocValuesField;
    using StoredField = StoredField;
    using StringField = StringField;
    using TermQuery = Lucene.Net.Search.TermQuery;
    using TestUtil = Lucene.Net.Util.TestUtil;
    using TextField = TextField;

    [TestFixture]
    public class TestIndexWriter : LuceneTestCase
    {
        private static readonly FieldType StoredTextType = new FieldType(TextField.TYPE_NOT_STORED);

        [Test]
        public virtual void TestDocCount()
        {
            Directory dir = NewDirectory();

            IndexWriter writer = null;
            IndexReader reader = null;
            int i;

            long savedWriteLockTimeout = IndexWriterConfig.DefaultWriteLockTimeout;
            try
            {
                IndexWriterConfig.DefaultWriteLockTimeout = 2000;
                Assert.AreEqual(2000, IndexWriterConfig.DefaultWriteLockTimeout);
                writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())));
            }
            finally
            {
                IndexWriterConfig.DefaultWriteLockTimeout = savedWriteLockTimeout;
            }

            // add 100 documents
            for (i = 0; i < 100; i++)
            {
                AddDocWithIndex(writer, i);
            }
            Assert.AreEqual(100, writer.MaxDoc);
            writer.Dispose();

            // delete 40 documents
            writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())).SetMergePolicy(NoMergePolicy.NO_COMPOUND_FILES));
            for (i = 0; i < 40; i++)
            {
                writer.DeleteDocuments(new Term("id", "" + i));
            }
            writer.Dispose();

            reader = DirectoryReader.Open(dir);
            Assert.AreEqual(60, reader.NumDocs);
            reader.Dispose();

            // merge the index down and check that the new doc count is correct
            writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())));
            Assert.AreEqual(60, writer.NumDocs());
            writer.ForceMerge(1);
            Assert.AreEqual(60, writer.MaxDoc);
            Assert.AreEqual(60, writer.NumDocs());
            writer.Dispose();

            // check that the index reader gives the same numbers.
            reader = DirectoryReader.Open(dir);
            Assert.AreEqual(60, reader.MaxDoc);
            Assert.AreEqual(60, reader.NumDocs);
            reader.Dispose();

            // make sure opening a new index for create over
            // this existing one works correctly:
            writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())).SetOpenMode(OpenMode_e.CREATE));
            Assert.AreEqual(0, writer.MaxDoc);
            Assert.AreEqual(0, writer.NumDocs());
            writer.Dispose();
            dir.Dispose();
        }

        /// <summary>
        /// LUCENENET specific
        /// Changed from internal static method to private to remove
        /// inter-dependencies between TestIndexWriter*.cs, TestAddIndexes.cs
        /// and TestDeletionPolicy.cs tests
        /// </summary>
        private void AddDoc(IndexWriter writer)
        {
            Document doc = new Document();
            doc.Add(NewTextField("content", "aaa", Field.Store.NO));
            writer.AddDocument(doc);
        }

        /// <summary>
        /// LUCENENET specific
        /// Changed from internal static method to private to remove
        /// inter-dependencies between TestIndexWriter*.cs, TestAddIndexes.cs
        /// and TestDeletionPolicy.cs tests
        /// </summary>
        private void AddDocWithIndex(IndexWriter writer, int index)
        {
            Document doc = new Document();
            doc.Add(NewField("content", "aaa " + index, StoredTextType));
            doc.Add(NewField("id", "" + index, StoredTextType));
            writer.AddDocument(doc);
        }

        public static void AssertNoUnreferencedFiles(Directory dir, string message)
        {
            string[] startFiles = dir.ListAll();
            (new IndexWriter(dir, new IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())))).Rollback();
            string[] endFiles = dir.ListAll();

            Array.Sort(startFiles);
            Array.Sort(endFiles);

            if (!Arrays.Equals(startFiles, endFiles))
            {
                Assert.Fail(message + ": before delete:\n    " + ArrayToString(startFiles) + "\n  after delete:\n    " + ArrayToString(endFiles));
            }
        }

        internal static string ArrayToString(string[] l)
        {
            string s = "";
            for (int i = 0; i < l.Length; i++)
            {
                if (i > 0)
                {
                    s += "\n    ";
                }
                s += l[i];
            }
            return s;
        }

        // Make sure we can open an index for create even when a
        // reader holds it open (this fails pre lock-less
        // commits on windows):
        [Test]
        public virtual void TestCreateWithReader()
        {
            Directory dir = NewDirectory();

            // add one document & close writer
            IndexWriter writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())));
            AddDoc(writer);
            writer.Dispose();

            // now open reader:
            IndexReader reader = DirectoryReader.Open(dir);
            Assert.AreEqual(reader.NumDocs, 1, "should be one document");

            // now open index for create:
            writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())).SetOpenMode(OpenMode_e.CREATE));
            Assert.AreEqual(writer.MaxDoc, 0, "should be zero documents");
            AddDoc(writer);
            writer.Dispose();

            Assert.AreEqual(reader.NumDocs, 1, "should be one document");
            IndexReader reader2 = DirectoryReader.Open(dir);
            Assert.AreEqual(reader2.NumDocs, 1, "should be one document");
            reader.Dispose();
            reader2.Dispose();

            dir.Dispose();
        }

        [Test]
        public virtual void TestChangesAfterClose([ValueSource(typeof(ConcurrentMergeSchedulers), "Values")]IConcurrentMergeScheduler scheduler)
        {
            Directory dir = NewDirectory();

            IndexWriter writer = null;

            var config = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())).SetMergeScheduler(scheduler);
            writer = new IndexWriter(dir, config);
            AddDoc(writer);

            // close
            writer.Dispose();
            try
            {
                AddDoc(writer);
                Assert.Fail("did not hit AlreadyClosedException");
            }
            catch (AlreadyClosedException e)
            {
                // expected
            }
            dir.Dispose();
        }

        [Test]
        public virtual void TestIndexNoDocuments()
        {
            Directory dir = NewDirectory();
            IndexWriter writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())));
            writer.Commit();
            writer.Dispose();

            IndexReader reader = DirectoryReader.Open(dir);
            Assert.AreEqual(0, reader.MaxDoc);
            Assert.AreEqual(0, reader.NumDocs);
            reader.Dispose();

            writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())).SetOpenMode(OpenMode_e.APPEND));
            writer.Commit();
            writer.Dispose();

            reader = DirectoryReader.Open(dir);
            Assert.AreEqual(0, reader.MaxDoc);
            Assert.AreEqual(0, reader.NumDocs);
            reader.Dispose();
            dir.Dispose();
        }

        [Test]
        public virtual void TestManyFields()
        {
            Directory dir = NewDirectory();
            IndexWriter writer = new IndexWriter(dir, (IndexWriterConfig)NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())).SetMaxBufferedDocs(10));
            for (int j = 0; j < 100; j++)
            {
                Document doc = new Document();
                doc.Add(NewField("a" + j, "aaa" + j, StoredTextType));
                doc.Add(NewField("b" + j, "aaa" + j, StoredTextType));
                doc.Add(NewField("c" + j, "aaa" + j, StoredTextType));
                doc.Add(NewField("d" + j, "aaa", StoredTextType));
                doc.Add(NewField("e" + j, "aaa", StoredTextType));
                doc.Add(NewField("f" + j, "aaa", StoredTextType));
                writer.AddDocument(doc);
            }
            writer.Dispose();

            IndexReader reader = DirectoryReader.Open(dir);
            Assert.AreEqual(100, reader.MaxDoc);
            Assert.AreEqual(100, reader.NumDocs);
            for (int j = 0; j < 100; j++)
            {
                Assert.AreEqual(1, reader.DocFreq(new Term("a" + j, "aaa" + j)));
                Assert.AreEqual(1, reader.DocFreq(new Term("b" + j, "aaa" + j)));
                Assert.AreEqual(1, reader.DocFreq(new Term("c" + j, "aaa" + j)));
                Assert.AreEqual(1, reader.DocFreq(new Term("d" + j, "aaa")));
                Assert.AreEqual(1, reader.DocFreq(new Term("e" + j, "aaa")));
                Assert.AreEqual(1, reader.DocFreq(new Term("f" + j, "aaa")));
            }
            reader.Dispose();
            dir.Dispose();
        }

        [Test]
        public virtual void TestSmallRAMBuffer()
        {
            Directory dir = NewDirectory();
            IndexWriter writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())).SetRAMBufferSizeMB(0.000001).SetMergePolicy(NewLogMergePolicy(10)));
            int lastNumFile = dir.ListAll().Length;
            for (int j = 0; j < 9; j++)
            {
                Document doc = new Document();
                doc.Add(NewField("field", "aaa" + j, StoredTextType));
                writer.AddDocument(doc);
                int numFile = dir.ListAll().Length;
                // Verify that with a tiny RAM buffer we see new
                // segment after every doc
                Assert.IsTrue(numFile > lastNumFile);
                lastNumFile = numFile;
            }
            writer.Dispose();
            dir.Dispose();
        }

        // Make sure it's OK to change RAM buffer size and
        // maxBufferedDocs in a write session
        [Test]
        public virtual void TestChangingRAMBuffer()
        {
            Directory dir = NewDirectory();
            IndexWriter writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())));
            writer.Config.SetMaxBufferedDocs(10);
            writer.Config.SetRAMBufferSizeMB(IndexWriterConfig.DISABLE_AUTO_FLUSH);

            int lastFlushCount = -1;
            for (int j = 1; j < 52; j++)
            {
                Document doc = new Document();
                doc.Add(new Field("field", "aaa" + j, StoredTextType));
                writer.AddDocument(doc);
                TestUtil.SyncConcurrentMerges(writer);
                int flushCount = writer.FlushCount;
                if (j == 1)
                {
                    lastFlushCount = flushCount;
                }
                else if (j < 10)
                // No new files should be created
                {
                    Assert.AreEqual(flushCount, lastFlushCount);
                }
                else if (10 == j)
                {
                    Assert.IsTrue(flushCount > lastFlushCount);
                    lastFlushCount = flushCount;
                    writer.Config.SetRAMBufferSizeMB(0.000001);
                    writer.Config.SetMaxBufferedDocs(IndexWriterConfig.DISABLE_AUTO_FLUSH);
                }
                else if (j < 20)
                {
                    Assert.IsTrue(flushCount > lastFlushCount);
                    lastFlushCount = flushCount;
                }
                else if (20 == j)
                {
                    writer.Config.SetRAMBufferSizeMB(16);
                    writer.Config.SetMaxBufferedDocs(IndexWriterConfig.DISABLE_AUTO_FLUSH);
                    lastFlushCount = flushCount;
                }
                else if (j < 30)
                {
                    Assert.AreEqual(flushCount, lastFlushCount);
                }
                else if (30 == j)
                {
                    writer.Config.SetRAMBufferSizeMB(0.000001);
                    writer.Config.SetMaxBufferedDocs(IndexWriterConfig.DISABLE_AUTO_FLUSH);
                }
                else if (j < 40)
                {
                    Assert.IsTrue(flushCount > lastFlushCount);
                    lastFlushCount = flushCount;
                }
                else if (40 == j)
                {
                    writer.Config.SetMaxBufferedDocs(10);
                    writer.Config.SetRAMBufferSizeMB(IndexWriterConfig.DISABLE_AUTO_FLUSH);
                    lastFlushCount = flushCount;
                }
                else if (j < 50)
                {
                    Assert.AreEqual(flushCount, lastFlushCount);
                    writer.Config.SetMaxBufferedDocs(10);
                    writer.Config.SetRAMBufferSizeMB(IndexWriterConfig.DISABLE_AUTO_FLUSH);
                }
                else if (50 == j)
                {
                    Assert.IsTrue(flushCount > lastFlushCount);
                }
            }
            writer.Dispose();
            dir.Dispose();
        }

        [Test]
        public virtual void TestChangingRAMBuffer2()
        {
            Directory dir = NewDirectory();
            IndexWriter writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())));
            writer.Config.SetMaxBufferedDocs(10);
            writer.Config.SetMaxBufferedDeleteTerms(10);
            writer.Config.SetRAMBufferSizeMB(IndexWriterConfig.DISABLE_AUTO_FLUSH);

            for (int j = 1; j < 52; j++)
            {
                Document doc = new Document();
                doc.Add(new Field("field", "aaa" + j, StoredTextType));
                writer.AddDocument(doc);
            }

            int lastFlushCount = -1;
            for (int j = 1; j < 52; j++)
            {
                writer.DeleteDocuments(new Term("field", "aaa" + j));
                TestUtil.SyncConcurrentMerges(writer);
                int flushCount = writer.FlushCount;

                if (j == 1)
                {
                    lastFlushCount = flushCount;
                }
                else if (j < 10)
                {
                    // No new files should be created
                    Assert.AreEqual(flushCount, lastFlushCount);
                }
                else if (10 == j)
                {
                    Assert.IsTrue(flushCount > lastFlushCount, "" + j);
                    lastFlushCount = flushCount;
                    writer.Config.SetRAMBufferSizeMB(0.000001);
                    writer.Config.SetMaxBufferedDeleteTerms(1);
                }
                else if (j < 20)
                {
                    Assert.IsTrue(flushCount > lastFlushCount);
                    lastFlushCount = flushCount;
                }
                else if (20 == j)
                {
                    writer.Config.SetRAMBufferSizeMB(16);
                    writer.Config.SetMaxBufferedDeleteTerms(IndexWriterConfig.DISABLE_AUTO_FLUSH);
                    lastFlushCount = flushCount;
                }
                else if (j < 30)
                {
                    Assert.AreEqual(flushCount, lastFlushCount);
                }
                else if (30 == j)
                {
                    writer.Config.SetRAMBufferSizeMB(0.000001);
                    writer.Config.SetMaxBufferedDeleteTerms(IndexWriterConfig.DISABLE_AUTO_FLUSH);
                    writer.Config.SetMaxBufferedDeleteTerms(1);
                }
                else if (j < 40)
                {
                    Assert.IsTrue(flushCount > lastFlushCount);
                    lastFlushCount = flushCount;
                }
                else if (40 == j)
                {
                    writer.Config.SetMaxBufferedDeleteTerms(10);
                    writer.Config.SetRAMBufferSizeMB(IndexWriterConfig.DISABLE_AUTO_FLUSH);
                    lastFlushCount = flushCount;
                }
                else if (j < 50)
                {
                    Assert.AreEqual(flushCount, lastFlushCount);
                    writer.Config.SetMaxBufferedDeleteTerms(10);
                    writer.Config.SetRAMBufferSizeMB(IndexWriterConfig.DISABLE_AUTO_FLUSH);
                }
                else if (50 == j)
                {
                    Assert.IsTrue(flushCount > lastFlushCount);
                }
            }
            writer.Dispose();
            dir.Dispose();
        }

        [Test]
        public virtual void TestDiverseDocs()
        {
            Directory dir = NewDirectory();
            IndexWriter writer = new IndexWriter(dir, (IndexWriterConfig)NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())).SetRAMBufferSizeMB(0.5));
            int n = AtLeast(1);
            for (int i = 0; i < n; i++)
            {
                // First, docs where every term is unique (heavy on
                // Posting instances)
                for (int j = 0; j < 100; j++)
                {
                    Document doc = new Document();
                    for (int k = 0; k < 100; k++)
                    {
                        doc.Add(NewField("field", Convert.ToString(Random().Next()), StoredTextType));
                    }
                    writer.AddDocument(doc);
                }

                // Next, many single term docs where only one term
                // occurs (heavy on byte blocks)
                for (int j = 0; j < 100; j++)
                {
                    Document doc = new Document();
                    doc.Add(NewField("field", "aaa aaa aaa aaa aaa aaa aaa aaa aaa aaa", StoredTextType));
                    writer.AddDocument(doc);
                }

                // Next, many single term docs where only one term
                // occurs but the terms are very long (heavy on
                // char[] arrays)
                for (int j = 0; j < 100; j++)
                {
                    StringBuilder b = new StringBuilder();
                    string x = Convert.ToString(j) + ".";
                    for (int k = 0; k < 1000; k++)
                    {
                        b.Append(x);
                    }
                    string longTerm = b.ToString();

                    Document doc = new Document();
                    doc.Add(NewField("field", longTerm, StoredTextType));
                    writer.AddDocument(doc);
                }
            }
            writer.Dispose();

            IndexReader reader = DirectoryReader.Open(dir);
            IndexSearcher searcher = NewSearcher(reader);
            int totalHits = searcher.Search(new TermQuery(new Term("field", "aaa")), null, 1).TotalHits;
            Assert.AreEqual(n * 100, totalHits);
            reader.Dispose();

            dir.Dispose();
        }

        [Test]
        public virtual void TestEnablingNorms()
        {
            Directory dir = NewDirectory();
            IndexWriter writer = new IndexWriter(dir, (IndexWriterConfig)NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())).SetMaxBufferedDocs(10));
            // Enable norms for only 1 doc, pre flush
            FieldType customType = new FieldType(TextField.TYPE_STORED);
            customType.OmitNorms = true;
            for (int j = 0; j < 10; j++)
            {
                Document doc = new Document();
                Field f = null;
                if (j != 8)
                {
                    f = NewField("field", "aaa", customType);
                }
                else
                {
                    f = NewField("field", "aaa", StoredTextType);
                }
                doc.Add(f);
                writer.AddDocument(doc);
            }
            writer.Dispose();

            Term searchTerm = new Term("field", "aaa");

            IndexReader reader = DirectoryReader.Open(dir);
            IndexSearcher searcher = NewSearcher(reader);
            ScoreDoc[] hits = searcher.Search(new TermQuery(searchTerm), null, 1000).ScoreDocs;
            Assert.AreEqual(10, hits.Length);
            reader.Dispose();

            writer = new IndexWriter(dir, (IndexWriterConfig)NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())).SetOpenMode(OpenMode_e.CREATE).SetMaxBufferedDocs(10));
            // Enable norms for only 1 doc, post flush
            for (int j = 0; j < 27; j++)
            {
                Document doc = new Document();
                Field f = null;
                if (j != 26)
                {
                    f = NewField("field", "aaa", customType);
                }
                else
                {
                    f = NewField("field", "aaa", StoredTextType);
                }
                doc.Add(f);
                writer.AddDocument(doc);
            }
            writer.Dispose();
            reader = DirectoryReader.Open(dir);
            searcher = NewSearcher(reader);
            hits = searcher.Search(new TermQuery(searchTerm), null, 1000).ScoreDocs;
            Assert.AreEqual(27, hits.Length);
            reader.Dispose();

            reader = DirectoryReader.Open(dir);
            reader.Dispose();

            dir.Dispose();
        }

        [Test]
        public virtual void TestHighFreqTerm()
        {
            Directory dir = NewDirectory();
            IndexWriter writer = new IndexWriter(dir, (IndexWriterConfig)NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())).SetRAMBufferSizeMB(0.01));
            // Massive doc that has 128 K a's
            StringBuilder b = new StringBuilder(1024 * 1024);
            for (int i = 0; i < 4096; i++)
            {
                b.Append(" a a a a a a a a");
                b.Append(" a a a a a a a a");
                b.Append(" a a a a a a a a");
                b.Append(" a a a a a a a a");
            }
            Document doc = new Document();
            FieldType customType = new FieldType(TextField.TYPE_STORED);
            customType.StoreTermVectors = true;
            customType.StoreTermVectorPositions = true;
            customType.StoreTermVectorOffsets = true;
            doc.Add(NewField("field", b.ToString(), customType));
            writer.AddDocument(doc);
            writer.Dispose();

            IndexReader reader = DirectoryReader.Open(dir);
            Assert.AreEqual(1, reader.MaxDoc);
            Assert.AreEqual(1, reader.NumDocs);
            Term t = new Term("field", "a");
            Assert.AreEqual(1, reader.DocFreq(t));
            DocsEnum td = TestUtil.Docs(Random(), reader, "field", new BytesRef("a"), MultiFields.GetLiveDocs(reader), null, DocsEnum.FLAG_FREQS);
            td.NextDoc();
            Assert.AreEqual(128 * 1024, td.Freq());
            reader.Dispose();
            dir.Dispose();
        }

        //Helper class for TestNullLockFactory
        public class MyRAMDirectory : MockDirectoryWrapper
        {
            private LockFactory myLockFactory;

            public MyRAMDirectory(Directory @delegate)
                : base(Random(), @delegate)
            {
                LockFactory_Renamed = null;
                myLockFactory = new SingleInstanceLockFactory();
            }

            public override Lock MakeLock(string name)
            {
                return myLockFactory.MakeLock(name);
            }
        }

        // Make sure that a Directory implementation that does
        // not use LockFactory at all (ie overrides makeLock and
        // implements its own private locking) works OK.  this
        // was raised on java-dev as loss of backwards
        // compatibility.
        [Test]
        public virtual void TestNullLockFactory()
        {
            Directory dir = new MyRAMDirectory(new RAMDirectory());
            IndexWriter writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())));
            for (int i = 0; i < 100; i++)
            {
                AddDoc(writer);
            }
            writer.Dispose();
            Term searchTerm = new Term("content", "aaa");
            IndexReader reader = DirectoryReader.Open(dir);
            IndexSearcher searcher = NewSearcher(reader);
            ScoreDoc[] hits = searcher.Search(new TermQuery(searchTerm), null, 1000).ScoreDocs;
            Assert.AreEqual(100, hits.Length, "did not get right number of hits");
            reader.Dispose();

            writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())).SetOpenMode(OpenMode_e.CREATE));
            writer.Dispose();
            dir.Dispose();
        }

        [Test]
        public virtual void TestFlushWithNoMerging()
        {
            Directory dir = NewDirectory();
            IndexWriter writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())).SetMaxBufferedDocs(2).SetMergePolicy(NewLogMergePolicy(10)));
            Document doc = new Document();
            FieldType customType = new FieldType(TextField.TYPE_STORED);
            customType.StoreTermVectors = true;
            customType.StoreTermVectorPositions = true;
            customType.StoreTermVectorOffsets = true;
            doc.Add(NewField("field", "aaa", customType));
            for (int i = 0; i < 19; i++)
            {
                writer.AddDocument(doc);
            }
            writer.Flush(false, true);
            writer.Dispose();
            SegmentInfos sis = new SegmentInfos();
            sis.Read(dir);
            // Since we flushed w/o allowing merging we should now
            // have 10 segments
            Assert.AreEqual(10, sis.Size());
            dir.Dispose();
        }

        // Make sure we can flush segment w/ norms, then add
        // empty doc (no norms) and flush
        [Test]
        public virtual void TestEmptyDocAfterFlushingRealDoc()
        {
            Directory dir = NewDirectory();
            IndexWriter writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())));
            Document doc = new Document();
            FieldType customType = new FieldType(TextField.TYPE_STORED);
            customType.StoreTermVectors = true;
            customType.StoreTermVectorPositions = true;
            customType.StoreTermVectorOffsets = true;
            doc.Add(NewField("field", "aaa", customType));
            writer.AddDocument(doc);
            writer.Commit();
            if (VERBOSE)
            {
                Console.WriteLine("\nTEST: now add empty doc");
            }
            writer.AddDocument(new Document());
            writer.Dispose();
            IndexReader reader = DirectoryReader.Open(dir);
            Assert.AreEqual(2, reader.NumDocs);
            reader.Dispose();
            dir.Dispose();
        }

        /// <summary>
        /// Test that no NullPointerException will be raised,
        /// when adding one document with a single, empty field
        /// and term vectors enabled.
        /// </summary>
        [Test]
        public virtual void TestBadSegment()
        {
            Directory dir = NewDirectory();
            IndexWriter iw = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())));

            Document document = new Document();
            FieldType customType = new FieldType(TextField.TYPE_NOT_STORED);
            customType.StoreTermVectors = true;
            document.Add(NewField("tvtest", "", customType));
            iw.AddDocument(document);
            iw.Dispose();
            dir.Dispose();
        }

#if !NETSTANDARD //NOTE: Cannot set ThreadPriority in .NET Core.
        // LUCENE-1036
        [Test]
        public virtual void TestMaxThreadPriority()
        {
            ThreadPriority pri = ThreadClass.Current().Priority;
            try
            {
                Directory dir = NewDirectory();
                IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())).SetMaxBufferedDocs(2).SetMergePolicy(NewLogMergePolicy());
                ((LogMergePolicy)conf.MergePolicy).MergeFactor = 2;
                IndexWriter iw = new IndexWriter(dir, conf);
                Document document = new Document();
                FieldType customType = new FieldType(TextField.TYPE_NOT_STORED);
                customType.StoreTermVectors = true;
                document.Add(NewField("tvtest", "a b c", customType));
                Thread.CurrentThread.Priority = ThreadPriority.Highest;
                for (int i = 0; i < 4; i++)
                {
                    iw.AddDocument(document);
                }
                iw.Dispose();
                dir.Dispose();
            }
            finally
            {
                Thread.CurrentThread.Priority = pri;
            }
        }
#endif

        [Test]
        public virtual void TestVariableSchema()
        {
            Directory dir = NewDirectory();
            for (int i = 0; i < 20; i++)
            {
                if (VERBOSE)
                {
                    Console.WriteLine("TEST: iter=" + i);
                }
                IndexWriter writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())).SetMaxBufferedDocs(2).SetMergePolicy(NewLogMergePolicy()));
                //LogMergePolicy lmp = (LogMergePolicy) writer.getConfig().getMergePolicy();
                //lmp.setMergeFactor(2);
                //lmp.setNoCFSRatio(0.0);
                Document doc = new Document();
                string contents = "aa bb cc dd ee ff gg hh ii jj kk";

                FieldType customType = new FieldType(TextField.TYPE_STORED);
                FieldType type = null;
                if (i == 7)
                {
                    // Add empty docs here
                    doc.Add(NewTextField("content3", "", Field.Store.NO));
                }
                else
                {
                    if (i % 2 == 0)
                    {
                        doc.Add(NewField("content4", contents, customType));
                        type = customType;
                    }
                    else
                    {
                        type = TextField.TYPE_NOT_STORED;
                    }
                    doc.Add(NewTextField("content1", contents, Field.Store.NO));
                    doc.Add(NewField("content3", "", customType));
                    doc.Add(NewField("content5", "", type));
                }

                for (int j = 0; j < 4; j++)
                {
                    writer.AddDocument(doc);
                }

                writer.Dispose();

                if (0 == i % 4)
                {
                    writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())));
                    //LogMergePolicy lmp2 = (LogMergePolicy) writer.getConfig().getMergePolicy();
                    //lmp2.setNoCFSRatio(0.0);
                    writer.ForceMerge(1);
                    writer.Dispose();
                }
            }
            dir.Dispose();
        }

        // LUCENE-1084: test unlimited field length
        [Test]
        public virtual void TestUnlimitedMaxFieldLength()
        {
            Directory dir = NewDirectory();

            IndexWriter writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())));

            Document doc = new Document();
            StringBuilder b = new StringBuilder();
            for (int i = 0; i < 10000; i++)
            {
                b.Append(" a");
            }
            b.Append(" x");
            doc.Add(NewTextField("field", b.ToString(), Field.Store.NO));
            writer.AddDocument(doc);
            writer.Dispose();

            IndexReader reader = DirectoryReader.Open(dir);
            Term t = new Term("field", "x");
            Assert.AreEqual(1, reader.DocFreq(t));
            reader.Dispose();
            dir.Dispose();
        }

        // LUCENE-1179
        [Test]
        public virtual void TestEmptyFieldName()
        {
            Directory dir = NewDirectory();
            IndexWriter writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())));
            Document doc = new Document();
            doc.Add(NewTextField("", "a b c", Field.Store.NO));
            writer.AddDocument(doc);
            writer.Dispose();
            dir.Dispose();
        }

        [Test]
        public virtual void TestEmptyFieldNameTerms()
        {
            Directory dir = NewDirectory();
            IndexWriter writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())));
            Document doc = new Document();
            doc.Add(NewTextField("", "a b c", Field.Store.NO));
            writer.AddDocument(doc);
            writer.Dispose();
            DirectoryReader reader = DirectoryReader.Open(dir);
            AtomicReader subreader = GetOnlySegmentReader(reader);
            TermsEnum te = subreader.Fields.Terms("").Iterator(null);
            Assert.AreEqual(new BytesRef("a"), te.Next());
            Assert.AreEqual(new BytesRef("b"), te.Next());
            Assert.AreEqual(new BytesRef("c"), te.Next());
            Assert.IsNull(te.Next());
            reader.Dispose();
            dir.Dispose();
        }

        [Test]
        public virtual void TestEmptyFieldNameWithEmptyTerm()
        {
            Directory dir = NewDirectory();
            IndexWriter writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())));
            Document doc = new Document();
            doc.Add(NewStringField("", "", Field.Store.NO));
            doc.Add(NewStringField("", "a", Field.Store.NO));
            doc.Add(NewStringField("", "b", Field.Store.NO));
            doc.Add(NewStringField("", "c", Field.Store.NO));
            writer.AddDocument(doc);
            writer.Dispose();
            DirectoryReader reader = DirectoryReader.Open(dir);
            AtomicReader subreader = GetOnlySegmentReader(reader);
            TermsEnum te = subreader.Fields.Terms("").Iterator(null);
            Assert.AreEqual(new BytesRef(""), te.Next());
            Assert.AreEqual(new BytesRef("a"), te.Next());
            Assert.AreEqual(new BytesRef("b"), te.Next());
            Assert.AreEqual(new BytesRef("c"), te.Next());
            Assert.IsNull(te.Next());
            reader.Dispose();
            dir.Dispose();
        }

        private sealed class MockIndexWriter : IndexWriter
        {
            public MockIndexWriter(Directory dir, IndexWriterConfig conf)
                : base(dir, conf)
            {
            }

            internal bool AfterWasCalled;
            internal bool BeforeWasCalled;

            protected internal override void DoAfterFlush()
            {
                AfterWasCalled = true;
            }

            protected internal override void DoBeforeFlush()
            {
                BeforeWasCalled = true;
            }
        }

        // LUCENE-1222
        [Test]
        public virtual void TestDoBeforeAfterFlush()
        {
            Directory dir = NewDirectory();
            MockIndexWriter w = new MockIndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())));
            Document doc = new Document();
            FieldType customType = new FieldType(TextField.TYPE_STORED);
            doc.Add(NewField("field", "a field", customType));
            w.AddDocument(doc);
            w.Commit();
            Assert.IsTrue(w.BeforeWasCalled);
            Assert.IsTrue(w.AfterWasCalled);
            w.BeforeWasCalled = false;
            w.AfterWasCalled = false;
            w.DeleteDocuments(new Term("field", "field"));
            w.Commit();
            Assert.IsTrue(w.BeforeWasCalled);
            Assert.IsTrue(w.AfterWasCalled);
            w.Dispose();

            IndexReader ir = DirectoryReader.Open(dir);
            Assert.AreEqual(0, ir.NumDocs);
            ir.Dispose();

            dir.Dispose();
        }

        // LUCENE-1255
        [Test]
        public virtual void TestNegativePositions()
        {
            TokenStream tokens = new TokenStreamAnonymousInnerClassHelper(this);

            Directory dir = NewDirectory();
            IndexWriter w = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())));
            Document doc = new Document();
            doc.Add(new TextField("field", tokens));
            try
            {
                w.AddDocument(doc);
                Assert.Fail("did not hit expected exception");
            }
            catch (System.ArgumentException iea)
            {
                // expected
            }
            w.Dispose();
            dir.Dispose();
        }

        private class TokenStreamAnonymousInnerClassHelper : TokenStream
        {
            private readonly TestIndexWriter OuterInstance;

            public TokenStreamAnonymousInnerClassHelper(TestIndexWriter outerInstance)
            {
                this.OuterInstance = outerInstance;
                termAtt = AddAttribute<ICharTermAttribute>();
                posIncrAtt = AddAttribute<IPositionIncrementAttribute>();
                terms = Arrays.AsList("a", "b", "c").GetEnumerator();
                first = true;
            }

            internal readonly ICharTermAttribute termAtt;
            internal readonly IPositionIncrementAttribute posIncrAtt;

            internal readonly IEnumerator<string> terms;
            internal bool first;

            public sealed override bool IncrementToken()
            {
                if (!terms.MoveNext())
                {
                    return false;
                }
                ClearAttributes();
                termAtt.Append(terms.Current);
                posIncrAtt.PositionIncrement = first ? 0 : 1;
                first = false;
                return true;
            }
        }

        // LUCENE-2529
        [Test]
        public virtual void TestPositionIncrementGapEmptyField()
        {
            Directory dir = NewDirectory();
            MockAnalyzer analyzer = new MockAnalyzer(Random());
            analyzer.PositionIncrementGap = 100;
            IndexWriter w = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer));
            Document doc = new Document();
            FieldType customType = new FieldType(TextField.TYPE_NOT_STORED);
            customType.StoreTermVectors = true;
            customType.StoreTermVectorPositions = true;
            Field f = NewField("field", "", customType);
            Field f2 = NewField("field", "crunch man", customType);
            doc.Add(f);
            doc.Add(f2);
            w.AddDocument(doc);
            w.Dispose();

            IndexReader r = DirectoryReader.Open(dir);
            Terms tpv = r.GetTermVectors(0).Terms("field");
            TermsEnum termsEnum = tpv.Iterator(null);
            Assert.IsNotNull(termsEnum.Next());
            DocsAndPositionsEnum dpEnum = termsEnum.DocsAndPositions(null, null);
            Assert.IsNotNull(dpEnum);
            Assert.IsTrue(dpEnum.NextDoc() != DocIdSetIterator.NO_MORE_DOCS);
            Assert.AreEqual(1, dpEnum.Freq());
            Assert.AreEqual(100, dpEnum.NextPosition());

            Assert.IsNotNull(termsEnum.Next());
            dpEnum = termsEnum.DocsAndPositions(null, dpEnum);
            Assert.IsNotNull(dpEnum);
            Assert.IsTrue(dpEnum.NextDoc() != DocIdSetIterator.NO_MORE_DOCS);
            Assert.AreEqual(1, dpEnum.Freq());
            Assert.AreEqual(101, dpEnum.NextPosition());
            Assert.IsNull(termsEnum.Next());

            r.Dispose();
            dir.Dispose();
        }

        [Test]
        public virtual void TestDeadlock()
        {
            Directory dir = NewDirectory();
            IndexWriter writer = new IndexWriter(dir, (IndexWriterConfig)NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())).SetMaxBufferedDocs(2));
            Document doc = new Document();

            FieldType customType = new FieldType(TextField.TYPE_STORED);
            customType.StoreTermVectors = true;
            customType.StoreTermVectorPositions = true;
            customType.StoreTermVectorOffsets = true;

            doc.Add(NewField("content", "aaa bbb ccc ddd eee fff ggg hhh iii", customType));
            writer.AddDocument(doc);
            writer.AddDocument(doc);
            writer.AddDocument(doc);
            writer.Commit();
            // index has 2 segments

            Directory dir2 = NewDirectory();
            IndexWriter writer2 = new IndexWriter(dir2, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())));
            writer2.AddDocument(doc);
            writer2.Dispose();

            IndexReader r1 = DirectoryReader.Open(dir2);
            writer.AddIndexes(r1, r1);
            writer.Dispose();

            IndexReader r3 = DirectoryReader.Open(dir);
            Assert.AreEqual(5, r3.NumDocs);
            r3.Dispose();

            r1.Dispose();

            dir2.Dispose();
            dir.Dispose();
        }

        private class IndexerThreadInterrupt : ThreadClass
        {
            private readonly TestIndexWriter OuterInstance;

            internal volatile bool Failed;
            internal volatile bool Finish;

            internal volatile bool AllowInterrupt = false;
            internal readonly Random Random;
            internal readonly Directory Adder;

            internal IndexerThreadInterrupt(TestIndexWriter outerInstance)
            {
                this.OuterInstance = outerInstance;
                this.Random = new Random(Random().Next());
                // make a little directory for addIndexes
                // LUCENE-2239: won't work with NIOFS/MMAP
                Adder = new MockDirectoryWrapper(this.Random, new RAMDirectory());
                IndexWriterConfig conf = OuterInstance.NewIndexWriterConfig(this.Random, TEST_VERSION_CURRENT, new MockAnalyzer(this.Random));
                IndexWriter w = new IndexWriter(Adder, conf);
                Document doc = new Document();
                doc.Add(OuterInstance.NewStringField(this.Random, "id", "500", Field.Store.NO));
                doc.Add(OuterInstance.NewField(this.Random, "field", "some prepackaged text contents", StoredTextType));
                if (DefaultCodecSupportsDocValues())
                {
                    doc.Add(new BinaryDocValuesField("binarydv", new BytesRef("500")));
                    doc.Add(new NumericDocValuesField("numericdv", 500));
                    doc.Add(new SortedDocValuesField("sorteddv", new BytesRef("500")));
                }
                if (DefaultCodecSupportsSortedSet())
                {
                    doc.Add(new SortedSetDocValuesField("sortedsetdv", new BytesRef("one")));
                    doc.Add(new SortedSetDocValuesField("sortedsetdv", new BytesRef("two")));
                }
                w.AddDocument(doc);
                doc = new Document();
                doc.Add(OuterInstance.NewStringField(this.Random, "id", "501", Field.Store.NO));
                doc.Add(OuterInstance.NewField(this.Random, "field", "some more contents", StoredTextType));
                if (DefaultCodecSupportsDocValues())
                {
                    doc.Add(new BinaryDocValuesField("binarydv", new BytesRef("501")));
                    doc.Add(new NumericDocValuesField("numericdv", 501));
                    doc.Add(new SortedDocValuesField("sorteddv", new BytesRef("501")));
                }
                if (DefaultCodecSupportsSortedSet())
                {
                    doc.Add(new SortedSetDocValuesField("sortedsetdv", new BytesRef("two")));
                    doc.Add(new SortedSetDocValuesField("sortedsetdv", new BytesRef("three")));
                }
                w.AddDocument(doc);
                w.DeleteDocuments(new Term("id", "500"));
                w.Dispose();
            }

            public override void Run()
            {
                // LUCENE-2239: won't work with NIOFS/MMAP
                MockDirectoryWrapper dir = new MockDirectoryWrapper(Random, new RAMDirectory());

                // When interrupt arrives in w.Dispose(), when it's
                // writing liveDocs, this can lead to double-write of
                // _X_N.del:
                //dir.setPreventDoubleWrite(false);
                IndexWriter w = null;
                while (!Finish)
                {
                    try
                    {
                        while (!Finish)
                        {
                            if (w != null)
                            {
                                // If interrupt arrives inside here, it's
                                // fine: we will cycle back and the first
                                // thing we do is try to close again,
                                // i.e. we'll never try to open a new writer
                                // until this one successfully closes:
                                w.Dispose();
                                w = null;
                            }
                            IndexWriterConfig conf = OuterInstance.NewIndexWriterConfig(Random, TEST_VERSION_CURRENT, new MockAnalyzer(Random)).SetMaxBufferedDocs(2);
                            w = new IndexWriter(dir, conf);

                            Document doc = new Document();
                            Field idField = OuterInstance.NewStringField(Random, "id", "", Field.Store.NO);
                            Field binaryDVField = null;
                            Field numericDVField = null;
                            Field sortedDVField = null;
                            Field sortedSetDVField = new SortedSetDocValuesField("sortedsetdv", new BytesRef());
                            doc.Add(idField);
                            doc.Add(OuterInstance.NewField(Random, "field", "some text contents", StoredTextType));
                            if (DefaultCodecSupportsDocValues())
                            {
                                binaryDVField = new BinaryDocValuesField("binarydv", new BytesRef());
                                numericDVField = new NumericDocValuesField("numericdv", 0);
                                sortedDVField = new SortedDocValuesField("sorteddv", new BytesRef());
                                doc.Add(binaryDVField);
                                doc.Add(numericDVField);
                                doc.Add(sortedDVField);
                            }
                            if (DefaultCodecSupportsSortedSet())
                            {
                                doc.Add(sortedSetDVField);
                            }
                            for (int i = 0; i < 100; i++)
                            {
                                idField.StringValue = Convert.ToString(i);
                                if (DefaultCodecSupportsDocValues())
                                {
                                    binaryDVField.BytesValue = new BytesRef(idField.StringValue);
                                    numericDVField.LongValue = i;
                                    sortedDVField.BytesValue = new BytesRef(idField.StringValue);
                                }
                                sortedSetDVField.BytesValue = new BytesRef(idField.StringValue);
                                int action = Random.Next(100);
                                if (action == 17)
                                {
                                    w.AddIndexes(Adder);
                                }
                                else if (action % 30 == 0)
                                {
                                    w.DeleteAll();
                                }
                                else if (action % 2 == 0)
                                {
                                    w.UpdateDocument(new Term("id", idField.StringValue), doc);
                                }
                                else
                                {
                                    w.AddDocument(doc);
                                }
                                if (Random.Next(3) == 0)
                                {
                                    IndexReader r = null;
                                    try
                                    {
                                        r = DirectoryReader.Open(w, Random.NextBoolean());
                                        if (Random.NextBoolean() && r.MaxDoc > 0)
                                        {
                                            int docid = Random.Next(r.MaxDoc);
                                            w.TryDeleteDocument(r, docid);
                                        }
                                    }
                                    finally
                                    {
                                        IOUtils.CloseWhileHandlingException(r);
                                    }
                                }
                                if (i % 10 == 0)
                                {
                                    w.Commit();
                                }
                                if (Random.Next(50) == 0)
                                {
                                    w.ForceMerge(1);
                                }
                            }
                            w.Dispose();
                            w = null;
                            DirectoryReader.Open(dir).Dispose();

                            // Strangely, if we interrupt a thread before
                            // all classes are loaded, the class loader
                            // seems to do scary things with the interrupt
                            // status.  In java 1.5, it'll throw an
                            // incorrect ClassNotFoundException.  In java
                            // 1.6, it'll silently clear the interrupt.
                            // So, on first iteration through here we
                            // don't open ourselves up for interrupts
                            // until we've done the above loop.
                            AllowInterrupt = true;
                        }
                    }
#if !NETSTANDARD
                    catch (ThreadInterruptedException re)
                    {
                        // NOTE: important to leave this verbosity/noise
                        // on!!  this test doesn't repro easily so when
                        // Jenkins hits a fail we need to study where the
                        // interrupts struck!
                        Console.WriteLine("TEST: got interrupt");
                        Console.WriteLine(re.StackTrace);
                        Exception e = re.InnerException;
                        Assert.IsTrue(e is ThreadInterruptedException);
                        if (Finish)
                        {
                            break;
                        }
                    }
#endif
                    catch (Exception t)
                    {
                        Console.WriteLine("FAILED; unexpected exception");
                        Console.WriteLine(t.StackTrace);
                        Failed = true;
                        break;
                    }
                }

                if (!Failed)
                {
                    if (VERBOSE)
                    {
                        Console.WriteLine("TEST: now rollback");
                    }
                    // clear interrupt state:
                    //Thread.interrupted();
                    if (w != null)
                    {
                        try
                        {
                            w.Rollback();
                        }
                        catch (IOException ioe)
                        {
                            throw new Exception(ioe.Message, ioe);
                        }
                    }

                    try
                    {
                        TestUtil.CheckIndex(dir);
                    }
                    catch (Exception e)
                    {
                        Failed = true;
                        Console.WriteLine("CheckIndex FAILED: unexpected exception");
                        Console.WriteLine(e.StackTrace);
                    }
                    try
                    {
                        IndexReader r = DirectoryReader.Open(dir);
                        //System.out.println("doc count=" + r.NumDocs);
                        r.Dispose();
                    }
                    catch (Exception e)
                    {
                        Failed = true;
                        Console.WriteLine("DirectoryReader.open FAILED: unexpected exception");
                        Console.WriteLine(e.StackTrace);
                    }
                }
                try
                {
                    IOUtils.Close(dir);
                }
                catch (IOException e)
                {
                    throw new Exception(e.Message, e);
                }
                try
                {
                    IOUtils.Close(Adder);
                }
                catch (IOException e)
                {
                    throw new Exception(e.Message, e);
                }
            }
        }

        [Test]
        public virtual void TestThreadInterruptDeadlock()
        {
            IndexerThreadInterrupt t = new IndexerThreadInterrupt(this);
            t.SetDaemon(true);
            t.Start();

            // Force class loader to load ThreadInterruptedException
            // up front... else we can see a false failure if 2nd
            // interrupt arrives while class loader is trying to
            // init this class (in servicing a first interrupt):
            //Assert.IsTrue((new ThreadInterruptedException(new Exception("Thread interrupted"))).InnerException is ThreadInterruptedException);

            // issue 300 interrupts to child thread
            int numInterrupts = AtLeast(300);
            int i = 0;
            while (i < numInterrupts)
            {
                // TODO: would be nice to also sometimes interrupt the
                // CMS merge threads too ...
                Thread.Sleep(10);
                if (t.AllowInterrupt)
                {
                    i++;
                    t.Interrupt();
                }
                if (!t.IsAlive)
                {
                    break;
                }
            }
            t.Finish = true;
            t.Join();
            Assert.IsFalse(t.Failed);
        }

        /// <summary>
        /// testThreadInterruptDeadlock but with 2 indexer threads </summary>
        [Test]
        public virtual void TestTwoThreadsInterruptDeadlock()
        {
            IndexerThreadInterrupt t1 = new IndexerThreadInterrupt(this);
            t1.SetDaemon(true);
            t1.Start();

            IndexerThreadInterrupt t2 = new IndexerThreadInterrupt(this);
            t2.SetDaemon(true);
            t2.Start();

            // Force class loader to load ThreadInterruptedException
            // up front... else we can see a false failure if 2nd
            // interrupt arrives while class loader is trying to
            // init this class (in servicing a first interrupt):
            // C# does not have the late load problem.
            //Assert.IsTrue((new ThreadInterruptedException(new Exception("Thread interrupted"))).InnerException is ThreadInterruptedException);

            // issue 300 interrupts to child thread
            int numInterrupts = AtLeast(300);
            int i = 0;
            while (i < numInterrupts)
            {
                // TODO: would be nice to also sometimes interrupt the
                // CMS merge threads too ...
                Thread.Sleep(10);
                IndexerThreadInterrupt t = Random().NextBoolean() ? t1 : t2;
                if (t.AllowInterrupt)
                {
                    i++;
                    t.Interrupt();
                }
                if (!t1.IsAlive && !t2.IsAlive)
                {
                    break;
                }
            }
            t1.Finish = true;
            t2.Finish = true;
            t1.Join();
            t2.Join();
            Assert.IsFalse(t1.Failed);
            Assert.IsFalse(t2.Failed);
        }

        [Test]
        public virtual void TestIndexStoreCombos()
        {
            Directory dir = NewDirectory();
            IndexWriter w = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())));
            var b = new byte[50];
            for (int i = 0; i < 50; i++)
            {
                b[i] = (byte)(i + 77);
            }

            Document doc = new Document();

            FieldType customType = new FieldType(StoredField.TYPE);
            customType.Tokenized = true;

            Field f = new Field("binary", b, 10, 17, customType);
            customType.Indexed = true;
            f.TokenStream = new MockTokenizer(new StringReader("doc1field1"), MockTokenizer.WHITESPACE, false);

            FieldType customType2 = new FieldType(TextField.TYPE_STORED);

            Field f2 = NewField("string", "value", customType2);
            f2.TokenStream = new MockTokenizer(new StringReader("doc1field2"), MockTokenizer.WHITESPACE, false);
            doc.Add(f);
            doc.Add(f2);
            w.AddDocument(doc);

            // add 2 docs to test in-memory merging
            f.TokenStream = new MockTokenizer(new StringReader("doc2field1"), MockTokenizer.WHITESPACE, false);
            f2.TokenStream = new MockTokenizer(new StringReader("doc2field2"), MockTokenizer.WHITESPACE, false);
            w.AddDocument(doc);

            // force segment flush so we can force a segment merge with doc3 later.
            w.Commit();

            f.TokenStream = new MockTokenizer(new StringReader("doc3field1"), MockTokenizer.WHITESPACE, false);
            f2.TokenStream = new MockTokenizer(new StringReader("doc3field2"), MockTokenizer.WHITESPACE, false);

            w.AddDocument(doc);
            w.Commit();
            w.ForceMerge(1); // force segment merge.
            w.Dispose();

            IndexReader ir = DirectoryReader.Open(dir);
            Document doc2 = ir.Document(0);
            IndexableField f3 = doc2.GetField("binary");
            b = f3.BinaryValue.Bytes;
            Assert.IsTrue(b != null);
            Assert.AreEqual(17, b.Length, 17);
            Assert.AreEqual(87, b[0]);

            Assert.IsTrue(ir.Document(0).GetField("binary").BinaryValue != null);
            Assert.IsTrue(ir.Document(1).GetField("binary").BinaryValue != null);
            Assert.IsTrue(ir.Document(2).GetField("binary").BinaryValue != null);

            Assert.AreEqual("value", ir.Document(0).Get("string"));
            Assert.AreEqual("value", ir.Document(1).Get("string"));
            Assert.AreEqual("value", ir.Document(2).Get("string"));

            // test that the terms were indexed.
            Assert.IsTrue(TestUtil.Docs(Random(), ir, "binary", new BytesRef("doc1field1"), null, null, DocsEnum.FLAG_NONE).NextDoc() != DocIdSetIterator.NO_MORE_DOCS);
            Assert.IsTrue(TestUtil.Docs(Random(), ir, "binary", new BytesRef("doc2field1"), null, null, DocsEnum.FLAG_NONE).NextDoc() != DocIdSetIterator.NO_MORE_DOCS);
            Assert.IsTrue(TestUtil.Docs(Random(), ir, "binary", new BytesRef("doc3field1"), null, null, DocsEnum.FLAG_NONE).NextDoc() != DocIdSetIterator.NO_MORE_DOCS);
            Assert.IsTrue(TestUtil.Docs(Random(), ir, "string", new BytesRef("doc1field2"), null, null, DocsEnum.FLAG_NONE).NextDoc() != DocIdSetIterator.NO_MORE_DOCS);
            Assert.IsTrue(TestUtil.Docs(Random(), ir, "string", new BytesRef("doc2field2"), null, null, DocsEnum.FLAG_NONE).NextDoc() != DocIdSetIterator.NO_MORE_DOCS);
            Assert.IsTrue(TestUtil.Docs(Random(), ir, "string", new BytesRef("doc3field2"), null, null, DocsEnum.FLAG_NONE).NextDoc() != DocIdSetIterator.NO_MORE_DOCS);

            ir.Dispose();
            dir.Dispose();
        }

        [Test]
        public virtual void TestNoDocsIndex()
        {
            Directory dir = NewDirectory();
            IndexWriter writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())));
            writer.AddDocument(new Document());
            writer.Dispose();

            dir.Dispose();
        }

        [Test]
        public virtual void TestIndexDivisor()
        {
            Directory dir = NewDirectory();
            IndexWriterConfig config = new IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random()));
            config.SetTermIndexInterval(2);
            IndexWriter w = new IndexWriter(dir, config);
            StringBuilder s = new StringBuilder();
            // must be > 256
            for (int i = 0; i < 300; i++)
            {
                s.Append(' ').Append(i);
            }
            Document d = new Document();
            Field f = NewTextField("field", s.ToString(), Field.Store.NO);
            d.Add(f);
            w.AddDocument(d);

            AtomicReader r = GetOnlySegmentReader(w.Reader);
            TermsEnum t = r.Fields.Terms("field").Iterator(null);
            int count = 0;
            while (t.Next() != null)
            {
                DocsEnum docs = TestUtil.Docs(Random(), t, null, null, DocsEnum.FLAG_NONE);
                Assert.AreEqual(0, docs.NextDoc());
                Assert.AreEqual(DocIdSetIterator.NO_MORE_DOCS, docs.NextDoc());
                count++;
            }
            Assert.AreEqual(300, count);
            r.Dispose();
            w.Dispose();
            dir.Dispose();
        }

        [Test]
        public virtual void TestDeleteUnusedFiles()
        {
            for (int iter = 0; iter < 2; iter++)
            {
                Directory dir = NewMockDirectory(); // relies on windows semantics

                MergePolicy mergePolicy = NewLogMergePolicy(true);

                // this test expects all of its segments to be in CFS
                mergePolicy.NoCFSRatio = 1.0;
                mergePolicy.MaxCFSSegmentSizeMB = double.PositiveInfinity;

                IndexWriter w = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())).SetMergePolicy(mergePolicy).SetUseCompoundFile(true));
                Document doc = new Document();
                doc.Add(NewTextField("field", "go", Field.Store.NO));
                w.AddDocument(doc);
                DirectoryReader r;
                if (iter == 0)
                {
                    // use NRT
                    r = w.Reader;
                }
                else
                {
                    // don't use NRT
                    w.Commit();
                    r = DirectoryReader.Open(dir);
                }

                IList<string> files = new List<string>(Arrays.AsList(dir.ListAll()));

                // RAMDir won't have a write.lock, but fs dirs will:
                files.Remove("write.lock");

                Assert.IsTrue(files.Contains("_0.cfs"));
                Assert.IsTrue(files.Contains("_0.cfe"));
                Assert.IsTrue(files.Contains("_0.si"));
                if (iter == 1)
                {
                    // we run a full commit so there should be a segments file etc.
                    Assert.IsTrue(files.Contains("segments_1"));
                    Assert.IsTrue(files.Contains("segments.gen"));
                    Assert.AreEqual(files.Count, 5, files.ToString());
                }
                else
                {
                    // this is an NRT reopen - no segments files yet

                    Assert.AreEqual(files.Count, 3, files.ToString());
                }
                w.AddDocument(doc);
                w.ForceMerge(1);
                if (iter == 1)
                {
                    w.Commit();
                }
                IndexReader r2 = DirectoryReader.OpenIfChanged(r);
                Assert.IsNotNull(r2);
                Assert.IsTrue(r != r2);
                files = Arrays.AsList(dir.ListAll());

                // NOTE: here we rely on "Windows" behavior, ie, even
                // though IW wanted to delete _0.cfs since it was
                // merged away, because we have a reader open
                // against this file, it should still be here:
                Assert.IsTrue(files.Contains("_0.cfs"));
                // forceMerge created this
                //Assert.IsTrue(files.Contains("_2.cfs"));
                w.DeleteUnusedFiles();

                files = Arrays.AsList(dir.ListAll());
                // r still holds this file open
                Assert.IsTrue(files.Contains("_0.cfs"));
                //Assert.IsTrue(files.Contains("_2.cfs"));

                r.Dispose();
                if (iter == 0)
                {
                    // on closing NRT reader, it calls writer.deleteUnusedFiles
                    files = Arrays.AsList(dir.ListAll());
                    Assert.IsFalse(files.Contains("_0.cfs"));
                }
                else
                {
                    // now writer can remove it
                    w.DeleteUnusedFiles();
                    files = Arrays.AsList(dir.ListAll());
                    Assert.IsFalse(files.Contains("_0.cfs"));
                }
                //Assert.IsTrue(files.Contains("_2.cfs"));

                w.Dispose();
                r2.Dispose();

                dir.Dispose();
            }
        }

        [Test]
        public virtual void TestDeleteUnsedFiles2()
        {
            // Validates that iw.DeleteUnusedFiles() also deletes unused index commits
            // in case a deletion policy which holds onto commits is used.
            Directory dir = NewDirectory();
            IndexWriter writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())).SetIndexDeletionPolicy(new SnapshotDeletionPolicy(new KeepOnlyLastCommitDeletionPolicy())));
            SnapshotDeletionPolicy sdp = (SnapshotDeletionPolicy)writer.Config.DelPolicy;

            // First commit
            Document doc = new Document();

            FieldType customType = new FieldType(TextField.TYPE_STORED);
            customType.StoreTermVectors = true;
            customType.StoreTermVectorPositions = true;
            customType.StoreTermVectorOffsets = true;

            doc.Add(NewField("c", "val", customType));
            writer.AddDocument(doc);
            writer.Commit();
            Assert.AreEqual(1, DirectoryReader.ListCommits(dir).Count);

            // Keep that commit
            IndexCommit id = sdp.Snapshot();

            // Second commit - now KeepOnlyLastCommit cannot delete the prev commit.
            doc = new Document();
            doc.Add(NewField("c", "val", customType));
            writer.AddDocument(doc);
            writer.Commit();
            Assert.AreEqual(2, DirectoryReader.ListCommits(dir).Count);

            // Should delete the unreferenced commit
            sdp.Release(id);
            writer.DeleteUnusedFiles();
            Assert.AreEqual(1, DirectoryReader.ListCommits(dir).Count);

            writer.Dispose();
            dir.Dispose();
        }

        [Test]
        public virtual void TestEmptyFSDirWithNoLock()
        {
            // Tests that if FSDir is opened w/ a NoLockFactory (or SingleInstanceLF),
            // then IndexWriter ctor succeeds. Previously (LUCENE-2386) it failed
            // when listAll() was called in IndexFileDeleter.
            Directory dir = NewFSDirectory(CreateTempDir("emptyFSDirNoLock"), NoLockFactory.DoNoLockFactory);
            (new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())))).Dispose();
            dir.Dispose();
        }

        [Test]
        public virtual void TestEmptyDirRollback()
        {
            // TODO: generalize this test
            //AssumeFalse("test makes assumptions about file counts", Codec.Default is SimpleTextCodec);

            // Tests that if IW is created over an empty Directory, some documents are
            // indexed, flushed (but not committed) and then IW rolls back, then no
            // files are left in the Directory.
            Directory dir = NewDirectory();
            IndexWriter writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())).SetMaxBufferedDocs(2).SetMergePolicy(NewLogMergePolicy()).SetUseCompoundFile(false));
            string[] files = dir.ListAll();

            // Creating over empty dir should not create any files,
            // or, at most the write.lock file
            int extraFileCount;
            if (files.Length == 1)
            {
                Assert.IsTrue(files[0].EndsWith("write.lock"));
                extraFileCount = 1;
            }
            else
            {
                Assert.AreEqual(0, files.Length);
                extraFileCount = 0;
            }

            Document doc = new Document();
            FieldType customType = new FieldType(TextField.TYPE_STORED);
            customType.StoreTermVectors = true;
            customType.StoreTermVectorPositions = true;
            customType.StoreTermVectorOffsets = true;
            // create as many files as possible
            doc.Add(NewField("c", "val", customType));
            writer.AddDocument(doc);
            // Adding just one document does not call flush yet.
            int computedExtraFileCount = 0;
            foreach (string file in dir.ListAll())
            {
                if (file.LastIndexOf('.') < 0 || !Arrays.AsList("fdx", "fdt", "tvx", "tvd", "tvf").Contains(file.Substring(file.LastIndexOf('.') + 1)))
                // don't count stored fields and term vectors in
                {
                    ++computedExtraFileCount;
                }
            }
            Assert.AreEqual(extraFileCount, computedExtraFileCount, "only the stored and term vector files should exist in the directory");

            doc = new Document();
            doc.Add(NewField("c", "val", customType));
            writer.AddDocument(doc);

            // The second document should cause a flush.
            Assert.IsTrue(dir.ListAll().Length > 5 + extraFileCount, "flush should have occurred and files should have been created");

            // After rollback, IW should remove all files
            writer.Rollback();
            string[] allFiles = dir.ListAll();
            Assert.IsTrue(allFiles.Length == 0 || Arrays.Equals(allFiles, new string[] { IndexWriter.WRITE_LOCK_NAME }), "no files should exist in the directory after rollback");

            // Since we rolled-back above, that close should be a no-op
            writer.Dispose();
            allFiles = dir.ListAll();
            Assert.IsTrue(allFiles.Length == 0 || Arrays.Equals(allFiles, new string[] { IndexWriter.WRITE_LOCK_NAME }), "expected a no-op close after IW.Rollback()");
            dir.Dispose();
        }

        [Test]
        public virtual void TestNoSegmentFile()
        {
            BaseDirectoryWrapper dir = NewDirectory();
            dir.LockFactory = NoLockFactory.DoNoLockFactory;
            IndexWriter w = new IndexWriter(dir, (IndexWriterConfig)NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())).SetMaxBufferedDocs(2));

            Document doc = new Document();
            FieldType customType = new FieldType(TextField.TYPE_STORED);
            customType.StoreTermVectors = true;
            customType.StoreTermVectorPositions = true;
            customType.StoreTermVectorOffsets = true;
            doc.Add(NewField("c", "val", customType));
            w.AddDocument(doc);
            w.AddDocument(doc);
            IndexWriter w2 = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())).SetMaxBufferedDocs(2).SetOpenMode(OpenMode_e.CREATE));

            w2.Dispose();
            // If we don't do that, the test fails on Windows
            w.Rollback();

            // this test leaves only segments.gen, which causes
            // DirectoryReader.indexExists to return true:
            dir.CheckIndexOnClose = false;
            dir.Dispose();
        }

        [Test]
        public virtual void TestNoUnwantedTVFiles()
        {
            Directory dir = NewDirectory();
            IndexWriter indexWriter = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())).SetRAMBufferSizeMB(0.01).SetMergePolicy(NewLogMergePolicy()));
            indexWriter.Config.MergePolicy.NoCFSRatio = 0.0;

            string BIG = "alskjhlaksjghlaksjfhalksvjepgjioefgjnsdfjgefgjhelkgjhqewlrkhgwlekgrhwelkgjhwelkgrhwlkejg";
            BIG = BIG + BIG + BIG + BIG;

            FieldType customType = new FieldType(TextField.TYPE_STORED);
            customType.OmitNorms = true;
            FieldType customType2 = new FieldType(TextField.TYPE_STORED);
            customType2.Tokenized = false;
            FieldType customType3 = new FieldType(TextField.TYPE_STORED);
            customType3.Tokenized = false;
            customType3.OmitNorms = true;

            for (int i = 0; i < 2; i++)
            {
                Document doc = new Document();
                doc.Add(new Field("id", Convert.ToString(i) + BIG, customType3));
                doc.Add(new Field("str", Convert.ToString(i) + BIG, customType2));
                doc.Add(new Field("str2", Convert.ToString(i) + BIG, StoredTextType));
                doc.Add(new Field("str3", Convert.ToString(i) + BIG, customType));
                indexWriter.AddDocument(doc);
            }

            indexWriter.Dispose();

            TestUtil.CheckIndex(dir);

            AssertNoUnreferencedFiles(dir, "no tv files");
            DirectoryReader r0 = DirectoryReader.Open(dir);
            foreach (AtomicReaderContext ctx in r0.Leaves)
            {
                SegmentReader sr = (SegmentReader)ctx.Reader;
                Assert.IsFalse(sr.FieldInfos.HasVectors());
            }

            r0.Dispose();
            dir.Dispose();
        }

        internal sealed class StringSplitAnalyzer : Analyzer
        {
            public override TokenStreamComponents CreateComponents(string fieldName, TextReader reader)
            {
                return new TokenStreamComponents(new StringSplitTokenizer(reader));
            }
        }

        private class StringSplitTokenizer : Tokenizer
        {
            private string[] Tokens;
            private int Upto;
            private readonly ICharTermAttribute TermAtt;

            public StringSplitTokenizer(TextReader r)
                : base(r)
            {
                TermAtt = AddAttribute<ICharTermAttribute>();
                try
                {
                    Reader = r;
                }
                catch (IOException e)
                {
                    throw new Exception(e.Message, e);
                }
            }

            public sealed override bool IncrementToken()
            {
                ClearAttributes();
                if (Upto < Tokens.Length)
                {
                    TermAtt.SetEmpty();
                    TermAtt.Append(Tokens[Upto]);
                    Upto++;
                    return true;
                }
                else
                {
                    return false;
                }
            }

            public override void Reset()
            {
                base.Reset();
                this.Upto = 0;
                StringBuilder b = new StringBuilder();
                char[] buffer = new char[1024];
                int n;
                while ((n = input.Read(buffer, 0, buffer.Length)) > 0)
                {
                    b.Append(buffer, 0, n);
                }
                this.Tokens = b.ToString().Split(' ');
            }
        }

        /// <summary>
        /// Make sure we skip wicked long terms.
        /// </summary>
        [Test]
        public virtual void TestWickedLongTerm()
        {
            Directory dir = NewDirectory();
            RandomIndexWriter w = new RandomIndexWriter(Random(), dir, new StringSplitAnalyzer(), Similarity, TimeZone);

            char[] chars = new char[DocumentsWriterPerThread.MAX_TERM_LENGTH_UTF8];
            Arrays.Fill(chars, 'x');
            Document doc = new Document();
            string bigTerm = new string(chars);
            BytesRef bigTermBytesRef = new BytesRef(bigTerm);

            // this contents produces a too-long term:
            string contents = "abc xyz x" + bigTerm + " another term";
            doc.Add(new TextField("content", contents, Field.Store.NO));
            try
            {
                w.AddDocument(doc);
                Assert.Fail("should have hit exception");
            }
            catch (System.ArgumentException iae)
            {
                // expected
            }

            // Make sure we can add another normal document
            doc = new Document();
            doc.Add(new TextField("content", "abc bbb ccc", Field.Store.NO));
            w.AddDocument(doc);

            // So we remove the deleted doc:
            w.ForceMerge(1);

            IndexReader reader = w.Reader;
            w.Dispose();

            // Make sure all terms < max size were indexed
            Assert.AreEqual(1, reader.DocFreq(new Term("content", "abc")));
            Assert.AreEqual(1, reader.DocFreq(new Term("content", "bbb")));
            Assert.AreEqual(0, reader.DocFreq(new Term("content", "term")));

            // Make sure the doc that has the massive term is NOT in
            // the index:
            Assert.AreEqual(1, reader.NumDocs, "document with wicked long term is in the index!");

            reader.Dispose();
            dir.Dispose();
            dir = NewDirectory();

            // Make sure we can add a document with exactly the
            // maximum length term, and search on that term:
            doc = new Document();
            FieldType customType = new FieldType(TextField.TYPE_NOT_STORED);
            customType.Tokenized = false;
            Field contentField = new Field("content", "", customType);
            doc.Add(contentField);

            w = new RandomIndexWriter(Random(), dir, Similarity, TimeZone);

            contentField.StringValue = "other";
            w.AddDocument(doc);

            contentField.StringValue = "term";
            w.AddDocument(doc);

            contentField.StringValue = bigTerm;
            w.AddDocument(doc);

            contentField.StringValue = "zzz";
            w.AddDocument(doc);

            reader = w.Reader;
            w.Dispose();
            Assert.AreEqual(1, reader.DocFreq(new Term("content", bigTerm)));

            SortedDocValues dti = FieldCache.DEFAULT.GetTermsIndex(SlowCompositeReaderWrapper.Wrap(reader), "content", (float)Random().NextDouble() * PackedInts.FAST);
            Assert.AreEqual(4, dti.ValueCount);
            BytesRef br = new BytesRef();
            dti.LookupOrd(2, br);
            Assert.AreEqual(bigTermBytesRef, br);
            reader.Dispose();
            dir.Dispose();
        }

        // LUCENE-3183
        [Test]
        public virtual void TestEmptyFieldNameTIIOne()
        {
            Directory dir = NewDirectory();
            IndexWriterConfig iwc = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random()));
            iwc.SetTermIndexInterval(1);
            iwc.SetReaderTermsIndexDivisor(1);
            IndexWriter writer = new IndexWriter(dir, iwc);
            Document doc = new Document();
            doc.Add(NewTextField("", "a b c", Field.Store.NO));
            writer.AddDocument(doc);
            writer.Dispose();
            dir.Dispose();
        }

        [Test]
        public virtual void TestDeleteAllNRTLeftoverFiles()
        {
            Directory d = new MockDirectoryWrapper(Random(), new RAMDirectory());
            IndexWriter w = new IndexWriter(d, new IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())));
            Document doc = new Document();
            for (int i = 0; i < 20; i++)
            {
                for (int j = 0; j < 100; ++j)
                {
                    w.AddDocument(doc);
                }
                w.Commit();
                DirectoryReader.Open(w, true).Dispose();

                w.DeleteAll();
                w.Commit();
                // Make sure we accumulate no files except for empty
                // segments_N and segments.gen:
                Assert.IsTrue(d.ListAll().Length <= 2);
            }

            w.Dispose();
            d.Dispose();
        }

        [Test]
        public virtual void TestNRTReaderVersion()
        {
            Directory d = new MockDirectoryWrapper(Random(), new RAMDirectory());
            IndexWriter w = new IndexWriter(d, new IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())));
            Document doc = new Document();
            doc.Add(NewStringField("id", "0", Field.Store.YES));
            w.AddDocument(doc);
            DirectoryReader r = w.Reader;
            long version = r.Version;
            r.Dispose();

            w.AddDocument(doc);
            r = w.Reader;
            long version2 = r.Version;
            r.Dispose();
            Debug.Assert(version2 > version);

            w.DeleteDocuments(new Term("id", "0"));
            r = w.Reader;
            w.Dispose();
            long version3 = r.Version;
            r.Dispose();
            Debug.Assert(version3 > version2);
            d.Dispose();
        }

        [Test]
        public virtual void TestWhetherDeleteAllDeletesWriteLock()
        {
            Directory d = NewFSDirectory(CreateTempDir("TestIndexWriter.testWhetherDeleteAllDeletesWriteLock"));
            // Must use SimpleFSLockFactory... NativeFSLockFactory
            // somehow "knows" a lock is held against write.lock
            // even if you remove that file:
            d.LockFactory = new SimpleFSLockFactory();
            RandomIndexWriter w1 = new RandomIndexWriter(Random(), d, Similarity, TimeZone);
            w1.DeleteAll();
            try
            {
                new RandomIndexWriter(Random(), d, NewIndexWriterConfig(TEST_VERSION_CURRENT, null).SetWriteLockTimeout(100));
                Assert.Fail("should not be able to create another writer");
            }
            catch (LockObtainFailedException lofe)
            {
                // expected
            }
            w1.Dispose();
            d.Dispose();
        }

        [Test]
        public virtual void TestChangeIndexOptions()
        {
            Directory dir = NewDirectory();
            IndexWriter w = new IndexWriter(dir, new IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())));

            FieldType docsAndFreqs = new FieldType(TextField.TYPE_NOT_STORED);
            docsAndFreqs.IndexOptions = FieldInfo.IndexOptions.DOCS_AND_FREQS;

            FieldType docsOnly = new FieldType(TextField.TYPE_NOT_STORED);
            docsOnly.IndexOptions = FieldInfo.IndexOptions.DOCS_ONLY;

            Document doc = new Document();
            doc.Add(new Field("field", "a b c", docsAndFreqs));
            w.AddDocument(doc);
            w.AddDocument(doc);

            doc = new Document();
            doc.Add(new Field("field", "a b c", docsOnly));
            w.AddDocument(doc);
            w.Dispose();
            dir.Dispose();
        }

        [Test]
        public virtual void TestOnlyUpdateDocuments()
        {
            Directory dir = NewDirectory();
            IndexWriter w = new IndexWriter(dir, new IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())));

            IList<Document> docs = new List<Document>();
            docs.Add(new Document());
            w.UpdateDocuments(new Term("foo", "bar"), docs);
            w.Dispose();
            dir.Dispose();
        }

        // LUCENE-3872
        [Test]
        public virtual void TestPrepareCommitThenClose()
        {
            Directory dir = NewDirectory();
            IndexWriter w = new IndexWriter(dir, new IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())));

            w.PrepareCommit();
            try
            {
                w.Dispose();
                Assert.Fail("should have hit exception");
            }
            catch (InvalidOperationException ise)
            {
                // expected
            }
            w.Commit();
            w.Dispose();
            IndexReader r = DirectoryReader.Open(dir);
            Assert.AreEqual(0, r.MaxDoc);
            r.Dispose();
            dir.Dispose();
        }

        // LUCENE-3872
        [Test]
        public virtual void TestPrepareCommitThenRollback()
        {
            Directory dir = NewDirectory();
            IndexWriter w = new IndexWriter(dir, new IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())));

            w.PrepareCommit();
            w.Rollback();
            Assert.IsFalse(DirectoryReader.IndexExists(dir));
            dir.Dispose();
        }

        // LUCENE-3872
        [Test]
        public virtual void TestPrepareCommitThenRollback2()
        {
            Directory dir = NewDirectory();
            IndexWriter w = new IndexWriter(dir, new IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())));

            w.Commit();
            w.AddDocument(new Document());
            w.PrepareCommit();
            w.Rollback();
            Assert.IsTrue(DirectoryReader.IndexExists(dir));
            IndexReader r = DirectoryReader.Open(dir);
            Assert.AreEqual(0, r.MaxDoc);
            r.Dispose();
            dir.Dispose();
        }

        [Test]
        public virtual void TestDontInvokeAnalyzerForUnAnalyzedFields()
        {
            Analyzer analyzer = new AnalyzerAnonymousInnerClassHelper(this);
            Directory dir = NewDirectory();
            IndexWriter w = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer));
            Document doc = new Document();
            FieldType customType = new FieldType(StringField.TYPE_NOT_STORED);
            customType.StoreTermVectors = true;
            customType.StoreTermVectorPositions = true;
            customType.StoreTermVectorOffsets = true;
            Field f = NewField("field", "abcd", customType);
            doc.Add(f);
            doc.Add(f);
            Field f2 = NewField("field", "", customType);
            doc.Add(f2);
            doc.Add(f);
            w.AddDocument(doc);
            w.Dispose();
            dir.Dispose();
        }

        private class AnalyzerAnonymousInnerClassHelper : Analyzer
        {
            private readonly TestIndexWriter OuterInstance;

            public AnalyzerAnonymousInnerClassHelper(TestIndexWriter outerInstance)
            {
                this.OuterInstance = outerInstance;
            }

            public override TokenStreamComponents CreateComponents(string fieldName, TextReader reader)
            {
                throw new InvalidOperationException("don't invoke me!");
            }

            public override int GetPositionIncrementGap(string fieldName)
            {
                throw new InvalidOperationException("don't invoke me!");
            }

            public override int GetOffsetGap(string fieldName)
            {
                throw new InvalidOperationException("don't invoke me!");
            }
        }

        //LUCENE-1468 -- make sure opening an IndexWriter with
        // create=true does not remove non-index files

        [Test]
        public virtual void TestOtherFiles()
        {
            Directory dir = NewDirectory();
            var iw = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())));
            iw.AddDocument(new Document());
            iw.Dispose();
            try
            {
                // Create my own random file:
                IndexOutput @out = dir.CreateOutput("myrandomfile", NewIOContext(Random()));
                @out.WriteByte((byte)(sbyte)42);
                @out.Dispose();

                (new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())))).Dispose();

                Assert.IsTrue(SlowFileExists(dir, "myrandomfile"));
            }
            finally
            {
                dir.Dispose();
            }
        }

        // LUCENE-3849
        [Test]
        public virtual void TestStopwordsPosIncHole()
        {
            Directory dir = NewDirectory();
            Analyzer a = new AnalyzerAnonymousInnerClassHelper2(this);
            RandomIndexWriter iw = new RandomIndexWriter(Random(), dir, a, Similarity, TimeZone);
            Document doc = new Document();
            doc.Add(new TextField("body", "just a", Field.Store.NO));
            doc.Add(new TextField("body", "test of gaps", Field.Store.NO));
            iw.AddDocument(doc);
            IndexReader ir = iw.Reader;
            iw.Dispose();
            IndexSearcher @is = NewSearcher(ir);
            PhraseQuery pq = new PhraseQuery();
            pq.Add(new Term("body", "just"), 0);
            pq.Add(new Term("body", "test"), 2);
            // body:"just ? test"
            Assert.AreEqual(1, @is.Search(pq, 5).TotalHits);
            ir.Dispose();
            dir.Dispose();
        }

        private class AnalyzerAnonymousInnerClassHelper2 : Analyzer
        {
            private readonly TestIndexWriter OuterInstance;

            public AnalyzerAnonymousInnerClassHelper2(TestIndexWriter outerInstance)
            {
                this.OuterInstance = outerInstance;
            }

            public override TokenStreamComponents CreateComponents(string fieldName, TextReader reader)
            {
                Tokenizer tokenizer = new MockTokenizer(reader);
                TokenStream stream = new MockTokenFilter(tokenizer, MockTokenFilter.ENGLISH_STOPSET);
                return new TokenStreamComponents(tokenizer, stream);
            }
        }

        // LUCENE-3849
        [Test]
        public virtual void TestStopwordsPosIncHole2()
        {
            // use two stopfilters for testing here
            Directory dir = NewDirectory();
            Automaton secondSet = BasicAutomata.MakeString("foobar");
            Analyzer a = new AnalyzerAnonymousInnerClassHelper3(this, secondSet);
            RandomIndexWriter iw = new RandomIndexWriter(Random(), dir, a, Similarity, TimeZone);
            Document doc = new Document();
            doc.Add(new TextField("body", "just a foobar", Field.Store.NO));
            doc.Add(new TextField("body", "test of gaps", Field.Store.NO));
            iw.AddDocument(doc);
            IndexReader ir = iw.Reader;
            iw.Dispose();
            IndexSearcher @is = NewSearcher(ir);
            PhraseQuery pq = new PhraseQuery();
            pq.Add(new Term("body", "just"), 0);
            pq.Add(new Term("body", "test"), 3);
            // body:"just ? ? test"
            Assert.AreEqual(1, @is.Search(pq, 5).TotalHits);
            ir.Dispose();
            dir.Dispose();
        }

        private class AnalyzerAnonymousInnerClassHelper3 : Analyzer
        {
            private readonly TestIndexWriter OuterInstance;

            private Automaton SecondSet;

            public AnalyzerAnonymousInnerClassHelper3(TestIndexWriter outerInstance, Automaton secondSet)
            {
                this.OuterInstance = outerInstance;
                this.SecondSet = secondSet;
            }

            public override TokenStreamComponents CreateComponents(string fieldName, TextReader reader)
            {
                Tokenizer tokenizer = new MockTokenizer(reader);
                TokenStream stream = new MockTokenFilter(tokenizer, MockTokenFilter.ENGLISH_STOPSET);
                stream = new MockTokenFilter(stream, new CharacterRunAutomaton(SecondSet));
                return new TokenStreamComponents(tokenizer, stream);
            }
        }

        // here we do better, there is no current segments file, so we don't delete anything.
        // however, if you actually go and make a commit, the next time you run indexwriter
        // this file will be gone.
        [Test]
        public virtual void TestOtherFiles2()
        {
            Directory dir = NewDirectory();
            try
            {
                // Create my own random file:
                IndexOutput @out = dir.CreateOutput("_a.frq", NewIOContext(Random()));
                @out.WriteByte((byte)(sbyte)42);
                @out.Dispose();

                (new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())))).Dispose();

                Assert.IsTrue(SlowFileExists(dir, "_a.frq"));

                IndexWriter iw = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())));
                iw.AddDocument(new Document());
                iw.Dispose();

                Assert.IsFalse(SlowFileExists(dir, "_a.frq"));
            }
            finally
            {
                dir.Dispose();
            }
        }

        // LUCENE-4398
        [Test]
        public virtual void TestRotatingFieldNames()
        {
            Directory dir = NewFSDirectory(CreateTempDir("TestIndexWriter.testChangingFields"));
            IndexWriterConfig iwc = new IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random()));
            iwc.SetRAMBufferSizeMB(0.2);
            iwc.SetMaxBufferedDocs(-1);
            IndexWriter w = new IndexWriter(dir, iwc);
            int upto = 0;

            FieldType ft = new FieldType(TextField.TYPE_NOT_STORED);
            ft.OmitNorms = true;

            int firstDocCount = -1;
            for (int iter = 0; iter < 10; iter++)
            {
                int startFlushCount = w.FlushCount;
                int docCount = 0;
                while (w.FlushCount == startFlushCount)
                {
                    Document doc = new Document();
                    for (int i = 0; i < 10; i++)
                    {
                        doc.Add(new Field("field" + (upto++), "content", ft));
                    }
                    w.AddDocument(doc);
                    docCount++;
                }

                if (VERBOSE)
                {
                    Console.WriteLine("TEST: iter=" + iter + " flushed after docCount=" + docCount);
                }

                if (iter == 0)
                {
                    firstDocCount = docCount;
                }

                Assert.IsTrue(((float)docCount) / firstDocCount > 0.9, "flushed after too few docs: first segment flushed at docCount=" + firstDocCount + ", but current segment flushed after docCount=" + docCount + "; iter=" + iter);

                if (upto > 5000)
                {
                    // Start re-using field names after a while
                    // ... important because otherwise we can OOME due
                    // to too many FieldInfo instances.
                    upto = 0;
                }
            }
            w.Dispose();
            dir.Dispose();
        }

        // LUCENE-4575
        [Test]
        public virtual void TestCommitWithUserDataOnly()
        {
            Directory dir = NewDirectory();
            IndexWriter writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, null));
            writer.Commit(); // first commit to complete IW create transaction.

            // this should store the commit data, even though no other changes were made
            writer.CommitData = new Dictionary<string, string>() {
            {"key", "value"}
        };
            writer.Commit();

            DirectoryReader r = DirectoryReader.Open(dir);
            Assert.AreEqual("value", r.IndexCommit.UserData["key"]);
            r.Dispose();

            // now check setCommitData and prepareCommit/commit sequence
            writer.CommitData = new Dictionary<string, string>() {
            {"key", "value1"}
        };
            writer.PrepareCommit();
            writer.CommitData = new Dictionary<string, string>() {
            {"key", "value2"}
        };
            writer.Commit(); // should commit the first commitData only, per protocol

            r = DirectoryReader.Open(dir);
            Assert.AreEqual("value1", r.IndexCommit.UserData["key"]);
            r.Dispose();

            // now should commit the second commitData - there was a bug where
            // IndexWriter.finishCommit overrode the second commitData
            writer.Commit();
            r = DirectoryReader.Open(dir);
            Assert.AreEqual("value2", r.IndexCommit.UserData["key"], "IndexWriter.finishCommit may have overridden the second commitData");
            r.Dispose();

            writer.Dispose();
            dir.Dispose();
        }

        [Test]
        public virtual void TestGetCommitData()
        {
            Directory dir = NewDirectory();
            IndexWriter writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, null));
            writer.CommitData = new Dictionary<string, string>() {
            {"key", "value"}
        };
            Assert.AreEqual("value", writer.CommitData["key"]);
            writer.Dispose();

            // validate that it's also visible when opening a new IndexWriter
            writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, null).SetOpenMode(OpenMode_e.APPEND));
            Assert.AreEqual("value", writer.CommitData["key"]);
            writer.Dispose();

            dir.Dispose();
        }

        [Test]
        public virtual void TestIterableThrowsException()
        {
            Directory dir = NewDirectory();
            IndexWriter w = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())));
            int iters = AtLeast(100);
            int docCount = 0;
            int docId = 0;
            HashSet<string> liveIds = new HashSet<string>();
            for (int i = 0; i < iters; i++)
            {
                IList<IEnumerable<IndexableField>> docs = new List<IEnumerable<IndexableField>>();
                FieldType ft = new FieldType(TextField.TYPE_NOT_STORED);
                FieldType idFt = new FieldType(TextField.TYPE_STORED);

                int numDocs = AtLeast(4);
                for (int j = 0; j < numDocs; j++)
                {
                    Document doc = new Document();
                    doc.Add(NewField("id", "" + (docId++), idFt));
                    doc.Add(NewField("foo", TestUtil.RandomSimpleString(Random()), ft));
                    docs.Add(doc);
                }
                bool success = false;
                try
                {
                    w.AddDocuments(new RandomFailingFieldIterable(docs, Random()));
                    success = true;
                }
                catch (Exception e)
                {
                    Assert.AreEqual("boom", e.Message);
                }
                finally
                {
                    if (success)
                    {
                        docCount += docs.Count;
                        foreach (IEnumerable<IndexableField> indexDocument in docs)
                        {
                            liveIds.Add(((Document)indexDocument).Get("id"));
                        }
                    }
                }
            }
            DirectoryReader reader = w.Reader;
            Assert.AreEqual(docCount, reader.NumDocs);
            IList<AtomicReaderContext> leaves = reader.Leaves;
            foreach (AtomicReaderContext atomicReaderContext in leaves)
            {
                AtomicReader ar = (AtomicReader)atomicReaderContext.Reader;
                Bits liveDocs = ar.LiveDocs;
                int maxDoc = ar.MaxDoc;
                for (int i = 0; i < maxDoc; i++)
                {
                    if (liveDocs == null || liveDocs.Get(i))
                    {
                        Assert.IsTrue(liveIds.Remove(ar.Document(i).Get("id")));
                    }
                }
            }
            Assert.IsTrue(liveIds.Count == 0);
            IOUtils.Close(reader, w, dir);
        }

        private class RandomFailingFieldIterable : IEnumerable<IEnumerable<IndexableField>>
        {
            internal readonly IList<IEnumerable<IndexableField>> DocList;
            internal readonly Random Random;

            public RandomFailingFieldIterable(IList<IEnumerable<IndexableField>> docList, Random random)
            {
                this.DocList = docList;
                this.Random = random;
            }

            public virtual IEnumerator<IEnumerable<IndexableField>> GetEnumerator()
            {
                return DocList.GetEnumerator();
                //return new IteratorAnonymousInnerClassHelper(this, docIter);
            }

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            /*
          private class IteratorAnonymousInnerClassHelper : IEnumerator<IEnumerable<IndexableField>>
          {
              private readonly RandomFailingFieldIterable OuterInstance;

              private IEnumerator<IEnumerable<IndexableField>> DocIter;

              public IteratorAnonymousInnerClassHelper(RandomFailingFieldIterable outerInstance, IEnumerator<IEnumerable<IndexableField>> docIter)
              {
                  this.OuterInstance = outerInstance;
                  this.DocIter = docIter;
              }

              public virtual bool HasNext()
              {
                return DocIter.hasNext();
              }

              public virtual IEnumerable<IndexableField> Next()
              {
                if (OuterInstance.Random.Next(5) == 0)
                {
                  throw new Exception("boom");
                }
                return DocIter.Next();
              }

              public virtual void Remove()
              {
                  throw new System.NotSupportedException();
              }
          }*/
        }

        // LUCENE-2727/LUCENE-2812/LUCENE-4738:
        [Test]
        public virtual void TestCorruptFirstCommit()
        {
            for (int i = 0; i < 6; i++)
            {
                BaseDirectoryWrapper dir = NewDirectory();
                dir.CreateOutput("segments_0", IOContext.DEFAULT).Dispose();
                IndexWriterConfig iwc = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random()));
                int mode = i / 2;
                if (mode == 0)
                {
                    iwc.SetOpenMode(OpenMode_e.CREATE);
                }
                else if (mode == 1)
                {
                    iwc.SetOpenMode(OpenMode_e.APPEND);
                }
                else if (mode == 2)
                {
                    iwc.SetOpenMode(OpenMode_e.CREATE_OR_APPEND);
                }

                if (VERBOSE)
                {
                    Console.WriteLine("\nTEST: i=" + i);
                }

                try
                {
                    if ((i & 1) == 0)
                    {
                        (new IndexWriter(dir, iwc)).Dispose();
                    }
                    else
                    {
                        (new IndexWriter(dir, iwc)).Rollback();
                    }
                    if (mode != 0)
                    {
                        Assert.Fail("expected exception");
                    }
                }
                catch (IOException ioe)
                {
                    // OpenMode_e.APPEND should throw an exception since no
                    // index exists:
                    if (mode == 0)
                    {
                        // Unexpected
                        throw ioe;
                    }
                }

                if (VERBOSE)
                {
                    Console.WriteLine("  at close: " + Arrays.ToString(dir.ListAll()));
                }

                if (mode != 0)
                {
                    dir.CheckIndexOnClose = false;
                }
                dir.Dispose();
            }
        }

        [Test]
        public virtual void TestHasUncommittedChanges()
        {
            Directory dir = NewDirectory();
            IndexWriter writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())));
            Assert.IsTrue(writer.HasUncommittedChanges()); // this will be true because a commit will create an empty index
            Document doc = new Document();
            doc.Add(NewTextField("myfield", "a b c", Field.Store.NO));
            writer.AddDocument(doc);
            Assert.IsTrue(writer.HasUncommittedChanges());

            // Must commit, waitForMerges, commit again, to be
            // certain that hasUncommittedChanges returns false:
            writer.Commit();
            writer.WaitForMerges();
            writer.Commit();
            Assert.IsFalse(writer.HasUncommittedChanges());
            writer.AddDocument(doc);
            Assert.IsTrue(writer.HasUncommittedChanges());
            writer.Commit();
            doc = new Document();
            doc.Add(NewStringField("id", "xyz", Field.Store.YES));
            writer.AddDocument(doc);
            Assert.IsTrue(writer.HasUncommittedChanges());

            // Must commit, waitForMerges, commit again, to be
            // certain that hasUncommittedChanges returns false:
            writer.Commit();
            writer.WaitForMerges();
            writer.Commit();
            Assert.IsFalse(writer.HasUncommittedChanges());
            writer.DeleteDocuments(new Term("id", "xyz"));
            Assert.IsTrue(writer.HasUncommittedChanges());

            // Must commit, waitForMerges, commit again, to be
            // certain that hasUncommittedChanges returns false:
            writer.Commit();
            writer.WaitForMerges();
            writer.Commit();
            Assert.IsFalse(writer.HasUncommittedChanges());
            writer.Dispose();

            writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())));
            Assert.IsFalse(writer.HasUncommittedChanges());
            writer.AddDocument(doc);
            Assert.IsTrue(writer.HasUncommittedChanges());

            writer.Dispose();
            dir.Dispose();
        }

        [Test]
        public virtual void TestMergeAllDeleted()
        {
            Directory dir = NewDirectory();
            IndexWriterConfig iwc = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random()));
            SetOnce<IndexWriter> iwRef = new SetOnce<IndexWriter>();
            iwc.InfoStream = new RandomIndexWriter.TestPointInfoStream(iwc.InfoStream, new TestPointAnonymousInnerClassHelper(this, iwRef));
            IndexWriter evilWriter = new IndexWriter(dir, iwc);
            iwRef.Set(evilWriter);
            for (int i = 0; i < 1000; i++)
            {
                AddDoc(evilWriter);
                if (Random().Next(17) == 0)
                {
                    evilWriter.Commit();
                }
            }
            evilWriter.DeleteDocuments(new MatchAllDocsQuery());
            evilWriter.ForceMerge(1);
            evilWriter.Dispose();
            dir.Dispose();
        }

        private class TestPointAnonymousInnerClassHelper : RandomIndexWriter.TestPoint
        {
            private readonly TestIndexWriter OuterInstance;

            private SetOnce<IndexWriter> IwRef;

            public TestPointAnonymousInnerClassHelper(TestIndexWriter outerInstance, SetOnce<IndexWriter> iwRef)
            {
                this.OuterInstance = outerInstance;
                this.IwRef = iwRef;
            }

            public void Apply(string message)
            {
                if ("startCommitMerge".Equals(message))
                {
                    IwRef.Get().KeepFullyDeletedSegments = false;
                }
                else if ("startMergeInit".Equals(message))
                {
                    IwRef.Get().KeepFullyDeletedSegments = true;
                }
            }
        }

        // LUCENE-5239
        [Test]
        public virtual void TestDeleteSameTermAcrossFields()
        {
            Directory dir = NewDirectory();
            IndexWriterConfig iwc = new IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random()));
            IndexWriter w = new IndexWriter(dir, iwc);
            Document doc = new Document();
            doc.Add(new TextField("a", "foo", Field.Store.NO));
            w.AddDocument(doc);

            // Should not delete the document; with LUCENE-5239 the
            // "foo" from the 2nd delete term would incorrectly
            // match field a's "foo":
            w.DeleteDocuments(new Term("a", "xxx"));
            w.DeleteDocuments(new Term("b", "foo"));
            IndexReader r = w.Reader;
            w.Dispose();

            // Make sure document was not (incorrectly) deleted:
            Assert.AreEqual(1, r.NumDocs);
            r.Dispose();
            dir.Dispose();
        }

        // LUCENE-5574
        [Test]
        public virtual void TestClosingNRTReaderDoesNotCorruptYourIndex()
        {
            // Windows disallows deleting & overwriting files still
            // open for reading:
            AssumeFalse("this test can't run on Windows", Constants.WINDOWS);

            MockDirectoryWrapper dir = NewMockDirectory();

            // Allow deletion of still open files:
            dir.NoDeleteOpenFile = false;

            // Allow writing to same file more than once:
            dir.PreventDoubleWrite = false;

            IndexWriterConfig iwc = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random()));
            LogMergePolicy lmp = new LogDocMergePolicy();
            lmp.MergeFactor = 2;
            iwc.SetMergePolicy(lmp);

            RandomIndexWriter w = new RandomIndexWriter(Random(), dir, iwc);
            Document doc = new Document();
            doc.Add(new TextField("a", "foo", Field.Store.NO));
            w.AddDocument(doc);
            w.Commit();
            w.AddDocument(doc);

            // Get a new reader, but this also sets off a merge:
            IndexReader r = w.Reader;
            w.Dispose();

            // Blow away index and make a new writer:
            foreach (string fileName in dir.ListAll())
            {
                dir.DeleteFile(fileName);
            }

            w = new RandomIndexWriter(Random(), dir, Similarity, TimeZone);
            w.AddDocument(doc);
            w.Dispose();
            r.Dispose();
            dir.Dispose();
        }
    }
}