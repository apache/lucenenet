using Lucene.Net.Facet.Taxonomy;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Facet.Index
{
    public interface ICategoryListBuilder
    {
        IDictionary<string, BytesRef> Build(IntsRef ordinals, IEnumerable<CategoryPath> categories);
    }
}
