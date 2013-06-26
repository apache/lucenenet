using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using ThreadState = Lucene.Net.Index.DocumentsWriterPerThreadPool.ThreadState;

namespace Lucene.Net.Index
{
    public sealed class DocumentsWriterFlushControl
    {
        private readonly long hardMaxBytesPerDWPT;
        private long activeBytes = 0;
        private long flushBytes = 0;
        private volatile int numPending = 0;
        private int numDocsSinceStalled = 0; // only with assert
        internal AtomicBoolean flushDeletes = new AtomicBoolean(false);
        private bool fullFlush = false;
        private readonly Queue<DocumentsWriterPerThread> flushQueue = new Queue<DocumentsWriterPerThread>();
        // only for safety reasons if a DWPT is close to the RAM limit
        private readonly LinkedList<BlockedFlush> blockedFlushes = new LinkedList<BlockedFlush>();
        private readonly IDictionary<DocumentsWriterPerThread, long> flushingWriters = new IdentityHashMap<DocumentsWriterPerThread, long>();


        internal double maxConfiguredRamBuffer = 0;
        internal long peakActiveBytes = 0;// only with assert
        internal long peakFlushBytes = 0;// only with assert
        internal long peakNetBytes = 0;// only with assert
        internal long peakDelta = 0; // only with assert
        internal readonly DocumentsWriterStallControl stallControl;
        private readonly DocumentsWriterPerThreadPool perThreadPool;
        private readonly FlushPolicy flushPolicy;
        private bool closed = false;
        private readonly DocumentsWriter documentsWriter;
        private readonly LiveIndexWriterConfig config;

        public DocumentsWriterFlushControl(DocumentsWriter documentsWriter, LiveIndexWriterConfig config)
        {
            this.stallControl = new DocumentsWriterStallControl();
            this.perThreadPool = documentsWriter.perThreadPool;
            this.flushPolicy = documentsWriter.flushPolicy;
            this.hardMaxBytesPerDWPT = config.RAMPerThreadHardLimitMB * 1024 * 1024;
            this.config = config;
            this.documentsWriter = documentsWriter;
        }

        public long ActiveBytes
        {
            get
            {
                lock (this)
                {
                    return activeBytes;
                }
            }
        }

        public long FlushBytes
        {
            get
            {
                lock (this)
                {
                    return flushBytes;
                }
            }
        }

        public long NetBytes
        {
            get
            {
                lock (this)
                {
                    return flushBytes + activeBytes;
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
                     * buffer). This means that those DWPT and their threads will not hit
                     * the stall control before asserting the memory which would in turn
                     * fail. To prevent this we only assert if the the largest document seen
                     * is smaller than the 1/2 of the maxRamBufferMB
                     */
                    //assert ram <= expected : "actual mem: " + ram + " byte, expected mem: " + expected
                    //    + " byte, flush mem: " + flushBytes + ", active mem: " + activeBytes
                    //    + ", pending DWPT: " + numPending + ", flushing DWPT: "
                    //    + numFlushingDWPT() + ", blocked DWPT: " + numBlockedFlushes()
                    //    + ", peakDelta mem: " + peakDelta + " byte";
                }
            }
            return true;
        }

        private void CommitPerThreadBytes(ThreadState perThread)
        {
            long delta = perThread.dwpt.BytesUsed
                - perThread.bytesUsed;
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
            //assert updatePeaks(delta);
        }

