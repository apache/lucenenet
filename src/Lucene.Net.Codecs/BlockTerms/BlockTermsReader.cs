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

namespace Lucene.Net.Codecs.BlockTerms
{

    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using Index;
    using Store;
    using Util;
    
    /// <summary>
    /// Handles a terms dict, but decouples all details of
    /// doc/freqs/positions reading to an instance of {@link
    /// PostingsReaderBase}.  This class is reusable for
    /// codecs that use a different format for
    /// docs/freqs/positions (though codecs are also free to
    /// make their own terms dict impl).
    ///
    /// This class also interacts with an instance of {@link
    /// TermsIndexReaderBase}, to abstract away the specific
    /// implementation of the terms dict index. 
    /// 
    /// @lucene.experimental
    /// </summary>
    public class BlockTermsReader : FieldsProducer
    {

        // Open input to the main terms dict file (_X.tis)
        private readonly IndexInput _input;

        // Reads the terms dict entries, to gather state to
        // produce DocsEnum on demand
        private readonly PostingsReaderBase _postingsReader;

        // Reads the terms index
        private TermsIndexReaderBase _indexReader;

        private readonly Dictionary<string, FieldReader> _fields = new Dictionary<string, FieldReader>();

        // keeps the dirStart offset
        private long _dirOffset;

        private readonly int _version;

        public BlockTermsReader(TermsIndexReaderBase indexReader, Directory dir, FieldInfos fieldInfos, SegmentInfo info,
            PostingsReaderBase postingsReader, IOContext context,
            String segmentSuffix)
        {
            _postingsReader = postingsReader;

            _input =
                dir.OpenInput(
                    IndexFileNames.SegmentFileName(info.Name, segmentSuffix, BlockTermsWriter.TERMS_EXTENSION),
                    context);

            var success = false;
            try
            {
                _version = ReadHeader(_input);

                // Have PostingsReader init itself
                postingsReader.Init(_input);

                // Read per-field details
                SeekDir(_input, _dirOffset);

                int numFields = _input.ReadVInt();
                if (numFields < 0)
                {
                    throw new CorruptIndexException(String.Format("Invalid number of fields: {0}, Resource: {1}",
                        numFields, _input));
                }

                for (var i = 0; i < numFields; i++)
                {
                    var field = _input.ReadVInt();
                    var numTerms = _input.ReadVLong();

                    Debug.Assert(numTerms >= 0);

                    var termsStartPointer = _input.ReadVLong();
                    var fieldInfo = fieldInfos.FieldInfo(field);
                    var sumTotalTermFreq = fieldInfo.IndexOptions == IndexOptions.DOCS_ONLY
                        ? -1
                        : _input.ReadVLong();
                    var sumDocFreq = _input.ReadVLong();
                    var docCount = _input.ReadVInt();
                    var longsSize = _version >= BlockTermsWriter.VERSION_META_ARRAY ? _input.ReadVInt() : 0;

                    if (docCount < 0 || docCount > info.DocCount)
                    {
                        // #docs with field must be <= #docs
                        throw new CorruptIndexException(
                            String.Format("Invalid DocCount: {0}, MaxDoc: {1}, Resource: {2}", docCount, info.DocCount,
                                _input));
                    }

                    if (sumDocFreq < docCount)
                    {
                        // #postings must be >= #docs with field
                        throw new CorruptIndexException(
                            String.Format("Invalid sumDocFreq: {0}, DocCount: {1}, Resource: {2}", sumDocFreq, docCount,
                                _input));
                    }

                    if (sumTotalTermFreq != -1 && sumTotalTermFreq < sumDocFreq)
                    {
                        // #positions must be >= #postings
                        throw new CorruptIndexException(
                            String.Format("Invalid sumTotalTermFreq: {0}, sumDocFreq: {1}, Resource: {2}",
                                sumTotalTermFreq, sumDocFreq, _input));
                    }

                    try
                    {
                        _fields.Add(fieldInfo.Name,
                            new FieldReader(fieldInfo, this, numTerms, termsStartPointer, sumTotalTermFreq, sumDocFreq,
                                docCount,
                                longsSize));
                    }
                    catch (ArgumentException)
                    {
                        throw new CorruptIndexException(String.Format("Duplicate fields: {0}, Resource: {1}",
                            fieldInfo.Name, _input));
                    }

                }
                success = true;
            }
            finally
            {
                if (!success)
                {
                    _input.Dispose();
                }
            }

            _indexReader = indexReader;
        }

