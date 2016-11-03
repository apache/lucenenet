using System;
using System.Threading;

namespace Lucene.Net.Store
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
    /// An interprocess mutex lock.
    /// <p>Typical use might look like:<pre class="prettyprint">
    /// new Lock.With(directory.makeLock("my.lock")) {
    ///     public Object doBody() {
    ///       <i>... code to execute while locked ...</i>
    ///     }
    ///   }.run();
    /// </pre>
    /// </summary>
    /// <seealso cref="Directory#makeLock(String)"/>
    public abstract class Lock : IDisposable
    {
        /// <summary>
        /// How long <seealso cref="#obtain(long)"/> waits, in milliseconds,
        ///  in between attempts to acquire the lock.
        /// </summary>
        public static long LOCK_POLL_INTERVAL = 1000;

        /// <summary>
        /// Pass this value to <seealso cref="#obtain(long)"/> to try
        ///  forever to obtain the lock.
        /// </summary>
        public const long LOCK_OBTAIN_WAIT_FOREVER = -1;

        /// <summary>
        /// Attempts to obtain exclusive access and immediately return
        ///  upon success or failure.  Use <seealso cref="#close"/> to
        ///  release the lock. </summary>
        /// <returns> true iff exclusive access is obtained </returns>
        public abstract bool Obtain();

        /// <summary>
        /// If a lock obtain called, this failureReason may be set
        /// with the "root cause" Exception as to why the lock was
        /// not obtained.
        /// </summary>
        public Exception FailureReason { get; protected set; }

        /// <summary>
        /// Attempts to obtain an exclusive lock within amount of
        ///  time given. Polls once per <seealso cref="#LOCK_POLL_INTERVAL"/>
        ///  (currently 1000) milliseconds until lockWaitTimeout is
        ///  passed. </summary>
        /// <param name="lockWaitTimeout"> length of time to wait in
        ///        milliseconds or {@link
        ///        #LOCK_OBTAIN_WAIT_FOREVER} to retry forever </param>
        /// <returns> true if lock was obtained </returns>
        /// <exception cref="LockObtainFailedException"> if lock wait times out </exception>
        /// <exception cref="IllegalArgumentException"> if lockWaitTimeout is
        ///         out of bounds </exception>
        /// <exception cref="System.IO.IOException"> if obtain() throws System.IO.IOException </exception>
        public bool Obtain(long lockWaitTimeout)
        {
            FailureReason = null;
            bool locked = Obtain();
            if (lockWaitTimeout < 0 && lockWaitTimeout != LOCK_OBTAIN_WAIT_FOREVER)
            {
                throw new System.ArgumentException("lockWaitTimeout should be LOCK_OBTAIN_WAIT_FOREVER or a non-negative number (got " + lockWaitTimeout + ")");
            }

            long maxSleepCount = lockWaitTimeout / LOCK_POLL_INTERVAL;
            long sleepCount = 0;
            while (!locked)
            {
                if (lockWaitTimeout != LOCK_OBTAIN_WAIT_FOREVER && sleepCount++ >= maxSleepCount)
                {
                    string reason = "Lock obtain timed out: " + this.ToString();
                    if (FailureReason != null)
                    {
                        reason += ": " + FailureReason;
                    }
                    LockObtainFailedException e = new LockObtainFailedException(reason);
                    e = FailureReason != null
                                        ? new LockObtainFailedException(reason, FailureReason)
                                        : new LockObtainFailedException(reason);
                    throw e;
                }

#if !NETSTANDARD
                try
                {
#endif
                    Thread.Sleep(TimeSpan.FromMilliseconds(LOCK_POLL_INTERVAL));
#if !NETSTANDARD                
                }
                catch (ThreadInterruptedException ie)
                {
                    throw new ThreadInterruptedException("Thread Interrupted Exception", ie);
                }
#endif
                locked = Obtain();
            }
            return locked;
        }

        /// <summary>
        /// Releases exclusive access. </summary>
        public abstract void Release();

        public virtual void Dispose()
        {
            Release();
        }

        /// <summary>
        /// Returns true if the resource is currently locked.  Note that one must
        /// still call <seealso cref="#obtain()"/> before using the resource.
        /// </summary>
        public abstract bool Locked { get; }

        /// <summary>
        /// Utility class for executing code with exclusive access. </summary>
        public abstract class With
        {
            internal Lock @lock;
            internal long LockWaitTimeout;

            /// <summary>
            /// Constructs an executor that will grab the named lock. </summary>
            public With(Lock @lock, long lockWaitTimeout)
            {
                this.@lock = @lock;
                this.LockWaitTimeout = lockWaitTimeout;
            }

            /// <summary>
            /// Code to execute with exclusive access. </summary>
            protected internal abstract object DoBody();

            /// <summary>
            /// Calls <seealso cref="#doBody"/> while <i>lock</i> is obtained.  Blocks if lock
            /// cannot be obtained immediately.  Retries to obtain lock once per second
            /// until it is obtained, or until it has tried ten times. Lock is released when
            /// <seealso cref="#doBody"/> exits. </summary>
            /// <exception cref="LockObtainFailedException"> if lock could not
            /// be obtained </exception>
            /// <exception cref="System.IO.IOException"> if <seealso cref="Lock#obtain"/> throws System.IO.IOException </exception>
            public virtual object Run()
            {
                bool locked = false;
                try
                {
                    locked = @lock.Obtain(LockWaitTimeout);
                    return DoBody();
                }
                finally
                {
                    if (locked)
                    {
                        @lock.Release();
                    }
                }
            }
        }
    }
}