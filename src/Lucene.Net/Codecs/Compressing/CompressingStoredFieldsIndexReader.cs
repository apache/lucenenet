using J2N.Numerics;
using Lucene.Net.Support;
using System;
using System.Runtime.CompilerServices;
using ArrayUtil = Lucene.Net.Util.ArrayUtil;

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

    using CorruptIndexException = Lucene.Net.Index.CorruptIndexException;
    using IndexInput = Lucene.Net.Store.IndexInput;
    using PackedInt32s = Lucene.Net.Util.Packed.PackedInt32s;
    using RamUsageEstimator = Lucene.Net.Util.RamUsageEstimator;
    using SegmentInfo = Lucene.Net.Index.SegmentInfo;

    /// <summary>
    /// Random-access reader for <see cref="CompressingStoredFieldsIndexWriter"/>.
    /// <para/>
    /// @lucene.internal
    /// </summary>
    public sealed class CompressingStoredFieldsIndexReader // LUCENENET specific: Not implementing ICloneable per Microsoft's recommendation
    {
        internal static long MoveLowOrderBitToSign(long n)
        {
            return ((n.TripleShift(1)) ^ -(n & 1));
        }

        internal readonly int maxDoc;
        internal readonly int[] docBases;
        internal readonly long[] startPointers;
        internal readonly int[] avgChunkDocs;
        internal readonly long[] avgChunkSizes;
        internal readonly PackedInt32s.Reader[] docBasesDeltas; // delta from the avg
        internal readonly PackedInt32s.Reader[] startPointersDeltas; // delta from the avg

        // It is the responsibility of the caller to close fieldsIndexIn after this constructor
        // has been called
        internal CompressingStoredFieldsIndexReader(IndexInput fieldsIndexIn, SegmentInfo si)
        {
            maxDoc = si.DocCount;
            int[] docBases = new int[16];
            long[] startPointers = new long[16];
            int[] avgChunkDocs = new int[16];
            long[] avgChunkSizes = new long[16];
            PackedInt32s.Reader[] docBasesDeltas = new PackedInt32s.Reader[16];
            PackedInt32s.Reader[] startPointersDeltas = new PackedInt32s.Reader[16];

            int packedIntsVersion = fieldsIndexIn.ReadVInt32();

            int blockCount = 0;

            for (; ; )
            {
                int numChunks = fieldsIndexIn.ReadVInt32();
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
                docBases[blockCount] = fieldsIndexIn.ReadVInt32();
                avgChunkDocs[blockCount] = fieldsIndexIn.ReadVInt32();
                int bitsPerDocBase = fieldsIndexIn.ReadVInt32();
                if (bitsPerDocBase > 32)
                {
                    throw new CorruptIndexException("Corrupted bitsPerDocBase (resource=" + fieldsIndexIn + ")");
                }
                docBasesDeltas[blockCount] = PackedInt32s.GetReaderNoHeader(fieldsIndexIn, PackedInt32s.Format.PACKED, packedIntsVersion, numChunks, bitsPerDocBase);

                // start pointers
                startPointers[blockCount] = fieldsIndexIn.ReadVInt64();
                avgChunkSizes[blockCount] = fieldsIndexIn.ReadVInt64();
                int bitsPerStartPointer = fieldsIndexIn.ReadVInt32();
                if (bitsPerStartPointer > 64)
                {
                    throw new CorruptIndexException("Corrupted bitsPerStartPointer (resource=" + fieldsIndexIn + ")");
                }
                startPointersDeltas[blockCount] = PackedInt32s.GetReaderNoHeader(fieldsIndexIn, PackedInt32s.Format.PACKED, packedIntsVersion, numChunks, bitsPerStartPointer);

                ++blockCount;
            }

            this.docBases = Arrays.CopyOf(docBases, blockCount);
            this.startPointers = Arrays.CopyOf(startPointers, blockCount);
            this.avgChunkDocs = Arrays.CopyOf(avgChunkDocs, blockCount);
            this.avgChunkSizes = Arrays.CopyOf(avgChunkSizes, blockCount);
            this.docBasesDeltas = Arrays.CopyOf(docBasesDeltas, blockCount);
            this.startPointersDeltas = Arrays.CopyOf(startPointersDeltas, blockCount);
        }

        private int Block(int docID)
        {
            int lo = 0, hi = docBases.Length - 1;
            while (lo <= hi)
            {
                int mid = (lo + hi).TripleShift(1);
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int RelativeDocBase(int block, int relativeChunk)
        {
            int expected = avgChunkDocs[block] * relativeChunk;
            long delta = MoveLowOrderBitToSign(docBasesDeltas[block].Get(relativeChunk));
            return expected + (int)delta;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private long RelativeStartPointer(int block, int relativeChunk)
        {
            long expected = avgChunkSizes[block] * relativeChunk;
            long delta = MoveLowOrderBitToSign(startPointersDeltas[block].Get(relativeChunk));
            return expected + delta;
        }

        private int RelativeChunk(int block, int relativeDoc)
        {
            int lo = 0, hi = docBasesDeltas[block].Count - 1;
            while (lo <= hi)
            {
                int mid = (lo + hi).TripleShift(1);
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

        internal long GetStartPointer(int docID)
        {
            if (docID < 0 || docID >= maxDoc)
            {
                throw new ArgumentOutOfRangeException(nameof(docID), "docID out of range [0-" + maxDoc + "]: " + docID); // LUCENENET specific - changed from IllegalArgumentException to ArgumentOutOfRangeException (.NET convention)
            }
            int block = Block(docID);
            int relativeChunk = RelativeChunk(block, docID - docBases[block]);
            return startPointers[block] + RelativeStartPointer(block, relativeChunk);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public object Clone()
        {
            return this;
        }

        internal long RamBytesUsed()
        {
            long res = 0;

            foreach (PackedInt32s.Reader r in docBasesDeltas)
            {
                res += r.RamBytesUsed();
            }
            foreach (PackedInt32s.Reader r in startPointersDeltas)
            {
                res += r.RamBytesUsed();
            }

            res += RamUsageEstimator.SizeOf(docBases);
            res += RamUsageEstimator.SizeOf(startPointers);
            res += RamUsageEstimator.SizeOf(avgChunkDocs);
            res += RamUsageEstimator.SizeOf(avgChunkSizes);

            return res;
        }
    }
}