using Lucene.Net.Store;
using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Util.Packed
{
    public sealed class BlockPackedReaderIterator
    {
        internal static long ZigZagDecode(long n)
        {
            return (Number.URShift(n, 1) ^ -(n & 1));
        }

        internal static long ReadVLong(DataInput input)
        {
            byte b = input.ReadByte();
            if (b >= 0) return b;
            long i = b & 0x7FL;
            b = input.ReadByte();
            i |= (b & 0x7FL) << 7;
            if (b >= 0) return i;
            b = input.ReadByte();
            i |= (b & 0x7FL) << 14;
            if (b >= 0) return i;
            b = input.ReadByte();
            i |= (b & 0x7FL) << 21;
            if (b >= 0) return i;
            b = input.ReadByte();
            i |= (b & 0x7FL) << 28;
            if (b >= 0) return i;
            b = input.ReadByte();
            i |= (b & 0x7FL) << 35;
            if (b >= 0) return i;
            b = input.ReadByte();
            i |= (b & 0x7FL) << 42;
            if (b >= 0) return i;
            b = input.ReadByte();
            i |= (b & 0x7FL) << 49;
            if (b >= 0) return i;
            b = input.ReadByte();
            i |= (b & 0xFFL) << 56;
            return i;
        }

        internal DataInput input;
        internal readonly int packedIntsVersion;
        internal long valueCount;
        internal readonly int blockSize;
        internal readonly long[] values;
        internal readonly LongsRef valuesRef;
        internal sbyte[] blocks;
        internal int off;
        internal long ord;

        public BlockPackedReaderIterator(DataInput input, int packedIntsVersion, int blockSize, long valueCount)
        {
            BlockPackedWriter.CheckBlockSize(blockSize);
            this.packedIntsVersion = packedIntsVersion;
            this.blockSize = blockSize;
            this.values = new long[blockSize];
            this.valuesRef = new LongsRef(this.values, 0, 0);
            Reset(input, valueCount);
        }

        public void Reset(DataInput input, long valueCount)
        {
            this.input = input;
            //assert valueCount >= 0;
            this.valueCount = valueCount;
            off = blockSize;
            ord = 0;
        }

        public void Skip(long count)
        {
            //assert count >= 0;
            if (ord + count > valueCount || ord + count < 0)
            {
                throw new System.IO.EndOfStreamException();
            }

            // 1. skip buffered values
            int skipBuffer = (int)Math.Min(count, blockSize - off);
            off += skipBuffer;
            ord += skipBuffer;
            count -= skipBuffer;
            if (count == 0L)
            {
                return;
            }

            // 2. skip as many blocks as necessary
            //assert off == blockSize;
            while (count >= blockSize)
            {
                int token = input.ReadByte() & 0xFF;
                int bitsPerValue = Number.URShift(token, BlockPackedWriter.BPV_SHIFT);
                if (bitsPerValue > 64)
                {
                    throw new System.IO.IOException("Corrupted");
                }
                if ((token & BlockPackedWriter.MIN_VALUE_EQUALS_0) == 0)
                {
                    ReadVLong(input);
                }
                long blockBytes = PackedInts.Format.PACKED.ByteCount(packedIntsVersion, blockSize, bitsPerValue);
                SkipBytes(blockBytes);
                ord += blockSize;
                count -= blockSize;
            }
            if (count == 0L)
            {
                return;
            }

            // 3. skip last values
            //assert count < blockSize;
            Refill();
            ord += count;
            off += (int)count;
        }

        private void SkipBytes(long count)
        {
            if (input is IndexInput)
            {
                IndexInput iin = (IndexInput)input;
                iin.Seek(iin.FilePointer + count);
            }
            else
            {
                if (blocks == null)
                {
                    blocks = new sbyte[blockSize];
                }
                long skipped = 0;
                while (skipped < count)
                {
                    int toSkip = (int)Math.Min(blocks.Length, count - skipped);
                    input.ReadBytes(blocks, 0, toSkip);
                    skipped += toSkip;
                }
            }
        }

        public long Next()
        {
            if (ord == valueCount)
            {
                throw new System.IO.EndOfStreamException();
            }
            if (off == blockSize)
            {
                Refill();
            }
            long value = values[off++];
            ++ord;
            return value;
        }

        public LongsRef Next(int count)
        {
            //assert count > 0;
            if (ord == valueCount)
            {
                throw new System.IO.EndOfStreamException();
            }
            if (off == blockSize)
            {
                Refill();
            }

            count = Math.Min(count, blockSize - off);
            count = (int)Math.Min(count, valueCount - ord);

            valuesRef.offset = off;
            valuesRef.length = count;
            off += count;
            ord += count;
            return valuesRef;
        }

        private void Refill()
        {
            int token = input.ReadByte() & 0xFF;
            bool minEquals0 = (token & BlockPackedWriter.MIN_VALUE_EQUALS_0) != 0;
            int bitsPerValue = Number.URShift(token, BlockPackedWriter.BPV_SHIFT);
            if (bitsPerValue > 64)
            {
                throw new System.IO.IOException("Corrupted");
            }
            long minValue = minEquals0 ? 0L : ZigZagDecode(1L + ReadVLong(input));
            //assert minEquals0 || minValue != 0;

            if (bitsPerValue == 0)
            {
                Arrays.Fill(values, minValue);
            }
            else
            {
                PackedInts.IDecoder decoder = PackedInts.GetDecoder(PackedInts.Format.PACKED, packedIntsVersion, bitsPerValue);
                int iterations = blockSize / decoder.ByteValueCount;
                int blocksSize = iterations * decoder.ByteBlockCount;
                if (blocks == null || blocks.Length < blocksSize)
                {
                    blocks = new sbyte[blocksSize];
                }

                int valueCount = (int)Math.Min(this.valueCount - ord, blockSize);
                int blocksCount = (int)PackedInts.Format.PACKED.ByteCount(packedIntsVersion, valueCount, bitsPerValue);
                input.ReadBytes(blocks, 0, blocksCount);

                decoder.Decode(blocks, 0, values, 0, iterations);

                if (minValue != 0)
                {
                    for (int i = 0; i < valueCount; ++i)
                    {
                        values[i] += minValue;
                    }
                }
            }
            off = 0;
        }

        public long Ord
        {
            get { return ord; }
        }
    }
}
