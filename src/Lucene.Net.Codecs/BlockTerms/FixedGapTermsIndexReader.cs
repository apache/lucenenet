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
        private long totalIndexInterval;

        private int indexDivisor;
        private readonly int indexInterval;

        // Closed if indexLoaded is true:
        private IndexInput input;
        private volatile bool indexLoaded;

        private readonly IComparer<BytesRef> termComp;

        private static readonly int PAGED_BYTES_BITS = 15;

        // all fields share this single logical byte[]
        private readonly PagedBytes termBytes = new PagedBytes(PAGED_BYTES_BITS);
        private PagedBytes.Reader termBytesReader;

        private readonly Dictionary<FieldInfo, FieldIndexData> fields = new Dictionary<FieldInfo, FieldIndexData>();

        // start of the field info data
        private long dirOffset;

        private readonly int version;

        public FixedGapTermsIndexReader(Directory dir, FieldInfos fieldInfos, String segment, int indexDivisor,
            IComparer<BytesRef> termComp, String segmentSuffix, IOContext context)
        {
            this.termComp = termComp;

            Debug.Assert(indexDivisor == -1 || indexDivisor > 0;

            input =
                dir.OpenInput(
                    IndexFileNames.SegmentFileName(segment, segmentSuffix,
                        FixedGapTermsIndexWriter.TERMS_INDEX_EXTENSION),
                    context);

            bool success = false;

            try
            {

                version = ReadHeader(input);

                if (version >= FixedGapTermsIndexWriter.VERSION_CHECKSUM)
                    CodecUtil.ChecksumEntireFile(input);
                
                indexInterval = input.ReadInt();
                
                if (indexInterval < 1)
                {
                    throw new CorruptIndexException(String.Format("Invalid indexInterval: {0}, Resource: {1}",
                        indexInterval, input));
                }

                this.indexDivisor = indexDivisor;

                if (indexDivisor < 0)
                {
                    totalIndexInterval = indexInterval;
                }
                else
                {
                    // In case terms index gets loaded, later, on demand
                    totalIndexInterval = indexInterval*indexDivisor;
                }

                Debug.Assert(totalIndexInterval > 0);

                SeekDir(input, dirOffset);

                // Read directory
                int numFields = input.ReadVInt();

                if (numFields < 0)
                    throw new CorruptIndexException(String.Format("Invalid numFields: {0}, Resource: {1}", numFields,
                        input));

                for (int i = 0; i < numFields; i++)
                {
                    int field = input.ReadVInt();
                    int numIndexTerms = input.ReadVInt();
                    if (numIndexTerms < 0)
                        throw new CorruptIndexException(String.Format("Invalid numIndexTerms: {0}, Resource: {1}",
                            numIndexTerms,
                            input));

                    long termsStart = input.ReadVLong();
                    long indexStart = input.ReadVLong();
                    long packedIndexStart = input.ReadVLong();
                    long packedOffsetsStart = input.ReadVLong();

                    if (packedIndexStart < indexStart)
                        throw new CorruptIndexException(
                            String.Format(
                                "Invalid packedIndexStart: {0}, IndexStart: {1}, NumIndexTerms: {2}, Resource: {3}",
                                packedIndexStart,
                                indexStart, numIndexTerms, input));

                    FieldInfo fieldInfo = fieldInfos.FieldInfo(field);

                    try
                    {
                        fields.Add(fieldInfo,
                            new FieldIndexData(fieldInfo, numIndexTerms, indexStart, termsStart, packedIndexStart,
                                packedOffsetsStart));
                    }
                    catch (ArgumentException)
                    {
                        throw new CorruptIndexException(String.Format("Duplicate field: {0}, Resource {1}",
                            fieldInfo.Name,
                            input));
                    }


                }
                success = true;
            }
            finally
            {
                if (!success)
                {
                    IOUtils.CloseWhileHandlingException(input);
                }
                if (indexDivisor > 0)
                {
                    input.Dispose();
                    input = null;
                    if (success)
                        indexLoaded = true;

                    termBytesReader = termBytes.Freeze(true);
                }
            }
        }

        public override int Divisor
        {
            get { return indexDivisor; }
        }

        private int ReadHeader(IndexInput input)
        {
            int version = CodecUtil.CheckHeader(input, FixedGapTermsIndexWriter.CODEC_NAME,
                FixedGapTermsIndexWriter.VERSION_START, FixedGapTermsIndexWriter.VERSION_CURRENT);
            if (version < FixedGapTermsIndexWriter.VERSION_APPEND_ONLY)
                dirOffset = input.ReadLong();

            return version;
        }

        private class IndexEnum : FieldIndexEnum
        {
            private readonly FieldIndexData.CoreFieldIndex fieldIndex;
            public override long Ord { get; set; }

            public IndexEnum(FieldIndexData.CoreFieldIndex fieldIndex)
            {
                Term = new BytesRef();
                this.fieldIndex = fieldIndex;
            }

            public override BytesRef Term { get; set; }

            public override long Seek(BytesRef target)
            {
                int lo = 0; // binary search
                int hi = fieldIndex.numIndexTerms - 1;
                Debug.Assert(totalIndexInterval > 0, "totalIndexInterval=" + totalIndexInterval);

                while (hi >= lo)
                {
                    int mid = (lo + hi) >> > 1;

                    readonly
                    long offset = fieldIndex.termOffsets.get(mid);
                    readonly
                    int length = (int) (fieldIndex.termOffsets.Get(1 + mid) - offset);
                    termBytesReader.FillSlice(Term, fieldIndex.termBytesStart + offset, length);

                    int delta = termComp.compare(target, term);
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
                        Debug.Assert(mid >= 0;
                        ord = mid*totalIndexInterval;
                        return fieldIndex.termsStart + fieldIndex.termsDictOffsets.get(mid);
                    }
                }

                if (hi < 0)
                {
                    Debug.Assert(hi == -1;
                    hi = 0;
                }

                
                long offset = fieldIndex.termOffsets.Get(hi);
                int length = (int) (fieldIndex.termOffsets.Get(1 + hi) - offset);
                termBytesReader.FillSlice(term, fieldIndex.termBytesStart + offset, length);

                ord = hi*totalIndexInterval;
                return fieldIndex.termsStart + fieldIndex.termsDictOffsets.get(hi);
            }

            public override long Next
            {
                get
                {
                    int idx = 1 + (int) (ord/totalIndexInterval);
                    if (idx >= fieldIndex.numIndexTerms)
                    {
                        return -1;
                    }
                    ord += totalIndexInterval;

                    long offset = fieldIndex.termOffsets.Get(idx);
                    int length = (int) (fieldIndex.termOffsets.Get(1 + idx) - offset);
                    termBytesReader.FillSlice(term, fieldIndex.termBytesStart + offset, length);
                    return fieldIndex.termsStart + fieldIndex.termsDictOffsets.Get(idx);
                }
            }

            public override long Seek(long ord)
            {
                int idx = (int) (ord/totalIndexInterval);
                // caller must ensure ord is in bounds
                Debug.Assert(idx < fieldIndex.NumIndexTerms);

                long offset = fieldIndex.termOffsets.get(idx);
                int length = (int) (fieldIndex.termOffsets.get(1 + idx) - offset);
                termBytesReader.FillSlice(term, fieldIndex.termBytesStart + offset, length);
                Ord = idx*totalIndexInterval;
                return fieldIndex.termsStart + fieldIndex.termsDictOffsets.get(idx);
            }
        }

        public override bool SupportsOrd
        {
            get { return true; }
        }

        protected class FieldIndexData
        {

            public volatile CoreFieldIndex CoreIndex;

            private readonly long indexStart;
            private readonly long termsStart;
            private readonly long packedIndexStart;
            private readonly long packedOffsetsStart;

            private readonly int numIndexTerms;

            public FieldIndexData(FieldInfo fieldInfo, int numIndexTerms, long indexStart, long termsStart,
                long packedIndexStart,
                long packedOffsetsStart)
            {

                this.termsStart = termsStart;
                this.indexStart = indexStart;
                this.packedIndexStart = packedIndexStart;
                this.packedOffsetsStart = packedOffsetsStart;
                this.numIndexTerms = numIndexTerms;

                if (indexDivisor > 0)
                {
                    loadTermsIndex();
                }
            }

            private void loadTermsIndex()
            {
                if (coreIndex == null)
                {
                    coreIndex = new CoreFieldIndex(indexStart, termsStart, packedIndexStart, packedOffsetsStart,
                        numIndexTerms);
                }
            }

            private class CoreFieldIndex
            {

                // where this field's terms begin in the packed byte[]
                // data
                private readonly long termBytesStart;

                // offset into index termBytes
                private readonly PackedInts.Reader termOffsets;

                // index pointers into main terms dict
                private readonly PackedInts.Reader termsDictOffsets;

                private readonly int numIndexTerms;
                private readonly long termsStart;

                public CoreFieldIndex(long indexStart, long termsStart, long packedIndexStart, long packedOffsetsStart,
                    int numIndexTerms)
                {

                    this.termsStart = termsStart;
                    termBytesStart = termBytes.Pointer;

                    IndexInput clone = input.Clone();
                    clone.Seek(indexStart);

                    // -1 is passed to mean "don't load term index", but
                    // if we are then later loaded it's overwritten with
                    // a real value
                    Debug.Assert(indexDivisor > 0);

                    this.numIndexTerms = 1 + (numIndexTerms - 1)/indexDivisor;

                    Debug.Assert(this.numIndexTerms > 0:
                    "numIndexTerms=" + numIndexTerms + " indexDivisor=" + indexDivisor;

                    if (indexDivisor == 1)
                    {
                        // Default (load all index terms) is fast -- slurp in the images from disk:

                        try
                        {
                        readonly
                            long numTermBytes = packedIndexStart - indexStart;
                            termBytes.copy(clone, numTermBytes);

                            // records offsets into main terms dict file
                            termsDictOffsets = PackedInts.getReader(clone);
                            Debug.Assert(termsDictOffsets.size() == numIndexTerms;

                            // records offsets into byte[] term data
                            termOffsets = PackedInts.GetReader(clone);
                            Debug.Assert(termOffsets.Size() == 1 + numIndexTerms);
                        }
                        finally
                        {
                            clone.Dispose();
                        }
                    }
                    else
                    {
                        // Get packed iterators
                        var clone1 = input.Clone();
                        var clone2 = input.Clone();

                        try
                        {
                            // Subsample the index terms
                            clone1.Seek(packedIndexStart);
                            
                            PackedInts.ReaderIterator termsDictOffsetsIter = PackedInts.GetReaderIterator(clone1,
                                PackedInts.DEFAULT_BUFFER_SIZE);

                            clone2.Seek(packedOffsetsStart);
                            
                            PackedInts.ReaderIterator termOffsetsIter = PackedInts.GetReaderIterator(clone2,
                                PackedInts.DEFAULT_BUFFER_SIZE);

                            // TODO: often we can get by w/ fewer bits per
                            // value, below.. .but this'd be more complex:
                            // we'd have to try @ fewer bits and then grow
                            // if we overflowed it.

                            PackedInts.Mutable termsDictOffsetsM = PackedInts.GetMutable(this.numIndexTerms,
                                termsDictOffsetsIter.BitsPerValue, PackedInts.DEFAULT);
                            PackedInts.Mutable termOffsetsM = PackedInts.GetMutable(this.numIndexTerms + 1,
                                termOffsetsIter.BitsPerValue, PackedInts.DEFAULT);

                            termsDictOffsets = termsDictOffsetsM;
                            termOffsets = termOffsetsM;

                            int upto = 0;

                            long termOffsetUpto = 0;

                            while (upto < this.numIndexTerms)
                            {
                                // main file offset copies straight over
                                termsDictOffsetsM.Set(upto, termsDictOffsetsIter.Next());

                                termOffsetsM.Set(upto, termOffsetUpto);

                                long termOffset = termOffsetsIter.Next();
                                long nextTermOffset = termOffsetsIter.Next();
                                int numTermBytes = (int) (nextTermOffset - termOffset);

                                clone.Seek(indexStart + termOffset);
                                
                                Debug.Assert(indexStart + termOffset < clone.Length(),
                                    String.Format("IndexStart: {0}, TermOffset: {1}, Len: {2}", indexStart, termOffset,
                                        clone.Length()));
                                
                                Debug.Assert(indexStart + termOffset + numTermBytes < clone.Length());

                                termBytes.Copy(clone, numTermBytes);
                                termOffsetUpto += numTermBytes;

                                upto++;
                                if (upto == this.numIndexTerms)
                                {
                                    break;
                                }

                                // skip terms:
                                termsDictOffsetsIter.Next();
                                for (int i = 0; i < indexDivisor - 2; i++)
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

                /** Returns approximate RAM bytes Used */

                public long RamBytesUsed()
                {
                    return ((termOffsets != null) ? termOffsets.RamBytesUsed() : 0) +
                           ((termsDictOffsets != null) ? termsDictOffsets.RamBytesUsed() : 0);
                }
            }
        }

        public override FieldIndexEnum GetFieldEnum(FieldInfo fieldInfo)
        {
            FieldIndexData fieldData = fields[fieldInfo];
            return fieldData.CoreIndex == null ? null : new IndexEnum(fieldData.CoreIndex);
        }

        public override void Dispose()
        {
            if (input != null && !indexLoaded)
                input.Dispose();
        }

        private void SeekDir(IndexInput input, long dirOffset)
        {
            if (version >= FixedGapTermsIndexWriter.VERSION_CHECKSUM)
            {
                input.Seek(input.Length() - CodecUtil.FooterLength() - 8);
                dirOffset = input.ReadLong();

            }
            else if (version >= FixedGapTermsIndexWriter.VERSION_APPEND_ONLY)
            {
                input.Seek(input.Length() - 8);
                dirOffset = input.ReadLong();
            }

            input.Seek(dirOffset);
        }

        public override long RamBytesUsed
        {
            get
            {
                var sizeInBytes = ((termBytes != null) ? termBytes.RamBytesUsed() : 0) +
                                  ((termBytesReader != null) ? termBytesReader.RamBytesUsed() : 0);

                return fields.Values.Aggregate(sizeInBytes,
                    (current, entry) => (long) (current + entry.CoreIndex.RamBytesUsed));
            }
        }

    }
}