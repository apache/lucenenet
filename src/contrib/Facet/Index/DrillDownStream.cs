using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Tokenattributes;
using Lucene.Net.Facet.Params;
using Lucene.Net.Facet.Taxonomy;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Facet.Index
{
    public class DrillDownStream : TokenStream
    {
        private readonly FacetIndexingParams indexingParams;
        private readonly IEnumerator<CategoryPath> categories;
        private readonly ICharTermAttribute termAttribute;
        private CategoryPath current;
        private bool isParent;

        public DrillDownStream(IEnumerable<CategoryPath> categories, FacetIndexingParams indexingParams)
        {
            termAttribute = AddAttribute<ICharTermAttribute>();
            this.categories = categories.GetEnumerator();
            this.indexingParams = indexingParams;
        }

        protected virtual void AddAdditionalAttributes(CategoryPath category, bool isParent)
        {
        }

        public override bool IncrementToken()
        {
            if (current.length == 0)
            {
                if (!categories.MoveNext())
                {
                    return false;
                }

                current = categories.Current;
                termAttribute.ResizeBuffer(current.FullPathLength());
                isParent = false;
            }

            int nChars = indexingParams.DrillDownTermText(current, termAttribute.Buffer);
            termAttribute.SetLength(nChars);
            AddAdditionalAttributes(current, isParent);
            current = current.Subpath(current.length - 1);
            isParent = true;
            return true;
        }

        public override void Reset()
        {
            // TODO: validate this logic
            categories.MoveNext();
            current = categories.Current;
            termAttribute.ResizeBuffer(current.FullPathLength());
            isParent = false;
        }
    }
}
