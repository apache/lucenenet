using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Lucene.Net.Index
{
    using Lucene.Net.Support;
    using ArrayUtil = Lucene.Net.Util.ArrayUtil;
    using BytesRef = Lucene.Net.Util.BytesRef;
    using FlushInfo = Lucene.Net.Store.FlushInfo;
    using IOContext = Lucene.Net.Store.IOContext;
    using IOUtils = Lucene.Net.Util.IOUtils;
    using RamUsageEstimator = Lucene.Net.Util.RamUsageEstimator;

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

    using TermVectorsWriter = Lucene.Net.Codecs.TermVectorsWriter;

    public sealed class TermVectorsConsumer : TermsHashConsumer
    {
        internal TermVectorsWriter Writer;
        internal readonly DocumentsWriterPerThread DocWriter;
        internal readonly DocumentsWriterPerThread.DocState DocState;
        internal readonly BytesRef FlushTerm = new BytesRef();

        // Used by perField when serializing the term vectors
        internal readonly ByteSliceReader VectorSliceReaderPos = new ByteSliceReader();

        internal readonly ByteSliceReader VectorSliceReaderOff = new ByteSliceReader();
        internal bool HasVectors;
        internal int NumVectorFields;
        internal int LastDocID;
        private TermVectorsConsumerPerField[] PerFields = new TermVectorsConsumerPerField[1];

        public TermVectorsConsumer(DocumentsWriterPerThread docWriter)
        {
            this.DocWriter = docWriter;
            DocState = docWriter.docState;
        }

        public override void Flush(IDictionary<string, TermsHashConsumerPerField> fieldsToFlush, SegmentWriteState state)
        {
            if (Writer != null)
            {
                int numDocs = state.SegmentInfo.DocCount;
                Debug.Assert(numDocs > 0);
                // At least one doc in this run had term vectors enabled
                try
                {
                    Fill(numDocs);
                    Debug.Assert(state.SegmentInfo != null);
                    Writer.Finish(state.FieldInfos, numDocs);
                }
                finally
                {
                    IOUtils.Close(Writer);
                    Writer = null;
                    LastDocID = 0;
                    HasVectors = false;
                }
            }

            foreach (TermsHashConsumerPerField field in fieldsToFlush.Values)
            {
                TermVectorsConsumerPerField perField = (TermVectorsConsumerPerField)field;
                perField.TermsHashPerField.Reset();
                perField.ShrinkHash();
            }
        }

        /// <summary>
        /// Fills in no-term-vectors for all docs we haven't seen
        ///  since the last doc that had term vectors.
        /// </summary>
        internal void Fill(int docID)
        {
            while (LastDocID < docID)
            {
                Writer.StartDocument(0);
                Writer.FinishDocument();
                LastDocID++;
            }
        }

        private void InitTermVectorsWriter()
        {
            if (Writer == null)
            {
                IOContext context = new IOContext(new FlushInfo(DocWriter.NumDocsInRAM, DocWriter.BytesUsed()));
                Writer = DocWriter.Codec.TermVectorsFormat.VectorsWriter(DocWriter.Directory, DocWriter.SegmentInfo, context);
                LastDocID = 0;
            }
        }

        public override void FinishDocument(TermsHash termsHash)
        {
            Debug.Assert(DocWriter.TestPoint("TermVectorsTermsWriter.finishDocument start"));

            if (!HasVectors)
            {
                return;
            }

            InitTermVectorsWriter();

            Fill(DocState.DocID);

            // Append term vectors to the real outputs:
            Writer.StartDocument(NumVectorFields);
            for (int i = 0; i < NumVectorFields; i++)
            {
                PerFields[i].FinishDocument();
            }
            Writer.FinishDocument();

            Debug.Assert(LastDocID == DocState.DocID, "lastDocID=" + LastDocID + " docState.docID=" + DocState.DocID);

            LastDocID++;

            termsHash.Reset();
            Reset();
            Debug.Assert(DocWriter.TestPoint("TermVectorsTermsWriter.finishDocument end"));
        }

        public override void Abort()
        {
            HasVectors = false;

            if (Writer != null)
            {
                Writer.Abort();
                Writer = null;
            }

            LastDocID = 0;
            Reset();
        }

        internal void Reset()
        {
            Arrays.Fill(PerFields, null); // don't hang onto stuff from previous doc
            NumVectorFields = 0;
        }

        public override TermsHashConsumerPerField AddField(TermsHashPerField termsHashPerField, FieldInfo fieldInfo)
        {
            return new TermVectorsConsumerPerField(termsHashPerField, this, fieldInfo);
        }

        internal void AddFieldToFlush(TermVectorsConsumerPerField fieldToFlush)
        {
            if (NumVectorFields == PerFields.Length)
            {
                int newSize = ArrayUtil.Oversize(NumVectorFields + 1, RamUsageEstimator.NUM_BYTES_OBJECT_REF);
                TermVectorsConsumerPerField[] newArray = new TermVectorsConsumerPerField[newSize];
                Array.Copy(PerFields, 0, newArray, 0, NumVectorFields);
                PerFields = newArray;
            }

            PerFields[NumVectorFields++] = fieldToFlush;
        }

        public override void StartDocument()
        {
            Debug.Assert(ClearLastVectorFieldName());
            Reset();
        }

        // Called only by assert
        internal bool ClearLastVectorFieldName()
        {
            LastVectorFieldName = null;
            return true;
        }

        // Called only by assert
        internal string LastVectorFieldName;

        internal bool VectorFieldsInOrder(FieldInfo fi)
        {
            try
            {
                return LastVectorFieldName != null ? LastVectorFieldName.CompareTo(fi.Name) < 0 : true;
            }
            finally
            {
                LastVectorFieldName = fi.Name;
            }
        }
    }
}