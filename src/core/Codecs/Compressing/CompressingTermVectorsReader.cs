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
    public sealed class CompressingTermVectorsReader: IDisposable, TermVectorsReader
    {
        private FieldInfos fieldInfos;
        CompressingStoredFieldsIndexReader indexReader;
        IndexInput vectorsStream;
        private int packedIntsVersion;
        private CompressionMode compressionMode;
        private Decompressor decompressor;
        private int chunkSize;
        private int numDocs;
        private bool closed;
        private BlockPackedReaderIterator reader;
        
        private CompressingTermVectorsReader(CompressingTermVectorsReader reader)
        {
            this.fieldInfos = reader.fieldInfos;
            this.vectorsStream = (IndexInput)reader.vectorsStream.Clone();
            this.indexReader = reader.indexReader.clone();
            this.packedIntsVersion = reader.packedIntsVersion;
            this.compressionMode = reader.compressionMode;
            this.decompressor = (Decompressor)reader.decompressor.Clone();
            this.chunkSize = reader.chunkSize;
            this.numDocs = reader.numDocs;
            this.reader = new BlockPackedReaderIterator(vectorsStream, packedIntsVersion, BLOCK_SIZE, 0);
            this.closed = false;
        }

          /** Sole constructor. */
        public CompressingTermVectorsReader(Directory d, SegmentInfo si, String segmentSuffix, FieldInfos fn,
            IOContext context, String formatName, CompressionMode compressionMode) 
        {
            this.compressionMode = compressionMode;
            string segment = si.name;
            bool success = false;
            fieldInfos = fn;
            numDocs = si.DocCount;
            IndexInput indexStream = null;
            try {
                vectorsStream = d.OpenInput(IndexFileNames.SegmentFileName(segment, segmentSuffix, VECTORS_EXTENSION), context);
                string indexStreamFN = IndexFileNames.SegmentFileName(segment, segmentSuffix, VECTORS_INDEX_EXTENSION);
                indexStream = d.OpenInput(indexStreamFN, context);

                string codecNameIdx = formatName + CODEC_SFX_IDX;
                string codecNameDat = formatName + CODEC_SFX_DAT;
                CodecUtil.CheckHeader(indexStream, codecNameIdx, VERSION_START, VERSION_CURRENT);
                CodecUtil.CheckHeader(vectorsStream, codecNameDat, VERSION_START, VERSION_CURRENT);

                indexReader = new CompressingStoredFieldsIndexReader(indexStream, si);
                indexStream = null;

                packedIntsVersion = vectorsStream.ReadVInt();
                chunkSize = vectorsStream.ReadVInt();
                decompressor = compressionMode.newDecompressor();
                this.reader = new BlockPackedReaderIterator(vectorsStream, packedIntsVersion, BLOCK_SIZE, 0);

                success = true;
            } finally {
                if (!success) {
                IOUtils.CloseWhileHandlingException(this, indexStream);
                }
            }
        }

        CompressionMode getCompressionMode() 
        {
            return compressionMode;
        }

        int getChunkSize() {
            return chunkSize;
        }

        int getPackedIntsVersion() {
            return packedIntsVersion;
        }

        CompressingStoredFieldsIndexReader getIndex() {
            return indexReader;
        }

        IndexInput getVectorsStream() {
            return vectorsStream;
        }

        /**
        * @throws AlreadyClosedException if this TermVectorsReader is closed
        */
        private void ensureOpen()
        {
            if (closed) {
                throw new AlreadyClosedException("this FieldsReader is closed");
            }
        }



        public void Dispose()
        {
            if (!closed)
            {
                IOUtils.Close(vectorsStream, indexReader);
                closed = true;
            }
        }

        public override Index.Fields Get(int doc)
        {
            ensureOpen();

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
            if (doc < docBase || doc >= docBase + chunkDocs || docBase + chunkDocs > numDocs) {
              throw new CorruptIndexException("docBase=" + docBase + ",chunkDocs=" + chunkDocs + ",doc=" + doc);
            }

            long skip; // number of fields to skip
            long numFields; // number of fields of the document we're looking for
            long totalFields; // total number of fields of the chunk (sum for all docs)
            if (chunkDocs == 1) {
              skip = 0;
              numFields = totalFields = vectorsStream.ReadVInt();
            } else {
              reader.Reset(vectorsStream, chunkDocs);
              long sum = 0;
              for (int i = docBase; i < doc; ++i) {
                sum += reader.Next();
              }
              skip = sum;
              numFields = (int) reader.Next();
              sum += numFields;
              for (int i = doc + 1; i < docBase + chunkDocs; ++i) {
                sum += reader.Next();
              }
              totalFields = sum;
            }

            if (numFields == 0) {
              // no vectors
              return null;
            }

            // read field numbers that have term vectors
            int[] fieldNums;
            {
              int token = vectorsStream.ReadByte() & 0xFF;
              int bitsPerFieldNum = token & 0x1F;
              int totalDistinctFields = Number.URShift(token, 5);
              if (totalDistinctFields == 0x07) {
                totalDistinctFields += vectorsStream.ReadVInt();
              }
              ++totalDistinctFields;
              PackedInts.ReaderIterator it = PackedInts.GetReaderIteratorNoHeader(vectorsStream, PackedInts.Format.PACKED, packedIntsVersion, totalDistinctFields, bitsPerFieldNum, 1);
              fieldNums = new int[totalDistinctFields];
              for (int i = 0; i < totalDistinctFields; ++i) {
                fieldNums[i] = (int) it.Next();
              }
            }

            // read field numbers and flags
            int[] fieldNumOffs = new int[numFields];
            PackedInts.Reader flags;
            {
              int bitsPerOff = PackedInts.BitsRequired(fieldNums.Length - 1);
              PackedInts.Reader allFieldNumOffs = PackedInts.GetReaderNoHeader(vectorsStream, PackedInts.Format.PACKED, packedIntsVersion, totalFields, bitsPerOff);
              switch (vectorsStream.ReadVInt()) {
                case 0:
                  PackedInts.Reader fieldFlags = PackedInts.getReaderNoHeader(vectorsStream, PackedInts.Format.PACKED, packedIntsVersion, fieldNums.Length, FLAGS_BITS);
                  PackedInts.Mutable f = PackedInts.GetMutable(totalFields, FLAGS_BITS, PackedInts.COMPACT);
                  for (int i = 0; i < totalFields; ++i) {
                    int fieldNumOff = (int) allFieldNumOffs.Get(i);
                    int fgs = (int) fieldFlags.Get(fieldNumOff);
                    f.Set(i, fgs);
                  }
                  flags = f;
                  break;
                case 1:
                  flags = PackedInts.GetReaderNoHeader(vectorsStream, PackedInts.Format.PACKED, packedIntsVersion, totalFields, FLAGS_BITS);
                  break;
                default:
                  throw new AssertionError();
              }
              for (int i = 0; i < numFields; ++i) {
                //hackmp - TODO - NEEDS REVIEW
                //Here again, seems to be a larger impact to change all ints to long, than simply cast.  Will need Pual to review..
                fieldNumOffs[i] = (int) allFieldNumOffs.Get((int)skip + i);
              }
            }

            // number of terms per field for all fields
            PackedInts.Reader numTerms;
            long totalTerms;
            {
              int bitsRequired = vectorsStream.ReadVInt();
              numTerms = PackedInts.GetReaderNoHeader(vectorsStream, PackedInts.Format.PACKED, packedIntsVersion, totalFields, bitsRequired);
              long sum = 0;
              for (int i = 0; i < totalFields; ++i) {
                sum += numTerms.Get(i);
              }
              totalTerms = sum;
            }

            // term lengths
            long docOff = 0, docLen = 0, totalLen;
            int[] fieldLengths = new int[numFields];
            int[][] prefixLengths = new int[numFields][];
            int[][] suffixLengths = new int[numFields][];
            {
              reader.Reset(vectorsStream, totalTerms);
              // skip
              long toSkip = 0;
              for (int i = 0; i < skip; ++i) {
                toSkip += numTerms.Get(i);
              }
              reader.Skip(toSkip);
              // read prefix lengths
              for (int i = 0; i < numFields; ++i) {
                //hackmp - TODO - NEEDS REVIEW
                //casting long to int
                long termCount = (int) numTerms.Get((int)skip + i);
                int[] fieldPrefixLengths = new int[termCount];
                prefixLengths[i] = fieldPrefixLengths;
                for (int j = 0; j < termCount; ) {
                  //hackmp - TODO - NEEDS REVIEW
                  //casting long to int..
                  LongsRef next = reader.Next((int)termCount - j);
                  for (int k = 0; k < next.length; ++k) {
                    fieldPrefixLengths[j++] = (int) next.longs[next.offset + k];
                  }
                }
              }
              reader.Skip(totalTerms - reader.Ord);

              reader.Reset(vectorsStream, totalTerms);
              // skip
              toSkip = 0;
              for (int i = 0; i < skip; ++i) {
                for (int j = 0; j < numTerms.Get(i); ++j) {
                  docOff += reader.Next();
                }
              }
              for (int i = 0; i < numFields; ++i) {
                  //HACKMP - TODO - NEEDS REVIEW
                  //..and again, casting long to int
                int termCount = (int) numTerms.Get((int)skip + i);
                int[] fieldSuffixLengths = new int[termCount];
                suffixLengths[i] = fieldSuffixLengths;
                for (int j = 0; j < termCount; ) {
                  LongsRef next = reader.Next(termCount - j);
                  for (int k = 0; k < next.length; ++k) {
                    fieldSuffixLengths[j++] = (int) next.longs[next.offset + k];
                  }
                }
                fieldLengths[i] = sum(suffixLengths[i]);
                docLen += fieldLengths[i];
              }     
              totalLen = docOff + docLen;
              for (long i = skip + numFields; i < totalFields; ++i) {
                  //hackmp - TODO - NEEDS REVIEW
                  //long > int
                for (int j = 0; j < numTerms.Get((int)i); ++j) 
                {
                  totalLen += reader.Next();
                }
              }
            }

            // term freqs
            int[] termFreqs = new int[totalTerms];
            {
              reader.Reset(vectorsStream, totalTerms);
              for (int i = 0; i < totalTerms; ) {
                //hackmp - TODO - NEEDS REVIEW
                //long > int
                LongsRef next = reader.Next((int)totalTerms - i);
                for (int k = 0; k < next.length; ++k) {
                  termFreqs[i++] = 1 + (int) next.longs[next.offset + k];
                }
              }
            }

            // total number of positions, offsets and payloads
            int totalPositions = 0, totalOffsets = 0, totalPayloads = 0;
            for (int i = 0, termIndex = 0; i < totalFields; ++i) 
            {
              int f = (int) flags.Get(i);
              int termCount = (int) numTerms.Get(i);
              for (int j = 0; j < termCount; ++j) {
                int freq = termFreqs[termIndex++];
                if ((f & POSITIONS) != 0) {
                  totalPositions += freq;
                }
                if ((f & OFFSETS) != 0) {
                  totalOffsets += freq;
                }
                if ((f & PAYLOADS) != 0) {
                  totalPayloads += freq;
                }
              }
            }

            int[][] positionIndex = positionIndex(skip, numFields, numTerms, termFreqs);
            int[][] positions, startOffsets, lengths;
            if (totalPositions > 0) {
              positions = readPositions(skip, numFields, flags, numTerms, termFreqs, POSITIONS, totalPositions, positionIndex);
            } else {
              positions = new int[numFields][];
            }

            if (totalOffsets > 0) {
              // average number of chars per term
              float[] charsPerTerm = new float[fieldNums.Length];
              for (int i = 0; i < charsPerTerm.Length; ++i) {
                charsPerTerm[i] = Number.IntBitsToFloat(vectorsStream.ReadInt());
              }
              startOffsets = readPositions(skip, numFields, flags, numTerms, termFreqs, OFFSETS, totalOffsets, positionIndex);
              lengths = readPositions(skip, numFields, flags, numTerms, termFreqs, OFFSETS, totalOffsets, positionIndex);

              for (int i = 0; i < numFields; ++i) {
                int[] fStartOffsets = startOffsets[i];
                int[] fPositions = positions[i];
                // patch offsets from positions
                if (fStartOffsets != null && fPositions != null) {
                  float fieldCharsPerTerm = charsPerTerm[fieldNumOffs[i]];
                  for (int j = 0; j < startOffsets[i].Length; ++j) {
                    fStartOffsets[j] += (int) (fieldCharsPerTerm * fPositions[j]);
                  }
                }
                if (fStartOffsets != null) {
                  int[] fPrefixLengths = prefixLengths[i];
                  int[] fSuffixLengths = suffixLengths[i];
                  int[] fLengths = lengths[i];
                    //hackmp - TODO - NEEDS REVIEW
                    //long > int
                  for (int j = 0, end = (int) numTerms.Get((int)skip + i); j < end; ++j) {
                    // delta-decode start offsets and  patch lengths using term lengths
                    int termLength = fPrefixLengths[j] + fSuffixLengths[j];
                    lengths[i][positionIndex[i][j]] += termLength;
                    for (int k = positionIndex[i][j] + 1; k < positionIndex[i][j + 1]; ++k) {
                      fStartOffsets[k] += fStartOffsets[k - 1];
                      fLengths[k] += termLength;
                    }
                  }
                }
              }
            } else {
              startOffsets = lengths = new int[numFields][];
            }
            if (totalPositions > 0) {
              // delta-decode positions
              for (int i = 0; i < numFields; ++i) {
                int[] fPositions = positions[i];
                int[] fpositionIndex = positionIndex[i];
                if (fPositions != null) {
                    //hackmp - TODO - NEED REVIEW
                    //long > int
                  for (int j = 0, end = (int) numTerms.Get((int)skip + i); j < end; ++j) {
                    // delta-decode start offsets
                    for (int k = fpositionIndex[j] + 1; k < fpositionIndex[j + 1]; ++k) {
                      fPositions[k] += fPositions[k - 1];
                    }
                  }
                }
              }
            }

            // payload lengths
            int[][] payloadIndex = new int[numFields][];
            long totalPayloadLength = 0;
            int payloadOff = 0;
            int payloadLen = 0;
            if (totalPayloads > 0) {
              reader.Reset(vectorsStream, totalPayloads);
              // skip
              int termIndex = 0;
              for (int i = 0; i < skip; ++i) {
                int f = (int) flags.Get(i);
                int termCount = (int) numTerms.Get(i);
                if ((f & PAYLOADS) != 0) {
                  for (int j = 0; j < termCount; ++j) {
                    int freq = termFreqs[termIndex + j];
                    for (int k = 0; k < freq; ++k) {
                      int l = (int) reader.Next();
                      payloadOff += l;
                    }
                  }
                }
                termIndex += termCount;
              }
              totalPayloadLength = payloadOff;
              // read doc payload lengths
              for (int i = 0; i < numFields; ++i) {
                  //hackmp - TODO - NEEDS REVIEW
                  //long > int
                int f = (int) flags.Get((int)skip + i);
                int termCount = (int) numTerms.Get((int)skip + i);
                if ((f & PAYLOADS) != 0) {
                  int totalFreq = positionIndex[i][termCount];
                  payloadIndex[i] = new int[totalFreq + 1];
                  int posIdx = 0;
                  payloadIndex[i][posIdx] = payloadLen;
                  for (int j = 0; j < termCount; ++j) {
                    int freq = termFreqs[termIndex + j];
                    for (int k = 0; k < freq; ++k) {
                      int payloadLength = (int) reader.Next();
                      payloadLen += payloadLength;
                      payloadIndex[i][posIdx+1] = payloadLen;
                      ++posIdx;
                    }
                  }
                }
                termIndex += termCount;
              }
              totalPayloadLength += payloadLen;
              for (long i = skip + numFields; i < totalFields; ++i) {
                  //hackmp - TODO - NEEDS REVIEW
                  //long > int
                int f = (int) flags.Get((int)i);
                int termCount = (int) numTerms.Get((int)i);
                if ((f & PAYLOADS) != 0) {
                  for (int j = 0; j < termCount; ++j) {
                    int freq = termFreqs[termIndex + j];
                    for (int k = 0; k < freq; ++k) {
                      totalPayloadLength += reader.Next();
                    }
                  }
                }
                termIndex += termCount;
              }
            }

            // decompress data
            BytesRef suffixBytes = new BytesRef();
            //hackmp - TODO - NEEDS REVIEW
            //long > int
            decompressor.Decompress(vectorsStream, (int)totalLen + (int)totalPayloadLength, (int)docOff + (int)payloadOff, (int)docLen + payloadLen, suffixBytes);
            suffixBytes.length = (int)docLen;
            BytesRef payloadBytes = new BytesRef(suffixBytes.bytes, suffixBytes.offset + (int)docLen, payloadLen);

            int[] fieldFlags = new int[numFields];
            for (int i = 0; i < numFields; ++i) {
                //hackmp - TODO - NEEDS REVIEW
                //long > int
              fieldFlags[i] = (int) flags.Get((int)skip + i);
            }

            int[] fieldNumTerms = new int[numFields];
            for (int i = 0; i < numFields; ++i) {
                //hackmp - TODO - NEEDS REVIEW
              fieldNumTerms[i] = (int) numTerms.Get((int)skip + i);
            }

            int[][] fieldTermFreqs = new int[numFields][];
            {
              long termIdx = 0;
              for (int i = 0; i < skip; ++i) {
                termIdx += numTerms.Get(i);
              }
              for (int i = 0; i < numFields; ++i) {
                  //hackmp - TODO - NEEDS REVIEW
                  //long > int
                long termCount = (int) numTerms.Get((int)skip + i);
                fieldTermFreqs[i] = new int[termCount];
                for (int j = 0; j < termCount; ++j) {
                  fieldTermFreqs[i][j] = termFreqs[termIdx++];
                }
              }
            }

            return new TVFields(fieldNums, fieldFlags, fieldNumOffs, fieldNumTerms, fieldLengths,
                prefixLengths, suffixLengths, fieldTermFreqs,
                positionIndex, positions, startOffsets, lengths,
                payloadBytes, payloadIndex,
                suffixBytes);
        }

        public override object Clone()
        {
            return new CompressingTermVectorsReader(this);
        }

        protected override void Dispose(bool disposing)
        {
            throw new NotImplementedException();
        }
    }
}
