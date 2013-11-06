using Lucene.Net.Facet.Search;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Facet.Partitions
{
    public interface IIntermediateFacetResult
    {
        FacetRequest FacetRequest { get; }
    }
}
