namespace Lucene.Net.Codecs.Lucene3x
{
    using Lucene.Net.Support;

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

    using IndexOutput = Lucene.Net.Store.IndexOutput;

    /// <summary>
    /// PreFlexRW skiplist implementation.
    /// @lucene.experimental
    /// </summary>
    public class PreFlexRWSkipListWriter : MultiLevelSkipListWriter
    {
        private int[] LastSkipDoc;
        private int[] LastSkipPayloadLength;
        private long[] LastSkipFreqPointer;
        private long[] LastSkipProxPointer;

        private IndexOutput FreqOutput;
        private IndexOutput ProxOutput;

        private int CurDoc;
        private bool CurStorePayloads;
        private int CurPayloadLength;
        private long CurFreqPointer;
        private long CurProxPointer;

        public PreFlexRWSkipListWriter(int skipInterval, int numberOfSkipLevels, int docCount, IndexOutput freqOutput, IndexOutput proxOutput)
            : base(skipInterval, numberOfSkipLevels, docCount)
        {
            this.FreqOutput = freqOutput;
            this.ProxOutput = proxOutput;

            LastSkipDoc = new int[numberOfSkipLevels];
            LastSkipPayloadLength = new int[numberOfSkipLevels];
            LastSkipFreqPointer = new long[numberOfSkipLevels];
            LastSkipProxPointer = new long[numberOfSkipLevels];
        }

        /// <summary>
        /// Sets the values for the current skip data.
        /// </summary>
        public virtual void SetSkipData(int doc, bool storePayloads, int payloadLength)
        {
            this.CurDoc = doc;
            this.CurStorePayloads = storePayloads;
            this.CurPayloadLength = payloadLength;
            this.CurFreqPointer = FreqOutput.FilePointer;
            if (ProxOutput != null)
            {
                this.CurProxPointer = ProxOutput.FilePointer;
            }
        }

        public override void ResetSkip()
        {
            base.ResetSkip();
            Arrays.Fill(LastSkipDoc, 0);
            Arrays.Fill(LastSkipPayloadLength, -1); // we don't have to write the first length in the skip list
            Arrays.Fill(LastSkipFreqPointer, FreqOutput.FilePointer);
            if (ProxOutput != null)
            {
                Arrays.Fill(LastSkipProxPointer, ProxOutput.FilePointer);
            }
        }

        protected internal override void WriteSkipData(int level, IndexOutput skipBuffer)
        {
            // To efficiently store payloads in the posting lists we do not store the length of
            // every payload. Instead we omit the length for a payload if the previous payload had
            // the same length.
            // However, in order to support skipping the payload length at every skip point must be known.
            // So we use the same length encoding that we use for the posting lists for the skip data as well:
            // Case 1: current field does not store payloads
            //           SkipDatum                 --> DocSkip, FreqSkip, ProxSkip
            //           DocSkip,FreqSkip,ProxSkip --> VInt
            //           DocSkip records the document number before every SkipInterval th  document in TermFreqs.
            //           Document numbers are represented as differences from the previous value in the sequence.
            // Case 2: current field stores payloads
            //           SkipDatum                 --> DocSkip, PayloadLength?, FreqSkip,ProxSkip
            //           DocSkip,FreqSkip,ProxSkip --> VInt
            //           PayloadLength             --> VInt
            //         In this case DocSkip/2 is the difference between
            //         the current and the previous value. If DocSkip
            //         is odd, then a PayloadLength encoded as VInt follows,
            //         if DocSkip is even, then it is assumed that the
            //         current payload length equals the length at the previous
            //         skip point
            if (CurStorePayloads)
            {
                int delta = CurDoc - LastSkipDoc[level];
                if (CurPayloadLength == LastSkipPayloadLength[level])
                {
                    // the current payload length equals the length at the previous skip point,
                    // so we don't store the length again
                    skipBuffer.WriteVInt(delta * 2);
                }
                else
                {
                    // the payload length is different from the previous one. We shift the DocSkip,
                    // set the lowest bit and store the current payload length as VInt.
                    skipBuffer.WriteVInt(delta * 2 + 1);
                    skipBuffer.WriteVInt(CurPayloadLength);
                    LastSkipPayloadLength[level] = CurPayloadLength;
                }
            }
            else
            {
                // current field does not store payloads
                skipBuffer.WriteVInt(CurDoc - LastSkipDoc[level]);
            }

            skipBuffer.WriteVInt((int)(CurFreqPointer - LastSkipFreqPointer[level]));
            skipBuffer.WriteVInt((int)(CurProxPointer - LastSkipProxPointer[level]));

            LastSkipDoc[level] = CurDoc;

            LastSkipFreqPointer[level] = CurFreqPointer;
            LastSkipProxPointer[level] = CurProxPointer;
        }
    }
}