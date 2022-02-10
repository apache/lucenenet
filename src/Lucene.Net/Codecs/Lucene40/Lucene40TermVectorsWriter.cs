using J2N.Numerics;
using J2N.Text;
using Lucene.Net.Diagnostics;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Lucene.Net.Codecs.Lucene40
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

    using ArrayUtil = Lucene.Net.Util.ArrayUtil;
    using AtomicReader = Lucene.Net.Index.AtomicReader;
    using BytesRef = Lucene.Net.Util.BytesRef;
    using DataInput = Lucene.Net.Store.DataInput;
    using Directory = Lucene.Net.Store.Directory;
    using FieldInfo = Lucene.Net.Index.FieldInfo;
    using FieldInfos = Lucene.Net.Index.FieldInfos;
    using Fields = Lucene.Net.Index.Fields;
    using IBits = Lucene.Net.Util.IBits;
    using IndexFileNames = Lucene.Net.Index.IndexFileNames;
    using IndexOutput = Lucene.Net.Store.IndexOutput;
    using IOContext = Lucene.Net.Store.IOContext;
    using IOUtils = Lucene.Net.Util.IOUtils;
    using MergeState = Lucene.Net.Index.MergeState;
    using SegmentReader = Lucene.Net.Index.SegmentReader;
    using StringHelper = Lucene.Net.Util.StringHelper;

    // TODO: make a new 4.0 TV format that encodes better
    //   - use startOffset (not endOffset) as base for delta on
    //     next startOffset because today for syns or ngrams or
    //     WDF or shingles etc. we are encoding negative vints
    //     (= slow, 5 bytes per)
    //   - if doc has no term vectors, write 0 into the tvx
    //     file; saves a seek to tvd only to read a 0 vint (and
    //     saves a byte in tvd)

    /// <summary>
    /// Lucene 4.0 Term Vectors writer.
    /// <para/>
    /// It writes .tvd, .tvf, and .tvx files.
    /// </summary>
    /// <seealso cref="Lucene40TermVectorsFormat"/>
    public sealed class Lucene40TermVectorsWriter : TermVectorsWriter
    {
        private readonly Directory directory;
        private readonly string segment;
#pragma warning disable CA2213 // Disposable fields should be disposed
        private IndexOutput tvx = null, tvd = null, tvf = null;
#pragma warning restore CA2213 // Disposable fields should be disposed

        /// <summary>
        /// Sole constructor. </summary>
        public Lucene40TermVectorsWriter(Directory directory, string segment, IOContext context)
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
                if (Debugging.AssertsEnabled)
                {
                    Debugging.Assert(Lucene40TermVectorsReader.HEADER_LENGTH_INDEX == tvx.Position); // LUCENENET specific: Renamed from getFilePointer() to match FileStream
                    Debugging.Assert(Lucene40TermVectorsReader.HEADER_LENGTH_DOCS == tvd.Position); // LUCENENET specific: Renamed from getFilePointer() to match FileStream
                    Debugging.Assert(Lucene40TermVectorsReader.HEADER_LENGTH_FIELDS == tvf.Position); // LUCENENET specific: Renamed from getFilePointer() to match FileStream
                }
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override void StartDocument(int numVectorFields)
        {
            lastFieldName = null;
            this.numVectorFields = numVectorFields;
            tvx.WriteInt64(tvd.Position); // LUCENENET specific: Renamed from getFilePointer() to match FileStream
            tvx.WriteInt64(tvf.Position); // LUCENENET specific: Renamed from getFilePointer() to match FileStream
            tvd.WriteVInt32(numVectorFields);
            fieldCount = 0;
            fps = ArrayUtil.Grow(fps, numVectorFields);
        }

        private long[] fps = new long[10]; // pointers to the tvf before writing each field
        private int fieldCount = 0; // number of fields we have written so far for this document
        private int numVectorFields = 0; // total number of fields we will write for this document
        private string lastFieldName;

        public override void StartField(FieldInfo info, int numTerms, bool positions, bool offsets, bool payloads)
        {
            if (Debugging.AssertsEnabled) Debugging.Assert(lastFieldName is null || info.Name.CompareToOrdinal(lastFieldName) > 0, "fieldName={0} lastFieldName={1}", info.Name, lastFieldName);
            lastFieldName = info.Name;
            this.positions = positions;
            this.offsets = offsets;
            this.payloads = payloads;
            lastTerm.Length = 0;
            lastPayloadLength = -1; // force first payload to write its length
            fps[fieldCount++] = tvf.Position; // LUCENENET specific: Renamed from getFilePointer() to match FileStream
            tvd.WriteVInt32(info.Number);
            tvf.WriteVInt32(numTerms);
            sbyte bits = 0x0;
            if (positions)
            {
                bits |= Lucene40TermVectorsReader.STORE_POSITIONS_WITH_TERMVECTOR;
            }
            if (offsets)
            {
                bits |= Lucene40TermVectorsReader.STORE_OFFSET_WITH_TERMVECTOR;
            }
            if (payloads)
            {
                bits |= Lucene40TermVectorsReader.STORE_PAYLOAD_WITH_TERMVECTOR;
            }
            tvf.WriteByte((byte)bits);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public override void FinishDocument()
        {
            if (Debugging.AssertsEnabled) Debugging.Assert(fieldCount == numVectorFields);
            for (int i = 1; i < fieldCount; i++)
            {
                tvd.WriteVInt64(fps[i] - fps[i - 1]);
            }
        }

        private readonly BytesRef lastTerm = new BytesRef(10);

        // NOTE: we override addProx, so we don't need to buffer when indexing.
        // we also don't buffer during bulk merges.
        private int[] offsetStartBuffer = new int[10];

        private int[] offsetEndBuffer = new int[10];
        private readonly BytesRef payloadData = new BytesRef(10); // LUCENENET: marked readonly
        private int bufferedIndex = 0;
        private int bufferedFreq = 0;
        private bool positions = false;
        private bool offsets = false;
        private bool payloads = false;

        public override void StartTerm(BytesRef term, int freq)
        {
            int prefix = StringHelper.BytesDifference(lastTerm, term);
            int suffix = term.Length - prefix;
            tvf.WriteVInt32(prefix);
            tvf.WriteVInt32(suffix);
            tvf.WriteBytes(term.Bytes, term.Offset + prefix, suffix);
            tvf.WriteVInt32(freq);
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
            payloadData.Length = 0;
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
                    int code = positions.ReadVInt32();
                    if ((code & 1) == 1)
                    {
                        int length = positions.ReadVInt32();
                        scratch.Grow(length);
                        scratch.Length = length;
                        positions.ReadBytes(scratch.Bytes, scratch.Offset, scratch.Length);
                        WritePosition(code.TripleShift(1), scratch);
                    }
                    else
                    {
                        WritePosition(code.TripleShift(1), null);
                    }
                }
                tvf.WriteBytes(payloadData.Bytes, payloadData.Offset, payloadData.Length);
            }
            else if (positions != null)
            {
                // pure positions, no payloads
                for (int i = 0; i < numProx; i++)
                {
                    tvf.WriteVInt32(positions.ReadVInt32().TripleShift(1));
                }
            }

            if (offsets != null)
            {
                for (int i = 0; i < numProx; i++)
                {
                    tvf.WriteVInt32(offsets.ReadVInt32());
                    tvf.WriteVInt32(offsets.ReadVInt32());
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
                tvf.WriteVInt32(startOffset - lastOffset);
                tvf.WriteVInt32(endOffset - startOffset);
                lastOffset = endOffset;
            }
        }

        public override void FinishTerm()
        {
            if (bufferedIndex > 0)
            {
                // dump buffer
                if (Debugging.AssertsEnabled)
                {
                    Debugging.Assert(positions && (offsets || payloads));
                    Debugging.Assert(bufferedIndex == bufferedFreq);
                }
                if (payloads)
                {
                    tvf.WriteBytes(payloadData.Bytes, payloadData.Offset, payloadData.Length);
                }
                if (offsets)
                {
                    for (int i = 0; i < bufferedIndex; i++)
                    {
                        tvf.WriteVInt32(offsetStartBuffer[i] - lastOffset);
                        tvf.WriteVInt32(offsetEndBuffer[i] - offsetStartBuffer[i]);
                        lastOffset = offsetEndBuffer[i];
                    }
                }
            }
        }

        private void WritePosition(int delta, BytesRef payload)
        {
            if (payloads)
            {
                int payloadLength = payload is null ? 0 : payload.Length;

                if (payloadLength != lastPayloadLength)
                {
                    lastPayloadLength = payloadLength;
                    tvf.WriteVInt32((delta << 1) | 1);
                    tvf.WriteVInt32(payloadLength);
                }
                else
                {
                    tvf.WriteVInt32(delta << 1);
                }
                if (payloadLength > 0)
                {
                    if (payloadLength + payloadData.Length < 0)
                    {
                        // we overflowed the payload buffer, just throw UOE
                        // having > System.Int32.MaxValue bytes of payload for a single term in a single doc is nuts.
                        throw UnsupportedOperationException.Create("A term cannot have more than System.Int32.MaxValue bytes of payload data in a single document");
                    }
                    payloadData.Append(payload);
                }
            }
            else
            {
                tvf.WriteVInt32(delta);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public override void Abort()
        {
            try
            {
                Dispose();
            }
            catch (Exception ignored) when (ignored.IsThrowable())
            {
            }
            IOUtils.DeleteFilesIgnoringExceptions(directory, 
                IndexFileNames.SegmentFileName(segment, "", Lucene40TermVectorsReader.VECTORS_INDEX_EXTENSION), 
                IndexFileNames.SegmentFileName(segment, "", Lucene40TermVectorsReader.VECTORS_DOCUMENTS_EXTENSION), 
                IndexFileNames.SegmentFileName(segment, "", Lucene40TermVectorsReader.VECTORS_FIELDS_EXTENSION));
        }

        /// <summary>
        /// Do a bulk copy of numDocs documents from reader to our
        /// streams.  This is used to expedite merging, if the
        /// field numbers are congruent.
        /// </summary>
        private void AddRawDocuments(Lucene40TermVectorsReader reader, int[] tvdLengths, int[] tvfLengths, int numDocs)
        {
            long tvdPosition = tvd.Position; // LUCENENET specific: Renamed from getFilePointer() to match FileStream
            long tvfPosition = tvf.Position; // LUCENENET specific: Renamed from getFilePointer() to match FileStream
            long tvdStart = tvdPosition;
            long tvfStart = tvfPosition;
            for (int i = 0; i < numDocs; i++)
            {
                tvx.WriteInt64(tvdPosition);
                tvdPosition += tvdLengths[i];
                tvx.WriteInt64(tvfPosition);
                tvfPosition += tvfLengths[i];
            }
            tvd.CopyBytes(reader.TvdStream, tvdPosition - tvdStart);
            tvf.CopyBytes(reader.TvfStream, tvfPosition - tvfStart);
            if (Debugging.AssertsEnabled)
            {
                Debugging.Assert(tvd.Position == tvdPosition); // LUCENENET specific: Renamed from getFilePointer() to match FileStream
                Debugging.Assert(tvf.Position == tvfPosition); // LUCENENET specific: Renamed from getFilePointer() to match FileStream
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public override int Merge(MergeState mergeState)
        {
            // Used for bulk-reading raw bytes for term vectors
            int[] rawDocLengths = new int[MAX_RAW_MERGE_DOCS];
            int[] rawDocLengths2 = new int[MAX_RAW_MERGE_DOCS];

            int idx = 0;
            int numDocs = 0;
            for (int i = 0; i < mergeState.Readers.Count; i++)
            {
                AtomicReader reader = mergeState.Readers[i];

                SegmentReader matchingSegmentReader = mergeState.MatchingSegmentReaders[idx++];
                Lucene40TermVectorsReader matchingVectorsReader = null;
                if (matchingSegmentReader != null)
                {
                    TermVectorsReader vectorsReader = matchingSegmentReader.TermVectorsReader;

                    if (vectorsReader != null && vectorsReader is Lucene40TermVectorsReader lucene40TermVectorsReader)
                    {
                        matchingVectorsReader = lucene40TermVectorsReader;
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
            Finish(mergeState.FieldInfos, numDocs);
            return numDocs;
        }

        /// <summary>
        /// Maximum number of contiguous documents to bulk-copy
        /// when merging term vectors.
        /// </summary>
        private const int MAX_RAW_MERGE_DOCS = 4192;

        private int CopyVectorsWithDeletions(MergeState mergeState, Lucene40TermVectorsReader matchingVectorsReader, AtomicReader reader, int[] rawDocLengths, int[] rawDocLengths2)
        {
            int maxDoc = reader.MaxDoc;
            IBits liveDocs = reader.LiveDocs;
            int totalNumDocs = 0;
            if (matchingVectorsReader != null)
            {
                // We can bulk-copy because the fieldInfos are "congruent"
                for (int docNum = 0; docNum < maxDoc; )
                {
                    if (!liveDocs.Get(docNum))
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
                        if (docNum >= maxDoc)
                        {
                            break;
                        }
                        if (!liveDocs.Get(docNum))
                        {
                            docNum++;
                            break;
                        }
                    } while (numDocs < MAX_RAW_MERGE_DOCS);

                    matchingVectorsReader.RawDocs(rawDocLengths, rawDocLengths2, start, numDocs);
                    AddRawDocuments(matchingVectorsReader, rawDocLengths, rawDocLengths2, numDocs);
                    totalNumDocs += numDocs;
                    mergeState.CheckAbort.Work(300 * numDocs);
                }
            }
            else
            {
                for (int docNum = 0; docNum < maxDoc; docNum++)
                {
                    if (!liveDocs.Get(docNum))
                    {
                        // skip deleted docs
                        continue;
                    }

                    // NOTE: it's very important to first assign to vectors then pass it to
                    // termVectorsWriter.addAllDocVectors; see LUCENE-1282
                    Fields vectors = reader.GetTermVectors(docNum);
                    AddAllDocVectors(vectors, mergeState);
                    totalNumDocs++;
                    mergeState.CheckAbort.Work(300);
                }
            }
            return totalNumDocs;
        }

        private int CopyVectorsNoDeletions(MergeState mergeState, Lucene40TermVectorsReader matchingVectorsReader, AtomicReader reader, int[] rawDocLengths, int[] rawDocLengths2)
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
                    mergeState.CheckAbort.Work(300 * len);
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
                    mergeState.CheckAbort.Work(300);
                }
            }
            return maxDoc;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override void Finish(FieldInfos fis, int numDocs)
        {
            if (Lucene40TermVectorsReader.HEADER_LENGTH_INDEX + ((long)numDocs) * 16 != tvx.Position) // LUCENENET specific: Renamed from getFilePointer() to match FileStream
            // this is most likely a bug in Sun JRE 1.6.0_04/_05;
            // we detect that the bug has struck, here, and
            // throw an exception to prevent the corruption from
            // entering the index.  See LUCENE-1282 for
            // details.
            {
                throw RuntimeException.Create("tvx size mismatch: mergedDocs is " + numDocs + " but tvx size is " + tvx.Position + " file=" + tvx.ToString() + "; now aborting this merge to prevent index corruption");
            }
        }

        /// <summary>
        /// Close all streams. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // make an effort to close all streams we can but remember and re-throw
                // the first exception encountered in this process
                IOUtils.Dispose(tvx, tvd, tvf);
                tvx = tvd = tvf = null;
            }
        }

        public override IComparer<BytesRef> Comparer => BytesRef.UTF8SortedAsUnicodeComparer;
    }
}