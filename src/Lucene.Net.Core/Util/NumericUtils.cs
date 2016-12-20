using Lucene.Net.Documents;

namespace Lucene.Net.Util
{
    using Lucene.Net.Search;
    using Lucene.Net.Support; // for javadocs
    using System;
    using FilteredTermsEnum = Lucene.Net.Index.FilteredTermsEnum;

    // javadocs
    using FloatField = FloatField; // javadocs
    using IntField = IntField; // javadocs
    using LongField = LongField; // javadocs

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

    using NumericTokenStream = Lucene.Net.Analysis.NumericTokenStream;
    using TermsEnum = Lucene.Net.Index.TermsEnum;

    /// <summary>
    /// this is a helper class to generate prefix-encoded representations for numerical values
    /// and supplies converters to represent float/double values as sortable integers/longs.
    ///
    /// <p>To quickly execute range queries in Apache Lucene, a range is divided recursively
    /// into multiple intervals for searching: The center of the range is searched only with
    /// the lowest possible precision in the trie, while the boundaries are matched
    /// more exactly. this reduces the number of terms dramatically.
    ///
    /// <p>this class generates terms to achieve this: First the numerical integer values need to
    /// be converted to bytes. For that integer values (32 bit or 64 bit) are made unsigned
    /// and the bits are converted to ASCII chars with each 7 bit. The resulting byte[] is
    /// sortable like the original integer value (even using UTF-8 sort order). Each value is also
    /// prefixed (in the first char) by the <code>shift</code> value (number of bits removed) used
    /// during encoding.
    ///
    /// <p>To also index floating point numbers, this class supplies two methods to convert them
    /// to integer values by changing their bit layout: <seealso cref="#doubleToSortableLong"/>,
    /// <seealso cref="#floatToSortableInt"/>. You will have no precision loss by
    /// converting floating point numbers to integers and back (only that the integer form
    /// is not usable). Other data types like dates can easily converted to longs or ints (e.g.
    /// date to long: <seealso cref="java.util.Date#getTime"/>).
    ///
    /// <p>For easy usage, the trie algorithm is implemented for indexing inside
    /// <seealso cref="NumericTokenStream"/> that can index <code>int</code>, <code>long</code>,
    /// <code>float</code>, and <code>double</code>. For querying,
    /// <seealso cref="NumericRangeQuery"/> and <seealso cref="NumericRangeFilter"/> implement the query part
    /// for the same data types.
    ///
    /// <p>this class can also be used, to generate lexicographically sortable (according to
    /// <seealso cref="BytesRef#getUTF8SortedAsUTF16Comparator()"/>) representations of numeric data
    /// types for other usages (e.g. sorting).
    ///
    /// @lucene.internal
    /// @since 2.9, API changed non backwards-compliant in 4.0
    /// </summary>
    public sealed class NumericUtils
    {
        private NumericUtils() // no instance!
        {
        }

        /// <summary>
        /// The default precision step used by <seealso cref="IntField"/>,
        /// <seealso cref="FloatField"/>, <seealso cref="LongField"/>, {@link
        /// DoubleField}, <seealso cref="NumericTokenStream"/>, {@link
        /// NumericRangeQuery}, and <seealso cref="NumericRangeFilter"/>.
        /// </summary>
        public const int PRECISION_STEP_DEFAULT = 4;

        /// <summary>
        /// Longs are stored at lower precision by shifting off lower bits. The shift count is
        /// stored as <code>SHIFT_START_LONG+shift</code> in the first byte
        /// </summary>
        public const char SHIFT_START_LONG = (char)0x20;

        /// <summary>
        /// The maximum term length (used for <code>byte[]</code> buffer size)
        /// for encoding <code>long</code> values. </summary>
        /// <seealso cref= #longToPrefixCodedBytes </seealso>
        public const int BUF_SIZE_LONG = 63 / 7 + 2;

        /// <summary>
        /// Integers are stored at lower precision by shifting off lower bits. The shift count is
        /// stored as <code>SHIFT_START_INT+shift</code> in the first byte
        /// </summary>
        public const sbyte SHIFT_START_INT = 0x60;

