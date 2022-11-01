using J2N.Threading;
using J2N.Threading.Atomic;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Index.Extensions;
using Lucene.Net.Store;
using Lucene.Net.Support.Threading;
using Lucene.Net.Util;
using NUnit.Framework;
using RandomizedTesting.Generators;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using JCG = J2N.Collections.Generic;
using Assert = Lucene.Net.TestFramework.Assert;
using Console = Lucene.Net.Util.SystemConsole;
using Lucene.Net.Attributes;

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
    using Directory = Lucene.Net.Store.Directory;
    using DirectoryReader = Lucene.Net.Index.DirectoryReader;
    using Document = Lucene.Net.Documents.Document;
    using Field = Lucene.Net.Documents.Field;
    using IIndexableField = Lucene.Net.Index.IIndexableField;
    using IndexCommit = Lucene.Net.Index.IndexCommit;
    using IndexReader = Lucene.Net.Index.IndexReader;
    using IndexWriter = Lucene.Net.Index.IndexWriter;
    using IndexWriterConfig = Lucene.Net.Index.IndexWriterConfig;
    using IOUtils = Lucene.Net.Util.IOUtils;
    using KeepOnlyLastCommitDeletionPolicy = Lucene.Net.Index.KeepOnlyLastCommitDeletionPolicy;
    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
    using MockAnalyzer = Lucene.Net.Analysis.MockAnalyzer;
    using NoMergePolicy = Lucene.Net.Index.NoMergePolicy;
    using NRTCachingDirectory = Lucene.Net.Store.NRTCachingDirectory;
    using OpenMode = Lucene.Net.Index.OpenMode;
    using RandomIndexWriter = Lucene.Net.Index.RandomIndexWriter;
    using SnapshotDeletionPolicy = Lucene.Net.Index.SnapshotDeletionPolicy;
    using Term = Lucene.Net.Index.Term;
    using TextField = Lucene.Net.Documents.TextField;
    using ThreadedIndexingAndSearchingTestCase = Lucene.Net.Index.ThreadedIndexingAndSearchingTestCase;
    using TrackingIndexWriter = Lucene.Net.Index.TrackingIndexWriter;
    using Version = Lucene.Net.Util.LuceneVersion;

    [SuppressCodecs("SimpleText", "Memory", "Direct")]
    [TestFixture]
    public class TestControlledRealTimeReopenThread : ThreadedIndexingAndSearchingTestCase
    {

        // Not guaranteed to reflect deletes:
        private SearcherManager nrtNoDeletes;

        // Is guaranteed to reflect deletes:
        private SearcherManager nrtDeletes;

        private TrackingIndexWriter genWriter;

        private ControlledRealTimeReopenThread<IndexSearcher> nrtDeletesThread;
        private ControlledRealTimeReopenThread<IndexSearcher> nrtNoDeletesThread;

        private readonly DisposableThreadLocal<long?> lastGens = new DisposableThreadLocal<long?>();
        private bool warmCalled;

        // LUCENENET specific - cleanup DisposableThreadLocal instances
        public override void AfterClass()
        {
            lastGens.Dispose();
            base.AfterClass();
        }

        [Test]
        [Slow]
        public virtual void TestControlledRealTimeReopenThread_Mem()
        {
            RunTest("TestControlledRealTimeReopenThread");
        }

        protected override IndexSearcher GetFinalSearcher()
        {
            if (Verbose)
            {
                Console.WriteLine("TEST: finalSearcher maxGen=" + maxGen);
            }
            nrtDeletesThread.WaitForGeneration(maxGen);
            return nrtDeletes.Acquire();
        }

        protected override Directory GetDirectory(Directory @in)
        {
            // Randomly swap in NRTCachingDir
            if (Random.NextBoolean())
            {
                if (Verbose)
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

        protected override void UpdateDocuments(Term id, IEnumerable<IEnumerable<IIndexableField>> docs)
        {
            long gen = genWriter.UpdateDocuments(id, docs);

            // Randomly verify the update "took":
            if (Random.Next(20) == 2)
            {
                if (Verbose)
                {
                    Console.WriteLine(Thread.CurrentThread.Name + ": nrt: verify " + id);
                }
                nrtDeletesThread.WaitForGeneration(gen);
                IndexSearcher s = nrtDeletes.Acquire();
                if (Verbose)
                {
                    Console.WriteLine(Thread.CurrentThread.Name + ": nrt: got searcher=" + s);
                }
                try
                {
                    assertEquals(docs.Count(), s.Search(new TermQuery(id), 10).TotalHits);
                }
                finally
                {
                    nrtDeletes.Release(s);
                }
            }

            lastGens.Value = gen;

        }

        protected override void AddDocuments(Term id, IEnumerable<IEnumerable<IIndexableField>> docs)
        {
            long gen = genWriter.AddDocuments(docs);
            // Randomly verify the add "took":
            if (Random.Next(20) == 2)
            {
                if (Verbose)
                {
                    Console.WriteLine(Thread.CurrentThread.Name + ": nrt: verify " + id);
                }
                nrtNoDeletesThread.WaitForGeneration(gen);
                IndexSearcher s = nrtNoDeletes.Acquire();
                if (Verbose)
                {
                    Console.WriteLine(Thread.CurrentThread.Name + ": nrt: got searcher=" + s);
                }
                try
                {
                    assertEquals(docs.Count(), s.Search(new TermQuery(id), 10).TotalHits);
                }
                finally
                {
                    nrtNoDeletes.Release(s);
                }
            }
            lastGens.Value = gen;
        }

        protected override void AddDocument(Term id, IEnumerable<IIndexableField> doc)
        {
            long gen = genWriter.AddDocument(doc);

            // Randomly verify the add "took":
            if (Random.Next(20) == 2)
            {
                if (Verbose)
                {
                    Console.WriteLine(Thread.CurrentThread.Name + ": nrt: verify " + id);
                }
                nrtNoDeletesThread.WaitForGeneration(gen);
                IndexSearcher s = nrtNoDeletes.Acquire();
                if (Verbose)
                {
                    Console.WriteLine(Thread.CurrentThread.Name + ": nrt: got searcher=" + s);
                }
                try
                {
                    assertEquals(1, s.Search(new TermQuery(id), 10).TotalHits);
                }
                finally
                {
                    nrtNoDeletes.Release(s);
                }
            }
            lastGens.Value = gen;
        }

        protected override void UpdateDocument(Term id, IEnumerable<IIndexableField> doc)
        {
            long gen = genWriter.UpdateDocument(id, doc);
            // Randomly verify the udpate "took":
            if (Random.Next(20) == 2)
            {
                if (Verbose)
                {
                    Console.WriteLine(Thread.CurrentThread.Name + ": nrt: verify " + id);
                }
                nrtDeletesThread.WaitForGeneration(gen);
                IndexSearcher s = nrtDeletes.Acquire();
                if (Verbose)
                {
                    Console.WriteLine(Thread.CurrentThread.Name + ": nrt: got searcher=" + s);
                }
                try
                {
                    assertEquals(1, s.Search(new TermQuery(id), 10).TotalHits);
                }
                finally
                {
                    nrtDeletes.Release(s);
                }
            }
            lastGens.Value = gen;
        }

        protected override void DeleteDocuments(Term id)
        {
            long gen = genWriter.DeleteDocuments(id);
            // randomly verify the delete "took":
            if (Random.Next(20) == 7)
            {
                if (Verbose)
                {
                    Console.WriteLine(Thread.CurrentThread.Name + ": nrt: verify del " + id);
                }
                nrtDeletesThread.WaitForGeneration(gen);
                IndexSearcher s = nrtDeletes.Acquire();
                if (Verbose)
                {
                    Console.WriteLine(Thread.CurrentThread.Name + ": nrt: got searcher=" + s);
                }
                try
                {
                    assertEquals(0, s.Search(new TermQuery(id), 10).TotalHits);
                }
                finally
                {
                    nrtDeletes.Release(s);
                }
            }
            lastGens.Value = gen;
        }

        protected override void DoAfterWriter(TaskScheduler es)
        {
            double minReopenSec = 0.01 + 0.05 * Random.NextDouble();
            double maxReopenSec = minReopenSec * (1.0 + 10 * Random.NextDouble());

            if (Verbose)
            {
                Console.WriteLine("TEST: make SearcherManager maxReopenSec=" + maxReopenSec + " minReopenSec=" + minReopenSec);
            }

            genWriter = new TrackingIndexWriter(m_writer);

            SearcherFactory sf = new SearcherFactoryAnonymousClass(this, es);

            nrtNoDeletes = new SearcherManager(m_writer, false, sf);
            nrtDeletes = new SearcherManager(m_writer, true, sf);

            nrtDeletesThread = new ControlledRealTimeReopenThread<IndexSearcher>(genWriter, nrtDeletes, maxReopenSec, minReopenSec);
            nrtDeletesThread.Name = "NRTDeletes Reopen Thread";
            nrtDeletesThread.Priority = (ThreadPriority)Math.Min((int)Thread.CurrentThread.Priority + 2, (int)ThreadPriority.Highest);
            nrtDeletesThread.IsBackground = (true);
            nrtDeletesThread.Start();

            nrtNoDeletesThread = new ControlledRealTimeReopenThread<IndexSearcher>(genWriter, nrtNoDeletes, maxReopenSec, minReopenSec);
            nrtNoDeletesThread.Name = "NRTNoDeletes Reopen Thread";
            nrtNoDeletesThread.Priority = (ThreadPriority)Math.Min((int)Thread.CurrentThread.Priority + 2, (int)ThreadPriority.Highest);
            nrtNoDeletesThread.IsBackground = (true);
            nrtNoDeletesThread.Start();
        }

        private sealed class SearcherFactoryAnonymousClass : SearcherFactory
        {
            private readonly TestControlledRealTimeReopenThread outerInstance;

            private TaskScheduler es;

            public SearcherFactoryAnonymousClass(TestControlledRealTimeReopenThread outerInstance, TaskScheduler es)
            {
                this.outerInstance = outerInstance;
                this.es = es;
            }

            public override IndexSearcher NewSearcher(IndexReader r)
            {
                outerInstance.warmCalled = true;
                IndexSearcher s = new IndexSearcher(r, es);
                s.Search(new TermQuery(new Term("body", "united")), 10);
                return s;
            }
        }

        protected override void DoAfterIndexingThreadDone()
        {
            long? gen = lastGens.Value;
            if (gen != null)
            {
                AddMaxGen((long)gen);
            }
        }

        private long maxGen = -1;

        private void AddMaxGen(long gen)
        {
            UninterruptableMonitor.Enter(this);
            try
            {
                maxGen = Math.Max(gen, maxGen);
            }
            finally
            {
                UninterruptableMonitor.Exit(this);
            }
        }

        protected override void DoSearching(TaskScheduler es, long stopTime)
        {
            RunSearchThreads(stopTime);
        }

        protected override IndexSearcher GetCurrentSearcher()
        {
            // Test doesn't assert deletions until the end, so we
            // can randomize whether dels must be applied
            SearcherManager nrt;
            if (Random.NextBoolean())
            {
                nrt = nrtDeletes;
            }
            else
            {
                nrt = nrtNoDeletes;
            }

            return nrt.Acquire();
        }

        protected override void ReleaseSearcher(IndexSearcher s)
        {
            // NOTE: a bit iffy... technically you should release
            // against the same SearcherManager you acquired from... but
            // both impls just decRef the underlying reader so we
            // can get away w/ cheating:
            nrtNoDeletes.Release(s);
        }

        protected override void DoClose()
        {
            Assert.IsTrue(warmCalled);
            if (Verbose)
            {
                Console.WriteLine("TEST: now close SearcherManagers");
            }
            nrtDeletesThread.Dispose();
            nrtDeletes.Dispose();
            nrtNoDeletesThread.Dispose();
            nrtNoDeletes.Dispose();
        }

        /*
         * LUCENE-3528 - NRTManager hangs in certain situations 
         */
        [Test]
        public virtual void TestThreadStarvationNoDeleteNRTReader()
        {
            IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random));
            conf.SetMergePolicy(Random.NextBoolean() ? NoMergePolicy.COMPOUND_FILES : NoMergePolicy.NO_COMPOUND_FILES);
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

            var t = new ThreadAnonymousClass(this, latch, signal, writer, manager);
            t.Start();
            _writer.waitAfterUpdate = true; // wait in addDocument to let some reopens go through
            long lastGen = writer.UpdateDocument(new Term("foo", "bar"), doc); // once this returns the doc is already reflected in the last reopen

            assertFalse(manager.IsSearcherCurrent()); // false since there is a delete in the queue

            IndexSearcher searcher = manager.Acquire();
            try
            {
                assertEquals(2, searcher.IndexReader.NumDocs);
            }
            finally
            {
                manager.Release(searcher);
            }

            ControlledRealTimeReopenThread<IndexSearcher> thread = new ControlledRealTimeReopenThread<IndexSearcher>(writer, manager, 0.01, 0.01);
            thread.Start(); // start reopening
            if (Verbose)
            {
                Console.WriteLine("waiting now for generation " + lastGen);
            }

            AtomicBoolean finished = new AtomicBoolean(false);
            var waiter = new ThreadAnonymousClass2(this, lastGen, thread, finished);
            waiter.Start();
            manager.MaybeRefresh();

            waiter.Join(1000);
            if (!finished)
            {
                waiter.Interrupt();
                fail("thread deadlocked on waitForGeneration");
            }
            thread.Dispose();
            thread.Join();
            IOUtils.Dispose(manager, _writer, d);
        }

        private sealed class ThreadAnonymousClass : ThreadJob
        {
            private readonly TestControlledRealTimeReopenThread outerInstance;

            private readonly CountdownEvent latch;
            private readonly CountdownEvent signal;
            private readonly TrackingIndexWriter writer;
            private readonly SearcherManager manager;

            public ThreadAnonymousClass(TestControlledRealTimeReopenThread outerInstance, CountdownEvent latch, CountdownEvent signal, TrackingIndexWriter writer, SearcherManager manager)
            {
                this.outerInstance = outerInstance;
                this.latch = latch;
                this.signal = signal;
                this.writer = writer;
                this.manager = manager;
            }

            public override void Run()
            {
                try
                {
                    signal.Wait();
                    manager.MaybeRefresh();
                    writer.DeleteDocuments(new TermQuery(new Term("foo", "barista")));
                    manager.MaybeRefresh(); // kick off another reopen so we inc. the internal gen
                }
                catch (Exception e) when (e.IsException())
                {
                    e.printStackTrace();
                }
                finally
                {
                    latch.Reset(latch.CurrentCount == 0 ? 0 : latch.CurrentCount - 1); // let the add below finish
                }
            }
        }

        private sealed class ThreadAnonymousClass2 : ThreadJob
        {
            private readonly TestControlledRealTimeReopenThread outerInstance;

            private readonly long lastGen;
            private readonly ControlledRealTimeReopenThread<IndexSearcher> thread;
            private readonly AtomicBoolean finished;

            public ThreadAnonymousClass2(TestControlledRealTimeReopenThread outerInstance, long lastGen, ControlledRealTimeReopenThread<IndexSearcher> thread, AtomicBoolean finished)
            {
                this.outerInstance = outerInstance;
                this.lastGen = lastGen;
                this.thread = thread;
                this.finished = finished;
            }

            public override void Run()
            {
                try
                {
                    thread.WaitForGeneration(lastGen);
                }
                catch (Exception ie) when (ie.IsInterruptedException())
                {
                    Thread.CurrentThread.Interrupt();
                    throw RuntimeException.Create(ie);
                }
                finished.Value = true;
            }
        }

        public class LatchedIndexWriter : IndexWriter
        {

            internal CountdownEvent latch;
            internal bool waitAfterUpdate = false;
            internal CountdownEvent signal;

            public LatchedIndexWriter(Directory d, IndexWriterConfig conf, CountdownEvent latch, CountdownEvent signal)
                : base(d, conf)
            {
                this.latch = latch;
                this.signal = signal;

            }

            public override void UpdateDocument(Term term, IEnumerable<IIndexableField> doc, Analyzer analyzer)
            {
                base.UpdateDocument(term, doc, analyzer);
                try
                {
                    if (waitAfterUpdate)
                    {
                        signal.Reset(signal.CurrentCount == 0 ? 0 : signal.CurrentCount - 1);
                        latch.Wait();
                    }
                }
                catch (Exception ie) when (ie.IsInterruptedException())
                {
                    throw new Util.ThreadInterruptedException(ie);
                }
            }
        }

        [Test]
        public virtual void TestEvilSearcherFactory()
        {
            Directory dir = NewDirectory();
            RandomIndexWriter w = new RandomIndexWriter(Random, dir);
            w.Commit();

            IndexReader other = DirectoryReader.Open(dir);

            SearcherFactory theEvilOne = new SearcherFactoryAnonymousClass2(this, other);

            try
            {
                new SearcherManager(w.IndexWriter, false, theEvilOne);
                fail("didn't hit expected exception");
            }
            catch (Exception ise) when (ise.IsIllegalStateException())
            {
                // expected
            }
            w.Dispose();
            other.Dispose();
            dir.Dispose();
        }

        private sealed class SearcherFactoryAnonymousClass2 : SearcherFactory
        {
            private readonly TestControlledRealTimeReopenThread outerInstance;

            private readonly IndexReader other;

            public SearcherFactoryAnonymousClass2(TestControlledRealTimeReopenThread outerInstance, IndexReader other)
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
        public virtual void TestListenerCalled()
        {
            Directory dir = NewDirectory();
            IndexWriter iw = new IndexWriter(dir, new IndexWriterConfig(TEST_VERSION_CURRENT, null));
            AtomicBoolean afterRefreshCalled = new AtomicBoolean(false);
            SearcherManager sm = new SearcherManager(iw, true, new SearcherFactory());
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
            private readonly TestControlledRealTimeReopenThread outerInstance;

            private AtomicBoolean afterRefreshCalled;

            public RefreshListenerAnonymousClass(TestControlledRealTimeReopenThread outerInstance, AtomicBoolean afterRefreshCalled)
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
                    afterRefreshCalled.Value = true;
                }
            }
        }

        // LUCENE-5461
        [Test]
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
                builder.Append(chars[Random.Next(chars.Length)]);
            }
            string content = builder.ToString();

            SnapshotDeletionPolicy sdp = new SnapshotDeletionPolicy(new KeepOnlyLastCommitDeletionPolicy());
            Directory dir = new NRTCachingDirectory(NewFSDirectory(CreateTempDir("nrt")), 5, 128);
            IndexWriterConfig config = new IndexWriterConfig(
#pragma warning disable 612, 618
                Version.LUCENE_46,
#pragma warning restore 612, 618
                new MockAnalyzer(Random));
            config.SetIndexDeletionPolicy(sdp);
            config.SetOpenMode(OpenMode.CREATE_OR_APPEND);
            IndexWriter iw = new IndexWriter(dir, config);
            SearcherManager sm = new SearcherManager(iw, true, new SearcherFactory());
            TrackingIndexWriter tiw = new TrackingIndexWriter(iw);
            ControlledRealTimeReopenThread<IndexSearcher> controlledRealTimeReopenThread = 
                new ControlledRealTimeReopenThread<IndexSearcher>(tiw, sm, maxStaleSecs, 0);

            controlledRealTimeReopenThread.IsBackground = (true);
            controlledRealTimeReopenThread.Start();

            IList<ThreadJob> commitThreads = new JCG.List<ThreadJob>();

            for (int i = 0; i < 500; i++)
            {
                if (i > 0 && i % 50 == 0)
                {
                    ThreadJob commitThread = new RunnableAnonymousClass(this, sdp, dir, iw);
                    commitThread.Start();
                    commitThreads.Add(commitThread);
                }
                Document d = new Document();
                d.Add(new TextField("count", i + "", Field.Store.NO));
                d.Add(new TextField("content", content, Field.Store.YES));
                long start = J2N.Time.NanoTime() / J2N.Time.MillisecondsPerNanosecond; // LUCENENET: Use NanoTime() rather than CurrentTimeMilliseconds() for more accurate/reliable results
                long l = tiw.AddDocument(d);
                controlledRealTimeReopenThread.WaitForGeneration(l);
                long wait = (J2N.Time.NanoTime() / J2N.Time.MillisecondsPerNanosecond) - start; // LUCENENET: Use NanoTime() rather than CurrentTimeMilliseconds() for more accurate/reliable results
                assertTrue("waited too long for generation " + wait, wait < (maxStaleSecs * 1000));
                IndexSearcher searcher = sm.Acquire();
                TopDocs td = searcher.Search(new TermQuery(new Term("count", i + "")), 10);
                sm.Release(searcher);
                assertEquals(1, td.TotalHits);
            }

            foreach (ThreadJob commitThread in commitThreads)
            {
                commitThread.Join();
            }

            controlledRealTimeReopenThread.Dispose();
            sm.Dispose();
            iw.Dispose();
            dir.Dispose();
        }

        private sealed class RunnableAnonymousClass : ThreadJob
        {
            private readonly TestControlledRealTimeReopenThread outerInstance;

            private SnapshotDeletionPolicy sdp;
            private Directory dir;
            private IndexWriter iw;

            public RunnableAnonymousClass(TestControlledRealTimeReopenThread outerInstance, SnapshotDeletionPolicy sdp, Directory dir, IndexWriter iw)
            {
                this.outerInstance = outerInstance;
                this.sdp = sdp;
                this.dir = dir;
                this.iw = iw;
            }

            public override void Run()
            {
                try
                {
                    iw.Commit();
                    IndexCommit ic = sdp.Snapshot();
                    foreach (string name in ic.FileNames)
                    {
                        //distribute, and backup
                        //System.out.println(names);
                        assertTrue(SlowFileExists(dir, name));
                    }
                }
                catch (Exception e) when (e.IsException())
                {
                    throw RuntimeException.Create(e);
                }
            }
        }


        /// <summary>
        /// This test was purposely written in a way that demonstrates how to use the
        /// ControlledRealTimeReopenThread.  It contains seperate Asserts for each of
        /// several use cases rather then trying to brake these use cases up into
        /// seperate unit tests.  This combined approach makes the behavior of 
        /// ControlledRealTimeReopenThread easier to understand.
        /// </summary>
        // LUCENENET specific - An extra test to demonstrate use of ControlledRealTimeReopen.
        [Test]
        [LuceneNetSpecific]
        [Ignore("Run Manually (contains timing code that doesn't play well with other tests)")]
        public void TestStraightForwardDemonstration()
        {

            RAMDirectory indexDir = new RAMDirectory();

            Analyzer standardAnalyzer = new StandardAnalyzer(TEST_VERSION_CURRENT);
            IndexWriterConfig indexConfig = new IndexWriterConfig(TEST_VERSION_CURRENT, standardAnalyzer);
            IndexWriter indexWriter = new IndexWriter(indexDir, indexConfig);
            TrackingIndexWriter trackingWriter = new TrackingIndexWriter(indexWriter);

            Document doc = new Document();
            doc.Add(new Int32Field("id", 1, Field.Store.YES));
            doc.Add(new StringField("name", "Doc1", Field.Store.YES));
            trackingWriter.AddDocument(doc);

            SearcherManager searcherManager = new SearcherManager(indexWriter, applyAllDeletes: true, null);

            //Reopen SearcherManager every 1 secs via background thread if no thread waiting for newer generation.
            //Reopen SearcherManager after .2 secs if another thread IS waiting on a newer generation.  
            var controlledRealTimeReopenThread = new ControlledRealTimeReopenThread<IndexSearcher>(trackingWriter, searcherManager, 1, 0.2);

            //Start() will start a seperate thread that will invoke the object's Run(). However,
            //calling Run() directly would execute that code on the current thread rather then a new thread
            //which would defeat the purpose of using controlledRealTimeReopenThread. This aspect of the API
            //is not as intuitive as it could be. ie. Call Start() not Run().
            controlledRealTimeReopenThread.IsBackground = true;                     //Set as a background thread
            controlledRealTimeReopenThread.Name = "Controlled Real Time Reopen Thread";
            controlledRealTimeReopenThread.Priority = (ThreadPriority)Math.Min((int)Thread.CurrentThread.Priority + 2, (int)ThreadPriority.Highest);
            controlledRealTimeReopenThread.Start();

            //An indexSearcher only sees Doc1
            IndexSearcher indexSearcher = searcherManager.Acquire();
            try
            {
                TopDocs topDocs = indexSearcher.Search(new MatchAllDocsQuery(), 1);
                assertEquals(1, topDocs.TotalHits);             //There is only one doc
            }
            finally
            {
                searcherManager.Release(indexSearcher);
            }

            //Add a 2nd document
            doc = new Document();
            doc.Add(new Int32Field("id", 2, Field.Store.YES));
            doc.Add(new StringField("name", "Doc2", Field.Store.YES));
            trackingWriter.AddDocument(doc);

            //Demonstrate that we can only see the first doc because we haven't 
            //waited 1 sec or called WaitForGeneration
            indexSearcher = searcherManager.Acquire();
            try
            {
                TopDocs topDocs = indexSearcher.Search(new MatchAllDocsQuery(), 1);
                assertEquals(1, topDocs.TotalHits);             //Can see both docs due to auto refresh after 1.1 secs
            }
            finally
            {
                searcherManager.Release(indexSearcher);
            }


            //Demonstrate that we can see both docs after we wait a little more
            //then 1 sec so that controlledRealTimeReopenThread max interval is exceeded
            //and it calls MaybeRefresh
            Thread.Sleep(1100);     //wait 1.1 secs as ms
            indexSearcher = searcherManager.Acquire();
            try
            {
                TopDocs topDocs = indexSearcher.Search(new MatchAllDocsQuery(), 1);
                assertEquals(2, topDocs.TotalHits);             //Can see both docs due to auto refresh after 1.1 secs
            }
            finally
            {
                searcherManager.Release(indexSearcher);
            }


            //Add a 3rd document
            doc = new Document();
            doc.Add(new Int32Field("id", 3, Field.Store.YES));
            doc.Add(new StringField("name", "Doc3", Field.Store.YES));
            long generation = trackingWriter.AddDocument(doc);

            //Demonstrate that if we call WaitForGeneration our wait will be
            // .2 secs or less (the min interval we set earlier) and then we will
            //see all 3 documents.
            Stopwatch stopwatch = Stopwatch.StartNew();
            controlledRealTimeReopenThread.WaitForGeneration(generation);
            stopwatch.Stop();
            assertTrue(stopwatch.Elapsed.TotalMilliseconds <= 200 + 30);   //30ms is fudged factor to account for call overhead.

            indexSearcher = searcherManager.Acquire();
            try
            {
                TopDocs topDocs = indexSearcher.Search(new MatchAllDocsQuery(), 1);
                assertEquals(3, topDocs.TotalHits);             //Can see both docs due to auto refresh after 1.1 secs
            }
            finally
            {
                searcherManager.Release(indexSearcher);
            }

            controlledRealTimeReopenThread.Dispose();
            searcherManager.Dispose();
            indexWriter.Dispose();
            indexDir.Dispose();
        }



        /// <summary>
        /// In this test multiple threads are created each of which is waiting on the same 
        /// generation before doing a search.  These threads will all stack up on the 
        /// WaitForGeneration(generation) call.  All threads should return from this call
        /// in approximately in the time expected, namely the value for targetMinStaleSec passed
        /// to ControlledRealTimeReopenThread in it's constructor.
        /// </summary>
        // LUCENENET specific - An extra test to test multithreaded use of ControlledRealTimeReopen.
        [Test]
        [LuceneNetSpecific]
        [Ignore("Run Manually (contains timing code that doesn't play well with other tests)")]
        public void TestMultithreadedWaitForGeneration()
        {
            Thread CreateWorker(int threadNum, ControlledRealTimeReopenThread<IndexSearcher> controlledReopen, long generation,
                                SearcherManager searcherManager, List<ThreadOutput> outputList)
            {
                ThreadStart threadStart = delegate
                {

                    Stopwatch stopwatch = Stopwatch.StartNew();
                    controlledReopen.WaitForGeneration(generation);
                    stopwatch.Stop();
                    double milliSecsWaited = stopwatch.Elapsed.TotalMilliseconds;

                    int numRecs = 0;
                    IndexSearcher indexSearcher = searcherManager.Acquire();
                    try
                    {
                        TopDocs topDocs = indexSearcher.Search(new MatchAllDocsQuery(), 1);
                        numRecs = topDocs.TotalHits;
                    }
                    finally
                    {
                        searcherManager.Release(indexSearcher);
                    }

                    lock (outputList)
                    {
                        outputList.Add(new ThreadOutput { ThreadNum = threadNum, NumRecs = numRecs, MilliSecsWaited = milliSecsWaited });
                    }

                };
                return new Thread(threadStart);
            }

            int threadCount = 3;
            List<ThreadOutput> outputList = new List<ThreadOutput>();

            RAMDirectory indexDir = new RAMDirectory();
            Analyzer standardAnalyzer = new StandardAnalyzer(TEST_VERSION_CURRENT);
            IndexWriterConfig indexConfig = new IndexWriterConfig(TEST_VERSION_CURRENT, standardAnalyzer);
            IndexWriter indexWriter = new IndexWriter(indexDir, indexConfig);
            TrackingIndexWriter trackingWriter = new TrackingIndexWriter(indexWriter);

            //Add two documents
            Document doc = new Document();
            doc.Add(new Int32Field("id", 1, Field.Store.YES));
            doc.Add(new StringField("name", "Doc1", Field.Store.YES));
            long generation = trackingWriter.AddDocument(doc);

            doc.Add(new Int32Field("id", 2, Field.Store.YES));
            doc.Add(new StringField("name", "Doc3", Field.Store.YES));
            generation = trackingWriter.AddDocument(doc);

            SearcherManager searcherManager = new SearcherManager(indexWriter, applyAllDeletes: true, null);

            //Reopen SearcherManager every 2 secs via background thread if no thread waiting for newer generation.
            //Reopen SearcherManager after .2 secs if another thread IS waiting on a newer generation.  
            double maxRefreshSecs = 2.0;
            double minRefreshSecs = .2;
            var controlledRealTimeReopenThread = new ControlledRealTimeReopenThread<IndexSearcher>(trackingWriter, searcherManager, maxRefreshSecs, minRefreshSecs);

            //Start() will start a seperate thread that will invoke the object's Run(). However,
            //calling Run() directly would execute that code on the current thread rather then a new thread
            //which would defeat the purpose of using controlledRealTimeReopenThread. This aspect of the API
            //is not as intuitive as it could be. ie. Call Start() not Run().
            controlledRealTimeReopenThread.IsBackground = true;                     //Set as a background thread
            controlledRealTimeReopenThread.Name = "Controlled Real Time Reopen Thread";
            controlledRealTimeReopenThread.Priority = (ThreadPriority)Math.Min((int)Thread.CurrentThread.Priority + 2, (int)ThreadPriority.Highest);
            controlledRealTimeReopenThread.Start();


            //Create the threads for doing searchers
            List<Thread> threadList = new List<Thread>();
            for (int i = 1; i <= threadCount; i++)
            {
                threadList.Add(CreateWorker(i, controlledRealTimeReopenThread, generation, searcherManager, outputList));
            }

            //Start all the threads
            foreach (Thread thread in threadList)
            {
                thread.Start();
            }

            //wait for the threads to finish.
            foreach (Thread thread in threadList)
            {
                thread.Join();                          //will wait here until the thread terminates.
            }

            //Now make sure that no thread waited longer then our min refresh time
            //plus a small fudge factor. Also verify that all threads resported back and
            //each saw 2 records.

            //Verify all threads reported back a result.
            assertEquals(threadCount, outputList.Count);

            int millisecsPerSec = 1000;
            foreach (ThreadOutput output in outputList)
            {
                //Verify the thread saw exactly 2 docs
                assertEquals(2, output.NumRecs);

                //Verify the thread wait time was around what was expected.
                Assert.True(output.MilliSecsWaited <= (minRefreshSecs * millisecsPerSec) + 30);   //30ms is fudged factor to account for call overhead
            }

            controlledRealTimeReopenThread.Dispose();                       //will kill and join to the thread
            Assert.False(controlledRealTimeReopenThread.IsAlive);           //to prove that Dispose really does kill the thread.

            searcherManager.Dispose();
            indexWriter.Dispose();
            indexDir.Dispose();

        }

        [DebuggerDisplay("ThreadNum:{ThreadNum}, NumRecs:{NumRecs}, MilliSecsWaited:{MilliSecsWaited}")]
        public class ThreadOutput
        {
            public int ThreadNum { get; set; }
            public int NumRecs { get; set; }
            public double MilliSecsWaited { get; set; }
        }
    }
}