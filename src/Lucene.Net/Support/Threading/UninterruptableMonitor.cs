using System;
using System.Threading;

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

    /// <summary>
    /// A drop-in replacement for <see cref="Monitor"/> that doesn't throw <see cref="ThreadInterruptedException"/>
    /// when entering locks, but defers the excepetion until a wait or sleep occurs. This is to mimic the behavior in Java,
    /// which does not throw when entering a lock.
    /// </summary>
    internal static class UninterruptableMonitor
    {
        public static void Enter(object obj, ref bool lockTaken)
        {
            // enter the lock and ignore any System.Threading.ThreadInterruptedException
            try
            {
                Monitor.Enter(obj, ref lockTaken);
            }
            catch (Exception ie) when(ie.IsInterruptedException())
            {
                RetryEnter(obj, ref lockTaken);

                // The lock has been obtained, now reset the interrupted status for the
                // current thread
                Thread.CurrentThread.Interrupt();
            }
        }

        private static void RetryEnter(object obj, ref bool lockTaken)
        {
            try
            {
                // An interrupted exception may have already cleared the flag, and this will succeed without any more excpetions
                Monitor.Enter(obj, ref lockTaken);
            }
            catch (Exception ie) when (ie.IsInterruptedException())
            {
                // try again until we succeed, since an interrupt could have happened since it was cleared
                RetryEnter(obj, ref lockTaken);
            }
        }

        public static void Enter(object obj)
        {
            // enter the lock and ignore any System.Threading.ThreadInterruptedException
            try
            {
                Monitor.Enter(obj);
            }
            catch (Exception ie) when (ie.IsInterruptedException())
            {
                RetryEnter(obj);

                // The lock has been obtained, now reset the interrupted status for the
                // current thread
                Thread.CurrentThread.Interrupt();
            }
        }

        private static void RetryEnter(object obj)
        {
            try
            {
                // An interrupted exception may have already cleared the flag, and this will succeed without any more excpetions
                Monitor.Enter(obj);
            }
            catch (Exception ie) when (ie.IsInterruptedException())
            {
                // try again until we succeed, since an interrupt could have happened since it was cleared
                RetryEnter(obj);
            }
        }

        public static void Exit(object obj)
        {
            Monitor.Exit(obj);
        }

        public static bool IsEntered(object obj)
        {
            return Monitor.IsEntered(obj);
        }

        public static bool TryEnter(object obj)
        {
            return Monitor.TryEnter(obj);
        }

        public static void TryEnter(object obj, ref bool lockTaken)
        {
            Monitor.TryEnter(obj, ref lockTaken);
        }

        public static bool TryEnter(object obj, int millisecondsTimeout)
        {
            return Monitor.TryEnter(obj, millisecondsTimeout);
        }

        public static bool TryEnter(object obj, TimeSpan timeout)
        {
            return Monitor.TryEnter(obj, timeout);
        }

        public static void TryEnter(object obj, int millisecondsTimeout, ref bool lockTaken)
        {
            Monitor.TryEnter(obj, millisecondsTimeout, ref lockTaken);
        }

        public static void TryEnter(object obj, TimeSpan timeout, ref bool lockTaken)
        {
            Monitor.TryEnter(obj, timeout, ref lockTaken);
        }

        public static void Pulse(object obj)
        {
            Monitor.Pulse(obj);
        }

        public static void PulseAll(object obj)
        {
            Monitor.PulseAll(obj);
        }

        public static void Wait(object obj)
        {
            Monitor.Wait(obj);
        }

        public static void Wait(object obj, int millisecondsTimeout)
        {
            Monitor.Wait(obj, millisecondsTimeout);
        }

        public static void Wait(object obj, TimeSpan timeout)
        {
            Monitor.Wait(obj, timeout);
        }

        public static void Wait(object obj, int millisecondsTimeout, bool exitContext)
        {
            Monitor.Wait(obj, millisecondsTimeout, exitContext);
        }

        public static void Wait(object obj, TimeSpan timeout, bool exitContext)
        {
            Monitor.Wait(obj, timeout, exitContext);
        }
    }
}
