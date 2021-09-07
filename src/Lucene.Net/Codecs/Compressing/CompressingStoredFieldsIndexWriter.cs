using Lucene.Net.Diagnostics;
using System;
using System.Runtime.CompilerServices;

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
    using PackedInt32s = Lucene.Net.Util.Packed.PackedInt32s;

    /// <summary>
    /// Efficient index format for block-based <see cref="Codec"/>s.
    /// <para/> this writer generates a file which can be loaded into memory using
    /// memory-efficient data structures to quickly locate the block that contains
    /// any document.
    /// <para>In order to have a compact in-memory representation, for every block of
    /// 1024 chunks, this index computes the average number of bytes per
    /// chunk and for every chunk, only stores the difference between
    /// <list type="bullet">
    /// <li>${chunk number} * ${average length of a chunk}</li>
    /// <li>and the actual start offset of the chunk</li>
    /// </list>
    /// </para>
    /// <para>Data is written as follows:</para>
    /// <list type="bullet">
    /// <li>PackedIntsVersion, &lt;Block&gt;<sup>BlockCount</sup>, BlocksEndMarker</li>
    /// <li>PackedIntsVersion --&gt; <see cref="PackedInt32s.VERSION_CURRENT"/> as a VInt (<see cref="Store.DataOutput.WriteVInt32(int)"/>) </li>
    /// <li>BlocksEndMarker --&gt; <tt>0</tt> as a VInt (<see cref="Store.DataOutput.WriteVInt32(int)"/>) , this marks the end of blocks since blocks are not allowed to start with <tt>0</tt></li>
    /// <li>Block --&gt; BlockChunks, &lt;DocBases&gt;, &lt;StartPointers&gt;</li>
    /// <li>BlockChunks --&gt; a VInt (<see cref="Store.DataOutput.WriteVInt32(int)"/>)  which is the number of chunks encoded in the block</li>
    /// <li>DocBases --&gt; DocBase, AvgChunkDocs, BitsPerDocBaseDelta, DocBaseDeltas</li>
    /// <li>DocBase --&gt; first document ID of the block of chunks, as a VInt (<see cref="Store.DataOutput.WriteVInt32(int)"/>) </li>
    /// <li>AvgChunkDocs --&gt; average number of documents in a single chunk, as a VInt (<see cref="Store.DataOutput.WriteVInt32(int)"/>) </li>
    /// <li>BitsPerDocBaseDelta --&gt; number of bits required to represent a delta from the average using <a href="https://developers.google.com/protocol-buffers/docs/encoding#types">ZigZag encoding</a></li>
    /// <li>DocBaseDeltas --&gt; packed (<see cref="PackedInt32s"/>) array of BlockChunks elements of BitsPerDocBaseDelta bits each, representing the deltas from the average doc base using <a href="https://developers.google.com/protocol-buffers/docs/encoding#types">ZigZag encoding</a>.</li>
    /// <li>StartPointers --&gt; StartPointerBase, AvgChunkSize, BitsPerStartPointerDelta, StartPointerDeltas</li>
    /// <li>StartPointerBase --&gt; the first start pointer of the block, as a VLong (<see cref="Store.DataOutput.WriteVInt64(long)"/>) </li>
    /// <li>AvgChunkSize --&gt; the average size of a chunk of compressed documents, as a VLong (<see cref="Store.DataOutput.WriteVInt64(long)"/>) </li>
    /// <li>BitsPerStartPointerDelta --&gt; number of bits required to represent a delta from the average using <a href="https://developers.google.com/protocol-buffers/docs/encoding#types">ZigZag encoding</a></li>
    /// <li>StartPointerDeltas --&gt; packed (<see cref="PackedInt32s"/>) array of BlockChunks elements of BitsPerStartPointerDelta bits each, representing the deltas from the average start pointer using <a href="https://developers.google.com/protocol-buffers/docs/encoding#types">ZigZag encoding</a></li>
    /// <li>Footer --&gt; CodecFooter (<see cref="CodecUtil.WriteFooter(IndexOutput)"/>) </li>
    /// </list>
    /// <para>Notes</para>
    /// <list type="bullet">
    /// <li>For any block, the doc base of the n-th chunk can be restored with
    /// <c>DocBase + AvgChunkDocs * n + DocBaseDeltas[n]</c>.</li>
    /// <li>For any block, the start pointer of the n-th chunk can be restored with
    /// <c>StartPointerBase + AvgChunkSize * n + StartPointerDeltas[n]</c>.</li>
    /// <li>Once data is loaded into memory, you can lookup the start pointer of any
    /// document by performing two binary searches: a first one based on the values
    /// of DocBase in order to find the right block, and then inside the block based
    /// on DocBaseDeltas (by reconstructing the doc bases for every chunk).</li>
    /// </list>
    /// @lucene.internal
    /// </summary>
    public sealed class CompressingStoredFieldsIndexWriter : IDisposable
    {
        internal const int BLOCK_SIZE = 1024; // number of chunks to serialize at once

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static long MoveSignToLowOrderBit(long n)
        {
            return (n >> 63) ^ (n << 1);
        }

        internal readonly IndexOutput fieldsIndexOut;
        internal int totalDocs;
        internal int blockDocs;
        internal int blockChunks;
        internal long firstStartPointer;
        internal long maxStartPointer;
        internal readonly int[] docBaseDeltas;
        internal readonly long[] startPointerDeltas;

        internal CompressingStoredFieldsIndexWriter(IndexOutput indexOutput)
        {
            this.fieldsIndexOut = indexOutput;
            Reset();
            totalDocs = 0;
            docBaseDeltas = new int[BLOCK_SIZE];
            startPointerDeltas = new long[BLOCK_SIZE];
            fieldsIndexOut.WriteVInt32(PackedInt32s.VERSION_CURRENT);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Reset()
        {
            blockChunks = 0;
            blockDocs = 0;
            firstStartPointer = -1; // means unset
        }

        private void WriteBlock()
        {
            if (Debugging.AssertsEnabled) Debugging.Assert(blockChunks > 0);
            fieldsIndexOut.WriteVInt32(blockChunks);

            // The trick here is that we only store the difference from the average start
            // pointer or doc base, this helps save bits per value.
            // And in order to prevent a few chunks that would be far from the average to
            // raise the number of bits per value for all of them, we only encode blocks
            // of 1024 chunks at once
            // See LUCENE-4512

            // doc bases
            int avgChunkDocs;
            if (blockChunks == 1)
            {
                avgChunkDocs = 0;
            }
            else
            {
                avgChunkDocs = (int)Math.Round((float)(blockDocs - docBaseDeltas[blockChunks - 1]) / (blockChunks - 1));
            }
            fieldsIndexOut.WriteVInt32(totalDocs - blockDocs); // docBase
            fieldsIndexOut.WriteVInt32(avgChunkDocs);
            int docBase = 0;
            long maxDelta = 0;
            for (int i = 0; i < blockChunks; ++i)
            {
                int delta = docBase - avgChunkDocs * i;
                maxDelta |= MoveSignToLowOrderBit(delta);
                docBase += docBaseDeltas[i];
            }

            int bitsPerDocBase = PackedInt32s.BitsRequired(maxDelta);
            fieldsIndexOut.WriteVInt32(bitsPerDocBase);
            PackedInt32s.Writer writer = PackedInt32s.GetWriterNoHeader(fieldsIndexOut, PackedInt32s.Format.PACKED, blockChunks, bitsPerDocBase, 1);
            docBase = 0;
            for (int i = 0; i < blockChunks; ++i)
            {
                long delta = docBase - avgChunkDocs * i;
                if (Debugging.AssertsEnabled) Debugging.Assert(PackedInt32s.BitsRequired(MoveSignToLowOrderBit(delta)) <= writer.BitsPerValue);
                writer.Add(MoveSignToLowOrderBit(delta));
                docBase += docBaseDeltas[i];
            }
            writer.Finish();

            // start pointers
            fieldsIndexOut.WriteVInt64(firstStartPointer);
            long avgChunkSize;
            if (blockChunks == 1)
            {
                avgChunkSize = 0;
            }
            else
            {
                avgChunkSize = (maxStartPointer - firstStartPointer) / (blockChunks - 1);
            }
            fieldsIndexOut.WriteVInt64(avgChunkSize);
            long startPointer = 0;
            maxDelta = 0;
            for (int i = 0; i < blockChunks; ++i)
            {
                startPointer += startPointerDeltas[i];
                long delta = startPointer - avgChunkSize * i;
                maxDelta |= MoveSignToLowOrderBit(delta);
            }

            int bitsPerStartPointer = PackedInt32s.BitsRequired(maxDelta);
            fieldsIndexOut.WriteVInt32(bitsPerStartPointer);
            writer = PackedInt32s.GetWriterNoHeader(fieldsIndexOut, PackedInt32s.Format.PACKED, blockChunks, bitsPerStartPointer, 1);
            startPointer = 0;
            for (int i = 0; i < blockChunks; ++i)
            {
                startPointer += startPointerDeltas[i];
                long delta = startPointer - avgChunkSize * i;
                if (Debugging.AssertsEnabled) Debugging.Assert(PackedInt32s.BitsRequired(MoveSignToLowOrderBit(delta)) <= writer.BitsPerValue);
                writer.Add(MoveSignToLowOrderBit(delta));
            }
            writer.Finish();
        }

        internal void WriteIndex(int numDocs, long startPointer)
        {
            if (blockChunks == BLOCK_SIZE)
            {
                WriteBlock();
                Reset();
            }

            if (firstStartPointer == -1)
            {
                firstStartPointer = maxStartPointer = startPointer;
            }
            if (Debugging.AssertsEnabled) Debugging.Assert(firstStartPointer > 0 && startPointer >= firstStartPointer);

            docBaseDeltas[blockChunks] = numDocs;
            startPointerDeltas[blockChunks] = startPointer - maxStartPointer;

            ++blockChunks;
            blockDocs += numDocs;
            totalDocs += numDocs;
            maxStartPointer = startPointer;
        }

        internal void Finish(int numDocs, long maxPointer)
        {
            if (numDocs != totalDocs)
            {
                throw IllegalStateException.Create("Expected " + numDocs + " docs, but got " + totalDocs);
            }
            if (blockChunks > 0)
            {
                WriteBlock();
            }
            fieldsIndexOut.WriteVInt32(0); // end marker
            fieldsIndexOut.WriteVInt64(maxPointer);
            CodecUtil.WriteFooter(fieldsIndexOut);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispose()
        {
            fieldsIndexOut.Dispose();
        }
    }
}