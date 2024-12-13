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
        public static FieldGroupingSearch<TGroupValue> ByField<TGroupValue>(string groupField)
        {
            return new FieldGroupingSearch<TGroupValue>(groupField);
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

    public class FieldGroupingSearch<T> : AbstractGroupingSearch<T>
    {
        private readonly string groupField;

        public FieldGroupingSearch(string groupField)
        {
            this.groupField = groupField;
        }

        public override ITopGroups<T> Search(IndexSearcher searcher, Filter filter, Query query, int groupOffset, int groupLimit)
        {
            int topN = groupOffset + groupLimit;
            IAbstractFirstPassGroupingCollector<T> firstPassCollector;
            IAbstractAllGroupsCollector<T> allGroupsCollector;
            AbstractAllGroupHeadsCollector allGroupHeadsCollector;

            if (groupField is null)
            {
                throw IllegalStateException.Create("groupField must be set via the constructor.");
            }

            firstPassCollector = (IAbstractFirstPassGroupingCollector<T>)new TermFirstPassGroupingCollector(groupField, groupSort, topN);
            if (allGroups)
            {
                allGroupsCollector = (IAbstractAllGroupsCollector<T>)new TermAllGroupsCollector(groupField, initialSize);
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

            ICollector firstRound;
            if (allGroupHeads || allGroups)
            {
                JCG.List<ICollector> collectors = new JCG.List<ICollector>();
                collectors.Add(firstPassCollector);

                if (allGroups)
                {
                    collectors.Add(allGroupsCollector);
                }
                if (allGroupHeads)
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
                matchingGroups = (ICollection<T>)allGroupsCollector.Groups;
            }
            else
            {
                matchingGroups = Collections.EmptyList<T>();
            }
            if (allGroupHeads)
            {
                matchingGroupHeads = allGroupHeadsCollector.RetrieveGroupHeads(searcher.IndexReader.MaxDoc);
            }
            else
            {
                matchingGroupHeads = new Bits.MatchNoBits(searcher.IndexReader.MaxDoc);
            }

            IEnumerable<ISearchGroup<T>> topSearchGroups = firstPassCollector.GetTopGroups(groupOffset, fillSortFields);
            if (topSearchGroups is null)
            {
                // LUCENENET specific - optimized empty array creation
                return new TopGroups<T>(Array.Empty<SortField>(), Array.Empty<SortField>(), 0, 0, Array.Empty<GroupDocs<T>>(), float.NaN);
            }

            int topNInsideGroup = groupDocsOffset + groupDocsLimit;
            IAbstractSecondPassGroupingCollector<T> secondPassCollector;

            secondPassCollector = new TermSecondPassGroupingCollector(groupField, topSearchGroups as IEnumerable<ISearchGroup<BytesRef>>,
                groupSort, sortWithinGroup, topNInsideGroup, includeScores, includeMaxScore, fillSortFields)
                as IAbstractSecondPassGroupingCollector<T>;

            if (cachedCollector != null && cachedCollector.IsCached)
            {
                cachedCollector.Replay(secondPassCollector);
            }
            else
            {
                searcher.Search(query, filter, secondPassCollector);
            }

            if (allGroups)
            {
                return new TopGroups<T>(secondPassCollector.GetTopGroups(groupDocsOffset), matchingGroups.Count);
            }
            else
            {
                return secondPassCollector.GetTopGroups(groupDocsOffset);
            }
        }
    }

    public class FunctionGroupingSearch<T> : AbstractGroupingSearch<T>
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

            firstPassCollector = new FunctionFirstPassGroupingCollector<T>(groupFunction, valueSourceContext, groupSort, topN);
            if (allGroups)
            {
                allGroupsCollector = new FunctionAllGroupsCollector<T>(groupFunction, valueSourceContext);
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


            ICollector firstRound;
            if (allGroupHeads || allGroups)
            {
                JCG.List<ICollector> collectors = new JCG.List<ICollector>();
                collectors.Add(firstPassCollector);

                if (allGroups)
                {
                    collectors.Add(allGroupsCollector);
                }
                if (allGroupHeads)
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
                matchingGroups = (ICollection<T>)allGroupsCollector.Groups;
            }
            else
            {
                matchingGroups = Collections.EmptyList<T>();
            }
            if (allGroupHeads)
            {
                matchingGroupHeads = allGroupHeadsCollector.RetrieveGroupHeads(searcher.IndexReader.MaxDoc);
            }
            else
            {
                matchingGroupHeads = new Bits.MatchNoBits(searcher.IndexReader.MaxDoc);
            }

            IEnumerable<ISearchGroup<T>> topSearchGroups = firstPassCollector.GetTopGroups(groupOffset, fillSortFields);
            if (topSearchGroups is null)
            {
                // LUCENENET specific - optimized empty array creation
                return new TopGroups<T>(Array.Empty<SortField>(), Array.Empty<SortField>(), 0, 0, Array.Empty<GroupDocs<T>>(), float.NaN);
            }

            int topNInsideGroup = groupDocsOffset + groupDocsLimit;
            IAbstractSecondPassGroupingCollector<T> secondPassCollector;

            secondPassCollector = new FunctionSecondPassGroupingCollector<T>(topSearchGroups as IEnumerable<ISearchGroup<T>>,
                groupSort, sortWithinGroup, topNInsideGroup, includeScores, includeMaxScore, fillSortFields, groupFunction, valueSourceContext)
                as IAbstractSecondPassGroupingCollector<T>;


            if (cachedCollector != null && cachedCollector.IsCached)
            {
                cachedCollector.Replay(secondPassCollector);
            }
            else
            {
                searcher.Search(query, filter, secondPassCollector);
            }

            if (allGroups)
            {
                return new TopGroups<T>(secondPassCollector.GetTopGroups(groupDocsOffset), matchingGroups.Count);
            }
            else
            {
                return secondPassCollector.GetTopGroups(groupDocsOffset);
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
            BlockGroupingCollector c = new BlockGroupingCollector(groupSort, topN, includeScores, groupEndDocs);
            searcher.Search(query, filter, c);
            int topNInsideGroup = groupDocsOffset + groupDocsLimit;
            return c.GetTopGroups<T>(sortWithinGroup, groupOffset, groupDocsOffset, topNInsideGroup, fillSortFields);
        }
    }

    public abstract class AbstractGroupingSearch<T>
    {
        protected Sort groupSort = Sort.RELEVANCE;
        protected Sort sortWithinGroup;

        protected int groupDocsOffset;
        protected int groupDocsLimit = 1;
        protected bool fillSortFields;
        protected bool includeScores = true;
        protected bool includeMaxScore = true;

        protected double? maxCacheRAMMB;
        protected int? maxDocsToCache;
        protected bool cacheScores;
        protected bool allGroups;
        protected bool allGroupHeads;
        protected int initialSize = 128;

        protected ICollection<T> matchingGroups;
        protected IBits matchingGroupHeads;

        public ITopGroups<T> Search(IndexSearcher searcher, Query query, int groupOffset, int groupLimit)
        {
            return Search(searcher, null, query, groupOffset, groupLimit);
        }

        public abstract ITopGroups<T> Search(IndexSearcher searcher, Filter filter, Query query, int groupOffset, int groupLimit);

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
            this.maxCacheRAMMB = maxCacheRAMMB;
            this.maxDocsToCache = null;
            this.cacheScores = cacheScores;
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
            this.maxDocsToCache = maxDocsToCache;
            this.maxCacheRAMMB = null;
            this.cacheScores = cacheScores;
            return this;
        }

        /// <summary>
        /// Disables any enabled cache.
        /// </summary>
        /// <returns><c>this</c></returns>
        public virtual AbstractGroupingSearch<T> DisableCaching()
        {
            this.maxCacheRAMMB = null;
            this.maxDocsToCache = null;
            return this;
        }

        /// <summary>
        /// Specifies how groups are sorted.
        /// Defaults to <see cref="Sort.RELEVANCE"/>.
        /// </summary>
        /// <param name="groupSort">The sort for the groups.</param>
        /// <returns><c>this</c></returns>
        public virtual AbstractGroupingSearch<T> SetGroupSort(Sort groupSort)
        {
            this.groupSort = groupSort;
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
            this.sortWithinGroup = sortWithinGroup;
            return this;
        }

        /// <summary>
        /// Specifies the offset for documents inside a group.
        /// </summary>
        /// <param name="groupDocsOffset">The offset for documents inside a</param>
        /// <returns><c>this</c></returns>
        public virtual AbstractGroupingSearch<T> SetGroupDocsOffset(int groupDocsOffset)
        {
            this.groupDocsOffset = groupDocsOffset;
            return this;
        }

        /// <summary>
        /// Specifies the number of documents to return inside a group from the specified groupDocsOffset.
        /// </summary>
        /// <param name="groupDocsLimit">The number of documents to return inside a group</param>
        /// <returns><c>this</c></returns>
        public virtual AbstractGroupingSearch<T> SetGroupDocsLimit(int groupDocsLimit)
        {
            this.groupDocsLimit = groupDocsLimit;
            return this;
        }

        /// <summary>
        /// Whether to also fill the sort fields per returned group and groups docs.
        /// </summary>
        /// <param name="fillSortFields">Whether to also fill the sort fields per returned group and groups docs</param>
        /// <returns><c>this</c></returns>
        public virtual AbstractGroupingSearch<T> SetFillSortFields(bool fillSortFields)
        {
            this.fillSortFields = fillSortFields;
            return this;
        }

        /// <summary>
        /// Whether to include the scores per doc inside a group.
        /// </summary>
        /// <param name="includeScores">Whether to include the scores per doc inside a group</param>
        /// <returns><c>this</c></returns>
        public virtual AbstractGroupingSearch<T> SetIncludeScores(bool includeScores)
        {
            this.includeScores = includeScores;
            return this;
        }

        /// <summary>
        /// Whether to include the score of the most relevant document per group.
        /// </summary>
        /// <param name="includeMaxScore">Whether to include the score of the most relevant document per group</param>
        /// <returns><c>this</c></returns>
        public virtual AbstractGroupingSearch<T> SetIncludeMaxScore(bool includeMaxScore)
        {
            this.includeMaxScore = includeMaxScore;
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
            this.allGroups = allGroups;
            return this;
        }

        /// <summary>
        /// If <see cref="SetAllGroups(bool)"/> was set to <c>true</c> then all matching groups are returned, otherwise
        /// an empty collection is returned.
        /// </summary>
        /// <returns>all matching groups are returned, or an empty collection</returns>
        public virtual ICollection<T> GetAllMatchingGroups()
        {
            return matchingGroups;
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
            this.allGroupHeads = allGroupHeads;
            return this;
        }

        /// <summary>
        /// Returns the matching group heads if <see cref="SetAllGroupHeads(bool)"/> was set to true or an empty bit set.
        /// </summary>
        /// <returns>The matching group heads if <see cref="SetAllGroupHeads(bool)"/> was set to true or an empty bit set</returns>
        public virtual IBits GetAllGroupHeads()
        {
            return matchingGroupHeads;
        }

        /// <summary>
        /// Sets the initial size of some internal used data structures.
        /// This prevents growing data structures many times. This can improve the performance of the grouping at the cost of
        /// more initial RAM.
        /// <para>
        /// The <see cref="SetAllGroups(bool)"/> and <see cref="SetAllGroupHeads(bool)"/> features use this option.
        /// Defaults to 128.
        /// </para>
        /// </summary>
        /// <param name="initialSize">The initial size of some internal used data structures</param>
        /// <returns><c>this</c></returns>
        public virtual AbstractGroupingSearch<T> SetInitialSize(int initialSize)
        {
            this.initialSize = initialSize;
            return this;
        }
    }
}
