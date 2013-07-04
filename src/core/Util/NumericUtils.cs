/* 
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
 * (the "License"); you may not use this file except in compliance with
 * the License.  You may obtain a copy of the License at
 * 
 * http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using Lucene.Net.Documents;
using Lucene.Net.Search;
using Lucene.Net.Support;
using NumericTokenStream = Lucene.Net.Analysis.NumericTokenStream;
using Lucene.Net.Index;

namespace Lucene.Net.Util
{

    /// <summary> This is a helper class to generate prefix-encoded representations for numerical values
    /// and supplies converters to represent float/double values as sortable integers/longs.
    /// 
    /// <p/>To quickly execute range queries in Apache Lucene, a range is divided recursively
    /// into multiple intervals for searching: The center of the range is searched only with
    /// the lowest possible precision in the trie, while the boundaries are matched
    /// more exactly. This reduces the number of terms dramatically.
    /// 
    /// <p/>This class generates terms to achive this: First the numerical integer values need to
    /// be converted to strings. For that integer values (32 bit or 64 bit) are made unsigned
    /// and the bits are converted to ASCII chars with each 7 bit. The resulting string is
    /// sortable like the original integer value. Each value is also prefixed
    /// (in the first char) by the <c>shift</c> value (number of bits removed) used
    /// during encoding.
    /// 
    /// <p/>To also index floating point numbers, this class supplies two methods to convert them
    /// to integer values by changing their bit layout: <see cref="DoubleToSortableLong" />,
    /// <see cref="FloatToSortableInt" />. You will have no precision loss by
    /// converting floating point numbers to integers and back (only that the integer form
    /// is not usable). Other data types like dates can easily converted to longs or ints (e.g.
    /// date to long: <see cref="DateTime" />).
    /// 
    /// <p/>For easy usage, the trie algorithm is implemented for indexing inside
    /// <see cref="NumericTokenStream" /> that can index <c>int</c>, <c>long</c>,
    /// <c>float</c>, and <c>double</c>. For querying,
    /// <see cref="NumericRangeQuery{T}" /> and <see cref="NumericRangeFilter{T}" /> implement the query part
    /// for the same data types.
    /// 
    /// <p/>This class can also be used, to generate lexicographically sortable (according
    /// <see cref="String.CompareTo(String)" />) representations of numeric data types for other
    /// usages (e.g. sorting).
    /// 
    /// <p/><font color="red"><b>NOTE:</b> This API is experimental and
    /// might change in incompatible ways in the next release.</font>
    /// 
    /// </summary>
    /// <since> 2.9
    /// </since>
    public static class NumericUtils
    {
        /// <summary> The default precision step used by <see cref="NumericField" />, <see cref="NumericTokenStream" />,
        /// <see cref="NumericRangeQuery{T}" />, and <see cref="NumericRangeFilter{T}" /> as default
        /// </summary>
        public const int PRECISION_STEP_DEFAULT = 4;

        /// <summary> Expert: Longs are stored at lower precision by shifting off lower bits. The shift count is
        /// stored as <c>SHIFT_START_LONG+shift</c> in the first character
        /// </summary>
        public static char SHIFT_START_LONG = (char)0x20;

        /// <summary> Expert: The maximum term length (used for <c>char[]</c> buffer size)
        /// for encoding <c>long</c> values.
        /// </summary>
        /// <seealso cref="LongToPrefixCoded(long,int,char[])">
        /// </seealso>
        public const int BUF_SIZE_LONG = 63 / 7 + 2;

        /// <summary> Expert: Integers are stored at lower precision by shifting off lower bits. The shift count is
        /// stored as <c>SHIFT_START_INT+shift</c> in the first character
        /// </summary>
        public static char SHIFT_START_INT = (char)0x60;

        /// <summary> Expert: The maximum term length (used for <c>char[]</c> buffer size)
        /// for encoding <c>int</c> values.
        /// </summary>
        /// <seealso cref="IntToPrefixCoded(int,int,char[])">
        /// </seealso>
        public const int BUF_SIZE_INT = 31 / 7 + 2;

        /// <summary> Expert: Returns prefix coded bits after reducing the precision by <c>shift</c> bits.
        /// This is method is used by <see cref="NumericTokenStream" />.
        /// </summary>
        /// <param name="val">the numeric value
        /// </param>
        /// <param name="shift">how many bits to strip from the right
        /// </param>
        /// <param name="bytes">that will contain the encoded chars
        /// </param>
        /// <returns> the hash code for indexing
        /// </returns>
        public static int LongToPrefixCoded(long val, int shift, BytesRef bytes)
        {
            LongToPrefixCodedBytes(val, shift, bytes);
            return bytes.GetHashCode();
        }

        /// <summary> Expert: Returns prefix coded bits after reducing the precision by <c>shift</c> bits.
        /// This is method is used by <see cref="NumericTokenStream" />.
        /// </summary>
        /// <param name="val">the numeric value
        /// </param>
        /// <param name="shift">how many bits to strip from the right
        /// </param>
        /// <param name="bytes">that will contain the encoded chars
        /// </param>
        /// <returns> the hash code for indexing
        /// </returns>
        public static int IntToPrefixCoded(int val, int shift, BytesRef bytes)
        {
            IntToPrefixCodedBytes(val, shift, bytes);
            return bytes.GetHashCode();
        }

        public static void LongToPrefixCodedBytes(long val, int shift, BytesRef bytes)
        {
            if ((shift & ~0x3f) != 0)  // ensure shift is 0..63
                throw new ArgumentException("Illegal shift value, must be 0..63");
            int nChars = (((63 - shift) * 37) >> 8) + 1;    // i/7 is the same as (i*37)>>8 for i in 0..63
            bytes.offset = 0;
            bytes.length = nChars + 1;   // one extra for the byte that contains the shift info
            if (bytes.bytes.Length < bytes.length)
            {
                bytes.bytes = new sbyte[NumericUtils.BUF_SIZE_LONG];  // use the max
            }
            bytes.bytes[0] = (sbyte)(SHIFT_START_LONG + shift);
            ulong sortableBits = (ulong)val ^ 0x8000000000000000L; // TODO: is this the right .NET translation?
            sortableBits >>= shift;
            while (nChars > 0)
            {
                // Store 7 bits per byte for compatibility
                // with UTF-8 encoding of terms
                bytes.bytes[nChars--] = (sbyte)(sortableBits & 0x7f);
                sortableBits >>= 7;
            }
        }

        public static void IntToPrefixCodedBytes(int val, int shift, BytesRef bytes)
        {
            if ((shift & ~0x1f) != 0)  // ensure shift is 0..31
                throw new ArgumentException("Illegal shift value, must be 0..31");
            int nChars = (((31 - shift) * 37) >> 8) + 1;    // i/7 is the same as (i*37)>>8 for i in 0..63
            bytes.offset = 0;
            bytes.length = nChars + 1;   // one extra for the byte that contains the shift info
            if (bytes.bytes.Length < bytes.length)
            {
                bytes.bytes = new sbyte[NumericUtils.BUF_SIZE_LONG];  // use the max
            }
            bytes.bytes[0] = (sbyte)(SHIFT_START_INT + shift);
            uint sortableBits = (uint)val ^ 0x80000000; // TODO: is this the right .NET translation?
            sortableBits >>= shift;
            while (nChars > 0)
            {
                // Store 7 bits per byte for compatibility
                // with UTF-8 encoding of terms
                bytes.bytes[nChars--] = (sbyte)(sortableBits & 0x7f);
                sortableBits >>= 7;
            }
        }

        public static int GetPrefixCodedLongShift(BytesRef val)
        {
            int shift = val.bytes[val.offset] - SHIFT_START_LONG;
            if (shift > 63 || shift < 0)
                throw new FormatException("Invalid shift value (" + shift + ") in prefixCoded bytes (is encoded value really an INT?)");
            return shift;
        }

        public static int GetPrefixCodedIntShift(BytesRef val)
        {
            int shift = val.bytes[val.offset] - SHIFT_START_INT;
            if (shift > 31 || shift < 0)
                throw new FormatException("Invalid shift value in prefixCoded bytes (is encoded value really an INT?)");
            return shift;
        }

        public static long PrefixCodedToLong(BytesRef val)
        {
            long sortableBits = 0L;
            for (int i = val.offset + 1, limit = val.offset + val.length; i < limit; i++)
            {
                sortableBits <<= 7;
                sbyte b = val.bytes[i];
                if (b < 0)
                {
                    throw new FormatException(
                      "Invalid prefixCoded numerical value representation (byte " +
                      (b & 0xff).ToString("X") + " at position " + (i - val.offset) + " is invalid)"
                    );
                }
                sortableBits |= (byte)b;
            }
            return (long)((ulong)(sortableBits << GetPrefixCodedLongShift(val)) ^ 0x8000000000000000L);
        }


        public static int PrefixCodedToInt(BytesRef val)
        {
            int sortableBits = 0;
            for (int i = val.offset + 1, limit = val.offset + val.length; i < limit; i++)
            {
                sortableBits <<= 7;
                sbyte b = val.bytes[i];
                if (b < 0)
                {
                    throw new FormatException(
                      "Invalid prefixCoded numerical value representation (byte " +
                      (b & 0xff).ToString("X") + " at position " + (i - val.offset) + " is invalid)"
                    );
                }
                sortableBits |= (byte)b;
            }
            return (int)((uint)(sortableBits << GetPrefixCodedIntShift(val)) ^ 0x80000000);
        }

        /// <summary> Converts a <c>double</c> value to a sortable signed <c>long</c>.
        /// The value is converted by getting their IEEE 754 floating-point &quot;double format&quot;
        /// bit layout and then some bits are swapped, to be able to compare the result as long.
        /// By this the precision is not reduced, but the value can easily used as a long.
        /// </summary>
        /// <seealso cref="SortableLongToDouble">
        /// </seealso>
        public static long DoubleToSortableLong(double val)
        {
            long f = BitConverter.DoubleToInt64Bits(val);   // {{Aroush-2.9}} will this work the same as 'java.lang.Double.doubleToRawLongBits()'?
            if (f < 0)
                f ^= 0x7fffffffffffffffL;
            return f;
        }

        /// <summary> Converts a sortable <c>long</c> back to a <c>double</c>.</summary>
        /// <seealso cref="DoubleToSortableLong">
        /// </seealso>
        public static double SortableLongToDouble(long val)
        {
            if (val < 0)
                val ^= 0x7fffffffffffffffL;
            return BitConverter.Int64BitsToDouble(val);
        }

        /// <summary> Convenience method: this just returns:
        /// sortableLongToDouble(prefixCodedToLong(val))
        /// </summary>
        public static double PrefixCodedToDouble(BytesRef val)
        {
            return SortableLongToDouble(PrefixCodedToLong(val));
        }

        /// <summary> Converts a <c>float</c> value to a sortable signed <c>int</c>.
        /// The value is converted by getting their IEEE 754 floating-point &quot;float format&quot;
        /// bit layout and then some bits are swapped, to be able to compare the result as int.
        /// By this the precision is not reduced, but the value can easily used as an int.
        /// </summary>
        /// <seealso cref="SortableIntToFloat">
        /// </seealso>
        public static int FloatToSortableInt(float val)
        {
            int f = BitConverter.ToInt32(BitConverter.GetBytes(val), 0);
            if (f < 0)
                f ^= 0x7fffffff;
            return f;
        }
        
        /// <summary> Converts a sortable <c>int</c> back to a <c>float</c>.</summary>
        /// <seealso cref="FloatToSortableInt">
        /// </seealso>
        public static float SortableIntToFloat(int val)
        {
            if (val < 0)
                val ^= 0x7fffffff;
            return BitConverter.ToSingle(BitConverter.GetBytes(val), 0);
        }

        /// <summary> Convenience method: this just returns:
        /// sortableIntToFloat(prefixCodedToInt(val))
        /// </summary>
        public static float PrefixCodedToFloat(BytesRef val)
        {
            return SortableIntToFloat(PrefixCodedToInt(val));
        }

        /// <summary> Expert: Splits a long range recursively.
        /// You may implement a builder that adds clauses to a
        /// <see cref="Lucene.Net.Search.BooleanQuery" /> for each call to its
        /// <see cref="LongRangeBuilder.AddRange(String,String)" />
        /// method.
        /// <p/>This method is used by <see cref="NumericRangeQuery{T}" />.
        /// </summary>
        public static void SplitLongRange(LongRangeBuilder builder, int precisionStep, long minBound, long maxBound)
        {
            SplitRange(builder, 64, precisionStep, minBound, maxBound);
        }

        /// <summary> Expert: Splits an int range recursively.
        /// You may implement a builder that adds clauses to a
        /// <see cref="Lucene.Net.Search.BooleanQuery" /> for each call to its
        /// <see cref="IntRangeBuilder.AddRange(String,String)" />
        /// method.
        /// <p/>This method is used by <see cref="NumericRangeQuery{T}" />.
        /// </summary>
        public static void SplitIntRange(IntRangeBuilder builder, int precisionStep, int minBound, int maxBound)
        {
            SplitRange(builder, 32, precisionStep, (long)minBound, (long)maxBound);
        }

        /// <summary>This helper does the splitting for both 32 and 64 bit. </summary>
        private static void SplitRange(System.Object builder, int valSize, int precisionStep, long minBound, long maxBound)
        {
            if (precisionStep < 1)
                throw new System.ArgumentException("precisionStep must be >=1");
            if (minBound > maxBound)
                return;
            for (int shift = 0; ; shift += precisionStep)
            {
                // calculate new bounds for inner precision
                long diff = 1L << (shift + precisionStep);
                long mask = ((1L << precisionStep) - 1L) << shift;
                bool hasLower = (minBound & mask) != 0L;
                bool hasUpper = (maxBound & mask) != mask;
                long nextMinBound = (hasLower ? (minBound + diff) : minBound) & ~mask;
                long nextMaxBound = (hasUpper ? (maxBound - diff) : maxBound) & ~mask;
                bool lowerWrapped = nextMinBound < minBound,
                     upperWrapped = nextMaxBound > maxBound;

                if (shift + precisionStep >= valSize || nextMinBound > nextMaxBound || lowerWrapped || upperWrapped)
                {
                    // We are in the lowest precision or the next precision is not available.
                    AddRange(builder, valSize, minBound, maxBound, shift);
                    // exit the split recursion loop
                    break;
                }

                if (hasLower)
                    AddRange(builder, valSize, minBound, minBound | mask, shift);
                if (hasUpper)
                    AddRange(builder, valSize, maxBound & ~mask, maxBound, shift);

                // recurse to next precision
                minBound = nextMinBound;
                maxBound = nextMaxBound;
            }
        }

        /// <summary>Helper that delegates to correct range builder </summary>
        private static void AddRange(System.Object builder, int valSize, long minBound, long maxBound, int shift)
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

        /// <summary> Expert: Callback for <see cref="SplitLongRange" />.
        /// You need to overwrite only one of the methods.
        /// <p/><font color="red"><b>NOTE:</b> This is a very low-level interface,
        /// the method signatures may change in later versions.</font>
        /// </summary>
        public abstract class LongRangeBuilder
        {

            /// <summary> Overwrite this method, if you like to receive the already prefix encoded range bounds.
            /// You can directly build classical (inclusive) range queries from them.
            /// </summary>
            public virtual void AddRange(BytesRef minPrefixCoded, BytesRef maxPrefixCoded)
            {
                throw new System.NotSupportedException();
            }

            /// <summary> Overwrite this method, if you like to receive the raw long range bounds.
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

        /// <summary> Expert: Callback for <see cref="SplitIntRange" />.
        /// You need to overwrite only one of the methods.
        /// <p/><font color="red"><b>NOTE:</b> This is a very low-level interface,
        /// the method signatures may change in later versions.</font>
        /// </summary>
        public abstract class IntRangeBuilder
        {

            /// <summary> Overwrite this method, if you like to receive the already prefix encoded range bounds.
            /// You can directly build classical range (inclusive) queries from them.
            /// </summary>
            public virtual void AddRange(BytesRef minPrefixCoded, BytesRef maxPrefixCoded)
            {
                throw new System.NotSupportedException();
            }

            /// <summary> Overwrite this method, if you like to receive the raw int range bounds.
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

        public static TermsEnum FilterPrefixCodedLongs(TermsEnum termsEnum)
        {
            return new AnonymousFilterPrefixCodedLongsTermsEnum(termsEnum, false);
        }

        private sealed class AnonymousFilterPrefixCodedLongsTermsEnum : FilteredTermsEnum
        {
            public AnonymousFilterPrefixCodedLongsTermsEnum(TermsEnum tenum, bool startWithSeek)
                : base(tenum, startWithSeek)
            {
            }

            protected override AcceptStatus Accept(BytesRef term)
            {
                return NumericUtils.GetPrefixCodedLongShift(term) == 0 ? AcceptStatus.YES : AcceptStatus.END;
            }
        }

        public static TermsEnum FilterPrefixCodedInts(TermsEnum termsEnum)
        {
            return new AnonymousFilterPrefixCodedIntsTermsEnum(termsEnum, false);
        }

        private sealed class AnonymousFilterPrefixCodedIntsTermsEnum : FilteredTermsEnum
        {
            public AnonymousFilterPrefixCodedIntsTermsEnum(TermsEnum tenum, bool startWithSeek)
                : base(tenum, startWithSeek)
            {
            }

            protected override AcceptStatus Accept(BytesRef term)
            {
                return NumericUtils.GetPrefixCodedIntShift(term) == 0 ? AcceptStatus.YES : AcceptStatus.END;
            }
        }
    }
}