using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Support;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lucene.Net.Search.Grouping.Terms
{
    /// <summary>
    /// A base implementation of <see cref="AbstractAllGroupHeadsCollector{GH}"/> for retrieving the most relevant groups when grouping
    /// on a string based group field. More specifically this all concrete implementations of this base implementation
    /// use <see cref="Index.SortedDocValues"/>.
    /// 
    /// @lucene.experimental
    /// </summary>
    /// <typeparam name="GH"></typeparam>
    public abstract class TermAllGroupHeadsCollector<GH> : AbstractAllGroupHeadsCollector<GH>
        where GH : AbstractAllGroupHeadsCollector_GroupHead
    {
        internal readonly string groupField;
        internal readonly BytesRef scratchBytesRef = new BytesRef();

        internal SortedDocValues groupIndex;
        internal AtomicReaderContext readerContext;

        protected TermAllGroupHeadsCollector(string groupField, int numberOfSorts)
            : base(numberOfSorts)
        {
            this.groupField = groupField;
        }
    }

    public class TermAllGroupHeadsCollector
    {
        private static readonly int DEFAULT_INITIAL_SIZE = 128;

        // Disallow creation
        private TermAllGroupHeadsCollector() { }

        /**
         * Creates an <code>AbstractAllGroupHeadsCollector</code> instance based on the supplied arguments.
         * This factory method decides with implementation is best suited.
         *
         * Delegates to {@link #create(String, org.apache.lucene.search.Sort, int)} with an initialSize of 128.
         *
         * @param groupField      The field to group by
         * @param sortWithinGroup The sort within each group
         * @return an <code>AbstractAllGroupHeadsCollector</code> instance based on the supplied arguments
         */
        public static AbstractAllGroupHeadsCollector Create(string groupField, Sort sortWithinGroup)
        {
            return Create(groupField, sortWithinGroup, DEFAULT_INITIAL_SIZE);
        }

        /**
         * Creates an <code>AbstractAllGroupHeadsCollector</code> instance based on the supplied arguments.
         * This factory method decides with implementation is best suited.
         *
         * @param groupField      The field to group by
         * @param sortWithinGroup The sort within each group
         * @param initialSize The initial allocation size of the internal int set and group list which should roughly match
         *                    the total number of expected unique groups. Be aware that the heap usage is
         *                    4 bytes * initialSize.
         * @return an <code>AbstractAllGroupHeadsCollector</code> instance based on the supplied arguments
         */
        public static AbstractAllGroupHeadsCollector Create(string groupField, Sort sortWithinGroup, int initialSize)
        {
            bool sortAllScore = true;
            bool sortAllFieldValue = true;

            foreach (SortField sortField in sortWithinGroup.GetSort())
            {
                if (sortField.Type == SortField.Type_e.SCORE)
                {
                    sortAllFieldValue = false;
                }
                else if (NeedGeneralImpl(sortField))
                {
                    return new GeneralAllGroupHeadsCollector(groupField, sortWithinGroup);
                }
                else
                {
                    sortAllScore = false;
                }
            }

            if (sortAllScore)
            {
                return new ScoreAllGroupHeadsCollector(groupField, sortWithinGroup, initialSize);
            }
            else if (sortAllFieldValue)
            {
                return new OrdAllGroupHeadsCollector(groupField, sortWithinGroup, initialSize);
            }
            else
            {
                return new OrdScoreAllGroupHeadsCollector(groupField, sortWithinGroup, initialSize);
            }
        }

        // Returns when a sort field needs the general impl.
        private static bool NeedGeneralImpl(SortField sortField)
        {
            SortField.Type_e sortType = sortField.Type;
            // Note (MvG): We can also make an optimized impl when sorting is SortField.DOC
            return sortType != SortField.Type_e.STRING_VAL && sortType != SortField.Type_e.STRING && sortType != SortField.Type_e.SCORE;
        }
    }

    // A general impl that works for any group sort.
    internal class GeneralAllGroupHeadsCollector : TermAllGroupHeadsCollector<GeneralAllGroupHeadsCollector.GroupHead>
    {

        private readonly Sort sortWithinGroup;
        private readonly IDictionary<BytesRef, GroupHead> groups;

        internal Scorer scorer;

        internal GeneralAllGroupHeadsCollector(string groupField, Sort sortWithinGroup)
            : base(groupField, sortWithinGroup.GetSort().Length)
        {
            this.sortWithinGroup = sortWithinGroup;
            groups = new HashMap<BytesRef, GroupHead>();

            SortField[] sortFields = sortWithinGroup.GetSort();
            for (int i = 0; i < sortFields.Length; i++)
            {
                reversed[i] = sortFields[i].Reverse ? -1 : 1;
            }
        }

        protected override void RetrieveGroupHeadAndAddIfNotExist(int doc)
        {
            int ord = groupIndex.GetOrd(doc);
            BytesRef groupValue;
            if (ord == -1)
            {
                groupValue = null;
            }
            else
            {
                groupIndex.LookupOrd(ord, scratchBytesRef);
                groupValue = scratchBytesRef;
            }
            GroupHead groupHead;
            if (!groups.TryGetValue(groupValue, out groupHead))
            {
                groupHead = new GroupHead(this, groupValue, sortWithinGroup, doc);
                groups[groupValue == null ? null : BytesRef.DeepCopyOf(groupValue)] = groupHead;
                temporalResult.stop = true;
            }
            else
            {
                temporalResult.stop = false;
            }
            temporalResult.groupHead = groupHead;
        }

        protected override ICollection<GroupHead> GetCollectedGroupHeads()
        {
            return groups.Values;
        }

        public override AtomicReaderContext NextReader
        {
            set
            {
                this.readerContext = value;
                groupIndex = FieldCache.DEFAULT.GetTermsIndex(value.AtomicReader, groupField);

                foreach (GroupHead groupHead in groups.Values)
                {
                    for (int i = 0; i < groupHead.comparators.Length; i++)
                    {
                        groupHead.comparators[i] = groupHead.comparators[i].SetNextReader(value);
                    }
                }
            }
        }

        public override Scorer Scorer
        {
            set
            {
                this.scorer = value;
                foreach (GroupHead groupHead in groups.Values)
                {
                    foreach (FieldComparator comparator in groupHead.comparators)
                    {
                        comparator.Scorer = value;
                    }
                }
            }
        }

        internal class GroupHead : AbstractAllGroupHeadsCollector_GroupHead /*AbstractAllGroupHeadsCollector.GroupHead<BytesRef>*/
        {
            private readonly GeneralAllGroupHeadsCollector outerInstance;
            public readonly BytesRef groupValue;

            internal readonly FieldComparator[] comparators;

            internal GroupHead(GeneralAllGroupHeadsCollector outerInstance, BytesRef groupValue, Sort sort, int doc)
                : base(doc + outerInstance.readerContext.DocBase)
            {
                this.outerInstance = outerInstance;
                this.groupValue = groupValue;

                SortField[] sortFields = sort.GetSort();
                comparators = new FieldComparator[sortFields.Length];
                for (int i = 0; i < sortFields.Length; i++)
                {
                    comparators[i] = sortFields[i].GetComparator(1, i).SetNextReader(outerInstance.readerContext);
                    comparators[i].Scorer = outerInstance.scorer;
                    comparators[i].Copy(0, doc);
                    comparators[i].Bottom = 0;
                }
            }

            public override int Compare(int compIDX, int doc)
            {
                return comparators[compIDX].CompareBottom(doc);
            }

            public override void UpdateDocHead(int doc)
            {
                foreach (FieldComparator comparator in comparators)
                {
                    comparator.Copy(0, doc);
                    comparator.Bottom = 0;
                }
                this.Doc = doc + outerInstance.readerContext.DocBase;
            }
        }
    }


    // AbstractAllGroupHeadsCollector optimized for ord fields and scores.
    internal class OrdScoreAllGroupHeadsCollector : TermAllGroupHeadsCollector<OrdScoreAllGroupHeadsCollector.GroupHead>
    {
        //private readonly TermAllGroupHeadsCollector<GH> outerInstance;
        private readonly SentinelIntSet ordSet;
        private readonly IList<GroupHead> collectedGroups;
        private readonly SortField[] fields;

        private SortedDocValues[] sortsIndex;
        private Scorer scorer;
        private GroupHead[] segmentGroupHeads;

        internal OrdScoreAllGroupHeadsCollector(/*TermAllGroupHeadsCollector<GH> outerInstance,*/ string groupField, Sort sortWithinGroup, int initialSize)
            : base(groupField, sortWithinGroup.GetSort().Length)
        {
            //this.outerInstance = outerInstance;
            ordSet = new SentinelIntSet(initialSize, -2);
            collectedGroups = new List<GroupHead>(initialSize);

            SortField[] sortFields = sortWithinGroup.GetSort();
            fields = new SortField[sortFields.Length];
            sortsIndex = new SortedDocValues[sortFields.Length];
            for (int i = 0; i < sortFields.Length; i++)
            {
                reversed[i] = sortFields[i].Reverse ? -1 : 1;
                fields[i] = sortFields[i];
            }
        }

        protected override ICollection<GroupHead> GetCollectedGroupHeads()
        {
            return collectedGroups;
        }

        public override Scorer Scorer
        {
            set
            {
                this.scorer = value;
            }
        }


        protected override void RetrieveGroupHeadAndAddIfNotExist(int doc)
        {
            int key = groupIndex.GetOrd(doc);
            GroupHead groupHead;
            if (!ordSet.Exists(key))
            {
                ordSet.Put(key);
                BytesRef term;
                if (key == -1)
                {
                    term = null;
                }
                else
                {
                    term = new BytesRef();
                    groupIndex.LookupOrd(key, term);
                }
                groupHead = new GroupHead(this, doc, term);
                collectedGroups.Add(groupHead);
                segmentGroupHeads[key + 1] = groupHead;
                temporalResult.stop = true;
            }
            else
            {
                temporalResult.stop = false;
                groupHead = segmentGroupHeads[key + 1];
            }
            temporalResult.groupHead = groupHead;
        }

        public override AtomicReaderContext NextReader
        {
            set
            {
                this.readerContext = value;
                groupIndex = FieldCache.DEFAULT.GetTermsIndex(value.AtomicReader, groupField);
                for (int i = 0; i < fields.Length; i++)
                {
                    if (fields[i].Type == SortField.Type_e.SCORE)
                    {
                        continue;
                    }

                    sortsIndex[i] = FieldCache.DEFAULT.GetTermsIndex(value.AtomicReader, fields[i].Field);
                }

                // Clear ordSet and fill it with previous encountered groups that can occur in the current segment.
                ordSet.Clear();
                segmentGroupHeads = new GroupHead[groupIndex.ValueCount + 1];
                foreach (GroupHead collectedGroup in collectedGroups)
                {
                    int ord;
                    if (collectedGroup.groupValue == null)
                    {
                        ord = -1;
                    }
                    else
                    {
                        ord = groupIndex.LookupTerm(collectedGroup.groupValue);
                    }
                    if (collectedGroup.groupValue == null || ord >= 0)
                    {
                        ordSet.Put(ord);
                        segmentGroupHeads[ord + 1] = collectedGroup;

                        for (int i = 0; i < sortsIndex.Length; i++)
                        {
                            if (fields[i].Type == SortField.Type_e.SCORE)
                            {
                                continue;
                            }
                            int sortOrd;
                            if (collectedGroup.sortValues[i] == null)
                            {
                                sortOrd = -1;
                            }
                            else
                            {
                                sortOrd = sortsIndex[i].LookupTerm(collectedGroup.sortValues[i]);
                            }
                            collectedGroup.sortOrds[i] = sortOrd;
                        }
                    }
                }
            }

        }

        internal class GroupHead : AbstractAllGroupHeadsCollector_GroupHead /*AbstractAllGroupHeadsCollector.GroupHead<BytesRef>*/
        {
            private readonly OrdScoreAllGroupHeadsCollector outerInstance;
            public readonly BytesRef groupValue;

            internal BytesRef[] sortValues;
            internal int[] sortOrds;
            internal float[] scores;

            internal GroupHead(OrdScoreAllGroupHeadsCollector outerInstance, int doc, BytesRef groupValue)
                : base(doc + outerInstance.readerContext.DocBase)
            {
                this.outerInstance = outerInstance;
                this.groupValue = groupValue;

                sortValues = new BytesRef[outerInstance.sortsIndex.Length];
                sortOrds = new int[outerInstance.sortsIndex.Length];
                scores = new float[outerInstance.sortsIndex.Length];
                for (int i = 0; i < outerInstance.sortsIndex.Length; i++)
                {
                    if (outerInstance.fields[i].Type == SortField.Type_e.SCORE)
                    {
                        scores[i] = outerInstance.scorer.Score();
                    }
                    else
                    {
                        sortOrds[i] = outerInstance.sortsIndex[i].GetOrd(doc);
                        sortValues[i] = new BytesRef();
                        if (sortOrds[i] != -1)
                        {
                            outerInstance.sortsIndex[i].Get(doc, sortValues[i]);
                        }
                    }
                }
            }

            public override int Compare(int compIDX, int doc)
            {
                if (outerInstance.fields[compIDX].Type == SortField.Type_e.SCORE)
                {
                    float score = outerInstance.scorer.Score();
                    if (scores[compIDX] < score)
                    {
                        return 1;
                    }
                    else if (scores[compIDX] > score)
                    {
                        return -1;
                    }
                    return 0;
                }
                else
                {
                    if (sortOrds[compIDX] < 0)
                    {
                        // The current segment doesn't contain the sort value we encountered before. Therefore the ord is negative.
                        if (outerInstance.sortsIndex[compIDX].GetOrd(doc) == -1)
                        {
                            outerInstance.scratchBytesRef.Length = 0;
                        }
                        else
                        {
                            outerInstance.sortsIndex[compIDX].Get(doc, outerInstance.scratchBytesRef);
                        }
                        return sortValues[compIDX].CompareTo(outerInstance.scratchBytesRef);
                    }
                    else
                    {
                        return sortOrds[compIDX] - outerInstance.sortsIndex[compIDX].GetOrd(doc);
                    }
                }
            }

            public override void UpdateDocHead(int doc)
            {
                for (int i = 0; i < outerInstance.sortsIndex.Length; i++)
                {
                    if (outerInstance.fields[i].Type == Search.SortField.Type_e.SCORE)
                    {
                        scores[i] = outerInstance.scorer.Score();
                    }
                    else
                    {
                        sortOrds[i] = outerInstance.sortsIndex[i].GetOrd(doc);
                        if (sortOrds[i] == -1)
                        {
                            sortValues[i].Length = 0;
                        }
                        else
                        {
                            outerInstance.sortsIndex[i].Get(doc, sortValues[i]);
                        }
                    }
                }
                this.Doc = doc + outerInstance.readerContext.DocBase;
            }
        }
    }


    // AbstractAllGroupHeadsCollector optimized for ord fields.
    internal class OrdAllGroupHeadsCollector : TermAllGroupHeadsCollector<OrdAllGroupHeadsCollector.GroupHead>
    {
        //private readonly TermAllGroupHeadsCollector<GH> outerInstance;
        private readonly SentinelIntSet ordSet;
        private readonly IList<GroupHead> collectedGroups;
        private readonly SortField[] fields;

        private SortedDocValues[] sortsIndex;
        private GroupHead[] segmentGroupHeads;

        internal OrdAllGroupHeadsCollector(/*TermAllGroupHeadsCollector<GH> outerInstance,*/ string groupField, Sort sortWithinGroup, int initialSize)
                    : base(groupField, sortWithinGroup.GetSort().Length)
        {
            //this.outerInstance = outerInstance;
            ordSet = new SentinelIntSet(initialSize, -2);
            collectedGroups = new List<GroupHead>(initialSize);

            SortField[] sortFields = sortWithinGroup.GetSort();
            fields = new SortField[sortFields.Length];
            sortsIndex = new SortedDocValues[sortFields.Length];
            for (int i = 0; i < sortFields.Length; i++)
            {
                reversed[i] = sortFields[i].Reverse ? -1 : 1;
                fields[i] = sortFields[i];
            }
        }

        protected override ICollection<GroupHead> GetCollectedGroupHeads()
        {
            return collectedGroups;
        }

        public override Scorer Scorer
        {
            set
            {
            }
        }


        protected override void RetrieveGroupHeadAndAddIfNotExist(int doc)
        {
            int key = groupIndex.GetOrd(doc);
            GroupHead groupHead;
            if (!ordSet.Exists(key))
            {
                ordSet.Put(key);
                BytesRef term;
                if (key == -1)
                {
                    term = null;
                }
                else
                {
                    term = new BytesRef();
                    groupIndex.LookupOrd(key, term);
                }
                groupHead = new GroupHead(this, doc, term);
                collectedGroups.Add(groupHead);
                segmentGroupHeads[key + 1] = groupHead;
                temporalResult.stop = true;
            }
            else
            {
                temporalResult.stop = false;
                groupHead = segmentGroupHeads[key + 1];
            }
            temporalResult.groupHead = groupHead;
        }

        public override AtomicReaderContext NextReader
        {
            set
            {
                this.readerContext = value;
                groupIndex = FieldCache.DEFAULT.GetTermsIndex(value.AtomicReader, groupField);
                for (int i = 0; i < fields.Length; i++)
                {
                    sortsIndex[i] = FieldCache.DEFAULT.GetTermsIndex(value.AtomicReader, fields[i].Field);
                }

                // Clear ordSet and fill it with previous encountered groups that can occur in the current segment.
                ordSet.Clear();
                segmentGroupHeads = new GroupHead[groupIndex.ValueCount + 1];
                foreach (GroupHead collectedGroup in collectedGroups)
                {
                    int groupOrd;
                    if (collectedGroup.groupValue == null)
                    {
                        groupOrd = -1;
                    }
                    else
                    {
                        groupOrd = groupIndex.LookupTerm(collectedGroup.groupValue);
                    }
                    if (collectedGroup.groupValue == null || groupOrd >= 0)
                    {
                        ordSet.Put(groupOrd);
                        segmentGroupHeads[groupOrd + 1] = collectedGroup;

                        for (int i = 0; i < sortsIndex.Length; i++)
                        {
                            int sortOrd;
                            if (collectedGroup.sortOrds[i] == -1)
                            {
                                sortOrd = -1;
                            }
                            else
                            {
                                sortOrd = sortsIndex[i].LookupTerm(collectedGroup.sortValues[i]);
                            }
                            collectedGroup.sortOrds[i] = sortOrd;
                        }
                    }
                }
            }
        }

        internal class GroupHead : AbstractAllGroupHeadsCollector_GroupHead /* AbstractAllGroupHeadsCollector.GroupHead<BytesRef>*/
        {
            private readonly OrdAllGroupHeadsCollector outerInstance;
            public readonly BytesRef groupValue;
            internal BytesRef[] sortValues;
            internal int[] sortOrds;

            internal GroupHead(OrdAllGroupHeadsCollector outerInstance, int doc, BytesRef groupValue)
                : base(doc + outerInstance.readerContext.DocBase)
            {
                this.outerInstance = outerInstance;
                this.groupValue = groupValue;

                sortValues = new BytesRef[outerInstance.sortsIndex.Length];
                sortOrds = new int[outerInstance.sortsIndex.Length];
                for (int i = 0; i < outerInstance.sortsIndex.Length; i++)
                {
                    sortOrds[i] = outerInstance.sortsIndex[i].GetOrd(doc);
                    sortValues[i] = new BytesRef();
                    if (sortOrds[i] != -1)
                    {
                        outerInstance.sortsIndex[i].Get(doc, sortValues[i]);
                    }
                }
            }

            public override int Compare(int compIDX, int doc)
            {
                if (sortOrds[compIDX] < 0)
                {
                    // The current segment doesn't contain the sort value we encountered before. Therefore the ord is negative.
                    if (outerInstance.sortsIndex[compIDX].GetOrd(doc) == -1)
                    {
                        outerInstance.scratchBytesRef.Length = 0;
                    }
                    else
                    {
                        outerInstance.sortsIndex[compIDX].Get(doc, outerInstance.scratchBytesRef);
                    }
                    return sortValues[compIDX].CompareTo(outerInstance.scratchBytesRef);
                }
                else
                {
                    return sortOrds[compIDX] - outerInstance.sortsIndex[compIDX].GetOrd(doc);
                }
            }

            public override void UpdateDocHead(int doc)
            {
                for (int i = 0; i < outerInstance.sortsIndex.Length; i++)
                {
                    sortOrds[i] = outerInstance.sortsIndex[i].GetOrd(doc);
                    if (sortOrds[i] == -1)
                    {
                        sortValues[i].Length = 0;
                    }
                    else
                    {
                        outerInstance.sortsIndex[i].LookupOrd(sortOrds[i], sortValues[i]);
                    }
                }
                this.Doc = doc + outerInstance.readerContext.DocBase;
            }

        }

    }


    // AbstractAllGroupHeadsCollector optimized for scores.
    internal class ScoreAllGroupHeadsCollector : TermAllGroupHeadsCollector<ScoreAllGroupHeadsCollector.GroupHead>
    {
        //private readonly TermAllGroupHeadsCollector<GH> outerInstance;
        private readonly SentinelIntSet ordSet;
        private readonly IList<GroupHead> collectedGroups;
        private readonly SortField[] fields;

        private Scorer scorer;
        private GroupHead[] segmentGroupHeads;

        internal ScoreAllGroupHeadsCollector(/*TermAllGroupHeadsCollector<GH> outerInstance,*/ string groupField, Sort sortWithinGroup, int initialSize)
                    : base(groupField, sortWithinGroup.GetSort().Length)
        {
            //this.outerInstance = outerInstance;
            ordSet = new SentinelIntSet(initialSize, -2);
            collectedGroups = new List<GroupHead>(initialSize);

            SortField[] sortFields = sortWithinGroup.GetSort();
            fields = new SortField[sortFields.Length];
            for (int i = 0; i < sortFields.Length; i++)
            {
                reversed[i] = sortFields[i].Reverse ? -1 : 1;
                fields[i] = sortFields[i];
            }
        }

        protected override ICollection<GroupHead> GetCollectedGroupHeads()
        {
            return collectedGroups;
        }

        public override Scorer Scorer
        {
            set
            {
                this.scorer = value;
            }
        }

        protected override void RetrieveGroupHeadAndAddIfNotExist(int doc)
        {
            int key = groupIndex.GetOrd(doc);
            GroupHead groupHead;
            if (!ordSet.Exists(key))
            {
                ordSet.Put(key);
                BytesRef term;
                if (key == -1)
                {
                    term = null;
                }
                else
                {
                    term = new BytesRef();
                    groupIndex.LookupOrd(key, term);
                }
                groupHead = new GroupHead(this, doc, term);
                collectedGroups.Add(groupHead);
                segmentGroupHeads[key + 1] = groupHead;
                temporalResult.stop = true;
            }
            else
            {
                temporalResult.stop = false;
                groupHead = segmentGroupHeads[key + 1];
            }
            temporalResult.groupHead = groupHead;
        }
        public override AtomicReaderContext NextReader
        {
            set
            {
                this.readerContext = value;
                groupIndex = FieldCache.DEFAULT.GetTermsIndex(value.AtomicReader, groupField);

                // Clear ordSet and fill it with previous encountered groups that can occur in the current segment.
                ordSet.Clear();
                segmentGroupHeads = new GroupHead[groupIndex.ValueCount + 1];
                foreach (GroupHead collectedGroup in collectedGroups)
                {
                    int ord;
                    if (collectedGroup.groupValue == null)
                    {
                        ord = -1;
                    }
                    else
                    {
                        ord = groupIndex.LookupTerm(collectedGroup.groupValue);
                    }
                    if (collectedGroup.groupValue == null || ord >= 0)
                    {
                        ordSet.Put(ord);
                        segmentGroupHeads[ord + 1] = collectedGroup;
                    }
                }
            }
        }

        internal class GroupHead : AbstractAllGroupHeadsCollector_GroupHead
        {
            private readonly ScoreAllGroupHeadsCollector outerInstance;
            // LUCENENET: Moved this here from the base class, AbstractAllGroupHeadsCollector_GroupHead so it doesn't
            // need to reference the generic closing type BytesRef.
            public readonly BytesRef groupValue;
            internal float[] scores;

            internal GroupHead(ScoreAllGroupHeadsCollector outerInstance, int doc, BytesRef groupValue)
                : base(doc + outerInstance.readerContext.DocBase)
            {
                this.outerInstance = outerInstance;
                this.groupValue = groupValue;

                scores = new float[outerInstance.fields.Length];
                float score = outerInstance.scorer.Score();
                for (int i = 0; i < scores.Length; i++)
                {
                    scores[i] = score;
                }
            }

            public override int Compare(int compIDX, int doc)
            {
                float score = outerInstance.scorer.Score();
                if (scores[compIDX] < score)
                {
                    return 1;
                }
                else if (scores[compIDX] > score)
                {
                    return -1;
                }
                return 0;
            }

            public override void UpdateDocHead(int doc)
            {
                float score = outerInstance.scorer.Score();
                for (int i = 0; i < scores.Length; i++)
                {
                    scores[i] = score;
                }
                this.Doc = doc + outerInstance.readerContext.DocBase;
            }
        }
    }
}
