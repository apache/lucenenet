using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Search.Grouping;
using Lucene.Net.Support;
using Lucene.Net.Util;

namespace Lucene.Net.Join
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
    /// Collects parent document hits for a Query containing one more more
    ///  BlockJoinQuery clauses, sorted by the
    ///  specified parent Sort.  Note that this cannot perform
    ///  arbitrary joins; rather, it requires that all joined
    ///  documents are indexed as a doc block (using {@link
    ///  IndexWriter#addDocuments} or {@link
    ///  IndexWriter#updateDocuments}).  Ie, the join is computed
    ///  at index time.
    /// 
    ///  <p>The parent Sort must only use
    ///  fields from the parent documents; sorting by field in
    ///  the child documents is not supported.</p>
    /// 
    ///  <p>You should only use this
    ///  collector if one or more of the clauses in the query is
    ///  a <seealso cref="ToParentBlockJoinQuery"/>.  This collector will find those query
    ///  clauses and record the matching child documents for the
    ///  top scoring parent documents.</p>
    /// 
    ///  <p>Multiple joins (star join) and nested joins and a mix
    ///  of the two are allowed, as long as in all cases the
    ///  documents corresponding to a single row of each joined
    ///  parent table were indexed as a doc block.</p>
    /// 
    ///  <p>For the simple star join you can retrieve the
    ///  <seealso cref="TopGroups"/> instance containing each <seealso cref="ToParentBlockJoinQuery"/>'s
    ///  matching child documents for the top parent groups,
    ///  using <seealso cref="#getTopGroups"/>.  Ie,
    ///  a single query, which will contain two or more
    ///  <seealso cref="ToParentBlockJoinQuery"/>'s as clauses representing the star join,
    ///  can then retrieve two or more <seealso cref="TopGroups"/> instances.</p>
    /// 
    ///  <p>For nested joins, the query will run correctly (ie,
    ///  match the right parent and child documents), however,
    ///  because TopGroups is currently unable to support nesting
    ///  (each group is not able to hold another TopGroups), you
    ///  are only able to retrieve the TopGroups of the first
    ///  join.  The TopGroups of the nested joins will not be
    ///  correct.
    /// 
    ///  See <seealso cref="org.apache.lucene.search.join"/> for a code
    ///  sample.
    /// 
    /// @lucene.experimental
    /// </summary>
    public class ToParentBlockJoinCollector : Collector
    {
        private readonly Sort sort;

        // Maps each BlockJoinQuery instance to its "slot" in
        // joinScorers and in OneGroup's cached doc/scores/count:
        private readonly IDictionary<Query, int?> joinQueryID = new Dictionary<Query, int?>();
        private readonly int numParentHits;
        private readonly FieldValueHitQueue<OneGroup> queue;
        private readonly FieldComparator[] comparators;
        private readonly int[] reverseMul;
        private readonly int compEnd;
        private readonly bool trackMaxScore;
        private readonly bool trackScores;

        private int docBase;
        private ToParentBlockJoinQuery.BlockJoinScorer[] joinScorers = new ToParentBlockJoinQuery.BlockJoinScorer[0];
        private AtomicReaderContext currentReaderContext;
        private Scorer scorer;
        private bool queueFull;

        private OneGroup bottom;
        private int totalHitCount;
        private float maxScore = float.NaN;

        /// <summary>
        ///  Creates a ToParentBlockJoinCollector.  The provided sort must
        ///  not be null.  If you pass true trackScores, all
        ///  ToParentBlockQuery instances must not use
        ///  ScoreMode.None. 
        /// </summary>
        public ToParentBlockJoinCollector(Sort sort, int numParentHits, bool trackScores, bool trackMaxScore)
        {
            // TODO: allow null sort to be specialized to relevance
            // only collector
            this.sort = sort;
            this.trackMaxScore = trackMaxScore;
            if (trackMaxScore)
            {
                maxScore = float.MinValue;
            }
            //System.out.println("numParentHits=" + numParentHits);
            this.trackScores = trackScores;
            this.numParentHits = numParentHits;
            queue = FieldValueHitQueue.Create<OneGroup>(sort.GetSort(), numParentHits);
            comparators = queue.Comparators;
            reverseMul = queue.ReverseMul;
            compEnd = comparators.Length - 1;
        }

        private sealed class OneGroup : FieldValueHitQueue.Entry
        {
            public OneGroup(int comparatorSlot, int parentDoc, float parentScore, int numJoins, bool doScores) 
                : base(comparatorSlot, parentDoc, parentScore)
            {
                //System.out.println("make OneGroup parentDoc=" + parentDoc);
                docs = new int[numJoins][];
                for (int joinId = 0; joinId < numJoins; joinId++)
                {
                    docs[joinId] = new int[5];
                }
                if (doScores)
                {
                    scores = new float[numJoins][];
                    for (int joinId = 0; joinId < numJoins; joinId++)
                    {
                        scores[joinId] = new float[5];
                    }
                }
                counts = new int[numJoins];
            }
            internal AtomicReaderContext readerContext;
            internal int[][] docs;
            internal float[][] scores;
            internal int[] counts;
        }
        
        public override void Collect(int parentDoc)
        {
            //System.out.println("\nC parentDoc=" + parentDoc);
            totalHitCount++;

            float score = float.NaN;

            if (trackMaxScore)
            {
                score = scorer.Score();
                maxScore = Math.Max(maxScore, score);
            }

            // TODO: we could sweep all joinScorers here and
            // aggregate total child hit count, so we can fill this
            // in getTopGroups (we wire it to 0 now)

            if (queueFull)
            {
                //System.out.println("  queueFull");
                // Fastmatch: return if this hit is not competitive
                for (int i = 0; ; i++)
                {
                    int c = reverseMul[i] * comparators[i].CompareBottom(parentDoc);
                    if (c < 0)
                    {
                        // Definitely not competitive.
                        //System.out.println("    skip");
                        return;
                    }
                    if (c > 0)
                    {
                        // Definitely competitive.
                        break;
                    }
                    if (i == compEnd)
                    {
                        // Here c=0. If we're at the last comparator, this doc is not
                        // competitive, since docs are visited in doc Id order, which means
                        // this doc cannot compete with any other document in the queue.
                        //System.out.println("    skip");
                        return;
                    }
                }

                //System.out.println("    competes!  doc=" + (docBase + parentDoc));

                // This hit is competitive - replace bottom element in queue & adjustTop
                for (int i = 0; i < comparators.Length; i++)
                {
                    comparators[i].Copy(bottom.Slot, parentDoc);
                }
                if (!trackMaxScore && trackScores)
                {
                    score = scorer.Score();
                }
                bottom.Doc = docBase + parentDoc;
                bottom.readerContext = currentReaderContext;
                bottom.Score = score;
                CopyGroups(bottom);
                bottom = queue.UpdateTop();

                for (int i = 0; i < comparators.Length; i++)
                {
                    comparators[i].Bottom = bottom.Slot;
                }
            }
            else
            {
                // Startup transient: queue is not yet full:
                int comparatorSlot = totalHitCount - 1;

                // Copy hit into queue
                for (int i = 0; i < comparators.Length; i++)
                {
                    comparators[i].Copy(comparatorSlot, parentDoc);
                }
                //System.out.println("  startup: new OG doc=" + (docBase+parentDoc));
                if (!trackMaxScore && trackScores)
                {
                    score = scorer.Score();
                }
                OneGroup og = new OneGroup(comparatorSlot, docBase + parentDoc, score, joinScorers.Length, trackScores);
                og.readerContext = currentReaderContext;
                CopyGroups(og);
                bottom = queue.Add(og);
                queueFull = totalHitCount == numParentHits;
                if (queueFull)
                {
                    // End of startup transient: queue just filled up:
                    for (int i = 0; i < comparators.Length; i++)
                    {
                        comparators[i].Bottom = bottom.Slot;
                    }
                }
            }
        }

        // Pulls out child doc and scores for all join queries:
        private void CopyGroups(OneGroup og)
        {
            // While rare, it's possible top arrays could be too
            // short if join query had null scorer on first
            // segment(s) but then became non-null on later segments
            int numSubScorers = joinScorers.Length;
            if (og.docs.Length < numSubScorers)
            {
                // While rare, this could happen if join query had
                // null scorer on first segment(s) but then became
                // non-null on later segments
                og.docs = ArrayUtil.Grow(og.docs);
            }
            if (og.counts.Length < numSubScorers)
            {
                og.counts = ArrayUtil.Grow(og.counts);
            }
            if (trackScores && og.scores.Length < numSubScorers)
            {
                og.scores = ArrayUtil.Grow(og.scores);
            }

            //System.out.println("\ncopyGroups parentDoc=" + og.doc);
            for (int scorerIDX = 0; scorerIDX < numSubScorers; scorerIDX++)
            {
                ToParentBlockJoinQuery.BlockJoinScorer joinScorer = joinScorers[scorerIDX];
                //System.out.println("  scorer=" + joinScorer);
                if (joinScorer != null && docBase + joinScorer.ParentDoc == og.Doc)
                {
                    og.counts[scorerIDX] = joinScorer.ChildCount;
                    //System.out.println("    count=" + og.counts[scorerIDX]);
                    og.docs[scorerIDX] = joinScorer.SwapChildDocs(og.docs[scorerIDX]);
                    Debug.Assert(og.docs[scorerIDX].Length >= og.counts[scorerIDX], "length=" + og.docs[scorerIDX].Length + " vs count=" + og.counts[scorerIDX]);
                    //System.out.println("    len=" + og.docs[scorerIDX].length);
                    /*
                      for(int idx=0;idx<og.counts[scorerIDX];idx++) {
                      System.out.println("    docs[" + idx + "]=" + og.docs[scorerIDX][idx]);
                      }
                    */
                    if (trackScores)
                    {
                        //System.out.println("    copy scores");
                        og.scores[scorerIDX] = joinScorer.SwapChildScores(og.scores[scorerIDX]);
                        Debug.Assert(og.scores[scorerIDX].Length >= og.counts[scorerIDX], "length=" + og.scores[scorerIDX].Length + " vs count=" + og.counts[scorerIDX]);
                    }
                }
                else
                {
                    og.counts[scorerIDX] = 0;
                }
            }
        }
        
        public override AtomicReaderContext NextReader
        {
            set
            {
                currentReaderContext = value;
                docBase = value.DocBase;
                for (int compIDX = 0; compIDX < comparators.Length; compIDX++)
                {
                    queue.SetComparator(compIDX, comparators[compIDX].SetNextReader(value));
                }
            }
        }

        public override bool AcceptsDocsOutOfOrder()
        {
            return false;
        }

        private void Enroll(ToParentBlockJoinQuery query, ToParentBlockJoinQuery.BlockJoinScorer scorer)
        {
            scorer.TrackPendingChildHits();
            int? slot;
            if (joinQueryID.TryGetValue(query, out slot))
            {
                joinScorers[(int) slot] = scorer;
            }
            else
            {
                joinQueryID[query] = joinScorers.Length;
                //System.out.println("found JQ: " + query + " slot=" + joinScorers.length);
                ToParentBlockJoinQuery.BlockJoinScorer[] newArray = new ToParentBlockJoinQuery.BlockJoinScorer[1 + joinScorers.Length];
                Array.Copy(joinScorers, 0, newArray, 0, joinScorers.Length);
                joinScorers = newArray;
                joinScorers[joinScorers.Length - 1] = scorer;
            }
        }

        public override Scorer Scorer
        {
            set
            {
                //System.out.println("C.setScorer scorer=" + value);
                // Since we invoke .score(), and the comparators likely
                // do as well, cache it so it's only "really" computed
                // once:
                scorer = new ScoreCachingWrappingScorer(value);
                for (int compIdx = 0; compIdx < comparators.Length; compIdx++)
                {
                    comparators[compIdx].Scorer = scorer;
                }
                Arrays.Fill(joinScorers, null);

                var queue2 = new ConcurrentQueue<Scorer>();
                //System.out.println("\nqueue: add top scorer=" + value);
                queue2.Enqueue(value);
//                while ((queue.Count > 0 && (queue.Dequeue()) != null))
//                {
//                    //System.out.println("  poll: " + value + "; " + value.getWeight().getQuery());
//                    if (value is ToParentBlockJoinQuery.BlockJoinScorer)
//                    {
//                        Enroll((ToParentBlockJoinQuery)value.Weight.Query, (ToParentBlockJoinQuery.BlockJoinScorer)value);
//                    }
//
//                    foreach (Scorer.ChildScorer sub in value.Children)
//                    {
//                        //System.out.println("  add sub: " + sub.child + "; " + sub.child.getWeight().getQuery());
//                        queue.Enqueue(sub.Child);
//                    }
//                }

                while (queue2.TryDequeue(out value))
                {
                    //System.out.println("  poll: " + value + "; " + value.getWeight().getQuery());
                    if (value is ToParentBlockJoinQuery.BlockJoinScorer)
                    {
                        Enroll((ToParentBlockJoinQuery)value.Weight.Query, (ToParentBlockJoinQuery.BlockJoinScorer)value);
                    }

                    foreach (Scorer.ChildScorer sub in value.Children)
                    {
                        //System.out.println("  add sub: " + sub.child + "; " + sub.child.getWeight().getQuery());
                        queue2.Enqueue(sub.Child);
                    }
                }
            }
        }

        private OneGroup[] sortedGroups;

        private void sortQueue()
        {
            sortedGroups = new OneGroup[queue.Size()];
            for (int downTo = queue.Size() - 1; downTo >= 0; downTo--)
            {
                sortedGroups[downTo] = queue.Pop();
            }
        }

        /// <summary>
        /// Returns the TopGroups for the specified
        ///  BlockJoinQuery. The groupValue of each GroupDocs will
        ///  be the parent docID for that group.
        ///  The number of documents within each group is calculated as minimum of <code>maxDocsPerGroup</code>
        ///  and number of matched child documents for that group.
        ///  Returns null if no groups matched.
        /// </summary>
        /// <param name="query"> Search query </param>
        /// <param name="withinGroupSort"> Sort criteria within groups </param>
        /// <param name="offset"> Parent docs offset </param>
        /// <param name="maxDocsPerGroup"> Upper bound of documents per group number </param>
        /// <param name="withinGroupOffset"> Offset within each group of child docs </param>
        /// <param name="fillSortFields"> Specifies whether to add sort fields or not </param>
        /// <returns> TopGroups for specified query </returns>
        /// <exception cref="IOException"> if there is a low-level I/O error </exception>
        public virtual TopGroups<int> GetTopGroups(ToParentBlockJoinQuery query, Sort withinGroupSort, int offset, int maxDocsPerGroup, int withinGroupOffset, bool fillSortFields)
        {
            int? slot;
            if (!joinQueryID.TryGetValue(query, out slot))
            {
                if (totalHitCount == 0)
                {
                    return null;
                }
            }

            if (sortedGroups == null)
            {
                if (offset >= queue.Size())
                {
                    return null;
                }
                sortQueue();
            }
            else if (offset > sortedGroups.Length)
            {
                return null;
            }

            return AccumulateGroups(slot == null ? -1 : (int)slot, offset, maxDocsPerGroup, withinGroupOffset, withinGroupSort, fillSortFields);
        }

        /// <summary>
        ///  Accumulates groups for the BlockJoinQuery specified by its slot.
        /// </summary>
        /// <param name="slot"> Search query's slot </param>
        /// <param name="offset"> Parent docs offset </param>
        /// <param name="maxDocsPerGroup"> Upper bound of documents per group number </param>
        /// <param name="withinGroupOffset"> Offset within each group of child docs </param>
        /// <param name="withinGroupSort"> Sort criteria within groups </param>
        /// <param name="fillSortFields"> Specifies whether to add sort fields or not </param>
        /// <returns> TopGroups for the query specified by slot </returns>
        /// <exception cref="IOException"> if there is a low-level I/O error </exception>
        private TopGroups<int> AccumulateGroups(int slot, int offset, int maxDocsPerGroup, int withinGroupOffset, Sort withinGroupSort, bool fillSortFields)
        {
            var groups = new GroupDocs<int>[sortedGroups.Length - offset];
            var fakeScorer = new FakeScorer();

            int totalGroupedHitCount = 0;
            //System.out.println("slot=" + slot);

            for (int groupIdx = offset; groupIdx < sortedGroups.Length; groupIdx++)
            {
                OneGroup og = sortedGroups[groupIdx];
                int numChildDocs;
                if (slot == -1 || slot >= og.counts.Length)
                {
                    numChildDocs = 0;
                }
                else
                {
                    numChildDocs = og.counts[slot];
                }

                // Number of documents in group should be bounded to prevent redundant memory allocation
                int numDocsInGroup = Math.Max(1, Math.Min(numChildDocs, maxDocsPerGroup));
                //System.out.println("parent doc=" + og.doc + " numChildDocs=" + numChildDocs + " maxDocsPG=" + maxDocsPerGroup);

                // At this point we hold all docs w/ in each group, unsorted; we now sort them:
                Collector collector;
                if (withinGroupSort == null)
                {
                    //System.out.println("sort by score");
                    // Sort by score
                    if (!trackScores)
                    {
                        throw new ArgumentException("cannot sort by relevance within group: trackScores=false");
                    }
                    collector = TopScoreDocCollector.Create(numDocsInGroup, true);
                }
                else
                {
                    // Sort by fields
                    collector = TopFieldCollector.Create(withinGroupSort, numDocsInGroup, fillSortFields, trackScores, trackMaxScore, true);
                }

                collector.Scorer = fakeScorer;
                collector.NextReader = og.readerContext;
                for (int docIdx = 0; docIdx < numChildDocs; docIdx++)
                {
                    //System.out.println("docIDX=" + docIDX + " vs " + og.docs[slot].length);
                    int doc = og.docs[slot][docIdx];
                    fakeScorer.doc = doc;
                    if (trackScores)
                    {
                        fakeScorer._score = og.scores[slot][docIdx];
                    }
                    collector.Collect(doc);
                }
                totalGroupedHitCount += numChildDocs;

                object[] groupSortValues;

                if (fillSortFields)
                {
                    groupSortValues = new object[comparators.Length];
                    for (int sortFieldIdx = 0; sortFieldIdx < comparators.Length; sortFieldIdx++)
                    {
                        groupSortValues[sortFieldIdx] = comparators[sortFieldIdx].Value(og.Slot);
                    }
                }
                else
                {
                    groupSortValues = null;
                }

                TopDocs topDocs;
                if (withinGroupSort == null)
                {
                    var tempCollector = (TopScoreDocCollector) collector;
                    topDocs = tempCollector.TopDocs(withinGroupOffset, numDocsInGroup);
                }
                else
                {
                    var tempCollector = (TopFieldCollector) collector;
                    topDocs = tempCollector.TopDocs(withinGroupOffset, numDocsInGroup);
                }
                
                groups[groupIdx - offset] = new GroupDocs<int>(og.Score, topDocs.MaxScore, numChildDocs, topDocs.ScoreDocs, og.Doc, groupSortValues);
            }

            return new TopGroups<int>(new TopGroups<int>(sort.GetSort(), withinGroupSort == null ? null : withinGroupSort.GetSort(), 0, totalGroupedHitCount, groups, maxScore), totalHitCount);
        }

        /// <summary>
        /// Returns the TopGroups for the specified BlockJoinQuery. The groupValue of each 
        /// GroupDocs will be the parent docID for that group. The number of documents within 
        /// each group equals to the total number of matched child documents for that group.
        /// Returns null if no groups matched.
        /// </summary>
        /// <param name="query">Search query</param>
        /// <param name="withinGroupSort">Sort criteria within groups</param>
        /// <param name="offset">Parent docs offset</param>
        /// <param name="withinGroupOffset">Offset within each group of child docs</param>
        /// <param name="fillSortFields">Specifies whether to add sort fields or not</param>
        /// <returns>TopGroups for specified query</returns>
        /// <exception cref="IOException"> if there is a low-level I/O error </exception>
        public virtual TopGroups<int> GetTopGroupsWithAllChildDocs(ToParentBlockJoinQuery query, Sort withinGroupSort, int offset, int withinGroupOffset, bool fillSortFields)
        {
            return GetTopGroups(query, withinGroupSort, offset, int.MaxValue, withinGroupOffset, fillSortFields);
        }

        /// <summary>
        /// Returns the highest score across all collected parent hits, as long as
        /// <code>trackMaxScores=true</code> was passed
        /// {@link #ToParentBlockJoinCollector(Sort, int, boolean, boolean) on
        /// construction}. Else, this returns <code>Float.NaN</code>
        /// </summary>
        public virtual float MaxScore
        {
            get { return maxScore; }
        }
    }
}