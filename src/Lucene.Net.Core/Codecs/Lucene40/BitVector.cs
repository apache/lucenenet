using System;
using System.Diagnostics;

namespace Lucene.Net.Codecs.Lucene40
{
    using Lucene.Net.Support;
    using BitUtil = Lucene.Net.Util.BitUtil;

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
    using MutableBits = Lucene.Net.Util.MutableBits;

    /// <summary>
    /// Optimized implementation of a vector of bits.  this is more-or-less like
    ///  java.util.BitSet, but also includes the following:
    ///  <ul>
    ///  <li>a count() method, which efficiently computes the number of one bits;</li>
    ///  <li>optimized read from and write to disk;</li>
    ///  <li>inlinable get() method;</li>
    ///  <li>store and load, as bit set or d-gaps, depending on sparseness;</li>
    ///  </ul>
    ///
    ///  @lucene.internal
    /// </summary>
    // pkg-private: if this thing is generally useful then it can go back in .util,
    // but the serialization must be here underneath the codec.
    public sealed class BitVector : MutableBits
    {
        private byte[] Bits;
        private int Size_Renamed;
        private int Count_Renamed;
        private int Version_Renamed;

        /// <summary>
        /// Constructs a vector capable of holding <code>n</code> bits. </summary>
        public BitVector(int n)
        {
            Size_Renamed = n;
            Bits = new byte[GetNumBytes(Size_Renamed)];
            Count_Renamed = 0;
        }

        internal BitVector(byte[] bits, int size)
        {
            this.Bits = bits;
            this.Size_Renamed = size;
            Count_Renamed = -1;
        }

        private int GetNumBytes(int size)
        {
            int bytesLength = (int)((uint)size >> 3);
            if ((size & 7) != 0)
            {
                bytesLength++;
            }
            return bytesLength;
        }

        public object Clone()
        {
            byte[] copyBits = new byte[Bits.Length];
            Array.Copy(Bits, 0, copyBits, 0, Bits.Length);
            BitVector clone = new BitVector(copyBits, Size_Renamed);
            clone.Count_Renamed = Count_Renamed;
            return clone;
        }

        /// <summary>
        /// Sets the value of <code>bit</code> to one. </summary>
        public void Set(int bit)
        {
            if (bit >= Size_Renamed)
            {
                throw new System.IndexOutOfRangeException("bit=" + bit + " size=" + Size_Renamed);
            }
            Bits[bit >> 3] |= (byte)(1 << (bit & 7));
            Count_Renamed = -1;
        }

        /// <summary>
        /// Sets the value of <code>bit</code> to true, and
        ///  returns true if bit was already set
        /// </summary>
        public bool GetAndSet(int bit)
        {
            if (bit >= Size_Renamed)
            {
                throw new System.IndexOutOfRangeException("bit=" + bit + " size=" + Size_Renamed);
            }
            int pos = bit >> 3;
            int v = Bits[pos];
            int flag = 1 << (bit & 7);
            if ((flag & v) != 0)
            {
                return true;
            }
            else
            {
                Bits[pos] = (byte)(v | flag);
                if (Count_Renamed != -1)
                {
                    Count_Renamed++;
                    Debug.Assert(Count_Renamed <= Size_Renamed);
                }
                return false;
            }
        }

        /// <summary>
        /// Sets the value of <code>bit</code> to zero. </summary>
        public void Clear(int bit)
        {
            if (bit >= Size_Renamed)
            {
                throw new System.IndexOutOfRangeException(bit.ToString());
            }
            Bits[bit >> 3] &= (byte)(~(1 << (bit & 7)));
            Count_Renamed = -1;
        }

        public bool GetAndClear(int bit)
        {
            if (bit >= Size_Renamed)
            {
                throw new System.IndexOutOfRangeException(bit.ToString());
            }
            int pos = bit >> 3;
            int v = Bits[pos];
            int flag = 1 << (bit & 7);
            if ((flag & v) == 0)
            {
                return false;
            }
            else
            {
                Bits[pos] &= (byte)(~flag);
                if (Count_Renamed != -1)
                {
                    Count_Renamed--;
                    Debug.Assert(Count_Renamed >= 0);
                }
                return true;
            }
        }

