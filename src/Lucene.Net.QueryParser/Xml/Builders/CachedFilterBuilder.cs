/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.Collections.Generic;
using Lucene.Net.Queryparser.Xml;
using Lucene.Net.Queryparser.Xml.Builders;
using Lucene.Net.Search;
using Org.W3c.Dom;
using Sharpen;

namespace Lucene.Net.Queryparser.Xml.Builders
{
	/// <summary>Filters are cached in an LRU Cache keyed on the contained query or filter object.
	/// 	</summary>
	/// <remarks>
	/// Filters are cached in an LRU Cache keyed on the contained query or filter object. Using this will
	/// speed up overall performance for repeated uses of the same expensive query/filter. The sorts of
	/// queries/filters likely to benefit from caching need not necessarily be complex - e.g. simple
	/// TermQuerys with a large DF (document frequency) can be expensive  on large indexes.
	/// A good example of this might be a term query on a field with only 2 possible  values -
	/// "true" or "false". In a large index, querying or filtering on this field requires reading
	/// millions  of document ids from disk which can more usefully be cached as a filter bitset.
	/// <p/>
	/// For Queries/Filters to be cached and reused the object must implement hashcode and
	/// equals methods correctly so that duplicate queries/filters can be detected in the cache.
	/// <p/>
	/// The CoreParser.maxNumCachedFilters property can be used to control the size of the LRU
	/// Cache established during the construction of CoreParser instances.
	/// </remarks>
	public class CachedFilterBuilder : FilterBuilder
	{
		private readonly QueryBuilderFactory queryFactory;

		private readonly FilterBuilderFactory filterFactory;

		private CachedFilterBuilder.LRUCache<object, Filter> filterCache;

		private readonly int cacheSize;

		public CachedFilterBuilder(QueryBuilderFactory queryFactory, FilterBuilderFactory
			 filterFactory, int cacheSize)
		{
			this.queryFactory = queryFactory;
			this.filterFactory = filterFactory;
			this.cacheSize = cacheSize;
		}

		/// <exception cref="Lucene.Net.Queryparser.Xml.ParserException"></exception>
		public virtual Filter GetFilter(Element e)
		{
			lock (this)
			{
				Element childElement = DOMUtils.GetFirstChildOrFail(e);
				if (filterCache == null)
				{
					filterCache = new CachedFilterBuilder.LRUCache<object, Filter>(cacheSize);
				}
				// Test to see if child Element is a query or filter that needs to be
				// cached
				QueryBuilder qb = queryFactory.GetQueryBuilder(childElement.GetNodeName());
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
				Filter cachedFilter = filterCache.Get(cacheKey);
				if (cachedFilter != null)
				{
					return cachedFilter;
				}
				// cache hit
				//cache miss
				if (qb != null)
				{
					cachedFilter = new QueryWrapperFilter(q);
				}
				else
				{
					cachedFilter = new CachingWrapperFilter(f);
				}
				filterCache.Put(cacheKey, cachedFilter);
				return cachedFilter;
			}
		}

		[System.Serializable]
		internal class LRUCache<K, V> : LinkedHashMap<K, V>
		{
			public LRUCache(int maxsize) : base(maxsize * 4 / 3 + 1, 0.75f, true)
			{
				this.maxsize = maxsize;
			}

			protected internal int maxsize;

			protected override bool RemoveEldestEntry(KeyValuePair<K, V> eldest)
			{
				return Count > maxsize;
			}
		}
	}
}
