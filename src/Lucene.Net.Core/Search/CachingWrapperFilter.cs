using Lucene.Net.Support;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

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
    /// Wraps another <seealso cref="Filter"/>'s result and caches it.  The purpose is to allow
    /// filters to simply filter, and then wrap with this class
    /// to add caching.
    /// </summary>
    public class CachingWrapperFilter : Filter
    {
        private readonly Filter _filter;

        //private readonly IDictionary<object, DocIdSet> Cache = Collections.synchronizedMap(new WeakHashMap<object, DocIdSet>());
        private readonly IDictionary<object, DocIdSet> _cache = new ConcurrentHashMapWrapper<object, DocIdSet>(new WeakDictionary<object, DocIdSet>());

        /// <summary>
        /// Wraps another filter's result and caches it. </summary>
        /// <param name="filter"> Filter to cache results of </param>
        public CachingWrapperFilter(Filter filter)
        {
            this._filter = filter;
        }

        /// <summary>
        /// Gets the contained filter. </summary>
        /// <returns> the contained filter. </returns>
        public virtual Filter Filter
        {
            get
            {
                return _filter;
            }
        }

        /// <summary>
        ///  Provide the DocIdSet to be cached, using the DocIdSet provided
        ///  by the wrapped Filter. <p>this implementation returns the given <seealso cref="DocIdSet"/>,
        ///  if <seealso cref="DocIdSet#isCacheable"/> returns <code>true</code>, else it calls
        ///  <seealso cref="#cacheImpl(DocIdSetIterator,AtomicReader)"/>
        ///  <p>Note: this method returns <seealso cref="#EMPTY_DOCIDSET"/> if the given docIdSet
        ///  is <code>null</code> or if <seealso cref="DocIdSet#iterator()"/> return <code>null</code>. The empty
        ///  instance is use as a placeholder in the cache instead of the <code>null</code> value.
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
        /// Default cache implementation: uses <seealso cref="WAH8DocIdSet"/>.
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

            DocIdSet docIdSet = _cache[key];
            if (docIdSet != null)
            {
                hitCount++;
            }
            else
            {
                missCount++;
                docIdSet = DocIdSetToCache(_filter.GetDocIdSet(context, null), reader);
                Debug.Assert(docIdSet.IsCacheable);
                _cache[key] = docIdSet;
            }

            return docIdSet == EMPTY_DOCIDSET ? null : BitsFilteredDocIdSet.Wrap(docIdSet, acceptDocs);
        }

        public override string ToString()
        {
            return this.GetType().Name + "(" + _filter + ")";
        }

        public override bool Equals(object o)
        {
            var other = o as CachingWrapperFilter;
            if (other == null)
            {
                return false;
            }
            return _filter.Equals(other._filter);
        }

        public override int GetHashCode()
        {
            return (_filter.GetHashCode() ^ this.GetType().GetHashCode());
        }

        /// <summary>
        /// An empty {@code DocIdSet} instance </summary>
        protected static readonly DocIdSet EMPTY_DOCIDSET = new DocIdSetAnonymousInnerClassHelper();

        private class DocIdSetAnonymousInnerClassHelper : DocIdSet
        {
            public override DocIdSetIterator GetIterator()
            {
                return DocIdSetIterator.GetEmpty();
            }

            public override bool IsCacheable
            {
                get
                {
                    return true;
                }
            }

            // we explicitly provide no random access, as this filter is 100% sparse and iterator exits faster
            public override IBits Bits
            {
                get { return null; }
            }
        }

        /// <summary>
        /// Returns total byte size used by cached filters. </summary>
        public virtual long SizeInBytes()
        {            
            IList<DocIdSet> docIdSets = new List<DocIdSet>(_cache.Values);
            return docIdSets.Sum(dis => RamUsageEstimator.SizeOf(dis));
        }
    }
}