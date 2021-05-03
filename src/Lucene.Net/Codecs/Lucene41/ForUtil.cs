using J2N.Numerics;
using Lucene.Net.Diagnostics;
using Lucene.Net.Store;
using Lucene.Net.Support;
using Lucene.Net.Util.Packed;
using System;
using System.IO;
using System.Runtime.CompilerServices;

namespace Lucene.Net.Codecs.Lucene41
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

    /// <summary>
    /// Encode all values in normal area with fixed bit width,
    /// which is determined by the max value in this block.
    /// </summary>
    internal sealed class ForUtil
    {
        /// <summary>
        /// Special number of bits per value used whenever all values to encode are equal.
        /// </summary>
        private const int ALL_VALUES_EQUAL = 0;

        /// <summary>
        /// Upper limit of the number of bytes that might be required to stored
        /// <see cref="Lucene41PostingsFormat.BLOCK_SIZE"/> encoded values.
        /// </summary>
        public static readonly int MAX_ENCODED_SIZE = Lucene41PostingsFormat.BLOCK_SIZE * 4;

        /// <summary>
        /// Upper limit of the number of values that might be decoded in a single call to
        /// <see cref="ReadBlock(IndexInput, byte[], int[])"/>. Although values after
        /// <see cref="Lucene41PostingsFormat.BLOCK_SIZE"/> are garbage, it is necessary to allocate value buffers
        /// whose size is &gt;= MAX_DATA_SIZE to avoid <see cref="IndexOutOfRangeException"/>s.
        /// </summary>
        public static readonly int MAX_DATA_SIZE = LoadMaxDataSize();

        private static int LoadMaxDataSize() // LUCENENET: Avoid static constructors (see https://github.com/apache/lucenenet/pull/224#issuecomment-469284006)
        {
            int maxDataSize = 0;
            for (int version = PackedInt32s.VERSION_START; version <= PackedInt32s.VERSION_CURRENT; version++)
            {
                foreach (PackedInt32s.Format format in PackedInt32s.Format.Values/* Enum.GetValues(typeof(PackedInts.Format))*/)
                {
                    for (int bpv = 1; bpv <= 32; ++bpv)
                    {
                        if (!format.IsSupported(bpv))
                        {
                            continue;
                        }
                        PackedInt32s.IDecoder decoder = PackedInt32s.GetDecoder(format, version, bpv);
                        int iterations = ComputeIterations(decoder);
                        maxDataSize = Math.Max(maxDataSize, iterations * decoder.ByteValueCount);
                    }
                }
            }
            return maxDataSize;
        }

        /// <summary>
        /// Compute the number of iterations required to decode <see cref="Lucene41PostingsFormat.BLOCK_SIZE"/>
        /// values with the provided <see cref="PackedInt32s.IDecoder"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int ComputeIterations(PackedInt32s.IDecoder decoder)
        {
            return (int)Math.Ceiling((float)Lucene41PostingsFormat.BLOCK_SIZE / decoder.ByteValueCount);
        }

        /// <summary>
        /// Compute the number of bytes required to encode a block of values that require
        /// <paramref name="bitsPerValue"/> bits per value with format <paramref name="format"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int EncodedSize(PackedInt32s.Format format, int packedIntsVersion, int bitsPerValue)
        {
            long byteCount = format.ByteCount(packedIntsVersion, Lucene41PostingsFormat.BLOCK_SIZE, bitsPerValue);
            if (Debugging.AssertsEnabled) Debugging.Assert(byteCount >= 0 && byteCount <= int.MaxValue, byteCount.ToString());
            return (int)byteCount;
        }

        private readonly int[] encodedSizes;
        private readonly PackedInt32s.IEncoder[] encoders;
        private readonly PackedInt32s.IDecoder[] decoders;
        private readonly int[] iterations;

        /// <summary>
        /// Create a new <see cref="ForUtil"/> instance and save state into <paramref name="out"/>.
        /// </summary>
        internal ForUtil(float acceptableOverheadRatio, DataOutput @out)
        {
            @out.WriteVInt32(PackedInt32s.VERSION_CURRENT);
            encodedSizes = new int[33];
            encoders = new PackedInt32s.IEncoder[33];
            decoders = new PackedInt32s.IDecoder[33];
            iterations = new int[33];

            for (int bpv = 1; bpv <= 32; ++bpv)
            {
                PackedInt32s.FormatAndBits formatAndBits = PackedInt32s.FastestFormatAndBits(Lucene41PostingsFormat.BLOCK_SIZE, bpv, acceptableOverheadRatio);
                if (Debugging.AssertsEnabled)
                {
                    Debugging.Assert(formatAndBits.Format.IsSupported(formatAndBits.BitsPerValue));
                    Debugging.Assert(formatAndBits.BitsPerValue <= 32);
                }
                encodedSizes[bpv] = EncodedSize(formatAndBits.Format, PackedInt32s.VERSION_CURRENT, formatAndBits.BitsPerValue);
                encoders[bpv] = PackedInt32s.GetEncoder(formatAndBits.Format, PackedInt32s.VERSION_CURRENT, formatAndBits.BitsPerValue);
                decoders[bpv] = PackedInt32s.GetDecoder(formatAndBits.Format, PackedInt32s.VERSION_CURRENT, formatAndBits.BitsPerValue);
                iterations[bpv] = ComputeIterations(decoders[bpv]);

                @out.WriteVInt32(formatAndBits.Format.Id << 5 | (formatAndBits.BitsPerValue - 1));
            }
        }

        /// <summary>
        /// Restore a <see cref="ForUtil"/> from a <see cref="DataInput"/>.
        /// </summary>
        internal ForUtil(DataInput @in)
        {
            int packedIntsVersion = @in.ReadVInt32();
            PackedInt32s.CheckVersion(packedIntsVersion);
            encodedSizes = new int[33];
            encoders = new PackedInt32s.IEncoder[33];
            decoders = new PackedInt32s.IDecoder[33];
            iterations = new int[33];

            for (int bpv = 1; bpv <= 32; ++bpv)
            {
                var code = @in.ReadVInt32();
                var formatId = code.TripleShift(5);
                var bitsPerValue = (code & 31) + 1;

                PackedInt32s.Format format = PackedInt32s.Format.ById(formatId);
                if (Debugging.AssertsEnabled) Debugging.Assert(format.IsSupported(bitsPerValue));
                encodedSizes[bpv] = EncodedSize(format, packedIntsVersion, bitsPerValue);
                encoders[bpv] = PackedInt32s.GetEncoder(format, packedIntsVersion, bitsPerValue);
                decoders[bpv] = PackedInt32s.GetDecoder(format, packedIntsVersion, bitsPerValue);
                iterations[bpv] = ComputeIterations(decoders[bpv]);
            }
        }

        /// <summary>
        /// Write a block of data (<c>For</c> format).
        /// </summary>
        /// <param name="data">     The data to write. </param>
        /// <param name="encoded">  A buffer to use to encode data. </param>
        /// <param name="out">      The destination output. </param>
        /// <exception cref="IOException"> If there is a low-level I/O error. </exception>
        internal void WriteBlock(int[] data, byte[] encoded, IndexOutput @out)
        {
            if (IsAllEqual(data))
            {
                @out.WriteByte((byte)ALL_VALUES_EQUAL);
                @out.WriteVInt32(data[0]);
                return;
            }

            int numBits = BitsRequired(data);
            if (Debugging.AssertsEnabled) Debugging.Assert(numBits > 0 && numBits <= 32, numBits.ToString());
            PackedInt32s.IEncoder encoder = encoders[numBits];
            int iters = iterations[numBits];
            if (Debugging.AssertsEnabled) Debugging.Assert(iters * encoder.ByteValueCount >= Lucene41PostingsFormat.BLOCK_SIZE);
            int encodedSize = encodedSizes[numBits];
            if (Debugging.AssertsEnabled) Debugging.Assert(iters * encoder.ByteBlockCount >= encodedSize);

            @out.WriteByte((byte)numBits);

            encoder.Encode(data, 0, encoded, 0, iters);
            @out.WriteBytes(encoded, encodedSize);
        }

        /// <summary>
        /// Read the next block of data (<c>For</c> format).
        /// </summary>
        /// <param name="in">        The input to use to read data. </param>
        /// <param name="encoded">   A buffer that can be used to store encoded data. </param>
        /// <param name="decoded">   Where to write decoded data. </param>
        /// <exception cref="IOException"> If there is a low-level I/O error. </exception>
        internal void ReadBlock(IndexInput @in, byte[] encoded, int[] decoded)
        {
            int numBits = @in.ReadByte();
            if (Debugging.AssertsEnabled) Debugging.Assert(numBits <= 32, numBits.ToString());

            if (numBits == ALL_VALUES_EQUAL)
            {
                int value = @in.ReadVInt32();
                Arrays.Fill(decoded, 0, Lucene41PostingsFormat.BLOCK_SIZE, value);
                return;
            }

            int encodedSize = encodedSizes[numBits];
            @in.ReadBytes(encoded, 0, encodedSize);

            PackedInt32s.IDecoder decoder = decoders[numBits];
            int iters = iterations[numBits];
            if (Debugging.AssertsEnabled) Debugging.Assert(iters * decoder.ByteValueCount >= Lucene41PostingsFormat.BLOCK_SIZE);

            decoder.Decode(encoded, 0, decoded, 0, iters);
        }

        /// <summary>
        /// Skip the next block of data.
        /// </summary>
        /// <param name="in">      The input where to read data. </param>
        /// <exception cref="IOException"> If there is a low-level I/O error. </exception>
        internal void SkipBlock(IndexInput @in)
        {
            int numBits = @in.ReadByte();
            if (numBits == ALL_VALUES_EQUAL)
            {
                @in.ReadVInt32();
                return;
            }
            if (Debugging.AssertsEnabled) Debugging.Assert(numBits > 0 && numBits <= 32, numBits.ToString());
            int encodedSize = encodedSizes[numBits];
            @in.Seek(@in.Position + encodedSize); // LUCENENET specific: Renamed from getFilePointer() to match FileStream
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

        /// <summary>
        /// Compute the number of bits required to serialize any of the longs in
        /// <paramref name="data"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int BitsRequired(int[] data)
        {
            long or = 0;
            for (int i = 0; i < Lucene41PostingsFormat.BLOCK_SIZE; ++i)
            {
                if (Debugging.AssertsEnabled) Debugging.Assert(data[i] >= 0);
                or |= (uint)data[i];
            }
            return PackedInt32s.BitsRequired(or);
        }
    }
}