        private int ReadHeader(DataInput input)
        {
            var version = CodecUtil.CheckHeader(input, BlockTermsWriter.CODEC_NAME,
                BlockTermsWriter.VERSION_START,
                BlockTermsWriter.VERSION_CURRENT);

            if (version < BlockTermsWriter.VERSION_APPEND_ONLY)
                _dirOffset = input.ReadLong();

            return version;
        }

        private void SeekDir(IndexInput input, long dirOffset)
        {
            if (_version >= BlockTermsWriter.VERSION_CHECKSUM)
            {
                input.Seek(input.Length() - CodecUtil.FooterLength() - 8);
                dirOffset = input.ReadLong();
            }
            else if (_version >= BlockTermsWriter.VERSION_APPEND_ONLY)
            {
                input.Seek(input.Length() - 8);
                dirOffset = input.ReadLong();
            }
            input.Seek(dirOffset);
        }

        public override void Dispose()
        {
            try
            {
                try
                {
                    if (_indexReader != null)
                        _indexReader.Dispose();
                }
                finally
                {
                    // null so if an app hangs on to us (ie, we are not
                    // GCable, despite being closed) we still free most
                    // ram
                    _indexReader = null;
                    if (_input != null)
                        _input.Dispose();
                }
            }
            finally
            {
                if (_postingsReader != null)
                    _postingsReader.Dispose();
            }
        }

        public override IEnumerator<string> GetEnumerator()
        {
            return _fields.Keys.GetEnumerator();
        }

        public override Terms Terms(String field)
        {
            Debug.Assert(field != null);

            return _fields[field];
        }

        public override long RamBytesUsed()
        {
            var sizeInBytes = (_postingsReader != null) ? _postingsReader.RamBytesUsed() : 0;
            sizeInBytes += (_indexReader != null) ? _indexReader.RamBytesUsed : 0;
            return sizeInBytes;
        }

        public override void CheckIntegrity()
        {
            // verify terms
            if (_version >= BlockTermsWriter.VERSION_CHECKSUM)
            {
                CodecUtil.ChecksumEntireFile(_input);
            }
            // verify postings
            _postingsReader.CheckIntegrity();
        }

        public override int Size
        {
            get
            {
                {
                    return _fields.Count;
                }
            }
        }

        /// <summary>
        /// Used as a key for the terms cache
        /// </summary>
        private class FieldAndTerm : DoubleBarrelLRUCache.CloneableKey
        {
            public String Field { get; set; }
            private BytesRef Term { get; set; }

            private FieldAndTerm(FieldAndTerm other)
            {
                Field = other.Field;
                Term = BytesRef.DeepCopyOf(other.Term);
            }

            public override bool Equals(Object other)
            {
                var o = (FieldAndTerm)other;
                return o.Field.Equals(Field) && Term.BytesEquals(o.Term);
            }

            public override DoubleBarrelLRUCache.CloneableKey Clone()
            {
                return new FieldAndTerm(this);
            }

            public override int GetHashCode()
            {
                return Field.GetHashCode() * 31 + Term.GetHashCode();
            }

            public FieldAndTerm()
            {

            }
        }

        private class FieldReader : Terms
        {
            private readonly BlockTermsReader _blockTermsReader;
            private readonly FieldInfo _fieldInfo;
            private readonly long _numTerms;
            private readonly long _termsStartPointer;
            private readonly long _sumTotalTermFreq;
            private readonly long _sumDocFreq;
            private readonly int _docCount;
            private readonly int _longsSize;

            public FieldReader(FieldInfo fieldInfo, BlockTermsReader blockTermsReader, long numTerms, long termsStartPointer, long sumTotalTermFreq,
                long sumDocFreq, int docCount, int longsSize)
            {
                Debug.Assert(numTerms > 0);

                _blockTermsReader = blockTermsReader;

                _fieldInfo = fieldInfo;
                _numTerms = numTerms;
                _termsStartPointer = termsStartPointer;
                _sumTotalTermFreq = sumTotalTermFreq;
                _sumDocFreq = sumDocFreq;
                _docCount = docCount;
                _longsSize = longsSize;
            }

