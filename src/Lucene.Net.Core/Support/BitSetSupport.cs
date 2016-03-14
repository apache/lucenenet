/*
 *
 * Licensed to the Apache Software Foundation (ASF) under one
 * or more contributor license agreements.  See the NOTICE file
 * distributed with this work for additional information
 * regarding copyright ownership.  The ASF licenses this file
 * to you under the Apache License, Version 2.0 (the
 * "License"); you may not use this file except in compliance
 * with the License.  You may obtain a copy of the License at
 *
 *   http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing,
 * software distributed under the License is distributed on an
 * "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
 * KIND, either express or implied.  See the License for the
 * specific language governing permissions and limitations
 * under the License.
 *
*/

using System;
using System.Collections;
using Lucene.Net.Util;

namespace Lucene.Net.Support
{
    /// <summary>
    /// This class provides supporting methods of java.util.BitSet
    /// that are not present in System.Collections.BitArray.
    /// </summary>
    public static class BitSetSupport
    {
        /// <summary>
        /// Returns the next set bit at or after index, or -1 if no such bit exists.
        /// </summary>
        /// <param name="bitArray"></param>
        /// <param name="index">the index of bit array at which to start checking</param>
        /// <returns>the next set bit or -1</returns>
        public static int NextSetBit(this BitArray bitArray, int index)
        {
            while (index < bitArray.Length)
            {
                // if index bit is set, return it
                // otherwise check next index bit
                if (bitArray.Get(index))
                    return index;
                else
                    index++;
            }
            // if no bits are set at or after index, return -1
            return -1;
        }

        public static int PrevSetBit(this BitArray bitArray, int index)
        {
            while (index >= 0 && index < bitArray.Length)
            {
                // if index bit is set, return it
                // otherwise check previous index bit
                if (bitArray.SafeGet(index))
                    return index;
                index--;
            }
            // if no bits are set at or before index, return -1
            return -1;
        }

        // Produces a bitwise-and of the two BitArrays without requiring they be the same length
        public static BitArray And_UnequalLengths(this BitArray bitsA, BitArray bitsB)
        {
            //Cycle only through fewest bits neccessary without requiring size equality
            var maxIdx = Math.Min(bitsA.Length, bitsB.Length);//exclusive
            var bits = new BitArray(maxIdx);
            for (int i = 0; i < maxIdx; i++)
            {
                bits[i] = bitsA[i] & bitsB[i];
            }
            return bits;
        }

        // Produces a bitwise-or of the two BitArrays without requiring they be the same length
        public static BitArray Or_UnequalLengths(this BitArray bitsA, BitArray bitsB)
        {
            var shorter = bitsA.Length < bitsB.Length ? bitsA : bitsB;
            var longer = bitsA.Length >= bitsB.Length ? bitsA : bitsB;
            var bits = new BitArray(longer.Length);
            for (int i = 0; i < longer.Length; i++)
            {
                if (i >= shorter.Length)
                {
                    bits[i] = longer[i];
                }
                else
                {
                    bits[i] = shorter[i] | longer[i];
                }
            }

            return bits;
        }

        // Produces a bitwise-xor of the two BitArrays without requiring they be the same length
        public static BitArray Xor_UnequalLengths(this BitArray bitsA, BitArray bitsB)
        {
            var shorter = bitsA.Length < bitsB.Length ? bitsA : bitsB;
            var longer = bitsA.Length >= bitsB.Length ? bitsA : bitsB;
            var bits = new BitArray(longer.Length);
            for (int i = 0; i < longer.Length; i++)
            {
                if (i >= shorter.Length)
                {
                    bits[i] = longer[i];
                }
                else
                {
                    bits[i] = shorter[i] ^ longer[i];
                }
            }

            return bits;
        }

        /// <summary>
        /// Returns the next un-set bit at or after index, or -1 if no such bit exists.
        /// </summary>
        /// <param name="bitArray"></param>
        /// <param name="index">the index of bit array at which to start checking</param>
        /// <returns>the next set bit or -1</returns>
        public static int NextClearBit(this BitArray bitArray, int index)
        {
            while (index < bitArray.Length)
            {
                // if index bit is not set, return it
                // otherwise check next index bit
                if (!bitArray.Get(index))
                    return index;
                else
                    index++;
            }
            // if no bits are set at or after index, return -1
            return -1;
        }

