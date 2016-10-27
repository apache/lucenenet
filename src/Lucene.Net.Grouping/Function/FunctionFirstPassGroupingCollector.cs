using Lucene.Net.Index;
using Lucene.Net.Queries.Function;
using Lucene.Net.Util.Mutable;
using System.Collections;

namespace Lucene.Net.Search.Grouping.Function
{
    /// <summary>
    /// Concrete implementation of <see cref="AbstractFirstPassGroupingCollector{TGroupValue}"/> that groups based on
    /// <see cref="ValueSource"/> instances.
    /// 
    /// @lucene.experimental
    /// </summary>
    public class FunctionFirstPassGroupingCollector : AbstractFirstPassGroupingCollector<MutableValue>
    {
        private readonly ValueSource groupByVS;
        private readonly IDictionary /* Map<?, ?> */ vsContext;

        private FunctionValues.AbstractValueFiller filler;
        private MutableValue mval;

        /// <summary>
        /// Creates a first pass collector.
        /// </summary>
        /// <param name="groupByVS">The <see cref="ValueSource"/> instance to group by</param>
        /// <param name="vsContext">The <see cref="ValueSource"/> context</param>
        /// <param name="groupSort">
        /// The <see cref="Sort"/> used to sort the
        /// groups.  The top sorted document within each group
        /// according to groupSort, determines how that group
        /// sorts against other groups.  This must be non-null,
        /// ie, if you want to groupSort by relevance use
        /// <see cref="Sort.RELEVANCE"/>.
        /// </param>
        /// <param name="topNGroups">How many top groups to keep.</param>
        /// <exception cref="IOException">When I/O related errors occur</exception>
        public FunctionFirstPassGroupingCollector(ValueSource groupByVS, IDictionary /* Map<?, ?> */ vsContext, Sort groupSort, int topNGroups)
            : base(groupSort, topNGroups)
        {
            this.groupByVS = groupByVS;
            this.vsContext = vsContext;
        }

        protected override MutableValue GetDocGroupValue(int doc)
        {
            filler.FillValue(doc);
            return mval;
        }

        protected override MutableValue CopyDocGroupValue(MutableValue groupValue, MutableValue reuse)
        {
            if (reuse != null)
            {
                reuse.Copy(groupValue);
                return reuse;
            }
            return groupValue.Duplicate();
        }

        public override AtomicReaderContext NextReader
        {
            set
            {
                base.NextReader = value;
                FunctionValues values = groupByVS.GetValues(vsContext, value);
                filler = values.ValueFiller;
                mval = filler.Value;
            }
        }
    }
}
