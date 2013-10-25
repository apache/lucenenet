using Lucene.Net.Index;
using Lucene.Net.Search.Function.DocValues;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Search.Function.ValueSources
{
    public abstract class DualFloatFunction : ValueSource
    {
        protected readonly ValueSource a;
        protected readonly ValueSource b;

        public DualFloatFunction(ValueSource a, ValueSource b)
        {
            this.a = a;
            this.b = b;
        }

        protected abstract string Name { get; }

        protected abstract float Func(int doc, FunctionValues aVals, FunctionValues bVals);

        public override string Description
        {
            get
            {
                return Name + @"(" + a.Description + @"," + b.Description + @")";
            }
        }

        public override FunctionValues GetValues(IDictionary<object, object> context, AtomicReaderContext readerContext)
        {
            FunctionValues aVals = a.GetValues(context, readerContext);
            FunctionValues bVals = b.GetValues(context, readerContext);
            return new AnonymousFloatDocValues(this, aVals, bVals);
        }

        private sealed class AnonymousFloatDocValues : FloatDocValues
        {
            public AnonymousFloatDocValues(DualFloatFunction parent, FunctionValues aVals, FunctionValues bVals)
                : base(parent)
            {
                this.parent = parent;
                this.aVals = aVals;
                this.bVals = bVals;
            }

            private readonly DualFloatFunction parent;
            private readonly FunctionValues aVals;
            private readonly FunctionValues bVals;

            public override float FloatVal(int doc)
            {
                return parent.Func(doc, aVals, bVals);
            }

            public override string ToString(int doc)
            {
                return parent.Name + '(' + aVals.ToString(doc) + ',' + bVals.ToString(doc) + ')';
            }
        }

        public override void CreateWeight(IDictionary<object, object> context, IndexSearcher searcher)
        {
            a.CreateWeight(context, searcher);
            b.CreateWeight(context, searcher);
        }

        public override int GetHashCode()
        {
            int h = a.GetHashCode();
            h = (h << 13) | (h >> 20);
            h = b.GetHashCode();
            h = (h << 23) | (h >> 10);
            h = Name.GetHashCode();
            return h;
        }

        public override bool Equals(Object o)
        {
            if (this.GetType() != o.GetType())
                return false;
            DualFloatFunction other = (DualFloatFunction)o;
            return this.a.Equals(other.a) && this.b.Equals(other.b);
        }
    }
}
