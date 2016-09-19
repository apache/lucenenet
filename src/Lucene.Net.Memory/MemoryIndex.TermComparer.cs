using System;
using System.Collections.Generic;

namespace Lucene.Net.Index.Memory
{
    public partial class MemoryIndex
    {
        private class TermComparer
        {
            /// <summary>
            /// Sorts term entries into ascending order; also works for
            /// <see cref="Array.BinarySearch{T}(T[], T, IComparer{T})"/> and 
            /// <see cref="Array.Sort{T}(T[], IComparer{T})"/>.
            /// </summary>
            public static int KeyComparer<TKey, TValue>(KeyValuePair<TKey, TValue> x, KeyValuePair<TKey, TValue> y)
                where TKey : class, IComparable<TKey>
            {
                if (x.Key == y.Key) return 0;
                return typeof(TKey) == typeof(string)
                           ? string.Compare(x.Key as string, y.Key as string, StringComparison.Ordinal)
                           : x.Key.CompareTo(y.Key);
            }
        }

        private sealed class TermComparer<TKey, TValue> : TermComparer, IComparer<KeyValuePair<TKey, TValue>>
            where TKey : class, IComparable<TKey>
        {
            public int Compare(KeyValuePair<TKey, TValue> x, KeyValuePair<TKey, TValue> y)
            {
                return KeyComparer(x, y);
            }
        }
    }
}
