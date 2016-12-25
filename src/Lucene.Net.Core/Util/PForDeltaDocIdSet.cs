using System;
using System.Diagnostics;

namespace Lucene.Net.Util
{
    using Lucene.Net.Support;

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

    using DocIdSet = Lucene.Net.Search.DocIdSet;
    using DocIdSetIterator = Lucene.Net.Search.DocIdSetIterator;
    using MonotonicAppendingLongBuffer = Lucene.Net.Util.Packed.MonotonicAppendingLongBuffer;
    using PackedInts = Lucene.Net.Util.Packed.PackedInts;

    /// <summary>
    /// <seealso cref="DocIdSet"/> implementation based on pfor-delta encoding.
    /// <p>this implementation is inspired from LinkedIn's Kamikaze
    /// (http://data.linkedin.com/opensource/kamikaze) and Daniel Lemire's JavaFastPFOR
    /// (https://github.com/lemire/JavaFastPFOR).</p>
    /// <p>On the contrary to the original PFOR paper, exceptions are encoded with
    /// FOR instead of Simple16.</p>
    /// </summary>
    public sealed class PForDeltaDocIdSet : DocIdSet
    {
        internal const int BLOCK_SIZE = 128;
        internal const int MAX_EXCEPTIONS = 24; // no more than 24 exceptions per block
        internal static readonly PackedInts.Decoder[] DECODERS = new PackedInts.Decoder[32];
        internal static readonly int[] ITERATIONS = new int[32];
        internal static readonly int[] BYTE_BLOCK_COUNTS = new int[32];
        internal static readonly int MAX_BYTE_BLOCK_COUNT;
        internal static readonly MonotonicAppendingLongBuffer SINGLE_ZERO_BUFFER = new MonotonicAppendingLongBuffer(0, 64, PackedInts.COMPACT);
        internal static readonly PForDeltaDocIdSet EMPTY = new PForDeltaDocIdSet(null, 0, int.MaxValue, SINGLE_ZERO_BUFFER, SINGLE_ZERO_BUFFER);
        internal static readonly int LAST_BLOCK = 1 << 5; // flag to indicate the last block
        internal static readonly int HAS_EXCEPTIONS = 1 << 6;
        internal static readonly int UNARY = 1 << 7;

        static PForDeltaDocIdSet()
        {
            SINGLE_ZERO_BUFFER.Add(0);
            SINGLE_ZERO_BUFFER.Freeze();
            int maxByteBLockCount = 0;
            for (int i = 1; i < ITERATIONS.Length; ++i)
            {
                DECODERS[i] = PackedInts.GetDecoder(PackedInts.Format.PACKED, PackedInts.VERSION_CURRENT, i);
                Debug.Assert(BLOCK_SIZE % DECODERS[i].ByteValueCount() == 0);
                ITERATIONS[i] = BLOCK_SIZE / DECODERS[i].ByteValueCount();
                BYTE_BLOCK_COUNTS[i] = ITERATIONS[i] * DECODERS[i].ByteBlockCount();
                maxByteBLockCount = Math.Max(maxByteBLockCount, DECODERS[i].ByteBlockCount());
            }
            MAX_BYTE_BLOCK_COUNT = maxByteBLockCount;
        }

        /// <summary>
        /// A builder for <seealso cref="PForDeltaDocIdSet"/>. </summary>
        public class Builder
        {
            internal readonly GrowableByteArrayDataOutput Data;
            internal readonly int[] Buffer = new int[BLOCK_SIZE];
            internal readonly int[] ExceptionIndices = new int[BLOCK_SIZE];
            internal readonly int[] Exceptions = new int[BLOCK_SIZE];
            internal int BufferSize;
            internal int PreviousDoc;
            internal int Cardinality;
            internal int IndexInterval_Renamed;
            internal int NumBlocks;

            // temporary variables used when compressing blocks
            internal readonly int[] Freqs = new int[32];

            internal int BitsPerValue;
            internal int NumExceptions;
            internal int BitsPerException;

            /// <summary>
            /// Sole constructor. </summary>
            public Builder()
            {
                Data = new GrowableByteArrayDataOutput(128);
                BufferSize = 0;
                PreviousDoc = -1;
                IndexInterval_Renamed = 2;
                Cardinality = 0;
                NumBlocks = 0;
            }

