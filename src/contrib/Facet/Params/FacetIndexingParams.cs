using Lucene.Net.Facet.Taxonomy;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Facet.Params
{
    public class FacetIndexingParams
    {
        protected static readonly CategoryListParams DEFAULT_CATEGORY_LIST_PARAMS = new CategoryListParams();
        public static readonly FacetIndexingParams DEFAULT = new FacetIndexingParams();
        public static readonly char DEFAULT_FACET_DELIM_CHAR = '';
        private readonly int partitionSize = int.MaxValue;
        protected readonly CategoryListParams clParams;

        public FacetIndexingParams()
            : this(DEFAULT_CATEGORY_LIST_PARAMS)
        {
        }

        public FacetIndexingParams(CategoryListParams categoryListParams)
        {
            clParams = categoryListParams;
        }

        public virtual CategoryListParams GetCategoryListParams(CategoryPath category)
        {
            return clParams;
        }

        public virtual int DrillDownTermText(CategoryPath path, char[] buffer)
        {
            return path.CopyFullPath(buffer, 0, FacetDelimChar);
        }

        public virtual int PartitionSize
        {
            get
            {
                return partitionSize;
            }
        }

        public virtual IList<CategoryListParams> AllCategoryListParams
        {
            get
            {
                return new[] { clParams };
            }
        }

        public override int GetHashCode()
        {
            int prime = 31;
            int result = 1;
            result = prime * result + ((clParams == null) ? 0 : clParams.GetHashCode());
            result = prime * result + partitionSize;
            foreach (CategoryListParams clp in AllCategoryListParams)
            {
                result ^= clp.GetHashCode();
            }

            return result;
        }

        public override bool Equals(Object obj)
        {
            if (this == obj)
            {
                return true;
            }

            if (obj == null)
            {
                return false;
            }

            if (!(obj is FacetIndexingParams))
            {
                return false;
            }

            FacetIndexingParams other = (FacetIndexingParams)obj;
            if (clParams == null)
            {
                if (other.clParams != null)
                {
                    return false;
                }
            }
            else if (!clParams.Equals(other.clParams))
            {
                return false;
            }

            if (partitionSize != other.partitionSize)
            {
                return false;
            }

            IEnumerable<CategoryListParams> cLs = AllCategoryListParams;
            IEnumerable<CategoryListParams> otherCLs = other.AllCategoryListParams;
            return cLs.Equals(otherCLs);
        }

        public virtual char FacetDelimChar
        {
            get
            {
                return DEFAULT_FACET_DELIM_CHAR;
            }
        }
    }
}
