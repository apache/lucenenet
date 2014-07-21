

namespace Lucene.Net
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Summary description for ArrayUtil
    /// </summary>
    public static class ArrayUtil
    {

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