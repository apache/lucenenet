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

    internal class ReentrantLock
    {
        // .NET Port: lock object used to emulate ReentrantLock
        private readonly object _lock = new object();

        // .NET Port: Estimated monitor queue length
        private int _queueLength = 0;

        // .NET Port: mimic ReentrantLock -- Monitor is re-entrant
        public void Lock()
        {
            // note about queue length: in java's ReentrantLock, getQueueLength() returns the number
            // of threads waiting on entering the lock. So here, we're incrementing the count before trying to enter,
            // meaning that until enter has completed the thread is waiting so the queue is incremented. Once
            // we enter the lock, then we immediately decrement it because that thread is no longer in the queue.
            // Due to race conditions, the queue length is an estimate only.
            Interlocked.Increment(ref _queueLength);
            UninterruptableMonitor.Enter(_lock);
            Interlocked.Decrement(ref _queueLength);
        }

        // .NET Port: mimic ReentrantLock -- Monitor is re-entrant
        public void Unlock()
        {
            UninterruptableMonitor.Exit(_lock);
        }

        public bool TryLock()
        {
            Interlocked.Increment(ref _queueLength);
            // NOTE: In Java, the ReentrantLock.tryEnter() method will "barge" to the
            // front of the queue when called, so there is not a chance (or a very small chance)
            // that it will return false. This differs from Monitor.TryEnter() in .NET, which returns
            // false immediately when it cannot lock. So, our next best option is to use the overload
            // of Monitor.Enter that always returns true when the lock is taken. It may throw a
            // ThreadInterruptedException, but that will be handled by UninterruptableMonitor.
            bool lockTaken = false;
            UninterruptableMonitor.Enter(_lock, ref lockTaken);
            Interlocked.Decrement(ref _queueLength);

            return lockTaken;
        }

        public int QueueLength
        {
            get
            {
                // hold onto the estimate for the length of this method
                int estimate = _queueLength;

                // should never be < 0, but just in case, as a negative number doesn't make sense.
                return estimate <= 0 ? 0 : estimate;
            }
        }

        public bool HasQueuedThreads => _queueLength > 0;

        public bool IsHeldByCurrentThread => UninterruptableMonitor.IsEntered(_lock);
    }
}
