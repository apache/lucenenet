using Lucene.Net.Util;
using System;
using System.Text;

namespace Lucene.Net.Support
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

    public static class StringExtensions
    {
        public static byte[] GetBytes(this string str, Encoding enc)
        {
            return enc.GetBytes(str);
        }

        public static BytesRef ToBytesRefArray(this string str, Encoding enc)
        {
            return new BytesRef(str.GetBytes(enc));
        }

        /// <summary>
        /// This method mimics the Java String.compareTo(String) method in that it
        /// <list type="number">
        /// <item><description>Compares the strings using lexographic sorting rules</description></item>
        /// <item><description>Performs a culture-insensitive comparison</description></item>
        /// </list>
        /// This method is a convenience to replace the .NET CompareTo method 
        /// on all strings, provided the logic does not expect specific values
        /// but is simply comparing them with <c>&gt;</c> or <c>&lt;</c>.
        /// </summary>
        /// <param name="str"></param>
        /// <param name="value">The string to compare with.</param>
        /// <returns>
        /// An integer that indicates the lexical relationship between the two comparands.
        /// Less than zero indicates the comparison value is greater than the current string.
        /// Zero indicates the strings are equal.
        /// Greater than zero indicates the comparison value is less than the current string.
        /// </returns>
        public static int CompareToOrdinal(this string str, string value)
        {
            return string.CompareOrdinal(str, value);
        }


        public static int CodePointAt(this string str, int index)
        {
            return Character.CodePointAt(str, index);
        }

        /// <summary>
        /// Returns the number of Unicode code points in the specified text
        /// range of this <see cref="string"/>. The text range begins at the
        /// specified <paramref name="beginIndex"/> and extends to the
        /// <see cref="char"/> at index <c>endIndex - 1</c>. Thus the
        /// length (in <see cref="char"/>s) of the text range is
        /// <c>endIndex-beginIndex</c>. Unpaired surrogates within
        /// the text range count as one code point each.
        /// </summary>
        /// <param name="str"></param>
        /// <param name="beginIndex">the index to the first <see cref="char"/> of the text range.</param>
        /// <param name="endIndex">the index after the last <see cref="char"/> of the text range.</param>
        /// <returns>the number of Unicode code points in the specified text range</returns>
        /// <exception cref="IndexOutOfRangeException">if the <paramref name="beginIndex"/> is negative, or
        /// <paramref name="endIndex"/> is larger than the length of this <see cref="string"/>, or
        /// <paramref name="beginIndex"/> is larger than <paramref name="endIndex"/>.</exception>
        public static int CodePointCount(this string str, int beginIndex, int endIndex)
        {
            if (beginIndex < 0 || endIndex > str.Length || beginIndex > endIndex)
            {
                throw new IndexOutOfRangeException();
            }
            return Character.CodePointCountImpl(str.ToCharArray(), beginIndex, endIndex - beginIndex);
        }

        public static int OffsetByCodePoints(this string seq, int index,
                                         int codePointOffset)
        {
            return Character.OffsetByCodePoints(seq, index, codePointOffset);
        }


        /// <summary>
        /// Convenience method to wrap a string in a <see cref="StringCharSequenceWrapper"/>
        /// so a string can be used as <see cref="ICharSequence"/> in .NET.
        /// </summary>
        public static ICharSequence ToCharSequence(this string str)
        {
            return new StringCharSequenceWrapper(str);
        }

        /// <summary>
        /// Returns the index within this string of the first occurrence of the
        /// specified <paramref name="codePoint"/>.
        /// </summary>
        /// <param name="str">this string</param>
        /// <param name="codePoint">a codePoint representing a single character or surrogate pair</param>
        /// <returns>the index of the first occurrence of the character (or surrogate pair) in the string, 
        /// or <c>-1</c> if the character (or surrogate pair) doesn't occur.</returns>
        public static int IndexOf(this string str, int codePoint)
        {
            if (codePoint >= 0 && codePoint < Character.MIN_SUPPLEMENTARY_CODE_POINT)
            {
                // handle most cases here (codePoint is a BMP code point)
                return str.IndexOf((char)codePoint);
            }
            else if (codePoint >= Character.MIN_CODE_POINT && codePoint <= Character.MAX_CODE_POINT)
            {
                // codePoint is a surogate pair
                char[] pair = Character.ToChars(codePoint);
                char hi = pair[0];
                char lo = pair[1];
                for (int i = 0; i < str.Length - 1; i++)
                {
                    if (str[i] == hi && str[i + 1] == lo)
                    {
                        return i;
                    }
                }
            }

            // codePoint is negative or not found in string
            return -1;
        }
    }
}