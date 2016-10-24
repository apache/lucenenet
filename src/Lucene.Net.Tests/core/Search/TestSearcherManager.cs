using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Lucene.Net.Search
{
    using Index;
    using Lucene.Net.Randomized.Generators;
    using Lucene.Net.Support;
    using NUnit.Framework;
    using AlreadyClosedException = Lucene.Net.Store.AlreadyClosedException;
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
        internal bool WarmCalled;

        private SearcherLifetimeManager.Pruner Pruner;

        [Test]
        public virtual void TestSearcherManager_Mem()
        {
            Pruner = new SearcherLifetimeManager.PruneByAge(TEST_NIGHTLY ? TestUtil.NextInt(Random(), 1, 20) : 1);
            RunTest("TestSearcherManager");
        }

        protected internal override IndexSearcher FinalSearcher
        {
            get
            {
                if (!IsNRT)
                {
                    Writer.Commit();
                }
                Assert.IsTrue(Mgr.MaybeRefresh() || Mgr.SearcherCurrent);
                return Mgr.Acquire();
            }
        }

        private SearcherManager Mgr;
        private SearcherLifetimeManager LifetimeMGR;
        private readonly IList<long> PastSearchers = new List<long>();
        private bool IsNRT;

        protected internal override void DoAfterWriter(TaskScheduler es)
        {
            SearcherFactory factory = new SearcherFactoryAnonymousInnerClassHelper(this, es);
            if (Random().NextBoolean())
            {
                // TODO: can we randomize the applyAllDeletes?  But
                // somehow for final searcher we must apply
                // deletes...
                Mgr = new SearcherManager(Writer, true, factory);
                IsNRT = true;
            }
            else
            {
                // SearcherManager needs to see empty commit:
                Writer.Commit();
                Mgr = new SearcherManager(Dir, factory);
                IsNRT = false;
                AssertMergedSegmentsWarmed = false;
            }

            LifetimeMGR = new SearcherLifetimeManager();
        }

        private class SearcherFactoryAnonymousInnerClassHelper : SearcherFactory
        {
            private readonly TestSearcherManager OuterInstance;

            private TaskScheduler Es;

            public SearcherFactoryAnonymousInnerClassHelper(TestSearcherManager outerInstance, TaskScheduler es)
            {
                this.OuterInstance = outerInstance;
                this.Es = es;
            }

            public override IndexSearcher NewSearcher(IndexReader r)
            {
                IndexSearcher s = new IndexSearcher(r, Es);
                OuterInstance.WarmCalled = true;
                s.Search(new TermQuery(new Term("body", "united")), 10);
                return s;
            }
        }

        protected internal override void DoSearching(TaskScheduler es, DateTime stopTime)
        {
            ThreadClass reopenThread = new ThreadAnonymousInnerClassHelper(this, stopTime);
            reopenThread.SetDaemon(true);
            reopenThread.Start();

            RunSearchThreads(stopTime);

            reopenThread.Join();
        }

        private class ThreadAnonymousInnerClassHelper : ThreadClass
        {
            private readonly TestSearcherManager OuterInstance;

            private DateTime StopTime;

            public ThreadAnonymousInnerClassHelper(TestSearcherManager outerInstance, DateTime stopTime)
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

                    while (DateTime.UtcNow < StopTime)
                    {
                        Thread.Sleep(TestUtil.NextInt(Random(), 1, 100));
                        OuterInstance.Writer.Commit();
                        Thread.Sleep(TestUtil.NextInt(Random(), 1, 5));
                        bool block = Random().NextBoolean();
                        if (block)
                        {
                            OuterInstance.Mgr.MaybeRefreshBlocking();
                            OuterInstance.LifetimeMGR.Prune(OuterInstance.Pruner);
                        }
                        else if (OuterInstance.Mgr.MaybeRefresh())
                        {
                            OuterInstance.LifetimeMGR.Prune(OuterInstance.Pruner);
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
                    OuterInstance.Failed.Set(true);
                    throw new Exception(t.Message, t);
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
                    if (Mgr.MaybeRefresh())
                    {
                        LifetimeMGR.Prune(Pruner);
                    }
                }

                IndexSearcher s = null;

                lock (PastSearchers)
                {
                    while (PastSearchers.Count != 0 && Random().NextDouble() < 0.25)
                    {
                        // 1/4 of the time pull an old searcher, ie, simulate
                        // a user doing a follow-on action on a previous
                        // search (drilling down/up, clicking next/prev page,
                        // etc.)
                        long token = PastSearchers[Random().Next(PastSearchers.Count)];
                        s = LifetimeMGR.Acquire(token);
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
                    s = Mgr.Acquire();
                    if (s.IndexReader.NumDocs != 0)
                    {
                        long token = LifetimeMGR.Record(s);
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
            s.IndexReader.DecRef();
        }

        protected internal override void DoClose()
        {
            Assert.IsTrue(WarmCalled);
            if (VERBOSE)
            {
                Console.WriteLine("TEST: now close SearcherManager");
            }
            Mgr.Dispose();
            LifetimeMGR.Dispose();
        }

        [Test]
        public virtual void TestIntermediateClose()
        {
            Directory dir = NewDirectory();
            // Test can deadlock if we use SMS:
            IndexWriter writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())).SetMergeScheduler(new ConcurrentMergeScheduler()));
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
                Assert.AreEqual(1, searcher.IndexReader.NumDocs);
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
                Assert.Fail("already closed");
            }
            catch (AlreadyClosedException ex)
            {
                // expected
            }
            Assert.IsFalse(success.Get());
            Assert.IsTrue(triedReopen.Get());
            Assert.IsNull(exc[0], "" + exc[0]);
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
            private readonly TestSearcherManager OuterInstance;

            private CountdownEvent AwaitEnterWarm;
            private CountdownEvent AwaitClose;
            private AtomicBoolean TriedReopen;
            private TaskScheduler Es;

            public SearcherFactoryAnonymousInnerClassHelper2(TestSearcherManager outerInstance, CountdownEvent awaitEnterWarm, CountdownEvent awaitClose, AtomicBoolean triedReopen, TaskScheduler es)
            {
                this.OuterInstance = outerInstance;
                this.AwaitEnterWarm = awaitEnterWarm;
                this.AwaitClose = awaitClose;
                this.TriedReopen = triedReopen;
                this.Es = es;
            }

            public override IndexSearcher NewSearcher(IndexReader r)
            {
#if !NETSTANDARD
                try
                {
#endif
                    if (TriedReopen.Get())
                    {
                        AwaitEnterWarm.Signal();
                        AwaitClose.Wait();
                    }
#if !NETSTANDARD
                }
                catch (ThreadInterruptedException e)
                {
                    //
                }
#endif
                return new IndexSearcher(r, Es);
            }
        }

        private class RunnableAnonymousInnerClassHelper : IThreadRunnable
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

            public void Run()
            {
                try
                {
                    TriedReopen.Set(true);
                    if (VERBOSE)
                    {
                        Console.WriteLine("NOW call maybeReopen");
                    }
                    SearcherManager.MaybeRefresh();
                    Success.Set(true);
                }
                catch (AlreadyClosedException)
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
                    Exc[0] = e;
                    // use success as the barrier here to make sure we see the write
                    Success.Set(false);
                }
            }
        }

        [Test]
        public virtual void TestCloseTwice()
        {
            // test that we can close SM twice (per IDisposable's contract).
            Directory dir = NewDirectory();
            (new IndexWriter(dir, new IndexWriterConfig(TEST_VERSION_CURRENT, null))).Dispose();
            SearcherManager sm = new SearcherManager(dir, null);
            sm.Dispose();
            sm.Dispose();
            dir.Dispose();
        }

        [Test]
        public virtual void TestReferenceDecrementIllegally([ValueSource(typeof(ConcurrentMergeSchedulers), "Values")]IConcurrentMergeScheduler scheduler)
        {
            Directory dir = NewDirectory();
            var config = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random()))
                            .SetMergeScheduler(scheduler);
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
            catch (AlreadyClosedException e)
            {
                // ok
            }

            try
            {
                // this should fail
                sm.MaybeRefresh();
            }
            catch (AlreadyClosedException e)
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
            Assert.IsFalse(afterRefreshCalled.Get());
            sm.MaybeRefreshBlocking();
            Assert.IsTrue(afterRefreshCalled.Get());
            sm.Dispose();
            iw.Dispose();
            dir.Dispose();
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

            public void BeforeRefresh()
            {
            }

            public void AfterRefresh(bool didRefresh)
            {
                if (didRefresh)
                {
                    AfterRefreshCalled.Set(true);
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
            catch (InvalidOperationException ise)
            {
                // expected
            }
            try
            {
                new SearcherManager(w.w, random.NextBoolean(), theEvilOne);
            }
            catch (InvalidOperationException ise)
            {
                // expected
            }
            w.Dispose();
            other.Dispose();
            dir.Dispose();
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
                return OuterInstance.NewSearcher(Other);
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
            Assert.IsTrue(sm.MaybeRefresh(), "failde to obtain the refreshLock!");

            sm.Dispose();
            dir.Dispose();
        }

        private class ThreadAnonymousInnerClassHelper2 : ThreadClass
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
                    Sm.MaybeRefreshBlocking();
                }
                catch (Exception e)
                {
                    throw new Exception(e.Message, e);
                }
            }
        }
    }
}