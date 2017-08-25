using Icu.Collation;
using Lucene.Net.Analysis.TokenAttributes;
using Lucene.Net.Support;
using Lucene.Net.Util;
#if NETSTANDARD
using SortKey = Icu.ObjectModel.SortKey;
#else
using SortKey = System.Globalization.SortKey;
#endif

namespace Lucene.Net.Collation.TokenAttributes
{
    /// <summary>
    /// Extension of <see cref="CharTermAttribute"/> that encodes the term
    /// text as a binary Unicode collation key instead of as UTF-8 bytes.
    /// </summary>
    [ExceptionToClassNameConvention]
    public class ICUCollatedTermAttribute : CharTermAttribute
    {
        private readonly Collator collator;
        //private readonly RawCollationKey key = new RawCollationKey();
        private SortKey key;

        /// <summary>
        /// Create a new ICUCollatedTermAttribute
        /// </summary>
        /// <param name="collator"><see cref="SortKey"/> generator.</param>
        public ICUCollatedTermAttribute(Collator collator)
        {
            // clone the collator: see http://userguide.icu-project.org/collation/architecture
            this.collator = (Collator)collator.Clone();
        }

        public override void FillBytesRef()
        {
            BytesRef bytes = this.BytesRef;
            key = collator.GetSortKey(ToString());
            bytes.Bytes = key.KeyData;
            bytes.Offset = 0;
            bytes.Length = key.KeyData.Length;
        }
    }
}
