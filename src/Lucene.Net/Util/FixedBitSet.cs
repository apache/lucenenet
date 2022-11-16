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
    /// BitSet of fixed length (numBits), backed by accessible (<see cref="GetBits()"/>)
    /// long[], accessed with an int index, implementing <see cref="GetBits()"/> and
    /// <see cref="DocIdSet"/>. If you need to manage more than 2.1B bits, use
    /// <see cref="Int64BitSet"/>.
    /// <para/>
    /// @lucene.internal
    /// </summary>
    public sealed class FixedBitSet : DocIdSet, IBits
    {
        /// <summary>
        /// A <see cref="DocIdSetIterator"/> which iterates over set bits in a
        /// <see cref="FixedBitSet"/>.
        /// </summary>
        public sealed class FixedBitSetIterator : DocIdSetIterator
        {
            internal readonly int numBits, numWords;
            internal readonly long[] bits;
            internal int doc = -1;

            /// <summary>
            /// Creates an iterator over the given <see cref="FixedBitSet"/>. </summary>
            public FixedBitSetIterator(FixedBitSet bits)
                : this(bits.bits, bits.numBits, bits.numWords)
            {
            }

            /// <summary>
            /// Creates an iterator over the given array of bits. </summary>
            public FixedBitSetIterator(long[] bits, int numBits, int wordLength)
            {
                this.bits = bits;
                this.numBits = numBits;
                this.numWords = wordLength;
            }

            public override int NextDoc()
            {
                if (doc == NO_MORE_DOCS || ++doc >= numBits)
                {
                    return doc = NO_MORE_DOCS;
                }
                int i = doc >> 6;
                int subIndex = doc & 0x3f; // index within the word
                long word = bits[i] >> subIndex; // skip all the bits to the right of index

                if (word != 0)
                {
                    return doc = doc + word.TrailingZeroCount();
                }

                while (++i < numWords)
                {
                    word = bits[i];
                    if (word != 0)
                    {
                        return doc = (i << 6) + word.TrailingZeroCount();
                    }
                }

                return doc = NO_MORE_DOCS;
            }

            public override int DocID => doc;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public override long GetCost()
            {
                return numBits;
            }

            public override int Advance(int target)
            {
                if (doc == NO_MORE_DOCS || target >= numBits)
                {
                    return doc = NO_MORE_DOCS;
                }
                int i = target >> 6;
                int subIndex = target & 0x3f; // index within the word
                long word = bits[i] >> subIndex; // skip all the bits to the right of index

                if (word != 0)
                {
                    return doc = target + word.TrailingZeroCount();
                }

                while (++i < numWords)
                {
                    word = bits[i];
                    if (word != 0)
                    {
                        return doc = (i << 6) + word.TrailingZeroCount();
                    }
                }

                return doc = NO_MORE_DOCS;
            }
        }

        /// <summary>
        /// If the given <see cref="FixedBitSet"/> is large enough to hold <paramref name="numBits"/>,
        /// returns the given bits, otherwise returns a new <see cref="FixedBitSet"/> which
        /// can hold the requested number of bits.
        ///
        /// <para/>
        /// <b>NOTE:</b> the returned bitset reuses the underlying <see cref="T:long[]"/> of
        /// the given <paramref name="bits"/> if possible. Also, calling <see cref="Length"/> on the
        /// returned bits may return a value greater than <paramref name="numBits"/>.
        /// </summary>
        public static FixedBitSet EnsureCapacity(FixedBitSet bits, int numBits)
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
                return new FixedBitSet(arr, arr.Length << 6);
            }
        }

        /// <summary>
        /// Returns the number of 64 bit words it would take to hold <paramref name="numBits"/> </summary>
        public static int Bits2words(int numBits)
        {
            int numLong = numBits.TripleShift(6);
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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long IntersectionCount(FixedBitSet a, FixedBitSet b)
        {
            return BitUtil.Pop_Intersect(a.bits, b.bits, 0, Math.Min(a.numWords, b.numWords));
        }

        /// <summary>
        /// Returns the popcount or cardinality of the union of the two sets. Neither
        /// set is modified.
        /// </summary>
        public static long UnionCount(FixedBitSet a, FixedBitSet b)
        {
            long tot = BitUtil.Pop_Union(a.bits, b.bits, 0, Math.Min(a.numWords, b.numWords));
            if (a.numWords < b.numWords)
            {
                tot += BitUtil.Pop_Array(b.bits, a.numWords, b.numWords - a.numWords);
            }
            else if (a.numWords > b.numWords)
            {
                tot += BitUtil.Pop_Array(a.bits, b.numWords, a.numWords - b.numWords);
            }
            return tot;
        }

        /// <summary>
        /// Returns the popcount or cardinality of "a and not b" or
        /// "intersection(a, not(b))". Neither set is modified.
        /// </summary>
        public static long AndNotCount(FixedBitSet a, FixedBitSet b)
        {
            long tot = BitUtil.Pop_AndNot(a.bits, b.bits, 0, Math.Min(a.numWords, b.numWords));
            if (a.numWords > b.numWords)
            {
                tot += BitUtil.Pop_Array(a.bits, b.numWords, a.numWords - b.numWords);
            }
            return tot;
        }

        internal readonly long[] bits;
        internal readonly int numBits;
        internal readonly int numWords;

        public FixedBitSet(int numBits)
        {
            this.numBits = numBits;
            bits = new long[Bits2words(numBits)];
            numWords = bits.Length;
        }

        public FixedBitSet(long[] storedBits, int numBits)
        {
            this.numWords = Bits2words(numBits);
            if (numWords > storedBits.Length)
            {
                throw new ArgumentException("The given long array is too small  to hold " + numBits + " bits");
            }
            this.numBits = numBits;
            this.bits = storedBits;
        }

        public override DocIdSetIterator GetIterator()
        {
            return new FixedBitSetIterator(bits, numBits, numWords);
        }

        public override IBits Bits => this;

        public int Length => numBits;

        /// <summary>
        /// This DocIdSet implementation is cacheable. </summary>
        public override bool IsCacheable => true;

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
        /// <see cref="long"/> in the backing bits array, and the result is not
        /// internally cached!
        /// </summary>
        public int Cardinality
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (int)BitUtil.Pop_Array(bits, 0, bits.Length);
        }

        public bool Get(int index)
        {
            if (Debugging.AssertsEnabled) Debugging.Assert(index >= 0 && index < numBits, "index={0}, numBits={1}", index, numBits);
            int i = index >> 6; // div 64
            // signed shift will keep a negative index and force an
            // array-index-out-of-bounds-exception, removing the need for an explicit check.
            int bit = index & 0x3f; // mod 64
            long bitmask = 1L << bit;
            return (bits[i] & bitmask) != 0;
        }

        public void Set(int index)
        {
            if (Debugging.AssertsEnabled) Debugging.Assert(index >= 0 && index < numBits, "index={0}, numBits={1}", index, numBits);
            int wordNum = index >> 6; // div 64
            int bit = index & 0x3f; // mod 64
            long bitmask = 1L << bit;
            bits[wordNum] |= bitmask;
        }

        public bool GetAndSet(int index)
        {
            if (Debugging.AssertsEnabled) Debugging.Assert(index >= 0 && index < numBits, "index={0}, numBits={1}", index, numBits);
            int wordNum = index >> 6; // div 64
            int bit = index & 0x3f; // mod 64
            long bitmask = 1L << bit;
            bool val = (bits[wordNum] & bitmask) != 0;
            bits[wordNum] |= bitmask;
            return val;
        }

        public void Clear(int index)
        {
            if (Debugging.AssertsEnabled) Debugging.Assert(index >= 0 && index < numBits, "index={0}, numBits={1}", index, numBits);
            int wordNum = index >> 6;
            int bit = index & 0x03f;
            long bitmask = 1L << bit;
            bits[wordNum] &= ~bitmask;
        }

        public bool GetAndClear(int index)
        {
            if (Debugging.AssertsEnabled) Debugging.Assert(index >= 0 && index < numBits, "index={0}, numBits={1}", index, numBits);
            int wordNum = index >> 6; // div 64
            int bit = index & 0x3f; // mod 64
            long bitmask = 1L << bit;
            bool val = (bits[wordNum] & bitmask) != 0;
            bits[wordNum] &= ~bitmask;
            return val;
        }

        /// <summary>
        /// Returns the index of the first set bit starting at the index specified.
        /// -1 is returned if there are no more set bits.
        /// </summary>
        public int NextSetBit(int index)
        {
            if (Debugging.AssertsEnabled) Debugging.Assert(index >= 0 && index < numBits, "index={0}, numBits={1}", index, numBits);
            int i = index >> 6;
            int subIndex = index & 0x3f; // index within the word
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
        /// Returns the index of the last set bit before or on the index specified.
        /// -1 is returned if there are no more set bits.
        /// </summary>
        public int PrevSetBit(int index)
        {
            if (Debugging.AssertsEnabled) Debugging.Assert(index >= 0 && index < numBits, "index={0}, numBits={1}", index, numBits);
            int i = index >> 6;
            int subIndex = index & 0x3f; // index within the word
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
        /// Does in-place OR of the bits provided by the
        /// iterator.
        /// </summary>
        public void Or(DocIdSetIterator iter)
        {
            if (iter.DocID == -1 && iter is OpenBitSetIterator obs)
            {
                Or(obs.arr, obs.words);
                // advance after last doc that would be accepted if standard
                // iteration is used (to exhaust it):
                obs.Advance(numBits);
            }
            else if (iter.DocID == -1 && iter is FixedBitSetIterator fbs)
            {
                Or(fbs.bits, fbs.numWords);
                // advance after last doc that would be accepted if standard
                // iteration is used (to exhaust it):
                fbs.Advance(numBits);
            }
            else
            {
                int doc;
                while ((doc = iter.NextDoc()) < numBits)
                {
                    Set(doc);
                }
            }
        }

        /// <summary>
        /// this = this OR other </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Or(FixedBitSet other)
        {
            Or(other.bits, other.numWords);
        }

        private void Or(long[] otherArr, int otherNumWords)
        {
            if (Debugging.AssertsEnabled) Debugging.Assert(otherNumWords <= numWords, "numWords={0}, otherNumWords={1}", numWords, otherNumWords);
            long[] thisArr = this.bits;
            int pos = Math.Min(numWords, otherNumWords);
            while (--pos >= 0)
            {
                thisArr[pos] |= otherArr[pos];
            }
        }

        /// <summary>
        /// this = this XOR other </summary>
        public void Xor(FixedBitSet other)
        {
            if (Debugging.AssertsEnabled) Debugging.Assert(other.numWords <= numWords, "numWords={0}, other.numWords={1}", numWords, other.numWords);
            long[] thisBits = this.bits;
            long[] otherBits = other.bits;
            int pos = Math.Min(numWords, other.numWords);
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
            while ((doc = iter.NextDoc()) < numBits)
            {
                Flip(doc, doc + 1);
            }
        }

        /// <summary>
        /// Does in-place AND of the bits provided by the
        /// iterator.
        /// </summary>
        public void And(DocIdSetIterator iter)
        {
            if (iter.DocID == -1 && iter is OpenBitSetIterator obs)
            {
                And(obs.arr, obs.words);
                // advance after last doc that would be accepted if standard
                // iteration is used (to exhaust it):
                obs.Advance(numBits);
            }
            else if (iter.DocID == -1 && iter is FixedBitSetIterator fbs)
            {
                And(fbs.bits, fbs.numWords);
                // advance after last doc that would be accepted if standard
                // iteration is used (to exhaust it):
                fbs.Advance(numBits);
            }
            else
            {
                if (numBits == 0)
                {
                    return;
                }
                int disiDoc, bitSetDoc = NextSetBit(0);
                while (bitSetDoc != -1 && (disiDoc = iter.Advance(bitSetDoc)) < numBits)
                {
                    Clear(bitSetDoc, disiDoc);
                    disiDoc++;
                    bitSetDoc = (disiDoc < numBits) ? NextSetBit(disiDoc) : -1;
                }
                if (bitSetDoc != -1)
                {
                    Clear(bitSetDoc, numBits);
                }
            }
        }

        /// <summary>
        /// Returns true if the sets have any elements in common </summary>
        public bool Intersects(FixedBitSet other)
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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void And(FixedBitSet other)
        {
            And(other.bits, other.numWords);
        }

        private void And(long[] otherArr, int otherNumWords)
        {
            long[] thisArr = this.bits;
            int pos = Math.Min(this.numWords, otherNumWords);
            while (--pos >= 0)
            {
                thisArr[pos] &= otherArr[pos];
            }
            if (this.numWords > otherNumWords)
            {
                Arrays.Fill(thisArr, otherNumWords, this.numWords, 0L);
            }
        }

        /// <summary>
        /// Does in-place AND NOT of the bits provided by the
        /// iterator.
        /// </summary>
        public void AndNot(DocIdSetIterator iter)
        {
            if (iter.DocID == -1 && iter is OpenBitSetIterator obs)
            {
                AndNot(obs.arr, obs.words);
                // advance after last doc that would be accepted if standard
                // iteration is used (to exhaust it):
                obs.Advance(numBits);
            }
            else if (iter.DocID == -1 && iter is FixedBitSetIterator fbs)
            {
                AndNot(fbs.bits, fbs.numWords);
                // advance after last doc that would be accepted if standard
                // iteration is used (to exhaust it):
                fbs.Advance(numBits);
            }
            else
            {
                int doc;
                while ((doc = iter.NextDoc()) < numBits)
                {
                    Clear(doc);
                }
            }
        }

        /// <summary>
        /// this = this AND NOT other </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AndNot(FixedBitSet other)
        {
            AndNot(other.bits, other.bits.Length);
        }

        private void AndNot(long[] otherArr, int otherNumWords)
        {
            long[] thisArr = this.bits;
            int pos = Math.Min(this.numWords, otherNumWords);
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
        /// <param name="startIndex"> Lower index </param>
        /// <param name="endIndex"> One-past the last bit to flip </param>
        public void Flip(int startIndex, int endIndex)
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
            long endmask = (-1L).TripleShift(-endIndex); // 64-(endIndex&0x3f) is the same as -endIndex due to wrap

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
        public void Set(int startIndex, int endIndex)
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

            int startWord = startIndex >> 6;
            int endWord = (endIndex - 1) >> 6;

            long startmask = -1L << startIndex;
            long endmask = (-1L).TripleShift(-endIndex); // 64-(endIndex&0x3f) is the same as -endIndex due to wrap

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
        public void Clear(int startIndex, int endIndex) // LUCENENET TODO: API: Change this to use startIndex and length to match .NET
        {
            if (Debugging.AssertsEnabled)
            {
                Debugging.Assert(startIndex >= 0 && startIndex < numBits, "startIndex={0}, numBits={1}", startIndex, numBits);
                Debugging.Assert(endIndex >= 0 && endIndex <= numBits, "endIndex={0}, numBits={1}", endIndex, numBits);
            }
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public FixedBitSet Clone()
        {
            long[] bits = new long[this.bits.Length];
            Arrays.Copy(this.bits, 0, bits, 0, bits.Length);
            return new FixedBitSet(bits, numBits);
        }

        /// <summary>
        /// Returns <c>true</c> if both sets have the same bits set </summary>
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