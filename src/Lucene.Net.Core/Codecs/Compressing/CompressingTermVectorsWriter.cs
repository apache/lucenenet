using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using ArrayUtil = Lucene.Net.Util.ArrayUtil;

namespace Lucene.Net.Codecs.Compressing
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

    using AtomicReader = Lucene.Net.Index.AtomicReader;
    using IBits = Lucene.Net.Util.IBits;
    using BlockPackedWriter = Lucene.Net.Util.Packed.BlockPackedWriter;
    using BufferedChecksumIndexInput = Lucene.Net.Store.BufferedChecksumIndexInput;
    using BytesRef = Lucene.Net.Util.BytesRef;
    using ChecksumIndexInput = Lucene.Net.Store.ChecksumIndexInput;
    using DataInput = Lucene.Net.Store.DataInput;
    using Directory = Lucene.Net.Store.Directory;
    using FieldInfo = Lucene.Net.Index.FieldInfo;
    using FieldInfos = Lucene.Net.Index.FieldInfos;
    using Fields = Lucene.Net.Index.Fields;
    using GrowableByteArrayDataOutput = Lucene.Net.Util.GrowableByteArrayDataOutput;
    using IndexFileNames = Lucene.Net.Index.IndexFileNames;
    using IndexInput = Lucene.Net.Store.IndexInput;
    using IndexOutput = Lucene.Net.Store.IndexOutput;
    using IOContext = Lucene.Net.Store.IOContext;
    using IOUtils = Lucene.Net.Util.IOUtils;
    using MergeState = Lucene.Net.Index.MergeState;
    using PackedInts = Lucene.Net.Util.Packed.PackedInts;
    using SegmentInfo = Lucene.Net.Index.SegmentInfo;
    using SegmentReader = Lucene.Net.Index.SegmentReader;
    using StringHelper = Lucene.Net.Util.StringHelper;

    /// <summary>
    /// <seealso cref="TermVectorsWriter"/> for <seealso cref="CompressingTermVectorsFormat"/>.
    /// @lucene.experimental
    /// </summary>
    public sealed class CompressingTermVectorsWriter : TermVectorsWriter
    {
        // hard limit on the maximum number of documents per chunk
        internal const int MAX_DOCUMENTS_PER_CHUNK = 128;

        internal const string VECTORS_EXTENSION = "tvd";
        internal const string VECTORS_INDEX_EXTENSION = "tvx";

        internal const string CODEC_SFX_IDX = "Index";
        internal const string CODEC_SFX_DAT = "Data";

        internal const int VERSION_START = 0;
        internal const int VERSION_CHECKSUM = 1;
        internal const int VERSION_CURRENT = VERSION_CHECKSUM;

        internal const int BLOCK_SIZE = 64;

        internal const int POSITIONS = 0x01;
        internal const int OFFSETS = 0x02;
        internal const int PAYLOADS = 0x04;
        internal static readonly int FLAGS_BITS = PackedInts.BitsRequired(POSITIONS | OFFSETS | PAYLOADS);

        private readonly Directory Directory;
        private readonly string Segment;
        private readonly string SegmentSuffix;
        private CompressingStoredFieldsIndexWriter IndexWriter;
        private IndexOutput VectorsStream;

        private readonly CompressionMode CompressionMode;
        private readonly Compressor Compressor;
        private readonly int ChunkSize;

        /// <summary>
        /// a pending doc </summary>
        private class DocData
        {
            private readonly CompressingTermVectorsWriter OuterInstance;

            internal readonly int NumFields;
            internal readonly LinkedList<FieldData> Fields;
            internal readonly int PosStart, OffStart, PayStart;

            internal DocData(CompressingTermVectorsWriter OuterInstance, int numFields, int posStart, int offStart, int payStart)
            {
                this.OuterInstance = OuterInstance;
                this.NumFields = numFields;
                this.Fields = new LinkedList<FieldData>();
                this.PosStart = posStart;
                this.OffStart = offStart;
                this.PayStart = payStart;
            }

            internal virtual FieldData AddField(int fieldNum, int numTerms, bool positions, bool offsets, bool payloads)
            {
                FieldData field;
                if (Fields.Count == 0)
                {
                    field = new FieldData(OuterInstance, fieldNum, numTerms, positions, offsets, payloads, PosStart, OffStart, PayStart);
                }
                else
                {
                    FieldData last = Fields.Last.Value;
                    int posStart = last.PosStart + (last.HasPositions ? last.TotalPositions : 0);
                    int offStart = last.OffStart + (last.HasOffsets ? last.TotalPositions : 0);
                    int payStart = last.PayStart + (last.HasPayloads ? last.TotalPositions : 0);
                    field = new FieldData(OuterInstance, fieldNum, numTerms, positions, offsets, payloads, posStart, offStart, payStart);
                }
                Fields.AddLast(field);
                return field;
            }
        }

        private DocData AddDocData(int numVectorFields)
        {
            FieldData last = null;
            //for (IEnumerator<DocData> it = PendingDocs.Reverse(); it.MoveNext();)
            foreach (DocData doc in PendingDocs.Reverse())
            {
                if (!(doc.Fields.Count == 0))
                {
                    last = doc.Fields.Last.Value;
                    break;
                }
            }
            DocData newDoc;
            if (last == null)
            {
                newDoc = new DocData(this, numVectorFields, 0, 0, 0);
            }
            else
            {
                int posStart = last.PosStart + (last.HasPositions ? last.TotalPositions : 0);
                int offStart = last.OffStart + (last.HasOffsets ? last.TotalPositions : 0);
                int payStart = last.PayStart + (last.HasPayloads ? last.TotalPositions : 0);
                newDoc = new DocData(this, numVectorFields, posStart, offStart, payStart);
            }
            PendingDocs.AddLast(newDoc);
            return newDoc;
        }

        /// <summary>
        /// a pending field </summary>
        private class FieldData
        {
            private readonly CompressingTermVectorsWriter OuterInstance;

            internal readonly bool HasPositions, HasOffsets, HasPayloads;
            internal readonly int FieldNum, Flags, NumTerms;
            internal readonly int[] Freqs, PrefixLengths, SuffixLengths;
            internal readonly int PosStart, OffStart, PayStart;
            internal int TotalPositions;
            internal int Ord;

            internal FieldData(CompressingTermVectorsWriter OuterInstance, int fieldNum, int numTerms, bool positions, bool offsets, bool payloads, int posStart, int offStart, int payStart)
            {
                this.OuterInstance = OuterInstance;
                this.FieldNum = fieldNum;
                this.NumTerms = numTerms;
                this.HasPositions = positions;
                this.HasOffsets = offsets;
                this.HasPayloads = payloads;
                this.Flags = (positions ? POSITIONS : 0) | (offsets ? OFFSETS : 0) | (payloads ? PAYLOADS : 0);
                this.Freqs = new int[numTerms];
                this.PrefixLengths = new int[numTerms];
                this.SuffixLengths = new int[numTerms];
                this.PosStart = posStart;
                this.OffStart = offStart;
                this.PayStart = payStart;
                TotalPositions = 0;
                Ord = 0;
            }

            internal virtual void AddTerm(int freq, int prefixLength, int suffixLength)
            {
                Freqs[Ord] = freq;
                PrefixLengths[Ord] = prefixLength;
                SuffixLengths[Ord] = suffixLength;
                ++Ord;
            }

            internal virtual void AddPosition(int position, int startOffset, int length, int payloadLength)
            {
                if (HasPositions)
                {
                    if (PosStart + TotalPositions == OuterInstance.PositionsBuf.Length)
                    {
                        OuterInstance.PositionsBuf = ArrayUtil.Grow(OuterInstance.PositionsBuf);
                    }
                    OuterInstance.PositionsBuf[PosStart + TotalPositions] = position;
                }
                if (HasOffsets)
                {
                    if (OffStart + TotalPositions == OuterInstance.StartOffsetsBuf.Length)
                    {
                        int newLength = ArrayUtil.Oversize(OffStart + TotalPositions, 4);
                        OuterInstance.StartOffsetsBuf = Arrays.CopyOf(OuterInstance.StartOffsetsBuf, newLength);
                        OuterInstance.LengthsBuf = Arrays.CopyOf(OuterInstance.LengthsBuf, newLength);
                    }
                    OuterInstance.StartOffsetsBuf[OffStart + TotalPositions] = startOffset;
                    OuterInstance.LengthsBuf[OffStart + TotalPositions] = length;
                }
                if (HasPayloads)
                {
                    if (PayStart + TotalPositions == OuterInstance.PayloadLengthsBuf.Length)
                    {
                        OuterInstance.PayloadLengthsBuf = ArrayUtil.Grow(OuterInstance.PayloadLengthsBuf);
                    }
                    OuterInstance.PayloadLengthsBuf[PayStart + TotalPositions] = payloadLength;
                }
                ++TotalPositions;
            }
        }

        private int NumDocs; // total number of docs seen
        private readonly LinkedList<DocData> PendingDocs; // pending docs
        private DocData CurDoc; // current document
        private FieldData CurField; // current field
        private readonly BytesRef LastTerm;
        private int[] PositionsBuf, StartOffsetsBuf, LengthsBuf, PayloadLengthsBuf;
        private readonly GrowableByteArrayDataOutput TermSuffixes; // buffered term suffixes
        private readonly GrowableByteArrayDataOutput PayloadBytes; // buffered term payloads
        private readonly BlockPackedWriter Writer;

        /// <summary>
        /// Sole constructor. </summary>
        public CompressingTermVectorsWriter(Directory directory, SegmentInfo si, string segmentSuffix, IOContext context, string formatName, CompressionMode compressionMode, int chunkSize)
        {
            Debug.Assert(directory != null);
            this.Directory = directory;
            this.Segment = si.Name;
            this.SegmentSuffix = segmentSuffix;
            this.CompressionMode = compressionMode;
            this.Compressor = compressionMode.NewCompressor();
            this.ChunkSize = chunkSize;

            NumDocs = 0;
            PendingDocs = new LinkedList<DocData>();
            TermSuffixes = new GrowableByteArrayDataOutput(ArrayUtil.Oversize(chunkSize, 1));
            PayloadBytes = new GrowableByteArrayDataOutput(ArrayUtil.Oversize(1, 1));
            LastTerm = new BytesRef(ArrayUtil.Oversize(30, 1));

            bool success = false;
            IndexOutput indexStream = directory.CreateOutput(IndexFileNames.SegmentFileName(Segment, segmentSuffix, VECTORS_INDEX_EXTENSION), context);
            try
            {
                VectorsStream = directory.CreateOutput(IndexFileNames.SegmentFileName(Segment, segmentSuffix, VECTORS_EXTENSION), context);

                string codecNameIdx = formatName + CODEC_SFX_IDX;
                string codecNameDat = formatName + CODEC_SFX_DAT;
                CodecUtil.WriteHeader(indexStream, codecNameIdx, VERSION_CURRENT);
                CodecUtil.WriteHeader(VectorsStream, codecNameDat, VERSION_CURRENT);
                Debug.Assert(CodecUtil.HeaderLength(codecNameDat) == VectorsStream.FilePointer);
                Debug.Assert(CodecUtil.HeaderLength(codecNameIdx) == indexStream.FilePointer);

                IndexWriter = new CompressingStoredFieldsIndexWriter(indexStream);
                indexStream = null;

                VectorsStream.WriteVInt(PackedInts.VERSION_CURRENT);
                VectorsStream.WriteVInt(chunkSize);
                Writer = new BlockPackedWriter(VectorsStream, BLOCK_SIZE);

                PositionsBuf = new int[1024];
                StartOffsetsBuf = new int[1024];
                LengthsBuf = new int[1024];
                PayloadLengthsBuf = new int[1024];

                success = true;
            }
            finally
            {
                if (!success)
                {
                    IOUtils.CloseWhileHandlingException(indexStream);
                    Abort();
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                try
                {
                    IOUtils.Close(VectorsStream, IndexWriter);
                }
                finally
                {
                    VectorsStream = null;
                    IndexWriter = null;
                }
            }
        }

        public override void Abort()
        {
            IOUtils.CloseWhileHandlingException(this);
            IOUtils.DeleteFilesIgnoringExceptions(Directory, IndexFileNames.SegmentFileName(Segment, SegmentSuffix, VECTORS_EXTENSION), IndexFileNames.SegmentFileName(Segment, SegmentSuffix, VECTORS_INDEX_EXTENSION));
        }

        public override void StartDocument(int numVectorFields)
        {
            CurDoc = AddDocData(numVectorFields);
        }

        public override void FinishDocument()
        {
            // append the payload bytes of the doc after its terms
            TermSuffixes.WriteBytes(PayloadBytes.Bytes, PayloadBytes.Length);
            PayloadBytes.Length = 0;
            ++NumDocs;
            if (TriggerFlush())
            {
                Flush();
            }
            CurDoc = null;
        }

        public override void StartField(FieldInfo info, int numTerms, bool positions, bool offsets, bool payloads)
        {
            CurField = CurDoc.AddField(info.Number, numTerms, positions, offsets, payloads);
            LastTerm.Length = 0;
        }

        public override void FinishField()
        {
            CurField = null;
        }

        public override void StartTerm(BytesRef term, int freq)
        {
            Debug.Assert(freq >= 1);
            int prefix = StringHelper.BytesDifference(LastTerm, term);
            CurField.AddTerm(freq, prefix, term.Length - prefix);
            TermSuffixes.WriteBytes(term.Bytes, term.Offset + prefix, term.Length - prefix);
            // copy last term
            if (LastTerm.Bytes.Length < term.Length)
            {
                LastTerm.Bytes = new byte[ArrayUtil.Oversize(term.Length, 1)];
            }
            LastTerm.Offset = 0;
            LastTerm.Length = term.Length;
            Array.Copy(term.Bytes, term.Offset, LastTerm.Bytes, 0, term.Length);
        }

        public override void AddPosition(int position, int startOffset, int endOffset, BytesRef payload)
        {
            Debug.Assert(CurField.Flags != 0);
            CurField.AddPosition(position, startOffset, endOffset - startOffset, payload == null ? 0 : payload.Length);
            if (CurField.HasPayloads && payload != null)
            {
                PayloadBytes.WriteBytes(payload.Bytes, payload.Offset, payload.Length);
            }
        }

        private bool TriggerFlush()
        {
            return TermSuffixes.Length >= ChunkSize || PendingDocs.Count >= MAX_DOCUMENTS_PER_CHUNK;
        }

        private void Flush()
        {
            int chunkDocs = PendingDocs.Count;
            Debug.Assert(chunkDocs > 0, chunkDocs.ToString());

            // write the index file
            IndexWriter.WriteIndex(chunkDocs, VectorsStream.FilePointer);

            int docBase = NumDocs - chunkDocs;
            VectorsStream.WriteVInt(docBase);
            VectorsStream.WriteVInt(chunkDocs);

            // total number of fields of the chunk
            int totalFields = FlushNumFields(chunkDocs);

            if (totalFields > 0)
            {
                // unique field numbers (sorted)
                int[] fieldNums = FlushFieldNums();
                // offsets in the array of unique field numbers
                FlushFields(totalFields, fieldNums);
                // flags (does the field have positions, offsets, payloads?)
                FlushFlags(totalFields, fieldNums);
                // number of terms of each field
                FlushNumTerms(totalFields);
                // prefix and suffix lengths for each field
                FlushTermLengths();
                // term freqs - 1 (because termFreq is always >=1) for each term
                FlushTermFreqs();
                // positions for all terms, when enabled
                FlushPositions();
                // offsets for all terms, when enabled
                FlushOffsets(fieldNums);
                // payload lengths for all terms, when enabled
                FlushPayloadLengths();

                // compress terms and payloads and write them to the output
                Compressor.Compress(TermSuffixes.Bytes, 0, TermSuffixes.Length, VectorsStream);
            }

            // reset
            PendingDocs.Clear();
            CurDoc = null;
            CurField = null;
            TermSuffixes.Length = 0;
        }

        private int FlushNumFields(int chunkDocs)
        {
            if (chunkDocs == 1)
            {
                int numFields = PendingDocs.First.Value.NumFields;
                VectorsStream.WriteVInt(numFields);
                return numFields;
            }
            else
            {
                Writer.Reset(VectorsStream);
                int totalFields = 0;
                foreach (DocData dd in PendingDocs)
                {
                    Writer.Add(dd.NumFields);
                    totalFields += dd.NumFields;
                }
                Writer.Finish();
                return totalFields;
            }
        }

        /// <summary>
        /// Returns a sorted array containing unique field numbers </summary>
        private int[] FlushFieldNums()
        {
            SortedSet<int> fieldNums = new SortedSet<int>();
            foreach (DocData dd in PendingDocs)
            {
                foreach (FieldData fd in dd.Fields)
                {
                    fieldNums.Add(fd.FieldNum);
                }
            }

            int numDistinctFields = fieldNums.Count;
            Debug.Assert(numDistinctFields > 0);
            int bitsRequired = PackedInts.BitsRequired(fieldNums.Last());
            int token = (Math.Min(numDistinctFields - 1, 0x07) << 5) | bitsRequired;
            VectorsStream.WriteByte((byte)(sbyte)token);
            if (numDistinctFields - 1 >= 0x07)
            {
                VectorsStream.WriteVInt(numDistinctFields - 1 - 0x07);
            }
            PackedInts.Writer writer = PackedInts.GetWriterNoHeader(VectorsStream, PackedInts.Format.PACKED, fieldNums.Count, bitsRequired, 1);
            foreach (int fieldNum in fieldNums)
            {
                writer.Add(fieldNum);
            }
            writer.Finish();

            int[] fns = new int[fieldNums.Count];
            int i = 0;
            foreach (int key in fieldNums)
            {
                fns[i++] = key;
            }
            return fns;
        }

        private void FlushFields(int totalFields, int[] fieldNums)
        {
            PackedInts.Writer writer = PackedInts.GetWriterNoHeader(VectorsStream, PackedInts.Format.PACKED, totalFields, PackedInts.BitsRequired(fieldNums.Length - 1), 1);
            foreach (DocData dd in PendingDocs)
            {
                foreach (FieldData fd in dd.Fields)
                {
                    int fieldNumIndex = Array.BinarySearch(fieldNums, fd.FieldNum);
                    Debug.Assert(fieldNumIndex >= 0);
                    writer.Add(fieldNumIndex);
                }
            }
            writer.Finish();
        }

        private void FlushFlags(int totalFields, int[] fieldNums)
        {
            // check if fields always have the same flags
            bool nonChangingFlags = true;
            int[] fieldFlags = new int[fieldNums.Length];
            Arrays.Fill(fieldFlags, -1);
            bool breakOuterLoop;
            foreach (DocData dd in PendingDocs)
            {
                breakOuterLoop = false;
                foreach (FieldData fd in dd.Fields)
                {
                    int fieldNumOff = Array.BinarySearch(fieldNums, fd.FieldNum);
                    Debug.Assert(fieldNumOff >= 0);
                    if (fieldFlags[fieldNumOff] == -1)
                    {
                        fieldFlags[fieldNumOff] = fd.Flags;
                    }
                    else if (fieldFlags[fieldNumOff] != fd.Flags)
                    {
                        nonChangingFlags = false;
                        breakOuterLoop = true;
                    }
                }
                if (breakOuterLoop)
                    break;
            }

            if (nonChangingFlags)
            {
                // write one flag per field num
                VectorsStream.WriteVInt(0);
                PackedInts.Writer writer = PackedInts.GetWriterNoHeader(VectorsStream, PackedInts.Format.PACKED, fieldFlags.Length, FLAGS_BITS, 1);
                foreach (int flags in fieldFlags)
                {
                    Debug.Assert(flags >= 0);
                    writer.Add(flags);
                }
                Debug.Assert(writer.Ord == fieldFlags.Length - 1);
                writer.Finish();
            }
            else
            {
                // write one flag for every field instance
                VectorsStream.WriteVInt(1);
                PackedInts.Writer writer = PackedInts.GetWriterNoHeader(VectorsStream, PackedInts.Format.PACKED, totalFields, FLAGS_BITS, 1);
                foreach (DocData dd in PendingDocs)
                {
                    foreach (FieldData fd in dd.Fields)
                    {
                        writer.Add(fd.Flags);
                    }
                }
                Debug.Assert(writer.Ord == totalFields - 1);
                writer.Finish();
            }
        }

        private void FlushNumTerms(int totalFields)
        {
            int maxNumTerms = 0;
            foreach (DocData dd in PendingDocs)
            {
                foreach (FieldData fd in dd.Fields)
                {
                    maxNumTerms |= fd.NumTerms;
                }
            }
            int bitsRequired = PackedInts.BitsRequired(maxNumTerms);
            VectorsStream.WriteVInt(bitsRequired);
            PackedInts.Writer writer = PackedInts.GetWriterNoHeader(VectorsStream, PackedInts.Format.PACKED, totalFields, bitsRequired, 1);
            foreach (DocData dd in PendingDocs)
            {
                foreach (FieldData fd in dd.Fields)
                {
                    writer.Add(fd.NumTerms);
                }
            }
            Debug.Assert(writer.Ord == totalFields - 1);
            writer.Finish();
        }

        private void FlushTermLengths()
        {
            Writer.Reset(VectorsStream);
            foreach (DocData dd in PendingDocs)
            {
                foreach (FieldData fd in dd.Fields)
                {
                    for (int i = 0; i < fd.NumTerms; ++i)
                    {
                        Writer.Add(fd.PrefixLengths[i]);
                    }
                }
            }
            Writer.Finish();
            Writer.Reset(VectorsStream);
            foreach (DocData dd in PendingDocs)
            {
                foreach (FieldData fd in dd.Fields)
                {
                    for (int i = 0; i < fd.NumTerms; ++i)
                    {
                        Writer.Add(fd.SuffixLengths[i]);
                    }
                }
            }
            Writer.Finish();
        }

        private void FlushTermFreqs()
        {
            Writer.Reset(VectorsStream);
            foreach (DocData dd in PendingDocs)
            {
                foreach (FieldData fd in dd.Fields)
                {
                    for (int i = 0; i < fd.NumTerms; ++i)
                    {
                        Writer.Add(fd.Freqs[i] - 1);
                    }
                }
            }
            Writer.Finish();
        }

        private void FlushPositions()
        {
            Writer.Reset(VectorsStream);
            foreach (DocData dd in PendingDocs)
            {
                foreach (FieldData fd in dd.Fields)
                {
                    if (fd.HasPositions)
                    {
                        int pos = 0;
                        for (int i = 0; i < fd.NumTerms; ++i)
                        {
                            int previousPosition = 0;
                            for (int j = 0; j < fd.Freqs[i]; ++j)
                            {
                                int position = PositionsBuf[fd.PosStart + pos++];
                                Writer.Add(position - previousPosition);
                                previousPosition = position;
                            }
                        }
                        Debug.Assert(pos == fd.TotalPositions);
                    }
                }
            }
            Writer.Finish();
        }

        private void FlushOffsets(int[] fieldNums)
        {
            bool hasOffsets = false;
            long[] sumPos = new long[fieldNums.Length];
            long[] sumOffsets = new long[fieldNums.Length];
            foreach (DocData dd in PendingDocs)
            {
                foreach (FieldData fd in dd.Fields)
                {
                    hasOffsets |= fd.HasOffsets;
                    if (fd.HasOffsets && fd.HasPositions)
                    {
                        int fieldNumOff = Array.BinarySearch(fieldNums, fd.FieldNum);
                        int pos = 0;
                        for (int i = 0; i < fd.NumTerms; ++i)
                        {
                            int previousPos = 0;
                            int previousOff = 0;
                            for (int j = 0; j < fd.Freqs[i]; ++j)
                            {
                                int position = PositionsBuf[fd.PosStart + pos];
                                int startOffset = StartOffsetsBuf[fd.OffStart + pos];
                                sumPos[fieldNumOff] += position - previousPos;
                                sumOffsets[fieldNumOff] += startOffset - previousOff;
                                previousPos = position;
                                previousOff = startOffset;
                                ++pos;
                            }
                        }
                        Debug.Assert(pos == fd.TotalPositions);
                    }
                }
            }

            if (!hasOffsets)
            {
                // nothing to do
                return;
            }

            float[] charsPerTerm = new float[fieldNums.Length];
            for (int i = 0; i < fieldNums.Length; ++i)
            {
                charsPerTerm[i] = (sumPos[i] <= 0 || sumOffsets[i] <= 0) ? 0 : (float)((double)sumOffsets[i] / sumPos[i]);
            }

            // start offsets
            for (int i = 0; i < fieldNums.Length; ++i)
            {
                VectorsStream.WriteInt(Number.FloatToIntBits(charsPerTerm[i]));
            }

            Writer.Reset(VectorsStream);
            foreach (DocData dd in PendingDocs)
            {
                foreach (FieldData fd in dd.Fields)
                {
                    if ((fd.Flags & OFFSETS) != 0)
                    {
                        int fieldNumOff = Array.BinarySearch(fieldNums, fd.FieldNum);
                        float cpt = charsPerTerm[fieldNumOff];
                        int pos = 0;
                        for (int i = 0; i < fd.NumTerms; ++i)
                        {
                            int previousPos = 0;
                            int previousOff = 0;
                            for (int j = 0; j < fd.Freqs[i]; ++j)
                            {
                                int position = fd.HasPositions ? PositionsBuf[fd.PosStart + pos] : 0;
                                int startOffset = StartOffsetsBuf[fd.OffStart + pos];
                                Writer.Add(startOffset - previousOff - (int)(cpt * (position - previousPos)));
                                previousPos = position;
                                previousOff = startOffset;
                                ++pos;
                            }
                        }
                    }
                }
            }
            Writer.Finish();

            // lengths
            Writer.Reset(VectorsStream);
            foreach (DocData dd in PendingDocs)
            {
                foreach (FieldData fd in dd.Fields)
                {
                    if ((fd.Flags & OFFSETS) != 0)
                    {
                        int pos = 0;
                        for (int i = 0; i < fd.NumTerms; ++i)
                        {
                            for (int j = 0; j < fd.Freqs[i]; ++j)
                            {
                                Writer.Add(LengthsBuf[fd.OffStart + pos++] - fd.PrefixLengths[i] - fd.SuffixLengths[i]);
                            }
                        }
                        Debug.Assert(pos == fd.TotalPositions);
                    }
                }
            }
            Writer.Finish();
        }

        private void FlushPayloadLengths()
        {
            Writer.Reset(VectorsStream);
            foreach (DocData dd in PendingDocs)
            {
                foreach (FieldData fd in dd.Fields)
                {
                    if (fd.HasPayloads)
                    {
                        for (int i = 0; i < fd.TotalPositions; ++i)
                        {
                            Writer.Add(PayloadLengthsBuf[fd.PayStart + i]);
                        }
                    }
                }
            }
            Writer.Finish();
        }

        public override void Finish(FieldInfos fis, int numDocs)
        {
            if (!(PendingDocs.Count == 0))
            {
                Flush();
            }
            if (numDocs != this.NumDocs)
            {
                throw new Exception("Wrote " + this.NumDocs + " docs, finish called with numDocs=" + numDocs);
            }
            IndexWriter.Finish(numDocs, VectorsStream.FilePointer);
            CodecUtil.WriteFooter(VectorsStream);
        }

        public override IComparer<BytesRef> Comparator
        {
            get
            {
                return BytesRef.UTF8SortedAsUnicodeComparer;
            }
        }

        public override void AddProx(int numProx, DataInput positions, DataInput offsets)
        {
            Debug.Assert((CurField.HasPositions) == (positions != null));
            Debug.Assert((CurField.HasOffsets) == (offsets != null));

            if (CurField.HasPositions)
            {
                int posStart = CurField.PosStart + CurField.TotalPositions;
                if (posStart + numProx > PositionsBuf.Length)
                {
                    PositionsBuf = ArrayUtil.Grow(PositionsBuf, posStart + numProx);
                }
                int position = 0;
                if (CurField.HasPayloads)
                {
                    int payStart = CurField.PayStart + CurField.TotalPositions;
                    if (payStart + numProx > PayloadLengthsBuf.Length)
                    {
                        PayloadLengthsBuf = ArrayUtil.Grow(PayloadLengthsBuf, payStart + numProx);
                    }
                    for (int i = 0; i < numProx; ++i)
                    {
                        int code = positions.ReadVInt();
                        if ((code & 1) != 0)
                        {
                            // this position has a payload
                            int payloadLength = positions.ReadVInt();
                            PayloadLengthsBuf[payStart + i] = payloadLength;
                            PayloadBytes.CopyBytes(positions, payloadLength);
                        }
                        else
                        {
                            PayloadLengthsBuf[payStart + i] = 0;
                        }
                        position += (int)((uint)code >> 1);
                        PositionsBuf[posStart + i] = position;
                    }
                }
                else
                {
                    for (int i = 0; i < numProx; ++i)
                    {
                        position += ((int)((uint)positions.ReadVInt() >> 1));
                        PositionsBuf[posStart + i] = position;
                    }
                }
            }

            if (CurField.HasOffsets)
            {
                int offStart = CurField.OffStart + CurField.TotalPositions;
                if (offStart + numProx > StartOffsetsBuf.Length)
                {
                    int newLength = ArrayUtil.Oversize(offStart + numProx, 4);
                    StartOffsetsBuf = Arrays.CopyOf(StartOffsetsBuf, newLength);
                    LengthsBuf = Arrays.CopyOf(LengthsBuf, newLength);
                }
                int lastOffset = 0, startOffset, endOffset;
                for (int i = 0; i < numProx; ++i)
                {
                    startOffset = lastOffset + offsets.ReadVInt();
                    endOffset = startOffset + offsets.ReadVInt();
                    lastOffset = endOffset;
                    StartOffsetsBuf[offStart + i] = startOffset;
                    LengthsBuf[offStart + i] = endOffset - startOffset;
                }
            }

            CurField.TotalPositions += numProx;
        }

        public override int Merge(MergeState mergeState)
        {
            int docCount = 0;
            int idx = 0;

            foreach (AtomicReader reader in mergeState.Readers)
            {
                SegmentReader matchingSegmentReader = mergeState.MatchingSegmentReaders[idx++];
                CompressingTermVectorsReader matchingVectorsReader = null;
                if (matchingSegmentReader != null)
                {
                    TermVectorsReader vectorsReader = matchingSegmentReader.TermVectorsReader;
                    // we can only bulk-copy if the matching reader is also a CompressingTermVectorsReader
                    if (vectorsReader != null && vectorsReader is CompressingTermVectorsReader)
                    {
                        matchingVectorsReader = (CompressingTermVectorsReader)vectorsReader;
                    }
                }

                int maxDoc = reader.MaxDoc;
                IBits liveDocs = reader.LiveDocs;

                if (matchingVectorsReader == null || matchingVectorsReader.Version != VERSION_CURRENT || matchingVectorsReader.CompressionMode != CompressionMode || matchingVectorsReader.ChunkSize != ChunkSize || matchingVectorsReader.PackedIntsVersion != PackedInts.VERSION_CURRENT)
                {
                    // naive merge...
                    for (int i = NextLiveDoc(0, liveDocs, maxDoc); i < maxDoc; i = NextLiveDoc(i + 1, liveDocs, maxDoc))
                    {
                        Fields vectors = reader.GetTermVectors(i);
                        AddAllDocVectors(vectors, mergeState);
                        ++docCount;
                        mergeState.CheckAbort.Work(300);
                    }
                }
                else
                {
                    CompressingStoredFieldsIndexReader index = matchingVectorsReader.Index;
                    IndexInput vectorsStreamOrig = matchingVectorsReader.VectorsStream;
                    vectorsStreamOrig.Seek(0);
                    ChecksumIndexInput vectorsStream = new BufferedChecksumIndexInput((IndexInput)vectorsStreamOrig.Clone());

                    for (int i = NextLiveDoc(0, liveDocs, maxDoc); i < maxDoc; )
                    {
                        // We make sure to move the checksum input in any case, otherwise the final
                        // integrity check might need to read the whole file a second time
                        long startPointer = index.GetStartPointer(i);
                        if (startPointer > vectorsStream.FilePointer)
                        {
                            vectorsStream.Seek(startPointer);
                        }
                        if ((PendingDocs.Count == 0) && (i == 0 || index.GetStartPointer(i - 1) < startPointer)) // start of a chunk
                        {
                            int docBase = vectorsStream.ReadVInt();
                            int chunkDocs = vectorsStream.ReadVInt();
                            Debug.Assert(docBase + chunkDocs <= matchingSegmentReader.MaxDoc);
                            if (docBase + chunkDocs < matchingSegmentReader.MaxDoc && NextDeletedDoc(docBase, liveDocs, docBase + chunkDocs) == docBase + chunkDocs)
                            {
                                long chunkEnd = index.GetStartPointer(docBase + chunkDocs);
                                long chunkLength = chunkEnd - vectorsStream.FilePointer;
                                IndexWriter.WriteIndex(chunkDocs, this.VectorsStream.FilePointer);
                                this.VectorsStream.WriteVInt(docCount);
                                this.VectorsStream.WriteVInt(chunkDocs);
                                this.VectorsStream.CopyBytes(vectorsStream, chunkLength);
                                docCount += chunkDocs;
                                this.NumDocs += chunkDocs;
                                mergeState.CheckAbort.Work(300 * chunkDocs);
                                i = NextLiveDoc(docBase + chunkDocs, liveDocs, maxDoc);
                            }
                            else
                            {
                                for (; i < docBase + chunkDocs; i = NextLiveDoc(i + 1, liveDocs, maxDoc))
                                {
                                    Fields vectors = reader.GetTermVectors(i);
                                    AddAllDocVectors(vectors, mergeState);
                                    ++docCount;
                                    mergeState.CheckAbort.Work(300);
                                }
                            }
                        }
                        else
                        {
                            Fields vectors = reader.GetTermVectors(i);
                            AddAllDocVectors(vectors, mergeState);
                            ++docCount;
                            mergeState.CheckAbort.Work(300);
                            i = NextLiveDoc(i + 1, liveDocs, maxDoc);
                        }
                    }

                    vectorsStream.Seek(vectorsStream.Length - CodecUtil.FooterLength());
                    CodecUtil.CheckFooter(vectorsStream);
                }
            }
            Finish(mergeState.FieldInfos, docCount);
            return docCount;
        }

        private static int NextLiveDoc(int doc, IBits liveDocs, int maxDoc)
        {
            if (liveDocs == null)
            {
                return doc;
            }
            while (doc < maxDoc && !liveDocs.Get(doc))
            {
                ++doc;
            }
            return doc;
        }

        private static int NextDeletedDoc(int doc, IBits liveDocs, int maxDoc)
        {
            if (liveDocs == null)
            {
                return maxDoc;
            }
            while (doc < maxDoc && liveDocs.Get(doc))
            {
                ++doc;
            }
            return doc;
        }
    }
}