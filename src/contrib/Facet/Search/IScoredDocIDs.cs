using Lucene.Net.Search;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Facet.Search
{
    public interface IScoredDocIDs
    {
        IScoredDocIDsIterator Iterator();

        DocIdSet DocIDs { get; }
        
        int Size { get; }
    }
}
