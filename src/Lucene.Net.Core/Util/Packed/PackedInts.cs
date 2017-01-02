using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using System.Diagnostics;

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
    ///
    /// @lucene.internal
    /// </summary>
    public class PackedInts
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
                throw new System.ArgumentException("Version is too old, should be at least " + VERSION_START + " (got " + version + ")");
            }
            else if (version > VERSION_CURRENT)
            {
                throw new System.ArgumentException("Version is too new, should be at most " + VERSION_CURRENT + " (got " + version + ")");
            }
        }

        /// <summary>
        /// A format to write packed ints.
        ///
        /// @lucene.internal
        /// </summary>
        //public enum Format
        //{
        /// <summary>
        /// Compact format, all bits are written contiguously.
        /// </summary>

        /// <summary>
        /// A format that may insert padding bits to improve encoding and decoding
        /// speed. Since this format doesn't support all possible bits per value, you
        /// should never use it directly, but rather use
        /// <seealso cref="PackedInts#fastestFormatAndBits(int, int, float)"/> to find the
        /// format that best suits your needs.
        /// </summary>

        /// <summary>
        /// Get a format according to its ID.
        /// </summary>
        //	{
        //	  for (Format format : Format.values())
        //	  {
        //		if (format.getId() == id)
        //		{
        //		  return format;
        //		}
        //	  }
        //	  throw new IllegalArgumentException("Unknown format id: " + id);
        //	}

        //	{
        //	  this.id = id;
        //	}

        /// <summary>
        /// Returns the ID of the format.
        /// </summary>

        /// <summary>
        /// Computes how many byte blocks are needed to store <code>values</code>
        /// values of size <code>bitsPerValue</code>.
        /// </summary>

        /// <summary>
        /// Computes how many long blocks are needed to store <code>values</code>
        /// values of size <code>bitsPerValue</code>.
        /// </summary>

        /// <summary>
        /// Tests whether the provided number of bits per value is supported by the
        /// format.
        /// </summary>

        /// <summary>
        /// Returns the overhead per value, in bits.
        /// </summary>

        /// <summary>
        /// Returns the overhead ratio (<code>overhead per value / bits per value</code>).
        /// </summary>
        //}
        /*	public static partial class EnumExtensionMethods
            {
                internal PACKED(this Format instance, 0)
                {
                  public long outerInstance.ByteCount(int packedIntsVersion, int valueCount, int bitsPerValue)
                  {
                    if (packedIntsVersion < VERSION_BYTE_ALIGNED)
                    {
                      return 8L * (long) Math.Ceiling((double) valueCount * bitsPerValue / 64);
                    }
                    else
                    {
                      return (long) Math.Ceiling((double) valueCount * bitsPerValue / 8);
                    }
                  }
                },
                outerInstance.PACKED_SINGLE_BLOCK(this Format instance, 1)
                {
                  public int outerInstance.LongCount(int packedIntsVersion, int valueCount, int bitsPerValue)
                  {
                    int valuesPerBlock = 64 / bitsPerValue;
                    return (int) Math.Ceiling((double) valueCount / valuesPerBlock);
                  }

                  public bool outerInstance.IsSupported(int bitsPerValue)
                  {
                    return Packed64SingleBlock.IsSupported(bitsPerValue);
                  }

                  public float outerInstance.OverheadPerValue(int bitsPerValue)
                  {
                    Debug.Assert(outerInstance.IsSupported(bitsPerValue));
                    int valuesPerBlock = 64 / bitsPerValue;
                    int overhead = 64 % bitsPerValue;
                    return (float) overhead / valuesPerBlock;
                  }
                }
                public static int outerInstance.Id //Tangible note: extension parameterthis Format instance
                {
                  return outerInstance.Id_Renamed;
                }
                public static long outerInstance.ByteCount(this Format instance, int packedIntsVersion, int valueCount, int bitsPerValue)
                {
                  Debug.Assert(bitsPerValue >= 0 && bitsPerValue <= 64, bitsPerValue);
                  // assume long-aligned
                  return 8L * outerInstance.LongCount(packedIntsVersion, valueCount, bitsPerValue);
                }
                public static int outerInstance.LongCount(this Format instance, int packedIntsVersion, int valueCount, int bitsPerValue)
                {
                  Debug.Assert(bitsPerValue >= 0 && bitsPerValue <= 64, bitsPerValue);
                  long byteCount = outerInstance.ByteCount(packedIntsVersion, valueCount, bitsPerValue);
                  Debug.Assert(byteCount < 8L * int.MaxValue);
                  if ((byteCount % 8) == 0)
                  {
                    return (int)(byteCount / 8);
                  }
                  else
                  {
                    return (int)(byteCount / 8 + 1);
                  }
                }
                public static bool outerInstance.IsSupported(this Format instance, int bitsPerValue)
                {
                  return bitsPerValue >= 1 && bitsPerValue <= 64;
                }
                public static float outerInstance.OverheadPerValue(this Format instance, int bitsPerValue)
                {
                  Debug.Assert(outerInstance.IsSupported(bitsPerValue));
                  return 0f;
                }
                public static final float outerInstance.OverheadRatio(this Format instance, int bitsPerValue)
                {
                  Debug.Assert(outerInstance.IsSupported(bitsPerValue));
                  return outerInstance.OverheadPerValue(bitsPerValue) / bitsPerValue;
                }
            }*/

        private sealed class PackedFormat : Format
        {
            public PackedFormat()
                : base(0)
            {
            }

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

            public override int LongCount(int packedIntsVersion, int valueCount, int bitsPerValue)
            {
                int valuesPerBlock = 64 / bitsPerValue;
                return (int)Math.Ceiling((double)valueCount / valuesPerBlock);
            }

            public override bool IsSupported(int bitsPerValue)
            {
                return Packed64SingleBlock.IsSupported(bitsPerValue);
            }

            public override float OverheadPerValue(int bitsPerValue)
            {
                int valuesPerBlock = 64 / bitsPerValue;

                int overhead = 64 % bitsPerValue;
                return (float)overhead / valuesPerBlock;
            }
        }

        public class Format
        {
            public static readonly Format PACKED = new PackedFormat();

            public static readonly Format PACKED_SINGLE_BLOCK = new PackedSingleBlockFormat();

            private static readonly Format[] values = new Format[] { PACKED, PACKED_SINGLE_BLOCK };

            public static IEnumerable<Format> Values()
            {
                return values;
            }

            public static Format ById(int id)
            {
                foreach (Format format in Values())
                {
                    if (format.GetId() == id)
                    {
                        return format;
                    }
                }
                throw new ArgumentException("Unknown format id: " + id);
            }

            internal Format(int id)
            {
                this.id = id;
            }

            public int id;

            public int GetId()
            {
                return id;
            }

            public virtual long ByteCount(int packedIntsVersion, int valueCount, int bitsPerValue)
            {
                // assume long-aligned
                return 8L * LongCount(packedIntsVersion, valueCount, bitsPerValue);
            }

            public virtual int LongCount(int packedIntsVersion, int valueCount, int bitsPerValue) // LUCENENET TODO: Rename Int64Count ?
            {
                long byteCount = ByteCount(packedIntsVersion, valueCount, bitsPerValue);

                if ((byteCount % 8) == 0)
                {
                    return (int)(byteCount / 8);
                }
                else
                {
                    return (int)(byteCount / 8 + 1);
                }
            }

            public virtual bool IsSupported(int bitsPerValue)
            {
                return bitsPerValue >= 1 && bitsPerValue <= 64;
            }

            public virtual float OverheadPerValue(int bitsPerValue) // LUCENENET TODO: Debug.Assert missing
            {
                return 0f;
            }

            public virtual float OverheadRatio(int bitsPerValue) // LUCENENET TODO: Debug.Assert missing
            {
                return OverheadPerValue(bitsPerValue) / bitsPerValue;
            }
        }

        /// <summary>
        /// Simple class that holds a format and a number of bits per value.
        /// </summary>
        public class FormatAndBits
        {
            public readonly Format format;
            public readonly int bitsPerValue;

            public FormatAndBits(Format format, int bitsPerValue)
            {
                this.format = format;
                this.bitsPerValue = bitsPerValue;
            }

            public override string ToString()
            {
                return "FormatAndBits(format=" + format + " bitsPerValue=" + bitsPerValue + ")";
            }
        }

        /// <summary>
        /// Try to find the <seealso cref="Format"/> and number of bits per value that would
        /// restore from disk the fastest reader whose overhead is less than
        /// <code>acceptableOverheadRatio</code>.
        /// </p><p>
        /// The <code>acceptableOverheadRatio</code> parameter makes sense for
        /// random-access <seealso cref="Reader"/>s. In case you only plan to perform
        /// sequential access on this stream later on, you should probably use
        /// <seealso cref="PackedInts#COMPACT"/>.
        /// </p><p>
        /// If you don't know how many values you are going to write, use
        /// <code>valueCount = -1</code>.
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
                        if (overhead <= acceptableOverhead)
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
            /// The minimum number of long blocks to encode in a single iteration, when
            /// using long encoding.
            /// </summary>
            int LongBlockCount { get; } // LUCENENET TODO: Rename Int64BlockCount ?

            /// <summary>
            /// The number of values that can be stored in <seealso cref="#longBlockCount()"/> long
            /// blocks.
            /// </summary>
            int LongValueCount { get; } // LUCENENET TODO: Rename Int64ValueCount ?

            /// <summary>
            /// The minimum number of byte blocks to encode in a single iteration, when
            /// using byte encoding.
            /// </summary>
            int ByteBlockCount { get; }

            /// <summary>
            /// The number of values that can be stored in <seealso cref="#byteBlockCount()"/> byte
            /// blocks.
            /// </summary>
            int ByteValueCount { get; }

            /// <summary>
            /// Read <code>iterations * blockCount()</code> blocks from <code>blocks</code>,
            /// decode them and write <code>iterations * valueCount()</code> values into
            /// <code>values</code>.
            /// </summary>
            /// <param name="blocks">       the long blocks that hold packed integer values </param>
            /// <param name="blocksOffset"> the offset where to start reading blocks </param>
            /// <param name="values">       the values buffer </param>
            /// <param name="valuesOffset"> the offset where to start writing values </param>
            /// <param name="iterations">   controls how much data to decode </param>
            void Decode(long[] blocks, int blocksOffset, long[] values, int valuesOffset, int iterations);

            /// <summary>
            /// Read <code>8 * iterations * blockCount()</code> blocks from <code>blocks</code>,
            /// decode them and write <code>iterations * valueCount()</code> values into
            /// <code>values</code>.
            /// </summary>
            /// <param name="blocks">       the long blocks that hold packed integer values </param>
            /// <param name="blocksOffset"> the offset where to start reading blocks </param>
            /// <param name="values">       the values buffer </param>
            /// <param name="valuesOffset"> the offset where to start writing values </param>
            /// <param name="iterations">   controls how much data to decode </param>
            void Decode(byte[] blocks, int blocksOffset, long[] values, int valuesOffset, int iterations);

            /// <summary>
            /// Read <code>iterations * blockCount()</code> blocks from <code>blocks</code>,
            /// decode them and write <code>iterations * valueCount()</code> values into
            /// <code>values</code>.
            /// </summary>
            /// <param name="blocks">       the long blocks that hold packed integer values </param>
            /// <param name="blocksOffset"> the offset where to start reading blocks </param>
            /// <param name="values">       the values buffer </param>
            /// <param name="valuesOffset"> the offset where to start writing values </param>
            /// <param name="iterations">   controls how much data to decode </param>
            void Decode(long[] blocks, int blocksOffset, int[] values, int valuesOffset, int iterations);

            /// <summary>
            /// Read <code>8 * iterations * blockCount()</code> blocks from <code>blocks</code>,
            /// decode them and write <code>iterations * valueCount()</code> values into
            /// <code>values</code>.
            /// </summary>
            /// <param name="blocks">       the long blocks that hold packed integer values </param>
            /// <param name="blocksOffset"> the offset where to start reading blocks </param>
            /// <param name="values">       the values buffer </param>
            /// <param name="valuesOffset"> the offset where to start writing values </param>
            /// <param name="iterations">   controls how much data to decode </param>
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
            /// </summary>
            int LongBlockCount { get; } // LUCENENET TODO: Rename Int64BlockCount ?

            /// <summary>
            /// The number of values that can be stored in <seealso cref="#longBlockCount()"/> long
            /// blocks.
            /// </summary>
            int LongValueCount { get; } // LUCENENET TODO:  Rename Int64ValueCount ?

            /// <summary>
            /// The minimum number of byte blocks to encode in a single iteration, when
            /// using byte encoding.
            /// </summary>
            int ByteBlockCount { get; }

            /// <summary>
            /// The number of values that can be stored in <seealso cref="#byteBlockCount()"/> byte
            /// blocks.
            /// </summary>
            int ByteValueCount { get; }

            /// <summary>
            /// Read <code>iterations * valueCount()</code> values from <code>values</code>,
            /// encode them and write <code>iterations * blockCount()</code> blocks into
            /// <code>blocks</code>.
            /// </summary>
            /// <param name="blocks">       the long blocks that hold packed integer values </param>
            /// <param name="blocksOffset"> the offset where to start writing blocks </param>
            /// <param name="values">       the values buffer </param>
            /// <param name="valuesOffset"> the offset where to start reading values </param>
            /// <param name="iterations">   controls how much data to encode </param>
            void Encode(long[] values, int valuesOffset, long[] blocks, int blocksOffset, int iterations);

            /// <summary>
            /// Read <code>iterations * valueCount()</code> values from <code>values</code>,
            /// encode them and write <code>8 * iterations * blockCount()</code> blocks into
            /// <code>blocks</code>.
            /// </summary>
            /// <param name="blocks">       the long blocks that hold packed integer values </param>
            /// <param name="blocksOffset"> the offset where to start writing blocks </param>
            /// <param name="values">       the values buffer </param>
            /// <param name="valuesOffset"> the offset where to start reading values </param>
            /// <param name="iterations">   controls how much data to encode </param>
            void Encode(long[] values, int valuesOffset, byte[] blocks, int blocksOffset, int iterations);

            /// <summary>
            /// Read <code>iterations * valueCount()</code> values from <code>values</code>,
            /// encode them and write <code>iterations * blockCount()</code> blocks into
            /// <code>blocks</code>.
            /// </summary>
            /// <param name="blocks">       the long blocks that hold packed integer values </param>
            /// <param name="blocksOffset"> the offset where to start writing blocks </param>
            /// <param name="values">       the values buffer </param>
            /// <param name="valuesOffset"> the offset where to start reading values </param>
            /// <param name="iterations">   controls how much data to encode </param>
            void Encode(int[] values, int valuesOffset, long[] blocks, int blocksOffset, int iterations);

            /// <summary>
            /// Read <code>iterations * valueCount()</code> values from <code>values</code>,
            /// encode them and write <code>8 * iterations * blockCount()</code> blocks into
            /// <code>blocks</code>.
            /// </summary>
            /// <param name="blocks">       the long blocks that hold packed integer values </param>
            /// <param name="blocksOffset"> the offset where to start writing blocks </param>
            /// <param name="values">       the values buffer </param>
            /// <param name="valuesOffset"> the offset where to start reading values </param>
            /// <param name="iterations">   controls how much data to encode </param>
            void Encode(int[] values, int valuesOffset, byte[] blocks, int blocksOffset, int iterations);
        }

        /// <summary>
        /// A read-only random access array of positive integers.
        /// @lucene.internal
        /// </summary>
        public abstract class Reader : NumericDocValues
        {
            /// <summary>
            /// Bulk get: read at least one and at most <code>len</code> longs starting
            /// from <code>index</code> into <code>arr[off:off+len]</code> and return
            /// the actual number of values that have been read.
            /// </summary>
            public virtual int Get(int index, long[] arr, int off, int len)
            {
                Debug.Assert(len > 0, "len must be > 0 (got " + len + ")");
                Debug.Assert(index >= 0 && index < Size());
                Debug.Assert(off + len <= arr.Length);

                int gets = Math.Min(Size() - index, len);
                for (int i = index, o = off, end = index + gets; i < end; ++i, ++o)
                {
                    arr[o] = Get(i);
                }
                return gets;
            }

            /// <returns> the number of bits used to store any given value.
            ///         Note: this does not imply that memory usage is
            ///         {@code bitsPerValue * #values} as implementations are free to
            ///         use non-space-optimal packing of bits. </returns>
            public abstract int BitsPerValue { get; }

            /// <returns> the number of values. </returns>
            public abstract int Size(); // LUCENENET TODO: Make property, rename Count

            /// <summary>
            /// Return the in-memory size in bytes.
            /// </summary>
            public abstract long RamBytesUsed();

            /// <summary>
            /// Expert: if the bit-width of this reader matches one of
            /// java's native types, returns the underlying array
            /// (ie, byte[], short[], int[], long[]); else, returns
            /// null.  Note that when accessing the array you must
            /// upgrade the type (bitwise AND with all ones), to
            /// interpret the full value as unsigned.  Ie,
            /// bytes[idx]&0xFF, shorts[idx]&0xFFFF, etc.
            /// </summary>
            public virtual object Array // LUCENENET TODO: Change to GetArray() (returns array)
            {
                get
                {
                    Debug.Assert(!HasArray());
                    return null;
                }
            }

            /// <summary>
            /// Returns true if this implementation is backed by a
            /// native java array.
            /// </summary>
            /// <seealso cref= #getArray </seealso>
            public virtual bool HasArray() // LUCENENET TODO: Make property
            {
                return false;
            }
        }

        /// <summary>
        /// Run-once iterator interface, to decode previously saved PackedInts.
        /// </summary>
        public interface IReaderIterator
        {
            /// <summary>
            /// Returns next value </summary>
            long Next();

            /// <summary>
            /// Returns at least 1 and at most <code>count</code> next values,
            /// the returned ref MUST NOT be modified
            /// </summary>
            LongsRef Next(int count);

            /// <summary>
            /// Returns number of bits per value </summary>
            int BitsPerValue { get; }

            /// <summary>
            /// Returns number of values </summary>
            int Size(); // LUCENENET TODO: Make property, rename Count

            /// <summary>
            /// Returns the current position </summary>
            int Ord(); // LUCENENET TODO: Make property ? check consistency
        }

        // LUCENENET NOTE: Was ReaderIteratorImpl in Lucene
        internal abstract class ReaderIterator : IReaderIterator
        {
            // LUCENENET TODO: Rename with m_
            protected readonly DataInput @in;
            protected readonly int bitsPerValue;
            protected readonly int valueCount;

            protected ReaderIterator(int valueCount, int bitsPerValue, DataInput @in)
            {
                this.@in = @in;
                this.bitsPerValue = bitsPerValue;
                this.valueCount = valueCount;
            }

            public virtual long Next()
            {
                LongsRef nextValues = Next(1);
                Debug.Assert(nextValues.Length > 0);
                long result = nextValues.Longs[nextValues.Offset];
                ++nextValues.Offset;
                --nextValues.Length;
                return result;
            }

            public abstract LongsRef Next(int count);

            public virtual int BitsPerValue
            {
                get
                {
                    return bitsPerValue;
                }
            }

            public virtual int Size() // LUCENENET TODO: make property, rename Count
            {
                return valueCount;
            }

            public abstract int Ord(); // LUCENENET TODO: Make property ?
        }

        /// <summary>
        /// A packed integer array that can be modified.
        /// @lucene.internal
        /// </summary>
        public abstract class Mutable : Reader
        {
            /// <summary>
            /// Set the value at the given index in the array. </summary>
            /// <param name="index"> where the value should be positioned. </param>
            /// <param name="value"> a value conforming to the constraints set by the array. </param>
            public abstract void Set(int index, long value);

            /// <summary>
            /// Bulk set: set at least one and at most <code>len</code> longs starting
            /// at <code>off</code> in <code>arr</code> into this mutable, starting at
            /// <code>index</code>. Returns the actual number of values that have been
            /// set.
            /// </summary>
            public virtual int Set(int index, long[] arr, int off, int len)
            {
                Debug.Assert(len > 0, "len must be > 0 (got " + len + ")");
                Debug.Assert(index >= 0 && index < Size());
                len = Math.Min(len, Size() - index);
                Debug.Assert(off + len <= arr.Length);

                for (int i = index, o = off, end = index + len; i < end; ++i, ++o)
                {
                    Set(i, arr[o]);
                }
                return len;
            }

            /// <summary>
            /// Fill the mutable from <code>fromIndex</code> (inclusive) to
            /// <code>toIndex</code> (exclusive) with <code>val</code>.
            /// </summary>
            public virtual void Fill(int fromIndex, int toIndex, long val)
            {
                Debug.Assert(val <= MaxValue(BitsPerValue));
                Debug.Assert(fromIndex <= toIndex);
                for (int i = fromIndex; i < toIndex; ++i)
                {
                    Set(i, val);
                }
            }

            /// <summary>
            /// Sets all values to 0.
            /// </summary>
            public virtual void Clear()
            {
                Fill(0, Size(), 0);
            }

            /// <summary>
            /// Save this mutable into <code>out</code>. Instantiating a reader from
            /// the generated data will return a reader with the same number of bits
            /// per value.
            /// </summary>
            public virtual void Save(DataOutput @out)
            {
                Writer writer = GetWriterNoHeader(@out, Format, Size(), BitsPerValue, DEFAULT_BUFFER_SIZE);
                writer.WriteHeader();
                for (int i = 0; i < Size(); ++i)
                {
                    writer.Add(Get(i));
                }
                writer.Finish();
            }

            /// <summary>
            /// The underlying format. </summary>
            internal virtual Format Format
            {
                get
                {
                    return Format.PACKED;
                }
            }
        }

        /// <summary>
        /// A simple base for Readers that keeps track of valueCount and bitsPerValue.
        /// @lucene.internal
        /// </summary>
        internal abstract class ReaderImpl : Reader
        {
            // LUCENENET TODO: Rename with m_
            protected readonly int bitsPerValue;
            protected readonly int valueCount;

            protected ReaderImpl(int valueCount, int bitsPerValue)
            {
                this.bitsPerValue = bitsPerValue;
                Debug.Assert(bitsPerValue > 0 && bitsPerValue <= 64, "bitsPerValue=" + bitsPerValue);
                this.valueCount = valueCount;
            }

            public override abstract long Get(int index);

            public override sealed int BitsPerValue
            {
                get
                {
                    return bitsPerValue;
                }
            }

            public override sealed int Size()
            {
                return valueCount;
            }
        }

        public abstract class MutableImpl : Mutable
        {
            // LUCENENET TODO: Rename with m_
            protected readonly int valueCount;
            protected readonly int bitsPerValue;

            protected MutableImpl(int valueCount, int bitsPerValue)
            {
                this.valueCount = valueCount;
                Debug.Assert(bitsPerValue > 0 && bitsPerValue <= 64, "bitsPerValue=" + bitsPerValue);
                this.bitsPerValue = bitsPerValue;
            }

            public override sealed int BitsPerValue
            {
                get
                {
                    return bitsPerValue;
                }
            }

            public override sealed int Size()
            {
                return valueCount;
            }
        }

        /// <summary>
        /// A <seealso cref="Reader"/> which has all its values equal to 0 (bitsPerValue = 0). </summary>
        public sealed class NullReader : Reader
        {
            private readonly int valueCount;

            /// <summary>
            /// Sole constructor. </summary>
            public NullReader(int valueCount)
            {
                this.valueCount = valueCount;
            }

            public override long Get(int index)
            {
                return 0;
            }

            public override int Get(int index, long[] arr, int off, int len)
            {
                Debug.Assert(len > 0, "len must be > 0 (got " + len + ")");
                Debug.Assert(index >= 0 && index < valueCount);
                len = Math.Min(len, valueCount - index);
                Arrays.Fill(arr, off, off + len, 0);
                return len;
            }

            public override int BitsPerValue
            {
                get
                {
                    return 0;
                }
            }

            public override int Size()
            {
                return valueCount;
            }

            public override long RamBytesUsed()
            {
                return RamUsageEstimator.AlignObjectSize(RamUsageEstimator.NUM_BYTES_OBJECT_HEADER + RamUsageEstimator.NUM_BYTES_INT);
            }
        }

        /// <summary>
        /// A write-once Writer.
        /// @lucene.internal
        /// </summary>
        public abstract class Writer
        {
            // LUCENENET TODO: Rename with m_
            protected readonly DataOutput @out;
            protected readonly int valueCount;
            protected readonly int bitsPerValue;

            protected Writer(DataOutput @out, int valueCount, int bitsPerValue)
            {
                Debug.Assert(bitsPerValue <= 64);
                Debug.Assert(valueCount >= 0 || valueCount == -1);
                this.@out = @out;
                this.valueCount = valueCount;
                this.bitsPerValue = bitsPerValue;
            }

            internal virtual void WriteHeader()
            {
                Debug.Assert(valueCount != -1);
                CodecUtil.WriteHeader(@out, CODEC_NAME, VERSION_CURRENT);
                @out.WriteVInt(bitsPerValue);
                @out.WriteVInt(valueCount);
                @out.WriteVInt(Format.id);
            }

            /// <summary>
            /// The format used to serialize values. </summary>
            protected internal abstract PackedInts.Format Format { get; }

            /// <summary>
            /// Add a value to the stream. </summary>
            public abstract void Add(long v);

            /// <summary>
            /// The number of bits per value. </summary>
            public int BitsPerValue() // LUCENENET TODO: make property
            {
                return bitsPerValue;
            }

            /// <summary>
            /// Perform end-of-stream operations. </summary>
            public abstract void Finish();

            /// <summary>
            /// Returns the current ord in the stream (number of values that have been
            /// written so far minus one).
            /// </summary>
            public abstract int Ord(); // LUCENENET TODO: make property ? check consistency
        }

        /// <summary>
        /// Get a <seealso cref="IDecoder"/>.
        /// </summary>
        /// <param name="format">         the format used to store packed ints </param>
        /// <param name="version">        the compatibility version </param>
        /// <param name="bitsPerValue">   the number of bits per value </param>
        /// <returns> a decoder </returns>
        public static IDecoder GetDecoder(Format format, int version, int bitsPerValue)
        {
            CheckVersion(version);
            return BulkOperation.Of(format, bitsPerValue);
        }

        /// <summary>
        /// Get an <seealso cref="IEncoder"/>.
        /// </summary>
        /// <param name="format">         the format used to store packed ints </param>
        /// <param name="version">        the compatibility version </param>
        /// <param name="bitsPerValue">   the number of bits per value </param>
        /// <returns> an encoder </returns>
        public static IEncoder GetEncoder(Format format, int version, int bitsPerValue)
        {
            CheckVersion(version);
            return BulkOperation.Of(format, bitsPerValue);
        }

        /// <summary>
        /// Expert: Restore a <seealso cref="Reader"/> from a stream without reading metadata at
        /// the beginning of the stream. this method is useful to restore data from
        /// streams which have been created using
        /// <seealso cref="PackedInts#getWriterNoHeader(DataOutput, Format, int, int, int)"/>.
        /// </summary>
        /// <param name="in">           the stream to read data from, positioned at the beginning of the packed values </param>
        /// <param name="format">       the format used to serialize </param>
        /// <param name="version">      the version used to serialize the data </param>
        /// <param name="valueCount">   how many values the stream holds </param>
        /// <param name="bitsPerValue"> the number of bits per value </param>
        /// <returns>             a Reader </returns>
        /// <exception cref="IOException"> If there is a low-level I/O error </exception>
        /// <seealso cref= PackedInts#getWriterNoHeader(DataOutput, Format, int, int, int)
        /// @lucene.internal </seealso>
        public static Reader GetReaderNoHeader(DataInput @in, Format format, int version, int valueCount, int bitsPerValue)
        {
            CheckVersion(version);

            if (format == PackedInts.Format.PACKED_SINGLE_BLOCK)
            {
                return Packed64SingleBlock.Create(@in, valueCount, bitsPerValue);
            }
            else if (format == PackedInts.Format.PACKED)
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
                        return new Direct64(version, @in, valueCount);

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
                throw new InvalidOperationException("Unknown Writer format: " + format);
            }
        }

        /// <summary>
        /// Expert: Restore a <seealso cref="Reader"/> from a stream without reading metadata at
        /// the beginning of the stream. this method is useful to restore data when
        /// metadata has been previously read using <seealso cref="#readHeader(DataInput)"/>.
        /// </summary>
        /// <param name="in">           the stream to read data from, positioned at the beginning of the packed values </param>
        /// <param name="header">       metadata result from <code>readHeader()</code> </param>
        /// <returns>             a Reader </returns>
        /// <exception cref="IOException"> If there is a low-level I/O error </exception>
        /// <seealso cref= #readHeader(DataInput)
        /// @lucene.internal </seealso>
        public static Reader GetReaderNoHeader(DataInput @in, Header header)
        {
            return GetReaderNoHeader(@in, header.format, header.version, header.valueCount, header.bitsPerValue);
        }

        /// <summary>
        /// Restore a <seealso cref="Reader"/> from a stream.
        /// </summary>
        /// <param name="in">           the stream to read data from </param>
        /// <returns>             a Reader </returns>
        /// <exception cref="IOException"> If there is a low-level I/O error
        /// @lucene.internal </exception>
        public static Reader GetReader(DataInput @in)
        {
            int version = CodecUtil.CheckHeader(@in, CODEC_NAME, VERSION_START, VERSION_CURRENT);
            int bitsPerValue = @in.ReadVInt();
            Debug.Assert(bitsPerValue > 0 && bitsPerValue <= 64, "bitsPerValue=" + bitsPerValue);
            int valueCount = @in.ReadVInt();
            Format format = Format.ById(@in.ReadVInt());

            return GetReaderNoHeader(@in, format, version, valueCount, bitsPerValue);
        }

        /// <summary>
        /// Expert: Restore a <seealso cref="IReaderIterator"/> from a stream without reading
        /// metadata at the beginning of the stream. this method is useful to restore
        /// data from streams which have been created using
        /// <seealso cref="PackedInts#getWriterNoHeader(DataOutput, Format, int, int, int)"/>.
        /// </summary>
        /// <param name="in">           the stream to read data from, positioned at the beginning of the packed values </param>
        /// <param name="format">       the format used to serialize </param>
        /// <param name="version">      the version used to serialize the data </param>
        /// <param name="valueCount">   how many values the stream holds </param>
        /// <param name="bitsPerValue"> the number of bits per value </param>
        /// <param name="mem">          how much memory the iterator is allowed to use to read-ahead (likely to speed up iteration) </param>
        /// <returns>             a ReaderIterator </returns>
        /// <seealso cref= PackedInts#getWriterNoHeader(DataOutput, Format, int, int, int)
        /// @lucene.internal </seealso>
        public static IReaderIterator GetReaderIteratorNoHeader(DataInput @in, Format format, int version, int valueCount, int bitsPerValue, int mem)
        {
            CheckVersion(version);
            return new PackedReaderIterator(format, version, valueCount, bitsPerValue, @in, mem);
        }

        /// <summary>
        /// Retrieve PackedInts as a <seealso cref="IReaderIterator"/> </summary>
        /// <param name="in"> positioned at the beginning of a stored packed int structure. </param>
        /// <param name="mem"> how much memory the iterator is allowed to use to read-ahead (likely to speed up iteration) </param>
        /// <returns> an iterator to access the values </returns>
        /// <exception cref="IOException"> if the structure could not be retrieved.
        /// @lucene.internal </exception>
        public static IReaderIterator GetReaderIterator(DataInput @in, int mem)
        {
            int version = CodecUtil.CheckHeader(@in, CODEC_NAME, VERSION_START, VERSION_CURRENT);
            int bitsPerValue = @in.ReadVInt();
            Debug.Assert(bitsPerValue > 0 && bitsPerValue <= 64, "bitsPerValue=" + bitsPerValue);
            int valueCount = @in.ReadVInt();
            Format format = Format.ById(@in.ReadVInt());
            return GetReaderIteratorNoHeader(@in, format, version, valueCount, bitsPerValue, mem);
        }

        /// <summary>
        /// Expert: Construct a direct <seealso cref="Reader"/> from a stream without reading
        /// metadata at the beginning of the stream. this method is useful to restore
        /// data from streams which have been created using
        /// <seealso cref="PackedInts#getWriterNoHeader(DataOutput, Format, int, int, int)"/>.
        /// </p><p>
        /// The returned reader will have very little memory overhead, but every call
        /// to <seealso cref="Reader#get(int)"/> is likely to perform a disk seek.
        /// </summary>
        /// <param name="in">           the stream to read data from </param>
        /// <param name="format">       the format used to serialize </param>
        /// <param name="version">      the version used to serialize the data </param>
        /// <param name="valueCount">   how many values the stream holds </param>
        /// <param name="bitsPerValue"> the number of bits per value </param>
        /// <returns> a direct Reader
        /// @lucene.internal </returns>
        public static Reader GetDirectReaderNoHeader(IndexInput @in, Format format, int version, int valueCount, int bitsPerValue)
        {
            CheckVersion(version);

            if (format == PackedInts.Format.PACKED_SINGLE_BLOCK)
            {
                return new DirectPacked64SingleBlockReader(bitsPerValue, valueCount, @in);
            }
            else if (format == PackedInts.Format.PACKED)
            {
                long byteCount = format.ByteCount(version, valueCount, bitsPerValue);
                if (byteCount != format.ByteCount(VERSION_CURRENT, valueCount, bitsPerValue))
                {
                    Debug.Assert(version == VERSION_START);
                    long endPointer = @in.FilePointer + byteCount;
                    // Some consumers of direct readers assume that reading the last value
                    // will make the underlying IndexInput go to the end of the packed
                    // stream, but this is not true because packed ints storage used to be
                    // long-aligned and is now byte-aligned, hence this additional
                    // condition when reading the last value
                    return new DirectPackedReaderAnonymousInnerClassHelper(bitsPerValue, valueCount, @in, endPointer);
                }
                else
                {
                    return new DirectPackedReader(bitsPerValue, valueCount, @in);
                }
            }
            else
            {
                throw new InvalidOperationException("Unknwown format: " + format);
            }
        }

        private class DirectPackedReaderAnonymousInnerClassHelper : DirectPackedReader
        {
            private IndexInput @in;
            private int ValueCount;
            private long EndPointer;

            public DirectPackedReaderAnonymousInnerClassHelper(int bitsPerValue, int valueCount, IndexInput @in, long endPointer)
                : base(bitsPerValue, valueCount, @in)
            {
                this.@in = @in;
                this.ValueCount = valueCount;
                this.EndPointer = endPointer;
            }

            public override long Get(int index)
            {
                long result = base.Get(index);
                if (index == ValueCount - 1)
                {
                    try
                    {
                        @in.Seek(EndPointer);
                    }
                    catch (System.IO.IOException e)
                    {
                        throw new InvalidOperationException("failed", e);
                    }
                }
                return result;
            }
        }

        /// <summary>
        /// Expert: Construct a direct <seealso cref="Reader"/> from an <seealso cref="IndexInput"/>
        /// without reading metadata at the beginning of the stream. this method is
        /// useful to restore data when metadata has been previously read using
        /// <seealso cref="#readHeader(DataInput)"/>.
        /// </summary>
        /// <param name="in">           the stream to read data from, positioned at the beginning of the packed values </param>
        /// <param name="header">       metadata result from <code>readHeader()</code> </param>
        /// <returns>             a Reader </returns>
        /// <exception cref="IOException"> If there is a low-level I/O error </exception>
        /// <seealso cref= #readHeader(DataInput)
        /// @lucene.internal </seealso>
        public static Reader GetDirectReaderNoHeader(IndexInput @in, Header header)
        {
            return GetDirectReaderNoHeader(@in, header.format, header.version, header.valueCount, header.bitsPerValue);
        }

        /// <summary>
        /// Construct a direct <seealso cref="Reader"/> from an <seealso cref="IndexInput"/>. this method
        /// is useful to restore data from streams which have been created using
        /// <seealso cref="PackedInts#getWriter(DataOutput, int, int, float)"/>.
        /// </p><p>
        /// The returned reader will have very little memory overhead, but every call
        /// to <seealso cref="Reader#get(int)"/> is likely to perform a disk seek.
        /// </summary>
        /// <param name="in">           the stream to read data from </param>
        /// <returns> a direct Reader </returns>
        /// <exception cref="IOException"> If there is a low-level I/O error
        /// @lucene.internal </exception>
        public static Reader GetDirectReader(IndexInput @in)
        {
            int version = CodecUtil.CheckHeader(@in, CODEC_NAME, VERSION_START, VERSION_CURRENT);
            int bitsPerValue = @in.ReadVInt();
            Debug.Assert(bitsPerValue > 0 && bitsPerValue <= 64, "bitsPerValue=" + bitsPerValue);
            int valueCount = @in.ReadVInt();
            Format format = Format.ById(@in.ReadVInt());
            return GetDirectReaderNoHeader(@in, format, version, valueCount, bitsPerValue);
        }

        /// <summary>
        /// Create a packed integer array with the given amount of values initialized
        /// to 0. the valueCount and the bitsPerValue cannot be changed after creation.
        /// All Mutables known by this factory are kept fully in RAM.
        /// </p><p>
        /// Positive values of <code>acceptableOverheadRatio</code> will trade space
        /// for speed by selecting a faster but potentially less memory-efficient
        /// implementation. An <code>acceptableOverheadRatio</code> of
        /// <seealso cref="PackedInts#COMPACT"/> will make sure that the most memory-efficient
        /// implementation is selected whereas <seealso cref="PackedInts#FASTEST"/> will make sure
        /// that the fastest implementation is selected.
        /// </summary>
        /// <param name="valueCount">   the number of elements </param>
        /// <param name="bitsPerValue"> the number of bits available for any given value </param>
        /// <param name="acceptableOverheadRatio"> an acceptable overhead
        ///        ratio per value </param>
        /// <returns> a mutable packed integer array
        /// @lucene.internal </returns>
        public static Mutable GetMutable(int valueCount, int bitsPerValue, float acceptableOverheadRatio)
        {
            FormatAndBits formatAndBits = FastestFormatAndBits(valueCount, bitsPerValue, acceptableOverheadRatio);
            return GetMutable(valueCount, formatAndBits.bitsPerValue, formatAndBits.format);
        }

        /// <summary>
        /// Same as <seealso cref="#getMutable(int, int, float)"/> with a pre-computed number
        ///  of bits per value and format.
        ///  @lucene.internal
        /// </summary>
        public static Mutable GetMutable(int valueCount, int bitsPerValue, PackedInts.Format format)
        {
            Debug.Assert(valueCount >= 0);

            if (format == PackedInts.Format.PACKED_SINGLE_BLOCK)
            {
                return Packed64SingleBlock.Create(valueCount, bitsPerValue);
            }
            else if (format == PackedInts.Format.PACKED)
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
                throw new InvalidOperationException();
            }
        }

        /// <summary>
        /// Expert: Create a packed integer array writer for the given output, format,
        /// value count, and number of bits per value.
        /// </p><p>
        /// The resulting stream will be long-aligned. this means that depending on
        /// the format which is used, up to 63 bits will be wasted. An easy way to
        /// make sure that no space is lost is to always use a <code>valueCount</code>
        /// that is a multiple of 64.
        /// </p><p>
        /// this method does not write any metadata to the stream, meaning that it is
        /// your responsibility to store it somewhere else in order to be able to
        /// recover data from the stream later on:
        /// <ul>
        ///   <li><code>format</code> (using <seealso cref="Format#getId()"/>),</li>
        ///   <li><code>valueCount</code>,</li>
        ///   <li><code>bitsPerValue</code>,</li>
        ///   <li><seealso cref="#VERSION_CURRENT"/>.</li>
        /// </ul>
        /// </p><p>
        /// It is possible to start writing values without knowing how many of them you
        /// are actually going to write. To do this, just pass <code>-1</code> as
        /// <code>valueCount</code>. On the other hand, for any positive value of
        /// <code>valueCount</code>, the returned writer will make sure that you don't
        /// write more values than expected and pad the end of stream with zeros in
        /// case you have written less than <code>valueCount</code> when calling
        /// <seealso cref="Writer#finish()"/>.
        /// </p><p>
        /// The <code>mem</code> parameter lets you control how much memory can be used
        /// to buffer changes in memory before flushing to disk. High values of
        /// <code>mem</code> are likely to improve throughput. On the other hand, if
        /// speed is not that important to you, a value of <code>0</code> will use as
        /// little memory as possible and should already offer reasonable throughput.
        /// </summary>
        /// <param name="out">          the data output </param>
        /// <param name="format">       the format to use to serialize the values </param>
        /// <param name="valueCount">   the number of values </param>
        /// <param name="bitsPerValue"> the number of bits per value </param>
        /// <param name="mem">          how much memory (in bytes) can be used to speed up serialization </param>
        /// <returns>             a Writer </returns>
        /// <seealso cref= PackedInts#getReaderIteratorNoHeader(DataInput, Format, int, int, int, int) </seealso>
        /// <seealso cref= PackedInts#getReaderNoHeader(DataInput, Format, int, int, int)
        /// @lucene.internal </seealso>
        public static Writer GetWriterNoHeader(DataOutput @out, Format format, int valueCount, int bitsPerValue, int mem)
        {
            return new PackedWriter(format, @out, valueCount, bitsPerValue, mem);
        }

        /// <summary>
        /// Create a packed integer array writer for the given output, format, value
        /// count, and number of bits per value.
        /// </p><p>
        /// The resulting stream will be long-aligned. this means that depending on
        /// the format which is used under the hoods, up to 63 bits will be wasted.
        /// An easy way to make sure that no space is lost is to always use a
        /// <code>valueCount</code> that is a multiple of 64.
        /// </p><p>
        /// this method writes metadata to the stream, so that the resulting stream is
        /// sufficient to restore a <seealso cref="Reader"/> from it. You don't need to track
        /// <code>valueCount</code> or <code>bitsPerValue</code> by yourself. In case
        /// this is a problem, you should probably look at
        /// <seealso cref="#getWriterNoHeader(DataOutput, Format, int, int, int)"/>.
        /// </p><p>
        /// The <code>acceptableOverheadRatio</code> parameter controls how
        /// readers that will be restored from this stream trade space
        /// for speed by selecting a faster but potentially less memory-efficient
        /// implementation. An <code>acceptableOverheadRatio</code> of
        /// <seealso cref="PackedInts#COMPACT"/> will make sure that the most memory-efficient
        /// implementation is selected whereas <seealso cref="PackedInts#FASTEST"/> will make sure
        /// that the fastest implementation is selected. In case you are only interested
        /// in reading this stream sequentially later on, you should probably use
        /// <seealso cref="PackedInts#COMPACT"/>.
        /// </summary>
        /// <param name="out">          the data output </param>
        /// <param name="valueCount">   the number of values </param>
        /// <param name="bitsPerValue"> the number of bits per value </param>
        /// <param name="acceptableOverheadRatio"> an acceptable overhead ratio per value </param>
        /// <returns>             a Writer </returns>
        /// <exception cref="IOException"> If there is a low-level I/O error
        /// @lucene.internal </exception>
        public static Writer GetWriter(DataOutput @out, int valueCount, int bitsPerValue, float acceptableOverheadRatio)
        {
            Debug.Assert(valueCount >= 0);

            FormatAndBits formatAndBits = FastestFormatAndBits(valueCount, bitsPerValue, acceptableOverheadRatio);
            Writer writer = GetWriterNoHeader(@out, formatAndBits.format, valueCount, formatAndBits.bitsPerValue, DEFAULT_BUFFER_SIZE);
            writer.WriteHeader();
            return writer;
        }

        /// <summary>
        /// Returns how many bits are required to hold values up
        ///  to and including maxValue </summary>
        /// <param name="maxValue"> the maximum value that should be representable. </param>
        /// <returns> the amount of bits needed to represent values from 0 to maxValue.
        /// @lucene.internal </returns>
        public static int BitsRequired(long maxValue)
        {
            if (maxValue < 0)
            {
                throw new System.ArgumentException("maxValue must be non-negative (got: " + maxValue + ")");
            }
            return Math.Max(1, 64 - Number.NumberOfLeadingZeros(maxValue));
        }

        /// <summary>
        /// Calculates the maximum unsigned long that can be expressed with the given
        /// number of bits. </summary>
        /// <param name="bitsPerValue"> the number of bits available for any given value. </param>
        /// <returns> the maximum value for the given bits.
        /// @lucene.internal </returns>
        public static long MaxValue(int bitsPerValue)
        {
            return bitsPerValue == 64 ? long.MaxValue : ~(~0L << bitsPerValue);
        }

        /// <summary>
        /// Copy <code>src[srcPos:srcPos+len]</code> into
        /// <code>dest[destPos:destPos+len]</code> using at most <code>mem</code>
        /// bytes.
        /// </summary>
        public static void Copy(Reader src, int srcPos, Mutable dest, int destPos, int len, int mem)
        {
            Debug.Assert(srcPos + len <= src.Size());
            Debug.Assert(destPos + len <= dest.Size());
            int capacity = (int)((uint)mem >> 3);
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
        /// Same as <seealso cref="#copy(Reader, int, Mutable, int, int, int)"/> but using a pre-allocated buffer. </summary>
        internal static void Copy(Reader src, int srcPos, Mutable dest, int destPos, int len, long[] buf)
        {
            Debug.Assert(buf.Length > 0);
            int remaining = 0;
            while (len > 0)
            {
                int read = src.Get(srcPos, buf, remaining, Math.Min(len, buf.Length - remaining));
                Debug.Assert(read > 0);
                srcPos += read;
                len -= read;
                remaining += read;
                int written = dest.Set(destPos, buf, 0, remaining);
                Debug.Assert(written > 0);
                destPos += written;
                if (written < remaining)
                {
                    Array.Copy(buf, written, buf, 0, remaining - written);
                }
                remaining -= written;
            }
            while (remaining > 0)
            {
                int written = dest.Set(destPos, buf, 0, remaining);
                destPos += written;
                remaining -= written;
                Array.Copy(buf, written, buf, 0, remaining);
            }
        }

        /// <summary>
        /// Expert: reads only the metadata from a stream. this is useful to later
        /// restore a stream or open a direct reader via
        /// <seealso cref="#getReaderNoHeader(DataInput, Header)"/>
        /// or <seealso cref="#getDirectReaderNoHeader(IndexInput, Header)"/>. </summary>
        /// <param name="in"> the stream to read data </param>
        /// <returns>   packed integer metadata. </returns>
        /// <exception cref="IOException"> If there is a low-level I/O error </exception>
        /// <seealso cref= #getReaderNoHeader(DataInput, Header) </seealso>
        /// <seealso cref= #getDirectReaderNoHeader(IndexInput, Header) </seealso>
        public static Header ReadHeader(DataInput @in)
        {
            int version = CodecUtil.CheckHeader(@in, CODEC_NAME, VERSION_START, VERSION_CURRENT);
            int bitsPerValue = @in.ReadVInt();
            Debug.Assert(bitsPerValue > 0 && bitsPerValue <= 64, "bitsPerValue=" + bitsPerValue);
            int valueCount = @in.ReadVInt();
            Format format = Format.ById(@in.ReadVInt());
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
        ///  its log in base 2.
        /// </summary>
        internal static int CheckBlockSize(int blockSize, int minBlockSize, int maxBlockSize)
        {
            if (blockSize < minBlockSize || blockSize > maxBlockSize)
            {
                throw new System.ArgumentException("blockSize must be >= " + minBlockSize + " and <= " + maxBlockSize + ", got " + blockSize);
            }
            if ((blockSize & (blockSize - 1)) != 0)
            {
                throw new System.ArgumentException("blockSize must be a power of two, got " + blockSize);
            }
            return Number.NumberOfTrailingZeros(blockSize);
        }

        /// <summary>
        /// Return the number of blocks required to store <code>size</code> values on
        ///  <code>blockSize</code>.
        /// </summary>
        internal static int NumBlocks(long size, int blockSize)
        {
            int numBlocks = (int)(size / blockSize) + (size % blockSize == 0 ? 0 : 1);
            if ((long)numBlocks * blockSize < size)
            {
                throw new System.ArgumentException("size is too large for this block size");
            }
            return numBlocks;
        }
    }
}