using System;

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


	using MockAnalyzer = Lucene.Net.Analysis.MockAnalyzer;
	using Document = Lucene.Net.Document.Document;
	using Field = Lucene.Net.Document.Field;
	using Directory = Lucene.Net.Store.Directory;
	using MockDirectoryWrapper = Lucene.Net.Store.MockDirectoryWrapper;
	using NoLockFactory = Lucene.Net.Store.NoLockFactory;
	using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;

	public class TestCrash : LuceneTestCase
	{

	  private IndexWriter InitIndex(Random random, bool initialCommit)
	  {
		return InitIndex(random, newMockDirectory(random), initialCommit);
	  }

	  private IndexWriter InitIndex(Random random, MockDirectoryWrapper dir, bool initialCommit)
	  {
		dir.LockFactory = NoLockFactory.NoLockFactory;

		IndexWriter writer = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random)).setMaxBufferedDocs(10).setMergeScheduler(new ConcurrentMergeScheduler()));
		((ConcurrentMergeScheduler) writer.Config.MergeScheduler).setSuppressExceptions();
		if (initialCommit)
		{
		  writer.commit();
		}

		Document doc = new Document();
		doc.add(newTextField("content", "aaa", Field.Store.NO));
		doc.add(newTextField("id", "0", Field.Store.NO));
		for (int i = 0;i < 157;i++)
		{
		  writer.addDocument(doc);
		}

		return writer;
	  }

	  private void Crash(IndexWriter writer)
	  {
		MockDirectoryWrapper dir = (MockDirectoryWrapper) writer.Directory;
		ConcurrentMergeScheduler cms = (ConcurrentMergeScheduler) writer.Config.MergeScheduler;
		cms.sync();
		dir.crash();
		cms.sync();
		dir.clearCrash();
	  }

	  public virtual void TestCrashWhileIndexing()
	  {
		// this test relies on being able to open a reader before any commit
		// happened, so we must create an initial commit just to allow that, but
		// before any documents were added.
		IndexWriter writer = InitIndex(random(), true);
		MockDirectoryWrapper dir = (MockDirectoryWrapper) writer.Directory;

		// We create leftover files because merging could be
		// running when we crash:
		dir.AssertNoUnrefencedFilesOnClose = false;

		Crash(writer);

		IndexReader reader = DirectoryReader.open(dir);
		Assert.IsTrue(reader.numDocs() < 157);
		reader.close();

		// Make a new dir, copying from the crashed dir, and
		// open IW on it, to confirm IW "recovers" after a
		// crash:
		Directory dir2 = newDirectory(dir);
		dir.close();

		(new RandomIndexWriter(random(), dir2)).close();
		dir2.close();
	  }

	  public virtual void TestWriterAfterCrash()
	  {
		// this test relies on being able to open a reader before any commit
		// happened, so we must create an initial commit just to allow that, but
		// before any documents were added.
		Console.WriteLine("TEST: initIndex");
		IndexWriter writer = InitIndex(random(), true);
		Console.WriteLine("TEST: done initIndex");
		MockDirectoryWrapper dir = (MockDirectoryWrapper) writer.Directory;

		// We create leftover files because merging could be
		// running / store files could be open when we crash:
		dir.AssertNoUnrefencedFilesOnClose = false;

		dir.PreventDoubleWrite = false;
		Console.WriteLine("TEST: now crash");
		Crash(writer);
		writer = InitIndex(random(), dir, false);
		writer.close();

		IndexReader reader = DirectoryReader.open(dir);
		Assert.IsTrue(reader.numDocs() < 314);
		reader.close();

		// Make a new dir, copying from the crashed dir, and
		// open IW on it, to confirm IW "recovers" after a
		// crash:
		Directory dir2 = newDirectory(dir);
		dir.close();

		(new RandomIndexWriter(random(), dir2)).close();
		dir2.close();
	  }

	  public virtual void TestCrashAfterReopen()
	  {
		IndexWriter writer = InitIndex(random(), false);
		MockDirectoryWrapper dir = (MockDirectoryWrapper) writer.Directory;

		// We create leftover files because merging could be
		// running when we crash:
		dir.AssertNoUnrefencedFilesOnClose = false;

		writer.close();
		writer = InitIndex(random(), dir, false);
		Assert.AreEqual(314, writer.maxDoc());
		Crash(writer);

		/*
		System.out.println("\n\nTEST: open reader");
		String[] l = dir.list();
		Arrays.sort(l);
		for(int i=0;i<l.length;i++)
		  System.out.println("file " + i + " = " + l[i] + " " +
		dir.fileLength(l[i]) + " bytes");
		*/

		IndexReader reader = DirectoryReader.open(dir);
		Assert.IsTrue(reader.numDocs() >= 157);
		reader.close();

		// Make a new dir, copying from the crashed dir, and
		// open IW on it, to confirm IW "recovers" after a
		// crash:
		Directory dir2 = newDirectory(dir);
		dir.close();

		(new RandomIndexWriter(random(), dir2)).close();
		dir2.close();
	  }

	  public virtual void TestCrashAfterClose()
	  {

		IndexWriter writer = InitIndex(random(), false);
		MockDirectoryWrapper dir = (MockDirectoryWrapper) writer.Directory;

		writer.close();
		dir.crash();

		/*
		String[] l = dir.list();
		Arrays.sort(l);
		for(int i=0;i<l.length;i++)
		  System.out.println("file " + i + " = " + l[i] + " " + dir.fileLength(l[i]) + " bytes");
		*/

		IndexReader reader = DirectoryReader.open(dir);
		Assert.AreEqual(157, reader.numDocs());
		reader.close();
		dir.close();
	  }

	  public virtual void TestCrashAfterCloseNoWait()
	  {

		IndexWriter writer = InitIndex(random(), false);
		MockDirectoryWrapper dir = (MockDirectoryWrapper) writer.Directory;

		writer.close(false);

		dir.crash();

		/*
		String[] l = dir.list();
		Arrays.sort(l);
		for(int i=0;i<l.length;i++)
		  System.out.println("file " + i + " = " + l[i] + " " + dir.fileLength(l[i]) + " bytes");
		*/
		IndexReader reader = DirectoryReader.open(dir);
		Assert.AreEqual(157, reader.numDocs());
		reader.close();
		dir.close();
	  }
	}

}