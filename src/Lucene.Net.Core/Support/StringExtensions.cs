using Lucene.Net.Util;
using System;
using System.Text;

namespace Lucene.Net.Support
{
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
        /// <item>Compares the strings using lexographic sorting rules</item>
        /// <item>Performs a culture-insensitive comparison</item>
        /// </list>
        /// This method is a convenience to replace the .NET CompareTo method 
        /// on all strings, provided the logic does not expect specific values
        /// but is simply comparing them with <code>></code> or <code><</code>.
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
    }
}