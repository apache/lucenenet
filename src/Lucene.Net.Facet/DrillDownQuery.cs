// Lucene version compatibility level 4.8.1
using Lucene.Net.Diagnostics;
using Lucene.Net.Support;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using JCG = J2N.Collections.Generic;

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

    using BooleanClause = Lucene.Net.Search.BooleanClause;
    using BooleanQuery = Lucene.Net.Search.BooleanQuery;
    using ConstantScoreQuery = Lucene.Net.Search.ConstantScoreQuery;
    using Filter = Lucene.Net.Search.Filter;
    using FilteredQuery = Lucene.Net.Search.FilteredQuery;
    using IndexReader = Lucene.Net.Index.IndexReader;
    using MatchAllDocsQuery = Lucene.Net.Search.MatchAllDocsQuery;
    using Occur = Lucene.Net.Search.Occur;
    using Query = Lucene.Net.Search.Query;
    using Term = Lucene.Net.Index.Term;
    using TermQuery = Lucene.Net.Search.TermQuery;

    /// <summary>
    /// A <see cref="Query"/> for drill-down over facet categories. You
    /// should call <see cref="Add(string, string[])"/> for every group of categories you
    /// want to drill-down over.
    /// <para/>
    /// <b>NOTE:</b> if you choose to create your own <see cref="Query"/> by calling
    /// <see cref="Term"/>, it is recommended to wrap it with <see cref="ConstantScoreQuery"/>
    /// and set the <see cref="Query.Boost">boost</see> to <c>0.0f</c>,
    /// so that it does not affect the scores of the documents.
    /// <para/>
    /// Collection initializer note: To create and populate a <see cref="DrillDownQuery"/>
    /// in a single statement, you can use the following example as a guide:
    /// 
    /// <code>
    /// var drillDownQuery = new DrillDownQuery(config)
    /// {
    ///     { "Publish Date", "2019" },
    ///     { "Publish Date", "2020" },
    ///     { "Publish Date", "2021" },
    ///     { "timestamp", NumericRangeQuery.NewInt64Range("timestamp", range.Min, range.Max, range.MinInclusive, range.MaxInclusive) },
    ///     { "filter", GetFilter() }
    /// };
    /// </code>
    /// <para/>
    /// @lucene.experimental
    /// </summary>
    public sealed class DrillDownQuery : Query, IEnumerable<BooleanClause>, IEnumerable<KeyValuePair<string, int>> // LUCENENET specific - Added collection initializer to make populating easier
    {
        /// <summary>
        /// Creates a drill-down term.
        /// </summary>
        public static Term Term(string field, string dim, params string[] path)
        {
            return new Term(field, FacetsConfig.PathToString(dim, path));
        }

        private readonly FacetsConfig config;
        private readonly BooleanQuery query;
        private readonly IDictionary<string, int> drillDownDims = new JCG.LinkedDictionary<string, int>();

        /// <summary>
        /// Used by <see cref="Clone"/>
        /// </summary>
        internal DrillDownQuery(FacetsConfig config, BooleanQuery query, IDictionary<string, int> drillDownDims)
        {
            this.query = (BooleanQuery)query.Clone();
            this.drillDownDims.PutAll(drillDownDims);
            this.config = config;
        }

        /// <summary>
        /// Used by <see cref="DrillSideways"/>
        /// </summary>
        internal DrillDownQuery(FacetsConfig config, Filter filter, DrillDownQuery other)
        {
            query = new BooleanQuery(true); // disable coord

            BooleanClause[] clauses = other.query.GetClauses();
            if (clauses.Length == other.drillDownDims.Count)
            {
                throw new ArgumentException("cannot apply filter unless baseQuery isn't null; pass ConstantScoreQuery instead");
            }
            if (Debugging.AssertsEnabled) Debugging.Assert(clauses.Length == 1 + other.drillDownDims.Count, "{0} vs {1}", clauses.Length, (1 + other.drillDownDims.Count));
            drillDownDims.PutAll(other.drillDownDims);
            query.Add(new FilteredQuery(clauses[0].Query, filter), Occur.MUST);
            for (int i = 1; i < clauses.Length; i++)
            {
                query.Add(clauses[i].Query, Occur.MUST);
            }
            this.config = config;
        }

        /// <summary>
        /// Used by <see cref="DrillSideways"/>
        /// </summary>
        internal DrillDownQuery(FacetsConfig config, Query baseQuery, IList<Query> clauses, IDictionary<string, int> drillDownDims)
        {
            query = new BooleanQuery(true);
            if (baseQuery != null)
            {
                query.Add(baseQuery, Occur.MUST);
            }
            foreach (Query clause in clauses)
            {
                query.Add(clause, Occur.MUST);
            }
            this.drillDownDims.PutAll(drillDownDims);
            this.config = config;
        }

        /// <summary>
        /// Creates a new <see cref="DrillDownQuery"/> without a base query, 
        /// to perform a pure browsing query (equivalent to using
        /// <see cref="MatchAllDocsQuery"/> as base). 
        /// </summary>
        public DrillDownQuery(FacetsConfig config)
            : this(config, null)
        {
        }

        /// <summary>
        /// Creates a new <see cref="DrillDownQuery"/> over the given base query. Can be
        /// <c>null</c>, in which case the result <see cref="Query"/> from
        /// <see cref="Rewrite(IndexReader)"/> will be a pure browsing query, filtering on
        /// the added categories only. 
        /// </summary>
        public DrillDownQuery(FacetsConfig config, Query baseQuery)
        {
            query = new BooleanQuery(true); // disable coord
            if (baseQuery != null)
            {
                query.Add(baseQuery, Occur.MUST);
            }
            this.config = config;
        }

        /// <summary>
        /// Merges (ORs) a new path into an existing AND'd
        /// clause. 
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        private void Merge(string dim, string[] path)
        {
            int index = 0;
            if (drillDownDims.TryGetValue(dim, out int idx))
            {
                index = idx;
            }

            if (query.GetClauses().Length == drillDownDims.Count + 1)
            {
                index++;
            }
            ConstantScoreQuery q = (ConstantScoreQuery)query.GetClauses()[index].Query;
            if ((q.Query is BooleanQuery) == false)
            {
                // App called .add(dim, customQuery) and then tried to
                // merge a facet label in:
                throw RuntimeException.Create("cannot merge with custom Query");
            }
            string indexedField = config.GetDimConfig(dim).IndexFieldName;

            BooleanQuery bq = (BooleanQuery)q.Query;
            bq.Add(new TermQuery(Term(indexedField, dim, path)), Occur.SHOULD);
        }

        /// <summary>
        /// Adds one dimension of drill downs; if you pass the same
        /// dimension more than once it is OR'd with the previous
        /// cofnstraints on that dimension, and all dimensions are
        /// AND'd against each other and the base query. 
        /// </summary>
        public void Add(string dim, params string[] path)
        {
            if (drillDownDims.ContainsKey(dim))
            {
                Merge(dim, path);
                return;
            }
            string indexedField = config.GetDimConfig(dim).IndexFieldName;

            BooleanQuery bq = new BooleanQuery(true)
            {
                { new TermQuery(Term(indexedField, dim, path)), Occur.SHOULD }
            }; // disable coord

            Add(dim, bq);
        }

        /// <summary>
        /// Expert: add a custom drill-down subQuery.  Use this
        /// when you have a separate way to drill-down on the
        /// dimension than the indexed facet ordinals. 
        /// </summary>
        public void Add(string dim, Query subQuery)
        {
            if (drillDownDims.ContainsKey(dim))
            {
                throw new ArgumentException("dimension \"" + dim + "\" already has a drill-down");
            }
            // TODO: we should use FilteredQuery?

            // So scores of the drill-down query don't have an
            // effect:
            ConstantScoreQuery drillDownQuery = new ConstantScoreQuery(subQuery);
            drillDownQuery.Boost = 0.0f;

            query.Add(drillDownQuery, Occur.MUST);

            drillDownDims[dim] = drillDownDims.Count;
        }

        /// <summary>
        /// Expert: add a custom drill-down Filter, e.g. when
        /// drilling down after range faceting. 
        /// </summary>
        public void Add(string dim, Filter subFilter)
        {
            if (drillDownDims.ContainsKey(dim))
            {
                throw new ArgumentException("dimension \"" + dim + "\" already has a drill-down");
            }

            // TODO: we should use FilteredQuery?

            // So scores of the drill-down query don't have an
            // effect:
            ConstantScoreQuery drillDownQuery = new ConstantScoreQuery(subFilter);
            drillDownQuery.Boost = 0.0f;

            query.Add(drillDownQuery, Occur.MUST);

            drillDownDims[dim] = drillDownDims.Count;
        }

        internal static Filter GetFilter(Query query)
        {
            if (query is ConstantScoreQuery scoreQuery)
            {
                ConstantScoreQuery csq = scoreQuery;
                Filter filter = csq.Filter;
                if (filter != null)
                {
                    return filter;
                }
                else
                {
                    return GetFilter(csq.Query);
                }
            }
            else
            {
                return null;
            }
        }

        public override object Clone()
        {
            return new DrillDownQuery(config, query, drillDownDims);
        }

        public override int GetHashCode()
        {
            const int prime = 31;
            int result = base.GetHashCode();
            return prime * result + query.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (!(obj is DrillDownQuery))
            {
                return false;
            }

            DrillDownQuery other = (DrillDownQuery)obj;
            return query.Equals(other.query) && base.Equals(other);
        }

        public override Query Rewrite(IndexReader r)
        {
            if (query.Clauses.Count == 0)
            {
                return new MatchAllDocsQuery();
            }

            IList<Filter> filters = new JCG.List<Filter>();
            IList<Query> queries = new JCG.List<Query>();
            IList<BooleanClause> clauses = query.Clauses;
            Query baseQuery;
            int startIndex;
            if (drillDownDims.Count == query.Clauses.Count)
            {
                baseQuery = new MatchAllDocsQuery();
                startIndex = 0;
            }
            else
            {
                baseQuery = clauses[0].Query;
                startIndex = 1;
            }

            for (int i = startIndex; i < clauses.Count; i++)
            {
                BooleanClause clause = clauses[i];
                Query queryClause = clause.Query;
                Filter filter = GetFilter(queryClause);
                if (filter != null)
                {
                    filters.Add(filter);
                }
                else
                {
                    queries.Add(queryClause);
                }
            }

            if (filters.Count == 0)
            {
                return query;
            }
            else
            {
                // Wrap all filters using FilteredQuery

                // TODO: this is hackish; we need to do it because
                // BooleanQuery can't be trusted to handle the
                // "expensive filter" case.  Really, each Filter should
                // know its cost and we should take that more
                // carefully into account when picking the right
                // strategy/optimization:
                Query wrapped;
                if (queries.Count == 0)
                {
                    wrapped = baseQuery;
                }
                else
                {
                    // disable coord
                    BooleanQuery wrappedBQ = new BooleanQuery(true);
                    if ((baseQuery is MatchAllDocsQuery) == false)
                    {
                        wrappedBQ.Add(baseQuery, Occur.MUST);
                    }
                    foreach (Query q in queries)
                    {
                        wrappedBQ.Add(q, Occur.MUST);
                    }
                    wrapped = wrappedBQ;
                }

                foreach (Filter filter in filters)
                {
                    wrapped = new FilteredQuery(wrapped, filter, FilteredQuery.QUERY_FIRST_FILTER_STRATEGY);
                }

                return wrapped;
            }
        }

        public override string ToString(string field)
        {
            return query.ToString(field);
        }

        internal BooleanQuery BooleanQuery => query;

        internal IDictionary<string, int> Dims => drillDownDims;

        // LUCENENET specific - added for collection initializer to work with Add() methods.
        IEnumerator IEnumerable.GetEnumerator() => query.GetEnumerator();

        /// <summary>
        /// Returns an iterator on the clauses in this query. It implements the <see cref="T:IEnumerable{BooleanClause}"/> interface to
        /// make it possible to do:
        /// <code>foreach (BooleanClause clause in drillDownQuery) {}</code>
        /// </summary>
        public IEnumerator<BooleanClause> GetEnumerator() => query.GetEnumerator();

        /// <summary>
        /// Returns an iterator on the dims in this query. It implements the <see cref="T:IEnumerable{KeyValuePair{string, int}}"/> interface to
        /// make it possible to do:
        /// <code>
        /// foreach (KeyValuePair&lt;string, int&gt; dimensionAndCount in (IEnumerable&lt;KeyValuePair&lt;string, int&gt;&gt;)drillDownQuery) {}
        /// </code>
        /// </summary>
        IEnumerator<KeyValuePair<string, int>> IEnumerable<KeyValuePair<string, int>>.GetEnumerator() => drillDownDims.GetEnumerator();
    }
}