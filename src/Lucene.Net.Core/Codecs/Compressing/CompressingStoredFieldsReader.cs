using Lucene.Net.Codecs.Lucene40;
using Lucene.Net.Support;
using System;
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

    using ArrayUtil = Lucene.Net.Util.ArrayUtil;
    using BufferedChecksumIndexInput = Lucene.Net.Store.BufferedChecksumIndexInput;
    using ByteArrayDataInput = Lucene.Net.Store.ByteArrayDataInput;
    using BytesRef = Lucene.Net.Util.BytesRef;
    using ChecksumIndexInput = Lucene.Net.Store.ChecksumIndexInput;
    using CorruptIndexException = Lucene.Net.Index.CorruptIndexException;
    using DataInput = Lucene.Net.Store.DataInput;
    using DataOutput = Lucene.Net.Store.DataOutput;
    using Directory = Lucene.Net.Store.Directory;
    using FieldInfo = Lucene.Net.Index.FieldInfo;
    using FieldInfos = Lucene.Net.Index.FieldInfos;
    using IndexFileNames = Lucene.Net.Index.IndexFileNames;
    using IndexInput = Lucene.Net.Store.IndexInput;
    using IOContext = Lucene.Net.Store.IOContext;
    using IOUtils = Lucene.Net.Util.IOUtils;
    using PackedInts = Lucene.Net.Util.Packed.PackedInts;
    using SegmentInfo = Lucene.Net.Index.SegmentInfo;
    using StoredFieldVisitor = Lucene.Net.Index.StoredFieldVisitor;

    /// <summary>
    /// <seealso cref="StoredFieldsReader"/> impl for <seealso cref="CompressingStoredFieldsFormat"/>.
    /// @lucene.experimental
    /// </summary>
    public sealed class CompressingStoredFieldsReader : StoredFieldsReader
    {
        // Do not reuse the decompression buffer when there is more than 32kb to decompress
        private static readonly int BUFFER_REUSE_THRESHOLD = 1 << 15;

        private readonly int Version_Renamed;
        private readonly FieldInfos FieldInfos;
        private readonly CompressingStoredFieldsIndexReader IndexReader;
        private readonly long MaxPointer;
        private readonly IndexInput FieldsStream;
        private readonly int ChunkSize_Renamed;
        private readonly int PackedIntsVersion;
        private readonly CompressionMode CompressionMode_Renamed;
        private readonly Decompressor Decompressor;
        private readonly BytesRef Bytes;
        private readonly int NumDocs;
        private bool Closed;

        // used by clone
        private CompressingStoredFieldsReader(CompressingStoredFieldsReader reader)
        {
            this.Version_Renamed = reader.Version_Renamed;
            this.FieldInfos = reader.FieldInfos;
            this.FieldsStream = (IndexInput)reader.FieldsStream.Clone();
            this.IndexReader = (CompressingStoredFieldsIndexReader)reader.IndexReader.Clone();
            this.MaxPointer = reader.MaxPointer;
            this.ChunkSize_Renamed = reader.ChunkSize_Renamed;
            this.PackedIntsVersion = reader.PackedIntsVersion;
            this.CompressionMode_Renamed = reader.CompressionMode_Renamed;
            this.Decompressor = (Decompressor)reader.Decompressor.Clone();
            this.NumDocs = reader.NumDocs;
            this.Bytes = new BytesRef(reader.Bytes.Bytes.Length);
            this.Closed = false;
        }

        /// <summary>
        /// Sole constructor. </summary>
        public CompressingStoredFieldsReader(Directory d, SegmentInfo si, string segmentSuffix, FieldInfos fn, IOContext context, string formatName, CompressionMode compressionMode)
        {
            this.CompressionMode_Renamed = compressionMode;
            string segment = si.Name;
            bool success = false;
            FieldInfos = fn;
            NumDocs = si.DocCount;
            ChecksumIndexInput indexStream = null;
            try
            {
                string indexStreamFN = IndexFileNames.SegmentFileName(segment, segmentSuffix, Lucene40StoredFieldsWriter.FIELDS_INDEX_EXTENSION);
                string fieldsStreamFN = IndexFileNames.SegmentFileName(segment, segmentSuffix, Lucene40StoredFieldsWriter.FIELDS_EXTENSION);
                // Load the index into memory
                indexStream = d.OpenChecksumInput(indexStreamFN, context);
                string codecNameIdx = formatName + CompressingStoredFieldsWriter.CODEC_SFX_IDX;
                Version_Renamed = CodecUtil.CheckHeader(indexStream, codecNameIdx, CompressingStoredFieldsWriter.VERSION_START, CompressingStoredFieldsWriter.VERSION_CURRENT);
                Debug.Assert(CodecUtil.HeaderLength(codecNameIdx) == indexStream.FilePointer);
                IndexReader = new CompressingStoredFieldsIndexReader(indexStream, si);

                long maxPointer = -1;

                if (Version_Renamed >= CompressingStoredFieldsWriter.VERSION_CHECKSUM)
                {
                    maxPointer = indexStream.ReadVLong();
                    CodecUtil.CheckFooter(indexStream);
                }
                else
                {
                    CodecUtil.CheckEOF(indexStream);
                }
                indexStream.Dispose();
                indexStream = null;

                // Open the data file and read metadata
                FieldsStream = d.OpenInput(fieldsStreamFN, context);
                if (Version_Renamed >= CompressingStoredFieldsWriter.VERSION_CHECKSUM)
                {
                    if (maxPointer + CodecUtil.FooterLength() != FieldsStream.Length())
                    {
                        throw new CorruptIndexException("Invalid fieldsStream maxPointer (file truncated?): maxPointer=" + maxPointer + ", length=" + FieldsStream.Length());
                    }
                }
                else
                {
                    maxPointer = FieldsStream.Length();
                }
                this.MaxPointer = maxPointer;
                string codecNameDat = formatName + CompressingStoredFieldsWriter.CODEC_SFX_DAT;
                int fieldsVersion = CodecUtil.CheckHeader(FieldsStream, codecNameDat, CompressingStoredFieldsWriter.VERSION_START, CompressingStoredFieldsWriter.VERSION_CURRENT);
                if (Version_Renamed != fieldsVersion)
                {
                    throw new CorruptIndexException("Version mismatch between stored fields index and data: " + Version_Renamed + " != " + fieldsVersion);
                }
                Debug.Assert(CodecUtil.HeaderLength(codecNameDat) == FieldsStream.FilePointer);

                if (Version_Renamed >= CompressingStoredFieldsWriter.VERSION_BIG_CHUNKS)
                {
                    ChunkSize_Renamed = FieldsStream.ReadVInt();
                }
                else
                {
                    ChunkSize_Renamed = -1;
                }
                PackedIntsVersion = FieldsStream.ReadVInt();
                Decompressor = compressionMode.NewDecompressor();
                this.Bytes = new BytesRef();

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

        /// <exception cref="AlreadyClosedException"> if this FieldsReader is closed </exception>
        private void EnsureOpen()
        {
            if (Closed)
            {
                throw new Exception("this FieldsReader is closed");
            }
        }

        /// <summary>
        /// Close the underlying <seealso cref="IndexInput"/>s.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (!Closed)
            {
                IOUtils.Close(FieldsStream);
                Closed = true;
            }
        }

        private static void ReadField(DataInput @in, StoredFieldVisitor visitor, FieldInfo info, int bits)
        {
            switch (bits & CompressingStoredFieldsWriter.TYPE_MASK)
            {
                case CompressingStoredFieldsWriter.BYTE_ARR:
                    int length = @in.ReadVInt();
                    var data = new byte[length];
                    @in.ReadBytes(data, 0, length);
                    visitor.BinaryField(info, data);
                    break;

                case CompressingStoredFieldsWriter.STRING:
                    length = @in.ReadVInt();
                    data = new byte[length];
                    @in.ReadBytes(data, 0, length);
                    visitor.StringField(info, IOUtils.CHARSET_UTF_8.GetString((byte[])(Array)data));
                    break;

                case CompressingStoredFieldsWriter.NUMERIC_INT:
                    visitor.Int32Field(info, @in.ReadInt());
                    break;

                case CompressingStoredFieldsWriter.NUMERIC_FLOAT:
                    visitor.SingleField(info, Number.IntBitsToFloat(@in.ReadInt()));
                    break;

                case CompressingStoredFieldsWriter.NUMERIC_LONG:
                    visitor.Int64Field(info, @in.ReadLong());
                    break;

                case CompressingStoredFieldsWriter.NUMERIC_DOUBLE:
                    visitor.DoubleField(info, BitConverter.Int64BitsToDouble(@in.ReadLong()));
                    break;

                default:
                    throw new InvalidOperationException("Unknown type flag: " + bits.ToString("x"));
            }
        }

        private static void SkipField(DataInput @in, int bits)
        {
            switch (bits & CompressingStoredFieldsWriter.TYPE_MASK)
            {
                case CompressingStoredFieldsWriter.BYTE_ARR:
                case CompressingStoredFieldsWriter.STRING:
                    int length = @in.ReadVInt();
                    @in.SkipBytes(length);
                    break;

                case CompressingStoredFieldsWriter.NUMERIC_INT:
                case CompressingStoredFieldsWriter.NUMERIC_FLOAT:
                    @in.ReadInt();
                    break;

                case CompressingStoredFieldsWriter.NUMERIC_LONG:
                case CompressingStoredFieldsWriter.NUMERIC_DOUBLE:
                    @in.ReadLong();
                    break;

                default:
                    throw new InvalidOperationException("Unknown type flag: " + bits.ToString("x"));
            }
        }

        public override void VisitDocument(int docID, StoredFieldVisitor visitor)
        {
            FieldsStream.Seek(IndexReader.GetStartPointer(docID));

            int docBase = FieldsStream.ReadVInt();
            int chunkDocs = FieldsStream.ReadVInt();
            if (docID < docBase || docID >= docBase + chunkDocs || docBase + chunkDocs > NumDocs)
            {
                throw new CorruptIndexException("Corrupted: docID=" + docID + ", docBase=" + docBase + ", chunkDocs=" + chunkDocs + ", numDocs=" + NumDocs + " (resource=" + FieldsStream + ")");
            }

            int numStoredFields, offset, length, totalLength;
            if (chunkDocs == 1)
            {
                numStoredFields = FieldsStream.ReadVInt();
                offset = 0;
                length = FieldsStream.ReadVInt();
                totalLength = length;
            }
            else
            {
                int bitsPerStoredFields = FieldsStream.ReadVInt();
                if (bitsPerStoredFields == 0)
                {
                    numStoredFields = FieldsStream.ReadVInt();
                }
                else if (bitsPerStoredFields > 31)
                {
                    throw new CorruptIndexException("bitsPerStoredFields=" + bitsPerStoredFields + " (resource=" + FieldsStream + ")");
                }
                else
                {
                    long filePointer = FieldsStream.FilePointer;
                    PackedInts.Reader reader = PackedInts.GetDirectReaderNoHeader(FieldsStream, PackedInts.Format.PACKED, PackedIntsVersion, chunkDocs, bitsPerStoredFields);
                    numStoredFields = (int)(reader.Get(docID - docBase));
                    FieldsStream.Seek(filePointer + PackedInts.Format.PACKED.ByteCount(PackedIntsVersion, chunkDocs, bitsPerStoredFields));
                }

                int bitsPerLength = FieldsStream.ReadVInt();
                if (bitsPerLength == 0)
                {
                    length = FieldsStream.ReadVInt();
                    offset = (docID - docBase) * length;
                    totalLength = chunkDocs * length;
                }
                else if (bitsPerStoredFields > 31)
                {
                    throw new CorruptIndexException("bitsPerLength=" + bitsPerLength + " (resource=" + FieldsStream + ")");
                }
                else
                {
                    PackedInts.ReaderIterator it = PackedInts.GetReaderIteratorNoHeader(FieldsStream, PackedInts.Format.PACKED, PackedIntsVersion, chunkDocs, bitsPerLength, 1);
                    int off = 0;
                    for (int i = 0; i < docID - docBase; ++i)
                    {
                        off += (int)it.Next();
                    }
                    offset = off;
                    length = (int)it.Next();
                    off += length;
                    for (int i = docID - docBase + 1; i < chunkDocs; ++i)
                    {
                        off += (int)it.Next();
                    }
                    totalLength = off;
                }
            }

            if ((length == 0) != (numStoredFields == 0))
            {
                throw new CorruptIndexException("length=" + length + ", numStoredFields=" + numStoredFields + " (resource=" + FieldsStream + ")");
            }
            if (numStoredFields == 0)
            {
                // nothing to do
                return;
            }

            DataInput documentInput;
            if (Version_Renamed >= CompressingStoredFieldsWriter.VERSION_BIG_CHUNKS && totalLength >= 2 * ChunkSize_Renamed)
            {
                Debug.Assert(ChunkSize_Renamed > 0);
                Debug.Assert(offset < ChunkSize_Renamed);

                Decompressor.Decompress(FieldsStream, ChunkSize_Renamed, offset, Math.Min(length, ChunkSize_Renamed - offset), Bytes);
                documentInput = new DataInputAnonymousInnerClassHelper(this, offset, length);
            }
            else
            {
                BytesRef bytes = totalLength <= BUFFER_REUSE_THRESHOLD ? this.Bytes : new BytesRef();
                Decompressor.Decompress(FieldsStream, totalLength, offset, length, bytes);
                Debug.Assert(bytes.Length == length);
                documentInput = new ByteArrayDataInput((byte[])(Array)bytes.Bytes, bytes.Offset, bytes.Length);
            }

            for (int fieldIDX = 0; fieldIDX < numStoredFields; fieldIDX++)
            {
                long infoAndBits = documentInput.ReadVLong();
                int fieldNumber = (int)((long)((ulong)infoAndBits >> CompressingStoredFieldsWriter.TYPE_BITS));
                FieldInfo fieldInfo = FieldInfos.FieldInfo(fieldNumber);

                int bits = (int)(infoAndBits & CompressingStoredFieldsWriter.TYPE_MASK);
                Debug.Assert(bits <= CompressingStoredFieldsWriter.NUMERIC_DOUBLE, "bits=" + bits.ToString("x"));

                switch (visitor.NeedsField(fieldInfo))
                {
                    case StoredFieldVisitor.Status.YES:
                        ReadField(documentInput, visitor, fieldInfo, bits);
                        break;

                    case StoredFieldVisitor.Status.NO:
                        SkipField(documentInput, bits);
                        break;

                    case StoredFieldVisitor.Status.STOP:
                        return;
                }
            }
        }

        private class DataInputAnonymousInnerClassHelper : DataInput
        {
            private readonly CompressingStoredFieldsReader OuterInstance;

            private int Offset;
            private int Length;

            public DataInputAnonymousInnerClassHelper(CompressingStoredFieldsReader outerInstance, int offset, int length)
            {
                this.OuterInstance = outerInstance;
                this.Offset = offset;
                this.Length = length;
                decompressed = outerInstance.Bytes.Length;
            }

            internal int decompressed;

            internal virtual void FillBuffer()
            {
                Debug.Assert(decompressed <= Length);
                if (decompressed == Length)
                {
                    throw new Exception();
                }
                int toDecompress = Math.Min(Length - decompressed, OuterInstance.ChunkSize_Renamed);
                OuterInstance.Decompressor.Decompress(OuterInstance.FieldsStream, toDecompress, 0, toDecompress, OuterInstance.Bytes);
                decompressed += toDecompress;
            }

            public override byte ReadByte()
            {
                if (OuterInstance.Bytes.Length == 0)
                {
                    FillBuffer();
                }
                --OuterInstance.Bytes.Length;
                return (byte)OuterInstance.Bytes.Bytes[OuterInstance.Bytes.Offset++];
            }

            public override void ReadBytes(byte[] b, int offset, int len)
            {
                while (len > OuterInstance.Bytes.Length)
                {
                    Array.Copy(OuterInstance.Bytes.Bytes, OuterInstance.Bytes.Offset, b, offset, OuterInstance.Bytes.Length);
                    len -= OuterInstance.Bytes.Length;
                    offset += OuterInstance.Bytes.Length;
                    FillBuffer();
                }
                Array.Copy(OuterInstance.Bytes.Bytes, OuterInstance.Bytes.Offset, b, offset, len);
                OuterInstance.Bytes.Offset += len;
                OuterInstance.Bytes.Length -= len;
            }
        }

        public override object Clone()
        {
            EnsureOpen();
            return new CompressingStoredFieldsReader(this);
        }

        internal int Version
        {
            get
            {
                return Version_Renamed;
            }
        }

        internal CompressionMode CompressionMode
        {
            get
            {
                return CompressionMode_Renamed;
            }
        }

        internal int ChunkSize
        {
            get
            {
                return ChunkSize_Renamed;
            }
        }

        internal ChunkIterator GetChunkIterator(int startDocID)
        {
            EnsureOpen();
            return new ChunkIterator(this, startDocID);
        }

        internal sealed class ChunkIterator
        {
            private readonly CompressingStoredFieldsReader OuterInstance;

            internal readonly ChecksumIndexInput FieldsStream;
            internal readonly BytesRef Spare;
            internal readonly BytesRef Bytes;
            internal int DocBase;
            internal int ChunkDocs;
            internal int[] NumStoredFields;
            internal int[] Lengths;

            internal ChunkIterator(CompressingStoredFieldsReader outerInstance, int startDocId)
            {
                this.OuterInstance = outerInstance;
                this.DocBase = -1;
                Bytes = new BytesRef();
                Spare = new BytesRef();
                NumStoredFields = new int[1];
                Lengths = new int[1];

                IndexInput @in = outerInstance.FieldsStream;
                @in.Seek(0);
                FieldsStream = new BufferedChecksumIndexInput(@in);
                FieldsStream.Seek(outerInstance.IndexReader.GetStartPointer(startDocId));
            }

            /// <summary>
            /// Return the decompressed size of the chunk
            /// </summary>
            internal int ChunkSize()
            {
                int sum = 0;
                for (int i = 0; i < ChunkDocs; ++i)
                {
                    sum += Lengths[i];
                }
                return sum;
            }

            /// <summary>
            /// Go to the chunk containing the provided doc ID.
            /// </summary>
            internal void Next(int doc)
            {
                Debug.Assert(doc >= DocBase + ChunkDocs, doc + " " + DocBase + " " + ChunkDocs);
                FieldsStream.Seek(OuterInstance.IndexReader.GetStartPointer(doc));

                int docBase = FieldsStream.ReadVInt();
                int chunkDocs = FieldsStream.ReadVInt();
                if (docBase < this.DocBase + this.ChunkDocs || docBase + chunkDocs > OuterInstance.NumDocs)
                {
                    throw new CorruptIndexException("Corrupted: current docBase=" + this.DocBase + ", current numDocs=" + this.ChunkDocs + ", new docBase=" + docBase + ", new numDocs=" + chunkDocs + " (resource=" + FieldsStream + ")");
                }
                this.DocBase = docBase;
                this.ChunkDocs = chunkDocs;

                if (chunkDocs > NumStoredFields.Length)
                {
                    int newLength = ArrayUtil.Oversize(chunkDocs, 4);
                    NumStoredFields = new int[newLength];
                    Lengths = new int[newLength];
                }

                if (chunkDocs == 1)
                {
                    NumStoredFields[0] = FieldsStream.ReadVInt();
                    Lengths[0] = FieldsStream.ReadVInt();
                }
                else
                {
                    int bitsPerStoredFields = FieldsStream.ReadVInt();
                    if (bitsPerStoredFields == 0)
                    {
                        CollectionsHelper.Fill(NumStoredFields, 0, chunkDocs, FieldsStream.ReadVInt());
                    }
                    else if (bitsPerStoredFields > 31)
                    {
                        throw new CorruptIndexException("bitsPerStoredFields=" + bitsPerStoredFields + " (resource=" + FieldsStream + ")");
                    }
                    else
                    {
                        PackedInts.ReaderIterator it = PackedInts.GetReaderIteratorNoHeader(FieldsStream, PackedInts.Format.PACKED, OuterInstance.PackedIntsVersion, chunkDocs, bitsPerStoredFields, 1);
                        for (int i = 0; i < chunkDocs; ++i)
                        {
                            NumStoredFields[i] = (int)it.Next();
                        }
                    }

                    int bitsPerLength = FieldsStream.ReadVInt();
                    if (bitsPerLength == 0)
                    {
                        CollectionsHelper.Fill(Lengths, 0, chunkDocs, FieldsStream.ReadVInt());
                    }
                    else if (bitsPerLength > 31)
                    {
                        throw new CorruptIndexException("bitsPerLength=" + bitsPerLength);
                    }
                    else
                    {
                        PackedInts.ReaderIterator it = PackedInts.GetReaderIteratorNoHeader(FieldsStream, PackedInts.Format.PACKED, OuterInstance.PackedIntsVersion, chunkDocs, bitsPerLength, 1);
                        for (int i = 0; i < chunkDocs; ++i)
                        {
                            Lengths[i] = (int)it.Next();
                        }
                    }
                }
            }

            /// <summary>
            /// Decompress the chunk.
            /// </summary>
            internal void Decompress()
            {
                // decompress data
                int chunkSize = ChunkSize();
                if (OuterInstance.Version_Renamed >= CompressingStoredFieldsWriter.VERSION_BIG_CHUNKS && chunkSize >= 2 * OuterInstance.ChunkSize_Renamed)
                {
                    Bytes.Offset = Bytes.Length = 0;
                    for (int decompressed = 0; decompressed < chunkSize; )
                    {
                        int toDecompress = Math.Min(chunkSize - decompressed, OuterInstance.ChunkSize_Renamed);
                        OuterInstance.Decompressor.Decompress(FieldsStream, toDecompress, 0, toDecompress, Spare);
                        Bytes.Bytes = ArrayUtil.Grow(Bytes.Bytes, Bytes.Length + Spare.Length);
                        Array.Copy(Spare.Bytes, Spare.Offset, Bytes.Bytes, Bytes.Length, Spare.Length);
                        Bytes.Length += Spare.Length;
                        decompressed += toDecompress;
                    }
                }
                else
                {
                    OuterInstance.Decompressor.Decompress(FieldsStream, chunkSize, 0, chunkSize, Bytes);
                }
                if (Bytes.Length != chunkSize)
                {
                    throw new CorruptIndexException("Corrupted: expected chunk size = " + ChunkSize() + ", got " + Bytes.Length + " (resource=" + FieldsStream + ")");
                }
            }

            /// <summary>
            /// Copy compressed data.
            /// </summary>
            internal void CopyCompressedData(DataOutput @out)
            {
                Debug.Assert(OuterInstance.Version == CompressingStoredFieldsWriter.VERSION_CURRENT);
                long chunkEnd = DocBase + ChunkDocs == OuterInstance.NumDocs ? OuterInstance.MaxPointer : OuterInstance.IndexReader.GetStartPointer(DocBase + ChunkDocs);
                @out.CopyBytes(FieldsStream, chunkEnd - FieldsStream.FilePointer);
            }

            /// <summary>
            /// Check integrity of the data. The iterator is not usable after this method has been called.
            /// </summary>
            internal void CheckIntegrity()
            {
                if (OuterInstance.Version_Renamed >= CompressingStoredFieldsWriter.VERSION_CHECKSUM)
                {
                    FieldsStream.Seek(FieldsStream.Length() - CodecUtil.FooterLength());
                    CodecUtil.CheckFooter(FieldsStream);
                }
            }
        }

        public override long RamBytesUsed()
        {
            return IndexReader.RamBytesUsed();
        }

        public override void CheckIntegrity()
        {
            if (Version_Renamed >= CompressingStoredFieldsWriter.VERSION_CHECKSUM)
            {
                CodecUtil.ChecksumEntireFile(FieldsStream);
            }
        }
    }
}