using System;
using System.Diagnostics;
using System.Threading;

namespace Lucene.Net.Index
{
    /*
     * Licensed to the Apache Software Foundation (ASF) under one or more
     * contributor license agreements. See the NOTICE file distributed with
     * this work for additional information regarding copyright ownership.
     * The ASF licenses this file to You under the Apache License, Version 2.0
     * (the "License"); you may not use this file except in compliance with
     * the License. You may obtain a copy of the License at
     *
     * http://www.apache.org/licenses/LICENSE-2.0
     *
     * Unless required by applicable law or agreed to in writing, software
     * distributed under the License is distributed on an "AS IS" BASIS,
     * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
     * See the License for the specific language governing permissions and
     * limitations under the License.
     */

    using Lucene.Net.Support;

    /// <summary>
    /// <seealso cref="DocumentsWriterPerThreadPool"/> controls <seealso cref="ThreadState"/> instances
    /// and their thread assignments during indexing. Each <seealso cref="ThreadState"/> holds
    /// a reference to a <seealso cref="DocumentsWriterPerThread"/> that is once a
    /// <seealso cref="ThreadState"/> is obtained from the pool exclusively used for indexing a
    /// single document by the obtaining thread. Each indexing thread must obtain
    /// such a <seealso cref="ThreadState"/> to make progress. Depending on the
    /// <seealso cref="DocumentsWriterPerThreadPool"/> implementation <seealso cref="ThreadState"/>
    /// assignments might differ from document to document.
    /// <p>
    /// Once a <seealso cref="DocumentsWriterPerThread"/> is selected for flush the thread pool
    /// is reusing the flushing <seealso cref="DocumentsWriterPerThread"/>s ThreadState with a
    /// new <seealso cref="DocumentsWriterPerThread"/> instance.
    /// </p>
    /// </summary>
    public abstract class DocumentsWriterPerThreadPool
    {
        /// <summary>
        /// <seealso cref="ThreadState"/> references and guards a
        /// <seealso cref="DocumentsWriterPerThread"/> instance that is used during indexing to
        /// build a in-memory index segment. <seealso cref="ThreadState"/> also holds all flush
        /// related per-thread data controlled by <seealso cref="DocumentsWriterFlushControl"/>.
        /// <p>
        /// A <seealso cref="ThreadState"/>, its methods and members should only accessed by one
        /// thread a time. Users must acquire the lock via <seealso cref="ThreadState#lock()"/>
        /// and release the lock in a finally block via <seealso cref="ThreadState#unlock()"/>
        /// before accessing the state.
        /// </summary>
        public sealed class ThreadState : ReentrantLock
        {
            internal DocumentsWriterPerThread Dwpt;

            // TODO this should really be part of DocumentsWriterFlushControl
            // write access guarded by DocumentsWriterFlushControl
            internal volatile bool FlushPending_Renamed = false;

            // TODO this should really be part of DocumentsWriterFlushControl
            // write access guarded by DocumentsWriterFlushControl
            internal long BytesUsed = 0;

            // guarded by Reentrant lock
            internal bool IsActive = true;

            public ThreadState(DocumentsWriterPerThread dpwt)
            {
                this.Dwpt = dpwt;
            }

            /// <summary>
            /// Resets the internal <seealso cref="DocumentsWriterPerThread"/> with the given one.
            /// if the given DWPT is <code>null</code> this ThreadState is marked as inactive and should not be used
            /// for indexing anymore. </summary>
            /// <seealso cref= #isActive()   </seealso>

            internal void Deactivate()
            {
                //Debug.Assert(this.HeldByCurrentThread);
                IsActive = false;
                Reset();
            }

            internal void Reset()
            {
                //Debug.Assert(this.HeldByCurrentThread);
                this.Dwpt = null;
                this.BytesUsed = 0;
                this.FlushPending_Renamed = false;
            }

            /// <summary>
            /// Returns <code>true</code> if this ThreadState is still open. this will
            /// only return <code>false</code> iff the DW has been closed and this
            /// ThreadState is already checked out for flush.
            /// </summary>
            internal bool Active
            {
                get
                {
                    //Debug.Assert(this.HeldByCurrentThread);
                    return IsActive;
                }
            }

