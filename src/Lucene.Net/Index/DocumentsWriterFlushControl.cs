using J2N.Runtime.CompilerServices;
using J2N.Threading.Atomic;
using Lucene.Net.Diagnostics;
using Lucene.Net.Support.Threading;
using System;
using System.Collections.Generic;
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

    using InfoStream = Lucene.Net.Util.InfoStream;
    using ThreadState = Lucene.Net.Index.DocumentsWriterPerThreadPool.ThreadState;

    /// <summary>
    /// This class controls <see cref="DocumentsWriterPerThread"/> flushing during
    /// indexing. It tracks the memory consumption per
    /// <see cref="DocumentsWriterPerThread"/> and uses a configured <see cref="flushPolicy"/> to
    /// decide if a <see cref="DocumentsWriterPerThread"/> must flush.
    /// <para/>
    /// In addition to the <see cref="flushPolicy"/> the flush control might set certain
    /// <see cref="DocumentsWriterPerThread"/> as flush pending iff a
    /// <see cref="DocumentsWriterPerThread"/> exceeds the
    /// <see cref="IndexWriterConfig.RAMPerThreadHardLimitMB"/> to prevent address
    /// space exhaustion.
    /// </summary>
    internal sealed class DocumentsWriterFlushControl
    {
        private readonly long hardMaxBytesPerDWPT;
        private long activeBytes = 0;
        private long flushBytes = 0;
        private volatile int numPending = 0;
        private int numDocsSinceStalled = 0; // only with assert
        internal readonly AtomicBoolean flushDeletes = new AtomicBoolean(false);
        private bool fullFlush = false;
        private readonly Queue<DocumentsWriterPerThread> flushQueue = new Queue<DocumentsWriterPerThread>();

        // only for safety reasons if a DWPT is close to the RAM limit
        private readonly LinkedList<BlockedFlush> blockedFlushes = new LinkedList<BlockedFlush>();

        private readonly IDictionary<DocumentsWriterPerThread, long> flushingWriters = new JCG.Dictionary<DocumentsWriterPerThread, long>(IdentityEqualityComparer<DocumentsWriterPerThread>.Default);

        internal double maxConfiguredRamBuffer = 0;
        internal long peakActiveBytes = 0; // only with assert
        internal long peakFlushBytes = 0; // only with assert
        internal long peakNetBytes = 0; // only with assert
        internal long peakDelta = 0; // only with assert
        internal readonly DocumentsWriterStallControl stallControl;
        private readonly DocumentsWriterPerThreadPool perThreadPool;
        private readonly FlushPolicy flushPolicy;
        private bool closed = false;
        private readonly DocumentsWriter documentsWriter;
        private readonly LiveIndexWriterConfig config;
        private readonly BufferedUpdatesStream bufferedUpdatesStream;
        private readonly InfoStream infoStream;

        internal DocumentsWriterFlushControl(DocumentsWriter documentsWriter, LiveIndexWriterConfig config, BufferedUpdatesStream bufferedUpdatesStream)
        {
            this.infoStream = config.InfoStream;
            this.stallControl = new DocumentsWriterStallControl();
            this.perThreadPool = documentsWriter.perThreadPool;
            this.flushPolicy = documentsWriter.flushPolicy;
            this.config = config;
            this.hardMaxBytesPerDWPT = config.RAMPerThreadHardLimitMB * 1024 * 1024;
            this.documentsWriter = documentsWriter;
            this.bufferedUpdatesStream = bufferedUpdatesStream;
        }

        public long ActiveBytes
        {
            get
            {
                UninterruptableMonitor.Enter(this);
                try
                {
                    return activeBytes;
                }
                finally
                {
                    UninterruptableMonitor.Exit(this);
                }
            }
        }

        public long FlushBytes
        {
            get
            {
                UninterruptableMonitor.Enter(this);
                try
                {
                    return flushBytes;
                }
                finally
                {
                    UninterruptableMonitor.Exit(this);
                }
            }
        }

        public long NetBytes
        {
            get
            {
                UninterruptableMonitor.Enter(this);
                try
                {
                    return flushBytes + activeBytes;
                }
                finally
                {
                    UninterruptableMonitor.Exit(this);
                }
            }
        }

        private long StallLimitBytes
        {
            get
            {
                double maxRamMB = config.RAMBufferSizeMB;
                return maxRamMB != IndexWriterConfig.DISABLE_AUTO_FLUSH ? (long)(2 * (maxRamMB * 1024 * 1024)) : long.MaxValue;
            }
        }

        private bool AssertMemory()
        {
            double maxRamMB = config.RAMBufferSizeMB;
            if (maxRamMB != IndexWriterConfig.DISABLE_AUTO_FLUSH)
            {
                // for this assert we must be tolerant to ram buffer changes!
                maxConfiguredRamBuffer = Math.Max(maxRamMB, maxConfiguredRamBuffer);
                long ram = flushBytes + activeBytes;
                long ramBufferBytes = (long)(maxConfiguredRamBuffer * 1024 * 1024);
                // take peakDelta into account - worst case is that all flushing, pending and blocked DWPT had maxMem and the last doc had the peakDelta

                // 2 * ramBufferBytes -> before we stall we need to cross the 2xRAM Buffer border this is still a valid limit
                // (numPending + numFlushingDWPT() + numBlockedFlushes()) * peakDelta) -> those are the total number of DWPT that are not active but not yet fully fluhsed
                // all of them could theoretically be taken out of the loop once they crossed the RAM buffer and the last document was the peak delta
                // (numDocsSinceStalled * peakDelta) -> at any given time there could be n threads in flight that crossed the stall control before we reached the limit and each of them could hold a peak document
                long expected = (2 * (ramBufferBytes)) + ((numPending + NumFlushingDWPT + NumBlockedFlushes) * peakDelta) + (numDocsSinceStalled * peakDelta);
                // the expected ram consumption is an upper bound at this point and not really the expected consumption
                if (peakDelta < (ramBufferBytes >> 1))
                {
                    /*
                     * if we are indexing with very low maxRamBuffer like 0.1MB memory can
                     * easily overflow if we check out some DWPT based on docCount and have
                     * several DWPT in flight indexing large documents (compared to the ram
                     * buffer). this means that those DWPT and their threads will not hit
                     * the stall control before asserting the memory which would in turn
                     * fail. To prevent this we only assert if the the largest document seen
                     * is smaller than the 1/2 of the maxRamBufferMB
                     */
                    if (Debugging.AssertsEnabled) Debugging.Assert(ram <= expected,
                        "actual mem: {0} byte, expected mem: {1}"
                        + " byte, flush mem: {2}, active mem: {3}"
                        + ", pending DWPT: {4}, flushing DWPT: {5}"
                        + ", blocked DWPT: {6}, peakDelta mem: {7} byte", ram, expected, flushBytes, activeBytes, numPending, NumFlushingDWPT, NumBlockedFlushes, peakDelta);
                }
            }
            return true;
        }

        private void CommitPerThreadBytes(ThreadState perThread)
        {
            long delta = perThread.dwpt.BytesUsed - perThread.bytesUsed;
            perThread.bytesUsed += delta;
            /*
             * We need to differentiate here if we are pending since setFlushPending
             * moves the perThread memory to the flushBytes and we could be set to
             * pending during a delete
             */
            if (perThread.flushPending)
            {
                flushBytes += delta;
            }
            else
            {
                activeBytes += delta;
            }
            if (Debugging.AssertsEnabled) Debugging.Assert(UpdatePeaks(delta));
        }

        // only for asserts
        private bool UpdatePeaks(long delta)
        {
            peakActiveBytes = Math.Max(peakActiveBytes, activeBytes);
            peakFlushBytes = Math.Max(peakFlushBytes, flushBytes);
            peakNetBytes = Math.Max(peakNetBytes, NetBytes);
            peakDelta = Math.Max(peakDelta, delta);

            return true;
        }

        internal DocumentsWriterPerThread DoAfterDocument(ThreadState perThread, bool isUpdate)
        {
            UninterruptableMonitor.Enter(this);
            try
            {
                try
                {
                    CommitPerThreadBytes(perThread);
                    if (!perThread.flushPending)
                    {
                        if (isUpdate)
                        {
                            flushPolicy.OnUpdate(this, perThread);
                        }
                        else
                        {
                            flushPolicy.OnInsert(this, perThread);
                        }
                        if (!perThread.flushPending && perThread.bytesUsed > hardMaxBytesPerDWPT)
                        {
                            // Safety check to prevent a single DWPT exceeding its RAM limit. this
                            // is super important since we can not address more than 2048 MB per DWPT
                            SetFlushPending(perThread);
                        }
                    }
                    DocumentsWriterPerThread flushingDWPT;
                    if (fullFlush)
                    {
                        if (perThread.flushPending)
                        {
                            CheckoutAndBlock(perThread);
                            flushingDWPT = NextPendingFlush();
                        }
                        else
                        {
                            flushingDWPT = null;
                        }
                    }
                    else
                    {
                        flushingDWPT = TryCheckoutForFlush(perThread);
                    }
                    return flushingDWPT;
                }
                finally
                {
                    bool stalled = UpdateStallState();
                    if (Debugging.AssertsEnabled) Debugging.Assert(AssertNumDocsSinceStalled(stalled) && AssertMemory());
                }
            }
            finally
            {
                UninterruptableMonitor.Exit(this);
            }
        }

        private bool AssertNumDocsSinceStalled(bool stalled)
        {
            /*
             *  updates the number of documents "finished" while we are in a stalled state.
             *  this is important for asserting memory upper bounds since it corresponds
             *  to the number of threads that are in-flight and crossed the stall control
             *  check before we actually stalled.
             *  see #assertMemory()
             */
            if (stalled)
            {
                numDocsSinceStalled++;
            }
            else
            {
                numDocsSinceStalled = 0;
            }
            return true;
        }

        internal void DoAfterFlush(DocumentsWriterPerThread dwpt)
        {
            UninterruptableMonitor.Enter(this);
            try
            {
                if (Debugging.AssertsEnabled) Debugging.Assert(flushingWriters.ContainsKey(dwpt));
                try
                {
                    long bytes = flushingWriters[dwpt];
                    flushingWriters.Remove(dwpt);
                    flushBytes -= bytes;
                    //perThreadPool.Recycle(dwpt); // LUCENENET: This is a no-op method in Lucene and it cannot be overridden
                    if (Debugging.AssertsEnabled) Debugging.Assert(AssertMemory());
                }
                finally
                {
                    try
                    {
                        UpdateStallState();
                    }
                    finally
                    {
                        UninterruptableMonitor.PulseAll(this);
                    }
                }
            }
            finally
            {
                UninterruptableMonitor.Exit(this);
            }
        }

        private bool UpdateStallState()
        {
            if (Debugging.AssertsEnabled) Debugging.Assert(UninterruptableMonitor.IsEntered(this));
            long limit = StallLimitBytes;
            /*
             * we block indexing threads if net byte grows due to slow flushes
             * yet, for small ram buffers and large documents we can easily
             * reach the limit without any ongoing flushes. we need to ensure
             * that we don't stall/block if an ongoing or pending flush can
             * not free up enough memory to release the stall lock.
             */
            bool stall = ((activeBytes + flushBytes) > limit) && (activeBytes < limit) && !closed;
            stallControl.UpdateStalled(stall);
            return stall;
        }

        public void WaitForFlush()
        {
            UninterruptableMonitor.Enter(this);
            try
            {
                while (flushingWriters.Count != 0)
                {
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
            finally
            {
                UninterruptableMonitor.Exit(this);
            }
        }

        /// <summary>
        /// Sets flush pending state on the given <see cref="ThreadState"/>. The
        /// <see cref="ThreadState"/> must have indexed at least on <see cref="Documents.Document"/> and must not be
        /// already pending.
        /// </summary>
        public void SetFlushPending(ThreadState perThread)
        {
            UninterruptableMonitor.Enter(this);
            try
            {
                if (Debugging.AssertsEnabled) Debugging.Assert(!perThread.flushPending);
                if (perThread.dwpt.NumDocsInRAM > 0)
                {
                    perThread.flushPending = true; // write access synced
                    long bytes = perThread.bytesUsed;
                    flushBytes += bytes;
                    activeBytes -= bytes;
                    numPending++; // write access synced
                    if (Debugging.AssertsEnabled) Debugging.Assert(AssertMemory());
                } // don't assert on numDocs since we could hit an abort excp. while selecting that dwpt for flushing
            }
            finally
            {
                UninterruptableMonitor.Exit(this);
            }
        }

        internal void DoOnAbort(ThreadState state)
        {
            UninterruptableMonitor.Enter(this);
            try
            {
                try
                {
                    if (state.flushPending)
                    {
                        flushBytes -= state.bytesUsed;
                    }
                    else
                    {
                        activeBytes -= state.bytesUsed;
                    }
                    if (Debugging.AssertsEnabled) Debugging.Assert(AssertMemory());
                    // Take it out of the loop this DWPT is stale
                    DocumentsWriterPerThreadPool.Reset(state, closed); // LUCENENET specific - made static per CA1822
                }
                finally
                {
                    UpdateStallState();
                }
            }
            finally
            {
                UninterruptableMonitor.Exit(this);
            }
        }

        internal DocumentsWriterPerThread TryCheckoutForFlush(ThreadState perThread)
        {
            UninterruptableMonitor.Enter(this);
            try
            {
                if (Debugging.AssertsEnabled) Debugging.Assert(perThread.IsHeldByCurrentThread); // LUCENENET specific: Since .NET Core doesn't use unfair locking, we need to ensure the current thread has a lock before calling InternalTryCheckoutForFlush.
                return perThread.flushPending ? InternalTryCheckOutForFlush(perThread) : null;
            }
            finally
            {
                UninterruptableMonitor.Exit(this);
            }
        }

        private void CheckoutAndBlock(ThreadState perThread)
        {
            perThread.@Lock();
            try
            {
                if (Debugging.AssertsEnabled)
                {
                    Debugging.Assert(perThread.flushPending, "can not block non-pending threadstate");
                    Debugging.Assert(fullFlush, "can not block if fullFlush == false");
                }
                DocumentsWriterPerThread dwpt;
                long bytes = perThread.bytesUsed;
                dwpt = DocumentsWriterPerThreadPool.Reset(perThread, closed); // LUCENENET specific - made method static per CA1822
                numPending--;
                blockedFlushes.AddLast(new BlockedFlush(dwpt, bytes));
            }
            finally
            {
                perThread.Unlock();
            }
        }

        private DocumentsWriterPerThread InternalTryCheckOutForFlush(ThreadState perThread)
        {
            if (Debugging.AssertsEnabled)
            {
                // LUCENENET specific - Since we need to mimic the unfair behavior of ReentrantLock, we need to ensure that all threads that enter here hold the lock.
                Debugging.Assert(perThread.IsHeldByCurrentThread);
                Debugging.Assert(UninterruptableMonitor.IsEntered(this));
                Debugging.Assert(perThread.flushPending);
            }
            try
            {
                // LUCENENET specific - We removed the call to perThread.TryLock() and the try-finally below as they are no longer needed.

                // We are pending so all memory is already moved to flushBytes
                if (perThread.IsInitialized)
                {
                    if (Debugging.AssertsEnabled) Debugging.Assert(perThread.IsHeldByCurrentThread);
                    DocumentsWriterPerThread dwpt;
                    long bytes = perThread.bytesUsed; // do that before
                    // replace!
                    dwpt = DocumentsWriterPerThreadPool.Reset(perThread, closed); // LUCENENET specific - made method static per CA1822
                    if (Debugging.AssertsEnabled) Debugging.Assert(!flushingWriters.ContainsKey(dwpt), "DWPT is already flushing");
                    // Record the flushing DWPT to reduce flushBytes in doAfterFlush
                    flushingWriters[dwpt] = bytes;
                    numPending--; // write access synced
                    return dwpt;
                }
                return null;
            }
            finally
            {
                UpdateStallState();
            }
        }

        public override string ToString()
        {
            return "DocumentsWriterFlushControl [activeBytes=" + activeBytes + ", flushBytes=" + flushBytes + "]";
        }

        internal DocumentsWriterPerThread NextPendingFlush()
        {
            int numPending;
            bool fullFlush;
            UninterruptableMonitor.Enter(this);
            try
            {
                DocumentsWriterPerThread poll;
                if (flushQueue.Count > 0 && (poll = flushQueue.Dequeue()) != null)
                {
                    UpdateStallState();
                    return poll;
                }
                fullFlush = this.fullFlush;
                numPending = this.numPending;
            }
            finally
            {
                UninterruptableMonitor.Exit(this);
            }
            if (numPending > 0 && !fullFlush) // don't check if we are doing a full flush
            {
                int limit = perThreadPool.NumThreadStatesActive;
                for (int i = 0; i < limit && numPending > 0; i++)
                {
                    ThreadState next = perThreadPool.GetThreadState(i);
                    if (next.flushPending && next.TryLock()) // LUCENENET specific: Since .NET Core 2+ uses fair locking, we need to ensure we have a lock before calling InternalTryCheckoutForFlush. See #
                    {
                        try
                        {
                            DocumentsWriterPerThread dwpt = TryCheckoutForFlush(next);
                            if (dwpt != null)
                            {
                                return dwpt;
                            }
                        }
                        finally
                        {
                            next.Unlock();
                        }
                    }
                }
            }
            return null;
        }

        internal void SetClosed()
        {
            UninterruptableMonitor.Enter(this);
            try
            {
                // set by DW to signal that we should not release new DWPT after close
                if (!closed)
                {
                    this.closed = true;
                    perThreadPool.DeactivateUnreleasedStates();
                }
            }
            finally
            {
                UninterruptableMonitor.Exit(this);
            }
        }

        /// <summary>
        /// Returns an iterator that provides access to all currently active <see cref="ThreadState"/>s
        /// </summary>
        public IEnumerator<ThreadState> AllActiveThreadStates()
        {
            return GetPerThreadsIterator(perThreadPool.NumThreadStatesActive);
        }

        private IEnumerator<ThreadState> GetPerThreadsIterator(int upto)
        {
            return new EnumeratorAnonymousClass(this, upto);
        }

        private sealed class EnumeratorAnonymousClass : IEnumerator<ThreadState>
        {
            private readonly DocumentsWriterFlushControl outerInstance;
            private ThreadState current;
            private readonly int upto;
            private int i;

            public EnumeratorAnonymousClass(DocumentsWriterFlushControl outerInstance, int upto)
            {
                this.outerInstance = outerInstance;
                this.upto = upto;
                i = 0;
            }

            public ThreadState Current => current;

            public void Dispose()
            {
                // LUCENENET: Intentionally blank
            }

            object System.Collections.IEnumerator.Current => Current;

            public bool MoveNext()
            {
                if (i < upto)
                {
                    current = outerInstance.perThreadPool.GetThreadState(i++);
                    return true;
                }

                return false;
            }

            public void Reset()
            {
                throw UnsupportedOperationException.Create();
            }
        }

        internal void DoOnDelete()
        {
            UninterruptableMonitor.Enter(this);
            try
            {
                // pass null this is a global delete no update
                flushPolicy.OnDelete(this, null);
            }
            finally
            {
                UninterruptableMonitor.Exit(this);
            }
        }

        /// <summary>
        /// Returns the number of delete terms in the global pool
        /// </summary>
        public int NumGlobalTermDeletes => documentsWriter.deleteQueue.NumGlobalTermDeletes + bufferedUpdatesStream.NumTerms;

        public long DeleteBytesUsed => documentsWriter.deleteQueue.BytesUsed + bufferedUpdatesStream.BytesUsed;

        internal int NumFlushingDWPT
        {
            get
            {
                UninterruptableMonitor.Enter(this);
                try
                {
                    return flushingWriters.Count;
                }
                finally
                {
                    UninterruptableMonitor.Exit(this);
                }
            }
        }

        public bool GetAndResetApplyAllDeletes() 
        {
            return flushDeletes.GetAndSet(false);
        }

        public void SetApplyAllDeletes()
        {
            flushDeletes.Value = true;
        }

        internal int NumActiveDWPT => this.perThreadPool.NumThreadStatesActive;

        internal ThreadState ObtainAndLock()
        {
            ThreadState perThread = perThreadPool.GetAndLock(/* Thread.CurrentThread, documentsWriter // LUCENENET: Not used */);
            bool success = false;
            try
            {
                if (perThread.IsInitialized && perThread.dwpt.deleteQueue != documentsWriter.deleteQueue)
                {
                    // There is a flush-all in process and this DWPT is
                    // now stale -- enroll it for flush and try for
                    // another DWPT:
                    AddFlushableState(perThread);
                }
                success = true;
                // simply return the ThreadState even in a flush all case sine we already hold the lock
                return perThread;
            }
            finally
            {
                if (!success) // make sure we unlock if this fails
                {
                    perThreadPool.Release(perThread);
                }
            }
        }

        internal void MarkForFullFlush()
        {
            DocumentsWriterDeleteQueue flushingQueue;
            UninterruptableMonitor.Enter(this);
            try
            {
                if (Debugging.AssertsEnabled)
                {
                    Debugging.Assert(!fullFlush, "called DWFC#markForFullFlush() while full flush is still running");
                    Debugging.Assert(fullFlushBuffer.Count == 0,"full flush buffer should be empty: {0}", fullFlushBuffer);
                }
                fullFlush = true;
                flushingQueue = documentsWriter.deleteQueue;
                // Set a new delete queue - all subsequent DWPT will use this queue until
                // we do another full flush
                DocumentsWriterDeleteQueue newQueue = new DocumentsWriterDeleteQueue(flushingQueue.generation + 1);
                documentsWriter.deleteQueue = newQueue;
            }
            finally
            {
                UninterruptableMonitor.Exit(this);
            }
            int limit = perThreadPool.NumThreadStatesActive;
            for (int i = 0; i < limit; i++)
            {
                ThreadState next = perThreadPool.GetThreadState(i);
                next.@Lock();
                try
                {
                    if (!next.IsInitialized)
                    {
                        if (closed && next.IsActive)
                        {
                            DocumentsWriterPerThreadPool.DeactivateThreadState(next); // LUCENENET specific - made method static per CA1822
                        }
                        continue;
                    }
                    if (Debugging.AssertsEnabled) Debugging.Assert(next.dwpt.deleteQueue == flushingQueue
                        || next.dwpt.deleteQueue == documentsWriter.deleteQueue,
                        " flushingQueue: {0} currentqueue: {1} perThread queue: {2} numDocsInRam: {3}",
                        flushingQueue, documentsWriter.deleteQueue, next.dwpt.deleteQueue, next.dwpt.NumDocsInRAM);
                    if (next.dwpt.deleteQueue != flushingQueue)
                    {
                        // this one is already a new DWPT
                        continue;
                    }
                    AddFlushableState(next);
                }
                finally
                {
                    next.Unlock();
                }
            }
            UninterruptableMonitor.Enter(this);
            try
            {
                /* make sure we move all DWPT that are where concurrently marked as
                 * pending and moved to blocked are moved over to the flushQueue. There is
                 * a chance that this happens since we marking DWPT for full flush without
                 * blocking indexing.*/
                PruneBlockedQueue(flushingQueue);
                if (Debugging.AssertsEnabled) Debugging.Assert(AssertBlockedFlushes(documentsWriter.deleteQueue));
                //FlushQueue.AddAll(FullFlushBuffer);
                foreach (var dwpt in fullFlushBuffer)
                {
                    flushQueue.Enqueue(dwpt);
                }
                fullFlushBuffer.Clear();
                UpdateStallState();
            }
            finally
            {
                UninterruptableMonitor.Exit(this);
            }
            if (Debugging.AssertsEnabled) Debugging.Assert(AssertActiveDeleteQueue(documentsWriter.deleteQueue));
        }

        private bool AssertActiveDeleteQueue(DocumentsWriterDeleteQueue queue)
        {
            int limit = perThreadPool.NumThreadStatesActive;
            for (int i = 0; i < limit; i++)
            {
                ThreadState next = perThreadPool.GetThreadState(i);
                next.@Lock();
                try
                {
                    if (Debugging.AssertsEnabled) Debugging.Assert(!next.IsInitialized || next.dwpt.deleteQueue == queue,"isInitialized: {0} numDocs: {1}", next.IsInitialized, (next.IsInitialized ? next.dwpt.NumDocsInRAM : 0));
                }
                finally
                {
                    next.Unlock();
                }
            }
            return true;
        }

        private readonly IList<DocumentsWriterPerThread> fullFlushBuffer = new JCG.List<DocumentsWriterPerThread>();

        internal void AddFlushableState(ThreadState perThread)
        {
            if (infoStream.IsEnabled("DWFC"))
            {
                infoStream.Message("DWFC", "addFlushableState " + perThread.dwpt);
            }
            DocumentsWriterPerThread dwpt = perThread.dwpt;
            if (Debugging.AssertsEnabled)
            {
                Debugging.Assert(perThread.IsHeldByCurrentThread);
                Debugging.Assert(perThread.IsInitialized);
                Debugging.Assert(fullFlush);
                Debugging.Assert(dwpt.deleteQueue != documentsWriter.deleteQueue);
            }
            if (dwpt.NumDocsInRAM > 0)
            {
                UninterruptableMonitor.Enter(this);
                try
                {
                    if (!perThread.flushPending)
                    {
                        SetFlushPending(perThread);
                    }
                    DocumentsWriterPerThread flushingDWPT = InternalTryCheckOutForFlush(perThread);
                    if (Debugging.AssertsEnabled)
                    {
                        Debugging.Assert(flushingDWPT != null, "DWPT must never be null here since we hold the lock and it holds documents");
                        Debugging.Assert(dwpt == flushingDWPT, "flushControl returned different DWPT");
                    }
                    fullFlushBuffer.Add(flushingDWPT);
                }
                finally
                {
                    UninterruptableMonitor.Exit(this);
                }
            }
            else
            {
                DocumentsWriterPerThreadPool.Reset(perThread, closed); // make this state inactive // LUCENENET specific - made method static per CA1822
            }
        }

        /// <summary>
        /// Prunes the blockedQueue by removing all DWPT that are associated with the given flush queue.
        /// </summary>
        private void PruneBlockedQueue(DocumentsWriterDeleteQueue flushingQueue)
        {
            var node = blockedFlushes.First;
            while (node != null)
            {
                var nextNode = node.Next;
                BlockedFlush blockedFlush = node.Value;
                if (blockedFlush.Dwpt.deleteQueue == flushingQueue)
                {
                    blockedFlushes.Remove(node);
                    if (Debugging.AssertsEnabled) Debugging.Assert(!flushingWriters.ContainsKey(blockedFlush.Dwpt), "DWPT is already flushing");
                    // Record the flushing DWPT to reduce flushBytes in doAfterFlush
                    flushingWriters[blockedFlush.Dwpt] = blockedFlush.Bytes;
                    // don't decr pending here - its already done when DWPT is blocked
                    flushQueue.Enqueue(blockedFlush.Dwpt);
                }
                node = nextNode;
            }
        }

        internal void FinishFullFlush()
        {
            UninterruptableMonitor.Enter(this);
            try
            {
                if (Debugging.AssertsEnabled)
                {
                    Debugging.Assert(fullFlush);
                    Debugging.Assert(flushQueue.Count == 0);
                    Debugging.Assert(flushingWriters.Count == 0);
                }
                try
                {
                    if (blockedFlushes.Count > 0)
                    {
                        if (Debugging.AssertsEnabled) Debugging.Assert(AssertBlockedFlushes(documentsWriter.deleteQueue));
                        PruneBlockedQueue(documentsWriter.deleteQueue);
                        if (Debugging.AssertsEnabled) Debugging.Assert(blockedFlushes.Count == 0);
                    }
                }
                finally
                {
                    fullFlush = false;
                    UpdateStallState();
                }
            }
            finally
            {
                UninterruptableMonitor.Exit(this);
            }
        }

        internal bool AssertBlockedFlushes(DocumentsWriterDeleteQueue flushingQueue)
        {
            foreach (BlockedFlush blockedFlush in blockedFlushes)
            {
                if (Debugging.AssertsEnabled) Debugging.Assert(blockedFlush.Dwpt.deleteQueue == flushingQueue);
            }
            return true;
        }

        internal void AbortFullFlushes(ISet<string> newFiles)
        {
            UninterruptableMonitor.Enter(this);
            try
            {
                try
                {
                    AbortPendingFlushes(newFiles);
                }
                finally
                {
                    fullFlush = false;
                }
            }
            finally
            {
                UninterruptableMonitor.Exit(this);
            }
        }

        internal void AbortPendingFlushes(ISet<string> newFiles)
        {
            UninterruptableMonitor.Enter(this);
            try
            {
                try
                {
                    foreach (DocumentsWriterPerThread dwpt in flushQueue)
                    {
                        try
                        {
                            documentsWriter.SubtractFlushedNumDocs(dwpt.NumDocsInRAM);
                            dwpt.Abort(newFiles);
                        }
                        catch (Exception ex) when (ex.IsThrowable())
                        {
                            // ignore - keep on aborting the flush queue
                        }
                        finally
                        {
                            DoAfterFlush(dwpt);
                        }
                    }
                    foreach (BlockedFlush blockedFlush in blockedFlushes)
                    {
                        try
                        {
                            flushingWriters[blockedFlush.Dwpt] = blockedFlush.Bytes;
                            documentsWriter.SubtractFlushedNumDocs(blockedFlush.Dwpt.NumDocsInRAM);
                            blockedFlush.Dwpt.Abort(newFiles);
                        }
                        catch (Exception ex) when (ex.IsThrowable())
                        {
                            // ignore - keep on aborting the blocked queue
                        }
                        finally
                        {
                            DoAfterFlush(blockedFlush.Dwpt);
                        }
                    }
                }
                finally
                {
                    flushQueue.Clear();
                    blockedFlushes.Clear();
                    UpdateStallState();
                }
            }
            finally
            {
                UninterruptableMonitor.Exit(this);
            }
        }

        /// <summary>
        /// Returns <c>true</c> if a full flush is currently running
        /// </summary>
        internal bool IsFullFlush
        {
            get
            {
                UninterruptableMonitor.Enter(this);
                try
                {
                    return fullFlush;
                }
                finally
                {
                    UninterruptableMonitor.Exit(this);
                }
            }
        }

        /// <summary>
        /// Returns the number of flushes that are already checked out but not yet
        /// actively flushing
        /// </summary>
        internal int NumQueuedFlushes
        {
            get
            {
                UninterruptableMonitor.Enter(this);
                try
                {
                    return flushQueue.Count;
                }
                finally
                {
                    UninterruptableMonitor.Exit(this);
                }
            }
        }

        /// <summary>
        /// Returns the number of flushes that are checked out but not yet available
        /// for flushing. This only applies during a full flush if a DWPT needs
        /// flushing but must not be flushed until the full flush has finished.
        /// </summary>
        internal int NumBlockedFlushes
        {
            get
            {
                UninterruptableMonitor.Enter(this);
                try
                {
                    return blockedFlushes.Count;
                }
                finally
                {
                    UninterruptableMonitor.Exit(this);
                }
            }
        }

        private class BlockedFlush
        {
            internal DocumentsWriterPerThread Dwpt { get; private set; }
            internal long Bytes { get; private set; }

            internal BlockedFlush(DocumentsWriterPerThread dwpt, long bytes)
                : base()
            {
                this.Dwpt = dwpt;
                this.Bytes = bytes;
            }
        }

        /// <summary>
        /// This method will block if too many DWPT are currently flushing and no
        /// checked out DWPT are available
        /// </summary>
        internal void WaitIfStalled()
        {
            if (infoStream.IsEnabled("DWFC"))
            {
                infoStream.Message("DWFC", "waitIfStalled: numFlushesPending: " + flushQueue.Count + " netBytes: " + NetBytes + " flushBytes: " + FlushBytes + " fullFlush: " + fullFlush);
            }
            stallControl.WaitIfStalled();
        }

        /// <summary>
        /// Returns <c>true</c> iff stalled
        /// </summary>
        internal bool AnyStalledThreads()
        {
            return stallControl.AnyStalledThreads();
        }

        /// <summary>
        /// Returns the <see cref="IndexWriter"/> <see cref="Util.InfoStream"/>
        /// </summary>
        public InfoStream InfoStream => infoStream;
    }
}