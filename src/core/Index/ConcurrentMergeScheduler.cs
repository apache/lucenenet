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

using System.Collections.Generic;
using Lucene.Net.Support;
using Directory = Lucene.Net.Store.Directory;
using System;
using System.Threading;
using Lucene.Net.Util;
using System.Text;

namespace Lucene.Net.Index
{

    /// <summary>A <see cref="MergeScheduler" /> that runs each merge using a
    /// separate thread, up until a maximum number of threads
    /// (<see cref="MaxThreadCount" />) at which when a merge is
    /// needed, the thread(s) that are updating the index will
    /// pause until one or more merges completes.  This is a
    /// simple way to use concurrency in the indexing process
    /// without having to create and manage application level
    /// threads. 
    /// </summary>

    public class ConcurrentMergeScheduler : MergeScheduler
    {

        private int mergeThreadPriority = -1;

        protected internal IList<MergeThread> mergeThreads = new List<MergeThread>();

        // Max number of threads allowed to be merging at once
        private int maxThreadCount = 1;

        private int maxMergeCount = 2;

        protected internal Directory dir;

        protected internal IndexWriter writer;

        protected internal int mergeThreadCount;

        public ConcurrentMergeScheduler()
        {
            //if (allInstances != null)
            //{
            //    // Only for testing
            //    AddMyself();
            //}
        }

        /// <summary>Gets or sets the max # simultaneous threads that may be
        /// running.  If a merge is necessary yet we already have
        /// this many threads running, the incoming thread (that
        /// is calling add/updateDocument) will block until
        /// a merge thread has completed. 
        /// </summary>
        public virtual int MaxThreadCount
        {
            set
            {
                if (value < 1)
                    throw new ArgumentException("count should be at least 1");
                if (value > maxMergeCount)
                    throw new ArgumentException("count should be <= maxMergeCount (= " + maxMergeCount + ")");
                maxThreadCount = value;
            }
            get { return maxThreadCount; }
        }

        public virtual int MaxMergeCount
        {
            get { return maxMergeCount; }
            set
            {
                if (value < 1)
                {
                    throw new ArgumentException("count should be at least 1");
                }
                if (value < maxThreadCount)
                {
                    throw new ArgumentException("count should be >= maxThreadCount (= " + maxThreadCount + ")");
                }
                maxMergeCount = value;
            }
        }

        /// <summary>Return the priority that merge threads run at.  By
        /// default the priority is 1 plus the priority of (ie,
        /// slightly higher priority than) the first thread that
        /// calls merge. 
        /// </summary>
        public virtual int MergeThreadPriority
        {
            get
            {
                lock (this)
                {
                    InitMergeThreadPriority();
                    return mergeThreadPriority;
                }
            }
            set
            {
                lock (this)
                {
                    if (value > (int)ThreadPriority.Highest || value < (int)ThreadPriority.Lowest)
                        throw new ArgumentException("priority must be in range " + (int)ThreadPriority.Lowest + " .. " + (int)ThreadPriority.Highest + " inclusive");
                    mergeThreadPriority = value;

                    int numThreads = MergeThreadCount;
                    for (int i = 0; i < numThreads; i++)
                    {
                        MergeThread merge = mergeThreads[i];
                        merge.SetThreadPriority(value);
                    }
                }
            }
        }

        private sealed class AnonymousCompareByMergeDocCountComparer : IComparer<ConcurrentMergeScheduler.MergeThread>
        {
            public int Compare(MergeThread t1, MergeThread t2)
            {
                MergePolicy.OneMerge m1 = t1.CurrentMerge;
                MergePolicy.OneMerge m2 = t2.CurrentMerge;

                int c1 = m1 == null ? int.MaxValue : m1.TotalDocCount;
                int c2 = m2 == null ? int.MaxValue : m2.TotalDocCount;

                return c2 - c1;
            }
        }

        protected static readonly IComparer<MergeThread> compareByMergeDocCount = new AnonymousCompareByMergeDocCountComparer();

