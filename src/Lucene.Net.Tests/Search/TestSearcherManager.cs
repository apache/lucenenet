using J2N.Threading;
using J2N.Threading.Atomic;
using Lucene.Net.Index;
using Lucene.Net.Index.Extensions;
using Lucene.Net.Support.Threading;
using NUnit.Framework;
using RandomizedTesting.Generators;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using JCG = J2N.Collections.Generic;
using Console = Lucene.Net.Util.SystemConsole;

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

    using Directory = Lucene.Net.Store.Directory;
    using DirectoryReader = Lucene.Net.Index.DirectoryReader;
    using Document = Documents.Document;
    using IndexReader = Lucene.Net.Index.IndexReader;
    using IndexWriter = Lucene.Net.Index.IndexWriter;
    using IndexWriterConfig = Lucene.Net.Index.IndexWriterConfig;
    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
    using MockAnalyzer = Lucene.Net.Analysis.MockAnalyzer;
    using RandomIndexWriter = Lucene.Net.Index.RandomIndexWriter;
    using Term = Lucene.Net.Index.Term;
    using TestUtil = Lucene.Net.Util.TestUtil;
    using ThreadedIndexingAndSearchingTestCase = Lucene.Net.Index.ThreadedIndexingAndSearchingTestCase;

    [SuppressCodecs("SimpleText", "Memory", "Direct")]
    [TestFixture]
    public class TestSearcherManager : ThreadedIndexingAndSearchingTestCase
    {
        internal bool warmCalled;

        private SearcherLifetimeManager.IPruner pruner;

        [Test]
        [Slow]
        public virtual void TestSearcherManager_Mem()
        {
            pruner = new SearcherLifetimeManager.PruneByAge(TestNightly ? TestUtil.NextInt32(Random, 1, 20) : 1);
            RunTest("TestSearcherManager");
        }

        protected override IndexSearcher GetFinalSearcher()
        {
            if (!isNRT)
            {
                m_writer.Commit();
            }
            assertTrue(mgr.MaybeRefresh() || mgr.IsSearcherCurrent());
            return mgr.Acquire();
        }

        private SearcherManager mgr;
        private SearcherLifetimeManager lifetimeMGR;
        private readonly IList<long> pastSearchers = new JCG.List<long>();
        private bool isNRT;

        protected override void DoAfterWriter(TaskScheduler es)
        {
            SearcherFactory factory = new SearcherFactoryAnonymousClass(this, es);
            if (Random.NextBoolean())
            {
                // TODO: can we randomize the applyAllDeletes?  But
                // somehow for final searcher we must apply
                // deletes...
                mgr = new SearcherManager(m_writer, true, factory);
                isNRT = true;
            }
            else
            {
                // SearcherManager needs to see empty commit:
                m_writer.Commit();
                mgr = new SearcherManager(m_dir, factory);
                isNRT = false;
                m_assertMergedSegmentsWarmed = false;
            }

            lifetimeMGR = new SearcherLifetimeManager();
        }

        private sealed class SearcherFactoryAnonymousClass : SearcherFactory
        {
            private readonly TestSearcherManager outerInstance;

            private TaskScheduler es;

            public SearcherFactoryAnonymousClass(TestSearcherManager outerInstance, TaskScheduler es)
            {
                this.outerInstance = outerInstance;
                this.es = es;
            }

            public override IndexSearcher NewSearcher(IndexReader r)
            {
                IndexSearcher s = new IndexSearcher(r, es);
                outerInstance.warmCalled = true;
                s.Search(new TermQuery(new Term("body", "united")), 10);
                return s;
            }
        }

        protected override void DoSearching(TaskScheduler es, long stopTime)
        {
            ThreadJob reopenThread = new ThreadAnonymousClass(this, stopTime);
            reopenThread.IsBackground = (true);
            reopenThread.Start();

            RunSearchThreads(stopTime);

            reopenThread.Join();
        }

        private sealed class ThreadAnonymousClass : ThreadJob
        {
            private readonly TestSearcherManager outerInstance;

            private long stopTime;

            public ThreadAnonymousClass(TestSearcherManager outerInstance, long stopTime)
            {
                this.outerInstance = outerInstance;
                this.stopTime = stopTime;
            }

            public override void Run()
            {
                try
                {
                    if (Verbose)
                    {
                        Console.WriteLine("[" + Thread.CurrentThread.Name + "]: launch reopen thread");
                    }

                    while (J2N.Time.NanoTime() / J2N.Time.MillisecondsPerNanosecond < stopTime) // LUCENENET: Use NanoTime() rather than CurrentTimeMilliseconds() for more accurate/reliable results
                    {
                        Thread.Sleep(TestUtil.NextInt32(Random, 1, 100));
                        outerInstance.m_writer.Commit();
                        Thread.Sleep(TestUtil.NextInt32(Random, 1, 5));
                        bool block = Random.NextBoolean();
                        if (block)
                        {
                            outerInstance.mgr.MaybeRefreshBlocking();
                            outerInstance.lifetimeMGR.Prune(outerInstance.pruner);
                        }
                        else if (outerInstance.mgr.MaybeRefresh())
                        {
                            outerInstance.lifetimeMGR.Prune(outerInstance.pruner);
                        }
                    }
                }
                catch (Exception t) when (t.IsThrowable())
                {
                    if (Verbose)
                    {
                        Console.WriteLine("TEST: reopen thread hit exc");
                        Console.Out.Write(t.StackTrace);
                    }
                    outerInstance.m_failed.Value = (true);
                    throw RuntimeException.Create(t);
                }
            }
        }

        protected override IndexSearcher GetCurrentSearcher()
        {
            if (Random.Next(10) == 7)
            {
                // NOTE: not best practice to call maybeReopen
                // synchronous to your search threads, but still we
                // test as apps will presumably do this for
                // simplicity:
                if (mgr.MaybeRefresh())
                {
                    lifetimeMGR.Prune(pruner);
                }
            }

            IndexSearcher s = null;

            UninterruptableMonitor.Enter(pastSearchers);
            try
            {
                while (pastSearchers.Count != 0 && Random.NextDouble() < 0.25)
                {
                    // 1/4 of the time pull an old searcher, ie, simulate
                    // a user doing a follow-on action on a previous
                    // search (drilling down/up, clicking next/prev page,
                    // etc.)
                    long token = pastSearchers[Random.Next(pastSearchers.Count)];
                    s = lifetimeMGR.Acquire(token);
                    if (s is null)
                    {
                        // Searcher was pruned
                        pastSearchers.Remove(token);
                    }
                    else
                    {
                        break;
                    }
                }
            }
            finally
            {
                UninterruptableMonitor.Exit(pastSearchers);
            }

            if (s is null)
            {
                s = mgr.Acquire();
                if (s.IndexReader.NumDocs != 0)
                {
                    long token = lifetimeMGR.Record(s);
                    UninterruptableMonitor.Enter(pastSearchers);
                    try
                    {
                        if (!pastSearchers.Contains(token))
                        {
                            pastSearchers.Add(token);
                        }
                    }
                    finally
                    {
                        UninterruptableMonitor.Exit(pastSearchers);
                    }
                }
            }

            return s;
        }

        protected override void ReleaseSearcher(IndexSearcher s)
        {
            s.IndexReader.DecRef();
        }

        protected override void DoClose()
        {
            assertTrue(warmCalled);
            if (Verbose)
            {
                Console.WriteLine("TEST: now close SearcherManager");
            }
            mgr.Dispose();
            lifetimeMGR.Dispose();
        }

        [Test]
        public virtual void TestIntermediateClose()
        {
            Directory dir = NewDirectory();
            // Test can deadlock if we use SMS:
            IndexWriter writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)).SetMergeScheduler(new ConcurrentMergeScheduler()));
            writer.AddDocument(new Document());
            writer.Commit();
            CountdownEvent awaitEnterWarm = new CountdownEvent(1);
            CountdownEvent awaitClose = new CountdownEvent(1);
            AtomicBoolean triedReopen = new AtomicBoolean(false);
            //TaskScheduler es = Random().NextBoolean() ? null : Executors.newCachedThreadPool(new NamedThreadFactory("testIntermediateClose"));
            TaskScheduler es = Random.NextBoolean() ? null : TaskScheduler.Default;
            SearcherFactory factory = new SearcherFactoryAnonymousClass2(this, awaitEnterWarm, awaitClose, triedReopen, es);
            SearcherManager searcherManager = Random.NextBoolean() ? new SearcherManager(dir, factory) : new SearcherManager(writer, Random.NextBoolean(), factory);
            if (Verbose)
            {
                Console.WriteLine("sm created");
            }
            IndexSearcher searcher = searcherManager.Acquire();
            try
            {
                assertEquals(1, searcher.IndexReader.NumDocs);
            }
            finally
            {
                searcherManager.Release(searcher);
            }
            writer.AddDocument(new Document());
            writer.Commit();
            AtomicBoolean success = new AtomicBoolean(false);
            Exception[] exc = new Exception[1];
            ThreadJob thread = new ThreadJob(() => new RunnableAnonymousClass(this, triedReopen, searcherManager, success, exc).Run());
            thread.Start();
            if (Verbose)
            {
                Console.WriteLine("THREAD started");
            }
            awaitEnterWarm.Wait();
            if (Verbose)
            {
                Console.WriteLine("NOW call close");
            }
            searcherManager.Dispose();
            awaitClose.Signal();
            thread.Join();
            try
            {
                searcherManager.Acquire();
                fail("already closed");
            }
            catch (Exception ex) when (ex.IsAlreadyClosedException())
            {
                // expected
            }
            assertFalse(success);
            assertTrue(triedReopen);
            assertNull("" + exc[0], exc[0]);
            writer.Dispose();
            dir.Dispose();
            //if (es != null)
            //{
            //    es.shutdown();
            //    es.awaitTermination(1, TimeUnit.SECONDS);
            //}
        }

        private sealed class SearcherFactoryAnonymousClass2 : SearcherFactory
        {
            private readonly TestSearcherManager outerInstance;

            private CountdownEvent awaitEnterWarm;
            private CountdownEvent awaitClose;
            private AtomicBoolean triedReopen;
            private TaskScheduler es;

            public SearcherFactoryAnonymousClass2(TestSearcherManager outerInstance, CountdownEvent awaitEnterWarm, CountdownEvent awaitClose, AtomicBoolean triedReopen, TaskScheduler es)
            {
                this.outerInstance = outerInstance;
                this.awaitEnterWarm = awaitEnterWarm;
                this.awaitClose = awaitClose;
                this.triedReopen = triedReopen;
                this.es = es;
            }

            public override IndexSearcher NewSearcher(IndexReader r)
            {
                try
                {
                    if (triedReopen)
                    {
                        awaitEnterWarm.Signal();
                        awaitClose.Wait();
                    }
                }
                catch (Exception e) when (e.IsInterruptedException())
                {
                    //
                }
                return new IndexSearcher(r, es);
            }
        }

        private sealed class RunnableAnonymousClass //: IThreadRunnable
        {
            private readonly TestSearcherManager outerInstance;

            private AtomicBoolean triedReopen;
            private SearcherManager searcherManager;
            private AtomicBoolean success;
            private Exception[] exc;

            public RunnableAnonymousClass(TestSearcherManager outerInstance, AtomicBoolean triedReopen, SearcherManager searcherManager, AtomicBoolean success, Exception[] exc)
            {
                this.outerInstance = outerInstance;
                this.triedReopen = triedReopen;
                this.searcherManager = searcherManager;
                this.success = success;
                this.exc = exc;
            }

            public void Run()
            {
                try
                {
                    triedReopen.Value = (true);
                    if (Verbose)
                    {
                        Console.WriteLine("NOW call maybeReopen");
                    }
                    searcherManager.MaybeRefresh();
                    success.Value = (true);
                }
                catch (Exception e) when (e.IsAlreadyClosedException())
                {
                    // expected
                }
                catch (Exception e) when (e.IsThrowable())
                {
                    if (Verbose)
                    {
                        Console.WriteLine("FAIL: unexpected exc");
                        Console.Out.Write(e.StackTrace);
                    }
                    exc[0] = e;
                    // use success as the barrier here to make sure we see the write
                    success.Value = (false);
                }
            }
        }

        [Test]
        public virtual void TestCloseTwice()
        {
            // test that we can close SM twice (per IDisposable's contract).
            Directory dir = NewDirectory();
            using (var iw = new IndexWriter(dir, new IndexWriterConfig(TEST_VERSION_CURRENT, null))) { }
            SearcherManager sm = new SearcherManager(dir, null);
            sm.Dispose();
            sm.Dispose();
            dir.Dispose();
        }

        [Test]
        public virtual void TestReferenceDecrementIllegally()
        {
            Directory dir = NewDirectory();
            var config = NewIndexWriterConfig(
                TEST_VERSION_CURRENT, new MockAnalyzer(Random)).SetMergeScheduler(new ConcurrentMergeScheduler());
            IndexWriter writer = new IndexWriter(dir, config);
            SearcherManager sm = new SearcherManager(writer, false, new SearcherFactory());
            writer.AddDocument(new Document());
            writer.Commit();
            sm.MaybeRefreshBlocking();

            IndexSearcher acquire = sm.Acquire();
            IndexSearcher acquire2 = sm.Acquire();
            sm.Release(acquire);
            sm.Release(acquire2);

            acquire = sm.Acquire();
            acquire.IndexReader.DecRef();
            sm.Release(acquire);

            try
            {
                sm.Acquire();
                fail("acquire should have thrown an InvalidOperationException since we modified the refCount outside of the manager");
            }
            catch (Exception ise) when (ise.IsIllegalStateException())
            {
                // expected
            }

            // sm.Dispose(); -- already closed
            writer.Dispose();
            dir.Dispose();
        }

        [Test]
        public virtual void TestEnsureOpen()
        {
            Directory dir = NewDirectory();
            (new IndexWriter(dir, new IndexWriterConfig(TEST_VERSION_CURRENT, null))).Dispose();
            SearcherManager sm = new SearcherManager(dir, null);
            IndexSearcher s = sm.Acquire();
            sm.Dispose();

            // this should succeed;
            sm.Release(s);

            try
            {
                // this should fail
                sm.Acquire();
            }
            catch (Exception e) when (e.IsAlreadyClosedException())
            {
                // ok
            }

            try
            {
                // this should fail
                sm.MaybeRefresh();
            }
            catch (Exception e) when (e.IsAlreadyClosedException())
            {
                // ok
            }
            dir.Dispose();
        }

        [Test]
        public virtual void TestListenerCalled()
        {
            Directory dir = NewDirectory();
            IndexWriter iw = new IndexWriter(dir, new IndexWriterConfig(TEST_VERSION_CURRENT, null));
            AtomicBoolean afterRefreshCalled = new AtomicBoolean(false);
            SearcherManager sm = new SearcherManager(iw, false, new SearcherFactory());
            sm.AddListener(new RefreshListenerAnonymousClass(this, afterRefreshCalled));
            iw.AddDocument(new Document());
            iw.Commit();
            assertFalse(afterRefreshCalled);
            sm.MaybeRefreshBlocking();
            assertTrue(afterRefreshCalled);
            sm.Dispose();
            iw.Dispose();
            dir.Dispose();
        }

        private sealed class RefreshListenerAnonymousClass : ReferenceManager.IRefreshListener
        {
            private readonly TestSearcherManager outerInstance;

            private AtomicBoolean afterRefreshCalled;

            public RefreshListenerAnonymousClass(TestSearcherManager outerInstance, AtomicBoolean afterRefreshCalled)
            {
                this.outerInstance = outerInstance;
                this.afterRefreshCalled = afterRefreshCalled;
            }

            public void BeforeRefresh()
            {
            }

            public void AfterRefresh(bool didRefresh)
            {
                if (didRefresh)
                {
                    afterRefreshCalled.Value = (true);
                }
            }
        }

        [Test]
        public virtual void TestEvilSearcherFactory()
        {
            Random random = Random;
            Directory dir = NewDirectory();
            RandomIndexWriter w = new RandomIndexWriter(random, dir);
            w.Commit();

            IndexReader other = DirectoryReader.Open(dir);

            SearcherFactory theEvilOne = new SearcherFactoryAnonymousClass3(this, other);

            try
            {
                new SearcherManager(dir, theEvilOne);
            }
            catch (Exception ise) when (ise.IsIllegalStateException())
            {
                // expected
            }
            try
            {
                new SearcherManager(w.IndexWriter, random.NextBoolean(), theEvilOne);
            }
            catch (Exception ise) when (ise.IsIllegalStateException())
            {
                // expected
            }
            w.Dispose();
            other.Dispose();
            dir.Dispose();
        }

        private sealed class SearcherFactoryAnonymousClass3 : SearcherFactory
        {
            private readonly TestSearcherManager outerInstance;

            private IndexReader other;

            public SearcherFactoryAnonymousClass3(TestSearcherManager outerInstance, IndexReader other)
            {
                this.outerInstance = outerInstance;
                this.other = other;
            }

            public override IndexSearcher NewSearcher(IndexReader ignored)
            {
                return LuceneTestCase.NewSearcher(other);
            }
        }

        [Test]
        public virtual void TestMaybeRefreshBlockingLock()
        {
            // make sure that maybeRefreshBlocking releases the lock, otherwise other
            // threads cannot obtain it.
            Directory dir = NewDirectory();
            RandomIndexWriter w = new RandomIndexWriter(Random, dir);
            w.Dispose();

            SearcherManager sm = new SearcherManager(dir, null);

            ThreadJob t = new ThreadAnonymousClass2(this, sm);
            t.Start();
            t.Join();

            // if maybeRefreshBlocking didn't release the lock, this will fail.
            assertTrue("failde to obtain the refreshLock!", sm.MaybeRefresh());

            sm.Dispose();
            dir.Dispose();
        }

        private sealed class ThreadAnonymousClass2 : ThreadJob
        {
            private readonly TestSearcherManager outerInstance;

            private SearcherManager sm;

            public ThreadAnonymousClass2(TestSearcherManager outerInstance, SearcherManager sm)
            {
                this.outerInstance = outerInstance;
                this.sm = sm;
            }

            public override void Run()
            {
                try
                {
                    // this used to not release the lock, preventing other threads from obtaining it.
                    sm.MaybeRefreshBlocking();
                }
                catch (Exception e) when (e.IsException())
                {
                    throw RuntimeException.Create(e);
                }
            }
        }
    }
}