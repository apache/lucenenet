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
    /// A <seealso cref="LockFactory"/> that wraps another {@link
    /// LockFactory} and verifies that each lock obtain/release
    /// is "correct" (never results in two processes holding the
    /// lock at the same time).  It does this by contacting an
    /// external server (<seealso cref="LockVerifyServer"/>) to assert that
    /// at most one process holds the lock at a time.  To use
    /// this, you should also run <seealso cref="LockVerifyServer"/> on the
    /// host & port matching what you pass to the constructor.
    /// </summary>
    /// <seealso> cref= LockVerifyServer </seealso>
    /// <seealso> cref= LockStressTest </seealso>

    public class VerifyingLockFactory : LockFactory
    {
        internal readonly LockFactory lf;
        internal readonly Stream @in;
        internal readonly Stream @out;

        private class CheckedLock : Lock
        {
            private readonly VerifyingLockFactory outerInstance;

            private readonly Lock @lock;

            public CheckedLock(VerifyingLockFactory outerInstance, Lock @lock)
            {
                this.outerInstance = outerInstance;
                this.@lock = @lock;
            }

            private void Verify(sbyte message) // LUCENENET TODO: sbyte unnecessary here
            {
                outerInstance.@out.WriteByte((byte)message);
                outerInstance.@out.Flush();
                int ret = outerInstance.@in.ReadByte();
                if (ret < 0)
                {
                    throw new InvalidOperationException("Lock server died because of locking error.");
                }
                if (ret != message)
                {
                    throw new System.IO.IOException("Protocol violation.");
                }
            }

            public override bool Obtain()
            {
                lock (this)
                {
                    bool obtained = @lock.Obtain();
                    if (obtained)
                    {
                        Verify((sbyte)1);
                    }
                    return obtained;
                }
            }

            public override bool IsLocked
            {
                get
                {
                    lock (this)
                    {
                        return @lock.IsLocked;
                    }
                }
            }

            public override void Release()
            {
                lock (this)
                {
                    if (IsLocked)
                    {
                        Verify((sbyte)0);
                        @lock.Release();
                    }
                }
            }
        }

        /// <param name="lf"> the LockFactory that we are testing </param>
        /// <param name="in"> the socket's input to <seealso cref="LockVerifyServer"/> </param>
        /// <param name="out"> the socket's output to <seealso cref="LockVerifyServer"/> </param>
        public VerifyingLockFactory(LockFactory lf, Stream @in, Stream @out)
        {
            this.lf = lf;
            this.@in = @in;
            this.@out = @out;
        }

        public override Lock MakeLock(string lockName)
        {
            lock (this)
            {
                return new CheckedLock(this, lf.MakeLock(lockName));
            }
        }

        public override void ClearLock(string lockName)
        {
            lock (this)
            {
                lf.ClearLock(lockName);
            }
        }
    }
}