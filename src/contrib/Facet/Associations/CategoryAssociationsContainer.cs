using Lucene.Net.Facet.Taxonomy;
using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Facet.Associations
{
    public class CategoryAssociationsContainer : IEnumerable<CategoryPath>
    {
        private readonly HashMap<CategoryPath, ICategoryAssociation> categoryAssociations = new HashMap<CategoryPath, ICategoryAssociation>();

        public virtual void SetAssociation(CategoryPath category, ICategoryAssociation association)
        {
            categoryAssociations[category] = association;
        }

        public virtual ICategoryAssociation GetAssociation(CategoryPath category)
        {
            return categoryAssociations[category];
        }

        public IEnumerator<CategoryPath> GetEnumerator()
        {
            return categoryAssociations.Keys.GetEnumerator();
        }

        public virtual void Clear()
        {
            categoryAssociations.Clear();
        }

        public override string ToString()
        {
            return categoryAssociations.ToString();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
