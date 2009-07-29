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

using Directory = Lucene.Net.Store.Directory;

namespace Lucene.Net.Index
{
    /// <summary>
    /// A {@link MergeScheduler} that runs each merge using a
    /// separate thread, up until a maximum number of threads
    /// ({@link #setMaxThreadCount}) at which when a merge is
    /// needed, the thread(s) that are updating the index will
    /// pause until one or more merges completes.  This is a
    /// simple way to use concurrency in the indexing process
    /// without having to create and manage application level
    /// threads.
    /// </summary>
    public class ConcurrentMergeScheduler : MergeScheduler
    {

        private int mergeThreadPriority = -1;

        protected System.Collections.Generic.List<MergeThread> mergeThreads = new System.Collections.Generic.List<MergeThread>();

        // max number of threads allowed to be merging at once
        private int maxThreadCount = 3;

        private System.Collections.Generic.List<System.Exception> exceptions = new System.Collections.Generic.List<System.Exception>();
        protected Directory dir;

        private bool closed;
        protected IndexWriter writer;
        protected int mergeThreadCount;

        public ConcurrentMergeScheduler()
        {
            if (allInstances != null)
            {
                // Only for testing
                AddMyself();
            }
        }

        /// <summary>Sets the max # simultaneous threads that may be
        /// running.  If a merge is necessary yet we already have
        /// this many threads running, the incoming thread (that
        /// is calling add/updateDocument) will block until
        /// a merge thread has completed.
        /// </summary>
        public virtual void SetMaxThreadCount(int count)
        {
            if (count < 1)
                throw new System.ArgumentException("count should be at least 1");
            maxThreadCount = count;
        }

        /// <summary>Get the max # simultaneous threads that may be</summary>
        /// <seealso cref="setMaxThreadCount.">
        /// </seealso>
        public virtual int GetMaxThreadCount()
        {
            return maxThreadCount;
        }

        /// <summary>Return the priority that merge threads run at.  By
        /// default the priority is 1 plus the priority of (ie,
        /// slightly higher priority than) the first thread that
        /// calls merge. 
        /// </summary>
        public virtual int GetMergeThreadPriority()
        {
            lock (this)
            {
                InitMergeThreadPriority();
                return mergeThreadPriority;
            }
        }

        /// <summary>Return the priority that merge threads run at. </summary>
        public virtual void SetMergeThreadPriority(int pri)
        {
            lock (this)
            {
                if (pri > (int)System.Threading.ThreadPriority.Highest || pri < (int)System.Threading.ThreadPriority.Lowest)
                    throw new System.ArgumentException("priority must be in range " + (int)System.Threading.ThreadPriority.Lowest + " .. " + (int)System.Threading.ThreadPriority.Highest + " inclusive");
                mergeThreadPriority = pri;

                int numThreads = MergeThreadCount();
                for (int i = 0; i < numThreads; i++)
                {
                    MergeThread merge = mergeThreads[i];
                    merge.SetThreadPriority(pri);
                }
            }
        }

        private void Message(System.String message)
        {
            if (writer != null)
                writer.Message("CMS: " + message);
        }

        private void InitMergeThreadPriority()
        {
            lock (this)
            {
                if (mergeThreadPriority == -1)
                {
                    // Default to slightly higher priority than our calling thread
                    mergeThreadPriority = 1 + (int)System.Threading.Thread.CurrentThread.Priority;
                    if (mergeThreadPriority > (int)System.Threading.ThreadPriority.Highest)
                        mergeThreadPriority = (int)System.Threading.ThreadPriority.Highest;
                }
            }
        }

        public override void Close()
        {
            closed = true;
        }

        public virtual void Sync()
        {
            lock (this)
            {
                while (MergeThreadCount() > 0)
                {
                    Message("now wait for threads; currently " + mergeThreads.Count + " still running");
                    int count = mergeThreads.Count;
                    for (int i = 0; i < count; i++)
                        Message("    " + i + ": " + mergeThreads[i]);

                    try
                    {
                        System.Threading.Monitor.Wait(this);
                    }
                    catch (System.Threading.ThreadInterruptedException)
                    {
                    }
                }
            }
        }

