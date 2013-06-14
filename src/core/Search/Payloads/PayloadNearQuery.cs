/* 
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
 * (the "License"); you may not use this file except in compliance with
 * the License.  You may obtain a copy of the License at
 * 
 * http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;

using IndexReader = Lucene.Net.Index.IndexReader;
using ToStringUtils = Lucene.Net.Util.ToStringUtils;
using Explanation = Lucene.Net.Search.Explanation;
using Scorer = Lucene.Net.Search.Scorer;
using Searcher = Lucene.Net.Search.Searcher;
using Similarity = Lucene.Net.Search.Similarity;
using Weight = Lucene.Net.Search.Weight;
using NearSpansOrdered = Lucene.Net.Search.Spans.NearSpansOrdered;
using NearSpansUnordered = Lucene.Net.Search.Spans.NearSpansUnordered;
using SpanNearQuery = Lucene.Net.Search.Spans.SpanNearQuery;
using SpanQuery = Lucene.Net.Search.Spans.SpanQuery;
using SpanScorer = Lucene.Net.Search.Spans.SpanScorer;
using SpanWeight = Lucene.Net.Search.Spans.SpanWeight;
using System.Text;
using Lucene.Net.Index;
using Lucene.Net.Util;

namespace Lucene.Net.Search.Payloads
{

    /// <summary> This class is very similar to
    /// <see cref="Lucene.Net.Search.Spans.SpanNearQuery" /> except that it factors
    /// in the value of the payloads located at each of the positions where the
    /// <see cref="Lucene.Net.Search.Spans.TermSpans" /> occurs.
    /// <p/>
    /// In order to take advantage of this, you must override
    /// <see cref="Lucene.Net.Search.Similarity.ScorePayload" />
    /// which returns 1 by default.
    /// <p/>
    /// Payload scores are aggregated using a pluggable <see cref="PayloadFunction" />.
    /// 
    /// </summary>
    /// <seealso cref="Lucene.Net.Search.Similarity.ScorePayload">
    /// </seealso>
    [Serializable]
    public class PayloadNearQuery : SpanNearQuery, ICloneable
    {
        protected String fieldName;
        protected PayloadFunction function;

        public PayloadNearQuery(SpanQuery[] clauses, int slop, bool inOrder)
            : this(clauses, slop, inOrder, new AveragePayloadFunction())
        {
        }

        public PayloadNearQuery(SpanQuery[] clauses, int slop, bool inOrder, PayloadFunction function)
            : base(clauses, slop, inOrder)
        {
            fieldName = clauses[0].Field; // all clauses must have same field
            this.function = function;
        }

        public override Weight CreateWeight(Searcher searcher)
        {
            return new PayloadNearSpanWeight(this, this, searcher);
        }

        public override Object Clone()
        {
            int sz = clauses.Count;
            SpanQuery[] newClauses = new SpanQuery[sz];

            for (int i = 0; i < sz; i++)
            {
                newClauses[i] = clauses[i];
            }
            PayloadNearQuery boostingNearQuery = new PayloadNearQuery(newClauses, internalSlop, inOrder);
            boostingNearQuery.Boost = Boost;
            return boostingNearQuery;
        }

        public override String ToString(String field)
        {
            StringBuilder buffer = new StringBuilder();
            buffer.Append("payloadNear([");
            var i = clauses.GetEnumerator();
            bool hasLastComma = false;
            while (i.MoveNext())
            {
                SpanQuery clause = i.Current;
                buffer.Append(clause.ToString(field));
                buffer.Append(", ");
                hasLastComma = true;
            }
            if (hasLastComma)
                buffer.Remove(buffer.Length - 1, 1);
            buffer.Append("], ");
            buffer.Append(internalSlop);
            buffer.Append(", ");
            buffer.Append(inOrder);
            buffer.Append(")");
            buffer.Append(ToStringUtils.Boost(Boost));
            return buffer.ToString();
        }

        public override int GetHashCode()
        {
            int prime = 31;
            int result = base.GetHashCode();
            result = prime * result + ((fieldName == null) ? 0 : fieldName.GetHashCode());
            result = prime * result + ((function == null) ? 0 : function.GetHashCode());
            return result;
        }

        public override bool Equals(System.Object obj)
        {
            if (this == obj)
                return true;
            if (!base.Equals(obj))
                return false;
            if (GetType() != obj.GetType())
                return false;
            PayloadNearQuery other = (PayloadNearQuery)obj;
            if (fieldName == null)
            {
                if (other.fieldName != null)
                    return false;
            }
            else if (!fieldName.Equals(other.fieldName))
                return false;
            if (function == null)
            {
                if (other.function != null)
                    return false;
            }
            else if (!function.Equals(other.function))
                return false;
            return true;
        }

        [Serializable]
        public class PayloadNearSpanWeight : SpanWeight
        {
            public PayloadNearSpanWeight(PayloadNearQuery enclosingInstance, SpanQuery query, Searcher searcher)
                : base(query, searcher)
            {
                this.enclosingInstance = enclosingInstance;
            }

            private PayloadNearQuery enclosingInstance;
            public PayloadNearQuery EnclosingInstance
            {
                get
                {
                    return enclosingInstance;
                }
            }

            public override Scorer Scorer(AtomicReaderContext context, bool scoreDocsInOrder, bool topScorer, IBits acceptDocs)
            {
                return new PayloadNearSpanScorer(Query.GetSpans(context, acceptDocs, base.termContexts), this,
                    similarity, similarity.SloppySimScorer(base.stats, context));
            }

            public override Explanation Explain(AtomicReaderContext context, int doc)
            {
                PayloadNearSpanScorer scorer = (PayloadNearSpanScorer)scorer(context, true, false, context.Reader.GetLiveDocs());
                if (scorer != null)
                {
                    int newDoc = scorer.Advance(doc);
                    if (newDoc == doc)
                    {
                        float freq = scorer.Freq();
                        SloppySimScorer docScorer = similarity.SloppySimScorer(stats, context);
                        Explanation expl = new Explanation();
                        expl.Description = "weight(" + Query + " in " + doc + ") [" + similarity.GetType().Name + "], result of:");
                        Explanation scoreExplanation = docScorer.explain(doc, new Explanation(freq, "phraseFreq=" + freq));
                        expl.AddDetail(scoreExplanation);
                        expl.Value = scoreExplanation.Value;
                        String field = ((SpanQuery)Query).Field;
                        // now the payloads part
                        Explanation payloadExpl = enclosingInstance.function.Explain(doc, field, scorer.payloadsSeen, scorer.payloadScore);
                        // combined
                        ComplexExplanation result = new ComplexExplanation();
                        result.AddDetail(expl);
                        result.AddDetail(payloadExpl);
                        result.Value = expl.Value * payloadExpl.Value;
                        result.Description = "PayloadNearQuery, product of:";
                        return result;
                    }
                }

                return new ComplexExplanation(false, 0.0f, "no matching term");

            }
        }

        public class PayloadNearSpanScorer : SpanScorer
        {

            private PayloadNearQuery enclosingInstance;

            new internal Lucene.Net.Search.Spans.Spans spans;

            internal float payloadScore;
            internal int payloadsSeen;

            protected PayloadNearSpanScorer(Lucene.Net.Search.Spans.Spans spans, Weight weight,
                Similarity similarity, Similarity.SloppySimScorer docScorer)
                : base(spans, weight, docScorer)
            {
                this.spans = spans;
            }

            // Get the payloads associated with all underlying subspans
            public virtual void GetPayloads(Lucene.Net.Search.Spans.Spans[] subSpans)
            {
                for (int i = 0; i < subSpans.Length; i++)
                {
                    if (subSpans[i] is NearSpansOrdered)
                    {
                        if (((NearSpansOrdered)subSpans[i]).IsPayloadAvailable())
                        {
                            ProcessPayloads(((NearSpansOrdered)subSpans[i]).GetPayload(), subSpans[i].Start(), subSpans[i].End());
                        }
                        GetPayloads(((NearSpansOrdered)subSpans[i]).GetSubSpans());
                    }
                    else if (subSpans[i] is NearSpansUnordered)
                    {
                        if (((NearSpansUnordered)subSpans[i]).IsPayloadAvailable())
                        {
                            ProcessPayloads(((NearSpansUnordered)subSpans[i]).GetPayload(), subSpans[i].Start(), subSpans[i].End());
                        }
                        GetPayloads(((NearSpansUnordered)subSpans[i]).GetSubSpans());
                    }
                }
            }

            // TODO change the whole spans api to use bytesRef, or nuke spans
            BytesRef scratch = new BytesRef();

            /// <summary> By default, uses the <see cref="PayloadFunction" /> to score the payloads, but
            /// can be overridden to do other things.
            /// 
            /// </summary>
            /// <param name="payLoads">The payloads
            /// </param>
            /// <param name="start">The start position of the span being scored
            /// </param>
            /// <param name="end">The end position of the span being scored
            /// 
            /// </param>
            /// <seealso cref="Spans">
            /// </seealso>
            protected internal virtual void ProcessPayloads(System.Collections.Generic.ICollection<sbyte[]> payLoads, int start, int end)
            {
                foreach (sbyte[] thePayload in payLoads)
                {
                    scratch.bytes = thePayload;
                    scratch.offset = 0;
                    scratch.length = thePayload.Length;
                    payloadScore = enclosingInstance.function.CurrentScore(doc, enclosingInstance.fieldName, start, end,
                        payloadsSeen, payloadScore, docScorer.computePayloadFactor(doc,
                            spans.Start(), spans.End(), scratch));
                    ++payloadsSeen;
                }
            }

            //
            protected override bool SetFreqCurrentDoc()
            {
                if (!more)
                {
                    return false;
                }
                doc = spans.Doc();
                freq = 0.0f;
                payloadScore = 0;
                payloadsSeen = 0;
                do
                {
                    int matchLength = spans.End() - spans.Start();
                    freq += docScorer.computeSlopFactor(matchLength);
                    Lucene.Net.Search.Spans.Spans[] spansArr = new Lucene.Net.Search.Spans.Spans[1];
                    spansArr[0] = spans;
                    GetPayloads(spansArr);
                    more = spans.Next();
                } while (more && (doc == spans.Doc()));
                return true;
            }

            public override float Score()
            {
                return base.Score() * enclosingInstance.function.DocScore(doc, enclosingInstance.fieldName, payloadsSeen, payloadScore);
            }

        }
    }
}