using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Lucene.Net.Randomized.Generators;
using Lucene.Net.Support;
using NUnit.Framework;

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
    using Document = Lucene.Net.Documents.Document;
    using Field = Lucene.Net.Documents.Field;
    using TextField = Lucene.Net.Documents.TextField;
    using DirectoryReader = Lucene.Net.Index.DirectoryReader;
    using IndexCommit = Lucene.Net.Index.IndexCommit;
    using IndexReader = Lucene.Net.Index.IndexReader;
    using IndexWriter = Lucene.Net.Index.IndexWriter;
    using IndexWriterConfig = Lucene.Net.Index.IndexWriterConfig;
    using IIndexableField = Lucene.Net.Index.IIndexableField;
    using KeepOnlyLastCommitDeletionPolicy = Lucene.Net.Index.KeepOnlyLastCommitDeletionPolicy;
    using NoMergePolicy = Lucene.Net.Index.NoMergePolicy;
    using OpenMode = Lucene.Net.Index.OpenMode;
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
    //using ThreadInterruptedException = Lucene.Net.Util.ThreadInterruptedException;
    using Version = Lucene.Net.Util.LuceneVersion;

    [SuppressCodecs("SimpleText", "Memory", "Direct")]
    [TestFixture]
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

        [Test]
        public virtual void TestControlledRealTimeReopenThread_Mem()
        {
            RunTest("TestControlledRealTimeReopenThread");
        }

        protected internal override IndexSearcher FinalSearcher
        {
            get
            {
                if (VERBOSE)
                {
                    Console.WriteLine("TEST: finalSearcher maxGen=" + MaxGen);
                }
                NrtDeletesThread.WaitForGeneration(MaxGen);
                return NrtDeletes.Acquire();
            }
        }

        protected internal override Directory GetDirectory(Directory @in)
        {
            // Randomly swap in NRTCachingDir
            if (Random().NextBoolean())
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

        protected internal override void UpdateDocuments(Term id, IEnumerable<IEnumerable<IIndexableField>> docs)
        {
            long gen = GenWriter.UpdateDocuments(id, docs);

            // Randomly verify the update "took":
            if (Random().Next(20) == 2)
            {
                if (VERBOSE)
                {
                    Console.WriteLine(Thread.CurrentThread.Name + ": nrt: verify " + id);
                }
                NrtDeletesThread.WaitForGeneration(gen);
                IndexSearcher s = NrtDeletes.Acquire();
                if (VERBOSE)
                {
                    Console.WriteLine(Thread.CurrentThread.Name + ": nrt: got searcher=" + s);
                }
                try
                {
                    Assert.AreEqual(docs.Count(), s.Search(new TermQuery(id), 10).TotalHits);
                }
                finally
                {
                    NrtDeletes.Release(s);
                }
            }

            LastGens.Value = gen;

        }

        protected internal override void AddDocuments(Term id, IEnumerable<IEnumerable<IIndexableField>> docs)
        {
            long gen = GenWriter.AddDocuments(docs);
            // Randomly verify the add "took":
            if (Random().Next(20) == 2)
            {
                if (VERBOSE)
                {
                    Console.WriteLine(Thread.CurrentThread.Name + ": nrt: verify " + id);
                }
                NrtNoDeletesThread.WaitForGeneration(gen);
                IndexSearcher s = NrtNoDeletes.Acquire();
                if (VERBOSE)
                {
                    Console.WriteLine(Thread.CurrentThread.Name + ": nrt: got searcher=" + s);
                }
                try
                {
                    Assert.AreEqual(docs.Count(), s.Search(new TermQuery(id), 10).TotalHits);
                }
                finally
                {
                    NrtNoDeletes.Release(s);
                }
            }
            LastGens.Value = gen;
        }

        protected internal override void AddDocument(Term id, IEnumerable<IIndexableField> doc)
        {
            long gen = GenWriter.AddDocument(doc);

            // Randomly verify the add "took":
            if (Random().Next(20) == 2)
            {
                if (VERBOSE)
                {
                    Console.WriteLine(Thread.CurrentThread.Name + ": nrt: verify " + id);
                }
                NrtNoDeletesThread.WaitForGeneration(gen);
                IndexSearcher s = NrtNoDeletes.Acquire();
                if (VERBOSE)
                {
                    Console.WriteLine(Thread.CurrentThread.Name + ": nrt: got searcher=" + s);
                }
                try
                {
                    Assert.AreEqual(1, s.Search(new TermQuery(id), 10).TotalHits);
                }
                finally
                {
                    NrtNoDeletes.Release(s);
                }
            }
            LastGens.Value = gen;
        }

        protected internal override void UpdateDocument(Term id, IEnumerable<IIndexableField> doc)
        {
            long gen = GenWriter.UpdateDocument(id, doc);
            // Randomly verify the udpate "took":
            if (Random().Next(20) == 2)
            {
                if (VERBOSE)
                {
                    Console.WriteLine(Thread.CurrentThread.Name + ": nrt: verify " + id);
                }
                NrtDeletesThread.WaitForGeneration(gen);
                IndexSearcher s = NrtDeletes.Acquire();
                if (VERBOSE)
                {
                    Console.WriteLine(Thread.CurrentThread.Name + ": nrt: got searcher=" + s);
                }
                try
                {
                    Assert.AreEqual(1, s.Search(new TermQuery(id), 10).TotalHits);
                }
                finally
                {
                    NrtDeletes.Release(s);
                }
            }
            LastGens.Value = gen;
        }

        protected internal override void DeleteDocuments(Term id)
        {
            long gen = GenWriter.DeleteDocuments(id);
            // randomly verify the delete "took":
            if (Random().Next(20) == 7)
            {
                if (VERBOSE)
                {
                    Console.WriteLine(Thread.CurrentThread.Name + ": nrt: verify del " + id);
                }
                NrtDeletesThread.WaitForGeneration(gen);
                IndexSearcher s = NrtDeletes.Acquire();
                if (VERBOSE)
                {
                    Console.WriteLine(Thread.CurrentThread.Name + ": nrt: got searcher=" + s);
                }
                try
                {
                    Assert.AreEqual(0, s.Search(new TermQuery(id), 10).TotalHits);
                }
                finally
                {
                    NrtDeletes.Release(s);
                }
            }
            LastGens.Value = gen;
        }

        protected internal override void DoAfterWriter(TaskScheduler es)
        {
            double minReopenSec = 0.01 + 0.05 * Random().NextDouble();
            double maxReopenSec = minReopenSec * (1.0 + 10 * Random().NextDouble());

            if (VERBOSE)
            {
                Console.WriteLine("TEST: make SearcherManager maxReopenSec=" + maxReopenSec + " minReopenSec=" + minReopenSec);
            }

            GenWriter = new TrackingIndexWriter(Writer);

            SearcherFactory sf = new SearcherFactoryAnonymousInnerClassHelper(this, es);

            NrtNoDeletes = new SearcherManager(Writer, false, sf);
            NrtDeletes = new SearcherManager(Writer, true, sf);

            NrtDeletesThread = new ControlledRealTimeReopenThread<IndexSearcher>(GenWriter, NrtDeletes, maxReopenSec, minReopenSec);
            NrtDeletesThread.Name = "NRTDeletes Reopen Thread";
            NrtDeletesThread.Priority = (ThreadPriority)Math.Min((int)Thread.CurrentThread.Priority + 2, (int)ThreadPriority.Highest);
            NrtDeletesThread.SetDaemon(true);
            NrtDeletesThread.Start();

            NrtNoDeletesThread = new ControlledRealTimeReopenThread<IndexSearcher>(GenWriter, NrtNoDeletes, maxReopenSec, minReopenSec);
            NrtNoDeletesThread.Name = "NRTNoDeletes Reopen Thread";
            NrtNoDeletesThread.Priority = (ThreadPriority)Math.Min((int)Thread.CurrentThread.Priority + 2, (int)ThreadPriority.Highest);
            NrtNoDeletesThread.SetDaemon(true);
            NrtNoDeletesThread.Start();
        }

        private class SearcherFactoryAnonymousInnerClassHelper : SearcherFactory
        {
            private readonly TestControlledRealTimeReopenThread OuterInstance;

            private TaskScheduler Es;

            public SearcherFactoryAnonymousInnerClassHelper(TestControlledRealTimeReopenThread outerInstance, TaskScheduler es)
            {
                this.OuterInstance = outerInstance;
                this.Es = es;
            }

            public override IndexSearcher NewSearcher(IndexReader r)
            {
                OuterInstance.WarmCalled = true;
                IndexSearcher s = new IndexSearcher(r, Es);
                s.Search(new TermQuery(new Term("body", "united")), 10);
                return s;
            }
        }

        protected internal override void DoAfterIndexingThreadDone()
        {
            long? gen = LastGens.Value;
            if (gen != null)
            {
                AddMaxGen((long)gen);
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

        protected internal override void DoSearching(TaskScheduler es, DateTime stopTime)
        {
            RunSearchThreads(stopTime);
        }

        protected internal override IndexSearcher CurrentSearcher
        {
            get
            {
                // Test doesn't assert deletions until the end, so we
                // can randomize whether dels must be applied
                SearcherManager nrt;
                if (Random().NextBoolean())
                {
                    nrt = NrtDeletes;
                }
                else
                {
                    nrt = NrtNoDeletes;
                }

                return nrt.Acquire();
            }
        }

        protected internal override void ReleaseSearcher(IndexSearcher s)
        {
            // NOTE: a bit iffy... technically you should release
            // against the same SearcherManager you acquired from... but
            // both impls just decRef the underlying reader so we
            // can get away w/ cheating:
            NrtNoDeletes.Release(s);
        }

        protected internal override void DoClose()
        {
            Assert.IsTrue(WarmCalled);
            if (VERBOSE)
            {
                Console.WriteLine("TEST: now close SearcherManagers");
            }
            NrtDeletesThread.Dispose();
            NrtDeletes.Dispose();
            NrtNoDeletesThread.Dispose();
            NrtNoDeletes.Dispose();
        }

        /*
         * LUCENE-3528 - NRTManager hangs in certain situations 
         */
        [Test]
        public virtual void TestThreadStarvationNoDeleteNRTReader()
        {
            IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random()));
            conf.SetMergePolicy(Random().NextBoolean() ? NoMergePolicy.COMPOUND_FILES : NoMergePolicy.NO_COMPOUND_FILES);
            Directory d = NewDirectory();
            CountdownEvent latch = new CountdownEvent(1);
            CountdownEvent signal = new CountdownEvent(1);

            LatchedIndexWriter _writer = new LatchedIndexWriter(d, conf, latch, signal);
            TrackingIndexWriter writer = new TrackingIndexWriter(_writer);
            SearcherManager manager = new SearcherManager(_writer, false, null);
            Document doc = new Document();
            doc.Add(NewTextField("test", "test", Field.Store.YES));
            writer.AddDocument(doc);
            manager.MaybeRefresh();
            ThreadClass t = new ThreadAnonymousInnerClassHelper(this, latch, signal, writer, manager);
            t.Start();
            _writer.WaitAfterUpdate = true; // wait in addDocument to let some reopens go through
            long lastGen = writer.UpdateDocument(new Term("foo", "bar"), doc); // once this returns the doc is already reflected in the last reopen

            Assert.IsFalse(manager.SearcherCurrent); // false since there is a delete in the queue

            IndexSearcher searcher = manager.Acquire();
            try
            {
                Assert.AreEqual(2, searcher.IndexReader.NumDocs);
            }
            finally
            {
                manager.Release(searcher);
            }
            ControlledRealTimeReopenThread<IndexSearcher> thread = new ControlledRealTimeReopenThread<IndexSearcher>(writer, manager, 0.01, 0.01);
            thread.Start(); // start reopening
            if (VERBOSE)
            {
                Console.WriteLine("waiting now for generation " + lastGen);
            }

            AtomicBoolean finished = new AtomicBoolean(false);
            ThreadClass waiter = new ThreadAnonymousInnerClassHelper2(this, lastGen, thread, finished);
            waiter.Start();
            manager.MaybeRefresh();
            waiter.Join(1000);
            if (!finished.Get())
            {
                waiter.Interrupt();
                Assert.Fail("thread deadlocked on waitForGeneration");
            }
            thread.Dispose();
            thread.Join();
            IOUtils.Close(manager, _writer, d);
        }

        private class ThreadAnonymousInnerClassHelper : ThreadClass
        {
            private readonly TestControlledRealTimeReopenThread OuterInstance;

            private CountdownEvent Latch;
            private CountdownEvent Signal;
            private TrackingIndexWriter Writer;
            private SearcherManager Manager;

            public ThreadAnonymousInnerClassHelper(TestControlledRealTimeReopenThread outerInstance, CountdownEvent latch, CountdownEvent signal, TrackingIndexWriter writer, SearcherManager manager)
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
                    Signal.Wait();
                    Manager.MaybeRefresh();
                    Writer.DeleteDocuments(new TermQuery(new Term("foo", "barista")));
                    Manager.MaybeRefresh(); // kick off another reopen so we inc. the internal gen
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                    Console.Write(e.StackTrace);
                }
                finally
                {
                    Latch.Reset(Latch.CurrentCount == 0 ? 0 : Latch.CurrentCount - 1); // let the add below finish
                }
            }
        }

        private class ThreadAnonymousInnerClassHelper2 : ThreadClass
        {
            private readonly TestControlledRealTimeReopenThread OuterInstance;

            private long LastGen;
            private ControlledRealTimeReopenThread<IndexSearcher> thread;
            private AtomicBoolean Finished;

            public ThreadAnonymousInnerClassHelper2(TestControlledRealTimeReopenThread outerInstance, long lastGen, ControlledRealTimeReopenThread<IndexSearcher> thread, AtomicBoolean finished)
            {
                this.OuterInstance = outerInstance;
                this.LastGen = lastGen;
                this.thread = thread;
                this.Finished = finished;
            }

            public override void Run()
            {
                try
                {
                    thread.WaitForGeneration(LastGen);
                }
                catch (ThreadInterruptedException ie)
                {
                    Thread.CurrentThread.Interrupt();
                    throw new Exception(ie.Message, ie);
                }
                Finished.Set(true);
            }
        }

        public class LatchedIndexWriter : IndexWriter
        {

            internal CountdownEvent Latch;
            internal bool WaitAfterUpdate = false;
            internal CountdownEvent Signal;

            public LatchedIndexWriter(Directory d, IndexWriterConfig conf, CountdownEvent latch, CountdownEvent signal)
                : base(d, conf)
            {
                this.Latch = latch;
                this.Signal = signal;

            }

            public override void UpdateDocument(Term term, IEnumerable<IIndexableField> doc, Analyzer analyzer)
            {
                base.UpdateDocument(term, doc, analyzer);
                try
                {
                    if (WaitAfterUpdate)
                    {
                        Signal.Reset(Signal.CurrentCount == 0 ? 0 : Signal.CurrentCount - 1);
                        Latch.Wait();
                    }
                }
                catch (ThreadInterruptedException e)
                {
                    throw;
                }
            }
        }

        [Test]
        public virtual void TestEvilSearcherFactory()
        {
            Directory dir = NewDirectory();
            RandomIndexWriter w = new RandomIndexWriter(Random(), dir, Similarity, TimeZone);
            w.Commit();

            IndexReader other = DirectoryReader.Open(dir);

            SearcherFactory theEvilOne = new SearcherFactoryAnonymousInnerClassHelper2(this, other);

            try
            {
                new SearcherManager(w.w, false, theEvilOne);
                Assert.Fail("didn't hit expected exception");
            }
            catch (InvalidOperationException ise)
            {
                // expected
            }
            w.Dispose();
            other.Dispose();
            dir.Dispose();
        }

        private class SearcherFactoryAnonymousInnerClassHelper2 : SearcherFactory
        {
            private readonly TestControlledRealTimeReopenThread OuterInstance;

            private IndexReader Other;

            public SearcherFactoryAnonymousInnerClassHelper2(TestControlledRealTimeReopenThread outerInstance, IndexReader other)
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
        public virtual void TestListenerCalled()
        {
            Directory dir = NewDirectory();
            IndexWriter iw = new IndexWriter(dir, new IndexWriterConfig(TEST_VERSION_CURRENT, null));
            AtomicBoolean afterRefreshCalled = new AtomicBoolean(false);
            SearcherManager sm = new SearcherManager(iw, true, new SearcherFactory());
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
            private readonly TestControlledRealTimeReopenThread OuterInstance;

            private AtomicBoolean AfterRefreshCalled;

            public RefreshListenerAnonymousInnerClassHelper(TestControlledRealTimeReopenThread outerInstance, AtomicBoolean afterRefreshCalled)
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

        // LUCENE-5461
        [Test, Timeout(120000)]
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
                builder.Append(chars[Random().Next(chars.Length)]);
            }
            string content = builder.ToString();

            SnapshotDeletionPolicy sdp = new SnapshotDeletionPolicy(new KeepOnlyLastCommitDeletionPolicy());
            Directory dir = new NRTCachingDirectory(NewFSDirectory(CreateTempDir("nrt")), 5, 128);
            IndexWriterConfig config = new IndexWriterConfig(Version.LUCENE_46, new MockAnalyzer(Random()));
            config.SetIndexDeletionPolicy(sdp);
            config.SetOpenMode(OpenMode.CREATE_OR_APPEND);
            IndexWriter iw = new IndexWriter(dir, config);
            SearcherManager sm = new SearcherManager(iw, true, new SearcherFactory());
            TrackingIndexWriter tiw = new TrackingIndexWriter(iw);
            ControlledRealTimeReopenThread<IndexSearcher> controlledRealTimeReopenThread = new ControlledRealTimeReopenThread<IndexSearcher>(tiw, sm, maxStaleSecs, 0);

            controlledRealTimeReopenThread.SetDaemon(true);
            controlledRealTimeReopenThread.Start();

            IList<ThreadClass> commitThreads = new List<ThreadClass>();

            for (int i = 0; i < 500; i++)
            {
                if (i > 0 && i % 50 == 0)
                {
                    ThreadClass commitThread = new RunnableAnonymousInnerClassHelper(this, sdp, dir, iw);
                    commitThread.Start();
                    commitThreads.Add(commitThread);
                }
                Document d = new Document();
                d.Add(new TextField("count", i + "", Field.Store.NO));
                d.Add(new TextField("content", content, Field.Store.YES));
                long start = DateTime.Now.Millisecond;
                long l = tiw.AddDocument(d);
                controlledRealTimeReopenThread.WaitForGeneration(l);
                long wait = DateTime.Now.Millisecond - start;
                Assert.IsTrue(wait < (maxStaleSecs * 1000), "waited too long for generation " + wait);
                IndexSearcher searcher = sm.Acquire();
                TopDocs td = searcher.Search(new TermQuery(new Term("count", i + "")), 10);
                sm.Release(searcher);
                Assert.AreEqual(1, td.TotalHits);
            }

            foreach (ThreadClass commitThread in commitThreads)
            {
                commitThread.Join();
            }

            controlledRealTimeReopenThread.Dispose();
            sm.Dispose();
            iw.Dispose();
            dir.Dispose();
        }

        private class RunnableAnonymousInnerClassHelper : ThreadClass
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
                    Iw.Commit();
                    IndexCommit ic = Sdp.Snapshot();
                    foreach (string name in ic.FileNames)
                    {
                        //distribute, and backup
                        //System.out.println(names);
                        Assert.IsTrue(SlowFileExists(Dir, name));
                    }
                }
                catch (Exception e)
                {
                    throw new Exception(e.Message, e);
                }
            }
        }
    }
}