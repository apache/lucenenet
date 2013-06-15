using Lucene.Net.Store;
using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Util.Packed
{
    public sealed class PackedWriter : PackedInts.Writer
    {
        internal bool finished;
        internal readonly PackedInts.Format format;
        internal readonly BulkOperation encoder;
        internal readonly sbyte[] nextBlocks;
        internal readonly long[] nextValues;
        internal readonly int iterations;
        internal int off;
        internal int written;

        public PackedWriter(PackedInts.Format format, DataOutput output, int valueCount, int bitsPerValue, int mem)
            : base(output, valueCount, bitsPerValue)
        {
            this.format = format;
            encoder = BulkOperation.Of(format, bitsPerValue);
            iterations = encoder.ComputeIterations(valueCount, mem);
            nextBlocks = new sbyte[iterations * encoder.ByteBlockCount];
            nextValues = new long[iterations * encoder.ByteValueCount];
            off = 0;
            written = 0;
            finished = false;
        }

        protected override PackedInts.Format Format
        {
            get { return format; }
        }

        public override void Add(long v)
        {
            //assert bitsPerValue == 64 || (v >= 0 && v <= PackedInts.maxValue(bitsPerValue)) : bitsPerValue;
            //assert !finished;
            if (valueCount != -1 && written >= valueCount)
            {
                throw new System.IO.EndOfStreamException("Writing past end of stream");
            }
            nextValues[off++] = v;
            if (off == nextValues.Length)
            {
                Flush();
            }
            ++written;
        }

        public override void Finish()
        {
            //assert !finished;
            if (valueCount != -1)
            {
                while (written < valueCount)
                {
                    Add(0L);
                }
            }
            Flush();
            finished = true;
        }

        private void Flush()
        {
            encoder.Encode(nextValues, 0, nextBlocks, 0, iterations);
            int blockCount = (int)format.ByteCount(PackedInts.VERSION_CURRENT, off, bitsPerValue);
            output.WriteBytes(nextBlocks, blockCount);
            Arrays.Fill(nextValues, 0L);
            off = 0;
        }

        public override int Ord()
        {
            return written - 1;
        }
    }
}
