using System.Collections.Generic;
using System.Diagnostics;

namespace Lucene.Net.Index
{
    using AppendingPackedLongBuffer = Lucene.Net.Util.Packed.AppendingPackedLongBuffer;
    using Bits = Lucene.Net.Util.Bits;
    using BytesRef = Lucene.Net.Util.BytesRef;
    using MonotonicAppendingLongBuffer = Lucene.Net.Util.Packed.MonotonicAppendingLongBuffer;
    using PackedInts = Lucene.Net.Util.Packed.PackedInts;

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

    using TermsEnumIndex = Lucene.Net.Index.MultiTermsEnum.TermsEnumIndex;
    using TermsEnumWithSlice = Lucene.Net.Index.MultiTermsEnum.TermsEnumWithSlice;

    /// <summary>
    /// A wrapper for CompositeIndexReader providing access to DocValues.
    ///
    /// <p><b>NOTE</b>: for multi readers, you'll get better
    /// performance by gathering the sub readers using
    /// <seealso cref="IndexReader#getContext()"/> to get the
    /// atomic leaves and then operate per-AtomicReader,
    /// instead of using this class.
    ///
    /// <p><b>NOTE</b>: this is very costly.
    ///
    /// @lucene.experimental
    /// @lucene.internal
    /// </summary>
    public class MultiDocValues
    {
        /// <summary>
        /// No instantiation </summary>
        private MultiDocValues()
        {
        }

        /// <summary>
        /// Returns a NumericDocValues for a reader's norms (potentially merging on-the-fly).
        /// <p>
        /// this is a slow way to access normalization values. Instead, access them per-segment
        /// with <seealso cref="AtomicReader#getNormValues(String)"/>
        /// </p>
        /// </summary>
        public static NumericDocValues GetNormValues(IndexReader r, string field)
        {
            IList<AtomicReaderContext> leaves = r.Leaves;
            int size = leaves.Count;
            if (size == 0)
            {
                return null;
            }
            else if (size == 1)
            {
                return leaves[0].AtomicReader.GetNormValues(field);
            }
            FieldInfo fi = MultiFields.GetMergedFieldInfos(r).FieldInfo(field);
            if (fi == null || fi.HasNorms() == false)
            {
                return null;
            }

            bool anyReal = false;
            NumericDocValues[] values = new NumericDocValues[size];
            int[] starts = new int[size + 1];
            for (int i = 0; i < size; i++)
            {
                AtomicReaderContext context = leaves[i];
                NumericDocValues v = context.AtomicReader.GetNormValues(field);
                if (v == null)
                {
                    v = DocValues.EMPTY_NUMERIC;
                }
                else
                {
                    anyReal = true;
                }
                values[i] = v;
                starts[i] = context.DocBase;
            }
            starts[size] = r.MaxDoc;

            Debug.Assert(anyReal);

            return new NumericDocValuesAnonymousInnerClassHelper(values, starts);
        }

        private class NumericDocValuesAnonymousInnerClassHelper : NumericDocValues
        {
            private NumericDocValues[] values;
            private int[] starts;

            public NumericDocValuesAnonymousInnerClassHelper(NumericDocValues[] values, int[] starts)
            {
                this.values = values;
                this.starts = starts;
            }

            public override long Get(int docID)
            {
                int subIndex = ReaderUtil.SubIndex(docID, starts);
                return values[subIndex].Get(docID - starts[subIndex]);
            }
        }

        /// <summary>
        /// Returns a NumericDocValues for a reader's docvalues (potentially merging on-the-fly)
        /// <p>
        /// this is a slow way to access numeric values. Instead, access them per-segment
        /// with <seealso cref="AtomicReader#getNumericDocValues(String)"/>
        /// </p>
        ///
        /// </summary>
        public static NumericDocValues GetNumericValues(IndexReader r, string field)
        {
            IList<AtomicReaderContext> leaves = r.Leaves;
            int size = leaves.Count;
            if (size == 0)
            {
                return null;
            }
            else if (size == 1)
            {
                return leaves[0].AtomicReader.GetNumericDocValues(field);
            }

            bool anyReal = false;
            NumericDocValues[] values = new NumericDocValues[size];
            int[] starts = new int[size + 1];
            for (int i = 0; i < size; i++)
            {
                AtomicReaderContext context = leaves[i];
                NumericDocValues v = context.AtomicReader.GetNumericDocValues(field);
                if (v == null)
                {
                    v = DocValues.EMPTY_NUMERIC;
                }
                else
                {
                    anyReal = true;
                }
                values[i] = v;
                starts[i] = context.DocBase;
            }
            starts[size] = r.MaxDoc;

            if (!anyReal)
            {
                return null;
            }
            else
            {
                return new NumericDocValuesAnonymousInnerClassHelper2(values, starts);
            }
        }

