using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using Lucene.Net.Diagnostics;
using System.Text;

namespace Lucene.Net.Search
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
    using IndexReader = Lucene.Net.Index.IndexReader;
    using Term = Lucene.Net.Index.Term;
    using ToStringUtils = Lucene.Net.Util.ToStringUtils;

    /// <summary>
    /// A query that wraps another query or a filter and simply returns a constant score equal to the
    /// query boost for every document that matches the filter or query.
    /// For queries it therefore simply strips of all scores and returns a constant one.
    /// </summary>
    public class ConstantScoreQuery : Query
    {
        protected readonly Filter m_filter;
        protected readonly Query m_query;

        /// <summary>
        /// Strips off scores from the passed in <see cref="Search.Query"/>. The hits will get a constant score
        /// dependent on the boost factor of this query.
        /// </summary>
        /// <exception cref="ArgumentNullException">if <paramref name="query"/> is <c>null</c>.</exception>
        public ConstantScoreQuery(Query query)
        {
            // LUCENENET specific: Changed guard clause to throw ArgumentNullException instead of NullPointerException
            this.m_filter = null;
            this.m_query = query ?? throw new ArgumentNullException(nameof(query), "Query may not be null"); // LUCENENET specific - changed from IllegalArgumentException to ArgumentNullException (.NET convention)
        }

        /// <summary>
        /// Wraps a <see cref="Search.Filter"/> as a <see cref="Search.Query"/>. The hits will get a constant score
        /// dependent on the boost factor of this query.
        /// If you simply want to strip off scores from a <see cref="Search.Query"/>, no longer use
        /// <c>new ConstantScoreQuery(new QueryWrapperFilter(query))</c>, instead
        /// use <see cref="ConstantScoreQuery(Query)"/>!
        /// </summary>
        /// <exception cref="ArgumentNullException">if <paramref name="filter"/> is <c>null</c>.</exception>
        public ConstantScoreQuery(Filter filter)
        {
            // LUCENENET specific: Changed guard clause to throw ArgumentNullException instead of NullPointerException
            this.m_filter = filter ?? throw new ArgumentNullException(nameof(filter), "Filter may not be null");
            this.m_query = null;
        }

        /// <summary>
        /// Returns the encapsulated filter, returns <c>null</c> if a query is wrapped. </summary>
        public virtual Filter Filter => m_filter;

        /// <summary>
        /// Returns the encapsulated query, returns <c>null</c> if a filter is wrapped. </summary>
        public virtual Query Query => m_query;

        public override Query Rewrite(IndexReader reader)
        {
            if (m_query != null)
            {
                Query rewritten = m_query.Rewrite(reader);
                if (rewritten != m_query)
                {
                    rewritten = new ConstantScoreQuery(rewritten);
                    rewritten.Boost = this.Boost;
                    return rewritten;
                }
            }
            else
            {
                if (Debugging.AssertsEnabled) Debugging.Assert(m_filter != null);
                // Fix outdated usage pattern from Lucene 2.x/early-3.x:
                // because ConstantScoreQuery only accepted filters,
                // QueryWrapperFilter was used to wrap queries.
                if (m_filter is QueryWrapperFilter qwf)
                {
                    Query rewritten = new ConstantScoreQuery(qwf.Query.Rewrite(reader));
                    rewritten.Boost = this.Boost;
                    return rewritten;
                }
            }
            return this;
        }

        public override void ExtractTerms(ISet<Term> terms)
        {
            // TODO: OK to not add any terms when wrapped a filter
            // and used with MultiSearcher, but may not be OK for
            // highlighting.
            // If a query was wrapped, we delegate to query.
            if (m_query != null)
            {
                m_query.ExtractTerms(terms);
            }
        }

        protected class ConstantWeight : Weight
        {
            private readonly ConstantScoreQuery outerInstance;

            private readonly Weight innerWeight;
            private float queryNorm;
            private float queryWeight;

            public ConstantWeight(ConstantScoreQuery outerInstance, IndexSearcher searcher)
            {
                this.outerInstance = outerInstance;
                this.innerWeight = outerInstance.m_query?.CreateWeight(searcher);
            }

            public override Query Query => outerInstance;

            public override float GetValueForNormalization()
            {
                // we calculate sumOfSquaredWeights of the inner weight, but ignore it (just to initialize everything)
                if (innerWeight != null)
                {
                    innerWeight.GetValueForNormalization();
                }
                queryWeight = outerInstance.Boost;
                return queryWeight * queryWeight;
            }

            public override void Normalize(float norm, float topLevelBoost)
            {
                this.queryNorm = norm * topLevelBoost;
                queryWeight *= this.queryNorm;
                // we normalize the inner weight, but ignore it (just to initialize everything)
                if (innerWeight != null)
                {
                    innerWeight.Normalize(norm, topLevelBoost);
                }
            }

            public override BulkScorer GetBulkScorer(AtomicReaderContext context, bool scoreDocsInOrder, IBits acceptDocs)
            {
                //DocIdSetIterator disi;
                if (outerInstance.m_filter != null)
                {
                    if (Debugging.AssertsEnabled) Debugging.Assert(outerInstance.m_query is null);
                    return base.GetBulkScorer(context, scoreDocsInOrder, acceptDocs);
                }
                else
                {
                    if (Debugging.AssertsEnabled) Debugging.Assert(outerInstance.m_query != null && innerWeight != null);
                    BulkScorer bulkScorer = innerWeight.GetBulkScorer(context, scoreDocsInOrder, acceptDocs);
                    if (bulkScorer is null)
                    {
                        return null;
                    }
                    return new ConstantBulkScorer(outerInstance, bulkScorer, this, queryWeight);
                }
            }

            public override Scorer GetScorer(AtomicReaderContext context, IBits acceptDocs)
            {
                DocIdSetIterator disi;
                if (outerInstance.m_filter != null)
                {
                    if (Debugging.AssertsEnabled) Debugging.Assert(outerInstance.m_query is null);
                    DocIdSet dis = outerInstance.m_filter.GetDocIdSet(context, acceptDocs);
                    if (dis is null)
                    {
                        return null;
                    }
                    disi = dis.GetIterator();
                }
                else
                {
                    if (Debugging.AssertsEnabled) Debugging.Assert(outerInstance.m_query != null && innerWeight != null);
                    disi = innerWeight.GetScorer(context, acceptDocs);
                }

                if (disi is null)
                {
                    return null;
                }
                return new ConstantScorer(outerInstance, disi, this, queryWeight);
            }

            public override bool ScoresDocsOutOfOrder => (innerWeight != null) ? innerWeight.ScoresDocsOutOfOrder : false;

            public override Explanation Explain(AtomicReaderContext context, int doc)
            {
                Scorer cs = GetScorer(context, (context.AtomicReader).LiveDocs);
                bool exists = (cs != null && cs.Advance(doc) == doc);

                ComplexExplanation result = new ComplexExplanation();
                if (exists)
                {
                    result.Description = outerInstance.ToString() + ", product of:";
                    result.Value = queryWeight;
                    result.Match = true;
                    result.AddDetail(new Explanation(outerInstance.Boost, "boost"));
                    result.AddDetail(new Explanation(queryNorm, "queryNorm"));
                }
                else
                {
                    result.Description = outerInstance.ToString() + " doesn't match id " + doc;
                    result.Value = 0;
                    result.Match = false;
                }
                return result;
            }
        }

        /// <summary>
        /// We return this as our <see cref="BulkScorer"/> so that if the CSQ
        /// wraps a query with its own optimized top-level
        /// scorer (e.g. <see cref="BooleanScorer"/>) we can use that
        /// top-level scorer.
        /// </summary>
        protected class ConstantBulkScorer : BulkScorer
        {
            private readonly ConstantScoreQuery outerInstance;

            internal readonly BulkScorer bulkScorer;
            internal readonly Weight weight;
            internal readonly float theScore;

            public ConstantBulkScorer(ConstantScoreQuery outerInstance, BulkScorer bulkScorer, Weight weight, float theScore)
            {
                this.outerInstance = outerInstance;
                this.bulkScorer = bulkScorer;
                this.weight = weight;
                this.theScore = theScore;
            }

            public override bool Score(ICollector collector, int max)
            {
                return bulkScorer.Score(WrapCollector(collector), max);
            }

            private ICollector WrapCollector(ICollector collector)
            {
                return new CollectorAnonymousClass(this, collector);
            }

            private sealed class CollectorAnonymousClass : ICollector
            {
                private readonly ConstantBulkScorer outerInstance;

                private readonly ICollector collector;

                public CollectorAnonymousClass(ConstantBulkScorer outerInstance, ICollector collector)
                {
                    this.outerInstance = outerInstance;
                    this.collector = collector;
                }
                
                public void SetScorer(Scorer scorer)
                {
                    // we must wrap again here, but using the value passed in as parameter:
                    collector.SetScorer(new ConstantScorer(outerInstance.outerInstance, scorer, outerInstance.weight, outerInstance.theScore));
                }

                public void Collect(int doc)
                {
                    collector.Collect(doc);
                }

                public void SetNextReader(AtomicReaderContext context)
                {
                    collector.SetNextReader(context);
                }

                public bool AcceptsDocsOutOfOrder => collector.AcceptsDocsOutOfOrder;
            }
        }

        // LUCENENET NOTE: Marked internal for testing
        protected internal class ConstantScorer : Scorer
        {
            private readonly ConstantScoreQuery outerInstance;

            internal readonly DocIdSetIterator docIdSetIterator;
            internal readonly float theScore;

            public ConstantScorer(ConstantScoreQuery outerInstance, DocIdSetIterator docIdSetIterator, Weight w, float theScore)
                : base(w)
            {
                this.outerInstance = outerInstance;
                this.theScore = theScore;
                this.docIdSetIterator = docIdSetIterator;
            }

            public override int NextDoc()
            {
                return docIdSetIterator.NextDoc();
            }

            public override int DocID => docIdSetIterator.DocID;

            public override float GetScore()
            {
                if (Debugging.AssertsEnabled) Debugging.Assert(docIdSetIterator.DocID != NO_MORE_DOCS);
                return theScore;
            }

            public override int Freq => 1;

            public override int Advance(int target)
            {
                return docIdSetIterator.Advance(target);
            }

            public override long GetCost()
            {
                return docIdSetIterator.GetCost();
            }

            public override ICollection<ChildScorer> GetChildren()
            {
                if (outerInstance.m_query != null)
                {
                    return new[] { new ChildScorer((Scorer)docIdSetIterator, "constant") };
                }
                else
                {
                    return Collections.EmptyList<ChildScorer>();
                }
            }
        }

        public override Weight CreateWeight(IndexSearcher searcher)
        {
            return new ConstantScoreQuery.ConstantWeight(this, searcher);
        }

        public override string ToString(string field)
        {
            return (new StringBuilder("ConstantScore(")).Append((m_query is null) ? m_filter.ToString() : m_query.ToString(field)).Append(')').Append(ToStringUtils.Boost(Boost)).ToString();
        }

        public override bool Equals(object o)
        {
            if (this == o)
            {
                return true;
            }
            if (!base.Equals(o))
            {
                return false;
            }
            if (o is ConstantScoreQuery other)
            {
                return ((this.m_filter is null) ? other.m_filter is null : this.m_filter.Equals(other.m_filter)) && ((this.m_query is null) ? other.m_query is null : this.m_query.Equals(other.m_query));
            }
            return false;
        }

        public override int GetHashCode()
        {
            return 31 * base.GetHashCode() + (m_query ?? (object)m_filter).GetHashCode();
        }
    }
}