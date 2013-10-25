using Lucene.Net.Util.Mutable;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Search.Function.DocValues
{
    public abstract class LongDocValues : FunctionValues
    {
        protected readonly ValueSource vs;

        public LongDocValues(ValueSource vs)
        {
            this.vs = vs;
        }

        public override byte ByteVal(int doc)
        {
            return (byte)LongVal(doc);
        }

        public override short ShortVal(int doc)
        {
            return (short)LongVal(doc);
        }

        public override float FloatVal(int doc)
        {
            return (float)LongVal(doc);
        }

        public override int IntVal(int doc)
        {
            return (int)LongVal(doc);
        }

        public abstract override long LongVal(int doc);

        public override bool BoolVal(int doc)
        {
            return LongVal(doc) != 0;
        }

        public override double DoubleVal(int doc)
        {
            return (double)LongVal(doc);
        }

        public override string StrVal(int doc)
        {
            return LongVal(doc).ToString();
        }

        public override object ObjectVal(int doc)
        {
            return Exists(doc) ? (object)LongVal(doc) : null;
        }

        public override string ToString(int doc)
        {
            return vs.Description + '=' + StrVal(doc);
        }

        public override ValueFiller GetValueFiller()
        {
            return new AnonymousValueFiller(this);
        }

        private sealed class AnonymousValueFiller : ValueFiller
        {
            private readonly MutableValueLong mval = new MutableValueLong();
            private readonly LongDocValues parent;

            public AnonymousValueFiller(LongDocValues parent)
            {
                this.parent = parent;
            }

            public override MutableValue Value
            {
                get { return mval; }
            }

            public override void FillValue(int doc)
            {
                mval.Value = parent.LongVal(doc);
                mval.Exists = parent.Exists(doc);
            }
        }
    }
}
