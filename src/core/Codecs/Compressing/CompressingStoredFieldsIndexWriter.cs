using Lucene.Net.Store;
using Lucene.Net.Util.Packed;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Codecs.Compressing
{
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
                //hackmp - TODO - This needs review.  The function as a whole is desgined with an int as the core value,
                //including contracts on other methods.  I NEVER like casting from double to int, but for now...
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
            PackedInts.Writer writer = PackedInts.GetWriterNoHeader(fieldsIndexOut,
                PackedInts.Format.PACKED, blockChunks, bitsPerDocBase, 1);
            docBase = 0;
            for (int i = 0; i < blockChunks; ++i)
            {
                long delta = docBase - avgChunkDocs * i;
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
            writer = PackedInts.GetWriterNoHeader(fieldsIndexOut, PackedInts.Format.PACKED,
                blockChunks, bitsPerStartPointer, 1);
            startPointer = 0;
            for (int i = 0; i < blockChunks; ++i)
            {
                startPointer += startPointerDeltas[i];
                long delta = startPointer - avgChunkSize * i;
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

            docBaseDeltas[blockChunks] = numDocs;
            startPointerDeltas[blockChunks] = startPointer - maxStartPointer;

            ++blockChunks;
            blockDocs += numDocs;
            totalDocs += numDocs;
            maxStartPointer = startPointer;
        }

        internal void Finish(int numDocs)
        {
            if (numDocs != totalDocs)
            {
                throw new ArgumentOutOfRangeException("Expected " + numDocs + " docs, but got " + totalDocs);
            }
            if (blockChunks > 0)
            {
                WriteBlock();
            }
            fieldsIndexOut.WriteVInt(0); // end marker
        }

        public void Dispose()
        {
            fieldsIndexOut.Dispose();
        }
    }
}
