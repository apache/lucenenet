using Lucene.Net.Documents;
using Lucene.Net.Facet.Index;
using Lucene.Net.Facet.Params;
using Lucene.Net.Facet.Taxonomy;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Facet.SortedSet
{
    public class SortedSetDocValuesFacetFields : FacetFields
    {
        public SortedSetDocValuesFacetFields()
            : this(FacetIndexingParams.DEFAULT)
        {
        }

        public SortedSetDocValuesFacetFields(FacetIndexingParams fip)
            : base(null, fip)
        {
            if (fip.PartitionSize != int.MaxValue)
            {
                throw new ArgumentException(@"partitions are not supported");
            }
        }

        public override void AddFields(Document doc, IEnumerable<CategoryPath> categories)
        {
            if (categories == null)
            {
                throw new ArgumentException(@"categories should not be null");
            }

            IDictionary<CategoryListParams, IEnumerable<CategoryPath>> categoryLists = CreateCategoryListMapping(categories);
            foreach (KeyValuePair<CategoryListParams, IEnumerable<CategoryPath>> e in categoryLists)
            {
                CategoryListParams clp = e.Key;
                string dvField = clp.field + SortedSetDocValuesReaderState.FACET_FIELD_EXTENSION;
                foreach (CategoryPath cp in e.Value)
                {
                    if (cp.length != 2)
                    {
                        throw new ArgumentException(@"only flat facets (dimension + label) are currently supported; got " + cp);
                    }

                    doc.Add(new SortedSetDocValuesField(dvField, new BytesRef(cp.ToString(indexingParams.FacetDelimChar))));
                }

                DrillDownStream drillDownStream = GetDrillDownStream(e.Value);
                Field drillDown = new Field(clp.field, drillDownStream, DrillDownFieldType());
                doc.Add(drillDown);
            }
        }
    }
}
