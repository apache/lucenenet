using Lucene.Net.Codecs.Lucene40;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Support;
using Lucene.Net.Util;
using Lucene.Net.Util.Packed;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Codecs.Compressing
{
    public sealed class CompressingStoredFieldsWriter : StoredFieldsWriter
    {
        internal const int MAX_DOCUMENTS_PER_CHUNK = 128;

        internal const int STRING = 0x00;
        internal const int BYTE_ARR = 0x01;
        internal const int NUMERIC_INT = 0x02;
        internal const int NUMERIC_FLOAT = 0x03;
        internal const int NUMERIC_LONG = 0x04;
        internal const int NUMERIC_DOUBLE = 0x05;

        internal static readonly int TYPE_BITS = PackedInts.BitsRequired(NUMERIC_DOUBLE);
        internal static readonly int TYPE_MASK = (int)PackedInts.MaxValue(TYPE_BITS);

        internal const String CODEC_SFX_IDX = "Index";
        internal const String CODEC_SFX_DAT = "Data";
        internal const int VERSION_START = 0;
        internal const int VERSION_CURRENT = VERSION_START;

        private readonly Directory directory;
        private readonly string segment;
        private readonly string segmentSuffix;
        private CompressingStoredFieldsIndexWriter indexWriter;
        private IndexOutput fieldsStream;

        private readonly CompressionMode compressionMode;
        private readonly Compressor compressor;
        private readonly int chunkSize;

        private readonly GrowableByteArrayDataOutput bufferedDocs;
        private int[] numStoredFields; // number of stored fields
        private int[] endOffsets; // end offsets in bufferedDocs
        private int docBase; // doc ID at the beginning of the chunk
        private int numBufferedDocs; // docBase + numBufferedDocs == current doc ID

        public CompressingStoredFieldsWriter(Directory directory, SegmentInfo si, string segmentSuffix, IOContext context, string formatName, CompressionMode compressionMode, int chunkSize)
        {
            this.directory = directory;
            this.segment = si.name;
            this.segmentSuffix = segmentSuffix;
            this.compressionMode = compressionMode;
            this.compressor = compressionMode.newCompressor();
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

                indexWriter = new CompressingStoredFieldsIndexWriter(indexStream);
                indexStream = null;

                fieldsStream.WriteVInt(PackedInts.VERSION_CURRENT);

                success = true;
            }
            finally
            {
                if (!success)
                {
                    IOUtils.CloseWhileHandlingException((IDisposable)indexStream);
                    Abort();
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            try
            {
                IOUtils.Close(fieldsStream, indexWriter);
            }
            finally
            {
                fieldsStream = null;
                indexWriter = null;
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

        public override void FinishDocument()
        {
            endOffsets[numBufferedDocs - 1] = bufferedDocs.Length;
            if (TriggerFlush())
            {
                Flush();
            }
        }

        private static void SaveInts(int[] values, int length, DataOutput output)
        {
            if (length == 1)
            {
                output.WriteVInt(values[0]);
            }
            else
            {
                bool allEqual = true;
                for (int i = 1; i < length; ++i)
                {
                    if (values[i] != values[0])
                    {
                        allEqual = false;
                        //break;
                    }
                }
                if (allEqual)
                {
                    output.WriteVInt(0);
                    output.WriteVInt(values[0]);
                }
                else
                {
                    long max = 0;
                    for (int i = 0; i < length; ++i)
                    {
                        max |= values[i];
                    }
                    int bitsRequired = PackedInts.BitsRequired(max);
                    output.WriteVInt(bitsRequired);
                    PackedInts.Writer w = PackedInts.GetWriterNoHeader(output, PackedInts.Format.PACKED, length, bitsRequired, 1);
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
            fieldsStream.WriteVInt(docBase);
            fieldsStream.WriteVInt(numBufferedDocs);

            // save numStoredFields
            SaveInts(numStoredFields, numBufferedDocs, fieldsStream);

            // save lengths
            SaveInts(lengths, numBufferedDocs, fieldsStream);
        }

        private bool TriggerFlush()
        {
            return bufferedDocs.Length >= chunkSize || // chunks of at least chunkSize bytes
                numBufferedDocs >= MAX_DOCUMENTS_PER_CHUNK;
        }

        private void Flush()
        {
            indexWriter.WriteIndex(numBufferedDocs, fieldsStream.FilePointer);

            // transform end offsets into lengths
            int[] lengths = endOffsets;
            for (int i = numBufferedDocs - 1; i > 0; --i)
            {
                lengths[i] = endOffsets[i] - endOffsets[i - 1];
            }

            WriteHeader(docBase, numBufferedDocs, numStoredFields, lengths);

            // compress stored fields to fieldsStream
            compressor.Compress(bufferedDocs.Bytes, 0, bufferedDocs.Length, fieldsStream);

            // reset
            docBase += numBufferedDocs;
            numBufferedDocs = 0;
            bufferedDocs.Length = 0;
        }

        public override void WriteField(FieldInfo info, IIndexableField field)
        {
          int bits = 0;
          BytesRef bytes;
          string str;

          object number = field.NumericValue;
          if (number != null) {
            if (number is byte || number is sbyte || number is short || number is int) {
              bits = NUMERIC_INT;
            } else if (number is long) {
              bits = NUMERIC_LONG;
            } else if (number is float) {
              bits = NUMERIC_FLOAT;
            } else if (number is double) {
              bits = NUMERIC_DOUBLE;
            } else {
              throw new ArgumentException("cannot store numeric type " + number.GetType());
            }
            str = null;
            bytes = null;
          } else {
            bytes = field.BinaryValue;
            if (bytes != null) {
              bits = BYTE_ARR;
              str = null;
            } else {
              bits = STRING;
              str = field.StringValue;
              if (str == null) {
                throw new ArgumentException("field " + field.Name + " is stored but does not have binaryValue, stringValue nor numericValue");
              }
            }
          }

          long infoAndBits = (((long) info.number) << TYPE_BITS) | bits;
          bufferedDocs.WriteVLong(infoAndBits);

          if (bytes != null) {
            bufferedDocs.WriteVInt(bytes.length);
            bufferedDocs.WriteBytes(bytes.bytes, bytes.offset, bytes.length);
          } else if (str != null) {
            bufferedDocs.WriteString(field.StringValue);
          } else {
            if (number is byte || number is sbyte || number is short || number is int) {
              bufferedDocs.WriteInt((int)number);
            } else if (number is long) {
              bufferedDocs.WriteLong((long)number);
            } else if (number is float) {
              bufferedDocs.WriteInt(Number.FloatToIntBits((float)number));
            } else if (number is double) {
              bufferedDocs.WriteLong(BitConverter.DoubleToInt64Bits((double)number));
            } else {
              throw new InvalidOperationException("Cannot get here");
            }
          }
        }

        public override void Abort()
        {
            IOUtils.CloseWhileHandlingException((IDisposable)this);
            IOUtils.DeleteFilesIgnoringExceptions(directory,
                IndexFileNames.SegmentFileName(segment, segmentSuffix, Lucene40StoredFieldsWriter.FIELDS_EXTENSION),
                IndexFileNames.SegmentFileName(segment, segmentSuffix, Lucene40StoredFieldsWriter.FIELDS_INDEX_EXTENSION));
        }

        public override void Finish(FieldInfos fis, int numDocs)
        {
            if (numBufferedDocs > 0)
            {
                Flush();
            }
            else
            {
                //assert bufferedDocs.length == 0;
            }
            if (docBase != numDocs)
            {
                throw new SystemException("Wrote " + docBase + " docs, finish called with numDocs=" + numDocs);
            }
            indexWriter.Finish(numDocs);
        }

        public override int Merge(MergeState mergeState)
        {
            int docCount = 0;
            int idx = 0;

            foreach (AtomicReader reader in mergeState.readers)
            {
                SegmentReader matchingSegmentReader = mergeState.matchingSegmentReaders[idx++];
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
                IBits liveDocs = reader.LiveDocs;

                if (matchingFieldsReader == null)
                {
                    // naive merge...
                    for (int i = NextLiveDoc(0, liveDocs, maxDoc); i < maxDoc; i = NextLiveDoc(i + 1, liveDocs, maxDoc))
                    {
                        Document doc = reader.Document(i);
                        AddDocument(doc, mergeState.fieldInfos);
                        ++docCount;
                        mergeState.checkAbort.Work(300);
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
                            if (startOffsets.Length < it.chunkDocs)
                            {
                                startOffsets = new int[ArrayUtil.Oversize(it.chunkDocs, 4)];
                            }
                            for (int i = 1; i < it.chunkDocs; ++i)
                            {
                                startOffsets[i] = startOffsets[i - 1] + it.lengths[i - 1];
                            }

                            if (compressionMode == matchingFieldsReader.CompressionMode // same compression mode
                                && numBufferedDocs == 0 // starting a new chunk
                                && startOffsets[it.chunkDocs - 1] < chunkSize // chunk is small enough
                                && startOffsets[it.chunkDocs - 1] + it.lengths[it.chunkDocs - 1] >= chunkSize // chunk is large enough
                                && NextDeletedDoc(it.docBase, liveDocs, it.docBase + it.chunkDocs) == it.docBase + it.chunkDocs)
                            { // no deletion in the chunk

                                // no need to decompress, just copy data
                                indexWriter.WriteIndex(it.chunkDocs, fieldsStream.FilePointer);
                                WriteHeader(this.docBase, it.chunkDocs, it.numStoredFields, it.lengths);
                                it.CopyCompressedData(fieldsStream);
                                this.docBase += it.chunkDocs;
                                docID = NextLiveDoc(it.docBase + it.chunkDocs, liveDocs, maxDoc);
                                docCount += it.chunkDocs;
                                mergeState.checkAbort.Work(300 * it.chunkDocs);
                            }
                            else
                            {
                                // decompress
                                it.Decompress();
                                if (startOffsets[it.chunkDocs - 1] + it.lengths[it.chunkDocs - 1] != it.bytes.length)
                                {
                                    throw new CorruptIndexException("Corrupted: expected chunk size=" + startOffsets[it.chunkDocs - 1] + it.lengths[it.chunkDocs - 1] + ", got " + it.bytes.length);
                                }
                                // copy non-deleted docs
                                for (; docID < it.docBase + it.chunkDocs; docID = NextLiveDoc(docID + 1, liveDocs, maxDoc))
                                {
                                    int diff = docID - it.docBase;
                                    StartDocument(it.numStoredFields[diff]);
                                    bufferedDocs.WriteBytes(it.bytes.bytes, it.bytes.offset + startOffsets[diff], it.lengths[diff]);
                                    FinishDocument();
                                    ++docCount;
                                    mergeState.checkAbort.Work(300);
                                }
                            }
                        } while (docID < maxDoc);
                    }
                }
            }

            Finish(mergeState.fieldInfos, docCount);
            return docCount;
        }

        private static int NextLiveDoc(int doc, IBits liveDocs, int maxDoc)
        {
            if (liveDocs == null)
            {
                return doc;
            }
            while (doc < maxDoc && !liveDocs[doc])
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
            while (doc < maxDoc && liveDocs[doc])
            {
                ++doc;
            }
            return doc;
        }

    }
}
