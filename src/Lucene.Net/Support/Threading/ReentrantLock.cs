using System;
using System.Runtime.CompilerServices;
using System.Threading;
#nullable enable

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
    /// A lock that uses an unfair locking strategy, similar to how it works in Java. This lock is unfair
    /// in that it will aquire the lock even if there are any threads waiting on <see cref="Lock()"/>.
    /// <para/>
    /// This implementation also does not use FIFO order when waiting on <see cref="Lock()"/>. Each queued thread will continue
    /// to acquire the lock continually, but yield between each iteration. So, any waiting thread could be next to
    /// aquire the lock. This differs from how it works in Java, but the overhead of fixing this behavior with a queue
    /// is probably not worth the cost.
    /// </summary>
    internal class ReentrantLock
    {
        private readonly object _lock = new object();

        /// <summary>
        /// Tries to aquire the lock. If the lock is not available, the thread will block
        /// until it can obtain the lock.
        /// <para/>
        /// FIFO order is not respected on waiting locks. Also, threads that are waiting
        /// are not allowed to sleep. Instead, they call <see cref="Thread.Yield()"/> and
        /// one of them will acquire the lock as soon as there are no other callers to
        /// <see cref="Lock()"/> or <see cref="TryLock()"/>.
        /// <para/>
        /// Threads that call <see cref="Lock()"/> and <see cref="TryLock()"/> are
        /// allowed to obtain the lock even if there are other threads waiting for it.
        /// This "barging" behavior is similar to how ReentryLock works in Java.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Lock()
        {
            while (!UninterruptableMonitor.TryEnter(_lock))
                Thread.Yield(); // Allow non-queued threads to win
        }

        /// <summary>
        /// NOTE: This is not the full implementation that correctly throws <see cref="ThreadInterruptedException"/>
        /// after <see cref="Thread.Interrupt()"/> is called. Since this is only used in tests and Lucene.NET doesn't
        /// support <see cref="Thread.Interrupt()"/>, this is okay. But if this method is ever used in production scenarios,
        /// the approach used for this lock needs to be reevaluated.
        /// </summary>
        [Obsolete("WARNING: This does not correctly throw ThreadInterruptedException and must be fixed prior to production use. This is only sufficient for testing.")]
        public void LockInterruptibly()
        {
            while (!Monitor.TryEnter(_lock))
                Thread.Yield(); // Allow non-queued threads to win
        }

        /// <summary>
        /// Releases the lock when called the same number of times as <see cref="Lock()"/>, <see cref="LockInterruptibly()"/>
        /// and <see cref="TryLock()"/> for the current task/thread. 
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Unlock()
        {
            UninterruptableMonitor.Exit(_lock);
        }

        /// <summary>
        /// Tries to aquire the lock and immediately returns a boolean value indicating
        /// whether the lock was obtained.
        /// <para/>
        /// Threads that call <see cref="Lock()"/> and <see cref="TryLock()"/> are
        /// allowed to obtain the lock even if there are other threads waiting for it.
        /// This "barging" behavior is similar to how ReentryLock works in Java.
        /// </summary>
        /// <returns><c>true</c> if the lock was obtained successfully; otherwise, <c>false</c>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryLock()
        {
            return UninterruptableMonitor.TryEnter(_lock);
        }

        /// <summary>
        /// Returns a value indicating whether the lock is held by the current thread.
        /// </summary>
        public bool IsHeldByCurrentThread => UninterruptableMonitor.IsEntered(_lock);
    }
}
