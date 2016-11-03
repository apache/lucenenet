using Lucene.Net.Documents;
using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;

namespace Lucene.Net.Search
{
    using Index;
    using NUnit.Framework;
    using Directory = Lucene.Net.Store.Directory;
    using Document = Documents.Document;
    using Field = Field;
    using IndexReader = Lucene.Net.Index.IndexReader;
    using IndexWriter = Lucene.Net.Index.IndexWriter;
    using IndexWriterConfig = Lucene.Net.Index.IndexWriterConfig;
    using IntField = IntField;
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
    using StringField = StringField;
    using Term = Lucene.Net.Index.Term;
    using TestUtil = Lucene.Net.Util.TestUtil;

    [TestFixture]
    public class TestLiveFieldValues : LuceneTestCase
    {
        [Test]
        public virtual void Test()
        {
            Directory dir = NewFSDirectory(CreateTempDir("livefieldupdates"));
            IndexWriterConfig iwc = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random()));

            IndexWriter w = new IndexWriter(dir, iwc);

            SearcherManager mgr = new SearcherManager(w, true, new SearcherFactoryAnonymousInnerClassHelper());

            const int missing = -1;

            LiveFieldValues<IndexSearcher, int?> rt = new LiveFieldValuesAnonymousInnerClassHelper(mgr, missing);

            int numThreads = TestUtil.NextInt(Random(), 2, 5);
            if (VERBOSE)
            {
                Console.WriteLine(numThreads + " threads");
            }

            CountdownEvent startingGun = new CountdownEvent(1);
            IList<ThreadClass> threads = new List<ThreadClass>();

            int iters = AtLeast(1000);
            int idCount = TestUtil.NextInt(Random(), 100, 10000);

            double reopenChance = Random().NextDouble() * 0.01;
            double deleteChance = Random().NextDouble() * 0.25;
            double addChance = Random().NextDouble() * 0.5;

            for (int t = 0; t < numThreads; t++)
            {
                int threadID = t;
                Random threadRandom = new Random(Random().Next());
                ThreadClass thread = new ThreadAnonymousInnerClassHelper(w, mgr, missing, rt, startingGun, iters, idCount, reopenChance, deleteChance, addChance, t, threadID, threadRandom);
                threads.Add(thread);
                thread.Start();
            }

            startingGun.Signal();

            foreach (ThreadClass thread in threads)
            {
                thread.Join();
            }
            mgr.MaybeRefresh();
            Assert.AreEqual(0, rt.Size());

            rt.Dispose();
            mgr.Dispose();
            w.Dispose();
            dir.Dispose();
        }

        private class SearcherFactoryAnonymousInnerClassHelper : SearcherFactory
        {
            public override IndexSearcher NewSearcher(IndexReader r)
            {
                return new IndexSearcher(r);
            }
        }

        private class LiveFieldValuesAnonymousInnerClassHelper : LiveFieldValues<IndexSearcher, int?>
        {
            public LiveFieldValuesAnonymousInnerClassHelper(SearcherManager mgr, int missing)
                : base(mgr, missing)
            {
            }

            protected internal override int? LookupFromSearcher(IndexSearcher s, string id)
            {
                TermQuery tq = new TermQuery(new Term("id", id));
                TopDocs hits = s.Search(tq, 1);
                Assert.IsTrue(hits.TotalHits <= 1);
                if (hits.TotalHits == 0)
                {
                    return null;
                }
                else
                {
                    Document doc = s.Doc(hits.ScoreDocs[0].Doc);
                    return (int)doc.GetField("field").NumericValue;
                }
            }
        }

        private class ThreadAnonymousInnerClassHelper : ThreadClass
        {
            private IndexWriter w;
            private SearcherManager Mgr;
            private int? Missing;
            private LiveFieldValues<IndexSearcher, int?> Rt;
            private CountdownEvent StartingGun;
            private int Iters;
            private int IdCount;
            private double ReopenChance;
            private double DeleteChance;
            private double AddChance;
            private int t;
            private int ThreadID;
            private Random ThreadRandom;

            public ThreadAnonymousInnerClassHelper(IndexWriter w, SearcherManager mgr, int? missing, LiveFieldValues<IndexSearcher, int?> rt, CountdownEvent startingGun, int iters, int idCount, double reopenChance, double deleteChance, double addChance, int t, int threadID, Random threadRandom)
            {
                this.w = w;
                this.Mgr = mgr;
                this.Missing = missing;
                this.Rt = rt;
                this.StartingGun = startingGun;
                this.Iters = iters;
                this.IdCount = idCount;
                this.ReopenChance = reopenChance;
                this.DeleteChance = deleteChance;
                this.AddChance = addChance;
                this.t = t;
                this.ThreadID = threadID;
                this.ThreadRandom = threadRandom;
            }

            public override void Run()
            {
                try
                {
                    IDictionary<string, int?> values = new Dictionary<string, int?>();
                    IList<string> allIDs = new SynchronizedList<string>();

                    StartingGun.Wait();
                    for (int iter = 0; iter < Iters; iter++)
                    {
                        // Add/update a document
                        Document doc = new Document();
                        // Threads must not update the same id at the
                        // same time:
                        if (ThreadRandom.NextDouble() <= AddChance)
                        {
                            string id = string.Format(CultureInfo.InvariantCulture, "{0}_{1:X4}", ThreadID, ThreadRandom.Next(IdCount));
                            int field = ThreadRandom.Next(int.MaxValue);
                            doc.Add(new StringField("id", id, Field.Store.YES));
                            doc.Add(new IntField("field", (int)field, Field.Store.YES));
                            w.UpdateDocument(new Term("id", id), doc);
                            Rt.Add(id, field);
                            if (!values.ContainsKey(id))//Key didn't exist before
                            {
                                allIDs.Add(id);
                            }
                            values[id] = field;
                        }

                        if (allIDs.Count > 0 && ThreadRandom.NextDouble() <= DeleteChance)
                        {
                            string randomID = allIDs[ThreadRandom.Next(allIDs.Count)];
                            w.DeleteDocuments(new Term("id", randomID));
                            Rt.Delete(randomID);
                            values[randomID] = Missing;
                        }

                        if (ThreadRandom.NextDouble() <= ReopenChance || Rt.Size() > 10000)
                        {
                            //System.out.println("refresh @ " + rt.Size());
                            Mgr.MaybeRefresh();
                            if (VERBOSE)
                            {
                                IndexSearcher s = Mgr.Acquire();
                                try
                                {
                                    Console.WriteLine("TEST: reopen " + s);
                                }
                                finally
                                {
                                    Mgr.Release(s);
                                }
                                Console.WriteLine("TEST: " + values.Count + " values");
                            }
                        }

                        if (ThreadRandom.Next(10) == 7)
                        {
                            Assert.AreEqual(null, Rt.Get("foo"));
                        }

                        if (allIDs.Count > 0)
                        {
                            string randomID = allIDs[ThreadRandom.Next(allIDs.Count)];
                            int? expected = values[randomID];
                            if (expected == Missing)
                            {
                                expected = null;
                            }
                            Assert.AreEqual(expected, Rt.Get(randomID), "id=" + randomID);
                        }
                    }
                }
                catch (Exception t)
                {
                    throw new Exception(t.Message, t);
                }
            }
        }
    }
}