            /// <summary>
            /// Set the index interval. Every <code>indexInterval</code>-th block will
            /// be stored in the index. Set to <seealso cref="Integer#MAX_VALUE"/> to disable indexing.
            /// </summary>
            public virtual Builder SetIndexInterval(int indexInterval)
            {
                if (indexInterval < 1)
                {
                    throw new System.ArgumentException("indexInterval must be >= 1");
                }
                this.IndexInterval_Renamed = indexInterval;
                return this;
            }

            /// <summary>
            /// Add a document to this builder. Documents must be added in order. </summary>
            public virtual Builder Add(int doc)
            {
                if (doc <= PreviousDoc)
                {
                    throw new System.ArgumentException("Doc IDs must be provided in order, but previousDoc=" + PreviousDoc + " and doc=" + doc);
                }
                Buffer[BufferSize++] = doc - PreviousDoc - 1;
                if (BufferSize == BLOCK_SIZE)
                {
                    EncodeBlock();
                    BufferSize = 0;
                }
                PreviousDoc = doc;
                ++Cardinality;
                return this;
            }

            /// <summary>
            /// Convenience method to add the content of a <seealso cref="DocIdSetIterator"/> to this builder. </summary>
            public virtual Builder Add(DocIdSetIterator it)
            {
                for (int doc = it.NextDoc(); doc != DocIdSetIterator.NO_MORE_DOCS; doc = it.NextDoc())
                {
                    Add(doc);
                }
                return this;
            }

            internal virtual void ComputeFreqs()
            {
                Arrays.Fill(Freqs, 0);
                for (int i = 0; i < BufferSize; ++i)
                {
                    ++Freqs[32 - Number.NumberOfLeadingZeros(Buffer[i])];
                }
            }

            internal virtual int PforBlockSize(int bitsPerValue, int numExceptions, int bitsPerException)
            {
                PackedInts.Format format = PackedInts.Format.PACKED;
                long blockSize = 1 + format.ByteCount(PackedInts.VERSION_CURRENT, BLOCK_SIZE, bitsPerValue); // header: number of bits per value
                if (numExceptions > 0)
                {
                    blockSize += 2 + numExceptions + format.ByteCount(PackedInts.VERSION_CURRENT, numExceptions, bitsPerException); // indices of the exceptions -  2 additional bytes in case of exceptions: numExceptions and bitsPerException
                }
                if (BufferSize < BLOCK_SIZE)
                {
                    blockSize += 1; // length of the block
                }
                return (int)blockSize;
            }

            internal virtual int UnaryBlockSize()
            {
                int deltaSum = 0;
                for (int i = 0; i < BLOCK_SIZE; ++i)
                {
                    deltaSum += 1 + Buffer[i];
                }
                int blockSize = (int)((uint)(deltaSum + 0x07) >> 3); // round to the next byte
                ++blockSize; // header
                if (BufferSize < BLOCK_SIZE)
                {
                    blockSize += 1; // length of the block
                }
                return blockSize;
            }

            internal virtual int ComputeOptimalNumberOfBits()
            {
                ComputeFreqs();
                BitsPerValue = 31;
                NumExceptions = 0;
                while (BitsPerValue > 0 && Freqs[BitsPerValue] == 0)
                {
                    --BitsPerValue;
                }
                int actualBitsPerValue = BitsPerValue;
                int blockSize = PforBlockSize(BitsPerValue, NumExceptions, BitsPerException);

                // Now try different values for bitsPerValue and pick the best one
                for (int bitsPerValue = this.BitsPerValue - 1, numExceptions = Freqs[this.BitsPerValue]; bitsPerValue >= 0 && numExceptions <= MAX_EXCEPTIONS; numExceptions += Freqs[bitsPerValue--])
                {
                    int newBlockSize = PforBlockSize(bitsPerValue, numExceptions, actualBitsPerValue - bitsPerValue);
                    if (newBlockSize < blockSize)
                    {
                        this.BitsPerValue = bitsPerValue;
                        this.NumExceptions = numExceptions;
                        blockSize = newBlockSize;
                    }
                }
                this.BitsPerException = actualBitsPerValue - BitsPerValue;
                Debug.Assert(BufferSize < BLOCK_SIZE || NumExceptions < BufferSize);
                return blockSize;
            }