            public override IComparer<BytesRef> Comparator
            {
                get { return BytesRef.UTF8SortedAsUnicodeComparer; }
            }

            public override TermsEnum Iterator(TermsEnum reuse)
            {
                return new SegmentTermsEnum(this, _blockTermsReader);
            }

            public override bool HasFreqs()
            {
                return _fieldInfo.IndexOptions >= IndexOptions.DOCS_AND_FREQS;
            }

            public override bool HasOffsets()
            {
                return _fieldInfo.IndexOptions >= IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS;
            }

            public override bool HasPositions()
            {
                return _fieldInfo.IndexOptions >= IndexOptions.DOCS_AND_FREQS_AND_POSITIONS;
            }

            public override bool HasPayloads()
            {
                return _fieldInfo.HasPayloads();
            }

            public override long Size()
            {
                return _numTerms;
            }

            public override long SumTotalTermFreq
            {
                get { return _sumTotalTermFreq; }
            }

            public override long SumDocFreq
            {
                get { return _sumDocFreq; }
            }

            public override int DocCount
            {
                get { return _docCount; }
            }

            // Iterates through terms in this field
            private class SegmentTermsEnum : TermsEnum
            {
                private readonly FieldReader _fieldReader;
                private readonly BlockTermsReader _blockTermsReader;

                private readonly IndexInput _input;
                private readonly BlockTermState _state;
                private readonly bool _doOrd;
                private readonly FieldAndTerm _fieldTerm = new FieldAndTerm();
                private readonly TermsIndexReaderBase.FieldIndexEnum _indexEnum;
                private readonly BytesRef _term = new BytesRef();

                /* This is true if indexEnum is "still" seek'd to the index term
                 for the current term. We set it to true on seeking, and then it
                 remains valid until next() is called enough times to load another
                 terms block: */
                private bool _indexIsCurrent;

                /* True if we've already called .next() on the indexEnum, to "bracket"
                the current block of terms: */
                private bool _didIndexNext;

                /* Next index term, bracketing the current block of terms; this is
                only valid if didIndexNext is true: */
                private BytesRef _nextIndexTerm;

                /* True after seekExact(TermState), do defer seeking.  If the app then
                calls next() (which is not "typical"), then we'll do the real seek */
                private bool _seekPending;

                /* How many blocks we've read since last seek.  Once this
                 is >= indexEnum.getDivisor() we set indexIsCurrent to false (since
                 the index can no long bracket seek-within-block). */
                private int _blocksSinceSeek;

                private byte[] _termSuffixes;
                private readonly ByteArrayDataInput _termSuffixesReader = new ByteArrayDataInput();

                /* Common prefix used for all terms in this block. */
                private int _termBlockPrefix;

                /* How many terms in current block */
                private int _blockTermCount;

                private byte[] _docFreqBytes;
                private readonly ByteArrayDataInput _freqReader = new ByteArrayDataInput();
                private int _metaDataUpto;

                private readonly long[] _longs;
                private byte[] _bytes;
                private ByteArrayDataInput _bytesReader;

                public SegmentTermsEnum(FieldReader fieldReader, BlockTermsReader blockTermsReader)
                {
                    _fieldReader = fieldReader;
                    _blockTermsReader = blockTermsReader;

                    _input = (IndexInput) _blockTermsReader._input.Clone();
                    _input.Seek(_fieldReader._termsStartPointer);
                    _indexEnum = _blockTermsReader._indexReader.GetFieldEnum(_fieldReader._fieldInfo);
                    _doOrd = _blockTermsReader._indexReader.SupportsOrd;
                    _fieldTerm.Field = _fieldReader._fieldInfo.Name;
                    _state = _blockTermsReader._postingsReader.NewTermState();
                    _state.TotalTermFreq = -1;
                    _state.Ord = -1;

                    _termSuffixes = new byte[128];
                    _docFreqBytes = new byte[64];
                    _longs = new long[_fieldReader._longsSize];
                }

                public override IComparer<BytesRef> Comparator
                {
                    get { return BytesRef.UTF8SortedAsUnicodeComparer; }
                }