        private class NumericDocValuesAnonymousInnerClassHelper2 : NumericDocValues
        {
            private NumericDocValues[] values;
            private int[] starts;

            public NumericDocValuesAnonymousInnerClassHelper2(NumericDocValues[] values, int[] starts)
            {
                this.values = values;
                this.starts = starts;
            }

            public override long Get(int docID)
            {
                int subIndex = ReaderUtil.SubIndex(docID, starts);
                return values[subIndex].Get(docID - starts[subIndex]);
            }
        }

        /// <summary>
        /// Returns a Bits for a reader's docsWithField (potentially merging on-the-fly)
        /// <p>
        /// this is a slow way to access this bitset. Instead, access them per-segment
        /// with <seealso cref="AtomicReader#getDocsWithField(String)"/>
        /// </p>
        ///
        /// </summary>
        public static Bits GetDocsWithField(IndexReader r, string field)
        {
            IList<AtomicReaderContext> leaves = r.Leaves;
            int size = leaves.Count;
            if (size == 0)
            {
                return null;
            }
            else if (size == 1)
            {
                return leaves[0].AtomicReader.GetDocsWithField(field);
            }

            bool anyReal = false;
            bool anyMissing = false;
            Bits[] values = new Bits[size];
            int[] starts = new int[size + 1];
            for (int i = 0; i < size; i++)
            {
                AtomicReaderContext context = leaves[i];
                Bits v = context.AtomicReader.GetDocsWithField(field);
                if (v == null)
                {
                    v = new Lucene.Net.Util.Bits_MatchNoBits(context.Reader.MaxDoc);
                    anyMissing = true;
                }
                else
                {
                    anyReal = true;
                    if (v is Lucene.Net.Util.Bits_MatchAllBits == false)
                    {
                        anyMissing = true;
                    }
                }
                values[i] = v;
                starts[i] = context.DocBase;
            }
            starts[size] = r.MaxDoc;

            if (!anyReal)
            {
                return null;
            }
            else if (!anyMissing)
            {
                return new Lucene.Net.Util.Bits_MatchAllBits(r.MaxDoc);
            }
            else
            {
                return new MultiBits(values, starts, false);
            }
        }

        /// <summary>
        /// Returns a BinaryDocValues for a reader's docvalues (potentially merging on-the-fly)
        /// <p>
        /// this is a slow way to access binary values. Instead, access them per-segment
        /// with <seealso cref="AtomicReader#getBinaryDocValues(String)"/>
        /// </p>
        /// </summary>
        public static BinaryDocValues GetBinaryValues(IndexReader r, string field)
        {
            IList<AtomicReaderContext> leaves = r.Leaves;
            int size = leaves.Count;

            if (size == 0)
            {
                return null;
            }
            else if (size == 1)
            {
                return leaves[0].AtomicReader.GetBinaryDocValues(field);
            }

            bool anyReal = false;
            BinaryDocValues[] values = new BinaryDocValues[size];
            int[] starts = new int[size + 1];
            for (int i = 0; i < size; i++)
            {
                AtomicReaderContext context = leaves[i];
                BinaryDocValues v = context.AtomicReader.GetBinaryDocValues(field);
                if (v == null)
                {
                    v = DocValues.EMPTY_BINARY;
                }
                else
                {
                    anyReal = true;
                }
                values[i] = v;
                starts[i] = context.DocBase;
            }
            starts[size] = r.MaxDoc;

            if (!anyReal)
            {
                return null;
            }
            else
            {
                return new BinaryDocValuesAnonymousInnerClassHelper(values, starts);
            }
        }

        private class BinaryDocValuesAnonymousInnerClassHelper : BinaryDocValues
        {
            private BinaryDocValues[] values;
            private int[] starts;

