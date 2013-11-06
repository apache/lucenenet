using Lucene.Net.Facet.Params;
using Lucene.Net.Facet.Search;
using Lucene.Net.Index;
using Lucene.Net.Support;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Facet.Associations
{
    public class SumFloatAssociationFacetsAggregator : IFacetsAggregator
    {
        private readonly BytesRef bytes = new BytesRef(32);

        public void Aggregate(FacetsCollector.MatchingDocs matchingDocs, CategoryListParams clp, FacetArrays facetArrays)
        {
            BinaryDocValues dv = matchingDocs.context.AtomicReader.GetBinaryDocValues(clp.field + CategoryFloatAssociation.ASSOCIATION_LIST_ID);
            if (dv == null)
            {
                return;
            }

            int length = matchingDocs.bits.Length;
            float[] values = facetArrays.GetFloatArray();
            int doc = 0;
            while (doc < length && (doc = matchingDocs.bits.NextSetBit(doc)) != -1)
            {
                dv.Get(doc, bytes);
                if (bytes.length == 0)
                {
                    continue;
                }

                int bytesUpto = bytes.offset + bytes.length;
                int pos = bytes.offset;
                while (pos < bytesUpto)
                {
                    int ordinal = ((bytes.bytes[pos++] & 0xFF) << 24) | ((bytes.bytes[pos++] & 0xFF) << 16) | ((bytes.bytes[pos++] & 0xFF) << 8) | (bytes.bytes[pos++] & 0xFF);
                    int value = ((bytes.bytes[pos++] & 0xFF) << 24) | ((bytes.bytes[pos++] & 0xFF) << 16) | ((bytes.bytes[pos++] & 0xFF) << 8) | (bytes.bytes[pos++] & 0xFF);
                    values[ordinal] += Number.IntBitsToFloat(value);
                }

                ++doc;
            }
        }

        public bool RequiresDocScores
        {
            get
            {
                return false;
            }
        }

        public void RollupValues(FacetRequest fr, int ordinal, int[] children, int[] siblings, FacetArrays facetArrays)
        {
        }
    }
}
