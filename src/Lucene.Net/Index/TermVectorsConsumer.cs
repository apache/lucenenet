using J2N.Text;
using Lucene.Net.Diagnostics;
using Lucene.Net.Support;
using System;
using System.Collections.Generic;
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
    using BytesRef = Lucene.Net.Util.BytesRef;
    using FlushInfo = Lucene.Net.Store.FlushInfo;
    using IOContext = Lucene.Net.Store.IOContext;
    using IOUtils = Lucene.Net.Util.IOUtils;
    using RamUsageEstimator = Lucene.Net.Util.RamUsageEstimator;
    using TermVectorsWriter = Lucene.Net.Codecs.TermVectorsWriter;

    internal sealed class TermVectorsConsumer : TermsHashConsumer
    {
        internal TermVectorsWriter writer;
        internal readonly DocumentsWriterPerThread docWriter;
        internal readonly DocumentsWriterPerThread.DocState docState;
        internal readonly BytesRef flushTerm = new BytesRef();

        // Used by perField when serializing the term vectors
        internal readonly ByteSliceReader vectorSliceReaderPos = new ByteSliceReader();

        internal readonly ByteSliceReader vectorSliceReaderOff = new ByteSliceReader();
        internal bool hasVectors;
        internal int numVectorFields;
        internal int lastDocID;
        private TermVectorsConsumerPerField[] perFields = new TermVectorsConsumerPerField[1];

        public TermVectorsConsumer(DocumentsWriterPerThread docWriter)
        {
            this.docWriter = docWriter;
            docState = docWriter.docState;
        }

        // LUCENENE specific - original was internal, but FreqProxTermsWriter requires public (little point, since both are internal classes)
        [MethodImpl(MethodImplOptions.NoInlining)]
        public override void Flush(IDictionary<string, TermsHashConsumerPerField> fieldsToFlush, SegmentWriteState state)
        {
            if (writer != null)
            {
                int numDocs = state.SegmentInfo.DocCount;
                if (Debugging.AssertsEnabled) Debugging.Assert(numDocs > 0);
                // At least one doc in this run had term vectors enabled
                try
                {
                    Fill(numDocs);
                    if (Debugging.AssertsEnabled) Debugging.Assert(state.SegmentInfo != null);
                    writer.Finish(state.FieldInfos, numDocs);
                }
                finally
                {
                    IOUtils.Dispose(writer);
                    writer = null;
                    lastDocID = 0;
                    hasVectors = false;
                }
            }

            foreach (TermsHashConsumerPerField field in fieldsToFlush.Values)
            {
                TermVectorsConsumerPerField perField = (TermVectorsConsumerPerField)field;
                perField.termsHashPerField.Reset();
                perField.ShrinkHash();
            }
        }

        /// <summary>
        /// Fills in no-term-vectors for all docs we haven't seen
        /// since the last doc that had term vectors.
        /// </summary>
        internal void Fill(int docID)
        {
            while (lastDocID < docID)
            {
                writer.StartDocument(0);
                writer.FinishDocument();
                lastDocID++;
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void InitTermVectorsWriter()
        {
            if (writer is null)
            {
                IOContext context = new IOContext(new FlushInfo(docWriter.NumDocsInRAM, docWriter.BytesUsed));
                writer = docWriter.codec.TermVectorsFormat.VectorsWriter(docWriter.directory, docWriter.SegmentInfo, context);
                lastDocID = 0;
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal override void FinishDocument(TermsHash termsHash)
        {
            if (Debugging.AssertsEnabled) Debugging.Assert(docWriter.TestPoint("TermVectorsTermsWriter.finishDocument start"));

            if (!hasVectors)
            {
                return;
            }

            InitTermVectorsWriter();

            Fill(docState.docID);

            // Append term vectors to the real outputs:
            writer.StartDocument(numVectorFields);
            for (int i = 0; i < numVectorFields; i++)
            {
                perFields[i].FinishDocument();
            }
            writer.FinishDocument();

            if (Debugging.AssertsEnabled) Debugging.Assert(lastDocID == docState.docID,"lastDocID={0} docState.docID={1}", lastDocID, docState.docID);

            lastDocID++;

            termsHash.Reset();
            Reset();
            if (Debugging.AssertsEnabled) Debugging.Assert(docWriter.TestPoint("TermVectorsTermsWriter.finishDocument end"));
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public override void Abort()
        {
            hasVectors = false;

            if (writer != null)
            {
                writer.Abort();
                writer = null;
            }

            lastDocID = 0;
            Reset();
        }

        internal void Reset()
        {
            Arrays.Fill(perFields, null); // don't hang onto stuff from previous doc
            numVectorFields = 0;
        }

        public override TermsHashConsumerPerField AddField(TermsHashPerField termsHashPerField, FieldInfo fieldInfo)
        {
            return new TermVectorsConsumerPerField(termsHashPerField, this, fieldInfo);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal void AddFieldToFlush(TermVectorsConsumerPerField fieldToFlush)
        {
            if (numVectorFields == perFields.Length)
            {
                int newSize = ArrayUtil.Oversize(numVectorFields + 1, RamUsageEstimator.NUM_BYTES_OBJECT_REF);
                TermVectorsConsumerPerField[] newArray = new TermVectorsConsumerPerField[newSize];
                Arrays.Copy(perFields, 0, newArray, 0, numVectorFields);
                perFields = newArray;
            }

            perFields[numVectorFields++] = fieldToFlush;
        }

        internal override void StartDocument()
        {
            if (Debugging.AssertsEnabled) Debugging.Assert(ClearLastVectorFieldName());
            Reset();
        }

        // Called only by assert
        internal bool ClearLastVectorFieldName()
        {
            lastVectorFieldName = null;
            return true;
        }

        // Called only by assert
        internal string lastVectorFieldName;

        internal bool VectorFieldsInOrder(FieldInfo fi)
        {
            try
            {
                return lastVectorFieldName != null ? lastVectorFieldName.CompareToOrdinal(fi.Name) < 0 : true;
            }
            finally
            {
                lastVectorFieldName = fi.Name;
            }
        }
    }
}