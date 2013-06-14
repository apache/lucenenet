using Lucene.Net.Store;
using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Util.Packed
{
    internal abstract class AbstractBlockPackedWriter
    {
        internal const int MAX_BLOCK_SIZE = 1 << (30 - 3);
        internal const int MIN_VALUE_EQUALS_0 = 1 << 0;
        internal const int BPV_SHIFT = 1;

        internal static void CheckBlockSize(int blockSize)
        {
            if (blockSize <= 0 || blockSize > MAX_BLOCK_SIZE)
            {
                throw new ArgumentException("blockSize must be > 0 and < " + MAX_BLOCK_SIZE + ", got " + blockSize);
            }
            if (blockSize < 64)
            {
                throw new ArgumentException("blockSize must be >= 64, got " + blockSize);
            }
            if ((blockSize & (blockSize - 1)) != 0)
            {
                throw new ArgumentException("blockSize must be a power of two, got " + blockSize);
            }
        }

        internal static long ZigZagEncode(long n)
        {
            return (n >> 63) ^ (n << 1);
        }

        // same as DataOutput.writeVLong but accepts negative values
        internal static void WriteVLong(DataOutput output, long i)
        {
            int k = 0;
            while ((i & ~0x7FL) != 0L && k++ < 8)
            {
                output.WriteByte((byte)((i & 0x7FL) | 0x80L));
                i = Number.URShift(i, 7);
            }
            output.WriteByte((byte)i);
        }

        protected DataOutput output;
        protected readonly long[] values;
        protected sbyte[] blocks;
        protected int off;
        protected long ord;
        protected bool finished;

        public AbstractBlockPackedWriter(DataOutput output, int blockSize)
        {
            CheckBlockSize(blockSize);
            Reset(output);
            values = new long[blockSize];
        }

        public virtual void Reset(DataOutput output)
        {
            //assert out != null;
            this.output = output;
            off = 0;
            ord = 0L;
            finished = false;
        }

        private void CheckNotFinished()
        {
            if (finished)
            {
                throw new InvalidOperationException("Already finished");
            }
        }

        public virtual void Add(long l)
        {
            CheckNotFinished();
            if (off == values.Length)
            {
                Flush();
            }
            values[off++] = l;
            ++ord;
        }

        internal virtual void AddBlockOfZeros()
        {
            CheckNotFinished();
            if (off != 0 && off != values.Length)
            {
                throw new InvalidOperationException("" + off);
            }
            if (off == values.Length)
            {
                Flush();
            }
            Arrays.Fill(values, 0);
            off = values.Length;
            ord += values.Length;
        }

        public virtual void Finish()
        {
            CheckNotFinished();
            if (off > 0)
            {
                Flush();
            }
            finished = true;
        }

        public virtual long Ord
        {
            get { return ord; }
        }

        protected abstract void Flush();

        protected void WriteValues(int bitsRequired)
        {
            PackedInts.IEncoder encoder = PackedInts.GetEncoder(PackedInts.Format.PACKED, PackedInts.VERSION_CURRENT, bitsRequired);
            int iterations = values.Length / encoder.ByteValueCount;
            int blockSize = encoder.ByteBlockCount * iterations;
            if (blocks == null || blocks.Length < blockSize)
            {
                blocks = new sbyte[blockSize];
            }
            if (off < values.Length)
            {
                Arrays.Fill(values, off, values.Length, 0L);
            }
            encoder.Encode(values, 0, blocks, 0, iterations);
            int blockCount = (int)PackedInts.Format.PACKED.ByteCount(PackedInts.VERSION_CURRENT, off, bitsRequired);
            output.WriteBytes(blocks, blockCount);
        }
    }
}
