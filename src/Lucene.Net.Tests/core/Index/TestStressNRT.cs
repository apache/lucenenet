using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Lucene.Net.Documents;

namespace Lucene.Net.Index
{
    using Lucene.Net.Randomized.Generators;
    using Lucene.Net.Support;
    using NUnit.Framework;
    using Directory = Lucene.Net.Store.Directory;
    using Document = Documents.Document;
    using FieldType = FieldType;
    using IndexSearcher = Lucene.Net.Search.IndexSearcher;
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
    using Query = Lucene.Net.Search.Query;
    using ScoreDoc = Lucene.Net.Search.ScoreDoc;
    using TermQuery = Lucene.Net.Search.TermQuery;
    using TestUtil = Lucene.Net.Util.TestUtil;
    using TopDocs = Lucene.Net.Search.TopDocs;

    [TestFixture]
    public class TestStressNRT : LuceneTestCase
    {
        internal volatile DirectoryReader Reader;

        internal readonly ConcurrentDictionary<int, long> Model = new ConcurrentDictionary<int, long>();
        internal IDictionary<int, long> CommittedModel = new Dictionary<int, long>();
        internal long SnapshotCount;
        internal long CommittedModelClock;
        internal volatile int LastId;
        internal readonly string Field = "val_l";
        internal object[] SyncArr;

        private void InitModel(int ndocs)
        {
            SnapshotCount = 0;
            CommittedModelClock = 0;
            LastId = 0;

            SyncArr = new object[ndocs];

            for (int i = 0; i < ndocs; i++)
            {
                Model[i] = -1L;
                SyncArr[i] = new object();
            }
            CommittedModel.PutAll(Model);
        }

        [Test]
        public virtual void Test()
        {
            // update variables
            int commitPercent = Random().Next(20);
            int softCommitPercent = Random().Next(100); // what percent of the commits are soft
            int deletePercent = Random().Next(50);
            int deleteByQueryPercent = Random().Next(25);
            int ndocs = AtLeast(50);
            int nWriteThreads = TestUtil.NextInt(Random(), 1, TEST_NIGHTLY ? 10 : 5);
            int maxConcurrentCommits = TestUtil.NextInt(Random(), 1, TEST_NIGHTLY ? 10 : 5); // number of committers at a time... needed if we want to avoid commit errors due to exceeding the max

            bool tombstones = Random().NextBoolean();

            // query variables
            AtomicLong operations = new AtomicLong(AtLeast(10000)); // number of query operations to perform in total

            int nReadThreads = TestUtil.NextInt(Random(), 1, TEST_NIGHTLY ? 10 : 5);
            InitModel(ndocs);

            FieldType storedOnlyType = new FieldType();
            storedOnlyType.IsStored = true;

            if (VERBOSE)
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

            AtomicInteger numCommitting = new AtomicInteger();

            IList<ThreadClass> threads = new List<ThreadClass>();

            Directory dir = NewDirectory();

            RandomIndexWriter writer = new RandomIndexWriter(Random(), dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())));
            writer.DoRandomForceMergeAssert = false;
            writer.Commit();
            Reader = DirectoryReader.Open(dir);

            for (int i = 0; i < nWriteThreads; i++)
            {
                ThreadClass thread = new ThreadAnonymousInnerClassHelper(this, "WRITER" + i, commitPercent, softCommitPercent, deletePercent, deleteByQueryPercent, ndocs, maxConcurrentCommits, tombstones, operations, storedOnlyType, numCommitting, writer);

                threads.Add(thread);
            }

            for (int i = 0; i < nReadThreads; i++)
            {
                ThreadClass thread = new ThreadAnonymousInnerClassHelper2(this, "READER" + i, ndocs, tombstones, operations);

                threads.Add(thread);
            }

            foreach (ThreadClass thread in threads)
            {
                thread.Start();
            }

            foreach (ThreadClass thread in threads)
            {
                thread.Join();
            }

