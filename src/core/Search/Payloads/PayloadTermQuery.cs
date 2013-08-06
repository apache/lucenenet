using System;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Search.Payloads;
using Lucene.Net.Search.Similarities;
using Lucene.Net.Search.Spans;
using Lucene.Net.Util;

namespace Lucene.Net.Search.Payloads
{
    public class PayloadTermQuery : SpanTermQuery
    {
        protected PayloadFunction function;
        private bool includeSpanScore;

        public PayloadTermQuery(Term term, PayloadFunction function) : this(term, function, true) { }

        public PayloadTermQuery(Term term, PayloadFunction function,
          bool includeSpanScore)
            : base(term)
        {
            this.function = function;
            this.includeSpanScore = includeSpanScore;
        }

        public override Weight CreateWeight(IndexSearcher searcher)
        {
            return new PayloadTermWeight(this, searcher);
        }

        protected class PayloadTermWeight : SpanWeight
        {
            protected readonly PayloadTermQuery parent;
            
            public PayloadTermWeight(PayloadTermQuery query, IndexSearcher searcher)
                : base(query, searcher)
            {
                this.parent = query;
            }

            public override Scorer Scorer(AtomicReaderContext context, bool scoreDocsInOrder,
                bool topScorer, IBits acceptDocs)
            {
                return new PayloadTermSpanScorer(this, (TermSpans)query.GetSpans(context, acceptDocs, termContexts),
                    this, similarity.GetSloppySimScorer(stats, context));
            }

            protected class PayloadTermSpanScorer : SpanScorer
            {
                private readonly PayloadTermWeight parent;

                protected BytesRef payload;
                protected internal float payloadScore;
                protected internal int payloadsSeen;
                private readonly TermSpans termSpans;

                public PayloadTermSpanScorer(PayloadTermWeight parent, TermSpans spans, Weight weight, Similarity.SloppySimScorer docScorer)
                    : base(spans, weight, docScorer)
                {
                    this.parent = parent;
                    termSpans = spans;
                }

                protected override bool SetFreqCurrentDoc()
                {
                    if (!more)
                    {
                        return false;
                    }
                    doc = spans.Doc;
                    freq = 0.0f;
                    numMatches = 0;
                    payloadScore = 0;
                    payloadsSeen = 0;
                    while (more && doc == spans.Doc)
                    {
                        int matchLength = spans.End - spans.Start;

                        freq += docScorer.ComputeSlopFactor(matchLength);
                        numMatches++;
                        ProcessPayload(parent.similarity);

                        more = spans.Next();// this moves positions to the next match in this
                        // document
                    }
                    return more || (freq != 0);
                }

                protected void ProcessPayload(Similarity similarity)
                {
                    if (termSpans.IsPayloadAvailable())
                    {
                        DocsAndPositionsEnum postings = termSpans.Postings;
                        payload = postings.Payload;
                        if (payload != null)
                        {
                            payloadScore = parent.parent.function.CurrentScore(doc, parent.parent.term.Field,
                                                                 spans.Start, spans.End, payloadsSeen, payloadScore,
                                                                 docScorer.ComputePayloadFactor(doc, spans.Start, spans.End, payload));
                        }
                        else
                        {
                            payloadScore = parent.parent.function.CurrentScore(doc, parent.parent.term.Field,
                                                                 spans.Start, spans.End, payloadsSeen, payloadScore, 1F);
                        }
                        payloadsSeen++;

                    }
                    else
                    {
                        // zero out the payload?
                    }
                }

                /**
                 * 
                 * @return {@link #getSpanScore()} * {@link #getPayloadScore()}
                 * @throws IOException if there is a low-level I/O error
                 */
                public override float Score()
                {

                    return parent.parent.includeSpanScore ? GetSpanScore() * GetPayloadScore()
                        : GetPayloadScore();
                }

                /**
                 * Returns the SpanScorer score only.
                 * <p/>
                 * Should not be overridden without good cause!
                 * 
                 * @return the score for just the Span part w/o the payload
                 * @throws IOException if there is a low-level I/O error
                 * 
                 * @see #score()
                 */
                protected float GetSpanScore()
                {
                    return base.Score();
                }

                /**
                 * The score for the payload
                 * 
                 * @return The score, as calculated by
                 *         {@link PayloadFunction#docScore(int, String, int, float)}
                 */
                protected internal float GetPayloadScore()
                {
                    return parent.parent.function.DocScore(doc, parent.parent.term.Field, payloadsSeen, payloadScore);
                }
            }

            public override Explanation Explain(AtomicReaderContext context, int doc)
            {
                PayloadTermSpanScorer scorer = (PayloadTermSpanScorer)Scorer(context, true, false, context.AtomicReader.LiveDocs);
                if (scorer != null)
                {
                    int newDoc = scorer.Advance(doc);
                    if (newDoc == doc)
                    {
                        float freq = scorer.SloppyFreq();
                        Similarity.SloppySimScorer docScorer = similarity.GetSloppySimScorer(stats, context);
                        Explanation expl = new Explanation();
                        expl.Description = "weight(" + Query + " in " + doc + ") [" + similarity.GetType().Name + "], result of:";
                        Explanation scoreExplanation = docScorer.Explain(doc, new Explanation(freq, "phraseFreq=" + freq));
                        expl.AddDetail(scoreExplanation);
                        expl.Value = scoreExplanation.Value;
                        // now the payloads part
                        // QUESTION: Is there a way to avoid this skipTo call? We need to know
                        // whether to load the payload or not
                        // GSI: I suppose we could toString the payload, but I don't think that
                        // would be a good idea
                        string field = ((SpanQuery)Query).Field;
                        Explanation payloadExpl = parent.function.Explain(doc, field, scorer.payloadsSeen, scorer.payloadScore);
                        payloadExpl.Value = scorer.GetPayloadScore();
                        // combined
                        ComplexExplanation result = new ComplexExplanation();
                        if (parent.includeSpanScore)
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
            int prime = 31;
            int result = base.GetHashCode();
            result = prime * result + ((function == null) ? 0 : function.GetHashCode());
            result = prime * result + (includeSpanScore ? 1231 : 1237);
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
            PayloadTermQuery other = (PayloadTermQuery)obj;
            if (function == null)
            {
                if (other.function != null)
                    return false;
            }
            else if (!function.Equals(other.function))
                return false;
            if (includeSpanScore != other.includeSpanScore)
                return false;
            return true;
        }

    }
}