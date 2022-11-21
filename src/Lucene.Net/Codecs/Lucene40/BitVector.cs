using J2N.Numerics;
using Lucene.Net.Diagnostics;
using Lucene.Net.Support;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using BitUtil = Lucene.Net.Util.BitUtil;

namespace Lucene.Net.Codecs.Lucene40
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

    using ChecksumIndexInput = Lucene.Net.Store.ChecksumIndexInput;
    using CompoundFileDirectory = Lucene.Net.Store.CompoundFileDirectory;
    using Directory = Lucene.Net.Store.Directory;
    using IndexInput = Lucene.Net.Store.IndexInput;
    using IndexOutput = Lucene.Net.Store.IndexOutput;
    using IOContext = Lucene.Net.Store.IOContext;
    using IOUtils = Lucene.Net.Util.IOUtils;
    using IMutableBits = Lucene.Net.Util.IMutableBits;

    /// <summary>
    /// Optimized implementation of a vector of bits.  This is more-or-less like
    /// <c>java.util.BitSet</c>, but also includes the following:
    /// <list type="bullet">
    ///     <item><description>a count() method, which efficiently computes the number of one bits;</description></item>
    ///     <item><description>optimized read from and write to disk;</description></item>
    ///     <item><description>inlinable get() method;</description></item>
    ///     <item><description>store and load, as bit set or d-gaps, depending on sparseness;</description></item>
    /// </list>
    /// <para/>
    /// @lucene.internal
    /// </summary>
    // pkg-private: if this thing is generally useful then it can go back in .util,
    // but the serialization must be here underneath the codec.
    internal sealed class BitVector : IMutableBits // LUCENENET specific: Not implementing ICloneable per Microsoft's recommendation
    {
        private byte[] bits;
        private int size;
        private int count;
        private readonly int version; // LUCENENET: marked readonly

        /// <summary>
        /// Constructs a vector capable of holding <paramref name="n"/> bits. </summary>
        public BitVector(int n)
        {
            size = n;
            bits = new byte[GetNumBytes(size)];
            count = 0;
        }

        internal BitVector(byte[] bits, int size)
        {
            this.bits = bits;
            this.size = size;
            count = -1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int GetNumBytes(int size) // LUCENENET: CA1822: Mark members as static
        {
            int bytesLength = size.TripleShift(3);
            if ((size & 7) != 0)
            {
                bytesLength++;
            }
            return bytesLength;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public object Clone()
        {
            byte[] copyBits = new byte[bits.Length];
            Arrays.Copy(bits, 0, copyBits, 0, bits.Length);
            BitVector clone = new BitVector(copyBits, size);
            clone.count = count;
            return clone;
        }

        /// <summary>
        /// Sets the value of <paramref name="bit"/> to one. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Set(int bit)
        {
            if (bit >= size)
            {
                throw new IndexOutOfRangeException("bit=" + bit + " size=" + size);
            }
            bits[bit >> 3] |= (byte)(1 << (bit & 7));
            count = -1;
        }

        /// <summary>
        /// Sets the value of <paramref name="bit"/> to <c>true</c>, and
        /// returns <c>true</c> if bit was already set.
        /// </summary>
        public bool GetAndSet(int bit)
        {
            if (bit >= size)
            {
                throw new IndexOutOfRangeException("bit=" + bit + " size=" + size);
            }
            int pos = bit >> 3;
            int v = bits[pos];
            int flag = 1 << (bit & 7);
            if ((flag & v) != 0)
            {
                return true;
            }
            else
            {
                bits[pos] = (byte)(v | flag);
                if (count != -1)
                {
                    count++;
                    if (Debugging.AssertsEnabled) Debugging.Assert(count <= size);
                }
                return false;
            }
        }

        /// <summary>
        /// Sets the value of <paramref name="bit"/> to zero. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear(int bit)
        {
            if (bit >= size)
            {
                throw new IndexOutOfRangeException(bit.ToString());
            }
            bits[bit >> 3] &= (byte)(~(1 << (bit & 7)));
            count = -1;
        }

        public bool GetAndClear(int bit)
        {
            if (bit >= size)
            {
                throw new IndexOutOfRangeException(bit.ToString());
            }
            int pos = bit >> 3;
            int v = bits[pos];
            int flag = 1 << (bit & 7);
            if ((flag & v) == 0)
            {
                return false;
            }
            else
            {
                bits[pos] &= (byte)(~flag);
                if (count != -1)
                {
                    count--;
                    if (Debugging.AssertsEnabled) Debugging.Assert(count >= 0);
                }
                return true;
            }
        }

        /// <summary>
        /// Returns <c>true</c> if <paramref name="bit"/> is one and
        /// <c>false</c> if it is zero.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Get(int bit)
        {
            if (Debugging.AssertsEnabled) Debugging.Assert(bit >= 0 && bit < size,"bit {0} is out of bounds 0..{1}", bit, (size - 1));
            return (bits[bit >> 3] & (1 << (bit & 7))) != 0;
        }

        // LUCENENET specific - removing this because 1) size is not .NETified and 2) it is identical to Length anyway
        ///// <summary>
        ///// Returns the number of bits in this vector.  this is also one greater than
        /////  the number of the largest valid bit number.
        ///// </summary>
        //public int Size()
        //{
        //    return size;
        //}

        /// <summary>
        /// Returns the number of bits in this vector.  This is also one greater than
        /// the number of the largest valid bit number.
        /// <para/>
        /// This is the equivalent of either size() or length() in Lucene.
        /// </summary>
        public int Length => size;

        /// <summary>
        /// Returns the total number of one bits in this vector.  This is efficiently
        /// computed and cached, so that, if the vector is not changed, no
        /// recomputation is done for repeated calls.
        /// </summary>
        public int Count() // LUCENENET TODO: API - make into a property
        {
            // if the vector has been modified
            if (count == -1)
            {
                int c = 0;
                int end = bits.Length;
                for (int i = 0; i < end; i++)
                {
                    c += BitUtil.BitCount(bits[i]); // sum bits per byte
                }
                count = c;
            }
            if (Debugging.AssertsEnabled) Debugging.Assert(count <= size,"count={0} size={1}", count, size);
            return count;
        }

        /// <summary>
        /// For testing </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetRecomputedCount()
        {
            int c = 0;
            int end = bits.Length;
            for (int i = 0; i < end; i++)
            {
                c += BitUtil.BitCount(bits[i]); // sum bits per byte
            }
            return c;
        }

        private const string CODEC = "BitVector";

        // Version before version tracking was added:
        public const int VERSION_PRE = -1;

        // First version:
        public const int VERSION_START = 0;

        // Changed DGaps to encode gaps between cleared bits, not
        // set:
        public const int VERSION_DGAPS_CLEARED = 1;

        // added checksum
        public const int VERSION_CHECKSUM = 2;

        // Increment version to change it:
        public const int VERSION_CURRENT = VERSION_CHECKSUM;

        public int Version => version;

        /// <summary>
        /// Writes this vector to the file <paramref name="name"/> in Directory
        /// <paramref name="d"/>, in a format that can be read by the constructor 
        /// <see cref="BitVector(Directory, string, IOContext)"/>.
        /// </summary>
        public void Write(Directory d, string name, IOContext context)
        {
            if (Debugging.AssertsEnabled) Debugging.Assert(!(d is CompoundFileDirectory));
            IndexOutput output = d.CreateOutput(name, context);
            try
            {
                output.WriteInt32(-2);
                CodecUtil.WriteHeader(output, CODEC, VERSION_CURRENT);
                if (IsSparse)
                {
                    // sparse bit-set more efficiently saved as d-gaps.
                    WriteClearedDgaps(output);
                }
                else
                {
                    WriteBits(output);
                }
                CodecUtil.WriteFooter(output);
                if (Debugging.AssertsEnabled) Debugging.Assert(VerifyCount());
            }
            finally
            {
                IOUtils.Dispose(output);
            }
        }

        /// <summary>
        /// Invert all bits. </summary>
        public void InvertAll()
        {
            if (count != -1)
            {
                count = size - count;
            }
            if (bits.Length > 0)
            {
                for (int idx = 0; idx < bits.Length; idx++)
                {
                    bits[idx] = (byte)(~bits[idx]);
                }
                ClearUnusedBits();
            }
        }

        private void ClearUnusedBits()
        {
            // Take care not to invert the "unused" bits in the
            // last byte:
            if (bits.Length > 0)
            {
                int lastNBits = size & 7;
                if (lastNBits != 0)
                {
                    int mask = (1 << lastNBits) - 1;
                    bits[bits.Length - 1] &= (byte)mask;
                }
            }
        }

        /// <summary>
        /// Set all bits. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetAll()
        {
            Arrays.Fill(bits, (byte)0xff);
            ClearUnusedBits();
            count = size;
        }

        /// <summary>
        /// Write as a bit set. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void WriteBits(IndexOutput output)
        {
            output.WriteInt32(Length); // write size
            output.WriteInt32(Count()); // write count
            output.WriteBytes(bits, bits.Length);
        }

        /// <summary>
        /// Write as a d-gaps list. </summary>
        private void WriteClearedDgaps(IndexOutput output)
        {
            output.WriteInt32(-1); // mark using d-gaps
            output.WriteInt32(Length); // write size
            output.WriteInt32(Count()); // write count
            int last = 0;
            int numCleared = Length - Count();
            for (int i = 0; i < bits.Length && numCleared > 0; i++)
            {
                if (bits[i] != 0xff)
                {
                    output.WriteVInt32(i - last);
                    output.WriteByte(bits[i]);
                    last = i;
                    numCleared -= (8 - BitUtil.BitCount(bits[i]));
                    if (Debugging.AssertsEnabled) Debugging.Assert(numCleared >= 0 || (i == (bits.Length - 1) && numCleared == -(8 - (size & 7))));
                }
            }
        }

        /// <summary>
        /// Indicates if the bit vector is sparse and should be saved as a d-gaps list, or dense, and should be saved as a bit set. </summary>
        private bool IsSparse
        {
            get
            {
                int clearedCount = Length - Count();
                if (clearedCount == 0)
                {
                    return true;
                }

                int avgGapLength = bits.Length / clearedCount;

                // expected number of bytes for vInt encoding of each gap
                int expectedDGapBytes;
                if (avgGapLength <= (1 << 7))
                {
                    expectedDGapBytes = 1;
                }
                else if (avgGapLength <= (1 << 14))
                {
                    expectedDGapBytes = 2;
                }
                else if (avgGapLength <= (1 << 21))
                {
                    expectedDGapBytes = 3;
                }
                else if (avgGapLength <= (1 << 28))
                {
                    expectedDGapBytes = 4;
                }
                else
                {
                    expectedDGapBytes = 5;
                }

                // +1 because we write the byte itself that contains the
                // set bit
                int bytesPerSetBit = expectedDGapBytes + 1;

                // note: adding 32 because we start with ((int) -1) to indicate d-gaps format.
                long expectedBits = 32 + 8 * bytesPerSetBit * clearedCount;

                // note: factor is for read/write of byte-arrays being faster than vints.
                const long factor = 10;
                return factor * expectedBits < Length;
            }
        }

        /// <summary>
        /// Constructs a bit vector from the file <paramref name="name"/> in Directory
        /// <paramref name="d"/>, as written by the <see cref="Write(Directory, string, IOContext)"/> method.
        /// </summary>
        public BitVector(Directory d, string name, IOContext context)
        {
            ChecksumIndexInput input = d.OpenChecksumInput(name, context);

            try
            {
                int firstInt = input.ReadInt32();

                if (firstInt == -2)
                {
                    // New format, with full header & version:
                    version = CodecUtil.CheckHeader(input, CODEC, VERSION_START, VERSION_CURRENT);
                    size = input.ReadInt32();
                }
                else
                {
                    version = VERSION_PRE;
                    size = firstInt;
                }
                if (size == -1)
                {
                    if (version >= VERSION_DGAPS_CLEARED)
                    {
                        ReadClearedDgaps(input);
                    }
                    else
                    {
                        ReadSetDgaps(input);
                    }
                }
                else
                {
                    ReadBits(input);
                }

                if (version < VERSION_DGAPS_CLEARED)
                {
                    InvertAll();
                }

                if (version >= VERSION_CHECKSUM)
                {
                    CodecUtil.CheckFooter(input);
                }
                else
                {
#pragma warning disable 612, 618
                    CodecUtil.CheckEOF(input);
#pragma warning restore 612, 618
                }
                if (Debugging.AssertsEnabled) Debugging.Assert(VerifyCount());
            }
            finally
            {
                input.Dispose();
            }
        }

        // asserts only
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool VerifyCount()
        {
            if (Debugging.AssertsEnabled) Debugging.Assert(count != -1);
            int countSav = count;
            count = -1;
            if (Debugging.AssertsEnabled) Debugging.Assert(countSav == Count(),"saved count was {0} but recomputed count is {1}", countSav, count);
            return true;
        }

        /// <summary>
        /// Read as a bit set. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ReadBits(IndexInput input)
        {
            count = input.ReadInt32(); // read count
            bits = new byte[GetNumBytes(size)]; // allocate bits
            input.ReadBytes(bits, 0, bits.Length);
        }

        /// <summary>
        /// Read as a d-gaps list. </summary>
        private void ReadSetDgaps(IndexInput input)
        {
            size = input.ReadInt32(); // (re)read size
            count = input.ReadInt32(); // read count
            bits = new byte[GetNumBytes(size)]; // allocate bits
            int last = 0;
            int n = Count();
            while (n > 0)
            {
                last += input.ReadVInt32();
                bits[last] = input.ReadByte();
                n -= BitUtil.BitCount(bits[last]);
                if (Debugging.AssertsEnabled) Debugging.Assert(n >= 0);
            }
        }

        /// <summary>
        /// Read as a d-gaps cleared bits list. </summary>
        private void ReadClearedDgaps(IndexInput input)
        {
            size = input.ReadInt32(); // (re)read size
            count = input.ReadInt32(); // read count
            bits = new byte[GetNumBytes(size)]; // allocate bits
            Arrays.Fill(bits, (byte)0xff);
            ClearUnusedBits();
            int last = 0;
            int numCleared = Length - Count();
            while (numCleared > 0)
            {
                last += input.ReadVInt32();
                bits[last] = input.ReadByte();
                numCleared -= 8 - BitUtil.BitCount(bits[last]);
                if (Debugging.AssertsEnabled) Debugging.Assert(numCleared >= 0 || (last == (bits.Length - 1) && numCleared == -(8 - (size & 7))));
            }
        }
    }
}