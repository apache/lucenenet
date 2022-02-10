using System.IO;

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
    using DocsAndPositionsEnum = Lucene.Net.Index.DocsAndPositionsEnum;
    using Similarity = Lucene.Net.Search.Similarities.Similarity;
    using SpanQuery = Lucene.Net.Search.Spans.SpanQuery;
    using SpanScorer = Lucene.Net.Search.Spans.SpanScorer;
    using SpanTermQuery = Lucene.Net.Search.Spans.SpanTermQuery;
    using SpanWeight = Lucene.Net.Search.Spans.SpanWeight;
    using Term = Lucene.Net.Index.Term;
    using TermSpans = Lucene.Net.Search.Spans.TermSpans;

    /// <summary>
    /// This class is very similar to
    /// <see cref="Lucene.Net.Search.Spans.SpanTermQuery"/> except that it factors
    /// in the value of the payload located at each of the positions where the
    /// <see cref="Lucene.Net.Index.Term"/> occurs.
    /// <para/>
    /// NOTE: In order to take advantage of this with the default scoring implementation
    /// (<see cref="Similarities.DefaultSimilarity"/>), you must override <see cref="Similarities.DefaultSimilarity.ScorePayload(int, int, int, BytesRef)"/>,
    /// which returns 1 by default.
    /// <para/>
    /// Payload scores are aggregated using a pluggable <see cref="PayloadFunction"/>. </summary>
    /// <seealso cref="Lucene.Net.Search.Similarities.Similarity.SimScorer.ComputePayloadFactor(int, int, int, BytesRef)"/>
    public class PayloadTermQuery : SpanTermQuery
    {
        protected PayloadFunction m_function;
        private readonly bool includeSpanScore; // LUCENENET: marked readonly

        public PayloadTermQuery(Term term, PayloadFunction function)
            : this(term, function, true)
        {
        }

        public PayloadTermQuery(Term term, PayloadFunction function, bool includeSpanScore)
            : base(term)
        {
            this.m_function = function;
            this.includeSpanScore = includeSpanScore;
        }

        public override Weight CreateWeight(IndexSearcher searcher)
        {
            return new PayloadTermWeight(this, this, searcher);
        }

        protected class PayloadTermWeight : SpanWeight
        {
            private readonly PayloadTermQuery outerInstance;

            public PayloadTermWeight(PayloadTermQuery outerInstance, PayloadTermQuery query, IndexSearcher searcher)
                : base(query, searcher)
            {
                this.outerInstance = outerInstance;
            }

            public override Scorer GetScorer(AtomicReaderContext context, IBits acceptDocs)
            {
                return new PayloadTermSpanScorer(this, (TermSpans)m_query.GetSpans(context, acceptDocs, m_termContexts), this, m_similarity.GetSimScorer(m_stats, context));
            }

            protected class PayloadTermSpanScorer : SpanScorer
            {
                private readonly PayloadTermQuery.PayloadTermWeight outerInstance;

                protected BytesRef m_payload;
                protected internal float m_payloadScore;
                protected internal int m_payloadsSeen;
                internal readonly TermSpans termSpans;

                public PayloadTermSpanScorer(PayloadTermQuery.PayloadTermWeight outerInstance, TermSpans spans, Weight weight, Similarity.SimScorer docScorer)
                    : base(spans, weight, docScorer)
                {
                    this.outerInstance = outerInstance;
                    termSpans = spans;
                }

                protected override bool SetFreqCurrentDoc()
                {
                    if (!m_more)
                    {
                        return false;
                    }
                    m_doc = m_spans.Doc;
                    m_freq = 0.0f;
                    m_numMatches = 0;
                    m_payloadScore = 0;
                    m_payloadsSeen = 0;
                    while (m_more && m_doc == m_spans.Doc)
                    {
                        int matchLength = m_spans.End - m_spans.Start;

                        m_freq += m_docScorer.ComputeSlopFactor(matchLength);
                        m_numMatches++;
                        ProcessPayload(outerInstance.m_similarity);

                        m_more = m_spans.MoveNext(); // this moves positions to the next match in this
                        // document
                    }
                    return m_more || (m_freq != 0);
                }

                protected internal virtual void ProcessPayload(Similarity similarity)
                {
                    if (termSpans.IsPayloadAvailable)
                    {
                        DocsAndPositionsEnum postings = termSpans.Postings;
                        m_payload = postings.GetPayload();
                        if (m_payload != null)
                        {
                            m_payloadScore = outerInstance.outerInstance.m_function.CurrentScore(m_doc, outerInstance.outerInstance.Term.Field, m_spans.Start, m_spans.End, m_payloadsSeen, m_payloadScore, m_docScorer.ComputePayloadFactor(m_doc, m_spans.Start, m_spans.End, m_payload));
                        }
                        else
                        {
                            m_payloadScore = outerInstance.outerInstance.m_function.CurrentScore(m_doc, outerInstance.outerInstance.Term.Field, m_spans.Start, m_spans.End, m_payloadsSeen, m_payloadScore, 1F);
                        }
                        m_payloadsSeen++;
                    }
                    else
                    {
                        // zero out the payload?
                    }
                }

                ///
                /// <returns> <see cref="GetSpanScore()"/> * <see cref="GetPayloadScore()"/> </returns>
                /// <exception cref="IOException"> if there is a low-level I/O error </exception>
                public override float GetScore()
                {
                    return outerInstance.outerInstance.includeSpanScore ? GetSpanScore() * GetPayloadScore() : GetPayloadScore();
                }

                /// <summary>
                /// Returns the <see cref="SpanScorer"/> score only.
                /// <para/>
                /// Should not be overridden without good cause!
                /// </summary>
                /// <returns> the score for just the Span part w/o the payload </returns>
                /// <exception cref="IOException"> if there is a low-level I/O error
                /// </exception>
                /// <seealso cref="GetScore()"/>
                protected internal virtual float GetSpanScore()
                {
                    return base.GetScore();
                }

                /// <summary>
                /// The score for the payload
                /// </summary>
                /// <returns> The score, as calculated by
                ///         <see cref="PayloadFunction.DocScore(int, string, int, float)"/> </returns>
                protected internal virtual float GetPayloadScore()
                {
                    return outerInstance.outerInstance.m_function.DocScore(m_doc, outerInstance.outerInstance.Term.Field, m_payloadsSeen, m_payloadScore);
                }
            }

            public override Explanation Explain(AtomicReaderContext context, int doc)
            {
                PayloadTermSpanScorer scorer = (PayloadTermSpanScorer)GetScorer(context, (context.AtomicReader).LiveDocs);
                if (scorer != null)
                {
                    int newDoc = scorer.Advance(doc);
                    if (newDoc == doc)
                    {
                        float freq = scorer.SloppyFreq;
                        Similarity.SimScorer docScorer = m_similarity.GetSimScorer(m_stats, context);
                        Explanation expl = new Explanation();
                        expl.Description = "weight(" + Query + " in " + doc + ") [" + m_similarity.GetType().Name + "], result of:";
                        Explanation scoreExplanation = docScorer.Explain(doc, new Explanation(freq, "phraseFreq=" + freq));
                        expl.AddDetail(scoreExplanation);
                        expl.Value = scoreExplanation.Value;
                        // now the payloads part
                        // QUESTION: Is there a way to avoid this skipTo call? We need to know
                        // whether to load the payload or not
                        // GSI: I suppose we could toString the payload, but I don't think that
                        // would be a good idea
                        string field = ((SpanQuery)Query).Field;
                        Explanation payloadExpl = outerInstance.m_function.Explain(doc, field, scorer.m_payloadsSeen, scorer.m_payloadScore);
                        payloadExpl.Value = scorer.GetPayloadScore();
                        // combined
                        ComplexExplanation result = new ComplexExplanation();
                        if (outerInstance.includeSpanScore)
                        {
                            result.AddDetail(expl);
                            result.AddDetail(payloadExpl);
                            result.Value = expl.Value * payloadExpl.Value;
                            result.Description = "btq, product of:";
                        }
                        else
                        {
                            result.AddDetail(payloadExpl);
                            result.Value = payloadExpl.Value;
                            result.Description = "btq(includeSpanScore=false), result of:";
                        }
                        result.Match = true; // LUCENE-1303
                        return result;
                    }
                }

                return new ComplexExplanation(false, 0.0f, "no matching term");
            }
        }

        public override int GetHashCode()
        {
            const int prime = 31;
            int result = base.GetHashCode();
            result = prime * result + ((m_function is null) ? 0 : m_function.GetHashCode());
            result = prime * result + (includeSpanScore ? 1231 : 1237);
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
            PayloadTermQuery other = (PayloadTermQuery)obj;
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
            if (includeSpanScore != other.includeSpanScore)
            {
                return false;
            }
            return true;
        }
    }
}