                /// <remarks>
                /// TODO: we may want an alternate mode here which is
                /// "if you are about to return NOT_FOUND I won't use
                /// the terms data from that"; eg FuzzyTermsEnum will
                /// (usually) just immediately call seek again if we
                /// return NOT_FOUND so it's a waste for us to fill in
                /// the term that was actually NOT_FOUND 
                /// </remarks>
                public override SeekStatus SeekCeil(BytesRef target)
                {
                    if (_indexEnum == null)
                        throw new InvalidOperationException("terms index was not loaded");
                  
                    var doSeek = true;

                    // See if we can avoid seeking, because target term
                    // is after current term but before next index term:
                    if (_indexIsCurrent)
                    {
                        var cmp = BytesRef.UTF8SortedAsUnicodeComparer.Compare(_term, target);

                        if (cmp == 0)
                            return SeekStatus.FOUND;     // Already at the requested term
                        
                        if (cmp < 0)
                        {
                            // Target term is after current term
                            if (!_didIndexNext)
                            {
                                _nextIndexTerm = _indexEnum.Next == -1 ? null : _indexEnum.Term;
                                _didIndexNext = true;
                            }

                            if (_nextIndexTerm == null ||
                                BytesRef.UTF8SortedAsUnicodeComparer.Compare(target, _nextIndexTerm) < 0)
                            {
                                // Optimization: requested term is within the
                                // same term block we are now in; skip seeking
                                // (but do scanning):
                                doSeek = false;
                            }
                        }
                    }

                    if (doSeek)
                    {
                        //System.out.println("  seek");

                        // Ask terms index to find biggest indexed term (=
                        // first term in a block) that's <= our text:
                        _input.Seek(_indexEnum.Seek(target).Value);
                        var result = NextBlock();

                        // Block must exist since, at least, the indexed term
                        // is in the block:
                        Debug.Assert(result);

                        _indexIsCurrent = true;
                        _didIndexNext = false;
                        _blocksSinceSeek = 0;

                        if (_doOrd)
                            _state.Ord = _indexEnum.Ord - 1;
                        
                        _term.CopyBytes(_indexEnum.Term);
                    }
                    else
                    {
                        if (_state.TermBlockOrd == _blockTermCount && !NextBlock())
                        {
                            _indexIsCurrent = false;
                            return SeekStatus.END;
                        }
                    }

                    _seekPending = false;

                    var common = 0;

                    // Scan within block.  We could do this by calling
                    // _next() and testing the resulting term, but this
                    // is wasteful.  Instead, we first confirm the
                    // target matches the common prefix of this block,
                    // and then we scan the term bytes directly from the
                    // termSuffixesreader's byte[], saving a copy into
                    // the BytesRef term per term.  Only when we return
                    // do we then copy the bytes into the term.

                    while (true)
                    {
                        // First, see if target term matches common prefix
                        // in this block:
                        if (common < _termBlockPrefix)
                        {

                            var cmp = (_term.Bytes[common] & 0xFF) - (target.Bytes[target.Offset + common] & 0xFF);
                            if (cmp < 0)
                            {

                                // TODO: maybe we should store common prefix
                                // in block header?  (instead of relying on
                                // last term of previous block)

                                // Target's prefix is after the common block
                                // prefix, so term cannot be in this block
                                // but it could be in next block.  We
                                // must scan to end-of-block to set common
                                // prefix for next block:
                                if (_state.TermBlockOrd < _blockTermCount)
                                {
                                    while (_state.TermBlockOrd < _blockTermCount - 1)
                                    {
                                        _state.TermBlockOrd++;
                                        _state.Ord++;
                                        _termSuffixesReader.SkipBytes(_termSuffixesReader.ReadVInt());
                                    }
                                    var suffix = _termSuffixesReader.ReadVInt();
                                    _term.Length = _termBlockPrefix + suffix;
                                    if (_term.Bytes.Length < _term.Length)
                                    {
                                        _term.Grow(_term.Length);
                                    }
                                    _termSuffixesReader.ReadBytes(_term.Bytes, _termBlockPrefix, suffix);
                                }
                                _state.Ord++;

                                if (!NextBlock())
                                {
                                    _indexIsCurrent = false;
                                    return SeekStatus.END;
                                }
                                common = 0;

                            }
                            else if (cmp > 0)
                            {
                                // Target's prefix is before the common prefix
                                // of this block, so we position to start of
                                // block and return NOT_FOUND:
                                Debug.Assert(_state.TermBlockOrd == 0);

                                var suffix = _termSuffixesReader.ReadVInt();
                                _term.Length = _termBlockPrefix + suffix;
                                if (_term.Bytes.Length < _term.Length)
                                {
                                    _term.Grow(_term.Length);
                                }
                                _termSuffixesReader.ReadBytes(_term.Bytes, _termBlockPrefix, suffix);
                                return SeekStatus.NOT_FOUND;
                            }
                            else
                            {
                                common++;
                            }

                            continue;
                        }

                        // Test every term in this block
                        while (true)
                        {
                            _state.TermBlockOrd++;
                            _state.Ord++;

                            var suffix = _termSuffixesReader.ReadVInt();

                            // We know the prefix matches, so just compare the new suffix:

                            var termLen = _termBlockPrefix + suffix;
                            var bytePos = _termSuffixesReader.Position;

                            var next = false;

                            var limit = target.Offset + (termLen < target.Length ? termLen : target.Length);
                            var targetPos = target.Offset + _termBlockPrefix;
                            while (targetPos < limit)
                            {
                                var cmp = (_termSuffixes[bytePos++] & 0xFF) - (target.Bytes[targetPos++] & 0xFF);
                                if (cmp < 0)
                                {
                                    // Current term is still before the target;
                                    // keep scanning
                                    next = true;
                                    break;
                                }

                                if (cmp <= 0) continue;

                                // Done!  Current term is after target. Stop
                                // here, fill in real term, return NOT_FOUND.
                                _term.Length = _termBlockPrefix + suffix;
                                if (_term.Bytes.Length < _term.Length)
                                {
                                    _term.Grow(_term.Length);
                                }
                                _termSuffixesReader.ReadBytes(_term.Bytes, _termBlockPrefix, suffix);
                                return SeekStatus.NOT_FOUND;
                            }

                            if (!next && target.Length <= termLen)
                            {
                                _term.Length = _termBlockPrefix + suffix;
                                if (_term.Bytes.Length < _term.Length)
                                {
                                    _term.Grow(_term.Length);
                                }
                                _termSuffixesReader.ReadBytes(_term.Bytes, _termBlockPrefix, suffix);

                                return target.Length == termLen ? SeekStatus.FOUND : SeekStatus.NOT_FOUND;
                            }

                            if (_state.TermBlockOrd == _blockTermCount)
                            {
                                // Must pre-fill term for next block's common prefix
                                _term.Length = _termBlockPrefix + suffix;
                                if (_term.Bytes.Length < _term.Length)
                                {
                                    _term.Grow(_term.Length);
                                }
                                _termSuffixesReader.ReadBytes(_term.Bytes, _termBlockPrefix, suffix);
                                break;
                            }
                            
                            _termSuffixesReader.SkipBytes(suffix);
                        }

                        // The purpose of the terms dict index is to seek
                        // the enum to the closest index term before the
                        // term we are looking for.  So, we should never
                        // cross another index term (besides the first
                        // one) while we are scanning:

                        Debug.Assert(_indexIsCurrent);

                        if (!NextBlock())
                        {
                            _indexIsCurrent = false;
                            return SeekStatus.END;
                        }
                        common = 0;
                    }
                }

