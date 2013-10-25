using Lucene.Net.Index;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Search.Function.ValueSources
{
    public abstract class MultiFunction : ValueSource
    {
        protected readonly IList<ValueSource> sources;
        
        public MultiFunction(IList<ValueSource> sources)
        {
            this.sources = sources;
        }

        protected abstract string Name { get; }

        public override string Description
        {
            get
            {
                return BuildDescription(Name, sources);
            }
        }

        public static string BuildDescription(string name, IList<ValueSource> sources)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(name).Append('(');
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

        public static FunctionValues[] ValsArr(IList<ValueSource> sources, IDictionary<object, object> fcontext, AtomicReaderContext readerContext)
        {
            FunctionValues[] valsArr = new FunctionValues[sources.Count];
            int i = 0;
            foreach (ValueSource source in sources)
            {
                valsArr[i++] = source.GetValues(fcontext, readerContext);
            }

            return valsArr;
        }

        public class Values : FunctionValues
        {
            protected internal readonly FunctionValues[] valsArr;
            private readonly MultiFunction parent;

            public Values(MultiFunction parent, FunctionValues[] valsArr)
            {
                this.parent = parent;
                this.valsArr = valsArr;
            }

            public override string ToString(int doc)
            {
                return MultiFunction.ToString(parent.Name, valsArr, doc);
            }

            public override ValueFiller GetValueFiller()
            {
                return base.GetValueFiller();
            }
        }

        public static string ToString(string name, FunctionValues[] valsArr, int doc)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(name).Append('(');
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

        public override void CreateWeight(IDictionary<object, object> context, IndexSearcher searcher)
        {
            foreach (ValueSource source in sources)
                source.CreateWeight(context, searcher);
        }

        public override int GetHashCode()
        {
            return sources.GetHashCode() + Name.GetHashCode();
        }

        public override bool Equals(Object o)
        {
            if (this.GetType() != o.GetType())
                return false;
            MultiFunction other = (MultiFunction)o;
            return this.sources.Equals(other.sources);
        }
    }
}