            internal bool Initialized
            {
                get
                {
                    //Debug.Assert(this.HeldByCurrentThread);
                    return Active && Dwpt != null;
                }
            }

            /// <summary>
            /// Returns the number of currently active bytes in this ThreadState's
            /// <seealso cref="DocumentsWriterPerThread"/>
            /// </summary>
            public long BytesUsedPerThread
            {
                get
                {
                    //Debug.Assert(this.HeldByCurrentThread);
                    // public for FlushPolicy
                    return BytesUsed;
                }
            }

            /// <summary>
            /// Returns this <seealso cref="ThreadState"/>s <seealso cref="DocumentsWriterPerThread"/>
            /// </summary>
            public DocumentsWriterPerThread DocumentsWriterPerThread
            {
                get
                {
                    //Debug.Assert(this.HeldByCurrentThread);
                    // public for FlushPolicy
                    return Dwpt;
                }
            }

            /// <summary>
            /// Returns <code>true</code> iff this <seealso cref="ThreadState"/> is marked as flush
            /// pending otherwise <code>false</code>
            /// </summary>
            public bool FlushPending
            {
                get
                {
                    return FlushPending_Renamed;
                }
            }
        }

        private ThreadState[] ThreadStates;
        private volatile int NumThreadStatesActive;

        /// <summary>
        /// Creates a new <seealso cref="DocumentsWriterPerThreadPool"/> with a given maximum of <seealso cref="ThreadState"/>s.
        /// </summary>
        public DocumentsWriterPerThreadPool(int maxNumThreadStates)
        {
            if (maxNumThreadStates < 1)
            {
                throw new System.ArgumentException("maxNumThreadStates must be >= 1 but was: " + maxNumThreadStates);
            }
            ThreadStates = new ThreadState[maxNumThreadStates];
            NumThreadStatesActive = 0;
            for (int i = 0; i < ThreadStates.Length; i++)
            {
                ThreadStates[i] = new ThreadState(null);
            }
        }

        public virtual object Clone()
        {
            // We should only be cloned before being used:
            if (NumThreadStatesActive != 0)
            {
                throw new InvalidOperationException("clone this object before it is used!");
            }

            DocumentsWriterPerThreadPool clone;

            clone = (DocumentsWriterPerThreadPool)base.MemberwiseClone();

            clone.ThreadStates = new ThreadState[ThreadStates.Length];
            for (int i = 0; i < ThreadStates.Length; i++)
            {
                clone.ThreadStates[i] = new ThreadState(null);
            }
            return clone;
        }

        /// <summary>
        /// Returns the max number of <seealso cref="ThreadState"/> instances available in this
        /// <seealso cref="DocumentsWriterPerThreadPool"/>
        /// </summary>
        public virtual int MaxThreadStates
        {
            get
            {
                return ThreadStates.Length;
            }
        }

        /// <summary>
        /// Returns the active number of <seealso cref="ThreadState"/> instances.
        /// </summary>
        public virtual int ActiveThreadState
        {
            get
            {
                return NumThreadStatesActive;
            }
        }

        /// <summary>
        /// Returns a new <seealso cref="ThreadState"/> iff any new state is available otherwise
        /// <code>null</code>.
        /// <p>
        /// NOTE: the returned <seealso cref="ThreadState"/> is already locked iff non-
        /// <code>null</code>.
        /// </summary>
        /// <returns> a new <seealso cref="ThreadState"/> iff any new state is available otherwise
        ///         <code>null</code> </returns>
        public virtual ThreadState NewThreadState()
        {
            lock (this)
            {
                if (NumThreadStatesActive < ThreadStates.Length)
                {
                    ThreadState threadState = ThreadStates[NumThreadStatesActive];
                    threadState.@Lock(); // lock so nobody else will get this ThreadState
                    bool unlock = true;
                    try
                    {
                        if (threadState.Active)
                        {
                            // unreleased thread states are deactivated during DW#close()
                            NumThreadStatesActive++; // increment will publish the ThreadState
                            Debug.Assert(threadState.Dwpt == null);
                            unlock = false;
                            return threadState;
                        }
                        // unlock since the threadstate is not active anymore - we are closed!
                        Debug.Assert(AssertUnreleasedThreadStatesInactive());
                        return null;
                    }
                    finally
                    {
                        if (unlock)
                        {
                            // in any case make sure we unlock if we fail
                            threadState.Unlock();
                        }
                    }
                }
                return null;
            }
        }

