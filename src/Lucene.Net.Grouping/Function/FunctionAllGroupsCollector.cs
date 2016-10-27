using Lucene.Net.Index;
using Lucene.Net.Queries.Function;
using Lucene.Net.Util.Mutable;
using System.Collections;
using System.Collections.Generic;

namespace Lucene.Net.Search.Grouping.Function
{
    /// <summary>
    /// A collector that collects all groups that match the
    /// query. Only the group value is collected, and the order
    /// is undefined.  This collector does not determine
    /// the most relevant document of a group.
    /// 
    /// <para>
    /// Implementation detail: Uses <see cref="ValueSource"/> and <see cref="FunctionValues"/> to retrieve the
    /// field values to group by.
    /// </para>
    /// @lucene.experimental
    /// </summary>
    public class FunctionAllGroupsCollector : AbstractAllGroupsCollector<MutableValue>
    {
        private readonly IDictionary /* Map<?, ?> */ vsContext;
        private readonly ValueSource groupBy;
        private readonly SortedSet<MutableValue> groups = new SortedSet<MutableValue>();

        private FunctionValues.AbstractValueFiller filler;
        private MutableValue mval;

        /**
         * Constructs a {@link FunctionAllGroupsCollector} instance.
         *
         * @param groupBy The {@link ValueSource} to group by
         * @param vsContext The ValueSource context
         */
        public FunctionAllGroupsCollector(ValueSource groupBy, IDictionary /* Map<?, ?> */ vsContext)
        {
            this.vsContext = vsContext;
            this.groupBy = groupBy;
        }

        public override ICollection<MutableValue> Groups
        {
            get
            {
                return groups;
            }
        }

        public override void Collect(int doc)
        {
            filler.FillValue(doc);
            if (!groups.Contains(mval))
            {
                groups.Add(mval.Duplicate());
            }
        }

        public override AtomicReaderContext NextReader
        {
            set
            {
                FunctionValues values = groupBy.GetValues(vsContext, value);
                filler = values.ValueFiller;
                mval = filler.Value;
            }
        }
    }
}
