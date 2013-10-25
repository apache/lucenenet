using Lucene.Net.Index;
using Lucene.Net.Search.Function.DocValues;
using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Search.Function.ValueSources
{
    public class ReciprocalFloatFunction : ValueSource
    {
        protected readonly ValueSource source;
        protected readonly float m;
        protected readonly float a;
        protected readonly float b;

        public ReciprocalFloatFunction(ValueSource source, float m, float a, float b)
        {
            this.source = source;
            this.m = m;
            this.a = a;
            this.b = b;
        }

        public override FunctionValues GetValues(IDictionary<object, object> context, AtomicReaderContext readerContext)
        {
            FunctionValues vals = source.GetValues(context, readerContext);

            return new AnonymousFloatDocValues(this, vals);
        }

        private sealed class AnonymousFloatDocValues : FloatDocValues
        {
            public AnonymousFloatDocValues(ReciprocalFloatFunction parent, FunctionValues vals)
                : base(parent)
            {
                this.parent = parent;
                this.vals = vals;
            }

            private readonly ReciprocalFloatFunction parent;
            private readonly FunctionValues vals;

            public override float FloatVal(int doc)
            {
                return parent.a / (parent.m * vals.FloatVal(doc) + parent.b);
            }

            public override string ToString(int doc)
            {
                return parent.a.ToString() + @"/(" + parent.m + @"*float(" + vals.ToString(doc) + ')' + '+' + parent.b + ')';
            }
        }

        public override void CreateWeight(IDictionary<object, object> context, IndexSearcher searcher)
        {
            source.CreateWeight(context, searcher);
        }

        public override string Description
        {
            get
            {
                return a.ToString() + @"/(" + m + @"*float(" + source.Description + @")" + @"+" + b + ')';
            }
        }

        public override int GetHashCode()
        {
            int h = Number.FloatToIntBits(a) + Number.FloatToIntBits(m);
            h = (h << 13) | (h >> 20);
            return h + (Number.FloatToIntBits(b)) + source.GetHashCode();
        }

        public override bool Equals(Object o)
        {
            if (typeof(ReciprocalFloatFunction) != o.GetType())
                return false;
            ReciprocalFloatFunction other = (ReciprocalFloatFunction)o;
            return this.m == other.m && this.a == other.a && this.b == other.b && this.source.Equals(other.source);
        }
    }
}