                public override BytesRef Next()
                {
                    // If seek was previously called and the term was cached,
                    // usually caller is just going to pull a D/&PEnum or get
                    // docFreq, etc.  But, if they then call next(),
                    // this method catches up all internal state so next()
                    // works properly:
                    if (!_seekPending) return _next();

                    Debug.Assert(!_indexIsCurrent);

                    _input.Seek(_state.BlockFilePointer);
                    var pendingSeekCount = _state.TermBlockOrd;
                    var result = NextBlock();

                    var savOrd = _state.Ord;

                    // Block must exist since seek(TermState) was called w/ a
                    // TermState previously returned by this enum when positioned
                    // on a real term:
                    Debug.Assert(result);

                    while (_state.TermBlockOrd < pendingSeekCount)
                    {
                        var nextResult = _next();
                        Debug.Assert(nextResult != null);
                    }
                    _seekPending = false;
                    _state.Ord = savOrd;
                    return _next();
                }

                /// <summary>
                /// Decodes only the term bytes of the next term.  If caller then asks for
                /// metadata, ie docFreq, totalTermFreq or pulls a D/P Enum, we then (lazily)
                /// decode all metadata up to the current term
                /// </summary>
                /// <returns></returns>
                private BytesRef _next()
                {
                    //System.out.println("BTR._next seg=" + segment + " this=" + this + " termCount=" + state.TermBlockOrd + " (vs " + blockTermCount + ")");
                    if (_state.TermBlockOrd == _blockTermCount && !NextBlock())
                    {
                        //System.out.println("  eof");
                        _indexIsCurrent = false;
                        return null;
                    }

                    // TODO: cutover to something better for these ints!  simple64?

                    var suffix = _termSuffixesReader.ReadVInt();
                    //System.out.println("  suffix=" + suffix);

                    _term.Length = _termBlockPrefix + suffix;
                    if (_term.Bytes.Length < _term.Length)
                    {
                        _term.Grow(_term.Length);
                    }
                    _termSuffixesReader.ReadBytes(_term.Bytes, _termBlockPrefix, suffix);
                    _state.TermBlockOrd++;

                    // NOTE: meaningless in the non-ord case
                    _state.Ord++;

                    return _term;
                }