        /// <summary>
        /// Returns <code>true</code> if <code>bit</code> is one and
        ///  <code>false</code> if it is zero.
        /// </summary>
        public bool Get(int bit)
        {
            Debug.Assert(bit >= 0 && bit < Size_Renamed, "bit " + bit + " is out of bounds 0.." + (Size_Renamed - 1));
            return (Bits[bit >> 3] & (1 << (bit & 7))) != 0;
        }

        /// <summary>
        /// Returns the number of bits in this vector.  this is also one greater than
        ///  the number of the largest valid bit number.
        /// </summary>
        public int Size()
        {
            return Size_Renamed;
        }

        public int Length()
        {
            return Size_Renamed;
        }

        /// <summary>
        /// Returns the total number of one bits in this vector.  this is efficiently
        ///  computed and cached, so that, if the vector is not changed, no
        ///  recomputation is done for repeated calls.
        /// </summary>
        public int Count()
        {
            // if the vector has been modified
            if (Count_Renamed == -1)
            {
                int c = 0;
                int end = Bits.Length;
                for (int i = 0; i < end; i++)
                {
                    c += BitUtil.BitCount(Bits[i]); // sum bits per byte
                }
                Count_Renamed = c;
            }
            Debug.Assert(Count_Renamed <= Size_Renamed, "count=" + Count_Renamed + " size=" + Size_Renamed);
            return Count_Renamed;
        }

        /// <summary>
        /// For testing </summary>
        public int RecomputedCount
        {
            get
            {
                int c = 0;
                int end = Bits.Length;
                for (int i = 0; i < end; i++)
                {
                    c += BitUtil.BitCount(Bits[i]); // sum bits per byte
                }
                return c;
            }
        }

        private static string CODEC = "BitVector";

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

        public int Version
        {
            get
            {
                return Version_Renamed;
            }
        }

        /// <summary>
        /// Writes this vector to the file <code>name</code> in Directory
        ///  <code>d</code>, in a format that can be read by the constructor {@link
        ///  #BitVector(Directory, String, IOContext)}.
        /// </summary>
        public void Write(Directory d, string name, IOContext context)
        {
            Debug.Assert(!(d is CompoundFileDirectory));
            IndexOutput output = d.CreateOutput(name, context);
            try
            {
                output.WriteInt(-2);
                CodecUtil.WriteHeader(output, CODEC, VERSION_CURRENT);
                if (Sparse)
                {
                    // sparse bit-set more efficiently saved as d-gaps.
                    WriteClearedDgaps(output);
                }
                else
                {
                    WriteBits(output);
                }
                CodecUtil.WriteFooter(output);
                Debug.Assert(VerifyCount());
            }
            finally
            {
                IOUtils.Close(output);
            }
        }

        /// <summary>
        /// Invert all bits </summary>
        public void InvertAll()
        {
            if (Count_Renamed != -1)
            {
                Count_Renamed = Size_Renamed - Count_Renamed;
            }
            if (Bits.Length > 0)
            {
                for (int idx = 0; idx < Bits.Length; idx++)
                {
                    Bits[idx] = (byte)(~Bits[idx]);
                }
                ClearUnusedBits();
            }
        }

        private void ClearUnusedBits()
        {
            // Take care not to invert the "unused" bits in the
            // last byte:
            if (Bits.Length > 0)
            {
                int lastNBits = Size_Renamed & 7;
                if (lastNBits != 0)
                {
                    int mask = (1 << lastNBits) - 1;
                    Bits[Bits.Length - 1] &= (byte)mask;
                }
            }
        }

        /// <summary>
        /// Set all bits </summary>
        public void SetAll()
        {
            Arrays.Fill(Bits, unchecked((byte)0xff));
            ClearUnusedBits();
            Count_Renamed = Size_Renamed;
        }

        /// <summary>
        /// Write as a bit set </summary>
        private void WriteBits(IndexOutput output)
        {
            output.WriteInt(Size()); // write size
            output.WriteInt(Count()); // write count
            output.WriteBytes(Bits, Bits.Length);
        }

