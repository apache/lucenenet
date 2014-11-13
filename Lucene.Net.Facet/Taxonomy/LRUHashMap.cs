using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Lucene.Net.Support;

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
    public class LRUHashMap<TV, TU> where TU : class //this is implementation of LRU Cache
    {

        public int MaxSize { get; set; }
        private int CleanSize;
        private TimeSpan MaxDuration;


        private readonly ConcurrentDictionary<TV, CacheDataObject<TU>> _cache = new ConcurrentDictionary<TV, CacheDataObject<TU>>();

        public LRUHashMap(int maxSize = 50000, int cleanPercentage = 30, TimeSpan maxDuration = default(TimeSpan))
        {
            MaxSize = maxSize;
            CleanSize = (int)Math.Max(MaxSize * (1.0 * cleanPercentage / 100), 1);
            if (maxDuration == default(TimeSpan))
            {
                MaxDuration = TimeSpan.FromDays(1);
            }
            else
            {
                MaxDuration = maxDuration;
            }
        }

        
        public bool Put(TV cacheKey, TU value)
        {
            return AddToCache(cacheKey, value);
        }

        public bool AddToCache(TV cacheKey, TU value)
        {
            var cachedResult = new CacheDataObject<TU>
            {
                Usage = 1, //value == null ? 1 : value.Usage + 1,
                Value = value,
                Timestamp = DateTime.UtcNow
            };

            _cache.AddOrUpdate(cacheKey, cachedResult, (_, __) => cachedResult);
            if (_cache.Count > MaxSize)
            {
                foreach (var source in _cache
                    .OrderByDescending(x => x.Value.Usage)
                    .ThenBy(x => x.Value.Timestamp)
                    .Skip(MaxSize - CleanSize))
                {
                    if (EqualityComparer<TV>.Default.Equals(source.Key, cacheKey))
                        continue; // we don't want to remove the one we just added
                    CacheDataObject<TU> ignored;
                    _cache.TryRemove(source.Key, out ignored);
                }
            }
            return true;
        }

        public TU Get(TV cacheKey, bool increment = false)
        {
            CacheDataObject<TU> value;
            if (_cache.TryGetValue(cacheKey, out value) && (DateTime.UtcNow - value.Timestamp) <= MaxDuration)
            {
                if (increment)
                {
                    Interlocked.Increment(ref value.Usage);
                }
                return value.Value;
            }
            return null;
        }

        public bool IsExistInCache(TV cacheKey)
        {
            return (_cache.ContainsKey(cacheKey));
        }

        public int Size()
        {
            return _cache.Count;
        }

        #region Nested type: CacheDataObject

        private class CacheDataObject<T> where T : class
        {
            public DateTime Timestamp;
            public int Usage;
            public T Value;
        }

        #endregion

    }

}