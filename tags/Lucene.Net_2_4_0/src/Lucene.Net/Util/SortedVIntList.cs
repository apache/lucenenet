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

using BitArray = System.Collections.BitArray;
using DocIdSet = Lucene.Net.Search.DocIdSet;
using DocIdSetIterator = Lucene.Net.Search.DocIdSetIterator;

namespace Lucene.Net.Util
{
    /**
     *  Store and iterate sorted integers in compressed form in RAM.
     *  <br>The code for compressing the differences between ascending integers was
     *  borrowed from {@link org.apache.lucene.store.IndexInput} and
     *  {@link org.apache.lucene.store.IndexOutput}.
     */
    public class SortedVIntList : DocIdSet
    {

        /** When a BitSet has fewer than 1 in BITS2VINTLIST_SIZE bits set,
        * a SortedVIntList representing the index numbers of the set bits
        * will be smaller than that BitSet.
        */
        protected internal static readonly int BITS2VINTLIST_SIZE = 8;

        private int size;
        private byte[] bytes;
        private int lastBytePos;

        /**
         *  Create a SortedVIntList from all elements of an array of integers.
         *
         * @param  sortedInts  A sorted array of non negative integers.
         */
        public SortedVIntList(int[] sortedInts)
            :
          this(sortedInts, sortedInts.Length)
        {
        }

        /**
         * Create a SortedVIntList from an array of integers.
         * @param  sortedInts  An array of sorted non negative integers.
         * @param  inputSize   The number of integers to be used from the array.
         */
        public SortedVIntList(int[] sortedInts, int inputSize)
        {
            SortedVIntListBuilder builder = new SortedVIntListBuilder(this);
            for (int i = 0; i < inputSize; i++)
            {
                builder.AddInt(sortedInts[i]);
            }
            builder.Done();
        }

        /**
         * Create a SortedVIntList from a BitSet.
         * @param  bits  A bit set representing a set of integers.
         */
        public SortedVIntList(SupportClass.CollectionsSupport.BitSet bits)
        {
            SortedVIntListBuilder builder = new SortedVIntListBuilder(this);
            int nextInt = bits.NextSetBit(0);
            while (nextInt != -1)
            {
                builder.AddInt(nextInt);
                nextInt = bits.NextSetBit(nextInt + 1);
            }
            builder.Done();
        }

        /**
         * Create a SortedVIntList from an OpenBitSet.
         * @param  bits  A bit set representing a set of integers.
         */
        public SortedVIntList(OpenBitSet bits)
        {
            SortedVIntListBuilder builder = new SortedVIntListBuilder(this);
            int nextInt = bits.NextSetBit(0);
            while (nextInt != -1)
            {
                builder.AddInt(nextInt);
                nextInt = bits.NextSetBit(nextInt + 1);
            }
            builder.Done();
        }

        /**
         * Create a SortedVIntList.
         * @param  docIdSetIterator  An iterator providing document numbers as a set of integers.
         *                  This DocIdSetIterator is iterated completely when this constructor
         *                  is called and it must provide the integers in non
         *                  decreasing order.
         */
        public SortedVIntList(DocIdSetIterator docIdSetIterator)
        {
            SortedVIntListBuilder builder = new SortedVIntListBuilder(this);
            while (docIdSetIterator.Next())
            {
                builder.AddInt(docIdSetIterator.Doc());
            }
            builder.Done();
        }



        private void InitBytes()
        {
            size = 0;
            bytes = new byte[128]; // initial byte size
            lastBytePos = 0;
        }

        private void ResizeBytes(int newSize)
        {
            if (newSize != bytes.Length)
            {
                byte[] newBytes = new byte[newSize];
                System.Array.Copy(bytes, 0, newBytes, 0, lastBytePos);
                bytes = newBytes;
            }
        }

        private static readonly int VB1 = 0x7F;
        private static readonly int BIT_SHIFT = 7;
        private readonly int MAX_BYTES_PER_INT = (31 / BIT_SHIFT) + 1;

        /**
         * @return    The total number of sorted integers.
         */
        public int Size()
        {
            return size;
        }

        /**
         * @return The size of the byte array storing the compressed sorted integers.
         */
        public int GetByteSize()
        {
            return bytes.Length;
        }

        /**
         * @return    An iterator over the sorted integers.
         */
        public override DocIdSetIterator Iterator()
        {
            return new AnonymousDocIdSetIterator(this);
        }

        private class AnonymousDocIdSetIterator : DocIdSetIterator
        {
            private SortedVIntList enclosingInstance;
            internal AnonymousDocIdSetIterator(SortedVIntList enclosingInstance)
            {
                this.enclosingInstance = enclosingInstance;
            }

            int bytePos = 0;
            int lastInt = 0;

            private void Advance()
            {
                // See org.apache.lucene.store.IndexInput.readVInt()
                byte b = enclosingInstance.bytes[bytePos++];
                lastInt += b & VB1;
                for (int s = BIT_SHIFT; (b & ~VB1) != 0; s += BIT_SHIFT)
                {
                    b = enclosingInstance.bytes[bytePos++];
                    lastInt += (b & VB1) << s;
                }
            }

            public override int Doc() { return lastInt; }

            public override bool Next()
            {
                if (bytePos >= enclosingInstance.lastBytePos)
                {
                    return false;
                }
                else
                {
                    Advance();
                    return true;
                }
            }

            public override bool SkipTo(int docNr)
            {
                while (bytePos < enclosingInstance.lastBytePos)
                {
                    Advance();
                    if (lastInt >= docNr)
                    { // No skipping to docNr available.
                        return true;
                    }
                }
                return false;
            }
        }

        //private class SortedVIntListBuilder {
        internal class SortedVIntListBuilder
        {
            private int lastInt = 0;

            private SortedVIntList enclosingInstance;
            internal SortedVIntListBuilder(SortedVIntList enclosingInstance)
            {
                this.enclosingInstance = enclosingInstance;
                enclosingInstance.InitBytes();
                lastInt = 0;
            }

            internal void AddInt(int nextInt)
            {
                int diff = nextInt - lastInt;
                if (diff < 0)
                {
                    throw new System.ArgumentException(
                        "Input not sorted or first element negative.");
                }

                if ((enclosingInstance.lastBytePos + enclosingInstance.MAX_BYTES_PER_INT) > enclosingInstance.bytes.Length)
                {
                    // biggest possible int does not fit
                    enclosingInstance.ResizeBytes((enclosingInstance.bytes.Length * 2) + enclosingInstance.MAX_BYTES_PER_INT);
                }

                // See org.apache.lucene.store.IndexOutput.writeVInt()
                while ((diff & ~VB1) != 0)
                { // The high bit of the next byte needs to be set.
                    enclosingInstance.bytes[enclosingInstance.lastBytePos++] = (byte)((diff & VB1) | ~VB1);
                    //{DOUG-2.4.0: mod'd to do logical right shift (>>>); have to use unsigned types and >>
                    //diff >>>= BIT_SHIFT;
                    diff = (int)((uint)diff >> BIT_SHIFT);
                }
                enclosingInstance.bytes[enclosingInstance.lastBytePos++] = (byte)diff; // Last byte, high bit not set.
                enclosingInstance.size++;
                lastInt = nextInt;
            }

            internal void Done()
            {
                enclosingInstance.ResizeBytes(enclosingInstance.lastBytePos);
            }
        }

    }
}