using Lucene.Net.Facet.Taxonomy;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Facet.Search
{
    public class SumScoreFacetRequest : FacetRequest
    {
        public SumScoreFacetRequest(CategoryPath path, int num)
            : base(path, num)
        {
        }

        public override IAggregator CreateAggregator(bool useComplements, FacetArrays arrays, TaxonomyReader taxonomy)
        {
            return new ScoringAggregator(arrays.GetFloatArray());
        }

        public override double GetValueOf(FacetArrays arrays, int ordinal)
        {
            return arrays.GetFloatArray()[ordinal];
        }

        public override FacetArraysSource FacetArraysSourceValue
        {
            get
            {
                return FacetArraysSource.FLOAT;
            }
        }
    }
}