            public BinaryDocValuesAnonymousInnerClassHelper(BinaryDocValues[] values, int[] starts)
            {
                this.values = values;
                this.starts = starts;
            }

            public override void Get(int docID, BytesRef result)
            {
                int subIndex = ReaderUtil.SubIndex(docID, starts);
                values[subIndex].Get(docID - starts[subIndex], result);
            }
        }

        /// <summary>
        /// Returns a SortedDocValues for a reader's docvalues (potentially doing extremely slow things).
        /// <p>
        /// this is an extremely slow way to access sorted values. Instead, access them per-segment
        /// with <seealso cref="AtomicReader#getSortedDocValues(String)"/>
        /// </p>
        /// </summary>
        public static SortedDocValues GetSortedValues(IndexReader r, string field)
        {
            IList<AtomicReaderContext> leaves = r.Leaves;
            int size = leaves.Count;

            if (size == 0)
            {
                return null;
            }
            else if (size == 1)
            {
                return leaves[0].AtomicReader.GetSortedDocValues(field);
            }

            bool anyReal = false;
            var values = new SortedDocValues[size];
            int[] starts = new int[size + 1];
            for (int i = 0; i < size; i++)
            {
                AtomicReaderContext context = leaves[i];
                SortedDocValues v = context.AtomicReader.GetSortedDocValues(field);
                if (v == null)
                {
                    v = DocValues.EMPTY_SORTED;
                }
                else
                {
                    anyReal = true;
                }
                values[i] = v;
                starts[i] = context.DocBase;
            }
            starts[size] = r.MaxDoc;

            if (!anyReal)
            {
                return null;
            }
            else
            {
                TermsEnum[] enums = new TermsEnum[values.Length];
                for (int i = 0; i < values.Length; i++)
                {
                    enums[i] = values[i].TermsEnum();
                }
                OrdinalMap mapping = new OrdinalMap(r.CoreCacheKey, enums);
                return new MultiSortedDocValues(values, starts, mapping);
            }
        }

        /// <summary>
        /// Returns a SortedSetDocValues for a reader's docvalues (potentially doing extremely slow things).
        /// <p>
        /// this is an extremely slow way to access sorted values. Instead, access them per-segment
        /// with <seealso cref="AtomicReader#getSortedSetDocValues(String)"/>
        /// </p>
        /// </summary>
        public static SortedSetDocValues GetSortedSetValues(IndexReader r, string field)
        {
            IList<AtomicReaderContext> leaves = r.Leaves;
            int size = leaves.Count;

            if (size == 0)
            {
                return null;
            }
            else if (size == 1)
            {
                return leaves[0].AtomicReader.GetSortedSetDocValues(field);
            }

            bool anyReal = false;
            SortedSetDocValues[] values = new SortedSetDocValues[size];
            int[] starts = new int[size + 1];
            for (int i = 0; i < size; i++)
            {
                AtomicReaderContext context = leaves[i];
                SortedSetDocValues v = context.AtomicReader.GetSortedSetDocValues(field);
                if (v == null)
                {
                    v = DocValues.EMPTY_SORTED_SET;
                }
                else
                {
                    anyReal = true;
                }
                values[i] = v;
                starts[i] = context.DocBase;
            }
            starts[size] = r.MaxDoc;

            if (!anyReal)
            {
                return null;
            }
            else
            {
                TermsEnum[] enums = new TermsEnum[values.Length];
                for (int i = 0; i < values.Length; i++)
                {
                    enums[i] = values[i].TermsEnum();
                }
                OrdinalMap mapping = new OrdinalMap(r.CoreCacheKey, enums);
                return new MultiSortedSetDocValues(values, starts, mapping);
            }
        }

        /// <summary>
        /// maps per-segment ordinals to/from global ordinal space </summary>
        // TODO: use more efficient packed ints structures?
        // TODO: pull this out? its pretty generic (maps between N ord()-enabled TermsEnums)
        public class OrdinalMap
        {
            // cache key of whoever asked for this awful thing
            internal readonly object owner;

            // globalOrd -> (globalOrd - segmentOrd) where segmentOrd is the the ordinal in the first segment that contains this term
            internal readonly MonotonicAppendingLongBuffer globalOrdDeltas;

            // globalOrd -> first segment container
            internal readonly AppendingPackedLongBuffer firstSegments;

