using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Util;

namespace Lucene.Net.Search.Grouping.Terms
{
    /// <summary>
    /// Concrete implementation of <see cref="AbstractFirstPassGroupingCollector{BytesRef}"/> that groups based on
    /// field values and more specifically uses <see cref="SortedDocValues"/>
    /// to collect groups.
    /// 
    /// @lucene.experimental
    /// </summary>
    public class TermFirstPassGroupingCollector : AbstractFirstPassGroupingCollector<BytesRef>
    {
        private readonly BytesRef scratchBytesRef = new BytesRef();
        private SortedDocValues index;

        private string groupField;

        /// <summary>
        /// Create the first pass collector.
        /// </summary>
        /// <param name="groupField">
        /// The field used to group
        /// documents. This field must be single-valued and
        /// indexed (<see cref="FieldCache"/> is used to access its value
        /// per-document).
        /// </param>
        /// <param name="groupSort">
        /// The <see cref="Sort"/> used to sort the
        /// groups.  The top sorted document within each group
        /// according to groupSort, determines how that group
        /// sorts against other groups.  This must be non-null,
        /// ie, if you want to groupSort by relevance use
        /// <see cref="Sort.RELEVANCE"/>.
        /// </param>
        /// <param name="topNGroups">
        /// How many top groups to keep.
        /// </param>
        /// <exception cref="IOException">When I/O related errors occur</exception>
        public TermFirstPassGroupingCollector(string groupField, Sort groupSort, int topNGroups)
            : base(groupSort, topNGroups)
        {
            this.groupField = groupField;
        }

        protected override BytesRef GetDocGroupValue(int doc)
        {
            int ord = index.GetOrd(doc);
            if (ord == -1)
            {
                return null;
            }
            else
            {
                index.LookupOrd(ord, scratchBytesRef);
                return scratchBytesRef;
            }
        }

        protected override BytesRef CopyDocGroupValue(BytesRef groupValue, BytesRef reuse)
        {
            if (groupValue == null)
            {
                return null;
            }
            else if (reuse != null)
            {
                reuse.CopyBytes(groupValue);
                return reuse;
            }
            else
            {
                return BytesRef.DeepCopyOf(groupValue);
            }
        }

        public override AtomicReaderContext NextReader
        {
            set
            {
                base.NextReader = value;
                index = FieldCache.DEFAULT.GetTermsIndex(value.AtomicReader, groupField);
            }
        }
    }
}
