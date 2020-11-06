using J2N.Threading;
using Lucene.Net.Support;
using System;
using System.IO;
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
    /// Utility class that runs a thread to manage periodic
    /// reopens of a <see cref="ReferenceManager{T}"/>, with methods to wait for a specific
    /// index changes to become visible.  To use this class you
    /// must first wrap your <see cref="Index.IndexWriter"/> with a
    /// <see cref="TrackingIndexWriter"/> and always use it to make changes
    /// to the index, saving the returned generation.  Then,
    /// when a given search request needs to see a specific
    /// index change, call the <see cref="WaitForGeneration(long)"/> to wait for
    /// that change to be visible.  Note that this will only
    /// scale well if most searches do not need to wait for a
    /// specific index generation.
    /// <para/>
    /// @lucene.experimental
    /// </summary>
    public class ControlledRealTimeReopenThread<T> : ThreadJob, IDisposable
         where T : class
    {
        private readonly ReferenceManager<T> manager;
        private readonly long targetMaxStaleNS;
        private readonly long targetMinStaleNS;
        private readonly TrackingIndexWriter writer;
        private volatile bool finish;
        private long waitingGen;
        private long searchingGen;
        private long refreshStartGen;

        private EventWaitHandle reopenCond = new AutoResetEvent(false);
        private EventWaitHandle available = new AutoResetEvent(false);

        /// <summary>
        /// Create <see cref="ControlledRealTimeReopenThread{T}"/>, to periodically
        /// reopen the a <see cref="ReferenceManager{T}"/>.
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
            if (targetMaxStaleSec < targetMinStaleSec)
            {
                throw new ArgumentException("targetMaxScaleSec (= " + targetMaxStaleSec.ToString("0.0") + ") < targetMinStaleSec (=" + targetMinStaleSec.ToString("0.0") + ")");
            }
            this.writer = writer;
            this.manager = manager;
            this.targetMaxStaleNS = (long)(1000000000 * targetMaxStaleSec);
            this.targetMinStaleNS = (long)(1000000000 * targetMinStaleSec);
            manager.AddListener(new HandleRefresh(this));
        }

        private class HandleRefresh : ReferenceManager.IRefreshListener
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
                // if we're finishing, , make it out so that all waiting search threads will return
                searchingGen = finish ? long.MaxValue : refreshStartGen;
                available.Set();
            }
            reopenCond.Reset();
        }

        /// <summary>
        /// Releases all resources used by the <see cref="ControlledRealTimeReopenThread{T}"/>.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases resources used by the <see cref="ControlledRealTimeReopenThread{T}"/> and
        /// if overridden in a derived class, optionally releases unmanaged resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources;
        /// <c>false</c> to release only unmanaged resources.</param>

        // LUCENENET specific - implemented proper dispose pattern
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                finish = true;
                reopenCond.Set();
                //#if FEATURE_THREAD_INTERRUPT
                //            try
                //            {
                //#endif
                Join();
                //#if FEATURE_THREAD_INTERRUPT // LUCENENET NOTE: Senseless to catch and rethrow the same exception type
                //            }
                //            catch (ThreadInterruptedException ie)
                //            {
                //                throw new ThreadInterruptedException(ie.ToString(), ie);
                //            }
                //#endif
                // LUCENENET specific: dispose reset event
                reopenCond.Dispose();
                available.Dispose();
            }
        }

        /// <summary>
        /// Waits for the target generation to become visible in
        /// the searcher.
        /// If the current searcher is older than the
        /// target generation, this method will block
        /// until the searcher is reopened, by another via
        /// <see cref="ReferenceManager{T}.MaybeRefresh()"/> or until the <see cref="ReferenceManager{T}"/> is closed.
        /// </summary>
        /// <param name="targetGen"> The generation to wait for </param>
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
        /// <see cref="ReferenceManager{T}.MaybeRefresh()"/>, the given waiting time has elapsed, or until
        /// the <see cref="ReferenceManager{T}"/> is closed.
        /// <para/>
        /// NOTE: if the waiting time elapses before the requested target generation is
        /// available the current <see cref="SearcherManager"/> is returned instead.
        /// </summary>
        /// <param name="targetGen">
        ///          The generation to wait for </param>
        /// <param name="maxMS">
        ///          Maximum milliseconds to wait, or -1 to wait indefinitely </param>
        /// <returns> <c>true</c> if the <paramref name="targetGen"/> is now available,
        ///         or false if <paramref name="maxMS"/> wait time was exceeded </returns>
        public virtual bool WaitForGeneration(long targetGen, int maxMS)
        {
            long curGen = writer.Generation;
            if (targetGen > curGen)
            {
                throw new ArgumentException("targetGen=" + targetGen + " was never returned by the ReferenceManager instance (current gen=" + curGen + ")");
            }
            lock (this)
                if (targetGen <= searchingGen)
                    return true;
                else
                {
                    waitingGen = Math.Max(waitingGen, targetGen);
                    reopenCond.Set();
                    available.Reset();
                }

            long startMS = Time.NanoTime() / 1000000;

            // LUCENENET specific - reading searchingGen not thread safe, so use Interlocked.Read()
            while (targetGen > Interlocked.Read(ref searchingGen))
            {
                if (maxMS < 0)
                {
                    available.WaitOne();
                }
                else
                {
                    long msLeft = (startMS + maxMS) - (Time.NanoTime()) / 1000000;
                    if (msLeft <= 0)
                    {
                        return false;
                    }
                    else
                    {
                        available.WaitOne(TimeSpan.FromMilliseconds(msLeft));
                    }
                }
            }

            return true;
        }

        public override void Run()
        {
            // TODO: maybe use private thread ticktock timer, in
            // case clock shift messes up nanoTime?
            long lastReopenStartNS = DateTime.UtcNow.Ticks * 100;

            //System.out.println("reopen: start");
            while (!finish)
            {
                bool hasWaiting;

                lock (this)
                    hasWaiting = waitingGen > searchingGen;

                long nextReopenStartNS = lastReopenStartNS + (hasWaiting ? targetMinStaleNS : targetMaxStaleNS);
                long sleepNS = nextReopenStartNS - Time.NanoTime();

                if (sleepNS > 0)
#if FEATURE_THREAD_INTERRUPT
                    try
                    {
#endif
                        reopenCond.WaitOne(TimeSpan.FromMilliseconds(sleepNS / Time.MILLISECONDS_PER_NANOSECOND));//Convert NS to Ticks
#if FEATURE_THREAD_INTERRUPT
                    }
#pragma warning disable 168
                    catch (ThreadInterruptedException ie)
#pragma warning restore 168
                    {
                        Thread.CurrentThread.Interrupt();
                        return;
                    }
#endif

                if (finish)
                {
                    break;
                }

                lastReopenStartNS = Time.NanoTime();
                // Save the gen as of when we started the reopen; the
                // listener (HandleRefresh above) copies this to
                // searchingGen once the reopen completes:
                refreshStartGen = writer.GetAndIncrementGeneration();
                try
                {
                    manager.MaybeRefreshBlocking();
                }
                catch (IOException ioe)
                {
                    throw new Exception(ioe.ToString(), ioe);
                }
            }
            // this will set the searchingGen so that all waiting threads will exit
            RefreshDone();
        }
    }
}