using Microsoft.Extensions.ObjectPool;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
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
    /// Lock object used to emulate ReentrantLock. This rougly represents "unfair" locking that is used in Java.
    /// The <see cref="TryLock()"/> method takes precedence over threads that are queued on <see cref="Lock()"/>.
    /// <para/>
    /// <see cref="Thread.Interrupt()"/> is not supported and if used, the behavior may differ from Java.
    /// <para />
    /// This implementation supports tasks as well as threads. That is, the reference counting is done using
    /// <see cref="AsyncLocal{T}"/> to ensure the counts are tracked for individual tasks. This differs from the Java
    /// implementation, but makes it safer to use with .NET.
    /// </summary>
    internal class ReentrantLock
    {
        // LUCENENET TODO: Figure out how to dispose this when all consumers of ReentrantLock no longer need it.
        private static readonly ObjectPool<ManualResetEventSlim> waitHandlePool = ObjectPool.Create<ManualResetEventSlim>();
        // This lock is for management of the queue. It does not represent the current lock object.
        internal readonly object syncLock = new object(); // internal for testing
        private readonly Queue<LockItem> queue = new Queue<LockItem>();
        private Thread? owningThread; // null if the lock instance is free. We set the thread here in case we need to port any more of ReentrantLock in the future.
        internal readonly AsyncLocal<int> reentrantCount = new AsyncLocal<int>(); // internal for testing
        private readonly AsyncLocal<ManualResetEventSlim> dequeuedTaskWaitHandle = new AsyncLocal<ManualResetEventSlim>();

#if DEBUG
        internal int queueCount; // internal for testing
        internal int dequeueCount; // internal for testing
        internal int poolReturnCount; // internal for testing
#endif

        public void Lock()
        {
            Thread currentThread = Thread.CurrentThread;

            UninterruptableMonitor.Enter(syncLock);
            try
            {
                if (owningThread == currentThread)
                {
                    reentrantCount.Value++;
                    return;
                }

                // We use unfair locking. This ignores the queue and immediately
                // takes the lock if it is available.
                if (owningThread is null)
                {
                    TakeLock(currentThread, reentrantCount);
                    return;
                }

                ManualResetEventSlim currentTaskWaitHandle = waitHandlePool.Get();
                queue.Enqueue(new LockItem(currentThread, currentTaskWaitHandle));
                reentrantCount.Value = 1; // We must reset this for the current task before giving up the lock, since there is no way to put it into the queue
                dequeuedTaskWaitHandle.Value = null!; // Safety to ensure we don't lose the wait handle until we are done with it in Unlock().
#if DEBUG
                queueCount++;
#endif
                while (true)
                {
                    UninterruptableMonitor.Exit(syncLock);
                    try
                    {
                        currentTaskWaitHandle.Wait(); // Wait outside of the lock on syncLock
                        Thread.Yield(); // Allow TryLock() to win
                    }
                    finally
                    {
                        UninterruptableMonitor.Enter(syncLock);
                    }

                    // Try to take the lock. Someone could have beat us. LockItem may not represent
                    // the current task.
                    if (owningThread is null && queue.TryDequeue(out LockItem item))
                    {
#if DEBUG
                        dequeueCount++;
#endif
                        // This will be returned to the pool in Unlock(). But since we no longer
                        // have it in the queue, we need to keep an AsyncLocal reference to it
                        // until that method is called.
                        dequeuedTaskWaitHandle.Value = item.WaitHandle;
                        TakeLock(item.Thread, null);
                        item.WaitHandle.Set();
                        break;
                    }
                }
            }
            finally
            {
                UninterruptableMonitor.Exit(syncLock);
            }
        }

        public void LockInterruptibly()
        {
            Thread currentThread = Thread.CurrentThread;

            Monitor.Enter(syncLock);
            try
            {
                if (owningThread == currentThread)
                {
                    reentrantCount.Value++;
                    return;
                }

                // We use unfair locking. This ignores the queue and immediately
                // takes the lock if it is available.
                if (owningThread is null)
                {
                    TakeLock(currentThread, reentrantCount);
                    return;
                }

                ManualResetEventSlim currentTaskWaitHandle = waitHandlePool.Get();
                queue.Enqueue(new LockItem(currentThread, currentTaskWaitHandle));
                reentrantCount.Value = 1; // We must reset this for the current task before giving up the lock, since there is no way to put it into the queue
                dequeuedTaskWaitHandle.Value = null!; // Safety to ensure we don't lose the wait handle until we are done with it in Unlock().
#if DEBUG
                queueCount++;
#endif
                while (true)
                {
                    Monitor.Exit(syncLock);
                    try
                    {
                        currentTaskWaitHandle.Wait(); // Wait outside of the lock on syncLock
                        Thread.Yield(); // Allow TryLock() to win
                    }
                    finally
                    {
                        Monitor.Enter(syncLock);
                    }

                    // Try to take the lock. Someone could have beat us. LockItem may not represent
                    // the current task.
                    if (owningThread is null && queue.TryDequeue(out LockItem item))
                    {
#if DEBUG
                        dequeueCount++;
#endif
                        // This will be returned to the pool in Unlock(). But since we no longer
                        // have it in the queue, we need to keep an AsyncLocal reference to it
                        // until that method is called.
                        dequeuedTaskWaitHandle.Value = item.WaitHandle;
                        TakeLock(item.Thread, null);
                        item.WaitHandle.Set();
                        break;
                    }
                }
            }
            finally
            {
                Monitor.Exit(syncLock);
            }
        }

        public void Unlock()
        {
            UninterruptableMonitor.Enter(syncLock);
            try
            {
                if (owningThread is null)
                {
                    throw new SynchronizationLockException("The lock is currently not active");
                }

                if (owningThread != Thread.CurrentThread)
                {
                    throw new SynchronizationLockException("Current thread does not own the lock");
                }

                // The current thread is done with the lock
                if ((--reentrantCount.Value) == 0)
                {
                    // Return the wait handle for the current task to the pool, since we are done with it.
                    // If we didn't have a queued task, we won't have a handle to return.
                    ManualResetEventSlim? dequeuedWaitHandle = dequeuedTaskWaitHandle.Value;
                    if (dequeuedWaitHandle != null)
                    {
#if DEBUG
                        poolReturnCount++;
#endif
                        dequeuedTaskWaitHandle.Value = null!;
                        dequeuedWaitHandle.Reset();
                        waitHandlePool.Return(dequeuedWaitHandle);
                    }

                    owningThread = null; // The lock is now free

                    // Wake up the task at the beginning of the queue to process
                    // the next queue item.
                    if (queue.TryPeek(out LockItem item))
                        item.WaitHandle.Set();
                }
            }
            finally
            {
                UninterruptableMonitor.Exit(syncLock);
            }
        }

        public bool TryLock()
        {
            // NOTE: In Java, the ReentrantLock.tryEnter() method will "barge" to the
            // front of the queue when called, so there is not a chance (or a very small chance)
            // that it will return false. This differs from Monitor.TryEnter() in .NET, which returns
            // false immediately when there are threads waiting in the queue.

            Thread currentThread = Thread.CurrentThread;

            UninterruptableMonitor.Enter(syncLock);
            try
            {
                if (owningThread == currentThread)
                {
                    reentrantCount.Value++;
                    return true;
                }

                // We use unfair locking. This ignores the queue and immediately
                // takes the lock if it is available.
                if (owningThread is null)
                {
                    TakeLock(currentThread, reentrantCount);
                    return true;
                }

                return false;
            }
            finally
            {
                UninterruptableMonitor.Exit(syncLock);
            }
        }

        public int QueueLength
        {
            get
            {
                lock (syncLock)
                {
                    return queue.Count;
                }
            }
        }

        public bool HasQueuedThreads
        {
            get
            {
                lock (syncLock)
                {
                    return queue.Count > 0;
                }
            }
        }

        public bool IsHeldByCurrentThread => owningThread == Thread.CurrentThread;

        public bool IsLocked => owningThread != null;

        public override string ToString()
            => owningThread is null ? "Unlocked" : "Locked";


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void TakeLock(Thread thread, AsyncLocal<int>? reentrantCount)
        {
            Debug.Assert(thread != null);

            owningThread = thread;
            if (reentrantCount != null)
                reentrantCount.Value = 1;
        }

        private struct LockItem
        {
            public Thread Thread { get; }
            public ManualResetEventSlim WaitHandle { get; }

            public LockItem(Thread thread, ManualResetEventSlim waitHandle)
            {
                Thread = thread;
                WaitHandle = waitHandle;
            }
        }
    }
}
