using Lucene.Net.Analysis;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Support;
using Lucene.Net.Util;
using NUnit.Framework;
using System;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading;

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

    /// <summary>
    /// Tests the {@link TimeLimitingCollector}.  This test checks (1) search
    /// correctness(regardless of timeout), (2) expected timeout behavior,
    /// and(3) a sanity test with multiple searching threads.
    /// </summary>
    public class TestTimeLimitingCollector : LuceneTestCase
    {
        private static readonly int SLOW_DOWN = 3;
        private static readonly long TIME_ALLOWED = 17 * SLOW_DOWN; // so searches can find about 17 docs.

        // max time allowed is relaxed for multithreading tests. 
        // the multithread case fails when setting this to 1 (no slack) and launching many threads (>2000).  
        // but this is not a real failure, just noise.
        private static readonly double MULTI_THREAD_SLACK = 7;

        private static readonly int N_DOCS = 3000;
        private static readonly int N_THREADS = 50;

        private IndexSearcher searcher;
        private Directory directory;
        private IndexReader reader;

        private readonly string FIELD_NAME = "body";
        private Query query;
        private Counter counter;
        private TimeLimitingCollector.TimerThread counterThread;

        /**
         * initializes searcher with a document set
         */
        public override void SetUp()
        {
            base.SetUp();
            counter = Lucene.Net.Util.Counter.NewCounter(true); 
            counterThread = new TimeLimitingCollector.TimerThread(counter);
            counterThread.Start();
            string[] docText = {
                "docThatNeverMatchesSoWeCanRequireLastDocCollectedToBeGreaterThanZero",
                "one blah three",
                "one foo three multiOne",
                "one foobar three multiThree",
                "blueberry pancakes",
                "blueberry pie",
                "blueberry strudel",
                "blueberry pizza",
            };
            directory = NewDirectory();
            RandomIndexWriter iw = new RandomIndexWriter(Random(), directory, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())).SetMergePolicy(NewLogMergePolicy()));

            for (int i = 0; i < N_DOCS; i++)
            {
                Add(docText[i % docText.Length], iw);
            }
            reader = iw.Reader;
            iw.Dispose();
            searcher = NewSearcher(reader);

            BooleanQuery booleanQuery = new BooleanQuery();
            booleanQuery.Add(new TermQuery(new Term(FIELD_NAME, "one")), BooleanClause.Occur.SHOULD);
            // start from 1, so that the 0th doc never matches
            for (int i = 1; i < docText.Length; i++)
            {
                string[] docTextParts = Regex.Split(docText[i], "\\s+");
                foreach (string docTextPart in docTextParts)
                {
                    // large query so that search will be longer
                    booleanQuery.Add(new TermQuery(new Term(FIELD_NAME, docTextPart)), BooleanClause.Occur.SHOULD);
                }
            }

            query = booleanQuery;

            // warm the searcher
            searcher.Search(query, null, 1000);
        }

        public override void TearDown()
        {
            reader.Dispose();
            directory.Dispose();
            counterThread.StopTimer();
            counterThread.Join();
            base.TearDown();
        }

        private void Add(string value, RandomIndexWriter iw)
        {
            Document d = new Document();
            d.Add(NewTextField(FIELD_NAME, value, Field.Store.NO));
            iw.AddDocument(d);
        }

        private void Search(Collector collector)
        {
            searcher.Search(query, collector);
        }

        /**
         * test search correctness with no timeout
         */
        [Test]
        public void TestSearch()
        {
            DoTestSearch();
        }

        private void DoTestSearch()
        {
            int totalResults = 0;
            int totalTLCResults = 0;
            try
            {
                MyHitCollector myHc = new MyHitCollector();
                Search(myHc);
                totalResults = myHc.HitCount();

                myHc = new MyHitCollector();
                long oneHour = 3600000;
                Collector tlCollector = CreateTimedCollector(myHc, oneHour, false);
                Search(tlCollector);
                totalTLCResults = myHc.HitCount();
            }
            catch (Exception e)
            {
                e.printStackTrace();
                assertTrue("Unexpected exception: " + e, false); //==fail
            }
            assertEquals("Wrong number of results!", totalResults, totalTLCResults);
        }

        private Collector CreateTimedCollector(MyHitCollector hc, long timeAllowed, bool greedy)
        {
            TimeLimitingCollector res = new TimeLimitingCollector(hc, counter, timeAllowed);
            res.Greedy = (greedy); // set to true to make sure at least one doc is collected.
            return res;
        }

        /**
         * Test that timeout is obtained, and soon enough!
         */
        [Test]
        public void TestTimeoutGreedy()
        {
            DoTestTimeout(false, true);
        }

        /**
         * Test that timeout is obtained, and soon enough!
         */
        [Test]
        public void TestTimeoutNotGreedy()
        {
            DoTestTimeout(false, false);
        }

        private void DoTestTimeout(bool multiThreaded, bool greedy)
        {
            // setup
            MyHitCollector myHc = new MyHitCollector();
            myHc.SetSlowDown(SLOW_DOWN);
            Collector tlCollector = CreateTimedCollector(myHc, TIME_ALLOWED, greedy);

            // search
            TimeLimitingCollector.TimeExceededException timoutException = null;
            try
            {
                Search(tlCollector);
            }
            catch (TimeLimitingCollector.TimeExceededException x)
            {
                timoutException = x;
            }
            catch (Exception e)
            {
                assertTrue("Unexpected exception: " + e, false); //==fail
            }

            // must get exception
            assertNotNull("Timeout expected!", timoutException);

            // greediness affect last doc collected
            int exceptionDoc = timoutException.LastDocCollected;
            int lastCollected = myHc.LastDocCollected;
            assertTrue("doc collected at timeout must be > 0!", exceptionDoc > 0);
            if (greedy)
            {
                assertTrue("greedy=" + greedy + " exceptionDoc=" + exceptionDoc + " != lastCollected=" + lastCollected, exceptionDoc == lastCollected);
                assertTrue("greedy, but no hits found!", myHc.HitCount() > 0);
            }
            else
            {
                assertTrue("greedy=" + greedy + " exceptionDoc=" + exceptionDoc + " not > lastCollected=" + lastCollected, exceptionDoc > lastCollected);
            }

            // verify that elapsed time at exception is within valid limits
            assertEquals(timoutException.TimeAllowed, TIME_ALLOWED);
            // a) Not too early
            assertTrue("elapsed=" + timoutException.TimeElapsed + " <= (allowed-resolution)=" + (TIME_ALLOWED - counterThread.Resolution),
                timoutException.TimeElapsed > TIME_ALLOWED - counterThread.Resolution);
            // b) Not too late.
            //    This part is problematic in a busy test system, so we just print a warning.
            //    We already verified that a timeout occurred, we just can't be picky about how long it took.
            if (timoutException.TimeElapsed > MaxTime(multiThreaded))
            {
                Console.WriteLine("Informative: timeout exceeded (no action required: most probably just " +
                  " because the test machine is slower than usual):  " +
                  "lastDoc=" + exceptionDoc +
                  " ,&& allowed=" + timoutException.TimeAllowed +
                  " ,&& elapsed=" + timoutException.TimeElapsed +
                  " >= " + MaxTimeStr(multiThreaded));
            }
        }

        private long MaxTime(bool multiThreaded)
        {
            long res = 2 * counterThread.Resolution + TIME_ALLOWED + SLOW_DOWN; // some slack for less noise in this test
            if (multiThreaded)
            {
                res = (long)(res * MULTI_THREAD_SLACK); // larger slack  
            }
            return res;
        }

        private string MaxTimeStr(bool multiThreaded)
        {
            string s =
              "( " +
              "2*resolution +  TIME_ALLOWED + SLOW_DOWN = " +
              "2*" + counterThread.Resolution + " + " + TIME_ALLOWED + " + " + SLOW_DOWN +
              ")";
            if (multiThreaded)
            {
                s = MULTI_THREAD_SLACK + " * " + s;
            }
            return MaxTime(multiThreaded) + " = " + s;
        }

        /**
         * Test timeout behavior when resolution is modified. 
         */
        [Test]
        public void TestModifyResolution()
        {
            try
            {
                // increase and test
                long resolution = 20 * TimeLimitingCollector.TimerThread.DEFAULT_RESOLUTION; //400
                counterThread.Resolution = (resolution);
                assertEquals(resolution, counterThread.Resolution);
                DoTestTimeout(false, true);
                // decrease much and test
                resolution = 5;
                counterThread.Resolution = (resolution);
                assertEquals(resolution, counterThread.Resolution);
                DoTestTimeout(false, true);
                // return to default and test
                resolution = TimeLimitingCollector.TimerThread.DEFAULT_RESOLUTION;
                counterThread.Resolution = (resolution);
                assertEquals(resolution, counterThread.Resolution);
                DoTestTimeout(false, true);
            }
            finally
            {
                counterThread.Resolution = (TimeLimitingCollector.TimerThread.DEFAULT_RESOLUTION);
            }
        }

        /** 
         * Test correctness with multiple searching threads.
         */
        [Test]
        public void TestSearchMultiThreaded()
        {
            DoTestMultiThreads(false);
        }

        /** 
         * Test correctness with multiple searching threads.
         */
        [Test]
        public void TestTimeoutMultiThreaded()
        {
            DoTestMultiThreads(true);
        }

        private void DoTestMultiThreads(bool withTimeout)
        {
            ThreadClass[] threadArray = new ThreadClass[N_THREADS];
            OpenBitSet success = new OpenBitSet(N_THREADS);
            for (int i = 0; i < threadArray.Length; ++i)
            {
                int num = i;
                threadArray[num] = new ThreadClassAnonymousHelper(this, success, withTimeout, num);
            }
            for (int i = 0; i < threadArray.Length; ++i)
            {
                threadArray[i].Start();
            }
            for (int i = 0; i < threadArray.Length; ++i)
            {
                threadArray[i].Join();
            }
            assertEquals("some threads failed!", N_THREADS, success.Cardinality());
        }

        internal class ThreadClassAnonymousHelper : ThreadClass
        {
            private readonly TestTimeLimitingCollector outerInstance;
            private readonly OpenBitSet success;
            private readonly bool withTimeout;
            private readonly int num;
            public ThreadClassAnonymousHelper(TestTimeLimitingCollector outerInstance, OpenBitSet success, bool withTimeout, int num)
            {
                this.outerInstance = outerInstance;
                this.success = success;
                this.withTimeout = withTimeout;
                this.num = num;
            }
            public override void Run()
            {
                if (withTimeout)
                {
                    outerInstance.DoTestTimeout(true, true);
                }
                else
                {
                    outerInstance.DoTestSearch();
                }
                lock (success)
                {
                    success.Set(num);
                }
            }
        }

        // counting collector that can slow down at collect().
        internal class MyHitCollector : Collector
        {
            private readonly OpenBitSet bits = new OpenBitSet();
            private int slowdown = 0;
            private int lastDocCollected = -1;
            private int docBase = 0;

            /**
             * amount of time to wait on each collect to simulate a long iteration
             */
            public void SetSlowDown(int milliseconds)
            {
                slowdown = milliseconds;
            }

            public int HitCount()
            {
                return (int)bits.Cardinality();
            }

            public int LastDocCollected
            {
                get
                {
                    return lastDocCollected;
                }
            }

            public override Scorer Scorer
            {
                set
                {
                    // scorer is not needed
                }
            }

            public override void Collect(int doc)
            {
                int docId = doc + docBase;
                if (slowdown > 0)
                {
                    try
                    {
                        ThreadClass.Sleep(slowdown);
                    }
#if NETSTANDARD
                    catch (Exception)
                    {
                        throw;
                    }
#else
                    catch (ThreadInterruptedException ie)
                    {
                        throw;
                    }
#endif
                }
                Debug.Assert(docId >= 0, " base=" + docBase + " doc=" + doc);
                bits.Set(docId);
                lastDocCollected = docId;
            }

            public override AtomicReaderContext NextReader
            {
                set
                {
                    docBase = value.DocBase;
                }
            }

            public override bool AcceptsDocsOutOfOrder()
            {
                return false;
            }

        }

    }
}