        /// <summary>
        /// The maximum term length (used for <code>byte[]</code> buffer size)
        /// for encoding <code>int</code> values. </summary>
        /// <seealso cref= #intToPrefixCodedBytes </seealso>
        public const int BUF_SIZE_INT = 31 / 7 + 2;

        /// <summary>
        /// Returns prefix coded bits after reducing the precision by <code>shift</code> bits.
        /// this is method is used by <seealso cref="NumericTokenStream"/>.
        /// After encoding, {@code bytes.offset} will always be 0. </summary>
        /// <param name="val"> the numeric value </param>
        /// <param name="shift"> how many bits to strip from the right </param>
        /// <param name="bytes"> will contain the encoded value </param>
        public static void LongToPrefixCoded(long val, int shift, BytesRef bytes)
        {
            LongToPrefixCodedBytes(val, shift, bytes);
        }

        /// <summary>
        /// Returns prefix coded bits after reducing the precision by <code>shift</code> bits.
        /// this is method is used by <seealso cref="NumericTokenStream"/>.
        /// After encoding, {@code bytes.offset} will always be 0. </summary>
        /// <param name="val"> the numeric value </param>
        /// <param name="shift"> how many bits to strip from the right </param>
        /// <param name="bytes"> will contain the encoded value </param>
        public static void IntToPrefixCoded(int val, int shift, BytesRef bytes)
        {
            IntToPrefixCodedBytes(val, shift, bytes);
        }

