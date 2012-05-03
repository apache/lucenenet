/**
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

using System;
using System.Diagnostics;
using Lucene.Net.Search;
using Lucene.Net.Util;

namespace Lucene.Net.Spatial.Util
{
	/** BitSet of fixed length (numBits), backed by accessible
 *  ({@link #getBits}) long[], accessed with an int index,
 *  implementing Bits and DocIdSet.  Unlike {@link
 *  OpenBitSet} this bit set does not auto-expand, cannot
 *  handle long index, and does not have fastXX/XX variants
 *  (just X).
 *
 * @lucene.internal
 **/
	internal class FixedBitSet : Bits
	{
		private readonly long[] bits;
		private readonly int numBits;

		/** returns the number of 64 bit words it would take to hold numBits */
		public static int bits2words(int numBits)
		{
			var numLong = (int)((uint)numBits >> 6);
			if ((numBits & 63) != 0)
			{
				numLong++;
			}
			return numLong;
		}

		public FixedBitSet(int numBits)
		{
			this.numBits = numBits;
			bits = new long[bits2words(numBits)];
		}

		/** Makes full copy. */
		public FixedBitSet(FixedBitSet other)
		{
			bits = new long[other.bits.Length];
			Array.Copy(other.bits, 0, bits, 0, bits.Length);
			numBits = other.numBits;
		}

		public Bits Bits()
		{
			return this;
		}

		public override int Length()
		{
			return numBits;
		}

		public bool IsCacheable()
		{
			return true;
		}

		/** Expert. */
		public long[] GetBits()
		{
			return bits;
		}

		/** Returns number of set bits.  NOTE: this visits every
		 *  long in the backing bits array, and the result is not
		 *  internally cached! */
		public int Cardinality()
		{
			return (int)BitUtil.Pop_array(bits, 0, bits.Length);
		}

		public override bool Get(int index)
		{
			Debug.Assert(index >= 0 && index < numBits /*: "index=" + index*/);
			int i = index >> 6;               // div 64
			// signed shift will keep a negative index and force an
			// array-index-out-of-bounds-exception, removing the need for an explicit check.
			int bit = index & 0x3f;           // mod 64
			long bitmask = 1L << bit;
			return (bits[i] & bitmask) != 0;
		}

		public void Set(int index)
		{
			Debug.Assert(index >= 0 && index < numBits);
			int wordNum = index >> 6;      // div 64
			int bit = index & 0x3f;     // mod 64
			long bitmask = 1L << bit;
			bits[wordNum] |= bitmask;
		}

		public bool GetAndSet(int index)
		{
			Debug.Assert(index >= 0 && index < numBits);
			int wordNum = index >> 6;      // div 64
			int bit = index & 0x3f;     // mod 64
			long bitmask = 1L << bit;
			bool val = (bits[wordNum] & bitmask) != 0;
			bits[wordNum] |= bitmask;
			return val;
		}

		public void Clear(int index)
		{
			Debug.Assert(index >= 0 && index < numBits);
			int wordNum = index >> 6;
			int bit = index & 0x03f;
			long bitmask = 1L << bit;
			bits[wordNum] &= ~bitmask;
		}

		public bool GetAndClear(int index)
		{
			Debug.Assert(index >= 0 && index < numBits);
			int wordNum = index >> 6;      // div 64
			int bit = index & 0x3f;     // mod 64
			long bitmask = 1L << bit;
			bool val = (bits[wordNum] & bitmask) != 0;
			bits[wordNum] &= ~bitmask;
			return val;
		}

		/** Returns the index of the first set bit starting at the index specified.
		 *  -1 is returned if there are no more set bits.
		 */
		public int NextSetBit(int index)
		{
			Debug.Assert(index >= 0 && index < numBits);
			int i = index >> 6;
			int subIndex = index & 0x3f;      // index within the word
			long word = bits[i] >> subIndex;  // skip all the bits to the right of index

			if (word != 0)
			{
				return (i << 6) + subIndex + BitUtil.Ntz(word);
			}

			while (++i < bits.Length)
			{
				word = bits[i];
				if (word != 0)
				{
					return (i << 6) + BitUtil.Ntz(word);
				}
			}

			return -1;
		}

		/** Returns the index of the last set bit before or on the index specified.
		 *  -1 is returned if there are no more set bits.
		 */
		public int PrevSetBit(int index)
		{
			Debug.Assert(index >= 0 && index < numBits/*: "index=" + index + " numBits=" + numBits*/);
			int i = index >> 6;
			int subIndex = index & 0x3f;  // index within the word
			long word = (bits[i] << (63 - subIndex));  // skip all the bits to the left of index

			if (word != 0)
			{
				return (i << 6) + subIndex - CompatibilityExtensions.BitUtilNlz(word); // See LUCENE-3197
			}

			while (--i >= 0)
			{
				word = bits[i];
				if (word != 0)
				{
					return (i << 6) + 63 - CompatibilityExtensions.BitUtilNlz(word);
				}
			}

			return -1;
		}

		/** Does in-place OR of the bits provided by the
		 *  iterator. */
		public void Or(DocIdSetIterator iter)
		{
			if (iter is OpenBitSetIterator && iter.DocID() == -1)
			{
				var obs = (OpenBitSetIterator)iter;
				Or(obs.arr, obs.words);
				// advance after last doc that would be accepted if standard
				// iteration is used (to exhaust it):
				obs.Advance(numBits);
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

		/** this = this OR other */
		public void Or(FixedBitSet other)
		{
			Or(other.bits, other.bits.Length);
		}

		private void Or(long[] otherArr, int otherLen)
		{
			long[] thisArr = this.bits;
			int pos = Math.Min(thisArr.Length, otherLen);
			while (--pos >= 0)
			{
				thisArr[pos] |= otherArr[pos];
			}
		}

		/** Does in-place AND of the bits provided by the
		 *  iterator. */
		public void And(DocIdSetIterator iter)
		{
			if (iter is OpenBitSetIterator && iter.DocID() == -1)
			{
				var obs = (OpenBitSetIterator)iter;
				And(obs.arr, obs.words);
				// advance after last doc that would be accepted if standard
				// iteration is used (to exhaust it):
				obs.Advance(numBits);
			}
			else
			{
				if (numBits == 0) return;
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

		/** this = this AND other */
		public void And(FixedBitSet other)
		{
			And(other.bits, other.bits.Length);
		}

		private void And(long[] otherArr, int otherLen)
		{
			long[] thisArr = this.bits;
			int pos = Math.Min(thisArr.Length, otherLen);
			while (--pos >= 0)
			{
				thisArr[pos] &= otherArr[pos];
			}
			if (thisArr.Length > otherLen)
			{
				Arrays.Fill(thisArr, otherLen, thisArr.Length, 0L);
			}
		}

		/** Does in-place AND NOT of the bits provided by the
		 *  iterator. */
		public void AndNot(DocIdSetIterator iter)
		{
			var obs = iter as OpenBitSetIterator;
			if (obs != null && iter.DocID() == -1)
			{
				AndNot(obs.arr, obs.words);
				// advance after last doc that would be accepted if standard
				// iteration is used (to exhaust it):
				obs.Advance(numBits);
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

		/** this = this AND NOT other */
		public void AndNot(FixedBitSet other)
		{
			AndNot(other.bits, other.bits.Length);
		}

		private void AndNot(long[] otherArr, int otherLen)
		{
			long[] thisArr = this.bits;
			int pos = Math.Min(thisArr.Length, otherLen);
			while (--pos >= 0)
			{
				thisArr[pos] &= ~otherArr[pos];
			}
		}

		// NOTE: no .isEmpty() here because that's trappy (ie,
		// typically isEmpty is low cost, but this one wouldn't
		// be)

		/** Flips a range of bits
		 *
		 * @param startIndex lower index
		 * @param endIndex one-past the last bit to flip
		 */
		public void Flip(int startIndex, int endIndex) {
    Debug.Assert(startIndex >= 0 && startIndex < numBits);
    Debug.Assert(endIndex >= 0 && endIndex <= numBits);
    if (endIndex <= startIndex) {
      return;
    }

    int startWord = startIndex >> 6;
    int endWord = (endIndex-1) >> 6;

    /*** Grrr, java shifting wraps around so -1L>>>64 == -1
     * for that reason, make sure not to use endmask if the bits to flip will
     * be zero in the last word (redefine endWord to be the last changed...)
    long startmask = -1L << (startIndex & 0x3f);     // example: 11111...111000
    long endmask = -1L >>> (64-(endIndex & 0x3f));   // example: 00111...111111
    ***/

    long startmask = -1L << startIndex;
    long endmask =  -1L >>> -endIndex;  // 64-(endIndex&0x3f) is the same as -endIndex due to wrap

    if (startWord == endWord) {
      bits[startWord] ^= (startmask & endmask);
      return;
    }

    bits[startWord] ^= startmask;

    for (var i=startWord+1; i<endWord; i++) {
      bits[i] = ~bits[i];
    }

    bits[endWord] ^= endmask;
  }

		/** Sets a range of bits
		 *
		 * @param startIndex lower index
		 * @param endIndex one-past the last bit to set
		 */
		public void Set(int startIndex, int endIndex) {
    Debug.Assert(startIndex >= 0 && startIndex < numBits);
    Debug.Assert(endIndex >= 0 && endIndex <= numBits);
    if (endIndex <= startIndex) {
      return;
    }

    int startWord = startIndex >> 6;
    int endWord = (endIndex-1) >> 6;

    long startmask = -1L << startIndex;
    long endmask = -1L >>> -endIndex;  // 64-(endIndex&0x3f) is the same as -endIndex due to wrap

    if (startWord == endWord) {
      bits[startWord] |= (startmask & endmask);
      return;
    }

    bits[startWord] |= startmask;
    Arrays.Fill(bits, startWord+1, endWord, -1L);
    bits[endWord] |= endmask;
  }

		/** Clears a range of bits.
		 *
		 * @param startIndex lower index
		 * @param endIndex one-past the last bit to clear
		 */
		public void Clear(int startIndex, int endIndex) {
    Debug.Assert(startIndex >= 0 && startIndex < numBits);
    Debug.Assert(endIndex >= 0 && endIndex <= numBits);
    if (endIndex <= startIndex) {
      return;
    }

    int startWord = startIndex >> 6;
    int endWord = (endIndex-1) >> 6;

    long startmask = -1L << startIndex;
    long endmask = -1L >>> -endIndex;  // 64-(endIndex&0x3f) is the same as -endIndex due to wrap

    // invert masks since we are clearing
    startmask = ~startmask;
    endmask = ~endmask;

    if (startWord == endWord) {
      bits[startWord] &= (startmask | endmask);
      return;
    }

    bits[startWord] &= startmask;
    Arrays.Fill(bits, startWord+1, endWord, 0L);
    bits[endWord] &= endmask;
  }

		//@Override
		public FixedBitSet Clone()
		{
			return new FixedBitSet(this);
		}

		/** returns true if both sets have the same bits set */
		public override bool Equals(Object o)
		{
			if (this == o)
			{
				return true;
			}

			var other = o as FixedBitSet;
			if (other == null)
			{
				return false;
			}

			if (numBits != other.Length())
			{
				return false;
			}
			return bits.Equals(other.bits);
		}

		public override int GetHashCode()
		{
			long h = 0;
			for (var i = bits.Length; --i >= 0; )
			{
				h ^= bits[i];
				h = (h << 1) | ((uint)h >> 63); // rotate left
			}
			// fold leftmost bits into right and add a constant to prevent
			// empty sets from returning 0, which is too common.
			return (int)((h >> 32) ^ h) + 0x98761234;
		}

	}
}
