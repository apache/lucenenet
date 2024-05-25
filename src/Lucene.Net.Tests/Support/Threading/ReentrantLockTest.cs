using J2N.Threading;
using J2N.Threading.Atomic;
using Lucene.Net.Attributes;
using Lucene.Net.Support.Threading;
using Lucene.Net.Util;
using NUnit.Framework;
using RandomizedTesting.Generators;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using Assert = Lucene.Net.TestFramework.Assert;
using Console = Lucene.Net.Util.SystemConsole;

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
    public class ReentrantLockTest : JSR166TestCase
    {
        //public static void main(String[] args)
        //{
        //    junit.textui.TestRunner.run(suite());
        //}
        //public static Test suite()
        //{
        //    return new TestSuite(ReentrantLockTest.class);
        //}

        /**
        * A runnable calling lockInterruptibly
        */
        private class InterruptibleLockRunnable : ThreadJob
        {
            private readonly ReentrantLock @lock;
            public InterruptibleLockRunnable(ReentrantLock l)
            {
                @lock = l;
            }

            public override void Run()
            {
                try
                {
#pragma warning disable 612, 618
                    @lock.LockInterruptibly();
#pragma warning restore 612, 618
                }
                catch (System.Threading.ThreadInterruptedException)
                {
                    // success
                }
            }
        }

        /**
         * A runnable calling lockInterruptibly that expects to be
         * interrupted
         */
        private class InterruptedLockRunnable : ThreadJob
        {
            private readonly ReentrantLockTest outerInstance;
            private readonly ReentrantLock @lock;
            public InterruptedLockRunnable(ReentrantLockTest outerInstance, ReentrantLock l)
            {
                this.outerInstance = outerInstance;
                @lock = l;
            }

            public override void Run()
            {
                try
                {
#pragma warning disable 612, 618
                    @lock.LockInterruptibly();
#pragma warning restore 612, 618
                    outerInstance.threadShouldThrow();
                }
                catch (System.Threading.ThreadInterruptedException)
                {
                    // success
                }
            }
        }

        ///**
        // * Subclass to expose protected methods
        // */
        //static class PublicReentrantLock extends ReentrantLock
        //{
        //    PublicReentrantLock() { super(); }
        //    public Collection<Thread> getQueuedThreads()
        //    {
        //        return super.getQueuedThreads();
        //    }
        //    public Collection<Thread> getWaitingThreads(Condition c)
        //    {
        //        return super.getWaitingThreads(c);
        //    }


        //}

        ///**
        // * Constructor sets given fairness
        // */
        //public void testConstructor()
        //{
        //    ReentrantLock rl = new ReentrantLock();
        //    assertFalse(rl.isFair());
        //    ReentrantLock r2 = new ReentrantLock(true);
        //    assertTrue(r2.isFair());
        //}

        ///**
        // * locking an unlocked lock succeeds
        // */
        //[Test]
        //public void TestLock()
        //{
        //    ReentrantLock rl = new ReentrantLock();
        //    rl.Lock();
        //    assertTrue(rl.IsLocked);
        //    rl.Unlock();
        //}

        ///**
        // * locking an unlocked fair lock succeeds
        // */
        //public void testFairLock()
        //{
        //    ReentrantLock rl = new ReentrantLock(true);
        //    rl.lock () ;
        //    assertTrue(rl.isLocked());
        //    rl.unlock();
        //}


        /**
         * Unlocking an unlocked lock throws IllegalMonitorStateException
         */
        [Test]
        public void TestUnlock_IllegalMonitorStateException()
        {
            ReentrantLock rl = new ReentrantLock();

            try
            {
                rl.Unlock();
                shouldThrow();
            }
            catch (SynchronizationLockException)
            {
                // success
            }
        }

        ///**
        // * tryLock on an unlocked lock succeeds
        // */
        //[Test]
        //public void TestTryLock()
        //{
        //    ReentrantLock rl = new ReentrantLock();
        //    assertTrue(rl.TryLock());
        //    assertTrue(rl.IsLocked);
        //    rl.Unlock();
        //}


        ///**
        // * hasQueuedThreads reports whether there are waiting threads
        // */
        //[Test]
        //[Ignore("Behavior differs from Java around interrupts, but Lucene.NET doesn't support interrupts or use this property.")]
        //public void TesthasQueuedThreads()
        //{
        //    ReentrantLock @lock = new ReentrantLock();
        //    ThreadJob t1 = new InterruptedLockRunnable(this, @lock);
        //    ThreadJob t2 = new InterruptibleLockRunnable(@lock);
        //    try
        //    {
        //        assertFalse(@lock.HasQueuedThreads);
        //        @lock.Lock();
        //        t1.Start();
        //        Thread.Sleep(SHORT_DELAY_MS);
        //        assertTrue(@lock.HasQueuedThreads);
        //        t2.Start();
        //        Thread.Sleep(SHORT_DELAY_MS);
        //        assertTrue(@lock.HasQueuedThreads);
        //        t1.Interrupt();
        //        Thread.Sleep(SHORT_DELAY_MS);
        //        assertTrue(@lock.HasQueuedThreads);
        //        @lock.Unlock();
        //        Thread.Sleep(SHORT_DELAY_MS);
        //        //assertFalse(@lock.HasQueuedThreads); // LUCENENET: Behavior differs from Java around interrupts, but Lucene.NET doesn't support interrupts or use this property.
        //        t1.Join();
        //        t2.Join();

        //        assertFalse(@lock.HasQueuedThreads); // LUCENENET: Added assert
        //    }
        //    catch (Exception e) when (e.IsException())
        //    {
        //        unexpectedException();
        //    }
        //}

        ///**
        // * getQueueLength reports number of waiting threads
        // */
        //[Test]
        //[Ignore("Behavior differs from Java around interrupts, but Lucene.NET doesn't support interrupts or use this property.")]
        //public void TestGetQueueLength()
        //{
        //    ReentrantLock @lock = new ReentrantLock();
        //    ThreadJob t1 = new InterruptedLockRunnable(this, @lock);
        //    ThreadJob t2 = new InterruptibleLockRunnable(@lock);
        //    try
        //    {
        //        assertEquals(0, @lock.QueueLength);
        //        @lock.Lock();
        //        t1.Start();
        //        Thread.Sleep(SHORT_DELAY_MS);
        //        assertEquals(1, @lock.QueueLength);
        //        t2.Start();
        //        Thread.Sleep(SHORT_DELAY_MS);
        //        assertEquals(2, @lock.QueueLength);
        //        t1.Interrupt();
        //        Thread.Sleep(SHORT_DELAY_MS);
        //        assertEquals(1, @lock.QueueLength);
        //        @lock.Unlock();
        //        Thread.Sleep(SHORT_DELAY_MS);
        //        //assertEquals(0, @lock.QueueLength); // LUCENENET: Behavior differs from Java around interrupts, but Lucene.NET doesn't support interrupts or use this property.
        //        t1.Join();
        //        t2.Join();

        //        assertEquals(0, @lock.QueueLength); // LUCENENET: Added assert
        //    }
        //    catch (Exception e) when (e.IsException())
        //    {
        //        unexpectedException();
        //    }
        //}

        ///**
        // * getQueueLength reports number of waiting threads
        // */
        //public void testGetQueueLength_fair()
        //{
        //    final ReentrantLock lock = new ReentrantLock(true);
        //    Thread t1 = new Thread(new InterruptedLockRunnable(lock));
        //    Thread t2 = new Thread(new InterruptibleLockRunnable(lock));
        //    try
        //    {
        //        assertEquals(0, lock.getQueueLength()) ;
        //        lock.lock () ;
        //        t1.start();
        //        Thread.sleep(SHORT_DELAY_MS);
        //        assertEquals(1, lock.getQueueLength()) ;
        //        t2.start();
        //        Thread.sleep(SHORT_DELAY_MS);
        //        assertEquals(2, lock.getQueueLength()) ;
        //        t1.interrupt();
        //        Thread.sleep(SHORT_DELAY_MS);
        //        assertEquals(1, lock.getQueueLength()) ;
        //        lock.unlock();
        //        Thread.sleep(SHORT_DELAY_MS);
        //        assertEquals(0, lock.getQueueLength()) ;
        //        t1.join();
        //        t2.join();
        //    }
        //    catch (Exception e)
        //    {
        //        unexpectedException();
        //    }
        //}

        ///**
        // * hasQueuedThread(null) throws NPE
        // */
        //public void testHasQueuedThreadNPE()
        //{
        //    final ReentrantLock sync = new ReentrantLock();
        //    try
        //    {
        //        sync.hasQueuedThread(null);
        //        shouldThrow();
        //    }
        //    catch (NullPointerException success)
        //    {
        //    }
        //}

        ///**
        // * hasQueuedThread reports whether a thread is queued.
        // */
        //public void testHasQueuedThread()
        //{
        //    final ReentrantLock sync = new ReentrantLock();
        //    Thread t1 = new Thread(new InterruptedLockRunnable(sync));
        //    Thread t2 = new Thread(new InterruptibleLockRunnable(sync));
        //    try
        //    {
        //        assertFalse(sync.hasQueuedThread(t1));
        //        assertFalse(sync.hasQueuedThread(t2));
        //        sync.lock () ;
        //        t1.start();
        //        Thread.sleep(SHORT_DELAY_MS);
        //        assertTrue(sync.hasQueuedThread(t1));
        //        t2.start();
        //        Thread.sleep(SHORT_DELAY_MS);
        //        assertTrue(sync.hasQueuedThread(t1));
        //        assertTrue(sync.hasQueuedThread(t2));
        //        t1.interrupt();
        //        Thread.sleep(SHORT_DELAY_MS);
        //        assertFalse(sync.hasQueuedThread(t1));
        //        assertTrue(sync.hasQueuedThread(t2));
        //        sync.unlock();
        //        Thread.sleep(SHORT_DELAY_MS);
        //        assertFalse(sync.hasQueuedThread(t1));
        //        Thread.sleep(SHORT_DELAY_MS);
        //        assertFalse(sync.hasQueuedThread(t2));
        //        t1.join();
        //        t2.join();
        //    }
        //    catch (Exception e)
        //    {
        //        unexpectedException();
        //    }
        //}


        ///**
        // * getQueuedThreads includes waiting threads
        // */
        //public void testGetQueuedThreads()
        //{
        //    final PublicReentrantLock lock = new PublicReentrantLock();
        //    Thread t1 = new Thread(new InterruptedLockRunnable(lock));
        //    Thread t2 = new Thread(new InterruptibleLockRunnable(lock));
        //    try
        //    {
        //        assertTrue(lock.getQueuedThreads().isEmpty()) ;
        //        lock.lock () ;
        //        assertTrue(lock.getQueuedThreads().isEmpty()) ;
        //        t1.start();
        //        Thread.sleep(SHORT_DELAY_MS);
        //        assertTrue(lock.getQueuedThreads().contains(t1)) ;
        //        t2.start();
        //        Thread.sleep(SHORT_DELAY_MS);
        //        assertTrue(lock.getQueuedThreads().contains(t1)) ;
        //        assertTrue(lock.getQueuedThreads().contains(t2)) ;
        //        t1.interrupt();
        //        Thread.sleep(SHORT_DELAY_MS);
        //        assertFalse(lock.getQueuedThreads().contains(t1)) ;
        //        assertTrue(lock.getQueuedThreads().contains(t2)) ;
        //        lock.unlock();
        //        Thread.sleep(SHORT_DELAY_MS);
        //        assertTrue(lock.getQueuedThreads().isEmpty()) ;
        //        t1.join();
        //        t2.join();
        //    }
        //    catch (Exception e)
        //    {
        //        unexpectedException();
        //    }
        //}


        ///**
        // * timed tryLock is interruptible.
        // */
        //public void testInterruptedException2()
        //{
        //    final ReentrantLock lock = new ReentrantLock();
        //    lock.lock () ;
        //    Thread t = new Thread(new Runnable()
        //        {
        //            public void run()
        //        {
        //            try
        //            {
        //                lock.tryLock(MEDIUM_DELAY_MS, TimeUnit.MILLISECONDS);
        //                threadShouldThrow();
        //            }
        //            catch (InterruptedException success) { }
        //        }
        //    });
        //    try {
        //        t.start();
        //        t.interrupt();
        //    } catch(Exception e){
        //        unexpectedException();
        //    }
        //}

        /**
         * TryLock on a locked lock fails
         */
        [Test]
        public void TestTryLockWhenLocked()
        {
            ReentrantLock @lock = new ReentrantLock();
            @lock.Lock();
            Thread t = new Thread(() =>
            {
                threadAssertFalse(@lock.TryLock());
            });
            try
            {
                t.Start();
                t.Join();
                @lock.Unlock();
            }
            catch (Exception e) when (e.IsException())
            {
                unexpectedException();
            }
        }

        ///**
        // * Timed tryLock on a locked lock times out
        // */
        //public void testTryLock_Timeout()
        //{
        //    final ReentrantLock lock = new ReentrantLock();
        //    lock.lock () ;
        //    Thread t = new Thread(new Runnable()
        //        {
        //            public void run()
        //        {
        //            try
        //            {
        //                threadAssertFalse(lock.tryLock(1, TimeUnit.MILLISECONDS)) ;
        //            }
        //            catch (Exception ex)
        //            {
        //                threadUnexpectedException();
        //            }
        //        }
        //    });
        //    try {
        //        t.start();
        //        t.join();
        //        lock.unlock();
        //    } catch(Exception e){
        //        unexpectedException();
        //    }
        //}

        ///**
        // * getHoldCount returns number of recursive holds
        // */
        //[Test]
        //public void testGetHoldCount()
        //{
        //    ReentrantLock @lock = new ReentrantLock();
        //    for (int i = 1; i <= SIZE; i++)
        //    {
        //                @lock.lock () ;
        //        assertEquals(i, @lock.getHoldCount()) ;
        //    }
        //    for (int i = SIZE; i > 0; i--)
        //    {
        //                @lock.Unlock();
        //        assertEquals(i - 1, @lock.getHoldCount()) ;
        //    }
        //}


        ///**
        // * isLocked is true when locked and false when not
        // */
        //[Test]
        //public void TestIsLocked()
        //{
        //    ReentrantLock @lock = new ReentrantLock();
        //    @lock.Lock();
        //    assertTrue(@lock.IsLocked);
        //    @lock.Unlock();
        //    assertFalse(@lock.IsLocked);
        //    Thread t = new Thread(() =>
        //    {
        //        @lock.Lock();
        //        try
        //        {
        //            Thread.Sleep(SMALL_DELAY_MS);
        //        }
        //        catch (Exception e)
        //        {
        //            threadUnexpectedException();
        //        }
        //        @lock.Unlock();
        //    });

        //    try
        //    {
        //        t.Start();
        //        Thread.Sleep(SHORT_DELAY_MS);
        //        assertTrue(@lock.IsLocked);
        //        t.Join();
        //        assertFalse(@lock.IsLocked);
        //    }
        //    catch (Exception e) when (e.IsException())
        //    {
        //        unexpectedException();
        //    }
        //}

        /**
         * lockInterruptibly is interruptible.
         */
        [Test]
        [Ignore("LUCENENET: LockInterruptibly() is broken, but it is not in use anywhere but in the tests. Technically, Lucene.NET does not support Thread.Interrupt().")]
        public void TestLockInterruptibly1()
        {
            ReentrantLock @lock = new ReentrantLock();
            @lock.Lock();
            ThreadJob t = new InterruptedLockRunnable(this, @lock);
            try
            {
                t.Start();
                Thread.Sleep(SHORT_DELAY_MS);
                t.Interrupt();
                Thread.Sleep(SHORT_DELAY_MS);
                @lock.Unlock();
                t.Join();
            }
            catch (Exception e) when (e.IsException())
            {
                unexpectedException();
            }
        }

        ///**
        // * lockInterruptibly succeeds when unlocked, else is interruptible
        // */
        //[Test]
        //[Ignore("LUCENENET: LockInterruptibly() is broken, but it is not in use anywhere but in the tests. Technically, Lucene.NET does not support Thread.Interrupt().")]
        //public void TestLockInterruptibly2()
        //{
        //    ReentrantLock @lock = new ReentrantLock();
        //    try
        //    {
        //        @lock.LockInterruptibly();
        //    }
        //    catch (Exception e) when (e.IsException())
        //    {
        //        unexpectedException();
        //    }
        //    ThreadJob t = new InterruptedLockRunnable(this, @lock);
        //    try
        //    {
        //        t.Start();
        //        t.Interrupt();
        //        assertTrue(@lock.IsLocked);
        //        assertTrue(@lock.IsHeldByCurrentThread);
        //        t.Join();
        //    }
        //    catch (Exception e) when (e.IsException())
        //    {
        //        unexpectedException();
        //    }
        //}






        ///**
        // * Calling await without holding lock throws IllegalMonitorStateException
        // */
        //public void testAwait_IllegalMonitor()
        //{
        //    final ReentrantLock lock = new ReentrantLock();
        //    final Condition c = lock.newCondition();
        //    try
        //    {
        //        c.await();
        //        shouldThrow();
        //    }
        //    catch (IllegalMonitorStateException success)
        //    {
        //    }
        //    catch (Exception ex)
        //    {
        //        unexpectedException();
        //    }
        //}

        ///**
        // * Calling signal without holding lock throws IllegalMonitorStateException
        // */
        //public void testSignal_IllegalMonitor()
        //{
        //    final ReentrantLock lock = new ReentrantLock();
        //    final Condition c = lock.newCondition();
        //    try
        //    {
        //        c.signal();
        //        shouldThrow();
        //    }
        //    catch (IllegalMonitorStateException success)
        //    {
        //    }
        //    catch (Exception ex)
        //    {
        //        unexpectedException();
        //    }
        //}

        ///**
        // * awaitNanos without a signal times out
        // */
        //public void testAwaitNanos_Timeout()
        //{
        //    final ReentrantLock lock = new ReentrantLock();
        //    final Condition c = lock.newCondition();
        //    try
        //    {
        //        lock.lock () ;
        //        long t = c.awaitNanos(100);
        //        assertTrue(t <= 0);
        //        lock.unlock();
        //    }
        //    catch (Exception ex)
        //    {
        //        unexpectedException();
        //    }
        //}

        ///**
        // *  timed await without a signal times out
        // */
        //public void testAwait_Timeout()
        //{
        //    final ReentrantLock lock = new ReentrantLock();
        //    final Condition c = lock.newCondition();
        //    try
        //    {
        //        lock.lock () ;
        //        c.await(SHORT_DELAY_MS, TimeUnit.MILLISECONDS);
        //        lock.unlock();
        //    }
        //    catch (Exception ex)
        //    {
        //        unexpectedException();
        //    }
        //}

        ///**
        // * awaitUntil without a signal times out
        // */
        //public void testAwaitUntil_Timeout()
        //{
        //    final ReentrantLock lock = new ReentrantLock();
        //    final Condition c = lock.newCondition();
        //    try
        //    {
        //        lock.lock () ;
        //        java.util.Date d = new java.util.Date();
        //        c.awaitUntil(new java.util.Date(d.getTime() + 10));
        //        lock.unlock();
        //    }
        //    catch (Exception ex)
        //    {
        //        unexpectedException();
        //    }
        //}

        ///**
        // * await returns when signalled
        // */
        //public void testAwait()
        //{
        //    final ReentrantLock lock = new ReentrantLock();
        //    final Condition c = lock.newCondition();
        //    Thread t = new Thread(new Runnable()
        //    {

        //        public void run()
        //        {
        //            try
        //            {
        //                lock.lock () ;
        //                c.await();
        //                lock.unlock();
        //            }
        //            catch (InterruptedException e)
        //            {
        //                threadUnexpectedException();
        //            }
        //        }
        //    });

        //    try {
        //        t.start();
        //        Thread.sleep(SHORT_DELAY_MS);
        //        lock.lock();
        //        c.signal();
        //        lock.unlock();
        //        t.join(SHORT_DELAY_MS);
        //        assertFalse(t.isAlive());
        //    }
        //    catch (Exception ex) {
        //        unexpectedException();
        //    }
        //}

        ///**
        // * hasWaiters throws NPE if null
        // */
        //public void testHasWaitersNPE()
        //{
        //    final ReentrantLock lock = new ReentrantLock();
        //    try
        //    {
        //        lock.hasWaiters(null);
        //        shouldThrow();
        //    }
        //    catch (NullPointerException success)
        //    {
        //    }
        //    catch (Exception ex)
        //    {
        //        unexpectedException();
        //    }
        //}

        ///**
        // * getWaitQueueLength throws NPE if null
        // */
        //public void testGetWaitQueueLengthNPE()
        //{
        //    final ReentrantLock lock = new ReentrantLock();
        //    try
        //    {
        //        lock.getWaitQueueLength(null);
        //        shouldThrow();
        //    }
        //    catch (NullPointerException success)
        //    {
        //    }
        //    catch (Exception ex)
        //    {
        //        unexpectedException();
        //    }
        //}


        ///**
        // * getWaitingThreads throws NPE if null
        // */
        //public void testGetWaitingThreadsNPE()
        //{
        //    final PublicReentrantLock lock = new PublicReentrantLock();
        //    try
        //    {
        //        lock.getWaitingThreads(null);
        //        shouldThrow();
        //    }
        //    catch (NullPointerException success)
        //    {
        //    }
        //    catch (Exception ex)
        //    {
        //        unexpectedException();
        //    }
        //}


        ///**
        // * hasWaiters throws IAE if not owned
        // */
        //public void testHasWaitersIAE()
        //{
        //    final ReentrantLock lock = new ReentrantLock();
        //    final Condition c = (lock.newCondition()) ;
        //    final ReentrantLock lock2 = new ReentrantLock();
        //    try
        //    {
        //        lock2.hasWaiters(c);
        //        shouldThrow();
        //    }
        //    catch (IllegalArgumentException success)
        //    {
        //    }
        //    catch (Exception ex)
        //    {
        //        unexpectedException();
        //    }
        //}

        ///**
        // * hasWaiters throws IMSE if not locked
        // */
        //public void testHasWaitersIMSE()
        //{
        //    final ReentrantLock lock = new ReentrantLock();
        //    final Condition c = (lock.newCondition()) ;
        //    try
        //    {
        //        lock.hasWaiters(c);
        //        shouldThrow();
        //    }
        //    catch (IllegalMonitorStateException success)
        //    {
        //    }
        //    catch (Exception ex)
        //    {
        //        unexpectedException();
        //    }
        //}


        ///**
        // * getWaitQueueLength throws IAE if not owned
        // */
        //public void testGetWaitQueueLengthIAE()
        //{
        //    final ReentrantLock lock = new ReentrantLock();
        //    final Condition c = (lock.newCondition()) ;
        //    final ReentrantLock lock2 = new ReentrantLock();
        //    try
        //    {
        //        lock2.getWaitQueueLength(c);
        //        shouldThrow();
        //    }
        //    catch (IllegalArgumentException success)
        //    {
        //    }
        //    catch (Exception ex)
        //    {
        //        unexpectedException();
        //    }
        //}

        ///**
        // * getWaitQueueLength throws IMSE if not locked
        // */
        //public void testGetWaitQueueLengthIMSE()
        //{
        //    final ReentrantLock lock = new ReentrantLock();
        //    final Condition c = (lock.newCondition()) ;
        //    try
        //    {
        //        lock.getWaitQueueLength(c);
        //        shouldThrow();
        //    }
        //    catch (IllegalMonitorStateException success)
        //    {
        //    }
        //    catch (Exception ex)
        //    {
        //        unexpectedException();
        //    }
        //}


        ///**
        // * getWaitingThreads throws IAE if not owned
        // */
        //public void testGetWaitingThreadsIAE()
        //{
        //    final PublicReentrantLock lock = new PublicReentrantLock();
        //    final Condition c = (lock.newCondition()) ;
        //    final PublicReentrantLock lock2 = new PublicReentrantLock();
        //    try
        //    {
        //        lock2.getWaitingThreads(c);
        //        shouldThrow();
        //    }
        //    catch (IllegalArgumentException success)
        //    {
        //    }
        //    catch (Exception ex)
        //    {
        //        unexpectedException();
        //    }
        //}

        ///**
        // * getWaitingThreads throws IMSE if not locked
        // */
        //public void testGetWaitingThreadsIMSE()
        //{
        //    final PublicReentrantLock lock = new PublicReentrantLock();
        //    final Condition c = (lock.newCondition()) ;
        //    try
        //    {
        //        lock.getWaitingThreads(c);
        //        shouldThrow();
        //    }
        //    catch (IllegalMonitorStateException success)
        //    {
        //    }
        //    catch (Exception ex)
        //    {
        //        unexpectedException();
        //    }
        //}



        ///**
        // * hasWaiters returns true when a thread is waiting, else false
        // */
        //public void testHasWaiters()
        //{
        //    final ReentrantLock lock = new ReentrantLock();
        //    final Condition c = lock.newCondition();
        //    Thread t = new Thread(new Runnable()
        //    {

        //        public void run()
        //        {
        //            try
        //            {
        //                lock.lock () ;
        //                threadAssertFalse(lock.hasWaiters(c)) ;
        //                threadAssertEquals(0, lock.getWaitQueueLength(c)) ;
        //                c.await();
        //                lock.unlock();
        //            }
        //            catch (InterruptedException e)
        //            {
        //                threadUnexpectedException();
        //            }
        //        }
        //    });

        //    try
        //    {
        //        t.start();
        //        Thread.sleep(SHORT_DELAY_MS);
        //        lock.lock () ;
        //        assertTrue(lock.hasWaiters(c)) ;
        //        assertEquals(1, lock.getWaitQueueLength(c)) ;
        //        c.signal();
        //        lock.unlock();
        //        Thread.sleep(SHORT_DELAY_MS);
        //        lock.lock () ;
        //        assertFalse(lock.hasWaiters(c)) ;
        //        assertEquals(0, lock.getWaitQueueLength(c)) ;
        //        lock.unlock();
        //        t.join(SHORT_DELAY_MS);
        //        assertFalse(t.isAlive());
        //    }
        //    catch (Exception ex)
        //    {
        //        unexpectedException();
        //    }
        //}

        ///**
        // * getWaitQueueLength returns number of waiting threads
        // */
        //public void testGetWaitQueueLength()
        //{
        //    final ReentrantLock lock = new ReentrantLock();
        //    final Condition c = lock.newCondition();
        //    Thread t1 = new Thread(new Runnable()
        //    {

        //        public void run()
        //        {
        //            try
        //            {
        //                lock.lock () ;
        //                threadAssertFalse(lock.hasWaiters(c)) ;
        //                threadAssertEquals(0, lock.getWaitQueueLength(c)) ;
        //                c.await();
        //                lock.unlock();
        //            }
        //            catch (InterruptedException e)
        //            {
        //                threadUnexpectedException();
        //            }
        //        }
        //    });

        //    Thread t2 = new Thread(new Runnable()
        //    {

        //        public void run()
        //        {
        //            try
        //            {
        //                lock.lock () ;
        //                threadAssertTrue(lock.hasWaiters(c)) ;
        //                threadAssertEquals(1, lock.getWaitQueueLength(c)) ;
        //                c.await();
        //                lock.unlock();
        //            }
        //            catch (InterruptedException e)
        //            {
        //                threadUnexpectedException();
        //            }
        //        }
        //    });

        //    try
        //    {
        //        t1.start();
        //        Thread.sleep(SHORT_DELAY_MS);
        //        t2.start();
        //        Thread.sleep(SHORT_DELAY_MS);
        //        lock.lock () ;
        //        assertTrue(lock.hasWaiters(c)) ;
        //        assertEquals(2, lock.getWaitQueueLength(c)) ;
        //        c.signalAll();
        //        lock.unlock();
        //        Thread.sleep(SHORT_DELAY_MS);
        //        lock.lock () ;
        //        assertFalse(lock.hasWaiters(c)) ;
        //        assertEquals(0, lock.getWaitQueueLength(c)) ;
        //        lock.unlock();
        //        t1.join(SHORT_DELAY_MS);
        //        t2.join(SHORT_DELAY_MS);
        //        assertFalse(t1.isAlive());
        //        assertFalse(t2.isAlive());
        //    }
        //    catch (Exception ex)
        //    {
        //        unexpectedException();
        //    }
        //}

        ///**
        // * getWaitingThreads returns only and all waiting threads
        // */
        //public void testGetWaitingThreads()
        //{
        //    final PublicReentrantLock lock = new PublicReentrantLock();
        //    final Condition c = lock.newCondition();
        //    Thread t1 = new Thread(new Runnable()
        //    {

        //        public void run()
        //        {
        //            try
        //            {
        //                lock.lock () ;
        //                threadAssertTrue(lock.getWaitingThreads(c).isEmpty()) ;
        //                c.await();
        //                lock.unlock();
        //            }
        //            catch (InterruptedException e)
        //            {
        //                threadUnexpectedException();
        //            }
        //        }
        //    });

        //    Thread t2 = new Thread(new Runnable()
        //    {

        //        public void run()
        //        {
        //            try
        //            {
        //                lock.lock () ;
        //                threadAssertFalse(lock.getWaitingThreads(c).isEmpty()) ;
        //                c.await();
        //                lock.unlock();
        //            }
        //            catch (InterruptedException e)
        //            {
        //                threadUnexpectedException();
        //            }
        //        }
        //    });

        //    try
        //    {
        //        lock.lock () ;
        //        assertTrue(lock.getWaitingThreads(c).isEmpty()) ;
        //        lock.unlock();
        //        t1.start();
        //        Thread.sleep(SHORT_DELAY_MS);
        //        t2.start();
        //        Thread.sleep(SHORT_DELAY_MS);
        //        lock.lock () ;
        //        assertTrue(lock.hasWaiters(c)) ;
        //        assertTrue(lock.getWaitingThreads(c).contains(t1)) ;
        //        assertTrue(lock.getWaitingThreads(c).contains(t2)) ;
        //        c.signalAll();
        //        lock.unlock();
        //        Thread.sleep(SHORT_DELAY_MS);
        //        lock.lock () ;
        //        assertFalse(lock.hasWaiters(c)) ;
        //        assertTrue(lock.getWaitingThreads(c).isEmpty()) ;
        //        lock.unlock();
        //        t1.join(SHORT_DELAY_MS);
        //        t2.join(SHORT_DELAY_MS);
        //        assertFalse(t1.isAlive());
        //        assertFalse(t2.isAlive());
        //    }
        //    catch (Exception ex)
        //    {
        //        unexpectedException();
        //    }
        //}

        ///** A helper class for uninterruptible wait tests */
        //class UninterruptableThread extends Thread
        //{
        //    private ReentrantLock lock;
        //    private Condition c;

        //    public volatile boolean canAwake = false;
        //    public volatile boolean interrupted = false;
        //    public volatile boolean lockStarted = false;

        //    public UninterruptableThread(ReentrantLock lock, Condition c)
        //    {
        //        this.lock = lock;
        //        this.c = c;
        //    }

        //    public synchronized void run()
        //    {
        //        lock.lock () ;
        //        lockStarted = true;

        //        while (!canAwake)
        //        {
        //            c.awaitUninterruptibly();
        //        }

        //        interrupted = isInterrupted();
        //        lock.unlock();
        //    }
        //}

        ///**
        // * awaitUninterruptibly doesn't abort on interrupt
        // */
        //public void testAwaitUninterruptibly()
        //{
        //    final ReentrantLock lock = new ReentrantLock();
        //    final Condition c = lock.newCondition();
        //    UninterruptableThread thread = new UninterruptableThread(lock, c);

        //    try
        //    {
        //        thread.start();

        //        while (!thread.lockStarted)
        //        {
        //            Thread.sleep(100);
        //        }

        //        lock.lock () ;
        //        try
        //        {
        //            thread.interrupt();
        //            thread.canAwake = true;
        //            c.signal();
        //        }
        //        finally
        //        {
        //            lock.unlock();
        //        }

        //        thread.join();
        //        assertTrue(thread.interrupted);
        //        assertFalse(thread.isAlive());
        //    }
        //    catch (Exception ex)
        //    {
        //        unexpectedException();
        //    }
        //}

        ///**
        // * await is interruptible
        // */
        //public void testAwait_Interrupt()
        //{
        //    final ReentrantLock lock = new ReentrantLock();
        //    final Condition c = lock.newCondition();
        //    Thread t = new Thread(new Runnable()
        //    {

        //        public void run()
        //        {
        //            try
        //            {
        //                lock.lock () ;
        //                c.await();
        //                lock.unlock();
        //                threadShouldThrow();
        //            }
        //            catch (InterruptedException success)
        //            {
        //            }
        //        }
        //    });

        //    try {
        //        t.start();
        //        Thread.sleep(SHORT_DELAY_MS);
        //        t.interrupt();
        //        t.join(SHORT_DELAY_MS);
        //        assertFalse(t.isAlive());
        //    }
        //    catch (Exception ex) {
        //        unexpectedException();
        //    }
        //}

        ///**
        // * awaitNanos is interruptible
        // */
        //public void testAwaitNanos_Interrupt()
        //{
        //    final ReentrantLock lock = new ReentrantLock();
        //    final Condition c = lock.newCondition();
        //    Thread t = new Thread(new Runnable()
        //    {

        //        public void run()
        //        {
        //            try
        //            {
        //                lock.lock () ;
        //                c.awaitNanos(1000 * 1000 * 1000); // 1 sec
        //                lock.unlock();
        //                threadShouldThrow();
        //            }
        //            catch (InterruptedException success)
        //            {
        //            }
        //        }
        //    });

        //    try
        //    {
        //        t.start();
        //        Thread.sleep(SHORT_DELAY_MS);
        //        t.interrupt();
        //        t.join(SHORT_DELAY_MS);
        //        assertFalse(t.isAlive());
        //    }
        //    catch (Exception ex)
        //    {
        //        unexpectedException();
        //    }
        //}

        ///**
        // * awaitUntil is interruptible
        // */
        //public void testAwaitUntil_Interrupt()
        //{
        //    final ReentrantLock lock = new ReentrantLock();
        //    final Condition c = lock.newCondition();
        //    Thread t = new Thread(new Runnable()
        //    {

        //        public void run()
        //        {
        //            try
        //            {
        //                lock.lock () ;
        //                java.util.Date d = new java.util.Date();
        //                c.awaitUntil(new java.util.Date(d.getTime() + 10000));
        //                lock.unlock();
        //                threadShouldThrow();
        //            }
        //            catch (InterruptedException success)
        //            {
        //            }
        //        }
        //    });

        //    try
        //    {
        //        t.start();
        //        Thread.sleep(SHORT_DELAY_MS);
        //        t.interrupt();
        //        t.join(SHORT_DELAY_MS);
        //        assertFalse(t.isAlive());
        //    }
        //    catch (Exception ex)
        //    {
        //        unexpectedException();
        //    }
        //}

        ///**
        // * signalAll wakes up all threads
        // */
        //public void testSignalAll()
        //{
        //    final ReentrantLock lock = new ReentrantLock();
        //    final Condition c = lock.newCondition();
        //    Thread t1 = new Thread(new Runnable()
        //    {

        //        public void run()
        //        {
        //            try
        //            {
        //                lock.lock () ;
        //                c.await();
        //                lock.unlock();
        //            }
        //            catch (InterruptedException e)
        //            {
        //                threadUnexpectedException();
        //            }
        //        }
        //    });

        //    Thread t2 = new Thread(new Runnable()
        //    {

        //        public void run()
        //        {
        //            try
        //            {
        //                lock.lock () ;
        //                c.await();
        //                lock.unlock();
        //            }
        //            catch (InterruptedException e)
        //            {
        //                threadUnexpectedException();
        //            }
        //        }
        //    });

        //    try
        //    {
        //        t1.start();
        //        t2.start();
        //        Thread.sleep(SHORT_DELAY_MS);
        //        lock.lock () ;
        //        c.signalAll();
        //        lock.unlock();
        //        t1.join(SHORT_DELAY_MS);
        //        t2.join(SHORT_DELAY_MS);
        //        assertFalse(t1.isAlive());
        //        assertFalse(t2.isAlive());
        //    }
        //    catch (Exception ex)
        //    {
        //        unexpectedException();
        //    }
        //}

        ///**
        // * await after multiple reentrant locking preserves lock count
        // */
        //public void testAwaitLockCount()
        //{
        //    final ReentrantLock lock = new ReentrantLock();
        //    final Condition c = lock.newCondition();
        //    Thread t1 = new Thread(new Runnable()
        //    {

        //        public void run()
        //        {
        //            try
        //            {
        //                lock.lock () ;
        //                threadAssertEquals(1, lock.getHoldCount()) ;
        //                c.await();
        //                threadAssertEquals(1, lock.getHoldCount()) ;
        //                lock.unlock();
        //            }
        //            catch (InterruptedException e)
        //            {
        //                threadUnexpectedException();
        //            }
        //        }
        //    });

        //    Thread t2 = new Thread(new Runnable()
        //    {

        //        public void run()
        //        {
        //            try
        //            {
        //                lock.lock () ;
        //                lock.lock () ;
        //                threadAssertEquals(2, lock.getHoldCount()) ;
        //                c.await();
        //                threadAssertEquals(2, lock.getHoldCount()) ;
        //                lock.unlock();
        //                lock.unlock();
        //            }
        //            catch (InterruptedException e)
        //            {
        //                threadUnexpectedException();
        //            }
        //        }
        //    });

        //    try
        //    {
        //        t1.start();
        //        t2.start();
        //        Thread.sleep(SHORT_DELAY_MS);
        //        lock.lock () ;
        //        c.signalAll();
        //        lock.unlock();
        //        t1.join(SHORT_DELAY_MS);
        //        t2.join(SHORT_DELAY_MS);
        //        assertFalse(t1.isAlive());
        //        assertFalse(t2.isAlive());
        //    }
        //    catch (Exception ex)
        //    {
        //        unexpectedException();
        //    }
        //}

        ///**
        // * A serialized lock deserializes as unlocked
        // */
        //public void testSerialization()
        //{
        //    ReentrantLock l = new ReentrantLock();
        //    l.lock () ;
        //    l.unlock();

        //    try
        //    {
        //        ByteArrayOutputStream bout = new ByteArrayOutputStream(10000);
        //        ObjectOutputStream out = new ObjectOutputStream(new BufferedOutputStream(bout));
        //            out.writeObject(l);
        //            out.close();

        //        ByteArrayInputStream bin = new ByteArrayInputStream(bout.toByteArray());
        //        ObjectInputStream in = new ObjectInputStream(new BufferedInputStream(bin));
        //        ReentrantLock r = (ReentrantLock) in.readObject();
        //        r.lock () ;
        //        r.unlock();
        //    }
        //    catch (Exception e)
        //    {
        //        e.printStackTrace();
        //        unexpectedException();
        //    }
        //}

        /**
         * toString indicates current lock state
         */
        [Test]
        [Ignore("LUCENENET: Not implemented")]
        public void TestToString()
        {
            ReentrantLock @lock = new ReentrantLock();
            string us = @lock.ToString();
            assertTrue(us.IndexOf("Unlocked", StringComparison.Ordinal) >= 0);
            @lock.Lock();
            string ls = @lock.ToString();
            assertTrue(ls.IndexOf("Locked", StringComparison.Ordinal) >= 0);
        }

        //[Test]
        //[LuceneNetSpecific]
        //public void TestReentry()
        //{
        //    ReentrantLock @lock = new ReentrantLock();
        //    assertFalse(@lock.IsLocked);
        //    @lock.Lock();
        //    assertTrue(@lock.IsLocked);
        //    @lock.TryLock();
        //    assertTrue(@lock.IsLocked);
        //    @lock.Lock();
        //    assertTrue(@lock.IsLocked);

        //    // Now unwind the stack
        //    @lock.Unlock();
        //    assertTrue(@lock.IsLocked);
        //    @lock.Unlock();
        //    assertTrue(@lock.IsLocked);
        //    @lock.Unlock();
        //    assertFalse(@lock.IsLocked);

        //    Assert.Throws<SynchronizationLockException>(() => @lock.Unlock());
        //}

        //[Test]
        //[LuceneNetSpecific]
        //public async Task TestReentryWithTasks()
        //{
        //    var @lock = new ReentrantLock();
        //    assertFalse(@lock.IsLocked);

        //    var task1 = Task.Run(() =>
        //    {
        //        @lock.Lock();
        //        assertEquals(1, @lock.reentrantCount.Value);
        //        assertTrue(@lock.IsLocked);

        //        @lock.TryLock();
        //        assertEquals(2, @lock.reentrantCount.Value);
        //        assertTrue(@lock.IsLocked);

        //        @lock.Lock();
        //        assertEquals(3, @lock.reentrantCount.Value);
        //        assertTrue(@lock.IsLocked);

        //        // Simulate work
        //        Thread.Sleep(300);

        //        // Now unwind the stack
        //        @lock.Unlock();
        //        assertEquals(2, @lock.reentrantCount.Value);
        //        assertTrue(@lock.IsLocked);

        //        @lock.Unlock();
        //        assertEquals(1, @lock.reentrantCount.Value);
        //        assertTrue(@lock.IsLocked);

        //        //lock (@lock.syncLock)
        //        {
        //            @lock.Unlock();
        //            assertEquals(0, @lock.reentrantCount.Value);
        //            assertFalse(@lock.IsLocked);
        //        }

        //        Assert.Throws<SynchronizationLockException>(() => @lock.Unlock());
        //    });

        //    var task2 = Task.Run(async () =>
        //    {
        //        // Wait a bit to ensure task1 has started and locked
        //        await Task.Delay(100);

        //        // Try to lock
        //        @lock.Lock();

        //        // Simulate work
        //        Thread.Sleep(100);

        //        //lock (@lock.syncLock)
        //        {
        //            @lock.Unlock();
        //            assertEquals(0, @lock.reentrantCount.Value);
        //            assertFalse(@lock.IsLocked);
        //        }

        //        Assert.Throws<SynchronizationLockException>(() => @lock.Unlock());

        //    });

        //    await Task.WhenAll(task1, task2);
        //}

        //#if DEBUG
        //        private static void DoWorkReentrant(ReentrantLock @lock)
        //        {
        //            @lock.Lock();
        //            assertEquals(1, @lock.reentrantCount.Value);
        //            assertTrue(@lock.IsLocked);

        //            @lock.TryLock();
        //            assertEquals(2, @lock.reentrantCount.Value);
        //            assertTrue(@lock.IsLocked);

        //            @lock.Lock();
        //            assertEquals(3, @lock.reentrantCount.Value);
        //            assertTrue(@lock.IsLocked);

        //            // Simulate work
        //            Thread.Sleep(300);

        //            // Now unwind the stack
        //            @lock.Unlock();
        //            assertEquals(2, @lock.reentrantCount.Value);
        //            assertTrue(@lock.IsLocked);

        //            @lock.Unlock();
        //            assertEquals(1, @lock.reentrantCount.Value);
        //            assertTrue(@lock.IsLocked);

        //            lock (@lock.syncLock)
        //            {
        //                @lock.Unlock();
        //                assertEquals(0, @lock.reentrantCount.Value);
        //                assertFalse(@lock.IsLocked);
        //            }

        //            Assert.Throws<SynchronizationLockException>(() => @lock.Unlock());
        //        }


        //        [Test]
        //        [Slow]
        //        [LuceneNetSpecific]
        //        public void TestQueueCompletionWithTasks()
        //        {
        //            var @lock = new ReentrantLock();
        //            assertEquals(0, @lock.queueCount);
        //            assertEquals(0, @lock.dequeueCount);
        //            assertEquals(0, @lock.poolReturnCount);

        //            var tasks = new List<Task>();

        //            for (int i = 0; i < 10; i++)
        //            {
        //                tasks.Add(Task.Factory.StartNew((@lock) =>
        //                {
        //                    DoWorkReentrant((ReentrantLock)@lock);
        //                }, @lock));
        //            }

        //            // Wait for all tasks to complete
        //            Task.WaitAll(tasks.ToArray());

        //            // Make sure everything that was queued has also been dequeued
        //            assertTrue(@lock.queueCount > 0);
        //            assertEquals(@lock.queueCount, @lock.dequeueCount);
        //            assertEquals(@lock.queueCount, @lock.poolReturnCount);

        //            Console.WriteLine($"{nameof(@lock.queueCount)}: {@lock.queueCount}");
        //            Console.WriteLine($"{nameof(@lock.dequeueCount)}: {@lock.dequeueCount}");
        //            Console.WriteLine($"{nameof(@lock.poolReturnCount)}: {@lock.poolReturnCount}");
        //        }
        //#endif
    }
}
