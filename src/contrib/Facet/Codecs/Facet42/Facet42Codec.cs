using Lucene.Net.Codecs;
using Lucene.Net.Codecs.Lucene42;
using Lucene.Net.Facet.Params;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Facet.Codecs.Facet42
{
    public class Facet42Codec : Lucene42Codec
    {
        private readonly ISet<String> facetFields;
        private readonly DocValuesFormat facetsDVFormat = DocValuesFormat.ForName(@"Facet42");
        private readonly DocValuesFormat lucene42DVFormat = DocValuesFormat.ForName(@"Lucene42");

        public Facet42Codec()
            : this(FacetIndexingParams.DEFAULT)
        {
        }

        public Facet42Codec(FacetIndexingParams fip)
        {
            if (fip.PartitionSize != int.MaxValue)
            {
                throw new ArgumentException("this Codec does not support partitions");
            }

            this.facetFields = new HashSet<String>();
            foreach (CategoryListParams clp in fip.AllCategoryListParams)
            {
                facetFields.Add(clp.field);
            }
        }

        public override DocValuesFormat GetDocValuesFormatForField(string field)
        {
            if (facetFields.Contains(field))
            {
                return facetsDVFormat;
            }
            else
            {
                return lucene42DVFormat;
            }
        }
    }
}
