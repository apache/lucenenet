using System.Collections.Generic;
using System.Text;
using Lucene.Net.Index;
using Lucene.Net.Queries.Function;
using Lucene.Net.Search;
using Lucene.Net.Support;
using Lucene.Net.Util;

namespace Lucene.Net.Queries
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
    /// Query that sets document score as a programmatic function of several (sub) scores:
    /// <ol>
    ///    <li>the score of its subQuery (any query)</li>
    ///    <li>(optional) the score of its <seealso cref="FunctionQuery"/> (or queries).</li>
    /// </ol>
    /// Subclasses can modify the computation by overriding <seealso cref="#getCustomScoreProvider"/>.
    /// 
    /// @lucene.experimental
    /// </summary>
    public class CustomScoreQuery : Query
    {

        internal Query subQuery;
        private Query[] scoringQueries; // never null (empty array if there are no valSrcQueries).
        private bool strict = false; // if true, valueSource part of query does not take part in weights normalization.

        /// <summary>
        /// Create a CustomScoreQuery over input subQuery. </summary>
        /// <param name="subQuery"> the sub query whose scored is being customized. Must not be null.  </param>
        public CustomScoreQuery(Query subQuery)
            : this(subQuery, new FunctionQuery[0])
        {
        }

        /// <summary>
        /// Create a CustomScoreQuery over input subQuery and a <seealso cref="FunctionQuery"/>. </summary>
        /// <param name="subQuery"> the sub query whose score is being customized. Must not be null. </param>
        /// <param name="scoringQuery"> a value source query whose scores are used in the custom score
        /// computation.  This parameter is optional - it can be null. </param>
        public CustomScoreQuery(Query subQuery, FunctionQuery scoringQuery)
            : this(subQuery, scoringQuery != null ? new FunctionQuery[] { scoringQuery } : new FunctionQuery[0])
        // don't want an array that contains a single null..
        {
        }

        /// <summary>
        /// Create a CustomScoreQuery over input subQuery and a <seealso cref="FunctionQuery"/>. </summary>
        /// <param name="subQuery"> the sub query whose score is being customized. Must not be null. </param>
        /// <param name="scoringQueries"> value source queries whose scores are used in the custom score
        /// computation.  This parameter is optional - it can be null or even an empty array. </param>
        public CustomScoreQuery(Query subQuery, params FunctionQuery[] scoringQueries)
        {
            this.subQuery = subQuery;
            this.scoringQueries = scoringQueries != null ? scoringQueries : new Query[0];
            if (subQuery == null)
            {
                throw new System.ArgumentException("<subquery> must not be null!");
            }
        }

        public override Query Rewrite(IndexReader reader)
        {
            CustomScoreQuery clone = null;

            Query sq = subQuery.Rewrite(reader);
            if (sq != subQuery)
            {
                clone = (CustomScoreQuery)Clone();
                clone.subQuery = sq;
            }

            for (int i = 0; i < scoringQueries.Length; i++)
            {
                Query v = scoringQueries[i].Rewrite(reader);
                if (v != scoringQueries[i])
                {
                    if (clone == null)
                    {
                        clone = (CustomScoreQuery)Clone();
                    }
                    clone.scoringQueries[i] = v;
                }
            }

            return clone ?? this;
        }

        public override void ExtractTerms(ISet<Term> terms)
        {
            subQuery.ExtractTerms(terms);
            foreach (Query scoringQuery in scoringQueries)
            {
                scoringQuery.ExtractTerms(terms);
            }
        }

        /*(non-Javadoc) @see org.apache.lucene.search.Query#clone() */

        public override object Clone()
        {
            var clone = (CustomScoreQuery)base.Clone();
            clone.subQuery = (Query)subQuery.Clone();
            clone.scoringQueries = new Query[scoringQueries.Length];
            for (int i = 0; i < scoringQueries.Length; i++)
            {
                clone.scoringQueries[i] = (Query)scoringQueries[i].Clone();
            }
            return clone;
        }

        /* (non-Javadoc) @see org.apache.lucene.search.Query#toString(java.lang.String) */

        public override string ToString(string field)
        {
            StringBuilder sb = (new StringBuilder(Name)).Append("(");
            sb.Append(subQuery.ToString(field));
            foreach (Query scoringQuery in scoringQueries)
            {
                sb.Append(", ").Append(scoringQuery.ToString(field));
            }
            sb.Append(")");
            sb.Append(strict ? " STRICT" : "");
            return sb.ToString() + ToStringUtils.Boost(Boost);
        }

        /// <summary>
        /// Returns true if <code>o</code> is equal to this. </summary>
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
            if (this.GetType() != o.GetType())
            {
                return false;
            }
            var other = (CustomScoreQuery)o;
            if (this.Boost != other.Boost || !this.subQuery.Equals(other.subQuery) || this.strict != other.strict ||
                this.scoringQueries.Length != other.scoringQueries.Length)
            {
                return false;
            }
            return Arrays.Equals(scoringQueries, other.scoringQueries);
        }

        /// <summary>
        /// Returns a hash code value for this object. </summary>
        public override int GetHashCode()
        {
            return (this.GetType().GetHashCode() + subQuery.GetHashCode() + Arrays.GetHashCode(scoringQueries)) ^
                   Number.FloatToIntBits(Boost) ^ (strict ? 1234 : 4321);
        }

        /// <summary>
        /// Returns a <seealso cref="CustomScoreProvider"/> that calculates the custom scores
        /// for the given <seealso cref="IndexReader"/>. The default implementation returns a default
        /// implementation as specified in the docs of <seealso cref="CustomScoreProvider"/>.
        /// @since 2.9.2
        /// </summary>
        protected internal virtual CustomScoreProvider GetCustomScoreProvider(AtomicReaderContext context)
        {
            return new CustomScoreProvider(context);
        }

        //=========================== W E I G H T ============================

        private class CustomWeight : Weight
        {
            private readonly CustomScoreQuery outerInstance;

            private readonly Weight subQueryWeight;
            private readonly Weight[] valSrcWeights;
            private readonly bool qStrict;
            private float queryWeight;

            public CustomWeight(CustomScoreQuery outerInstance, IndexSearcher searcher)
            {
                this.outerInstance = outerInstance;
                this.subQueryWeight = outerInstance.subQuery.CreateWeight(searcher);
                this.valSrcWeights = new Weight[outerInstance.scoringQueries.Length];
                for (int i = 0; i < outerInstance.scoringQueries.Length; i++)
                {
                    this.valSrcWeights[i] = outerInstance.scoringQueries[i].CreateWeight(searcher);
                }
                this.qStrict = outerInstance.strict;
            }

            /*(non-Javadoc) @see org.apache.lucene.search.Weight#getQuery() */

            public override Query Query
            {
                get { return outerInstance; }
            }

            public override float GetValueForNormalization()
            {
                float sum = subQueryWeight.GetValueForNormalization();
                foreach (Weight valSrcWeight in valSrcWeights)
                {
                    if (qStrict)
                    {
                        var _ = valSrcWeight.GetValueForNormalization();
                        // do not include ValueSource part in the query normalization
                    }
                    else
                    {
                        sum += valSrcWeight.GetValueForNormalization();
                    }
                }
                return sum;
            }

            /*(non-Javadoc) @see org.apache.lucene.search.Weight#normalize(float) */

            public override void Normalize(float norm, float topLevelBoost)
            {
                // note we DONT incorporate our boost, nor pass down any topLevelBoost 
                // (e.g. from outer BQ), as there is no guarantee that the CustomScoreProvider's 
                // function obeys the distributive law... it might call sqrt() on the subQuery score
                // or some other arbitrary function other than multiplication.
                // so, instead boosts are applied directly in score()
                subQueryWeight.Normalize(norm, 1f);
                foreach (Weight valSrcWeight in valSrcWeights)
                {
                    if (qStrict)
                    {
                        valSrcWeight.Normalize(1, 1); // do not normalize the ValueSource part
                    }
                    else
                    {
                        valSrcWeight.Normalize(norm, 1f);
                    }
                }
                queryWeight = topLevelBoost * outerInstance.Boost;
            }

            public override Scorer Scorer(AtomicReaderContext context, Bits acceptDocs)
            {
                Scorer subQueryScorer = subQueryWeight.Scorer(context, acceptDocs);
                if (subQueryScorer == null)
                {
                    return null;
                }
                var valSrcScorers = new Scorer[valSrcWeights.Length];
                for (int i = 0; i < valSrcScorers.Length; i++)
                {
                    valSrcScorers[i] = valSrcWeights[i].Scorer(context, acceptDocs);
                }
                return new CustomScorer(outerInstance, outerInstance.GetCustomScoreProvider(context), this, queryWeight,
                    subQueryScorer, valSrcScorers);
            }

            public override Explanation Explain(AtomicReaderContext context, int doc)
            {
                Explanation explain = DoExplain(context, doc);
                return explain ?? new Explanation(0.0f, "no matching docs");
            }

            internal virtual Explanation DoExplain(AtomicReaderContext info, int doc)
            {
                var subQueryExpl = subQueryWeight.Explain(info, doc);
                if (!subQueryExpl.IsMatch)
                {
                    return subQueryExpl;
                }
                // match
                var valSrcExpls = new Explanation[valSrcWeights.Length];
                for (int i = 0; i < valSrcWeights.Length; i++)
                {
                    valSrcExpls[i] = valSrcWeights[i].Explain(info, doc);
                }
                Explanation customExp = outerInstance.GetCustomScoreProvider(info)
                    .CustomExplain(doc, subQueryExpl, valSrcExpls);
                float sc = outerInstance.Boost * customExp.Value;
                Explanation res = new ComplexExplanation(true, sc, outerInstance.ToString() + ", product of:");
                res.AddDetail(customExp);
                res.AddDetail(new Explanation(outerInstance.Boost, "queryBoost"));
                // actually using the q boost as q weight (== weight value)
                return res;
            }

            public override bool ScoresDocsOutOfOrder
            {
                get { return false; }
            }

        }

        //=========================== S C O R E R ============================

        /// <summary>
        /// A scorer that applies a (callback) function on scores of the subQuery.
        /// </summary>
        private class CustomScorer : Scorer
        {
            private readonly CustomScoreQuery outerInstance;

            private readonly float qWeight;
            private readonly Scorer subQueryScorer;
            private readonly Scorer[] valSrcScorers;
            private readonly CustomScoreProvider provider;
            private readonly float[] vScores; // reused in score() to avoid allocating this array for each doc

            // constructor
            internal CustomScorer(CustomScoreQuery outerInstance, CustomScoreProvider provider, CustomWeight w,
                float qWeight, Scorer subQueryScorer, Scorer[] valSrcScorers)
                : base(w)
            {
                this.outerInstance = outerInstance;
                this.qWeight = qWeight;
                this.subQueryScorer = subQueryScorer;
                this.valSrcScorers = valSrcScorers;
                this.vScores = new float[valSrcScorers.Length];
                this.provider = provider;
            }

            public override int NextDoc()
            {
                int doc = subQueryScorer.NextDoc();
                if (doc != NO_MORE_DOCS)
                {
                    foreach (Scorer valSrcScorer in valSrcScorers)
                    {
                        valSrcScorer.Advance(doc);
                    }
                }
                return doc;
            }

            public override int DocID
            {
                get { return subQueryScorer.DocID; }
            }

            /*(non-Javadoc) @see org.apache.lucene.search.Scorer#score() */

            public override float Score()
            {
                for (int i = 0; i < valSrcScorers.Length; i++)
                {
                    vScores[i] = valSrcScorers[i].Score();
                }
                return qWeight * provider.CustomScore(subQueryScorer.DocID, subQueryScorer.Score(), vScores);
            }

            public override int Freq
            {
                get { return subQueryScorer.Freq; }
            }

            public override ICollection<ChildScorer> Children
            {
                get { return Collections.Singleton(new ChildScorer(subQueryScorer, "CUSTOM")); }
            }

            public override int Advance(int target)
            {
                int doc = subQueryScorer.Advance(target);
                if (doc != NO_MORE_DOCS)
                {
                    foreach (Scorer valSrcScorer in valSrcScorers)
                    {
                        valSrcScorer.Advance(doc);
                    }
                }
                return doc;
            }

            public override long Cost()
            {
                return subQueryScorer.Cost();
            }
        }

        public override Weight CreateWeight(IndexSearcher searcher)
        {                
            return new CustomWeight(this, searcher);
        }

        /// <summary>
        /// Checks if this is strict custom scoring.
        /// In strict custom scoring, the <seealso cref="ValueSource"/> part does not participate in weight normalization.
        /// This may be useful when one wants full control over how scores are modified, and does 
        /// not care about normalizing by the <seealso cref="ValueSource"/> part.
        /// One particular case where this is useful if for testing this query.   
        /// <P>
        /// Note: only has effect when the <seealso cref="ValueSource"/> part is not null.
        /// </summary>
        public virtual bool Strict { get; set; }


        /// <summary>
        /// The sub-query that CustomScoreQuery wraps, affecting both the score and which documents match. </summary>
        public virtual Query SubQuery
        {
            get { return subQuery; }
        }

        /// <summary>
        /// The scoring queries that only affect the score of CustomScoreQuery. </summary>
        public virtual Query[] ScoringQueries
        {
            get { return scoringQueries; }
        }

        /// <summary>
        /// A short name of this query, used in <seealso cref="#toString(String)"/>.
        /// </summary>
        public virtual string Name
        {
            get { return "custom"; }
        }

    }
}