        private int MergeThreadCount()
        {
            lock (this)
            {
                int count = 0;
                int numThreads = mergeThreads.Count;
                for (int i = 0; i < numThreads; i++)
                    if (mergeThreads[i].IsAlive)
                        count++;
                return count;
            }
        }

        public override void Merge(IndexWriter writer)
        {
            this.writer = writer;
            InitMergeThreadPriority();
            dir = writer.GetDirectory();

            // First, quickly run through the newly proposed merges
            // and add any orthogonal merges (ie a merge not
            // involving segments already pending to be merged) to
            // the queue.  If we are way behind on merging, many of
            // these newly proposed merges will likely already be
            // registered.

            Message("now merge");
            Message("  index: " + writer.SegString());

            // Iterate, pulling from the IndexWriter's queue of
            // pending merges, until its empty:
            while (true)
            {
                // TODO: we could be careful about which merges to do in
                // the BG (eg maybe the "biggest" ones) vs FG, which
                // merges to do first (the easiest ones?), etc.

                MergePolicy.OneMerge merge = writer.GetNextMerge();
                if (merge == null)
                {
                    Message("  no more merges pending; now return");
                    return;
                }

                // We do this w/ the primary thread to keep
                // deterministic assignment of segment names
                writer.MergeInit(merge);

                lock (this)
                {
                    while (MergeThreadCount() >= maxThreadCount)
                    {
                        Message("   too may merge threads running; stalling...");
                        try
                        {
                            System.Threading.Monitor.Wait(this);
                        }
                        catch (System.Threading.ThreadInterruptedException)
                        {
                            SupportClass.ThreadClass.Current().Interrupt();
                        }
                    }

                    Message("  consider merge " + merge.SegString(dir));

                    System.Diagnostics.Debug.Assert(MergeThreadCount() < maxThreadCount);

                    // OK to spawn a new merge thread to handle this
                    // merge:
                    MergeThread merger = GetMergeThread(writer, merge);
                    mergeThreads.Add(merger);
                    Message("    launch new thread [" + merger.Name + "]");
                    merger.Start();
                }
            }
        }

        /// <summary>
        /// Does the acural merge, by calling IndexWriter.Merge().
        /// </summary>
        /// <param name="merge"></param>
        virtual protected void DoMerge(MergePolicy.OneMerge merge)
        {
            writer.Merge(merge);
        }

        /// <summary>
        /// Create and return a new MergeThread.
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="merge"></param>
        /// <returns></returns>
        virtual protected MergeThread GetMergeThread(IndexWriter writer, MergePolicy.OneMerge merge)
        {
            MergeThread thread = new MergeThread(this, writer, merge);
            thread.SetThreadPriority(mergeThreadPriority);
            thread.IsBackground = true;
            thread.Name = "Lucene Merge Thread #" + mergeThreadCount++;
            return thread;
        }

        protected class MergeThread : SupportClass.ThreadClass
        {
            private void InitBlock(ConcurrentMergeScheduler enclosingInstance)
            {
                this.enclosingInstance = enclosingInstance;
            }
            private ConcurrentMergeScheduler enclosingInstance;
            public ConcurrentMergeScheduler Enclosing_Instance
            {
                get
                {
                    return enclosingInstance;
                }

            }

            internal IndexWriter writer;
            internal MergePolicy.OneMerge startMerge;
            internal MergePolicy.OneMerge runningMerge;

            public MergeThread(ConcurrentMergeScheduler enclosingInstance, IndexWriter writer, MergePolicy.OneMerge startMerge)
            {
                InitBlock(enclosingInstance);
                this.writer = writer;
                this.startMerge = startMerge;
            }

            public virtual void SetRunningMerge(MergePolicy.OneMerge merge)
            {
                lock (this)
                {
                    runningMerge = merge;
                }
            }

            public virtual MergePolicy.OneMerge GetRunningMerge()
            {
                lock (this)
                {
                    return runningMerge;
                }
            }

