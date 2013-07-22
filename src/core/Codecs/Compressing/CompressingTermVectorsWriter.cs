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
    public sealed class CompressingTermVectorsWriter: TermVectorsWriter
    {
        public static int MAX_DOCUMENTS_PER_CHUNK = 128;

        static string VECTORS_EXTENSION = "tvd";
        static string VECTORS_INDEX_EXTENSION = "tvx";

        static string CODEC_SFX_IDX = "Index";
        static string CODEC_SFX_DAT = "Data";

        static int VERSION_START = 0;
        static int VERSION_CURRENT = VERSION_START;

        static int BLOCK_SIZE = 64;

        static int POSITIONS = 0x01;
        static int   OFFSETS = 0x02;
        static int  PAYLOADS = 0x04;
        static int FLAGS_BITS = PackedInts.BitsRequired(POSITIONS | OFFSETS | PAYLOADS);

        private Directory directory;
        private string segment;
        private string segmentSuffix;
        private CompressingStoredFieldsIndexWriter indexWriter;
        private IndexOutput vectorsStream;

        private CompressionMode compressionMode;
        private Compressor compressor;
        private int chunkSize;

        private int numDocs; // total number of docs seen
        private Deque<DocData> pendingDocs; // pending docs
        private DocData curDoc; // current document
        private FieldData curField; // current field
        private BytesRef lastTerm;
        private int[] positionsBuf, startOffsetsBuf, lengthsBuf, payloadLengthsBuf;
        private GrowableByteArrayDataOutput termSuffixes; // buffered term suffixes
        private GrowableByteArrayDataOutput payloadBytes; // buffered term payloads
        private BlockPackedWriter writer;
        
        /** a pending doc */
        private class DocData 
        {
            int numFields;
            Deque<FieldData> fields;
            int posStart, offStart, payStart;
            DocData(int numFields, int posStart, int offStart, int payStart) {
                this.numFields = numFields;
                this.fields = new ArrayDeque<FieldData>(numFields);
                this.posStart = posStart;
                this.offStart = offStart;
                this.payStart = payStart;
            }

            FieldData addField(int fieldNum, int numTerms, bool positions, bool offsets, bool payloads) 
            {
                FieldData field;
                if (fields.isEmpty()) 
                {
                    field = new FieldData(fieldNum, numTerms, positions, offsets, payloads, posStart, offStart, payStart);
                } 
                else 
                {
                    FieldData last = fields.getLast();
                    int posStart = last.posStart + (last.hasPositions ? last.totalPositions : 0);
                    int offStart = last.offStart + (last.hasOffsets ? last.totalPositions : 0);
                    int payStart = last.payStart + (last.hasPayloads ? last.totalPositions : 0);
                    field = new FieldData(fieldNum, numTerms, positions, offsets, payloads, posStart, offStart, payStart);
                }
                fields.add(field);
                return field;
            }
        }

        private DocData addDocData(int numVectorFields) 
        {
            FieldData last = null;
            for (Iterator<DocData> it = pendingDocs.descendingIterator(); it.hasNext(); ) 
            {
                final DocData doc = it.next();
                if (!doc.fields.isEmpty()) 
                {
                    last = doc.fields.getLast();
                    break;
                }
            }

            DocData doc;
            if (last == null) 
            {
                doc = new DocData(numVectorFields, 0, 0, 0);
            } 
            else 
            {
                int posStart = last.posStart + (last.hasPositions ? last.totalPositions : 0);
                int offStart = last.offStart + (last.hasOffsets ? last.totalPositions : 0);
                int payStart = last.payStart + (last.hasPayloads ? last.totalPositions : 0);
                doc = new DocData(numVectorFields, posStart, offStart, payStart);
            }
            pendingDocs.add(doc);
            return doc;
        }

        /** a pending field */
        private class FieldData 
        {
            bool hasPositions, hasOffsets, hasPayloads;
            int fieldNum, flags, numTerms;
            int[] freqs, prefixLengths, suffixLengths;
            int posStart, offStart, payStart;
            int totalPositions;
            int ord;

            public FieldData(int fieldNum, int numTerms, bool positions, bool offsets, bool payloads, int posStart, int offStart, int payStart) 
            {
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

            public void addTerm(int freq, int prefixLength, int suffixLength) 
            {
              freqs[ord] = freq;
              prefixLengths[ord] = prefixLength;
              suffixLengths[ord] = suffixLength;
              ++ord;
            }
            
            public void addPosition(int position, int startOffset, int length, int payloadLength) 
            {
              if (hasPositions) 
              {
                if (posStart + totalPositions == positionsBuf.length) 
                {
                  positionsBuf = ArrayUtil.grow(positionsBuf);
                }

                positionsBuf[posStart + totalPositions] = position;
              }
              if (hasOffsets) {
                if (offStart + totalPositions == startOffsetsBuf.length) 
                {
                  int newLength = ArrayUtil.Oversize(offStart + totalPositions, 4);
                  startOffsetsBuf = Arrays.CopyOf(startOffsetsBuf, newLength);
                  lengthsBuf = Arrays.CopyOf(lengthsBuf, newLength);
                }
                startOffsetsBuf[offStart + totalPositions] = startOffset;
                lengthsBuf[offStart + totalPositions] = length;
              }
              if (hasPayloads) {
                if (payStart + totalPositions == payloadLengthsBuf.length) {
                  payloadLengthsBuf = ArrayUtil.Grow(payloadLengthsBuf);
                }
                payloadLengthsBuf[payStart + totalPositions] = payloadLength;
              }
              ++totalPositions;
            }
        }

        /** Sole constructor. */
        public CompressingTermVectorsWriter(Directory directory, SegmentInfo si, string segmentSuffix, IOContext context,
            String formatName, CompressionMode compressionMode, int chunkSize) 
        {
            this.directory = directory;
            this.segment = si.name;
            this.segmentSuffix = segmentSuffix;
            this.compressionMode = compressionMode;
            this.compressor = compressionMode.newCompressor();
            this.chunkSize = chunkSize;

            numDocs = 0;
            pendingDocs = new ArrayDeque<DocData>();
            termSuffixes = new GrowableByteArrayDataOutput(ArrayUtil.Oversize(chunkSize, 1));
            payloadBytes = new GrowableByteArrayDataOutput(ArrayUtil.Oversize(1, 1));
            lastTerm = new BytesRef(ArrayUtil.Oversize(30, 1));

            bool success = false;
            IndexOutput indexStream = directory.CreateOutput(IndexFileNames.SegmentFileName(segment, segmentSuffix, VECTORS_INDEX_EXTENSION), context);
            try {
                vectorsStream = directory.CreateOutput(IndexFileNames.SegmentFileName(segment, segmentSuffix, VECTORS_EXTENSION), context);

                string codecNameIdx = formatName + CODEC_SFX_IDX;
                string codecNameDat = formatName + CODEC_SFX_DAT;
                CodecUtil.writeHeader(indexStream, codecNameIdx, VERSION_CURRENT);
                CodecUtil.writeHeader(vectorsStream, codecNameDat, VERSION_CURRENT);

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
            } finally {
                if (!success) {
                IOUtils.CloseWhileHandlingException(indexStream);
                Abort();
                }
            }
        }

        public override void StartDocument(int numVectorFields)
        {
            curDoc = addDocData(numVectorFields);
        }

        public override void FinishDocument() 
        {
            // append the payload bytes of the doc after its terms
            termSuffixes.WriteBytes(payloadBytes.Bytes, payloadBytes.Length);
            payloadBytes.Length = 0;
            ++numDocs;
            if (triggerFlush()) {
              Flush();
            }
            curDoc = null;
        }

        public override void FinishField()
        {
            curField = null;
        }

        public override void StartField(Index.FieldInfo info, int numTerms, bool positions, bool offsets, bool payloads)
        {
            curField = curDoc.AddField(info.number, numTerms, positions, offsets, payloads);
            lastTerm.length = 0;
        }

        public override void StartTerm(Util.BytesRef term, int freq)
        {
            int prefix = StringHelper.BytesDifference(lastTerm, term);
            curField.addTerm(freq, prefix, term.length - prefix);
            termSuffixes.WriteBytes(term.bytes, term.offset + prefix, term.length - prefix);
            // copy last term
            if (lastTerm.bytes.Length < term.length) {
              lastTerm.bytes = new sbyte[ArrayUtil.Oversize(term.length, 1)];
            }
            lastTerm.offset = 0;
            lastTerm.length = term.length;
            Array.Copy(term.bytes, term.offset, lastTerm.bytes, 0, term.length);
        }

        public override void AddPosition(int position, int startOffset, int endOffset, Util.BytesRef payload)
        {
            curField.addPosition(position, startOffset, endOffset - startOffset, payload == null ? 0 : payload.length);
            if (curField.HasPayloads && payload != null)
            {
                payloadBytes.WriteBytes(payload.bytes, payload.offset, payload.length);
            }
        }

        private bool triggerFlush()
        {
            return termSuffixes.Length >= chunkSize
                || pendingDocs.size() >= MAX_DOCUMENTS_PER_CHUNK;
        }

        private void flush() 
        {
            int chunkDocs = pendingDocs.size();

            // write the index file
            indexWriter.WriteIndex(chunkDocs, vectorsStream.GetFilePointer());

            int docBase = numDocs - chunkDocs;
            vectorsStream.WriteVInt(docBase);
            vectorsStream.WriteVInt(chunkDocs);

            // total number of fields of the chunk
            int totalFields = flushNumFields(chunkDocs);

            if (totalFields > 0) {
              // unique field numbers (sorted)
              int[] fieldNums = flushFieldNums();
              // offsets in the array of unique field numbers
              flushFields(totalFields, fieldNums);
              // flags (does the field have positions, offsets, payloads?)
              flushFlags(totalFields, fieldNums);
              // number of terms of each field
              flushNumTerms(totalFields);
              // prefix and suffix lengths for each field
              flushTermLengths();
              // term freqs - 1 (because termFreq is always >=1) for each term
              flushTermFreqs();
              // positions for all terms, when enabled
              flushPositions();
              // offsets for all terms, when enabled
              flushOffsets(fieldNums);
              // payload lengths for all terms, when enabled
              flushPayloadLengths();

              // compress terms and payloads and write them to the output
              compressor.Compress(termSuffixes.Bytes, 0, termSuffixes.Length, vectorsStream);
            }

            // reset
            pendingDocs.clear();
            curDoc = null;
            curField = null;
            termSuffixes.Length = 0;
        }

        private int flushNumFields(int chunkDocs) 
        {
            if (chunkDocs == 1) {
              int numFields = pendingDocs.getFirst().numFields;
              vectorsStream.WriteVInt(numFields);
              return numFields;
            } else {
              writer.Reset(vectorsStream);
              int totalFields = 0;
              for (DocData dd : pendingDocs) {
                writer.Add(dd.numFields);
                totalFields += dd.numFields;
              }
              writer.Finish();
              return totalFields;
            }
        }

          /** Returns a sorted array containing unique field numbers */
        private int[] flushFieldNums()
        {
            SortedSet<int> fieldNums = new TreeSet<int>();
            for (DocData dd : pendingDocs) {
                for (FieldData fd : dd.fields) {
                fieldNums.Add(fd.fieldNum);
                }
            }

            int numDistinctFields = fieldNums.size();
            int bitsRequired = PackedInts.bitsRequired(fieldNums.Last());
            int token = (Math.Min(numDistinctFields - 1, 0x07) << 5) | bitsRequired;
            vectorsStream.WriteByte((byte) token);
            if (numDistinctFields - 1 >= 0x07) {
                vectorsStream.WriteVInt(numDistinctFields - 1 - 0x07);
            }
            PackedInts.Writer writer = PackedInts.getWriterNoHeader(vectorsStream, PackedInts.Format.PACKED, fieldNums.size(), bitsRequired, 1);
            for (int fieldNum : fieldNums) {
                writer.Add(fieldNum);
            }
            writer.Finish();

            int[] fns = new int[fieldNums.size()];
            int i = 0;
            for (int key : fieldNums) {
                fns[i++] = key;
            }
            return fns;
        }

        private void flushFields(int totalFields, int[] fieldNums) throws IOException {
            final PackedInts.Writer writer = PackedInts.getWriterNoHeader(vectorsStream, PackedInts.Format.PACKED, totalFields, PackedInts.bitsRequired(fieldNums.length - 1), 1);
            for (DocData dd : pendingDocs) {
                for (FieldData fd : dd.fields) {
                final int fieldNumIndex = Arrays.binarySearch(fieldNums, fd.fieldNum);
                assert fieldNumIndex >= 0;
                writer.add(fieldNumIndex);
                }
            }
            writer.finish();
        }

        private void flushFlags(int totalFields, int[] fieldNums) 
        {
            // check if fields always have the same flags
            bool nonChangingFlags = true;
            int[] fieldFlags = new int[fieldNums.Length];
            Arrays.Fill(fieldFlags, -1);
            outer:
            for (DocData dd : pendingDocs) {
                for (FieldData fd : dd.fields) {
                int fieldNumOff = Arrays.BinarySearch(fieldNums, fd.ieldNum);
                if (fieldFlags[fieldNumOff] == -1) {
                    fieldFlags[fieldNumOff] = fd.flags;
                } else if (fieldFlags[fieldNumOff] != fd.flags) {
                    nonChangingFlags = false;
                    break outer;
                }
                }
            }

            if (nonChangingFlags) {
                // write one flag per field num
                vectorsStream.WriteVInt(0);
                PackedInts.Writer writer = PackedInts.GetWriterNoHeader(vectorsStream, PackedInts.Format.PACKED, fieldFlags.length, FLAGS_BITS, 1);
                for (int flags : fieldFlags) {
                writer.Add(flags);
                }
                writer.Finish();
            } else {
                // write one flag for every field instance
                vectorsStream.WriteVInt(1);
                PackedInts.Writer writer = PackedInts.GetWriterNoHeader(vectorsStream, PackedInts.Format.PACKED, totalFields, FLAGS_BITS, 1);
                for (DocData dd : pendingDocs) {
                for (FieldData fd : dd.fields) {
                    writer.add(fd.flags);
                }
                }
                writer.Finish();
            }
        }

        private void flushNumTerms(int totalFields) 
        {
            int maxNumTerms = 0;
            for (DocData dd : pendingDocs) {
                for (FieldData fd : dd.fields) {
                maxNumTerms |= fd.numTerms;
                }
            }
            
            int bitsRequired = PackedInts.bitsRequired(maxNumTerms);
            vectorsStream.WriteVInt(bitsRequired);
            PackedInts.Writer writer = PackedInts.getWriterNoHeader(
                vectorsStream, PackedInts.Format.PACKED, totalFields, bitsRequired, 1);
            for (DocData dd : pendingDocs) {
                for (FieldData fd : dd.fields) {
                writer.add(fd.numTerms);
                }
            }
            writer.finish();
        }

        private void flushTermLengths() 
        {
            writer.reset(vectorsStream);
            for (DocData dd : pendingDocs) {
                for (FieldData fd : dd.fields) {
                for (int i = 0; i < fd.numTerms; ++i) {
                    writer.add(fd.prefixLengths[i]);
                }
                }
            }
            writer.finish();
            writer.reset(vectorsStream);
            for (DocData dd : pendingDocs) {
                for (FieldData fd : dd.fields) {
                for (int i = 0; i < fd.numTerms; ++i) {
                    writer.add(fd.suffixLengths[i]);
                }
                }
            }
            writer.finish();
        }

        private void flushTermFreqs() 
        {
            writer.reset(vectorsStream);
            for (DocData dd : pendingDocs) {
                for (FieldData fd : dd.fields) {
                for (int i = 0; i < fd.numTerms; ++i) {
                    writer.add(fd.freqs[i] - 1);
                }
                }
            }
            writer.finish();
        }

        private void flushPositions()
        {
            writer.reset(vectorsStream);
            for (DocData dd : pendingDocs) {
                for (FieldData fd : dd.fields) {
                if (fd.hasPositions) {
                    int pos = 0;
                    for (int i = 0; i < fd.numTerms; ++i) {
                    int previousPosition = 0;
                    for (int j = 0; j < fd.freqs[i]; ++j) {
                        int position = positionsBuf[fd .posStart + pos++];
                        writer.add(position - previousPosition);
                        previousPosition = position;
                    }
                    }
                }
                }
            }
            writer.finish();
        }

        private void flushOffsets(int[] fieldNums) 
        {
            bool hasOffsets = false;
            long[] sumPos = new long[fieldNums.length];
            long[] sumOffsets = new long[fieldNums.length];
            for (DocData dd : pendingDocs) {
                for (FieldData fd : dd.fields) {
                hasOffsets |= fd.hasOffsets;
                if (fd.hasOffsets && fd.hasPositions) {
                    int fieldNumOff = Arrays.binarySearch(fieldNums, fd.fieldNum);
                    int pos = 0;
                    for (int i = 0; i < fd.numTerms; ++i) {
                    int previousPos = 0;
                    int previousOff = 0;
                    for (int j = 0; j < fd.freqs[i]; ++j) {
                        int position = positionsBuf[fd.posStart + pos];
                        int startOffset = startOffsetsBuf[fd.offStart + pos];
                        sumPos[fieldNumOff] += position - previousPos;
                        sumOffsets[fieldNumOff] += startOffset - previousOff;
                        previousPos = position;
                        previousOff = startOffset;
                        ++pos;
                    }
                    }
                }
                }
            }

            if (!hasOffsets) {
                // nothing to do
                return;
            }

            float[] charsPerTerm = new float[fieldNums.length];
            for (int i = 0; i < fieldNums.length; ++i) {
                charsPerTerm[i] = (sumPos[i] <= 0 || sumOffsets[i] <= 0) ? 0 : (float) ((double) sumOffsets[i] / sumPos[i]);
            }

            // start offsets
            for (int i = 0; i < fieldNums.length; ++i) {
                vectorsStream.writeInt(Float.floatToRawIntBits(charsPerTerm[i]));
            }

            writer.reset(vectorsStream);
            for (DocData dd : pendingDocs) {
                for (FieldData fd : dd.fields) {
                if ((fd.flags & OFFSETS) != 0) {
                    int fieldNumOff = Arrays.binarySearch(fieldNums, fd.fieldNum);
                    float cpt = charsPerTerm[fieldNumOff];
                    int pos = 0;
                    for (int i = 0; i < fd.numTerms; ++i) {
                    int previousPos = 0;
                    int previousOff = 0;
                    for (int j = 0; j < fd.freqs[i]; ++j) {
                        final int position = fd.hasPositions ? positionsBuf[fd.posStart + pos] : 0;
                        final int startOffset = startOffsetsBuf[fd.offStart + pos];
                        writer.add(startOffset - previousOff - (int) (cpt * (position - previousPos)));
                        previousPos = position;
                        previousOff = startOffset;
                        ++pos;
                    }
                    }
                }
                }
            }
            writer.finish();

            // lengths
            writer.reset(vectorsStream);
            for (DocData dd : pendingDocs) {
                for (FieldData fd : dd.fields) {
                if ((fd.flags & OFFSETS) != 0) {
                    int pos = 0;
                    for (int i = 0; i < fd.numTerms; ++i) {
                    for (int j = 0; j < fd.freqs[i]; ++j) {
                        writer.add(lengthsBuf[fd.offStart + pos++] - fd.prefixLengths[i] - fd.suffixLengths[i]);
                    }
                    }
                }
                }
            }
            writer.finish();
        }

        private void flushPayloadLengths() 
        {
            writer.reset(vectorsStream);
            for (DocData dd : pendingDocs) {
                for (FieldData fd : dd.fields) {
                if (fd.hasPayloads) {
                    for (int i = 0; i < fd.totalPositions; ++i) {
                    writer.add(payloadLengthsBuf[fd.payStart + i]);
                    }
                }
                }
            }
            writer.finish();
        }



        public override void Abort()
        {
            IOUtils.CloseWhileHandlingException(this);
            IOUtils.DeleteFilesIgnoringExceptions(directory,
            IndexFileNames.SegmentFileName(segment, segmentSuffix, VECTORS_EXTENSION),
            IndexFileNames.SegmentFileName(segment, segmentSuffix, VECTORS_INDEX_EXTENSION));
        }

        public override void Finish(Index.FieldInfos fis, int numDocs)
        {
            if (!pendingDocs.isEmpty()) {
              flush();
            }
            if (numDocs != this.numDocs) {
              throw new RuntimeException("Wrote " + this.numDocs + " docs, finish called with numDocs=" + numDocs);
            }
            indexWriter.finish(numDocs);
        }

        public override IComparer<Util.BytesRef> Comparator
        {
            get 
            { 
                return BytesRef.getUTF8SortedAsUnicodeComparator(); 
            }
        }

        public void addProx(int numProx, DataInput positions, DataInput offsets)
        {

            if (curField.hasPositions) {
                final int posStart = curField.posStart + curField.totalPositions;
                if (posStart + numProx > positionsBuf.length) {
                positionsBuf = ArrayUtil.grow(positionsBuf, posStart + numProx);
                }
                int position = 0;
                if (curField.hasPayloads) {
                final int payStart = curField.payStart + curField.totalPositions;
                if (payStart + numProx > payloadLengthsBuf.length) {
                    payloadLengthsBuf = ArrayUtil.grow(payloadLengthsBuf, payStart + numProx);
                }
                for (int i = 0; i < numProx; ++i) {
                    final int code = positions.readVInt();
                    if ((code & 1) != 0) {
                    // This position has a payload
                    final int payloadLength = positions.readVInt();
                    payloadLengthsBuf[payStart + i] = payloadLength;
                    payloadBytes.copyBytes(positions, payloadLength);
                    } else {
                    payloadLengthsBuf[payStart + i] = 0;
                    }
                    position += code >>> 1;
                    positionsBuf[posStart + i] = position;
                }
                } else {
                for (int i = 0; i < numProx; ++i) {
                    position += (positions.readVInt() >>> 1);
                    positionsBuf[posStart + i] = position;
                }
                }
            }

            if (curField.hasOffsets) {
                int offStart = curField.offStart + curField.totalPositions;
                if (offStart + numProx > startOffsetsBuf.length) {
                    int newLength = ArrayUtil.oversize(offStart + numProx, 4);
                    startOffsetsBuf = Arrays.copyOf(startOffsetsBuf, newLength);
                    lengthsBuf = Arrays.copyOf(lengthsBuf, newLength);
                }
                
                int lastOffset = 0, startOffset, endOffset;
                for (int i = 0; i < numProx; ++i) {
                startOffset = lastOffset + offsets.readVInt();
                endOffset = startOffset + offsets.readVInt();
                lastOffset = endOffset;
                startOffsetsBuf[offStart + i] = startOffset;
                lengthsBuf[offStart + i] = endOffset - startOffset;
                }
            }

            curField.totalPositions += numProx;
        }

        public int merge(MergeState mergeState) 
        {
            int docCount = 0;
            int idx = 0;

            for (AtomicReader reader : mergeState.readers) 
            {
                SegmentReader matchingSegmentReader = mergeState.matchingSegmentReaders[idx++];
                CompressingTermVectorsReader matchingVectorsReader = null;
                if (matchingSegmentReader != null) {
                TermVectorsReader vectorsReader = matchingSegmentReader.getTermVectorsReader();
                // we can only bulk-copy if the matching reader is also a CompressingTermVectorsReader
                if (vectorsReader != null && vectorsReader instanceof CompressingTermVectorsReader) {
                    matchingVectorsReader = (CompressingTermVectorsReader) vectorsReader;
                }
                }

                int maxDoc = reader.maxDoc();
                Bits liveDocs = reader.getLiveDocs();

                if (matchingVectorsReader == null
                    || matchingVectorsReader.getCompressionMode() != compressionMode
                    || matchingVectorsReader.getChunkSize() != chunkSize
                    || matchingVectorsReader.getPackedIntsVersion() != PackedInts.VERSION_CURRENT) {
                // naive merge...
                for (int i = nextLiveDoc(0, liveDocs, maxDoc); i < maxDoc; i = nextLiveDoc(i + 1, liveDocs, maxDoc)) {
                    Fields vectors = reader.getTermVectors(i);
                    addAllDocVectors(vectors, mergeState);
                    ++docCount;
                    mergeState.checkAbort.work(300);
                }
                } else {
                CompressingStoredFieldsIndexReader index = matchingVectorsReader.getIndex();
                IndexInput vectorsStream = matchingVectorsReader.getVectorsStream();
                for (int i = nextLiveDoc(0, liveDocs, maxDoc); i < maxDoc; ) {
                    if (pendingDocs.isEmpty()
                        && (i == 0 || index.getStartPointer(i - 1) < index.getStartPointer(i))) { // start of a chunk
                    long startPointer = index.getStartPointer(i);
                    vectorsStream.seek(startPointer);
                    int docBase = vectorsStream.readVInt();
                    int chunkDocs = vectorsStream.readVInt();
                    if (docBase + chunkDocs < matchingSegmentReader.maxDoc()
                        && nextDeletedDoc(docBase, liveDocs, docBase + chunkDocs) == docBase + chunkDocs) {
                        long chunkEnd = index.getStartPointer(docBase + chunkDocs);
                        long chunkLength = chunkEnd - vectorsStream.getFilePointer();
                        indexWriter.writeIndex(chunkDocs, this.vectorsStream.getFilePointer());
                        this.vectorsStream.writeVInt(docCount);
                        this.vectorsStream.writeVInt(chunkDocs);
                        this.vectorsStream.copyBytes(vectorsStream, chunkLength);
                        docCount += chunkDocs;
                        this.numDocs += chunkDocs;
                        mergeState.checkAbort.work(300 * chunkDocs);
                        i = nextLiveDoc(docBase + chunkDocs, liveDocs, maxDoc);
                    } else {
                        for (; i < docBase + chunkDocs; i = nextLiveDoc(i + 1, liveDocs, maxDoc)) {
                        Fields vectors = reader.getTermVectors(i);
                        addAllDocVectors(vectors, mergeState);
                        ++docCount;
                        mergeState.checkAbort.work(300);
                        }
                    }
                    } else {
                    Fields vectors = reader.getTermVectors(i);
                    addAllDocVectors(vectors, mergeState);
                    ++docCount;
                    mergeState.checkAbort.work(300);
                    i = nextLiveDoc(i + 1, liveDocs, maxDoc);
                    }
                }
                }
            }
            finish(mergeState.fieldInfos, docCount);
            return docCount;
        }

        private static int nextLiveDoc(int doc, Bits liveDocs, int maxDoc) 
        {
            if (liveDocs == null) {
                return doc;
            }
            while (doc < maxDoc && !liveDocs.get(doc)) {
                ++doc;
            }
            return doc;
        }

        private static int nextDeletedDoc(int doc, Bits liveDocs, int maxDoc) 
        {
            if (liveDocs == null) {
                return maxDoc;
            }
            while (doc < maxDoc && liveDocs.get(doc)) {
                ++doc;
            }
            return doc;
        }

        protected override void Dispose(bool disposing)
        {
            try 
            {
                IOUtils.Close(vectorsStream, indexWriter);
            } finally {
                vectorsStream = null;
                indexWriter = null;
            }
        }
    }
}
