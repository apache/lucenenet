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

namespace Lucene.Net.Codecs.Pulsing
{

    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using Index;
    using Store;
    using Util;

    /// <summary>
    /// Concrete class that reads the current doc/freq/skip postings format 
    /// 
    /// @lucene.experimental
    /// 
    /// TODO: -- should we switch "hasProx" higher up?  and
    /// create two separate docs readers, one that also reads
    /// prox and one that doesn't?
    /// </summary>
    public class PulsingPostingsReader : PostingsReaderBase
    {

        // Fallback reader for non-pulsed terms:
        private readonly PostingsReaderBase _wrappedPostingsReader;
        private readonly SegmentReadState _segmentState;
        private int _maxPositions;
        private int _version;
        private SortedDictionary<int, int> _fields;

        public PulsingPostingsReader(SegmentReadState state, PostingsReaderBase wrappedPostingsReader)
        {
            _wrappedPostingsReader = wrappedPostingsReader;
            _segmentState = state;
        }

        public override void Init(IndexInput termsIn)
        {
            _version = CodecUtil.CheckHeader(termsIn, PulsingPostingsWriter.CODEC,
                PulsingPostingsWriter.VERSION_START,
                PulsingPostingsWriter.VERSION_CURRENT);

            _maxPositions = termsIn.ReadVInt();
            _wrappedPostingsReader.Init(termsIn);

            if (_wrappedPostingsReader is PulsingPostingsReader || _version < PulsingPostingsWriter.VERSION_META_ARRAY)
            {
                _fields = null;
            }
            else
            {
                _fields = new SortedDictionary<int, int>();
                var summaryFileName = IndexFileNames.SegmentFileName(_segmentState.SegmentInfo.Name,
                    _segmentState.SegmentSuffix, PulsingPostingsWriter.SUMMARY_EXTENSION);
                IndexInput input = null;

                try
                {
                    input =
                        _segmentState.Directory.OpenInput(summaryFileName, _segmentState.Context);
                    CodecUtil.CheckHeader(input,
                        PulsingPostingsWriter.CODEC,
                        _version,
                        PulsingPostingsWriter.VERSION_CURRENT);

                    var numField = input.ReadVInt();
                    for (var i = 0; i < numField; i++)
                    {
                        var fieldNum = input.ReadVInt();
                        var longsSize = input.ReadVInt();
                        _fields.Add(fieldNum, longsSize);
                    }
                }
                finally
                {
                    IOUtils.CloseWhileHandlingException(input);
                }
            }
        }

        public override BlockTermState NewTermState()
        {
            return new PulsingTermState {WrappedTermState = _wrappedPostingsReader.NewTermState()};
        }

        public override void DecodeTerm(long[] empty, DataInput input, FieldInfo fieldInfo, BlockTermState _termState,
            bool absolute)
        {
            var termState = (PulsingTermState) _termState;

            Debug.Assert(empty.Length == 0);

            termState.Absolute = termState.Absolute || absolute;
            // if we have positions, its total TF, otherwise its computed based on docFreq.
            // TODO Double check this is right..
            long count = IndexOptions.DOCS_AND_FREQS_AND_POSITIONS.CompareTo(fieldInfo.IndexOptions) <= 0
                ? termState.TotalTermFreq
                : termState.DocFreq;
           
            if (count <= _maxPositions)
            {
                // Inlined into terms dict -- just read the byte[] blob in,
                // but don't decode it now (we only decode when a DocsEnum
                // or D&PEnum is pulled):
                termState.PostingsSize = input.ReadVInt();
                if (termState.Postings == null || termState.Postings.Length < termState.PostingsSize)
                {
                    termState.Postings = new byte[ArrayUtil.Oversize(termState.PostingsSize, 1)];
                }
                // TODO: sort of silly to copy from one big byte[]
                // (the blob holding all inlined terms' blobs for
                // current term block) into another byte[] (just the
                // blob for this term)...
                input.ReadBytes(termState.Postings, 0, termState.PostingsSize);
                //System.out.println("  inlined bytes=" + termState.postingsSize);
                termState.Absolute = termState.Absolute || absolute;
            }
            else
            {
                var longsSize = _fields == null ? 0 : _fields[fieldInfo.Number];
                if (termState.Longs == null)
                {
                    termState.Longs = new long[longsSize];
                }
                for (var i = 0; i < longsSize; i++)
                {
                    termState.Longs[i] = input.ReadVLong();
                }
                termState.PostingsSize = -1;
                termState.WrappedTermState.DocFreq = termState.DocFreq;
                termState.WrappedTermState.TotalTermFreq = termState.TotalTermFreq;
                _wrappedPostingsReader.DecodeTerm(termState.Longs, input, fieldInfo,
                    termState.WrappedTermState,
                    termState.Absolute);
                termState.Absolute = false;
            }
        }

