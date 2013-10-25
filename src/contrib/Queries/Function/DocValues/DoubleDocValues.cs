using Lucene.Net.Util.Mutable;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Search.Function.DocValues
{
    public abstract class DoubleDocValues : FunctionValues
    {
        protected readonly ValueSource vs;

        public DoubleDocValues(ValueSource vs)
        {
            this.vs = vs;
        }

        public override byte ByteVal(int doc)
        {
            return (byte)DoubleVal(doc);
        }

        public override short ShortVal(int doc)
        {
            return (short)DoubleVal(doc);
        }

        public override float FloatVal(int doc)
        {
            return (float)DoubleVal(doc);
        }

        public override int IntVal(int doc)
        {
            return (int)DoubleVal(doc);
        }

        public override long LongVal(int doc)
        {
            return (long)DoubleVal(doc);
        }

        public override bool BoolVal(int doc)
        {
            return DoubleVal(doc) != 0;
        }

        public abstract override double DoubleVal(int doc);

        public override string StrVal(int doc)
        {
            return DoubleVal(doc).ToString();
        }

        public override object ObjectVal(int doc)
        {
            return Exists(doc) ? (object)DoubleVal(doc) : null;
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
            private readonly MutableValueDouble mval = new MutableValueDouble();
            private readonly DoubleDocValues parent;

            public AnonymousValueFiller(DoubleDocValues parent)
            {
                this.parent = parent;
            }

            public override MutableValue Value
            {
                get { return mval; }
            }

            public override void FillValue(int doc)
            {
                mval.Value = parent.DoubleVal(doc);
                mval.Exists = parent.Exists(doc);
            }
        }
    }
}