        /// <summary>
        /// Returns the number of bits set to true in this BitSet.
        /// </summary>
        /// <param name="bits">The BitArray object.</param>
        /// <returns>The number of bits set to true in this BitSet.</returns>
        public static int Cardinality(this BitArray bits)
        {
            int count = 0;
            for (int i = 0; i < bits.Length; i++)
            {
                if (bits[i])
                    count++;
            }
            return count;
        }

        /// <summary>
        /// Sets the bit at the given <paramref name="index"/> to true.
        /// </summary>
        /// <param name="bits">The BitArray object.</param>
        /// <param name="index">The position to set to true.</param>
        public static void Set(this BitArray bits, int index)
        {
            bits.SafeSet(index, true);
        }

        /// <summary>
        /// Sets the bit at the given <paramref name="index"/> to true.
        /// </summary>
        /// <param name="bits">The BitArray object.</param>
        /// <param name="fromIndex">The start of the range to set(inclusive)</param>
        /// <param name="toIndex">The end of the range to set(exclusive)</param>
        /// <param name="value">the value to set to the range</param>
        public static void Set(this BitArray bits, int fromIndex, int toIndex, bool value)
        {
            for (int i = fromIndex; i < toIndex; ++i)
            {
                bits.SafeSet(i, value);
            }
        }

        /// <summary>
        /// Sets the bit at the given <paramref name="index"/> to false.
        /// </summary>
        /// <param name="bits">The BitArray object.</param>
        /// <param name="index">The position to set to false.</param>
        public static void Clear(this BitArray bits, int index)
        {
            bits.SafeSet(index, false);
        }

        /// <summary>
        /// Sets all bits to false
        /// </summary>
        /// <param name="bits">The BitArray object.</param>
        public static void Clear(this BitArray bits)
        {
            bits.SetAll(false);
        }

        //Flip all bits in the desired range, startIdx inclusive to endIdx exclusive
        public static void Flip(this BitArray bits, int startIdx, int endIdx)
        {
            for (int i = startIdx; i < endIdx; i++)
            {
                bits[i] = !bits[i];
            }
        }

        // Sets all bits in the range to false [startIdx, endIdx)
        public static void Clear(this BitArray bits, int startIdx, int endIdx)
        {
            for (int i = startIdx; i < endIdx; i++)
            {
                bits[i] = false;
            }
        }

        // Sets all bits in the range to true [startIdx, endIdx)
        public static void Set(this BitArray bits, int startIdx, int endIdx)
        {
            for (int i = startIdx; i < endIdx; i++)
            {
                bits[i] = true;
            }
        }

        // Emulates the Java BitSet.Get() method.
        // Prevents exceptions from being thrown when the index is too high.
        public static bool SafeGet(this BitArray a, int loc)
        {
            return loc < a.Length && a.Get(loc);
        }

        //Emulates the Java BitSet.Set() method. Required to reconcile differences between Java BitSet and C# BitArray
        public static void SafeSet(this BitArray a, int loc, bool value)
        {
            if (loc >= a.Length)
                a.Length = loc + 1;

            a.Set(loc, value);
        }

        // Clears all bits in this BitArray that correspond to a set bit in the parameter BitArray
        public static void AndNot(this BitArray bitsA, BitArray bitsB)
        {
            //Debug.Assert(bitsA.Length == bitsB.Length, "BitArray lengths are not the same");
            for (int i = 0; i < bitsA.Length; i++)
            {
                //bitsA was longer than bitsB
                if (i >= bitsB.Length)
                {
                    return;
                }
                if (bitsA[i] && bitsB[i])
                {
                    bitsA[i] = false;
                }
            }
        }

        //Does a deep comparison of two BitArrays
        public static bool BitWiseEquals(this BitArray bitsA, BitArray bitsB)
        {
            if (bitsA == bitsB)
                return true;
            if (bitsA.Length != bitsB.Length)
                return false;

            for (int i = 0; i < bitsA.Length; i++)
            {
                if (bitsA[i] != bitsB[i])
                    return false;
            }

            return true;
        }

        //Compares a BitArray with an OpenBitSet
        public static bool Equal(this BitArray a, OpenBitSet b)
        {
            var bitArrayCardinality = a.Cardinality();
            if (bitArrayCardinality != b.Cardinality())
                return false;

            for (int i = 0; i < bitArrayCardinality; i++)
            {
                if (a.SafeGet(i) != b.Get(i))
                    return false;
            }

            return true;
        }
    }
}