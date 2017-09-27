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

    /// <summary>
    /// This is a <see cref="DocFieldConsumer"/> that inverts each field,
    /// separately, from a <see cref="Documents.Document"/>, and accepts a
    /// <see cref="InvertedDocConsumer"/> to process those terms.
    /// </summary>
    internal sealed class DocInverter : DocFieldConsumer
    {
        internal readonly InvertedDocConsumer consumer;
        internal readonly InvertedDocEndConsumer endConsumer;

        internal readonly DocumentsWriterPerThread.DocState docState;

        public DocInverter(DocumentsWriterPerThread.DocState docState, InvertedDocConsumer consumer, InvertedDocEndConsumer endConsumer)
        {
            this.docState = docState;
            this.consumer = consumer;
            this.endConsumer = endConsumer;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal override void Flush(IDictionary<string, DocFieldConsumerPerField> fieldsToFlush, SegmentWriteState state)
        {
            IDictionary<string, InvertedDocConsumerPerField> childFieldsToFlush = new Dictionary<string, InvertedDocConsumerPerField>();
            IDictionary<string, InvertedDocEndConsumerPerField> endChildFieldsToFlush = new Dictionary<string, InvertedDocEndConsumerPerField>();

            foreach (KeyValuePair<string, DocFieldConsumerPerField> fieldToFlush in fieldsToFlush)
            {
                DocInverterPerField perField = (DocInverterPerField)fieldToFlush.Value;
                childFieldsToFlush[fieldToFlush.Key] = perField.consumer;
                endChildFieldsToFlush[fieldToFlush.Key] = perField.endConsumer;
            }

            consumer.Flush(childFieldsToFlush, state);
            endConsumer.Flush(endChildFieldsToFlush, state);
        }

        public override void StartDocument()
        {
            consumer.StartDocument();
            endConsumer.StartDocument();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public override void FinishDocument()
        {
            // TODO: allow endConsumer.finishDocument to also return
            // a DocWriter
            endConsumer.FinishDocument();
            consumer.FinishDocument();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal override void Abort()
        {
            try
            {
                consumer.Abort();
            }
            finally
            {
                endConsumer.Abort();
            }
        }

        public override DocFieldConsumerPerField AddField(FieldInfo fi)
        {
            return new DocInverterPerField(this, fi);
        }
    }
}