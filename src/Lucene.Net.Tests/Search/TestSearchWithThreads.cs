using J2N.Threading;
using J2N.Threading.Atomic;
using Lucene.Net.Documents;
using NUnit.Framework;
using RandomizedTesting.Generators;
using System;
using System.Text;
using Assert = Lucene.Net.TestFramework.Assert;
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
            RandomIndexWriter w = new RandomIndexWriter(Random, dir);

            long startTime = J2N.Time.NanoTime() / J2N.Time.MillisecondsPerNanosecond; // LUCENENET: Use NanoTime() rather than CurrentTimeMilliseconds() for more accurate/reliable results

            // TODO: replace w/ the @nightly test data; make this
            // into an optional @nightly stress test
            Document doc = new Document();
            Field body = NewTextField("body", "", Field.Store.NO);
            doc.Add(body);
            StringBuilder sb = new StringBuilder();
            for (int docCount = 0; docCount < NUM_DOCS; docCount++)
            {
                int numTerms = Random.Next(10);
                for (int termCount = 0; termCount < numTerms; termCount++)
                {
                    sb.Append(Random.NextBoolean() ? "aaa" : "bbb");
                    sb.Append(' ');
                }
                body.SetStringValue(sb.ToString());
                w.AddDocument(doc);
                sb.Remove(0, sb.Length);
            }
            IndexReader r = w.GetReader();
            w.Dispose();

            long endTime = J2N.Time.NanoTime() / J2N.Time.MillisecondsPerNanosecond; // LUCENENET: Use NanoTime() rather than CurrentTimeMilliseconds() for more accurate/reliable results
            if (Verbose)
            {
                Console.WriteLine("BUILD took " + (endTime - startTime));
            }

            IndexSearcher s = NewSearcher(r);

            AtomicBoolean failed = new AtomicBoolean();
            AtomicInt64 netSearch = new AtomicInt64();

            ThreadJob[] threads = new ThreadJob[NUM_SEARCH_THREADS];
            for (int threadID = 0; threadID < NUM_SEARCH_THREADS; threadID++)
            {
                threads[threadID] = new ThreadAnonymousClass(this, s, failed, netSearch);
                threads[threadID].IsBackground = (true);
            }

            foreach (ThreadJob t in threads)
            {
                t.Start();
            }

            foreach (ThreadJob t in threads)
            {
                t.Join();
            }

            if (Verbose)
            {
                Console.WriteLine(NUM_SEARCH_THREADS + " threads did " + netSearch + " searches");
            }

            r.Dispose();
            dir.Dispose();
        }

        private sealed class ThreadAnonymousClass : ThreadJob
        {
            private readonly TestSearchWithThreads outerInstance;

            private readonly IndexSearcher s;
            private readonly AtomicBoolean failed;
            private readonly AtomicInt64 netSearch;

            public ThreadAnonymousClass(TestSearchWithThreads outerInstance, IndexSearcher s, AtomicBoolean failed, AtomicInt64 netSearch)
            {
                this.outerInstance = outerInstance;
                this.s = s;
                this.failed = failed;
                this.netSearch = netSearch;
                col = new TotalHitCountCollector();
            }

            internal TotalHitCountCollector col;

            public override void Run()
            {
                try
                {
                    long totHits = 0;
                    long totSearch = 0;
                    long stopAt = (J2N.Time.NanoTime() / J2N.Time.MillisecondsPerNanosecond) + outerInstance.RUN_TIME_MSEC; // LUCENENET: Use NanoTime() rather than CurrentTimeMilliseconds() for more accurate/reliable results
                    while (J2N.Time.NanoTime() / J2N.Time.MillisecondsPerNanosecond < stopAt && !failed) // LUCENENET: Use NanoTime() rather than CurrentTimeMilliseconds() for more accurate/reliable results
                    {
                        s.Search(new TermQuery(new Term("body", "aaa")), col);
                        totHits += col.TotalHits;
                        s.Search(new TermQuery(new Term("body", "bbb")), col);
                        totHits += col.TotalHits;
                        totSearch++;
                    }
                    Assert.IsTrue(totSearch > 0 && totHits > 0);
                    netSearch.AddAndGet(totSearch);
                }
                catch (Exception exc) when (exc.IsException())
                {
                    failed.Value = (true);
                    throw RuntimeException.Create(exc);
                }
            }
        }
    }
}