                public override BytesRef Term()
                {
                    return _term;
                }

                public override int DocFreq()
                {
                    DecodeMetaData();
                    return _state.DocFreq;
                }

                public override long TotalTermFreq()
                {
                    DecodeMetaData();
                    return _state.TotalTermFreq;
                }

                public override DocsEnum Docs(Bits liveDocs, DocsEnum reuse, int flags)
                {
                    DecodeMetaData();
                    return _blockTermsReader._postingsReader.Docs(_fieldReader._fieldInfo, _state, liveDocs, reuse, flags);
                }

                public override DocsAndPositionsEnum DocsAndPositions(Bits liveDocs, DocsAndPositionsEnum reuse,
                    int flags)
                {
                    if (_fieldReader._fieldInfo.IndexOptions.GetValueOrDefault().CompareTo(IndexOptions.DOCS_AND_FREQS_AND_POSITIONS) < 0)
                    {
                        // Positions were not indexed:
                        return null;
                    }

                    DecodeMetaData();
                    return _blockTermsReader._postingsReader.DocsAndPositions(_fieldReader._fieldInfo, _state, liveDocs, reuse, flags);
                }

                public override void SeekExact(BytesRef target, TermState otherState)
                {
                    //System.out.println("BTR.seekExact termState target=" + target.utf8ToString() + " " + target + " this=" + this);
                    Debug.Assert(otherState is BlockTermState);
                    Debug.Assert(!_doOrd || ((BlockTermState) otherState).Ord < _fieldReader._numTerms);
                    _state.CopyFrom(otherState);
                    _seekPending = true;
                    _indexIsCurrent = false;
                    _term.CopyBytes(target);
                }

                public override TermState TermState()
                {
                    DecodeMetaData();
                    return (TermState) _state.Clone();
                }

                public override void SeekExact(long ord)
                {
                    if (_indexEnum == null)
                        throw new InvalidOperationException("terms index was not loaded");

                    Debug.Assert(ord < _fieldReader._numTerms);

                    // TODO: if ord is in same terms block and
                    // after current ord, we should avoid this seek just
                    // like we do in the seek(BytesRef) case
                    _input.Seek(_indexEnum.Seek(ord).Value);
                    bool result = NextBlock();

                    // Block must exist since ord < numTerms:
                    Debug.Assert(result);

                    _indexIsCurrent = true;
                    _didIndexNext = false;
                    _blocksSinceSeek = 0;
                    _seekPending = false;

                    _state.Ord = _indexEnum.Ord - 1;
                    Debug.Assert(_state.Ord >= -1, "Ord=" + _state.Ord);
                    _term.CopyBytes(_indexEnum.Term);

                    // Now, scan:
                    var left = (int) (ord - _state.Ord);
                    while (left > 0)
                    {
                        var term = _next();
                        Debug.Assert(term != null);
                        left--;
                        Debug.Assert(_indexIsCurrent);
                    }

                }

