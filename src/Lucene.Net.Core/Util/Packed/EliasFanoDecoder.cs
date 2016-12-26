using Lucene.Net.Support;
using System;
using System.Diagnostics;

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

namespace Lucene.Net.Util.Packed
{
    /// <summary>
    /// A decoder for an <seealso cref="EliasFanoEncoder"/>.
    /// @lucene.internal
    /// </summary>
    public class EliasFanoDecoder
    {
        private static readonly int LOG2_LONG_SIZE = Number.NumberOfTrailingZeros((sizeof(long) * 8));

        private readonly EliasFanoEncoder EfEncoder;
        private readonly long NumEncoded_Renamed;
        private long EfIndex = -1; // the decoding index.
        private long SetBitForIndex = -1; // the index of the high bit at the decoding index.

        public const long NO_MORE_VALUES = -1L;

        private readonly long NumIndexEntries;
        private readonly long IndexMask;

        /// <summary>
        /// Construct a decoder for a given <seealso cref="EliasFanoEncoder"/>.
        /// The decoding index is set to just before the first encoded value.
        /// </summary>
        public EliasFanoDecoder(EliasFanoEncoder efEncoder)
        {
            this.EfEncoder = efEncoder;
            this.NumEncoded_Renamed = efEncoder.NumEncoded; // not final in EliasFanoEncoder
            this.NumIndexEntries = efEncoder.CurrentEntryIndex; // not final in EliasFanoEncoder
            this.IndexMask = (1L << efEncoder.NIndexEntryBits) - 1;
        }

        /// <returns> The Elias-Fano encoder that is decoded. </returns>
        public virtual EliasFanoEncoder EliasFanoEncoder
        {
            get
            {
                return EfEncoder;
            }
        }

        /// <summary>
        /// The number of values encoded by the encoder. </summary>
        /// <returns> The number of values encoded by the encoder. </returns>
        public virtual long NumEncoded() // LUCENENET TODO: make property
        {
            return NumEncoded_Renamed;
        }

        /// <summary>
        /// The current decoding index.
        /// The first value encoded by <seealso cref="EliasFanoEncoder#encodeNext"/> has index 0.
        /// Only valid directly after
        /// <seealso cref="#nextValue"/>, <seealso cref="#advanceToValue"/>,
        /// <seealso cref="#previousValue"/>, or <seealso cref="#backToValue"/>
        /// returned another value than <seealso cref="#NO_MORE_VALUES"/>,
        /// or <seealso cref="#advanceToIndex"/> returned true. </summary>
        /// <returns> The decoding index of the last decoded value, or as last set by <seealso cref="#advanceToIndex"/>. </returns>
        public virtual long CurrentIndex()
        {
            if (EfIndex < 0)
            {
                throw new InvalidOperationException("index before sequence");
            }
            if (EfIndex >= NumEncoded_Renamed)
            {
                throw new InvalidOperationException("index after sequence");
            }
            return EfIndex;
        }

        /// <summary>
        /// The value at the current decoding index.
        /// Only valid when <seealso cref="#currentIndex"/> would return a valid result.
        /// <br>this is only intended for use after <seealso cref="#advanceToIndex"/> returned true. </summary>
        /// <returns> The value encoded at <seealso cref="#currentIndex"/>. </returns>
        public virtual long CurrentValue()
        {
            return CombineHighLowValues(CurrentHighValue(), CurrentLowValue());
        }

        ///  <returns> The high value for the current decoding index. </returns>
        private long CurrentHighValue()
        {
            return SetBitForIndex - EfIndex; // sequence of unary gaps
        }

        /// <summary>
        /// See also <seealso cref="EliasFanoEncoder#packValue"/> </summary>
        private static long UnPackValue(long[] longArray, int numBits, long packIndex, long bitsMask)
        {
            if (numBits == 0)
            {
                return 0;
            }
            long bitPos = packIndex * numBits;
            int index = (int)((long)((ulong)bitPos >> LOG2_LONG_SIZE));
            int bitPosAtIndex = (int)(bitPos & ((sizeof(long) * 8) - 1));
            long value = (long)((ulong)longArray[index] >> bitPosAtIndex);
            if ((bitPosAtIndex + numBits) > (sizeof(long) * 8))
            {
                value |= (longArray[index + 1] << ((sizeof(long) * 8) - bitPosAtIndex));
            }
            value &= bitsMask;
            return value;
        }

