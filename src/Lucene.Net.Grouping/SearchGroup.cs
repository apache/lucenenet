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
    /// Represents a group that is found during the first pass search.
    /// 
    /// @lucene.experimental
    /// </summary>
    /// <typeparam name="TGroupValue"></typeparam>
    public class SearchGroup<TGroupValue> : ISearchGroup<TGroupValue>
    {
        /// <summary>
        /// The value that defines this group 
        /// </summary>
        public TGroupValue GroupValue { get; set; }

        /// <summary>
        /// The sort values used during sorting. These are the
        /// groupSort field values of the highest rank document
        /// (by the groupSort) within the group.  Can be
        /// <c>null</c> if <c>fillFields=false</c> had
        /// been passed to <see cref="AbstractFirstPassGroupingCollector{TGroupValue}.GetTopGroups(int, bool)"/>
        /// </summary>
        public object[] SortValues { get; set; }

        public override string ToString()
        {
            return ("SearchGroup(groupValue=" + GroupValue + " sortValues=" + Arrays.ToString(SortValues) + ")");
        }

        public override bool Equals(object o)
        {
            if (this == o) return true;
            if (o == null || GetType() != o.GetType()) return false;

            SearchGroup<TGroupValue> that = (SearchGroup<TGroupValue>)o;

            if (GroupValue == null)
            {
                if (that.GroupValue != null)
                {
                    return false;
                }
            }
            else if (!GroupValue.Equals(that.GroupValue))
            {
                return false;
            }

            return true;
        }

        public override int GetHashCode()
        {
            return GroupValue != null ? GroupValue.GetHashCode() : 0;
        }
    }

    /// <summary>
    /// LUCENENET specific class used to nest types to mimic the syntax used 
    /// by Lucene (that is, without specifying the generic closing type of <see cref="SearchGroup{TGroupValue}"/>)
    /// </summary>
    public class SearchGroup
    {
        /// <summary>
        /// Prevent direct creation
        /// </summary>
        private SearchGroup() { }

        private class ShardIter<T>
        {
            public readonly IEnumerator<ISearchGroup<T>> iter;
            public readonly int shardIndex;

            public ShardIter(IEnumerable<ISearchGroup<T>> shard, int shardIndex)
            {
                this.shardIndex = shardIndex;
                iter = shard.GetEnumerator();
                //Debug.Assert(iter.hasNext()); // No reasonable way to do this in .NET
            }

            public ISearchGroup<T> Next()
            {
                //Debug.Assert(iter.hasNext()); // No reasonable way to do this in .NET
                ISearchGroup<T> group = iter.Current;
                if (group.SortValues == null)
                {
                    throw new ArgumentException("group.sortValues is null; you must pass fillFields=true to the first pass collector");
                }
                return group;
            }

            public override string ToString()
            {
                return "ShardIter(shard=" + shardIndex + ")";
            }
        }

        /// <summary>
        /// Holds all shards currently on the same group
        /// </summary>
        /// <typeparam name="T"></typeparam>
        private class MergedGroup<T>
        {

            // groupValue may be null!
            public readonly T groupValue;

            public object[] topValues;
            public readonly List<ShardIter<T>> shards = new List<ShardIter<T>>();
            public int minShardIndex;
            public bool processed;
            public bool inQueue;

            public MergedGroup(T groupValue)
            {
                this.groupValue = groupValue;
            }

            // Only for assert
            private bool NeverEquals(object other)
            {
                if (other is MergedGroup<T>)
                {
                    MergedGroup<T> otherMergedGroup = (MergedGroup<T>)other;
                    if (groupValue == null)
                    {
                        Debug.Assert(otherMergedGroup.groupValue != null);
                    }
                    else
                    {
                        Debug.Assert(!groupValue.Equals(otherMergedGroup.groupValue));
                    }
                }
                return true;
            }

            public override bool Equals(object other)
            {
                // We never have another MergedGroup instance with
                // same groupValue
                Debug.Assert(NeverEquals(other));

                if (other is MergedGroup<T>)
                {
                    MergedGroup<T> otherMergedGroup = (MergedGroup<T>)other;
                    if (groupValue == null)
                    {
                        return otherMergedGroup == null;
                    }
                    else
                    {
                        return groupValue.Equals(otherMergedGroup);
                    }
                }
                else
                {
                    return false;
                }
            }

            public override int GetHashCode()
            {
                if (groupValue == null)
                {
                    return 0;
                }
                else
                {
                    return groupValue.GetHashCode();
                }
            }
        }

        private class GroupComparator<T> : IComparer<MergedGroup<T>>
        {

            public readonly FieldComparator[] comparators;
            public readonly int[] reversed;

            public GroupComparator(Sort groupSort)
            {
                SortField[] sortFields = groupSort.GetSort();
                comparators = new FieldComparator[sortFields.Length];
                reversed = new int[sortFields.Length];
                for (int compIDX = 0; compIDX < sortFields.Length; compIDX++)
                {
                    SortField sortField = sortFields[compIDX];
                    comparators[compIDX] = sortField.GetComparator(1, compIDX);
                    reversed[compIDX] = sortField.Reverse ? -1 : 1;
                }
            }

            public virtual int Compare(MergedGroup<T> group, MergedGroup<T> other)
            {
                if (group == other)
                {
                    return 0;
                }
                //System.out.println("compare group=" + group + " other=" + other);
                object[] groupValues = group.topValues;
                object[] otherValues = other.topValues;
                //System.out.println("  groupValues=" + groupValues + " otherValues=" + otherValues);
                for (int compIDX = 0; compIDX < comparators.Length; compIDX++)
                {
                    int c = reversed[compIDX] * comparators[compIDX].CompareValues(groupValues[compIDX],
                                                                                         otherValues[compIDX]);
                    if (c != 0)
                    {
                        return c;
                    }
                }

                // Tie break by min shard index:
                Debug.Assert(group.minShardIndex != other.minShardIndex);
                return group.minShardIndex - other.minShardIndex;
            }
        }

        private class GroupMerger<T>
        {

            private readonly GroupComparator<T> groupComp;
            private readonly TreeSet<MergedGroup<T>> queue;
            private readonly IDictionary<T, MergedGroup<T>> groupsSeen;

            public GroupMerger(Sort groupSort)
            {
                groupComp = new GroupComparator<T>(groupSort);
                queue = new TreeSet<MergedGroup<T>>(groupComp);
                groupsSeen = new HashMap<T, MergedGroup<T>>();
            }

            private void UpdateNextGroup(int topN, ShardIter<T> shard)
            {
                while (shard.iter.MoveNext())
                {
                    ISearchGroup<T> group = shard.Next();
                    MergedGroup<T> mergedGroup = groupsSeen.ContainsKey(group.GroupValue) ? groupsSeen[group.GroupValue] : null;
                    bool isNew = mergedGroup == null;
                    //System.out.println("    next group=" + (group.groupValue == null ? "null" : ((BytesRef) group.groupValue).utf8ToString()) + " sort=" + Arrays.toString(group.sortValues));

                    if (isNew)
                    {
                        // Start a new group:
                        //System.out.println("      new");
                        mergedGroup = new MergedGroup<T>(group.GroupValue);
                        mergedGroup.minShardIndex = shard.shardIndex;
                        Debug.Assert(group.SortValues != null);
                        mergedGroup.topValues = group.SortValues;
                        groupsSeen[group.GroupValue] = mergedGroup;
                        mergedGroup.inQueue = true;
                        queue.Add(mergedGroup);
                    }
                    else if (mergedGroup.processed)
                    {
                        // This shard produced a group that we already
                        // processed; move on to next group...
                        continue;
                    }
                    else
                    {
                        //System.out.println("      old");
                        bool competes = false;
                        for (int compIDX = 0; compIDX < groupComp.comparators.Length; compIDX++)
                        {
                            int cmp = groupComp.reversed[compIDX] * groupComp.comparators[compIDX].CompareValues(group.SortValues[compIDX],
                                                                                                                       mergedGroup.topValues[compIDX]);
                            if (cmp < 0)
                            {
                                // Definitely competes
                                competes = true;
                                break;
                            }
                            else if (cmp > 0)
                            {
                                // Definitely does not compete
                                break;
                            }
                            else if (compIDX == groupComp.comparators.Length - 1)
                            {
                                if (shard.shardIndex < mergedGroup.minShardIndex)
                                {
                                    competes = true;
                                }
                            }
                        }

                        //System.out.println("      competes=" + competes);

                        if (competes)
                        {
                            // Group's sort changed -- remove & re-insert
                            if (mergedGroup.inQueue)
                            {
                                queue.Remove(mergedGroup);
                            }
                            mergedGroup.topValues = group.SortValues;
                            mergedGroup.minShardIndex = shard.shardIndex;
                            queue.Add(mergedGroup);
                            mergedGroup.inQueue = true;
                        }
                    }

                    mergedGroup.shards.Add(shard);
                    break;
                }

                // Prune un-competitive groups:
                while (queue.Count > topN)
                {
                    MergedGroup<T> group = queue.Last();
                    queue.Remove(group);
                    //System.out.println("PRUNE: " + group);
                    group.inQueue = false;
                }
            }

            public virtual ICollection<SearchGroup<T>> Merge(IList<IEnumerable<ISearchGroup<T>>> shards, int offset, int topN)
            {

                int maxQueueSize = offset + topN;

                //System.out.println("merge");
                // Init queue:
                for (int shardIDX = 0; shardIDX < shards.Count; shardIDX++)
                {
                    IEnumerable<ISearchGroup<T>> shard = shards[shardIDX];
                    if (shard.Any())
                    {
                        //System.out.println("  insert shard=" + shardIDX);
                        UpdateNextGroup(maxQueueSize, new ShardIter<T>(shard, shardIDX));
                    }
                }

                // Pull merged topN groups:
                List<SearchGroup<T>> newTopGroups = new List<SearchGroup<T>>();

                int count = 0;

                while (queue.Count != 0)
                {
                    MergedGroup<T> group = queue.First();
                    queue.Remove(group);
                    group.processed = true;
                    //System.out.println("  pop: shards=" + group.shards + " group=" + (group.groupValue == null ? "null" : (((BytesRef) group.groupValue).utf8ToString())) + " sortValues=" + Arrays.toString(group.topValues));
                    if (count++ >= offset)
                    {
                        SearchGroup<T> newGroup = new SearchGroup<T>();
                        newGroup.GroupValue = group.groupValue;
                        newGroup.SortValues = group.topValues;
                        newTopGroups.Add(newGroup);
                        if (newTopGroups.Count == topN)
                        {
                            break;
                        }
                        //} else {
                        // System.out.println("    skip < offset");
                    }

                    // Advance all iters in this group:
                    foreach (ShardIter<T> shardIter in group.shards)
                    {
                        UpdateNextGroup(maxQueueSize, shardIter);
                    }
                }

                if (newTopGroups.Count == 0)
                {
                    return null;
                }
                else
                {
                    return newTopGroups;
                }
            }
        }

        /// <summary>
        /// Merges multiple collections of top groups, for example
        /// obtained from separate index shards.  The provided
        /// groupSort must match how the groups were sorted, and
        /// the provided SearchGroups must have been computed
        /// with <c>fillFields=true</c> passed to
        /// <see cref="AbstractFirstPassGroupingCollector{TGroupValue}.GetTopGroups(int, bool)"/>.
        /// <para>
        /// NOTE: this returns null if the topGroups is empty.
        /// </para>
        /// </summary>
        public static ICollection<SearchGroup<T>> Merge<T>(IList<IEnumerable<ISearchGroup<T>>> topGroups, int offset, int topN, Sort groupSort)
        {
            if (topGroups.Count == 0)
            {
                return null;
            }
            else
            {
                return new GroupMerger<T>(groupSort).Merge(topGroups, offset, topN);
            }
        }
    }


    /// <summary>
    /// LUCENENET specific interface used to provide covariance
    /// with the TGroupValue type to simulate Java's wildcard generics.
    /// </summary>
    /// <typeparam name="TGroupValue"></typeparam>
    public interface ISearchGroup<out TGroupValue>
    {
        /// <summary>
        /// The value that defines this group 
        /// </summary>
        TGroupValue GroupValue { get; }

        /// <summary>
        /// The sort values used during sorting. These are the
        /// groupSort field values of the highest rank document
        /// (by the groupSort) within the group.  Can be
        /// <c>null</c> if <c>fillFields=false</c> had
        /// been passed to <see cref="AbstractFirstPassGroupingCollector{TGroupValue}.GetTopGroups(int, bool)"/>
        /// </summary>
        object[] SortValues { get; set; }
    }
}
