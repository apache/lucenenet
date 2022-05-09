// Lucene version compatibility level 4.8.1
using J2N.Numerics;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Util;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using JCG = J2N.Collections.Generic;

namespace Lucene.Net.Queries.Function
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
    /// Query that is boosted by a <see cref="Function.ValueSource"/>
    /// </summary>
    // TODO: BoostedQuery and BoostingQuery in the same module? 
    // something has to give
    public class BoostedQuery : Query
    {
        private Query q;
        private readonly ValueSource boostVal; // optional, can be null

        public BoostedQuery(Query subQuery, ValueSource boostVal)
        {
            this.q = subQuery;
            this.boostVal = boostVal;
        }

        public virtual Query Query => q;

        public virtual ValueSource ValueSource => boostVal;
    
        public override Query Rewrite(IndexReader reader)
        {
            var newQ = q.Rewrite(reader);
            if (Equals(newQ, q))
            {
                return this;
            }
            var bq = (BoostedQuery)this.MemberwiseClone();
            bq.q = newQ;
            return bq;
        }

        public override void ExtractTerms(ISet<Term> terms)
        {
            q.ExtractTerms(terms);
        }

        public override Weight CreateWeight(IndexSearcher searcher)
        {
            return new BoostedQuery.BoostedWeight(this, searcher);
        }

        private class BoostedWeight : Weight
        {
            private readonly BoostedQuery outerInstance;

            //private readonly IndexSearcher searcher; // LUCENENET: Never read
            internal readonly Weight qWeight;
            internal readonly IDictionary fcontext;

            public BoostedWeight(BoostedQuery outerInstance, IndexSearcher searcher)
            {
                this.outerInstance = outerInstance;
                //this.searcher = searcher; // LUCENENET: Never read
                this.qWeight = outerInstance.q.CreateWeight(searcher);
                this.fcontext = ValueSource.NewContext(searcher);
                outerInstance.boostVal.CreateWeight(fcontext, searcher);
            }

            public override Query Query => outerInstance;

            public override float GetValueForNormalization()
            {
                float sum = qWeight.GetValueForNormalization();
                sum *= outerInstance.Boost * outerInstance.Boost;
                return sum;
            }

            public override void Normalize(float norm, float topLevelBoost)
            {
                topLevelBoost *= outerInstance.Boost;
                qWeight.Normalize(norm, topLevelBoost);
            }

            public override Scorer GetScorer(AtomicReaderContext context, IBits acceptDocs)
            {
                Scorer subQueryScorer = qWeight.GetScorer(context, acceptDocs);
                if (subQueryScorer is null)
                {
                    return null;
                }
                return new BoostedQuery.CustomScorer(outerInstance, context, this, outerInstance.Boost, subQueryScorer, outerInstance.boostVal);
            }

            public override Explanation Explain(AtomicReaderContext readerContext, int doc)
            {
                Explanation subQueryExpl = qWeight.Explain(readerContext, doc);
                if (!subQueryExpl.IsMatch)
                {
                    return subQueryExpl;
                }
                FunctionValues vals = outerInstance.boostVal.GetValues(fcontext, readerContext);
                float sc = subQueryExpl.Value * vals.SingleVal(doc);
                Explanation res = new ComplexExplanation(true, sc, outerInstance.ToString() + ", product of:");
                res.AddDetail(subQueryExpl);
                res.AddDetail(vals.Explain(doc));
                return res;
            }
        }

        private sealed class CustomScorer : Scorer
        {
            private readonly BoostedQuery outerInstance;

            private readonly BoostedQuery.BoostedWeight weight;
            private readonly float qWeight;
            private readonly Scorer scorer;
            private readonly FunctionValues vals;
            private readonly AtomicReaderContext readerContext;

            public CustomScorer(BoostedQuery outerInstance, AtomicReaderContext readerContext, BoostedQuery.BoostedWeight w, float qWeight, Scorer scorer, ValueSource vs)
                : base(w)
            {
                this.outerInstance = outerInstance;
                this.weight = w;
                this.qWeight = qWeight;
                this.scorer = scorer;
                this.readerContext = readerContext;
                this.vals = vs.GetValues(weight.fcontext, readerContext);
            }

            public override int DocID => scorer.DocID;

            public override int Advance(int target)
            {
                return scorer.Advance(target);
            }

            public override int NextDoc()
            {
                return scorer.NextDoc();
            }

            public override float GetScore()
            {
                float score = qWeight * scorer.GetScore() * vals.SingleVal(scorer.DocID);

                // Current Lucene priority queues can't handle NaN and -Infinity, so
                // map to -Float.MAX_VALUE. This conditional handles both -infinity
                // and NaN since comparisons with NaN are always false.
                return score > float.NegativeInfinity ? score : -float.MaxValue;
            }

            public override int Freq => scorer.Freq;

            public override ICollection<ChildScorer> GetChildren()
            {
                return new JCG.List<ChildScorer> { new ChildScorer(scorer, "CUSTOM") };
            }

            public Explanation Explain(int doc)
            {
                var subQueryExpl = weight.qWeight.Explain(readerContext, doc);
                if (!subQueryExpl.IsMatch)
                {
                    return subQueryExpl;
                }
                float sc = subQueryExpl.Value * vals.SingleVal(doc);
                Explanation res = new ComplexExplanation(true, sc, outerInstance.ToString() + ", product of:");
                res.AddDetail(subQueryExpl);
                res.AddDetail(vals.Explain(doc));
                return res;
            }

            public override long GetCost()
            {
                return scorer.GetCost();
            }
        }


        public override string ToString(string field)
        {
            var sb = new StringBuilder();
            sb.Append("boost(").Append(q.ToString(field)).Append(',').Append(boostVal).Append(')');
            sb.Append(ToStringUtils.Boost(Boost));
            return sb.ToString();
        }

        public override bool Equals(object o)
        {
            if (!base.Equals(o))
            {
                return false;
            }
            var other = (BoostedQuery)o;
            return this.q.Equals(other.q) && this.boostVal.Equals(other.boostVal);
        }

        public override int GetHashCode()
        {
            int h = q.GetHashCode();
            h ^= (h << 17) | (h.TripleShift(16));
            h += boostVal.GetHashCode();
            h ^= (h << 8) | (h.TripleShift(25));
            h += J2N.BitConversion.SingleToInt32Bits(Boost);
            return h;
        }
    }
}