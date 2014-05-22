using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Lucene.Net.Search
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
	using Document = Lucene.Net.Document.Document;
	using Field = Lucene.Net.Document.Field;
	using TextField = Lucene.Net.Document.TextField;
	using DirectoryReader = Lucene.Net.Index.DirectoryReader;
	using IndexCommit = Lucene.Net.Index.IndexCommit;
	using IndexReader = Lucene.Net.Index.IndexReader;
	using IndexWriter = Lucene.Net.Index.IndexWriter;
	using IndexWriterConfig = Lucene.Net.Index.IndexWriterConfig;
	using IndexableField = Lucene.Net.Index.IndexableField;
	using KeepOnlyLastCommitDeletionPolicy = Lucene.Net.Index.KeepOnlyLastCommitDeletionPolicy;
	using NoMergePolicy = Lucene.Net.Index.NoMergePolicy;
	using RandomIndexWriter = Lucene.Net.Index.RandomIndexWriter;
	using SnapshotDeletionPolicy = Lucene.Net.Index.SnapshotDeletionPolicy;
	using Term = Lucene.Net.Index.Term;
	using ThreadedIndexingAndSearchingTestCase = Lucene.Net.Index.ThreadedIndexingAndSearchingTestCase;
	using TrackingIndexWriter = Lucene.Net.Index.TrackingIndexWriter;
	using Directory = Lucene.Net.Store.Directory;
	using NRTCachingDirectory = Lucene.Net.Store.NRTCachingDirectory;
	using IOUtils = Lucene.Net.Util.IOUtils;
	using SuppressCodecs = Lucene.Net.Util.LuceneTestCase.SuppressCodecs;
	using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
	using TestUtil = Lucene.Net.Util.TestUtil;
	using ThreadInterruptedException = Lucene.Net.Util.ThreadInterruptedException;
	using Version = Lucene.Net.Util.Version;

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @SuppressCodecs({ "SimpleText", "Memory", "Direct" }) public class TestControlledRealTimeReopenThread extends Lucene.Net.Index.ThreadedIndexingAndSearchingTestCase
	public class TestControlledRealTimeReopenThread : ThreadedIndexingAndSearchingTestCase
	{

	  // Not guaranteed to reflect deletes:
	  private SearcherManager NrtNoDeletes;

	  // Is guaranteed to reflect deletes:
	  private SearcherManager NrtDeletes;

	  private TrackingIndexWriter GenWriter;

	  private ControlledRealTimeReopenThread<IndexSearcher> NrtDeletesThread;
	  private ControlledRealTimeReopenThread<IndexSearcher> NrtNoDeletesThread;

	  private readonly ThreadLocal<long?> LastGens = new ThreadLocal<long?>();
	  private bool WarmCalled;

	  public virtual void TestControlledRealTimeReopenThread()
	  {
		runTest("TestControlledRealTimeReopenThread");
	  }

	  protected internal override IndexSearcher FinalSearcher
	  {
		  get
		  {
			if (VERBOSE)
			{
			  Console.WriteLine("TEST: finalSearcher maxGen=" + MaxGen);
			}
			NrtDeletesThread.waitForGeneration(MaxGen);
			return NrtDeletes.acquire();
		  }
	  }

	  protected internal override Directory GetDirectory(Directory @in)
	  {
		// Randomly swap in NRTCachingDir
		if (random().nextBoolean())
		{
		  if (VERBOSE)
		  {
			Console.WriteLine("TEST: wrap NRTCachingDir");
		  }

		  return new NRTCachingDirectory(@in, 5.0, 60.0);
		}
		else
		{
		  return @in;
		}
	  }

	  protected internal override void updateDocuments<T1>(Term id, IList<T1> docs) where T1 : Iterable<T1 extends Lucene.Net.Index.IndexableField>
	  {
		long gen = GenWriter.updateDocuments(id, docs);

		// Randomly verify the update "took":
		if (random().Next(20) == 2)
		{
		  if (VERBOSE)
		  {
			Console.WriteLine(Thread.CurrentThread.Name + ": nrt: verify " + id);
		  }
		  NrtDeletesThread.waitForGeneration(gen);
		  IndexSearcher s = NrtDeletes.acquire();
		  if (VERBOSE)
		  {
			Console.WriteLine(Thread.CurrentThread.Name + ": nrt: got searcher=" + s);
		  }
		  try
		  {
			Assert.AreEqual(docs.Count, s.search(new TermQuery(id), 10).totalHits);
		  }
		  finally
		  {
			NrtDeletes.release(s);
		  }
		}

		LastGens.set(gen);
	  }

	  protected internal override void addDocuments<T1>(Term id, IList<T1> docs) where T1 : Iterable<T1 extends Lucene.Net.Index.IndexableField>
	  {
		long gen = GenWriter.addDocuments(docs);
		// Randomly verify the add "took":
		if (random().Next(20) == 2)
		{
		  if (VERBOSE)
		  {
			Console.WriteLine(Thread.CurrentThread.Name + ": nrt: verify " + id);
		  }
		  NrtNoDeletesThread.waitForGeneration(gen);
		  IndexSearcher s = NrtNoDeletes.acquire();
		  if (VERBOSE)
		  {
			Console.WriteLine(Thread.CurrentThread.Name + ": nrt: got searcher=" + s);
		  }
		  try
		  {
			Assert.AreEqual(docs.Count, s.search(new TermQuery(id), 10).totalHits);
		  }
		  finally
		  {
			NrtNoDeletes.release(s);
		  }
		}
		LastGens.set(gen);
	  }

	  protected internal override void addDocument<T1>(Term id, IEnumerable<T1> doc) where T1 : Lucene.Net.Index.IndexableField
	  {
		long gen = GenWriter.addDocument(doc);

		// Randomly verify the add "took":
		if (random().Next(20) == 2)
		{
		  if (VERBOSE)
		  {
			Console.WriteLine(Thread.CurrentThread.Name + ": nrt: verify " + id);
		  }
		  NrtNoDeletesThread.waitForGeneration(gen);
		  IndexSearcher s = NrtNoDeletes.acquire();
		  if (VERBOSE)
		  {
			Console.WriteLine(Thread.CurrentThread.Name + ": nrt: got searcher=" + s);
		  }
		  try
		  {
			Assert.AreEqual(1, s.search(new TermQuery(id), 10).totalHits);
		  }
		  finally
		  {
			NrtNoDeletes.release(s);
		  }
		}
		LastGens.set(gen);
	  }

	  protected internal override void updateDocument<T1>(Term id, IEnumerable<T1> doc) where T1 : Lucene.Net.Index.IndexableField
	  {
		long gen = GenWriter.updateDocument(id, doc);
		// Randomly verify the udpate "took":
		if (random().Next(20) == 2)
		{
		  if (VERBOSE)
		  {
			Console.WriteLine(Thread.CurrentThread.Name + ": nrt: verify " + id);
		  }
		  NrtDeletesThread.waitForGeneration(gen);
		  IndexSearcher s = NrtDeletes.acquire();
		  if (VERBOSE)
		  {
			Console.WriteLine(Thread.CurrentThread.Name + ": nrt: got searcher=" + s);
		  }
		  try
		  {
			Assert.AreEqual(1, s.search(new TermQuery(id), 10).totalHits);
		  }
		  finally
		  {
			NrtDeletes.release(s);
		  }
		}
		LastGens.set(gen);
	  }

	  protected internal override void DeleteDocuments(Term id)
	  {
		long gen = GenWriter.deleteDocuments(id);
		// randomly verify the delete "took":
		if (random().Next(20) == 7)
		{
		  if (VERBOSE)
		  {
			Console.WriteLine(Thread.CurrentThread.Name + ": nrt: verify del " + id);
		  }
		  NrtDeletesThread.waitForGeneration(gen);
		  IndexSearcher s = NrtDeletes.acquire();
		  if (VERBOSE)
		  {
			Console.WriteLine(Thread.CurrentThread.Name + ": nrt: got searcher=" + s);
		  }
		  try
		  {
			Assert.AreEqual(0, s.search(new TermQuery(id), 10).totalHits);
		  }
		  finally
		  {
			NrtDeletes.release(s);
		  }
		}
		LastGens.set(gen);
	  }

	  protected internal override void DoAfterWriter(ExecutorService es)
	  {
		double minReopenSec = 0.01 + 0.05 * random().NextDouble();
		double maxReopenSec = minReopenSec * (1.0 + 10 * random().NextDouble());

		if (VERBOSE)
		{
		  Console.WriteLine("TEST: make SearcherManager maxReopenSec=" + maxReopenSec + " minReopenSec=" + minReopenSec);
		}

		GenWriter = new TrackingIndexWriter(writer);

		SearcherFactory sf = new SearcherFactoryAnonymousInnerClassHelper(this, es);

		NrtNoDeletes = new SearcherManager(writer, false, sf);
		NrtDeletes = new SearcherManager(writer, true, sf);

		NrtDeletesThread = new ControlledRealTimeReopenThread<>(GenWriter, NrtDeletes, maxReopenSec, minReopenSec);
		NrtDeletesThread.Name = "NRTDeletes Reopen Thread";
		NrtDeletesThread.Priority = Math.Min(Thread.CurrentThread.Priority + 2, Thread.MAX_PRIORITY);
		NrtDeletesThread.Daemon = true;
		NrtDeletesThread.start();

		NrtNoDeletesThread = new ControlledRealTimeReopenThread<>(GenWriter, NrtNoDeletes, maxReopenSec, minReopenSec);
		NrtNoDeletesThread.Name = "NRTNoDeletes Reopen Thread";
		NrtNoDeletesThread.Priority = Math.Min(Thread.CurrentThread.Priority + 2, Thread.MAX_PRIORITY);
		NrtNoDeletesThread.Daemon = true;
		NrtNoDeletesThread.start();
	  }

	  private class SearcherFactoryAnonymousInnerClassHelper : SearcherFactory
	  {
		  private readonly TestControlledRealTimeReopenThread OuterInstance;

		  private ExecutorService Es;

		  public SearcherFactoryAnonymousInnerClassHelper(TestControlledRealTimeReopenThread outerInstance, ExecutorService es)
		  {
			  this.OuterInstance = outerInstance;
			  this.Es = es;
		  }

		  public override IndexSearcher NewSearcher(IndexReader r)
		  {
			OuterInstance.WarmCalled = true;
			IndexSearcher s = new IndexSearcher(r, Es);
			s.search(new TermQuery(new Term("body", "united")), 10);
			return s;
		  }
	  }

	  protected internal override void DoAfterIndexingThreadDone()
	  {
		long? gen = LastGens.get();
		if (gen != null)
		{
		  AddMaxGen(gen);
		}
	  }

	  private long MaxGen = -1;

	  private void AddMaxGen(long gen)
	  {
		  lock (this)
		  {
			MaxGen = Math.Max(gen, MaxGen);
		  }
	  }

	  protected internal override void DoSearching(ExecutorService es, long stopTime)
	  {
		runSearchThreads(stopTime);
	  }

	  protected internal override IndexSearcher CurrentSearcher
	  {
		  get
		  {
			// Test doesn't assert deletions until the end, so we
			// can randomize whether dels must be applied
			SearcherManager nrt;
			if (random().nextBoolean())
			{
			  nrt = NrtDeletes;
			}
			else
			{
			  nrt = NrtNoDeletes;
			}
    
			return nrt.acquire();
		  }
	  }

	  protected internal override void ReleaseSearcher(IndexSearcher s)
	  {
		// NOTE: a bit iffy... technically you should release
		// against the same SearcherManager you acquired from... but
		// both impls just decRef the underlying reader so we
		// can get away w/ cheating:
		NrtNoDeletes.release(s);
	  }

	  protected internal override void DoClose()
	  {
		Assert.IsTrue(WarmCalled);
		if (VERBOSE)
		{
		  Console.WriteLine("TEST: now close SearcherManagers");
		}
		NrtDeletesThread.close();
		NrtDeletes.close();
		NrtNoDeletesThread.close();
		NrtNoDeletes.close();
	  }

	  /*
	   * LUCENE-3528 - NRTManager hangs in certain situations 
	   */
	  public virtual void TestThreadStarvationNoDeleteNRTReader()
	  {
		IndexWriterConfig conf = newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random()));
		conf.MergePolicy = random().nextBoolean() ? NoMergePolicy.COMPOUND_FILES : NoMergePolicy.NO_COMPOUND_FILES;
		Directory d = newDirectory();
		CountDownLatch latch = new CountDownLatch(1);
		CountDownLatch signal = new CountDownLatch(1);

		LatchedIndexWriter _writer = new LatchedIndexWriter(d, conf, latch, signal);
		TrackingIndexWriter writer = new TrackingIndexWriter(_writer);
		SearcherManager manager = new SearcherManager(_writer, false, null);
		Document doc = new Document();
		doc.add(newTextField("test", "test", Field.Store.YES));
		writer.addDocument(doc);
		manager.maybeRefresh();
		Thread t = new ThreadAnonymousInnerClassHelper(this, latch, signal, writer, manager);
		t.Start();
		_writer.WaitAfterUpdate = true; // wait in addDocument to let some reopens go through
		long lastGen = writer.updateDocument(new Term("foo", "bar"), doc); // once this returns the doc is already reflected in the last reopen

		Assert.IsFalse(manager.SearcherCurrent); // false since there is a delete in the queue

		IndexSearcher searcher = manager.acquire();
		try
		{
		  Assert.AreEqual(2, searcher.IndexReader.numDocs());
		}
		finally
		{
		  manager.release(searcher);
		}
		ControlledRealTimeReopenThread<IndexSearcher> thread = new ControlledRealTimeReopenThread<IndexSearcher>(writer, manager, 0.01, 0.01);
		thread.start(); // start reopening
		if (VERBOSE)
		{
		  Console.WriteLine("waiting now for generation " + lastGen);
		}

		AtomicBoolean finished = new AtomicBoolean(false);
		Thread waiter = new ThreadAnonymousInnerClassHelper2(this, lastGen, thread, finished);
		waiter.Start();
		manager.maybeRefresh();
		waiter.Join(1000);
		if (!finished.get())
		{
		  waiter.Interrupt();
		  Assert.Fail("thread deadlocked on waitForGeneration");
		}
		thread.close();
		thread.join();
		IOUtils.close(manager, _writer, d);
	  }

	  private class ThreadAnonymousInnerClassHelper : System.Threading.Thread
	  {
		  private readonly TestControlledRealTimeReopenThread OuterInstance;

		  private CountDownLatch Latch;
		  private CountDownLatch Signal;
		  private TrackingIndexWriter Writer;
		  private SearcherManager Manager;

		  public ThreadAnonymousInnerClassHelper(TestControlledRealTimeReopenThread outerInstance, CountDownLatch latch, CountDownLatch signal, TrackingIndexWriter writer, SearcherManager manager)
		  {
			  this.OuterInstance = outerInstance;
			  this.Latch = latch;
			  this.Signal = signal;
			  this.Writer = writer;
			  this.Manager = manager;
		  }

		  public override void Run()
		  {
			try
			{
			  Signal.@await();
			  Manager.maybeRefresh();
			  Writer.deleteDocuments(new TermQuery(new Term("foo", "barista")));
			  Manager.maybeRefresh(); // kick off another reopen so we inc. the internal gen
			}
			catch (Exception e)
			{
			  Console.WriteLine(e.ToString());
			  Console.Write(e.StackTrace);
			}
			finally
			{
			  Latch.countDown(); // let the add below finish
			}
		  }
	  }

	  private class ThreadAnonymousInnerClassHelper2 : Thread
	  {
		  private readonly TestControlledRealTimeReopenThread OuterInstance;

		  private long LastGen;
		  private ControlledRealTimeReopenThread<IndexSearcher> Thread;
		  private AtomicBoolean Finished;

		  public ThreadAnonymousInnerClassHelper2(TestControlledRealTimeReopenThread outerInstance, long lastGen, ControlledRealTimeReopenThread<IndexSearcher> thread, AtomicBoolean finished)
		  {
			  this.OuterInstance = outerInstance;
			  this.LastGen = lastGen;
			  this.Thread = thread;
			  this.Finished = finished;
		  }

		  public override void Run()
		  {
			try
			{
			  Thread.waitForGeneration(LastGen);
			}
			catch (InterruptedException ie)
			{
			  Thread.CurrentThread.Interrupt();
			  throw new Exception(ie);
			}
			Finished.set(true);
		  }
	  }

	  public class LatchedIndexWriter : IndexWriter
	  {

		internal CountDownLatch Latch;
		internal bool WaitAfterUpdate = false;
		internal CountDownLatch Signal;

		public LatchedIndexWriter(Directory d, IndexWriterConfig conf, CountDownLatch latch, CountDownLatch signal) : base(d, conf)
		{
		  this.Latch = latch;
		  this.Signal = signal;

		}

		public override void updateDocument<T1>(Term term, IEnumerable<T1> doc, Analyzer analyzer) where T1 : Lucene.Net.Index.IndexableField
		{
		  base.updateDocument(term, doc, analyzer);
		  try
		  {
			if (WaitAfterUpdate)
			{
			  Signal.countDown();
			  Latch.@await();
			}
		  }
		  catch (InterruptedException e)
		  {
			throw new ThreadInterruptedException(e);
		  }
		}
	  }

	  public virtual void TestEvilSearcherFactory()
	  {
		Directory dir = newDirectory();
		RandomIndexWriter w = new RandomIndexWriter(random(), dir);
		w.commit();

		IndexReader other = DirectoryReader.open(dir);

		SearcherFactory theEvilOne = new SearcherFactoryAnonymousInnerClassHelper(this, other);

		try
		{
		  new SearcherManager(w.w, false, theEvilOne);
		  Assert.Fail("didn't hit expected exception");
		}
		catch (IllegalStateException ise)
		{
		  // expected
		}
		w.close();
		other.close();
		dir.close();
	  }

	  private class SearcherFactoryAnonymousInnerClassHelper : SearcherFactory
	  {
		  private readonly TestControlledRealTimeReopenThread OuterInstance;

		  private IndexReader Other;

		  public SearcherFactoryAnonymousInnerClassHelper(TestControlledRealTimeReopenThread outerInstance, IndexReader other)
		  {
			  this.OuterInstance = outerInstance;
			  this.Other = other;
		  }

		  public override IndexSearcher NewSearcher(IndexReader ignored)
		  {
			return LuceneTestCase.newSearcher(Other);
		  }
	  }

	  public virtual void TestListenerCalled()
	  {
		Directory dir = newDirectory();
		IndexWriter iw = new IndexWriter(dir, new IndexWriterConfig(TEST_VERSION_CURRENT, null));
		AtomicBoolean afterRefreshCalled = new AtomicBoolean(false);
		SearcherManager sm = new SearcherManager(iw, true, new SearcherFactory());
		sm.addListener(new RefreshListenerAnonymousInnerClassHelper(this, afterRefreshCalled));
		iw.addDocument(new Document());
		iw.commit();
		Assert.IsFalse(afterRefreshCalled.get());
		sm.maybeRefreshBlocking();
		Assert.IsTrue(afterRefreshCalled.get());
		sm.close();
		iw.close();
		dir.close();
	  }

	  private class RefreshListenerAnonymousInnerClassHelper : ReferenceManager.RefreshListener
	  {
		  private readonly TestControlledRealTimeReopenThread OuterInstance;

		  private AtomicBoolean AfterRefreshCalled;

		  public RefreshListenerAnonymousInnerClassHelper(TestControlledRealTimeReopenThread outerInstance, AtomicBoolean afterRefreshCalled)
		  {
			  this.OuterInstance = outerInstance;
			  this.AfterRefreshCalled = afterRefreshCalled;
		  }

		  public override void BeforeRefresh()
		  {
		  }
		  public override void AfterRefresh(bool didRefresh)
		  {
			if (didRefresh)
			{
			  AfterRefreshCalled.set(true);
			}
		  }
	  }

	  // LUCENE-5461
	  public virtual void TestCRTReopen()
	  {
		//test behaving badly

		//should be high enough
		int maxStaleSecs = 20;

		//build crap data just to store it.
		string s = "        abcdefghijklmnopqrstuvwxyz     ";
		char[] chars = s.ToCharArray();
		StringBuilder builder = new StringBuilder(2048);
		for (int i = 0; i < 2048; i++)
		{
		  builder.Append(chars[random().Next(chars.Length)]);
		}
		string content = builder.ToString();

		SnapshotDeletionPolicy sdp = new SnapshotDeletionPolicy(new KeepOnlyLastCommitDeletionPolicy());
		Directory dir = new NRTCachingDirectory(newFSDirectory(createTempDir("nrt")), 5, 128);
		IndexWriterConfig config = new IndexWriterConfig(Version.LUCENE_46, new MockAnalyzer(random()));
		config.IndexDeletionPolicy = sdp;
		config.OpenMode = IndexWriterConfig.OpenMode.CREATE_OR_APPEND;
		IndexWriter iw = new IndexWriter(dir, config);
		SearcherManager sm = new SearcherManager(iw, true, new SearcherFactory());
		TrackingIndexWriter tiw = new TrackingIndexWriter(iw);
		ControlledRealTimeReopenThread<IndexSearcher> controlledRealTimeReopenThread = new ControlledRealTimeReopenThread<IndexSearcher>(tiw, sm, maxStaleSecs, 0);

		controlledRealTimeReopenThread.Daemon = true;
		controlledRealTimeReopenThread.start();

		IList<Thread> commitThreads = new List<Thread>();

		for (int i = 0; i < 500; i++)
		{
		  if (i > 0 && i % 50 == 0)
		  {
			Thread commitThread = new Thread(new RunnableAnonymousInnerClassHelper(this, sdp, dir, iw));
			commitThread.Start();
			commitThreads.Add(commitThread);
		  }
		  Document d = new Document();
		  d.add(new TextField("count", i + "", Field.Store.NO));
		  d.add(new TextField("content", content, Field.Store.YES));
		  long start = System.currentTimeMillis();
		  long l = tiw.addDocument(d);
		  controlledRealTimeReopenThread.waitForGeneration(l);
		  long wait = System.currentTimeMillis() - start;
		  Assert.IsTrue("waited too long for generation " + wait, wait < (maxStaleSecs * 1000));
		  IndexSearcher searcher = sm.acquire();
		  TopDocs td = searcher.search(new TermQuery(new Term("count", i + "")), 10);
		  sm.release(searcher);
		  Assert.AreEqual(1, td.totalHits);
		}

		foreach (Thread commitThread in commitThreads)
		{
		  commitThread.Join();
		}

		controlledRealTimeReopenThread.close();
		sm.close();
		iw.close();
		dir.close();
	  }

	  private class RunnableAnonymousInnerClassHelper : Runnable
	  {
		  private readonly TestControlledRealTimeReopenThread OuterInstance;

		  private SnapshotDeletionPolicy Sdp;
		  private Directory Dir;
		  private IndexWriter Iw;

		  public RunnableAnonymousInnerClassHelper(TestControlledRealTimeReopenThread outerInstance, SnapshotDeletionPolicy sdp, Directory dir, IndexWriter iw)
		  {
			  this.OuterInstance = outerInstance;
			  this.Sdp = sdp;
			  this.Dir = dir;
			  this.Iw = iw;
		  }

		  public override void Run()
		  {
			try
			{
			  Iw.commit();
			  IndexCommit ic = Sdp.snapshot();
			  foreach (string name in ic.FileNames)
			  {
				//distribute, and backup
				//System.out.println(names);
				Assert.IsTrue(slowFileExists(Dir, name));
			  }
			}
			catch (Exception e)
			{
			  throw new Exception(e);
			}
		  }
	  }
	}

}