using System;
using System.Collections.Generic;
using System.Threading;

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


	using MockAnalyzer = Lucene.Net.Analysis.MockAnalyzer;
	using Document = Lucene.Net.Document.Document;
	using Field = Lucene.Net.Document.Field;
	using DirectoryReader = Lucene.Net.Index.DirectoryReader;
	using IndexReader = Lucene.Net.Index.IndexReader;
	using IndexWriter = Lucene.Net.Index.IndexWriter;
	using IndexWriterConfig = Lucene.Net.Index.IndexWriterConfig;
	using Term = Lucene.Net.Index.Term;
	using OpenMode = Lucene.Net.Index.IndexWriterConfig.OpenMode_e;
	using IndexSearcher = Lucene.Net.Search.IndexSearcher;
	using Query = Lucene.Net.Search.Query;
	using TermQuery = Lucene.Net.Search.TermQuery;
	using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
	using TestUtil = Lucene.Net.Util.TestUtil;
	using TestUtil = Lucene.Net.Util.TestUtil;

	public class TestLockFactory : LuceneTestCase
	{

		// Verify: we can provide our own LockFactory implementation, the right
		// methods are called at the right time, locks are created, etc.

		public virtual void TestCustomLockFactory()
		{
			Directory dir = new MockDirectoryWrapper(random(), new RAMDirectory());
			MockLockFactory lf = new MockLockFactory(this);
			dir.LockFactory = lf;

			// Lock prefix should have been set:
			Assert.IsTrue("lock prefix was not set by the RAMDirectory", lf.LockPrefixSet);

			IndexWriter writer = new IndexWriter(dir, new IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())));

			// add 100 documents (so that commit lock is used)
			for (int i = 0; i < 100; i++)
			{
				AddDoc(writer);
			}

			// Both write lock and commit lock should have been created:
			Assert.AreEqual("# of unique locks created (after instantiating IndexWriter)", 1, lf.LocksCreated.Count);
			Assert.IsTrue("# calls to makeLock is 0 (after instantiating IndexWriter)", lf.MakeLockCount >= 1);

			foreach (String lockName in lf.LocksCreated.Keys)
			{
				MockLockFactory.MockLock @lock = (MockLockFactory.MockLock) lf.LocksCreated[lockName];
				Assert.IsTrue("# calls to Lock.obtain is 0 (after instantiating IndexWriter)", @lock.LockAttempts > 0);
			}

			writer.close();
		}

		// Verify: we can use the NoLockFactory with RAMDirectory w/ no
		// exceptions raised:
		// Verify: NoLockFactory allows two IndexWriters
		public virtual void TestRAMDirectoryNoLocking()
		{
			MockDirectoryWrapper dir = new MockDirectoryWrapper(random(), new RAMDirectory());
			dir.LockFactory = NoLockFactory.NoLockFactory;
			dir.WrapLockFactory = false; // we are gonna explicitly test we get this back
			Assert.IsTrue("RAMDirectory.setLockFactory did not take", typeof(NoLockFactory).isInstance(dir.LockFactory));

			IndexWriter writer = new IndexWriter(dir, new IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())));
			writer.commit(); // required so the second open succeed
			// Create a 2nd IndexWriter.  this is normally not allowed but it should run through since we're not
			// using any locks:
			IndexWriter writer2 = null;
			try
			{
				writer2 = new IndexWriter(dir, (new IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random()))).setOpenMode(IndexWriterConfig.OpenMode_e.APPEND));
			}
			catch (Exception e)
			{
				e.printStackTrace(System.out);
				Assert.Fail("Should not have hit an IOException with no locking");
			}

			writer.close();
			if (writer2 != null)
			{
				writer2.close();
			}
		}

		// Verify: SingleInstanceLockFactory is the default lock for RAMDirectory
		// Verify: RAMDirectory does basic locking correctly (can't create two IndexWriters)
		public virtual void TestDefaultRAMDirectory()
		{
			Directory dir = new RAMDirectory();

			Assert.IsTrue("RAMDirectory did not use correct LockFactory: got " + dir.LockFactory, typeof(SingleInstanceLockFactory).isInstance(dir.LockFactory));

			IndexWriter writer = new IndexWriter(dir, new IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())));

			// Create a 2nd IndexWriter.  this should fail:
			IndexWriter writer2 = null;
			try
			{
				writer2 = new IndexWriter(dir, (new IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random()))).setOpenMode(IndexWriterConfig.OpenMode_e.APPEND));
				Assert.Fail("Should have hit an IOException with two IndexWriters on default SingleInstanceLockFactory");
			}
			catch (IOException e)
			{
			}

			writer.close();
			if (writer2 != null)
			{
				writer2.close();
			}
		}

		public virtual void TestSimpleFSLockFactory()
		{
		  // test string file instantiation
		  new SimpleFSLockFactory("test");
		}

		// Verify: do stress test, by opening IndexReaders and
		// IndexWriters over & over in 2 threads and making sure
		// no unexpected exceptions are raised:
