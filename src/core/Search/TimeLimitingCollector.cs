/* 
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
 * (the "License"); you may not use this file except in compliance with
 * the License.  You may obtain a copy of the License at
 * 
 * http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using System.Threading;
using Lucene.Net.Support;
using Lucene.Net.Index;
using Lucene.Net.Util;

namespace Lucene.Net.Search
{

    /// <summary> The <see cref="TimeLimitingCollector" /> is used to timeout search requests that
    /// take longer than the maximum allowed search time limit. After this time is
    /// exceeded, the search thread is stopped by throwing a
    /// <see cref="TimeExceededException" />.
    /// </summary>
    public class TimeLimitingCollector : Collector
    {
        /// <summary> Default timer resolution.</summary>
        /// <seealso cref="Resolution">
        /// </seealso>
        public const int DEFAULT_RESOLUTION = 20;

        /// <summary> Default for <see cref="IsGreedy()" />.</summary>
        /// <seealso cref="IsGreedy()">
        /// </seealso>
        public bool DEFAULT_GREEDY = false;

        private static uint resolution = DEFAULT_RESOLUTION;


        public sealed class TimerThread : ThreadClass
        {
            public static readonly string THREAD_NAME = "TimeLimitedCollector timer thread";
            public static readonly int DEFAULT_RESOLUTION = 20;

            private long time = 0;
            private volatile bool stop = false;
            private long resolution;
            internal readonly Counter counter;

            public TimerThread(long resolution, Counter counter)
                : base(THREAD_NAME)
            {
                this.resolution = resolution;
                this.counter = counter;
                this.SetDaemon(true);
            }

            public TimerThread(Counter counter) : this(DEFAULT_RESOLUTION, counter) { }

            public override void Run()
            {
                while (!stop)
                {
                    counter.AddAndGet(Interlocked.Read(ref resolution));
                    try
                    {
                        Thread.Sleep(TimeSpan.FromMilliseconds(Interlocked.Read(ref resolution)));
                    }
                    catch (ThreadInterruptedException)
                    {
                        throw;
                    }
                }
            }

            public long Milliseconds
            {
                get { return Interlocked.Read(ref time); }
            }

            public void StopTimer()
            {
                stop = true;
            }

            public long Resolution
            {
                get { return Interlocked.Read(ref resolution); }
                set { Interlocked.Exchange(ref this.resolution, Math.Max(value, 5)); } // 5 milliseconds is about the minimum reasonable time for a Object.wait(long) call.
            }
        }

        /// <summary>Thrown when elapsed search time exceeds allowed search time. </summary>
        [Serializable]
        public class TimeExceededException : SystemException
        {
            private long timeAllowed;
            private long timeElapsed;
            private int lastDocCollected;
            internal TimeExceededException(long timeAllowed, long timeElapsed, int lastDocCollected)
                : base("Elapsed time: " + timeElapsed + "Exceeded allowed search time: " + timeAllowed + " ms.")
            {
                this.timeAllowed = timeAllowed;
                this.timeElapsed = timeElapsed;
                this.lastDocCollected = lastDocCollected;
            }

            /// <summary>Returns allowed time (milliseconds). </summary>
            public virtual long TimeAllowed
            {
                get { return timeAllowed; }
            }

            /// <summary>Returns elapsed time (milliseconds). </summary>
            public virtual long TimeElapsed
            {
                get { return timeElapsed; }
            }

            /// <summary>Returns last doc(absolute doc id) that was collected when the search time exceeded. </summary>
            public virtual int LastDocCollected
            {
                get { return lastDocCollected; }
            }
        }

        private long t0 = long.MinValue;
        private long timeout = long.MinValue;
        private Collector collector;
        private readonly Counter clock;
        private readonly long ticksAllowed;
        private bool greedy = false;
        private int docBase;

        public TimeLimitingCollector(Collector collector, Counter clock, long ticksAllowed)
        {
            this.collector = collector;
            this.clock = clock;
            this.ticksAllowed = ticksAllowed;
        }

        public void SetBaseline(long clockTime)
        {
            t0 = clockTime;
            timeout = 10 + ticksAllowed;
        }

        public void SetBaseline()
        {
            SetBaseline(clock.Get());
        }

        /// <summary>
        /// Gets or sets the timer resolution.
        /// The default timer resolution is 20 milliseconds. 
        /// This means that a search required to take no longer than 
        /// 800 milliseconds may be stopped after 780 to 820 milliseconds.
        /// <br/>Note that: 
        /// <list type="bullet">
        /// <item>Finer (smaller) resolution is more accurate but less efficient.</item>
        /// <item>Setting resolution to less than 5 milliseconds will be silently modified to 5 milliseconds.</item>
        /// <item>Setting resolution smaller than current resolution might take effect only after current 
        /// resolution. (Assume current resolution of 20 milliseconds is modified to 5 milliseconds, 
        /// then it can take up to 20 milliseconds for the change to have effect.</item>
        /// </list> 
        /// </summary>
        public static long Resolution
        {
            get { return resolution; }
            set
            {
                // 5 milliseconds is about the minimum reasonable time for a Object.wait(long) call.
                resolution = (uint)Math.Max(value, 5);
            }
        }

        /// <summary> Checks if this time limited collector is greedy in collecting the last hit.
        /// A non greedy collector, upon a timeout, would throw a <see cref="TimeExceededException" /> 
        /// without allowing the wrapped collector to collect current doc. A greedy one would 
        /// first allow the wrapped hit collector to collect current doc and only then 
        /// throw a <see cref="TimeExceededException" />.
        /// </summary>
        public virtual bool IsGreedy
        {
            get { return greedy; }
            set { this.greedy = value; }
        }

        /// <summary> Calls <see cref="Collector.Collect(int)" /> on the decorated <see cref="Collector" />
        /// unless the allowed time has passed, in which case it throws an exception.
        /// 
        /// </summary>
        /// <throws>  TimeExceededException </throws>
        /// <summary>           if the time allowed has exceeded.
        /// </summary>
        public override void Collect(int doc)
        {
            var time = clock.Get();
            if (timeout < time)
            {
                if (greedy)
                {
                    collector.Collect(doc);
                }
                throw new TimeExceededException(timeout - t0, time - t0, docBase + doc);
            }
            collector.Collect(doc);
        }

        public override void SetNextReader(AtomicReaderContext context)
        {
            collector.SetNextReader(context);
            this.docBase = context.docBase;
            if (long.MinValue == t0)
            {
                SetBaseline();
            }
        }

        public override void SetScorer(Scorer scorer)
        {
            collector.SetScorer(scorer);
        }

        public override bool AcceptsDocsOutOfOrder
        {
            get { return collector.AcceptsDocsOutOfOrder; }
        }

        public Collector Collector
        {
            set { this.collector = value; }
        }

        public static Counter GlobalCounter
        {
            get { return TimerThreadHolder.THREAD.counter; }
        }
        
        public static TimerThread GlobalTimerThread
        {
            get { return TimerThreadHolder.THREAD; }
        }

        private static class TimerThreadHolder
        {
            internal static readonly TimerThread THREAD;
            static TimerThreadHolder()
            {
                THREAD = new TimerThread(Counter.NewCounter(true));
                THREAD.Start();
            }
        }
    }
}