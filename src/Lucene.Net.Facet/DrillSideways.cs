// Lucene version compatibility level 4.8.1
using J2N.Collections.Generic.Extensions;
using Lucene.Net.Diagnostics;
using Lucene.Net.Facet.SortedSet;
using Lucene.Net.Facet.Taxonomy;
using Lucene.Net.Search;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Lucene.Net.Facet
{
    /*
     * Licensed to the Apache Software Foundation (ASF) under one or more
     * contributor license agreements.  See the NOTICE file distributed with
     * this work for additional information regarding copyright ownership.
     * The ASF licenses this file to You under the Apache License, Version 2.0
     * (the "License"); you may not use this file except in compliance with
     * the License.  You may obtain a copy of the License at
     *
     *     http://www.apache.org/licenses/LICENSE-2.0
     *
     * Unless required by applicable law or agreed to in writing, software
     * distributed under the License is distributed on an "AS IS" BASIS,
     * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
     * See the License for the specific language governing permissions and
     * limitations under the License.
     */

    /// <summary>
    /// Computes drill down and sideways counts for the provided
    /// <see cref="DrillDownQuery"/>.  Drill sideways counts include
    /// alternative values/aggregates for the drill-down
    /// dimensions so that a dimension does not disappear after
    /// the user drills down into it.
    /// 
    /// <para> Use one of the static search
    /// methods to do the search, and then get the hits and facet
    /// results from the returned <see cref="DrillSidewaysResult"/>.
    /// 
    /// </para>
    /// <para><b>NOTE</b>: this allocates one <see cref="FacetsCollector"/>
    /// for each drill-down, plus one.  If your
    /// index has high number of facet labels then this will
    /// multiply your memory usage.
    /// 
    /// @lucene.experimental
    /// </para>
    /// </summary>
    public class DrillSideways
    {
        /// <summary>
        /// <see cref="IndexSearcher"/> passed to constructor.
        /// </summary>
        protected readonly IndexSearcher m_searcher;

        /// <summary>
        /// <see cref="TaxonomyReader"/> passed to constructor.
        /// </summary>
        protected readonly TaxonomyReader m_taxoReader;

        /// <summary>
        /// <see cref="SortedSetDocValuesReaderState"/> passed to
        /// constructor; can be <c>null</c>. 
        /// </summary>
        protected readonly SortedSetDocValuesReaderState m_state;

        /// <summary>
        /// <see cref="FacetsConfig"/> passed to constructor.
        /// </summary>
        protected readonly FacetsConfig m_config;

        /// <summary>
        /// Create a new <see cref="DrillSideways"/> instance.
        /// </summary>
        public DrillSideways(IndexSearcher searcher, FacetsConfig config, TaxonomyReader taxoReader)
            : this(searcher, config, taxoReader, null)
        {
        }

        /// <summary>
        /// Create a new <see cref="DrillSideways"/> instance, assuming the categories were
        /// indexed with <see cref="SortedSetDocValuesFacetField"/>. 
        /// </summary>
        public DrillSideways(IndexSearcher searcher, FacetsConfig config, SortedSetDocValuesReaderState state)
            : this(searcher, config, null, state)
        {
        }

        /// <summary>
        /// Create a new <see cref="DrillSideways"/> instance, where some
        /// dimensions were indexed with <see cref="SortedSetDocValuesFacetField"/>
        /// and others were indexed with <see cref="FacetField"/>. 
        /// </summary>
        public DrillSideways(IndexSearcher searcher, FacetsConfig config, TaxonomyReader taxoReader, SortedSetDocValuesReaderState state)
        {
            this.m_searcher = searcher;
            this.m_config = config;
            this.m_taxoReader = taxoReader;
            this.m_state = state;
        }

        /// <summary>
        /// Subclass can override to customize per-dim Facets
        /// impl. 
        /// </summary>
        protected virtual Facets BuildFacetsResult(FacetsCollector drillDowns, FacetsCollector[] drillSideways, string[] drillSidewaysDims)
        {

            Facets drillDownFacets;
            var drillSidewaysFacets = new Dictionary<string, Facets>();

            if (m_taxoReader != null)
            {
                drillDownFacets = new FastTaxonomyFacetCounts(m_taxoReader, m_config, drillDowns);
                if (drillSideways != null)
                {
                    for (int i = 0; i < drillSideways.Length; i++)
                    {
                        drillSidewaysFacets[drillSidewaysDims[i]] = new FastTaxonomyFacetCounts(m_taxoReader, m_config, drillSideways[i]);
                    }
                }
            }
            else
            {
                drillDownFacets = new SortedSetDocValuesFacetCounts(m_state, drillDowns);
                if (drillSideways != null)
                {
                    for (int i = 0; i < drillSideways.Length; i++)
                    {
                        drillSidewaysFacets[drillSidewaysDims[i]] = new SortedSetDocValuesFacetCounts(m_state, drillSideways[i]);
                    }
                }
            }

            if (drillSidewaysFacets.Count == 0)
            {
                return drillDownFacets;
            }
            else
            {
                return new MultiFacets(drillSidewaysFacets, drillDownFacets);
            }
        }

        /// <summary>
        /// Search, collecting hits with a <see cref="ICollector"/>, and
        /// computing drill down and sideways counts.
        /// </summary>
        public virtual DrillSidewaysResult Search(DrillDownQuery query, ICollector hitCollector)
        {

            IDictionary<string, int> drillDownDims = query.Dims;

            FacetsCollector drillDownCollector = new FacetsCollector();

            if (drillDownDims.Count == 0)
            {
                // There are no drill-down dims, so there is no
                // drill-sideways to compute:
                m_searcher.Search(query, MultiCollector.Wrap(hitCollector, drillDownCollector));
                return new DrillSidewaysResult(BuildFacetsResult(drillDownCollector, null, null), null);
            }

            BooleanQuery ddq = query.BooleanQuery;
            BooleanClause[] clauses = ddq.GetClauses();

            Query baseQuery;
            int startClause;
            if (clauses.Length == drillDownDims.Count)
            {
                // TODO: we could optimize this pure-browse case by
                // making a custom scorer instead:
                baseQuery = new MatchAllDocsQuery();
                startClause = 0;
            }
            else
            {
                if (Debugging.AssertsEnabled) Debugging.Assert(clauses.Length == 1 + drillDownDims.Count);
                baseQuery = clauses[0].Query;
                startClause = 1;
            }

            FacetsCollector[] drillSidewaysCollectors = new FacetsCollector[drillDownDims.Count];
            for (int i = 0; i < drillSidewaysCollectors.Length; i++)
            {
                drillSidewaysCollectors[i] = new FacetsCollector();
            }

            Query[] drillDownQueries = new Query[clauses.Length - startClause];
            for (int i = startClause; i < clauses.Length; i++)
            {
                drillDownQueries[i - startClause] = clauses[i].Query;
            }
            DrillSidewaysQuery dsq = new DrillSidewaysQuery(baseQuery, drillDownCollector, drillSidewaysCollectors, drillDownQueries, ScoreSubDocsAtOnce);
            m_searcher.Search(dsq, hitCollector);

            return new DrillSidewaysResult(BuildFacetsResult(drillDownCollector, drillSidewaysCollectors, drillDownDims.Keys.ToArray()), null);
        }

        /// <summary>
        /// Search, sorting by <see cref="Sort"/>, and computing
        /// drill down and sideways counts.
        /// </summary>
        public virtual DrillSidewaysResult Search(DrillDownQuery query, Filter filter, FieldDoc after, int topN, Sort sort, bool doDocScores, bool doMaxScore)
        {
            if (filter != null)
            {
                query = new DrillDownQuery(m_config, filter, query);
            }
            if (sort != null)
            {
                int limit = m_searcher.IndexReader.MaxDoc;
                if (limit == 0)
                {
                    limit = 1; // the collector does not alow numHits = 0
                }
                topN = Math.Min(topN, limit);
                TopFieldCollector hitCollector = TopFieldCollector.Create(sort, topN, after, true, doDocScores, doMaxScore, true);
                DrillSidewaysResult r = Search(query, hitCollector);
                return new DrillSidewaysResult(r.Facets, hitCollector.GetTopDocs());
            }
            else
            {
                return Search(after, query, topN);
            }
        }

        /// <summary>
        /// Search, sorting by score, and computing
        /// drill down and sideways counts.
        /// </summary>
        public virtual DrillSidewaysResult Search(DrillDownQuery query, int topN)
        {
            return Search(null, query, topN);
        }

        /// <summary>
        /// Search, sorting by score, and computing
        /// drill down and sideways counts.
        /// </summary>
        public virtual DrillSidewaysResult Search(ScoreDoc after, DrillDownQuery query, int topN)
        {
            int limit = m_searcher.IndexReader.MaxDoc;
            if (limit == 0)
            {
                limit = 1; // the collector does not alow numHits = 0
            }
            topN = Math.Min(topN, limit);
            TopScoreDocCollector hitCollector = TopScoreDocCollector.Create(topN, after, true);
            DrillSidewaysResult r = Search(query, hitCollector);
            return new DrillSidewaysResult(r.Facets, hitCollector.GetTopDocs());
        }

        /// <summary>
        /// Override this and return true if your collector
        /// (e.g., <see cref="Join.ToParentBlockJoinCollector"/>) expects all
        /// sub-scorers to be positioned on the document being
        /// collected.  This will cause some performance loss;
        /// default is <c>false</c>.  Note that if you return true from
        /// this method (in a subclass) be sure your collector
        /// also returns <c>false</c> from <see cref="ICollector.AcceptsDocsOutOfOrder"/>: 
        /// this will trick <see cref="BooleanQuery"/> into also scoring all subDocs at
        /// once. 
        /// </summary>
        protected virtual bool ScoreSubDocsAtOnce => false;
    }

    /// <summary>
    /// Result of a drill sideways search, including the
    /// <see cref="Facet.Facets"/> and <see cref="TopDocs"/>. 
    /// </summary>
    public class DrillSidewaysResult
    {
        /// <summary>
        /// Combined drill down &amp; sideways results.
        /// </summary>
        public Facets Facets { get; private set; }

        /// <summary>
        /// Hits.
        /// </summary>
        public TopDocs Hits { get; private set; }

        /// <summary>
        /// Sole constructor.
        /// </summary>
        public DrillSidewaysResult(Facets facets, TopDocs hits)
        {
            this.Facets = facets;
            this.Hits = hits;
        }
    }
}