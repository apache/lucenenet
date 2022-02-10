using J2N.Threading.Atomic;
using Lucene.Net.Diagnostics;
using Lucene.Net.Support.Threading;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
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

    using Analyzer = Lucene.Net.Analysis.Analyzer;
    using BytesRef = Lucene.Net.Util.BytesRef;
    using Directory = Lucene.Net.Store.Directory;
    using FlushedSegment = Lucene.Net.Index.DocumentsWriterPerThread.FlushedSegment;
    using IEvent = Lucene.Net.Index.IndexWriter.IEvent;
    using InfoStream = Lucene.Net.Util.InfoStream;
    using Query = Lucene.Net.Search.Query;
    using SegmentFlushTicket = Lucene.Net.Index.DocumentsWriterFlushQueue.SegmentFlushTicket;
    using ThreadState = Lucene.Net.Index.DocumentsWriterPerThreadPool.ThreadState;

    /// <summary>
    /// This class accepts multiple added documents and directly
    /// writes segment files.
    /// <para/>
    /// Each added document is passed to the <see cref="DocConsumer"/>,
    /// which in turn processes the document and interacts with
    /// other consumers in the indexing chain.  Certain
    /// consumers, like <see cref="StoredFieldsConsumer"/> and 
    /// <see cref="TermVectorsConsumer"/>, digest a document and
    /// immediately write bytes to the "doc store" files (ie,
    /// they do not consume RAM per document, except while they
    /// are processing the document).
    /// <para/>
    /// Other consumers, eg <see cref="FreqProxTermsWriter"/> and
    /// <see cref="NormsConsumer"/>, buffer bytes in RAM and flush only
    /// when a new segment is produced.
    /// <para/>
    /// Once we have used our allowed RAM buffer, or the number
    /// of added docs is large enough (in the case we are
    /// flushing by doc count instead of RAM usage), we create a
    /// real segment and flush it to the Directory.
    /// <para/>
    /// Threads:
    /// <para/>
    /// Multiple threads are allowed into AddDocument at once.
    /// There is an initial synchronized call to <see cref="DocumentsWriterPerThreadPool.GetThreadState(int)"/>
    /// which allocates a <see cref="ThreadState"/> for this thread.  The same
    /// thread will get the same <see cref="ThreadState"/> over time (thread
    /// affinity) so that if there are consistent patterns (for
    /// example each thread is indexing a different content
    /// source) then we make better use of RAM.  Then
    /// ProcessDocument is called on that <see cref="ThreadState"/> without
    /// synchronization (most of the "heavy lifting" is in this
    /// call).  Finally the synchronized "finishDocument" is
    /// called to flush changes to the directory.
    /// <para/>
    /// When flush is called by <see cref="IndexWriter"/> we forcefully idle
    /// all threads and flush only once they are all idle.  this
    /// means you can call flush with a given thread even while
    /// other threads are actively adding/deleting documents.
    /// <para/>
    ///
    /// Exceptions:
    /// <para/>
    /// Because this class directly updates in-memory posting
    /// lists, and flushes stored fields and term vectors
    /// directly to files in the directory, there are certain
    /// limited times when an exception can corrupt this state.
    /// For example, a disk full while flushing stored fields
    /// leaves this file in a corrupt state.  Or, an OOM
    /// exception while appending to the in-memory posting lists
    /// can corrupt that posting list.  We call such exceptions
    /// "aborting exceptions".  In these cases we must call
    /// <see cref="Abort(IndexWriter)"/> to discard all docs added since the last flush.
    /// <para/>
    /// All other exceptions ("non-aborting exceptions") can
    /// still partially update the index structures.  These
    /// updates are consistent, but, they represent only a part
    /// of the document seen up until the exception was hit.
    /// When this happens, we immediately mark the document as
    /// deleted so that the document is always atomically ("all
    /// or none") added to the index.
    /// </summary>
    internal sealed class DocumentsWriter : IDisposable
    {
        private readonly Directory directory;

        private volatile bool closed;

        private readonly InfoStream infoStream;

        private readonly LiveIndexWriterConfig config;

        private readonly AtomicInt32 numDocsInRAM = new AtomicInt32(0);

        // TODO: cut over to BytesRefHash in BufferedDeletes
        internal volatile DocumentsWriterDeleteQueue deleteQueue = new DocumentsWriterDeleteQueue();

        private readonly DocumentsWriterFlushQueue ticketQueue = new DocumentsWriterFlushQueue();

        /// <summary>
        /// we preserve changes during a full flush since IW might not checkout before
        /// we release all changes. NRT Readers otherwise suddenly return true from
        /// IsCurrent() while there are actually changes currently committed. See also
        /// <see cref="AnyChanges()"/> &amp; <see cref="FlushAllThreads(IndexWriter)"/>
        /// </summary>
        private volatile bool pendingChangesInCurrentFullFlush;

        internal readonly DocumentsWriterPerThreadPool perThreadPool;
        internal readonly FlushPolicy flushPolicy;
        internal readonly DocumentsWriterFlushControl flushControl;
        private readonly IndexWriter writer;
        private readonly ConcurrentQueue<IEvent> events;

        internal DocumentsWriter(IndexWriter writer, LiveIndexWriterConfig config, Directory directory)
        {
            this.directory = directory;
            this.config = config;
            this.infoStream = config.InfoStream;
            this.perThreadPool = config.IndexerThreadPool;
            flushPolicy = config.FlushPolicy;
            this.writer = writer;
            this.events = new ConcurrentQueue<IEvent>();
            flushControl = new DocumentsWriterFlushControl(this, config, writer.bufferedUpdatesStream);
        }

        internal bool DeleteQueries(params Query[] queries)
        {
            UninterruptableMonitor.Enter(this);
            try
            {
                // TODO why is this synchronized?
                DocumentsWriterDeleteQueue deleteQueue = this.deleteQueue;
                deleteQueue.AddDelete(queries);
                flushControl.DoOnDelete();
                return ApplyAllDeletes(deleteQueue);
            }
            finally
            {
                UninterruptableMonitor.Exit(this);
            }
        }

        // TODO: we could check w/ FreqProxTermsWriter: if the
        // term doesn't exist, don't bother buffering into the
        // per-DWPT map (but still must go into the global map)
        internal bool DeleteTerms(params Term[] terms)
        {
            UninterruptableMonitor.Enter(this);
            try
            {
                // TODO why is this synchronized?
                DocumentsWriterDeleteQueue deleteQueue = this.deleteQueue;
                deleteQueue.AddDelete(terms);
                flushControl.DoOnDelete();
                return ApplyAllDeletes(deleteQueue);
            }
            finally
            {
                UninterruptableMonitor.Exit(this);
            }
        }

        internal bool UpdateNumericDocValue(Term term, string field, long? value)
        {
            UninterruptableMonitor.Enter(this);
            try
            {
                DocumentsWriterDeleteQueue deleteQueue = this.deleteQueue;
                deleteQueue.AddNumericUpdate(new NumericDocValuesUpdate(term, field, value));
                flushControl.DoOnDelete();
                return ApplyAllDeletes(deleteQueue);
            }
            finally
            {
                UninterruptableMonitor.Exit(this);
            }
        }

        internal bool UpdateBinaryDocValue(Term term, string field, BytesRef value)
        {
            UninterruptableMonitor.Enter(this);
            try
            {
                DocumentsWriterDeleteQueue deleteQueue = this.deleteQueue;
                deleteQueue.AddBinaryUpdate(new BinaryDocValuesUpdate(term, field, value));
                flushControl.DoOnDelete();
                return ApplyAllDeletes(deleteQueue);
            }
            finally
            {
                UninterruptableMonitor.Exit(this);
            }
        }

        internal DocumentsWriterDeleteQueue CurrentDeleteSession => deleteQueue;

        private bool ApplyAllDeletes(DocumentsWriterDeleteQueue deleteQueue)
        {
            if (flushControl.GetAndResetApplyAllDeletes())
            {
                if (deleteQueue != null && !flushControl.IsFullFlush)
                {
                    ticketQueue.AddDeletes(deleteQueue);
                }
                PutEvent(ApplyDeletesEvent.INSTANCE); // apply deletes event forces a purge
                return true;
            }
            return false;
        }

        internal int PurgeBuffer(IndexWriter writer, bool forced)
        {
            if (forced)
            {
                return ticketQueue.ForcePurge(writer);
            }
            else
            {
                return ticketQueue.TryPurge(writer);
            }
        }

        /// <summary>
        /// Returns how many docs are currently buffered in RAM. </summary>
        internal int NumDocs => numDocsInRAM;

        private void EnsureOpen()
        {
            if (closed)
            {
                throw AlreadyClosedException.Create(this.GetType().FullName, "this IndexWriter is disposed.");
            }
        }

        /// <summary>
        /// Called if we hit an exception at a bad time (when
        ///  updating the index files) and must discard all
        ///  currently buffered docs.  this resets our state,
        ///  discarding any docs added since last flush.
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal void Abort(IndexWriter writer)
        {
            UninterruptableMonitor.Enter(this);
            try
            {
                if (Debugging.AssertsEnabled) Debugging.Assert(!UninterruptableMonitor.IsEntered(writer), "IndexWriter lock should never be hold when aborting");
                bool success = false;
                JCG.HashSet<string> newFilesSet = new JCG.HashSet<string>();
                try
                {
                    deleteQueue.Clear();
                    if (infoStream.IsEnabled("DW"))
                    {
                        infoStream.Message("DW", "abort");
                    }
                    int limit = perThreadPool.NumThreadStatesActive;
                    for (int i = 0; i < limit; i++)
                    {
                        ThreadState perThread = perThreadPool.GetThreadState(i);
                        perThread.@Lock();
                        try
                        {
                            AbortThreadState(perThread, newFilesSet);
                        }
                        finally
                        {
                            perThread.Unlock();
                        }
                    }
                    flushControl.AbortPendingFlushes(newFilesSet);
                    PutEvent(new DeleteNewFilesEvent(newFilesSet));
                    flushControl.WaitForFlush();
                    success = true;
                }
                finally
                {
                    if (infoStream.IsEnabled("DW"))
                    {
                        infoStream.Message("DW", "done abort; abortedFiles=" + string.Format(J2N.Text.StringFormatter.InvariantCulture, "{0}", newFilesSet) + " success=" + success);
                    }
                }
            }
            finally
            {
                UninterruptableMonitor.Exit(this);
            }
        }

        internal void LockAndAbortAll(IndexWriter indexWriter)
        {
            UninterruptableMonitor.Enter(this);
            try
            {
                if (Debugging.AssertsEnabled) Debugging.Assert(indexWriter.HoldsFullFlushLock);
                if (infoStream.IsEnabled("DW"))
                {
                    infoStream.Message("DW", "lockAndAbortAll");
                }
                bool success = false;
                try
                {
                    deleteQueue.Clear();
                    int limit = perThreadPool.MaxThreadStates;
                    JCG.HashSet<string> newFilesSet = new JCG.HashSet<string>();
                    for (int i = 0; i < limit; i++)
                    {
                        ThreadState perThread = perThreadPool.GetThreadState(i);
                        perThread.@Lock();
                        AbortThreadState(perThread, newFilesSet);
                    }
                    deleteQueue.Clear();
                    flushControl.AbortPendingFlushes(newFilesSet);
                    PutEvent(new DeleteNewFilesEvent(newFilesSet));
                    flushControl.WaitForFlush();
                    success = true;
                }
                finally
                {
                    if (infoStream.IsEnabled("DW"))
                    {
                        infoStream.Message("DW", "finished lockAndAbortAll success=" + success);
                    }
                    if (!success)
                    {
                        // if something happens here we unlock all states again
                        UnlockAllAfterAbortAll(indexWriter);
                    }
                }
            }
            finally
            {
                UninterruptableMonitor.Exit(this);
            }
        }

        private void AbortThreadState(ThreadState perThread, ISet<string> newFiles)
        {
            if (Debugging.AssertsEnabled) Debugging.Assert(perThread.IsHeldByCurrentThread);
            if (perThread.IsActive) // we might be closed
            {
                if (perThread.IsInitialized)
                {
                    try
                    {
                        SubtractFlushedNumDocs(perThread.dwpt.NumDocsInRAM);
                        perThread.dwpt.Abort(newFiles);
                    }
                    finally
                    {
                        perThread.dwpt.CheckAndResetHasAborted();
                        flushControl.DoOnAbort(perThread);
                    }
                }
                else
                {
                    flushControl.DoOnAbort(perThread);
                }
            }
            else
            {
                if (Debugging.AssertsEnabled) Debugging.Assert(closed);
            }
        }

        internal void UnlockAllAfterAbortAll(IndexWriter indexWriter)
        {
            UninterruptableMonitor.Enter(this);
            try
            {
                if (Debugging.AssertsEnabled) Debugging.Assert(indexWriter.HoldsFullFlushLock);
                if (infoStream.IsEnabled("DW"))
                {
                    infoStream.Message("DW", "unlockAll");
                }
                int limit = perThreadPool.MaxThreadStates;
                for (int i = 0; i < limit; i++)
                {
                    try
                    {
                        ThreadState perThread = perThreadPool.GetThreadState(i);
                        if (perThread.IsHeldByCurrentThread)
                        {
                            perThread.Unlock();
                        }
                    }
                    catch (Exception e) when (e.IsThrowable())
                    {
                        if (infoStream.IsEnabled("DW"))
                        {
                            infoStream.Message("DW", "unlockAll: could not unlock state: " + i + " msg:" + e.Message);
                        }
                        // ignore & keep on unlocking
                    }
                }
            }
            finally
            {
                UninterruptableMonitor.Exit(this);
            }
        }

        internal bool AnyChanges()
        {
            if (infoStream.IsEnabled("DW"))
            {
                infoStream.Message("DW", "anyChanges? numDocsInRam=" + numDocsInRAM + " deletes=" + AnyDeletions() + " hasTickets:" + ticketQueue.HasTickets + " pendingChangesInFullFlush: " + pendingChangesInCurrentFullFlush);
            }
            /*
             * changes are either in a DWPT or in the deleteQueue.
             * yet if we currently flush deletes and / or dwpt there
             * could be a window where all changes are in the ticket queue
             * before they are published to the IW. ie we need to check if the
             * ticket queue has any tickets.
             */
            return numDocsInRAM != 0 || AnyDeletions() || ticketQueue.HasTickets || pendingChangesInCurrentFullFlush;
        }

        public int BufferedDeleteTermsSize => deleteQueue.BufferedUpdatesTermsSize;

        //for testing
        public int NumBufferedDeleteTerms => deleteQueue.NumGlobalTermDeletes;

        public bool AnyDeletions()
        {
            return deleteQueue.AnyChanges();
        }

        public void Dispose()
        {
            closed = true;
            flushControl.SetClosed();
        }

        private bool PreUpdate()
        {
            EnsureOpen();
            bool hasEvents = false;
            if (flushControl.AnyStalledThreads() || flushControl.NumQueuedFlushes > 0)
            {
                // Help out flushing any queued DWPTs so we can un-stall:
                if (infoStream.IsEnabled("DW"))
                {
                    infoStream.Message("DW", "DocumentsWriter has queued dwpt; will hijack this thread to flush pending segment(s)");
                }
                do
                {
                    // Try pick up pending threads here if possible
                    DocumentsWriterPerThread flushingDWPT;
                    while ((flushingDWPT = flushControl.NextPendingFlush()) != null)
                    {
                        // Don't push the delete here since the update could fail!
                        hasEvents |= DoFlush(flushingDWPT);
                    }

                    if (infoStream.IsEnabled("DW"))
                    {
                        if (flushControl.AnyStalledThreads())
                        {
                            infoStream.Message("DW", "WARNING DocumentsWriter has stalled threads; waiting");
                        }
                    }

                    flushControl.WaitIfStalled(); // block if stalled
                } while (flushControl.NumQueuedFlushes != 0); // still queued DWPTs try help flushing

                if (infoStream.IsEnabled("DW"))
                {
                    infoStream.Message("DW", "continue indexing after helping out flushing DocumentsWriter is healthy");
                }
            }
            return hasEvents;
        }

        private bool PostUpdate(DocumentsWriterPerThread flushingDWPT, bool hasEvents)
        {
            hasEvents |= ApplyAllDeletes(deleteQueue);
            if (flushingDWPT != null)
            {
                hasEvents |= DoFlush(flushingDWPT);
            }
            else
            {
                DocumentsWriterPerThread nextPendingFlush = flushControl.NextPendingFlush();
                if (nextPendingFlush != null)
                {
                    hasEvents |= DoFlush(nextPendingFlush);
                }
            }

            return hasEvents;
        }

        private void EnsureInitialized(ThreadState state)
        {
            if (state.IsActive && state.dwpt is null)
            {
                FieldInfos.Builder infos = new FieldInfos.Builder(writer.globalFieldNumberMap);
                state.dwpt = new DocumentsWriterPerThread(writer.NewSegmentName(), directory, config, infoStream, deleteQueue, infos);
            }
        }

        internal bool UpdateDocuments(IEnumerable<IEnumerable<IIndexableField>> docs, Analyzer analyzer, Term delTerm)
        {
            bool hasEvents = PreUpdate();

            ThreadState perThread = flushControl.ObtainAndLock();
            DocumentsWriterPerThread flushingDWPT;

            try
            {
                if (!perThread.IsActive)
                {
                    EnsureOpen();
                    if (Debugging.AssertsEnabled) Debugging.Assert(false, "perThread is not active but we are still open");
                }
                EnsureInitialized(perThread);
                if (Debugging.AssertsEnabled) Debugging.Assert(perThread.IsInitialized);
                DocumentsWriterPerThread dwpt = perThread.dwpt;
                int dwptNumDocs = dwpt.NumDocsInRAM;
                try
                {
                    int docCount = dwpt.UpdateDocuments(docs, analyzer, delTerm);
                    numDocsInRAM.AddAndGet(docCount);
                }
                finally
                {
                    if (dwpt.CheckAndResetHasAborted())
                    {
                        if (dwpt.PendingFilesToDelete.Count > 0)
                        {
                            PutEvent(new DeleteNewFilesEvent(dwpt.PendingFilesToDelete));
                        }
                        SubtractFlushedNumDocs(dwptNumDocs);
                        flushControl.DoOnAbort(perThread);
                    }
                }
                bool isUpdate = delTerm != null;
                flushingDWPT = flushControl.DoAfterDocument(perThread, isUpdate);
            }
            finally
            {
                perThreadPool.Release(perThread);
            }

            return PostUpdate(flushingDWPT, hasEvents);
        }

        internal bool UpdateDocument(IEnumerable<IIndexableField> doc, Analyzer analyzer, Term delTerm)
        {
            bool hasEvents = PreUpdate();

            ThreadState perThread = flushControl.ObtainAndLock();

            DocumentsWriterPerThread flushingDWPT;
            try
            {
                if (!perThread.IsActive)
                {
                    EnsureOpen();
                    if (Debugging.AssertsEnabled) Debugging.Assert(false, "perThread is not active but we are still open");
                }
                EnsureInitialized(perThread);
                if (Debugging.AssertsEnabled) Debugging.Assert(perThread.IsInitialized);
                DocumentsWriterPerThread dwpt = perThread.dwpt;
                int dwptNumDocs = dwpt.NumDocsInRAM;
                try
                {
                    dwpt.UpdateDocument(doc, analyzer, delTerm);
                    numDocsInRAM.IncrementAndGet();
                }
                finally
                {
                    if (dwpt.CheckAndResetHasAborted())
                    {
                        if (dwpt.PendingFilesToDelete.Count > 0)
                        {
                            PutEvent(new DeleteNewFilesEvent(dwpt.PendingFilesToDelete));
                        }
                        SubtractFlushedNumDocs(dwptNumDocs);
                        flushControl.DoOnAbort(perThread);
                    }
                }
                bool isUpdate = delTerm != null;
                flushingDWPT = flushControl.DoAfterDocument(perThread, isUpdate);
            }
            finally
            {
                perThreadPool.Release(perThread);
            }

            return PostUpdate(flushingDWPT, hasEvents);
        }

        private bool DoFlush(DocumentsWriterPerThread flushingDWPT)
        {
            bool hasEvents = false;
            while (flushingDWPT != null)
            {
                hasEvents = true;
                bool success = false;
                SegmentFlushTicket ticket = null;
                try
                {
                    if (Debugging.AssertsEnabled) Debugging.Assert(currentFullFlushDelQueue is null
                        || flushingDWPT.deleteQueue == currentFullFlushDelQueue,
                        "expected: {0} but was: {1} {2}", currentFullFlushDelQueue, flushingDWPT.deleteQueue, flushControl.IsFullFlush);
                    /*
                     * Since with DWPT the flush process is concurrent and several DWPT
                     * could flush at the same time we must maintain the order of the
                     * flushes before we can apply the flushed segment and the frozen global
                     * deletes it is buffering. The reason for this is that the global
                     * deletes mark a certain point in time where we took a DWPT out of
                     * rotation and freeze the global deletes.
                     *
                     * Example: A flush 'A' starts and freezes the global deletes, then
                     * flush 'B' starts and freezes all deletes occurred since 'A' has
                     * started. if 'B' finishes before 'A' we need to wait until 'A' is done
                     * otherwise the deletes frozen by 'B' are not applied to 'A' and we
                     * might miss to deletes documents in 'A'.
                     */
                    try
                    {
                        // Each flush is assigned a ticket in the order they acquire the ticketQueue lock
                        ticket = ticketQueue.AddFlushTicket(flushingDWPT);

                        int flushingDocsInRam = flushingDWPT.NumDocsInRAM;
                        bool dwptSuccess = false;
                        try
                        {
                            // flush concurrently without locking
                            FlushedSegment newSegment = flushingDWPT.Flush();
                            ticketQueue.AddSegment(ticket, newSegment);
                            dwptSuccess = true;
                        }
                        finally
                        {
                            SubtractFlushedNumDocs(flushingDocsInRam);
                            if (flushingDWPT.PendingFilesToDelete.Count > 0)
                            {
                                PutEvent(new DeleteNewFilesEvent(flushingDWPT.PendingFilesToDelete));
                                hasEvents = true;
                            }
                            if (!dwptSuccess)
                            {
                                PutEvent(new FlushFailedEvent(flushingDWPT.SegmentInfo));
                                hasEvents = true;
                            }
                        }
                        // flush was successful once we reached this point - new seg. has been assigned to the ticket!
                        success = true;
                    }
                    finally
                    {
                        if (!success && ticket != null)
                        {
                            // In the case of a failure make sure we are making progress and
                            // apply all the deletes since the segment flush failed since the flush
                            // ticket could hold global deletes see FlushTicket#canPublish()
                            ticketQueue.MarkTicketFailed(ticket);
                        }
                    }
                    /*
                     * Now we are done and try to flush the ticket queue if the head of the
                     * queue has already finished the flush.
                     */
                    if (ticketQueue.TicketCount >= perThreadPool.NumThreadStatesActive)
                    {
                        // this means there is a backlog: the one
                        // thread in innerPurge can't keep up with all
                        // other threads flushing segments.  In this case
                        // we forcefully stall the producers.
                        PutEvent(ForcedPurgeEvent.INSTANCE);
                        break;
                    }
                }
                finally
                {
                    flushControl.DoAfterFlush(flushingDWPT);
                    flushingDWPT.CheckAndResetHasAborted();
                }

                flushingDWPT = flushControl.NextPendingFlush();
            }
            if (hasEvents)
            {
                PutEvent(MergePendingEvent.INSTANCE);
            }
            // If deletes alone are consuming > 1/2 our RAM
            // buffer, force them all to apply now. this is to
            // prevent too-frequent flushing of a long tail of
            // tiny segments:
            double ramBufferSizeMB = config.RAMBufferSizeMB;
            if (ramBufferSizeMB != Index.IndexWriterConfig.DISABLE_AUTO_FLUSH && flushControl.DeleteBytesUsed > (1024 * 1024 * ramBufferSizeMB / 2))
            {
                if (infoStream.IsEnabled("DW"))
                {
                    infoStream.Message("DW", "force apply deletes bytesUsed=" + flushControl.DeleteBytesUsed + " vs ramBuffer=" + (1024 * 1024 * ramBufferSizeMB));
                }
                hasEvents = true;
                if (!this.ApplyAllDeletes(deleteQueue))
                {
                    PutEvent(ApplyDeletesEvent.INSTANCE);
                }
            }

            return hasEvents;
        }

        internal void SubtractFlushedNumDocs(int numFlushed)
        {
            int oldValue = numDocsInRAM;
            while (!numDocsInRAM.CompareAndSet(oldValue, oldValue - numFlushed))
            {
                oldValue = numDocsInRAM;
            }
        }

        // for asserts
        private volatile DocumentsWriterDeleteQueue currentFullFlushDelQueue = null;

        // for asserts
        private bool SetFlushingDeleteQueue(DocumentsWriterDeleteQueue session)
        {
            UninterruptableMonitor.Enter(this);
            try
            {
                currentFullFlushDelQueue = session;
                return true;
            }
            finally
            {
                UninterruptableMonitor.Exit(this);
            }
        }

        /*
         * FlushAllThreads is synced by IW fullFlushLock. Flushing all threads is a
         * two stage operation; the caller must ensure (in try/finally) that finishFlush
         * is called after this method, to release the flush lock in DWFlushControl
         */

        internal bool FlushAllThreads(IndexWriter indexWriter)
        {
            DocumentsWriterDeleteQueue flushingDeleteQueue;
            if (infoStream.IsEnabled("DW"))
            {
                infoStream.Message("DW", "startFullFlush");
            }

            UninterruptableMonitor.Enter(this);
            try
            {
                pendingChangesInCurrentFullFlush = AnyChanges();
                flushingDeleteQueue = deleteQueue;
                /* Cutover to a new delete queue.  this must be synced on the flush control
                 * otherwise a new DWPT could sneak into the loop with an already flushing
                 * delete queue */
                flushControl.MarkForFullFlush(); // swaps the delQueue synced on FlushControl
                if (Debugging.AssertsEnabled) Debugging.Assert(SetFlushingDeleteQueue(flushingDeleteQueue));
            }
            finally
            {
                UninterruptableMonitor.Exit(this);
            }
            if (Debugging.AssertsEnabled)
            {
                Debugging.Assert(currentFullFlushDelQueue != null);
                Debugging.Assert(currentFullFlushDelQueue != deleteQueue);
            }

            bool anythingFlushed = false;
            try
            {
                DocumentsWriterPerThread flushingDWPT;
                // Help out with flushing:
                while ((flushingDWPT = flushControl.NextPendingFlush()) != null)
                {
                    anythingFlushed |= DoFlush(flushingDWPT);
                }
                // If a concurrent flush is still in flight wait for it
                flushControl.WaitForFlush();
                if (!anythingFlushed && flushingDeleteQueue.AnyChanges()) // apply deletes if we did not flush any document
                {
                    if (infoStream.IsEnabled("DW"))
                    {
                        infoStream.Message("DW", Thread.CurrentThread.Name + ": flush naked frozen global deletes");
                    }
                    ticketQueue.AddDeletes(flushingDeleteQueue);
                }
                ticketQueue.ForcePurge(indexWriter);
                if (Debugging.AssertsEnabled) Debugging.Assert(!flushingDeleteQueue.AnyChanges() && !ticketQueue.HasTickets);
            }
            finally
            {
                if (Debugging.AssertsEnabled) Debugging.Assert(flushingDeleteQueue == currentFullFlushDelQueue);
            }
            return anythingFlushed;
        }

        internal void FinishFullFlush(bool success)
        {
            try
            {
                if (infoStream.IsEnabled("DW"))
                {
                    infoStream.Message("DW", Thread.CurrentThread.Name + " finishFullFlush success=" + success);
                }
                if (Debugging.AssertsEnabled) Debugging.Assert(SetFlushingDeleteQueue(null));
                if (success)
                {
                    // Release the flush lock
                    flushControl.FinishFullFlush();
                }
                else
                {
                    JCG.HashSet<string> newFilesSet = new JCG.HashSet<string>();
                    flushControl.AbortFullFlushes(newFilesSet);
                    PutEvent(new DeleteNewFilesEvent(newFilesSet));
                }
            }
            finally
            {
                pendingChangesInCurrentFullFlush = false;
            }
        }

        public LiveIndexWriterConfig IndexWriterConfig => config;

        private void PutEvent(IEvent @event)
        {
            events.Enqueue(@event);
        }

        internal sealed class ApplyDeletesEvent : IEvent
        {
            internal static readonly IEvent INSTANCE = new ApplyDeletesEvent();
            private static int instCount = 0; // LUCENENET: Made static, otherwise this makes no sense at all

            internal ApplyDeletesEvent()
            {
                if (Debugging.AssertsEnabled) Debugging.Assert(instCount == 0);
                instCount++;
            }

            public void Process(IndexWriter writer, bool triggerMerge, bool forcePurge)
            {
                writer.ApplyDeletesAndPurge(true); // we always purge!
            }
        }

        internal sealed class MergePendingEvent : IEvent
        {
            internal static readonly IEvent INSTANCE = new MergePendingEvent();
            private static int instCount = 0; // LUCENENET: Made static, otherwise this makes no sense at all

            internal MergePendingEvent()
            {
                if (Debugging.AssertsEnabled) Debugging.Assert(instCount == 0);
                instCount++;
            }

            public void Process(IndexWriter writer, bool triggerMerge, bool forcePurge)
            {
                writer.DoAfterSegmentFlushed(triggerMerge, forcePurge);
            }
        }

        internal sealed class ForcedPurgeEvent : IEvent
        {
            internal static readonly IEvent INSTANCE = new ForcedPurgeEvent();
            private static int instCount = 0; // LUCENENET: Made static, otherwise this makes no sense at all

            internal ForcedPurgeEvent()
            {
                if (Debugging.AssertsEnabled) Debugging.Assert(instCount == 0);
                instCount++;
            }

            public void Process(IndexWriter writer, bool triggerMerge, bool forcePurge)
            {
                writer.Purge(true);
            }
        }

        internal class FlushFailedEvent : IEvent
        {
            private readonly SegmentInfo info;

            public FlushFailedEvent(SegmentInfo info)
            {
                this.info = info;
            }

            public void Process(IndexWriter writer, bool triggerMerge, bool forcePurge)
            {
                writer.FlushFailed(info);
            }
        }

        internal class DeleteNewFilesEvent : IEvent
        {
            private readonly ICollection<string> files;

            public DeleteNewFilesEvent(ICollection<string> files)
            {
                this.files = files;
            }

            public void Process(IndexWriter writer, bool triggerMerge, bool forcePurge)
            {
                writer.DeleteNewFiles(files);
            }
        }

        public ConcurrentQueue<IEvent> EventQueue => events;
    }
}