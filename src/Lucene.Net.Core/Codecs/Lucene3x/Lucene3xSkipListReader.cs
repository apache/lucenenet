using Lucene.Net.Support;
using System;

namespace Lucene.Net.Codecs.Lucene3x
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

    /// @deprecated (4.0) this is only used to read indexes created
    /// before 4.0.
    [Obsolete("(4.0) this is only used to read indexes created")]
    internal sealed class Lucene3xSkipListReader : MultiLevelSkipListReader
    {
        private bool currentFieldStoresPayloads;
        private long[] freqPointer;
        private long[] proxPointer;
        private int[] payloadLength;

        private long lastFreqPointer;
        private long lastProxPointer;
        private int lastPayloadLength;

        public Lucene3xSkipListReader(IndexInput skipStream, int maxSkipLevels, int skipInterval)
            : base(skipStream, maxSkipLevels, skipInterval)
        {
            freqPointer = new long[maxSkipLevels];
            proxPointer = new long[maxSkipLevels];
            payloadLength = new int[maxSkipLevels];
        }

        public void Init(long skipPointer, long freqBasePointer, long proxBasePointer, int df, bool storesPayloads)
        {
            base.Init(skipPointer, df);
            this.currentFieldStoresPayloads = storesPayloads;
            lastFreqPointer = freqBasePointer;
            lastProxPointer = proxBasePointer;

            CollectionsHelper.Fill(freqPointer, freqBasePointer);
            CollectionsHelper.Fill(proxPointer, proxBasePointer);
            CollectionsHelper.Fill(payloadLength, 0);
        }

        /// <summary>
        /// Returns the freq pointer of the doc to which the last call of
        /// <seealso cref="MultiLevelSkipListReader#skipTo(int)"/> has skipped.
        /// </summary>
        public long FreqPointer
        {
            get
            {
                return lastFreqPointer;
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
                return lastProxPointer;
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
                return lastPayloadLength;
            }
        }

        protected override void SeekChild(int level)
        {
            base.SeekChild(level);
            freqPointer[level] = lastFreqPointer;
            proxPointer[level] = lastProxPointer;
            payloadLength[level] = lastPayloadLength;
        }

        protected override void SetLastSkipData(int level)
        {
            base.SetLastSkipData(level);
            lastFreqPointer = freqPointer[level];
            lastProxPointer = proxPointer[level];
            lastPayloadLength = payloadLength[level];
        }

        protected override int ReadSkipData(int level, IndexInput skipStream)
        {
            int delta;
            if (currentFieldStoresPayloads)
            {
                // the current field stores payloads.
                // if the doc delta is odd then we have
                // to read the current payload length
                // because it differs from the length of the
                // previous payload
                delta = skipStream.ReadVInt();
                if ((delta & 1) != 0)
                {
                    payloadLength[level] = skipStream.ReadVInt();
                }
                delta = (int)((uint)delta >> 1);
            }
            else
            {
                delta = skipStream.ReadVInt();
            }

            freqPointer[level] += skipStream.ReadVInt();
            proxPointer[level] += skipStream.ReadVInt();

            return delta;
        }
    }
}