using Lucene.Net.Queries.Function;
using Lucene.Net.Search.Grouping.Function;
using Lucene.Net.Search.Grouping.Terms;
using Lucene.Net.Support;
using Lucene.Net.Util;
using Lucene.Net.Util.Mutable;
using System;
using System.Collections;
using System.Collections.Generic;
using JCG = J2N.Collections.Generic;

// ReSharper disable VirtualMemberNeverOverridden.Global - justification: this is a public API

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
    /// <para />
    /// LUCENENET Note: This class has been significantly changed from Lucene.
    /// The previous implementation combined field, function, and doc block grouping
    /// into one large class and required the use of Java's generic type erasure and
    /// wildcard generics to handle the different types of grouping. This implementation
    /// splits the different types of grouping into separate classes and uses a common
    /// base class to handle the common functionality.
    /// <para />
    /// This class contains three static factory methods to create instances of the
    /// different types of grouping:
    /// <list type="bullet">
    ///     <item>
    ///         <description>
    ///             <see cref="ByField(string)"/> - Constructs a <see cref="FieldGroupingSearch" /> instance
    ///             that groups documents by index terms using the <see cref="FieldCache"/>.
    ///             The group field can only have one token per document. This means that the field must not be analysed.
    ///         </description>
    ///     </item>
    ///     <item>
    ///         <description>
    ///             <see cref="ByFunction{TMutableValue}(ValueSource, IDictionary)"/> - Constructs a <see cref="FunctionGroupingSearch{TMutableValue}"/> instance
    ///             that groups documents by function using a <see cref="ValueSource"/> instance.
    ///         </description>
    ///     </item>
    ///     <item>
    ///         <description>
    ///             <see cref="ByDocBlock(Filter)"/> - Constructs a <see cref="DocBlockGroupingSearch"/> instance
    ///             that groups documents by doc block. This method can only be used when documents belonging in a group are indexed in one block.
    ///         </description>
    ///     </item>
    /// </list>
    /// These types each return a type that behaves like the original GroupingSearch class,
    /// but specific to the type of grouping being performed.
    /// <para />
    /// It is not required to use these methods; you can also create instances of the
    /// specific grouping classes directly which will be closer to the original Lucene
    /// usage.
    /// <para />
    /// @lucene.experimental
    /// </summary>
    /// <seealso cref="FieldGroupingSearch"/>
    /// <seealso cref="FunctionGroupingSearch{TMutableValue}"/>
    /// <seealso cref="DocBlockGroupingSearch"/>
    public static class GroupingSearch
    {
        /// <summary>
        /// Constructs a <see cref="FieldGroupingSearch"/> instance that groups documents by index terms using the <see cref="FieldCache"/>.
        /// The group field can only have one token per document. This means that the field must not be analysed.
        /// </summary>
        /// <param name="groupField">The name of the field to group by.</param>
        /// <returns>A <see cref="FieldGroupingSearch"/> instance.</returns>
        public static FieldGroupingSearch ByField(string groupField)
        {
            return new FieldGroupingSearch(groupField);
        }

        /// <summary>
        /// Constructs a <see cref="FunctionGroupingSearch{TMutableValue}"/> instance that groups documents by function using a <see cref="ValueSource"/> instance.
        /// </summary>
        /// <param name="groupFunction">The function to group by specified as <see cref="ValueSource"/></param>
        /// <param name="valueSourceContext">The context of the specified groupFunction</param>
        /// <typeparam name="TMutableValue">The type of the mutable value</typeparam>
        /// <returns>A <see cref="FunctionGroupingSearch{TMutableValue}"/> instance.</returns>
        public static FunctionGroupingSearch<TMutableValue> ByFunction<TMutableValue>(ValueSource groupFunction, IDictionary valueSourceContext)
            where TMutableValue : MutableValue
        {
            return new FunctionGroupingSearch<TMutableValue>(groupFunction, valueSourceContext);
        }

        /// <summary>
        /// Constructs a <see cref="DocBlockGroupingSearch"/> instance that groups documents by doc block.
        /// This method can only be used when documents belonging in a group are indexed in one block.
        /// </summary>
        /// <param name="groupEndDocs">The filter that marks the last document in all doc blocks</param>
        /// <returns>A <see cref="DocBlockGroupingSearch"/> instance.</returns>
        public static DocBlockGroupingSearch ByDocBlock(Filter groupEndDocs)
        {
            return new DocBlockGroupingSearch(groupEndDocs);
        }
    }

    /// <summary>
    /// A grouping search that groups documents by index terms using the <see cref="FieldCache"/>.
    /// The group field can only have one token per document. This means that the field must not be analyzed.
    /// </summary>
    /// <seealso cref="GroupingSearch.ByField(string)"/>
    /// <remarks>
    /// LUCENENET specific.
    /// </remarks>
    // ReSharper disable once ClassWithVirtualMembersNeverInherited.Global - justification: this is a public API
    public class FieldGroupingSearch : TwoPassGroupingSearch<BytesRef, FieldGroupingSearch>
    {
        private readonly string groupField;
        private int initialSize = 128;

        /// <summary>
        /// Constructs a <see cref="FieldGroupingSearch"/> instance that groups documents by index terms using the <see cref="FieldCache"/>.
        /// </summary>
        /// <param name="groupField">The name of the field to group by.</param>
        public FieldGroupingSearch(string groupField)
        {
            this.groupField = groupField;
        }

        /// <inheritdoc/>
        protected override AbstractFirstPassGroupingCollector<BytesRef> CreateFirstPassCollector(Sort groupSort, int topN)
        {
            if (groupField is null)
            {
                throw IllegalStateException.Create("groupField must be set via the constructor.");
            }

            return new TermFirstPassGroupingCollector(groupField, groupSort, topN);
        }

        /// <inheritdoc/>
        protected override AbstractAllGroupsCollector<BytesRef> CreateAllGroupsCollector()
        {
            if (groupField is null)
            {
                throw IllegalStateException.Create("groupField must be set via the constructor.");
            }

            return new TermAllGroupsCollector(groupField, initialSize);
        }

        /// <inheritdoc/>
        protected override AbstractAllGroupHeadsCollector CreateAllGroupHeadsCollector(Sort sortWithinGroup)
        {
            if (groupField is null)
            {
                throw IllegalStateException.Create("groupField must be set via the constructor.");
            }

            return TermAllGroupHeadsCollector.Create(groupField, sortWithinGroup, initialSize);
        }

        /// <inheritdoc/>
        protected override AbstractSecondPassGroupingCollector<BytesRef> CreateSecondPassCollector(
            ICollection<SearchGroup<BytesRef>> topSearchGroups, Sort groupSort, Sort sortWithinGroup,
            int maxDocsPerGroup, bool getScores, bool getMaxScores, bool fillSortFields)
        {
            if (groupField is null)
            {
                throw IllegalStateException.Create("groupField must be set via the constructor.");
            }

            return new TermSecondPassGroupingCollector(groupField, topSearchGroups,
                groupSort, sortWithinGroup, maxDocsPerGroup, getScores, getMaxScores, fillSortFields);
        }

        /// <summary>
        /// Sets the initial size of some internal used data structures.
        /// This prevents growing data structures many times. This can improve the performance of the grouping at the cost of
        /// more initial RAM.
        /// <para>
        /// The <see cref="TwoPassGroupingSearch{T, TSelf}.SetAllGroups(bool)"/> and
        /// <see cref="TwoPassGroupingSearch{T, TSelf}.SetAllGroupHeads(bool)"/> features use this option.
        /// Defaults to 128.
        /// </para>
        /// </summary>
        /// <param name="initialSize">The initial size of some internal used data structures</param>
        /// <returns><c>this</c></returns>
        // ReSharper disable once ParameterHidesMember - justification: typical upstream Java style
        public virtual FieldGroupingSearch SetInitialSize(int initialSize)
        {
            this.initialSize = initialSize;
            return this;
        }
    }

    /// <summary>
    /// A grouping search that groups documents by function using a <see cref="ValueSource"/> instance.
    /// </summary>
    /// <typeparam name="T">The type of the mutable value</typeparam>
    /// <remarks>
    /// LUCENENET specific.
    /// </remarks>
    /// <seealso cref="GroupingSearch.ByFunction{TMutableValue}(ValueSource, IDictionary)"/>
    public class FunctionGroupingSearch<T> : TwoPassGroupingSearch<T, FunctionGroupingSearch<T>>
        where T : MutableValue
    {
        private readonly ValueSource groupFunction;
        private readonly IDictionary /* Map<?, ?> */ valueSourceContext;

        /// <summary>
        /// Constructs a <see cref="FunctionGroupingSearch{T}"/> instance that groups documents by function using a <see cref="ValueSource"/> instance.
        /// </summary>
        /// <param name="groupFunction">The function to group by specified as <see cref="ValueSource"/></param>
        /// <param name="valueSourceContext">The context of the specified groupFunction</param>
        public FunctionGroupingSearch(ValueSource groupFunction, IDictionary /* Map<?, ?> */ valueSourceContext)
        {
            this.groupFunction = groupFunction;
            this.valueSourceContext = valueSourceContext;
        }

        /// <inheritdoc/>
        protected override AbstractFirstPassGroupingCollector<T> CreateFirstPassCollector(Sort groupSort, int topN)
        {
            if (groupFunction is null)
            {
                throw IllegalStateException.Create("groupFunction must be set via the constructor by specifying a ValueSource.");
            }

            return new FunctionFirstPassGroupingCollector<T>(groupFunction, valueSourceContext, groupSort, topN);
        }

        /// <inheritdoc/>
        protected override AbstractAllGroupsCollector<T> CreateAllGroupsCollector()
        {
            if (groupFunction is null)
            {
                throw IllegalStateException.Create("groupFunction must be set via the constructor by specifying a ValueSource.");
            }

            return new FunctionAllGroupsCollector<T>(groupFunction, valueSourceContext);
        }

        /// <inheritdoc/>
        protected override AbstractAllGroupHeadsCollector CreateAllGroupHeadsCollector(Sort sortWithinGroup)
        {
            if (groupFunction is null)
            {
                throw IllegalStateException.Create("groupFunction must be set via the constructor by specifying a ValueSource.");
            }

            return new FunctionAllGroupHeadsCollector(groupFunction, valueSourceContext, sortWithinGroup);
        }

        /// <inheritdoc/>
        protected override AbstractSecondPassGroupingCollector<T> CreateSecondPassCollector(
            ICollection<SearchGroup<T>> topSearchGroups, Sort groupSort, Sort sortWithinGroup,
            int maxDocsPerGroup, bool getScores, bool getMaxScores, bool fillSortFields)
        {
            if (groupFunction is null)
            {
                throw IllegalStateException.Create("groupFunction must be set via the constructor by specifying a ValueSource.");
            }

            return new FunctionSecondPassGroupingCollector<T>(topSearchGroups,
                groupSort, sortWithinGroup, maxDocsPerGroup, getScores, getMaxScores, fillSortFields, groupFunction, valueSourceContext);
        }
    }

