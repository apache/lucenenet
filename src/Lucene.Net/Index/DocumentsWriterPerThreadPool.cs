// Lucene version compatibility level: 4.8.1
using Lucene.Net.Diagnostics;
using Lucene.Net.Support.Threading;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
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

    /// <summary>
    /// <see cref="DocumentsWriterPerThreadPool"/> controls <see cref="ThreadState"/> instances
    /// and their thread assignments during indexing. Each <see cref="ThreadState"/> holds
    /// a reference to a <see cref="DocumentsWriterPerThread"/> that is once a
    /// <see cref="ThreadState"/> is obtained from the pool exclusively used for indexing a
    /// single document by the obtaining thread. Each indexing thread must obtain
    /// such a <see cref="ThreadState"/> to make progress. Depending on the
    /// <see cref="DocumentsWriterPerThreadPool"/> implementation <see cref="ThreadState"/>
    /// assignments might differ from document to document.
    /// <para/>
    /// Once a <see cref="DocumentsWriterPerThread"/> is selected for flush the thread pool
    /// is reusing the flushing <see cref="DocumentsWriterPerThread"/>s <see cref="ThreadState"/> with a
    /// new <see cref="DocumentsWriterPerThread"/> instance.
    /// </summary>

    internal sealed class DocumentsWriterPerThreadPool // LUCENENET specific: Not implementing ICloneable per Microsoft's recommendation
    {
        /// <summary>
        /// <see cref="ThreadState"/> references and guards a
        /// <see cref="Index.DocumentsWriterPerThread"/> instance that is used during indexing to
        /// build a in-memory index segment. <see cref="ThreadState"/> also holds all flush
        /// related per-thread data controlled by <see cref="DocumentsWriterFlushControl"/>.
        /// <para/>
        /// A <see cref="ThreadState"/>, its methods and members should only accessed by one
        /// thread a time. Users must acquire the lock via <see cref="ReentrantLock.Lock()"/>
        /// and release the lock in a finally block via <see cref="ReentrantLock.Unlock()"/>
        /// (on the <see cref="ThreadState"/> instance) before accessing the state.
        /// </summary>
        internal sealed class ThreadState : ReentrantLock
        {
            internal DocumentsWriterPerThread dwpt;

            // TODO this should really be part of DocumentsWriterFlushControl
            // write access guarded by DocumentsWriterFlushControl
            internal volatile bool flushPending = false;

            // TODO this should really be part of DocumentsWriterFlushControl
            // write access guarded by DocumentsWriterFlushControl
            internal long bytesUsed = 0;

            // guarded by Reentrant lock
            internal bool isActive = true;

            internal ThreadState(DocumentsWriterPerThread dpwt)
            {
                this.dwpt = dpwt;
            }

            /// <summary>
            /// Resets the internal <see cref="DocumentsWriterPerThread"/> with the given one.
            /// if the given DWPT is <c>null</c> this <see cref="ThreadState"/> is marked as inactive and should not be used
            /// for indexing anymore. </summary>
            /// <seealso cref="IsActive"/>
            internal void Deactivate() // LUCENENET NOTE: Made internal because it is called outside of this context
            {
                if (Debugging.AssertsEnabled) Debugging.Assert(this.IsHeldByCurrentThread);
                isActive = false;
                Reset();
            }

            internal void Reset() // LUCENENET NOTE: Made internal because it is called outside of this context
            {
                if (Debugging.AssertsEnabled) Debugging.Assert(this.IsHeldByCurrentThread);
                this.dwpt = null;
                this.bytesUsed = 0;
                this.flushPending = false;
            }

            /// <summary>
            /// Returns <c>true</c> if this <see cref="ThreadState"/> is still open. This will
            /// only return <c>false</c> iff the DW has been disposed and this
            /// <see cref="ThreadState"/> is already checked out for flush.
            /// </summary>
            internal bool IsActive
            {
                get
                {
                    if (Debugging.AssertsEnabled) Debugging.Assert(this.IsHeldByCurrentThread);
                    return isActive;
                }

            }

            internal bool IsInitialized
            {
                get
                {
                    if (Debugging.AssertsEnabled) Debugging.Assert(this.IsHeldByCurrentThread);
                    return IsActive && dwpt != null;
                }
            }


            /// <summary>
            /// Returns the number of currently active bytes in this ThreadState's
            /// <see cref="DocumentsWriterPerThread"/>
            /// </summary>
            public long BytesUsedPerThread
            {
                get
                {
                    if (Debugging.AssertsEnabled) Debugging.Assert(this.IsHeldByCurrentThread);
                    // public for FlushPolicy
                    return bytesUsed;
                }
            }

            /// <summary>
            /// Returns this <see cref="ThreadState"/>s <see cref="DocumentsWriterPerThread"/>
            /// </summary>
            public DocumentsWriterPerThread DocumentsWriterPerThread
            {
                get
                {
                    if (Debugging.AssertsEnabled) Debugging.Assert(this.IsHeldByCurrentThread);
                    // public for FlushPolicy
                    return dwpt;
                }
            }

            /// <summary>
            /// Returns <c>true</c> iff this <see cref="ThreadState"/> is marked as flush
            /// pending otherwise <c>false</c>
            /// </summary>
            public bool IsFlushPending => flushPending;
        }

        private readonly ThreadState[] threadStates;
        private volatile int numThreadStatesActive;

        private readonly ThreadState[] freeList;
        private volatile int freeCount;

        /// <summary>
        /// Creates a new <see cref="DocumentsWriterPerThreadPool"/> with a given maximum of <see cref="ThreadState"/>s.
        /// </summary>
        internal DocumentsWriterPerThreadPool(int maxNumThreadStates)
        {
            if (maxNumThreadStates < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(maxNumThreadStates), "maxNumThreadStates must be >= 1 but was: " + maxNumThreadStates); // LUCENENET specific - changed from IllegalArgumentException to ArgumentOutOfRangeException (.NET convention)
            }
            threadStates = new ThreadState[maxNumThreadStates];
            numThreadStatesActive = 0;
            for (int i = 0; i < threadStates.Length; i++)
            {
                threadStates[i] = new ThreadState(null);
            }
            freeList = new ThreadState[maxNumThreadStates];
        }

        public object Clone()
        {
            // We should only be cloned before being used:
            if (numThreadStatesActive != 0)
            {
                throw IllegalStateException.Create("clone this object before it is used!");
            }
            return new DocumentsWriterPerThreadPool(threadStates.Length);
        }

        /// <summary>
        /// Returns the max number of <see cref="ThreadState"/> instances available in this
        /// <see cref="DocumentsWriterPerThreadPool"/>
        /// </summary>
        public int MaxThreadStates => threadStates.Length;

        /// <summary>
        /// Returns the active number of <see cref="ThreadState"/> instances.
        /// </summary>
        public int NumThreadStatesActive => numThreadStatesActive; // LUCENENET NOTE: Changed from getActiveThreadState() because the name wasn't clear

        /// <summary>
        /// Returns a new <see cref="ThreadState"/> iff any new state is available otherwise
        /// <c>null</c>.
        /// <para/>
        /// NOTE: the returned <see cref="ThreadState"/> is already locked iff non-<c>null</c>.
        /// </summary>
        /// <returns> a new <see cref="ThreadState"/> iff any new state is available otherwise
        ///         <c>null</c> </returns>
        public ThreadState NewThreadState()
        {
            if (Debugging.AssertsEnabled) Debugging.Assert(numThreadStatesActive < threadStates.Length);

            ThreadState threadState = threadStates[numThreadStatesActive];
            threadState.Lock(); // lock so nobody else will get this ThreadState
            bool unlock = true;
            try
            {
                if (threadState.IsActive)
                {
                    // unreleased thread states are deactivated during DW#close()
                    numThreadStatesActive++; // increment will publish the ThreadState
                                                    //System.out.println("activeCount=" + numThreadStatesActive);
                    if (Debugging.AssertsEnabled) Debugging.Assert(threadState.dwpt is null);
                    unlock = false;
                    return threadState;
                }
                // we are closed: unlock since the threadstate is not active anymore
                if (Debugging.AssertsEnabled) Debugging.Assert(AssertUnreleasedThreadStatesInactive());
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

        private bool AssertUnreleasedThreadStatesInactive()
        {
            UninterruptableMonitor.Enter(this);
            try
            {
                for (int i = numThreadStatesActive; i < threadStates.Length; i++)
                {
                    if (Debugging.AssertsEnabled) Debugging.Assert(threadStates[i].TryLock(), "unreleased threadstate should not be locked");
                    try
                    {
                        if (Debugging.AssertsEnabled) Debugging.Assert(!threadStates[i].IsInitialized, "expected unreleased thread state to be inactive");
                    }
                    finally
                    {
                        threadStates[i].Unlock();
                    }
                }
                return true;
            }
            finally
            {
                UninterruptableMonitor.Exit(this);
            }
        }

        /// <summary>
        /// Deactivate all unreleased threadstates
        /// </summary>
        internal void DeactivateUnreleasedStates()
        {
            UninterruptableMonitor.Enter(this);
            try
            {
                for (int i = numThreadStatesActive; i < threadStates.Length; i++)
                {
                    ThreadState threadState = threadStates[i];
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
            finally
            {
                UninterruptableMonitor.Exit(this);
            }
        }

        internal static DocumentsWriterPerThread Reset(ThreadState threadState, bool closed) // LUCENENET: CA1822: Mark members as static
        {
            if (Debugging.AssertsEnabled) Debugging.Assert(threadState.IsHeldByCurrentThread);
            DocumentsWriterPerThread dwpt = threadState.dwpt;
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

        // LUCENENET: Called in one place, but since there is no implementation it is just wasted CPU
        //internal void Recycle(DocumentsWriterPerThread dwpt)
        //{
        //    // don't recycle DWPT by default
        //}

        // you cannot subclass this without being in o.a.l.index package anyway, so
        // the class is already pkg-private... fix me: see LUCENE-4013
        public ThreadState GetAndLock(/* Thread requestingThread, DocumentsWriter documentsWriter // LUCENENET: Not referenced */)
        {
            ThreadState threadState = null;
            UninterruptableMonitor.Enter(this);
            try
            {
                for (;;)
                {
                    if (freeCount > 0)
                    {
                        // Important that we are LIFO here! This way if number of concurrent indexing threads was once high, but has now reduced, we only use a
                        // limited number of thread states:
                        threadState = freeList[freeCount - 1];
                        if (threadState.dwpt is null)
                        {
                            // This thread-state is not initialized, e.g. it
                            // was just flushed. See if we can instead find
                            // another free thread state that already has docs
                            // indexed. This way if incoming thread concurrency
                            // has decreased, we don't leave docs
                            // indefinitely buffered, tying up RAM.  This
                            // will instead get those thread states flushed,
                            // freeing up RAM for larger segment flushes:
                            for (int i = 0; i < freeCount; i++)
                            {
                                if (freeList[i].dwpt != null)
                                {
                                    // Use this one instead, and swap it with
                                    // the un-initialized one:
                                    ThreadState ts = freeList[i];
                                    freeList[i] = threadState;
                                    threadState = ts;
                                    break;
                                }
                            }
                        }

                        freeCount--;
                        break;
                    }
                    else if (NumThreadStatesActive < threadStates.Length)
                    {
                        // ThreadState is already locked before return by this method:
                        return NewThreadState();
                    }
                    else
                    {
                        // Wait until a thread state frees up:
                        try
                        {
                            UninterruptableMonitor.Wait(this);
                        }
                        catch (Exception ie) when (ie.IsInterruptedException())
                        {
                            throw new Util.ThreadInterruptedException(ie);
                        }
                    }
                }
            }
            finally
            {
                UninterruptableMonitor.Exit(this);
            }

            // This could take time, e.g. if the threadState is [briefly] checked for flushing:
            threadState.Lock();

            return threadState;
        }

        public void Release(ThreadState state)
        {
            state.Unlock();
            UninterruptableMonitor.Enter(this);
            try
            {
                Debug.Assert(freeCount < freeList.Length);
                freeList[freeCount++] = state;
                // In case any thread is waiting, wake one of them up since we just released a thread state; notify() should be sufficient but we do
                // notifyAll defensively:
                UninterruptableMonitor.PulseAll(this);
            }
            finally
            {
                UninterruptableMonitor.Exit(this);
            }
        }

        /// <summary>
        /// Returns the <i>i</i>th active <seealso cref="ThreadState"/> where <i>i</i> is the
        /// given ord.
        /// </summary>
        /// <param name="ord">
        ///          the ordinal of the <seealso cref="ThreadState"/> </param>
        /// <returns> the <i>i</i>th active <seealso cref="ThreadState"/> where <i>i</i> is the
        ///         given ord. </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal ThreadState GetThreadState(int ord)
        {
            return threadStates[ord];
        }

        /// <summary>
        /// Returns the <see cref="ThreadState"/> with the minimum estimated number of threads
        /// waiting to acquire its lock or <c>null</c> if no <see cref="ThreadState"/>
        /// is yet visible to the calling thread.
        /// </summary>
        internal ThreadState MinContendedThreadState()
        {
            ThreadState minThreadState = null;
            int limit = numThreadStatesActive;
            for (int i = 0; i < limit; i++)
            {
                ThreadState state = threadStates[i];
                if (minThreadState is null || state.QueueLength < minThreadState.QueueLength)
                {
                    minThreadState = state;
                }
            }
            return minThreadState;
        }

        /// <summary>
        /// Returns the number of currently deactivated <see cref="ThreadState"/> instances.
        /// A deactivated <see cref="ThreadState"/> should not be used for indexing anymore.
        /// </summary>
        /// <returns> the number of currently deactivated <see cref="ThreadState"/> instances. </returns>
        internal int NumDeactivatedThreadStates()
        {
            int count = 0;
            for (int i = 0; i < threadStates.Length; i++)
            {
                ThreadState threadState = threadStates[i];
                threadState.@Lock();
                try
                {
                    if (!threadState.isActive)
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
        /// Deactivates an active <see cref="ThreadState"/>. Inactive <see cref="ThreadState"/> can
        /// not be used for indexing anymore once they are deactivated. This method should only be used
        /// if the parent <see cref="DocumentsWriter"/> is closed or aborted.
        /// </summary>
        /// <param name="threadState"> the state to deactivate </param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void DeactivateThreadState(ThreadState threadState) // LUCENENET: CA1822: Mark members as static
        {
            if (Debugging.AssertsEnabled) Debugging.Assert(threadState.IsActive);
            threadState.Deactivate();
        }
    }
}