        public override DocsEnum Docs(FieldInfo field, BlockTermState _termState, IBits liveDocs, DocsEnum reuse,
            int flags)
        {
            var termState = (PulsingTermState) _termState;
            if (termState.PostingsSize != -1)
            {
                PulsingDocsEnum postings;
                if (reuse is PulsingDocsEnum)
                {
                    postings = (PulsingDocsEnum) reuse;
                    if (!postings.CanReuse(field))
                    {
                        postings = new PulsingDocsEnum(field);
                    }
                }
                else
                {
                    // the 'reuse' is actually the wrapped enum
                    var previous = (PulsingDocsEnum) GetOther(reuse);
                    if (previous != null && previous.CanReuse(field))
                    {
                        postings = previous;
                    }
                    else
                    {
                        postings = new PulsingDocsEnum(field);
                    }
                }
                
                if (reuse != postings)
                    SetOther(postings, reuse); // postings.other = reuse
                
                return postings.Reset(liveDocs, termState);
            }

            if (!(reuse is PulsingDocsEnum))
                return _wrappedPostingsReader.Docs(field, termState.WrappedTermState, liveDocs, reuse, flags);

            var wrapped = _wrappedPostingsReader.Docs(field, termState.WrappedTermState, liveDocs,
                GetOther(reuse), flags);

            SetOther(wrapped, reuse); // wrapped.other = reuse
            return wrapped;
        }

        public override DocsAndPositionsEnum DocsAndPositions(FieldInfo field, BlockTermState _termState, IBits liveDocs,
            DocsAndPositionsEnum reuse,
            int flags)
        {

            var termState = (PulsingTermState) _termState;

            if (termState.PostingsSize != -1)
            {
                PulsingDocsAndPositionsEnum postings;
                if (reuse is PulsingDocsAndPositionsEnum)
                {
                    postings = (PulsingDocsAndPositionsEnum) reuse;
                    if (!postings.CanReuse(field))
                    {
                        postings = new PulsingDocsAndPositionsEnum(field);
                    }
                }
                else
                {
                    // the 'reuse' is actually the wrapped enum
                    var previous = (PulsingDocsAndPositionsEnum) GetOther(reuse);
                    if (previous != null && previous.CanReuse(field))
                    {
                        postings = previous;
                    }
                    else
                    {
                        postings = new PulsingDocsAndPositionsEnum(field);
                    }
                }
                if (reuse != postings)
                {
                    SetOther(postings, reuse); // postings.other = reuse 
                }
                return postings.Reset(liveDocs, termState);
            }

            if (!(reuse is PulsingDocsAndPositionsEnum))
                return _wrappedPostingsReader.DocsAndPositions(field, termState.WrappedTermState, liveDocs, reuse,
                    flags);

            var wrapped = _wrappedPostingsReader.DocsAndPositions(field,
                termState.WrappedTermState,
                liveDocs, (DocsAndPositionsEnum) GetOther(reuse),
                flags);
            SetOther(wrapped, reuse); // wrapped.other = reuse
            return wrapped;
        }

        public override long RamBytesUsed()
        {
            return ((_wrappedPostingsReader != null) ? _wrappedPostingsReader.RamBytesUsed() : 0);
        }

        public override void CheckIntegrity()
        {
            _wrappedPostingsReader.CheckIntegrity();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _wrappedPostingsReader.Dispose();
            }
        }
        
        /// <summary>
        /// for a docsenum, gets the 'other' reused enum.
        /// Example: Pulsing(Standard).
        /// when doing a term range query you are switching back and forth
        /// between Pulsing and Standard
        ///  
        /// The way the reuse works is that Pulsing.other = Standard and
        /// Standard.other = Pulsing.
        /// </summary>
        private DocsEnum GetOther(DocsEnum de)
        {
            if (de == null)
                return null;
            
            var atts = de.Attributes;
            DocsEnum result;
            atts.AddAttribute<IPulsingEnumAttribute>().Enums().TryGetValue(this, out result);
            return result;
        }

