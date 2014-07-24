

namespace Lucene.Net
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Summary description for ArrayUtil
    /// </summary>
    public static class ArrayUtil
    {

        public static int ParseInt(this char[] chars, int offset = 0, int limit = -1, int radix = 10)
        {
            Check.NotNull("chars", chars);
            Check.Condition(chars.Length == 0, "chars", "The parameter, chars, must not be an empty array.");
            Check.Condition(offset < 0 || offset > chars.Length, "offset", 
                "The parameter, offset ({0}), must be greater than -1 and less than parameter, chars.Length ({1}).", offset, chars.Length);

            if (limit < 0)
                limit = chars.Length;

            Check.Condition(
                limit > chars.Length, "limit", 
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

            return Parse(chars, offset, limit, radix, negative);
        }

        private static int Parse(char[] chars, int offset, int len, int radix, bool negative)
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