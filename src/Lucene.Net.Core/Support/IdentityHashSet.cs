using System;
using System.Collections.Generic;

namespace Lucene.Net.Support
{
#if FEATURE_SERIALIZABLE
    [Serializable]
#endif
    public class IdentityHashSet<T> : HashSet<T>
    {
        public IdentityHashSet()
            : base(new IdentityComparer<T>())
        {
        }

        public IdentityHashSet(IEnumerable<T> collection)
            : base(collection, new IdentityComparer<T>())
        {
        }
    }
}