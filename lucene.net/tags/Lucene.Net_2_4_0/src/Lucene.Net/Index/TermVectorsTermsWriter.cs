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

using System.Collections.Generic;

using IndexOutput = Lucene.Net.Store.IndexOutput;
using RAMOutputStream = Lucene.Net.Store.RAMOutputStream;
using ArrayUtil = Lucene.Net.Util.ArrayUtil;

namespace Lucene.Net.Index
{
    internal sealed class TermVectorsTermsWriter : TermsHashConsumer
    {

        internal readonly DocumentsWriter docWriter;
        //internal TermVectorsWriter termVectorsWriter;
        internal PerDoc[] docFreeList = new PerDoc[1];
        internal int freeCount;
        internal IndexOutput tvx;
        internal IndexOutput tvd;
        internal IndexOutput tvf;
        internal int lastDocID;

        public TermVectorsTermsWriter(DocumentsWriter docWriter)
        {
            this.docWriter = docWriter;
        }

        internal override TermsHashConsumerPerThread addThread(TermsHashPerThread termsHashPerThread)
        {
            return new TermVectorsTermsWriterPerThread(termsHashPerThread, this);
        }

        internal override void createPostings(RawPostingList[] postings, int start, int count)
        {
            int end = start + count;
            for (int i = start; i < end; i++)
                postings[i] = new PostingList();
        }

        internal override void flush(IDictionary<object, object> threadsAndFields, DocumentsWriter.FlushState state)
        {
            lock (this)
            {

                if (tvx != null)
                {

                    if (state.numDocsInStore > 0)
                        // In case there are some documents that we
                        // didn't see (because they hit a non-aborting exception):
                        fill(state.numDocsInStore - docWriter.GetDocStoreOffset());

                    tvx.Flush();
                    tvd.Flush();
                    tvf.Flush();
                }

                IEnumerator<KeyValuePair<object, object>> it = threadsAndFields.GetEnumerator();
                while (it.MoveNext())
                {
                    KeyValuePair<object, object> entry = (KeyValuePair<object, object>)it.Current;
                    IEnumerator<object> it2 = ((ICollection<object>)entry.Value).GetEnumerator();
                    while (it2.MoveNext())
                    {
                        TermVectorsTermsWriterPerField perField = (TermVectorsTermsWriterPerField)it2.Current;
                        perField.termsHashPerField.reset();
                        perField.shrinkHash();
                    }

                    TermVectorsTermsWriterPerThread perThread = (TermVectorsTermsWriterPerThread)entry.Key;
                    perThread.termsHashPerThread.reset(true);
                }

            }
        }

        internal override void closeDocStore(DocumentsWriter.FlushState state)
        {
            lock (this)
            {
                if (tvx != null)
                {
                    // At least one doc in this run had term vectors
                    // enabled
                    fill(state.numDocsInStore - docWriter.GetDocStoreOffset());
                    tvx.Close();
                    tvf.Close();
                    tvd.Close();
                    tvx = null;
                    System.Diagnostics.Debug.Assert(state.docStoreSegmentName != null);
                    if (4 + state.numDocsInStore * 16 != state.directory.FileLength(state.docStoreSegmentName + "." + IndexFileNames.VECTORS_INDEX_EXTENSION))
                        throw new System.SystemException("after flush: tvx size mismatch: " + state.numDocsInStore + " docs vs " + state.directory.FileLength(state.docStoreSegmentName + "." + IndexFileNames.VECTORS_INDEX_EXTENSION) + " length in bytes of " + state.docStoreSegmentName + "." + IndexFileNames.VECTORS_INDEX_EXTENSION);

                    string tvxFile = state.docStoreSegmentName + "." + IndexFileNames.VECTORS_INDEX_EXTENSION;
                    string tvfFile = state.docStoreSegmentName + "." + IndexFileNames.VECTORS_FIELDS_EXTENSION;
                    string tvdFile = state.docStoreSegmentName + "." + IndexFileNames.VECTORS_DOCUMENTS_EXTENSION;
      
                    state.flushedFiles[tvxFile] = tvxFile;
                    state.flushedFiles[tvfFile] = tvfFile;
                    state.flushedFiles[tvdFile] = tvdFile;

                    docWriter.RemoveOpenFile(tvxFile);
                    docWriter.RemoveOpenFile(tvfFile);
                    docWriter.RemoveOpenFile(tvdFile);

                    lastDocID = 0;
                }
            }
        }

        internal int allocCount;

        internal PerDoc getPerDoc()
        {
            lock (this)
            {
                if (freeCount == 0)
                {
                    allocCount++;
                    if (allocCount > docFreeList.Length)
                    {
                        // Grow our free list up front to make sure we have
                        // enough space to recycle all outstanding PerDoc
                        // instances
                        System.Diagnostics.Debug.Assert(allocCount == 1 + docFreeList.Length);
                        docFreeList = new PerDoc[ArrayUtil.GetNextSize(allocCount)];
                    }
                    return new PerDoc(this);
                }
                else
                    return docFreeList[--freeCount];
            }
        }

        /** Fills in no-term-vectors for all docs we haven't seen
         *  since the last doc that had term vectors. */
        internal void fill(int docID)
        {
            int docStoreOffset = docWriter.GetDocStoreOffset();
            int end = docID + docStoreOffset;
            if (lastDocID < end)
            {
                long tvfPosition = tvf.GetFilePointer();
                while (lastDocID < end)
                {
                    tvx.WriteLong(tvd.GetFilePointer());
                    tvd.WriteVInt(0);
                    tvx.WriteLong(tvfPosition);
                    lastDocID++;
                }
            }
        }