            internal virtual void PforEncode()
            {
                if (NumExceptions > 0)
                {
                    int mask = (1 << BitsPerValue) - 1;
                    int ex = 0;
                    for (int i = 0; i < BufferSize; ++i)
                    {
                        if (Buffer[i] > mask)
                        {
                            ExceptionIndices[ex] = i;
                            Exceptions[ex++] = (int)((uint)Buffer[i] >> BitsPerValue);
                            Buffer[i] &= mask;
                        }
                    }
                    Debug.Assert(ex == NumExceptions);
                    Arrays.Fill(Exceptions, NumExceptions, BLOCK_SIZE, 0);
                }

                if (BitsPerValue > 0)
                {
                    PackedInts.Encoder encoder = PackedInts.GetEncoder(PackedInts.Format.PACKED, PackedInts.VERSION_CURRENT, BitsPerValue);
                    int numIterations = ITERATIONS[BitsPerValue];
                    encoder.Encode(Buffer, 0, Data.Bytes, Data.Length, numIterations);
                    Data.Length += encoder.ByteBlockCount() * numIterations;
                }

                if (NumExceptions > 0)
                {
                    Debug.Assert(BitsPerException > 0);
                    Data.WriteByte((byte)(sbyte)NumExceptions);
                    Data.WriteByte((byte)(sbyte)BitsPerException);
                    PackedInts.Encoder encoder = PackedInts.GetEncoder(PackedInts.Format.PACKED, PackedInts.VERSION_CURRENT, BitsPerException);
                    int numIterations = (NumExceptions + encoder.ByteValueCount() - 1) / encoder.ByteValueCount();
                    encoder.Encode(Exceptions, 0, Data.Bytes, Data.Length, numIterations);
                    Data.Length += (int)PackedInts.Format.PACKED.ByteCount(PackedInts.VERSION_CURRENT, NumExceptions, BitsPerException);
                    for (int i = 0; i < NumExceptions; ++i)
                    {
                        Data.WriteByte((byte)(sbyte)ExceptionIndices[i]);
                    }
                }
            }

            internal virtual void UnaryEncode()
            {
                int current = 0;
                for (int i = 0, doc = -1; i < BLOCK_SIZE; ++i)
                {
                    doc += 1 + Buffer[i];
                    while (doc >= 8)
                    {
                        Data.WriteByte((byte)(sbyte)current);
                        current = 0;
                        doc -= 8;
                    }
                    current |= 1 << doc;
                }
                if (current != 0)
                {
                    Data.WriteByte((byte)(sbyte)current);
                }
            }

            internal virtual void EncodeBlock()
            {
                int originalLength = Data.Length;
                Arrays.Fill(Buffer, BufferSize, BLOCK_SIZE, 0);
                int unaryBlockSize = UnaryBlockSize();
                int pforBlockSize = ComputeOptimalNumberOfBits();
                int blockSize;
                if (pforBlockSize <= unaryBlockSize)
                {
                    // use pfor
                    blockSize = pforBlockSize;
                    Data.Bytes = ArrayUtil.Grow(Data.Bytes, Data.Length + blockSize + MAX_BYTE_BLOCK_COUNT);
                    int token = BufferSize < BLOCK_SIZE ? LAST_BLOCK : 0;
                    token |= BitsPerValue;
                    if (NumExceptions > 0)
                    {
                        token |= HAS_EXCEPTIONS;
                    }
                    Data.WriteByte((byte)(sbyte)token);
                    PforEncode();
                }
                else
                {
                    // use unary
                    blockSize = unaryBlockSize;
                    int token = UNARY | (BufferSize < BLOCK_SIZE ? LAST_BLOCK : 0);
                    Data.WriteByte((byte)(sbyte)token);
                    UnaryEncode();
                }

                if (BufferSize < BLOCK_SIZE)
                {
                    Data.WriteByte((byte)(sbyte)BufferSize);
                }

                ++NumBlocks;

                Debug.Assert(Data.Length - originalLength == blockSize, (Data.Length - originalLength) + " <> " + blockSize);
            }

