using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Facet.Taxonomy
{
    public abstract class ParallelTaxonomyArrays
    {
        public abstract int[] Parents { get; }
        public abstract int[] Children { get; }
        public abstract int[] Siblings { get; }
    }
}
