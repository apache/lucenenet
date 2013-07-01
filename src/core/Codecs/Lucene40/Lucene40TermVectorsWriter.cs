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
    public sealed class Lucene40TermVectorsWriter : TermVectorsWriter
    {
        private readonly Directory directory;
        private readonly String segment;
        private IndexOutput tvx = null, tvd = null, tvf = null;

        public Lucene40TermVectorsWriter(Directory directory, String segment, IOContext context)
        {
            this.directory = directory;
            this.segment = segment;
            bool success = false;
            try
            {
                // Open files for TermVector storage
                tvx = directory.CreateOutput(IndexFileNames.SegmentFileName(segment, "", Lucene40TermVectorsReader.VECTORS_INDEX_EXTENSION), context);
                CodecUtil.WriteHeader(tvx, Lucene40TermVectorsReader.CODEC_NAME_INDEX, Lucene40TermVectorsReader.VERSION_CURRENT);
                tvd = directory.CreateOutput(IndexFileNames.SegmentFileName(segment, "", Lucene40TermVectorsReader.VECTORS_DOCUMENTS_EXTENSION), context);
                CodecUtil.WriteHeader(tvd, Lucene40TermVectorsReader.CODEC_NAME_DOCS, Lucene40TermVectorsReader.VERSION_CURRENT);
                tvf = directory.CreateOutput(IndexFileNames.SegmentFileName(segment, "", Lucene40TermVectorsReader.VECTORS_FIELDS_EXTENSION), context);
                CodecUtil.WriteHeader(tvf, Lucene40TermVectorsReader.CODEC_NAME_FIELDS, Lucene40TermVectorsReader.VERSION_CURRENT);
                //assert HEADER_LENGTH_INDEX == tvx.getFilePointer();
                //assert HEADER_LENGTH_DOCS == tvd.getFilePointer();
                //assert HEADER_LENGTH_FIELDS == tvf.getFilePointer();
                success = true;
            }
            finally
            {
                if (!success)
                {
                    Abort();
                }
            }
        }

        public override void StartDocument(int numVectorFields)
        {
            lastFieldName = null;
            this.numVectorFields = numVectorFields;
            tvx.WriteLong(tvd.FilePointer);
            tvx.WriteLong(tvf.FilePointer);
            tvd.WriteVInt(numVectorFields);
            fieldCount = 0;
            fps = ArrayUtil.Grow(fps, numVectorFields);
        }

        private long[] fps = new long[10]; // pointers to the tvf before writing each field 
        private int fieldCount = 0;        // number of fields we have written so far for this document
        private int numVectorFields = 0;   // total number of fields we will write for this document
        private String lastFieldName;

        public override void StartField(FieldInfo info, int numTerms, bool positions, bool offsets, bool payloads)
        {
            //assert lastFieldName == null || info.name.compareTo(lastFieldName) > 0: "fieldName=" + info.name + " lastFieldName=" + lastFieldName;
            lastFieldName = info.name;
            this.positions = positions;
            this.offsets = offsets;
            this.payloads = payloads;
            lastTerm.length = 0;
            lastPayloadLength = -1; // force first payload to write its length
            fps[fieldCount++] = tvf.FilePointer;
            tvd.WriteVInt(info.number);
            tvf.WriteVInt(numTerms);
            byte bits = 0x0;
            if (positions)
                bits |= Lucene40TermVectorsReader.STORE_POSITIONS_WITH_TERMVECTOR;
            if (offsets)
                bits |= Lucene40TermVectorsReader.STORE_OFFSET_WITH_TERMVECTOR;
            if (payloads)
                bits |= Lucene40TermVectorsReader.STORE_PAYLOAD_WITH_TERMVECTOR;
            tvf.WriteByte(bits);
        }

        public override void FinishDocument()
        {
            //assert fieldCount == numVectorFields;
            for (int i = 1; i < fieldCount; i++)
            {
                tvd.WriteVLong(fps[i] - fps[i - 1]);
            }
        }

        private readonly BytesRef lastTerm = new BytesRef(10);

        // NOTE: we override addProx, so we don't need to buffer when indexing.
        // we also don't buffer during bulk merges.
        private int[] offsetStartBuffer = new int[10];
        private int[] offsetEndBuffer = new int[10];
        private BytesRef payloadData = new BytesRef(10);
        private int bufferedIndex = 0;
        private int bufferedFreq = 0;
        private bool positions = false;
        private bool offsets = false;
        private bool payloads = false;

        public override void StartTerm(BytesRef term, int freq)
        {
            int prefix = StringHelper.BytesDifference(lastTerm, term);
            int suffix = term.length - prefix;
            tvf.WriteVInt(prefix);
            tvf.WriteVInt(suffix);
            tvf.WriteBytes(term.bytes, term.offset + prefix, suffix);
            tvf.WriteVInt(freq);
            lastTerm.CopyBytes(term);
            lastPosition = lastOffset = 0;

            if (offsets && positions)
            {
                // we might need to buffer if its a non-bulk merge
                offsetStartBuffer = ArrayUtil.Grow(offsetStartBuffer, freq);
                offsetEndBuffer = ArrayUtil.Grow(offsetEndBuffer, freq);
            }
            bufferedIndex = 0;
            bufferedFreq = freq;
            payloadData.length = 0;
        }

        internal int lastPosition = 0;
        internal int lastOffset = 0;
        internal int lastPayloadLength = -1; // force first payload to write its length

        internal BytesRef scratch = new BytesRef(); // used only by this optimized flush below

        public override void AddProx(int numProx, DataInput positions, DataInput offsets)
        {
            if (payloads)
            {
                // TODO, maybe overkill and just call super.addProx() in this case?
                // we do avoid buffering the offsets in RAM though.
                for (int i = 0; i < numProx; i++)
                {
                    int code = positions.ReadVInt();
                    if ((code & 1) == 1)
                    {
                        int length = positions.ReadVInt();
                        scratch.Grow(length);
                        scratch.length = length;
                        positions.ReadBytes(scratch.bytes, scratch.offset, scratch.length);
                        WritePosition(Number.URShift(code, 1), scratch);
                    }
                    else
                    {
                        WritePosition(Number.URShift(code, 1), null);
                    }
                }
                tvf.WriteBytes(payloadData.bytes, payloadData.offset, payloadData.length);
            }
            else if (positions != null)
            {
                // pure positions, no payloads
                for (int i = 0; i < numProx; i++)
                {
                    tvf.WriteVInt(Number.URShift(positions.ReadVInt(), 1));
                }
            }

            if (offsets != null)
            {
                for (int i = 0; i < numProx; i++)
                {
                    tvf.WriteVInt(offsets.ReadVInt());
                    tvf.WriteVInt(offsets.ReadVInt());
                }
            }
        }

        public override void AddPosition(int position, int startOffset, int endOffset, BytesRef payload)
        {
            if (positions && (offsets || payloads))
            {
                // write position delta
                WritePosition(position - lastPosition, payload);
                lastPosition = position;

                // buffer offsets
                if (offsets)
                {
                    offsetStartBuffer[bufferedIndex] = startOffset;
                    offsetEndBuffer[bufferedIndex] = endOffset;
                }

                bufferedIndex++;
            }
            else if (positions)
            {
                // write position delta
                WritePosition(position - lastPosition, payload);
                lastPosition = position;
            }
            else if (offsets)
            {
                // write offset deltas
                tvf.WriteVInt(startOffset - lastOffset);
                tvf.WriteVInt(endOffset - startOffset);
                lastOffset = endOffset;
            }
        }

        public override void FinishTerm()
        {
            if (bufferedIndex > 0)
            {
                // dump buffer
                //assert positions && (offsets || payloads);
                //assert bufferedIndex == bufferedFreq;
                if (payloads)
                {
                    tvf.WriteBytes(payloadData.bytes, payloadData.offset, payloadData.length);
                }
                for (int i = 0; i < bufferedIndex; i++)
                {
                    if (offsets)
                    {
                        tvf.WriteVInt(offsetStartBuffer[i] - lastOffset);
                        tvf.WriteVInt(offsetEndBuffer[i] - offsetStartBuffer[i]);
                        lastOffset = offsetEndBuffer[i];
                    }
                }
            }
        }

        private void WritePosition(int delta, BytesRef payload)
        {
            if (payloads)
            {
                int payloadLength = payload == null ? 0 : payload.length;

                if (payloadLength != lastPayloadLength)
                {
                    lastPayloadLength = payloadLength;
                    tvf.WriteVInt((delta << 1) | 1);
                    tvf.WriteVInt(payloadLength);
                }
                else
                {
                    tvf.WriteVInt(delta << 1);
                }
                if (payloadLength > 0)
                {
                    if (payloadLength + payloadData.length < 0)
                    {
                        // we overflowed the payload buffer, just throw UOE
                        // having > Integer.MAX_VALUE bytes of payload for a single term in a single doc is nuts.
                        throw new NotSupportedException("A term cannot have more than Integer.MAX_VALUE bytes of payload data in a single document");
                    }
                    payloadData.Append(payload);
                }
            }
            else
            {
                tvf.WriteVInt(delta);
            }
        }

        public override void Abort()
        {
            try
            {
                Dispose();
            }
            catch { }
            IOUtils.DeleteFilesIgnoringExceptions(directory, IndexFileNames.SegmentFileName(segment, "", Lucene40TermVectorsReader.VECTORS_INDEX_EXTENSION),
                IndexFileNames.SegmentFileName(segment, "", Lucene40TermVectorsReader.VECTORS_DOCUMENTS_EXTENSION),
                IndexFileNames.SegmentFileName(segment, "", Lucene40TermVectorsReader.VECTORS_FIELDS_EXTENSION));
        }

        private void AddRawDocuments(Lucene40TermVectorsReader reader, int[] tvdLengths, int[] tvfLengths, int numDocs)
        {
            long tvdPosition = tvd.FilePointer;
            long tvfPosition = tvf.FilePointer;
            long tvdStart = tvdPosition;
            long tvfStart = tvfPosition;
            for (int i = 0; i < numDocs; i++)
            {
                tvx.WriteLong(tvdPosition);
                tvdPosition += tvdLengths[i];
                tvx.WriteLong(tvfPosition);
                tvfPosition += tvfLengths[i];
            }
            tvd.CopyBytes(reader.TvdStream, tvdPosition - tvdStart);
            tvf.CopyBytes(reader.TvfStream, tvfPosition - tvfStart);
            //assert tvd.getFilePointer() == tvdPosition;
            //assert tvf.getFilePointer() == tvfPosition;
        }

        public override int Merge(MergeState mergeState)
        {
            // Used for bulk-reading raw bytes for term vectors
            int[] rawDocLengths = new int[MAX_RAW_MERGE_DOCS];
            int[] rawDocLengths2 = new int[MAX_RAW_MERGE_DOCS];

            int idx = 0;
            int numDocs = 0;
            for (int i = 0; i < mergeState.readers.Count; i++)
            {
                AtomicReader reader = mergeState.readers[i];

                SegmentReader matchingSegmentReader = mergeState.matchingSegmentReaders[idx++];
                Lucene40TermVectorsReader matchingVectorsReader = null;
                if (matchingSegmentReader != null)
                {
                    TermVectorsReader vectorsReader = matchingSegmentReader.TermVectorsReader;

                    if (vectorsReader != null && vectorsReader is Lucene40TermVectorsReader)
                    {
                        matchingVectorsReader = (Lucene40TermVectorsReader)vectorsReader;
                    }
                }
                if (reader.LiveDocs != null)
                {
                    numDocs += CopyVectorsWithDeletions(mergeState, matchingVectorsReader, reader, rawDocLengths, rawDocLengths2);
                }
                else
                {
                    numDocs += CopyVectorsNoDeletions(mergeState, matchingVectorsReader, reader, rawDocLengths, rawDocLengths2);
                }
            }
            Finish(mergeState.fieldInfos, numDocs);
            return numDocs;
        }

        private const int MAX_RAW_MERGE_DOCS = 4192;

        private int CopyVectorsWithDeletions(MergeState mergeState,
                                        Lucene40TermVectorsReader matchingVectorsReader,
                                        AtomicReader reader,
                                        int[] rawDocLengths,
                                        int[] rawDocLengths2)
        {
            int maxDoc = reader.MaxDoc;
            IBits liveDocs = reader.LiveDocs;
            int totalNumDocs = 0;
            if (matchingVectorsReader != null)
            {
                // We can bulk-copy because the fieldInfos are "congruent"
                for (int docNum = 0; docNum < maxDoc; )
                {
                    if (!liveDocs[docNum])
                    {
                        // skip deleted docs
                        ++docNum;
                        continue;
                    }
                    // We can optimize this case (doing a bulk byte copy) since the field
                    // numbers are identical
                    int start = docNum, numDocs = 0;
                    do
                    {
                        docNum++;
                        numDocs++;
                        if (docNum >= maxDoc) break;
                        if (!liveDocs[docNum])
                        {
                            docNum++;
                            break;
                        }
                    } while (numDocs < MAX_RAW_MERGE_DOCS);

                    matchingVectorsReader.RawDocs(rawDocLengths, rawDocLengths2, start, numDocs);
                    AddRawDocuments(matchingVectorsReader, rawDocLengths, rawDocLengths2, numDocs);
                    totalNumDocs += numDocs;
                    mergeState.checkAbort.Work(300 * numDocs);
                }
            }
            else
            {
                for (int docNum = 0; docNum < maxDoc; docNum++)
                {
                    if (!liveDocs[docNum])
                    {
                        // skip deleted docs
                        continue;
                    }

                    // NOTE: it's very important to first assign to vectors then pass it to
                    // termVectorsWriter.addAllDocVectors; see LUCENE-1282
                    Fields vectors = reader.GetTermVectors(docNum);
                    AddAllDocVectors(vectors, mergeState);
                    totalNumDocs++;
                    mergeState.checkAbort.Work(300);
                }
            }
            return totalNumDocs;
        }

        private int CopyVectorsNoDeletions(MergeState mergeState,
                                     Lucene40TermVectorsReader matchingVectorsReader,
                                     AtomicReader reader,
                                     int[] rawDocLengths,
                                     int[] rawDocLengths2)
        {
            int maxDoc = reader.MaxDoc;
            if (matchingVectorsReader != null)
            {
                // We can bulk-copy because the fieldInfos are "congruent"
                int docCount = 0;
                while (docCount < maxDoc)
                {
                    int len = Math.Min(MAX_RAW_MERGE_DOCS, maxDoc - docCount);
                    matchingVectorsReader.RawDocs(rawDocLengths, rawDocLengths2, docCount, len);
                    AddRawDocuments(matchingVectorsReader, rawDocLengths, rawDocLengths2, len);
                    docCount += len;
                    mergeState.checkAbort.Work(300 * len);
                }
            }
            else
            {
                for (int docNum = 0; docNum < maxDoc; docNum++)
                {
                    // NOTE: it's very important to first assign to vectors then pass it to
                    // termVectorsWriter.addAllDocVectors; see LUCENE-1282
                    Fields vectors = reader.GetTermVectors(docNum);
                    AddAllDocVectors(vectors, mergeState);
                    mergeState.checkAbort.Work(300);
                }
            }
            return maxDoc;
        }

        public override void Finish(FieldInfos fis, int numDocs)
        {
            if (Lucene40TermVectorsReader.HEADER_LENGTH_INDEX + ((long)numDocs) * 16 != tvx.FilePointer)
                // This is most likely a bug in Sun JRE 1.6.0_04/_05;
                // we detect that the bug has struck, here, and
                // throw an exception to prevent the corruption from
                // entering the index.  See LUCENE-1282 for
                // details.
                throw new SystemException("tvx size mismatch: mergedDocs is " + numDocs + " but tvx size is " + tvx.FilePointer + " file=" + tvx.ToString() + "; now aborting this merge to prevent index corruption");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // make an effort to close all streams we can but remember and re-throw
                // the first exception encountered in this process
                IOUtils.Close(tvx, tvd, tvf);
                tvx = tvd = tvf = null;
            }
        }

        public override IComparer<BytesRef> Comparator
        {
            get { return BytesRef.UTF8SortedAsUnicodeComparer; }
        }
    }
}
