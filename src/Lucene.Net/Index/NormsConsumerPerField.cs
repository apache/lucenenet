using J2N.Text;
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

    using DocValuesConsumer = Lucene.Net.Codecs.DocValuesConsumer;
    using Similarity = Lucene.Net.Search.Similarities.Similarity;

    internal sealed class NormsConsumerPerField : InvertedDocEndConsumerPerField, IComparable<NormsConsumerPerField>
    {
        private readonly FieldInfo fieldInfo;
        private readonly DocumentsWriterPerThread.DocState docState;
        private readonly Similarity similarity;
        private readonly FieldInvertState fieldState;
        private NumericDocValuesWriter consumer;

        public NormsConsumerPerField(DocInverterPerField docInverterPerField, FieldInfo fieldInfo /*, NormsConsumer parent // LUCENENET: Not referenced */)
        {
            this.fieldInfo = fieldInfo;
            docState = docInverterPerField.docState;
            fieldState = docInverterPerField.fieldState;
            similarity = docState.similarity;
        }

        public int CompareTo(NormsConsumerPerField other)
        {
            return fieldInfo.Name.CompareToOrdinal(other.fieldInfo.Name);
        }

        internal override void Finish()
        {
            if (fieldInfo.IsIndexed && !fieldInfo.OmitsNorms)
            {
                if (consumer is null)
                {
                    fieldInfo.NormType = DocValuesType.NUMERIC;
                    consumer = new NumericDocValuesWriter(fieldInfo, docState.docWriter.bytesUsed, false);
                }
                consumer.AddValue(docState.docID, similarity.ComputeNorm(fieldState));
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal void Flush(SegmentWriteState state, DocValuesConsumer normsWriter)
        {
            int docCount = state.SegmentInfo.DocCount;
            if (consumer is null)
            {
                return; // null type - not omitted but not written -
                // meaning the only docs that had
                // norms hit exceptions (but indexed=true is set...)
            }
            consumer.Finish(docCount);
            consumer.Flush(state, normsWriter);
        }

        internal bool IsEmpty => consumer is null;

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal override void Abort()
        {
            //
        }
    }
}