            // for every segment, segmentOrd -> (globalOrd - segmentOrd)
            internal readonly MonotonicAppendingLongBuffer[] ordDeltas;

            /// <summary>
            /// Creates an ordinal map that allows mapping ords to/from a merged
            /// space from <code>subs</code>. </summary>
            /// <param name="owner"> a cache key </param>
            /// <param name="subs"> TermsEnums that support <seealso cref="TermsEnum#ord()"/>. They need
            ///             not be dense (e.g. can be FilteredTermsEnums}. </param>
            /// <exception cref="IOException"> if an I/O error occurred. </exception>
            public OrdinalMap(object owner, TermsEnum[] subs)
            {
                // create the ordinal mappings by pulling a termsenum over each sub's
                // unique terms, and walking a multitermsenum over those
                this.owner = owner;
                globalOrdDeltas = new MonotonicAppendingLongBuffer(PackedInts.COMPACT);
                firstSegments = new AppendingPackedLongBuffer(PackedInts.COMPACT);
                ordDeltas = new MonotonicAppendingLongBuffer[subs.Length];
                for (int i = 0; i < ordDeltas.Length; i++)
                {
                    ordDeltas[i] = new MonotonicAppendingLongBuffer();
                }
                long[] segmentOrds = new long[subs.Length];
                ReaderSlice[] slices = new ReaderSlice[subs.Length];
                TermsEnumIndex[] indexes = new TermsEnumIndex[slices.Length];
                for (int i = 0; i < slices.Length; i++)
                {
                    slices[i] = new ReaderSlice(0, 0, i);
                    indexes[i] = new TermsEnumIndex(subs[i], i);
                }
                MultiTermsEnum mte = new MultiTermsEnum(slices);
                mte.Reset(indexes);
                long globalOrd = 0;
                while (mte.Next() != null)
                {
                    TermsEnumWithSlice[] matches = mte.MatchArray;
                    for (int i = 0; i < mte.MatchCount; i++)
                    {
                        int segmentIndex = matches[i].Index;
                        long segmentOrd = matches[i].Terms.Ord();
                        long delta = globalOrd - segmentOrd;
                        // for each unique term, just mark the first segment index/delta where it occurs
                        if (i == 0)
                        {
                            firstSegments.Add(segmentIndex);
                            globalOrdDeltas.Add(delta);
                        }
                        // for each per-segment ord, map it back to the global term.
                        while (segmentOrds[segmentIndex] <= segmentOrd)
                        {
                            ordDeltas[segmentIndex].Add(delta);
                            segmentOrds[segmentIndex]++;
                        }
                    }
                    globalOrd++;
                }
                firstSegments.Freeze();
                globalOrdDeltas.Freeze();
                for (int i = 0; i < ordDeltas.Length; ++i)
                {
                    ordDeltas[i].Freeze();
                }
            }

            /// <summary>
            /// Given a segment number and segment ordinal, returns
            /// the corresponding global ordinal.
            /// </summary>
            public virtual long GetGlobalOrd(int segmentIndex, long segmentOrd)
            {
                return segmentOrd + ordDeltas[segmentIndex].Get(segmentOrd);
            }

            /// <summary>
            /// Given global ordinal, returns the ordinal of the first segment which contains
            /// this ordinal (the corresponding to the segment return <seealso cref="#getFirstSegmentNumber"/>).
            /// </summary>
            public virtual long GetFirstSegmentOrd(long globalOrd)
            {
                return globalOrd - globalOrdDeltas.Get(globalOrd);
            }

            /// <summary>
            /// Given a global ordinal, returns the index of the first
            /// segment that contains this term.
            /// </summary>
            public virtual int GetFirstSegmentNumber(long globalOrd)
            {
                return (int)firstSegments.Get(globalOrd);
            }

            /// <summary>
            /// Returns the total number of unique terms in global ord space.
            /// </summary>
            public virtual long ValueCount
            {
                get
                {
                    return globalOrdDeltas.Size();
                }
            }

            /// <summary>
            /// Returns total byte size used by this ordinal map.
            /// </summary>
            public virtual long RamBytesUsed()
            {
                long size = globalOrdDeltas.RamBytesUsed() + firstSegments.RamBytesUsed();
                for (int i = 0; i < ordDeltas.Length; i++)
                {
                    size += ordDeltas[i].RamBytesUsed();
                }
                return size;
            }
        }