        ///  <returns> The low value for the current decoding index. </returns>
        private long CurrentLowValue()
        {
            Debug.Assert(((EfIndex >= 0) && (EfIndex < NumEncoded_Renamed)), "efIndex " + EfIndex);
            return UnPackValue(EfEncoder.LowerLongs, EfEncoder.NumLowBits, EfIndex, EfEncoder.LowerBitsMask);
        }

        ///  <returns> The given highValue shifted left by the number of low bits from by the EliasFanoSequence,
        ///           logically OR-ed with the given lowValue. </returns>
        private long CombineHighLowValues(long highValue, long lowValue)
        {
            return (highValue << EfEncoder.NumLowBits) | lowValue;
        }

        private long CurHighLong;

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
        public virtual void ToBeforeSequence()
        {
            EfIndex = -1;
            SetBitForIndex = -1;
        }

        /// <returns> the number of bits in a long after (setBitForIndex modulo Long.SIZE) </returns>
        private int CurrentRightShift
        {
            get
            {
                int s = (int)(SetBitForIndex & ((sizeof(long) * 8) - 1));
                return s;
            }
        }

        /// <summary>
        /// Increment efIndex and setBitForIndex and
        /// shift curHighLong so that it does not contain the high bits before setBitForIndex. </summary>
        /// <returns> true iff efIndex still smaller than numEncoded. </returns>
        private bool ToAfterCurrentHighBit()
        {
            EfIndex += 1;
            if (EfIndex >= NumEncoded_Renamed)
            {
                return false;
            }
            SetBitForIndex += 1;
            int highIndex = (int)((long)((ulong)SetBitForIndex >> LOG2_LONG_SIZE));
            CurHighLong = (long)((ulong)EfEncoder.UpperLongs[highIndex] >> CurrentRightShift);
            return true;
        }

        /// <summary>
        /// The current high long has been determined to not contain the set bit that is needed.
        ///  Increment setBitForIndex to the next high long and set curHighLong accordingly.
        /// </summary>
        private void ToNextHighLong()
        {
            SetBitForIndex += (sizeof(long) * 8) - (SetBitForIndex & ((sizeof(long) * 8) - 1));
            //assert getCurrentRightShift() == 0;
            int highIndex = (int)((long)((ulong)SetBitForIndex >> LOG2_LONG_SIZE));
            CurHighLong = EfEncoder.UpperLongs[highIndex];
        }

        /// <summary>
        /// setBitForIndex and efIndex have just been incremented, scan to the next high set bit
        ///  by incrementing setBitForIndex, and by setting curHighLong accordingly.
        /// </summary>
        private void ToNextHighValue()
        {
            while (CurHighLong == 0L)
            {
                ToNextHighLong(); // inlining and unrolling would simplify somewhat
            }
            SetBitForIndex += Number.NumberOfTrailingZeros(CurHighLong);
        }

        /// <summary>
        /// setBitForIndex and efIndex have just been incremented, scan to the next high set bit
        ///  by incrementing setBitForIndex, and by setting curHighLong accordingly. </summary>
        ///  <returns> the next encoded high value. </returns>
        private long NextHighValue()
        {
            ToNextHighValue();
            return CurrentHighValue();
        }

