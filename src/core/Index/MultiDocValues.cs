using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lucene.Net.Util;
using Lucene.Net.Util.Packed;
using TermsEnumIndex = Lucene.Net.Index.MultiTermsEnum.TermsEnumIndex;
using TermsEnumWithSlice = Lucene.Net.Index.MultiTermsEnum.TermsEnumWithSlice;

namespace Lucene.Net.Index
{
    public static class MultiDocValues
    {
        private sealed class AnonymousNormNumericDocValues : NumericDocValues
        {
            private readonly int[] starts;
            private readonly NumericDocValues[] values;

            public AnonymousNormNumericDocValues(int[] starts, NumericDocValues[] values)
            {
                this.starts = starts;
                this.values = values;
            }

            public override long Get(int docID)
            {
                int subIndex = ReaderUtil.SubIndex(docID, starts);
                return values[subIndex].Get(docID - starts[subIndex]);
            }
        }

        public static NumericDocValues GetNormValues(IndexReader r, String field)
        {
            IList<AtomicReaderContext> leaves = r.Leaves;
            int size = leaves.Count;
            if (size == 0)
            {
                return null;
            }
            else if (size == 1)
            {
                return leaves[0].Reader.GetNormValues(field);
            }
            FieldInfo fi = MultiFields.GetMergedFieldInfos(r).FieldInfo(field);
            if (fi == null || fi.HasNorms == false)
            {
                return null;
            }

            bool anyReal = false;
            NumericDocValues[] values = new NumericDocValues[size];
            int[] starts = new int[size + 1];
            for (int i = 0; i < size; i++)
            {
                AtomicReaderContext context = leaves[i];
                NumericDocValues v = context.Reader.GetNormValues(field);
                if (v == null)
                {
                    v = NumericDocValues.EMPTY;
                }
                else
                {
                    anyReal = true;
                }
                values[i] = v;
                starts[i] = context.docBase;
            }
            starts[size] = r.MaxDoc;

            //assert anyReal;

            return new AnonymousNormNumericDocValues(starts, values);
        }

        private sealed class AnonymousNumericNumericDocValues : NumericDocValues
        {
            private readonly int[] starts;
            private readonly NumericDocValues[] values;

            public AnonymousNumericNumericDocValues(int[] starts, NumericDocValues[] values)
            {
                this.starts = starts;
                this.values = values;
            }

            public override long Get(int docID)
            {
                int subIndex = ReaderUtil.SubIndex(docID, starts);
                return values[subIndex].Get(docID - starts[subIndex]);
            }
        }

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
                return leaves[0].Reader.GetNumericDocValues(field);
            }

            bool anyReal = false;
            NumericDocValues[] values = new NumericDocValues[size];
            int[] starts = new int[size + 1];
            for (int i = 0; i < size; i++)
            {
                AtomicReaderContext context = leaves[i];
                NumericDocValues v = context.Reader.GetNumericDocValues(field);
                if (v == null)
                {
                    v = NumericDocValues.EMPTY;
                }
                else
                {
                    anyReal = true;
                }
                values[i] = v;
                starts[i] = context.docBase;
            }
            starts[size] = r.MaxDoc;

