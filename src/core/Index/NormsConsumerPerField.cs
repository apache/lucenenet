using Lucene.Net.Codecs;
using Lucene.Net.Search.Similarities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Index
{
    internal sealed class NormsConsumerPerField : InvertedDocEndConsumerPerField, IComparable<NormsConsumerPerField>
    {
        private readonly FieldInfo fieldInfo;
        private readonly DocumentsWriterPerThread.DocState docState;
        private readonly Similarity similarity;
        private readonly FieldInvertState fieldState;
        private NumericDocValuesWriter consumer;

        public NormsConsumerPerField(DocInverterPerField docInverterPerField, FieldInfo fieldInfo, NormsConsumer parent)
        {
            this.fieldInfo = fieldInfo;
            docState = docInverterPerField.docState;
            fieldState = docInverterPerField.fieldState;
            similarity = docState.similarity;
        }

        public int CompareTo(NormsConsumerPerField other)
        {
            return fieldInfo.name.CompareTo(other.fieldInfo.name);
        }

        public override void Finish()
        {
            if (fieldInfo.IsIndexed && !fieldInfo.OmitsNorms)
            {
                if (consumer == null)
                {
                    fieldInfo.NormType = FieldInfo.DocValuesType.NUMERIC;
                    consumer = new NumericDocValuesWriter(fieldInfo, docState.docWriter.bytesUsed);
                }
                consumer.AddValue(docState.docID, similarity.ComputeNorm(fieldState));
            }
        }

        internal void Flush(SegmentWriteState state, DocValuesConsumer normsWriter)
        {
            int docCount = state.segmentInfo.DocCount;
            if (consumer == null)
            {
                return; // null type - not omitted but not written -
                // meaning the only docs that had
                // norms hit exceptions (but indexed=true is set...)
            }
            consumer.Finish(docCount);
            consumer.Flush(state, normsWriter);
        }

        internal bool IsEmpty
        {
            get { return consumer == null; }
        }

        public override void Abort()
        {
        }
    }
}
