using Lucene.Net.Attributes;
using Lucene.Net.Util;
using NUnit.Framework;
using System;
using System.Threading;
using System.Threading.Tasks;
using Assert = Lucene.Net.TestFramework.Assert;

namespace Lucene.Net.Support
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
    /// LUCENENET specific: unit tests for <see cref="DrainReclaimer"/>, the lock-free
    /// per-user drain barrier that defers a cleanup action until all in-flight users
    /// have drained. These exercise the handshake at the primitive level, independent
    /// of <c>MMapDirectory</c> (its real consumer): registration, the
    /// <c>Enter</c>/<c>Exit</c> bracket and re-entrancy, fail-fast after close, and
    /// the core invariant that the cleanup runs exactly once and only after every
    /// active user has drained.
    /// </summary>
    [TestFixture]
    [LuceneNetSpecific]
    public class TestDrainReclaimer : LuceneTestCase
    {
        // ------------------------------------------------------------------
        // Registration + basic bracket
        // ------------------------------------------------------------------

        [Test]
        public void TestRegisterReturnsDistinctSlots()
        {
            var r = new DrainReclaimer();
            var a = r.Register();
            var b = r.Register();
            Assert.IsNotNull(a);
            Assert.IsNotNull(b);
            Assert.AreNotSame(a, b, "each Register must return its own slot");
        }

        [Test]
        public void TestEnterExitBalancesDepth()
        {
            var r = new DrainReclaimer();
            var slot = r.Register();
            Assert.AreEqual(0, slot.Depth);
            slot.EnterCore();
            Assert.AreEqual(1, slot.Depth, "EnterCore bumps depth");
            slot.Exit();
            Assert.AreEqual(0, slot.Depth, "Exit restores depth");
        }

        [Test]
        public void TestEnterIsReentrant()
        {
            var r = new DrainReclaimer();
            var slot = r.Register();
            slot.EnterCore();
            slot.EnterCore();
            Assert.AreEqual(2, slot.Depth, "nested Enter increments depth");
            slot.Exit();
            Assert.AreEqual(1, slot.Depth, "inner Exit leaves the outer bracket open");
            slot.Exit();
            Assert.AreEqual(0, slot.Depth);
        }

        [Test]
        public void TestReadScopeUsingEndsBracket()
        {
            var r = new DrainReclaimer();
            var slot = r.Register();
            using (slot.Enter())
            {
                Assert.AreEqual(1, slot.Depth, "the using scope holds the bracket open");
            }
            Assert.AreEqual(0, slot.Depth, "disposing the scope ends the bracket");
        }

        // ------------------------------------------------------------------
        // Fail-fast after Close
        // ------------------------------------------------------------------

        [Test]
        public void TestEnterAfterCloseThrowsAlreadyClosed()
        {
            var r = new DrainReclaimer();
            var slot = r.Register();
            r.Close(() => { });
            Assert.IsTrue(r.IsClosed);
            try
            {
                slot.EnterCore();
                Assert.Fail("Enter after Close must throw AlreadyClosed");
            }
            catch (Exception e) when (e.IsAlreadyClosedException())
            {
                // expected
            }
            Assert.AreEqual(0, slot.Depth, "a rejected Enter must not leave depth elevated");
        }

        [Test]
        public void TestSlotRegisteredAfterCloseStillFailsFast()
        {
            // Registering a brand-new user after Close is allowed (it just gets a
            // slot), but its first Enter must observe the closed flag and throw.
            var r = new DrainReclaimer();
            r.Close(() => { });
            var late = r.Register();
            try
            {
                late.EnterCore();
                Assert.Fail("Enter on a slot registered after Close must throw");
            }
            catch (Exception e) when (e.IsAlreadyClosedException())
            {
                // expected
            }
        }

        // ------------------------------------------------------------------
        // Cleanup timing: idle vs active
        // ------------------------------------------------------------------

        [Test]
        public void TestCloseRunsCleanupImmediatelyWhenIdle()
        {
            var r = new DrainReclaimer();
            r.Register(); // a registered-but-idle user must not block cleanup
            int cleaned = 0;
            r.Close(() => cleaned++);
            Assert.AreEqual(1, cleaned, "with no active user, Close runs the cleanup inline");
        }

        [Test]
        public void TestCloseBlocksUntilUserDrainsThenCleansUp()
        {
            // A user parked inside the bracket on another thread must BLOCK Close
            // (which spin-waits until the user drains) and hold off the cleanup; once
            // the user exits, Close finishes and runs the cleanup inline.
            var r = new DrainReclaimer();
            var slot = r.Register();

            var entered = new ManualResetEventSlim(false);
            var resume = new ManualResetEventSlim(false);
            slot.OnEnterForTest = () => { entered.Set(); resume.Wait(); };

            int cleaned = 0;
            var user = new Thread(() =>
            {
                slot.EnterCore(); // parks inside the bracket via OnEnterForTest
                slot.Exit();
            }) { IsBackground = true };
            user.Start();

            Assert.IsTrue(entered.Wait(TimeSpan.FromSeconds(5)), "user should park inside the bracket");

            // Close on another thread while the user is active: it must BLOCK (not
            // return) and must NOT clean up while the user is inside the bracket.
            var closer = new Thread(() => r.Close(() => Interlocked.Increment(ref cleaned)))
            { IsBackground = true };
            closer.Start();

            Thread.Sleep(150); // give Close time to spin
            Assert.IsFalse(closer.Join(TimeSpan.FromMilliseconds(1)),
                "Close must block while a user is inside the bracket, not return");
            Assert.AreEqual(0, Volatile.Read(ref cleaned),
                "cleanup must not run while a user is inside the bracket");

            // Release the user: Close observes the drain, finishes, and runs cleanup.
            resume.Set();
            Assert.IsTrue(user.Join(TimeSpan.FromSeconds(5)), "user thread should finish");
            Assert.IsTrue(closer.Join(TimeSpan.FromSeconds(5)),
                "Close must return once the user has drained");
            Assert.AreEqual(1, Volatile.Read(ref cleaned),
                "Close runs the cleanup exactly once, synchronously, after the drain");
        }

        [Test]
        public void TestCleanupRunsExactlyOnce()
        {
            // Many users active at Close; Close blocks until all drain, then fires the
            // cleanup exactly once.
            const int users = 8;
            var r = new DrainReclaimer();
            var slots = new DrainReclaimer.Slot[users];
            for (int i = 0; i < users; i++) slots[i] = r.Register();

            int cleaned = 0;
            var resume = new ManualResetEventSlim(false);
            var entered = new CountdownEvent(users);
            var threads = new Thread[users];
            for (int i = 0; i < users; i++)
            {
                var slot = slots[i];
                slot.OnEnterForTest = () => { entered.Signal(); resume.Wait(); };
                threads[i] = new Thread(() => { slot.EnterCore(); slot.Exit(); }) { IsBackground = true };
                threads[i].Start();
            }
            Assert.IsTrue(entered.Wait(TimeSpan.FromSeconds(5)), "all users should park");

            var closer = new Thread(() => r.Close(() => Interlocked.Increment(ref cleaned)))
            { IsBackground = true };
            closer.Start();

            resume.Set();
            foreach (var t in threads) Assert.IsTrue(t.Join(TimeSpan.FromSeconds(5)));
            Assert.IsTrue(closer.Join(TimeSpan.FromSeconds(5)));

            for (int i = 0; i < 2000 && Volatile.Read(ref cleaned) == 0; i++) Thread.Sleep(1);
            Assert.AreEqual(1, Volatile.Read(ref cleaned), "cleanup must run exactly once");
        }

        [Test]
        public void TestSlotsAreIndependent()
        {
            // One user being active must not be confused with another being active:
            // closing while only slot A is active defers; draining A then cleans up,
            // even though idle slot B was also registered.
            var r = new DrainReclaimer();
            var a = r.Register();
            var b = r.Register();
            b.EnterCore();
            b.Exit(); // B is now idle

            a.EnterCore(); // only A active
            int cleaned = 0;
            var closer = new Thread(() => r.Close(() => Interlocked.Increment(ref cleaned)))
            { IsBackground = true };
            closer.Start();
            Thread.Sleep(150);
            Assert.AreEqual(0, Volatile.Read(ref cleaned), "A active -> deferred");

            a.Exit();
            Assert.IsTrue(closer.Join(TimeSpan.FromSeconds(5)));
            for (int i = 0; i < 2000 && Volatile.Read(ref cleaned) == 0; i++) Thread.Sleep(1);
            Assert.AreEqual(1, Volatile.Read(ref cleaned));
        }

        // ------------------------------------------------------------------
        // Concurrency stress: never clean up under an active user
        // ------------------------------------------------------------------

        [Test, LuceneNetSpecific, Slow, Nightly]
        public void TestConcurrentEnterExitVsCloseNeverCleansUnderActiveUser()
        {
            // Hammer the handshake: N users repeatedly Enter/Exit while one thread
            // Closes at a random moment. The cleanup callback asserts no user is
            // inside the bracket when it runs (would be the use-after-free analog).
            // Repeated across many iterations to shake out races.
            const int iterations = 2000;
            const int userThreads = 6;

            for (int iter = 0; iter < iterations; iter++)
            {
                var r = new DrainReclaimer();
                var slots = new DrainReclaimer.Slot[userThreads];
                for (int i = 0; i < userThreads; i++) slots[i] = r.Register();

                int active = 0;          // live count of users inside the bracket
                int violation = 0;       // set if cleanup saw active > 0
                int cleaned = 0;
                var stop = new ManualResetEventSlim(false);

                var workers = new Task[userThreads];
                for (int i = 0; i < userThreads; i++)
                {
                    var slot = slots[i];
                    workers[i] = Task.Run(() =>
                    {
                        try
                        {
                            while (!stop.IsSet)
                            {
                                slot.EnterCore();
                                Interlocked.Increment(ref active);
                                // tiny critical section
                                Interlocked.Decrement(ref active);
                                slot.Exit();
                            }
                        }
                        catch (Exception e) when (e.IsAlreadyClosedException())
                        {
                            // expected once Close wins; EnterCore threw before we
                            // incremented active, so nothing to undo.
                        }
                    });
                }

                // Let the workers run a moment, then close.
                Thread.Yield();
                r.Close(() =>
                {
                    Interlocked.Increment(ref cleaned);
                    if (Volatile.Read(ref active) != 0)
                    {
                        Interlocked.Exchange(ref violation, 1);
                    }
                });

                stop.Set();
                Task.WaitAll(workers);

                Assert.AreEqual(0, Volatile.Read(ref violation),
                    $"iteration {iter}: cleanup ran while a user was inside the bracket");
                Assert.AreEqual(1, Volatile.Read(ref cleaned),
                    $"iteration {iter}: cleanup must run exactly once");
                // After everything drains, a final check: no slot left elevated.
                foreach (var s in slots)
                {
                    Assert.AreEqual(0, Volatile.Read(ref s.Depth),
                        $"iteration {iter}: a slot was left with depth != 0");
                }
            }
        }
    }
}
