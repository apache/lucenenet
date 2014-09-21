using System.Collections;
using System.Collections.Generic;
using Lucene.Net.Index;
using Lucene.Net.Search;
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
    /// Returns a score for each document based on a ValueSource,
    /// often some function of the value of a field.
    /// 
    /// <b>Note: This API is experimental and may change in non backward-compatible ways in the future</b>
    /// 
    /// 
    /// </summary>
    public class FunctionQuery : Query
    {
        internal readonly ValueSource func;

        /// <param name="func"> defines the function to be used for scoring </param>
        public FunctionQuery(ValueSource func)
        {
            this.func = func;
        }

        /// <returns> The associated ValueSource </returns>
        public virtual ValueSource ValueSource
        {
            get
            {
                return func;
            }
        }

        public override Query Rewrite(IndexReader reader)
        {
            return this;
        }

        public override void ExtractTerms(HashSet<Term> terms)
        {
        }

        protected internal class FunctionWeight : Weight
        {
            private readonly FunctionQuery outerInstance;

            protected internal readonly IndexSearcher searcher;
            protected internal float queryNorm;
            protected internal float queryWeight;
            protected internal readonly IDictionary context;

            //JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
            //ORIGINAL LINE: public FunctionWeight(IndexSearcher searcher) throws java.io.IOException
            public FunctionWeight(FunctionQuery outerInstance, IndexSearcher searcher)
            {
                this.outerInstance = outerInstance;
                this.searcher = searcher;
                this.context = ValueSource.newContext(searcher);
                outerInstance.func.CreateWeight(context, searcher);
            }

            public override Query Query
            {
                get
                {
                    return outerInstance;
                }
            }

            public override float ValueForNormalization
            {
                get
                {
                    queryWeight = Boost;
                    return queryWeight * queryWeight;
                }
            }

            public override void Normalize(float norm, float topLevelBoost)
            {
                this.queryNorm = norm * topLevelBoost;
                queryWeight *= this.queryNorm;
            }

            public override Scorer Scorer(AtomicReaderContext context, Bits acceptDocs)
            {
                return new AllScorer(outerInstance, context, acceptDocs, this, queryWeight);
            }

            public override Explanation Explain(AtomicReaderContext context, int doc)
            {
                return ((AllScorer)Scorer(context, context.reader().LiveDocs)).Explain(doc);
            }
        }

        protected internal class AllScorer : Scorer
        {
            private readonly FunctionQuery outerInstance;

            internal readonly IndexReader reader;
            internal readonly FunctionWeight weight;
            internal readonly int maxDoc;
            internal readonly float qWeight;
            internal int doc = -1;
            internal readonly FunctionValues vals;
            internal readonly Bits acceptDocs;

            //JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
            //ORIGINAL LINE: public AllScorer(org.apache.lucene.index.AtomicReaderContext context, org.apache.lucene.util.Bits acceptDocs, FunctionWeight w, float qWeight) throws java.io.IOException
            public AllScorer(FunctionQuery outerInstance, AtomicReaderContext context, Bits acceptDocs, FunctionWeight w, float qWeight)
                : base(w)
            {
                this.outerInstance = outerInstance;
                this.weight = w;
                this.qWeight = qWeight;
                this.reader = context.Reader;
                this.maxDoc = reader.MaxDoc;
                this.acceptDocs = acceptDocs;
                vals = outerInstance.func.GetValues(weight.context, context);
            }

            public override int DocID()
            {
                return doc;
            }

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

            //JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
            //ORIGINAL LINE: @Override public int advance(int target) throws java.io.IOException
            public override int Advance(int target)
            {
                // this will work even if target==NO_MORE_DOCS
                doc = target - 1;
                return NextDoc();
            }

            //JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
            //ORIGINAL LINE: @Override public float score() throws java.io.IOException
            public override float Score()
            {
                float score = qWeight * vals.FloatVal(doc);

                // Current Lucene priority queues can't handle NaN and -Infinity, so
                // map to -Float.MAX_VALUE. This conditional handles both -infinity
                // and NaN since comparisons with NaN are always false.
                return score > float.NegativeInfinity ? score : -float.MaxValue;
            }

            public override long Cost()
            {
                return maxDoc;
            }

            //JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
            //ORIGINAL LINE: @Override public int freq() throws java.io.IOException
            public override int Freq()
            {
                return 1;
            }

            //JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
            //ORIGINAL LINE: public Explanation explain(int doc) throws java.io.IOException
            public virtual Explanation Explain(int doc)
            {
                float sc = qWeight * vals.FloatVal(doc);

                Explanation result = new ComplexExplanation(true, sc, "FunctionQuery(" + outerInstance.func + "), product of:");

                result.AddDetail(vals.Explain(doc));
                result.AddDetail(new Explanation(Boost, "boost"));
                result.AddDetail(new Explanation(weight.queryNorm, "queryNorm"));
                return result;
            }
        }


        //JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
        //ORIGINAL LINE: @Override public Weight CreateWeight(IndexSearcher searcher) throws java.io.IOException
        public override Weight CreateWeight(IndexSearcher searcher)
        {
            return new FunctionQuery.FunctionWeight(this, searcher);
        }


        /// <summary>
        /// Prints a user-readable version of this query. </summary>
        public override string ToString(string field)
        {
            float boost = Boost;
            return (boost != 1.0 ? "(" : "") + func.ToString() + (boost == 1.0 ? "" : ")^" + boost);
        }


        /// <summary>
        /// Returns true if <code>o</code> is equal to this. </summary>
        public override bool Equals(object o)
        {
            if (!typeof(FunctionQuery).IsInstanceOfType(o))
            {
                return false;
            }
            FunctionQuery other = (FunctionQuery)o;
            return this.Boost == other.Boost && this.func.Equals(other.func);
        }

        /// <summary>
        /// Returns a hash code value for this object. </summary>
        public override int GetHashCode()
        {
            return func.GetHashCode() * 31 + Number.FloatToIntBits(Boost);
        }

    }

}