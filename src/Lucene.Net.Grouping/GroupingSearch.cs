using Lucene.Net.Queries.Function;
using Lucene.Net.Search.Grouping.Function;
using Lucene.Net.Search.Grouping.Terms;
using Lucene.Net.Support;
using Lucene.Net.Util;
using Lucene.Net.Util.Mutable;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using JCG = J2N.Collections.Generic;

namespace Lucene.Net.Search.Grouping
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
    /// Convenience class to perform grouping in a non distributed environment.
    /// @lucene.experimental
    /// </summary>
    public static class GroupingSearch
    {
        public static FieldGroupingSearch ByField(string groupField)
        {
            return new FieldGroupingSearch(groupField);
        }

        public static FunctionGroupingSearch<TMutableValue> ByFunction<TMutableValue>(ValueSource groupFunction, IDictionary valueSourceContext)
            where TMutableValue : MutableValue
        {
            return new FunctionGroupingSearch<TMutableValue>(groupFunction, valueSourceContext);
        }

        public static DocBlockGroupingSearch<TGroupValue> ByDocBlock<TGroupValue>(Filter groupEndDocs)
        {
            return new DocBlockGroupingSearch<TGroupValue>(groupEndDocs);
        }
    }

    public class FieldGroupingSearch : AbstractFieldOrFunctionGroupingSearch<BytesRef>
    {
        private readonly string groupField;
        private int initialSize = 128;

        public FieldGroupingSearch(string groupField)
        {
            this.groupField = groupField;
        }

        public override ITopGroups<BytesRef> Search(IndexSearcher searcher, Filter filter, Query query, int groupOffset, int groupLimit)
        {
            int topN = groupOffset + groupLimit;
            IAbstractFirstPassGroupingCollector<BytesRef> firstPassCollector;
            AbstractAllGroupsCollector<BytesRef> allGroupsCollector;
            AbstractAllGroupHeadsCollector allGroupHeadsCollector;

            if (groupField is null)
            {
                throw IllegalStateException.Create("groupField must be set via the constructor.");
            }

            firstPassCollector = new TermFirstPassGroupingCollector(groupField, GroupSort, topN);
            if (AllGroups)
            {
                allGroupsCollector = new TermAllGroupsCollector(groupField, initialSize);
            }
            else
            {
                allGroupsCollector = null;
            }
            if (AllGroupHeads)
            {
                allGroupHeadsCollector = TermAllGroupHeadsCollector.Create(groupField, SortWithinGroup, initialSize);
            }
            else
            {
                allGroupHeadsCollector = null;
            }

            ICollector firstRound;
            if (AllGroupHeads || AllGroups)
            {
                JCG.List<ICollector> collectors = new JCG.List<ICollector>();
                collectors.Add(firstPassCollector);

                if (AllGroups)
                {
                    collectors.Add(allGroupsCollector);
                }
                if (AllGroupHeads)
                {
                    collectors.Add(allGroupHeadsCollector);
                }
                firstRound = MultiCollector.Wrap(collectors.ToArray(/* new Collector[collectors.size()] */));
            }
            else
            {
                firstRound = firstPassCollector;
            }

            CachingCollector cachedCollector = null;
            if (MaxCacheRAMMB != null || MaxDocsToCache != null)
            {
                if (MaxCacheRAMMB != null)
                {
                    cachedCollector = CachingCollector.Create(firstRound, CacheScores, MaxCacheRAMMB.Value);
                }
                else
                {
                    cachedCollector = CachingCollector.Create(firstRound, CacheScores, MaxDocsToCache.Value);
                }
                searcher.Search(query, filter, cachedCollector);
            }
            else
            {
                searcher.Search(query, filter, firstRound);
            }

            if (AllGroups)
            {
                MatchingGroups = (ICollection<BytesRef>)allGroupsCollector.Groups;
            }
            else
            {
                MatchingGroups = Collections.EmptyList<BytesRef>();
            }
            if (AllGroupHeads)
            {
                MatchingGroupHeads = allGroupHeadsCollector.RetrieveGroupHeads(searcher.IndexReader.MaxDoc);
            }
            else
            {
                MatchingGroupHeads = new Bits.MatchNoBits(searcher.IndexReader.MaxDoc);
            }

            IEnumerable<ISearchGroup<BytesRef>> topSearchGroups = firstPassCollector.GetTopGroups(groupOffset, FillSortFields);
            if (topSearchGroups is null)
            {
                // LUCENENET specific - optimized empty array creation
                return new TopGroups<BytesRef>(Array.Empty<SortField>(), Array.Empty<SortField>(), 0, 0, Array.Empty<GroupDocs<BytesRef>>(), float.NaN);
            }

            int topNInsideGroup = GroupDocsOffset + GroupDocsLimit;
            IAbstractSecondPassGroupingCollector<BytesRef> secondPassCollector;

            secondPassCollector = new TermSecondPassGroupingCollector(groupField, topSearchGroups,
                GroupSort, SortWithinGroup, topNInsideGroup, IncludeScores, IncludeMaxScore, FillSortFields);

            if (cachedCollector != null && cachedCollector.IsCached)
            {
                cachedCollector.Replay(secondPassCollector);
            }
            else
            {
                searcher.Search(query, filter, secondPassCollector);
            }

            if (AllGroups)
            {
                return new TopGroups<BytesRef>(secondPassCollector.GetTopGroups(GroupDocsOffset), MatchingGroups.Count);
            }
            else
            {
                return secondPassCollector.GetTopGroups(GroupDocsOffset);
            }
        }


        /// <summary>
        /// Sets the initial size of some internal used data structures.
        /// This prevents growing data structures many times. This can improve the performance of the grouping at the cost of
        /// more initial RAM.
        /// <para>
        /// The <see cref="AbstractFieldOrFunctionGroupingSearch{T}.SetAllGroups(bool)"/> and
        /// <see cref="AbstractFieldOrFunctionGroupingSearch{T}.SetAllGroupHeads(bool)"/> features use this option.
        /// Defaults to 128.
        /// </para>
        /// </summary>
        /// <param name="initialSize">The initial size of some internal used data structures</param>
        /// <returns><c>this</c></returns>
        public virtual FieldGroupingSearch SetInitialSize(int initialSize)
        {
            this.initialSize = initialSize;
            return this;
        }
    }

    public class FunctionGroupingSearch<T> : AbstractFieldOrFunctionGroupingSearch<T>
        where T : MutableValue
    {
        private readonly ValueSource groupFunction;
        private readonly IDictionary /* Map<?, ?> */ valueSourceContext;

        public FunctionGroupingSearch(ValueSource groupFunction, IDictionary /* Map<?, ?> */ valueSourceContext)
        {
            this.groupFunction = groupFunction;
            this.valueSourceContext = valueSourceContext;
        }

        public override ITopGroups<T> Search(IndexSearcher searcher, Filter filter, Query query, int groupOffset, int groupLimit)
        {
            int topN = groupOffset + groupLimit;
            FunctionFirstPassGroupingCollector<T> firstPassCollector;
            FunctionAllGroupsCollector<T> allGroupsCollector;
            AbstractAllGroupHeadsCollector allGroupHeadsCollector;

            if (groupFunction is null)
            {
                throw IllegalStateException.Create("groupFunction must be set via the constructor by specifying a ValueSource.");
            }

            firstPassCollector = new FunctionFirstPassGroupingCollector<T>(groupFunction, valueSourceContext, GroupSort, topN);
            if (AllGroups)
            {
                allGroupsCollector = new FunctionAllGroupsCollector<T>(groupFunction, valueSourceContext);
            }
            else
            {
                allGroupsCollector = null;
            }
            if (AllGroupHeads)
            {
                allGroupHeadsCollector = new FunctionAllGroupHeadsCollector(groupFunction, valueSourceContext, SortWithinGroup);
            }
            else
            {
                allGroupHeadsCollector = null;
            }


            ICollector firstRound;
            if (AllGroupHeads || AllGroups)
            {
                JCG.List<ICollector> collectors = new JCG.List<ICollector>();
                collectors.Add(firstPassCollector);

                if (AllGroups)
                {
                    collectors.Add(allGroupsCollector);
                }
                if (AllGroupHeads)
                {
                    collectors.Add(allGroupHeadsCollector);
                }
                firstRound = MultiCollector.Wrap(collectors.ToArray(/* new Collector[collectors.size()] */));
            }
            else
            {
                firstRound = firstPassCollector;
            }

            CachingCollector cachedCollector = null;
            if (MaxCacheRAMMB != null || MaxDocsToCache != null)
            {
                if (MaxCacheRAMMB != null)
                {
                    cachedCollector = CachingCollector.Create(firstRound, CacheScores, MaxCacheRAMMB.Value);
                }
                else
                {
                    cachedCollector = CachingCollector.Create(firstRound, CacheScores, MaxDocsToCache.Value);
                }
                searcher.Search(query, filter, cachedCollector);
            }
            else
            {
                searcher.Search(query, filter, firstRound);
            }

            if (AllGroups)
            {
                MatchingGroups = (ICollection<T>)allGroupsCollector.Groups;
            }
            else
            {
                MatchingGroups = Collections.EmptyList<T>();
            }
            if (AllGroupHeads)
            {
                MatchingGroupHeads = allGroupHeadsCollector.RetrieveGroupHeads(searcher.IndexReader.MaxDoc);
            }
            else
            {
                MatchingGroupHeads = new Bits.MatchNoBits(searcher.IndexReader.MaxDoc);
            }

            IEnumerable<ISearchGroup<T>> topSearchGroups = firstPassCollector.GetTopGroups(groupOffset, FillSortFields);
            if (topSearchGroups is null)
            {
                // LUCENENET specific - optimized empty array creation
                return new TopGroups<T>(Array.Empty<SortField>(), Array.Empty<SortField>(), 0, 0, Array.Empty<GroupDocs<T>>(), float.NaN);
            }

            int topNInsideGroup = GroupDocsOffset + GroupDocsLimit;
            IAbstractSecondPassGroupingCollector<T> secondPassCollector;

            secondPassCollector = new FunctionSecondPassGroupingCollector<T>(topSearchGroups,
                GroupSort, SortWithinGroup, topNInsideGroup, IncludeScores, IncludeMaxScore, FillSortFields, groupFunction, valueSourceContext);


            if (cachedCollector != null && cachedCollector.IsCached)
            {
                cachedCollector.Replay(secondPassCollector);
            }
            else
            {
                searcher.Search(query, filter, secondPassCollector);
            }

            if (AllGroups)
            {
                return new TopGroups<T>(secondPassCollector.GetTopGroups(GroupDocsOffset), MatchingGroups.Count);
            }
            else
            {
                return secondPassCollector.GetTopGroups(GroupDocsOffset);
            }
        }
    }

    public class DocBlockGroupingSearch<T> : AbstractGroupingSearch<T>
    {
        private readonly Filter groupEndDocs;

        public DocBlockGroupingSearch(Filter groupEndDocs)
        {
            this.groupEndDocs = groupEndDocs;
        }

        public override ITopGroups<T> Search(IndexSearcher searcher, Filter filter, Query query, int groupOffset, int groupLimit)
        {
            int topN = groupOffset + groupLimit;
            BlockGroupingCollector c = new BlockGroupingCollector(GroupSort, topN, IncludeScores, groupEndDocs);
            searcher.Search(query, filter, c);
            int topNInsideGroup = GroupDocsOffset + GroupDocsLimit;
            return c.GetTopGroups<T>(SortWithinGroup, groupOffset, GroupDocsOffset, topNInsideGroup, FillSortFields);
        }
    }

    public abstract class AbstractFieldOrFunctionGroupingSearch<T> : AbstractGroupingSearch<T>
    {
        // LUCENENET: Converted to protected properties
        protected bool IncludeMaxScore { get; private set; } = true;

        protected double? MaxCacheRAMMB { get; private set; }
        protected int? MaxDocsToCache { get; private set; }
        protected bool CacheScores { get; private set; }
        protected bool AllGroups { get; private set; }
        protected bool AllGroupHeads { get; private set; }

        protected ICollection<T> MatchingGroups { get; set; }
        protected IBits MatchingGroupHeads { get; set; }

        /// <summary>
        /// Enables caching for the second pass search. The cache will not grow over a specified limit in MB.
        /// The cache is filled during the first pass searched and then replayed during the second pass searched.
        /// If the cache grows beyond the specified limit, then the cache is purged and not used in the second pass search.
        /// </summary>
        /// <param name="maxCacheRAMMB">The maximum amount in MB the cache is allowed to hold</param>
        /// <param name="cacheScores">Whether to cache the scores</param>
        /// <returns><c>this</c></returns>
        public virtual AbstractGroupingSearch<T> SetCachingInMB(double maxCacheRAMMB, bool cacheScores)
        {
            this.MaxCacheRAMMB = maxCacheRAMMB;
            this.MaxDocsToCache = null;
            this.CacheScores = cacheScores;
            return this;
        }

        /// <summary>
        /// Enables caching for the second pass search. The cache will not contain more than the maximum specified documents.
        /// The cache is filled during the first pass searched and then replayed during the second pass searched.
        /// If the cache grows beyond the specified limit, then the cache is purged and not used in the second pass search.
        /// </summary>
        /// <param name="maxDocsToCache">The maximum number of documents the cache is allowed to hold</param>
        /// <param name="cacheScores">Whether to cache the scores</param>
        /// <returns><c>this</c></returns>
        public virtual AbstractGroupingSearch<T> SetCaching(int maxDocsToCache, bool cacheScores)
        {
            this.MaxDocsToCache = maxDocsToCache;
            this.MaxCacheRAMMB = null;
            this.CacheScores = cacheScores;
            return this;
        }

        /// <summary>
        /// Disables any enabled cache.
        /// </summary>
        /// <returns><c>this</c></returns>
        public virtual AbstractGroupingSearch<T> DisableCaching()
        {
            this.MaxCacheRAMMB = null;
            this.MaxDocsToCache = null;
            return this;
        }

        /// <summary>
        /// Whether to include the score of the most relevant document per group.
        /// </summary>
        /// <param name="includeMaxScore">Whether to include the score of the most relevant document per group</param>
        /// <returns><c>this</c></returns>
        public virtual AbstractGroupingSearch<T> SetIncludeMaxScore(bool includeMaxScore)
        {
            this.IncludeMaxScore = includeMaxScore;
            return this;
        }

        /// <summary>
        /// Whether to also compute all groups matching the query.
        /// This can be used to determine the number of groups, which can be used for accurate pagination.
        /// <para>
        /// When grouping by doc block the number of groups are automatically included in the <see cref="TopGroups"/> and this
        /// option doesn't have any influence.
        /// </para>
        /// </summary>
        /// <param name="allGroups">to also compute all groups matching the query</param>
        /// <returns><c>this</c></returns>
        public virtual AbstractGroupingSearch<T> SetAllGroups(bool allGroups)
        {
            this.AllGroups = allGroups;
            return this;
        }

        /// <summary>
        /// If <see cref="SetAllGroups(bool)"/> was set to <c>true</c> then all matching groups are returned, otherwise
        /// an empty collection is returned.
        /// </summary>
        /// <returns>all matching groups are returned, or an empty collection</returns>
        public virtual ICollection<T> GetAllMatchingGroups()
        {
            return MatchingGroups;
        }

        /// <summary>
        /// Whether to compute all group heads (most relevant document per group) matching the query.
        /// <para>
        /// This feature isn't enabled when grouping by doc block.
        /// </para>
        /// </summary>
        /// <param name="allGroupHeads">Whether to compute all group heads (most relevant document per group) matching the query</param>
        /// <returns><c>this</c></returns>
        public virtual AbstractGroupingSearch<T> SetAllGroupHeads(bool allGroupHeads)
        {
            this.AllGroupHeads = allGroupHeads;
            return this;
        }

        /// <summary>
        /// Returns the matching group heads if <see cref="SetAllGroupHeads(bool)"/> was set to true or an empty bit set.
        /// </summary>
        /// <returns>The matching group heads if <see cref="SetAllGroupHeads(bool)"/> was set to true or an empty bit set</returns>
        public virtual IBits GetAllGroupHeads()
        {
            return MatchingGroupHeads;
        }
    }

    public abstract class AbstractGroupingSearch<T>
    {
        // LUCENENET: Converted to protected properties
        protected Sort GroupSort { get; private set; } = Sort.RELEVANCE;
        protected Sort SortWithinGroup { get; private set; }

        protected int GroupDocsOffset { get; private set; }
        protected int GroupDocsLimit { get; private set; } = 1;
        protected bool FillSortFields { get; private set; }
        protected bool IncludeScores { get; private set; } = true;

        public ITopGroups<T> Search(IndexSearcher searcher, Query query, int groupOffset, int groupLimit)
        {
            return Search(searcher, null, query, groupOffset, groupLimit);
        }

        public abstract ITopGroups<T> Search(IndexSearcher searcher, Filter filter, Query query, int groupOffset, int groupLimit);

        /// <summary>
        /// Specifies how groups are sorted.
        /// Defaults to <see cref="Sort.RELEVANCE"/>.
        /// </summary>
        /// <param name="groupSort">The sort for the groups.</param>
        /// <returns><c>this</c></returns>
        public virtual AbstractGroupingSearch<T> SetGroupSort(Sort groupSort)
        {
            this.GroupSort = groupSort;
            return this;
        }

        /// <summary>
        /// Specified how documents inside a group are sorted.
        /// Defaults to <see cref="Sort.RELEVANCE"/>.
        /// </summary>
        /// <param name="sortWithinGroup">The sort for documents inside a group</param>
        /// <returns><c>this</c></returns>
        public virtual AbstractGroupingSearch<T> SetSortWithinGroup(Sort sortWithinGroup)
        {
            this.SortWithinGroup = sortWithinGroup;
            return this;
        }

        /// <summary>
        /// Specifies the offset for documents inside a group.
        /// </summary>
        /// <param name="groupDocsOffset">The offset for documents inside a</param>
        /// <returns><c>this</c></returns>
        public virtual AbstractGroupingSearch<T> SetGroupDocsOffset(int groupDocsOffset)
        {
            this.GroupDocsOffset = groupDocsOffset;
            return this;
        }

        /// <summary>
        /// Specifies the number of documents to return inside a group from the specified groupDocsOffset.
        /// </summary>
        /// <param name="groupDocsLimit">The number of documents to return inside a group</param>
        /// <returns><c>this</c></returns>
        public virtual AbstractGroupingSearch<T> SetGroupDocsLimit(int groupDocsLimit)
        {
            this.GroupDocsLimit = groupDocsLimit;
            return this;
        }

        /// <summary>
        /// Whether to also fill the sort fields per returned group and groups docs.
        /// </summary>
        /// <param name="fillSortFields">Whether to also fill the sort fields per returned group and groups docs</param>
        /// <returns><c>this</c></returns>
        public virtual AbstractGroupingSearch<T> SetFillSortFields(bool fillSortFields)
        {
            this.FillSortFields = fillSortFields;
            return this;
        }

        /// <summary>
        /// Whether to include the scores per doc inside a group.
        /// </summary>
        /// <param name="includeScores">Whether to include the scores per doc inside a group</param>
        /// <returns><c>this</c></returns>
        public virtual AbstractGroupingSearch<T> SetIncludeScores(bool includeScores)
        {
            this.IncludeScores = includeScores;
            return this;
        }
    }
}
