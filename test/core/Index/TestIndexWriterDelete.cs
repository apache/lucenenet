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


	using Analyzer = Lucene.Net.Analysis.Analyzer;
	using MockAnalyzer = Lucene.Net.Analysis.MockAnalyzer;
	using MockTokenizer = Lucene.Net.Analysis.MockTokenizer;
	using Document = Lucene.Net.Document.Document;
	using Field = Lucene.Net.Document.Field;
	using FieldType = Lucene.Net.Document.FieldType;
	using NumericDocValuesField = Lucene.Net.Document.NumericDocValuesField;
	using StringField = Lucene.Net.Document.StringField;
	using IndexSearcher = Lucene.Net.Search.IndexSearcher;
	using ScoreDoc = Lucene.Net.Search.ScoreDoc;
	using TermQuery = Lucene.Net.Search.TermQuery;
	using Directory = Lucene.Net.Store.Directory;
	using MockDirectoryWrapper = Lucene.Net.Store.MockDirectoryWrapper;
	using RAMDirectory = Lucene.Net.Store.RAMDirectory;
	using IOUtils = Lucene.Net.Util.IOUtils;
	using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
	using TestUtil = Lucene.Net.Util.TestUtil;

	public class TestIndexWriterDelete : LuceneTestCase
	{

	  // test the simple case
	  public virtual void TestSimpleCase()
	  {
		string[] keywords = new string[] {"1", "2"};
		string[] unindexed = new string[] {"Netherlands", "Italy"};
		string[] unstored = new string[] {"Amsterdam has lots of bridges", "Venice has lots of canals"};
		string[] text = new string[] {"Amsterdam", "Venice"};

		Directory dir = newDirectory();
		IndexWriter modifier = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random(), MockTokenizer.WHITESPACE, false)).setMaxBufferedDeleteTerms(1));

		FieldType custom1 = new FieldType();
		custom1.Stored = true;
		for (int i = 0; i < keywords.Length; i++)
		{
		  Document doc = new Document();
		  doc.add(newStringField("id", keywords[i], Field.Store.YES));
		  doc.add(newField("country", unindexed[i], custom1));
		  doc.add(newTextField("contents", unstored[i], Field.Store.NO));
		  doc.add(newTextField("city", text[i], Field.Store.YES));
		  modifier.addDocument(doc);
		}
		modifier.forceMerge(1);
		modifier.commit();

		Term term = new Term("city", "Amsterdam");
		int hitCount = GetHitCount(dir, term);
		Assert.AreEqual(1, hitCount);
		if (VERBOSE)
		{
		  Console.WriteLine("\nTEST: now delete by term=" + term);
		}
		modifier.deleteDocuments(term);
		modifier.commit();

		if (VERBOSE)
		{
		  Console.WriteLine("\nTEST: now getHitCount");
		}
		hitCount = GetHitCount(dir, term);
		Assert.AreEqual(0, hitCount);

		modifier.close();
		dir.close();
	  }

	  // test when delete terms only apply to disk segments
	  public virtual void TestNonRAMDelete()
	  {

		Directory dir = newDirectory();
		IndexWriter modifier = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random(), MockTokenizer.WHITESPACE, false)).setMaxBufferedDocs(2).setMaxBufferedDeleteTerms(2));
		int id = 0;
		int value = 100;

		for (int i = 0; i < 7; i++)
		{
		  AddDoc(modifier, ++id, value);
		}
		modifier.commit();

		Assert.AreEqual(0, modifier.NumBufferedDocuments);
		Assert.IsTrue(0 < modifier.SegmentCount);

		modifier.commit();

		IndexReader reader = DirectoryReader.open(dir);
		Assert.AreEqual(7, reader.numDocs());
		reader.close();

		modifier.deleteDocuments(new Term("value", Convert.ToString(value)));

		modifier.commit();

		reader = DirectoryReader.open(dir);
		Assert.AreEqual(0, reader.numDocs());
		reader.close();
		modifier.close();
		dir.close();
	  }

	  public virtual void TestMaxBufferedDeletes()
	  {
		Directory dir = newDirectory();
		IndexWriter writer = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random(), MockTokenizer.WHITESPACE, false)).setMaxBufferedDeleteTerms(1));

		writer.addDocument(new Document());
		writer.deleteDocuments(new Term("foobar", "1"));
		writer.deleteDocuments(new Term("foobar", "1"));
		writer.deleteDocuments(new Term("foobar", "1"));
		Assert.AreEqual(3, writer.FlushDeletesCount);
		writer.close();
		dir.close();
	  }

	  // test when delete terms only apply to ram segments
	  public virtual void TestRAMDeletes()
	  {
		for (int t = 0;t < 2;t++)
		{
		  if (VERBOSE)
		  {
			Console.WriteLine("TEST: t=" + t);
		  }
		  Directory dir = newDirectory();
		  IndexWriter modifier = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random(), MockTokenizer.WHITESPACE, false)).setMaxBufferedDocs(4).setMaxBufferedDeleteTerms(4));
		  int id = 0;
		  int value = 100;

		  AddDoc(modifier, ++id, value);
		  if (0 == t)
		  {
			modifier.deleteDocuments(new Term("value", Convert.ToString(value)));
		  }
		  else
		  {
			modifier.deleteDocuments(new TermQuery(new Term("value", Convert.ToString(value))));
		  }
		  AddDoc(modifier, ++id, value);
		  if (0 == t)
		  {
			modifier.deleteDocuments(new Term("value", Convert.ToString(value)));
			Assert.AreEqual(2, modifier.NumBufferedDeleteTerms);
			Assert.AreEqual(1, modifier.BufferedDeleteTermsSize);
		  }
		  else
		  {
			modifier.deleteDocuments(new TermQuery(new Term("value", Convert.ToString(value))));
		  }

		  AddDoc(modifier, ++id, value);
		  Assert.AreEqual(0, modifier.SegmentCount);
		  modifier.commit();

		  IndexReader reader = DirectoryReader.open(dir);
		  Assert.AreEqual(1, reader.numDocs());

		  int hitCount = GetHitCount(dir, new Term("id", Convert.ToString(id)));
		  Assert.AreEqual(1, hitCount);
		  reader.close();
		  modifier.close();
		  dir.close();
		}
	  }

	  // test when delete terms apply to both disk and ram segments
	  public virtual void TestBothDeletes()
	  {
		Directory dir = newDirectory();
		IndexWriter modifier = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random(), MockTokenizer.WHITESPACE, false)).setMaxBufferedDocs(100).setMaxBufferedDeleteTerms(100));

		int id = 0;
		int value = 100;

		for (int i = 0; i < 5; i++)
		{
		  AddDoc(modifier, ++id, value);
		}

		value = 200;
		for (int i = 0; i < 5; i++)
		{
		  AddDoc(modifier, ++id, value);
		}
		modifier.commit();

		for (int i = 0; i < 5; i++)
		{
		  AddDoc(modifier, ++id, value);
		}
		modifier.deleteDocuments(new Term("value", Convert.ToString(value)));

		modifier.commit();

		IndexReader reader = DirectoryReader.open(dir);
		Assert.AreEqual(5, reader.numDocs());
		modifier.close();
		reader.close();
		dir.close();
	  }

	  // test that batched delete terms are flushed together
	  public virtual void TestBatchDeletes()
	  {
		Directory dir = newDirectory();
		IndexWriter modifier = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random(), MockTokenizer.WHITESPACE, false)).setMaxBufferedDocs(2).setMaxBufferedDeleteTerms(2));

		int id = 0;
		int value = 100;

		for (int i = 0; i < 7; i++)
		{
		  AddDoc(modifier, ++id, value);
		}
		modifier.commit();

		IndexReader reader = DirectoryReader.open(dir);
		Assert.AreEqual(7, reader.numDocs());
		reader.close();

		id = 0;
		modifier.deleteDocuments(new Term("id", Convert.ToString(++id)));
		modifier.deleteDocuments(new Term("id", Convert.ToString(++id)));

		modifier.commit();

		reader = DirectoryReader.open(dir);
		Assert.AreEqual(5, reader.numDocs());
		reader.close();

		Term[] terms = new Term[3];
		for (int i = 0; i < terms.Length; i++)
		{
		  terms[i] = new Term("id", Convert.ToString(++id));
		}
		modifier.deleteDocuments(terms);
		modifier.commit();
		reader = DirectoryReader.open(dir);
		Assert.AreEqual(2, reader.numDocs());
		reader.close();

		modifier.close();
		dir.close();
	  }

	  // test deleteAll()
	  public virtual void TestDeleteAll()
	  {
		Directory dir = newDirectory();
		IndexWriter modifier = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random(), MockTokenizer.WHITESPACE, false)).setMaxBufferedDocs(2).setMaxBufferedDeleteTerms(2));

		int id = 0;
		int value = 100;

		for (int i = 0; i < 7; i++)
		{
		  AddDoc(modifier, ++id, value);
		}
		modifier.commit();

		IndexReader reader = DirectoryReader.open(dir);
		Assert.AreEqual(7, reader.numDocs());
		reader.close();

		// Add 1 doc (so we will have something buffered)
		AddDoc(modifier, 99, value);

		// Delete all
		modifier.deleteAll();

		// Delete all shouldn't be on disk yet
		reader = DirectoryReader.open(dir);
		Assert.AreEqual(7, reader.numDocs());
		reader.close();

		// Add a doc and update a doc (after the deleteAll, before the commit)
		AddDoc(modifier, 101, value);
		UpdateDoc(modifier, 102, value);

		// commit the delete all
		modifier.commit();

		// Validate there are no docs left
		reader = DirectoryReader.open(dir);
		Assert.AreEqual(2, reader.numDocs());
		reader.close();

		modifier.close();
		dir.close();
	  }


	  public virtual void TestDeleteAllNoDeadLock()
	  {
		Directory dir = newDirectory();
		RandomIndexWriter modifier = new RandomIndexWriter(random(), dir);
		int numThreads = atLeast(2);
		Thread[] threads = new Thread[numThreads];
		CountDownLatch latch = new CountDownLatch(1);
		CountDownLatch doneLatch = new CountDownLatch(numThreads);
		for (int i = 0; i < numThreads; i++)
		{
		  int offset = i;
		  threads[i] = new ThreadAnonymousInnerClassHelper(this, modifier, latch, doneLatch, offset);
		  threads[i].Start();
		}
		latch.countDown();
		while (!doneLatch.@await(1, TimeUnit.MILLISECONDS))
		{
		  modifier.deleteAll();
		  if (VERBOSE)
		  {
			Console.WriteLine("del all");
		  }
		}

		modifier.deleteAll();
		foreach (Thread thread in threads)
		{
		  thread.Join();
		}

		modifier.close();
		DirectoryReader reader = DirectoryReader.open(dir);
		Assert.AreEqual(reader.maxDoc(), 0);
		Assert.AreEqual(reader.numDocs(), 0);
		Assert.AreEqual(reader.numDeletedDocs(), 0);
		reader.close();

		dir.close();
	  }

	  private class ThreadAnonymousInnerClassHelper : System.Threading.Thread
	  {
		  private readonly TestIndexWriterDelete OuterInstance;

		  private RandomIndexWriter Modifier;
		  private CountDownLatch Latch;
		  private CountDownLatch DoneLatch;
		  private int Offset;

		  public ThreadAnonymousInnerClassHelper(TestIndexWriterDelete outerInstance, RandomIndexWriter modifier, CountDownLatch latch, CountDownLatch doneLatch, int offset)
		  {
			  this.OuterInstance = outerInstance;
			  this.Modifier = modifier;
			  this.Latch = latch;
			  this.DoneLatch = doneLatch;
			  this.Offset = offset;
		  }

		  public override void Run()
		  {
			int id = Offset * 1000;
			int value = 100;
			try
			{
			  Latch.@await();
			  for (int j = 0; j < 1000; j++)
			  {
				Document doc = new Document();
				doc.add(newTextField("content", "aaa", Field.Store.NO));
				doc.add(newStringField("id", Convert.ToString(id++), Field.Store.YES));
				doc.add(newStringField("value", Convert.ToString(value), Field.Store.NO));
				if (defaultCodecSupportsDocValues())
				{
				  doc.add(new NumericDocValuesField("dv", value));
				}
				Modifier.addDocument(doc);
				if (VERBOSE)
				{
				  Console.WriteLine("\tThread[" + Offset + "]: add doc: " + id);
				}
			  }
			}
			catch (Exception e)
			{
			  throw new Exception(e);
			}
			finally
			{
			  DoneLatch.countDown();
			  if (VERBOSE)
			  {
				Console.WriteLine("\tThread[" + Offset + "]: done indexing");
			  }
			}
		  }
	  }

	  // test rollback of deleteAll()
	  public virtual void TestDeleteAllRollback()
	  {
		Directory dir = newDirectory();
		IndexWriter modifier = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random(), MockTokenizer.WHITESPACE, false)).setMaxBufferedDocs(2).setMaxBufferedDeleteTerms(2));

		int id = 0;
		int value = 100;

		for (int i = 0; i < 7; i++)
		{
		  AddDoc(modifier, ++id, value);
		}
		modifier.commit();

		AddDoc(modifier, ++id, value);

		IndexReader reader = DirectoryReader.open(dir);
		Assert.AreEqual(7, reader.numDocs());
		reader.close();

		// Delete all
		modifier.deleteAll();

		// Roll it back
		modifier.rollback();
		modifier.close();

		// Validate that the docs are still there
		reader = DirectoryReader.open(dir);
		Assert.AreEqual(7, reader.numDocs());
		reader.close();

		dir.close();
	  }


	  // test deleteAll() w/ near real-time reader
	  public virtual void TestDeleteAllNRT()
	  {
		Directory dir = newDirectory();
		IndexWriter modifier = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random(), MockTokenizer.WHITESPACE, false)).setMaxBufferedDocs(2).setMaxBufferedDeleteTerms(2));

		int id = 0;
		int value = 100;

		for (int i = 0; i < 7; i++)
		{
		  AddDoc(modifier, ++id, value);
		}
		modifier.commit();

		IndexReader reader = modifier.Reader;
		Assert.AreEqual(7, reader.numDocs());
		reader.close();

		AddDoc(modifier, ++id, value);
		AddDoc(modifier, ++id, value);

		// Delete all
		modifier.deleteAll();

		reader = modifier.Reader;
		Assert.AreEqual(0, reader.numDocs());
		reader.close();


		// Roll it back
		modifier.rollback();
		modifier.close();

		// Validate that the docs are still there
		reader = DirectoryReader.open(dir);
		Assert.AreEqual(7, reader.numDocs());
		reader.close();

		dir.close();
	  }


	  private void UpdateDoc(IndexWriter modifier, int id, int value)
	  {
		Document doc = new Document();
		doc.add(newTextField("content", "aaa", Field.Store.NO));
		doc.add(newStringField("id", Convert.ToString(id), Field.Store.YES));
		doc.add(newStringField("value", Convert.ToString(value), Field.Store.NO));
		if (defaultCodecSupportsDocValues())
		{
		  doc.add(new NumericDocValuesField("dv", value));
		}
		modifier.updateDocument(new Term("id", Convert.ToString(id)), doc);
	  }


	  private void AddDoc(IndexWriter modifier, int id, int value)
	  {
		Document doc = new Document();
		doc.add(newTextField("content", "aaa", Field.Store.NO));
		doc.add(newStringField("id", Convert.ToString(id), Field.Store.YES));
		doc.add(newStringField("value", Convert.ToString(value), Field.Store.NO));
		if (defaultCodecSupportsDocValues())
		{
		  doc.add(new NumericDocValuesField("dv", value));
		}
		modifier.addDocument(doc);
	  }

	  private int GetHitCount(Directory dir, Term term)
	  {
		IndexReader reader = DirectoryReader.open(dir);
		IndexSearcher searcher = newSearcher(reader);
		int hitCount = searcher.search(new TermQuery(term), null, 1000).totalHits;
		reader.close();
		return hitCount;
	  }

	  public virtual void TestDeletesOnDiskFull()
	  {
		DoTestOperationsOnDiskFull(false);
	  }

	  public virtual void TestUpdatesOnDiskFull()
	  {
		DoTestOperationsOnDiskFull(true);
	  }

	  /// <summary>
	  /// Make sure if modifier tries to commit but hits disk full that modifier
	  /// remains consistent and usable. Similar to TestIndexReader.testDiskFull().
	  /// </summary>
	  private void DoTestOperationsOnDiskFull(bool updates)
	  {

		Term searchTerm = new Term("content", "aaa");
		int START_COUNT = 157;
		int END_COUNT = 144;

		// First build up a starting index:
		MockDirectoryWrapper startDir = newMockDirectory();
		// TODO: find the resource leak that only occurs sometimes here.
		startDir.NoDeleteOpenFile = false;
		IndexWriter writer = new IndexWriter(startDir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random(), MockTokenizer.WHITESPACE, false)));
		for (int i = 0; i < 157; i++)
		{
		  Document d = new Document();
		  d.add(newStringField("id", Convert.ToString(i), Field.Store.YES));
		  d.add(newTextField("content", "aaa " + i, Field.Store.NO));
		  if (defaultCodecSupportsDocValues())
		  {
			d.add(new NumericDocValuesField("dv", i));
		  }
		  writer.addDocument(d);
		}
		writer.close();

		long diskUsage = startDir.sizeInBytes();
		long diskFree = diskUsage + 10;

		IOException err = null;

		bool done = false;

		// Iterate w/ ever increasing free disk space:
		while (!done)
		{
		  if (VERBOSE)
		  {
			Console.WriteLine("TEST: cycle");
		  }
		  MockDirectoryWrapper dir = new MockDirectoryWrapper(random(), new RAMDirectory(startDir, newIOContext(random())));
		  dir.PreventDoubleWrite = false;
		  dir.AllowRandomFileNotFoundException = false;
		  IndexWriter modifier = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random(), MockTokenizer.WHITESPACE, false)).setMaxBufferedDocs(1000).setMaxBufferedDeleteTerms(1000).setMergeScheduler(new ConcurrentMergeScheduler()));
		  ((ConcurrentMergeScheduler) modifier.Config.MergeScheduler).setSuppressExceptions();

		  // For each disk size, first try to commit against
		  // dir that will hit random IOExceptions & disk
		  // full; after, give it infinite disk space & turn
		  // off random IOExceptions & retry w/ same reader:
		  bool success = false;

		  for (int x = 0; x < 2; x++)
		  {
			if (VERBOSE)
			{
			  Console.WriteLine("TEST: x=" + x);
			}

			double rate = 0.1;
			double diskRatio = ((double)diskFree) / diskUsage;
			long thisDiskFree;
			string testName;

			if (0 == x)
			{
			  thisDiskFree = diskFree;
			  if (diskRatio >= 2.0)
			  {
				rate /= 2;
			  }
			  if (diskRatio >= 4.0)
			  {
				rate /= 2;
			  }
			  if (diskRatio >= 6.0)
			  {
				rate = 0.0;
			  }
			  if (VERBOSE)
			  {
				Console.WriteLine("\ncycle: " + diskFree + " bytes");
			  }
			  testName = "disk full during reader.close() @ " + thisDiskFree + " bytes";
			  dir.RandomIOExceptionRateOnOpen = random().NextDouble() * 0.01;
			}
			else
			{
			  thisDiskFree = 0;
			  rate = 0.0;
			  if (VERBOSE)
			  {
				Console.WriteLine("\ncycle: same writer: unlimited disk space");
			  }
			  testName = "reader re-use after disk full";
			  dir.RandomIOExceptionRateOnOpen = 0.0;
			}

			dir.MaxSizeInBytes = thisDiskFree;
			dir.RandomIOExceptionRate = rate;

			try
			{
			  if (0 == x)
			  {
				int docId = 12;
				for (int i = 0; i < 13; i++)
				{
				  if (updates)
				  {
					Document d = new Document();
					d.add(newStringField("id", Convert.ToString(i), Field.Store.YES));
					d.add(newTextField("content", "bbb " + i, Field.Store.NO));
					if (defaultCodecSupportsDocValues())
					{
					  d.add(new NumericDocValuesField("dv", i));
					}
					modifier.updateDocument(new Term("id", Convert.ToString(docId)), d);
				  } // deletes
				  else
				  {
					modifier.deleteDocuments(new Term("id", Convert.ToString(docId)));
					// modifier.setNorm(docId, "contents", (float)2.0);
				  }
				  docId += 12;
				}
			  }
			  modifier.close();
			  success = true;
			  if (0 == x)
			  {
				done = true;
			  }
			}
			catch (IOException e)
			{
			  if (VERBOSE)
			  {
				Console.WriteLine("  hit IOException: " + e);
				e.printStackTrace(System.out);
			  }
			  err = e;
			  if (1 == x)
			  {
				Console.WriteLine(e.ToString());
				Console.Write(e.StackTrace);
				Assert.Fail(testName + " hit IOException after disk space was freed up");
			  }
			}
			// prevent throwing a random exception here!!
			double randomIOExceptionRate = dir.RandomIOExceptionRate;
			long maxSizeInBytes = dir.MaxSizeInBytes;
			dir.RandomIOExceptionRate = 0.0;
			dir.RandomIOExceptionRateOnOpen = 0.0;
			dir.MaxSizeInBytes = 0;
			if (!success)
			{
			  // Must force the close else the writer can have
			  // open files which cause exc in MockRAMDir.close
			  if (VERBOSE)
			  {
				Console.WriteLine("TEST: now rollback");
			  }
			  modifier.rollback();
			}

			// If the close() succeeded, make sure there are
			// no unreferenced files.
			if (success)
			{
			  TestUtil.checkIndex(dir);
			  TestIndexWriter.AssertNoUnreferencedFiles(dir, "after writer.close");
			}
			dir.RandomIOExceptionRate = randomIOExceptionRate;
			dir.MaxSizeInBytes = maxSizeInBytes;

			// Finally, verify index is not corrupt, and, if
			// we succeeded, we see all docs changed, and if
			// we failed, we see either all docs or no docs
			// changed (transactional semantics):
			IndexReader newReader = null;
			try
			{
			  newReader = DirectoryReader.open(dir);
			}
			catch (IOException e)
			{
			  Console.WriteLine(e.ToString());
			  Console.Write(e.StackTrace);
			  Assert.Fail(testName + ":exception when creating IndexReader after disk full during close: " + e);
			}

			IndexSearcher searcher = newSearcher(newReader);
			ScoreDoc[] hits = null;
			try
			{
			  hits = searcher.search(new TermQuery(searchTerm), null, 1000).scoreDocs;
			}
			catch (IOException e)
			{
			  Console.WriteLine(e.ToString());
			  Console.Write(e.StackTrace);
			  Assert.Fail(testName + ": exception when searching: " + e);
			}
			int result2 = hits.Length;
			if (success)
			{
			  if (x == 0 && result2 != END_COUNT)
			  {
				Assert.Fail(testName + ": method did not throw exception but hits.length for search on term 'aaa' is " + result2 + " instead of expected " + END_COUNT);
			  }
			  else if (x == 1 && result2 != START_COUNT && result2 != END_COUNT)
			  {
				// It's possible that the first exception was
				// "recoverable" wrt pending deletes, in which
				// case the pending deletes are retained and
				// then re-flushing (with plenty of disk
				// space) will succeed in flushing the
				// deletes:
				Assert.Fail(testName + ": method did not throw exception but hits.length for search on term 'aaa' is " + result2 + " instead of expected " + START_COUNT + " or " + END_COUNT);
			  }
			}
			else
			{
			  // On hitting exception we still may have added
			  // all docs:
			  if (result2 != START_COUNT && result2 != END_COUNT)
			  {
				Console.WriteLine(err.ToString());
				Console.Write(err.StackTrace);
				Assert.Fail(testName + ": method did throw exception but hits.length for search on term 'aaa' is " + result2 + " instead of expected " + START_COUNT + " or " + END_COUNT);
			  }
			}
			newReader.close();
			if (result2 == END_COUNT)
			{
			  break;
			}
		  }
		  dir.close();
		  modifier.close();

		  // Try again with 10 more bytes of free space:
		  diskFree += 10;
		}
		startDir.close();
	  }

	  // this test tests that buffered deletes are cleared when
	  // an Exception is hit during flush.
	  public virtual void TestErrorAfterApplyDeletes()
	  {

		MockDirectoryWrapper.Failure failure = new FailureAnonymousInnerClassHelper(this);

		// create a couple of files

		string[] keywords = new string[] {"1", "2"};
		string[] unindexed = new string[] {"Netherlands", "Italy"};
		string[] unstored = new string[] {"Amsterdam has lots of bridges", "Venice has lots of canals"};
		string[] text = new string[] {"Amsterdam", "Venice"};

		MockDirectoryWrapper dir = newMockDirectory();
		IndexWriter modifier = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random(), MockTokenizer.WHITESPACE, false)).setMaxBufferedDeleteTerms(2).setReaderPooling(false).setMergePolicy(newLogMergePolicy()));

		MergePolicy lmp = modifier.Config.MergePolicy;
		lmp.NoCFSRatio = 1.0;

		dir.failOn(failure.reset());

		FieldType custom1 = new FieldType();
		custom1.Stored = true;
		for (int i = 0; i < keywords.Length; i++)
		{
		  Document doc = new Document();
		  doc.add(newStringField("id", keywords[i], Field.Store.YES));
		  doc.add(newField("country", unindexed[i], custom1));
		  doc.add(newTextField("contents", unstored[i], Field.Store.NO));
		  doc.add(newTextField("city", text[i], Field.Store.YES));
		  modifier.addDocument(doc);
		}
		// flush (and commit if ac)

		if (VERBOSE)
		{
		  Console.WriteLine("TEST: now full merge");
		}

		modifier.forceMerge(1);
		if (VERBOSE)
		{
		  Console.WriteLine("TEST: now commit");
		}
		modifier.commit();

		// one of the two files hits

		Term term = new Term("city", "Amsterdam");
		int hitCount = GetHitCount(dir, term);
		Assert.AreEqual(1, hitCount);

		// open the writer again (closed above)

		// delete the doc
		// max buf del terms is two, so this is buffered

		if (VERBOSE)
		{
		  Console.WriteLine("TEST: delete term=" + term);
		}

		modifier.deleteDocuments(term);

		// add a doc (needed for the !ac case; see below)
		// doc remains buffered

		if (VERBOSE)
		{
		  Console.WriteLine("TEST: add empty doc");
		}
		Document doc = new Document();
		modifier.addDocument(doc);

		// commit the changes, the buffered deletes, and the new doc

		// The failure object will fail on the first write after the del
		// file gets created when processing the buffered delete

		// in the ac case, this will be when writing the new segments
		// files so we really don't need the new doc, but it's harmless

		// a new segments file won't be created but in this
		// case, creation of the cfs file happens next so we
		// need the doc (to test that it's okay that we don't
		// lose deletes if failing while creating the cfs file)
		bool failed = false;
		try
		{
		  if (VERBOSE)
		  {
			Console.WriteLine("TEST: now commit for failure");
		  }
		  modifier.commit();
		}
		catch (IOException ioe)
		{
		  // expected
		  failed = true;
		}

		Assert.IsTrue(failed);

		// The commit above failed, so we need to retry it (which will
		// succeed, because the failure is a one-shot)

		modifier.commit();

		hitCount = GetHitCount(dir, term);

		// Make sure the delete was successfully flushed:
		Assert.AreEqual(0, hitCount);

		modifier.close();
		dir.close();
	  }

	  private class FailureAnonymousInnerClassHelper : MockDirectoryWrapper.Failure
	  {
		  private readonly TestIndexWriterDelete OuterInstance;

		  public FailureAnonymousInnerClassHelper(TestIndexWriterDelete outerInstance)
		  {
			  this.OuterInstance = outerInstance;
			  sawMaybe = false;
			  failed = false;
		  }

		  internal bool sawMaybe;
		  internal bool failed;
		  internal Thread thread;
		  public override MockDirectoryWrapper.Failure Reset()
		  {
			thread = Thread.CurrentThread;
			sawMaybe = false;
			failed = false;
			return this;
		  }
		  public override void Eval(MockDirectoryWrapper dir)
		  {
			if (Thread.CurrentThread != thread)
			{
			  // don't fail during merging
			  return;
			}
			if (sawMaybe && !failed)
			{
			  bool seen = false;
			  StackTraceElement[] trace = (new Exception()).StackTrace;
			  for (int i = 0; i < trace.Length; i++)
			  {
				if ("applyDeletesAndUpdates".Equals(trace[i].MethodName) || "slowFileExists".Equals(trace[i].MethodName))
				{
				  seen = true;
				  break;
				}
			  }
			  if (!seen)
			  {
				// Only fail once we are no longer in applyDeletes
				failed = true;
				if (VERBOSE)
				{
				  Console.WriteLine("TEST: mock failure: now fail");
				  (new Exception()).printStackTrace(System.out);
				}
				throw new IOException("fail after applyDeletes");
			  }
			}
			if (!failed)
			{
			  StackTraceElement[] trace = (new Exception()).StackTrace;
			  for (int i = 0; i < trace.Length; i++)
			  {
				if ("applyDeletesAndUpdates".Equals(trace[i].MethodName))
				{
				  if (VERBOSE)
				  {
					Console.WriteLine("TEST: mock failure: saw applyDeletes");
					(new Exception()).printStackTrace(System.out);
				  }
				  sawMaybe = true;
				  break;
				}
			  }
			}
		  }
	  }

	  // this test tests that the files created by the docs writer before
	  // a segment is written are cleaned up if there's an i/o error

	  public virtual void TestErrorInDocsWriterAdd()
	  {

		MockDirectoryWrapper.Failure failure = new FailureAnonymousInnerClassHelper2(this);

		// create a couple of files

		string[] keywords = new string[] {"1", "2"};
		string[] unindexed = new string[] {"Netherlands", "Italy"};
		string[] unstored = new string[] {"Amsterdam has lots of bridges", "Venice has lots of canals"};
		string[] text = new string[] {"Amsterdam", "Venice"};

		MockDirectoryWrapper dir = newMockDirectory();
		IndexWriter modifier = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random(), MockTokenizer.WHITESPACE, false)));
		modifier.commit();
		dir.failOn(failure.reset());

		FieldType custom1 = new FieldType();
		custom1.Stored = true;
		for (int i = 0; i < keywords.Length; i++)
		{
		  Document doc = new Document();
		  doc.add(newStringField("id", keywords[i], Field.Store.YES));
		  doc.add(newField("country", unindexed[i], custom1));
		  doc.add(newTextField("contents", unstored[i], Field.Store.NO));
		  doc.add(newTextField("city", text[i], Field.Store.YES));
		  try
		  {
			modifier.addDocument(doc);
		  }
		  catch (IOException io)
		  {
			if (VERBOSE)
			{
			  Console.WriteLine("TEST: got expected exc:");
			  io.printStackTrace(System.out);
			}
			break;
		  }
		}

		modifier.close();
		TestIndexWriter.AssertNoUnreferencedFiles(dir, "docsWriter.abort() failed to delete unreferenced files");
		dir.close();
	  }

	  private class FailureAnonymousInnerClassHelper2 : MockDirectoryWrapper.Failure
	  {
		  private readonly TestIndexWriterDelete OuterInstance;

		  public FailureAnonymousInnerClassHelper2(TestIndexWriterDelete outerInstance)
		  {
			  this.OuterInstance = outerInstance;
			  failed = false;
		  }

		  internal bool failed;
		  public override MockDirectoryWrapper.Failure Reset()
		  {
			failed = false;
			return this;
		  }
		  public override void Eval(MockDirectoryWrapper dir)
		  {
			if (!failed)
			{
			  failed = true;
			  throw new IOException("fail in add doc");
			}
		  }
	  }

	  public virtual void TestDeleteNullQuery()
	  {
		Directory dir = newDirectory();
		IndexWriter modifier = new IndexWriter(dir, new IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random(), MockTokenizer.WHITESPACE, false)));

		for (int i = 0; i < 5; i++)
		{
		  AddDoc(modifier, i, 2 * i);
		}

		modifier.deleteDocuments(new TermQuery(new Term("nada", "nada")));
		modifier.commit();
		Assert.AreEqual(5, modifier.numDocs());
		modifier.close();
		dir.close();
	  }

	  public virtual void TestDeleteAllSlowly()
	  {
		Directory dir = newDirectory();
		RandomIndexWriter w = new RandomIndexWriter(random(), dir);
		int NUM_DOCS = atLeast(1000);
		IList<int?> ids = new List<int?>(NUM_DOCS);
		for (int id = 0;id < NUM_DOCS;id++)
		{
		  ids.Add(id);
		}
		Collections.shuffle(ids, random());
		foreach (int id in ids)
		{
		  Document doc = new Document();
		  doc.add(newStringField("id", "" + id, Field.Store.NO));
		  w.addDocument(doc);
		}
		Collections.shuffle(ids, random());
		int upto = 0;
		while (upto < ids.Count)
		{
		  int left = ids.Count - upto;
		  int inc = Math.Min(left, TestUtil.Next(random(), 1, 20));
		  int limit = upto + inc;
		  while (upto < limit)
		  {
			w.deleteDocuments(new Term("id", "" + ids[upto++]));
		  }
		  IndexReader r = w.Reader;
		  Assert.AreEqual(NUM_DOCS - upto, r.numDocs());
		  r.close();
		}

		w.close();
		dir.close();
	  }

	  public virtual void TestIndexingThenDeleting()
	  {
		// TODO: move this test to its own class and just @SuppressCodecs?
		// TODO: is it enough to just use newFSDirectory?
		string fieldFormat = TestUtil.getPostingsFormat("field");
		assumeFalse("this test cannot run with Memory codec", fieldFormat.Equals("Memory"));
		assumeFalse("this test cannot run with SimpleText codec", fieldFormat.Equals("SimpleText"));
		assumeFalse("this test cannot run with Direct codec", fieldFormat.Equals("Direct"));
		Random r = random();
		Directory dir = newDirectory();
		// note this test explicitly disables payloads
		Analyzer analyzer = new AnalyzerAnonymousInnerClassHelper(this);
		IndexWriter w = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, analyzer).setRAMBufferSizeMB(1.0).setMaxBufferedDocs(IndexWriterConfig.DISABLE_AUTO_FLUSH).setMaxBufferedDeleteTerms(IndexWriterConfig.DISABLE_AUTO_FLUSH));
		Document doc = new Document();
		doc.add(newTextField("field", "go 1 2 3 4 5 6 7 8 9 10 11 12 13 14 15 16 17 18 19 20", Field.Store.NO));
		int num = atLeast(3);
		for (int iter = 0; iter < num; iter++)
		{
		  int count = 0;

		  bool doIndexing = r.nextBoolean();
		  if (VERBOSE)
		  {
			Console.WriteLine("TEST: iter doIndexing=" + doIndexing);
		  }
		  if (doIndexing)
		  {
			// Add docs until a flush is triggered
			int startFlushCount = w.FlushCount;
			while (w.FlushCount == startFlushCount)
			{
			  w.addDocument(doc);
			  count++;
			}
		  }
		  else
		  {
			// Delete docs until a flush is triggered
			int startFlushCount = w.FlushCount;
			while (w.FlushCount == startFlushCount)
			{
			  w.deleteDocuments(new Term("foo", "" + count));
			  count++;
			}
		  }
		  Assert.IsTrue("flush happened too quickly during " + (doIndexing ? "indexing" : "deleting") + " count=" + count, count > 2500);
		}
		w.close();
		dir.close();
	  }

	  private class AnalyzerAnonymousInnerClassHelper : Analyzer
	  {
		  private readonly TestIndexWriterDelete OuterInstance;

		  public AnalyzerAnonymousInnerClassHelper(TestIndexWriterDelete outerInstance)
		  {
			  this.OuterInstance = outerInstance;
		  }

		  public override TokenStreamComponents CreateComponents(string fieldName, Reader reader)
		  {
			return new TokenStreamComponents(new MockTokenizer(reader, MockTokenizer.WHITESPACE, true));
		  }
	  }

	  // LUCENE-3340: make sure deletes that we don't apply
	  // during flush (ie are just pushed into the stream) are
	  // in fact later flushed due to their RAM usage:
	  public virtual void TestFlushPushedDeletesByRAM()
	  {
		Directory dir = newDirectory();
		// Cannot use RandomIndexWriter because we don't want to
		// ever call commit() for this test:
		// note: tiny rambuffer used, as with a 1MB buffer the test is too slow (flush @ 128,999)
		IndexWriter w = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setRAMBufferSizeMB(0.1f).setMaxBufferedDocs(1000).setMergePolicy(NoMergePolicy.NO_COMPOUND_FILES).setReaderPooling(false));
		int count = 0;
		while (true)
		{
		  Document doc = new Document();
		  doc.add(new StringField("id", count + "", Field.Store.NO));
		  Term delTerm;
		  if (count == 1010)
		  {
			// this is the only delete that applies
			delTerm = new Term("id", "" + 0);
		  }
		  else
		  {
			// These get buffered, taking up RAM, but delete
			// nothing when applied:
			delTerm = new Term("id", "x" + count);
		  }
		  w.updateDocument(delTerm, doc);
		  // Eventually segment 0 should get a del docs:
		  // TODO: fix this test
		  if (slowFileExists(dir, "_0_1.del") || slowFileExists(dir, "_0_1.liv"))
		  {
			if (VERBOSE)
			{
			  Console.WriteLine("TEST: deletes created @ count=" + count);
			}
			break;
		  }
		  count++;

		  // Today we applyDeletes @ count=21553; even if we make
		  // sizable improvements to RAM efficiency of buffered
		  // del term we're unlikely to go over 100K:
		  if (count > 100000)
		  {
			Assert.Fail("delete's were not applied");
		  }
		}
		w.close();
		dir.close();
	  }

	  // LUCENE-3340: make sure deletes that we don't apply
	  // during flush (ie are just pushed into the stream) are
	  // in fact later flushed due to their RAM usage:
	  public virtual void TestFlushPushedDeletesByCount()
	  {
		Directory dir = newDirectory();
		// Cannot use RandomIndexWriter because we don't want to
		// ever call commit() for this test:
		int flushAtDelCount = atLeast(1020);
		IndexWriter w = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setMaxBufferedDeleteTerms(flushAtDelCount).setMaxBufferedDocs(1000).setRAMBufferSizeMB(IndexWriterConfig.DISABLE_AUTO_FLUSH).setMergePolicy(NoMergePolicy.NO_COMPOUND_FILES).setReaderPooling(false));
		int count = 0;
		while (true)
		{
		  Document doc = new Document();
		  doc.add(new StringField("id", count + "", Field.Store.NO));
		  Term delTerm;
		  if (count == 1010)
		  {
			// this is the only delete that applies
			delTerm = new Term("id", "" + 0);
		  }
		  else
		  {
			// These get buffered, taking up RAM, but delete
			// nothing when applied:
			delTerm = new Term("id", "x" + count);
		  }
		  w.updateDocument(delTerm, doc);
		  // Eventually segment 0 should get a del docs:
		  // TODO: fix this test
		  if (slowFileExists(dir, "_0_1.del") || slowFileExists(dir, "_0_1.liv"))
		  {
			break;
		  }
		  count++;
		  if (count > flushAtDelCount)
		  {
			Assert.Fail("delete's were not applied at count=" + flushAtDelCount);
		  }
		}
		w.close();
		dir.close();
	  }

	  // Make sure buffered (pushed) deletes don't use up so
	  // much RAM that it forces long tail of tiny segments:
