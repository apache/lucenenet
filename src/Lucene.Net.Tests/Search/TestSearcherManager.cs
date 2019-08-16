using NUnit.Framework;
using Lucene.Net.Attributes;
using Lucene.Net.Index;
using Lucene.Net.Randomized.Generators;
using Lucene.Net.Support;
using Lucene.Net.Support.Threading;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Console = Lucene.Net.Support.SystemConsole;

namespace Lucene.Net.Search
{
    using Directory = Lucene.Net.Store.Directory;
    using DirectoryReader = Lucene.Net.Index.DirectoryReader;
    using Document = Documents.Document;
    using IndexReader = Lucene.Net.Index.IndexReader;
    using IndexWriter = Lucene.Net.Index.IndexWriter;
    using IndexWriterConfig = Lucene.Net.Index.IndexWriterConfig;
    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;

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
        public virtual void TestSearcherManager_Mem()
        {
            pruner = new SearcherLifetimeManager.PruneByAge(TEST_NIGHTLY ? TestUtil.NextInt(Random(), 1, 20) : 1);
            RunTest("TestSearcherManager");
        }

        protected internal override IndexSearcher FinalSearcher
        {
            get
            {
                if (!isNRT)
                {
                    writer.Commit();
                }
                assertTrue(mgr.MaybeRefresh() || mgr.IsSearcherCurrent());
                return mgr.Acquire();
            }
        }

        private SearcherManager mgr;
        private SearcherLifetimeManager lifetimeMGR;
        private readonly IList<long> pastSearchers = new List<long>();
        private bool isNRT;

        protected internal override void DoAfterWriter(TaskScheduler es)
        {
            SearcherFactory factory = new SearcherFactoryAnonymousInnerClassHelper(this, es);
            if (Random().NextBoolean())
            {
                // TODO: can we randomize the applyAllDeletes?  But
                // somehow for final searcher we must apply
                // deletes...
                mgr = new SearcherManager(writer, true, factory);
                isNRT = true;
            }
            else
            {
                // SearcherManager needs to see empty commit:
                writer.Commit();
                mgr = new SearcherManager(dir, factory);
                isNRT = false;
                assertMergedSegmentsWarmed = false;
            }

            lifetimeMGR = new SearcherLifetimeManager();
        }

        private class SearcherFactoryAnonymousInnerClassHelper : SearcherFactory
        {
            private readonly TestSearcherManager outerInstance;

            private TaskScheduler es;

            public SearcherFactoryAnonymousInnerClassHelper(TestSearcherManager outerInstance, TaskScheduler es)
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

        protected internal override void DoSearching(TaskScheduler es, long stopTime)
        {
            ThreadClass reopenThread = new ThreadAnonymousInnerClassHelper(this, stopTime);
            reopenThread.SetDaemon(true);
            reopenThread.Start();

            RunSearchThreads(stopTime);

            reopenThread.Join();
        }

        private class ThreadAnonymousInnerClassHelper : ThreadClass
        {
            private readonly TestSearcherManager outerInstance;

            private long stopTime;

            public ThreadAnonymousInnerClassHelper(TestSearcherManager outerInstance, long stopTime)
            {
                this.outerInstance = outerInstance;
                this.stopTime = stopTime;
            }

