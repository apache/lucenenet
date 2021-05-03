using Lucene.Net.Diagnostics;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Support;
using System.Diagnostics;
using System.IO;

namespace Lucene.Net.Codecs.Sep
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

    // TODO: -- skip data should somehow be more local to the particular stream 
    // (doc, freq, pos, payload)

    /// <summary>
    /// Implements the skip list writer for the default posting list format
    /// that stores positions and payloads.
    /// <para/>
    /// @lucene.experimental
    /// </summary>
    internal class SepSkipListWriter : MultiLevelSkipListWriter
    {
        private readonly int[] lastSkipDoc; // LUCENENET: marked readonly
        private readonly int[] lastSkipPayloadLength; // LUCENENET: marked readonly
        private readonly long[] lastSkipPayloadPointer; // LUCENENET: marked readonly

        private readonly Int32IndexOutput.Index[] docIndex; // LUCENENET: marked readonly
        private readonly Int32IndexOutput.Index[] freqIndex; // LUCENENET: marked readonly
        private readonly Int32IndexOutput.Index[] posIndex; // LUCENENET: marked readonly

        private readonly Int32IndexOutput freqOutput; // LUCENENET: marked readonly
        // TODO: -- private again
        internal Int32IndexOutput posOutput;
        // TODO: -- private again
        internal IndexOutput payloadOutput;

        private int curDoc;
        private bool curStorePayloads;
        private int curPayloadLength;
        private long curPayloadPointer;

        /// <exception cref="IOException"/>
        internal SepSkipListWriter(int skipInterval, int numberOfSkipLevels, int docCount,
                          Int32IndexOutput freqOutput,
                          Int32IndexOutput docOutput,
                          Int32IndexOutput posOutput,
                          IndexOutput payloadOutput)
            : base(skipInterval, numberOfSkipLevels, docCount)
        {

            this.freqOutput = freqOutput;
            this.posOutput = posOutput;
            this.payloadOutput = payloadOutput;

            lastSkipDoc = new int[numberOfSkipLevels];
            lastSkipPayloadLength = new int[numberOfSkipLevels];
            // TODO: -- also cutover normal IndexOutput to use getIndex()?
            lastSkipPayloadPointer = new long[numberOfSkipLevels];

            freqIndex = new Int32IndexOutput.Index[numberOfSkipLevels];
            docIndex = new Int32IndexOutput.Index[numberOfSkipLevels];
            posIndex = new Int32IndexOutput.Index[numberOfSkipLevels];

            for (int i = 0; i < numberOfSkipLevels; i++)
            {
                if (freqOutput != null)
                {
                    freqIndex[i] = freqOutput.GetIndex();
                }
                docIndex[i] = docOutput.GetIndex();
                if (posOutput != null)
                {
                    posIndex[i] = posOutput.GetIndex();
                }
            }
        }

        private IndexOptions indexOptions;

        internal void SetIndexOptions(IndexOptions v)
        {
            indexOptions = v;
        }

        /// <exception cref="IOException"/>
        internal void SetPosOutput(Int32IndexOutput posOutput)
        {
            this.posOutput = posOutput;
            for (int i = 0; i < m_numberOfSkipLevels; i++)
            {
                posIndex[i] = posOutput.GetIndex();
            }
        }

        internal void SetPayloadOutput(IndexOutput payloadOutput)
        {
            this.payloadOutput = payloadOutput;
        }

        /// <summary>
        /// Sets the values for the current skip data. 
        /// </summary>
        // Called @ every index interval (every 128th (by default)
        // doc)
        internal void SetSkipData(int doc, bool storePayloads, int payloadLength)
        {
            this.curDoc = doc;
            this.curStorePayloads = storePayloads;
            this.curPayloadLength = payloadLength;
            if (payloadOutput != null)
            {
                this.curPayloadPointer = payloadOutput.Position; // LUCENENET specific: Renamed from getFilePointer() to match FileStream
            }
        }

        // Called @ start of new term
        /// <exception cref="IOException"/>
        protected internal void ResetSkip(Int32IndexOutput.Index topDocIndex, Int32IndexOutput.Index topFreqIndex, Int32IndexOutput.Index topPosIndex)
        {
            base.ResetSkip();

            Arrays.Fill(lastSkipDoc, 0);
            Arrays.Fill(lastSkipPayloadLength, -1);  // we don't have to write the first length in the skip list
            for (int i = 0; i < m_numberOfSkipLevels; i++)
            {
                docIndex[i].CopyFrom(topDocIndex, true);
                if (freqOutput != null)
                {
                    freqIndex[i].CopyFrom(topFreqIndex, true);
                }
                if (posOutput != null)
                {
                    posIndex[i].CopyFrom(topPosIndex, true);
                }
            }
            if (payloadOutput != null)
            {
                Arrays.Fill(lastSkipPayloadPointer, payloadOutput.Position); // LUCENENET specific: Renamed from getFilePointer() to match FileStream
            }
        }

        /// <exception cref="IOException"/>
        protected override void WriteSkipData(int level, IndexOutput skipBuffer)
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

            if (Debugging.AssertsEnabled) Debugging.Assert(indexOptions == IndexOptions.DOCS_AND_FREQS_AND_POSITIONS || !curStorePayloads);

            if (curStorePayloads)
            {
                int delta = curDoc - lastSkipDoc[level];
                if (curPayloadLength == lastSkipPayloadLength[level])
                {
                    // the current payload length equals the length at the previous skip point,
                    // so we don't store the length again
                    skipBuffer.WriteVInt32(delta << 1);
                }
                else
                {
                    // the payload length is different from the previous one. We shift the DocSkip, 
                    // set the lowest bit and store the current payload length as VInt.
                    skipBuffer.WriteVInt32(delta << 1 | 1);
                    skipBuffer.WriteVInt32(curPayloadLength);
                    lastSkipPayloadLength[level] = curPayloadLength;
                }
            }
            else
            {
                // current field does not store payloads
                skipBuffer.WriteVInt32(curDoc - lastSkipDoc[level]);
            }

            if (indexOptions != IndexOptions.DOCS_ONLY)
            {
                freqIndex[level].Mark();
                freqIndex[level].Write(skipBuffer, false);
            }
            docIndex[level].Mark();
            docIndex[level].Write(skipBuffer, false);
            if (indexOptions == IndexOptions.DOCS_AND_FREQS_AND_POSITIONS)
            {
                posIndex[level].Mark();
                posIndex[level].Write(skipBuffer, false);
                if (curStorePayloads)
                {
                    skipBuffer.WriteVInt32((int)(curPayloadPointer - lastSkipPayloadPointer[level]));
                }
            }

            lastSkipDoc[level] = curDoc;
            lastSkipPayloadPointer[level] = curPayloadPointer;
        }
    }
}