using Lucene.Net.Facet.Search;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Facet.Sampling
{
    public interface ISampleFixer
    {
        void FixResult(IScoredDocIDs origDocIds, FacetResult fres);
    }
}
