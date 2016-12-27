using Lucene.Net.Support;
using System;
using System.Diagnostics;

namespace Lucene.Net.Util
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
    /// BitSet of fixed length (numBits), backed by accessible (<seealso cref="#getBits"/>)
    /// long[], accessed with a long index. Use it only if you intend to store more
    /// than 2.1B bits, otherwise you should use <seealso cref="FixedBitSet"/>.
    ///
    /// @lucene.internal
    /// </summary>
    public sealed class LongBitSet // LUCENENET TODO: Rename Int64BitSet ?
    {
        private readonly long[] bits;
        private readonly long NumBits;
        private readonly int NumWords;

        /// <summary>
        /// If the given <seealso cref="LongBitSet"/> is large enough to hold
        /// {@code numBits}, returns the given bits, otherwise returns a new
        /// <seealso cref="LongBitSet"/> which can hold the requested number of bits.
        ///
        /// <p>
        /// <b>NOTE:</b> the returned bitset reuses the underlying {@code long[]} of
        /// the given {@code bits} if possible. Also, calling <seealso cref="#length()"/> on the
        /// returned bits may return a value greater than {@code numBits}.
        /// </summary>
        public static LongBitSet EnsureCapacity(LongBitSet bits, long numBits)
        {
            if (numBits < bits.Length())
            {
                return bits;
            }
            else
            {
                int numWords = Bits2words(numBits);
                long[] arr = bits.Bits;
                if (numWords >= arr.Length)
                {
                    arr = ArrayUtil.Grow(arr, numWords + 1);
                }
                return new LongBitSet(arr, arr.Length << 6);
            }
        }

        /// <summary>
        /// returns the number of 64 bit words it would take to hold numBits </summary>
        public static int Bits2words(long numBits)
        {
            int numLong = (int)((long)((ulong)numBits >> 6));
            if ((numBits & 63) != 0)
            {
                numLong++;
            }
            return numLong;
        }

        public LongBitSet(long numBits)
        {
            this.NumBits = numBits;
            bits = new long[Bits2words(numBits)];
            NumWords = bits.Length;
        }

        public LongBitSet(long[] storedBits, long numBits)
        {
            this.NumWords = Bits2words(numBits);
            if (NumWords > storedBits.Length)
            {
                throw new System.ArgumentException("The given long array is too small  to hold " + numBits + " bits");
            }
            this.NumBits = numBits;
            this.bits = storedBits;
        }

        /// <summary>
        /// Returns the number of bits stored in this bitset. </summary>
        public long Length() // LUCENENET TODO: Make property
        {
            return NumBits;
        }

        /// <summary>
        /// Expert. </summary>
        public long[] Bits // LUCENENET TODO: Change to GetBits() (array)
        {
            get
            {
                return bits;
            }
        }

        /// <summary>
        /// Returns number of set bits.  NOTE: this visits every
        ///  long in the backing bits array, and the result is not
        ///  internally cached!
        /// </summary>
        public long Cardinality()
        {
            return BitUtil.Pop_Array(bits, 0, bits.Length);
        }

        public bool Get(long index)
        {
            Debug.Assert(index >= 0 && index < NumBits, "index=" + index);
            int i = (int)(index >> 6); // div 64
            // signed shift will keep a negative index and force an
            // array-index-out-of-bounds-exception, removing the need for an explicit check.
            int bit = (int)(index & 0x3f); // mod 64
            long bitmask = 1L << bit;
            return (bits[i] & bitmask) != 0;
        }

        public void Set(long index)
        {
            Debug.Assert(index >= 0 && index < NumBits, "index=" + index + " numBits=" + NumBits);
            int wordNum = (int)(index >> 6); // div 64
            int bit = (int)(index & 0x3f); // mod 64
            long bitmask = 1L << bit;
            bits[wordNum] |= bitmask;
        }

        public bool GetAndSet(long index)
        {
            Debug.Assert(index >= 0 && index < NumBits);
            int wordNum = (int)(index >> 6); // div 64
            int bit = (int)(index & 0x3f); // mod 64
            long bitmask = 1L << bit;
            bool val = (bits[wordNum] & bitmask) != 0;
            bits[wordNum] |= bitmask;
            return val;
        }

        public void Clear(long index)
        {
            Debug.Assert(index >= 0 && index < NumBits);
            int wordNum = (int)(index >> 6);
            int bit = (int)(index & 0x03f);
            long bitmask = 1L << bit;
            bits[wordNum] &= ~bitmask;
        }

        public bool GetAndClear(long index)
        {
            Debug.Assert(index >= 0 && index < NumBits);
            int wordNum = (int)(index >> 6); // div 64
            int bit = (int)(index & 0x3f); // mod 64
            long bitmask = 1L << bit;
            bool val = (bits[wordNum] & bitmask) != 0;
            bits[wordNum] &= ~bitmask;
            return val;
        }

        /// <summary>
        /// Returns the index of the first set bit starting at the index specified.
        ///  -1 is returned if there are no more set bits.
        /// </summary>
        public long NextSetBit(long index)
        {
            Debug.Assert(index >= 0 && index < NumBits);
            int i = (int)(index >> 6);
            int subIndex = (int)(index & 0x3f); // index within the word
            long word = bits[i] >> subIndex; // skip all the bits to the right of index

            if (word != 0)
            {
                return index + Number.NumberOfTrailingZeros(word);
            }

            while (++i < NumWords)
            {
                word = bits[i];
                if (word != 0)
                {
                    return (i << 6) + Number.NumberOfTrailingZeros(word);
                }
            }

            return -1;
        }

        /// <summary>
        /// Returns the index of the last set bit before or on the index specified.
        ///  -1 is returned if there are no more set bits.
        /// </summary>
        public long PrevSetBit(long index)
        {
            Debug.Assert(index >= 0 && index < NumBits, "index=" + index + " numBits=" + NumBits);
            int i = (int)(index >> 6);
            int subIndex = (int)(index & 0x3f); // index within the word
            long word = (bits[i] << (63 - subIndex)); // skip all the bits to the left of index

            if (word != 0)
            {
                return (i << 6) + subIndex - Number.NumberOfLeadingZeros(word); // See LUCENE-3197
            }

            while (--i >= 0)
            {
                word = bits[i];
                if (word != 0)
                {
                    return (i << 6) + 63 - Number.NumberOfLeadingZeros(word);
                }
            }

            return -1;
        }

        /// <summary>
        /// this = this OR other </summary>
        public void Or(LongBitSet other)
        {
            Debug.Assert(other.NumWords <= NumWords, "numWords=" + NumWords + ", other.numWords=" + other.NumWords);
            int pos = Math.Min(NumWords, other.NumWords);
            while (--pos >= 0)
            {
                bits[pos] |= other.bits[pos];
            }
        }

        /// <summary>
        /// this = this XOR other </summary>
        public void Xor(LongBitSet other)
        {
            Debug.Assert(other.NumWords <= NumWords, "numWords=" + NumWords + ", other.numWords=" + other.NumWords);
            int pos = Math.Min(NumWords, other.NumWords);
            while (--pos >= 0)
            {
                bits[pos] ^= other.bits[pos];
            }
        }

        /// <summary>
        /// returns true if the sets have any elements in common </summary>
        public bool Intersects(LongBitSet other)
        {
            int pos = Math.Min(NumWords, other.NumWords);
            while (--pos >= 0)
            {
                if ((bits[pos] & other.bits[pos]) != 0)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// this = this AND other </summary>
        public void And(LongBitSet other)
        {
            int pos = Math.Min(NumWords, other.NumWords);
            while (--pos >= 0)
            {
                bits[pos] &= other.bits[pos];
            }
            if (NumWords > other.NumWords)
            {
                Arrays.Fill(bits, other.NumWords, NumWords, 0L);
            }
        }

        /// <summary>
        /// this = this AND NOT other </summary>
        public void AndNot(LongBitSet other)
        {
            int pos = Math.Min(NumWords, other.bits.Length);
            while (--pos >= 0)
            {
                bits[pos] &= ~other.bits[pos];
            }
        }

        // NOTE: no .isEmpty() here because that's trappy (ie,
        // typically isEmpty is low cost, but this one wouldn't
        // be)

        /// <summary>
        /// Flips a range of bits
        /// </summary>
        /// <param name="startIndex"> lower index </param>
        /// <param name="endIndex"> one-past the last bit to flip </param>
        public void Flip(long startIndex, long endIndex)
        {
            Debug.Assert(startIndex >= 0 && startIndex < NumBits);
            Debug.Assert(endIndex >= 0 && endIndex <= NumBits);
            if (endIndex <= startIndex)
            {
                return;
            }

            int startWord = (int)(startIndex >> 6);
            int endWord = (int)((endIndex - 1) >> 6);

            /*
            ///* Grrr, java shifting wraps around so -1L>>>64 == -1
            /// for that reason, make sure not to use endmask if the bits to flip will
            /// be zero in the last word (redefine endWord to be the last changed...)
            /// long startmask = -1L << (startIndex & 0x3f);     // example: 11111...111000
            /// long endmask = -1L >>> (64-(endIndex & 0x3f));   // example: 00111...111111
            /// **
            */

            //LUCENE TO-DO
            long startmask = -1L << (int)startIndex;
            long endmask = (long)(unchecked(((ulong)-1L)) >> (int)-endIndex); // 64-(endIndex&0x3f) is the same as -endIndex due to wrap

            if (startWord == endWord)
            {
                bits[startWord] ^= (startmask & endmask);
                return;
            }

            bits[startWord] ^= startmask;

            for (int i = startWord + 1; i < endWord; i++)
            {
                bits[i] = ~bits[i];
            }

            bits[endWord] ^= endmask;
        }

        /// <summary>
        /// Sets a range of bits
        /// </summary>
        /// <param name="startIndex"> lower index </param>
        /// <param name="endIndex"> one-past the last bit to set </param>
        public void Set(long startIndex, long endIndex)
        {
            Debug.Assert(startIndex >= 0 && startIndex < NumBits);
            Debug.Assert(endIndex >= 0 && endIndex <= NumBits);
            if (endIndex <= startIndex)
            {
                return;
            }

            int startWord = (int)(startIndex >> 6);
            int endWord = (int)((endIndex - 1) >> 6);

            //LUCENE TO-DO
            long startmask = -1L << (int)startIndex;
            long endmask = (long)(0xffffffffffffffffUL >> (int)-endIndex);//-(int)((uint)1L >> (int)-endIndex); // 64-(endIndex&0x3f) is the same as -endIndex due to wrap

            if (startWord == endWord)
            {
                bits[startWord] |= (startmask & endmask);
                return;
            }

            bits[startWord] |= startmask;
            Arrays.Fill(bits, startWord + 1, endWord, -1L);
            bits[endWord] |= endmask;
        }

        /// <summary>
        /// Clears a range of bits.
        /// </summary>
        /// <param name="startIndex"> lower index </param>
        /// <param name="endIndex"> one-past the last bit to clear </param>
        public void Clear(long startIndex, long endIndex)
        {
            Debug.Assert(startIndex >= 0 && startIndex < NumBits);
            Debug.Assert(endIndex >= 0 && endIndex <= NumBits);
            if (endIndex <= startIndex)
            {
                return;
            }

            int startWord = (int)(startIndex >> 6);
            int endWord = (int)((endIndex - 1) >> 6);

            // Casting long to int discards MSBs, so it is no problem because we are taking mod 64.
            long startmask = (-1L) << (int)startIndex;  // -1 << (startIndex mod 64)
            long endmask = (-1L) << (int)endIndex;            // -1 << (endIndex mod 64)
            if ((endIndex & 0x3f) == 0)
            {
                endmask = 0;
            }

            startmask = ~startmask;

            if (startWord == endWord)
            {
                bits[startWord] &= (startmask | endmask);
                return;
            }

            bits[startWord] &= startmask;
            Arrays.Fill(bits, startWord + 1, endWord, 0L);
            bits[endWord] &= endmask;
        }

        public LongBitSet Clone()
        {
            long[] bits = new long[this.bits.Length];
            Array.Copy(this.bits, 0, bits, 0, bits.Length);
            return new LongBitSet(bits, NumBits);
        }

        /// <summary>
        /// returns true if both sets have the same bits set </summary>
        public override bool Equals(object o)
        {
            if (this == o)
            {
                return true;
            }
            if (!(o is LongBitSet))
            {
                return false;
            }
            LongBitSet other = (LongBitSet)o;
            if (NumBits != other.Length())
            {
                return false;
            }
            return Arrays.Equals(bits, other.bits);
        }

        public override int GetHashCode()
        {
            long h = 0;
            for (int i = NumWords; --i >= 0; )
            {
                h ^= bits[i];
                h = (h << 1) | ((long)((ulong)h >> 63)); // rotate left
            }
            // fold leftmost bits into right and add a constant to prevent
            // empty sets from returning 0, which is too common.
            return (int)((h >> 32) ^ h) + unchecked((int)0x98761234);
        }
    }
}