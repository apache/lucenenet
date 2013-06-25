using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Index
{
    public abstract class Fields : IEnumerable<String> 
    {
        protected Fields()
        {
        }

        public abstract IEnumerator<String> GetEnumerator();

        public abstract Terms Terms(String field);

        public abstract int Size { get; }

        public static readonly Fields[] EMPTY_ARRAY = new Fields[0];

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
