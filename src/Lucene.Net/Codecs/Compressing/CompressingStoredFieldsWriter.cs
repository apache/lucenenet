using J2N;
using Lucene.Net.Codecs.Lucene40;
using Lucene.Net.Diagnostics;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Support;
using Lucene.Net.Util;
using Lucene.Net.Util.Packed;
using System;
using System.Runtime.CompilerServices;
using Document = Lucene.Net.Documents.Document;

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
    /// <see cref="StoredFieldsWriter"/> impl for <see cref="CompressingStoredFieldsFormat"/>.
    /// <para/>
    /// @lucene.experimental
    /// </summary>
    public sealed class CompressingStoredFieldsWriter : StoredFieldsWriter
    {
        // hard limit on the maximum number of documents per chunk
        internal const int MAX_DOCUMENTS_PER_CHUNK = 128;

        internal const int STRING = 0x00;
        internal const int BYTE_ARR = 0x01;

        /// <summary>
        /// NOTE: This was NUMERIC_INT in Lucene
        /// </summary>
        internal const int NUMERIC_INT32 = 0x02;

        /// <summary>
        /// NOTE: This was NUMERIC_FLOAT in Lucene
        /// </summary>
        internal const int NUMERIC_SINGLE = 0x03;

        /// <summary>
        /// NOTE:This was NUMERIC_LONG in Lucene
        /// </summary>
        internal const int NUMERIC_INT64 = 0x04;
        internal const int NUMERIC_DOUBLE = 0x05;

        internal static readonly int TYPE_BITS = PackedInt32s.BitsRequired(NUMERIC_DOUBLE);
        internal static readonly int TYPE_MASK = (int)PackedInt32s.MaxValue(TYPE_BITS);

        internal const string CODEC_SFX_IDX = "Index";
        internal const string CODEC_SFX_DAT = "Data";
        internal const int VERSION_START = 0;
        internal const int VERSION_BIG_CHUNKS = 1;
        internal const int VERSION_CHECKSUM = 2;
        internal const int VERSION_CURRENT = VERSION_CHECKSUM;

        private readonly Directory directory;
        private readonly string segment;
        private readonly string segmentSuffix;
#pragma warning disable CA2213 // Disposable fields should be disposed
        private CompressingStoredFieldsIndexWriter indexWriter;
        private IndexOutput fieldsStream;
#pragma warning restore CA2213 // Disposable fields should be disposed

        private readonly CompressionMode compressionMode;
        private readonly Compressor compressor;
        private readonly int chunkSize;

        private readonly GrowableByteArrayDataOutput bufferedDocs;
        private int[] numStoredFields; // number of stored fields
        private int[] endOffsets; // end offsets in bufferedDocs
        private int docBase; // doc ID at the beginning of the chunk
        private int numBufferedDocs; // docBase + numBufferedDocs == current doc ID

        /// <summary>
        /// Sole constructor. </summary>
        public CompressingStoredFieldsWriter(Directory directory, SegmentInfo si, string segmentSuffix, IOContext context, string formatName, CompressionMode compressionMode, int chunkSize)
        {
            if (Debugging.AssertsEnabled) Debugging.Assert(directory != null);
            this.directory = directory;
            this.segment = si.Name;
            this.segmentSuffix = segmentSuffix;
            this.compressionMode = compressionMode;
            this.compressor = compressionMode.NewCompressor();
            this.chunkSize = chunkSize;
            this.docBase = 0;
            this.bufferedDocs = new GrowableByteArrayDataOutput(chunkSize);
            this.numStoredFields = new int[16];
            this.endOffsets = new int[16];
            this.numBufferedDocs = 0;

            bool success = false;
            IndexOutput indexStream = directory.CreateOutput(IndexFileNames.SegmentFileName(segment, segmentSuffix, Lucene40StoredFieldsWriter.FIELDS_INDEX_EXTENSION), context);
            try
            {
                fieldsStream = directory.CreateOutput(IndexFileNames.SegmentFileName(segment, segmentSuffix, Lucene40StoredFieldsWriter.FIELDS_EXTENSION), context);

                string codecNameIdx = formatName + CODEC_SFX_IDX;
                string codecNameDat = formatName + CODEC_SFX_DAT;
                CodecUtil.WriteHeader(indexStream, codecNameIdx, VERSION_CURRENT);
                CodecUtil.WriteHeader(fieldsStream, codecNameDat, VERSION_CURRENT);
                if (Debugging.AssertsEnabled)
                {
                    Debugging.Assert(CodecUtil.HeaderLength(codecNameDat) == fieldsStream.Position); // LUCENENET specific: Renamed from getFilePointer() to match FileStream
                    Debugging.Assert(CodecUtil.HeaderLength(codecNameIdx) == indexStream.Position); // LUCENENET specific: Renamed from getFilePointer() to match FileStream
                }

                indexWriter = new CompressingStoredFieldsIndexWriter(indexStream);
                indexStream = null;

                fieldsStream.WriteVInt32(chunkSize);
                fieldsStream.WriteVInt32(PackedInt32s.VERSION_CURRENT);

                success = true;
            }
            finally
            {
                if (!success)
                {
                    IOUtils.DisposeWhileHandlingException(indexStream);
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
                    IOUtils.Dispose(fieldsStream, indexWriter);
                }
                finally
                {
                    fieldsStream = null;
                    indexWriter = null;
                }
            }
        }

        public override void StartDocument(int numStoredFields)
        {
            if (numBufferedDocs == this.numStoredFields.Length)
            {
                int newLength = ArrayUtil.Oversize(numBufferedDocs + 1, 4);
                this.numStoredFields = Arrays.CopyOf(this.numStoredFields, newLength);
                endOffsets = Arrays.CopyOf(endOffsets, newLength);
            }
            this.numStoredFields[numBufferedDocs] = numStoredFields;
            ++numBufferedDocs;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public override void FinishDocument()
        {
            endOffsets[numBufferedDocs - 1] = bufferedDocs.Length;
            if (TriggerFlush())
            {
                Flush();
            }
        }

        /// <summary>
        /// NOTE: This was saveInts() in Lucene.
        /// </summary>
        private static void SaveInt32s(int[] values, int length, DataOutput @out)
        {
            if (Debugging.AssertsEnabled) Debugging.Assert(length > 0);
            if (length == 1)
            {
                @out.WriteVInt32(values[0]);
            }
            else
            {
                bool allEqual = true;
                for (int i = 1; i < length; ++i)
                {
                    if (values[i] != values[0])
                    {
                        allEqual = false;
                        break;
                    }
                }
                if (allEqual)
                {
                    @out.WriteVInt32(0);
                    @out.WriteVInt32(values[0]);
                }
                else
                {
                    long max = 0;
                    for (int i = 0; i < length; ++i)
                    {
                        max |= (uint)values[i];
                    }
                    int bitsRequired = PackedInt32s.BitsRequired(max);
                    @out.WriteVInt32(bitsRequired);
                    PackedInt32s.Writer w = PackedInt32s.GetWriterNoHeader(@out, PackedInt32s.Format.PACKED, length, bitsRequired, 1);
                    for (int i = 0; i < length; ++i)
                    {
                        w.Add(values[i]);
                    }
                    w.Finish();
                }
            }
        }

        private void WriteHeader(int docBase, int numBufferedDocs, int[] numStoredFields, int[] lengths)
        {
            // save docBase and numBufferedDocs
            fieldsStream.WriteVInt32(docBase);
            fieldsStream.WriteVInt32(numBufferedDocs);

            // save numStoredFields
            SaveInt32s(numStoredFields, numBufferedDocs, fieldsStream);

            // save lengths
            SaveInt32s(lengths, numBufferedDocs, fieldsStream);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool TriggerFlush()
        {
            return bufferedDocs.Length >= chunkSize || numBufferedDocs >= MAX_DOCUMENTS_PER_CHUNK; // chunks of at least chunkSize bytes
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void Flush()
        {
            indexWriter.WriteIndex(numBufferedDocs, fieldsStream.Position); // LUCENENET specific: Renamed from getFilePointer() to match FileStream

            // transform end offsets into lengths
            int[] lengths = endOffsets;
            for (int i = numBufferedDocs - 1; i > 0; --i)
            {
                lengths[i] = endOffsets[i] - endOffsets[i - 1];
                if (Debugging.AssertsEnabled) Debugging.Assert(lengths[i] >= 0);
            }
            WriteHeader(docBase, numBufferedDocs, numStoredFields, lengths);

            // compress stored fields to fieldsStream
            if (bufferedDocs.Length >= 2 * chunkSize)
            {
                // big chunk, slice it
                for (int compressed = 0; compressed < bufferedDocs.Length; compressed += chunkSize)
                {
                    compressor.Compress(bufferedDocs.Bytes, compressed, Math.Min(chunkSize, bufferedDocs.Length - compressed), fieldsStream);
                }
            }
            else
            {
                compressor.Compress(bufferedDocs.Bytes, 0, bufferedDocs.Length, fieldsStream);
            }

            // reset
            docBase += numBufferedDocs;
            numBufferedDocs = 0;
            bufferedDocs.Length = 0;
        }

        public override void WriteField(FieldInfo info, IIndexableField field)
        {
            int bits/* = 0*/; // LUCENENET: IDE0059: Remove unnecessary value assignment
            BytesRef bytes;
            string @string;

            // LUCENENET specific - To avoid boxing/unboxing, we don't
            // call GetNumericValue(). Instead, we check the field.NumericType and then
            // call the appropriate conversion method. 
            if (field.NumericType != NumericFieldType.NONE)
            {
                switch (field.NumericType)
                {
                    case NumericFieldType.BYTE:
                    case NumericFieldType.INT16:
                    case NumericFieldType.INT32:
                        bits = NUMERIC_INT32;
                        break;
                    case NumericFieldType.INT64:
                        bits = NUMERIC_INT64;
                        break;
                    case NumericFieldType.SINGLE:
                        bits = NUMERIC_SINGLE;
                        break;
                    case NumericFieldType.DOUBLE:
                        bits = NUMERIC_DOUBLE;
                        break;
                    default:
                        throw new ArgumentException("cannot store numeric type " + field.NumericType);
                }

                @string = null;
                bytes = null;
            }
            else
            {
                bytes = field.GetBinaryValue();
                if (bytes != null)
                {
                    bits = BYTE_ARR;
                    @string = null;
                }
                else
                {
                    bits = STRING;
                    @string = field.GetStringValue();
                    if (@string is null)
                    {
                        throw new ArgumentException("field " + field.Name + " is stored but does not have BinaryValue, StringValue nor NumericValue");
                    }
                }
            }

            long infoAndBits = (((long)info.Number) << TYPE_BITS) | (uint)bits;
            bufferedDocs.WriteVInt64(infoAndBits);

            if (bytes != null)
            {
                bufferedDocs.WriteVInt32(bytes.Length);
                bufferedDocs.WriteBytes(bytes.Bytes, bytes.Offset, bytes.Length);
            }
            else if (@string != null)
            {
                bufferedDocs.WriteString(field.GetStringValue());
            }
            else
            {
                switch (field.NumericType)
                {
                    case NumericFieldType.BYTE:
                    case NumericFieldType.INT16:
                    case NumericFieldType.INT32:
                        bufferedDocs.WriteInt32(field.GetInt32Value().Value);
                        break;
                    case NumericFieldType.INT64:
                        bufferedDocs.WriteInt64(field.GetInt64Value().Value);
                        break;
                    case NumericFieldType.SINGLE:
                        bufferedDocs.WriteInt32(BitConversion.SingleToInt32Bits(field.GetSingleValue().Value));
                        break;
                    case NumericFieldType.DOUBLE:
                        bufferedDocs.WriteInt64(BitConversion.DoubleToInt64Bits(field.GetDoubleValue().Value));
                        break;
                    default:
                        throw AssertionError.Create("Cannot get here");
                }
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public override void Abort()
        {
            IOUtils.DisposeWhileHandlingException(this);
            IOUtils.DeleteFilesIgnoringExceptions(directory, IndexFileNames.SegmentFileName(segment, segmentSuffix, Lucene40StoredFieldsWriter.FIELDS_EXTENSION), IndexFileNames.SegmentFileName(segment, segmentSuffix, Lucene40StoredFieldsWriter.FIELDS_INDEX_EXTENSION));
        }

        public override void Finish(FieldInfos fis, int numDocs)
        {
            if (numBufferedDocs > 0)
            {
                Flush();
            }
            else
            {
                if (Debugging.AssertsEnabled) Debugging.Assert(bufferedDocs.Length == 0);
            }
            if (docBase != numDocs)
            {
                throw RuntimeException.Create("Wrote " + docBase + " docs, finish called with numDocs=" + numDocs);
            }
            indexWriter.Finish(numDocs, fieldsStream.Position); // LUCENENET specific: Renamed from getFilePointer() to match FileStream
            CodecUtil.WriteFooter(fieldsStream);
            if (Debugging.AssertsEnabled) Debugging.Assert(bufferedDocs.Length == 0);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public override int Merge(MergeState mergeState)
        {
            int docCount = 0;
            int idx = 0;

            foreach (AtomicReader reader in mergeState.Readers)
            {
                SegmentReader matchingSegmentReader = mergeState.MatchingSegmentReaders[idx++];
                CompressingStoredFieldsReader matchingFieldsReader = null;
                if (matchingSegmentReader != null)
                {
                    StoredFieldsReader fieldsReader = matchingSegmentReader.FieldsReader;
                    // we can only bulk-copy if the matching reader is also a CompressingStoredFieldsReader
                    if (fieldsReader != null && fieldsReader is CompressingStoredFieldsReader compressingStoredFieldsReader)
                    {
                        matchingFieldsReader = compressingStoredFieldsReader;
                    }
                }

                int maxDoc = reader.MaxDoc;
                IBits liveDocs = reader.LiveDocs;

                if (matchingFieldsReader is null || matchingFieldsReader.Version != VERSION_CURRENT || matchingFieldsReader.CompressionMode != compressionMode || matchingFieldsReader.ChunkSize != chunkSize) // the way data is decompressed depends on the chunk size -  means reader version is not the same as the writer version
                {
                    // naive merge...
                    for (int i = NextLiveDoc(0, liveDocs, maxDoc); i < maxDoc; i = NextLiveDoc(i + 1, liveDocs, maxDoc))
                    {
                        Document doc = reader.Document(i);
                        AddDocument(doc, mergeState.FieldInfos);
                        ++docCount;
                        mergeState.CheckAbort.Work(300);
                    }
                }
                else
                {
                    int docID = NextLiveDoc(0, liveDocs, maxDoc);
                    if (docID < maxDoc)
                    {
                        // not all docs were deleted
                        CompressingStoredFieldsReader.ChunkIterator it = matchingFieldsReader.GetChunkIterator(docID);
                        int[] startOffsets = Arrays.Empty<int>();
                        do
                        {
                            // go to the next chunk that contains docID
                            it.Next(docID);
                            // transform lengths into offsets
                            if (startOffsets.Length < it.chunkDocs)
                            {
                                startOffsets = new int[ArrayUtil.Oversize(it.chunkDocs, 4)];
                            }
                            for (int i = 1; i < it.chunkDocs; ++i)
                            {
                                startOffsets[i] = startOffsets[i - 1] + it.lengths[i - 1];
                            }

                            if (numBufferedDocs == 0 && startOffsets[it.chunkDocs - 1] < chunkSize && startOffsets[it.chunkDocs - 1] + it.lengths[it.chunkDocs - 1] >= chunkSize && NextDeletedDoc(it.docBase, liveDocs, it.docBase + it.chunkDocs) == it.docBase + it.chunkDocs) // no deletion in the chunk -  chunk is large enough -  chunk is small enough -  starting a new chunk
                            {
                                if (Debugging.AssertsEnabled) Debugging.Assert(docID == it.docBase);

                                // no need to decompress, just copy data
                                indexWriter.WriteIndex(it.chunkDocs, fieldsStream.Position); // LUCENENET specific: Renamed from getFilePointer() to match FileStream
                                WriteHeader(this.docBase, it.chunkDocs, it.numStoredFields, it.lengths);
                                it.CopyCompressedData(fieldsStream);
                                this.docBase += it.chunkDocs;
                                docID = NextLiveDoc(it.docBase + it.chunkDocs, liveDocs, maxDoc);
                                docCount += it.chunkDocs;
                                mergeState.CheckAbort.Work(300 * it.chunkDocs);
                            }
                            else
                            {
                                // decompress
                                it.Decompress();
                                if (startOffsets[it.chunkDocs - 1] + it.lengths[it.chunkDocs - 1] != it.bytes.Length)
                                {
                                    throw new CorruptIndexException("Corrupted: expected chunk size=" + startOffsets[it.chunkDocs - 1] + it.lengths[it.chunkDocs - 1] + ", got " + it.bytes.Length);
                                }
                                // copy non-deleted docs
                                for (; docID < it.docBase + it.chunkDocs; docID = NextLiveDoc(docID + 1, liveDocs, maxDoc))
                                {
                                    int diff = docID - it.docBase;
                                    StartDocument(it.numStoredFields[diff]);
                                    bufferedDocs.WriteBytes(it.bytes.Bytes, it.bytes.Offset + startOffsets[diff], it.lengths[diff]);
                                    FinishDocument();
                                    ++docCount;
                                    mergeState.CheckAbort.Work(300);
                                }
                            }
                        } while (docID < maxDoc);

                        it.CheckIntegrity();
                    }
                }
            }
            Finish(mergeState.FieldInfos, docCount);
            return docCount;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int NextLiveDoc(int doc, IBits liveDocs, int maxDoc)
        {
            if (liveDocs is null)
            {
                return doc;
            }
            while (doc < maxDoc && !liveDocs.Get(doc))
            {
                ++doc;
            }
            return doc;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int NextDeletedDoc(int doc, IBits liveDocs, int maxDoc)
        {
            if (liveDocs is null)
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