using Lucene.Net.Diagnostics;
using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
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
        [WritableArray]
        [SuppressMessage("Microsoft.Performance", "CA1819", Justification = "Lucene's design requires some writable array properties")]
        public object[] SortValues { get; set; }

        public override string ToString()
        {
            return ("SearchGroup(groupValue=" + GroupValue + " sortValues=" + Arrays.ToString(SortValues) + ")");
        }

        public override bool Equals(object o)
        {
            if (this == o) return true;
            if (o is null || GetType() != o.GetType()) return false;

            SearchGroup<TGroupValue> that = (SearchGroup<TGroupValue>)o;

            if (GroupValue is null)
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
    public static class SearchGroup // LUCENENET specific: CA1052 Static holder types should be Static or NotInheritable
    {
        private class ShardIter<T>
        {
            public IEnumerator<ISearchGroup<T>> Iter => iter;
            private readonly IEnumerator<ISearchGroup<T>> iter;

            public int ShardIndex => shardIndex;
            private readonly int shardIndex;

            public ShardIter(IEnumerable<ISearchGroup<T>> shard, int shardIndex)
            {
                this.shardIndex = shardIndex;
                iter = shard.GetEnumerator();
                //if (Debugging.AssertsEnabled) Debugging.Assert(iter.hasNext()); // No reasonable way to do this in .NET
            }

            public ISearchGroup<T> Next()
            {
                //if (Debugging.AssertsEnabled) Debugging.Assert(iter.hasNext()); // No reasonable way to do this in .NET
                ISearchGroup<T> group = iter.Current;
                if (group.SortValues is null)
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
            public T GroupValue => groupValue;
            private readonly T groupValue;

            [WritableArray]
            [SuppressMessage("Microsoft.Performance", "CA1819", Justification = "Lucene's design requires some writable array properties")]
            public object[] TopValues
            {
                get => topValues;
                set => topValues = value;
            }
            private object[] topValues;

            public IList<ShardIter<T>> Shards => shards;
            private readonly IList<ShardIter<T>> shards = new JCG.List<ShardIter<T>>();

            public int MinShardIndex
            {
                get => minShardIndex;
                set => minShardIndex = value;
            }
            private int minShardIndex;

            public bool IsProcessed
            {
                get => processed;
                set => processed = value;
            }
            private bool processed;

            public bool IsInQueue
            {
                get => inQueue;
                set => inQueue = value;
            }
            private bool inQueue;

            // LUCENENET specific - store whether T is value type
            // for optimization of GetHashCode() and Equals()
            private readonly static bool groupValueIsValueType = typeof(T).IsValueType;

            public MergedGroup(T groupValue)
            {
                this.groupValue = groupValue;
            }

            // Only for assert
            private bool NeverEquals(object other)
            {
                if (other is MergedGroup<T> otherMergedGroup)
                {
                    if (groupValue is null)
                    {
                        if (Debugging.AssertsEnabled) Debugging.Assert(otherMergedGroup.groupValue != null);
                    }
                    else
                    {
                        
                        if (Debugging.AssertsEnabled) Debugging.Assert(!groupValueIsValueType 
                            ? JCG.EqualityComparer<T>.Default.Equals(groupValue, otherMergedGroup.groupValue)

                            // LUCENENET specific - use J2N.Collections.StructuralEqualityComparer.Default.Equals() if we have a reference type
                            // to ensure if it is a collection its contents are compared
                            : J2N.Collections.StructuralEqualityComparer.Default.Equals(groupValue, otherMergedGroup.groupValue));
                    }
                }
                return true;
            }

            public override bool Equals(object other)
            {
                // We never have another MergedGroup instance with
                // same groupValue
                if (Debugging.AssertsEnabled) Debugging.Assert(NeverEquals(other));

                if (other is MergedGroup<T> otherMergedGroup)
                {
                    if (groupValue is null)
                    {
                        return otherMergedGroup is null;
                    }
                    else
                    {
                        // LUCENENET specific - use J2N.Collections.StructuralEqualityComparer.Default.Equals() if we have a reference type
                        // to ensure if it is a collection its contents are compared
                        return groupValueIsValueType ?
                            JCG.EqualityComparer<T>.Default.Equals(groupValue, otherMergedGroup.groupValue) :
                            J2N.Collections.StructuralEqualityComparer.Default.Equals(groupValue, otherMergedGroup.groupValue);
                    }
                }
                else
                {
                    return false;
                }
            }

            public override int GetHashCode()
            {
                if (groupValue is null)
                {
                    return 0;
                }
                else
                {
                    // LUCENENET specific - use J2N.Collections.StructuralEqualityComparer.Default.GetHashCode() if we have a reference type
                    // to ensure if it is a collection its contents are compared
                    return groupValueIsValueType ?
                        JCG.EqualityComparer<T>.Default.GetHashCode(groupValue) :
                        J2N.Collections.StructuralEqualityComparer.Default.GetHashCode(groupValue);
                }
            }
        }

        private class GroupComparer<T> : IComparer<MergedGroup<T>>
        {
            [WritableArray]
            [SuppressMessage("Microsoft.Performance", "CA1819", Justification = "Lucene's design requires some writable array properties")]
            public FieldComparer[] Comparers => comparers;

            private readonly FieldComparer[] comparers;

            [WritableArray]
            [SuppressMessage("Microsoft.Performance", "CA1819", Justification = "Lucene's design requires some writable array properties")]
            public int[] Reversed => reversed;

            private readonly int[] reversed;

            public GroupComparer(Sort groupSort)
            {
                SortField[] sortFields = groupSort.GetSort();
                comparers = new FieldComparer[sortFields.Length];
                reversed = new int[sortFields.Length];
                for (int compIDX = 0; compIDX < sortFields.Length; compIDX++)
                {
                    SortField sortField = sortFields[compIDX];
                    comparers[compIDX] = sortField.GetComparer(1, compIDX);
                    reversed[compIDX] = sortField.IsReverse ? -1 : 1;
                }
            }

            public virtual int Compare(MergedGroup<T> group, MergedGroup<T> other)
            {
                if (group == other)
                {
                    return 0;
                }
                //System.out.println("compare group=" + group + " other=" + other);
                object[] groupValues = group.TopValues;
                object[] otherValues = other.TopValues;
                //System.out.println("  groupValues=" + groupValues + " otherValues=" + otherValues);
                for (int compIDX = 0; compIDX < comparers.Length; compIDX++)
                {
                    int c = reversed[compIDX] * comparers[compIDX].CompareValues(groupValues[compIDX],
                                                                                         otherValues[compIDX]);
                    if (c != 0)
                    {
                        return c;
                    }
                }

                // Tie break by min shard index:
                if (Debugging.AssertsEnabled) Debugging.Assert(group.MinShardIndex != other.MinShardIndex);
                return group.MinShardIndex - other.MinShardIndex;
            }
        }

        private class GroupMerger<T>
        {

            private readonly GroupComparer<T> groupComp;
            private readonly JCG.SortedSet<MergedGroup<T>> queue;
            private readonly IDictionary<T, MergedGroup<T>> groupsSeen;

            public GroupMerger(Sort groupSort)
            {
                groupComp = new GroupComparer<T>(groupSort);
                queue = new JCG.SortedSet<MergedGroup<T>>(groupComp);
                groupsSeen = new JCG.Dictionary<T, MergedGroup<T>>();
            }

            private void UpdateNextGroup(int topN, ShardIter<T> shard)
            {
                while (shard.Iter.MoveNext())
                {
                    ISearchGroup<T> group = shard.Next();
                    bool isNew = !groupsSeen.TryGetValue(group.GroupValue, out MergedGroup<T> mergedGroup) || mergedGroup is null;
                    //System.out.println("    next group=" + (group.groupValue is null ? "null" : ((BytesRef) group.groupValue).utf8ToString()) + " sort=" + Arrays.toString(group.sortValues));

                    if (isNew)
                    {
                        // Start a new group:
                        //System.out.println("      new");
                        mergedGroup = new MergedGroup<T>(group.GroupValue);
                        mergedGroup.MinShardIndex = shard.ShardIndex;
                        if (Debugging.AssertsEnabled) Debugging.Assert(group.SortValues != null);
                        mergedGroup.TopValues = group.SortValues;
                        groupsSeen[group.GroupValue] = mergedGroup;
                        mergedGroup.IsInQueue = true;
                        queue.Add(mergedGroup);
                    }
                    else if (mergedGroup.IsProcessed)
                    {
                        // This shard produced a group that we already
                        // processed; move on to next group...
                        continue;
                    }
                    else
                    {
                        //System.out.println("      old");
                        bool competes = false;
                        for (int compIDX = 0; compIDX < groupComp.Comparers.Length; compIDX++)
                        {
                            int cmp = groupComp.Reversed[compIDX] * groupComp.Comparers[compIDX].CompareValues(group.SortValues[compIDX],
                                                                                                                       mergedGroup.TopValues[compIDX]);
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
                            else if (compIDX == groupComp.Comparers.Length - 1)
                            {
                                if (shard.ShardIndex < mergedGroup.MinShardIndex)
                                {
                                    competes = true;
                                }
                            }
                        }

                        //System.out.println("      competes=" + competes);

                        if (competes)
                        {
                            // Group's sort changed -- remove & re-insert
                            if (mergedGroup.IsInQueue)
                            {
                                queue.Remove(mergedGroup);
                            }
                            mergedGroup.TopValues = group.SortValues;
                            mergedGroup.MinShardIndex = shard.ShardIndex;
                            queue.Add(mergedGroup);
                            mergedGroup.IsInQueue = true;
                        }
                    }

                    mergedGroup.Shards.Add(shard);
                    break;
                }

                // Prune un-competitive groups:
                while (queue.Count > topN)
                {
                    MergedGroup<T> group = queue.Max;
                    queue.Remove(group);
                    //System.out.println("PRUNE: " + group);
                    group.IsInQueue = false;
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
                    if (shard.Any()) // LUCENENET TODO: Change back to .Count if/when IEnumerable<T> is changed to ICollection<T> or IReadOnlyCollection<T>
                    {
                        //System.out.println("  insert shard=" + shardIDX);
                        UpdateNextGroup(maxQueueSize, new ShardIter<T>(shard, shardIDX));
                    }
                }

                // Pull merged topN groups:
                IList<SearchGroup<T>> newTopGroups = new JCG.List<SearchGroup<T>>();

                int count = 0;

                while (queue.Count != 0)
                {
                    MergedGroup<T> group = queue.Min;
                    queue.Remove(group);
                    group.IsProcessed = true;
                    //System.out.println("  pop: shards=" + group.shards + " group=" + (group.groupValue is null ? "null" : (((BytesRef) group.groupValue).utf8ToString())) + " sortValues=" + Arrays.toString(group.topValues));
                    if (count++ >= offset)
                    {
                        SearchGroup<T> newGroup = new SearchGroup<T>();
                        newGroup.GroupValue = group.GroupValue;
                        newGroup.SortValues = group.TopValues;
                        newTopGroups.Add(newGroup);
                        if (newTopGroups.Count == topN)
                        {
                            break;
                        }
                        //} else {
                        // System.out.println("    skip < offset");
                    }

                    // Advance all iters in this group:
                    foreach (ShardIter<T> shardIter in group.Shards)
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
        [SuppressMessage("Microsoft.Performance", "CA1819", Justification = "Lucene's design requires some writable array properties")]
        [WritableArray]
        object[] SortValues { get; set; }
    }
}