            public override void Run()
            {
                try
                {
                    if (VERBOSE)
                    {
                        Console.WriteLine("[" + Thread.CurrentThread.Name + "]: launch reopen thread");
                    }

                    while (Environment.TickCount < stopTime)
                    {
                        Thread.Sleep(TestUtil.NextInt(Random(), 1, 100));
                        outerInstance.writer.Commit();
                        Thread.Sleep(TestUtil.NextInt(Random(), 1, 5));
                        bool block = Random().NextBoolean();
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
                catch (Exception t)
                {
                    if (VERBOSE)
                    {
                        Console.WriteLine("TEST: reopen thread hit exc");
                        Console.Out.Write(t.StackTrace);
                    }
                    outerInstance.m_failed.Set(true);
                    throw new Exception(t.ToString(), t);
                }
            }
        }

        protected internal override IndexSearcher CurrentSearcher
        {
            get
            {
                if (Random().Next(10) == 7)
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

                lock (pastSearchers)
                {
                    while (pastSearchers.Count != 0 && Random().NextDouble() < 0.25)
                    {
                        // 1/4 of the time pull an old searcher, ie, simulate
                        // a user doing a follow-on action on a previous
                        // search (drilling down/up, clicking next/prev page,
                        // etc.)
                        long token = pastSearchers[Random().Next(pastSearchers.Count)];
                        s = lifetimeMGR.Acquire(token);
                        if (s == null)
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

                if (s == null)
                {
                    s = mgr.Acquire();
                    if (s.IndexReader.NumDocs != 0)
                    {
                        long token = lifetimeMGR.Record(s);
                        lock (pastSearchers)
                        {
                            if (!pastSearchers.Contains(token))
                            {
                                pastSearchers.Add(token);
                            }
                        }
                    }
                }

                return s;
            }
        }

        protected internal override void ReleaseSearcher(IndexSearcher s)
        {
            s.IndexReader.DecRef();
        }

        protected internal override void DoClose()
        {
            assertTrue(warmCalled);
            if (VERBOSE)
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
            IConcurrentMergeScheduler scheduler;
#if !FEATURE_CONCURRENTMERGESCHEDULER
            scheduler = new TaskMergeScheduler();
#else
            scheduler = new ConcurrentMergeScheduler();
#endif
            IndexWriter writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())).SetMergeScheduler(scheduler));
            writer.AddDocument(new Document());
            writer.Commit();
            CountdownEvent awaitEnterWarm = new CountdownEvent(1);
            CountdownEvent awaitClose = new CountdownEvent(1);
            AtomicBoolean triedReopen = new AtomicBoolean(false);
            //TaskScheduler es = Random().NextBoolean() ? null : Executors.newCachedThreadPool(new NamedThreadFactory("testIntermediateClose"));
            TaskScheduler es = Random().NextBoolean() ? null : TaskScheduler.Default;
            SearcherFactory factory = new SearcherFactoryAnonymousInnerClassHelper2(this, awaitEnterWarm, awaitClose, triedReopen, es);
            SearcherManager searcherManager = Random().NextBoolean() ? new SearcherManager(dir, factory) : new SearcherManager(writer, Random().NextBoolean(), factory);
            if (VERBOSE)
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
            ThreadClass thread = new ThreadClass(() => new RunnableAnonymousInnerClassHelper(this, triedReopen, searcherManager, success, exc).Run());
            thread.Start();
            if (VERBOSE)
            {
                Console.WriteLine("THREAD started");
            }
            awaitEnterWarm.Wait();
            if (VERBOSE)
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
#pragma warning disable 168
            catch (ObjectDisposedException ex)
#pragma warning restore 168
            {
                // expected
            }
            assertFalse(success.Get());
            assertTrue(triedReopen.Get());
            assertNull("" + exc[0], exc[0]);
            writer.Dispose();
            dir.Dispose();
            //if (es != null)
            //{
            //    es.shutdown();
            //    es.awaitTermination(1, TimeUnit.SECONDS);
            //}
        }

        private class SearcherFactoryAnonymousInnerClassHelper2 : SearcherFactory
        {
            private readonly TestSearcherManager outerInstance;

            private CountdownEvent awaitEnterWarm;
            private CountdownEvent awaitClose;
            private AtomicBoolean triedReopen;
            private TaskScheduler es;

            public SearcherFactoryAnonymousInnerClassHelper2(TestSearcherManager outerInstance, CountdownEvent awaitEnterWarm, CountdownEvent awaitClose, AtomicBoolean triedReopen, TaskScheduler es)
            {
                this.outerInstance = outerInstance;
                this.awaitEnterWarm = awaitEnterWarm;
                this.awaitClose = awaitClose;
                this.triedReopen = triedReopen;
                this.es = es;
            }

            public override IndexSearcher NewSearcher(IndexReader r)
            {
#if !NETSTANDARD1_6
                try
                {
#endif
                    if (triedReopen.Get())
                    {
                        awaitEnterWarm.Signal();
                        awaitClose.Wait();
                    }
#if !NETSTANDARD1_6
                }
#pragma warning disable 168
                catch (ThreadInterruptedException e)
#pragma warning restore 168
                {
                    //
                }
#endif
                return new IndexSearcher(r, es);
            }
        }

        private class RunnableAnonymousInnerClassHelper : IThreadRunnable
        {
            private readonly TestSearcherManager outerInstance;

            private AtomicBoolean triedReopen;
            private SearcherManager searcherManager;
            private AtomicBoolean success;
            private Exception[] exc;

