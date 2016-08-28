using Lucene.Net.Util;
using System.Text;

namespace Lucene.Net.Support
{
    public static class StringSupport
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


        public static int CodePointCount(this string str, int beginIndex, int endIndex)
        {
            return Character.CodePointCount(str, beginIndex, endIndex);
        }
    }
}