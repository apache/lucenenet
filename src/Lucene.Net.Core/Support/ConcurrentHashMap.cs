using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Lucene.Net.Support
{
    public class ConcurrentHashMap<TKey, TValue> : HashMap<TKey, TValue>
    {
        public ConcurrentHashMap()
            : this(0)
        {
        }

        public ConcurrentHashMap(int initialCapacity)
            : this(initialCapacity, EqualityComparer<TKey>.Default)
        {
        }

        public ConcurrentHashMap(IEqualityComparer<TKey> comparer)
            : this(0, EqualityComparer<TKey>.Default)
        {
        }

        public ConcurrentHashMap(int initialCapacity, IEqualityComparer<TKey> comparer)
            : base(new ConcurrentDictionary<TKey, TValue>(Environment.ProcessorCount, initialCapacity, comparer), comparer)
        {
        }
    }
}