//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Nightly public void testStressLocks() throws Exception
		public virtual void TestStressLocks()
		{
		  _testStressLocks(null, createTempDir("index.TestLockFactory6"));
		}

		// Verify: do stress test, by opening IndexReaders and
		// IndexWriters over & over in 2 threads and making sure
		// no unexpected exceptions are raised, but use
		// NativeFSLockFactory:
//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Nightly public void testStressLocksNativeFSLockFactory() throws Exception
		public virtual void TestStressLocksNativeFSLockFactory()
		{
		  File dir = createTempDir("index.TestLockFactory7");
		  _testStressLocks(new NativeFSLockFactory(dir), dir);
		}

		public virtual void _testStressLocks(LockFactory lockFactory, File indexDir)
		{
			Directory dir = newFSDirectory(indexDir, lockFactory);

			// First create a 1 doc index:
			IndexWriter w = new IndexWriter(dir, (new IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random()))).setOpenMode(IndexWriterConfig.OpenMode_e.CREATE));
			AddDoc(w);
			w.close();

			WriterThread writer = new WriterThread(this, 100, dir);
			SearcherThread searcher = new SearcherThread(this, 100, dir);
			writer.Start();
			searcher.Start();

			while (writer.IsAlive || searcher.IsAlive)
			{
			  Thread.Sleep(1000);
			}

			Assert.IsTrue("IndexWriter hit unexpected exceptions", !writer.HitException);
			Assert.IsTrue("IndexSearcher hit unexpected exceptions", !searcher.HitException);

			dir.close();
			// Cleanup
			TestUtil.rm(indexDir);
		}

		// Verify: NativeFSLockFactory works correctly
		public virtual void TestNativeFSLockFactory()
		{
		  NativeFSLockFactory f = new NativeFSLockFactory(createTempDir(LuceneTestCase.TestClass.SimpleName));

		  f.LockPrefix = "test";
		  Lock l = f.makeLock("commit");
		  Lock l2 = f.makeLock("commit");

		  Assert.IsTrue("failed to obtain lock", l.obtain());
		  Assert.IsTrue("succeeded in obtaining lock twice", !l2.obtain());
		  l.close();

		  Assert.IsTrue("failed to obtain 2nd lock after first one was freed", l2.obtain());
		  l2.close();

		  // Make sure we can obtain first one again, test isLocked():
		  Assert.IsTrue("failed to obtain lock", l.obtain());
		  Assert.IsTrue(l.Locked);
		  Assert.IsTrue(l2.Locked);
		  l.close();
		  Assert.IsFalse(l.Locked);
		  Assert.IsFalse(l2.Locked);
		}


		// Verify: NativeFSLockFactory works correctly if the lock file exists
		public virtual void TestNativeFSLockFactoryLockExists()
		{
		  File tempDir = createTempDir(LuceneTestCase.TestClass.SimpleName);
		  File lockFile = new File(tempDir, "test.lock");
		  lockFile.createNewFile();

		  Lock l = (new NativeFSLockFactory(tempDir)).makeLock("test.lock");
		  Assert.IsTrue("failed to obtain lock", l.obtain());
		  l.close();
		  Assert.IsFalse("failed to release lock", l.Locked);
		  if (lockFile.exists())
		  {
			lockFile.delete();
		  }
		}

		// Verify: NativeFSLockFactory assigns null as lockPrefix if the lockDir is inside directory
		public virtual void TestNativeFSLockFactoryPrefix()
		{

		  File fdir1 = createTempDir("TestLockFactory.8");
		  File fdir2 = createTempDir("TestLockFactory.8.Lockdir");
		  Directory dir1 = newFSDirectory(fdir1, new NativeFSLockFactory(fdir1));
		  // same directory, but locks are stored somewhere else. The prefix of the lock factory should != null
		  Directory dir2 = newFSDirectory(fdir1, new NativeFSLockFactory(fdir2));

		  string prefix1 = dir1.LockFactory.LockPrefix;
		  assertNull("Lock prefix for lockDir same as directory should be null", prefix1);

		  string prefix2 = dir2.LockFactory.LockPrefix;
		  Assert.IsNotNull("Lock prefix for lockDir outside of directory should be not null", prefix2);

		  dir1.close();
		  dir2.close();
		  TestUtil.rm(fdir1);
		  TestUtil.rm(fdir2);
		}

		// Verify: default LockFactory has no prefix (ie
		// write.lock is stored in index):
		public virtual void TestDefaultFSLockFactoryPrefix()
		{

		  // Make sure we get null prefix, which wont happen if setLockFactory is ever called.
		  File dirName = createTempDir("TestLockFactory.10");

		  Directory dir = new SimpleFSDirectory(dirName);
		  assertNull("Default lock prefix should be null", dir.LockFactory.LockPrefix);
		  dir.close();

		  dir = new MMapDirectory(dirName);
		  assertNull("Default lock prefix should be null", dir.LockFactory.LockPrefix);
		  dir.close();

		  dir = new NIOFSDirectory(dirName);
		  assertNull("Default lock prefix should be null", dir.LockFactory.LockPrefix);
		  dir.close();

		  TestUtil.rm(dirName);
		}

		private class WriterThread : System.Threading.Thread
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
				for (int i = 0;i < this.NumIteration;i++)
				{
					try
					{
						writer = new IndexWriter(Dir, (new IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random()))).setOpenMode(IndexWriterConfig.OpenMode_e.APPEND));
					}
					catch (IOException e)
					{
						if (e.ToString().IndexOf(" timed out:") == -1)
						{
							HitException = true;
							Console.WriteLine("Stress Test Index Writer: creation hit unexpected IOException: " + e.ToString());
							e.printStackTrace(System.out);
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
						e.printStackTrace(System.out);
						break;
					}
					if (writer != null)
					{
						try
						{
							outerInstance.AddDoc(writer);
						}
						catch (IOException e)
						{
							HitException = true;
							Console.WriteLine("Stress Test Index Writer: addDoc hit unexpected exception: " + e.ToString());
							e.printStackTrace(System.out);
							break;
						}
						try
						{
							writer.close();
						}
						catch (IOException e)
						{
							HitException = true;
							Console.WriteLine("Stress Test Index Writer: close hit unexpected exception: " + e.ToString());
							e.printStackTrace(System.out);
							break;
						}
						writer = null;
					}
				}
			}
		}

		private class SearcherThread : System.Threading.Thread
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
				for (int i = 0;i < this.NumIteration;i++)
				{
					try
					{
						reader = DirectoryReader.open(Dir);
						searcher = newSearcher(reader);
					}
					catch (Exception e)
					{
						HitException = true;
						Console.WriteLine("Stress Test Index Searcher: create hit unexpected exception: " + e.ToString());
						e.printStackTrace(System.out);
						break;
					}
					try
					{
					  searcher.search(query, null, 1000);
					}
					catch (IOException e)
					{
					  HitException = true;
					  Console.WriteLine("Stress Test Index Searcher: search hit unexpected exception: " + e.ToString());
					  e.printStackTrace(System.out);
					  break;
					}
					// System.out.println(hits.length() + " total results");
					try
					{
					  reader.close();
					}
					catch (IOException e)
					{
					  HitException = true;
					  Console.WriteLine("Stress Test Index Searcher: close hit unexpected exception: " + e.ToString());
					  e.printStackTrace(System.out);
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
			public IDictionary<string, Lock> LocksCreated = Collections.synchronizedMap(new Dictionary<string, Lock>());
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
			doc.add(newTextField("content", "aaa", Field.Store.NO));
			writer.addDocument(doc);
		}
	}

}