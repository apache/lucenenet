using System.Linq;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Support;
using Lucene.Net.Util;
using Lucene.Net.Util.Packed;
using System;
using System.Collections.Generic;
using System.Diagnostics;

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
    /// <seealso cref="TermVectorsReader"/> for <seealso cref="CompressingTermVectorsFormat"/>.
    /// @lucene.experimental
    /// </summary>
    public sealed class CompressingTermVectorsReader : TermVectorsReader, IDisposable
    {
        private readonly FieldInfos fieldInfos;
        internal readonly CompressingStoredFieldsIndexReader indexReader;
        internal readonly IndexInput vectorsStream;
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
                Debug.Assert(CodecUtil.HeaderLength(codecNameIdx) == indexStream.FilePointer);
                indexReader = new CompressingStoredFieldsIndexReader(indexStream, si);

                if (version >= CompressingTermVectorsWriter.VERSION_CHECKSUM)
                {
                    indexStream.ReadVLong(); // the end of the data file
                    CodecUtil.CheckFooter(indexStream);
                }
                else
                {
                    CodecUtil.CheckEOF(indexStream);
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
                    throw new Exception("Version mismatch between stored fields index and data: " + version + " != " + version2);
                }
                Debug.Assert(CodecUtil.HeaderLength(codecNameDat) == vectorsStream.FilePointer);

                packedIntsVersion = vectorsStream.ReadVInt();
                chunkSize = vectorsStream.ReadVInt();
                decompressor = compressionMode.NewDecompressor();
                this.reader = new BlockPackedReaderIterator(vectorsStream, packedIntsVersion, CompressingTermVectorsWriter.BLOCK_SIZE, 0);

                success = true;
            }
            finally
            {
                if (!success)
                {
                    IOUtils.CloseWhileHandlingException(this, indexStream);
                }
            }
        }

        internal CompressionMode CompressionMode
        {
            get
            {
                return compressionMode;
            }
        }

        internal int ChunkSize
        {
            get
            {
                return chunkSize;
            }
        }

        internal int PackedIntsVersion
        {
            get
            {
                return packedIntsVersion;
            }
        }

        internal int Version
        {
            get
            {
                return version;
            }
        }

        internal CompressingStoredFieldsIndexReader Index
        {
            get
            {
                return indexReader;
            }
        }

        internal IndexInput VectorsStream
        {
            get
            {
                return vectorsStream;
            }
        }

        /// <exception cref="AlreadyClosedException"> if this TermVectorsReader is closed </exception>
        private void EnsureOpen()
        {
            if (closed)
            {
                throw new Exception("this FieldsReader is closed");
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (!closed)
            {
                IOUtils.Close(vectorsStream);
                closed = true;
            }
        }

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
            int docBase = vectorsStream.ReadVInt();
            int chunkDocs = vectorsStream.ReadVInt();
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
                numFields = totalFields = vectorsStream.ReadVInt();
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
                Debug.Assert(token != 0); // means no term vectors, cannot happen since we checked for numFields == 0
                int bitsPerFieldNum = token & 0x1F;
                int totalDistinctFields = (int)((uint)token >> 5);
                if (totalDistinctFields == 0x07)
                {
                    totalDistinctFields += vectorsStream.ReadVInt();
                }
                ++totalDistinctFields;
                PackedInts.IReaderIterator it = PackedInts.GetReaderIteratorNoHeader(vectorsStream, PackedInts.Format.PACKED, packedIntsVersion, totalDistinctFields, bitsPerFieldNum, 1);
                fieldNums = new int[totalDistinctFields];
                for (int i = 0; i < totalDistinctFields; ++i)
                {
                    fieldNums[i] = (int)it.Next();
                }
            }

            // read field numbers and flags
            int[] fieldNumOffs = new int[numFields];
            PackedInts.Reader flags;
            {
                int bitsPerOff = PackedInts.BitsRequired(fieldNums.Length - 1);
                PackedInts.Reader allFieldNumOffs = PackedInts.GetReaderNoHeader(vectorsStream, PackedInts.Format.PACKED, packedIntsVersion, totalFields, bitsPerOff);
                switch (vectorsStream.ReadVInt())
                {
                    case 0:
                        PackedInts.Reader fieldFlags = PackedInts.GetReaderNoHeader(vectorsStream, PackedInts.Format.PACKED, packedIntsVersion, fieldNums.Length, CompressingTermVectorsWriter.FLAGS_BITS);
                        PackedInts.Mutable f = PackedInts.GetMutable(totalFields, CompressingTermVectorsWriter.FLAGS_BITS, PackedInts.COMPACT);
                        for (int i = 0; i < totalFields; ++i)
                        {
                            int fieldNumOff = (int)allFieldNumOffs.Get(i);
                            Debug.Assert(fieldNumOff >= 0 && fieldNumOff < fieldNums.Length);
                            int fgs = (int)fieldFlags.Get(fieldNumOff);
                            f.Set(i, fgs);
                        }
                        flags = f;
                        break;

                    case 1:
                        flags = PackedInts.GetReaderNoHeader(vectorsStream, PackedInts.Format.PACKED, packedIntsVersion, totalFields, CompressingTermVectorsWriter.FLAGS_BITS);
                        break;

                    default:
                        throw new Exception();
                }
                for (int i = 0; i < numFields; ++i)
                {
                    fieldNumOffs[i] = (int)allFieldNumOffs.Get(skip + i);
                }
            }

            // number of terms per field for all fields
            PackedInts.Reader numTerms;
            int totalTerms;
            {
                int bitsRequired = vectorsStream.ReadVInt();
                numTerms = PackedInts.GetReaderNoHeader(vectorsStream, PackedInts.Format.PACKED, packedIntsVersion, totalFields, bitsRequired);
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
                        LongsRef next = reader.Next(termCount - j);
                        for (int k = 0; k < next.Length; ++k)
                        {
                            fieldPrefixLengths[j++] = (int)next.Longs[next.Offset + k];
                        }
                    }
                }
                reader.Skip(totalTerms - reader.Ord);

                reader.Reset(vectorsStream, totalTerms);
                // skip
                toSkip = 0;
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
                        LongsRef next = reader.Next(termCount - j);
                        for (int k = 0; k < next.Length; ++k)
                        {
                            fieldSuffixLengths[j++] = (int)next.Longs[next.Offset + k];
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
                    LongsRef next = reader.Next(totalTerms - i);
                    for (int k = 0; k < next.Length; ++k)
                    {
                        termFreqs[i++] = 1 + (int)next.Longs[next.Offset + k];
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
                Debug.Assert(i != totalFields - 1 || termIndex == totalTerms, termIndex + " " + totalTerms);
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
                    charsPerTerm[i] = Number.IntBitsToFloat(vectorsStream.ReadInt());
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
                        Debug.Assert(posIdx == totalFreq);
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
                Debug.Assert(termIndex == totalTerms, termIndex + " " + totalTerms);
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

            Debug.Assert(Sum(fieldLengths) == docLen, Sum(fieldLengths) + " != " + docLen);

            return new TVFields(this, fieldNums, FieldFlags, fieldNumOffs, fieldNumTerms, fieldLengths, prefixLengths, suffixLengths, fieldTermFreqs, positionIndex, positions, startOffsets, lengths, payloadBytes, payloadIndex, suffixBytes);
        }

        // field -> term index -> position index
        private int[][] PositionIndex(int skip, int numFields, PackedInts.Reader numTerms, int[] termFreqs)
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

        private int[][] ReadPositions(int skip, int numFields, PackedInts.Reader flags, PackedInts.Reader numTerms, int[] termFreqs, int flag, int totalPositions, int[][] positionIndex)
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
                        LongsRef nextPositions = reader.Next(totalFreq - j);
                        for (int k = 0; k < nextPositions.Length; ++k)
                        {
                            fieldPositions[j++] = (int)nextPositions.Longs[nextPositions.Offset + k];
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

            public override Terms Terms(string field)
            {
                FieldInfo fieldInfo = outerInstance.fieldInfos.FieldInfo(field);
                if (fieldInfo == null)
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
                Debug.Assert(fieldLen >= 0);
                return new TVTerms(outerInstance, numTerms[idx], fieldFlags[idx], prefixLengths[idx], suffixLengths[idx], termFreqs[idx], positionIndex[idx], positions[idx], startOffsets[idx], lengths[idx], payloadIndex[idx], payloadBytes, new BytesRef(suffixBytes.Bytes, suffixBytes.Offset + fieldOff, fieldLen));
            }

            public override int Size
            {
                get { return fieldNumOffs.Length; }
            }
        }

        private class TVTerms : Terms
        {
            private readonly CompressingTermVectorsReader outerInstance;

            private readonly int numTerms, flags;
            private readonly int[] prefixLengths, suffixLengths, termFreqs, positionIndex, positions, startOffsets, lengths, payloadIndex;
            private readonly BytesRef termBytes, payloadBytes;

            internal TVTerms(CompressingTermVectorsReader outerInstance, int numTerms, int flags, int[] prefixLengths, int[] suffixLengths, int[] termFreqs, int[] positionIndex, int[] positions, int[] startOffsets, int[] lengths, int[] payloadIndex, BytesRef payloadBytes, BytesRef termBytes)
            {
                this.outerInstance = outerInstance;
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

            public override TermsEnum Iterator(TermsEnum reuse)
            {
                TVTermsEnum termsEnum;
                if (reuse != null && reuse is TVTermsEnum)
                {
                    termsEnum = (TVTermsEnum)reuse;
                }
                else
                {
                    termsEnum = new TVTermsEnum();
                }
                termsEnum.Reset(numTerms, flags, prefixLengths, suffixLengths, termFreqs, positionIndex, positions, startOffsets, lengths, payloadIndex, payloadBytes, new ByteArrayDataInput((byte[])(Array)termBytes.Bytes, termBytes.Offset, termBytes.Length));
                return termsEnum;
            }

            public override IComparer<BytesRef> Comparator 
            {
                get
                {
                    return BytesRef.UTF8SortedAsUnicodeComparer;
                }
            }

            public override long Size
            {
                get { return numTerms; }
            }

            public override long SumTotalTermFreq
            {
                get
                {
                    return -1L;
                }
            }

            public override long SumDocFreq
            {
                get
                {
                    return numTerms;
                }
            }

            public override int DocCount
            {
                get
                {
                    return 1;
                }
            }

            public override bool HasFreqs
            {
                get { return true; }
            }

            public override bool HasOffsets
            {
                get { return (flags & CompressingTermVectorsWriter.OFFSETS) != 0; }
            }

            public override bool HasPositions
            {
                get { return (flags & CompressingTermVectorsWriter.POSITIONS) != 0; }
            }

            public override bool HasPayloads
            {
                get { return (flags & CompressingTermVectorsWriter.PAYLOADS) != 0; }
            }
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

            internal virtual void Reset()
            {
                term.Length = 0;
                @in.Position = startPos;
                ord = -1;
            }

            public override BytesRef Next()
            {
                if (ord == numTerms - 1)
                {
                    return null;
                }
                else
                {
                    Debug.Assert(ord < numTerms);
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

                return term;
            }

            public override IComparer<BytesRef> Comparator
            {
                get
                {
                    return BytesRef.UTF8SortedAsUnicodeComparer;
                }
            }

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
                while (true)
                {
                    BytesRef term = Next();
                    if (term == null)
                    {
                        return TermsEnum.SeekStatus.END;
                    }
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
            }

            public override void SeekExact(long ord)
            {
                throw new System.NotSupportedException();
            }

            public override BytesRef Term
            {
                get { return term; }
            }

            public override long Ord
            {
                get { throw new System.NotSupportedException(); }
            }

            public override int DocFreq
            {
                get { return 1; }
            }

            public override long TotalTermFreq
            {
                get { return termFreqs[ord]; }
            }

            public override sealed DocsEnum Docs(IBits liveDocs, DocsEnum reuse, int flags)
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

                docsEnum.Reset(liveDocs, termFreqs[ord], positionIndex[ord], positions, startOffsets, lengths, payloads, payloadIndex);
                return docsEnum;
            }

            public override DocsAndPositionsEnum DocsAndPositions(IBits liveDocs, DocsAndPositionsEnum reuse, int flags)
            {
                if (positions == null && startOffsets == null)
                {
                    return null;
                }
                // TODO: slightly sheisty
                return (DocsAndPositionsEnum)Docs(liveDocs, reuse, flags);
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

            private void CheckDoc()
            {
                if (doc == NO_MORE_DOCS)
                {
                    throw new Exception("DocsEnum exhausted");
                }
                else if (doc == -1)
                {
                    throw new Exception("DocsEnum not started");
                }
            }

            private void CheckPosition()
            {
                CheckDoc();
                if (i < 0)
                {
                    throw new Exception("Position enum not started");
                }
                else if (i >= termFreq)
                {
                    throw new Exception("Read past last position");
                }
            }

            public override int NextPosition()
            {
                if (doc != 0)
                {
                    throw new Exception();
                }
                else if (i >= termFreq - 1)
                {
                    throw new Exception("Read past last position");
                }

                ++i;

                if (payloadIndex != null)
                {
                    payload.Offset = basePayloadOffset + payloadIndex[positionIndex + i];
                    payload.Length = payloadIndex[positionIndex + i + 1] - payloadIndex[positionIndex + i];
                }

                if (positions == null)
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
                    if (startOffsets == null)
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
                    if (startOffsets == null)
                    {
                        return -1;
                    }
                    else
                    {
                        return startOffsets[positionIndex + i] + lengths[positionIndex + i];
                    }
                }
            }

            public override BytesRef Payload
            {
                get
                {
                    CheckPosition();
                    if (payloadIndex == null || payload.Length == 0)
                    {
                        return null;
                    }
                    else
                    {
                        return payload;
                    }
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

            public override int DocID
            {
                get { return doc; }
            }

            public override int NextDoc()
            {
                if (doc == -1 && (liveDocs == null || liveDocs.Get(0)))
                {
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

            public override long Cost()
            {
                return 1;
            }
        }

        private static int Sum(int[] arr)
        {
            return arr.Sum();
        }

        public override long RamBytesUsed()
        {
            return indexReader.RamBytesUsed();
        }

        public override void CheckIntegrity()
        {
            if (version >= CompressingTermVectorsWriter.VERSION_CHECKSUM)
            {
                CodecUtil.ChecksumEntireFile(vectorsStream);
            }
        }
    }
}