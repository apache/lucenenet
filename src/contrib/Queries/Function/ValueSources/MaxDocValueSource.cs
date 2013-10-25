using Lucene.Net.Index;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Search.Function.ValueSources
{
    public class MaxDocValueSource : ValueSource
    {
        public virtual string Name
        {
            get
            {
                return "maxdoc";
            }
        }

        public override string Description
        {
            get
            {
                return Name + "()";
            }
        }

        public override void CreateWeight(IDictionary<object, object> context, IndexSearcher searcher)
        {
            context["searcher"] = searcher;
        }

        public override FunctionValues GetValues(IDictionary<object, object> context, AtomicReaderContext readerContext)
        {
            IndexSearcher searcher = (IndexSearcher)context["searcher"];
            return new ConstIntDocValues(searcher.IndexReader.MaxDoc, this);
        }

        public override bool Equals(Object o)
        {
            return this.GetType() == o.GetType();
        }

        public override int GetHashCode()
        {
            return this.GetType().GetHashCode();
        }
    }
}
