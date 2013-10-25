using Lucene.Net.Util.Mutable;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Search.Function.DocValues
{
    public abstract class IntDocValues : FunctionValues
    {
        protected readonly ValueSource vs;

        public IntDocValues(ValueSource vs)
        {
            this.vs = vs;
        }

        public override byte ByteVal(int doc)
        {
            return (byte)IntVal(doc);
        }

        public override short ShortVal(int doc)
        {
            return (short)IntVal(doc);
        }

        public override float FloatVal(int doc)
        {
            return (float)IntVal(doc);   
        }

        public abstract override int IntVal(int doc);

        public override long LongVal(int doc)
        {
            return (long)IntVal(doc);
        }

        public override bool BoolVal(int doc)
        {
            return IntVal(doc) != 0;
        }

        public override double DoubleVal(int doc)
        {
            return (double)IntVal(doc);
        }

        public override string StrVal(int doc)
        {
            return IntVal(doc).ToString();
        }

        public override object ObjectVal(int doc)
        {
            return Exists(doc) ? (object)IntVal(doc) : null;
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
            private readonly MutableValueInt mval = new MutableValueInt();
            private readonly IntDocValues parent;

            public AnonymousValueFiller(IntDocValues parent)
            {
                this.parent = parent;
            }

            public override MutableValue Value
            {
                get { return mval; }
            }

            public override void FillValue(int doc)
            {
                mval.Value = parent.IntVal(doc);
                mval.Exists = parent.Exists(doc);
            }
        }
    }
}
