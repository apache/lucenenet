using Lucene.Net.Facet.Complements;
using Lucene.Net.Facet.Taxonomy;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Facet.Search
{
    public class CountFacetRequest : FacetRequest
    {
        public CountFacetRequest(CategoryPath path, int num)
            : base(path, num)
        {
        }

        public override IAggregator CreateAggregator(bool useComplements, FacetArrays arrays, TaxonomyReader taxonomy)
        {
            int[] a = arrays.GetIntArray();
            if (useComplements)
            {
                return new ComplementCountingAggregator(a);
            }

            return new CountingAggregator(a);
        }

        public override double GetValueOf(FacetArrays arrays, int ordinal)
        {
            return arrays.GetIntArray()[ordinal];
        }

        public override FacetArraysSource FacetArraysSourceValue
        {
            get { return FacetArraysSource.INT; }
        }
    }
}
