using Lucene.Net.Index;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
    /// SecondPassGroupingCollector is the second of two passes
    /// necessary to collect grouped docs.  This pass gathers the
    /// top N documents per top group computed from the
    /// first pass. Concrete subclasses define what a group is and how it
    /// is internally collected.
    /// <para>
    /// See <a href="https://github.com/apache/lucene-solr/blob/releases/lucene-solr/4.8.0/lucene/grouping/src/java/org/apache/lucene/search/grouping/package.html">org.apache.lucene.search.grouping</a> for more
    /// details including a full code example.
    /// </para>
    /// @lucene.experimental
    /// </summary>
    /// <typeparam name="TGroupValue"></typeparam>
    public abstract class AbstractSecondPassGroupingCollector<TGroupValue> : IAbstractSecondPassGroupingCollector<TGroupValue>
    {
        protected readonly IDictionary<TGroupValue, AbstractSecondPassGroupingCollector.SearchGroupDocs<TGroupValue>> m_groupMap;
        private readonly int maxDocsPerGroup;
        protected AbstractSecondPassGroupingCollector.SearchGroupDocs<TGroupValue>[] m_groupDocs;
        private readonly IEnumerable<ISearchGroup<TGroupValue>> groups;
        private readonly Sort withinGroupSort;
        private readonly Sort groupSort;

        private int totalHitCount;
        private int totalGroupedHitCount;

        protected AbstractSecondPassGroupingCollector(IEnumerable<ISearchGroup<TGroupValue>> groups, Sort groupSort, Sort withinGroupSort,
                                                   int maxDocsPerGroup, bool getScores, bool getMaxScores, bool fillSortFields) // LUCENENET: CA1012: Abstract types should not have constructors (marked protected)
        {

            //System.out.println("SP init");
            if (!groups.Any()) // LUCENENET TODO: Change back to .Count if/when IEnumerable<T> is changed to ICollection<T> or IReadOnlyCollection<T>
            {
                throw new ArgumentException("no groups to collect (groups.Count is 0)");
            }

            this.groupSort = groupSort;
            this.withinGroupSort = withinGroupSort;
            this.groups = groups;
            this.maxDocsPerGroup = maxDocsPerGroup;
            m_groupMap = new JCG.Dictionary<TGroupValue, AbstractSecondPassGroupingCollector.SearchGroupDocs<TGroupValue>>(groups.Count());

            foreach (SearchGroup<TGroupValue> group in groups)
            {
                //System.out.println("  prep group=" + (group.groupValue is null ? "null" : group.groupValue.utf8ToString()));
                ITopDocsCollector collector;
                if (withinGroupSort is null)
                {
                    // Sort by score
                    collector = TopScoreDocCollector.Create(maxDocsPerGroup, true);
                }
                else
                {
                    // Sort by fields
                    collector = TopFieldCollector.Create(withinGroupSort, maxDocsPerGroup, fillSortFields, getScores, getMaxScores, true);
                }
                m_groupMap[group.GroupValue] = new AbstractSecondPassGroupingCollector.SearchGroupDocs<TGroupValue>(group.GroupValue, collector);
            }
        }

        public virtual void SetScorer(Scorer scorer)
        {
            foreach (AbstractSecondPassGroupingCollector.SearchGroupDocs<TGroupValue> group in m_groupMap.Values)
            {
                group.Collector.SetScorer(scorer);
            }
        }

        public virtual void Collect(int doc)
        {
            totalHitCount++;
            AbstractSecondPassGroupingCollector.SearchGroupDocs<TGroupValue> group = RetrieveGroup(doc);
            if (group != null)
            {
                totalGroupedHitCount++;
                group.Collector.Collect(doc);
            }
        }

        /// <summary>
        /// Returns the group the specified doc belongs to or <c>null</c> if no group could be retrieved.
        /// </summary>
        /// <param name="doc">The specified doc</param>
        /// <returns>the group the specified doc belongs to or <c>null</c> if no group could be retrieved</returns>
        /// <exception cref="IOException">If an I/O related error occurred</exception>
        protected abstract AbstractSecondPassGroupingCollector.SearchGroupDocs<TGroupValue> RetrieveGroup(int doc);

        public virtual void SetNextReader(AtomicReaderContext context)
        {
            //System.out.println("SP.setNextReader");
            foreach (AbstractSecondPassGroupingCollector.SearchGroupDocs<TGroupValue> group in m_groupMap.Values)
            {
                group.Collector.SetNextReader(context);
            }
        }

        public virtual bool AcceptsDocsOutOfOrder => false;

        public virtual ITopGroups<TGroupValue> GetTopGroups(int withinGroupOffset)
        {
            GroupDocs<TGroupValue>[] groupDocsResult = new GroupDocs<TGroupValue>[groups.Count()];

            int groupIDX = 0;
            float maxScore = float.Epsilon; // LUCENENET: Epsilon in .NET is the same as MIN_VALUE in Java
            foreach (var group in groups)
            {
                AbstractSecondPassGroupingCollector.SearchGroupDocs<TGroupValue> groupDocs = m_groupMap[group.GroupValue];
                TopDocs topDocs = groupDocs.Collector.GetTopDocs(withinGroupOffset, maxDocsPerGroup);
                groupDocsResult[groupIDX++] = new GroupDocs<TGroupValue>(float.NaN,
                                                                              topDocs.MaxScore,
                                                                              topDocs.TotalHits,
                                                                              topDocs.ScoreDocs,
                                                                              groupDocs.GroupValue,
                                                                              group.SortValues);
                maxScore = Math.Max(maxScore, topDocs.MaxScore);
            }

            return new TopGroups<TGroupValue>(groupSort.GetSort(),
                                                   withinGroupSort?.GetSort(),
                                                   totalHitCount, totalGroupedHitCount, groupDocsResult,
                                                   maxScore);
        }


        
    }

    /// <summary>
    /// LUCENENET specific class used to simulate the syntax used 
    /// to access nested classes of <see cref="AbstractAllGroupHeadsCollector{GH}"/>
    /// without referencing the generic closing type.
    /// </summary>
    public static class AbstractSecondPassGroupingCollector // LUCENENET specific: CA1052 Static holder types should be Static or NotInheritable
    {
        // TODO: merge with SearchGroup or not?
        // ad: don't need to build a new hashmap
        // disad: blows up the size of SearchGroup if we need many of them, and couples implementations
        public class SearchGroupDocs<TGroupValue>
        {
            public TGroupValue GroupValue => groupValue;
            private readonly TGroupValue groupValue;

            public ITopDocsCollector Collector => collector;
            private readonly ITopDocsCollector collector;
            public SearchGroupDocs(TGroupValue groupValue, ITopDocsCollector collector)
            {
                this.groupValue = groupValue;
                this.collector = collector;
            }
        }
    }

    /// <summary>
    /// LUCENENET specific interface used to apply covariance to TGroupValue
    /// to simulate Java's wildcard generics.
    /// </summary>
    /// <typeparam name="TGroupValue"></typeparam>
    public interface IAbstractSecondPassGroupingCollector<out TGroupValue> : ICollector
    {
        ITopGroups<TGroupValue> GetTopGroups(int withinGroupOffset);
    }
}