        /// <summary>
        /// If another value is available after the current decoding index, return this value and
        /// and increase the decoding index by 1. Otherwise return <seealso cref="#NO_MORE_VALUES"/>.
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
        /// Advance the decoding index to a given index.
        /// and return <code>true</code> iff it is available.
        /// <br>See also <seealso cref="#currentValue"/>.
        /// <br>The current implementation does not use the index on the upper bit zero bit positions.
        /// <br>Note: there is currently no implementation of <code>backToIndex</code>.
        /// </summary>
        public virtual bool AdvanceToIndex(long index)
        {
            Debug.Assert(index > EfIndex);
            if (index >= NumEncoded_Renamed)
            {
                EfIndex = NumEncoded_Renamed;
                return false;
            }
            if (!ToAfterCurrentHighBit())
            {
                Debug.Assert(false);
            }
            /* CHECKME: Add a (binary) search in the upperZeroBitPositions here. */
            int curSetBits = Number.BitCount(CurHighLong);
            while ((EfIndex + curSetBits) < index) // curHighLong has not enough set bits to reach index
            {
                EfIndex += curSetBits;
                ToNextHighLong();
                curSetBits = Number.BitCount(CurHighLong);
            }
            // curHighLong has enough set bits to reach index
            while (EfIndex < index)
            {
                /* CHECKME: Instead of the linear search here, use (forward) broadword selection from
                 * "Broadword Implementation of Rank/Select Queries", Sebastiano Vigna, January 30, 2012.
                 */
                if (!ToAfterCurrentHighBit())
                {
                    Debug.Assert(false);
                }
                ToNextHighValue();
            }
            return true;
        }

