using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Facet.Taxonomy.WriterCache.Lru
{
    public class NameHashIntCacheLRU : NameIntCacheLRU
    {
        internal NameHashIntCacheLRU(int maxCacheSize)
            : base(maxCacheSize)
        {
        }

        internal override Object Key(CategoryPath name)
        {
            return name.LongHashCode();
        }

        internal override Object Key(CategoryPath name, int prefixLen)
        {
            return name.Subpath(prefixLen).LongHashCode();
        }
    }
}
