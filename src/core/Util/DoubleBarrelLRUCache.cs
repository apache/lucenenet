using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Lucene.Net.Util
{
    public sealed class DoubleBarrelLRUCache<K, V> : DoubleBarrelLRUCache
        where K : DoubleBarrelLRUCache.CloneableKey
    {
        private readonly IDictionary<K, V> cache1;
        private readonly IDictionary<K, V> cache2;
        private int countdown; // not readonly due to Interlocked usage
        private volatile bool swapped;
        private readonly int maxSize;

        public DoubleBarrelLRUCache(int maxSize)
        {
            this.maxSize = maxSize;
            countdown = maxSize;
            cache1 = new ConcurrentDictionary<K, V>(); // TODO: do we need to create a ConcurrentHashMap type?
            cache2 = new ConcurrentDictionary<K, V>();
        }

        public V this[K key]
        {
            get
            {
                IDictionary<K, V> primary;
                IDictionary<K, V> secondary;

                if (swapped)
                {
                    primary = cache2;
                    secondary = cache1;
                }
                else
                {
                    primary = cache1;
                    secondary = cache2;
                }

                // Try primary first
                V result;
                if (!primary.TryGetValue(key, out result))
                {
                    // Not found -- try secondary
                    if (secondary.TryGetValue(key, out result))
                    {
                        // Promote to primary
                        this[(K)key.Clone()] = result;
                    }
                }
                return result;
            }
            set
            {
                IDictionary<K, V> primary;
                IDictionary<K, V> secondary;

                if (swapped)
                {
                    primary = cache2;
                    secondary = cache1;
                }
                else
                {
                    primary = cache1;
                    secondary = cache2;
                }

                primary[key] = value;

                if (Interlocked.Decrement(ref countdown) == 0)
                {
                    // Time to swap

                    // NOTE: there is saturation risk here, that the
                    // thread that's doing the clear() takes too long to
                    // do so, while other threads continue to add to
                    // primary, but in practice this seems not to be an
                    // issue (see LUCENE-2075 for benchmark & details)

                    // First, clear secondary
                    secondary.Clear();

                    // Second, swap
                    swapped = !swapped;

                    // Third, reset countdown
                    Interlocked.Exchange(ref countdown, maxSize);
                }
            }
        }
    }

    // .NET Port: non-generic base class to hold nested type
    public abstract class DoubleBarrelLRUCache
    {
        public abstract class CloneableKey
        {
            public abstract CloneableKey Clone();
        }
    }
}