            public RunnableAnonymousInnerClassHelper(TestSearcherManager outerInstance, AtomicBoolean triedReopen, SearcherManager searcherManager, AtomicBoolean success, Exception[] exc)
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
                    triedReopen.Set(true);
                    if (VERBOSE)
                    {
                        Console.WriteLine("NOW call maybeReopen");
                    }
                    searcherManager.MaybeRefresh();
                    success.Set(true);
                }
                catch (ObjectDisposedException)
                {
                    // expected
                }
                catch (Exception e)
                {
                    if (VERBOSE)
                    {
                        Console.WriteLine("FAIL: unexpected exc");
                        Console.Out.Write(e.StackTrace);
                    }
                    exc[0] = e;
                    // use success as the barrier here to make sure we see the write
                    success.Set(false);
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
        public virtual void TestReferenceDecrementIllegally([ValueSource(typeof(ConcurrentMergeSchedulerFactories), "Values")]Func<IConcurrentMergeScheduler> newScheduler)
        {
            Directory dir = NewDirectory();
            var config = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random()))
                            .SetMergeScheduler(newScheduler());
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

            Assert.Throws<InvalidOperationException>(() => sm.Acquire(), "acquire should have thrown an InvalidOperationException since we modified the refCount outside of the manager");

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
#pragma warning disable 168
            catch (ObjectDisposedException e)
#pragma warning restore 168
            {
                // ok
            }

            try
            {
                // this should fail
                sm.MaybeRefresh();
            }
#pragma warning disable 168
            catch (ObjectDisposedException e)
#pragma warning restore 168
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
            sm.AddListener(new RefreshListenerAnonymousInnerClassHelper(this, afterRefreshCalled));
            iw.AddDocument(new Document());
            iw.Commit();
            assertFalse(afterRefreshCalled.Get());
            sm.MaybeRefreshBlocking();
            assertTrue(afterRefreshCalled.Get());
            sm.Dispose();
            iw.Dispose();
            dir.Dispose();
        }

        private class RefreshListenerAnonymousInnerClassHelper : ReferenceManager.IRefreshListener
        {
            private readonly TestSearcherManager outerInstance;

            private AtomicBoolean afterRefreshCalled;

            public RefreshListenerAnonymousInnerClassHelper(TestSearcherManager outerInstance, AtomicBoolean afterRefreshCalled)
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
                    afterRefreshCalled.Set(true);
                }
            }
        }

        [Test]
        public virtual void TestEvilSearcherFactory()
        {
            Random random = Random();
            Directory dir = NewDirectory();
            RandomIndexWriter w = new RandomIndexWriter(random, dir, Similarity, TimeZone);
            w.Commit();

            IndexReader other = DirectoryReader.Open(dir);

            SearcherFactory theEvilOne = new SearcherFactoryAnonymousInnerClassHelper3(this, other);

            try
            {
                new SearcherManager(dir, theEvilOne);
            }
#pragma warning disable 168
            catch (InvalidOperationException ise)
#pragma warning restore 168
            {
                // expected
            }
            try
            {
                new SearcherManager(w.w, random.NextBoolean(), theEvilOne);
            }
#pragma warning disable 168
            catch (InvalidOperationException ise)
#pragma warning restore 168
            {
                // expected
            }
            w.Dispose();
            other.Dispose();
            dir.Dispose();
        }

        private class SearcherFactoryAnonymousInnerClassHelper3 : SearcherFactory
        {
            private readonly TestSearcherManager outerInstance;

            private IndexReader other;

            public SearcherFactoryAnonymousInnerClassHelper3(TestSearcherManager outerInstance, IndexReader other)
            {
                this.outerInstance = outerInstance;
                this.other = other;
            }

            public override IndexSearcher NewSearcher(IndexReader ignored)
            {
                return outerInstance.NewSearcher(other);
            }
        }

        [Test]
        public virtual void TestMaybeRefreshBlockingLock()
        {
            // make sure that maybeRefreshBlocking releases the lock, otherwise other
            // threads cannot obtain it.
            Directory dir = NewDirectory();
            RandomIndexWriter w = new RandomIndexWriter(Random(), dir, Similarity, TimeZone);
            w.Dispose();

            SearcherManager sm = new SearcherManager(dir, null);

            ThreadClass t = new ThreadAnonymousInnerClassHelper2(this, sm);
            t.Start();
            t.Join();

            // if maybeRefreshBlocking didn't release the lock, this will fail.
            assertTrue("failde to obtain the refreshLock!", sm.MaybeRefresh());

            sm.Dispose();
            dir.Dispose();
        }

        private class ThreadAnonymousInnerClassHelper2 : ThreadClass
        {
            private readonly TestSearcherManager outerInstance;

            private SearcherManager sm;

            public ThreadAnonymousInnerClassHelper2(TestSearcherManager outerInstance, SearcherManager sm)
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
                catch (Exception e)
                {
                    throw new Exception(e.ToString(), e);
                }
            }
        }
    }
}