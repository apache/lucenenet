using Lucene.Net.Facet.Taxonomy;
using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Facet.Params
{
    public class PerDimensionIndexingParams : FacetIndexingParams
    {
        private readonly IDictionary<String, CategoryListParams> clParamsMap;

        public PerDimensionIndexingParams(IDictionary<CategoryPath, CategoryListParams> paramsMap)
            : this(paramsMap, DEFAULT_CATEGORY_LIST_PARAMS)
        {
        }

        public PerDimensionIndexingParams(IDictionary<CategoryPath, CategoryListParams> paramsMap, 
            CategoryListParams categoryListParams)
            : base(categoryListParams)
        {
            clParamsMap = new HashMap<String, CategoryListParams>();
            foreach (KeyValuePair<CategoryPath, CategoryListParams> e in paramsMap)
            {
                clParamsMap[e.Key.components[0]] = e.Value;
            }
        }

        public override IList<CategoryListParams> AllCategoryListParams
        {
            get
            {
                List<CategoryListParams> vals = new List<CategoryListParams>(clParamsMap.Values);
                vals.Add(clParams);
                return vals;
            }
        }

        public override CategoryListParams GetCategoryListParams(CategoryPath category)
        {
            if (category != null)
            {
                CategoryListParams clParams = clParamsMap[category.components[0]];
                if (clParams != null)
                {
                    return clParams;
                }
            }

            return null;
        }
    }
}
