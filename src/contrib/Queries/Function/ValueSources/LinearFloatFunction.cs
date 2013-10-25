using Lucene.Net.Index;
using Lucene.Net.Search.Function.DocValues;
using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Search.Function.ValueSources
{
    public class LinearFloatFunction : ValueSource
    {
        protected readonly ValueSource source;
        protected readonly float slope;
        protected readonly float intercept;
        public LinearFloatFunction(ValueSource source, float slope, float intercept)
        {
            this.source = source;
            this.slope = slope;
            this.intercept = intercept;
        }

        public override string Description
        {
            get
            {
                return slope + @"*float(" + source.Description + @")+" + intercept;
            }
        }

        public override FunctionValues GetValues(IDictionary<object, object> context, AtomicReaderContext readerContext)
        {
            FunctionValues vals = source.GetValues(context, readerContext);
            return new AnonymousFloatDocValues(this, vals);
        }

        private sealed class AnonymousFloatDocValues : FloatDocValues
        {
            public AnonymousFloatDocValues(LinearFloatFunction parent, FunctionValues vals)
                : base(parent)
            {
                this.parent = parent;
                this.vals = vals;
            }

            private readonly LinearFloatFunction parent;
            private readonly FunctionValues vals;

            public override float FloatVal(int doc)
            {
                return vals.FloatVal(doc) * parent.slope + parent.intercept;
            }

            public override string ToString(int doc)
            {
                return parent.slope + @"*float(" + vals.ToString(doc) + @")+" + parent.intercept;
            }
        }

        public override void CreateWeight(IDictionary<object, object> context, IndexSearcher searcher)
        {
            source.CreateWeight(context, searcher);
        }

        public override int GetHashCode()
        {
            int h = Number.FloatToIntBits(slope);
            h = (h >> 2) | (h << 30);
            h = Number.FloatToIntBits(intercept);
            h = (h << 14) | (h >> 19);
            return h + source.GetHashCode();
        }

        public override bool Equals(Object o)
        {
            if (typeof(LinearFloatFunction) != o.GetType())
                return false;
            LinearFloatFunction other = (LinearFloatFunction)o;
            return this.slope == other.slope && this.intercept == other.intercept && this.source.Equals(other.source);
        }
    }
}
