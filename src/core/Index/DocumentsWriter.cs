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

using Lucene.Net.Analysis;
using Lucene.Net.Codecs;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Support;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using FieldNumbers = Lucene.Net.Index.FieldInfos.FieldNumbers;
using FlushedSegment = Lucene.Net.Index.DocumentsWriterPerThread.FlushedSegment;
using IndexingChain = Lucene.Net.Index.DocumentsWriterPerThread.IndexingChain;
using ThreadState = Lucene.Net.Index.DocumentsWriterPerThreadPool.ThreadState;
using SegmentFlushTicket = Lucene.Net.Index.DocumentsWriterFlushQueue.SegmentFlushTicket;
using Lucene.Net.Search.Similarities;

namespace Lucene.Net.Index
{

    /// <summary> This class accepts multiple added documents and directly
    /// writes a single segment file.  It does this more
    /// efficiently than creating a single segment per document
    /// (with DocumentWriter) and doing standard merges on those
    /// segments.
    /// 
    /// Each added document is passed to the <see cref="DocConsumer" />,
    /// which in turn processes the document and interacts with
    /// other consumers in the indexing chain.  Certain
    /// consumers, like <see cref="StoredFieldsWriter" /> and <see cref="TermVectorsTermsWriter" />
    ///, digest a document and
    /// immediately write bytes to the "doc store" files (ie,
    /// they do not consume RAM per document, except while they
    /// are processing the document).
    /// 
    /// Other consumers, eg <see cref="FreqProxTermsWriter" /> and
    /// <see cref="NormsWriter" />, buffer bytes in RAM and flush only
    /// when a new segment is produced.
    /// Once we have used our allowed RAM buffer, or the number
    /// of added docs is large enough (in the case we are
    /// flushing by doc count instead of RAM usage), we create a
    /// real segment and flush it to the Directory.
    /// 
    /// Threads:
    /// 
    /// Multiple threads are allowed into addDocument at once.
    /// There is an initial synchronized call to getThreadState
    /// which allocates a ThreadState for this thread.  The same
    /// thread will get the same ThreadState over time (thread
    /// affinity) so that if there are consistent patterns (for
    /// example each thread is indexing a different content
    /// source) then we make better use of RAM.  Then
    /// processDocument is called on that ThreadState without
    /// synchronization (most of the "heavy lifting" is in this
    /// call).  Finally the synchronized "finishDocument" is
    /// called to flush changes to the directory.
    /// 
    /// When flush is called by IndexWriter we forcefully idle 
    /// all threads and flush only once they are all idle.  This
    /// means you can call flush with a given thread even while
    /// other threads are actively adding/deleting documents.
    /// 
    /// 
    /// Exceptions:
    /// 
    /// Because this class directly updates in-memory posting
    /// lists, and flushes stored fields and term vectors
    /// directly to files in the directory, there are certain
    /// limited times when an exception can corrupt this state.
    /// For example, a disk full while flushing stored fields
    /// leaves this file in a corrupt state.  Or, an OOM
    /// exception while appending to the in-memory posting lists
    /// can corrupt that posting list.  We call such exceptions
    /// "aborting exceptions".  In these cases we must call
    /// abort() to discard all docs added since the last flush.
    /// 
    /// All other exceptions ("non-aborting exceptions") can
    /// still partially update the index structures.  These
    /// updates are consistent, but, they represent only a part
    /// of the document seen up until the exception was hit.
    /// When this happens, we immediately mark the document as
    /// deleted so that the document is always atomically ("all
    /// or none") added to the index.
    /// </summary>

    public sealed class DocumentsWriter : IDisposable
    {
        internal Directory directory;

        private volatile bool closed;

        internal readonly InfoStream infoStream;
        internal Similarity similarity;

        internal IList<String> newFiles;

        internal readonly IndexWriter indexWriter;

        private int numDocsInRAM = 0;

        // TODO: cut over to BytesRefHash in BufferedDeletes
        internal volatile DocumentsWriterDeleteQueue deleteQueue = new DocumentsWriterDeleteQueue();
        private readonly DocumentsWriterFlushQueue ticketQueue = new DocumentsWriterFlushQueue();
        /*
         * we preserve changes during a full flush since IW might not checkout before
         * we release all changes. NRT Readers otherwise suddenly return true from
         * isCurrent while there are actually changes currently committed. See also
         * #anyChanges() & #flushAllThreads
         */
        private volatile bool pendingChangesInCurrentFullFlush;

