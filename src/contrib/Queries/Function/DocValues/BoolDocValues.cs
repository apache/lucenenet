using Lucene.Net.Util.Mutable;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Search.Function.DocValues
{
    public abstract class BoolDocValues : FunctionValues
    {
        protected readonly ValueSource vs;

        public BoolDocValues(ValueSource vs)
        {
            this.vs = vs;
        }

        public abstract override bool BoolVal(int doc);

        public override byte ByteVal(int doc)
        {
            return BoolVal(doc) ? (byte)1 : (byte)0;
        }

        public override short ShortVal(int doc)
        {
            return BoolVal(doc) ? (short)1 : (short)0;
        }

        public override float FloatVal(int doc)
        {
            return BoolVal(doc) ? (float)1 : (float)0;
        }

        public override int IntVal(int doc)
        {
            return BoolVal(doc) ? 1 : 0;
        }

        public override long LongVal(int doc)
        {
            return BoolVal(doc) ? (long)1 : (long)0;
        }

        public override double DoubleVal(int doc)
        {
            return BoolVal(doc) ? (double)1 : (double)0;
        }

        public override string StrVal(int doc)
        {
            return BoolVal(doc).ToString();
        }

        public override object ObjectVal(int doc)
        {
            return Exists(doc) ? (bool?)BoolVal(doc) : null;
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
            private readonly MutableValueBool mval = new MutableValueBool();
            private readonly BoolDocValues parent;

            public AnonymousValueFiller(BoolDocValues parent)
            {
                this.parent = parent;
            }

            public override MutableValue Value
            {
                get { return mval; }
            }

            public override void FillValue(int doc)
            {
                mval.Value = parent.BoolVal(doc);
                mval.Exists = parent.Exists(doc);
            }
        }
    }
}
