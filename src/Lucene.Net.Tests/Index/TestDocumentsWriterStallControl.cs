using J2N.Threading;
using J2N.Threading.Atomic;
using Lucene.Net.Support.Threading;
using NUnit.Framework;
using RandomizedTesting.Generators;
using System;
using System.Collections.Generic;
using System.Threading;
using Assert = Lucene.Net.TestFramework.Assert;
using Console = Lucene.Net.Util.SystemConsole;

namespace Lucene.Net.Index
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

    /// <summary>
    /// Tests for <seealso cref="DocumentsWriterStallControl"/>
    /// </summary>
    [TestFixture]
    [Timeout(900_000)] // 15 minutes
    public class TestDocumentsWriterStallControl : LuceneTestCase
    {
        [Test]
        public virtual void TestSimpleStall()
        {
            DocumentsWriterStallControl ctrl = new DocumentsWriterStallControl();

            ctrl.UpdateStalled(false);
            ThreadJob[] waitThreads = WaitThreads(AtLeast(1), ctrl);
            Start(waitThreads);
            Assert.IsFalse(ctrl.HasBlocked);
            Assert.IsFalse(ctrl.AnyStalledThreads());
            Join(waitThreads);

            // now stall threads and wake them up again
            ctrl.UpdateStalled(true);
            waitThreads = WaitThreads(AtLeast(1), ctrl);
            Start(waitThreads);
            AwaitState(ThreadState.WaitSleepJoin, waitThreads);
            Assert.IsTrue(ctrl.HasBlocked);
            Assert.IsTrue(ctrl.AnyStalledThreads());
            ctrl.UpdateStalled(false);
            Assert.IsFalse(ctrl.AnyStalledThreads());
            Join(waitThreads);
        }

        [Test]
        public virtual void TestRandom()
        {
            DocumentsWriterStallControl ctrl = new DocumentsWriterStallControl();
            ctrl.UpdateStalled(false);

            ThreadJob[] stallThreads = new ThreadJob[AtLeast(3)];
            for (int i = 0; i < stallThreads.Length; i++)
            {
                int stallProbability = 1 + Random.Next(10);
                stallThreads[i] = new ThreadAnonymousClass(ctrl, stallProbability);
            }
            Start(stallThreads);
            long time = J2N.Time.NanoTime() / J2N.Time.MillisecondsPerNanosecond; // LUCENENET: Use NanoTime() rather than CurrentTimeMilliseconds() for more accurate/reliable results
            /*
             * use a 100 sec timeout to make sure we not hang forever. join will fail in
             * that case
             */
            while (((J2N.Time.NanoTime() / J2N.Time.MillisecondsPerNanosecond) - time) < 100 * 1000 && !Terminated(stallThreads)) // LUCENENET: Use NanoTime() rather than CurrentTimeMilliseconds() for more accurate/reliable results
            {
                ctrl.UpdateStalled(false);
                if (Random.NextBoolean())
                {
                    Thread.Yield();
                }
                else
                {
                    Thread.Sleep(1);
                }
            }
            Join(stallThreads);
        }

        private sealed class ThreadAnonymousClass : ThreadJob
        {
            private readonly DocumentsWriterStallControl ctrl;
            private readonly int stallProbability;

            public ThreadAnonymousClass(DocumentsWriterStallControl ctrl, int stallProbability)
            {
                this.ctrl = ctrl;
                this.stallProbability = stallProbability;
            }

            public override void Run()
            {
                int iters = AtLeast(1000);
                for (int j = 0; j < iters; j++)
                {
                    ctrl.UpdateStalled(Random.Next(stallProbability) == 0);
                    if (Random.Next(5) == 0) // thread 0 only updates
                    {
                        ctrl.WaitIfStalled();
                    }
                }
            }
        }

        [Test]
        public virtual void TestAccquireReleaseRace()
        {
            DocumentsWriterStallControl ctrl = new DocumentsWriterStallControl();
            ctrl.UpdateStalled(false);
            AtomicBoolean stop = new AtomicBoolean(false);
            AtomicBoolean checkPoint = new AtomicBoolean(true);

            int numStallers = AtLeast(1);
            int numReleasers = AtLeast(1);
            int numWaiters = AtLeast(1);
            var sync = new Synchronizer(numStallers + numReleasers, numStallers + numReleasers + numWaiters);
            var threads = new ThreadJob[numReleasers + numStallers + numWaiters];
            IList<Exception> exceptions = new SynchronizedList<Exception>();
            for (int i = 0; i < numReleasers; i++)
            {
                threads[i] = new Updater(stop, checkPoint, ctrl, sync, true, exceptions);
            }
            for (int i = numReleasers; i < numReleasers + numStallers; i++)
            {
                threads[i] = new Updater(stop, checkPoint, ctrl, sync, false, exceptions);
            }
            for (int i = numReleasers + numStallers; i < numReleasers + numStallers + numWaiters; i++)
            {
                threads[i] = new Waiter(stop, checkPoint, ctrl, sync, exceptions);
            }

            Start(threads);
            int iters = AtLeast(10000);
            //float checkPointProbability = TestNightly ? 0.5f : 0.1f;
            // LUCENENET specific - reduced probabliltiy on x86 to prevent it from timing out.
            float checkPointProbability = TestNightly ? (Lucene.Net.Util.Constants.RUNTIME_IS_64BIT ? 0.5f : 0.25f) : 0.1f;
            for (int i = 0; i < iters; i++)
            {
                if (checkPoint)
                {
                    Assert.IsTrue(sync.updateJoin.Wait(TimeSpan.FromSeconds(10)), "timed out waiting for update threads - deadlock?");
                    if (exceptions.Count > 0)
                    {
                        foreach (Exception throwable in exceptions)
                        {
                            Console.WriteLine(throwable.ToString());
                            Console.Write(throwable.StackTrace);
                        }
                        Assert.Fail("got exceptions in threads");
                    }

                    if (ctrl.HasBlocked && ctrl.IsHealthy)
                    {
                        AssertState(numReleasers, numStallers, numWaiters, threads, ctrl);
                    }

                    checkPoint.Value = false;
                    sync.waiter.Signal();
                    sync.leftCheckpoint.Wait();
                }
                Assert.IsFalse(checkPoint);
                Assert.AreEqual(0, sync.waiter.CurrentCount);
                if (checkPointProbability >= Random.NextSingle())
                {
                    sync.Reset(numStallers + numReleasers, numStallers + numReleasers + numWaiters);
                    checkPoint.Value = true;
                }
            }
            if (!checkPoint)
            {
                sync.Reset(numStallers + numReleasers, numStallers + numReleasers + numWaiters);
                checkPoint.Value = true;
            }

            Assert.IsTrue(sync.updateJoin.Wait(TimeSpan.FromSeconds(10)));
            AssertState(numReleasers, numStallers, numWaiters, threads, ctrl);
            checkPoint.Value = false;
            stop.Value = true;
            sync.waiter.Signal();
            sync.leftCheckpoint.Wait();

            for (int i = 0; i < threads.Length; i++)
            {
                ctrl.UpdateStalled(false);
                threads[i].Join(2000);
                if (threads[i].IsAlive && threads[i] is Waiter)
                {
                    if (threads[i].State == ThreadState.WaitSleepJoin)
                    {
                        Assert.Fail("waiter is not released - anyThreadsStalled: " + ctrl.AnyStalledThreads());
                    }
                }
            }
        }

        private void AssertState(int numReleasers, int numStallers, int numWaiters, ThreadJob[] threads, DocumentsWriterStallControl ctrl)
        {
            int millisToSleep = 100;
            while (true)
            {
                if (ctrl.HasBlocked && ctrl.IsHealthy)
                {
                    for (int n = numReleasers + numStallers; n < numReleasers + numStallers + numWaiters; n++)
                    {
                        if (ctrl.IsThreadQueued(threads[n]))
                        {
                            if (millisToSleep < 60000)
                            {
                                Thread.Sleep(millisToSleep);
                                millisToSleep *= 2;
                                break;
                            }
                            else
                            {
                                Assert.Fail("control claims no stalled threads but waiter seems to be blocked ");
                            }
                        }
                    }
                    break;
                }
                else
                {
                    break;
                }
            }
        }

        internal class Waiter : ThreadJob
        {
            internal Synchronizer sync;
            internal DocumentsWriterStallControl ctrl;
            internal AtomicBoolean checkPoint;
            internal AtomicBoolean stop;
            internal IList<Exception> exceptions;

            public Waiter(AtomicBoolean stop, AtomicBoolean checkPoint, DocumentsWriterStallControl ctrl, Synchronizer sync, IList<Exception> exceptions)
                : base("waiter")
            {
                this.stop = stop;
                this.checkPoint = checkPoint;
                this.ctrl = ctrl;
                this.sync = sync;
                this.exceptions = exceptions;
            }

            public override void Run()
            {
                try
                {
                    while (!stop)
                    {
                        ctrl.WaitIfStalled();
                        if (checkPoint)
                        {
                            try
                            {
                                Assert.IsTrue(sync.Await());
                            }
                            catch (Exception e) when (e.IsInterruptedException())
                            {
                                Console.WriteLine("[Waiter] got interrupted - wait count: " + sync.waiter.CurrentCount);
                                throw new Util.ThreadInterruptedException(e);
                            }
                        }
                    }
                }
                catch (Exception e) when (e.IsThrowable())
                {
                    Console.WriteLine(e.ToString());
                    Console.Write(e.StackTrace);
                    exceptions.Add(e);
                }
            }
        }

        internal class Updater : ThreadJob
        {
            internal Synchronizer sync;
            internal DocumentsWriterStallControl ctrl;
            internal AtomicBoolean checkPoint;
            internal AtomicBoolean stop;
            internal bool release;
            internal IList<Exception> exceptions;

            public Updater(AtomicBoolean stop, AtomicBoolean checkPoint, DocumentsWriterStallControl ctrl, Synchronizer sync, bool release, IList<Exception> exceptions)
                : base("updater")
            {
                this.stop = stop;
                this.checkPoint = checkPoint;
                this.ctrl = ctrl;
                this.sync = sync;
                this.release = release;
                this.exceptions = exceptions;
            }

            public override void Run()
            {
                try
                {
                    while (!stop)
                    {
                        int internalIters = release && Random.NextBoolean() ? AtLeast(5) : 1;
                        for (int i = 0; i < internalIters; i++)
                        {
                            ctrl.UpdateStalled(Random.NextBoolean());
                        }
                        if (checkPoint)
                        {
                            sync.updateJoin.Signal();
                            try
                            {
                                Assert.IsTrue(sync.Await());
                            }
                            catch (Exception e) when (e.IsInterruptedException())
                            {
                                Console.WriteLine("[Updater] got interrupted - wait count: " + sync.waiter.CurrentCount);
                                throw new Util.ThreadInterruptedException(e);
                            }
                            // LUCENENET: Not sure why this catch block was added, but I suspect it was for debugging purposes. Commented it rather than removing it because
                            // there may be some value to debugging this way.
                            //catch (Exception e)
                            //{
                            //    Console.Write("signal failed with : " + e);
                            //    throw; // LUCENENET: CA2200: Rethrow to preserve stack details (https://docs.microsoft.com/en-us/visualstudio/code-quality/ca2200-rethrow-to-preserve-stack-details)
                            //}

                            sync.leftCheckpoint.Signal();
                        }
                        if (Random.NextBoolean())
                        {
                            Thread.Yield();
                        }
                    }
                }
                catch (Exception e) when (e.IsThrowable())
                {
                    Console.WriteLine(e.ToString());
                    Console.Write(e.StackTrace);
                    exceptions.Add(e);
                }

                // LUCENENET specific - possible InvalidOperationException here if Signal() is called more than what is required to decrement to zero
                if (!sync.updateJoin.IsSet)
                {
                    sync.updateJoin.Signal();
                }
            }
        }

        public static bool Terminated(ThreadJob[] threads)
        {
            foreach (ThreadJob thread in threads)
            {
                if (ThreadState.Stopped != thread.State)
                {
                    return false;
                }
            }
            return true;
        }

        public static void Start(ThreadJob[] tostart)
        {
            foreach (ThreadJob thread in tostart)
            {
                thread.Start();
            }
        }

        public static void Join(ThreadJob[] toJoin)
        {
            foreach (ThreadJob thread in toJoin)
            {
                thread.Join();
            }
        }

        internal static ThreadJob[] WaitThreads(int num, DocumentsWriterStallControl ctrl)
        {
            ThreadJob[] array = new ThreadJob[num];
            for (int i = 0; i < array.Length; i++)
            {
                array[i] = new ThreadAnonymousClass2(ctrl);
            }
            return array;
        }

        private sealed class ThreadAnonymousClass2 : ThreadJob
        {
            private readonly DocumentsWriterStallControl ctrl;

            public ThreadAnonymousClass2(DocumentsWriterStallControl ctrl)
            {
                this.ctrl = ctrl;
            }

            public override void Run()
            {
                ctrl.WaitIfStalled();
            }
        }

        /// <summary>
        /// Waits for all incoming threads to be in wait()
        ///  methods.
        /// </summary>
        public static void AwaitState(ThreadState state, params ThreadJob[] threads)
        {
            while (true)
            {
                bool done = true;
                foreach (ThreadJob thread in threads)
                {
                    if (thread.State != state)
                    {
                        done = false;
                        break;
                    }
                }
                if (done)
                {
                    return;
                }
                if (Random.NextBoolean())
                {
                    Thread.Yield();
                }
                else
                {
                    Thread.Sleep(1);
                }
            }
        }

        public sealed class Synchronizer
        {
            internal volatile CountdownEvent waiter;
            internal volatile CountdownEvent updateJoin;
            internal volatile CountdownEvent leftCheckpoint;

            public Synchronizer(int numUpdater, int numThreads)
            {
                Reset(numUpdater, numThreads);
            }

            public void Reset(int numUpdaters, int numThreads)
            {
                this.waiter = new CountdownEvent(1);
                this.updateJoin = new CountdownEvent(numUpdaters);
                this.leftCheckpoint = new CountdownEvent(numUpdaters);
            }

            public bool Await()
            {
                return waiter.Wait(TimeSpan.FromSeconds(10));
            }
        }
    }
}