using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Search.Function.ValueSources
{
    public abstract class SingleFunction : ValueSource
    {
        protected readonly ValueSource source;

        public SingleFunction(ValueSource source)
        {
            this.source = source;
        }

        protected abstract string Name { get; }

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
            SingleFunction other = (SingleFunction)o;
            return this.Name.Equals(other.Name) && this.source.Equals(other.source);
        }

        public override void CreateWeight(IDictionary<object, object> context, IndexSearcher searcher)
        {
            source.CreateWeight(context, searcher);
        }
    }
}