        internal void initTermVectorsWriter()
        {
            lock (this)
            {
                if (tvx == null)
                {

                    string docStoreSegment = docWriter.GetDocStoreSegment();

                    if (docStoreSegment == null)
                        return;

                    System.Diagnostics.Debug.Assert(docStoreSegment != null);

                    // If we hit an exception while init'ing the term
                    // vector output files, we must abort this segment
                    // because those files will be in an unknown
                    // state:
                    tvx = docWriter.directory.CreateOutput(docStoreSegment + "." + IndexFileNames.VECTORS_INDEX_EXTENSION);
                    tvd = docWriter.directory.CreateOutput(docStoreSegment + "." + IndexFileNames.VECTORS_DOCUMENTS_EXTENSION);
                    tvf = docWriter.directory.CreateOutput(docStoreSegment + "." + IndexFileNames.VECTORS_FIELDS_EXTENSION);

                    tvx.WriteInt(TermVectorsReader.FORMAT_CURRENT);
                    tvd.WriteInt(TermVectorsReader.FORMAT_CURRENT);
                    tvf.WriteInt(TermVectorsReader.FORMAT_CURRENT);

                    docWriter.AddOpenFile(docStoreSegment + "." + IndexFileNames.VECTORS_INDEX_EXTENSION);
                    docWriter.AddOpenFile(docStoreSegment + "." + IndexFileNames.VECTORS_FIELDS_EXTENSION);
                    docWriter.AddOpenFile(docStoreSegment + "." + IndexFileNames.VECTORS_DOCUMENTS_EXTENSION);

                    lastDocID = 0;
                }
            }
        }

        internal void finishDocument(PerDoc perDoc)
        {
            lock (this)
            {

                System.Diagnostics.Debug.Assert(docWriter.writer.TestPoint("TermVectorsTermsWriter.finishDocument start"));

                initTermVectorsWriter();

                fill(perDoc.docID);

                // Append term vectors to the real outputs:
                tvx.WriteLong(tvd.GetFilePointer());
                tvx.WriteLong(tvf.GetFilePointer());
                tvd.WriteVInt(perDoc.numVectorFields);
                if (perDoc.numVectorFields > 0)
                {
                    for (int i = 0; i < perDoc.numVectorFields; i++)
                        tvd.WriteVInt(perDoc.fieldNumbers[i]);
                    System.Diagnostics.Debug.Assert(0 == perDoc.fieldPointers[0]);
                    long lastPos = perDoc.fieldPointers[0];
                    for (int i = 1; i < perDoc.numVectorFields; i++)
                    {
                        long pos = perDoc.fieldPointers[i];
                        tvd.WriteVLong(pos - lastPos);
                        lastPos = pos;
                    }
                    perDoc.tvf.WriteTo(tvf);
                    perDoc.tvf.Reset();
                    perDoc.numVectorFields = 0;
                }

                System.Diagnostics.Debug.Assert(lastDocID == perDoc.docID + docWriter.GetDocStoreOffset());

                lastDocID++;

                free(perDoc);
                System.Diagnostics.Debug.Assert(docWriter.writer.TestPoint("TermVectorsTermsWriter.finishDocument end"));
            }
        }

        public bool freeRAM()
        {
            // We don't hold any state beyond one doc, so we don't
            // free persistent RAM here
            return false;
        }

        internal override void Abort()
        {
            if (tvx != null)
            {
                try
                {
                    tvx.Close();
                }
                catch (System.Exception)
                {
                }
                tvx = null;
            }
            if (tvd != null)
            {
                try
                {
                    tvd.Close();
                }
                catch (System.Exception)
                {
                }
                tvd = null;
            }
            if (tvf != null)
            {
                try
                {
                    tvf.Close();
                }
                catch (System.Exception)
                {
                }
                tvf = null;
            }
            lastDocID = 0;
        }

        internal void free(PerDoc doc)
        {
            lock (this)
            {
                System.Diagnostics.Debug.Assert(freeCount < docFreeList.Length);
                docFreeList[freeCount++] = doc;
            }
        }

        internal class PerDoc : DocumentsWriter.DocWriter
        {
            // TODO: use something more memory efficient; for small
            // docs the 1024 buffer size of RAMOutputStream wastes alot
            internal RAMOutputStream tvf = new RAMOutputStream();
            internal int numVectorFields;

            internal int[] fieldNumbers = new int[1];
            internal long[] fieldPointers = new long[1];

            private TermVectorsTermsWriter enclosing_instance;

            internal PerDoc(TermVectorsTermsWriter enclosing_instance)
            {
                this.enclosing_instance = enclosing_instance;
            }

            internal void reset()
            {
                tvf.Reset();
                numVectorFields = 0;
            }

            internal override void Abort()
            {
                reset();
                enclosing_instance.free(this);
            }

            internal void addField(int fieldNumber)
            {
                if (numVectorFields == fieldNumbers.Length)
                {
                    fieldNumbers = ArrayUtil.Grow(fieldNumbers);
                    fieldPointers = ArrayUtil.Grow(fieldPointers);
                }
                fieldNumbers[numVectorFields] = fieldNumber;
                fieldPointers[numVectorFields] = tvf.GetFilePointer();
                numVectorFields++;
            }

            internal override long SizeInBytes()
            {
                return tvf.SizeInBytes();
            }

            internal override void Finish()
            {
                enclosing_instance.finishDocument(this);
            }
        }

        internal class PostingList : RawPostingList
        {
            internal int freq;                                       // How many times this term occurred in the current doc
            internal int lastOffset;                                 // Last offset we saw
            internal int lastPosition;                               // Last position where this term occurred
        }

        internal override int bytesPerPosting()
        {
            return RawPostingList.BYTES_SIZE + 3 * DocumentsWriter.INT_NUM_BYTE;
        }
    }
}
