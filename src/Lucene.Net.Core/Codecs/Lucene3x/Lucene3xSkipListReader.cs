using System;

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

    using IndexInput = Lucene.Net.Store.IndexInput;

    /// @deprecated (4.0) this is only used to read indexes created
    /// before 4.0.
    [Obsolete("(4.0) this is only used to read indexes created")]
    internal sealed class Lucene3xSkipListReader : MultiLevelSkipListReader
    {
        private bool CurrentFieldStoresPayloads;
        private long[] FreqPointer_Renamed;
        private long[] ProxPointer_Renamed;
        private int[] PayloadLength_Renamed;

        private long LastFreqPointer;
        private long LastProxPointer;
        private int LastPayloadLength;

        public Lucene3xSkipListReader(IndexInput skipStream, int maxSkipLevels, int skipInterval)
            : base(skipStream, maxSkipLevels, skipInterval)
        {
            FreqPointer_Renamed = new long[maxSkipLevels];
            ProxPointer_Renamed = new long[maxSkipLevels];
            PayloadLength_Renamed = new int[maxSkipLevels];
        }

        public void Init(long skipPointer, long freqBasePointer, long proxBasePointer, int df, bool storesPayloads)
        {
            base.Init(skipPointer, df);
            this.CurrentFieldStoresPayloads = storesPayloads;
            LastFreqPointer = freqBasePointer;
            LastProxPointer = proxBasePointer;

            CollectionsHelper.Fill(FreqPointer_Renamed, freqBasePointer);
            CollectionsHelper.Fill(ProxPointer_Renamed, proxBasePointer);
            CollectionsHelper.Fill(PayloadLength_Renamed, 0);
        }

        /// <summary>
        /// Returns the freq pointer of the doc to which the last call of
        /// <seealso cref="MultiLevelSkipListReader#skipTo(int)"/> has skipped.
        /// </summary>
        public long FreqPointer
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
        public long ProxPointer
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
        public int PayloadLength
        {
            get
            {
                return LastPayloadLength;
            }
        }

        protected override void SeekChild(int level)
        {
            base.SeekChild(level);
            FreqPointer_Renamed[level] = LastFreqPointer;
            ProxPointer_Renamed[level] = LastProxPointer;
            PayloadLength_Renamed[level] = LastPayloadLength;
        }

        protected override int LastSkipData
        {
            set
            {
                base.LastSkipData = value;
                LastFreqPointer = FreqPointer_Renamed[value];
                LastProxPointer = ProxPointer_Renamed[value];
                LastPayloadLength = PayloadLength_Renamed[value];
            }
        }

        protected override int ReadSkipData(int level, IndexInput skipStream)
        {
            int delta;
            if (CurrentFieldStoresPayloads)
            {
                // the current field stores payloads.
                // if the doc delta is odd then we have
                // to read the current payload length
                // because it differs from the length of the
                // previous payload
                delta = skipStream.ReadVInt();
                if ((delta & 1) != 0)
                {
                    PayloadLength_Renamed[level] = skipStream.ReadVInt();
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