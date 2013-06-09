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
    }
}
