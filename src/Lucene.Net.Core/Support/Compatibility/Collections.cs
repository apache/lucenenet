using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

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

        public static IList<T> UnmodifiableList<T>(IEnumerable<T> items)
        {
            return ImmutableList.Create<T>(items.ToArray());
        }

        public static IList<T> UnmodifiableList<T>(List<T> items)
        {
            return items.AsReadOnly();
        }

        public static ISet<T> UnmodifiableSet<T>(IEnumerable<T> items)
        {
            return ImmutableHashSet.Create<T>(items.ToArray());
        }

        public static IDictionary<T, TS> UnmodifiableMap<T, TS>(IDictionary<T, TS> d)
        {
            var builder = ImmutableDictionary.CreateBuilder<T, TS>();
            builder.AddRange(d);
            return builder.ToImmutable();
        }

        public static IDictionary<T, S> SingletonMap<T, S>(T key, S value)
        {
            return new Dictionary<T, S> {{key, value}};
        }
    }
}
