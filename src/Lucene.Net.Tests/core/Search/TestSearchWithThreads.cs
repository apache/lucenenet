using System;
using System.Text;
using Lucene.Net.Documents;

namespace Lucene.Net.Search
{
    using Lucene.Net.Randomized.Generators;
    using Lucene.Net.Support;
    using NUnit.Framework;
    using Directory = Lucene.Net.Store.Directory;

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

    using Document = Documents.Document;
    using Field = Field;
    using IndexReader = Lucene.Net.Index.IndexReader;
    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
    using RandomIndexWriter = Lucene.Net.Index.RandomIndexWriter;
    using Term = Lucene.Net.Index.Term;

    [SuppressCodecs("SimpleText", "Memory", "Direct")]
    [TestFixture]
    public class TestSearchWithThreads : LuceneTestCase
    {
        internal int NUM_DOCS;
        internal readonly int NUM_SEARCH_THREADS = 5;
        internal int RUN_TIME_MSEC;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            NUM_DOCS = AtLeast(10000);
            RUN_TIME_MSEC = AtLeast(1000);
        }

        [Test]
        public virtual void Test()
        {
            Directory dir = NewDirectory();
            RandomIndexWriter w = new RandomIndexWriter(Random(), dir, Similarity, TimeZone);

            long startTime = Environment.TickCount;

            // TODO: replace w/ the @nightly test data; make this
            // into an optional @nightly stress test
            Document doc = new Document();
            Field body = NewTextField("body", "", Field.Store.NO);
            doc.Add(body);
            StringBuilder sb = new StringBuilder();
            for (int docCount = 0; docCount < NUM_DOCS; docCount++)
            {
                int numTerms = Random().Next(10);
                for (int termCount = 0; termCount < numTerms; termCount++)
                {
                    sb.Append(Random().NextBoolean() ? "aaa" : "bbb");
                    sb.Append(' ');
                }
                body.StringValue = sb.ToString();
                w.AddDocument(doc);
                sb.Remove(0, sb.Length);
            }
            IndexReader r = w.Reader;
            w.Dispose();

            long endTime = Environment.TickCount;
            if (VERBOSE)
            {
                Console.WriteLine("BUILD took " + (endTime - startTime));
            }

            IndexSearcher s = NewSearcher(r);

            AtomicBoolean failed = new AtomicBoolean();
            AtomicLong netSearch = new AtomicLong();

            ThreadClass[] threads = new ThreadClass[NUM_SEARCH_THREADS];
            for (int threadID = 0; threadID < NUM_SEARCH_THREADS; threadID++)
            {
                threads[threadID] = new ThreadAnonymousInnerClassHelper(this, s, failed, netSearch);
                threads[threadID].SetDaemon(true);
            }

            foreach (ThreadClass t in threads)
            {
                t.Start();
            }

            foreach (ThreadClass t in threads)
            {
                t.Join();
            }

            if (VERBOSE)
            {
                Console.WriteLine(NUM_SEARCH_THREADS + " threads did " + netSearch.Get() + " searches");
            }

            r.Dispose();
            dir.Dispose();
        }

        private class ThreadAnonymousInnerClassHelper : ThreadClass
        {
            private readonly TestSearchWithThreads OuterInstance;

            private IndexSearcher s;
            private AtomicBoolean Failed;
            private AtomicLong NetSearch;

            public ThreadAnonymousInnerClassHelper(TestSearchWithThreads outerInstance, IndexSearcher s, AtomicBoolean failed, AtomicLong netSearch)
            {
                this.OuterInstance = outerInstance;
                this.s = s;
                this.Failed = failed;
                this.NetSearch = netSearch;
                col = new TotalHitCountCollector();
            }

            internal TotalHitCountCollector col;

            public override void Run()
            {
                try
                {
                    long totHits = 0;
                    long totSearch = 0;
                    long stopAt = Environment.TickCount + OuterInstance.RUN_TIME_MSEC;
                    while (Environment.TickCount < stopAt && !Failed.Get())
                    {
                        s.Search(new TermQuery(new Term("body", "aaa")), col);
                        totHits += col.TotalHits;
                        s.Search(new TermQuery(new Term("body", "bbb")), col);
                        totHits += col.TotalHits;
                        totSearch++;
                    }
                    Assert.IsTrue(totSearch > 0 && totHits > 0);
                    NetSearch.AddAndGet(totSearch);
                }
                catch (Exception exc)
                {
                    Failed.Set(true);
                    throw new Exception(exc.Message, exc);
                }
            }
        }
    }
}