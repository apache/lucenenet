using J2N;
using J2N.Threading;
using J2N.Threading.Atomic;
using Lucene.Net.Support.Threading;
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
        // LUCENENET: java final converted readonly
        private readonly ReferenceManager<T> manager;
        private readonly long targetMaxStaleNS;
        private readonly long targetMinStaleNS;
        private readonly TrackingIndexWriter writer;
        private volatile bool finish;
        private long waitingGen;
        private long searchingGen;
        private readonly AtomicInt64 refreshStartGen = new AtomicInt64();
        private readonly AtomicBoolean isDisposed = new AtomicBoolean(false);

        protected readonly EventWaitHandle m_notify = new ManualResetEvent(false);  // LUCENENET specific: used to mimic intrinsic monitor used by java wait and notifyAll keywords.
        private readonly EventWaitHandle reopenCond = new AutoResetEvent(false);    // LUCENENET NOTE: unlike java, in c# we don't need to lock reopenCond when calling methods on it.


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
            this.targetMaxStaleNS = (long)(Time.SecondsPerNanosecond * targetMaxStaleSec);
            this.targetMinStaleNS = (long)(Time.SecondsPerNanosecond * targetMinStaleSec);
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
            UninterruptableMonitor.Enter(this);
            try
            {
				// if we're finishing, make it out so that all waiting search threads will return
                searchingGen = finish ? long.MaxValue : refreshStartGen;
                m_notify.Set();                        // LUCENENET NOTE:  Will notify all and remain signaled, so it must be reset in WaitForGeneration
            }
            finally
            {
                UninterruptableMonitor.Exit(this);
            }
        }

        /// <summary>
        /// Kills the thread and releases all resources used by the
        /// <see cref="ControlledRealTimeReopenThread{T}"/>. Also joins to the
        /// thread so that when this method returns the thread is no longer alive.
        /// </summary>
        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Kills the thread and releases all resources used by the
        /// <see cref="ControlledRealTimeReopenThread{T}"/>. Also joins to the
        /// thread so that when this method returns the thread is no longer alive.
        /// </summary>
        // LUCENENET specific - Support for Dispose(bool) since this is a non-sealed class.
        protected virtual void Dispose(bool disposing)
        {
            // LUCENENET: Prevent double-dispose of our managed resources.
            if (isDisposed.GetAndSet(true))
            {
                return;
            }

            if (disposing)
            {
                finish = true;

                // So thread wakes up and notices it should finish:
                reopenCond.Set();

                try
                {
                    Join();
                }
                catch (Exception ie) when (ie.IsInterruptedException())
                {
                    throw new Util.ThreadInterruptedException(ie);
                }
                finally
                {
                    RefreshDone();

                    // LUCENENET specific: dispose reset events
                    reopenCond.Dispose();
                    m_notify.Dispose();
                }
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
            // LUCENENET NOTE: Porting this method is a bit tricky because the java wait method releases the
            //                 syncronize lock and c# has no similar primitive.  So we must handle locking a
            //                 bit differently here to mimic that affect.

            long curGen = writer.Generation;
            if (targetGen > curGen)
            {
                throw new ArgumentException("targetGen=" + targetGen + " was never returned by the ReferenceManager instance (current gen=" + curGen + ")");
            }

            UninterruptableMonitor.Enter(this);
            try
            {
                if (targetGen <= searchingGen)
                {
                    return true;
                }

                // Need to find waitingGen inside lock as its used to determine
                // stale time
                waitingGen = Math.Max(waitingGen, targetGen);
                reopenCond.Set();                                   // LUCENENET NOTE: gives Run() an oppertunity to notice one is now waiting if one wasn't before.
                m_notify.Reset();                                   // LUCENENET specific: required to "close the door". Java's notifyAll keyword didn't need this.
            }
            finally
            {
                UninterruptableMonitor.Exit(this);
            }

            long startMS = Time.NanoTime() / Time.MillisecondsPerNanosecond;
            while (targetGen > Interlocked.Read(ref searchingGen))      // LUCENENET specific - reading searchingGen not thread safe, so use Interlocked.Read()
            {
                if (maxMS < 0)
                {
                    m_notify.WaitOne();
                }
                else
                {
                    long msLeft = (startMS + maxMS) - (Time.NanoTime()) / Time.MillisecondsPerNanosecond;
                    if (msLeft <= 0)
                    {
                        return false;
                    }
                    else
                    {
                        m_notify.WaitOne(TimeSpan.FromMilliseconds(msLeft));
                    }
                }
            }

            return true;
        }

        public override void Run()
        {
            // TODO: maybe use private thread ticktock timer, in
            // case clock shift messes up nanoTime?
            // LUCENENET NOTE: Time.NanoTime() is not affected by clock changes.
            long lastReopenStartNS = Time.NanoTime();

            //System.out.println("reopen: start");
            while (!finish)
            {

                // TODO: try to guestimate how long reopen might
                // take based on past data?

                // Loop until we've waiting long enough before the
                // next reopen:
                while (!finish)
                {

                    try
                    {
                        // Need lock before finding out if has waiting
                        bool hasWaiting;
                		UninterruptableMonitor.Enter(this);
                		try
                		{
                            // True if we have someone waiting for reopened searcher:
                            hasWaiting = waitingGen > searchingGen;
                		}
                		finally
                		{
                    		UninterruptableMonitor.Exit(this);
                		}

                        long nextReopenStartNS = lastReopenStartNS + (hasWaiting ? targetMinStaleNS : targetMaxStaleNS);
                        long sleepNS = nextReopenStartNS - Time.NanoTime();

                        if (sleepNS > 0)
                        {
                            reopenCond.WaitOne(TimeSpan.FromMilliseconds(sleepNS / Time.MillisecondsPerNanosecond));//Convert NS to MS
                        }
                        else
                        {
                            break;
                        }
                    }
                    catch (Exception ie) when (ie.IsInterruptedException())
                    {
                        Thread.CurrentThread.Interrupt();
                        return;
                    }

                }
                if (finish)
                {
                    break;
                }

                lastReopenStartNS = Time.NanoTime();
                // Save the gen as of when we started the reopen; the
                // listener (HandleRefresh above) copies this to
                // searchingGen once the reopen completes:
                refreshStartGen.Value = writer.GetAndIncrementGeneration();
                try
                {
                    manager.MaybeRefreshBlocking();
                }
                catch (Exception ioe) when (ioe.IsIOException())
                {
                    throw RuntimeException.Create(ioe);
                }
            }
        }
    }
}