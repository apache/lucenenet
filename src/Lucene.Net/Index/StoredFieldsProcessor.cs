using Lucene.Net.Diagnostics;
using Lucene.Net.Support;
using Lucene.Net.Support.Threading;
using System;
using System.Runtime.CompilerServices;

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

    using ArrayUtil = Lucene.Net.Util.ArrayUtil;
    using Codec = Lucene.Net.Codecs.Codec;
    using IOContext = Lucene.Net.Store.IOContext;
    using IOUtils = Lucene.Net.Util.IOUtils;
    using RamUsageEstimator = Lucene.Net.Util.RamUsageEstimator;
    using StoredFieldsWriter = Lucene.Net.Codecs.StoredFieldsWriter;

    /// <summary>
    /// This is a <see cref="StoredFieldsConsumer"/> that writes stored fields. </summary>
    internal sealed class StoredFieldsProcessor : StoredFieldsConsumer
    {
        internal StoredFieldsWriter fieldsWriter;
        internal readonly DocumentsWriterPerThread docWriter;
        internal int lastDocID;

        internal readonly DocumentsWriterPerThread.DocState docState;
        internal readonly Codec codec;

        public StoredFieldsProcessor(DocumentsWriterPerThread docWriter)
        {
            this.docWriter = docWriter;
            this.docState = docWriter.docState;
            this.codec = docWriter.codec;
        }

        private int numStoredFields;
        private IIndexableField[] storedFields = new IIndexableField[1];
        private FieldInfo[] fieldInfos = new FieldInfo[1];

        public void Reset()
        {
            numStoredFields = 0;
            Arrays.Fill(storedFields, null);
            Arrays.Fill(fieldInfos, null);
        }

        public override void StartDocument()
        {
            Reset();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
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
            if (fieldsWriter != null)
            {
                bool success = false;
                try
                {
                    fieldsWriter.Finish(state.FieldInfos, numDocs);
                    success = true;
                }
                finally
                {
                    if (success)
                    {
                        IOUtils.Dispose(fieldsWriter);
                    }
                    else
                    {
                        IOUtils.DisposeWhileHandlingException(fieldsWriter);
                    }
                }
            }
        }

        private void InitFieldsWriter(IOContext context)
        {
            UninterruptableMonitor.Enter(this);
            try
            {
                if (fieldsWriter is null)
                {
                    fieldsWriter = codec.StoredFieldsFormat.FieldsWriter(docWriter.directory, docWriter.SegmentInfo, context);
                    lastDocID = 0;
                }
            }
            finally
            {
                UninterruptableMonitor.Exit(this);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public override void Abort()
        {
            Reset();

            if (fieldsWriter != null)
            {
                fieldsWriter.Abort();
                fieldsWriter = null;
                lastDocID = 0;
            }
        }

        /// <summary>
        /// Fills in any hole in the docIDs </summary>
        internal void Fill(int docID)
        {
            // We must "catch up" for all docs before us
            // that had no stored fields:
            while (lastDocID < docID)
            {
                fieldsWriter.StartDocument(0);
                lastDocID++;
                fieldsWriter.FinishDocument();
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal override void FinishDocument()
        {
            if (Debugging.AssertsEnabled) Debugging.Assert(docWriter.TestPoint("StoredFieldsWriter.finishDocument start"));

            InitFieldsWriter(IOContext.DEFAULT);
            Fill(docState.docID);

            if (fieldsWriter != null && numStoredFields > 0)
            {
                fieldsWriter.StartDocument(numStoredFields);
                for (int i = 0; i < numStoredFields; i++)
                {
                    fieldsWriter.WriteField(fieldInfos[i], storedFields[i]);
                }
                fieldsWriter.FinishDocument();
                lastDocID++;
            }

            Reset();
            if (Debugging.AssertsEnabled) Debugging.Assert(docWriter.TestPoint("StoredFieldsWriter.finishDocument end"));
        }

        public override void AddField(int docID, IIndexableField field, FieldInfo fieldInfo)
        {
            if (field.IndexableFieldType.IsStored)
            {
                if (numStoredFields == storedFields.Length)
                {
                    int newSize = ArrayUtil.Oversize(numStoredFields + 1, RamUsageEstimator.NUM_BYTES_OBJECT_REF);
                    IIndexableField[] newArray = new IIndexableField[newSize];
                    Arrays.Copy(storedFields, 0, newArray, 0, numStoredFields);
                    storedFields = newArray;

                    FieldInfo[] newInfoArray = new FieldInfo[newSize];
                    Arrays.Copy(fieldInfos, 0, newInfoArray, 0, numStoredFields);
                    fieldInfos = newInfoArray;
                }

                storedFields[numStoredFields] = field;
                fieldInfos[numStoredFields] = fieldInfo;
                numStoredFields++;

                if (Debugging.AssertsEnabled) Debugging.Assert(docState.TestPoint("StoredFieldsWriterPerThread.processFields.writeField"));
            }
        }
    }
}