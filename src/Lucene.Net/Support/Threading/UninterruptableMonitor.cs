using System;
using System.Runtime.CompilerServices;
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
    /// <para/>
    /// <b>NOTE:</b> this is just a best effort. The BCL and other libraries we depend
    /// on don't take such measures, so any call to an API that we don't own could result
    /// in a <see cref="System.Threading.ThreadInterruptedException"/> if it attempts to
    /// aquire a lock. It is not practical to put a try/catch block around every 3rd party
    /// API call that attempts to lock. As such, Lucene.NET does not support
    /// <see cref="Thread.Interrupt()"/> and using it is discouraged.
    /// See https://github.com/apache/lucenenet/issues/526.
    /// </summary>
    internal static class UninterruptableMonitor
    {
        /// <summary>
        /// Acquires an exclusive lock on the specified object, and atomically sets a
        /// value that indicates whether the lock was taken. See
        /// <see cref="Monitor.Enter(object, ref bool)"/> for more details.
        /// <para/>
        /// If the lock is interrupted, this method will not throw a
        /// <see cref="System.Threading.ThreadInterruptedException"/>. Instead,
        /// it will reset the interrupt state. This matches the behavior of the
        /// <c>synchronized</c> keyword in Java, which never throws when the current
        /// thread is in an interrupted state. It allows us to catch
        /// <see cref="System.Threading.ThreadInterruptedException"/> in a specific part
        /// of the application, rather than allowing it to be thrown anywhere we atempt
        /// to lock.
        /// <para/>
        /// <b>NOTE:</b> this is just a best effort. The BCL and other libraries we depend
        /// on don't take such measures, so any call to an API that we don't own could result
        /// in a <see cref="System.Threading.ThreadInterruptedException"/> if it attempts to
        /// aquire a lock. It is not practical to put a try/catch block around every 3rd party
        /// API call that attempts to lock. As such, Lucene.NET does not support
        /// <see cref="Thread.Interrupt()"/> and using it is discouraged.
        /// See https://github.com/apache/lucenenet/issues/526.
        /// </summary>
        public static void Enter(object obj, ref bool lockTaken)
        {
            // enter the lock and ignore any System.Threading.ThreadInterruptedException
            try
            {
                Monitor.Enter(obj, ref lockTaken); // Fast path - don't allocate retry on stack in this case
            }
            catch (Exception ie) when (ie.IsInterruptedException())
            {
                do
                {
                    try
                    {
                        // The interrupted exception may have already cleared the flag, and this will
                        // succeed without any more exceptions
                        Monitor.Enter(obj, ref lockTaken);
                        break;
                    }
                    catch (Exception e) when (e.IsInterruptedException())
                    {
                        // try again until we succeed, since an interrupt could have happened since it was cleared
                    }
                }
                while (true);

                // The lock has been obtained, now reset the interrupted status for the
                // current thread
                Thread.CurrentThread.Interrupt();
            }
        }

        /// <summary>
        /// Acquires an exclusive lock on the specified object. See
        /// <see cref="Monitor.Enter(object)"/> for more details.
        /// <para/>
        /// If the lock is interrupted, this method will not throw a
        /// <see cref="System.Threading.ThreadInterruptedException"/>. Instead,
        /// it will reset the interrupt state. This matches the behavior of the
        /// <c>synchronized</c> keyword in Java, which never throws when the current
        /// thread is in an interrupted state. It allows us to catch
        /// <see cref="System.Threading.ThreadInterruptedException"/> in a specific part
        /// of the application, rather than allowing it to be thrown anywhere we atempt
        /// to lock.
        /// <para/>
        /// <b>NOTE:</b> this is just a best effort. The BCL and other libraries we depend
        /// on don't take such measures, so any call to an API that we don't own could result
        /// in a <see cref="System.Threading.ThreadInterruptedException"/> if it attempts to
        /// aquire a lock. It is not practical to put a try/catch block around every 3rd party
        /// API call that attempts to lock. As such, Lucene.NET does not support
        /// <see cref="Thread.Interrupt()"/> and using it is discouraged.
        /// See https://github.com/apache/lucenenet/issues/526.
        /// </summary>
        public static void Enter(object obj)
        {
            // enter the lock and ignore any System.Threading.ThreadInterruptedException
            try
            {
                Monitor.Enter(obj); // Fast path - don't allocate retry on stack in this case
            }
            catch (Exception ie) when (ie.IsInterruptedException())
            {
                do
                {
                    try
                    {
                        // The interrupted exception may have already cleared the flag, and this will
                        // succeed without any more exceptions
                        Monitor.Enter(obj);
                        break;
                    }
                    catch (Exception e) when (e.IsInterruptedException())
                    {
                        // try again until we succeed, since an interrupt could have happened since it was cleared
                    }
                }
                while (true);

                // The lock has been obtained, now reset the interrupted status for the
                // current thread
                Thread.CurrentThread.Interrupt();
            }
        }

        /// <summary>
        /// Cascades the call to <see cref="Monitor.Exit(object)"/>.
        /// <para/>
        /// Releases an exclusive lock on the specified object.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Exit(object obj)
        {
            Monitor.Exit(obj);
        }

        /// <summary>
        /// Cascades the call to <see cref="Monitor.IsEntered(object)"/>.
        /// <para/>
        /// Determines whether the current thread holds the lock on the
        /// specified object.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsEntered(object obj)
        {
            return Monitor.IsEntered(obj);
        }

        /// <summary>
        /// Cascades the call to <see cref="Monitor.TryEnter(object)"/>.
        /// <para/>
        /// Attempts to acquire an exclusive lock on the specified object.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryEnter(object obj)
        {
            return Monitor.TryEnter(obj);
        }

        /// <summary>
        /// Cascades the call to <see cref="Monitor.TryEnter(object, ref bool)"/>.
        /// <para/>
        /// Attempts to acquire an exclusive lock on the specified object, and atomically
        /// sets a value that indicates whether the lock was taken.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void TryEnter(object obj, ref bool lockTaken)
        {
            Monitor.TryEnter(obj, ref lockTaken);
        }

        /// <summary>
        /// Cascades the call to <see cref="Monitor.TryEnter(object, int)"/>.
        /// <para/>
        /// Attempts, for the specified number of milliseconds, to acquire an
        /// exclusive lock on the specified object.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryEnter(object obj, int millisecondsTimeout)
        {
            return Monitor.TryEnter(obj, millisecondsTimeout);
        }

        /// <summary>
        /// Cascades the call to <see cref="Monitor.TryEnter(object, TimeSpan)"/>.
        /// <para/>
        /// Attempts, for the specified amount of time, to acquire an exclusive
        /// lock on the specified object.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryEnter(object obj, TimeSpan timeout)
        {
            return Monitor.TryEnter(obj, timeout);
        }

        /// <summary>
        /// Cascades the call to <see cref="Monitor.TryEnter(object, int, ref bool)"/>.
        /// <para/>
        /// Attempts, for the specified number of milliseconds, to acquire an exclusive lock on the specified
        /// object, and atomically sets a value that indicates whether the lock was taken.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void TryEnter(object obj, int millisecondsTimeout, ref bool lockTaken)
        {
            Monitor.TryEnter(obj, millisecondsTimeout, ref lockTaken);
        }

        /// <summary>
        /// Cascades the call to <see cref="Monitor.TryEnter(object, TimeSpan, ref bool)"/>.
        /// <para/>
        /// Attempts, for the specified amount of time, to acquire an exclusive lock on the specified object,
        /// and atomically sets a value that indicates whether the lock was taken.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void TryEnter(object obj, TimeSpan timeout, ref bool lockTaken)
        {
            Monitor.TryEnter(obj, timeout, ref lockTaken);
        }

        /// <summary>
        /// Cascades the call to <see cref="Monitor.Pulse(object)"/>.
        /// <para/>
        /// Notifies a thread in the waiting queue of a change in the locked object's state.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Pulse(object obj)
        {
            Monitor.Pulse(obj);
        }

        /// <summary>
        /// Cascades the call to <see cref="Monitor.PulseAll(object)"/>.
        /// <para/>
        /// Notifies all waiting threads of a change in the object's state.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void PulseAll(object obj)
        {
            Monitor.PulseAll(obj);
        }

        /// <summary>
        /// Cascades the call to <see cref="Monitor.Wait(object)"/>.
        /// <para/>
        /// Releases the lock on an object and blocks the current thread until it reacquires the lock.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Wait(object obj)
        {
            Monitor.Wait(obj);
        }

        /// <summary>
        /// Cascades the call to <see cref="Monitor.Wait(object, int)"/>.
        /// <para/>
        /// Releases the lock on an object and blocks the current thread until it reacquires the lock.
        /// If the specified time-out interval elapses, the thread enters the ready queue.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Wait(object obj, int millisecondsTimeout)
        {
            Monitor.Wait(obj, millisecondsTimeout);
        }

        /// <summary>
        /// Cascades the call to <see cref="Monitor.Wait(object, TimeSpan)"/>.
        /// <para/>
        /// Releases the lock on an object and blocks the current thread until it reacquires the lock.
        /// If the specified time-out interval elapses, the thread enters the ready queue.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Wait(object obj, TimeSpan timeout)
        {
            Monitor.Wait(obj, timeout);
        }

        /// <summary>
        /// Cascades the call to <see cref="Monitor.Wait(object, int, bool)"/>.
        /// <para/>
        /// Releases the lock on an object and blocks the current thread until it
        /// reacquires the lock. If the specified time-out interval elapses, the
        /// thread enters the ready queue. This method also specifies whether the
        /// synchronization domain for the context (if in a synchronized context)
        /// is exited before the wait and reacquired afterward.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Wait(object obj, int millisecondsTimeout, bool exitContext)
        {
            Monitor.Wait(obj, millisecondsTimeout, exitContext);
        }

        /// <summary>
        /// Cascades the call to <see cref="Monitor.Wait(object, TimeSpan, bool)"/>.
        /// <para/>
        /// Releases the lock on an object and blocks the current thread until it reacquires the lock
        /// If the specified time-out interval elapses, the thread enters the ready queue. This method
        /// also specifies whether the synchronization domain for the context (if in a synchronized
        /// context) is exited before the wait and reacquired afterward.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Wait(object obj, TimeSpan timeout, bool exitContext)
        {
            Monitor.Wait(obj, timeout, exitContext);
        }
    }
}
