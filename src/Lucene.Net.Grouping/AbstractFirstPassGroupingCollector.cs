using Lucene.Net.Diagnostics;
using Lucene.Net.Index;
using Lucene.Net.Support.Threading;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
    /// FirstPassGroupingCollector is the first of two passes necessary
    /// to collect grouped hits.  This pass gathers the top N sorted
    /// groups. Concrete subclasses define what a group is and how it
    /// is internally collected.
    /// 
    /// <para>
    /// See <a href="https://github.com/apache/lucene-solr/blob/releases/lucene-solr/4.8.0/lucene/grouping/src/java/org/apache/lucene/search/grouping/package.html">org.apache.lucene.search.grouping</a> for more
    /// details including a full code example.
    /// </para>
    /// @lucene.experimental
    /// </summary>
    /// <typeparam name="TGroupValue"></typeparam>
    public abstract class AbstractFirstPassGroupingCollector<TGroupValue> : IAbstractFirstPassGroupingCollector<TGroupValue>
    {
        private readonly Sort groupSort;
        private readonly FieldComparer[] comparers;
        private readonly int[] reversed;
        private readonly int topNGroups;
        private readonly IDictionary<TGroupValue, CollectedSearchGroup<TGroupValue>> groupMap;
        private readonly int compIDXEnd;

        // Set once we reach topNGroups unique groups:
        // @lucene.internal
        protected JCG.SortedSet<CollectedSearchGroup<TGroupValue>> m_orderedGroups;
        private int docBase;
        private int spareSlot;

        /// <summary>
        /// Create the first pass collector.
        /// </summary>
        /// <param name="groupSort">
        /// The <see cref="Sort"/> used to sort the
        /// groups.  The top sorted document within each group
        /// according to groupSort, determines how that group
        /// sorts against other groups.  This must be non-null,
        /// ie, if you want to groupSort by relevance use
        /// Sort.RELEVANCE.
        /// </param>
        /// <param name="topNGroups">How many top groups to keep.</param>
        /// <exception cref="IOException">If I/O related errors occur</exception>
        protected AbstractFirstPassGroupingCollector(Sort groupSort, int topNGroups) // LUCENENET: CA1012: Abstract types should not have constructors (marked protected)
        {
            if (topNGroups < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(topNGroups), "topNGroups must be >= 1 (got " + topNGroups + ")"); // LUCENENET specific - changed from IllegalArgumentException to ArgumentOutOfRangeException (.NET convention)
            }

            // TODO: allow null groupSort to mean "by relevance",
            // and specialize it?
            this.groupSort = groupSort;

            this.topNGroups = topNGroups;

            SortField[] sortFields = groupSort.GetSort();
            comparers = new FieldComparer[sortFields.Length];
            compIDXEnd = comparers.Length - 1;
            reversed = new int[sortFields.Length];
            for (int i = 0; i < sortFields.Length; i++)
            {
                SortField sortField = sortFields[i];

                // use topNGroups + 1 so we have a spare slot to use for comparing (tracked by this.spareSlot):
                comparers[i] = sortField.GetComparer(topNGroups + 1, i);
                reversed[i] = sortField.IsReverse ? -1 : 1;
            }

            spareSlot = topNGroups;
            groupMap = new JCG.Dictionary<TGroupValue, CollectedSearchGroup<TGroupValue>>(topNGroups);
        }

        /// <summary>
        /// Returns top groups, starting from offset.  This may
        /// return null, if no groups were collected, or if the
        /// number of unique groups collected is &lt;= offset.
        /// </summary>
        /// <param name="groupOffset">The offset in the collected groups</param>
        /// <param name="fillFields">Whether to fill to <see cref="SearchGroup{TGroupValue}.SortValues"/></param>
        /// <returns>top groups, starting from offset</returns>
        public virtual IEnumerable<ISearchGroup<TGroupValue>> GetTopGroups(int groupOffset, bool fillFields)
        {

            //System.out.println("FP.getTopGroups groupOffset=" + groupOffset + " fillFields=" + fillFields + " groupMap.size()=" + groupMap.size());

            if (groupOffset < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(groupOffset), "groupOffset must be >= 0 (got " + groupOffset + ")"); // LUCENENET specific - changed from IllegalArgumentException to ArgumentOutOfRangeException (.NET convention)
            }

            if (groupMap.Count <= groupOffset)
            {
                return null;
            }

            if (m_orderedGroups is null)
            {
                BuildSortedSet();
            }

            ICollection<ISearchGroup<TGroupValue>> result = new JCG.List<ISearchGroup<TGroupValue>>();
            int upto = 0;
            int sortFieldCount = groupSort.GetSort().Length;
            foreach (CollectedSearchGroup<TGroupValue> group in m_orderedGroups)
            {
                if (upto++ < groupOffset)
                {
                    continue;
                }
                //System.out.println("  group=" + (group.groupValue is null ? "null" : group.groupValue.utf8ToString()));
                SearchGroup<TGroupValue> searchGroup = new SearchGroup<TGroupValue>();
                searchGroup.GroupValue = group.GroupValue;
                if (fillFields)
                {
                    searchGroup.SortValues = new object[sortFieldCount];
                    for (int sortFieldIDX = 0; sortFieldIDX < sortFieldCount; sortFieldIDX++)
                    {
                        searchGroup.SortValues[sortFieldIDX] = comparers[sortFieldIDX].GetValue(group.ComparerSlot);
                    }
                }
                result.Add(searchGroup);
            }
            //System.out.println("  return " + result.size() + " groups");
            return result;
        }

        public virtual void SetScorer(Scorer scorer)
        {
            foreach (FieldComparer comparer in comparers)
            {
                comparer.SetScorer(scorer);
            }
        }

        public virtual void Collect(int doc)
        {
            //System.out.println("FP.collect doc=" + doc);

            // If orderedGroups != null we already have collected N groups and
            // can short circuit by comparing this document to the bottom group,
            // without having to find what group this document belongs to.

            // Even if this document belongs to a group in the top N, we'll know that
            // we don't have to update that group.

            // Downside: if the number of unique groups is very low, this is
            // wasted effort as we will most likely be updating an existing group.
            if (m_orderedGroups != null)
            {
                for (int compIDX = 0; ; compIDX++)
                {
                    int c = reversed[compIDX] * comparers[compIDX].CompareBottom(doc);
                    if (c < 0)
                    {
                        // Definitely not competitive. So don't even bother to continue
                        return;
                    }
                    else if (c > 0)
                    {
                        // Definitely competitive.
                        break;
                    }
                    else if (compIDX == compIDXEnd)
                    {
                        // Here c=0. If we're at the last comparer, this doc is not
                        // competitive, since docs are visited in doc Id order, which means
                        // this doc cannot compete with any other document in the queue.
                        return;
                    }
                }
            }

            // TODO: should we add option to mean "ignore docs that
            // don't have the group field" (instead of stuffing them
            // under null group)?
            TGroupValue groupValue = GetDocGroupValue(doc);

            if (!groupMap.TryGetValue(groupValue, out CollectedSearchGroup<TGroupValue> group))
            {

                // First time we are seeing this group, or, we've seen
                // it before but it fell out of the top N and is now
                // coming back

                if (groupMap.Count < topNGroups)
                {

                    // Still in startup transient: we have not
                    // seen enough unique groups to start pruning them;
                    // just keep collecting them

                    // Add a new CollectedSearchGroup:
                    CollectedSearchGroup<TGroupValue> sg = new CollectedSearchGroup<TGroupValue>();
                    sg.GroupValue = CopyDocGroupValue(groupValue, default);
                    sg.ComparerSlot = groupMap.Count;
                    sg.TopDoc = docBase + doc;
                    foreach (FieldComparer fc in comparers)
                    {
                        fc.Copy(sg.ComparerSlot, doc);
                    }
                    groupMap[sg.GroupValue] = sg;

                    if (groupMap.Count == topNGroups)
                    {
                        // End of startup transient: we now have max
                        // number of groups; from here on we will drop
                        // bottom group when we insert new one:
                        BuildSortedSet();
                    }

                    return;
                }

                // We already tested that the document is competitive, so replace
                // the bottom group with this new group.
                //CollectedSearchGroup<TGroupValue> bottomGroup = orderedGroups.PollLast();
                CollectedSearchGroup<TGroupValue> bottomGroup;
                UninterruptableMonitor.Enter(m_orderedGroups);
                try
                {
                    bottomGroup = m_orderedGroups.Last();
                    m_orderedGroups.Remove(bottomGroup);
                }
                finally
                {
                    UninterruptableMonitor.Exit(m_orderedGroups);
                }
                if (Debugging.AssertsEnabled) Debugging.Assert(m_orderedGroups.Count == topNGroups - 1);

                groupMap.Remove(bottomGroup.GroupValue);

                // reuse the removed CollectedSearchGroup
                bottomGroup.GroupValue = CopyDocGroupValue(groupValue, bottomGroup.GroupValue);
                bottomGroup.TopDoc = docBase + doc;

                foreach (FieldComparer fc in comparers)
                {
                    fc.Copy(bottomGroup.ComparerSlot, doc);
                }

                groupMap[bottomGroup.GroupValue] = bottomGroup;
                m_orderedGroups.Add(bottomGroup);
                if (Debugging.AssertsEnabled) Debugging.Assert(m_orderedGroups.Count == topNGroups);

                int lastComparerSlot = m_orderedGroups.Last().ComparerSlot;
                foreach (FieldComparer fc in comparers)
                {
                    fc.SetBottom(lastComparerSlot);
                }

                return;
            }

            // Update existing group:
            for (int compIDX = 0; ; compIDX++)
            {
                FieldComparer fc = comparers[compIDX];
                fc.Copy(spareSlot, doc);

                int c = reversed[compIDX] * fc.Compare(group.ComparerSlot, spareSlot);
                if (c < 0)
                {
                    // Definitely not competitive.
                    return;
                }
                else if (c > 0)
                {
                    // Definitely competitive; set remaining comparers:
                    for (int compIDX2 = compIDX + 1; compIDX2 < comparers.Length; compIDX2++)
                    {
                        comparers[compIDX2].Copy(spareSlot, doc);
                    }
                    break;
                }
                else if (compIDX == compIDXEnd)
                {
                    // Here c=0. If we're at the last comparer, this doc is not
                    // competitive, since docs are visited in doc Id order, which means
                    // this doc cannot compete with any other document in the queue.
                    return;
                }
            }

            // Remove before updating the group since lookup is done via comparers
            // TODO: optimize this

            CollectedSearchGroup<TGroupValue> prevLast;
            if (m_orderedGroups != null)
            {
                UninterruptableMonitor.Enter(m_orderedGroups);
                try
                {
                    prevLast = m_orderedGroups.Last();
                    m_orderedGroups.Remove(group);
                }
                finally
                {
                    UninterruptableMonitor.Exit(m_orderedGroups);
                }
                if (Debugging.AssertsEnabled) Debugging.Assert(m_orderedGroups.Count == topNGroups - 1);
            }
            else
            {
                prevLast = null;
            }

            group.TopDoc = docBase + doc;

            // Swap slots
            int tmp = spareSlot;
            spareSlot = group.ComparerSlot;
            group.ComparerSlot = tmp;

            // Re-add the changed group
            if (m_orderedGroups != null)
            {
                m_orderedGroups.Add(group);
                if (Debugging.AssertsEnabled) Debugging.Assert(m_orderedGroups.Count == topNGroups);
                var newLast = m_orderedGroups.Last();
                // If we changed the value of the last group, or changed which group was last, then update bottom:
                if (group == newLast || prevLast != newLast)
                {
                    foreach (FieldComparer fc in comparers)
                    {
                        fc.SetBottom(newLast.ComparerSlot);
                    }
                }
            }
        }

        private class BuildSortedSetComparer : IComparer<ICollectedSearchGroup>
        {
            private readonly AbstractFirstPassGroupingCollector<TGroupValue> outerInstance;
            public BuildSortedSetComparer(AbstractFirstPassGroupingCollector<TGroupValue> outerInstance)
            {
                this.outerInstance = outerInstance;
            }
            public int Compare(ICollectedSearchGroup o1, ICollectedSearchGroup o2)
            {
                for (int compIDX = 0; ; compIDX++)
                {
                    FieldComparer fc = outerInstance.comparers[compIDX];
                    int c = outerInstance.reversed[compIDX] * fc.Compare(o1.ComparerSlot, o2.ComparerSlot);
                    if (c != 0)
                    {
                        return c;
                    }
                    else if (compIDX == outerInstance.compIDXEnd)
                    {
                        return o1.TopDoc - o2.TopDoc;
                    }
                }
            }
        }

        private void BuildSortedSet()
        {
            var comparer = new BuildSortedSetComparer(this);
            m_orderedGroups = new JCG.SortedSet<CollectedSearchGroup<TGroupValue>>(comparer);
            m_orderedGroups.UnionWith(groupMap.Values);
            if (Debugging.AssertsEnabled) Debugging.Assert(m_orderedGroups.Count > 0);

            foreach (FieldComparer fc in comparers)
            {
                fc.SetBottom(m_orderedGroups.Last().ComparerSlot);
            }
        }

        public virtual bool AcceptsDocsOutOfOrder => false;

        public virtual void SetNextReader(AtomicReaderContext context)
        {
            docBase = context.DocBase;
            for (int i = 0; i < comparers.Length; i++)
            {
                comparers[i] = comparers[i].SetNextReader(context);
            }
        }

        /// <summary>
        /// Returns the group value for the specified doc.
        /// </summary>
        /// <param name="doc">The specified doc</param>
        /// <returns>the group value for the specified doc</returns>
        protected abstract TGroupValue GetDocGroupValue(int doc);

        /// <summary>
        /// Returns a copy of the specified group value by creating a new instance and copying the value from the specified
        /// groupValue in the new instance. Or optionally the reuse argument can be used to copy the group value in.
        /// </summary>
        /// <param name="groupValue">The group value to copy</param>
        /// <param name="reuse">Optionally a reuse instance to prevent a new instance creation</param>
        /// <returns>a copy of the specified group value</returns>
        protected abstract TGroupValue CopyDocGroupValue(TGroupValue groupValue, TGroupValue reuse);

    }

    /// <summary>
    /// LUCENENET specific interface used to apply covariance to TGroupValue
    /// to simulate Java's wildcard generics.
    /// </summary>
    /// <typeparam name="TGroupValue"></typeparam>
    public interface IAbstractFirstPassGroupingCollector<out TGroupValue> : ICollector
    {
        /// <summary>
        /// Returns top groups, starting from offset.  This may
        /// return null, if no groups were collected, or if the
        /// number of unique groups collected is &lt;= offset.
        /// </summary>
        /// <param name="groupOffset">The offset in the collected groups</param>
        /// <param name="fillFields">Whether to fill to <see cref="SearchGroup{TGroupValue}.SortValues"/></param>
        /// <returns>top groups, starting from offset</returns>
        /// <remarks>
        /// LUCENENET NOTE: We must use <see cref="IEnumerable{TGroupValue}"/> rather than 
        /// <see cref="ICollection{TGroupValue}"/> here because we need this to be covariant
        /// </remarks>
        IEnumerable<ISearchGroup<TGroupValue>> GetTopGroups(int groupOffset, bool fillFields);
    }
}
