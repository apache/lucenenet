/* 
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
 * (the "License"); you may not use this file except in compliance with
 * the License.  You may obtain a copy of the License at
 * 
 * http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using Lucene.Net.Util;

namespace Lucene.Net.Search
{

    /// <summary> Represents hits returned by <see cref="Searcher.Search(Query,Filter,int)" />
    /// and <see cref="Searcher.Search(Query,int)" />
    /// </summary>
    [Serializable]
    public class TopDocs
    {
        private int _totalHits;
        private ScoreDoc[] _scoreDocs;
        private float _maxScore;

        /// <summary>The total number of hits for the query.</summary>
        public int TotalHits
        {
            get { return _totalHits; }
            set { _totalHits = value; }
        }

        /// <summary>The top hits for the query. </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public ScoreDoc[] ScoreDocs
        {
            get { return _scoreDocs; }
            set { _scoreDocs = value; }
        }

        /// <summary>
        /// Gets or sets the maximum score value encountered, needed for normalizing.
        /// Note that in case scores are not tracked, this returns <see cref="float.NaN" />.
        /// </summary>
        public float MaxScore
        {
            get { return _maxScore; }
            set { _maxScore = value; }
        }

        /// <summary>Constructs a TopDocs with a default maxScore=Float.NaN. </summary>
        internal TopDocs(int totalHits, ScoreDoc[] scoreDocs)
            : this(totalHits, scoreDocs, float.NaN)
        {
        }

        /// <summary></summary>
        public TopDocs(int totalHits, ScoreDoc[] scoreDocs, float maxScore)
        {
            TotalHits = totalHits;
            ScoreDocs = scoreDocs;
            MaxScore = maxScore;
        }

        private class ShardRef
        {
            internal readonly int shardIndex;

            // Which hit within the shard:
            internal int hitIndex;

            public ShardRef(int shardIndex)
            {
                this.shardIndex = shardIndex;
            }

            public override string ToString()
            {
                return "ShardRef(shardIndex=" + shardIndex + " hitIndex=" + hitIndex + ")";
            }
        }

        private class ScoreMergeSortQueue : PriorityQueue<ShardRef>
        {
            private readonly ScoreDoc[][] shardHits;

            public ScoreMergeSortQueue(TopDocs[] shardHits)
                : base(shardHits.Length)
            {
                this.shardHits = new ScoreDoc[shardHits.Length][];
                for (var shardIDX = 0; shardIDX < shardHits.Length; shardIDX++)
                {
                    this.shardHits[shardIDX] = shardHits[shardIDX].ScoreDocs;
                }
            }

            public override bool LessThan(ShardRef first, ShardRef second)
            {
                if (first == second) throw new ArgumentException("first and second must not be equal");
                var firstScore = shardHits[first.shardIndex][first.hitIndex].Score;
                var secondScore = shardHits[second.shardIndex][second.hitIndex].Score;

                if (firstScore < secondScore)
                {
                    return false;
                }
                else if (firstScore > secondScore)
                {
                    return true;
                }
                else
                {
                    // Tie break: earlier shard wins
                    if (first.shardIndex < second.shardIndex)
                    {
                        return true;
                    }
                    else if (first.shardIndex > second.shardIndex)
                    {
                        return false;
                    }
                    else
                    {
                        // Tie break in same shard: resolve however the
                        // shard had resolved it
                        // assert first.hitIndex != second.hitIndex
                        return first.hitIndex < second.hitIndex;
                    }
                }
            }
        }

        private class MergeSortQueue : PriorityQueue<ShardRef>
        {
            internal readonly ScoreDoc[][] shardHits;
            internal FieldComparator[] comparators;
            private int[] reverseMul;

            public MergeSortQueue(Sort sort, TopDocs[] shardHits)
                : base(shardHits.Length)
            {
                this.shardHits = new ScoreDoc[shardHits.Length][];
                for (var shardIDX = 0; shardIDX < shardHits.Length; shardIDX++)
                {
                    var shard = shardHits[shardIDX].ScoreDocs;
                    if (shard != null)
                    {
                        this.shardHits[shardIDX] = shard;
                        // Fail gracefully if API is misused:
                        for (var hitIDX = 0; hitIDX < shard.Length; hitIDX++)
                        {
                            var sd = shard[hitIDX];
                            if (!(sd is FieldDoc))
                                throw new ArgumentException("shard " + shardIDX + " was not sorted by the provided Sort (expected FieldDoc but got ScoreDoc)");
                            var fd = sd as FieldDoc;
                            if (fd.fields == null)
                                throw new ArgumentException("shard " + shardIDX + " did not set sort field values (FieldDoc.fields is null); you must pass fillFields=true to IndexSearcher.search on each shard");
                        }
                    }
                }

                var sortFields = sort.GetSort();
                comparators = new FieldComparator[sortFields.Length];
                reverseMul = new int[sortFields.Length];
                for (var compIDX = 0; compIDX < sortFields.Length; compIDX++)
                {
                    var sortField = sortFields[compIDX];
                    comparators[compIDX] = sortField.GetComparator(1, compIDX);
                    reverseMul[compIDX] = sortField.Reverse ? -1 : 1;
                }
            }

            public override bool LessThan(ShardRef first, ShardRef second)
            {
                if (first == second) throw new ArgumentException("first and second must not be equal");

                var firstFD = (FieldDoc)shardHits[first.shardIndex][first.hitIndex];
                var secondFD = (FieldDoc)shardHits[second.shardIndex][second.hitIndex];

                for (var compIDX = 0; compIDX < comparators.Length; compIDX++)
                {
                    FieldComparator comp = comparators[compIDX];

                    var cmp = reverseMul[compIDX] * comp.CompareValues(firstFD.fields[compIDX], secondFD.fields[compIDX]);

                    if (cmp != 0)
                    {
                        return cmp < 0;
                    }
                }

                // Tie break: earlier shard wins
                if (first.shardIndex < second.shardIndex)
                {
                    return true;
                }
                else if (first.shardIndex > second.shardIndex)
                {
                    return false;
                }
                else
                {
                    // Tie break in same shard: resolve however the
                    // shard had resolved it:
                    //assert first.hitIndex != second.hitIndex;
                    return first.hitIndex < second.hitIndex;
                }
            }
        }

        public static TopDocs merge(Sort sort, int topN, TopDocs[] shardHits)
        {

            PriorityQueue<ShardRef> queue;
            if (sort == null)
            {
                queue = new ScoreMergeSortQueue(shardHits);
            }
            else
            {
                queue = new MergeSortQueue(sort, shardHits);
            }

            var totalHitCount = 0;
            var availHitCount = 0;
            var maxScore = float.MinValue;
            for (var shardIDX = 0; shardIDX < shardHits.Length; shardIDX++)
            {
                var shard = shardHits[shardIDX];
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
            var hits = new ScoreDoc[Math.Min(topN, availHitCount)];

            var hitUpto = 0;
            while (hitUpto < hits.Length)
            {
                //assert queue.size() > 0;
                ShardRef shardRef = queue.Pop();
                ScoreDoc hit = shardHits[shardRef.shardIndex].ScoreDocs[shardRef.hitIndex++];
                hit.ShardIndex = shardRef.shardIndex;
                hits[hitUpto] = hit;

                //System.out.println("  hitUpto=" + hitUpto);
                //System.out.println("    doc=" + hits[hitUpto].doc + " score=" + hits[hitUpto].score);

                hitUpto++;

                if (shardRef.hitIndex < shardHits[shardRef.shardIndex].ScoreDocs.Length)
                {
                    // Not done with this these TopDocs yet:
                    queue.Add(shardRef);
                }
            }

            if (sort == null)
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