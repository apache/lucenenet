using J2N.Threading;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using JCG = J2N.Collections.Generic;
using Assert = Lucene.Net.TestFramework.Assert;
using Console = Lucene.Net.Util.SystemConsole;
using RandomizedTesting.Generators;

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
    using Document = Documents.Document;
    using Field = Field;
    using IndexReader = Lucene.Net.Index.IndexReader;
    using IndexWriter = Lucene.Net.Index.IndexWriter;
    using IndexWriterConfig = Lucene.Net.Index.IndexWriterConfig;
    using Int32Field = Int32Field;
    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
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
            IndexWriterConfig iwc = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random));

            IndexWriter w = new IndexWriter(dir, iwc);

            SearcherManager mgr = new SearcherManager(w, true, new SearcherFactoryAnonymousClass());

            const int missing = -1;

            LiveFieldValues<IndexSearcher, int?> rt = new LiveFieldValuesAnonymousClass(mgr, missing);

            int numThreads = TestUtil.NextInt32(Random, 2, 5);
            if (Verbose)
            {
                Console.WriteLine(numThreads + " threads");
            }

            CountdownEvent startingGun = new CountdownEvent(1);
            IList<ThreadJob> threads = new JCG.List<ThreadJob>();

            int iters = AtLeast(1000);
            int idCount = TestUtil.NextInt32(Random, 100, 10000);

            double reopenChance = Random.NextDouble() * 0.01;
            double deleteChance = Random.NextDouble() * 0.25;
            double addChance = Random.NextDouble() * 0.5;

            for (int t = 0; t < numThreads; t++)
            {
                int threadID = t;
                Random threadRandom = new J2N.Randomizer(Random.NextInt64());
                ThreadJob thread = new ThreadAnonymousClass(w, mgr, missing, rt, startingGun, iters, idCount, reopenChance, deleteChance, addChance, t, threadID, threadRandom);
                threads.Add(thread);
                thread.Start();
            }

            startingGun.Signal();

            foreach (ThreadJob thread in threads)
            {
                thread.Join();
            }
            mgr.MaybeRefresh();
            Assert.AreEqual(0, rt.Count);

            rt.Dispose();
            mgr.Dispose();
            w.Dispose();
            dir.Dispose();
        }

        private sealed class SearcherFactoryAnonymousClass : SearcherFactory
        {
            public override IndexSearcher NewSearcher(IndexReader r)
            {
                return new IndexSearcher(r);
            }
        }

        private sealed class LiveFieldValuesAnonymousClass : LiveFieldValues<IndexSearcher, int?>
        {
            public LiveFieldValuesAnonymousClass(SearcherManager mgr, int missing)
                : base(mgr, missing)
            {
            }

            protected override int? LookupFromSearcher(IndexSearcher s, string id)
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
                    return doc.GetField("field").GetInt32Value();
                }
            }
        }

        private sealed class ThreadAnonymousClass : ThreadJob
        {
            private readonly IndexWriter w;
            private readonly SearcherManager mgr;
            private readonly int? missing;
            private readonly LiveFieldValues<IndexSearcher, int?> rt;
            private readonly CountdownEvent startingGun;
            private readonly int iters;
            private readonly int idCount;
            private readonly double reopenChance;
            private readonly double deleteChance;
            private readonly double addChance;
            private readonly int t;
            private readonly int threadID;
            private readonly Random threadRandom;

            public ThreadAnonymousClass(IndexWriter w, SearcherManager mgr, int? missing, LiveFieldValues<IndexSearcher, int?> rt, CountdownEvent startingGun, int iters, int idCount, double reopenChance, double deleteChance, double addChance, int t, int threadID, Random threadRandom)
            {
                this.w = w;
                this.mgr = mgr;
                this.missing = missing;
                this.rt = rt;
                this.startingGun = startingGun;
                this.iters = iters;
                this.idCount = idCount;
                this.reopenChance = reopenChance;
                this.deleteChance = deleteChance;
                this.addChance = addChance;
                this.t = t;
                this.threadID = threadID;
                this.threadRandom = threadRandom;
            }

            public override void Run()
            {
                try
                {
                    IDictionary<string, int?> values = new Dictionary<string, int?>();
                    IList<string> allIDs = new SynchronizedList<string>();

                    startingGun.Wait();
                    for (int iter = 0; iter < iters; iter++)
                    {
                        // Add/update a document
                        Document doc = new Document();
                        // Threads must not update the same id at the
                        // same time:
                        if (threadRandom.NextDouble() <= addChance)
                        {
                            string id = string.Format(CultureInfo.InvariantCulture, "{0}_{1:X4}", threadID, threadRandom.Next(idCount));
                            int field = threadRandom.Next(int.MaxValue);
                            doc.Add(new StringField("id", id, Field.Store.YES));
                            doc.Add(new Int32Field("field", (int)field, Field.Store.YES));
                            w.UpdateDocument(new Term("id", id), doc);
                            rt.Add(id, field);
                            if (!values.ContainsKey(id))//Key didn't exist before
                            {
                                allIDs.Add(id);
                            }
                            values[id] = field;
                        }

                        if (allIDs.Count > 0 && threadRandom.NextDouble() <= deleteChance)
                        {
                            string randomID = allIDs[threadRandom.Next(allIDs.Count)];
                            w.DeleteDocuments(new Term("id", randomID));
                            rt.Delete(randomID);
                            values[randomID] = missing;
                        }

                        if (threadRandom.NextDouble() <= reopenChance || rt.Count > 10000)
                        {
                            //System.out.println("refresh @ " + rt.Size());
                            mgr.MaybeRefresh();
                            if (Verbose)
                            {
                                IndexSearcher s = mgr.Acquire();
                                try
                                {
                                    Console.WriteLine("TEST: reopen " + s);
                                }
                                finally
                                {
                                    mgr.Release(s);
                                }
                                Console.WriteLine("TEST: " + values.Count + " values");
                            }
                        }

                        if (threadRandom.Next(10) == 7)
                        {
                            Assert.AreEqual(null, rt.Get("foo"));
                        }

                        if (allIDs.Count > 0)
                        {
                            string randomID = allIDs[threadRandom.Next(allIDs.Count)];
                            int? expected = values[randomID];
                            if (expected == missing)
                            {
                                expected = null;
                            }
                            Assert.AreEqual(expected, rt.Get(randomID), "id=" + randomID);
                        }
                    }
                }
                catch (Exception t) when (t.IsThrowable())
                {
                    throw RuntimeException.Create(t);
                }
            }
        }
    }
}