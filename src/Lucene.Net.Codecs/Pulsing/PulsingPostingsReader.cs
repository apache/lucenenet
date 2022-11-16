using J2N.Numerics;
using J2N.Runtime.CompilerServices;
using Lucene.Net.Diagnostics;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Support;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using JCG = J2N.Collections.Generic;

namespace Lucene.Net.Codecs.Pulsing
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
    /// Concrete class that reads the current doc/freq/skip postings format.
    /// <para/>
    /// @lucene.experimental
    /// </summary>
    // TODO: -- should we switch "hasProx" higher up?  and
    // create two separate docs readers, one that also reads
    // prox and one that doesn't?
    public class PulsingPostingsReader : PostingsReaderBase
    {
        // Fallback reader for non-pulsed terms:
        private readonly PostingsReaderBase _wrappedPostingsReader;
        private readonly SegmentReadState _segmentState;
        private int _maxPositions;
        private int _version;
        private IDictionary<int, int> _fields;

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

            _maxPositions = termsIn.ReadVInt32();
            _wrappedPostingsReader.Init(termsIn);

            if (_wrappedPostingsReader is PulsingPostingsReader || _version < PulsingPostingsWriter.VERSION_META_ARRAY)
            {
                _fields = null;
            }
            else
            {
                _fields = new JCG.SortedDictionary<int, int>();
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

                    var numField = input.ReadVInt32();
                    for (var i = 0; i < numField; i++)
                    {
                        var fieldNum = input.ReadVInt32();
                        var longsSize = input.ReadVInt32();
                        _fields.Add(fieldNum, longsSize);
                    }
                }
                finally
                {
                    IOUtils.DisposeWhileHandlingException(input);
                }
            }
        }

        internal class PulsingTermState : BlockTermState
        {
            internal bool Absolute { get; set; }

            /// <summary>
            /// NOTE: This was longs (field) in Lucene
            /// </summary>
            [WritableArray]
            [SuppressMessage("Microsoft.Performance", "CA1819", Justification = "Lucene's design requires some writable array properties")]
            internal long[] Int64s { get; set; }
            [WritableArray]
            [SuppressMessage("Microsoft.Performance", "CA1819", Justification = "Lucene's design requires some writable array properties")]
            internal byte[] Postings { get; set; }
            internal int PostingsSize { get; set; } // -1 if this term was not inlined
            internal BlockTermState WrappedTermState { get; set; }

            public override object Clone()
            {
                var clone = (PulsingTermState)base.Clone();
                if (PostingsSize != -1)
                {
                    clone.Postings = new byte[PostingsSize];
                    Arrays.Copy(Postings, 0, clone.Postings, 0, PostingsSize);
                }
                else
                {
                    if (Debugging.AssertsEnabled) Debugging.Assert(WrappedTermState != null);
                    clone.WrappedTermState = (BlockTermState)WrappedTermState.Clone();
                    clone.Absolute = Absolute;

                    if (Int64s is null) return clone;

                    clone.Int64s = new long[Int64s.Length];
                    Arrays.Copy(Int64s, 0, clone.Int64s, 0, Int64s.Length);
                }
                return clone;
            }

            public override void CopyFrom(TermState other)
            {
                base.CopyFrom(other);
                var _other = (PulsingTermState)other;
                PostingsSize = _other.PostingsSize;
                if (_other.PostingsSize != -1)
                {
                    if (Postings is null || Postings.Length < _other.PostingsSize)
                    {
                        Postings = new byte[ArrayUtil.Oversize(_other.PostingsSize, 1)];
                    }
                    Arrays.Copy(_other.Postings, 0, Postings, 0, _other.PostingsSize);
                }
                else
                {
                    WrappedTermState.CopyFrom(_other.WrappedTermState);
                }
            }

            public override string ToString()
            {
                if (PostingsSize == -1)
                    return "PulsingTermState: not inlined: wrapped=" + WrappedTermState;

                return "PulsingTermState: inlined size=" + PostingsSize + " " + base.ToString();
            }
        }

        public override BlockTermState NewTermState()
        {
            return new PulsingTermState {WrappedTermState = _wrappedPostingsReader.NewTermState()};
        }

        public override void DecodeTerm(long[] empty, DataInput input, FieldInfo fieldInfo, BlockTermState termState,
            bool absolute)
        {
            var termState2 = (PulsingTermState) termState;

            if (Debugging.AssertsEnabled) Debugging.Assert(empty.Length == 0);

            termState2.Absolute = termState2.Absolute || absolute;
            // if we have positions, its total TF, otherwise its computed based on docFreq.
            // TODO Double check this is right..
            // LUCENENET specific - to avoid boxing, changed from CompareTo() to IndexOptionsComparer.Compare()
            long count = IndexOptionsComparer.Default.Compare(fieldInfo.IndexOptions, IndexOptions.DOCS_AND_FREQS_AND_POSITIONS) >= 0
                ? termState2.TotalTermFreq
                : termState2.DocFreq;
           
            if (count <= _maxPositions)
            {
                // Inlined into terms dict -- just read the byte[] blob in,
                // but don't decode it now (we only decode when a DocsEnum
                // or D&PEnum is pulled):
                termState2.PostingsSize = input.ReadVInt32();
                if (termState2.Postings is null || termState2.Postings.Length < termState2.PostingsSize)
                {
                    termState2.Postings = new byte[ArrayUtil.Oversize(termState2.PostingsSize, 1)];
                }
                // TODO: sort of silly to copy from one big byte[]
                // (the blob holding all inlined terms' blobs for
                // current term block) into another byte[] (just the
                // blob for this term)...
                input.ReadBytes(termState2.Postings, 0, termState2.PostingsSize);
                //System.out.println("  inlined bytes=" + termState.postingsSize);
                termState2.Absolute = termState2.Absolute || absolute;
            }
            else
            {
                //System.out.println("  not inlined");
                var longsSize = _fields is null ? 0 : _fields[fieldInfo.Number];
                if (termState2.Int64s is null)
                {
                    termState2.Int64s = new long[longsSize];
                }
                for (var i = 0; i < longsSize; i++)
                {
                    termState2.Int64s[i] = input.ReadVInt64();
                }
                termState2.PostingsSize = -1;
                termState2.WrappedTermState.DocFreq = termState2.DocFreq;
                termState2.WrappedTermState.TotalTermFreq = termState2.TotalTermFreq;
                _wrappedPostingsReader.DecodeTerm(termState2.Int64s, input, fieldInfo,
                    termState2.WrappedTermState,
                    termState2.Absolute);
                termState2.Absolute = false;
            }
        }

        public override DocsEnum Docs(FieldInfo field, BlockTermState termState, IBits liveDocs, DocsEnum reuse,
            DocsFlags flags)
        {
            var termState2 = (PulsingTermState) termState;
            if (termState2.PostingsSize != -1)
            {
                if (reuse is PulsingDocsEnum postings)
                {
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
                
                return postings.Reset(liveDocs, termState2);
            }

            if (!(reuse is PulsingDocsEnum))
                return _wrappedPostingsReader.Docs(field, termState2.WrappedTermState, liveDocs, reuse, flags);

            var wrapped = _wrappedPostingsReader.Docs(field, termState2.WrappedTermState, liveDocs,
                GetOther(reuse), flags);

            SetOther(wrapped, reuse); // wrapped.other = reuse
            return wrapped;
        }

        public override DocsAndPositionsEnum DocsAndPositions(FieldInfo field, BlockTermState termState, IBits liveDocs,
            DocsAndPositionsEnum reuse,
            DocsAndPositionsFlags flags)
        {

            var termState2 = (PulsingTermState) termState;

            if (termState2.PostingsSize != -1)
            {
                if (reuse is PulsingDocsAndPositionsEnum postings)
                {
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
                return postings.Reset(liveDocs, termState2);
            }

            if (!(reuse is PulsingDocsAndPositionsEnum))
                return _wrappedPostingsReader.DocsAndPositions(field, termState2.WrappedTermState, liveDocs, reuse,
                    flags);

            var wrapped = _wrappedPostingsReader.DocsAndPositions(field,
                termState2.WrappedTermState,
                liveDocs, (DocsAndPositionsEnum) GetOther(reuse),
                flags);
            SetOther(wrapped, reuse); // wrapped.other = reuse
            return wrapped;
        }

        private class PulsingDocsEnum : DocsEnum
        {
            private byte[] _postingsBytes;
            private readonly ByteArrayDataInput _postings = new ByteArrayDataInput();
            private readonly IndexOptions _indexOptions;
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
                // LUCENENET specific - to avoid boxing, changed from CompareTo() to IndexOptionsComparer.Compare()
                _storeOffsets = IndexOptionsComparer.Default.Compare(_indexOptions, IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS) >= 0;
            }

            public virtual PulsingDocsEnum Reset(IBits liveDocs, PulsingTermState termState)
            {
                if (Debugging.AssertsEnabled) Debugging.Assert(termState.PostingsSize != -1);

                // Must make a copy of termState's byte[] so that if
                // app does TermsEnum.next(), this DocsEnum is not affected
                if (_postingsBytes is null)
                {
                    _postingsBytes = new byte[termState.PostingsSize];
                }
                else if (_postingsBytes.Length < termState.PostingsSize)
                {
                    _postingsBytes = ArrayUtil.Grow(_postingsBytes, termState.PostingsSize);
                }
                Arrays.Copy(termState.Postings, 0, _postingsBytes, 0, termState.PostingsSize);
                _postings.Reset(_postingsBytes, 0, termState.PostingsSize);
                _docId = -1;
                _accum = 0;
                _freq = 1;
                _cost = termState.DocFreq;
                _payloadLength = 0;
                this._liveDocs = liveDocs;
                return this;
            }

            internal bool CanReuse(FieldInfo fieldInfo)
            {
                return _indexOptions == fieldInfo.IndexOptions && _storePayloads == fieldInfo.HasPayloads;
            }

            public override int NextDoc()
            {
                while (true)
                {
                    if (_postings.Eof)
                        return _docId = NO_MORE_DOCS;

                    var code = _postings.ReadVInt32();
                    if (_indexOptions == IndexOptions.DOCS_ONLY)
                    {
                        _accum += code;
                    }
                    else
                    {
                        _accum += code.TripleShift(1); // shift off low bit
                        _freq = (code & 1) != 0 ? 1 : _postings.ReadVInt32();

                        // LUCENENET specific - to avoid boxing, changed from CompareTo() to IndexOptionsComparer.Compare()
                        if (IndexOptionsComparer.Default.Compare(_indexOptions, IndexOptions.DOCS_AND_FREQS_AND_POSITIONS) >= 0)
                        {
                            // Skip positions
                            if (_storePayloads)
                            {
                                for (var pos = 0; pos < _freq; pos++)
                                {
                                    var posCode = _postings.ReadVInt32();
                                    if ((posCode & 1) != 0)
                                    {
                                        _payloadLength = _postings.ReadVInt32();
                                    }
                                    if (_storeOffsets && (_postings.ReadVInt32() & 1) != 0)
                                    {
                                        // new offset length
                                        _postings.ReadVInt32();
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
                                    _postings.ReadVInt32();
                                    if (_storeOffsets && (_postings.ReadVInt32() & 1) != 0)
                                    {
                                        // new offset length
                                        _postings.ReadVInt32();
                                    }
                                }
                            }
                        }
                    }

                    if (_liveDocs is null || _liveDocs.Get(_accum))
                        return (_docId = _accum);
                }
            }

            public override int Freq => _freq;

            public override int DocID => _docId;

            public override int Advance(int target)
            {
                return _docId = SlowAdvance(target);
            }

            public override long GetCost()
            {
                return _cost;
            }
        }

        private class PulsingDocsAndPositionsEnum : DocsAndPositionsEnum
        {
            private byte[] _postingsBytes;
            private readonly ByteArrayDataInput _postings = new ByteArrayDataInput();
            private readonly bool _storePayloads;
            private readonly bool _storeOffsets;
            // note: we could actually reuse across different options, if we passed this to reset()
            // and re-init'ed storeOffsets accordingly (made it non-final)
            private readonly IndexOptions _indexOptions;

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
                // LUCENENET specific - to avoid boxing, changed from CompareTo() to IndexOptionsComparer.Compare()
                _storeOffsets = IndexOptionsComparer.Default.Compare(_indexOptions, IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS) >= 0;
            }

            internal bool CanReuse(FieldInfo fieldInfo)
            {
                return _indexOptions == fieldInfo.IndexOptions && _storePayloads == fieldInfo.HasPayloads;
            }

            public virtual PulsingDocsAndPositionsEnum Reset(IBits liveDocs, PulsingTermState termState)
            {
                if (Debugging.AssertsEnabled) Debugging.Assert(termState.PostingsSize != -1);

                if (_postingsBytes is null)
                {
                    _postingsBytes = new byte[termState.PostingsSize];
                }
                else if (_postingsBytes.Length < termState.PostingsSize)
                {
                    _postingsBytes = ArrayUtil.Grow(_postingsBytes, termState.PostingsSize);
                }

                Arrays.Copy(termState.Postings, 0, _postingsBytes, 0, termState.PostingsSize);
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

            public override int NextDoc()
            {
                while (true)
                {
                    SkipPositions();

                    if (_postings.Eof)
                    {
                        return _docId = NO_MORE_DOCS;
                    }

                    var code = _postings.ReadVInt32();
                    _accum += code.TripleShift(1); // shift off low bit 
                    _freq = (code & 1) != 0 ? 1 : _postings.ReadVInt32();
                    _posPending = _freq;
                    _startOffset = _storeOffsets ? 0 : -1; // always return -1 if no offsets are stored

                    if (_liveDocs != null && !_liveDocs.Get(_accum)) continue;

                    _position = 0;
                    return (_docId = _accum);
                }
            }

            public override int Freq => _freq;

            public override int DocID => _docId;

            public override int Advance(int target)
            {
                return _docId = SlowAdvance(target);
            }

            public override int NextPosition()
            {
                if (Debugging.AssertsEnabled) Debugging.Assert(_posPending > 0);

                _posPending--;

                if (_storePayloads)
                {
                    if (!_payloadRetrieved)
                    {
                        _postings.SkipBytes(_payloadLength);
                    }
                    int code = _postings.ReadVInt32();
                    if ((code & 1) != 0)
                    {
                        _payloadLength = _postings.ReadVInt32();
                    }
                    _position += code.TripleShift(1);
                    _payloadRetrieved = false;
                }
                else
                {
                    _position += _postings.ReadVInt32();
                }

                if (_storeOffsets)
                {
                    int offsetCode = _postings.ReadVInt32();
                    if ((offsetCode & 1) != 0)
                    {
                        // new offset length
                        _offsetLength = _postings.ReadVInt32();
                    }
                    _startOffset += offsetCode.TripleShift(1);
                }

                return _position;
            }

            public override int StartOffset => _startOffset;

            public override int EndOffset => _startOffset + _offsetLength;

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

            public override BytesRef GetPayload()
            {
                if (_payloadRetrieved)
                    return _payload;

                if (_storePayloads && _payloadLength > 0)
                {
                    _payloadRetrieved = true;
                    if (_payload is null)
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

            public override long GetCost()
            {
                return _cost;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _wrappedPostingsReader.Dispose();
            }
        }

        /// <summary>
        /// For a docsenum, gets the 'other' reused enum.
        /// Example: Pulsing(Standard).
        /// When doing a term range query you are switching back and forth
        /// between Pulsing and Standard.
        /// <para/>
        /// The way the reuse works is that Pulsing.other = Standard and
        /// Standard.other = Pulsing.
        /// </summary>
        private DocsEnum GetOther(DocsEnum de)
        {
            if (de is null)
                return null;

            var atts = de.Attributes;
            atts.AddAttribute<IPulsingEnumAttribute>().Enums.TryGetValue(this, out DocsEnum result);
            return result;
        }

        /// <summary>
        /// For a docsenum, sets the 'other' reused enum.
        /// see <see cref="GetOther(DocsEnum)"/> for an example.
        /// </summary>
        private DocsEnum SetOther(DocsEnum de, DocsEnum other)
        {
            var atts = de.Attributes;
            return atts.AddAttribute<IPulsingEnumAttribute>().Enums[this] = other;
        }

        ///<summary>
        /// A per-docsenum attribute that stores additional reuse information
        /// so that pulsing enums can keep a reference to their wrapped enums,
        /// and vice versa. this way we can always reuse.
        /// <para/>
        /// @lucene.internal 
        /// </summary>
        public interface IPulsingEnumAttribute : IAttribute
        {
            IDictionary<PulsingPostingsReader, DocsEnum> Enums { get; }
        }

        /// <summary>
        /// Implementation of <see cref="PulsingEnumAttribute"/> for reuse of
        /// wrapped postings readers underneath pulsing.
        /// <para/>
        /// @lucene.internal
        /// </summary>
        public sealed class PulsingEnumAttribute : Util.Attribute, IPulsingEnumAttribute
        {
            // we could store 'other', but what if someone 'chained' multiple postings readers,
            // this could cause problems?
            // TODO: we should consider nuking this map and just making it so if you do this,
            // you don't reuse? and maybe pulsingPostingsReader should throw an exc if it wraps
            // another pulsing, because this is just stupid and wasteful. 
            // we still have to be careful in case someone does Pulsing(Stomping(Pulsing(...
            private readonly IDictionary<PulsingPostingsReader, DocsEnum> _enums = new JCG.Dictionary<PulsingPostingsReader, DocsEnum>(IdentityEqualityComparer<PulsingPostingsReader>.Default);

            public IDictionary<PulsingPostingsReader, DocsEnum> Enums => _enums;

            public override void Clear()
            {
                // our state is per-docsenum, so this makes no sense.
                // its best not to clear, in case a wrapped enum has a per-doc attribute or something
                // and is calling clearAttributes(), so they don't nuke the reuse information!
            }

            public override void CopyTo(IAttribute target) // LUCENENET specific - intentionally expanding target to use IAttribute rather than Attribute
            {
                // this makes no sense for us, because our state is per-docsenum.
                // we don't want to copy any stuff over to another docsenum ever!
            }
        }

        public override long RamBytesUsed()
        {
            return ((_wrappedPostingsReader != null) ? _wrappedPostingsReader.RamBytesUsed() : 0);
        }

        public override void CheckIntegrity()
        {
            _wrappedPostingsReader.CheckIntegrity();
        }  
    }
}
