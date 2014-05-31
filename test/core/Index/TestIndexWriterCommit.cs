using System;
using System.Collections.Generic;
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


	using Lucene.Net.Analysis;
	using Document = Lucene.Net.Document.Document;
	using Field = Lucene.Net.Document.Field;
	using OpenMode = Lucene.Net.Index.IndexWriterConfig.OpenMode_e;
	using IndexSearcher = Lucene.Net.Search.IndexSearcher;
	using ScoreDoc = Lucene.Net.Search.ScoreDoc;
	using TermQuery = Lucene.Net.Search.TermQuery;
	using Directory = Lucene.Net.Store.Directory;
	using MockDirectoryWrapper = Lucene.Net.Store.MockDirectoryWrapper;
	using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
	using TestUtil = Lucene.Net.Util.TestUtil;

	public class TestIndexWriterCommit : LuceneTestCase
	{
	  /*
	   * Simple test for "commit on close": open writer then
	   * add a bunch of docs, making sure reader does not see
	   * these docs until writer is closed.
	   */
	  public virtual void TestCommitOnClose()
	  {
		  Directory dir = newDirectory();
		  IndexWriter writer = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())));
		  for (int i = 0; i < 14; i++)
		  {
			TestIndexWriter.AddDoc(writer);
		  }
		  writer.close();

		  Term searchTerm = new Term("content", "aaa");
		  DirectoryReader reader = DirectoryReader.open(dir);
		  IndexSearcher searcher = newSearcher(reader);
		  ScoreDoc[] hits = searcher.search(new TermQuery(searchTerm), null, 1000).scoreDocs;
		  Assert.AreEqual("first number of hits", 14, hits.Length);
		  reader.close();

		  reader = DirectoryReader.open(dir);

		  writer = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())));
		  for (int i = 0;i < 3;i++)
		  {
			for (int j = 0;j < 11;j++)
			{
			  TestIndexWriter.AddDoc(writer);
			}
			IndexReader r = DirectoryReader.open(dir);
			searcher = newSearcher(r);
			hits = searcher.search(new TermQuery(searchTerm), null, 1000).scoreDocs;
			Assert.AreEqual("reader incorrectly sees changes from writer", 14, hits.Length);
			r.close();
			Assert.IsTrue("reader should have still been current", reader.Current);
		  }

		  // Now, close the writer:
		  writer.close();
		  Assert.IsFalse("reader should not be current now", reader.Current);

		  IndexReader r = DirectoryReader.open(dir);
		  searcher = newSearcher(r);
		  hits = searcher.search(new TermQuery(searchTerm), null, 1000).scoreDocs;
		  Assert.AreEqual("reader did not see changes after writer was closed", 47, hits.Length);
		  r.close();
		  reader.close();
		  dir.close();
	  }

	  /*
	   * Simple test for "commit on close": open writer, then
	   * add a bunch of docs, making sure reader does not see
	   * them until writer has closed.  Then instead of
	   * closing the writer, call abort and verify reader sees
	   * nothing was added.  Then verify we can open the index
	   * and add docs to it.
	   */
	  public virtual void TestCommitOnCloseAbort()
	  {
		Directory dir = newDirectory();
		IndexWriter writer = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setMaxBufferedDocs(10));
		for (int i = 0; i < 14; i++)
		{
		  TestIndexWriter.AddDoc(writer);
		}
		writer.close();

		Term searchTerm = new Term("content", "aaa");
		IndexReader reader = DirectoryReader.open(dir);
		IndexSearcher searcher = newSearcher(reader);
		ScoreDoc[] hits = searcher.search(new TermQuery(searchTerm), null, 1000).scoreDocs;
		Assert.AreEqual("first number of hits", 14, hits.Length);
		reader.close();

		writer = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setOpenMode(OpenMode.APPEND).setMaxBufferedDocs(10));
		for (int j = 0;j < 17;j++)
		{
		  TestIndexWriter.AddDoc(writer);
		}
		// Delete all docs:
		writer.deleteDocuments(searchTerm);

		reader = DirectoryReader.open(dir);
		searcher = newSearcher(reader);
		hits = searcher.search(new TermQuery(searchTerm), null, 1000).scoreDocs;
		Assert.AreEqual("reader incorrectly sees changes from writer", 14, hits.Length);
		reader.close();

		// Now, close the writer:
		writer.rollback();

		TestIndexWriter.AssertNoUnreferencedFiles(dir, "unreferenced files remain after rollback()");

		reader = DirectoryReader.open(dir);
		searcher = newSearcher(reader);
		hits = searcher.search(new TermQuery(searchTerm), null, 1000).scoreDocs;
		Assert.AreEqual("saw changes after writer.abort", 14, hits.Length);
		reader.close();

		// Now make sure we can re-open the index, add docs,
		// and all is good:
		writer = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setOpenMode(OpenMode.APPEND).setMaxBufferedDocs(10));

		// On abort, writer in fact may write to the same
		// segments_N file:
		if (dir is MockDirectoryWrapper)
		{
		  ((MockDirectoryWrapper)dir).PreventDoubleWrite = false;
		}

		for (int i = 0;i < 12;i++)
		{
		  for (int j = 0;j < 17;j++)
		  {
			TestIndexWriter.AddDoc(writer);
		  }
		  IndexReader r = DirectoryReader.open(dir);
		  searcher = newSearcher(r);
		  hits = searcher.search(new TermQuery(searchTerm), null, 1000).scoreDocs;
		  Assert.AreEqual("reader incorrectly sees changes from writer", 14, hits.Length);
		  r.close();
		}

		writer.close();
		IndexReader r = DirectoryReader.open(dir);
		searcher = newSearcher(r);
		hits = searcher.search(new TermQuery(searchTerm), null, 1000).scoreDocs;
		Assert.AreEqual("didn't see changes after close", 218, hits.Length);
		r.close();

		dir.close();
	  }

	  /*
	   * Verify that a writer with "commit on close" indeed
	   * cleans up the temp segments created after opening
	   * that are not referenced by the starting segments
	   * file.  We check this by using MockDirectoryWrapper to
	   * measure max temp disk space used.
	   */
	  public virtual void TestCommitOnCloseDiskUsage()
	  {
		// MemoryCodec, since it uses FST, is not necessarily
		// "additive", ie if you add up N small FSTs, then merge
		// them, the merged result can easily be larger than the
		// sum because the merged FST may use array encoding for
		// some arcs (which uses more space):

		string idFormat = TestUtil.getPostingsFormat("id");
		string contentFormat = TestUtil.getPostingsFormat("content");
		assumeFalse("this test cannot run with Memory codec", idFormat.Equals("Memory") || contentFormat.Equals("Memory"));
		MockDirectoryWrapper dir = newMockDirectory();
		Analyzer analyzer;
		if (random().nextBoolean())
		{
		  // no payloads
		 analyzer = new AnalyzerAnonymousInnerClassHelper(this);
		}
		else
		{
		  // fixed length payloads
		  int length = random().Next(200);
		  analyzer = new AnalyzerAnonymousInnerClassHelper2(this, length);
		}

		IndexWriter writer = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, analyzer).setMaxBufferedDocs(10).setReaderPooling(false).setMergePolicy(newLogMergePolicy(10)));
		for (int j = 0;j < 30;j++)
		{
		  TestIndexWriter.AddDocWithIndex(writer, j);
		}
		writer.close();
		dir.resetMaxUsedSizeInBytes();

		dir.TrackDiskUsage = true;
		long startDiskUsage = dir.MaxUsedSizeInBytes;
		writer = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, analyzer).setOpenMode(OpenMode.APPEND).setMaxBufferedDocs(10).setMergeScheduler(new SerialMergeScheduler()).setReaderPooling(false).setMergePolicy(newLogMergePolicy(10)));
		for (int j = 0;j < 1470;j++)
		{
		  TestIndexWriter.AddDocWithIndex(writer, j);
		}
		long midDiskUsage = dir.MaxUsedSizeInBytes;
		dir.resetMaxUsedSizeInBytes();
		writer.forceMerge(1);
		writer.close();

		DirectoryReader.open(dir).close();

		long endDiskUsage = dir.MaxUsedSizeInBytes;

		// Ending index is 50X as large as starting index; due
		// to 3X disk usage normally we allow 150X max
		// transient usage.  If something is wrong w/ deleter
		// and it doesn't delete intermediate segments then it
		// will exceed this 150X:
		// System.out.println("start " + startDiskUsage + "; mid " + midDiskUsage + ";end " + endDiskUsage);
		Assert.IsTrue("writer used too much space while adding documents: mid=" + midDiskUsage + " start=" + startDiskUsage + " end=" + endDiskUsage + " max=" + (startDiskUsage * 150), midDiskUsage < 150 * startDiskUsage);
		Assert.IsTrue("writer used too much space after close: endDiskUsage=" + endDiskUsage + " startDiskUsage=" + startDiskUsage + " max=" + (startDiskUsage * 150), endDiskUsage < 150 * startDiskUsage);
		dir.close();
	  }

	  private class AnalyzerAnonymousInnerClassHelper : Analyzer
	  {
		  private readonly TestIndexWriterCommit OuterInstance;

		  public AnalyzerAnonymousInnerClassHelper(TestIndexWriterCommit outerInstance)
		  {
			  this.OuterInstance = outerInstance;
		  }

		  public override TokenStreamComponents CreateComponents(string fieldName, Reader reader)
		  {
			return new TokenStreamComponents(new MockTokenizer(reader, MockTokenizer.WHITESPACE, true));
		  }
	  }

	  private class AnalyzerAnonymousInnerClassHelper2 : Analyzer
	  {
		  private readonly TestIndexWriterCommit OuterInstance;

		  private int Length;

		  public AnalyzerAnonymousInnerClassHelper2(TestIndexWriterCommit outerInstance, int length)
		  {
			  this.OuterInstance = outerInstance;
			  this.Length = length;
		  }

		  public override TokenStreamComponents CreateComponents(string fieldName, Reader reader)
		  {
			Tokenizer tokenizer = new MockTokenizer(reader, MockTokenizer.WHITESPACE, true);
			return new TokenStreamComponents(tokenizer, new MockFixedLengthPayloadFilter(random(), tokenizer, Length));
		  }
	  }


	  /*
	   * Verify that calling forceMerge when writer is open for
	   * "commit on close" works correctly both for rollback()
	   * and close().
	   */
	  public virtual void TestCommitOnCloseForceMerge()
	  {
		Directory dir = newDirectory();
		// Must disable throwing exc on double-write: this
		// test uses IW.rollback which easily results in
		// writing to same file more than once
		if (dir is MockDirectoryWrapper)
		{
		  ((MockDirectoryWrapper)dir).PreventDoubleWrite = false;
		}
		IndexWriter writer = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setMaxBufferedDocs(10).setMergePolicy(newLogMergePolicy(10)));
		for (int j = 0;j < 17;j++)
		{
		  TestIndexWriter.AddDocWithIndex(writer, j);
		}
		writer.close();

		writer = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setOpenMode(OpenMode.APPEND));
		writer.forceMerge(1);

		// Open a reader before closing (commiting) the writer:
		DirectoryReader reader = DirectoryReader.open(dir);

		// Reader should see index as multi-seg at this
		// point:
		Assert.IsTrue("Reader incorrectly sees one segment", reader.leaves().size() > 1);
		reader.close();

		// Abort the writer:
		writer.rollback();
		TestIndexWriter.AssertNoUnreferencedFiles(dir, "aborted writer after forceMerge");

		// Open a reader after aborting writer:
		reader = DirectoryReader.open(dir);

		// Reader should still see index as multi-segment
		Assert.IsTrue("Reader incorrectly sees one segment", reader.leaves().size() > 1);
		reader.close();

		if (VERBOSE)
		{
		  Console.WriteLine("TEST: do real full merge");
		}
		writer = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setOpenMode(OpenMode.APPEND));
		writer.forceMerge(1);
		writer.close();

		if (VERBOSE)
		{
		  Console.WriteLine("TEST: writer closed");
		}
		TestIndexWriter.AssertNoUnreferencedFiles(dir, "aborted writer after forceMerge");

		// Open a reader after aborting writer:
		reader = DirectoryReader.open(dir);

		// Reader should see index as one segment
		Assert.AreEqual("Reader incorrectly sees more than one segment", 1, reader.leaves().size());
		reader.close();
		dir.close();
	  }

	  // LUCENE-2095: make sure with multiple threads commit
	  // doesn't return until all changes are in fact in the
	  // index
	  public virtual void TestCommitThreadSafety()
	  {
		const int NUM_THREADS = 5;
		const double RUN_SEC = 0.5;
		Directory dir = newDirectory();
		RandomIndexWriter w = new RandomIndexWriter(random(), dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setMergePolicy(newLogMergePolicy()));
		TestUtil.reduceOpenFiles(w.w);
		w.commit();
		AtomicBoolean failed = new AtomicBoolean();
		Thread[] threads = new Thread[NUM_THREADS];
		long endTime = System.currentTimeMillis() + ((long)(RUN_SEC * 1000));
		for (int i = 0;i < NUM_THREADS;i++)
		{
		  int finalI = i;
		  threads[i] = new ThreadAnonymousInnerClassHelper(this, dir, w, failed, endTime, finalI);
		  threads[i].Start();
		}
		for (int i = 0;i < NUM_THREADS;i++)
		{
		  threads[i].Join();
		}
		Assert.IsFalse(failed.get());
		w.close();
		dir.close();
	  }

	  private class ThreadAnonymousInnerClassHelper : System.Threading.Thread
	  {
		  private readonly TestIndexWriterCommit OuterInstance;

		  private Directory Dir;
		  private RandomIndexWriter w;
		  private AtomicBoolean Failed;
		  private long EndTime;
		  private int FinalI;

		  public ThreadAnonymousInnerClassHelper(TestIndexWriterCommit outerInstance, Directory dir, RandomIndexWriter w, AtomicBoolean failed, long endTime, int finalI)
		  {
			  this.OuterInstance = outerInstance;
			  this.Dir = dir;
			  this.w = w;
			  this.Failed = failed;
			  this.EndTime = endTime;
			  this.FinalI = finalI;
		  }

		  public override void Run()
		  {
			try
			{
			  Document doc = new Document();
			  DirectoryReader r = DirectoryReader.open(Dir);
			  Field f = newStringField("f", "", Field.Store.NO);
			  doc.add(f);
			  int count = 0;
			  do
			  {
				if (Failed.get())
				{
					break;
				}
				for (int j = 0;j < 10;j++)
				{
				  string s = FinalI + "_" + Convert.ToString(count++);
				  f.StringValue = s;
				  w.addDocument(doc);
				  w.commit();
				  DirectoryReader r2 = DirectoryReader.openIfChanged(r);
				  Assert.IsNotNull(r2);
				  Assert.IsTrue(r2 != r);
				  r.close();
				  r = r2;
				  Assert.AreEqual("term=f:" + s + "; r=" + r, 1, r.docFreq(new Term("f", s)));
				}
			  } while (System.currentTimeMillis() < EndTime);
			  r.close();
			}
			catch (Exception t)
			{
			  Failed.set(true);
			  throw new Exception(t);
			}
		  }
	  }

	  // LUCENE-1044: test writer.commit() when ac=false
	  public virtual void TestForceCommit()
	  {
		Directory dir = newDirectory();

		IndexWriter writer = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setMaxBufferedDocs(2).setMergePolicy(newLogMergePolicy(5)));
		writer.commit();

		for (int i = 0; i < 23; i++)
		{
		  TestIndexWriter.AddDoc(writer);
		}

		DirectoryReader reader = DirectoryReader.open(dir);
		Assert.AreEqual(0, reader.numDocs());
		writer.commit();
		DirectoryReader reader2 = DirectoryReader.openIfChanged(reader);
		Assert.IsNotNull(reader2);
		Assert.AreEqual(0, reader.numDocs());
		Assert.AreEqual(23, reader2.numDocs());
		reader.close();

		for (int i = 0; i < 17; i++)
		{
		  TestIndexWriter.AddDoc(writer);
		}
		Assert.AreEqual(23, reader2.numDocs());
		reader2.close();
		reader = DirectoryReader.open(dir);
		Assert.AreEqual(23, reader.numDocs());
		reader.close();
		writer.commit();

		reader = DirectoryReader.open(dir);
		Assert.AreEqual(40, reader.numDocs());
		reader.close();
		writer.close();
		dir.close();
	  }

	  public virtual void TestFutureCommit()
	  {
		Directory dir = newDirectory();

		IndexWriter w = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setIndexDeletionPolicy(NoDeletionPolicy.INSTANCE));
		Document doc = new Document();
		w.addDocument(doc);

		// commit to "first"
		IDictionary<string, string> commitData = new Dictionary<string, string>();
		commitData["tag"] = "first";
		w.CommitData = commitData;
		w.commit();

		// commit to "second"
		w.addDocument(doc);
		commitData["tag"] = "second";
		w.CommitData = commitData;
		w.close();

		// open "first" with IndexWriter
		IndexCommit commit = null;
		foreach (IndexCommit c in DirectoryReader.listCommits(dir))
		{
		  if (c.UserData.get("tag").Equals("first"))
		  {
			commit = c;
			break;
		  }
		}

		Assert.IsNotNull(commit);

		w = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setIndexDeletionPolicy(NoDeletionPolicy.INSTANCE).setIndexCommit(commit));

		Assert.AreEqual(1, w.numDocs());

		// commit IndexWriter to "third"
		w.addDocument(doc);
		commitData["tag"] = "third";
		w.CommitData = commitData;
		w.close();

		// make sure "second" commit is still there
		commit = null;
		foreach (IndexCommit c in DirectoryReader.listCommits(dir))
		{
		  if (c.UserData.get("tag").Equals("second"))
		  {
			commit = c;
			break;
		  }
		}

		Assert.IsNotNull(commit);

		dir.close();
	  }

	  public virtual void TestZeroCommits()
	  {
		// Tests that if we don't call commit(), the directory has 0 commits. this has
		// changed since LUCENE-2386, where before IW would always commit on a fresh
		// new index.
		Directory dir = newDirectory();
		IndexWriter writer = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())));
		try
		{
		  DirectoryReader.listCommits(dir);
		  Assert.Fail("listCommits should have thrown an exception over empty index");
		}
		catch (IndexNotFoundException e)
		{
		  // that's expected !
		}
		// No changes still should generate a commit, because it's a new index.
		writer.close();
		Assert.AreEqual("expected 1 commits!", 1, DirectoryReader.listCommits(dir).size());
		dir.close();
	  }

	  // LUCENE-1274: test writer.prepareCommit()
	  public virtual void TestPrepareCommit()
	  {
		Directory dir = newDirectory();

		IndexWriter writer = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setMaxBufferedDocs(2).setMergePolicy(newLogMergePolicy(5)));
		writer.commit();

		for (int i = 0; i < 23; i++)
		{
		  TestIndexWriter.AddDoc(writer);
		}

		DirectoryReader reader = DirectoryReader.open(dir);
		Assert.AreEqual(0, reader.numDocs());

		writer.prepareCommit();

		IndexReader reader2 = DirectoryReader.open(dir);
		Assert.AreEqual(0, reader2.numDocs());

		writer.commit();

		IndexReader reader3 = DirectoryReader.openIfChanged(reader);
		Assert.IsNotNull(reader3);
		Assert.AreEqual(0, reader.numDocs());
		Assert.AreEqual(0, reader2.numDocs());
		Assert.AreEqual(23, reader3.numDocs());
		reader.close();
		reader2.close();

		for (int i = 0; i < 17; i++)
		{
		  TestIndexWriter.AddDoc(writer);
		}

		Assert.AreEqual(23, reader3.numDocs());
		reader3.close();
		reader = DirectoryReader.open(dir);
		Assert.AreEqual(23, reader.numDocs());
		reader.close();

		writer.prepareCommit();

		reader = DirectoryReader.open(dir);
		Assert.AreEqual(23, reader.numDocs());
		reader.close();

		writer.commit();
		reader = DirectoryReader.open(dir);
		Assert.AreEqual(40, reader.numDocs());
		reader.close();
		writer.close();
		dir.close();
	  }

	  // LUCENE-1274: test writer.prepareCommit()
	  public virtual void TestPrepareCommitRollback()
	  {
		Directory dir = newDirectory();
		if (dir is MockDirectoryWrapper)
		{
		  ((MockDirectoryWrapper)dir).PreventDoubleWrite = false;
		}

		IndexWriter writer = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setMaxBufferedDocs(2).setMergePolicy(newLogMergePolicy(5)));
		writer.commit();

		for (int i = 0; i < 23; i++)
		{
		  TestIndexWriter.AddDoc(writer);
		}

		DirectoryReader reader = DirectoryReader.open(dir);
		Assert.AreEqual(0, reader.numDocs());

		writer.prepareCommit();

		IndexReader reader2 = DirectoryReader.open(dir);
		Assert.AreEqual(0, reader2.numDocs());

		writer.rollback();

		IndexReader reader3 = DirectoryReader.openIfChanged(reader);
		assertNull(reader3);
		Assert.AreEqual(0, reader.numDocs());
		Assert.AreEqual(0, reader2.numDocs());
		reader.close();
		reader2.close();

		writer = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())));
		for (int i = 0; i < 17; i++)
		{
		  TestIndexWriter.AddDoc(writer);
		}

		reader = DirectoryReader.open(dir);
		Assert.AreEqual(0, reader.numDocs());
		reader.close();

		writer.prepareCommit();

		reader = DirectoryReader.open(dir);
		Assert.AreEqual(0, reader.numDocs());
		reader.close();

		writer.commit();
		reader = DirectoryReader.open(dir);
		Assert.AreEqual(17, reader.numDocs());
		reader.close();
		writer.close();
		dir.close();
	  }

	  // LUCENE-1274
	  public virtual void TestPrepareCommitNoChanges()
	  {
		Directory dir = newDirectory();

		IndexWriter writer = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())));
		writer.prepareCommit();
		writer.commit();
		writer.close();

		IndexReader reader = DirectoryReader.open(dir);
		Assert.AreEqual(0, reader.numDocs());
		reader.close();
		dir.close();
	  }

	  // LUCENE-1382
	  public virtual void TestCommitUserData()
	  {
		Directory dir = newDirectory();
		IndexWriter w = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setMaxBufferedDocs(2));
		for (int j = 0;j < 17;j++)
		{
		  TestIndexWriter.AddDoc(w);
		}
		w.close();

		DirectoryReader r = DirectoryReader.open(dir);
		// commit(Map) never called for this index
		Assert.AreEqual(0, r.IndexCommit.UserData.size());
		r.close();

		w = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setMaxBufferedDocs(2));
		for (int j = 0;j < 17;j++)
		{
		  TestIndexWriter.AddDoc(w);
		}
		IDictionary<string, string> data = new Dictionary<string, string>();
		data["label"] = "test1";
		w.CommitData = data;
		w.close();

		r = DirectoryReader.open(dir);
		Assert.AreEqual("test1", r.IndexCommit.UserData.get("label"));
		r.close();

		w = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())));
		w.forceMerge(1);
		w.close();

		dir.close();
	  }
	}

}