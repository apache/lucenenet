using Lucene.Net.Index;
using Lucene.Net.Support;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Search
{
    public class CommonTermsQuery : Query
    {
        protected readonly List<Term> terms = new List<Term>();
        protected readonly bool disableCoord;
        protected readonly float maxTermFrequency;
        protected readonly Occur lowFreqOccur;
        protected readonly Occur highFreqOccur;
        protected float lowFreqBoost = 1.0f;
        protected float highFreqBoost = 1.0f;
        protected float minNrShouldMatch = 0;

        public CommonTermsQuery(Occur highFreqOccur, Occur lowFreqOccur, float maxTermFrequency)
            : this(highFreqOccur, lowFreqOccur, maxTermFrequency, false)
        {
        }

        public CommonTermsQuery(Occur highFreqOccur, Occur lowFreqOccur, float maxTermFrequency, bool disableCoord)
        {
            if (highFreqOccur == Occur.MUST_NOT)
            {
                throw new ArgumentException(
                    "highFreqOccur should be MUST or SHOULD but was MUST_NOT");
            }
            if (lowFreqOccur == Occur.MUST_NOT)
            {
                throw new ArgumentException(
                    "lowFreqOccur should be MUST or SHOULD but was MUST_NOT");
            }
            this.disableCoord = disableCoord;
            this.highFreqOccur = highFreqOccur;
            this.lowFreqOccur = lowFreqOccur;
            this.maxTermFrequency = maxTermFrequency;
        }

        public void Add(Term term)
        {
            if (term == null)
            {
                throw new ArgumentException("Term must not be null");
            }
            this.terms.Add(term);
        }

        public override Query Rewrite(IndexReader reader)
        {
            if (this.terms.Count == 0)
            {
                return new BooleanQuery();
            }
            else if (this.terms.Count == 1)
            {
                TermQuery tq = new TermQuery(this.terms[0]);
                tq.Boost = Boost;
                return tq;
            }
            IList<AtomicReaderContext> leaves = reader.Leaves;
            int maxDoc = reader.MaxDoc;
            TermContext[] contextArray = new TermContext[terms.Count];
            Term[] queryTerms = this.terms.ToArray();
            CollectTermContext(reader, leaves, contextArray, queryTerms);
            return BuildQuery(maxDoc, contextArray, queryTerms);
        }

        protected int CalcLowFreqMinimumNumberShouldMatch(int numOptional)
        {
            if (minNrShouldMatch >= 1.0f || minNrShouldMatch == 0.0f)
            {
                return (int)minNrShouldMatch;
            }
            return (int)(Math.Round(minNrShouldMatch * numOptional));
        }

        protected Query BuildQuery(int maxDoc, TermContext[] contextArray, Term[] queryTerms)
        {
            BooleanQuery lowFreq = new BooleanQuery(disableCoord);
            BooleanQuery highFreq = new BooleanQuery(disableCoord);
            highFreq.Boost = highFreqBoost;
            lowFreq.Boost = lowFreqBoost;
            BooleanQuery query = new BooleanQuery(true);
            for (int i = 0; i < queryTerms.Length; i++)
            {
                TermContext termContext = contextArray[i];
                if (termContext == null)
                {
                    lowFreq.Add(new TermQuery(queryTerms[i]), lowFreqOccur);
                }
                else
                {
                    if ((maxTermFrequency >= 1f && termContext.DocFreq > maxTermFrequency)
                        || (termContext.DocFreq > (int)Math.Ceiling(maxTermFrequency
                            * (float)maxDoc)))
                    {
                        highFreq
                            .Add(new TermQuery(queryTerms[i], termContext), highFreqOccur);
                    }
                    else
                    {
                        lowFreq.Add(new TermQuery(queryTerms[i], termContext), lowFreqOccur);
                    }
                }

            }
            int numLowFreqClauses = lowFreq.Clauses.Length;
            if (lowFreqOccur == Occur.SHOULD && numLowFreqClauses > 0)
            {
                int minMustMatch = CalcLowFreqMinimumNumberShouldMatch(numLowFreqClauses);
                lowFreq.MinimumNumberShouldMatch = minMustMatch;
            }
            if (lowFreq.Clauses.Length == 0)
            {
                /*
                 * if lowFreq is empty we rewrite the high freq terms in a conjunction to
                 * prevent slow queries.
                 */
                if (highFreqOccur == Occur.MUST)
                {
                    highFreq.Boost = Boost;
                    return highFreq;
                }
                else
                {
                    BooleanQuery highFreqConjunction = new BooleanQuery();
                    foreach (BooleanClause booleanClause in highFreq)
                    {
                        highFreqConjunction.Add(booleanClause.Query, Occur.MUST);
                    }
                    highFreqConjunction.Boost = Boost;
                    return highFreqConjunction;

                }
            }
            else if (highFreq.Clauses.Length == 0)
            {
                // only do low freq terms - we don't have high freq terms
                lowFreq.Boost = Boost;
                return lowFreq;
            }
            else
            {
                query.Add(highFreq, Occur.SHOULD);
                query.Add(lowFreq, Occur.MUST);
                query.Boost = Boost;
                return query;
            }
        }

        public void CollectTermContext(IndexReader reader,
      IList<AtomicReaderContext> leaves, TermContext[] contextArray,
      Term[] queryTerms)
        {
            TermsEnum termsEnum = null;
            foreach (AtomicReaderContext context in leaves)
            {
                Fields fields = context.AtomicReader.Fields;
                if (fields == null)
                {
                    // reader has no fields
                    continue;
                }
                for (int i = 0; i < queryTerms.Length; i++)
                {
                    Term term = queryTerms[i];
                    TermContext termContext = contextArray[i];
                    Terms terms = fields.Terms(term.Field);
                    if (terms == null)
                    {
                        // field does not exist
                        continue;
                    }
                    termsEnum = terms.Iterator(termsEnum);
                    //assert termsEnum != null;

                    if (termsEnum == TermsEnum.EMPTY) continue;
                    if (termsEnum.SeekExact(term.Bytes, false))
                    {
                        if (termContext == null)
                        {
                            contextArray[i] = new TermContext(reader.Context,
                                termsEnum.TermState, context.ord, termsEnum.DocFreq,
                                termsEnum.TotalTermFreq);
                        }
                        else
                        {
                            termContext.Register(termsEnum.TermState, context.ord,
                                termsEnum.DocFreq, termsEnum.TotalTermFreq);
                        }
                    }
                }
            }
        }

        public bool IsCoordDisabled
        {
            get
            {
                return disableCoord;
            }
        }

        public float MinimumNumberShouldMatch
        {
            get { return minNrShouldMatch; }
            set { minNrShouldMatch = value; }
        }

        public override void ExtractTerms(ISet<Term> terms)
        {
            terms.UnionWith(this.terms);
        }

        public override string ToString(string field)
        {
            StringBuilder buffer = new StringBuilder();
            bool needParens = (Boost != 1.0)
                || (MinimumNumberShouldMatch > 0);
            if (needParens)
            {
                buffer.Append("(");
            }
            for (int i = 0; i < terms.Count; i++)
            {
                Term t = terms[i];
                buffer.Append(new TermQuery(t).ToString());

                if (i != terms.Count - 1) buffer.Append(", ");
            }
            if (needParens)
            {
                buffer.Append(")");
            }
            if (MinimumNumberShouldMatch > 0)
            {
                buffer.Append('~');
                buffer.Append(MinimumNumberShouldMatch);
            }
            if (Boost != 1.0f)
            {
                buffer.Append(ToStringUtils.Boost(Boost));
            }
            return buffer.ToString();
        }

        public override int GetHashCode()
        {
            const int prime = 31;
            int result = base.GetHashCode();
            result = prime * result + (disableCoord ? 1231 : 1237);
            result = prime * result + Number.FloatToIntBits(highFreqBoost);
            result = prime * result
                + ((highFreqOccur == null) ? 0 : highFreqOccur.GetHashCode());
            result = prime * result + Number.FloatToIntBits(lowFreqBoost);
            result = prime * result
                + ((lowFreqOccur == null) ? 0 : lowFreqOccur.GetHashCode());
            result = prime * result + Number.FloatToIntBits(maxTermFrequency);
            result = prime * result + Number.FloatToIntBits(minNrShouldMatch);
            result = prime * result + ((terms == null) ? 0 : terms.GetHashCode());
            return result;
        }

        public override bool Equals(object obj)
        {
            if (this == obj) return true;
            if (!base.Equals(obj)) return false;
            if (GetType() != obj.GetType()) return false;
            CommonTermsQuery other = (CommonTermsQuery)obj;
            if (disableCoord != other.disableCoord) return false;
            if (Number.FloatToIntBits(highFreqBoost) != Number
                .FloatToIntBits(other.highFreqBoost)) return false;
            if (highFreqOccur != other.highFreqOccur) return false;
            if (Number.FloatToIntBits(lowFreqBoost) != Number
                .FloatToIntBits(other.lowFreqBoost)) return false;
            if (lowFreqOccur != other.lowFreqOccur) return false;
            if (Number.FloatToIntBits(maxTermFrequency) != Number
                .FloatToIntBits(other.maxTermFrequency)) return false;
            if (minNrShouldMatch != other.minNrShouldMatch) return false;
            if (terms == null)
            {
                if (other.terms != null) return false;
            }
            else if (!terms.Equals(other.terms)) return false;
            return true;
        }
    }
}