using System;
using System.Diagnostics;

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

    using IndexOutput = Lucene.Net.Store.IndexOutput;
    using PackedInts = Lucene.Net.Util.Packed.PackedInts;

    /// <summary>
    /// Efficient index format for block-based <seealso cref="Codec"/>s.
    /// <p> this writer generates a file which can be loaded into memory using
    /// memory-efficient data structures to quickly locate the block that contains
    /// any document.
    /// <p>In order to have a compact in-memory representation, for every block of
    /// 1024 chunks, this index computes the average number of bytes per
    /// chunk and for every chunk, only stores the difference between<ul>
    /// <li>${chunk number} * ${average length of a chunk}</li>
    /// <li>and the actual start offset of the chunk</li></ul></p>
    /// <p>Data is written as follows:</p>
    /// <ul>
    /// <li>PackedIntsVersion, &lt;Block&gt;<sup>BlockCount</sup>, BlocksEndMarker</li>
    /// <li>PackedIntsVersion --&gt; <seealso cref="PackedInts#VERSION_CURRENT"/> as a <seealso cref="DataOutput#writeVInt VInt"/></li>
    /// <li>BlocksEndMarker --&gt; <tt>0</tt> as a <seealso cref="DataOutput#writeVInt VInt"/>, this marks the end of blocks since blocks are not allowed to start with <tt>0</tt></li>
    /// <li>Block --&gt; BlockChunks, &lt;DocBases&gt;, &lt;StartPointers&gt;</li>
    /// <li>BlockChunks --&gt; a <seealso cref="DataOutput#writeVInt VInt"/> which is the number of chunks encoded in the block</li>
    /// <li>DocBases --&gt; DocBase, AvgChunkDocs, BitsPerDocBaseDelta, DocBaseDeltas</li>
    /// <li>DocBase --&gt; first document ID of the block of chunks, as a <seealso cref="DataOutput#writeVInt VInt"/></li>
    /// <li>AvgChunkDocs --&gt; average number of documents in a single chunk, as a <seealso cref="DataOutput#writeVInt VInt"/></li>
    /// <li>BitsPerDocBaseDelta --&gt; number of bits required to represent a delta from the average using <a href="https://developers.google.com/protocol-buffers/docs/encoding#types">ZigZag encoding</a></li>
    /// <li>DocBaseDeltas --&gt; <seealso cref="PackedInts packed"/> array of BlockChunks elements of BitsPerDocBaseDelta bits each, representing the deltas from the average doc base using <a href="https://developers.google.com/protocol-buffers/docs/encoding#types">ZigZag encoding</a>.</li>
    /// <li>StartPointers --&gt; StartPointerBase, AvgChunkSize, BitsPerStartPointerDelta, StartPointerDeltas</li>
    /// <li>StartPointerBase --&gt; the first start pointer of the block, as a <seealso cref="DataOutput#writeVLong VLong"/></li>
    /// <li>AvgChunkSize --&gt; the average size of a chunk of compressed documents, as a <seealso cref="DataOutput#writeVLong VLong"/></li>
    /// <li>BitsPerStartPointerDelta --&gt; number of bits required to represent a delta from the average using <a href="https://developers.google.com/protocol-buffers/docs/encoding#types">ZigZag encoding</a></li>
    /// <li>StartPointerDeltas --&gt; <seealso cref="PackedInts packed"/> array of BlockChunks elements of BitsPerStartPointerDelta bits each, representing the deltas from the average start pointer using <a href="https://developers.google.com/protocol-buffers/docs/encoding#types">ZigZag encoding</a></li>
    /// <li>Footer --&gt; <seealso cref="CodecUtil#writeFooter CodecFooter"/></li>
    /// </ul>
    /// <p>Notes</p>
    /// <ul>
    /// <li>For any block, the doc base of the n-th chunk can be restored with
    /// <code>DocBase + AvgChunkDocs * n + DocBaseDeltas[n]</code>.</li>
    /// <li>For any block, the start pointer of the n-th chunk can be restored with
    /// <code>StartPointerBase + AvgChunkSize * n + StartPointerDeltas[n]</code>.</li>
    /// <li>Once data is loaded into memory, you can lookup the start pointer of any
    /// document by performing two binary searches: a first one based on the values
    /// of DocBase in order to find the right block, and then inside the block based
    /// on DocBaseDeltas (by reconstructing the doc bases for every chunk).</li>
    /// </ul>
    /// @lucene.internal
    /// </summary>
    public sealed class CompressingStoredFieldsIndexWriter : IDisposable
    {
        internal const int BLOCK_SIZE = 1024; // number of chunks to serialize at once

        internal static long MoveSignToLowOrderBit(long n)
        {
            return (n >> 63) ^ (n << 1);
        }

        internal readonly IndexOutput FieldsIndexOut;
        internal int TotalDocs;
        internal int BlockDocs;
        internal int BlockChunks;
        internal long FirstStartPointer;
        internal long MaxStartPointer;
        internal readonly int[] DocBaseDeltas;
        internal readonly long[] StartPointerDeltas;

        internal CompressingStoredFieldsIndexWriter(IndexOutput indexOutput)
        {
            this.FieldsIndexOut = indexOutput;
            Reset();
            TotalDocs = 0;
            DocBaseDeltas = new int[BLOCK_SIZE];
            StartPointerDeltas = new long[BLOCK_SIZE];
            FieldsIndexOut.WriteVInt(PackedInts.VERSION_CURRENT);
        }

        private void Reset()
        {
            BlockChunks = 0;
            BlockDocs = 0;
            FirstStartPointer = -1; // means unset
        }

        private void WriteBlock()
        {
            Debug.Assert(BlockChunks > 0);
            FieldsIndexOut.WriteVInt(BlockChunks);

            // The trick here is that we only store the difference from the average start
            // pointer or doc base, this helps save bits per value.
            // And in order to prevent a few chunks that would be far from the average to
            // raise the number of bits per value for all of them, we only encode blocks
            // of 1024 chunks at once
            // See LUCENE-4512

            // doc bases
            int avgChunkDocs;
            if (BlockChunks == 1)
            {
                avgChunkDocs = 0;
            }
            else
            {
                avgChunkDocs = (int)Math.Round((float)(BlockDocs - DocBaseDeltas[BlockChunks - 1]) / (BlockChunks - 1));
            }
            FieldsIndexOut.WriteVInt(TotalDocs - BlockDocs); // docBase
            FieldsIndexOut.WriteVInt(avgChunkDocs);
            int docBase = 0;
            long maxDelta = 0;
            for (int i = 0; i < BlockChunks; ++i)
            {
                int delta = docBase - avgChunkDocs * i;
                maxDelta |= MoveSignToLowOrderBit(delta);
                docBase += DocBaseDeltas[i];
            }

            int bitsPerDocBase = PackedInts.BitsRequired(maxDelta);
            FieldsIndexOut.WriteVInt(bitsPerDocBase);
            PackedInts.Writer writer = PackedInts.GetWriterNoHeader(FieldsIndexOut, PackedInts.Format.PACKED, BlockChunks, bitsPerDocBase, 1);
            docBase = 0;
            for (int i = 0; i < BlockChunks; ++i)
            {
                long delta = docBase - avgChunkDocs * i;
                Debug.Assert(PackedInts.BitsRequired(MoveSignToLowOrderBit(delta)) <= writer.BitsPerValue);
                writer.Add(MoveSignToLowOrderBit(delta));
                docBase += DocBaseDeltas[i];
            }
            writer.Finish();

            // start pointers
            FieldsIndexOut.WriteVLong(FirstStartPointer);
            long avgChunkSize;
            if (BlockChunks == 1)
            {
                avgChunkSize = 0;
            }
            else
            {
                avgChunkSize = (MaxStartPointer - FirstStartPointer) / (BlockChunks - 1);
            }
            FieldsIndexOut.WriteVLong(avgChunkSize);
            long startPointer = 0;
            maxDelta = 0;
            for (int i = 0; i < BlockChunks; ++i)
            {
                startPointer += StartPointerDeltas[i];
                long delta = startPointer - avgChunkSize * i;
                maxDelta |= MoveSignToLowOrderBit(delta);
            }

            int bitsPerStartPointer = PackedInts.BitsRequired(maxDelta);
            FieldsIndexOut.WriteVInt(bitsPerStartPointer);
            writer = PackedInts.GetWriterNoHeader(FieldsIndexOut, PackedInts.Format.PACKED, BlockChunks, bitsPerStartPointer, 1);
            startPointer = 0;
            for (int i = 0; i < BlockChunks; ++i)
            {
                startPointer += StartPointerDeltas[i];
                long delta = startPointer - avgChunkSize * i;
                Debug.Assert(PackedInts.BitsRequired(MoveSignToLowOrderBit(delta)) <= writer.BitsPerValue);
                writer.Add(MoveSignToLowOrderBit(delta));
            }
            writer.Finish();
        }

        internal void WriteIndex(int numDocs, long startPointer)
        {
            if (BlockChunks == BLOCK_SIZE)
            {
                WriteBlock();
                Reset();
            }

            if (FirstStartPointer == -1)
            {
                FirstStartPointer = MaxStartPointer = startPointer;
            }
            Debug.Assert(FirstStartPointer > 0 && startPointer >= FirstStartPointer);

            DocBaseDeltas[BlockChunks] = numDocs;
            StartPointerDeltas[BlockChunks] = startPointer - MaxStartPointer;

            ++BlockChunks;
            BlockDocs += numDocs;
            TotalDocs += numDocs;
            MaxStartPointer = startPointer;
        }

        internal void Finish(int numDocs, long maxPointer)
        {
            if (numDocs != TotalDocs)
            {
                throw new Exception("Expected " + numDocs + " docs, but got " + TotalDocs);
            }
            if (BlockChunks > 0)
            {
                WriteBlock();
            }
            FieldsIndexOut.WriteVInt(0); // end marker
            FieldsIndexOut.WriteVLong(maxPointer);
            CodecUtil.WriteFooter(FieldsIndexOut);
        }

        public void Dispose()
        {
            FieldsIndexOut.Dispose();
        }
    }
}