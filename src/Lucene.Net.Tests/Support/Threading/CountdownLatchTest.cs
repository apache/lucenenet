// Some code adapted from Apache Harmony: https://github.com/apache/harmony/blob/02970cb7227a335edd2c8457ebdde0195a735733/classlib/modules/concurrent/src/test/java/CountDownLatchTest.java

using J2N.Threading;
using Lucene.Net.Support.Threading;
using NUnit.Framework;
using System;
using System.Threading;
using Assert = Lucene.Net.TestFramework.Assert;

namespace Lucene.Net.Support.Threading
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

    [TestFixture]
    public class CountdownLatchTest : JSR166TestCase
    {
        //public static void main(String[] args)
        //{
        //    junit.textui.TestRunner.run(suite());
        //}
        //public static Test suite()
        //{
        //    return new TestSuite(CountDownLatchTest.class);
        //}

        /**
         * negative constructor argument throws IAE
         */
        [Test]
        public void TestConstructor()
        {
            try
            {
                _ = new CountdownLatch(-1);
                shouldThrow();
            }
            catch (ArgumentOutOfRangeException)
            {
                // success
            }
        }

        /**
         * getCount returns initial count and decreases after countDown
         */
        [Test]
        public void TestGetCount()
        {
            CountdownLatch l = new CountdownLatch(2);
            assertEquals(2, l.Count);
            l.Signal();
            assertEquals(1, l.Count);
        }

        /**
         * countDown decrements count when positive and has no effect when zero
         */
        [Test]
        public void TestSignal() // LUCENENET: renamed from TestCountDown (Signal() was CountDown())
        {
            CountdownLatch l = new CountdownLatch(1);
            assertEquals(1, l.Count);
            l.Signal();
            assertEquals(0, l.Count);
            l.Signal();
            assertEquals(0, l.Count);
        }

        /**
         * await returns after countDown to zero, but not before
         */
        [Test]
        public void TestWait() // LUCENENET: renamed from TestAwait (Wait() was Await())
        {
            CountdownLatch l = new CountdownLatch(2);

            ThreadJob t = new ThreadAnonymousClassForTestWait(this, l);
            t.Start();
            try
            {
                assertEquals(l.Count, 2);
                Thread.Sleep(SHORT_DELAY_MS);
                l.Signal();
                assertEquals(l.Count, 1);
                l.Signal();
                assertEquals(l.Count, 0);
                t.Join();
            }
            catch (Exception e) when (e.IsInterruptedException())
            {
                unexpectedException();
            }
        }

        private sealed class ThreadAnonymousClassForTestWait : ThreadJob // LUCENENET: renamed from ThreadAnonymousClassForTestAwait
        {
            private readonly CountdownLatchTest outerInstance;
            private readonly CountdownLatch l;

            public ThreadAnonymousClassForTestWait(CountdownLatchTest outerInstance, CountdownLatch l)
            {
                this.outerInstance = outerInstance;
                this.l = l;
            }

            public override void Run()
            {
                try
                {
                    outerInstance.threadAssertTrue(l.Count > 0);
                    l.Wait();
                    outerInstance.threadAssertTrue(l.Count == 0);
                }
                catch (Exception e) when (e.IsInterruptedException())
                {
                    outerInstance.threadUnexpectedException();
                }
            }
        }

        /**
         * timed await returns after countDown to zero
         */
        [Test]
        public void TestTimedWait() // LUCENENET: renamed from TestTimedAwait (Wait() was Await())
        {
            CountdownLatch l = new CountdownLatch(2);

            ThreadJob t = new ThreadAnonymousClassForTestTimedWait(this, l);
            t.Start();
            try
            {
                assertEquals(l.Count, 2);
                Thread.Sleep(SHORT_DELAY_MS);
                l.Signal();
                assertEquals(l.Count, 1);
                l.Signal();
                assertEquals(l.Count, 0);
                t.Join();
            }
            catch (Exception e) when (e.IsInterruptedException())
            {
                unexpectedException();
            }
        }

        private sealed class ThreadAnonymousClassForTestTimedWait : ThreadJob // LUCENENET: renamed from ThreadAnonymousClassForTestTimedAwait
        {
            private readonly CountdownLatchTest outerInstance;
            private readonly CountdownLatch l;

            public ThreadAnonymousClassForTestTimedWait(CountdownLatchTest outerInstance, CountdownLatch l)
            {
                this.outerInstance = outerInstance;
                this.l = l;
            }

            public override void Run()
            {
                try
                {
                    outerInstance.threadAssertTrue(l.Count > 0);
                    outerInstance.threadAssertTrue(l.Wait(TimeSpan.FromMilliseconds(SMALL_DELAY_MS)));
                }
                catch (Exception e) when (e.IsInterruptedException())
                {
                    outerInstance.threadUnexpectedException();
                }
            }
        }

        /**
         * await throws IE if interrupted before counted down
         */
        [Test]
        public void TestWait_InterruptedException() // LUCENENET: renamed from TestAwait_InterruptedException (Wait() was Await())
        {
            CountdownLatch l = new CountdownLatch(1);
            ThreadJob t = new ThreadAnonymousClassForTestWaitInterruptedException(this, l);
            t.Start();
            try
            {
                assertEquals(l.Count, 1);
                t.Interrupt();
                t.Join();
            }
            catch (Exception e) when (e.IsInterruptedException())
            {
                unexpectedException();
            }
        }

        private sealed class ThreadAnonymousClassForTestWaitInterruptedException : ThreadJob // LUCENENET: renamed from ThreadAnonymousClassForTestAwaitInterruptedException
        {
            private readonly CountdownLatchTest outerInstance;
            private readonly CountdownLatch l;

            public ThreadAnonymousClassForTestWaitInterruptedException(CountdownLatchTest outerInstance, CountdownLatch l)
            {
                this.outerInstance = outerInstance;
                this.l = l;
            }

            public override void Run()
            {
                try
                {
                    outerInstance.threadAssertTrue(l.Count > 0);
                    l.Wait();
                    outerInstance.threadShouldThrow();
                }
                catch (Exception e) when (e.IsInterruptedException())
                {
                    // success
                }
            }
        }

        /**
         * timed await throws IE if interrupted before counted down
         */
        [Test]
        public void TestTimedWait_InterruptedException() // LUCENENET: renamed from TestTimedAwait_InterruptedException (Wait() was Await())
        {
            CountdownLatch l = new CountdownLatch(1);
            ThreadJob t = new ThreadAnonymousClassForTestTimedWaitInterruptedException(this, l);
            t.Start();
            try
            {
                Thread.Sleep(SHORT_DELAY_MS);
                assertEquals(l.Count, 1);
                t.Interrupt();
                t.Join();
            }
            catch (Exception e) when (e.IsInterruptedException())
            {
                unexpectedException();
            }
        }

        private sealed class ThreadAnonymousClassForTestTimedWaitInterruptedException : ThreadJob // LUCENENET: renamed from ThreadAnonymousClassForTestTimedAwaitInterruptedException
        {
            private readonly CountdownLatchTest outerInstance;
            private readonly CountdownLatch l;

            public ThreadAnonymousClassForTestTimedWaitInterruptedException(CountdownLatchTest outerInstance, CountdownLatch l)
            {
                this.outerInstance = outerInstance;
                this.l = l;
            }

            public override void Run()
            {
                try
                {
                    outerInstance.threadAssertTrue(l.Count > 0);
                    l.Wait(TimeSpan.FromMilliseconds(MEDIUM_DELAY_MS));
                    outerInstance.threadShouldThrow();
                }
                catch (Exception e) when (e.IsInterruptedException())
                {
                    // success
                }
            }
        }

        /**
         * timed await times out if not counted down before timeout
         */
        [Test]
        public void TestWaitTimeout() // LUCENENET: renamed from TestAwaitTimeout (Wait() was Await())
        {
            CountdownLatch l = new CountdownLatch(1);
            ThreadJob t = new ThreadAnonymousClassForTestWaitTimeout(this, l);
            t.Start();
            try
            {
                assertEquals(l.Count, 1);
                t.Join();
            }
            catch (Exception e) when (e.IsInterruptedException())
            {
                unexpectedException();
            }
        }

        private sealed class ThreadAnonymousClassForTestWaitTimeout : ThreadJob // LUCENENET: renamed from ThreadAnonymousClassForTestAwaitTimeout
        {
            private readonly CountdownLatchTest outerInstance;
            private readonly CountdownLatch l;

            public ThreadAnonymousClassForTestWaitTimeout(CountdownLatchTest outerInstance, CountdownLatch l)
            {
                this.outerInstance = outerInstance;
                this.l = l;
            }

            public override void Run()
            {
                try
                {
                    outerInstance.threadAssertTrue(l.Count > 0);
                    outerInstance.threadAssertFalse(l.Wait(TimeSpan.FromMilliseconds(SHORT_DELAY_MS)));
                    outerInstance.threadAssertTrue(l.Count > 0);
                }
                catch (Exception ie) when (ie.IsInterruptedException())
                {
                    outerInstance.threadUnexpectedException();
                }
            }
        }

        /**
         * toString indicates current count
         */
        [Test]
        public void TestToString()
        {
            CountdownLatch s = new CountdownLatch(2);
            string us = s.ToString();
            assertTrue(us.IndexOf("Count = 2", StringComparison.Ordinal) >= 0);
            s.Signal();
            string s1 = s.ToString();
            assertTrue(s1.IndexOf("Count = 1", StringComparison.Ordinal) >= 0);
            s.Signal();
            string s2 = s.ToString();
            assertTrue(s2.IndexOf("Count = 0", StringComparison.Ordinal) >= 0);
        }
    }
}
