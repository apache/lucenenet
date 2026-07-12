using J2N;
using J2N.Threading;
using J2N.Threading.Atomic;
using Lucene.Net.Util;
using NUnit.Framework;
using System;
using Assert = Lucene.Net.TestFramework.Assert;
using ThreadInterruptedException = Lucene.Net.Util.ThreadInterruptedException;

namespace Lucene.Net.Store
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

    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
    using SimpleRateLimiter = Lucene.Net.Store.RateLimiter.SimpleRateLimiter;

    /// <summary>
    /// Simple testcase for RateLimiter.SimpleRateLimiter
    /// </summary>
    [TestFixture]
    public sealed class TestRateLimiter : LuceneTestCase
    {
        [Test]
        public void TestPause()
        {
            SimpleRateLimiter limiter = new SimpleRateLimiter(10); // 10 MB / Sec
            limiter.Pause(2); //init
            long pause = 0;
            for (int i = 0; i < 3; i++)
            {
                pause += limiter.Pause(4 * 1024 * 1024); // fire up 3 * 4 MB
            }
            //long convert = TimeUnit.MILLISECONDS.convert(pause, TimeUnit.NANOSECONDS);

            // 1000000 Milliseconds per nanosecond
            long convert = pause / 1000000;
            Assert.IsTrue(convert < 2000L, "we should sleep less than 2 seconds but did: " + convert + " millis");
            Assert.IsTrue(convert > 1000L, "we should sleep at least 1 second but did only: " + convert + " millis");
        }

        [Test]
        public void TestThreads()
        {
            double targetMBPerSec = 10.0 + 20 * Random.NextDouble();
            SimpleRateLimiter limiter = new SimpleRateLimiter(targetMBPerSec);

            CountdownLatch startingGun = new CountdownLatch(1);

            ThreadJob[] threads = new ThreadJob[TestUtil.NextInt32(Random, 3, 6)];
            AtomicInt64 totBytes = new AtomicInt64();
            for (int i = 0; i < threads.Length; i++)
            {
                threads[i] = new TestThreadsThreadJobAnonymousClass(startingGun, totBytes, limiter);
                threads[i].Start();
            }

            long startNS = Time.NanoTime();
            startingGun.Signal();
            foreach (ThreadJob thread in threads)
            {
                thread.Join();
            }
            long endNS = Time.NanoTime();
            double actualMBPerSec = (totBytes.Value/1024.0/1024.0)/((endNS-startNS)/1000000000.0);

            // TODO: this may false trip .... could be we can only assert that it never exceeds the max, so slow jenkins doesn't trip:
            double ratio = actualMBPerSec/targetMBPerSec;

            // LUCENENET: backport commits 090b804 (Lucene 6.0.0) and a893aaa (Lucene 7.0.0) with assertion fixes for test reliability
            // Only enforce that it wasn't too fast; if machine is bogged down (can't schedule threads / sleep properly) then it may falsely be too slow:
            AssumeTrue("actualMBPerSec=" + actualMBPerSec + " targetMBPerSec=" + targetMBPerSec, 0.9 <= ratio);
            Assert.IsTrue(ratio <= 1.1, "targetMBPerSec=" + targetMBPerSec + " actualMBPerSec=" + actualMBPerSec);
        }

        private class TestThreadsThreadJobAnonymousClass(
            CountdownLatch startingGun,
            AtomicInt64 totBytes,
            SimpleRateLimiter limiter)
            : ThreadJob
        {
            public override void Run()
            {
                try
                {
                    startingGun.Wait();
                }
                catch (Exception ie) when (ie.IsInterruptedException())
                {
                    throw new ThreadInterruptedException(ie);
                }

                long bytesSinceLastPause = 0;
                for (int i = 0; i < 500; i++)
                {
                    long numBytes = TestUtil.NextInt32(Random, 1000, 10000);
                    totBytes.AddAndGet(numBytes);
                    bytesSinceLastPause += numBytes;
                    if (bytesSinceLastPause > limiter.MinPauseCheckBytes)
                    {
                        limiter.Pause(bytesSinceLastPause);
                        bytesSinceLastPause = 0;
                    }
                }
            }
        }
    }
}
