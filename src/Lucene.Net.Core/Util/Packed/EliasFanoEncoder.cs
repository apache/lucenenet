using Lucene.Net.Support;
using System;
using System.Diagnostics;
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
    /// <p>
    /// The Elias-Fano encoding is a high bits / low bits representation of
    /// a monotonically increasing sequence of <code>numValues > 0</code> natural numbers <code>x[i]</code>
    /// <p>
    /// <code>0 <= x[0] <= x[1] <= ... <= x[numValues-2] <= x[numValues-1] <= upperBound</code>
    /// <p>
    /// where <code>upperBound > 0</code> is an upper bound on the last value.
    /// <br>
    /// The Elias-Fano encoding uses less than half a bit per encoded number more
    /// than the smallest representation
    /// that can encode any monotone sequence with the same bounds.
    /// <p>
    /// The lower <code>L</code> bits of each <code>x[i]</code> are stored explicitly and contiguously
    /// in the lower-bits array, with <code>L</code> chosen as (<code>log()</code> base 2):
    /// <p>
    /// <code>L = max(0, floor(log(upperBound/numValues)))</code>
    /// <p>
    /// The upper bits are stored in the upper-bits array as a sequence of unary-coded gaps (<code>x[-1] = 0</code>):
    /// <p>
    /// <code>(x[i]/2**L) - (x[i-1]/2**L)</code>
    /// <p>
    /// The unary code encodes a natural number <code>n</code> by <code>n</code> 0 bits followed by a 1 bit:
    /// <code>0...01</code>. <br>
    /// In the upper bits the total the number of 1 bits is <code>numValues</code>
    /// and the total number of 0 bits is:<p>
    /// <code>floor(x[numValues-1]/2**L) <= upperBound/(2**max(0, floor(log(upperBound/numValues)))) <= 2*numValues</code>
    /// <p>
    /// The Elias-Fano encoding uses at most
    /// <p>
    /// <code>2 + ceil(log(upperBound/numValues))</code>
    /// <p>
    /// bits per encoded number. With <code>upperBound</code> in these bounds (<code>p</code> is an integer):
    /// <p>
    /// <code>2**p < x[numValues-1] <= upperBound <= 2**(p+1)</code>
    /// <p>
    /// the number of bits per encoded number is minimized.
    /// <p>
    /// In this implementation the values in the sequence can be given as <code>long</code>,
    /// <code>numValues = 0</code> and <code>upperBound = 0</code> are allowed,
    /// and each of the upper and lower bit arrays should fit in a <code>long[]</code>.
    /// <br>
    /// An index of positions of zero's in the upper bits is also built.
    /// <p>
    /// this implementation is based on this article:
    /// <br>
    /// Sebastiano Vigna, "Quasi Succinct Indices", June 19, 2012, sections 3, 4 and 9.
    /// Retrieved from http://arxiv.org/pdf/1206.4300 .
    ///
    /// <p>The articles originally describing the Elias-Fano representation are:
    /// <br>Peter Elias, "Efficient storage and retrieval by content and address of static files",
    /// J. Assoc. Comput. Mach., 21(2):246â€"260, 1974.
    /// <br>Robert M. Fano, "On the number of bits required to implement an associative memory",
    ///  Memorandum 61, Computer Structures Group, Project MAC, MIT, Cambridge, Mass., 1971.
    ///
    /// @lucene.internal
    /// </summary>

    public class EliasFanoEncoder
    {
        internal readonly long NumValues;
        private readonly long UpperBound;
        internal readonly int NumLowBits;
        internal readonly long LowerBitsMask;
        internal readonly long[] UpperLongs;
        internal readonly long[] LowerLongs;
        private static readonly int LOG2_LONG_SIZE = Number.NumberOfTrailingZeros(sizeof(long) * 8);

        internal long NumEncoded = 0L;
        internal long LastEncoded = 0L;

        /// <summary>
        /// The default index interval for zero upper bits. </summary>
        public const long DEFAULT_INDEX_INTERVAL = 256;

        internal readonly long NumIndexEntries;
        internal readonly long IndexInterval;
        internal readonly int NIndexEntryBits;

        /// <summary>
        /// upperZeroBitPositionIndex[i] (filled using packValue) will contain the bit position
        ///  just after the zero bit ((i+1) * indexInterval) in the upper bits.
        /// </summary>
        internal readonly long[] UpperZeroBitPositionIndex;

        internal long CurrentEntryIndex; // also indicates how many entries in the index are valid.

        /// <summary>
        /// Construct an Elias-Fano encoder.
        /// After construction, call <seealso cref="#encodeNext"/> <code>numValues</code> times to encode
        /// a non decreasing sequence of non negative numbers. </summary>
        /// <param name="numValues"> The number of values that is to be encoded. </param>
        /// <param name="upperBound">  At least the highest value that will be encoded.
        ///                For space efficiency this should not exceed the power of two that equals
        ///                or is the first higher than the actual maximum.
        ///                <br>When <code>numValues >= (upperBound/3)</code>
        ///                a <seealso cref="FixedBitSet"/> will take less space. </param>
        /// <param name="indexInterval"> The number of high zero bits for which a single index entry is built.
        ///                The index will have at most <code>2 * numValues / indexInterval</code> entries
        ///                and each index entry will use at most <code>ceil(log2(3 * numValues))</code> bits,
        ///                see <seealso cref="EliasFanoEncoder"/>. </param>
        /// <exception cref="IllegalArgumentException"> when:
        ///         <ul>
        ///         <li><code>numValues</code> is negative, or
        ///         <li><code>numValues</code> is non negative and <code>upperBound</code> is negative, or
        ///         <li>the low bits do not fit in a <code>long[]</code>:
        ///             <code>(L * numValues / 64) > Integer.MAX_VALUE</code>, or
        ///         <li>the high bits do not fit in a <code>long[]</code>:
        ///             <code>(2 * numValues / 64) > Integer.MAX_VALUE</code>, or
        ///         <li><code>indexInterval < 2</code>,
        ///         <li>the index bits do not fit in a <code>long[]</code>:
        ///             <code>(numValues / indexInterval * ceil(2log(3 * numValues)) / 64) > Integer.MAX_VALUE</code>.
        ///         </ul> </exception>
        public EliasFanoEncoder(long numValues, long upperBound, long indexInterval)
        {
            if (numValues < 0L)
            {
                throw new System.ArgumentException("numValues should not be negative: " + numValues);
            }
            this.NumValues = numValues;
            if ((numValues > 0L) && (upperBound < 0L))
            {
                throw new System.ArgumentException("upperBound should not be negative: " + upperBound + " when numValues > 0");
            }
            this.UpperBound = numValues > 0 ? upperBound : -1L; // if there is no value, -1 is the best upper bound
            int nLowBits = 0;
            if (this.NumValues > 0) // nLowBits = max(0; floor(2log(upperBound/numValues)))
            {
                long lowBitsFac = this.UpperBound / this.NumValues;
                if (lowBitsFac > 0)
                {
                    nLowBits = 63 - Number.NumberOfLeadingZeros(lowBitsFac); // see Long.numberOfLeadingZeros javadocs
                }
            }
            this.NumLowBits = nLowBits;
            this.LowerBitsMask = (long)(unchecked((ulong)long.MaxValue) >> (sizeof(long) * 8 - 1 - this.NumLowBits));

            long numLongsForLowBits = NumLongsForBits(numValues * NumLowBits);
            if (numLongsForLowBits > int.MaxValue)
            {
                throw new System.ArgumentException("numLongsForLowBits too large to index a long array: " + numLongsForLowBits);
            }
            this.LowerLongs = new long[(int)numLongsForLowBits];

            long numHighBitsClear = (long)((ulong)((this.UpperBound > 0) ? this.UpperBound : 0) >> this.NumLowBits);
            Debug.Assert(numHighBitsClear <= (2 * this.NumValues));
            long numHighBitsSet = this.NumValues;

            long numLongsForHighBits = NumLongsForBits(numHighBitsClear + numHighBitsSet);
            if (numLongsForHighBits > int.MaxValue)
            {
                throw new System.ArgumentException("numLongsForHighBits too large to index a long array: " + numLongsForHighBits);
            }
            this.UpperLongs = new long[(int)numLongsForHighBits];
            if (indexInterval < 2)
            {
                throw new System.ArgumentException("indexInterval should at least 2: " + indexInterval);
            }
            // For the index:
            long maxHighValue = (long)((ulong)upperBound >> this.NumLowBits);
            long nIndexEntries = maxHighValue / indexInterval; // no zero value index entry
            this.NumIndexEntries = (nIndexEntries >= 0) ? nIndexEntries : 0;
            long maxIndexEntry = maxHighValue + numValues - 1; // clear upper bits, set upper bits, start at zero
            this.NIndexEntryBits = (maxIndexEntry <= 0) ? 0 : (64 - Number.NumberOfLeadingZeros(maxIndexEntry));
            long numLongsForIndexBits = NumLongsForBits(NumIndexEntries * NIndexEntryBits);
            if (numLongsForIndexBits > int.MaxValue)
            {
                throw new System.ArgumentException("numLongsForIndexBits too large to index a long array: " + numLongsForIndexBits);
            }
            this.UpperZeroBitPositionIndex = new long[(int)numLongsForIndexBits];
            this.CurrentEntryIndex = 0;
            this.IndexInterval = indexInterval;
        }

        /// <summary>
        /// Construct an Elias-Fano encoder using <seealso cref="#DEFAULT_INDEX_INTERVAL"/>.
        /// </summary>
        public EliasFanoEncoder(long numValues, long upperBound)
            : this(numValues, upperBound, DEFAULT_INDEX_INTERVAL)
        {
        }

        private static long NumLongsForBits(long numBits) // Note: int version in FixedBitSet.bits2words()
        {
            Debug.Assert(numBits >= 0, numBits.ToString());
            return (long)((ulong)(numBits + (sizeof(long) * 8 - 1)) >> LOG2_LONG_SIZE);
        }

        /// <summary>
        /// Call at most <code>numValues</code> times to encode a non decreasing sequence of non negative numbers. </summary>
        /// <param name="x"> The next number to be encoded. </param>
        /// <exception cref="IllegalStateException"> when called more than <code>numValues</code> times. </exception>
        /// <exception cref="IllegalArgumentException"> when:
        ///         <ul>
        ///         <li><code>x</code> is smaller than an earlier encoded value, or
        ///         <li><code>x</code> is larger than <code>upperBound</code>.
        ///         </ul> </exception>
        public virtual void EncodeNext(long x)
        {
            if (NumEncoded >= NumValues)
            {
                throw new InvalidOperationException("encodeNext called more than " + NumValues + " times.");
            }
            if (LastEncoded > x)
            {
                throw new System.ArgumentException(x + " smaller than previous " + LastEncoded);
            }
            if (x > UpperBound)
            {
                throw new System.ArgumentException(x + " larger than upperBound " + UpperBound);
            }
            long highValue = (long)((ulong)x >> NumLowBits);
            EncodeUpperBits(highValue);
            EncodeLowerBits(x & LowerBitsMask);
            LastEncoded = x;
            // Add index entries:
            long indexValue = (CurrentEntryIndex + 1) * IndexInterval;
            while (indexValue <= highValue)
            {
                long afterZeroBitPosition = indexValue + NumEncoded;
                PackValue(afterZeroBitPosition, UpperZeroBitPositionIndex, NIndexEntryBits, CurrentEntryIndex);
                CurrentEntryIndex += 1;
                indexValue += IndexInterval;
            }
            NumEncoded++;
        }

        private void EncodeUpperBits(long highValue)
        {
            long nextHighBitNum = NumEncoded + highValue; // sequence of unary gaps
            UpperLongs[(int)((long)((ulong)nextHighBitNum >> LOG2_LONG_SIZE))] |= (1L << (int)(nextHighBitNum & ((sizeof(long) * 8) - 1)));
        }

        private void EncodeLowerBits(long lowValue)
        {
            PackValue(lowValue, LowerLongs, NumLowBits, NumEncoded);
        }

        private static void PackValue(long value, long[] longArray, int numBits, long packIndex)
        {
            if (numBits != 0)
            {
                long bitPos = numBits * packIndex;
                int index = (int)((long)((ulong)bitPos >> LOG2_LONG_SIZE));
                int bitPosAtIndex = (int)(bitPos & ((sizeof(long) * 8) - 1));
                longArray[index] |= (value << bitPosAtIndex);
                if ((bitPosAtIndex + numBits) > (sizeof(long) * 8))
                {
                    longArray[index + 1] = ((long)((ulong)value >> ((sizeof(long) * 8) - bitPosAtIndex)));
                }
            }
        }

        /// <summary>
        /// Provide an indication that it is better to use an <seealso cref="EliasFanoEncoder"/> than a <seealso cref="FixedBitSet"/>
        ///  to encode document identifiers.
        ///  this indication is not precise and may change in the future.
        ///  <br>An EliasFanoEncoder is favoured when the size of the encoding by the EliasFanoEncoder
        ///  (including some space for its index) is at most about 5/6 of the size of the FixedBitSet,
        ///  this is the same as comparing estimates of the number of bits accessed by a pair of FixedBitSets and
        ///  by a pair of non indexed EliasFanoDocIdSets when determining the intersections of the pairs.
        ///  <br>A bit set is preferred when <code>upperbound <= 256</code>.
        ///  <br>It is assumed that <seealso cref="#DEFAULT_INDEX_INTERVAL"/> is used. </summary>
        ///  <param name="numValues"> The number of document identifiers that is to be encoded. Should be non negative. </param>
        ///  <param name="upperBound"> The maximum possible value for a document identifier. Should be at least <code>numValues</code>. </param>
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
        /// Returns an <seealso cref="EliasFanoDecoder"/> to access the encoded values.
        /// Perform all calls to <seealso cref="#encodeNext"/> before calling <seealso cref="#getDecoder"/>.
        /// </summary>
        public virtual EliasFanoDecoder Decoder // LUCENENET TODO: change to GetDecoder() (returns new instance)
        {
            get
            {
                // decode as far as currently encoded as determined by numEncoded.
                return new EliasFanoDecoder(this);
            }
        }

        /// <summary>
        /// Expert. The low bits. </summary>
        public virtual long[] LowerBits // LUCENENET TODO: Change to GetLowerBits() (array)
        {
            get
            {
                return LowerLongs;
            }
        }

        /// <summary>
        /// Expert. The high bits. </summary>
        public virtual long[] UpperBits // LUCENENET TODO: Change to GetUpperBits() (array)
        {
            get
            {
                return UpperLongs;
            }
        }

        /// <summary>
        /// Expert. The index bits. </summary>
        public virtual long[] IndexBits // LUCENENET TODO: Change to GetIndexBits() (array)
        {
            get
            {
                return UpperZeroBitPositionIndex;
            }
        }

        public override string ToString()
        {
            StringBuilder s = new StringBuilder("EliasFanoSequence");
            s.Append(" numValues " + NumValues);
            s.Append(" numEncoded " + NumEncoded);
            s.Append(" upperBound " + UpperBound);
            s.Append(" lastEncoded " + LastEncoded);
            s.Append(" numLowBits " + NumLowBits);
            s.Append("\nupperLongs[" + UpperLongs.Length + "]");
            for (int i = 0; i < UpperLongs.Length; i++)
            {
                s.Append(" " + ToStringUtils.LongHex(UpperLongs[i]));
            }
            s.Append("\nlowerLongs[" + LowerLongs.Length + "]");
            for (int i = 0; i < LowerLongs.Length; i++)
            {
                s.Append(" " + ToStringUtils.LongHex(LowerLongs[i]));
            }
            s.Append("\nindexInterval: " + IndexInterval + ", nIndexEntryBits: " + NIndexEntryBits);
            s.Append("\nupperZeroBitPositionIndex[" + UpperZeroBitPositionIndex.Length + "]");
            for (int i = 0; i < UpperZeroBitPositionIndex.Length; i++)
            {
                s.Append(" " + ToStringUtils.LongHex(UpperZeroBitPositionIndex[i]));
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
            return (this.NumValues == oefs.NumValues) && (this.NumEncoded == oefs.NumEncoded) && (this.NumLowBits == oefs.NumLowBits) && (this.NumIndexEntries == oefs.NumIndexEntries) && (this.IndexInterval == oefs.IndexInterval) && Arrays.Equals(this.UpperLongs, oefs.UpperLongs) && Arrays.Equals(this.LowerLongs, oefs.LowerLongs); // no need to check index content
        }

        public override int GetHashCode()
        {
            int h = ((int)(31 * (NumValues + 7 * (NumEncoded + 5 * (NumLowBits + 3 * (NumIndexEntries + 11 * IndexInterval)))))) ^ Arrays.GetHashCode(UpperLongs) ^ Arrays.GetHashCode(LowerLongs);
            return h;
        }
    }
}