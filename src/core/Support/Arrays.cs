using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Support
{
    public static class Arrays
    {
        public static void Fill<T>(T[] a, T val)
        {
            for (int i = 0; i < a.Length; i++)
            {
                a[i] = val;
            }
        }

        public static void Fill<T>(T[] a, int fromIndex, int toIndex, T val)
        {
            if (fromIndex < 0 || fromIndex >= a.Length)
                throw new ArgumentOutOfRangeException("fromIndex");

            if (toIndex < 0 || toIndex >= a.Length || toIndex < fromIndex)
                throw new ArgumentOutOfRangeException("toIndex");

            for (int i = fromIndex; i <= toIndex; i++)
            {
                a[i] = val;
            }
        }

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

        public static string ToString(IEnumerable<string> values)
        {
            return string.Join(", ", values);
        }
    }
}
