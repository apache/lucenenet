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
    public sealed class CompressingStoredFieldsIndexReader : ICloneable, IDisposable
    {
        internal readonly IndexInput fieldsIndexIn;

        internal static long MoveLowOrderBitToSign(long n)
        {
            return ((Number.URShift(n, 1) ^ -(n & 1)));
        }

        internal readonly int maxDoc;
        internal readonly int[] docBases;
        internal readonly long[] startPointers;
        internal readonly int[] avgChunkDocs;
        internal readonly long[] avgChunkSizes;
        internal readonly PackedInts.IReader[] docBasesDeltas; // delta from the avg
        internal readonly PackedInts.IReader[] startPointersDeltas; // delta from the avg

        public CompressingStoredFieldsIndexReader(IndexInput fieldsIndexIn, SegmentInfo si)
        {
            this.fieldsIndexIn = fieldsIndexIn;
            maxDoc = si.DocCount;
            int[] docBases = new int[16];
            long[] startPointers = new long[16];
            int[] avgChunkDocs = new int[16];
            long[] avgChunkSizes = new long[16];
            PackedInts.Reader[] docBasesDeltas = new PackedInts.Reader[16];
            PackedInts.Reader[] startPointersDeltas = new PackedInts.Reader[16];

            int packedIntsVersion = fieldsIndexIn.ReadVInt();

            int blockCount = 0;

            for (; ; )
            {
                int numChunks = fieldsIndexIn.ReadVInt();
                if (numChunks == 0)
                {
                    break;
                }

                if (blockCount == docBases.Length)
                {
                    int newSize = ArrayUtil.Oversize(blockCount + 1, 8);
                    docBases = Arrays.CopyOf(docBases, newSize);
                    startPointers = Arrays.CopyOf(startPointers, newSize);
                    avgChunkDocs = Arrays.CopyOf(avgChunkDocs, newSize);
                    avgChunkSizes = Arrays.CopyOf(avgChunkSizes, newSize);
                    docBasesDeltas = Arrays.CopyOf(docBasesDeltas, newSize);
                    startPointersDeltas = Arrays.CopyOf(startPointersDeltas, newSize);
                }

                // doc bases
                docBases[blockCount] = fieldsIndexIn.ReadVInt();
                avgChunkDocs[blockCount] = fieldsIndexIn.ReadVInt();
                int bitsPerDocBase = fieldsIndexIn.ReadVInt();
                if (bitsPerDocBase > 32)
                {
                    throw new CorruptIndexException("Corrupted");
                }
                docBasesDeltas[blockCount] = (Lucene.Net.Util.Packed.PackedInts.Reader)PackedInts.GetReaderNoHeader(fieldsIndexIn, PackedInts.Format.PACKED, packedIntsVersion, numChunks, bitsPerDocBase);

                // start pointers
                startPointers[blockCount] = fieldsIndexIn.ReadVLong();
                avgChunkSizes[blockCount] = fieldsIndexIn.ReadVLong();
                int bitsPerStartPointer = fieldsIndexIn.ReadVInt();
                if (bitsPerStartPointer > 64)
                {
                    throw new CorruptIndexException("Corrupted");
                }
                startPointersDeltas[blockCount] = (Lucene.Net.Util.Packed.PackedInts.Reader)PackedInts.GetReaderNoHeader(fieldsIndexIn, PackedInts.Format.PACKED, packedIntsVersion, numChunks, bitsPerStartPointer);

                ++blockCount;
            }

            this.docBases = Arrays.CopyOf(docBases, blockCount);
            this.startPointers = Arrays.CopyOf(startPointers, blockCount);
            this.avgChunkDocs = Arrays.CopyOf(avgChunkDocs, blockCount);
            this.avgChunkSizes = Arrays.CopyOf(avgChunkSizes, blockCount);
            this.docBasesDeltas = Arrays.CopyOf(docBasesDeltas, blockCount);
            this.startPointersDeltas = Arrays.CopyOf(startPointersDeltas, blockCount);
        }

        private CompressingStoredFieldsIndexReader(CompressingStoredFieldsIndexReader other)
        {
            this.fieldsIndexIn = null;
            this.maxDoc = other.maxDoc;
            this.docBases = other.docBases;
            this.startPointers = other.startPointers;
            this.avgChunkDocs = other.avgChunkDocs;
            this.avgChunkSizes = other.avgChunkSizes;
            this.docBasesDeltas = other.docBasesDeltas;
            this.startPointersDeltas = other.startPointersDeltas;
        }

        private int Block(int docID)
        {
            int lo = 0, hi = docBases.Length - 1;
            while (lo <= hi)
            {
                int mid = Number.URShift(lo + hi, 1);
                int midValue = docBases[mid];
                if (midValue == docID)
                {
                    return mid;
                }
                else if (midValue < docID)
                {
                    lo = mid + 1;
                }
                else
                {
                    hi = mid - 1;
                }
            }
            return hi;
        }

        private int RelativeDocBase(int block, int relativeChunk)
        {
            int expected = avgChunkDocs[block] * relativeChunk;
            long delta = MoveLowOrderBitToSign(docBasesDeltas[block].Get(relativeChunk));
            return expected + (int)delta;
        }

        private long RelativeStartPointer(int block, int relativeChunk)
        {
            long expected = avgChunkSizes[block] * relativeChunk;
            long delta = MoveLowOrderBitToSign(startPointersDeltas[block].Get(relativeChunk));
            return expected + delta;
        }

        private int RelativeChunk(int block, int relativeDoc)
        {
            int lo = 0, hi = docBasesDeltas[block].Size() - 1;
            while (lo <= hi)
            {
                int mid = Number.URShift(lo + hi, 1);
                int midValue = RelativeDocBase(block, mid);
                if (midValue == relativeDoc)
                {
                    return mid;
                }
                else if (midValue < relativeDoc)
                {
                    lo = mid + 1;
                }
                else
                {
                    hi = mid - 1;
                }
            }
            return hi;
        }

        public long GetStartPointer(int docID)
        {
            if (docID < 0 || docID >= maxDoc)
            {
                throw new ArgumentException("docID out of range [0-" + maxDoc + "]: " + docID);
            }
            int block = Block(docID);
            int relativeChunk = this.RelativeChunk(block, docID - docBases[block]);
            return startPointers[block] + RelativeStartPointer(block, relativeChunk);
        }

        public object Clone()
        {
            if (fieldsIndexIn == null)
            {
                return this;
            }
            else
            {
                return new CompressingStoredFieldsIndexReader(this);
            }
        }

        public void Dispose()
        {
            IOUtils.Close(fieldsIndexIn);
        }

    }
}
