using Lucene.Net.Facet.Encoding;
using Lucene.Net.Facet.Params;
using Lucene.Net.Index;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Facet.Search
{
    public sealed class FastCountingFacetsAggregator : IntRollupFacetsAggregator
    {
        private readonly BytesRef buf = new BytesRef(32);

        internal static bool VerifySearchParams(FacetSearchParams fsp)
        {
            foreach (FacetRequest fr in fsp.facetRequests)
            {
                CategoryListParams clp = fsp.indexingParams.GetCategoryListParams(fr.categoryPath);
                if (clp.CreateEncoder().CreateMatchingDecoder().GetType() != typeof(DGapVInt8IntDecoder))
                {
                    return false;
                }
            }

            return true;
        }

        public override void Aggregate(FacetsCollector.MatchingDocs matchingDocs, CategoryListParams clp, FacetArrays facetArrays)
        {
            BinaryDocValues dv = matchingDocs.context.AtomicReader.GetBinaryDocValues(clp.field);
            if (dv == null)
            {
                return;
            }

            int length = matchingDocs.bits.Length;
            int[] counts = facetArrays.GetIntArray();
            int doc = 0;
            while (doc < length && (doc = matchingDocs.bits.NextSetBit(doc)) != -1)
            {
                dv.Get(doc, buf);
                if (buf.length > 0)
                {
                    int upto = buf.offset + buf.length;
                    int ord = 0;
                    int offset = buf.offset;
                    int prev = 0;
                    while (offset < upto)
                    {
                        sbyte b = buf.bytes[offset++];
                        if (b >= 0)
                        {
                            prev = ord = ((ord << 7) | (byte)b) + prev;
                            ++counts[ord];
                            ord = 0;
                        }
                        else
                        {
                            ord = (ord << 7) | (b & 0x7F);
                        }
                    }
                }

                ++doc;
            }
        }
    }
}
