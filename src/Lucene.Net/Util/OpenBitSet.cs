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

    using DocIdSet = Lucene.Net.Search.DocIdSet;
    using DocIdSetIterator = Lucene.Net.Search.DocIdSetIterator;

    /// <summary>
    /// An "open" BitSet implementation that allows direct access to the array of words
    /// storing the bits.
    /// <para/>
    /// NOTE: This can be used in .NET any place where a <c>java.util.BitSet</c> is used in Java.
    /// <para/>
    /// Unlike <c>java.util.BitSet</c>, the fact that bits are packed into an array of longs
    /// is part of the interface.  This allows efficient implementation of other algorithms
    /// by someone other than the author.  It also allows one to efficiently implement
    /// alternate serialization or interchange formats.
    /// <para/>
    /// <see cref="OpenBitSet"/> is faster than <c>java.util.BitSet</c> in most operations
    /// and *much* faster at calculating cardinality of sets and results of set operations.
    /// It can also handle sets of larger cardinality (up to 64 * 2**32-1)
    /// <para/>
    /// The goals of <see cref="OpenBitSet"/> are the fastest implementation possible, and
    /// maximum code reuse.  Extra safety and encapsulation
    /// may always be built on top, but if that's built in, the cost can never be removed (and
    /// hence people re-implement their own version in order to get better performance).
    /// <para/>
    /// <h3>Performance Results</h3>
    ///
    /// Test system: Pentium 4, Sun Java 1.5_06 -server -Xbatch -Xmx64M
    /// <para/>BitSet size = 1,000,000
    /// <para/>Results are java.util.BitSet time divided by OpenBitSet time.
    /// <list type="table">
    ///     <listheader>
    ///         <term></term> <term>cardinality</term> <term>IntersectionCount</term> <term>Union</term> <term>NextSetBit</term> <term>Get</term> <term>GetIterator</term>
    ///     </listheader>
    ///     <item>
    ///         <term>50% full</term> <description>3.36</description> <description>3.96</description> <description>1.44</description> <description>1.46</description> <description>1.99</description> <description>1.58</description>
    ///     </item>
    ///     <item>
    ///         <term>1% full</term> <description>3.31</description> <description>3.90</description> <description>&#160;</description> <description>1.04</description> <description>&#160;</description> <description>0.99</description>
    ///     </item>
    /// </list>
    /// <para/>
    /// <para/>
    /// Test system: AMD Opteron, 64 bit linux, Sun Java 1.5_06 -server -Xbatch -Xmx64M
    /// <para/>BitSet size = 1,000,000
    /// <para/>Results are java.util.BitSet time divided by OpenBitSet time.
    /// <list type="table">
    ///     <listheader>
    ///         <term></term> <term>cardinality</term> <term>IntersectionCount</term> <term>Union</term> <term>NextSetBit</term> <term>Get</term> <term>GetIterator</term>
    ///     </listheader>
    ///     <item>
    ///         <term>50% full</term> <description>2.50</description> <description>3.50</description> <description>1.00</description> <description>1.03</description> <description>1.12</description> <description>1.25</description>
    ///     </item>
    ///     <item>
    ///         <term>1% full</term> <description>2.51</description> <description>3.49</description> <description>&#160;</description> <description>1.00</description> <description>&#160;</description> <description>1.02</description>
    ///     </item>
    /// </list>
    /// </summary>
    public class OpenBitSet : DocIdSet, IBits // LUCENENET specific: Not implementing ICloneable per Microsoft's recommendation
    {
        protected internal long[] m_bits;
        protected internal int m_wlen; // number of words (elements) used in the array

        // Used only for assert:
        private long numBits;

        /// <summary>
        /// Constructs an <see cref="OpenBitSet"/> large enough to hold <paramref name="numBits"/>. </summary>
        public OpenBitSet(long numBits)
        {
            this.numBits = numBits;
            m_bits = new long[Bits2words(numBits)];
            m_wlen = m_bits.Length;
        }

        /// <summary>
        /// Constructor: allocates enough space for 64 bits. </summary>
        public OpenBitSet()
            : this(64)
        {
        }

        /// <summary>
        /// Constructs an <see cref="OpenBitSet"/> from an existing <see cref="T:long[]"/>.
        /// <para/>
        /// The first 64 bits are in long[0], with bit index 0 at the least significant
        /// bit, and bit index 63 at the most significant. Given a bit index, the word
        /// containing it is long[index/64], and it is at bit number index%64 within
        /// that word.
        /// <para/>
        /// <paramref name="numWords"/> are the number of elements in the array that contain set bits
        /// (non-zero longs). <paramref name="numWords"/> should be &lt;= bits.Length, and any existing
        /// words in the array at position &gt;= numWords should be zero.
        /// </summary>
        public OpenBitSet(long[] bits, int numWords)
        {
            if (numWords > bits.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(numWords), "numWords cannot exceed bits.Length"); // LUCENENET specific - changed from IllegalArgumentException to ArgumentOutOfRangeException (.NET convention)
            }
            this.m_bits = bits;
            this.m_wlen = numWords;
            this.numBits = m_wlen * 64;
        }

        public override DocIdSetIterator GetIterator()
        {
            return new OpenBitSetIterator(m_bits, m_wlen);
        }

        public override IBits Bits => this;

        /// <summary>
        /// This DocIdSet implementation is cacheable. </summary>
        public override bool IsCacheable => true;

        /// <summary>
        /// Returns the current capacity in bits (1 greater than the index of the last bit). </summary>
        public virtual long Capacity => m_bits.Length << 6;

        // LUCENENET specific - eliminating this extra property, since it is identical to
        // Length anyway, and Length is required by the IBits interface.
        ///// <summary>
        ///// Returns the current capacity of this set.  Included for
        ///// compatibility.  this is *not* equal to <seealso cref="#cardinality"/>.
        ///// </summary>
        //public virtual long Size
        //{
        //    get { return Capacity; }
        //}

        /// <summary>
        /// Returns the current capacity of this set. This is *not* equal to <see cref="Cardinality"/>.
        /// <para/>
        /// NOTE: This is equivalent to size() or length() in Lucene.
        /// </summary>
        public virtual int Length => m_bits.Length << 6;

        int IBits.Length => Length;

        /// <summary>
        /// Returns <c>true</c> if there are no set bits </summary>
        public virtual bool IsEmpty => Cardinality == 0;

        /// <summary>
        /// Expert: returns the <see cref="T:long[]"/> storing the bits. </summary>
        [WritableArray]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual long[] GetBits()
        {
            return m_bits;
        }

        /// <summary>
        /// Expert: gets the number of <see cref="long"/>s in the array that are in use. </summary>
        public virtual int NumWords => m_wlen;

        /// <summary>
        /// Returns <c>true</c> or <c>false</c> for the specified bit <paramref name="index"/>. </summary>
        public virtual bool Get(int index)
        {
            int i = index >> 6; // div 64
            // signed shift will keep a negative index and force an
            // array-index-out-of-bounds-exception, removing the need for an explicit check.
            if (i >= m_bits.Length)
            {
                return false;
            }

            int bit = index & 0x3f; // mod 64
            long bitmask = 1L << bit;
            return (m_bits[i] & bitmask) != 0;
        }

        /// <summary>
        /// Returns <c>true</c> or <c>false</c> for the specified bit <paramref name="index"/>.
        /// The index should be less than the <see cref="Length"/>.
        /// </summary>
        public virtual bool FastGet(int index)
        {
            if (Debugging.AssertsEnabled) Debugging.Assert(index >= 0 && index < numBits);
            int i = index >> 6; // div 64
            // signed shift will keep a negative index and force an
            // array-index-out-of-bounds-exception, removing the need for an explicit check.
            int bit = index & 0x3f; // mod 64
            long bitmask = 1L << bit;
            return (m_bits[i] & bitmask) != 0;
        }

        /// <summary>
        /// Returns <c>true</c> or <c>false</c> for the specified bit <paramref name="index"/>.
        /// </summary>
        public virtual bool Get(long index)
        {
            int i = (int)(index >> 6); // div 64
            if (i >= m_bits.Length)
            {
                return false;
            }
            int bit = (int)index & 0x3f; // mod 64
            long bitmask = 1L << bit;
            return (m_bits[i] & bitmask) != 0;
        }

        /// <summary>
        /// Returns <c>true</c> or <c>false</c> for the specified bit <paramref name="index"/>.
        /// The index should be less than the <see cref="Length"/>.
        /// </summary>
        public virtual bool FastGet(long index)
        {
            if (Debugging.AssertsEnabled) Debugging.Assert(index >= 0 && index < numBits);
            int i = (int)(index >> 6); // div 64
            int bit = (int)index & 0x3f; // mod 64
            long bitmask = 1L << bit;
            return (m_bits[i] & bitmask) != 0;
        }

        /*
        // alternate implementation of get()
        public boolean get1(int index) {
          int i = index >> 6;                // div 64
          int bit = index & 0x3f;            // mod 64
          return ((bits[i]>>>bit) & 0x01) != 0;
          // this does a long shift and a bittest (on x86) vs
          // a long shift, and a long AND, (the test for zero is prob a no-op)
          // testing on a P4 indicates this is slower than (bits[i] & bitmask) != 0;
        }
        */

        /// <summary>
        /// Returns 1 if the bit is set, 0 if not.
        /// The <paramref name="index"/> should be less than the <see cref="Length"/>.
        /// </summary>
        public virtual int GetBit(int index)
        {
            if (Debugging.AssertsEnabled) Debugging.Assert(index >= 0 && index < numBits);
            int i = index >> 6; // div 64
            int bit = index & 0x3f; // mod 64
            return ((int)m_bits[i].TripleShift(bit)) & 0x01;
        }

        /*
        public boolean get2(int index) {
          int word = index >> 6;            // div 64
          int bit = index & 0x0000003f;     // mod 64
          return (bits[word] << bit) < 0;   // hmmm, this would work if bit order were reversed
          // we could right shift and check for parity bit, if it was available to us.
        }
        */

        /// <summary>
        /// Sets a bit, expanding the set size if necessary. </summary>
        public virtual void Set(long index)
        {
            int wordNum = ExpandingWordNum(index);
            int bit = (int)index & 0x3f;
            long bitmask = 1L << bit;
            m_bits[wordNum] |= bitmask;
        }

        /// <summary>
        /// Sets the bit at the specified <paramref name="index"/>.
        /// The <paramref name="index"/> should be less than the <see cref="Length"/>.
        /// </summary>
        public virtual void FastSet(int index)
        {
            if (Debugging.AssertsEnabled) Debugging.Assert(index >= 0 && index < numBits);
            int wordNum = index >> 6; // div 64
            int bit = index & 0x3f; // mod 64
            long bitmask = 1L << bit;
            m_bits[wordNum] |= bitmask;
        }

        /// <summary>
        /// Sets the bit at the specified <paramref name="index"/>.
        /// The <paramref name="index"/> should be less than the <see cref="Length"/>.
        /// </summary>
        public virtual void FastSet(long index)
        {
            if (Debugging.AssertsEnabled) Debugging.Assert(index >= 0 && index < numBits);
            int wordNum = (int)(index >> 6);
            int bit = (int)index & 0x3f;
            long bitmask = 1L << bit;
            m_bits[wordNum] |= bitmask;
        }

        /// <summary>
        /// Sets a range of bits, expanding the set size if necessary.
        /// </summary>
        /// <param name="startIndex"> Lower index </param>
        /// <param name="endIndex"> One-past the last bit to set </param>
        public virtual void Set(long startIndex, long endIndex) // LUCENENET TODO: API: Change this to use startIndex and length to match .NET
        {
            if (endIndex <= startIndex)
            {
                return;
            }

            int startWord = (int)(startIndex >> 6);

            // since endIndex is one past the end, this is index of the last
            // word to be changed.
            int endWord = ExpandingWordNum(endIndex - 1);

            long startmask = -1L << (int)startIndex;
            long endmask = (long)(0xffffffffffffffffUL >> (int)-endIndex); // 64-(endIndex&0x3f) is the same as -endIndex due to wrap

            if (startWord == endWord)
            {
                m_bits[startWord] |= (startmask & endmask);
                return;
            }

            m_bits[startWord] |= startmask;
            Arrays.Fill(m_bits, startWord + 1, endWord, -1L);
            m_bits[endWord] |= endmask;
        }

        protected virtual int ExpandingWordNum(long index)
        {
            int wordNum = (int)(index >> 6);
            if (wordNum >= m_wlen)
            {
                EnsureCapacity(index + 1);
            }
            return wordNum;
        }

        /// <summary>
        /// Clears a bit.
        /// The <paramref name="index"/> should be less than the <see cref="Length"/>.
        /// </summary>
        public virtual void FastClear(int index)
        {
            if (Debugging.AssertsEnabled) Debugging.Assert(index >= 0 && index < numBits);
            int wordNum = index >> 6;
            int bit = index & 0x03f;
            long bitmask = 1L << bit;
            m_bits[wordNum] &= ~bitmask;
            // hmmm, it takes one more instruction to clear than it does to set... any
            // way to work around this?  If there were only 63 bits per word, we could
            // use a right shift of 10111111...111 in binary to position the 0 in the
            // correct place (using sign extension).
            // Could also use Long.rotateRight() or rotateLeft() *if* they were converted
            // by the JVM into a native instruction.
            // bits[word] &= Long.rotateLeft(0xfffffffe,bit);
        }

        /// <summary>
        /// Clears a bit.
        /// The <paramref name="index"/> should be less than the <see cref="Length"/>.
        /// </summary>
        public virtual void FastClear(long index)
        {
            if (Debugging.AssertsEnabled) Debugging.Assert(index >= 0 && index < numBits);
            int wordNum = (int)(index >> 6); // div 64
            int bit = (int)index & 0x3f; // mod 64
            long bitmask = 1L << bit;
            m_bits[wordNum] &= ~bitmask;
        }

        /// <summary>
        /// Clears a bit, allowing access beyond the current set size without changing the size. </summary>
        public virtual void Clear(long index)
        {
            int wordNum = (int)(index >> 6); // div 64
            if (wordNum >= m_wlen)
            {
                return;
            }
            int bit = (int)index & 0x3f; // mod 64
            long bitmask = 1L << bit;
            m_bits[wordNum] &= ~bitmask;
        }

        /// <summary>
        /// Clears a range of bits.  Clearing past the end does not change the size of the set.
        /// </summary>
        /// <param name="startIndex"> Lower index </param>
        /// <param name="endIndex"> One-past the last bit to clear </param>
        public virtual void Clear(int startIndex, int endIndex) // LUCENENET TODO: API: Change this to use startIndex and length to match .NET
        {
            if (endIndex <= startIndex)
            {
                return;
            }

            int startWord = (startIndex >> 6);
            if (startWord >= m_wlen)
            {
                return;
            }

            // since endIndex is one past the end, this is index of the last
            // word to be changed.
            int endWord = ((endIndex - 1) >> 6);

            long startmask = (-1L) << startIndex;  // -1 << (startIndex mod 64)
            long endmask = (-1L) << endIndex;      // -1 << (endIndex mod 64)
            if ((endIndex & 0x3f) == 0)
            {
                endmask = 0;
            }

            startmask = ~startmask;

            if (startWord == endWord)
            {
                m_bits[startWord] &= (startmask | endmask);
                return;
            }

            m_bits[startWord] &= startmask;

            int middle = Math.Min(m_wlen, endWord);
            Arrays.Fill(m_bits, startWord + 1, middle, 0L);
            if (endWord < m_wlen)
            {
                m_bits[endWord] &= endmask;
            }
        }

        /// <summary>
        /// Clears a range of bits.  Clearing past the end does not change the size of the set.
        /// </summary>
        /// <param name="startIndex"> Lower index </param>
        /// <param name="endIndex"> One-past the last bit to clear </param>
        public virtual void Clear(long startIndex, long endIndex) // LUCENENET TODO: API: Change this to use startIndex and length to match .NET
        {
            if (endIndex <= startIndex)
            {
                return;
            }

            int startWord = (int)(startIndex >> 6);
            if (startWord >= m_wlen)
            {
                return;
            }

            // since endIndex is one past the end, this is index of the last
            // word to be changed.
            int endWord = (int)((endIndex - 1) >> 6);

            long startmask = -1L << (int)startIndex;
            long endmask = (-1L).TripleShift((int)-endIndex); // 64-(endIndex&0x3f) is the same as -endIndex due to wrap

            // invert masks since we are clearing
            startmask = ~startmask;
            endmask = ~endmask;

            if (startWord == endWord)
            {
                m_bits[startWord] &= (startmask | endmask);
                return;
            }

            m_bits[startWord] &= startmask;

            int middle = Math.Min(m_wlen, endWord);
            Arrays.Fill(m_bits, startWord + 1, middle, 0L);
            if (endWord < m_wlen)
            {
                m_bits[endWord] &= endmask;
            }
        }

        /// <summary>
        /// Sets a bit and returns the previous value.
        /// The <paramref name="index"/> should be less than the <see cref="Length"/>.
        /// </summary>
        public virtual bool GetAndSet(int index)
        {
            if (Debugging.AssertsEnabled) Debugging.Assert(index >= 0 && index < numBits);
            int wordNum = index >> 6; // div 64
            int bit = index & 0x3f; // mod 64
            long bitmask = 1L << bit;
            bool val = (m_bits[wordNum] & bitmask) != 0;
            m_bits[wordNum] |= bitmask;
            return val;
        }

        /// <summary>
        /// Sets a bit and returns the previous value.
        /// The <paramref name="index"/> should be less than the <see cref="Length"/>.
        /// </summary>
        public virtual bool GetAndSet(long index)
        {
            if (Debugging.AssertsEnabled) Debugging.Assert(index >= 0 && index < numBits);
            int wordNum = (int)(index >> 6); // div 64
            int bit = (int)index & 0x3f; // mod 64
            long bitmask = 1L << bit;
            bool val = (m_bits[wordNum] & bitmask) != 0;
            m_bits[wordNum] |= bitmask;
            return val;
        }

        /// <summary>
        /// Flips a bit.
        /// The <paramref name="index"/> should be less than the <see cref="Length"/>.
        /// </summary>
        public virtual void FastFlip(int index)
        {
            if (Debugging.AssertsEnabled) Debugging.Assert(index >= 0 && index < numBits);
            int wordNum = index >> 6; // div 64
            int bit = index & 0x3f; // mod 64
            long bitmask = 1L << bit;
            m_bits[wordNum] ^= bitmask;
        }

        /// <summary>
        /// Flips a bit.
        /// The <paramref name="index"/> should be less than the <see cref="Length"/>.
        /// </summary>
        public virtual void FastFlip(long index)
        {
            if (Debugging.AssertsEnabled) Debugging.Assert(index >= 0 && index < numBits);
            int wordNum = (int)(index >> 6); // div 64
            int bit = (int)index & 0x3f; // mod 64
            long bitmask = 1L << bit;
            m_bits[wordNum] ^= bitmask;
        }

        /// <summary>
        /// Flips a bit, expanding the set size if necessary. </summary>
        public virtual void Flip(long index)
        {
            int wordNum = ExpandingWordNum(index);
            int bit = (int)index & 0x3f; // mod 64
            long bitmask = 1L << bit;
            m_bits[wordNum] ^= bitmask;
        }

        /// <summary>
        /// Flips a bit and returns the resulting bit value.
        /// The <paramref name="index"/> should be less than the <see cref="Length"/>.
        /// </summary>
        public virtual bool FlipAndGet(int index)
        {
            if (Debugging.AssertsEnabled) Debugging.Assert(index >= 0 && index < numBits);
            int wordNum = index >> 6; // div 64
            int bit = index & 0x3f; // mod 64
            long bitmask = 1L << bit;
            m_bits[wordNum] ^= bitmask;
            return (m_bits[wordNum] & bitmask) != 0;
        }

        /// <summary>
        /// Flips a bit and returns the resulting bit value.
        /// The <paramref name="index"/> should be less than the <see cref="Length"/>.
        /// </summary>
        public virtual bool FlipAndGet(long index)
        {
            if (Debugging.AssertsEnabled) Debugging.Assert(index >= 0 && index < numBits);
            int wordNum = (int)(index >> 6); // div 64
            int bit = (int)index & 0x3f; // mod 64
            long bitmask = 1L << bit;
            m_bits[wordNum] ^= bitmask;
            return (m_bits[wordNum] & bitmask) != 0;
        }

        /// <summary>
        /// Flips a range of bits, expanding the set size if necessary.
        /// </summary>
        /// <param name="startIndex"> Lower index </param>
        /// <param name="endIndex"> One-past the last bit to flip </param>
        public virtual void Flip(long startIndex, long endIndex) // LUCENENET TODO: API: Change this to use startIndex and length to match .NET
        {
            if (endIndex <= startIndex)
            {
                return;
            }

            int startWord = (int)(startIndex >> 6);

            // since endIndex is one past the end, this is index of the last
            // word to be changed.
            int endWord = ExpandingWordNum(endIndex - 1);


            //* Grrr, java shifting wraps around so -1L>>>64 == -1
            // for that reason, make sure not to use endmask if the bits to flip will
            // be zero in the last word (redefine endWord to be the last changed...)
            // long startmask = -1L << (startIndex & 0x3f);     // example: 11111...111000
            // long endmask = -1L >>> (64-(endIndex & 0x3f));   // example: 00111...111111
            // **

            long startmask = -1L << (int)startIndex;
            long endmask = (long)(0xffffffffffffffffUL >> (int)-endIndex); // 64-(endIndex&0x3f) is the same as -endIndex due to wrap

            if (startWord == endWord)
            {
                m_bits[startWord] ^= (startmask & endmask);
                return;
            }

            m_bits[startWord] ^= startmask;

            for (int i = startWord + 1; i < endWord; i++)
            {
                m_bits[i] = ~m_bits[i];
            }

            m_bits[endWord] ^= endmask;
        }

        /*
        public static int pop(long v0, long v1, long v2, long v3) {
          // derived from pop_array by setting last four elems to 0.
          // exchanges one pop() call for 10 elementary operations
          // saving about 7 instructions... is there a better way?
            long twosA=v0 & v1;
            long ones=v0^v1;

            long u2=ones^v2;
            long twosB =(ones&v2)|(u2&v3);
            ones=u2^v3;

            long fours=(twosA&twosB);
            long twos=twosA^twosB;

            return (pop(fours)<<2)
                   + (pop(twos)<<1)
                   + pop(ones);
        }
        */

        /// <summary>
        /// Gets the number of set bits.
        /// </summary>
        /// <returns> The number of set bits. </returns>
        public virtual long Cardinality
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => BitUtil.Pop_Array(m_bits, 0, m_wlen);
        }


        /// <summary>
        /// Returns the popcount or cardinality of the intersection of the two sets.
        /// Neither set is modified.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long IntersectionCount(OpenBitSet a, OpenBitSet b)
        {
            return BitUtil.Pop_Intersect(a.m_bits, b.m_bits, 0, Math.Min(a.m_wlen, b.m_wlen));
        }

        /// <summary>
        /// Returns the popcount or cardinality of the union of the two sets.
        /// Neither set is modified.
        /// </summary>
        public static long UnionCount(OpenBitSet a, OpenBitSet b)
        {
            long tot = BitUtil.Pop_Union(a.m_bits, b.m_bits, 0, Math.Min(a.m_wlen, b.m_wlen));
            if (a.m_wlen < b.m_wlen)
            {
                tot += BitUtil.Pop_Array(b.m_bits, a.m_wlen, b.m_wlen - a.m_wlen);
            }
            else if (a.m_wlen > b.m_wlen)
            {
                tot += BitUtil.Pop_Array(a.m_bits, b.m_wlen, a.m_wlen - b.m_wlen);
            }
            return tot;
        }

        /// <summary>
        /// Returns the popcount or cardinality of "a and not b"
        /// or "intersection(a, not(b))".
        /// Neither set is modified.
        /// </summary>
        public static long AndNotCount(OpenBitSet a, OpenBitSet b)
        {
            long tot = BitUtil.Pop_AndNot(a.m_bits, b.m_bits, 0, Math.Min(a.m_wlen, b.m_wlen));
            if (a.m_wlen > b.m_wlen)
            {
                tot += BitUtil.Pop_Array(a.m_bits, b.m_wlen, a.m_wlen - b.m_wlen);
            }
            return tot;
        }

        /// <summary>
        /// Returns the popcount or cardinality of the exclusive-or of the two sets.
        /// Neither set is modified.
        /// </summary>
        public static long XorCount(OpenBitSet a, OpenBitSet b)
        {
            long tot = BitUtil.Pop_Xor(a.m_bits, b.m_bits, 0, Math.Min(a.m_wlen, b.m_wlen));
            if (a.m_wlen < b.m_wlen)
            {
                tot += BitUtil.Pop_Array(b.m_bits, a.m_wlen, b.m_wlen - a.m_wlen);
            }
            else if (a.m_wlen > b.m_wlen)
            {
                tot += BitUtil.Pop_Array(a.m_bits, b.m_wlen, a.m_wlen - b.m_wlen);
            }
            return tot;
        }

        /// <summary>
        /// Returns the index of the first set bit starting at the <paramref name="index"/> specified.
        /// -1 is returned if there are no more set bits.
        /// </summary>
        public virtual int NextSetBit(int index)
        {
            int i = index >> 6;
            if (i >= m_wlen)
            {
                return -1;
            }
            int subIndex = index & 0x3f; // index within the word
            long word = m_bits[i] >> subIndex; // skip all the bits to the right of index

            if (word != 0)
            {
                return (i << 6) + subIndex + word.TrailingZeroCount();
            }

            while (++i < m_wlen)
            {
                word = m_bits[i];
                if (word != 0)
                {
                    return (i << 6) + word.TrailingZeroCount();
                }
            }

            return -1;
        }

        /// <summary>
        /// Returns the index of the first set bit starting at the <paramref name="index"/> specified.
        /// -1 is returned if there are no more set bits.
        /// </summary>
        public virtual long NextSetBit(long index)
        {
            int i = (int)index.TripleShift(6);
            if (i >= m_wlen)
            {
                return -1;
            }
            int subIndex = (int)index & 0x3f; // index within the word
            long word = m_bits[i].TripleShift(subIndex); // skip all the bits to the right of index

            if (word != 0)
            {
                return (((long)i) << 6) + (subIndex + word.TrailingZeroCount());
            }

            while (++i < m_wlen)
            {
                word = m_bits[i];
                if (word != 0)
                {
                    return (((long)i) << 6) + word.TrailingZeroCount();
                }
            }

            return -1;
        }

        /// <summary>
        /// Returns the index of the first set bit starting downwards at
        /// the <paramref name="index"/> specified.
        /// -1 is returned if there are no more set bits.
        /// </summary>
        public virtual int PrevSetBit(int index)
        {
            int i = index >> 6;
            int subIndex;
            long word;
            if (i >= m_wlen)
            {
                i = m_wlen - 1;
                if (i < 0)
                {
                    return -1;
                }
                subIndex = 63; // last possible bit
                word = m_bits[i];
            }
            else
            {
                if (i < 0)
                {
                    return -1;
                }
                subIndex = index & 0x3f; // index within the word
                word = (m_bits[i] << (63 - subIndex)); // skip all the bits to the left of index
            }

            if (word != 0)
            {
                return (i << 6) + subIndex - word.LeadingZeroCount(); // See LUCENE-3197
            }

            while (--i >= 0)
            {
                word = m_bits[i];
                if (word != 0)
                {
                    return (i << 6) + 63 - word.LeadingZeroCount();
                }
            }

            return -1;
        }

        /// <summary>
        /// Returns the index of the first set bit starting downwards at
        /// the <paramref name="index"/> specified.
        /// -1 is returned if there are no more set bits.
        /// </summary>
        public virtual long PrevSetBit(long index)
        {
            int i = (int)(index >> 6);
            int subIndex;
            long word;
            if (i >= m_wlen)
            {
                i = m_wlen - 1;
                if (i < 0)
                {
                    return -1;
                }
                subIndex = 63; // last possible bit
                word = m_bits[i];
            }
            else
            {
                if (i < 0)
                {
                    return -1;
                }
                subIndex = (int)index & 0x3f; // index within the word
                word = (m_bits[i] << (63 - subIndex)); // skip all the bits to the left of index
            }

            if (word != 0)
            {
                return (((long)i) << 6) + subIndex - word.LeadingZeroCount(); // See LUCENE-3197
            }

            while (--i >= 0)
            {
                word = m_bits[i];
                if (word != 0)
                {
                    return (((long)i) << 6) + 63 - word.LeadingZeroCount();
                }
            }

            return -1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public object Clone()
        {
            //OpenBitSet obs = (OpenBitSet)base.Clone();
            //obs.bits = (long[])obs.bits.Clone(); // hopefully an array clone is as fast(er) than arraycopy
            OpenBitSet obs = new OpenBitSet((long[])m_bits.Clone(), m_wlen);
            return obs;
        }

        /// <summary>
        /// this = this AND other </summary>
        public virtual void Intersect(OpenBitSet other)
        {
            int newLen = Math.Min(this.m_wlen, other.m_wlen);
            long[] thisArr = this.m_bits;
            long[] otherArr = other.m_bits;
            // testing against zero can be more efficient
            int pos = newLen;
            while (--pos >= 0)
            {
                thisArr[pos] &= otherArr[pos];
            }
            if (this.m_wlen > newLen)
            {
                // fill zeros from the new shorter length to the old length
                Arrays.Fill(m_bits, newLen, this.m_wlen, 0);
            }
            this.m_wlen = newLen;
        }

        /// <summary>
        /// this = this OR other </summary>
        public virtual void Union(OpenBitSet other)
        {
            int newLen = Math.Max(m_wlen, other.m_wlen);
            // LUCENENET specific: Since EnsureCapacityWords
            // sets m_wlen, we need to save the value here to ensure the
            // tail of the array is copied. Also removed the double-set
            // after Array.Copy.
            // https://github.com/apache/lucenenet/pull/154
            int oldLen = m_wlen;
            EnsureCapacityWords(newLen);
            if (Debugging.AssertsEnabled) Debugging.Assert((numBits = Math.Max(other.numBits, numBits)) >= 0);

            long[] thisArr = this.m_bits;
            long[] otherArr = other.m_bits;
            int pos = Math.Min(oldLen, other.m_wlen);
            while (--pos >= 0)
            {
                thisArr[pos] |= otherArr[pos];
            }
            if (oldLen < newLen)
            {
                Arrays.Copy(otherArr, oldLen, thisArr, oldLen, newLen - oldLen);
            }
        }

        /// <summary>
        /// Remove all elements set in other. this = this AND_NOT other. </summary>
        public virtual void Remove(OpenBitSet other)
        {
            int idx = Math.Min(m_wlen, other.m_wlen);
            long[] thisArr = this.m_bits;
            long[] otherArr = other.m_bits;
            while (--idx >= 0)
            {
                thisArr[idx] &= ~otherArr[idx];
            }
        }

        /// <summary>
        /// this = this XOR other </summary>
        public virtual void Xor(OpenBitSet other)
        {
            int newLen = Math.Max(m_wlen, other.m_wlen);
            // LUCENENET specific: Since EnsureCapacityWords
            // sets m_wlen, we need to save the value here to ensure the
            // tail of the array is copied. Also removed the double-set
            // after Array.Copy.
            // https://github.com/apache/lucenenet/pull/154
            int oldLen = m_wlen;
            EnsureCapacityWords(newLen);
            if (Debugging.AssertsEnabled) Debugging.Assert((numBits = Math.Max(other.numBits, numBits)) >= 0);

            long[] thisArr = this.m_bits;
            long[] otherArr = other.m_bits;
            int pos = Math.Min(oldLen, other.m_wlen);
            while (--pos >= 0)
            {
                thisArr[pos] ^= otherArr[pos];
            }
            if (oldLen < newLen)
            {
                Arrays.Copy(otherArr, oldLen, thisArr, oldLen, newLen - oldLen);
            }
        }

        // some BitSet compatability methods

        /// <summary>see <see cref="Intersect(OpenBitSet)"/></summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void And(OpenBitSet other)
        {
            Intersect(other);
        }

        /// <summary>see <see cref="Union(OpenBitSet)"/></summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void Or(OpenBitSet other)
        {
            Union(other);
        }

        /// <summary>see <see cref="AndNot(OpenBitSet)"/></summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void AndNot(OpenBitSet other)
        {
            Remove(other);
        }

        /// <summary>
        /// returns <c>true</c> if the sets have any elements in common. </summary>
        public virtual bool Intersects(OpenBitSet other)
        {
            int pos = Math.Min(this.m_wlen, other.m_wlen);
            long[] thisArr = this.m_bits;
            long[] otherArr = other.m_bits;
            while (--pos >= 0)
            {
                if ((thisArr[pos] & otherArr[pos]) != 0)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Expand the <see cref="T:long[]"/> with the size given as a number of words (64 bit longs). </summary>
        public virtual void EnsureCapacityWords(int numWords)
        {
            m_bits = ArrayUtil.Grow(m_bits, numWords);
            m_wlen = numWords;
            if (Debugging.AssertsEnabled) Debugging.Assert((this.numBits = Math.Max(this.numBits, numWords << 6)) >= 0);
        }

        /// <summary>
        /// Ensure that the <see cref="T:long[]"/> is big enough to hold numBits, expanding it if
        /// necessary.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void EnsureCapacity(long numBits)
        {
            EnsureCapacityWords(Bits2words(numBits));
            // ensureCapacityWords sets numBits to a multiple of 64, but we want to set
            // it to exactly what the app asked.
            if (Debugging.AssertsEnabled) Debugging.Assert((this.numBits = Math.Max(this.numBits, numBits)) >= 0);
        }

        /// <summary>
        /// Lowers numWords, the number of words in use,
        /// by checking for trailing zero words.
        /// </summary>
        public virtual void TrimTrailingZeros()
        {
            int idx = m_wlen - 1;
            while (idx >= 0 && m_bits[idx] == 0)
            {
                idx--;
            }
            m_wlen = idx + 1;
        }

        /// <summary>
        /// Returns the number of 64 bit words it would take to hold <paramref name="numBits"/>. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Bits2words(long numBits)
        {
            return (int)(((numBits - 1) >> 6) + 1);
        }

        /// <summary>
        /// Returns <c>true</c> if both sets have the same bits set. </summary>
        public override bool Equals(object o)
        {
            if (this == o)
            {
                return true;
            }
            if (!(o is OpenBitSet))
            {
                return false;
            }
            OpenBitSet a;
            OpenBitSet b = (OpenBitSet)o;
            // make a the larger set.
            if (b.m_wlen > this.m_wlen)
            {
                a = b;
                b = this;
            }
            else
            {
                a = this;
            }

            // check for any set bits out of the range of b
            for (int i = a.m_wlen - 1; i >= b.m_wlen; i--)
            {
                if (a.m_bits[i] != 0)
                {
                    return false;
                }
            }

            for (int i = b.m_wlen - 1; i >= 0; i--)
            {
                if (a.m_bits[i] != b.m_bits[i])
                {
                    return false;
                }
            }

            return true;
        }

        public override int GetHashCode()
        {
            // Start with a zero hash and use a mix that results in zero if the input is zero.
            // this effectively truncates trailing zeros without an explicit check.
            long h = 0;
            for (int i = m_bits.Length; --i >= 0; )
            {
                h ^= m_bits[i];
                h = (h << 1) | (h.TripleShift(63)); // rotate left
            }
            // fold leftmost bits into right and add a constant to prevent
            // empty sets from returning 0, which is too common.
            return (int)((h >> 32) ^ h) + unchecked((int)0x98761234);
        }
    }
}