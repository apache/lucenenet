using Lucene.Net.Index;
using Lucene.Net.Search.Function.DocValues;
using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Search.Function.ValueSources
{
    public abstract class MultiFloatFunction : ValueSource
    {
        protected readonly ValueSource[] sources;

        public MultiFloatFunction(ValueSource[] sources)
        {
            this.sources = sources;
        }

        protected abstract string Name { get; }

        protected abstract float Func(int doc, FunctionValues[] valsArr);
        
        public override string Description
        {
            get
            {
                StringBuilder sb = new StringBuilder();
                sb.Append(Name).Append('(');
                bool firstTime = true;
                foreach (ValueSource source in sources)
                {
                    if (firstTime)
                    {
                        firstTime = false;
                    }
                    else
                    {
                        sb.Append(',');
                    }

                    sb.Append(source);
                }

                sb.Append(')');
                return sb.ToString();
            }
        }

        public override FunctionValues GetValues(IDictionary<object, object> context, AtomicReaderContext readerContext)
        {
            FunctionValues[] valsArr = new FunctionValues[sources.Length];
            for (int i = 0; i < sources.Length; i++)
            {
                valsArr[i] = sources[i].GetValues(context, readerContext);
            }

            return new AnonymousFloatDocValues(this, valsArr);
        }

        private sealed class AnonymousFloatDocValues : FloatDocValues
        {
            public AnonymousFloatDocValues(MultiFloatFunction parent, FunctionValues[] valsArr)
                : base(parent)
            {
                this.parent = parent;
                this.valsArr = valsArr;
            }

            private readonly MultiFloatFunction parent;
            private readonly FunctionValues[] valsArr;

            public override float FloatVal(int doc)
            {
                return parent.Func(doc, valsArr);
            }

            public override string ToString(int doc)
            {
                StringBuilder sb = new StringBuilder();
                sb.Append(parent.Name).Append('(');
                bool firstTime = true;
                foreach (FunctionValues vals in valsArr)
                {
                    if (firstTime)
                    {
                        firstTime = false;
                    }
                    else
                    {
                        sb.Append(',');
                    }

                    sb.Append(vals.ToString(doc));
                }

                sb.Append(')');
                return sb.ToString();
            }
        }

        public override void CreateWeight(IDictionary<object, object> context, IndexSearcher searcher)
        {
            foreach (ValueSource source in sources)
                source.CreateWeight(context, searcher);
        }

        public override int GetHashCode()
        {
            return Arrays.HashCode(sources) + Name.GetHashCode();
        }

        public override bool Equals(Object o)
        {
            if (this.GetType() != o.GetType())
                return false;
            MultiFloatFunction other = (MultiFloatFunction)o;
            return this.Name.Equals(other.Name) && Arrays.Equals(this.sources, other.sources);
        }
    }
}
