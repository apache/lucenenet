/*
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
 * (the "License"); you may not use this file except in compliance with
 * the License.  You may obtain a copy of the License at
 * 
 * http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;

using NUnit.Framework;

using KeywordAnalyzer = Lucene.Net.Analysis.KeywordAnalyzer;
using WhitespaceAnalyzer = Lucene.Net.Analysis.WhitespaceAnalyzer;
using StandardAnalyzer = Lucene.Net.Analysis.Standard.StandardAnalyzer;
using Document = Lucene.Net.Documents.Document;
using Field = Lucene.Net.Documents.Field;
using Index = Lucene.Net.Documents.Field.Index;
using Store = Lucene.Net.Documents.Field.Store;
using MaxFieldLength = Lucene.Net.Index.IndexWriter.MaxFieldLength;
using IndexSearcher = Lucene.Net.Search.IndexSearcher;
using ScoreDoc = Lucene.Net.Search.ScoreDoc;
using TermQuery = Lucene.Net.Search.TermQuery;
using Directory = Lucene.Net.Store.Directory;
using FSDirectory = Lucene.Net.Store.FSDirectory;
using RAMDirectory = Lucene.Net.Store.RAMDirectory;
using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;

namespace Lucene.Net.Index
{
    [TestFixture]
    public class TestIndexReaderReopen : LuceneTestCase
    {
        private class AnonymousClassTestReopen : TestReopen_Renamed_Class
        {
            public AnonymousClassTestReopen(Lucene.Net.Store.Directory dir1, TestIndexReaderReopen enclosingInstance)
            {
                InitBlock(dir1, enclosingInstance);
            }
            private void InitBlock(Lucene.Net.Store.Directory dir1, TestIndexReaderReopen enclosingInstance)
            {
                this.dir1 = dir1;
                this.enclosingInstance = enclosingInstance;
            }

            private Lucene.Net.Store.Directory dir1;
            private TestIndexReaderReopen enclosingInstance;
            public TestIndexReaderReopen Enclosing_Instance
            {
                get
                {
                    return enclosingInstance;
                }

            }

            protected internal override void ModifyIndex(int i)
            {
                TestIndexReaderReopen.ModifyIndex(i, dir1);
            }

            protected internal override IndexReader OpenReader()
            {
                return IndexReader.Open(dir1);
            }
        }

        private class AnonymousClassTestReopen1 : TestReopen_Renamed_Class
        {
            public AnonymousClassTestReopen1(Lucene.Net.Store.Directory dir2, TestIndexReaderReopen enclosingInstance)
            {
                InitBlock(dir2, enclosingInstance);
            }
            private void InitBlock(Lucene.Net.Store.Directory dir2, TestIndexReaderReopen enclosingInstance)
            {
                this.dir2 = dir2;
                this.enclosingInstance = enclosingInstance;
            }

            private Lucene.Net.Store.Directory dir2;
            private TestIndexReaderReopen enclosingInstance;
            public TestIndexReaderReopen Enclosing_Instance
            {
                get
                {
                    return enclosingInstance;
                }

            }

            protected internal override void ModifyIndex(int i)
            {
                TestIndexReaderReopen.ModifyIndex(i, dir2);
            }

            protected internal override IndexReader OpenReader()
            {
                return IndexReader.Open(dir2);
            }
        }

        private class AnonymousClassTestReopen2 : TestReopen_Renamed_Class
        {
            public AnonymousClassTestReopen2(Lucene.Net.Store.Directory dir1, Lucene.Net.Store.Directory dir2, TestIndexReaderReopen enclosingInstance)
            {
                InitBlock(dir1, dir2, enclosingInstance);
            }
            private void InitBlock(Lucene.Net.Store.Directory dir1, Lucene.Net.Store.Directory dir2, TestIndexReaderReopen enclosingInstance)
            {
                this.dir1 = dir1;
                this.dir2 = dir2;
                this.enclosingInstance = enclosingInstance;
            }
            private Lucene.Net.Store.Directory dir1;
            private Lucene.Net.Store.Directory dir2;
            private TestIndexReaderReopen enclosingInstance;
            public TestIndexReaderReopen Enclosing_Instance
            {
                get
                {
                    return enclosingInstance;
                }

            }

            protected internal override void ModifyIndex(int i)
            {
                TestIndexReaderReopen.ModifyIndex(i, dir1);
                TestIndexReaderReopen.ModifyIndex(i, dir2);
            }

            protected internal override IndexReader OpenReader()
            {
                ParallelReader pr = new ParallelReader();
                pr.Add(IndexReader.Open(dir1));
                pr.Add(IndexReader.Open(dir2));
                return pr;
            }
        }

        private class AnonymousClassTestReopen3 : TestReopen_Renamed_Class
        {
            public AnonymousClassTestReopen3(Lucene.Net.Store.Directory dir3, Lucene.Net.Store.Directory dir4, TestIndexReaderReopen enclosingInstance)
            {
                InitBlock(dir3, dir4, enclosingInstance);
            }
            private void InitBlock(Lucene.Net.Store.Directory dir3, Lucene.Net.Store.Directory dir4, TestIndexReaderReopen enclosingInstance)
            {
                this.dir3 = dir3;
                this.dir4 = dir4;
                this.enclosingInstance = enclosingInstance;
            }
            private Lucene.Net.Store.Directory dir3;
            private Lucene.Net.Store.Directory dir4;
            private TestIndexReaderReopen enclosingInstance;
            public TestIndexReaderReopen Enclosing_Instance
            {
                get
                {
                    return enclosingInstance;
                }

            }

            protected internal override void ModifyIndex(int i)
            {
                TestIndexReaderReopen.ModifyIndex(i, dir3);
                TestIndexReaderReopen.ModifyIndex(i, dir4);
            }

            protected internal override IndexReader OpenReader()
            {
                ParallelReader pr = new ParallelReader();
                pr.Add(IndexReader.Open(dir3));
                pr.Add(IndexReader.Open(dir4));
                pr.Add(new FilterIndexReader(IndexReader.Open(dir3)));
                return pr;
            }
        }

        private class AnonymousClassTestReopen4 : TestReopen_Renamed_Class
        {
            public AnonymousClassTestReopen4(Lucene.Net.Store.Directory dir1, Lucene.Net.Store.Directory dir2, TestIndexReaderReopen enclosingInstance)
            {
                InitBlock(dir1, dir2, enclosingInstance);
            }
            private void InitBlock(Lucene.Net.Store.Directory dir1, Lucene.Net.Store.Directory dir2, TestIndexReaderReopen enclosingInstance)
            {
                this.dir1 = dir1;
                this.dir2 = dir2;
                this.enclosingInstance = enclosingInstance;
            }
            private Lucene.Net.Store.Directory dir1;
            private Lucene.Net.Store.Directory dir2;
            private TestIndexReaderReopen enclosingInstance;
            public TestIndexReaderReopen Enclosing_Instance
            {
                get
                {
                    return enclosingInstance;
                }

            }

            protected internal override void ModifyIndex(int i)
            {
                TestIndexReaderReopen.ModifyIndex(i, dir1);
                TestIndexReaderReopen.ModifyIndex(i, dir2);
            }

            protected internal override IndexReader OpenReader()
            {
                return new MultiReader(new IndexReader[] { IndexReader.Open(dir1), IndexReader.Open(dir2) });
            }
        }

        private class AnonymousClassTestReopen5 : TestReopen_Renamed_Class
        {
            public AnonymousClassTestReopen5(Lucene.Net.Store.Directory dir3, Lucene.Net.Store.Directory dir4, TestIndexReaderReopen enclosingInstance)
            {
                InitBlock(dir3, dir4, enclosingInstance);
            }
            private void InitBlock(Lucene.Net.Store.Directory dir3, Lucene.Net.Store.Directory dir4, TestIndexReaderReopen enclosingInstance)
            {
                this.dir3 = dir3;
                this.dir4 = dir4;
                this.enclosingInstance = enclosingInstance;
            }
            private Lucene.Net.Store.Directory dir3;
            private Lucene.Net.Store.Directory dir4;
            private TestIndexReaderReopen enclosingInstance;
            public TestIndexReaderReopen Enclosing_Instance
            {
                get
                {
                    return enclosingInstance;
                }

            }

            protected internal override void ModifyIndex(int i)
            {
                TestIndexReaderReopen.ModifyIndex(i, dir3);
                TestIndexReaderReopen.ModifyIndex(i, dir4);
            }

            protected internal override IndexReader OpenReader()
            {
                return new MultiReader(new IndexReader[] { IndexReader.Open(dir3), IndexReader.Open(dir4), new FilterIndexReader(IndexReader.Open(dir3)) });
            }
        }

        private class AnonymousClassTestReopen6 : TestReopen_Renamed_Class
        {
            public AnonymousClassTestReopen6(Lucene.Net.Store.Directory dir1, Lucene.Net.Store.Directory dir4, Lucene.Net.Store.Directory dir5, Lucene.Net.Store.Directory dir2, Lucene.Net.Store.Directory dir3, TestIndexReaderReopen enclosingInstance)
            {
                InitBlock(dir1, dir4, dir5, dir2, dir3, enclosingInstance);
            }
            private void InitBlock(Lucene.Net.Store.Directory dir1, Lucene.Net.Store.Directory dir4, Lucene.Net.Store.Directory dir5, Lucene.Net.Store.Directory dir2, Lucene.Net.Store.Directory dir3, TestIndexReaderReopen enclosingInstance)
            {
                this.dir1 = dir1;
                this.dir4 = dir4;
                this.dir5 = dir5;
                this.dir2 = dir2;
                this.dir3 = dir3;
                this.enclosingInstance = enclosingInstance;
            }
            private Lucene.Net.Store.Directory dir1;
            private Lucene.Net.Store.Directory dir4;
            private Lucene.Net.Store.Directory dir5;
            private Lucene.Net.Store.Directory dir2;
            private Lucene.Net.Store.Directory dir3;
            private TestIndexReaderReopen enclosingInstance;
            public TestIndexReaderReopen Enclosing_Instance
            {
                get
                {
                    return enclosingInstance;
                }

            }

            protected internal override void ModifyIndex(int i)
            {
                // only change norms in this index to maintain the same number of docs for each of ParallelReader's subreaders
                if (i == 1)
                    TestIndexReaderReopen.ModifyIndex(i, dir1);

                TestIndexReaderReopen.ModifyIndex(i, dir4);
                TestIndexReaderReopen.ModifyIndex(i, dir5);
            }

            protected internal override IndexReader OpenReader()
            {
                ParallelReader pr = new ParallelReader();
                pr.Add(IndexReader.Open(dir1));
                pr.Add(IndexReader.Open(dir2));
                MultiReader mr = new MultiReader(new IndexReader[] { IndexReader.Open(dir3), IndexReader.Open(dir4) });
                return new MultiReader(new IndexReader[] { pr, mr, IndexReader.Open(dir5) });
            }
        }

        private class AnonymousClassTestReopen7 : TestReopen_Renamed_Class
        {
            public AnonymousClassTestReopen7(Lucene.Net.Store.Directory dir, int n, TestIndexReaderReopen enclosingInstance)
            {
                InitBlock(dir, n, enclosingInstance);
            }
            private void InitBlock(Lucene.Net.Store.Directory dir, int n, TestIndexReaderReopen enclosingInstance)
            {
                this.dir = dir;
                this.n = n;
                this.enclosingInstance = enclosingInstance;
            }
            private Lucene.Net.Store.Directory dir;
            private int n;
            private TestIndexReaderReopen enclosingInstance;
            public TestIndexReaderReopen Enclosing_Instance
            {
                get
                {
                    return enclosingInstance;
                }

            }
            protected internal override void ModifyIndex(int i)
            {
                if (i % 3 == 0)
                {
                    IndexReader modifier = IndexReader.Open(dir);
                    modifier.SetNorm(i, "field1", 50);
//System.Console.WriteLine("TestIndexReaderReopen.AnonymousClassTestReopen7.ModifyIndex(): calling IndexReader.SetNorm() <soon locked>");
                    modifier.Close();
//System.Console.WriteLine("TestIndexReaderReopen.AnonymousClassTestReopen7.ModifyIndex(): called IndexReader.Close() <unlocked>");
                }
                else if (i % 3 == 1)
                {
                    IndexReader modifier = IndexReader.Open(dir);
//System.Console.WriteLine("TestIndexReaderReopen.AnonymousClassTestReopen7.ModifyIndex(): calling IndexReader.DeleteDocument(i) <soon locked>");
                    modifier.DeleteDocument(i);
                    modifier.Close();
//System.Console.WriteLine("TestIndexReaderReopen.AnonymousClassTestReopen7.ModifyIndex(): called IndexReader.Close() <unlocked>");
                }
                else
                {
//System.Console.WriteLine("TestIndexReaderReopen.AnonymousClassTestReopen7.ModifyIndex(): calling IndexWriter<init> <soon locked>");
                    IndexWriter modifier = new IndexWriter(dir, new StandardAnalyzer(), IndexWriter.MaxFieldLength.LIMITED);
                    modifier.AddDocument(Lucene.Net.Index.TestIndexReaderReopen.CreateDocument(n + i, 6));
                    modifier.Close();
//System.Console.WriteLine("TestIndexReaderReopen.AnonymousClassTestReopen7.ModifyIndex(): called IndexWriter.Close() <unlocked>");
                }
            }

            protected internal override IndexReader OpenReader()
            {
//System.Console.WriteLine("TestIndexReaderReopen.AnonymousClassTestReopen7.OpenReader(): calling IndexReader.Open()");
                return IndexReader.Open(dir);
            }
        }

        private class AnonymousClassReaderThreadTask : ReaderThreadTask
        {
            public AnonymousClassReaderThreadTask(int index, Lucene.Net.Index.IndexReader r, TestReopen_Renamed_Class test, System.Collections.Hashtable readersToClose, System.Collections.IList readers, System.Random rnd, TestIndexReaderReopen enclosingInstance)
            {
                InitBlock(index, r, test, readersToClose, readers, rnd, enclosingInstance);
            }
            private void InitBlock(int index, Lucene.Net.Index.IndexReader r, TestReopen_Renamed_Class test, System.Collections.Hashtable readersToClose, System.Collections.IList readers, System.Random rnd, TestIndexReaderReopen enclosingInstance)
            {
                this.index = index;
                this.r = r;
                this.test = test;
                this.readersToClose = readersToClose;
                this.readers = readers;
                this.rnd = rnd;
                this.enclosingInstance = enclosingInstance;
            }

            private int index;
            private Lucene.Net.Index.IndexReader r;
            private TestReopen_Renamed_Class test;
            private System.Collections.Hashtable readersToClose;
            private System.Collections.IList readers;
            private System.Random rnd;
            private TestIndexReaderReopen enclosingInstance;
            public TestIndexReaderReopen Enclosing_Instance
            {
                get
                {
                    return enclosingInstance;
                }

            }

            public override void Run()
            {
                while (!stopped)
                {
                    if (index % 2 == 0)
                    {
                        // refresh reader synchronized
                        ReaderCouple c = (Enclosing_Instance.RefreshReader(r, test, index, true));
                        if (!readersToClose.Contains(c.newReader))
                            readersToClose.Add(c.newReader, c.newReader);
                        if (!readersToClose.Contains(c.refreshedReader))
                            readersToClose.Add(c.refreshedReader, c.refreshedReader);
                        readers.Add(c);
                        // prevent too many readers
                        break;
                    }
                    else
                    {
                        // not synchronized
//System.Console.WriteLine("TestIndexReaderReopen.AnonymousClassReaderThreadTask.Run(): calling IndexReader.Reopen()");
                        IndexReader refreshed = r.Reopen();

                        IndexSearcher searcher = new IndexSearcher(refreshed);
                        ScoreDoc[] hits = searcher.Search(new TermQuery(new Term("field1", "a" + rnd.Next(refreshed.MaxDoc()))), null, 1000).scoreDocs;
                        if (hits.Length > 0)
                        {
                            searcher.Doc(hits[0].doc);
                        }

                        // r might have changed because this is not a 
                        // synchronized method. However we don't want
                        // to make it synchronized to test 
                        // thread-safety of IndexReader.close().
                        // That's why we add refreshed also to 
                        // readersToClose, because double closing is fine
                        if (refreshed != r)
                        {
                            refreshed.Close();
                        }
                        if (!readersToClose.Contains(refreshed))
                            readersToClose.Add(refreshed, refreshed);
                    }
                    try
                    {
                        lock (this)
                        {
                            System.Threading.Monitor.Wait(this, TimeSpan.FromMilliseconds(1000));
                        }
                    }
                    catch (System.Threading.ThreadInterruptedException)
                    {
                    }
                }
            }
        }

        private class AnonymousClassReaderThreadTask1 : ReaderThreadTask
        {
            public AnonymousClassReaderThreadTask1(System.Collections.IList readers, System.Random rnd, TestIndexReaderReopen enclosingInstance)
            {
                InitBlock(readers, rnd, enclosingInstance);
            }
            private void InitBlock(System.Collections.IList readers, System.Random rnd, TestIndexReaderReopen enclosingInstance)
            {
                this.readers = readers;
                this.rnd = rnd;
                this.enclosingInstance = enclosingInstance;
            }
            private System.Collections.IList readers;
            private System.Random rnd;
            private TestIndexReaderReopen enclosingInstance;
            public TestIndexReaderReopen Enclosing_Instance
            {
                get
                {
                    return enclosingInstance;
                }

            }
            public override void Run()
            {
                while (!stopped)
                {
                    int numReaders = readers.Count;
                    if (numReaders > 0)
                    {
                        ReaderCouple c = (ReaderCouple)readers[rnd.Next(numReaders)];
//System.Console.WriteLine("TestIndexReaderReopen.AnonymousClassReaderThreadTask1.Run(): testing IndexReader equality");
                        TestIndexReader.AssertIndexEquals(c.newReader, c.refreshedReader);
                    }

                    try
                    {
                        lock (this)
                        {
                            System.Threading.Monitor.Wait(this, TimeSpan.FromMilliseconds(100));
                        }
                    }
                    catch (System.Threading.ThreadInterruptedException)
                    {
                    }
                }
            }
        }

        private System.IO.FileInfo indexDir;

        [Test]
        public virtual void TestReopen()
        {
            Directory dir1 = new RAMDirectory();

            CreateIndex(dir1, false);
            PerformDefaultTests(new AnonymousClassTestReopen(dir1, this));

            Directory dir2 = new RAMDirectory();

            CreateIndex(dir2, true);
            PerformDefaultTests(new AnonymousClassTestReopen1(dir2, this));
        }

        [Test]
        public virtual void TestParallelReaderReopen()
        {
            Directory dir1 = new RAMDirectory();
            CreateIndex(dir1, true);
            Directory dir2 = new RAMDirectory();
            CreateIndex(dir2, true);

            PerformDefaultTests(new AnonymousClassTestReopen2(dir1, dir2, this));

            Directory dir3 = new RAMDirectory();
            CreateIndex(dir3, true);
            Directory dir4 = new RAMDirectory();
            CreateIndex(dir4, true);

            PerformTestsWithExceptionInReopen(new AnonymousClassTestReopen3(dir3, dir4, this));
        }

        // LUCENE-1228: IndexWriter.commit() does not update the index version
        // populate an index in iterations.
        // at the end of every iteration, commit the index and reopen/recreate the reader.
        // in each iteration verify the work of previous iteration. 
        // try this once with reopen once recreate, on both RAMDir and FSDir.
        public void TestCommitReopenFS()
        {
            Directory dir = FSDirectory.GetDirectory(indexDir);
            DoTestReopenWithCommit(dir, true);
        }
        public void TestCommitRecreateFS()
        {
            Directory dir = FSDirectory.GetDirectory(indexDir);
            DoTestReopenWithCommit(dir, false);
        }
        public void TestCommitReopenRAM()
        {
            Directory dir = new RAMDirectory();
            DoTestReopenWithCommit(dir, true);
        }
        public void TestCommitRecreateRAM()
        {
            Directory dir = new RAMDirectory();
            DoTestReopenWithCommit(dir, false);
        }

        private void DoTestReopenWithCommit(Directory dir, bool withReopen)
        {
            IndexWriter iwriter = new IndexWriter(dir, new KeywordAnalyzer(), true, MaxFieldLength.LIMITED);
            iwriter.SetMergeScheduler(new SerialMergeScheduler());
            IndexReader reader = IndexReader.Open(dir);
            try
            {
                int M = 3;
                for (int i = 0; i < 4; i++)
                {
                    for (int j = 0; j < M; j++)
                    {
                        Document doc = new Document();
                        doc.Add(new Field("id", i + "_" + j, Field.Store.YES, Field.Index.NOT_ANALYZED));
                        iwriter.AddDocument(doc);
                        if (i > 0)
                        {
                            int k = i - 1;
                            int n = j + k * M;
                            Document prevItereationDoc = reader.Document(n);
                            Assert.IsNotNull(prevItereationDoc);
                            String id = prevItereationDoc.Get("id");
                            Assert.AreEqual(k + "_" + j, id);
                        }
                    }
                    iwriter.Commit();
                    if (withReopen)
                    {
                        // reopen
                        IndexReader r2 = reader.Reopen();
                        if (reader != r2)
                        {
                            reader.Close();
                            reader = r2;
                        }
                    }
                    else
                    {
                        // recreate
                        reader.Close();
                        reader = IndexReader.Open(dir);
                    }
                }
            }
            finally
            {
                iwriter.Close();
                reader.Close();
            }
        }

        [Test]
        public virtual void TestMultiReaderReopen()
        {
            Directory dir1 = new RAMDirectory();
            CreateIndex(dir1, true);
            Directory dir2 = new RAMDirectory();
            CreateIndex(dir2, true);

            PerformDefaultTests(new AnonymousClassTestReopen4(dir1, dir2, this));

            Directory dir3 = new RAMDirectory();
            CreateIndex(dir3, true);
            Directory dir4 = new RAMDirectory();
            CreateIndex(dir4, true);

            PerformTestsWithExceptionInReopen(new AnonymousClassTestReopen5(dir3, dir4, this));
        }

        [Test]
        public virtual void TestMixedReaders()
        {
            Directory dir1 = new RAMDirectory();
            CreateIndex(dir1, true);
            Directory dir2 = new RAMDirectory();
            CreateIndex(dir2, true);
            Directory dir3 = new RAMDirectory();
            CreateIndex(dir3, false);
            Directory dir4 = new RAMDirectory();
            CreateIndex(dir4, true);
            Directory dir5 = new RAMDirectory();
            CreateIndex(dir5, false);

            PerformDefaultTests(new AnonymousClassTestReopen6(dir1, dir4, dir5, dir2, dir3, this));
        }


        private void PerformDefaultTests(TestReopen_Renamed_Class test)
        {
            IndexReader index1 = test.OpenReader();
            IndexReader index2 = test.OpenReader();

            TestIndexReader.AssertIndexEquals(index1, index2);

            // verify that reopen() does not return a new reader instance
            // in case the index has no changes
            ReaderCouple couple = RefreshReader(index2, false);
            Assert.IsTrue(couple.refreshedReader == index2);

            couple = RefreshReader(index2, test, 0, true);
            index1 = couple.newReader;
            IndexReader index2_refreshed = couple.refreshedReader;
            index2.Close();

            // test if refreshed reader and newly opened reader return equal results
            TestIndexReader.AssertIndexEquals(index1, index2_refreshed);

            index1.Close();
            index2_refreshed.Close();
            AssertReaderClosed(index2, true, true);
            AssertReaderClosed(index2_refreshed, true, true);

            index2 = test.OpenReader();

            for (int i = 1; i < 4; i++)
            {

                index1.Close();
                couple = RefreshReader(index2, test, i, true);
                // refresh IndexReader
                index2.Close();

                index2 = couple.refreshedReader;
                index1 = couple.newReader;
                TestIndexReader.AssertIndexEquals(index1, index2);
            }

            index1.Close();
            index2.Close();
            AssertReaderClosed(index1, true, true);
            AssertReaderClosed(index2, true, true);
        }

        [Test]
        public virtual void TestReferenceCounting()
        {

            for (int mode = 0; mode < 4; mode++)
            {
                Directory dir1 = new RAMDirectory();
                CreateIndex(dir1, true);

                IndexReader reader0 = IndexReader.Open(dir1);
                AssertRefCountEquals(1, reader0);

                Assert.IsTrue(reader0 is MultiSegmentReader);
                SegmentReader[] subReaders0 = ((MultiSegmentReader)reader0).GetSubReaders();
                for (int i = 0; i < subReaders0.Length; i++)
                {
                    AssertRefCountEquals(1, subReaders0[i]);
                }

                // delete first document, so that only one of the subReaders have to be re-opened
                IndexReader modifier = IndexReader.Open(dir1);
                modifier.DeleteDocument(0);
                modifier.Close();

                IndexReader reader1 = RefreshReader(reader0, true).refreshedReader;
                Assert.IsTrue(reader1 is MultiSegmentReader);
                SegmentReader[] subReaders1 = ((MultiSegmentReader)reader1).GetSubReaders();
                Assert.AreEqual(subReaders0.Length, subReaders1.Length);

                for (int i = 0; i < subReaders0.Length; i++)
                {
                    AssertRefCountEquals(2, subReaders0[i]);
                    if (subReaders0[i] != subReaders1[i])
                    {
                        AssertRefCountEquals(1, subReaders1[i]);
                    }
                }

                // delete first document, so that only one of the subReaders have to be re-opened
                modifier = IndexReader.Open(dir1);
                modifier.DeleteDocument(1);
                modifier.Close();

                IndexReader reader2 = RefreshReader(reader1, true).refreshedReader;
                Assert.IsTrue(reader2 is MultiSegmentReader);
                SegmentReader[] subReaders2 = ((MultiSegmentReader)reader2).GetSubReaders();
                Assert.AreEqual(subReaders1.Length, subReaders2.Length);

                for (int i = 0; i < subReaders2.Length; i++)
                {
                    if (subReaders2[i] == subReaders1[i])
                    {
                        if (subReaders1[i] == subReaders0[i])
                        {
                            AssertRefCountEquals(3, subReaders2[i]);
                        }
                        else
                        {
                            AssertRefCountEquals(2, subReaders2[i]);
                        }
                    }
                    else
                    {
                        AssertRefCountEquals(1, subReaders2[i]);
                        if (subReaders0[i] == subReaders1[i])
                        {
                            AssertRefCountEquals(3, subReaders2[i]);
                            AssertRefCountEquals(2, subReaders0[i]);
                        }
                        else
                        {
                            AssertRefCountEquals(3, subReaders0[i]);
                            AssertRefCountEquals(1, subReaders1[i]);
                        }
                    }
                }

                IndexReader reader3 = RefreshReader(reader0, true).refreshedReader;
                Assert.IsTrue(reader3 is MultiSegmentReader);
                SegmentReader[] subReaders3 = ((MultiSegmentReader)reader3).GetSubReaders();
                Assert.AreEqual(subReaders3.Length, subReaders0.Length);

                // try some permutations
                switch (mode)
                {

                    case 0:
                        reader0.Close();
                        reader1.Close();
                        reader2.Close();
                        reader3.Close();
                        break;

                    case 1:
                        reader3.Close();
                        reader2.Close();
                        reader1.Close();
                        reader0.Close();
                        break;

                    case 2:
                        reader2.Close();
                        reader3.Close();
                        reader0.Close();
                        reader1.Close();
                        break;

                    case 3:
                        reader1.Close();
                        reader3.Close();
                        reader2.Close();
                        reader0.Close();
                        break;
                }

                AssertReaderClosed(reader0, true, true);
                AssertReaderClosed(reader1, true, true);
                AssertReaderClosed(reader2, true, true);
                AssertReaderClosed(reader3, true, true);
            }
        }


        [Test]
        public virtual void TestReferenceCountingMultiReader()
        {
            for (int mode = 0; mode <= 1; mode++)
            {
                Directory dir1 = new RAMDirectory();
                CreateIndex(dir1, false);
                Directory dir2 = new RAMDirectory();
                CreateIndex(dir2, true);

                IndexReader reader1 = IndexReader.Open(dir1);
                AssertRefCountEquals(1, reader1);

                IndexReader multiReader1 = new MultiReader(new IndexReader[] { reader1, IndexReader.Open(dir2) }, (mode == 0));
                ModifyIndex(0, dir2);
                AssertRefCountEquals(1 + mode, reader1);

                IndexReader multiReader2 = multiReader1.Reopen();
                // index1 hasn't changed, so multiReader2 should share reader1 now with multiReader1
                AssertRefCountEquals(2 + mode, reader1);

                ModifyIndex(0, dir1);
                IndexReader reader2 = reader1.Reopen();
                AssertRefCountEquals(3 + mode, reader1);

                ModifyIndex(1, dir1);
                IndexReader reader3 = reader2.Reopen();
                AssertRefCountEquals(4 + mode, reader1);
                AssertRefCountEquals(1, reader2);

                multiReader1.Close();
                AssertRefCountEquals(3 + mode, reader1);

                multiReader1.Close();
                AssertRefCountEquals(3 + mode, reader1);

                reader1.Close();
                AssertRefCountEquals(3, reader1);

                multiReader2.Close();
                AssertRefCountEquals(2, reader1);

                multiReader2.Close();
                AssertRefCountEquals(2, reader1);

                reader3.Close();
                AssertRefCountEquals(1, reader1);
                AssertReaderOpen(reader1);

                reader2.Close();
                AssertRefCountEquals(0, reader1);
                AssertReaderClosed(reader1, true, false);

                reader2.Close();
                AssertRefCountEquals(0, reader1);

                reader3.Close();
                AssertRefCountEquals(0, reader1);
                AssertReaderClosed(reader1, true, true);
            }
        }

        [Test]
        public virtual void TestReferenceCountingParallelReader()
        {
            for (int mode = 0; mode <= 1; mode++)
            {
                Directory dir1 = new RAMDirectory();
                CreateIndex(dir1, false);
                Directory dir2 = new RAMDirectory();
                CreateIndex(dir2, true);

                IndexReader reader1 = IndexReader.Open(dir1);
                AssertRefCountEquals(1, reader1);

                ParallelReader parallelReader1 = new ParallelReader(mode == 0);
                parallelReader1.Add(reader1);
                parallelReader1.Add(IndexReader.Open(dir2));
                ModifyIndex(1, dir2);
                AssertRefCountEquals(1 + mode, reader1);

                IndexReader parallelReader2 = parallelReader1.Reopen();
                // index1 hasn't changed, so parallelReader2 should share reader1 now with multiReader1
                AssertRefCountEquals(2 + mode, reader1);

                ModifyIndex(0, dir1);
                ModifyIndex(0, dir2);
                IndexReader reader2 = reader1.Reopen();
                AssertRefCountEquals(3 + mode, reader1);

                ModifyIndex(4, dir1);
                IndexReader reader3 = reader2.Reopen();
                AssertRefCountEquals(4 + mode, reader1);
                AssertRefCountEquals(1, reader2);

                parallelReader1.Close();
                AssertRefCountEquals(3 + mode, reader1);

                parallelReader1.Close();
                AssertRefCountEquals(3 + mode, reader1);

                reader1.Close();
                AssertRefCountEquals(3, reader1);

                parallelReader2.Close();
                AssertRefCountEquals(2, reader1);

                parallelReader2.Close();
                AssertRefCountEquals(2, reader1);

                reader3.Close();
                AssertRefCountEquals(1, reader1);
                AssertReaderOpen(reader1);

                reader2.Close();
                AssertRefCountEquals(0, reader1);
                AssertReaderClosed(reader1, true, false);

                reader2.Close();
                AssertRefCountEquals(0, reader1);

                reader3.Close();
                AssertRefCountEquals(0, reader1);
                AssertReaderClosed(reader1, true, true);
            }
        }

        [Test]
        public virtual void TestNormsRefCounting()
        {
            Directory dir1 = new RAMDirectory();
            CreateIndex(dir1, false);

            SegmentReader reader1 = (SegmentReader)IndexReader.Open(dir1);
            IndexReader modifier = IndexReader.Open(dir1);
            modifier.DeleteDocument(0);
            modifier.Close();

            SegmentReader reader2 = (SegmentReader)reader1.Reopen();
            modifier = IndexReader.Open(dir1);
            modifier.SetNorm(1, "field1", 50);
            modifier.SetNorm(1, "field2", 50);
            modifier.Close();

            SegmentReader reader3 = (SegmentReader)reader2.Reopen();
            modifier = IndexReader.Open(dir1);
            modifier.DeleteDocument(2);
            modifier.Close();
            SegmentReader reader4 = (SegmentReader)reader3.Reopen();

            modifier = IndexReader.Open(dir1);
            modifier.DeleteDocument(3);
            modifier.Close();
            SegmentReader reader5 = (SegmentReader)reader3.Reopen();

            // Now reader2-reader5 references reader1. reader1 and reader2
            // share the same norms. reader3, reader4, reader5 also share norms.
            AssertRefCountEquals(5, reader1);
            Assert.IsFalse(reader1.NormsClosed());
            reader1.Close();
            AssertRefCountEquals(4, reader1);
            Assert.IsFalse(reader1.NormsClosed());
            reader2.Close();
            AssertRefCountEquals(3, reader1);
            // now the norms for field1 and field2 should be closed
            Assert.IsTrue(reader1.NormsClosed("field1"));
            Assert.IsTrue(reader1.NormsClosed("field2"));
            // but the norms for field3 and field4 should still be open
            Assert.IsFalse(reader1.NormsClosed("field3"));
            Assert.IsFalse(reader1.NormsClosed("field4"));

            reader3.Close();
            AssertRefCountEquals(2, reader1);
            Assert.IsFalse(reader3.NormsClosed());
            reader5.Close();
            AssertRefCountEquals(1, reader1);
            Assert.IsFalse(reader3.NormsClosed());
            reader4.Close();
            AssertRefCountEquals(0, reader1);

            // and now all norms that reader1 used should be closed
            Assert.IsTrue(reader1.NormsClosed());

            // now that reader3, reader4 and reader5 are closed,
            // the norms that those three readers shared should be
            // closed as well
            Assert.IsTrue(reader3.NormsClosed());
        }

        private void PerformTestsWithExceptionInReopen(TestReopen_Renamed_Class test)
        {
            IndexReader index1 = test.OpenReader();
            IndexReader index2 = test.OpenReader();

            TestIndexReader.AssertIndexEquals(index1, index2);

            try
            {
                ReaderCouple couple = RefreshReader(index1, test, 0, true);
                Assert.Fail("Expected exception not thrown.");
            }
            catch (System.Exception)
            {
                // expected exception
            }

            // index2 should still be usable and unaffected by the failed reopen() call
            TestIndexReader.AssertIndexEquals(index1, index2);
        }

        [Test]
        public virtual void TestDougFoo()
        {
            Directory dir = new RAMDirectory();

            Document doc = new Document();
            doc.Add(new Field("field", "content", Field.Store.NO, Field.Index.ANALYZED));
            
            IndexWriter writer = new IndexWriter(dir, new StandardAnalyzer(), IndexWriter.MaxFieldLength.LIMITED);
            writer.AddDocument(doc);
            writer.Optimize();
            writer.Close();

            IndexReader reader = IndexReader.Open(dir);
            byte[] norms = reader.Norms("field");

            Assert.AreEqual(1, norms.Length);

            IndexReader modifier = IndexReader.Open(dir);
            modifier.SetNorm(0, "field", 50);
            modifier.Close();

            IndexReader newReader = IndexReader.Open(dir);
            IndexReader reopenedReader = reader.Reopen();

            TestIndexReader.AssertIndexEquals(newReader, reopenedReader);
        }

        [Test]
        public virtual void TestDougBar()
        {
            int numDocs = 5;
            Directory dir = new RAMDirectory();

            IndexWriter writer = new IndexWriter(dir, new StandardAnalyzer(), IndexWriter.MaxFieldLength.LIMITED);
            Document doc;
            for (int i = 0; i < numDocs; i++)
            {
                doc = new Document();
                doc.Add(new Field("field", "content " + Lucene.Net.Util.English.IntToEnglish(i), Field.Store.NO, Field.Index.ANALYZED));
                writer.AddDocument(doc);
            }
            writer.Optimize();
            writer.Close();

            IndexReader modifier = IndexReader.Open(dir);
            for (int i = 0; i < numDocs; i++)
                modifier.SetNorm(i, "field", 50);
            modifier.Close();

            IndexReader reader = IndexReader.Open(dir);

            writer = new IndexWriter(dir, new StandardAnalyzer(), IndexWriter.MaxFieldLength.LIMITED);
            doc = new Document();
            doc.Add(new Field("field", "content " + Lucene.Net.Util.English.IntToEnglish(numDocs), Field.Store.NO, Field.Index.ANALYZED));
            writer.AddDocument(doc);
            writer.Close();

            IndexReader newReader = IndexReader.Open(dir);
            byte[] newNorms = newReader.Norms("field");

            IndexReader refreshed = reader.Reopen();
            byte[] refreshedNorms = refreshed.Norms("field");
           
            Assert.AreEqual(newNorms.Length, refreshedNorms.Length);
            for (int i = 0; i < newNorms.Length; i++)
                Assert.AreEqual(newNorms[i], refreshedNorms[i]);

            //TestIndexReader.AssertIndexEquals(newReader, refreshed);

            reader.Close();
            newReader.Close();
            refreshed.Close();
        }

        [Test]
        public virtual void TestThreadSafety()
        {
            Directory dir = new RAMDirectory();
            int n = 150;

            IndexWriter writer = new IndexWriter(dir, new StandardAnalyzer(), IndexWriter.MaxFieldLength.LIMITED);
            for (int i = 0; i < n; i++)
            {
                writer.AddDocument(CreateDocument(i, 3));
            }
            writer.Optimize();
            writer.Close();

            TestReopen_Renamed_Class test = new AnonymousClassTestReopen7(dir, n, this);

            System.Collections.IList readers = System.Collections.ArrayList.Synchronized(new System.Collections.ArrayList());
            IndexReader firstReader = IndexReader.Open(dir);
            IndexReader reader = firstReader;
            System.Random rnd = new System.Random();

            ReaderThread[] threads = new ReaderThread[n];
            System.Collections.Hashtable readersToClose = System.Collections.Hashtable.Synchronized(new System.Collections.Hashtable());

            // my test iterations
            System.Collections.ArrayList iterList = new System.Collections.ArrayList();
            for (int i = 0; i < n; i++)
            {
                iterList.Add(i);
                //if (i < 20) // AnonymousClassReaderThreadTask
                //    iterList.Add(i);
                //if (i >= 20 && i < 50) // AnonymousClassReaderThreadTask1
                ////if (i == 20) // AnonymousClassReaderThreadTask1
                //    iterList.Add(i);
                //if (i >= 50 && i < 70) // AnonymousClassReaderThreadTask
                //    iterList.Add(i);
            }

            //for (int h = 0; h < iterList.Count; h++)
            //    System.Console.WriteLine(iterList[h]);

            //for (int i = 0; i < n; i++)
            for (int h = 0; h < iterList.Count; h++)
            {
                // my test iterations
                int i = (int)iterList[h];

                if (i % 10 == 0)
                {
//System.Console.WriteLine("TestIndexReaderReopen.TestThreadSafety(): calling IndexReader.Reopen()");
                    IndexReader refreshed = reader.Reopen();
                    if (refreshed != reader)
                    {
                        if (!readersToClose.Contains(reader))
                            readersToClose.Add(reader, reader);
                    }
                    reader = refreshed;
                }
                IndexReader r = reader;

                int index = i;

                ReaderThreadTask task;

                if (i < 20 || (i >= 50 && i < 70) || i > 90)
                {
                    task = new AnonymousClassReaderThreadTask(index, r, test, readersToClose, readers, rnd, this);
                }
                else
                {
                    task = new AnonymousClassReaderThreadTask1(readers, rnd, this);
                }

                threads[i] = new ReaderThread(task);
                threads[i].Start();
            }

            lock (this)
            {
                try
                {
                    System.Threading.Monitor.Wait(this, 15000);
                }
                catch (System.Threading.ThreadInterruptedException)
                {
                }
            }

            for (int i = 0; i < n; i++)
            {
                if (threads[i] != null)
                {
                    threads[i].StopThread();
                }
            }

            for (int i = 0; i < n; i++)
            {
                if (threads[i] != null)
                {
                    try
                    {
                        threads[i].Join();
                        if (threads[i].error != null)
                        {
                            string msg = "Error occurred in thread " + threads[i].Name + ":\n" + threads[i].error.Message;
                            Assert.Fail(msg);
                        }
                    }
                    catch (System.Threading.ThreadInterruptedException)
                    {
                    }
                }
            }

            System.Collections.IEnumerator it = readersToClose.Keys.GetEnumerator();
            while (it.MoveNext())
            {
                ((IndexReader)it.Current).Close();
            }

            firstReader.Close();
            reader.Close();

            it = readersToClose.Keys.GetEnumerator();
            while (it.MoveNext())
            {
                AssertReaderClosed((IndexReader)it.Current, true, true);
            }

            AssertReaderClosed(reader, true, true);
            AssertReaderClosed(firstReader, true, true);
        }

        private class ReaderCouple
        {
            internal ReaderCouple(IndexReader r1, IndexReader r2)
            {
                newReader = r1;
                refreshedReader = r2;
            }

            internal IndexReader newReader;
            internal IndexReader refreshedReader;
        }

        abstract internal class ReaderThreadTask
        {
            protected internal bool stopped;
            public virtual void Stop()
            {
                this.stopped = true;
            }

            public abstract void Run();
        }

        private class ReaderThread : SupportClass.ThreadClass
        {
            private ReaderThreadTask task;
            internal System.Exception error;

            internal ReaderThread(ReaderThreadTask task)
            {
                this.task = task;
            }

            public virtual void StopThread()
            {
                this.task.Stop();
            }

            override public void Run()
            {
                try
                {
                    this.task.Run();
                }
                catch (System.Exception r)
                {
                    System.Console.Out.WriteLine(r.StackTrace);
                    this.error = r;
                }
            }
        }

        private System.Object createReaderMutex = new System.Object();

        private ReaderCouple RefreshReader(IndexReader reader, bool hasChanges)
        {
            return RefreshReader(reader, null, -1, hasChanges);
        }

        private ReaderCouple RefreshReader(IndexReader reader, TestReopen_Renamed_Class test, int modify, bool hasChanges)
        {
            lock (createReaderMutex)
            {
                IndexReader r = null;
                if (test != null)
                {
                    test.ModifyIndex(modify);
                    r = test.OpenReader();
                }

//System.Console.WriteLine("TestIndexReaderReopen.RefreshReader(): calling IndexReader.Reopen()");
                IndexReader refreshed = reader.Reopen();
                if (hasChanges)
                {
                    if (refreshed == reader)
                    {
                        Assert.Fail("No new IndexReader instance created during refresh.");
                    }
                }
                else
                {
                    if (refreshed != reader)
                    {
                        Assert.Fail("New IndexReader instance created during refresh even though index had no changes.");
                    }
                }
//TestIndexReader.AssertIndexEquals(r, refreshed);
                return new ReaderCouple(r, refreshed);
            }
        }

        private static void CreateIndex(Directory dir, bool multiSegment)
        {
            IndexWriter w = new IndexWriter(dir, new WhitespaceAnalyzer(), IndexWriter.MaxFieldLength.LIMITED);

            w.SetMergePolicy(new LogDocMergePolicy());

            for (int i = 0; i < 100; i++)
            {
                w.AddDocument(CreateDocument(i, 4));
                if (multiSegment && (i % 10) == 0)
                {
                    w.Flush();
                }
            }

            if (!multiSegment)
            {
                w.Optimize();
            }

            w.Close();

            IndexReader r = IndexReader.Open(dir);
            if (multiSegment)
            {
                Assert.IsTrue(r is MultiSegmentReader);
            }
            else
            {
                Assert.IsTrue(r is SegmentReader);
            }
            r.Close();
        }

        private static Document CreateDocument(int n, int numFields)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            Document doc = new Document();
            sb.Append("a");
            sb.Append(n);
            doc.Add(new Field("field1", sb.ToString(), Field.Store.YES, Field.Index.ANALYZED));
            sb.Append(" b");
            sb.Append(n);
            for (int i = 1; i < numFields; i++)
            {
                doc.Add(new Field("field" + (i + 1), sb.ToString(), Field.Store.YES, Field.Index.ANALYZED));
            }
            return doc;
        }

        private static void ModifyIndex(int i, Directory dir)
        {
            switch (i)
            {

                case 0:
                    {
                        IndexWriter w = new IndexWriter(dir, new WhitespaceAnalyzer(), IndexWriter.MaxFieldLength.LIMITED);
                        w.DeleteDocuments(new Term("field2", "a11"));
                        w.DeleteDocuments(new Term("field2", "b30"));
                        w.Close();
                        break;
                    }

                case 1:
                    {
                        IndexReader reader = IndexReader.Open(dir);
                        reader.SetNorm(4, "field1", 123);
                        reader.SetNorm(44, "field2", 222);
                        reader.SetNorm(44, "field4", 22);
                        reader.Close();
                        break;
                    }

                case 2:
                    {
                        IndexWriter w = new IndexWriter(dir, new WhitespaceAnalyzer(), IndexWriter.MaxFieldLength.LIMITED);
                        w.Optimize();
                        w.Close();
                        break;
                    }

                case 3:
                    {
                        IndexWriter w = new IndexWriter(dir, new WhitespaceAnalyzer(), IndexWriter.MaxFieldLength.LIMITED);
                        w.AddDocument(CreateDocument(101, 4));
                        w.Optimize();
                        w.AddDocument(CreateDocument(102, 4));
                        w.AddDocument(CreateDocument(103, 4));
                        w.Close();
                        break;
                    }

                case 4:
                    {
                        IndexReader reader = IndexReader.Open(dir);
                        reader.SetNorm(5, "field1", 123);
                        reader.SetNorm(55, "field2", 222);
                        reader.Close();
                        break;
                    }
            }
        }

        private void AssertReaderClosed(IndexReader reader, bool checkSubReaders, bool checkNormsClosed)
        {
            Assert.AreEqual(0, reader.GetRefCount());

            if (checkNormsClosed && reader is SegmentReader)
            {
                Assert.IsTrue(((SegmentReader)reader).NormsClosed());
            }

            if (checkSubReaders)
            {
                if (reader is MultiSegmentReader)
                {
                    SegmentReader[] subReaders = ((MultiSegmentReader)reader).GetSubReaders();
                    for (int i = 0; i < subReaders.Length; i++)
                    {
                        AssertReaderClosed(subReaders[i], checkSubReaders, checkNormsClosed);
                    }
                }

                if (reader is MultiReader)
                {
                    IndexReader[] subReaders = ((MultiReader)reader).GetSubReaders();
                    for (int i = 0; i < subReaders.Length; i++)
                    {
                        AssertReaderClosed(subReaders[i], checkSubReaders, checkNormsClosed);
                    }
                }

                if (reader is ParallelReader)
                {
                    IndexReader[] subReaders = ((ParallelReader)reader).GetSubReaders();
                    for (int i = 0; i < subReaders.Length; i++)
                    {
                        AssertReaderClosed(subReaders[i], checkSubReaders, checkNormsClosed);
                    }
                }
            }
        }

        private void AssertReaderOpen(IndexReader reader)
        {
            reader.EnsureOpen();

            if (reader is MultiSegmentReader)
            {
                SegmentReader[] subReaders = ((MultiSegmentReader)reader).GetSubReaders();
                for (int i = 0; i < subReaders.Length; i++)
                {
                    AssertReaderOpen(subReaders[i]);
                }
            }
        }

        private void AssertRefCountEquals(int refCount, IndexReader reader)
        {
            Assert.AreEqual(refCount, reader.GetRefCount(), "Reader has wrong refCount value.");
        }


        private abstract class TestReopen_Renamed_Class
        {
            protected internal abstract IndexReader OpenReader();
            protected internal abstract void ModifyIndex(int i);
        }

        [SetUp]
        override public void SetUp()
        {
            base.SetUp();
            string tempDir = SupportClass.AppSettings.Get("tempDir", "");
            if (tempDir == null || tempDir.Equals(""))
                throw new System.IO.IOException("tempDir undefined, cannot run test");
            indexDir = new System.IO.FileInfo(System.IO.Path.Combine(tempDir, "IndexReaderReopen"));
        }
    }
}