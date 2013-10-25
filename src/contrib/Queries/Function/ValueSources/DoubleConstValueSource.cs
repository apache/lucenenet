using Lucene.Net.Index;
using Lucene.Net.Search.Function.DocValues;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Search.Function.ValueSources
{
    public class DoubleConstValueSource : ConstNumberSource
    {
        readonly double constant;
        private readonly float fv;
        private readonly long lv;

        public DoubleConstValueSource(double constant)
        {
            this.constant = constant;
            this.fv = (float)constant;
            this.lv = (long)constant;
        }

        public override string Description
        {
            get
            {
                return @"const(" + constant + @")";
            }
        }

        public override FunctionValues GetValues(IDictionary<object, object> context, AtomicReaderContext readerContext)
        {
            return new AnonymousDoubleDocValues(this);
        }

        private sealed class AnonymousDoubleDocValues : DoubleDocValues
        {
            public AnonymousDoubleDocValues(DoubleConstValueSource parent)
                : base(parent)
            {
                this.parent = parent;
            }

            private readonly DoubleConstValueSource parent;

            public override float FloatVal(int doc)
            {
                return parent.fv;
            }

            public override int IntVal(int doc)
            {
                return (int)parent.lv;
            }

            public override long LongVal(int doc)
            {
                return parent.lv;
            }

            public override double DoubleVal(int doc)
            {
                return parent.constant;
            }

            public override string StrVal(int doc)
            {
                return parent.constant.ToString();
            }

            public override Object ObjectVal(int doc)
            {
                return parent.constant;
            }

            public override string ToString(int doc)
            {
                return parent.Description;
            }
        }

        public override int GetHashCode()
        {
            long bits = BitConverter.DoubleToInt64Bits(constant);
            return (int)(bits ^ (bits >> 32));
        }

        public override bool Equals(Object o)
        {
            if (!(o is DoubleConstValueSource))
                return false;
            DoubleConstValueSource other = (DoubleConstValueSource)o;
            return this.constant == other.constant;
        }

        public override int GetInt()
        {
            return (int)lv;
        }

        public override long GetLong()
        {
            return lv;
        }

        public override float GetFloat()
        {
            return fv;
        }

        public override double GetDouble()
        {
            return constant;
        }

        public override object GetNumber()
        {
            return constant;
        }

        public override bool GetBool()
        {
            return constant != 0;
        }
    }
}
