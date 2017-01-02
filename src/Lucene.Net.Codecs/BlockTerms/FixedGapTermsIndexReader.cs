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
    using System.Linq;
    using Index;
    using Store;
    using Util;
    using Util.Packed;

    /// <summary>
    /// TermsIndexReader for simple every Nth terms indexes
    /// 
    /// See FixedGapTermsIndexWriter
    /// 
    /// lucene.experimental
    /// </summary>
    public class FixedGapTermsIndexReader : TermsIndexReaderBase
    {

        // NOTE: long is overkill here, since this number is 128
        // by default and only indexDivisor * 128 if you change
        // the indexDivisor at search time.  But, we use this in a
        // number of places to multiply out the actual ord, and we
        // will overflow int during those multiplies.  So to avoid
        // having to upgrade each multiple to long in multiple
        // places (error prone), we use long here:
        private readonly long _totalIndexInterval;
        private readonly int _indexDivisor;
        private readonly int indexInterval;

        // Closed if indexLoaded is true:
        private readonly IndexInput _input;

        private volatile bool _indexLoaded;
        private readonly IComparer<BytesRef> _termComp;
        private const int PAGED_BYTES_BITS = 15;

        // all fields share this single logical byte[]
        private readonly PagedBytes _termBytes = new PagedBytes(PAGED_BYTES_BITS);
        private readonly PagedBytes.Reader _termBytesReader;
        private readonly Dictionary<FieldInfo, FieldIndexData> _fields = new Dictionary<FieldInfo, FieldIndexData>();

        // start of the field info data
        private long _dirOffset;
        private readonly int _version;

        public FixedGapTermsIndexReader(Directory dir, FieldInfos fieldInfos, String segment, int indexDivisor,
            IComparer<BytesRef> termComp, String segmentSuffix, IOContext context)
        {
            _termComp = termComp;

            Debug.Assert(indexDivisor == -1 || indexDivisor > 0);

            _input =
                dir.OpenInput(
                    IndexFileNames.SegmentFileName(segment, segmentSuffix,
                        FixedGapTermsIndexWriter.TERMS_INDEX_EXTENSION),
                    context);

            var success = false;

            try
            {

                _version = ReadHeader(_input);

                if (_version >= FixedGapTermsIndexWriter.VERSION_CHECKSUM)
                    CodecUtil.ChecksumEntireFile(_input);
                
                indexInterval = _input.ReadInt();
                
                if (indexInterval < 1)
                {
                    throw new CorruptIndexException(String.Format("Invalid indexInterval: {0}, Resource: {1}",
                        indexInterval, _input));
                }

                _indexDivisor = indexDivisor;

                if (indexDivisor < 0)
                {
                    _totalIndexInterval = indexInterval;
                }
                else
                {
                    // In case terms index gets loaded, later, on demand
                    _totalIndexInterval = indexInterval*indexDivisor;
                }

                Debug.Assert(_totalIndexInterval > 0);

                SeekDir(_input, _dirOffset);

                // Read directory
                int numFields = _input.ReadVInt();

                if (numFields < 0)
                    throw new CorruptIndexException(String.Format("Invalid numFields: {0}, Resource: {1}", numFields,
                        _input));

                for (int i = 0; i < numFields; i++)
                {
                    int field = _input.ReadVInt();
                    int numIndexTerms = _input.ReadVInt();
                    if (numIndexTerms < 0)
                        throw new CorruptIndexException(String.Format("Invalid numIndexTerms: {0}, Resource: {1}",
                            numIndexTerms,
                            _input));

                    long termsStart = _input.ReadVLong();
                    long indexStart = _input.ReadVLong();
                    long packedIndexStart = _input.ReadVLong();
                    long packedOffsetsStart = _input.ReadVLong();

                    if (packedIndexStart < indexStart)
                        throw new CorruptIndexException(
                            String.Format(
                                "Invalid packedIndexStart: {0}, IndexStart: {1}, NumIndexTerms: {2}, Resource: {3}",
                                packedIndexStart,
                                indexStart, numIndexTerms, _input));

                    FieldInfo fieldInfo = fieldInfos.FieldInfo(field);

                    try
                    {
                        _fields.Add(fieldInfo,
                            new FieldIndexData(numIndexTerms, indexStart, termsStart, packedIndexStart,
                                packedOffsetsStart, this));
                    }
                    catch (ArgumentException)
                    {
                        throw new CorruptIndexException(String.Format("Duplicate field: {0}, Resource {1}",
                            fieldInfo.Name,
                            _input));
                    }


                }
                success = true;
            }
            finally
            {
                if (!success)
                {
                    IOUtils.CloseWhileHandlingException(_input);
                }
                if (indexDivisor > 0)
                {
                    _input.Dispose();
                    _input = null;
                    if (success)
                        _indexLoaded = true;

                    _termBytesReader = _termBytes.Freeze(true);
                }
            }
        }

        public override int Divisor
        {
            get { return _indexDivisor; }
        }

        private int ReadHeader(DataInput input)
        {
            var version = CodecUtil.CheckHeader(input, FixedGapTermsIndexWriter.CODEC_NAME,
                FixedGapTermsIndexWriter.VERSION_START, FixedGapTermsIndexWriter.VERSION_CURRENT);
            
            if (version < FixedGapTermsIndexWriter.VERSION_APPEND_ONLY)
                _dirOffset = input.ReadLong();

            return version;
        }

        public override bool SupportsOrd
        {
            get { return true; }
        }

        public override FieldIndexEnum GetFieldEnum(FieldInfo fieldInfo)
        {
            FieldIndexData fieldData = _fields[fieldInfo];
            return fieldData.CoreIndex == null ? null : new IndexEnum(fieldData.CoreIndex, this);
        }

        public override void Dispose()
        {
            if (_input != null && !_indexLoaded)
                _input.Dispose();
        }

        private void SeekDir(IndexInput input, long dirOffset)
        {
            if (_version >= FixedGapTermsIndexWriter.VERSION_CHECKSUM)
            {
                input.Seek(input.Length - CodecUtil.FooterLength() - 8);
                dirOffset = input.ReadLong();

            }
            else if (_version >= FixedGapTermsIndexWriter.VERSION_APPEND_ONLY)
            {
                input.Seek(input.Length - 8);
                dirOffset = input.ReadLong();
            }

            input.Seek(dirOffset);
        }

        public override long RamBytesUsed
        {
            get
            {
                var sizeInBytes = ((_termBytes != null) ? _termBytes.RamBytesUsed() : 0) +
                                  ((_termBytesReader != null) ? _termBytesReader.RamBytesUsed() : 0);

                return _fields.Values.Aggregate(sizeInBytes,
                    (current, entry) => (current + entry.CoreIndex.RamBytesUsed));
            }
        }

        private class IndexEnum : FieldIndexEnum
        {
            private readonly FieldIndexData.CoreFieldIndex _fieldIndex;
            private readonly FixedGapTermsIndexReader _fgtir;

            public IndexEnum(FieldIndexData.CoreFieldIndex fieldIndex, FixedGapTermsIndexReader fgtir)
            {
                Term = new BytesRef();
                _fieldIndex = fieldIndex;
                _fgtir = fgtir;
            }

            public override long Ord { get; set; }

            public override sealed BytesRef Term { get; set; }

            public override long? Seek(BytesRef target)
            {
                var lo = 0; // binary search
                var hi = _fieldIndex.NumIndexTerms - 1;

                Debug.Assert(_fgtir._totalIndexInterval > 0,
                    String.Format("TotalIndexInterval: {0}", _fgtir._totalIndexInterval));

                long offset;
                int length;
                while (hi >= lo)
                {
                    var mid = (int)((uint)(lo + hi) >> 1);

                    offset = _fieldIndex.TermOffsets.Get(mid);
                    length = (int) (_fieldIndex.TermOffsets.Get(1 + mid) - offset);
                    _fgtir._termBytesReader.FillSlice(Term, _fieldIndex.TermBytesStart + offset, length);

                    int delta = _fgtir._termComp.Compare(target, Term);
                    if (delta < 0)
                    {
                        hi = mid - 1;
                    }
                    else if (delta > 0)
                    {
                        lo = mid + 1;
                    }
                    else
                    {
                        Debug.Assert(mid >= 0);
                        Ord = mid * _fgtir._totalIndexInterval;
                        return _fieldIndex.TermsStart + _fieldIndex.TermsDictOffsets.Get(mid);
                    }
                }

                if (hi < 0)
                {
                    Debug.Assert(hi == -1);
                    hi = 0;
                }

                offset = _fieldIndex.TermOffsets.Get(hi);
                length = (int) (_fieldIndex.TermOffsets.Get(1 + hi) - offset);
                _fgtir._termBytesReader.FillSlice(Term, _fieldIndex.TermBytesStart + offset, length);

                Ord = hi * _fgtir._totalIndexInterval;
                return _fieldIndex.TermsStart + _fieldIndex.TermsDictOffsets.Get(hi);
            }

            public override long? Next
            {
                get
                {
                    var idx = 1 + (int)(Ord / _fgtir._totalIndexInterval);
                    if (idx >= _fieldIndex.NumIndexTerms)
                        return -1;

                    Ord += _fgtir._totalIndexInterval;

                    var offset = _fieldIndex.TermOffsets.Get(idx);
                    var length = (int)(_fieldIndex.TermOffsets.Get(1 + idx) - offset);

                    _fgtir._termBytesReader.FillSlice(Term, _fieldIndex.TermBytesStart + offset, length);

                    return _fieldIndex.TermsStart + _fieldIndex.TermsDictOffsets.Get(idx);
                }
            }

            public override long? Seek(long ord)
            {
                var idx = (int)(ord / _fgtir._totalIndexInterval);

                // caller must ensure ord is in bounds
                Debug.Assert(idx < _fieldIndex.NumIndexTerms);

                var offset = _fieldIndex.TermOffsets.Get(idx);
                var length = (int)(_fieldIndex.TermOffsets.Get(1 + idx) - offset);

                _fgtir._termBytesReader.FillSlice(Term, _fieldIndex.TermBytesStart + offset, length);
                Ord = idx * _fgtir._totalIndexInterval;

                return _fieldIndex.TermsStart + _fieldIndex.TermsDictOffsets.Get(idx);
            }
        }

        protected class FieldIndexData
        {
            public volatile CoreFieldIndex CoreIndex;

            private readonly long _indexStart;
            private readonly long _termsStart;
            private readonly long _packedIndexStart;
            private readonly long _packedOffsetsStart;
            private readonly int _numIndexTerms;
            private readonly FixedGapTermsIndexReader _fgtir;

            public FieldIndexData(int numIndexTerms, long indexStart, long termsStart,
                long packedIndexStart,
                long packedOffsetsStart, FixedGapTermsIndexReader fgtir)
            {

                _termsStart = termsStart;
                _indexStart = indexStart;
                _packedIndexStart = packedIndexStart;
                _packedOffsetsStart = packedOffsetsStart;
                _numIndexTerms = numIndexTerms;
                _fgtir = fgtir;

                if (_fgtir._indexDivisor > 0)
                    LoadTermsIndex();
            }

            private void LoadTermsIndex()
            {
                if (CoreIndex == null)
                    CoreIndex = new CoreFieldIndex(_indexStart, _termsStart, _packedIndexStart, _packedOffsetsStart,
                        _numIndexTerms, _fgtir);
            }

            public class CoreFieldIndex
            {
                /// <summary>
                /// Where this fields term begin in the packed byte[] data
                /// </summary>
                public long TermBytesStart { get; private set; }

                /// <summary>
                /// Offset into index TermBytes
                /// </summary>
                public PackedInts.Reader TermOffsets { get; private set; }

                /// <summary>
                /// Index pointers into main terms dict
                /// </summary>
                public PackedInts.Reader TermsDictOffsets { get; private set; }

                /// <summary>Returns approximate RAM bytes Used</summary>
                public long RamBytesUsed
                {
                    get
                    {
                        return ((TermOffsets != null) ? TermOffsets.RamBytesUsed() : 0) +
                               ((TermsDictOffsets != null) ? TermsDictOffsets.RamBytesUsed() : 0);
                    }
                }

                public int NumIndexTerms { get; private set; }
                public long TermsStart { get; private set; }

                public CoreFieldIndex(long indexStart, long termsStart, long packedIndexStart, long packedOffsetsStart,
                    int numIndexTerms, FixedGapTermsIndexReader fgtir)
                {
                    TermsStart = termsStart;
                    TermBytesStart = fgtir._termBytes.Pointer;

                    var clone = (IndexInput)fgtir._input.Clone();
                    clone.Seek(indexStart);

                    // -1 is passed to mean "don't load term index", but
                    // if we are then later loaded it's overwritten with
                    // a real value
                    Debug.Assert(fgtir._indexDivisor > 0);

                    NumIndexTerms = 1 + (numIndexTerms - 1)/fgtir._indexDivisor;

                    Debug.Assert(NumIndexTerms > 0,
                        String.Format("NumIndexTerms: {0}, IndexDivisor: {1}", NumIndexTerms, fgtir._indexDivisor));

                    if (fgtir._indexDivisor == 1)
                    {
                        // Default (load all index terms) is fast -- slurp in the images from disk:

                        try
                        {
                            var numTermBytes = packedIndexStart - indexStart;
                            fgtir._termBytes.Copy(clone, numTermBytes);

                            // records offsets into main terms dict file
                            TermsDictOffsets = PackedInts.GetReader(clone);
                            Debug.Assert(TermsDictOffsets.Size() == numIndexTerms);

                            // records offsets into byte[] term data
                            TermOffsets = PackedInts.GetReader(clone);
                            Debug.Assert(TermOffsets.Size() == 1 + numIndexTerms);
                        }
                        finally
                        {
                            clone.Dispose();
                        }
                    }
                    else
                    {
                        // Get packed iterators
                        var clone1 = (IndexInput)fgtir._input.Clone();
                        var clone2 = (IndexInput)fgtir._input.Clone();

                        try
                        {
                            // Subsample the index terms
                            clone1.Seek(packedIndexStart);
                            
                            PackedInts.IReaderIterator termsDictOffsetsIter = PackedInts.GetReaderIterator(clone1,
                                PackedInts.DEFAULT_BUFFER_SIZE);

                            clone2.Seek(packedOffsetsStart);
                            
                            PackedInts.IReaderIterator termOffsetsIter = PackedInts.GetReaderIterator(clone2,
                                PackedInts.DEFAULT_BUFFER_SIZE);

                            // TODO: often we can get by w/ fewer bits per
                            // value, below.. .but this'd be more complex:
                            // we'd have to try @ fewer bits and then grow
                            // if we overflowed it.

                            PackedInts.Mutable termsDictOffsetsM = PackedInts.GetMutable(NumIndexTerms,
                                termsDictOffsetsIter.BitsPerValue, PackedInts.DEFAULT);
                            PackedInts.Mutable termOffsetsM = PackedInts.GetMutable(NumIndexTerms + 1,
                                termOffsetsIter.BitsPerValue, PackedInts.DEFAULT);

                            TermsDictOffsets = termsDictOffsetsM;
                            TermOffsets = termOffsetsM;

                            var upto = 0;
                            long termOffsetUpto = 0;

                            while (upto < NumIndexTerms)
                            {
                                // main file offset copies straight over
                                termsDictOffsetsM.Set(upto, termsDictOffsetsIter.Next());

                                termOffsetsM.Set(upto, termOffsetUpto);

                                var termOffset = termOffsetsIter.Next();
                                var nextTermOffset = termOffsetsIter.Next();
                                var numTermBytes = (int) (nextTermOffset - termOffset);

                                clone.Seek(indexStart + termOffset);
                                
                                Debug.Assert(indexStart + termOffset < clone.Length,
                                    String.Format("IndexStart: {0}, TermOffset: {1}, Len: {2}", indexStart, termOffset,
                                        clone.Length));
                                
                                Debug.Assert(indexStart + termOffset + numTermBytes < clone.Length);

                                fgtir._termBytes.Copy(clone, numTermBytes);
                                termOffsetUpto += numTermBytes;

                                upto++;
                                if (upto == NumIndexTerms)
                                    break;
                                
                                // skip terms:
                                termsDictOffsetsIter.Next();
                                for (var i = 0; i < fgtir._indexDivisor - 2; i++)
                                {
                                    termOffsetsIter.Next();
                                    termsDictOffsetsIter.Next();
                                }
                            }
                            termOffsetsM.Set(upto, termOffsetUpto);

                        }
                        finally
                        {
                            clone1.Dispose();
                            clone2.Dispose();
                            clone.Dispose();
                        }
                    }
                }

            }
        }

    }
}