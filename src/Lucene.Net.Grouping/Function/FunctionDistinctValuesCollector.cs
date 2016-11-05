using Lucene.Net.Index;
using Lucene.Net.Queries.Function;
using Lucene.Net.Support;
using Lucene.Net.Util.Mutable;
using System.Collections;
using System.Collections.Generic;

namespace Lucene.Net.Search.Grouping.Function
{
    /// <summary>
    /// Function based implementation of <see cref="AbstractDistinctValuesCollector"/>.
    /// 
    /// @lucene.experimental
    /// </summary>
    public class FunctionDistinctValuesCollector : AbstractDistinctValuesCollector<FunctionDistinctValuesCollector.GroupCount>
    {
        private readonly IDictionary /* Map<?, ?> */ vsContext;
        private readonly ValueSource groupSource;
        private readonly ValueSource countSource;
        private readonly IDictionary<MutableValue, GroupCount> groupMap;

        private FunctionValues.AbstractValueFiller groupFiller;
        private FunctionValues.AbstractValueFiller countFiller;
        private MutableValue groupMval;
        private MutableValue countMval;

        public FunctionDistinctValuesCollector(IDictionary /*Map<?, ?>*/ vsContext, ValueSource groupSource, ValueSource countSource, IEnumerable<ISearchGroup<MutableValue>> groups)
        {
            this.vsContext = vsContext;
            this.groupSource = groupSource;
            this.countSource = countSource;
            groupMap = new LinkedHashMap<MutableValue, GroupCount>();
            foreach (SearchGroup<MutableValue> group in groups)
            {
                groupMap[group.GroupValue] = new GroupCount(group.GroupValue);
            }
        }

        public override IEnumerable<GroupCount> Groups
        {
            get { return new List<GroupCount>(groupMap.Values); }
        }

        public override void Collect(int doc)
        {
            groupFiller.FillValue(doc);
            GroupCount groupCount;
            if (groupMap.TryGetValue(groupMval, out groupCount))
            {
                countFiller.FillValue(doc);
                ((ISet<MutableValue>)groupCount.UniqueValues).Add(countMval.Duplicate());
            }
        }

        public override AtomicReaderContext NextReader
        {
            set
            {
                FunctionValues values = groupSource.GetValues(vsContext, value);
                groupFiller = values.ValueFiller;
                groupMval = groupFiller.Value;
                values = countSource.GetValues(vsContext, value);
                countFiller = values.ValueFiller;
                countMval = countFiller.Value;
            }
        }



        /** Holds distinct values for a single group.
         *
         * @lucene.experimental */
        public class GroupCount : AbstractGroupCount<MutableValue> /*AbstractDistinctValuesCollector.GroupCount<MutableValue>*/
        {
            internal GroupCount(MutableValue groupValue)
                : base(groupValue)
            {
            }
        }
    }
}
