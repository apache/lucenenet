using Lucene.Net.Diagnostics;
using Lucene.Net.Store;
using Lucene.Net.Support;
using System;

namespace Lucene.Net.Codecs.Lucene40
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
    /// Implements the skip list writer for the 4.0 posting list format
    /// that stores positions and payloads.
    /// </summary>
    /// <seealso cref="Lucene40PostingsFormat"/>
    [Obsolete("Only for reading old 4.0 segments")]
    public class Lucene40SkipListWriter : MultiLevelSkipListWriter
    {
        private readonly int[] lastSkipDoc;
        private readonly int[] lastSkipPayloadLength;
        private readonly int[] lastSkipOffsetLength;
        private readonly long[] lastSkipFreqPointer;
        private readonly long[] lastSkipProxPointer;

        private readonly IndexOutput freqOutput;
        private readonly IndexOutput proxOutput;

        private int curDoc;
        private bool curStorePayloads;
        private bool curStoreOffsets;
        private int curPayloadLength;
        private int curOffsetLength;
        private long curFreqPointer;
        private long curProxPointer;

        /// <summary>
        /// Sole constructor. </summary>
        public Lucene40SkipListWriter(int skipInterval, int numberOfSkipLevels, int docCount, IndexOutput freqOutput, IndexOutput proxOutput)
            : base(skipInterval, numberOfSkipLevels, docCount)
        {
            this.freqOutput = freqOutput;
            this.proxOutput = proxOutput;

            lastSkipDoc = new int[numberOfSkipLevels];
            lastSkipPayloadLength = new int[numberOfSkipLevels];
            lastSkipOffsetLength = new int[numberOfSkipLevels];
            lastSkipFreqPointer = new long[numberOfSkipLevels];
            lastSkipProxPointer = new long[numberOfSkipLevels];
        }

        /// <summary>
        /// Sets the values for the current skip data.
        /// </summary>
        public virtual void SetSkipData(int doc, bool storePayloads, int payloadLength, bool storeOffsets, int offsetLength)
        {
            if (Debugging.AssertsEnabled) Debugging.Assert(storePayloads || payloadLength == -1);
            if (Debugging.AssertsEnabled) Debugging.Assert(storeOffsets || offsetLength == -1);
            this.curDoc = doc;
            this.curStorePayloads = storePayloads;
            this.curPayloadLength = payloadLength;
            this.curStoreOffsets = storeOffsets;
            this.curOffsetLength = offsetLength;
            this.curFreqPointer = freqOutput.Position; // LUCENENET specific: Renamed from getFilePointer() to match FileStream
            if (proxOutput != null)
            {
                this.curProxPointer = proxOutput.Position; // LUCENENET specific: Renamed from getFilePointer() to match FileStream
            }
        }

        public override void ResetSkip()
        {
            base.ResetSkip();
            Arrays.Fill(lastSkipDoc, 0);
            Arrays.Fill(lastSkipPayloadLength, -1); // we don't have to write the first length in the skip list
            Arrays.Fill(lastSkipOffsetLength, -1); // we don't have to write the first length in the skip list
            Arrays.Fill(lastSkipFreqPointer, freqOutput.Position); // LUCENENET specific: Renamed from getFilePointer() to match FileStream
            if (proxOutput != null)
            {
                Arrays.Fill(lastSkipProxPointer, proxOutput.Position); // LUCENENET specific: Renamed from getFilePointer() to match FileStream
            }
        }

        protected override void WriteSkipData(int level, IndexOutput skipBuffer)
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
            int delta = curDoc - lastSkipDoc[level];

            if (curStorePayloads || curStoreOffsets)
            {
                if (Debugging.AssertsEnabled) Debugging.Assert(curStorePayloads || curPayloadLength == lastSkipPayloadLength[level]);
                if (Debugging.AssertsEnabled) Debugging.Assert(curStoreOffsets || curOffsetLength == lastSkipOffsetLength[level]);

                if (curPayloadLength == lastSkipPayloadLength[level] && curOffsetLength == lastSkipOffsetLength[level])
                {
                    // the current payload/offset lengths equals the lengths at the previous skip point,
                    // so we don't store the lengths again
                    skipBuffer.WriteVInt32(delta << 1);
                }
                else
                {
                    // the payload and/or offset length is different from the previous one. We shift the DocSkip,
                    // set the lowest bit and store the current payload and/or offset lengths as VInts.
                    skipBuffer.WriteVInt32(delta << 1 | 1);

                    if (curStorePayloads)
                    {
                        skipBuffer.WriteVInt32(curPayloadLength);
                        lastSkipPayloadLength[level] = curPayloadLength;
                    }
                    if (curStoreOffsets)
                    {
                        skipBuffer.WriteVInt32(curOffsetLength);
                        lastSkipOffsetLength[level] = curOffsetLength;
                    }
                }
            }
            else
            {
                // current field does not store payloads or offsets
                skipBuffer.WriteVInt32(delta);
            }

            skipBuffer.WriteVInt32((int)(curFreqPointer - lastSkipFreqPointer[level]));
            skipBuffer.WriteVInt32((int)(curProxPointer - lastSkipProxPointer[level]));

            lastSkipDoc[level] = curDoc;

            lastSkipFreqPointer[level] = curFreqPointer;
            lastSkipProxPointer[level] = curProxPointer;
        }
    }
}