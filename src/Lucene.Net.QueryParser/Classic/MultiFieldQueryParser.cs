using Lucene.Net.Analysis;
using Lucene.Net.Search;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using JCG = J2N.Collections.Generic;

namespace Lucene.Net.QueryParsers.Classic
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
    /// A <see cref="QueryParser"/> which constructs queries to search multiple fields.
    /// </summary>
    public class MultiFieldQueryParser : QueryParser
    {
        protected string[] m_fields;
        protected IDictionary<string, float> m_boosts;

        /// <summary> 
        /// Creates a <see cref="MultiFieldQueryParser"/>. Allows passing of a map with term to
        /// Boost, and the boost to apply to each term.
        /// 
        /// <para/>
        /// It will, when <see cref="QueryParserBase.Parse(string)"/> is called, construct a query like this
        /// (assuming the query consists of two terms and you specify the two fields
        /// <c>title</c> and <c>body</c>):
        /// <para/>
        /// 
        /// <code>
        /// (title:term1 body:term1) (title:term2 body:term2)
        /// </code>
        /// 
        /// <para/>
        /// When <see cref="QueryParserBase.DefaultOperator"/> is set to <see cref="QueryParserBase.AND_OPERATOR"/>, the result will be:
        /// <para/>
        /// 
        /// <code>
        /// +(title:term1 body:term1) +(title:term2 body:term2)
        /// </code>
        /// 
        /// <para/>
        /// When you pass a boost (title=>5 body=>10) you can get
        /// <para/>
        /// 
        /// <code>
        /// +(title:term1^5.0 body:term1^10.0) +(title:term2^5.0 body:term2^10.0)
        /// </code>
        /// 
        /// <para/>
        /// In other words, all the query's terms must appear, but it doesn't matter
        /// in what fields they appear.
        /// <para/>
        /// </summary>
        public MultiFieldQueryParser(LuceneVersion matchVersion, string[] fields, Analyzer analyzer, IDictionary<string, float> boosts)
            : this(matchVersion, fields, analyzer)
        {
            this.m_boosts = boosts;
        }

        /// <summary> 
        /// Creates a MultiFieldQueryParser.
        /// 
        /// <para/>
        /// It will, when <see cref="QueryParserBase.Parse(string)"/> is called, construct a query like this
        /// (assuming the query consists of two terms and you specify the two fields
        /// <c>title</c> and <c>body</c>):
        /// <para/>
        /// 
        /// <code>
        /// (title:term1 body:term1) (title:term2 body:term2)
        /// </code>
        /// 
        /// <para/>
        /// When <see cref="QueryParserBase.DefaultOperator"/> is set to <see cref="QueryParserBase.AND_OPERATOR"/>, the result will be:
        /// <para/>
        /// 
        /// <code>
        /// +(title:term1 body:term1) +(title:term2 body:term2)
        /// </code>
        /// 
        /// <para/>
        /// In other words, all the query's terms must appear, but it doesn't matter
        /// in what fields they appear.
        /// <para/>
        /// </summary>
        public MultiFieldQueryParser(LuceneVersion matchVersion, string[] fields, Analyzer analyzer)
            : base(matchVersion, null, analyzer)
        {
            this.m_fields = fields;
        }

        protected internal override Query GetFieldQuery(string field, string queryText, int slop)
        {
            if (field is null)
            {
                IList<BooleanClause> clauses = new JCG.List<BooleanClause>();
                for (int i = 0; i < m_fields.Length; i++)
                {
                    Query q = base.GetFieldQuery(m_fields[i], queryText, true);
                    if (q != null)
                    {
                        //If the user passes a map of boosts
                        if (m_boosts != null)
                        {
                            //Get the boost from the map and apply them
                            float boost = m_boosts[m_fields[i]];
                            q.Boost = boost;
                        }
                        ApplySlop(q, slop);
                        clauses.Add(new BooleanClause(q, Occur.SHOULD));
                    }
                }
                if (clauses.Count == 0)
                    // happens for stopwords
                    return null;
                return GetBooleanQuery(clauses, true);
            }
            Query q2 = base.GetFieldQuery(field, queryText, true);
            ApplySlop(q2, slop);
            return q2;
        }

        private static void ApplySlop(Query q, int slop) // LUCENENET: CA1822: Mark members as static
        {
            if (q is PhraseQuery phraseQuery)
            {
                phraseQuery.Slop = slop;
            }
            else if (q is MultiPhraseQuery multiPhraseQuery)
            {
                multiPhraseQuery.Slop = slop;
            }
        }

        protected internal override Query GetFieldQuery(string field, string queryText, bool quoted)
        {
            if (field is null)
            {
                IList<BooleanClause> clauses = new JCG.List<BooleanClause>();
                for (int i = 0; i < m_fields.Length; i++)
                {
                    Query q = base.GetFieldQuery(m_fields[i], queryText, quoted);
                    if (q != null)
                    {
                        //If the user passes a map of boosts
                        if (m_boosts != null)
                        {
                            //Get the boost from the map and apply them
                            float boost = m_boosts[m_fields[i]];
                            q.Boost = boost;
                        }
                        clauses.Add(new BooleanClause(q, Occur.SHOULD));
                    }
                }
                if (clauses.Count == 0)  // happens for stopwords
                    return null;
                return GetBooleanQuery(clauses, true);
            }
            Query q2 = base.GetFieldQuery(field, queryText, quoted);
            return q2;
        }

        protected internal override Query GetFuzzyQuery(string field, string termStr, float minSimilarity)
        {
            if (field is null)
            {
                IList<BooleanClause> clauses = new JCG.List<BooleanClause>();
                for (int i = 0; i < m_fields.Length; i++)
                {
                    clauses.Add(new BooleanClause(GetFuzzyQuery(m_fields[i], termStr, minSimilarity), Occur.SHOULD));
                }
                return GetBooleanQuery(clauses, true);
            }
            return base.GetFuzzyQuery(field, termStr, minSimilarity);
        }

        protected internal override Query GetPrefixQuery(string field, string termStr)
        {
            if (field is null)
            {
                IList<BooleanClause> clauses = new JCG.List<BooleanClause>();
                for (int i = 0; i < m_fields.Length; i++)
                {
                    clauses.Add(new BooleanClause(GetPrefixQuery(m_fields[i], termStr), Occur.SHOULD));
                }
                return GetBooleanQuery(clauses, true);
            }
            return base.GetPrefixQuery(field, termStr);
        }

        protected internal override Query GetWildcardQuery(string field, string termStr)
        {
            if (field is null)
            {
                IList<BooleanClause> clauses = new JCG.List<BooleanClause>();
                for (int i = 0; i < m_fields.Length; i++)
                {
                    clauses.Add(new BooleanClause(GetWildcardQuery(m_fields[i], termStr), Occur.SHOULD));
                }
                return GetBooleanQuery(clauses, true);
            }
            return base.GetWildcardQuery(field, termStr);
        }


        protected internal override Query GetRangeQuery(string field, string part1, string part2, bool startInclusive, bool endInclusive)
        {
            if (field is null)
            {
                IList<BooleanClause> clauses = new JCG.List<BooleanClause>();
                for (int i = 0; i < m_fields.Length; i++)
                {
                    clauses.Add(new BooleanClause(GetRangeQuery(m_fields[i], part1, part2, startInclusive, endInclusive), Occur.SHOULD));
                }
                return GetBooleanQuery(clauses, true);
            }
            return base.GetRangeQuery(field, part1, part2, startInclusive, endInclusive);
        }

        protected internal override Query GetRegexpQuery(string field, string termStr)
        {
            if (field is null)
            {
                IList<BooleanClause> clauses = new JCG.List<BooleanClause>();
                for (int i = 0; i < m_fields.Length; i++)
                {
                    clauses.Add(new BooleanClause(GetRegexpQuery(m_fields[i], termStr),
                        Occur.SHOULD));
                }
                return GetBooleanQuery(clauses, true);
            }
            return base.GetRegexpQuery(field, termStr);
        }

        /// <summary> 
        /// Parses a query which searches on the fields specified.
        /// <para/>
        /// If x fields are specified, this effectively constructs:
        /// 
        /// <code>
        /// (field1:query1) (field2:query2) (field3:query3)...(fieldx:queryx)
        /// </code>
        /// 
        /// </summary>
        /// <param name="matchVersion">Lucene version to match; this is passed through to
        /// <see cref="QueryParser"/>.</param>
        /// <param name="queries">Queries strings to parse</param>
        /// <param name="fields">Fields to search on</param>
        /// <param name="analyzer">Analyzer to use</param>
        /// <exception cref="ParseException">if query parsing fails</exception>
        /// <exception cref="ArgumentException">
        /// if the length of the queries array differs from the length of
        /// the fields array
        /// </exception>
        public static Query Parse(LuceneVersion matchVersion, string[] queries, string[] fields, Analyzer analyzer)
        {
            // LUCENENET: Added null guard clauses
            if (queries is null)
                throw new ArgumentNullException(nameof(queries));
            if (fields is null)
                throw new ArgumentNullException(nameof(fields));

            if (queries.Length != fields.Length)
                throw new ArgumentException("queries.Length != fields.Length");
            BooleanQuery bQuery = new BooleanQuery();
            for (int i = 0; i < fields.Length; i++)
            {
                QueryParser qp = new QueryParser(matchVersion, fields[i], analyzer);
                Query q = qp.Parse(queries[i]);
                if (q != null && (!(q is BooleanQuery booleanQuery) || booleanQuery.Clauses.Count > 0))
                {
                    bQuery.Add(q, Occur.SHOULD);
                }
            }
            return bQuery;
        }

        /// <summary> 
        /// Parses a query, searching on the fields specified. Use this if you need
        /// to specify certain fields as required, and others as prohibited.
        /// <para/>
        /// Usage:
        /// <code>
        ///     string[] fields = {&quot;filename&quot;, &quot;contents&quot;, &quot;description&quot;};
        ///     Occur[] flags = {Occur.SHOULD,
        ///         Occur.MUST,
        ///         Occur.MUST_NOT};
        ///     MultiFieldQueryParser.Parse(&quot;query&quot;, fields, flags, analyzer);
        /// </code>
        /// <para/>
        /// The code above would construct a query:
        /// 
        /// <code>
        /// (filename:query) +(contents:query) -(description:query)
        /// </code>
        /// 
        /// </summary>
        /// <param name="matchVersion">Lucene version to match; this is passed through to
        /// <see cref="QueryParser"/>.</param>
        /// <param name="query">Query string to parse</param>
        /// <param name="fields">Fields to search on</param>
        /// <param name="flags">Flags describing the fields</param>
        /// <param name="analyzer">Analyzer to use</param>
        /// <exception cref="ParseException">if query parsing fails</exception>
        /// <exception cref="ArgumentException">
        /// if the length of the fields array differs from the length of
        /// the flags array
        /// </exception>
        public static Query Parse(LuceneVersion matchVersion, string query, string[] fields, Occur[] flags, Analyzer analyzer)
        {
            // LUCENENET: Added null guard clauses
            if (query is null)
                throw new ArgumentNullException(nameof(query));
            if (fields is null)
                throw new ArgumentNullException(nameof(fields));
            if (flags is null)
                throw new ArgumentNullException(nameof(flags));

            if (fields.Length != flags.Length)
                throw new ArgumentException("fields.Length != flags.Length");
            BooleanQuery bQuery = new BooleanQuery();
            for (int i = 0; i < fields.Length; i++)
            {
                QueryParser qp = new QueryParser(matchVersion, fields[i], analyzer);
                Query q = qp.Parse(query);
                if (q != null && (!(q is BooleanQuery booleanQuery) || booleanQuery.Clauses.Count > 0))
                {
                    bQuery.Add(q, flags[i]);
                }
            }
            return bQuery;
        }

        /// <summary> 
        /// Parses a query, searching on the fields specified. Use this if you need
        /// to specify certain fields as required, and others as prohibited.
        /// <para/>
        /// Usage:
        /// <code>
        ///     string[] query = {&quot;query1&quot;, &quot;query2&quot;, &quot;query3&quot;};
        ///     string[] fields = {&quot;filename&quot;, &quot;contents&quot;, &quot;description&quot;};
        ///     Occur[] flags = {Occur.SHOULD,
        ///         Occur.MUST,
        ///         Occur.MUST_NOT};
        ///     MultiFieldQueryParser.Parse(query, fields, flags, analyzer);
        /// </code>
        /// <para/>
        /// The code above would construct a query:
        /// 
        /// <code>
        /// (filename:query1) +(contents:query2) -(description:query3)
        /// </code>
        /// 
        /// </summary>
        /// <param name="matchVersion">Lucene version to match; this is passed through to
        /// <see cref="QueryParser"/>.</param>
        /// <param name="queries">Queries string to parse</param>
        /// <param name="fields">Fields to search on</param>
        /// <param name="flags">Flags describing the fields</param>
        /// <param name="analyzer">Analyzer to use</param>
        /// <exception cref="ParseException">if query parsing fails</exception>
        /// <exception cref="ArgumentException">if the length of the queries, fields, and flags array differ</exception>
        public static Query Parse(LuceneVersion matchVersion, string[] queries, string[] fields, Occur[] flags, Analyzer analyzer)
        {
            // LUCENENET: Added null guard clauses
            if (queries is null)
                throw new ArgumentNullException(nameof(queries));
            if (fields is null)
                throw new ArgumentNullException(nameof(fields));
            if (flags is null)
                throw new ArgumentNullException(nameof(flags));

            if (!(queries.Length == fields.Length && queries.Length == flags.Length))
                throw new ArgumentException("queries, fields, and flags array have have different length");
            BooleanQuery bQuery = new BooleanQuery();
            for (int i = 0; i < fields.Length; i++)
            {
                QueryParser qp = new QueryParser(matchVersion, fields[i], analyzer);
                Query q = qp.Parse(queries[i]);
                if (q != null && (!(q is BooleanQuery booleanQuery) || booleanQuery.Clauses.Count > 0))
                {
                    bQuery.Add(q, flags[i]);
                }
            }
            return bQuery;
        }
    }
}