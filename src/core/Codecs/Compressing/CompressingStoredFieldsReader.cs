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

using Lucene.Net.Codecs.Lucene40;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Support;
using Lucene.Net.Util;
using Lucene.Net.Util.Packed;
using System;

namespace Lucene.Net.Codecs.Compressing
{

    /**
     * {@link StoredFieldsReader} impl for {@link CompressingStoredFieldsFormat}.
     * @lucene.experimental
     */
    public sealed class CompressingStoredFieldsReader : StoredFieldsReader
    {

        private FieldInfos fieldInfos;
        private CompressingStoredFieldsIndexReader indexReader;
        private IndexInput fieldsStream;
        private int packedIntsVersion;
        private CompressionMode compressionMode;
        private Decompressor decompressor;
        private BytesRef bytes;
        private int numDocs;
        private bool closed;

        // used by clone
        private CompressingStoredFieldsReader(CompressingStoredFieldsReader reader)
        {
            this.fieldInfos = reader.fieldInfos;
            this.fieldsStream = (IndexInput)reader.fieldsStream.Clone();
            this.indexReader = (CompressingStoredFieldsIndexReader)reader.indexReader.Clone();
            this.packedIntsVersion = reader.packedIntsVersion;
            this.compressionMode = reader.compressionMode;
            this.decompressor = (Decompressor)reader.decompressor.Clone();
            this.numDocs = reader.numDocs;
            this.bytes = new BytesRef(reader.bytes.bytes.Length);
            this.closed = false;
        }

        /** Sole constructor. */
        public CompressingStoredFieldsReader(Directory d, SegmentInfo si, string segmentSuffix, FieldInfos fn,
            IOContext context, string formatName, CompressionMode compressionMode)
        {
            this.compressionMode = compressionMode;
            string segment = si.name;
            bool success = false;
            fieldInfos = fn;
            numDocs = si.DocCount;
            IndexInput indexStream = null;
            try
            {
                fieldsStream = d.OpenInput(IndexFileNames.SegmentFileName(segment, segmentSuffix, Lucene40StoredFieldsWriter.FIELDS_EXTENSION), context);
                string indexStreamFN = IndexFileNames.SegmentFileName(segment, segmentSuffix, Lucene40StoredFieldsWriter.FIELDS_INDEX_EXTENSION);
                indexStream = d.OpenInput(indexStreamFN, context);

                string codecNameIdx = formatName + CompressingStoredFieldsWriter.CODEC_SFX_IDX;
                string codecNameDat = formatName + CompressingStoredFieldsWriter.CODEC_SFX_DAT;
                CodecUtil.CheckHeader(indexStream, codecNameIdx, CompressingStoredFieldsWriter.VERSION_START, CompressingStoredFieldsWriter.VERSION_CURRENT);
                CodecUtil.CheckHeader(fieldsStream, codecNameDat, CompressingStoredFieldsWriter.VERSION_START, CompressingStoredFieldsWriter.VERSION_CURRENT);

                indexReader = new CompressingStoredFieldsIndexReader(indexStream, si);
                indexStream = null;

                packedIntsVersion = fieldsStream.ReadVInt();
                decompressor = compressionMode.newDecompressor();
                this.bytes = new BytesRef();

                success = true;
            }
            finally
            {
                if (!success)
                {
                    IOUtils.CloseWhileHandlingException((IDisposable)this, indexStream);
                }
            }
        }

        /**
         * @throws AlreadyClosedException if this FieldsReader is closed
         */
        private void EnsureOpen()
        {
            if (closed)
            {
                throw new AlreadyClosedException("this FieldsReader is closed");
            }
        }

        /** 
         * Close the underlying {@link IndexInput}s.
         */
        protected override void Dispose(bool disposing)
        {
            if (!closed)
            {
                IOUtils.Close(fieldsStream, indexReader);
                closed = true;
            }
        }

