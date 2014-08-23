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

namespace Lucene.Net.Util
{
    using System;
    using System.Diagnostics;
    using Lucene.Net.Support;
    using DocIdSet = Lucene.Net.Search.DocIdSet;
    using DocIdSetIterator = Lucene.Net.Search.DocIdSetIterator;

    // ReSharper disable CSharpWarnings::CS1574
    /// <summary>
    /// An "open" BitSet implementation that allows direct access to the array of words
    /// storing the bits.
    /// </summary>
    /// <remarks>
    /// 
    /// <p/>
    /// Unlike java.util.bitset, the fact that bits are packed into an array of longs
    /// is part of the interface.  this allows efficient implementation of other algorithms
    /// by someone other than the author.  It also allows one to efficiently implement
    /// alternate serialization or interchange formats.
    /// <p/>
    /// <code>OpenBitSet</code> is faster than <code>java.util.BitSet</code> in most operations
    /// and *much* faster at calculating cardinality of sets and results of set operations.
    /// It can also handle sets of larger cardinality (up to 64 * 2**32-1)
    /// <p/>
    /// The goals of <code>OpenBitSet</code> are the fastest implementation possible, and
    /// maximum code reuse.  Extra safety and encapsulation
    /// may always be built on top, but if that's built in, the cost can never be removed (and
    /// hence people re-implement their own version in order to get better performance).
    /// If you want a "safe", totally encapsulated (and slower and limited) BitSet
    /// class, use <code>java.util.BitSet</code>.
    /// <p/>
    /// <h3>Performance Results</h3>
    ///
    /// Test system: Pentium 4, Sun Java 1.5_06 -server -Xbatch -Xmx64M
    /// <br/>BitSet size = 1,000,000
    /// <br/>Results are java.util.BitSet time divided by OpenBitSet time.
    /// <table border="1">
    /// <tr>
    ///  <th></th> <th>cardinality</th> <th>intersect_count</th> <th>union</th> <th>nextSetBit</th> <th>get</th> <th>iterator</th>
    /// </tr>
    /// <tr>
    ///  <th>50% full</th> <td>3.36</td> <td>3.96</td> <td>1.44</td> <td>1.46</td> <td>1.99</td> <td>1.58</td>
    /// </tr>
    /// <tr>
    ///   <th>1% full</th> <td>3.31</td> <td>3.90</td> <td>&amp;nbsp;</td> <td>1.04</td> <td>&amp;nbsp;</td> <td>0.99</td>
    /// </tr>
    /// </table>
    /// <br/>
    /// Test system: AMD Opteron, 64 bit linux, Sun Java 1.5_06 -server -Xbatch -Xmx64M
    /// <br/>BitSet size = 1,000,000
    /// <br/>Results are java.util.BitSet time divided by OpenBitSet time.
    /// <table border="1">
    /// <tr>
    ///  <th></th> <th>cardinality</th> <th>intersect_count</th> <th>union</th> <th>nextSetBit</th> <th>get</th> <th>iterator</th>
    /// </tr>
    /// <tr>
    ///  <th>50% full</th> <td>2.50</td> <td>3.50</td> <td>1.00</td> <td>1.03</td> <td>1.12</td> <td>1.25</td>
    /// </tr>
    /// <tr>
    ///   <th>1% full</th> <td>2.51</td> <td>3.49</td> <td>&amp;nbsp;</td> <td>1.00</td> <td>&amp;nbsp;</td> <td>1.02</td>
    /// </tr>
    /// </table>
    /// </remarks>

    public class OpenBitSet : DocIdSet, IBits, ICloneable
    {
        private long[] bits;
        private int wordLength; // number of words (elements) used in the array

        // Used only for assert:
        private long numBits;

        /// <summary>
        /// Constructs an OpenBitSet large enough to hold {@code numBits}. </summary>
        public OpenBitSet(long numBits)
        {
            this.numBits = numBits;
            bits = new long[Bits2Words(numBits)];
            wordLength = bits.Length;
        }

        /// <summary>
        /// Constructor: allocates enough space for 64 bits. </summary>
        public OpenBitSet()
            : this(64)
        {
        }

