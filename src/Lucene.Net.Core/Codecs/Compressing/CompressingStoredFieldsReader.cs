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

        private readonly int version;
        private readonly FieldInfos fieldInfos;
        private readonly CompressingStoredFieldsIndexReader indexReader;
        private readonly long maxPointer;
        private readonly IndexInput fieldsStream;
        private readonly int chunkSize;
        private readonly int packedIntsVersion;
        private readonly CompressionMode compressionMode;
        private readonly Decompressor decompressor;
        private readonly BytesRef bytes;
        private readonly int numDocs;
        private bool closed;

        // used by clone
        private CompressingStoredFieldsReader(CompressingStoredFieldsReader reader)
        {
            this.version = reader.version;
            this.fieldInfos = reader.fieldInfos;
            this.fieldsStream = (IndexInput)reader.fieldsStream.Clone();
            this.indexReader = (CompressingStoredFieldsIndexReader)reader.indexReader.Clone();
            this.maxPointer = reader.maxPointer;
            this.chunkSize = reader.chunkSize;
            this.packedIntsVersion = reader.packedIntsVersion;
            this.compressionMode = reader.compressionMode;
            this.decompressor = (Decompressor)reader.decompressor.Clone();
            this.numDocs = reader.numDocs;
            this.bytes = new BytesRef(reader.bytes.Bytes.Length);
            this.closed = false;
        }

        /// <summary>
        /// Sole constructor. </summary>
        public CompressingStoredFieldsReader(Directory d, SegmentInfo si, string segmentSuffix, FieldInfos fn, IOContext context, string formatName, CompressionMode compressionMode)
        {
            this.compressionMode = compressionMode;
            string segment = si.Name;
            bool success = false;
            fieldInfos = fn;
            numDocs = si.DocCount;
            ChecksumIndexInput indexStream = null;
            try
            {
                string indexStreamFN = IndexFileNames.SegmentFileName(segment, segmentSuffix, Lucene40StoredFieldsWriter.FIELDS_INDEX_EXTENSION);
                string fieldsStreamFN = IndexFileNames.SegmentFileName(segment, segmentSuffix, Lucene40StoredFieldsWriter.FIELDS_EXTENSION);
                // Load the index into memory
                indexStream = d.OpenChecksumInput(indexStreamFN, context);
                string codecNameIdx = formatName + CompressingStoredFieldsWriter.CODEC_SFX_IDX;
                version = CodecUtil.CheckHeader(indexStream, codecNameIdx, CompressingStoredFieldsWriter.VERSION_START, CompressingStoredFieldsWriter.VERSION_CURRENT);
                Debug.Assert(CodecUtil.HeaderLength(codecNameIdx) == indexStream.FilePointer);
                indexReader = new CompressingStoredFieldsIndexReader(indexStream, si);

                long maxPointer = -1;

                if (version >= CompressingStoredFieldsWriter.VERSION_CHECKSUM)
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
                fieldsStream = d.OpenInput(fieldsStreamFN, context);
                if (version >= CompressingStoredFieldsWriter.VERSION_CHECKSUM)
                {
                    if (maxPointer + CodecUtil.FooterLength() != fieldsStream.Length)
                    {
                        throw new CorruptIndexException("Invalid fieldsStream maxPointer (file truncated?): maxPointer=" + maxPointer + ", length=" + fieldsStream.Length);
                    }
                }
                else
                {
                    maxPointer = fieldsStream.Length;
                }
                this.maxPointer = maxPointer;
                string codecNameDat = formatName + CompressingStoredFieldsWriter.CODEC_SFX_DAT;
                int fieldsVersion = CodecUtil.CheckHeader(fieldsStream, codecNameDat, CompressingStoredFieldsWriter.VERSION_START, CompressingStoredFieldsWriter.VERSION_CURRENT);
                if (version != fieldsVersion)
                {
                    throw new CorruptIndexException("Version mismatch between stored fields index and data: " + version + " != " + fieldsVersion);
                }
                Debug.Assert(CodecUtil.HeaderLength(codecNameDat) == fieldsStream.FilePointer);

                if (version >= CompressingStoredFieldsWriter.VERSION_BIG_CHUNKS)
                {
                    chunkSize = fieldsStream.ReadVInt();
                }
                else
                {
                    chunkSize = -1;
                }
                packedIntsVersion = fieldsStream.ReadVInt();
                decompressor = compressionMode.NewDecompressor();
                this.bytes = new BytesRef();

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
            if (closed)
            {
                throw new Exception("this FieldsReader is closed");
            }
        }

        /// <summary>
        /// Close the underlying <seealso cref="IndexInput"/>s.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (!closed)
            {
                IOUtils.Close(fieldsStream);
                closed = true;
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
            fieldsStream.Seek(indexReader.GetStartPointer(docID));

            int docBase = fieldsStream.ReadVInt();
            int chunkDocs = fieldsStream.ReadVInt();
            if (docID < docBase || docID >= docBase + chunkDocs || docBase + chunkDocs > numDocs)
            {
                throw new CorruptIndexException("Corrupted: docID=" + docID + ", docBase=" + docBase + ", chunkDocs=" + chunkDocs + ", numDocs=" + numDocs + " (resource=" + fieldsStream + ")");
            }

            int numStoredFields, offset, length, totalLength;
            if (chunkDocs == 1)
            {
                numStoredFields = fieldsStream.ReadVInt();
                offset = 0;
                length = fieldsStream.ReadVInt();
                totalLength = length;
            }
            else
            {
                int bitsPerStoredFields = fieldsStream.ReadVInt();
                if (bitsPerStoredFields == 0)
                {
                    numStoredFields = fieldsStream.ReadVInt();
                }
                else if (bitsPerStoredFields > 31)
                {
                    throw new CorruptIndexException("bitsPerStoredFields=" + bitsPerStoredFields + " (resource=" + fieldsStream + ")");
                }
                else
                {
                    long filePointer = fieldsStream.FilePointer;
                    PackedInts.Reader reader = PackedInts.GetDirectReaderNoHeader(fieldsStream, PackedInts.Format.PACKED, packedIntsVersion, chunkDocs, bitsPerStoredFields);
                    numStoredFields = (int)(reader.Get(docID - docBase));
                    fieldsStream.Seek(filePointer + PackedInts.Format.PACKED.ByteCount(packedIntsVersion, chunkDocs, bitsPerStoredFields));
                }

                int bitsPerLength = fieldsStream.ReadVInt();
                if (bitsPerLength == 0)
                {
                    length = fieldsStream.ReadVInt();
                    offset = (docID - docBase) * length;
                    totalLength = chunkDocs * length;
                }
                else if (bitsPerStoredFields > 31)
                {
                    throw new CorruptIndexException("bitsPerLength=" + bitsPerLength + " (resource=" + fieldsStream + ")");
                }
                else
                {
                    PackedInts.IReaderIterator it = PackedInts.GetReaderIteratorNoHeader(fieldsStream, PackedInts.Format.PACKED, packedIntsVersion, chunkDocs, bitsPerLength, 1);
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
                throw new CorruptIndexException("length=" + length + ", numStoredFields=" + numStoredFields + " (resource=" + fieldsStream + ")");
            }
            if (numStoredFields == 0)
            {
                // nothing to do
                return;
            }

            DataInput documentInput;
            if (version >= CompressingStoredFieldsWriter.VERSION_BIG_CHUNKS && totalLength >= 2 * chunkSize)
            {
                Debug.Assert(chunkSize > 0);
                Debug.Assert(offset < chunkSize);

                decompressor.Decompress(fieldsStream, chunkSize, offset, Math.Min(length, chunkSize - offset), bytes);
                documentInput = new DataInputAnonymousInnerClassHelper(this, offset, length);
            }
            else
            {
                BytesRef bytes = totalLength <= BUFFER_REUSE_THRESHOLD ? this.bytes : new BytesRef();
                decompressor.Decompress(fieldsStream, totalLength, offset, length, bytes);
                Debug.Assert(bytes.Length == length);
                documentInput = new ByteArrayDataInput((byte[])(Array)bytes.Bytes, bytes.Offset, bytes.Length);
            }

            for (int fieldIDX = 0; fieldIDX < numStoredFields; fieldIDX++)
            {
                long infoAndBits = documentInput.ReadVLong();
                int fieldNumber = (int)((long)((ulong)infoAndBits >> CompressingStoredFieldsWriter.TYPE_BITS));
                FieldInfo fieldInfo = fieldInfos.FieldInfo(fieldNumber);

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
            private readonly CompressingStoredFieldsReader outerInstance;

            private int offset;
            private int length;

            public DataInputAnonymousInnerClassHelper(CompressingStoredFieldsReader outerInstance, int offset, int length)
            {
                this.outerInstance = outerInstance;
                this.offset = offset;
                this.length = length;
                decompressed = outerInstance.bytes.Length;
            }

            internal int decompressed;

            internal virtual void FillBuffer()
            {
                Debug.Assert(decompressed <= length);
                if (decompressed == length)
                {
                    throw new Exception();
                }
                int toDecompress = Math.Min(length - decompressed, outerInstance.chunkSize);
                outerInstance.decompressor.Decompress(outerInstance.fieldsStream, toDecompress, 0, toDecompress, outerInstance.bytes);
                decompressed += toDecompress;
            }

            public override byte ReadByte()
            {
                if (outerInstance.bytes.Length == 0)
                {
                    FillBuffer();
                }
                --outerInstance.bytes.Length;
                return (byte)outerInstance.bytes.Bytes[outerInstance.bytes.Offset++];
            }

            public override void ReadBytes(byte[] b, int offset, int len)
            {
                while (len > outerInstance.bytes.Length)
                {
                    Array.Copy(outerInstance.bytes.Bytes, outerInstance.bytes.Offset, b, offset, outerInstance.bytes.Length);
                    len -= outerInstance.bytes.Length;
                    offset += outerInstance.bytes.Length;
                    FillBuffer();
                }
                Array.Copy(outerInstance.bytes.Bytes, outerInstance.bytes.Offset, b, offset, len);
                outerInstance.bytes.Offset += len;
                outerInstance.bytes.Length -= len;
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
                return version;
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

        internal ChunkIterator GetChunkIterator(int startDocID)
        {
            EnsureOpen();
            return new ChunkIterator(this, startDocID);
        }

        internal sealed class ChunkIterator
        {
            private readonly CompressingStoredFieldsReader outerInstance;

            internal readonly ChecksumIndexInput fieldsStream;
            internal readonly BytesRef spare;
            internal readonly BytesRef bytes;
            internal int docBase;
            internal int chunkDocs;
            internal int[] numStoredFields;
            internal int[] lengths;

            internal ChunkIterator(CompressingStoredFieldsReader outerInstance, int startDocId)
            {
                this.outerInstance = outerInstance;
                this.docBase = -1;
                bytes = new BytesRef();
                spare = new BytesRef();
                numStoredFields = new int[1];
                lengths = new int[1];

                IndexInput @in = outerInstance.fieldsStream;
                @in.Seek(0);
                fieldsStream = new BufferedChecksumIndexInput(@in);
                fieldsStream.Seek(outerInstance.indexReader.GetStartPointer(startDocId));
            }

            /// <summary>
            /// Return the decompressed size of the chunk
            /// </summary>
            internal int ChunkSize()
            {
                int sum = 0;
                for (int i = 0; i < chunkDocs; ++i)
                {
                    sum += lengths[i];
                }
                return sum;
            }

            /// <summary>
            /// Go to the chunk containing the provided doc ID.
            /// </summary>
            internal void Next(int doc)
            {
                Debug.Assert(doc >= this.docBase + this.chunkDocs, doc + " " + this.docBase + " " + this.chunkDocs);
                fieldsStream.Seek(outerInstance.indexReader.GetStartPointer(doc));

                int docBase = fieldsStream.ReadVInt();
                int chunkDocs = fieldsStream.ReadVInt();
                if (docBase < this.docBase + this.chunkDocs || docBase + chunkDocs > outerInstance.numDocs)
                {
                    throw new CorruptIndexException("Corrupted: current docBase=" + this.docBase + ", current numDocs=" + this.chunkDocs + ", new docBase=" + docBase + ", new numDocs=" + chunkDocs + " (resource=" + fieldsStream + ")");
                }
                this.docBase = docBase;
                this.chunkDocs = chunkDocs;

                if (chunkDocs > numStoredFields.Length)
                {
                    int newLength = ArrayUtil.Oversize(chunkDocs, 4);
                    numStoredFields = new int[newLength];
                    lengths = new int[newLength];
                }

                if (chunkDocs == 1)
                {
                    numStoredFields[0] = fieldsStream.ReadVInt();
                    lengths[0] = fieldsStream.ReadVInt();
                }
                else
                {
                    int bitsPerStoredFields = fieldsStream.ReadVInt();
                    if (bitsPerStoredFields == 0)
                    {
                        CollectionsHelper.Fill(numStoredFields, 0, chunkDocs, fieldsStream.ReadVInt());
                    }
                    else if (bitsPerStoredFields > 31)
                    {
                        throw new CorruptIndexException("bitsPerStoredFields=" + bitsPerStoredFields + " (resource=" + fieldsStream + ")");
                    }
                    else
                    {
                        PackedInts.IReaderIterator it = PackedInts.GetReaderIteratorNoHeader(fieldsStream, PackedInts.Format.PACKED, outerInstance.packedIntsVersion, chunkDocs, bitsPerStoredFields, 1);
                        for (int i = 0; i < chunkDocs; ++i)
                        {
                            numStoredFields[i] = (int)it.Next();
                        }
                    }

                    int bitsPerLength = fieldsStream.ReadVInt();
                    if (bitsPerLength == 0)
                    {
                        CollectionsHelper.Fill(lengths, 0, chunkDocs, fieldsStream.ReadVInt());
                    }
                    else if (bitsPerLength > 31)
                    {
                        throw new CorruptIndexException("bitsPerLength=" + bitsPerLength);
                    }
                    else
                    {
                        PackedInts.IReaderIterator it = PackedInts.GetReaderIteratorNoHeader(fieldsStream, PackedInts.Format.PACKED, outerInstance.packedIntsVersion, chunkDocs, bitsPerLength, 1);
                        for (int i = 0; i < chunkDocs; ++i)
                        {
                            lengths[i] = (int)it.Next();
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
                if (outerInstance.version >= CompressingStoredFieldsWriter.VERSION_BIG_CHUNKS && chunkSize >= 2 * outerInstance.chunkSize)
                {
                    bytes.Offset = bytes.Length = 0;
                    for (int decompressed = 0; decompressed < chunkSize; )
                    {
                        int toDecompress = Math.Min(chunkSize - decompressed, outerInstance.chunkSize);
                        outerInstance.decompressor.Decompress(fieldsStream, toDecompress, 0, toDecompress, spare);
                        bytes.Bytes = ArrayUtil.Grow(bytes.Bytes, bytes.Length + spare.Length);
                        Array.Copy(spare.Bytes, spare.Offset, bytes.Bytes, bytes.Length, spare.Length);
                        bytes.Length += spare.Length;
                        decompressed += toDecompress;
                    }
                }
                else
                {
                    outerInstance.decompressor.Decompress(fieldsStream, chunkSize, 0, chunkSize, bytes);
                }
                if (bytes.Length != chunkSize)
                {
                    throw new CorruptIndexException("Corrupted: expected chunk size = " + ChunkSize() + ", got " + bytes.Length + " (resource=" + fieldsStream + ")");
                }
            }

            /// <summary>
            /// Copy compressed data.
            /// </summary>
            internal void CopyCompressedData(DataOutput @out)
            {
                Debug.Assert(outerInstance.Version == CompressingStoredFieldsWriter.VERSION_CURRENT);
                long chunkEnd = docBase + chunkDocs == outerInstance.numDocs ? outerInstance.maxPointer : outerInstance.indexReader.GetStartPointer(docBase + chunkDocs);
                @out.CopyBytes(fieldsStream, chunkEnd - fieldsStream.FilePointer);
            }

            /// <summary>
            /// Check integrity of the data. The iterator is not usable after this method has been called.
            /// </summary>
            internal void CheckIntegrity()
            {
                if (outerInstance.version >= CompressingStoredFieldsWriter.VERSION_CHECKSUM)
                {
                    fieldsStream.Seek(fieldsStream.Length - CodecUtil.FooterLength());
                    CodecUtil.CheckFooter(fieldsStream);
                }
            }
        }

        public override long RamBytesUsed()
        {
            return indexReader.RamBytesUsed();
        }

        public override void CheckIntegrity()
        {
            if (version >= CompressingStoredFieldsWriter.VERSION_CHECKSUM)
            {
                CodecUtil.ChecksumEntireFile(fieldsStream);
            }
        }
    }
}