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
    using Util;

    /// <summary>
    /// Concrete class that reads the current doc/freq/skip
    /// postings format.    
    /// 
    /// @lucene.experimental
    /// </summary>
    /// <remarks>
    /// TODO: -- should we switch "hasProx" higher up?  and
    /// create two separate docs readers, one that also reads
    /// prox and one that doesn't?
    /// </remarks>
    public class SepPostingsReader : PostingsReaderBase
    {
        private readonly IntIndexInput _freqIn;
        private readonly IntIndexInput _docIn;
        private readonly IntIndexInput _posIn;
        private readonly IndexInput _payloadIn;
        private readonly IndexInput _skipIn;

        private int _skipInterval;
        private int _maxSkipLevels;
        private int _skipMinimum;

        public SepPostingsReader(Directory dir, FieldInfos fieldInfos, SegmentInfo segmentInfo, IOContext context,
            IntStreamFactory intFactory, string segmentSuffix)
        {
            var success = false;
            try
            {

                var docFileName = IndexFileNames.SegmentFileName(segmentInfo.Name, segmentSuffix,
                    SepPostingsWriter.DOC_EXTENSION);
                _docIn = intFactory.OpenInput(dir, docFileName, context);

                _skipIn =
                    dir.OpenInput(
                        IndexFileNames.SegmentFileName(segmentInfo.Name, segmentSuffix, SepPostingsWriter.SKIP_EXTENSION),
                        context);

                if (fieldInfos.HasFreq)
                {
                    _freqIn = intFactory.OpenInput(dir,
                        IndexFileNames.SegmentFileName(segmentInfo.Name, segmentSuffix, SepPostingsWriter.FREQ_EXTENSION),
                        context);
                }
                else
                {
                    _freqIn = null;
                }
                if (fieldInfos.HasProx)
                {
                    _posIn = intFactory.OpenInput(dir,
                        IndexFileNames.SegmentFileName(segmentInfo.Name, segmentSuffix, SepPostingsWriter.POS_EXTENSION),
                        context);
                    _payloadIn =
                        dir.OpenInput(
                            IndexFileNames.SegmentFileName(segmentInfo.Name, segmentSuffix,
                                SepPostingsWriter.PAYLOAD_EXTENSION), context);
                }
                else
                {
                    _posIn = null;
                    _payloadIn = null;
                }
                success = true;
            }
            finally
            {
                if (!success)
                {
                    Dispose();
                }
            }
        }

        public override void Init(IndexInput termsIn)
        {
            // Make sure we are talking to the matching past writer
            CodecUtil.CheckHeader(termsIn, SepPostingsWriter.CODEC, SepPostingsWriter.VERSION_START,
                SepPostingsWriter.VERSION_START);
            _skipInterval = termsIn.ReadInt();
            _maxSkipLevels = termsIn.ReadInt();
            _skipMinimum = termsIn.ReadInt();
        }

        protected override void Dispose(bool disposing)
        {
            if (!disposing) return;

            IOUtils.Close(_freqIn, _docIn, _skipIn, _posIn, _payloadIn);
        }

        public override BlockTermState NewTermState()
        {
            var state = new SepTermState {DOC_INDEX = _docIn.Index()};

            if (_freqIn != null)
                state.FREQ_INDEX = _freqIn.Index();

            if (_posIn != null)
                state.POS_INDEX = _posIn.Index();

            return state;
        }

        public override void DecodeTerm(long[] empty, DataInput input, FieldInfo fieldInfo, BlockTermState bTermState,
            bool absolute)
        {
            var termState = (SepTermState) bTermState;
            termState.DOC_INDEX.Read(input, absolute);
            if (fieldInfo.IndexOptions != IndexOptions.DOCS_ONLY)
            {
                termState.FREQ_INDEX.Read(input, absolute);
                if (fieldInfo.IndexOptions == IndexOptions.DOCS_AND_FREQS_AND_POSITIONS)
                {
                    termState.POS_INDEX.Read(input, absolute);
                    
                    if (fieldInfo.HasPayloads)
                    {
                        if (absolute)
                        {
                            termState.PAYLOAD_FP = input.ReadVLong();
                        }
                        else
                        {
                            termState.PAYLOAD_FP += input.ReadVLong();
                        }
                    }
                }
            }

            if (termState.DocFreq >= _skipMinimum)
            {
                if (absolute)
                {
                    termState.SKIP_FP = input.ReadVLong();
                }
                else
                {
                    termState.SKIP_FP += input.ReadVLong();
                }
            }
            else if (absolute)
            {
                termState.SKIP_FP = 0;
            }
        }

        public override DocsEnum Docs(FieldInfo fieldInfo, BlockTermState bTermState, Bits liveDocs, DocsEnum reuse,
            int flags)
        {
            var termState = (SepTermState)bTermState;

            SepDocsEnum docsEnum;
            if (!(reuse is SepDocsEnum))
            {
                docsEnum = new SepDocsEnum(this);
            }
            else
            {
                docsEnum = (SepDocsEnum) reuse;
                if (docsEnum.START_DOC_IN != _docIn)
                {
                    // If you are using ParellelReader, and pass in a
                    // reused DocsAndPositionsEnum, it could have come
                    // from another reader also using sep codec
                    docsEnum = new SepDocsEnum(this);
                }
            }

            return docsEnum.Init(fieldInfo, termState, liveDocs);
        }

        public override DocsAndPositionsEnum DocsAndPositions(FieldInfo fieldInfo, BlockTermState bTermState,
            Bits liveDocs, DocsAndPositionsEnum reuse, int flags)
        {

            Debug.Assert(fieldInfo.IndexOptions == IndexOptions.DOCS_AND_FREQS_AND_POSITIONS);
            var termState = (SepTermState)bTermState;
            SepDocsAndPositionsEnum postingsEnum;
            if (!(reuse is SepDocsAndPositionsEnum))
            {
                postingsEnum = new SepDocsAndPositionsEnum(this);
            }
            else
            {
                postingsEnum = (SepDocsAndPositionsEnum) reuse;
                if (postingsEnum.START_DOC_IN != _docIn)
                {
                    // If you are using ParellelReader, and pass in a
                    // reused DocsAndPositionsEnum, it could have come
                    // from another reader also using sep codec
                    postingsEnum = new SepDocsAndPositionsEnum(this);
                }
            }

            return postingsEnum.Init(fieldInfo, termState, liveDocs);
        }

        public override long RamBytesUsed()
        {
            return 0;
        }

        public override void CheckIntegrity()
        {
            // TODO: remove sep layout, its fallen behind on features...
        }

        internal sealed class SepTermState : BlockTermState
        {
            // We store only the seek point to the docs file because
            // the rest of the info (freqIndex, posIndex, etc.) is
            // stored in the docs file:
            internal IntIndexInputIndex DOC_INDEX;
            internal IntIndexInputIndex POS_INDEX;
            internal IntIndexInputIndex FREQ_INDEX;
            internal long PAYLOAD_FP;
            internal long SKIP_FP;

            public override object Clone()
            {
                var other = new SepTermState();
                other.CopyFrom(this);
                return other;
            }

            public override void CopyFrom(TermState tsOther)
            {
                base.CopyFrom(tsOther);

                var other = (SepTermState)tsOther;
                if (DOC_INDEX == null)
                {
                    DOC_INDEX = other.DOC_INDEX.Clone();
                }
                else
                {
                    DOC_INDEX.CopyFrom(other.DOC_INDEX);
                }
                if (other.FREQ_INDEX != null)
                {
                    if (FREQ_INDEX == null)
                    {
                        FREQ_INDEX = other.FREQ_INDEX.Clone();
                    }
                    else
                    {
                        FREQ_INDEX.CopyFrom(other.FREQ_INDEX);
                    }
                }
                else
                {
                    FREQ_INDEX = null;
                }
                if (other.POS_INDEX != null)
                {
                    if (POS_INDEX == null)
                    {
                        POS_INDEX = other.POS_INDEX.Clone();
                    }
                    else
                    {
                        POS_INDEX.CopyFrom(other.POS_INDEX);
                    }
                }
                else
                {
                    POS_INDEX = null;
                }
                PAYLOAD_FP = other.PAYLOAD_FP;
                SKIP_FP = other.SKIP_FP;
            }

            public override string ToString()
            {
                return base.ToString() + " docIndex=" + DOC_INDEX + " freqIndex=" + FREQ_INDEX + " posIndex=" + POS_INDEX +
                       " payloadFP=" + PAYLOAD_FP + " skipFP=" + SKIP_FP;
            }
        }


        internal class SepDocsEnum : DocsEnum
        {
            private readonly SepPostingsReader _outerInstance;

            private int _docFreq;
            private int _doc = -1;
            private int _accum;
            private int _count;
            private int _freq;

            // TODO: -- should we do omitTF with 2 different enum classes?
            private bool _omitTf;
            private IndexOptions _indexOptions;
            private bool _storePayloads;
            private Bits _liveDocs;
            private readonly IntIndexInputReader _docReader;
            private readonly IntIndexInputReader _freqReader;
            private long _skipFp;

            private readonly IntIndexInputIndex _docIndex;
            private readonly IntIndexInputIndex _freqIndex;
            private readonly IntIndexInputIndex _posIndex;

            // TODO: -- should we do hasProx with 2 different enum classes?

            private bool _skipped;
            private SepSkipListReader _skipper;

            internal IntIndexInput START_DOC_IN;

            internal SepDocsEnum(SepPostingsReader outerInstance)
            {
                _outerInstance = outerInstance;
                _docReader = outerInstance._docIn.Reader();
                _docIndex = outerInstance._docIn.Index();
                if (outerInstance._freqIn != null)
                {
                    _freqReader = outerInstance._freqIn.Reader();
                    _freqIndex = outerInstance._freqIn.Index();
                }
                else
                {
                    _freqReader = null;
                    _freqIndex = null;
                }
                _posIndex = outerInstance._posIn != null ? outerInstance._posIn.Index() : null;

                START_DOC_IN = outerInstance._docIn;
            }

            internal virtual SepDocsEnum Init(FieldInfo fieldInfo, SepTermState termState, Bits liveDocs)
            {
                _liveDocs = liveDocs;
                if (fieldInfo.IndexOptions.HasValue)
                    _indexOptions = fieldInfo.IndexOptions.Value;

                _omitTf = _indexOptions == IndexOptions.DOCS_ONLY;
                _storePayloads = fieldInfo.HasPayloads;

                // TODO: can't we only do this if consumer
                // skipped consuming the previous docs?
                _docIndex.CopyFrom(termState.DOC_INDEX);
                _docIndex.Seek(_docReader);

                if (!_omitTf)
                {
                    _freqIndex.CopyFrom(termState.FREQ_INDEX);
                    _freqIndex.Seek(_freqReader);
                }

                _docFreq = termState.DocFreq;
                // NOTE: unused if docFreq < skipMinimum:
                _skipFp = termState.SKIP_FP;
                _count = 0;
                _doc = -1;
                _accum = 0;
                _freq = 1;
                _skipped = false;

                return this;
            }

            public override int NextDoc()
            {

                while (true)
                {
                    if (_count == _docFreq)
                    {
                        return _doc = NO_MORE_DOCS;
                    }

                    _count++;

                    // Decode next doc
                    //System.out.println("decode docDelta:");
                    _accum += _docReader.Next();

                    if (!_omitTf)
                    {
                        //System.out.println("decode freq:");
                        _freq = _freqReader.Next();
                    }

                    if (_liveDocs == null || _liveDocs.Get(_accum))
                    {
                        break;
                    }
                }
                return (_doc = _accum);
            }

            public override int Freq
            {
                get { return _freq; }
            }

            public override int DocID()
            {
                return _doc;
            }

            public override int Advance(int target)
            {

                if ((target - _outerInstance._skipInterval) >= _doc && _docFreq >= _outerInstance._skipMinimum)
                {

                    // There are enough docs in the posting to have
                    // skip data, and its not too close

                    if (_skipper == null)
                    {
                        // This DocsEnum has never done any skipping
                        _skipper = new SepSkipListReader((IndexInput) _outerInstance._skipIn.Clone(),
                            _outerInstance._freqIn,
                            _outerInstance._docIn, _outerInstance._posIn, _outerInstance._maxSkipLevels,
                            _outerInstance._skipInterval);

                    }

                    if (!_skipped)
                    {
                        // We haven't yet skipped for this posting
                        _skipper.Init(_skipFp, _docIndex, _freqIndex, _posIndex, 0, _docFreq, _storePayloads);
                        _skipper.IndexOptions = _indexOptions;

                        _skipped = true;
                    }

                    int newCount = _skipper.SkipTo(target);

                    if (newCount > _count)
                    {

                        // Skipper did move
                        if (!_omitTf)
                        {
                            _skipper.FreqIndex.Seek(_freqReader);
                        }
                        _skipper.DocIndex.Seek(_docReader);
                        _count = newCount;
                        _doc = _accum = _skipper.Doc;
                    }
                }

                // Now, linear scan for the rest:
                do
                {
                    if (NextDoc() == NO_MORE_DOCS)
                    {
                        return NO_MORE_DOCS;
                    }
                } while (target > _doc);

                return _doc;
            }

            public override long Cost()
            {
                return _docFreq;
            }
        }

        internal class SepDocsAndPositionsEnum : DocsAndPositionsEnum
        {
            private readonly SepPostingsReader _outerInstance;
            private BytesRef _payload;

            private int _docFreq;
            private int _doc = -1;
            private int _accum;
            private int _count;
            private int _freq;

            private bool _storePayloads;
            private Bits _liveDocs;
            private readonly IntIndexInputReader _docReader;
            private readonly IntIndexInputReader _freqReader;
            private readonly IntIndexInputReader _posReader;
            private readonly IndexInput _payloadIn;
            private long _skipFp;

            private readonly IntIndexInputIndex _docIndex;
            private readonly IntIndexInputIndex _freqIndex;
            private readonly IntIndexInputIndex _posIndex;

            private long _payloadFp;

            private int _pendingPosCount;
            private int _position;
            private int _payloadLength;
            private long _pendingPayloadBytes;

            private bool _skipped;
            private SepSkipListReader _skipper;
            private bool _payloadPending;
            private bool _posSeekPending;

            internal IntIndexInput START_DOC_IN;

            internal SepDocsAndPositionsEnum(SepPostingsReader outerInstance)
            {
                _outerInstance = outerInstance;
                _docReader = outerInstance._docIn.Reader();
                _docIndex = outerInstance._docIn.Index();
                _freqReader = outerInstance._freqIn.Reader();
                _freqIndex = outerInstance._freqIn.Index();
                _posReader = outerInstance._posIn.Reader();
                _posIndex = outerInstance._posIn.Index();
                _payloadIn = (IndexInput) outerInstance._payloadIn.Clone();

                START_DOC_IN = outerInstance._docIn;
            }

            internal virtual SepDocsAndPositionsEnum Init(FieldInfo fieldInfo, SepTermState termState, Bits liveDocs)
            {
                _liveDocs = liveDocs;
                _storePayloads = fieldInfo.HasPayloads;

                // TODO: can't we only do this if consumer skipped consuming the previous docs?
                _docIndex.CopyFrom(termState.DOC_INDEX);
                _docIndex.Seek(_docReader);

                _freqIndex.CopyFrom(termState.FREQ_INDEX);
                _freqIndex.Seek(_freqReader);

                _posIndex.CopyFrom(termState.POS_INDEX);
                _posSeekPending = true;
                _payloadPending = false;

                _payloadFp = termState.PAYLOAD_FP;
                _skipFp = termState.SKIP_FP;

                _docFreq = termState.DocFreq;
                _count = 0;
                _doc = -1;
                _accum = 0;
                _pendingPosCount = 0;
                _pendingPayloadBytes = 0;
                _skipped = false;

                return this;
            }

            public override int NextDoc()
            {
                while (true)
                {
                    if (_count == _docFreq)
                        return _doc = NO_MORE_DOCS;

                    _count++;

                    // Decode next doc
                    _accum += _docReader.Next();
                    _freq = _freqReader.Next();
                    _pendingPosCount += _freq;

                    if (_liveDocs == null || _liveDocs.Get(_accum))
                        break;
                }

                _position = 0;
                return (_doc = _accum);
            }

            public override int Freq
            {
                get { return _freq; }
            }

            public override int DocID()
            {
                return _doc;
            }

            public override int Advance(int target)
            {
                //System.out.println("SepD&P advance target=" + target + " vs current=" + doc + " this=" + this);

                if ((target - _outerInstance._skipInterval) >= _doc && _docFreq >= _outerInstance._skipMinimum)
                {

                    // There are enough docs in the posting to have
                    // skip data, and its not too close

                    if (_skipper == null)
                    {
                        //System.out.println("  create skipper");
                        // This DocsEnum has never done any skipping
                        _skipper = new SepSkipListReader((IndexInput) _outerInstance._skipIn.Clone(),
                            _outerInstance._freqIn,
                            _outerInstance._docIn, _outerInstance._posIn, _outerInstance._maxSkipLevels,
                            _outerInstance._skipInterval);
                    }

                    if (!_skipped)
                    {
                        //System.out.println("  init skip data skipFP=" + skipFP);
                        // We haven't yet skipped for this posting
                        _skipper.Init(_skipFp, _docIndex, _freqIndex, _posIndex, _payloadFp, _docFreq, _storePayloads);
                        _skipper.IndexOptions = IndexOptions.DOCS_AND_FREQS_AND_POSITIONS;
                        _skipped = true;
                    }

                    int newCount = _skipper.SkipTo(target);

                    if (newCount > _count)
                    {

                        // Skipper did move
                        _skipper.FreqIndex.Seek(_freqReader);
                        _skipper.DocIndex.Seek(_docReader);

                        // NOTE: don't seek pos here; do it lazily
                        // instead.  Eg a PhraseQuery may skip to many
                        // docs before finally asking for positions...

                        _posIndex.CopyFrom(_skipper.PosIndex);
                        _posSeekPending = true;
                        _count = newCount;
                        _doc = _accum = _skipper.Doc;

                        _payloadFp = _skipper.PayloadPointer;
                        _pendingPosCount = 0;
                        _pendingPayloadBytes = 0;
                        _payloadPending = false;
                        _payloadLength = _skipper.PayloadLength;
                    }
                }

                // Now, linear scan for the rest:
                do
                {
                    if (NextDoc() == NO_MORE_DOCS)
                    {
                        return NO_MORE_DOCS;
                    }

                } while (target > _doc);

                return _doc;
            }

            public override int NextPosition()
            {
                if (_posSeekPending)
                {
                    _posIndex.Seek(_posReader);
                    _payloadIn.Seek(_payloadFp);
                    _posSeekPending = false;
                }

                int code;

                // scan over any docs that were iterated without their positions
                while (_pendingPosCount > _freq)
                {
                    code = _posReader.Next();
                    if (_storePayloads && (code & 1) != 0)
                    {
                        // Payload length has changed
                        _payloadLength = _posReader.Next();
                        Debug.Assert(_payloadLength >= 0);
                    }
                    _pendingPosCount--;
                    _position = 0;
                    _pendingPayloadBytes += _payloadLength;
                }

                code = _posReader.Next();

                if (_storePayloads)
                {
                    if ((code & 1) != 0)
                    {
                        // Payload length has changed
                        _payloadLength = _posReader.Next();
                        Debug.Assert(_payloadLength >= 0);
                    }
                    _position += (int) ((uint) code >> 1);
                    _pendingPayloadBytes += _payloadLength;
                    _payloadPending = _payloadLength > 0;
                }
                else
                {
                    _position += code;
                }

                _pendingPosCount--;
                Debug.Assert(_pendingPosCount >= 0);
                return _position;
            }

            public override int StartOffset
            {
                get { return -1; }
            }

            public override int EndOffset
            {
                get { return -1; }
            }

            public override BytesRef Payload
            {
                get
                {
                    if (!_payloadPending)
                    {
                        return null;
                    }

                    if (_pendingPayloadBytes == 0)
                    {
                        return _payload;
                    }

                    Debug.Assert(_pendingPayloadBytes >= _payloadLength);

                    if (_pendingPayloadBytes > _payloadLength)
                    {
                        _payloadIn.Seek(_payloadIn.FilePointer + (_pendingPayloadBytes - _payloadLength));
                    }

                    if (_payload == null)
                    {
                        _payload = new BytesRef {Bytes = new byte[_payloadLength]};
                    }
                    else if (_payload.Bytes.Length < _payloadLength)
                    {
                        _payload.Grow(_payloadLength);
                    }

                    _payloadIn.ReadBytes(_payload.Bytes, 0, _payloadLength);
                    _payload.Length = _payloadLength;
                    _pendingPayloadBytes = 0;
                    return _payload;
                }
            }

            public override long Cost()
            {
                return _docFreq;
            }
        }
    }
}