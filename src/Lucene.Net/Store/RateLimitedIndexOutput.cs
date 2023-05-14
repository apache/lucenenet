using System;
using System.Runtime.CompilerServices;
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
    /// A rate limiting (<see cref="RateLimiter"/>) <see cref="IndexOutput"/>
    /// <para/>
    /// @lucene.internal
    /// </summary>
    internal sealed class RateLimitedIndexOutput : BufferedIndexOutput
    {
        private readonly IndexOutput @delegate;
        private readonly BufferedIndexOutput bufferedDelegate;
        private readonly RateLimiter rateLimiter;
        private int disposed = 0; // LUCENENET specific - allow double-dispose

        internal RateLimitedIndexOutput(RateLimiter rateLimiter, IndexOutput @delegate)
        {
            // TODO should we make buffer size configurable
            if (@delegate is BufferedIndexOutput bufferedIndexOutput)
            {
                bufferedDelegate = bufferedIndexOutput;
                this.@delegate = @delegate;
            }
            else
            {
                this.@delegate = @delegate;
                bufferedDelegate = null;
            }
            this.rateLimiter = rateLimiter;
        }

        protected internal override void FlushBuffer(byte[] b, int offset, int len)
        {
            rateLimiter.Pause(len);
            if (bufferedDelegate != null)
            {
                bufferedDelegate.FlushBuffer(b, offset, len);
            }
            else
            {
                @delegate.WriteBytes(b, offset, len);
            }
        }

        public override long Length
        {
            get
            {
                EnsureOpen(); // LUCENENET specific - ensure we can't be abused after dispose
                return @delegate.Length;
            }
            set
            {
                // LUCENENET: Intentionally blank
            }
        }

        [Obsolete("(4.1) this method will be removed in Lucene 5.0")]
        public override void Seek(long pos)
        {
            EnsureOpen(); // LUCENENET specific - ensure we can't be abused after dispose
            Flush();
            @delegate.Seek(pos);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public override void Flush()
        {
            try
            {
                base.Flush();
            }
            finally
            {
                @delegate.Flush();
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (0 != Interlocked.CompareExchange(ref this.disposed, 1, 0)) return; // LUCENENET specific - allow double-dispose

            if (disposing)
            {
                try
                {
                    base.Dispose(disposing);
                }
                finally
                {
                    @delegate.Dispose();
                }
            }
        }

        // LUCENENET specific - ensure we can't be abused after dispose
        private bool IsOpen => Interlocked.CompareExchange(ref this.disposed, 0, 0) == 0 ? true : false;

        // LUCENENET specific - ensure we can't be abused after dispose
        private void EnsureOpen()
        {
            if (!IsOpen)
            {
                throw AlreadyClosedException.Create(this.GetType().FullName, "this IndexOutput is disposed.");
            }
        }
    }
}