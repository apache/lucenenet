using J2N.Numerics;
using Lucene.Net.Diagnostics;
using Lucene.Net.Support;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;

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
    /// Encode a non decreasing sequence of non negative whole numbers in the Elias-Fano encoding
    /// that was introduced in the 1970's by Peter Elias and Robert Fano.
    /// <para/>
    /// The Elias-Fano encoding is a high bits / low bits representation of
    /// a monotonically increasing sequence of <c>numValues > 0</c> natural numbers <c>x[i]</c>
    /// <para/>
    /// <c>0 &lt;= x[0] &lt;= x[1] &lt;= ... &lt;= x[numValues-2] &lt;= x[numValues-1] &lt;= upperBound</c>
    /// <para/>
    /// where <c>upperBound > 0</c> is an upper bound on the last value.
    /// <para/>
    /// The Elias-Fano encoding uses less than half a bit per encoded number more
    /// than the smallest representation
    /// that can encode any monotone sequence with the same bounds.
    /// <para/>
    /// The lower <c>L</c> bits of each <c>x[i]</c> are stored explicitly and contiguously
    /// in the lower-bits array, with <c>L</c> chosen as (<c>Log()</c> base 2):
    /// <para/>
    /// <c>L = max(0, floor(log(upperBound/numValues)))</c>
    /// <para/>
    /// The upper bits are stored in the upper-bits array as a sequence of unary-coded gaps (<c>x[-1] = 0</c>):
    /// <para/>
    /// <c>(x[i]/2**L) - (x[i-1]/2**L)</c>
    /// <para/>
    /// The unary code encodes a natural number <c>n</c> by <c>n</c> 0 bits followed by a 1 bit:
    /// <c>0...01</c>. 
    /// <para/>
    /// In the upper bits the total the number of 1 bits is <c>numValues</c>
    /// and the total number of 0 bits is:
    /// <para/>
    /// <c>floor(x[numValues-1]/2**L) &lt;= upperBound/(2**max(0, floor(log(upperBound/numValues)))) &lt;= 2*numValues</c>
    /// <para/>
    /// The Elias-Fano encoding uses at most
    /// <para/>
    /// <c>2 + Ceil(Log(upperBound/numValues))</c>
    /// <para/>
    /// bits per encoded number. With <c>upperBound</c> in these bounds (<c>p</c> is an integer):
    /// <para/>
    /// <c>2**p &lt; x[numValues-1] &lt;= upperBound &lt;= 2**(p+1)</c>
    /// <para/>
    /// the number of bits per encoded number is minimized.
    /// <para/>
    /// In this implementation the values in the sequence can be given as <c>long</c>,
    /// <c>numValues = 0</c> and <c>upperBound = 0</c> are allowed,
    /// and each of the upper and lower bit arrays should fit in a <c>long[]</c>.
    /// <para/>
    /// An index of positions of zero's in the upper bits is also built.
    /// <para/>
    /// this implementation is based on this article:
    /// <para/>
    /// Sebastiano Vigna, "Quasi Succinct Indices", June 19, 2012, sections 3, 4 and 9.
    /// Retrieved from http://arxiv.org/pdf/1206.4300 .
    ///
    /// <para/>The articles originally describing the Elias-Fano representation are:
    /// <para/>Peter Elias, "Efficient storage and retrieval by content and address of static files",
    /// J. Assoc. Comput. Mach., 21(2):246â€"260, 1974.
    /// <para/>Robert M. Fano, "On the number of bits required to implement an associative memory",
    ///  Memorandum 61, Computer Structures Group, Project MAC, MIT, Cambridge, Mass., 1971.
    /// <para/>
    /// @lucene.internal
    /// </summary>
    public class EliasFanoEncoder
    {
        internal readonly long numValues;
        private readonly long upperBound;
        internal readonly int numLowBits;
        internal readonly long lowerBitsMask;
        internal readonly long[] upperLongs;
        internal readonly long[] lowerLongs;

        /// <summary>
        /// NOTE: This was LOG2_LONG_SIZE in Lucene.
        /// </summary>
        private static readonly int LOG2_INT64_SIZE = (sizeof(long) * 8).TrailingZeroCount();

        internal long numEncoded = 0L;
        internal long lastEncoded = 0L;

        /// <summary>
        /// The default index interval for zero upper bits. </summary>
        public const long DEFAULT_INDEX_INTERVAL = 256;

        internal readonly long numIndexEntries;
        internal readonly long indexInterval;
        internal readonly int nIndexEntryBits;

        /// <summary>
        /// upperZeroBitPositionIndex[i] (filled using packValue) will contain the bit position
        /// just after the zero bit ((i+1) * indexInterval) in the upper bits.
        /// </summary>
        internal readonly long[] upperZeroBitPositionIndex;

        internal long currentEntryIndex; // also indicates how many entries in the index are valid.

        /// <summary>
        /// Construct an Elias-Fano encoder.
        /// After construction, call <see cref="EncodeNext(long)"/> <paramref name="numValues"/> times to encode
        /// a non decreasing sequence of non negative numbers. 
        /// </summary>
        /// <param name="numValues"> The number of values that is to be encoded. </param>
        /// <param name="upperBound">  At least the highest value that will be encoded.
        ///                For space efficiency this should not exceed the power of two that equals
        ///                or is the first higher than the actual maximum.
        ///                <para/>When <c>numValues >= (upperBound/3)</c>
        ///                a <see cref="FixedBitSet"/> will take less space. </param>
        /// <param name="indexInterval"> The number of high zero bits for which a single index entry is built.
        ///                The index will have at most <c>2 * numValues / indexInterval</c> entries
        ///                and each index entry will use at most <c>Ceil(Log2(3 * numValues))</c> bits,
        ///                see <see cref="EliasFanoEncoder"/>. </param>
        /// <exception cref="ArgumentException"> when:
        ///         <list type="bullet">
        ///         <item><description><paramref name="numValues"/> is negative, or</description></item>
        ///         <item><description><paramref name="numValues"/> is non negative and <paramref name="upperBound"/> is negative, or</description></item>
        ///         <item><description>the low bits do not fit in a <c>long[]</c>:
        ///             <c>(L * numValues / 64) > System.Int32.MaxValue</c>, or</description></item>
        ///         <item><description>the high bits do not fit in a <c>long[]</c>:
        ///             <c>(2 * numValues / 64) > System.Int32.MaxValue</c>, or</description></item>
        ///         <item><description><c>indexInterval &lt; 2</c>,</description></item>
        ///         <item><description>the index bits do not fit in a <c>long[]</c>:
        ///             <c>(numValues / indexInterval * ceil(2log(3 * numValues)) / 64) > System.Int32.MaxValue</c>.</description></item>
        ///         </list> </exception>
        public EliasFanoEncoder(long numValues, long upperBound, long indexInterval)
        {
            if (numValues < 0L)
            {
                throw new ArgumentOutOfRangeException(nameof(numValues), "numValues should not be negative: " + numValues); // LUCENENET specific - changed from IllegalArgumentException to ArgumentOutOfRangeException (.NET convention)
            }
            this.numValues = numValues;
            if ((numValues > 0L) && (upperBound < 0L))
            {
                throw new ArgumentOutOfRangeException(nameof(upperBound), "upperBound should not be negative: " + upperBound + " when numValues > 0"); // LUCENENET specific - changed from IllegalArgumentException to ArgumentOutOfRangeException (.NET convention)
            }
            this.upperBound = numValues > 0 ? upperBound : -1L; // if there is no value, -1 is the best upper bound
            int nLowBits = 0;
            if (this.numValues > 0) // nLowBits = max(0; floor(2log(upperBound/numValues)))
            {
                long lowBitsFac = this.upperBound / this.numValues;
                if (lowBitsFac > 0)
                {
                    nLowBits = 63 - lowBitsFac.LeadingZeroCount(); // see Long.numberOfLeadingZeros javadocs
                }
            }
            this.numLowBits = nLowBits;
            this.lowerBitsMask = long.MaxValue.TripleShift(sizeof(long) * 8 - 1 - this.numLowBits);

            long numLongsForLowBits = NumInt64sForBits(numValues * numLowBits);
            if (numLongsForLowBits > int.MaxValue)
            {
                throw new ArgumentException("numLongsForLowBits too large to index a long array: " + numLongsForLowBits);
            }
            this.lowerLongs = new long[(int)numLongsForLowBits];

            long numHighBitsClear = ((this.upperBound > 0) ? this.upperBound : 0).TripleShift(this.numLowBits);
            if (Debugging.AssertsEnabled) Debugging.Assert(numHighBitsClear <= (2 * this.numValues));
            long numHighBitsSet = this.numValues;

            long numLongsForHighBits = NumInt64sForBits(numHighBitsClear + numHighBitsSet);
            if (numLongsForHighBits > int.MaxValue)
            {
                throw new ArgumentException("numLongsForHighBits too large to index a long array: " + numLongsForHighBits);
            }
            this.upperLongs = new long[(int)numLongsForHighBits];
            if (indexInterval < 2)
            {
                throw new ArgumentOutOfRangeException(nameof(indexInterval), "indexInterval should at least 2: " + indexInterval); // LUCENENET specific - changed from IllegalArgumentException to ArgumentOutOfRangeException (.NET convention)
            }
            // For the index:
            long maxHighValue = upperBound.TripleShift(this.numLowBits);
            long nIndexEntries = maxHighValue / indexInterval; // no zero value index entry
            this.numIndexEntries = (nIndexEntries >= 0) ? nIndexEntries : 0;
            long maxIndexEntry = maxHighValue + numValues - 1; // clear upper bits, set upper bits, start at zero
            this.nIndexEntryBits = (maxIndexEntry <= 0) ? 0 : (64 - maxIndexEntry.LeadingZeroCount());
            long numLongsForIndexBits = NumInt64sForBits(numIndexEntries * nIndexEntryBits);
            if (numLongsForIndexBits > int.MaxValue)
            {
                throw new ArgumentException("numLongsForIndexBits too large to index a long array: " + numLongsForIndexBits);
            }
            this.upperZeroBitPositionIndex = new long[(int)numLongsForIndexBits];
            this.currentEntryIndex = 0;
            this.indexInterval = indexInterval;
        }

        /// <summary>
        /// Construct an Elias-Fano encoder using <see cref="DEFAULT_INDEX_INTERVAL"/>.
        /// </summary>
        public EliasFanoEncoder(long numValues, long upperBound)
            : this(numValues, upperBound, DEFAULT_INDEX_INTERVAL)
        {
        }

        /// <summary>
        /// NOTE: This was numLongsForBits() in Lucene.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static long NumInt64sForBits(long numBits) // Note: int version in FixedBitSet.bits2words()
        {
            if (Debugging.AssertsEnabled) Debugging.Assert(numBits >= 0, "{0}", numBits);
            return (numBits + (sizeof(long) * 8 - 1)).TripleShift(LOG2_INT64_SIZE);
        }

        /// <summary>
        /// Call at most <see cref="numValues"/> times to encode a non decreasing sequence of non negative numbers. </summary>
        /// <param name="x"> The next number to be encoded. </param>
        /// <exception cref="InvalidOperationException"> when called more than <see cref="numValues"/> times. </exception>
        /// <exception cref="ArgumentOutOfRangeException"> when:
        ///         <list type="bullet">
        ///         <item><description><paramref name="x"/> is smaller than an earlier encoded value, or</description></item>
        ///         <item><description><paramref name="x"/> is larger than <see cref="upperBound"/>.</description></item>
        ///         </list> </exception>
        public virtual void EncodeNext(long x)
        {
            if (numEncoded >= numValues)
            {
                throw IllegalStateException.Create("EncodeNext() called more than " + numValues + " times.");
            }
            if (lastEncoded > x)
            {
                throw new ArgumentOutOfRangeException(nameof(x), x + " smaller than previous " + lastEncoded); // LUCENENET specific - changed from IllegalArgumentException to ArgumentOutOfRangeException (.NET convention)
            }
            if (x > upperBound)
            {
                throw new ArgumentOutOfRangeException(nameof(x), x + " larger than upperBound " + upperBound); // LUCENENET specific - changed from IllegalArgumentException to ArgumentOutOfRangeException (.NET convention)
            }
            long highValue = x.TripleShift(numLowBits);
            EncodeUpperBits(highValue);
            EncodeLowerBits(x & lowerBitsMask);
            lastEncoded = x;
            // Add index entries:
            long indexValue = (currentEntryIndex + 1) * indexInterval;
            while (indexValue <= highValue)
            {
                long afterZeroBitPosition = indexValue + numEncoded;
                PackValue(afterZeroBitPosition, upperZeroBitPositionIndex, nIndexEntryBits, currentEntryIndex);
                currentEntryIndex += 1;
                indexValue += indexInterval;
            }
            numEncoded++;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void EncodeUpperBits(long highValue)
        {
            long nextHighBitNum = numEncoded + highValue; // sequence of unary gaps
            upperLongs[(int)(nextHighBitNum.TripleShift(LOG2_INT64_SIZE))] |= (1L << (int)(nextHighBitNum & ((sizeof(long) * 8) - 1)));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void EncodeLowerBits(long lowValue)
        {
            PackValue(lowValue, lowerLongs, numLowBits, numEncoded);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void PackValue(long value, long[] longArray, int numBits, long packIndex)
        {
            if (numBits != 0)
            {
                long bitPos = numBits * packIndex;
                int index = (int)(bitPos.TripleShift(LOG2_INT64_SIZE));
                int bitPosAtIndex = (int)(bitPos & ((sizeof(long) * 8) - 1));
                longArray[index] |= (value << bitPosAtIndex);
                if ((bitPosAtIndex + numBits) > (sizeof(long) * 8))
                {
                    longArray[index + 1] = value.TripleShift((sizeof(long) * 8) - bitPosAtIndex);
                }
            }
        }

        /// <summary>
        /// Provide an indication that it is better to use an <see cref="EliasFanoEncoder"/> than a <see cref="FixedBitSet"/>
        /// to encode document identifiers.
        /// This indication is not precise and may change in the future.
        /// <para/>An <see cref="EliasFanoEncoder"/> is favored when the size of the encoding by the <see cref="EliasFanoEncoder"/>
        /// (including some space for its index) is at most about 5/6 of the size of the <see cref="FixedBitSet"/>,
        /// this is the same as comparing estimates of the number of bits accessed by a pair of <see cref="FixedBitSet"/>s and
        /// by a pair of non indexed <see cref="EliasFanoDocIdSet"/>s when determining the intersections of the pairs.
        /// <para/>A bit set is preferred when <c>upperbound &lt;= 256</c>.
        /// <para/>It is assumed that <see cref="DEFAULT_INDEX_INTERVAL"/> is used. 
        /// </summary>
        /// <param name="numValues"> The number of document identifiers that is to be encoded. Should be non negative. </param>
        /// <param name="upperBound"> The maximum possible value for a document identifier. Should be at least <paramref name="numValues"/>. </param>
        public static bool SufficientlySmallerThanBitSet(long numValues, long upperBound)
        {
            /* When (upperBound / 6) == numValues,
             * the number of bits per entry for the EliasFanoEncoder is 2 + ceil(2log(upperBound/numValues)) == 5.
             *
             * For intersecting two bit sets upperBound bits are accessed, roughly half of one, half of the other.
             * For intersecting two EliasFano sequences without index on the upper bits,
             * all (2 * 3 * numValues) upper bits are accessed.
             */
            return (upperBound > (4 * (sizeof(long) * 8))) && (upperBound / 7) > numValues; // 6 + 1 to allow some room for the index. -  prefer a bit set when it takes no more than 4 longs.
        }

        /// <summary>
        /// Returns an <see cref="EliasFanoDecoder"/> to access the encoded values.
        /// Perform all calls to <see cref="EncodeNext(long)"/> before calling <see cref="GetDecoder()"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual EliasFanoDecoder GetDecoder()
        {
            // decode as far as currently encoded as determined by numEncoded.
            return new EliasFanoDecoder(this);
        }

        /// <summary>
        /// Expert. The low bits. </summary>
        [WritableArray]
        [SuppressMessage("Microsoft.Performance", "CA1819", Justification = "Lucene's design requires some writable array properties")]
        public virtual long[] LowerBits => lowerLongs;

        /// <summary>
        /// Expert. The high bits. </summary>
        [WritableArray]
        [SuppressMessage("Microsoft.Performance", "CA1819", Justification = "Lucene's design requires some writable array properties")]
        public virtual long[] UpperBits => upperLongs;

        /// <summary>
        /// Expert. The index bits. </summary>
        [WritableArray]
        [SuppressMessage("Microsoft.Performance", "CA1819", Justification = "Lucene's design requires some writable array properties")]
        public virtual long[] IndexBits => upperZeroBitPositionIndex;

        public override string ToString()
        {
            StringBuilder s = new StringBuilder("EliasFanoSequence");
            s.Append(" numValues " + numValues);
            s.Append(" numEncoded " + numEncoded);
            s.Append(" upperBound " + upperBound);
            s.Append(" lastEncoded " + lastEncoded);
            s.Append(" numLowBits " + numLowBits);
            s.Append("\nupperLongs[" + upperLongs.Length + "]");
            for (int i = 0; i < upperLongs.Length; i++)
            {
                s.Append(" " + ToStringUtils.Int64Hex(upperLongs[i]));
            }
            s.Append("\nlowerLongs[" + lowerLongs.Length + "]");
            for (int i = 0; i < lowerLongs.Length; i++)
            {
                s.Append(" " + ToStringUtils.Int64Hex(lowerLongs[i]));
            }
            s.Append("\nindexInterval: " + indexInterval + ", nIndexEntryBits: " + nIndexEntryBits);
            s.Append("\nupperZeroBitPositionIndex[" + upperZeroBitPositionIndex.Length + "]");
            for (int i = 0; i < upperZeroBitPositionIndex.Length; i++)
            {
                s.Append(" " + ToStringUtils.Int64Hex(upperZeroBitPositionIndex[i]));
            }
            return s.ToString();
        }

        public override bool Equals(object other)
        {
            if (!(other is EliasFanoEncoder))
            {
                return false;
            }
            EliasFanoEncoder oefs = (EliasFanoEncoder)other;
            // no equality needed for upperBound
            return (this.numValues == oefs.numValues)
                && (this.numEncoded == oefs.numEncoded)
                && (this.numLowBits == oefs.numLowBits)
                && (this.numIndexEntries == oefs.numIndexEntries)
                && (this.indexInterval == oefs.indexInterval)
                && Arrays.Equals(this.upperLongs, oefs.upperLongs)
                && Arrays.Equals(this.lowerLongs, oefs.lowerLongs); // no need to check index content
        }

        public override int GetHashCode()
        {
            int h = ((int)(31 * (numValues + 7 * (numEncoded + 5 * (numLowBits + 3 * (numIndexEntries + 11 * indexInterval))))))
                ^ Arrays.GetHashCode(upperLongs)
                ^ Arrays.GetHashCode(lowerLongs);
            return h;
        }
    }
}