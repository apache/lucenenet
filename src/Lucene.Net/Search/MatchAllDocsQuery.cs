using Lucene.Net.Util;
using System.Collections.Generic;
using System.Text;

namespace Lucene.Net.Search
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

    using AtomicReaderContext = Lucene.Net.Index.AtomicReaderContext;
    using IBits = Lucene.Net.Util.IBits;
    using IndexReader = Lucene.Net.Index.IndexReader;
    using Term = Lucene.Net.Index.Term;
    using ToStringUtils = Lucene.Net.Util.ToStringUtils;

    /// <summary>
    /// A query that matches all documents.
    /// </summary>
    public class MatchAllDocsQuery : Query
    {
        private class MatchAllScorer : Scorer
        {
            internal readonly float score;
            private int doc = -1;
            private readonly int maxDoc;
            private readonly IBits liveDocs;

            internal MatchAllScorer(IndexReader reader, IBits liveDocs, Weight w, float score)
                : base(w)
            {
                this.liveDocs = liveDocs;
                this.score = score;
                maxDoc = reader.MaxDoc;
            }

            public override int DocID => doc;

            public override int NextDoc()
            {
                doc++;
                while (liveDocs != null && doc < maxDoc && !liveDocs.Get(doc))
                {
                    doc++;
                }
                if (doc == maxDoc)
                {
                    doc = NO_MORE_DOCS;
                }
                return doc;
            }

            public override float GetScore()
            {
                return score;
            }

            public override int Freq => 1;

            public override int Advance(int target)
            {
                doc = target - 1;
                return NextDoc();
            }

            public override long GetCost()
            {
                return maxDoc;
            }
        }

        private class MatchAllDocsWeight : Weight
        {
            private readonly MatchAllDocsQuery outerInstance;

            private float queryWeight;
            private float queryNorm;

            public MatchAllDocsWeight(MatchAllDocsQuery outerInstance /*, IndexSearcher searcher // LUCENENET: Never read */)
            {
                this.outerInstance = outerInstance;
            }

            public override string ToString()
            {
                return "weight(" + outerInstance + ")";
            }

            public override Query Query => outerInstance;

            public override float GetValueForNormalization()
            {
                queryWeight = outerInstance.Boost;
                return queryWeight * queryWeight;
            }

            public override void Normalize(float queryNorm, float topLevelBoost)
            {
                this.queryNorm = queryNorm * topLevelBoost;
                queryWeight *= this.queryNorm;
            }

            public override Scorer GetScorer(AtomicReaderContext context, IBits acceptDocs)
            {
                return new MatchAllScorer(context.Reader, acceptDocs, this, queryWeight);
            }

            public override Explanation Explain(AtomicReaderContext context, int doc)
            {
                // explain query weight
                Explanation queryExpl = new ComplexExplanation(true, queryWeight, "MatchAllDocsQuery, product of:");
                if (outerInstance.Boost != 1.0f)
                {
                    queryExpl.AddDetail(new Explanation(outerInstance.Boost, "boost"));
                }
                queryExpl.AddDetail(new Explanation(queryNorm, "queryNorm"));

                return queryExpl;
            }
        }

        public override Weight CreateWeight(IndexSearcher searcher)
        {
            return new MatchAllDocsWeight(this /*, searcher // LUCENENET: Never read */);
        }

        public override void ExtractTerms(ISet<Term> terms)
        {
            // LUCENENET: Intentionally blank
        }

        public override string ToString(string field)
        {
            StringBuilder buffer = new StringBuilder();
            buffer.Append("*:*");
            buffer.Append(ToStringUtils.Boost(Boost));
            return buffer.ToString();
        }

        public override bool Equals(object o)
        {
            if (!(o is MatchAllDocsQuery))
            {
                return false;
            }
            MatchAllDocsQuery other = (MatchAllDocsQuery)o;
            // LUCENENET specific - compare bits rather than using equality operators to prevent these comparisons from failing in x86 in .NET Framework with optimizations enabled
            return NumericUtils.SingleToSortableInt32(this.Boost) == NumericUtils.SingleToSortableInt32(other.Boost);
        }

        public override int GetHashCode()
        {
            return J2N.BitConversion.SingleToInt32Bits(Boost) ^ 0x1AA71190;
        }
    }
}