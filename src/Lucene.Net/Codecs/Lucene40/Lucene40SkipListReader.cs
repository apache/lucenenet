using J2N.Numerics;
using Lucene.Net.Support;
using System;
using System.Runtime.CompilerServices;

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

    using IndexInput = Lucene.Net.Store.IndexInput;

    /// <summary>
    /// Implements the skip list reader for the 4.0 posting list format
    /// that stores positions and payloads.
    /// </summary>
    /// <seealso cref="Lucene40PostingsFormat"/>
    [Obsolete("Only for reading old 4.0 segments")]
    public class Lucene40SkipListReader : MultiLevelSkipListReader
    {
        private bool currentFieldStoresPayloads;
        private bool currentFieldStoresOffsets;
        private readonly long[] freqPointer; // LUCENENET: marked readonly
        private readonly long[] proxPointer; // LUCENENET: marked readonly
        private readonly int[] payloadLength; // LUCENENET: marked readonly
        private readonly int[] offsetLength; // LUCENENET: marked readonly

        private long lastFreqPointer;
        private long lastProxPointer;
        private int lastPayloadLength;
        private int lastOffsetLength;

        /// <summary>
        /// Sole constructor. </summary>
        public Lucene40SkipListReader(IndexInput skipStream, int maxSkipLevels, int skipInterval)
            : base(skipStream, maxSkipLevels, skipInterval)
        {
            freqPointer = new long[maxSkipLevels];
            proxPointer = new long[maxSkipLevels];
            payloadLength = new int[maxSkipLevels];
            offsetLength = new int[maxSkipLevels];
        }

        /// <summary>
        /// Per-term initialization. </summary>
        public virtual void Init(long skipPointer, long freqBasePointer, long proxBasePointer, int df, bool storesPayloads, bool storesOffsets)
        {
            base.Init(skipPointer, df);
            this.currentFieldStoresPayloads = storesPayloads;
            this.currentFieldStoresOffsets = storesOffsets;
            lastFreqPointer = freqBasePointer;
            lastProxPointer = proxBasePointer;

            Arrays.Fill(freqPointer, freqBasePointer);
            Arrays.Fill(proxPointer, proxBasePointer);
            Arrays.Fill(payloadLength, 0);
            Arrays.Fill(offsetLength, 0);
        }

        /// <summary>
        /// Returns the freq pointer of the doc to which the last call of
        /// <see cref="MultiLevelSkipListReader.SkipTo(int)"/> has skipped.
        /// </summary>
        public virtual long FreqPointer => lastFreqPointer;

        /// <summary>
        /// Returns the prox pointer of the doc to which the last call of
        /// <see cref="MultiLevelSkipListReader.SkipTo(int)"/> has skipped.
        /// </summary>
        public virtual long ProxPointer => lastProxPointer;

        /// <summary>
        /// Returns the payload length of the payload stored just before
        /// the doc to which the last call of <see cref="MultiLevelSkipListReader.SkipTo(int)"/>
        /// has skipped.
        /// </summary>
        public virtual int PayloadLength => lastPayloadLength;

        /// <summary>
        /// Returns the offset length (endOffset-startOffset) of the position stored just before
        /// the doc to which the last call of <see cref="MultiLevelSkipListReader.SkipTo(int)"/>
        /// has skipped.
        /// </summary>
        public virtual int OffsetLength => lastOffsetLength;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override void SeekChild(int level)
        {
            base.SeekChild(level);
            freqPointer[level] = lastFreqPointer;
            proxPointer[level] = lastProxPointer;
            payloadLength[level] = lastPayloadLength;
            offsetLength[level] = lastOffsetLength;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override void SetLastSkipData(int level)
        {
            base.SetLastSkipData(level);
            lastFreqPointer = freqPointer[level];
            lastProxPointer = proxPointer[level];
            lastPayloadLength = payloadLength[level];
            lastOffsetLength = offsetLength[level];
        }

        protected override int ReadSkipData(int level, IndexInput skipStream)
        {
            int delta;
            if (currentFieldStoresPayloads || currentFieldStoresOffsets)
            {
                // the current field stores payloads and/or offsets.
                // if the doc delta is odd then we have
                // to read the current payload/offset lengths
                // because it differs from the lengths of the
                // previous payload/offset
                delta = skipStream.ReadVInt32();
                if ((delta & 1) != 0)
                {
                    if (currentFieldStoresPayloads)
                    {
                        payloadLength[level] = skipStream.ReadVInt32();
                    }
                    if (currentFieldStoresOffsets)
                    {
                        offsetLength[level] = skipStream.ReadVInt32();
                    }
                }
                delta = delta.TripleShift(1);
            }
            else
            {
                delta = skipStream.ReadVInt32();
            }

            freqPointer[level] += skipStream.ReadVInt32();
            proxPointer[level] += skipStream.ReadVInt32();

            return delta;
        }
    }
}