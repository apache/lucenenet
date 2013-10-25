using Lucene.Net.Index;
using Lucene.Net.Support;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Search.Function
{
    public class BoostedQuery : Query
    {
        private Query q;
        private readonly ValueSource boostVal; // optional, can be null

        public BoostedQuery(Query subQuery, ValueSource boostVal)
        {
            this.q = subQuery;
            this.boostVal = boostVal;
        }

        public Query Query { get { return q; } }
        public ValueSource ValueSource { get { return boostVal; } }

        public override Query Rewrite(IndexReader reader)
        {
            Query newQ = q.Rewrite(reader);
            if (newQ == q) return this;
            BoostedQuery bq = (BoostedQuery)this.Clone();
            bq.q = newQ;
            return bq;
        }

        public override void ExtractTerms(ISet<Term> terms)
        {
            q.ExtractTerms(terms);
        }

        public override Weight CreateWeight(IndexSearcher searcher)
        {
            return new BoostedQuery.BoostedWeight(this, searcher);
        }

        private class BoostedWeight : Weight
        {
            readonly IndexSearcher searcher;
            internal Weight qWeight;
            internal IDictionary<object, object> fcontext;

            private readonly BoostedQuery parent;

            public BoostedWeight(BoostedQuery parent, IndexSearcher searcher)
            {
                this.parent = parent;
                this.searcher = searcher;
                this.qWeight = parent.q.CreateWeight(searcher);
                this.fcontext = ValueSource.NewContext(searcher);
                parent.boostVal.CreateWeight(fcontext, searcher);
            }

            public override Query Query
            {
                get { return parent; }
            }

            public override float ValueForNormalization
            {
                get
                {
                    float sum = qWeight.ValueForNormalization;
                    sum *= parent.Boost * parent.Boost;
                    return sum;
                }
            }

            public override void Normalize(float norm, float topLevelBoost)
            {
                topLevelBoost *= parent.Boost;
                qWeight.Normalize(norm, topLevelBoost);
            }

            public override Scorer Scorer(AtomicReaderContext context, bool scoreDocsInOrder, bool topScorer, IBits acceptDocs)
            {
                // we are gonna advance() the subscorer
                Scorer subQueryScorer = qWeight.Scorer(context, true, false, acceptDocs);
                if (subQueryScorer == null)
                {
                    return null;
                }
                return new BoostedQuery.CustomScorer(parent, context, this, parent.Boost, subQueryScorer, parent.boostVal);
            }

            public override Explanation Explain(AtomicReaderContext readerContext, int doc)
            {
                Explanation subQueryExpl = qWeight.Explain(readerContext, doc);
                if (!subQueryExpl.IsMatch)
                {
                    return subQueryExpl;
                }
                FunctionValues vals = parent.boostVal.GetValues(fcontext, readerContext);
                float sc = subQueryExpl.Value * vals.FloatVal(doc);
                Explanation res = new ComplexExplanation(
                  true, sc, parent.ToString() + ", product of:");
                res.AddDetail(subQueryExpl);
                res.AddDetail(vals.Explain(doc));
                return res;
            }
        }

        private class CustomScorer : Scorer
        {
            private readonly BoostedQuery.BoostedWeight weight;
            private readonly float qWeight;
            private readonly Scorer scorer;
            private readonly FunctionValues vals;
            private readonly AtomicReaderContext readerContext;

            private readonly BoostedQuery parent;

            public CustomScorer(BoostedQuery parent, AtomicReaderContext readerContext, BoostedQuery.BoostedWeight w, float qWeight, Scorer scorer, ValueSource vs)
                : base(w)
            {
                this.weight = w;
                this.qWeight = qWeight;
                this.scorer = scorer;
                this.readerContext = readerContext;
                this.vals = vs.GetValues(weight.fcontext, readerContext);
            }

            public override int DocID
            {
                get { return scorer.DocID; }
            }

            public override int Advance(int target)
            {
                return scorer.Advance(target);
            }

            public override int NextDoc()
            {
                return scorer.NextDoc();
            }

            public override float Score()
            {
                float score = qWeight * scorer.Score() * vals.FloatVal(scorer.DocID);

                // Current Lucene priority queues can't handle NaN and -Infinity, so
                // map to -Float.MAX_VALUE. This conditional handles both -infinity
                // and NaN since comparisons with NaN are always false.
                return score > float.NegativeInfinity ? score : -float.MaxValue;
            }

            public override int Freq
            {
                get { return scorer.Freq; }
            }

            public override ICollection<ChildScorer> Children
            {
                get
                {
                    return new List<ChildScorer>() { new ChildScorer(scorer, "CUSTOM") };
                }
            }

            public Explanation Explain(int doc)
            {
                Explanation subQueryExpl = weight.qWeight.Explain(readerContext, doc);
                if (!subQueryExpl.IsMatch)
                {
                    return subQueryExpl;
                }
                float sc = subQueryExpl.Value * vals.FloatVal(doc);
                Explanation res = new ComplexExplanation(
                  true, sc, parent.ToString() + ", product of:");
                res.AddDetail(subQueryExpl);
                res.AddDetail(vals.Explain(doc));
                return res;
            }

            public override long Cost
            {
                get { return scorer.Cost; }
            }
        }

        public override string ToString(string field)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("boost(").Append(q.ToString(field)).Append(',').Append(boostVal).Append(')');
            sb.Append(ToStringUtils.Boost(Boost));
            return sb.ToString();
        }

        public override bool Equals(object o)
        {
            if (!base.Equals(o)) return false;
            BoostedQuery other = (BoostedQuery)o;
            return this.q.Equals(other.q)
                   && this.boostVal.Equals(other.boostVal);
        }

        public override int GetHashCode()
        {
            int h = q.GetHashCode();
            h ^= (h << 17) | Number.URShift(h, 16);
            h += boostVal.GetHashCode();
            h ^= (h << 8) | Number.URShift(h, 25);
            h += Number.FloatToIntBits(Boost);
            return h;
        }
    }
}
