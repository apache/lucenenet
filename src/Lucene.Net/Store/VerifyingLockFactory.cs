using Lucene.Net.Support.Threading;
using System;
using System.IO;

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
    /// A <see cref="LockFactory"/> that wraps another 
    /// <see cref="LockFactory"/> and verifies that each lock obtain/release
    /// is "correct" (never results in two processes holding the
    /// lock at the same time).  It does this by contacting an
    /// external server (<see cref="LockVerifyServer"/>) to assert that
    /// at most one process holds the lock at a time.  To use
    /// this, you should also run <see cref="LockVerifyServer"/> on the
    /// host &amp; port matching what you pass to the constructor.
    /// </summary>
    /// <seealso cref="LockVerifyServer"/>
    /// <seealso cref="LockStressTest"/>
    public class VerifyingLockFactory : LockFactory
    {
        internal readonly LockFactory lf;
        internal readonly Stream stream;

        private class CheckedLock : Lock
        {
            private readonly VerifyingLockFactory outerInstance;

            private readonly Lock @lock;

            public CheckedLock(VerifyingLockFactory outerInstance, Lock @lock)
            {
                this.outerInstance = outerInstance;
                this.@lock = @lock;
            }

            private void Verify(byte message)
            {
                outerInstance.stream.WriteByte(message);
                outerInstance.stream.Flush();
                int ret = outerInstance.stream.ReadByte();
                if (ret < 0)
                {
                    throw IllegalStateException.Create("Lock server died because of locking error.");
                }
                if (ret != message)
                {
                    throw new IOException("Protocol violation.");
                }
            }

            public override bool Obtain()
            {
                UninterruptableMonitor.Enter(this);
                try
                {
                    bool obtained = @lock.Obtain();
                    if (obtained)
                    {
                        Verify((byte)1);
                    }
                    return obtained;
                }
                finally
                {
                    UninterruptableMonitor.Exit(this);
                }
            }

            public override bool IsLocked()
            {
                UninterruptableMonitor.Enter(this);
                try
                {
                    return @lock.IsLocked();
                }
                finally
                {
                    UninterruptableMonitor.Exit(this);
                }
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    UninterruptableMonitor.Enter(this);
                    try
                    {
                        if (IsLocked())
                        {
                            Verify((byte)0);
                            @lock.Dispose();
                        }
                    }
                    finally
                    {
                        UninterruptableMonitor.Exit(this);
                    }
                }
            }
        }

        /// <summary>
        /// Creates a new <see cref="VerifyingLockFactory"/> instance.
        /// </summary>
        /// <param name="lf"> the <see cref="LockFactory"/> that we are testing </param>
        /// <param name="stream"> the socket's stream input/output to <see cref="LockVerifyServer"/> </param>
        public VerifyingLockFactory(LockFactory lf, Stream stream)
        {
            this.lf = lf;
            this.stream = stream;
        }

        public override Lock MakeLock(string lockName)
        {
            UninterruptableMonitor.Enter(this);
            try
            {
                return new CheckedLock(this, lf.MakeLock(lockName));
            }
            finally
            {
                UninterruptableMonitor.Exit(this);
            }
        }

        public override void ClearLock(string lockName)
        {
            UninterruptableMonitor.Enter(this);
            try
            {
                lf.ClearLock(lockName);
            }
            finally
            {
                UninterruptableMonitor.Exit(this);
            }
        }
    }
}