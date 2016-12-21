using System.Collections.Generic;

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

    using ByteBlockPool = Lucene.Net.Util.ByteBlockPool;
    using BytesRef = Lucene.Net.Util.BytesRef;
    using Counter = Lucene.Net.Util.Counter;
    using IntBlockPool = Lucene.Net.Util.IntBlockPool;

    /// <summary>
    /// this class implements <seealso cref="InvertedDocConsumer"/>, which
    ///  is passed each token produced by the analyzer on each
    ///  field.  It stores these tokens in a hash table, and
    ///  allocates separate byte streams per token.  Consumers of
    ///  this class, eg <seealso cref="FreqProxTermsWriter"/> and {@link
    ///  TermVectorsConsumer}, write their own byte streams
    ///  under each term.
    /// </summary>
    internal sealed class TermsHash : InvertedDocConsumer
    {
        internal readonly TermsHashConsumer Consumer;
        internal readonly TermsHash NextTermsHash;

        internal readonly IntBlockPool IntPool;
        internal readonly ByteBlockPool BytePool;
        internal ByteBlockPool TermBytePool;
        internal readonly Counter BytesUsed;

        internal readonly bool Primary;
        internal readonly DocumentsWriterPerThread.DocState DocState;

        // Used when comparing postings via termRefComp, in TermsHashPerField
        internal readonly BytesRef Tr1 = new BytesRef();

        internal readonly BytesRef Tr2 = new BytesRef();

        // Used by perField to obtain terms from the analysis chain
        internal readonly BytesRef TermBytesRef = new BytesRef(10);

        internal readonly bool TrackAllocations;

        public TermsHash(DocumentsWriterPerThread docWriter, TermsHashConsumer consumer, bool trackAllocations, TermsHash nextTermsHash)
        {
            this.DocState = docWriter.docState;
            this.Consumer = consumer;
            this.TrackAllocations = trackAllocations;
            this.NextTermsHash = nextTermsHash;
            this.BytesUsed = trackAllocations ? docWriter.bytesUsed : Counter.NewCounter();
            IntPool = new IntBlockPool(docWriter.intBlockAllocator);
            BytePool = new ByteBlockPool(docWriter.ByteBlockAllocator);

            if (nextTermsHash != null)
            {
                // We are primary
                Primary = true;
                TermBytePool = BytePool;
                nextTermsHash.TermBytePool = BytePool;
            }
            else
            {
                Primary = false;
            }
        }

        public override void Abort()
        {
            Reset();
            try
            {
                Consumer.Abort();
            }
            finally
            {
                if (NextTermsHash != null)
                {
                    NextTermsHash.Abort();
                }
            }
        }

        // Clear all state
        internal void Reset()
        {
            // we don't reuse so we drop everything and don't fill with 0
            IntPool.Reset(false, false);
            BytePool.Reset(false, false);
        }

        internal override void Flush(IDictionary<string, InvertedDocConsumerPerField> fieldsToFlush, SegmentWriteState state)
        {
            IDictionary<string, TermsHashConsumerPerField> childFields = new Dictionary<string, TermsHashConsumerPerField>();
            IDictionary<string, InvertedDocConsumerPerField> nextChildFields;

            if (NextTermsHash != null)
            {
                nextChildFields = new Dictionary<string, InvertedDocConsumerPerField>();
            }
            else
            {
                nextChildFields = null;
            }

            foreach (KeyValuePair<string, InvertedDocConsumerPerField> entry in fieldsToFlush)
            {
                TermsHashPerField perField = (TermsHashPerField)entry.Value;
                childFields[entry.Key] = perField.Consumer;
                if (NextTermsHash != null)
                {
                    nextChildFields[entry.Key] = perField.NextPerField;
                }
            }

            Consumer.Flush(childFields, state);

            if (NextTermsHash != null)
            {
                NextTermsHash.Flush(nextChildFields, state);
            }
        }

        internal override InvertedDocConsumerPerField AddField(DocInverterPerField docInverterPerField, FieldInfo fieldInfo)
        {
            return new TermsHashPerField(docInverterPerField, this, NextTermsHash, fieldInfo);
        }

        internal override void FinishDocument()
        {
            Consumer.FinishDocument(this);
            if (NextTermsHash != null)
            {
                NextTermsHash.Consumer.FinishDocument(NextTermsHash);
            }
        }

        internal override void StartDocument()
        {
            Consumer.StartDocument();
            if (NextTermsHash != null)
            {
                NextTermsHash.Consumer.StartDocument();
            }
        }
    }
}