using Lucene.Net.Support;
using System;
using System.Threading;

namespace Lucene.Net.Search
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

    using TrackingIndexWriter = Lucene.Net.Index.TrackingIndexWriter;

    /// <summary>
    /// Utility class that runs a thread to manage periodicc
    ///  reopens of a <seealso cref="ReferenceManager"/>, with methods to wait for a specific
    ///  index changes to become visible.  To use this class you
    ///  must first wrap your <seealso cref="Index.IndexWriter"/> with a {@link
    ///  TrackingIndexWriter} and always use it to make changes
    ///  to the index, saving the returned generation.  Then,
    ///  when a given search request needs to see a specific
    ///  index change, call the {#waitForGeneration} to wait for
    ///  that change to be visible.  Note that this will only
    ///  scale well if most searches do not need to wait for a
    ///  specific index generation.
    ///
    /// @lucene.experimental
    /// </summary>

    public class ControlledRealTimeReopenThread<T> : ThreadClass, IDisposable
        where T : class
    {
        /*private void InitializeInstanceFields()
        {
            ReopenCond = ReopenLock.NewCondition();
        }*/

        private readonly ReferenceManager<T> manager;
        private readonly long targetMaxStaleNS;
        private readonly long targetMinStaleNS;
        private readonly TrackingIndexWriter writer;
        private volatile bool finish;
        private long waitingGen;
        private long searchingGen;
        private long refreshStartGen;

        private readonly ReentrantLock reopenLock = new ReentrantLock();
        private ManualResetEvent reopenCond = new ManualResetEvent(false);

        /// <summary>
        /// Create ControlledRealTimeReopenThread, to periodically
        /// reopen the a <seealso cref="ReferenceManager"/>.
        /// </summary>
        /// <param name="targetMaxStaleSec"> Maximum time until a new
        ///        reader must be opened; this sets the upper bound
        ///        on how slowly reopens may occur, when no
        ///        caller is waiting for a specific generation to
        ///        become visible.
        /// </param>
        /// <param name="targetMinStaleSec"> Mininum time until a new
        ///        reader can be opened; this sets the lower bound
        ///        on how quickly reopens may occur, when a caller
        ///        is waiting for a specific generation to
        ///        become visible. </param>
        public ControlledRealTimeReopenThread(TrackingIndexWriter writer, ReferenceManager<T> manager, double targetMaxStaleSec, double targetMinStaleSec)
        {
            //InitializeInstanceFields();
            if (targetMaxStaleSec < targetMinStaleSec)
            {
                throw new System.ArgumentException("targetMaxScaleSec (= " + targetMaxStaleSec + ") < targetMinStaleSec (=" + targetMinStaleSec + ")");
            }
            this.writer = writer;
            this.manager = manager;
            this.targetMaxStaleNS = (long)(1000000000 * targetMaxStaleSec);
            this.targetMinStaleNS = (long)(1000000000 * targetMinStaleSec);
            manager.AddListener(new HandleRefresh(this));
        }

        private class HandleRefresh : ReferenceManager.RefreshListener
        {
            private readonly ControlledRealTimeReopenThread<T> outerInstance;

            public HandleRefresh(ControlledRealTimeReopenThread<T> outerInstance)
            {
                this.outerInstance = outerInstance;
            }

            public virtual void BeforeRefresh()
            {
            }

            public virtual void AfterRefresh(bool didRefresh)
            {
                outerInstance.RefreshDone();
            }
        }

        private void RefreshDone()
        {
            lock (this)
            {
                searchingGen = refreshStartGen;
                Monitor.PulseAll(this);
            }
        }

        public void Dispose() // LUCENENET TODO: Implement disposable pattern
        {
            lock (this)
            {
                //System.out.println("NRT: set finish");

                finish = true;

                // So thread wakes up and notices it should finish:
                reopenLock.Lock();
                try
                {
                    reopenCond.Set();
                }
                finally
                {
                    reopenLock.Unlock();
                }

#if !NETSTANDARD
                try
                {
#endif
                    Join();
#if !NETSTANDARD
                }
                catch (ThreadInterruptedException ie)
                {
                    throw new ThreadInterruptedException("Thread Interrupted Exception", ie);
                }
#endif
                // Max it out so any waiting search threads will return:
                searchingGen = long.MaxValue;
                Monitor.PulseAll(this);
            }
        }

        /// <summary>
        /// Waits for the target generation to become visible in
        /// the searcher.
        /// If the current searcher is older than the
        /// target generation, this method will block
        /// until the searcher is reopened, by another via
        /// <seealso cref="ReferenceManager#maybeRefresh"/> or until the <seealso cref="ReferenceManager"/> is closed.
        /// </summary>
        /// <param name="targetGen"> the generation to wait for </param>
        public virtual void WaitForGeneration(long targetGen)
        {
            WaitForGeneration(targetGen, -1);
        }

        /// <summary>
        /// Waits for the target generation to become visible in
        /// the searcher, up to a maximum specified milli-seconds.
        /// If the current searcher is older than the target
        /// generation, this method will block until the
        /// searcher has been reopened by another thread via
        /// <seealso cref="ReferenceManager#maybeRefresh"/>, the given waiting time has elapsed, or until
        /// the <seealso cref="ReferenceManager"/> is closed.
        /// <p>
        /// NOTE: if the waiting time elapses before the requested target generation is
        /// available the current <seealso cref="SearcherManager"/> is returned instead.
        /// </summary>
        /// <param name="targetGen">
        ///          the generation to wait for </param>
        /// <param name="maxMS">
        ///          maximum milliseconds to wait, or -1 to wait indefinitely </param>
        /// <returns> true if the targetGeneration is now available,
        ///         or false if maxMS wait time was exceeded </returns>
        public virtual bool WaitForGeneration(long targetGen, int maxMS)
        {
            lock (this)
            {
                long curGen = writer.Generation;
                if (targetGen > curGen)
                {
                    throw new System.ArgumentException("targetGen=" + targetGen + " was never returned by the ReferenceManager instance (current gen=" + curGen + ")");
                }
                if (targetGen > searchingGen)
                {
                    // Notify the reopen thread that the waitingGen has
                    // changed, so it may wake up and realize it should
                    // not sleep for much or any longer before reopening:
                    reopenLock.Lock();

                    // Need to find waitingGen inside lock as its used to determine
                    // stale time
                    waitingGen = Math.Max(waitingGen, targetGen);

                    try
                    {
                        reopenCond.Set();
                    }
                    finally
                    {
                        reopenLock.Unlock();
                    }

                    long startMS = Environment.TickCount;//System.nanoTime() / 1000000;

                    while (targetGen > searchingGen)
                    {
                        if (maxMS < 0)
                        {
                            Monitor.Wait(this);
                        }
                        else
                        {
                            long msLeft = (startMS + maxMS) - Environment.TickCount;//(System.nanoTime()) / 1000000;
                            if (msLeft <= 0)
                            {
                                return false;
                            }
                            else
                            {
                                Monitor.Wait(this, TimeSpan.FromMilliseconds(msLeft));
                            }
                        }
                    }
                }

                return true;
            }
        }

        public override void Run()
        {
            // TODO: maybe use private thread ticktock timer, in
            // case clock shift messes up nanoTime?
            long lastReopenStartNS = DateTime.Now.Ticks * 100;

            //System.out.println("reopen: start");
            while (!finish)
            {
                // TODO: try to guestimate how long reopen might
                // take based on past data?

                // Loop until we've waiting long enough before the
                // next reopen:
                while (!finish)
                {
                    // Need lock before finding out if has waiting

                    reopenLock.Lock();

                    try
                    {
                        // True if we have someone waiting for reopened searcher:
                        bool hasWaiting = waitingGen > searchingGen;
                        long nextReopenStartNS = lastReopenStartNS + (hasWaiting ? targetMinStaleNS : targetMaxStaleNS);

                        long sleepNS = nextReopenStartNS - (DateTime.Now.Ticks * 100);

                        if (sleepNS > 0)
                        {
                            reopenCond.WaitOne(new TimeSpan(sleepNS / 100));//Convert NS to Ticks
                        }
                        else
                        {
                            break;
                        }

                    }
#if !NETSTANDARD
                    catch (ThreadInterruptedException ie)
                    {
                        Thread.CurrentThread.Interrupt();
                        return;
                    }
#endif
                    finally
                    {
                        reopenLock.Unlock();
                    }
                }

                if (finish)
                {
                    break;
                }

                lastReopenStartNS = DateTime.Now.Ticks * 100;
                // Save the gen as of when we started the reopen; the
                // listener (HandleRefresh above) copies this to
                // searchingGen once the reopen completes:
                refreshStartGen = writer.GetAndIncrementGeneration();
                try
                {
                    manager.MaybeRefreshBlocking();
                }
                catch (System.IO.IOException ioe)
                {
                    throw new Exception(ioe.Message, ioe);
                }
            }
        }
    }
}