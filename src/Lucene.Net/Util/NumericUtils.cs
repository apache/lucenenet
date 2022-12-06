using J2N.Numerics;
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

    using FilteredTermsEnum = Lucene.Net.Index.FilteredTermsEnum;
    using TermsEnum = Lucene.Net.Index.TermsEnum;

    /// <summary>
    /// This is a helper class to generate prefix-encoded representations for numerical values
    /// and supplies converters to represent float/double values as sortable integers/longs.
    ///
    /// <para/>To quickly execute range queries in Apache Lucene, a range is divided recursively
    /// into multiple intervals for searching: The center of the range is searched only with
    /// the lowest possible precision in the trie, while the boundaries are matched
    /// more exactly. this reduces the number of terms dramatically.
    ///
    /// <para/>This class generates terms to achieve this: First the numerical integer values need to
    /// be converted to bytes. For that integer values (32 bit or 64 bit) are made unsigned
    /// and the bits are converted to ASCII chars with each 7 bit. The resulting byte[] is
    /// sortable like the original integer value (even using UTF-8 sort order). Each value is also
    /// prefixed (in the first char) by the <c>shift</c> value (number of bits removed) used
    /// during encoding.
    ///
    /// <para/>To also index floating point numbers, this class supplies two methods to convert them
    /// to integer values by changing their bit layout: <see cref="DoubleToSortableInt64(double)"/>,
    /// <see cref="SingleToSortableInt32(float)"/>. You will have no precision loss by
    /// converting floating point numbers to integers and back (only that the integer form
    /// is not usable). Other data types like dates can easily converted to <see cref="long"/>s or <see cref="int"/>s (e.g.
    /// date to long: <see cref="DateTime.Ticks"/>).
    ///
    /// <para/>For easy usage, the trie algorithm is implemented for indexing inside
    /// <see cref="Analysis.NumericTokenStream"/> that can index <see cref="int"/>, <see cref="long"/>,
    /// <see cref="float"/>, and <see cref="double"/>. For querying,
    /// <see cref="Search.NumericRangeQuery"/> and <see cref="Search.NumericRangeFilter"/> implement the query part
    /// for the same data types.
    ///
    /// <para/>This class can also be used, to generate lexicographically sortable (according to
    /// <see cref="BytesRef.UTF8SortedAsUTF16Comparer"/>) representations of numeric data
    /// types for other usages (e.g. sorting).
    ///
    /// <para/>
    /// @lucene.internal
    /// @since 2.9, API changed non backwards-compliant in 4.0
    /// </summary>
    public static class NumericUtils // LUCENENET specific - changed to static
    {
        /// <summary>
        /// The default precision step used by <see cref="Documents.Int32Field"/>,
        /// <see cref="Documents.SingleField"/>, <see cref="Documents.Int64Field"/>, 
        /// <see cref="Documents.DoubleField"/>, <see cref="Analysis.NumericTokenStream"/>,
        /// <see cref="Search.NumericRangeQuery"/>, and <see cref="Search.NumericRangeFilter"/>.
        /// </summary>
        public const int PRECISION_STEP_DEFAULT = 4;

        /// <summary>
        /// Longs are stored at lower precision by shifting off lower bits. The shift count is
        /// stored as <c>SHIFT_START_INT64+shift</c> in the first byte
        /// <para/>
        /// NOTE: This was SHIFT_START_LONG in Lucene
        /// </summary>
        public const char SHIFT_START_INT64 = (char)0x20;

        /// <summary>
        /// The maximum term length (used for <see cref="T:byte[]"/> buffer size)
        /// for encoding <see cref="long"/> values.
        /// <para/>
        /// NOTE: This was BUF_SIZE_LONG in Lucene
        /// </summary>
        /// <seealso cref="Int64ToPrefixCodedBytes(long, int, BytesRef)"/>
        public const int BUF_SIZE_INT64 = 63 / 7 + 2;

        /// <summary>
        /// Integers are stored at lower precision by shifting off lower bits. The shift count is
        /// stored as <c>SHIFT_START_INT32+shift</c> in the first byte
        /// <para/>
        /// NOTE: This was SHIFT_START_INT in Lucene
        /// </summary>
        public const byte SHIFT_START_INT32 = 0x60;

        /// <summary>
        /// The maximum term length (used for <see cref="T:byte[]"/> buffer size)
        /// for encoding <see cref="int"/> values.
        /// <para/>
        /// NOTE: This was BUF_SIZE_INT in Lucene
        /// </summary>
        /// <seealso cref="Int32ToPrefixCodedBytes(int, int, BytesRef)"/>
        public const int BUF_SIZE_INT32 = 31 / 7 + 2;

        /// <summary>
        /// Returns prefix coded bits after reducing the precision by <paramref name="shift"/> bits.
        /// This is method is used by <see cref="Analysis.NumericTokenStream"/>.
        /// After encoding, <c>bytes.Offset</c> will always be 0. 
        /// <para/>
        /// NOTE: This was longToPrefixCoded() in Lucene
        /// </summary>
        /// <param name="val"> The numeric value </param>
        /// <param name="shift"> How many bits to strip from the right </param>
        /// <param name="bytes"> Will contain the encoded value </param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Int64ToPrefixCoded(long val, int shift, BytesRef bytes)
        {
            Int64ToPrefixCodedBytes(val, shift, bytes);
        }

        /// <summary>
        /// Returns prefix coded bits after reducing the precision by <paramref name="shift"/> bits.
        /// This is method is used by <see cref="Analysis.NumericTokenStream"/>.
        /// After encoding, <c>bytes.Offset</c> will always be 0. 
        /// <para/>
        /// NOTE: This was intToPrefixCoded() in Lucene
        /// </summary>
        /// <param name="val"> The numeric value </param>
        /// <param name="shift"> How many bits to strip from the right </param>
        /// <param name="bytes"> Will contain the encoded value </param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Int32ToPrefixCoded(int val, int shift, BytesRef bytes)
        {
            Int32ToPrefixCodedBytes(val, shift, bytes);
        }

        /// <summary>
        /// Returns prefix coded bits after reducing the precision by <paramref name="shift"/> bits.
        /// This is method is used by <see cref="Analysis.NumericTokenStream"/>.
        /// After encoding, <c>bytes.Offset</c> will always be 0. 
        /// <para/>
        /// NOTE: This was longToPrefixCodedBytes() in Lucene
        /// </summary>
        /// <param name="val"> The numeric value </param>
        /// <param name="shift"> How many bits to strip from the right </param>
        /// <param name="bytes"> Will contain the encoded value </param>
        public static void Int64ToPrefixCodedBytes(long val, int shift, BytesRef bytes)
        {
            if ((shift & ~0x3f) != 0) // ensure shift is 0..63
            {
                throw new ArgumentOutOfRangeException(nameof(shift), "Illegal shift value, must be 0..63"); // LUCENENET specific - changed from IllegalArgumentException to ArgumentOutOfRangeException (.NET convention)
            }
            int nChars = (((63 - shift) * 37) >> 8) + 1; // i/7 is the same as (i*37)>>8 for i in 0..63
            bytes.Offset = 0;
            bytes.Length = nChars + 1; // one extra for the byte that contains the shift info
            if (bytes.Bytes.Length < bytes.Length)
            {
                bytes.Bytes = new byte[NumericUtils.BUF_SIZE_INT64]; // use the max
            }
            bytes.Bytes[0] = (byte)(SHIFT_START_INT64 + shift);
            ulong sortableBits = BitConverter.ToUInt64(BitConverter.GetBytes(val), 0) ^ 0x8000000000000000L; // LUCENENET TODO: Performance - Benchmark this
            sortableBits = sortableBits >> shift;
            while (nChars > 0)
            {
                // Store 7 bits per byte for compatibility
                // with UTF-8 encoding of terms
                bytes.Bytes[nChars--] = (byte)(sortableBits & 0x7f);
                sortableBits = sortableBits >> 7;
            }
        }

        /// <summary>
        /// Returns prefix coded bits after reducing the precision by <paramref name="shift"/> bits.
        /// This is method is used by <see cref="Analysis.NumericTokenStream"/>.
        /// After encoding, <c>bytes.Offset</c> will always be 0. 
        /// <para/>
        /// NOTE: This was intToPrefixCodedBytes() in Lucene
        /// </summary>
        /// <param name="val"> The numeric value </param>
        /// <param name="shift"> How many bits to strip from the right </param>
        /// <param name="bytes"> Will contain the encoded value </param>
        public static void Int32ToPrefixCodedBytes(int val, int shift, BytesRef bytes)
        {
            if ((shift & ~0x1f) != 0) // ensure shift is 0..31
            {
                throw new ArgumentOutOfRangeException(nameof(shift), "Illegal shift value, must be 0..31"); // LUCENENET specific - changed from IllegalArgumentException to ArgumentOutOfRangeException (.NET convention)
            }
            int nChars = (((31 - shift) * 37) >> 8) + 1; // i/7 is the same as (i*37)>>8 for i in 0..63
            bytes.Offset = 0;
            bytes.Length = nChars + 1; // one extra for the byte that contains the shift info
            if (bytes.Bytes.Length < bytes.Length)
            {
                bytes.Bytes = new byte[NumericUtils.BUF_SIZE_INT64]; // use the max
            }
            bytes.Bytes[0] = (byte)(SHIFT_START_INT32 + shift);
            int sortableBits = val ^ unchecked((int)0x80000000);
            sortableBits = sortableBits.TripleShift(shift);
            while (nChars > 0)
            {
                // Store 7 bits per byte for compatibility
                // with UTF-8 encoding of terms
                bytes.Bytes[nChars--] = (byte)(sortableBits & 0x7f);
                sortableBits = sortableBits.TripleShift(7);
            }
        }

        /// <summary>
        /// Returns the shift value from a prefix encoded <see cref="long"/>. 
        /// <para/>
        /// NOTE: This was getPrefixCodedLongShift() in Lucene
        /// </summary>
        /// <exception cref="FormatException"> if the supplied <see cref="BytesRef"/> is
        /// not correctly prefix encoded. </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetPrefixCodedInt64Shift(BytesRef val)
        {
            int shift = val.Bytes[val.Offset] - SHIFT_START_INT64;
            if (shift > 63 || shift < 0)
            {
                throw NumberFormatException.Create("Invalid shift value (" + shift + ") in prefixCoded bytes (is encoded value really an INT?)");
            }
            return shift;
        }

        /// <summary>
        /// Returns the shift value from a prefix encoded <see cref="int"/>. 
        /// <para/>
        /// NOTE: This was getPrefixCodedIntShift() in Lucene
        /// </summary>
        /// <exception cref="FormatException"> if the supplied <see cref="BytesRef"/> is
        /// not correctly prefix encoded. </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetPrefixCodedInt32Shift(BytesRef val)
        {
            int shift = val.Bytes[val.Offset] - SHIFT_START_INT32;
            if (shift > 31 || shift < 0)
            {
                throw NumberFormatException.Create("Invalid shift value in prefixCoded bytes (is encoded value really an INT?)");
            }
            return shift;
        }

        /// <summary>
        /// Returns a <see cref="long"/> from prefixCoded bytes.
        /// Rightmost bits will be zero for lower precision codes.
        /// This method can be used to decode a term's value. 
        /// <para/>
        /// NOTE: This was prefixCodedToLong() in Lucene
        /// </summary>
        /// <exception cref="FormatException"> if the supplied <see cref="BytesRef"/> is
        /// not correctly prefix encoded. </exception>
        /// <seealso cref="Int64ToPrefixCodedBytes(long, int, BytesRef)"/>
        public static long PrefixCodedToInt64(BytesRef val)
        {
            long sortableBits = 0L;
            for (int i = val.Offset + 1, limit = val.Offset + val.Length; i < limit; i++)
            {
                sortableBits <<= 7;
                var b = val.Bytes[i];
                if (b < 0)
                {
                    throw NumberFormatException.Create("Invalid prefixCoded numerical value representation (byte " + (b & 0xff).ToString("x") + " at position " + (i - val.Offset) + " is invalid)");
                }
                sortableBits |= (byte)b;
            }
            return (long)((ulong)(sortableBits << GetPrefixCodedInt64Shift(val)) ^ 0x8000000000000000L); // LUCENENET TODO: Is the casting here necessary?
        }

        /// <summary>
        /// Returns an <see cref="int"/> from prefixCoded bytes.
        /// Rightmost bits will be zero for lower precision codes.
        /// This method can be used to decode a term's value. 
        /// <para/>
        /// NOTE: This was prefixCodedToInt() in Lucene
        /// </summary>
        /// <exception cref="FormatException"> if the supplied <see cref="BytesRef"/> is
        /// not correctly prefix encoded. </exception>
        /// <seealso cref="Int32ToPrefixCodedBytes(int, int, BytesRef)"/>
        public static int PrefixCodedToInt32(BytesRef val)
        {
            long sortableBits = 0;
            for (int i = val.Offset, limit = val.Offset + val.Length; i < limit; i++)
            {
                sortableBits <<= 7;
                var b = val.Bytes[i];
                if (b < 0)
                {
                    throw NumberFormatException.Create("Invalid prefixCoded numerical value representation (byte " + (b & 0xff).ToString("x") + " at position " + (i - val.Offset) + " is invalid)");
                }
                sortableBits |= b;
            }
            return (int)((sortableBits << GetPrefixCodedInt32Shift(val)) ^ 0x80000000);
        }

        /// <summary>
        /// Converts a <see cref="double"/> value to a sortable signed <see cref="long"/>.
        /// The value is converted by getting their IEEE 754 floating-point &quot;double format&quot;
        /// bit layout and then some bits are swapped, to be able to compare the result as <see cref="long"/>.
        /// By this the precision is not reduced, but the value can easily used as a <see cref="long"/>.
        /// The sort order (including <see cref="double.NaN"/>) is defined by
        /// <see cref="double.CompareTo(double)"/>; <c>NaN</c> is greater than positive infinity. 
        /// <para/>
        /// NOTE: This was doubleToSortableLong() in Lucene
        /// </summary>
        /// <seealso cref="SortableInt64ToDouble(long)"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long DoubleToSortableInt64(double val)
        {
            long f = J2N.BitConversion.DoubleToInt64Bits(val);
            if (f < 0)
            {
                f ^= 0x7fffffffffffffffL;
            }
            return f;
        }

        /// <summary>
        /// Converts a sortable <see cref="long"/> back to a <see cref="double"/>. 
        /// <para/>
        /// NOTE: This was sortableLongToDouble() in Lucene
        /// </summary>
        /// <seealso cref="DoubleToSortableInt64(double)"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double SortableInt64ToDouble(long val)
        {
            if (val < 0)
            {
                val ^= 0x7fffffffffffffffL;
            }
            return J2N.BitConversion.Int64BitsToDouble(val);
        }

        /// <summary>
        /// Converts a <see cref="float"/> value to a sortable signed <see cref="int"/>.
        /// The value is converted by getting their IEEE 754 floating-point &quot;float format&quot;
        /// bit layout and then some bits are swapped, to be able to compare the result as <see cref="int"/>.
        /// By this the precision is not reduced, but the value can easily used as an <see cref="int"/>.
        /// The sort order (including <see cref="float.NaN"/>) is defined by
        /// <seealso cref="float.CompareTo(float)"/>; <c>NaN</c> is greater than positive infinity. 
        /// <para/>
        /// NOTE: This was floatToSortableInt() in Lucene
        /// </summary>
        /// <seealso cref="SortableInt32ToSingle(int)"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int SingleToSortableInt32(float val)
        {
            int f = J2N.BitConversion.SingleToInt32Bits(val);
            if (f < 0)
            {
                f ^= 0x7fffffff;
            }
            return f;
        }

        /// <summary>
        /// Converts a sortable <see cref="int"/> back to a <see cref="float"/>. 
        /// <para/>
        /// NOTE: This was sortableIntToFloat() in Lucene
        /// </summary>
        /// <seealso cref="SingleToSortableInt32"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float SortableInt32ToSingle(int val)
        {
            if (val < 0)
            {
                val ^= 0x7fffffff;
            }
            return J2N.BitConversion.Int32BitsToSingle(val);
        }

        /// <summary>
        /// Splits a long range recursively.
        /// You may implement a builder that adds clauses to a
        /// <see cref="Lucene.Net.Search.BooleanQuery"/> for each call to its
        /// <see cref="Int64RangeBuilder.AddRange(BytesRef, BytesRef)"/>
        /// method.
        /// <para/>
        /// This method is used by <see cref="Search.NumericRangeQuery"/>.
        /// <para/>
        /// NOTE: This was splitLongRange() in Lucene
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SplitInt64Range(Int64RangeBuilder builder, int precisionStep, long minBound, long maxBound)
        {
            SplitRange(builder, 64, precisionStep, minBound, maxBound);
        }

        /// <summary>
        /// Splits an <see cref="int"/> range recursively.
        /// You may implement a builder that adds clauses to a
        /// <see cref="Lucene.Net.Search.BooleanQuery"/> for each call to its
        /// <see cref="Int32RangeBuilder.AddRange(BytesRef, BytesRef)"/>
        /// method.
        /// <para/>
        /// This method is used by <see cref="Search.NumericRangeQuery"/>.
        /// <para/>
        /// NOTE: This was splitIntRange() in Lucene
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SplitInt32Range(Int32RangeBuilder builder, int precisionStep, int minBound, int maxBound)
        {
            SplitRange(builder, 32, precisionStep, minBound, maxBound);
        }

        /// <summary>
        /// This helper does the splitting for both 32 and 64 bit. </summary>
        private static void SplitRange(object builder, int valSize, int precisionStep, long minBound, long maxBound)
        {
            if (precisionStep < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(precisionStep), "precisionStep must be >=1"); // LUCENENET specific - changed from IllegalArgumentException to ArgumentOutOfRangeException (.NET convention)
            }
            if (minBound > maxBound)
            {
                return;
            }
            for (int shift = 0; ; shift += precisionStep)
            {
                // calculate new bounds for inner precision
                long diff = 1L << (shift + precisionStep), mask = ((1L << precisionStep) - 1L) << shift;
                bool hasLower = (minBound & mask) != 0L, hasUpper = (maxBound & mask) != mask;
                long nextMinBound = (hasLower ? (minBound + diff) : minBound) & ~mask, nextMaxBound = (hasUpper ? (maxBound - diff) : maxBound) & ~mask;
                bool lowerWrapped = nextMinBound < minBound, upperWrapped = nextMaxBound > maxBound;

                if (shift + precisionStep >= valSize || nextMinBound > nextMaxBound || lowerWrapped || upperWrapped)
                {
                    // We are in the lowest precision or the next precision is not available.
                    AddRange(builder, valSize, minBound, maxBound, shift);
                    // exit the split recursion loop
                    break;
                }

                if (hasLower)
                {
                    AddRange(builder, valSize, minBound, minBound | mask, shift);
                }
                if (hasUpper)
                {
                    AddRange(builder, valSize, maxBound & ~mask, maxBound, shift);
                }

                // recurse to next precision
                minBound = nextMinBound;
                maxBound = nextMaxBound;
            }
        }

        /// <summary>
        /// Helper that delegates to correct range builder. </summary>
        private static void AddRange(object builder, int valSize, long minBound, long maxBound, int shift)
        {
            // for the max bound set all lower bits (that were shifted away):
            // this is important for testing or other usages of the splitted range
            // (e.g. to reconstruct the full range). The prefixEncoding will remove
            // the bits anyway, so they do not hurt!
            maxBound |= (1L << shift) - 1L;
            // delegate to correct range builder
            switch (valSize)
            {
                case 64:
                    ((Int64RangeBuilder)builder).AddRange(minBound, maxBound, shift);
                    break;

                case 32:
                    ((Int32RangeBuilder)builder).AddRange((int)minBound, (int)maxBound, shift);
                    break;

                default:
                    // Should not happen!
                    throw new ArgumentException("valSize must be 32 or 64.");
            }
        }

        /// <summary>
        /// Callback for <see cref="SplitInt64Range(Int64RangeBuilder, int, long, long)"/>.
        /// You need to override only one of the methods.
        /// <para/>
        /// NOTE: This was LongRangeBuilder in Lucene
        /// <para/>
        /// @lucene.internal
        /// @since 2.9, API changed non backwards-compliant in 4.0
        /// </summary>
        public abstract class Int64RangeBuilder
        {
            /// <summary>
            /// Override this method, if you like to receive the already prefix encoded range bounds.
            /// You can directly build classical (inclusive) range queries from them.
            /// </summary>
            public virtual void AddRange(BytesRef minPrefixCoded, BytesRef maxPrefixCoded)
            {
                throw UnsupportedOperationException.Create();
            }

            /// <summary>
            /// Override this method, if you like to receive the raw long range bounds.
            /// You can use this for e.g. debugging purposes (print out range bounds).
            /// </summary>
            public virtual void AddRange(long min, long max, int shift)
            {
                BytesRef minBytes = new BytesRef(BUF_SIZE_INT64), maxBytes = new BytesRef(BUF_SIZE_INT64);
                Int64ToPrefixCodedBytes(min, shift, minBytes);
                Int64ToPrefixCodedBytes(max, shift, maxBytes);
                AddRange(minBytes, maxBytes);
            }
        }

        /// <summary>
        /// Callback for <see cref="SplitInt32Range(Int32RangeBuilder, int, int, int)"/>.
        /// You need to override only one of the methods.
        /// <para/>
        /// NOTE: This was IntRangeBuilder in Lucene
        /// 
        /// @lucene.internal
        /// @since 2.9, API changed non backwards-compliant in 4.0
        /// </summary>
        public abstract class Int32RangeBuilder
        {
            /// <summary>
            /// Override this method, if you like to receive the already prefix encoded range bounds.
            /// You can directly build classical range (inclusive) queries from them.
            /// </summary>
            public virtual void AddRange(BytesRef minPrefixCoded, BytesRef maxPrefixCoded)
            {
                throw UnsupportedOperationException.Create();
            }

            /// <summary>
            /// Override this method, if you like to receive the raw int range bounds.
            /// You can use this for e.g. debugging purposes (print out range bounds).
            /// </summary>
            public virtual void AddRange(int min, int max, int shift)
            {
                BytesRef minBytes = new BytesRef(BUF_SIZE_INT32), maxBytes = new BytesRef(BUF_SIZE_INT32);
                Int32ToPrefixCodedBytes(min, shift, minBytes);
                Int32ToPrefixCodedBytes(max, shift, maxBytes);
                AddRange(minBytes, maxBytes);
            }
        }

        /// <summary>
        /// Filters the given <see cref="TermsEnum"/> by accepting only prefix coded 64 bit
        /// terms with a shift value of <c>0</c>.
        /// <para/>
        /// NOTE: This was filterPrefixCodedLongs() in Lucene
        /// </summary>
        /// <param name="termsEnum">
        ///          The terms enum to filter </param>
        /// <returns> A filtered <see cref="TermsEnum"/> that only returns prefix coded 64 bit
        ///         terms with a shift value of <c>0</c>. </returns>
        public static TermsEnum FilterPrefixCodedInt64s(TermsEnum termsEnum)
        {
            return new FilteredTermsEnumAnonymousClass(termsEnum);
        }

        private sealed class FilteredTermsEnumAnonymousClass : FilteredTermsEnum
        {
            public FilteredTermsEnumAnonymousClass(TermsEnum termsEnum)
                : base(termsEnum, false)
            {
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            protected override AcceptStatus Accept(BytesRef term)
            {
                return NumericUtils.GetPrefixCodedInt64Shift(term) == 0 ? AcceptStatus.YES : AcceptStatus.END;
            }
        }

        /// <summary>
        /// Filters the given <see cref="TermsEnum"/> by accepting only prefix coded 32 bit
        /// terms with a shift value of <c>0</c>.
        /// <para/>
        /// NOTE: This was filterPrefixCodedInts() in Lucene
        /// </summary>
        /// <param name="termsEnum">
        ///          The terms enum to filter </param>
        /// <returns> A filtered <see cref="TermsEnum"/> that only returns prefix coded 32 bit
        ///         terms with a shift value of <c>0</c>. </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TermsEnum FilterPrefixCodedInt32s(TermsEnum termsEnum)
        {
            return new FilteredTermsEnumAnonymousClass2(termsEnum);
        }

        private sealed class FilteredTermsEnumAnonymousClass2 : FilteredTermsEnum
        {
            public FilteredTermsEnumAnonymousClass2(TermsEnum termsEnum)
                : base(termsEnum, false)
            {
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            protected override AcceptStatus Accept(BytesRef term)
            {
                return NumericUtils.GetPrefixCodedInt32Shift(term) == 0 ? AcceptStatus.YES : AcceptStatus.END;
            }
        }
    }
}