using Lucene.Net.Analysis;
using Lucene.Net.Codecs;
using Lucene.Net.Store;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using DeleteSlice = Lucene.Net.Index.DocumentsWriterDeleteQueue.DeleteSlice;

namespace Lucene.Net.Index
{
    internal class DocumentsWriterPerThread
    {
        internal abstract class IndexingChain
        {
            public abstract DocConsumer GetChain(DocumentsWriterPerThread documentsWriterPerThread);
        }

        internal static readonly IndexingChain defaultIndexingChain = new AnonymousDefaultIndexingChain();

        private sealed class AnonymousDefaultIndexingChain : IndexingChain
        {
            public override DocConsumer GetChain(DocumentsWriterPerThread documentsWriterPerThread)
            {
                /*
                 This is the current indexing chain:

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

        internal class DocState
        {
            internal readonly DocumentsWriterPerThread docWriter;
            internal Analyzer analyzer;
            internal InfoStream infoStream;
            internal Similarity similarity;
            internal int docID;
            internal IEnumerable<IIndexableField> doc;
            internal String maxTermPrefix;

            internal DocState(DocumentsWriterPerThread docWriter, InfoStream infoStream)
            {
                this.docWriter = docWriter;
                this.infoStream = infoStream;
            }

            // Only called by asserts
            public bool TestPoint(String name)
            {
                return docWriter.writer.TestPoint(name);
            }

            public void Clear()
            {
                // don't hold onto doc nor analyzer, in case it is
                // largish:
                doc = null;
                analyzer = null;
            }
        }

        internal class FlushedSegment
        {
            internal readonly SegmentInfoPerCommit segmentInfo;
            internal readonly FieldInfos fieldInfos;
            internal readonly FrozenBufferedDeletes segmentDeletes;
            internal readonly IMutableBits liveDocs;
            internal readonly int delCount;

            internal FlushedSegment(SegmentInfoPerCommit segmentInfo, FieldInfos fieldInfos,
                           BufferedDeletes segmentDeletes, IMutableBits liveDocs, int delCount)
            {
                this.segmentInfo = segmentInfo;
                this.fieldInfos = fieldInfos;
                this.segmentDeletes = segmentDeletes != null && segmentDeletes.Any() ? new FrozenBufferedDeletes(segmentDeletes, true) : null;
                this.liveDocs = liveDocs;
                this.delCount = delCount;
            }
        }

        internal void Abort()
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
                catch
                {
                }

                pendingDeletes.Clear();
                deleteSlice = deleteQueue.NewSlice();
                // Reset all postings data
                DoAfterFlush();

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

        private readonly static bool INFO_VERBOSE = false;
        internal readonly DocumentsWriter parent;
        internal readonly Codec codec;
        internal readonly IndexWriter writer;
        internal readonly TrackingDirectoryWrapper directory;
        internal readonly Directory directoryOrig;
        internal readonly DocState docState;
        internal readonly DocConsumer consumer;
        internal readonly Counter bytesUsed;

        internal SegmentWriteState flushState;
        //Deletes for our still-in-RAM (to be flushed next) segment
        internal BufferedDeletes pendingDeletes;
        internal SegmentInfo segmentInfo;     // Current segment we are working on
        internal bool aborting = false;   // True if an abort is pending
        internal bool hasAborted = false; // True if the last exception throws by #updateDocument was aborting

        private FieldInfos.Builder fieldInfos;
        private readonly InfoStream infoStream;
        private int numDocsInRAM;
        private int flushedDocCount;
        internal DocumentsWriterDeleteQueue deleteQueue;
        internal DeleteSlice deleteSlice;
        private readonly NumberFormatInfo nf = CultureInfo.InvariantCulture.NumberFormat;
        internal readonly ByteBlockPool.Allocator byteBlockAllocator;
        internal readonly IntBlockPool.Allocator intBlockAllocator;

        public DocumentsWriterPerThread(Directory directory, DocumentsWriter parent,
            FieldInfos.Builder fieldInfos, IndexingChain indexingChain)
        {
            this.directoryOrig = directory;
            this.directory = new TrackingDirectoryWrapper(directory);
            this.parent = parent;
            this.fieldInfos = fieldInfos;
            this.writer = parent.indexWriter;
            this.infoStream = parent.infoStream;
            this.codec = parent.codec;
            this.docState = new DocState(this, infoStream);
            this.docState.similarity = parent.indexWriter.Config.Similarity;
            bytesUsed = Counter.NewCounter();
            byteBlockAllocator = new ByteBlockPool.DirectTrackingAllocator(bytesUsed);
            pendingDeletes = new BufferedDeletes();
            intBlockAllocator = new IntBlockAllocator(bytesUsed);
            Initialize();
            // this should be the last call in the ctor 
            // it really sucks that we need to pull this within the ctor and pass this ref to the chain!
            consumer = indexingChain.GetChain(this);
        }

        public DocumentsWriterPerThread(DocumentsWriterPerThread other, FieldInfos.Builder fieldInfos)
            : this(other.directoryOrig, other.parent, fieldInfos, other.parent.chain)
        {
        }

        internal void Initialize()
        {
            deleteQueue = parent.deleteQueue;
            //assert numDocsInRAM == 0 : "num docs " + numDocsInRAM;
            pendingDeletes.Clear();
            deleteSlice = null;
        }

        internal void SetAborting()
        {
            aborting = true;
        }

        internal bool CheckAndResetHasAborted()
        {
            bool retval = hasAborted;
            hasAborted = false;
            return retval;
        }

        public void UpdateDocument(IEnumerable<IIndexableField> doc, Analyzer analyzer, Term delTerm)
        {
            //assert writer.testPoint("DocumentsWriterPerThread addDocument start");
            //assert deleteQueue != null;
            docState.doc = doc;
            docState.analyzer = analyzer;
            docState.docID = numDocsInRAM;
            if (segmentInfo == null)
            {
                InitSegmentInfo();
            }
            if (INFO_VERBOSE && infoStream.IsEnabled("DWPT"))
            {
                infoStream.Message("DWPT", Thread.CurrentThread.Name + " update delTerm=" + delTerm + " docID=" + docState.docID + " seg=" + segmentInfo.name);
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
                        Abort();
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
                    Abort();
                }
            }
            FinishDocument(delTerm);
        }

        private void InitSegmentInfo()
        {
            String segment = writer.NewSegmentName();
            segmentInfo = new SegmentInfo(directoryOrig, Constants.LUCENE_MAIN_VERSION, segment, -1,
                                          false, codec, null, null);
            //assert numDocsInRAM == 0;
            if (INFO_VERBOSE && infoStream.IsEnabled("DWPT"))
            {
                infoStream.Message("DWPT", Thread.CurrentThread.Name + " init seg=" + segment + " delQueue=" + deleteQueue);
            }
        }

        public int UpdateDocuments(IEnumerable<IEnumerable<IIndexableField>> docs, Analyzer analyzer, Term delTerm)
        {
            //assert writer.testPoint("DocumentsWriterPerThread addDocuments start");
            //assert deleteQueue != null;
            docState.analyzer = analyzer;
            if (segmentInfo == null)
            {
                InitSegmentInfo();
            }
            if (INFO_VERBOSE && infoStream.IsEnabled("DWPT"))
            {
                infoStream.Message("DWPT", Thread.CurrentThread.Name + " update delTerm=" + delTerm + " docID=" + docState.docID + " seg=" + segmentInfo.name);
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
                                Abort();
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
                            Abort();
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
                    //assert deleteSlice.isTailItem(delTerm) : "expected the delete term as the tail item";
                    deleteSlice.Apply(pendingDeletes, numDocsInRAM - docCount);
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
            if (deleteSlice == null)
            {
                deleteSlice = deleteQueue.NewSlice();
                if (delTerm != null)
                {
                    deleteQueue.Add(delTerm, deleteSlice);
                    deleteSlice.Reset();
                }

            }
            else
            {
                if (delTerm != null)
                {
                    deleteQueue.Add(delTerm, deleteSlice);
                    //assert deleteSlice.isTailItem(delTerm) : "expected the delete term as the tail item";
                    deleteSlice.Apply(pendingDeletes, numDocsInRAM);
                }
                else if (deleteQueue.UpdateSlice(deleteSlice))
                {
                    deleteSlice.Apply(pendingDeletes, numDocsInRAM);
                }
            }
            ++numDocsInRAM;
        }

        // Buffer a specific docID for deletion.  Currently only
        // used when we hit a exception when adding a document
        internal void DeleteDocID(int docIDUpto)
        {
            pendingDeletes.AddDocID(docIDUpto);
            // NOTE: we do not trigger flush here.  This is
            // potentially a RAM leak, if you have an app that tries
            // to add docs but every single doc always hits a
            // non-aborting exception.  Allowing a flush here gets
            // very messy because we are only invoked when handling
            // exceptions so to do this properly, while handling an
            // exception we'd have to go off and flush new deletes
            // which is risky (likely would hit some other
            // confounding exception).
        }

        public int NumDeleteTerms
        {
            get
            {
                // public for FlushPolicy
                return pendingDeletes.numTermDeletes;
            }
        }

        public int NumDocsInRAM
        {
            get
            {
                // public for FlushPolicy
                return numDocsInRAM;
            }
        }

        private void DoAfterFlush()
        {
            segmentInfo = null;
            consumer.DoAfterFlush();
            directory.CreatedFiles.Clear();
            fieldInfos = new FieldInfos.Builder(fieldInfos.globalFieldNumbers);
            parent.SubtractFlushedNumDocs(numDocsInRAM);
            numDocsInRAM = 0;
        }

        internal FrozenBufferedDeletes PrepareFlush()
        {
            //assert numDocsInRAM > 0;
            FrozenBufferedDeletes globalDeletes = deleteQueue.FreezeGlobalBuffer(deleteSlice);
            /* deleteSlice can possibly be null if we have hit non-aborting exceptions during indexing and never succeeded 
            adding a document. */
            if (deleteSlice != null)
            {
                // apply all deletes before we flush and release the delete slice
                deleteSlice.Apply(pendingDeletes, numDocsInRAM);
                //assert deleteSlice.isEmpty();
                deleteSlice = null;
            }
            return globalDeletes;
        }

