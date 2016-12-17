using System;

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

    using IndexInput = Lucene.Net.Store.IndexInput;

    /// <summary>
    /// Implements the skip list reader for the 4.0 posting list format
    /// that stores positions and payloads.
    /// </summary>
    /// <seealso cref= Lucene40PostingsFormat </seealso>
    /// @deprecated Only for reading old 4.0 segments
    [Obsolete("Only for reading old 4.0 segments")]
    public class Lucene40SkipListReader : MultiLevelSkipListReader
    {
        private bool CurrentFieldStoresPayloads;
        private bool CurrentFieldStoresOffsets;
        private long[] FreqPointer_Renamed;
        private long[] ProxPointer_Renamed;
        private int[] PayloadLength_Renamed;
        private int[] OffsetLength_Renamed;

        private long LastFreqPointer;
        private long LastProxPointer;
        private int LastPayloadLength;
        private int LastOffsetLength;

        /// <summary>
        /// Sole constructor. </summary>
        public Lucene40SkipListReader(IndexInput skipStream, int maxSkipLevels, int skipInterval)
            : base(skipStream, maxSkipLevels, skipInterval)
        {
            FreqPointer_Renamed = new long[maxSkipLevels];
            ProxPointer_Renamed = new long[maxSkipLevels];
            PayloadLength_Renamed = new int[maxSkipLevels];
            OffsetLength_Renamed = new int[maxSkipLevels];
        }

        /// <summary>
        /// Per-term initialization. </summary>
        public virtual void Init(long skipPointer, long freqBasePointer, long proxBasePointer, int df, bool storesPayloads, bool storesOffsets)
        {
            base.Init(skipPointer, df);
            this.CurrentFieldStoresPayloads = storesPayloads;
            this.CurrentFieldStoresOffsets = storesOffsets;
            LastFreqPointer = freqBasePointer;
            LastProxPointer = proxBasePointer;

            CollectionsHelper.Fill(FreqPointer_Renamed, freqBasePointer);
            CollectionsHelper.Fill(ProxPointer_Renamed, proxBasePointer);
            CollectionsHelper.Fill(PayloadLength_Renamed, 0);
            CollectionsHelper.Fill(OffsetLength_Renamed, 0);
        }

        /// <summary>
        /// Returns the freq pointer of the doc to which the last call of
        /// <seealso cref="MultiLevelSkipListReader#skipTo(int)"/> has skipped.
        /// </summary>
        public virtual long FreqPointer
        {
            get
            {
                return LastFreqPointer;
            }
        }

        /// <summary>
        /// Returns the prox pointer of the doc to which the last call of
        /// <seealso cref="MultiLevelSkipListReader#skipTo(int)"/> has skipped.
        /// </summary>
        public virtual long ProxPointer
        {
            get
            {
                return LastProxPointer;
            }
        }

        /// <summary>
        /// Returns the payload length of the payload stored just before
        /// the doc to which the last call of <seealso cref="MultiLevelSkipListReader#skipTo(int)"/>
        /// has skipped.
        /// </summary>
        public virtual int PayloadLength
        {
            get
            {
                return LastPayloadLength;
            }
        }

        /// <summary>
        /// Returns the offset length (endOffset-startOffset) of the position stored just before
        /// the doc to which the last call of <seealso cref="MultiLevelSkipListReader#skipTo(int)"/>
        /// has skipped.
        /// </summary>
        public virtual int OffsetLength
        {
            get
            {
                return LastOffsetLength;
            }
        }

        protected override void SeekChild(int level)
        {
            base.SeekChild(level);
            FreqPointer_Renamed[level] = LastFreqPointer;
            ProxPointer_Renamed[level] = LastProxPointer;
            PayloadLength_Renamed[level] = LastPayloadLength;
            OffsetLength_Renamed[level] = LastOffsetLength;
        }

        protected override void SetLastSkipData(int level)
        {
            base.SetLastSkipData(level);
            LastFreqPointer = FreqPointer_Renamed[level];
            LastProxPointer = ProxPointer_Renamed[level];
            LastPayloadLength = PayloadLength_Renamed[level];
            LastOffsetLength = OffsetLength_Renamed[level];
        }

        protected override int ReadSkipData(int level, IndexInput skipStream)
        {
            int delta;
            if (CurrentFieldStoresPayloads || CurrentFieldStoresOffsets)
            {
                // the current field stores payloads and/or offsets.
                // if the doc delta is odd then we have
                // to read the current payload/offset lengths
                // because it differs from the lengths of the
                // previous payload/offset
                delta = skipStream.ReadVInt();
                if ((delta & 1) != 0)
                {
                    if (CurrentFieldStoresPayloads)
                    {
                        PayloadLength_Renamed[level] = skipStream.ReadVInt();
                    }
                    if (CurrentFieldStoresOffsets)
                    {
                        OffsetLength_Renamed[level] = skipStream.ReadVInt();
                    }
                }
                delta = (int)((uint)delta >> 1);
            }
            else
            {
                delta = skipStream.ReadVInt();
            }

            FreqPointer_Renamed[level] += skipStream.ReadVInt();
            ProxPointer_Renamed[level] += skipStream.ReadVInt();

            return delta;
        }
    }
}