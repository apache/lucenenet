using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Util;
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
        private readonly Int32IndexInput _freqIn;
        private readonly Int32IndexInput _docIn;
        private readonly Int32IndexInput _posIn;
        private readonly IndexInput _payloadIn;
        private readonly IndexInput _skipIn;

        private int _skipInterval;
        private int _maxSkipLevels;
        private int _skipMinimum;

        public SepPostingsReader(Directory dir, FieldInfos fieldInfos, SegmentInfo segmentInfo, IOContext context,
            Int32StreamFactory intFactory, string segmentSuffix)
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
            _skipInterval = termsIn.ReadInt32();
            _maxSkipLevels = termsIn.ReadInt32();
            _skipMinimum = termsIn.ReadInt32();
        }

        protected override void Dispose(bool disposing)
        {
            if (!disposing) return;

            IOUtils.Close(_freqIn, _docIn, _skipIn, _posIn, _payloadIn);
        }

        internal sealed class SepTermState : BlockTermState
        {
            // We store only the seek point to the docs file because
            // the rest of the info (freqIndex, posIndex, etc.) is
            // stored in the docs file:
            internal Int32IndexInput.AbstractIndex docIndex;
            internal Int32IndexInput.AbstractIndex posIndex;
            internal Int32IndexInput.AbstractIndex freqIndex;
            internal long payloadFP;
            internal long skipFP;

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
                if (docIndex == null)
                {
                    docIndex = other.docIndex.Clone();
                }
                else
                {
                    docIndex.CopyFrom(other.docIndex);
                }
                if (other.freqIndex != null)
                {
                    if (freqIndex == null)
                    {
                        freqIndex = other.freqIndex.Clone();
                    }
                    else
                    {
                        freqIndex.CopyFrom(other.freqIndex);
                    }
                }
                else
                {
                    freqIndex = null;
                }
                if (other.posIndex != null)
                {
                    if (posIndex == null)
                    {
                        posIndex = other.posIndex.Clone();
                    }
                    else
                    {
                        posIndex.CopyFrom(other.posIndex);
                    }
                }
                else
                {
                    posIndex = null;
                }
                payloadFP = other.payloadFP;
                skipFP = other.skipFP;
            }

            public override string ToString()
            {
                return base.ToString() + " docIndex=" + docIndex + " freqIndex=" + freqIndex + " posIndex=" + posIndex +
                       " payloadFP=" + payloadFP + " skipFP=" + skipFP;
            }
        }

        public override BlockTermState NewTermState()
        {
            var state = new SepTermState { docIndex = _docIn.GetIndex() };

            if (_freqIn != null)
            {
                state.freqIndex = _freqIn.GetIndex();
            }

            if (_posIn != null)
            {
                state.posIndex = _posIn.GetIndex();
            }

            return state;
        }

        public override void DecodeTerm(long[] empty, DataInput input, FieldInfo fieldInfo, BlockTermState bTermState,
            bool absolute)
        {
            var termState = (SepTermState) bTermState;
            termState.docIndex.Read(input, absolute);
            if (fieldInfo.IndexOptions != IndexOptions.DOCS_ONLY)
            {
                termState.freqIndex.Read(input, absolute);
                if (fieldInfo.IndexOptions == IndexOptions.DOCS_AND_FREQS_AND_POSITIONS)
                {
                    //System.out.println("  freqIndex=" + termState.freqIndex);
                    termState.posIndex.Read(input, absolute);
                    //System.out.println("  posIndex=" + termState.posIndex);
                    if (fieldInfo.HasPayloads)
                    {
                        if (absolute)
                        {
                            termState.payloadFP = input.ReadVInt64();
                        }
                        else
                        {
                            termState.payloadFP += input.ReadVInt64();
                        }
                        //System.out.println("  payloadFP=" + termState.payloadFP);
                    }
                }
            }

            if (termState.DocFreq >= _skipMinimum)
            {
                //System.out.println("   readSkip @ " + in.getPosition());
                if (absolute)
                {
                    termState.skipFP = input.ReadVInt64();
                }
                else
                {
                    termState.skipFP += input.ReadVInt64();
                }
                //System.out.println("  skipFP=" + termState.skipFP);
            }
            else if (absolute)
            {
                termState.skipFP = 0;
            }
        }

        public override DocsEnum Docs(FieldInfo fieldInfo, BlockTermState bTermState, IBits liveDocs, DocsEnum reuse,
            DocsFlags flags)
        {
            SepTermState termState = (SepTermState)bTermState;
            SepDocsEnum docsEnum;
            if (reuse == null || !(reuse is SepDocsEnum))
            {
                docsEnum = new SepDocsEnum(this);
            }
            else
            {
                docsEnum = (SepDocsEnum) reuse;
                if (docsEnum.startDocIn != _docIn)
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
            IBits liveDocs, DocsAndPositionsEnum reuse, DocsAndPositionsFlags flags)
        {

            Debug.Assert(fieldInfo.IndexOptions == IndexOptions.DOCS_AND_FREQS_AND_POSITIONS);
            var termState = (SepTermState)bTermState;
            SepDocsAndPositionsEnum postingsEnum;
            if (reuse == null || !(reuse is SepDocsAndPositionsEnum))
            {
                postingsEnum = new SepDocsAndPositionsEnum(this);
            }
            else
            {
                postingsEnum = (SepDocsAndPositionsEnum) reuse;
                if (postingsEnum.startDocIn != _docIn)
                {
                    // If you are using ParellelReader, and pass in a
                    // reused DocsAndPositionsEnum, it could have come
                    // from another reader also using sep codec
                    postingsEnum = new SepDocsAndPositionsEnum(this);
                }
            }

            return postingsEnum.Init(fieldInfo, termState, liveDocs);
        }

        internal class SepDocsEnum : DocsEnum
        {
            private readonly SepPostingsReader _outerInstance;

            private int _docFreq;
            private int _doc = -1;
            private int _accum;
            private int _count;
            private int _freq;
            //private long freqStart; // LUCENENET: not used

            // TODO: -- should we do omitTF with 2 different enum classes?
            private bool _omitTf;
            private IndexOptions _indexOptions;
            private bool _storePayloads;
            private IBits _liveDocs;
            private readonly Int32IndexInput.AbstractReader _docReader;
            private readonly Int32IndexInput.AbstractReader _freqReader;
            private long _skipFp;

            private readonly Int32IndexInput.AbstractIndex _docIndex;
            private readonly Int32IndexInput.AbstractIndex _freqIndex;
            private readonly Int32IndexInput.AbstractIndex _posIndex;
            internal readonly Int32IndexInput startDocIn;

            // TODO: -- should we do hasProx with 2 different enum classes?

            private bool _skipped;
            private SepSkipListReader _skipper;

            internal SepDocsEnum(SepPostingsReader outerInstance)
            {
                _outerInstance = outerInstance;

                startDocIn = outerInstance._docIn;
                _docReader = outerInstance._docIn.GetReader();
                _docIndex = outerInstance._docIn.GetIndex();
                if (outerInstance._freqIn != null)
                {
                    _freqReader = outerInstance._freqIn.GetReader();
                    _freqIndex = outerInstance._freqIn.GetIndex();
                }
                else
                {
                    _freqReader = null;
                    _freqIndex = null;
                }
                if (outerInstance._posIn != null)
                {
                    _posIndex = outerInstance._posIn.GetIndex();                 // only init this so skipper can read it
                }
                else
                {
                    _posIndex = null;
                }
            }

            internal virtual SepDocsEnum Init(FieldInfo fieldInfo, SepTermState termState, IBits liveDocs)
            {
                this._liveDocs = liveDocs;
                this._indexOptions = fieldInfo.IndexOptions;
                _omitTf = _indexOptions == IndexOptions.DOCS_ONLY;
                _storePayloads = fieldInfo.HasPayloads;

                // TODO: can't we only do this if consumer
                // skipped consuming the previous docs?
                _docIndex.CopyFrom(termState.docIndex);
                _docIndex.Seek(_docReader);

                if (!_omitTf)
                {
                    _freqIndex.CopyFrom(termState.freqIndex);
                    _freqIndex.Seek(_freqReader);
                }

                _docFreq = termState.DocFreq;
                // NOTE: unused if docFreq < skipMinimum:
                _skipFp = termState.skipFP;
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

            public override int DocID
            {
                get { return _doc; }
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
                        _skipper = new SepSkipListReader((IndexInput)_outerInstance._skipIn.Clone(),
                            _outerInstance._freqIn,
                            _outerInstance._docIn, 
                            _outerInstance._posIn, 
                            _outerInstance._maxSkipLevels,
                            _outerInstance._skipInterval);

                    }

                    if (!_skipped)
                    {
                        // We haven't yet skipped for this posting
                        _skipper.Init(_skipFp, 
                                    _docIndex, 
                                    _freqIndex, 
                                    _posIndex, 
                                    0, 
                                    _docFreq, 
                                    _storePayloads);
                        _skipper.SetIndexOptions(_indexOptions);

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

            public override long GetCost()
            {
                return _docFreq;
            }
        }

        internal class SepDocsAndPositionsEnum : DocsAndPositionsEnum
        {
            private readonly SepPostingsReader _outerInstance;

            private int _docFreq;
            private int _doc = -1;
            private int _accum;
            private int _count;
            private int _freq;
            // private long freqStart; // LUCENENET - Not used

            private bool _storePayloads;
            private IBits _liveDocs;
            private readonly Int32IndexInput.AbstractReader _docReader;
            private readonly Int32IndexInput.AbstractReader _freqReader;
            private readonly Int32IndexInput.AbstractReader _posReader;
            private readonly IndexInput _payloadIn;
            private long _skipFp;

            private readonly Int32IndexInput.AbstractIndex _docIndex;
            private readonly Int32IndexInput.AbstractIndex _freqIndex;
            private readonly Int32IndexInput.AbstractIndex _posIndex;
            internal Int32IndexInput startDocIn;

            private long _payloadFp;

            private int _pendingPosCount;
            private int _position;
            private int _payloadLength;
            private long _pendingPayloadBytes;

            private bool _skipped;
            private SepSkipListReader _skipper;
            private bool _payloadPending;
            private bool _posSeekPending;

            internal SepDocsAndPositionsEnum(SepPostingsReader outerInstance)
            {
                _outerInstance = outerInstance;

                startDocIn = outerInstance._docIn;
                _docReader = outerInstance._docIn.GetReader();
                _docIndex = outerInstance._docIn.GetIndex();
                _freqReader = outerInstance._freqIn.GetReader();
                _freqIndex = outerInstance._freqIn.GetIndex();
                _posReader = outerInstance._posIn.GetReader();
                _posIndex = outerInstance._posIn.GetIndex();
                _payloadIn = (IndexInput) outerInstance._payloadIn.Clone();
            }

            internal virtual SepDocsAndPositionsEnum Init(FieldInfo fieldInfo, SepTermState termState, IBits liveDocs)
            {
                _liveDocs = liveDocs;
                _storePayloads = fieldInfo.HasPayloads;
                //System.out.println("Sep D&P init");

                // TODO: can't we only do this if consumer
                // skipped consuming the previous docs?
                _docIndex.CopyFrom(termState.docIndex);
                _docIndex.Seek(_docReader);
                //System.out.println("  docIndex=" + docIndex);

                _freqIndex.CopyFrom(termState.freqIndex);
                _freqIndex.Seek(_freqReader);
                //System.out.println("  freqIndex=" + freqIndex);

                _posIndex.CopyFrom(termState.posIndex);
                //System.out.println("  posIndex=" + posIndex);
                _posSeekPending = true;
                _payloadPending = false;

                _payloadFp = termState.payloadFP;
                _skipFp = termState.skipFP;
                //System.out.println("  skipFP=" + skipFP);

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
                    {
                        return _doc = NO_MORE_DOCS;
                    }

                    _count++;

                    // TODO: maybe we should do the 1-bit trick for encoding
                    // freq=1 case?

                    // Decode next doc
                    //System.out.println("  sep d&p read doc");
                    _accum += _docReader.Next();

                    //System.out.println("  sep d&p read freq");
                    _freq = _freqReader.Next();

                    _pendingPosCount += _freq;

                    if (_liveDocs == null || _liveDocs.Get(_accum))
                    {
                        break;
                    }
                }

                _position = 0;
                return (_doc = _accum);
            }

            public override int Freq
            {
                get { return _freq; }
            }

            public override int DocID
            {
                get { return _doc; }
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
                            _outerInstance._docIn, 
                            _outerInstance._posIn, 
                            _outerInstance._maxSkipLevels,
                            _outerInstance._skipInterval);
                    }

                    if (!_skipped)
                    {
                        //System.out.println("  init skip data skipFP=" + skipFP);
                        // We haven't yet skipped for this posting
                        _skipper.Init(_skipFp, _docIndex, _freqIndex, _posIndex, _payloadFp, _docFreq, _storePayloads);
                        _skipper.SetIndexOptions(IndexOptions.DOCS_AND_FREQS_AND_POSITIONS);
                        _skipped = true;
                    }

                    int newCount = _skipper.SkipTo(target);
                    //System.out.println("  skip newCount=" + newCount + " vs " + count);

                    if (newCount > _count)
                    {
                        // Skipper did move
                        _skipper.FreqIndex.Seek(_freqReader);
                        _skipper.DocIndex.Seek(_docReader);
                        //System.out.println("  doc seek'd to " + skipper.getDocIndex());
                        // NOTE: don't seek pos here; do it lazily
                        // instead.  Eg a PhraseQuery may skip to many
                        // docs before finally asking for positions...
                        _posIndex.CopyFrom(_skipper.PosIndex);
                        _posSeekPending = true;
                        _count = newCount;
                        _doc = _accum = _skipper.Doc;
                        //System.out.println("    moved to doc=" + doc);
                        //payloadIn.seek(skipper.getPayloadPointer());
                        _payloadFp = _skipper.PayloadPointer;
                        _pendingPosCount = 0;
                        _pendingPayloadBytes = 0;
                        _payloadPending = false;
                        _payloadLength = _skipper.PayloadLength;
                        //System.out.println("    move payloadLen=" + payloadLength);
                    }
                }

                // Now, linear scan for the rest:
                do
                {
                    if (NextDoc() == NO_MORE_DOCS)
                    {
                        //System.out.println("  advance nextDoc=END");
                        return NO_MORE_DOCS;
                    }
                    //System.out.println("  advance nextDoc=" + doc);
                } while (target > _doc);

                //System.out.println("  return doc=" + doc);
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

            private BytesRef _payload;

            public override BytesRef GetPayload()
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
                    _payloadIn.Seek(_payloadIn.GetFilePointer() + (_pendingPayloadBytes - _payloadLength));
                }

                if (_payload == null)
                {
                    _payload = new BytesRef { Bytes = new byte[_payloadLength] };
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

            public override long GetCost()
            {
                return _docFreq;
            }
        }

        public override long RamBytesUsed()
        {
            return 0;
        }

        public override void CheckIntegrity()
        {
            // TODO: remove sep layout, its fallen behind on features...
        }
    }
}