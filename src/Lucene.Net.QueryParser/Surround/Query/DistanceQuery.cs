using Lucene.Net.Index;
using Lucene.Net.Search.Spans;
using System;
using System.Collections.Generic;

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

    /// <summary>
    /// Factory for NEAR queries 
    /// </summary>
    public class DistanceQuery : ComposedQuery, IDistanceSubQuery
    {
        public DistanceQuery(
            IList<SrndQuery> queries,
            bool infix,
            int opDistance,
            string opName,
            bool ordered)
            : base(queries, infix, opName)
        {
            this.opDistance = opDistance; /* the distance indicated in the operator */
            this.ordered = ordered;
        }

        private readonly int opDistance; // LUCENENET: marked readonly
        public virtual int OpDistance => opDistance;

        private readonly bool ordered; // LUCENENET: marked readonly
        public virtual bool QueriesOrdered => ordered;


        public virtual string DistanceSubQueryNotAllowed()
        {
            var sqi = GetSubQueriesEnumerator();
            while (sqi.MoveNext())
            {
                var leq = sqi.Current;
                if (leq is IDistanceSubQuery)
                {
                    var dsq = sqi.Current as IDistanceSubQuery;
                    string m = dsq.DistanceSubQueryNotAllowed();
                    if (m != null)
                    {
                        return m;
                    }
                }
                else
                {
                    return "Operator " + OperatorName + " does not allow subquery " + leq.ToString();
                }
            }
            return null; /* subqueries acceptable */
        }

        public virtual void AddSpanQueries(SpanNearClauseFactory sncf)
        {
            Search.Query snq = GetSpanNearQuery(sncf.IndexReader,
                                  sncf.FieldName,
                                  Weight,
                                  sncf.BasicQueryFactory);
            sncf.AddSpanQuery(snq);
        }

        public virtual Search.Query GetSpanNearQuery(
            IndexReader reader,
            string fieldName,
            float boost,
            BasicQueryFactory qf)
        {
            SpanQuery[] spanClauses = new SpanQuery[NrSubQueries];
            var sqi = GetSubQueriesEnumerator();
            int qi = 0;
            while (sqi.MoveNext())
            {
                SpanNearClauseFactory sncf = new SpanNearClauseFactory(reader, fieldName, qf);

                ((IDistanceSubQuery)sqi.Current).AddSpanQueries(sncf);
                if (sncf.Count == 0)
                { /* distance operator requires all sub queries */
                    while (sqi.MoveNext())
                    { /* produce evt. error messages but ignore results */
                        ((IDistanceSubQuery)sqi.Current).AddSpanQueries(sncf);
                        sncf.Clear();
                    }
                    return SrndQuery.TheEmptyLcnQuery;
                }

                spanClauses[qi] = sncf.MakeSpanClause();
                qi++;
            }
            SpanNearQuery r = new SpanNearQuery(spanClauses, OpDistance - 1, QueriesOrdered);
            r.Boost = boost;
            return r;
        }

        public override Search.Query MakeLuceneQueryFieldNoBoost(string fieldName, BasicQueryFactory qf)
        {
            return new DistanceRewriteQuery(this, fieldName, qf);
        }
    }
}
