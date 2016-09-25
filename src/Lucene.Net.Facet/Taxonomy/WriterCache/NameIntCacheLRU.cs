using System.Collections.Generic;
using System.Linq;

namespace Lucene.Net.Facet.Taxonomy.WriterCache
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
    /// An an LRU cache of mapping from name to int.
    /// Used to cache Ordinals of category paths.
    /// 
    /// @lucene.experimental
    /// </summary>
    // Note: Nothing in this class is synchronized. The caller is assumed to be
    // synchronized so that no two methods of this class are called concurrently.
    public class NameIntCacheLRU
    {
        private Dictionary<object, int?> cache;
        internal long nMisses = 0; // for debug
        internal long nHits = 0; // for debug
        private int capacity;

        internal NameIntCacheLRU(int capacity)
        {
            this.capacity = capacity;
            CreateCache(capacity);
        }

        /// <summary>
        /// Maximum number of cache entries before eviction. </summary>
        public virtual int Capacity
        {
            get
            {
                return capacity;
            }
        }

        /// <summary>
        /// Number of entries currently in the cache. </summary>
        public virtual int Count
        {
            get
            {
                return cache.Count;
            }
        }

        private void CreateCache(int maxSize)
        {
            // LUCENENET TODO: Create an adapter so we can plug in either a generic
            // dictionary or LRUHashMap or alternatively make LRUHashMap implement IDictionary<TKey, TValue>
            //if (maxSize < int.MaxValue)
            //{
            //    cache = new LRUHashMap<object,int?>(1000,true); //for LRU
            //}
            //else
            {
                cache = new Dictionary<object, int?>(1000); //no need for LRU
            }
        }

        internal virtual int? Get(FacetLabel name)
        {
            object key = Key(name);
            int? res = cache.ContainsKey(key) ? cache[key] : null;
            if (res == null)
            {
                nMisses++;
            }
            else
            {
                nHits++;
            }
            return res;
        }

        /// <summary>
        /// Subclasses can override this to provide caching by e.g. hash of the string. </summary>
        internal virtual object Key(FacetLabel name)
        {
            return name;
        }

        internal virtual object Key(FacetLabel name, int prefixLen)
        {
            return name.Subpath(prefixLen);
        }

        /// <summary>
        /// Add a new value to cache.
        /// Return true if cache became full and some room need to be made. 
        /// </summary>
        internal virtual bool Put(FacetLabel name, int? val)
        {
            cache[Key(name)] = val;
            return CacheFull;
        }

        internal virtual bool Put(FacetLabel name, int prefixLen, int? val)
        {
            cache[Key(name, prefixLen)] = val;
            return CacheFull;
        }

        private bool CacheFull
        {
            get
            {
                return cache.Count > capacity;
            }
        }

        internal virtual void Clear()
        {
            cache.Clear();
        }

        internal virtual string Stats()
        {
            return "#miss=" + nMisses + " #hit=" + nHits;
        }

        /// <summary>
        /// If cache is full remove least recently used entries from cache. Return true
        /// if anything was removed, false otherwise.
        /// 
        /// See comment in DirectoryTaxonomyWriter.addToCache(CategoryPath, int) for an
        /// explanation why we clean 2/3rds of the cache, and not just one entry.
        /// </summary>
        internal virtual bool MakeRoomLRU()
        {
            if (!CacheFull)
            {
                return false;
            }
            int n = cache.Count - (2 * capacity) / 3;
            if (n <= 0)
            {
                return false;
            }

            // LUCENENET: Loop in reverse so we can safely delete
            // a range of items (0 - n) without a 
            // "Collection was modified" conflict
            for (int i = n - 1; i >= 0; i--)
            {
                cache.Remove(cache.Keys.ElementAt(i));
            }
            return true;
        }
    }
}