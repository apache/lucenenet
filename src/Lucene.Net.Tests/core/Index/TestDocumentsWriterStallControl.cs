using System;
using System.Collections.Generic;
using System.Threading;

namespace Lucene.Net.Index
{
    using Lucene.Net.Randomized.Generators;
    using Lucene.Net.Support;
    using NUnit.Framework;
    /*
             * Licensed to the Apache Software Foundation (ASF) under one or more
             * contributor license agreements. See the NOTICE file distributed with this
             * work for additional information regarding copyright ownership. The ASF
             * licenses this file to You under the Apache License, Version 2.0 (the
             * "License"); you may not use this file except in compliance with the License.
             * You may obtain a copy of the License at
             *
             * http://www.apache.org/licenses/LICENSE-2.0
             *
             * Unless required by applicable law or agreed to in writing, software
             * distributed under the License is distributed on an "AS IS" BASIS, WITHOUT
             * WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the
             * License for the specific language governing permissions and limitations under
             * the License.
             */

    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;

    /// <summary>
    /// Tests for <seealso cref="DocumentsWriterStallControl"/>
    /// </summary>
    [TestFixture]
    public class TestDocumentsWriterStallControl : LuceneTestCase
    {
        [Test]
        public virtual void TestSimpleStall()
        {
            DocumentsWriterStallControl ctrl = new DocumentsWriterStallControl();

            ctrl.UpdateStalled(false);
            ThreadClass[] waitThreads = WaitThreads(AtLeast(1), ctrl);
            Start(waitThreads);
            Assert.IsFalse(ctrl.HasBlocked());
            Assert.IsFalse(ctrl.AnyStalledThreads());
            Join(waitThreads);

            // now stall threads and wake them up again
            ctrl.UpdateStalled(true);
            waitThreads = WaitThreads(AtLeast(1), ctrl);
            Start(waitThreads);
            AwaitState(ThreadState.WaitSleepJoin, waitThreads);
            Assert.IsTrue(ctrl.HasBlocked());
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

            ThreadClass[] stallThreads = new ThreadClass[AtLeast(3)];
            for (int i = 0; i < stallThreads.Length; i++)
            {
                int stallProbability = 1 + Random().Next(10);
                stallThreads[i] = new ThreadAnonymousInnerClassHelper(ctrl, stallProbability);
            }
            Start(stallThreads);
            long time = Environment.TickCount;
            /*
             * use a 100 sec timeout to make sure we not hang forever. join will fail in
             * that case
             */
            while ((Environment.TickCount - time) < 100 * 1000 && !Terminated(stallThreads))
            {
                ctrl.UpdateStalled(false);
                if (Random().NextBoolean())
                {
                    Thread.Sleep(0);
                }
                else
                {
                    Thread.Sleep(1);
                }
            }
            Join(stallThreads);
        }

        private class ThreadAnonymousInnerClassHelper : ThreadClass
        {
            private DocumentsWriterStallControl Ctrl;
            private int StallProbability;

            public ThreadAnonymousInnerClassHelper(DocumentsWriterStallControl ctrl, int stallProbability)
            {
                this.Ctrl = ctrl;
                this.StallProbability = stallProbability;
            }

