using J2N.Threading;
using Lucene.Net.Diagnostics;
using Lucene.Net.Support.Threading;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Security;
using System.Text;
using System.Threading;
using JCG = J2N.Collections.Generic;

namespace Lucene.Net.Index
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

    using CollectionUtil = Lucene.Net.Util.CollectionUtil;
    using Directory = Lucene.Net.Store.Directory;

    /// <summary>
    /// A <see cref="MergeScheduler"/> that runs each merge using a
    /// separate thread.
    ///
    /// <para>Specify the max number of threads that may run at
    /// once, and the maximum number of simultaneous merges
    /// with <see cref="SetMaxMergesAndThreads"/>.</para>
    ///
    /// <para>If the number of merges exceeds the max number of threads
    /// then the largest merges are paused until one of the smaller
    /// merges completes.</para>
    ///
    /// <para>If more than <see cref="MaxMergeCount"/> merges are
    /// requested then this class will forcefully throttle the
    /// incoming threads by pausing until one more more merges
    /// complete.</para>
    /// </summary>
    public class ConcurrentMergeScheduler : MergeScheduler, IConcurrentMergeScheduler
    {
        private int mergeThreadPriority = -1;

        /// <summary>
        /// List of currently active <see cref="MergeThread"/>s. </summary>
        protected internal IList<MergeThread> m_mergeThreads = new JCG.List<MergeThread>();

        /// <summary>
        /// Default <see cref="MaxThreadCount"/>.
        /// We default to 1: tests on spinning-magnet drives showed slower
        /// indexing performance if more than one merge thread runs at
        /// once (though on an SSD it was faster)
        /// </summary>
        public const int DEFAULT_MAX_THREAD_COUNT = 1;

        /// <summary>
        /// Default <see cref="MaxMergeCount"/>. </summary>
        public const int DEFAULT_MAX_MERGE_COUNT = 2;

        // Max number of merge threads allowed to be running at
        // once.  When there are more merges then this, we
        // forcefully pause the larger ones, letting the smaller
        // ones run, up until maxMergeCount merges at which point
        // we forcefully pause incoming threads (that presumably
        // are the ones causing so much merging).
        private int maxThreadCount = DEFAULT_MAX_THREAD_COUNT;

        // Max number of merges we accept before forcefully
        // throttling the incoming threads
        private int maxMergeCount = DEFAULT_MAX_MERGE_COUNT;

        /// <summary>
        /// <see cref="Directory"/> that holds the index. </summary>
        protected internal Directory m_dir;

        /// <summary>
        /// <see cref="IndexWriter"/> that owns this instance. </summary>
        protected internal IndexWriter m_writer;

        /// <summary>
        /// How many <see cref="MergeThread"/>s have kicked off (this is use
        /// to name them).
        /// </summary>
        protected internal int m_mergeThreadCount;

        /// <summary>
        /// Sole constructor, with all settings set to default
        /// values.
        /// </summary>
        public ConcurrentMergeScheduler()
        {
        }

        /// <summary>
        /// Sets the maximum number of merge threads and simultaneous merges allowed.
        /// </summary>
        /// <param name="maxMergeCount"> the max # simultaneous merges that are allowed.
        ///       If a merge is necessary yet we already have this many
        ///       threads running, the incoming thread (that is calling
        ///       add/updateDocument) will block until a merge thread
        ///       has completed.  Note that we will only run the
        ///       smallest <paramref name="maxThreadCount"/> merges at a time. </param>
        /// <param name="maxThreadCount"> The max # simultaneous merge threads that should
        ///       be running at once.  This must be &lt;= <paramref name="maxMergeCount"/> </param>
        public virtual void SetMaxMergesAndThreads(int maxMergeCount, int maxThreadCount)
        {
            if (maxThreadCount < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(maxThreadCount), "maxThreadCount should be at least 1"); // LUCENENET specific - changed from IllegalArgumentException to ArgumentOutOfRangeException (.NET convention)
            }
            if (maxMergeCount < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(maxMergeCount), "maxMergeCount should be at least 1"); // LUCENENET specific - changed from IllegalArgumentException to ArgumentOutOfRangeException (.NET convention)
            }
            if (maxThreadCount > maxMergeCount)
            {
                throw new ArgumentException("maxThreadCount should be <= maxMergeCount (= " + maxMergeCount + ")");
            }
            this.maxThreadCount = maxThreadCount;
            this.maxMergeCount = maxMergeCount;
        }

        /// <summary>
        /// Returns <see cref="maxThreadCount"/>.
        /// </summary>
        /// <seealso cref="SetMaxMergesAndThreads(int, int)"/>
        public virtual int MaxThreadCount => maxThreadCount;

        /// <summary>
        /// See <see cref="SetMaxMergesAndThreads(int, int)"/>. </summary>
        public virtual int MaxMergeCount => maxMergeCount;

        /// <summary>
        /// Return the priority that merge threads run at.  By
        /// default the priority is 1 plus the priority of (ie,
        /// slightly higher priority than) the first thread that
        /// calls merge.
        /// </summary>
        public virtual int MergeThreadPriority
        {
            get
            {
                UninterruptableMonitor.Enter(this);
                try
                {
                    InitMergeThreadPriority();
                    return mergeThreadPriority;
                }
                finally
                {
                    UninterruptableMonitor.Exit(this);
                }
            }
        }

        /// <summary>
        /// Set the base priority that merge threads run at.
        /// Note that CMS may increase priority of some merge
        /// threads beyond this base priority.  It's best not to
        /// set this any higher than
        /// <see cref="ThreadPriority.Highest"/>(4)-maxThreadCount, so that CMS has
        /// room to set relative priority among threads.
        /// </summary>
        public virtual void SetMergeThreadPriority(int priority)
        {
            UninterruptableMonitor.Enter(this);
            try
            {
                if (priority > (int)ThreadPriority.Highest || priority < (int)ThreadPriority.Lowest)
                {
                    throw new ArgumentOutOfRangeException(nameof(priority), "priority must be in range " + (int)ThreadPriority.Lowest + " .. " + (int)ThreadPriority.Highest + " inclusive"); // LUCENENET specific - changed from IllegalArgumentException to ArgumentOutOfRangeException (.NET convention)
                }
                mergeThreadPriority = priority;
                UpdateMergeThreads();
            }
            finally
            {
                UninterruptableMonitor.Exit(this);
            }
        }

        /// <summary>
        /// Sorts <see cref="MergeThread"/>s; larger merges come first. </summary>
        protected internal static readonly IComparer<MergeThread> compareByMergeDocCount = Comparer<MergeThread>.Create((t1, t2) =>
            {
                MergePolicy.OneMerge m1 = t1.CurrentMerge;
                MergePolicy.OneMerge m2 = t2.CurrentMerge;

                int c1 = m1 is null ? int.MaxValue : m1.TotalDocCount;
                int c2 = m2 is null ? int.MaxValue : m2.TotalDocCount;

                return c2 - c1;
            });

        /// <summary>
        /// Called whenever the running merges have changed, to pause &amp; unpause
        /// threads. This method sorts the merge threads by their merge size in
        /// descending order and then pauses/unpauses threads from first to last --
        /// that way, smaller merges are guaranteed to run before larger ones.
        /// </summary>
        protected virtual void UpdateMergeThreads()
        {
            UninterruptableMonitor.Enter(this);
            try
            {
                // Only look at threads that are alive & not in the
                // process of stopping (ie have an active merge):
                IList<MergeThread> activeMerges = new JCG.List<MergeThread>();

                int threadIdx = 0;
                while (threadIdx < m_mergeThreads.Count)
                {
                    MergeThread mergeThread = m_mergeThreads[threadIdx];
                    if (!mergeThread.IsAlive)
                    {
                        // Prune any dead threads
                        m_mergeThreads.RemoveAt(threadIdx);
                        continue;
                    }
                    if (mergeThread.CurrentMerge != null)
                    {
                        activeMerges.Add(mergeThread);
                    }
                    threadIdx++;
                }

                // Sort the merge threads in descending order.
                CollectionUtil.TimSort(activeMerges, compareByMergeDocCount);

                int pri = mergeThreadPriority;
                int activeMergeCount = activeMerges.Count;
                for (threadIdx = 0; threadIdx < activeMergeCount; threadIdx++)
                {
                    MergeThread mergeThread = activeMerges[threadIdx];
                    MergePolicy.OneMerge merge = mergeThread.CurrentMerge;
                    if (merge is null)
                    {
                        continue;
                    }

                    // pause the thread if maxThreadCount is smaller than the number of merge threads.
                    bool doPause = threadIdx < activeMergeCount - maxThreadCount;

                    if (IsVerbose)
                    {
                        if (doPause != merge.IsPaused)
                        {
                            if (doPause)
                            {
                                Message("pause thread " + mergeThread.Name);
                            }
                            else
                            {
                                Message("unpause thread " + mergeThread.Name);
                            }
                        }
                    }
                    if (doPause != merge.IsPaused)
                    {
                        merge.SetPause(doPause);
                    }

                    if (!doPause)
                    {
                        if (IsVerbose)
                        {
                            Message("set priority of merge thread " + mergeThread.Name + " to " + pri);
                        }
                        mergeThread.SetThreadPriority((ThreadPriority)pri);
                        pri = Math.Min((int)ThreadPriority.Highest, 1 + pri);
                    }
                }
            }
            finally
            {
                UninterruptableMonitor.Exit(this);
            }
        }

        /// <summary>
        /// Returns <c>true</c> if verbosing is enabled. This method is usually used in
        /// conjunction with <see cref="Message(String)"/>, like that:
        ///
        /// <code>
        /// if (IsVerbose) 
        /// {
        ///     Message(&quot;your message&quot;);
        /// }
        /// </code>
        /// </summary>
        protected virtual bool IsVerbose => m_writer != null && m_writer.infoStream.IsEnabled("CMS");

        /// <summary>
        /// Outputs the given message - this method assumes <see cref="IsVerbose"/> was
        /// called and returned <c>true</c>.
        /// </summary>
        protected internal virtual void Message(string message)
        {
            m_writer.infoStream.Message("CMS", message);
        }

        private void InitMergeThreadPriority()
        {
            UninterruptableMonitor.Enter(this);
            try
            {
                if (mergeThreadPriority == -1)
                {
                    // Default to slightly higher priority than our
                    // calling thread
                    mergeThreadPriority = 1 + (int)ThreadJob.CurrentThread.Priority;
                    if (mergeThreadPriority > (int)ThreadPriority.Highest)
                    {
                        mergeThreadPriority = (int)ThreadPriority.Highest;
                    }
                }
            }
            finally
            {
                UninterruptableMonitor.Exit(this);
            }
        }

        protected override void Dispose(bool disposing)
        {
            Sync();
        }

        /// <summary>
        /// Wait for any running merge threads to finish. This call is not interruptible as used by <see cref="Dispose(bool)"/>. </summary>
        public virtual void Sync()
        {
            bool interrupted = false;
            try
            {
                while (true)
                {
                    MergeThread toSync = null;
                    UninterruptableMonitor.Enter(this);
                    try
                    {
                        foreach (MergeThread t in m_mergeThreads)
                        {
                            if (t != null && t.IsAlive)
                            {
                                toSync = t;
                                break;
                            }
                        }
                    }
                    finally
                    {
                        UninterruptableMonitor.Exit(this);
                    }
                    if (toSync != null)
                    {
                        try
                        {
                            toSync.Join();
                        }
                        catch (Exception ie) when (ie.IsInterruptedException())
                        {
                            // ignore this Exception, we will retry until all threads are dead
                            interrupted = true;
                        }
                    }
                    else
                    {
                        break;
                    }
                }
            }
            finally
            {
                // finally, restore interrupt status:
                if (interrupted)
                {
                    Thread.CurrentThread.Interrupt();
                }
            }
        }

        /// <summary>
        /// Returns the number of merge threads that are alive. Note that this number
        /// is &lt;= <see cref="m_mergeThreads"/> size.
        /// </summary>
        protected virtual int MergeThreadCount
        {
            get
            {
                UninterruptableMonitor.Enter(this);
                try
                {
                    int count = 0;
                    foreach (MergeThread mt in m_mergeThreads)
                    {
                        if (mt.IsAlive && mt.CurrentMerge != null)
                        {
                            count++;
                        }
                    }
                    return count;
                }
                finally
                {
                    UninterruptableMonitor.Exit(this);
                }
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public override void Merge(IndexWriter writer, MergeTrigger trigger, bool newMergesFound)
        {
            UninterruptableMonitor.Enter(this);
            try
            {
                if (Debugging.AssertsEnabled) Debugging.Assert(!UninterruptableMonitor.IsEntered(writer));

                this.m_writer = writer;

                InitMergeThreadPriority();

                m_dir = writer.Directory;

                // First, quickly run through the newly proposed merges
                // and add any orthogonal merges (ie a merge not
                // involving segments already pending to be merged) to
                // the queue.  If we are way behind on merging, many of
                // these newly proposed merges will likely already be
                // registered.

                if (IsVerbose)
                {
                    Message("now merge");
                    Message("  index: " + writer.SegString());
                }

                // Iterate, pulling from the IndexWriter's queue of
                // pending merges, until it's empty:
                while (true)
                {
                    long startStallTime = 0;
                    while (writer.HasPendingMerges() && MergeThreadCount >= maxMergeCount)
                    {
                        // this means merging has fallen too far behind: we
                        // have already created maxMergeCount threads, and
                        // now there's at least one more merge pending.
                        // Note that only maxThreadCount of
                        // those created merge threads will actually be
                        // running; the rest will be paused (see
                        // updateMergeThreads).  We stall this producer
                        // thread to prevent creation of new segments,
                        // until merging has caught up:
                        startStallTime = J2N.Time.NanoTime() / J2N.Time.MillisecondsPerNanosecond; // LUCENENET: Use NanoTime() rather than CurrentTimeMilliseconds() for more accurate/reliable results
                        if (IsVerbose)
                        {
                            Message("    too many merges; stalling...");
                        }
                        try
                        {
                            UninterruptableMonitor.Wait(this);
                        }
                        catch (Exception ie) when (ie.IsInterruptedException())
                        {
                            throw new Util.ThreadInterruptedException(ie);
                        }
                    }

                    if (IsVerbose)
                    {
                        if (startStallTime != 0)
                        {
                            Message("  stalled for " + ((J2N.Time.NanoTime() / J2N.Time.MillisecondsPerNanosecond) - startStallTime) + " msec"); // LUCENENET: Use NanoTime() rather than CurrentTimeMilliseconds() for more accurate/reliable results
                        }
                    }

                    MergePolicy.OneMerge merge = writer.NextMerge();
                    if (merge is null)
                    {
                        if (IsVerbose)
                        {
                            Message("  no more merges pending; now return");
                        }
                        return;
                    }

                    bool success = false;
                    try
                    {
                        if (IsVerbose)
                        {
                            Message("  consider merge " + writer.SegString(merge.Segments));
                        }

                        // OK to spawn a new merge thread to handle this
                        // merge:
                        MergeThread merger = GetMergeThread(writer, merge);
                        m_mergeThreads.Add(merger);
                        if (IsVerbose)
                        {
                            Message("    launch new thread [" + merger.Name + "]");
                        }

                        merger.Start();

                        // Must call this after starting the thread else
                        // the new thread is removed from mergeThreads
                        // (since it's not alive yet):
                        UpdateMergeThreads();

                        success = true;
                    }
                    finally
                    {
                        if (!success)
                        {
                            writer.MergeFinish(merge);
                        }
                    }
                }
            }
            finally
            {
                UninterruptableMonitor.Exit(this);
            }
        }

        /// <summary>
        /// Does the actual merge, by calling <see cref="IndexWriter.Merge(MergePolicy.OneMerge)"/> </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        protected virtual void DoMerge(MergePolicy.OneMerge merge)
        {
            m_writer.Merge(merge);
        }

        /// <summary>
        /// Create and return a new <see cref="MergeThread"/> </summary>
        protected virtual MergeThread GetMergeThread(IndexWriter writer, MergePolicy.OneMerge merge)
        {
            UninterruptableMonitor.Enter(this);
            try
            {
                MergeThread thread = new MergeThread(this, writer, merge);
                thread.SetThreadPriority((ThreadPriority)mergeThreadPriority);
                thread.IsBackground = true;
                thread.Name = "Lucene Merge Thread #" + m_mergeThreadCount++;
                return thread;
            }
            finally
            {
                UninterruptableMonitor.Exit(this);
            }
        }

        /// <summary>
        /// Runs a merge thread, which may run one or more merges
        /// in sequence.
        /// </summary>
        protected internal class MergeThread : ThreadJob
        {
            private readonly ConcurrentMergeScheduler outerInstance;

            internal IndexWriter tWriter;
            internal MergePolicy.OneMerge startMerge;
            internal MergePolicy.OneMerge runningMerge;
            private volatile bool done;

            /// <summary>
            /// Sole constructor. </summary>
            public MergeThread(ConcurrentMergeScheduler outerInstance, IndexWriter writer, MergePolicy.OneMerge startMerge)
            {
                this.outerInstance = outerInstance;
                this.tWriter = writer;
                this.startMerge = startMerge;
            }

            /// <summary>
            /// Record the currently running merge. </summary>
            public virtual MergePolicy.OneMerge RunningMerge
            {
                set
                {
                    UninterruptableMonitor.Enter(this);
                    try
                    {
                        runningMerge = value;
                    }
                    finally
                    {
                        UninterruptableMonitor.Exit(this);
                    }
                }
                get
                {
                    UninterruptableMonitor.Enter(this);
                    try
                    {
                        return runningMerge;
                    }
                    finally
                    {
                        UninterruptableMonitor.Exit(this);
                    }
                }
            }

            /// <summary>
            /// Return the current merge, or <c>null</c> if this 
            /// <see cref="MergeThread"/> is done.
            /// </summary>
            public virtual MergePolicy.OneMerge CurrentMerge
            {
                get
                {
                    UninterruptableMonitor.Enter(this);
                    try
                    {
                        if (done)
                        {
                            return null;
                        }
                        else if (runningMerge != null)
                        {
                            return runningMerge;
                        }
                        else
                        {
                            return startMerge;
                        }
                    }
                    finally
                    {
                        UninterruptableMonitor.Exit(this);
                    }
                }
            }

            /// <summary>
            /// Set the priority of this thread. </summary>
            public virtual void SetThreadPriority(ThreadPriority priority)
            {
                // LUCENENET: We don't have to worry about JRE bugs here, and
                // SecurityException is not thrown from Thread.Priority. The exceptions
                // it throws (ArgumentException and ThreadStateException) are both valid
                // cases and are extremely unlikely (we never abort, and users would have to cast
                // an invalid int to a ThreadPriority).
                //try
                //{
                    Priority = priority;
                //}
                //catch (NullReferenceException npe)
                //{
                //    // Strangely, Sun's JDK 1.5 on Linux sometimes
                //    // throws NPE out of here...
                //}
                //catch (SecurityException se)
                //{
                //    // Ignore this because we will still run fine with
                //    // normal thread priority
                //}
            }

            public override void Run()
            {
                // First time through the while loop we do the merge
                // that we were started with:
                MergePolicy.OneMerge merge = this.startMerge;

                try
                {
                    if (outerInstance.IsVerbose)
                    {
                        outerInstance.Message("  merge thread: start");
                    }

                    while (true)
                    {
                        RunningMerge = merge;
                        outerInstance.DoMerge(merge);

                        // Subsequent times through the loop we do any new
                        // merge that writer says is necessary:
                        merge = tWriter.NextMerge();

                        // Notify here in case any threads were stalled;
                        // they will notice that the pending merge has
                        // been pulled and possibly resume:
                        UninterruptableMonitor.Enter(outerInstance);
                        try
                        {
                            UninterruptableMonitor.PulseAll(outerInstance);
                        }
                        finally
                        {
                            UninterruptableMonitor.Exit(outerInstance);
                        }

                        if (merge != null)
                        {
                            outerInstance.UpdateMergeThreads();
                            if (outerInstance.IsVerbose)
                            {
                                outerInstance.Message("  merge thread: do another merge " + tWriter.SegString(merge.Segments));
                            }
                        }
                        else
                        {
                            break;
                        }
                    }

                    if (outerInstance.IsVerbose)
                    {
                        outerInstance.Message("  merge thread: done");
                    }
                }
                catch (Exception exc) when (exc.IsThrowable())
                {
                    // Ignore the exception if it was due to abort:
                    if (!(exc is MergePolicy.MergeAbortedException))
                    {
                        //System.out.println(Thread.currentThread().getName() + ": CMS: exc");
                        //exc.printStackTrace(System.out);
                        if (!outerInstance.suppressExceptions)
                        {
                            // suppressExceptions is normally only set during
                            // testing.
                            outerInstance.HandleMergeException(exc);
                        }
                    }
                }
                finally
                {
                    done = true;
                    UninterruptableMonitor.Enter(outerInstance);
                    try
                    {
                        outerInstance.UpdateMergeThreads();
                        UninterruptableMonitor.PulseAll(outerInstance);
                    }
                    finally
                    {
                        UninterruptableMonitor.Exit(outerInstance);
                    }
                }
            }
        }

        /// <summary>
        /// Called when an exception is hit in a background merge
        /// thread
        /// </summary>
        protected virtual void HandleMergeException(Exception exc)
        {
            try
            {
                // When an exception is hit during merge, IndexWriter
                // removes any partial files and then allows another
                // merge to run.  If whatever caused the error is not
                // transient then the exception will keep happening,
                // so, we sleep here to avoid saturating CPU in such
                // cases:
                Thread.Sleep(250);
            }
            catch (Exception ie) when (ie.IsInterruptedException())
            {
                throw new Util.ThreadInterruptedException(ie);
            }
            throw new MergePolicy.MergeException(exc, m_dir);
        }

        private bool suppressExceptions;

        /// <summary>
        /// Used for testing </summary>
        public virtual void SetSuppressExceptions()
        {
            suppressExceptions = true;
        }

        /// <summary>
        /// Used for testing </summary>
        public virtual void ClearSuppressExceptions()
        {
            suppressExceptions = false;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder(this.GetType().Name + ": ");
            sb.Append("maxThreadCount=").Append(maxThreadCount).Append(", ");
            sb.Append("maxMergeCount=").Append(maxMergeCount).Append(", ");
            sb.Append("mergeThreadPriority=").Append(mergeThreadPriority);
            return sb.ToString();
        }

        public override object Clone()
        {
            ConcurrentMergeScheduler clone = (ConcurrentMergeScheduler)base.Clone();
            clone.m_writer = null;
            clone.m_dir = null;
            clone.m_mergeThreads = new JCG.List<MergeThread>();
            return clone;
        }
    }
}