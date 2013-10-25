using Lucene.Net.Search;
using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace Lucene.Net.QueryParsers.Xml.Builders
{
    public class CachedFilterBuilder : IFilterBuilder
    {
        private readonly QueryBuilderFactory queryFactory;
        private readonly FilterBuilderFactory filterFactory;
        private LRUCache<Object, Filter> filterCache;
        private readonly int cacheSize;

        public CachedFilterBuilder(QueryBuilderFactory queryFactory, FilterBuilderFactory filterFactory, int cacheSize)
        {
            this.queryFactory = queryFactory;
            this.filterFactory = filterFactory;
            this.cacheSize = cacheSize;
        }

        public Filter GetFilter(XElement e)
        {
            lock (this)
            {
                XElement childElement = DOMUtils.GetFirstChildOrFail(e);
                if (filterCache == null)
                {
                    filterCache = new LRUCache<Object, Filter>(cacheSize);
                }

                IQueryBuilder qb = queryFactory.GetQueryBuilder(childElement.Name.LocalName);
                Object cacheKey = null;
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

                Filter cachedFilter = filterCache[cacheKey];
                if (cachedFilter != null)
                {
                    return cachedFilter;
                }

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
        }

        class LRUCache<K, V> : HashMap<K, V>
        {
            // TODO: finish impl

            public LRUCache(int maxsize)
            {
                this.maxsize = maxsize;
            }

            protected int maxsize;
        }
    }
}