        /// <summary>
        /// Write as a d-gaps list </summary>
        private void WriteClearedDgaps(IndexOutput output)
        {
            output.WriteInt(-1); // mark using d-gaps
            output.WriteInt(Size()); // write size
            output.WriteInt(Count()); // write count
            int last = 0;
            int numCleared = Size() - Count();
            for (int i = 0; i < Bits.Length && numCleared > 0; i++)
            {
                if (Bits[i] != unchecked((byte)0xff))
                {
                    output.WriteVInt(i - last);
                    output.WriteByte(Bits[i]);
                    last = i;
                    numCleared -= (8 - BitUtil.BitCount(Bits[i]));
                    Debug.Assert(numCleared >= 0 || (i == (Bits.Length - 1) && numCleared == -(8 - (Size_Renamed & 7))));
                }
            }
        }

        /// <summary>
        /// Indicates if the bit vector is sparse and should be saved as a d-gaps list, or dense, and should be saved as a bit set. </summary>
        private bool Sparse
        {
            get
            {
                int clearedCount = Size() - Count();
                if (clearedCount == 0)
                {
                    return true;
                }

                int avgGapLength = Bits.Length / clearedCount;

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
                return factor * expectedBits < Size();
            }
        }

        /// <summary>
        /// Constructs a bit vector from the file <code>name</code> in Directory
        ///  <code>d</code>, as written by the <seealso cref="#write"/> method.
        /// </summary>
        public BitVector(Directory d, string name, IOContext context)
        {
            ChecksumIndexInput input = d.OpenChecksumInput(name, context);

            try
            {
                int firstInt = input.ReadInt();

                if (firstInt == -2)
                {
                    // New format, with full header & version:
                    Version_Renamed = CodecUtil.CheckHeader(input, CODEC, VERSION_START, VERSION_CURRENT);
                    Size_Renamed = input.ReadInt();
                }
                else
                {
                    Version_Renamed = VERSION_PRE;
                    Size_Renamed = firstInt;
                }
                if (Size_Renamed == -1)
                {
                    if (Version_Renamed >= VERSION_DGAPS_CLEARED)
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

                if (Version_Renamed < VERSION_DGAPS_CLEARED)
                {
                    InvertAll();
                }

                if (Version_Renamed >= VERSION_CHECKSUM)
                {
                    CodecUtil.CheckFooter(input);
                }
                else
                {
                    CodecUtil.CheckEOF(input);
                }
                Debug.Assert(VerifyCount());
            }
            finally
            {
                input.Dispose();
            }
        }

        // asserts only
        private bool VerifyCount()
        {
            Debug.Assert(Count_Renamed != -1);
            int countSav = Count_Renamed;
            Count_Renamed = -1;
            Debug.Assert(countSav == Count(), "saved count was " + countSav + " but recomputed count is " + Count_Renamed);
            return true;
        }

        /// <summary>
        /// Read as a bit set </summary>
        private void ReadBits(IndexInput input)
        {
            Count_Renamed = input.ReadInt(); // read count
            Bits = new byte[GetNumBytes(Size_Renamed)]; // allocate bits
            input.ReadBytes(Bits, 0, Bits.Length);
        }

        /// <summary>
        /// read as a d-gaps list </summary>
        private void ReadSetDgaps(IndexInput input)
        {
            Size_Renamed = input.ReadInt(); // (re)read size
            Count_Renamed = input.ReadInt(); // read count
            Bits = new byte[GetNumBytes(Size_Renamed)]; // allocate bits
            int last = 0;
            int n = Count();
            while (n > 0)
            {
                last += input.ReadVInt();
                Bits[last] = input.ReadByte();
                n -= BitUtil.BitCount(Bits[last]);
                Debug.Assert(n >= 0);
            }
        }

        /// <summary>
        /// read as a d-gaps cleared bits list </summary>
        private void ReadClearedDgaps(IndexInput input)
        {
            Size_Renamed = input.ReadInt(); // (re)read size
            Count_Renamed = input.ReadInt(); // read count
            Bits = new byte[GetNumBytes(Size_Renamed)]; // allocate bits
            for (int i = 0; i < Bits.Length; ++i)
            {
                Bits[i] = 0xff;
            }
            ClearUnusedBits();
            int last = 0;
            int numCleared = Size() - Count();
            while (numCleared > 0)
            {
                last += input.ReadVInt();
                Bits[last] = input.ReadByte();
                numCleared -= 8 - BitUtil.BitCount(Bits[last]);
                Debug.Assert(numCleared >= 0 || (last == (Bits.Length - 1) && numCleared == -(8 - (Size_Renamed & 7))));
            }
        }
    }
}