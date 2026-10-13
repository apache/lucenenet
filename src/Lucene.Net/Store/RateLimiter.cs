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
    /// <see cref="IndexOutput"/>s would call <see cref="Pause"/> whenever they have read
    /// or written more than <see cref="MinPauseCheckBytes"/> bytes.
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
        /// How many bytes the caller should add up itself before invoking <see cref="Pause"/>.
        /// </summary>
        public abstract long MinPauseCheckBytes { get; }

        /// <summary>
        /// Simple class to rate limit IO.
        /// </summary>
        public class SimpleRateLimiter : RateLimiter
        {
            private const int MIN_PAUSE_CHECK_MSEC = 5;

            // LUCENENET: mbPerSec/minPauseCheckBytes are volatile in Lucene; we use
            // AtomicDouble/AtomicInt64 for atomicity. lastNS is a plain long guarded by
            // UninterruptableMonitor in Pause() (matches upstream, which is non-volatile).
            private readonly AtomicDouble mbPerSec = new AtomicDouble();
            private readonly AtomicInt64 minPauseCheckBytes = new AtomicInt64();
            private long lastNS;

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
                minPauseCheckBytes.Value = (long) ((MIN_PAUSE_CHECK_MSEC / 1000.0) * mbPerSec * 1024 * 1024);
            }

            public override long MinPauseCheckBytes => minPauseCheckBytes;

            /// <summary>
            /// The current mb per second rate limit.
            /// </summary>
            public override double MbPerSec => this.mbPerSec;

            /// <summary>
            /// Pauses, if necessary, to keep the instantaneous IO
            /// rate at or below the target. Be sure to only call
            /// this method when <paramref name="bytes"/> &gt; <see cref="MinPauseCheckBytes"/>,
            /// otherwise it will pause way too long!
            /// </summary>
            /// <returns> the pause time in nanoseconds </returns>
            public override long Pause(long bytes)
            {
                long startNS = Time.NanoTime();

                double secondsToPause = (bytes/1024.0/1024.0) / mbPerSec;

                long targetNS;

                // Sync'd to read + write lastNS:
                UninterruptableMonitor.Enter(this);
                try
                {
                    // Time we should sleep until; this is purely instantaneous
                    // rate (just adds seconds onto the last time we had paused to);
                    // maybe we should also offer decayed recent history one?
                    targetNS = lastNS + (long) (1000000000 * secondsToPause);

                    if (startNS >= targetNS)
                    {
                        // OK, current time is already beyond the target sleep time,
                        // no pausing to do.

                        // Set to startNS, not targetNS, to enforce the instant rate, not
                        // the "averaged over all history" rate:
                        lastNS = startNS;
                        return 0;
                    }

                    lastNS = targetNS;
                }
                finally
                {
                    UninterruptableMonitor.Exit(this);
                }

                long curNS = startNS;

                // While loop because Thread.sleep doesn't always sleep
                // enough:
                while (true)
                {
                    var pauseNS = targetNS - curNS;
                    if (pauseNS > 0)
                    {
                        try
                        {
                            // LUCENENET NOTE: retaining original comment re: JVMs below
                            // NOTE: except maybe on real-time JVMs, minimum realistic sleep time
                            // is 1 msec; if you pass just 1 nsec the default impl rounds
                            // this up to 1 msec:
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
