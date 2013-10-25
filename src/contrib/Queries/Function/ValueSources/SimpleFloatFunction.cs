using Lucene.Net.Index;
using Lucene.Net.Search.Function.DocValues;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Search.Function.ValueSources
{
    public abstract class SimpleFloatFunction : SingleFunction
    {
        public SimpleFloatFunction(ValueSource source)
            : base(source)
        {
        }

        protected abstract float Func(int doc, FunctionValues vals);

        public override FunctionValues GetValues(IDictionary<object, object> context, AtomicReaderContext readerContext)
        {
            FunctionValues vals = source.GetValues(context, readerContext);
            return new AnonymousFloatDocValues(this, vals);
        }

        private sealed class AnonymousFloatDocValues : FloatDocValues
        {
            public AnonymousFloatDocValues(SimpleFloatFunction parent, FunctionValues vals)
                : base(parent)
            {
                this.parent = parent;
                this.vals = vals;
            }

            private readonly SimpleFloatFunction parent;
            private readonly FunctionValues vals;

            public override float FloatVal(int doc)
            {
                return parent.Func(doc, vals);
            }

            public override string ToString(int doc)
            {
                return parent.Name + '(' + vals.ToString(doc) + ')';
            }
        }
    }
}
