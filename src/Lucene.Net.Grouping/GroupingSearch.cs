/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.Collections;
using System.Collections.Generic;
using Lucene.Net.Queries.Function;
using Lucene.Net.Search;
using Lucene.Net.Search.Grouping;
using Lucene.Net.Search.Grouping.Function;
using Lucene.Net.Search.Grouping.Term;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Search.Grouping
{
	/// <summary>Convenience class to perform grouping in a non distributed environment.</summary>
	/// <remarks>Convenience class to perform grouping in a non distributed environment.</remarks>
	/// <lucene.experimental></lucene.experimental>
	public class GroupingSearch
	{
		private readonly string groupField;

		private readonly ValueSource groupFunction;

		private readonly IDictionary<object, object> valueSourceContext;

		private readonly Filter groupEndDocs;

		private Sort groupSort = Sort.RELEVANCE;

		private Sort sortWithinGroup;

		private int groupDocsOffset;

		private int groupDocsLimit = 1;

		private bool fillSortFields;

		private bool includeScores = true;

		private bool includeMaxScore = true;

		private double maxCacheRAMMB;

		private int maxDocsToCache;

		private bool cacheScores;

		private bool allGroups;

		private bool allGroupHeads;

		private int initialSize = 128;

		private ICollection<object> matchingGroups;

		private Bits matchingGroupHeads;

		/// <summary>
		/// Constructs a <code>GroupingSearch</code> instance that groups documents by index terms using the
		/// <see cref="Lucene.Net.Search.FieldCache">Lucene.Net.Search.FieldCache
		/// 	</see>
		/// .
		/// The group field can only have one token per document. This means that the field must not be analysed.
		/// </summary>
		/// <param name="groupField">The name of the field to group by.</param>
		public GroupingSearch(string groupField) : this(groupField, null, null, null)
		{
		}

		/// <summary>
		/// Constructs a <code>GroupingSearch</code> instance that groups documents by function using a
		/// <see cref="Lucene.Net.Queries.Function.ValueSource">Lucene.Net.Queries.Function.ValueSource
		/// 	</see>
		/// instance.
		/// </summary>
		/// <param name="groupFunction">
		/// The function to group by specified as
		/// <see cref="Lucene.Net.Queries.Function.ValueSource">Lucene.Net.Queries.Function.ValueSource
		/// 	</see>
		/// </param>
		/// <param name="valueSourceContext">The context of the specified groupFunction</param>
		public GroupingSearch(ValueSource groupFunction, IDictionary<object, object> valueSourceContext
			) : this(null, groupFunction, valueSourceContext, null)
		{
		}

		/// <summary>Constructor for grouping documents by doc block.</summary>
		/// <remarks>
		/// Constructor for grouping documents by doc block.
		/// This constructor can only be used when documents belonging in a group are indexed in one block.
		/// </remarks>
		/// <param name="groupEndDocs">The filter that marks the last document in all doc blocks
		/// 	</param>
		public GroupingSearch(Filter groupEndDocs) : this(null, null, null, groupEndDocs)
		{
		}

		private GroupingSearch(string groupField, ValueSource groupFunction, IDictionary<
			object, object> valueSourceContext, Filter groupEndDocs)
		{
			this.groupField = groupField;
			this.groupFunction = groupFunction;
			this.valueSourceContext = valueSourceContext;
			this.groupEndDocs = groupEndDocs;
		}

		/// <summary>Executes a grouped search.</summary>
		/// <remarks>Executes a grouped search. Both the first pass and second pass are executed on the specified searcher.
		/// 	</remarks>
		/// <param name="searcher">
		/// The
		/// <see cref="Lucene.Net.Search.IndexSearcher">Lucene.Net.Search.IndexSearcher
		/// 	</see>
		/// instance to execute the grouped search on.
		/// </param>
		/// <param name="query">The query to execute with the grouping</param>
		/// <param name="groupOffset">The group offset</param>
		/// <param name="groupLimit">The number of groups to return from the specified group offset
		/// 	</param>
		/// <returns>
		/// the grouped result as a
		/// <see cref="TopGroups{GROUP_VALUE_TYPE}">TopGroups&lt;GROUP_VALUE_TYPE&gt;</see>
		/// instance
		/// </returns>
		/// <exception cref="System.IO.IOException">If any I/O related errors occur</exception>
		public virtual TopGroups<T> Search<T>(IndexSearcher searcher, Query query, int groupOffset
			, int groupLimit)
		{
			return Search(searcher, null, query, groupOffset, groupLimit);
		}

		/// <summary>Executes a grouped search.</summary>
		/// <remarks>Executes a grouped search. Both the first pass and second pass are executed on the specified searcher.
		/// 	</remarks>
		/// <param name="searcher">
		/// The
		/// <see cref="Lucene.Net.Search.IndexSearcher">Lucene.Net.Search.IndexSearcher
		/// 	</see>
		/// instance to execute the grouped search on.
		/// </param>
		/// <param name="filter">The filter to execute with the grouping</param>
		/// <param name="query">The query to execute with the grouping</param>
		/// <param name="groupOffset">The group offset</param>
		/// <param name="groupLimit">The number of groups to return from the specified group offset
		/// 	</param>
		/// <returns>
		/// the grouped result as a
		/// <see cref="TopGroups{GROUP_VALUE_TYPE}">TopGroups&lt;GROUP_VALUE_TYPE&gt;</see>
		/// instance
		/// </returns>
		/// <exception cref="System.IO.IOException">If any I/O related errors occur</exception>
		public virtual TopGroups<T> Search<T>(IndexSearcher searcher, Filter filter, Query
			 query, int groupOffset, int groupLimit)
		{
			if (groupField != null || groupFunction != null)
			{
				return GroupByFieldOrFunction(searcher, filter, query, groupOffset, groupLimit);
			}
			else
			{
				if (groupEndDocs != null)
				{
					return (TopGroups<T>)GroupByDocBlock(searcher, filter, query, groupOffset, groupLimit
						);
				}
				else
				{
					throw new InvalidOperationException("Either groupField, groupFunction or groupEndDocs must be set."
						);
				}
			}
		}

		// This can't happen...
		/// <exception cref="System.IO.IOException"></exception>
		protected internal virtual TopGroups GroupByFieldOrFunction(IndexSearcher searcher
			, Filter filter, Query query, int groupOffset, int groupLimit)
		{
			int topN = groupOffset + groupLimit;
			AbstractFirstPassGroupingCollector firstPassCollector;
			AbstractAllGroupsCollector allGroupsCollector;
			AbstractAllGroupHeadsCollector allGroupHeadsCollector;
			if (groupFunction != null)
			{
				firstPassCollector = new FunctionFirstPassGroupingCollector(groupFunction, valueSourceContext
					, groupSort, topN);
				if (allGroups)
				{
					allGroupsCollector = new FunctionAllGroupsCollector(groupFunction, valueSourceContext
						);
				}
				else
				{
					allGroupsCollector = null;
				}
				if (allGroupHeads)
				{
					allGroupHeadsCollector = new FunctionAllGroupHeadsCollector(groupFunction, valueSourceContext
						, sortWithinGroup);
				}
				else
				{
					allGroupHeadsCollector = null;
				}
			}
			else
			{
				firstPassCollector = new TermFirstPassGroupingCollector(groupField, groupSort, topN
					);
				if (allGroups)
				{
					allGroupsCollector = new TermAllGroupsCollector(groupField, initialSize);
				}
				else
				{
					allGroupsCollector = null;
				}
				if (allGroupHeads)
				{
					allGroupHeadsCollector = TermAllGroupHeadsCollector.Create(groupField, sortWithinGroup
						, initialSize);
				}
				else
				{
					allGroupHeadsCollector = null;
				}
			}
			Collector firstRound;
			if (allGroupHeads || allGroups)
			{
				IList<Collector> collectors = new AList<Collector>();
				collectors.AddItem(firstPassCollector);
				if (allGroups)
				{
					collectors.AddItem(allGroupsCollector);
				}
				if (allGroupHeads)
				{
					collectors.AddItem(allGroupHeadsCollector);
				}
				firstRound = MultiCollector.Wrap(Sharpen.Collections.ToArray(collectors, new Collector
					[collectors.Count]));
			}
			else
			{
				firstRound = firstPassCollector;
			}
			CachingCollector cachedCollector = null;
			if (maxCacheRAMMB != null || maxDocsToCache != null)
			{
				if (maxCacheRAMMB != null)
				{
					cachedCollector = CachingCollector.Create(firstRound, cacheScores, maxCacheRAMMB);
				}
				else
				{
					cachedCollector = CachingCollector.Create(firstRound, cacheScores, maxDocsToCache
						);
				}
				searcher.Search(query, filter, cachedCollector);
			}
			else
			{
				searcher.Search(query, filter, firstRound);
			}
			if (allGroups)
			{
				matchingGroups = allGroupsCollector.GetGroups();
			}
			else
			{
				matchingGroups = Sharpen.Collections.EmptyList();
			}
			if (allGroupHeads)
			{
				matchingGroupHeads = allGroupHeadsCollector.RetrieveGroupHeads(searcher.GetIndexReader
					().MaxDoc());
			}
			else
			{
				matchingGroupHeads = new Bits.MatchNoBits(searcher.GetIndexReader().MaxDoc());
			}
			ICollection<SearchGroup> topSearchGroups = firstPassCollector.GetTopGroups(groupOffset
				, fillSortFields);
			if (topSearchGroups == null)
			{
				return new TopGroups(new SortField[0], new SortField[0], 0, 0, new GroupDocs[0], 
					float.NaN);
			}
			int topNInsideGroup = groupDocsOffset + groupDocsLimit;
			AbstractSecondPassGroupingCollector secondPassCollector;
			if (groupFunction != null)
			{
				secondPassCollector = new FunctionSecondPassGroupingCollector((ICollection)topSearchGroups
					, groupSort, sortWithinGroup, topNInsideGroup, includeScores, includeMaxScore, fillSortFields
					, groupFunction, valueSourceContext);
			}
			else
			{
				secondPassCollector = new TermSecondPassGroupingCollector(groupField, (ICollection
					)topSearchGroups, groupSort, sortWithinGroup, topNInsideGroup, includeScores, includeMaxScore
					, fillSortFields);
			}
			if (cachedCollector != null && cachedCollector.IsCached())
			{
				cachedCollector.Replay(secondPassCollector);
			}
			else
			{
				searcher.Search(query, filter, secondPassCollector);
			}
			if (allGroups)
			{
				return new TopGroups(secondPassCollector.GetTopGroups(groupDocsOffset), matchingGroups
					.Count);
			}
			else
			{
				return secondPassCollector.GetTopGroups(groupDocsOffset);
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		protected internal virtual TopGroups<object> GroupByDocBlock(IndexSearcher searcher
			, Filter filter, Query query, int groupOffset, int groupLimit)
		{
			int topN = groupOffset + groupLimit;
			BlockGroupingCollector c = new BlockGroupingCollector(groupSort, topN, includeScores
				, groupEndDocs);
			searcher.Search(query, filter, c);
			int topNInsideGroup = groupDocsOffset + groupDocsLimit;
			return c.GetTopGroups(sortWithinGroup, groupOffset, groupDocsOffset, topNInsideGroup
				, fillSortFields);
		}

		/// <summary>Enables caching for the second pass search.</summary>
		/// <remarks>
		/// Enables caching for the second pass search. The cache will not grow over a specified limit in MB.
		/// The cache is filled during the first pass searched and then replayed during the second pass searched.
		/// If the cache grows beyond the specified limit, then the cache is purged and not used in the second pass search.
		/// </remarks>
		/// <param name="maxCacheRAMMB">The maximum amount in MB the cache is allowed to hold
		/// 	</param>
		/// <param name="cacheScores">Whether to cache the scores</param>
		/// <returns><code>this</code></returns>
		public virtual Lucene.Net.Search.Grouping.GroupingSearch SetCachingInMB(double
			 maxCacheRAMMB, bool cacheScores)
		{
			this.maxCacheRAMMB = maxCacheRAMMB;
			this.maxDocsToCache = null;
			this.cacheScores = cacheScores;
			return this;
		}

		/// <summary>Enables caching for the second pass search.</summary>
		/// <remarks>
		/// Enables caching for the second pass search. The cache will not contain more than the maximum specified documents.
		/// The cache is filled during the first pass searched and then replayed during the second pass searched.
		/// If the cache grows beyond the specified limit, then the cache is purged and not used in the second pass search.
		/// </remarks>
		/// <param name="maxDocsToCache">The maximum number of documents the cache is allowed to hold
		/// 	</param>
		/// <param name="cacheScores">Whether to cache the scores</param>
		/// <returns><code>this</code></returns>
		public virtual Lucene.Net.Search.Grouping.GroupingSearch SetCaching(int maxDocsToCache
			, bool cacheScores)
		{
			this.maxDocsToCache = maxDocsToCache;
			this.maxCacheRAMMB = null;
			this.cacheScores = cacheScores;
			return this;
		}

		/// <summary>Disables any enabled cache.</summary>
		/// <remarks>Disables any enabled cache.</remarks>
		/// <returns><code>this</code></returns>
		public virtual Lucene.Net.Search.Grouping.GroupingSearch DisableCaching()
		{
			this.maxCacheRAMMB = null;
			this.maxDocsToCache = null;
			return this;
		}

		/// <summary>Specifies how groups are sorted.</summary>
		/// <remarks>
		/// Specifies how groups are sorted.
		/// Defaults to
		/// <see cref="Lucene.Net.Search.Sort.RELEVANCE">Lucene.Net.Search.Sort.RELEVANCE
		/// 	</see>
		/// .
		/// </remarks>
		/// <param name="groupSort">The sort for the groups.</param>
		/// <returns><code>this</code></returns>
		public virtual Lucene.Net.Search.Grouping.GroupingSearch SetGroupSort(Sort
			 groupSort)
		{
			this.groupSort = groupSort;
			return this;
		}

		/// <summary>Specified how documents inside a group are sorted.</summary>
		/// <remarks>
		/// Specified how documents inside a group are sorted.
		/// Defaults to
		/// <see cref="Lucene.Net.Search.Sort.RELEVANCE">Lucene.Net.Search.Sort.RELEVANCE
		/// 	</see>
		/// .
		/// </remarks>
		/// <param name="sortWithinGroup">The sort for documents inside a group</param>
		/// <returns><code>this</code></returns>
		public virtual Lucene.Net.Search.Grouping.GroupingSearch SetSortWithinGroup
			(Sort sortWithinGroup)
		{
			this.sortWithinGroup = sortWithinGroup;
			return this;
		}

		/// <summary>Specifies the offset for documents inside a group.</summary>
		/// <remarks>Specifies the offset for documents inside a group.</remarks>
		/// <param name="groupDocsOffset">The offset for documents inside a</param>
		/// <returns><code>this</code></returns>
		public virtual Lucene.Net.Search.Grouping.GroupingSearch SetGroupDocsOffset
			(int groupDocsOffset)
		{
			this.groupDocsOffset = groupDocsOffset;
			return this;
		}

		/// <summary>Specifies the number of documents to return inside a group from the specified groupDocsOffset.
		/// 	</summary>
		/// <remarks>Specifies the number of documents to return inside a group from the specified groupDocsOffset.
		/// 	</remarks>
		/// <param name="groupDocsLimit">The number of documents to return inside a group</param>
		/// <returns><code>this</code></returns>
		public virtual Lucene.Net.Search.Grouping.GroupingSearch SetGroupDocsLimit
			(int groupDocsLimit)
		{
			this.groupDocsLimit = groupDocsLimit;
			return this;
		}

		/// <summary>Whether to also fill the sort fields per returned group and groups docs.
		/// 	</summary>
		/// <remarks>Whether to also fill the sort fields per returned group and groups docs.
		/// 	</remarks>
		/// <param name="fillSortFields">Whether to also fill the sort fields per returned group and groups docs
		/// 	</param>
		/// <returns><code>this</code></returns>
		public virtual Lucene.Net.Search.Grouping.GroupingSearch SetFillSortFields
			(bool fillSortFields)
		{
			this.fillSortFields = fillSortFields;
			return this;
		}

		/// <summary>Whether to include the scores per doc inside a group.</summary>
		/// <remarks>Whether to include the scores per doc inside a group.</remarks>
		/// <param name="includeScores">Whether to include the scores per doc inside a group</param>
		/// <returns><code>this</code></returns>
		public virtual Lucene.Net.Search.Grouping.GroupingSearch SetIncludeScores(
			bool includeScores)
		{
			this.includeScores = includeScores;
			return this;
		}

		/// <summary>Whether to include the score of the most relevant document per group.</summary>
		/// <remarks>Whether to include the score of the most relevant document per group.</remarks>
		/// <param name="includeMaxScore">Whether to include the score of the most relevant document per group
		/// 	</param>
		/// <returns><code>this</code></returns>
		public virtual Lucene.Net.Search.Grouping.GroupingSearch SetIncludeMaxScore
			(bool includeMaxScore)
		{
			this.includeMaxScore = includeMaxScore;
			return this;
		}

		/// <summary>Whether to also compute all groups matching the query.</summary>
		/// <remarks>
		/// Whether to also compute all groups matching the query.
		/// This can be used to determine the number of groups, which can be used for accurate pagination.
		/// <p/>
		/// When grouping by doc block the number of groups are automatically included in the
		/// <see cref="TopGroups{GROUP_VALUE_TYPE}">TopGroups&lt;GROUP_VALUE_TYPE&gt;</see>
		/// and this
		/// option doesn't have any influence.
		/// </remarks>
		/// <param name="allGroups">to also compute all groups matching the query</param>
		/// <returns><code>this</code></returns>
		public virtual Lucene.Net.Search.Grouping.GroupingSearch SetAllGroups(bool
			 allGroups)
		{
			this.allGroups = allGroups;
			return this;
		}

		/// <summary>
		/// If
		/// <see cref="SetAllGroups(bool)">SetAllGroups(bool)</see>
		/// was set to <code>true</code> then all matching groups are returned, otherwise
		/// an empty collection is returned.
		/// </summary>
		/// <?></?>
		/// <returns>all matching groups are returned, or an empty collection</returns>
		public virtual ICollection<T> GetAllMatchingGroups<T>()
		{
			return (ICollection<T>)matchingGroups;
		}

		/// <summary>Whether to compute all group heads (most relevant document per group) matching the query.
		/// 	</summary>
		/// <remarks>
		/// Whether to compute all group heads (most relevant document per group) matching the query.
		/// <p/>
		/// This feature isn't enabled when grouping by doc block.
		/// </remarks>
		/// <param name="allGroupHeads">Whether to compute all group heads (most relevant document per group) matching the query
		/// 	</param>
		/// <returns><code>this</code></returns>
		public virtual Lucene.Net.Search.Grouping.GroupingSearch SetAllGroupHeads(
			bool allGroupHeads)
		{
			this.allGroupHeads = allGroupHeads;
			return this;
		}

		/// <summary>
		/// Returns the matching group heads if
		/// <see cref="SetAllGroupHeads(bool)">SetAllGroupHeads(bool)</see>
		/// was set to true or an empty bit set.
		/// </summary>
		/// <returns>
		/// The matching group heads if
		/// <see cref="SetAllGroupHeads(bool)">SetAllGroupHeads(bool)</see>
		/// was set to true or an empty bit set
		/// </returns>
		public virtual Bits GetAllGroupHeads()
		{
			return matchingGroupHeads;
		}

		/// <summary>Sets the initial size of some internal used data structures.</summary>
		/// <remarks>
		/// Sets the initial size of some internal used data structures.
		/// This prevents growing data structures many times. This can improve the performance of the grouping at the cost of
		/// more initial RAM.
		/// <p/>
		/// The
		/// <see cref="SetAllGroups(bool)">SetAllGroups(bool)</see>
		/// and
		/// <see cref="SetAllGroupHeads(bool)">SetAllGroupHeads(bool)</see>
		/// features use this option.
		/// Defaults to 128.
		/// </remarks>
		/// <param name="initialSize">The initial size of some internal used data structures</param>
		/// <returns><code>this</code></returns>
		public virtual Lucene.Net.Search.Grouping.GroupingSearch SetInitialSize(int
			 initialSize)
		{
			this.initialSize = initialSize;
			return this;
		}
	}
}
