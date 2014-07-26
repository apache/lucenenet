using System;

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
        private readonly FieldInfo FieldInfo;
        private readonly DocumentsWriterPerThread.DocState DocState;
        private readonly Similarity Similarity;
        private readonly FieldInvertState FieldState;
        private NumericDocValuesWriter Consumer;

        public NormsConsumerPerField(DocInverterPerField docInverterPerField, FieldInfo fieldInfo, NormsConsumer parent)
        {
            this.FieldInfo = fieldInfo;
            DocState = docInverterPerField.DocState;
            FieldState = docInverterPerField.FieldState;
            Similarity = DocState.Similarity;
        }

        public int CompareTo(NormsConsumerPerField other)
        {
            return FieldInfo.Name.CompareTo(other.FieldInfo.Name);
        }

        internal override void Finish()
        {
            if (FieldInfo.Indexed && !FieldInfo.OmitsNorms())
            {
                if (Consumer == null)
                {
                    FieldInfo.NormType = FieldInfo.DocValuesType_e.NUMERIC;
                    Consumer = new NumericDocValuesWriter(FieldInfo, DocState.DocWriter.bytesUsed, false);
                }
                Consumer.AddValue(DocState.DocID, Similarity.ComputeNorm(FieldState));
            }
        }

        internal void Flush(SegmentWriteState state, DocValuesConsumer normsWriter)
        {
            int docCount = state.SegmentInfo.DocCount;
            if (Consumer == null)
            {
                return; // null type - not omitted but not written -
                // meaning the only docs that had
                // norms hit exceptions (but indexed=true is set...)
            }
            Consumer.Finish(docCount);
            Consumer.Flush(state, normsWriter);
        }

        internal bool Empty
        {
            get
            {
                return Consumer == null;
            }
        }

        internal override void Abort()
        {
            //
        }
    }
}