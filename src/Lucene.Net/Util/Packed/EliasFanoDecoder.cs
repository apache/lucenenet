using J2N.Numerics;
using Lucene.Net.Diagnostics;
using System;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace Lucene.Net.Util.Packed
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
    /// A decoder for an <see cref="Packed.EliasFanoEncoder"/>.
    /// <para/>
    /// @lucene.internal
    /// </summary>
    public class EliasFanoDecoder
    {
        /// <summary>
        /// NOTE: This was LOG2_LONG_SIZE in Lucene.
        /// </summary>
        private static readonly int LOG2_INT64_SIZE = (sizeof(long) * 8).TrailingZeroCount();

        private readonly EliasFanoEncoder efEncoder;
        private readonly long numEncoded;
        private long efIndex = -1; // the decoding index.
        private long setBitForIndex = -1; // the index of the high bit at the decoding index.

        public const long NO_MORE_VALUES = -1L;

        private readonly long numIndexEntries;
        private readonly long indexMask;

        /// <summary>
        /// Construct a decoder for a given <see cref="Packed.EliasFanoEncoder"/>.
        /// The decoding index is set to just before the first encoded value.
        /// </summary>
        public EliasFanoDecoder(EliasFanoEncoder efEncoder)
        {
            this.efEncoder = efEncoder;
            this.numEncoded = efEncoder.numEncoded; // not final in EliasFanoEncoder
            this.numIndexEntries = efEncoder.currentEntryIndex; // not final in EliasFanoEncoder
            this.indexMask = (1L << efEncoder.nIndexEntryBits) - 1;
        }

        /// <returns> The Elias-Fano encoder that is decoded. </returns>
        public virtual EliasFanoEncoder EliasFanoEncoder => efEncoder;

        /// <summary>
        /// The number of values encoded by the encoder. </summary>
        /// <returns> The number of values encoded by the encoder. </returns>
        public virtual long NumEncoded => numEncoded;

        /// <summary>
        /// The current decoding index.
        /// The first value encoded by <see cref="EliasFanoEncoder.EncodeNext(long)"/> has index 0.
        /// Only valid directly after
        /// <see cref="NextValue()"/>, <see cref="AdvanceToValue(long)"/>,
        /// <see cref="PreviousValue()"/>, or <see cref="BackToValue(long)"/>
        /// returned another value than <see cref="NO_MORE_VALUES"/>,
        /// or <see cref="AdvanceToIndex(long)"/> returned <c>true</c>. </summary>
        /// <returns> The decoding index of the last decoded value, or as last set by <see cref="AdvanceToIndex(long)"/>. </returns>
        public virtual long CurrentIndex()
        {
            if (efIndex < 0)
            {
                throw IllegalStateException.Create("index before sequence");
            }
            if (efIndex >= numEncoded)
            {
                throw IllegalStateException.Create("index after sequence");
            }
            return efIndex;
        }

        /// <summary>
        /// The value at the current decoding index.
        /// Only valid when <see cref="CurrentIndex()"/> would return a valid result.
        /// <para/>
        /// This is only intended for use after <see cref="AdvanceToIndex(long)"/> returned <c>true</c>. </summary>
        /// <returns> The value encoded at <see cref="CurrentIndex()"/>. </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual long CurrentValue()
        {
            return CombineHighLowValues(CurrentHighValue(), CurrentLowValue());
        }

        ///  <returns> The high value for the current decoding index. </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private long CurrentHighValue()
        {
            return setBitForIndex - efIndex; // sequence of unary gaps
        }

        /// <summary>
        /// See also <see cref="EliasFanoEncoder.PackValue(long, long[], int, long)"/> </summary>
        private static long UnPackValue(long[] longArray, int numBits, long packIndex, long bitsMask)
        {
            if (numBits == 0)
            {
                return 0;
            }
            long bitPos = packIndex * numBits;
            int index = (int)(bitPos.TripleShift(LOG2_INT64_SIZE));
            int bitPosAtIndex = (int)(bitPos & ((sizeof(long) * 8) - 1));
            long value = longArray[index].TripleShift(bitPosAtIndex);
            if ((bitPosAtIndex + numBits) > (sizeof(long) * 8))
            {
                value |= (longArray[index + 1] << ((sizeof(long) * 8) - bitPosAtIndex));
            }
            value &= bitsMask;
            return value;
        }

        ///  <returns> The low value for the current decoding index. </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private long CurrentLowValue()
        {
            if (Debugging.AssertsEnabled) Debugging.Assert(((efIndex >= 0) && (efIndex < numEncoded)), "efIndex {0}", efIndex);
            return UnPackValue(efEncoder.lowerLongs, efEncoder.numLowBits, efIndex, efEncoder.lowerBitsMask);
        }

        ///  <returns> The given <paramref name="highValue"/> shifted left by the number of low bits from by the EliasFanoSequence,
        ///           logically OR-ed with the given <paramref name="lowValue"/>. </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private long CombineHighLowValues(long highValue, long lowValue)
        {
            return (highValue << efEncoder.numLowBits) | lowValue;
        }

        private long curHighLong;

        /* The implementation of forward decoding and backward decoding is done by the following method pairs.
         *
         * toBeforeSequence - toAfterSequence
         * getCurrentRightShift - getCurrentLeftShift
         * toAfterCurrentHighBit - toBeforeCurrentHighBit
         * toNextHighLong - toPreviousHighLong
         * nextHighValue - previousHighValue
         * nextValue - previousValue
         * advanceToValue - backToValue
         *
         */

        /* Forward decoding section */

        /// <summary>
        /// Set the decoding index to just before the first encoded value.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void ToBeforeSequence()
        {
            efIndex = -1;
            setBitForIndex = -1;
        }

        /// <returns> The number of bits in a <see cref="long"/> after (<see cref="setBitForIndex"/> modulo <c>sizeof(long)</c>). </returns>
        private int CurrentRightShift
        {
            get
            {
                int s = (int)(setBitForIndex & ((sizeof(long) * 8) - 1));
                return s;
            }
        }

        /// <summary>
        /// Increment <see cref="efIndex"/> and <see cref="setBitForIndex"/> and
        /// shift <see cref="curHighLong"/> so that it does not contain the high bits before <see cref="setBitForIndex"/>. </summary>
        /// <returns> <c>true</c> if <see cref="efIndex"/> still smaller than <see cref="numEncoded"/>. </returns>
        private bool ToAfterCurrentHighBit()
        {
            efIndex += 1;
            if (efIndex >= numEncoded)
            {
                return false;
            }
            setBitForIndex += 1;
            int highIndex = (int)(setBitForIndex.TripleShift(LOG2_INT64_SIZE));
            curHighLong = efEncoder.upperLongs[highIndex].TripleShift(CurrentRightShift);
            return true;
        }

        /// <summary>
        /// The current high long has been determined to not contain the set bit that is needed.
        /// Increment <see cref="setBitForIndex"/> to the next high long and set <see cref="curHighLong"/> accordingly.
        /// <para/>
        /// NOTE: this was toNextHighLong() in Lucene.
        /// </summary>
        private void ToNextHighInt64()
        {
            setBitForIndex += (sizeof(long) * 8) - (setBitForIndex & ((sizeof(long) * 8) - 1));
            //assert getCurrentRightShift() == 0;
            int highIndex = (int)(setBitForIndex.TripleShift(LOG2_INT64_SIZE));
            curHighLong = efEncoder.upperLongs[highIndex];
        }

        /// <summary>
        /// <see cref="setBitForIndex"/> and <see cref="efIndex"/> have just been incremented, scan to the next high set bit
        /// by incrementing <see cref="setBitForIndex"/>, and by setting <see cref="curHighLong"/> accordingly.
        /// </summary>
        private void ToNextHighValue()
        {
            while (curHighLong == 0L)
            {
                ToNextHighInt64(); // inlining and unrolling would simplify somewhat
            }
            setBitForIndex += curHighLong.TrailingZeroCount();
        }

        /// <summary>
        /// <see cref="setBitForIndex"/> and <see cref="efIndex"/> have just been incremented, scan to the next high set bit
        /// by incrementing <see cref="setBitForIndex"/>, and by setting <see cref="curHighLong"/> accordingly. </summary>
        /// <returns> The next encoded high value. </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private long NextHighValue()
        {
            ToNextHighValue();
            return CurrentHighValue();
        }

        /// <summary>
        /// If another value is available after the current decoding index, return this value and
        /// and increase the decoding index by 1. Otherwise return <see cref="NO_MORE_VALUES"/>.
        /// </summary>
        public virtual long NextValue()
        {
            if (!ToAfterCurrentHighBit())
            {
                return NO_MORE_VALUES;
            }
            long highValue = NextHighValue();
            return CombineHighLowValues(highValue, CurrentLowValue());
        }

        /// <summary>
        /// Advance the decoding index to a given <paramref name="index"/>.
        /// and return <c>true</c> iff it is available.
        /// <para/>See also <see cref="CurrentValue()"/>.
        /// <para/>The current implementation does not use the index on the upper bit zero bit positions.
        /// <para/>Note: there is currently no implementation of <c>BackToIndex()</c>.
        /// </summary>
        public virtual bool AdvanceToIndex(long index)
        {
            if (Debugging.AssertsEnabled) Debugging.Assert(index > efIndex);
            if (index >= numEncoded)
            {
                efIndex = numEncoded;
                return false;
            }
            if (!ToAfterCurrentHighBit())
            {
                if (Debugging.AssertsEnabled) Debugging.Assert(false);
            }
            /* CHECKME: Add a (binary) search in the upperZeroBitPositions here. */
            int curSetBits = curHighLong.PopCount();
            while ((efIndex + curSetBits) < index) // curHighLong has not enough set bits to reach index
            {
                efIndex += curSetBits;
                ToNextHighInt64();
                curSetBits = curHighLong.PopCount();
            }
            // curHighLong has enough set bits to reach index
            while (efIndex < index)
            {
                /* CHECKME: Instead of the linear search here, use (forward) broadword selection from
                 * "Broadword Implementation of Rank/Select Queries", Sebastiano Vigna, January 30, 2012.
                 */
                if (!ToAfterCurrentHighBit())
                {
                    if (Debugging.AssertsEnabled) Debugging.Assert(false);
                }
                ToNextHighValue();
            }
            return true;
        }

        /// <summary>
        /// Given a <paramref name="target"/> value, advance the decoding index to the first bigger or equal value
        /// and return it if it is available. Otherwise return <see cref="NO_MORE_VALUES"/>.
        /// <para/>
        /// The current implementation uses the index on the upper zero bit positions.
        /// </summary>
        public virtual long AdvanceToValue(long target)
        {
            efIndex += 1;
            if (efIndex >= numEncoded)
            {
                return NO_MORE_VALUES;
            }
            setBitForIndex += 1; // the high bit at setBitForIndex belongs to the unary code for efIndex

            int highIndex = (int)(setBitForIndex.TripleShift(LOG2_INT64_SIZE));
            long upperLong = efEncoder.upperLongs[highIndex];
            curHighLong = upperLong.TripleShift(((int)(setBitForIndex & ((sizeof(long) * 8) - 1)))); // may contain the unary 1 bit for efIndex

            // determine index entry to advance to
            long highTarget = target.TripleShift(efEncoder.numLowBits);

            long indexEntryIndex = (highTarget / efEncoder.indexInterval) - 1;
            if (indexEntryIndex >= 0) // not before first index entry
            {
                if (indexEntryIndex >= numIndexEntries)
                {
                    indexEntryIndex = numIndexEntries - 1; // no further than last index entry
                }
                long indexHighValue = (indexEntryIndex + 1) * efEncoder.indexInterval;
                if (Debugging.AssertsEnabled) Debugging.Assert(indexHighValue <= highTarget);
                if (indexHighValue > (setBitForIndex - efIndex)) // advance to just after zero bit position of index entry.
                {
                    setBitForIndex = UnPackValue(efEncoder.upperZeroBitPositionIndex, efEncoder.nIndexEntryBits, indexEntryIndex, indexMask);
                    efIndex = setBitForIndex - indexHighValue; // the high bit at setBitForIndex belongs to the unary code for efIndex
                    highIndex = (int)setBitForIndex.TripleShift(LOG2_INT64_SIZE);
                    upperLong = efEncoder.upperLongs[highIndex];
                    curHighLong = upperLong.TripleShift((int)(setBitForIndex & ((sizeof(long) * 8) - 1))); // may contain the unary 1 bit for efIndex
                }
                if (Debugging.AssertsEnabled) Debugging.Assert(efIndex < numEncoded); // there is a high value to be found.
            }

            int curSetBits = curHighLong.PopCount(); // shifted right.
            int curClearBits = (sizeof(long) * 8) - curSetBits - ((int)(setBitForIndex & ((sizeof(long) * 8) - 1))); // subtract right shift, may be more than encoded

            while (((setBitForIndex - efIndex) + curClearBits) < highTarget)
            {
                // curHighLong has not enough clear bits to reach highTarget
                efIndex += curSetBits;
                if (efIndex >= numEncoded)
                {
                    return NO_MORE_VALUES;
                }
                setBitForIndex += (sizeof(long) * 8) - (setBitForIndex & ((sizeof(long) * 8) - 1));
                // highIndex = (int)(setBitForIndex >>> LOG2_LONG_SIZE);
                if (Debugging.AssertsEnabled) Debugging.Assert((highIndex + 1) == (int)(setBitForIndex.TripleShift(LOG2_INT64_SIZE)));
                highIndex += 1;
                upperLong = efEncoder.upperLongs[highIndex];
                curHighLong = upperLong;
                curSetBits = curHighLong.PopCount();
                curClearBits = (sizeof(long) * 8) - curSetBits;
            }
            // curHighLong has enough clear bits to reach highTarget, and may not have enough set bits.
            while (curHighLong == 0L)
            {
                setBitForIndex += (sizeof(long) * 8) - (setBitForIndex & ((sizeof(long) * 8) - 1));
                if (Debugging.AssertsEnabled) Debugging.Assert((highIndex + 1) == setBitForIndex.TripleShift(LOG2_INT64_SIZE));
                highIndex += 1;
                upperLong = efEncoder.upperLongs[highIndex];
                curHighLong = upperLong;
            }

            // curHighLong has enough clear bits to reach highTarget, has at least 1 set bit, and may not have enough set bits.
            int rank = (int)(highTarget - (setBitForIndex - efIndex)); // the rank of the zero bit for highValue.
            if (Debugging.AssertsEnabled) Debugging.Assert((rank <= (sizeof(long) * 8)), "rank {0}", rank);
            if (rank >= 1)
            {
                long invCurHighLong = ~curHighLong;
                int clearBitForValue = (rank <= 8) ? BroadWord.SelectNaive(invCurHighLong, rank) : BroadWord.Select(invCurHighLong, rank);
                if (Debugging.AssertsEnabled) Debugging.Assert(clearBitForValue <= ((sizeof(long) * 8) - 1));
                setBitForIndex += clearBitForValue + 1; // the high bit just before setBitForIndex is zero
                int oneBitsBeforeClearBit = clearBitForValue - rank + 1;
                efIndex += oneBitsBeforeClearBit; // the high bit at setBitForIndex and belongs to the unary code for efIndex
                if (efIndex >= numEncoded)
                {
                    return NO_MORE_VALUES;
                }

                if ((setBitForIndex & ((sizeof(long) * 8) - 1)) == 0L) // exhausted curHighLong
                {
                    if (Debugging.AssertsEnabled) Debugging.Assert((highIndex + 1) == setBitForIndex.TripleShift(LOG2_INT64_SIZE));
                    highIndex += 1;
                    upperLong = efEncoder.upperLongs[highIndex];
                    curHighLong = upperLong;
                }
                else
                {
                    if (Debugging.AssertsEnabled) Debugging.Assert(highIndex == setBitForIndex.TripleShift(LOG2_INT64_SIZE));
                    curHighLong = upperLong.TripleShift(((int)(setBitForIndex & ((sizeof(long) * 8) - 1))));
                }
                // curHighLong has enough clear bits to reach highTarget, and may not have enough set bits.

                while (curHighLong == 0L)
                {
                    setBitForIndex += (sizeof(long) * 8) - (setBitForIndex & ((sizeof(long) * 8) - 1));
                    if (Debugging.AssertsEnabled) Debugging.Assert((highIndex + 1) == setBitForIndex.TripleShift(LOG2_INT64_SIZE));
                    highIndex += 1;
                    upperLong = efEncoder.upperLongs[highIndex];
                    curHighLong = upperLong;
                }
            }
            setBitForIndex += curHighLong.TrailingZeroCount();
            if (Debugging.AssertsEnabled) Debugging.Assert((setBitForIndex - efIndex) >= highTarget); // highTarget reached

            // Linear search also with low values
            long currentValue = CombineHighLowValues((setBitForIndex - efIndex), CurrentLowValue());
            while (currentValue < target)
            {
                currentValue = NextValue();
                if (currentValue == NO_MORE_VALUES)
                {
                    return NO_MORE_VALUES;
                }
            }
            return currentValue;
        }

        /* Backward decoding section */

        /// <summary>
        /// Set the decoding index to just after the last encoded value.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void ToAfterSequence()
        {
            efIndex = numEncoded; // just after last index
            setBitForIndex = (efEncoder.lastEncoded.TripleShift(efEncoder.numLowBits)) + numEncoded;
        }

        /// <returns> the number of bits in a long before (<see cref="setBitForIndex"/> modulo <c>sizeof(long)</c>) </returns>
        private int CurrentLeftShift
        {
            get
            {
                int s = (sizeof(long) * 8) - 1 - (int)(setBitForIndex & ((sizeof(long) * 8) - 1));
                return s;
            }
        }

        /// <summary>
        /// Decrement <see cref="efIndex"/> and <see cref="setBitForIndex"/> and
        /// shift <see cref="curHighLong"/> so that it does not contain the high bits after <see cref="setBitForIndex"/>. </summary>
        /// <returns> <c>true</c> if <see cref="efIndex"/> still >= 0. </returns>
        private bool ToBeforeCurrentHighBit()
        {
            efIndex -= 1;
            if (efIndex < 0)
            {
                return false;
            }
            setBitForIndex -= 1;
            int highIndex = (int)setBitForIndex.TripleShift(LOG2_INT64_SIZE);
            curHighLong = efEncoder.upperLongs[highIndex] << CurrentLeftShift;
            return true;
        }

        /// <summary>
        /// The current high long has been determined to not contain the set bit that is needed.
        /// Decrement <see cref="setBitForIndex"/> to the previous high long and set <see cref="curHighLong"/> accordingly.
        /// <para/>
        /// NOTE: this was toPreviousHighLong() in Lucene.
        /// </summary>
        private void ToPreviousHighInt64()
        {
            setBitForIndex -= (setBitForIndex & ((sizeof(long) * 8) - 1)) + 1;
            //assert getCurrentLeftShift() == 0;
            int highIndex = (int)setBitForIndex.TripleShift(LOG2_INT64_SIZE);
            curHighLong = efEncoder.upperLongs[highIndex];
        }

        /// <summary>
        /// <see cref="setBitForIndex"/> and <see cref="efIndex"/> have just been decremented, scan to the previous high set bit
        /// by decrementing <see cref="setBitForIndex"/> and by setting <see cref="curHighLong"/> accordingly. </summary>
        /// <returns> The previous encoded high value. </returns>
        private long PreviousHighValue()
        {
            while (curHighLong == 0L)
            {
                ToPreviousHighInt64(); // inlining and unrolling would simplify somewhat
            }
            setBitForIndex -= curHighLong.LeadingZeroCount();
            return CurrentHighValue();
        }

        /// <summary>
        /// If another value is available before the current decoding index, return this value
        /// and decrease the decoding index by 1. Otherwise return <see cref="NO_MORE_VALUES"/>.
        /// </summary>
        public virtual long PreviousValue()
        {
            if (!ToBeforeCurrentHighBit())
            {
                return NO_MORE_VALUES;
            }
            long highValue = PreviousHighValue();
            return CombineHighLowValues(highValue, CurrentLowValue());
        }

        /// <summary>
        /// <see cref="setBitForIndex"/> and <see cref="efIndex"/> have just been decremented, scan backward to the high set bit
        /// of at most a given high value
        /// by decrementing <see cref="setBitForIndex"/> and by setting <see cref="curHighLong"/> accordingly.
        /// <para/>
        /// The current implementation does not use the index on the upper zero bit positions. 
        /// </summary>
        /// <returns> The largest encoded high value that is at most the given one. </returns>
        private long BackToHighValue(long highTarget)
        {
            /* CHECKME: Add using the index as in advanceToHighValue */
            int curSetBits = curHighLong.PopCount(); // is shifted by getCurrentLeftShift()
            int curClearBits = (sizeof(long) * 8) - curSetBits - CurrentLeftShift;
            while ((CurrentHighValue() - curClearBits) > highTarget)
            {
                // curHighLong has not enough clear bits to reach highTarget
                efIndex -= curSetBits;
                if (efIndex < 0)
                {
                    return NO_MORE_VALUES;
                }
                ToPreviousHighInt64();
                //assert getCurrentLeftShift() == 0;
                curSetBits = curHighLong.PopCount();
                curClearBits = (sizeof(long) * 8) - curSetBits;
            }
            // curHighLong has enough clear bits to reach highTarget, but may not have enough set bits.
            long highValue = PreviousHighValue();
            while (highValue > highTarget)
            {
                /* CHECKME: See at advanceToHighValue on using broadword bit selection. */
                if (!ToBeforeCurrentHighBit())
                {
                    return NO_MORE_VALUES;
                }
                highValue = PreviousHighValue();
            }
            return highValue;
        }

        /// <summary>
        /// Given a target value, go back to the first smaller or equal value
        /// and return it if it is available. Otherwise return <see cref="NO_MORE_VALUES"/>.
        /// <para/>
        /// The current implementation does not use the index on the upper zero bit positions.
        /// </summary>
        public virtual long BackToValue(long target)
        {
            if (!ToBeforeCurrentHighBit())
            {
                return NO_MORE_VALUES;
            }
            long highTarget = target.TripleShift(efEncoder.numLowBits);
            long highValue = BackToHighValue(highTarget);
            if (highValue == NO_MORE_VALUES)
            {
                return NO_MORE_VALUES;
            }
            // Linear search with low values:
            long currentValue = CombineHighLowValues(highValue, CurrentLowValue());
            while (currentValue > target)
            {
                currentValue = PreviousValue();
                if (currentValue == NO_MORE_VALUES)
                {
                    return NO_MORE_VALUES;
                }
            }
            return currentValue;
        }
    }
}