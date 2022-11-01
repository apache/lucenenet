// Lucene version compatibility level 4.8.1
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Util;
using System.Collections;
using System.Collections.Generic;
using System.IO;

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
    /// Returns a score for each document based on a <see cref="Function.ValueSource"/>,
    /// often some function of the value of a field.
    /// 
    /// <b>Note: This API is experimental and may change in non backward-compatible ways in the future</b>
    /// </summary>
    public class FunctionQuery : Query
    {
        private readonly ValueSource func;

        /// <param name="func"> defines the function to be used for scoring </param>
        public FunctionQuery(ValueSource func)
        {
            this.func = func;
        }

        /// <returns> The associated <see cref="Function.ValueSource"/> </returns>
        public virtual ValueSource ValueSource => func;

        public override Query Rewrite(IndexReader reader)
        {
            return this;
        }

        public override void ExtractTerms(ISet<Term> terms)
        {
            // LUCENENET: Intentionally blank
        }

        protected internal class FunctionWeight : Weight
        {
            private readonly FunctionQuery outerInstance;

            protected readonly IndexSearcher m_searcher;
            protected internal float m_queryNorm;
            protected float m_queryWeight;
            protected internal readonly IDictionary m_context;

            public FunctionWeight(FunctionQuery outerInstance, IndexSearcher searcher)
            {
                this.outerInstance = outerInstance;
                this.m_searcher = searcher;
                this.m_context = ValueSource.NewContext(searcher);
                outerInstance.func.CreateWeight(m_context, searcher);
            }

            public override Query Query => outerInstance;

            public override float GetValueForNormalization()
            {
                m_queryWeight = outerInstance.Boost;
                return m_queryWeight * m_queryWeight;
            }

            public override void Normalize(float norm, float topLevelBoost)
            {
                this.m_queryNorm = norm * topLevelBoost;
                m_queryWeight *= this.m_queryNorm;
            }

            public override Scorer GetScorer(AtomicReaderContext ctx, IBits acceptDocs)
            {
                return new AllScorer(outerInstance, ctx, acceptDocs, this, m_queryWeight);
            }

            public override Explanation Explain(AtomicReaderContext ctx, int doc)
            {
                return ((AllScorer)GetScorer(ctx, ctx.AtomicReader.LiveDocs)).Explain(doc);
            }
        }

        protected class AllScorer : Scorer
        {
            private readonly FunctionQuery outerInstance;

            private readonly IndexReader reader;
            private readonly FunctionWeight weight;
            private readonly int maxDoc;
            private readonly float qWeight;
            private int doc = -1;
            private readonly FunctionValues vals;
            private readonly IBits acceptDocs;

            /// <exception cref="IOException"/>
            public AllScorer(FunctionQuery outerInstance, AtomicReaderContext context, IBits acceptDocs, FunctionWeight w, float qWeight)
                : base(w)
            {
                this.outerInstance = outerInstance;
                this.weight = w;
                this.qWeight = qWeight;
                this.reader = context.Reader;
                this.maxDoc = reader.MaxDoc;
                this.acceptDocs = acceptDocs;
                vals = outerInstance.func.GetValues(weight.m_context, context);
            }

            public override int DocID => doc;

            // instead of matching all docs, we could also embed a query.
            // the score could either ignore the subscore, or boost it.
            // Containment:  floatline(foo:myTerm, "myFloatField", 1.0, 0.0f)
            // Boost:        foo:myTerm^floatline("myFloatField",1.0,0.0f)
            public override int NextDoc()
            {
                for (; ; )
                {
                    ++doc;
                    if (doc >= maxDoc)
                    {
                        return doc = NO_MORE_DOCS;
                    }
                    if (acceptDocs != null && !acceptDocs.Get(doc))
                    {
                        continue;
                    }
                    return doc;
                }
            }

            public override int Advance(int target)
            {
                // this will work even if target==NO_MORE_DOCS
                doc = target - 1;
                return NextDoc();
            }

            public override float GetScore()
            {
                float score = qWeight * vals.SingleVal(doc);

                // Current Lucene priority queues can't handle NaN and -Infinity, so
                // map to -Float.MAX_VALUE. This conditional handles both -infinity
                // and NaN since comparisons with NaN are always false.
                return score > float.NegativeInfinity ? score : -float.MaxValue;
            }

            public override long GetCost()
            {
                return maxDoc;
            }

            public override int Freq => 1;

            public virtual Explanation Explain(int d)
            {
                float sc = qWeight * vals.SingleVal(d);

                Explanation result = new ComplexExplanation(true, sc, "FunctionQuery(" + outerInstance.func + "), product of:");

                result.AddDetail(vals.Explain(d));
                result.AddDetail(new Explanation(outerInstance.Boost, "boost"));
                result.AddDetail(new Explanation(weight.m_queryNorm, "queryNorm"));
                return result;
            }
        }

        public override Weight CreateWeight(IndexSearcher searcher)
        {
            return new FunctionQuery.FunctionWeight(this, searcher);
        }

        /// <summary>
        /// Prints a user-readable version of this query.
        /// </summary>
        public override string ToString(string field)
        {
            float boost = Boost;
            return (boost != 1.0 ? "(" : "") + func + (boost == 1.0 ? "" : ")^" + boost);
        }


        /// <summary>
        /// Returns true if <paramref name="o"/> is equal to this.
        /// </summary>
        public override bool Equals(object o)
        {
            if (o is null) return false;
            if (!(o is FunctionQuery other)) return false;
            return Boost == other.Boost 
                && func.Equals(other.func);
        }

        /// <summary>
        /// Returns a hash code value for this object.
        /// </summary>
        public override int GetHashCode()
        {
            return func.GetHashCode() * 31 + J2N.BitConversion.SingleToInt32Bits(Boost);
        }
    }
}