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
            fieldsIndexOut.WriteVInt(PackedInts.VERSION_CURRENT);
        }

        private void Reset()
        {
            blockChunks = 0;
            blockDocs = 0;
            firstStartPointer = -1; // means unset
        }

        private void WriteBlock()
        {
            Debug.Assert(blockChunks > 0);
            fieldsIndexOut.WriteVInt(blockChunks);

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
            fieldsIndexOut.WriteVInt(totalDocs - blockDocs); // docBase
            fieldsIndexOut.WriteVInt(avgChunkDocs);
            int docBase = 0;
            long maxDelta = 0;
            for (int i = 0; i < blockChunks; ++i)
            {
                int delta = docBase - avgChunkDocs * i;
                maxDelta |= MoveSignToLowOrderBit(delta);
                docBase += docBaseDeltas[i];
            }

            int bitsPerDocBase = PackedInts.BitsRequired(maxDelta);
            fieldsIndexOut.WriteVInt(bitsPerDocBase);
            PackedInts.Writer writer = PackedInts.GetWriterNoHeader(fieldsIndexOut, PackedInts.Format.PACKED, blockChunks, bitsPerDocBase, 1);
            docBase = 0;
            for (int i = 0; i < blockChunks; ++i)
            {
                long delta = docBase - avgChunkDocs * i;
                Debug.Assert(PackedInts.BitsRequired(MoveSignToLowOrderBit(delta)) <= writer.BitsPerValue);
                writer.Add(MoveSignToLowOrderBit(delta));
                docBase += docBaseDeltas[i];
            }
            writer.Finish();

            // start pointers
            fieldsIndexOut.WriteVLong(firstStartPointer);
            long avgChunkSize;
            if (blockChunks == 1)
            {
                avgChunkSize = 0;
            }
            else
            {
                avgChunkSize = (maxStartPointer - firstStartPointer) / (blockChunks - 1);
            }
            fieldsIndexOut.WriteVLong(avgChunkSize);
            long startPointer = 0;
            maxDelta = 0;
            for (int i = 0; i < blockChunks; ++i)
            {
                startPointer += startPointerDeltas[i];
                long delta = startPointer - avgChunkSize * i;
                maxDelta |= MoveSignToLowOrderBit(delta);
            }

            int bitsPerStartPointer = PackedInts.BitsRequired(maxDelta);
            fieldsIndexOut.WriteVInt(bitsPerStartPointer);
            writer = PackedInts.GetWriterNoHeader(fieldsIndexOut, PackedInts.Format.PACKED, blockChunks, bitsPerStartPointer, 1);
            startPointer = 0;
            for (int i = 0; i < blockChunks; ++i)
            {
                startPointer += startPointerDeltas[i];
                long delta = startPointer - avgChunkSize * i;
                Debug.Assert(PackedInts.BitsRequired(MoveSignToLowOrderBit(delta)) <= writer.BitsPerValue);
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
            Debug.Assert(firstStartPointer > 0 && startPointer >= firstStartPointer);

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
                throw new Exception("Expected " + numDocs + " docs, but got " + totalDocs);
            }
            if (blockChunks > 0)
            {
                WriteBlock();
            }
            fieldsIndexOut.WriteVInt(0); // end marker
            fieldsIndexOut.WriteVLong(maxPointer);
            CodecUtil.WriteFooter(fieldsIndexOut);
        }

        public void Dispose()
        {
            fieldsIndexOut.Dispose();
        }
    }
}