        internal FlushedSegment Flush()
        {
            //assert numDocsInRAM > 0;
            //assert deleteSlice == null : "all deletes must be applied in prepareFlush";
            segmentInfo.DocCount = numDocsInRAM;
            flushState = new SegmentWriteState(infoStream, directory, segmentInfo, fieldInfos.Finish(),
                writer.Config.TermIndexInterval,
                pendingDeletes, new IOContext(new FlushInfo(numDocsInRAM, BytesUsed)));
            double startMBUsed = parent.flushControl.netBytes() / 1024.0 / 1024.0;

            // Apply delete-by-docID now (delete-byDocID only
            // happens when an exception is hit processing that
            // doc, eg if analyzer has some problem w/ the text):
            if (pendingDeletes.docIDs.Count > 0)
            {
                flushState.liveDocs = codec.LiveDocsFormat().NewLiveDocs(numDocsInRAM);
                foreach (int delDocID in pendingDeletes.docIDs)
                {
                    flushState.liveDocs.Clear(delDocID);
                }
                flushState.delCountOnFlush = pendingDeletes.docIDs.Count;
                Interlocked.Add(ref pendingDeletes.bytesUsed, -pendingDeletes.docIDs.Count * BufferedDeletes.BYTES_PER_DEL_DOCID);
                pendingDeletes.docIDs.Clear();
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
                infoStream.Message("DWPT", "flush postings as segment " + flushState.segmentInfo.name + " numDocs=" + numDocsInRAM);
            }

            bool success = false;

            try
            {
                consumer.Flush(flushState);
                pendingDeletes.terms.Clear();
                segmentInfo.Files = new HashSet<String>(directory.CreatedFiles);

                SegmentInfoPerCommit segmentInfoPerCommit = new SegmentInfoPerCommit(segmentInfo, 0, -1L);
                if (infoStream.IsEnabled("DWPT"))
                {
                    infoStream.Message("DWPT", "new segment has " + (flushState.liveDocs == null ? 0 : (flushState.segmentInfo.DocCount - flushState.delCountOnFlush)) + " deleted docs");
                    infoStream.Message("DWPT", "new segment has " +
                                       (flushState.fieldInfos.HasVectors ? "vectors" : "no vectors") + "; " +
                                       (flushState.fieldInfos.HasNorms ? "norms" : "no norms") + "; " +
                                       (flushState.fieldInfos.HasDocValues ? "docValues" : "no docValues") + "; " +
                                       (flushState.fieldInfos.HasProx ? "prox" : "no prox") + "; " +
                                       (flushState.fieldInfos.HasFreq ? "freqs" : "no freqs"));
                    infoStream.Message("DWPT", "flushedFiles=" + segmentInfoPerCommit.Files);
                    infoStream.Message("DWPT", "flushed codec=" + codec);
                }

                flushedDocCount += flushState.segmentInfo.DocCount;

                BufferedDeletes segmentDeletes;
                if (pendingDeletes.queries.Count == 0)
                {
                    pendingDeletes.Clear();
                    segmentDeletes = null;
                }
                else
                {
                    segmentDeletes = pendingDeletes;
                    pendingDeletes = new BufferedDeletes();
                }

                if (infoStream.IsEnabled("DWPT"))
                {
                    double newSegmentSize = segmentInfoPerCommit.SizeInBytes / 1024.0 / 1024.0;
                    infoStream.Message("DWPT", "flushed: segment=" + segmentInfo.name +
                            " ramUsed=" + startMBUsed.ToString(nf) + " MB" +
                            " newFlushedSize(includes docstores)=" + newSegmentSize.ToString(nf) + " MB" +
                            " docs/MB=" + (flushedDocCount / newSegmentSize).ToString(nf));
                }

                //assert segmentInfo != null;

                FlushedSegment fs = new FlushedSegment(segmentInfoPerCommit, flushState.fieldInfos,
                                                       segmentDeletes, flushState.liveDocs, flushState.delCountOnFlush);
                SealFlushedSegment(fs);
                DoAfterFlush();
                success = true;

                return fs;
            }
            finally
            {
                if (!success)
                {
                    if (segmentInfo != null)
                    {
                        writer.FlushFailed(segmentInfo);
                    }
                    Abort();
                }
            }
        }