        /// <summary>
        /// Constructs an OpenBitSet from an existing long[].
        /// </summary>
        /// <remarks>
        /// <p>
        /// The first 64 bits are in long[0], with bit index 0 at the least significant
        /// bit, and bit index 63 at the most significant. Given a bit index, the word
        /// containing it is long[index/64], and it is at bit number index%64 within
        /// that word.
        /// </p>
        /// <p>
        /// numWords are the number of elements in the array that contain set bits
        /// (non-zero longs). numWords should be &lt;= bits.length, and any existing
        /// words in the array at position &gt;= numWords should be zero.
        /// </p>
        /// </remarks>
        public OpenBitSet(long[] bits, int numWords)
        {
            if (numWords > bits.Length)
            {
                throw new ArgumentException("numWords cannot exceed bits.length");
            }
            this.bits = bits;
            this.wordLength = numWords;
            this.numBits = wordLength * 64;
        }

        /// <summary>
        /// Provides a <seealso cref="DocIdSetIterator" /> to access the set.
        /// this implementation can return <code>null</code> if there
        /// are no docs that match.
        /// </summary>
        /// <returns>DocIdSetIterator.</returns>
        public override DocIdSetIterator GetIterator()
        {
            return new OpenBitSetIterator(bits, this.wordLength);
        }

        /// <summary>
        /// Gets the bits.
        /// </summary>
        /// <returns>IBits.</returns>
        public override IBits GetBits()
        {
            return this;
        }

        /// <summary>

        /// Gets the <see cref="System.Boolean"/> at the specified index.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        public bool this[int index]
        {
            get { return this.Get(index); }
        }

        /// <summary>
        /// this DocIdSet implementation is cacheable. </summary>
        public override bool Cacheable
        {
            get
            {
                return true;
            }
        }

        /// <summary>
        /// Returns the current capacity in bits (1 greater than the index of the last bit) </summary>
        public virtual long Capacity()
        {
            return bits.Length << 6;
        }

        /// <summary>
        /// Returns the current capacity of this set.  Included for
        /// compatibility.  this is *not* equal to <seealso cref="Cardinality"/>
        /// </summary>
        public virtual long Size()
        {
            return Capacity();
        }

        /// <summary>
        /// Returns the number of bits in the set.
        /// </summary>
        /// <value>The length.</value>
        public virtual int Length
        {
            get
            {
                return bits.Length << 6;
            }
        }

        /// <summary>
        /// Returns true if there are no set bits </summary>
        public virtual bool Empty
        {
            get
            {
                return Cardinality() == 0;
            }
        }

        /// <summary>
        /// Expert: returns the long[] storing the bits </summary>
        public virtual long[] Bits
        {
            get
            {
                return bits;
            }
        }

        /// <summary>
        /// Expert: gets the number of longs in the array that are in use </summary>
        public virtual int NumWords
        {
            get
            {
                return wordLength;
            }
        }

        /// <summary>
        /// Returns true or false for the specified bit index. </summary>
        public virtual bool Get(int index)
        {
            int i = index >> 6; // div 64
            // signed shift will keep a negative index and force an
            // array-index-out-of-bounds-exception, removing the need for an explicit check.
            if (i >= bits.Length)
            {
                return false;
            }

            int bit = index & 0x3f; // mod 64
            long bitMask = 1L << bit;
            return (bits[i] & bitMask) != 0;
        }

        /// <summary>
        /// Returns true or false for the specified bit index.
        /// The index should be less than the OpenBitSet size
        /// </summary>
        public virtual bool FastGet(int index)
        {
            Debug.Assert(index >= 0 && index < numBits);
            int i = index >> 6; // div 64
            // signed shift will keep a negative index and force an
            // array-index-out-of-bounds-exception, removing the need for an explicit check.
            int bit = index & 0x3f; // mod 64
            long bitMask = 1L << bit;
            return (bits[i] & bitMask) != 0;
        }

