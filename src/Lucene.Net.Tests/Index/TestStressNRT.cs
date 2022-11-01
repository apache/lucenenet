using J2N.Threading;
using J2N.Threading.Atomic;
using Lucene.Net.Diagnostics;
using Lucene.Net.Documents;
using Lucene.Net.Support;
using Lucene.Net.Support.Threading;
using NUnit.Framework;
using RandomizedTesting.Generators;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using JCG = J2N.Collections.Generic;
using Console = Lucene.Net.Util.SystemConsole;

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

    using Directory = Lucene.Net.Store.Directory;
    using Document = Documents.Document;
    using FieldType = FieldType;
    using IndexSearcher = Lucene.Net.Search.IndexSearcher;
    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
    using MockAnalyzer = Lucene.Net.Analysis.MockAnalyzer;
    using Query = Lucene.Net.Search.Query;
    using ScoreDoc = Lucene.Net.Search.ScoreDoc;
    using TermQuery = Lucene.Net.Search.TermQuery;
    using TestUtil = Lucene.Net.Util.TestUtil;
    using TopDocs = Lucene.Net.Search.TopDocs;

    [TestFixture]
    public class TestStressNRT : LuceneTestCase
    {
        private volatile DirectoryReader reader;

        private readonly ConcurrentDictionary<int, long> model = new ConcurrentDictionary<int, long>();
        private IDictionary<int, long> committedModel = new Dictionary<int, long>();
        private long snapshotCount;
        private long committedModelClock;
        private volatile int lastId;
        private readonly string field = "val_l";
        private object[] syncArr;

        private void InitModel(int ndocs)
        {
            snapshotCount = 0;
            committedModelClock = 0;
            lastId = 0;

            syncArr = new object[ndocs];

            for (int i = 0; i < ndocs; i++)
            {
                model[i] = -1L;
                syncArr[i] = new object();
            }
            committedModel.PutAll(model);
        }

        [Test]
        public virtual void Test()
        {
            // update variables
            int commitPercent = Random.Next(20);
            int softCommitPercent = Random.Next(100); // what percent of the commits are soft
            int deletePercent = Random.Next(50);
            int deleteByQueryPercent = Random.Next(25);
            int ndocs = AtLeast(50);
            int nWriteThreads = TestUtil.NextInt32(Random, 1, TestNightly ? 10 : 5);
            int maxConcurrentCommits = TestUtil.NextInt32(Random, 1, TestNightly ? 10 : 5); // number of committers at a time... needed if we want to avoid commit errors due to exceeding the max

            bool tombstones = Random.NextBoolean();

            // query variables
            AtomicInt64 operations = new AtomicInt64(AtLeast(10000)); // number of query operations to perform in total

            int nReadThreads = TestUtil.NextInt32(Random, 1, TestNightly ? 10 : 5);
            InitModel(ndocs);

            FieldType storedOnlyType = new FieldType();
            storedOnlyType.IsStored = true;

            if (Verbose)
            {
                Console.WriteLine("\n");
                Console.WriteLine("TEST: commitPercent=" + commitPercent);
                Console.WriteLine("TEST: softCommitPercent=" + softCommitPercent);
                Console.WriteLine("TEST: deletePercent=" + deletePercent);
                Console.WriteLine("TEST: deleteByQueryPercent=" + deleteByQueryPercent);
                Console.WriteLine("TEST: ndocs=" + ndocs);
                Console.WriteLine("TEST: nWriteThreads=" + nWriteThreads);
                Console.WriteLine("TEST: nReadThreads=" + nReadThreads);
                Console.WriteLine("TEST: maxConcurrentCommits=" + maxConcurrentCommits);
                Console.WriteLine("TEST: tombstones=" + tombstones);
                Console.WriteLine("TEST: operations=" + operations);
                Console.WriteLine("\n");
            }

            AtomicInt32 numCommitting = new AtomicInt32();

            IList<ThreadJob> threads = new JCG.List<ThreadJob>();

            Directory dir = NewDirectory();

            RandomIndexWriter writer = new RandomIndexWriter(Random, dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)));
            writer.DoRandomForceMergeAssert = false;
            writer.Commit();
            reader = DirectoryReader.Open(dir);

            for (int i = 0; i < nWriteThreads; i++)
            {
                ThreadJob thread = new ThreadAnonymousClass(this, "WRITER" + i, commitPercent, softCommitPercent, deletePercent, deleteByQueryPercent, ndocs, maxConcurrentCommits, tombstones, operations, storedOnlyType, numCommitting, writer);

                threads.Add(thread);
            }

            for (int i = 0; i < nReadThreads; i++)
            {
                ThreadJob thread = new ThreadAnonymousClass2(this, "READER" + i, ndocs, tombstones, operations);

                threads.Add(thread);
            }

            foreach (ThreadJob thread in threads)
            {
                thread.Start();
            }

            foreach (ThreadJob thread in threads)
            {
                thread.Join();
            }

            writer.Dispose();
            if (Verbose)
            {
                Console.WriteLine("TEST: close reader=" + reader);
            }
            reader.Dispose();
            dir.Dispose();
        }

        private sealed class ThreadAnonymousClass : ThreadJob
        {
            private readonly TestStressNRT outerInstance;

            private readonly int commitPercent;
            private readonly int softCommitPercent;
            private readonly int deletePercent;
            private readonly int deleteByQueryPercent;
            private readonly int ndocs;
            private readonly int maxConcurrentCommits;
            private readonly bool tombstones;
            private readonly AtomicInt64 operations;
            private readonly FieldType storedOnlyType;
            private readonly AtomicInt32 numCommitting;
            private readonly RandomIndexWriter writer;

            public ThreadAnonymousClass(TestStressNRT outerInstance, string str, int commitPercent, int softCommitPercent, int deletePercent, int deleteByQueryPercent, int ndocs, int maxConcurrentCommits, bool tombstones, AtomicInt64 operations, FieldType storedOnlyType, AtomicInt32 numCommitting, RandomIndexWriter writer)
                : base(str)
            {
                this.outerInstance = outerInstance;
                this.commitPercent = commitPercent;
                this.softCommitPercent = softCommitPercent;
                this.deletePercent = deletePercent;
                this.deleteByQueryPercent = deleteByQueryPercent;
                this.ndocs = ndocs;
                this.maxConcurrentCommits = maxConcurrentCommits;
                this.tombstones = tombstones;
                this.operations = operations;
                this.storedOnlyType = storedOnlyType;
                this.numCommitting = numCommitting;
                this.writer = writer;
                rand = new J2N.Randomizer(Random.NextInt64());
            }

            internal Random rand;

            public override void Run()
            {
                try
                {
                    while (operations > 0)
                    {
                        int oper = rand.Next(100);

                        if (oper < commitPercent)
                        {
                            if (numCommitting.IncrementAndGet() <= maxConcurrentCommits)
                            {
                                IDictionary<int, long> newCommittedModel;
                                long version;
                                DirectoryReader oldReader;

                                UninterruptableMonitor.Enter(outerInstance);
                                try
                                {
                                    newCommittedModel = new Dictionary<int, long>(outerInstance.model); // take a snapshot
                                    version = outerInstance.snapshotCount++;
                                    oldReader = outerInstance.reader;
                                    oldReader.IncRef(); // increment the reference since we will use this for reopening
                                }
                                finally
                                {
                                    UninterruptableMonitor.Exit(outerInstance);
                                }

                                DirectoryReader newReader;
                                if (rand.Next(100) < softCommitPercent)
                                {
                                    // assertU(h.Commit("softCommit","true"));
                                    if (Random.NextBoolean())
                                    {
                                        if (Verbose)
                                        {
                                            Console.WriteLine("TEST: " + Thread.CurrentThread.Name + ": call writer.getReader");
                                        }
                                        newReader = writer.GetReader(true);
                                    }
                                    else
                                    {
                                        if (Verbose)
                                        {
                                            Console.WriteLine("TEST: " + Thread.CurrentThread.Name + ": reopen reader=" + oldReader + " version=" + version);
                                        }
                                        newReader = DirectoryReader.OpenIfChanged(oldReader, writer.IndexWriter, true);
                                    }
                                }
                                else
                                {
                                    // assertU(commit());
                                    if (Verbose)
                                    {
                                        Console.WriteLine("TEST: " + Thread.CurrentThread.Name + ": commit+reopen reader=" + oldReader + " version=" + version);
                                    }
                                    writer.Commit();
                                    if (Verbose)
                                    {
                                        Console.WriteLine("TEST: " + Thread.CurrentThread.Name + ": now reopen after commit");
                                    }
                                    newReader = DirectoryReader.OpenIfChanged(oldReader);
                                }

                                // Code below assumes newReader comes w/
                                // extra ref:
                                if (newReader is null)
                                {
                                    oldReader.IncRef();
                                    newReader = oldReader;
                                }

                                oldReader.DecRef();

                                UninterruptableMonitor.Enter(outerInstance);
                                try
                                {
                                    // install the new reader if it's newest (and check the current version since another reader may have already been installed)
                                    //System.out.println(Thread.currentThread().getName() + ": newVersion=" + newReader.getVersion());
                                    if (Debugging.AssertsEnabled) Debugging.Assert(newReader.RefCount > 0);
                                    if (Debugging.AssertsEnabled) Debugging.Assert(outerInstance.reader.RefCount > 0);
                                    if (newReader.Version > outerInstance.reader.Version)
                                    {
                                        if (Verbose)
                                        {
                                            Console.WriteLine("TEST: " + Thread.CurrentThread.Name + ": install new reader=" + newReader);
                                        }
                                        outerInstance.reader.DecRef();
                                        outerInstance.reader = newReader;

                                        // Silly: forces fieldInfos to be
                                        // loaded so we don't hit IOE on later
                                        // reader.toString
                                        newReader.ToString();

                                        // install this snapshot only if it's newer than the current one
                                        if (version >= outerInstance.committedModelClock)
                                        {
                                            if (Verbose)
                                            {
                                                Console.WriteLine("TEST: " + Thread.CurrentThread.Name + ": install new model version=" + version);
                                            }
                                            outerInstance.committedModel = newCommittedModel;
                                            outerInstance.committedModelClock = version;
                                        }
                                        else
                                        {
                                            if (Verbose)
                                            {
                                                Console.WriteLine("TEST: " + Thread.CurrentThread.Name + ": skip install new model version=" + version);
                                            }
                                        }
                                    }
                                    else
                                    {
                                        // if the same reader, don't decRef.
                                        if (Verbose)
                                        {
                                            Console.WriteLine("TEST: " + Thread.CurrentThread.Name + ": skip install new reader=" + newReader);
                                        }
                                        newReader.DecRef();
                                    }
                                }
                                finally
                                {
                                    UninterruptableMonitor.Exit(outerInstance);
                                }
                            }
                            numCommitting.DecrementAndGet();
                        }
                        else
                        {
                            int id = rand.Next(ndocs);
                            object sync = outerInstance.syncArr[id];

                            // set the lastId before we actually change it sometimes to try and
                            // uncover more race conditions between writing and reading
                            bool before = Random.NextBoolean();
                            if (before)
                            {
                                outerInstance.lastId = id;
                            }

                            // We can't concurrently update the same document and retain our invariants of increasing values
                            // since we can't guarantee what order the updates will be executed.
                            UninterruptableMonitor.Enter(sync);
                            try
                            {
                                long val = outerInstance.model[id];
                                long nextVal = Math.Abs(val) + 1;

                                if (oper < commitPercent + deletePercent)
                                {
                                    // assertU("<delete><id>" + id + "</id></delete>");

                                    // add tombstone first
                                    if (tombstones)
                                    {
                                        Document d = new Document();
                                        d.Add(NewStringField("id", "-" + Convert.ToString(id), Documents.Field.Store.YES));
                                        d.Add(NewField(outerInstance.field, Convert.ToString(nextVal), storedOnlyType));
                                        writer.UpdateDocument(new Term("id", "-" + Convert.ToString(id)), d);
                                    }

                                    if (Verbose)
                                    {
                                        Console.WriteLine("TEST: " + Thread.CurrentThread.Name + ": term delDocs id:" + id + " nextVal=" + nextVal);
                                    }
                                    writer.DeleteDocuments(new Term("id", Convert.ToString(id)));
                                    outerInstance.model[id] = -nextVal;
                                }
                                else if (oper < commitPercent + deletePercent + deleteByQueryPercent)
                                {
                                    //assertU("<delete><query>id:" + id + "</query></delete>");

                                    // add tombstone first
                                    if (tombstones)
                                    {
                                        Document d = new Document();
                                        d.Add(NewStringField("id", "-" + Convert.ToString(id), Documents.Field.Store.YES));
                                        d.Add(NewField(outerInstance.field, Convert.ToString(nextVal), storedOnlyType));
                                        writer.UpdateDocument(new Term("id", "-" + Convert.ToString(id)), d);
                                    }

                                    if (Verbose)
                                    {
                                        Console.WriteLine("TEST: " + Thread.CurrentThread.Name + ": query delDocs id:" + id + " nextVal=" + nextVal);
                                    }
                                    writer.DeleteDocuments(new TermQuery(new Term("id", Convert.ToString(id))));
                                    outerInstance.model[id] = -nextVal;
                                }
                                else
                                {
                                    // assertU(adoc("id",Integer.toString(id), field, Long.toString(nextVal)));
                                    Document d = new Document();
                                    d.Add(NewStringField("id", Convert.ToString(id), Documents.Field.Store.YES));
                                    d.Add(NewField(outerInstance.field, Convert.ToString(nextVal), storedOnlyType));
                                    if (Verbose)
                                    {
                                        Console.WriteLine("TEST: " + Thread.CurrentThread.Name + ": u id:" + id + " val=" + nextVal);
                                    }
                                    writer.UpdateDocument(new Term("id", Convert.ToString(id)), d);
                                    if (tombstones)
                                    {
                                        // remove tombstone after new addition (this should be optional?)
                                        writer.DeleteDocuments(new Term("id", "-" + Convert.ToString(id)));
                                    }
                                    outerInstance.model[id] = nextVal;
                                }
                            }
                            finally
                            {
                                UninterruptableMonitor.Exit(sync);
                            }

                            if (!before)
                            {
                                outerInstance.lastId = id;
                            }
                        }
                    }
                }
                catch (Exception e) when (e.IsThrowable())
                {
                    Console.WriteLine(Thread.CurrentThread.Name + ": FAILED: unexpected exception");
                    Console.WriteLine(e.StackTrace);
                    throw RuntimeException.Create(e);
                }
            }
        }

        private sealed class ThreadAnonymousClass2 : ThreadJob
        {
            private readonly TestStressNRT outerInstance;

            private readonly int ndocs;
            private readonly bool tombstones;
            private readonly AtomicInt64 operations;

            public ThreadAnonymousClass2(TestStressNRT outerInstance, string str, int ndocs, bool tombstones, AtomicInt64 operations)
                : base(str)
            {
                this.outerInstance = outerInstance;
                this.ndocs = ndocs;
                this.tombstones = tombstones;
                this.operations = operations;
                rand = new J2N.Randomizer(Random.NextInt64());
            }

            internal Random rand;

            public override void Run()
            {
                try
                {
                    IndexReader lastReader = null;
                    IndexSearcher lastSearcher = null;

                    while (operations.DecrementAndGet() >= 0)
                    {
                        // bias toward a recently changed doc
                        int id = rand.Next(100) < 25 ? outerInstance.lastId : rand.Next(ndocs);

                        // when indexing, we update the index, then the model
                        // so when querying, we should first check the model, and then the index

                        long val;
                        DirectoryReader r;
                        UninterruptableMonitor.Enter(outerInstance);
                        try
                        {
                            val = outerInstance.committedModel[id];
                            r = outerInstance.reader;
                            r.IncRef();
                        }
                        finally
                        {
                            UninterruptableMonitor.Exit(outerInstance);
                        }

                        if (Verbose)
                        {
                            Console.WriteLine("TEST: " + Thread.CurrentThread.Name + ": s id=" + id + " val=" + val + " r=" + r.Version);
                        }

                        //  sreq = req("wt","json", "q","id:"+Integer.toString(id), "omitHeader","true");
                        IndexSearcher searcher;
                        if (r == lastReader)
                        {
                            // Just re-use lastSearcher, else
                            // newSearcher may create too many thread
                            // pools (ExecutorService):
                            searcher = lastSearcher;
                        }
                        else
                        {
                            searcher = NewSearcher(r);
                            lastReader = r;
                            lastSearcher = searcher;
                        }
                        Query q = new TermQuery(new Term("id", Convert.ToString(id)));
                        TopDocs results = searcher.Search(q, 10);

                        if (results.TotalHits == 0 && tombstones)
                        {
                            // if we couldn't find the doc, look for its tombstone
                            q = new TermQuery(new Term("id", "-" + Convert.ToString(id)));
                            results = searcher.Search(q, 1);
                            if (results.TotalHits == 0)
                            {
                                if (val == -1L)
                                {
                                    // expected... no doc was added yet
                                    r.DecRef();
                                    continue;
                                }
                                Assert.Fail("No documents or tombstones found for id " + id + ", expected at least " + val + " reader=" + r);
                            }
                        }

                        if (results.TotalHits == 0 && !tombstones)
                        {
                            // nothing to do - we can't tell anything from a deleted doc without tombstones
                        }
                        else
                        {
                            // we should have found the document, or its tombstone
                            if (results.TotalHits != 1)
                            {
                                Console.WriteLine("FAIL: hits id:" + id + " val=" + val);
                                foreach (ScoreDoc sd in results.ScoreDocs)
                                {
                                    Document doc = r.Document(sd.Doc);
                                    Console.WriteLine("  docID=" + sd.Doc + " id:" + doc.Get("id") + " foundVal=" + doc.Get(outerInstance.field));
                                }
                                Assert.Fail("id=" + id + " reader=" + r + " totalHits=" + results.TotalHits);
                            }
                            Document doc_ = searcher.Doc(results.ScoreDocs[0].Doc);
                            long foundVal = Convert.ToInt64(doc_.Get(outerInstance.field));
                            if (foundVal < Math.Abs(val))
                            {
                                Assert.Fail("foundVal=" + foundVal + " val=" + val + " id=" + id + " reader=" + r);
                            }
                        }

                        r.DecRef();
                    }
                }
                catch (Exception e) when (e.IsThrowable())
                {
                    operations.Value = ((int)-1L);
                    Console.WriteLine(Thread.CurrentThread.Name + ": FAILED: unexpected exception");
                    Console.WriteLine(e.StackTrace);
                    throw RuntimeException.Create(e);
                }
            }
        }
    }
}