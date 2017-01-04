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

namespace Lucene.Net.Codecs.Sep
{
    using System.Diagnostics;
    using Index;
    using Store;
    using Support;

    /// <summary>
    /// Implements the skip list reader for the default posting list format
    /// that stores positions and payloads.
    /// 
    /// @lucene.experimental
    /// </summary>

    // TODO: rewrite this as recursive classes?
    internal class SepSkipListReader : MultiLevelSkipListReader
    {
        private bool _currentFieldStoresPayloads;
        private readonly IntIndexInputIndex[] _freqIndex;
        private readonly IntIndexInputIndex[] _docIndex;
        private readonly IntIndexInputIndex[] _posIndex;
        private readonly long[] _payloadPointer;
        private readonly int[] _payloadLength;

        private readonly IntIndexInputIndex _lastFreqIndex;
        private readonly IntIndexInputIndex _lastDocIndex;
        private readonly IntIndexInputIndex _lastPosIndex;

        private IndexOptions _indexOptions;

        private long _lastPayloadPointer;
        private int _lastPayloadLength;

        internal SepSkipListReader(IndexInput skipStream, IntIndexInput freqIn, IntIndexInput docIn, IntIndexInput posIn,
            int maxSkipLevels, int skipInterval)
            : base(skipStream, maxSkipLevels, skipInterval)
        {
            if (freqIn != null)
                _freqIndex = new IntIndexInputIndex[maxSkipLevels];

            _docIndex = new IntIndexInputIndex[maxSkipLevels];

            if (posIn != null)
                _posIndex = new IntIndexInputIndex[m_maxNumberOfSkipLevels];

            for (var i = 0; i < maxSkipLevels; i++)
            {
                if (freqIn != null)
                    _freqIndex[i] = freqIn.Index();

                _docIndex[i] = docIn.Index();

                if (posIn != null)
                    _posIndex[i] = posIn.Index();
            }

            _payloadPointer = new long[maxSkipLevels];
            _payloadLength = new int[maxSkipLevels];

            _lastFreqIndex = freqIn != null ? freqIn.Index() : null;
            _lastDocIndex = docIn.Index();
            _lastPosIndex = posIn != null ? posIn.Index() : null;
        }


        internal virtual IndexOptions IndexOptions // LUCENENET TODO: Make into SetIndexOptions(IndexOptions)
        {
            set { _indexOptions = value; }
        }

        internal virtual void Init(long skipPointer, IntIndexInputIndex docBaseIndex, IntIndexInputIndex freqBaseIndex,
            IntIndexInputIndex posBaseIndex, long payloadBasePointer, int df, bool storesPayloads)
        {

            base.Init(skipPointer, df);
            _currentFieldStoresPayloads = storesPayloads;

            _lastPayloadPointer = payloadBasePointer;

            for (var i = 0; i < m_maxNumberOfSkipLevels; i++)
            {
                _docIndex[i].CopyFrom(docBaseIndex);
                if (_freqIndex != null)
                    _freqIndex[i].CopyFrom(freqBaseIndex);

                if (posBaseIndex != null)
                    _posIndex[i].CopyFrom(posBaseIndex);
            }
            Arrays.Fill(_payloadPointer, payloadBasePointer);
            Arrays.Fill(_payloadLength, 0);
        }

        internal virtual long PayloadPointer
        {
            get { return _lastPayloadPointer; }
        }

        /// <summary>
        /// Returns the payload length of the payload stored just before 
        /// the doc to which the last call of <seealso cref="MultiLevelSkipListReader#skipTo(int)"/> 
        /// has skipped.  
        /// </summary>
        internal virtual int PayloadLength
        {
            get { return _lastPayloadLength; }
        }

        protected override void SeekChild(int level)
        {
            base.SeekChild(level);
            _payloadPointer[level] = _lastPayloadPointer;
            _payloadLength[level] = _lastPayloadLength;
        }

        protected override void SetLastSkipData(int level)
        {
            base.SetLastSkipData(level);

            _lastPayloadPointer = _payloadPointer[level];
            _lastPayloadLength = _payloadLength[level];
                
            if (_freqIndex != null)
                _lastFreqIndex.CopyFrom(_freqIndex[level]);
                
            _lastDocIndex.CopyFrom(_docIndex[level]);

            if (_lastPosIndex != null)
                _lastPosIndex.CopyFrom(_posIndex[level]);

            if (level <= 0) return;

            if (_freqIndex != null)
                _freqIndex[level - 1].CopyFrom(_freqIndex[level]);
                
            _docIndex[level - 1].CopyFrom(_docIndex[level]);
            
            if (_posIndex != null)
                _posIndex[level - 1].CopyFrom(_posIndex[level]);
        }

        internal virtual IntIndexInputIndex FreqIndex
        {
            get { return _lastFreqIndex; }
        }

        internal virtual IntIndexInputIndex PosIndex
        {
            get { return _lastPosIndex; }
        }

        internal virtual IntIndexInputIndex DocIndex
        {
            get { return _lastDocIndex; }
        }

        protected override int ReadSkipData(int level, IndexInput skipStream)
        {
            int delta;
            Debug.Assert(_indexOptions == IndexOptions.DOCS_AND_FREQS_AND_POSITIONS ||
                         !_currentFieldStoresPayloads);
            if (_currentFieldStoresPayloads)
            {
                // the current field stores payloads.
                // if the doc delta is odd then we have
                // to read the current payload length
                // because it differs from the length of the
                // previous payload
                delta = skipStream.ReadVInt();
                if ((delta & 1) != 0)
                {
                    _payloadLength[level] = skipStream.ReadVInt();
                }
                delta = (int) ((uint) delta >> 1);
            }
            else
            {
                delta = skipStream.ReadVInt();
            }

            if (_indexOptions != IndexOptions.DOCS_ONLY)
                _freqIndex[level].Read(skipStream, false);
            
            _docIndex[level].Read(skipStream, false);
            if (_indexOptions != IndexOptions.DOCS_AND_FREQS_AND_POSITIONS) return delta;

            _posIndex[level].Read(skipStream, false);
            
            if (_currentFieldStoresPayloads)
                _payloadPointer[level] += skipStream.ReadVInt();
            
            return delta;
        }
    }

}