        /// <summary>
        /// Returns true or false for the specified bit index
        /// </summary>
        public virtual bool Get(long index)
        {
            var i = (int)(index >> 6); // div 64
            if (i >= bits.Length)
            {
                return false;
            }
            int bit = (int)index & 0x3f; // mod 64
            long bitMask = 1L << bit;
            return (bits[i] & bitMask) != 0;
        }

        /// <summary>
        /// Returns true or false for the specified bit index.
        /// The index should be less than the OpenBitSet size.
        /// </summary>
        public virtual bool FastGet(long index)
        {
            Debug.Assert(index >= 0 && index < numBits);
            int i = (int)(index >> 6); // div 64
            int bit = (int)index & 0x3f; // mod 64
            long bitMask = 1L << bit;
            return (bits[i] & bitMask) != 0;
        }

        /*
        // alternate implementation of get()
        public boolean get1(int index) {
          int i = index >> 6;                // div 64
          int bit = index & 0x3f;            // mod 64
          return ((bits[i]>>>bit) & 0x01) != 0;
          // this does a long shift and a bittest (on x86) vs
          // a long shift, and a long AND, (the test for zero is prob a no-op)
          // testing on a P4 indicates this is slower than (bits[i] & bitMask) != 0;
        }
        */

        /// <summary>
        /// returns 1 if the bit is set, 0 if not.
        /// The index should be less than the OpenBitSet size
        /// </summary>
        public virtual int GetBit(int index)
        {
            Debug.Assert(index >= 0 && index < numBits);
            int i = index >> 6; // div 64
            int bit = index & 0x3f; // mod 64
            return ((int)((long)((ulong)bits[i] >> bit))) & 0x01;
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
        /// sets a bit, expanding the set size if necessary </summary>
        public virtual void Set(long index)
        {
            int wordNum = ExpandingWordNum(index);
            int bit = (int)index & 0x3f;
            long bitMask = 1L << bit;
            bits[wordNum] |= bitMask;
        }

        /// <summary>
        /// Sets the bit at the specified index.
        /// The index should be less than the OpenBitSet size.
        /// </summary>
        public virtual void FastSet(int index)
        {
            Debug.Assert(index >= 0 && index < numBits);
            int wordNum = index >> 6; // div 64
            int bit = index & 0x3f; // mod 64
            long bitMask = 1L << bit;
            bits[wordNum] |= bitMask;
        }

        /// <summary>
        /// Sets the bit at the specified index.
        /// The index should be less than the OpenBitSet size.
        /// </summary>
        public virtual void FastSet(long index)
        {
            Debug.Assert(index >= 0 && index < numBits);
            int wordNum = (int)(index >> 6);
            int bit = (int)index & 0x3f;
            long bitMask = 1L << bit;
            bits[wordNum] |= bitMask;
        }

        /// <summary>
        /// Sets a range of bits, expanding the set size if necessary
        /// </summary>
        /// <param name="startIndex"> lower index </param>
        /// <param name="endIndex"> one-past the last bit to set </param>
        public virtual void Set(long startIndex, long endIndex)
        {
            if (endIndex <= startIndex)
            {
                return;
            }

            int startWord = (int)(startIndex >> 6);

            // since endIndex is one past the end, this is index of the last
            // word to be changed.
            int endWord = ExpandingWordNum(endIndex - 1);

            long startMask = -1L << (int)startIndex;
            long endMask = -(int)((uint)1L >> (int)-endIndex); // 64-(endIndex&0x3f) is the same as -endIndex due to wrap

            if (startWord == endWord)
            {
                bits[startWord] |= (startMask & endMask);
                return;
            }

            bits[startWord] |= startMask;
            bits.Fill(startWord + 1, endWord, -1L);
            bits[endWord] |= endMask;
        }

        protected internal virtual int ExpandingWordNum(long index)
        {
            int wordNum = (int)(index >> 6);
            if (wordNum >= wordLength)
            {
                EnsureCapacity(index + 1);
            }
            return wordNum;
        }

        /// <summary>
        /// clears a bit.
        /// The index should be less than the OpenBitSet size.
        /// </summary>
        public virtual void FastClear(int index)
        {
            Debug.Assert(index >= 0 && index < numBits);
            int wordNum = index >> 6;
            int bit = index & 0x03f;
            long bitMask = 1L << bit;
            bits[wordNum] &= ~bitMask;
            // hmmm, it takes one more instruction to clear than it does to set... any
            // way to work around this?  If there were only 63 bits per word, we could
            // use a right shift of 10111111...111 in binary to position the 0 in the
            // correct place (using sign extension).
            // Could also use Long.rotateRight() or rotateLeft() *if* they were converted
            // by the JVM into a native instruction.
            // bits[word] &= Long.rotateLeft(0xfffffffe,bit);
        }

        /// <summary>
        /// clears a bit.
        /// The index should be less than the OpenBitSet size.
        /// </summary>
        public virtual void FastClear(long index)
        {
            Debug.Assert(index >= 0 && index < numBits);
            int wordNum = (int)(index >> 6); // div 64
            int bit = (int)index & 0x3f; // mod 64
            long bitMask = 1L << bit;
            bits[wordNum] &= ~bitMask;
        }

        /// <summary>
        /// clears a bit, allowing access beyond the current set size without changing the size. </summary>
        public virtual void Clear(long index)
        {
            int wordNum = (int)(index >> 6); // div 64
            if (wordNum >= wordLength)
            {
                return;
            }
            int bit = (int)index & 0x3f; // mod 64
            long bitMask = 1L << bit;
            bits[wordNum] &= ~bitMask;
        }

        /// <summary>
        /// Clears a range of bits.  Clearing past the end does not change the size of the set.
        /// </summary>
        /// <param name="startIndex"> lower index </param>
        /// <param name="endIndex"> one-past the last bit to clear </param>
        public virtual void Clear(int startIndex, int endIndex)
        {
            if (endIndex <= startIndex)
            {
                return;
            }

            int startWord = (startIndex >> 6);
            if (startWord >= wordLength)
            {
                return;
            }

            // since endIndex is one past the end, this is index of the last
            // word to be changed.
            int endWord = ((endIndex - 1) >> 6);

            //LUCENE TO-DO
            long startMask = -1L << startIndex;
            long endMask = -(int)((uint)1L >> -endIndex); // 64-(endIndex&0x3f) is the same as -endIndex due to wrap

            // invert masks since we are clearing
            startMask = ~startMask;
            endMask = ~endMask;

            if (startWord == endWord)
            {
                bits[startWord] &= (startMask | endMask);
                return;
            }

            bits[startWord] &= startMask;

            int middle = Math.Min(wordLength, endWord);
            bits.Fill(startWord + 1, middle, 0L);
            if (endWord < wordLength)
            {
                bits[endWord] &= endMask;
            }
        }

        /// <summary>
        /// Clears a range of bits.  Clearing past the end does not change the size of the set.
        /// </summary>
        /// <param name="startIndex"> lower index </param>
        /// <param name="endIndex"> one-past the last bit to clear </param>
        public virtual void Clear(long startIndex, long endIndex)
        {
            if (endIndex <= startIndex)
            {
                return;
            }

            int startWord = (int)(startIndex >> 6);
            if (startWord >= wordLength)
            {
                return;
            }

            // since endIndex is one past the end, this is index of the last
            // word to be changed.
            int endWord = (int)((endIndex - 1) >> 6);

            //LUCENE TO-DO
            long startMask = -1L << (int)startIndex;
            long endMask = -(int)((uint)1L >> (int)-endIndex); // 64-(endIndex&0x3f) is the same as -endIndex due to wrap

            // invert masks since we are clearing
            startMask = ~startMask;
            endMask = ~endMask;

            if (startWord == endWord)
            {
                bits[startWord] &= (startMask | endMask);
                return;
            }

            bits[startWord] &= startMask;

            int middle = Math.Min(wordLength, endWord);
            bits.Fill(startWord + 1, middle, 0L);
            if (endWord < wordLength)
            {
                bits[endWord] &= endMask;
            }
        }

        /// <summary>
        /// Sets a bit and returns the previous value.
        /// The index should be less than the OpenBitSet size.
        /// </summary>
        public virtual bool GetAndSet(int index)
        {
            Debug.Assert(index >= 0 && index < numBits);
            int wordNum = index >> 6; // div 64
            int bit = index & 0x3f; // mod 64
            long bitMask = 1L << bit;
            bool val = (bits[wordNum] & bitMask) != 0;
            bits[wordNum] |= bitMask;
            return val;
        }

        /// <summary>
        /// Sets a bit and returns the previous value.
        /// The index should be less than the OpenBitSet size.
        /// </summary>
        public virtual bool GetAndSet(long index)
        {
            Debug.Assert(index >= 0 && index < numBits);
            int wordNum = (int)(index >> 6); // div 64
            int bit = (int)index & 0x3f; // mod 64
            long bitMask = 1L << bit;
            bool val = (bits[wordNum] & bitMask) != 0;
            bits[wordNum] |= bitMask;
            return val;
        }

        /// <summary>
        /// flips a bit.
        /// The index should be less than the OpenBitSet size.
        /// </summary>
        public virtual void FastFlip(int index)
        {
            Debug.Assert(index >= 0 && index < numBits);
            int wordNum = index >> 6; // div 64
            int bit = index & 0x3f; // mod 64
            long bitMask = 1L << bit;
            bits[wordNum] ^= bitMask;
        }

        /// <summary>
        /// flips a bit.
        /// The index should be less than the OpenBitSet size.
        /// </summary>
        public virtual void FastFlip(long index)
        {
            Debug.Assert(index >= 0 && index < numBits);
            int wordNum = (int)(index >> 6); // div 64
            int bit = (int)index & 0x3f; // mod 64
            long bitMask = 1L << bit;
            bits[wordNum] ^= bitMask;
        }

        /// <summary>
        /// flips a bit, expanding the set size if necessary </summary>
        public virtual void Flip(long index)
        {
            int wordNum = ExpandingWordNum(index);
            int bit = (int)index & 0x3f; // mod 64
            long bitMask = 1L << bit;
            bits[wordNum] ^= bitMask;
        }

        /// <summary>
        /// flips a bit and returns the resulting bit value.
        /// The index should be less than the OpenBitSet size.
        /// </summary>
        public virtual bool FlipAndGet(int index)
        {
            Debug.Assert(index >= 0 && index < numBits);
            int wordNum = index >> 6; // div 64
            int bit = index & 0x3f; // mod 64
            long bitMask = 1L << bit;
            bits[wordNum] ^= bitMask;
            return (bits[wordNum] & bitMask) != 0;
        }

        /// <summary>
        /// flips a bit and returns the resulting bit value.
        /// The index should be less than the OpenBitSet size.
        /// </summary>
        public virtual bool FlipAndGet(long index)
        {
            Debug.Assert(index >= 0 && index < numBits);
            int wordNum = (int)(index >> 6); // div 64
            int bit = (int)index & 0x3f; // mod 64
            long bitMask = 1L << bit;
            bits[wordNum] ^= bitMask;
            return (bits[wordNum] & bitMask) != 0;
        }

        /// <summary>
        /// Flips a range of bits, expanding the set size if necessary
        /// </summary>
        /// <param name="startIndex"> lower index </param>
        /// <param name="endIndex"> one-past the last bit to flip </param>
        public virtual void Flip(long startIndex, long endIndex)
        {
            if (endIndex <= startIndex)
            {
                return;
            }
            int startWord = (int)(startIndex >> 6);

            // since endIndex is one past the end, this is index of the last
            // word to be changed.
            int endWord = ExpandingWordNum(endIndex - 1);

           
            // Grrr, java shifting wraps around so -1L>>>64 == -1
            // for that reason, make sure not to use endmask if the bits to flip will
            // be zero in the last word (redefine endWord to be the last changed...)
            // long startmask = -1L << (startIndex & 0x3f);     // example: 11111...111000
            // long endmask = -1L >>> (64-(endIndex & 0x3f));   // example: 00111...111111
           

            //LUCENE TO-DO
            long startMask = -1L << (int)startIndex;
            long endMask = -(int)((uint)1L >> (int)-endIndex); // 64-(endIndex&0x3f) is the same as -endIndex due to wrap

            if (startWord == endWord)
            {
                bits[startWord] ^= (startMask & endMask);
                return;
            }

            bits[startWord] ^= startMask;

            for (int i = startWord + 1; i < endWord; i++)
            {
                bits[i] = ~bits[i];
            }

            bits[endWord] ^= endMask;
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

        /// <returns> the number of set bits </returns>
        public virtual long Cardinality()
        {
            return BitUtil.PopArray(bits, 0, wordLength);
        }

        /// <summary>
        /// Returns the popcount or cardinality of the intersection of the two sets.
        /// Neither set is modified.
        /// </summary>
        public static long IntersectionCount(OpenBitSet a, OpenBitSet b)
        {
            return BitUtil.PopIntersect(a.bits, b.bits, 0, Math.Min(a.wordLength, b.wordLength));
        }

        /// <summary>
        /// Returns the popcount or cardinality of the union of the two sets.
        /// Neither set is modified.
        /// </summary>
        public static long UnionCount(OpenBitSet a, OpenBitSet b)
        {
            long tot = BitUtil.PopUnion(a.bits, b.bits, 0, Math.Min(a.wordLength, b.wordLength));
            if (a.wordLength < b.wordLength)
            {
                tot += BitUtil.PopArray(b.bits, a.wordLength, b.wordLength - a.wordLength);
            }
            else if (a.wordLength > b.wordLength)
            {
                tot += BitUtil.PopArray(a.bits, b.wordLength, a.wordLength - b.wordLength);
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
            long tot = BitUtil.PopAndNot(a.bits, b.bits, 0, Math.Min(a.wordLength, b.wordLength));
            if (a.wordLength > b.wordLength)
            {
                tot += BitUtil.PopArray(a.bits, b.wordLength, a.wordLength - b.wordLength);
            }
            return tot;
        }

        /// <summary>
        /// Returns the popcount or cardinality of the exclusive-or of the two sets.
        /// Neither set is modified.
        /// </summary>
        public static long XorCount(OpenBitSet a, OpenBitSet b)
        {
            long tot = BitUtil.PopXor(a.bits, b.bits, 0, Math.Min(a.wordLength, b.wordLength));
            if (a.wordLength < b.wordLength)
            {
                tot += BitUtil.PopArray(b.bits, a.wordLength, b.wordLength - a.wordLength);
            }
            else if (a.wordLength > b.wordLength)
            {
                tot += BitUtil.PopArray(a.bits, b.wordLength, a.wordLength - b.wordLength);
            }
            return tot;
        }

        /// <summary>
        /// Returns the index of the first set bit starting at the index specified.
        ///  -1 is returned if there are no more set bits.
        /// </summary>
        public virtual int NextSetBit(int index)
        {
            int i = index >> 6;
            if (i >= wordLength)
            {
                return -1;
            }
            int subIndex = index & 0x3f; // index within the word
            long word = bits[i] >> subIndex; // skip all the bits to the right of index

            if (word != 0)
            {
                return (i << 6) + subIndex + word.NumberOfTrailingZeros();
            }

            while (++i < wordLength)
            {
                word = bits[i];
                if (word != 0)
                {
                    return (i << 6) + word.NumberOfTrailingZeros();
                }
            }

            return -1;
        }

        /// <summary>
        /// Returns the index of the first set bit starting at the index specified.
        ///  -1 is returned if there are no more set bits.
        /// </summary>
        public virtual long NextSetBit(long index)
        {
            int i = (int)((long)((ulong)index >> 6));
            if (i >= wordLength)
            {
                return -1;
            }
            int subIndex = (int)index & 0x3f; // index within the word
            long word = (long)((ulong)bits[i] >> subIndex); // skip all the bits to the right of index

            if (word != 0)
            {
                return (((long)i) << 6) + (subIndex + word.NumberOfTrailingZeros());
            }

            while (++i < wordLength)
            {
                word = bits[i];
                if (word != 0)
                {
                    return (((long)i) << 6) + word.NumberOfTrailingZeros();
                }
            }

            return -1;
        }

        /// <summary>
        /// Returns the index of the first set bit starting downwards at
        ///  the index specified.
        ///  -1 is returned if there are no more set bits.
        /// </summary>
        public virtual int PrevSetBit(int index)
        {
            int i = index >> 6;
            int subIndex;
            long word;
            if (i >= wordLength)
            {
                i = wordLength - 1;
                if (i < 0)
                {
                    return -1;
                }
                subIndex = 63; // last possible bit
                word = bits[i];
            }
            else
            {
                if (i < 0)
                {
                    return -1;
                }
                subIndex = index & 0x3f; // index within the word
                word = (bits[i] << (63 - subIndex)); // skip all the bits to the left of index
            }

            if (word != 0)
            {
                return (i << 6) + subIndex - word.NumberOfLeadingZeros(); // See LUCENE-3197
            }

            while (--i >= 0)
            {
                word = bits[i];
                if (word != 0)
                {
                    return (i << 6) + 63 - word.NumberOfLeadingZeros();
                }
            }

            return -1;
        }

        /// <summary>
        /// Returns the index of the first set bit starting downwards at
        ///  the index specified.
        ///  -1 is returned if there are no more set bits.
        /// </summary>
        public virtual long PrevSetBit(long index)
        {
            int i = (int)(index >> 6);
            int subIndex;
            long word;
            if (i >= wordLength)
            {
                i = wordLength - 1;
                if (i < 0)
                {
                    return -1;
                }
                subIndex = 63; // last possible bit
                word = bits[i];
            }
            else
            {
                if (i < 0)
                {
                    return -1;
                }
                subIndex = (int)index & 0x3f; // index within the word
                word = (bits[i] << (63 - subIndex)); // skip all the bits to the left of index
            }

            if (word != 0)
            {
                return (((long)i) << 6) + subIndex - word.NumberOfLeadingZeros(); // See LUCENE-3197
            }

            while (--i >= 0)
            {
                word = bits[i];
                if (word != 0)
                {
                    return (((long)i) << 6) + 63 - word.NumberOfLeadingZeros();
                }
            }

            return -1;
        }

        /// <summary>
        /// Clones <see cref="OpenBitSet"/>. Set <paramref name="deepCopy"/> to
        /// to <c>true</c> for most copies.
        /// </summary>
        /// <param name="deepCopy">if set to <c>true</c> [deep copy].</param>
        /// <returns>System.Object.</returns>
        /// <exception cref="System.InvalidOperationException"></exception>
        public object Clone(bool deepCopy = true)
        {
            if (!deepCopy) 
                return new OpenBitSet(this.bits.Copy(), this.wordLength);
            
            try
            {
                //OpenBitSet obs = (OpenBitSet)base.Clone();
                //obs.bits = (long[])obs.bits.Clone(); // hopefully an array clone is as fast(er) than arraycopy
                var obs = new OpenBitSet((long[])this.bits.Clone(), this.wordLength);
                return obs;
            }
            catch (Exception e)
            {
                throw new InvalidOperationException(e.Message, e);
            }
        }

        /// <summary>
        /// this = this AND other </summary>
        public virtual void Intersect(OpenBitSet other)
        {
            int newLen = Math.Min(this.wordLength, other.wordLength);
            long[] thisArr = this.bits;
            long[] otherArr = other.bits;
            // testing against zero can be more efficient
            int pos = newLen;
            while (--pos >= 0)
            {
                thisArr[pos] &= otherArr[pos];
            }
            if (this.wordLength > newLen)
            {
                // fill zeros from the new shorter length to the old length
                bits.Fill(newLen, this.wordLength, 0L);
            }
            this.wordLength = newLen;
        }

        /// <summary>
        /// this = this OR other </summary>
        public virtual void Union(OpenBitSet other)
        {
            int newLen = Math.Max(wordLength, other.wordLength);
            EnsureCapacityWords(newLen);
            Debug.Assert((numBits = Math.Max(other.numBits, numBits)) >= 0);

            long[] thisArr = this.bits;
            long[] otherArr = other.bits;
            int pos = Math.Min(wordLength, other.wordLength);
            while (--pos >= 0)
            {
                thisArr[pos] |= otherArr[pos];
            }
            if (this.wordLength < newLen)
            {
                Array.Copy(otherArr, this.wordLength, thisArr, this.wordLength, newLen - this.wordLength);
            }
            this.wordLength = newLen;
        }

        /// <summary>
        /// Remove all elements set in other. this = this AND_NOT other </summary>
        public virtual void Remove(OpenBitSet other)
        {
            int idx = Math.Min(wordLength, other.wordLength);
            long[] thisArr = this.bits;
            long[] otherArr = other.bits;
            while (--idx >= 0)
            {
                thisArr[idx] &= ~otherArr[idx];
            }
        }

        /// <summary>
        /// this = this XOR other </summary>
        public virtual void Xor(OpenBitSet other)
        {
            int newLen = Math.Max(wordLength, other.wordLength);
            EnsureCapacityWords(newLen);
            Debug.Assert((numBits = Math.Max(other.numBits, numBits)) >= 0);

            long[] thisArr = this.bits;
            long[] otherArr = other.bits;
            int pos = Math.Min(wordLength, other.wordLength);
            while (--pos >= 0)
            {
                thisArr[pos] ^= otherArr[pos];
            }
            if (this.wordLength < newLen)
            {
                Array.Copy(otherArr, this.wordLength, thisArr, this.wordLength, newLen - this.wordLength);
            }
            this.wordLength = newLen;
        }

        // some BitSet compatability methods

        //** see {@link intersect} */
        public virtual void And(OpenBitSet other)
        {
            Intersect(other);
        }

        //** see {@link union} */
        public virtual void Or(OpenBitSet other)
        {
            Union(other);
        }

        //** see {@link andNot} */
        public virtual void AndNot(OpenBitSet other)
        {
            Remove(other);
        }

        /// <summary>
        /// returns true if the sets have any elements in common </summary>
        public virtual bool Intersects(OpenBitSet other)
        {
            int pos = Math.Min(this.wordLength, other.wordLength);
            long[] thisArr = this.bits;
            long[] otherArr = other.bits;
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
        /// Expand the long[] with the size given as a number of words (64 bit longs). </summary>
        public virtual void EnsureCapacityWords(int numWords)
        {
            bits = ArrayUtil.Grow(bits, numWords);
            wordLength = numWords;
            Debug.Assert((this.numBits = Math.Max(this.numBits, numWords << 6)) >= 0);
        }

        /// <summary>
        /// Ensure that the long[] is big enough to hold numBits, expanding it if
        /// necessary.
        /// </summary>
        public virtual void EnsureCapacity(long numberOfBits)
        {
            EnsureCapacityWords(Bits2Words(numberOfBits));
            // ensureCapacityWords sets numBits to a multiple of 64, but we want to set
            // it to exactly what the app asked.
            Debug.Assert((this.numBits = Math.Max(this.numBits, numberOfBits)) >= 0);
        }

        /// <summary>
        /// Lowers numWords, the number of words in use,
        /// by checking for trailing zero words.
        /// </summary>
        public virtual void TrimTrailingZeros()
        {
            int idx = wordLength - 1;
            while (idx >= 0 && bits[idx] == 0)
            {
                idx--;
            }
            wordLength = idx + 1;
        }

        /// <summary>
        /// returns the number of 64 bit words it would take to hold numBits </summary>
        public static int Bits2Words(long numBits)
        {
            return (((int)((uint)(numBits - 1) >> 6)) + 1);
        }

        /// <summary>
        /// returns true if both sets have the same bits set </summary>
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
            if (b.wordLength > this.wordLength)
            {
                a = b;
                b = this;
            }
            else
            {
                a = this;
            }

            // check for any set bits out of the range of b
            for (int i = a.wordLength - 1; i >= b.wordLength; i--)
            {
                if (a.bits[i] != 0)
                {
                    return false;
                }
            }

            for (int i = b.wordLength - 1; i >= 0; i--)
            {
                if (a.bits[i] != b.bits[i])
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
            // ReSharper disable NonReadonlyFieldInGetHashCode
            for (int i = bits.Length; --i >= 0; )
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