        /// <summary>
        /// Returns prefix coded bits after reducing the precision by <code>shift</code> bits.
        /// this is method is used by <seealso cref="NumericTokenStream"/>.
        /// After encoding, {@code bytes.offset} will always be 0. </summary>
        /// <param name="val"> the numeric value </param>
        /// <param name="shift"> how many bits to strip from the right </param>
        /// <param name="bytes"> will contain the encoded value </param>
        public static void LongToPrefixCodedBytes(long val, int shift, BytesRef bytes)
        {
            if ((shift & ~0x3f) != 0) // ensure shift is 0..63
            {
                throw new System.ArgumentException("Illegal shift value, must be 0..63");
            }
            int nChars = (((63 - shift) * 37) >> 8) + 1; // i/7 is the same as (i*37)>>8 for i in 0..63
            bytes.Offset = 0;
            bytes.Length = nChars + 1; // one extra for the byte that contains the shift info
            if (bytes.Bytes.Length < bytes.Length)
            {
                bytes.Bytes = new byte[NumericUtils.BUF_SIZE_LONG]; // use the max
            }
            bytes.Bytes[0] = (byte)(SHIFT_START_LONG + shift);
            ulong sortableBits = BitConverter.ToUInt64(BitConverter.GetBytes(val), 0) ^ 0x8000000000000000L;
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
        /// Returns prefix coded bits after reducing the precision by <code>shift</code> bits.
        /// this is method is used by <seealso cref="NumericTokenStream"/>.
        /// After encoding, {@code bytes.offset} will always be 0. </summary>
        /// <param name="val"> the numeric value </param>
        /// <param name="shift"> how many bits to strip from the right </param>
        /// <param name="bytes"> will contain the encoded value </param>
        public static void IntToPrefixCodedBytes(int val, int shift, BytesRef bytes)
        {
            if ((shift & ~0x1f) != 0) // ensure shift is 0..31
            {
                throw new System.ArgumentException("Illegal shift value, must be 0..31");
            }
            int nChars = (((31 - shift) * 37) >> 8) + 1; // i/7 is the same as (i*37)>>8 for i in 0..63
            bytes.Offset = 0;
            bytes.Length = nChars + 1; // one extra for the byte that contains the shift info
            if (bytes.Bytes.Length < bytes.Length)
            {
                bytes.Bytes = new byte[NumericUtils.BUF_SIZE_LONG]; // use the max
            }
            bytes.Bytes[0] = (byte)(SHIFT_START_INT + shift);
            int sortableBits = val ^ unchecked((int)0x80000000);
            sortableBits = Number.URShift(sortableBits, shift);
            while (nChars > 0)
            {
                // Store 7 bits per byte for compatibility
                // with UTF-8 encoding of terms
                bytes.Bytes[nChars--] = (byte)(sortableBits & 0x7f);
                sortableBits = (int)((uint)sortableBits >> 7);
            }
        }

        /// <summary>
        /// Returns the shift value from a prefix encoded {@code long}. </summary>
        /// <exception cref="NumberFormatException"> if the supplied <seealso cref="BytesRef"/> is
        /// not correctly prefix encoded. </exception>
        public static int GetPrefixCodedLongShift(BytesRef val)
        {
            int shift = val.Bytes[val.Offset] - SHIFT_START_LONG;
            if (shift > 63 || shift < 0)
            {
                throw new System.FormatException("Invalid shift value (" + shift + ") in prefixCoded bytes (is encoded value really an INT?)");
            }
            return shift;
        }

        /// <summary>
        /// Returns the shift value from a prefix encoded {@code int}. </summary>
        /// <exception cref="NumberFormatException"> if the supplied <seealso cref="BytesRef"/> is
        /// not correctly prefix encoded. </exception>
        public static int GetPrefixCodedIntShift(BytesRef val)
        {
            int shift = val.Bytes[val.Offset] - SHIFT_START_INT;
            if (shift > 31 || shift < 0)
            {
                throw new System.FormatException("Invalid shift value in prefixCoded bytes (is encoded value really an INT?)");
            }
            return shift;
        }

        /// <summary>
        /// Returns a long from prefixCoded bytes.
        /// Rightmost bits will be zero for lower precision codes.
        /// this method can be used to decode a term's value. </summary>
        /// <exception cref="NumberFormatException"> if the supplied <seealso cref="BytesRef"/> is
        /// not correctly prefix encoded. </exception>
        /// <seealso cref= #longToPrefixCodedBytes </seealso>
        public static long PrefixCodedToLong(BytesRef val)
        {
            long sortableBits = 0L;
            for (int i = val.Offset + 1, limit = val.Offset + val.Length; i < limit; i++)
            {
                sortableBits <<= 7;
                var b = val.Bytes[i];
                if (b < 0)
                {
                    throw new System.FormatException("Invalid prefixCoded numerical value representation (byte " + (b & 0xff).ToString("x") + " at position " + (i - val.Offset) + " is invalid)");
                }
                sortableBits |= (byte)b;
            }
            return (long)((ulong)(sortableBits << GetPrefixCodedLongShift(val)) ^ 0x8000000000000000L);
        }

        /// <summary>
        /// Returns an int from prefixCoded bytes.
        /// Rightmost bits will be zero for lower precision codes.
        /// this method can be used to decode a term's value. </summary>
        /// <exception cref="NumberFormatException"> if the supplied <seealso cref="BytesRef"/> is
        /// not correctly prefix encoded. </exception>
        /// <seealso cref= #intToPrefixCodedBytes </seealso>
        public static int PrefixCodedToInt(BytesRef val)
        {
            long sortableBits = 0;
            for (int i = val.Offset, limit = val.Offset + val.Length; i < limit; i++)
            {
                sortableBits <<= 7;
                var b = val.Bytes[i];
                if (b < 0)
                {
                    throw new System.FormatException("Invalid prefixCoded numerical value representation (byte " + (b & 0xff).ToString("x") + " at position " + (i - val.Offset) + " is invalid)");
                }
                sortableBits |= (sbyte)b;
            }
            return (int)((sortableBits << GetPrefixCodedIntShift(val)) ^ 0x80000000);
        }

        /// <summary>
        /// Converts a <code>double</code> value to a sortable signed <code>long</code>.
        /// The value is converted by getting their IEEE 754 floating-point &quot;double format&quot;
        /// bit layout and then some bits are swapped, to be able to compare the result as long.
        /// By this the precision is not reduced, but the value can easily used as a long.
        /// The sort order (including <seealso cref="Double#NaN"/>) is defined by
        /// <seealso cref="Double#compareTo"/>; {@code NaN} is greater than positive infinity. </summary>
        /// <seealso cref= #sortableLongToDouble </seealso>
        public static long DoubleToSortableLong(double val)
        {
            long f = Number.DoubleToLongBits(val);
            if (f < 0)
            {
                f ^= 0x7fffffffffffffffL;
            }
            return f;
        }

        /// <summary>
        /// Converts a sortable <code>long</code> back to a <code>double</code>. </summary>
        /// <seealso cref= #doubleToSortableLong </seealso>
        public static double SortableLongToDouble(long val)
        {
            if (val < 0)
            {
                val ^= 0x7fffffffffffffffL;
            }
            return BitConverter.Int64BitsToDouble(val);
        }

        /// <summary>
        /// Converts a <code>float</code> value to a sortable signed <code>int</code>.
        /// The value is converted by getting their IEEE 754 floating-point &quot;float format&quot;
        /// bit layout and then some bits are swapped, to be able to compare the result as int.
        /// By this the precision is not reduced, but the value can easily used as an int.
        /// The sort order (including <seealso cref="Float#NaN"/>) is defined by
        /// <seealso cref="Float#compareTo"/>; {@code NaN} is greater than positive infinity. </summary>
        /// <seealso cref= #sortableIntToFloat </seealso>
        public static int FloatToSortableInt(float val)
        {
            int f = Number.FloatToIntBits(val);
            if (f < 0)
            {
                f ^= 0x7fffffff;
            }
            return f;
        }

        /// <summary>
        /// Converts a sortable <code>int</code> back to a <code>float</code>. </summary>
        /// <seealso cref= #floatToSortableInt </seealso>
        public static float SortableIntToFloat(int val)
        {
            if (val < 0)
            {
                val ^= 0x7fffffff;
            }
            return Number.IntBitsToFloat(val);
        }

        /// <summary>
        /// Splits a long range recursively.
        /// You may implement a builder that adds clauses to a
        /// <seealso cref="Lucene.Net.Search.BooleanQuery"/> for each call to its
        /// <seealso cref="LongRangeBuilder#addRange(BytesRef,BytesRef)"/>
        /// method.
        /// <p>this method is used by <seealso cref="NumericRangeQuery"/>.
        /// </summary>
        public static void SplitLongRange(LongRangeBuilder builder, int precisionStep, long minBound, long maxBound)
        {
            SplitRange(builder, 64, precisionStep, minBound, maxBound);
        }

        /// <summary>
        /// Splits an int range recursively.
        /// You may implement a builder that adds clauses to a
        /// <seealso cref="Lucene.Net.Search.BooleanQuery"/> for each call to its
        /// <seealso cref="IntRangeBuilder#addRange(BytesRef,BytesRef)"/>
        /// method.
        /// <p>this method is used by <seealso cref="NumericRangeQuery"/>.
        /// </summary>
        public static void SplitIntRange(IntRangeBuilder builder, int precisionStep, int minBound, int maxBound)
        {
            SplitRange(builder, 32, precisionStep, minBound, maxBound);
        }

        /// <summary>
        /// this helper does the splitting for both 32 and 64 bit. </summary>
        private static void SplitRange(object builder, int valSize, int precisionStep, long minBound, long maxBound)
        {
            if (precisionStep < 1)
            {
                throw new System.ArgumentException("precisionStep must be >=1");
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
        /// Helper that delegates to correct range builder </summary>
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
                    ((LongRangeBuilder)builder).AddRange(minBound, maxBound, shift);
                    break;

                case 32:
                    ((IntRangeBuilder)builder).AddRange((int)minBound, (int)maxBound, shift);
                    break;

                default:
                    // Should not happen!
                    throw new System.ArgumentException("valSize must be 32 or 64.");
            }
        }

        /// <summary>
        /// Callback for <seealso cref="#splitLongRange"/>.
        /// You need to overwrite only one of the methods.
        /// @lucene.internal
        /// @since 2.9, API changed non backwards-compliant in 4.0
        /// </summary>
        public abstract class LongRangeBuilder
        {
            /// <summary>
            /// Overwrite this method, if you like to receive the already prefix encoded range bounds.
            /// You can directly build classical (inclusive) range queries from them.
            /// </summary>
            public virtual void AddRange(BytesRef minPrefixCoded, BytesRef maxPrefixCoded)
            {
                throw new System.NotSupportedException();
            }

            /// <summary>
            /// Overwrite this method, if you like to receive the raw long range bounds.
            /// You can use this for e.g. debugging purposes (print out range bounds).
            /// </summary>
            public virtual void AddRange(long min, long max, int shift)
            {
                BytesRef minBytes = new BytesRef(BUF_SIZE_LONG), maxBytes = new BytesRef(BUF_SIZE_LONG);
                LongToPrefixCodedBytes(min, shift, minBytes);
                LongToPrefixCodedBytes(max, shift, maxBytes);
                AddRange(minBytes, maxBytes);
            }
        }

        /// <summary>
        /// Callback for <seealso cref="#splitIntRange"/>.
        /// You need to overwrite only one of the methods.
        /// @lucene.internal
        /// @since 2.9, API changed non backwards-compliant in 4.0
        /// </summary>
        public abstract class IntRangeBuilder
        {
            /// <summary>
            /// Overwrite this method, if you like to receive the already prefix encoded range bounds.
            /// You can directly build classical range (inclusive) queries from them.
            /// </summary>
            public virtual void AddRange(BytesRef minPrefixCoded, BytesRef maxPrefixCoded)
            {
                throw new System.NotSupportedException();
            }

            /// <summary>
            /// Overwrite this method, if you like to receive the raw int range bounds.
            /// You can use this for e.g. debugging purposes (print out range bounds).
            /// </summary>
            public virtual void AddRange(int min, int max, int shift)
            {
                BytesRef minBytes = new BytesRef(BUF_SIZE_INT), maxBytes = new BytesRef(BUF_SIZE_INT);
                IntToPrefixCodedBytes(min, shift, minBytes);
                IntToPrefixCodedBytes(max, shift, maxBytes);
                AddRange(minBytes, maxBytes);
            }
        }

        /// <summary>
        /// Filters the given <seealso cref="TermsEnum"/> by accepting only prefix coded 64 bit
        /// terms with a shift value of <tt>0</tt>.
        /// </summary>
        /// <param name="termsEnum">
        ///          the terms enum to filter </param>
        /// <returns> a filtered <seealso cref="TermsEnum"/> that only returns prefix coded 64 bit
        ///         terms with a shift value of <tt>0</tt>. </returns>
        public static TermsEnum FilterPrefixCodedLongs(TermsEnum termsEnum)
        {
            return new FilteredTermsEnumAnonymousInnerClassHelper(termsEnum);
        }

        private class FilteredTermsEnumAnonymousInnerClassHelper : FilteredTermsEnum
        {
            public FilteredTermsEnumAnonymousInnerClassHelper(TermsEnum termsEnum)
                : base(termsEnum, false)
            {
            }

            protected override AcceptStatus Accept(BytesRef term)
            {
                return NumericUtils.GetPrefixCodedLongShift(term) == 0 ? AcceptStatus.YES : AcceptStatus.END;
            }
        }

        /// <summary>
        /// Filters the given <seealso cref="TermsEnum"/> by accepting only prefix coded 32 bit
        /// terms with a shift value of <tt>0</tt>.
        /// </summary>
        /// <param name="termsEnum">
        ///          the terms enum to filter </param>
        /// <returns> a filtered <seealso cref="TermsEnum"/> that only returns prefix coded 32 bit
        ///         terms with a shift value of <tt>0</tt>. </returns>
        public static TermsEnum FilterPrefixCodedInts(TermsEnum termsEnum)
        {
            return new FilteredTermsEnumAnonymousInnerClassHelper2(termsEnum);
        }

        private class FilteredTermsEnumAnonymousInnerClassHelper2 : FilteredTermsEnum
        {
            public FilteredTermsEnumAnonymousInnerClassHelper2(TermsEnum termsEnum)
                : base(termsEnum, false)
            {
            }

            protected override AcceptStatus Accept(BytesRef term)
            {
                return NumericUtils.GetPrefixCodedIntShift(term) == 0 ? AcceptStatus.YES : AcceptStatus.END;
            }
        }
    }
}