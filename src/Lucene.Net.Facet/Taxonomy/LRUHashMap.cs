using System;
using System.Collections.Generic;
using System.Linq;

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
    /// LRUHashMap is an extension of Java's HashMap, which has a bounded size();
    /// When it reaches that size, each time a new element is added, the least
    /// recently used (LRU) entry is removed.
    /// <para>
    /// Java makes it very easy to implement LRUHashMap - all its functionality is
    /// already available from <seealso cref="java.util.LinkedHashMap"/>, and we just need to
    /// configure that properly.
    /// </para>
    /// <para>
    /// Note that like HashMap, LRUHashMap is unsynchronized, and the user MUST
    /// synchronize the access to it if used from several threads. Moreover, while
    /// with HashMap this is only a concern if one of the threads is modifies the
    /// map, with LURHashMap every read is a modification (because the LRU order
    /// needs to be remembered) so proper synchronization is always necessary.
    /// </para>
    /// <para>
    /// With the usual synchronization mechanisms available to the user, this
    /// unfortunately means that LRUHashMap will probably perform sub-optimally under
    /// heavy contention: while one thread uses the hash table (reads or writes), any
    /// other thread will be blocked from using it - or even just starting to use it
    /// (e.g., calculating the hash function). A more efficient approach would be not
    /// to use LinkedHashMap at all, but rather to use a non-locking (as much as
    /// possible) thread-safe solution, something along the lines of
    /// java.util.concurrent.ConcurrentHashMap (though that particular class does not
    /// support the additional LRU semantics, which will need to be added separately
    /// using a concurrent linked list or additional storage of timestamps (in an
    /// array or inside the entry objects), or whatever).
    /// 
    /// @lucene.experimental
    /// </para>
    /// </summary>
    public class LRUHashMap<TKey, TValue> where TValue : class //this is implementation of LRU Cache
    {
        private readonly Dictionary<TKey, CacheDataObject> cache;
        // We can't use a ReaderWriterLockSlim because every read is also a 
        // write, so we gain nothing by doing so
        private readonly object syncLock = new object();
        // Record last access so we can tie break if 2 calls make it in within
        // the same millisecond.
        private long lastAccess;
        private int capacity;

        public LRUHashMap(int capacity)
        {
            if (capacity < 1)
            {
                throw new ArgumentOutOfRangeException("capacity must be at least 1");
            }
            this.capacity = capacity;
            this.cache = new Dictionary<TKey, CacheDataObject>(capacity);
        }

        public virtual int Capacity
        {
            get { return capacity; }
            set
            {
                if (value < 1)
                {
                    throw new ArgumentOutOfRangeException("Capacity must be at least 1");
                }
                capacity = value;
            }
        }

        public bool Put(TKey key, TValue value)
        {
            lock (syncLock)
            { 
                CacheDataObject cdo;
                if (cache.TryGetValue(key, out cdo))
                {
                    // Item already exists, update our last access time
                    cdo.timestamp = GetTimestamp();
                }
                else
                {
                    cache[key] = new CacheDataObject
                    {
                        value = value,
                        timestamp = GetTimestamp()
                    };
                    // We have added a new item, so we may need to remove the eldest
                    if (cache.Count > Capacity)
                    {
                        // Remove the eldest item (lowest timestamp) from the cache
                        cache.Remove(cache.OrderBy(x => x.Value.timestamp).First().Key);
                    }
                }
            }
            return true;
        }

        public TValue Get(TKey key)
        {
            lock (syncLock)
            {
                CacheDataObject cdo;
                if (cache.TryGetValue(key, out cdo))
                {
                    // Write our last access time
                    cdo.timestamp = GetTimestamp();

                    return cdo.value;
                }
            }
            return null;
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            lock (syncLock)
            {
                CacheDataObject cdo;
                if (cache.TryGetValue(key, out cdo))
                {
                    // Write our last access time
                    cdo.timestamp = GetTimestamp();
                    value = cdo.value;

                    return true;
                }

                value = null;
                return false;
            }
        }

        public bool ContainsKey(TKey key)
        {
            return cache.ContainsKey(key);
        }

        public int Count
        {
            get
            {
                return cache.Count;
            }
        }

        private long GetTimestamp()
        {
            long ticks = DateTime.UtcNow.Ticks;
            if (ticks <= lastAccess)
            {
                // Tie break by incrementing
                // when 2 calls happen within the
                // same millisecond
                ticks = ++lastAccess;
            }
            else
            {
                lastAccess = ticks;
            }
            return ticks;
        }
        

        #region Nested type: CacheDataObject

        private class CacheDataObject
        {
            // Ticks representing the last access time
            public long timestamp;
            public TValue value;

            public override string ToString()
            {
                return "Last Access: " + timestamp.ToString() + " - " + value.ToString();
            }
        }

        #endregion
    }
}