using System;
using System.Collections.Generic;
using System.Linq;

namespace Lucene.Net.Support
{
    public static class Arrays
    {
        public static int GetHashCode<T>(T[] a)
        {
            if (a == null)
                return 0;

            int hash = 17;

            foreach (var item in a)
            {
                hash = hash * 23 + (item == null ? 0 : item.GetHashCode());
            }

            return hash;
        }

        /// <summary>
        /// Assigns the specified value to each element of the specified array.
        /// </summary>
        /// <typeparam name="T">the type of the array</typeparam>
        /// <param name="a">the array to be filled</param>
        /// <param name="val">the value to be stored in all elements of the array</param>
        public static void Fill<T>(T[] a, T val)
        {
            for (int i = 0; i < a.Length; i++)
            {
                a[i] = val;
            }
        }

        /// <summary>
        /// Assigns the specified long value to each element of the specified
        /// range of the specified array of longs.  The range to be filled
        /// extends from index <paramref name="fromIndex"/>, inclusive, to index
        /// <paramref name="toIndex"/>, exclusive.  (If <c>fromIndex==toIndex</c>, the
        /// range to be filled is empty.)
        /// </summary>
        /// <typeparam name="T">the type of the array</typeparam>
        /// <param name="a">the array to be filled</param>
        /// <param name="fromIndex">
        /// the index of the first element (inclusive) to be
        /// filled with the specified value
        /// </param>
        /// <param name="toIndex">
        /// the index of the last element (exclusive) to be
        /// filled with the specified value
        /// </param>
        /// <param name="val">the value to be stored in all elements of the array</param>
        /// <exception cref="ArgumentException">if <c>fromIndex &gt; toIndex</c></exception>
        /// <exception cref="ArgumentOutOfRangeException">if <c>fromIndex &lt; 0</c> or <c>toIndex &gt; a.Length</c></exception>
        public static void Fill<T>(T[] a, int fromIndex, int toIndex, T val)
        {
            //Java Arrays.fill exception logic
            if (fromIndex > toIndex)
                throw new ArgumentException("fromIndex(" + fromIndex + ") > toIndex(" + toIndex + ")");
            if (fromIndex < 0)
                throw new ArgumentOutOfRangeException("fromIndex");
            if (toIndex > a.Length)
                throw new ArgumentOutOfRangeException("toIndex");

            for (int i = fromIndex; i < toIndex; i++)
            {
                a[i] = val;
            }
        }

        /// <summary>
        /// Compares the entire members of one array whith the other one.
        /// </summary>
        /// <param name="a">The array to be compared.</param>
        /// <param name="b">The array to be compared with.</param>
        /// <returns>Returns true if the two specified arrays of Objects are equal
        /// to one another. The two arrays are considered equal if both arrays
        /// contain the same number of elements, and all corresponding pairs of
        /// elements in the two arrays are equal. Two objects e1 and e2 are
        /// considered equal if (e1==null ? e2==null : e1.equals(e2)). In other
        /// words, the two arrays are equal if they contain the same elements in
        /// the same order. Also, two array references are considered equal if
        /// both are null.</returns>
        public static bool Equals<T>(T[] a, T[] b)
        {
            if (a == null)
                return b == null;

            if (a.Length != b.Length)
                return false;

            for (int i = 0; i < a.Length; i++)
            {
                if (!object.Equals(a[i], b[i]))
                    return false;
            }

            return true;
        }

        public static T[] CopyOf<T>(T[] original, int newLength)
        {
            T[] newArray = new T[newLength];

            for (int i = 0; i < Math.Min(original.Length, newLength); i++)
            {
                newArray[i] = original[i];
            }

            return newArray;
        }

        public static T[] CopyOfRange<T>(T[] original, int startIndexInc, int endIndexExc)
        {
            int newLength = endIndexExc - startIndexInc;
            T[] newArray = new T[newLength];

            for (int i = startIndexInc, j = 0; i < endIndexExc; i++, j++)
            {
                newArray[j] = original[i];
            }

            return newArray;
        }

        public static string ToString(IEnumerable<string> values)
        {
            if (values == null)
                return string.Empty;

            return string.Join(", ", values);
        }

        public static string ToString<T>(IEnumerable<T> values)
        {
            if (values == null)
                return string.Empty;

            return string.Join(", ", values);
        }

        public static List<T> AsList<T>(params T[] objects)
        {
            return objects.ToList();
        }
    }
}
