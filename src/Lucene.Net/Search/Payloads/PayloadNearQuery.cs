using System;
using System.Collections.Generic;
using System.Text;

namespace Lucene.Net.Search.Payloads
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
    using BytesRef = Lucene.Net.Util.BytesRef;
    using NearSpansOrdered = Lucene.Net.Search.Spans.NearSpansOrdered;
    using NearSpansUnordered = Lucene.Net.Search.Spans.NearSpansUnordered;
    using Similarity = Lucene.Net.Search.Similarities.Similarity;
    using SpanNearQuery = Lucene.Net.Search.Spans.SpanNearQuery;
    using SpanQuery = Lucene.Net.Search.Spans.SpanQuery;
    using Spans = Lucene.Net.Search.Spans.Spans;
    using SpanScorer = Lucene.Net.Search.Spans.SpanScorer;
    using SpanWeight = Lucene.Net.Search.Spans.SpanWeight;
    using ToStringUtils = Lucene.Net.Util.ToStringUtils;

    /// <summary>
    /// This class is very similar to
    /// <see cref="Lucene.Net.Search.Spans.SpanNearQuery"/> except that it factors
    /// in the value of the payloads located at each of the positions where the
    /// <see cref="Lucene.Net.Search.Spans.TermSpans"/> occurs.
    /// <para/>
    /// NOTE: In order to take advantage of this with the default scoring implementation
    /// (<see cref="Similarities.DefaultSimilarity"/>), you must override <see cref="Similarities.DefaultSimilarity.ScorePayload(int, int, int, BytesRef)"/>,
    /// which returns 1 by default.
    /// <para/>
    /// Payload scores are aggregated using a pluggable <see cref="PayloadFunction"/>.
    /// </summary>
    /// <seealso cref="Lucene.Net.Search.Similarities.Similarity.SimScorer.ComputePayloadFactor(int, int, int, BytesRef)"/>
    public class PayloadNearQuery : SpanNearQuery
    {
        protected string m_fieldName;
        protected PayloadFunction m_function;

        public PayloadNearQuery(SpanQuery[] clauses, int slop, bool inOrder)
            : this(clauses, slop, inOrder, new AveragePayloadFunction())
        {
        }

        public PayloadNearQuery(SpanQuery[] clauses, int slop, bool inOrder, PayloadFunction function)
            : base(clauses, slop, inOrder)
        {
            m_fieldName = clauses[0].Field; // all clauses must have same field
            this.m_function = function;
        }

        public override Weight CreateWeight(IndexSearcher searcher)
        {
            return new PayloadNearSpanWeight(this, this, searcher);
        }

        public override object Clone()
        {
            int sz = m_clauses.Count;
            SpanQuery[] newClauses = new SpanQuery[sz];

            for (int i = 0; i < sz; i++)
            {
                newClauses[i] = (SpanQuery)m_clauses[i].Clone();
            }
            PayloadNearQuery boostingNearQuery = new PayloadNearQuery(newClauses, m_slop, m_inOrder, m_function);
            boostingNearQuery.Boost = Boost;
            return boostingNearQuery;
        }

        public override string ToString(string field)
        {
            StringBuilder buffer = new StringBuilder();
            buffer.Append("payloadNear([");
            bool hasCommaSpace = false;
            foreach (SpanQuery clause in m_clauses)
            {
                buffer.Append(clause.ToString(field));
                buffer.Append(", ");
                hasCommaSpace = true;
            }

            if (hasCommaSpace)
                buffer.Remove(buffer.Length - 2, 2);

            buffer.Append("], ");
            buffer.Append(m_slop);
            buffer.Append(", ");
            buffer.Append(m_inOrder);
            buffer.Append(')');
            buffer.Append(ToStringUtils.Boost(Boost));
            return buffer.ToString();
        }

        public override int GetHashCode()
        {
            const int prime = 31;
            int result = base.GetHashCode();
            result = prime * result + ((m_fieldName is null) ? 0 : m_fieldName.GetHashCode());
            result = prime * result + ((m_function is null) ? 0 : m_function.GetHashCode());
            return result;
        }

        public override bool Equals(object obj)
        {
            if (this == obj)
            {
                return true;
            }
            if (!base.Equals(obj))
            {
                return false;
            }
            if (this.GetType() != obj.GetType())
            {
                return false;
            }
            PayloadNearQuery other = (PayloadNearQuery)obj;
            if (m_fieldName is null)
            {
                if (other.m_fieldName != null)
                {
                    return false;
                }
            }
            else if (!m_fieldName.Equals(other.m_fieldName, StringComparison.Ordinal))
            {
                return false;
            }
            if (m_function is null)
            {
                if (other.m_function != null)
                {
                    return false;
                }
            }
            else if (!m_function.Equals(other.m_function))
            {
                return false;
            }
            return true;
        }

        public class PayloadNearSpanWeight : SpanWeight
        {
            private readonly PayloadNearQuery outerInstance;

            public PayloadNearSpanWeight(PayloadNearQuery outerInstance, SpanQuery query, IndexSearcher searcher)
                : base(query, searcher)
            {
                this.outerInstance = outerInstance;
            }

            public override Scorer GetScorer(AtomicReaderContext context, IBits acceptDocs)
            {
                return new PayloadNearSpanScorer(outerInstance, m_query.GetSpans(context, acceptDocs, m_termContexts), this, m_similarity, m_similarity.GetSimScorer(m_stats, context));
            }

            public override Explanation Explain(AtomicReaderContext context, int doc)
            {
                PayloadNearSpanScorer scorer = (PayloadNearSpanScorer)GetScorer(context, (context.AtomicReader).LiveDocs);
                if (scorer != null)
                {
                    int newDoc = scorer.Advance(doc);
                    if (newDoc == doc)
                    {
                        float freq = scorer.Freq;
                        Similarity.SimScorer docScorer = m_similarity.GetSimScorer(m_stats, context);
                        Explanation expl = new Explanation();
                        expl.Description = "weight(" + Query + " in " + doc + ") [" + m_similarity.GetType().Name + "], result of:";
                        Explanation scoreExplanation = docScorer.Explain(doc, new Explanation(freq, "phraseFreq=" + freq));
                        expl.AddDetail(scoreExplanation);
                        expl.Value = scoreExplanation.Value;
                        string field = ((SpanQuery)Query).Field;
                        // now the payloads part
                        Explanation payloadExpl = outerInstance.m_function.Explain(doc, field, scorer.payloadsSeen, scorer.m_payloadScore);
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
            private readonly PayloadNearQuery outerInstance;

            internal Spans spans;
            protected internal float m_payloadScore;
            internal int payloadsSeen;

#pragma warning disable IDE0060 // Remove unused parameter
            protected internal PayloadNearSpanScorer(PayloadNearQuery outerInstance, Spans spans, Weight weight, Similarity similarity, Similarity.SimScorer docScorer)
#pragma warning restore IDE0060 // Remove unused parameter
                : base(spans, weight, docScorer)
            {
                this.outerInstance = outerInstance;
                this.spans = spans;
            }

            // Get the payloads associated with all underlying subspans
            public virtual void GetPayloads(Spans[] subSpans)
            {
                for (var i = 0; i < subSpans.Length; i++)
                {
                    if (subSpans[i] is NearSpansOrdered span)
                    {
                        if (span.IsPayloadAvailable)
                        {
                            ProcessPayloads(span.GetPayload(), subSpans[i].Start, subSpans[i].End);
                        }
                        GetPayloads(span.SubSpans);
                    }
                    else
                    {
                        if (subSpans[i] is NearSpansUnordered unordered)
                        {
                            if (unordered.IsPayloadAvailable)
                            {
                                ProcessPayloads(unordered.GetPayload(), subSpans[i].Start, subSpans[i].End);
                            }
                            GetPayloads(unordered.SubSpans);
                        }
                    }
                }
            }

            // TODO change the whole spans api to use bytesRef, or nuke spans
            internal BytesRef scratch = new BytesRef();

            /// <summary>
            /// By default, uses the <see cref="PayloadFunction"/> to score the payloads, but
            /// can be overridden to do other things.
            /// </summary>
            /// <param name="payLoads"> The payloads </param>
            /// <param name="start"> The start position of the span being scored </param>
            /// <param name="end"> The end position of the span being scored
            /// </param>
            /// <seealso cref="Spans.Spans"/>
            protected virtual void ProcessPayloads(ICollection<byte[]> payLoads, int start, int end)
            {
                foreach (var thePayload in payLoads)
                {
                    scratch.Bytes = thePayload;
                    scratch.Offset = 0;
                    scratch.Length = thePayload.Length;
                    m_payloadScore = outerInstance.m_function.CurrentScore(m_doc, outerInstance.m_fieldName, start, end, payloadsSeen, m_payloadScore, m_docScorer.ComputePayloadFactor(m_doc, spans.Start, spans.End, scratch));
                    ++payloadsSeen;
                }
            }

            //
            protected override bool SetFreqCurrentDoc()
            {
                if (!m_more)
                {
                    return false;
                }
                m_doc = spans.Doc;
                m_freq = 0.0f;
                m_payloadScore = 0;
                payloadsSeen = 0;
                do
                {
                    int matchLength = spans.End - spans.Start;
                    m_freq += m_docScorer.ComputeSlopFactor(matchLength);
                    Spans[] spansArr = new Spans[1];
                    spansArr[0] = spans;
                    GetPayloads(spansArr);
                    m_more = spans.MoveNext();
                } while (m_more && (m_doc == spans.Doc));
                return true;
            }

            public override float GetScore()
            {
                return base.GetScore() * outerInstance.m_function.DocScore(m_doc, outerInstance.m_fieldName, payloadsSeen, m_payloadScore);
            }
        }
    }
}