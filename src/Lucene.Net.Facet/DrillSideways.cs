using Lucene.Net.Facet.SortedSet;
using Lucene.Net.Facet.Taxonomy;
using Lucene.Net.Search;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

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
    /// <seealso cref="DrillDownQuery"/>.  Drill sideways counts include
    /// alternative values/aggregates for the drill-down
    /// dimensions so that a dimension does not disappear after
    /// the user drills down into it.
    /// 
    /// <para> Use one of the static search
    /// methods to do the search, and then get the hits and facet
    /// results from the returned <seealso cref="DrillSidewaysResult"/>.
    /// 
    /// </para>
    /// <para><b>NOTE</b>: this allocates one {@link
    /// FacetsCollector} for each drill-down, plus one.  If your
    /// index has high number of facet labels then this will
    /// multiply your memory usage.
    /// 
    /// @lucene.experimental
    /// </para>
    /// </summary>
    public class DrillSideways
    {
        /// <summary>
        /// <seealso cref="IndexSearcher"/> passed to constructor. </summary>
        protected internal readonly IndexSearcher searcher;

        /// <summary>
        /// <seealso cref="TaxonomyReader"/> passed to constructor. </summary>
        protected internal readonly TaxonomyReader taxoReader;

        /// <summary>
        /// <seealso cref="SortedSetDocValuesReaderState"/> passed to
        ///  constructor; can be null. 
        /// </summary>
        protected internal readonly SortedSetDocValuesReaderState state;

        /// <summary>
        /// <seealso cref="FacetsConfig"/> passed to constructor. </summary>
        protected internal readonly FacetsConfig config;

        /// <summary>
        /// Create a new {@code DrillSideways} instance. </summary>
        public DrillSideways(IndexSearcher searcher, FacetsConfig config, TaxonomyReader taxoReader)
            : this(searcher, config, taxoReader, null)
        {
        }

        /// <summary>
        /// Create a new {@code DrillSideways} instance, assuming the categories were
        ///  indexed with <seealso cref="SortedSetDocValuesFacetField"/>. 
        /// </summary>
        public DrillSideways(IndexSearcher searcher, FacetsConfig config, SortedSetDocValuesReaderState state)
            : this(searcher, config, null, state)
        {
        }

        /// <summary>
        /// Create a new {@code DrillSideways} instance, where some
        ///  dimensions were indexed with {@link
        ///  SortedSetDocValuesFacetField} and others were indexed
        ///  with <seealso cref="FacetField"/>. 
        /// </summary>
        public DrillSideways(IndexSearcher searcher, FacetsConfig config, TaxonomyReader taxoReader, SortedSetDocValuesReaderState state)
        {
            this.searcher = searcher;
            this.config = config;
            this.taxoReader = taxoReader;
            this.state = state;
        }

        /// <summary>
        /// Subclass can override to customize per-dim Facets
        ///  impl. 
        /// </summary>
        protected virtual Facets BuildFacetsResult(FacetsCollector drillDowns, FacetsCollector[] drillSideways, string[] drillSidewaysDims)
        {

            Facets drillDownFacets;
            var drillSidewaysFacets = new Dictionary<string, Facets>();

            if (taxoReader != null)
            {
                drillDownFacets = new FastTaxonomyFacetCounts(taxoReader, config, drillDowns);
                if (drillSideways != null)
                {
                    for (int i = 0; i < drillSideways.Length; i++)
                    {
                        drillSidewaysFacets[drillSidewaysDims[i]] = new FastTaxonomyFacetCounts(taxoReader, config, drillSideways[i]);
                    }
                }
            }
            else
            {
                drillDownFacets = new SortedSetDocValuesFacetCounts(state, drillDowns);
                if (drillSideways != null)
                {
                    for (int i = 0; i < drillSideways.Length; i++)
                    {
                        drillSidewaysFacets[drillSidewaysDims[i]] = new SortedSetDocValuesFacetCounts(state, drillSideways[i]);
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
        /// Search, collecting hits with a <seealso cref="Collector"/>, and
        /// computing drill down and sideways counts.
        /// </summary>
        public virtual DrillSidewaysResult Search(DrillDownQuery query, Collector hitCollector)
        {

            IDictionary<string, int?> drillDownDims = query.Dims;

            FacetsCollector drillDownCollector = new FacetsCollector();

            if (drillDownDims.Count == 0)
            {
                // There are no drill-down dims, so there is no
                // drill-sideways to compute:
                searcher.Search(query, MultiCollector.Wrap(hitCollector, drillDownCollector));
                return new DrillSidewaysResult(BuildFacetsResult(drillDownCollector, null, null), null);
            }

            BooleanQuery ddq = query.BooleanQuery;
            BooleanClause[] clauses = ddq.Clauses;

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
                Debug.Assert(clauses.Length == 1 + drillDownDims.Count);
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
            DrillSidewaysQuery dsq = new DrillSidewaysQuery(baseQuery, drillDownCollector, drillSidewaysCollectors, drillDownQueries, ScoreSubDocsAtOnce());
            searcher.Search(dsq, hitCollector);

            return new DrillSidewaysResult(BuildFacetsResult(drillDownCollector, drillSidewaysCollectors, drillDownDims.Keys.ToArray()), null);
        }

        /// <summary>
        /// Search, sorting by <seealso cref="Sort"/>, and computing
        /// drill down and sideways counts.
        /// </summary>
        public virtual DrillSidewaysResult Search(DrillDownQuery query, Filter filter, FieldDoc after, int topN, Sort sort, bool doDocScores, bool doMaxScore)
        {
            if (filter != null)
            {
                query = new DrillDownQuery(config, filter, query);
            }
            if (sort != null)
            {
                int limit = searcher.IndexReader.MaxDoc;
                if (limit == 0)
                {
                    limit = 1; // the collector does not alow numHits = 0
                }
                topN = Math.Min(topN, limit);
                TopFieldCollector hitCollector = TopFieldCollector.Create(sort, topN, after, true, doDocScores, doMaxScore, true);
                DrillSidewaysResult r = Search(query, hitCollector);
                return new DrillSidewaysResult(r.Facets, hitCollector.TopDocs());
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
            int limit = searcher.IndexReader.MaxDoc;
            if (limit == 0)
            {
                limit = 1; // the collector does not alow numHits = 0
            }
            topN = Math.Min(topN, limit);
            TopScoreDocCollector hitCollector = TopScoreDocCollector.Create(topN, after, true);
            DrillSidewaysResult r = Search(query, hitCollector);
            return new DrillSidewaysResult(r.Facets, hitCollector.TopDocs());
        }

        /// <summary>
        /// Override this and return true if your collector
        ///  (e.g., {@code ToParentBlockJoinCollector}) expects all
        ///  sub-scorers to be positioned on the document being
        ///  collected.  This will cause some performance loss;
        ///  default is false.  Note that if you return true from
        ///  this method (in a subclass) be sure your collector
        ///  also returns false from {@link
        ///  Collector#acceptsDocsOutOfOrder}: this will trick
        ///  {@code BooleanQuery} into also scoring all subDocs at
        ///  once. 
        /// </summary>
        protected virtual bool ScoreSubDocsAtOnce()
        {
            return false;
        }

        /// <summary>
        /// Result of a drill sideways search, including the
        ///  <seealso crTopDocsetss"/> and <seealso cref="Lucene"/>. 
        /// </summary>
        public class DrillSidewaysResult
        {
            /// <summary>
            /// Combined drill down & sideways results. </summary>
            public readonly Facets Facets;

            /// <summary>
            /// Hits. </summary>
            public readonly TopDocs Hits;

            /// <summary>
            /// Sole constructor. </summary>
            public DrillSidewaysResult(Facets facets, TopDocs hits)
            {
                this.Facets = facets;
                this.Hits = hits;
            }
        }
    }
}