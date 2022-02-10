using J2N.Collections.Concurrent;
using Lucene.Net.Search;
using Lucene.Net.Support.Threading;
using System.Xml;

namespace Lucene.Net.QueryParsers.Xml.Builders
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
    /// Filters are cached in an LRU Cache keyed on the contained query or filter object. Using this will
    /// speed up overall performance for repeated uses of the same expensive query/filter. The sorts of
    /// queries/filters likely to benefit from caching need not necessarily be complex - e.g. simple
    /// TermQuerys with a large DF (document frequency) can be expensive  on large indexes.
    /// A good example of this might be a term query on a field with only 2 possible  values -
    /// "true" or "false". In a large index, querying or filtering on this field requires reading
    /// millions  of document ids from disk which can more usefully be cached as a filter bitset.
    /// <para/>
    /// For Queries/Filters to be cached and reused the object must implement hashcode and
    /// equals methods correctly so that duplicate queries/filters can be detected in the cache.
    /// <para/>
    /// The CoreParser.maxNumCachedFilters property can be used to control the size of the LRU
    /// Cache established during the construction of CoreParser instances.
    /// </summary>
    public class CachedFilterBuilder : IFilterBuilder
    {
        private readonly QueryBuilderFactory queryFactory;
        private readonly FilterBuilderFactory filterFactory;

        private LurchTable<object, Filter> filterCache;

        private readonly int cacheSize;

        public CachedFilterBuilder(QueryBuilderFactory queryFactory,
                                   FilterBuilderFactory filterFactory,
                                   int cacheSize)
        {
            this.queryFactory = queryFactory;
            this.filterFactory = filterFactory;
            this.cacheSize = cacheSize;
        }

        public virtual Filter GetFilter(XmlElement e)
        {
            UninterruptableMonitor.Enter(this);
            try
            {
                XmlElement childElement = DOMUtils.GetFirstChildOrFail(e);

                if (filterCache is null)
                {
                    filterCache = new LurchTable<object, Filter>(LurchTableOrder.Access, cacheSize);
                }

                // Test to see if child Element is a query or filter that needs to be
                // cached
                IQueryBuilder qb = queryFactory.GetQueryBuilder(childElement.Name);
                object cacheKey = null;
                Query q = null;
                Filter f = null;
                if (qb != null)
                {
                    q = qb.GetQuery(childElement);
                    cacheKey = q;
                }
                else
                {
                    f = filterFactory.GetFilter(childElement);
                    cacheKey = f;
                }
                if (filterCache.TryGetValue(cacheKey, out Filter cachedFilter) && cachedFilter != null)
                {
                    return cachedFilter; // cache hit
                }

                //cache miss
                if (qb != null)
                {
                    cachedFilter = new QueryWrapperFilter(q);
                }
                else
                {
                    cachedFilter = new CachingWrapperFilter(f);
                }

                filterCache[cacheKey] = cachedFilter;
                return cachedFilter;
            }
            finally
            {
                UninterruptableMonitor.Exit(this);
            }
        }

        // LUCENENET NOTE: LRUCache replaced with LurchTable
    }
}
