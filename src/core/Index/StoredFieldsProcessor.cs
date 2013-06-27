using Lucene.Net.Codecs;
using Lucene.Net.Store;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Index
{
    internal sealed class StoredFieldsProcessor : StoredFieldsConsumer
    {
        internal StoredFieldsWriter fieldsWriter;
        internal readonly DocumentsWriterPerThread docWriter;
        internal int lastDocID;

        internal int freeCount;

        internal readonly DocumentsWriterPerThread.DocState docState;
        internal readonly Codec codec;

        public StoredFieldsProcessor(DocumentsWriterPerThread docWriter)
        {
            this.docWriter = docWriter;
            this.docState = docWriter.docState;
            this.codec = docWriter.codec;
        }

        private int numStoredFields;
        private IIndexableField[] storedFields;
        private FieldInfo[] fieldInfos;

        public void Reset()
        {
            numStoredFields = 0;
            storedFields = new IIndexableField[1];
            fieldInfos = new FieldInfo[1];
        }

        public override void StartDocument()
        {
            Reset();
        }

        public override void Flush(SegmentWriteState state)
        {
            int numDocs = state.segmentInfo.DocCount;

            if (numDocs > 0)
            {
                // It's possible that all documents seen in this segment
                // hit non-aborting exceptions, in which case we will
                // not have yet init'd the FieldsWriter:
                InitFieldsWriter(state.context);
                Fill(numDocs);
            }

            if (fieldsWriter != null)
            {
                try
                {
                    fieldsWriter.Finish(state.fieldInfos, numDocs);
                }
                finally
                {
                    fieldsWriter.Dispose();
                    fieldsWriter = null;
                    lastDocID = 0;
                }
            }
        }

        private void InitFieldsWriter(IOContext context)
        {
            lock (this)
            {
                if (fieldsWriter == null)
                {
                    fieldsWriter = codec.StoredFieldsFormat().FieldsWriter(docWriter.directory, docWriter.SegmentInfo, context);
                    lastDocID = 0;
                }
            }
        }

        internal int allocCount;

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

        public override void FinishDocument()
        {
            //assert docWriter.writer.testPoint("StoredFieldsWriter.finishDocument start");

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
            //assert docWriter.writer.testPoint("StoredFieldsWriter.finishDocument end");
        }

        public override void AddField(int docID, IIndexableField field, FieldInfo fieldInfo)
        {
            if (field.FieldType.Stored)
            {
                if (numStoredFields == storedFields.Length)
                {
                    int newSize = ArrayUtil.Oversize(numStoredFields + 1, RamUsageEstimator.NUM_BYTES_OBJECT_REF);
                    IIndexableField[] newArray = new IIndexableField[newSize];
                    Array.Copy(storedFields, 0, newArray, 0, numStoredFields);
                    storedFields = newArray;

                    FieldInfo[] newInfoArray = new FieldInfo[newSize];
                    Array.Copy(fieldInfos, 0, newInfoArray, 0, numStoredFields);
                    fieldInfos = newInfoArray;
                }

                storedFields[numStoredFields] = field;
                fieldInfos[numStoredFields] = fieldInfo;
                numStoredFields++;

                //assert docState.testPoint("StoredFieldsWriterPerThread.processFields.writeField");
            }
        }
    }
}