        internal void SealFlushedSegment(FlushedSegment flushedSegment)
        {
            //assert flushedSegment != null;

            SegmentInfoPerCommit newSegment = flushedSegment.segmentInfo;

            IndexWriter.SetDiagnostics(newSegment.info, IndexWriter.SOURCE_FLUSH);

            IOContext context = new IOContext(new FlushInfo(newSegment.info.DocCount, newSegment.SizeInBytes));

            bool success = false;
            try
            {
                if (writer.UseCompoundFile(newSegment))
                {

                    // Now build compound file
                    ICollection<String> oldFiles = IndexWriter.CreateCompoundFile(infoStream, directory, MergeState.CheckAbort.NONE, newSegment.info, context);
                    newSegment.info.UseCompoundFile = true;
                    writer.DeleteNewFiles(oldFiles);
                }

                // Have codec write SegmentInfo.  Must do this after
                // creating CFS so that 1) .si isn't slurped into CFS,
                // and 2) .si reflects useCompoundFile=true change
                // above:
                codec.SegmentInfoFormat().SegmentInfoWriter.Write(directory, newSegment.info, flushedSegment.fieldInfos, context);

                // TODO: ideally we would freeze newSegment here!!
                // because any changes after writing the .si will be
                // lost... 

                // Must write deleted docs after the CFS so we don't
                // slurp the del file into CFS:
                if (flushedSegment.liveDocs != null)
                {
                    int delCount = flushedSegment.delCount;
                    //assert delCount > 0;
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

                    SegmentInfoPerCommit info = flushedSegment.segmentInfo;
                    Codec codec2 = info.info.Codec;
                    codec2.LiveDocsFormat().WriteLiveDocs(flushedSegment.liveDocs, directory, info, delCount, context);
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
                        infoStream.Message("DWPT", "hit exception " +
                            "reating compound file for newly flushed segment " + newSegment.info.name);
                    }
                    writer.FlushFailed(newSegment.info);
                }
            }
        }