        internal DocumentsWriterPerThread DoAfterDocument(ThreadState perThread, bool isUpdate)
        {
            lock (this)
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
                            // Safety check to prevent a single DWPT exceeding its RAM limit. This
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
                            flushingDWPT = NextPendingFlush;
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
                    //assert assertNumDocsSinceStalled(stalled) && assertMemory();
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
            lock (this)
            {
                //assert flushingWriters.containsKey(dwpt);
                try
                {
                    long bytes = flushingWriters[dwpt];
                    flushingWriters.Remove(dwpt);
                    flushBytes -= bytes;
                    perThreadPool.Recycle(dwpt);
                    //assert assertMemory();
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
            //assert Thread.holdsLock(this);
            long limit = StallLimitBytes;
            /*
             * we block indexing threads if net byte grows due to slow flushes
             * yet, for small ram buffers and large documents we can easily
             * reach the limit without any ongoing flushes. we need to ensure
             * that we don't stall/block if an ongoing or pending flush can
             * not free up enough memory to release the stall lock.
             */
            bool stall = ((activeBytes + flushBytes) > limit) &&
                                  (activeBytes < limit) &&
                                  !closed;
            stallControl.UpdateStalled(stall);
            return stall;
        }

        public void WaitForFlush()
        {
            lock (this)
            {
                //assert !Thread.holdsLock(this.documentsWriter.indexWriter) : "IW lock should never be hold when waiting on flush";
                while (flushingWriters.Count != 0)
                {
                    try
                    {
                        Monitor.Wait(this);
                    }
                    catch (ThreadInterruptedException)
                    {
                        throw;
                    }
                }
            }
        }

        public void SetFlushPending(ThreadState perThread)
        {
            lock (this)
            {
                //assert !perThread.flushPending;
                if (perThread.dwpt.NumDocsInRAM > 0)
                {
                    perThread.flushPending = true; // write access synced
                    long bytes = perThread.bytesUsed;
                    flushBytes += bytes;
                    activeBytes -= bytes;
                    numPending++; // write access synced
                    //assert assertMemory();
                } // don't assert on numDocs since we could hit an abort excp. while selecting that dwpt for flushing
            }
        }

        internal void DoOnAbort(ThreadState state)
        {
            lock (this)
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
                    //assert assertMemory();
                    // Take it out of the loop this DWPT is stale
                    perThreadPool.ReplaceForFlush(state, closed);
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
                return perThread.flushPending ? InternalTryCheckOutForFlush(perThread) : null;
            }
        }

        private void CheckoutAndBlock(ThreadState perThread)
        {
            perThread.Lock();
            try
            {
                //assert perThread.flushPending : "can not block non-pending threadstate";
                //assert fullFlush : "can not block if fullFlush == false";
                DocumentsWriterPerThread dwpt;
                long bytes = perThread.bytesUsed;
                dwpt = perThreadPool.ReplaceForFlush(perThread, closed);
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
            //assert Thread.holdsLock(this);
            //assert perThread.flushPending;
            try
            {
                // We are pending so all memory is already moved to flushBytes
                if (perThread.TryLock())
                {
                    try
                    {
                        if (perThread.IsActive)
                        {
                            //assert perThread.isHeldByCurrentThread();
                            DocumentsWriterPerThread dwpt;
                            long bytes = perThread.bytesUsed; // do that before
                            // replace!
                            dwpt = perThreadPool.ReplaceForFlush(perThread, closed);
                            //assert !flushingWriters.containsKey(dwpt) : "DWPT is already flushing";
                            // Record the flushing DWPT to reduce flushBytes in doAfterFlush
                            flushingWriters[dwpt] = bytes;
                            numPending--; // write access synced
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
            return "DocumentsWriterFlushControl [activeBytes=" + activeBytes
                + ", flushBytes=" + flushBytes + "]";
        }

        internal DocumentsWriterPerThread NextPendingFlush
        {
            get
            {
                int numPending;
                bool fullFlush;
                lock (this)
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
                if (numPending > 0 && !fullFlush)
                { // don't check if we are doing a full flush
                    int limit = perThreadPool.ActiveThreadState;
                    for (int i = 0; i < limit && numPending > 0; i++)
                    {
                        ThreadState next = perThreadPool.GetThreadState(i);
                        if (next.flushPending)
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
        }

        internal void SetClosed()
        {
            lock (this)
            {
                // set by DW to signal that we should not release new DWPT after close
                if (!closed)
                {
                    this.closed = true;
                    perThreadPool.DeactivateUnreleasedStates();
                }
            }
        }

        public IEnumerator<ThreadState> AllActiveThreadStates
        {
            get
            {
                return GetPerThreadsIterator(perThreadPool.ActiveThreadState);
            }
        }

        private IEnumerator<ThreadState> GetPerThreadsIterator(int upto)
        {
            return new AnonymousPerThreadsIterator(this, upto);
        }

        private sealed class AnonymousPerThreadsIterator : IEnumerator<ThreadState>
        {
            private readonly DocumentsWriterFlushControl parent;
            private readonly int upto;
            private ThreadState current;
            private int i = 0;

            public AnonymousPerThreadsIterator(DocumentsWriterFlushControl parent, int upto)
            {
                this.parent = parent;
                this.upto = upto;
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
                if (i < upto)
                {
                    current = parent.perThreadPool.GetThreadState(i++);
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
                flushPolicy.OnDelete(this, null);
            }
        }

        public int NumGlobalTermDeletes
        {
            get
            {
                return documentsWriter.deleteQueue.NumGlobalTermDeletes + documentsWriter.indexWriter.bufferedDeletesStream.NumTerms;
            }
        }

        public long DeleteBytesUsed
        {
            get
            {
                return documentsWriter.deleteQueue.BytesUsed + documentsWriter.indexWriter.bufferedDeletesStream.BytesUsed;
            }
        }

        internal int NumFlushingDWPT
        {
            get
            {
                return flushingWriters.Count;
            }
        }

        public bool DoApplyAllDeletes()
        {
            return flushDeletes.GetAndSet(false);
        }

        public void SetApplyAllDeletes()
        {
            flushDeletes.Set(true);
        }

        internal int NumActiveDWPT
        {
            get { return this.perThreadPool.ActiveThreadState; }
        }

        internal ThreadState ObtainAndLock()
        {
            ThreadState perThread = perThreadPool.GetAndLock(Thread.CurrentThread, documentsWriter);
            bool success = false;
            try
            {
                if (perThread.IsActive
                    && perThread.dwpt.deleteQueue != documentsWriter.deleteQueue)
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
                if (!success)
                { // make sure we unlock if this fails
                    perThread.Unlock();
                }
            }
        }

        internal void MarkForFullFlush()
        {
            DocumentsWriterDeleteQueue flushingQueue;
            lock (this)
            {
                //assert !fullFlush : "called DWFC#markForFullFlush() while full flush is still running";
                //assert fullFlushBuffer.isEmpty() : "full flush buffer should be empty: "+ fullFlushBuffer;
                fullFlush = true;
                flushingQueue = documentsWriter.deleteQueue;
                // Set a new delete queue - all subsequent DWPT will use this queue until
                // we do another full flush
                DocumentsWriterDeleteQueue newQueue = new DocumentsWriterDeleteQueue(flushingQueue.generation + 1);
                documentsWriter.deleteQueue = newQueue;
            }
            int limit = perThreadPool.ActiveThreadState;
            for (int i = 0; i < limit; i++)
            {
                ThreadState next = perThreadPool.GetThreadState(i);
                next.Lock();
                try
                {
                    if (!next.IsActive)
                    {
                        continue;
                    }
                    //assert next.dwpt.deleteQueue == flushingQueue
                    //    || next.dwpt.deleteQueue == documentsWriter.deleteQueue : " flushingQueue: "
                    //    + flushingQueue
                    //    + " currentqueue: "
                    //    + documentsWriter.deleteQueue
                    //    + " perThread queue: "
                    //    + next.dwpt.deleteQueue
                    //    + " numDocsInRam: " + next.dwpt.getNumDocsInRAM();
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
            lock (this)
            {
                /* make sure we move all DWPT that are where concurrently marked as
                 * pending and moved to blocked are moved over to the flushQueue. There is
                 * a chance that this happens since we marking DWPT for full flush without
                 * blocking indexing.*/
                PruneBlockedQueue(flushingQueue);
                //assert assertBlockedFlushes(documentsWriter.deleteQueue);
                foreach (var dwpt in fullFlushBuffer)
                {
                    flushQueue.Enqueue(dwpt);
                }
                fullFlushBuffer.Clear();
                UpdateStallState();
            }
            //assert assertActiveDeleteQueue(documentsWriter.deleteQueue);
        }

        private bool AssertActiveDeleteQueue(DocumentsWriterDeleteQueue queue)
        {
            int limit = perThreadPool.ActiveThreadState;
            for (int i = 0; i < limit; i++)
            {
                ThreadState next = perThreadPool.GetThreadState(i);
                next.Lock();
                try
                {
                    //assert !next.isActive() || next.dwpt.deleteQueue == queue;
                }
                finally
                {
                    next.Unlock();
                }
            }
            return true;
        }

        private readonly IList<DocumentsWriterPerThread> fullFlushBuffer = new List<DocumentsWriterPerThread>();

        internal void AddFlushableState(ThreadState perThread)
        {
            if (documentsWriter.infoStream.IsEnabled("DWFC"))
            {
                documentsWriter.infoStream.Message("DWFC", "addFlushableState " + perThread.dwpt);
            }
            DocumentsWriterPerThread dwpt = perThread.dwpt;
            //assert perThread.isHeldByCurrentThread();
            //assert perThread.isActive();
            //assert fullFlush;
            //assert dwpt.deleteQueue != documentsWriter.deleteQueue;
            if (dwpt.NumDocsInRAM > 0)
            {
                lock (this)
                {
                    if (!perThread.flushPending)
                    {
                        SetFlushPending(perThread);
                    }
                    DocumentsWriterPerThread flushingDWPT = InternalTryCheckOutForFlush(perThread);
                    //assert flushingDWPT != null : "DWPT must never be null here since we hold the lock and it holds documents";
                    //assert dwpt == flushingDWPT : "flushControl returned different DWPT";
                    fullFlushBuffer.Add(flushingDWPT);
                }
            }
            else
            {
                if (closed)
                {
                    perThreadPool.DeactivateThreadState(perThread); // make this state inactive
                }
                else
                {
                    perThreadPool.ReinitThreadState(perThread);
                }
            }
        }

        private void PruneBlockedQueue(DocumentsWriterDeleteQueue flushingQueue)
        {
            IEnumerator<BlockedFlush> iterator = blockedFlushes.ToList().GetEnumerator();
            while (iterator.MoveNext())
            {
                BlockedFlush blockedFlush = iterator.Current;
                if (blockedFlush.dwpt.deleteQueue == flushingQueue)
                {
                    blockedFlushes.Remove(blockedFlush);
                    //assert !flushingWriters.containsKey(blockedFlush.dwpt) : "DWPT is already flushing";
                    // Record the flushing DWPT to reduce flushBytes in doAfterFlush
                    flushingWriters[blockedFlush.dwpt] = blockedFlush.bytes;
                    // don't decr pending here - its already done when DWPT is blocked
                    flushQueue.Enqueue(blockedFlush.dwpt);
                }
            }
        }

        internal void FinishFullFlush()
        {
            lock (this)
            {
                //assert fullFlush;
                //assert flushQueue.isEmpty();
                //assert flushingWriters.isEmpty();
                try
                {
                    if (blockedFlushes.Count > 0)
                    {
                        //assert assertBlockedFlushes(documentsWriter.deleteQueue);
                        PruneBlockedQueue(documentsWriter.deleteQueue);
                        //assert blockedFlushes.isEmpty();
                    }
                }
                finally
                {
                    fullFlush = false;
                    UpdateStallState();
                }
            }
        }

        internal bool AssertBlockedFlushes(DocumentsWriterDeleteQueue flushingQueue)
        {
            foreach (BlockedFlush blockedFlush in blockedFlushes)
            {
                //assert blockedFlush.dwpt.deleteQueue == flushingQueue;
            }
            return true;
        }

        internal void AbortFullFlushes()
        {
            lock (this)
            {
                try
                {
                    AbortPendingFlushes();
                }
                finally
                {
                    fullFlush = false;
                }
            }
        }

        internal void AbortPendingFlushes()
        {
            lock (this)
            {
                try
                {
                    foreach (DocumentsWriterPerThread dwpt in flushQueue)
                    {
                        try
                        {
                            dwpt.Abort();
                        }
                        catch
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
                            flushingWriters[blockedFlush.dwpt] = blockedFlush.bytes;
                            blockedFlush.dwpt.Abort();
                        }
                        catch
                        {
                            // ignore - keep on aborting the blocked queue
                        }
                        finally
                        {
                            DoAfterFlush(blockedFlush.dwpt);
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
        }

        internal bool IsFullFlush
        {
            get
            {
                lock (this)
                {
                    return fullFlush;
                }
            }
        }

        internal int NumQueuedFlushes
        {
            get
            {
                lock (this)
                {
                    return flushQueue.Count;
                }
            }
        }

        internal int NumBlockedFlushes
        {
            get
            {
                lock (this)
                {
                    return blockedFlushes.Count;
                }
            }
        }

        private class BlockedFlush
        {
            internal readonly DocumentsWriterPerThread dwpt;
            internal readonly long bytes;

            public BlockedFlush(DocumentsWriterPerThread dwpt, long bytes)
            {
                this.dwpt = dwpt;
                this.bytes = bytes;
            }
        }

        internal void WaitIfStalled()
        {
            if (documentsWriter.infoStream.IsEnabled("DWFC"))
            {
                documentsWriter.infoStream.Message("DWFC",
                    "waitIfStalled: numFlushesPending: " + flushQueue.Count
                        + " netBytes: " + NetBytes + " flushBytes: " + FlushBytes
                        + " fullFlush: " + fullFlush);
            }
            stallControl.WaitIfStalled();
        }

        internal bool AnyStalledThreads
        {
            get { return stallControl.AnyStalledThreads; }
        }
    }
}
