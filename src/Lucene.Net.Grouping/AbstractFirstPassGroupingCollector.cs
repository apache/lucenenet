using Lucene.Net.Index;
using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

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
    public abstract class AbstractFirstPassGroupingCollector<TGroupValue> : Collector, IAbstractFirstPassGroupingCollector<TGroupValue>
    {
        private readonly Sort groupSort;
        private readonly FieldComparator[] comparators;
        private readonly int[] reversed;
        private readonly int topNGroups;
        private readonly IDictionary<TGroupValue, CollectedSearchGroup<TGroupValue>> groupMap;
        private readonly int compIDXEnd;

        // Set once we reach topNGroups unique groups:
        // @lucene.internal
        protected SortedSet<CollectedSearchGroup<TGroupValue>> orderedGroups;
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
        public AbstractFirstPassGroupingCollector(Sort groupSort, int topNGroups)
        {
            if (topNGroups < 1)
            {
                throw new ArgumentException("topNGroups must be >= 1 (got " + topNGroups + ")");
            }

            // TODO: allow null groupSort to mean "by relevance",
            // and specialize it?
            this.groupSort = groupSort;

            this.topNGroups = topNGroups;

            SortField[] sortFields = groupSort.GetSort();
            comparators = new FieldComparator[sortFields.Length];
            compIDXEnd = comparators.Length - 1;
            reversed = new int[sortFields.Length];
            for (int i = 0; i < sortFields.Length; i++)
            {
                SortField sortField = sortFields[i];

                // use topNGroups + 1 so we have a spare slot to use for comparing (tracked by this.spareSlot):
                comparators[i] = sortField.GetComparator(topNGroups + 1, i);
                reversed[i] = sortField.Reverse ? -1 : 1;
            }

            spareSlot = topNGroups;
            groupMap = new HashMap<TGroupValue, CollectedSearchGroup<TGroupValue>>(topNGroups);
        }

        /// <summary>
        /// Returns top groups, starting from offset.  This may
        /// return null, if no groups were collected, or if the
        /// number of unique groups collected is &lt;= offset.
        /// </summary>
        /// <param name="groupOffset">The offset in the collected groups</param>
        /// <param name="fillFields">Whether to fill to <see cref="SearchGroup.sortValues"/></param>
        /// <returns>top groups, starting from offset</returns>
        public virtual IEnumerable<ISearchGroup<TGroupValue>> GetTopGroups(int groupOffset, bool fillFields)
        {

            //System.out.println("FP.getTopGroups groupOffset=" + groupOffset + " fillFields=" + fillFields + " groupMap.size()=" + groupMap.size());

            if (groupOffset < 0)
            {
                throw new ArgumentException("groupOffset must be >= 0 (got " + groupOffset + ")");
            }

            if (groupMap.Count <= groupOffset)
            {
                return null;
            }

            if (orderedGroups == null)
            {
                BuildSortedSet();
            }

            ICollection<ISearchGroup<TGroupValue>> result = new List<ISearchGroup<TGroupValue>>();
            int upto = 0;
            int sortFieldCount = groupSort.GetSort().Length;
            foreach (CollectedSearchGroup<TGroupValue> group in orderedGroups)
            {
                if (upto++ < groupOffset)
                {
                    continue;
                }
                //System.out.println("  group=" + (group.groupValue == null ? "null" : group.groupValue.utf8ToString()));
                SearchGroup<TGroupValue> searchGroup = new SearchGroup<TGroupValue>();
                searchGroup.GroupValue = group.GroupValue;
                if (fillFields)
                {
                    searchGroup.SortValues = new object[sortFieldCount];
                    for (int sortFieldIDX = 0; sortFieldIDX < sortFieldCount; sortFieldIDX++)
                    {
                        searchGroup.SortValues[sortFieldIDX] = comparators[sortFieldIDX].Value(group.ComparatorSlot);
                    }
                }
                result.Add(searchGroup);
            }
            //System.out.println("  return " + result.size() + " groups");
            return result;
        }

        public override Scorer Scorer
        {
            set
            {
                foreach (FieldComparator comparator in comparators)
                {
                    comparator.Scorer = value;
                }
            }
        }

        public override void Collect(int doc)
        {
            //System.out.println("FP.collect doc=" + doc);

            // If orderedGroups != null we already have collected N groups and
            // can short circuit by comparing this document to the bottom group,
            // without having to find what group this document belongs to.

            // Even if this document belongs to a group in the top N, we'll know that
            // we don't have to update that group.

            // Downside: if the number of unique groups is very low, this is
            // wasted effort as we will most likely be updating an existing group.
            if (orderedGroups != null)
            {
                for (int compIDX = 0; ; compIDX++)
                {
                    int c = reversed[compIDX] * comparators[compIDX].CompareBottom(doc);
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
                        // Here c=0. If we're at the last comparator, this doc is not
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

            CollectedSearchGroup<TGroupValue> group;
            if (!groupMap.TryGetValue(groupValue, out group))
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
                    sg.GroupValue = CopyDocGroupValue(groupValue, default(TGroupValue));
                    sg.ComparatorSlot = groupMap.Count;
                    sg.TopDoc = docBase + doc;
                    foreach (FieldComparator fc in comparators)
                    {
                        fc.Copy(sg.ComparatorSlot, doc);
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
                lock (orderedGroups)
                {
                    bottomGroup = orderedGroups.Last();
                    orderedGroups.Remove(bottomGroup);
                }
                Debug.Assert(orderedGroups.Count == topNGroups - 1);

                groupMap.Remove(bottomGroup.GroupValue);

                // reuse the removed CollectedSearchGroup
                bottomGroup.GroupValue = CopyDocGroupValue(groupValue, bottomGroup.GroupValue);
                bottomGroup.TopDoc = docBase + doc;

                foreach (FieldComparator fc in comparators)
                {
                    fc.Copy(bottomGroup.ComparatorSlot, doc);
                }

                groupMap[bottomGroup.GroupValue] = bottomGroup;
                orderedGroups.Add(bottomGroup);
                Debug.Assert(orderedGroups.Count == topNGroups);

                int lastComparatorSlot = orderedGroups.Last().ComparatorSlot;
                foreach (FieldComparator fc in comparators)
                {
                    fc.Bottom = lastComparatorSlot;
                }

                return;
            }

            // Update existing group:
            for (int compIDX = 0; ; compIDX++)
            {
                FieldComparator fc = comparators[compIDX];
                fc.Copy(spareSlot, doc);

                int c = reversed[compIDX] * fc.Compare(group.ComparatorSlot, spareSlot);
                if (c < 0)
                {
                    // Definitely not competitive.
                    return;
                }
                else if (c > 0)
                {
                    // Definitely competitive; set remaining comparators:
                    for (int compIDX2 = compIDX + 1; compIDX2 < comparators.Length; compIDX2++)
                    {
                        comparators[compIDX2].Copy(spareSlot, doc);
                    }
                    break;
                }
                else if (compIDX == compIDXEnd)
                {
                    // Here c=0. If we're at the last comparator, this doc is not
                    // competitive, since docs are visited in doc Id order, which means
                    // this doc cannot compete with any other document in the queue.
                    return;
                }
            }

            // Remove before updating the group since lookup is done via comparators
            // TODO: optimize this

            CollectedSearchGroup<TGroupValue> prevLast;
            if (orderedGroups != null)
            {
                lock (orderedGroups)
                {
                    prevLast = orderedGroups.Last();
                    orderedGroups.Remove(group);
                }
                Debug.Assert(orderedGroups.Count == topNGroups - 1);
            }
            else
            {
                prevLast = null;
            }

            group.TopDoc = docBase + doc;

            // Swap slots
            int tmp = spareSlot;
            spareSlot = group.ComparatorSlot;
            group.ComparatorSlot = tmp;

            // Re-add the changed group
            if (orderedGroups != null)
            {
                orderedGroups.Add(group);
                Debug.Assert(orderedGroups.Count == topNGroups);
                var newLast = orderedGroups.Last();
                // If we changed the value of the last group, or changed which group was last, then update bottom:
                if (group == newLast || prevLast != newLast)
                {
                    foreach (FieldComparator fc in comparators)
                    {
                        fc.Bottom = newLast.ComparatorSlot;
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
                    FieldComparator fc = outerInstance.comparators[compIDX];
                    int c = outerInstance.reversed[compIDX] * fc.Compare(o1.ComparatorSlot, o2.ComparatorSlot);
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
            var comparator = new BuildSortedSetComparer(this);
            orderedGroups = new SortedSet<CollectedSearchGroup<TGroupValue>>(comparator);
            orderedGroups.UnionWith(groupMap.Values);
            Debug.Assert(orderedGroups.Count > 0);

            foreach (FieldComparator fc in comparators)
            {
                fc.Bottom = orderedGroups.Last().ComparatorSlot;
            }
        }

        public override bool AcceptsDocsOutOfOrder()
        {
            return false;
        }

        public override AtomicReaderContext NextReader
        {
            set
            {
                docBase = value.DocBase;
                for (int i = 0; i < comparators.Length; i++)
                {
                    comparators[i] = comparators[i].SetNextReader(value);
                }
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
    public interface IAbstractFirstPassGroupingCollector<out TGroupValue>
    {
        /// <summary>
        /// Returns top groups, starting from offset.  This may
        /// return null, if no groups were collected, or if the
        /// number of unique groups collected is &lt;= offset.
        /// </summary>
        /// <param name="groupOffset">The offset in the collected groups</param>
        /// <param name="fillFields">Whether to fill to <see cref="SearchGroup.sortValues"/></param>
        /// <returns>top groups, starting from offset</returns>
        /// <remarks>
        /// LUCENENET NOTE: We must use <see cref="IEnumerable{TGroupValue}"/> rather than 
        /// <see cref="ICollection{TGroupValue}"/> here because we need this to be covariant
        /// </remarks>
        IEnumerable<ISearchGroup<TGroupValue>> GetTopGroups(int groupOffset, bool fillFields);
    }
}
