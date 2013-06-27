using Lucene.Net.Codecs;
using Lucene.Net.Store;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Index
{
    internal sealed class TermVectorsConsumer : TermsHashConsumer
    {
        internal TermVectorsWriter writer;
        internal readonly DocumentsWriterPerThread docWriter;
        internal int freeCount;
        internal int lastDocID;

        internal readonly DocumentsWriterPerThread.DocState docState;
        internal readonly BytesRef flushTerm = new BytesRef();

        // Used by perField when serializing the term vectors
        internal readonly ByteSliceReader vectorSliceReaderPos = new ByteSliceReader();
        internal readonly ByteSliceReader vectorSliceReaderOff = new ByteSliceReader();
        internal bool hasVectors;

        public TermVectorsConsumer(DocumentsWriterPerThread docWriter)
        {
            this.docWriter = docWriter;
            docState = docWriter.docState;
        }

        public override void Flush(IDictionary<string, TermsHashConsumerPerField> fieldsToFlush, SegmentWriteState state)
        {
            if (writer != null)
            {
                int numDocs = state.segmentInfo.DocCount;
                // At least one doc in this run had term vectors enabled
                try
                {
                    Fill(numDocs);
                    //assert state.segmentInfo != null;
                    writer.Finish(state.fieldInfos, numDocs);
                }
                finally
                {
                    IOUtils.Close(writer);
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

        internal void Fill(int docID)
        {
            while (lastDocID < docID)
            {
                writer.StartDocument(0);
                writer.FinishDocument();
                lastDocID++;
            }
        }

        private void InitTermVectorsWriter()
        {
            if (writer == null)
            {
                IOContext context = new IOContext(new FlushInfo(docWriter.NumDocsInRAM, docWriter.BytesUsed));
                writer = docWriter.codec.TermVectorsFormat().VectorsWriter(docWriter.directory, docWriter.SegmentInfo, context);
                lastDocID = 0;
            }
        }

        public override void FinishDocument(TermsHash termsHash)
        {
            //assert docWriter.writer.testPoint("TermVectorsTermsWriter.finishDocument start");

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

            //assert lastDocID == docState.docID: "lastDocID=" + lastDocID + " docState.docID=" + docState.docID;

            lastDocID++;

            termsHash.Reset();
            Reset();
            //assert docWriter.writer.testPoint("TermVectorsTermsWriter.finishDocument end");
        }

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

        internal int numVectorFields;

        internal TermVectorsConsumerPerField[] perFields;

        internal void Reset()
        {
            numVectorFields = 0;
            perFields = new TermVectorsConsumerPerField[1];
        }

        public override TermsHashConsumerPerField AddField(TermsHashPerField termsHashPerField, FieldInfo fieldInfo)
        {
            return new TermVectorsConsumerPerField(termsHashPerField, this, fieldInfo);
        }

        internal void AddFieldToFlush(TermVectorsConsumerPerField fieldToFlush)
        {
            if (numVectorFields == perFields.Length)
            {
                int newSize = ArrayUtil.Oversize(numVectorFields + 1, RamUsageEstimator.NUM_BYTES_OBJECT_REF);
                TermVectorsConsumerPerField[] newArray = new TermVectorsConsumerPerField[newSize];
                Array.Copy(perFields, 0, newArray, 0, numVectorFields);
                perFields = newArray;
            }

            perFields[numVectorFields++] = fieldToFlush;
        }

        public override void StartDocument()
        {
            //assert clearLastVectorFieldName();
            Reset();
        }

        // Called only by assert
        internal bool ClearLastVectorFieldName()
        {
            lastVectorFieldName = null;
            return true;
        }

        // Called only by assert
        String lastVectorFieldName;
        internal bool VectorFieldsInOrder(FieldInfo fi)
        {
            try
            {
                if (lastVectorFieldName != null)
                    return lastVectorFieldName.CompareTo(fi.name) < 0;
                else
                    return true;
            }
            finally
            {
                lastVectorFieldName = fi.name;
            }
        }
    }
}
