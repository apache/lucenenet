using System;
using System.Diagnostics;

namespace Lucene.Net.Index
{
    using Lucene.Net.Support;
    using ArrayUtil = Lucene.Net.Util.ArrayUtil;

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

    using Codec = Lucene.Net.Codecs.Codec;
    using IOContext = Lucene.Net.Store.IOContext;
    using IOUtils = Lucene.Net.Util.IOUtils;
    using RamUsageEstimator = Lucene.Net.Util.RamUsageEstimator;
    using StoredFieldsWriter = Lucene.Net.Codecs.StoredFieldsWriter;

    /// <summary>
    /// this is a StoredFieldsConsumer that writes stored fields. </summary>
    internal sealed class StoredFieldsProcessor : StoredFieldsConsumer
    {
        internal StoredFieldsWriter FieldsWriter;
        internal readonly DocumentsWriterPerThread DocWriter;
        internal int LastDocID;

        internal readonly DocumentsWriterPerThread.DocState DocState;
        internal readonly Codec Codec;

        public StoredFieldsProcessor(DocumentsWriterPerThread docWriter)
        {
            this.DocWriter = docWriter;
            this.DocState = docWriter.docState;
            this.Codec = docWriter.Codec;
        }

        private int NumStoredFields;
        private IndexableField[] StoredFields = new IndexableField[1];
        private FieldInfo[] FieldInfos = new FieldInfo[1];

        public void Reset()
        {
            NumStoredFields = 0;
            Arrays.Fill(StoredFields, null);
            Arrays.Fill(FieldInfos, null);
        }

        public override void StartDocument()
        {
            Reset();
        }

        public override void Flush(SegmentWriteState state)
        {
            int numDocs = state.SegmentInfo.DocCount;
            if (numDocs > 0)
            {
                // It's possible that all documents seen in this segment
                // hit non-aborting exceptions, in which case we will
                // not have yet init'd the FieldsWriter:
                InitFieldsWriter(state.Context);
                Fill(numDocs);
            }
            if (FieldsWriter != null)
            {
                bool success = false;
                try
                {
                    FieldsWriter.Finish(state.FieldInfos, numDocs);
                    success = true;
                }
                finally
                {
                    if (success)
                    {
                        IOUtils.Close(FieldsWriter);
                    }
                    else
                    {
                        IOUtils.CloseWhileHandlingException(FieldsWriter);
                    }
                }
            }
        }

        private void InitFieldsWriter(IOContext context)
        {
            lock (this)
            {
                if (FieldsWriter == null)
                {
                    FieldsWriter = Codec.StoredFieldsFormat.FieldsWriter(DocWriter.Directory, DocWriter.SegmentInfo, context);
                    LastDocID = 0;
                }
            }
        }

        public override void Abort()
        {
            Reset();

            if (FieldsWriter != null)
            {
                FieldsWriter.Abort();
                FieldsWriter = null;
                LastDocID = 0;
            }
        }

        /// <summary>
        /// Fills in any hole in the docIDs </summary>
        internal void Fill(int docID)
        {
            // We must "catch up" for all docs before us
            // that had no stored fields:
            while (LastDocID < docID)
            {
                FieldsWriter.StartDocument(0);
                LastDocID++;
                FieldsWriter.FinishDocument();
            }
        }

        public override void FinishDocument()
        {
            Debug.Assert(DocWriter.TestPoint("StoredFieldsWriter.finishDocument start"));

            InitFieldsWriter(IOContext.DEFAULT);
            Fill(DocState.DocID);

            if (FieldsWriter != null && NumStoredFields > 0)
            {
                FieldsWriter.StartDocument(NumStoredFields);
                for (int i = 0; i < NumStoredFields; i++)
                {
                    FieldsWriter.WriteField(FieldInfos[i], StoredFields[i]);
                }
                FieldsWriter.FinishDocument();
                LastDocID++;
            }

            Reset();
            Debug.Assert(DocWriter.TestPoint("StoredFieldsWriter.finishDocument end"));
        }

        public override void AddField(int docID, IndexableField field, FieldInfo fieldInfo)
        {
            if (field.FieldType.IsStored)
            {
                if (NumStoredFields == StoredFields.Length)
                {
                    int newSize = ArrayUtil.Oversize(NumStoredFields + 1, RamUsageEstimator.NUM_BYTES_OBJECT_REF);
                    IndexableField[] newArray = new IndexableField[newSize];
                    Array.Copy(StoredFields, 0, newArray, 0, NumStoredFields);
                    StoredFields = newArray;

                    FieldInfo[] newInfoArray = new FieldInfo[newSize];
                    Array.Copy(FieldInfos, 0, newInfoArray, 0, NumStoredFields);
                    FieldInfos = newInfoArray;
                }

                StoredFields[NumStoredFields] = field;
                FieldInfos[NumStoredFields] = fieldInfo;
                NumStoredFields++;

                Debug.Assert(DocState.TestPoint("StoredFieldsWriterPerThread.processFields.writeField"));
            }
        }
    }
}