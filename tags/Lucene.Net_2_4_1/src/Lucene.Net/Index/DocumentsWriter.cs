/**
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
using Document = Lucene.Net.Documents.Document;
using Similarity = Lucene.Net.Search.Similarity;
using Query = Lucene.Net.Search.Query;
using IndexSearcher = Lucene.Net.Search.IndexSearcher;
using Scorer = Lucene.Net.Search.Scorer;
using Weight = Lucene.Net.Search.Weight;
using Directory = Lucene.Net.Store.Directory;
using AlreadyClosedException = Lucene.Net.Store.AlreadyClosedException;
using ArrayUtil = Lucene.Net.Util.ArrayUtil;

using System.Collections.Generic;

namespace Lucene.Net.Index
{
    /**
     * This class accepts multiple added documents and directly
     * writes a single segment file.  It does this more
     * efficiently than creating a single segment per document
     * (with DocumentWriter) and doing standard merges on those
     * segments.
     *
     * Each added document is passed to the {@link DocConsumer},
     * which in turn processes the document and interacts with
     * other consumers in the indexing chain.  Certain
     * consumers, like {@link StoredFieldsWriter} and {@link
     * TermVectorsTermsWriter}, digest a document and
     * immediately write bytes to the "doc store" files (ie,
     * they do not consume RAM per document, except while they
     * are processing the document).
     *
     * Other consumers, eg {@link FreqProxTermsWriter} and
     * {@link NormsWriter}, buffer bytes in RAM and flush only
     * when a new segment is produced.

     * Once we have used our allowed RAM buffer, or the number
     * of added docs is large enough (in the case we are
     * flushing by doc count instead of RAM usage), we create a
     * real segment and flush it to the Directory.
     *
     * Threads:
     *
     * Multiple threads are allowed into addDocument at once.
     * There is an initial synchronized call to getThreadState
     * which allocates a ThreadState for this thread.  The same
     * thread will get the same ThreadState over time (thread
     * affinity) so that if there are consistent patterns (for
     * example each thread is indexing a different content
     * source) then we make better use of RAM.  Then
     * processDocument is called on that ThreadState without
     * synchronization (most of the "heavy lifting" is in this
     * call).  Finally the synchronized "finishDocument" is
     * called to flush changes to the directory.
     *
     * When flush is called by IndexWriter, or, we flush
     * internally when autoCommit=false, we forcefully idle all
     * threads and flush only once they are all idle.  This
     * means you can call flush with a given thread even while
     * other threads are actively adding/deleting documents.
     *
     *
     * Exceptions:
     *
     * Because this class directly updates in-memory posting
     * lists, and flushes stored fields and term vectors
     * directly to files in the directory, there are certain
     * limited times when an exception can corrupt this state.
     * For example, a disk full while flushing stored fields
     * leaves this file in a corrupt state.  Or, an OOM
     * exception while appending to the in-memory posting lists
     * can corrupt that posting list.  We call such exceptions
     * "aborting exceptions".  In these cases we must call
     * abort() to discard all docs added since the last flush.
     *
     * All other exceptions ("non-aborting exceptions") can
     * still partially update the index structures.  These
     * updates are consistent, but, they represent only a part
     * of the document seen up until the exception was hit.
     * When this happens, we immediately mark the document as
     * deleted so that the document is always atomically ("all
     * or none") added to the index.
     */

    sealed public class DocumentsWriter
    {
        internal IndexWriter writer;
        internal Directory directory;

        internal string segment;                         // Current segment we are working on
        private string docStoreSegment;         // Current doc-store segment we are writing
        private int docStoreOffset;                     // Current starting doc-store offset of current segment

        private int nextDocID;                          // Next docID to be added
        private int numDocsInRAM;                       // # docs buffered in RAM
        internal int numDocsInStore;                     // # docs written to doc stores

        // Max # ThreadState instances; if there are more threads
        // than this they share ThreadStates
        private const int MAX_THREAD_STATE = 5;
        private DocumentsWriterThreadState[] threadStates = new DocumentsWriterThreadState[0];
        private readonly Dictionary<SupportClass.ThreadClass, DocumentsWriterThreadState> threadBindings =
            new Dictionary<SupportClass.ThreadClass, DocumentsWriterThreadState>();

        private int pauseThreads;               // Non-zero when we need all threads to
        // pause (eg to flush)
        internal bool flushPending;                   // True when a thread has decided to flush
        internal bool bufferIsFull;                   // True when it's time to write segment
        private bool aborting;               // True if an abort is pending

        private DocFieldProcessor docFieldProcessor;

        internal System.IO.TextWriter infoStream;
        internal int maxFieldLength = IndexWriter.DEFAULT_MAX_FIELD_LENGTH;
        internal Similarity similarity;

        //internal List<object> newFiles;

        internal class DocState
        {
            internal DocumentsWriter docWriter;
            internal Analyzer analyzer;
            internal int maxFieldLength;
            internal System.IO.TextWriter infoStream;
            internal Similarity similarity;
            internal int docID;
            internal Document doc;
            internal string maxTermPrefix;

            // Only called by asserts
            public bool TestPoint(string name)
            {
                return docWriter.writer.TestPoint(name);
            }
        }

        internal class FlushState
        {
            internal DocumentsWriter docWriter;
            internal Directory directory;
            internal string segmentName;
            internal string docStoreSegmentName;
            internal int numDocsInRAM;
            internal int numDocsInStore;
            // emulate java.util.HashSet
            internal IDictionary<string, string> flushedFiles;

            public string SegmentFileName(string ext)
            {
                return segmentName + "." + ext;
            }
        }

        /** Consumer returns this on each doc.  This holds any
         *  state that must be flushed synchronized "in docID
         *  order".  We gather these and flush them in order. */
        internal abstract class DocWriter
        {
            internal DocWriter next;
            internal int docID;
            internal abstract void Finish();
            internal abstract void Abort();
            internal abstract long SizeInBytes();

            internal void SetNext(DocWriter next)
            {
                this.next = next;
            }
        }

        internal readonly DocConsumer consumer;

        // Deletes done after the last flush; these are discarded
        // on abort
        private BufferedDeletes deletesInRAM = new BufferedDeletes();

        // Deletes done before the last flush; these are still
        // kept on abort
        private BufferedDeletes deletesFlushed = new BufferedDeletes();

        // The max number of delete terms that can be buffered before
        // they must be flushed to disk.
        private int maxBufferedDeleteTerms = IndexWriter.DEFAULT_MAX_BUFFERED_DELETE_TERMS;

        // How much RAM we can use before flushing.  This is 0 if
        // we are flushing by doc count instead.
        private static readonly long DEFAULT_RAM_BUFFER_SIZE_B = (long)(IndexWriter.DEFAULT_RAM_BUFFER_SIZE_MB * 1024 * 1024);
        private long ramBufferSize = DEFAULT_RAM_BUFFER_SIZE_B;
        private long waitQueuePauseBytes = (long)(DEFAULT_RAM_BUFFER_SIZE_B * 0.1);
        private long waitQueueResumeBytes = (long)(DEFAULT_RAM_BUFFER_SIZE_B * 0.05);

        // If we've allocated 5% over our RAM budget, we then
        // free down to 95%
        private long freeTrigger = (long)(IndexWriter.DEFAULT_RAM_BUFFER_SIZE_MB * 1024 * 1024 * 1.05);
        private long freeLevel = (long)(IndexWriter.DEFAULT_RAM_BUFFER_SIZE_MB * 1024 * 1024 * 0.95);

        // Flush @ this number of docs.  If ramBufferSize is
        // non-zero we will flush by RAM usage instead.
        private int maxBufferedDocs = IndexWriter.DEFAULT_MAX_BUFFERED_DOCS;

        private int flushedDocCount;                      // How many docs already flushed to index

        internal void UpdateFlushedDocCount(int n)
        {
            lock (this) { flushedDocCount += n; }
        }
        internal int GetFlushedDocCount()
        {
            lock (this) { return flushedDocCount; }
        }
        internal void SetFlushedDocCount(int n)
        {
            lock (this) { flushedDocCount = n; }
        }

        private bool closed;

        internal DocumentsWriter(Directory directory, IndexWriter writer)
        {
            this.directory = directory;
            this.writer = writer;
            this.similarity = writer.GetSimilarity();
            flushedDocCount = writer.MaxDoc();

            byteBlockAllocator = new ByteBlockAllocator(this);
            waitQueue = new WaitQueue(this);

            /*
              This is the current indexing chain:

              DocConsumer / DocConsumerPerThread
                --> code: DocFieldProcessor / DocFieldProcessorPerThread
                  --> DocFieldConsumer / DocFieldConsumerPerThread / DocFieldConsumerPerField
                    --> code: DocFieldConsumers / DocFieldConsumersPerThread / DocFieldConsumersPerField
                      --> code: DocInverter / DocInverterPerThread / DocInverterPerField
                        --> InvertedDocConsumer / InvertedDocConsumerPerThread / InvertedDocConsumerPerField
                          --> code: TermsHash / TermsHashPerThread / TermsHashPerField
                            --> TermsHashConsumer / TermsHashConsumerPerThread / TermsHashConsumerPerField
                              --> code: FreqProxTermsWriter / FreqProxTermsWriterPerThread / FreqProxTermsWriterPerField
                              --> code: TermVectorsTermsWriter / TermVectorsTermsWriterPerThread / TermVectorsTermsWriterPerField
                        --> InvertedDocEndConsumer / InvertedDocConsumerPerThread / InvertedDocConsumerPerField
                          --> code: NormsWriter / NormsWriterPerThread / NormsWriterPerField
                      --> code: StoredFieldsWriter / StoredFieldsWriterPerThread / StoredFieldsWriterPerField
            */

            // TODO FI: this should be something the user can pass in
            // Build up indexing chain:
            TermsHashConsumer termVectorsWriter = new TermVectorsTermsWriter(this);
            TermsHashConsumer freqProxWriter = new FreqProxTermsWriter();

            InvertedDocConsumer termsHash = new TermsHash(this, true, freqProxWriter,
                                                                 new TermsHash(this, false, termVectorsWriter, null));
            NormsWriter normsWriter = new NormsWriter();
            DocInverter docInverter = new DocInverter(termsHash, normsWriter);
            StoredFieldsWriter fieldsWriter = new StoredFieldsWriter(this);
            DocFieldConsumers docFieldConsumers = new DocFieldConsumers(docInverter, fieldsWriter);
            consumer = docFieldProcessor = new DocFieldProcessor(this, docFieldConsumers);
        }

        /** Returns true if any of the fields in the current
         *  buffered docs have omitTf==false */
        internal bool HasProx()
        {
            return docFieldProcessor.fieldInfos.HasProx();
        }

        /** If non-null, various details of indexing are printed
         *  here. */
        internal void SetInfoStream(System.IO.TextWriter infoStream)
        {
            lock (this)
            {
                this.infoStream = infoStream;
                for (int i = 0; i < threadStates.Length; i++)
                    threadStates[i].docState.infoStream = infoStream;
            }
        }

        internal void SetMaxFieldLength(int maxFieldLength)
        {
            lock (this)
            {
                this.maxFieldLength = maxFieldLength;
                for (int i = 0; i < threadStates.Length; i++)
                    threadStates[i].docState.maxFieldLength = maxFieldLength;
            }
        }

        internal void SetSimilarity(Similarity similarity)
        {
            lock (this)
            {
                this.similarity = similarity;
                for (int i = 0; i < threadStates.Length; i++)
                    threadStates[i].docState.similarity = similarity;
            }
        }

        /** Set how much RAM we can use before flushing. */
        internal void SetRAMBufferSizeMB(double mb)
        {
            lock (this)
            {
                if (mb == IndexWriter.DISABLE_AUTO_FLUSH)
                {
                    ramBufferSize = IndexWriter.DISABLE_AUTO_FLUSH;
                    waitQueuePauseBytes = 4 * 1024 * 1024;
                    waitQueueResumeBytes = 2 * 1024 * 1024;
                }
                else
                {
                    ramBufferSize = (long)(mb * 1024 * 1024);
                    waitQueuePauseBytes = (long)(ramBufferSize * 0.1);
                    waitQueueResumeBytes = (long)(ramBufferSize * 0.05);
                    freeTrigger = (long)(1.05 * ramBufferSize);
                    freeLevel = (long)(0.95 * ramBufferSize);
                }
            }
        }

        internal double GetRAMBufferSizeMB()
        {
            lock (this)
            {
                if (ramBufferSize == IndexWriter.DISABLE_AUTO_FLUSH)
                {
                    return ramBufferSize;
                }
                else
                {
                    return ramBufferSize / 1024F / 1024F;
                }
            }
        }

        /** Set max buffered docs, which means we will flush by
         *  doc count instead of by RAM usage. */
        internal void SetMaxBufferedDocs(int count)
        {
            maxBufferedDocs = count;
        }

        internal int GetMaxBufferedDocs()
        {
            return maxBufferedDocs;
        }

        /** Get current segment name we are writing. */
        internal string GetSegment()
        {
            return segment;
        }

        /** Returns how many docs are currently buffered in RAM. */
        internal int GetNumDocsInRAM()
        {
            return numDocsInRAM;
        }

        /** Returns the current doc store segment we are writing
         *  to.  This will be the same as segment when autoCommit
         *  * is true. */
        internal string GetDocStoreSegment()
        {
            lock (this)
            {
                return docStoreSegment;
            }
        }

        /** Returns the doc offset into the shared doc store for
         *  the current buffered docs. */
        internal int GetDocStoreOffset()
        {
            return docStoreOffset;
        }

        /** Closes the current open doc stores an returns the doc
         *  store segment name.  This returns null if there are *
         *  no buffered documents. */
        internal string CloseDocStore()
        {
            lock (this)
            {
                System.Diagnostics.Debug.Assert(AllThreadsIdle());

                if (infoStream != null)
                    Message("closeDocStore: " + openFiles.Count + " files to flush to segment " + docStoreSegment + " numDocs=" + numDocsInStore);

                bool success = false;

                try
                {
                    InitFlushState(true);
                    closedFiles.Clear();

                    consumer.closeDocStore(flushState);
                    System.Diagnostics.Debug.Assert(0 == openFiles.Count);

                    string s = docStoreSegment;
                    docStoreSegment = null;
                    docStoreOffset = 0;
                    numDocsInStore = 0;
                    success = true;
                    return s;
                }
                finally
                {
                    if (!success)
                    {
                        Abort();
                    }
                }
            }
        }

        private List<string> abortedFiles;               // List<object> of files that were written before last abort()

        private FlushState flushState;

        internal List<string> AbortedFiles()
        {
            return abortedFiles;
        }

        internal void Message(string message)
        {
            writer.Message("DW: " + message);
        }

        internal readonly List<string> openFiles = new List<string>();
        internal readonly List<string> closedFiles = new List<string>();

        /* Returns ICollection<object> of files in use by this instance,
         * including any flushed segments. */
        internal List<string> OpenFiles()
        {
            lock (this) { return new List<string>(openFiles); }
        }

        internal List<string> ClosedFiles()
        {
            lock (this) { return new List<string>(closedFiles); }
        }

        internal void AddOpenFile(string name)
        {
            lock (this)
            {
                System.Diagnostics.Debug.Assert(!openFiles.Contains(name));
                openFiles.Add(name);
            }
        }

        internal void RemoveOpenFile(string name)
        {
            lock (this)
            {
                System.Diagnostics.Debug.Assert(openFiles.Contains(name));
                openFiles.Remove(name);
                closedFiles.Add(name);
            }
        }

        internal void SetAborting()
        {
            lock (this)
            {
                aborting = true;
            }
        }

        /** Called if we hit an exception at a bad time (when
         *  updating the index files) and must discard all
         *  currently buffered docs.  This resets our state,
         *  discarding any docs added since last flush. */
        internal void Abort()
        {
            lock (this)
            {
                try
                {
                    Message("docWriter: now abort");

                    // Forcefully remove waiting ThreadStates from line
                    waitQueue.Abort();

                    // Wait for all other threads to finish with
                    // DocumentsWriter:
                    PauseAllThreads();

                    try
                    {

                        System.Diagnostics.Debug.Assert(0 == waitQueue.numWaiting);

                        waitQueue.waitingBytes = 0;

                        try
                        {
                            abortedFiles = OpenFiles();
                        }
                        catch (System.Exception)
                        {
                            abortedFiles = null;
                        }

                        deletesInRAM.Clear();

                        openFiles.Clear();

                        for (int i = 0; i < threadStates.Length; i++)
                            try
                            {
                                threadStates[i].consumer.abort();
                            }
                            catch (System.Exception)
                            {
                            }

                        try
                        {
                            consumer.abort();
                        }
                        catch (System.Exception)
                        {
                        }

                        docStoreSegment = null;
                        numDocsInStore = 0;
                        docStoreOffset = 0;

                        // Reset all postings data
                        DoAfterFlush();

                    }
                    finally
                    {
                        ResumeAllThreads();
                    }
                }
                finally
                {
                    aborting = false;
                    System.Threading.Monitor.PulseAll(this);
                }
            }
        }

        /** Reset after a flush */
        private void DoAfterFlush()
        {
            // All ThreadStates should be idle when we are called
            System.Diagnostics.Debug.Assert(AllThreadsIdle());
            threadBindings.Clear();
            waitQueue.Reset();
            segment = null;
            numDocsInRAM = 0;
            nextDocID = 0;
            bufferIsFull = false;
            flushPending = false;
            for (int i = 0; i < threadStates.Length; i++)
                threadStates[i].doAfterFlush();
            numBytesUsed = 0;
        }

        // Returns true if an abort is in progress
        internal bool PauseAllThreads()
        {
            lock (this)
            {
                pauseThreads++;
                while (!AllThreadsIdle())
                {
                    try
                    {
                        System.Threading.Monitor.Wait(this);
                    }
                    catch (System.Threading.ThreadInterruptedException)
                    {
                        SupportClass.ThreadClass.Current().Interrupt();
                    }
                }

                return aborting;
            }
        }

        internal void ResumeAllThreads()
        {
            lock (this)
            {
                pauseThreads--;
                System.Diagnostics.Debug.Assert(pauseThreads >= 0);
                if (0 == pauseThreads)
                    System.Threading.Monitor.PulseAll(this);
            }
        }

        private bool AllThreadsIdle()
        {
            lock (this)
            {
                for (int i = 0; i < threadStates.Length; i++)
                    if (!threadStates[i].isIdle)
                        return false;
                return true;
            }
        }

        private void InitFlushState(bool onlyDocStore)
        {
            lock (this)
            {
                InitSegmentName(onlyDocStore);

                if (flushState == null)
                {
                    flushState = new FlushState();
                    flushState.directory = directory;
                    flushState.docWriter = this;
                }

                flushState.docStoreSegmentName = docStoreSegment;
                flushState.segmentName = segment;
                flushState.numDocsInRAM = numDocsInRAM;
                flushState.numDocsInStore = numDocsInStore;
                flushState.flushedFiles = new System.Collections.Generic.Dictionary<string,string>();
            }
        }

        /** Flush all pending docs to a new segment */
        internal int Flush(bool closeDocStore)
        {
            lock (this)
            {
                System.Diagnostics.Debug.Assert(AllThreadsIdle());

                System.Diagnostics.Debug.Assert(numDocsInRAM > 0);

                System.Diagnostics.Debug.Assert(nextDocID == numDocsInRAM);
                System.Diagnostics.Debug.Assert(waitQueue.numWaiting == 0);
                System.Diagnostics.Debug.Assert(waitQueue.waitingBytes == 0);

                InitFlushState(false);

                docStoreOffset = numDocsInStore;

                if (infoStream != null)
                    Message("flush postings as segment " + flushState.segmentName + " numDocs=" + numDocsInRAM);

                bool success = false;

                try
                {

                    if (closeDocStore)
                    {
                        System.Diagnostics.Debug.Assert(flushState.docStoreSegmentName != null);
                        System.Diagnostics.Debug.Assert(flushState.docStoreSegmentName.Equals(flushState.segmentName));
                        CloseDocStore();
                        flushState.numDocsInStore = 0;
                    }

                    System.Collections.Generic.Dictionary<object, object> threads = new System.Collections.Generic.Dictionary<object, object>();
                    for (int i = 0; i < threadStates.Length; i++)
                        threads[threadStates[i].consumer] = threadStates[i].consumer;
                    consumer.Flush(threads.Keys, flushState);

                    if (infoStream != null)
                    {
                        long newSegmentSize = SegmentSize(flushState.segmentName);
                        string message = string.Format(nf, "  oldRAMSize={0:d} newFlushedSize={1:d} docs/MB={2:f} new/old={3:%}",
                            new object[] { numBytesUsed, newSegmentSize, (numDocsInRAM / (newSegmentSize / 1024.0 / 1024.0)), (newSegmentSize / numBytesUsed) });
                        Message(message);
                    }

                    flushedDocCount += flushState.numDocsInRAM;

                    DoAfterFlush();

                    success = true;

                }   
                finally
                {
                    if (!success)
                    {
                        Abort();
                    }
                }

                System.Diagnostics.Debug.Assert(waitQueue.waitingBytes == 0);

                return flushState.numDocsInRAM;
            }
        }

        /** Build compound file for the segment we just flushed */
        internal void CreateCompoundFile(string segment)
        {

            CompoundFileWriter cfsWriter = new CompoundFileWriter(directory, segment + "." + IndexFileNames.COMPOUND_FILE_EXTENSION);
            IEnumerator<string> it = flushState.flushedFiles.Keys.GetEnumerator();
            while (it.MoveNext())
                cfsWriter.AddFile(it.Current);

            // Perform the merge
            cfsWriter.Close();
        }

        /** Set flushPending if it is not already set and returns
         *  whether it was set. This is used by IndexWriter to
         *  trigger a single flush even when multiple threads are
         *  trying to do so. */
        internal bool SetFlushPending()
        {
            lock (this)
            {
                if (flushPending)
                    return false;
                else
                {
                    flushPending = true;
                    return true;
                }
            }
        }

        internal void ClearFlushPending()
        {
            lock (this)
            {
                flushPending = false;
            }
        }

        internal void PushDeletes()
        {
            lock (this) { deletesFlushed.Update(deletesInRAM); }
        }

        internal void Close()
        {
            lock (this)
            {
                closed = true;
                System.Threading.Monitor.PulseAll(this);
            }
        }

        internal void InitSegmentName(bool onlyDocStore)
        {
            lock (this)
            {
                if (segment == null && (!onlyDocStore || docStoreSegment == null))
                {
                    segment = writer.NewSegmentName();
                    System.Diagnostics.Debug.Assert(numDocsInRAM == 0);
                }
                if (docStoreSegment == null)
                {
                    docStoreSegment = segment;
                    System.Diagnostics.Debug.Assert(numDocsInStore == 0);
                }
            }
        }

        /** Returns a free (idle) ThreadState that may be used for
         * indexing this one document.  This call also pauses if a
         * flush is pending.  If delTerm is non-null then we
         * buffer this deleted term after the thread state has
         * been acquired. */
        internal DocumentsWriterThreadState GetThreadState(Document doc, Term delTerm)
        {
            lock (this)
            {
                // First, find a thread state.  If this thread already
                // has affinity to a specific ThreadState, use that one
                // again.
                DocumentsWriterThreadState state;
                if (threadBindings.ContainsKey(SupportClass.ThreadClass.Current()))
                {
                    state = threadBindings[SupportClass.ThreadClass.Current()];
                }
                else
                {

                    // First time this thread has called us since last
                    // flush.  Find the least loaded thread state:
                    DocumentsWriterThreadState minThreadState = null;
                    for (int i = 0; i < threadStates.Length; i++)
                    {
                        DocumentsWriterThreadState ts = threadStates[i];
                        if (minThreadState == null || ts.numThreads < minThreadState.numThreads)
                            minThreadState = ts;
                    }
                    if (minThreadState != null && (minThreadState.numThreads == 0 || threadStates.Length >= MAX_THREAD_STATE))
                    {
                        state = minThreadState;
                        state.numThreads++;
                    }
                    else
                    {
                        // Just create a new "private" thread state
                        DocumentsWriterThreadState[] newArray = new DocumentsWriterThreadState[1 + threadStates.Length];
                        if (threadStates.Length > 0)
                            System.Array.Copy(threadStates, 0, newArray, 0, threadStates.Length);
                        state = newArray[threadStates.Length] = new DocumentsWriterThreadState(this);
                        threadStates = newArray;
                    }
                    threadBindings[SupportClass.ThreadClass.Current()] = state;
                }

                // Next, wait until my thread state is idle (in case
                // it's shared with other threads) and for threads to
                // not be paused nor a flush pending:
                WaitReady(state);

                // Allocate segment name if this is the first doc since
                // last flush:
                InitSegmentName(false);

                state.isIdle = false;

                bool success = false;
                try
                {
                    state.docState.docID = nextDocID;

                    System.Diagnostics.Debug.Assert(writer.TestPoint("DocumentsWriter.ThreadState.init start"));

                    if (delTerm != null)
                    {
                        AddDeleteTerm(delTerm, state.docState.docID);
                        state.doFlushAfter = TimeToFlushDeletes();
                    }

                    System.Diagnostics.Debug.Assert(writer.TestPoint("DocumentsWriter.ThreadState.init after delTerm"));

                    nextDocID++;
                    numDocsInRAM++;

                    // We must at this point commit to flushing to ensure we
                    // always get N docs when we flush by doc count, even if
                    // > 1 thread is adding documents:
                    if (!flushPending &&
                        maxBufferedDocs != IndexWriter.DISABLE_AUTO_FLUSH
                        && numDocsInRAM >= maxBufferedDocs)
                    {
                        flushPending = true;
                        state.doFlushAfter = true;
                    }

                    success = true;
                }
                finally
                {
                    if (!success)
                    {
                        // Forcefully idle this ThreadState:
                        state.isIdle = true;
                        System.Threading.Monitor.PulseAll(this);
                        if (state.doFlushAfter)
                        {
                            state.doFlushAfter = false;
                            flushPending = false;
                        }
                    }
                }

                return state;
            }
        }

        /** Returns true if the caller (IndexWriter) should now
         * flush. */
        internal bool AddDocument(Document doc, Analyzer analyzer)
        {
            return UpdateDocument(doc, analyzer, null);
        }

        internal bool UpdateDocument(Term t, Document doc, Analyzer analyzer)
        {
            return UpdateDocument(doc, analyzer, t);
        }

        internal bool UpdateDocument(Document doc, Analyzer analyzer, Term delTerm)
        {

            // This call is synchronized but fast
            DocumentsWriterThreadState state = GetThreadState(doc, delTerm);

            DocState docState = state.docState;
            docState.doc = doc;
            docState.analyzer = analyzer;

            bool success = false;
            try
            {
                // This call is not synchronized and does all the
                // work
                DocWriter perDoc = state.consumer.processDocument();

                // This call is synchronized but fast
                FinishDocument(state, perDoc);
                success = true;
            }
            // {{dougsale-2.4.0}}:
            // Transferring control from within a finally block is not allowed in C#.
            // Since 'success' is a local variable (each thread will have its own copy),
            // the only way the code in this finally block will be executed is if an exception
            // occurs in 'state.consumer.processDocument()' or 'finishDocument(...)'
            // So, this 'finally' may be changed to a 'catch', provided that we rethrow the 
            // exception after executing the 'finally' code block
            //finally
            catch (System.Exception x)
            {
                // this check is not necessary when the code is written properly
                // as a 'catch' instead of a 'finally' with the boolean check 
                if (!success)
                {
                    lock (this)
                    {

                        if (aborting)
                        {
                            state.isIdle = true;
                            System.Threading.Monitor.PulseAll(this);
                            Abort();
                        }
                        else
                        {
                            skipDocWriter.docID = docState.docID;
                            bool success2 = false;
                            try
                            {
                                waitQueue.Add(skipDocWriter);
                                success2 = true;
                            }
                            // {{dougsale-2.4.0}}:
                            // Transferring control from within a finally block is not allowed in C#.
                            // Since 'success2' is a local variable and each thread will have its own copy,
                            // the only way the code in this finally block will be executed is if an exception
                            // occurs in 'waitQueue.Add(...)'
                            // So, this 'finally' may be changed to a 'catch'
                            catch (System.Exception)
                            {
                                // i don't see why this check is necessary - each thread has it's own stack
                                // and own copy of 'success2'...
                                if (!success2)
                                {
                                    state.isIdle = true;
                                    System.Threading.Monitor.PulseAll(this);
                                    Abort();
                                    return false;
                                }
                            }

                            state.isIdle = true;
                            System.Threading.Monitor.PulseAll(this);

                            // If this thread state had decided to flush, we
                            // must clear it so another thread can flush
                            if (state.doFlushAfter)
                            {
                                state.doFlushAfter = false;
                                flushPending = false;
                                System.Threading.Monitor.PulseAll(this);
                            }

                            // Immediately mark this document as deleted
                            // since likely it was partially added.  This
                            // keeps indexing as "all or none" (atomic) when
                            // adding a document:
                            AddDeleteDocID(state.docState.docID);
                        }
                    }
                }
                throw x;
            }

            return state.doFlushAfter || TimeToFlushDeletes();
        }

        // for testing
        internal int GetNumBufferedDeleteTerms()
        {
            lock (this) { return deletesInRAM.numTerms; }
        }

        // for testing
        internal IDictionary<object, object> GetBufferedDeleteTerms()
        {
            lock (this) { return deletesInRAM.terms; }
        }

        /** Called whenever a merge has completed and the merged segments had deletions */
        internal void RemapDeletes(SegmentInfos infos, int[][] docMaps, int[] delCounts, MergePolicy.OneMerge merge, int mergeDocCount)
        {
            lock (this)
            {
                if (docMaps == null)
                    // The merged segments had no deletes so docIDs did not change and we have nothing to do
                    return;
                MergeDocIDRemapper mapper = new MergeDocIDRemapper(infos, docMaps, delCounts, merge, mergeDocCount);
                deletesInRAM.Remap(mapper, infos, docMaps, delCounts, merge, mergeDocCount);
                deletesFlushed.Remap(mapper, infos, docMaps, delCounts, merge, mergeDocCount);
                flushedDocCount -= mapper.docShift;
            }
        }

        private void WaitReady(DocumentsWriterThreadState state)
        {
            lock (this)
            {
                while (!closed && ((state != null && !state.isIdle) || pauseThreads != 0 || flushPending || aborting))
                {
                    try
                    {
                        System.Threading.Monitor.Wait(this);
                    }
                    catch (System.Threading.ThreadInterruptedException)
                    {
                        SupportClass.ThreadClass.Current().Interrupt();
                    }
                }

                if (closed)
                    throw new AlreadyClosedException("this IndexWriter is closed");
            }
        }

        internal bool BufferDeleteTerms(Term[] terms)
        {
            lock (this)
            {
                WaitReady(null);
                for (int i = 0; i < terms.Length; i++)
                    AddDeleteTerm(terms[i], numDocsInRAM);
                return TimeToFlushDeletes();
            }
        }

        internal bool BufferDeleteTerm(Term term)
        {
            lock (this)
            {
                WaitReady(null);
                AddDeleteTerm(term, numDocsInRAM);
                return TimeToFlushDeletes();
            }
        }

        internal bool BufferDeleteQueries(Query[] queries)
        {
            lock (this)
            {
                WaitReady(null);
                for (int i = 0; i < queries.Length; i++)
                    AddDeleteQuery(queries[i], numDocsInRAM);
                return TimeToFlushDeletes();
            }
        }

        internal bool BufferDeleteQuery(Query query)
        {
            lock (this)
            {
                WaitReady(null);
                AddDeleteQuery(query, numDocsInRAM);
                return TimeToFlushDeletes();
            }
        }

        internal bool DeletesFull()
        {
            lock (this)
            {
                return maxBufferedDeleteTerms != IndexWriter.DISABLE_AUTO_FLUSH
                  && ((deletesInRAM.numTerms + deletesInRAM.queries.Count + deletesInRAM.docIDs.Count) >= maxBufferedDeleteTerms);
            }
        }

        private bool TimeToFlushDeletes()
        {
            lock (this)
            {
                return (bufferIsFull || DeletesFull()) && SetFlushPending();
            }
        }

        internal void SetMaxBufferedDeleteTerms(int maxBufferedDeleteTerms)
        {
            this.maxBufferedDeleteTerms = maxBufferedDeleteTerms;
        }

        internal int GetMaxBufferedDeleteTerms()
        {
            return maxBufferedDeleteTerms;
        }

        internal bool HasDeletes()
        {
            lock (this) { return deletesFlushed.Any(); }
        }

        internal bool ApplyDeletes(SegmentInfos infos)
        {
            lock (this)
            {
                if (!HasDeletes())
                    return false;

                if (infoStream != null)
                    Message("apply " + deletesFlushed.numTerms + " buffered deleted terms and " +
                            deletesFlushed.docIDs.Count + " deleted docIDs and " +
                            deletesFlushed.queries.Count + " deleted queries on " +
                            +infos.Count + " segments.");

                int infosEnd = infos.Count;

                int docStart = 0;
                bool any = false;
                for (int i = 0; i < infosEnd; i++)
                {
                    IndexReader reader = SegmentReader.Get(infos.Info(i), false);
                    bool success = false;
                    try
                    {
                        any |= ApplyDeletes(reader, docStart);
                        docStart += reader.MaxDoc();
                        success = true;
                    }
                    finally
                    {
                        if (reader != null)
                        {
                            try
                            {
                                if (success)
                                    reader.DoCommit();
                            }
                            finally
                            {
                                reader.DoClose();
                            }
                        }
                    }
                }

                deletesFlushed.Clear();

                return any;
            }
        }

        // Apply buffered delete terms, queries and docIDs to the
        // provided reader
        private bool ApplyDeletes(IndexReader reader, int docIDStart)
        {
            lock (this)
            {
                int docEnd = docIDStart + reader.MaxDoc();
                bool any = false;

                // Delete by term
                IEnumerator<KeyValuePair<object, object>> iter = deletesFlushed.terms.GetEnumerator();
                while (iter.MoveNext())
                {
                    KeyValuePair<object, object> entry = (KeyValuePair<object, object>)iter.Current;
                    Term term = (Term)entry.Key;

                    TermDocs docs = reader.TermDocs(term);
                    if (docs != null)
                    {
                        int limit = ((BufferedDeletes.Num)entry.Value).GetNum();
                        try
                        {
                            while (docs.Next())
                            {
                                int docID = docs.Doc();
                                if (docIDStart + docID >= limit)
                                    break;
                                reader.DeleteDocument(docID);
                                any = true;
                            }
                        }
                        finally
                        {
                            docs.Close();
                        }
                    }
                }

                // Delete by docID
                IEnumerator<object> iter2 = deletesFlushed.docIDs.GetEnumerator();
                while (iter2.MoveNext())
                {
                    int docID = (int)iter2.Current;
                    if (docID >= docIDStart && docID < docEnd)
                    {
                        reader.DeleteDocument(docID - docIDStart);
                        any = true;
                    }
                }

                // Delete by query
                IndexSearcher searcher = new IndexSearcher(reader);
                iter = deletesFlushed.queries.GetEnumerator();
                while (iter.MoveNext())
                {
                    KeyValuePair<object, object> entry = (KeyValuePair<object, object>)iter.Current;
                    Query query = (Query)entry.Key;
                    int limit = (int)entry.Value;
                    Weight weight = query.Weight(searcher);
                    Scorer scorer = weight.Scorer(reader);
                    while (scorer.Next())
                    {
                        int docID = scorer.Doc();
                        if (docIDStart + docID >= limit)
                            break;
                        reader.DeleteDocument(docID);
                        any = true;
                    }
                }
                searcher.Close();
                return any;
            }
        }

        // Buffer a term in bufferedDeleteTerms, which records the
        // current number of documents buffered in ram so that the
        // delete term will be applied to those documents as well
        // as the disk segments.
        private void AddDeleteTerm(Term term, int docCount)
        {
            lock (this)
            {
                //BufferedDeletes.Num num = (BufferedDeletes.Num)deletesInRAM.terms[term];
                BufferedDeletes.Num num = deletesInRAM.terms.ContainsKey(term)? (BufferedDeletes.Num)deletesInRAM.terms[term] : null;
                int docIDUpto = flushedDocCount + docCount;
                if (num == null)
                    deletesInRAM.terms[term] = new BufferedDeletes.Num(docIDUpto);
                else
                    num.SetNum(docIDUpto);
                deletesInRAM.numTerms++;
            }
        }

        // Buffer a specific docID for deletion.  Currently only
        // used when we hit a exception when adding a document
        private void AddDeleteDocID(int docID)
        {
            lock (this) { deletesInRAM.docIDs.Add(flushedDocCount + docID); }
        }

        private void AddDeleteQuery(Query query, int docID)
        {
            lock (this) { deletesInRAM.queries[query] = flushedDocCount + docID; }
        }

        internal bool DoBalanceRAM()
        {
            lock (this) { return ramBufferSize != IndexWriter.DISABLE_AUTO_FLUSH && !bufferIsFull && (numBytesUsed >= ramBufferSize || numBytesAlloc >= freeTrigger); }
        }

        /** Does the synchronized work to finish/flush the
         *  inverted document. */
        private void FinishDocument(DocumentsWriterThreadState perThread, DocWriter docWriter)
        {

            if (DoBalanceRAM())
                // Must call this w/o holding synchronized(this) else
                // we'll hit deadlock:
                BalanceRAM();

            lock (this)
            {

                System.Diagnostics.Debug.Assert(docWriter == null || docWriter.docID == perThread.docState.docID);


                if (aborting)
                {

                    // We are currently aborting, and another thread is
                    // waiting for me to become idle.  We just forcefully
                    // idle this threadState; it will be fully reset by
                    // abort()
                    if (docWriter != null)
                        try
                        {
                            docWriter.Abort();
                        }
                        catch (System.Exception)
                        {
                        }

                    perThread.isIdle = true;
                    System.Threading.Monitor.PulseAll(this);
                    return;
                }

                bool doPause;

                if (docWriter != null)
                    doPause = waitQueue.Add(docWriter);
                else
                {
                    skipDocWriter.docID = perThread.docState.docID;
                    doPause = waitQueue.Add(skipDocWriter);
                }

                if (doPause)
                    WaitForWaitQueue();

                if (bufferIsFull && !flushPending)
                {
                    flushPending = true;
                    perThread.doFlushAfter = true;
                }

                perThread.isIdle = true;
                System.Threading.Monitor.PulseAll(this);
            }
        }

        internal void WaitForWaitQueue()
        {
            lock (this)
            {
                do
                {
                    try
                    {
                        System.Threading.Monitor.Wait(this);
                    }
                    catch (System.Threading.ThreadInterruptedException)
                    {
                        SupportClass.ThreadClass.Current().Interrupt();
                    }
                } while (!waitQueue.DoResume());
            }
        }

        internal class SkipDocWriter : DocWriter
        {
            internal override void Finish()
            {
            }
            internal override void Abort()
            {
            }
            internal override long SizeInBytes()
            {
                return 0;
            }
        }

        internal readonly SkipDocWriter skipDocWriter = new SkipDocWriter();

        internal long GetRAMUsed()
        {
            return numBytesUsed;
        }

        internal long numBytesAlloc;
        internal long numBytesUsed;

        internal System.Globalization.NumberFormatInfo nf = System.Globalization.CultureInfo.CurrentCulture.NumberFormat;

        // TODO FI: this is not flexible -- we can't hardwire
        // extensions in here:
        private long SegmentSize(string segmentName)
        {
            // Used only when infoStream != null
            System.Diagnostics.Debug.Assert(infoStream != null);

            long size = directory.FileLength(segmentName + ".tii") +
              directory.FileLength(segmentName + ".tis") +
              directory.FileLength(segmentName + ".frq") +
              directory.FileLength(segmentName + ".prx");

            string normFileName = segmentName + ".nrm";
            if (directory.FileExists(normFileName))
                size += directory.FileLength(normFileName);

            return size;
        }

        // Coarse estimates used to measure RAM usage of buffered deletes
        internal const int object_HEADER_BYTES = 8;
        internal const int POINTER_NUM_BYTE = 4;
        internal const int INT_NUM_BYTE = 4;
        internal const int CHAR_NUM_BYTE = 2;

        /* Initial chunks size of the shared byte[] blocks used to
           store postings data */
        internal const int BYTE_BLOCK_SHIFT = 15;
        internal static readonly int BYTE_BLOCK_SIZE = (int)(1 << BYTE_BLOCK_SHIFT);
        public static readonly int BYTE_BLOCK_SIZE_For_NUnit_Test = (int)(1 << BYTE_BLOCK_SHIFT);
        internal static readonly int BYTE_BLOCK_MASK = BYTE_BLOCK_SIZE - 1;
        internal static readonly int BYTE_BLOCK_NOT_MASK = ~BYTE_BLOCK_MASK;

        internal class ByteBlockAllocator : ByteBlockPool.Allocator
        {
            internal List<byte[]> freeByteBlocks = new List<byte[]>();

            private DocumentsWriter enclosing_instance;
            internal ByteBlockAllocator(DocumentsWriter enclosing_instance)
            {
                this.enclosing_instance = enclosing_instance;
            }

            /* Allocate another byte[] from the shared pool */
            public override byte[] GetByteBlock(bool trackAllocations)
            {
                lock (enclosing_instance)
                {
                    int size = freeByteBlocks.Count;
                    byte[] b;
                    if (0 == size)
                    {
                        // Always record a block allocated, even if
                        // trackAllocations is false.  This is necessary
                        // because this block will be shared between
                        // things that don't track allocations (term
                        // vectors) and things that do (freq/prox
                        // postings).
                        enclosing_instance.numBytesAlloc += BYTE_BLOCK_SIZE;
                        b = new byte[BYTE_BLOCK_SIZE];
                    }
                    else
                    {
                        b = freeByteBlocks[size - 1];
                        freeByteBlocks.RemoveAt(size - 1);
                    }
                    if (trackAllocations)
                        enclosing_instance.numBytesUsed += BYTE_BLOCK_SIZE;
                    System.Diagnostics.Debug.Assert(enclosing_instance.numBytesUsed <= enclosing_instance.numBytesAlloc);
                    return b;
                }
            }

            /* Return byte[]'s to the pool */
            public override void RecycleByteBlocks(byte[][] blocks, int start, int end)
            {
                lock (enclosing_instance)
                {
                    for (int i = start; i < end; i++)
                        freeByteBlocks.Add(blocks[i]);
                }
            }
        }

        /* Initial chunks size of the shared int[] blocks used to
           store postings data */
        internal const int INT_BLOCK_SHIFT = 13;
        internal static readonly int INT_BLOCK_SIZE = (int)(1 << INT_BLOCK_SHIFT);
        internal static readonly int INT_BLOCK_MASK = INT_BLOCK_SIZE - 1;

        private List<int[]> freeIntBlocks = new List<int[]>();

        /* Allocate another int[] from the shared pool */
        internal int[] GetIntBlock(bool trackAllocations)
        {
            lock (this)
            {
                int size = freeIntBlocks.Count;
                int[] b;
                if (0 == size)
                {
                    // Always record a block allocated, even if
                    // trackAllocations is false.  This is necessary
                    // because this block will be shared between
                    // things that don't track allocations (term
                    // vectors) and things that do (freq/prox
                    // postings).
                    numBytesAlloc += INT_BLOCK_SIZE * INT_NUM_BYTE;
                    b = new int[INT_BLOCK_SIZE];
                }
                else
                {
                    b = freeIntBlocks[size - 1];
                    freeIntBlocks.RemoveAt(size - 1);
                }
                if (trackAllocations)
                    numBytesUsed += INT_BLOCK_SIZE * INT_NUM_BYTE;
                System.Diagnostics.Debug.Assert(numBytesUsed <= numBytesAlloc);
                return b;
            }
        }

        internal void BytesAllocated(long numBytes)
        {
            lock (this)
            {
                numBytesAlloc += numBytes;
                System.Diagnostics.Debug.Assert(numBytesUsed <= numBytesAlloc);
            }
        }

        internal void BytesUsed(long numBytes)
        {
            lock (this)
            {
                numBytesUsed += numBytes;
                System.Diagnostics.Debug.Assert(numBytesUsed <= numBytesAlloc);
            }
        }

        /* Return int[]s to the pool */
        internal void RecycleIntBlocks(int[][] blocks, int start, int end)
        {
            lock (this)
            {
                for (int i = start; i < end; i++)
                    freeIntBlocks.Add(blocks[i]);
            }
        }

        internal ByteBlockAllocator byteBlockAllocator;

        /* Initial chunk size of the shared char[] blocks used to
           store term text */
        internal const int CHAR_BLOCK_SHIFT = 14;
        public /* for testing, was: internal */ static readonly int CHAR_BLOCK_SIZE = (int)(1 << CHAR_BLOCK_SHIFT);
        internal static readonly int CHAR_BLOCK_MASK = CHAR_BLOCK_SIZE - 1;

        internal static readonly int MAX_TERM_LENGTH = CHAR_BLOCK_SIZE - 1;

        private List<char[]> freeCharBlocks = new List<char[]>();

        /* Allocate another char[] from the shared pool */
        internal char[] GetCharBlock()
        {
            lock (this)
            {
                int size = freeCharBlocks.Count;
                char[] c;
                if (0 == size)
                {
                    numBytesAlloc += CHAR_BLOCK_SIZE * CHAR_NUM_BYTE;
                    c = new char[CHAR_BLOCK_SIZE];
                }
                else
                {
                    c = freeCharBlocks[size - 1];
                    freeCharBlocks.RemoveAt(size - 1);
                }
                // We always track allocations of char blocks, for now,
                // because nothing that skips allocation tracking
                // (currently only term vectors) uses its own char
                // blocks.
                numBytesUsed += CHAR_BLOCK_SIZE * CHAR_NUM_BYTE;
                System.Diagnostics.Debug.Assert(numBytesUsed <= numBytesAlloc);
                return c;
            }
        }

        /* Return char[]s to the pool */
        internal void RecycleCharBlocks(char[][] blocks, int numBlocks)
        {
            lock (this)
            {
                for (int i = 0; i < numBlocks; i++)
                    freeCharBlocks.Add(blocks[i]);
            }
        }

        internal string ToMB(long v)
        {
            return string.Format(nf, "{0:f}", new object[] { (v / 1024F / 1024F) } );
        }

        /* We have three pools of RAM: Postings, byte blocks
         * (holds freq/prox posting data) and char blocks (holds
         * characters in the term).  Different docs require
         * varying amount of storage from these three classes.
         * For example, docs with many unique single-occurrence
         * short terms will use up the Postings RAM and hardly any
         * of the other two.  Whereas docs with very large terms
         * will use alot of char blocks RAM and relatively less of
         * the other two.  This method just frees allocations from
         * the pools once we are over-budget, which balances the
         * pools to match the current docs. */
        internal void BalanceRAM()
        {

            // We flush when we've used our target usage
            long flushTrigger = (long)ramBufferSize;

            if (numBytesAlloc > freeTrigger)
            {

                if (infoStream != null)
                    Message("  RAM: now balance allocations: usedMB=" + ToMB(numBytesUsed) +
                            " vs trigger=" + ToMB(flushTrigger) +
                            " allocMB=" + ToMB(numBytesAlloc) +
                            " vs trigger=" + ToMB(freeTrigger) +
                            " byteBlockFree=" + ToMB(byteBlockAllocator.freeByteBlocks.Count * BYTE_BLOCK_SIZE) +
                            " charBlockFree=" + ToMB(freeCharBlocks.Count * CHAR_BLOCK_SIZE * CHAR_NUM_BYTE));

                long startBytesAlloc = numBytesAlloc;

                int iter = 0;

                // We free equally from each pool in 32 KB
                // chunks until we are below our threshold
                // (freeLevel)

                bool any = true;

                while (numBytesAlloc > freeLevel)
                {

                    lock (this)
                    {
                        if (0 == byteBlockAllocator.freeByteBlocks.Count && 0 == freeCharBlocks.Count && 0 == freeIntBlocks.Count && !any)
                        {
                            // Nothing else to free -- must flush now.
                            bufferIsFull = numBytesUsed > flushTrigger;
                            if (infoStream != null)
                            {
                                if (numBytesUsed > flushTrigger)
                                    Message("    nothing to free; now set bufferIsFull");
                                else
                                    Message("    nothing to free");
                            }
                            System.Diagnostics.Debug.Assert(numBytesUsed <= numBytesAlloc);
                            break;
                        }

                        if ((0 == iter % 4) && byteBlockAllocator.freeByteBlocks.Count > 0)
                        {
                            byteBlockAllocator.freeByteBlocks.RemoveAt(byteBlockAllocator.freeByteBlocks.Count - 1);
                            numBytesAlloc -= BYTE_BLOCK_SIZE;
                        }

                        if ((1 == iter % 4) && freeCharBlocks.Count > 0)
                        {
                            freeCharBlocks.RemoveAt(freeCharBlocks.Count - 1);
                            numBytesAlloc -= CHAR_BLOCK_SIZE * CHAR_NUM_BYTE;
                        }

                        if ((2 == iter % 4) && freeIntBlocks.Count > 0)
                        {
                            freeIntBlocks.RemoveAt(freeIntBlocks.Count - 1);
                            numBytesAlloc -= INT_BLOCK_SIZE * INT_NUM_BYTE;
                        }
                    }

                    if ((3 == iter % 4) && any)
                        // Ask consumer to free any recycled state
                        any = consumer.freeRAM();

                    iter++;
                }

                if (infoStream != null)
                    Message(string.Format(nf, "    after free: freedMB={0:f} usedMB={1:f} allocMB={2:f}",
                        new object[] { ((startBytesAlloc - numBytesAlloc) / 1024.0 / 1024.0), (numBytesUsed / 1024.0 / 1024.0), (numBytesAlloc / 1024.0 / 1024.0) }));
            }
            else
            {
                // If we have not crossed the 100% mark, but have
                // crossed the 95% mark of RAM we are actually
                // using, go ahead and flush.  This prevents
                // over-allocating and then freeing, with every
                // flush.
                lock (this)
                {

                    if (numBytesUsed > flushTrigger)
                    {
                        if (infoStream != null)
							Message(string.Format(nf, "  RAM: now flush @ usedMB={0:f} allocMB={1:f} triggerMB={2:f}",
								new object[] { (numBytesUsed / 1024.0 / 1024.0), (numBytesAlloc / 1024.0 / 1024.0), (flushTrigger / 1024.0 / 1024.0) }));

                        bufferIsFull = true;
                    }
                }
            }
        }

        internal readonly WaitQueue waitQueue;

        internal class WaitQueue
        {
            internal DocWriter[] waiting;
            internal int nextWriteDocID;
            internal int nextWriteLoc;
            internal int numWaiting;
            internal long waitingBytes;

            private DocumentsWriter enclosing_instance;

            public WaitQueue(DocumentsWriter enclosing_instance)
            {
                this.enclosing_instance = enclosing_instance;
                waiting = new DocWriter[10];
            }

            internal void Reset()
            {
                lock (this)
                {
                    // NOTE: nextWriteLoc doesn't need to be reset
                    System.Diagnostics.Debug.Assert(numWaiting == 0);
                    System.Diagnostics.Debug.Assert(waitingBytes == 0);
                    nextWriteDocID = 0;
                }
            }

            internal bool DoResume()
            {
                lock (this)
                {
                    return waitingBytes <= enclosing_instance.waitQueueResumeBytes;
                }
            }

            internal bool DoPause()
            {
                lock (this)
                {
                    return waitingBytes > enclosing_instance.waitQueuePauseBytes;
                }
            }

            internal void Abort()
            {
                lock (this)
                {
                    int count = 0;
                    for (int i = 0; i < waiting.Length; i++)
                    {
                        DocWriter doc = waiting[i];
                        if (doc != null)
                        {
                            doc.Abort();
                            waiting[i] = null;
                            count++;
                        }
                    }
                    waitingBytes = 0;
                    System.Diagnostics.Debug.Assert(count == numWaiting);
                    numWaiting = 0;
                }
            }

            private void WriteDocument(DocWriter doc)
            {
                System.Diagnostics.Debug.Assert(doc == enclosing_instance.skipDocWriter || nextWriteDocID == doc.docID);
                bool success = false;
                try
                {
                    doc.Finish();
                    nextWriteDocID++;
                    enclosing_instance.numDocsInStore++;
                    nextWriteLoc++;
                    System.Diagnostics.Debug.Assert(nextWriteLoc <= waiting.Length);
                    if (nextWriteLoc == waiting.Length)
                        nextWriteLoc = 0;
                    success = true;
                }
                finally
                {
                    if (!success)
                        enclosing_instance.SetAborting();
                }
            }

            public bool Add(DocWriter doc)
            {
                lock (this)
                {
                    System.Diagnostics.Debug.Assert(doc.docID >= nextWriteDocID);

                    if (doc.docID == nextWriteDocID)
                    {
                        WriteDocument(doc);
                        while (true)
                        {
                            doc = waiting[nextWriteLoc];
                            if (doc != null)
                            {
                                numWaiting--;
                                waiting[nextWriteLoc] = null;
                                waitingBytes -= doc.SizeInBytes();
                                WriteDocument(doc);
                            }
                            else
                                break;
                        }
                    }
                    else
                    {

                        // I finished before documents that were added,
                        // before me.  This can easily happen when I am a
                        // small doc and the docs before me were large, or,
                        // just due to luck in the thread scheduling.  Just
                        // add myself to the queue and when that large doc
                        // finishes, it will flush me:
                        int gap = doc.docID - nextWriteDocID;
                        if (gap >= waiting.Length)
                        {
                            // Grow queue
                            DocWriter[] newArray = new DocWriter[ArrayUtil.GetNextSize(gap)];
                            System.Diagnostics.Debug.Assert(nextWriteLoc >= 0);
                            System.Array.Copy(waiting, nextWriteLoc, newArray, 0, waiting.Length - nextWriteLoc);
                            System.Array.Copy(waiting, 0, newArray, waiting.Length - nextWriteLoc, nextWriteLoc);
                            nextWriteLoc = 0;
                            waiting = newArray;
                            gap = doc.docID - nextWriteDocID;
                        }

                        int loc = nextWriteLoc + gap;
                        if (loc >= waiting.Length)
                            loc -= waiting.Length;

                        // We should only wrap one time
                        System.Diagnostics.Debug.Assert(loc < waiting.Length);

                        // Nobody should be in my spot!
                        System.Diagnostics.Debug.Assert(waiting[loc] == null);
                        waiting[loc] = doc;
                        numWaiting++;
                        waitingBytes += doc.SizeInBytes();
                    }

                    return DoPause();
                }
            }
        }
    }
}
