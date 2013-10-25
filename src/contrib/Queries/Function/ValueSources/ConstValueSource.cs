using Lucene.Net.Index;
using Lucene.Net.Search.Function.DocValues;
using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Search.Function.ValueSources
{
    public class ConstValueSource : ConstNumberSource
    {
        readonly float constant;
        private readonly double dv;

        public ConstValueSource(float constant)
        {
            this.constant = constant;
            this.dv = constant;
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
            return new AnonymousFloatDocValues(this);
        }

        private sealed class AnonymousFloatDocValues : FloatDocValues
        {
            public AnonymousFloatDocValues(ConstValueSource parent)
                : base(parent)
            {
                this.parent = parent;
            }

            private readonly ConstValueSource parent;

            public override float FloatVal(int doc)
            {
                return parent.constant;
            }

            public override int IntVal(int doc)
            {
                return (int)parent.constant;
            }

            public override long LongVal(int doc)
            {
                return (long)parent.constant;
            }

            public override double DoubleVal(int doc)
            {
                return parent.dv;
            }

            public override string ToString(int doc)
            {
                return parent.Description;
            }

            public override Object ObjectVal(int doc)
            {
                return parent.constant;
            }

            public override bool BoolVal(int doc)
            {
                return parent.constant != 0.0f;
            }
        }

        public override int GetHashCode()
        {
            return Number.FloatToIntBits(constant) * 31;
        }

        public override bool Equals(Object o)
        {
            if (!(o is ConstValueSource))
                return false;
            ConstValueSource other = (ConstValueSource)o;
            return this.constant == other.constant;
        }

        public override int GetInt()
        {
            return (int)constant;
        }

        public override long GetLong()
        {
            return (long)constant;
        }

        public override float GetFloat()
        {
            return constant;
        }

        public override double GetDouble()
        {
            return dv;
        }

        public override object GetNumber()
        {
            return constant;
        }

        public override bool GetBool()
        {
            return constant != 0.0f;
        }
    }
}
