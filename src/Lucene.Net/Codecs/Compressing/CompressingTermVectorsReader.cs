using J2N.Numerics;
using Lucene.Net.Diagnostics;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Lucene.Net.Util.Packed;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

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

    /// <summary>
    /// <see cref="TermVectorsReader"/> for <see cref="CompressingTermVectorsFormat"/>.
    /// <para/>
    /// @lucene.experimental
    /// </summary>
    public sealed class CompressingTermVectorsReader : TermVectorsReader // LUCENENET specific - removed IDisposable, it is already implemented in base class
    {
        private readonly FieldInfos fieldInfos;
        internal readonly CompressingStoredFieldsIndexReader indexReader;
#pragma warning disable CA2213 // Disposable fields should be disposed
        internal readonly IndexInput vectorsStream;
#pragma warning restore CA2213 // Disposable fields should be disposed
        private readonly int version;
        private readonly int packedIntsVersion;
        private readonly CompressionMode compressionMode;
        private readonly Decompressor decompressor;
        private readonly int chunkSize;
        private readonly int numDocs;
        private bool closed;
        private readonly BlockPackedReaderIterator reader;

        // used by clone
        private CompressingTermVectorsReader(CompressingTermVectorsReader reader)
        {
            this.fieldInfos = reader.fieldInfos;
            this.vectorsStream = (IndexInput)reader.vectorsStream.Clone();
            this.indexReader = (CompressingStoredFieldsIndexReader)reader.indexReader.Clone();
            this.packedIntsVersion = reader.packedIntsVersion;
            this.compressionMode = reader.compressionMode;
            this.decompressor = (Decompressor)reader.decompressor.Clone();
            this.chunkSize = reader.chunkSize;
            this.numDocs = reader.numDocs;
            this.reader = new BlockPackedReaderIterator(vectorsStream, packedIntsVersion, CompressingTermVectorsWriter.BLOCK_SIZE, 0);
            this.version = reader.version;
            this.closed = false;
        }

        /// <summary>
        /// Sole constructor. </summary>
        public CompressingTermVectorsReader(Directory d, SegmentInfo si, string segmentSuffix, FieldInfos fn, IOContext context, string formatName, CompressionMode compressionMode)
        {
            this.compressionMode = compressionMode;
            string segment = si.Name;
            bool success = false;
            fieldInfos = fn;
            numDocs = si.DocCount;
            ChecksumIndexInput indexStream = null;
            try
            {
                // Load the index into memory
                string indexStreamFN = IndexFileNames.SegmentFileName(segment, segmentSuffix, CompressingTermVectorsWriter.VECTORS_INDEX_EXTENSION);
                indexStream = d.OpenChecksumInput(indexStreamFN, context);
                string codecNameIdx = formatName + CompressingTermVectorsWriter.CODEC_SFX_IDX;
                version = CodecUtil.CheckHeader(indexStream, codecNameIdx, CompressingTermVectorsWriter.VERSION_START, CompressingTermVectorsWriter.VERSION_CURRENT);
                if (Debugging.AssertsEnabled) Debugging.Assert(CodecUtil.HeaderLength(codecNameIdx) == indexStream.Position); // LUCENENET specific: Renamed from getFilePointer() to match FileStream
                indexReader = new CompressingStoredFieldsIndexReader(indexStream, si);

                if (version >= CompressingTermVectorsWriter.VERSION_CHECKSUM)
                {
                    indexStream.ReadVInt64(); // the end of the data file
                    CodecUtil.CheckFooter(indexStream);
                }
                else
                {
#pragma warning disable 612, 618
                    CodecUtil.CheckEOF(indexStream);
#pragma warning restore 612, 618
                }
                indexStream.Dispose();
                indexStream = null;

                // Open the data file and read metadata
                string vectorsStreamFN = IndexFileNames.SegmentFileName(segment, segmentSuffix, CompressingTermVectorsWriter.VECTORS_EXTENSION);
                vectorsStream = d.OpenInput(vectorsStreamFN, context);
                string codecNameDat = formatName + CompressingTermVectorsWriter.CODEC_SFX_DAT;
                int version2 = CodecUtil.CheckHeader(vectorsStream, codecNameDat, CompressingTermVectorsWriter.VERSION_START, CompressingTermVectorsWriter.VERSION_CURRENT);
                if (version != version2)
                {
                    throw RuntimeException.Create("Version mismatch between stored fields index and data: " + version + " != " + version2);
                }
                if (Debugging.AssertsEnabled) Debugging.Assert(CodecUtil.HeaderLength(codecNameDat) == vectorsStream.Position); // LUCENENET specific: Renamed from getFilePointer() to match FileStream

                packedIntsVersion = vectorsStream.ReadVInt32();
                chunkSize = vectorsStream.ReadVInt32();
                decompressor = compressionMode.NewDecompressor();
                this.reader = new BlockPackedReaderIterator(vectorsStream, packedIntsVersion, CompressingTermVectorsWriter.BLOCK_SIZE, 0);

                success = true;
            }
            finally
            {
                if (!success)
                {
                    IOUtils.DisposeWhileHandlingException(this, indexStream);
                }
            }
        }

        internal CompressionMode CompressionMode => compressionMode;

        internal int ChunkSize => chunkSize;

        /// <summary>
        /// NOTE: This was getPackedIntsVersion() in Lucene
        /// </summary>
        internal int PackedInt32sVersion => packedIntsVersion;

        internal int Version => version;

        internal CompressingStoredFieldsIndexReader Index => indexReader;

        internal IndexInput VectorsStream => vectorsStream;

        /// <exception cref="ObjectDisposedException"> if this <see cref="TermVectorsReader"/> is disposed. </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void EnsureOpen()
        {
            if (closed)
            {
                throw AlreadyClosedException.Create(this.GetType().FullName, "this FieldsReader is disposed.");
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override void Dispose(bool disposing)
        {
            if (!closed)
            {
                IOUtils.Dispose(vectorsStream);
                closed = true;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override object Clone()
        {
            return new CompressingTermVectorsReader(this);
        }

        public override Fields Get(int doc)
        {
            EnsureOpen();

            // seek to the right place
            {
                long startPointer = indexReader.GetStartPointer(doc);
                vectorsStream.Seek(startPointer);
            }

            // decode
            // - docBase: first doc ID of the chunk
            // - chunkDocs: number of docs of the chunk
            int docBase = vectorsStream.ReadVInt32();
            int chunkDocs = vectorsStream.ReadVInt32();
            if (doc < docBase || doc >= docBase + chunkDocs || docBase + chunkDocs > numDocs)
            {
                throw new CorruptIndexException("docBase=" + docBase + ",chunkDocs=" + chunkDocs + ",doc=" + doc + " (resource=" + vectorsStream + ")");
            }

            int skip; // number of fields to skip
            int numFields; // number of fields of the document we're looking for
            int totalFields; // total number of fields of the chunk (sum for all docs)
            if (chunkDocs == 1)
            {
                skip = 0;
                numFields = totalFields = vectorsStream.ReadVInt32();
            }
            else
            {
                reader.Reset(vectorsStream, chunkDocs);
                int sum = 0;
                for (int i = docBase; i < doc; ++i)
                {
                    sum += (int)reader.Next();
                }
                skip = sum;
                numFields = (int)reader.Next();
                sum += numFields;
                for (int i = doc + 1; i < docBase + chunkDocs; ++i)
                {
                    sum += (int)reader.Next();
                }
                totalFields = sum;
            }

            if (numFields == 0)
            {
                // no vectors
                return null;
            }

            // read field numbers that have term vectors
            int[] fieldNums;
            {
                int token = vectorsStream.ReadByte() & 0xFF;
                if (Debugging.AssertsEnabled) Debugging.Assert(token != 0); // means no term vectors, cannot happen since we checked for numFields == 0
                int bitsPerFieldNum = token & 0x1F;
                int totalDistinctFields = token.TripleShift(5);
                if (totalDistinctFields == 0x07)
                {
                    totalDistinctFields += vectorsStream.ReadVInt32();
                }
                ++totalDistinctFields;
                PackedInt32s.IReaderIterator it = PackedInt32s.GetReaderIteratorNoHeader(vectorsStream, PackedInt32s.Format.PACKED, packedIntsVersion, totalDistinctFields, bitsPerFieldNum, 1);
                fieldNums = new int[totalDistinctFields];
                for (int i = 0; i < totalDistinctFields; ++i)
                {
                    fieldNums[i] = (int)it.Next();
                }
            }

            // read field numbers and flags
            int[] fieldNumOffs = new int[numFields];
            PackedInt32s.Reader flags;
            {
                int bitsPerOff = PackedInt32s.BitsRequired(fieldNums.Length - 1);
                PackedInt32s.Reader allFieldNumOffs = PackedInt32s.GetReaderNoHeader(vectorsStream, PackedInt32s.Format.PACKED, packedIntsVersion, totalFields, bitsPerOff);
                switch (vectorsStream.ReadVInt32())
                {
                    case 0:
                        PackedInt32s.Reader fieldFlags = PackedInt32s.GetReaderNoHeader(vectorsStream, PackedInt32s.Format.PACKED, packedIntsVersion, fieldNums.Length, CompressingTermVectorsWriter.FLAGS_BITS);
                        PackedInt32s.Mutable f = PackedInt32s.GetMutable(totalFields, CompressingTermVectorsWriter.FLAGS_BITS, PackedInt32s.COMPACT);
                        for (int i = 0; i < totalFields; ++i)
                        {
                            int fieldNumOff = (int)allFieldNumOffs.Get(i);
                            if (Debugging.AssertsEnabled) Debugging.Assert(fieldNumOff >= 0 && fieldNumOff < fieldNums.Length);
                            int fgs = (int)fieldFlags.Get(fieldNumOff);
                            f.Set(i, fgs);
                        }
                        flags = f;
                        break;

                    case 1:
                        flags = PackedInt32s.GetReaderNoHeader(vectorsStream, PackedInt32s.Format.PACKED, packedIntsVersion, totalFields, CompressingTermVectorsWriter.FLAGS_BITS);
                        break;

                    default:
                        throw AssertionError.Create();
                }
                for (int i = 0; i < numFields; ++i)
                {
                    fieldNumOffs[i] = (int)allFieldNumOffs.Get(skip + i);
                }
            }

            // number of terms per field for all fields
            PackedInt32s.Reader numTerms;
            int totalTerms;
            {
                int bitsRequired = vectorsStream.ReadVInt32();
                numTerms = PackedInt32s.GetReaderNoHeader(vectorsStream, PackedInt32s.Format.PACKED, packedIntsVersion, totalFields, bitsRequired);
                int sum = 0;
                for (int i = 0; i < totalFields; ++i)
                {
                    sum += (int)numTerms.Get(i);
                }
                totalTerms = sum;
            }

            // term lengths
            int docOff = 0, docLen = 0, totalLen;
            int[] fieldLengths = new int[numFields];
            int[][] prefixLengths = new int[numFields][];
            int[][] suffixLengths = new int[numFields][];
            {
                reader.Reset(vectorsStream, totalTerms);
                // skip
                int toSkip = 0;
                for (int i = 0; i < skip; ++i)
                {
                    toSkip += (int)numTerms.Get(i);
                }
                reader.Skip(toSkip);
                // read prefix lengths
                for (int i = 0; i < numFields; ++i)
                {
                    int termCount = (int)numTerms.Get(skip + i);
                    int[] fieldPrefixLengths = new int[termCount];
                    prefixLengths[i] = fieldPrefixLengths;
                    for (int j = 0; j < termCount; )
                    {
                        Int64sRef next = reader.Next(termCount - j);
                        for (int k = 0; k < next.Length; ++k)
                        {
                            fieldPrefixLengths[j++] = (int)next.Int64s[next.Offset + k];
                        }
                    }
                }
                reader.Skip(totalTerms - reader.Ord);

                reader.Reset(vectorsStream, totalTerms);
                // skip
                //toSkip = 0; // LUCENENET: IDE0059: Remove unnecessary value assignment
                for (int i = 0; i < skip; ++i)
                {
                    for (int j = 0; j < numTerms.Get(i); ++j)
                    {
                        docOff += (int)reader.Next();
                    }
                }
                for (int i = 0; i < numFields; ++i)
                {
                    int termCount = (int)numTerms.Get(skip + i);
                    int[] fieldSuffixLengths = new int[termCount];
                    suffixLengths[i] = fieldSuffixLengths;
                    for (int j = 0; j < termCount; )
                    {
                        Int64sRef next = reader.Next(termCount - j);
                        for (int k = 0; k < next.Length; ++k)
                        {
                            fieldSuffixLengths[j++] = (int)next.Int64s[next.Offset + k];
                        }
                    }
                    fieldLengths[i] = Sum(suffixLengths[i]);
                    docLen += fieldLengths[i];
                }
                totalLen = docOff + docLen;
                for (int i = skip + numFields; i < totalFields; ++i)
                {
                    for (int j = 0; j < numTerms.Get(i); ++j)
                    {
                        totalLen += (int)reader.Next();
                    }
                }
            }

            // term freqs
            int[] termFreqs = new int[totalTerms];
            {
                reader.Reset(vectorsStream, totalTerms);
                for (int i = 0; i < totalTerms; )
                {
                    Int64sRef next = reader.Next(totalTerms - i);
                    for (int k = 0; k < next.Length; ++k)
                    {
                        termFreqs[i++] = 1 + (int)next.Int64s[next.Offset + k];
                    }
                }
            }

            // total number of positions, offsets and payloads
            int totalPositions = 0, totalOffsets = 0, totalPayloads = 0;
            for (int i = 0, termIndex = 0; i < totalFields; ++i)
            {
                int f = (int)flags.Get(i);
                int termCount = (int)numTerms.Get(i);
                for (int j = 0; j < termCount; ++j)
                {
                    int freq = termFreqs[termIndex++];
                    if ((f & CompressingTermVectorsWriter.POSITIONS) != 0)
                    {
                        totalPositions += freq;
                    }
                    if ((f & CompressingTermVectorsWriter.OFFSETS) != 0)
                    {
                        totalOffsets += freq;
                    }
                    if ((f & CompressingTermVectorsWriter.PAYLOADS) != 0)
                    {
                        totalPayloads += freq;
                    }
                }
                if (Debugging.AssertsEnabled) Debugging.Assert(i != totalFields - 1 || termIndex == totalTerms, "{0} {1}", termIndex, totalTerms);
            }

            int[][] positionIndex = PositionIndex(skip, numFields, numTerms, termFreqs);
            int[][] positions, startOffsets, lengths;
            if (totalPositions > 0)
            {
                positions = ReadPositions(skip, numFields, flags, numTerms, termFreqs, CompressingTermVectorsWriter.POSITIONS, totalPositions, positionIndex);
            }
            else
            {
                positions = new int[numFields][];
            }

            if (totalOffsets > 0)
            {
                // average number of chars per term
                float[] charsPerTerm = new float[fieldNums.Length];
                for (int i = 0; i < charsPerTerm.Length; ++i)
                {
                    charsPerTerm[i] = J2N.BitConversion.Int32BitsToSingle(vectorsStream.ReadInt32());
                }
                startOffsets = ReadPositions(skip, numFields, flags, numTerms, termFreqs, CompressingTermVectorsWriter.OFFSETS, totalOffsets, positionIndex);
                lengths = ReadPositions(skip, numFields, flags, numTerms, termFreqs, CompressingTermVectorsWriter.OFFSETS, totalOffsets, positionIndex);

                for (int i = 0; i < numFields; ++i)
                {
                    int[] fStartOffsets = startOffsets[i];
                    int[] fPositions = positions[i];
                    // patch offsets from positions
                    if (fStartOffsets != null && fPositions != null)
                    {
                        float fieldCharsPerTerm = charsPerTerm[fieldNumOffs[i]];
                        for (int j = 0; j < startOffsets[i].Length; ++j)
                        {
                            fStartOffsets[j] += (int)(fieldCharsPerTerm * fPositions[j]);
                        }
                    }
                    if (fStartOffsets != null)
                    {
                        int[] fPrefixLengths = prefixLengths[i];
                        int[] fSuffixLengths = suffixLengths[i];
                        int[] fLengths = lengths[i];
                        for (int j = 0, end = (int)numTerms.Get(skip + i); j < end; ++j)
                        {
                            // delta-decode start offsets and  patch lengths using term lengths
                            int termLength = fPrefixLengths[j] + fSuffixLengths[j];
                            lengths[i][positionIndex[i][j]] += termLength;
                            for (int k = positionIndex[i][j] + 1; k < positionIndex[i][j + 1]; ++k)
                            {
                                fStartOffsets[k] += fStartOffsets[k - 1];
                                fLengths[k] += termLength;
                            }
                        }
                    }
                }
            }
            else
            {
                startOffsets = lengths = new int[numFields][];
            }
            if (totalPositions > 0)
            {
                // delta-decode positions
                for (int i = 0; i < numFields; ++i)
                {
                    int[] fPositions = positions[i];
                    int[] fpositionIndex = positionIndex[i];
                    if (fPositions != null)
                    {
                        for (int j = 0, end = (int)numTerms.Get(skip + i); j < end; ++j)
                        {
                            // delta-decode start offsets
                            for (int k = fpositionIndex[j] + 1; k < fpositionIndex[j + 1]; ++k)
                            {
                                fPositions[k] += fPositions[k - 1];
                            }
                        }
                    }
                }
            }

            // payload lengths
            int[][] payloadIndex = new int[numFields][];
            int totalPayloadLength = 0;
            int payloadOff = 0;
            int payloadLen = 0;
            if (totalPayloads > 0)
            {
                reader.Reset(vectorsStream, totalPayloads);
                // skip
                int termIndex = 0;
                for (int i = 0; i < skip; ++i)
                {
                    int f = (int)flags.Get(i);
                    int termCount = (int)numTerms.Get(i);
                    if ((f & CompressingTermVectorsWriter.PAYLOADS) != 0)
                    {
                        for (int j = 0; j < termCount; ++j)
                        {
                            int freq = termFreqs[termIndex + j];
                            for (int k = 0; k < freq; ++k)
                            {
                                int l = (int)reader.Next();
                                payloadOff += l;
                            }
                        }
                    }
                    termIndex += termCount;
                }
                totalPayloadLength = payloadOff;
                // read doc payload lengths
                for (int i = 0; i < numFields; ++i)
                {
                    int f = (int)flags.Get(skip + i);
                    int termCount = (int)numTerms.Get(skip + i);
                    if ((f & CompressingTermVectorsWriter.PAYLOADS) != 0)
                    {
                        int totalFreq = positionIndex[i][termCount];
                        payloadIndex[i] = new int[totalFreq + 1];
                        int posIdx = 0;
                        payloadIndex[i][posIdx] = payloadLen;
                        for (int j = 0; j < termCount; ++j)
                        {
                            int freq = termFreqs[termIndex + j];
                            for (int k = 0; k < freq; ++k)
                            {
                                int payloadLength = (int)reader.Next();
                                payloadLen += payloadLength;
                                payloadIndex[i][posIdx + 1] = payloadLen;
                                ++posIdx;
                            }
                        }
                        if (Debugging.AssertsEnabled) Debugging.Assert(posIdx == totalFreq);
                    }
                    termIndex += termCount;
                }
                totalPayloadLength += payloadLen;
                for (int i = skip + numFields; i < totalFields; ++i)
                {
                    int f = (int)flags.Get(i);
                    int termCount = (int)numTerms.Get(i);
                    if ((f & CompressingTermVectorsWriter.PAYLOADS) != 0)
                    {
                        for (int j = 0; j < termCount; ++j)
                        {
                            int freq = termFreqs[termIndex + j];
                            for (int k = 0; k < freq; ++k)
                            {
                                totalPayloadLength += (int)reader.Next();
                            }
                        }
                    }
                    termIndex += termCount;
                }
                if (Debugging.AssertsEnabled) Debugging.Assert(termIndex == totalTerms, "{0} {1}", termIndex, totalTerms);
            }

            // decompress data
            BytesRef suffixBytes = new BytesRef();
            decompressor.Decompress(vectorsStream, totalLen + totalPayloadLength, docOff + payloadOff, docLen + payloadLen, suffixBytes);
            suffixBytes.Length = docLen;
            BytesRef payloadBytes = new BytesRef(suffixBytes.Bytes, suffixBytes.Offset + docLen, payloadLen);

            int[] FieldFlags = new int[numFields];
            for (int i = 0; i < numFields; ++i)
            {
                FieldFlags[i] = (int)flags.Get(skip + i);
            }

            int[] fieldNumTerms = new int[numFields];
            for (int i = 0; i < numFields; ++i)
            {
                fieldNumTerms[i] = (int)numTerms.Get(skip + i);
            }

            int[][] fieldTermFreqs = new int[numFields][];
            {
                int termIdx = 0;
                for (int i = 0; i < skip; ++i)
                {
                    termIdx += (int)numTerms.Get(i);
                }
                for (int i = 0; i < numFields; ++i)
                {
                    int termCount = (int)numTerms.Get(skip + i);
                    fieldTermFreqs[i] = new int[termCount];
                    for (int j = 0; j < termCount; ++j)
                    {
                        fieldTermFreqs[i][j] = termFreqs[termIdx++];
                    }
                }
            }

            if (Debugging.AssertsEnabled) Debugging.Assert(Sum(fieldLengths) == docLen, "{0} != {1}", Sum(fieldLengths), docLen);

            return new TVFields(this, fieldNums, FieldFlags, fieldNumOffs, fieldNumTerms, fieldLengths, prefixLengths, suffixLengths, fieldTermFreqs, positionIndex, positions, startOffsets, lengths, payloadBytes, payloadIndex, suffixBytes);
        }

        // field -> term index -> position index
        private static int[][] PositionIndex(int skip, int numFields, PackedInt32s.Reader numTerms, int[] termFreqs) // LUCENENET: CA1822: Mark members as static
        {
            int[][] positionIndex = new int[numFields][];
            int termIndex = 0;
            for (int i = 0; i < skip; ++i)
            {
                int termCount = (int)numTerms.Get(i);
                termIndex += termCount;
            }
            for (int i = 0; i < numFields; ++i)
            {
                int termCount = (int)numTerms.Get(skip + i);
                positionIndex[i] = new int[termCount + 1];
                for (int j = 0; j < termCount; ++j)
                {
                    int freq = termFreqs[termIndex + j];
                    positionIndex[i][j + 1] = positionIndex[i][j] + freq;
                }
                termIndex += termCount;
            }
            return positionIndex;
        }

        private int[][] ReadPositions(int skip, int numFields, PackedInt32s.Reader flags, PackedInt32s.Reader numTerms, int[] termFreqs, int flag, int totalPositions, int[][] positionIndex)
        {
            int[][] positions = new int[numFields][];
            reader.Reset(vectorsStream, totalPositions);
            // skip
            int toSkip = 0;
            int termIndex = 0;
            for (int i = 0; i < skip; ++i)
            {
                int f = (int)flags.Get(i);
                int termCount = (int)numTerms.Get(i);
                if ((f & flag) != 0)
                {
                    for (int j = 0; j < termCount; ++j)
                    {
                        int freq = termFreqs[termIndex + j];
                        toSkip += freq;
                    }
                }
                termIndex += termCount;
            }
            reader.Skip(toSkip);
            // read doc positions
            for (int i = 0; i < numFields; ++i)
            {
                int f = (int)flags.Get(skip + i);
                int termCount = (int)numTerms.Get(skip + i);
                if ((f & flag) != 0)
                {
                    int totalFreq = positionIndex[i][termCount];
                    int[] fieldPositions = new int[totalFreq];
                    positions[i] = fieldPositions;
                    for (int j = 0; j < totalFreq; )
                    {
                        Int64sRef nextPositions = reader.Next(totalFreq - j);
                        for (int k = 0; k < nextPositions.Length; ++k)
                        {
                            fieldPositions[j++] = (int)nextPositions.Int64s[nextPositions.Offset + k];
                        }
                    }
                }
                termIndex += termCount;
            }
            reader.Skip(totalPositions - reader.Ord);
            return positions;
        }

        private class TVFields : Fields
        {
            private readonly CompressingTermVectorsReader outerInstance;

            internal readonly int[] fieldNums, fieldFlags, fieldNumOffs, numTerms, fieldLengths;
            internal readonly int[][] prefixLengths, suffixLengths, termFreqs, positionIndex, positions, startOffsets, lengths, payloadIndex;
            internal readonly BytesRef suffixBytes, payloadBytes;

            public TVFields(CompressingTermVectorsReader outerInstance, int[] fieldNums, int[] fieldFlags, int[] fieldNumOffs, int[] numTerms, int[] fieldLengths, int[][] prefixLengths, int[][] suffixLengths, int[][] termFreqs, int[][] positionIndex, int[][] positions, int[][] startOffsets, int[][] lengths, BytesRef payloadBytes, int[][] payloadIndex, BytesRef suffixBytes)
            {
                this.outerInstance = outerInstance;
                this.fieldNums = fieldNums;
                this.fieldFlags = fieldFlags;
                this.fieldNumOffs = fieldNumOffs;
                this.numTerms = numTerms;
                this.fieldLengths = fieldLengths;
                this.prefixLengths = prefixLengths;
                this.suffixLengths = suffixLengths;
                this.termFreqs = termFreqs;
                this.positionIndex = positionIndex;
                this.positions = positions;
                this.startOffsets = startOffsets;
                this.lengths = lengths;
                this.payloadBytes = payloadBytes;
                this.payloadIndex = payloadIndex;
                this.suffixBytes = suffixBytes;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public override IEnumerator<string> GetEnumerator()
            {
                return GetFieldInfoNameEnumerable().GetEnumerator();
            }

            private IEnumerable<string> GetFieldInfoNameEnumerable()
            {
                int i = 0;

                while (i < fieldNumOffs.Length)
                {
                    int fieldNum = fieldNums[fieldNumOffs[i++]];
                    yield return outerInstance.fieldInfos.FieldInfo(fieldNum).Name;
                }
            }

            public override Terms GetTerms(string field)
            {
                Index.FieldInfo fieldInfo = outerInstance.fieldInfos.FieldInfo(field);
                if (fieldInfo is null)
                {
                    return null;
                }
                int idx = -1;
                for (int i = 0; i < fieldNumOffs.Length; ++i)
                {
                    if (fieldNums[fieldNumOffs[i]] == fieldInfo.Number)
                    {
                        idx = i;
                        break;
                    }
                }

                if (idx == -1 || numTerms[idx] == 0)
                {
                    // no term
                    return null;
                }
                int fieldOff = 0, fieldLen = -1;
                for (int i = 0; i < fieldNumOffs.Length; ++i)
                {
                    if (i < idx)
                    {
                        fieldOff += fieldLengths[i];
                    }
                    else
                    {
                        fieldLen = fieldLengths[i];
                        break;
                    }
                }
                if (Debugging.AssertsEnabled) Debugging.Assert(fieldLen >= 0);
                return new TVTerms(numTerms[idx], fieldFlags[idx], prefixLengths[idx], suffixLengths[idx], termFreqs[idx], positionIndex[idx], positions[idx], startOffsets[idx], lengths[idx], payloadIndex[idx], payloadBytes, new BytesRef(suffixBytes.Bytes, suffixBytes.Offset + fieldOff, fieldLen));
            }

            public override int Count => fieldNumOffs.Length;
        }

        private class TVTerms : Terms
        {
            private readonly int numTerms, flags;
            private readonly int[] prefixLengths, suffixLengths, termFreqs, positionIndex, positions, startOffsets, lengths, payloadIndex;
            private readonly BytesRef termBytes, payloadBytes;

            internal TVTerms(int numTerms, int flags, int[] prefixLengths, int[] suffixLengths, int[] termFreqs, int[] positionIndex, int[] positions, int[] startOffsets, int[] lengths, int[] payloadIndex, BytesRef payloadBytes, BytesRef termBytes)
            {
                this.numTerms = numTerms;
                this.flags = flags;
                this.prefixLengths = prefixLengths;
                this.suffixLengths = suffixLengths;
                this.termFreqs = termFreqs;
                this.positionIndex = positionIndex;
                this.positions = positions;
                this.startOffsets = startOffsets;
                this.lengths = lengths;
                this.payloadIndex = payloadIndex;
                this.payloadBytes = payloadBytes;
                this.termBytes = termBytes;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public override TermsEnum GetEnumerator()
            {
                var termsEnum = new TVTermsEnum();
                termsEnum.Reset(numTerms, flags, prefixLengths, suffixLengths, termFreqs, positionIndex, positions, startOffsets, lengths, payloadIndex, payloadBytes, new ByteArrayDataInput(termBytes.Bytes, termBytes.Offset, termBytes.Length));
                return termsEnum;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public override TermsEnum GetEnumerator(TermsEnum reuse)
            {
                if (reuse is null || !(reuse is TVTermsEnum termsEnum))
                    termsEnum = new TVTermsEnum();

                termsEnum.Reset(numTerms, flags, prefixLengths, suffixLengths, termFreqs, positionIndex, positions, startOffsets, lengths, payloadIndex, payloadBytes, new ByteArrayDataInput(termBytes.Bytes, termBytes.Offset, termBytes.Length));
                return termsEnum;
            }

            public override IComparer<BytesRef> Comparer => BytesRef.UTF8SortedAsUnicodeComparer;

            public override long Count => numTerms;

            public override long SumTotalTermFreq => -1L;

            public override long SumDocFreq => numTerms;

            public override int DocCount => 1;

            public override bool HasFreqs => true;

            public override bool HasOffsets => (flags & CompressingTermVectorsWriter.OFFSETS) != 0;

            public override bool HasPositions => (flags & CompressingTermVectorsWriter.POSITIONS) != 0;

            public override bool HasPayloads => (flags & CompressingTermVectorsWriter.PAYLOADS) != 0;
        }

        private class TVTermsEnum : TermsEnum
        {
            private int numTerms, startPos, ord;
            private int[] prefixLengths, suffixLengths, termFreqs, positionIndex, positions, startOffsets, lengths, payloadIndex;
            private ByteArrayDataInput @in;
            private BytesRef payloads;
            private readonly BytesRef term;

            internal TVTermsEnum()
            {
                term = new BytesRef(16);
            }

            internal virtual void Reset(int numTerms, int flags, int[] prefixLengths, int[] suffixLengths, int[] termFreqs, int[] positionIndex, int[] positions, int[] startOffsets, int[] lengths, int[] payloadIndex, BytesRef payloads, ByteArrayDataInput @in)
            {
                this.numTerms = numTerms;
                this.prefixLengths = prefixLengths;
                this.suffixLengths = suffixLengths;
                this.termFreqs = termFreqs;
                this.positionIndex = positionIndex;
                this.positions = positions;
                this.startOffsets = startOffsets;
                this.lengths = lengths;
                this.payloadIndex = payloadIndex;
                this.payloads = payloads;
                this.@in = @in;
                startPos = @in.Position;
                Reset();
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal virtual void Reset()
            {
                term.Length = 0;
                @in.Position = startPos;
                ord = -1;
            }

            public override bool MoveNext()
            {
                if (ord == numTerms - 1)
                {
                    return false;
                }
                else
                {
                    if (Debugging.AssertsEnabled) Debugging.Assert(ord < numTerms);
                    ++ord;
                }

                // read term
                term.Offset = 0;
                term.Length = prefixLengths[ord] + suffixLengths[ord];
                if (term.Length > term.Bytes.Length)
                {
                    term.Bytes = ArrayUtil.Grow(term.Bytes, term.Length);
                }
                @in.ReadBytes(term.Bytes, prefixLengths[ord], suffixLengths[ord]);

                return term != null;
            }

            [Obsolete("Use MoveNext() and Term instead. This method will be removed in 4.8.0 release candidate."), System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
            public override BytesRef Next()
            {
                if (MoveNext())
                    return term;
                return null;
            }

            public override IComparer<BytesRef> Comparer => BytesRef.UTF8SortedAsUnicodeComparer;

            public override TermsEnum.SeekStatus SeekCeil(BytesRef text)
            {
                if (ord < numTerms && ord >= 0)
                {
                    int cmp = Term.CompareTo(text);
                    if (cmp == 0)
                    {
                        return TermsEnum.SeekStatus.FOUND;
                    }
                    else if (cmp > 0)
                    {
                        Reset();
                    }
                }
                // linear scan
                while (MoveNext())
                {
                    int cmp = term.CompareTo(text);
                    if (cmp > 0)
                    {
                        return TermsEnum.SeekStatus.NOT_FOUND;
                    }
                    else if (cmp == 0)
                    {
                        return TermsEnum.SeekStatus.FOUND;
                    }
                }
                return TermsEnum.SeekStatus.END;
            }

            public override void SeekExact(long ord)
            {
                throw UnsupportedOperationException.Create();
            }

            public override BytesRef Term => term;

            public override long Ord => throw UnsupportedOperationException.Create();

            public override int DocFreq => 1;

            public override long TotalTermFreq => termFreqs[ord];

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public override sealed DocsEnum Docs(IBits liveDocs, DocsEnum reuse, DocsFlags flags)
            {
                if (reuse is null || !(reuse is TVDocsEnum docsEnum))
                    docsEnum = new TVDocsEnum();

                docsEnum.Reset(liveDocs, termFreqs[ord], positionIndex[ord], positions, startOffsets, lengths, payloads, payloadIndex);
                return docsEnum;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public override DocsAndPositionsEnum DocsAndPositions(IBits liveDocs, DocsAndPositionsEnum reuse, DocsAndPositionsFlags flags)
            {
                if (positions is null && startOffsets is null)
                {
                    return null;
                }
                // TODO: slightly sheisty
                return (DocsAndPositionsEnum)Docs(liveDocs, reuse, (DocsFlags)flags);
            }
        }

        private class TVDocsEnum : DocsAndPositionsEnum
        {
            private IBits liveDocs;
            private int doc = -1;
            private int termFreq;
            private int positionIndex;
            private int[] positions;
            private int[] startOffsets;
            private int[] lengths;
            private readonly BytesRef payload;
            private int[] payloadIndex;
            private int basePayloadOffset;
            private int i;

            internal TVDocsEnum()
            {
                payload = new BytesRef();
            }

            public virtual void Reset(IBits liveDocs, int freq, int positionIndex, int[] positions, int[] startOffsets, int[] lengths, BytesRef payloads, int[] payloadIndex)
            {
                this.liveDocs = liveDocs;
                this.termFreq = freq;
                this.positionIndex = positionIndex;
                this.positions = positions;
                this.startOffsets = startOffsets;
                this.lengths = lengths;
                this.basePayloadOffset = payloads.Offset;
                this.payload.Bytes = payloads.Bytes;
                payload.Offset = payload.Length = 0;
                this.payloadIndex = payloadIndex;

                doc = i = -1;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private void CheckDoc()
            {
                if (doc == NO_MORE_DOCS)
                {
                    throw IllegalStateException.Create("DocsEnum exhausted");
                }
                else if (doc == -1)
                {
                    throw IllegalStateException.Create("DocsEnum not started");
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private void CheckPosition()
            {
                CheckDoc();
                if (i < 0)
                {
                    throw IllegalStateException.Create("Position enum not started");
                }
                else if (i >= termFreq)
                {
                    throw IllegalStateException.Create("Read past last position");
                }
            }

            public override int NextPosition()
            {
                if (doc != 0)
                {
                    throw IllegalStateException.Create();
                }
                else if (i >= termFreq - 1)
                {
                    throw IllegalStateException.Create("Read past last position");
                }

                ++i;

                if (payloadIndex != null)
                {
                    payload.Offset = basePayloadOffset + payloadIndex[positionIndex + i];
                    payload.Length = payloadIndex[positionIndex + i + 1] - payloadIndex[positionIndex + i];
                }

                if (positions is null)
                {
                    return -1;
                }
                else
                {
                    return positions[positionIndex + i];
                }
            }

            public override int StartOffset
            {
                get
                {
                    CheckPosition();
                    if (startOffsets is null)
                    {
                        return -1;
                    }
                    else
                    {
                        return startOffsets[positionIndex + i];
                    }
                }
            }

            public override int EndOffset
            {
                get
                {
                    CheckPosition();
                    if (startOffsets is null)
                    {
                        return -1;
                    }
                    else
                    {
                        return startOffsets[positionIndex + i] + lengths[positionIndex + i];
                    }
                }
            }

            public override BytesRef GetPayload()
            {
                CheckPosition();
                if (payloadIndex is null || payload.Length == 0)
                {
                    return null;
                }
                else
                {
                    return payload;
                }
            }

            public override int Freq
            {
                get
                {
                    CheckDoc();
                    return termFreq;
                }
            }

            public override int DocID => doc;

            public override int NextDoc()
            {
                if (doc == -1 && (liveDocs is null || liveDocs.Get(0)))
                {
                    return (doc = 0);
                }
                else
                {
                    return (doc = NO_MORE_DOCS);
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public override int Advance(int target)
            {
                return SlowAdvance(target);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public override long GetCost()
            {
                return 1;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int Sum(int[] arr)
        {
            int sum = 0;
            for (int i = 0; i < arr.Length; i++)
                sum += arr[i];
            return sum;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override long RamBytesUsed()
        {
            return indexReader.RamBytesUsed();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override void CheckIntegrity()
        {
            if (version >= CompressingTermVectorsWriter.VERSION_CHECKSUM)
            {
                CodecUtil.ChecksumEntireFile(vectorsStream);
            }
        }
    }
}