        /// <summary>
        /// for a docsenum, sets the 'other' reused enum.
        /// see GetOther for an example.
        /// </summary>
        private DocsEnum SetOther(DocsEnum de, DocsEnum other)
        {
            var atts = de.Attributes;
            return atts.AddAttribute<IPulsingEnumAttribute>().Enums()[this] = other;
        }

        ///<summary>
        /// A per-docsenum attribute that stores additional reuse information
        /// so that pulsing enums can keep a reference to their wrapped enums,
        /// and vice versa. this way we can always reuse.
        /// 
        /// @lucene.internal 
        /// </summary>
        public interface IPulsingEnumAttribute : IAttribute
        {
            Dictionary<PulsingPostingsReader, DocsEnum> Enums(); // LUCENENET TODO: Make property, change to IDictionary
        }

        internal class PulsingTermState : BlockTermState
        {
            public bool Absolute { get; set; }
            public long[] Longs { get; set; }
            public byte[] Postings { get; set; }
            public int PostingsSize { get; set; } // -1 if this term was not inlined
            public BlockTermState WrappedTermState { get; set; }

            public override object Clone()
            {
                var clone = (PulsingTermState) base.Clone();
                if (PostingsSize != -1)
                {
                    clone.Postings = new byte[PostingsSize];
                    Array.Copy(Postings, 0, clone.Postings, 0, PostingsSize);
                }
                else
                {
                    Debug.Assert(WrappedTermState != null);
                    clone.WrappedTermState = (BlockTermState) WrappedTermState.Clone();
                    clone.Absolute = Absolute;
                    
                    if (Longs == null) return clone;

                    clone.Longs = new long[Longs.Length];
                    Array.Copy(Longs, 0, clone.Longs, 0, Longs.Length);
                }
                return clone;
            }

            public override void CopyFrom(TermState other)
            {
                base.CopyFrom(other);
                var _other = (PulsingTermState) other;
                PostingsSize = _other.PostingsSize;
                if (_other.PostingsSize != -1)
                {
                    if (Postings == null || Postings.Length < _other.PostingsSize)
                    {
                        Postings = new byte[ArrayUtil.Oversize(_other.PostingsSize, 1)];
                    }
                    Array.Copy(_other.Postings, 0, Postings, 0, _other.PostingsSize);
                }
                else
                {
                    WrappedTermState.CopyFrom(_other.WrappedTermState);
                }
            }

            public override String ToString()
            {
                if (PostingsSize == -1)
                    return "PulsingTermState: not inlined: wrapped=" + WrappedTermState;
                
                return "PulsingTermState: inlined size=" + PostingsSize + " " + base.ToString();
            }
        }

        internal class PulsingDocsEnum : DocsEnum
        {
            private byte[] _postingsBytes;
            private readonly ByteArrayDataInput _postings = new ByteArrayDataInput();
            private readonly IndexOptions? _indexOptions;
            private readonly bool _storePayloads;
            private readonly bool _storeOffsets;
            private IBits _liveDocs;

            private int _docId = -1;
            private int _accum;
            private int _freq;
            private int _payloadLength;
            private int _cost;

            public PulsingDocsEnum(FieldInfo fieldInfo)
            {
                _indexOptions = fieldInfo.IndexOptions;
                _storePayloads = fieldInfo.HasPayloads;
                _storeOffsets = _indexOptions.Value.CompareTo(IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS) >= 0;
            }

            public PulsingDocsEnum Reset(IBits liveDocs, PulsingTermState termState)
            {
                Debug.Assert(termState.PostingsSize != -1);

                // Must make a copy of termState's byte[] so that if
                // app does TermsEnum.next(), this DocsEnum is not affected
                if (_postingsBytes == null)
                {
                    _postingsBytes = new byte[termState.PostingsSize];
                }
                else if (_postingsBytes.Length < termState.PostingsSize)
                {
                    _postingsBytes = ArrayUtil.Grow(_postingsBytes, termState.PostingsSize);
                }
                System.Array.Copy(termState.Postings, 0, _postingsBytes, 0, termState.PostingsSize);
                _postings.Reset(_postingsBytes, 0, termState.PostingsSize);
                _docId = -1;
                _accum = 0;
                _freq = 1;
                _cost = termState.DocFreq;
                _payloadLength = 0;
                this._liveDocs = liveDocs;
                return this;
            }

