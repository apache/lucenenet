using System;
using System.Collections.Generic;

namespace Lucene.Net.Support
{
#if FEATURE_SERIALIZABLE
    [Serializable]
#endif
    public class IdentityHashMap<TKey, TValue> : HashMap<TKey, TValue>
    {
        public IdentityHashMap()
            : base(new IdentityComparer<TKey>())
        {
        }

        public IdentityHashMap(int initialCapacity)
            : base(initialCapacity, new IdentityComparer<TKey>())
        {
        }

        public IdentityHashMap(IDictionary<TKey, TValue> wrappedDictionary)
            : base(wrappedDictionary, new IdentityComparer<TKey>())
        {
        }
    }
}