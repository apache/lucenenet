﻿using Lucene.Net.Queries.Function;
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

        private ICollection /* Collection<?> */ matchingGroups;
        private IBits matchingGroupHeads;

        /// <summary>
        /// Constructs a <see cref="GroupingSearch"/> instance that groups documents by index terms using the <see cref="FieldCache"/>.
        /// The group field can only have one token per document. This means that the field must not be analysed.
        /// </summary>
        /// <param name="groupField">The name of the field to group by.</param>
        public GroupingSearch(string groupField)
            : this(groupField, null, null, null)
        {
        }

        /// <summary>
        /// Constructs a <see cref="GroupingSearch"/> instance that groups documents by function using a <see cref="ValueSource"/>
        /// instance.
        /// </summary>
        /// <param name="groupFunction">The function to group by specified as <see cref="ValueSource"/></param>
        /// <param name="valueSourceContext">The context of the specified groupFunction</param>
        public GroupingSearch(ValueSource groupFunction, IDictionary /* Map<?, ?> */ valueSourceContext)
            : this(null, groupFunction, valueSourceContext, null)
        {

        }

        /// <summary>
        /// Constructor for grouping documents by doc block.
        /// This constructor can only be used when documents belonging in a group are indexed in one block.
        /// </summary>
        /// <param name="groupEndDocs">The filter that marks the last document in all doc blocks</param>
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

        /// <summary>
        /// Executes a grouped search. Both the first pass and second pass are executed on the specified searcher.
        /// </summary>
        /// <param name="searcher">The <see cref="IndexSearcher"/> instance to execute the grouped search on.</param>
        /// <param name="query">The query to execute with the grouping</param>
        /// <param name="groupOffset">The group offset</param>
        /// <param name="groupLimit">The number of groups to return from the specified group offset</param>
        /// <returns>the grouped result as a <see cref="ITopGroups{Object}"/> instance</returns>
        /// <exception cref="IOException">If any I/O related errors occur</exception>
        // LUCENENET additional method signature. Makes discovering the return type easier. 
        public virtual ITopGroups<object> Search(IndexSearcher searcher, Query query, int groupOffset, int groupLimit)
        {
            return Search<object>(searcher, null, query, groupOffset, groupLimit);
        }

        /// <summary>
        /// Executes a grouped search. Both the first pass and second pass are executed on the specified searcher.
        /// </summary>
        /// <param name="searcher">The <see cref="IndexSearcher"/> instance to execute the grouped search on.</param>
        /// <param name="filter">The filter to execute with the grouping</param>
        /// <param name="query">The query to execute with the grouping</param>
        /// <param name="groupOffset">The group offset</param>
        /// <param name="groupLimit">The number of groups to return from the specified group offset</param>
        /// <returns>the grouped result as a <see cref="ITopGroups{Object}"/> instance</returns>
        /// <exception cref="IOException">If any I/O related errors occur</exception>
        // LUCENENET additional method signature. Makes discovering the return type easier. 
        public virtual ITopGroups<object> Search(IndexSearcher searcher, Filter filter, Query query, int groupOffset, int groupLimit)
        {
            if (groupFunction != null)
            {
                return GroupByFunction<MutableValue>(searcher, filter, query, groupOffset, groupLimit);
            }
            else if (groupField != null)
            {
                return GroupByField<BytesRef>(searcher, filter, query, groupOffset, groupLimit);
            }
            else if (groupEndDocs != null)
            {
                return GroupByDocBlock<object>(searcher, filter, query, groupOffset, groupLimit);
            }
            else
            {
                throw IllegalStateException.Create("Either groupField, groupFunction or groupEndDocs must be set."); // This can't happen...
            }
        }

        /// <summary>
        /// Executes a grouped search. Both the first pass and second pass are executed on the specified searcher.
        /// </summary>
        /// <typeparam name="TGroupValue">The expected return type of the search.</typeparam>
        /// <param name="searcher">The <see cref="IndexSearcher"/> instance to execute the grouped search on.</param>
        /// <param name="query">The query to execute with the grouping</param>
        /// <param name="groupOffset">The group offset</param>
        /// <param name="groupLimit">The number of groups to return from the specified group offset</param>
        /// <returns>the grouped result as a <see cref="ITopGroups{Object}"/> instance</returns>
        /// <exception cref="IOException">If any I/O related errors occur</exception>
        public virtual ITopGroups<TGroupValue> Search<TGroupValue>(IndexSearcher searcher, Query query, int groupOffset, int groupLimit)
        {
            return Search<TGroupValue>(searcher, null, query, groupOffset, groupLimit);
        }

        /// <summary>
        /// Executes a grouped search. Both the first pass and second pass are executed on the specified searcher.
        /// </summary>
        /// <typeparam name="TGroupValue">The expected return type of the search.</typeparam>
        /// <param name="searcher">The <see cref="IndexSearcher"/> instance to execute the grouped search on.</param>
        /// <param name="filter">The filter to execute with the grouping</param>
        /// <param name="query">The query to execute with the grouping</param>
        /// <param name="groupOffset">The group offset</param>
        /// <param name="groupLimit">The number of groups to return from the specified group offset</param>
        /// <returns>the grouped result as a <see cref="ITopGroups{Object}"/> instance</returns>
        /// <exception cref="IOException">If any I/O related errors occur</exception>
        public virtual ITopGroups<TGroupValue> Search<TGroupValue>(IndexSearcher searcher, Filter filter, Query query, int groupOffset, int groupLimit)
        {
            if (groupFunction != null)
            {
                //LUCENENET Specific gaurd clause provides diagnostic info when TGroupValue is inappropriate type.
                if (!typeof(TGroupValue).IsSubclassOf(typeof(MutableValue)) && typeof(TGroupValue) != typeof(MutableValue) && typeof(TGroupValue) != typeof(object))
                {
                    throw IllegalStateException.Create("Generic closing type specified must be MutableValue, inherit from MutableValue, or be of type object since grouping by function.");
                }
                return (ITopGroups<TGroupValue>)GroupByFunction<MutableValue>(searcher, filter, query, groupOffset, groupLimit);
            }
            else if (groupField != null)
            {
                return GroupByField<TGroupValue>(searcher, filter, query, groupOffset, groupLimit);
            }
            else if (groupEndDocs != null)
            {
                return GroupByDocBlock<TGroupValue>(searcher, filter, query, groupOffset, groupLimit);
            }
            else
            {
                throw IllegalStateException.Create("Either groupField, groupFunction or groupEndDocs must be set."); // This can't happen...
            }
        }

        /// <summary>
        /// Executes a grouped search base on the field specified via the constructor. Both the first pass
        /// and second pass are executed on the specified searcher.
        /// </summary>
        /// <param name="searcher">The <see cref="IndexSearcher"/> instance to execute the grouped search on.</param>
        /// <param name="query">The query to execute with the grouping</param>
        /// <param name="groupOffset">The group offset</param>
        /// <param name="groupLimit">The number of groups to return from the specified group offset</param>
        /// <returns>the grouped result as a <see cref="ITopGroups{Object}"/> instance</returns>
        /// <exception cref="IOException">If any I/O related errors occur</exception>
        // LUCENENET additional method signature. Makes searching by field easier due to concrete return type
        public virtual ITopGroups<BytesRef> SearchByField(IndexSearcher searcher, Query query, int groupOffset, int groupLimit)
        {
            return GroupByField<BytesRef>(searcher, null, query, groupOffset, groupLimit);
        }


        /// <summary>
        /// Executes a grouped search base on the field specified via the constructor. Both the first pass
        /// and second pass are executed on the specified searcher.
        /// </summary>
        /// <param name="searcher">The <see cref="IndexSearcher"/> instance to execute the grouped search on.</param>
        /// <param name="filter">The filter to execute with the grouping</param>
        /// <param name="query">The query to execute with the grouping</param>
        /// <param name="groupOffset">The group offset</param>
        /// <param name="groupLimit">The number of groups to return from the specified group offset</param>
        /// <returns>the grouped result as a <see cref="ITopGroups{Object}"/> instance</returns>
        /// <exception cref="IOException">If any I/O related errors occur</exception>
        // LUCENENET additional method signature. Makes searching by field easier due to concrete return type
        public virtual ITopGroups<BytesRef> SearchByField(IndexSearcher searcher, Filter filter, Query query, int groupOffset, int groupLimit)
        {
            if (groupField is null)
            {
                throw IllegalStateException.Create("Must use constructor to set pass a non null value for groupField.");
            }

            if (groupFunction != null)
            {
                throw IllegalStateException.Create("The groupFunction must be null.");
            }
            return GroupByField<BytesRef>(searcher, filter, query, groupOffset, groupLimit);
        }

        /// <summary>
        /// Executes a grouped search base on the function specified by a <see cref="ValueSource"/> passed via the constructor.
        /// Both the first pass and second pass are executed on the specified searcher.
        /// </summary>
        /// <param name="searcher">The <see cref="IndexSearcher"/> instance to execute the grouped search on.</param>
        /// <param name="query">The query to execute with the grouping</param>
        /// <param name="groupOffset">The group offset</param>
        /// <param name="groupLimit">The number of groups to return from the specified group offset</param>
        /// <returns>the grouped result as a <see cref="ITopGroups{Object}"/> instance</returns>
        /// <exception cref="IOException">If any I/O related errors occur</exception>
        // LUCENENET additional method signature. Makes searching by function easier due to ability to specify type of MutableValue returned.
        public virtual ITopGroups<TMutableValue> SearchByFunction<TMutableValue>(IndexSearcher searcher, Query query, int groupOffset, int groupLimit)
            where TMutableValue : MutableValue
        {
            return GroupByFunction<TMutableValue>(searcher, null, query, groupOffset, groupLimit);
        }

        /// <summary>
        /// Executes a grouped search base on the function specified by a <see cref="ValueSource"/> passed via the constructor.
        /// Both the first pass and second pass are executed on the specified searcher.
        /// </summary>
        /// <param name="searcher">The <see cref="IndexSearcher"/> instance to execute the grouped search on.</param>
        /// <param name="filter">The filter to execute with the grouping</param>
        /// <param name="query">The query to execute with the grouping</param>
        /// <param name="groupOffset">The group offset</param>
        /// <param name="groupLimit">The number of groups to return from the specified group offset</param>
        /// <returns>the grouped result as a <see cref="ITopGroups{Object}"/> instance</returns>
        /// <exception cref="IOException">If any I/O related errors occur</exception>
        // LUCENENET additional method signature. Makes searching by function easier due to ability to specify type of MutableValue returned.
        public virtual ITopGroups<TMutableValue> SearchByFunction<TMutableValue>(IndexSearcher searcher, Filter filter, Query query, int groupOffset, int groupLimit)
            where TMutableValue : MutableValue
        {
            if (groupFunction is null)
            {
                throw IllegalStateException.Create("Either groupField, groupFunction or groupEndDocs must be set."); // This can't happen...
            }

            if (groupField != null)
            {
                throw new Exception("The valueSource must be null.");
            }
            return GroupByFunction<TMutableValue>(searcher, filter, query, groupOffset, groupLimit);
        }

        //LUCENENET: Method replaced by two methods GroupByField and GroupByFunction so that a generic type constraint
        //           can be added to the GroupByFunction case.
        //protected virtual ITopGroups GroupByFieldOrFunction(IndexSearcher searcher, Filter filter, Query query, int groupOffset, int groupLimit)
        //{
        //}


        //LUCENENET Specific. One of two methods that replace GroupByFieldOrFunction. Used support SearchByField.
        //          This method is essentually a Field specific version of the GroupByFieldOrFunction.
        protected virtual ITopGroups<TGroupValue> GroupByField<TGroupValue>(IndexSearcher searcher, Filter filter, Query query, int groupOffset, int groupLimit)
        {
            int topN = groupOffset + groupLimit;
            IAbstractFirstPassGroupingCollector<TGroupValue> firstPassCollector;
            IAbstractAllGroupsCollector<TGroupValue> allGroupsCollector;
            AbstractAllGroupHeadsCollector allGroupHeadsCollector;

            if (groupField is null)
            {
                throw IllegalStateException.Create("groupField must be set via the constructor.");
            }

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
                matchingGroups = (ICollection)allGroupsCollector.Groups;
            }
            else
            {
                matchingGroups = (ICollection)Collections.EmptyList<TGroupValue>();
            }
            if (allGroupHeads)
            {
                matchingGroupHeads = allGroupHeadsCollector.RetrieveGroupHeads(searcher.IndexReader.MaxDoc);
            }
            else
            {
                matchingGroupHeads = new Bits.MatchNoBits(searcher.IndexReader.MaxDoc);
            }

            IEnumerable<ISearchGroup<TGroupValue>> topSearchGroups = firstPassCollector.GetTopGroups(groupOffset, fillSortFields);
            if (topSearchGroups is null)
            {
                // LUCENENET specific - optimized empty array creation
                return new TopGroups<TGroupValue>(Arrays.Empty<SortField>(), Arrays.Empty<SortField>(), 0, 0, Arrays.Empty<GroupDocs<TGroupValue>>(), float.NaN);
            }

            int topNInsideGroup = groupDocsOffset + groupDocsLimit;
            IAbstractSecondPassGroupingCollector<TGroupValue> secondPassCollector;
            
            secondPassCollector = new TermSecondPassGroupingCollector(groupField, topSearchGroups as IEnumerable<ISearchGroup<BytesRef>>, 
                groupSort, sortWithinGroup, topNInsideGroup, includeScores, includeMaxScore, fillSortFields)
                as IAbstractSecondPassGroupingCollector<TGroupValue>;

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
                return new TopGroups<TGroupValue>(secondPassCollector.GetTopGroups(groupDocsOffset), matchingGroups.Count);
            }
            else
            {
                return secondPassCollector.GetTopGroups(groupDocsOffset);
            }
        }

        //LUCENENET Specific. One of two methods that replace GroupByFieldOrFunction. Used support
        //          SearchByFunction in a way that eliminates casting for the caller.
        //          This method is essentually a Function specific version of the GroupByFieldOrFunction.
        protected virtual ITopGroups<TMutableValue> GroupByFunction<TMutableValue>(IndexSearcher searcher, Filter filter, Query query, int groupOffset, int groupLimit)
            where TMutableValue: MutableValue
        {
            int topN = groupOffset + groupLimit;
            FunctionFirstPassGroupingCollector<TMutableValue> firstPassCollector;
            FunctionAllGroupsCollector<TMutableValue> allGroupsCollector;
            AbstractAllGroupHeadsCollector allGroupHeadsCollector;

            if (groupFunction is null)
            {
                throw IllegalStateException.Create("groupFunction must be set via the constructor by specifying a ValueSource.");
            }
           
            firstPassCollector = new FunctionFirstPassGroupingCollector<TMutableValue>(groupFunction, valueSourceContext, groupSort, topN);
            if (allGroups)
            {
                allGroupsCollector = new FunctionAllGroupsCollector<TMutableValue>(groupFunction, valueSourceContext);
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
                matchingGroups = (ICollection)allGroupsCollector.Groups;
            }
            else
            {
                matchingGroups = (ICollection)Collections.EmptyList<TMutableValue>();
            }
            if (allGroupHeads)
            {
                matchingGroupHeads = allGroupHeadsCollector.RetrieveGroupHeads(searcher.IndexReader.MaxDoc);
            }
            else
            {
                matchingGroupHeads = new Bits.MatchNoBits(searcher.IndexReader.MaxDoc);
            }

            IEnumerable<ISearchGroup<TMutableValue>> topSearchGroups = firstPassCollector.GetTopGroups(groupOffset, fillSortFields);
            if (topSearchGroups is null)
            {
                // LUCENENET specific - optimized empty array creation
                return new TopGroups<TMutableValue>(Arrays.Empty<SortField>(), Arrays.Empty<SortField>(), 0, 0, Arrays.Empty<GroupDocs<TMutableValue>>(), float.NaN);
            }

            int topNInsideGroup = groupDocsOffset + groupDocsLimit;
            IAbstractSecondPassGroupingCollector<TMutableValue> secondPassCollector;
           
            secondPassCollector = new FunctionSecondPassGroupingCollector<TMutableValue>(topSearchGroups as IEnumerable<ISearchGroup<TMutableValue>>,
                groupSort, sortWithinGroup, topNInsideGroup, includeScores, includeMaxScore, fillSortFields, groupFunction, valueSourceContext)
                as IAbstractSecondPassGroupingCollector<TMutableValue>;
            

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
                return new TopGroups<TMutableValue>(secondPassCollector.GetTopGroups(groupDocsOffset), matchingGroups.Count);
            }
            else
            {
                return secondPassCollector.GetTopGroups(groupDocsOffset);
            }
        }

        protected virtual ITopGroups<TGroupValue> GroupByDocBlock<TGroupValue>(IndexSearcher searcher, Filter filter, Query query, int groupOffset, int groupLimit)
        {
            int topN = groupOffset + groupLimit;
            BlockGroupingCollector c = new BlockGroupingCollector(groupSort, topN, includeScores, groupEndDocs);
            searcher.Search(query, filter, c);
            int topNInsideGroup = groupDocsOffset + groupDocsLimit;
            return c.GetTopGroups<TGroupValue>(sortWithinGroup, groupOffset, groupDocsOffset, topNInsideGroup, fillSortFields);
        }

        /// <summary>
        /// Enables caching for the second pass search. The cache will not grow over a specified limit in MB.
        /// The cache is filled during the first pass searched and then replayed during the second pass searched.
        /// If the cache grows beyond the specified limit, then the cache is purged and not used in the second pass search.
        /// </summary>
        /// <param name="maxCacheRAMMB">The maximum amount in MB the cache is allowed to hold</param>
        /// <param name="cacheScores">Whether to cache the scores</param>
        /// <returns><c>this</c></returns>
        public virtual GroupingSearch SetCachingInMB(double maxCacheRAMMB, bool cacheScores)
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
        public virtual GroupingSearch SetCaching(int maxDocsToCache, bool cacheScores)
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
        public virtual GroupingSearch DisableCaching()
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
        public virtual GroupingSearch SetGroupSort(Sort groupSort)
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
        public virtual GroupingSearch SetSortWithinGroup(Sort sortWithinGroup)
        {
            this.sortWithinGroup = sortWithinGroup;
            return this;
        }

        /// <summary>
        /// Specifies the offset for documents inside a group.
        /// </summary>
        /// <param name="groupDocsOffset">The offset for documents inside a</param>
        /// <returns><c>this</c></returns>
        public virtual GroupingSearch SetGroupDocsOffset(int groupDocsOffset)
        {
            this.groupDocsOffset = groupDocsOffset;
            return this;
        }

        /// <summary>
        /// Specifies the number of documents to return inside a group from the specified groupDocsOffset.
        /// </summary>
        /// <param name="groupDocsLimit">The number of documents to return inside a group</param>
        /// <returns><c>this</c></returns>
        public virtual GroupingSearch SetGroupDocsLimit(int groupDocsLimit)
        {
            this.groupDocsLimit = groupDocsLimit;
            return this;
        }

        /// <summary>
        /// Whether to also fill the sort fields per returned group and groups docs.
        /// </summary>
        /// <param name="fillSortFields">Whether to also fill the sort fields per returned group and groups docs</param>
        /// <returns><c>this</c></returns>
        public virtual GroupingSearch SetFillSortFields(bool fillSortFields)
        {
            this.fillSortFields = fillSortFields;
            return this;
        }

        /// <summary>
        /// Whether to include the scores per doc inside a group.
        /// </summary>
        /// <param name="includeScores">Whether to include the scores per doc inside a group</param>
        /// <returns><c>this</c></returns>
        public virtual GroupingSearch SetIncludeScores(bool includeScores)
        {
            this.includeScores = includeScores;
            return this;
        }

        /// <summary>
        /// Whether to include the score of the most relevant document per group.
        /// </summary>
        /// <param name="includeMaxScore">Whether to include the score of the most relevant document per group</param>
        /// <returns><c>this</c></returns>
        public virtual GroupingSearch SetIncludeMaxScore(bool includeMaxScore)
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
        public virtual GroupingSearch SetAllGroups(bool allGroups)
        {
            this.allGroups = allGroups;
            return this;
        }

        /// <summary>
        /// If <see cref="SetAllGroups(bool)"/> was set to <c>true</c> then all matching groups are returned, otherwise
        /// an empty collection is returned.
        /// </summary>
        /// <typeparam name="T">The group value type. This can be a <see cref="BytesRef"/> or a <see cref="MutableValue"/> instance. 
        /// If grouping by doc block this the group value is always <c>null</c>.</typeparam>
        /// <returns>all matching groups are returned, or an empty collection</returns>
        public virtual ICollection<T> GetAllMatchingGroups<T>()
        {
            return (ICollection<T>)matchingGroups;
        }

        /// <summary>
        /// If <see cref="SetAllGroups(bool)"/> was set to <c>true</c> then all matching groups are returned, otherwise
        /// an empty collection is returned.
        /// </summary>
        /// <returns>all matching groups are returned, or an empty collection</returns>
        /// <remarks>
        /// LUCENENET specific used to get the groups if the type is unknown or if the code expects
        /// any type, since <see cref="GetAllMatchingGroups{T}"/>
        /// will throw an exception if the return type is incorrect.
        /// </remarks>
        public virtual ICollection GetAllMatchingGroups()
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
        public virtual GroupingSearch SetAllGroupHeads(bool allGroupHeads)
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
        public virtual GroupingSearch SetInitialSize(int initialSize)
        {
            this.initialSize = initialSize;
            return this;
        }
    }
}
