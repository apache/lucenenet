using J2N.Numerics;
using Lucene.Net.Diagnostics;
using Lucene.Net.Support;
using System;
using System.Runtime.CompilerServices;

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
    /// BitSet of fixed length (<see cref="numBits"/>), backed by accessible (<see cref="GetBits()"/>)
    /// <see cref="T:long[]"/>, accessed with a <see cref="long"/> index. Use it only if you intend to store more
    /// than 2.1B bits, otherwise you should use <see cref="FixedBitSet"/>.
    /// <para/>
    /// NOTE: This was LongBitSet in Lucene
    /// <para/>
    /// @lucene.internal
    /// </summary>
#if FEATURE_SERIALIZABLE
    [Serializable]
#endif
    public sealed class Int64BitSet
    {
        internal readonly long[] bits; // LUCENENET: Internal for testing
        private readonly long numBits;
        internal readonly int numWords; // LUCENENET: Internal for testing

        /// <summary>
        /// If the given <see cref="Int64BitSet"/> is large enough to hold
        /// <paramref name="numBits"/>, returns the given <paramref name="bits"/>, otherwise returns a new
        /// <see cref="Int64BitSet"/> which can hold the requested number of bits.
        ///
        /// <para/>
        /// <b>NOTE:</b> the returned bitset reuses the underlying <see cref="T:long[]"/> of
        /// the given <paramref name="bits"/> if possible. Also, reading <see cref="Length"/> on the
        /// returned bits may return a value greater than <paramref name="numBits"/>.
        /// </summary>
        public static Int64BitSet EnsureCapacity(Int64BitSet bits, long numBits)
        {
            if (numBits < bits.Length)
            {
                return bits;
            }
            else
            {
                int numWords = Bits2words(numBits);
                long[] arr = bits.GetBits();
                if (numWords >= arr.Length)
                {
                    arr = ArrayUtil.Grow(arr, numWords + 1);
                }
                return new Int64BitSet(arr, arr.Length << 6);
            }
        }

        /// <summary>
        /// Returns the number of 64 bit words it would take to hold <paramref name="numBits"/>. </summary>
        public static int Bits2words(long numBits)
        {
            int numLong = (int)numBits.TripleShift(6);
            if ((numBits & 63) != 0)
            {
                numLong++;
            }
            return numLong;
        }

        public Int64BitSet(long numBits)
        {
            this.numBits = numBits;
            bits = new long[Bits2words(numBits)];
            numWords = bits.Length;
        }

        public Int64BitSet(long[] storedBits, long numBits)
        {
            this.numWords = Bits2words(numBits);
            if (numWords > storedBits.Length)
            {
                throw new ArgumentException("The given long array is too small  to hold " + numBits + " bits");
            }
            this.numBits = numBits;
            this.bits = storedBits;
        }

        /// <summary>
        /// Returns the number of bits stored in this bitset. </summary>
        public long Length => numBits;

        /// <summary>
        /// Expert. </summary>
        [WritableArray]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public long[] GetBits()
        {
            return bits;
        }

        /// <summary>
        /// Gets the number of set bits.  NOTE: this visits every
        /// long in the backing bits array, and the result is not
        /// internally cached!
        /// </summary>
        public long Cardinality
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => BitUtil.Pop_Array(bits, 0, bits.Length);
        }

        public bool Get(long index)
        {
            if (Debugging.AssertsEnabled) Debugging.Assert(index >= 0 && index < numBits, "index={0}", index);
            int i = (int)(index >> 6); // div 64
            // signed shift will keep a negative index and force an
            // array-index-out-of-bounds-exception, removing the need for an explicit check.
            int bit = (int)(index & 0x3f); // mod 64
            long bitmask = 1L << bit;
            return (bits[i] & bitmask) != 0;
        }

        public void Set(long index)
        {
            if (Debugging.AssertsEnabled) Debugging.Assert(index >= 0 && index < numBits, "index={0} numBits={1}", index, numBits);
            int wordNum = (int)(index >> 6); // div 64
            int bit = (int)(index & 0x3f); // mod 64
            long bitmask = 1L << bit;
            bits[wordNum] |= bitmask;
        }

        public bool GetAndSet(long index)
        {
            if (Debugging.AssertsEnabled) Debugging.Assert(index >= 0 && index < numBits);
            int wordNum = (int)(index >> 6); // div 64
            int bit = (int)(index & 0x3f); // mod 64
            long bitmask = 1L << bit;
            bool val = (bits[wordNum] & bitmask) != 0;
            bits[wordNum] |= bitmask;
            return val;
        }

        public void Clear(long index)
        {
            if (Debugging.AssertsEnabled) Debugging.Assert(index >= 0 && index < numBits);
            int wordNum = (int)(index >> 6);
            int bit = (int)(index & 0x03f);
            long bitmask = 1L << bit;
            bits[wordNum] &= ~bitmask;
        }

        public bool GetAndClear(long index)
        {
            if (Debugging.AssertsEnabled) Debugging.Assert(index >= 0 && index < numBits);
            int wordNum = (int)(index >> 6); // div 64
            int bit = (int)(index & 0x3f); // mod 64
            long bitmask = 1L << bit;
            bool val = (bits[wordNum] & bitmask) != 0;
            bits[wordNum] &= ~bitmask;
            return val;
        }

        /// <summary>
        /// Returns the index of the first set bit starting at the <paramref name="index"/> specified.
        /// -1 is returned if there are no more set bits.
        /// </summary>
        public long NextSetBit(long index)
        {
            if (Debugging.AssertsEnabled) Debugging.Assert(index >= 0 && index < numBits);
            int i = (int)(index >> 6);
            int subIndex = (int)(index & 0x3f); // index within the word
            long word = bits[i] >> subIndex; // skip all the bits to the right of index

            if (word != 0)
            {
                return index + word.TrailingZeroCount();
            }

            while (++i < numWords)
            {
                word = bits[i];
                if (word != 0)
                {
                    return (i << 6) + word.TrailingZeroCount();
                }
            }

            return -1;
        }

        /// <summary>
        /// Returns the index of the last set bit before or on the <paramref name="index"/> specified.
        /// -1 is returned if there are no more set bits.
        /// </summary>
        public long PrevSetBit(long index)
        {
            if (Debugging.AssertsEnabled) Debugging.Assert(index >= 0 && index < numBits, "index={0} numBits={1}", index, numBits);
            int i = (int)(index >> 6);
            int subIndex = (int)(index & 0x3f); // index within the word
            long word = (bits[i] << (63 - subIndex)); // skip all the bits to the left of index

            if (word != 0)
            {
                return (i << 6) + subIndex - word.LeadingZeroCount(); // See LUCENE-3197
            }

            while (--i >= 0)
            {
                word = bits[i];
                if (word != 0)
                {
                    return (i << 6) + 63 - word.LeadingZeroCount();
                }
            }

            return -1;
        }

        /// <summary>
        /// this = this OR other </summary>
        public void Or(Int64BitSet other)
        {
            if (Debugging.AssertsEnabled) Debugging.Assert(other.numWords <= numWords, "numWords={0}, other.numWords={1}", numWords, other.numWords);
            int pos = Math.Min(numWords, other.numWords);
            while (--pos >= 0)
            {
                bits[pos] |= other.bits[pos];
            }
        }

        /// <summary>
        /// this = this XOR other </summary>
        public void Xor(Int64BitSet other)
        {
            if (Debugging.AssertsEnabled) Debugging.Assert(other.numWords <= numWords, "numWords={0}, other.numWords={1}", numWords, other.numWords);
            int pos = Math.Min(numWords, other.numWords);
            while (--pos >= 0)
            {
                bits[pos] ^= other.bits[pos];
            }
        }

        /// <summary>
        /// Returns <c>true</c> if the sets have any elements in common </summary>
        public bool Intersects(Int64BitSet other)
        {
            int pos = Math.Min(numWords, other.numWords);
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
        public void And(Int64BitSet other)
        {
            int pos = Math.Min(numWords, other.numWords);
            while (--pos >= 0)
            {
                bits[pos] &= other.bits[pos];
            }
            if (numWords > other.numWords)
            {
                Arrays.Fill(bits, other.numWords, numWords, 0L);
            }
        }

        /// <summary>
        /// this = this AND NOT other </summary>
        public void AndNot(Int64BitSet other)
        {
            int pos = Math.Min(numWords, other.bits.Length);
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
        /// <param name="startIndex"> Lower index </param>
        /// <param name="endIndex"> One-past the last bit to flip </param>
        public void Flip(long startIndex, long endIndex)
        {
            if (Debugging.AssertsEnabled)
            {
                Debugging.Assert(startIndex >= 0 && startIndex < numBits);
                Debugging.Assert(endIndex >= 0 && endIndex <= numBits);
            }
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

            long startmask = -1L << (int)startIndex;
            long endmask = (-1L).TripleShift((int)-endIndex); // 64-(endIndex&0x3f) is the same as -endIndex due to wrap

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
        /// <param name="startIndex"> Lower index </param>
        /// <param name="endIndex"> One-past the last bit to set </param>
        public void Set(long startIndex, long endIndex)
        {
            if (Debugging.AssertsEnabled)
            {
                Debugging.Assert(startIndex >= 0 && startIndex < numBits);
                Debugging.Assert(endIndex >= 0 && endIndex <= numBits);
            }
            if (endIndex <= startIndex)
            {
                return;
            }

            int startWord = (int)(startIndex >> 6);
            int endWord = (int)((endIndex - 1) >> 6);

            long startmask = -1L << (int)startIndex;
            long endmask = (-1L).TripleShift((int)-endIndex); // 64-(endIndex&0x3f) is the same as -endIndex due to wrap

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
        /// <param name="startIndex"> Lower index </param>
        /// <param name="endIndex"> One-past the last bit to clear </param>
        public void Clear(long startIndex, long endIndex)
        {
            if (Debugging.AssertsEnabled)
            {
                Debugging.Assert(startIndex >= 0 && startIndex < numBits);
                Debugging.Assert(endIndex >= 0 && endIndex <= numBits);
            }
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Int64BitSet Clone()
        {
            long[] bits = new long[this.bits.Length];
            Arrays.Copy(this.bits, 0, bits, 0, bits.Length);
            return new Int64BitSet(bits, numBits);
        }

        /// <summary>
        /// Returns <c>true</c> if both sets have the same bits set </summary>
        public override bool Equals(object o)
        {
            if (this == o)
            {
                return true;
            }
            if (!(o is Int64BitSet))
            {
                return false;
            }
            Int64BitSet other = (Int64BitSet)o;
            if (numBits != other.Length)
            {
                return false;
            }
            return Arrays.Equals(bits, other.bits);
        }

        public override int GetHashCode()
        {
            long h = 0;
            for (int i = numWords; --i >= 0; )
            {
                h ^= bits[i];
                h = (h << 1) | (h.TripleShift(63)); // rotate left
            }
            // fold leftmost bits into right and add a constant to prevent
            // empty sets from returning 0, which is too common.
            return (int)((h >> 32) ^ h) + unchecked((int)0x98761234);
        }
    }
}