using System;
using System.Collections.Generic;
using System.Threading;

namespace Lucene.Net.Index
{

	/// <summary>
	/// Copyright 2004 The Apache Software Foundation
	/// 
	/// Licensed under the Apache License, Version 2.0 (the "License");
	/// you may not use this file except in compliance with the License.
	/// You may obtain a copy of the License at
	/// 
	///     http://www.apache.org/licenses/LICENSE-2.0
	/// 
	/// Unless required by applicable law or agreed to in writing, software
	/// distributed under the License is distributed on an "AS IS" BASIS,
	/// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
	/// See the License for the specific language governing permissions and
	/// limitations under the License.
	/// </summary>


	using MockAnalyzer = Lucene.Net.Analysis.MockAnalyzer;
	using Codec = Lucene.Net.Codecs.Codec;
	using Document = Lucene.Net.Document.Document;
	using Field = Lucene.Net.Document.Field;
	using TextField = Lucene.Net.Document.TextField;
	using DocIdSetIterator = Lucene.Net.Search.DocIdSetIterator;
	using IndexSearcher = Lucene.Net.Search.IndexSearcher;
	using Query = Lucene.Net.Search.Query;
	using TermQuery = Lucene.Net.Search.TermQuery;
	using TopDocs = Lucene.Net.Search.TopDocs;
	using AlreadyClosedException = Lucene.Net.Store.AlreadyClosedException;
	using Directory = Lucene.Net.Store.Directory;
	using FakeIOException = Lucene.Net.Store.MockDirectoryWrapper.FakeIOException;
	using MockDirectoryWrapper = Lucene.Net.Store.MockDirectoryWrapper;
	using RAMDirectory = Lucene.Net.Store.RAMDirectory;
	using BytesRef = Lucene.Net.Util.BytesRef;
	using InfoStream = Lucene.Net.Util.InfoStream;
	using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
	using TestUtil = Lucene.Net.Util.TestUtil;
	using ThreadInterruptedException = Lucene.Net.Util.ThreadInterruptedException;
	using Test = org.junit.Test;

	public class TestIndexWriterReader : LuceneTestCase
	{

	  private readonly int NumThreads = TEST_NIGHTLY ? 5 : 3;

	  public static int Count(Term t, IndexReader r)
	  {
		int count = 0;
		DocsEnum td = TestUtil.docs(random(), r, t.field(), new BytesRef(t.text()), MultiFields.getLiveDocs(r), null, 0);

		if (td != null)
		{
		  while (td.nextDoc() != DocIdSetIterator.NO_MORE_DOCS)
		  {
			td.docID();
			count++;
		  }
		}
		return count;
	  }

	  public virtual void TestAddCloseOpen()
	  {
		// Can't use assertNoDeletes: this test pulls a non-NRT
		// reader in the end:
		Directory dir1 = newDirectory();
		IndexWriterConfig iwc = newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random()));

