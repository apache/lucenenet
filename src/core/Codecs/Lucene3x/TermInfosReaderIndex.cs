using Lucene.Net.Index;
using Lucene.Net.Support;
using Lucene.Net.Util;
using Lucene.Net.Util.Packed;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Codecs.Lucene3x
{
    [Obsolete]
    internal class TermInfosReaderIndex
    {
        private const int MAX_PAGE_BITS = 18; // 256 KB block
        private Term[] fields;
        private int totalIndexInterval;
        private IComparer<BytesRef> comparator = BytesRef.UTF8SortedAsUTF16Comparer;
        private readonly PagedBytes.PagedBytesDataInput dataInput;
        private readonly PackedInts.Reader indexToDataOffset;
        private readonly int indexSize;
        private readonly int skipInterval;

        internal TermInfosReaderIndex(SegmentTermEnum indexEnum, int indexDivisor, long tiiFileLength, int totalIndexInterval)
        {
            this.totalIndexInterval = totalIndexInterval;
            indexSize = 1 + ((int)indexEnum.size - 1) / indexDivisor;
            skipInterval = indexEnum.skipInterval;
            // this is only an inital size, it will be GCed once the build is complete
            long initialSize = (long)(tiiFileLength * 1.5) / indexDivisor;
            PagedBytes dataPagedBytes = new PagedBytes(EstimatePageBits(initialSize));
            PagedBytes.PagedBytesDataOutput dataOutput = dataPagedBytes.GetDataOutput();

            int bitEstimate = 1 + MathUtil.Log(tiiFileLength, 2);
            GrowableWriter indexToTerms = new GrowableWriter(bitEstimate, indexSize, PackedInts.DEFAULT);

            String currentField = null;
            List<String> fieldStrs = new List<String>();
            int fieldCounter = -1;
            for (int i = 0; indexEnum.Next(); i++)
            {
                Term term = indexEnum.Term;
                if (currentField == null || !currentField.Equals(term.Field))
                {
                    currentField = term.Field;
                    fieldStrs.Add(currentField);
                    fieldCounter++;
                }
                TermInfo termInfo = indexEnum.TermInfo();
                indexToTerms.Set(i, dataOutput.Position);
                dataOutput.WriteVInt(fieldCounter);
                dataOutput.WriteString(term.Text);
                dataOutput.WriteVInt(termInfo.docFreq);
                if (termInfo.docFreq >= skipInterval)
                {
                    dataOutput.WriteVInt(termInfo.skipOffset);
                }
                dataOutput.WriteVLong(termInfo.freqPointer);
                dataOutput.WriteVLong(termInfo.proxPointer);
                dataOutput.WriteVLong(indexEnum.indexPointer);
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
            indexToDataOffset = (PackedInts.Reader)indexToTerms.Mutable;
        }

        private static int EstimatePageBits(long estSize)
        {
            return Math.Max(Math.Min(64 - Number.NumberOfLeadingZeros(estSize), MAX_PAGE_BITS), 4);
        }

        internal void SeekEnum(SegmentTermEnum enumerator, int indexOffset)
        {
            PagedBytes.PagedBytesDataInput input = (PagedBytes.PagedBytesDataInput)dataInput.Clone();

            input.Position = indexToDataOffset.Get(indexOffset);

            // read the term
            int fieldId = input.ReadVInt();
            Term field = fields[fieldId];
            Term term = new Term(field.Field, input.ReadString());

            // read the terminfo
            TermInfo termInfo = new TermInfo();
            termInfo.docFreq = input.ReadVInt();
            if (termInfo.docFreq >= skipInterval)
            {
                termInfo.skipOffset = input.ReadVInt();
            }
            else
            {
                termInfo.skipOffset = 0;
            }
            termInfo.freqPointer = input.ReadVLong();
            termInfo.proxPointer = input.ReadVLong();

            long pointer = input.ReadVLong();

            // perform the seek
            enumerator.Seek(pointer, ((long)indexOffset * totalIndexInterval) - 1, term, termInfo);
        }

        internal int GetIndexOffset(Term term)
        {
            int lo = 0;
            int hi = indexSize - 1;
            PagedBytes.PagedBytesDataInput input = (PagedBytes.PagedBytesDataInput)dataInput.Clone();
            BytesRef scratch = new BytesRef();
            while (hi >= lo)
            {
                int mid = Number.URShift((lo + hi), 1);
                int delta = CompareTo(term, mid, input, scratch);
                if (delta < 0)
                    hi = mid - 1;
                else if (delta > 0)
                    lo = mid + 1;
                else
                    return mid;
            }
            return hi;
        }

        internal Term GetTerm(int termIndex)
        {
            PagedBytes.PagedBytesDataInput input = (PagedBytes.PagedBytesDataInput)dataInput.Clone();
            input.Position = indexToDataOffset.Get(termIndex);

            // read the term
            int fieldId = input.ReadVInt();
            Term field = fields[fieldId];
            return new Term(field.Field, input.ReadString());
        }

        internal int Length
        {
            get
            {
                return indexSize;
            }
        }

        internal int CompareTo(Term term, int termIndex)
        {
            return CompareTo(term, termIndex, (PagedBytes.PagedBytesDataInput)dataInput.Clone(), new BytesRef());
        }

        private int CompareTo(Term term, int termIndex, PagedBytes.PagedBytesDataInput input, BytesRef reuse)
        {
            // if term field does not equal mid's field index, then compare fields
            // else if they are equal, compare term's string values...
            int c = CompareField(term, termIndex, input);
            if (c == 0)
            {
                reuse.length = input.ReadVInt();
                reuse.Grow(reuse.length);
                input.ReadBytes(reuse.bytes, 0, reuse.length);
                return comparator.Compare(term.Bytes, reuse);
            }
            return c;
        }

        private int CompareField(Term term, int termIndex, PagedBytes.PagedBytesDataInput input)
        {
            input.Position = indexToDataOffset.Get(termIndex);
            return term.Field.CompareTo(fields[input.ReadVInt()].Field);
        }
    }
}
