using Lucene.Net.Diagnostics;
using Lucene.Net.Index;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics;

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

    // TODO: this sentence is too long for the class summary.
    /// <summary>
    /// BlockGroupingCollector performs grouping with a
    /// single pass collector, as long as you are grouping by a
    /// doc block field, ie all documents sharing a given group
    /// value were indexed as a doc block using the atomic
    /// <see cref="IndexWriter.AddDocuments(IEnumerable{IEnumerable{IIndexableField}}, Analysis.Analyzer)"/> or
    /// <see cref="IndexWriter.UpdateDocuments(Term, IEnumerable{IEnumerable{IIndexableField}}, Analysis.Analyzer)"/>
    /// API.
    /// 
    /// <para>
    /// This results in faster performance (~25% faster QPS)
    /// than the two-pass grouping collectors, with the tradeoff
    /// being that the documents in each group must always be
    /// indexed as a block.  This collector also fills in
    /// TopGroups.totalGroupCount without requiring the separate
    /// <see cref="Terms.TermAllGroupsCollector"/>. However, this collector does
    /// not fill in the groupValue of each group; this field
    /// will always be null.
    /// </para>
    /// <para>
    /// <c>NOTE</c>: this collector makes no effort to verify
    /// the docs were in fact indexed as a block, so it's up to
    /// you to ensure this was the case.
    /// </para>
    /// <para>
    /// See <a href="https://github.com/apache/lucene-solr/blob/releases/lucene-solr/4.8.0/lucene/grouping/src/java/org/apache/lucene/search/grouping/package.html">org.apache.lucene.search.grouping</a> for more
    /// details including a full code example.
    /// </para>
    /// @lucene.experimental
    /// </summary>
    public class BlockGroupingCollector : ICollector
    {
        private int[] pendingSubDocs;
        private float[] pendingSubScores;
        private int subDocUpto;

        private readonly Sort groupSort;
        private readonly int topNGroups;
        private readonly Filter lastDocPerGroup;

        // TODO: specialize into 2 classes, static "create" method:
        private readonly bool needsScores;

        private readonly FieldComparer[] comparers;
        private readonly int[] reversed;
        private readonly int compIDXEnd;
        private int bottomSlot;
        private bool queueFull;
        private AtomicReaderContext currentReaderContext;

        private int topGroupDoc;
        private int totalHitCount;
        private int totalGroupCount;
        private int docBase;
        private int groupEndDocID;
        private DocIdSetIterator lastDocPerGroupBits;
        private Scorer scorer;
        private readonly GroupQueue groupQueue;
        private bool groupCompetes;

        private sealed class FakeScorer : Scorer
        {
            internal float score;
            internal int doc;

            public FakeScorer()
                    : base(null)
            {
            }

            public override float GetScore()
            {
                return score;
            }

            public override int Freq => throw UnsupportedOperationException.Create(); // TODO: wtf does this class do?

            public override int DocID => doc;

            public override int Advance(int target)
            {
                throw UnsupportedOperationException.Create();
            }

            public override int NextDoc()
            {
                throw UnsupportedOperationException.Create();
            }

            public override long GetCost()
            {
                return 1;
            }

            public override Weight Weight => throw UnsupportedOperationException.Create();

            public override ICollection<ChildScorer> GetChildren()
            {
                throw UnsupportedOperationException.Create();
            }
        }

        private sealed class OneGroup
        {
            internal AtomicReaderContext readerContext;
            //internal int groupOrd;
            internal int topGroupDoc;
            internal int[] docs;
            internal float[] scores;
            internal int count;
            internal int comparerSlot;
        }

        // Sorts by groupSort.  Not static -- uses comparers, reversed
        private sealed class GroupQueue : PriorityQueue<OneGroup>
        {

            private readonly BlockGroupingCollector outerInstance;
            public GroupQueue(BlockGroupingCollector outerInstance, int size)
                        : base(size)
            {
                this.outerInstance = outerInstance;
            }

            protected internal override bool LessThan(OneGroup group1, OneGroup group2)
            {

                //System.out.println("    ltcheck");
                if (Debugging.AssertsEnabled)
                {
                    Debugging.Assert(group1 != group2);
                    Debugging.Assert(group1.comparerSlot != group2.comparerSlot);
                }

                int numComparers = outerInstance.comparers.Length;
                for (int compIDX = 0; compIDX < numComparers; compIDX++)
                {
                    int c = outerInstance.reversed[compIDX] * outerInstance.comparers[compIDX].Compare(group1.comparerSlot, group2.comparerSlot);
                    if (c != 0)
                    {
                        // Short circuit
                        return c > 0;
                    }
                }

                // Break ties by docID; lower docID is always sorted first
                return group1.topGroupDoc > group2.topGroupDoc;
            }
        }

        // Called when we transition to another group; if the
        // group is competitive we insert into the group queue
        private void ProcessGroup()
        {
            totalGroupCount++;
            //System.out.println("    processGroup ord=" + lastGroupOrd + " competes=" + groupCompetes + " count=" + subDocUpto + " groupDoc=" + topGroupDoc);
            if (groupCompetes)
            {
                if (!queueFull)
                {
                    // Startup transient: always add a new OneGroup
                    OneGroup og = new OneGroup();
                    og.count = subDocUpto;
                    og.topGroupDoc = docBase + topGroupDoc;
                    og.docs = pendingSubDocs;
                    pendingSubDocs = new int[10];
                    if (needsScores)
                    {
                        og.scores = pendingSubScores;
                        pendingSubScores = new float[10];
                    }
                    og.readerContext = currentReaderContext;
                    //og.groupOrd = lastGroupOrd;
                    og.comparerSlot = bottomSlot;
                    OneGroup bottomGroup = groupQueue.Add(og);
                    //System.out.println("      ADD group=" + getGroupString(lastGroupOrd) + " newBottom=" + getGroupString(bottomGroup.groupOrd));
                    queueFull = groupQueue.Count == topNGroups;
                    if (queueFull)
                    {
                        // Queue just became full; now set the real bottom
                        // in the comparers:
                        bottomSlot = bottomGroup.comparerSlot;
                        //System.out.println("    set bottom=" + bottomSlot);
                        for (int i = 0; i < comparers.Length; i++)
                        {
                            comparers[i].SetBottom(bottomSlot);
                        }
                        //System.out.println("     QUEUE FULL");
                    }
                    else
                    {
                        // Queue not full yet -- just advance bottomSlot:
                        bottomSlot = groupQueue.Count;
                    }
                }
                else
                {
                    // Replace bottom element in PQ and then updateTop
                    OneGroup og = groupQueue.Top;
                    if (Debugging.AssertsEnabled) Debugging.Assert(og != null);
                    og.count = subDocUpto;
                    og.topGroupDoc = docBase + topGroupDoc;
                    // Swap pending docs
                    int[] savDocs = og.docs;
                    og.docs = pendingSubDocs;
                    pendingSubDocs = savDocs;
                    if (needsScores)
                    {
                        // Swap pending scores
                        float[] savScores = og.scores;
                        og.scores = pendingSubScores;
                        pendingSubScores = savScores;
                    }
                    og.readerContext = currentReaderContext;
                    //og.groupOrd = lastGroupOrd;
                    bottomSlot = groupQueue.UpdateTop().comparerSlot;

                    //System.out.println("    set bottom=" + bottomSlot);
                    for (int i = 0; i < comparers.Length; i++)
                    {
                        comparers[i].SetBottom(bottomSlot);
                    }
                }
            }
            subDocUpto = 0;
        }

        /// <summary>
        /// Create the single pass collector.
        /// </summary>
        /// <param name="groupSort">
        /// The <see cref="Sort"/> used to sort the
        /// groups.  The top sorted document within each group
        /// according to groupSort, determines how that group
        /// sorts against other groups.  This must be non-null,
        /// ie, if you want to groupSort by relevance use
        /// <see cref="Sort.RELEVANCE"/>.
        /// </param>
        /// <param name="topNGroups">How many top groups to keep.</param>
        /// <param name="needsScores">
        /// true if the collected documents
        /// require scores, either because relevance is included
        /// in the withinGroupSort or because you plan to pass true
        /// for either GetScores or GetMaxScores to <see cref="GetTopGroups(Sort, int, int, int, bool)"/>
        /// </param>
        /// <param name="lastDocPerGroup">
        /// a <see cref="Filter"/> that marks the
        /// last document in each group.
        /// </param>
        public BlockGroupingCollector(Sort groupSort, int topNGroups, bool needsScores, Filter lastDocPerGroup)
        {

            if (topNGroups < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(topNGroups), "topNGroups must be >= 1 (got " + topNGroups + ")"); // LUCENENET specific - changed from IllegalArgumentException to ArgumentOutOfRangeException (.NET convention)
            }

            groupQueue = new GroupQueue(this, topNGroups);
            pendingSubDocs = new int[10];
            if (needsScores)
            {
                pendingSubScores = new float[10];
            }

            this.needsScores = needsScores;
            this.lastDocPerGroup = lastDocPerGroup;
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
                comparers[i] = sortField.GetComparer(topNGroups, i);
                reversed[i] = sortField.IsReverse ? -1 : 1;
            }
        }

        // TODO: maybe allow no sort on retrieving groups?  app
        // may want to simply process docs in the group itself?
        // typically they will be presented as a "single" result
        // in the UI?

        /// <summary>
        /// Returns the grouped results.  Returns null if the
        /// number of groups collected is &lt;= groupOffset.
        /// 
        /// <para>
        /// <b>NOTE</b>: This collector is unable to compute
        /// the groupValue per group so it will always be null.
        /// This is normally not a problem, as you can obtain the
        /// value just like you obtain other values for each
        /// matching document (eg, via stored fields, via
        /// FieldCache, etc.)
        /// </para>
        /// </summary>
        /// <param name="withinGroupSort">
        /// The <see cref="Sort"/> used to sort
        /// documents within each group.  Passing null is
        /// allowed, to sort by relevance.
        /// </param>
        /// <param name="groupOffset">Which group to start from</param>
        /// <param name="withinGroupOffset">
        /// Which document to start from within each group
        /// </param>
        /// <param name="maxDocsPerGroup">
        /// How many top documents to keep within each group.
        /// </param>
        /// <param name="fillSortFields">
        /// If true then the Comparable values for the sort fields will be set
        /// </param>
        public virtual ITopGroups<object> GetTopGroups(Sort withinGroupSort, int groupOffset, int withinGroupOffset, int maxDocsPerGroup, bool fillSortFields)
        {
            return GetTopGroups<object>(withinGroupSort, groupOffset, withinGroupOffset, maxDocsPerGroup, fillSortFields);
        }

        /// <summary>
        /// Returns the grouped results.  Returns null if the
        /// number of groups collected is &lt;= groupOffset.
        /// 
        /// <para>
        /// <b>NOTE</b>: This collector is unable to compute
        /// the groupValue per group so it will always be null.
        /// This is normally not a problem, as you can obtain the
        /// value just like you obtain other values for each
        /// matching document (eg, via stored fields, via
        /// FieldCache, etc.)
        /// </para>
        /// </summary>
        /// <typeparam name="TGroupValue">The expected return type for group value</typeparam>
        /// <param name="withinGroupSort">
        /// The <see cref="Sort"/> used to sort
        /// documents within each group.  Passing null is
        /// allowed, to sort by relevance.
        /// </param>
        /// <param name="groupOffset">Which group to start from</param>
        /// <param name="withinGroupOffset">
        /// Which document to start from within each group
        /// </param>
        /// <param name="maxDocsPerGroup">
        /// How many top documents to keep within each group.
        /// </param>
        /// <param name="fillSortFields">
        /// If true then the Comparable values for the sort fields will be set
        /// </param>
        public virtual ITopGroups<TGroupValue> GetTopGroups<TGroupValue>(Sort withinGroupSort, int groupOffset, int withinGroupOffset, int maxDocsPerGroup, bool fillSortFields)
        {

            //if (queueFull) {
            //System.out.println("getTopGroups groupOffset=" + groupOffset + " topNGroups=" + topNGroups);
            //}
            if (subDocUpto != 0)
            {
                ProcessGroup();
            }
            if (groupOffset >= groupQueue.Count)
            {
                return null;
            }
            int totalGroupedHitCount = 0;

            FakeScorer fakeScorer = new FakeScorer();

            float maxScore = float.Epsilon; // LUCENENET: Epsilon in .NET is the same as MIN_VALUE in Java

            GroupDocs<TGroupValue>[] groups = new GroupDocs<TGroupValue>[groupQueue.Count - groupOffset];
            for (int downTo = groupQueue.Count - groupOffset - 1; downTo >= 0; downTo--)
            {
                OneGroup og = groupQueue.Pop();

                // At this point we hold all docs w/ in each group,
                // unsorted; we now sort them:
                ITopDocsCollector collector;
                if (withinGroupSort is null)
                {
                    // Sort by score
                    if (!needsScores)
                    {
                        throw new ArgumentException("cannot sort by relevance within group: needsScores=false");
                    }
                    collector = TopScoreDocCollector.Create(maxDocsPerGroup, true);
                }
                else
                {
                    // Sort by fields
                    collector = TopFieldCollector.Create(withinGroupSort, maxDocsPerGroup, fillSortFields, needsScores, needsScores, true);
                }

                collector.SetScorer(fakeScorer);
                collector.SetNextReader(og.readerContext);
                for (int docIDX = 0; docIDX < og.count; docIDX++)
                {
                    int doc = og.docs[docIDX];
                    fakeScorer.doc = doc;
                    if (needsScores)
                    {
                        fakeScorer.score = og.scores[docIDX];
                    }
                    collector.Collect(doc);
                }
                totalGroupedHitCount += og.count;

                object[] groupSortValues;

                if (fillSortFields)
                {
                    groupSortValues = new object[comparers.Length];
                    for (int sortFieldIDX = 0; sortFieldIDX < comparers.Length; sortFieldIDX++)
                    {
                        groupSortValues[sortFieldIDX] = comparers[sortFieldIDX].GetValue(og.comparerSlot);
                    }
                }
                else
                {
                    groupSortValues = null;
                }

                TopDocs topDocs = collector.GetTopDocs(withinGroupOffset, maxDocsPerGroup);

                // TODO: we could aggregate scores across children
                // by Sum/Avg instead of passing NaN:
                groups[downTo] = new GroupDocs<TGroupValue>(float.NaN,
                                                       topDocs.MaxScore,
                                                       og.count,
                                                       topDocs.ScoreDocs,
                                                       default,
                                                       groupSortValues);
                maxScore = Math.Max(maxScore, topDocs.MaxScore);
            }

            /*
            while (groupQueue.size() != 0) {
              final OneGroup og = groupQueue.pop();
              //System.out.println("  leftover: og ord=" + og.groupOrd + " count=" + og.count);
              totalGroupedHitCount += og.count;
            }
            */

            return new TopGroups<TGroupValue>(new TopGroups<TGroupValue>(groupSort.GetSort(),
                                               withinGroupSort?.GetSort(),
                                               totalHitCount, totalGroupedHitCount, groups, maxScore),
                                 totalGroupCount);
        }

        public virtual void SetScorer(Scorer scorer)
        {
            this.scorer = scorer;
            foreach (FieldComparer comparer in comparers)
            {
                comparer.SetScorer(scorer);
            }
        }

        public virtual void Collect(int doc)
        {

            // System.out.println("C " + doc);

            if (doc > groupEndDocID)
            {
                // Group changed
                if (subDocUpto != 0)
                {
                    ProcessGroup();
                }
                groupEndDocID = lastDocPerGroupBits.Advance(doc);
                //System.out.println("  adv " + groupEndDocID + " " + lastDocPerGroupBits);
                subDocUpto = 0;
                groupCompetes = !queueFull;
            }

            totalHitCount++;

            // Always cache doc/score within this group:
            if (subDocUpto == pendingSubDocs.Length)
            {
                pendingSubDocs = ArrayUtil.Grow(pendingSubDocs);
            }
            pendingSubDocs[subDocUpto] = doc;
            if (needsScores)
            {
                if (subDocUpto == pendingSubScores.Length)
                {
                    pendingSubScores = ArrayUtil.Grow(pendingSubScores);
                }
                pendingSubScores[subDocUpto] = scorer.GetScore();
            }
            subDocUpto++;

            if (groupCompetes)
            {
                if (subDocUpto == 1)
                {
                    if (Debugging.AssertsEnabled) Debugging.Assert(!queueFull);

                    //System.out.println("    init copy to bottomSlot=" + bottomSlot);
                    foreach (FieldComparer fc in comparers)
                    {
                        fc.Copy(bottomSlot, doc);
                        fc.SetBottom(bottomSlot);
                    }
                    topGroupDoc = doc;
                }
                else
                {
                    // Compare to bottomSlot
                    for (int compIDX = 0; ; compIDX++)
                    {
                        int c = reversed[compIDX] * comparers[compIDX].CompareBottom(doc);
                        if (c < 0)
                        {
                            // Definitely not competitive -- done
                            return;
                        }
                        else if (c > 0)
                        {
                            // Definitely competitive.
                            break;
                        }
                        else if (compIDX == compIDXEnd)
                        {
                            // Ties with bottom, except we know this docID is
                            // > docID in the queue (docs are visited in
                            // order), so not competitive:
                            return;
                        }
                    }

                    //System.out.println("       best w/in group!");

                    foreach (FieldComparer fc in comparers)
                    {
                        fc.Copy(bottomSlot, doc);
                        // Necessary because some comparers cache
                        // details of bottom slot; this forces them to
                        // re-cache:
                        fc.SetBottom(bottomSlot);
                    }
                    topGroupDoc = doc;
                }
            }
            else
            {
                // We're not sure this group will make it into the
                // queue yet
                for (int compIDX = 0; ; compIDX++)
                {
                    int c = reversed[compIDX] * comparers[compIDX].CompareBottom(doc);
                    if (c < 0)
                    {
                        // Definitely not competitive -- done
                        //System.out.println("    doc doesn't compete w/ top groups");
                        return;
                    }
                    else if (c > 0)
                    {
                        // Definitely competitive.
                        break;
                    }
                    else if (compIDX == compIDXEnd)
                    {
                        // Ties with bottom, except we know this docID is
                        // > docID in the queue (docs are visited in
                        // order), so not competitive:
                        //System.out.println("    doc doesn't compete w/ top groups");
                        return;
                    }
                }
                groupCompetes = true;
                foreach (FieldComparer fc in comparers)
                {
                    fc.Copy(bottomSlot, doc);
                    // Necessary because some comparers cache
                    // details of bottom slot; this forces them to
                    // re-cache:
                    fc.SetBottom(bottomSlot);
                }
                topGroupDoc = doc;
                //System.out.println("        doc competes w/ top groups");
            }
        }

        public virtual bool AcceptsDocsOutOfOrder => false;

        public virtual void SetNextReader(AtomicReaderContext context)
        {
            if (subDocUpto != 0)
            {
                ProcessGroup();
            }
            subDocUpto = 0;
            docBase = context.DocBase;
            //System.out.println("setNextReader base=" + docBase + " r=" + readerContext.reader);
            lastDocPerGroupBits = lastDocPerGroup.GetDocIdSet(context, context.AtomicReader.LiveDocs).GetIterator();
            groupEndDocID = -1;

            currentReaderContext = context;
            for (int i = 0; i < comparers.Length; i++)
            {
                comparers[i] = comparers[i].SetNextReader(context);
            }
        }
    }
}
