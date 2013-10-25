using Lucene.Net.Util.Mutable;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Search.Function.DocValues
{
    public abstract class FloatDocValues : FunctionValues
    {
        protected readonly ValueSource vs;

        public FloatDocValues(ValueSource vs)
        {
            this.vs = vs;
        }

        public override byte ByteVal(int doc)
        {
            return (byte)FloatVal(doc);
        }

        public override short ShortVal(int doc)
        {
            return (short)FloatVal(doc);
        }

        public abstract override float FloatVal(int doc);

        public override int IntVal(int doc)
        {
            return (int)FloatVal(doc);
        }

        public override long LongVal(int doc)
        {
            return (long)FloatVal(doc);
        }

        public override bool BoolVal(int doc)
        {
            return FloatVal(doc) != 0;
        }

        public override double DoubleVal(int doc)
        {
            return (double)FloatVal(doc);
        }

        public override string StrVal(int doc)
        {
            return FloatVal(doc).ToString();
        }

        public override object ObjectVal(int doc)
        {
            return Exists(doc) ? (object)FloatVal(doc) : null;
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
            private readonly MutableValueFloat mval = new MutableValueFloat();
            private readonly FloatDocValues parent;

            public AnonymousValueFiller(FloatDocValues parent)
            {
                this.parent = parent;
            }

            public override MutableValue Value
            {
                get { return mval; }
            }

            public override void FillValue(int doc)
            {
                mval.Value = parent.FloatVal(doc);
                mval.Exists = parent.Exists(doc);
            }
        }
    }
}
