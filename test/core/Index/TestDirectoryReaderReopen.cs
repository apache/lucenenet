using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

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
	using FieldType = Lucene.Net.Document.FieldType;
	using TextField = Lucene.Net.Document.TextField;
	using OpenMode = Lucene.Net.Index.IndexWriterConfig.OpenMode_e;
	using IndexSearcher = Lucene.Net.Search.IndexSearcher;
	using ScoreDoc = Lucene.Net.Search.ScoreDoc;
	using TermQuery = Lucene.Net.Search.TermQuery;
	using Directory = Lucene.Net.Store.Directory;
	using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
	using TestUtil = Lucene.Net.Util.TestUtil;

	public class TestDirectoryReaderReopen : LuceneTestCase
	{

	  public virtual void TestReopen()
	  {
		Directory dir1 = newDirectory();

		CreateIndex(random(), dir1, false);
		PerformDefaultTests(new TestReopenAnonymousInnerClassHelper(this, dir1));
		dir1.close();

		Directory dir2 = newDirectory();

		CreateIndex(random(), dir2, true);
		PerformDefaultTests(new TestReopenAnonymousInnerClassHelper2(this, dir2));
		dir2.close();
	  }

	  private class TestReopenAnonymousInnerClassHelper : TestReopen
	  {
		  private readonly TestDirectoryReaderReopen OuterInstance;

		  private Directory Dir1;

		  public TestReopenAnonymousInnerClassHelper(TestDirectoryReaderReopen outerInstance, Directory dir1)
		  {
			  this.OuterInstance = outerInstance;
			  this.Dir1 = dir1;
		  }


		  protected internal override void ModifyIndex(int i)
		  {
			TestDirectoryReaderReopen.ModifyIndex(i, Dir1);
		  }

		  protected internal override DirectoryReader OpenReader()
		  {
			return DirectoryReader.open(Dir1);
		  }

	  }

	  private class TestReopenAnonymousInnerClassHelper2 : TestReopen
	  {
		  private readonly TestDirectoryReaderReopen OuterInstance;

		  private Directory Dir2;

		  public TestReopenAnonymousInnerClassHelper2(TestDirectoryReaderReopen outerInstance, Directory dir2)
		  {
			  this.OuterInstance = outerInstance;
			  this.Dir2 = dir2;
		  }


		  protected internal override void ModifyIndex(int i)
		  {
			TestDirectoryReaderReopen.ModifyIndex(i, Dir2);
		  }

		  protected internal override DirectoryReader OpenReader()
		  {
			return DirectoryReader.open(Dir2);
		  }

	  }

	  // LUCENE-1228: IndexWriter.commit() does not update the index version
	  // populate an index in iterations.
	  // at the end of every iteration, commit the index and reopen/recreate the reader.
	  // in each iteration verify the work of previous iteration. 
	  // try this once with reopen once recreate, on both RAMDir and FSDir.
	  public virtual void TestCommitReopen()
	  {
		Directory dir = newDirectory();
		DoTestReopenWithCommit(random(), dir, true);
		dir.close();
	  }
	  public virtual void TestCommitRecreate()
	  {
		Directory dir = newDirectory();
		DoTestReopenWithCommit(random(), dir, false);
		dir.close();
	  }

	  private void DoTestReopenWithCommit(Random random, Directory dir, bool withReopen)
	  {
		IndexWriter iwriter = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random)).setOpenMode(OpenMode.CREATE).setMergeScheduler(new SerialMergeScheduler()).setMergePolicy(newLogMergePolicy()));
		iwriter.commit();
		DirectoryReader reader = DirectoryReader.open(dir);
		try
		{
		  int M = 3;
		  FieldType customType = new FieldType(TextField.TYPE_STORED);
		  customType.Tokenized = false;
		  FieldType customType2 = new FieldType(TextField.TYPE_STORED);
		  customType2.Tokenized = false;
		  customType2.OmitNorms = true;
		  FieldType customType3 = new FieldType();
		  customType3.Stored = true;
		  for (int i = 0; i < 4; i++)
		  {
			for (int j = 0; j < M; j++)
			{
			  Document doc = new Document();
			  doc.add(newField("id", i + "_" + j, customType));
			  doc.add(newField("id2", i + "_" + j, customType2));
			  doc.add(newField("id3", i + "_" + j, customType3));
			  iwriter.addDocument(doc);
			  if (i > 0)
			  {
				int k = i - 1;
				int n = j + k * M;
				Document prevItereationDoc = reader.document(n);
				Assert.IsNotNull(prevItereationDoc);
				string id = prevItereationDoc.get("id");
				Assert.AreEqual(k + "_" + j, id);
			  }
			}
			iwriter.commit();
			if (withReopen)
			{
			  // reopen
			  DirectoryReader r2 = DirectoryReader.openIfChanged(reader);
			  if (r2 != null)
			  {
				reader.close();
				reader = r2;
			  }
			}
			else
			{
			  // recreate
			  reader.close();
			  reader = DirectoryReader.open(dir);
			}
		  }
		}
		finally
		{
		  iwriter.close();
		  reader.close();
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
		Assert.IsTrue(couple.RefreshedReader == index2);

		couple = RefreshReader(index2, test, 0, true);
		index1.close();
		index1 = couple.NewReader;

		DirectoryReader index2_refreshed = couple.RefreshedReader;
		index2.close();

		// test if refreshed reader and newly opened reader return equal results
		TestDirectoryReader.AssertIndexEquals(index1, index2_refreshed);

		index2_refreshed.close();
		AssertReaderClosed(index2, true);
		AssertReaderClosed(index2_refreshed, true);

		index2 = test.OpenReader();

		for (int i = 1; i < 4; i++)
		{

		  index1.close();
		  couple = RefreshReader(index2, test, i, true);
		  // refresh DirectoryReader
		  index2.close();

		  index2 = couple.RefreshedReader;
		  index1 = couple.NewReader;
		  TestDirectoryReader.AssertIndexEquals(index1, index2);
		}

		index1.close();
		index2.close();
		AssertReaderClosed(index1, true);
		AssertReaderClosed(index2, true);
	  }

	  public virtual void TestThreadSafety()
	  {
		Directory dir = newDirectory();
		// NOTE: this also controls the number of threads!
		int n = TestUtil.Next(random(), 20, 40);
		IndexWriter writer = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())));
		for (int i = 0; i < n; i++)
		{
		  writer.addDocument(CreateDocument(i, 3));
		}
		writer.forceMerge(1);
		writer.close();

		TestReopen test = new TestReopenAnonymousInnerClassHelper3(this, dir, n);

		IList<ReaderCouple> readers = Collections.synchronizedList(new List<ReaderCouple>());
		DirectoryReader firstReader = DirectoryReader.open(dir);
		DirectoryReader reader = firstReader;

		ReaderThread[] threads = new ReaderThread[n];
		Set<DirectoryReader> readersToClose = Collections.synchronizedSet(new HashSet<DirectoryReader>());

		for (int i = 0; i < n; i++)
		{
		  if (i % 2 == 0)
		  {
			DirectoryReader refreshed = DirectoryReader.openIfChanged(reader);
			if (refreshed != null)
			{
			  readersToClose.add(reader);
			  reader = refreshed;
			}
		  }
		  DirectoryReader r = reader;

		  int index = i;

		  ReaderThreadTask task;

		  if (i < 4 || (i >= 10 && i < 14) || i > 18)
		  {
			task = new ReaderThreadTaskAnonymousInnerClassHelper(this, test, readers, readersToClose, r, index);
		  }
		  else
		  {
			task = new ReaderThreadTaskAnonymousInnerClassHelper2(this, readers);
		  }

		  threads[i] = new ReaderThread(task);
		  threads[i].Start();
		}

		lock (this)
		{
		  Monitor.Wait(this, TimeSpan.FromMilliseconds(1000));
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
			if (threads[i].Error != null)
			{
			  string msg = "Error occurred in thread " + threads[i].Name + ":\n" + threads[i].Error.Message;
			  Assert.Fail(msg);
			}
		  }

		}

		foreach (DirectoryReader readerToClose in readersToClose)
		{
		  readerToClose.close();
		}

		firstReader.close();
		reader.close();

		foreach (DirectoryReader readerToClose in readersToClose)
		{
		  AssertReaderClosed(readerToClose, true);
		}

		AssertReaderClosed(reader, true);
		AssertReaderClosed(firstReader, true);

		dir.close();
	  }

	  private class TestReopenAnonymousInnerClassHelper3 : TestReopen
	  {
		  private readonly TestDirectoryReaderReopen OuterInstance;

		  private Directory Dir;
		  private int n;

		  public TestReopenAnonymousInnerClassHelper3(TestDirectoryReaderReopen outerInstance, Directory dir, int n)
		  {
			  this.OuterInstance = outerInstance;
			  this.Dir = dir;
			  this.n = n;
		  }

		  protected internal override void ModifyIndex(int i)
		  {
		   IndexWriter modifier = new IndexWriter(Dir, new IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())));
		   modifier.addDocument(CreateDocument(n + i, 6));
		   modifier.close();
		  }

		  protected internal override DirectoryReader OpenReader()
		  {
			return DirectoryReader.open(Dir);
		  }
	  }

	  private class ReaderThreadTaskAnonymousInnerClassHelper : ReaderThreadTask
	  {
		  private readonly TestDirectoryReaderReopen OuterInstance;

		  private Lucene.Net.Index.TestDirectoryReaderReopen.TestReopen Test;
		  private IList<ReaderCouple> Readers;
		  private Set<DirectoryReader> ReadersToClose;
		  private DirectoryReader r;
		  private int Index;

		  public ReaderThreadTaskAnonymousInnerClassHelper(TestDirectoryReaderReopen outerInstance, Lucene.Net.Index.TestDirectoryReaderReopen.TestReopen test, IList<ReaderCouple> readers, Set<DirectoryReader> readersToClose, DirectoryReader r, int index)
		  {
			  this.OuterInstance = outerInstance;
			  this.Test = test;
			  this.Readers = readers;
			  this.ReadersToClose = readersToClose;
			  this.r = r;
			  this.Index = index;
		  }


		  public override void Run()
		  {
			Random rnd = LuceneTestCase.random();
			while (!stopped)
			{
			  if (Index % 2 == 0)
			  {
				// refresh reader synchronized
				ReaderCouple c = (outerInstance.RefreshReader(r, Test, Index, true));
				ReadersToClose.add(c.NewReader);
				ReadersToClose.add(c.RefreshedReader);
				Readers.Add(c);
				// prevent too many readers
				break;
			  }
			  else
			  {
				// not synchronized
				DirectoryReader refreshed = DirectoryReader.openIfChanged(r);
				if (refreshed == null)
				{
				  refreshed = r;
				}

				IndexSearcher searcher = newSearcher(refreshed);
				ScoreDoc[] hits = searcher.search(new TermQuery(new Term("field1", "a" + rnd.Next(refreshed.maxDoc()))), null, 1000).scoreDocs;
				if (hits.Length > 0)
				{
				  searcher.doc(hits[0].doc);
				}
				if (refreshed != r)
				{
				  refreshed.close();
				}
			  }
			  lock (this)
			  {
				Monitor.Wait(this, TimeSpan.FromMilliseconds(TestUtil.Next(random(), 1, 100)));
			  }
			}
		  }

	  }

	  private class ReaderThreadTaskAnonymousInnerClassHelper2 : ReaderThreadTask
	  {
		  private readonly TestDirectoryReaderReopen OuterInstance;

		  private IList<ReaderCouple> Readers;

		  public ReaderThreadTaskAnonymousInnerClassHelper2(TestDirectoryReaderReopen outerInstance, IList<ReaderCouple> readers)
		  {
			  this.OuterInstance = outerInstance;
			  this.Readers = readers;
		  }

		  public override void Run()
		  {
			Random rnd = LuceneTestCase.random();
			while (!stopped)
			{
			  int numReaders = Readers.Count;
			  if (numReaders > 0)
			  {
				ReaderCouple c = Readers[rnd.Next(numReaders)];
				TestDirectoryReader.AssertIndexEquals(c.NewReader, c.RefreshedReader);
			  }

			  lock (this)
			  {
				Monitor.Wait(this, TimeSpan.FromMilliseconds(TestUtil.Next(random(), 1, 100)));
			  }
			}
		  }
	  }

	  private class ReaderCouple
	  {
		internal ReaderCouple(DirectoryReader r1, DirectoryReader r2)
		{
		  NewReader = r1;
		  RefreshedReader = r2;
		}

		internal DirectoryReader NewReader;
		internal DirectoryReader RefreshedReader;
	  }

	  internal abstract class ReaderThreadTask
	  {
		protected internal volatile bool Stopped;
		public virtual void Stop()
		{
		  this.Stopped = true;
		}

		public abstract void Run();
	  }

	  private class ReaderThread : System.Threading.Thread
	  {
		internal ReaderThreadTask Task;
		internal Exception Error;


		internal ReaderThread(ReaderThreadTask task)
		{
		  this.Task = task;
		}

		public virtual void StopThread()
		{
		  this.Task.Stop();
		}

		public override void Run()
		{
		  try
		  {
			this.Task.Run();
		  }
		  catch (Exception r)
		  {
			r.printStackTrace(System.out);
			this.Error = r;
		  }
		}
	  }

	  private object CreateReaderMutex = new object();

	  private ReaderCouple RefreshReader(DirectoryReader reader, bool hasChanges)
	  {
		return RefreshReader(reader, null, -1, hasChanges);
	  }

	  internal virtual ReaderCouple RefreshReader(DirectoryReader reader, TestReopen test, int modify, bool hasChanges)
	  {
		lock (CreateReaderMutex)
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
			refreshed = DirectoryReader.openIfChanged(reader);
			if (refreshed == null)
			{
			  refreshed = reader;
			}
		  }
		  finally
		  {
			if (refreshed == null && r != null)
			{
			  // Hit exception -- close opened reader
			  r.close();
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
	  }

	  public static void CreateIndex(Random random, Directory dir, bool multiSegment)
	  {
		IndexWriter.unlock(dir);
		IndexWriter w = new IndexWriter(dir, LuceneTestCase.newIndexWriterConfig(random, TEST_VERSION_CURRENT, new MockAnalyzer(random)).setMergePolicy(new LogDocMergePolicy()));

		for (int i = 0; i < 100; i++)
		{
		  w.addDocument(CreateDocument(i, 4));
		  if (multiSegment && (i % 10) == 0)
		  {
			w.commit();
		  }
		}

		if (!multiSegment)
		{
		  w.forceMerge(1);
		}

		w.close();

		DirectoryReader r = DirectoryReader.open(dir);
		if (multiSegment)
		{
		  Assert.IsTrue(r.leaves().size() > 1);
		}
		else
		{
		  Assert.IsTrue(r.leaves().size() == 1);
		}
		r.close();
	  }

	  public static Document CreateDocument(int n, int numFields)
	  {
		StringBuilder sb = new StringBuilder();
		Document doc = new Document();
		sb.Append("a");
		sb.Append(n);
		FieldType customType2 = new FieldType(TextField.TYPE_STORED);
		customType2.Tokenized = false;
		customType2.OmitNorms = true;
		FieldType customType3 = new FieldType();
		customType3.Stored = true;
		doc.add(new TextField("field1", sb.ToString(), Field.Store.YES));
		doc.add(new Field("fielda", sb.ToString(), customType2));
		doc.add(new Field("fieldb", sb.ToString(), customType3));
		sb.Append(" b");
		sb.Append(n);
		for (int i = 1; i < numFields; i++)
		{
		  doc.add(new TextField("field" + (i + 1), sb.ToString(), Field.Store.YES));
		}
		return doc;
	  }

	  internal static void ModifyIndex(int i, Directory dir)
	  {
		switch (i)
		{
		  case 0:
		  {
			if (VERBOSE)
			{
			  Console.WriteLine("TEST: modify index");
			}
			IndexWriter w = new IndexWriter(dir, new IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())));
			w.deleteDocuments(new Term("field2", "a11"));
			w.deleteDocuments(new Term("field2", "b30"));
			w.close();
			break;
		  }
		  case 1:
		  {
			IndexWriter w = new IndexWriter(dir, new IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())));
			w.forceMerge(1);
			w.close();
			break;
		  }
		  case 2:
		  {
			IndexWriter w = new IndexWriter(dir, new IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())));
			w.addDocument(CreateDocument(101, 4));
			w.forceMerge(1);
			w.addDocument(CreateDocument(102, 4));
			w.addDocument(CreateDocument(103, 4));
			w.close();
			break;
		  }
		  case 3:
		  {
			IndexWriter w = new IndexWriter(dir, new IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())));
			w.addDocument(CreateDocument(101, 4));
			w.close();
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
//JAVA TO C# CONVERTER TODO TASK: Java wildcard generics are not converted to .NET:
//ORIGINAL LINE: java.util.List<? extends IndexReader> subReaders = ((CompositeReader) reader).getSequentialSubReaders();
		  IList<?> subReaders = ((CompositeReader) reader).SequentialSubReaders;
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
		public override void onInit<T1>(IList<T1> commits) where T1 : IndexCommit
		{
		}
		public override void OnCommit<T1>(IList<T1> commits) where T1 : IndexCommit
		{
		}
	  }

	  public virtual void TestReopenOnCommit()
	  {
		Directory dir = newDirectory();
		IndexWriter writer = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setIndexDeletionPolicy(new KeepAllCommits()).setMaxBufferedDocs(-1).setMergePolicy(newLogMergePolicy(10)));
		for (int i = 0;i < 4;i++)
		{
		  Document doc = new Document();
		  doc.add(newStringField("id", "" + i, Field.Store.NO));
		  writer.addDocument(doc);
		  IDictionary<string, string> data = new Dictionary<string, string>();
		  data["index"] = i + "";
		  writer.CommitData = data;
		  writer.commit();
		}
		for (int i = 0;i < 4;i++)
		{
		  writer.deleteDocuments(new Term("id", "" + i));
		  IDictionary<string, string> data = new Dictionary<string, string>();
		  data["index"] = (4 + i) + "";
		  writer.CommitData = data;
		  writer.commit();
		}
		writer.close();

		DirectoryReader r = DirectoryReader.open(dir);
		Assert.AreEqual(0, r.numDocs());

		ICollection<IndexCommit> commits = DirectoryReader.listCommits(dir);
		foreach (IndexCommit commit in commits)
		{
		  DirectoryReader r2 = DirectoryReader.openIfChanged(r, commit);
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
			Assert.AreEqual(1 + v, r2.numDocs());
		  }
		  else
		  {
			Assert.AreEqual(7 - v, r2.numDocs());
		  }
		  r.close();
		  r = r2;
		}
		r.close();
		dir.close();
	  }

	  public virtual void TestOpenIfChangedNRTToCommit()
	  {
		Directory dir = newDirectory();

		// Can't use RIW because it randomly commits:
		IndexWriter w = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())));
		Document doc = new Document();
		doc.add(newStringField("field", "value", Field.Store.NO));
		w.addDocument(doc);
		w.commit();
		IList<IndexCommit> commits = DirectoryReader.listCommits(dir);
		Assert.AreEqual(1, commits.Count);
		w.addDocument(doc);
		DirectoryReader r = DirectoryReader.open(w, true);

		Assert.AreEqual(2, r.numDocs());
		IndexReader r2 = DirectoryReader.openIfChanged(r, commits[0]);
		Assert.IsNotNull(r2);
		r.close();
		Assert.AreEqual(1, r2.numDocs());
		w.close();
		r2.close();
		dir.close();
	  }
	}

}