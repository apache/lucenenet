using J2N;
using J2N.Threading.Atomic;
using Lucene.Net.Support.Threading;
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
    /// Abstract base class to rate limit IO.  Typically implementations are
    /// shared across multiple <see cref="IndexInput"/>s or <see cref="IndexOutput"/>s (for example
    /// those involved all merging).  Those <see cref="IndexInput"/>s and
    /// <see cref="IndexOutput"/>s would call <see cref="Pause"/> whenever they
    /// want to read bytes or write bytes.
    /// </summary>
    public abstract class RateLimiter
    {
        /// <summary>
        /// Sets an updated mb per second rate limit.
        /// </summary>
        public abstract void SetMbPerSec(double mbPerSec);

        /// <summary>
        /// The current mb per second rate limit.
        /// </summary>
        public abstract double MbPerSec { get; }

        /// <summary>
        /// Pauses, if necessary, to keep the instantaneous IO
        /// rate at or below the target.
        /// <para>
        /// Note: the implementation is thread-safe
        /// </para>
        /// </summary>
        /// <returns> the pause time in nano seconds </returns>
        public abstract long Pause(long bytes);

        /// <summary>
        /// Simple class to rate limit IO.
        /// </summary>
        public class SimpleRateLimiter : RateLimiter
        {
            // LUCENENET: these fields are volatile in Lucene, but that is not
            // valid in .NET. Instead, we use AtomicInt64/AtomicDouble to ensure atomicity.
            private readonly AtomicDouble mbPerSec = new AtomicDouble();
            private readonly AtomicDouble nsPerByte = new AtomicDouble();
            private readonly AtomicInt64 lastNS = new AtomicInt64();

            // TODO: we could also allow eg a sub class to dynamically
            // determine the allowed rate, eg if an app wants to
            // change the allowed rate over time or something

            /// <summary>
            /// <paramref name="mbPerSec"/> is the MB/sec max IO rate </summary>
            public SimpleRateLimiter(double mbPerSec)
            {
                SetMbPerSec(mbPerSec);
            }

            /// <summary>
            /// Sets an updated mb per second rate limit.
            /// </summary>
            public override void SetMbPerSec(double mbPerSec)
            {
                this.mbPerSec.Value = mbPerSec;
                if (mbPerSec == 0)
                    nsPerByte.Value = 0;
                else
                    nsPerByte.Value = 1000000000.0 / (1024 * 1024 * mbPerSec);
            }

            /// <summary>
            /// The current mb per second rate limit.
            /// </summary>
            public override double MbPerSec => this.mbPerSec;

            /// <summary>
            /// Pauses, if necessary, to keep the instantaneous IO
            /// rate at or below the target. NOTE: multiple threads
            /// may safely use this, however the implementation is
            /// not perfectly thread safe but likely in practice this
            /// is harmless (just means in some rare cases the rate
            /// might exceed the target).  It's best to call this
            /// with a biggish count, not one byte at a time. </summary>
            /// <returns> the pause time in nano seconds </returns>
            public override long Pause(long bytes)
            {
                if (bytes == 1)
                {
                    return 0;
                }

                // TODO: this is purely instantaneous rate; maybe we
                // should also offer decayed recent history one?
                var targetNS = /*lastNS =*/ lastNS.AddAndGet((long)(bytes * nsPerByte));
                long startNS;
                var curNS = startNS = Time.NanoTime() /* ns */;
                if (lastNS < curNS)
                {
                    lastNS.Value = curNS;
                }

                // While loop because Thread.sleep doesn't always sleep
                // enough:
                while (true)
                {
                    var pauseNS = targetNS - curNS;
                    if (pauseNS > 0)
                    {
                        try
                        {
                            Thread.Sleep(TimeSpan.FromMilliseconds(pauseNS / 1000000));
                        }
                        catch (Exception ie) when (ie.IsInterruptedException())
                        {
                            throw new Util.ThreadInterruptedException(ie);
                        }

                        curNS = Time.NanoTime();
                        continue;
                    }
                    break;
                }
                return curNS - startNS;
            }
        }
    }
}
