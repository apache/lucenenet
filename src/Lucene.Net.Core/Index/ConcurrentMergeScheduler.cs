using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Lucene.Net.Index
{
    using Lucene.Net.Support;
    using CollectionUtil = Lucene.Net.Util.CollectionUtil;

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

    using Directory = Lucene.Net.Store.Directory;

    /// <summary>
    /// A <seealso cref="MergeScheduler"/> that runs each merge using a
    ///  separate thread.
    ///
    ///  <p>Specify the max number of threads that may run at
    ///  once, and the maximum number of simultaneous merges
    ///  with <seealso cref="#setMaxMergesAndThreads"/>.</p>
    ///
    ///  <p>If the number of merges exceeds the max number of threads
    ///  then the largest merges are paused until one of the smaller
    ///  merges completes.</p>
    ///
    ///  <p>If more than <seealso cref="#getMaxMergeCount"/> merges are
    ///  requested then this class will forcefully throttle the
    ///  incoming threads by pausing until one more more merges
    ///  complete.</p>
    /// </summary>
    public class ConcurrentMergeScheduler : MergeScheduler, IConcurrentMergeScheduler
    {
        private int MergeThreadPriority_Renamed = -1;

        /// <summary>
        /// List of currently active <seealso cref="MergeThread"/>s. </summary>
        protected internal IList<MergeThread> MergeThreads = new List<MergeThread>();

        /// <summary>
        /// Default {@code maxThreadCount}.
        /// We default to 1: tests on spinning-magnet drives showed slower
        /// indexing performance if more than one merge thread runs at
        /// once (though on an SSD it was faster)
        /// </summary>
        public const int DEFAULT_MAX_THREAD_COUNT = 1;

        /// <summary>
        /// Default {@code maxMergeCount}. </summary>
        public const int DEFAULT_MAX_MERGE_COUNT = 2;

        // Max number of merge threads allowed to be running at
        // once.  When there are more merges then this, we
        // forcefully pause the larger ones, letting the smaller
        // ones run, up until maxMergeCount merges at which point
        // we forcefully pause incoming threads (that presumably
        // are the ones causing so much merging).
        private int MaxThreadCount_Renamed = DEFAULT_MAX_THREAD_COUNT;

        // Max number of merges we accept before forcefully
        // throttling the incoming threads
        private int MaxMergeCount_Renamed = DEFAULT_MAX_MERGE_COUNT;

        /// <summary>
        /// <seealso cref="Directory"/> that holds the index. </summary>
        protected internal Directory Dir;

        /// <summary>
        /// <seealso cref="IndexWriter"/> that owns this instance. </summary>
        protected internal IndexWriter Writer;

        /// <summary>
        /// How many <seealso cref="MergeThread"/>s have kicked off (this is use
        ///  to name them).
        /// </summary>
        protected internal int MergeThreadCount_Renamed;

        /// <summary>
        /// Sole constructor, with all settings set to default
        ///  values.
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
        ///       smallest <code>maxThreadCount</code> merges at a time. </param>
        /// <param name="maxThreadCount"> the max # simultaneous merge threads that should
        ///       be running at once.  this must be &lt;= <code>maxMergeCount</code> </param>
        public virtual void SetMaxMergesAndThreads(int maxMergeCount, int maxThreadCount)
        {
            if (maxThreadCount < 1)
            {
                throw new System.ArgumentException("maxThreadCount should be at least 1");
            }
            if (maxMergeCount < 1)
            {
                throw new System.ArgumentException("maxMergeCount should be at least 1");
            }
            if (maxThreadCount > maxMergeCount)
            {
                throw new System.ArgumentException("maxThreadCount should be <= maxMergeCount (= " + maxMergeCount + ")");
            }
            this.MaxThreadCount_Renamed = maxThreadCount;
            this.MaxMergeCount_Renamed = maxMergeCount;
        }

        /// <summary>
        /// Returns {@code maxThreadCount}.
        /// </summary>
        /// <seealso cref= #setMaxMergesAndThreads(int, int)  </seealso>
        public virtual int MaxThreadCount
        {
            get
            {
                return MaxThreadCount_Renamed;
            }
        }

        /// <summary>
        /// See <seealso cref="#setMaxMergesAndThreads"/>. </summary>
        public virtual int MaxMergeCount
        {
            get
            {
                return MaxMergeCount_Renamed;
            }
        }

        /// <summary>
        /// Return the priority that merge threads run at.  By
        ///  default the priority is 1 plus the priority of (ie,
        ///  slightly higher priority than) the first thread that
        ///  calls merge.
        /// </summary>
        public virtual int MergeThreadPriority
        {
            get
            {
                lock (this)
                {
                    InitMergeThreadPriority();
                    return MergeThreadPriority_Renamed;
                }
            }
            set
            {
                lock (this)
                {
                    if (value > (int)ThreadPriority.Highest || value < (int)ThreadPriority.Lowest)
                    {
                        throw new System.ArgumentException("priority must be in range " + (int)ThreadPriority.Highest + " .. " + (int)ThreadPriority.Lowest + " inclusive");
                    }
                    MergeThreadPriority_Renamed = value;
                    UpdateMergeThreads();
                }
            }
        }

        /// <summary>
        /// Sorts <seealso cref="MergeThread"/>s; larger merges come first. </summary>
        protected internal static readonly IComparer<MergeThread> compareByMergeDocCount = new ComparatorAnonymousInnerClassHelper();

        private class ComparatorAnonymousInnerClassHelper : IComparer<MergeThread>
        {
            public ComparatorAnonymousInnerClassHelper()
            {
            }

            public virtual int Compare(MergeThread t1, MergeThread t2)
            {
                MergePolicy.OneMerge m1 = t1.CurrentMerge;
                MergePolicy.OneMerge m2 = t2.CurrentMerge;

                int c1 = m1 == null ? int.MaxValue : m1.TotalDocCount;
                int c2 = m2 == null ? int.MaxValue : m2.TotalDocCount;

                return c2 - c1;
            }
        }

        /// <summary>
        /// Called whenever the running merges have changed, to pause & unpause
        /// threads. this method sorts the merge threads by their merge size in
        /// descending order and then pauses/unpauses threads from first to last --
        /// that way, smaller merges are guaranteed to run before larger ones.
        /// </summary>
        protected internal virtual void UpdateMergeThreads()
        {
            lock (this)
            {
                // Only look at threads that are alive & not in the
                // process of stopping (ie have an active merge):
                IList<MergeThread> activeMerges = new List<MergeThread>();

                int threadIdx = 0;
                while (threadIdx < MergeThreads.Count)
                {
                    MergeThread mergeThread = MergeThreads[threadIdx];
                    if (!mergeThread.IsAlive)
                    {
                        // Prune any dead threads
                        MergeThreads.RemoveAt(threadIdx);
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

                int pri = MergeThreadPriority_Renamed;
                int activeMergeCount = activeMerges.Count;
                for (threadIdx = 0; threadIdx < activeMergeCount; threadIdx++)
                {
                    MergeThread mergeThread = activeMerges[threadIdx];
                    MergePolicy.OneMerge merge = mergeThread.CurrentMerge;
                    if (merge == null)
                    {
                        continue;
                    }

                    // pause the thread if maxThreadCount is smaller than the number of merge threads.
                    bool doPause = threadIdx < activeMergeCount - MaxThreadCount_Renamed;

                    if (Verbose())
                    {
                        if (doPause != merge.Pause)
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
                    if (doPause != merge.Pause)
                    {
                        merge.Pause = doPause;
                    }

                    if (!doPause)
                    {
                        if (Verbose())
                        {
                            Message("set priority of merge thread " + mergeThread.Name + " to " + pri);
                        }
                        mergeThread.ThreadPriority = pri;
                        pri = Math.Min((int)ThreadPriority.Highest, 1 + pri);
                    }
                }
            }
        }

        /// <summary>
        /// Returns true if verbosing is enabled. this method is usually used in
        /// conjunction with <seealso cref="#message(String)"/>, like that:
        ///
        /// <pre class="prettyprint">
        /// if (verbose()) {
        ///   message(&quot;your message&quot;);
        /// }
        /// </pre>
        /// </summary>
        protected internal virtual bool Verbose()
        {
            return Writer != null && Writer.infoStream.IsEnabled("CMS");
        }

        /// <summary>
        /// Outputs the given message - this method assumes <seealso cref="#verbose()"/> was
        /// called and returned true.
        /// </summary>
        protected internal virtual void Message(string message)
        {
            Writer.infoStream.Message("CMS", message);
        }

        private void InitMergeThreadPriority()
        {
            lock (this)
            {
                if (MergeThreadPriority_Renamed == -1)
                {
                    // Default to slightly higher priority than our
                    // calling thread
                    MergeThreadPriority_Renamed = 1 + (int)ThreadClass.Current().Priority;
                    if (MergeThreadPriority_Renamed > (int)ThreadPriority.Highest)
                    {
                        MergeThreadPriority_Renamed = (int)ThreadPriority.Highest;
                    }
                }
            }
        }

        public override void Dispose()
        {
            Sync();
        }

        /// <summary>
        /// Wait for any running merge threads to finish. this call is not interruptible as used by <seealso cref="#close()"/>. </summary>
        public virtual void Sync()
        {
            bool interrupted = false;
            try
            {
                while (true)
                {
                    MergeThread toSync = null;
                    lock (this)
                    {
                        foreach (MergeThread t in MergeThreads)
                        {
                            if (t.IsAlive)
                            {
                                toSync = t;
                                break;
                            }
                        }
                    }
                    if (toSync != null)
                    {
                        try
                        {
                            toSync.Join();
                        }
                        catch (ThreadInterruptedException ie)
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
        /// is &lt;= <seealso cref="#mergeThreads"/> size.
        /// </summary>
        protected internal virtual int MergeThreadCount()
        {
            lock (this)
            {
                int count = 0;
                foreach (MergeThread mt in MergeThreads)
                {
                    if (mt.IsAlive && mt.CurrentMerge != null)
                    {
                        count++;
                    }
                }
                return count;
            }
        }

        public override void Merge(IndexWriter writer, MergeTrigger trigger, bool newMergesFound)
        {
            lock (this)
            {
                //Debug.Assert(!Thread.holdsLock(writer));

                this.Writer = writer;

                InitMergeThreadPriority();

                Dir = writer.Directory;

                // First, quickly run through the newly proposed merges
                // and add any orthogonal merges (ie a merge not
                // involving segments already pending to be merged) to
                // the queue.  If we are way behind on merging, many of
                // these newly proposed merges will likely already be
                // registered.

                if (Verbose())
                {
                    Message("now merge");
                    Message("  index: " + writer.SegString());
                }

                // Iterate, pulling from the IndexWriter's queue of
                // pending merges, until it's empty:
                while (true)
                {
                    long startStallTime = 0;
                    while (writer.HasPendingMerges() && MergeThreadCount() >= MaxMergeCount_Renamed)
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
                        startStallTime = Environment.TickCount;
                        if (Verbose())
                        {
                            Message("    too many merges; stalling...");
                        }
                        try
                        {
                            Monitor.Wait(this);
                        }
                        catch (ThreadInterruptedException ie)
                        {
                            throw new ThreadInterruptedException("Thread Interrupted Exception", ie);
                        }
                    }

                    if (Verbose())
                    {
                        if (startStallTime != 0)
                        {
                            Message("  stalled for " + (Environment.TickCount - startStallTime) + " msec");
                        }
                    }

                    MergePolicy.OneMerge merge = writer.NextMerge;
                    if (merge == null)
                    {
                        if (Verbose())
                        {
                            Message("  no more merges pending; now return");
                        }
                        return;
                    }

                    bool success = false;
                    try
                    {
                        if (Verbose())
                        {
                            Message("  consider merge " + writer.SegString(merge.Segments));
                        }

                        // OK to spawn a new merge thread to handle this
                        // merge:
                        MergeThread merger = GetMergeThread(writer, merge);
                        MergeThreads.Add(merger);
                        if (Verbose())
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
        }

        /// <summary>
        /// Does the actual merge, by calling <seealso cref="IndexWriter#merge"/> </summary>
        protected internal virtual void DoMerge(MergePolicy.OneMerge merge)
        {
            Writer.Merge(merge);
        }

        /// <summary>
        /// Create and return a new MergeThread </summary>
        protected internal virtual MergeThread GetMergeThread(IndexWriter writer, MergePolicy.OneMerge merge)
        {
            lock (this)
            {
                MergeThread thread = new MergeThread(this, writer, merge);
                thread.ThreadPriority = MergeThreadPriority_Renamed;
                thread.IsBackground = true;
                thread.Name = "Lucene Merge Thread #" + MergeThreadCount_Renamed++;
                return thread;
            }
        }

        /// <summary>
        /// Runs a merge thread, which may run one or more merges
        ///  in sequence.
        /// </summary>
        protected internal class MergeThread : ThreadClass//System.Threading.Thread
        {
            private readonly ConcurrentMergeScheduler OuterInstance;

            internal IndexWriter TWriter;
            internal MergePolicy.OneMerge StartMerge;
            internal MergePolicy.OneMerge RunningMerge_Renamed;
            internal volatile bool Done;

            /// <summary>
            /// Sole constructor. </summary>
            public MergeThread(ConcurrentMergeScheduler outerInstance, IndexWriter writer, MergePolicy.OneMerge startMerge)
            {
                this.OuterInstance = outerInstance;
                this.TWriter = writer;
                this.StartMerge = startMerge;
            }

            /// <summary>
            /// Record the currently running merge. </summary>
            public virtual MergePolicy.OneMerge RunningMerge
            {
                set
                {
                    lock (this)
                    {
                        RunningMerge_Renamed = value;
                    }
                }
                get
                {
                    lock (this)
                    {
                        return RunningMerge_Renamed;
                    }
                }
            }

            /// <summary>
            /// Return the current merge, or null if this {@code
            ///  MergeThread} is done.
            /// </summary>
            public virtual MergePolicy.OneMerge CurrentMerge
            {
                get
                {
                    lock (this)
                    {
                        if (Done)
                        {
                            return null;
                        }
                        else if (RunningMerge_Renamed != null)
                        {
                            return RunningMerge_Renamed;
                        }
                        else
                        {
                            return StartMerge;
                        }
                    }
                }
            }

            /// <summary>
            /// Set the priority of this thread. </summary>
            public virtual int ThreadPriority
            {
                set
                {
                    try
                    {
                        Priority = (ThreadPriority)value;
                    }
                    catch (System.NullReferenceException npe)
                    {
                        // Strangely, Sun's JDK 1.5 on Linux sometimes
                        // throws NPE out of here...
                    }
                    catch (System.Security.SecurityException se)
                    {
                        // Ignore this because we will still run fine with
                        // normal thread priority
                    }
                }
            }

            public override void Run()
            {
                // First time through the while loop we do the merge
                // that we were started with:
                MergePolicy.OneMerge merge = this.StartMerge;

                try
                {
                    if (OuterInstance.Verbose())
                    {
                        OuterInstance.Message("  merge thread: start");
                    }

                    while (true)
                    {
                        RunningMerge = merge;
                        OuterInstance.DoMerge(merge);

                        // Subsequent times through the loop we do any new
                        // merge that writer says is necessary:
                        merge = TWriter.NextMerge;

                        // Notify here in case any threads were stalled;
                        // they will notice that the pending merge has
                        // been pulled and possibly resume:
                        lock (OuterInstance)
                        {
                            Monitor.PulseAll(OuterInstance);
                        }

                        if (merge != null)
                        {
                            OuterInstance.UpdateMergeThreads();
                            if (OuterInstance.Verbose())
                            {
                                OuterInstance.Message("  merge thread: do another merge " + TWriter.SegString(merge.Segments));
                            }
                        }
                        else
                        {
                            break;
                        }
                    }

                    if (OuterInstance.Verbose())
                    {
                        OuterInstance.Message("  merge thread: done");
                    }
                }
                catch (Exception exc)
                {
                    // Ignore the exception if it was due to abort:
                    if (!(exc is MergePolicy.MergeAbortedException))
                    {
                        //System.out.println(Thread.currentThread().getName() + ": CMS: exc");
                        //exc.printStackTrace(System.out);
                        if (!OuterInstance.SuppressExceptions)
                        {
                            // suppressExceptions is normally only set during
                            // testing.
                            OuterInstance.HandleMergeException(exc);
                        }
                    }
                }
                finally
                {
                    Done = true;
                    lock (OuterInstance)
                    {
                        OuterInstance.UpdateMergeThreads();
                        Monitor.PulseAll(OuterInstance);
                    }
                }
            }
        }

        /// <summary>
        /// Called when an exception is hit in a background merge
        ///  thread
        /// </summary>
        protected internal virtual void HandleMergeException(Exception exc)
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
            catch (ThreadInterruptedException ie)
            {
                throw new ThreadInterruptedException("Thread Interrupted Exception", ie);
            }
            throw new MergePolicy.MergeException(exc, Dir);
        }

        private bool SuppressExceptions;

        /// <summary>
        /// Used for testing </summary>
        public virtual void SetSuppressExceptions()
        {
            SuppressExceptions = true;
        }

        /// <summary>
        /// Used for testing </summary>
        public virtual void ClearSuppressExceptions()
        {
            SuppressExceptions = false;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder(this.GetType().Name + ": ");
            sb.Append("maxThreadCount=").Append(MaxThreadCount_Renamed).Append(", ");
            sb.Append("maxMergeCount=").Append(MaxMergeCount_Renamed).Append(", ");
            sb.Append("mergeThreadPriority=").Append(MergeThreadPriority_Renamed);
            return sb.ToString();
        }

        public override IMergeScheduler Clone()
        {
            ConcurrentMergeScheduler clone = (ConcurrentMergeScheduler)base.Clone();
            clone.Writer = null;
            clone.Dir = null;
            clone.MergeThreads = new List<MergeThread>();
            return clone;
        }
    }
}