using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lucene.Net.Index.Memory
{
    class TermComparer
    {
        /// <summary>
        /// Sorts term entries into ascending order; also works for
        /// Arrays.binarySearch() and Arrays.sort()
        /// </summary>
        public static int KeyComparer<TKey, TValue>(KeyValuePair<TKey, TValue> x, KeyValuePair<TKey, TValue> y)
            where TKey : class, IComparable<TKey>
        {
            if (x.Key == y.Key) return 0;
            return typeof (TKey) == typeof (string)
                       ? string.Compare(x.Key as string, y.Key as string, StringComparison.Ordinal)
                       : x.Key.CompareTo(y.Key);
        }
    }

    sealed class TermComparer<T> : TermComparer, IComparer<KeyValuePair<string, T>>
    {
        public int Compare(KeyValuePair<string, T> x, KeyValuePair<string, T> y)
        {
            return KeyComparer(x, y);
        }
    }
}