#nullable enable
    /// <summary>
    /// A grouping search that groups documents by doc block.
    /// This class can only be used when documents belonging in a group are indexed in one block.
    /// </summary>
    /// <remarks>
    /// LUCENENET specific.
    /// </remarks>
    public class DocBlockGroupingSearch : GroupingSearch<object?, DocBlockGroupingSearch>
    {
        private readonly Filter groupEndDocs;

        /// <summary>
        /// Constructs a <see cref="DocBlockGroupingSearch"/> instance that groups documents by doc block.
        /// This class can only be used when documents belonging in a group are indexed in one block.
        /// </summary>
        /// <param name="groupEndDocs">The filter that marks the last document in all doc blocks</param>
        public DocBlockGroupingSearch(Filter groupEndDocs)
        {
            this.groupEndDocs = groupEndDocs;
        }

        /// <inheritdoc cref="GroupingSearch{T,TSelf}.Search(IndexSearcher,Filter,Query,int,int)"/>
        public override TopGroups<object?> Search(IndexSearcher searcher, Filter filter, Query query, int groupOffset, int groupLimit)
        {
            int topN = groupOffset + groupLimit;
            BlockGroupingCollector c = new BlockGroupingCollector(GroupSort, topN, IncludeScores, groupEndDocs);
            searcher.Search(query, filter, c);
            int topNInsideGroup = GroupDocsOffset + GroupDocsLimit;
            return c.GetTopGroups<object?>(SortWithinGroup, groupOffset, GroupDocsOffset, topNInsideGroup, FillSortFields);
        }
#nullable restore
    }

    /// <summary>
    /// Abstract base class for two-pass grouping search implementations. Two-pass grouping search first collects the
    /// top N groups in the first pass, and then collects the top documents within those top groups in the second pass.
    /// </summary>
    /// <typeparam name="T">The type of the group value</typeparam>
    /// <typeparam name="TSelf">The type of the concrete grouping search implementation, used for fluent method chaining</typeparam>
    /// <remarks>
    /// LUCENENET specific.
    /// <para />
    /// Notes for implementers:
    /// <para />
    /// This abstract class represents a template method pattern for implementing two-pass grouping search.
    /// To create a custom implementation, you must implement the factory methods provided. See the summaries for each
    /// one, and refer to <see cref="FieldGroupingSearch"/> and <see cref="FunctionGroupingSearch{T}"/> as reference
    /// implementations.
    /// </remarks>
    /// <seealso cref="GroupingSearch"/>
    public abstract class TwoPassGroupingSearch<T, TSelf> : GroupingSearch<T, TSelf>
        where TSelf : TwoPassGroupingSearch<T, TSelf>
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

        #region Abstract factory methods

        /// <summary>
        /// Creates the first-pass grouping collector used to determine the top groups.
        /// </summary>
        /// <param name="groupSort">The sort to use for ordering groups.</param>
        /// <param name="topN">The maximum number of top groups to collect.</param>
        /// <returns>A new <see cref="AbstractFirstPassGroupingCollector{T}"/> instance.</returns>
        protected abstract AbstractFirstPassGroupingCollector<T> CreateFirstPassCollector(Sort groupSort, int topN);

        /// <summary>
        /// Creates the collector used to determine all groups matching the query.
        /// </summary>
        /// <returns>A new <see cref="AbstractAllGroupsCollector{T}"/> instance.</returns>
        protected abstract AbstractAllGroupsCollector<T> CreateAllGroupsCollector();

        /// <summary>
        /// Creates the collector used to determine all group head documents (most relevant document per group).
        /// </summary>
        /// <param name="sortWithinGroup">The sort to use within each group.</param>
        /// <returns>A new <see cref="AbstractAllGroupHeadsCollector"/> instance.</returns>
        protected abstract AbstractAllGroupHeadsCollector CreateAllGroupHeadsCollector(Sort sortWithinGroup);

        /// <summary>
        /// Creates the second-pass grouping collector used to collect documents within the top groups.
        /// </summary>
        /// <param name="topSearchGroups">The top groups from the first pass.</param>
        /// <param name="groupSort">The sort to use for ordering groups.</param>
        /// <param name="sortWithinGroup">The sort to use within each group.</param>
        /// <param name="maxDocsPerGroup">The maximum number of documents to collect per group.</param>
        /// <param name="getScores">Whether to include scores for each document.</param>
        /// <param name="getMaxScores">Whether to include the maximum score per group.</param>
        /// <param name="fillSortFields">Whether to populate sort field values.</param>
        /// <returns>A new <see cref="AbstractSecondPassGroupingCollector{T}"/> instance.</returns>
        protected abstract AbstractSecondPassGroupingCollector<T> CreateSecondPassCollector(
            ICollection<SearchGroup<T>> topSearchGroups, Sort groupSort, Sort sortWithinGroup,
            int maxDocsPerGroup, bool getScores, bool getMaxScores, bool fillSortFields);

        #endregion

        /// <inheritdoc cref="GroupingSearch{T,TSelf}.Search(IndexSearcher,Filter,Query,int,int)"/>
        public override TopGroups<T> Search(IndexSearcher searcher, Filter filter, Query query, int groupOffset, int groupLimit)
        {
            int topN = groupOffset + groupLimit;

            AbstractFirstPassGroupingCollector<T> firstPassCollector = CreateFirstPassCollector(GroupSort, topN);

            AbstractAllGroupsCollector<T> allGroupsCollector = AllGroups ? CreateAllGroupsCollector() : null;

            AbstractAllGroupHeadsCollector allGroupHeadsCollector = AllGroupHeads ? CreateAllGroupHeadsCollector(SortWithinGroup) : null;

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
                cachedCollector = MaxCacheRAMMB != null
                    ? CachingCollector.Create(firstRound, CacheScores, MaxCacheRAMMB.Value)
                    : CachingCollector.Create(firstRound, CacheScores, MaxDocsToCache!.Value);
                searcher.Search(query, filter, cachedCollector);
            }
            else
            {
                searcher.Search(query, filter, firstRound);
            }

            MatchingGroups = AllGroups
                ? allGroupsCollector!.Groups    // [!]: initialized above when AllGroups is true
                : Collections.EmptyList<T>();

            MatchingGroupHeads = AllGroupHeads
                ? allGroupHeadsCollector!.RetrieveGroupHeads(searcher.IndexReader.MaxDoc) // [!]: initialized above when AllGroupHeads is true
                : new Bits.MatchNoBits(searcher.IndexReader.MaxDoc);

            ICollection<SearchGroup<T>> topSearchGroups = firstPassCollector.GetTopGroups(groupOffset, FillSortFields);
            if (topSearchGroups is null)
            {
                // LUCENENET specific - optimized empty array creation
                return new TopGroups<T>(Array.Empty<SortField>(), Array.Empty<SortField>(), 0, 0, Array.Empty<GroupDocs<T>>(), float.NaN);
            }

            int topNInsideGroup = GroupDocsOffset + GroupDocsLimit;

            AbstractSecondPassGroupingCollector<T> secondPassCollector = CreateSecondPassCollector(topSearchGroups,
                GroupSort, SortWithinGroup, topNInsideGroup, IncludeScores, IncludeMaxScore, FillSortFields);

            if (cachedCollector != null && cachedCollector.IsCached)
            {
                cachedCollector.Replay(secondPassCollector);
            }
            else
            {
                searcher.Search(query, filter, secondPassCollector);
            }

            return AllGroups
                ? new TopGroups<T>(secondPassCollector.GetTopGroups(GroupDocsOffset), MatchingGroups.Count)
                : secondPassCollector.GetTopGroups(GroupDocsOffset);
        }

        /// <summary>
        /// Enables caching for the second pass search. The cache will not grow over a specified limit in MB.
        /// The cache is filled during the first pass searched and then replayed during the second pass searched.
        /// If the cache grows beyond the specified limit, then the cache is purged and not used in the second pass search.
        /// </summary>
        /// <param name="maxCacheRAMMB">The maximum amount in MB the cache is allowed to hold</param>
        /// <param name="cacheScores">Whether to cache the scores</param>
        /// <returns><c>this</c></returns>
        public virtual TSelf SetCachingInMB(double maxCacheRAMMB, bool cacheScores)
        {
            this.MaxCacheRAMMB = maxCacheRAMMB;
            this.MaxDocsToCache = null;
            this.CacheScores = cacheScores;
            return (TSelf)this;
        }

        /// <summary>
        /// Enables caching for the second pass search. The cache will not contain more than the maximum specified documents.
        /// The cache is filled during the first pass searched and then replayed during the second pass searched.
        /// If the cache grows beyond the specified limit, then the cache is purged and not used in the second pass search.
        /// </summary>
        /// <param name="maxDocsToCache">The maximum number of documents the cache is allowed to hold</param>
        /// <param name="cacheScores">Whether to cache the scores</param>
        /// <returns><c>this</c></returns>
        public virtual TSelf SetCaching(int maxDocsToCache, bool cacheScores)
        {
            this.MaxDocsToCache = maxDocsToCache;
            this.MaxCacheRAMMB = null;
            this.CacheScores = cacheScores;
            return (TSelf)this;
        }

        /// <summary>
        /// Disables any enabled cache.
        /// </summary>
        /// <returns><c>this</c></returns>
        public virtual TSelf DisableCaching()
        {
            this.MaxCacheRAMMB = null;
            this.MaxDocsToCache = null;
            return (TSelf)this;
        }

        /// <summary>
        /// Whether to include the score of the most relevant document per group.
        /// </summary>
        /// <param name="includeMaxScore">Whether to include the score of the most relevant document per group</param>
        /// <returns><c>this</c></returns>
        public virtual TSelf SetIncludeMaxScore(bool includeMaxScore)
        {
            this.IncludeMaxScore = includeMaxScore;
            return (TSelf)this;
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
        public virtual TSelf SetAllGroups(bool allGroups)
        {
            this.AllGroups = allGroups;
            return (TSelf)this;
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
        public virtual TSelf SetAllGroupHeads(bool allGroupHeads)
        {
            this.AllGroupHeads = allGroupHeads;
            return (TSelf)this;
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

    /// <summary>
    /// Abstract base class for grouping search implementations.
    /// </summary>
    /// <typeparam name="T">The type of the group value</typeparam>
    /// <typeparam name="TSelf">The type of the concrete grouping search implementation, used for fluent method chaining</typeparam>
    /// <remarks>
    /// LUCENENET specific.
    /// <para />
    /// Notes for implementers:
    /// <para />
    /// The <typeparamref name="TSelf"/> type parameter creates a "Curiously-Recurring Template Pattern" (CRTP) that
    /// allows the base class to return the type of the concrete implementation in its fluent API methods.
    /// This pattern is used to enable method chaining while maintaining the specific type of the concrete
    /// implementation, which is not possible with a non-generic base class or with a base class that uses a common
    /// interface for method chaining. By using this pattern, users of the grouping search classes can call
    /// configuration methods in a fluent manner and still have access to the specific methods of the concrete
    /// implementation without needing to cast back to the concrete type.
    /// </remarks>
    /// <seealso cref="GroupingSearch"/>
    public abstract class GroupingSearch<T, TSelf> : IGroupingSearch
        where TSelf : GroupingSearch<T, TSelf>
    {
        // LUCENENET: Converted to protected properties
        protected Sort GroupSort { get; private set; } = Sort.RELEVANCE;
        protected Sort SortWithinGroup { get; private set; }

        protected int GroupDocsOffset { get; private set; }
        protected int GroupDocsLimit { get; private set; } = 1;
        protected bool FillSortFields { get; private set; }
        protected bool IncludeScores { get; private set; } = true;

        /// <summary>
        /// Executes a grouped search. Both the first pass and second pass are executed on the specified searcher.
        /// </summary>
        /// <param name="searcher">The <see cref="IndexSearcher"/> instance to execute the grouped search on.</param>
        /// <param name="query">The query to execute with the grouping</param>
        /// <param name="groupOffset">The group offset</param>
        /// <param name="groupLimit">The number of groups to return from the specified group offset</param>
        /// <returns>the grouped result as a <see cref="TopGroups"/> instance</returns>
        public TopGroups<T> Search(IndexSearcher searcher, Query query, int groupOffset, int groupLimit)
        {
            return Search(searcher, null, query, groupOffset, groupLimit);
        }

        /// <summary>
        /// Executes a grouped search. Both the first pass and second pass are executed on the specified searcher.
        /// </summary>
        /// <param name="searcher">The <see cref="IndexSearcher"/> instance to execute the grouped search on.</param>
        /// <param name="filter">The filter to execute with the grouping</param>
        /// <param name="query">The query to execute with the grouping</param>
        /// <param name="groupOffset">The group offset</param>
        /// <param name="groupLimit">The number of groups to return from the specified group offset</param>
        /// <returns>the grouped result as a <see cref="TopGroups"/> instance</returns>
        public abstract TopGroups<T> Search(IndexSearcher searcher, Filter filter, Query query, int groupOffset, int groupLimit);

        /// <summary>
        /// Specifies how groups are sorted.
        /// Defaults to <see cref="Sort.RELEVANCE"/>.
        /// </summary>
        /// <param name="groupSort">The sort for the groups.</param>
        /// <returns><c>this</c></returns>
        public virtual TSelf SetGroupSort(Sort groupSort)
        {
            this.GroupSort = groupSort;
            return (TSelf)this;
        }

        /// <summary>
        /// Specified how documents inside a group are sorted.
        /// Defaults to <see cref="Sort.RELEVANCE"/>.
        /// </summary>
        /// <param name="sortWithinGroup">The sort for documents inside a group</param>
        /// <returns><c>this</c></returns>
        public virtual TSelf SetSortWithinGroup(Sort sortWithinGroup)
        {
            this.SortWithinGroup = sortWithinGroup;
            return (TSelf)this;
        }

        /// <summary>
        /// Specifies the offset for documents inside a group.
        /// </summary>
        /// <param name="groupDocsOffset">The offset for documents inside a</param>
        /// <returns><c>this</c></returns>
        public virtual TSelf SetGroupDocsOffset(int groupDocsOffset)
        {
            this.GroupDocsOffset = groupDocsOffset;
            return (TSelf)this;
        }

        /// <summary>
        /// Specifies the number of documents to return inside a group from the specified groupDocsOffset.
        /// </summary>
        /// <param name="groupDocsLimit">The number of documents to return inside a group</param>
        /// <returns><c>this</c></returns>
        public virtual TSelf SetGroupDocsLimit(int groupDocsLimit)
        {
            this.GroupDocsLimit = groupDocsLimit;
            return (TSelf)this;
        }

        /// <summary>
        /// Whether to also fill the sort fields per returned group and groups docs.
        /// </summary>
        /// <param name="fillSortFields">Whether to also fill the sort fields per returned group and groups docs</param>
        /// <returns><c>this</c></returns>
        public virtual TSelf SetFillSortFields(bool fillSortFields)
        {
            this.FillSortFields = fillSortFields;
            return (TSelf)this;
        }

        /// <summary>
        /// Whether to include the scores per doc inside a group.
        /// </summary>
        /// <param name="includeScores">Whether to include the scores per doc inside a group</param>
        /// <returns><c>this</c></returns>
        public virtual TSelf SetIncludeScores(bool includeScores)
        {
            this.IncludeScores = includeScores;
            return (TSelf)this;
        }

        #region Explicit interface implementations

        /// <inheritdoc cref="IGroupingSearch.Search(IndexSearcher, Query, int, int)"/>
        ITopGroups IGroupingSearch.Search(IndexSearcher searcher, Query query, int groupOffset, int groupLimit)
            => Search(searcher, query, groupOffset, groupLimit);

        /// <inheritdoc cref="IGroupingSearch.Search(IndexSearcher, Filter, Query, int, int)"/>
        ITopGroups IGroupingSearch.Search(IndexSearcher searcher, Filter filter, Query query, int groupOffset, int groupLimit)
            => Search(searcher, filter, query, groupOffset, groupLimit);

        #endregion
    }

    /// <summary>
    /// An interface for non-generic access to <see cref="GroupingSearch{T,TSelf}"/>.
    /// </summary>
    /// <remarks>
    /// LUCENENET specific. This was introduced to avoid type-erasure differences from the upstream Java code.
    /// </remarks>
    public interface IGroupingSearch
    {
        /// <summary>
        /// Executes a grouped search. Both the first pass and second pass are executed on the specified searcher.
        /// </summary>
        /// <param name="searcher">The <see cref="IndexSearcher"/> instance to execute the grouped search on.</param>
        /// <param name="query">The query to execute with the grouping</param>
        /// <param name="groupOffset">The group offset</param>
        /// <param name="groupLimit">The number of groups to return from the specified group offset</param>
        /// <returns>the grouped result as an <see cref="ITopGroups"/> instance</returns>
        ITopGroups Search(IndexSearcher searcher, Query query, int groupOffset, int groupLimit);

        /// <summary>
        /// Executes a grouped search. Both the first pass and second pass are executed on the specified searcher.
        /// </summary>
        /// <param name="searcher">The <see cref="IndexSearcher"/> instance to execute the grouped search on.</param>
        /// <param name="filter">The filter to execute with the grouping</param>
        /// <param name="query">The query to execute with the grouping</param>
        /// <param name="groupOffset">The group offset</param>
        /// <param name="groupLimit">The number of groups to return from the specified group offset</param>
        /// <returns>the grouped result as an <see cref="ITopGroups"/> instance</returns>
        ITopGroups Search(IndexSearcher searcher, Filter filter, Query query, int groupOffset, int groupLimit);
    }
}