        private static void ReadField(ByteArrayDataInput input, StoredFieldVisitor visitor, FieldInfo info, int bits)
        {
            switch (bits & CompressingStoredFieldsWriter.TYPE_MASK)
            {
                case CompressingStoredFieldsWriter.BYTE_ARR:
                    int length = input.ReadVInt();
                    byte[] data = new byte[length];
                    input.ReadBytes(data, 0, length);
                    visitor.BinaryField(info, (sbyte[])(Array)data);
                    break;
                case CompressingStoredFieldsWriter.STRING:
                    length = input.ReadVInt();
                    data = new byte[length];
                    input.ReadBytes(data, 0, length);
                    visitor.StringField(info, IOUtils.CHARSET_UTF_8.GetString(data));
                    break;
                case CompressingStoredFieldsWriter.NUMERIC_INT:
                    visitor.IntField(info, input.ReadInt());
                    break;
                case CompressingStoredFieldsWriter.NUMERIC_FLOAT:
                    visitor.FloatField(info, Number.IntBitsToFloat(input.ReadInt()));
                    break;
                case CompressingStoredFieldsWriter.NUMERIC_LONG:
                    visitor.LongField(info, input.ReadLong());
                    break;
                case CompressingStoredFieldsWriter.NUMERIC_DOUBLE:
                    visitor.DoubleField(info, BitConverter.Int64BitsToDouble(input.ReadLong()));
                    break;
                default:
                    throw new InvalidOperationException("Unknown type flag: " + bits.ToString("X"));
            }
        }

        private static void SkipField(ByteArrayDataInput input, int bits)
        {
            switch (bits & CompressingStoredFieldsWriter.TYPE_MASK)
            {
                case CompressingStoredFieldsWriter.BYTE_ARR:
                case CompressingStoredFieldsWriter.STRING:
                    int length = input.ReadVInt();
                    input.SkipBytes(length);
                    break;
                case CompressingStoredFieldsWriter.NUMERIC_INT:
                case CompressingStoredFieldsWriter.NUMERIC_FLOAT:
                    input.ReadInt();
                    break;
                case CompressingStoredFieldsWriter.NUMERIC_LONG:
                case CompressingStoredFieldsWriter.NUMERIC_DOUBLE:
                    input.ReadLong();
                    break;
                default:
                    throw new InvalidOperationException("Unknown type flag: " + bits.ToString("X"));
            }
        }