            public virtual void SetThreadPriority(int pri)
            {
                try
                {
                    Priority = (System.Threading.ThreadPriority)pri;
                }
                catch (System.NullReferenceException)
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

            override public void Run()
            {

                // First time through the while loop we do the merge
                // that we were started with:
                MergePolicy.OneMerge merge = this.startMerge;

                try
                {

                    Enclosing_Instance.Message("  merge thread: start");

                    while (true)
                    {
                        SetRunningMerge(merge);
                        Enclosing_Instance.DoMerge(merge);

                        // Subsequent times through the loop we do any new
                        // merge that writer says is necessary:
                        merge = writer.GetNextMerge();
                        if (merge != null)
                        {
                            writer.MergeInit(merge);
                            Enclosing_Instance.Message("  merge thread: do another merge " + merge.SegString(Enclosing_Instance.dir));
                        }
                        else
                            break;
                    }

                    Enclosing_Instance.Message("  merge thread: done");
                }
                catch (System.Exception exc)
                {
                    // Ignore the exception if it was due to abort:
                    if (!(exc is MergePolicy.MergeAbortedException))
                    {
                        lock (Enclosing_Instance)
                        {
                            Enclosing_Instance.exceptions.Add(exc);
                        }

                        if (!Enclosing_Instance.suppressExceptions)
                        {
                            // suppressExceptions is normally only set during
                            // testing.
                            Lucene.Net.Index.ConcurrentMergeScheduler.anyExceptions = true;
                            Enclosing_Instance.HandleMergeException(exc);
                        }
                    }
                }
                finally
                {
                    lock (Enclosing_Instance)
                    {
                        System.Threading.Monitor.PulseAll(Enclosing_Instance);
                        bool removed = Enclosing_Instance.mergeThreads.Remove(this);
                        System.Diagnostics.Debug.Assert(removed);
                    }
                }
            }

            public override System.String ToString()
            {
                MergePolicy.OneMerge merge = GetRunningMerge();
                if (merge == null)
                    merge = startMerge;
                return "merge thread: " + merge.SegString(Enclosing_Instance.dir);
            }
        }

        virtual protected void HandleMergeException(System.Exception exc)
        {
            throw new MergePolicy.MergeException(exc, dir);
        }

        internal static bool anyExceptions = false;

        /// <summary>Used for testing </summary>
        public static bool AnyUnhandledExceptions()
        {
            lock (allInstances.SyncRoot)
            {
                int count = allInstances.Count;
                // Make sure all outstanding threads are done so we see
                // any exceptions they may produce:
                for (int i = 0; i < count; i++)
                    ((ConcurrentMergeScheduler)allInstances[i]).Sync();
                bool v = anyExceptions;
                anyExceptions = false;
                return v;
            }
        }

        public static void ClearUnhandledExceptions()
        {
            lock (allInstances)
            {
                anyExceptions = false;
            }
        }

        /// <summary>Used for testing </summary>
        private void AddMyself()
        {
            lock (allInstances.SyncRoot)
            {
                int size = 0;
                int upto = 0;
                for (int i = 0; i < size; i++)
                {
                    ConcurrentMergeScheduler other = (ConcurrentMergeScheduler)allInstances[i];
                    if (!(other.closed && 0 == other.MergeThreadCount()))
                        // Keep this one for now: it still has threads or
                        // may spawn new threads
                        allInstances[upto++] = other;
                }
                ((System.Collections.IList)((System.Collections.ArrayList)allInstances).GetRange(upto, allInstances.Count - upto)).Clear();
                allInstances.Add(this);
            }
        }

        private bool suppressExceptions;

        /// <summary>Used for testing </summary>
        internal virtual void SetSuppressExceptions()
        {
            suppressExceptions = true;
        }

        /// <summary>Used for testing </summary>
        internal virtual void ClearSuppressExceptions()
        {
            suppressExceptions = false;
        }

        /// <summary>Used for testing </summary>
        private static System.Collections.IList allInstances;
        public static void SetTestMode()
        {
            allInstances = new System.Collections.ArrayList();
        }

        public void SetSuppressExceptions_ForNUnitTest()
        {
            SetSuppressExceptions();
        }

        public void ClearSuppressExceptions_ForNUnitTest()
        {
            ClearSuppressExceptions();
        }
    }
}