            writer.Dispose();
            if (VERBOSE)
            {
                Console.WriteLine("TEST: close reader=" + Reader);
            }
            Reader.Dispose();
            dir.Dispose();
        }

        private class ThreadAnonymousInnerClassHelper : ThreadClass
        {
            private readonly TestStressNRT OuterInstance;

            private int CommitPercent;
            private int SoftCommitPercent;
            private int DeletePercent;
            private int DeleteByQueryPercent;
            private int Ndocs;
            private int MaxConcurrentCommits;
            private bool Tombstones;
            private AtomicLong Operations;
            private FieldType StoredOnlyType;
            private AtomicInteger NumCommitting;
            private RandomIndexWriter Writer;

            public ThreadAnonymousInnerClassHelper(TestStressNRT outerInstance, string str, int commitPercent, int softCommitPercent, int deletePercent, int deleteByQueryPercent, int ndocs, int maxConcurrentCommits, bool tombstones, AtomicLong operations, FieldType storedOnlyType, AtomicInteger numCommitting, RandomIndexWriter writer)
                : base(str)
            {
                this.OuterInstance = outerInstance;
                this.CommitPercent = commitPercent;
                this.SoftCommitPercent = softCommitPercent;
                this.DeletePercent = deletePercent;
                this.DeleteByQueryPercent = deleteByQueryPercent;
                this.Ndocs = ndocs;
                this.MaxConcurrentCommits = maxConcurrentCommits;
                this.Tombstones = tombstones;
                this.Operations = operations;
                this.StoredOnlyType = storedOnlyType;
                this.NumCommitting = numCommitting;
                this.Writer = writer;
                rand = new Random(Random().Next());
            }

            internal Random rand;

            public override void Run()
            {
                try
                {
                    while (Operations.Get() > 0)
                    {
                        int oper = rand.Next(100);

                        if (oper < CommitPercent)
                        {
                            if (NumCommitting.IncrementAndGet() <= MaxConcurrentCommits)
                            {
                                IDictionary<int, long> newCommittedModel;
                                long version;
                                DirectoryReader oldReader;

                                lock (OuterInstance)
                                {
                                    newCommittedModel = new Dictionary<int, long>(OuterInstance.Model); // take a snapshot
                                    version = OuterInstance.SnapshotCount++;
                                    oldReader = OuterInstance.Reader;
                                    oldReader.IncRef(); // increment the reference since we will use this for reopening
                                }

                                DirectoryReader newReader;
                                if (rand.Next(100) < SoftCommitPercent)
                                {
                                    // assertU(h.Commit("softCommit","true"));
                                    if (Random().NextBoolean())
                                    {
                                        if (VERBOSE)
                                        {
                                            Console.WriteLine("TEST: " + Thread.CurrentThread.Name + ": call writer.getReader");
                                        }
                                        newReader = Writer.GetReader(true);
                                    }
                                    else
                                    {
                                        if (VERBOSE)
                                        {
                                            Console.WriteLine("TEST: " + Thread.CurrentThread.Name + ": reopen reader=" + oldReader + " version=" + version);
                                        }
                                        newReader = DirectoryReader.OpenIfChanged(oldReader, Writer.w, true);
                                    }
                                }
                                else
                                {
                                    // assertU(commit());
                                    if (VERBOSE)
                                    {
                                        Console.WriteLine("TEST: " + Thread.CurrentThread.Name + ": commit+reopen reader=" + oldReader + " version=" + version);
                                    }
                                    Writer.Commit();
                                    if (VERBOSE)
                                    {
                                        Console.WriteLine("TEST: " + Thread.CurrentThread.Name + ": now reopen after commit");
                                    }
                                    newReader = DirectoryReader.OpenIfChanged(oldReader);
                                }

                                // Code below assumes newReader comes w/
                                // extra ref:
                                if (newReader == null)
                                {
                                    oldReader.IncRef();
                                    newReader = oldReader;
                                }

                                oldReader.DecRef();

                                lock (OuterInstance)
                                {
                                    // install the new reader if it's newest (and check the current version since another reader may have already been installed)
                                    //System.out.println(Thread.currentThread().getName() + ": newVersion=" + newReader.getVersion());
                                    Debug.Assert(newReader.RefCount > 0);
                                    Debug.Assert(OuterInstance.Reader.RefCount > 0);
                                    if (newReader.Version > OuterInstance.Reader.Version)
                                    {
                                        if (VERBOSE)
                                        {
                                            Console.WriteLine("TEST: " + Thread.CurrentThread.Name + ": install new reader=" + newReader);
                                        }
                                        OuterInstance.Reader.DecRef();
                                        OuterInstance.Reader = newReader;

                                        // Silly: forces fieldInfos to be
                                        // loaded so we don't hit IOE on later
                                        // reader.toString
                                        newReader.ToString();

                                        // install this snapshot only if it's newer than the current one
                                        if (version >= OuterInstance.CommittedModelClock)
                                        {
                                            if (VERBOSE)
                                            {
                                                Console.WriteLine("TEST: " + Thread.CurrentThread.Name + ": install new model version=" + version);
                                            }
                                            OuterInstance.CommittedModel = newCommittedModel;
                                            OuterInstance.CommittedModelClock = version;
                                        }
                                        else
                                        {
                                            if (VERBOSE)
                                            {
                                                Console.WriteLine("TEST: " + Thread.CurrentThread.Name + ": skip install new model version=" + version);
                                            }
                                        }
                                    }
                                    else
                                    {
                                        // if the same reader, don't decRef.
                                        if (VERBOSE)
                                        {
                                            Console.WriteLine("TEST: " + Thread.CurrentThread.Name + ": skip install new reader=" + newReader);
                                        }
                                        newReader.DecRef();
                                    }
                                }
                            }
                            NumCommitting.DecrementAndGet();
                        }
                        else
                        {
                            int id = rand.Next(Ndocs);
                            object sync = OuterInstance.SyncArr[id];

                            // set the lastId before we actually change it sometimes to try and
                            // uncover more race conditions between writing and reading
                            bool before = Random().NextBoolean();
                            if (before)
                            {
                                OuterInstance.LastId = id;
                            }

                            // We can't concurrently update the same document and retain our invariants of increasing values
                            // since we can't guarantee what order the updates will be executed.
                            lock (sync)
                            {
                                long val = OuterInstance.Model[id];
                                long nextVal = Math.Abs(val) + 1;

                                if (oper < CommitPercent + DeletePercent)
                                {
                                    // assertU("<delete><id>" + id + "</id></delete>");

                                    // add tombstone first
                                    if (Tombstones)
                                    {
                                        Document d = new Document();
                                        d.Add(OuterInstance.NewStringField("id", "-" + Convert.ToString(id), Documents.Field.Store.YES));
                                        d.Add(OuterInstance.NewField(OuterInstance.Field, Convert.ToString(nextVal), StoredOnlyType));
                                        Writer.UpdateDocument(new Term("id", "-" + Convert.ToString(id)), d);
                                    }

                                    if (VERBOSE)
                                    {
                                        Console.WriteLine("TEST: " + Thread.CurrentThread.Name + ": term delDocs id:" + id + " nextVal=" + nextVal);
                                    }
                                    Writer.DeleteDocuments(new Term("id", Convert.ToString(id)));
                                    OuterInstance.Model[id] = -nextVal;
                                }
                                else if (oper < CommitPercent + DeletePercent + DeleteByQueryPercent)
                                {
                                    //assertU("<delete><query>id:" + id + "</query></delete>");

                                    // add tombstone first
                                    if (Tombstones)
                                    {
                                        Document d = new Document();
                                        d.Add(OuterInstance.NewStringField("id", "-" + Convert.ToString(id), Documents.Field.Store.YES));
                                        d.Add(OuterInstance.NewField(OuterInstance.Field, Convert.ToString(nextVal), StoredOnlyType));
                                        Writer.UpdateDocument(new Term("id", "-" + Convert.ToString(id)), d);
                                    }

                                    if (VERBOSE)
                                    {
                                        Console.WriteLine("TEST: " + Thread.CurrentThread.Name + ": query delDocs id:" + id + " nextVal=" + nextVal);
                                    }
                                    Writer.DeleteDocuments(new TermQuery(new Term("id", Convert.ToString(id))));
                                    OuterInstance.Model[id] = -nextVal;
                                }
                                else
                                {
                                    // assertU(adoc("id",Integer.toString(id), field, Long.toString(nextVal)));
                                    Document d = new Document();
                                    d.Add(OuterInstance.NewStringField("id", Convert.ToString(id), Documents.Field.Store.YES));
                                    d.Add(OuterInstance.NewField(OuterInstance.Field, Convert.ToString(nextVal), StoredOnlyType));
                                    if (VERBOSE)
                                    {
                                        Console.WriteLine("TEST: " + Thread.CurrentThread.Name + ": u id:" + id + " val=" + nextVal);
                                    }
                                    Writer.UpdateDocument(new Term("id", Convert.ToString(id)), d);
                                    if (Tombstones)
                                    {
                                        // remove tombstone after new addition (this should be optional?)
                                        Writer.DeleteDocuments(new Term("id", "-" + Convert.ToString(id)));
                                    }
                                    OuterInstance.Model[id] = nextVal;
                                }
                            }

                            if (!before)
                            {
                                OuterInstance.LastId = id;
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(Thread.CurrentThread.Name + ": FAILED: unexpected exception");
                    Console.WriteLine(e.StackTrace);
                    throw new Exception(e.Message, e);
                }
            }
        }

        private class ThreadAnonymousInnerClassHelper2 : ThreadClass
        {
            private readonly TestStressNRT OuterInstance;

            private int Ndocs;
            private bool Tombstones;
            private AtomicLong Operations;

            public ThreadAnonymousInnerClassHelper2(TestStressNRT outerInstance, string str, int ndocs, bool tombstones, AtomicLong operations)
                : base(str)
            {
                this.OuterInstance = outerInstance;
                this.Ndocs = ndocs;
                this.Tombstones = tombstones;
                this.Operations = operations;
                rand = new Random(Random().Next());
            }

            internal Random rand;

            public override void Run()
            {
                try
                {
                    IndexReader lastReader = null;
                    IndexSearcher lastSearcher = null;

                    while (Operations.DecrementAndGet() >= 0)
                    {
                        // bias toward a recently changed doc
                        int id = rand.Next(100) < 25 ? OuterInstance.LastId : rand.Next(Ndocs);

                        // when indexing, we update the index, then the model
                        // so when querying, we should first check the model, and then the index

                        long val;
                        DirectoryReader r;
                        lock (OuterInstance)
                        {
                            val = OuterInstance.CommittedModel[id];
                            r = OuterInstance.Reader;
                            r.IncRef();
                        }

                        if (VERBOSE)
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
                            searcher = OuterInstance.NewSearcher(r);
                            lastReader = r;
                            lastSearcher = searcher;
                        }
                        Query q = new TermQuery(new Term("id", Convert.ToString(id)));
                        TopDocs results = searcher.Search(q, 10);

                        if (results.TotalHits == 0 && Tombstones)
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

                        if (results.TotalHits == 0 && !Tombstones)
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
                                    Console.WriteLine("  docID=" + sd.Doc + " id:" + doc.Get("id") + " foundVal=" + doc.Get(OuterInstance.Field));
                                }
                                Assert.Fail("id=" + id + " reader=" + r + " totalHits=" + results.TotalHits);
                            }
                            Document doc_ = searcher.Doc(results.ScoreDocs[0].Doc);
                            long foundVal = Convert.ToInt64(doc_.Get(OuterInstance.Field));
                            if (foundVal < Math.Abs(val))
                            {
                                Assert.Fail("foundVal=" + foundVal + " val=" + val + " id=" + id + " reader=" + r);
                            }
                        }

                        r.DecRef();
                    }
                }
                catch (Exception e)
                {
                    Operations.Set((int)-1L);
                    Console.WriteLine(Thread.CurrentThread.Name + ": FAILED: unexpected exception");
                    Console.WriteLine(e.StackTrace);
                    throw new Exception(e.Message, e);
                }
            }
        }
    }
}