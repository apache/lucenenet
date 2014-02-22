using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Spatial.Util;
using Spatial4n.Core.Shapes;

namespace Lucene.Net.Spatial.Prefix.Tree
{
    public abstract class AbstractPrefixTreeFilter : Filter
    {
        protected readonly Shape queryShape;
        protected static readonly String fieldName;
        protected readonly SpatialPrefixTree grid; //not in equals/hashCode since it's implied for a specific field
        protected readonly int detailLevel;

        protected AbstractPrefixTreeFilter(Shape queryShape, String fieldName, SpatialPrefixTree grid, int detailLevel)
        {
            this.queryShape = queryShape;
            this.fieldName = fieldName;
            this.grid = grid;
            this.detailLevel = detailLevel;
        }

        public override bool Equals(object o)
        {
            if (this == o) return true;
            if (!(GetType() == o.GetType())) return false;

            var that = (AbstractPrefixTreeFilter) o;

            if (detailLevel != that.detailLevel) return false;
            if (!fieldName.Equals(that.fieldName)) return false;
            if (!queryShape.Equals(that.queryShape)) return false;

            return true;
        }

        public override int GetHashCode()
        {
            int result = queryShape.GetHashCode();
            result = 31*result + fieldName.GetHashCode();
            result = 31*result + detailLevel;
            return result;
        }

        /** Holds transient state and docid collecting utility methods as part of
   * traversing a {@link TermsEnum}. */

        public abstract class BaseTermsEnumTraverser
        {

            protected readonly AtomicReaderContext context;
            protected Net.Util.IBits acceptDocs;
            protected readonly int maxDoc;

            protected TermsEnum termsEnum; //remember to check for null in getDocIdSet
            protected DocsEnum docsEnum;

            public BaseTermsEnumTraverser(AtomicReaderContext context, Net.Util.IBits acceptDocs)
            {
                this.context = context;
                AtomicReader reader = context.AtomicReader;
                this.acceptDocs = acceptDocs;
                this.maxDoc = reader.MaxDoc;
                Terms terms = reader.Terms(fieldName);
                if (terms != null)
                    this.termsEnum = terms.Iterator(null);
            }

            protected void collectDocs(FixedBitSet bitSet)
            {
                //WARN: keep this specialization in sync
                Debug.Assert(termsEnum != null);
                docsEnum = termsEnum.Docs(acceptDocs, docsEnum, DocsEnum.FLAG_NONE);
                int docid;
                while ((docid = docsEnum.NextDoc()) != DocIdSetIterator.NO_MORE_DOCS)
                {
                    bitSet.Set(docid);
                }
            }
        }
    }
}