        private ICollection<String> abortedFiles;               // List of files that were written before last abort()

        internal readonly IndexingChain chain;

        internal readonly DocumentsWriterPerThreadPool perThreadPool;
        internal readonly FlushPolicy flushPolicy;
        internal readonly DocumentsWriterFlushControl flushControl;

        internal readonly Codec codec;

        internal DocumentsWriter(Codec codec, LiveIndexWriterConfig config, Directory directory, IndexWriter writer, FieldNumbers globalFieldNumbers,
            BufferedDeletesStream bufferedDeletesStream)
        {
            this.codec = codec;
            this.directory = directory;
            this.indexWriter = writer;
            this.infoStream = config.InfoStream;
            this.similarity = config.Similarity;
            this.perThreadPool = config.IndexerThreadPool;
            this.chain = config.IndexingChain;
            this.perThreadPool.Initialize(this, globalFieldNumbers, config);
            flushPolicy = config.FlushPolicy;
            //assert flushPolicy != null;
            flushPolicy.Init(this);
            flushControl = new DocumentsWriterFlushControl(this, config);
        }

        internal void DeleteQueries(params Query[] queries)
        {
            deleteQueue.AddDelete(queries);
            flushControl.DoOnDelete();
            if (flushControl.DoApplyAllDeletes())
            {
                ApplyAllDeletes(deleteQueue);
            }
        }

        // TODO: we could check w/ FreqProxTermsWriter: if the
        // term doesn't exist, don't bother buffering into the
        // per-DWPT map (but still must go into the global map)
        internal void DeleteTerms(params Term[] terms)
        {
            lock (this)
            {
                DocumentsWriterDeleteQueue deleteQueue = this.deleteQueue;
                deleteQueue.AddDelete(terms);
                flushControl.DoOnDelete();
                if (flushControl.DoApplyAllDeletes())
                {
                    ApplyAllDeletes(deleteQueue);
                }
            }
        }

        internal DocumentsWriterDeleteQueue CurrentDeleteSession
        {
            get { return deleteQueue; }
        }

        private void ApplyAllDeletes(DocumentsWriterDeleteQueue deleteQueue)
        {
            if (deleteQueue != null && !flushControl.IsFullFlush)
            {
                ticketQueue.AddDeletesAndPurge(this, deleteQueue);
            }
            indexWriter.ApplyAllDeletes();
            Interlocked.Increment(ref indexWriter.flushCount);
        }

        internal int NumDocs
        {
            get { return numDocsInRAM; }
        }

        internal ICollection<String> AbortedFiles
        {
            get { return abortedFiles; }
        }

        private void EnsureOpen()
        {
            if (closed)
            {
                throw new AlreadyClosedException("this IndexWriter is closed");
            }
        }

        internal void Abort()
        {
            lock (this)
            {
                bool success = false;

                try
                {
                    deleteQueue.Clear();
                    if (infoStream.IsEnabled("DW"))
                    {
                        infoStream.Message("DW", "abort");
                    }

                    int limit = perThreadPool.ActiveThreadState;
                    for (int i = 0; i < limit; i++)
                    {
                        ThreadState perThread = perThreadPool.GetThreadState(i);
                        perThread.Lock();
                        try
                        {
                            if (perThread.IsActive)
                            { // we might be closed
                                try
                                {
                                    perThread.dwpt.Abort();
                                }
                                finally
                                {
                                    perThread.dwpt.CheckAndResetHasAborted();
                                    flushControl.DoOnAbort(perThread);
                                }
                            }
                            else
                            {
                                //assert closed;
                            }
                        }
                        finally
                        {
                            perThread.Unlock();
                        }
                    }
                    flushControl.AbortPendingFlushes();
                    flushControl.WaitForFlush();
                    success = true;
                }
                finally
                {
                    if (infoStream.IsEnabled("DW"))
                    {
                        infoStream.Message("DW", "done abort; abortedFiles=" + abortedFiles + " success=" + success);
                    }
                }
            }
        }

