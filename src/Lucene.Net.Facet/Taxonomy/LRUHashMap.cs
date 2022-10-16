// Lucene version compatibility level 4.8.1
using J2N.Collections.Concurrent;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Lucene.Net.Facet.Taxonomy
{
    /*
     * Licensed to the Apache Software Foundation (ASF) under one or more
     * contributor license agreements.  See the NOTICE file distributed with
     * this work for additional information regarding copyright ownership.
     * The ASF licenses this file to You under the Apache License, Version 2.0
     * (the "License"); you may not use this file except in compliance with
     * the License.  You may obtain a copy of the License at
     *
     *     http://www.apache.org/licenses/LICENSE-2.0
     *
     * Unless required by applicable law or agreed to in writing, software
     * distributed under the License is distributed on an "AS IS" BASIS,
     * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
     * See the License for the specific language governing permissions and
     * limitations under the License.
     */

    /// <summary>
    /// <see cref="LruDictionary{TKey, TValue}"/> is similar to of Java's HashMap, which has a bounded <see cref="Limit"/>;
    /// When it reaches that <see cref="Limit"/>, each time a new element is added, the least
    /// recently used (LRU) entry is removed.
    /// <para>
    /// Unlike the Java Lucene implementation, this one is thread safe because it is backed by the <see cref="LurchTable{TKey, TValue}"/>.
    /// Do note that every time an element is read from <see cref="LruDictionary{TKey, TValue}"/>,
    /// a write operation also takes place to update the element's last access time.
    /// This is because the LRU order needs to be remembered to determine which element
    /// to evict when the <see cref="Limit"/> is exceeded. 
    /// </para>
    /// <para>
    /// 
    /// @lucene.experimental
    /// </para>
    /// </summary>
    public class LruDictionary<TKey, TValue> : IDictionary<TKey, TValue>
    {
        private readonly LurchTable<TKey, TValue> cache;

        /// <summary>
        /// Create a new hash map with a bounded size and with least recently
        /// used entries removed.
        /// </summary>
        /// <param name="limit">
        /// The maximum size (in number of entries) to which the map can grow
        /// before the least recently used entries start being removed.
        /// <para/>
        /// Setting <paramref name="limit"/> to a very large value, like <see cref="int.MaxValue"/>
        /// is allowed, but is less efficient than
        /// using <see cref="J2N.Collections.Generic.Dictionary{TKey, TValue}"/> or 
        /// <see cref="Dictionary{TKey, TValue}"/> because our class needs
        /// to keep track of the use order (via an additional doubly-linked
        /// list) which is not used when the map's size is always below the
        /// maximum size.
        /// </param>
        public LruDictionary(int limit)
            : this(limit, null)
        {
        }

        /// <summary>
        /// Create a new hash map with a bounded size and with least recently
        /// used entries removed.
        /// <para/>
        /// LUCENENET specific overload to allow passing in custom <see cref="IEqualityComparer{T}"/>. 
        /// See LUCENENET-602.
        /// </summary>
        /// <param name="limit">
        /// The maximum size (in number of entries) to which the map can grow
        /// before the least recently used entries start being removed.
        /// <para/>
        /// Setting <paramref name="limit"/> to a very large value, like <see cref="int.MaxValue"/>
        /// is allowed, but is less efficient than
        /// using <see cref="J2N.Collections.Generic.Dictionary{TKey, TValue}"/> or 
        /// <see cref="Dictionary{TKey, TValue}"/> because our class needs
        /// to keep track of the use order (via an additional doubly-linked
        /// list) which is not used when the map's size is always below the
        /// maximum size.
        /// </param>
        /// <param name="comparer">
        /// The <see cref="IEqualityComparer{TKey}"/> implementation to use when comparing keys, 
        /// or <c>null</c> to use the default <see cref="IEqualityComparer{TKey}"/> for the type of the key.
        /// </param>
        public LruDictionary(int limit, IEqualityComparer<TKey> comparer)
        {
            cache = new LurchTable<TKey, TValue>(LurchTableOrder.Access, limit, comparer);
        }

        /// <summary>
        /// Allows changing the dictionary's maximal number of elements
        /// which was defined at construction time.
        /// <para>
        /// Note that if the dictionary is already larger than <see cref="Limit"/>, the current 
        /// implementation does not shrink it (by removing the oldest elements);
        /// Rather, the map remains in its current size as new elements are
        /// added, and will only start shrinking (until settling again on the
        /// given <see cref="Limit"/>) if existing elements are explicitly deleted.
        /// </para>
        /// </summary>
        public virtual int Limit
        {
            get => cache.Limit;
            set
            {
                if (value < 1)
                {
                    throw new ArgumentOutOfRangeException(nameof(Limit), "Limit must be at least 1"); // LUCENENET specific - changed from IllegalArgumentException to ArgumentOutOfRangeException (.NET convention)
                }
                cache.Limit = value;
            }
        }

        public virtual TValue Put(TKey key, TValue value)
        {
            TValue oldValue = default;
            cache.AddOrUpdate(key, value, (k, v) =>
            {
                oldValue = cache[key];
                return value;
            });
            return oldValue;
        }

        public virtual TValue Get(TKey key)
        {
            if (!cache.TryGetValue(key, out TValue result))
            {
                return default;
            }
            return result;
        }

        #region IDictionary<TKey, TValue> members

        public virtual TValue this[TKey key]
        {
            get => cache[key];
            set => cache[key] = value;
        }

        public virtual int Count => cache.Count;

        public virtual bool IsReadOnly => false;

        public virtual ICollection<TKey> Keys => cache.Keys;

        public virtual ICollection<TValue> Values => cache.Values;

        public virtual void Add(KeyValuePair<TKey, TValue> item)
        {
            cache.Add(item.Key, item.Value);
        }

        public virtual void Add(TKey key, TValue value)
        {
            cache.Add(key, value);
        }

        public virtual void Clear()
        {
            cache.Clear();
        }

        public virtual bool Contains(KeyValuePair<TKey, TValue> item)
        {
            return ((ICollection<KeyValuePair<TKey, TValue>>)cache).Contains(item);
        }

        public virtual bool ContainsKey(TKey key)
        {
            return cache.ContainsKey(key);
        }

        public virtual void CopyTo(KeyValuePair<TKey, TValue>[] array, int index)
        {
            cache.CopyTo(array, index);
        }

        public virtual IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            return cache.GetEnumerator();
        }

        public virtual bool Remove(KeyValuePair<TKey, TValue> item)
        {
            return cache.TryRemove(item);
        }

        public virtual bool Remove(TKey key)
        {
            return cache.Remove(key);
        }

        public virtual bool TryGetValue(TKey key, out TValue value)
        {
            return cache.TryGetValue(key, out value);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion
    }
}