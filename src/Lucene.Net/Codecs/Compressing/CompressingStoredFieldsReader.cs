using J2N.Numerics;
using Lucene.Net.Codecs.Lucene40;
using Lucene.Net.Diagnostics;
using Lucene.Net.Support;
using System;
using System.IO;
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
    using PackedInt32s = Lucene.Net.Util.Packed.PackedInt32s;
    using SegmentInfo = Lucene.Net.Index.SegmentInfo;
    using StoredFieldVisitor = Lucene.Net.Index.StoredFieldVisitor;

    /// <summary>
    /// <see cref="StoredFieldsReader"/> impl for <see cref="CompressingStoredFieldsFormat"/>.
    /// <para/>
    /// @lucene.experimental
    /// </summary>
    public sealed class CompressingStoredFieldsReader : StoredFieldsReader
    {
        // Do not reuse the decompression buffer when there is more than 32kb to decompress
        private const int BUFFER_REUSE_THRESHOLD = 1 << 15;

        private readonly int version;
        private readonly FieldInfos fieldInfos;
        private readonly CompressingStoredFieldsIndexReader indexReader;
        private readonly long maxPointer;
#pragma warning disable CA2213 // Disposable fields should be disposed
        private readonly IndexInput fieldsStream;
#pragma warning restore CA2213 // Disposable fields should be disposed
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
                if (Debugging.AssertsEnabled) Debugging.Assert(CodecUtil.HeaderLength(codecNameIdx) == indexStream.Position); // LUCENENET specific: Renamed from getFilePointer() to match FileStream
                indexReader = new CompressingStoredFieldsIndexReader(indexStream, si);

                long maxPointer = -1;

                if (version >= CompressingStoredFieldsWriter.VERSION_CHECKSUM)
                {
                    maxPointer = indexStream.ReadVInt64();
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
                if (Debugging.AssertsEnabled) Debugging.Assert(CodecUtil.HeaderLength(codecNameDat) == fieldsStream.Position); // LUCENENET specific: Renamed from getFilePointer() to match FileStream

                if (version >= CompressingStoredFieldsWriter.VERSION_BIG_CHUNKS)
                {
                    chunkSize = fieldsStream.ReadVInt32();
                }
                else
                {
                    chunkSize = -1;
                }
                packedIntsVersion = fieldsStream.ReadVInt32();
                decompressor = compressionMode.NewDecompressor();
                this.bytes = new BytesRef();

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

        /// <exception cref="ObjectDisposedException"> If this FieldsReader is disposed. </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void EnsureOpen()
        {
            if (closed)
            {
                throw AlreadyClosedException.Create(this.GetType().FullName, "this FieldsReader is disposed.");
            }
        }

        /// <summary>
        /// Dispose the underlying <see cref="IndexInput"/>s.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override void Dispose(bool disposing)
        {
            if (!closed)
            {
                IOUtils.Dispose(fieldsStream);
                closed = true;
            }
        }

        private static void ReadField(DataInput @in, StoredFieldVisitor visitor, FieldInfo info, int bits)
        {
            switch (bits & CompressingStoredFieldsWriter.TYPE_MASK)
            {
                case CompressingStoredFieldsWriter.BYTE_ARR:
                    int length = @in.ReadVInt32();
                    var data = new byte[length];
                    @in.ReadBytes(data, 0, length);
                    visitor.BinaryField(info, data);
                    break;

                case CompressingStoredFieldsWriter.STRING:
                    length = @in.ReadVInt32();
                    data = new byte[length];
                    @in.ReadBytes(data, 0, length);
#pragma warning disable 612, 618
                    visitor.StringField(info, IOUtils.CHARSET_UTF_8.GetString(data));
#pragma warning restore 612, 618
                    break;

                case CompressingStoredFieldsWriter.NUMERIC_INT32:
                    visitor.Int32Field(info, @in.ReadInt32());
                    break;

                case CompressingStoredFieldsWriter.NUMERIC_SINGLE:
                    visitor.SingleField(info, J2N.BitConversion.Int32BitsToSingle(@in.ReadInt32()));
                    break;

                case CompressingStoredFieldsWriter.NUMERIC_INT64:
                    visitor.Int64Field(info, @in.ReadInt64());
                    break;

                case CompressingStoredFieldsWriter.NUMERIC_DOUBLE:
                    visitor.DoubleField(info, J2N.BitConversion.Int64BitsToDouble(@in.ReadInt64()));
                    break;

                default:
                    throw AssertionError.Create("Unknown type flag: " + bits.ToString("x"));
            }
        }

        private static void SkipField(DataInput @in, int bits)
        {
            switch (bits & CompressingStoredFieldsWriter.TYPE_MASK)
            {
                case CompressingStoredFieldsWriter.BYTE_ARR:
                case CompressingStoredFieldsWriter.STRING:
                    int length = @in.ReadVInt32();
                    @in.SkipBytes(length);
                    break;

                case CompressingStoredFieldsWriter.NUMERIC_INT32:
                case CompressingStoredFieldsWriter.NUMERIC_SINGLE:
                    @in.ReadInt32();
                    break;

                case CompressingStoredFieldsWriter.NUMERIC_INT64:
                case CompressingStoredFieldsWriter.NUMERIC_DOUBLE:
                    @in.ReadInt64();
                    break;

                default:
                    throw AssertionError.Create("Unknown type flag: " + bits.ToString("x"));
            }
        }

        public override void VisitDocument(int docID, StoredFieldVisitor visitor)
        {
            fieldsStream.Seek(indexReader.GetStartPointer(docID));

            int docBase = fieldsStream.ReadVInt32();
            int chunkDocs = fieldsStream.ReadVInt32();
            if (docID < docBase || docID >= docBase + chunkDocs || docBase + chunkDocs > numDocs)
            {
                throw new CorruptIndexException("Corrupted: docID=" + docID + ", docBase=" + docBase + ", chunkDocs=" + chunkDocs + ", numDocs=" + numDocs + " (resource=" + fieldsStream + ")");
            }

            int numStoredFields, offset, length, totalLength;
            if (chunkDocs == 1)
            {
                numStoredFields = fieldsStream.ReadVInt32();
                offset = 0;
                length = fieldsStream.ReadVInt32();
                totalLength = length;
            }
            else
            {
                int bitsPerStoredFields = fieldsStream.ReadVInt32();
                if (bitsPerStoredFields == 0)
                {
                    numStoredFields = fieldsStream.ReadVInt32();
                }
                else if (bitsPerStoredFields > 31)
                {
                    throw new CorruptIndexException("bitsPerStoredFields=" + bitsPerStoredFields + " (resource=" + fieldsStream + ")");
                }
                else
                {
                    long filePointer = fieldsStream.Position; // LUCENENET specific: Renamed from getFilePointer() to match FileStream
                    PackedInt32s.Reader reader = PackedInt32s.GetDirectReaderNoHeader(fieldsStream, PackedInt32s.Format.PACKED, packedIntsVersion, chunkDocs, bitsPerStoredFields);
                    numStoredFields = (int)(reader.Get(docID - docBase));
                    fieldsStream.Seek(filePointer + PackedInt32s.Format.PACKED.ByteCount(packedIntsVersion, chunkDocs, bitsPerStoredFields));
                }

                int bitsPerLength = fieldsStream.ReadVInt32();
                if (bitsPerLength == 0)
                {
                    length = fieldsStream.ReadVInt32();
                    offset = (docID - docBase) * length;
                    totalLength = chunkDocs * length;
                }
                else if (bitsPerStoredFields > 31)
                {
                    throw new CorruptIndexException("bitsPerLength=" + bitsPerLength + " (resource=" + fieldsStream + ")");
                }
                else
                {
                    PackedInt32s.IReaderIterator it = PackedInt32s.GetReaderIteratorNoHeader(fieldsStream, PackedInt32s.Format.PACKED, packedIntsVersion, chunkDocs, bitsPerLength, 1);
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
                if (Debugging.AssertsEnabled)
                {
                    Debugging.Assert(chunkSize > 0);
                    Debugging.Assert(offset < chunkSize);
                }

                decompressor.Decompress(fieldsStream, chunkSize, offset, Math.Min(length, chunkSize - offset), bytes);
                documentInput = new DataInputAnonymousClass(this, length);
            }
            else
            {
                BytesRef bytes = totalLength <= BUFFER_REUSE_THRESHOLD ? this.bytes : new BytesRef();
                decompressor.Decompress(fieldsStream, totalLength, offset, length, bytes);
                if (Debugging.AssertsEnabled) Debugging.Assert(bytes.Length == length);
                documentInput = new ByteArrayDataInput(bytes.Bytes, bytes.Offset, bytes.Length);
            }

            for (int fieldIDX = 0; fieldIDX < numStoredFields; fieldIDX++)
            {
                long infoAndBits = documentInput.ReadVInt64();
                int fieldNumber = (int)infoAndBits.TripleShift(CompressingStoredFieldsWriter.TYPE_BITS);
                FieldInfo fieldInfo = fieldInfos.FieldInfo(fieldNumber);

                int bits = (int)(infoAndBits & CompressingStoredFieldsWriter.TYPE_MASK);
                if (Debugging.AssertsEnabled) Debugging.Assert(bits <= CompressingStoredFieldsWriter.NUMERIC_DOUBLE,"bits={0:x}", bits);

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

        private sealed class DataInputAnonymousClass : DataInput
        {
            private readonly CompressingStoredFieldsReader outerInstance;

            private readonly int length;

            public DataInputAnonymousClass(CompressingStoredFieldsReader outerInstance, int length)
            {
                this.outerInstance = outerInstance;
                this.length = length;
                decompressed = outerInstance.bytes.Length;
            }

            internal int decompressed;

            internal void FillBuffer()
            {
                if (Debugging.AssertsEnabled) Debugging.Assert(decompressed <= length);
                if (decompressed == length)
                {
                    throw EOFException.Create();
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
                    Arrays.Copy(outerInstance.bytes.Bytes, outerInstance.bytes.Offset, b, offset, outerInstance.bytes.Length);
                    len -= outerInstance.bytes.Length;
                    offset += outerInstance.bytes.Length;
                    FillBuffer();
                }
                Arrays.Copy(outerInstance.bytes.Bytes, outerInstance.bytes.Offset, b, offset, len);
                outerInstance.bytes.Offset += len;
                outerInstance.bytes.Length -= len;
            }
        }

        public override object Clone()
        {
            EnsureOpen();
            return new CompressingStoredFieldsReader(this);
        }

        internal int Version => version;

        internal CompressionMode CompressionMode => compressionMode;

        internal int ChunkSize => chunkSize;

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
            /// Go to the chunk containing the provided <paramref name="doc"/> ID.
            /// </summary>
            internal void Next(int doc)
            {
                if (Debugging.AssertsEnabled) Debugging.Assert(doc >= this.docBase + this.chunkDocs, "{0} {1} {2}", doc, this.docBase, this.chunkDocs);
                fieldsStream.Seek(outerInstance.indexReader.GetStartPointer(doc));

                int docBase = fieldsStream.ReadVInt32();
                int chunkDocs = fieldsStream.ReadVInt32();
                if (docBase < this.docBase + this.chunkDocs || docBase + chunkDocs > outerInstance.numDocs)
                {
                    throw new CorruptIndexException($"Corrupted: current docBase={this.docBase}, current numDocs={this.chunkDocs}, new docBase={docBase}, new numDocs={chunkDocs} (resource={fieldsStream})");
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
                    numStoredFields[0] = fieldsStream.ReadVInt32();
                    lengths[0] = fieldsStream.ReadVInt32();
                }
                else
                {
                    int bitsPerStoredFields = fieldsStream.ReadVInt32();
                    if (bitsPerStoredFields == 0)
                    {
                        Arrays.Fill(numStoredFields, 0, chunkDocs, fieldsStream.ReadVInt32());
                    }
                    else if (bitsPerStoredFields > 31)
                    {
                        throw new CorruptIndexException("bitsPerStoredFields=" + bitsPerStoredFields + " (resource=" + fieldsStream + ")");
                    }
                    else
                    {
                        PackedInt32s.IReaderIterator it = PackedInt32s.GetReaderIteratorNoHeader(fieldsStream, PackedInt32s.Format.PACKED, outerInstance.packedIntsVersion, chunkDocs, bitsPerStoredFields, 1);
                        for (int i = 0; i < chunkDocs; ++i)
                        {
                            numStoredFields[i] = (int)it.Next();
                        }
                    }

                    int bitsPerLength = fieldsStream.ReadVInt32();
                    if (bitsPerLength == 0)
                    {
                        Arrays.Fill(lengths, 0, chunkDocs, fieldsStream.ReadVInt32());
                    }
                    else if (bitsPerLength > 31)
                    {
                        throw new CorruptIndexException($"bitsPerLength={bitsPerLength}");
                    }
                    else
                    {
                        PackedInt32s.IReaderIterator it = PackedInt32s.GetReaderIteratorNoHeader(fieldsStream, PackedInt32s.Format.PACKED, outerInstance.packedIntsVersion, chunkDocs, bitsPerLength, 1);
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
                        Arrays.Copy(spare.Bytes, spare.Offset, bytes.Bytes, bytes.Length, spare.Length);
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
                if (Debugging.AssertsEnabled) Debugging.Assert(outerInstance.Version == CompressingStoredFieldsWriter.VERSION_CURRENT);
                long chunkEnd = docBase + chunkDocs == outerInstance.numDocs ? outerInstance.maxPointer : outerInstance.indexReader.GetStartPointer(docBase + chunkDocs);
                @out.CopyBytes(fieldsStream, chunkEnd - fieldsStream.Position); // LUCENENET specific: Renamed from getFilePointer() to match FileStream
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