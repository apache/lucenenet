using Lucene.Net.Facet.Search;
using Lucene.Net.Facet.Taxonomy;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Facet.Associations
{
    public class AssociationFloatSumFacetRequest : FacetRequest
    {
        public AssociationFloatSumFacetRequest(CategoryPath path, int num)
            : base(path, num)
        {
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
