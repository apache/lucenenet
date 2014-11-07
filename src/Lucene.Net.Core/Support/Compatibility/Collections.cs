using System.Collections.Generic;
using System.Collections.Immutable;

namespace Lucene.Net
{
    public static class Collections
    {
        public static ISet<T> Singleton<T>(T o)
        {
            return ImmutableHashSet.Create(o);
        }

        public static IList<T> EmptyList<T>()
        {
            return ImmutableList<T>.Empty;
        }
    }
}
