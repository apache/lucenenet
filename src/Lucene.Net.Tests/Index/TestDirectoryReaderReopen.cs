using J2N.Threading;
using Lucene.Net.Documents;
using Lucene.Net.Index.Extensions;
using Lucene.Net.Support;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using JCG = J2N.Collections.Generic;
using Assert = Lucene.Net.TestFramework.Assert;
using Console = Lucene.Net.Util.SystemConsole;
using Lucene.Net.Support.Threading;

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
    using Field = Field;
    using FieldType = FieldType;
    using IndexSearcher = Lucene.Net.Search.IndexSearcher;
    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
    using MockAnalyzer = Lucene.Net.Analysis.MockAnalyzer;
    using ScoreDoc = Lucene.Net.Search.ScoreDoc;
    using TermQuery = Lucene.Net.Search.TermQuery;
    using TestUtil = Lucene.Net.Util.TestUtil;
    using TextField = TextField;

    [TestFixture]
    public class TestDirectoryReaderReopen : LuceneTestCase
    {
        [Test]
        public virtual void TestReopen_Mem()
        {
            Directory dir1 = NewDirectory();

            CreateIndex(Random, dir1, false);
            PerformDefaultTests(new TestReopenAnonymousClass(this, dir1));
            dir1.Dispose();

            Directory dir2 = NewDirectory();

            CreateIndex(Random, dir2, true);
            PerformDefaultTests(new TestReopenAnonymousClass2(this, dir2));
            dir2.Dispose();
        }

        private sealed class TestReopenAnonymousClass : TestReopen
        {
            private readonly TestDirectoryReaderReopen outerInstance;

            private Directory dir1;

            public TestReopenAnonymousClass(TestDirectoryReaderReopen outerInstance, Directory dir1)
            {
                this.outerInstance = outerInstance;
                this.dir1 = dir1;
            }

            protected internal override void ModifyIndex(int i)
            {
                TestDirectoryReaderReopen.ModifyIndex(i, dir1);
            }

            protected internal override DirectoryReader OpenReader()
            {
                return DirectoryReader.Open(dir1);
            }
        }

        private sealed class TestReopenAnonymousClass2 : TestReopen
        {
            private readonly TestDirectoryReaderReopen outerInstance;

            private readonly Directory dir2;

            public TestReopenAnonymousClass2(TestDirectoryReaderReopen outerInstance, Directory dir2)
            {
                this.outerInstance = outerInstance;
                this.dir2 = dir2;
            }

            protected internal override void ModifyIndex(int i)
            {
                TestDirectoryReaderReopen.ModifyIndex(i, dir2);
            }

            protected internal override DirectoryReader OpenReader()
            {
                return DirectoryReader.Open(dir2);
            }
        }

        // LUCENE-1228: IndexWriter.Commit() does not update the index version
        // populate an index in iterations.
        // at the end of every iteration, commit the index and reopen/recreate the reader.
        // in each iteration verify the work of previous iteration.
        // try this once with reopen once recreate, on both RAMDir and FSDir.
        [Test]
        public virtual void TestCommitReopen()
        {
            Directory dir = NewDirectory();
            DoTestReopenWithCommit(Random, dir, true);
            dir.Dispose();
        }

        [Test]
        public virtual void TestCommitRecreate()
        {
            Directory dir = NewDirectory();
            DoTestReopenWithCommit(Random, dir, false);
            dir.Dispose();
        }

        private void DoTestReopenWithCommit(Random random, Directory dir, bool withReopen)
        {
            IndexWriter iwriter = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random)).SetOpenMode(OpenMode.CREATE).SetMergeScheduler(new SerialMergeScheduler()).SetMergePolicy(NewLogMergePolicy()));
            iwriter.Commit();
            DirectoryReader reader = DirectoryReader.Open(dir);
            try
            {
                int M = 3;
                FieldType customType = new FieldType(TextField.TYPE_STORED);
                customType.IsTokenized = false;
                FieldType customType2 = new FieldType(TextField.TYPE_STORED);
                customType2.IsTokenized = false;
                customType2.OmitNorms = true;
                FieldType customType3 = new FieldType();
                customType3.IsStored = true;
                for (int i = 0; i < 4; i++)
                {
                    for (int j = 0; j < M; j++)
                    {
                        Document doc = new Document();
                        doc.Add(NewField("id", i + "_" + j, customType));
                        doc.Add(NewField("id2", i + "_" + j, customType2));
                        doc.Add(NewField("id3", i + "_" + j, customType3));
                        iwriter.AddDocument(doc);
                        if (i > 0)
                        {
                            int k = i - 1;
                            int n = j + k * M;
                            Document prevItereationDoc = reader.Document(n);
                            Assert.IsNotNull(prevItereationDoc);
                            string id = prevItereationDoc.Get("id");
                            Assert.AreEqual(k + "_" + j, id);
                        }
                    }
                    iwriter.Commit();
                    if (withReopen)
                    {
                        // reopen
                        DirectoryReader r2 = DirectoryReader.OpenIfChanged(reader);
                        if (r2 != null)
                        {
                            reader.Dispose();
                            reader = r2;
                        }
                    }
                    else
                    {
                        // recreate
                        reader.Dispose();
                        reader = DirectoryReader.Open(dir);
                    }
                }
            }
            finally
            {
                iwriter.Dispose();
                reader.Dispose();
            }
        }

        private void PerformDefaultTests(TestReopen test)
        {
            DirectoryReader index1 = test.OpenReader();
            DirectoryReader index2 = test.OpenReader();

            TestDirectoryReader.AssertIndexEquals(index1, index2);

            // verify that reopen() does not return a new reader instance
            // in case the index has no changes
            ReaderCouple couple = RefreshReader(index2, false);
            Assert.IsTrue(couple.refreshedReader == index2);

            couple = RefreshReader(index2, test, 0, true);
            index1.Dispose();
            index1 = couple.newReader;

            DirectoryReader index2_refreshed = couple.refreshedReader;
            index2.Dispose();

            // test if refreshed reader and newly opened reader return equal results
            TestDirectoryReader.AssertIndexEquals(index1, index2_refreshed);

            index2_refreshed.Dispose();
            AssertReaderClosed(index2, true);
            AssertReaderClosed(index2_refreshed, true);

            index2 = test.OpenReader();

            for (int i = 1; i < 4; i++)
            {
                index1.Dispose();
                couple = RefreshReader(index2, test, i, true);
                // refresh DirectoryReader
                index2.Dispose();

                index2 = couple.refreshedReader;
                index1 = couple.newReader;
                TestDirectoryReader.AssertIndexEquals(index1, index2);
            }

            index1.Dispose();
            index2.Dispose();
            AssertReaderClosed(index1, true);
            AssertReaderClosed(index2, true);
        }

        [Test]
        public virtual void TestThreadSafety()
        {
            Directory dir = NewDirectory();
            // NOTE: this also controls the number of threads!
            int n = TestUtil.NextInt32(Random, 20, 40);
            IndexWriter writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)));
            for (int i = 0; i < n; i++)
            {
                writer.AddDocument(CreateDocument(i, 3));
            }
            writer.ForceMerge(1);
            writer.Dispose();

            TestReopen test = new TestReopenAnonymousClass3(this, dir, n);

            IList<ReaderCouple> readers = new SynchronizedList<ReaderCouple>();
            DirectoryReader firstReader = DirectoryReader.Open(dir);
            DirectoryReader reader = firstReader;

            ReaderThread[] threads = new ReaderThread[n];
            ISet<DirectoryReader> readersToClose = new JCG.HashSet<DirectoryReader>().AsConcurrent();

            for (int i = 0; i < n; i++)
            {
                if (i % 2 == 0)
                {
                    DirectoryReader refreshed = DirectoryReader.OpenIfChanged(reader);
                    if (refreshed != null)
                    {
                        readersToClose.Add(reader);
                        reader = refreshed;
                    }
                }
                DirectoryReader r = reader;

                int index = i;

                ReaderThreadTask task;

                if (i < 4 || (i >= 10 && i < 14) || i > 18)
                {
                    task = new ReaderThreadTaskAnonymousClass(this, test, readers, readersToClose, r, index);
                }
                else
                {
                    task = new ReaderThreadTaskAnonymousClass2(this, readers);
                }

                threads[i] = new ReaderThread(task);
                threads[i].Start();
            }

            UninterruptableMonitor.Enter(this);
            try
            {
                UninterruptableMonitor.Wait(this, TimeSpan.FromMilliseconds(1000));
            }
            finally
            {
                UninterruptableMonitor.Exit(this);
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
                    threads[i].Join();
                    if (threads[i].error != null)
                    {
                        string msg = "Error occurred in thread " + threads[i].Name + ":\n" + threads[i].error.Message;
                        Assert.Fail(msg);
                    }
                }
            }

            foreach (DirectoryReader readerToClose in readersToClose)
            {
                readerToClose.Dispose();
            }

            firstReader.Dispose();
            reader.Dispose();

            foreach (DirectoryReader readerToClose in readersToClose)
            {
                AssertReaderClosed(readerToClose, true);
            }

            AssertReaderClosed(reader, true);
            AssertReaderClosed(firstReader, true);

            dir.Dispose();
        }

        private sealed class TestReopenAnonymousClass3 : TestReopen
        {
            private readonly TestDirectoryReaderReopen outerInstance;

            private readonly Directory dir;
            private readonly int n;

            public TestReopenAnonymousClass3(TestDirectoryReaderReopen outerInstance, Directory dir, int n)
            {
                this.outerInstance = outerInstance;
                this.dir = dir;
                this.n = n;
            }

            protected internal override void ModifyIndex(int i)
            {
                IndexWriter modifier = new IndexWriter(dir, new IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)));
                modifier.AddDocument(CreateDocument(n + i, 6));
                modifier.Dispose();
            }

            protected internal override DirectoryReader OpenReader()
            {
                return DirectoryReader.Open(dir);
            }
        }

        private sealed class ReaderThreadTaskAnonymousClass : ReaderThreadTask
        {
            private readonly TestDirectoryReaderReopen outerInstance;

            private readonly TestReopen test;
            private readonly IList<ReaderCouple> readers;
            private readonly ISet<DirectoryReader> readersToClose;
            private readonly DirectoryReader r;
            private readonly int index;

            public ReaderThreadTaskAnonymousClass(TestDirectoryReaderReopen outerInstance, Lucene.Net.Index.TestDirectoryReaderReopen.TestReopen test, IList<ReaderCouple> readers, ISet<DirectoryReader> readersToClose, DirectoryReader r, int index)
            {
                this.outerInstance = outerInstance;
                this.test = test;
                this.readers = readers;
                this.readersToClose = readersToClose;
                this.r = r;
                this.index = index;
            }

            public override void Run()
            {
                Random rnd = LuceneTestCase.Random;
                while (!stopped)
                {
                    if (index % 2 == 0)
                    {
                        // refresh reader synchronized
                        ReaderCouple c = (outerInstance.RefreshReader(r, test, index, true));
                        readersToClose.Add(c.newReader);
                        readersToClose.Add(c.refreshedReader);
                        readers.Add(c);
                        // prevent too many readers
                        break;
                    }
                    else
                    {
                        // not synchronized
                        DirectoryReader refreshed = DirectoryReader.OpenIfChanged(r);
                        if (refreshed is null)
                        {
                            refreshed = r;
                        }

                        IndexSearcher searcher = NewSearcher(refreshed);
                        ScoreDoc[] hits = searcher.Search(new TermQuery(new Term("field1", "a" + rnd.Next(refreshed.MaxDoc))), null, 1000).ScoreDocs;
                        if (hits.Length > 0)
                        {
                            searcher.Doc(hits[0].Doc);
                        }
                        if (refreshed != r)
                        {
                            refreshed.Dispose();
                        }
                    }
                    UninterruptableMonitor.Enter(this);
                    try
                    {
                        UninterruptableMonitor.Wait(this, TimeSpan.FromMilliseconds(TestUtil.NextInt32(Random, 1, 100)));
                    }
                    finally
                    {
                        UninterruptableMonitor.Exit(this);
                    }
                }
            }
        }

        private sealed class ReaderThreadTaskAnonymousClass2 : ReaderThreadTask
        {
            private readonly TestDirectoryReaderReopen outerInstance;

            private readonly IList<ReaderCouple> readers;

            public ReaderThreadTaskAnonymousClass2(TestDirectoryReaderReopen outerInstance, IList<ReaderCouple> readers)
            {
                this.outerInstance = outerInstance;
                this.readers = readers;
            }

            public override void Run()
            {
                Random rnd = LuceneTestCase.Random;
                while (!stopped)
                {
                    int numReaders = readers.Count;
                    if (numReaders > 0)
                    {
                        ReaderCouple c = readers[rnd.Next(numReaders)];
                        TestDirectoryReader.AssertIndexEquals(c.newReader, c.refreshedReader);
                    }

                    UninterruptableMonitor.Enter(this);
                    try
                    {
                        UninterruptableMonitor.Wait(this, TimeSpan.FromMilliseconds(TestUtil.NextInt32(Random, 1, 100)));
                    }
                    finally
                    {
                        UninterruptableMonitor.Exit(this);
                    }
                }
            }
        }

        internal class ReaderCouple
        {
            internal ReaderCouple(DirectoryReader r1, DirectoryReader r2)
            {
                newReader = r1;
                refreshedReader = r2;
            }

            internal DirectoryReader newReader;
            internal DirectoryReader refreshedReader;
        }

        internal abstract class ReaderThreadTask
        {
            protected internal volatile bool stopped;

            public virtual void Stop()
            {
                this.stopped = true;
            }

            public abstract void Run();
        }

        private class ReaderThread : ThreadJob
        {
            internal ReaderThreadTask task;
            internal Exception error;

            internal ReaderThread(ReaderThreadTask task)
            {
                this.task = task;
            }

            public virtual void StopThread()
            {
                this.task.Stop();
            }

            public override void Run()
            {
                try
                {
                    this.task.Run();
                }
                catch (Exception r) when (r.IsThrowable())
                {
                    Console.WriteLine(r.StackTrace);
                    this.error = r;
                }
            }
        }

        private object createReaderMutex = new object();

        private ReaderCouple RefreshReader(DirectoryReader reader, bool hasChanges)
        {
            return RefreshReader(reader, null, -1, hasChanges);
        }

        internal virtual ReaderCouple RefreshReader(DirectoryReader reader, TestReopen test, int modify, bool hasChanges)
        {
            UninterruptableMonitor.Enter(createReaderMutex);
            try
            {
                DirectoryReader r = null;
                if (test != null)
                {
                    test.ModifyIndex(modify);
                    r = test.OpenReader();
                }

                DirectoryReader refreshed = null;
                try
                {
                    refreshed = DirectoryReader.OpenIfChanged(reader);
                    if (refreshed is null)
                    {
                        refreshed = reader;
                    }
                }
                finally
                {
                    if (refreshed is null && r != null)
                    {
                        // Hit exception -- close opened reader
                        r.Dispose();
                    }
                }

                if (hasChanges)
                {
                    if (refreshed == reader)
                    {
                        Assert.Fail("No new DirectoryReader instance created during refresh.");
                    }
                }
                else
                {
                    if (refreshed != reader)
                    {
                        Assert.Fail("New DirectoryReader instance created during refresh even though index had no changes.");
                    }
                }

                return new ReaderCouple(r, refreshed);
            }
            finally
            {
                UninterruptableMonitor.Exit(createReaderMutex);
            }
        }

        /// <summary>
        /// LUCENENET specific
        /// Is non-static because NewIndexWriterConfig is no longer static.
        /// </summary>
        public void CreateIndex(Random random, Directory dir, bool multiSegment)
        {
            IndexWriter.Unlock(dir);
            IndexWriter w = new IndexWriter(dir, NewIndexWriterConfig(random, TEST_VERSION_CURRENT, new MockAnalyzer(random)).SetMergePolicy(new LogDocMergePolicy()));

            for (int i = 0; i < 100; i++)
            {
                w.AddDocument(CreateDocument(i, 4));
                if (multiSegment && (i % 10) == 0)
                {
                    w.Commit();
                }
            }

            if (!multiSegment)
            {
                w.ForceMerge(1);
            }

            w.Dispose();

            DirectoryReader r = DirectoryReader.Open(dir);
            if (multiSegment)
            {
                Assert.IsTrue(r.Leaves.Count > 1);
            }
            else
            {
                Assert.IsTrue(r.Leaves.Count == 1);
            }
            r.Dispose();
        }

        public static Document CreateDocument(int n, int numFields)
        {
            StringBuilder sb = new StringBuilder();
            Document doc = new Document();
            sb.Append('a');
            sb.Append(n);
            FieldType customType2 = new FieldType(TextField.TYPE_STORED);
            customType2.IsTokenized = false;
            customType2.OmitNorms = true;
            FieldType customType3 = new FieldType();
            customType3.IsStored = true;
            doc.Add(new TextField("field1", sb.ToString(), Field.Store.YES));
            doc.Add(new Field("fielda", sb.ToString(), customType2));
            doc.Add(new Field("fieldb", sb.ToString(), customType3));
            sb.Append(" b");
            sb.Append(n);
            for (int i = 1; i < numFields; i++)
            {
                doc.Add(new TextField("field" + (i + 1), sb.ToString(), Field.Store.YES));
            }
            return doc;
        }

        internal static void ModifyIndex(int i, Directory dir)
        {
            switch (i)
            {
                case 0:
                    {
                        if (Verbose)
                        {
                            Console.WriteLine("TEST: modify index");
                        }
                        IndexWriter w = new IndexWriter(dir, new IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)));
                        w.DeleteDocuments(new Term("field2", "a11"));
                        w.DeleteDocuments(new Term("field2", "b30"));
                        w.Dispose();
                        break;
                    }
                case 1:
                    {
                        IndexWriter w = new IndexWriter(dir, new IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)));
                        w.ForceMerge(1);
                        w.Dispose();
                        break;
                    }
                case 2:
                    {
                        IndexWriter w = new IndexWriter(dir, new IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)));
                        w.AddDocument(CreateDocument(101, 4));
                        w.ForceMerge(1);
                        w.AddDocument(CreateDocument(102, 4));
                        w.AddDocument(CreateDocument(103, 4));
                        w.Dispose();
                        break;
                    }
                case 3:
                    {
                        IndexWriter w = new IndexWriter(dir, new IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)));
                        w.AddDocument(CreateDocument(101, 4));
                        w.Dispose();
                        break;
                    }
            }
        }

        internal static void AssertReaderClosed(IndexReader reader, bool checkSubReaders)
        {
            Assert.AreEqual(0, reader.RefCount);

            if (checkSubReaders && reader is CompositeReader)
            {
                // we cannot use reader context here, as reader is
                // already closed and calling getTopReaderContext() throws AlreadyClosed!
                IList<IndexReader> subReaders = ((CompositeReader)reader).GetSequentialSubReaders();
                foreach (IndexReader r in subReaders)
                {
                    AssertReaderClosed(r, checkSubReaders);
                }
            }
        }

        internal abstract class TestReopen
        {
            protected internal abstract DirectoryReader OpenReader();

            protected internal abstract void ModifyIndex(int i);
        }

        internal class KeepAllCommits : IndexDeletionPolicy
        {
            public override void OnInit<T>(IList<T> commits)
            {
            }

            public override void OnCommit<T>(IList<T> commits)
            {
            }
        }

        [Test]
        public virtual void TestReopenOnCommit()
        {
            Directory dir = NewDirectory();
            IndexWriter writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)).SetIndexDeletionPolicy(new KeepAllCommits()).SetMaxBufferedDocs(-1).SetMergePolicy(NewLogMergePolicy(10)));
            for (int i = 0; i < 4; i++)
            {
                Document doc = new Document();
                doc.Add(NewStringField("id", "" + i, Field.Store.NO));
                writer.AddDocument(doc);
                IDictionary<string, string> data = new Dictionary<string, string>();
                data["index"] = i + "";
                writer.SetCommitData(data);
                writer.Commit();
            }
            for (int i = 0; i < 4; i++)
            {
                writer.DeleteDocuments(new Term("id", "" + i));
                IDictionary<string, string> data = new Dictionary<string, string>();
                data["index"] = (4 + i) + "";
                writer.SetCommitData(data);
                writer.Commit();
            }
            writer.Dispose();

            DirectoryReader r = DirectoryReader.Open(dir);
            Assert.AreEqual(0, r.NumDocs);

            ICollection<IndexCommit> commits = DirectoryReader.ListCommits(dir);
            foreach (IndexCommit commit in commits)
            {
                DirectoryReader r2 = DirectoryReader.OpenIfChanged(r, commit);
                Assert.IsNotNull(r2);
                Assert.IsTrue(r2 != r);

                IDictionary<string, string> s = commit.UserData;
                int v;
                if (s.Count == 0)
                {
                    // First commit created by IW
                    v = -1;
                }
                else
                {
                    v = Convert.ToInt32(s["index"]);
                }
                if (v < 4)
                {
                    Assert.AreEqual(1 + v, r2.NumDocs);
                }
                else
                {
                    Assert.AreEqual(7 - v, r2.NumDocs);
                }
                r.Dispose();
                r = r2;
            }
            r.Dispose();
            dir.Dispose();
        }

        [Test]
        public virtual void TestOpenIfChangedNRTToCommit()
        {
            Directory dir = NewDirectory();

            // Can't use RIW because it randomly commits:
            IndexWriter w = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)));
            Document doc = new Document();
            doc.Add(NewStringField("field", "value", Field.Store.NO));
            w.AddDocument(doc);
            w.Commit();
            IList<IndexCommit> commits = DirectoryReader.ListCommits(dir);
            Assert.AreEqual(1, commits.Count);
            w.AddDocument(doc);
            DirectoryReader r = DirectoryReader.Open(w, true);

            Assert.AreEqual(2, r.NumDocs);
            IndexReader r2 = DirectoryReader.OpenIfChanged(r, commits[0]);
            Assert.IsNotNull(r2);
            r.Dispose();
            Assert.AreEqual(1, r2.NumDocs);
            w.Dispose();
            r2.Dispose();
            dir.Dispose();
        }
    }
}