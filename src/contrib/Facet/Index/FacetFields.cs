using Lucene.Net.Documents;
using Lucene.Net.Facet.Params;
using Lucene.Net.Facet.Taxonomy;
using Lucene.Net.Index;
using Lucene.Net.Support;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Facet.Index
{
    public class FacetFields
    {
        private static readonly FieldType DRILL_DOWN_TYPE = new FieldType(TextField.TYPE_NOT_STORED);

        static FacetFields()
        {
            DRILL_DOWN_TYPE.IndexOptions = FieldInfo.IndexOptions.DOCS_ONLY;
            DRILL_DOWN_TYPE.OmitNorms = true;
            DRILL_DOWN_TYPE.Freeze();
        }

        protected readonly ITaxonomyWriter taxonomyWriter;
        protected readonly FacetIndexingParams indexingParams;

        public FacetFields(ITaxonomyWriter taxonomyWriter)
            : this(taxonomyWriter, FacetIndexingParams.DEFAULT)
        {
        }

        public FacetFields(ITaxonomyWriter taxonomyWriter, FacetIndexingParams params_renamed)
        {
            this.taxonomyWriter = taxonomyWriter;
            this.indexingParams = params_renamed;
        }

        protected virtual IDictionary<CategoryListParams, IEnumerable<CategoryPath>> CreateCategoryListMapping(IEnumerable<CategoryPath> categories)
        {
            if (indexingParams.AllCategoryListParams.Count == 1)
            {
                return new Dictionary<CategoryListParams, IEnumerable<CategoryPath>>() { { indexingParams.GetCategoryListParams(null), categories } };
            }

            HashMap<CategoryListParams, IEnumerable<CategoryPath>> categoryLists = new HashMap<CategoryListParams, IEnumerable<CategoryPath>>();
            foreach (CategoryPath cp in categories)
            {
                CategoryListParams clp = indexingParams.GetCategoryListParams(cp);
                List<CategoryPath> list = (List<CategoryPath>)categoryLists[clp];
                if (list == null)
                {
                    list = new List<CategoryPath>();
                    categoryLists[clp] = list;
                }

                list.Add(cp);
            }

            return categoryLists;
        }

        protected virtual IDictionary<String, BytesRef> GetCategoryListData(CategoryListParams categoryListParams, IntsRef ordinals, IEnumerable<CategoryPath> categories)
        {
            return new CountingListBuilder(categoryListParams, indexingParams, taxonomyWriter).Build(ordinals, categories);
        }

        protected virtual DrillDownStream GetDrillDownStream(IEnumerable<CategoryPath> categories)
        {
            return new DrillDownStream(categories, indexingParams);
        }

        protected virtual FieldType DrillDownFieldType()
        {
            return DRILL_DOWN_TYPE;
        }

        protected virtual void AddCountingListData(Document doc, IDictionary<String, BytesRef> categoriesData, string field)
        {
            foreach (KeyValuePair<String, BytesRef> entry in categoriesData)
            {
                doc.Add(new BinaryDocValuesField(field + entry.Key, entry.Value));
            }
        }

        public virtual void AddFields(Document doc, IEnumerable<CategoryPath> categories)
        {
            if (categories == null)
            {
                throw new ArgumentException(@"categories should not be null");
            }

            IDictionary<CategoryListParams, IEnumerable<CategoryPath>> categoryLists = CreateCategoryListMapping(categories);
            IntsRef ordinals = new IntsRef(32);
            foreach (KeyValuePair<CategoryListParams, IEnumerable<CategoryPath>> e in categoryLists)
            {
                CategoryListParams clp = e.Key;
                string field = clp.field;
                ordinals.length = 0;
                int maxNumOrds = 0;
                foreach (CategoryPath cp in e.Value)
                {
                    int ordinal = taxonomyWriter.AddCategory(cp);
                    maxNumOrds += cp.length;
                    if (ordinals.ints.Length < maxNumOrds)
                    {
                        ordinals.Grow(maxNumOrds);
                    }

                    ordinals.ints[ordinals.length++] = ordinal;
                }

                IDictionary<String, BytesRef> categoriesData = GetCategoryListData(clp, ordinals, e.Value);
                AddCountingListData(doc, categoriesData, field);
                DrillDownStream drillDownStream = GetDrillDownStream(e.Value);
                Field drillDown = new Field(field, drillDownStream, DrillDownFieldType());
                doc.Add(drillDown);
            }
        }
    }
}
