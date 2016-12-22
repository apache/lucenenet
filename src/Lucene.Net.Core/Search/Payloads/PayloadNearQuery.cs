using System.Collections.Generic;
using System.Text;

namespace Lucene.Net.Search.Payloads
{
    using Lucene.Net.Index;

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
    /// this class is very similar to
    /// <seealso cref="Lucene.Net.Search.Spans.SpanNearQuery"/> except that it factors
    /// in the value of the payloads located at each of the positions where the
    /// <seealso cref="Lucene.Net.Search.Spans.TermSpans"/> occurs.
    /// <p/>
    /// NOTE: In order to take advantage of this with the default scoring implementation
    /// (<seealso cref="DefaultSimilarity"/>), you must override <seealso cref="DefaultSimilarity#scorePayload(int, int, int, BytesRef)"/>,
    /// which returns 1 by default.
    /// <p/>
    /// Payload scores are aggregated using a pluggable <seealso cref="PayloadFunction"/>.
    /// </summary>
    /// <seealso cref= Lucene.Net.Search.Similarities.Similarity.SimScorer#computePayloadFactor(int, int, int, BytesRef) </seealso>
    public class PayloadNearQuery : SpanNearQuery
    {
        protected string fieldName;
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

        public override Weight CreateWeight(IndexSearcher searcher)
        {
            return new PayloadNearSpanWeight(this, this, searcher);
        }

        public override object Clone()
        {
            int sz = clauses.Count;
            SpanQuery[] newClauses = new SpanQuery[sz];

            for (int i = 0; i < sz; i++)
            {
                newClauses[i] = (SpanQuery)clauses[i].Clone();
            }
            PayloadNearQuery boostingNearQuery = new PayloadNearQuery(newClauses, slop, inOrder, function);
            boostingNearQuery.Boost = Boost;
            return boostingNearQuery;
        }

        public override string ToString(string field)
        {
            StringBuilder buffer = new StringBuilder();
            buffer.Append("payloadNear([");
            IEnumerator<SpanQuery> i = clauses.GetEnumerator();
            bool hasCommaSpace = false;
            while (i.MoveNext())
            {
                SpanQuery clause = i.Current;
                buffer.Append(clause.ToString(field));
                buffer.Append(", ");
                hasCommaSpace = true;
            }

            if (hasCommaSpace)
                buffer.Remove(buffer.Length - 2, 2);

            buffer.Append("], ");
            buffer.Append(slop);
            buffer.Append(", ");
            buffer.Append(inOrder);
            buffer.Append(")");
            buffer.Append(ToStringUtils.Boost(Boost));
            return buffer.ToString();
        }

        public override int GetHashCode()
        {
            const int prime = 31;
            int result = base.GetHashCode();
            result = prime * result + ((fieldName == null) ? 0 : fieldName.GetHashCode());
            result = prime * result + ((function == null) ? 0 : function.GetHashCode());
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
            if (fieldName == null)
            {
                if (other.fieldName != null)
                {
                    return false;
                }
            }
            else if (!fieldName.Equals(other.fieldName))
            {
                return false;
            }
            if (function == null)
            {
                if (other.function != null)
                {
                    return false;
                }
            }
            else if (!function.Equals(other.function))
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

            public override Scorer Scorer(AtomicReaderContext context, Bits acceptDocs)
            {
                return new PayloadNearSpanScorer(outerInstance, query.GetSpans(context, acceptDocs, TermContexts), this, Similarity, Similarity.DoSimScorer(Stats, context));
            }

            public override Explanation Explain(AtomicReaderContext context, int doc)
            {
                PayloadNearSpanScorer scorer = (PayloadNearSpanScorer)Scorer(context, (context.AtomicReader).LiveDocs);
                if (scorer != null)
                {
                    int newDoc = scorer.Advance(doc);
                    if (newDoc == doc)
                    {
                        float freq = scorer.Freq;
                        Similarity.SimScorer docScorer = Similarity.DoSimScorer(Stats, context);
                        Explanation expl = new Explanation();
                        expl.Description = "weight(" + Query + " in " + doc + ") [" + Similarity.GetType().Name + "], result of:";
                        Explanation scoreExplanation = docScorer.Explain(doc, new Explanation(freq, "phraseFreq=" + freq));
                        expl.AddDetail(scoreExplanation);
                        expl.Value = scoreExplanation.Value;
                        string field = ((SpanQuery)Query).Field;
                        // now the payloads part
                        Explanation payloadExpl = outerInstance.function.Explain(doc, field, scorer.payloadsSeen, scorer.payloadScore);
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

            internal new Spans spans;
            protected internal float payloadScore;
            internal int payloadsSeen;

            protected internal PayloadNearSpanScorer(PayloadNearQuery outerInstance, Spans spans, Weight weight, Similarity similarity, Similarity.SimScorer docScorer)
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
                    var span = subSpans[i] as NearSpansOrdered;
                    if (span != null)
                    {
                        if (span.PayloadAvailable)
                        {
                            ProcessPayloads(span.Payload, subSpans[i].Start(), subSpans[i].End());
                        }
                        GetPayloads(span.SubSpans);
                    }
                    else
                    {
                        var unordered = subSpans[i] as NearSpansUnordered;
                        if (unordered != null)
                        {
                            if (unordered.PayloadAvailable)
                            {
                                ProcessPayloads(unordered.Payload, subSpans[i].Start(), subSpans[i].End());
                            }
                            GetPayloads(unordered.SubSpans);
                        }
                    }
                }
            }

            // TODO change the whole spans api to use bytesRef, or nuke spans
            internal BytesRef scratch = new BytesRef();

            /// <summary>
            /// By default, uses the <seealso cref="PayloadFunction"/> to score the payloads, but
            /// can be overridden to do other things.
            /// </summary>
            /// <param name="payLoads"> The payloads </param>
            /// <param name="start"> The start position of the span being scored </param>
            /// <param name="end"> The end position of the span being scored
            /// </param>
            /// <seealso cref= spans </seealso>
            protected virtual void ProcessPayloads(ICollection<byte[]> payLoads, int start, int end)
            {
                foreach (var thePayload in payLoads)
                {
                    scratch.Bytes = thePayload;
                    scratch.Offset = 0;
                    scratch.Length = thePayload.Length;
                    payloadScore = outerInstance.function.CurrentScore(Doc, outerInstance.fieldName, start, end, payloadsSeen, payloadScore, DocScorer.ComputePayloadFactor(Doc, spans.Start(), spans.End(), scratch));
                    ++payloadsSeen;
                }
            }

            //
            protected internal override bool SetFreqCurrentDoc()
            {
                if (!More)
                {
                    return false;
                }
                Doc = spans.Doc();
                Freq_Renamed = 0.0f;
                payloadScore = 0;
                payloadsSeen = 0;
                do
                {
                    int matchLength = spans.End() - spans.Start();
                    Freq_Renamed += DocScorer.ComputeSlopFactor(matchLength);
                    Spans[] spansArr = new Spans[1];
                    spansArr[0] = spans;
                    GetPayloads(spansArr);
                    More = spans.Next();
                } while (More && (Doc == spans.Doc()));
                return true;
            }

            public override float Score()
            {
                return base.Score() * outerInstance.function.DocScore(Doc, outerInstance.fieldName, payloadsSeen, payloadScore);
            }
        }
    }
}