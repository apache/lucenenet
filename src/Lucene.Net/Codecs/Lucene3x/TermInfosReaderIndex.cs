using J2N.Numerics;
using J2N.Text;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using JCG = J2N.Collections.Generic;

namespace Lucene.Net.Codecs.Lucene3x
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

    using BytesRef = Lucene.Net.Util.BytesRef;
    using GrowableWriter = Lucene.Net.Util.Packed.GrowableWriter;
    using MathUtil = Lucene.Net.Util.MathUtil;
    using PackedInt32s = Lucene.Net.Util.Packed.PackedInt32s;
    using PagedBytes = Lucene.Net.Util.PagedBytes;
    using PagedBytesDataInput = Lucene.Net.Util.PagedBytes.PagedBytesDataInput;
    using PagedBytesDataOutput = Lucene.Net.Util.PagedBytes.PagedBytesDataOutput;
    using RamUsageEstimator = Lucene.Net.Util.RamUsageEstimator;
    using Term = Lucene.Net.Index.Term;

    /// <summary>
    /// This stores a monotonically increasing set of <c>Term, TermInfo</c> pairs in an
    /// index segment. Pairs are accessed either by <see cref="Term"/> or by ordinal position the
    /// set. The <see cref="Index.Terms"/> and <see cref="TermInfo"/> are actually serialized and stored into a byte
    /// array and pointers to the position of each are stored in a <see cref="int"/> array. </summary>
    [Obsolete("Only for reading existing 3.x indexes")]
    internal class TermInfosReaderIndex
    {
        private const int MAX_PAGE_BITS = 18; // 256 KB block
        private readonly Term[] fields; // LUCENENET: marked readonly
        private readonly int totalIndexInterval; // LUCENENET: marked readonly
        private readonly IComparer<BytesRef> comparer = BytesRef.UTF8SortedAsUTF16Comparer; // LUCENENET: marked readonly
        private readonly PagedBytesDataInput dataInput;
        private readonly PackedInt32s.Reader indexToDataOffset;
        private readonly int indexSize;
        private readonly int skipInterval;
        private readonly long ramBytesUsed;

        /// <summary>
        /// Loads the segment information at segment load time.
        /// </summary>
        /// <param name="indexEnum">
        ///          The term enum. </param>
        /// <param name="indexDivisor">
        ///          The index divisor. </param>
        /// <param name="tiiFileLength">
        ///          The size of the tii file, used to approximate the size of the
        ///          buffer. </param>
        /// <param name="totalIndexInterval">
        ///          The total index interval. </param>
        public TermInfosReaderIndex(SegmentTermEnum indexEnum, int indexDivisor, long tiiFileLength, int totalIndexInterval)
        {
            this.totalIndexInterval = totalIndexInterval;
            indexSize = 1 + ((int)indexEnum.size - 1) / indexDivisor;
            skipInterval = indexEnum.skipInterval;
            // this is only an inital size, it will be GCed once the build is complete
            long initialSize = (long)(tiiFileLength * 1.5) / indexDivisor;
            PagedBytes dataPagedBytes = new PagedBytes(EstimatePageBits(initialSize));
            PagedBytesDataOutput dataOutput = dataPagedBytes.GetDataOutput();

            int bitEstimate = 1 + MathUtil.Log(tiiFileLength, 2);
            GrowableWriter indexToTerms = new GrowableWriter(bitEstimate, indexSize, PackedInt32s.DEFAULT);

            string currentField = null;
            IList<string> fieldStrs = new JCG.List<string>();
            int fieldCounter = -1;
            for (int i = 0; indexEnum.Next(); i++)
            {
                Term term = indexEnum.Term();
                if (currentField is null || !currentField.Equals(term.Field, StringComparison.Ordinal))
                {
                    currentField = term.Field;
                    fieldStrs.Add(currentField);
                    fieldCounter++;
                }
                TermInfo termInfo = indexEnum.TermInfo();
                indexToTerms.Set(i, dataOutput.GetPosition());
                dataOutput.WriteVInt32(fieldCounter);
                dataOutput.WriteString(term.Text);
                dataOutput.WriteVInt32(termInfo.DocFreq);
                if (termInfo.DocFreq >= skipInterval)
                {
                    dataOutput.WriteVInt32(termInfo.SkipOffset);
                }
                dataOutput.WriteVInt64(termInfo.FreqPointer);
                dataOutput.WriteVInt64(termInfo.ProxPointer);
                dataOutput.WriteVInt64(indexEnum.indexPointer);
                for (int j = 1; j < indexDivisor; j++)
                {
                    if (!indexEnum.Next())
                    {
                        break;
                    }
                }
            }

            fields = new Term[fieldStrs.Count];
            for (int i = 0; i < fields.Length; i++)
            {
                fields[i] = new Term(fieldStrs[i]);
            }

            dataPagedBytes.Freeze(true);
            dataInput = dataPagedBytes.GetDataInput();
            indexToDataOffset = indexToTerms.Mutable;

            ramBytesUsed = fields.Length * (RamUsageEstimator.NUM_BYTES_OBJECT_REF + RamUsageEstimator.ShallowSizeOfInstance(typeof(Term))) + dataPagedBytes.RamBytesUsed() + indexToDataOffset.RamBytesUsed();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int EstimatePageBits(long estSize)
        {
            return Math.Max(Math.Min(64 - estSize.LeadingZeroCount(), MAX_PAGE_BITS), 4);
        }

        internal virtual void SeekEnum(SegmentTermEnum enumerator, int indexOffset)
        {
            PagedBytesDataInput input = (PagedBytesDataInput)dataInput.Clone();

            input.SetPosition(indexToDataOffset.Get(indexOffset));

            // read the term
            int fieldId = input.ReadVInt32();
            Term field = fields[fieldId];
            Term term = new Term(field.Field, input.ReadString());

            // read the terminfo
            var termInfo = new TermInfo();
            termInfo.DocFreq = input.ReadVInt32();
            if (termInfo.DocFreq >= skipInterval)
            {
                termInfo.SkipOffset = input.ReadVInt32();
            }
            else
            {
                termInfo.SkipOffset = 0;
            }
            termInfo.FreqPointer = input.ReadVInt64();
            termInfo.ProxPointer = input.ReadVInt64();

            long pointer = input.ReadVInt64();

            // perform the seek
            enumerator.Seek(pointer, ((long)indexOffset * totalIndexInterval) - 1, term, termInfo);
        }

        /// <summary>
        /// Binary search for the given term.
        /// </summary>
        /// <param name="term">
        ///          The term to locate. </param>
        /// <exception cref="IOException"> If there is a low-level I/O error. </exception>
        internal virtual int GetIndexOffset(Term term)
        {
            int lo = 0;
            int hi = indexSize - 1;
            PagedBytesDataInput input = (PagedBytesDataInput)dataInput.Clone();
            BytesRef scratch = new BytesRef();
            while (hi >= lo)
            {
                int mid = (lo + hi).TripleShift(1);
                int delta = CompareTo(term, mid, input, scratch);
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
                    return mid;
                }
            }
            return hi;
        }

        /// <summary>
        /// Gets the term at the given position.  For testing.
        /// </summary>
        /// <param name="termIndex">
        ///          The position to read the term from the index. </param>
        /// <returns> The term. </returns>
        /// <exception cref="IOException"> If there is a low-level I/O error. </exception>
        internal virtual Term GetTerm(int termIndex)
        {
            PagedBytesDataInput input = (PagedBytesDataInput)dataInput.Clone();
            input.SetPosition(indexToDataOffset.Get(termIndex));

            // read the term
            int fieldId = input.ReadVInt32();
            Term field = fields[fieldId];
            return new Term(field.Field, input.ReadString());
        }

        /// <summary>
        /// Returns the number of terms.
        /// </summary>
        /// <returns> int. </returns>
        internal virtual int Length => indexSize;

        /// <summary>
        /// The compares the given term against the term in the index specified by the
        /// term index. ie It returns negative N when term is less than index term;
        /// </summary>
        /// <param name="term">
        ///          The given term. </param>
        /// <param name="termIndex">
        ///          The index of the of term to compare. </param>
        /// <returns> int. </returns>
        /// <exception cref="IOException"> If there is a low-level I/O error. </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal virtual int CompareTo(Term term, int termIndex)
        {
            return CompareTo(term, termIndex, (PagedBytesDataInput)dataInput.Clone(), new BytesRef());
        }

        /// <summary>
        /// Compare the fields of the terms first, and if not equals return from
        /// compare. If equal compare terms.
        /// </summary>
        /// <param name="term">
        ///          The term to compare. </param>
        /// <param name="termIndex">
        ///          The position of the term in the input to compare </param>
        /// <param name="input">
        ///          The input buffer. </param>
        /// <returns> int. </returns>
        /// <exception cref="IOException"> If there is a low-level I/O error. </exception>
        private int CompareTo(Term term, int termIndex, PagedBytesDataInput input, BytesRef reuse)
        {
            // if term field does not equal mid's field index, then compare fields
            // else if they are equal, compare term's string values...
            int c = CompareField(term, termIndex, input);
            if (c == 0)
            {
                reuse.Length = input.ReadVInt32();
                reuse.Grow(reuse.Length);
                input.ReadBytes(reuse.Bytes, 0, reuse.Length);
                return comparer.Compare(term.Bytes, reuse);
            }
            return c;
        }

        /// <summary>
        /// Compares the fields before checking the text of the terms.
        /// </summary>
        /// <param name="term">
        ///          The given term. </param>
        /// <param name="termIndex">
        ///          The term that exists in the data block. </param>
        /// <param name="input">
        ///          The data block. </param>
        /// <returns> int. </returns>
        /// <exception cref="IOException"> If there is a low-level I/O error. </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int CompareField(Term term, int termIndex, PagedBytesDataInput input)
        {
            input.SetPosition(indexToDataOffset.Get(termIndex));
            return term.Field.CompareToOrdinal(fields[input.ReadVInt32()].Field);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal virtual long RamBytesUsed()
        {
            return ramBytesUsed;
        }
    }
}