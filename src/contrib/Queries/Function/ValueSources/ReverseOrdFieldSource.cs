using Lucene.Net.Index;
using Lucene.Net.Search.Function.DocValues;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Search.Function.ValueSources
{
    public class ReverseOrdFieldSource : ValueSource
    {
        public readonly string field;

        public ReverseOrdFieldSource(string field)
        {
            this.field = field;
        }

        public override string Description
        {
            get 
            {
                return @"rord(" + field + ')';
            }
        }

        public override FunctionValues GetValues(IDictionary<object, object> context, AtomicReaderContext readerContext)
        {
            IndexReader topReader = ReaderUtil.GetTopLevelContext(readerContext).Reader;
            AtomicReader r = topReader is CompositeReader ? new SlowCompositeReaderWrapper((CompositeReader) topReader) : (AtomicReader) topReader;
            int off = readerContext.docBase;
            SortedDocValues sindex = FieldCache.DEFAULT.GetTermsIndex(r, field);
            int end = sindex.ValueCount;

            return new AnonymousIntDocValues(this, sindex, end, off);
        }

        private sealed class AnonymousIntDocValues : IntDocValues
        {
            public AnonymousIntDocValues(ReverseOrdFieldSource parent, SortedDocValues sindex, int end, int off)
                : base(parent)
            {
                this.parent = parent;
                this.sindex = sindex;
                this.end = end;
                this.off = off;
            }

            private readonly ReverseOrdFieldSource parent;
            private readonly SortedDocValues sindex;
            private readonly int end, off;

            public override int IntVal(int doc)
            {
                return (end - sindex.GetOrd(doc + off) - 1);
            }
        }

        public override bool Equals(Object o)
        {
            if (o == null || (o.GetType() != typeof (ReverseOrdFieldSource)))
                return false;
            ReverseOrdFieldSource other = (ReverseOrdFieldSource) o;
            return this.field.Equals(other.field);
        }

        private static readonly int hcode = typeof(ReverseOrdFieldSource).GetHashCode();
        public override int GetHashCode()
        {
            return hcode + field.GetHashCode();
        }
    }
}
