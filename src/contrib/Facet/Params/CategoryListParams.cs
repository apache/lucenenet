using Lucene.Net.Facet.Encoding;
using Lucene.Net.Facet.Search;
using Lucene.Net.Facet.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Facet.Params
{
    public class CategoryListParams
    {
        public enum OrdinalPolicy
        {
            NO_PARENTS,
            ALL_PARENTS,
            ALL_BUT_DIMENSION
        }

        public static readonly string DEFAULT_FIELD = "$facets";
        public static readonly OrdinalPolicy DEFAULT_ORDINAL_POLICY = OrdinalPolicy.ALL_BUT_DIMENSION;
        public readonly string field;
        private readonly int hashCode;

        public CategoryListParams()
            : this(DEFAULT_FIELD)
        {
        }

        public CategoryListParams(string field)
        {
            this.field = field;
            this.hashCode = field.GetHashCode();
        }

        public virtual IntEncoder CreateEncoder()
        {
            return new SortingIntEncoder(new UniqueValuesIntEncoder(new DGapVInt8IntEncoder()));
        }

        public override bool Equals(Object o)
        {
            if (o == this)
            {
                return true;
            }

            if (!(o is CategoryListParams))
            {
                return false;
            }

            CategoryListParams other = (CategoryListParams)o;
            if (hashCode != other.hashCode)
            {
                return false;
            }

            return field.Equals(other.field);
        }

        public override int GetHashCode()
        {
            return hashCode;
        }

        public virtual ICategoryListIterator CreateCategoryListIterator(int partition)
        {
            string categoryListTermStr = PartitionsUtils.PartitionName(partition);
            string docValuesField = field + categoryListTermStr;
            return new DocValuesCategoryListIterator(docValuesField, CreateEncoder().CreateMatchingDecoder());
        }

        public virtual OrdinalPolicy GetOrdinalPolicy(string dimension)
        {
            return DEFAULT_ORDINAL_POLICY;
        }

        public override string ToString()
        {
            return @"field=" + field + @" encoder=" + CreateEncoder() + @" ordinalPolicy=" + GetOrdinalPolicy(null);
        }
    }
}