                public override long Ord()
                {
                    if (!_doOrd)
                        throw new NotSupportedException();

                    return _state.Ord;
                }

                // Does initial decode of next block of terms; this
                // doesn't actually decode the docFreq, totalTermFreq,
                // postings details (frq/prx offset, etc.) metadata;
                // it just loads them as byte[] blobs which are then      
                // decoded on-demand if the metadata is ever requested
                // for any term in this block.  This enables terms-only
                // intensive consumes (eg certain MTQs, respelling) to
                // not pay the price of decoding metadata they won't
                // use.

                private bool NextBlock()
                {
                    // TODO: we still lazy-decode the byte[] for each
                    // term (the suffix), but, if we decoded
                    // all N terms up front then seeking could do a fast
                    // bsearch w/in the block...

                    _state.BlockFilePointer = _input.FilePointer;
                    _blockTermCount = _input.ReadVInt();

                    if (_blockTermCount == 0)
                        return false;

                    _termBlockPrefix = _input.ReadVInt();

                    // term suffixes:
                    int len = _input.ReadVInt();
                    if (_termSuffixes.Length < len)
                    {
                        _termSuffixes = new byte[ArrayUtil.Oversize(len, 1)];
                    }
                    //System.out.println("  termSuffixes len=" + len);
                    _input.ReadBytes(_termSuffixes, 0, len);

                    _termSuffixesReader.Reset(_termSuffixes, 0, len);

                    // docFreq, totalTermFreq
                    len = _input.ReadVInt();
                    if (_docFreqBytes.Length < len)
                        _docFreqBytes = new byte[ArrayUtil.Oversize(len, 1)];

                    _input.ReadBytes(_docFreqBytes, 0, len);
                    _freqReader.Reset(_docFreqBytes, 0, len);

                    // metadata
                    len = _input.ReadVInt();
                    if (_bytes == null)
                    {
                        _bytes = new byte[ArrayUtil.Oversize(len, 1)];
                        _bytesReader = new ByteArrayDataInput();
                    }
                    else if (_bytes.Length < len)
                    {
                        _bytes = new byte[ArrayUtil.Oversize(len, 1)];
                    }

                    _input.ReadBytes(_bytes, 0, len);
                    _bytesReader.Reset(_bytes, 0, len);

                    _metaDataUpto = 0;
                    _state.TermBlockOrd = 0;

                    _blocksSinceSeek++;
                    _indexIsCurrent = _indexIsCurrent && (_blocksSinceSeek < _blockTermsReader._indexReader.Divisor);

                    return true;
                }

                private void DecodeMetaData()
                {
                    //System.out.println("BTR.decodeMetadata mdUpto=" + metaDataUpto + " vs termCount=" + state.TermBlockOrd + " state=" + state);
                    if (!_seekPending)
                    {
                        // TODO: cutover to random-access API
                        // here.... really stupid that we have to decode N
                        // wasted term metadata just to get to the N+1th
                        // that we really need...

                        // lazily catch up on metadata decode:

                        var limit = _state.TermBlockOrd;
                        var absolute = _metaDataUpto == 0;
                        // TODO: better API would be "jump straight to term=N"???
                        while (_metaDataUpto < limit)
                        {
                            //System.out.println("  decode mdUpto=" + metaDataUpto);
                            // TODO: we could make "tiers" of metadata, ie,
                            // decode docFreq/totalTF but don't decode postings
                            // metadata; this way caller could get
                            // docFreq/totalTF w/o paying decode cost for
                            // postings

                            // TODO: if docFreq were bulk decoded we could
                            // just skipN here:

                            _state.DocFreq = _freqReader.ReadVInt();
                            if (_fieldReader._fieldInfo.IndexOptions != IndexOptions.DOCS_ONLY)
                            {
                                _state.TotalTermFreq = _state.DocFreq + _freqReader.ReadVLong();
                            }
                            // metadata
                            for (int i = 0; i < _longs.Length; i++)
                            {
                                _longs[i] = _bytesReader.ReadVLong();
                            }
                            _blockTermsReader._postingsReader.DecodeTerm(_longs, _bytesReader, _fieldReader._fieldInfo, _state, absolute);
                            _metaDataUpto++;
                            absolute = false;
                        }
                    }
                }

            }
        }

     
    }
}