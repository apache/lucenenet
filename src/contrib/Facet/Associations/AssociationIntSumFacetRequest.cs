using Lucene.Net.Facet.Search;
using Lucene.Net.Facet.Taxonomy;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Facet.Associations
{
    public class AssociationIntSumFacetRequest : FacetRequest
    {
        public AssociationIntSumFacetRequest(CategoryPath path, int num)
            : base(path, num)
        {
        }

        public override FacetArraysSource FacetArraysSourceValue
        {
            get
            {
                return FacetArraysSource.INT;
            }
        }

        public override double GetValueOf(FacetArrays arrays, int ordinal)
        {
            return arrays.GetIntArray()[ordinal];
        }
    }
}