        private bool AssertUnreleasedThreadStatesInactive()
        {
            lock (this)
            {
                for (int i = NumThreadStatesActive; i < ThreadStates.Length; i++)
                {
                    Debug.Assert(ThreadStates[i].TryLock(), "unreleased threadstate should not be locked");
                    try
                    {
                        Debug.Assert(!ThreadStates[i].Initialized, "expected unreleased thread state to be inactive");
                    }
                    finally
                    {
                        ThreadStates[i].Unlock();
                    }
                }
                return true;
            }
        }

        /// <summary>
        /// Deactivate all unreleased threadstates
        /// </summary>
        internal virtual void DeactivateUnreleasedStates()
        {
            lock (this)
            {
                for (int i = NumThreadStatesActive; i < ThreadStates.Length; i++)
                {
                    ThreadState threadState = ThreadStates[i];
                    threadState.@Lock();
                    try
                    {
                        threadState.Deactivate();
                    }
                    finally
                    {
                        threadState.Unlock();
                    }
                }
            }
        }

        internal virtual DocumentsWriterPerThread Reset(ThreadState threadState, bool closed)
        {
            //Debug.Assert(threadState.HeldByCurrentThread);
            DocumentsWriterPerThread dwpt = threadState.Dwpt;
            if (!closed)
            {
                threadState.Reset();
            }
            else
            {
                threadState.Deactivate();
            }
            return dwpt;
        }

        internal virtual void Recycle(DocumentsWriterPerThread dwpt)
        {
            // don't recycle DWPT by default
        }

        // you cannot subclass this without being in o.a.l.index package anyway, so
        // the class is already pkg-private... fix me: see LUCENE-4013
        public abstract ThreadState GetAndLock(Thread requestingThread, DocumentsWriter documentsWriter);

        /// <summary>
        /// Returns the <i>i</i>th active <seealso cref="ThreadState"/> where <i>i</i> is the
        /// given ord.
        /// </summary>
        /// <param name="ord">
        ///          the ordinal of the <seealso cref="ThreadState"/> </param>
        /// <returns> the <i>i</i>th active <seealso cref="ThreadState"/> where <i>i</i> is the
        ///         given ord. </returns>
        internal virtual ThreadState GetThreadState(int ord)
        {
            return ThreadStates[ord];
        }

        /// <summary>
        /// Returns the ThreadState with the minimum estimated number of threads
        /// waiting to acquire its lock or <code>null</code> if no <seealso cref="ThreadState"/>
        /// is yet visible to the calling thread.
        /// </summary>
        internal virtual ThreadState MinContendedThreadState()
        {
            ThreadState minThreadState = null;
            int limit = NumThreadStatesActive;
            for (int i = 0; i < limit; i++)
            {
                ThreadState state = ThreadStates[i];
                if (minThreadState == null || state.QueueLength < minThreadState.QueueLength)
                {
                    minThreadState = state;
                }
            }
            return minThreadState;
        }

        /// <summary>
        /// Returns the number of currently deactivated <seealso cref="ThreadState"/> instances.
        /// A deactivated <seealso cref="ThreadState"/> should not be used for indexing anymore.
        /// </summary>
        /// <returns> the number of currently deactivated <seealso cref="ThreadState"/> instances. </returns>
        internal virtual int NumDeactivatedThreadStates()
        {
            int count = 0;
            for (int i = 0; i < ThreadStates.Length; i++)
            {
                ThreadState threadState = ThreadStates[i];
                threadState.@Lock();
                try
                {
                    if (!threadState.IsActive)
                    {
                        count++;
                    }
                }
                finally
                {
                    threadState.Unlock();
                }
            }
            return count;
        }

        /// <summary>
        /// Deactivates an active <seealso cref="ThreadState"/>. Inactive <seealso cref="ThreadState"/> can
        /// not be used for indexing anymore once they are deactivated. this method should only be used
        /// if the parent <seealso cref="DocumentsWriter"/> is closed or aborted.
        /// </summary>
        /// <param name="threadState"> the state to deactivate </param>
        internal virtual void DeactivateThreadState(ThreadState threadState)
        {
            Debug.Assert(threadState.Active);
            threadState.Deactivate();
        }
    }
}