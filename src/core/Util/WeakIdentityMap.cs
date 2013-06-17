using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace Lucene.Net.Util
{
    public sealed class WeakIdentityMap<K, V>
        where K : class
    {
        // .NET port notes: ReferenceQueue<T> has no equivalent in .NET.
        // TODO: when we can target .NET 4.5, change this to WeakReference<K>
        private readonly IDictionary<IdentityWeakReference, V> backingStore;
        private readonly bool reapOnRead;

        public static WeakIdentityMap<K, V> NewHashMap()
        {
            // .NET port notes: since reaping is more expensive in .NET, do not do by default
            return NewHashMap(false);
        }

        public static WeakIdentityMap<K, V> NewHashMap(bool reapOnRead)
        {
            return new WeakIdentityMap<K, V>(new HashMap<IdentityWeakReference, V>(), reapOnRead);
        }

        public static WeakIdentityMap<K, V> NewConcurrentHashMap()
        {
            // .NET port notes: since reaping is more expensive in .NET, do not do by default
            return NewConcurrentHashMap(false);
        }

        public static WeakIdentityMap<K, V> NewConcurrentHashMap(bool reapOnRead)
        {
            return new WeakIdentityMap<K, V>(new ConcurrentHashMap<IdentityWeakReference, V>(), reapOnRead);
        }

        private WeakIdentityMap(IDictionary<IdentityWeakReference, V> backingStore, bool reapOnRead)
        {
            this.backingStore = backingStore;
            this.reapOnRead = reapOnRead;
        }

        public void Clear()
        {
            backingStore.Clear();
            Reap();
        }

        public bool ContainsKey(object key)
        {
            if (reapOnRead) Reap();

            return backingStore.ContainsKey(new IdentityWeakReference(key));
        }

        public V this[K key]
        {
            get
            {
                if (reapOnRead) Reap();
                return backingStore[new IdentityWeakReference(key)];
            }
            set
            {
                Reap();
                backingStore[new IdentityWeakReference(key)] = value;
            }
        }

        public bool IsEmpty
        {
            get { return Size == 0; }
        }

        public V Remove(object key)
        {
            Reap();

            var keyref = new IdentityWeakReference(key);
            V value = backingStore[keyref];
            backingStore.Remove(keyref);

            return value;
        }

        public int Size
        {
            get
            {
                if (backingStore.Count == 0)
                    return 0;

                if (reapOnRead) Reap();
                return backingStore.Count;
            }
        }

        public IEnumerable<K> Keys
        {
            // .NET port: using this method which mimics IDictionary instead of KeyIterator()
            get
            {
                foreach (var key in backingStore.Keys)
                {
                    var target = key.Target;

                    if (target == null)
                        continue;
                    else if (target == NULL)
                        yield return null;
                    else
                        yield return (K)target;
                }
            }
        }

        public IEnumerable<V> Values
        {
            get
            {
                if (reapOnRead) Reap();
                return backingStore.Values;
            }
        }

        public void Reap()
        {
            lock (backingStore)
            {
                List<IdentityWeakReference> keysToRemove = new List<IdentityWeakReference>();

                foreach (IdentityWeakReference zombie in backingStore.Keys)
                {
                    if (!zombie.IsAlive)
                        keysToRemove.Add(zombie);
                }

                foreach (var key in keysToRemove)
                {
                    backingStore.Remove(key);
                }
            }
        }

        private static readonly object NULL = new object();

        private class IdentityWeakReference : WeakReference
        {
            private readonly int hash;

            public IdentityWeakReference(Object target)
                : base(target == null ? NULL : target)
            {
                hash = RuntimeHelpers.GetHashCode(target);
            }

            public override int GetHashCode()
            {
                return hash;
            }

            public override bool Equals(object o)
            {
                if (ReferenceEquals(this, o))
                {
                    return true;
                }
                if (o is IdentityWeakReference)
                {
                    IdentityWeakReference iwr = (IdentityWeakReference)o;
                    if (this.Target == iwr.Target)
                    {
                        return true;
                    }
                }
                return false;
            }
        }        
    }
}
