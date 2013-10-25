using Lucene.Net.Index;
using Lucene.Net.Support;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Search.Function
{
    public class FunctionQuery : Query
    {
        readonly ValueSource func;

        public FunctionQuery(ValueSource func)
        {
            this.func = func;
        }

        public ValueSource ValueSource
        {
            get
            {
                return func;
            }
        }

        public override Query Rewrite(IndexReader reader)
        {
            return this;
        }

        public override void ExtractTerms(ISet<Term> terms)
        {
        }

        protected class FunctionWeight : Weight
        {
            protected readonly IndexSearcher searcher;
            protected internal float queryNorm;
            protected float queryWeight;
            protected internal readonly IDictionary<object, object> context;

            private readonly FunctionQuery parent;

            public FunctionWeight(FunctionQuery parent, IndexSearcher searcher)
            {
                this.parent = parent;
                this.searcher = searcher;
                this.context = ValueSource.NewContext(searcher);
                parent.func.CreateWeight(context, searcher);
            }

            public override Query Query
            {
                get { return parent; }
            }

            public override float ValueForNormalization
            {
                get
                {
                    queryWeight = parent.Boost;
                    return queryWeight * queryWeight;
                }
            }

            public override void Normalize(float norm, float topLevelBoost)
            {
                this.queryNorm = norm * topLevelBoost;
                queryWeight *= this.queryNorm;
            }

            public override Scorer Scorer(AtomicReaderContext context, bool scoreDocsInOrder, bool topScorer, IBits acceptDocs)
            {
                return new AllScorer(parent, context, acceptDocs, this, queryWeight);
            }

            public override Explanation Explain(AtomicReaderContext context, int doc)
            {
                return ((AllScorer)Scorer(context, true, true, context.AtomicReader.LiveDocs)).Explain(doc);
            }
        }

        protected class AllScorer : Scorer
        {
            readonly IndexReader reader;
            readonly FunctionWeight weight;
            readonly int maxDoc;
            readonly float qWeight;
            int doc = -1;
            readonly FunctionValues vals;
            readonly IBits acceptDocs;

            private readonly FunctionQuery parent;

            public AllScorer(FunctionQuery parent, AtomicReaderContext context, IBits acceptDocs, FunctionWeight w, float qWeight)
                : base(w)
            {
                this.weight = w;
                this.qWeight = qWeight;
                this.reader = context.Reader;
                this.maxDoc = reader.MaxDoc;
                this.acceptDocs = acceptDocs;
                vals = parent.func.GetValues(weight.context, context);
            }

            public override int DocID
            {
                get { return doc; }
            }

            public override int NextDoc()
            {
                for (; ; )
                {
                    ++doc;
                    if (doc >= maxDoc)
                    {
                        return doc = NO_MORE_DOCS;
                    }
                    if (acceptDocs != null && !acceptDocs[doc]) continue;
                    return doc;
                }
            }

            public override int Advance(int target)
            {
                // this will work even if target==NO_MORE_DOCS
                doc = target - 1;
                return NextDoc();
            }

            public override float Score()
            {
                float score = qWeight * vals.FloatVal(doc);

                // Current Lucene priority queues can't handle NaN and -Infinity, so
                // map to -Float.MAX_VALUE. This conditional handles both -infinity
                // and NaN since comparisons with NaN are always false.
                return score > float.NegativeInfinity ? score : -float.MaxValue;
            }

            public override long Cost
            {
                get { return maxDoc; }
            }

            public override int Freq
            {
                get { return 1; }
            }

            public Explanation Explain(int doc)
            {
                float sc = qWeight * vals.FloatVal(doc);

                Explanation result = new ComplexExplanation
                  (true, sc, "FunctionQuery(" + parent.func + "), product of:");

                result.AddDetail(vals.Explain(doc));
                result.AddDetail(new Explanation(parent.Boost, "boost"));
                result.AddDetail(new Explanation(weight.queryNorm, "queryNorm"));
                return result;
            }
        }

        public override Weight CreateWeight(IndexSearcher searcher)
        {
            return new FunctionQuery.FunctionWeight(this, searcher);
        }

        public override string ToString(string field)
        {
            float boost = Boost;
            return (boost != 1.0 ? "(" : "") + func.ToString()
                    + (boost == 1.0 ? "" : ")^" + boost);
        }

        public override bool Equals(object o)
        {
            if (o.GetType() != typeof(FunctionQuery)) return false;
            FunctionQuery other = (FunctionQuery)o;
            return this.Boost == other.Boost
                    && this.func.Equals(other.func);
        }

        public override int GetHashCode()
        {
            return func.GetHashCode() * 31 + Number.FloatToIntBits(Boost);
        }
    }
}
