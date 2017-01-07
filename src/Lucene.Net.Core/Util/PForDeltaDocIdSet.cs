using Lucene.Net.Support;
using System;
using System.Diagnostics;

namespace Lucene.Net.Util
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
        internal static readonly PackedInts.IDecoder[] DECODERS = new PackedInts.IDecoder[32];
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
                Debug.Assert(BLOCK_SIZE % DECODERS[i].ByteValueCount == 0);
                ITERATIONS[i] = BLOCK_SIZE / DECODERS[i].ByteValueCount;
                BYTE_BLOCK_COUNTS[i] = ITERATIONS[i] * DECODERS[i].ByteBlockCount;
                maxByteBLockCount = Math.Max(maxByteBLockCount, DECODERS[i].ByteBlockCount);
            }
            MAX_BYTE_BLOCK_COUNT = maxByteBLockCount;
        }

        /// <summary>
        /// A builder for <seealso cref="PForDeltaDocIdSet"/>. </summary>
        public class Builder
        {
            internal readonly GrowableByteArrayDataOutput data;
            internal readonly int[] buffer = new int[BLOCK_SIZE];
            internal readonly int[] exceptionIndices = new int[BLOCK_SIZE];
            internal readonly int[] exceptions = new int[BLOCK_SIZE];
            internal int bufferSize;
            internal int previousDoc;
            internal int cardinality;
            internal int indexInterval;
            internal int numBlocks;

            // temporary variables used when compressing blocks
            internal readonly int[] freqs = new int[32];

            internal int bitsPerValue;
            internal int numExceptions;
            internal int bitsPerException;

            /// <summary>
            /// Sole constructor. </summary>
            public Builder()
            {
                data = new GrowableByteArrayDataOutput(128);
                bufferSize = 0;
                previousDoc = -1;
                indexInterval = 2;
                cardinality = 0;
                numBlocks = 0;
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
                this.indexInterval = indexInterval;
                return this;
            }

            /// <summary>
            /// Add a document to this builder. Documents must be added in order. </summary>
            public virtual Builder Add(int doc)
            {
                if (doc <= previousDoc)
                {
                    throw new System.ArgumentException("Doc IDs must be provided in order, but previousDoc=" + previousDoc + " and doc=" + doc);
                }
                buffer[bufferSize++] = doc - previousDoc - 1;
                if (bufferSize == BLOCK_SIZE)
                {
                    EncodeBlock();
                    bufferSize = 0;
                }
                previousDoc = doc;
                ++cardinality;
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
                Arrays.Fill(freqs, 0);
                for (int i = 0; i < bufferSize; ++i)
                {
                    ++freqs[32 - Number.NumberOfLeadingZeros(buffer[i])];
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
                if (bufferSize < BLOCK_SIZE)
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
                    deltaSum += 1 + buffer[i];
                }
                int blockSize = (int)((uint)(deltaSum + 0x07) >> 3); // round to the next byte
                ++blockSize; // header
                if (bufferSize < BLOCK_SIZE)
                {
                    blockSize += 1; // length of the block
                }
                return blockSize;
            }

            internal virtual int ComputeOptimalNumberOfBits()
            {
                ComputeFreqs();
                bitsPerValue = 31;
                numExceptions = 0;
                while (bitsPerValue > 0 && freqs[bitsPerValue] == 0)
                {
                    --bitsPerValue;
                }
                int actualBitsPerValue = bitsPerValue;
                int blockSize = PforBlockSize(bitsPerValue, numExceptions, bitsPerException);

                // Now try different values for bitsPerValue and pick the best one
                for (int bitsPerValue = this.bitsPerValue - 1, numExceptions = freqs[this.bitsPerValue]; bitsPerValue >= 0 && numExceptions <= MAX_EXCEPTIONS; numExceptions += freqs[bitsPerValue--])
                {
                    int newBlockSize = PforBlockSize(bitsPerValue, numExceptions, actualBitsPerValue - bitsPerValue);
                    if (newBlockSize < blockSize)
                    {
                        this.bitsPerValue = bitsPerValue;
                        this.numExceptions = numExceptions;
                        blockSize = newBlockSize;
                    }
                }
                this.bitsPerException = actualBitsPerValue - bitsPerValue;
                Debug.Assert(bufferSize < BLOCK_SIZE || numExceptions < bufferSize);
                return blockSize;
            }

            internal virtual void PforEncode()
            {
                if (numExceptions > 0)
                {
                    int mask = (1 << bitsPerValue) - 1;
                    int ex = 0;
                    for (int i = 0; i < bufferSize; ++i)
                    {
                        if (buffer[i] > mask)
                        {
                            exceptionIndices[ex] = i;
                            exceptions[ex++] = (int)((uint)buffer[i] >> bitsPerValue);
                            buffer[i] &= mask;
                        }
                    }
                    Debug.Assert(ex == numExceptions);
                    Arrays.Fill(exceptions, numExceptions, BLOCK_SIZE, 0);
                }

                if (bitsPerValue > 0)
                {
                    PackedInts.IEncoder encoder = PackedInts.GetEncoder(PackedInts.Format.PACKED, PackedInts.VERSION_CURRENT, bitsPerValue);
                    int numIterations = ITERATIONS[bitsPerValue];
                    encoder.Encode(buffer, 0, data.Bytes, data.Length, numIterations);
                    data.Length += encoder.ByteBlockCount * numIterations;
                }

                if (numExceptions > 0)
                {
                    Debug.Assert(bitsPerException > 0);
                    data.WriteByte((byte)(sbyte)numExceptions);
                    data.WriteByte((byte)(sbyte)bitsPerException);
                    PackedInts.IEncoder encoder = PackedInts.GetEncoder(PackedInts.Format.PACKED, PackedInts.VERSION_CURRENT, bitsPerException);
                    int numIterations = (numExceptions + encoder.ByteValueCount - 1) / encoder.ByteValueCount;
                    encoder.Encode(exceptions, 0, data.Bytes, data.Length, numIterations);
                    data.Length += (int)PackedInts.Format.PACKED.ByteCount(PackedInts.VERSION_CURRENT, numExceptions, bitsPerException);
                    for (int i = 0; i < numExceptions; ++i)
                    {
                        data.WriteByte((byte)(sbyte)exceptionIndices[i]);
                    }
                }
            }

            internal virtual void UnaryEncode()
            {
                int current = 0;
                for (int i = 0, doc = -1; i < BLOCK_SIZE; ++i)
                {
                    doc += 1 + buffer[i];
                    while (doc >= 8)
                    {
                        data.WriteByte((byte)(sbyte)current);
                        current = 0;
                        doc -= 8;
                    }
                    current |= 1 << doc;
                }
                if (current != 0)
                {
                    data.WriteByte((byte)(sbyte)current);
                }
            }

            internal virtual void EncodeBlock()
            {
                int originalLength = data.Length;
                Arrays.Fill(buffer, bufferSize, BLOCK_SIZE, 0);
                int unaryBlockSize = UnaryBlockSize();
                int pforBlockSize = ComputeOptimalNumberOfBits();
                int blockSize;
                if (pforBlockSize <= unaryBlockSize)
                {
                    // use pfor
                    blockSize = pforBlockSize;
                    data.Bytes = ArrayUtil.Grow(data.Bytes, data.Length + blockSize + MAX_BYTE_BLOCK_COUNT);
                    int token = bufferSize < BLOCK_SIZE ? LAST_BLOCK : 0;
                    token |= bitsPerValue;
                    if (numExceptions > 0)
                    {
                        token |= HAS_EXCEPTIONS;
                    }
                    data.WriteByte((byte)(sbyte)token);
                    PforEncode();
                }
                else
                {
                    // use unary
                    blockSize = unaryBlockSize;
                    int token = UNARY | (bufferSize < BLOCK_SIZE ? LAST_BLOCK : 0);
                    data.WriteByte((byte)(sbyte)token);
                    UnaryEncode();
                }

                if (bufferSize < BLOCK_SIZE)
                {
                    data.WriteByte((byte)(sbyte)bufferSize);
                }

                ++numBlocks;

                Debug.Assert(data.Length - originalLength == blockSize, (data.Length - originalLength) + " <> " + blockSize);
            }

            /// <summary>
            /// Build the <seealso cref="PForDeltaDocIdSet"/> instance. </summary>
            public virtual PForDeltaDocIdSet Build()
            {
                Debug.Assert(bufferSize < BLOCK_SIZE);

                if (cardinality == 0)
                {
                    Debug.Assert(previousDoc == -1);
                    return EMPTY;
                }

                EncodeBlock();
                var dataArr = Arrays.CopyOf(data.Bytes, data.Length + MAX_BYTE_BLOCK_COUNT);

                int indexSize = (numBlocks - 1) / indexInterval + 1;
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
                    Iterator it = new Iterator(dataArr, cardinality, int.MaxValue, SINGLE_ZERO_BUFFER, SINGLE_ZERO_BUFFER);
                    for (int k = 0; k < indexSize; ++k)
                    {
                        docIDs.Add(it.DocID + 1);
                        offsets.Add(it.offset);
                        for (int i = 0; i < indexInterval; ++i)
                        {
                            it.SkipBlock();
                            if (it.DocID == DocIdSetIterator.NO_MORE_DOCS)
                            {
                                goto indexBreak;
                            }
                        }
                    //indexContinue: ;
                    }
                indexBreak:
                    docIDs.Freeze();
                    offsets.Freeze();
                }

                return new PForDeltaDocIdSet(dataArr, cardinality, indexInterval, docIDs, offsets);
            }
        }

        internal readonly byte[] data;
        internal readonly MonotonicAppendingLongBuffer docIDs, offsets; // for the index
        internal readonly int cardinality, indexInterval;

        internal PForDeltaDocIdSet(byte[] data, int cardinality, int indexInterval, MonotonicAppendingLongBuffer docIDs, MonotonicAppendingLongBuffer offsets)
        {
            this.data = data;
            this.cardinality = cardinality;
            this.indexInterval = indexInterval;
            this.docIDs = docIDs;
            this.offsets = offsets;
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
            if (data == null)
            {
                return null;
            }
            else
            {
                return new Iterator(data, cardinality, indexInterval, docIDs, offsets);
            }
        }

        internal class Iterator : DocIdSetIterator
        {
            // index
            internal readonly int indexInterval;

            internal readonly MonotonicAppendingLongBuffer docIDs, offsets;

            internal readonly int cardinality;
            internal readonly byte[] data;
            internal int offset; // offset in data

            internal readonly int[] nextDocs;
            internal int i; // index in nextDeltas

            internal readonly int[] nextExceptions;

            internal int blockIdx;
            internal int docID;

            internal Iterator(byte[] data, int cardinality, int indexInterval, MonotonicAppendingLongBuffer docIDs, MonotonicAppendingLongBuffer offsets)
            {
                this.data = data;
                this.cardinality = cardinality;
                this.indexInterval = indexInterval;
                this.docIDs = docIDs;
                this.offsets = offsets;
                offset = 0;
                nextDocs = new int[BLOCK_SIZE];
                Arrays.Fill(nextDocs, -1);
                i = BLOCK_SIZE;
                nextExceptions = new int[BLOCK_SIZE];
                blockIdx = -1;
                docID = -1;
            }

            public override int DocID
            {
                get { return docID; }
            }

            internal virtual void PforDecompress(byte token)
            {
                int bitsPerValue = token & 0x1F;
                if (bitsPerValue == 0)
                {
                    Arrays.Fill(nextDocs, 0);
                }
                else
                {
                    DECODERS[bitsPerValue].Decode(data, offset, nextDocs, 0, ITERATIONS[bitsPerValue]);
                    offset += BYTE_BLOCK_COUNTS[bitsPerValue];
                }
                if ((token & HAS_EXCEPTIONS) != 0)
                {
                    // there are exceptions
                    int numExceptions = data[offset++];
                    int bitsPerException = data[offset++];
                    int numIterations = (numExceptions + DECODERS[bitsPerException].ByteValueCount - 1) / DECODERS[bitsPerException].ByteValueCount;
                    DECODERS[bitsPerException].Decode(data, offset, nextExceptions, 0, numIterations);
                    offset += (int)PackedInts.Format.PACKED.ByteCount(PackedInts.VERSION_CURRENT, numExceptions, bitsPerException);
                    for (int i = 0; i < numExceptions; ++i)
                    {
                        nextDocs[data[offset++]] |= nextExceptions[i] << bitsPerValue;
                    }
                }
                for (int previousDoc = docID, i = 0; i < BLOCK_SIZE; ++i)
                {
                    int doc = previousDoc + 1 + nextDocs[i];
                    previousDoc = nextDocs[i] = doc;
                }
            }

            internal virtual void UnaryDecompress(byte token)
            {
                Debug.Assert((token & HAS_EXCEPTIONS) == 0);
                int docID = this.docID;
                for (int i = 0; i < BLOCK_SIZE; )
                {
                    var b = data[offset++];
                    for (int bitList = BitUtil.BitList(b); bitList != 0; ++i, bitList = (int)((uint)bitList >> 4))
                    {
                        nextDocs[i] = docID + (bitList & 0x0F);
                    }
                    docID += 8;
                }
            }

            internal virtual void DecompressBlock()
            {
                var token = data[offset++];

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
                    int blockSize = data[offset++];
                    Arrays.Fill(nextDocs, blockSize, BLOCK_SIZE, NO_MORE_DOCS);
                }
                ++blockIdx;
            }

            internal virtual void SkipBlock()
            {
                Debug.Assert(i == BLOCK_SIZE);
                DecompressBlock();
                docID = nextDocs[BLOCK_SIZE - 1];
            }

            public override int NextDoc()
            {
                if (i == BLOCK_SIZE)
                {
                    DecompressBlock();
                    i = 0;
                }
                return docID = nextDocs[i++];
            }

            internal virtual int ForwardBinarySearch(int target)
            {
                // advance forward and double the window at each step
                int indexSize = (int)docIDs.Count;
                int lo = Math.Max(blockIdx / indexInterval, 0), hi = lo + 1;
                Debug.Assert(blockIdx == -1 || docIDs.Get(lo) <= docID);
                Debug.Assert(lo + 1 == docIDs.Count || docIDs.Get(lo + 1) > docID);
                while (true)
                {
                    if (hi >= indexSize)
                    {
                        hi = indexSize - 1;
                        break;
                    }
                    else if (docIDs.Get(hi) >= target)
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
                    int midDocID = (int)docIDs.Get(mid);
                    if (midDocID <= target)
                    {
                        lo = mid + 1;
                    }
                    else
                    {
                        hi = mid - 1;
                    }
                }
                Debug.Assert(docIDs.Get(hi) <= target);
                Debug.Assert(hi + 1 == docIDs.Count || docIDs.Get(hi + 1) > target);
                return hi;
            }

            public override int Advance(int target)
            {
                Debug.Assert(target > docID);
                if (nextDocs[BLOCK_SIZE - 1] < target)
                {
                    // not in the next block, now use the index
                    int index = ForwardBinarySearch(target);
                    int offset = (int)offsets.Get(index);
                    if (offset > this.offset)
                    {
                        this.offset = offset;
                        docID = (int)docIDs.Get(index) - 1;
                        blockIdx = index * indexInterval - 1;
                        while (true)
                        {
                            DecompressBlock();
                            if (nextDocs[BLOCK_SIZE - 1] >= target)
                            {
                                break;
                            }
                            docID = nextDocs[BLOCK_SIZE - 1];
                        }
                        i = 0;
                    }
                }
                return SlowAdvance(target);
            }

            public override long Cost()
            {
                return cardinality;
            }
        }

        /// <summary>
        /// Return the number of documents in this <seealso cref="DocIdSet"/> in constant time. </summary>
        public int Cardinality()
        {
            return cardinality;
        }

        /// <summary>
        /// Return the memory usage of this instance. </summary>
        public long RamBytesUsed()
        {
            return RamUsageEstimator.AlignObjectSize(3 * RamUsageEstimator.NUM_BYTES_OBJECT_REF) + docIDs.RamBytesUsed() + offsets.RamBytesUsed();
        }
    }
}