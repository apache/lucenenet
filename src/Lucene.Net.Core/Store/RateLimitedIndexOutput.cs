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
    /// A <seealso cref="rateLimiter rate limiting"/> <seealso cref="IndexOutput"/>
    ///
    /// @lucene.internal
    /// </summary>
    internal sealed class RateLimitedIndexOutput : BufferedIndexOutput
    {
        private readonly IndexOutput @delegate;
        private readonly BufferedIndexOutput bufferedDelegate;
        private readonly RateLimiter rateLimiter;

        internal RateLimitedIndexOutput(RateLimiter rateLimiter, IndexOutput @delegate)
        {
            // TODO should we make buffer size configurable
            if (@delegate is BufferedIndexOutput)
            {
                bufferedDelegate = (BufferedIndexOutput)@delegate;
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
                return @delegate.Length;
            }
            set
            {
            }
        }

        public override void Seek(long pos)
        {
            Flush();
            @delegate.Seek(pos);
        }

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

        public override void Dispose()
        {
            try
            {
                base.Dispose();
            }
            finally
            {
                @delegate.Dispose();
            }
        }
    }
}