            public bool CanReuse(FieldInfo fieldInfo)
            {
                return _indexOptions == fieldInfo.IndexOptions && _storePayloads == fieldInfo.HasPayloads;
            }

            public override int DocID
            {
                get { return _docId; }
            }

            public override int NextDoc()
            {
                while (true)
                {
                    if (_postings.Eof)
                        return _docId = NO_MORE_DOCS;
                    
                    var code = _postings.ReadVInt();
                    if (_indexOptions == IndexOptions.DOCS_ONLY)
                    {
                        _accum += code;
                    }
                    else
                    {
                        _accum += (int)((uint)code >> 1); ; // shift off low bit
                        _freq = (code & 1) != 0 ? 1 : _postings.ReadVInt();

                        if (_indexOptions.Value.CompareTo(IndexOptions.DOCS_AND_FREQS_AND_POSITIONS) >= 0)
                        {
                            // Skip positions
                            if (_storePayloads)
                            {
                                for (var pos = 0; pos < _freq; pos++)
                                {
                                    var posCode = _postings.ReadVInt();
                                    if ((posCode & 1) != 0)
                                    {
                                        _payloadLength = _postings.ReadVInt();
                                    }
                                    if (_storeOffsets && (_postings.ReadVInt() & 1) != 0)
                                    {
                                        // new offset length
                                        _postings.ReadVInt();
                                    }
                                    if (_payloadLength != 0)
                                    {
                                        _postings.SkipBytes(_payloadLength);
                                    }
                                }
                            }
                            else
                            {
                                for (var pos = 0; pos < _freq; pos++)
                                {
                                    // TODO: skipVInt
                                    _postings.ReadVInt();
                                    if (_storeOffsets && (_postings.ReadVInt() & 1) != 0)
                                    {
                                        // new offset length
                                        _postings.ReadVInt();
                                    }
                                }
                            }
                        }
                    }

                    if (_liveDocs == null || _liveDocs.Get(_accum))
                        return (_docId = _accum);
                }
            }

            public override int Advance(int target)
            {
                return _docId = SlowAdvance(target);
            }

            public override long GetCost()
            {
                return _cost;
            }

            public override int Freq
            {
                get { return _freq; }
            }
        }

        internal class PulsingDocsAndPositionsEnum : DocsAndPositionsEnum
        {
            private byte[] _postingsBytes;
            private readonly ByteArrayDataInput _postings = new ByteArrayDataInput();
            private readonly bool _storePayloads;
            private readonly bool _storeOffsets;
            // note: we could actually reuse across different options, if we passed this to reset()
            // and re-init'ed storeOffsets accordingly (made it non-final)
            private readonly IndexOptions? _indexOptions;

            private IBits _liveDocs;
            private int _docId = -1;
            private int _accum;
            private int _freq;
            private int _posPending;
            private int _position;
            private int _payloadLength;
            private BytesRef _payload;
            private int _startOffset;
            private int _offsetLength;

            private bool _payloadRetrieved;
            private int _cost;

            public PulsingDocsAndPositionsEnum(FieldInfo fieldInfo)
            {
                _indexOptions = fieldInfo.IndexOptions;
                _storePayloads = fieldInfo.HasPayloads;
                _storeOffsets =
                    _indexOptions.Value.CompareTo(IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS) >= 0;
            }

            public PulsingDocsAndPositionsEnum Reset(IBits liveDocs, PulsingTermState termState)
            {
                Debug.Assert(termState.PostingsSize != -1);

                if (_postingsBytes == null)
                {
                    _postingsBytes = new byte[termState.PostingsSize];
                }
                else if (_postingsBytes.Length < termState.PostingsSize)
                {
                    _postingsBytes = ArrayUtil.Grow(_postingsBytes, termState.PostingsSize);
                }

                Array.Copy(termState.Postings, 0, _postingsBytes, 0, termState.PostingsSize);
                _postings.Reset(_postingsBytes, 0, termState.PostingsSize);
                this._liveDocs = liveDocs;
                _payloadLength = 0;
                _posPending = 0;
                _docId = -1;
                _accum = 0;
                _cost = termState.DocFreq;
                _startOffset = _storeOffsets ? 0 : -1; // always return -1 if no offsets are stored
                _offsetLength = 0;
                //System.out.println("PR d&p reset storesPayloads=" + storePayloads + " bytes=" + bytes.length + " this=" + this);
                return this;
            }

