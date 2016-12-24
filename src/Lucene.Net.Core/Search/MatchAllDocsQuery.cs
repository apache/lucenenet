using System.Text;

namespace Lucene.Net.Search
{
    using Lucene.Net.Support;
    using System.Collections.Generic;

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
    using Bits = Lucene.Net.Util.Bits;
    using IndexReader = Lucene.Net.Index.IndexReader;
    using Term = Lucene.Net.Index.Term;
    using ToStringUtils = Lucene.Net.Util.ToStringUtils;

    /// <summary>
    /// A query that matches all documents.
    ///
    /// </summary>
    public class MatchAllDocsQuery : Query
    {
        private class MatchAllScorer : Scorer
        {
            private readonly MatchAllDocsQuery OuterInstance;

            internal readonly float Score_Renamed; // LUCENENET TODO: Rename (private)
            private int Doc = -1; // LUCENENET TODO: Rename (private)
            private readonly int MaxDoc; // LUCENENET TODO: Rename (private)
            private readonly Bits LiveDocs; // LUCENENET TODO: Rename (private)

            internal MatchAllScorer(MatchAllDocsQuery outerInstance, IndexReader reader, Bits liveDocs, Weight w, float score)
                : base(w)
            {
                this.OuterInstance = outerInstance;
                this.LiveDocs = liveDocs;
                this.Score_Renamed = score;
                MaxDoc = reader.MaxDoc;
            }

            public override int DocID()
            {
                return Doc;
            }

            public override int NextDoc()
            {
                Doc++;
                while (LiveDocs != null && Doc < MaxDoc && !LiveDocs.Get(Doc))
                {
                    Doc++;
                }
                if (Doc == MaxDoc)
                {
                    Doc = NO_MORE_DOCS;
                }
                return Doc;
            }

            public override float Score()
            {
                return Score_Renamed;
            }

            public override int Freq
            {
                get { return 1; }
            }

            public override int Advance(int target)
            {
                Doc = target - 1;
                return NextDoc();
            }

            public override long Cost()
            {
                return MaxDoc;
            }
        }

        private class MatchAllDocsWeight : Weight
        {
            private readonly MatchAllDocsQuery OuterInstance; // LUCENENET TODO: Rename (private)

            private float QueryWeight; // LUCENENET TODO: Rename (private)
            private float QueryNorm; // LUCENENET TODO: Rename (private)

            public MatchAllDocsWeight(MatchAllDocsQuery outerInstance, IndexSearcher searcher)
            {
                this.OuterInstance = outerInstance;
            }

            public override string ToString()
            {
                return "weight(" + OuterInstance + ")";
            }

            public override Query Query
            {
                get
                {
                    return OuterInstance;
                }
            }

            public override float ValueForNormalization
            {
                get
                {
                    QueryWeight = OuterInstance.Boost;
                    return QueryWeight * QueryWeight;
                }
            }

            public override void Normalize(float queryNorm, float topLevelBoost)
            {
                this.QueryNorm = queryNorm * topLevelBoost;
                QueryWeight *= this.QueryNorm;
            }

            public override Scorer Scorer(AtomicReaderContext context, Bits acceptDocs)
            {
                return new MatchAllScorer(OuterInstance, context.Reader, acceptDocs, this, QueryWeight);
            }

            public override Explanation Explain(AtomicReaderContext context, int doc)
            {
                // explain query weight
                Explanation queryExpl = new ComplexExplanation(true, QueryWeight, "MatchAllDocsQuery, product of:");
                if (OuterInstance.Boost != 1.0f)
                {
                    queryExpl.AddDetail(new Explanation(OuterInstance.Boost, "boost"));
                }
                queryExpl.AddDetail(new Explanation(QueryNorm, "queryNorm"));

                return queryExpl;
            }
        }

        public override Weight CreateWeight(IndexSearcher searcher)
        {
            return new MatchAllDocsWeight(this, searcher);
        }

        public override void ExtractTerms(ISet<Term> terms)
        {
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
            return this.Boost == other.Boost;
        }

        public override int GetHashCode()
        {
            return Number.FloatToIntBits(Boost) ^ 0x1AA71190;
        }
    }
}