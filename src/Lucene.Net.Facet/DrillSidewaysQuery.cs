// Lucene version compatibility level 4.8.1
using Lucene.Net.Support;
using System;

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

    using AtomicReaderContext = Lucene.Net.Index.AtomicReaderContext;
    using IBits = Lucene.Net.Util.IBits;
    using BulkScorer = Lucene.Net.Search.BulkScorer;
    using ICollector = Lucene.Net.Search.ICollector;
    using DocIdSet = Lucene.Net.Search.DocIdSet;
    using DocIdSetIterator = Lucene.Net.Search.DocIdSetIterator;
    using Explanation = Lucene.Net.Search.Explanation;
    using Filter = Lucene.Net.Search.Filter;
    using IndexReader = Lucene.Net.Index.IndexReader;
    using IndexSearcher = Lucene.Net.Search.IndexSearcher;
    using Query = Lucene.Net.Search.Query;
    using Scorer = Lucene.Net.Search.Scorer;
    using Weight = Lucene.Net.Search.Weight;

    /// <summary>
    /// Only purpose is to punch through and return a
    /// <see cref="DrillSidewaysScorer"/> 
    /// </summary>
    internal class DrillSidewaysQuery : Query
    {
        internal readonly Query baseQuery;
        internal readonly ICollector drillDownCollector;
        internal readonly ICollector[] drillSidewaysCollectors;
        internal readonly Query[] drillDownQueries;
        internal readonly bool scoreSubDocsAtOnce;

        internal DrillSidewaysQuery(Query baseQuery, ICollector drillDownCollector,
            ICollector[] drillSidewaysCollectors, Query[] drillDownQueries, bool scoreSubDocsAtOnce)
        {
            this.baseQuery = baseQuery;
            this.drillDownCollector = drillDownCollector;
            this.drillSidewaysCollectors = drillSidewaysCollectors;
            this.drillDownQueries = drillDownQueries;
            this.scoreSubDocsAtOnce = scoreSubDocsAtOnce;
        }

        public override string ToString(string field)
        {
            return nameof(DrillSidewaysQuery);
        }

        public override Query Rewrite(IndexReader reader)
        {
            Query newQuery = baseQuery;
            while (true)
            {
                Query rewrittenQuery = newQuery.Rewrite(reader);
                if (rewrittenQuery == newQuery)
                {
                    break;
                }
                newQuery = rewrittenQuery;
            }
            if (newQuery == baseQuery)
            {
                return this;
            }
            else
            {
                return new DrillSidewaysQuery(newQuery, drillDownCollector, drillSidewaysCollectors, drillDownQueries, scoreSubDocsAtOnce);
            }
        }

        public override Weight CreateWeight(IndexSearcher searcher)
        {
            Weight baseWeight = baseQuery.CreateWeight(searcher);
            object[] drillDowns = new object[drillDownQueries.Length];
            for (int dim = 0; dim < drillDownQueries.Length; dim++)
            {
                Query query = drillDownQueries[dim];
                Filter filter = DrillDownQuery.GetFilter(query);
                if (filter != null)
                {
                    drillDowns[dim] = filter;
                }
                else
                {
                    // TODO: would be nice if we could say "we will do no
                    // scoring" here....
                    drillDowns[dim] = searcher.Rewrite(query).CreateWeight(searcher);
                }
            }

            return new WeightAnonymousClass(this, baseWeight, drillDowns);
        }

        private sealed class WeightAnonymousClass : Weight
        {
            private readonly DrillSidewaysQuery outerInstance;

            private readonly Weight baseWeight;
            private readonly object[] drillDowns;

            public WeightAnonymousClass(DrillSidewaysQuery outerInstance, Weight baseWeight, object[] drillDowns)
            {
                this.outerInstance = outerInstance;
                this.baseWeight = baseWeight;
                this.drillDowns = drillDowns;
            }

            public override Explanation Explain(AtomicReaderContext context, int doc)
            {
                return baseWeight.Explain(context, doc);
            }

            public override Query Query => outerInstance.baseQuery;

            public override float GetValueForNormalization()
            {
                return baseWeight.GetValueForNormalization();
            }

            public override void Normalize(float norm, float topLevelBoost)
            {
                baseWeight.Normalize(norm, topLevelBoost);
            }

            public override bool ScoresDocsOutOfOrder =>
                // TODO: would be nice if AssertingIndexSearcher
                // confirmed this for us
                false;

            public override Scorer GetScorer(AtomicReaderContext context, IBits acceptDocs)
            {
                // We can only run as a top scorer:
                throw UnsupportedOperationException.Create();
            }

            public override BulkScorer GetBulkScorer(AtomicReaderContext context, bool scoreDocsInOrder, IBits acceptDocs)
            {
                // TODO: it could be better if we take acceptDocs
                // into account instead of baseScorer?
                Scorer baseScorer = baseWeight.GetScorer(context, acceptDocs);

                DrillSidewaysScorer.DocsAndCost[] dims = new DrillSidewaysScorer.DocsAndCost[drillDowns.Length];
                int nullCount = 0;
                for (int dim = 0; dim < dims.Length; dim++)
                {
                    dims[dim] = new DrillSidewaysScorer.DocsAndCost();
                    dims[dim].sidewaysCollector = outerInstance.drillSidewaysCollectors[dim];
                    if (drillDowns[dim] is Filter filter)
                    {
                        // Pass null for acceptDocs because we already
                        // passed it to baseScorer and baseScorer is
                        // MUST'd here
                        DocIdSet dis = filter.GetDocIdSet(context, null);

                        if (dis is null)
                        {
                            continue;
                        }

                        IBits bits = dis.Bits;

                        if (bits != null)
                        {
                            // TODO: this logic is too naive: the
                            // existence of bits() in DIS today means
                            // either "I'm a cheap FixedBitSet so apply me down
                            // low as you decode the postings" or "I'm so
                            // horribly expensive so apply me after all
                            // other Query/Filter clauses pass"

                            // Filter supports random access; use that to
                            // prevent .advance() on costly filters:
                            dims[dim].bits = bits;

                            // TODO: Filter needs to express its expected
                            // cost somehow, before pulling the iterator;
                            // we should use that here to set the order to
                            // check the filters:

                        }
                        else
                        {
                            DocIdSetIterator disi = dis.GetIterator();
                            if (disi is null)
                            {
                                nullCount++;
                                continue;
                            }
                            dims[dim].disi = disi;
                        }
                    }
                    else
                    {
                        DocIdSetIterator disi = ((Weight)drillDowns[dim]).GetScorer(context, null);
                        if (disi is null)
                        {
                            nullCount++;
                            continue;
                        }
                        dims[dim].disi = disi;
                    }
                }

                // If more than one dim has no matches, then there
                // are no hits nor drill-sideways counts.  Or, if we
                // have only one dim and that dim has no matches,
                // same thing.
                //if (nullCount > 1 || (nullCount == 1 && dims.length == 1)) {
                if (nullCount > 1)
                {
                    return null;
                }

                // Sort drill-downs by most restrictive first:
                Array.Sort(dims);

                if (baseScorer is null)
                {
                    return null;
                }

                return new DrillSidewaysScorer(context, baseScorer, outerInstance.drillDownCollector, dims, outerInstance.scoreSubDocsAtOnce);
            }
        }

        // TODO: these should do "deeper" equals/hash on the 2-D drillDownTerms array

        public override int GetHashCode()
        {
            const int prime = 31;
            int result = base.GetHashCode();
            result = prime * result + ((baseQuery is null) ? 0 : baseQuery.GetHashCode());
            result = prime * result + ((drillDownCollector is null) ? 0 : drillDownCollector.GetHashCode());
            result = prime * result + Arrays.GetHashCode(drillDownQueries);
            result = prime * result + Arrays.GetHashCode(drillSidewaysCollectors);
            return result;
        }

        public override bool Equals(object obj)
        {
            if (this == obj)
            {
                return true;
            }
            if (!base.Equals(obj))
            {
                return false;
            }
            if (this.GetType() != obj.GetType())
            {
                return false;
            }
            DrillSidewaysQuery other = (DrillSidewaysQuery)obj;
            if (baseQuery is null)
            {
                if (other.baseQuery != null)
                {
                    return false;
                }
            }
            else if (!baseQuery.Equals(other.baseQuery))
            {
                return false;
            }
            if (drillDownCollector is null)
            {
                if (other.drillDownCollector != null)
                {
                    return false;
                }
            }
            else if (!drillDownCollector.Equals(other.drillDownCollector))
            {
                return false;
            }
            if (!Arrays.Equals(drillDownQueries, other.drillDownQueries))
            {
                return false;
            }
            if (!Arrays.Equals(drillSidewaysCollectors, other.drillSidewaysCollectors))
            {
                return false;
            }
            return true;
        }
    }
}