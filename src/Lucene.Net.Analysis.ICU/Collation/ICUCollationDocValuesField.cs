using Icu.Collation;
#if NETSTANDARD
using Icu.ObjectModel; // For SortKey
#endif
using Lucene.Net.Documents;
using Lucene.Net.Support;
using Lucene.Net.Util;
using System.Globalization;

namespace Lucene.Net.Collation
{
    /// <summary>
    /// Indexes sort keys as a single-valued <see cref="SortedDocValuesField"/>.
    /// </summary>
    /// <remarks>
    /// This is more efficient that <see cref="ICUCollationKeyAnalyzer"/> if the field 
    /// only has one value: no uninversion is necessary to sort on the field, 
    /// locale-sensitive range queries can still work via <see cref="Search.FieldCacheRangeFilter"/>, 
    /// and the underlying data structures built at index-time are likely more efficient 
    /// and use less memory than FieldCache.
    /// </remarks>
    [ExceptionToClassNameConvention]
    public sealed class ICUCollationDocValuesField : Field
    {
        private readonly string name;
        private readonly Collator collator;
        private readonly BytesRef bytes = new BytesRef();
        private SortKey key;

        /// <summary>
        /// Create a new <see cref="ICUCollationDocValuesField"/>.
        /// <para/>
        /// NOTE: you should not create a new one for each document, instead
        /// just make one and reuse it during your indexing process, setting
        /// the value via <see cref="SetStringValue(string)"/>.
        /// </summary>
        /// <param name="name">Field name.</param>
        /// <param name="collator">Collator for generating collation keys.</param>
        // TODO: can we make this trap-free? maybe just synchronize on the collator
        // instead? 
        public ICUCollationDocValuesField(string name, Collator collator)
            : base(name, SortedDocValuesField.TYPE)
        {
            this.name = name;
            this.collator = (Collator)collator.Clone();
            m_fieldsData = bytes; // so wrong setters cannot be called
        }

        public override string Name
        {
            get
            {
                return name;
            }
        }

        public override void SetStringValue(string value)
        {
            key = collator.GetSortKey(value);
            bytes.Bytes = key.KeyData;
            bytes.Offset = 0;
            bytes.Length = key.KeyData.Length;
        }
    }
}
