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
      private IndexOptions _indexOptions;

      private readonly IntIndexOutputIndex[] _docIndex;
      private readonly IntIndexOutputIndex[] _freqIndex;
      private readonly IntIndexOutputIndex[] _posIndex;

	  private readonly IntIndexOutput _freqOutput;
	  private IntIndexOutput _posOutput;
      private IndexOutput _payloadOutput;

	  private int _curDoc;
	  private bool _curStorePayloads;
	  private int _curPayloadLength;
	  private long _curPayloadPointer;

	    internal SepSkipListWriter(int skipInterval, int numberOfSkipLevels, int docCount, IntIndexOutput freqOutput,
	        IntIndexOutput docOutput, IntIndexOutput posOutput, IndexOutput payloadOutput)
	        : base(skipInterval, numberOfSkipLevels, docCount)
	    {

	        _freqOutput = freqOutput;
	        _posOutput = posOutput;
	        _payloadOutput = payloadOutput;

	        _lastSkipDoc = new int[numberOfSkipLevels];
	        _lastSkipPayloadLength = new int[numberOfSkipLevels];
	        // TODO: -- also cutover normal IndexOutput to use getIndex()?
	        _lastSkipPayloadPointer = new long[numberOfSkipLevels];

	        _freqIndex = new IntIndexOutputIndex[numberOfSkipLevels];
	        _docIndex = new IntIndexOutputIndex[numberOfSkipLevels];
	        _posIndex = new IntIndexOutputIndex[numberOfSkipLevels];

	        for (var i = 0; i < numberOfSkipLevels; i++)
	        {
	            if (freqOutput != null)
	            {
	                _freqIndex[i] = freqOutput.Index();
	            }
	            _docIndex[i] = docOutput.Index();
	            if (posOutput != null)
	            {
	                _posIndex[i] = posOutput.Index();
	            }
	        }
	    }

	    internal virtual IndexOptions IndexOptions
	    {
	        set { _indexOptions = value; }
	    }

	    internal virtual IntIndexOutput PosOutput
	    {
	        set
	        {
	            _posOutput = value;
                for (var i = 0; i < m_numberOfSkipLevels; i++)
	            {
	                _posIndex[i] = value.Index();
	            }
	        }
	    }

	    internal virtual IndexOutput PayloadOutput
	    {
	        set { _payloadOutput = value; }
	    }

	    /// <summary>
	    /// Sets the values for the current skip data. 
        /// Called @ every index interval (every 128th (by default) doc)
	    /// </summary>
	    internal virtual void SetSkipData(int doc, bool storePayloads, int payloadLength)
	    {
	        _curDoc = doc;
	        _curStorePayloads = storePayloads;
	        _curPayloadLength = payloadLength;
	        if (_payloadOutput != null)
	        {
	            _curPayloadPointer = _payloadOutput.FilePointer;
	        }
	    }
        
	    /// <summary>
	    /// Called @ start of new term
	    /// </summary>
        protected internal virtual void ResetSkip(IntIndexOutputIndex topDocIndex, IntIndexOutputIndex topFreqIndex,
	        IntIndexOutputIndex topPosIndex)
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
	                skipBuffer.WriteVInt(delta << 1);
	            }
	            else
	            {
	                // the payload length is different from the previous one. We shift the DocSkip, 
	                // set the lowest bit and store the current payload length as VInt.
	                skipBuffer.WriteVInt(delta << 1 | 1);
	                skipBuffer.WriteVInt(_curPayloadLength);
	                _lastSkipPayloadLength[level] = _curPayloadLength;
	            }
	        }
	        else
	        {
	            // current field does not store payloads
	            skipBuffer.WriteVInt(_curDoc - _lastSkipDoc[level]);
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
	                skipBuffer.WriteVInt((int) (_curPayloadPointer - _lastSkipPayloadPointer[level]));
	            }
	        }

	        _lastSkipDoc[level] = _curDoc;
	        _lastSkipPayloadPointer[level] = _curPayloadPointer;
	    }
	}

}