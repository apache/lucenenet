using Lucene.Net.Codecs.Lucene40;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Support;
using Lucene.Net.Util;
using Lucene.Net.Util.Packed;
using System;
using System.Diagnostics;
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
    /// <seealso cref="StoredFieldsWriter"/> impl for <seealso cref="CompressingStoredFieldsFormat"/>.
    /// @lucene.experimental
    /// </summary>
    public sealed class CompressingStoredFieldsWriter : StoredFieldsWriter
    {
        // hard limit on the maximum number of documents per chunk
        internal const int MAX_DOCUMENTS_PER_CHUNK = 128;

        internal const int STRING = 0x00;
        internal const int BYTE_ARR = 0x01;
        internal const int NUMERIC_INT = 0x02;
        internal const int NUMERIC_FLOAT = 0x03;
        internal const int NUMERIC_LONG = 0x04;
        internal const int NUMERIC_DOUBLE = 0x05;

        internal static readonly int TYPE_BITS = PackedInts.BitsRequired(NUMERIC_DOUBLE);
        internal static readonly int TYPE_MASK = (int)PackedInts.MaxValue(TYPE_BITS);

        internal const string CODEC_SFX_IDX = "Index";
        internal const string CODEC_SFX_DAT = "Data";
        internal const int VERSION_START = 0;
        internal const int VERSION_BIG_CHUNKS = 1;
        internal const int VERSION_CHECKSUM = 2;
        internal const int VERSION_CURRENT = VERSION_CHECKSUM;

        private readonly Directory Directory;
        private readonly string Segment;
        private readonly string SegmentSuffix;
        private CompressingStoredFieldsIndexWriter IndexWriter;
        private IndexOutput FieldsStream;

        private readonly CompressionMode CompressionMode;
        private readonly Compressor Compressor;
        private readonly int ChunkSize;

        private readonly GrowableByteArrayDataOutput BufferedDocs;
        private int[] NumStoredFields; // number of stored fields
        private int[] EndOffsets; // end offsets in bufferedDocs
        private int DocBase; // doc ID at the beginning of the chunk
        private int NumBufferedDocs; // docBase + numBufferedDocs == current doc ID

        /// <summary>
        /// Sole constructor. </summary>
        public CompressingStoredFieldsWriter(Directory directory, SegmentInfo si, string segmentSuffix, IOContext context, string formatName, CompressionMode compressionMode, int chunkSize)
        {
            Debug.Assert(directory != null);
            this.Directory = directory;
            this.Segment = si.Name;
            this.SegmentSuffix = segmentSuffix;
            this.CompressionMode = compressionMode;
            this.Compressor = compressionMode.NewCompressor();
            this.ChunkSize = chunkSize;
            this.DocBase = 0;
            this.BufferedDocs = new GrowableByteArrayDataOutput(chunkSize);
            this.NumStoredFields = new int[16];
            this.EndOffsets = new int[16];
            this.NumBufferedDocs = 0;

            bool success = false;
            IndexOutput indexStream = directory.CreateOutput(IndexFileNames.SegmentFileName(Segment, segmentSuffix, Lucene40StoredFieldsWriter.FIELDS_INDEX_EXTENSION), context);
            try
            {
                FieldsStream = directory.CreateOutput(IndexFileNames.SegmentFileName(Segment, segmentSuffix, Lucene40StoredFieldsWriter.FIELDS_EXTENSION), context);

                string codecNameIdx = formatName + CODEC_SFX_IDX;
                string codecNameDat = formatName + CODEC_SFX_DAT;
                CodecUtil.WriteHeader(indexStream, codecNameIdx, VERSION_CURRENT);
                CodecUtil.WriteHeader(FieldsStream, codecNameDat, VERSION_CURRENT);
                Debug.Assert(CodecUtil.HeaderLength(codecNameDat) == FieldsStream.FilePointer);
                Debug.Assert(CodecUtil.HeaderLength(codecNameIdx) == indexStream.FilePointer);

                IndexWriter = new CompressingStoredFieldsIndexWriter(indexStream);
                indexStream = null;

                FieldsStream.WriteVInt(chunkSize);
                FieldsStream.WriteVInt(PackedInts.VERSION_CURRENT);

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
                    IOUtils.Close(FieldsStream, IndexWriter);
                }
                finally
                {
                    FieldsStream = null;
                    IndexWriter = null;
                }
            }
        }

        public override void StartDocument(int numStoredFields)
        {
            if (NumBufferedDocs == this.NumStoredFields.Length)
            {
                int newLength = ArrayUtil.Oversize(NumBufferedDocs + 1, 4);
                this.NumStoredFields = Arrays.CopyOf(this.NumStoredFields, newLength);
                EndOffsets = Arrays.CopyOf(EndOffsets, newLength);
            }
            this.NumStoredFields[NumBufferedDocs] = numStoredFields;
            ++NumBufferedDocs;
        }

        public override void FinishDocument()
        {
            EndOffsets[NumBufferedDocs - 1] = BufferedDocs.Length;
            if (TriggerFlush())
            {
                Flush();
            }
        }

        private static void SaveInts(int[] values, int length, DataOutput @out)
        {
            Debug.Assert(length > 0);
            if (length == 1)
            {
                @out.WriteVInt(values[0]);
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
                    @out.WriteVInt(0);
                    @out.WriteVInt(values[0]);
                }
                else
                {
                    long max = 0;
                    for (int i = 0; i < length; ++i)
                    {
                        max |= (uint)values[i];
                    }
                    int bitsRequired = PackedInts.BitsRequired(max);
                    @out.WriteVInt(bitsRequired);
                    PackedInts.Writer w = PackedInts.GetWriterNoHeader(@out, PackedInts.Format.PACKED, length, bitsRequired, 1);
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
            FieldsStream.WriteVInt(docBase);
            FieldsStream.WriteVInt(numBufferedDocs);

            // save numStoredFields
            SaveInts(numStoredFields, numBufferedDocs, FieldsStream);

            // save lengths
            SaveInts(lengths, numBufferedDocs, FieldsStream);
        }

        private bool TriggerFlush()
        {
            return BufferedDocs.Length >= ChunkSize || NumBufferedDocs >= MAX_DOCUMENTS_PER_CHUNK; // chunks of at least chunkSize bytes
        }

        private void Flush()
        {
            IndexWriter.WriteIndex(NumBufferedDocs, FieldsStream.FilePointer);

            // transform end offsets into lengths
            int[] lengths = EndOffsets;
            for (int i = NumBufferedDocs - 1; i > 0; --i)
            {
                lengths[i] = EndOffsets[i] - EndOffsets[i - 1];
                Debug.Assert(lengths[i] >= 0);
            }
            WriteHeader(DocBase, NumBufferedDocs, NumStoredFields, lengths);

            // compress stored fields to fieldsStream
            if (BufferedDocs.Length >= 2 * ChunkSize)
            {
                // big chunk, slice it
                for (int compressed = 0; compressed < BufferedDocs.Length; compressed += ChunkSize)
                {
                    Compressor.Compress(BufferedDocs.Bytes, compressed, Math.Min(ChunkSize, BufferedDocs.Length - compressed), FieldsStream);
                }
            }
            else
            {
                Compressor.Compress(BufferedDocs.Bytes, 0, BufferedDocs.Length, FieldsStream);
            }

            // reset
            DocBase += NumBufferedDocs;
            NumBufferedDocs = 0;
            BufferedDocs.Length = 0;
        }

        public override void WriteField(FieldInfo info, IIndexableField field)
        {
            int bits = 0;
            BytesRef bytes;
            string @string;

            object number = (object)field.GetNumericValue();
            if (number != null)
            {
                if (number is string)
                {
                    string numStr = number.ToString();
                    sbyte dummySbyte;
                    short dummyShort;
                    int dummyInt;
                    long dummyLong;
                    float dummyFloat;
                    double dummyDouble;
                    if (sbyte.TryParse(numStr, out dummySbyte) || short.TryParse(numStr, out dummyShort) || int.TryParse(numStr, out dummyInt))
                    {
                        bits = NUMERIC_INT;
                    }
                    else if (long.TryParse(numStr, out dummyLong))
                    {
                        bits = NUMERIC_LONG;
                    }
                    else if (float.TryParse(numStr, out dummyFloat))
                    {
                        bits = NUMERIC_FLOAT;
                    }
                    else if (double.TryParse(numStr, out dummyDouble))
                    {
                        bits = NUMERIC_DOUBLE;
                    }
                    else
                    {
                        throw new System.ArgumentException("cannot store numeric type " + number.GetType());
                    }
                }
                else
                {
                    if (number is sbyte || number is short || number is int)
                    {
                        bits = NUMERIC_INT;
                    }
                    else if (number is long)
                    {
                        bits = NUMERIC_LONG;
                    }
                    else if (number is float)
                    {
                        bits = NUMERIC_FLOAT;
                    }
                    else if (number is double)
                    {
                        bits = NUMERIC_DOUBLE;
                    }
                    else
                    {
                        throw new System.ArgumentException("cannot store numeric type " + number.GetType());
                    }
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
                    if (@string == null)
                    {
                        throw new System.ArgumentException("field " + field.Name + " is stored but does not have binaryValue, stringValue nor numericValue");
                    }
                }
            }

            long infoAndBits = (((long)info.Number) << TYPE_BITS) | bits;
            BufferedDocs.WriteVLong(infoAndBits);

            if (bytes != null)
            {
                BufferedDocs.WriteVInt(bytes.Length);
                BufferedDocs.WriteBytes(bytes.Bytes, bytes.Offset, bytes.Length);
            }
            else if (@string != null)
            {
                BufferedDocs.WriteString(field.GetStringValue());
            }
            else
            {
                if (number is string)
                {
                    string numStr = number.ToString();
                    sbyte dummySbyte;
                    short dummyShort;
                    int dummyInt;
                    long dummyLong;
                    float dummyFloat;
                    double dummyDouble;
                    if (sbyte.TryParse(numStr, out dummySbyte) || short.TryParse(numStr, out dummyShort) ||
                        int.TryParse(numStr, out dummyInt))
                    {
                        bits = NUMERIC_INT;
                    }
                    else if (long.TryParse(numStr, out dummyLong))
                    {
                        bits = NUMERIC_LONG;
                    }
                    else if (float.TryParse(numStr, out dummyFloat))
                    {
                        bits = NUMERIC_FLOAT;
                    }
                    else if (double.TryParse(numStr, out dummyDouble))
                    {
                        bits = NUMERIC_DOUBLE;
                    }
                    else
                    {
                        throw new System.ArgumentException("cannot store numeric type " + number.GetType());
                    }
                }
                else
                {
                    if (number is sbyte || number is short || number is int)
                    {
                        BufferedDocs.WriteInt((int)number);
                    }
                    else if (number is long)
                    {
                        BufferedDocs.WriteLong((long)number);
                    }
                    else if (number is float)
                    {
                        BufferedDocs.WriteInt(Number.FloatToIntBits((float)number));
                    }
                    else if (number is double)
                    {
                        BufferedDocs.WriteLong(BitConverter.DoubleToInt64Bits((double)number));
                    }
                    else
                    {
                        throw new Exception("Cannot get here");
                    }
                }
            }
        }

        public override void Abort()
        {
            IOUtils.CloseWhileHandlingException(this);
            IOUtils.DeleteFilesIgnoringExceptions(Directory, IndexFileNames.SegmentFileName(Segment, SegmentSuffix, Lucene40StoredFieldsWriter.FIELDS_EXTENSION), IndexFileNames.SegmentFileName(Segment, SegmentSuffix, Lucene40StoredFieldsWriter.FIELDS_INDEX_EXTENSION));
        }

        public override void Finish(FieldInfos fis, int numDocs)
        {
            if (NumBufferedDocs > 0)
            {
                Flush();
            }
            else
            {
                Debug.Assert(BufferedDocs.Length == 0);
            }
            if (DocBase != numDocs)
            {
                throw new Exception("Wrote " + DocBase + " docs, finish called with numDocs=" + numDocs);
            }
            IndexWriter.Finish(numDocs, FieldsStream.FilePointer);
            CodecUtil.WriteFooter(FieldsStream);
            Debug.Assert(BufferedDocs.Length == 0);
        }

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
                    if (fieldsReader != null && fieldsReader is CompressingStoredFieldsReader)
                    {
                        matchingFieldsReader = (CompressingStoredFieldsReader)fieldsReader;
                    }
                }

                int maxDoc = reader.MaxDoc;
                Bits liveDocs = reader.LiveDocs;

                if (matchingFieldsReader == null || matchingFieldsReader.Version != VERSION_CURRENT || matchingFieldsReader.CompressionMode != CompressionMode || matchingFieldsReader.ChunkSize != ChunkSize) // the way data is decompressed depends on the chunk size -  means reader version is not the same as the writer version
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
                        int[] startOffsets = new int[0];
                        do
                        {
                            // go to the next chunk that contains docID
                            it.Next(docID);
                            // transform lengths into offsets
                            if (startOffsets.Length < it.ChunkDocs)
                            {
                                startOffsets = new int[ArrayUtil.Oversize(it.ChunkDocs, 4)];
                            }
                            for (int i = 1; i < it.ChunkDocs; ++i)
                            {
                                startOffsets[i] = startOffsets[i - 1] + it.Lengths[i - 1];
                            }

                            if (NumBufferedDocs == 0 && startOffsets[it.ChunkDocs - 1] < ChunkSize && startOffsets[it.ChunkDocs - 1] + it.Lengths[it.ChunkDocs - 1] >= ChunkSize && NextDeletedDoc(it.DocBase, liveDocs, it.DocBase + it.ChunkDocs) == it.DocBase + it.ChunkDocs) // no deletion in the chunk -  chunk is large enough -  chunk is small enough -  starting a new chunk
                            {
                                Debug.Assert(docID == it.DocBase);

                                // no need to decompress, just copy data
                                IndexWriter.WriteIndex(it.ChunkDocs, FieldsStream.FilePointer);
                                WriteHeader(this.DocBase, it.ChunkDocs, it.NumStoredFields, it.Lengths);
                                it.CopyCompressedData(FieldsStream);
                                this.DocBase += it.ChunkDocs;
                                docID = NextLiveDoc(it.DocBase + it.ChunkDocs, liveDocs, maxDoc);
                                docCount += it.ChunkDocs;
                                mergeState.CheckAbort.Work(300 * it.ChunkDocs);
                            }
                            else
                            {
                                // decompress
                                it.Decompress();
                                if (startOffsets[it.ChunkDocs - 1] + it.Lengths[it.ChunkDocs - 1] != it.Bytes.Length)
                                {
                                    throw new CorruptIndexException("Corrupted: expected chunk size=" + startOffsets[it.ChunkDocs - 1] + it.Lengths[it.ChunkDocs - 1] + ", got " + it.Bytes.Length);
                                }
                                // copy non-deleted docs
                                for (; docID < it.DocBase + it.ChunkDocs; docID = NextLiveDoc(docID + 1, liveDocs, maxDoc))
                                {
                                    int diff = docID - it.DocBase;
                                    StartDocument(it.NumStoredFields[diff]);
                                    BufferedDocs.WriteBytes(it.Bytes.Bytes, it.Bytes.Offset + startOffsets[diff], it.Lengths[diff]);
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

        private static int NextLiveDoc(int doc, Bits liveDocs, int maxDoc)
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

        private static int NextDeletedDoc(int doc, Bits liveDocs, int maxDoc)
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