using Lucene.Net.Facet.Params;
using Lucene.Net.Facet.Taxonomy;
using Lucene.Net.Index;
using Lucene.Net.Search;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Facet.Search
{
    public class DrillSideways
    {
        protected readonly IndexSearcher searcher;
        protected readonly TaxonomyReader taxoReader;

        public DrillSideways(IndexSearcher searcher, TaxonomyReader taxoReader)
        {
            this.searcher = searcher;
            this.taxoReader = taxoReader;
        }

        private static DrillDownQuery MoveDrillDownOnlyClauses(DrillDownQuery in_renamed, FacetSearchParams fsp)
        {
            ISet<String> facetDims = new HashSet<String>();
            foreach (FacetRequest fr in fsp.facetRequests)
            {
                if (fr.categoryPath.length == 0)
                {
                    throw new ArgumentException(@"all FacetRequests must have CategoryPath with length > 0");
                }

                facetDims.Add(fr.categoryPath.components[0]);
            }

            BooleanClause[] clauses = in_renamed.BooleanQuery.Clauses;
            IDictionary<String, int?> drillDownDims = in_renamed.Dims;
            int startClause;

            if (clauses.Length == drillDownDims.Count)
            {
                startClause = 0;
            }
            else
            {
                startClause = 1;
            }

            List<Query> nonFacetClauses = new List<Query>();
            List<Query> facetClauses = new List<Query>();
            for (int i = startClause; i < clauses.Length; i++)
            {
                Query q = clauses[i].Query;
                string dim = in_renamed.GetDim(q);
                if (!facetDims.Contains(dim))
                {
                    nonFacetClauses.Add(q);
                }
                else
                {
                    facetClauses.Add(q);
                }
            }

            if (nonFacetClauses.Count > 0)
            {
                BooleanQuery newBaseQuery = new BooleanQuery(true);
                if (startClause == 1)
                {
                    newBaseQuery.Add(clauses[0].Query, Occur.MUST);
                }

                foreach (Query q in nonFacetClauses)
                {
                    newBaseQuery.Add(q, Occur.MUST);
                }

                return new DrillDownQuery(fsp.indexingParams, newBaseQuery, facetClauses);
            }
            else
            {
                return in_renamed;
            }
        }

        public virtual DrillSidewaysResult Search(DrillDownQuery query, Collector hitCollector, FacetSearchParams fsp)
        {
            if (query.fip != fsp.indexingParams)
            {
                throw new ArgumentException(@"DrillDownQuery's FacetIndexingParams should match FacetSearchParams'");
            }

            query = MoveDrillDownOnlyClauses(query, fsp);
            var drillDownDims = query.Dims;
            if (drillDownDims.Count == 0)
            {
                FacetsCollector c = FacetsCollector.Create(GetDrillDownAccumulator(fsp));
                searcher.Search(query, MultiCollector.Wrap(hitCollector, c));
                return new DrillSidewaysResult(c.GetFacetResults(), null);
            }

            BooleanQuery ddq = query.BooleanQuery;
            BooleanClause[] clauses = ddq.Clauses;
            Query baseQuery;
            int startClause;
            if (clauses.Length == drillDownDims.Count)
            {
                baseQuery = new MatchAllDocsQuery();
                startClause = 0;
            }
            else
            {
                baseQuery = clauses[0].Query;
                startClause = 1;
            }

            Term[][] drillDownTerms = new Term[clauses.Length - startClause][];
            for (int i = startClause; i < clauses.Length; i++)
            {
                Query q = clauses[i].Query;
                q = ((ConstantScoreQuery)q).Query;
                if (q is TermQuery)
                {
                    drillDownTerms[i - startClause] = new Term[] { ((TermQuery)q).Term };
                }
                else
                {
                    BooleanQuery q2 = (BooleanQuery)q;
                    BooleanClause[] clauses2 = q2.Clauses;
                    drillDownTerms[i - startClause] = new Term[clauses2.Length];
                    for (int j = 0; j < clauses2.Length; j++)
                    {
                        drillDownTerms[i - startClause][j] = ((TermQuery)clauses2[j].Query).Term;
                    }
                }
            }

            FacetsCollector drillDownCollector = FacetsCollector.Create(GetDrillDownAccumulator(fsp));
            FacetsCollector[] drillSidewaysCollectors = new FacetsCollector[drillDownDims.Count];
            int idx = 0;
            foreach (string dim in drillDownDims.Keys)
            {
                List<FacetRequest> requests = new List<FacetRequest>();
                foreach (FacetRequest fr in fsp.facetRequests)
                {
                    if (fr.categoryPath.components[0].Equals(dim))
                    {
                        requests.Add(fr);
                    }
                }

                if (requests.Count == 0)
                {
                    throw new ArgumentException(@"could not find FacetRequest for drill-sideways dimension \" + dim + @"\");
                }

                drillSidewaysCollectors[idx++] = FacetsCollector.Create(GetDrillSidewaysAccumulator(dim, new FacetSearchParams(fsp.indexingParams, requests)));
            }

            DrillSidewaysQuery dsq = new DrillSidewaysQuery(baseQuery, drillDownCollector, drillSidewaysCollectors, drillDownTerms);
            searcher.Search(dsq, hitCollector);
            int numDims = drillDownDims.Count;
            List<FacetResult>[] drillSidewaysResults = new List<FacetResult>[numDims];
            List<FacetResult> drillDownResults = null;
            List<FacetResult> mergedResults = new List<FacetResult>();
            int[] requestUpto = new int[drillDownDims.Count];
            for (int i = 0; i < fsp.facetRequests.Count; i++)
            {
                FacetRequest fr = fsp.facetRequests[i];
                int? dimIndex = drillDownDims[fr.categoryPath.components[0]];
                if (dimIndex == null)
                {
                    if (drillDownResults == null)
                    {
                        drillDownResults = drillDownCollector.GetFacetResults();
                    }

                    mergedResults.Add(drillDownResults[i]);
                }
                else
                {
                    int dim = dimIndex.Value;
                    List<FacetResult> sidewaysResult = drillSidewaysResults[dim];
                    if (sidewaysResult == null)
                    {
                        sidewaysResult = drillSidewaysCollectors[dim].GetFacetResults();
                        drillSidewaysResults[dim] = sidewaysResult;
                    }

                    mergedResults.Add(sidewaysResult[requestUpto[dim]]);
                    requestUpto[dim]++;
                }
            }

            return new DrillSidewaysResult(mergedResults, null);
        }

        public virtual DrillSidewaysResult Search(DrillDownQuery query, Filter filter, FieldDoc after, int topN, Sort sort, bool doDocScores, bool doMaxScore, FacetSearchParams fsp)
        {
            if (filter != null)
            {
                query = new DrillDownQuery(filter, query);
            }

            if (sort != null)
            {
                TopFieldCollector hitCollector = TopFieldCollector.Create(sort, Math.Min(topN, searcher.IndexReader.MaxDoc), after, true, doDocScores, doMaxScore, true);
                DrillSidewaysResult r = Search(query, hitCollector, fsp);
                r.hits = hitCollector.TopDocs();
                return r;
            }
            else
            {
                return Search(after, query, topN, fsp);
            }
        }

        public virtual DrillSidewaysResult Search(ScoreDoc after, DrillDownQuery query, int topN, FacetSearchParams fsp)
        {
            TopScoreDocCollector hitCollector = TopScoreDocCollector.Create(Math.Min(topN, searcher.IndexReader.MaxDoc), after, true);
            DrillSidewaysResult r = Search(query, hitCollector, fsp);
            r.hits = hitCollector.TopDocs();
            return r;
        }

        protected virtual FacetsAccumulator GetDrillDownAccumulator(FacetSearchParams fsp)
        {
            return FacetsAccumulator.Create(fsp, searcher.IndexReader, taxoReader);
        }

        protected virtual FacetsAccumulator GetDrillSidewaysAccumulator(string dim, FacetSearchParams fsp)
        {
            return FacetsAccumulator.Create(fsp, searcher.IndexReader, taxoReader);
        }

        public class DrillSidewaysResult
        {
            public readonly List<FacetResult> facetResults;
            public TopDocs hits;
            
            internal DrillSidewaysResult(List<FacetResult> facetResults, TopDocs hits)
            {
                this.facetResults = facetResults;
                this.hits = hits;
            }
        }
    }
}
