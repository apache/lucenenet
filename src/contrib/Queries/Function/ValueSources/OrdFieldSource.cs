using Lucene.Net.Index;
using Lucene.Net.Search.Function.DocValues;
using Lucene.Net.Util.Mutable;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Search.Function.ValueSources
{
    public class OrdFieldSource : ValueSource
    {
        protected readonly string field;

        public OrdFieldSource(string field)
        {
            this.field = field;
        }

        public override string Description
        {
            get
            {
                return "ord(" + field + ')';
            }
        }

        public override FunctionValues GetValues(IDictionary<object, object> context, AtomicReaderContext readerContext)
        {
            int off = readerContext.docBase;
            IndexReader topReader = ReaderUtil.GetTopLevelContext(readerContext).Reader;
            AtomicReader r = topReader is CompositeReader ? new SlowCompositeReaderWrapper((CompositeReader)topReader) : (AtomicReader)topReader;
            SortedDocValues sindex = FieldCache.DEFAULT.GetTermsIndex(r, field);
            return new AnonymousIntDocValues(this, sindex, off);
        }

        private sealed class AnonymousValueFiller : FunctionValues.ValueFiller
        {
            public AnonymousValueFiller(OrdFieldSource parent, SortedDocValues sindex)
            {
                this.parent = parent;
                this.sindex = sindex;
            }

            private readonly OrdFieldSource parent;
            private readonly SortedDocValues sindex;

            private readonly MutableValueInt mval = new MutableValueInt();

            public override MutableValue Value
            {
                get
                {
                    return mval;
                }
            }

            public override void FillValue(int doc)
            {
                mval.Value = sindex.GetOrd(doc);
                mval.Exists = mval.Value != 0;
            }
        }

        private sealed class AnonymousIntDocValues : IntDocValues
        {
            public AnonymousIntDocValues(OrdFieldSource parent, SortedDocValues sindex, int off)
                : base(parent)
            {
                this.parent = parent;
                this.sindex = sindex;
                this.off = off;
            }

            private readonly OrdFieldSource parent;
            private readonly SortedDocValues sindex;
            private readonly int off;

            protected string ToTerm(string readableValue)
            {
                return readableValue;
            }

            public override int IntVal(int doc)
            {
                return sindex.GetOrd(doc + off);
            }

            public override int OrdVal(int doc)
            {
                return sindex.GetOrd(doc + off);
            }

            public override int NumOrd()
            {
                return sindex.ValueCount;
            }

            public override bool Exists(int doc)
            {
                return sindex.GetOrd(doc + off) != 0;
            }

            public override ValueFiller GetValueFiller()
            {
                return new AnonymousValueFiller(parent, sindex);
            }
        }

        public override bool Equals(Object o)
        {
            return o != null && o.GetType() == typeof(OrdFieldSource) && this.field.Equals(((OrdFieldSource)o).field);
        }

        private static readonly int hcode = typeof(OrdFieldSource).GetHashCode();

        public override int GetHashCode()
        {
            return hcode + field.GetHashCode();
        }
    }
}
