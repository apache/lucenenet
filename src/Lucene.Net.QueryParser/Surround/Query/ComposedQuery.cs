using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
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

    /// <summary>
    /// Base class for composite queries (such as AND/OR/NOT)
    /// </summary>
    public abstract class ComposedQuery : SrndQuery
    {
        // LUCENENET specific - provided protected parameterless constructor to allow subclasses
        // avoid issues with virtual Recompose method
        protected ComposedQuery(bool operatorInfix, string opName)
        {
            this.operatorInfix = operatorInfix;
            this.m_opName = opName;
        }

        [SuppressMessage("CodeQuality", "IDE0079:Remove unnecessary suppression", Justification = "This is a SonarCloud issue")]
        [SuppressMessage("CodeQuality", "S1699:Constructors should only call non-overridable methods", Justification = "Required for continuity with Lucene's design")]
        protected ComposedQuery(IList<SrndQuery> qs, bool operatorInfix, string opName) // LUCENENET: CA1012: Abstract types should not have constructors (marked protected)
            : this(operatorInfix, opName)
        {
            Recompose(qs);
        }

        protected virtual void Recompose(IList<SrndQuery> queries)
        {
            if (queries.Count < 2) throw AssertionError.Create("Too few subqueries");
            this.m_queries = new JCG.List<SrndQuery>(queries);
        }

        protected string m_opName;
        public virtual string OperatorName => m_opName;

        protected IList<SrndQuery> m_queries;

        public virtual IEnumerator<SrndQuery> GetSubQueriesEnumerator()
        {
            return m_queries.GetEnumerator();
        }

        public virtual int NrSubQueries => m_queries.Count;

        public virtual SrndQuery GetSubQuery(int qn) { return m_queries[qn]; }

        private readonly bool operatorInfix;
        public virtual bool IsOperatorInfix => operatorInfix; /* else prefix operator */

        public virtual IList<Search.Query> MakeLuceneSubQueriesField(string fn, BasicQueryFactory qf)
        {
            IList<Search.Query> luceneSubQueries = new JCG.List<Search.Query>();
            using (IEnumerator<SrndQuery> sqi = GetSubQueriesEnumerator())
            {
                while (sqi.MoveNext())
                {
                    luceneSubQueries.Add((sqi.Current).MakeLuceneQueryField(fn, qf));
                }
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
        protected virtual string PrefixSeparator => ", ";
        protected virtual string BracketOpen => "(";
        protected virtual string BracketClose => ")";

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
                    r.Append(' ');
                    r.Append(OperatorName); /* infix operator */
                    r.Append(' ');
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
