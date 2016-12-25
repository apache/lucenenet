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
    using DocIdSetIterator = Lucene.Net.Search.DocIdSetIterator;

    /// <summary>
    /// An iterator to iterate over set bits in an OpenBitSet.
    /// this is faster than nextSetBit() for iterating over the complete set of bits,
    /// especially when the density of the bits set is high.
    /// </summary>
    public class OpenBitSetIterator : DocIdSetIterator
    {
        // hmmm, what about an iterator that finds zeros though,
        // or a reverse iterator... should they be separate classes
        // for efficiency, or have a common root interface?  (or
        // maybe both?  could ask for a SetBitsIterator, etc...

        internal readonly long[] Arr;
        internal readonly int Words;
        private int i = -1;
        private long Word;
        private int WordShift;
        private int IndexArray;
        private int CurDocId = -1;

        public OpenBitSetIterator(OpenBitSet obs)
            : this(obs.Bits, obs.NumWords)
        {
        }

        public OpenBitSetIterator(long[] bits, int numWords)
        {
            Arr = bits;
            Words = numWords;
        }

        // 64 bit shifts
        private void Shift()
        {
            if ((int)Word == 0)
            {
                WordShift += 32;
                Word = (long)((ulong)Word >> 32);
            }
            if ((Word & 0x0000FFFF) == 0)
            {
                WordShift += 16;
                Word = (long)((ulong)Word >> 16);
            }
            if ((Word & 0x000000FF) == 0)
            {
                WordShift += 8;
                Word = (long)((ulong)Word >> 8);
            }
            IndexArray = BitUtil.BitList((byte)Word);
        }

        /// <summary>
        ///*** alternate shift implementations
        /// // 32 bit shifts, but a long shift needed at the end
        /// private void shift2() {
        ///  int y = (int)word;
        ///  if (y==0) {wordShift +=32; y = (int)(word >>>32); }
        ///  if ((y & 0x0000FFFF) == 0) { wordShift +=16; y>>>=16; }
        ///  if ((y & 0x000000FF) == 0) { wordShift +=8; y>>>=8; }
        ///  indexArray = bitlist[y & 0xff];
        ///  word >>>= (wordShift +1);
        /// }
        ///
        /// private void shift3() {
        ///  int lower = (int)word;
        ///  int lowByte = lower & 0xff;
        ///  if (lowByte != 0) {
        ///    indexArray=bitlist[lowByte];
        ///    return;
        ///  }
        ///  shift();
        /// }
        /// *****
        /// </summary>

        public override int NextDoc()
        {
            if (IndexArray == 0)
            {
                if (Word != 0)
                {
                    Word = (long)((ulong)Word >> 8);
                    WordShift += 8;
                }

                while (Word == 0)
                {
                    if (++i >= Words)
                    {
                        return CurDocId = NO_MORE_DOCS;
                    }
                    Word = Arr[i];
                    WordShift = -1; // loop invariant code motion should move this
                }

                // after the first time, should I go with a linear search, or
                // stick with the binary search in shift?
                Shift();
            }

            int bitIndex = (IndexArray & 0x0f) + WordShift;
            IndexArray = (int)((uint)IndexArray >> 4);
            // should i<<6 be cached as a separate variable?
            // it would only save one cycle in the best circumstances.
            return CurDocId = (i << 6) + bitIndex;
        }

        public override int Advance(int target)
        {
            IndexArray = 0;
            i = target >> 6;
            if (i >= Words)
            {
                Word = 0; // setup so next() will also return -1
                return CurDocId = NO_MORE_DOCS;
            }
            WordShift = target & 0x3f;
            Word = (long)((ulong)Arr[i] >> WordShift);
            if (Word != 0)
            {
                WordShift--; // compensate for 1 based arrIndex
            }
            else
            {
                while (Word == 0)
                {
                    if (++i >= Words)
                    {
                        return CurDocId = NO_MORE_DOCS;
                    }
                    Word = Arr[i];
                }
                WordShift = -1;
            }

            Shift();

            int bitIndex = (IndexArray & 0x0f) + WordShift;
            IndexArray = (int)((uint)IndexArray >> 4);
            // should i<<6 be cached as a separate variable?
            // it would only save one cycle in the best circumstances.
            return CurDocId = (i << 6) + bitIndex;
        }

        public override int DocID
        {
            get { return CurDocId; }
        }

        public override long Cost()
        {
            return Words / 64;
        }
    }
}