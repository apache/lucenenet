using System.Diagnostics;

namespace Lucene.Net.Store
{
    using Lucene.Net.Support;
    using System.Collections.Generic;

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

    ///
    /// <summary>
    /// A <seealso cref="Directory"/> wrapper that allows <seealso cref="IndexOutput"/> rate limiting using
    /// <seealso cref="IOContext.Context IO context"/> specific <seealso cref="RateLimiter rate limiters"/>.
    /// </summary>
    ///  <seealso cref= #setRateLimiter(RateLimiter, IOContext.Context)
    /// @lucene.experimental </seealso>
    public sealed class RateLimitedDirectoryWrapper : FilterDirectory
    {
        // we need to be volatile here to make sure we see all the values that are set
        // / modified concurrently
        //private volatile RateLimiter[] ContextRateLimiters = new RateLimiter[Enum.GetValues(typeof(IOContext.Context_e)).Length];
        private IDictionary<IOContext.Context_e?, RateLimiter> ContextRateLimiters = new ConcurrentHashMap<IOContext.Context_e?, RateLimiter>();

        public RateLimitedDirectoryWrapper(Directory wrapped)
            : base(wrapped)
        {
        }

        public override IndexOutput CreateOutput(string name, IOContext context)
        {
            EnsureOpen();
            IndexOutput output = base.CreateOutput(name, context);
            RateLimiter limiter = GetRateLimiter(context.Context);
            if (limiter != null)
            {
                return new RateLimitedIndexOutput(limiter, output);
            }
            return output;
        }

        public override IndexInputSlicer CreateSlicer(string name, IOContext context)
        {
            EnsureOpen();
            return @in.CreateSlicer(name, context);
        }

        public override void Copy(Directory to, string src, string dest, IOContext context)
        {
            EnsureOpen();
            @in.Copy(to, src, dest, context);
        }

        private RateLimiter GetRateLimiter(IOContext.Context_e context)
        {
            Debug.Assert(context != null);
            return ContextRateLimiters[context];
        }

        /// <summary>
        /// Sets the maximum (approx) MB/sec allowed by all write IO performed by
        /// <seealso cref="IndexOutput"/> created with the given <seealso cref="IOContext.Context"/>. Pass
        /// <code>null</code> to have no limit.
        ///
        /// <p>
        /// <b>NOTE</b>: For already created <seealso cref="IndexOutput"/> instances there is no
        /// guarantee this new rate will apply to them; it will only be guaranteed to
        /// apply for new created <seealso cref="IndexOutput"/> instances.
        /// <p>
        /// <b>NOTE</b>: this is an optional operation and might not be respected by
        /// all Directory implementations. Currently only <seealso cref="FSDirectory buffered"/>
        /// Directory implementations use rate-limiting.
        /// </summary>
        /// <exception cref="IllegalArgumentException">
        ///           if context is <code>null</code> </exception>
        /// <exception cref="AlreadyClosedException"> if the <seealso cref="Directory"/> is already closed
        /// @lucene.experimental </exception>
        public void SetMaxWriteMBPerSec(double mbPerSec, IOContext.Context_e? context)
        {
            EnsureOpen();
            if (context == null)
            {
                throw new System.ArgumentException("Context must not be null");
            }
            //int ord = context.ordinal();
            RateLimiter limiter = ContextRateLimiters[context];
            if (mbPerSec == null)
            {
                if (limiter != null)
                {
                    limiter.MbPerSec = double.MaxValue;
                    ContextRateLimiters[context] = null;
                }
            }
            else if (limiter != null)
            {
                limiter.MbPerSec = mbPerSec;
                ContextRateLimiters[context] = limiter; // cross the mem barrier again
            }
            else
            {
                ContextRateLimiters[context] = new RateLimiter.SimpleRateLimiter(mbPerSec);
            }
        }

        /// <summary>
        /// Sets the rate limiter to be used to limit (approx) MB/sec allowed by all IO
        /// performed with the given <seealso cref="IOContext.Context context"/>. Pass <code>null</code> to
        /// have no limit.
        ///
        /// <p>
        /// Passing an instance of rate limiter compared to setting it using
        /// <seealso cref="#setMaxWriteMBPerSec(Double, IOContext.Context)"/>
        /// allows to use the same limiter instance across several directories globally
        /// limiting IO across them.
        /// </summary>
        /// <exception cref="IllegalArgumentException">
        ///           if context is <code>null</code> </exception>
        /// <exception cref="AlreadyClosedException"> if the <seealso cref="Directory"/> is already closed
        /// @lucene.experimental </exception>
        public void SetRateLimiter(RateLimiter mergeWriteRateLimiter, IOContext.Context_e context)
        {
            EnsureOpen();
            if (context == null)
            {
                throw new System.ArgumentException("Context must not be null");
            }
            ContextRateLimiters[context] = mergeWriteRateLimiter;
        }

        /// <summary>
        /// See <seealso cref="#setMaxWriteMBPerSec"/>.
        /// </summary>
        /// <exception cref="IllegalArgumentException">
        ///           if context is <code>null</code> </exception>
        /// <exception cref="AlreadyClosedException"> if the <seealso cref="Directory"/> is already closed
        /// @lucene.experimental </exception>
        public double GetMaxWriteMBPerSec(IOContext.Context_e context)
        {
            EnsureOpen();
            if (context == null)
            {
                throw new System.ArgumentException("Context must not be null");
            }
            RateLimiter limiter = GetRateLimiter(context);
            return limiter == null ? 0 : limiter.MbPerSec;
        }
    }
}