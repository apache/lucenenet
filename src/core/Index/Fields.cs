using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Index
{
    public abstract class Fields : IEnumerator<String> 
    {
        protected Fields()
        {
        }

        public abstract IEnumerable<String> Iterator { get; }

        public abstract Terms Terms(String field);

        public abstract int Size { get; }

        public static readonly Fields[] EMPTY_ARRAY = new Fields[0];
    }
}
