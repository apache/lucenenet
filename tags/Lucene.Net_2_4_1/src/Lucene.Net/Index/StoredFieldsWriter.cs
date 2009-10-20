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

using ArrayUtil = Lucene.Net.Util.ArrayUtil;
using RAMOutputStream = Lucene.Net.Store.RAMOutputStream;

namespace Lucene.Net.Index
{
    /** This is a DocFieldConsumer that writes stored fields. */
    internal sealed class StoredFieldsWriter : DocFieldConsumer
    {

        internal FieldsWriter fieldsWriter;
        internal readonly DocumentsWriter docWriter;
        internal int lastDocID;

        internal PerDoc[] docFreeList = new PerDoc[1];
        internal int freeCount;

        public StoredFieldsWriter(DocumentsWriter docWriter)
        {
            this.docWriter = docWriter;
        }

        internal override DocFieldConsumerPerThread addThread(DocFieldProcessorPerThread docFieldProcessorPerThread)
        {
            return new StoredFieldsWriterPerThread(docFieldProcessorPerThread, this);
        }

        internal override void flush(IDictionary<object, ICollection<object>> threadsAndFields, DocumentsWriter.FlushState state)
        {
            lock (this)
            {

                if (state.numDocsInStore > 0)
                {
                    // It's possible that all documents seen in this segment
                    // hit non-aborting exceptions, in which case we will
                    // not have yet init'd the FieldsWriter:
                    initFieldsWriter();

                    // Fill fdx file to include any final docs that we
                    // skipped because they hit non-aborting exceptions
                    fill(state.numDocsInStore - docWriter.GetDocStoreOffset());
                }

                if (fieldsWriter != null)
                    fieldsWriter.Flush();
            }
        }

        private void initFieldsWriter()
        {
            if (fieldsWriter == null)
            {
                string docStoreSegment = docWriter.GetDocStoreSegment();
                if (docStoreSegment != null)
                {
                    System.Diagnostics.Debug.Assert(docStoreSegment != null);
                    fieldsWriter = new FieldsWriter(docWriter.directory,
                                                    docStoreSegment,
                                                    fieldInfos);
                    docWriter.AddOpenFile(docStoreSegment + "." + IndexFileNames.FIELDS_EXTENSION);
                    docWriter.AddOpenFile(docStoreSegment + "." + IndexFileNames.FIELDS_INDEX_EXTENSION);
                    lastDocID = 0;
                }
            }
        }

        internal override void closeDocStore(DocumentsWriter.FlushState state)
        {
            lock (this)
            {
                int inc = state.numDocsInStore - lastDocID;
                if (inc > 0)
                {
                    initFieldsWriter();
                    fill(state.numDocsInStore - docWriter.GetDocStoreOffset());
                }

                if (fieldsWriter != null)
                {
                    fieldsWriter.Close();
                    fieldsWriter = null;
                    lastDocID = 0;
                    System.Diagnostics.Debug.Assert(state.docStoreSegmentName != null);

                    string fdtFile = state.docStoreSegmentName + "." + IndexFileNames.FIELDS_EXTENSION;
                    string fdxFile = state.docStoreSegmentName + "." + IndexFileNames.FIELDS_INDEX_EXTENSION;

                    state.flushedFiles[fdtFile] = fdtFile;
                    state.flushedFiles[fdxFile] = fdxFile;

                    state.docWriter.RemoveOpenFile(fdtFile);
                    state.docWriter.RemoveOpenFile(fdxFile);

                    if (4 + state.numDocsInStore * 8 != state.directory.FileLength(fdxFile))
                        throw new System.SystemException("after flush: fdx size mismatch: " + state.numDocsInStore + " docs vs " + state.directory.FileLength(fdxFile) + " length in bytes of " + fdxFile);
                }
            }
        }

        internal int allocCount;

        internal PerDoc getPerDoc() {
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

        internal override void Abort()
        {
            lock (this)
            {
                if (fieldsWriter != null)
                {
                    try
                    {
                        fieldsWriter.Close();
                    }
                    catch (System.Exception)
                    {
                    }
                    fieldsWriter = null;
                    lastDocID = 0;
                }
            }
        }

        /** Fills in any hole in the docIDs */
        internal void fill(int docID)
        {
            int docStoreOffset = docWriter.GetDocStoreOffset();

            // We must "catch up" for all docs before us
            // that had no stored fields:
            int end = docID + docStoreOffset;
            while (lastDocID < end)
            {
                fieldsWriter.SkipDocument();
                lastDocID++;
            }
        }

        internal void finishDocument(PerDoc perDoc)
        {
            lock (this)
            {
                System.Diagnostics.Debug.Assert(docWriter.writer.TestPoint("StoredFieldsWriter.finishDocument start"));
                initFieldsWriter();

                fill(perDoc.docID);

                // Append stored fields to the real FieldsWriter:
                fieldsWriter.FlushDocument(perDoc.numStoredFields, perDoc.fdt);
                lastDocID++;
                perDoc.reset();
                free(perDoc);
                System.Diagnostics.Debug.Assert(docWriter.writer.TestPoint("StoredFieldsWriter.finishDocument end"));
            }
        }

        internal override bool freeRAM()
        {
            return false;
        }

        internal void free(PerDoc perDoc)
        {
            lock (this)
            {
                System.Diagnostics.Debug.Assert(freeCount < docFreeList.Length);
                System.Diagnostics.Debug.Assert(0 == perDoc.numStoredFields);
                System.Diagnostics.Debug.Assert(0 == perDoc.fdt.Length());
                System.Diagnostics.Debug.Assert(0 == perDoc.fdt.GetFilePointer());
                docFreeList[freeCount++] = perDoc;
            }
        }

        internal class PerDoc : DocumentsWriter.DocWriter
        {
            // TODO: use something more memory efficient; for small
            // docs the 1024 buffer size of RAMOutputStream wastes alot
            internal RAMOutputStream fdt = new RAMOutputStream();
            internal int numStoredFields;

            private StoredFieldsWriter enclosing_instance;
            
            internal PerDoc(StoredFieldsWriter enclosing_instance)
            {
                this.enclosing_instance = enclosing_instance;
            }

            internal void reset()
            {
                fdt.Reset();
                numStoredFields = 0;
            }

            internal override void Abort()
            {
                reset();
                enclosing_instance.free(this);
            }

            internal override long SizeInBytes()
            {
                return fdt.SizeInBytes();
            }

            internal override void Finish()
            {
                enclosing_instance.finishDocument(this);
            }
        }
    }
}
