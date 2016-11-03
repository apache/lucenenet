using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace Lucene.Net.Index
{
    using Lucene.Net.Support;
    using InfoStream = Lucene.Net.Util.InfoStream;

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

    using ThreadState = Lucene.Net.Index.DocumentsWriterPerThreadPool.ThreadState;

    /// <summary>
    /// this class controls <seealso cref="DocumentsWriterPerThread"/> flushing during
    /// indexing. It tracks the memory consumption per
    /// <seealso cref="DocumentsWriterPerThread"/> and uses a configured <seealso cref="FlushPolicy"/> to
    /// decide if a <seealso cref="DocumentsWriterPerThread"/> must flush.
    /// <p>
    /// In addition to the <seealso cref="FlushPolicy"/> the flush control might set certain
    /// <seealso cref="DocumentsWriterPerThread"/> as flush pending iff a
    /// <seealso cref="DocumentsWriterPerThread"/> exceeds the
    /// <seealso cref="IndexWriterConfig#getRAMPerThreadHardLimitMB()"/> to prevent address
    /// space exhaustion.
    /// </summary>
    public sealed class DocumentsWriterFlushControl
    {
        private readonly long HardMaxBytesPerDWPT;
        private long ActiveBytes_Renamed = 0;
        private long FlushBytes_Renamed = 0;
        private volatile int NumPending = 0;
        private int NumDocsSinceStalled = 0; // only with assert
        internal readonly AtomicBoolean FlushDeletes = new AtomicBoolean(false);
        private bool FullFlush_Renamed = false;
        private readonly Queue<DocumentsWriterPerThread> FlushQueue = new Queue<DocumentsWriterPerThread>();

        // only for safety reasons if a DWPT is close to the RAM limit
        private readonly LinkedList<BlockedFlush> BlockedFlushes = new LinkedList<BlockedFlush>();

        private readonly IdentityHashMap<DocumentsWriterPerThread, long?> FlushingWriters = new IdentityHashMap<DocumentsWriterPerThread, long?>();

        internal double MaxConfiguredRamBuffer = 0;
        internal long PeakActiveBytes = 0; // only with assert
        internal long PeakFlushBytes = 0; // only with assert
        internal long PeakNetBytes = 0; // only with assert
        internal long PeakDelta = 0; // only with assert
        internal readonly DocumentsWriterStallControl StallControl;
        private readonly DocumentsWriterPerThreadPool PerThreadPool;
        private readonly FlushPolicy FlushPolicy;
        private bool Closed = false;
        private readonly DocumentsWriter DocumentsWriter;
        private readonly LiveIndexWriterConfig Config;
        private readonly BufferedUpdatesStream BufferedUpdatesStream;
        private readonly InfoStream InfoStream_Renamed;

        internal DocumentsWriterFlushControl(DocumentsWriter documentsWriter, LiveIndexWriterConfig config, BufferedUpdatesStream bufferedUpdatesStream)
        {
            this.InfoStream_Renamed = config.InfoStream;
            this.StallControl = new DocumentsWriterStallControl();
            this.PerThreadPool = documentsWriter.PerThreadPool;
            this.FlushPolicy = documentsWriter.FlushPolicy;
            this.Config = config;
            this.HardMaxBytesPerDWPT = config.RAMPerThreadHardLimitMB * 1024 * 1024;
            this.DocumentsWriter = documentsWriter;
            this.BufferedUpdatesStream = bufferedUpdatesStream;
        }

        public long ActiveBytes()
        {
            lock (this)
            {
                return ActiveBytes_Renamed;
            }
        }

        public long FlushBytes()
        {
            lock (this)
            {
                return FlushBytes_Renamed;
            }
        }

        public long NetBytes()
        {
            lock (this)
            {
                return FlushBytes_Renamed + ActiveBytes_Renamed;
            }
        }

        private long StallLimitBytes()
        {
            double maxRamMB = Config.RAMBufferSizeMB;
            return maxRamMB != IndexWriterConfig.DISABLE_AUTO_FLUSH ? (long)(2 * (maxRamMB * 1024 * 1024)) : long.MaxValue;
        }

        private bool AssertMemory()
        {
            double maxRamMB = Config.RAMBufferSizeMB;
            if (maxRamMB != IndexWriterConfig.DISABLE_AUTO_FLUSH)
            {
                // for this assert we must be tolerant to ram buffer changes!
                MaxConfiguredRamBuffer = Math.Max(maxRamMB, MaxConfiguredRamBuffer);
                long ram = FlushBytes_Renamed + ActiveBytes_Renamed;
                long ramBufferBytes = (long)(MaxConfiguredRamBuffer * 1024 * 1024);
                // take peakDelta into account - worst case is that all flushing, pending and blocked DWPT had maxMem and the last doc had the peakDelta

                // 2 * ramBufferBytes -> before we stall we need to cross the 2xRAM Buffer border this is still a valid limit
                // (numPending + numFlushingDWPT() + numBlockedFlushes()) * peakDelta) -> those are the total number of DWPT that are not active but not yet fully fluhsed
                // all of them could theoretically be taken out of the loop once they crossed the RAM buffer and the last document was the peak delta
                // (numDocsSinceStalled * peakDelta) -> at any given time there could be n threads in flight that crossed the stall control before we reached the limit and each of them could hold a peak document
                long expected = (2 * (ramBufferBytes)) + ((NumPending + NumFlushingDWPT() + NumBlockedFlushes()) * PeakDelta) + (NumDocsSinceStalled * PeakDelta);
                // the expected ram consumption is an upper bound at this point and not really the expected consumption
                if (PeakDelta < (ramBufferBytes >> 1))
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
                    Debug.Assert(ram <= expected, "actual mem: " + ram + " byte, expected mem: " + expected + " byte, flush mem: " + FlushBytes_Renamed + ", active mem: " + ActiveBytes_Renamed + ", pending DWPT: " + NumPending + ", flushing DWPT: " + NumFlushingDWPT() + ", blocked DWPT: " + NumBlockedFlushes() + ", peakDelta mem: " + PeakDelta + " byte");
                }
            }
            return true;
        }

        private void CommitPerThreadBytes(ThreadState perThread)
        {
            long delta = perThread.Dwpt.BytesUsed() - perThread.BytesUsed;
            perThread.BytesUsed += delta;
            /*
             * We need to differentiate here if we are pending since setFlushPending
             * moves the perThread memory to the flushBytes and we could be set to
             * pending during a delete
             */
            if (perThread.FlushPending_Renamed)
            {
                FlushBytes_Renamed += delta;
            }
            else
            {
                ActiveBytes_Renamed += delta;
            }
            Debug.Assert(UpdatePeaks(delta));
        }

        // only for asserts
        private bool UpdatePeaks(long delta)
        {
            PeakActiveBytes = Math.Max(PeakActiveBytes, ActiveBytes_Renamed);
            PeakFlushBytes = Math.Max(PeakFlushBytes, FlushBytes_Renamed);
            PeakNetBytes = Math.Max(PeakNetBytes, NetBytes());
            PeakDelta = Math.Max(PeakDelta, delta);

            return true;
        }

        internal DocumentsWriterPerThread DoAfterDocument(ThreadState perThread, bool isUpdate)
        {
            lock (this)
            {
                try
                {
                    CommitPerThreadBytes(perThread);
                    if (!perThread.FlushPending_Renamed)
                    {
                        if (isUpdate)
                        {
                            FlushPolicy.OnUpdate(this, perThread);
                        }
                        else
                        {
                            FlushPolicy.OnInsert(this, perThread);
                        }
                        if (!perThread.FlushPending_Renamed && perThread.BytesUsed > HardMaxBytesPerDWPT)
                        {
                            // Safety check to prevent a single DWPT exceeding its RAM limit. this
                            // is super important since we can not address more than 2048 MB per DWPT
                            FlushPending = perThread;
                        }
                    }
                    DocumentsWriterPerThread flushingDWPT;
                    if (FullFlush_Renamed)
                    {
                        if (perThread.FlushPending_Renamed)
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
                    Debug.Assert(AssertNumDocsSinceStalled(stalled) && AssertMemory());
                }
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
                NumDocsSinceStalled++;
            }
            else
            {
                NumDocsSinceStalled = 0;
            }
            return true;
        }

        internal void DoAfterFlush(DocumentsWriterPerThread dwpt)
        {
            lock (this)
            {
                Debug.Assert(FlushingWriters.ContainsKey(dwpt));
                try
                {
                    long? bytes = FlushingWriters[dwpt];
                    FlushingWriters.Remove(dwpt);
                    FlushBytes_Renamed -= (long)bytes;
                    PerThreadPool.Recycle(dwpt);
                    Debug.Assert(AssertMemory());
                }
                finally
                {
                    try
                    {
                        UpdateStallState();
                    }
                    finally
                    {
                        Monitor.PulseAll(this);
                    }
                }
            }
        }

        private bool UpdateStallState()
        {
            //Debug.Assert(Thread.holdsLock(this));
            long limit = StallLimitBytes();
            /*
             * we block indexing threads if net byte grows due to slow flushes
             * yet, for small ram buffers and large documents we can easily
             * reach the limit without any ongoing flushes. we need to ensure
             * that we don't stall/block if an ongoing or pending flush can
             * not free up enough memory to release the stall lock.
             */
            bool stall = ((ActiveBytes_Renamed + FlushBytes_Renamed) > limit) && (ActiveBytes_Renamed < limit) && !Closed;
            StallControl.UpdateStalled(stall);
            return stall;
        }

        public void WaitForFlush()
        {
             lock (this)
            {
                while (FlushingWriters.Count != 0)
                {
#if !NETSTANDARD
                    try
                    {
#endif
                    Monitor.Wait(this);
#if !NETSTANDARD
                    }
                    catch (ThreadInterruptedException e)
                    {
                        throw new ThreadInterruptedException("Thread Interrupted Exception", e);
                    }
#endif
                }
            }
        }

        /// <summary>
        /// Sets flush pending state on the given <seealso cref="ThreadState"/>. The
        /// <seealso cref="ThreadState"/> must have indexed at least on Document and must not be
        /// already pending.
        /// </summary>
        public ThreadState FlushPending
        {
            set
            {
                lock (this)
                {
                    Debug.Assert(!value.FlushPending_Renamed);
                    if (value.Dwpt.NumDocsInRAM > 0)
                    {
                        value.FlushPending_Renamed = true; // write access synced
                        long bytes = value.BytesUsed;
                        FlushBytes_Renamed += bytes;
                        ActiveBytes_Renamed -= bytes;
                        NumPending++; // write access synced
                        Debug.Assert(AssertMemory());
                    } // don't assert on numDocs since we could hit an abort excp. while selecting that dwpt for flushing
                }
            }
        }

        internal void DoOnAbort(ThreadState state)
        {
            lock (this)
            {
                try
                {
                    if (state.FlushPending_Renamed)
                    {
                        FlushBytes_Renamed -= state.BytesUsed;
                    }
                    else
                    {
                        ActiveBytes_Renamed -= state.BytesUsed;
                    }
                    Debug.Assert(AssertMemory());
                    // Take it out of the loop this DWPT is stale
                    PerThreadPool.Reset(state, Closed);
                }
                finally
                {
                    UpdateStallState();
                }
            }
        }

        internal DocumentsWriterPerThread TryCheckoutForFlush(ThreadState perThread)
        {
            lock (this)
            {
                return perThread.FlushPending_Renamed ? InternalTryCheckOutForFlush(perThread) : null;
            }
        }

        private void CheckoutAndBlock(ThreadState perThread)
        {
            perThread.@Lock();
            try
            {
                Debug.Assert(perThread.FlushPending_Renamed, "can not block non-pending threadstate");
                Debug.Assert(FullFlush_Renamed, "can not block if fullFlush == false");
                DocumentsWriterPerThread dwpt;
                long bytes = perThread.BytesUsed;
                dwpt = PerThreadPool.Reset(perThread, Closed);
                NumPending--;
                BlockedFlushes.AddLast(new BlockedFlush(dwpt, bytes));
            }
            finally
            {
                perThread.Unlock();
            }
        }

        private DocumentsWriterPerThread InternalTryCheckOutForFlush(ThreadState perThread)
        {
            //Debug.Assert(Thread.HoldsLock(this));
            Debug.Assert(perThread.FlushPending_Renamed);
            try
            {
                // We are pending so all memory is already moved to flushBytes
                if (perThread.TryLock())
                {
                    try
                    {
                        if (perThread.Initialized)
                        {
                            //Debug.Assert(perThread.HeldByCurrentThread);
                            DocumentsWriterPerThread dwpt;
                            long bytes = perThread.BytesUsed; // do that before
                            // replace!
                            dwpt = PerThreadPool.Reset(perThread, Closed);
                            Debug.Assert(!FlushingWriters.ContainsKey(dwpt), "DWPT is already flushing");
                            // Record the flushing DWPT to reduce flushBytes in doAfterFlush
                            FlushingWriters[dwpt] = Convert.ToInt64(bytes);
                            NumPending--; // write access synced
                            return dwpt;
                        }
                    }
                    finally
                    {
                        perThread.Unlock();
                    }
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
            return "DocumentsWriterFlushControl [activeBytes=" + ActiveBytes_Renamed + ", flushBytes=" + FlushBytes_Renamed + "]";
        }

        internal DocumentsWriterPerThread NextPendingFlush()
        {
            int numPending;
            bool fullFlush;
            lock (this)
            {
                DocumentsWriterPerThread poll;
                if (FlushQueue.Count > 0 && (poll = FlushQueue.Dequeue()) != null)
                {
                    UpdateStallState();
                    return poll;
                }
                fullFlush = this.FullFlush_Renamed;
                numPending = this.NumPending;
            }
            if (numPending > 0 && !fullFlush) // don't check if we are doing a full flush
            {
                int limit = PerThreadPool.ActiveThreadState;
                for (int i = 0; i < limit && numPending > 0; i++)
                {
                    ThreadState next = PerThreadPool.GetThreadState(i);
                    if (next.FlushPending_Renamed)
                    {
                        DocumentsWriterPerThread dwpt = TryCheckoutForFlush(next);
                        if (dwpt != null)
                        {
                            return dwpt;
                        }
                    }
                }
            }
            return null;
        }

        internal void SetClosed()
        {
            lock (this)
            {
                // set by DW to signal that we should not release new DWPT after close
                if (!Closed)
                {
                    this.Closed = true;
                    PerThreadPool.DeactivateUnreleasedStates();
                }
            }
        }

        /// <summary>
        /// Returns an iterator that provides access to all currently active <seealso cref="ThreadState"/>s
        /// </summary>
        public IEnumerator<ThreadState> AllActiveThreadStates()
        {
            return GetPerThreadsIterator(PerThreadPool.ActiveThreadState);
        }

        private IEnumerator<ThreadState> GetPerThreadsIterator(int upto)
        {
            return new IteratorAnonymousInnerClassHelper(this, upto);
        }

        private class IteratorAnonymousInnerClassHelper : IEnumerator<ThreadState>
        {
            private readonly DocumentsWriterFlushControl OuterInstance;
            private ThreadState current;
            private int Upto;
            private int i;

            public IteratorAnonymousInnerClassHelper(DocumentsWriterFlushControl outerInstance, int upto)
            {
                this.OuterInstance = outerInstance;
                this.Upto = upto;
                i = 0;
            }

            public ThreadState Current
            {
                get { return current; }
            }

            public void Dispose()
            {
            }

            object System.Collections.IEnumerator.Current
            {
                get { return Current; }
            }

            public bool MoveNext()
            {
                if (i < Upto)
                {
                    current = OuterInstance.PerThreadPool.GetThreadState(i++);
                    return true;
                }

                return false;
            }

            public void Reset()
            {
                throw new NotImplementedException();
            }
        }

        internal void DoOnDelete()
        {
            lock (this)
            {
                // pass null this is a global delete no update
                FlushPolicy.OnDelete(this, null);
            }
        }

        /// <summary>
        /// Returns the number of delete terms in the global pool
        /// </summary>
        public int NumGlobalTermDeletes
        {
            get
            {
                return DocumentsWriter.DeleteQueue.NumGlobalTermDeletes() + BufferedUpdatesStream.NumTerms();
            }
        }

        public long DeleteBytesUsed
        {
            get
            {
                return DocumentsWriter.DeleteQueue.BytesUsed() + BufferedUpdatesStream.BytesUsed();
            }
        }

        internal int NumFlushingDWPT()
        {
            lock (this)
            {
                return FlushingWriters.Count;
            }
        }

        public bool AndResetApplyAllDeletes
        {
            get
            {
                return FlushDeletes.GetAndSet(false);
            }
        }

        public void SetApplyAllDeletes()
        {
            FlushDeletes.Set(true);
        }

        internal int NumActiveDWPT()
        {
            return this.PerThreadPool.ActiveThreadState;
        }

        internal ThreadState ObtainAndLock()
        {
            ThreadState perThread = PerThreadPool.GetAndLock(Thread.CurrentThread, DocumentsWriter);
            bool success = false;
            try
            {
                if (perThread.Initialized && perThread.Dwpt.DeleteQueue != DocumentsWriter.DeleteQueue)
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
                    perThread.Unlock();
                }
            }
        }

        internal void MarkForFullFlush()
        {
            DocumentsWriterDeleteQueue flushingQueue;
            lock (this)
            {
                Debug.Assert(!FullFlush_Renamed, "called DWFC#markForFullFlush() while full flush is still running");
                Debug.Assert(FullFlushBuffer.Count == 0, "full flush buffer should be empty: " + FullFlushBuffer);
                FullFlush_Renamed = true;
                flushingQueue = DocumentsWriter.DeleteQueue;
                // Set a new delete queue - all subsequent DWPT will use this queue until
                // we do another full flush
                DocumentsWriterDeleteQueue newQueue = new DocumentsWriterDeleteQueue(flushingQueue.Generation + 1);
                DocumentsWriter.DeleteQueue = newQueue;
            }
            int limit = PerThreadPool.ActiveThreadState;
            for (int i = 0; i < limit; i++)
            {
                ThreadState next = PerThreadPool.GetThreadState(i);
                next.@Lock();
                try
                {
                    if (!next.Initialized)
                    {
                        if (Closed && next.Active)
                        {
                            PerThreadPool.DeactivateThreadState(next);
                        }
                        continue;
                    }
                    Debug.Assert(next.Dwpt.DeleteQueue == flushingQueue || next.Dwpt.DeleteQueue == DocumentsWriter.DeleteQueue, " flushingQueue: " + flushingQueue + " currentqueue: " + DocumentsWriter.DeleteQueue + " perThread queue: " + next.Dwpt.DeleteQueue + " numDocsInRam: " + next.Dwpt.NumDocsInRAM);
                    if (next.Dwpt.DeleteQueue != flushingQueue)
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
            lock (this)
            {
                /* make sure we move all DWPT that are where concurrently marked as
                 * pending and moved to blocked are moved over to the flushQueue. There is
                 * a chance that this happens since we marking DWPT for full flush without
                 * blocking indexing.*/
                PruneBlockedQueue(flushingQueue);
                Debug.Assert(AssertBlockedFlushes(DocumentsWriter.DeleteQueue));
                //FlushQueue.AddAll(FullFlushBuffer);
                foreach (var dwpt in FullFlushBuffer)
                {
                    FlushQueue.Enqueue(dwpt);
                }
                FullFlushBuffer.Clear();
                UpdateStallState();
            }
            Debug.Assert(AssertActiveDeleteQueue(DocumentsWriter.DeleteQueue));
        }

        private bool AssertActiveDeleteQueue(DocumentsWriterDeleteQueue queue)
        {
            int limit = PerThreadPool.ActiveThreadState;
            for (int i = 0; i < limit; i++)
            {
                ThreadState next = PerThreadPool.GetThreadState(i);
                next.@Lock();
                try
                {
                    Debug.Assert(!next.Initialized || next.Dwpt.DeleteQueue == queue, "isInitialized: " + next.Initialized + " numDocs: " + (next.Initialized ? next.Dwpt.NumDocsInRAM : 0));
                }
                finally
                {
                    next.Unlock();
                }
            }
            return true;
        }

        private readonly IList<DocumentsWriterPerThread> FullFlushBuffer = new List<DocumentsWriterPerThread>();

        internal void AddFlushableState(ThreadState perThread)
        {
            if (InfoStream_Renamed.IsEnabled("DWFC"))
            {
                InfoStream_Renamed.Message("DWFC", "addFlushableState " + perThread.Dwpt);
            }
            DocumentsWriterPerThread dwpt = perThread.Dwpt;
            //Debug.Assert(perThread.HeldByCurrentThread);
            Debug.Assert(perThread.Initialized);
            Debug.Assert(FullFlush_Renamed);
            Debug.Assert(dwpt.DeleteQueue != DocumentsWriter.DeleteQueue);
            if (dwpt.NumDocsInRAM > 0)
            {
                lock (this)
                {
                    if (!perThread.FlushPending_Renamed)
                    {
                        FlushPending = perThread;
                    }
                    DocumentsWriterPerThread flushingDWPT = InternalTryCheckOutForFlush(perThread);
                    Debug.Assert(flushingDWPT != null, "DWPT must never be null here since we hold the lock and it holds documents");
                    Debug.Assert(dwpt == flushingDWPT, "flushControl returned different DWPT");
                    FullFlushBuffer.Add(flushingDWPT);
                }
            }
            else
            {
                PerThreadPool.Reset(perThread, Closed); // make this state inactive
            }
        }

        /// <summary>
        /// Prunes the blockedQueue by removing all DWPT that are associated with the given flush queue.
        /// </summary>
        private void PruneBlockedQueue(DocumentsWriterDeleteQueue flushingQueue)
        {
            var node = BlockedFlushes.First;
            while (node != null)
            {
                var nextNode = node.Next;
                BlockedFlush blockedFlush = node.Value;
                if (blockedFlush.Dwpt.DeleteQueue == flushingQueue)
                {
                    BlockedFlushes.Remove(node);
                    Debug.Assert(!FlushingWriters.ContainsKey(blockedFlush.Dwpt), "DWPT is already flushing");
                    // Record the flushing DWPT to reduce flushBytes in doAfterFlush
                    FlushingWriters[blockedFlush.Dwpt] = Convert.ToInt64(blockedFlush.Bytes);
                    // don't decr pending here - its already done when DWPT is blocked
                    FlushQueue.Enqueue(blockedFlush.Dwpt);
                }
                node = nextNode;
            }
        }

        internal void FinishFullFlush()
        {
            lock (this)
            {
                Debug.Assert(FullFlush_Renamed);
                Debug.Assert(FlushQueue.Count == 0);
                Debug.Assert(FlushingWriters.Count == 0);
                try
                {
                    if (BlockedFlushes.Count > 0)
                    {
                        Debug.Assert(AssertBlockedFlushes(DocumentsWriter.DeleteQueue));
                        PruneBlockedQueue(DocumentsWriter.DeleteQueue);
                        Debug.Assert(BlockedFlushes.Count == 0);
                    }
                }
                finally
                {
                    FullFlush_Renamed = false;
                    UpdateStallState();
                }
            }
        }

        internal bool AssertBlockedFlushes(DocumentsWriterDeleteQueue flushingQueue)
        {
            foreach (BlockedFlush blockedFlush in BlockedFlushes)
            {
                Debug.Assert(blockedFlush.Dwpt.DeleteQueue == flushingQueue);
            }
            return true;
        }

        internal void AbortFullFlushes(ISet<string> newFiles)
        {
            lock (this)
            {
                try
                {
                    AbortPendingFlushes(newFiles);
                }
                finally
                {
                    FullFlush_Renamed = false;
                }
            }
        }

        internal void AbortPendingFlushes(ISet<string> newFiles)
        {
            lock (this)
            {
                try
                {
                    foreach (DocumentsWriterPerThread dwpt in FlushQueue)
                    {
                        try
                        {
                            DocumentsWriter.SubtractFlushedNumDocs(dwpt.NumDocsInRAM);
                            dwpt.Abort(newFiles);
                        }
                        catch (Exception)
                        {
                            // ignore - keep on aborting the flush queue
                        }
                        finally
                        {
                            DoAfterFlush(dwpt);
                        }
                    }
                    foreach (BlockedFlush blockedFlush in BlockedFlushes)
                    {
                        try
                        {
                            FlushingWriters[blockedFlush.Dwpt] = Convert.ToInt64(blockedFlush.Bytes);
                            DocumentsWriter.SubtractFlushedNumDocs(blockedFlush.Dwpt.NumDocsInRAM);
                            blockedFlush.Dwpt.Abort(newFiles);
                        }
                        catch (Exception)
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
                    FlushQueue.Clear();
                    BlockedFlushes.Clear();
                    UpdateStallState();
                }
            }
        }

        /// <summary>
        /// Returns <code>true</code> if a full flush is currently running
        /// </summary>
        internal bool FullFlush
        {
            get
            {
                lock (this)
                {
                    return FullFlush_Renamed;
                }
            }
        }

        /// <summary>
        /// Returns the number of flushes that are already checked out but not yet
        /// actively flushing
        /// </summary>
        internal int NumQueuedFlushes()
        {
            lock (this)
            {
                return FlushQueue.Count;
            }
        }

        /// <summary>
        /// Returns the number of flushes that are checked out but not yet available
        /// for flushing. this only applies during a full flush if a DWPT needs
        /// flushing but must not be flushed until the full flush has finished.
        /// </summary>
        internal int NumBlockedFlushes()
        {
            lock (this)
            {
                return BlockedFlushes.Count;
            }
        }

        private class BlockedFlush
        {
            internal readonly DocumentsWriterPerThread Dwpt;
            internal readonly long Bytes;

            internal BlockedFlush(DocumentsWriterPerThread dwpt, long bytes)
                : base()
            {
                this.Dwpt = dwpt;
                this.Bytes = bytes;
            }
        }

        /// <summary>
        /// this method will block if too many DWPT are currently flushing and no
        /// checked out DWPT are available
        /// </summary>
        internal void WaitIfStalled()
        {
            if (InfoStream_Renamed.IsEnabled("DWFC"))
            {
                InfoStream_Renamed.Message("DWFC", "waitIfStalled: numFlushesPending: " + FlushQueue.Count + " netBytes: " + NetBytes() + " flushBytes: " + FlushBytes() + " fullFlush: " + FullFlush_Renamed);
            }
            StallControl.WaitIfStalled();
        }

        /// <summary>
        /// Returns <code>true</code> iff stalled
        /// </summary>
        internal bool AnyStalledThreads()
        {
            return StallControl.AnyStalledThreads();
        }

        /// <summary>
        /// Returns the <seealso cref="IndexWriter"/> <seealso cref="InfoStream"/>
        /// </summary>
        public InfoStream InfoStream
        {
            get
            {
                return InfoStream_Renamed;
            }
        }
    }
}