        /// <summary>
        /// Given a target value, advance the decoding index to the first bigger or equal value
        /// and return it if it is available. Otherwise return <seealso cref="#NO_MORE_VALUES"/>.
        /// <br>The current implementation uses the index on the upper zero bit positions.
        /// </summary>
        public virtual long AdvanceToValue(long target)
        {
            EfIndex += 1;
            if (EfIndex >= NumEncoded_Renamed)
            {
                return NO_MORE_VALUES;
            }
            SetBitForIndex += 1; // the high bit at setBitForIndex belongs to the unary code for efIndex

            int highIndex = (int)((long)((ulong)SetBitForIndex >> LOG2_LONG_SIZE));
            long upperLong = EfEncoder.UpperLongs[highIndex];
            CurHighLong = (long)((ulong)upperLong >> ((int)(SetBitForIndex & ((sizeof(long) * 8) - 1)))); // may contain the unary 1 bit for efIndex

            // determine index entry to advance to
            long highTarget = (long)((ulong)target >> EfEncoder.NumLowBits);

            long indexEntryIndex = (highTarget / EfEncoder.IndexInterval) - 1;
            if (indexEntryIndex >= 0) // not before first index entry
            {
                if (indexEntryIndex >= NumIndexEntries)
                {
                    indexEntryIndex = NumIndexEntries - 1; // no further than last index entry
                }
                long indexHighValue = (indexEntryIndex + 1) * EfEncoder.IndexInterval;
                Debug.Assert(indexHighValue <= highTarget);
                if (indexHighValue > (SetBitForIndex - EfIndex)) // advance to just after zero bit position of index entry.
                {
                    SetBitForIndex = UnPackValue(EfEncoder.UpperZeroBitPositionIndex, EfEncoder.NIndexEntryBits, indexEntryIndex, IndexMask);
                    EfIndex = SetBitForIndex - indexHighValue; // the high bit at setBitForIndex belongs to the unary code for efIndex
                    highIndex = (int)(((ulong)SetBitForIndex >> LOG2_LONG_SIZE));
                    upperLong = EfEncoder.UpperLongs[highIndex];
                    CurHighLong = (long)((ulong)upperLong >> ((int)(SetBitForIndex & ((sizeof(long) * 8) - 1)))); // may contain the unary 1 bit for efIndex
                }
                Debug.Assert(EfIndex < NumEncoded_Renamed); // there is a high value to be found.
            }

            int curSetBits = Number.BitCount(CurHighLong); // shifted right.
            int curClearBits = (sizeof(long) * 8) - curSetBits - ((int)(SetBitForIndex & ((sizeof(long) * 8) - 1))); // subtract right shift, may be more than encoded

            while (((SetBitForIndex - EfIndex) + curClearBits) < highTarget)
            {
                // curHighLong has not enough clear bits to reach highTarget
                EfIndex += curSetBits;
                if (EfIndex >= NumEncoded_Renamed)
                {
                    return NO_MORE_VALUES;
                }
                SetBitForIndex += (sizeof(long) * 8) - (SetBitForIndex & ((sizeof(long) * 8) - 1));
                // highIndex = (int)(setBitForIndex >>> LOG2_LONG_SIZE);
                Debug.Assert((highIndex + 1) == (int)((long)((ulong)SetBitForIndex >> LOG2_LONG_SIZE)));
                highIndex += 1;
                upperLong = EfEncoder.UpperLongs[highIndex];
                CurHighLong = upperLong;
                curSetBits = Number.BitCount(CurHighLong);
                curClearBits = (sizeof(long) * 8) - curSetBits;
            }
            // curHighLong has enough clear bits to reach highTarget, and may not have enough set bits.
            while (CurHighLong == 0L)
            {
                SetBitForIndex += (sizeof(long) * 8) - (SetBitForIndex & ((sizeof(long) * 8) - 1));
                Debug.Assert((highIndex + 1) == (int)((ulong)SetBitForIndex >> LOG2_LONG_SIZE));
                highIndex += 1;
                upperLong = EfEncoder.UpperLongs[highIndex];
                CurHighLong = upperLong;
            }

            // curHighLong has enough clear bits to reach highTarget, has at least 1 set bit, and may not have enough set bits.
            int rank = (int)(highTarget - (SetBitForIndex - EfIndex)); // the rank of the zero bit for highValue.
            Debug.Assert((rank <= (sizeof(long) * 8)), ("rank " + rank));
            if (rank >= 1)
            {
                long invCurHighLong = ~CurHighLong;
                int clearBitForValue = (rank <= 8) ? BroadWord.SelectNaive(invCurHighLong, rank) : BroadWord.Select(invCurHighLong, rank);
                Debug.Assert(clearBitForValue <= ((sizeof(long) * 8) - 1));
                SetBitForIndex += clearBitForValue + 1; // the high bit just before setBitForIndex is zero
                int oneBitsBeforeClearBit = clearBitForValue - rank + 1;
                EfIndex += oneBitsBeforeClearBit; // the high bit at setBitForIndex and belongs to the unary code for efIndex
                if (EfIndex >= NumEncoded_Renamed)
                {
                    return NO_MORE_VALUES;
                }

                if ((SetBitForIndex & ((sizeof(long) * 8) - 1)) == 0L) // exhausted curHighLong
                {
                    Debug.Assert((highIndex + 1) == (int)((ulong)SetBitForIndex >> LOG2_LONG_SIZE));
                    highIndex += 1;
                    upperLong = EfEncoder.UpperLongs[highIndex];
                    CurHighLong = upperLong;
                }
                else
                {
                    Debug.Assert(highIndex == (int)((ulong)SetBitForIndex >> LOG2_LONG_SIZE));
                    CurHighLong = (long)((ulong)upperLong >> ((int)(SetBitForIndex & ((sizeof(long) * 8) - 1))));
                }
                // curHighLong has enough clear bits to reach highTarget, and may not have enough set bits.

                while (CurHighLong == 0L)
                {
                    SetBitForIndex += (sizeof(long) * 8) - (SetBitForIndex & ((sizeof(long) * 8) - 1));
                    Debug.Assert((highIndex + 1) == (int)((ulong)SetBitForIndex >> LOG2_LONG_SIZE));
                    highIndex += 1;
                    upperLong = EfEncoder.UpperLongs[highIndex];
                    CurHighLong = upperLong;
                }
            }
            SetBitForIndex += Number.NumberOfTrailingZeros(CurHighLong);
            Debug.Assert((SetBitForIndex - EfIndex) >= highTarget); // highTarget reached

            // Linear search also with low values
            long currentValue = CombineHighLowValues((SetBitForIndex - EfIndex), CurrentLowValue());
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
        public virtual void ToAfterSequence()
        {
            EfIndex = NumEncoded_Renamed; // just after last index
            SetBitForIndex = ((long)((ulong)EfEncoder.LastEncoded >> EfEncoder.NumLowBits)) + NumEncoded_Renamed;
        }

        /// <returns> the number of bits in a long before (setBitForIndex modulo Long.SIZE) </returns>
        private int CurrentLeftShift
        {
            get
            {
                int s = (sizeof(long) * 8) - 1 - (int)(SetBitForIndex & ((sizeof(long) * 8) - 1));
                return s;
            }
        }

        /// <summary>
        /// Decrement efindex and setBitForIndex and
        /// shift curHighLong so that it does not contain the high bits after setBitForIndex. </summary>
        /// <returns> true iff efindex still >= 0 </returns>
        private bool ToBeforeCurrentHighBit()
        {
            EfIndex -= 1;
            if (EfIndex < 0)
            {
                return false;
            }
            SetBitForIndex -= 1;
            int highIndex = (int)((ulong)SetBitForIndex >> LOG2_LONG_SIZE);
            CurHighLong = EfEncoder.UpperLongs[highIndex] << CurrentLeftShift;
            return true;
        }

        /// <summary>
        /// The current high long has been determined to not contain the set bit that is needed.
        ///  Decrement setBitForIndex to the previous high long and set curHighLong accordingly.
        /// </summary>
        private void ToPreviousHighLong()
        {
            SetBitForIndex -= (SetBitForIndex & ((sizeof(long) * 8) - 1)) + 1;
            //assert getCurrentLeftShift() == 0;
            int highIndex = (int)((ulong)SetBitForIndex >> LOG2_LONG_SIZE);
            CurHighLong = EfEncoder.UpperLongs[highIndex];
        }

        /// <summary>
        /// setBitForIndex and efIndex have just been decremented, scan to the previous high set bit
        ///  by decrementing setBitForIndex and by setting curHighLong accordingly. </summary>
        ///  <returns> the previous encoded high value. </returns>
        private long PreviousHighValue()
        {
            while (CurHighLong == 0L)
            {
                ToPreviousHighLong(); // inlining and unrolling would simplify somewhat
            }
            SetBitForIndex -= Number.NumberOfLeadingZeros(CurHighLong);
            return CurrentHighValue();
        }

        /// <summary>
        /// If another value is available before the current decoding index, return this value
        /// and decrease the decoding index by 1. Otherwise return <seealso cref="#NO_MORE_VALUES"/>.
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
        /// setBitForIndex and efIndex have just been decremented, scan backward to the high set bit
        ///  of at most a given high value
        ///  by decrementing setBitForIndex and by setting curHighLong accordingly.
        /// <br>The current implementation does not use the index on the upper zero bit positions. </summary>
        ///  <returns> the largest encoded high value that is at most the given one. </returns>
        private long BackToHighValue(long highTarget)
        {
            /* CHECKME: Add using the index as in advanceToHighValue */
            int curSetBits = Number.BitCount(CurHighLong); // is shifted by getCurrentLeftShift()
            int curClearBits = (sizeof(long) * 8) - curSetBits - CurrentLeftShift;
            while ((CurrentHighValue() - curClearBits) > highTarget)
            {
                // curHighLong has not enough clear bits to reach highTarget
                EfIndex -= curSetBits;
                if (EfIndex < 0)
                {
                    return NO_MORE_VALUES;
                }
                ToPreviousHighLong();
                //assert getCurrentLeftShift() == 0;
                curSetBits = Number.BitCount(CurHighLong);
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
        /// and return it if it is available. Otherwise return <seealso cref="#NO_MORE_VALUES"/>.
        /// <br>The current implementation does not use the index on the upper zero bit positions.
        /// </summary>
        public virtual long BackToValue(long target)
        {
            if (!ToBeforeCurrentHighBit())
            {
                return NO_MORE_VALUES;
            }
            long highTarget = (long)((ulong)target >> EfEncoder.NumLowBits);
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