using System;
using System.Collections.Generic;
using System.Diagnostics;

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
    using Bits = Lucene.Net.Util.Bits;
    using BytesRef = Lucene.Net.Util.BytesRef;
    using DataInput = Lucene.Net.Store.DataInput;
    using Directory = Lucene.Net.Store.Directory;
    using FieldInfo = Lucene.Net.Index.FieldInfo;
    using FieldInfos = Lucene.Net.Index.FieldInfos;
    using Fields = Lucene.Net.Index.Fields;
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
    /// <p>
    /// It writes .tvd, .tvf, and .tvx files.
    /// </summary>
    /// <seealso cref= Lucene40TermVectorsFormat </seealso>
    public sealed class Lucene40TermVectorsWriter : TermVectorsWriter
    {
        private readonly Directory Directory;
        private readonly string Segment;
        private IndexOutput Tvx = null, Tvd = null, Tvf = null;

        /// <summary>
        /// Sole constructor. </summary>
        public Lucene40TermVectorsWriter(Directory directory, string segment, IOContext context)
        {
            this.Directory = directory;
            this.Segment = segment;
            bool success = false;
            try
            {
                // Open files for TermVector storage
                Tvx = directory.CreateOutput(IndexFileNames.SegmentFileName(segment, "", Lucene40TermVectorsReader.VECTORS_INDEX_EXTENSION), context);
                CodecUtil.WriteHeader(Tvx, Lucene40TermVectorsReader.CODEC_NAME_INDEX, Lucene40TermVectorsReader.VERSION_CURRENT);
                Tvd = directory.CreateOutput(IndexFileNames.SegmentFileName(segment, "", Lucene40TermVectorsReader.VECTORS_DOCUMENTS_EXTENSION), context);
                CodecUtil.WriteHeader(Tvd, Lucene40TermVectorsReader.CODEC_NAME_DOCS, Lucene40TermVectorsReader.VERSION_CURRENT);
                Tvf = directory.CreateOutput(IndexFileNames.SegmentFileName(segment, "", Lucene40TermVectorsReader.VECTORS_FIELDS_EXTENSION), context);
                CodecUtil.WriteHeader(Tvf, Lucene40TermVectorsReader.CODEC_NAME_FIELDS, Lucene40TermVectorsReader.VERSION_CURRENT);
                Debug.Assert(Lucene40TermVectorsReader.HEADER_LENGTH_INDEX == Tvx.FilePointer);
                Debug.Assert(Lucene40TermVectorsReader.HEADER_LENGTH_DOCS == Tvd.FilePointer);
                Debug.Assert(Lucene40TermVectorsReader.HEADER_LENGTH_FIELDS == Tvf.FilePointer);
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
            LastFieldName = null;
            this.NumVectorFields = numVectorFields;
            Tvx.WriteLong(Tvd.FilePointer);
            Tvx.WriteLong(Tvf.FilePointer);
            Tvd.WriteVInt(numVectorFields);
            FieldCount = 0;
            Fps = ArrayUtil.Grow(Fps, numVectorFields);
        }

        private long[] Fps = new long[10]; // pointers to the tvf before writing each field
        private int FieldCount = 0; // number of fields we have written so far for this document
        private int NumVectorFields = 0; // total number of fields we will write for this document
        private string LastFieldName;

        public override void StartField(FieldInfo info, int numTerms, bool positions, bool offsets, bool payloads)
        {
            Debug.Assert(LastFieldName == null || info.Name.CompareTo(LastFieldName) > 0, "fieldName=" + info.Name + " lastFieldName=" + LastFieldName);
            LastFieldName = info.Name;
            this.Positions = positions;
            this.Offsets = offsets;
            this.Payloads = payloads;
            LastTerm.Length = 0;
            LastPayloadLength = -1; // force first payload to write its length
            Fps[FieldCount++] = Tvf.FilePointer;
            Tvd.WriteVInt(info.Number);
            Tvf.WriteVInt(numTerms);
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
            Tvf.WriteByte((byte)bits);
        }

        public override void FinishDocument()
        {
            Debug.Assert(FieldCount == NumVectorFields);
            for (int i = 1; i < FieldCount; i++)
            {
                Tvd.WriteVLong(Fps[i] - Fps[i - 1]);
            }
        }

        private readonly BytesRef LastTerm = new BytesRef(10);

        // NOTE: we override addProx, so we don't need to buffer when indexing.
        // we also don't buffer during bulk merges.
        private int[] OffsetStartBuffer = new int[10];

        private int[] OffsetEndBuffer = new int[10];
        private BytesRef PayloadData = new BytesRef(10);
        private int BufferedIndex = 0;
        private int BufferedFreq = 0;
        private bool Positions = false;
        private bool Offsets = false;
        private bool Payloads = false;

        public override void StartTerm(BytesRef term, int freq)
        {
            int prefix = StringHelper.BytesDifference(LastTerm, term);
            int suffix = term.Length - prefix;
            Tvf.WriteVInt(prefix);
            Tvf.WriteVInt(suffix);
            Tvf.WriteBytes(term.Bytes, term.Offset + prefix, suffix);
            Tvf.WriteVInt(freq);
            LastTerm.CopyBytes(term);
            LastPosition = LastOffset = 0;

            if (Offsets && Positions)
            {
                // we might need to buffer if its a non-bulk merge
                OffsetStartBuffer = ArrayUtil.Grow(OffsetStartBuffer, freq);
                OffsetEndBuffer = ArrayUtil.Grow(OffsetEndBuffer, freq);
            }
            BufferedIndex = 0;
            BufferedFreq = freq;
            PayloadData.Length = 0;
        }

        internal int LastPosition = 0;
        internal int LastOffset = 0;
        internal int LastPayloadLength = -1; // force first payload to write its length

        internal BytesRef Scratch = new BytesRef(); // used only by this optimized flush below

        public override void AddProx(int numProx, DataInput positions, DataInput offsets)
        {
            if (Payloads)
            {
                // TODO, maybe overkill and just call super.addProx() in this case?
                // we do avoid buffering the offsets in RAM though.
                for (int i = 0; i < numProx; i++)
                {
                    int code = positions.ReadVInt();
                    if ((code & 1) == 1)
                    {
                        int length = positions.ReadVInt();
                        Scratch.Grow(length);
                        Scratch.Length = length;
                        positions.ReadBytes(Scratch.Bytes, Scratch.Offset, Scratch.Length);
                        WritePosition((int)((uint)code >> 1), Scratch);
                    }
                    else
                    {
                        WritePosition((int)((uint)code >> 1), null);
                    }
                }
                Tvf.WriteBytes(PayloadData.Bytes, PayloadData.Offset, PayloadData.Length);
            }
            else if (positions != null)
            {
                // pure positions, no payloads
                for (int i = 0; i < numProx; i++)
                {
                    Tvf.WriteVInt((int)((uint)positions.ReadVInt() >> 1));
                }
            }

            if (offsets != null)
            {
                for (int i = 0; i < numProx; i++)
                {
                    Tvf.WriteVInt(offsets.ReadVInt());
                    Tvf.WriteVInt(offsets.ReadVInt());
                }
            }
        }

        public override void AddPosition(int position, int startOffset, int endOffset, BytesRef payload)
        {
            if (Positions && (Offsets || Payloads))
            {
                // write position delta
                WritePosition(position - LastPosition, payload);
                LastPosition = position;

                // buffer offsets
                if (Offsets)
                {
                    OffsetStartBuffer[BufferedIndex] = startOffset;
                    OffsetEndBuffer[BufferedIndex] = endOffset;
                }

                BufferedIndex++;
            }
            else if (Positions)
            {
                // write position delta
                WritePosition(position - LastPosition, payload);
                LastPosition = position;
            }
            else if (Offsets)
            {
                // write offset deltas
                Tvf.WriteVInt(startOffset - LastOffset);
                Tvf.WriteVInt(endOffset - startOffset);
                LastOffset = endOffset;
            }
        }

        public override void FinishTerm()
        {
            if (BufferedIndex > 0)
            {
                // dump buffer
                Debug.Assert(Positions && (Offsets || Payloads));
                Debug.Assert(BufferedIndex == BufferedFreq);
                if (Payloads)
                {
                    Tvf.WriteBytes(PayloadData.Bytes, PayloadData.Offset, PayloadData.Length);
                }
                if (Offsets)
                {
                    for (int i = 0; i < BufferedIndex; i++)
                    {
                        Tvf.WriteVInt(OffsetStartBuffer[i] - LastOffset);
                        Tvf.WriteVInt(OffsetEndBuffer[i] - OffsetStartBuffer[i]);
                        LastOffset = OffsetEndBuffer[i];
                    }
                }
            }
        }

        private void WritePosition(int delta, BytesRef payload)
        {
            if (Payloads)
            {
                int payloadLength = payload == null ? 0 : payload.Length;

                if (payloadLength != LastPayloadLength)
                {
                    LastPayloadLength = payloadLength;
                    Tvf.WriteVInt((delta << 1) | 1);
                    Tvf.WriteVInt(payloadLength);
                }
                else
                {
                    Tvf.WriteVInt(delta << 1);
                }
                if (payloadLength > 0)
                {
                    if (payloadLength + PayloadData.Length < 0)
                    {
                        // we overflowed the payload buffer, just throw UOE
                        // having > Integer.MAX_VALUE bytes of payload for a single term in a single doc is nuts.
                        throw new System.NotSupportedException("A term cannot have more than Integer.MAX_VALUE bytes of payload data in a single document");
                    }
                    PayloadData.Append(payload);
                }
            }
            else
            {
                Tvf.WriteVInt(delta);
            }
        }

        public override void Abort()
        {
            try
            {
                Dispose();
            }
            catch (Exception ignored)
            {
            }
            IOUtils.DeleteFilesIgnoringExceptions(Directory, 
                IndexFileNames.SegmentFileName(Segment, "", Lucene40TermVectorsReader.VECTORS_INDEX_EXTENSION), 
                IndexFileNames.SegmentFileName(Segment, "", Lucene40TermVectorsReader.VECTORS_DOCUMENTS_EXTENSION), 
                IndexFileNames.SegmentFileName(Segment, "", Lucene40TermVectorsReader.VECTORS_FIELDS_EXTENSION));
        }

        /// <summary>
        /// Do a bulk copy of numDocs documents from reader to our
        /// streams.  this is used to expedite merging, if the
        /// field numbers are congruent.
        /// </summary>
        private void AddRawDocuments(Lucene40TermVectorsReader reader, int[] tvdLengths, int[] tvfLengths, int numDocs)
        {
            long tvdPosition = Tvd.FilePointer;
            long tvfPosition = Tvf.FilePointer;
            long tvdStart = tvdPosition;
            long tvfStart = tvfPosition;
            for (int i = 0; i < numDocs; i++)
            {
                Tvx.WriteLong(tvdPosition);
                tvdPosition += tvdLengths[i];
                Tvx.WriteLong(tvfPosition);
                tvfPosition += tvfLengths[i];
            }
            Tvd.CopyBytes(reader.TvdStream, tvdPosition - tvdStart);
            Tvf.CopyBytes(reader.TvfStream, tvfPosition - tvfStart);
            Debug.Assert(Tvd.FilePointer == tvdPosition);
            Debug.Assert(Tvf.FilePointer == tvfPosition);
        }

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
            Finish(mergeState.FieldInfos, numDocs);
            return numDocs;
        }

        /// <summary>
        /// Maximum number of contiguous documents to bulk-copy
        ///    when merging term vectors
        /// </summary>
        private const int MAX_RAW_MERGE_DOCS = 4192;

        private int CopyVectorsWithDeletions(MergeState mergeState, Lucene40TermVectorsReader matchingVectorsReader, AtomicReader reader, int[] rawDocLengths, int[] rawDocLengths2)
        {
            int maxDoc = reader.MaxDoc;
            Bits liveDocs = reader.LiveDocs;
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
                    mergeState.checkAbort.Work(300 * numDocs);
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
                    mergeState.checkAbort.Work(300);
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
            if (Lucene40TermVectorsReader.HEADER_LENGTH_INDEX + ((long)numDocs) * 16 != Tvx.FilePointer)
            // this is most likely a bug in Sun JRE 1.6.0_04/_05;
            // we detect that the bug has struck, here, and
            // throw an exception to prevent the corruption from
            // entering the index.  See LUCENE-1282 for
            // details.
            {
                throw new Exception("tvx size mismatch: mergedDocs is " + numDocs + " but tvx size is " + Tvx.FilePointer + " file=" + Tvx.ToString() + "; now aborting this merge to prevent index corruption");
            }
        }

        /// <summary>
        /// Close all streams. </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // make an effort to close all streams we can but remember and re-throw
                // the first exception encountered in this process
                IOUtils.Close(Tvx, Tvd, Tvf);
                Tvx = Tvd = Tvf = null;
            }
        }

        public override IComparer<BytesRef> Comparator
        {
            get
            {
                return BytesRef.UTF8SortedAsUnicodeComparer;
            }
        }
    }
}