// Some code adapted from Apache Harmony: https://github.com/apache/harmony/blob/02970cb7227a335edd2c8457ebdde0195a735733/classlib/modules/concurrent/src/test/java/CountDownLatchTest.java

#region Copyright 2010 by Apache Harmony, Licensed under the Apache License, Version 2.0
/*  Licensed to the Apache Software Foundation (ASF) under one or more
 *  contributor license agreements.  See the NOTICE file distributed with
 *  this work for additional information regarding copyright ownership.
 *  The ASF licenses this file to You under the Apache License, Version 2.0
 *  (the "License"); you may not use this file except in compliance with
 *  the License.  You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 *  Unless required by applicable law or agreed to in writing, software
 *  distributed under the License is distributed on an "AS IS" BASIS,
 *  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 *  See the License for the specific language governing permissions and
 *  limitations under the License.
 */
#endregion

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
    public class CountDownLatchTest : JSR166TestCase
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
                _ = new CountDownLatch(-1);
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
            CountDownLatch l = new CountDownLatch(2);
            assertEquals(2, l.Count);
            l.CountDown();
            assertEquals(1, l.Count);
        }

        /**
         * countDown decrements count when positive and has no effect when zero
         */
        [Test]
        public void TestCountDown()
        {
            CountDownLatch l = new CountDownLatch(1);
            assertEquals(1, l.Count);
            l.CountDown();
            assertEquals(0, l.Count);
            l.CountDown();
            assertEquals(0, l.Count);
        }

        /**
         * await returns after countDown to zero, but not before
         */
        [Test]
        public void TestAwait()
        {
            CountDownLatch l = new CountDownLatch(2);

            ThreadJob t = new ThreadAnonymousClassForTestAwait(this, l);
            t.Start();
            try
            {
                assertEquals(l.Count, 2);
                Thread.Sleep(SHORT_DELAY_MS);
                l.CountDown();
                assertEquals(l.Count, 1);
                l.CountDown();
                assertEquals(l.Count, 0);
                t.Join();
            }
            catch (Exception e) when (e.IsInterruptedException())
            {
                unexpectedException();
            }
        }

        private sealed class ThreadAnonymousClassForTestAwait : ThreadJob
        {
            private readonly CountDownLatchTest outerInstance;
            private readonly CountDownLatch l;

            public ThreadAnonymousClassForTestAwait(CountDownLatchTest outerInstance, CountDownLatch l)
            {
                this.outerInstance = outerInstance;
                this.l = l;
            }

            public override void Run()
            {
                outerInstance.threadAssertTrue(l.Count > 0);
                l.Await();
                outerInstance.threadAssertTrue(l.Count == 0);
            }
        }

        /**
         * timed await returns after countDown to zero
         */
        [Test]
        public void TestTimedAwait()
        {
            CountDownLatch l = new CountDownLatch(2);

            ThreadJob t = new ThreadAnonymousClassForTestTimedAwait(this, l);
            t.Start();
            try
            {
                assertEquals(l.Count, 2);
                Thread.Sleep(SHORT_DELAY_MS);
                l.CountDown();
                assertEquals(l.Count, 1);
                l.CountDown();
                assertEquals(l.Count, 0);
                t.Join();
            }
            catch (Exception e) when (e.IsInterruptedException())
            {
                unexpectedException();
            }
        }

        private sealed class ThreadAnonymousClassForTestTimedAwait : ThreadJob
        {
            private readonly CountDownLatchTest outerInstance;
            private readonly CountDownLatch l;

            public ThreadAnonymousClassForTestTimedAwait(CountDownLatchTest outerInstance, CountDownLatch l)
            {
                this.outerInstance = outerInstance;
                this.l = l;
            }

            public override void Run()
            {
                outerInstance.threadAssertTrue(l.Count > 0);
                outerInstance.threadAssertTrue(l.Await(TimeSpan.FromMilliseconds(SMALL_DELAY_MS)));
            }
        }

        /**
         * await throws IE if interrupted before counted down
         */
        [Test]
        [Ignore("LUCENENET: CountDownLatch.Await() does not honor Thread.Interrupt() because Lucene.NET does not support Java-style thread interrupts. See the LUCENENET note on CountDownLatch.Await() for details.")]
        public void TestAwait_InterruptedException()
        {
            CountDownLatch l = new CountDownLatch(1);
            ThreadJob t = new ThreadAnonymousClassForTestAwaitInterruptedException(this, l);
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

        private sealed class ThreadAnonymousClassForTestAwaitInterruptedException : ThreadJob
        {
            private readonly CountDownLatchTest outerInstance;
            private readonly CountDownLatch l;

            public ThreadAnonymousClassForTestAwaitInterruptedException(CountDownLatchTest outerInstance, CountDownLatch l)
            {
                this.outerInstance = outerInstance;
                this.l = l;
            }

            public override void Run()
            {
                try
                {
                    outerInstance.threadAssertTrue(l.Count > 0);
                    l.Await();
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
        [Ignore("LUCENENET: CountDownLatch.Await(TimeSpan) does not honor Thread.Interrupt() because Lucene.NET does not support Java-style thread interrupts. See the LUCENENET note on CountDownLatch.Await() for details.")]
        public void TestTimedAwait_InterruptedException()
        {
            CountDownLatch l = new CountDownLatch(1);
            ThreadJob t = new ThreadAnonymousClassForTestTimedAwaitInterruptedException(this, l);
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

        private sealed class ThreadAnonymousClassForTestTimedAwaitInterruptedException : ThreadJob
        {
            private readonly CountDownLatchTest outerInstance;
            private readonly CountDownLatch l;

            public ThreadAnonymousClassForTestTimedAwaitInterruptedException(CountDownLatchTest outerInstance, CountDownLatch l)
            {
                this.outerInstance = outerInstance;
                this.l = l;
            }

            public override void Run()
            {
                try
                {
                    outerInstance.threadAssertTrue(l.Count > 0);
                    l.Await(TimeSpan.FromMilliseconds(MEDIUM_DELAY_MS));
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
        public void TestAwaitTimeout()
        {
            CountDownLatch l = new CountDownLatch(1);
            ThreadJob t = new ThreadAnonymousClassForTestAwaitTimeout(this, l);
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

        private sealed class ThreadAnonymousClassForTestAwaitTimeout : ThreadJob
        {
            private readonly CountDownLatchTest outerInstance;
            private readonly CountDownLatch l;

            public ThreadAnonymousClassForTestAwaitTimeout(CountDownLatchTest outerInstance, CountDownLatch l)
            {
                this.outerInstance = outerInstance;
                this.l = l;
            }

            public override void Run()
            {
                outerInstance.threadAssertTrue(l.Count > 0);
                outerInstance.threadAssertFalse(l.Await(TimeSpan.FromMilliseconds(SHORT_DELAY_MS)));
                outerInstance.threadAssertTrue(l.Count > 0);
            }
        }

        /**
         * toString indicates current count
         */
        [Test]
        public void TestToString()
        {
            CountDownLatch s = new CountDownLatch(2);
            string us = s.ToString();
            assertTrue(us.IndexOf("Count = 2", StringComparison.Ordinal) >= 0);
            s.CountDown();
            string s1 = s.ToString();
            assertTrue(s1.IndexOf("Count = 1", StringComparison.Ordinal) >= 0);
            s.CountDown();
            string s2 = s.ToString();
            assertTrue(s2.IndexOf("Count = 0", StringComparison.Ordinal) >= 0);
        }
    }
}
