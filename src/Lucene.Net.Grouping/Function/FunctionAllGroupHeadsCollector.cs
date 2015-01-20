using System.Collections.Generic;
using Lucene.Net.Index;
using Lucene.Net.Queries.Function;
using Lucene.Net.Search;
using Lucene.Net.Search.Grouping;
using Lucene.Net.Util.Mutable;

namespace Lucene.Net.Grouping.Function
{
    /// <summary>
    /// An implementation of
    /// <see cref="AbstractAllGroupHeadsCollector{GH}">Lucene.Net.Search.Grouping.AbstractAllGroupHeadsCollector&lt;GH&gt;
    /// 	</see>
    /// for retrieving the most relevant groups when grouping
    /// by
    /// <see cref="Lucene.Net.Queries.Function.ValueSource">Lucene.Net.Queries.Function.ValueSource
    /// 	</see>
    /// .
    /// </summary>
    /// <lucene.experimental></lucene.experimental>
    public class FunctionAllGroupHeadsCollector : AbstractAllGroupHeadsCollector<FunctionAllGroupHeadsCollector.GroupHead>
    {
        private readonly ValueSource groupBy;

        private readonly IDictionary<object, object> vsContext;

        private readonly IDictionary<MutableValue, GroupHead
            > groups;

        private readonly Sort sortWithinGroup;

        private FunctionValues.ValueFiller filler;

        private MutableValue mval;

        private AtomicReaderContext readerContext;

        private Scorer scorer;

        /// <summary>
        /// Constructs a
        /// <see cref="FunctionAllGroupHeadsCollector">FunctionAllGroupHeadsCollector</see>
        /// instance.
        /// </summary>
        /// <param name="groupBy">
        /// The
        /// <see cref="Lucene.Net.Queries.Function.ValueSource">Lucene.Net.Queries.Function.ValueSource
        /// 	</see>
        /// to group by
        /// </param>
        /// <param name="vsContext">The ValueSource context</param>
        /// <param name="sortWithinGroup">The sort within a group</param>
        public FunctionAllGroupHeadsCollector(ValueSource groupBy, IDictionary<object, object
            > vsContext, Sort sortWithinGroup)
            : base(sortWithinGroup.GetSort().Length)
        {
            groups = new Dictionary<MutableValue, FunctionAllGroupHeadsCollector.GroupHead>();
            this.sortWithinGroup = sortWithinGroup;
            this.groupBy = groupBy;
            this.vsContext = vsContext;
            SortField[] sortFields = sortWithinGroup.GetSort();
            for (int i = 0; i < sortFields.Length; i++)
            {
                reversed[i] = sortFields[i].Reverse ? -1 : 1;
            }
        }

        /// <exception cref="System.IO.IOException"></exception>
        protected internal override void RetrieveGroupHeadAndAddIfNotExist(int doc)
        {
            filler.FillValue(doc);
            GroupHead groupHead = groups[mval];
            if (groupHead == null)
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

        protected internal override ICollection<GroupHead>
             GetCollectedGroupHeads()
        {
            return groups.Values;
        }

        
        public override Scorer Scorer
        {
            set
            {
                this.scorer = value;
                foreach (var groupHead in groups.Values)
                {
                    foreach (FieldComparator<object> comparator in groupHead.comparators)
                    {
                        comparator.Scorer=value;
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
                filler = values.GetValueFiller();
                mval = filler.GetValue();
                foreach (FunctionAllGroupHeadsCollector.GroupHead groupHead in groups.Values)
                {
                    for (int i = 0; i < groupHead.comparators.Length; i++)
                    {
                        groupHead.comparators[i] = groupHead.comparators[i].SetNextReader(value);
                    }
                }
            }
        }

        /// <summary>Holds current head document for a single group.</summary>
        /// <remarks>Holds current head document for a single group.</remarks>
        /// <lucene.experimental></lucene.experimental>
        public class GroupHead : AbstractAllGroupHeadsCollector.GroupHead<MutableValue>
        {
            internal readonly FieldComparator[] comparators;

            /// <exception cref="System.IO.IOException"></exception>
            internal GroupHead(FunctionAllGroupHeadsCollector _enclosing, MutableValue groupValue
                , Sort sort, int doc)
                : base(groupValue, doc + _enclosing.readerContext.DocBase)
            {
                this._enclosing = _enclosing;
                SortField[] sortFields = sort.GetSort();
                this.comparators = new FieldComparator[sortFields.Length];
                for (int i = 0; i < sortFields.Length; i++)
                {
                    this.comparators[i] = sortFields[i].GetComparator(1, i).SetNextReader(this._enclosing
                        .readerContext);
                    this.comparators[i].Scorer = (this._enclosing.scorer);
                    this.comparators[i].Copy(0, doc);
                    this.comparators[i].Bottom = (0);
                }
            }

            
            protected internal override int Compare(int compIDX, int doc)
            {
                return this.comparators[compIDX].CompareBottom(doc);
            }

            
            protected internal override void UpdateDocHead(int doc)
            {
                foreach (FieldComparator<object> comparator in this.comparators)
                {
                    comparator.Copy(0, doc);
                    comparator.Bottom = (0);
                }
                this.doc = doc + this._enclosing.readerContext.DocBase;
            }

            private readonly FunctionAllGroupHeadsCollector _enclosing;
        }
    }
}
