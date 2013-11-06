using Lucene.Net.Documents;
using Lucene.Net.Facet.Index;
using Lucene.Net.Facet.Params;
using Lucene.Net.Facet.Taxonomy;
using Lucene.Net.Index;
using Lucene.Net.Support;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Facet.Associations
{
    public class AssociationsFacetFields : FacetFields
    {
        private static readonly FieldType DRILL_DOWN_TYPE = new FieldType(TextField.TYPE_NOT_STORED);

        static AssociationsFacetFields()
        {
            DRILL_DOWN_TYPE.IndexOptions = FieldInfo.IndexOptions.DOCS_AND_FREQS_AND_POSITIONS;
            DRILL_DOWN_TYPE.Freeze();
        }

        public AssociationsFacetFields(ITaxonomyWriter taxonomyWriter)
            : base(taxonomyWriter)
        {
        }

        public AssociationsFacetFields(ITaxonomyWriter taxonomyWriter, FacetIndexingParams params_renamed)
            : base(taxonomyWriter, params_renamed)
        {
        }

        protected override IDictionary<CategoryListParams, IEnumerable<CategoryPath>> CreateCategoryListMapping(IEnumerable<CategoryPath> categories)
        {
            CategoryAssociationsContainer categoryAssociations = (CategoryAssociationsContainer)categories;
            HashMap<CategoryListParams, IEnumerable<CategoryPath>> categoryLists = new HashMap<CategoryListParams, IEnumerable<CategoryPath>>();
            foreach (CategoryPath cp in categories)
            {
                CategoryListParams clp = indexingParams.GetCategoryListParams(cp);
                CategoryAssociationsContainer clpContainer = (CategoryAssociationsContainer)categoryLists[clp];
                if (clpContainer == null)
                {
                    clpContainer = new CategoryAssociationsContainer();
                    categoryLists[clp] = clpContainer;
                }

                clpContainer.SetAssociation(cp, categoryAssociations.GetAssociation(cp));
            }

            return categoryLists;
        }

        protected override IDictionary<string, BytesRef> GetCategoryListData(CategoryListParams categoryListParams, IntsRef ordinals, IEnumerable<CategoryPath> categories)
        {
            AssociationsListBuilder associations = new AssociationsListBuilder((CategoryAssociationsContainer)categories);
            return associations.Build(ordinals, categories);
        }

        protected override DrillDownStream GetDrillDownStream(IEnumerable<CategoryPath> categories)
        {
            return new AssociationsDrillDownStream((CategoryAssociationsContainer)categories, indexingParams);
        }

        protected override FieldType DrillDownFieldType()
        {
            return DRILL_DOWN_TYPE;
        }

        public override void AddFields(Document doc, IEnumerable<CategoryPath> categories)
        {
            if (!(categories is CategoryAssociationsContainer))
            {
                throw new ArgumentException(@"categories must be of type " + typeof(CategoryAssociationsContainer).Name);
            }

            base.AddFields(doc, categories);
        }
    }
}
