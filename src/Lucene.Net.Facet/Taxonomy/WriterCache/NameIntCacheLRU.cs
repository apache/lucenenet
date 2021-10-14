// Lucene version compatibility level 4.8.1
using J2N.Collections.Concurrent;
using Lucene.Net.Support.Threading;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

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
    /// <para/>
    /// NOTE: This was NameIntCacheLRU in Lucene
    /// <para/>
    /// @lucene.experimental
    /// </summary>
    /// <remarks>
    /// Note: Nothing in this class is synchronized. The caller is assumed to be
    /// synchronized so that no two methods of this class are called concurrently.
    /// </remarks>
    public class NameInt32CacheLru : IInternalNameInt32CacheLru // LUCENENET specific - added interface
    {
        private readonly IInternalNameInt32CacheLru cache;
        internal NameInt32CacheLru(int limit)
        {
            this.cache = new NameCacheLru<FacetLabel>(
                limit,
                (name) => name,
                (name, prefixLength) => name.Subpath(prefixLength));
        }

        /// <inheritdoc/>
        public int Count => cache.Count;

        /// <inheritdoc/>
        public int Limit => cache.Limit;

        /// <inheritdoc/>
        string IInternalNameInt32CacheLru.Stats => cache.Stats;

        /// <inheritdoc/>
        void IInternalNameInt32CacheLru.Clear() => cache.Clear();

        /// <inheritdoc/>
        bool IInternalNameInt32CacheLru.MakeRoomLRU() => cache.MakeRoomLRU();

        /// <inheritdoc/>
        bool IInternalNameInt32CacheLru.Put(FacetLabel name, int val) => cache.Put(name, val);

        /// <inheritdoc/>
        bool IInternalNameInt32CacheLru.Put(FacetLabel name, int prefixLen, int val) => cache.Put(name, prefixLen, val);

        /// <inheritdoc/>
        bool IInternalNameInt32CacheLru.TryGetValue(FacetLabel name, out int value) => cache.TryGetValue(name, out value);
    }

    /// <summary>
    /// Public members of the <see cref="NameInt32CacheLru"/> that are shared between instances.
    /// </summary>
    public interface INameInt32CacheLru
    {
        /// <summary>
        /// Number of entries currently in the cache.
        /// </summary>
        int Count { get; }

        /// <summary>
        /// Maximum number of cache entries before eviction.
        /// </summary>
        int Limit { get; }

    }

    // LUCENENET specific - defined interface so we can share public and internal members betwen cache
    // instances.
    internal interface IInternalNameInt32CacheLru : INameInt32CacheLru
    {
        string Stats { get; }

        void Clear();

        /// <summary>
        /// If cache is full remove least recently used entries from cache. Return <c>true</c>
        /// if anything was removed, <c>false</c> otherwise.
        /// <para/>
        /// See comment in <see cref="Directory.DirectoryTaxonomyWriter.AddToCache(FacetLabel, int)"/> for an
        /// explanation why we clean 2/3rds of the cache, and not just one entry.
        /// </summary>
        bool MakeRoomLRU();

        /// <summary>
        /// Add a new value to cache.
        /// Return true if cache became full and some room need to be made. 
        /// </summary>
        bool Put(FacetLabel name, int val);

        bool Put(FacetLabel name, int prefixLen, int val);

        bool TryGetValue(FacetLabel name, out int value);
    }

    /// <summary>
    /// Class for name LRU caches. Users can specify
    /// a type that can be used as the key of the cache. The
    /// key may be either a value type or a reference type.
    /// </summary>
    /// <typeparam name="TName">The type of key for the cache.</typeparam>
    // LUCENENET specific - extracted the logic out of NameInt32CacheLru to make
    // it generic so boxing/unboxing can be avoided during lookups, while also
    // keeping the generic closing type from being exposed on the public API.
    internal sealed class NameCacheLru<TName> : IInternalNameInt32CacheLru
    {
        private IDictionary<TName, int> cache;
        internal long nMisses = 0; // for debug
        internal long nHits = 0; // for debug
        private readonly int maxCacheSize;
        private readonly object syncLock = new object(); // LUCENENET specific so we don't lock this
        private readonly Func<FacetLabel, TName> getKey;
        private readonly Func<FacetLabel, int, TName> getKeyWithPrefixLength;

        internal NameCacheLru(int limit, Func<FacetLabel, TName> getKey, Func<FacetLabel, int, TName> getKeyWithPrefixLength)
        {
            this.getKey = getKey ?? throw new ArgumentNullException(nameof(getKey)); // LUCENENET specific - changed from IllegalArgumentException to ArgumentNullException (.NET convention)
            this.getKeyWithPrefixLength = getKeyWithPrefixLength ?? throw new ArgumentNullException(nameof(getKeyWithPrefixLength)); // LUCENENET specific - changed from IllegalArgumentException to ArgumentNullException (.NET convention)
            this.maxCacheSize = limit;
            CreateCache(limit);
        }

        /// <summary>
        /// Maximum number of cache entries before eviction.
        /// </summary>
        public int Limit => maxCacheSize;

        /// <summary>
        /// Number of entries currently in the cache.
        /// </summary>
        public int Count => cache.Count;

        private void CreateCache(int maxSize)
        {
            if (maxSize < int.MaxValue)
            {
                cache = new LurchTable<TName, int>(capacity: 1000, ordering: LurchTableOrder.Access); //for LRU
            }
            else
            {
#if FEATURE_DICTIONARY_REMOVE_CONTINUEENUMERATION
                cache = new Dictionary<TName, int>(capacity: 1000);
#else
                // LUCENENET specific - we use ConcurrentDictionary here because it supports deleting while
                // iterating through the collection, but Dictionary does not.
                cache = new ConcurrentDictionary<TName, int>(concurrencyLevel: 3, capacity: 1000); //no need for LRU
#endif
            }
        }

        bool IInternalNameInt32CacheLru.TryGetValue(FacetLabel name, out int value) // LUCENENET specific - use TryGetValue() instead of Get()
        {
            if (!cache.TryGetValue(getKey(name), out value))
            {
                nMisses++;
                return false;
            }
            else
            {
                nHits++;
                return true;
            }
        }

        // LUCENENET: No need for Key() functions, they are passed in as delegates through the constructor.

        /// <summary>
        /// Add a new value to cache.
        /// Return true if cache became full and some room need to be made. 
        /// </summary>
        bool IInternalNameInt32CacheLru.Put(FacetLabel name, int val)
        {
            cache[getKey(name)] = val;
            return IsCacheFull;
        }

        bool IInternalNameInt32CacheLru.Put(FacetLabel name, int prefixLen, int val)
        {
            cache[getKeyWithPrefixLength(name, prefixLen)] = val;
            return IsCacheFull;
        }

        private bool IsCacheFull => cache.Count > maxCacheSize;

        void IInternalNameInt32CacheLru.Clear() => cache.Clear();

        string IInternalNameInt32CacheLru.Stats => "#miss=" + nMisses + " #hit=" + nHits;

        /// <summary>
        /// If cache is full remove least recently used entries from cache. Return <c>true</c>
        /// if anything was removed, <c>false</c> otherwise.
        /// <para/>
        /// See comment in <see cref="Directory.DirectoryTaxonomyWriter.AddToCache(FacetLabel, int)"/> for an
        /// explanation why we clean 2/3rds of the cache, and not just one entry.
        /// </summary>
        bool IInternalNameInt32CacheLru.MakeRoomLRU()
        {
            if (!IsCacheFull)
            {
                return false;
            }
            int n = cache.Count - (2 * maxCacheSize) / 3;
            if (n <= 0)
            {
                return false;
            }

            UninterruptableMonitor.Enter(syncLock);
            try
            {
                // Double-check that another thread didn't beat us to the operation
                n = cache.Count - (2 * maxCacheSize) / 3;
                if (n <= 0)
                {
                    return false;
                }

                //System.Diagnostics.Debug.WriteLine("Removing cache entries in MakeRoomLRU");
                using var it = cache.GetEnumerator();
                int i = 0;
                while (i < n && it.MoveNext())
                {
                    cache.Remove(it.Current.Key);
                    i++;
                }
            }
            finally
            {
                UninterruptableMonitor.Exit(syncLock);
            }
            return true;
        }
    }
}