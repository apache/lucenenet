using Lucene.Net.Index;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Search.Function.ValueSources
{
    public class NumDocsValueSource : ValueSource
    {
        public virtual string Name
        {
            get
            {
                return "numdocs";
            }
        }

        public override string Description
        {
            get
            {
                return Name + "()";
            }
        }

        public override FunctionValues GetValues(IDictionary<object, object> context, AtomicReaderContext readerContext)
        {
            return new ConstIntDocValues(ReaderUtil.GetTopLevelContext(readerContext).Reader.NumDocs, this);
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
