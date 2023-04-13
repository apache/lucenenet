using J2N.Text;
using Lucene.Net.Index;
using System;
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
    /// Base class for queries that expand to sets of simple terms.
    /// </summary>
    public abstract class SimpleTerm : SrndQuery, IDistanceSubQuery, IComparable<SimpleTerm>
    {
        protected SimpleTerm(bool quoted) // LUCENENET: CA1012: Abstract types should not have constructors (marked protected)
        { 
            this.quoted = quoted; 
        }

        private readonly bool quoted; // LUCENENET: marked readonly
        internal bool IsQuoted => quoted;

        public virtual string Quote => "\"";
        public virtual string FieldOperator => "/";

        public abstract string ToStringUnquoted();

        [Obsolete("deprecated (March 2011) Not normally used, to be removed from Lucene 4.0. This class implementing Comparable is to be removed at the same time.")]
        public int CompareTo(SimpleTerm ost)
        {
            /* for ordering terms and prefixes before using an index, not used */
            return this.ToStringUnquoted().CompareToOrdinal(ost.ToStringUnquoted());
        }

        protected virtual void SuffixToString(StringBuilder r) { } /* override for prefix query */


        public override string ToString()
        {
            StringBuilder r = new StringBuilder();
            if (IsQuoted)
            {
                r.Append(Quote);
            }
            r.Append(ToStringUnquoted());
            if (IsQuoted)
            {
                r.Append(Quote);
            }
            SuffixToString(r);
            WeightToString(r);
            return r.ToString();
        }

        public abstract void VisitMatchingTerms(
                            IndexReader reader,
                            string fieldName,
                            IMatchingTermVisitor mtv);

        /// <summary>
        /// Callback to visit each matching term during "rewrite"
        /// in <see cref="VisitMatchingTerm(Term)"/>
        /// </summary>
        public interface IMatchingTermVisitor
        {
            void VisitMatchingTerm(Term t);
        }

        public virtual string DistanceSubQueryNotAllowed()
        {
            return null;
        }

        public virtual void AddSpanQueries(SpanNearClauseFactory sncf)
        {
            VisitMatchingTerms(
                sncf.IndexReader,
                sncf.FieldName,
                new AddSpanQueriesMatchingTermVisitor(sncf, Weight));
        }

        internal class AddSpanQueriesMatchingTermVisitor : IMatchingTermVisitor
        {
            private readonly SpanNearClauseFactory sncf;
            private readonly float weight;

            public AddSpanQueriesMatchingTermVisitor(SpanNearClauseFactory sncf, float weight)
            {
                this.sncf = sncf;
                this.weight = weight;
            }

            public void VisitMatchingTerm(Term term)
            {
                sncf.AddTermWeighted(term, weight);
            }
        }

        public override Search.Query MakeLuceneQueryFieldNoBoost(string fieldName, BasicQueryFactory qf)
        {
            return new SimpleTermRewriteQuery(this, fieldName, qf);
        }
    }
}
