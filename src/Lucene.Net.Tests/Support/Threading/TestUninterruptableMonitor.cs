using J2N.Threading;
using J2N.Threading.Atomic;
using Lucene.Net.Attributes;
using Lucene.Net.Support.Threading;
using Lucene.Net.Util;
using NUnit.Framework;
using RandomizedTesting.Generators;
using System;
using System.Threading;
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

    [TestFixture]
    public class TestUninterruptableMonitor : LuceneTestCase
    {
        private class TransactionlThreadInterrupt : ThreadJob
        {
            private static AtomicInt32 transactionNumber = new AtomicInt32(0);

            // Share locks between threads
            private static readonly object lock1 = new object();
            private static readonly object lock2 = new object();
            private static readonly object lock3 = new object();

            internal volatile bool failed;
            internal volatile bool finish;

            internal volatile bool allowInterrupt = false;
            internal volatile bool transactionInProgress = false;
            

            public override void Run()
            {
                while (!finish)
                {
                    try
                    {
                        TransactionalMethod();
                        TransactionalMethod();
                        InterruptableMethod();
                        TransactionalMethod();
                        InterruptableMethod();

                        // Make sure these don't throw System.Threading.ThreadInterruptedException
                        Assert.IsFalse(UninterruptableMonitor.IsEntered(lock1));
                        Assert.IsFalse(UninterruptableMonitor.IsEntered(lock2));
                        Assert.IsFalse(UninterruptableMonitor.IsEntered(lock3));

                        if (UninterruptableMonitor.TryEnter(lock1))
                        {
                            try
                            {
                                Assert.IsTrue(UninterruptableMonitor.IsEntered(lock1));
                            }
                            finally
                            {
                                UninterruptableMonitor.Exit(lock1);
                            }
                        }

                        allowInterrupt = true;
                    }
                    catch (Util.ThreadInterruptedException re)
                    {
                        // Success - we received the correct exception type
                        Console.WriteLine("TEST: got interrupt");
                        Console.WriteLine(GetToStringFrom(re));

                        Exception e = re.InnerException;
                        Assert.IsTrue(e is System.Threading.ThreadInterruptedException);

                        // Make sure we didn't interrupt in the middle of a transaction
                        Assert.IsFalse(transactionInProgress);

                        if (finish)
                        {
                            break;
                        }
                    }
                    catch (Exception t) when (t.IsThrowable())
                    {
                        Console.WriteLine("FAILED; unexpected exception");
                        Console.WriteLine(GetToStringFrom(t));

                        // Make sure we didn't error in the middle of a transaction
                        Assert.IsFalse(transactionInProgress);

                        failed = true;
                        break;
                    }
                }
            }


            private void TransactionalMethod()
            {
                Assert.IsFalse(transactionInProgress, "The prior transaction failed to complete (was interrupted).");
                transactionInProgress = true;
                int transactionId = transactionNumber.IncrementAndGet();
                if (Verbose)
                {
                    Console.WriteLine($"transaction STARTED: {transactionId}");
                }

                // Nested lock test
                UninterruptableMonitor.Enter(lock1);
                try
                {
                    if (Verbose)
                    {
                        Console.WriteLine("acquired lock1 successfully");
                        Console.WriteLine("sleeping...");
                    }

                    // Use SpinWait instead of Sleep to demonstrate the 
                    // effect of calling Interrupt on a running thread.
                    Thread.SpinWait(1000000);

                    UninterruptableMonitor.Enter(lock2);
                    try
                    {
                        if (Verbose)
                        {
                            Console.WriteLine("acquired lock2 successfully");
                            Console.WriteLine("sleeping...");
                        }

                        // Use SpinWait instead of Sleep to demonstrate the 
                        // effect of calling Interrupt on a running thread.
                        Thread.SpinWait(1000000);
                    }
                    finally
                    {
                        UninterruptableMonitor.Exit(lock2);
                    }
                }
                finally
                {
                    UninterruptableMonitor.Exit(lock1);
                }

                // inline lock test
                UninterruptableMonitor.Enter(lock3);
                try
                {
                    if (Verbose)
                    {
                        Console.WriteLine("acquired lock3 successfully");
                        Console.WriteLine("sleeping...");
                    }

                    // Use SpinWait instead of Sleep to demonstrate the 
                    // effect of calling Interrupt on a running thread.
                    Thread.SpinWait(1000000);
                }
                finally
                {
                    UninterruptableMonitor.Exit(lock3);
                }

                if (Verbose)
                {
                    Console.WriteLine($"transaction COMPLETED: {transactionId}");
                }

                transactionInProgress = false;
            }

            private void InterruptableMethod()
            {
                UninterruptableMonitor.Enter(lock1);
                try
                {
                    if (Verbose)
                    {
                        Console.WriteLine("acquired lock1 successfully");
                        Console.WriteLine("sleeping...");
                    }

                    try
                    {
                        UninterruptableMonitor.Wait(lock1, TimeSpan.FromMilliseconds(200));
                    }
                    catch (Exception ie) when (ie.IsInterruptedException())
                    {
                        throw new Util.ThreadInterruptedException(ie);
                    }
                }
                finally
                {
                    UninterruptableMonitor.Exit(lock1);
                }
            }

            /// <summary>
            /// Safely gets the ToString() of an exception while ignoring any System.Threading.ThreadInterruptedException and retrying.
            /// </summary>
            private string GetToStringFrom(Exception exception)
            {
                // Clear interrupt state:
                try
                {
                    Thread.Sleep(0);
                }
                catch (Exception ie) when (ie.IsInterruptedException())
                {
                    // ignore
                }
                try
                {
                    return exception.ToString();
                }
                catch (Exception ie) when (ie.IsInterruptedException())
                {
                    return GetToStringFrom(exception);
                }
            }
        }

        [Test, LuceneNetSpecific]
        [Slow]
        [Ignore("Lucene.NET does not support Thread.Interrupt(). See https://github.com/apache/lucenenet/issues/526.")]
        public virtual void TestThreadInterrupt()
        {
            TransactionlThreadInterrupt t = new TransactionlThreadInterrupt();
            t.IsBackground = (true);
            t.Start();

            // issue 300 interrupts to child thread
            int numInterrupts = AtLeast(300);
            int i = 0;
            while (i < numInterrupts)
            {
                // TODO: would be nice to also sometimes interrupt the
                // CMS merge threads too ...
                Thread.Sleep(10);
                if (t.allowInterrupt)
                {
                    i++;
                    t.Interrupt();
                }
                if (!t.IsAlive)
                {
                    break;
                }
            }
            t.finish = true;
            t.Join();

            Assert.IsFalse(t.failed);
            Assert.IsFalse(t.transactionInProgress);
        }

        [Test, LuceneNetSpecific]
        [Slow]
        [Ignore("Lucene.NET does not support Thread.Interrupt(). See https://github.com/apache/lucenenet/issues/526.")]
        public virtual void TestTwoThreadsInterrupt()
        {
            TransactionlThreadInterrupt t1 = new TransactionlThreadInterrupt();
            t1.IsBackground = (true);
            t1.Start();

            TransactionlThreadInterrupt t2 = new TransactionlThreadInterrupt();
            t2.IsBackground = (true);
            t2.Start();

            // issue 300 interrupts to child thread
            int numInterrupts = AtLeast(300);
            int i = 0;
            while (i < numInterrupts)
            {
                // TODO: would be nice to also sometimes interrupt the
                // CMS merge threads too ...
                Thread.Sleep(10);
                TransactionlThreadInterrupt t = Random.NextBoolean() ? t1 : t2;
                if (t.allowInterrupt)
                {
                    i++;
                    t.Interrupt();
                }
                if (!t1.IsAlive && !t2.IsAlive)
                {
                    break;
                }
            }
            t1.finish = true;
            t2.finish = true;
            t1.Join();
            t2.Join();

            Assert.IsFalse(t1.failed);
            Assert.IsFalse(t2.failed);
            Assert.IsFalse(t1.transactionInProgress);
            Assert.IsFalse(t2.transactionInProgress);
        }
    }
}