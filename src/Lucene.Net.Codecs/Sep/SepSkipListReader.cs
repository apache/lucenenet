using J2N.Numerics;
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

    /// <summary>
    /// Implements the skip list reader for the default posting list format
    /// that stores positions and payloads.
    /// <para/>
    /// @lucene.experimental
    /// </summary>

    // TODO: rewrite this as recursive classes?
    internal class SepSkipListReader : MultiLevelSkipListReader
    {
        private bool currentFieldStoresPayloads;
        private readonly Int32IndexInput.Index[] freqIndex; // LUCENENET: marked readonly
        private readonly Int32IndexInput.Index[] docIndex; // LUCENENET: marked readonly
        private readonly Int32IndexInput.Index[] posIndex; // LUCENENET: marked readonly
        private readonly long[] payloadPointer; // LUCENENET: marked readonly
        private readonly int[] payloadLength; // LUCENENET: marked readonly

        private readonly Int32IndexInput.Index lastFreqIndex;
        private readonly Int32IndexInput.Index lastDocIndex;
        // TODO: -- make private again
        internal readonly Int32IndexInput.Index lastPosIndex;

        private long lastPayloadPointer;
        private int lastPayloadLength;

        /// <exception cref="IOException"/>
        internal SepSkipListReader(IndexInput skipStream,
                          Int32IndexInput freqIn,
                          Int32IndexInput docIn,
                          Int32IndexInput posIn,
                          int maxSkipLevels,
                          int skipInterval)
            : base(skipStream, maxSkipLevels, skipInterval)
        {
            if (freqIn != null)
            {
                freqIndex = new Int32IndexInput.Index[maxSkipLevels];
            }
            docIndex = new Int32IndexInput.Index[maxSkipLevels];
            if (posIn != null)
            {
                posIndex = new Int32IndexInput.Index[m_maxNumberOfSkipLevels];
            }
            for (int i = 0; i < maxSkipLevels; i++)
            {
                if (freqIn != null)
                {
                    freqIndex[i] = freqIn.GetIndex();
                }
                docIndex[i] = docIn.GetIndex();
                if (posIn != null)
                {
                    posIndex[i] = posIn.GetIndex();
                }
            }
            payloadPointer = new long[maxSkipLevels];
            payloadLength = new int[maxSkipLevels];

            if (freqIn != null)
            {
                lastFreqIndex = freqIn.GetIndex();
            }
            else
            {
                lastFreqIndex = null;
            }
            lastDocIndex = docIn.GetIndex();
            if (posIn != null)
            {
                lastPosIndex = posIn.GetIndex();
            }
            else
            {
                lastPosIndex = null;
            }
        }

        internal IndexOptions indexOptions;

        internal void SetIndexOptions(IndexOptions v)
        {
            indexOptions = v;
        }

        internal void Init(long skipPointer,
                  Int32IndexInput.Index docBaseIndex,
                  Int32IndexInput.Index freqBaseIndex,
                  Int32IndexInput.Index posBaseIndex,
                  long payloadBasePointer,
                  int df,
                  bool storesPayloads)
        {

            base.Init(skipPointer, df);
            this.currentFieldStoresPayloads = storesPayloads;

            lastPayloadPointer = payloadBasePointer;

            for (int i = 0; i < m_maxNumberOfSkipLevels; i++)
            {
                docIndex[i].CopyFrom(docBaseIndex);
                if (freqIndex != null)
                {
                    freqIndex[i].CopyFrom(freqBaseIndex);
                }
                if (posBaseIndex != null)
                {
                    posIndex[i].CopyFrom(posBaseIndex);
                }
            }
            Arrays.Fill(payloadPointer, payloadBasePointer);
            Arrays.Fill(payloadLength, 0);
        }

        internal long PayloadPointer => lastPayloadPointer;

        /// <summary>
        /// Returns the payload length of the payload stored just before 
        /// the doc to which the last call of <see cref="MultiLevelSkipListReader.SkipTo(int)"/> 
        /// has skipped.
        /// </summary>
        internal int PayloadLength => lastPayloadLength;

        /// <exception cref="IOException"/>
        protected override void SeekChild(int level)
        {
            base.SeekChild(level);
            payloadPointer[level] = lastPayloadPointer;
            payloadLength[level] = lastPayloadLength;
        }

        protected override void SetLastSkipData(int level)
        {
            base.SetLastSkipData(level);

            lastPayloadPointer = payloadPointer[level];
            lastPayloadLength = payloadLength[level];
            if (freqIndex != null)
            {
                lastFreqIndex.CopyFrom(freqIndex[level]);
            }
            lastDocIndex.CopyFrom(docIndex[level]);
            if (lastPosIndex != null)
            {
                lastPosIndex.CopyFrom(posIndex[level]);
            }

            if (level > 0)
            {
                if (freqIndex != null)
                {
                    freqIndex[level - 1].CopyFrom(freqIndex[level]);
                }
                docIndex[level - 1].CopyFrom(docIndex[level]);
                if (posIndex != null)
                {
                    posIndex[level - 1].CopyFrom(posIndex[level]);
                }
            }
        }

        internal Int32IndexInput.Index FreqIndex => lastFreqIndex;

        internal Int32IndexInput.Index PosIndex => lastPosIndex;

        internal Int32IndexInput.Index DocIndex => lastDocIndex;

        /// <exception cref="IOException"/>
        protected override int ReadSkipData(int level, IndexInput skipStream)
        {
            int delta;
            if (Debugging.AssertsEnabled) Debugging.Assert(indexOptions == IndexOptions.DOCS_AND_FREQS_AND_POSITIONS || !currentFieldStoresPayloads);
            if (currentFieldStoresPayloads)
            {
                // the current field stores payloads.
                // if the doc delta is odd then we have
                // to read the current payload length
                // because it differs from the length of the
                // previous payload
                delta = skipStream.ReadVInt32();
                if ((delta & 1) != 0)
                {
                    payloadLength[level] = skipStream.ReadVInt32();
                }
                //delta >>>= 1;
                delta = delta.TripleShift(1);
            }
            else
            {
                delta = skipStream.ReadVInt32();
            }
            if (indexOptions != IndexOptions.DOCS_ONLY)
            {
                freqIndex[level].Read(skipStream, false);
            }
            docIndex[level].Read(skipStream, false);
            if (indexOptions == IndexOptions.DOCS_AND_FREQS_AND_POSITIONS)
            {
                posIndex[level].Read(skipStream, false);
                if (currentFieldStoresPayloads)
                {
                    payloadPointer[level] += skipStream.ReadVInt32();
                }
            }

            return delta;
        }
    }
}