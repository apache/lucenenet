﻿using Lucene.Net.Diagnostics;
using Lucene.Net.Support;
using Lucene.Net.Util;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Lucene.Net.Search
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
    /// Represents hits returned by 
    /// <see cref="IndexSearcher.Search(Query,Filter,int)"/> and 
    /// <see cref="IndexSearcher.Search(Query,int)"/>.
    /// </summary>
    public class TopDocs
    {
        /// <summary>
        /// The total number of hits for the query. </summary>
        public int TotalHits { get; set; }

        /// <summary>
        /// The top hits for the query. </summary>
        [WritableArray]
        [SuppressMessage("Microsoft.Performance", "CA1819", Justification = "Lucene's design requires some writable array properties")]
        public ScoreDoc[] ScoreDocs { get; set; }

        /// <summary>
        /// Stores the maximum score value encountered, needed for normalizing. </summary>
        private float maxScore;

        /// <summary>
        /// Returns the maximum score value encountered. Note that in case
        /// scores are not tracked, this returns <see cref="float.NaN"/>.
        /// </summary>
        public virtual float MaxScore
        {
            get => maxScore;
            set => this.maxScore = value;
        }

        /// <summary>
        /// Constructs a <see cref="TopDocs"/> with a default <c>maxScore=System.Single.NaN</c>. </summary>
        internal TopDocs(int totalHits, ScoreDoc[] scoreDocs)
            : this(totalHits, scoreDocs, float.NaN)
        {
        }

        public TopDocs(int totalHits, ScoreDoc[] scoreDocs, float maxScore)
        {
            this.TotalHits = totalHits;
            this.ScoreDocs = scoreDocs;
            this.maxScore = maxScore;
        }

        // Refers to one hit:
        private class ShardRef
        {
            // Which shard (index into shardHits[]):
            internal int ShardIndex { get; private set; }

            // Which hit within the shard:
            internal int HitIndex { get; set; }

            public ShardRef(int shardIndex)
            {
                this.ShardIndex = shardIndex;
            }

            public override string ToString()
            {
                return "ShardRef(shardIndex=" + ShardIndex + " hitIndex=" + HitIndex + ")";
            }
        }

        // Specialized MergeSortQueue that just merges by
        // relevance score, descending:
        private class ScoreMergeSortQueue : Util.PriorityQueue<ShardRef>
        {
            internal readonly ScoreDoc[][] shardHits;

            public ScoreMergeSortQueue(TopDocs[] shardHits)
                : base(shardHits.Length)
            {
                this.shardHits = new ScoreDoc[shardHits.Length][];
                for (int shardIDX = 0; shardIDX < shardHits.Length; shardIDX++)
                {
                    this.shardHits[shardIDX] = shardHits[shardIDX].ScoreDocs;
                }
            }

            // Returns true if first is < second
            protected internal override bool LessThan(ShardRef first, ShardRef second)
            {
                if (Debugging.AssertsEnabled) Debugging.Assert(first != second);
                float firstScore = shardHits[first.ShardIndex][first.HitIndex].Score;
                float secondScore = shardHits[second.ShardIndex][second.HitIndex].Score;

                // LUCENENET specific - compare bits rather than using equality operators to prevent these comparisons from failing in x86 in .NET Framework with optimizations enabled
                if (NumericUtils.SingleToSortableInt32(firstScore) < NumericUtils.SingleToSortableInt32(secondScore))
                {
                    return false;
                }
                else if (NumericUtils.SingleToSortableInt32(firstScore) > NumericUtils.SingleToSortableInt32(secondScore))
                {
                    return true;
                }
                else
                {
                    // Tie break: earlier shard wins
                    if (first.ShardIndex < second.ShardIndex)
                    {
                        return true;
                    }
                    else if (first.ShardIndex > second.ShardIndex)
                    {
                        return false;
                    }
                    else
                    {
                        // Tie break in same shard: resolve however the
                        // shard had resolved it:
                        if (Debugging.AssertsEnabled) Debugging.Assert(first.HitIndex != second.HitIndex);
                        return first.HitIndex < second.HitIndex;
                    }
                }
            }
        }

        private class MergeSortQueue : Util.PriorityQueue<ShardRef>
        {
            // These are really FieldDoc instances:
            internal readonly ScoreDoc[][] shardHits;

            internal readonly FieldComparer[] comparers;
            internal readonly int[] reverseMul;

            public MergeSortQueue(Sort sort, TopDocs[] shardHits)
                : base(shardHits.Length)
            {
                this.shardHits = new ScoreDoc[shardHits.Length][];
                for (int shardIDX = 0; shardIDX < shardHits.Length; shardIDX++)
                {
                    ScoreDoc[] shard = shardHits[shardIDX].ScoreDocs;
                    //System.out.println("  init shardIdx=" + shardIDX + " hits=" + shard);
                    if (shard != null)
                    {
                        this.shardHits[shardIDX] = shard;
                        // Fail gracefully if API is misused:
                        for (int hitIDX = 0; hitIDX < shard.Length; hitIDX++)
                        {
                            ScoreDoc sd = shard[hitIDX];
                            if (!(sd is FieldDoc))
                            {
                                throw new ArgumentException("shard " + shardIDX + " was not sorted by the provided Sort (expected FieldDoc but got ScoreDoc)");
                            }
                            FieldDoc fd = (FieldDoc)sd;
                            if (fd.Fields is null)
                            {
                                throw new ArgumentException("shard " + shardIDX + " did not set sort field values (FieldDoc.fields is null); you must pass fillFields=true to IndexSearcher.search on each shard");
                            }
                        }
                    }
                }

                SortField[] sortFields = sort.GetSort();
                comparers = new FieldComparer[sortFields.Length];
                reverseMul = new int[sortFields.Length];
                for (int compIDX = 0; compIDX < sortFields.Length; compIDX++)
                {
                    SortField sortField = sortFields[compIDX];
                    comparers[compIDX] = sortField.GetComparer(1, compIDX);
                    reverseMul[compIDX] = sortField.IsReverse ? -1 : 1;
                }
            }

            // Returns true if first is < second
            protected internal override bool LessThan(ShardRef first, ShardRef second)
            {
                if (Debugging.AssertsEnabled) Debugging.Assert(first != second);
                FieldDoc firstFD = (FieldDoc)shardHits[first.ShardIndex][first.HitIndex];
                FieldDoc secondFD = (FieldDoc)shardHits[second.ShardIndex][second.HitIndex];
                //System.out.println("  lessThan:\n     first=" + first + " doc=" + firstFD.doc + " score=" + firstFD.score + "\n    second=" + second + " doc=" + secondFD.doc + " score=" + secondFD.score);

                for (int compIDX = 0; compIDX < comparers.Length; compIDX++)
                {
                    FieldComparer comp = comparers[compIDX];
                    //System.out.println("    cmp idx=" + compIDX + " cmp1=" + firstFD.fields[compIDX] + " cmp2=" + secondFD.fields[compIDX] + " reverse=" + reverseMul[compIDX]);

                    int cmp = reverseMul[compIDX] * comp.CompareValues(firstFD.Fields[compIDX], secondFD.Fields[compIDX]);

                    if (cmp != 0)
                    {
                        //System.out.println("    return " + (cmp < 0));
                        return cmp < 0;
                    }
                }

                // Tie break: earlier shard wins
                if (first.ShardIndex < second.ShardIndex)
                {
                    //System.out.println("    return tb true");
                    return true;
                }
                else if (first.ShardIndex > second.ShardIndex)
                {
                    //System.out.println("    return tb false");
                    return false;
                }
                else
                {
                    // Tie break in same shard: resolve however the
                    // shard had resolved it:
                    //System.out.println("    return tb " + (first.hitIndex < second.hitIndex));
                    if (Debugging.AssertsEnabled) Debugging.Assert(first.HitIndex != second.HitIndex);
                    return first.HitIndex < second.HitIndex;
                }
            }
        }

        /// <summary>
        /// Returns a new <see cref="TopDocs"/>, containing <paramref name="topN"/> results across
        /// the provided <see cref="TopDocs"/>, sorting by the specified 
        /// <see cref="Sort"/>.  Each of the <see cref="TopDocs"/> must have been sorted by
        /// the same <see cref="Sort"/>, and sort field values must have been
        /// filled (ie, <c>fillFields=true</c> must be
        /// passed to
        /// <see cref="TopFieldCollector.Create(Sort, int, bool, bool, bool, bool)"/>.
        ///
        /// <para/>Pass <paramref name="sort"/>=null to merge sort by score descending.
        /// <para/>
        /// @lucene.experimental
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static TopDocs Merge(Sort sort, int topN, TopDocs[] shardHits)
        {
            return Merge(sort, 0, topN, shardHits);
        }

        /// <summary>
        /// Same as <see cref="Merge(Sort, int, TopDocs[])"/> but also slices the result at the same time based
        /// on the provided start and size. The return <c>TopDocs</c> will always have a scoreDocs with length of 
        /// at most <see cref="Util.PriorityQueue{T}.Count"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static TopDocs Merge(Sort sort, int start, int size, TopDocs[] shardHits)
        {
            Util.PriorityQueue<ShardRef> queue;
            if (sort is null)
            {
                queue = new ScoreMergeSortQueue(shardHits);
            }
            else
            {
                queue = new MergeSortQueue(sort, shardHits);
            }

            int totalHitCount = 0;
            int availHitCount = 0;
            float maxScore = float.Epsilon; // LUCENENET: Epsilon in .NET is the same as MIN_VALUE in Java
            for (int shardIDX = 0; shardIDX < shardHits.Length; shardIDX++)
            {
                TopDocs shard = shardHits[shardIDX];
                // totalHits can be non-zero even if no hits were
                // collected, when searchAfter was used:
                totalHitCount += shard.TotalHits;
                if (shard.ScoreDocs != null && shard.ScoreDocs.Length > 0)
                {
                    availHitCount += shard.ScoreDocs.Length;
                    queue.Add(new ShardRef(shardIDX));
                    maxScore = Math.Max(maxScore, shard.MaxScore);
                    //System.out.println("  maxScore now " + maxScore + " vs " + shard.getMaxScore());
                }
            }

            if (availHitCount == 0)
            {
                maxScore = float.NaN;
            }

            ScoreDoc[] hits;
            if (availHitCount <= start)
            {
                hits = Arrays.Empty<ScoreDoc>();
            }
            else
            {
                hits = new ScoreDoc[Math.Min(size, availHitCount - start)];
                int requestedResultWindow = start + size;
                int numIterOnHits = Math.Min(availHitCount, requestedResultWindow);
                int hitUpto = 0;
                while (hitUpto < numIterOnHits)
                {
                    if (Debugging.AssertsEnabled) Debugging.Assert(queue.Count > 0);
                    ShardRef @ref = queue.Pop();
                    ScoreDoc hit = shardHits[@ref.ShardIndex].ScoreDocs[@ref.HitIndex++];
                    hit.ShardIndex = @ref.ShardIndex;
                    if (hitUpto >= start)
                    {
                        hits[hitUpto - start] = hit;
                    }

                    //System.out.println("  hitUpto=" + hitUpto);
                    //System.out.println("    doc=" + hits[hitUpto].doc + " score=" + hits[hitUpto].score);

                    hitUpto++;

                    if (@ref.HitIndex < shardHits[@ref.ShardIndex].ScoreDocs.Length)
                    {
                        // Not done with this these TopDocs yet:
                        queue.Add(@ref);
                    }
                }
            }

            if (sort is null)
            {
                return new TopDocs(totalHitCount, hits, maxScore);
            }
            else
            {
                return new TopFieldDocs(totalHitCount, hits, sort.GetSort(), maxScore);
            }
        }
    }
}