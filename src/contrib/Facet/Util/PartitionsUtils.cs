using Lucene.Net.Facet.Params;
using Lucene.Net.Facet.Taxonomy;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Facet.Util
{
    public static class PartitionsUtils
    {
        public const string PART_NAME_PREFIX = "$part";

        public static int PartitionSize(FacetIndexingParams indexingParams, TaxonomyReader taxonomyReader)
        {
            return Math.Min(indexingParams.PartitionSize, taxonomyReader.Size);
        }

        public static int PartitionNumber(FacetIndexingParams iParams, int ordinal)
        {
            return ordinal / iParams.PartitionSize;
        }

        public static string PartitionNameByOrdinal(FacetIndexingParams iParams, int ordinal)
        {
            int partition = PartitionNumber(iParams, ordinal);
            return PartitionName(partition);
        }

        public static string PartitionName(int partition)
        {
            if (partition == 0)
            {
                return "";
            }

            return PART_NAME_PREFIX + partition.ToString();
        }
    }
}
