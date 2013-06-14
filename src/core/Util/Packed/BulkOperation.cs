using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Util.Packed
{
    internal abstract class BulkOperation : PackedInts.IDecoder, PackedInts.IEncoder
    {
        private static readonly BulkOperation[] packedBulkOps = new BulkOperation[] {
            new BulkOperationPacked1(),
            new BulkOperationPacked2(),
            new BulkOperationPacked3(),
            new BulkOperationPacked4(),
            new BulkOperationPacked5(),
            new BulkOperationPacked6(),
            new BulkOperationPacked7(),
            new BulkOperationPacked8(),
            new BulkOperationPacked9(),
            new BulkOperationPacked10(),
            new BulkOperationPacked11(),
            new BulkOperationPacked12(),
            new BulkOperationPacked13(),
            new BulkOperationPacked14(),
            new BulkOperationPacked15(),
            new BulkOperationPacked16(),
            new BulkOperationPacked17(),
            new BulkOperationPacked18(),
            new BulkOperationPacked19(),
            new BulkOperationPacked20(),
            new BulkOperationPacked21(),
            new BulkOperationPacked22(),
            new BulkOperationPacked23(),
            new BulkOperationPacked24(),
            new BulkOperationPacked(25),
            new BulkOperationPacked(26),
            new BulkOperationPacked(27),
            new BulkOperationPacked(28),
            new BulkOperationPacked(29),
            new BulkOperationPacked(30),
            new BulkOperationPacked(31),
            new BulkOperationPacked(32),
            new BulkOperationPacked(33),
            new BulkOperationPacked(34),
            new BulkOperationPacked(35),
            new BulkOperationPacked(36),
            new BulkOperationPacked(37),
            new BulkOperationPacked(38),
            new BulkOperationPacked(39),
            new BulkOperationPacked(40),
            new BulkOperationPacked(41),
            new BulkOperationPacked(42),
            new BulkOperationPacked(43),
            new BulkOperationPacked(44),
            new BulkOperationPacked(45),
            new BulkOperationPacked(46),
            new BulkOperationPacked(47),
            new BulkOperationPacked(48),
            new BulkOperationPacked(49),
            new BulkOperationPacked(50),
            new BulkOperationPacked(51),
            new BulkOperationPacked(52),
            new BulkOperationPacked(53),
            new BulkOperationPacked(54),
            new BulkOperationPacked(55),
            new BulkOperationPacked(56),
            new BulkOperationPacked(57),
            new BulkOperationPacked(58),
            new BulkOperationPacked(59),
            new BulkOperationPacked(60),
            new BulkOperationPacked(61),
            new BulkOperationPacked(62),
            new BulkOperationPacked(63),
            new BulkOperationPacked(64),
        };

        // NOTE: this is sparse (some entries are null):
        private static readonly BulkOperation[] packedSingleBlockBulkOps = new BulkOperation[] {
            new BulkOperationPackedSingleBlock(1),
            new BulkOperationPackedSingleBlock(2),
            new BulkOperationPackedSingleBlock(3),
            new BulkOperationPackedSingleBlock(4),
            new BulkOperationPackedSingleBlock(5),
            new BulkOperationPackedSingleBlock(6),
            new BulkOperationPackedSingleBlock(7),
            new BulkOperationPackedSingleBlock(8),
            new BulkOperationPackedSingleBlock(9),
            new BulkOperationPackedSingleBlock(10),
            null,
            new BulkOperationPackedSingleBlock(12),
            null,
            null,
            null,
            new BulkOperationPackedSingleBlock(16),
            null,
            null,
            null,
            null,
            new BulkOperationPackedSingleBlock(21),
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            new BulkOperationPackedSingleBlock(32),
        };

        public static BulkOperation Of(PackedInts.Format format, int bitsPerValue)
        {
            if (format == PackedInts.Format.PACKED)
            {
                return packedBulkOps[bitsPerValue - 1];
            }
            else if (format == PackedInts.Format.PACKED_SINGLE_BLOCK)
            {
                return packedSingleBlockBulkOps[bitsPerValue - 1];
            }
            else
            {
                throw new ArgumentException();
            }
        }

        protected virtual int WriteLong(long block, sbyte[] blocks, int blocksOffset)
        {
            for (int j = 1; j <= 8; ++j)
            {
                blocks[blocksOffset++] = (sbyte)Number.URShift(block, (64 - (j << 3)));
            }
            return blocksOffset;
        }

        public int ComputeIterations(int valueCount, int ramBudget)
        {
            int iterations = ramBudget / (ByteBlockCount + 8 * ByteValueCount);
            if (iterations == 0)
            {
                // at least 1
                return 1;
            }
            else if ((iterations - 1) * ByteValueCount >= valueCount)
            {
                // don't allocate for more than the size of the reader
                return (int)Math.Ceiling((double)valueCount / ByteValueCount);
            }
            else
            {
                return iterations;
            }
        }

        public abstract int LongBlockCount { get; }

        public abstract int LongValueCount { get; }

        public abstract int ByteBlockCount { get; }

        public abstract int ByteValueCount { get; }

        public abstract void Decode(long[] blocks, int blocksOffset, long[] values, int valuesOffset, int iterations);

        public abstract void Decode(sbyte[] blocks, int blocksOffset, long[] values, int valuesOffset, int iterations);

        public abstract void Decode(long[] blocks, int blocksOffset, int[] values, int valuesOffset, int iterations);

        public abstract void Decode(sbyte[] blocks, int blocksOffset, int[] values, int valuesOffset, int iterations);

        public abstract void Encode(long[] values, int valuesOffset, long[] blocks, int blocksOffset, int iterations);

        public abstract void Encode(long[] values, int valuesOffset, sbyte[] blocks, int blocksOffset, int iterations);

        public abstract void Encode(int[] values, int valuesOffset, long[] blocks, int blocksOffset, int iterations);

        public abstract void Encode(int[] values, int valuesOffset, sbyte[] blocks, int blocksOffset, int iterations);
    }
}
