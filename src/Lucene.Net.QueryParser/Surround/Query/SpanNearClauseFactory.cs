using Lucene.Net.Diagnostics;
using Lucene.Net.Index;
using Lucene.Net.Search.Spans;
using System;
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

    /// <summary>
    /// SpanNearClauseFactory:
    /// <para/>
    /// Operations:
    /// 
    /// <list type="bullet">
    ///     <item><description>create for a field name and an indexreader.</description></item>
    /// 
    ///     <item><description>add a weighted Term - this should add a corresponding SpanTermQuery, or increase the weight of an existing one.</description></item>
    /// 
    ///     <item><description>add a weighted subquery SpanNearQuery</description></item>
    /// 
    ///     <item><description>create a clause for SpanNearQuery from the things added above.</description></item>
    /// </list>
    /// <para/>
    /// For this, create an array of SpanQuery's from the added ones.
    /// The clause normally is a SpanOrQuery over the added subquery SpanNearQuery
    /// the SpanTermQuery's for the added Term's
    /// <para/>
    /// When  it is necessary to suppress double subqueries as much as possible:
    /// GetHashCode() and Equals() on unweighted SpanQuery are needed (possibly via GetTerms(),
    /// the terms are individually hashable).
    /// Idem SpanNearQuery: hash on the subqueries and the slop.
    /// Evt. merge SpanNearQuery's by adding the weights of the corresponding subqueries.
    /// <para/>
    /// To be determined:
    /// Are SpanQuery weights handled correctly during search by Lucene?
    /// Should the resulting SpanOrQuery be sorted?
    /// Could other SpanQueries be added for use in this factory:
    /// - SpanOrQuery: in principle yes, but it only has access to it's terms
    ///                via getTerms(); are the corresponding weights available?
    /// - SpanFirstQuery: treat similar to subquery SpanNearQuery. (ok?)
    /// - SpanNotQuery: treat similar to subquery SpanNearQuery. (ok?)
    /// 
    /// Factory for <see cref="SpanOrQuery"/>
    /// </summary>
    public class SpanNearClauseFactory
    {
        public SpanNearClauseFactory(IndexReader reader, string fieldName, BasicQueryFactory qf) {
            this.reader = reader;
            this.fieldName = fieldName;
            this.weightBySpanQuery = new JCG.Dictionary<SpanQuery, float>();
            this.qf = qf;
          }

        private readonly IndexReader reader; // LUCENENET: marked readonly
        private readonly string fieldName; // LUCENENET: marked readonly
        private readonly IDictionary<SpanQuery, float> weightBySpanQuery; // LUCENENET: marked readonly
        private readonly BasicQueryFactory qf; // LUCENENET: marked readonly

        public virtual IndexReader IndexReader => reader;

        public virtual string FieldName => fieldName;

        public virtual BasicQueryFactory BasicQueryFactory => qf;

        public virtual int Count => weightBySpanQuery.Count;

        public virtual void Clear() { weightBySpanQuery.Clear(); }

        protected virtual void AddSpanQueryWeighted(SpanQuery sq, float weight)
        {
            float w;
            if (weightBySpanQuery.TryGetValue(sq, out float weightValue))
                w = weightValue + weight;
            else
                w = weight;
            weightBySpanQuery[sq] = w;
        }

        public virtual void AddTermWeighted(Term t, float weight)
        {
            SpanTermQuery stq = qf.NewSpanTermQuery(t);
            /* CHECKME: wrap in Hashable...? */
            AddSpanQueryWeighted(stq, weight);
        }

        public virtual void AddSpanQuery(Search.Query q)
        {
            if (q == SrndQuery.TheEmptyLcnQuery)
                return;
            if (!(q is SpanQuery))
                throw AssertionError.Create("Expected SpanQuery: " + q.ToString(FieldName));
            AddSpanQueryWeighted((SpanQuery)q, q.Boost);
        }

        public virtual SpanQuery MakeSpanClause()
        {
            JCG.List<SpanQuery> spanQueries = new JCG.List<SpanQuery>();
            foreach (var wsq in weightBySpanQuery)
            {
                wsq.Key.Boost = wsq.Value;
                spanQueries.Add(wsq.Key);
            }
            if (spanQueries.Count == 1)
                return spanQueries[0];
            else
                return new SpanOrQuery(spanQueries.ToArray());
        }
    }
}
