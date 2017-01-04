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

        private readonly Directory directory;
        private readonly string segment;
        private readonly string segmentSuffix;
        private CompressingStoredFieldsIndexWriter indexWriter;
        private IndexOutput vectorsStream;

        private readonly CompressionMode compressionMode;
        private readonly Compressor compressor;
        private readonly int chunkSize;

        /// <summary>
        /// a pending doc </summary>
        private class DocData
        {
            private readonly CompressingTermVectorsWriter outerInstance;

            internal readonly int numFields;
            internal readonly LinkedList<FieldData> fields;
            internal readonly int posStart, offStart, payStart;

            internal DocData(CompressingTermVectorsWriter outerInstance, int numFields, int posStart, int offStart, int payStart)
            {
                this.outerInstance = outerInstance;
                this.numFields = numFields;
                this.fields = new LinkedList<FieldData>();
                this.posStart = posStart;
                this.offStart = offStart;
                this.payStart = payStart;
            }

            internal virtual FieldData AddField(int fieldNum, int numTerms, bool positions, bool offsets, bool payloads)
            {
                FieldData field;
                if (fields.Count == 0)
                {
                    field = new FieldData(outerInstance, fieldNum, numTerms, positions, offsets, payloads, posStart, offStart, payStart);
                }
                else
                {
                    FieldData last = fields.Last.Value;
                    int posStart = last.posStart + (last.hasPositions ? last.totalPositions : 0);
                    int offStart = last.offStart + (last.hasOffsets ? last.totalPositions : 0);
                    int payStart = last.payStart + (last.hasPayloads ? last.totalPositions : 0);
                    field = new FieldData(outerInstance, fieldNum, numTerms, positions, offsets, payloads, posStart, offStart, payStart);
                }
                fields.AddLast(field);
                return field;
            }
        }

        private DocData AddDocData(int numVectorFields)
        {
            FieldData last = null;
            //for (IEnumerator<DocData> it = PendingDocs.Reverse(); it.MoveNext();)
            foreach (DocData doc in pendingDocs.Reverse())
            {
                if (!(doc.fields.Count == 0))
                {
                    last = doc.fields.Last.Value;
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
                int posStart = last.posStart + (last.hasPositions ? last.totalPositions : 0);
                int offStart = last.offStart + (last.hasOffsets ? last.totalPositions : 0);
                int payStart = last.payStart + (last.hasPayloads ? last.totalPositions : 0);
                newDoc = new DocData(this, numVectorFields, posStart, offStart, payStart);
            }
            pendingDocs.AddLast(newDoc);
            return newDoc;
        }

        /// <summary>
        /// a pending field </summary>
        private class FieldData
        {
            private readonly CompressingTermVectorsWriter outerInstance;

            internal readonly bool hasPositions, hasOffsets, hasPayloads;
            internal readonly int fieldNum, flags, numTerms;
            internal readonly int[] freqs, prefixLengths, suffixLengths;
            internal readonly int posStart, offStart, payStart;
            internal int totalPositions;
            internal int ord;

            internal FieldData(CompressingTermVectorsWriter outerInstance, int fieldNum, int numTerms, bool positions, bool offsets, bool payloads, int posStart, int offStart, int payStart)
            {
                this.outerInstance = outerInstance;
                this.fieldNum = fieldNum;
                this.numTerms = numTerms;
                this.hasPositions = positions;
                this.hasOffsets = offsets;
                this.hasPayloads = payloads;
                this.flags = (positions ? POSITIONS : 0) | (offsets ? OFFSETS : 0) | (payloads ? PAYLOADS : 0);
                this.freqs = new int[numTerms];
                this.prefixLengths = new int[numTerms];
                this.suffixLengths = new int[numTerms];
                this.posStart = posStart;
                this.offStart = offStart;
                this.payStart = payStart;
                totalPositions = 0;
                ord = 0;
            }

            internal virtual void AddTerm(int freq, int prefixLength, int suffixLength)
            {
                freqs[ord] = freq;
                prefixLengths[ord] = prefixLength;
                suffixLengths[ord] = suffixLength;
                ++ord;
            }

            internal virtual void AddPosition(int position, int startOffset, int length, int payloadLength)
            {
                if (hasPositions)
                {
                    if (posStart + totalPositions == outerInstance.positionsBuf.Length)
                    {
                        outerInstance.positionsBuf = ArrayUtil.Grow(outerInstance.positionsBuf);
                    }
                    outerInstance.positionsBuf[posStart + totalPositions] = position;
                }
                if (hasOffsets)
                {
                    if (offStart + totalPositions == outerInstance.startOffsetsBuf.Length)
                    {
                        int newLength = ArrayUtil.Oversize(offStart + totalPositions, 4);
                        outerInstance.startOffsetsBuf = Arrays.CopyOf(outerInstance.startOffsetsBuf, newLength);
                        outerInstance.lengthsBuf = Arrays.CopyOf(outerInstance.lengthsBuf, newLength);
                    }
                    outerInstance.startOffsetsBuf[offStart + totalPositions] = startOffset;
                    outerInstance.lengthsBuf[offStart + totalPositions] = length;
                }
                if (hasPayloads)
                {
                    if (payStart + totalPositions == outerInstance.payloadLengthsBuf.Length)
                    {
                        outerInstance.payloadLengthsBuf = ArrayUtil.Grow(outerInstance.payloadLengthsBuf);
                    }
                    outerInstance.payloadLengthsBuf[payStart + totalPositions] = payloadLength;
                }
                ++totalPositions;
            }
        }

        private int numDocs; // total number of docs seen
        private readonly LinkedList<DocData> pendingDocs; // pending docs
        private DocData curDoc; // current document
        private FieldData curField; // current field
        private readonly BytesRef lastTerm;
        private int[] positionsBuf, startOffsetsBuf, lengthsBuf, payloadLengthsBuf;
        private readonly GrowableByteArrayDataOutput termSuffixes; // buffered term suffixes
        private readonly GrowableByteArrayDataOutput payloadBytes; // buffered term payloads
        private readonly BlockPackedWriter writer;

        /// <summary>
        /// Sole constructor. </summary>
        public CompressingTermVectorsWriter(Directory directory, SegmentInfo si, string segmentSuffix, IOContext context, string formatName, CompressionMode compressionMode, int chunkSize)
        {
            Debug.Assert(directory != null);
            this.directory = directory;
            this.segment = si.Name;
            this.segmentSuffix = segmentSuffix;
            this.compressionMode = compressionMode;
            this.compressor = compressionMode.NewCompressor();
            this.chunkSize = chunkSize;

            numDocs = 0;
            pendingDocs = new LinkedList<DocData>();
            termSuffixes = new GrowableByteArrayDataOutput(ArrayUtil.Oversize(chunkSize, 1));
            payloadBytes = new GrowableByteArrayDataOutput(ArrayUtil.Oversize(1, 1));
            lastTerm = new BytesRef(ArrayUtil.Oversize(30, 1));

            bool success = false;
            IndexOutput indexStream = directory.CreateOutput(IndexFileNames.SegmentFileName(segment, segmentSuffix, VECTORS_INDEX_EXTENSION), context);
            try
            {
                vectorsStream = directory.CreateOutput(IndexFileNames.SegmentFileName(segment, segmentSuffix, VECTORS_EXTENSION), context);

                string codecNameIdx = formatName + CODEC_SFX_IDX;
                string codecNameDat = formatName + CODEC_SFX_DAT;
                CodecUtil.WriteHeader(indexStream, codecNameIdx, VERSION_CURRENT);
                CodecUtil.WriteHeader(vectorsStream, codecNameDat, VERSION_CURRENT);
                Debug.Assert(CodecUtil.HeaderLength(codecNameDat) == vectorsStream.FilePointer);
                Debug.Assert(CodecUtil.HeaderLength(codecNameIdx) == indexStream.FilePointer);

                indexWriter = new CompressingStoredFieldsIndexWriter(indexStream);
                indexStream = null;

                vectorsStream.WriteVInt(PackedInts.VERSION_CURRENT);
                vectorsStream.WriteVInt(chunkSize);
                writer = new BlockPackedWriter(vectorsStream, BLOCK_SIZE);

                positionsBuf = new int[1024];
                startOffsetsBuf = new int[1024];
                lengthsBuf = new int[1024];
                payloadLengthsBuf = new int[1024];

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
                    IOUtils.Close(vectorsStream, indexWriter);
                }
                finally
                {
                    vectorsStream = null;
                    indexWriter = null;
                }
            }
        }

        public override void Abort()
        {
            IOUtils.CloseWhileHandlingException(this);
            IOUtils.DeleteFilesIgnoringExceptions(directory, IndexFileNames.SegmentFileName(segment, segmentSuffix, VECTORS_EXTENSION), IndexFileNames.SegmentFileName(segment, segmentSuffix, VECTORS_INDEX_EXTENSION));
        }

        public override void StartDocument(int numVectorFields)
        {
            curDoc = AddDocData(numVectorFields);
        }

        public override void FinishDocument()
        {
            // append the payload bytes of the doc after its terms
            termSuffixes.WriteBytes(payloadBytes.Bytes, payloadBytes.Length);
            payloadBytes.Length = 0;
            ++numDocs;
            if (TriggerFlush())
            {
                Flush();
            }
            curDoc = null;
        }

        public override void StartField(FieldInfo info, int numTerms, bool positions, bool offsets, bool payloads)
        {
            curField = curDoc.AddField(info.Number, numTerms, positions, offsets, payloads);
            lastTerm.Length = 0;
        }

        public override void FinishField()
        {
            curField = null;
        }

        public override void StartTerm(BytesRef term, int freq)
        {
            Debug.Assert(freq >= 1);
            int prefix = StringHelper.BytesDifference(lastTerm, term);
            curField.AddTerm(freq, prefix, term.Length - prefix);
            termSuffixes.WriteBytes(term.Bytes, term.Offset + prefix, term.Length - prefix);
            // copy last term
            if (lastTerm.Bytes.Length < term.Length)
            {
                lastTerm.Bytes = new byte[ArrayUtil.Oversize(term.Length, 1)];
            }
            lastTerm.Offset = 0;
            lastTerm.Length = term.Length;
            Array.Copy(term.Bytes, term.Offset, lastTerm.Bytes, 0, term.Length);
        }

        public override void AddPosition(int position, int startOffset, int endOffset, BytesRef payload)
        {
            Debug.Assert(curField.flags != 0);
            curField.AddPosition(position, startOffset, endOffset - startOffset, payload == null ? 0 : payload.Length);
            if (curField.hasPayloads && payload != null)
            {
                payloadBytes.WriteBytes(payload.Bytes, payload.Offset, payload.Length);
            }
        }

        private bool TriggerFlush()
        {
            return termSuffixes.Length >= chunkSize || pendingDocs.Count >= MAX_DOCUMENTS_PER_CHUNK;
        }

        private void Flush()
        {
            int chunkDocs = pendingDocs.Count;
            Debug.Assert(chunkDocs > 0, chunkDocs.ToString());

            // write the index file
            indexWriter.WriteIndex(chunkDocs, vectorsStream.FilePointer);

            int docBase = numDocs - chunkDocs;
            vectorsStream.WriteVInt(docBase);
            vectorsStream.WriteVInt(chunkDocs);

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
                compressor.Compress(termSuffixes.Bytes, 0, termSuffixes.Length, vectorsStream);
            }

            // reset
            pendingDocs.Clear();
            curDoc = null;
            curField = null;
            termSuffixes.Length = 0;
        }

        private int FlushNumFields(int chunkDocs)
        {
            if (chunkDocs == 1)
            {
                int numFields = pendingDocs.First.Value.numFields;
                vectorsStream.WriteVInt(numFields);
                return numFields;
            }
            else
            {
                writer.Reset(vectorsStream);
                int totalFields = 0;
                foreach (DocData dd in pendingDocs)
                {
                    writer.Add(dd.numFields);
                    totalFields += dd.numFields;
                }
                writer.Finish();
                return totalFields;
            }
        }

        /// <summary>
        /// Returns a sorted array containing unique field numbers </summary>
        private int[] FlushFieldNums()
        {
            SortedSet<int> fieldNums = new SortedSet<int>();
            foreach (DocData dd in pendingDocs)
            {
                foreach (FieldData fd in dd.fields)
                {
                    fieldNums.Add(fd.fieldNum);
                }
            }

            int numDistinctFields = fieldNums.Count;
            Debug.Assert(numDistinctFields > 0);
            int bitsRequired = PackedInts.BitsRequired(fieldNums.Last());
            int token = (Math.Min(numDistinctFields - 1, 0x07) << 5) | bitsRequired;
            vectorsStream.WriteByte((byte)(sbyte)token);
            if (numDistinctFields - 1 >= 0x07)
            {
                vectorsStream.WriteVInt(numDistinctFields - 1 - 0x07);
            }
            PackedInts.Writer writer = PackedInts.GetWriterNoHeader(vectorsStream, PackedInts.Format.PACKED, fieldNums.Count, bitsRequired, 1);
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
            PackedInts.Writer writer = PackedInts.GetWriterNoHeader(vectorsStream, PackedInts.Format.PACKED, totalFields, PackedInts.BitsRequired(fieldNums.Length - 1), 1);
            foreach (DocData dd in pendingDocs)
            {
                foreach (FieldData fd in dd.fields)
                {
                    int fieldNumIndex = Array.BinarySearch(fieldNums, fd.fieldNum);
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
            foreach (DocData dd in pendingDocs)
            {
                breakOuterLoop = false;
                foreach (FieldData fd in dd.fields)
                {
                    int fieldNumOff = Array.BinarySearch(fieldNums, fd.fieldNum);
                    Debug.Assert(fieldNumOff >= 0);
                    if (fieldFlags[fieldNumOff] == -1)
                    {
                        fieldFlags[fieldNumOff] = fd.flags;
                    }
                    else if (fieldFlags[fieldNumOff] != fd.flags)
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
                vectorsStream.WriteVInt(0);
                PackedInts.Writer writer = PackedInts.GetWriterNoHeader(vectorsStream, PackedInts.Format.PACKED, fieldFlags.Length, FLAGS_BITS, 1);
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
                vectorsStream.WriteVInt(1);
                PackedInts.Writer writer = PackedInts.GetWriterNoHeader(vectorsStream, PackedInts.Format.PACKED, totalFields, FLAGS_BITS, 1);
                foreach (DocData dd in pendingDocs)
                {
                    foreach (FieldData fd in dd.fields)
                    {
                        writer.Add(fd.flags);
                    }
                }
                Debug.Assert(writer.Ord == totalFields - 1);
                writer.Finish();
            }
        }

        private void FlushNumTerms(int totalFields)
        {
            int maxNumTerms = 0;
            foreach (DocData dd in pendingDocs)
            {
                foreach (FieldData fd in dd.fields)
                {
                    maxNumTerms |= fd.numTerms;
                }
            }
            int bitsRequired = PackedInts.BitsRequired(maxNumTerms);
            vectorsStream.WriteVInt(bitsRequired);
            PackedInts.Writer writer = PackedInts.GetWriterNoHeader(vectorsStream, PackedInts.Format.PACKED, totalFields, bitsRequired, 1);
            foreach (DocData dd in pendingDocs)
            {
                foreach (FieldData fd in dd.fields)
                {
                    writer.Add(fd.numTerms);
                }
            }
            Debug.Assert(writer.Ord == totalFields - 1);
            writer.Finish();
        }

        private void FlushTermLengths()
        {
            writer.Reset(vectorsStream);
            foreach (DocData dd in pendingDocs)
            {
                foreach (FieldData fd in dd.fields)
                {
                    for (int i = 0; i < fd.numTerms; ++i)
                    {
                        writer.Add(fd.prefixLengths[i]);
                    }
                }
            }
            writer.Finish();
            writer.Reset(vectorsStream);
            foreach (DocData dd in pendingDocs)
            {
                foreach (FieldData fd in dd.fields)
                {
                    for (int i = 0; i < fd.numTerms; ++i)
                    {
                        writer.Add(fd.suffixLengths[i]);
                    }
                }
            }
            writer.Finish();
        }

        private void FlushTermFreqs()
        {
            writer.Reset(vectorsStream);
            foreach (DocData dd in pendingDocs)
            {
                foreach (FieldData fd in dd.fields)
                {
                    for (int i = 0; i < fd.numTerms; ++i)
                    {
                        writer.Add(fd.freqs[i] - 1);
                    }
                }
            }
            writer.Finish();
        }

        private void FlushPositions()
        {
            writer.Reset(vectorsStream);
            foreach (DocData dd in pendingDocs)
            {
                foreach (FieldData fd in dd.fields)
                {
                    if (fd.hasPositions)
                    {
                        int pos = 0;
                        for (int i = 0; i < fd.numTerms; ++i)
                        {
                            int previousPosition = 0;
                            for (int j = 0; j < fd.freqs[i]; ++j)
                            {
                                int position = positionsBuf[fd.posStart + pos++];
                                writer.Add(position - previousPosition);
                                previousPosition = position;
                            }
                        }
                        Debug.Assert(pos == fd.totalPositions);
                    }
                }
            }
            writer.Finish();
        }

        private void FlushOffsets(int[] fieldNums)
        {
            bool hasOffsets = false;
            long[] sumPos = new long[fieldNums.Length];
            long[] sumOffsets = new long[fieldNums.Length];
            foreach (DocData dd in pendingDocs)
            {
                foreach (FieldData fd in dd.fields)
                {
                    hasOffsets |= fd.hasOffsets;
                    if (fd.hasOffsets && fd.hasPositions)
                    {
                        int fieldNumOff = Array.BinarySearch(fieldNums, fd.fieldNum);
                        int pos = 0;
                        for (int i = 0; i < fd.numTerms; ++i)
                        {
                            int previousPos = 0;
                            int previousOff = 0;
                            for (int j = 0; j < fd.freqs[i]; ++j)
                            {
                                int position = positionsBuf[fd.posStart + pos];
                                int startOffset = startOffsetsBuf[fd.offStart + pos];
                                sumPos[fieldNumOff] += position - previousPos;
                                sumOffsets[fieldNumOff] += startOffset - previousOff;
                                previousPos = position;
                                previousOff = startOffset;
                                ++pos;
                            }
                        }
                        Debug.Assert(pos == fd.totalPositions);
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
                vectorsStream.WriteInt(Number.FloatToIntBits(charsPerTerm[i]));
            }

            writer.Reset(vectorsStream);
            foreach (DocData dd in pendingDocs)
            {
                foreach (FieldData fd in dd.fields)
                {
                    if ((fd.flags & OFFSETS) != 0)
                    {
                        int fieldNumOff = Array.BinarySearch(fieldNums, fd.fieldNum);
                        float cpt = charsPerTerm[fieldNumOff];
                        int pos = 0;
                        for (int i = 0; i < fd.numTerms; ++i)
                        {
                            int previousPos = 0;
                            int previousOff = 0;
                            for (int j = 0; j < fd.freqs[i]; ++j)
                            {
                                int position = fd.hasPositions ? positionsBuf[fd.posStart + pos] : 0;
                                int startOffset = startOffsetsBuf[fd.offStart + pos];
                                writer.Add(startOffset - previousOff - (int)(cpt * (position - previousPos)));
                                previousPos = position;
                                previousOff = startOffset;
                                ++pos;
                            }
                        }
                    }
                }
            }
            writer.Finish();

            // lengths
            writer.Reset(vectorsStream);
            foreach (DocData dd in pendingDocs)
            {
                foreach (FieldData fd in dd.fields)
                {
                    if ((fd.flags & OFFSETS) != 0)
                    {
                        int pos = 0;
                        for (int i = 0; i < fd.numTerms; ++i)
                        {
                            for (int j = 0; j < fd.freqs[i]; ++j)
                            {
                                writer.Add(lengthsBuf[fd.offStart + pos++] - fd.prefixLengths[i] - fd.suffixLengths[i]);
                            }
                        }
                        Debug.Assert(pos == fd.totalPositions);
                    }
                }
            }
            writer.Finish();
        }

        private void FlushPayloadLengths()
        {
            writer.Reset(vectorsStream);
            foreach (DocData dd in pendingDocs)
            {
                foreach (FieldData fd in dd.fields)
                {
                    if (fd.hasPayloads)
                    {
                        for (int i = 0; i < fd.totalPositions; ++i)
                        {
                            writer.Add(payloadLengthsBuf[fd.payStart + i]);
                        }
                    }
                }
            }
            writer.Finish();
        }

        public override void Finish(FieldInfos fis, int numDocs)
        {
            if (!(pendingDocs.Count == 0))
            {
                Flush();
            }
            if (numDocs != this.numDocs)
            {
                throw new Exception("Wrote " + this.numDocs + " docs, finish called with numDocs=" + numDocs);
            }
            indexWriter.Finish(numDocs, vectorsStream.FilePointer);
            CodecUtil.WriteFooter(vectorsStream);
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
            Debug.Assert((curField.hasPositions) == (positions != null));
            Debug.Assert((curField.hasOffsets) == (offsets != null));

            if (curField.hasPositions)
            {
                int posStart = curField.posStart + curField.totalPositions;
                if (posStart + numProx > positionsBuf.Length)
                {
                    positionsBuf = ArrayUtil.Grow(positionsBuf, posStart + numProx);
                }
                int position = 0;
                if (curField.hasPayloads)
                {
                    int payStart = curField.payStart + curField.totalPositions;
                    if (payStart + numProx > payloadLengthsBuf.Length)
                    {
                        payloadLengthsBuf = ArrayUtil.Grow(payloadLengthsBuf, payStart + numProx);
                    }
                    for (int i = 0; i < numProx; ++i)
                    {
                        int code = positions.ReadVInt();
                        if ((code & 1) != 0)
                        {
                            // this position has a payload
                            int payloadLength = positions.ReadVInt();
                            payloadLengthsBuf[payStart + i] = payloadLength;
                            payloadBytes.CopyBytes(positions, payloadLength);
                        }
                        else
                        {
                            payloadLengthsBuf[payStart + i] = 0;
                        }
                        position += (int)((uint)code >> 1);
                        positionsBuf[posStart + i] = position;
                    }
                }
                else
                {
                    for (int i = 0; i < numProx; ++i)
                    {
                        position += ((int)((uint)positions.ReadVInt() >> 1));
                        positionsBuf[posStart + i] = position;
                    }
                }
            }

            if (curField.hasOffsets)
            {
                int offStart = curField.offStart + curField.totalPositions;
                if (offStart + numProx > startOffsetsBuf.Length)
                {
                    int newLength = ArrayUtil.Oversize(offStart + numProx, 4);
                    startOffsetsBuf = Arrays.CopyOf(startOffsetsBuf, newLength);
                    lengthsBuf = Arrays.CopyOf(lengthsBuf, newLength);
                }
                int lastOffset = 0, startOffset, endOffset;
                for (int i = 0; i < numProx; ++i)
                {
                    startOffset = lastOffset + offsets.ReadVInt();
                    endOffset = startOffset + offsets.ReadVInt();
                    lastOffset = endOffset;
                    startOffsetsBuf[offStart + i] = startOffset;
                    lengthsBuf[offStart + i] = endOffset - startOffset;
                }
            }

            curField.totalPositions += numProx;
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

                if (matchingVectorsReader == null || matchingVectorsReader.Version != VERSION_CURRENT || matchingVectorsReader.CompressionMode != compressionMode || matchingVectorsReader.ChunkSize != chunkSize || matchingVectorsReader.PackedIntsVersion != PackedInts.VERSION_CURRENT)
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
                        if ((pendingDocs.Count == 0) && (i == 0 || index.GetStartPointer(i - 1) < startPointer)) // start of a chunk
                        {
                            int docBase = vectorsStream.ReadVInt();
                            int chunkDocs = vectorsStream.ReadVInt();
                            Debug.Assert(docBase + chunkDocs <= matchingSegmentReader.MaxDoc);
                            if (docBase + chunkDocs < matchingSegmentReader.MaxDoc && NextDeletedDoc(docBase, liveDocs, docBase + chunkDocs) == docBase + chunkDocs)
                            {
                                long chunkEnd = index.GetStartPointer(docBase + chunkDocs);
                                long chunkLength = chunkEnd - vectorsStream.FilePointer;
                                indexWriter.WriteIndex(chunkDocs, this.vectorsStream.FilePointer);
                                this.vectorsStream.WriteVInt(docCount);
                                this.vectorsStream.WriteVInt(chunkDocs);
                                this.vectorsStream.CopyBytes(vectorsStream, chunkLength);
                                docCount += chunkDocs;
                                this.numDocs += chunkDocs;
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