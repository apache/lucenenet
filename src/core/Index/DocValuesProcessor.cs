using Lucene.Net.Codecs;
using Lucene.Net.Support;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Index
{
    internal sealed class DocValuesProcessor : StoredFieldsConsumer
    {
        // TODO: somewhat wasteful we also keep a map here; would
        // be more efficient if we could "reuse" the map/hash
        // lookup DocFieldProcessor already did "above"
        private readonly IDictionary<String, DocValuesWriter> writers = new HashMap<String, DocValuesWriter>();
        private readonly Counter bytesUsed;

        public DocValuesProcessor(Counter bytesUsed)
        {
            this.bytesUsed = bytesUsed;
        }

        public override void StartDocument()
        {
        }

        public override void FinishDocument()
        {
        }

        public override void AddField(int docID, IIndexableField field, FieldInfo fieldInfo)
        {
            FieldInfo.DocValuesType dvType = field.FieldType.DocValueType;
            if (dvType != null)
            {
                fieldInfo.DocValuesTypeValue = dvType;
                if (dvType == FieldInfo.DocValuesType.BINARY)
                {
                    AddBinaryField(fieldInfo, docID, field.BinaryValue);
                }
                else if (dvType == FieldInfo.DocValuesType.SORTED)
                {
                    AddSortedField(fieldInfo, docID, field.BinaryValue);
                }
                else if (dvType == FieldInfo.DocValuesType.SORTED_SET)
                {
                    AddSortedSetField(fieldInfo, docID, field.BinaryValue);
                }
                else if (dvType == FieldInfo.DocValuesType.NUMERIC)
                {
                    if (!(field.NumericValue is long))
                    {
                        throw new ArgumentException("illegal type " + field.NumericValue.GetType().Name + ": DocValues types must be Long");
                    }
                    AddNumericField(fieldInfo, docID, (long)field.NumericValue);
                }
                else
                {
                    //assert false: "unrecognized DocValues.Type: " + dvType;
                }
            }
        }

        public override void Flush(SegmentWriteState state)
        {
            if (writers.Count > 0)
            {
                DocValuesFormat fmt = state.segmentInfo.Codec.DocValuesFormat();
                DocValuesConsumer dvConsumer = fmt.FieldsConsumer(state);
                bool success = false;
                try
                {
                    foreach (DocValuesWriter writer in writers.Values)
                    {
                        writer.Finish(state.segmentInfo.DocCount);
                        writer.Flush(state, dvConsumer);
                    }
                    // TODO: catch missing DV fields here?  else we have
                    // null/"" depending on how docs landed in segments?
                    // but we can't detect all cases, and we should leave
                    // this behavior undefined. dv is not "schemaless": its column-stride.
                    writers.Clear();
                    success = true;
                }
                finally
                {
                    if (success)
                    {
                        IOUtils.Close(dvConsumer);
                    }
                    else
                    {
                        IOUtils.CloseWhileHandlingException((IDisposable)dvConsumer);
                    }
                }
            }
        }

        internal void AddBinaryField(FieldInfo fieldInfo, int docID, BytesRef value)
        {
            DocValuesWriter writer = writers[fieldInfo.name];
            BinaryDocValuesWriter binaryWriter;
            if (writer == null)
            {
                binaryWriter = new BinaryDocValuesWriter(fieldInfo, bytesUsed);
                writers[fieldInfo.name] = binaryWriter;
            }
            else if (!(writer is BinaryDocValuesWriter))
            {
                throw new ArgumentException("Incompatible DocValues type: field \"" + fieldInfo.name + "\" changed from " + GetTypeDesc(writer) + " to binary");
            }
            else
            {
                binaryWriter = (BinaryDocValuesWriter)writer;
            }
            binaryWriter.AddValue(docID, value);
        }

        internal void AddSortedField(FieldInfo fieldInfo, int docID, BytesRef value)
        {
            DocValuesWriter writer = writers[fieldInfo.name];
            SortedDocValuesWriter sortedWriter;
            if (writer == null)
            {
                sortedWriter = new SortedDocValuesWriter(fieldInfo, bytesUsed);
                writers[fieldInfo.name] = sortedWriter;
            }
            else if (!(writer is SortedDocValuesWriter))
            {
                throw new ArgumentException("Incompatible DocValues type: field \"" + fieldInfo.name + "\" changed from " + GetTypeDesc(writer) + " to sorted");
            }
            else
            {
                sortedWriter = (SortedDocValuesWriter)writer;
            }
            sortedWriter.AddValue(docID, value);
        }

        internal void AddSortedSetField(FieldInfo fieldInfo, int docID, BytesRef value)
        {
            DocValuesWriter writer = writers[fieldInfo.name];
            SortedSetDocValuesWriter sortedSetWriter;
            if (writer == null)
            {
                sortedSetWriter = new SortedSetDocValuesWriter(fieldInfo, bytesUsed);
                writers[fieldInfo.name] = sortedSetWriter;
            }
            else if (!(writer is SortedSetDocValuesWriter))
            {
                throw new ArgumentException("Incompatible DocValues type: field \"" + fieldInfo.name + "\" changed from " + GetTypeDesc(writer) + " to sorted");
            }
            else
            {
                sortedSetWriter = (SortedSetDocValuesWriter)writer;
            }
            sortedSetWriter.AddValue(docID, value);
        }

        internal void AddNumericField(FieldInfo fieldInfo, int docID, long value)
        {
            DocValuesWriter writer = writers[fieldInfo.name];
            NumericDocValuesWriter numericWriter;
            if (writer == null)
            {
                numericWriter = new NumericDocValuesWriter(fieldInfo, bytesUsed);
                writers[fieldInfo.name] = numericWriter;
            }
            else if (!(writer is NumericDocValuesWriter))
            {
                throw new ArgumentException("Incompatible DocValues type: field \"" + fieldInfo.name + "\" changed from " + GetTypeDesc(writer) + " to numeric");
            }
            else
            {
                numericWriter = (NumericDocValuesWriter)writer;
            }
            numericWriter.AddValue(docID, value);
        }

        private String GetTypeDesc(DocValuesWriter obj)
        {
            if (obj is BinaryDocValuesWriter)
            {
                return "binary";
            }
            else if (obj is NumericDocValuesWriter)
            {
                return "numeric";
            }
            else
            {
                //assert obj instanceof SortedDocValuesWriter;
                return "sorted";
            }
        }

        public override void Abort()
        {
            foreach (DocValuesWriter writer in writers.Values)
            {
                try
                {
                    writer.Abort();
                }
                catch
                {
                }
            }
            writers.Clear();
        }
    }
}
