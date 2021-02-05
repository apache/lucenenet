// Lucene version compatibility level 4.8.1

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
    /// It uses as key, hash of the path instead of the path.
    /// This way the cache takes less RAM, but correctness depends on
    /// assuming no collisions.
    /// <para/>
    /// NOTE: this was NameHashIntCacheLRU in Lucene
    /// <para/>
    /// @lucene.experimental
    /// </summary>
    public class NameHashInt32CacheLru : IInternalNameInt32CacheLru // LUCENENET specific - added interface
    {
        private readonly IInternalNameInt32CacheLru cache;
        internal NameHashInt32CacheLru(int limit)
        {
            this.cache = new NameCacheLru<long>(
               limit,
               (name) => name.Int64HashCode(),
               (name, prefixLength) => name.Subpath(prefixLength).Int64HashCode());
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
}