            public bool CanReuse(FieldInfo fieldInfo)
            {
                return _indexOptions == fieldInfo.IndexOptions && _storePayloads == fieldInfo.HasPayloads;
            }

            public override int NextDoc()
            {
                while (true)
                {
                    SkipPositions();

                    if (_postings.Eof)
                    {
                        return _docId = NO_MORE_DOCS;
                    }

                    var code = _postings.ReadVInt();
                    _accum += (int)((uint)code >> 1); // shift off low bit 
                    _freq = (code & 1) != 0 ? 1 : _postings.ReadVInt();
                    _posPending = _freq;
                    _startOffset = _storeOffsets ? 0 : -1; // always return -1 if no offsets are stored

                    if (_liveDocs != null && !_liveDocs.Get(_accum)) continue;

                    _position = 0;
                    return (_docId = _accum);
                }
            }

            public override int Freq
            {
                get { return _freq; }
            }

            public override int DocID
            {
                get { return _docId; }
            }

            public override int Advance(int target)
            {
                return _docId = SlowAdvance(target);
            }

            public override int NextPosition()
            {
                Debug.Assert(_posPending > 0);

                _posPending--;

                if (_storePayloads)
                {
                    if (!_payloadRetrieved)
                    {
                        _postings.SkipBytes(_payloadLength);
                    }
                    int code = _postings.ReadVInt();
                    if ((code & 1) != 0)
                    {
                        _payloadLength = _postings.ReadVInt();
                    }
                    _position += (int)((uint)code >> 1);
                    _payloadRetrieved = false;
                }
                else
                {
                    _position += _postings.ReadVInt();
                }

                if (_storeOffsets)
                {
                    int offsetCode = _postings.ReadVInt();
                    if ((offsetCode & 1) != 0)
                    {
                        // new offset length
                        _offsetLength = _postings.ReadVInt();
                    }
                    _startOffset += (int)((uint)offsetCode >> 1);
                }

                return _position;
            }

            public override int StartOffset
            {
                get { return _startOffset; }
            }

            public override int EndOffset
            {
                get { return _startOffset + _offsetLength; }
            }

            public override BytesRef Payload
            {
                get
                {
                    if (_payloadRetrieved)
                        return _payload;
                    
                    if (_storePayloads && _payloadLength > 0)
                    {
                        _payloadRetrieved = true;
                        if (_payload == null)
                        {
                            _payload = new BytesRef(_payloadLength);
                        }
                        else
                        {
                            _payload.Grow(_payloadLength);
                        }
                        _postings.ReadBytes(_payload.Bytes, 0, _payloadLength);
                        _payload.Length = _payloadLength;
                        return _payload;
                    }
                    
                    return null;
                }
            }

            private void SkipPositions()
            {
                while (_posPending != 0)
                {
                    NextPosition();
                }
                if (_storePayloads && !_payloadRetrieved)
                {
                    _postings.SkipBytes(_payloadLength);
                    _payloadRetrieved = true;
                }
            }
            
            public override long GetCost()
            {
                return _cost;
            }
        }
        
        /// <summary>
        /// Implementation of {@link PulsingEnumAttribute} for reuse of
        /// wrapped postings readers underneath pulsing.
        /// 
        /// @lucene.internal
        /// </summary>
        internal sealed class PulsingEnumAttribute : Util.Attribute, IPulsingEnumAttribute
        {
            // we could store 'other', but what if someone 'chained' multiple postings readers,
            // this could cause problems?
            // TODO: we should consider nuking this map and just making it so if you do this,
            // you don't reuse? and maybe pulsingPostingsReader should throw an exc if it wraps
            // another pulsing, because this is just stupid and wasteful. 
            // we still have to be careful in case someone does Pulsing(Stomping(Pulsing(...
            private readonly Dictionary<PulsingPostingsReader, DocsEnum> _enums = new Dictionary<PulsingPostingsReader, DocsEnum>();

            public Dictionary<PulsingPostingsReader, DocsEnum> Enums()
            {
                return _enums;
            }
            public override void Clear()
            {
                // our state is per-docsenum, so this makes no sense.
                // its best not to clear, in case a wrapped enum has a per-doc attribute or something
                // and is calling clearAttributes(), so they don't nuke the reuse information!
            }

            public override void CopyTo(Util.IAttribute target)
            {
                // this makes no sense for us, because our state is per-docsenum.
                // we don't want to copy any stuff over to another docsenum ever!
            }
        }
    }
}