            public override void Run()
            {
                int iters = AtLeast(1000);
                for (int j = 0; j < iters; j++)
                {
                    Ctrl.UpdateStalled(Random().Next(StallProbability) == 0);
                    if (Random().Next(5) == 0) // thread 0 only updates
                    {
                        Ctrl.WaitIfStalled();
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
            var threads = new ThreadClass[numReleasers + numStallers + numWaiters];
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
            float checkPointProbability = TEST_NIGHTLY ? 0.5f : 0.1f;
            for (int i = 0; i < iters; i++)
            {
                if (checkPoint.Get())
                {
                    Assert.IsTrue(sync.UpdateJoin.Wait(new TimeSpan(0, 0, 0, 10)), "timed out waiting for update threads - deadlock?");
                    if (exceptions.Count > 0)
                    {
                        foreach (Exception throwable in exceptions)
                        {
                            Console.WriteLine(throwable.ToString());
                            Console.Write(throwable.StackTrace);
                        }
                        Assert.Fail("got exceptions in threads");
                    }

                    if (ctrl.HasBlocked() && ctrl.Healthy)
                    {
                        AssertState(numReleasers, numStallers, numWaiters, threads, ctrl);
                    }

                    checkPoint.Set(false);
                    sync.Waiter.Signal();
                    sync.LeftCheckpoint.Wait();
                }
                Assert.IsFalse(checkPoint.Get());
                Assert.AreEqual(0, sync.Waiter.CurrentCount);
                if (checkPointProbability >= (float)Random().NextDouble())
                {
                    sync.Reset(numStallers + numReleasers, numStallers + numReleasers + numWaiters);
                    checkPoint.Set(true);
                }
            }
            if (!checkPoint.Get())
            {
                sync.Reset(numStallers + numReleasers, numStallers + numReleasers + numWaiters);
                checkPoint.Set(true);
            }

            Assert.IsTrue(sync.UpdateJoin.Wait(new TimeSpan(0, 0, 0, 10)));
            AssertState(numReleasers, numStallers, numWaiters, threads, ctrl);
            checkPoint.Set(false);
            stop.Set(true);
            sync.Waiter.Signal();
            sync.LeftCheckpoint.Wait();

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

        private void AssertState(int numReleasers, int numStallers, int numWaiters, ThreadClass[] threads, DocumentsWriterStallControl ctrl)
        {
            int millisToSleep = 100;
            while (true)
            {
                if (ctrl.HasBlocked() && ctrl.Healthy)
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

        public class Waiter : ThreadClass
        {
            internal Synchronizer Sync;
            internal DocumentsWriterStallControl Ctrl;
            internal AtomicBoolean CheckPoint;
            internal AtomicBoolean Stop;
            internal IList<Exception> Exceptions;

            public Waiter(AtomicBoolean stop, AtomicBoolean checkPoint, DocumentsWriterStallControl ctrl, Synchronizer sync, IList<Exception> exceptions)
                : base("waiter")
            {
                this.Stop = stop;
                this.CheckPoint = checkPoint;
                this.Ctrl = ctrl;
                this.Sync = sync;
                this.Exceptions = exceptions;
            }

            public override void Run()
            {
                try
                {
                    while (!Stop.Get())
                    {
                        Ctrl.WaitIfStalled();
                        if (CheckPoint.Get())
                        {
#if !NETSTANDARD
                            try
                            {
#endif
                                Assert.IsTrue(Sync.await());
#if !NETSTANDARD
                            }
                            catch (ThreadInterruptedException e)
                            {
                                Console.WriteLine("[Waiter] got interrupted - wait count: " + Sync.Waiter.CurrentCount);
                                throw new ThreadInterruptedException("Thread Interrupted Exception", e);
                            }
#endif
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                    Console.Write(e.StackTrace);
                    Exceptions.Add(e);
                }
            }
        }

        public class Updater : ThreadClass
        {
            internal Synchronizer Sync;
            internal DocumentsWriterStallControl Ctrl;
            internal AtomicBoolean CheckPoint;
            internal AtomicBoolean Stop;
            internal bool Release;
            internal IList<Exception> Exceptions;

            public Updater(AtomicBoolean stop, AtomicBoolean checkPoint, DocumentsWriterStallControl ctrl, Synchronizer sync, bool release, IList<Exception> exceptions)
                : base("updater")
            {
                this.Stop = stop;
                this.CheckPoint = checkPoint;
                this.Ctrl = ctrl;
                this.Sync = sync;
                this.Release = release;
                this.Exceptions = exceptions;
            }

            public override void Run()
            {
                try
                {
                    while (!Stop.Get())
                    {
                        int internalIters = Release && Random().NextBoolean() ? AtLeast(5) : 1;
                        for (int i = 0; i < internalIters; i++)
                        {
                            Ctrl.UpdateStalled(Random().NextBoolean());
                        }
                        if (CheckPoint.Get())
                        {
                            Sync.UpdateJoin.Signal();
#if !NETSTANDARD
                            try
                            {
#endif
                                Assert.IsTrue(Sync.await());
#if !NETSTANDARD
                            }
                            catch (ThreadInterruptedException e)
                            {
                                Console.WriteLine("[Updater] got interrupted - wait count: " + Sync.Waiter.CurrentCount);
                                throw new ThreadInterruptedException("Thread Interrupted Exception", e);
                            }
#endif
                            Sync.LeftCheckpoint.Signal();
                        }
                        if (Random().NextBoolean())
                        {
                            Thread.Sleep(0);
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                    Console.Write(e.StackTrace);
                    Exceptions.Add(e);
                }
                Sync.UpdateJoin.Signal();
            }
        }

        public static bool Terminated(ThreadClass[] threads)
        {
            foreach (ThreadClass thread in threads)
            {
                if (ThreadState.Stopped != thread.State)
                {
                    return false;
                }
            }
            return true;
        }

        public static void Start(ThreadClass[] tostart)
        {
            foreach (ThreadClass thread in tostart)
            {
                thread.Start();
            }
            Thread.Sleep(1); // let them start
        }

        public static void Join(ThreadClass[] toJoin)
        {
            foreach (ThreadClass thread in toJoin)
            {
                thread.Join();
            }
        }

        public static ThreadClass[] WaitThreads(int num, DocumentsWriterStallControl ctrl)
        {
            ThreadClass[] array = new ThreadClass[num];
            for (int i = 0; i < array.Length; i++)
            {
                array[i] = new ThreadAnonymousInnerClassHelper2(ctrl);
            }
            return array;
        }

        private class ThreadAnonymousInnerClassHelper2 : ThreadClass
        {
            private DocumentsWriterStallControl Ctrl;

            public ThreadAnonymousInnerClassHelper2(DocumentsWriterStallControl ctrl)
            {
                this.Ctrl = ctrl;
            }

            public override void Run()
            {
                Ctrl.WaitIfStalled();
            }
        }

        /// <summary>
        /// Waits for all incoming threads to be in wait()
        ///  methods.
        /// </summary>
        public static void AwaitState(ThreadState state, params ThreadClass[] threads)
        {
            while (true)
            {
                bool done = true;
                foreach (ThreadClass thread in threads)
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
                if (Random().NextBoolean())
                {
                    Thread.Sleep(0);
                }
                else
                {
                    Thread.Sleep(1);
                }
            }
        }

        public sealed class Synchronizer
        {
            internal volatile CountdownEvent Waiter;
            internal volatile CountdownEvent UpdateJoin;
            internal volatile CountdownEvent LeftCheckpoint;

            public Synchronizer(int numUpdater, int numThreads)
            {
                Reset(numUpdater, numThreads);
            }

            public void Reset(int numUpdaters, int numThreads)
            {
                this.Waiter = new CountdownEvent(1);
                this.UpdateJoin = new CountdownEvent(numUpdaters);
                this.LeftCheckpoint = new CountdownEvent(numUpdaters);
            }

            public bool @await()
            {
                return Waiter.Wait(new TimeSpan(0, 0, 0, 10));
            }
        }
    }
}