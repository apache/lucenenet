/**
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

using WhitespaceAnalyzer = Lucene.Net.Analysis.WhitespaceAnalyzer;
using Document = Lucene.Net.Documents.Document;
using Field = Lucene.Net.Documents.Field;
using IndexWriter = Lucene.Net.Index.IndexWriter;
using MaxFieldLength = Lucene.Net.Index.IndexWriter.MaxFieldLength;
using QueryParser = Lucene.Net.QueryParsers.QueryParser;
using Directory = Lucene.Net.Store.Directory;
using RAMDirectory = Lucene.Net.Store.RAMDirectory;
using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;

using BitSet = SupportClass.CollectionsSupport.BitSet;
using Thread = SupportClass.ThreadClass;

using Exception = System.Exception;
using InterruptedException = System.Threading.ThreadInterruptedException;
using IOException = System.IO.IOException;
using String = System.String;

using NUnit.Framework;

namespace Lucene.Net.Search
{


    /**
     * Tests the TimeLimitedCollector.  This test checks (1) search
     * correctness (regardless of timeout), (2) expected timeout behavior,
     * and (3) a sanity test with multiple searching threads.
     */
    [TestFixture()]
    public class TestTimeLimitedCollector : LuceneTestCase
    {
        private static readonly int SLOW_DOWN = 47;
        private static readonly long TIME_ALLOWED = 17 * SLOW_DOWN; // so searches can find about 17 docs.

        // max time allowed is relaxed for multithreading tests. 
        // the multithread case fails when setting this to 1 (no slack) and launching many threads (>2000).  
        // but this is not a real failure, just noise.
        private static readonly long MULTI_THREAD_SLACK = 7;

        private static readonly int N_DOCS = 3000;
        private static readonly int N_THREADS = 50;

        private Searcher searcher;
        private readonly String FIELD_NAME = "body";
        private Query query;

        public TestTimeLimitedCollector()
        {
        }

        /**
         * initializes searcher with a document set
         */
        [TestFixtureSetUp()]
        protected void setUp()
        {
            String[] docText = {
                "docThatNeverMatchesSoWeCanRequireLastDocCollectedToBeGreaterThanZero",
                "one blah three",
                "one foo three multiOne",
                "one foobar three multiThree",
                "blueberry pancakes",
                "blueberry pie",
                "blueberry strudel",
                "blueberry pizza",
            };
            Directory directory = new RAMDirectory();
            IndexWriter iw = new IndexWriter(directory, new WhitespaceAnalyzer(), true, MaxFieldLength.UNLIMITED);

            for (int i = 0; i < N_DOCS; i++)
            {
                add(docText[i % docText.Length], iw);
            }
            iw.Close();
            searcher = new IndexSearcher(directory);

            String qtxt = "one";
            for (int i = 0; i < docText.Length; i++)
            {
                qtxt += ' ' + docText[i]; // large query so that search will be longer
            }
            QueryParser queryParser = new QueryParser(FIELD_NAME, new WhitespaceAnalyzer());
            query = queryParser.Parse(qtxt);

            // warm the searcher
            searcher.Search(query, null, 1000);
        }

        [TestFixtureTearDown()]
        public void tearDown()
        {
            searcher.Close();
        }

        private void add(String value, IndexWriter iw)
        {
            Document d = new Document();
            d.Add(new Field(FIELD_NAME, value, Field.Store.NO, Field.Index.ANALYZED));
            iw.AddDocument(d);
        }

        private void search(HitCollector collector)
        {
            searcher.Search(query, collector);
        }

        /**
         * test search correctness with no timeout
         */
        [Test]
        public void testSearch()
        {
            doTestSearch();
        }

        private void doTestSearch()
        {
            int totalResults = 0;
            int totalTLCResults = 0;
            try
            {
                MyHitCollector myHc = new MyHitCollector();
                search(myHc);
                totalResults = myHc.hitCount();

                myHc = new MyHitCollector();
                long oneHour = 3600000;
                HitCollector tlCollector = createTimedCollector(myHc, oneHour, false);
                search(tlCollector);
                totalTLCResults = myHc.hitCount();
            }
            catch (Exception e)
            {
                Assert.IsTrue(false, "Unexpected exception: " + e); //==fail
            }
            Assert.AreEqual(totalResults, totalTLCResults, "Wrong number of results!");
        }

        private HitCollector createTimedCollector(MyHitCollector hc, long timeAllowed, bool greedy)
        {
            TimeLimitedCollector res = new TimeLimitedCollector(hc, timeAllowed);
            res.setGreedy(greedy); // set to true to make sure at least one doc is collected.
            return res;
        }

        /**
         * Test that timeout is obtained, and soon enough!
         */
        [Test]
        public void testTimeoutGreedy()
        {
            doTestTimeout(false, true);
        }

        /**
         * Test that timeout is obtained, and soon enough!
         */
        [Test]
        public void testTimeoutNotGreedy()
        {
            doTestTimeout(false, false);
        }

        private void doTestTimeout(bool multiThreaded, bool greedy)
        {
            // setup
            MyHitCollector myHc = new MyHitCollector();
            myHc.setSlowDown(SLOW_DOWN);
            HitCollector tlCollector = createTimedCollector(myHc, TIME_ALLOWED, greedy);

            // search
            TimeLimitedCollector.TimeExceededException timoutException = null;
            try
            {
                search(tlCollector);
            }
            catch (TimeLimitedCollector.TimeExceededException x)
            {
                timoutException = x;
            }
            catch (Exception e)
            {
                Assert.IsTrue(false, "Unexpected exception: " + e); //==fail
            }

            // must get exception
            Assert.IsNotNull(timoutException, "Timeout expected!");

            // greediness affect last doc collected
            int exceptionDoc = timoutException.getLastDocCollected();
            int lastCollected = myHc.getLastDocCollected();
            Assert.IsTrue(exceptionDoc > 0, "doc collected at timeout must be > 0!");
            if (greedy)
            {
                Assert.IsTrue(exceptionDoc == lastCollected, "greedy=" + greedy + " exceptionDoc=" + exceptionDoc + " != lastCollected=" + lastCollected);
                Assert.IsTrue(myHc.hitCount() > 0, "greedy, but no hits found!");
            }
            else
            {
                Assert.IsTrue(exceptionDoc > lastCollected, "greedy=" + greedy + " exceptionDoc=" + exceptionDoc + " not > lastCollected=" + lastCollected);
            }

            // verify that elapsed time at exception is within valid limits
            Assert.AreEqual(timoutException.getTimeAllowed(), TIME_ALLOWED);
            // a) Not too early
            Assert.IsTrue(timoutException.getTimeElapsed() > TIME_ALLOWED - TimeLimitedCollector.getResolution(),
                "elapsed=" + timoutException.getTimeElapsed() + " <= (allowed-resolution)=" + (TIME_ALLOWED - TimeLimitedCollector.getResolution())
                );
            // b) Not too late.
            //    This part is problematic in a busy test system, so we just print a warning.
            //    We already verified that a timeout occurred, we just can't be picky about how long it took.
            if (timoutException.getTimeElapsed() > maxTime(multiThreaded))
            {
                System.Console.Out.WriteLine("Informative: timeout exceeded (no action required: most probably just " +
                  " because the test machine is slower than usual):  " +
                  "lastDoc=" + exceptionDoc +
                  " ,&& allowed=" + timoutException.getTimeAllowed() +
                  " ,&& elapsed=" + timoutException.getTimeElapsed() +
                  " >= " + maxTimeStr(multiThreaded));
            }
        }

        private long maxTime(bool multiThreaded)
        {
            long res = 2 * TimeLimitedCollector.getResolution() + TIME_ALLOWED + SLOW_DOWN; // some slack for less noise in this test
            if (multiThreaded)
            {
                res *= MULTI_THREAD_SLACK; // larger slack  
            }
            return res;
        }

        private String maxTimeStr(bool multiThreaded)
        {
            String s =
              "( " +
              "2*resolution +  TIME_ALLOWED + SLOW_DOWN = " +
              "2*" + TimeLimitedCollector.getResolution() + " + " + TIME_ALLOWED + " + " + SLOW_DOWN +
              ")";
            if (multiThreaded)
            {
                s = MULTI_THREAD_SLACK + " * " + s;
            }
            return maxTime(multiThreaded) + " = " + s;
        }

        /**
         * Test timeout behavior when resolution is modified. 
         */
        [Test]
        public void testModifyResolution()
        {
            try
            {
                // increase and test
                uint resolution = 20 * TimeLimitedCollector.DEFAULT_RESOLUTION; //400
                //TimeLimitedCollector.setResolution(resolution);
                //Assert.AreEqual(resolution, TimeLimitedCollector.getResolution());
                doTestTimeout(false, true);
                // decrease much and test
                resolution = 5;
                //TimeLimitedCollector.setResolution(resolution);
                //Assert.AreEqual(resolution, TimeLimitedCollector.getResolution());
                doTestTimeout(false, true);
                // return to default and test
                resolution = TimeLimitedCollector.DEFAULT_RESOLUTION;
                //TimeLimitedCollector.setResolution(resolution);
                //Assert.AreEqual(resolution, TimeLimitedCollector.getResolution());
                doTestTimeout(false, true);
            }
            finally
            {
                TimeLimitedCollector.setResolution(TimeLimitedCollector.DEFAULT_RESOLUTION);
            }
        }

        /** 
         * Test correctness with multiple searching threads.
         */
        [Test]
        public void testSearchMultiThreaded()
        {
            doTestMultiThreads(false);
        }

        /** 
         * Test correctness with multiple searching threads.
         */
        [Test]
        public void testTimeoutMultiThreaded()
        {
            doTestMultiThreads(true);
        }

        internal class AnonymousClassThread : Thread
        {
            private TestTimeLimitedCollector enclosingInstance;
            private BitSet success;
            private bool withTimeout;
            private int num;

            internal AnonymousClassThread(TestTimeLimitedCollector enclosingInstance, BitSet success, bool withTimeout, int num)
                : base()
            {
                this.enclosingInstance = enclosingInstance;
                this.success = success;
                this.withTimeout = withTimeout;
                this.num = num;
            }

            override public void Run()
            {
                if (withTimeout)
                {
                    enclosingInstance.doTestTimeout(true, true);
                }
                else
                {
                    enclosingInstance.doTestSearch();
                }
                lock (success)
                {
                    success.Set(num);
                }
            }
        }

        private void doTestMultiThreads(bool withTimeout)
        {
            Thread[] threadArray = new Thread[N_THREADS];
            BitSet success = new BitSet(N_THREADS);
            for (int i = 0; i < threadArray.Length; ++i)
            {
                int num = i;
                threadArray[num] = new AnonymousClassThread(this, success, withTimeout, num);
            }
            for (int i = 0; i < threadArray.Length; ++i)
            {
                threadArray[i].Start();
            }
            bool interrupted = false;
            for (int i = 0; i < threadArray.Length; ++i)
            {
                try
                {
                    threadArray[i].Join();
                }
                catch (InterruptedException)
                {
                    interrupted = true;
                }
            }
            if (interrupted)
            {
                Thread.CurrentThread().Interrupt();
            }
            Assert.AreEqual(N_THREADS, success.Cardinality(), "some threads failed!");
        }

        // counting hit collector that can slow down at collect().
        private class MyHitCollector : HitCollector
        {
            private readonly BitSet bits = new BitSet();
            private int slowdown = 0;
            private int lastDocCollected = -1;

            /**
             * amount of time to wait on each collect to simulate a long iteration
             */
            public void setSlowDown(int milliseconds)
            {
                slowdown = milliseconds;
            }

            override public void Collect(int doc, float score)
            {
                if (slowdown > 0)
                {
                    try
                    {
                        Thread.Sleep(slowdown);
                    }
                    catch (InterruptedException x)
                    {
                        System.Console.Out.WriteLine("caught " + x);
                    }
                }
                bits.Set(doc);
                lastDocCollected = doc;
            }

            public int hitCount()
            {
                return bits.Cardinality();
            }

            public int getLastDocCollected()
            {
                return lastDocCollected;
            }

        }

    }

}