            /// <summary>
            /// Build the <seealso cref="PForDeltaDocIdSet"/> instance. </summary>
            public virtual PForDeltaDocIdSet Build()
            {
                Debug.Assert(BufferSize < BLOCK_SIZE);

                if (Cardinality == 0)
                {
                    Debug.Assert(PreviousDoc == -1);
                    return EMPTY;
                }

                EncodeBlock();
                var dataArr = Arrays.CopyOf(Data.Bytes, Data.Length + MAX_BYTE_BLOCK_COUNT);

                int indexSize = (NumBlocks - 1) / IndexInterval_Renamed + 1;
                MonotonicAppendingLongBuffer docIDs, offsets;
                if (indexSize <= 1)
                {
                    docIDs = offsets = SINGLE_ZERO_BUFFER;
                }
                else
                {
                    const int pageSize = 128;
                    int initialPageCount = (indexSize + pageSize - 1) / pageSize;
                    docIDs = new MonotonicAppendingLongBuffer(initialPageCount, pageSize, PackedInts.COMPACT);
                    offsets = new MonotonicAppendingLongBuffer(initialPageCount, pageSize, PackedInts.COMPACT);
                    // Now build the index
                    Iterator it = new Iterator(dataArr, Cardinality, int.MaxValue, SINGLE_ZERO_BUFFER, SINGLE_ZERO_BUFFER);
                    for (int k = 0; k < indexSize; ++k)
                    {
                        docIDs.Add(it.DocID() + 1);
                        offsets.Add(it.Offset);
                        for (int i = 0; i < IndexInterval_Renamed; ++i)
                        {
                            it.SkipBlock();
                            if (it.DocID() == DocIdSetIterator.NO_MORE_DOCS)
                            {
                                goto indexBreak;
                            }
                        }
                    indexContinue: ;
                    }
                indexBreak:
                    docIDs.Freeze();
                    offsets.Freeze();
                }

                return new PForDeltaDocIdSet(dataArr, Cardinality, IndexInterval_Renamed, docIDs, offsets);
            }
        }

        internal readonly byte[] Data;
        internal readonly MonotonicAppendingLongBuffer DocIDs, Offsets; // for the index
        internal readonly int Cardinality_Renamed, IndexInterval;

        internal PForDeltaDocIdSet(byte[] data, int cardinality, int indexInterval, MonotonicAppendingLongBuffer docIDs, MonotonicAppendingLongBuffer offsets)
        {
            this.Data = data;
            this.Cardinality_Renamed = cardinality;
            this.IndexInterval = indexInterval;
            this.DocIDs = docIDs;
            this.Offsets = offsets;
        }

        public override bool IsCacheable
        {
            get
            {
                return true;
            }
        }

        public override DocIdSetIterator GetIterator()
        {
            if (Data == null)
            {
                return null;
            }
            else
            {
                return new Iterator(Data, Cardinality_Renamed, IndexInterval, DocIDs, Offsets);
            }
        }

        internal class Iterator : DocIdSetIterator
        {
            // index
            internal readonly int IndexInterval;

            internal readonly MonotonicAppendingLongBuffer DocIDs, Offsets;

            internal readonly int Cardinality;
            internal readonly byte[] Data;
            internal int Offset; // offset in data

            internal readonly int[] NextDocs;
            internal int i; // index in nextDeltas

            internal readonly int[] NextExceptions;

            internal int BlockIdx;
            internal int DocID_Renamed;

            internal Iterator(byte[] data, int cardinality, int indexInterval, MonotonicAppendingLongBuffer docIDs, MonotonicAppendingLongBuffer offsets)
            {
                this.Data = data;
                this.Cardinality = cardinality;
                this.IndexInterval = indexInterval;
                this.DocIDs = docIDs;
                this.Offsets = offsets;
                Offset = 0;
                NextDocs = new int[BLOCK_SIZE];
                Arrays.Fill(NextDocs, -1);
                i = BLOCK_SIZE;
                NextExceptions = new int[BLOCK_SIZE];
                BlockIdx = -1;
                DocID_Renamed = -1;
            }

            public override int DocID()
            {
                return DocID_Renamed;
            }

            internal virtual void PforDecompress(byte token)
            {
                int bitsPerValue = token & 0x1F;
                if (bitsPerValue == 0)
                {
                    Arrays.Fill(NextDocs, 0);
                }
                else
                {
                    DECODERS[bitsPerValue].Decode(Data, Offset, NextDocs, 0, ITERATIONS[bitsPerValue]);
                    Offset += BYTE_BLOCK_COUNTS[bitsPerValue];
                }
                if ((token & HAS_EXCEPTIONS) != 0)
                {
                    // there are exceptions
                    int numExceptions = Data[Offset++];
                    int bitsPerException = Data[Offset++];
                    int numIterations = (numExceptions + DECODERS[bitsPerException].ByteValueCount() - 1) / DECODERS[bitsPerException].ByteValueCount();
                    DECODERS[bitsPerException].Decode(Data, Offset, NextExceptions, 0, numIterations);
                    Offset += (int)PackedInts.Format.PACKED.ByteCount(PackedInts.VERSION_CURRENT, numExceptions, bitsPerException);
                    for (int i = 0; i < numExceptions; ++i)
                    {
                        NextDocs[Data[Offset++]] |= NextExceptions[i] << bitsPerValue;
                    }
                }
                for (int previousDoc = DocID_Renamed, i = 0; i < BLOCK_SIZE; ++i)
                {
                    int doc = previousDoc + 1 + NextDocs[i];
                    previousDoc = NextDocs[i] = doc;
                }
            }

