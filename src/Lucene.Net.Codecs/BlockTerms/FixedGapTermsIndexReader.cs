using J2N.Numerics;
using Lucene.Net.Diagnostics;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Support;
using Lucene.Net.Util;
using Lucene.Net.Util.Packed;
using System.Collections.Generic;
using System.Diagnostics;

namespace Lucene.Net.Codecs.BlockTerms
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
    /// <see cref="TermsIndexReaderBase"/> for simple every Nth terms indexes.
    /// <para/>
    /// @lucene.experimental
    /// </summary>
    /// <seealso cref="FixedGapTermsIndexWriter"/>
    public class FixedGapTermsIndexReader : TermsIndexReaderBase
    {
        // NOTE: long is overkill here, since this number is 128
        // by default and only indexDivisor * 128 if you change
        // the indexDivisor at search time.  But, we use this in a
        // number of places to multiply out the actual ord, and we
        // will overflow int during those multiplies.  So to avoid
        // having to upgrade each multiple to long in multiple
        // places (error prone), we use long here:
        private readonly long totalIndexInterval;

        private readonly int indexDivisor;
        private readonly int indexInterval;

        // Closed if indexLoaded is true:
        private readonly IndexInput input;
        private readonly /*volatile*/ bool indexLoaded;

        private readonly IComparer<BytesRef> termComp;

        private const int PAGED_BYTES_BITS = 15;

        // all fields share this single logical byte[]
        private readonly PagedBytes termBytes = new PagedBytes(PAGED_BYTES_BITS);
        private readonly PagedBytes.Reader termBytesReader;

        private readonly IDictionary<FieldInfo, FieldIndexData> fields = new Dictionary<FieldInfo, FieldIndexData>();

        // start of the field info data
        private long dirOffset;

        private readonly int version;

        public FixedGapTermsIndexReader(Directory dir, FieldInfos fieldInfos, string segment, int indexDivisor,
            IComparer<BytesRef> termComp, string segmentSuffix, IOContext context)
        {
            this.termComp = termComp;

            if (Debugging.AssertsEnabled) Debugging.Assert(indexDivisor == -1 || indexDivisor > 0);

            input = dir.OpenInput(IndexFileNames.SegmentFileName(segment, segmentSuffix, FixedGapTermsIndexWriter.TERMS_INDEX_EXTENSION), context);

            bool success = false;

            try
            {
                version = ReadHeader(input);

                if (version >= FixedGapTermsIndexWriter.VERSION_CHECKSUM)
                {
                    CodecUtil.ChecksumEntireFile(input);
                }

                indexInterval = input.ReadInt32();
                if (indexInterval < 1)
                {
                    throw new CorruptIndexException("invalid indexInterval: " + indexInterval + " (resource=" + input + ")");
                }
                this.indexDivisor = indexDivisor;

                if (indexDivisor < 0)
                {
                    totalIndexInterval = indexInterval;
                }
                else
                {
                    // In case terms index gets loaded, later, on demand
                    totalIndexInterval = indexInterval * indexDivisor;
                }
                if (Debugging.AssertsEnabled) Debugging.Assert(totalIndexInterval > 0);

                SeekDir(input, dirOffset);

                // Read directory
                int numFields = input.ReadVInt32();
                if (numFields < 0)
                {
                    throw new CorruptIndexException("invalid numFields: " + numFields + " (resource=" + input + ")");
                }
                //System.out.println("FGR: init seg=" + segment + " div=" + indexDivisor + " nF=" + numFields);
                for (int i = 0; i < numFields; i++)
                {
                    int field = input.ReadVInt32();
                    int numIndexTerms = input.ReadVInt32();
                    if (numIndexTerms < 0)
                    {
                        throw new CorruptIndexException("invalid numIndexTerms: " + numIndexTerms + " (resource=" + input + ")");
                    }
                    long termsStart = input.ReadVInt64();
                    long indexStart = input.ReadVInt64();
                    long packedIndexStart = input.ReadVInt64();
                    long packedOffsetsStart = input.ReadVInt64();
                    if (packedIndexStart < indexStart)
                    {
                        throw new CorruptIndexException("invalid packedIndexStart: " + packedIndexStart + " indexStart: " + indexStart + "numIndexTerms: " + numIndexTerms + " (resource=" + input + ")");
                    }
                    FieldInfo fieldInfo = fieldInfos.FieldInfo(field);
                    FieldIndexData previous = fields.Put(fieldInfo, new FieldIndexData(this, /* fieldInfo, // LUCENENET: Not referenced */
                        numIndexTerms, indexStart, termsStart, packedIndexStart, packedOffsetsStart));
                    if (previous != null)
                    {
                        throw new CorruptIndexException("duplicate field: " + fieldInfo.Name + " (resource=" + input + ")");
                    }
                }
                success = true;
            }
            finally
            {
                if (!success)
                {
                    IOUtils.DisposeWhileHandlingException(input);
                }
                if (indexDivisor > 0)
                {
                    input.Dispose();
                    input = null;
                    if (success)
                    {
                        indexLoaded = true;
                    }
                    termBytesReader = termBytes.Freeze(true);
                }
            }
        }

        public override int Divisor => indexDivisor;

        private int ReadHeader(DataInput input)
        {
            int version = CodecUtil.CheckHeader(input, FixedGapTermsIndexWriter.CODEC_NAME,
                FixedGapTermsIndexWriter.VERSION_START, FixedGapTermsIndexWriter.VERSION_CURRENT);
            if (version < FixedGapTermsIndexWriter.VERSION_APPEND_ONLY)
            {
                dirOffset = input.ReadInt64();
            }
            return version;
        }

        private class IndexEnum : FieldIndexEnum
        {
            // Outer intstance
            private readonly FixedGapTermsIndexReader outerInstance;

            private readonly FieldIndexData.CoreFieldIndex fieldIndex;
            private readonly BytesRef term = new BytesRef();
            private long ord;

            public IndexEnum(FixedGapTermsIndexReader outerInstance, FieldIndexData.CoreFieldIndex fieldIndex)
            {
                this.outerInstance = outerInstance;
                this.fieldIndex = fieldIndex;
            }

            public override sealed BytesRef Term => term;

            public override long Seek(BytesRef target)
            {
                int lo = 0;          // binary search
                int hi = fieldIndex.numIndexTerms - 1;
                if (Debugging.AssertsEnabled) Debugging.Assert(outerInstance.totalIndexInterval > 0, "totalIndexInterval={0}", outerInstance.totalIndexInterval);

                while (hi >= lo)
                {
                    int mid = (lo + hi).TripleShift(1);

                    long offset2 = fieldIndex.termOffsets.Get(mid);
                    int length2 = (int)(fieldIndex.termOffsets.Get(1 + mid) - offset2);
                    outerInstance.termBytesReader.FillSlice(term, fieldIndex.termBytesStart + offset2, length2);

                    int delta = outerInstance.termComp.Compare(target, term);
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
                        if (Debugging.AssertsEnabled) Debugging.Assert(mid >= 0);
                        ord = mid * outerInstance.totalIndexInterval;
                        return fieldIndex.termsStart + fieldIndex.termsDictOffsets.Get(mid);
                    }
                }

                if (hi < 0)
                {
                    if (Debugging.AssertsEnabled) Debugging.Assert(hi == -1);
                    hi = 0;
                }

                long offset = fieldIndex.termOffsets.Get(hi);
                int length = (int)(fieldIndex.termOffsets.Get(1 + hi) - offset);
                outerInstance.termBytesReader.FillSlice(term, fieldIndex.termBytesStart + offset, length);

                ord = hi * outerInstance.totalIndexInterval;
                return fieldIndex.termsStart + fieldIndex.termsDictOffsets.Get(hi);
            }

            public override long Next()
            {
                int idx = 1 + (int)(ord / outerInstance.totalIndexInterval);
                if (idx >= fieldIndex.numIndexTerms)
                {
                    return -1;
                }
                ord += outerInstance.totalIndexInterval;

                long offset = fieldIndex.termOffsets.Get(idx);
                int length = (int)(fieldIndex.termOffsets.Get(1 + idx) - offset);
                outerInstance.termBytesReader.FillSlice(term, fieldIndex.termBytesStart + offset, length);
                return fieldIndex.termsStart + fieldIndex.termsDictOffsets.Get(idx);
            }

            public override long Ord => ord;

            public override long Seek(long ord)
            {
                int idx = (int)(ord / outerInstance.totalIndexInterval);
                // caller must ensure ord is in bounds
                if (Debugging.AssertsEnabled) Debugging.Assert(idx < fieldIndex.numIndexTerms);
                long offset = fieldIndex.termOffsets.Get(idx);
                int length = (int)(fieldIndex.termOffsets.Get(1 + idx) - offset);
                outerInstance.termBytesReader.FillSlice(term, fieldIndex.termBytesStart + offset, length);
                this.ord = idx * outerInstance.totalIndexInterval;
                return fieldIndex.termsStart + fieldIndex.termsDictOffsets.Get(idx);
            }
        }

        public override bool SupportsOrd => true;

        private class FieldIndexData
        {
            private readonly FixedGapTermsIndexReader outerInstance;

            internal volatile CoreFieldIndex coreIndex;

            private readonly long indexStart;
            private readonly long termsStart;
            private readonly long packedIndexStart;
            private readonly long packedOffsetsStart;

            private readonly int numIndexTerms;

            public FieldIndexData(FixedGapTermsIndexReader outerInstance, /*FieldInfo fieldInfo, // LUCENENET: Not Referenced */
                int numIndexTerms, long indexStart, long termsStart,
                long packedIndexStart, long packedOffsetsStart)
            {
                this.outerInstance = outerInstance;

                this.termsStart = termsStart;
                this.indexStart = indexStart;
                this.packedIndexStart = packedIndexStart;
                this.packedOffsetsStart = packedOffsetsStart;
                this.numIndexTerms = numIndexTerms;

                if (outerInstance.indexDivisor > 0)
                {
                    LoadTermsIndex();
                }
            }

            private void LoadTermsIndex()
            {
                if (coreIndex is null)
                {
                    coreIndex = new CoreFieldIndex(this, indexStart, termsStart, packedIndexStart, packedOffsetsStart, numIndexTerms);
                }
            }

            internal sealed class CoreFieldIndex
            {
                // where this field's terms begin in the packed byte[]
                // data
                internal readonly long termBytesStart;

                // offset into index termBytes
                internal readonly PackedInt32s.Reader termOffsets;

                // index pointers into main terms dict
                internal readonly PackedInt32s.Reader termsDictOffsets;

                internal readonly int numIndexTerms;
                internal readonly long termsStart;

                public CoreFieldIndex(FieldIndexData outerInstance, long indexStart, long termsStart, long packedIndexStart, long packedOffsetsStart,
                    int numIndexTerms)
                {
                    this.termsStart = termsStart;
                    termBytesStart = outerInstance.outerInstance.termBytes.GetPointer();

                    IndexInput clone = (IndexInput)outerInstance.outerInstance.input.Clone();
                    clone.Seek(indexStart);

                    // -1 is passed to mean "don't load term index", but
                    // if we are then later loaded it's overwritten with
                    // a real value
                    if (Debugging.AssertsEnabled) Debugging.Assert(outerInstance.outerInstance.indexDivisor > 0);

                    this.numIndexTerms = 1 + (numIndexTerms - 1) / outerInstance.outerInstance.indexDivisor;

                    if (Debugging.AssertsEnabled) Debugging.Assert(this.numIndexTerms > 0, "numIndexTerms={0} indexDivisor={1}", numIndexTerms, outerInstance.outerInstance.indexDivisor);

                    if (outerInstance.outerInstance.indexDivisor == 1)
                    {
                        // Default (load all index terms) is fast -- slurp in the images from disk:

                        try
                        {
                            long numTermBytes = packedIndexStart - indexStart;
                            outerInstance.outerInstance.termBytes.Copy(clone, numTermBytes);

                            // records offsets into main terms dict file
                            termsDictOffsets = PackedInt32s.GetReader(clone);
                            if (Debugging.AssertsEnabled) Debugging.Assert(termsDictOffsets.Count == numIndexTerms);

                            // records offsets into byte[] term data
                            termOffsets = PackedInt32s.GetReader(clone);
                            if (Debugging.AssertsEnabled) Debugging.Assert(termOffsets.Count == 1 + numIndexTerms);
                        }
                        finally
                        {
                            clone.Dispose();
                        }
                    }
                    else
                    {
                        // Get packed iterators
                        IndexInput clone1 = (IndexInput)outerInstance.outerInstance.input.Clone();
                        IndexInput clone2 = (IndexInput)outerInstance.outerInstance.input.Clone();

                        try
                        {
                            // Subsample the index terms
                            clone1.Seek(packedIndexStart);
                            PackedInt32s.IReaderIterator termsDictOffsetsIter = PackedInt32s.GetReaderIterator(clone1, PackedInt32s.DEFAULT_BUFFER_SIZE);

                            clone2.Seek(packedOffsetsStart);
                            PackedInt32s.IReaderIterator termOffsetsIter = PackedInt32s.GetReaderIterator(clone2, PackedInt32s.DEFAULT_BUFFER_SIZE);

                            // TODO: often we can get by w/ fewer bits per
                            // value, below.. .but this'd be more complex:
                            // we'd have to try @ fewer bits and then grow
                            // if we overflowed it.

                            PackedInt32s.Mutable termsDictOffsetsM = PackedInt32s.GetMutable(this.numIndexTerms, termsDictOffsetsIter.BitsPerValue, PackedInt32s.DEFAULT);
                            PackedInt32s.Mutable termOffsetsM = PackedInt32s.GetMutable(this.numIndexTerms + 1, termOffsetsIter.BitsPerValue, PackedInt32s.DEFAULT);

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
                                int numTermBytes = (int)(nextTermOffset - termOffset);

                                clone.Seek(indexStart + termOffset);
                                if (Debugging.AssertsEnabled)
                                {
                                    Debugging.Assert(indexStart + termOffset < clone.Length, "indexStart={0} termOffset={1} len={2}", indexStart, termOffset, clone.Length);
                                    Debugging.Assert(indexStart + termOffset + numTermBytes < clone.Length);
                                }

                                outerInstance.outerInstance.termBytes.Copy(clone, numTermBytes);
                                termOffsetUpto += numTermBytes;

                                upto++;
                                if (upto == this.numIndexTerms)
                                {
                                    break;
                                }

                                // skip terms:
                                termsDictOffsetsIter.Next();
                                for (int i = 0; i < outerInstance.outerInstance.indexDivisor - 2; i++)
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

                /// <summary>Returns approximate RAM bytes used.</summary>
                public long RamBytesUsed()
                {
                    return ((termOffsets != null) ? termOffsets.RamBytesUsed() : 0) +
                        ((termsDictOffsets != null) ? termsDictOffsets.RamBytesUsed() : 0);
                }
            }
        }

        public override FieldIndexEnum GetFieldEnum(FieldInfo fieldInfo)
        {
            if (!fields.TryGetValue(fieldInfo, out FieldIndexData fieldData) || fieldData is null || fieldData.coreIndex is null)
            {
                return null;
            }
            else
            {
                return new IndexEnum(this, fieldData.coreIndex);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (input != null && !indexLoaded)
                {
                    input.Dispose();
                }
            }
        }

        private void SeekDir(IndexInput input, long dirOffset)
        {
            if (version >= FixedGapTermsIndexWriter.VERSION_CHECKSUM)
            {
                input.Seek(input.Length - CodecUtil.FooterLength() - 8);
                dirOffset = input.ReadInt64();
            }
            else if (version >= FixedGapTermsIndexWriter.VERSION_APPEND_ONLY)
            {
                input.Seek(input.Length - 8);
                dirOffset = input.ReadInt64();
            }
            input.Seek(dirOffset);
        }

        public override long RamBytesUsed()
        {
            long sizeInBytes = ((termBytes != null) ? termBytes.RamBytesUsed() : 0) +
                ((termBytesReader != null) ? termBytesReader.RamBytesUsed() : 0);
            foreach (FieldIndexData entry in fields.Values)
            {
                sizeInBytes += entry.coreIndex.RamBytesUsed();
            }
            return sizeInBytes;
        }
    }
}