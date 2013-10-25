using Lucene.Net.Index;
using Lucene.Net.Search.Function.DocValues;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Search.Function.ValueSources
{
    public abstract class MultiBoolFunction : BoolFunction
    {
        protected readonly IList<ValueSource> sources;

        public MultiBoolFunction(IList<ValueSource> sources)
        {
            this.sources = sources;
        }

        protected abstract string Name { get; }

        protected abstract bool Func(int doc, FunctionValues[] vals);

        public override FunctionValues GetValues(IDictionary<object, object> context, AtomicReaderContext readerContext)
        {
            FunctionValues[] vals = new FunctionValues[sources.Count];
            int i = 0;
            foreach (ValueSource source in sources)
            {
                vals[i++] = source.GetValues(context, readerContext);
            }

            return new AnonymousBoolDocValues(this, vals);
        }

        private sealed class AnonymousBoolDocValues : BoolDocValues
        {
            public AnonymousBoolDocValues(MultiBoolFunction parent, FunctionValues[] vals)
                : base(parent)
            {
                this.parent = parent;
                this.vals = vals;
            }

            private readonly MultiBoolFunction parent;
            private readonly FunctionValues[] vals;

            public override bool BoolVal(int doc)
            {
                return parent.Func(doc, vals);
            }

            public override string ToString(int doc)
            {
                StringBuilder sb = new StringBuilder(parent.Name);
                sb.Append('(');
                bool first = true;
                foreach (FunctionValues dv in vals)
                {
                    if (first)
                    {
                        first = false;
                    }
                    else
                    {
                        sb.Append(',');
                    }

                    sb.Append(dv.ToString(doc));
                }

                return sb.ToString();
            }
        }

        public override string Description
        {
            get
            {
                StringBuilder sb = new StringBuilder(Name);
                sb.Append('(');
                bool first = true;
                foreach (ValueSource source in sources)
                {
                    if (first)
                    {
                        first = false;
                    }
                    else
                    {
                        sb.Append(',');
                    }

                    sb.Append(source.Description);
                }

                return sb.ToString();
            }
        }

        public override int GetHashCode()
        {
            return sources.GetHashCode() + Name.GetHashCode();
        }

        public override bool Equals(Object o)
        {
            if (this.GetType() != o.GetType())
                return false;
            MultiBoolFunction other = (MultiBoolFunction)o;
            return this.sources.Equals(other.sources);
        }

        public override void CreateWeight(IDictionary<object, object> context, IndexSearcher searcher)
        {
            foreach (ValueSource source in sources)
            {
                source.CreateWeight(context, searcher);
            }
        }
    }
}