        /// <summary>
        /// Implements SortedDocValues over n subs, using an OrdinalMap
        /// @lucene.internal
        /// </summary>
        public class MultiSortedDocValues : SortedDocValues
        {
            /// <summary>
            /// docbase for each leaf: parallel with <seealso cref="#values"/> </summary>
            public readonly int[] DocStarts; // LUCENENET TODO: Make getter method (properties shouldn't return array)

            /// <summary>
            /// leaf values </summary>
            public readonly SortedDocValues[] Values; // LUCENENET TODO: Make getter method

            /// <summary>
            /// ordinal map mapping ords from <code>values</code> to global ord space </summary>
            public OrdinalMap Mapping { get; private set; }

            /// <summary>
            /// Creates a new MultiSortedDocValues over <code>values</code> </summary>
            internal MultiSortedDocValues(SortedDocValues[] values, int[] docStarts, OrdinalMap mapping)
            {
                Debug.Assert(values.Length == mapping.ordDeltas.Length);
                Debug.Assert(docStarts.Length == values.Length + 1);
                this.Values = values;
                this.DocStarts = docStarts;
                this.Mapping = mapping;
            }

            public override int GetOrd(int docID)
            {
                int subIndex = ReaderUtil.SubIndex(docID, DocStarts);
                int segmentOrd = Values[subIndex].GetOrd(docID - DocStarts[subIndex]);
                return segmentOrd == -1 ? segmentOrd : (int)Mapping.GetGlobalOrd(subIndex, segmentOrd);
            }

            public override void LookupOrd(int ord, BytesRef result)
            {
                int subIndex = Mapping.GetFirstSegmentNumber(ord);
                int segmentOrd = (int)Mapping.GetFirstSegmentOrd(ord);
                Values[subIndex].LookupOrd(segmentOrd, result);
            }

            public override int ValueCount
            {
                get
                {
                    return (int)Mapping.ValueCount;
                }
            }
        }

        /// <summary>
        /// Implements MultiSortedSetDocValues over n subs, using an OrdinalMap
        /// @lucene.internal
        /// </summary>
        public class MultiSortedSetDocValues : SortedSetDocValues
        {
            /// <summary>
            /// docbase for each leaf: parallel with <seealso cref="#values"/> </summary>
            public readonly int[] DocStarts; // LUCENENET TODO: Make getter method (properties should not return array)

            /// <summary>
            /// leaf values </summary>
            public readonly SortedSetDocValues[] Values; // LUCENENET TODO: Make getter method

            /// <summary>
            /// ordinal map mapping ords from <code>values</code> to global ord space </summary>
            public OrdinalMap Mapping { get; private set; }

            internal int CurrentSubIndex;

            /// <summary>
            /// Creates a new MultiSortedSetDocValues over <code>values</code> </summary>
            internal MultiSortedSetDocValues(SortedSetDocValues[] values, int[] docStarts, OrdinalMap mapping)
            {
                Debug.Assert(values.Length == mapping.ordDeltas.Length);
                Debug.Assert(docStarts.Length == values.Length + 1);
                this.Values = values;
                this.DocStarts = docStarts;
                this.Mapping = mapping;
            }

            public override long NextOrd()
            {
                long segmentOrd = Values[CurrentSubIndex].NextOrd();
                if (segmentOrd == NO_MORE_ORDS)
                {
                    return segmentOrd;
                }
                else
                {
                    return Mapping.GetGlobalOrd(CurrentSubIndex, segmentOrd);
                }
            }

            public override int Document
            {
                set
                {
                    CurrentSubIndex = ReaderUtil.SubIndex(value, DocStarts);
                    Values[CurrentSubIndex].Document = value - DocStarts[CurrentSubIndex];
                }
            }

            public override void LookupOrd(long ord, BytesRef result)
            {
                int subIndex = Mapping.GetFirstSegmentNumber(ord);
                long segmentOrd = Mapping.GetFirstSegmentOrd(ord);
                Values[subIndex].LookupOrd(segmentOrd, result);
            }

            public override long ValueCount
            {
                get
                {
                    return Mapping.ValueCount;
                }
            }
        }
    }
}