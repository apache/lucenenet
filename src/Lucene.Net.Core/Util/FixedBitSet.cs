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

    using DocIdSet = Lucene.Net.Search.DocIdSet;
    using DocIdSetIterator = Lucene.Net.Search.DocIdSetIterator;

    /// <summary>
    /// BitSet of fixed length (numBits), backed by accessible (<seealso cref="#getBits"/>)
    /// long[], accessed with an int index, implementing <seealso cref="GetBits"/> and
    /// <seealso cref="DocIdSet"/>. If you need to manage more than 2.1B bits, use
    /// <seealso cref="LongBitSet"/>.
    ///
    /// @lucene.internal
    /// </summary>
    public sealed class FixedBitSet : DocIdSet, IBits
    {
        /// <summary>
        /// A <seealso cref="DocIdSetIterator"/> which iterates over set bits in a
        /// <seealso cref="FixedBitSet"/>.
        /// </summary>
        public sealed class FixedBitSetIterator : DocIdSetIterator
        {
            internal readonly int NumBits, NumWords;
            internal readonly long[] bits;
            internal int Doc = -1;

            /// <summary>
            /// Creates an iterator over the given <seealso cref="FixedBitSet"/>. </summary>
            public FixedBitSetIterator(FixedBitSet bits)
                : this(bits.bits, bits.NumBits, bits.NumWords)
            {
            }

            /// <summary>
            /// Creates an iterator over the given array of bits. </summary>
            public FixedBitSetIterator(long[] bits, int numBits, int wordLength)
            {
                this.bits = bits;
                this.NumBits = numBits;
                this.NumWords = wordLength;
            }

            public override int NextDoc()
            {
                if (Doc == NO_MORE_DOCS || ++Doc >= NumBits)
                {
                    return Doc = NO_MORE_DOCS;
                }
                int i = Doc >> 6;
                int subIndex = Doc & 0x3f; // index within the word
                long word = bits[i] >> subIndex; // skip all the bits to the right of index

                if (word != 0)
                {
                    return Doc = Doc + Number.NumberOfTrailingZeros(word);
                }

                while (++i < NumWords)
                {
                    word = bits[i];
                    if (word != 0)
                    {
                        return Doc = (i << 6) + Number.NumberOfTrailingZeros(word);
                    }
                }

                return Doc = NO_MORE_DOCS;
            }

            public override int DocID
            {
                get { return Doc; }
            }

            public override long Cost()
            {
                return NumBits;
            }

            public override int Advance(int target)
            {
                if (Doc == NO_MORE_DOCS || target >= NumBits)
                {
                    return Doc = NO_MORE_DOCS;
                }
                int i = target >> 6;
                int subIndex = target & 0x3f; // index within the word
                long word = bits[i] >> subIndex; // skip all the bits to the right of index

                if (word != 0)
                {
                    return Doc = target + Number.NumberOfTrailingZeros(word);
                }

                while (++i < NumWords)
                {
                    word = bits[i];
                    if (word != 0)
                    {
                        return Doc = (i << 6) + Number.NumberOfTrailingZeros(word);
                    }
                }

                return Doc = NO_MORE_DOCS;
            }
        }

        /// <summary>
        /// If the given <seealso cref="FixedBitSet"/> is large enough to hold {@code numBits},
        /// returns the given bits, otherwise returns a new <seealso cref="FixedBitSet"/> which
        /// can hold the requested number of bits.
        ///
        /// <p>
        /// <b>NOTE:</b> the returned bitset reuses the underlying {@code long[]} of
        /// the given {@code bits} if possible. Also, calling <seealso cref="#length()"/> on the
        /// returned bits may return a value greater than {@code numBits}.
        /// </summary>
        public static FixedBitSet EnsureCapacity(FixedBitSet bits, int numBits)
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
                return new FixedBitSet(arr, arr.Length << 6);
            }
        }

        /// <summary>
        /// returns the number of 64 bit words it would take to hold numBits </summary>
        public static int Bits2words(int numBits)
        {
            int numLong = (int)((uint)numBits >> 6);
            if ((numBits & 63) != 0)
            {
                numLong++;
            }
            return numLong;
        }

        /// <summary>
        /// Returns the popcount or cardinality of the intersection of the two sets.
        /// Neither set is modified.
        /// </summary>
        public static long IntersectionCount(FixedBitSet a, FixedBitSet b)
        {
            return BitUtil.Pop_intersect(a.bits, b.bits, 0, Math.Min(a.NumWords, b.NumWords));
        }

        /// <summary>
        /// Returns the popcount or cardinality of the union of the two sets. Neither
        /// set is modified.
        /// </summary>
        public static long UnionCount(FixedBitSet a, FixedBitSet b)
        {
            long tot = BitUtil.Pop_union(a.bits, b.bits, 0, Math.Min(a.NumWords, b.NumWords));
            if (a.NumWords < b.NumWords)
            {
                tot += BitUtil.Pop_array(b.bits, a.NumWords, b.NumWords - a.NumWords);
            }
            else if (a.NumWords > b.NumWords)
            {
                tot += BitUtil.Pop_array(a.bits, b.NumWords, a.NumWords - b.NumWords);
            }
            return tot;
        }

        /// <summary>
        /// Returns the popcount or cardinality of "a and not b" or
        /// "intersection(a, not(b))". Neither set is modified.
        /// </summary>
        public static long AndNotCount(FixedBitSet a, FixedBitSet b)
        {
            long tot = BitUtil.Pop_andnot(a.bits, b.bits, 0, Math.Min(a.NumWords, b.NumWords));
            if (a.NumWords > b.NumWords)
            {
                tot += BitUtil.Pop_array(a.bits, b.NumWords, a.NumWords - b.NumWords);
            }
            return tot;
        }

        internal readonly long[] bits;
        internal readonly int NumBits;
        internal readonly int NumWords;

        public FixedBitSet(int numBits)
        {
            this.NumBits = numBits;
            bits = new long[Bits2words(numBits)];
            NumWords = bits.Length;
        }

        public FixedBitSet(long[] storedBits, int numBits)
        {
            this.NumWords = Bits2words(numBits);
            if (NumWords > storedBits.Length)
            {
                throw new System.ArgumentException("The given long array is too small  to hold " + numBits + " bits");
            }
            this.NumBits = numBits;
            this.bits = storedBits;
        }

        public override DocIdSetIterator GetIterator()
        {
            return new FixedBitSetIterator(bits, NumBits, NumWords);
        }

        public override IBits GetBits()
        {
            return this;
        }

        public int Length() // LUCENENET TODO: make property
        {
            return NumBits;
        }

        /// <summary>
        /// this DocIdSet implementation is cacheable. </summary>
        public override bool IsCacheable
        {
            get
            {
                return true;
            }
        }

        /// <summary>
        /// Expert. </summary>
        public long[] Bits // LUCENENET TODO: change to GetBits() (array)
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
        public int Cardinality()
        {
            return (int)BitUtil.Pop_array(bits, 0, bits.Length);
        }

        public bool Get(int index)
        {
            Debug.Assert(index >= 0 && index < NumBits, "index=" + index + ", numBits=" + NumBits);
            int i = index >> 6; // div 64
            // signed shift will keep a negative index and force an
            // array-index-out-of-bounds-exception, removing the need for an explicit check.
            int bit = index & 0x3f; // mod 64
            long bitmask = 1L << bit;
            return (bits[i] & bitmask) != 0;
        }

        public void Set(int index)
        {
            Debug.Assert(index >= 0 && index < NumBits, "index=" + index + ", numBits=" + NumBits);
            int wordNum = index >> 6; // div 64
            int bit = index & 0x3f; // mod 64
            long bitmask = 1L << bit;
            bits[wordNum] |= bitmask;
        }

        public bool GetAndSet(int index)
        {
            Debug.Assert(index >= 0 && index < NumBits);
            int wordNum = index >> 6; // div 64
            int bit = index & 0x3f; // mod 64
            long bitmask = 1L << bit;
            bool val = (bits[wordNum] & bitmask) != 0;
            bits[wordNum] |= bitmask;
            return val;
        }

        public void Clear(int index)
        {
            Debug.Assert(index >= 0 && index < NumBits);
            int wordNum = index >> 6;
            int bit = index & 0x03f;
            long bitmask = 1L << bit;
            bits[wordNum] &= ~bitmask;
        }

        public bool GetAndClear(int index)
        {
            Debug.Assert(index >= 0 && index < NumBits);
            int wordNum = index >> 6; // div 64
            int bit = index & 0x3f; // mod 64
            long bitmask = 1L << bit;
            bool val = (bits[wordNum] & bitmask) != 0;
            bits[wordNum] &= ~bitmask;
            return val;
        }

        /// <summary>
        /// Returns the index of the first set bit starting at the index specified.
        ///  -1 is returned if there are no more set bits.
        /// </summary>
        public int NextSetBit(int index)
        {
            Debug.Assert(index >= 0 && index < NumBits, "index=" + index + ", numBits=" + NumBits);
            int i = index >> 6;
            int subIndex = index & 0x3f; // index within the word
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
        public int PrevSetBit(int index)
        {
            Debug.Assert(index >= 0 && index < NumBits, "index=" + index + " numBits=" + NumBits);
            int i = index >> 6;
            int subIndex = index & 0x3f; // index within the word
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
        /// Does in-place OR of the bits provided by the
        ///  iterator.
        /// </summary>
        public void Or(DocIdSetIterator iter)
        {
            if (iter is OpenBitSetIterator && iter.DocID == -1)
            {
                OpenBitSetIterator obs = (OpenBitSetIterator)iter;
                Or(obs.Arr, obs.Words);
                // advance after last doc that would be accepted if standard
                // iteration is used (to exhaust it):
                obs.Advance(NumBits);
            }
            else if (iter is FixedBitSetIterator && iter.DocID == -1)
            {
                FixedBitSetIterator fbs = (FixedBitSetIterator)iter;
                Or(fbs.bits, fbs.NumWords);
                // advance after last doc that would be accepted if standard
                // iteration is used (to exhaust it):
                fbs.Advance(NumBits);
            }
            else
            {
                int doc;
                while ((doc = iter.NextDoc()) < NumBits)
                {
                    Set(doc);
                }
            }
        }

        /// <summary>
        /// this = this OR other </summary>
        public void Or(FixedBitSet other)
        {
            Or(other.bits, other.NumWords);
        }

        private void Or(long[] otherArr, int otherNumWords)
        {
            Debug.Assert(otherNumWords <= NumWords, "numWords=" + NumWords + ", otherNumWords=" + otherNumWords);
            long[] thisArr = this.bits;
            int pos = Math.Min(NumWords, otherNumWords);
            while (--pos >= 0)
            {
                thisArr[pos] |= otherArr[pos];
            }
        }

        /// <summary>
        /// this = this XOR other </summary>
        public void Xor(FixedBitSet other)
        {
            Debug.Assert(other.NumWords <= NumWords, "numWords=" + NumWords + ", other.numWords=" + other.NumWords);
            long[] thisBits = this.bits;
            long[] otherBits = other.bits;
            int pos = Math.Min(NumWords, other.NumWords);
            while (--pos >= 0)
            {
                thisBits[pos] ^= otherBits[pos];
            }
        }

        /// <summary>
        /// Does in-place XOR of the bits provided by the iterator. </summary>
        public void Xor(DocIdSetIterator iter)
        {
            int doc;
            while ((doc = iter.NextDoc()) < NumBits)
            {
                Flip(doc, doc + 1);
            }
        }

        /// <summary>
        /// Does in-place AND of the bits provided by the
        ///  iterator.
        /// </summary>
        public void And(DocIdSetIterator iter)
        {
            if (iter is OpenBitSetIterator && iter.DocID == -1)
            {
                OpenBitSetIterator obs = (OpenBitSetIterator)iter;
                And(obs.Arr, obs.Words);
                // advance after last doc that would be accepted if standard
                // iteration is used (to exhaust it):
                obs.Advance(NumBits);
            }
            else if (iter is FixedBitSetIterator && iter.DocID == -1)
            {
                FixedBitSetIterator fbs = (FixedBitSetIterator)iter;
                And(fbs.bits, fbs.NumWords);
                // advance after last doc that would be accepted if standard
                // iteration is used (to exhaust it):
                fbs.Advance(NumBits);
            }
            else
            {
                if (NumBits == 0)
                {
                    return;
                }
                int disiDoc, bitSetDoc = NextSetBit(0);
                while (bitSetDoc != -1 && (disiDoc = iter.Advance(bitSetDoc)) < NumBits)
                {
                    Clear(bitSetDoc, disiDoc);
                    disiDoc++;
                    bitSetDoc = (disiDoc < NumBits) ? NextSetBit(disiDoc) : -1;
                }
                if (bitSetDoc != -1)
                {
                    Clear(bitSetDoc, NumBits);
                }
            }
        }

        /// <summary>
        /// returns true if the sets have any elements in common </summary>
        public bool Intersects(FixedBitSet other)
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
        public void And(FixedBitSet other)
        {
            And(other.bits, other.NumWords);
        }

        private void And(long[] otherArr, int otherNumWords)
        {
            long[] thisArr = this.bits;
            int pos = Math.Min(this.NumWords, otherNumWords);
            while (--pos >= 0)
            {
                thisArr[pos] &= otherArr[pos];
            }
            if (this.NumWords > otherNumWords)
            {
                Arrays.Fill(thisArr, otherNumWords, this.NumWords, 0L);
            }
        }

        /// <summary>
        /// Does in-place AND NOT of the bits provided by the
        ///  iterator.
        /// </summary>
        public void AndNot(DocIdSetIterator iter)
        {
            if (iter is OpenBitSetIterator && iter.DocID == -1)
            {
                OpenBitSetIterator obs = (OpenBitSetIterator)iter;
                AndNot(obs.Arr, obs.Words);
                // advance after last doc that would be accepted if standard
                // iteration is used (to exhaust it):
                obs.Advance(NumBits);
            }
            else if (iter is FixedBitSetIterator && iter.DocID == -1)
            {
                FixedBitSetIterator fbs = (FixedBitSetIterator)iter;
                AndNot(fbs.bits, fbs.NumWords);
                // advance after last doc that would be accepted if standard
                // iteration is used (to exhaust it):
                fbs.Advance(NumBits);
            }
            else
            {
                int doc;
                while ((doc = iter.NextDoc()) < NumBits)
                {
                    Clear(doc);
                }
            }
        }

        /// <summary>
        /// this = this AND NOT other </summary>
        public void AndNot(FixedBitSet other)
        {
            AndNot(other.bits, other.bits.Length);
        }

        private void AndNot(long[] otherArr, int otherNumWords)
        {
            long[] thisArr = this.bits;
            int pos = Math.Min(this.NumWords, otherNumWords);
            while (--pos >= 0)
            {
                thisArr[pos] &= ~otherArr[pos];
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
        public void Flip(int startIndex, int endIndex)
        {
            Debug.Assert(startIndex >= 0 && startIndex < NumBits);
            Debug.Assert(endIndex >= 0 && endIndex <= NumBits);
            if (endIndex <= startIndex)
            {
                return;
            }

            int startWord = startIndex >> 6;
            int endWord = (endIndex - 1) >> 6;

            /*
            ///* Grrr, java shifting wraps around so -1L>>>64 == -1
            /// for that reason, make sure not to use endmask if the bits to flip will
            /// be zero in the last word (redefine endWord to be the last changed...)
            /// long startmask = -1L << (startIndex & 0x3f);     // example: 11111...111000
            /// long endmask = -1L >>> (64-(endIndex & 0x3f));   // example: 00111...111111
            /// **
            */

            long startmask = -1L << startIndex;
            long endmask = (long)(unchecked((ulong)-1) >> -endIndex);
            //long endmask = -(int)((uint)1L >> -endIndex); // 64-(endIndex&0x3f) is the same as -endIndex due to wrap

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
        public void Set(int startIndex, int endIndex)
        {
            Debug.Assert(startIndex >= 0 && startIndex < NumBits);
            Debug.Assert(endIndex >= 0 && endIndex <= NumBits);
            if (endIndex <= startIndex)
            {
                return;
            }

            int startWord = startIndex >> 6;
            int endWord = (endIndex - 1) >> 6;

            long startmask = -1L << startIndex;
            long endmask = (long)(unchecked((ulong)-1) >> -endIndex);
            //long endmask = -(int)((uint)1UL >> -endIndex); // 64-(endIndex&0x3f) is the same as -endIndex due to wrap

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
        public void Clear(int startIndex, int endIndex)
        {
            Debug.Assert(startIndex >= 0 && startIndex < NumBits, "startIndex=" + startIndex + ", numBits=" + NumBits);
            Debug.Assert(endIndex >= 0 && endIndex <= NumBits, "endIndex=" + endIndex + ", numBits=" + NumBits);
            if (endIndex <= startIndex)
            {
                return;
            }

            int startWord = startIndex >> 6;
            int endWord = (endIndex - 1) >> 6;

            long startmask = (-1L) << startIndex;  // -1 << (startIndex mod 64)
            long endmask = (-1L) << endIndex;      // -1 << (endIndex mod 64)
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

        public FixedBitSet Clone()
        {
            long[] bits = new long[this.bits.Length];
            Array.Copy(this.bits, 0, bits, 0, bits.Length);
            return new FixedBitSet(bits, NumBits);
        }

        /// <summary>
        /// returns true if both sets have the same bits set </summary>
        public override bool Equals(object o)
        {
            if (this == o)
            {
                return true;
            }
            if (!(o is FixedBitSet))
            {
                return false;
            }
            var other = (FixedBitSet)o;
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