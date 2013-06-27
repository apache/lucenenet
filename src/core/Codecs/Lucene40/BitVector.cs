using Lucene.Net.Store;
using Lucene.Net.Support;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Codecs.Lucene40
{
    internal sealed class BitVector : ICloneable, IMutableBits
    {
        private byte[] bits;
        private int size;
        private int count;
        private int version;

        /** Constructs a vector capable of holding <code>n</code> bits. */
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

        private int GetNumBytes(int size)
        {
            int bytesLength = Number.URShift(size, 3);
            if ((size & 7) != 0)
            {
                bytesLength++;
            }
            return bytesLength;
        }

        public object Clone()
        {
            byte[] copyBits = new byte[bits.Length];
            Array.Copy(bits, 0, copyBits, 0, bits.Length);
            BitVector clone = new BitVector(copyBits, size);
            clone.count = count;
            return clone;
        }

        public void Set(int bit)
        {
            if (bit >= size)
            {
                throw new IndexOutOfRangeException("bit=" + bit + " size=" + size);
            }
            bits[bit >> 3] |= (byte)(1 << (bit & 7));
            count = -1;
        }

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
                return true;
            else
            {
                bits[pos] = (byte)(v | flag);
                if (count != -1)
                {
                    count++;
                    //assert count <= size;
                }
                return false;
            }
        }

        public void Clear(int bit)
        {
            if (bit >= size)
            {
                throw new IndexOutOfRangeException(bit.ToString() + " is out of range");
            }
            bits[bit >> 3] &= (byte)~(1 << (bit & 7));
            count = -1;
        }

        public bool GetAndClear(int bit)
        {
            if (bit >= size)
            {
                throw new IndexOutOfRangeException(bit.ToString() + " is out of range");
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
                bits[pos] &= (byte)~flag;
                if (count != -1)
                {
                    count--;
                    //assert count >= 0;
                }
                return true;
            }
        }

        public bool this[int bit]
        {
            get
            {
                //assert bit >= 0 && bit < size: "bit " + bit + " is out of bounds 0.." + (size-1);
                return (bits[bit >> 3] & (1 << (bit & 7))) != 0;
            }
        }

        public int Size
        {
            get { return size; }
        }

        public int Length
        {
            get { return size; }
        }

        public int Count
        {
            get
            {
                // if the vector has been modified
                if (count == -1)
                {
                    int c = 0;
                    int end = bits.Length;
                    for (int i = 0; i < end; i++)
                    {
                        c += BYTE_COUNTS[bits[i] & 0xFF];  // sum bits per byte
                    }
                    count = c;
                }
                //assert count <= size: "count=" + count + " size=" + size;
                return count;
            }
        }

        public int RecomputedCount
        {
            get
            {
                int c = 0;
                int end = bits.Length;
                for (int i = 0; i < end; i++)
                {
                    c += BYTE_COUNTS[bits[i] & 0xFF];  // sum bits per byte
                }
                return c;
            }
        }

        private static readonly byte[] BYTE_COUNTS = {  // table of bits/byte
            0, 1, 1, 2, 1, 2, 2, 3, 1, 2, 2, 3, 2, 3, 3, 4,
            1, 2, 2, 3, 2, 3, 3, 4, 2, 3, 3, 4, 3, 4, 4, 5,
            1, 2, 2, 3, 2, 3, 3, 4, 2, 3, 3, 4, 3, 4, 4, 5,
            2, 3, 3, 4, 3, 4, 4, 5, 3, 4, 4, 5, 4, 5, 5, 6,
            1, 2, 2, 3, 2, 3, 3, 4, 2, 3, 3, 4, 3, 4, 4, 5,
            2, 3, 3, 4, 3, 4, 4, 5, 3, 4, 4, 5, 4, 5, 5, 6,
            2, 3, 3, 4, 3, 4, 4, 5, 3, 4, 4, 5, 4, 5, 5, 6,
            3, 4, 4, 5, 4, 5, 5, 6, 4, 5, 5, 6, 5, 6, 6, 7,
            1, 2, 2, 3, 2, 3, 3, 4, 2, 3, 3, 4, 3, 4, 4, 5,
            2, 3, 3, 4, 3, 4, 4, 5, 3, 4, 4, 5, 4, 5, 5, 6,
            2, 3, 3, 4, 3, 4, 4, 5, 3, 4, 4, 5, 4, 5, 5, 6,
            3, 4, 4, 5, 4, 5, 5, 6, 4, 5, 5, 6, 5, 6, 6, 7,
            2, 3, 3, 4, 3, 4, 4, 5, 3, 4, 4, 5, 4, 5, 5, 6,
            3, 4, 4, 5, 4, 5, 5, 6, 4, 5, 5, 6, 5, 6, 6, 7,
            3, 4, 4, 5, 4, 5, 5, 6, 4, 5, 5, 6, 5, 6, 6, 7,
            4, 5, 5, 6, 5, 6, 6, 7, 5, 6, 6, 7, 6, 7, 7, 8
          };

        private static string CODEC = "BitVector";

        // Version before version tracking was added:
        public const int VERSION_PRE = -1;

        // First version:
        public const int VERSION_START = 0;

        // Changed DGaps to encode gaps between cleared bits, not
        // set:
        public const int VERSION_DGAPS_CLEARED = 1;

        // Increment version to change it:
        public const int VERSION_CURRENT = VERSION_DGAPS_CLEARED;

        public int Version
        {
            get
            {
                return version;
            }
        }

        public void Write(Directory d, String name, IOContext context)
        {
            //assert !(d instanceof CompoundFileDirectory);
            IndexOutput output = d.CreateOutput(name, context);
            try
            {
                output.WriteInt(-2);
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
                //assert verifyCount();
            }
            finally
            {
                IOUtils.Close((IDisposable)output);
            }
        }

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

        public void SetAll()
        {
            Arrays.Fill(bits, (byte)0xff);
            ClearUnusedBits();
            count = size;
        }

        private void WriteBits(IndexOutput output)
        {
            output.WriteInt(Size);        // write size
            output.WriteInt(Count);       // write count
            output.WriteBytes(bits, bits.Length);
        }

        private void WriteClearedDgaps(IndexOutput output)
        {
            output.WriteInt(-1);            // mark using d-gaps                         
            output.WriteInt(Size);        // write size
            output.WriteInt(Count);       // write count
            int last = 0;
            int numCleared = Size - Count;
            for (int i = 0; i < bits.Length && numCleared > 0; i++)
            {
                if (bits[i] != (byte)0xff)
                {
                    output.WriteVInt(i - last);
                    output.WriteByte(bits[i]);
                    last = i;
                    numCleared -= (8 - BYTE_COUNTS[bits[i] & 0xFF]);
                    //assert numCleared >= 0 || (i == (bits.length-1) && numCleared == -(8-(size&7)));
                }
            }
        }

        private bool IsSparse
        {
            get
            {
                int clearedCount = Size - Count;
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
                long factor = 10;
                return factor * expectedBits < Size;
            }
        }

        public BitVector(Directory d, String name, IOContext context)
        {
            IndexInput input = d.OpenInput(name, context);

            try
            {
                int firstInt = input.ReadInt();

                if (firstInt == -2)
                {
                    // New format, with full header & version:
                    version = CodecUtil.CheckHeader(input, CODEC, VERSION_START, VERSION_CURRENT);
                    size = input.ReadInt();
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

                //assert verifyCount();
            }
            finally
            {
                input.Dispose();
            }
        }

        // asserts only
        private bool VerifyCount()
        {
            //assert count != -1;
            int countSav = count;
            count = -1;
            //assert countSav == count(): "saved count was " + countSav + " but recomputed count is " + count;
            return true;
        }

        /** Read as a bit set */
        private void ReadBits(IndexInput input)
        {
            count = input.ReadInt();        // read count
            bits = new byte[GetNumBytes(size)];     // allocate bits
            input.ReadBytes(bits, 0, bits.Length);
        }

        private void ReadSetDgaps(IndexInput input)
        {
            size = input.ReadInt();       // (re)read size
            count = input.ReadInt();        // read count
            bits = new byte[GetNumBytes(size)];     // allocate bits
            int last = 0;
            int n = Count;
            while (n > 0)
            {
                last += input.ReadVInt();
                bits[last] = input.ReadByte();
                n -= BYTE_COUNTS[bits[last] & 0xFF];
                //assert n >= 0;
            }
        }

        private void ReadClearedDgaps(IndexInput input)
        {
            size = input.ReadInt();       // (re)read size
            count = input.ReadInt();        // read count
            bits = new byte[GetNumBytes(size)];     // allocate bits
            Arrays.Fill(bits, (byte)0xff);
            ClearUnusedBits();
            int last = 0;
            int numCleared = Size - Count;
            while (numCleared > 0)
            {
                last += input.ReadVInt();
                bits[last] = input.ReadByte();
                numCleared -= 8 - BYTE_COUNTS[bits[last] & 0xFF];
                //assert numCleared >= 0 || (last == (bits.length-1) && numCleared == -(8-(size&7)));
            }
        }
    }
}
