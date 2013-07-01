using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Support;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Codecs.Lucene40
{
    public class Lucene40TermVectorsReader : TermVectorsReader, IDisposable
    {
        internal const byte STORE_POSITIONS_WITH_TERMVECTOR = 0x1;

        internal const byte STORE_OFFSET_WITH_TERMVECTOR = 0x2;

        internal const byte STORE_PAYLOAD_WITH_TERMVECTOR = 0x4;

        /** Extension of vectors fields file */
        internal const String VECTORS_FIELDS_EXTENSION = "tvf";

        /** Extension of vectors documents file */
        internal const String VECTORS_DOCUMENTS_EXTENSION = "tvd";

        /** Extension of vectors index file */
        internal const String VECTORS_INDEX_EXTENSION = "tvx";

        internal const String CODEC_NAME_FIELDS = "Lucene40TermVectorsFields";
        internal const String CODEC_NAME_DOCS = "Lucene40TermVectorsDocs";
        internal const String CODEC_NAME_INDEX = "Lucene40TermVectorsIndex";

        internal const int VERSION_NO_PAYLOADS = 0;
        internal const int VERSION_PAYLOADS = 1;
        internal const int VERSION_START = VERSION_NO_PAYLOADS;
        internal const int VERSION_CURRENT = VERSION_PAYLOADS;

        internal static readonly long HEADER_LENGTH_FIELDS = CodecUtil.HeaderLength(CODEC_NAME_FIELDS);
        internal static readonly long HEADER_LENGTH_DOCS = CodecUtil.HeaderLength(CODEC_NAME_DOCS);
        internal static readonly long HEADER_LENGTH_INDEX = CodecUtil.HeaderLength(CODEC_NAME_INDEX);

        private FieldInfos fieldInfos;

        private IndexInput tvx;
        private IndexInput tvd;
        private IndexInput tvf;
        private int size;
        private int numTotalDocs;

        Lucene40TermVectorsReader(FieldInfos fieldInfos, IndexInput tvx, IndexInput tvd, IndexInput tvf, int size, int numTotalDocs)
        {
            this.fieldInfos = fieldInfos;
            this.tvx = tvx;
            this.tvd = tvd;
            this.tvf = tvf;
            this.size = size;
            this.numTotalDocs = numTotalDocs;
        }

        public Lucene40TermVectorsReader(Directory d, SegmentInfo si, FieldInfos fieldInfos, IOContext context)
        {
            String segment = si.name;
            int size = si.DocCount;

            bool success = false;

            try
            {
                String idxName = IndexFileNames.SegmentFileName(segment, "", VECTORS_INDEX_EXTENSION);
                tvx = d.OpenInput(idxName, context);
                int tvxVersion = CodecUtil.CheckHeader(tvx, CODEC_NAME_INDEX, VERSION_START, VERSION_CURRENT);

                String fn = IndexFileNames.SegmentFileName(segment, "", VECTORS_DOCUMENTS_EXTENSION);
                tvd = d.OpenInput(fn, context);
                int tvdVersion = CodecUtil.CheckHeader(tvd, CODEC_NAME_DOCS, VERSION_START, VERSION_CURRENT);
                fn = IndexFileNames.SegmentFileName(segment, "", VECTORS_FIELDS_EXTENSION);
                tvf = d.OpenInput(fn, context);
                int tvfVersion = CodecUtil.CheckHeader(tvf, CODEC_NAME_FIELDS, VERSION_START, VERSION_CURRENT);
                //assert HEADER_LENGTH_INDEX == tvx.getFilePointer();
                //assert HEADER_LENGTH_DOCS == tvd.getFilePointer();
                //assert HEADER_LENGTH_FIELDS == tvf.getFilePointer();
                //assert tvxVersion == tvdVersion;
                //assert tvxVersion == tvfVersion;

                numTotalDocs = (int)(tvx.Length - HEADER_LENGTH_INDEX >> 4);

                this.size = numTotalDocs;
                //assert size == 0 || numTotalDocs == size;

                this.fieldInfos = fieldInfos;
                success = true;
            }
            finally
            {
                // With lock-less commits, it's entirely possible (and
                // fine) to hit a FileNotFound exception above. In
                // this case, we want to explicitly close any subset
                // of things that were opened so that we don't have to
                // wait for a GC to do so.
                if (!success)
                {
                    try
                    {
                        Dispose();
                    }
                    catch { } // ensure we throw our original exception
                }
            }
        }

        // Used for bulk copy when merging
        internal virtual IndexInput TvdStream
        {
            get
            {
                return tvd;
            }
        }

        // Used for bulk copy when merging
        internal virtual IndexInput TvfStream
        {
            get
            {
                return tvf;
            }
        }

        // Not private to avoid synthetic access$NNN methods
        internal virtual void SeekTvx(int docNum)
        {
            tvx.Seek(docNum * 16L + HEADER_LENGTH_INDEX);
        }

        internal void RawDocs(int[] tvdLengths, int[] tvfLengths, int startDocID, int numDocs)
        {

            if (tvx == null)
            {
                Arrays.Fill(tvdLengths, 0);
                Arrays.Fill(tvfLengths, 0);
                return;
            }

            SeekTvx(startDocID);

            long tvdPosition = tvx.ReadLong();
            tvd.Seek(tvdPosition);

            long tvfPosition = tvx.ReadLong();
            tvf.Seek(tvfPosition);

            long lastTvdPosition = tvdPosition;
            long lastTvfPosition = tvfPosition;

            int count = 0;
            while (count < numDocs)
            {
                int docID = startDocID + count + 1;
                //assert docID <= numTotalDocs;
                if (docID < numTotalDocs)
                {
                    tvdPosition = tvx.ReadLong();
                    tvfPosition = tvx.ReadLong();
                }
                else
                {
                    tvdPosition = tvd.Length;
                    tvfPosition = tvf.Length;
                    //assert count == numDocs-1;
                }
                tvdLengths[count] = (int)(tvdPosition - lastTvdPosition);
                tvfLengths[count] = (int)(tvfPosition - lastTvfPosition);
                count++;
                lastTvdPosition = tvdPosition;
                lastTvfPosition = tvfPosition;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                IOUtils.Close(tvx, tvd, tvf);
            }
        }

        internal int Size
        {
            get
            {
                return size;
            }
        }

        private class TVFields : Fields
        {
            private readonly Lucene40TermVectorsReader parent;
            private readonly int[] fieldNumbers;
            private readonly long[] fieldFPs;
            private readonly IDictionary<int, int> fieldNumberToIndex = new HashMap<int, int>();

            public TVFields(Lucene40TermVectorsReader parent, int docID)
            {
                this.parent = parent;
                parent.SeekTvx(docID);
                parent.tvd.Seek(parent.tvx.ReadLong());

                int fieldCount = parent.tvd.ReadVInt();
                //assert fieldCount >= 0;
                if (fieldCount != 0)
                {
                    fieldNumbers = new int[fieldCount];
                    fieldFPs = new long[fieldCount];
                    for (int fieldUpto = 0; fieldUpto < fieldCount; fieldUpto++)
                    {
                        int fieldNumber = parent.tvd.ReadVInt();
                        fieldNumbers[fieldUpto] = fieldNumber;
                        fieldNumberToIndex[fieldNumber] = fieldUpto;
                    }

                    long position = parent.tvx.ReadLong();
                    fieldFPs[0] = position;
                    for (int fieldUpto = 1; fieldUpto < fieldCount; fieldUpto++)
                    {
                        position += parent.tvd.ReadVLong();
                        fieldFPs[fieldUpto] = position;
                    }
                }
                else
                {
                    // TODO: we can improve writer here, eg write 0 into
                    // tvx file, so we know on first read from tvx that
                    // this doc has no TVs
                    fieldNumbers = null;
                    fieldFPs = null;
                }
            }

            public override IEnumerator<string> GetEnumerator()
            {
                return GetFieldInfoEnumerable().GetEnumerator();
            }

            private IEnumerable<string> GetFieldInfoEnumerable()
            {
                int fieldUpto = 0;

                while (fieldNumbers != null && fieldUpto < fieldNumbers.Length)
                {
                    yield return parent.fieldInfos.FieldInfo(fieldNumbers[fieldUpto++]).name;
                }
            }

            public override Terms Terms(string field)
            {
                FieldInfo fieldInfo = parent.fieldInfos.FieldInfo(field);
                if (fieldInfo == null)
                {
                    // No such field
                    return null;
                }

                int fieldIndex = fieldNumberToIndex[fieldInfo.number];
                if (fieldIndex == null)
                {
                    // Term vectors were not indexed for this field
                    return null;
                }

                return new TVTerms(parent, fieldFPs[fieldIndex]);
            }

            public override int Size
            {
                get
                {
                    if (fieldNumbers == null)
                    {
                        return 0;
                    }
                    else
                    {
                        return fieldNumbers.Length;
                    }
                }
            }
        }

        private class TVTerms : Terms
        {
            private readonly Lucene40TermVectorsReader parent;
            private readonly int numTerms;
            private readonly long tvfFPStart;
            private readonly bool storePositions;
            private readonly bool storeOffsets;
            private readonly bool storePayloads;

            public TVTerms(Lucene40TermVectorsReader parent, long tvfFP)
            {
                this.parent = parent;
                parent.tvf.Seek(tvfFP);
                numTerms = parent.tvf.ReadVInt();
                byte bits = parent.tvf.ReadByte();
                storePositions = (bits & STORE_POSITIONS_WITH_TERMVECTOR) != 0;
                storeOffsets = (bits & STORE_OFFSET_WITH_TERMVECTOR) != 0;
                storePayloads = (bits & STORE_PAYLOAD_WITH_TERMVECTOR) != 0;
                tvfFPStart = parent.tvf.FilePointer;
            }

            public override TermsEnum Iterator(TermsEnum reuse)
            {
                TVTermsEnum termsEnum;
                if (reuse is TVTermsEnum)
                {
                    termsEnum = (TVTermsEnum)reuse;
                    if (!termsEnum.CanReuse(parent.tvf))
                    {
                        termsEnum = new TVTermsEnum(parent);
                    }
                }
                else
                {
                    termsEnum = new TVTermsEnum(parent);
                }
                termsEnum.Reset(numTerms, tvfFPStart, storePositions, storeOffsets, storePayloads);
                return termsEnum;
            }

            public override long Size
            {
                get { return numTerms; }
            }

            public override long SumTotalTermFreq
            {
                get { return -1; }
            }

            public override long SumDocFreq
            {
                get
                {
                    // Every term occurs in just one doc:
                    return numTerms;
                }
            }

            public override int DocCount
            {
                get { return 1; }
            }

            public override IComparer<BytesRef> Comparator
            {
                get
                {
                    // TODO: really indexer hardwires
                    // this...?  I guess codec could buffer and re-sort...
                    return BytesRef.UTF8SortedAsUnicodeComparer;
                }
            }

            public override bool HasOffsets
            {
                get { return storeOffsets; }
            }

            public override bool HasPositions
            {
                get { return storePositions; }
            }

            public override bool HasPayloads
            {
                get { return storePayloads; }
            }
        }

        private class TVTermsEnum : TermsEnum
        {
            private readonly Lucene40TermVectorsReader parent;
            private readonly IndexInput origTVF;
            private readonly IndexInput tvf;
            private int numTerms;
            private int nextTerm;
            private int freq;
            private BytesRef lastTerm = new BytesRef();
            private BytesRef term = new BytesRef();
            private bool storePositions;
            private bool storeOffsets;
            private bool storePayloads;
            private long tvfFP;

            private int[] positions;
            private int[] startOffsets;
            private int[] endOffsets;

            // one shared byte[] for any term's payloads
            private int[] payloadOffsets;
            private int lastPayloadLength;
            private byte[] payloadData;

            // NOTE: tvf is pre-positioned by caller
            public TVTermsEnum(Lucene40TermVectorsReader parent)
            {
                this.parent = parent;
                this.origTVF = parent.tvf;
                tvf = (IndexInput)origTVF.Clone();
            }

            public bool CanReuse(IndexInput tvf)
            {
                return tvf == origTVF;
            }

            public void Reset(int numTerms, long tvfFPStart, bool storePositions, bool storeOffsets, bool storePayloads)
            {
                this.numTerms = numTerms;
                this.storePositions = storePositions;
                this.storeOffsets = storeOffsets;
                this.storePayloads = storePayloads;
                nextTerm = 0;
                tvf.Seek(tvfFPStart);
                tvfFP = tvfFPStart;
                positions = null;
                startOffsets = null;
                endOffsets = null;
                payloadOffsets = null;
                payloadData = null;
                lastPayloadLength = -1;
            }

            // NOTE: slow!  (linear scan)
            public override SeekStatus SeekCeil(BytesRef text, bool useCache)
            {
                if (nextTerm != 0)
                {
                    int cmp = text.CompareTo(term);
                    if (cmp < 0)
                    {
                        nextTerm = 0;
                        tvf.Seek(tvfFP);
                    }
                    else if (cmp == 0)
                    {
                        return SeekStatus.FOUND;
                    }
                }

                while (Next() != null)
                {
                    int cmp = text.CompareTo(term);
                    if (cmp < 0)
                    {
                        return SeekStatus.NOT_FOUND;
                    }
                    else if (cmp == 0)
                    {
                        return SeekStatus.FOUND;
                    }
                }

                return SeekStatus.END;
            }

            public override void SeekExact(long ord)
            {
                throw new NotSupportedException();
            }

            public override BytesRef Next()
            {
                if (nextTerm >= numTerms)
                {
                    return null;
                }
                term.CopyBytes(lastTerm);
                int start = tvf.ReadVInt();
                int deltaLen = tvf.ReadVInt();
                term.length = start + deltaLen;
                term.Grow(term.length);
                tvf.ReadBytes(term.bytes, start, deltaLen);
                freq = tvf.ReadVInt();

                if (storePayloads)
                {
                    positions = new int[freq];
                    payloadOffsets = new int[freq];
                    int totalPayloadLength = 0;
                    int pos = 0;
                    for (int posUpto = 0; posUpto < freq; posUpto++)
                    {
                        int code = tvf.ReadVInt();
                        pos += Number.URShift(code, 1);
                        positions[posUpto] = pos;
                        if ((code & 1) != 0)
                        {
                            // length change
                            lastPayloadLength = tvf.ReadVInt();
                        }
                        payloadOffsets[posUpto] = totalPayloadLength;
                        totalPayloadLength += lastPayloadLength;
                        //assert totalPayloadLength >= 0;
                    }
                    payloadData = new byte[totalPayloadLength];
                    tvf.ReadBytes(payloadData, 0, payloadData.Length);
                }
                else if (storePositions /* no payloads */)
                {
                    // TODO: we could maybe reuse last array, if we can
                    // somehow be careful about consumer never using two
                    // D&PEnums at once...
                    positions = new int[freq];
                    int pos = 0;
                    for (int posUpto = 0; posUpto < freq; posUpto++)
                    {
                        pos += tvf.ReadVInt();
                        positions[posUpto] = pos;
                    }
                }

                if (storeOffsets)
                {
                    startOffsets = new int[freq];
                    endOffsets = new int[freq];
                    int offset = 0;
                    for (int posUpto = 0; posUpto < freq; posUpto++)
                    {
                        startOffsets[posUpto] = offset + tvf.ReadVInt();
                        offset = endOffsets[posUpto] = startOffsets[posUpto] + tvf.ReadVInt();
                    }
                }

                lastTerm.CopyBytes(term);
                nextTerm++;
                return term;
            }

            public override BytesRef Term
            {
                get { return term; }
            }

            public override long Ord
            {
                get { throw new NotSupportedException(); }
            }

            public override int DocFreq
            {
                get { return 1; }
            }

            public override long TotalTermFreq
            {
                get { return freq; }
            }

            public override DocsEnum Docs(IBits liveDocs, DocsEnum reuse, int flags)
            {
                TVDocsEnum docsEnum;
                if (reuse != null && reuse is TVDocsEnum)
                {
                    docsEnum = (TVDocsEnum)reuse;
                }
                else
                {
                    docsEnum = new TVDocsEnum();
                }
                docsEnum.Reset(liveDocs, freq);
                return docsEnum;
            }

            public override DocsAndPositionsEnum DocsAndPositions(IBits liveDocs, DocsAndPositionsEnum reuse, int flags)
            {
                if (!storePositions && !storeOffsets)
                {
                    return null;
                }

                TVDocsAndPositionsEnum docsAndPositionsEnum;
                if (reuse != null && reuse is TVDocsAndPositionsEnum)
                {
                    docsAndPositionsEnum = (TVDocsAndPositionsEnum)reuse;
                }
                else
                {
                    docsAndPositionsEnum = new TVDocsAndPositionsEnum();
                }
                docsAndPositionsEnum.Reset(liveDocs, positions, startOffsets, endOffsets, payloadOffsets, (sbyte[])(Array)payloadData);
                return docsAndPositionsEnum;
            }

            public override IComparer<BytesRef> Comparator
            {
                get { return BytesRef.UTF8SortedAsUnicodeComparer; }
            }
        }

        private class TVDocsEnum : DocsEnum
        {
            private bool didNext;
            private int doc = -1;
            private int freq;
            private IBits liveDocs;

            public override int Freq
            {
                get { return freq; }
            }

            public override int DocID
            {
                get { return doc; }
            }

            public override int NextDoc()
            {
                if (!didNext && (liveDocs == null || liveDocs[0]))
                {
                    didNext = true;
                    return (doc = 0);
                }
                else
                {
                    return (doc = NO_MORE_DOCS);
                }
            }

            public override int Advance(int target)
            {
                return SlowAdvance(target);
            }

            public void Reset(IBits liveDocs, int freq)
            {
                this.liveDocs = liveDocs;
                this.freq = freq;
                this.doc = -1;
                didNext = false;
            }

            public override long Cost
            {
                get { return 1; }
            }
        }

        private class TVDocsAndPositionsEnum : DocsAndPositionsEnum
        {
            private bool didNext;
            private int doc = -1;
            private int nextPos;
            private IBits liveDocs;
            private int[] positions;
            private int[] startOffsets;
            private int[] endOffsets;
            private int[] payloadOffsets;
            private BytesRef payload = new BytesRef();
            private sbyte[] payloadBytes;

            public override int Freq
            {
                get
                {
                    if (positions != null)
                    {
                        return positions.Length;
                    }
                    else
                    {
                        //assert startOffsets != null;
                        return startOffsets.Length;
                    }
                }
            }

            public override int DocID
            {
                get { return doc; }
            }

            public override int NextDoc()
            {
                if (!didNext && (liveDocs == null || liveDocs[0]))
                {
                    didNext = true;
                    return (doc = 0);
                }
                else
                {
                    return (doc = NO_MORE_DOCS);
                }
            }

            public override int Advance(int target)
            {
                return SlowAdvance(target);
            }

            public void Reset(IBits liveDocs, int[] positions, int[] startOffsets, int[] endOffsets, int[] payloadLengths, sbyte[] payloadBytes)
            {
                this.liveDocs = liveDocs;
                this.positions = positions;
                this.startOffsets = startOffsets;
                this.endOffsets = endOffsets;
                this.payloadOffsets = payloadLengths;
                this.payloadBytes = payloadBytes;
                this.doc = -1;
                didNext = false;
                nextPos = 0;
            }

            public override BytesRef Payload
            {
                get
                {
                    if (payloadOffsets == null)
                    {
                        return null;
                    }
                    else
                    {
                        int off = payloadOffsets[nextPos - 1];
                        int end = nextPos == payloadOffsets.Length ? payloadBytes.Length : payloadOffsets[nextPos];
                        if (end - off == 0)
                        {
                            return null;
                        }
                        payload.bytes = payloadBytes;
                        payload.offset = off;
                        payload.length = end - off;
                        return payload;
                    }
                }
            }

            public override int NextPosition()
            {
                //assert(positions != null && nextPos < positions.length) ||
                //  startOffsets != null && nextPos < startOffsets.length;

                if (positions != null)
                {
                    return positions[nextPos++];
                }
                else
                {
                    nextPos++;
                    return -1;
                }
            }

            public override int StartOffset
            {
                get
                {
                    if (startOffsets == null)
                    {
                        return -1;
                    }
                    else
                    {
                        return startOffsets[nextPos - 1];
                    }
                }
            }

            public override int EndOffset
            {
                get
                {
                    if (endOffsets == null)
                    {
                        return -1;
                    }
                    else
                    {
                        return endOffsets[nextPos - 1];
                    }
                }
            }

            public override long Cost
            {
                get { return 1; }
            }
        }

        public override Fields Get(int docID)
        {
            if (tvx != null)
            {
                Fields fields = new TVFields(this, docID);
                if (fields.Size == 0)
                {
                    // TODO: we can improve writer here, eg write 0 into
                    // tvx file, so we know on first read from tvx that
                    // this doc has no TVs
                    return null;
                }
                else
                {
                    return fields;
                }
            }
            else
            {
                return null;
            }
        }

        public override object Clone()
        {
            IndexInput cloneTvx = null;
            IndexInput cloneTvd = null;
            IndexInput cloneTvf = null;

            // These are null when a TermVectorsReader was created
            // on a segment that did not have term vectors saved
            if (tvx != null && tvd != null && tvf != null)
            {
                cloneTvx = (IndexInput)tvx.Clone();
                cloneTvd = (IndexInput)tvd.Clone();
                cloneTvf = (IndexInput)tvf.Clone();
            }

            return new Lucene40TermVectorsReader(fieldInfos, cloneTvx, cloneTvd, cloneTvf, size, numTotalDocs);
        }
    }
}
