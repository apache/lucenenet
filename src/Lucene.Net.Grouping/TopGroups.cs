using J2N.Collections;
using Lucene.Net.Support;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

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
    /// Represents result returned by a grouping search.
    /// 
    /// @lucene.experimental 
    /// </summary>
    public class TopGroups<TGroupValue> : ITopGroups<TGroupValue>
    {
        /// <summary>
        /// Number of documents matching the search </summary>
        public int TotalHitCount { get; private set; }

        /// <summary>
        /// Number of documents grouped into the topN groups </summary>
        public int TotalGroupedHitCount { get; private set; }

        /// <summary>
        /// The total number of unique groups. If <c>null</c> this value is not computed. </summary>
        public int? TotalGroupCount { get; private set; }

        /// <summary>
        /// Group results in groupSort order </summary>
        [WritableArray]
        [SuppressMessage("Microsoft.Performance", "CA1819", Justification = "Lucene's design requires some writable array properties")]
        public IGroupDocs<TGroupValue>[] Groups { get; private set; }

        /// <summary>
        /// How groups are sorted against each other </summary>
        [WritableArray]
        [SuppressMessage("Microsoft.Performance", "CA1819", Justification = "Lucene's design requires some writable array properties")]
        public SortField[] GroupSort { get; private set; }

        /// <summary>
        /// How docs are sorted within each group </summary>
        [WritableArray]
        [SuppressMessage("Microsoft.Performance", "CA1819", Justification = "Lucene's design requires some writable array properties")]
        public SortField[] WithinGroupSort { get; private set; }

        /// <summary>
        /// Highest score across all hits, or
        /// <see cref="float.NaN"/> if scores were not computed. 
        /// </summary>
        public float MaxScore { get; private set; }

        public TopGroups(SortField[] groupSort, SortField[] withinGroupSort, int totalHitCount, int totalGroupedHitCount, IGroupDocs<TGroupValue>[] groups, float maxScore)
        {
            GroupSort = groupSort;
            WithinGroupSort = withinGroupSort;
            TotalHitCount = totalHitCount;
            TotalGroupedHitCount = totalGroupedHitCount;
            Groups = groups;
            TotalGroupCount = null;
            MaxScore = maxScore;
        }

        public TopGroups(ITopGroups<TGroupValue> oldTopGroups, int? totalGroupCount)
        {
            GroupSort = oldTopGroups.GroupSort;
            WithinGroupSort = oldTopGroups.WithinGroupSort;
            TotalHitCount = oldTopGroups.TotalHitCount;
            TotalGroupedHitCount = oldTopGroups.TotalGroupedHitCount;
            Groups = oldTopGroups.Groups;
            MaxScore = oldTopGroups.MaxScore;
            TotalGroupCount = totalGroupCount;
        }
    }

    /// <summary>
    /// LUCENENET specific class used to nest types to mimic the syntax used 
    /// by Lucene (that is, without specifying the generic closing type of <see cref="TopGroups{TGroupValue}"/>)
    /// </summary>
    public static class TopGroups // LUCENENET specific: CA1052 Static holder types should be Static or NotInheritable
    {
        /// <summary>
        /// How the GroupDocs score (if any) should be merged. </summary>
        public enum ScoreMergeMode
        {
            /// <summary>
            /// Set score to Float.NaN
            /// </summary>
            None,

            /// <summary>
            /// Sum score across all shards for this group.
            /// </summary>
            Total,

            /// <summary>
            /// Avg score across all shards for this group.
            /// </summary>
            Avg,
        }

        /// <summary>
        /// Merges an array of TopGroups, for example obtained from the second-pass 
        /// collector across multiple shards. Each TopGroups must have been sorted by the
        /// same groupSort and docSort, and the top groups passed to all second-pass 
        /// collectors must be the same.
        /// 
        /// <b>NOTE</b>: We can't always compute an exact totalGroupCount.
        /// Documents belonging to a group may occur on more than
        /// one shard and thus the merged totalGroupCount can be
        /// higher than the actual totalGroupCount. In this case the
        /// totalGroupCount represents a upper bound. If the documents
        /// of one group do only reside in one shard then the
        /// totalGroupCount is exact.
        /// 
        /// <b>NOTE</b>: the topDocs in each GroupDocs is actually
        /// an instance of TopDocsAndShards
        /// </summary>
        public static TopGroups<T> Merge<T>(ITopGroups<T>[] shardGroups, Sort groupSort, Sort docSort, int docOffset, int docTopN, ScoreMergeMode scoreMergeMode)
        {
            //System.out.println("TopGroups.merge");

            if (shardGroups.Length == 0)
            {
                return null;
            }

            // LUCENENET specific - store whether T is value type
            // for optimization of GetHashCode() and Equals()
            bool shardGroupsIsValueType = typeof(T).IsValueType;

            int totalHitCount = 0;
            int totalGroupedHitCount = 0;
            // Optionally merge the totalGroupCount.
            int? totalGroupCount = null;

            int numGroups = shardGroups[0].Groups.Length;
            foreach (var shard in shardGroups)
            {
                if (numGroups != shard.Groups.Length)
                {
                    throw new ArgumentException("number of groups differs across shards; you must pass same top groups to all shards' second-pass collector");
                }
                totalHitCount += shard.TotalHitCount;
                totalGroupedHitCount += shard.TotalGroupedHitCount;
                if (shard.TotalGroupCount != null)
                {
                    if (totalGroupCount is null)
                    {
                        totalGroupCount = 0;
                    }

                    totalGroupCount += shard.TotalGroupCount;
                }
            }

            var mergedGroupDocs = new GroupDocs<T>[numGroups];

            TopDocs[] shardTopDocs = new TopDocs[shardGroups.Length];
            float totalMaxScore = float.Epsilon; // LUCENENET: Epsilon in .NET is the same as MIN_VALUE in Java

            for (int groupIDX = 0; groupIDX < numGroups; groupIDX++)
            {
                T groupValue = shardGroups[0].Groups[groupIDX].GroupValue;
                //System.out.println("  merge groupValue=" + groupValue + " sortValues=" + Arrays.toString(shardGroups[0].groups[groupIDX].groupSortValues));
                float maxScore = float.Epsilon; // LUCENENET: Epsilon in .NET is the same as MIN_VALUE in Java
                int totalHits = 0;
                double scoreSum = 0.0;
                for (int shardIdx = 0; shardIdx < shardGroups.Length; shardIdx++)
                {
                    //System.out.println("    shard=" + shardIDX);
                    ITopGroups<T> shard = shardGroups[shardIdx];
                    var shardGroupDocs = shard.Groups[groupIDX];
                    if (groupValue is null)
                    {
                        if (shardGroupDocs.GroupValue != null)
                        {
                            throw new ArgumentException("group values differ across shards; you must pass same top groups to all shards' second-pass collector");
                        }
                    }
                    // LUCENENET specific - use StructuralEqualityComparer.Default.Equals() if we have a reference type
                    // to ensure if it is a collection its contents are compared
                    else if (!(shardGroupsIsValueType ? groupValue.Equals(shardGroupDocs.GroupValue) : StructuralEqualityComparer.Default.Equals(groupValue, shardGroupDocs.GroupValue)))
                    {
                        throw new ArgumentException("group values differ across shards; you must pass same top groups to all shards' second-pass collector");
                    }

                    /*
                    for(ScoreDoc sd : shardGroupDocs.scoreDocs) {
                      System.out.println("      doc=" + sd.doc);
                    }
                    */

                    shardTopDocs[shardIdx] = new TopDocs(shardGroupDocs.TotalHits, shardGroupDocs.ScoreDocs, shardGroupDocs.MaxScore);
                    maxScore = Math.Max(maxScore, shardGroupDocs.MaxScore);
                    totalHits += shardGroupDocs.TotalHits;
                    scoreSum += shardGroupDocs.Score;
                }

                TopDocs mergedTopDocs = TopDocs.Merge(docSort, docOffset + docTopN, shardTopDocs);

                // Slice;
                ScoreDoc[] mergedScoreDocs;
                if (docOffset == 0)
                {
                    mergedScoreDocs = mergedTopDocs.ScoreDocs;
                }
                else if (docOffset >= mergedTopDocs.ScoreDocs.Length)
                {
                    mergedScoreDocs = Arrays.Empty<ScoreDoc>();
                }
                else
                {
                    mergedScoreDocs = new ScoreDoc[mergedTopDocs.ScoreDocs.Length - docOffset];
                    Arrays.Copy(mergedTopDocs.ScoreDocs, docOffset, mergedScoreDocs, 0, mergedTopDocs.ScoreDocs.Length - docOffset);
                }

                float groupScore;
                switch (scoreMergeMode)
                {
                    case ScoreMergeMode.None:
                        groupScore = float.NaN;
                        break;
                    case ScoreMergeMode.Avg:
                        if (totalHits > 0)
                        {
                            groupScore = (float)(scoreSum / totalHits);
                        }
                        else
                        {
                            groupScore = float.NaN;
                        }
                        break;
                    case ScoreMergeMode.Total:
                        groupScore = (float)scoreSum;
                        break;
                    default:
                        throw new ArgumentException("can't handle ScoreMergeMode " + scoreMergeMode);
                }

                //System.out.println("SHARDS=" + Arrays.toString(mergedTopDocs.shardIndex));
                mergedGroupDocs[groupIDX] = new GroupDocs<T>(groupScore, maxScore, totalHits, mergedScoreDocs, groupValue, shardGroups[0].Groups[groupIDX].GroupSortValues);
                totalMaxScore = Math.Max(totalMaxScore, maxScore);
            }

            if (totalGroupCount != null)
            {
                var result = new TopGroups<T>(groupSort.GetSort(), docSort?.GetSort(), totalHitCount, totalGroupedHitCount, mergedGroupDocs, totalMaxScore);
                return new TopGroups<T>(result, totalGroupCount);
            }

            return new TopGroups<T>(groupSort.GetSort(), docSort?.GetSort(), totalHitCount, totalGroupedHitCount, mergedGroupDocs, totalMaxScore);
        }
    }

    /// <summary>
    /// LUCENENET specific interface used to provide covariance
    /// with the TGroupValue type to simulate Java's wildcard generics.
    /// </summary>
    /// <typeparam name="TGroupValue"></typeparam>
    public interface ITopGroups<out TGroupValue>
    {
        /// <summary>
        /// Number of documents matching the search </summary>
        int TotalHitCount { get; }

        /// <summary>
        /// Number of documents grouped into the topN groups </summary>
        int TotalGroupedHitCount { get; }

        /// <summary>
        /// The total number of unique groups. If <c>null</c> this value is not computed. </summary>
        int? TotalGroupCount { get; }

        /// <summary>
        /// Group results in groupSort order </summary>
        [SuppressMessage("Performance", "CA1819:Properties should not return arrays", Justification = "Lucene's design requires some array properties")]
        IGroupDocs<TGroupValue>[] Groups { get; }

        /// <summary>
        /// How groups are sorted against each other </summary>
        [SuppressMessage("Performance", "CA1819:Properties should not return arrays", Justification = "Lucene's design requires some array properties")]
        SortField[] GroupSort { get; }

        /// <summary>
        /// How docs are sorted within each group </summary>
        [SuppressMessage("Performance", "CA1819:Properties should not return arrays", Justification = "Lucene's design requires some array properties")]
        SortField[] WithinGroupSort { get; }

        /// <summary>
        /// Highest score across all hits, or
        /// <see cref="float.NaN"/> if scores were not computed. 
        /// </summary>
        float MaxScore { get; }
    }
}