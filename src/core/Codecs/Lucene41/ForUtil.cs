using Lucene.Net.Store;
using Lucene.Net.Support;
using Lucene.Net.Util.Packed;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Codecs.Lucene41
{
    internal sealed class ForUtil
    {
        private const int ALL_VALUES_EQUAL = 0;

        internal const int MAX_ENCODED_SIZE = Lucene41PostingsFormat.BLOCK_SIZE * 4;

        internal static readonly int MAX_DATA_SIZE;

        static ForUtil()
        {
            int maxDataSize = 0;
            for (int version = PackedInts.VERSION_START; version <= PackedInts.VERSION_CURRENT; version++)
            {
                foreach (PackedInts.Format format in PackedInts.Format.Values())
                {
                    for (int bpv = 1; bpv <= 32; ++bpv)
                    {
                        if (!format.IsSupported(bpv))
                        {
                            continue;
                        }
                        PackedInts.IDecoder decoder = PackedInts.GetDecoder(format, version, bpv);
                        int iterations = ComputeIterations(decoder);
                        maxDataSize = Math.Max(maxDataSize, iterations * decoder.ByteValueCount);
                    }
                }
            }
            MAX_DATA_SIZE = maxDataSize;
        }

        private static int ComputeIterations(PackedInts.IDecoder decoder)
        {
            return (int)Math.Ceiling((float)Lucene41PostingsFormat.BLOCK_SIZE / decoder.ByteValueCount);
        }

        private static int EncodedSize(PackedInts.Format format, int packedIntsVersion, int bitsPerValue)
        {
            long byteCount = format.ByteCount(packedIntsVersion, Lucene41PostingsFormat.BLOCK_SIZE, bitsPerValue);
            //assert byteCount >= 0 && byteCount <= Integer.MAX_VALUE : byteCount;
            return (int)byteCount;
        }

        private readonly int[] encodedSizes;
        private readonly PackedInts.IEncoder[] encoders;
        private readonly PackedInts.IDecoder[] decoders;
        private readonly int[] iterations;

        internal ForUtil(float acceptableOverheadRatio, DataOutput output)
        {
            output.WriteVInt(PackedInts.VERSION_CURRENT);
            encodedSizes = new int[33];
            encoders = new PackedInts.IEncoder[33];
            decoders = new PackedInts.IDecoder[33];
            iterations = new int[33];

            for (int bpv = 1; bpv <= 32; ++bpv)
            {
                PackedInts.FormatAndBits formatAndBits = PackedInts.FastestFormatAndBits(
                    Lucene41PostingsFormat.BLOCK_SIZE, bpv, acceptableOverheadRatio);
                //assert formatAndBits.format.isSupported(formatAndBits.bitsPerValue);
                //assert formatAndBits.bitsPerValue <= 32;
                encodedSizes[bpv] = EncodedSize(formatAndBits.Format, PackedInts.VERSION_CURRENT, formatAndBits.BitsPerValue);
                encoders[bpv] = PackedInts.GetEncoder(
                    formatAndBits.Format, PackedInts.VERSION_CURRENT, formatAndBits.BitsPerValue);
                decoders[bpv] = PackedInts.GetDecoder(
                    formatAndBits.Format, PackedInts.VERSION_CURRENT, formatAndBits.BitsPerValue);
                iterations[bpv] = ComputeIterations(decoders[bpv]);

                output.WriteVInt(formatAndBits.Format.GetId() << 5 | (formatAndBits.BitsPerValue - 1));
            }
        }

        internal ForUtil(DataInput input)
        {
            int packedIntsVersion = input.ReadVInt();
            PackedInts.CheckVersion(packedIntsVersion);
            encodedSizes = new int[33];
            encoders = new PackedInts.IEncoder[33];
            decoders = new PackedInts.IDecoder[33];
            iterations = new int[33];

            for (int bpv = 1; bpv <= 32; ++bpv)
            {
                int code = input.ReadVInt();
                int formatId = Number.URShift(code, 5);
                int bitsPerValue = (code & 31) + 1;

                PackedInts.Format format = PackedInts.Format.ById(formatId);
                //assert format.isSupported(bitsPerValue);
                encodedSizes[bpv] = EncodedSize(format, packedIntsVersion, bitsPerValue);
                encoders[bpv] = PackedInts.GetEncoder(
                    format, packedIntsVersion, bitsPerValue);
                decoders[bpv] = PackedInts.GetDecoder(
                    format, packedIntsVersion, bitsPerValue);
                iterations[bpv] = ComputeIterations(decoders[bpv]);
            }
        }

        internal void WriteBlock(int[] data, sbyte[] encoded, IndexOutput output)
        {
            if (IsAllEqual(data))
            {
                output.WriteByte((byte)ALL_VALUES_EQUAL);
                output.WriteVInt(data[0]);
                return;
            }

            int numBits = BitsRequired(data);
            //assert numBits > 0 && numBits <= 32 : numBits;
            PackedInts.IEncoder encoder = encoders[numBits];
            int iters = iterations[numBits];
            //assert iters * encoder.byteValueCount() >= BLOCK_SIZE;
            int encodedSize = encodedSizes[numBits];
            //assert iters * encoder.byteBlockCount() >= encodedSize;

            output.WriteByte((byte)numBits);

            encoder.Encode(data, 0, encoded, 0, iters);
            output.WriteBytes(encoded, encodedSize);
        }

        internal void ReadBlock(IndexInput input, sbyte[] encoded, int[] decoded)
        {
            int numBits = input.ReadByte();
            //assert numBits <= 32 : numBits;

            if (numBits == ALL_VALUES_EQUAL)
            {
                int value = input.ReadVInt();
                Arrays.Fill(decoded, 0, Lucene41PostingsFormat.BLOCK_SIZE, value);
                return;
            }

            int encodedSize = encodedSizes[numBits];
            input.ReadBytes(encoded, 0, encodedSize);

            PackedInts.IDecoder decoder = decoders[numBits];
            int iters = iterations[numBits];
            //assert iters * decoder.byteValueCount() >= BLOCK_SIZE;

            decoder.Decode(encoded, 0, decoded, 0, iters);
        }

        internal void SkipBlock(IndexInput input)
        {
            int numBits = input.ReadByte();
            if (numBits == ALL_VALUES_EQUAL)
            {
                input.ReadVInt();
                return;
            }
            //assert numBits > 0 && numBits <= 32 : numBits;
            int encodedSize = encodedSizes[numBits];
            input.Seek(input.FilePointer + encodedSize);
        }

        private static bool IsAllEqual(int[] data)
        {
            int v = data[0];
            for (int i = 1; i < Lucene41PostingsFormat.BLOCK_SIZE; ++i)
            {
                if (data[i] != v)
                {
                    return false;
                }
            }
            return true;
        }

        private static int BitsRequired(int[] data)
        {
            long or = 0;
            for (int i = 0; i < Lucene41PostingsFormat.BLOCK_SIZE; ++i)
            {
                //assert data[i] >= 0;
                or |= (uint)data[i];
            }
            return PackedInts.BitsRequired(or);
        }
    }
}
