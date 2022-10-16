using Lucene.Net.Support.Threading;
using System;
using System.IO;
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
    /// <para/>Typical use might look like:
    /// 
    /// <code>
    ///     var result = Lock.With.NewAnonymous&lt;string&gt;(
    ///         @lock: directory.MakeLock("my.lock"), 
    ///         lockWaitTimeout: Lock.LOCK_OBTAIN_WAIT_FOREVER, 
    ///         doBody: () =>
    ///     {
    ///         //... code to execute while locked ...
    ///         return "the result";
    ///     }).Run();
    /// </code>
    /// </summary>
    /// <seealso cref="Directory.MakeLock(string)"/>
    public abstract class Lock : IDisposable
    {
        /// <summary>
        /// How long <see cref="Obtain(long)"/> waits, in milliseconds,
        /// in between attempts to acquire the lock.
        /// </summary>
        public static long LOCK_POLL_INTERVAL = 1000;

        /// <summary>
        /// Pass this value to <see cref="Obtain(long)"/> to try
        /// forever to obtain the lock.
        /// </summary>
        public const long LOCK_OBTAIN_WAIT_FOREVER = -1;

        /// <summary>
        /// Creates a new instance with the ability to specify the <see cref="With{T}.DoBody()"/> method
        /// through the <paramref name="doBody"/> argument
        /// <para/>
        /// Simple example:
        /// <code>
        ///     var result = Lock.With.NewAnonymous&lt;string&gt;(
        ///         @lock: directory.MakeLock("my.lock"), 
        ///         lockWaitTimeout: Lock.LOCK_OBTAIN_WAIT_FOREVER, 
        ///         doBody: () =>
        ///     {
        ///         //... code to execute while locked ...
        ///         return "the result";
        ///     }).Run();
        /// </code>
        /// <para/>
        /// The result of the operation is the value that is returned from <paramref name="doBody"/>
        /// (i.e. () => { return "the result"; }). The type of <typeparam name="T"/> determines the
        /// return type of the operation.
        /// </summary>
        /// <param name="lock"> the <see cref="Lock"/> instance to use </param>
        /// <param name="lockWaitTimeout"> length of time to wait in
        ///        milliseconds or 
        ///        <see cref="LOCK_OBTAIN_WAIT_FOREVER"/> to retry forever </param>
        /// <param name="doBody"> a delegate method that </param>
        /// <returns>The value that is returned from the <paramref name="doBody"/> delegate method (i.e. () => { return theObject; })</returns>
        public static With<T> NewAnonymous<T>(Lock @lock, int lockWaitTimeout, Func<T> doBody)
        {
            return new AnonymousWith<T>(@lock, lockWaitTimeout, doBody);
        }

        /// <summary>
        /// Attempts to obtain exclusive access and immediately return
        /// upon success or failure.  Use <see cref="Dispose()"/> to
        /// release the lock. </summary>
        /// <returns> true iff exclusive access is obtained </returns>
        public abstract bool Obtain();

        /// <summary>
        /// If a lock obtain called, this failureReason may be set
        /// with the "root cause" <see cref="Exception"/> as to why the lock was
        /// not obtained.
        /// </summary>
        protected internal Exception FailureReason { get; set; }

        /// <summary>
        /// Attempts to obtain an exclusive lock within amount of
        /// time given. Polls once per <see cref="LOCK_POLL_INTERVAL"/>
        /// (currently 1000) milliseconds until <paramref name="lockWaitTimeout"/> is
        /// passed.
        /// </summary>
        /// <param name="lockWaitTimeout"> length of time to wait in
        ///        milliseconds or 
        ///        <see cref="LOCK_OBTAIN_WAIT_FOREVER"/> to retry forever </param>
        /// <returns> <c>true</c> if lock was obtained </returns>
        /// <exception cref="LockObtainFailedException"> if lock wait times out </exception>
        /// <exception cref="ArgumentOutOfRangeException"> if <paramref name="lockWaitTimeout"/> is
        ///         out of bounds </exception>
        /// <exception cref="IOException"> if <see cref="Obtain()"/> throws <see cref="IOException"/> </exception>
        public bool Obtain(long lockWaitTimeout)
        {
            FailureReason = null;
            bool locked = Obtain();
            if (lockWaitTimeout < 0 && lockWaitTimeout != LOCK_OBTAIN_WAIT_FOREVER)
            {
                throw new ArgumentOutOfRangeException(nameof(lockWaitTimeout), "lockWaitTimeout should be LOCK_OBTAIN_WAIT_FOREVER or a non-negative number (got " + lockWaitTimeout + ")"); // LUCENENET specific - changed from IllegalArgumentException to ArgumentOutOfRangeException (.NET convention)
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
                    LockObtainFailedException e = FailureReason != null
                        ? new LockObtainFailedException(reason, FailureReason)
                        : new LockObtainFailedException(reason);
                    throw e;
                }

                try
                {
                    Thread.Sleep(TimeSpan.FromMilliseconds(LOCK_POLL_INTERVAL));
                }
                catch (Exception ie) when (ie.IsInterruptedException())
                {
                    throw new Util.ThreadInterruptedException(ie);
                }

                locked = Obtain();
            }
            return locked;
        }
        
        /// <summary>
        /// Releases exclusive access. </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases exclusive access. </summary>
        protected abstract void Dispose(bool disposing);


        /// <summary>
        /// Returns <c>true</c> if the resource is currently locked.  Note that one must
        /// still call <see cref="Obtain()"/> before using the resource.
        /// </summary>
        public abstract bool IsLocked();


        /// <summary>
        /// Utility class for executing code with exclusive access. </summary>
        public abstract class With<T> // LUCENENET specific - made generic so we don't need to deal with casting
        {
            private readonly Lock @lock; // LUCENENET: marked readonly
            private readonly long lockWaitTimeout; // LUCENENET: marked readonly

            /// <summary>
            /// Constructs an executor that will grab the named <paramref name="lock"/>. </summary>
            /// <param name="lock"> the <see cref="Lock"/> instance to use </param>
            /// <param name="lockWaitTimeout"> length of time to wait in
            ///        milliseconds or 
            ///        <see cref="LOCK_OBTAIN_WAIT_FOREVER"/> to retry forever </param>
            protected With(Lock @lock, long lockWaitTimeout) // LUCENENET: CA1012: Abstract types should not have constructors (marked protected)
            {
                this.@lock = @lock;
                this.lockWaitTimeout = lockWaitTimeout;
            }

            /// <summary>
            /// Code to execute with exclusive access. </summary>
            protected abstract T DoBody();

            /// <summary>
            /// Calls <see cref="DoBody"/> while <i>lock</i> is obtained.  Blocks if lock
            /// cannot be obtained immediately.  Retries to obtain lock once per second
            /// until it is obtained, or until it has tried ten times. Lock is released when
            /// <see cref="DoBody"/> exits. </summary>
            /// <exception cref="LockObtainFailedException"> if lock could not
            /// be obtained </exception>
            /// <exception cref="IOException"> if <see cref="Lock.Obtain()"/> throws <see cref="IOException"/> </exception>
            public virtual T Run()
            {
                bool locked = false;
                try
                {
                    locked = @lock.Obtain(lockWaitTimeout);
                    return DoBody();
                }
                finally
                {
                    if (locked)
                    {
                        @lock.Dispose();
                    }
                }
            }
        }

        /// <summary>
        /// LUCENENET specific class to simulate the anonymous creation of a With class in Java
        /// by using deletate methods.
        /// </summary>
        private class AnonymousWith<T> : With<T>
        {
            private readonly Func<T> doBody;
            public AnonymousWith(Lock @lock, int lockWaitTimeout, Func<T> doBody)
                : base(@lock, lockWaitTimeout)
            {
                this.doBody = doBody ?? throw new ArgumentNullException(nameof(doBody));
            }

            protected override T DoBody()
            {
                return doBody();
            }
        }
    }
}