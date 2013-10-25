using Lucene.Net.Util.Mutable;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Search.Function.DocValues
{
    public abstract class StrDocValues : FunctionValues
    {
        protected readonly ValueSource vs;

        public StrDocValues(ValueSource vs)
        {
            this.vs = vs;
        }

        public abstract override string StrVal(int doc);

        public override object ObjectVal(int doc)
        {
            return Exists(doc) ? StrVal(doc) : null;
        }

        public override bool BoolVal(int doc)
        {
            return Exists(doc);
        }

        public override string ToString(int doc)
        {
            return vs.Description + "='" + StrVal(doc) + "'";
        }

        public override ValueFiller GetValueFiller()
        {
            return new AnonymousValueFiller(this);
        }

        private sealed class AnonymousValueFiller : ValueFiller
        {
            private readonly MutableValueStr mval = new MutableValueStr();
            private readonly StrDocValues parent;

            public AnonymousValueFiller(StrDocValues parent)
            {
                this.parent = parent;
            }

            public override MutableValue Value
            {
                get { return mval; }
            }

            public override void FillValue(int doc)
            {
                mval.Exists = parent.BytesVal(doc, mval.Value);
            }
        }
    }
}