        protected void UpdateMergeThreads()
        {
            lock (this)
            {
                // Only look at threads that are alive & not in the
                // process of stopping (ie have an active merge):
                IList<MergeThread> activeMerges = new List<MergeThread>();

                int threadIdx = 0;
                while (threadIdx < mergeThreads.Count)
                {
                    MergeThread mergeThread = mergeThreads[threadIdx];
                    if (!mergeThread.IsAlive)
                    {
                        // Prune any dead threads
                        mergeThreads.RemoveAt(threadIdx);
                        continue;
                    }
                    if (mergeThread.CurrentMerge != null)
                    {
                        activeMerges.Add(mergeThread);
                    }
                    threadIdx++;
                }

                // Sort the merge threads in descending order.
                CollectionUtil.MergeSort(activeMerges, compareByMergeDocCount);

                int pri = mergeThreadPriority;
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
                    bool doPause = threadIdx < activeMergeCount - maxThreadCount;

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
                        mergeThread.SetThreadPriority(pri);
                        pri = Math.Min((int)ThreadPriority.Highest, 1 + pri);
                    }
                }
            }
        }

        private bool Verbose()
        {
            return writer != null && writer.InfoStream.IsEnabled("CMS");
        }

        private void Message(String message)
        {
            if (Verbose())
                writer.InfoStream.Message("CMS", message);
        }

        private void InitMergeThreadPriority()
        {
            lock (this)
            {
                if (mergeThreadPriority == -1)
                {
                    // Default to slightly higher priority than our
                    // calling thread
                    mergeThreadPriority = 1 + (System.Int32)ThreadClass.Current().Priority;
                    if (mergeThreadPriority > (int)System.Threading.ThreadPriority.Highest)
                        mergeThreadPriority = (int)System.Threading.ThreadPriority.Highest;
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            Sync();
        }

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
                        foreach (MergeThread t in mergeThreads)
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
                if (interrupted) Thread.CurrentThread.Interrupt();
            }
        }

        private int MergeThreadCount
        {
            get
            {
                lock (this)
                {
                    int count = 0;
                    foreach (MergeThread mt in mergeThreads)
                    {
                        if (mt.IsAlive && mt.CurrentMerge != null)
                        {
                            count++;
                        }
                    }
                    return count;
                }
            }
        }

        public override void Merge(IndexWriter writer)
        {
            // TODO: .NET doesn't support this
            // assert !Thread.holdsLock(writer);

            this.writer = writer;

            InitMergeThreadPriority();

            dir = writer.Directory;

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
                while (writer.HasPendingMerges && MergeThreadCount >= maxMergeCount)
                {
                    // This means merging has fallen too far behind: we
                    // have already created maxMergeCount threads, and
                    // now there's at least one more merge pending.
                    // Note that only maxThreadCount of
                    // those created merge threads will actually be
                    // running; the rest will be paused (see
                    // updateMergeThreads).  We stall this producer
                    // thread to prevent creation of new segments,
                    // until merging has caught up:
                    startStallTime = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;
                    if (Verbose())
                    {
                        Message("    too many merges; stalling...");
                    }
                    try
                    {
                        Wait();
                    }
                    catch (ThreadInterruptedException ie)
                    {
                        throw new ThreadInterruptedException(ie);
                    }
                }

                if (Verbose())
                {
                    if (startStallTime != 0)
                    {
                        Message("  stalled for " + ((DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond) - startStallTime) + " msec");
                    }
                }

                MergePolicy.OneMerge merge = writer.GetNextMerge();
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
                        Message("  consider merge " + writer.SegString(merge.segments));
                    }

                    // OK to spawn a new merge thread to handle this
                    // merge:
                    MergeThread merger = GetMergeThread(writer, merge);
                    mergeThreads.Add(merger);
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

        /// <summary>Does the actual merge, by calling <see cref="IndexWriter.Merge" /> </summary>
        protected internal virtual void DoMerge(MergePolicy.OneMerge merge)
        {
            writer.Merge(merge);
        }

        /// <summary>Create and return a new MergeThread </summary>
        protected internal virtual MergeThread GetMergeThread(IndexWriter writer, MergePolicy.OneMerge merge)
        {
            lock (this)
            {
                var thread = new MergeThread(this, writer, merge);
                thread.SetThreadPriority(mergeThreadPriority);
                thread.IsBackground = true;
                thread.Name = "Lucene Merge Thread #" + mergeThreadCount++;
                return thread;
            }
        }

        public class MergeThread : ThreadClass
        {
            private readonly ConcurrentMergeScheduler parent;
            internal IndexWriter tWriter;
            internal MergePolicy.OneMerge startMerge;
            internal MergePolicy.OneMerge runningMerge;
            private volatile bool done;

            public MergeThread(ConcurrentMergeScheduler parent, IndexWriter writer, MergePolicy.OneMerge startMerge)
            {
                this.parent = parent;
                this.tWriter = writer;
                this.startMerge = startMerge;
            }

            public virtual MergePolicy.OneMerge RunningMerge
            {
                get
                {
                    lock (this)
                    {
                        return runningMerge;
                    }
                }
                set
                {
                    lock (this)
                    {
                        runningMerge = value;
                    }
                }
            }

            public virtual MergePolicy.OneMerge CurrentMerge
            {
                get
                {
                    lock (this)
                    {
                        if (done)
                            return null;
                        else if (runningMerge != null)
                            return runningMerge;
                        else
                            return startMerge;
                    }
                }
            }

            public virtual void SetThreadPriority(int pri)
            {
                try
                {
                    Priority = (ThreadPriority)pri;
                }
                catch (NullReferenceException)
                {
                    // Strangely, Sun's JDK 1.5 on Linux sometimes
                    // throws NPE out of here...
                }
                catch (System.Security.SecurityException)
                {
                    // Ignore this because we will still run fine with
                    // normal thread priority
                }
            }

            public override void Run()
            {

                // First time through the while loop we do the merge
                // that we were started with:
                MergePolicy.OneMerge merge = this.startMerge;

                try
                {

                    if (parent.Verbose())
                        parent.Message("  merge thread: start");

                    while (true)
                    {
                        RunningMerge = merge;
                        parent.DoMerge(merge);

                        // Subsequent times through the loop we do any new
                        // merge that writer says is necessary:
                        merge = tWriter.GetNextMerge();

                        // Notify here in case any threads were stalled;
                        // they will notice that the pending merge has
                        // been pulled and possibly resume:
                        lock (parent)
                        {
                            Monitor.PulseAll(parent);
                        }

                        if (merge != null)
                        {
                            parent.UpdateMergeThreads();
                            if (parent.Verbose())
                                parent.Message("  merge thread: do another merge " + merge.SegString(merge.segments));
                        }
                        else
                            break;
                    }

                    if (parent.Verbose())
                        parent.Message("  merge thread: done");
                }
                catch (Exception exc)
                {
                    // Ignore the exception if it was due to abort:
                    if (!(exc is MergePolicy.MergeAbortedException))
                    {
                        if (!parent.suppressExceptions)
                        {
                            // suppressExceptions is normally only set during
                            // testing.
                            parent.HandleMergeException(exc);
                        }
                    }
                }
                finally
                {
                    done = true;
                    lock (parent)
                    {
                        parent.UpdateMergeThreads();
                        Monitor.PulseAll(parent);
                    }
                }
            }

            public override String ToString()
            {
                MergePolicy.OneMerge merge = RunningMerge ?? startMerge;
                return "merge thread: " + merge.SegString(parent.dir);
            }
        }

        /// <summary>Called when an exception is hit in a background merge
        /// thread 
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
            catch (ThreadInterruptedException)
            {
                throw;
            }
            throw new MergePolicy.MergeException(exc, dir);
        }
        
        private bool suppressExceptions;

        /// <summary>Used for testing </summary>
        public virtual void SetSuppressExceptions()
        {
            suppressExceptions = true;
        }

        /// <summary>Used for testing </summary>
        public virtual void ClearSuppressExceptions()
        {
            suppressExceptions = false;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder(GetType().Name + ": ");
            sb.Append("maxThreadCount=").Append(maxThreadCount).Append(", ");
            sb.Append("maxMergeCount=").Append(maxMergeCount).Append(", ");
            sb.Append("mergeThreadPriority=").Append(mergeThreadPriority);
            return sb.ToString();
        }

        public override object Clone()
        {
            ConcurrentMergeScheduler clone = (ConcurrentMergeScheduler)base.Clone();
            clone.writer = null;
            clone.dir = null;
            clone.mergeThreads = new List<MergeThread>();
            return clone;
        }
    }
}