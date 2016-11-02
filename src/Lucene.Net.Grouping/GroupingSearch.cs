using Lucene.Net.Queries.Function;
using Lucene.Net.Search;
using Lucene.Net.Search.Grouping;
using Lucene.Net.Search.Grouping.Function;
using Lucene.Net.Search.Grouping.Terms;
using Lucene.Net.Util;
using Lucene.Net.Util.Mutable;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lucene.Net.Search.Grouping
{
    /// <summary>
    /// Convenience class to perform grouping in a non distributed environment.
    /// @lucene.experimental
    /// </summary>
    public class GroupingSearch
    {
        private readonly string groupField;
        private readonly ValueSource groupFunction;
        private readonly IDictionary /* Map<?, ?> */ valueSourceContext;
        private readonly Filter groupEndDocs;

        private Sort groupSort = Sort.RELEVANCE;
        private Sort sortWithinGroup;

        private int groupDocsOffset;
        private int groupDocsLimit = 1;
        private bool fillSortFields;
        private bool includeScores = true;
        private bool includeMaxScore = true;

        private double? maxCacheRAMMB;
        private int? maxDocsToCache;
        private bool cacheScores;
        private bool allGroups;
        private bool allGroupHeads;
        private int initialSize = 128;

        private IList /* Collection<?> */ matchingGroups;
        private Bits matchingGroupHeads;

        /**
         * Constructs a <code>GroupingSearch</code> instance that groups documents by index terms using the {@link FieldCache}.
         * The group field can only have one token per document. This means that the field must not be analysed.
         *
         * @param groupField The name of the field to group by.
         */
        public GroupingSearch(string groupField)
            : this(groupField, null, null, null)
        {
        }

        /**
         * Constructs a <code>GroupingSearch</code> instance that groups documents by function using a {@link ValueSource}
         * instance.
         *
         * @param groupFunction      The function to group by specified as {@link ValueSource}
         * @param valueSourceContext The context of the specified groupFunction
         */
        public GroupingSearch(ValueSource groupFunction, IDictionary /* Map<?, ?> */ valueSourceContext)
            : this(null, groupFunction, valueSourceContext, null)
        {

        }

        /**
         * Constructor for grouping documents by doc block.
         * This constructor can only be used when documents belonging in a group are indexed in one block.
         *
         * @param groupEndDocs The filter that marks the last document in all doc blocks
         */
        public GroupingSearch(Filter groupEndDocs)
            : this(null, null, null, groupEndDocs)
        {
        }

        private GroupingSearch(string groupField, ValueSource groupFunction, IDictionary /* Map<?, ?> */ valueSourceContext, Filter groupEndDocs)
        {
            this.groupField = groupField;
            this.groupFunction = groupFunction;
            this.valueSourceContext = valueSourceContext;
            this.groupEndDocs = groupEndDocs;
        }

        /**
         * Executes a grouped search. Both the first pass and second pass are executed on the specified searcher.
         *
         * @param searcher    The {@link org.apache.lucene.search.IndexSearcher} instance to execute the grouped search on.
         * @param query       The query to execute with the grouping
         * @param groupOffset The group offset
         * @param groupLimit  The number of groups to return from the specified group offset
         * @return the grouped result as a {@link TopGroups} instance
         * @throws IOException If any I/O related errors occur
         */
        public ITopGroups<TGroupValue> Search<TGroupValue>(IndexSearcher searcher, Query query, int groupOffset, int groupLimit)
        {
            return Search<TGroupValue>(searcher, null, query, groupOffset, groupLimit);
        }

        /**
         * Executes a grouped search. Both the first pass and second pass are executed on the specified searcher.
         *
         * @param searcher    The {@link org.apache.lucene.search.IndexSearcher} instance to execute the grouped search on.
         * @param filter      The filter to execute with the grouping
         * @param query       The query to execute with the grouping
         * @param groupOffset The group offset
         * @param groupLimit  The number of groups to return from the specified group offset
         * @return the grouped result as a {@link TopGroups} instance
         * @throws IOException If any I/O related errors occur
         */
        public ITopGroups<TGroupValue> Search<TGroupValue>(IndexSearcher searcher, Filter filter, Query query, int groupOffset, int groupLimit)
        {
            if (groupField != null || groupFunction != null)
            {
                return GroupByFieldOrFunction<TGroupValue>(searcher, filter, query, groupOffset, groupLimit);
            }
            else if (groupEndDocs != null)
            {
                return (TopGroups<TGroupValue>)GroupByDocBlock<TGroupValue>(searcher, filter, query, groupOffset, groupLimit);
            }
            else
            {
                throw new InvalidOperationException("Either groupField, groupFunction or groupEndDocs must be set."); // This can't happen...
            }
        }

        protected ITopGroups<TGroupValue> GroupByFieldOrFunction<TGroupValue>(IndexSearcher searcher, Filter filter, Query query, int groupOffset, int groupLimit)
        {
            int topN = groupOffset + groupLimit;
            IAbstractFirstPassGroupingCollector<TGroupValue> firstPassCollector;
            IAbstractAllGroupsCollector<TGroupValue> allGroupsCollector;
            AbstractAllGroupHeadsCollector allGroupHeadsCollector;
            if (groupFunction != null)
            {
                firstPassCollector = (IAbstractFirstPassGroupingCollector<TGroupValue>)new FunctionFirstPassGroupingCollector(groupFunction, valueSourceContext, groupSort, topN);
                if (allGroups)
                {
                    allGroupsCollector = (IAbstractAllGroupsCollector<TGroupValue>)new FunctionAllGroupsCollector(groupFunction, valueSourceContext);
                }
                else
                {
                    allGroupsCollector = null;
                }
                if (allGroupHeads)
                {
                    allGroupHeadsCollector = new FunctionAllGroupHeadsCollector(groupFunction, valueSourceContext, sortWithinGroup);
                }
                else
                {
                    allGroupHeadsCollector = null;
                }
            }
            else
            {
                firstPassCollector = (IAbstractFirstPassGroupingCollector<TGroupValue>)new TermFirstPassGroupingCollector(groupField, groupSort, topN);
                if (allGroups)
                {
                    allGroupsCollector = (IAbstractAllGroupsCollector<TGroupValue>)new TermAllGroupsCollector(groupField, initialSize);
                }
                else
                {
                    allGroupsCollector = null;
                }
                if (allGroupHeads)
                {
                    allGroupHeadsCollector = TermAllGroupHeadsCollector.Create(groupField, sortWithinGroup, initialSize);
                }
                else
                {
                    allGroupHeadsCollector = null;
                }
            }

            Collector firstRound;
            if (allGroupHeads || allGroups)
            {
                List<Collector> collectors = new List<Collector>();
                // LUCENENET TODO: Make the Collector abstract class into an interface
                // so we can remove the casting here
                collectors.Add((Collector)firstPassCollector);
                if (allGroups)
                {
                    // LUCENENET TODO: Make the Collector abstract class into an interface
                    // so we can remove the casting here
                    collectors.Add((Collector)allGroupsCollector);
                }
                if (allGroupHeads)
                {
                    collectors.Add(allGroupHeadsCollector);
                }
                firstRound = MultiCollector.Wrap(collectors.ToArray(/* new Collector[collectors.size()] */));
            }
            else
            {
                // LUCENENET TODO: Make the Collector abstract class into an interface
                // so we can remove the casting here
                firstRound = (Collector)firstPassCollector;
            }

            CachingCollector cachedCollector = null;
            if (maxCacheRAMMB != null || maxDocsToCache != null)
            {
                if (maxCacheRAMMB != null)
                {
                    cachedCollector = CachingCollector.Create(firstRound, cacheScores, maxCacheRAMMB.Value);
                }
                else
                {
                    cachedCollector = CachingCollector.Create(firstRound, cacheScores, maxDocsToCache.Value);
                }
                searcher.Search(query, filter, cachedCollector);
            }
            else
            {
                searcher.Search(query, filter, firstRound);
            }

            if (allGroups)
            {
                matchingGroups = (IList)allGroupsCollector.Groups;
            }
            else
            {
                matchingGroups = new List<TGroupValue>();
            }
            if (allGroupHeads)
            {
                matchingGroupHeads = allGroupHeadsCollector.RetrieveGroupHeads(searcher.IndexReader.MaxDoc);
            }
            else
            {
                matchingGroupHeads = new Bits_MatchNoBits(searcher.IndexReader.MaxDoc);
            }

            IEnumerable<ISearchGroup<TGroupValue>> topSearchGroups = firstPassCollector.GetTopGroups(groupOffset, fillSortFields);
            if (topSearchGroups == null)
            {
                return new TopGroups<TGroupValue>(new SortField[0], new SortField[0], 0, 0, new GroupDocs<TGroupValue>[0], float.NaN);
            }

            int topNInsideGroup = groupDocsOffset + groupDocsLimit;
            IAbstractSecondPassGroupingCollector<TGroupValue> secondPassCollector;
            if (groupFunction != null)
            {
                secondPassCollector = (IAbstractSecondPassGroupingCollector<TGroupValue>)new FunctionSecondPassGroupingCollector(topSearchGroups as ICollection<SearchGroup<MutableValue>>, groupSort, sortWithinGroup, topNInsideGroup, includeScores, includeMaxScore, fillSortFields, groupFunction, valueSourceContext);
            }
            else
            {
                secondPassCollector = (IAbstractSecondPassGroupingCollector<TGroupValue>)new TermSecondPassGroupingCollector(groupField, topSearchGroups as ICollection<SearchGroup<BytesRef>>, groupSort, sortWithinGroup, topNInsideGroup, includeScores, includeMaxScore, fillSortFields);
            }

            if (cachedCollector != null && cachedCollector.Cached)
            {
                cachedCollector.Replay((Collector)secondPassCollector);
            }
            else
            {
                searcher.Search(query, filter, (Collector)secondPassCollector);
            }

            if (allGroups)
            {
                return new TopGroups<TGroupValue>(secondPassCollector.GetTopGroups(groupDocsOffset), matchingGroups.Count);
            }
            else
            {
                return secondPassCollector.GetTopGroups(groupDocsOffset);
            }
        }

        protected ITopGroups<T> GroupByDocBlock<T>(IndexSearcher searcher, Filter filter, Query query, int groupOffset, int groupLimit)
        {
            int topN = groupOffset + groupLimit;
            BlockGroupingCollector c = new BlockGroupingCollector(groupSort, topN, includeScores, groupEndDocs);
            searcher.Search(query, filter, c);
            int topNInsideGroup = groupDocsOffset + groupDocsLimit;
            return c.GetTopGroups<T>(sortWithinGroup, groupOffset, groupDocsOffset, topNInsideGroup, fillSortFields);
        }

        /**
         * Enables caching for the second pass search. The cache will not grow over a specified limit in MB.
         * The cache is filled during the first pass searched and then replayed during the second pass searched.
         * If the cache grows beyond the specified limit, then the cache is purged and not used in the second pass search.
         *
         * @param maxCacheRAMMB The maximum amount in MB the cache is allowed to hold
         * @param cacheScores   Whether to cache the scores
         * @return <code>this</code>
         */
        public GroupingSearch SetCachingInMB(double maxCacheRAMMB, bool cacheScores)
        {
            this.maxCacheRAMMB = maxCacheRAMMB;
            this.maxDocsToCache = null;
            this.cacheScores = cacheScores;
            return this;
        }

        /**
         * Enables caching for the second pass search. The cache will not contain more than the maximum specified documents.
         * The cache is filled during the first pass searched and then replayed during the second pass searched.
         * If the cache grows beyond the specified limit, then the cache is purged and not used in the second pass search.
         *
         * @param maxDocsToCache The maximum number of documents the cache is allowed to hold
         * @param cacheScores    Whether to cache the scores
         * @return <code>this</code>
         */
        public GroupingSearch SetCaching(int maxDocsToCache, bool cacheScores)
        {
            this.maxDocsToCache = maxDocsToCache;
            this.maxCacheRAMMB = null;
            this.cacheScores = cacheScores;
            return this;
        }

        /**
         * Disables any enabled cache.
         *
         * @return <code>this</code>
         */
        public GroupingSearch DisableCaching()
        {
            this.maxCacheRAMMB = null;
            this.maxDocsToCache = null;
            return this;
        }

        /**
         * Specifies how groups are sorted.
         * Defaults to {@link Sort#RELEVANCE}.
         *
         * @param groupSort The sort for the groups.
         * @return <code>this</code>
         */
        public GroupingSearch SetGroupSort(Sort groupSort)
        {
            this.groupSort = groupSort;
            return this;
        }

        /**
         * Specified how documents inside a group are sorted.
         * Defaults to {@link Sort#RELEVANCE}.
         *
         * @param sortWithinGroup The sort for documents inside a group
         * @return <code>this</code>
         */
        public GroupingSearch SetSortWithinGroup(Sort sortWithinGroup)
        {
            this.sortWithinGroup = sortWithinGroup;
            return this;
        }

        /**
         * Specifies the offset for documents inside a group.
         *
         * @param groupDocsOffset The offset for documents inside a
         * @return <code>this</code>
         */
        public GroupingSearch SetGroupDocsOffset(int groupDocsOffset)
        {
            this.groupDocsOffset = groupDocsOffset;
            return this;
        }

        /**
         * Specifies the number of documents to return inside a group from the specified groupDocsOffset.
         *
         * @param groupDocsLimit The number of documents to return inside a group
         * @return <code>this</code>
         */
        public GroupingSearch SetGroupDocsLimit(int groupDocsLimit)
        {
            this.groupDocsLimit = groupDocsLimit;
            return this;
        }

        /**
         * Whether to also fill the sort fields per returned group and groups docs.
         *
         * @param fillSortFields Whether to also fill the sort fields per returned group and groups docs
         * @return <code>this</code>
         */
        public GroupingSearch SetFillSortFields(bool fillSortFields)
        {
            this.fillSortFields = fillSortFields;
            return this;
        }

        /**
         * Whether to include the scores per doc inside a group.
         *
         * @param includeScores Whether to include the scores per doc inside a group
         * @return <code>this</code>
         */
        public GroupingSearch SetIncludeScores(bool includeScores)
        {
            this.includeScores = includeScores;
            return this;
        }

        /**
         * Whether to include the score of the most relevant document per group.
         *
         * @param includeMaxScore Whether to include the score of the most relevant document per group
         * @return <code>this</code>
         */
        public GroupingSearch SetIncludeMaxScore(bool includeMaxScore)
        {
            this.includeMaxScore = includeMaxScore;
            return this;
        }

        /**
         * Whether to also compute all groups matching the query.
         * This can be used to determine the number of groups, which can be used for accurate pagination.
         * <p/>
         * When grouping by doc block the number of groups are automatically included in the {@link TopGroups} and this
         * option doesn't have any influence.
         *
         * @param allGroups to also compute all groups matching the query
         * @return <code>this</code>
         */
        public GroupingSearch SetAllGroups(bool allGroups)
        {
            this.allGroups = allGroups;
            return this;
        }

        /**
         * If {@link #setAllGroups(boolean)} was set to <code>true</code> then all matching groups are returned, otherwise
         * an empty collection is returned.
         *
         * @param <T> The group value type. This can be a {@link BytesRef} or a {@link MutableValue} instance. If grouping
         *            by doc block this the group value is always <code>null</code>.
         * @return all matching groups are returned, or an empty collection
         */
        public ICollection<T> GetAllMatchingGroups<T>()
        {
            return (ICollection<T>)matchingGroups;
        }

        /**
         * Whether to compute all group heads (most relevant document per group) matching the query.
         * <p/>
         * This feature isn't enabled when grouping by doc block.
         *
         * @param allGroupHeads Whether to compute all group heads (most relevant document per group) matching the query
         * @return <code>this</code>
         */
        public GroupingSearch SetAllGroupHeads(bool allGroupHeads)
        {
            this.allGroupHeads = allGroupHeads;
            return this;
        }

        /**
         * Returns the matching group heads if {@link #setAllGroupHeads(boolean)} was set to true or an empty bit set.
         *
         * @return The matching group heads if {@link #setAllGroupHeads(boolean)} was set to true or an empty bit set
         */
        public Bits GetAllGroupHeads()
        {
            return matchingGroupHeads;
        }

        /**
         * Sets the initial size of some internal used data structures.
         * This prevents growing data structures many times. This can improve the performance of the grouping at the cost of
         * more initial RAM.
         * <p/>
         * The {@link #setAllGroups} and {@link #setAllGroupHeads} features use this option.
         * Defaults to 128.
         *
         * @param initialSize The initial size of some internal used data structures
         * @return <code>this</code>
         */
        public GroupingSearch SetInitialSize(int initialSize)
        {
            this.initialSize = initialSize;
            return this;
        }
    }
}
