using System;
using System.Collections.Generic;
using System.Text;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Search.Payloads;
using Lucene.Net.Search.Similarities;
using Lucene.Net.Search.Spans;
using Lucene.Net.Util;

namespace Lucene.Net.Search.Payloads
{
    public class PayloadNearQuery : SpanNearQuery
    {
        protected string fieldName;
        protected PayloadFunction function;

        public PayloadNearQuery(SpanQuery[] clauses, int slop, bool inOrder)
            : this(clauses, slop, inOrder, new AveragePayloadFunction())
        {
        }

        public PayloadNearQuery(SpanQuery[] clauses, int slop, bool inOrder,
                                PayloadFunction function)
            : base(clauses, slop, inOrder)
        {
            fieldName = clauses[0].Field; // all clauses must have same field
            this.function = function;
        }

        public override Weight CreateWeight(IndexSearcher searcher)
        {
            return new PayloadNearSpanWeight(this, searcher);
        }

        public override object Clone()
        {
            int sz = clauses.Count;
            var newClauses = new SpanQuery[sz];

            for (int i = 0; i < sz; i++)
            {
                newClauses[i] = (SpanQuery)clauses[i].Clone();
            }
            var boostingNearQuery = new PayloadNearQuery(newClauses, Slop,
                                                         inOrder, function);
            boostingNearQuery.Boost = Boost;
            return boostingNearQuery;
        }

        public override string ToString(string field)
        {
            var buffer = new StringBuilder();
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
            buffer.Append(Slop);
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

        public override bool Equals(Object obj)
        {
            if (this == obj)
                return true;
            if (!base.Equals(obj))
                return false;
            if (GetType() != obj.GetType())
                return false;
            var other = (PayloadNearQuery)obj;
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

        public class PayloadNearSpanScorer : SpanScorer
        {
            private readonly PayloadNearQuery parent;

            private readonly BytesRef scratch = new BytesRef();
            protected internal float payloadScore;
            internal int payloadsSeen;
            private SpansBase spans;

            public PayloadNearSpanScorer(PayloadNearQuery parent, SpansBase spans, Weight weight,
                                            Similarity similarity, Similarity.SloppySimScorer docScorer)
                : base(spans, weight, docScorer)
            {
                this.parent = parent;
                this.spans = spans;
            }

            // Get the payloads associated with all underlying subspans
            public void GetPayloads(SpansBase[] subSpans)
            {
                for (int i = 0; i < subSpans.Length; i++)
                {
                    if (subSpans[i] is NearSpansOrdered)
                    {
                        if ((subSpans[i]).IsPayloadAvailable())
                        {
                            ProcessPayloads((subSpans[i]).GetPayload(),
                                            subSpans[i].Start, subSpans[i].End);
                        }
                        GetPayloads(((NearSpansOrdered)subSpans[i]).GetSubSpans());
                    }
                    else if (subSpans[i] is NearSpansUnordered)
                    {
                        if ((subSpans[i]).IsPayloadAvailable())
                        {
                            ProcessPayloads((subSpans[i]).GetPayload(),
                                            subSpans[i].Start, subSpans[i].End);
                        }
                        GetPayloads(((NearSpansUnordered)subSpans[i]).GetSubSpans());
                    }
                }
            }

            // TODO change the whole spans api to use bytesRef, or nuke spans

            protected void ProcessPayloads(ICollection<sbyte[]> payLoads, int start, int end)
            {
                foreach (var thePayload in payLoads)
                {
                    scratch.bytes = thePayload;
                    scratch.offset = 0;
                    scratch.length = thePayload.Length;
                    payloadScore = parent.function.CurrentScore(doc, parent.fieldName, start, end,
                                                         payloadsSeen, payloadScore, docScorer.ComputePayloadFactor(doc, spans.Start, spans.End, scratch));
                    ++payloadsSeen;
                }
            }

            protected override bool SetFreqCurrentDoc()
            {
                if (!more)
                {
                    return false;
                }
                doc = spans.Doc;
                freq = 0.0f;
                payloadScore = 0;
                payloadsSeen = 0;
                do
                {
                    int matchLength = spans.End - spans.Start;
                    freq += docScorer.ComputeSlopFactor(matchLength);
                    var spansArr = new SpansBase[1];
                    spansArr[0] = spans;
                    GetPayloads(spansArr);
                    more = spans.Next();
                } while (more && (doc == spans.Doc));
                return true;
            }

            public float Score()
            {
                return base.Score()
                       * parent.function.DocScore(doc, parent.fieldName, payloadsSeen, payloadScore);
            }
        }

        public class PayloadNearSpanWeight : SpanWeight
        {
            private readonly PayloadNearQuery parent;

            public PayloadNearSpanWeight(PayloadNearQuery query, IndexSearcher searcher)
                : base(query, searcher)
            {
                this.parent = query;
            }
            
            public override Scorer Scorer(AtomicReaderContext context, bool scoreDocsInOrder,
                                          bool topScorer, IBits acceptDocs)
            {
                return new PayloadNearSpanScorer(parent, query.GetSpans(context, acceptDocs, termContexts), this,
                                                 similarity, similarity.GetSloppySimScorer(stats, context));
            }

            public override Explanation Explain(AtomicReaderContext context, int doc)
            {
                var scorer = (PayloadNearSpanScorer)Scorer(context, true, false, ((AtomicReader)context.Reader).LiveDocs);
                if (scorer != null)
                {
                    int newDoc = scorer.Advance(doc);
                    if (newDoc == doc)
                    {
                        float freq = scorer.Freq;
                        Similarity.SloppySimScorer docScorer = similarity.GetSloppySimScorer(stats, context);
                        var expl = new Explanation();
                        expl.Description = "weight(" + Query + " in " + doc + ") [" + similarity.GetType().Name +
                                           "], result of:";
                        Explanation scoreExplanation = docScorer.Explain(doc, new Explanation(freq, "phraseFreq=" + freq));
                        expl.AddDetail(scoreExplanation);
                        expl.Value = scoreExplanation.Value;
                        String field = ((SpanQuery)Query).Field;
                        // now the payloads part
                        Explanation payloadExpl = parent.function.Explain(doc, field, scorer.payloadsSeen, scorer.payloadScore);
                        // combined
                        var result = new ComplexExplanation();
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
    }
}