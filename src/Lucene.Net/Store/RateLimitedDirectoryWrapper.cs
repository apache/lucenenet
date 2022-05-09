using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;

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

    ///
    /// <summary>
    /// A <see cref="Directory"/> wrapper that allows <see cref="IndexOutput"/> rate limiting using
    /// IO context (<see cref="IOContext.UsageContext"/>) specific rate limiters (<see cref="RateLimiter"/>).
    /// <para/>
    /// @lucene.experimental
    /// </summary>
    /// <seealso cref="SetRateLimiter(RateLimiter, IOContext.UsageContext)"/>
    public sealed class RateLimitedDirectoryWrapper : FilterDirectory
    {
        // we need to be volatile here to make sure we see all the values that are set
        // / modified concurrently
        private readonly IDictionary<IOContext.UsageContext, RateLimiter> _contextRateLimiters = new ConcurrentDictionary<IOContext.UsageContext, RateLimiter>();

        public RateLimitedDirectoryWrapper(Directory wrapped)
            : base(wrapped)
        {
        }

        public override IndexOutput CreateOutput(string name, IOContext context)
        {
            EnsureOpen();
            var output = base.CreateOutput(name, context);
            var limiter = GetRateLimiter(context.Context);
            if (limiter != null)
            {
                return new RateLimitedIndexOutput(limiter, output);
            }
            return output;
        }

        public override IndexInputSlicer CreateSlicer(string name, IOContext context)
        {
            EnsureOpen();
            return m_input.CreateSlicer(name, context);
        }

        public override void Copy(Directory to, string src, string dest, IOContext context)
        {
            EnsureOpen();
            m_input.Copy(to, src, dest, context);
        }

        private RateLimiter GetRateLimiter(IOContext.UsageContext context)
        {
            //if (Debugging.AssertsEnabled) Debugging.Assert(context != null); // LUCENENET NOTE: In .NET, enum can never be null
            return _contextRateLimiters.TryGetValue(context, out RateLimiter ret) ? ret : null;
        }

        /// <summary>
        /// Sets the maximum (approx) MB/sec allowed by all write IO performed by
        /// <see cref="IndexOutput"/> created with the given <see cref="IOContext.UsageContext"/>. Pass 
        /// <c>null</c> for <paramref name="mbPerSec"/> to have no limit.
        ///
        /// <para/>
        /// <b>NOTE</b>: For already created <see cref="IndexOutput"/> instances there is no
        /// guarantee this new rate will apply to them; it will only be guaranteed to
        /// apply for new created <see cref="IndexOutput"/> instances.
        /// <para/>
        /// <b>NOTE</b>: this is an optional operation and might not be respected by
        /// all <see cref="Directory"/> implementations. Currently only buffered (<see cref="FSDirectory"/>)
        /// <see cref="Directory"/> implementations use rate-limiting.
        /// <para/>
        /// @lucene.experimental
        /// </summary>
        /// <exception cref="ObjectDisposedException"> if the <see cref="Directory"/> is already disposed
        /// </exception>
        public void SetMaxWriteMBPerSec(double? mbPerSec, IOContext.UsageContext context)
        {
            EnsureOpen();
            //if (context is null) // LUCENENET NOTE: enum values can never be null in .NET
            //{
            //    throw new ArgumentException("Context must not be null");
            //}
            //int ord = context.ordinal();
            _contextRateLimiters.TryGetValue(context, out RateLimiter limiter);

            if (mbPerSec is null)
            {
                if (limiter != null)
                {
                    limiter.SetMbPerSec(double.MaxValue);
                    _contextRateLimiters[context] = null;
                }
            }
            else if (limiter != null)
            {
                limiter.SetMbPerSec(mbPerSec.Value);
                _contextRateLimiters[context] = limiter; // cross the mem barrier again
            }
            else
            {
                _contextRateLimiters[context] = new RateLimiter.SimpleRateLimiter(mbPerSec.Value);
            }
        }

        /// <summary>
        /// Sets the rate limiter to be used to limit (approx) MB/sec allowed by all IO
        /// performed with the given context (<see cref="IOContext.UsageContext"/>). Pass <c>null</c> to
        /// have no limit.
        ///
        /// <para/>
        /// Passing an instance of rate limiter compared to setting it using
        /// <see cref="SetMaxWriteMBPerSec(double?, IOContext.UsageContext)"/>
        /// allows to use the same limiter instance across several directories globally
        /// limiting IO across them.
        /// <para/>
        /// @lucene.experimental
        /// </summary>
        /// <exception cref="ObjectDisposedException"> if the <see cref="Directory"/> is already disposed
        /// </exception>
        public void SetRateLimiter(RateLimiter mergeWriteRateLimiter, IOContext.UsageContext context)
        {
            EnsureOpen();
            _contextRateLimiters[context] = mergeWriteRateLimiter;
        }

        /// <summary>
        /// See <see cref="SetMaxWriteMBPerSec"/>.
        /// <para/>
        /// @lucene.experimental
        /// </summary>
        /// <exception cref="ObjectDisposedException"> if the <see cref="Directory"/> is already disposed
        /// </exception>
        public double GetMaxWriteMBPerSec(IOContext.UsageContext context)
        {
            EnsureOpen();
            var limiter = GetRateLimiter(context);
            return limiter is null ? 0 : limiter.MbPerSec;
        }
    }
}