            internal virtual void UnaryDecompress(byte token)
            {
                Debug.Assert((token & HAS_EXCEPTIONS) == 0);
                int docID = this.DocID_Renamed;
                for (int i = 0; i < BLOCK_SIZE; )
                {
                    var b = Data[Offset++];
                    for (int bitList = BitUtil.BitList(b); bitList != 0; ++i, bitList = (int)((uint)bitList >> 4))
                    {
                        NextDocs[i] = docID + (bitList & 0x0F);
                    }
                    docID += 8;
                }
            }

            internal virtual void DecompressBlock()
            {
                var token = Data[Offset++];

                if ((token & UNARY) != 0)
                {
                    UnaryDecompress(token);
                }
                else
                {
                    PforDecompress(token);
                }

                if ((token & LAST_BLOCK) != 0)
                {
                    int blockSize = Data[Offset++];
                    Arrays.Fill(NextDocs, blockSize, BLOCK_SIZE, NO_MORE_DOCS);
                }
                ++BlockIdx;
            }

            internal virtual void SkipBlock()
            {
                Debug.Assert(i == BLOCK_SIZE);
                DecompressBlock();
                DocID_Renamed = NextDocs[BLOCK_SIZE - 1];
            }

            public override int NextDoc()
            {
                if (i == BLOCK_SIZE)
                {
                    DecompressBlock();
                    i = 0;
                }
                return DocID_Renamed = NextDocs[i++];
            }

            internal virtual int ForwardBinarySearch(int target)
            {
                // advance forward and double the window at each step
                int indexSize = (int)DocIDs.Size();
                int lo = Math.Max(BlockIdx / IndexInterval, 0), hi = lo + 1;
                Debug.Assert(BlockIdx == -1 || DocIDs.Get(lo) <= DocID_Renamed);
                Debug.Assert(lo + 1 == DocIDs.Size() || DocIDs.Get(lo + 1) > DocID_Renamed);
                while (true)
                {
                    if (hi >= indexSize)
                    {
                        hi = indexSize - 1;
                        break;
                    }
                    else if (DocIDs.Get(hi) >= target)
                    {
                        break;
                    }
                    int newLo = hi;
                    hi += (hi - lo) << 1;
                    lo = newLo;
                }

                // we found a window containing our target, let's binary search now
                while (lo <= hi)
                {
                    int mid = (int)((uint)(lo + hi) >> 1);
                    int midDocID = (int)DocIDs.Get(mid);
                    if (midDocID <= target)
                    {
                        lo = mid + 1;
                    }
                    else
                    {
                        hi = mid - 1;
                    }
                }
                Debug.Assert(DocIDs.Get(hi) <= target);
                Debug.Assert(hi + 1 == DocIDs.Size() || DocIDs.Get(hi + 1) > target);
                return hi;
            }

            public override int Advance(int target)
            {
                Debug.Assert(target > DocID_Renamed);
                if (NextDocs[BLOCK_SIZE - 1] < target)
                {
                    // not in the next block, now use the index
                    int index = ForwardBinarySearch(target);
                    int offset = (int)Offsets.Get(index);
                    if (offset > this.Offset)
                    {
                        this.Offset = offset;
                        DocID_Renamed = (int)DocIDs.Get(index) - 1;
                        BlockIdx = index * IndexInterval - 1;
                        while (true)
                        {
                            DecompressBlock();
                            if (NextDocs[BLOCK_SIZE - 1] >= target)
                            {
                                break;
                            }
                            DocID_Renamed = NextDocs[BLOCK_SIZE - 1];
                        }
                        i = 0;
                    }
                }
                return SlowAdvance(target);
            }

            public override long Cost()
            {
                return Cardinality;
            }
        }

        /// <summary>
        /// Return the number of documents in this <seealso cref="DocIdSet"/> in constant time. </summary>
        public int Cardinality()
        {
            return Cardinality_Renamed;
        }

        /// <summary>
        /// Return the memory usage of this instance. </summary>
        public long RamBytesUsed()
        {
            return RamUsageEstimator.AlignObjectSize(3 * RamUsageEstimator.NUM_BYTES_OBJECT_REF) + DocIDs.RamBytesUsed() + Offsets.RamBytesUsed();
        }
    }
}