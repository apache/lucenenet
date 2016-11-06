using Lucene.Net.Index;
using Lucene.Net.Queries.Function;
using Lucene.Net.Search;
using Lucene.Net.Util.Mutable;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lucene.Net.Search.Grouping.Function
{
    /// <summary>
    /// An implementation of <see cref="AbstractAllGroupHeadsCollector"/> for retrieving the most relevant groups when grouping
    /// by <see cref="ValueSource"/>.
    /// 
    /// @lucene.experimental
    /// </summary>
    public class FunctionAllGroupHeadsCollector : AbstractAllGroupHeadsCollector<FunctionAllGroupHeadsCollector.GroupHead>
    {
        private readonly ValueSource groupBy;
        private readonly IDictionary /* Map<?, ?> */ vsContext;
        private readonly IDictionary<MutableValue, GroupHead> groups;
        private readonly Sort sortWithinGroup;

        private FunctionValues.AbstractValueFiller filler;
        private MutableValue mval;
        private AtomicReaderContext readerContext;
        private Scorer scorer;

        /**
         * Constructs a {@link FunctionAllGroupHeadsCollector} instance.
         *
         * @param groupBy The {@link ValueSource} to group by
         * @param vsContext The ValueSource context
         * @param sortWithinGroup The sort within a group
         */
        public FunctionAllGroupHeadsCollector(ValueSource groupBy, IDictionary /* Map<?, ?> */ vsContext, Sort sortWithinGroup)
            : base(sortWithinGroup.GetSort().Length)
        {
            groups = new Dictionary<MutableValue, GroupHead>();
            this.sortWithinGroup = sortWithinGroup;
            this.groupBy = groupBy;
            this.vsContext = vsContext;

            SortField[] sortFields = sortWithinGroup.GetSort();
            for (int i = 0; i < sortFields.Length; i++)
            {
                reversed[i] = sortFields[i].Reverse ? -1 : 1;
            }
        }

        protected override void RetrieveGroupHeadAndAddIfNotExist(int doc)
        {
            filler.FillValue(doc);
            GroupHead groupHead;
            if (!groups.TryGetValue(mval, out groupHead))
            {
                MutableValue groupValue = mval.Duplicate();
                groupHead = new GroupHead(this, groupValue, sortWithinGroup, doc);
                groups[groupValue] = groupHead;
                temporalResult.stop = true;
            }
            else
            {
                temporalResult.stop = false;
            }
            this.temporalResult.groupHead = groupHead;
        }

        protected override ICollection<GroupHead> GetCollectedGroupHeads()
        {
            return groups.Values;
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

        public override AtomicReaderContext NextReader
        {
            set
            {
                this.readerContext = value;
                FunctionValues values = groupBy.GetValues(vsContext, value);
                filler = values.ValueFiller;
                mval = filler.Value;

                foreach (GroupHead groupHead in groups.Values)
                {
                    for (int i = 0; i < groupHead.comparators.Length; i++)
                    {
                        groupHead.comparators[i] = groupHead.comparators[i].SetNextReader(value);
                    }
                }
            }
        }

        /** Holds current head document for a single group.
         *
         * @lucene.experimental */
        public class GroupHead : AbstractAllGroupHeadsCollector_GroupHead /*AbstractAllGroupHeadsCollector.GroupHead<MutableValue>*/
        {

            private readonly FunctionAllGroupHeadsCollector outerInstance;
            public readonly MutableValue groupValue;
            internal readonly FieldComparator[] comparators;

            internal GroupHead(FunctionAllGroupHeadsCollector outerInstance, MutableValue groupValue, Sort sort, int doc)
                        /*: base(groupValue, doc + outerInstance.readerContext.DocBase)*/
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
}
