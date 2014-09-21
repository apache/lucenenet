using System.Collections;
using System.Collections.Generic;
using System.Text;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Support;
using Lucene.Net.Util;
using org.apache.lucene.queries.function;

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
    /// Query that is boosted by a ValueSource
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

        public virtual Query Query
        {
            get
            {
                return q;
            }
        }
        public virtual ValueSource ValueSource
        {
            get
            {
                return boostVal;
            }
        }
    
        public override Query Rewrite(IndexReader reader)
        {
            Query newQ = q.Rewrite(reader);
            if (newQ == q)
            {
                return this;
            }
            BoostedQuery bq = (BoostedQuery)this.MemberwiseClone();
            bq.q = newQ;
            return bq;
        }

        public override void ExtractTerms(HashSet<Term> terms)
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

            internal readonly IndexSearcher searcher;
            internal Weight qWeight;
            internal IDictionary fcontext;

            //JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
            //ORIGINAL LINE: public BoostedWeight(IndexSearcher searcher) throws java.io.IOException
            public BoostedWeight(BoostedQuery outerInstance, IndexSearcher searcher)
            {
                this.outerInstance = outerInstance;
                this.searcher = searcher;
                this.qWeight = outerInstance.q.CreateWeight(searcher);
                this.fcontext = ValueSource.newContext(searcher);
                outerInstance.boostVal.CreateWeight(fcontext, searcher);
            }

            public override Query Query
            {
                get
                {
                    return outerInstance;
                }
            }

            //JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
            //ORIGINAL LINE: @Override public float getValueForNormalization() throws java.io.IOException
            public override float ValueForNormalization
            {
                get
                {
                    float sum = qWeight.ValueForNormalization;
                    sum *= Boost * Boost;
                    return sum;
                }
            }

            public override void normalize(float norm, float topLevelBoost)
            {
                topLevelBoost *= Boost;
                qWeight.Normalize(norm, topLevelBoost);
            }

            public override Scorer Scorer(AtomicReaderContext context, Bits acceptDocs)
            {
                Scorer subQueryScorer = qWeight.Scorer(context, acceptDocs);
                if (subQueryScorer == null)
                {
                    return null;
                }
                return new BoostedQuery.CustomScorer(outerInstance, context, this, Boost, subQueryScorer, outerInstance.boostVal);
            }

            public override Explanation Explain(AtomicReaderContext readerContext, int doc)
            {
                Explanation subQueryExpl = qWeight.Explain(readerContext, doc);
                if (!subQueryExpl.Match)
                {
                    return subQueryExpl;
                }
                FunctionValues vals = outerInstance.boostVal.GetValues(fcontext, readerContext);
                float sc = subQueryExpl.Value * vals.FloatVal(doc);
                Explanation res = new ComplexExplanation(true, sc, outerInstance.ToString() + ", product of:");
                res.AddDetail(subQueryExpl);
                res.AddDetail(vals.explain(doc));
                return res;
            }
        }


        private class CustomScorer : Scorer
        {
            private readonly BoostedQuery outerInstance;

            internal readonly BoostedQuery.BoostedWeight weight;
            internal readonly float qWeight;
            internal readonly Scorer scorer;
            internal readonly FunctionValues vals;
            internal readonly AtomicReaderContext readerContext;

            //JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
            //ORIGINAL LINE: private CustomScorer(org.apache.lucene.index.AtomicReaderContext readerContext, BoostedQuery.BoostedWeight w, float qWeight, Scorer scorer, ValueSource vs) throws java.io.IOException
            internal CustomScorer(BoostedQuery outerInstance, AtomicReaderContext readerContext, BoostedQuery.BoostedWeight w, float qWeight, Scorer scorer, ValueSource vs)
                : base(w)
            {
                this.outerInstance = outerInstance;
                this.weight = w;
                this.qWeight = qWeight;
                this.scorer = scorer;
                this.readerContext = readerContext;
                this.vals = vs.GetValues(weight.fcontext, readerContext);
            }

            public override int DocID()
            {
                return scorer.DocID();
            }

            //JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
            //ORIGINAL LINE: @Override public int advance(int target) throws java.io.IOException
            public override int Advance(int target)
            {
                return scorer.Advance(target);
            }

            //JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
            //ORIGINAL LINE: @Override public int nextDoc() throws java.io.IOException
            public override int NextDoc()
            {
                return scorer.NextDoc();
            }

            //JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
            //ORIGINAL LINE: @Override public float score() throws java.io.IOException
            public override float Score()
            {
                float score = qWeight * scorer.Score() * vals.FloatVal(scorer.DocID());

                // Current Lucene priority queues can't handle NaN and -Infinity, so
                // map to -Float.MAX_VALUE. This conditional handles both -infinity
                // and NaN since comparisons with NaN are always false.
                return score > float.NegativeInfinity ? score : -float.MaxValue;
            }

            public override int Freq()
            {
                return scorer.Freq();
            }

            public override ICollection<ChildScorer> Children
            {
                get
                {
                    return Collections.Singleton(new ChildScorer(scorer, "CUSTOM"));
                }
            }

            //JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
            //ORIGINAL LINE: public Explanation explain(int doc) throws java.io.IOException
            public virtual Explanation explain(int doc)
            {
                Explanation subQueryExpl = weight.qWeight.Explain(readerContext, doc);
                if (!subQueryExpl.Match)
                {
                    return subQueryExpl;
                }
                float sc = subQueryExpl.Value * vals.FloatVal(doc);
                Explanation res = new ComplexExplanation(true, sc, outerInstance.ToString() + ", product of:");
                res.AddDetail(subQueryExpl);
                res.AddDetail(vals.explain(doc));
                return res;
            }

            public override long Cost()
            {
                return scorer.Cost();
            }
        }


        public override string ToString(string field)
        {
            StringBuilder sb = new StringBuilder();
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
            BoostedQuery other = (BoostedQuery)o;
            return this.q.Equals(other.q) && this.boostVal.Equals(other.boostVal);
        }

        public override int GetHashCode()
        {
            int h = q.GetHashCode();
            h ^= (h << 17) | ((int)((uint)h >> 16));
            h += boostVal.GetHashCode();
            h ^= (h << 8) | ((int)((uint)h >> 25));
            h += Number.FloatToIntBits(Boost);
            return h;
        }
    }
}