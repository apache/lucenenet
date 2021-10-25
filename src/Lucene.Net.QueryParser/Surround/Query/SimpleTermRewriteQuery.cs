using Lucene.Net.Index;
using Lucene.Net.Search;
using System.Collections.Generic;
using JCG = J2N.Collections.Generic;

namespace Lucene.Net.QueryParsers.Surround.Query
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

    internal class SimpleTermRewriteQuery : RewriteQuery<SimpleTerm>
    {
        public SimpleTermRewriteQuery(
            SimpleTerm srndQuery,
            string fieldName,
            BasicQueryFactory qf)
            : base(srndQuery, fieldName, qf)
        {
        }

        public override Search.Query Rewrite(IndexReader reader)
        {
            var luceneSubQueries = new JCG.List<Search.Query>();
            m_srndQuery.VisitMatchingTerms(reader, m_fieldName, 
                new SimpleTermRewriteMatchingTermVisitor(luceneSubQueries, m_qf));
            return (luceneSubQueries.Count == 0) ? SrndQuery.TheEmptyLcnQuery
                : (luceneSubQueries.Count == 1) ? luceneSubQueries[0]
                : SrndBooleanQuery.MakeBooleanQuery(
                /* luceneSubQueries all have default weight */
                luceneSubQueries, Occur.SHOULD); /* OR the subquery terms */
        }

        internal class SimpleTermRewriteMatchingTermVisitor : SimpleTerm.IMatchingTermVisitor
        {
            private readonly IList<Search.Query> luceneSubQueries;
            private readonly BasicQueryFactory qf;

            public SimpleTermRewriteMatchingTermVisitor(IList<Search.Query> luceneSubQueries, BasicQueryFactory qf)
            {
                this.luceneSubQueries = luceneSubQueries;
                this.qf = qf;
            }

            public void VisitMatchingTerm(Term term)
            {
                luceneSubQueries.Add(qf.NewTermQuery(term));
            }
        }
    }
}
