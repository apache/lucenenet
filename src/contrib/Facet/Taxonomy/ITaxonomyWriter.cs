using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Facet.Taxonomy
{
    public interface ITaxonomyWriter
    {
        int AddCategory(CategoryPath categoryPath);
        
        int GetParent(int ordinal);
        
        int Size { get; }

        IDictionary<String, String> CommitData { get; set; }
    }
}
