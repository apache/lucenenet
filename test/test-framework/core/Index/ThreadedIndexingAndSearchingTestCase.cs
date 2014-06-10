using System;
using System.Diagnostics;
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


	using MockAnalyzer = Lucene.Net.Analysis.MockAnalyzer;
	using Document = Lucene.Net.Document.Document;
	using Field = Lucene.Net.Document.Field;
	using IndexSearcher = Lucene.Net.Search.IndexSearcher;
	using PhraseQuery = Lucene.Net.Search.PhraseQuery;
	using Query = Lucene.Net.Search.Query;
	using ScoreDoc = Lucene.Net.Search.ScoreDoc;
	using Sort = Lucene.Net.Search.Sort;
	using SortField = Lucene.Net.Search.SortField;
	using TermQuery = Lucene.Net.Search.TermQuery;
	using TopDocs = Lucene.Net.Search.TopDocs;
	using BaseDirectoryWrapper = Lucene.Net.Store.BaseDirectoryWrapper;
	using Directory = Lucene.Net.Store.Directory;
	using Bits = Lucene.Net.Util.Bits;
	using BytesRef = Lucene.Net.Util.BytesRef;
	using FailOnNonBulkMergesInfoStream = Lucene.Net.Util.FailOnNonBulkMergesInfoStream;
	using LineFileDocs = Lucene.Net.Util.LineFileDocs;
	using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
	using NamedThreadFactory = Lucene.Net.Util.NamedThreadFactory;
	using PrintStreamInfoStream = Lucene.Net.Util.PrintStreamInfoStream;
	using TestUtil = Lucene.Net.Util.TestUtil;

	// TODO
	//   - mix in forceMerge, addIndexes
	//   - randomly mix in non-congruent docs

	/// <summary>
	/// Utility class that spawns multiple indexing and
	///  searching threads. 
	/// </summary>
	public abstract class ThreadedIndexingAndSearchingTestCase : LuceneTestCase
	{

	  protected internal readonly AtomicBoolean Failed = new AtomicBoolean();
	  protected internal readonly AtomicInteger AddCount = new AtomicInteger();
	  protected internal readonly AtomicInteger DelCount = new AtomicInteger();
	  protected internal readonly AtomicInteger PackCount = new AtomicInteger();

	  protected internal Directory Dir;
	  protected internal IndexWriter Writer;

	  private class SubDocs
	  {
		public readonly string PackID;
		public readonly IList<string> SubIDs;
		public bool Deleted;

		public SubDocs(string packID, IList<string> subIDs)
		{
		  this.PackID = packID;
		  this.SubIDs = subIDs;
		}
	  }

	  // Called per-search
	  protected internal abstract IndexSearcher CurrentSearcher {get;}

	  protected internal abstract IndexSearcher FinalSearcher {get;}

	  protected internal virtual void ReleaseSearcher(IndexSearcher s)
	  {
	  }

	  // Called once to run searching
	  protected internal abstract void DoSearching(ExecutorService es, long stopTime);

	  protected internal virtual Directory GetDirectory(Directory @in)
	  {
		return @in;
	  }

	  protected internal virtual void updateDocuments<T1>(Term id, IList<T1> docs) where T1 : Iterable<T1 extends IndexableField>
	  {
		Writer.updateDocuments(id, docs);
	  }

	  protected internal virtual void addDocuments<T1>(Term id, IList<T1> docs) where T1 : Iterable<T1 extends IndexableField>
	  {
		Writer.addDocuments(docs);
	  }

	  protected internal virtual void addDocument<T1>(Term id, IEnumerable<T1> doc) where T1 : IndexableField
	  {
		Writer.addDocument(doc);
	  }

	  protected internal virtual void updateDocument<T1>(Term term, IEnumerable<T1> doc) where T1 : IndexableField
	  {
		Writer.updateDocument(term, doc);
	  }

	  protected internal virtual void DeleteDocuments(Term term)
	  {
		Writer.deleteDocuments(term);
	  }

	  protected internal virtual void DoAfterIndexingThreadDone()
	  {
	  }

	  private Thread[] LaunchIndexingThreads(LineFileDocs docs, int numThreads, long stopTime, Set<string> delIDs, Set<string> delPackIDs, IList<SubDocs> allSubDocs)
	  {
		Thread[] threads = new Thread[numThreads];
		for (int thread = 0;thread < numThreads;thread++)
		{
		  threads[thread] = new ThreadAnonymousInnerClassHelper(this, docs, stopTime, delIDs, delPackIDs, allSubDocs);
		  threads[thread].Daemon = true;
		  threads[thread].Start();
		}

		return threads;
	  }

	  private class ThreadAnonymousInnerClassHelper : System.Threading.Thread
	  {
		  private readonly ThreadedIndexingAndSearchingTestCase OuterInstance;

		  private LineFileDocs Docs;
		  private long StopTime;
		  private Set<string> DelIDs;
		  private Set<string> DelPackIDs;
		  private IList<SubDocs> AllSubDocs;

		  public ThreadAnonymousInnerClassHelper(ThreadedIndexingAndSearchingTestCase outerInstance, LineFileDocs docs, long stopTime, Set<string> delIDs, Set<string> delPackIDs, IList<SubDocs> allSubDocs)
		  {
			  this.OuterInstance = outerInstance;
			  this.Docs = docs;
			  this.StopTime = stopTime;
			  this.DelIDs = delIDs;
			  this.DelPackIDs = delPackIDs;
			  this.AllSubDocs = allSubDocs;
		  }

		  public override void Run()
		  {
			// TODO: would be better if this were cross thread, so that we make sure one thread deleting anothers added docs works:
			IList<string> toDeleteIDs = new List<string>();
			IList<SubDocs> toDeleteSubDocs = new List<SubDocs>();
			while (System.currentTimeMillis() < StopTime && !OuterInstance.Failed.get())
			{
			  try
			  {

				// Occasional longish pause if running
				// nightly
				if (LuceneTestCase.TEST_NIGHTLY && Random().Next(6) == 3)
				{
				  if (VERBOSE)
				  {
					Console.WriteLine(Thread.CurrentThread.Name + ": now long sleep");
				  }
				  Thread.Sleep(TestUtil.NextInt(Random(), 50, 500));
				}

				// Rate limit ingest rate:
				if (Random().Next(7) == 5)
				{
				  Thread.Sleep(TestUtil.NextInt(Random(), 1, 10));
				  if (VERBOSE)
				  {
					Console.WriteLine(Thread.CurrentThread.Name + ": done sleep");
				  }
				}

				Document doc = Docs.NextDoc();
				if (doc == null)
				{
				  break;
				}

				// Maybe add randomly named field
				string addedField;
				if (Random().nextBoolean())
				{
				  addedField = "extra" + Random().Next(40);
				  doc.add(NewTextField(addedField, "a random field", Field.Store.YES));
				}
				else
				{
				  addedField = null;
				}

				if (Random().nextBoolean())
				{

				  if (Random().nextBoolean())
				  {
					// Add/update doc block:
					string packID;
					SubDocs delSubDocs;
					if (toDeleteSubDocs.Count > 0 && Random().nextBoolean())
					{
					  delSubDocs = toDeleteSubDocs[Random().Next(toDeleteSubDocs.Count)];
					  Debug.Assert(!delSubDocs.Deleted);
					  toDeleteSubDocs.Remove(delSubDocs);
					  // Update doc block, replacing prior packID
					  packID = delSubDocs.PackID;
					}
					else
					{
					  delSubDocs = null;
					  // Add doc block, using new packID
					  packID = OuterInstance.PackCount.AndIncrement + "";
					}

					Field packIDField = NewStringField("packID", packID, Field.Store.YES);
					IList<string> docIDs = new List<string>();
					SubDocs subDocs = new SubDocs(packID, docIDs);
					IList<Document> docsList = new List<Document>();

					AllSubDocs.Add(subDocs);
					doc.add(packIDField);
					docsList.Add(TestUtil.CloneDocument(doc));
					docIDs.Add(doc.get("docid"));

					int maxDocCount = TestUtil.NextInt(Random(), 1, 10);
					while (docsList.Count < maxDocCount)
					{
					  doc = Docs.NextDoc();
					  if (doc == null)
					  {
						break;
					  }
					  docsList.Add(TestUtil.CloneDocument(doc));
					  docIDs.Add(doc.get("docid"));
					}
					OuterInstance.AddCount.addAndGet(docsList.Count);

					Term packIDTerm = new Term("packID", packID);

					if (delSubDocs != null)
					{
					  delSubDocs.Deleted = true;
					  DelIDs.addAll(delSubDocs.SubIDs);
					  OuterInstance.DelCount.addAndGet(delSubDocs.SubIDs.Count);
					  if (VERBOSE)
					  {
						Console.WriteLine(Thread.CurrentThread.Name + ": update pack packID=" + delSubDocs.PackID + " count=" + docsList.Count + " docs=" + docIDs);
					  }
					  outerInstance.UpdateDocuments(packIDTerm, docsList);
					}
					else
					{
					  if (VERBOSE)
					  {
						Console.WriteLine(Thread.CurrentThread.Name + ": add pack packID=" + packID + " count=" + docsList.Count + " docs=" + docIDs);
					  }
					  outerInstance.AddDocuments(packIDTerm, docsList);
					}
					doc.removeField("packID");

					if (Random().Next(5) == 2)
					{
					  if (VERBOSE)
					  {
						Console.WriteLine(Thread.CurrentThread.Name + ": buffer del id:" + packID);
					  }
					  toDeleteSubDocs.Add(subDocs);
					}

				  }
				  else
				  {
					// Add single doc
					string docid = doc.get("docid");
					if (VERBOSE)
					{
					  Console.WriteLine(Thread.CurrentThread.Name + ": add doc docid:" + docid);
					}
					outerInstance.AddDocument(new Term("docid", docid), doc);
					OuterInstance.AddCount.AndIncrement;

					if (Random().Next(5) == 3)
					{
					  if (VERBOSE)
					  {
						Console.WriteLine(Thread.CurrentThread.Name + ": buffer del id:" + doc.get("docid"));
					  }
					  toDeleteIDs.Add(docid);
					}
				  }
				}
				else
				{

				  // Update single doc, but we never re-use
				  // and ID so the delete will never
				  // actually happen:
				  if (VERBOSE)
				  {
					Console.WriteLine(Thread.CurrentThread.Name + ": update doc id:" + doc.get("docid"));
				  }
				  string docid = doc.get("docid");
				  outerInstance.UpdateDocument(new Term("docid", docid), doc);
				  OuterInstance.AddCount.AndIncrement;

				  if (Random().Next(5) == 3)
				  {
					if (VERBOSE)
					{
					  Console.WriteLine(Thread.CurrentThread.Name + ": buffer del id:" + doc.get("docid"));
					}
					toDeleteIDs.Add(docid);
				  }
				}

				if (Random().Next(30) == 17)
				{
				  if (VERBOSE)
				  {
					Console.WriteLine(Thread.CurrentThread.Name + ": apply " + toDeleteIDs.Count + " deletes");
				  }
				  foreach (string id in toDeleteIDs)
				  {
					if (VERBOSE)
					{
					  Console.WriteLine(Thread.CurrentThread.Name + ": del term=id:" + id);
					}
					outerInstance.DeleteDocuments(new Term("docid", id));
				  }
				  int count = OuterInstance.DelCount.addAndGet(toDeleteIDs.Count);
				  if (VERBOSE)
				  {
					Console.WriteLine(Thread.CurrentThread.Name + ": tot " + count + " deletes");
				  }
				  DelIDs.addAll(toDeleteIDs);
				  toDeleteIDs.Clear();

				  foreach (SubDocs subDocs in toDeleteSubDocs)
				  {
					Debug.Assert(!subDocs.Deleted);
					DelPackIDs.add(subDocs.PackID);
					outerInstance.DeleteDocuments(new Term("packID", subDocs.PackID));
					subDocs.Deleted = true;
					if (VERBOSE)
					{
					  Console.WriteLine(Thread.CurrentThread.Name + ": del subs: " + subDocs.SubIDs + " packID=" + subDocs.PackID);
					}
					DelIDs.addAll(subDocs.SubIDs);
					OuterInstance.DelCount.addAndGet(subDocs.SubIDs.Count);
				  }
				  toDeleteSubDocs.Clear();
				}
				if (addedField != null)
				{
				  doc.removeField(addedField);
				}
			  }
			  catch (Exception t)
			  {
				Console.WriteLine(Thread.CurrentThread.Name + ": hit exc");
				Console.WriteLine(t.ToString());
				Console.Write(t.StackTrace);
				OuterInstance.Failed.set(true);
				throw new Exception(t);
			  }
			}
			if (VERBOSE)
			{
			  Console.WriteLine(Thread.CurrentThread.Name + ": indexing done");
			}

			outerInstance.DoAfterIndexingThreadDone();
		  }
	  }

	  protected internal virtual void RunSearchThreads(long stopTimeMS)
	  {
		int numThreads = TestUtil.NextInt(Random(), 1, 5);
		Thread[] searchThreads = new Thread[numThreads];
		AtomicInteger totHits = new AtomicInteger();

		// silly starting guess:
		AtomicInteger totTermCount = new AtomicInteger(100);

		// TODO: we should enrich this to do more interesting searches
		for (int thread = 0;thread < searchThreads.Length;thread++)
		{
		  searchThreads[thread] = new ThreadAnonymousInnerClassHelper2(this, stopTimeMS, totHits, totTermCount);
		  searchThreads[thread].Daemon = true;
		  searchThreads[thread].Start();
		}

		for (int thread = 0;thread < searchThreads.Length;thread++)
		{
		  searchThreads[thread].Join();
		}

		if (VERBOSE)
		{
		  Console.WriteLine("TEST: DONE search: totHits=" + totHits);
		}
	  }

	  private class ThreadAnonymousInnerClassHelper2 : System.Threading.Thread
	  {
		  private readonly ThreadedIndexingAndSearchingTestCase OuterInstance;

		  private long StopTimeMS;
		  private AtomicInteger TotHits;
		  private AtomicInteger TotTermCount;

		  public ThreadAnonymousInnerClassHelper2(ThreadedIndexingAndSearchingTestCase outerInstance, long stopTimeMS, AtomicInteger totHits, AtomicInteger totTermCount)
		  {
			  this.OuterInstance = outerInstance;
			  this.StopTimeMS = stopTimeMS;
			  this.TotHits = totHits;
			  this.TotTermCount = totTermCount;
		  }

		  public override void Run()
		  {
			if (VERBOSE)
			{
			  Console.WriteLine(Thread.CurrentThread.Name + ": launch search thread");
			}
			while (System.currentTimeMillis() < StopTimeMS)
			{
			  try
			  {
				IndexSearcher s = outerInstance.CurrentSearcher;
				try
				{
				  // Verify 1) IW is correctly setting
				  // diagnostics, and 2) segment warming for
				  // merged segments is actually happening:
				  foreach (AtomicReaderContext sub in s.IndexReader.leaves())
				  {
					SegmentReader segReader = (SegmentReader) sub.reader();
					IDictionary<string, string> diagnostics = segReader.SegmentInfo.info.Diagnostics;
					Assert.IsNotNull(diagnostics);
					string source = diagnostics["source"];
					Assert.IsNotNull(source);
					if (source.Equals("merge"))
					{
					  Assert.IsTrue("sub reader " + sub + " wasn't warmed: warmed=" + OuterInstance.Warmed + " diagnostics=" + diagnostics + " si=" + segReader.SegmentInfo, !OuterInstance.AssertMergedSegmentsWarmed || OuterInstance.Warmed.ContainsKey(segReader.core));
					}
				  }
				  if (s.IndexReader.numDocs() > 0)
				  {
					outerInstance.SmokeTestSearcher(s);
					Fields fields = MultiFields.getFields(s.IndexReader);
					if (fields == null)
					{
					  continue;
					}
					Terms terms = fields.terms("body");
					if (terms == null)
					{
					  continue;
					}
					TermsEnum termsEnum = terms.iterator(null);
					int seenTermCount = 0;
					int shift;
					int trigger;
					if (TotTermCount.get() < 30)
					{
					  shift = 0;
					  trigger = 1;
					}
					else
					{
					  trigger = TotTermCount.get() / 30;
					  shift = Random().Next(trigger);
					}
					while (System.currentTimeMillis() < StopTimeMS)
					{
					  BytesRef term = termsEnum.next();
					  if (term == null)
					  {
						TotTermCount.set(seenTermCount);
						break;
					  }
					  seenTermCount++;
					  // search 30 terms
					  if ((seenTermCount + shift) % trigger == 0)
					  {
						//if (VERBOSE) {
						//System.out.println(Thread.currentThread().getName() + " now search body:" + term.utf8ToString());
						//}
						TotHits.addAndGet(outerInstance.RunQuery(s, new TermQuery(new Term("body", term))));
					  }
					}
					//if (VERBOSE) {
					//System.out.println(Thread.currentThread().getName() + ": search done");
					//}
				  }
				}
				finally
				{
				  outerInstance.ReleaseSearcher(s);
				}
			  }
			  catch (Exception t)
			  {
				Console.WriteLine(Thread.CurrentThread.Name + ": hit exc");
				OuterInstance.Failed.set(true);
				t.printStackTrace(System.out);
				throw new Exception(t);
			  }
			}
		  }
	  }

	  protected internal virtual void DoAfterWriter(ExecutorService es)
	  {
	  }

	  protected internal virtual void DoClose()
	  {
	  }

	  protected internal bool AssertMergedSegmentsWarmed = true;

	  private readonly IDictionary<SegmentCoreReaders, bool?> Warmed = Collections.synchronizedMap(new WeakHashMap<SegmentCoreReaders, bool?>());

	  public virtual void RunTest(string testName)
	  {

		Failed.set(false);
		AddCount.set(0);
		DelCount.set(0);
		PackCount.set(0);

		long t0 = System.currentTimeMillis();

		Random random = new Random(Random().nextLong());
		LineFileDocs docs = new LineFileDocs(random, DefaultCodecSupportsDocValues());
		File tempDir = CreateTempDir(testName);
		Dir = GetDirectory(NewMockFSDirectory(tempDir)); // some subclasses rely on this being MDW
		if (Dir is BaseDirectoryWrapper)
		{
		  ((BaseDirectoryWrapper) Dir).CheckIndexOnClose = false; // don't double-checkIndex, we do it ourselves.
		}
		MockAnalyzer analyzer = new MockAnalyzer(Random());
		analyzer.MaxTokenLength = TestUtil.NextInt(Random(), 1, IndexWriter.MAX_TERM_LENGTH);
		IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer).setInfoStream(new FailOnNonBulkMergesInfoStream());

		if (LuceneTestCase.TEST_NIGHTLY)
		{
		  // newIWConfig makes smallish max seg size, which
		  // results in tons and tons of segments for this test
		  // when run nightly:
		  MergePolicy mp = conf.MergePolicy;
		  if (mp is TieredMergePolicy)
		  {
			((TieredMergePolicy) mp).MaxMergedSegmentMB = 5000.0;
		  }
		  else if (mp is LogByteSizeMergePolicy)
		  {
			((LogByteSizeMergePolicy) mp).MaxMergeMB = 1000.0;
		  }
		  else if (mp is LogMergePolicy)
		  {
			((LogMergePolicy) mp).MaxMergeDocs = 100000;
		  }
		}

		conf.MergedSegmentWarmer = new IndexReaderWarmerAnonymousInnerClassHelper(this);

		if (VERBOSE)
		{
		  conf.InfoStream = new PrintStreamInfoStreamAnonymousInnerClassHelper(this, System.out);
		}
		Writer = new IndexWriter(Dir, conf);
		TestUtil.ReduceOpenFiles(Writer);

		ExecutorService es = Random().nextBoolean() ? null : Executors.newCachedThreadPool(new NamedThreadFactory(testName));

		DoAfterWriter(es);

		int NUM_INDEX_THREADS = TestUtil.NextInt(Random(), 2, 4);

		int RUN_TIME_SEC = LuceneTestCase.TEST_NIGHTLY ? 300 : RANDOM_MULTIPLIER;

		Set<string> delIDs = Collections.synchronizedSet(new HashSet<string>());
		Set<string> delPackIDs = Collections.synchronizedSet(new HashSet<string>());
		IList<SubDocs> allSubDocs = Collections.synchronizedList(new List<SubDocs>());

		long stopTime = System.currentTimeMillis() + RUN_TIME_SEC * 1000;

		Thread[] indexThreads = LaunchIndexingThreads(docs, NUM_INDEX_THREADS, stopTime, delIDs, delPackIDs, allSubDocs);

		if (VERBOSE)
		{
		  Console.WriteLine("TEST: DONE start " + NUM_INDEX_THREADS + " indexing threads [" + (System.currentTimeMillis() - t0) + " ms]");
		}

		// Let index build up a bit
		Thread.Sleep(100);

		DoSearching(es, stopTime);

		if (VERBOSE)
		{
		  Console.WriteLine("TEST: all searching done [" + (System.currentTimeMillis() - t0) + " ms]");
		}

		for (int thread = 0;thread < indexThreads.Length;thread++)
		{
		  indexThreads[thread].Join();
		}

		if (VERBOSE)
		{
		  Console.WriteLine("TEST: done join indexing threads [" + (System.currentTimeMillis() - t0) + " ms]; addCount=" + AddCount + " delCount=" + DelCount);
		}

		IndexSearcher s = FinalSearcher;
		if (VERBOSE)
		{
		  Console.WriteLine("TEST: finalSearcher=" + s);
		}

		Assert.IsFalse(Failed.get());

		bool doFail = false;

		// Verify: make sure delIDs are in fact deleted:
		foreach (string id in delIDs)
		{
		  TopDocs hits = s.search(new TermQuery(new Term("docid", id)), 1);
		  if (hits.totalHits != 0)
		  {
			Console.WriteLine("doc id=" + id + " is supposed to be deleted, but got " + hits.totalHits + " hits; first docID=" + hits.scoreDocs[0].doc);
			doFail = true;
		  }
		}

		// Verify: make sure delPackIDs are in fact deleted:
		foreach (string id in delPackIDs)
		{
		  TopDocs hits = s.search(new TermQuery(new Term("packID", id)), 1);
		  if (hits.totalHits != 0)
		  {
			Console.WriteLine("packID=" + id + " is supposed to be deleted, but got " + hits.totalHits + " matches");
			doFail = true;
		  }
		}

		// Verify: make sure each group of sub-docs are still in docID order:
		foreach (SubDocs subDocs in allSubDocs)
		{
		  TopDocs hits = s.search(new TermQuery(new Term("packID", subDocs.PackID)), 20);
		  if (!subDocs.Deleted)
		  {
			// We sort by relevance but the scores should be identical so sort falls back to by docID:
			if (hits.totalHits != subDocs.SubIDs.Count)
			{
			  Console.WriteLine("packID=" + subDocs.PackID + ": expected " + subDocs.SubIDs.Count + " hits but got " + hits.totalHits);
			  doFail = true;
			}
			else
			{
			  int lastDocID = -1;
			  int startDocID = -1;
			  foreach (ScoreDoc scoreDoc in hits.scoreDocs)
			  {
				int docID = scoreDoc.doc;
				if (lastDocID != -1)
				{
				  Assert.AreEqual(1 + lastDocID, docID);
				}
				else
				{
				  startDocID = docID;
				}
				lastDocID = docID;
				Document doc = s.doc(docID);
				Assert.AreEqual(subDocs.PackID, doc.get("packID"));
			  }

			  lastDocID = startDocID - 1;
			  foreach (string subID in subDocs.SubIDs)
			  {
				hits = s.search(new TermQuery(new Term("docid", subID)), 1);
				Assert.AreEqual(1, hits.totalHits);
				int docID = hits.scoreDocs[0].doc;
				if (lastDocID != -1)
				{
				  Assert.AreEqual(1 + lastDocID, docID);
				}
				lastDocID = docID;
			  }
			}
		  }
		  else
		  {
			// Pack was deleted -- make sure its docs are
			// deleted.  We can't verify packID is deleted
			// because we can re-use packID for update:
			foreach (string subID in subDocs.SubIDs)
			{
			  Assert.AreEqual(0, s.search(new TermQuery(new Term("docid", subID)), 1).totalHits);
			}
		  }
		}

		// Verify: make sure all not-deleted docs are in fact
		// not deleted:
		int endID = Convert.ToInt32(docs.NextDoc().get("docid"));
		docs.Close();

		for (int id = 0;id < endID;id++)
		{
		  string stringID = "" + id;
		  if (!delIDs.contains(stringID))
		  {
			TopDocs hits = s.search(new TermQuery(new Term("docid", stringID)), 1);
			if (hits.totalHits != 1)
			{
			  Console.WriteLine("doc id=" + stringID + " is not supposed to be deleted, but got hitCount=" + hits.totalHits + "; delIDs=" + delIDs);
			  doFail = true;
			}
		  }
		}
		Assert.IsFalse(doFail);

		Assert.AreEqual("index=" + Writer.segString() + " addCount=" + AddCount + " delCount=" + DelCount, AddCount.get() - DelCount.get(), s.IndexReader.numDocs());
		ReleaseSearcher(s);

		Writer.commit();

		Assert.AreEqual("index=" + Writer.segString() + " addCount=" + AddCount + " delCount=" + DelCount, AddCount.get() - DelCount.get(), Writer.numDocs());

		DoClose();
		Writer.close(false);

		// Cannot shutdown until after writer is closed because
		// writer has merged segment warmer that uses IS to run
		// searches, and that IS may be using this es!
		if (es != null)
		{
		  es.shutdown();
		  es.awaitTermination(1, TimeUnit.SECONDS);
		}

		TestUtil.CheckIndex(Dir);
		Dir.close();
		TestUtil.Rm(tempDir);

		if (VERBOSE)
		{
		  Console.WriteLine("TEST: done [" + (System.currentTimeMillis() - t0) + " ms]");
		}
	  }

	  private class IndexReaderWarmerAnonymousInnerClassHelper : IndexWriter.IndexReaderWarmer
	  {
		  private readonly ThreadedIndexingAndSearchingTestCase OuterInstance;

		  public IndexReaderWarmerAnonymousInnerClassHelper(ThreadedIndexingAndSearchingTestCase outerInstance)
		  {
			  this.OuterInstance = outerInstance;
		  }

		  public override void Warm(AtomicReader reader)
		  {
			if (VERBOSE)
			{
			  Console.WriteLine("TEST: now warm merged reader=" + reader);
			}
			OuterInstance.Warmed[((SegmentReader) reader).core] = true;
			int maxDoc = reader.maxDoc();
			Bits liveDocs = reader.LiveDocs;
			int sum = 0;
			int inc = Math.Max(1, maxDoc / 50);
			for (int docID = 0;docID < maxDoc;docID += inc)
			{
			  if (liveDocs == null || liveDocs.get(docID))
			  {
				Document doc = reader.document(docID);
				sum += doc.Fields.size();
			  }
			}

			IndexSearcher searcher = NewSearcher(reader);
			sum += searcher.search(new TermQuery(new Term("body", "united")), 10).totalHits;

			if (VERBOSE)
			{
			  Console.WriteLine("TEST: warm visited " + sum + " fields");
			}
		  }
	  }

	  private class PrintStreamInfoStreamAnonymousInnerClassHelper : PrintStreamInfoStream
	  {
		  private readonly ThreadedIndexingAndSearchingTestCase OuterInstance;

		  public PrintStreamInfoStreamAnonymousInnerClassHelper(ThreadedIndexingAndSearchingTestCase outerInstance, UnknownType out) : base(out)
		  {
			  this.OuterInstance = outerInstance;
		  }

		  public override void Message(string component, string message)
		  {
			if ("TP".Equals(component))
			{
			  return; // ignore test points!
			}
			base.message(component, message);
		  }
	  }

	  private int RunQuery(IndexSearcher s, Query q)
	  {
		s.search(q, 10);
		int hitCount = s.search(q, null, 10, new Sort(new SortField("title", SortField.Type.STRING))).totalHits;
		if (DefaultCodecSupportsDocValues())
		{
		  Sort dvSort = new Sort(new SortField("title", SortField.Type.STRING));
		  int hitCount2 = s.search(q, null, 10, dvSort).totalHits;
		  Assert.AreEqual(hitCount, hitCount2);
		}
		return hitCount;
	  }

	  protected internal virtual void SmokeTestSearcher(IndexSearcher s)
	  {
		RunQuery(s, new TermQuery(new Term("body", "united")));
		RunQuery(s, new TermQuery(new Term("titleTokenized", "states")));
		PhraseQuery pq = new PhraseQuery();
		pq.add(new Term("body", "united"));
		pq.add(new Term("body", "states"));
		RunQuery(s, pq);
	  }
	}

}