        internal void LockAndAbortAll()
        {
            //assert indexWriter.holdsFullFlushLock();
            if (infoStream.IsEnabled("DW"))
            {
                infoStream.Message("DW", "lockAndAbortAll");
            }
            bool success = false;
            try
            {
                deleteQueue.Clear();
                int limit = perThreadPool.MaxThreadStates;
                for (int i = 0; i < limit; i++)
                {
                    ThreadState perThread = perThreadPool.GetThreadState(i);
                    perThread.Lock();
                    if (perThread.IsActive)
                    { // we might be closed or 
                        try
                        {
                            perThread.dwpt.Abort();
                        }
                        finally
                        {
                            perThread.dwpt.CheckAndResetHasAborted();
                            flushControl.DoOnAbort(perThread);
                        }
                    }
                }
                deleteQueue.Clear();
                flushControl.AbortPendingFlushes();
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
                    UnlockAllAfterAbortAll();
                }
            }
        }

        internal void UnlockAllAfterAbortAll()
        {
            lock (this)
            {
                //assert indexWriter.holdsFullFlushLock();
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
                        //if (perThread.isHeldByCurrentThread())
                        try
                        {
                            // .NET Port: since we aren't yet targeting .NET 4.5, we don't have the IsEntered(obj) method on Monitor
                            // to be able to create an equivalent of IsHeldByCurrentThread.
                            // Instead, we'll try/catch here.
                            perThread.Unlock();
                        }
                        catch
                        {
                        }
                    }
                    catch (Exception e)
                    {
                        if (infoStream.IsEnabled("DW"))
                        {
                            infoStream.Message("DW", "unlockAll: could not unlock state: " + i + " msg:" + e.Message);
                        }
                        // ignore & keep on unlocking
                    }
                }
            }
        }

        internal bool AnyChanges
        {
            get
            {
                if (infoStream.IsEnabled("DW"))
                {
                    infoStream.Message("DW", "anyChanges? numDocsInRam=" + numDocsInRAM
                        + " deletes=" + AnyDeletions + " hasTickets:"
                        + ticketQueue.HasTickets + " pendingChangesInFullFlush: "
                        + pendingChangesInCurrentFullFlush);
                }
                /*
                 * changes are either in a DWPT or in the deleteQueue.
                 * yet if we currently flush deletes and / or dwpt there
                 * could be a window where all changes are in the ticket queue
                 * before they are published to the IW. ie we need to check if the 
                 * ticket queue has any tickets.
                 */
                return numDocsInRAM != 0 || AnyDeletions || ticketQueue.HasTickets || pendingChangesInCurrentFullFlush;
            }
        }

        public int BufferedDeleteTermsSize
        {
            get
            {
                return deleteQueue.BufferedDeleteTermsSize;
            }
        }

        //for testing
        public int NumBufferedDeleteTerms
        {
            get
            {
                return deleteQueue.NumGlobalTermDeletes;
            }
        }

        public bool AnyDeletions
        {
            get
            {
                return deleteQueue.AnyChanges;
            }
        }

        public void Dispose()
        {
            closed = true;
            flushControl.SetClosed();
        }

        private bool PreUpdate()
        {
            EnsureOpen();
            bool maybeMerge = false;
            if (flushControl.AnyStalledThreads || flushControl.NumQueuedFlushes > 0)
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
                    while ((flushingDWPT = flushControl.NextPendingFlush) != null)
                    {
                        // Don't push the delete here since the update could fail!
                        maybeMerge |= DoFlush(flushingDWPT);
                    }

                    if (infoStream.IsEnabled("DW"))
                    {
                        if (flushControl.AnyStalledThreads)
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
            return maybeMerge;
        }

        private bool PostUpdate(DocumentsWriterPerThread flushingDWPT, bool maybeMerge)
        {
            if (flushControl.DoApplyAllDeletes())
            {
                ApplyAllDeletes(deleteQueue);
            }
            if (flushingDWPT != null)
            {
                maybeMerge |= DoFlush(flushingDWPT);
            }
            else
            {
                DocumentsWriterPerThread nextPendingFlush = flushControl.NextPendingFlush;
                if (nextPendingFlush != null)
                {
                    maybeMerge |= DoFlush(nextPendingFlush);
                }
            }

            return maybeMerge;
        }

        internal bool UpdateDocuments(IEnumerable<IEnumerable<IIndexableField>> docs, Analyzer analyzer, Term delTerm)
        {
            bool maybeMerge = PreUpdate();

            ThreadState perThread = flushControl.ObtainAndLock();
            DocumentsWriterPerThread flushingDWPT;

            try
            {
                if (!perThread.IsActive)
                {
                    EnsureOpen();
                    //assert false: "perThread is not active but we are still open";
                }

                DocumentsWriterPerThread dwpt = perThread.dwpt;
                try
                {
                    int docCount = dwpt.UpdateDocuments(docs, analyzer, delTerm);
                    Interlocked.Add(ref numDocsInRAM, docCount);
                }
                finally
                {
                    if (dwpt.CheckAndResetHasAborted())
                    {
                        flushControl.DoOnAbort(perThread);
                    }
                }
                bool isUpdate = delTerm != null;
                flushingDWPT = flushControl.DoAfterDocument(perThread, isUpdate);
            }
            finally
            {
                perThread.Unlock();
            }

            return PostUpdate(flushingDWPT, maybeMerge);
        }

        internal bool UpdateDocument(IEnumerable<IIndexableField> doc, Analyzer analyzer, Term delTerm)
        {

            bool maybeMerge = PreUpdate();

            ThreadState perThread = flushControl.ObtainAndLock();

            DocumentsWriterPerThread flushingDWPT;

            try
            {

                if (!perThread.IsActive)
                {
                    EnsureOpen();
                    throw new InvalidOperationException("perThread is not active but we are still open");
                }

                DocumentsWriterPerThread dwpt = perThread.dwpt;
                try
                {
                    dwpt.UpdateDocument(doc, analyzer, delTerm);
                    Interlocked.Increment(ref numDocsInRAM);
                }
                finally
                {
                    if (dwpt.CheckAndResetHasAborted())
                    {
                        flushControl.DoOnAbort(perThread);
                    }
                }
                bool isUpdate = delTerm != null;
                flushingDWPT = flushControl.DoAfterDocument(perThread, isUpdate);
            }
            finally
            {
                perThread.Unlock();
            }

            return PostUpdate(flushingDWPT, maybeMerge);
        }

        private bool DoFlush(DocumentsWriterPerThread flushingDWPT)
        {
            bool maybeMerge = false;
            while (flushingDWPT != null)
            {
                maybeMerge = true;
                bool success = false;
                SegmentFlushTicket ticket = null;
                try
                {
                    //assert currentFullFlushDelQueue == null
                    //    || flushingDWPT.deleteQueue == currentFullFlushDelQueue : "expected: "
                    //    + currentFullFlushDelQueue + "but was: " + flushingDWPT.deleteQueue
                    //    + " " + flushControl.isFullFlush();
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

                        // flush concurrently without locking
                        FlushedSegment newSegment = flushingDWPT.Flush();
                        ticketQueue.AddSegment(ticket, newSegment);
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
                    if (ticketQueue.TicketCount >= perThreadPool.ActiveThreadState)
                    {
                        // This means there is a backlog: the one
                        // thread in innerPurge can't keep up with all
                        // other threads flushing segments.  In this case
                        // we forcefully stall the producers.
                        ticketQueue.ForcePurge(this);
                    }
                    else
                    {
                        ticketQueue.TryPurge(this);
                    }

                }
                finally
                {
                    flushControl.DoAfterFlush(flushingDWPT);
                    flushingDWPT.CheckAndResetHasAborted();
                    Interlocked.Increment(ref indexWriter.flushCount);
                    indexWriter.DoAfterFlush();
                }

                flushingDWPT = flushControl.NextPendingFlush;
            }

            // If deletes alone are consuming > 1/2 our RAM
            // buffer, force them all to apply now. This is to
            // prevent too-frequent flushing of a long tail of
            // tiny segments:
            double ramBufferSizeMB = indexWriter.Config.RAMBufferSizeMB;
            if (ramBufferSizeMB != IndexWriterConfig.DISABLE_AUTO_FLUSH &&
                flushControl.DeleteBytesUsed > (1024 * 1024 * ramBufferSizeMB / 2))
            {
                if (infoStream.IsEnabled("DW"))
                {
                    infoStream.Message("DW", "force apply deletes bytesUsed=" + flushControl.DeleteBytesUsed + " vs ramBuffer=" + (1024 * 1024 * ramBufferSizeMB));
                }
                ApplyAllDeletes(deleteQueue);
            }

            return maybeMerge;
        }

        internal void FinishFlush(FlushedSegment newSegment, FrozenBufferedDeletes bufferedDeletes)
        {
            // Finish the flushed segment and publish it to IndexWriter
            if (newSegment == null)
            {
                //assert bufferedDeletes != null;
                if (bufferedDeletes != null && bufferedDeletes.Any())
                {
                    indexWriter.PublishFrozenDeletes(bufferedDeletes);
                    if (infoStream.IsEnabled("DW"))
                    {
                        infoStream.Message("DW", "flush: push buffered deletes: " + bufferedDeletes);
                    }
                }
            }
            else
            {
                PublishFlushedSegment(newSegment, bufferedDeletes);
            }
        }

        internal void SubtractFlushedNumDocs(int numFlushed)
        {
            int oldValue = numDocsInRAM;
            while (Interlocked.CompareExchange(ref numDocsInRAM, oldValue - numFlushed, oldValue) != oldValue)
            {
                oldValue = numDocsInRAM;
            }
        }

        private void PublishFlushedSegment(FlushedSegment newSegment, FrozenBufferedDeletes globalPacket)
        {
            //assert newSegment != null;
            //assert newSegment.segmentInfo != null;
            FrozenBufferedDeletes segmentDeletes = newSegment.segmentDeletes;
            //System.out.println("FLUSH: " + newSegment.segmentInfo.info.name);
            if (infoStream.IsEnabled("DW"))
            {
                infoStream.Message("DW", "publishFlushedSegment seg-private deletes=" + segmentDeletes);
            }

            if (segmentDeletes != null && infoStream.IsEnabled("DW"))
            {
                infoStream.Message("DW", "flush: push buffered seg private deletes: " + segmentDeletes);
            }
            // now publish!
            indexWriter.PublishFlushedSegment(newSegment.segmentInfo, segmentDeletes, globalPacket);
        }

        private volatile DocumentsWriterDeleteQueue currentFullFlushDelQueue = null;

        // for asserts
        private bool SetFlushingDeleteQueue(DocumentsWriterDeleteQueue session)
        {
            lock (this)
            {
                currentFullFlushDelQueue = session;
                return true;
            }
        }

        internal bool FlushAllThreads()
        {
            DocumentsWriterDeleteQueue flushingDeleteQueue;
            if (infoStream.IsEnabled("DW"))
            {
                infoStream.Message("DW", Thread.CurrentThread.Name + " startFullFlush");
            }

            lock (this)
            {
                pendingChangesInCurrentFullFlush = AnyChanges;
                flushingDeleteQueue = deleteQueue;
                /* Cutover to a new delete queue.  This must be synced on the flush control
                 * otherwise a new DWPT could sneak into the loop with an already flushing
                 * delete queue */
                flushControl.MarkForFullFlush(); // swaps the delQueue synced on FlushControl
                //assert setFlushingDeleteQueue(flushingDeleteQueue);
            }
            //assert currentFullFlushDelQueue != null;
            //assert currentFullFlushDelQueue != deleteQueue;

            bool anythingFlushed = false;
            try
            {
                DocumentsWriterPerThread flushingDWPT;
                // Help out with flushing:
                while ((flushingDWPT = flushControl.NextPendingFlush) != null)
                {
                    anythingFlushed |= DoFlush(flushingDWPT);
                }
                // If a concurrent flush is still in flight wait for it
                flushControl.WaitForFlush();
                if (!anythingFlushed && flushingDeleteQueue.AnyChanges)
                { // apply deletes if we did not flush any document
                    if (infoStream.IsEnabled("DW"))
                    {
                        infoStream.Message("DW", Thread.CurrentThread.Name + ": flush naked frozen global deletes");
                    }
                    ticketQueue.AddDeletesAndPurge(this, flushingDeleteQueue);
                }
                else
                {
                    ticketQueue.ForcePurge(this);
                }
                //assert !flushingDeleteQueue.anyChanges() && !ticketQueue.hasTickets();
            }
            finally
            {
                //assert flushingDeleteQueue == currentFullFlushDelQueue;
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
                //assert setFlushingDeleteQueue(null);
                if (success)
                {
                    // Release the flush lock
                    flushControl.FinishFullFlush();
                }
                else
                {
                    flushControl.AbortFullFlushes();
                }
            }
            finally
            {
                pendingChangesInCurrentFullFlush = false;
            }
        }
    }
}