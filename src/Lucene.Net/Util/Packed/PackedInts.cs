using J2N.Numerics;
using Lucene.Net.Diagnostics;
using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;

namespace Lucene.Net.Util.Packed
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

    using CodecUtil = Lucene.Net.Codecs.CodecUtil;
    using DataInput = Lucene.Net.Store.DataInput;
    using DataOutput = Lucene.Net.Store.DataOutput;
    using IndexInput = Lucene.Net.Store.IndexInput;
    using NumericDocValues = Lucene.Net.Index.NumericDocValues;

    /// <summary>
    /// Simplistic compression for array of unsigned long values.
    /// Each value is >= 0 and &lt;= a specified maximum value.  The
    /// values are stored as packed ints, with each value
    /// consuming a fixed number of bits.
    /// <para/>
    /// NOTE: This was PackedInts in Lucene.
    /// <para/>
    /// @lucene.internal
    /// </summary>
    public static class PackedInt32s // LUCENENET specific: CA1052 Static holder types should be Static or NotInheritable
    {
        /// <summary>
        /// At most 700% memory overhead, always select a direct implementation.
        /// </summary>
        public static float FASTEST = 7f;

        /// <summary>
        /// At most 50% memory overhead, always select a reasonably fast implementation.
        /// </summary>
        public static float FAST = 0.5f;

        /// <summary>
        /// At most 20% memory overhead.
        /// </summary>
        public static float DEFAULT = 0.2f;

        /// <summary>
        /// No memory overhead at all, but the returned implementation may be slow.
        /// </summary>
        public static float COMPACT = 0f;

        /// <summary>
        /// Default amount of memory to use for bulk operations.
        /// </summary>
        public static int DEFAULT_BUFFER_SIZE = 1024; // 1K

        public static string CODEC_NAME = "PackedInts";
        public static int VERSION_START = 0; // PackedInts were long-aligned
        public static int VERSION_BYTE_ALIGNED = 1;
        public static int VERSION_CURRENT = VERSION_BYTE_ALIGNED;

        /// <summary>
        /// Check the validity of a version number.
        /// </summary>
        public static void CheckVersion(int version)
        {
            if (version < VERSION_START)
            {
                throw new ArgumentOutOfRangeException(nameof(version), "Version is too old, should be at least " + VERSION_START + " (got " + version + ")"); // LUCENENET specific - changed from IllegalArgumentException to ArgumentOutOfRangeException (.NET convention)
            }
            else if (version > VERSION_CURRENT)
            {
                throw new ArgumentOutOfRangeException(nameof(version), "Version is too new, should be at most " + VERSION_CURRENT + " (got " + version + ")"); // LUCENENET specific - changed from IllegalArgumentException to ArgumentOutOfRangeException (.NET convention)
            }
        }

        private sealed class PackedFormat : Format
        {
            public PackedFormat()
                : base(0)
            {
            }

            /// <summary>
            /// Computes how many <see cref="byte"/> blocks are needed to store <paramref name="valueCount"/>
            /// values of size <paramref name="bitsPerValue"/>.
            /// </summary>
            public override long ByteCount(int packedIntsVersion, int valueCount, int bitsPerValue)
            {
                if (packedIntsVersion < VERSION_BYTE_ALIGNED)
                {
                    return 8L * (long)Math.Ceiling((double)valueCount * bitsPerValue / 64);
                }
                return (long)Math.Ceiling((double)valueCount * bitsPerValue / 8);
            }
        }

        private sealed class PackedSingleBlockFormat : Format
        {
            public PackedSingleBlockFormat()
                : base(1)
            {
            }

            /// <summary>
            /// Computes how many <see cref="long"/> blocks are needed to store <paramref name="valueCount"/>
            /// values of size <paramref name="bitsPerValue"/>.
            /// <para/>
            /// NOTE: This was longCount() in Lucene.
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public override int Int64Count(int packedIntsVersion, int valueCount, int bitsPerValue)
            {
                int valuesPerBlock = 64 / bitsPerValue;
                return (int)Math.Ceiling((double)valueCount / valuesPerBlock);
            }

            /// <summary>
            /// Tests whether the provided number of bits per value is supported by the
            /// format.
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public override bool IsSupported(int bitsPerValue)
            {
                return Packed64SingleBlock.IsSupported(bitsPerValue);
            }

            /// <summary>
            /// Returns the overhead per value, in bits.
            /// </summary>
            public override float OverheadPerValue(int bitsPerValue)
            {
                if (Debugging.AssertsEnabled) Debugging.Assert(IsSupported(bitsPerValue));
                int valuesPerBlock = 64 / bitsPerValue;
                int overhead = 64 % bitsPerValue;
                return (float)overhead / valuesPerBlock;
            }
        }

        /// <summary>
        /// A format to write packed <see cref="int"/>s.
        /// <para/>
        /// @lucene.internal
        /// </summary>
        public class Format
        {
            /// <summary>
            /// Compact format, all bits are written contiguously.
            /// </summary>
            public static readonly Format PACKED = new PackedFormat();

            /// <summary>
            /// A format that may insert padding bits to improve encoding and decoding
            /// speed. Since this format doesn't support all possible bits per value, you
            /// should never use it directly, but rather use
            /// <see cref="PackedInt32s.FastestFormatAndBits(int, int, float)"/> to find the
            /// format that best suits your needs.
            /// </summary>
            public static readonly Format PACKED_SINGLE_BLOCK = new PackedSingleBlockFormat();

            private static readonly Format[] values = new Format[] { PACKED, PACKED_SINGLE_BLOCK };

            public static IEnumerable<Format> Values => values;

            /// <summary>
            /// Get a format according to its ID.
            /// </summary>
            public static Format ById(int id)
            {
                foreach (Format format in Values)
                {
                    if (format.Id == id)
                    {
                        return format;
                    }
                }
                throw new ArgumentException("Unknown format id: " + id);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal Format(int id)
            {
                this.Id = id;
            }

            /// <summary>
            /// Returns the ID of the format.
            /// </summary>
            public int Id { get; private set; }

            /// <summary>
            /// Computes how many <see cref="byte"/> blocks are needed to store <paramref name="valueCount"/>
            /// values of size <paramref name="bitsPerValue"/>.
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public virtual long ByteCount(int packedIntsVersion, int valueCount, int bitsPerValue)
            {
                if (Debugging.AssertsEnabled) Debugging.Assert(bitsPerValue >= 0 && bitsPerValue <= 64, "{0}", bitsPerValue);
                // assume long-aligned
                return 8L * Int64Count(packedIntsVersion, valueCount, bitsPerValue);
            }

            /// <summary>
            /// Computes how many <see cref="long"/> blocks are needed to store <paramref name="valueCount"/>
            /// values of size <paramref name="bitsPerValue"/>.
            /// <para/>
            /// NOTE: This was longCount() in Lucene.
            /// </summary>
            public virtual int Int64Count(int packedIntsVersion, int valueCount, int bitsPerValue)
            {
                if (Debugging.AssertsEnabled) Debugging.Assert(bitsPerValue >= 0 && bitsPerValue <= 64, "{0}", bitsPerValue);
                long byteCount = ByteCount(packedIntsVersion, valueCount, bitsPerValue);
                if (Debugging.AssertsEnabled) Debugging.Assert(byteCount < 8L * int.MaxValue);
                if ((byteCount % 8) == 0)
                    return (int)(byteCount / 8);
                else
                    return (int)(byteCount / 8 + 1);
            }

            /// <summary>
            /// Tests whether the provided number of bits per value is supported by the
            /// format.
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public virtual bool IsSupported(int bitsPerValue)
            {
                return bitsPerValue >= 1 && bitsPerValue <= 64;
            }

            /// <summary>
            /// Returns the overhead per value, in bits.
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public virtual float OverheadPerValue(int bitsPerValue)
            {
                if (Debugging.AssertsEnabled) Debugging.Assert(IsSupported(bitsPerValue));
                return 0f;
            }

            /// <summary>
            /// Returns the overhead ratio (<c>overhead per value / bits per value</c>).
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public virtual float OverheadRatio(int bitsPerValue)
            {
                if (Debugging.AssertsEnabled) Debugging.Assert(IsSupported(bitsPerValue));
                return OverheadPerValue(bitsPerValue) / bitsPerValue;
            }
        }

        /// <summary>
        /// Simple class that holds a format and a number of bits per value.
        /// </summary>
        public class FormatAndBits
        {
            public Format Format { get; private set; }
            public int BitsPerValue { get; private set; }

            public FormatAndBits(Format format, int bitsPerValue)
            {
                this.Format = format;
                this.BitsPerValue = bitsPerValue;
            }

            public override string ToString()
            {
                return "FormatAndBits(format=" + Format + " bitsPerValue=" + BitsPerValue + ")";
            }
        }

        /// <summary>
        /// Try to find the <see cref="Format"/> and number of bits per value that would
        /// restore from disk the fastest reader whose overhead is less than
        /// <paramref name="acceptableOverheadRatio"/>.
        /// <para/>
        /// The <paramref name="acceptableOverheadRatio"/> parameter makes sense for
        /// random-access <see cref="Reader"/>s. In case you only plan to perform
        /// sequential access on this stream later on, you should probably use
        /// <see cref="PackedInt32s.COMPACT"/>.
        /// <para/>
        /// If you don't know how many values you are going to write, use
        /// <c><paramref name="valueCount"/> = -1</c>.
        /// </summary>
        public static FormatAndBits FastestFormatAndBits(int valueCount, int bitsPerValue, float acceptableOverheadRatio)
        {
            if (valueCount == -1)
            {
                valueCount = int.MaxValue;
            }

            acceptableOverheadRatio = Math.Max(COMPACT, acceptableOverheadRatio);
            acceptableOverheadRatio = Math.Min(FASTEST, acceptableOverheadRatio);
            float acceptableOverheadPerValue = acceptableOverheadRatio * bitsPerValue; // in bits

            int maxBitsPerValue = bitsPerValue + (int)acceptableOverheadPerValue;

            int actualBitsPerValue = -1;
            Format format = Format.PACKED;

            if (bitsPerValue <= 8 && maxBitsPerValue >= 8)
            {
                actualBitsPerValue = 8;
            }
            else if (bitsPerValue <= 16 && maxBitsPerValue >= 16)
            {
                actualBitsPerValue = 16;
            }
            else if (bitsPerValue <= 32 && maxBitsPerValue >= 32)
            {
                actualBitsPerValue = 32;
            }
            else if (bitsPerValue <= 64 && maxBitsPerValue >= 64)
            {
                actualBitsPerValue = 64;
            }
            else if (valueCount <= Packed8ThreeBlocks.MAX_SIZE && bitsPerValue <= 24 && maxBitsPerValue >= 24)
            {
                actualBitsPerValue = 24;
            }
            else if (valueCount <= Packed16ThreeBlocks.MAX_SIZE && bitsPerValue <= 48 && maxBitsPerValue >= 48)
            {
                actualBitsPerValue = 48;
            }
            else
            {
                for (int bpv = bitsPerValue; bpv <= maxBitsPerValue; ++bpv)
                {
                    if (Format.PACKED_SINGLE_BLOCK.IsSupported(bpv))
                    {
                        float overhead = Format.PACKED_SINGLE_BLOCK.OverheadPerValue(bpv);
                        float acceptableOverhead = acceptableOverheadPerValue + bitsPerValue - bpv;
                        // LUCENENET specific - compare bits rather than using equality operators to prevent these comparisons from failing in x86 in .NET Framework with optimizations enabled
                        if (NumericUtils.SingleToSortableInt32(overhead) <= NumericUtils.SingleToSortableInt32(acceptableOverhead))
                        {
                            actualBitsPerValue = bpv;
                            format = Format.PACKED_SINGLE_BLOCK;
                            break;
                        }
                    }
                }
                if (actualBitsPerValue < 0)
                {
                    actualBitsPerValue = bitsPerValue;
                }
            }

            return new FormatAndBits(format, actualBitsPerValue);
        }

        /// <summary>
        /// A decoder for packed integers.
        /// </summary>
        public interface IDecoder
        {
            /// <summary>
            /// The minimum number of <see cref="long"/> blocks to encode in a single iteration, when
            /// using long encoding.
            /// <para/>
            /// NOTE: This was longBlockCount() in Lucene.
            /// </summary>
            int Int64BlockCount { get; }

            /// <summary>
            /// The number of values that can be stored in <see cref="Int64BlockCount"/> <see cref="long"/>
            /// blocks.
            /// <para/>
            /// NOTE: This was longValueCount() in Lucene.
            /// </summary>
            int Int64ValueCount { get; }

            /// <summary>
            /// The minimum number of <see cref="byte"/> blocks to encode in a single iteration, when
            /// using byte encoding.
            /// </summary>
            int ByteBlockCount { get; }

            /// <summary>
            /// The number of values that can be stored in <see cref="ByteBlockCount"/> <see cref="byte"/>
            /// blocks.
            /// </summary>
            int ByteValueCount { get; }

            /// <summary>
            /// Read <c>iterations * BlockCount</c> blocks from <paramref name="blocks"/>,
            /// decode them and write <c>iterations * ValueCount</c> values into
            /// <paramref name="values"/>.
            /// </summary>
            /// <param name="blocks">       The long blocks that hold packed integer values. </param>
            /// <param name="blocksOffset"> The offset where to start reading blocks. </param>
            /// <param name="values">       The values buffer. </param>
            /// <param name="valuesOffset"> The offset where to start writing values. </param>
            /// <param name="iterations">   Controls how much data to decode. </param>
            void Decode(long[] blocks, int blocksOffset, long[] values, int valuesOffset, int iterations);

            /// <summary>
            /// Read <c>8 * iterations * BlockCount</c> blocks from <paramref name="blocks"/>,
            /// decode them and write <c>iterations * ValueCount</c> values into
            /// <paramref name="values"/>.
            /// </summary>
            /// <param name="blocks">       The long blocks that hold packed integer values. </param>
            /// <param name="blocksOffset"> The offset where to start reading blocks. </param>
            /// <param name="values">       The values buffer. </param>
            /// <param name="valuesOffset"> The offset where to start writing values. </param>
            /// <param name="iterations">   Controls how much data to decode. </param>
            void Decode(byte[] blocks, int blocksOffset, long[] values, int valuesOffset, int iterations);

            /// <summary>
            /// Read <c>iterations * BlockCount</c> blocks from <paramref name="blocks"/>,
            /// decode them and write <c>iterations * ValueCount</c> values into
            /// <paramref name="values"/>.
            /// </summary>
            /// <param name="blocks">       The long blocks that hold packed integer values. </param>
            /// <param name="blocksOffset"> The offset where to start reading blocks. </param>
            /// <param name="values">       The values buffer. </param>
            /// <param name="valuesOffset"> The offset where to start writing values. </param>
            /// <param name="iterations">   Controls how much data to decode. </param>
            void Decode(long[] blocks, int blocksOffset, int[] values, int valuesOffset, int iterations);

            /// <summary>
            /// Read <c>8 * iterations * BlockCount</c> blocks from <paramref name="blocks"/>,
            /// decode them and write <c>iterations * ValueCount</c> values into
            /// <paramref name="values"/>.
            /// </summary>
            /// <param name="blocks">       The long blocks that hold packed integer values. </param>
            /// <param name="blocksOffset"> The offset where to start reading blocks. </param>
            /// <param name="values">       The values buffer. </param>
            /// <param name="valuesOffset"> The offset where to start writing values. </param>
            /// <param name="iterations">   Controls how much data to decode. </param>
            void Decode(byte[] blocks, int blocksOffset, int[] values, int valuesOffset, int iterations);
        }

        /// <summary>
        /// An encoder for packed integers.
        /// </summary>
        public interface IEncoder
        {
            /// <summary>
            /// The minimum number of long blocks to encode in a single iteration, when
            /// using long encoding.
            /// <para/>
            /// NOTE: This was longBlockCount() in Lucene
            /// </summary>
            int Int64BlockCount { get; }

            /// <summary>
            /// The number of values that can be stored in <see cref="Int64BlockCount"/> long
            /// blocks.
            /// <para/>
            /// NOTE: This was longValueCount() in Lucene
            /// </summary>
            int Int64ValueCount { get; }

            /// <summary>
            /// The minimum number of byte blocks to encode in a single iteration, when
            /// using byte encoding.
            /// </summary>
            int ByteBlockCount { get; }

            /// <summary>
            /// The number of values that can be stored in <see cref="ByteBlockCount"/> byte
            /// blocks.
            /// </summary>
            int ByteValueCount { get; }

            /// <summary>
            /// Read <c>iterations * ValueCount</c> values from <paramref name="values"/>,
            /// encode them and write <c>iterations * BlockCount</c> blocks into
            /// <paramref name="blocks"/>.
            /// </summary>
            /// <param name="blocks">       The long blocks that hold packed integer values. </param>
            /// <param name="blocksOffset"> The offset where to start writing blocks. </param>
            /// <param name="values">       The values buffer. </param>
            /// <param name="valuesOffset"> The offset where to start reading values. </param>
            /// <param name="iterations">   Controls how much data to encode. </param>
            void Encode(long[] values, int valuesOffset, long[] blocks, int blocksOffset, int iterations);

            /// <summary>
            /// Read <c>iterations * ValueCount</c> values from <paramref name="values"/>,
            /// encode them and write <c>8 * iterations * BlockCount</c> blocks into
            /// <paramref name="blocks"/>.
            /// </summary>
            /// <param name="blocks">       The long blocks that hold packed integer values. </param>
            /// <param name="blocksOffset"> The offset where to start writing blocks. </param>
            /// <param name="values">       The values buffer. </param>
            /// <param name="valuesOffset"> The offset where to start reading values. </param>
            /// <param name="iterations">   Controls how much data to encode. </param>
            void Encode(long[] values, int valuesOffset, byte[] blocks, int blocksOffset, int iterations);

            /// <summary>
            /// Read <c>iterations * ValueCount</c> values from <paramref name="values"/>,
            /// encode them and write <c>iterations * BlockCount</c> blocks into
            /// <paramref name="blocks"/>.
            /// </summary>
            /// <param name="blocks">       The long blocks that hold packed integer values. </param>
            /// <param name="blocksOffset"> The offset where to start writing blocks. </param>
            /// <param name="values">       The values buffer. </param>
            /// <param name="valuesOffset"> The offset where to start reading values. </param>
            /// <param name="iterations">   Controls how much data to encode. </param>
            void Encode(int[] values, int valuesOffset, long[] blocks, int blocksOffset, int iterations);

            /// <summary>
            /// Read <c>iterations * ValueCount</c> values from <paramref name="values"/>,
            /// encode them and write <c>8 * iterations * BlockCount</c> blocks into
            /// <paramref name="blocks"/>.
            /// </summary>
            /// <param name="blocks">       The long blocks that hold packed integer values. </param>
            /// <param name="blocksOffset"> The offset where to start writing blocks. </param>
            /// <param name="values">       The values buffer. </param>
            /// <param name="valuesOffset"> The offset where to start reading values. </param>
            /// <param name="iterations">   Controls how much data to encode. </param>
            void Encode(int[] values, int valuesOffset, byte[] blocks, int blocksOffset, int iterations);
        }

        /// <summary>
        /// A read-only random access array of positive integers.
        /// <para/>
        /// @lucene.internal
        /// </summary>
        public abstract class Reader : NumericDocValues
        {
            /// <summary>
            /// Bulk get: read at least one and at most <paramref name="len"/> longs starting
            /// from <paramref name="index"/> into <c>arr[off:off+len]</c> and return
            /// the actual number of values that have been read.
            /// </summary>
            public virtual int Get(int index, long[] arr, int off, int len)
            {
                if (Debugging.AssertsEnabled)
                {
                    Debugging.Assert(len > 0, "len must be > 0 (got {0})", len);
                    Debugging.Assert(index >= 0 && index < Count);
                    Debugging.Assert(off + len <= arr.Length);
                }

                int gets = Math.Min(Count - index, len);
                for (int i = index, o = off, end = index + gets; i < end; ++i, ++o)
                {
                    arr[o] = Get(i);
                }
                return gets;
            }

            /// <returns> The number of bits used to store any given value.
            ///         Note: this does not imply that memory usage is
            ///         <c>bitsPerValue * #values</c> as implementations are free to
            ///         use non-space-optimal packing of bits. </returns>
            public abstract int BitsPerValue { get; }

            /// <summary>
            /// The number of values.
            /// <para/>
            /// NOTE: This was size() in Lucene.
            /// </summary>
            public abstract int Count { get; }

            /// <summary>
            /// Return the in-memory size in bytes.
            /// </summary>
            public abstract long RamBytesUsed();

            /// <summary>
            /// Expert: if the bit-width of this reader matches one of
            /// .NET's native types, returns the underlying array
            /// (ie, byte[], short[], int[], long[]); else, returns
            /// <c>null</c>.  Note that when accessing the array you must
            /// upgrade the type (bitwise AND with all ones), to
            /// interpret the full value as unsigned.  Ie,
            /// bytes[idx]&amp;0xFF, shorts[idx]&amp;0xFFFF, etc.
            /// </summary>
            public virtual object GetArray()
            {
                if (Debugging.AssertsEnabled) Debugging.Assert(!HasArray);
                return null;
            }

            /// <summary>
            /// Returns <c>true</c> if this implementation is backed by a
            /// native .NET array.
            /// </summary>
            /// <seealso cref="GetArray"/>
            public virtual bool HasArray => false;
        }

        /// <summary>
        /// Run-once iterator interface, to decode previously saved <see cref="PackedInt32s"/>.
        /// </summary>
        public interface IReaderIterator
        {
            /// <summary>
            /// Returns next value. </summary>
            long Next();

            /// <summary>
            /// Returns at least 1 and at most <paramref name="count"/> next values,
            /// the returned ref MUST NOT be modified.
            /// </summary>
            Int64sRef Next(int count);

            /// <summary>
            /// Returns number of bits per value. </summary>
            int BitsPerValue { get; }

            /// <summary>
            /// Returns number of values.
            /// <para/>
            /// NOTE: This was size() in Lucene.
            /// </summary>
            int Count { get; }

            /// <summary>
            /// Returns the current position. </summary>
            int Ord { get; }
        }

        // LUCENENET NOTE: Was ReaderIteratorImpl in Lucene
        internal abstract class ReaderIterator : IReaderIterator
        {
            protected readonly DataInput m_in;
            protected readonly int m_bitsPerValue;
            protected readonly int m_valueCount;

            protected ReaderIterator(int valueCount, int bitsPerValue, DataInput @in)
            {
                this.m_in = @in;
                this.m_bitsPerValue = bitsPerValue;
                this.m_valueCount = valueCount;
            }

            public virtual long Next()
            {
                Int64sRef nextValues = Next(1);
                if (Debugging.AssertsEnabled) Debugging.Assert(nextValues.Length > 0);
                long result = nextValues.Int64s[nextValues.Offset];
                ++nextValues.Offset;
                --nextValues.Length;
                return result;
            }

            public abstract Int64sRef Next(int count);

            public virtual int BitsPerValue => m_bitsPerValue;

            public virtual int Count => m_valueCount;

            public abstract int Ord { get; }
        }

        /// <summary>
        /// A packed integer array that can be modified.
        /// <para/>
        /// @lucene.internal
        /// </summary>
        public abstract class Mutable : Reader
        {
            /// <summary>
            /// Set the value at the given index in the array. </summary>
            /// <param name="index"> Where the value should be positioned. </param>
            /// <param name="value"> A value conforming to the constraints set by the array. </param>
            public abstract void Set(int index, long value);

            /// <summary>
            /// Bulk set: set at least one and at most <paramref name="len"/> longs starting
            /// at <paramref name="off"/> in <paramref name="arr"/> into this mutable, starting at
            /// <paramref name="index"/>. Returns the actual number of values that have been
            /// set.
            /// </summary>
            public virtual int Set(int index, long[] arr, int off, int len)
            {
                if (Debugging.AssertsEnabled)
                {
                    Debugging.Assert(len > 0, "len must be > 0 (got {0})", len);
                    Debugging.Assert(index >= 0 && index < Count);
                }
                len = Math.Min(len, Count - index);
                if (Debugging.AssertsEnabled) Debugging.Assert(off + len <= arr.Length);

                for (int i = index, o = off, end = index + len; i < end; ++i, ++o)
                {
                    Set(i, arr[o]);
                }
                return len;
            }

            /// <summary>
            /// Fill the mutable from <paramref name="fromIndex"/> (inclusive) to
            /// <paramref name="toIndex"/> (exclusive) with <paramref name="val"/>.
            /// </summary>
            public virtual void Fill(int fromIndex, int toIndex, long val)
            {
                if (Debugging.AssertsEnabled)
                {
                    Debugging.Assert(val <= MaxValue(BitsPerValue));
                    Debugging.Assert(fromIndex <= toIndex);
                }
                for (int i = fromIndex; i < toIndex; ++i)
                {
                    Set(i, val);
                }
            }

            /// <summary>
            /// Sets all values to 0.
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public virtual void Clear()
            {
                Fill(0, Count, 0);
            }

            /// <summary>
            /// Save this mutable into <paramref name="out"/>. Instantiating a reader from
            /// the generated data will return a reader with the same number of bits
            /// per value.
            /// </summary>
            public virtual void Save(DataOutput @out)
            {
                Writer writer = GetWriterNoHeader(@out, Format, Count, BitsPerValue, DEFAULT_BUFFER_SIZE);
                writer.WriteHeader();
                for (int i = 0; i < Count; ++i)
                {
                    writer.Add(Get(i));
                }
                writer.Finish();
            }

            /// <summary>
            /// The underlying format. </summary>
            internal virtual Format Format => Format.PACKED;
        }

        /// <summary>
        /// A simple base for <see cref="Reader"/>s that keeps track of valueCount and bitsPerValue.
        /// <para/>
        /// @lucene.internal
        /// </summary>
        internal abstract class ReaderImpl : Reader
        {
            protected readonly int m_bitsPerValue;
            protected readonly int m_valueCount;

            protected ReaderImpl(int valueCount, int bitsPerValue)
            {
                this.m_bitsPerValue = bitsPerValue;
                if (Debugging.AssertsEnabled) Debugging.Assert(bitsPerValue > 0 && bitsPerValue <= 64, "bitsPerValue={0}", bitsPerValue);
                this.m_valueCount = valueCount;
            }

            public override abstract long Get(int index);

            public override sealed int BitsPerValue => m_bitsPerValue;

            public override sealed int Count => m_valueCount;
        }

        public abstract class MutableImpl : Mutable
        {
            protected readonly int m_valueCount;
            protected readonly int m_bitsPerValue;

            protected MutableImpl(int valueCount, int bitsPerValue)
            {
                this.m_valueCount = valueCount;
                if (Debugging.AssertsEnabled) Debugging.Assert(bitsPerValue > 0 && bitsPerValue <= 64, "bitsPerValue={0}", bitsPerValue);
                this.m_bitsPerValue = bitsPerValue;
            }

            public override sealed int BitsPerValue => m_bitsPerValue;

            public override sealed int Count => m_valueCount;
        }

        /// <summary>
        /// A <see cref="Reader"/> which has all its values equal to 0 (bitsPerValue = 0). </summary>
        public sealed class NullReader : Reader
        {
            private readonly int valueCount;

            /// <summary>
            /// Sole constructor. </summary>
            public NullReader(int valueCount)
            {
                this.valueCount = valueCount;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public override long Get(int index)
            {
                return 0;
            }

            public override int Get(int index, long[] arr, int off, int len)
            {
                if (Debugging.AssertsEnabled)
                {
                    Debugging.Assert(len > 0, "len must be > 0 (got {0})", len);
                    Debugging.Assert(index >= 0 && index < valueCount);
                }
                len = Math.Min(len, valueCount - index);
                Arrays.Fill(arr, off, off + len, 0);
                return len;
            }

            public override int BitsPerValue => 0;

            public override int Count => valueCount;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public override long RamBytesUsed()
            {
                return RamUsageEstimator.AlignObjectSize(RamUsageEstimator.NUM_BYTES_OBJECT_HEADER + RamUsageEstimator.NUM_BYTES_INT32);
            }
        }

        /// <summary>
        /// A write-once Writer.
        /// <para/>
        /// @lucene.internal
        /// </summary>
        public abstract class Writer
        {
            protected readonly DataOutput m_out;
            protected readonly int m_valueCount;
            protected readonly int m_bitsPerValue;

            protected Writer(DataOutput @out, int valueCount, int bitsPerValue)
            {
                if (Debugging.AssertsEnabled)
                {
                    Debugging.Assert(bitsPerValue <= 64);
                    Debugging.Assert(valueCount >= 0 || valueCount == -1);
                }
                this.m_out = @out;
                this.m_valueCount = valueCount;
                this.m_bitsPerValue = bitsPerValue;
            }

            internal virtual void WriteHeader()
            {
                if (Debugging.AssertsEnabled) Debugging.Assert(m_valueCount != -1);
                CodecUtil.WriteHeader(m_out, CODEC_NAME, VERSION_CURRENT);
                m_out.WriteVInt32(m_bitsPerValue);
                m_out.WriteVInt32(m_valueCount);
                m_out.WriteVInt32(Format.Id);
            }

            /// <summary>
            /// The format used to serialize values. </summary>
            protected internal abstract PackedInt32s.Format Format { get; }

            /// <summary>
            /// Add a value to the stream. </summary>
            public abstract void Add(long v);

            /// <summary>
            /// The number of bits per value. </summary>
            public int BitsPerValue => m_bitsPerValue;

            /// <summary>
            /// Perform end-of-stream operations. </summary>
            public abstract void Finish();

            /// <summary>
            /// Returns the current ord in the stream (number of values that have been
            /// written so far minus one).
            /// </summary>
            public abstract int Ord { get; }
        }

        /// <summary>
        /// Get a <see cref="IDecoder"/>.
        /// </summary>
        /// <param name="format">         The format used to store packed <see cref="int"/>s. </param>
        /// <param name="version">        The compatibility version. </param>
        /// <param name="bitsPerValue">   The number of bits per value. </param>
        /// <returns> A decoder. </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IDecoder GetDecoder(Format format, int version, int bitsPerValue)
        {
            CheckVersion(version);
            return BulkOperation.Of(format, bitsPerValue);
        }

        /// <summary>
        /// Get an <see cref="IEncoder"/>.
        /// </summary>
        /// <param name="format">         The format used to store packed <see cref="int"/>s. </param>
        /// <param name="version">        The compatibility version. </param>
        /// <param name="bitsPerValue">   The number of bits per value. </param>
        /// <returns> An encoder. </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IEncoder GetEncoder(Format format, int version, int bitsPerValue)
        {
            CheckVersion(version);
            return BulkOperation.Of(format, bitsPerValue);
        }

        /// <summary>
        /// Expert: Restore a <see cref="Reader"/> from a stream without reading metadata at
        /// the beginning of the stream. This method is useful to restore data from
        /// streams which have been created using
        /// <see cref="PackedInt32s.GetWriterNoHeader(DataOutput, Format, int, int, int)"/>.
        /// <para/>
        /// @lucene.internal
        /// </summary>
        /// <param name="in">           The stream to read data from, positioned at the beginning of the packed values. </param>
        /// <param name="format">       The format used to serialize. </param>
        /// <param name="version">      The version used to serialize the data. </param>
        /// <param name="valueCount">   How many values the stream holds. </param>
        /// <param name="bitsPerValue"> The number of bits per value. </param>
        /// <returns>             A <see cref="Reader"/>. </returns>
        /// <exception cref="IOException"> If there is a low-level I/O error. </exception>
        /// <seealso cref="PackedInt32s.GetWriterNoHeader(DataOutput, Format, int, int, int)"/>
        public static Reader GetReaderNoHeader(DataInput @in, Format format, int version, int valueCount, int bitsPerValue)
        {
            CheckVersion(version);

            if (format == PackedInt32s.Format.PACKED_SINGLE_BLOCK)
            {
                return Packed64SingleBlock.Create(@in, valueCount, bitsPerValue);
            }
            else if (format == PackedInt32s.Format.PACKED)
            {
                switch (bitsPerValue)
                {
                    case 8:
                        return new Direct8(version, @in, valueCount);

                    case 16:
                        return new Direct16(version, @in, valueCount);

                    case 32:
                        return new Direct32(version, @in, valueCount);

                    case 64:
                        return new Direct64(/*version,*/ @in, valueCount); // LUCENENET specific - removed unused parameter

                    case 24:
                        if (valueCount <= Packed8ThreeBlocks.MAX_SIZE)
                        {
                            return new Packed8ThreeBlocks(version, @in, valueCount);
                        }
                        break;

                    case 48:
                        if (valueCount <= Packed16ThreeBlocks.MAX_SIZE)
                        {
                            return new Packed16ThreeBlocks(version, @in, valueCount);
                        }
                        break;
                }
                return new Packed64(version, @in, valueCount, bitsPerValue);
            }
            else
            {
                throw AssertionError.Create("Unknown Writer format: " + format);
            }
        }

        /// <summary>
        /// Expert: Restore a <see cref="Reader"/> from a stream without reading metadata at
        /// the beginning of the stream. this method is useful to restore data when
        /// metadata has been previously read using <see cref="ReadHeader(DataInput)"/>.
        /// <para/>
        /// @lucene.internal
        /// </summary>
        /// <param name="in">           The stream to read data from, positioned at the beginning of the packed values. </param>
        /// <param name="header">       Metadata result from <see cref="ReadHeader(DataInput)"/>. </param>
        /// <returns>             A <see cref="Reader"/>. </returns>
        /// <exception cref="IOException"> If there is a low-level I/O error. </exception>
        /// <seealso cref="ReadHeader(DataInput)"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Reader GetReaderNoHeader(DataInput @in, Header header)
        {
            return GetReaderNoHeader(@in, header.format, header.version, header.valueCount, header.bitsPerValue);
        }

        /// <summary>
        /// Restore a <see cref="Reader"/> from a stream.
        /// <para/>
        /// @lucene.internal
        /// </summary>
        /// <param name="in">           The stream to read data from. </param>
        /// <returns>             A <see cref="Reader"/>. </returns>
        /// <exception cref="IOException"> If there is a low-level I/O error </exception>
        public static Reader GetReader(DataInput @in)
        {
            int version = CodecUtil.CheckHeader(@in, CODEC_NAME, VERSION_START, VERSION_CURRENT);
            int bitsPerValue = @in.ReadVInt32();
            if (Debugging.AssertsEnabled) Debugging.Assert(bitsPerValue > 0 && bitsPerValue <= 64, "bitsPerValue={0}", bitsPerValue);
            int valueCount = @in.ReadVInt32();
            Format format = Format.ById(@in.ReadVInt32());

            return GetReaderNoHeader(@in, format, version, valueCount, bitsPerValue);
        }

        /// <summary>
        /// Expert: Restore a <see cref="IReaderIterator"/> from a stream without reading
        /// metadata at the beginning of the stream. This method is useful to restore
        /// data from streams which have been created using
        /// <see cref="PackedInt32s.GetWriterNoHeader(DataOutput, Format, int, int, int)"/>.
        /// <para/>
        /// @lucene.internal
        /// </summary>
        /// <param name="in">           The stream to read data from, positioned at the beginning of the packed values. </param>
        /// <param name="format">       The format used to serialize. </param>
        /// <param name="version">      The version used to serialize the data. </param>
        /// <param name="valueCount">   How many values the stream holds. </param>
        /// <param name="bitsPerValue"> the number of bits per value. </param>
        /// <param name="mem">          How much memory the iterator is allowed to use to read-ahead (likely to speed up iteration). </param>
        /// <returns>             A <see cref="IReaderIterator"/>. </returns>
        /// <seealso cref="PackedInt32s.GetWriterNoHeader(DataOutput, Format, int, int, int)"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IReaderIterator GetReaderIteratorNoHeader(DataInput @in, Format format, int version, int valueCount, int bitsPerValue, int mem)
        {
            CheckVersion(version);
            return new PackedReaderIterator(format, version, valueCount, bitsPerValue, @in, mem);
        }

        /// <summary>
        /// Retrieve <see cref="PackedInt32s"/> as a <see cref="IReaderIterator"/>. 
        /// <para/>
        /// @lucene.internal
        /// </summary>
        /// <param name="in"> Positioned at the beginning of a stored packed int structure. </param>
        /// <param name="mem"> How much memory the iterator is allowed to use to read-ahead (likely to speed up iteration). </param>
        /// <returns> An iterator to access the values. </returns>
        /// <exception cref="IOException"> If the structure could not be retrieved. </exception>
        public static IReaderIterator GetReaderIterator(DataInput @in, int mem)
        {
            int version = CodecUtil.CheckHeader(@in, CODEC_NAME, VERSION_START, VERSION_CURRENT);
            int bitsPerValue = @in.ReadVInt32();
            if (Debugging.AssertsEnabled) Debugging.Assert(bitsPerValue > 0 && bitsPerValue <= 64, "bitsPerValue={0}", bitsPerValue);
            int valueCount = @in.ReadVInt32();
            Format format = Format.ById(@in.ReadVInt32());
            return GetReaderIteratorNoHeader(@in, format, version, valueCount, bitsPerValue, mem);
        }

        /// <summary>
        /// Expert: Construct a direct <see cref="Reader"/> from a stream without reading
        /// metadata at the beginning of the stream. This method is useful to restore
        /// data from streams which have been created using
        /// <see cref="PackedInt32s.GetWriterNoHeader(DataOutput, Format, int, int, int)"/>.
        /// <para/>
        /// The returned reader will have very little memory overhead, but every call
        /// to <see cref="NumericDocValues.Get(int)"/> is likely to perform a disk seek.
        /// <para/>
        /// @lucene.internal
        /// </summary>
        /// <param name="in">           The stream to read data from. </param>
        /// <param name="format">       The format used to serialize. </param>
        /// <param name="version">      The version used to serialize the data. </param>
        /// <param name="valueCount">   How many values the stream holds. </param>
        /// <param name="bitsPerValue"> The number of bits per value. </param>
        /// <returns> A direct <see cref="Reader"/>. </returns>
        public static Reader GetDirectReaderNoHeader(IndexInput @in, Format format, int version, int valueCount, int bitsPerValue)
        {
            CheckVersion(version);

            if (format == PackedInt32s.Format.PACKED_SINGLE_BLOCK)
            {
                return new DirectPacked64SingleBlockReader(bitsPerValue, valueCount, @in);
            }
            else if (format == PackedInt32s.Format.PACKED)
            {
                long byteCount = format.ByteCount(version, valueCount, bitsPerValue);
                if (byteCount != format.ByteCount(VERSION_CURRENT, valueCount, bitsPerValue))
                {
                    if (Debugging.AssertsEnabled) Debugging.Assert(version == VERSION_START);
                    long endPointer = @in.Position + byteCount; // LUCENENET specific: Renamed from getFilePointer() to match FileStream
                    // Some consumers of direct readers assume that reading the last value
                    // will make the underlying IndexInput go to the end of the packed
                    // stream, but this is not true because packed ints storage used to be
                    // long-aligned and is now byte-aligned, hence this additional
                    // condition when reading the last value
                    return new DirectPackedReaderAnonymousClass(bitsPerValue, valueCount, @in, endPointer);
                }
                else
                {
                    return new DirectPackedReader(bitsPerValue, valueCount, @in);
                }
            }
            else
            {
                throw AssertionError.Create("Unknwown format: " + format);
            }
        }

        private sealed class DirectPackedReaderAnonymousClass : DirectPackedReader
        {
            private readonly IndexInput @in;
            private readonly int valueCount;
            private readonly long endPointer;

            public DirectPackedReaderAnonymousClass(int bitsPerValue, int valueCount, IndexInput @in, long endPointer)
                : base(bitsPerValue, valueCount, @in)
            {
                this.@in = @in;
                this.valueCount = valueCount;
                this.endPointer = endPointer;
            }

            public override long Get(int index)
            {
                long result = base.Get(index);
                if (index == valueCount - 1)
                {
                    try
                    {
                        @in.Seek(endPointer);
                    }
                    catch (Exception e) when (e.IsIOException())
                    {
                        throw IllegalStateException.Create("failed", e);
                    }
                }
                return result;
            }
        }

        /// <summary>
        /// Expert: Construct a direct <see cref="Reader"/> from an <see cref="IndexInput"/>
        /// without reading metadata at the beginning of the stream. this method is
        /// useful to restore data when metadata has been previously read using
        /// <see cref="ReadHeader(DataInput)"/>.
        /// <para/>
        /// @lucene.internal
        /// </summary>
        /// <param name="in">           The stream to read data from, positioned at the beginning of the packed values. </param>
        /// <param name="header">       Metadata result from <see cref="ReadHeader(DataInput)"/>. </param>
        /// <returns>             A <see cref="Reader"/>. </returns>
        /// <exception cref="IOException"> If there is a low-level I/O error. </exception>
        /// <seealso cref="ReadHeader(DataInput)"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Reader GetDirectReaderNoHeader(IndexInput @in, Header header)
        {
            return GetDirectReaderNoHeader(@in, header.format, header.version, header.valueCount, header.bitsPerValue);
        }

        /// <summary>
        /// Construct a direct <see cref="Reader"/> from an <see cref="IndexInput"/>. this method
        /// is useful to restore data from streams which have been created using
        /// <see cref="PackedInt32s.GetWriter(DataOutput, int, int, float)"/>.
        /// <para/>
        /// The returned reader will have very little memory overhead, but every call
        /// to <see cref="NumericDocValues.Get(int)"/> is likely to perform a disk seek.
        /// <para/>
        /// @lucene.internal
        /// </summary>
        /// <param name="in">           The stream to read data from. </param>
        /// <returns> A direct <see cref="Reader"/>. </returns>
        /// <exception cref="IOException"> If there is a low-level I/O error. </exception>
        public static Reader GetDirectReader(IndexInput @in)
        {
            int version = CodecUtil.CheckHeader(@in, CODEC_NAME, VERSION_START, VERSION_CURRENT);
            int bitsPerValue = @in.ReadVInt32();
            if (Debugging.AssertsEnabled) Debugging.Assert(bitsPerValue > 0 && bitsPerValue <= 64, "bitsPerValue={0}", bitsPerValue);
            int valueCount = @in.ReadVInt32();
            Format format = Format.ById(@in.ReadVInt32());
            return GetDirectReaderNoHeader(@in, format, version, valueCount, bitsPerValue);
        }

        /// <summary>
        /// Create a packed integer array with the given amount of values initialized
        /// to 0. The <paramref name="valueCount"/> and the <paramref name="bitsPerValue"/> cannot be changed after creation.
        /// All Mutables known by this factory are kept fully in RAM.
        /// <para/>
        /// Positive values of <paramref name="acceptableOverheadRatio"/> will trade space
        /// for speed by selecting a faster but potentially less memory-efficient
        /// implementation. An <paramref name="acceptableOverheadRatio"/> of
        /// <see cref="PackedInt32s.COMPACT"/> will make sure that the most memory-efficient
        /// implementation is selected whereas <see cref="PackedInt32s.FASTEST"/> will make sure
        /// that the fastest implementation is selected.
        /// <para/>
        /// @lucene.internal
        /// </summary>
        /// <param name="valueCount">   The number of elements. </param>
        /// <param name="bitsPerValue"> The number of bits available for any given value. </param>
        /// <param name="acceptableOverheadRatio"> An acceptable overhead
        ///        ratio per value. </param>
        /// <returns> A mutable packed integer array. </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Mutable GetMutable(int valueCount, int bitsPerValue, float acceptableOverheadRatio)
        {
            FormatAndBits formatAndBits = FastestFormatAndBits(valueCount, bitsPerValue, acceptableOverheadRatio);
            return GetMutable(valueCount, formatAndBits.BitsPerValue, formatAndBits.Format);
        }

        /// <summary>
        /// Same as <see cref="GetMutable(int, int, float)"/> with a pre-computed number
        /// of bits per value and format.
        /// <para/>
        /// @lucene.internal
        /// </summary>
        public static Mutable GetMutable(int valueCount, int bitsPerValue, PackedInt32s.Format format)
        {
            if (Debugging.AssertsEnabled) Debugging.Assert(valueCount >= 0);

            if (format == PackedInt32s.Format.PACKED_SINGLE_BLOCK)
            {
                return Packed64SingleBlock.Create(valueCount, bitsPerValue);
            }
            else if (format == PackedInt32s.Format.PACKED)
            {
                switch (bitsPerValue)
                {
                    case 8:
                        return new Direct8(valueCount);

                    case 16:
                        return new Direct16(valueCount);

                    case 32:
                        return new Direct32(valueCount);

                    case 64:
                        return new Direct64(valueCount);

                    case 24:
                        if (valueCount <= Packed8ThreeBlocks.MAX_SIZE)
                        {
                            return new Packed8ThreeBlocks(valueCount);
                        }
                        break;

                    case 48:
                        if (valueCount <= Packed16ThreeBlocks.MAX_SIZE)
                        {
                            return new Packed16ThreeBlocks(valueCount);
                        }
                        break;
                }
                return new Packed64(valueCount, bitsPerValue);
            }
            else
            {
                throw AssertionError.Create();
            }
        }

        /// <summary>
        /// Expert: Create a packed integer array writer for the given output, format,
        /// value count, and number of bits per value.
        /// <para/>
        /// The resulting stream will be long-aligned. this means that depending on
        /// the format which is used, up to 63 bits will be wasted. An easy way to
        /// make sure that no space is lost is to always use a <paramref name="valueCount"/>
        /// that is a multiple of 64.
        /// <para/>
        /// This method does not write any metadata to the stream, meaning that it is
        /// your responsibility to store it somewhere else in order to be able to
        /// recover data from the stream later on:
        /// <list type="bullet">
        ///   <item><description><paramref name="format"/> (using <see cref="Format.Id"/>),</description></item>
        ///   <item><description><paramref name="valueCount"/>,</description></item>
        ///   <item><description><paramref name="bitsPerValue"/>,</description></item>
        ///   <item><description><see cref="VERSION_CURRENT"/>.</description></item>
        /// </list>
        /// <para/>
        /// It is possible to start writing values without knowing how many of them you
        /// are actually going to write. To do this, just pass <c>-1</c> as
        /// <paramref name="valueCount"/>. On the other hand, for any positive value of
        /// <paramref name="valueCount"/>, the returned writer will make sure that you don't
        /// write more values than expected and pad the end of stream with zeros in
        /// case you have written less than <paramref name="valueCount"/> when calling
        /// <see cref="Writer.Finish()"/>.
        /// <para/>
        /// The <paramref name="mem"/> parameter lets you control how much memory can be used
        /// to buffer changes in memory before flushing to disk. High values of
        /// <paramref name="mem"/> are likely to improve throughput. On the other hand, if
        /// speed is not that important to you, a value of <c>0</c> will use as
        /// little memory as possible and should already offer reasonable throughput.
        /// <para/>
        /// @lucene.internal
        /// </summary>
        /// <param name="out">          The data output. </param>
        /// <param name="format">       The format to use to serialize the values. </param>
        /// <param name="valueCount">   The number of values. </param>
        /// <param name="bitsPerValue"> The number of bits per value. </param>
        /// <param name="mem">          How much memory (in bytes) can be used to speed up serialization. </param>
        /// <returns>             A <see cref="Writer"/>. </returns>
        /// <seealso cref="PackedInt32s.GetReaderIteratorNoHeader(DataInput, Format, int, int, int, int)"/>
        /// <seealso cref="PackedInt32s.GetReaderNoHeader(DataInput, Format, int, int, int)"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Writer GetWriterNoHeader(DataOutput @out, Format format, int valueCount, int bitsPerValue, int mem)
        {
            return new PackedWriter(format, @out, valueCount, bitsPerValue, mem);
        }

        /// <summary>
        /// Create a packed integer array writer for the given output, format, value
        /// count, and number of bits per value.
        /// <para/>
        /// The resulting stream will be long-aligned. this means that depending on
        /// the format which is used under the hoods, up to 63 bits will be wasted.
        /// An easy way to make sure that no space is lost is to always use a
        /// <paramref name="valueCount"/> that is a multiple of 64.
        /// <para/>
        /// This method writes metadata to the stream, so that the resulting stream is
        /// sufficient to restore a <see cref="Reader"/> from it. You don't need to track
        /// <paramref name="valueCount"/> or <paramref name="bitsPerValue"/> by yourself. In case
        /// this is a problem, you should probably look at
        /// <see cref="GetWriterNoHeader(DataOutput, Format, int, int, int)"/>.
        /// <para/>
        /// The <paramref name="acceptableOverheadRatio"/> parameter controls how
        /// readers that will be restored from this stream trade space
        /// for speed by selecting a faster but potentially less memory-efficient
        /// implementation. An <paramref name="acceptableOverheadRatio"/> of
        /// <see cref="PackedInt32s.COMPACT"/> will make sure that the most memory-efficient
        /// implementation is selected whereas <see cref="PackedInt32s.FASTEST"/> will make sure
        /// that the fastest implementation is selected. In case you are only interested
        /// in reading this stream sequentially later on, you should probably use
        /// <see cref="PackedInt32s.COMPACT"/>.
        /// <para/>
        /// @lucene.internal
        /// </summary>
        /// <param name="out">          The data output. </param>
        /// <param name="valueCount">   The number of values. </param>
        /// <param name="bitsPerValue"> The number of bits per value. </param>
        /// <param name="acceptableOverheadRatio"> An acceptable overhead ratio per value. </param>
        /// <returns>             A <see cref="Writer"/>. </returns>
        /// <exception cref="IOException"> If there is a low-level I/O error. </exception>
        public static Writer GetWriter(DataOutput @out, int valueCount, int bitsPerValue, float acceptableOverheadRatio)
        {
            if (Debugging.AssertsEnabled) Debugging.Assert(valueCount >= 0);

            FormatAndBits formatAndBits = FastestFormatAndBits(valueCount, bitsPerValue, acceptableOverheadRatio);
            Writer writer = GetWriterNoHeader(@out, formatAndBits.Format, valueCount, formatAndBits.BitsPerValue, DEFAULT_BUFFER_SIZE);
            writer.WriteHeader();
            return writer;
        }

        /// <summary>
        /// Returns how many bits are required to hold values up
        /// to and including <paramref name="maxValue"/>. 
        /// <para/>
        /// @lucene.internal
        /// </summary>
        /// <param name="maxValue"> The maximum value that should be representable. </param>
        /// <returns> The amount of bits needed to represent values from 0 to <paramref name="maxValue"/>. </returns>
        public static int BitsRequired(long maxValue)
        {
            if (maxValue < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxValue), "maxValue must be non-negative (got: " + maxValue + ")"); // LUCENENET specific - changed from IllegalArgumentException to ArgumentOutOfRangeException (.NET convention)
            }
            return Math.Max(1, 64 - maxValue.LeadingZeroCount());
        }

        /// <summary>
        /// Calculates the maximum unsigned long that can be expressed with the given
        /// number of bits. 
        /// <para/>
        /// @lucene.internal
        /// </summary>
        /// <param name="bitsPerValue"> The number of bits available for any given value. </param>
        /// <returns> The maximum value for the given bits. </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long MaxValue(int bitsPerValue)
        {
            return bitsPerValue == 64 ? long.MaxValue : ~(~0L << bitsPerValue);
        }

        /// <summary>
        /// Copy <c>src[srcPos:srcPos+len]</c> into
        /// <c>dest[destPos:destPos+len]</c> using at most <paramref name="mem"/>
        /// bytes.
        /// </summary>
        public static void Copy(Reader src, int srcPos, Mutable dest, int destPos, int len, int mem)
        {
            if (Debugging.AssertsEnabled)
            {
                Debugging.Assert(srcPos + len <= src.Count);
                Debugging.Assert(destPos + len <= dest.Count);
            }
            int capacity = mem.TripleShift(3);
            if (capacity == 0)
            {
                for (int i = 0; i < len; ++i)
                {
                    dest.Set(destPos++, src.Get(srcPos++));
                }
            }
            else if (len > 0)
            {
                // use bulk operations
                long[] buf = new long[Math.Min(capacity, len)];
                Copy(src, srcPos, dest, destPos, len, buf);
            }
        }

        /// <summary>
        /// Same as <see cref="Copy(Reader, int, Mutable, int, int, int)"/> but using a pre-allocated buffer. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void Copy(Reader src, int srcPos, Mutable dest, int destPos, int len, long[] buf)
        {
            if (Debugging.AssertsEnabled) Debugging.Assert(buf.Length > 0);
            int remaining = 0;
            while (len > 0)
            {
                int read = src.Get(srcPos, buf, remaining, Math.Min(len, buf.Length - remaining));
                if (Debugging.AssertsEnabled) Debugging.Assert(read > 0);
                srcPos += read;
                len -= read;
                remaining += read;
                int written = dest.Set(destPos, buf, 0, remaining);
                if (Debugging.AssertsEnabled) Debugging.Assert(written > 0);
                destPos += written;
                if (written < remaining)
                {
                    Arrays.Copy(buf, written, buf, 0, remaining - written);
                }
                remaining -= written;
            }
            while (remaining > 0)
            {
                int written = dest.Set(destPos, buf, 0, remaining);
                destPos += written;
                remaining -= written;
                Arrays.Copy(buf, written, buf, 0, remaining);
            }
        }

        /// <summary>
        /// Expert: reads only the metadata from a stream. This is useful to later
        /// restore a stream or open a direct reader via
        /// <see cref="GetReaderNoHeader(DataInput, Header)"/>
        /// or <see cref="GetDirectReaderNoHeader(IndexInput, Header)"/>. </summary>
        /// <param name="in"> The stream to read data. </param>
        /// <returns>   Packed integer metadata. </returns>
        /// <exception cref="IOException"> If there is a low-level I/O error. </exception>
        /// <seealso cref="GetReaderNoHeader(DataInput, Header)"/>
        /// <seealso cref="GetDirectReaderNoHeader(IndexInput, Header)"/>
        public static Header ReadHeader(DataInput @in)
        {
            int version = CodecUtil.CheckHeader(@in, CODEC_NAME, VERSION_START, VERSION_CURRENT);
            int bitsPerValue = @in.ReadVInt32();
            if (Debugging.AssertsEnabled) Debugging.Assert(bitsPerValue > 0 && bitsPerValue <= 64, "bitsPerValue={0}", bitsPerValue);
            int valueCount = @in.ReadVInt32();
            Format format = Format.ById(@in.ReadVInt32());
            return new Header(format, valueCount, bitsPerValue, version);
        }

        /// <summary>
        /// Header identifying the structure of a packed integer array. </summary>
        public class Header
        {
            internal readonly Format format;
            internal readonly int valueCount;
            internal readonly int bitsPerValue;
            internal readonly int version;

            public Header(Format format, int valueCount, int bitsPerValue, int version)
            {
                this.format = format;
                this.valueCount = valueCount;
                this.bitsPerValue = bitsPerValue;
                this.version = version;
            }
        }

        /// <summary>
        /// Check that the block size is a power of 2, in the right bounds, and return
        /// its log in base 2.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int CheckBlockSize(int blockSize, int minBlockSize, int maxBlockSize)
        {
            if (blockSize < minBlockSize || blockSize > maxBlockSize)
            {
                throw new ArgumentException("blockSize must be >= " + minBlockSize + " and <= " + maxBlockSize + ", got " + blockSize);
            }
            if ((blockSize & (blockSize - 1)) != 0)
            {
                throw new ArgumentException("blockSize must be a power of two, got " + blockSize);
            }
            return blockSize.TrailingZeroCount();
        }

        /// <summary>
        /// Return the number of blocks required to store <paramref name="size"/> values on
        /// <paramref name="blockSize"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int NumBlocks(long size, int blockSize)
        {
            int numBlocks = (int)(size / blockSize) + (size % blockSize == 0 ? 0 : 1);
            if ((long)numBlocks * blockSize < size)
            {
                throw new ArgumentException("size is too large for this block size");
            }
            return numBlocks;
        }
    }
}