using System;
using System.Diagnostics;

namespace Lucene.Net.Codecs.Lucene40
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
    /// Implements the skip list writer for the 4.0 posting list format
    /// that stores positions and payloads.
    /// </summary>
    /// <seealso> cref= Lucene40PostingsFormat </seealso>
    /// @deprecated Only for reading old 4.0 segments
    [Obsolete("Only for reading old 4.0 segments")]
    public class Lucene40SkipListWriter : MultiLevelSkipListWriter
    {
        private int[] LastSkipDoc;
        private int[] LastSkipPayloadLength;
        private int[] LastSkipOffsetLength;
        private long[] LastSkipFreqPointer;
        private long[] LastSkipProxPointer;

        private IndexOutput FreqOutput;
        private IndexOutput ProxOutput;

        private int CurDoc;
        private bool CurStorePayloads;
        private bool CurStoreOffsets;
        private int CurPayloadLength;
        private int CurOffsetLength;
        private long CurFreqPointer;
        private long CurProxPointer;

        /// <summary>
        /// Sole constructor. </summary>
        public Lucene40SkipListWriter(int skipInterval, int numberOfSkipLevels, int docCount, IndexOutput freqOutput, IndexOutput proxOutput)
            : base(skipInterval, numberOfSkipLevels, docCount)
        {
            this.FreqOutput = freqOutput;
            this.ProxOutput = proxOutput;

            LastSkipDoc = new int[numberOfSkipLevels];
            LastSkipPayloadLength = new int[numberOfSkipLevels];
            LastSkipOffsetLength = new int[numberOfSkipLevels];
            LastSkipFreqPointer = new long[numberOfSkipLevels];
            LastSkipProxPointer = new long[numberOfSkipLevels];
        }

        /// <summary>
        /// Sets the values for the current skip data.
        /// </summary>
        public virtual void SetSkipData(int doc, bool storePayloads, int payloadLength, bool storeOffsets, int offsetLength)
        {
            Debug.Assert(storePayloads || payloadLength == -1);
            Debug.Assert(storeOffsets || offsetLength == -1);
            this.CurDoc = doc;
            this.CurStorePayloads = storePayloads;
            this.CurPayloadLength = payloadLength;
            this.CurStoreOffsets = storeOffsets;
            this.CurOffsetLength = offsetLength;
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
            Arrays.Fill(LastSkipOffsetLength, -1); // we don't have to write the first length in the skip list
            Arrays.Fill(LastSkipFreqPointer, FreqOutput.FilePointer);
            if (ProxOutput != null)
            {
                Arrays.Fill(LastSkipProxPointer, ProxOutput.FilePointer);
            }
        }

        protected internal override void WriteSkipData(int level, IndexOutput skipBuffer)
        {
            // To efficiently store payloads/offsets in the posting lists we do not store the length of
            // every payload/offset. Instead we omit the length if the previous lengths were the same
            //
            // However, in order to support skipping, the length at every skip point must be known.
            // So we use the same length encoding that we use for the posting lists for the skip data as well:
            // Case 1: current field does not store payloads/offsets
            //           SkipDatum                 --> DocSkip, FreqSkip, ProxSkip
            //           DocSkip,FreqSkip,ProxSkip --> VInt
            //           DocSkip records the document number before every SkipInterval th  document in TermFreqs.
            //           Document numbers are represented as differences from the previous value in the sequence.
            // Case 2: current field stores payloads/offsets
            //           SkipDatum                 --> DocSkip, PayloadLength?,OffsetLength?,FreqSkip,ProxSkip
            //           DocSkip,FreqSkip,ProxSkip --> VInt
            //           PayloadLength,OffsetLength--> VInt
            //         In this case DocSkip/2 is the difference between
            //         the current and the previous value. If DocSkip
            //         is odd, then a PayloadLength encoded as VInt follows,
            //         if DocSkip is even, then it is assumed that the
            //         current payload/offset lengths equals the lengths at the previous
            //         skip point
            int delta = CurDoc - LastSkipDoc[level];

            if (CurStorePayloads || CurStoreOffsets)
            {
                Debug.Assert(CurStorePayloads || CurPayloadLength == LastSkipPayloadLength[level]);
                Debug.Assert(CurStoreOffsets || CurOffsetLength == LastSkipOffsetLength[level]);

                if (CurPayloadLength == LastSkipPayloadLength[level] && CurOffsetLength == LastSkipOffsetLength[level])
                {
                    // the current payload/offset lengths equals the lengths at the previous skip point,
                    // so we don't store the lengths again
                    skipBuffer.WriteVInt(delta << 1);
                }
                else
                {
                    // the payload and/or offset length is different from the previous one. We shift the DocSkip,
                    // set the lowest bit and store the current payload and/or offset lengths as VInts.
                    skipBuffer.WriteVInt(delta << 1 | 1);

                    if (CurStorePayloads)
                    {
                        skipBuffer.WriteVInt(CurPayloadLength);
                        LastSkipPayloadLength[level] = CurPayloadLength;
                    }
                    if (CurStoreOffsets)
                    {
                        skipBuffer.WriteVInt(CurOffsetLength);
                        LastSkipOffsetLength[level] = CurOffsetLength;
                    }
                }
            }
            else
            {
                // current field does not store payloads or offsets
                skipBuffer.WriteVInt(delta);
            }

            skipBuffer.WriteVInt((int)(CurFreqPointer - LastSkipFreqPointer[level]));
            skipBuffer.WriteVInt((int)(CurProxPointer - LastSkipProxPointer[level]));

            LastSkipDoc[level] = CurDoc;

            LastSkipFreqPointer[level] = CurFreqPointer;
            LastSkipProxPointer[level] = CurProxPointer;
        }
    }
}