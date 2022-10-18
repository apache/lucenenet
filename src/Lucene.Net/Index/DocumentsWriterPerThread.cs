using Lucene.Net.Diagnostics;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
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

    using Allocator = Lucene.Net.Util.ByteBlockPool.Allocator;
    using Analyzer = Lucene.Net.Analysis.Analyzer;
    using Codec = Lucene.Net.Codecs.Codec;
    using Constants = Lucene.Net.Util.Constants;
    using Counter = Lucene.Net.Util.Counter;
    using DeleteSlice = Lucene.Net.Index.DocumentsWriterDeleteQueue.DeleteSlice;
    using Directory = Lucene.Net.Store.Directory;
    using DirectTrackingAllocator = Lucene.Net.Util.ByteBlockPool.DirectTrackingAllocator;
    using FlushInfo = Lucene.Net.Store.FlushInfo;
    using InfoStream = Lucene.Net.Util.InfoStream;
    using Int32BlockPool = Lucene.Net.Util.Int32BlockPool;
    using IOContext = Lucene.Net.Store.IOContext;
    using IMutableBits = Lucene.Net.Util.IMutableBits;
    using RamUsageEstimator = Lucene.Net.Util.RamUsageEstimator;
    using Similarity = Lucene.Net.Search.Similarities.Similarity;
    using TrackingDirectoryWrapper = Lucene.Net.Store.TrackingDirectoryWrapper;

    internal class DocumentsWriterPerThread
    {
        /// <summary>
        /// The <see cref="IndexingChain"/> must define the <see cref="GetChain(DocumentsWriterPerThread)"/> method
        /// which returns the <see cref="DocConsumer"/> that the <see cref="DocumentsWriter"/> calls to process the
        /// documents.
        /// </summary>
        internal abstract class IndexingChain
        {
            internal abstract DocConsumer GetChain(DocumentsWriterPerThread documentsWriterPerThread);
        }

        private static readonly IndexingChain defaultIndexingChain = new IndexingChainAnonymousClass();

        public static IndexingChain DefaultIndexingChain => defaultIndexingChain;

        private sealed class IndexingChainAnonymousClass : IndexingChain
        {
            public IndexingChainAnonymousClass()
            {
            }

            internal override DocConsumer GetChain(DocumentsWriterPerThread documentsWriterPerThread)
            {
                /*
                this is the current indexing chain:

                DocConsumer / DocConsumerPerThread
                  --> code: DocFieldProcessor
                    --> DocFieldConsumer / DocFieldConsumerPerField
                      --> code: DocFieldConsumers / DocFieldConsumersPerField
                        --> code: DocInverter / DocInverterPerField
                          --> InvertedDocConsumer / InvertedDocConsumerPerField
                            --> code: TermsHash / TermsHashPerField
                              --> TermsHashConsumer / TermsHashConsumerPerField
                                --> code: FreqProxTermsWriter / FreqProxTermsWriterPerField
                                --> code: TermVectorsTermsWriter / TermVectorsTermsWriterPerField
                          --> InvertedDocEndConsumer / InvertedDocConsumerPerField
                            --> code: NormsConsumer / NormsConsumerPerField
                    --> StoredFieldsConsumer
                      --> TwoStoredFieldConsumers
                        -> code: StoredFieldsProcessor
                        -> code: DocValuesProcessor
              */

                // Build up indexing chain:

                TermsHashConsumer termVectorsWriter = new TermVectorsConsumer(documentsWriterPerThread);
                TermsHashConsumer freqProxWriter = new FreqProxTermsWriter();

                InvertedDocConsumer termsHash = new TermsHash(documentsWriterPerThread, freqProxWriter, true, 
                    new TermsHash(documentsWriterPerThread, termVectorsWriter, false, null));
                NormsConsumer normsWriter = new NormsConsumer();
                DocInverter docInverter = new DocInverter(documentsWriterPerThread.docState, termsHash, normsWriter);
                StoredFieldsConsumer storedFields = new TwoStoredFieldsConsumers(
                                                            new StoredFieldsProcessor(documentsWriterPerThread), 
                                                            new DocValuesProcessor(documentsWriterPerThread.bytesUsed));
                return new DocFieldProcessor(documentsWriterPerThread, docInverter, storedFields);
            }
        }

        public class DocState
        {
            internal readonly DocumentsWriterPerThread docWriter;
            internal Analyzer analyzer;
            internal InfoStream infoStream;
            internal Similarity similarity;
            internal int docID;
            internal IEnumerable<IIndexableField> doc;
            internal string maxTermPrefix;

            internal DocState(DocumentsWriterPerThread docWriter, InfoStream infoStream)
            {
                this.docWriter = docWriter;
                this.infoStream = infoStream;
            }

            // Only called by asserts
            public virtual bool TestPoint(string name)
            {
                return docWriter.TestPoint(name);
            }

            public virtual void Clear()
            {
                // don't hold onto doc nor analyzer, in case it is
                // largish:
                doc = null;
                analyzer = null;
            }
        }

        internal class FlushedSegment
        {
            internal readonly SegmentCommitInfo segmentInfo;
            internal readonly FieldInfos fieldInfos;
            internal readonly FrozenBufferedUpdates segmentUpdates;
            internal readonly IMutableBits liveDocs;
            internal readonly int delCount;

            internal FlushedSegment(SegmentCommitInfo segmentInfo, FieldInfos fieldInfos, BufferedUpdates segmentUpdates, IMutableBits liveDocs, int delCount)
            {
                this.segmentInfo = segmentInfo;
                this.fieldInfos = fieldInfos;
                this.segmentUpdates = segmentUpdates != null && segmentUpdates.Any() ? new FrozenBufferedUpdates(segmentUpdates, true) : null;
                this.liveDocs = liveDocs;
                this.delCount = delCount;
            }
        }

        /// <summary>
        /// Called if we hit an exception at a bad time (when
        /// updating the index files) and must discard all
        /// currently buffered docs.  this resets our state,
        /// discarding any docs added since last flush.
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal virtual void Abort(ISet<string> createdFiles)
        {
            //System.out.println(Thread.currentThread().getName() + ": now abort seg=" + segmentInfo.name);
            hasAborted = aborting = true;
            try
            {
                if (infoStream.IsEnabled("DWPT"))
                {
                    infoStream.Message("DWPT", "now abort");
                }
                try
                {
                    consumer.Abort();
                }
                catch (Exception t) when (t.IsThrowable())
                {
                    // ignore
                }

                pendingUpdates.Clear();
                createdFiles.UnionWith(directory.CreatedFiles);
            }
            finally
            {
                aborting = false;
                if (infoStream.IsEnabled("DWPT"))
                {
                    infoStream.Message("DWPT", "done abort");
                }
            }
        }

        private const bool INFO_VERBOSE = false;
        internal readonly Codec codec;
        internal readonly TrackingDirectoryWrapper directory;
        internal readonly Directory directoryOrig;
        internal readonly DocState docState;
        internal readonly DocConsumer consumer;
        internal readonly Counter bytesUsed;

        internal SegmentWriteState flushState;

        // Updates for our still-in-RAM (to be flushed next) segment
        internal readonly BufferedUpdates pendingUpdates;

        private readonly SegmentInfo segmentInfo; // Current segment we are working on
        internal bool aborting = false; // True if an abort is pending
        internal bool hasAborted = false; // True if the last exception throws by #updateDocument was aborting

        private readonly FieldInfos.Builder fieldInfos; // LUCENENET: marked readonly
        private readonly InfoStream infoStream;
        private int numDocsInRAM;
        internal readonly DocumentsWriterDeleteQueue deleteQueue;
        private readonly DeleteSlice deleteSlice;
        private readonly NumberFormatInfo nf = CultureInfo.InvariantCulture.NumberFormat;
        internal readonly Allocator byteBlockAllocator;
        internal readonly Int32BlockPool.Allocator intBlockAllocator;
        private readonly LiveIndexWriterConfig indexWriterConfig;

        public DocumentsWriterPerThread(string segmentName, Directory directory, LiveIndexWriterConfig indexWriterConfig, InfoStream infoStream, DocumentsWriterDeleteQueue deleteQueue, FieldInfos.Builder fieldInfos)
        {
            this.directoryOrig = directory;
            this.directory = new TrackingDirectoryWrapper(directory);
            this.fieldInfos = fieldInfos;
            this.indexWriterConfig = indexWriterConfig;
            this.infoStream = infoStream;
            this.codec = indexWriterConfig.Codec;
            this.docState = new DocState(this, infoStream);
            this.docState.similarity = indexWriterConfig.Similarity;
            bytesUsed = Counter.NewCounter();
            byteBlockAllocator = new DirectTrackingAllocator(bytesUsed);
            pendingUpdates = new BufferedUpdates();
            intBlockAllocator = new Int32BlockAllocator(bytesUsed);
            this.deleteQueue = deleteQueue;
            if (Debugging.AssertsEnabled) Debugging.Assert(numDocsInRAM == 0,"num docs {0}", numDocsInRAM);
            pendingUpdates.Clear();
            deleteSlice = deleteQueue.NewSlice();

            segmentInfo = new SegmentInfo(directoryOrig, Constants.LUCENE_MAIN_VERSION, segmentName, -1, false, codec, null);
            if (Debugging.AssertsEnabled) Debugging.Assert(numDocsInRAM == 0);
            if (INFO_VERBOSE && infoStream.IsEnabled("DWPT"))
            {
                infoStream.Message("DWPT", Thread.CurrentThread.Name + " init seg=" + segmentName + " delQueue=" + deleteQueue);
            }
            // this should be the last call in the ctor
            // it really sucks that we need to pull this within the ctor and pass this ref to the chain!
            consumer = indexWriterConfig.IndexingChain.GetChain(this);
        }

        internal virtual void SetAborting()
        {
            aborting = true;
        }

        internal virtual bool CheckAndResetHasAborted()
        {
            bool retval = hasAborted;
            hasAborted = false;
            return retval;
        }

        internal bool TestPoint(string message)
        {
            if (infoStream.IsEnabled("TP"))
            {
                infoStream.Message("TP", message);
            }
            return true;
        }

        public virtual void UpdateDocument(IEnumerable<IIndexableField> doc, Analyzer analyzer, Term delTerm)
        {
            if (Debugging.AssertsEnabled)
            {
                Debugging.Assert(TestPoint("DocumentsWriterPerThread addDocument start"));
                Debugging.Assert(deleteQueue != null);
            }
            docState.doc = doc;
            docState.analyzer = analyzer;
            docState.docID = numDocsInRAM;
            if (INFO_VERBOSE && infoStream.IsEnabled("DWPT"))
            {
                infoStream.Message("DWPT", Thread.CurrentThread.Name + " update delTerm=" + delTerm + " docID=" + docState.docID + " seg=" + segmentInfo.Name);
            }
            bool success = false;
            try
            {
                try
                {
                    consumer.ProcessDocument(fieldInfos);
                }
                finally
                {
                    docState.Clear();
                }
                success = true;
            }
            finally
            {
                if (!success)
                {
                    if (!aborting)
                    {
                        // mark document as deleted
                        DeleteDocID(docState.docID);
                        numDocsInRAM++;
                    }
                    else
                    {
                        Abort(filesToDelete);
                    }
                }
            }
            success = false;
            try
            {
                consumer.FinishDocument();
                success = true;
            }
            finally
            {
                if (!success)
                {
                    Abort(filesToDelete);
                }
            }
            FinishDocument(delTerm);
        }

        public virtual int UpdateDocuments(IEnumerable<IEnumerable<IIndexableField>> docs, Analyzer analyzer, Term delTerm)
        {
            if (Debugging.AssertsEnabled)
            {
                Debugging.Assert(TestPoint("DocumentsWriterPerThread addDocuments start"));
                Debugging.Assert(deleteQueue != null);
            }
            docState.analyzer = analyzer;
            if (INFO_VERBOSE && infoStream.IsEnabled("DWPT"))
            {
                infoStream.Message("DWPT", Thread.CurrentThread.Name + " update delTerm=" + delTerm + " docID=" + docState.docID + " seg=" + segmentInfo.Name);
            }
            int docCount = 0;
            bool allDocsIndexed = false;
            try
            {
                foreach (IEnumerable<IIndexableField> doc in docs)
                {
                    docState.doc = doc;
                    docState.docID = numDocsInRAM;
                    docCount++;

                    bool success = false;
                    try
                    {
                        consumer.ProcessDocument(fieldInfos);
                        success = true;
                    }
                    finally
                    {
                        if (!success)
                        {
                            // An exc is being thrown...
                            if (!aborting)
                            {
                                // Incr here because finishDocument will not
                                // be called (because an exc is being thrown):
                                numDocsInRAM++;
                            }
                            else
                            {
                                Abort(filesToDelete);
                            }
                        }
                    }
                    success = false;
                    try
                    {
                        consumer.FinishDocument();
                        success = true;
                    }
                    finally
                    {
                        if (!success)
                        {
                            Abort(filesToDelete);
                        }
                    }

                    FinishDocument(null);
                }
                allDocsIndexed = true;

                // Apply delTerm only after all indexing has
                // succeeded, but apply it only to docs prior to when
                // this batch started:
                if (delTerm != null)
                {
                    deleteQueue.Add(delTerm, deleteSlice);
                    if (Debugging.AssertsEnabled) Debugging.Assert(deleteSlice.IsTailItem(delTerm), "expected the delete term as the tail item");
                    deleteSlice.Apply(pendingUpdates, numDocsInRAM - docCount);
                }
            }
            finally
            {
                if (!allDocsIndexed && !aborting)
                {
                    // the iterator threw an exception that is not aborting
                    // go and mark all docs from this block as deleted
                    int docID = numDocsInRAM - 1;
                    int endDocID = docID - docCount;
                    while (docID > endDocID)
                    {
                        DeleteDocID(docID);
                        docID--;
                    }
                }
                docState.Clear();
            }

            return docCount;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void FinishDocument(Term delTerm)
        {
            /*
             * here we actually finish the document in two steps 1. push the delete into
             * the queue and update our slice. 2. increment the DWPT private document
             * id.
             *
             * the updated slice we get from 1. holds all the deletes that have occurred
             * since we updated the slice the last time.
             */
            bool applySlice = numDocsInRAM != 0;
            if (delTerm != null)
            {
                deleteQueue.Add(delTerm, deleteSlice);
                if (Debugging.AssertsEnabled) Debugging.Assert(deleteSlice.IsTailItem(delTerm), "expected the delete term as the tail item");
            }
            else
            {
                applySlice &= deleteQueue.UpdateSlice(deleteSlice);
            }

            if (applySlice)
            {
                deleteSlice.Apply(pendingUpdates, numDocsInRAM);
            } // if we don't need to apply we must reset!
            else
            {
                deleteSlice.Reset();
            }
            ++numDocsInRAM;
        }

        // Buffer a specific docID for deletion. Currently only
        // used when we hit an exception when adding a document
        internal virtual void DeleteDocID(int docIDUpto)
        {
            pendingUpdates.AddDocID(docIDUpto);
            // NOTE: we do not trigger flush here.  this is
            // potentially a RAM leak, if you have an app that tries
            // to add docs but every single doc always hits a
            // non-aborting exception.  Allowing a flush here gets
            // very messy because we are only invoked when handling
            // exceptions so to do this properly, while handling an
            // exception we'd have to go off and flush new deletes
            // which is risky (likely would hit some other
            // confounding exception).
        }

        /// <summary>
        /// Returns the number of delete terms in this <see cref="DocumentsWriterPerThread"/>
        /// </summary>
        public virtual int NumDeleteTerms => pendingUpdates.numTermDeletes; // public for FlushPolicy

        /// <summary>
        /// Returns the number of RAM resident documents in this <see cref="DocumentsWriterPerThread"/>
        /// </summary>
        public virtual int NumDocsInRAM => numDocsInRAM; // public for FlushPolicy

        /// <summary>
        /// Prepares this DWPT for flushing. this method will freeze and return the
        /// <see cref="DocumentsWriterDeleteQueue"/>s global buffer and apply all pending
        /// deletes to this DWPT.
        /// </summary>
        internal virtual FrozenBufferedUpdates PrepareFlush()
        {
            if (Debugging.AssertsEnabled) Debugging.Assert(numDocsInRAM > 0);
            FrozenBufferedUpdates globalUpdates = deleteQueue.FreezeGlobalBuffer(deleteSlice);
            /* deleteSlice can possibly be null if we have hit non-aborting exceptions during indexing and never succeeded
            adding a document. */
            if (deleteSlice != null)
            {
                // apply all deletes before we flush and release the delete slice
                deleteSlice.Apply(pendingUpdates, numDocsInRAM);
                if (Debugging.AssertsEnabled) Debugging.Assert(deleteSlice.IsEmpty);
                deleteSlice.Reset();
            }
            return globalUpdates;
        }

        /// <summary>
        /// Flush all pending docs to a new segment </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal virtual FlushedSegment Flush()
        {
            if (Debugging.AssertsEnabled)
            {
                Debugging.Assert(numDocsInRAM > 0);
                Debugging.Assert(deleteSlice.IsEmpty, "all deletes must be applied in prepareFlush");
            }
            segmentInfo.DocCount = numDocsInRAM;
            SegmentWriteState flushState = new SegmentWriteState(infoStream, directory, segmentInfo, fieldInfos.Finish(), indexWriterConfig.TermIndexInterval, pendingUpdates, new IOContext(new FlushInfo(numDocsInRAM, BytesUsed)));
            double startMBUsed = BytesUsed / 1024.0 / 1024.0;

            // Apply delete-by-docID now (delete-byDocID only
            // happens when an exception is hit processing that
            // doc, eg if analyzer has some problem w/ the text):
            if (pendingUpdates.docIDs.Count > 0)
            {
                flushState.LiveDocs = codec.LiveDocsFormat.NewLiveDocs(numDocsInRAM);
                foreach (int delDocID in pendingUpdates.docIDs)
                {
                    flushState.LiveDocs.Clear(delDocID);
                }
                flushState.DelCountOnFlush = pendingUpdates.docIDs.Count;
                pendingUpdates.bytesUsed.AddAndGet(-pendingUpdates.docIDs.Count * BufferedUpdates.BYTES_PER_DEL_DOCID);
                pendingUpdates.docIDs.Clear();
            }

            if (aborting)
            {
                if (infoStream.IsEnabled("DWPT"))
                {
                    infoStream.Message("DWPT", "flush: skip because aborting is set");
                }
                return null;
            }

            if (infoStream.IsEnabled("DWPT"))
            {
                infoStream.Message("DWPT", "flush postings as segment " + flushState.SegmentInfo.Name + " numDocs=" + numDocsInRAM);
            }

            bool success = false;

            try
            {
                consumer.Flush(flushState);
                pendingUpdates.terms.Clear();
                segmentInfo.SetFiles(new JCG.HashSet<string>(directory.CreatedFiles));

                SegmentCommitInfo segmentInfoPerCommit = new SegmentCommitInfo(segmentInfo, 0, -1L, -1L);
                if (infoStream.IsEnabled("DWPT"))
                {
                    infoStream.Message("DWPT", "new segment has " + (flushState.LiveDocs is null ? 0 : (flushState.SegmentInfo.DocCount - flushState.DelCountOnFlush)) + " deleted docs");
                    infoStream.Message("DWPT", "new segment has " + (flushState.FieldInfos.HasVectors ? "vectors" : "no vectors") + "; " + (flushState.FieldInfos.HasNorms ? "norms" : "no norms") + "; " + (flushState.FieldInfos.HasDocValues ? "docValues" : "no docValues") + "; " + (flushState.FieldInfos.HasProx ? "prox" : "no prox") + "; " + (flushState.FieldInfos.HasFreq ? "freqs" : "no freqs"));
                    infoStream.Message("DWPT", "flushedFiles=" + string.Format(J2N.Text.StringFormatter.InvariantCulture, "{0}", segmentInfoPerCommit.GetFiles()));
                    infoStream.Message("DWPT", "flushed codec=" + codec);
                }

                BufferedUpdates segmentDeletes;
                if (pendingUpdates.queries.Count == 0 && pendingUpdates.numericUpdates.Count == 0 && pendingUpdates.binaryUpdates.Count == 0)
                {
                    pendingUpdates.Clear();
                    segmentDeletes = null;
                }
                else
                {
                    segmentDeletes = pendingUpdates;
                }

                if (infoStream.IsEnabled("DWPT"))
                {
                    double newSegmentSize = segmentInfoPerCommit.GetSizeInBytes() / 1024.0 / 1024.0;
                    infoStream.Message("DWPT", "flushed: segment=" + segmentInfo.Name + " ramUsed=" + startMBUsed.ToString(nf) + " MB" + " newFlushedSize(includes docstores)=" + newSegmentSize.ToString(nf) + " MB" + " docs/MB=" + (flushState.SegmentInfo.DocCount / newSegmentSize).ToString(nf));
                }

                if (Debugging.AssertsEnabled) Debugging.Assert(segmentInfo != null);

                FlushedSegment fs = new FlushedSegment(segmentInfoPerCommit, flushState.FieldInfos, segmentDeletes, flushState.LiveDocs, flushState.DelCountOnFlush);
                SealFlushedSegment(fs);
                success = true;

                return fs;
            }
            finally
            {
                if (!success)
                {
                    Abort(filesToDelete);
                }
            }
        }

        private readonly JCG.HashSet<string> filesToDelete = new JCG.HashSet<string>();

        public virtual ISet<string> PendingFilesToDelete => filesToDelete;

        /// <summary>
        /// Seals the <see cref="Index.SegmentInfo"/> for the new flushed segment and persists
        /// the deleted documents <see cref="IMutableBits"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal virtual void SealFlushedSegment(FlushedSegment flushedSegment)
        {
            if (Debugging.AssertsEnabled) Debugging.Assert(flushedSegment != null);

            SegmentCommitInfo newSegment = flushedSegment.segmentInfo;

            IndexWriter.SetDiagnostics(newSegment.Info, IndexWriter.SOURCE_FLUSH);

            IOContext context = new IOContext(new FlushInfo(newSegment.Info.DocCount, newSegment.GetSizeInBytes()));

            bool success = false;
            try
            {
                if (indexWriterConfig.UseCompoundFile)
                {
                    filesToDelete.UnionWith(IndexWriter.CreateCompoundFile(infoStream, directory, CheckAbort.NONE, newSegment.Info, context));
                    newSegment.Info.UseCompoundFile = true;
                }

                // Have codec write SegmentInfo.  Must do this after
                // creating CFS so that 1) .si isn't slurped into CFS,
                // and 2) .si reflects useCompoundFile=true change
                // above:
                codec.SegmentInfoFormat.SegmentInfoWriter.Write(directory, newSegment.Info, flushedSegment.fieldInfos, context);

                // TODO: ideally we would freeze newSegment here!!
                // because any changes after writing the .si will be
                // lost...

                // Must write deleted docs after the CFS so we don't
                // slurp the del file into CFS:
                if (flushedSegment.liveDocs != null)
                {
                    int delCount = flushedSegment.delCount;
                    if (Debugging.AssertsEnabled) Debugging.Assert(delCount > 0);
                    if (infoStream.IsEnabled("DWPT"))
                    {
                        infoStream.Message("DWPT", "flush: write " + delCount + " deletes gen=" + flushedSegment.segmentInfo.DelGen);
                    }

                    // TODO: we should prune the segment if it's 100%
                    // deleted... but merge will also catch it.

                    // TODO: in the NRT case it'd be better to hand
                    // this del vector over to the
                    // shortly-to-be-opened SegmentReader and let it
                    // carry the changes; there's no reason to use
                    // filesystem as intermediary here.

                    SegmentCommitInfo info = flushedSegment.segmentInfo;
                    Codec codec = info.Info.Codec;
                    codec.LiveDocsFormat.WriteLiveDocs(flushedSegment.liveDocs, directory, info, delCount, context);
                    newSegment.DelCount = delCount;
                    newSegment.AdvanceDelGen();
                }

                success = true;
            }
            finally
            {
                if (!success)
                {
                    if (infoStream.IsEnabled("DWPT"))
                    {
                        infoStream.Message("DWPT", "hit exception creating compound file for newly flushed segment " + newSegment.Info.Name);
                    }
                }
            }
        }

        /// <summary>
        /// Get current segment info we are writing. </summary>
        internal virtual SegmentInfo SegmentInfo => segmentInfo;

        public virtual long BytesUsed => bytesUsed + pendingUpdates.bytesUsed;

        /// <summary>
        /// Initial chunks size of the shared byte[] blocks used to
        /// store postings data
        /// </summary>
        internal static readonly int BYTE_BLOCK_NOT_MASK = ~ByteBlockPool.BYTE_BLOCK_MASK;

        /// <summary>
        /// if you increase this, you must fix field cache impl for
        /// getTerms/getTermsIndex requires &lt;= 32768
        /// </summary>
        internal static readonly int MAX_TERM_LENGTH_UTF8 = ByteBlockPool.BYTE_BLOCK_SIZE - 2;

        /// <summary>
        /// NOTE: This was IntBlockAllocator in Lucene
        /// </summary>
        private class Int32BlockAllocator : Int32BlockPool.Allocator
        {
            private readonly Counter bytesUsed;

            public Int32BlockAllocator(Counter bytesUsed)
                : base(Int32BlockPool.INT32_BLOCK_SIZE)
            {
                this.bytesUsed = bytesUsed;
            }

            /// <summary>
            /// Allocate another int[] from the shared pool
            /// </summary>
            public override int[] GetInt32Block()
            {
                int[] b = new int[Int32BlockPool.INT32_BLOCK_SIZE];
                bytesUsed.AddAndGet(Int32BlockPool.INT32_BLOCK_SIZE * RamUsageEstimator.NUM_BYTES_INT32);
                return b;
            }

            public override void RecycleInt32Blocks(int[][] blocks, int offset, int length)
            {
                bytesUsed.AddAndGet(-(length * (Int32BlockPool.INT32_BLOCK_SIZE * RamUsageEstimator.NUM_BYTES_INT32)));
            }
        }

        public override string ToString()
        {
            return "DocumentsWriterPerThread [pendingDeletes=" + pendingUpdates + ", segment=" + (segmentInfo != null ? segmentInfo.Name : "null") + ", aborting=" + aborting + ", numDocsInRAM=" + numDocsInRAM + ", deleteQueue=" + deleteQueue + "]";
        }
    }
}