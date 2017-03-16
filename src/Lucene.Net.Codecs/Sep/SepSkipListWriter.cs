using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Support;
using System.Diagnostics;

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
    /// Implements the skip list writer for the default posting list format
    /// that stores positions and payloads.
    /// 
    /// @lucene.experimental
    /// </summary>
    /// <remarks>
    /// TODO: -- skip data should somehow be more local to the particular stream 
    /// (doc, freq, pos, payload)
    /// </remarks>
    internal class SepSkipListWriter : MultiLevelSkipListWriter
    {
        private readonly int[] _lastSkipDoc;
        private readonly int[] _lastSkipPayloadLength;
        private readonly long[] _lastSkipPayloadPointer;

        private readonly Int32IndexOutput.AbstractIndex[] _docIndex;
        private readonly Int32IndexOutput.AbstractIndex[] _freqIndex;
        private readonly Int32IndexOutput.AbstractIndex[] _posIndex;

        private readonly Int32IndexOutput _freqOutput;
        private Int32IndexOutput _posOutput;
        private IndexOutput _payloadOutput;

        private int _curDoc;
        private bool _curStorePayloads;
        private int _curPayloadLength;
        private long _curPayloadPointer;

        internal SepSkipListWriter(int skipInterval, int numberOfSkipLevels, int docCount, 
            Int32IndexOutput freqOutput,
            Int32IndexOutput docOutput, 
            Int32IndexOutput posOutput, 
            IndexOutput payloadOutput)
            : base(skipInterval, numberOfSkipLevels, docCount)
        {
            this._freqOutput = freqOutput;
            this._posOutput = posOutput;
            this._payloadOutput = payloadOutput;

            _lastSkipDoc = new int[numberOfSkipLevels];
            _lastSkipPayloadLength = new int[numberOfSkipLevels];
            // TODO: -- also cutover normal IndexOutput to use getIndex()?
            _lastSkipPayloadPointer = new long[numberOfSkipLevels];

            _freqIndex = new Int32IndexOutput.AbstractIndex[numberOfSkipLevels];
            _docIndex = new Int32IndexOutput.AbstractIndex[numberOfSkipLevels];
            _posIndex = new Int32IndexOutput.AbstractIndex[numberOfSkipLevels];

            for (var i = 0; i < numberOfSkipLevels; i++)
            {
                if (freqOutput != null)
                {
                    _freqIndex[i] = freqOutput.GetIndex();
                }
                _docIndex[i] = docOutput.GetIndex();
                if (posOutput != null)
                {
                    _posIndex[i] = posOutput.GetIndex();
                }
            }
        }

        private IndexOptions? _indexOptions;

        internal virtual void SetIndexOptions(IndexOptions? v)
        {
            _indexOptions = v;
        }

        internal virtual void SetPosOutput(Int32IndexOutput posOutput) 
        {
            _posOutput = posOutput;
            for (var i = 0; i < m_numberOfSkipLevels; i++)
            {
                _posIndex[i] = posOutput.GetIndex();
            }
        }

        internal virtual void SetPayloadOutput(IndexOutput payloadOutput)
        {
            _payloadOutput = payloadOutput;
        }

        /// <summary>
        /// Sets the values for the current skip data. 
        /// Called @ every index interval (every 128th (by default) doc)
        /// </summary>
        internal virtual void SetSkipData(int doc, bool storePayloads, int payloadLength)
        {
            this._curDoc = doc;
            this._curStorePayloads = storePayloads;
            this._curPayloadLength = payloadLength;
            if (_payloadOutput != null)
            {
                _curPayloadPointer = _payloadOutput.FilePointer;
            }
        }

        /// <summary>
        /// Called @ start of new term
        /// </summary>
        protected internal virtual void ResetSkip(Int32IndexOutput.AbstractIndex topDocIndex, Int32IndexOutput.AbstractIndex topFreqIndex,
            Int32IndexOutput.AbstractIndex topPosIndex)
        {
            base.ResetSkip();

            Arrays.Fill(_lastSkipDoc, 0);
            Arrays.Fill(_lastSkipPayloadLength, -1); // we don't have to write the first length in the skip list
            for (int i = 0; i < m_numberOfSkipLevels; i++)
            {
                _docIndex[i].CopyFrom(topDocIndex, true);
                if (_freqOutput != null)
                {
                    _freqIndex[i].CopyFrom(topFreqIndex, true);
                }
                if (_posOutput != null)
                {
                    _posIndex[i].CopyFrom(topPosIndex, true);
                }
            }
            if (_payloadOutput != null)
            {
                Arrays.Fill(_lastSkipPayloadPointer, _payloadOutput.FilePointer);
            }
        }

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

            Debug.Assert(_indexOptions == IndexOptions.DOCS_AND_FREQS_AND_POSITIONS || !_curStorePayloads);

            if (_curStorePayloads)
            {
                int delta = _curDoc - _lastSkipDoc[level];
                if (_curPayloadLength == _lastSkipPayloadLength[level])
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
                    skipBuffer.WriteVInt32(_curPayloadLength);
                    _lastSkipPayloadLength[level] = _curPayloadLength;
                }
            }
            else
            {
                // current field does not store payloads
                skipBuffer.WriteVInt32(_curDoc - _lastSkipDoc[level]);
            }

            if (_indexOptions != IndexOptions.DOCS_ONLY)
            {
                _freqIndex[level].Mark();
                _freqIndex[level].Write(skipBuffer, false);
            }
            _docIndex[level].Mark();
            _docIndex[level].Write(skipBuffer, false);
            if (_indexOptions == IndexOptions.DOCS_AND_FREQS_AND_POSITIONS)
            {
                _posIndex[level].Mark();
                _posIndex[level].Write(skipBuffer, false);
                if (_curStorePayloads)
                {
                    skipBuffer.WriteVInt32((int)(_curPayloadPointer - _lastSkipPayloadPointer[level]));
                }
            }

            _lastSkipDoc[level] = _curDoc;
            _lastSkipPayloadPointer[level] = _curPayloadPointer;
        }
    }
}