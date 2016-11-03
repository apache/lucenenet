using System.Threading;

namespace Lucene.Net.Store
{
    using System;

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
    ///  shared across multiple IndexInputs or IndexOutputs (for example
    ///  those involved all merging).  Those IndexInputs and
    ///  IndexOutputs would call <seealso cref="#pause"/> whenever they
    ///  want to read bytes or write bytes.
    /// </summary>
    public abstract class RateLimiter
    {
        /// <summary>
        /// Sets an updated mb per second rate limit.
        /// </summary>
        public abstract double MbPerSec { set; get; }

        /// <summary>
        /// Pauses, if necessary, to keep the instantaneous IO
        ///  rate at or below the target.
        ///  <p>
        ///  Note: the implementation is thread-safe
        ///  </p> </summary>
        ///  <returns> the pause time in nano seconds
        ///  </returns>
        public abstract long Pause(long bytes);

        /// <summary>
        /// Simple class to rate limit IO.
        /// </summary>
        public class SimpleRateLimiter : RateLimiter
        {
            internal double mbPerSec;
            internal double NsPerByte;
            internal long LastNS;

            // TODO: we could also allow eg a sub class to dynamically
            // determine the allowed rate, eg if an app wants to
            // change the allowed rate over time or something

            /// <summary>
            /// mbPerSec is the MB/sec max IO rate </summary>
            public SimpleRateLimiter(double mbPerSec)
            {
                MbPerSec = mbPerSec;
            }

            /// <summary>
            /// Sets an updated mb per second rate limit.
            /// </summary>
            public override double MbPerSec
            {
                set
                {
                    this.mbPerSec = value;
                    if (value == 0)
                        NsPerByte = 0;
                    else
                        NsPerByte = 1000000000.0 / (1024 * 1024 * value);
                }
                get
                {
                    return this.mbPerSec;
                }
            }

            /// <summary>
            /// Pauses, if necessary, to keep the instantaneous IO
            ///  rate at or below the target. NOTE: multiple threads
            ///  may safely use this, however the implementation is
            ///  not perfectly thread safe but likely in practice this
            ///  is harmless (just means in some rare cases the rate
            ///  might exceed the target).  It's best to call this
            ///  with a biggish count, not one byte at a time. </summary>
            ///  <returns> the pause time in nano seconds
            ///  </returns>
            public override long Pause(long bytes)
            {
                if (bytes == 1)
                {
                    return 0;
                }

                // TODO: this is purely instantaneous rate; maybe we
                // should also offer decayed recent history one?
                var targetNS = LastNS = LastNS + ((long)(bytes * NsPerByte));
                long startNS;
                var curNS = startNS = DateTime.UtcNow.Ticks * 100 /* ns */;
                if (LastNS < curNS)
                {
                    LastNS = curNS;
                }

                // While loop because Thread.sleep doesn't always sleep
                // enough:
                while (true)
                {
                    var pauseNS = targetNS - curNS;
                    if (pauseNS > 0)
                    {
#if !NETSTANDARD
                        try
                        {
#endif
                            Thread.Sleep((int)(pauseNS / 1000000));
#if !NETSTANDARD
                        }
                        catch (ThreadInterruptedException ie)
                        {
                            throw new ThreadInterruptedException("Thread Interrupted Exception", ie);
                        }
#endif
                        curNS = DateTime.UtcNow.Ticks * 100;
                        continue;
                    }
                    break;
                }
                return curNS - startNS;
            }
        }
    }
}