using System.Collections.Generic;
using System.Diagnostics;
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
        /// Strips off scores from the passed in Query. The hits will get a constant score
        /// dependent on the boost factor of this query.
        /// </summary>
        public ConstantScoreQuery(Query query)
        {
            if (query == null)
            {
                throw new System.NullReferenceException("Query may not be null");
            }
            this.m_filter = null;
            this.m_query = query;
        }

        /// <summary>
        /// Wraps a Filter as a Query. The hits will get a constant score
        /// dependent on the boost factor of this query.
        /// If you simply want to strip off scores from a Query, no longer use
        /// {@code new ConstantScoreQuery(new QueryWrapperFilter(query))}, instead
        /// use <seealso cref="#ConstantScoreQuery(Query)"/>!
        /// </summary>
        public ConstantScoreQuery(Filter filter)
        {
            if (filter == null)
            {
                throw new System.NullReferenceException("Filter may not be null");
            }
            this.m_filter = filter;
            this.m_query = null;
        }

        /// <summary>
        /// Returns the encapsulated filter, returns {@code null} if a query is wrapped. </summary>
        public virtual Filter Filter
        {
            get
            {
                return m_filter;
            }
        }

        /// <summary>
        /// Returns the encapsulated query, returns {@code null} if a filter is wrapped. </summary>
        public virtual Query Query
        {
            get
            {
                return m_query;
            }
        }

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
                Debug.Assert(m_filter != null);
                // Fix outdated usage pattern from Lucene 2.x/early-3.x:
                // because ConstantScoreQuery only accepted filters,
                // QueryWrapperFilter was used to wrap queries.
                if (m_filter is QueryWrapperFilter)
                {
                    QueryWrapperFilter qwf = (QueryWrapperFilter)m_filter;
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
                this.innerWeight = (outerInstance.m_query == null) ? null : outerInstance.m_query.CreateWeight(searcher);
            }

            public override Query Query
            {
                get
                {
                    return outerInstance;
                }
            }

            public override float GetValueForNormalization()
            {
                // we calculate sumOfSquaredWeights of the inner weight, but ignore it (just to initialize everything)
                /*if (InnerWeight != null)
                {
                    InnerWeight.ValueForNormalization;
                }*/
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
                    Debug.Assert(outerInstance.m_query == null);
                    return base.GetBulkScorer(context, scoreDocsInOrder, acceptDocs);
                }
                else
                {
                    Debug.Assert(outerInstance.m_query != null && innerWeight != null);
                    BulkScorer bulkScorer = innerWeight.GetBulkScorer(context, scoreDocsInOrder, acceptDocs);
                    if (bulkScorer == null)
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
                    Debug.Assert(outerInstance.m_query == null);
                    DocIdSet dis = outerInstance.m_filter.GetDocIdSet(context, acceptDocs);
                    if (dis == null)
                    {
                        return null;
                    }
                    disi = dis.GetIterator();
                }
                else
                {
                    Debug.Assert(outerInstance.m_query != null && innerWeight != null);
                    disi = innerWeight.GetScorer(context, acceptDocs);
                }

                if (disi == null)
                {
                    return null;
                }
                return new ConstantScorer(outerInstance, disi, this, queryWeight);
            }

            public override bool ScoresDocsOutOfOrder
            {
                get { return (innerWeight != null) ? innerWeight.ScoresDocsOutOfOrder : false; }
            }

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
        /// We return this as our <seealso cref="bulkScorer"/> so that if the CSQ
        ///  wraps a query with its own optimized top-level
        ///  scorer (e.g. BooleanScorer) we can use that
        ///  top-level scorer.
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
                return new CollectorAnonymousInnerClassHelper(this, collector);
            }

            private class CollectorAnonymousInnerClassHelper : ICollector
            {
                private readonly ConstantBulkScorer outerInstance;

                private ICollector collector;

                public CollectorAnonymousInnerClassHelper(ConstantBulkScorer outerInstance, Lucene.Net.Search.ICollector collector)
                {
                    this.outerInstance = outerInstance;
                    this.collector = collector;
                }
                
                public virtual void SetScorer(Scorer scorer)
                {
                    // we must wrap again here, but using the value passed in as parameter:
                    collector.SetScorer(new ConstantScorer(outerInstance.outerInstance, scorer, outerInstance.weight, outerInstance.theScore));
                }

                public virtual void Collect(int doc)
                {
                    collector.Collect(doc);
                }

                public virtual void SetNextReader(AtomicReaderContext context)
                {
                    collector.SetNextReader(context);
                }

                public virtual bool AcceptsDocsOutOfOrder
                {
                    get { return collector.AcceptsDocsOutOfOrder; }
                }
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

            public override int DocID
            {
                get { return docIdSetIterator.DocID; }
            }

            public override float Score()
            {
                Debug.Assert(docIdSetIterator.DocID != NO_MORE_DOCS);
                return theScore;
            }

            public override int Freq
            {
                get { return 1; }
            }

            public override int Advance(int target)
            {
                return docIdSetIterator.Advance(target);
            }

            public override long Cost()
            {
                return docIdSetIterator.Cost();
            }

            public override ICollection<ChildScorer> GetChildren()
            {
                if (outerInstance.m_query != null)
                {
                    //LUCENE TO-DO
                    //return Collections.singletonList(new ChildScorer((Scorer)DocIdSetIterator, "constant"));
                    return new[] { new ChildScorer((Scorer)docIdSetIterator, "constant") };
                }
                else
                {
                    //LUCENE TO-DO
                    return new List<ChildScorer>();
                    //return Collections.emptyList();
                }
            }
        }

        public override Weight CreateWeight(IndexSearcher searcher)
        {
            return new ConstantScoreQuery.ConstantWeight(this, searcher);
        }

        public override string ToString(string field)
        {
            return (new StringBuilder("ConstantScore(")).Append((m_query == null) ? m_filter.ToString() : m_query.ToString(field)).Append(')').Append(ToStringUtils.Boost(Boost)).ToString();
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
            if (o is ConstantScoreQuery)
            {
                ConstantScoreQuery other = (ConstantScoreQuery)o;
                return ((this.m_filter == null) ? other.m_filter == null : this.m_filter.Equals(other.m_filter)) && ((this.m_query == null) ? other.m_query == null : this.m_query.Equals(other.m_query));
            }
            return false;
        }

        public override int GetHashCode()
        {
            return 31 * base.GetHashCode() + ((m_query == null) ? (object)m_filter : m_query).GetHashCode();
        }
    }
}