//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Nightly public void testApplyDeletesOnFlush() throws Exception
	  public virtual void TestApplyDeletesOnFlush()
	  {
		Directory dir = newDirectory();
		// Cannot use RandomIndexWriter because we don't want to
		// ever call commit() for this test:
		AtomicInteger docsInSegment = new AtomicInteger();
		AtomicBoolean closing = new AtomicBoolean();
		AtomicBoolean sawAfterFlush = new AtomicBoolean();
		IndexWriter w = new IndexWriterAnonymousInnerClassHelper(this, dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setRAMBufferSizeMB(0.5).setMaxBufferedDocs(-1).setMergePolicy(NoMergePolicy.NO_COMPOUND_FILES).setReaderPooling(false), docsInSegment, closing, sawAfterFlush);
		int id = 0;
		while (true)
		{
		  StringBuilder sb = new StringBuilder();
		  for (int termIDX = 0;termIDX < 100;termIDX++)
		  {
			sb.Append(' ').Append(TestUtil.randomRealisticUnicodeString(random()));
		  }
		  if (id == 500)
		  {
			w.deleteDocuments(new Term("id", "0"));
		  }
		  Document doc = new Document();
		  doc.add(newStringField("id", "" + id, Field.Store.NO));
		  doc.add(newTextField("body", sb.ToString(), Field.Store.NO));
		  w.updateDocument(new Term("id", "" + id), doc);
		  docsInSegment.incrementAndGet();
		  // TODO: fix this test
		  if (slowFileExists(dir, "_0_1.del") || slowFileExists(dir, "_0_1.liv"))
		  {
			if (VERBOSE)
			{
			  Console.WriteLine("TEST: deletes created @ id=" + id);
			}
			break;
		  }
		  id++;
		}
		closing.set(true);
		Assert.IsTrue(sawAfterFlush.get());
		w.close();
		dir.close();
	  }

	  private class IndexWriterAnonymousInnerClassHelper : IndexWriter
	  {
		  private readonly TestIndexWriterDelete OuterInstance;

		  private AtomicInteger DocsInSegment;
		  private AtomicBoolean Closing;
		  private AtomicBoolean SawAfterFlush;

		  public IndexWriterAnonymousInnerClassHelper(TestIndexWriterDelete outerInstance, Directory dir, UnknownType setReaderPooling, AtomicInteger docsInSegment, AtomicBoolean closing, AtomicBoolean sawAfterFlush) : base(dir, setReaderPooling)
		  {
			  this.OuterInstance = outerInstance;
			  this.DocsInSegment = docsInSegment;
			  this.Closing = closing;
			  this.SawAfterFlush = sawAfterFlush;
		  }

		  public override void DoAfterFlush()
		  {
			Assert.IsTrue("only " + DocsInSegment.get() + " in segment", Closing.get() || DocsInSegment.get() >= 7);
			DocsInSegment.set(0);
			SawAfterFlush.set(true);
		  }
	  }

	  // LUCENE-4455
	  public virtual void TestDeletesCheckIndexOutput()
	  {
		Directory dir = newDirectory();
		IndexWriterConfig iwc = new IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random()));
		iwc.MaxBufferedDocs = 2;
		IndexWriter w = new IndexWriter(dir, iwc.clone());
		Document doc = new Document();
		doc.add(newField("field", "0", StringField.TYPE_NOT_STORED));
		w.addDocument(doc);

		doc = new Document();
		doc.add(newField("field", "1", StringField.TYPE_NOT_STORED));
		w.addDocument(doc);
		w.commit();
		Assert.AreEqual(1, w.SegmentCount);

		w.deleteDocuments(new Term("field", "0"));
		w.commit();
		Assert.AreEqual(1, w.SegmentCount);
		w.close();

		ByteArrayOutputStream bos = new ByteArrayOutputStream(1024);
		CheckIndex checker = new CheckIndex(dir);
		checker.setInfoStream(new PrintStream(bos, false, IOUtils.UTF_8), false);
		CheckIndex.Status indexStatus = checker.checkIndex(null);
		Assert.IsTrue(indexStatus.clean);
		string s = bos.ToString(IOUtils.UTF_8);

		// Segment should have deletions:
		Assert.IsTrue(s.Contains("has deletions"));
		w = new IndexWriter(dir, iwc.clone());
		w.forceMerge(1);
		w.close();

		bos = new ByteArrayOutputStream(1024);
		checker.setInfoStream(new PrintStream(bos, false, IOUtils.UTF_8), false);
		indexStatus = checker.checkIndex(null);
		Assert.IsTrue(indexStatus.clean);
		s = bos.ToString(IOUtils.UTF_8);
		Assert.IsFalse(s.Contains("has deletions"));
		dir.close();
	  }

	  public virtual void TestTryDeleteDocument()
	  {

		Directory d = newDirectory();

		IndexWriterConfig iwc = new IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random()));
		IndexWriter w = new IndexWriter(d, iwc);
		Document doc = new Document();
		w.addDocument(doc);
		w.addDocument(doc);
		w.addDocument(doc);
		w.close();

		iwc = new IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random()));
		iwc.OpenMode = IndexWriterConfig.OpenMode.APPEND;
		w = new IndexWriter(d, iwc);
		IndexReader r = DirectoryReader.open(w, false);
		Assert.IsTrue(w.tryDeleteDocument(r, 1));
		Assert.IsTrue(w.tryDeleteDocument(r.leaves().get(0).reader(), 0));
		r.close();
		w.close();

		r = DirectoryReader.open(d);
		Assert.AreEqual(2, r.numDeletedDocs());
		Assert.IsNotNull(MultiFields.getLiveDocs(r));
		r.close();
		d.close();
	  }
	}

}