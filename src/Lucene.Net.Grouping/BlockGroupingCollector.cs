using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lucene.Net.Search.Grouping
{
    // TODO: this sentence is too long for the class summary.
    /// <summary>
    /// BlockGroupingCollector performs grouping with a
    /// single pass collector, as long as you are grouping by a
    /// doc block field, ie all documents sharing a given group
    /// value were indexed as a doc block using the atomic
    /// <see cref="IndexWriter.AddDocuments(IEnumerable{IEnumerable{IndexableField}}, Analysis.Analyzer)"/> or
    /// <see cref="IndexWriter.UpdateDocuments(Term, IEnumerable{IEnumerable{IndexableField}}, Analysis.Analyzer)"/>
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
    /// See {@link org.apache.lucene.search.grouping} for more
    /// details including a full code example.
    /// </para>
    /// @lucene.experimental
    /// </summary>
    public class BlockGroupingCollector : Collector
    {
        private int[] pendingSubDocs;
        private float[] pendingSubScores;
        private int subDocUpto;

        private readonly Sort groupSort;
        private readonly int topNGroups;
        private readonly Filter lastDocPerGroup;

        // TODO: specialize into 2 classes, static "create" method:
        private readonly bool needsScores;

        private readonly FieldComparator[] comparators;
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

            public override float Score()
            {
                return score;
            }

            public override int Freq()
            {
                throw new InvalidOperationException(); // TODO: wtf does this class do?
            }

            public override int DocID()
            {
                return doc;
            }

            public override int Advance(int target)
            {
                throw new InvalidOperationException();
            }

            public override int NextDoc()
            {
                throw new InvalidOperationException();
            }

            public override long Cost()
            {
                return 1;
            }

            public override Weight Weight
            {
                get
                {
                    throw new InvalidOperationException();
                }
            }


            public override ICollection<ChildScorer> Children
            {
                get
                {
                    throw new InvalidOperationException();
                }
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
            internal int comparatorSlot;
        }

        // Sorts by groupSort.  Not static -- uses comparators, reversed
        private sealed class GroupQueue : PriorityQueue<OneGroup>
        {

            private readonly BlockGroupingCollector outerInstance;
            public GroupQueue(BlockGroupingCollector outerInstance, int size)
                        : base(size)
            {
                this.outerInstance = outerInstance;
            }

            public override bool LessThan(OneGroup group1, OneGroup group2)
            {

                //System.out.println("    ltcheck");
                Debug.Assert(group1 != group2);
                Debug.Assert(group1.comparatorSlot != group2.comparatorSlot);

                int numComparators = outerInstance.comparators.Length;
                for (int compIDX = 0; compIDX < numComparators; compIDX++)
                {
                    int c = outerInstance.reversed[compIDX] * outerInstance.comparators[compIDX].Compare(group1.comparatorSlot, group2.comparatorSlot);
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
                    og.comparatorSlot = bottomSlot;
                    OneGroup bottomGroup = groupQueue.Add(og);
                    //System.out.println("      ADD group=" + getGroupString(lastGroupOrd) + " newBottom=" + getGroupString(bottomGroup.groupOrd));
                    queueFull = groupQueue.Size() == topNGroups;
                    if (queueFull)
                    {
                        // Queue just became full; now set the real bottom
                        // in the comparators:
                        bottomSlot = bottomGroup.comparatorSlot;
                        //System.out.println("    set bottom=" + bottomSlot);
                        for (int i = 0; i < comparators.Length; i++)
                        {
                            comparators[i].Bottom = bottomSlot;
                        }
                        //System.out.println("     QUEUE FULL");
                    }
                    else
                    {
                        // Queue not full yet -- just advance bottomSlot:
                        bottomSlot = groupQueue.Size();
                    }
                }
                else
                {
                    // Replace bottom element in PQ and then updateTop
                    OneGroup og = groupQueue.Top();
                    Debug.Assert(og != null);
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
                    bottomSlot = groupQueue.UpdateTop().comparatorSlot;

                    //System.out.println("    set bottom=" + bottomSlot);
                    for (int i = 0; i < comparators.Length; i++)
                    {
                        comparators[i].Bottom = bottomSlot;
                    }
                }
            }
            subDocUpto = 0;
        }

        /**
         * Create the single pass collector.
         *
         *  @param groupSort The {@link Sort} used to sort the
         *    groups.  The top sorted document within each group
         *    according to groupSort, determines how that group
         *    sorts against other groups.  This must be non-null,
         *    ie, if you want to groupSort by relevance use
         *    Sort.RELEVANCE.
         *  @param topNGroups How many top groups to keep.
         *  @param needsScores true if the collected documents
         *    require scores, either because relevance is included
         *    in the withinGroupSort or because you plan to pass true
         *    for either getSscores or getMaxScores to {@link
         *    #getTopGroups}
         *  @param lastDocPerGroup a {@link Filter} that marks the
         *    last document in each group.
         */
        public BlockGroupingCollector(Sort groupSort, int topNGroups, bool needsScores, Filter lastDocPerGroup)
        {

            if (topNGroups < 1)
            {
                throw new ArgumentException("topNGroups must be >= 1 (got " + topNGroups + ")");
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
            comparators = new FieldComparator[sortFields.Length];
            compIDXEnd = comparators.Length - 1;
            reversed = new int[sortFields.Length];
            for (int i = 0; i < sortFields.Length; i++)
            {
                SortField sortField = sortFields[i];
                comparators[i] = sortField.GetComparator(topNGroups, i);
                reversed[i] = sortField.Reverse ? -1 : 1;
            }
        }

        // TODO: maybe allow no sort on retrieving groups?  app
        // may want to simply process docs in the group itself?
        // typically they will be presented as a "single" result
        // in the UI?

        /** Returns the grouped results.  Returns null if the
         *  number of groups collected is <= groupOffset.
         *
         *  <p><b>NOTE</b>: This collector is unable to compute
         *  the groupValue per group so it will always be null.
         *  This is normally not a problem, as you can obtain the
         *  value just like you obtain other values for each
         *  matching document (eg, via stored fields, via
         *  FieldCache, etc.)
         *
         *  @param withinGroupSort The {@link Sort} used to sort
         *    documents within each group.  Passing null is
         *    allowed, to sort by relevance.
         *  @param groupOffset Which group to start from
         *  @param withinGroupOffset Which document to start from
         *    within each group
         *  @param maxDocsPerGroup How many top documents to keep
         *     within each group.
         *  @param fillSortFields If true then the Comparable
         *     values for the sort fields will be set
         */
        public TopGroups<T> GetTopGroups<T>(Sort withinGroupSort, int groupOffset, int withinGroupOffset, int maxDocsPerGroup, bool fillSortFields)
        {

            //if (queueFull) {
            //System.out.println("getTopGroups groupOffset=" + groupOffset + " topNGroups=" + topNGroups);
            //}
            if (subDocUpto != 0)
            {
                ProcessGroup();
            }
            if (groupOffset >= groupQueue.Size())
            {
                return null;
            }
            int totalGroupedHitCount = 0;

            FakeScorer fakeScorer = new FakeScorer();

            float maxScore = float.MinValue;

            GroupDocs<T>[] groups = new GroupDocs<T>[groupQueue.Size() - groupOffset];
            for (int downTo = groupQueue.Size() - groupOffset - 1; downTo >= 0; downTo--)
            {
                OneGroup og = groupQueue.Pop();

                // At this point we hold all docs w/ in each group,
                // unsorted; we now sort them:
                ITopDocsCollector collector;
                if (withinGroupSort == null)
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

                collector.Scorer = fakeScorer;
                collector.NextReader = og.readerContext;
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
                    groupSortValues = new IComparable[comparators.Length];
                    for (int sortFieldIDX = 0; sortFieldIDX < comparators.Length; sortFieldIDX++)
                    {
                        groupSortValues[sortFieldIDX] = comparators[sortFieldIDX].Value(og.comparatorSlot);
                    }
                }
                else
                {
                    groupSortValues = null;
                }

                TopDocs topDocs = collector.TopDocs(withinGroupOffset, maxDocsPerGroup);

                // TODO: we could aggregate scores across children
                // by Sum/Avg instead of passing NaN:
                groups[downTo] = new GroupDocs<T>(float.NaN,
                                                       topDocs.MaxScore,
                                                       og.count,
                                                       topDocs.ScoreDocs,
                                                       default(T),
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

            return new TopGroups<T>(new TopGroups<T>(groupSort.GetSort(),
                                               withinGroupSort == null ? null : withinGroupSort.GetSort(),
                                               totalHitCount, totalGroupedHitCount, groups, maxScore),
                                 totalGroupCount);
        }

        public override Scorer Scorer
        {
            set
            {
                this.scorer = value;
                foreach (FieldComparator comparator in comparators)
                {
                    comparator.Scorer = value;
                }
            }
        }

        public override void Collect(int doc)
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
                pendingSubScores[subDocUpto] = scorer.Score();
            }
            subDocUpto++;

            if (groupCompetes)
            {
                if (subDocUpto == 1)
                {
                    Debug.Assert(!queueFull);

                    //System.out.println("    init copy to bottomSlot=" + bottomSlot);
                    foreach (FieldComparator fc in comparators)
                    {
                        fc.Copy(bottomSlot, doc);
                        fc.Bottom = bottomSlot;
                    }
                    topGroupDoc = doc;
                }
                else
                {
                    // Compare to bottomSlot
                    for (int compIDX = 0; ; compIDX++)
                    {
                        int c = reversed[compIDX] * comparators[compIDX].CompareBottom(doc);
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

                    foreach (FieldComparator fc in comparators)
                    {
                        fc.Copy(bottomSlot, doc);
                        // Necessary because some comparators cache
                        // details of bottom slot; this forces them to
                        // re-cache:
                        fc.Bottom = bottomSlot;
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
                    int c = reversed[compIDX] * comparators[compIDX].CompareBottom(doc);
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
                foreach (FieldComparator fc in comparators)
                {
                    fc.Copy(bottomSlot, doc);
                    // Necessary because some comparators cache
                    // details of bottom slot; this forces them to
                    // re-cache:
                    fc.Bottom = bottomSlot;
                }
                topGroupDoc = doc;
                //System.out.println("        doc competes w/ top groups");
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
                if (subDocUpto != 0)
                {
                    ProcessGroup();
                }
                subDocUpto = 0;
                docBase = value.DocBase;
                //System.out.println("setNextReader base=" + docBase + " r=" + readerContext.reader);
                lastDocPerGroupBits = lastDocPerGroup.GetDocIdSet(value, value.AtomicReader.LiveDocs).GetIterator();
                groupEndDocID = -1;

                currentReaderContext = value;
                for (int i = 0; i < comparators.Length; i++)
                {
                    comparators[i] = comparators[i].SetNextReader(value);
                }

            }
        }
    }
}
