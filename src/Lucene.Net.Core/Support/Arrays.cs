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

        public static void Fill<T>(T[] a, T val)
        {
            for (int i = 0; i < a.Length; i++)
            {
                a[i] = val;
            }
        }

        public static void Fill<T>(T[] a, int fromIndex, int toIndex, T val)
        {
            //Java Arrays.fill exception logic
            if(fromIndex > toIndex || fromIndex < 0 || toIndex > a.Length)
                throw new ArgumentOutOfRangeException("fromIndex");

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

        public static int GetHashCode<T>(IEnumerable<T> values)
        {
            if (values == null)
                return 0;

            const int prime = 17;

            int hashCode = 23;

            unchecked
            {
                foreach (var value in values)
                {
                    if (value == null)
                        continue;

                    hashCode = hashCode * prime + value.GetHashCode();
                }
            }

            return hashCode;
        }

        public static List<T> AsList<T>(params T[] objects)
        {
            return objects.ToList();
        }
    }
}