        internal SegmentInfo SegmentInfo
        {
            get { return segmentInfo; }
        }

        internal long BytesUsed
        {
            get { return bytesUsed.Get() + pendingDeletes.bytesUsed; }
        }

        internal const int BYTE_BLOCK_NOT_MASK = ~ByteBlockPool.BYTE_BLOCK_MASK;

        internal const int MAX_TERM_LENGTH_UTF8 = ByteBlockPool.BYTE_BLOCK_SIZE - 2;

        private class IntBlockAllocator : IntBlockPool.Allocator
        {
            private readonly Counter bytesUsed;

            public IntBlockAllocator(Counter bytesUsed)
                : base(IntBlockPool.INT_BLOCK_SIZE)
            {

                this.bytesUsed = bytesUsed;
            }

            public override int[] IntBlock
            {
                get
                {
                    int[] b = new int[IntBlockPool.INT_BLOCK_SIZE];
                    bytesUsed.AddAndGet(IntBlockPool.INT_BLOCK_SIZE
                        * RamUsageEstimator.NUM_BYTES_INT);
                    return b;
                }
            }

            public override void RecycleIntBlocks(int[][] blocks, int offset, int length)
            {
                bytesUsed.AddAndGet(-(length * (IntBlockPool.INT_BLOCK_SIZE * RamUsageEstimator.NUM_BYTES_INT)));
            }
        }

        public override string ToString()
        {
            return "DocumentsWriterPerThread [pendingDeletes=" + pendingDeletes
              + ", segment=" + (segmentInfo != null ? segmentInfo.name : "null") + ", aborting=" + aborting + ", numDocsInRAM="
                + numDocsInRAM + ", deleteQueue=" + deleteQueue + "]";
        }
    }
}
