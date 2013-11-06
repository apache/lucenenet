using Lucene.Net.Facet.Params;
using Lucene.Net.Facet.Search;
using Lucene.Net.Facet.Taxonomy;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Facet.Sampling
{
    internal class TakmiSampleFixer : ISampleFixer
    {
        private TaxonomyReader taxonomyReader;
        private IndexReader indexReader;
        private FacetSearchParams searchParams;

        public TakmiSampleFixer(IndexReader indexReader, TaxonomyReader taxonomyReader, FacetSearchParams searchParams)
        {
            this.indexReader = indexReader;
            this.taxonomyReader = taxonomyReader;
            this.searchParams = searchParams;
        }

        public void FixResult(IScoredDocIDs origDocIds, FacetResult fres)
        {
            FacetResultNode topRes = fres.FacetResultNode;
            FixResultNode(topRes, origDocIds);
        }

        private void FixResultNode(FacetResultNode facetResNode, IScoredDocIDs docIds)
        {
            Recount(facetResNode, docIds);
            foreach (FacetResultNode frn in facetResNode.subResults)
            {
                FixResultNode(frn, docIds);
            }
        }

        private void Recount(FacetResultNode fresNode, IScoredDocIDs docIds)
        {
            if (fresNode.label == null)
            {
                fresNode.label = taxonomyReader.GetPath(fresNode.ordinal);
            }

            CategoryPath catPath = fresNode.label;
            Term drillDownTerm = DrillDownQuery.Term(searchParams.indexingParams, catPath);
            IBits liveDocs = MultiFields.GetLiveDocs(indexReader);
            int updatedCount = CountIntersection(MultiFields.GetTermDocsEnum(indexReader, liveDocs, drillDownTerm.Field, drillDownTerm.Bytes, 0), docIds.Iterator());
            fresNode.value = updatedCount;
        }

        private static int CountIntersection(DocsEnum p1, IScoredDocIDsIterator p2)
        {
            if (p1 == null || p1.NextDoc() == DocIdSetIterator.NO_MORE_DOCS)
            {
                return 0;
            }

            if (!p2.Next())
            {
                return 0;
            }

            int d1 = p1.DocID;
            int d2 = p2.DocID;
            int count = 0;
            for (; ; )
            {
                if (d1 == d2)
                {
                    ++count;
                    if (p1.NextDoc() == DocIdSetIterator.NO_MORE_DOCS)
                    {
                        break;
                    }

                    d1 = p1.DocID;
                    if (!Advance(p2, d1))
                    {
                        break;
                    }

                    d2 = p2.DocID;
                }
                else if (d1 < d2)
                {
                    if (p1.Advance(d2) == DocIdSetIterator.NO_MORE_DOCS)
                    {
                        break;
                    }

                    d1 = p1.DocID;
                }
                else
                {
                    if (!Advance(p2, d1))
                    {
                        break;
                    }

                    d2 = p2.DocID;
                }
            }

            return count;
        }

        private static bool Advance(IScoredDocIDsIterator iterator, int targetDoc)
        {
            while (iterator.Next())
            {
                if (iterator.DocID >= targetDoc)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
