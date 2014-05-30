using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Support
{
    public class IdentityHashMap<TKey, TValue> : HashMap<TKey, TValue>
    {
        public IdentityHashMap()
            : base(new IdentityComparer<TKey>())
        {
        }
    }
}