		IndexWriter writer = new IndexWriter(dir1, iwc);
		for (int i = 0; i < 97 ; i++)
		{
		  DirectoryReader reader = writer.Reader;
		  if (i == 0)
		  {
			writer.addDocument(DocHelper.createDocument(i, "x", 1 + random().Next(5)));
		  }
		  else
		  {
			int previous = random().Next(i);
			// a check if the reader is current here could fail since there might be
			// merges going on.
			switch (random().Next(5))
			{
			case 0:
			case 1:
			case 2:
			  writer.addDocument(DocHelper.createDocument(i, "x", 1 + random().Next(5)));
			  break;
			case 3:
			  writer.updateDocument(new Term("id", "" + previous), DocHelper.createDocument(previous, "x", 1 + random().Next(5)));
			  break;
			case 4:
			  writer.deleteDocuments(new Term("id", "" + previous));
		  break;
			}
		  }
		  Assert.IsFalse(reader.Current);
		  reader.close();
		}
		writer.forceMerge(1); // make sure all merging is done etc.
		DirectoryReader reader = writer.Reader;
		writer.commit(); // no changes that are not visible to the reader
		Assert.IsTrue(reader.Current);
		writer.close();
		Assert.IsTrue(reader.Current); // all changes are visible to the reader
		iwc = newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random()));
		writer = new IndexWriter(dir1, iwc);
		Assert.IsTrue(reader.Current);
		writer.addDocument(DocHelper.createDocument(1, "x", 1 + random().Next(5)));
		Assert.IsTrue(reader.Current); // segments in ram but IW is different to the readers one
		writer.close();
		Assert.IsFalse(reader.Current); // segments written
		reader.close();
		dir1.close();
	  }

	  public virtual void TestUpdateDocument()
	  {
		bool doFullMerge = true;

		Directory dir1 = newDirectory();
		IndexWriterConfig iwc = newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random()));
		if (iwc.MaxBufferedDocs < 20)
		{
		  iwc.MaxBufferedDocs = 20;
		}
		// no merging
		if (random().nextBoolean())
		{
		  iwc.MergePolicy = NoMergePolicy.NO_COMPOUND_FILES;
		}
		else
		{
		  iwc.MergePolicy = NoMergePolicy.COMPOUND_FILES;
		}
		if (VERBOSE)
		{
		  Console.WriteLine("TEST: make index");
		}
		IndexWriter writer = new IndexWriter(dir1, iwc);

		// create the index
		CreateIndexNoClose(!doFullMerge, "index1", writer);

		// writer.flush(false, true, true);

		// get a reader
		DirectoryReader r1 = writer.Reader;
		Assert.IsTrue(r1.Current);

		string id10 = r1.document(10).getField("id").stringValue();

		Document newDoc = r1.document(10);
		newDoc.removeField("id");
		newDoc.add(newStringField("id", Convert.ToString(8000), Field.Store.YES));
		writer.updateDocument(new Term("id", id10), newDoc);
		Assert.IsFalse(r1.Current);

		DirectoryReader r2 = writer.Reader;
		Assert.IsTrue(r2.Current);
		Assert.AreEqual(0, Count(new Term("id", id10), r2));
		if (VERBOSE)
		{
		  Console.WriteLine("TEST: verify id");
		}
		Assert.AreEqual(1, Count(new Term("id", Convert.ToString(8000)), r2));

		r1.close();
		Assert.IsTrue(r2.Current);
		writer.close();
		Assert.IsTrue(r2.Current);

		DirectoryReader r3 = DirectoryReader.open(dir1);
		Assert.IsTrue(r3.Current);
		Assert.IsTrue(r2.Current);
		Assert.AreEqual(0, Count(new Term("id", id10), r3));
		Assert.AreEqual(1, Count(new Term("id", Convert.ToString(8000)), r3));

		writer = new IndexWriter(dir1, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())));
		Document doc = new Document();
		doc.add(newTextField("field", "a b c", Field.Store.NO));
		writer.addDocument(doc);
		Assert.IsTrue(r2.Current);
		Assert.IsTrue(r3.Current);

		writer.close();

		Assert.IsFalse(r2.Current);
		Assert.IsTrue(!r3.Current);

		r2.close();
		r3.close();

		dir1.close();
	  }

	  public virtual void TestIsCurrent()
	  {
		Directory dir = newDirectory();
		IndexWriterConfig iwc = newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random()));

		IndexWriter writer = new IndexWriter(dir, iwc);
		Document doc = new Document();
		doc.add(newTextField("field", "a b c", Field.Store.NO));
		writer.addDocument(doc);
		writer.close();

		iwc = newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random()));
		writer = new IndexWriter(dir, iwc);
		doc = new Document();
		doc.add(newTextField("field", "a b c", Field.Store.NO));
		DirectoryReader nrtReader = writer.Reader;
		Assert.IsTrue(nrtReader.Current);
		writer.addDocument(doc);
		Assert.IsFalse(nrtReader.Current); // should see the changes
		writer.forceMerge(1); // make sure we don't have a merge going on
		Assert.IsFalse(nrtReader.Current);
		nrtReader.close();

		DirectoryReader dirReader = DirectoryReader.open(dir);
		nrtReader = writer.Reader;

		Assert.IsTrue(dirReader.Current);
		Assert.IsTrue(nrtReader.Current); // nothing was committed yet so we are still current
		Assert.AreEqual(2, nrtReader.maxDoc()); // sees the actual document added
		Assert.AreEqual(1, dirReader.maxDoc());
		writer.close(); // close is actually a commit both should see the changes
		Assert.IsTrue(nrtReader.Current);
		Assert.IsFalse(dirReader.Current); // this reader has been opened before the writer was closed / committed

		dirReader.close();
		nrtReader.close();
		dir.close();
	  }

	  /// <summary>
	  /// Test using IW.addIndexes
	  /// </summary>
	  public virtual void TestAddIndexes()
	  {
		bool doFullMerge = false;

		Directory dir1 = GetAssertNoDeletesDirectory(newDirectory());
		IndexWriterConfig iwc = newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random()));
		if (iwc.MaxBufferedDocs < 20)
		{
		  iwc.MaxBufferedDocs = 20;
		}
		// no merging
		if (random().nextBoolean())
		{
		  iwc.MergePolicy = NoMergePolicy.NO_COMPOUND_FILES;
		}
		else
		{
		  iwc.MergePolicy = NoMergePolicy.COMPOUND_FILES;
		}
		IndexWriter writer = new IndexWriter(dir1, iwc);

		// create the index
		CreateIndexNoClose(!doFullMerge, "index1", writer);
		writer.flush(false, true);

		// create a 2nd index
		Directory dir2 = newDirectory();
		IndexWriter writer2 = new IndexWriter(dir2, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())));
		CreateIndexNoClose(!doFullMerge, "index2", writer2);
		writer2.close();

		DirectoryReader r0 = writer.Reader;
		Assert.IsTrue(r0.Current);
		writer.addIndexes(dir2);
		Assert.IsFalse(r0.Current);
		r0.close();

		DirectoryReader r1 = writer.Reader;
		Assert.IsTrue(r1.Current);

		writer.commit();
		Assert.IsTrue(r1.Current); // we have seen all changes - no change after opening the NRT reader

		Assert.AreEqual(200, r1.maxDoc());

		int index2df = r1.docFreq(new Term("indexname", "index2"));

		Assert.AreEqual(100, index2df);

		// verify the docs are from different indexes
		Document doc5 = r1.document(5);
		Assert.AreEqual("index1", doc5.get("indexname"));
		Document doc150 = r1.document(150);
		Assert.AreEqual("index2", doc150.get("indexname"));
		r1.close();
		writer.close();
		dir1.close();
		dir2.close();
	  }

	  public virtual void TestAddIndexes2()
	  {
		bool doFullMerge = false;

		Directory dir1 = GetAssertNoDeletesDirectory(newDirectory());
		IndexWriter writer = new IndexWriter(dir1, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())));

		// create a 2nd index
		Directory dir2 = newDirectory();
		IndexWriter writer2 = new IndexWriter(dir2, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())));
		CreateIndexNoClose(!doFullMerge, "index2", writer2);
		writer2.close();

		writer.addIndexes(dir2);
		writer.addIndexes(dir2);
		writer.addIndexes(dir2);
		writer.addIndexes(dir2);
		writer.addIndexes(dir2);

		IndexReader r1 = writer.Reader;
		Assert.AreEqual(500, r1.maxDoc());

		r1.close();
		writer.close();
		dir1.close();
		dir2.close();
	  }

	  /// <summary>
	  /// Deletes using IW.deleteDocuments
	  /// </summary>
	  public virtual void TestDeleteFromIndexWriter()
	  {
		bool doFullMerge = true;

		Directory dir1 = GetAssertNoDeletesDirectory(newDirectory());
		IndexWriter writer = new IndexWriter(dir1, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setReaderTermsIndexDivisor(2));
		// create the index
		CreateIndexNoClose(!doFullMerge, "index1", writer);
		writer.flush(false, true);
		// get a reader
		IndexReader r1 = writer.Reader;

		string id10 = r1.document(10).getField("id").stringValue();

		// deleted IW docs should not show up in the next getReader
		writer.deleteDocuments(new Term("id", id10));
		IndexReader r2 = writer.Reader;
		Assert.AreEqual(1, Count(new Term("id", id10), r1));
		Assert.AreEqual(0, Count(new Term("id", id10), r2));

		string id50 = r1.document(50).getField("id").stringValue();
		Assert.AreEqual(1, Count(new Term("id", id50), r1));

		writer.deleteDocuments(new Term("id", id50));

		IndexReader r3 = writer.Reader;
		Assert.AreEqual(0, Count(new Term("id", id10), r3));
		Assert.AreEqual(0, Count(new Term("id", id50), r3));

		string id75 = r1.document(75).getField("id").stringValue();
		writer.deleteDocuments(new TermQuery(new Term("id", id75)));
		IndexReader r4 = writer.Reader;
		Assert.AreEqual(1, Count(new Term("id", id75), r3));
		Assert.AreEqual(0, Count(new Term("id", id75), r4));

		r1.close();
		r2.close();
		r3.close();
		r4.close();
		writer.close();

		// reopen the writer to verify the delete made it to the directory
		writer = new IndexWriter(dir1, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())));
		IndexReader w2r1 = writer.Reader;
		Assert.AreEqual(0, Count(new Term("id", id10), w2r1));
		w2r1.close();
		writer.close();
		dir1.close();
	  }

	  public virtual void TestAddIndexesAndDoDeletesThreads()
	  {
		const int numIter = 2;
		int numDirs = 3;

		Directory mainDir = GetAssertNoDeletesDirectory(newDirectory());

		IndexWriter mainWriter = new IndexWriter(mainDir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setMergePolicy(newLogMergePolicy()));
		TestUtil.reduceOpenFiles(mainWriter);

		AddDirectoriesThreads addDirThreads = new AddDirectoriesThreads(this, numIter, mainWriter);
		addDirThreads.LaunchThreads(numDirs);
		addDirThreads.JoinThreads();

		//Assert.AreEqual(100 + numDirs * (3 * numIter / 4) * addDirThreads.numThreads
		//    * addDirThreads.NUM_INIT_DOCS, addDirThreads.mainWriter.numDocs());
		Assert.AreEqual((int)addDirThreads.Count, addDirThreads.MainWriter.numDocs());

		addDirThreads.Close(true);

		Assert.IsTrue(addDirThreads.Failures.Count == 0);

		TestUtil.checkIndex(mainDir);

		IndexReader reader = DirectoryReader.open(mainDir);
		Assert.AreEqual((int)addDirThreads.Count, reader.numDocs());
		//Assert.AreEqual(100 + numDirs * (3 * numIter / 4) * addDirThreads.numThreads
		//    * addDirThreads.NUM_INIT_DOCS, reader.numDocs());
		reader.close();

		addDirThreads.CloseDir();
		mainDir.close();
	  }

	  private class AddDirectoriesThreads
	  {
		  internal bool InstanceFieldsInitialized = false;

		  internal virtual void InitializeInstanceFields()
		  {
			  Threads = new Thread[outerInstance.NumThreads];
		  }

		  private readonly TestIndexWriterReader OuterInstance;

		internal Directory AddDir;
		internal const int NUM_INIT_DOCS = 100;
		internal int NumDirs;
		internal Thread[] Threads;
		internal IndexWriter MainWriter;
		internal readonly IList<Exception> Failures = new List<Exception>();
		internal IndexReader[] Readers;
		internal bool DidClose = false;
		internal AtomicInteger Count = new AtomicInteger(0);
		internal AtomicInteger NumaddIndexes = new AtomicInteger(0);

		public AddDirectoriesThreads(TestIndexWriterReader outerInstance, int numDirs, IndexWriter mainWriter)
		{
			this.OuterInstance = outerInstance;

			if (!InstanceFieldsInitialized)
			{
				InitializeInstanceFields();
				InstanceFieldsInitialized = true;
			}
		  this.NumDirs = numDirs;
		  this.MainWriter = mainWriter;
		  AddDir = newDirectory();
		  IndexWriter writer = new IndexWriter(AddDir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setMaxBufferedDocs(2));
		  TestUtil.reduceOpenFiles(writer);
		  for (int i = 0; i < NUM_INIT_DOCS; i++)
		  {
			Document doc = DocHelper.createDocument(i, "addindex", 4);
			writer.addDocument(doc);
		  }

		  writer.close();

		  Readers = new IndexReader[numDirs];
		  for (int i = 0; i < numDirs; i++)
		  {
			Readers[i] = DirectoryReader.open(AddDir);
		  }
		}

		internal virtual void JoinThreads()
		{
		  for (int i = 0; i < outerInstance.NumThreads; i++)
		  {
			try
			{
			  Threads[i].Join();
			}
			catch (InterruptedException ie)
			{
			  throw new ThreadInterruptedException(ie);
			}
		  }
		}

		internal virtual void Close(bool doWait)
		{
		  DidClose = true;
		  if (doWait)
		  {
			MainWriter.waitForMerges();
		  }
		  MainWriter.close(doWait);
		}

		internal virtual void CloseDir()
		{
		  for (int i = 0; i < NumDirs; i++)
		  {
			Readers[i].close();
		  }
		  AddDir.close();
		}

		internal virtual void Handle(Exception t)
		{
		  t.printStackTrace(System.out);
		  lock (Failures)
		  {
			Failures.Add(t);
		  }
		}

		internal virtual void LaunchThreads(int numIter)
		{
		  for (int i = 0; i < outerInstance.NumThreads; i++)
		  {
			Threads[i] = new ThreadAnonymousInnerClassHelper(this, numIter);
		  }
		  for (int i = 0; i < outerInstance.NumThreads; i++)
		  {
			Threads[i].Start();
		  }
		}

		private class ThreadAnonymousInnerClassHelper : System.Threading.Thread
		{
			private readonly AddDirectoriesThreads OuterInstance;

			private int NumIter;

			public ThreadAnonymousInnerClassHelper(AddDirectoriesThreads outerInstance, int numIter)
			{
				this.OuterInstance = outerInstance;
				this.NumIter = numIter;
			}

			public override void Run()
			{
			  try
			  {
				Directory[] dirs = new Directory[OuterInstance.NumDirs];
				for (int k = 0; k < OuterInstance.NumDirs; k++)
				{
				  dirs[k] = new MockDirectoryWrapper(random(), new RAMDirectory(OuterInstance.AddDir, newIOContext(random())));
				}
				//int j = 0;
				//while (true) {
				  // System.out.println(Thread.currentThread().getName() + ": iter
				  // j=" + j);
				  for (int x = 0; x < NumIter; x++)
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
			  catch (Exception t)
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
			  MainWriter.addIndexes(dirs);
			  MainWriter.forceMerge(1);
			  break;
			case 1:
			  MainWriter.addIndexes(dirs);
			  NumaddIndexes.incrementAndGet();
			  break;
			case 2:
			  MainWriter.addIndexes(Readers);
			  break;
			case 3:
			  MainWriter.commit();
		  break;
		  }
		  Count.addAndGet(dirs.Length * NUM_INIT_DOCS);
		}
	  }

	  public virtual void TestIndexWriterReopenSegmentFullMerge()
	  {
		DoTestIndexWriterReopenSegment(true);
	  }

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
		Directory dir1 = GetAssertNoDeletesDirectory(newDirectory());
		IndexWriter writer = new IndexWriter(dir1, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())));
		IndexReader r1 = writer.Reader;
		Assert.AreEqual(0, r1.maxDoc());
		CreateIndexNoClose(false, "index1", writer);
		writer.flush(!doFullMerge, true);

		IndexReader iwr1 = writer.Reader;
		Assert.AreEqual(100, iwr1.maxDoc());

		IndexReader r2 = writer.Reader;
		Assert.AreEqual(r2.maxDoc(), 100);
		// add 100 documents
		for (int x = 10000; x < 10000 + 100; x++)
		{
		  Document d = DocHelper.createDocument(x, "index1", 5);
		  writer.addDocument(d);
		}
		writer.flush(false, true);
		// verify the reader was reopened internally
		IndexReader iwr2 = writer.Reader;
		Assert.IsTrue(iwr2 != r1);
		Assert.AreEqual(200, iwr2.maxDoc());
		// should have flushed out a segment
		IndexReader r3 = writer.Reader;
		Assert.IsTrue(r2 != r3);
		Assert.AreEqual(200, r3.maxDoc());

		// dec ref the readers rather than close them because
		// closing flushes changes to the writer
		r1.close();
		iwr1.close();
		r2.close();
		r3.close();
		iwr2.close();
		writer.close();

		// test whether the changes made it to the directory
		writer = new IndexWriter(dir1, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())));
		IndexReader w2r1 = writer.Reader;
		// insure the deletes were actually flushed to the directory
		Assert.AreEqual(200, w2r1.maxDoc());
		w2r1.close();
		writer.close();

		dir1.close();
	  }

	  /*
	   * Delete a document by term and return the doc id
	   * 
	   * public static int deleteDocument(Term term, IndexWriter writer) throws
	   * IOException { IndexReader reader = writer.getReader(); TermDocs td =
	   * reader.termDocs(term); int doc = -1; //if (td.next()) { // doc = td.doc();
	   * //} //writer.deleteDocuments(term); td.close(); return doc; }
	   */

	  public static void CreateIndex(Random random, Directory dir1, string indexName, bool multiSegment)
	  {
		IndexWriter w = new IndexWriter(dir1, LuceneTestCase.newIndexWriterConfig(random, TEST_VERSION_CURRENT, new MockAnalyzer(random)).setMergePolicy(new LogDocMergePolicy()));
		for (int i = 0; i < 100; i++)
		{
		  w.addDocument(DocHelper.createDocument(i, indexName, 4));
		}
		if (!multiSegment)
		{
		  w.forceMerge(1);
		}
		w.close();
	  }

	  public static void CreateIndexNoClose(bool multiSegment, string indexName, IndexWriter w)
	  {
		for (int i = 0; i < 100; i++)
		{
		  w.addDocument(DocHelper.createDocument(i, indexName, 4));
		}
		if (!multiSegment)
		{
		  w.forceMerge(1);
		}
	  }

	  private class MyWarmer : IndexWriter.IndexReaderWarmer
	  {
		internal int WarmCount;
		public override void Warm(AtomicReader reader)
		{
		  WarmCount++;
		}
	  }

	  public virtual void TestMergeWarmer()
	  {

		Directory dir1 = GetAssertNoDeletesDirectory(newDirectory());
		// Enroll warmer
		MyWarmer warmer = new MyWarmer();
		IndexWriter writer = new IndexWriter(dir1, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setMaxBufferedDocs(2).setMergedSegmentWarmer(warmer).setMergeScheduler(new ConcurrentMergeScheduler()).setMergePolicy(newLogMergePolicy()));

		// create the index
		CreateIndexNoClose(false, "test", writer);

		// get a reader to put writer into near real-time mode
		IndexReader r1 = writer.Reader;

		((LogMergePolicy) writer.Config.MergePolicy).MergeFactor = 2;

		int num = atLeast(100);
		for (int i = 0; i < num; i++)
		{
		  writer.addDocument(DocHelper.createDocument(i, "test", 4));
		}
		((ConcurrentMergeScheduler) writer.Config.MergeScheduler).sync();

		Assert.IsTrue(warmer.WarmCount > 0);
		int count = warmer.WarmCount;

		writer.addDocument(DocHelper.createDocument(17, "test", 4));
		writer.forceMerge(1);
		Assert.IsTrue(warmer.WarmCount > count);

		writer.close();
		r1.close();
		dir1.close();
	  }

	  public virtual void TestAfterCommit()
	  {
		Directory dir1 = GetAssertNoDeletesDirectory(newDirectory());
		IndexWriter writer = new IndexWriter(dir1, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setMergeScheduler(new ConcurrentMergeScheduler()));
		writer.commit();

		// create the index
		CreateIndexNoClose(false, "test", writer);

		// get a reader to put writer into near real-time mode
		DirectoryReader r1 = writer.Reader;
		TestUtil.checkIndex(dir1);
		writer.commit();
		TestUtil.checkIndex(dir1);
		Assert.AreEqual(100, r1.numDocs());

		for (int i = 0; i < 10; i++)
		{
		  writer.addDocument(DocHelper.createDocument(i, "test", 4));
		}
		((ConcurrentMergeScheduler) writer.Config.MergeScheduler).sync();

		DirectoryReader r2 = DirectoryReader.openIfChanged(r1);
		if (r2 != null)
		{
		  r1.close();
		  r1 = r2;
		}
		Assert.AreEqual(110, r1.numDocs());
		writer.close();
		r1.close();
		dir1.close();
	  }

	  // Make sure reader remains usable even if IndexWriter closes
	  public virtual void TestAfterClose()
	  {
		Directory dir1 = GetAssertNoDeletesDirectory(newDirectory());
		IndexWriter writer = new IndexWriter(dir1, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())));

		// create the index
		CreateIndexNoClose(false, "test", writer);

		DirectoryReader r = writer.Reader;
		writer.close();

		TestUtil.checkIndex(dir1);

		// reader should remain usable even after IndexWriter is closed:
		Assert.AreEqual(100, r.numDocs());
		Query q = new TermQuery(new Term("indexname", "test"));
		IndexSearcher searcher = newSearcher(r);
		Assert.AreEqual(100, searcher.search(q, 10).totalHits);
		try
		{
		  DirectoryReader.openIfChanged(r);
		  Assert.Fail("failed to hit AlreadyClosedException");
		}
		catch (AlreadyClosedException ace)
		{
		  // expected
		}
		r.close();
		dir1.close();
	  }

	  // Stress test reopen during addIndexes
	  public virtual void TestDuringAddIndexes()
	  {
		Directory dir1 = GetAssertNoDeletesDirectory(newDirectory());
		IndexWriter writer = new IndexWriter(dir1, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setMergePolicy(newLogMergePolicy(2)));

		// create the index
		CreateIndexNoClose(false, "test", writer);
		writer.commit();

		Directory[] dirs = new Directory[10];
		for (int i = 0;i < 10;i++)
		{
		  dirs[i] = new MockDirectoryWrapper(random(), new RAMDirectory(dir1, newIOContext(random())));
		}

		DirectoryReader r = writer.Reader;

		const float SECONDS = 0.5f;

		long endTime = (long)(System.currentTimeMillis() + 1000.0 * SECONDS);
		IList<Exception> excs = Collections.synchronizedList(new List<Exception>());

		// Only one thread can addIndexes at a time, because
		// IndexWriter acquires a write lock in each directory:
		Thread[] threads = new Thread[1];
		for (int i = 0;i < threads.Length;i++)
		{
		  threads[i] = new ThreadAnonymousInnerClassHelper(this, writer, dirs, endTime, excs);
		  threads[i].Daemon = true;
		  threads[i].Start();
		}

		int lastCount = 0;
		while (System.currentTimeMillis() < endTime)
		{
		  DirectoryReader r2 = DirectoryReader.openIfChanged(r);
		  if (r2 != null)
		  {
			r.close();
			r = r2;
		  }
		  Query q = new TermQuery(new Term("indexname", "test"));
		  IndexSearcher searcher = newSearcher(r);
		  int count = searcher.search(q, 10).totalHits;
		  Assert.IsTrue(count >= lastCount);
		  lastCount = count;
		}

		for (int i = 0;i < threads.Length;i++)
		{
		  threads[i].Join();
		}
		// final check
		DirectoryReader r2 = DirectoryReader.openIfChanged(r);
		if (r2 != null)
		{
		  r.close();
		  r = r2;
		}
		Query q = new TermQuery(new Term("indexname", "test"));
		IndexSearcher searcher = newSearcher(r);
		int count = searcher.search(q, 10).totalHits;
		Assert.IsTrue(count >= lastCount);

		Assert.AreEqual(0, excs.Count);
		r.close();
		if (dir1 is MockDirectoryWrapper)
		{
		  ICollection<string> openDeletedFiles = ((MockDirectoryWrapper)dir1).OpenDeletedFiles;
		  Assert.AreEqual("openDeleted=" + openDeletedFiles, 0, openDeletedFiles.Count);
		}

		writer.close();

		dir1.close();
	  }

	  private class ThreadAnonymousInnerClassHelper : System.Threading.Thread
	  {
		  private readonly TestIndexWriterReader OuterInstance;

		  private IndexWriter Writer;
		  private Directory[] Dirs;
		  private long EndTime;
		  private IList<Exception> Excs;

		  public ThreadAnonymousInnerClassHelper(TestIndexWriterReader outerInstance, IndexWriter writer, Directory[] dirs, long endTime, IList<Exception> excs)
		  {
			  this.OuterInstance = outerInstance;
			  this.Writer = writer;
			  this.Dirs = dirs;
			  this.EndTime = endTime;
			  this.Excs = excs;
		  }

		  public override void Run()
		  {
			do
			{
			  try
			  {
				Writer.addIndexes(Dirs);
				Writer.maybeMerge();
			  }
			  catch (Exception t)
			  {
				Excs.Add(t);
				throw new Exception(t);
			  }
			} while (System.currentTimeMillis() < EndTime);
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
	  public virtual void TestDuringAddDelete()
	  {
		Directory dir1 = newDirectory();
		IndexWriter writer = new IndexWriter(dir1, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setMergePolicy(newLogMergePolicy(2)));

		// create the index
		CreateIndexNoClose(false, "test", writer);
		writer.commit();

		DirectoryReader r = writer.Reader;

		const float SECONDS = 0.5f;

		long endTime = (long)(System.currentTimeMillis() + 1000.0 * SECONDS);
		IList<Exception> excs = Collections.synchronizedList(new List<Exception>());

		Thread[] threads = new Thread[NumThreads];
		for (int i = 0;i < NumThreads;i++)
		{
		  threads[i] = new ThreadAnonymousInnerClassHelper2(this, writer, r, endTime, excs);
		  threads[i].Daemon = true;
		  threads[i].Start();
		}

		int sum = 0;
		while (System.currentTimeMillis() < endTime)
		{
		  DirectoryReader r2 = DirectoryReader.openIfChanged(r);
		  if (r2 != null)
		  {
			r.close();
			r = r2;
		  }
		  Query q = new TermQuery(new Term("indexname", "test"));
		  IndexSearcher searcher = newSearcher(r);
		  sum += searcher.search(q, 10).totalHits;
		}

		for (int i = 0;i < NumThreads;i++)
		{
		  threads[i].Join();
		}
		// at least search once
		DirectoryReader r2 = DirectoryReader.openIfChanged(r);
		if (r2 != null)
		{
		  r.close();
		  r = r2;
		}
		Query q = new TermQuery(new Term("indexname", "test"));
		IndexSearcher searcher = newSearcher(r);
		sum += searcher.search(q, 10).totalHits;
		Assert.IsTrue("no documents found at all", sum > 0);

		Assert.AreEqual(0, excs.Count);
		writer.close();

		r.close();
		dir1.close();
	  }

	  private class ThreadAnonymousInnerClassHelper2 : System.Threading.Thread
	  {
		  private readonly TestIndexWriterReader OuterInstance;

		  private IndexWriter Writer;
		  private DirectoryReader r;
		  private long EndTime;
		  private IList<Exception> Excs;

		  public ThreadAnonymousInnerClassHelper2(TestIndexWriterReader outerInstance, IndexWriter writer, DirectoryReader r, long endTime, IList<Exception> excs)
		  {
			  this.OuterInstance = outerInstance;
			  this.Writer = writer;
			  this.r = r;
			  this.EndTime = endTime;
			  this.Excs = excs;
			  r = new Random(random().nextLong());
		  }

		  internal readonly Random r;

		  public override void Run()
		  {
			int count = 0;
			do
			{
			  try
			  {
				for (int docUpto = 0;docUpto < 10;docUpto++)
				{
				  Writer.addDocument(DocHelper.createDocument(10 * count + docUpto, "test", 4));
				}
				count++;
				int limit = count * 10;
				for (int delUpto = 0;delUpto < 5;delUpto++)
				{
				  int x = r.Next(limit);
				  Writer.deleteDocuments(new Term("field3", "b" + x));
				}
			  }
			  catch (Exception t)
			  {
				Excs.Add(t);
				throw new Exception(t);
			  }
			} while (System.currentTimeMillis() < EndTime);
		  }
	  }

	  public virtual void TestForceMergeDeletes()
	  {
		Directory dir = newDirectory();
		IndexWriter w = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setMergePolicy(newLogMergePolicy()));
		Document doc = new Document();
		doc.add(newTextField("field", "a b c", Field.Store.NO));
		Field id = newStringField("id", "", Field.Store.NO);
		doc.add(id);
		id.StringValue = "0";
		w.addDocument(doc);
		id.StringValue = "1";
		w.addDocument(doc);
		w.deleteDocuments(new Term("id", "0"));

		IndexReader r = w.Reader;
		w.forceMergeDeletes();
		w.close();
		r.close();
		r = DirectoryReader.open(dir);
		Assert.AreEqual(1, r.numDocs());
		Assert.IsFalse(r.hasDeletions());
		r.close();
		dir.close();
	  }

	  public virtual void TestDeletesNumDocs()
	  {
		Directory dir = newDirectory();
		IndexWriter w = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())));
		Document doc = new Document();
		doc.add(newTextField("field", "a b c", Field.Store.NO));
		Field id = newStringField("id", "", Field.Store.NO);
		doc.add(id);
		id.StringValue = "0";
		w.addDocument(doc);
		id.StringValue = "1";
		w.addDocument(doc);
		IndexReader r = w.Reader;
		Assert.AreEqual(2, r.numDocs());
		r.close();

		w.deleteDocuments(new Term("id", "0"));
		r = w.Reader;
		Assert.AreEqual(1, r.numDocs());
		r.close();

		w.deleteDocuments(new Term("id", "1"));
		r = w.Reader;
		Assert.AreEqual(0, r.numDocs());
		r.close();

		w.close();
		dir.close();
	  }

	  public virtual void TestEmptyIndex()
	  {
		// Ensures that getReader works on an empty index, which hasn't been committed yet.
		Directory dir = newDirectory();
		IndexWriter w = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())));
		IndexReader r = w.Reader;
		Assert.AreEqual(0, r.numDocs());
		r.close();
		w.close();
		dir.close();
	  }

	  public virtual void TestSegmentWarmer()
	  {
		Directory dir = newDirectory();
		AtomicBoolean didWarm = new AtomicBoolean();
		IndexWriter w = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setMaxBufferedDocs(2).setReaderPooling(true).setMergedSegmentWarmer(new IndexReaderWarmerAnonymousInnerClassHelper(this, didWarm)).
				setMergePolicy(newLogMergePolicy(10)));

		Document doc = new Document();
		doc.add(newStringField("foo", "bar", Field.Store.NO));
		for (int i = 0;i < 20;i++)
		{
		  w.addDocument(doc);
		}
		w.waitForMerges();
		w.close();
		dir.close();
		Assert.IsTrue(didWarm.get());
	  }

	  private class IndexReaderWarmerAnonymousInnerClassHelper : IndexWriter.IndexReaderWarmer
	  {
		  private readonly TestIndexWriterReader OuterInstance;

		  private AtomicBoolean DidWarm;

		  public IndexReaderWarmerAnonymousInnerClassHelper(TestIndexWriterReader outerInstance, AtomicBoolean didWarm)
		  {
			  this.OuterInstance = outerInstance;
			  this.DidWarm = didWarm;
		  }

		  public override void Warm(AtomicReader r)
		  {
			IndexSearcher s = newSearcher(r);
			TopDocs hits = s.search(new TermQuery(new Term("foo", "bar")), 10);
			Assert.AreEqual(20, hits.totalHits);
			DidWarm.set(true);
		  }
	  }

	  public virtual void TestSimpleMergedSegmentWramer()
	  {
		Directory dir = newDirectory();
		AtomicBoolean didWarm = new AtomicBoolean();
		InfoStream infoStream = new InfoStreamAnonymousInnerClassHelper(this, didWarm);
		IndexWriter w = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setMaxBufferedDocs(2).setReaderPooling(true).setInfoStream(infoStream).setMergedSegmentWarmer(new SimpleMergedSegmentWarmer(infoStream)).setMergePolicy(newLogMergePolicy(10)));

		Document doc = new Document();
		doc.add(newStringField("foo", "bar", Field.Store.NO));
		for (int i = 0;i < 20;i++)
		{
		  w.addDocument(doc);
		}
		w.waitForMerges();
		w.close();
		dir.close();
		Assert.IsTrue(didWarm.get());
	  }

	  private class InfoStreamAnonymousInnerClassHelper : InfoStream
	  {
		  private readonly TestIndexWriterReader OuterInstance;

		  private AtomicBoolean DidWarm;

		  public InfoStreamAnonymousInnerClassHelper(TestIndexWriterReader outerInstance, AtomicBoolean didWarm)
		  {
			  this.OuterInstance = outerInstance;
			  this.DidWarm = didWarm;
		  }

		  public override void Close()
		  {
		  }
		  public override void Message(string component, string message)
		  {
			if ("SMSW".Equals(component))
			{
			  DidWarm.set(true);
			}
		  }

		  public override bool IsEnabled(string component)
		  {
			return true;
		  }
	  }

	  public virtual void TestNoTermsIndex()
	  {
		// Some Codecs don't honor the ReaderTermsIndexDivisor, so skip the test if
		// they're picked.
		assumeFalse("PreFlex codec does not support ReaderTermsIndexDivisor!", "Lucene3x".Equals(Codec.Default.Name));

		IndexWriterConfig conf = newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setReaderTermsIndexDivisor(-1);

		// Don't proceed if picked Codec is in the list of illegal ones.
		string format = TestUtil.getPostingsFormat("f");
		assumeFalse("Format: " + format + " does not support ReaderTermsIndexDivisor!", (format.Equals("FSTPulsing41") || format.Equals("FSTOrdPulsing41") || format.Equals("FST41") || format.Equals("FSTOrd41") || format.Equals("SimpleText") || format.Equals("Memory") || format.Equals("MockRandom") || format.Equals("Direct")));

		Directory dir = newDirectory();
		IndexWriter w = new IndexWriter(dir, conf);
		Document doc = new Document();
		doc.add(new TextField("f", "val", Field.Store.NO));
		w.addDocument(doc);
		SegmentReader r = getOnlySegmentReader(DirectoryReader.open(w, true));
		try
		{
		  TestUtil.docs(random(), r, "f", new BytesRef("val"), null, null, DocsEnum.FLAG_NONE);
		  Assert.Fail("should have failed to seek since terms index was not loaded.");
		}
		catch (IllegalStateException e)
		{
		  // expected - we didn't load the term index
		}
		finally
		{
		  r.close();
		  w.close();
		  dir.close();
		}
	  }

	  public virtual void TestReopenAfterNoRealChange()
	  {
		Directory d = GetAssertNoDeletesDirectory(newDirectory());
		IndexWriter w = new IndexWriter(d, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())));

		DirectoryReader r = w.Reader; // start pooling readers

		DirectoryReader r2 = DirectoryReader.openIfChanged(r);
		assertNull(r2);

		w.addDocument(new Document());
		DirectoryReader r3 = DirectoryReader.openIfChanged(r);
		Assert.IsNotNull(r3);
		Assert.IsTrue(r3.Version != r.Version);
		Assert.IsTrue(r3.Current);

		// Deletes nothing in reality...:
		w.deleteDocuments(new Term("foo", "bar"));

		// ... but IW marks this as not current:
		Assert.IsFalse(r3.Current);
		DirectoryReader r4 = DirectoryReader.openIfChanged(r3);
		assertNull(r4);

		// Deletes nothing in reality...:
		w.deleteDocuments(new Term("foo", "bar"));
		DirectoryReader r5 = DirectoryReader.openIfChanged(r3, w, true);
		assertNull(r5);

		r3.close();

		w.close();
		d.close();
	  }

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void testNRTOpenExceptions() throws Exception
	  public virtual void TestNRTOpenExceptions()
	  {
		// LUCENE-5262: test that several failed attempts to obtain an NRT reader
		// don't leak file handles.
		MockDirectoryWrapper dir = (MockDirectoryWrapper) GetAssertNoDeletesDirectory(newMockDirectory());
		AtomicBoolean shouldFail = new AtomicBoolean();
		dir.failOn(new FailureAnonymousInnerClassHelper(this, dir, shouldFail));

		IndexWriterConfig conf = newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random()));
		conf.MergePolicy = NoMergePolicy.COMPOUND_FILES; // prevent merges from getting in the way
		IndexWriter writer = new IndexWriter(dir, conf);

		// create a segment and open an NRT reader
		writer.addDocument(new Document());
		writer.Reader.close();

		// add a new document so a new NRT reader is required
		writer.addDocument(new Document());

		// try to obtain an NRT reader twice: first time it fails and closes all the
		// other NRT readers. second time it fails, but also fails to close the
		// other NRT reader, since it is already marked closed!
		for (int i = 0; i < 2; i++)
		{
		  shouldFail.set(true);
		  try
		  {
			writer.Reader.close();
		  }
		  catch (FakeIOException e)
		  {
			// expected
			if (VERBOSE)
			{
			  Console.WriteLine("hit expected fake IOE");
			}
		  }
		}

		writer.close();
		dir.close();
	  }

	  private class FailureAnonymousInnerClassHelper : MockDirectoryWrapper.Failure
	  {
		  private readonly TestIndexWriterReader OuterInstance;

		  private MockDirectoryWrapper Dir;
		  private AtomicBoolean ShouldFail;

		  public FailureAnonymousInnerClassHelper(TestIndexWriterReader outerInstance, MockDirectoryWrapper dir, AtomicBoolean shouldFail)
		  {
			  this.OuterInstance = outerInstance;
			  this.Dir = dir;
			  this.ShouldFail = shouldFail;
		  }

		  public override void Eval(MockDirectoryWrapper dir)
		  {
			StackTraceElement[] trace = (new Exception()).StackTrace;
			if (ShouldFail.get())
			{
			  for (int i = 0; i < trace.Length; i++)
			  {
				if ("getReadOnlyClone".Equals(trace[i].MethodName))
				{
				  if (VERBOSE)
				  {
					Console.WriteLine("TEST: now fail; exc:");
					(new Exception()).printStackTrace(System.out);
				  }
				  ShouldFail.set(false);
				  throw new FakeIOException();
				}
			  }
			}
		  }
	  }

	  /// <summary>
	  /// Make sure if all we do is open NRT reader against
	  ///  writer, we don't see merge starvation. 
	  /// </summary>
	  public virtual void TestTooManySegments()
	  {
		Directory dir = GetAssertNoDeletesDirectory(newDirectory());
		// Don't use newIndexWriterConfig, because we need a
		// "sane" mergePolicy:
		IndexWriterConfig iwc = new IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random()));
		IndexWriter w = new IndexWriter(dir, iwc);
		// Create 500 segments:
		for (int i = 0;i < 500;i++)
		{
		  Document doc = new Document();
		  doc.add(newStringField("id", "" + i, Field.Store.NO));
		  w.addDocument(doc);
		  IndexReader r = DirectoryReader.open(w, true);
		  // Make sure segment count never exceeds 100:
		  Assert.IsTrue(r.leaves().size() < 100);
		  r.close();
		}
		w.close();
		dir.close();
	  }
	}

}