using Lucene.Net.Attributes;
using Lucene.Net.Documents;
using Lucene.Net.Support;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace Lucene.Net.Store
{
    using System.Reflection;
    using DirectoryReader = Lucene.Net.Index.DirectoryReader;
    using Document = Documents.Document;
    using Field = Field;
    using IndexReader = Lucene.Net.Index.IndexReader;
    using IndexSearcher = Lucene.Net.Search.IndexSearcher;
    using IndexWriter = Lucene.Net.Index.IndexWriter;
    using IndexWriterConfig = Lucene.Net.Index.IndexWriterConfig;
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
    using Query = Lucene.Net.Search.Query;
    using Term = Lucene.Net.Index.Term;
    using TermQuery = Lucene.Net.Search.TermQuery;

    [TestFixture]
    public class TestLockFactory : LuceneTestCase
    {
        // Verify: we can provide our own LockFactory implementation, the right
        // methods are called at the right time, locks are created, etc.
        [Test]
        public virtual void TestCustomLockFactory()
        {
            Directory dir = new MockDirectoryWrapper(Random(), new RAMDirectory());
            MockLockFactory lf = new MockLockFactory(this);
            dir.LockFactory = lf;

            // Lock prefix should have been set:
            Assert.IsTrue(lf.LockPrefixSet, "lock prefix was not set by the RAMDirectory");

            IndexWriter writer = new IndexWriter(dir, new IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())));

            // add 100 documents (so that commit lock is used)
            for (int i = 0; i < 100; i++)
            {
                AddDoc(writer);
            }

            // Both write lock and commit lock should have been created:
            Assert.AreEqual(1, lf.LocksCreated.Count, "# of unique locks created (after instantiating IndexWriter)");
            Assert.IsTrue(lf.MakeLockCount >= 1, "# calls to makeLock is 0 (after instantiating IndexWriter)");

            foreach (String lockName in lf.LocksCreated.Keys)
            {
                MockLockFactory.MockLock @lock = (MockLockFactory.MockLock)lf.LocksCreated[lockName];
                Assert.IsTrue(@lock.LockAttempts > 0, "# calls to Lock.obtain is 0 (after instantiating IndexWriter)");
            }

            writer.Dispose();
        }

        // Verify: we can use the NoLockFactory with RAMDirectory w/ no
        // exceptions raised:
        // Verify: NoLockFactory allows two IndexWriters
        [Test]
        public virtual void TestRAMDirectoryNoLocking()
        {
            MockDirectoryWrapper dir = new MockDirectoryWrapper(Random(), new RAMDirectory());
            dir.LockFactory = NoLockFactory.DoNoLockFactory;
            dir.WrapLockFactory = false; // we are gonna explicitly test we get this back
            Assert.IsTrue(typeof(NoLockFactory).IsInstanceOfType(dir.LockFactory), "RAMDirectory.setLockFactory did not take");

            IndexWriter writer = new IndexWriter(dir, new IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())));
            writer.Commit(); // required so the second open succeed
            // Create a 2nd IndexWriter.  this is normally not allowed but it should run through since we're not
            // using any locks:
            IndexWriter writer2 = null;
            try
            {
                writer2 = new IndexWriter(dir, (new IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random()))).SetOpenMode(IndexWriterConfig.OpenMode_e.APPEND));
            }
            catch (Exception e)
            {
                Console.Out.Write(e.StackTrace);
                Assert.Fail("Should not have hit an IOException with no locking");
            }

            writer.Dispose();
            if (writer2 != null)
            {
                writer2.Dispose();
            }
        }

        // Verify: SingleInstanceLockFactory is the default lock for RAMDirectory
        // Verify: RAMDirectory does basic locking correctly (can't create two IndexWriters)
        [Test]
        public virtual void TestDefaultRAMDirectory()
        {
            Directory dir = new RAMDirectory();

            Assert.IsTrue(typeof(SingleInstanceLockFactory).IsInstanceOfType(dir.LockFactory), "RAMDirectory did not use correct LockFactory: got " + dir.LockFactory);

            IndexWriter writer = new IndexWriter(dir, new IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())));

            // Create a 2nd IndexWriter.  this should fail:
            IndexWriter writer2 = null;
            try
            {
                writer2 = new IndexWriter(dir, (new IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random()))).SetOpenMode(IndexWriterConfig.OpenMode_e.APPEND));
                Assert.Fail("Should have hit an IOException with two IndexWriters on default SingleInstanceLockFactory");
            }
            catch (IOException e)
            {
            }

            writer.Dispose();
            if (writer2 != null)
            {
                writer2.Dispose();
            }
        }

        [Test]
        public virtual void TestSimpleFSLockFactory()
        {
            // test string file instantiation
            new SimpleFSLockFactory("test");
        }

        // Verify: do stress test, by opening IndexReaders and
        // IndexWriters over & over in 2 threads and making sure
        // no unexpected exceptions are raised:
        [Test]
        public virtual void TestStressLocks()
        {
            _testStressLocks(null, CreateTempDir("index.TestLockFactory6"));
        }

        // Verify: do stress test, by opening IndexReaders and
        // IndexWriters over & over in 2 threads and making sure
        // no unexpected exceptions are raised, but use
        // NativeFSLockFactory:
        [Test]
        public virtual void TestStressLocksNativeFSLockFactory()
        {
            DirectoryInfo dir = CreateTempDir("index.TestLockFactory7");
            _testStressLocks(new NativeFSLockFactory(dir), dir);
        }

        public virtual void _testStressLocks(LockFactory lockFactory, DirectoryInfo indexDir)
        {
            Directory dir = NewFSDirectory(indexDir, lockFactory);

            // First create a 1 doc index:
            IndexWriter w = new IndexWriter(dir, (new IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random()))).SetOpenMode(IndexWriterConfig.OpenMode_e.CREATE));
            AddDoc(w);
            w.Dispose();

            WriterThread writer = new WriterThread(this, 100, dir);
            SearcherThread searcher = new SearcherThread(this, 100, dir);
            writer.Start();
            searcher.Start();

            while (writer.IsAlive || searcher.IsAlive)
            {
                Thread.Sleep(1000);
            }

            Assert.IsTrue(!writer.HitException, "IndexWriter hit unexpected exceptions");
            Assert.IsTrue(!searcher.HitException, "IndexSearcher hit unexpected exceptions");

            dir.Dispose();
            // Cleanup
            System.IO.Directory.Delete(indexDir.FullName, true);
        }

        // Verify: NativeFSLockFactory works correctly
        [Test, MaxTime(int.MaxValue)]
        public virtual void TestNativeFSLockFactory()
        {
            var f = new NativeFSLockFactory(CreateTempDir("testNativeFsLockFactory"));

            f.LockPrefix = "test";
            var l = f.MakeLock("commit");
            var l2 = f.MakeLock("commit");

            Assert.IsTrue(l.Obtain(), "failed to obtain lock, got exception: {0}", l.FailureReason);
            Assert.IsTrue(!l2.Obtain(), "succeeded in obtaining lock twice");
            l.Dispose();

            Assert.IsTrue(l2.Obtain(), "failed to obtain 2nd lock after first one was freed, got exception: {0}", l2.FailureReason);
            l2.Dispose();

            // Make sure we can obtain first one again, test isLocked():
            Assert.IsTrue(l.Obtain(), "failed to obtain lock, got exception: {0}", l.FailureReason);
            Assert.IsTrue(l.Locked);
            Assert.IsTrue(l2.Locked);
            l.Dispose();
            Assert.IsFalse(l.Locked);
            Assert.IsFalse(l2.Locked);
        }

        // Verify: NativeFSLockFactory works correctly if the lock file exists
        [Test]
        public virtual void TestNativeFSLockFactoryLockExists()
        {
            DirectoryInfo tempDir = CreateTempDir("testNativeFsLockFactory");

            // Touch the lock file
            var lockFile = new FileInfo(Path.Combine(tempDir.FullName, "test.lock"));
            using (lockFile.Create()){};

            var l = (new NativeFSLockFactory(tempDir)).MakeLock("test.lock");
            Assert.IsTrue(l.Obtain(), "failed to obtain lock, got exception: {0}", l.FailureReason);
            l.Dispose();
            Assert.IsFalse(l.Locked, "failed to release lock, got exception: {0}", l.FailureReason);
            if (lockFile.Exists)
            {
                lockFile.Delete();
            }
        }

        // Verify: NativeFSLockFactory assigns null as lockPrefix if the lockDir is inside directory
        [Test]
        public virtual void TestNativeFSLockFactoryPrefix()
        {
            DirectoryInfo fdir1 = CreateTempDir("TestLockFactory.8");
            DirectoryInfo fdir2 = CreateTempDir("TestLockFactory.8.Lockdir");
            Directory dir1 = NewFSDirectory(fdir1, new NativeFSLockFactory(fdir1));
            // same directory, but locks are stored somewhere else. The prefix of the lock factory should != null
            Directory dir2 = NewFSDirectory(fdir1, new NativeFSLockFactory(fdir2));

            string prefix1 = dir1.LockFactory.LockPrefix;
            Assert.IsNull(prefix1, "Lock prefix for lockDir same as directory should be null");

            string prefix2 = dir2.LockFactory.LockPrefix;
            Assert.IsNotNull(prefix2, "Lock prefix for lockDir outside of directory should be not null");

            dir1.Dispose();
            dir2.Dispose();
            System.IO.Directory.Delete(fdir1.FullName, true);
            System.IO.Directory.Delete(fdir2.FullName, true);
        }

        // Verify: default LockFactory has no prefix (ie
        // write.lock is stored in index):
        [Test]
        public virtual void TestDefaultFSLockFactoryPrefix()
        {
            // Make sure we get null prefix, which wont happen if setLockFactory is ever called.
            DirectoryInfo dirName = CreateTempDir("TestLockFactory.10");

            Directory dir = new SimpleFSDirectory(dirName);
            Assert.IsNull(dir.LockFactory.LockPrefix, "Default lock prefix should be null");
            dir.Dispose();

            dir = new MMapDirectory(dirName);
            Assert.IsNull(dir.LockFactory.LockPrefix, "Default lock prefix should be null");
            dir.Dispose();

            dir = new NIOFSDirectory(dirName);
            Assert.IsNull(dir.LockFactory.LockPrefix, "Default lock prefix should be null");
            dir.Dispose();

            System.IO.Directory.Delete(dirName.FullName, true);
        }

        private class WriterThread : ThreadClass
        {
            private readonly TestLockFactory OuterInstance;

            internal Directory Dir;
            internal int NumIteration;
            public bool HitException = false;

            public WriterThread(TestLockFactory outerInstance, int numIteration, Directory dir)
            {
                this.OuterInstance = outerInstance;
                this.NumIteration = numIteration;
                this.Dir = dir;
            }

            public override void Run()
            {
                IndexWriter writer = null;
                for (int i = 0; i < this.NumIteration; i++)
                {
                    try
                    {
                        writer = new IndexWriter(Dir, (new IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random()))).SetOpenMode(IndexWriterConfig.OpenMode_e.APPEND));
                    }
                    catch (IOException e)
                    {
                        if (e.ToString().IndexOf(" timed out:") == -1)
                        {
                            HitException = true;
                            Console.WriteLine("Stress Test Index Writer: creation hit unexpected IOException: " + e.ToString());
                            Console.Out.Write(e.StackTrace);
                        }
                        else
                        {
                            // lock obtain timed out
                            // NOTE: we should at some point
                            // consider this a failure?  The lock
                            // obtains, across IndexReader &
                            // IndexWriters should be "fair" (ie
                            // FIFO).
                        }
                    }
                    catch (Exception e)
                    {
                        HitException = true;
                        Console.WriteLine("Stress Test Index Writer: creation hit unexpected exception: " + e.ToString());
                        Console.Out.Write(e.StackTrace);
                        break;
                    }
                    if (writer != null)
                    {
                        try
                        {
                            OuterInstance.AddDoc(writer);
                        }
                        catch (IOException e)
                        {
                            HitException = true;
                            Console.WriteLine("Stress Test Index Writer: addDoc hit unexpected exception: " + e.ToString());
                            Console.Out.Write(e.StackTrace);
                            break;
                        }
                        try
                        {
                            writer.Dispose();
                        }
                        catch (IOException e)
                        {
                            HitException = true;
                            Console.WriteLine("Stress Test Index Writer: close hit unexpected exception: " + e.ToString());
                            Console.Out.Write(e.StackTrace);
                            break;
                        }
                        writer = null;
                    }
                }
            }
        }

        private class SearcherThread : ThreadClass
        {
            private readonly TestLockFactory OuterInstance;

            internal Directory Dir;
            internal int NumIteration;
            public bool HitException = false;

            public SearcherThread(TestLockFactory outerInstance, int numIteration, Directory dir)
            {
                this.OuterInstance = outerInstance;
                this.NumIteration = numIteration;
                this.Dir = dir;
            }

            public override void Run()
            {
                IndexReader reader = null;
                IndexSearcher searcher = null;
                Query query = new TermQuery(new Term("content", "aaa"));
                for (int i = 0; i < this.NumIteration; i++)
                {
                    try
                    {
                        reader = DirectoryReader.Open(Dir);
                        searcher = OuterInstance.NewSearcher(reader);
                    }
                    catch (Exception e)
                    {
                        HitException = true;
                        Console.WriteLine("Stress Test Index Searcher: create hit unexpected exception: " + e.ToString());
                        Console.Out.Write(e.StackTrace);
                        break;
                    }
                    try
                    {
                        searcher.Search(query, null, 1000);
                    }
                    catch (IOException e)
                    {
                        HitException = true;
                        Console.WriteLine("Stress Test Index Searcher: search hit unexpected exception: " + e.ToString());
                        Console.Out.Write(e.StackTrace);
                        break;
                    }
                    // System.out.println(hits.Length() + " total results");
                    try
                    {
                        reader.Dispose();
                    }
                    catch (IOException e)
                    {
                        HitException = true;
                        Console.WriteLine("Stress Test Index Searcher: close hit unexpected exception: " + e.ToString());
                        Console.Out.Write(e.StackTrace);
                        break;
                    }
                }
            }
        }

        public class MockLockFactory : LockFactory
        {
            private readonly TestLockFactory OuterInstance;

            public MockLockFactory(TestLockFactory outerInstance)
            {
                this.OuterInstance = outerInstance;
            }

            public bool LockPrefixSet;
            public IDictionary<string, Lock> LocksCreated = /*CollectionsHelper.SynchronizedMap(*/new Dictionary<string, Lock>()/*)*/;
            public int MakeLockCount = 0;

            public override string LockPrefix
            {
                set
                {
                    base.LockPrefix = value;
                    LockPrefixSet = true;
                }
            }

            public override Lock MakeLock(string lockName)
            {
                lock (this)
                {
                    Lock @lock = new MockLock(this);
                    LocksCreated[lockName] = @lock;
                    MakeLockCount++;
                    return @lock;
                }
            }

            public override void ClearLock(string specificLockName)
            {
            }

            public class MockLock : Lock
            {
                private readonly TestLockFactory.MockLockFactory OuterInstance;

                public MockLock(TestLockFactory.MockLockFactory outerInstance)
                {
                    this.OuterInstance = outerInstance;
                }

                public int LockAttempts;

                public override bool Obtain()
                {
                    LockAttempts++;
                    return true;
                }

                public override void Release()
                {
                    // do nothing
                }

                public override bool Locked
                {
                    get
                    {
                        return false;
                    }
                }
            }
        }

        private void AddDoc(IndexWriter writer)
        {
            Document doc = new Document();
            doc.Add(NewTextField("content", "aaa", Field.Store.NO));
            writer.AddDocument(doc);
        }
    }
}