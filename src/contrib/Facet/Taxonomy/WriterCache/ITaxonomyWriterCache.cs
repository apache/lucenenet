using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Facet.Taxonomy.WriterCache
{
    public interface ITaxonomyWriterCache
    {
        void Close();
        
        int? Get(CategoryPath categoryPath);
        
        bool Put(CategoryPath categoryPath, int? ordinal);
        
        bool IsFull { get; }
        
        void Clear();
    }
}
