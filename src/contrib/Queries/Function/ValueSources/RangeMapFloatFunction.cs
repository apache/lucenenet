using Lucene.Net.Index;
using Lucene.Net.Search.Function.DocValues;
using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Search.Function.ValueSources
{
    public class RangeMapFloatFunction : ValueSource
    {
        protected readonly ValueSource source;
        protected readonly float min;
        protected readonly float max;
        protected readonly float target;
        protected readonly float? defaultVal;

        public RangeMapFloatFunction(ValueSource source, float min, float max, float target, float? def)
        {
            this.source = source;
            this.min = min;
            this.max = max;
            this.target = target;
            this.defaultVal = def;
        }

        public override string Description
        {
            get
            {
                return @"map(" + source.Description + @"," + min + @"," + max + @"," + target + @")";
            }
        }

        public override FunctionValues GetValues(IDictionary<object, object> context, AtomicReaderContext readerContext)
        {
            FunctionValues vals = source.GetValues(context, readerContext);
            return new AnonymousFloatDocValues(this, vals);
        }

        private sealed class AnonymousFloatDocValues : FloatDocValues
        {
            public AnonymousFloatDocValues(RangeMapFloatFunction parent, FunctionValues vals)
                : base(parent)
            {
                this.parent = parent;
                this.vals = vals;
            }

            private readonly RangeMapFloatFunction parent;
            private readonly FunctionValues vals;

            public override float FloatVal(int doc)
            {
                float val = vals.FloatVal(doc);
                return (val >= parent.min && val <= parent.max) ? parent.target : (parent.defaultVal == null ? val : parent.defaultVal.Value);
            }

            public override string ToString(int doc)
            {
                return @"map(" + vals.ToString(doc) + @",min=" + parent.min + @",max=" + parent.max + @",target=" + parent.target + @")";
            }
        }

        public override void CreateWeight(IDictionary<object, object> context, IndexSearcher searcher)
        {
            source.CreateWeight(context, searcher);
        }

        public override int GetHashCode()
        {
            int h = source.GetHashCode();
            h = (h << 10) | (h >> 23);
            h = Number.FloatToIntBits(min);
            h = (h << 14) | (h >> 19);
            h = Number.FloatToIntBits(max);
            h = (h << 13) | (h >> 20);
            h = Number.FloatToIntBits(target);
            if (defaultVal != null)
                h = defaultVal.GetHashCode();
            return h;
        }

        public override bool Equals(Object o)
        {
            if (typeof(RangeMapFloatFunction) != o.GetType())
                return false;
            RangeMapFloatFunction other = (RangeMapFloatFunction)o;
            return this.min == other.min && this.max == other.max && this.target == other.target && this.source.Equals(other.source) && (this.defaultVal == other.defaultVal || (this.defaultVal != null && this.defaultVal.Equals(other.defaultVal)));
        }
    }
}
