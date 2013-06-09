using Lucene.Net.Search;
using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Util
{
    public sealed class FixedBitSet : DocIdSet, IBits
    {
        private readonly long[] bits;
        private readonly int numBits;
        private readonly int wordLength;

        public static int Bits2Words(int numBits)
        {
            int numLong = Number.URShift(numBits, 6);
            if ((numBits & 63) != 0)
            {
                numLong++;
            }
            return numLong;
        }

        public FixedBitSet(int numBits)
        {
            this.numBits = numBits;
            bits = new long[Bits2Words(numBits)];
            wordLength = bits.Length;
        }

        public FixedBitSet(long[] storedBits, int numBits)
        {
            this.wordLength = Bits2Words(numBits);
            if (wordLength > storedBits.Length)
            {
                throw new ArgumentException("The given long array is too small to hold " + numBits + " bits");
            }
            this.numBits = numBits;
            this.bits = storedBits;
        }

        public FixedBitSet(FixedBitSet other)
        {
            bits = new long[other.wordLength];
            Array.Copy(other.bits, 0, bits, 0, other.wordLength);
            numBits = other.numBits;
            wordLength = other.wordLength;
        }

        public override DocIdSetIterator Iterator()
        {
            return new OpenBitSetIterator(bits, wordLength);
        }

        public IBits Bits
        {
            get
            {
                return this;
            }
        }

        public int Length
        {
            get
            {
                return numBits;
            }
        }

        public override bool IsCacheable
        {
            get
            {
                return true;
            }
        }

        public long[] GetBits()
        {
            return bits;
        }

        public int Cardinality()
        {
            return (int)BitUtil.Pop_array(bits, 0, bits.Length);
        }

        public bool this[int index]
        {
            get
            {
                //assert index >= 0 && index < numBits: "index=" + index;
                int i = index >> 6;               // div 64
                // signed shift will keep a negative index and force an
                // array-index-out-of-bounds-exception, removing the need for an explicit check.
                int bit = index & 0x3f;           // mod 64
                long bitmask = 1L << bit;
                return (bits[i] & bitmask) != 0;
            }
        }

        public bool Set(int index)
        {
            //assert index >= 0 && index < numBits: "index=" + index + " numBits=" + numBits;
            int wordNum = index >> 6;      // div 64
            int bit = index & 0x3f;     // mod 64
            long bitmask = 1L << bit;
            bits[wordNum] |= bitmask;
        }

        public bool GetAndSet(int index)
        {
            //assert index >= 0 && index < numBits;
            int wordNum = index >> 6;      // div 64
            int bit = index & 0x3f;     // mod 64
            long bitmask = 1L << bit;
            bool val = (bits[wordNum] & bitmask) != 0;
            bits[wordNum] |= bitmask;
            return val;
        }

        public void Clear(int index)
        {
            //assert index >= 0 && index < numBits;
            int wordNum = index >> 6;
            int bit = index & 0x03f;
            long bitmask = 1L << bit;
            bits[wordNum] &= ~bitmask;
        }

        public bool GetAndClear(int index)
        {
            //assert index >= 0 && index < numBits;
            int wordNum = index >> 6;      // div 64
            int bit = index & 0x3f;     // mod 64
            long bitmask = 1L << bit;
            bool val = (bits[wordNum] & bitmask) != 0;
            bits[wordNum] &= ~bitmask;
            return val;
        }

        public int NextSetBit(int index)
        {
            //assert index >= 0 && index < numBits;
            int i = index >> 6;
            int subIndex = index & 0x3f;      // index within the word
            long word = bits[i] >> subIndex;  // skip all the bits to the right of index

            if (word != 0)
            {
                return (i << 6) + subIndex + Number.NumberOfTrailingZeros(word);
            }

            while (++i < wordLength)
            {
                word = bits[i];
                if (word != 0)
                {
                    return (i << 6) + Number.NumberOfTrailingZeros(word);
                }
            }

            return -1;
        }

        public int PrevBitSet(int index)
        {
            //assert index >= 0 && index < numBits: "index=" + index + " numBits=" + numBits;
            int i = index >> 6;
            int subIndex = index & 0x3f;  // index within the word
            long word = (bits[i] << (63 - subIndex));  // skip all the bits to the left of index

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

        public void Or(DocIdSetIterator iter)
        {
            if (iter is OpenBitSetIterator && iter.DocID() == -1)
            {
                OpenBitSetIterator obs = (OpenBitSetIterator)iter;
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

        public void Or(FixedBitSet other)
        {
            Or(other.bits, other.wordLength);
        }

        private void Or(long[] otherArr, int otherLen)
        {
            long[] thisArr = this.bits;
            int pos = Math.Min(wordLength, otherLen);
            while (--pos >= 0)
            {
                thisArr[pos] |= otherArr[pos];
            }
        }

        public void And(DocIdSetIterator iter)
        {
            if (iter is OpenBitSetIterator && iter.DocID() == -1)
            {
                OpenBitSetIterator obs = (OpenBitSetIterator)iter;
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

        public void And(FixedBitSet other)
        {
            And(other.bits, other.wordLength);
        }

        private void And(long[] otherArr, int otherLen)
        {
            long[] thisArr = this.bits;
            int pos = Math.Min(this.wordLength, otherLen);
            while (--pos >= 0)
            {
                thisArr[pos] &= otherArr[pos];
            }
            if (this.wordLength > otherLen)
            {
                Arrays.Fill(thisArr, otherLen, this.wordLength, 0L);
            }
        }

        public void AndNot(DocIdSetIterator iter)
        {
            if (iter is OpenBitSetIterator && iter.DocID() == -1)
            {
                OpenBitSetIterator obs = (OpenBitSetIterator)iter;
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

        public void AndNot(FixedBitSet other)
        {
            AndNot(other.bits, other.wordLength);
        }

        private void AndNot(long[] otherArr, int otherLen)
        {
            long[] thisArr = this.bits;
            int pos = Math.Min(this.wordLength, otherLen);
            while (--pos >= 0)
            {
                thisArr[pos] &= ~otherArr[pos];
            }
        }

        public void Flip(int startIndex, int endIndex)
        {
            //assert startIndex >= 0 && startIndex < numBits;
            //assert endIndex >= 0 && endIndex <= numBits;
            if (endIndex <= startIndex)
            {
                return;
            }

            int startWord = startIndex >> 6;
            int endWord = (endIndex - 1) >> 6;

            /*** Grrr, java shifting wraps around so -1L>>>64 == -1
             * for that reason, make sure not to use endmask if the bits to flip will
             * be zero in the last word (redefine endWord to be the last changed...)
            long startmask = -1L << (startIndex & 0x3f);     // example: 11111...111000
            long endmask = -1L >>> (64-(endIndex & 0x3f));   // example: 00111...111111
            ***/

            long startmask = -1L << startIndex;
            long endmask = Number.URShift(-1L, -endIndex);  // 64-(endIndex&0x3f) is the same as -endIndex due to wrap

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

        public void Set(int startIndex, int endIndex)
        {
            //assert startIndex >= 0 && startIndex < numBits;
            //assert endIndex >= 0 && endIndex <= numBits;
            if (endIndex <= startIndex)
            {
                return;
            }

            int startWord = startIndex >> 6;
            int endWord = (endIndex - 1) >> 6;

            long startmask = -1L << startIndex;
            long endmask = Number.URShift(-1L, -endIndex);  // 64-(endIndex&0x3f) is the same as -endIndex due to wrap

            if (startWord == endWord)
            {
                bits[startWord] |= (startmask & endmask);
                return;
            }

            bits[startWord] |= startmask;
            Arrays.Fill(bits, startWord + 1, endWord, -1L);
            bits[endWord] |= endmask;
        }

        public void Clear(int startIndex, int endIndex)
        {
            //assert startIndex >= 0 && startIndex < numBits;
            //assert endIndex >= 0 && endIndex <= numBits;
            if (endIndex <= startIndex)
            {
                return;
            }

            int startWord = startIndex >> 6;
            int endWord = (endIndex - 1) >> 6;

            long startmask = -1L << startIndex;
            long endmask = Number.URShift(-1L, -endIndex);  // 64-(endIndex&0x3f) is the same as -endIndex due to wrap

            // invert masks since we are clearing
            startmask = ~startmask;
            endmask = ~endmask;

            if (startWord == endWord)
            {
                bits[startWord] &= (startmask | endmask);
                return;
            }

            bits[startWord] &= startmask;
            Arrays.Fill(bits, startWord + 1, endWord, 0L);
            bits[endWord] &= endmask;
        }

        public /* override? */ FixedBitSet Clone()
        {
            return new FixedBitSet(this);
        }

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
            FixedBitSet other = (FixedBitSet)o;
            if (numBits != other.Length)
            {
                return false;
            }
            return Arrays.Equals(bits, other.bits);
        }

        public override int GetHashCode()
        {
            long h = 0;
            for (int i = wordLength; --i >= 0; )
            {
                h ^= bits[i];
                h = (h << 1) | Number.URShift(h, 63); // rotate left
            }
            // fold leftmost bits into right and add a constant to prevent
            // empty sets from returning 0, which is too common.
            return (int)(((h >> 32) ^ h) + 0x98761234);
        }
    }
}
