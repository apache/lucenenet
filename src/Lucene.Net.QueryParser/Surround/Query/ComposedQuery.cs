using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
    /// Base class for composite queries (such as AND/OR/NOT)
    /// </summary>
    public abstract class ComposedQuery : SrndQuery
    {
        public ComposedQuery(IList<SrndQuery> qs, bool operatorInfix, string opName)
        {
            Recompose(qs);
            this.operatorInfix = operatorInfix;
            this.m_opName = opName;
        }

        protected virtual void Recompose(IList<SrndQuery> queries)
        {
            if (queries.Count < 2) throw new InvalidOperationException("Too few subqueries");
            this.m_queries = new List<SrndQuery>(queries);
        }

        protected string m_opName;
        public virtual string OperatorName { get { return m_opName; } }

        protected IList<SrndQuery> m_queries;

        public virtual IEnumerator<SrndQuery> GetSubQueriesEnumerator()
        {
            return m_queries.GetEnumerator();
        }

        public virtual int NrSubQueries { get { return m_queries.Count; } }

        public virtual SrndQuery GetSubQuery(int qn) { return m_queries[qn]; }

        private bool operatorInfix;
        public virtual bool IsOperatorInfix { get { return operatorInfix; } } /* else prefix operator */

        public virtual IList<Search.Query> MakeLuceneSubQueriesField(string fn, BasicQueryFactory qf)
        {
            List<Search.Query> luceneSubQueries = new List<Search.Query>();
            IEnumerator<SrndQuery> sqi = GetSubQueriesEnumerator();
            while (sqi.MoveNext())
            {
                luceneSubQueries.Add((sqi.Current).MakeLuceneQueryField(fn, qf));
            }
            return luceneSubQueries;
        }

        public override string ToString()
        {
            StringBuilder r = new StringBuilder();
            if (IsOperatorInfix)
            {
                InfixToString(r);
            }
            else
            {
                PrefixToString(r);
            }
            WeightToString(r);
            return r.ToString();
        }

        // Override for different spacing
        protected virtual string PrefixSeparator { get { return ", "; } }
        protected virtual string BracketOpen { get { return "("; } }
        protected virtual string BracketClose { get { return ")"; } }

        protected virtual void InfixToString(StringBuilder r)
        {
            /* Brackets are possibly redundant in the result. */
            IEnumerator<SrndQuery> sqi = GetSubQueriesEnumerator();
            r.Append(BracketOpen);
            if (sqi.MoveNext())
            {
                r.Append(sqi.Current.ToString());
                while (sqi.MoveNext())
                {
                    r.Append(" ");
                    r.Append(OperatorName); /* infix operator */
                    r.Append(" ");
                    r.Append(sqi.Current.ToString());
                }
            }
            r.Append(BracketClose);
        }

        protected virtual void PrefixToString(StringBuilder r)
        {
            IEnumerator<SrndQuery> sqi = GetSubQueriesEnumerator();
            r.Append(OperatorName); /* prefix operator */
            r.Append(BracketOpen);
            if (sqi.MoveNext())
            {
                r.Append(sqi.Current.ToString());
                while (sqi.MoveNext())
                {
                    r.Append(PrefixSeparator);
                    r.Append(sqi.Current.ToString());
                }
            }
            r.Append(BracketClose);
        }

        public override bool IsFieldsSubQueryAcceptable
        {
            get
            {
                /* at least one subquery should be acceptable */
                IEnumerator<SrndQuery> sqi = GetSubQueriesEnumerator();
                while (sqi.MoveNext())
                {
                    if ((sqi.Current).IsFieldsSubQueryAcceptable)
                    {
                        return true;
                    }
                }
                return false;
            }
        }
    }
}
