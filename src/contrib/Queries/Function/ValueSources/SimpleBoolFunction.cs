using Lucene.Net.Index;
using Lucene.Net.Search.Function.DocValues;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Search.Function.ValueSources
{
    public abstract class SimpleBoolFunction : BoolFunction
    {
        protected readonly ValueSource source;

        public SimpleBoolFunction(ValueSource source)
        {
            this.source = source;
        }

        protected abstract string Name { get; }

        protected abstract bool Func(int doc, FunctionValues vals);
        
        public override FunctionValues GetValues(IDictionary<object, object> context, AtomicReaderContext readerContext)
        {
            FunctionValues vals = source.GetValues(context, readerContext);
            return new AnonymousBoolDocValues(this, vals);
        }

        private sealed class AnonymousBoolDocValues : BoolDocValues
        {
            public AnonymousBoolDocValues(SimpleBoolFunction parent, FunctionValues vals)
                : base(parent)
            {
                this.parent = parent;
                this.vals = vals;
            }

            private readonly SimpleBoolFunction parent;
            private readonly FunctionValues vals;

            public override bool BoolVal(int doc)
            {
                return parent.Func(doc, vals);
            }

            public override string ToString(int doc)
            {
                return parent.Name + '(' + vals.ToString(doc) + ')';
            }
        }

        public override string Description
        {
            get
            {
                return Name + '(' + source.Description + ')';
            }
        }

        public override int GetHashCode()
        {
            return source.GetHashCode() + Name.GetHashCode();
        }

        public override bool Equals(Object o)
        {
            if (this.GetType() != o.GetType())
                return false;
            SimpleBoolFunction other = (SimpleBoolFunction)o;
            return this.source.Equals(other.source);
        }

        public override void CreateWeight(IDictionary<object, object> context, IndexSearcher searcher)
        {
            source.CreateWeight(context, searcher);
        }
    }
}
