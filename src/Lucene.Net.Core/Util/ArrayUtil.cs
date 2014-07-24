

namespace Lucene.Net
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Utility methods for manipulating arrays.
    /// </summary>
    public static class ArrayUtil
    {


        /// <summary>
        /// Parses the string argument as if it was an int value and returns the result.
        /// </summary>
        /// <param name="chars">A string representation of an int quantity. </param>
        /// <param name="offset">The position in the <paramref name="chars"/> array to start parsing.</param>
        /// <param name="limit">The number of characters to parse after the <paramref name="offset"/>.</param>
        /// <param name="radix"> The base to use for conversion. </param>
        /// <returns> int the value represented by the argument </returns>
        /// <exception cref="ArgumentNullException">Throws when <paramref name="chars"/> is null.</exception>
        /// <exception cref="ArgumentException">Throws when <paramref name="chars"/> is empty.</exception>
        /// <exception cref="ArgumentOutOfRangeException">
        ///     <list>
        ///         <item>Throws when <paramref name="offset"/> is less than zero or greater than <paramref name="chars"/>.Length.</item>
        ///         <item>Throws when <paramref name="limit"/> is greater than <paramref name="chars"/>.Length.</item>
        ///         <item>Throws when <paramref name="radix"/> is less than 2 or greater than 36.</item>
        ///     </list>
        /// </exception>
        /// <exception cref="FormatException">Throws when a character cannot be translated into a integer.</exception>
        public static int ParseInt(this string chars, int offset = 0, int limit = -1, int radix = 10)
        {
            Check.NotNull("chars", chars);

            return ParseInt(chars.ToCharArray(), offset, limit, radix);
        }

        /// <summary>
        /// Parses the string argument as if it was an int value and returns the result.
        /// </summary>
        /// <param name="chars">A string representation of an int quantity. </param>
        /// <param name="offset">The position in the <paramref name="chars"/> array to start parsing.</param>
        /// <param name="limit">The number of characters to parse after the <paramref name="offset"/>.</param>
        /// <param name="radix"> The base to use for conversion. </param>
        /// <returns>The integer value that was parsed from <pararef name="chars" />.</returns>
        /// <exception cref="ArgumentNullException">Throws when <paramref name="chars"/> is null.</exception>
        /// <exception cref="ArgumentException">Throws when <paramref name="chars"/> is empty.</exception>
        /// <exception cref="ArgumentOutOfRangeException">
        ///     <list>
        ///         <item>Throws when <paramref name="offset"/> is less than zero or greater than <paramref name="chars"/>.Length.</item>
        ///         <item>Throws when <paramref name="limit"/> is greater than <paramref name="chars"/>.Length.</item>
        ///         <item>Throws when <paramref name="radix"/> is less than 2 or greater than 36.</item>
        ///     </list>
        /// </exception>
        /// <exception cref="FormatException">Throws when a character cannot be translated into a integer.</exception>
        public static int ParseInt(this char[] chars, int offset = 0, int limit = -1, int radix = 10)
        {
            Check.NotNull("chars", chars, false);
            Check.Condition(chars.Length == 0, "chars", "The parameter, chars, must not be an empty array.");
            Check.Condition<ArgumentOutOfRangeException>(
                offset < 0 || offset > chars.Length,  
                "The parameter, offset ({0}), must be greater than -1 and less than parameter, chars.Length ({1}).", offset, chars.Length);

            if (limit < 0)
                limit = chars.Length;

            Check.Condition<ArgumentOutOfRangeException>(
                limit > chars.Length,
                "The parameter, limit ({0}), must be less than or equal to the length of the parameter, chars ({1}).", 
                 limit, chars.Length);

            const int minRadix = 2;
            const int maxRadix = 36;

            Check.Condition<ArgumentOutOfRangeException>(
                radix < minRadix || radix > maxRadix,
                "The parameter, radix ({0}), must be greater than {1} and less than {2}.", radix, minRadix, maxRadix);

            bool negative = chars[offset] == '-';

            Check.Condition<FormatException>(negative && limit == 1, "A negative sign, '-', cannot be converted into an integer.");

            if (negative == true)
            {
                offset++;
                limit--;
            }

            return ParseInt(chars, offset, limit, radix, negative);
        }

        // parse() in Java
        private static int ParseInt(char[] chars, int offset, int len, int radix, bool negative)
        {
            int max = int.MinValue / radix;
            int result = 0;
            for (int i = 0; i < len; i++)
            {
                int digit = (int)System.Char.GetNumericValue(chars[i + offset]);
                if (digit == -1)
                {
                    throw new System.FormatException("Unable to parse");
                }
                if (max > result)
                {
                    throw new System.FormatException("Unable to parse");
                }
                int next = result * radix - digit;
                if (next > result)
                {
                    throw new System.FormatException("Unable to parse");
                }
                result = next;
            }
  
            if (!negative)
            {
                result = -result;
                if (result < 0)
                {
                    throw new System.FormatException("Unable to parse");
                }
            }
            return result;
        }


        private class NaturalComparator<T> : IComparer<T> where T : IComparable<T>
        {
            public int Compare(T x, T y)
            {
                return x.CompareTo(y);
            }
        }

        public static IComparer<T> NaturalComparer<T>() where T : IComparable<T>
        {
            return new NaturalComparator<T>();
        }

        public static T[] CopyOf<T>(this T[] array, int length)
        {
            var copy = new T[length];
            Array.Copy(array, copy, length);

            return copy;
        }

        public static T[] CopyOfRange<T>(this T[] array, int start, int end)
        {
            //Check.SliceInRangeOfLength(start, end, array.Length);

            var length = end - start;
            var copy = new T[length];
            Array.Copy(array, start, copy, 0, length);

            return copy;
        }

        public static void Swap<T>(this T[] array, int x, int y)
        {
            T tmp = array[x];
            array[x] = array[y];
            array[y] = tmp;
        }
    }
}