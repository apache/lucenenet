using Lucene.Net.Diagnostics;
using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Lucene.Net.Search
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

    using AtomicReader = Lucene.Net.Index.AtomicReader;
    using AtomicReaderContext = Lucene.Net.Index.AtomicReaderContext;
    using IBits = Lucene.Net.Util.IBits;
    using RamUsageEstimator = Lucene.Net.Util.RamUsageEstimator;
    using WAH8DocIdSet = Lucene.Net.Util.WAH8DocIdSet;

    /// <summary>
    /// Wraps another <see cref="Search.Filter"/>'s result and caches it.  The purpose is to allow
    /// filters to simply filter, and then wrap with this class
    /// to add caching.
    /// </summary>
    public class CachingWrapperFilter : Filter
    {
        private readonly Filter filter;

        // LUCENENET specific - since neither ConditionalWeakTable or ConcurrentDictionaryWrapper expose its lock, we need to synchronize externally
        private readonly object cacheLock = new object();
#if FEATURE_CONDITIONALWEAKTABLE_ADDORUPDATE
        private readonly ConditionalWeakTable<object, DocIdSet> cache = new ConditionalWeakTable<object, DocIdSet>();
#else
        // LUCENENET specific - since we are doing external synchronization anyway, there is no point in using .AsConcurrent() here.
        private readonly IDictionary<object, DocIdSet> cache = new WeakDictionary<object, DocIdSet>();
#endif

        /// <summary>
        /// Wraps another filter's result and caches it. </summary>
        /// <param name="filter"> Filter to cache results of </param>
        public CachingWrapperFilter(Filter filter)
        {
            // LUCENENET specific - throw when the value is null, since it will cause excpetions in other methods
            this.filter = filter ?? throw new ArgumentNullException(nameof(filter));
        }

        /// <summary>
        /// Gets the contained filter. </summary>
        /// <returns> the contained filter. </returns>
        public virtual Filter Filter => filter;

        /// <summary>
        /// Provide the <see cref="DocIdSet"/> to be cached, using the <see cref="DocIdSet"/> provided
        /// by the wrapped Filter. 
        /// <para/>This implementation returns the given <see cref="DocIdSet"/>,
        /// if <see cref="DocIdSet.IsCacheable"/> returns <c>true</c>, else it calls
        /// <see cref="CacheImpl(DocIdSetIterator, AtomicReader)"/>
        /// <para/>Note: this method returns <see cref="EMPTY_DOCIDSET"/> if the given <paramref name="docIdSet"/>
        /// is <c>null</c> or if <see cref="DocIdSet.GetIterator()"/> return <c>null</c>. The empty
        /// instance is use as a placeholder in the cache instead of the <c>null</c> value.
        /// </summary>
        protected virtual DocIdSet DocIdSetToCache(DocIdSet docIdSet, AtomicReader reader)
        {
            if (docIdSet == null)
            {
                // this is better than returning null, as the nonnull result can be cached
                return EMPTY_DOCIDSET;
            }
            else if (docIdSet.IsCacheable)
            {
                return docIdSet;
            }
            else
            {
                DocIdSetIterator it = docIdSet.GetIterator();
                // null is allowed to be returned by iterator(),
                // in this case we wrap with the sentinel set,
                // which is cacheable.
                if (it == null)
                {
                    return EMPTY_DOCIDSET;
                }
                else
                {
                    return CacheImpl(it, reader);
                }
            }
        }

        /// <summary>
        /// Default cache implementation: uses <see cref="WAH8DocIdSet"/>.
        /// </summary>
        protected virtual DocIdSet CacheImpl(DocIdSetIterator iterator, AtomicReader reader)
        {
            var builder = new WAH8DocIdSet.Builder();
            builder.Add(iterator);
            return builder.Build();
        }

        // for testing
        internal int hitCount, missCount;

        public override DocIdSet GetDocIdSet(AtomicReaderContext context, IBits acceptDocs)
        {
            var reader = context.AtomicReader;
            object key = reader.CoreCacheKey;

            // LUCENENET specific - externally sync on all methods to the cache, since we cannot do this with our collections
            bool got;
            DocIdSet docIdSet;
            lock (cacheLock)
                got = cache.TryGetValue(key, out docIdSet);

            if (got && docIdSet != null)
            {
                hitCount++;
            }
            else
            {
                missCount++;
                docIdSet = DocIdSetToCache(filter.GetDocIdSet(context, null), reader);
                if (Debugging.AssertsEnabled) Debugging.Assert(docIdSet.IsCacheable);

                // LUCENENET specific - externally sync on all methods to the cache, since we cannot do this with our collections
                lock (cacheLock)
#if FEATURE_CONDITIONALWEAKTABLE_ADDORUPDATE
                    cache.AddOrUpdate(key, docIdSet);
#else
                    cache[key] = docIdSet;
#endif
            }

            return docIdSet == EMPTY_DOCIDSET ? null : BitsFilteredDocIdSet.Wrap(docIdSet, acceptDocs);
        }

        public override string ToString()
        {
            return this.GetType().Name + "(" + filter + ")";
        }

        public override bool Equals(object o)
        {
            if (o is null) return false;
            if (!(o is CachingWrapperFilter other)) return false;
            return filter.Equals(other.filter);
        }

        public override int GetHashCode()
        {
            return (filter.GetHashCode() ^ this.GetType().GetHashCode());
        }

        /// <summary>
        /// An empty <see cref="DocIdSet"/> instance </summary>
        protected static readonly DocIdSet EMPTY_DOCIDSET = new DocIdSetAnonymousClass();

        private class DocIdSetAnonymousClass : DocIdSet
        {
            public override DocIdSetIterator GetIterator()
            {
                return DocIdSetIterator.GetEmpty();
            }

            public override bool IsCacheable => true;

            // we explicitly provide no random access, as this filter is 100% sparse and iterator exits faster
            public override IBits Bits => null;
        }

        /// <summary>
        /// Returns total byte size used by cached filters. </summary>
        public virtual long GetSizeInBytes()
        {
            // Sync only to pull the current set of values:
            List<DocIdSet> docIdSets;
            lock (cacheLock)
            {
#if FEATURE_CONDITIONALWEAKTABLE_ENUMERATOR
                docIdSets = new List<DocIdSet>();
                foreach (var pair in cache)
                    docIdSets.Add(pair.Value);
#else
                docIdSets = new List<DocIdSet>(cache.Values);
#endif
            }

            long total = 0;
            foreach (DocIdSet dis in docIdSets)
            {
                total += RamUsageEstimator.SizeOf(dis);
            }

            return total;
        }
    }
}