using System;
using System.Collections.Generic;
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


	using MockAnalyzer = Lucene.Net.Analysis.MockAnalyzer;
	using Document = Lucene.Net.Document.Document;
	using ConcurrentMergeScheduler = Lucene.Net.Index.ConcurrentMergeScheduler;
	using DirectoryReader = Lucene.Net.Index.DirectoryReader;
	using IndexReader = Lucene.Net.Index.IndexReader;
	using IndexWriter = Lucene.Net.Index.IndexWriter;
	using IndexWriterConfig = Lucene.Net.Index.IndexWriterConfig;
	using RandomIndexWriter = Lucene.Net.Index.RandomIndexWriter;
	using Term = Lucene.Net.Index.Term;
	using ThreadedIndexingAndSearchingTestCase = Lucene.Net.Index.ThreadedIndexingAndSearchingTestCase;
	using AlreadyClosedException = Lucene.Net.Store.AlreadyClosedException;
	using Directory = Lucene.Net.Store.Directory;
	using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
	using SuppressCodecs = Lucene.Net.Util.LuceneTestCase.SuppressCodecs;
	using NamedThreadFactory = Lucene.Net.Util.NamedThreadFactory;
	using TestUtil = Lucene.Net.Util.TestUtil;

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @SuppressCodecs({ "SimpleText", "Memory", "Direct" }) public class TestSearcherManager extends Lucene.Net.Index.ThreadedIndexingAndSearchingTestCase
	public class TestSearcherManager : ThreadedIndexingAndSearchingTestCase
	{

	  internal bool WarmCalled;

	  private SearcherLifetimeManager.Pruner Pruner;

	  public virtual void TestSearcherManager()
	  {
		Pruner = new SearcherLifetimeManager.PruneByAge(TEST_NIGHTLY ? TestUtil.Next(random(), 1, 20) : 1);
		runTest("TestSearcherManager");
	  }

	  protected internal override IndexSearcher FinalSearcher
	  {
		  get
		  {
			if (!IsNRT)
			{
			  writer.commit();
			}
			Assert.IsTrue(Mgr.maybeRefresh() || Mgr.SearcherCurrent);
			return Mgr.acquire();
		  }
	  }

	  private SearcherManager Mgr;
	  private SearcherLifetimeManager LifetimeMGR;
	  private readonly IList<long?> PastSearchers = new List<long?>();
	  private bool IsNRT;

	  protected internal override void DoAfterWriter(ExecutorService es)
	  {
		SearcherFactory factory = new SearcherFactoryAnonymousInnerClassHelper(this, es);
		if (random().nextBoolean())
		{
		  // TODO: can we randomize the applyAllDeletes?  But
		  // somehow for final searcher we must apply
		  // deletes...
		  Mgr = new SearcherManager(writer, true, factory);
		  IsNRT = true;
		}
		else
		{
		  // SearcherManager needs to see empty commit:
		  writer.commit();
		  Mgr = new SearcherManager(dir, factory);
		  IsNRT = false;
		  assertMergedSegmentsWarmed = false;
		}

		LifetimeMGR = new SearcherLifetimeManager();
	  }

	  private class SearcherFactoryAnonymousInnerClassHelper : SearcherFactory
	  {
		  private readonly TestSearcherManager OuterInstance;

		  private ExecutorService Es;

		  public SearcherFactoryAnonymousInnerClassHelper(TestSearcherManager outerInstance, ExecutorService es)
		  {
			  this.OuterInstance = outerInstance;
			  this.Es = es;
		  }

		  public override IndexSearcher NewSearcher(IndexReader r)
		  {
			IndexSearcher s = new IndexSearcher(r, Es);
			OuterInstance.WarmCalled = true;
			s.search(new TermQuery(new Term("body", "united")), 10);
			return s;
		  }
	  }

	  protected internal override void DoSearching(ExecutorService es, long stopTime)
	  {

		Thread reopenThread = new ThreadAnonymousInnerClassHelper(this, stopTime);
		reopenThread.Daemon = true;
		reopenThread.Start();

		runSearchThreads(stopTime);

		reopenThread.Join();
	  }

	  private class ThreadAnonymousInnerClassHelper : System.Threading.Thread
	  {
		  private readonly TestSearcherManager OuterInstance;

		  private long StopTime;

		  public ThreadAnonymousInnerClassHelper(TestSearcherManager outerInstance, long stopTime)
		  {
			  this.OuterInstance = outerInstance;
			  this.StopTime = stopTime;
		  }

		  public override void Run()
		  {
			try
			{
			  if (VERBOSE)
			  {
				Console.WriteLine("[" + Thread.CurrentThread.Name + "]: launch reopen thread");
			  }

			  while (System.currentTimeMillis() < StopTime)
			  {
				Thread.Sleep(TestUtil.Next(random(), 1, 100));
				writer.commit();
				Thread.Sleep(TestUtil.Next(random(), 1, 5));
				bool block = random().nextBoolean();
				if (block)
				{
				  OuterInstance.Mgr.maybeRefreshBlocking();
				  OuterInstance.LifetimeMGR.prune(OuterInstance.Pruner);
				}
				else if (OuterInstance.Mgr.maybeRefresh())
				{
				  OuterInstance.LifetimeMGR.prune(OuterInstance.Pruner);
				}
			  }
			}
			catch (Exception t)
			{
			  if (VERBOSE)
			  {
				Console.WriteLine("TEST: reopen thread hit exc");
				t.printStackTrace(System.out);
			  }
			  failed.set(true);
			  throw new Exception(t);
			}
		  }
	  }

	  protected internal override IndexSearcher CurrentSearcher
	  {
		  get
		  {
			if (random().Next(10) == 7)
			{
			  // NOTE: not best practice to call maybeReopen
			  // synchronous to your search threads, but still we
			  // test as apps will presumably do this for
			  // simplicity:
			  if (Mgr.maybeRefresh())
			  {
				LifetimeMGR.prune(Pruner);
			  }
			}
    
			IndexSearcher s = null;
    
			lock (PastSearchers)
			{
			  while (PastSearchers.Count != 0 && random().NextDouble() < 0.25)
			  {
				// 1/4 of the time pull an old searcher, ie, simulate
				// a user doing a follow-on action on a previous
				// search (drilling down/up, clicking next/prev page,
				// etc.)
				long? token = PastSearchers[random().Next(PastSearchers.Count)];
				s = LifetimeMGR.acquire(token);
				if (s == null)
				{
				  // Searcher was pruned
				  PastSearchers.Remove(token);
				}
				else
				{
				  break;
				}
			  }
			}
    
			if (s == null)
			{
			  s = Mgr.acquire();
			  if (s.IndexReader.numDocs() != 0)
			  {
				long? token = LifetimeMGR.record(s);
				lock (PastSearchers)
				{
				  if (!PastSearchers.Contains(token))
				  {
					PastSearchers.Add(token);
				  }
				}
			  }
			}
    
			return s;
		  }
	  }

	  protected internal override void ReleaseSearcher(IndexSearcher s)
	  {
		s.IndexReader.decRef();
	  }

	  protected internal override void DoClose()
	  {
		Assert.IsTrue(WarmCalled);
		if (VERBOSE)
		{
		  Console.WriteLine("TEST: now close SearcherManager");
		}
		Mgr.close();
		LifetimeMGR.close();
	  }

	  public virtual void TestIntermediateClose()
	  {
		Directory dir = newDirectory();
		// Test can deadlock if we use SMS:
		IndexWriter writer = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setMergeScheduler(new ConcurrentMergeScheduler()));
		writer.addDocument(new Document());
		writer.commit();
		CountDownLatch awaitEnterWarm = new CountDownLatch(1);
		CountDownLatch awaitClose = new CountDownLatch(1);
		AtomicBoolean triedReopen = new AtomicBoolean(false);
		ExecutorService es = random().nextBoolean() ? null : Executors.newCachedThreadPool(new NamedThreadFactory("testIntermediateClose"));
		SearcherFactory factory = new SearcherFactoryAnonymousInnerClassHelper2(this, awaitEnterWarm, awaitClose, triedReopen, es);
		SearcherManager searcherManager = random().nextBoolean() ? new SearcherManager(dir, factory) : new SearcherManager(writer, random().nextBoolean(), factory);
		if (VERBOSE)
		{
		  Console.WriteLine("sm created");
		}
		IndexSearcher searcher = searcherManager.acquire();
		try
		{
		  Assert.AreEqual(1, searcher.IndexReader.numDocs());
		}
		finally
		{
		  searcherManager.release(searcher);
		}
		writer.addDocument(new Document());
		writer.commit();
		AtomicBoolean success = new AtomicBoolean(false);
		Exception[] exc = new Exception[1];
		Thread thread = new Thread(new RunnableAnonymousInnerClassHelper(this, triedReopen, searcherManager, success, exc));
		thread.Start();
		if (VERBOSE)
		{
		  Console.WriteLine("THREAD started");
		}
		awaitEnterWarm.@await();
		if (VERBOSE)
		{
		  Console.WriteLine("NOW call close");
		}
		searcherManager.close();
		awaitClose.countDown();
		thread.Join();
		try
		{
		  searcherManager.acquire();
		  Assert.Fail("already closed");
		}
		catch (AlreadyClosedException ex)
		{
		  // expected
		}
		Assert.IsFalse(success.get());
		Assert.IsTrue(triedReopen.get());
		assertNull("" + exc[0], exc[0]);
		writer.close();
		dir.close();
		if (es != null)
		{
		  es.shutdown();
		  es.awaitTermination(1, TimeUnit.SECONDS);
		}
	  }

	  private class SearcherFactoryAnonymousInnerClassHelper2 : SearcherFactory
	  {
		  private readonly TestSearcherManager OuterInstance;

		  private CountDownLatch AwaitEnterWarm;
		  private CountDownLatch AwaitClose;
		  private AtomicBoolean TriedReopen;
		  private ExecutorService Es;

		  public SearcherFactoryAnonymousInnerClassHelper2(TestSearcherManager outerInstance, CountDownLatch awaitEnterWarm, CountDownLatch awaitClose, AtomicBoolean triedReopen, ExecutorService es)
		  {
			  this.OuterInstance = outerInstance;
			  this.AwaitEnterWarm = awaitEnterWarm;
			  this.AwaitClose = awaitClose;
			  this.TriedReopen = triedReopen;
			  this.Es = es;
		  }

		  public override IndexSearcher NewSearcher(IndexReader r)
		  {
			try
			{
			  if (TriedReopen.get())
			  {
				AwaitEnterWarm.countDown();
				AwaitClose.@await();
			  }
			}
			catch (InterruptedException e)
			{
			  //
			}
			return new IndexSearcher(r, Es);
		  }
	  }

	  private class RunnableAnonymousInnerClassHelper : Runnable
	  {
		  private readonly TestSearcherManager OuterInstance;

		  private AtomicBoolean TriedReopen;
		  private SearcherManager SearcherManager;
		  private AtomicBoolean Success;
		  private Exception[] Exc;

		  public RunnableAnonymousInnerClassHelper(TestSearcherManager outerInstance, AtomicBoolean triedReopen, SearcherManager searcherManager, AtomicBoolean success, Exception[] exc)
		  {
			  this.OuterInstance = outerInstance;
			  this.TriedReopen = triedReopen;
			  this.SearcherManager = searcherManager;
			  this.Success = success;
			  this.Exc = exc;
		  }

		  public override void Run()
		  {
			try
			{
			  TriedReopen.set(true);
			  if (VERBOSE)
			  {
				Console.WriteLine("NOW call maybeReopen");
			  }
			  SearcherManager.maybeRefresh();
			  Success.set(true);
			}
			catch (AlreadyClosedException e)
			{
			  // expected
			}
			catch (Exception e)
			{
			  if (VERBOSE)
			  {
				Console.WriteLine("FAIL: unexpected exc");
				e.printStackTrace(System.out);
			  }
			  Exc[0] = e;
			  // use success as the barrier here to make sure we see the write
			  Success.set(false);

			}
		  }
	  }

	  public virtual void TestCloseTwice()
	  {
		// test that we can close SM twice (per IDisposable's contract).
		Directory dir = newDirectory();
		(new IndexWriter(dir, new IndexWriterConfig(TEST_VERSION_CURRENT, null))).close();
		SearcherManager sm = new SearcherManager(dir, null);
		sm.close();
		sm.close();
		dir.close();
	  }

	  public virtual void TestReferenceDecrementIllegally()
	  {
		Directory dir = newDirectory();
		IndexWriter writer = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setMergeScheduler(new ConcurrentMergeScheduler()));
		SearcherManager sm = new SearcherManager(writer, false, new SearcherFactory());
		writer.addDocument(new Document());
		writer.commit();
		sm.maybeRefreshBlocking();

		IndexSearcher acquire = sm.acquire();
		IndexSearcher acquire2 = sm.acquire();
		sm.release(acquire);
		sm.release(acquire2);


		acquire = sm.acquire();
		acquire.IndexReader.decRef();
		sm.release(acquire);
		try
		{
		  sm.acquire();
		  Assert.Fail("acquire should have thrown an IllegalStateException since we modified the refCount outside of the manager");
		}
		catch (IllegalStateException ex)
		{
		  //
		}

		// sm.close(); -- already closed
		writer.close();
		dir.close();
	  }


	  public virtual void TestEnsureOpen()
	  {
		Directory dir = newDirectory();
		(new IndexWriter(dir, new IndexWriterConfig(TEST_VERSION_CURRENT, null))).close();
		SearcherManager sm = new SearcherManager(dir, null);
		IndexSearcher s = sm.acquire();
		sm.close();

		// this should succeed;
		sm.release(s);

		try
		{
		  // this should fail
		  sm.acquire();
		}
		catch (AlreadyClosedException e)
		{
		  // ok
		}

		try
		{
		  // this should fail
		  sm.maybeRefresh();
		}
		catch (AlreadyClosedException e)
		{
		  // ok
		}
		dir.close();
	  }

	  public virtual void TestListenerCalled()
	  {
		Directory dir = newDirectory();
		IndexWriter iw = new IndexWriter(dir, new IndexWriterConfig(TEST_VERSION_CURRENT, null));
		AtomicBoolean afterRefreshCalled = new AtomicBoolean(false);
		SearcherManager sm = new SearcherManager(iw, false, new SearcherFactory());
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
		  private readonly TestSearcherManager OuterInstance;

		  private AtomicBoolean AfterRefreshCalled;

		  public RefreshListenerAnonymousInnerClassHelper(TestSearcherManager outerInstance, AtomicBoolean afterRefreshCalled)
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

	  public virtual void TestEvilSearcherFactory()
	  {
		Random random = random();
		Directory dir = newDirectory();
		RandomIndexWriter w = new RandomIndexWriter(random, dir);
		w.commit();

		IndexReader other = DirectoryReader.open(dir);

		SearcherFactory theEvilOne = new SearcherFactoryAnonymousInnerClassHelper3(this, other);

		try
		{
		  new SearcherManager(dir, theEvilOne);
		}
		catch (IllegalStateException ise)
		{
		  // expected
		}
		try
		{
		  new SearcherManager(w.w, random.nextBoolean(), theEvilOne);
		}
		catch (IllegalStateException ise)
		{
		  // expected
		}
		w.close();
		other.close();
		dir.close();
	  }

	  private class SearcherFactoryAnonymousInnerClassHelper3 : SearcherFactory
	  {
		  private readonly TestSearcherManager OuterInstance;

		  private IndexReader Other;

		  public SearcherFactoryAnonymousInnerClassHelper3(TestSearcherManager outerInstance, IndexReader other)
		  {
			  this.OuterInstance = outerInstance;
			  this.Other = other;
		  }

		  public override IndexSearcher NewSearcher(IndexReader ignored)
		  {
			return LuceneTestCase.newSearcher(Other);
		  }
	  }

	  public virtual void TestMaybeRefreshBlockingLock()
	  {
		// make sure that maybeRefreshBlocking releases the lock, otherwise other
		// threads cannot obtain it.
		Directory dir = newDirectory();
		RandomIndexWriter w = new RandomIndexWriter(random(), dir);
		w.close();

		SearcherManager sm = new SearcherManager(dir, null);

		Thread t = new ThreadAnonymousInnerClassHelper2(this, sm);
		t.Start();
		t.Join();

		// if maybeRefreshBlocking didn't release the lock, this will fail.
		Assert.IsTrue("failde to obtain the refreshLock!", sm.maybeRefresh());

		sm.close();
		dir.close();
	  }

	  private class ThreadAnonymousInnerClassHelper2 : System.Threading.Thread
	  {
		  private readonly TestSearcherManager OuterInstance;

		  private SearcherManager Sm;

		  public ThreadAnonymousInnerClassHelper2(TestSearcherManager outerInstance, SearcherManager sm)
		  {
			  this.OuterInstance = outerInstance;
			  this.Sm = sm;
		  }

		  public override void Run()
		  {
			try
			{
			  // this used to not release the lock, preventing other threads from obtaining it.
			  Sm.maybeRefreshBlocking();
			}
			catch (Exception e)
			{
			  throw new Exception(e);
			}
		  }
	  }

	}

}