            if (!anyReal)
            {
                return null;
            }
            else
            {
                return new AnonymousNumericNumericDocValues(starts, values);
            }
        }

        private sealed class AnonymousBinaryDocValues : BinaryDocValues
        {
            private readonly int[] starts;
            private readonly BinaryDocValues[] values;

            public AnonymousBinaryDocValues(int[] starts, BinaryDocValues[] values)
            {
                this.starts = starts;
                this.values = values;
            }

            public override void Get(int docID, BytesRef result)
            {
                int subIndex = ReaderUtil.SubIndex(docID, starts);
                values[subIndex].Get(docID - starts[subIndex], result);
            }
        }

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
                return leaves[0].Reader.GetBinaryDocValues(field);
            }

            bool anyReal = false;
            BinaryDocValues[] values = new BinaryDocValues[size];
            int[] starts = new int[size + 1];
            for (int i = 0; i < size; i++)
            {
                AtomicReaderContext context = leaves[i];
                BinaryDocValues v = context.Reader.GetBinaryDocValues(field);
                if (v == null)
                {
                    v = BinaryDocValues.EMPTY;
                }
                else
                {
                    anyReal = true;
                }
                values[i] = v;
                starts[i] = context.docBase;
            }
            starts[size] = r.MaxDoc;

            if (!anyReal)
            {
                return null;
            }
            else
            {
                return new AnonymousBinaryDocValues(starts, values);
            }
        }

        public static SortedDocValues GetSortedValues(IndexReader r, String field)
        {
            IList<AtomicReaderContext> leaves = r.Leaves;
            int size = leaves.Count;

            if (size == 0)
            {
                return null;
            }
            else if (size == 1)
            {
                return leaves[0].Reader.GetSortedDocValues(field);
            }

            bool anyReal = false;
            SortedDocValues[] values = new SortedDocValues[size];
            int[] starts = new int[size + 1];
            for (int i = 0; i < size; i++)
            {
                AtomicReaderContext context = leaves[i];
                SortedDocValues v = context.Reader.GetSortedDocValues(field);
                if (v == null)
                {
                    v = SortedDocValues.EMPTY;
                }
                else
                {
                    anyReal = true;
                }
                values[i] = v;
                starts[i] = context.docBase;
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
                    enums[i] = values[i].TermsEnum;
                }
                OrdinalMap mapping = new OrdinalMap(r.CoreCacheKey, enums);
                return new MultiSortedDocValues(values, starts, mapping);
            }
        }

        public static SortedSetDocValues GetSortedSetValues(IndexReader r, String field)
        {
            IList<AtomicReaderContext> leaves = r.Leaves;
            int size = leaves.Count;

            if (size == 0)
            {
                return null;
            }
            else if (size == 1)
            {
                return leaves[0].Reader.GetSortedSetDocValues(field);
            }

            bool anyReal = false;
            SortedSetDocValues[] values = new SortedSetDocValues[size];
            int[] starts = new int[size + 1];
            for (int i = 0; i < size; i++)
            {
                AtomicReaderContext context = leaves[i];
                SortedSetDocValues v = context.Reader.GetSortedSetDocValues(field);
                if (v == null)
                {
                    v = SortedSetDocValues.EMPTY;
                }
                else
                {
                    anyReal = true;
                }
                values[i] = v;
                starts[i] = context.docBase;
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
                    enums[i] = values[i].TermsEnum;
                }
                OrdinalMap mapping = new OrdinalMap(r.CoreCacheKey, enums);
                return new MultiSortedSetDocValues(values, starts, mapping);
            }
        }

        public class OrdinalMap
        {
            internal readonly object owner;

            internal readonly MonotonicAppendingLongBuffer globalOrdDeltas;

            internal readonly AppendingLongBuffer subIndexes;

            internal readonly MonotonicAppendingLongBuffer[] ordDeltas;

            public OrdinalMap(object owner, TermsEnum[] subs)
            {
                // create the ordinal mappings by pulling a termsenum over each sub's 
                // unique terms, and walking a multitermsenum over those
                this.owner = owner;
                globalOrdDeltas = new MonotonicAppendingLongBuffer();
                subIndexes = new AppendingLongBuffer();
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
                        int subIndex = matches[i].index;
                        long segmentOrd = matches[i].terms.Ord;
                        long delta = globalOrd - segmentOrd;
                        // for each unique term, just mark the first subindex/delta where it occurs
                        if (i == 0)
                        {
                            subIndexes.Add(subIndex);
                            globalOrdDeltas.Add(delta);
                        }
                        // for each per-segment ord, map it back to the global term.
                        while (segmentOrds[subIndex] <= segmentOrd)
                        {
                            ordDeltas[subIndex].Add(delta);
                            segmentOrds[subIndex]++;
                        }
                    }
                    globalOrd++;
                }
            }

            public long GetGlobalOrd(int subIndex, long segmentOrd)
            {
                return segmentOrd + ordDeltas[subIndex].Get(segmentOrd);
            }

            public long GetSegmentOrd(int subIndex, long globalOrd)
            {
                return globalOrd - globalOrdDeltas.Get(globalOrd);
            }

            public int GetSegmentNumber(long globalOrd)
            {
                return (int)subIndexes.Get(globalOrd);
            }

            public long ValueCount
            {
                get { return globalOrdDeltas.Size; }
            }
        }

        public class MultiSortedDocValues : SortedDocValues
        {
            public readonly int[] docStarts;

            public readonly SortedDocValues[] values;

            public readonly OrdinalMap mapping;

            internal MultiSortedDocValues(SortedDocValues[] values, int[] docStarts, OrdinalMap mapping)
            {
                //assert values.length == mapping.ordDeltas.length;
                //assert docStarts.length == values.length + 1;
                this.values = values;
                this.docStarts = docStarts;
                this.mapping = mapping;
            }

            public override int GetOrd(int docID)
            {
                int subIndex = ReaderUtil.SubIndex(docID, docStarts);
                int segmentOrd = values[subIndex].GetOrd(docID - docStarts[subIndex]);
                return (int)mapping.GetGlobalOrd(subIndex, segmentOrd);
            }

            public override void LookupOrd(int ord, BytesRef result)
            {
                int subIndex = mapping.GetSegmentNumber(ord);
                int segmentOrd = (int)mapping.GetSegmentOrd(subIndex, ord);
                values[subIndex].LookupOrd(segmentOrd, result);
            }

            public override int ValueCount
            {
                get { return (int)mapping.ValueCount; }
            }
        }

        public class MultiSortedSetDocValues : SortedSetDocValues
        {
            public readonly int[] docStarts;
            public readonly SortedSetDocValues[] values;
            public readonly OrdinalMap mapping;
            internal int currentSubIndex;

            internal MultiSortedSetDocValues(SortedSetDocValues[] values, int[] docStarts, OrdinalMap mapping)
            {
                //assert values.length == mapping.ordDeltas.length;
                //assert docStarts.length == values.length + 1;
                this.values = values;
                this.docStarts = docStarts;
                this.mapping = mapping;
            }

            public override long NextOrd()
            {
                long segmentOrd = values[currentSubIndex].NextOrd();
                if (segmentOrd == NO_MORE_ORDS)
                {
                    return segmentOrd;
                }
                else
                {
                    return mapping.GetGlobalOrd(currentSubIndex, segmentOrd);
                }
            }

            public override void SetDocument(int docID)
            {
                currentSubIndex = ReaderUtil.SubIndex(docID, docStarts);
                values[currentSubIndex].SetDocument(docID - docStarts[currentSubIndex]);
            }

            public override void LookupOrd(long ord, BytesRef result)
            {
                int subIndex = mapping.GetSegmentNumber(ord);
                long segmentOrd = mapping.GetSegmentOrd(subIndex, ord);
                values[subIndex].LookupOrd(segmentOrd, result);
            }

            public override long ValueCount
            {
                get { return mapping.ValueCount; }
            }
        }
    }
}