        public override void VisitDocument(int docID, StoredFieldVisitor visitor)
        {
            fieldsStream.Seek(indexReader.GetStartPointer(docID));

            int docBase = fieldsStream.ReadVInt();
            int chunkDocs = fieldsStream.ReadVInt();
            if (docID < docBase
                || docID >= docBase + chunkDocs
                || docBase + chunkDocs > numDocs)
            {
                throw new CorruptIndexException("Corrupted: docID=" + docID
                    + ", docBase=" + docBase + ", chunkDocs=" + chunkDocs
                    + ", numDocs=" + numDocs);
            }

            int numStoredFields, length, offset, totalLength;
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
                    throw new CorruptIndexException("bitsPerStoredFields=" + bitsPerStoredFields);
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
                    throw new CorruptIndexException("bitsPerLength=" + bitsPerLength);
                }
                else
                {
                    PackedInts.ReaderIterator it = (PackedInts.ReaderIterator)PackedInts.GetReaderIteratorNoHeader(fieldsStream, PackedInts.Format.PACKED, packedIntsVersion, chunkDocs, bitsPerLength, 1);
                    int off = 0;
                    for (int i = 0; i < docID - docBase; ++i)
                    {
                        //TODO - HACKMP - Paul, this is a point of concern for me, in that everything from this file, and the 
                        //decompressor.Decompress() contract is looking for int.  But, I don't want to simply cast from long to int here.
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
                throw new CorruptIndexException("length=" + length + ", numStoredFields=" + numStoredFields);
            }
            if (numStoredFields == 0)
            {
                // nothing to do
                return;
            }

            decompressor.Decompress(fieldsStream, totalLength, offset, length, bytes);

            ByteArrayDataInput documentInput = new ByteArrayDataInput((byte[])(Array)bytes.bytes, bytes.offset, bytes.length);
            for (int fieldIDX = 0; fieldIDX < numStoredFields; fieldIDX++)
            {
                long infoAndBits = documentInput.ReadVLong();
                int fieldNumber = (int)Number.URShift(infoAndBits, CompressingStoredFieldsWriter.TYPE_BITS); // (infoAndBits >>> TYPE_BITS);
                FieldInfo fieldInfo = fieldInfos.FieldInfo(fieldNumber);

                int bits = (int)(infoAndBits & CompressingStoredFieldsWriter.TYPE_MASK);

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

        public override object Clone()
        {
            EnsureOpen();
            return new CompressingStoredFieldsReader(this);
        }

        public CompressionMode CompressionMode
        {
            get
            {
                return compressionMode;
            }
        }

        // .NET Port: renamed to GetChunkIterator to avoid conflict with nested type.
        internal ChunkIterator GetChunkIterator(int startDocID)
        {
            EnsureOpen();
            fieldsStream.Seek(indexReader.GetStartPointer(startDocID));
            return new ChunkIterator(this);
        }

        internal sealed class ChunkIterator
        {
            internal BytesRef bytes;
            internal int docBase;
            internal int chunkDocs;
            internal int[] numStoredFields;
            internal int[] lengths;

            private readonly CompressingStoredFieldsReader parent;

            public ChunkIterator(CompressingStoredFieldsReader parent)
            {
                this.parent = parent; // .NET Port

                this.docBase = -1;
                bytes = new BytesRef();
                numStoredFields = new int[1];
                lengths = new int[1];
            }

            /**
             * Return the decompressed size of the chunk
             */
            public int ChunkSize()
            {
                int sum = 0;
                for (int i = 0; i < chunkDocs; ++i)
                {
                    sum += lengths[i];
                }
                return sum;
            }

            /**
             * Go to the chunk containing the provided doc ID.
             */
            public void Next(int doc)
            {
                parent.fieldsStream.Seek(parent.indexReader.GetStartPointer(doc));

                int docBase = parent.fieldsStream.ReadVInt();
                int chunkDocs = parent.fieldsStream.ReadVInt();
                if (docBase < this.docBase + this.chunkDocs
                    || docBase + chunkDocs > parent.numDocs)
                {
                    throw new CorruptIndexException("Corrupted: current docBase=" + this.docBase
                        + ", current numDocs=" + this.chunkDocs + ", new docBase=" + docBase
                        + ", new numDocs=" + chunkDocs);
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
                    numStoredFields[0] = parent.fieldsStream.ReadVInt();
                    lengths[0] = parent.fieldsStream.ReadVInt();
                }
                else
                {
                    int bitsPerStoredFields = parent.fieldsStream.ReadVInt();
                    if (bitsPerStoredFields == 0)
                    {
                        Arrays.Fill(numStoredFields, 0, chunkDocs, parent.fieldsStream.ReadVInt());
                    }
                    else if (bitsPerStoredFields > 31)
                    {
                        throw new CorruptIndexException("bitsPerStoredFields=" + bitsPerStoredFields);
                    }
                    else
                    {
                        PackedInts.ReaderIterator it = (PackedInts.ReaderIterator)PackedInts.GetReaderIteratorNoHeader(parent.fieldsStream, PackedInts.Format.PACKED, parent.packedIntsVersion, chunkDocs, bitsPerStoredFields, 1);
                        for (int i = 0; i < chunkDocs; ++i)
                        {
                            numStoredFields[i] = (int)it.Next();
                        }
                    }

                    int bitsPerLength = parent.fieldsStream.ReadVInt();
                    if (bitsPerLength == 0)
                    {
                        Arrays.Fill(lengths, 0, chunkDocs, parent.fieldsStream.ReadVInt());
                    }
                    else if (bitsPerLength > 31)
                    {
                        throw new CorruptIndexException("bitsPerLength=" + bitsPerLength);
                    }
                    else
                    {
                        PackedInts.ReaderIterator it = (PackedInts.ReaderIterator)PackedInts.GetReaderIteratorNoHeader(parent.fieldsStream, PackedInts.Format.PACKED, parent.packedIntsVersion, chunkDocs, bitsPerLength, 1);
                        for (int i = 0; i < chunkDocs; ++i)
                        {
                            lengths[i] = (int)it.Next();
                        }
                    }
                }
            }

            /**
             * Decompress the chunk.
             */
            public void Decompress()
            {
                // decompress data
                int chunkSize = this.ChunkSize();
                parent.decompressor.Decompress(parent.fieldsStream, chunkSize, 0, chunkSize, bytes);
                if (bytes.length != chunkSize)
                {
                    throw new CorruptIndexException("Corrupted: expected chunk size = " + this.ChunkSize() + ", got " + bytes.length);
                }
            }

            /**
             * Copy compressed data.
             */
            public void CopyCompressedData(DataOutput output)
            {
                long chunkEnd = docBase + chunkDocs == parent.numDocs
                    ? parent.fieldsStream.Length
                    : parent.indexReader.GetStartPointer(docBase + chunkDocs);
                output.CopyBytes(parent.fieldsStream, chunkEnd - parent.fieldsStream.FilePointer);
            }

        }

    }
}