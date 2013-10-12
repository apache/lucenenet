using Lucene.Net.Index;
using Lucene.Net.Support;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Search
{
    public class CustomScoreQuery : Query
    {
        private Query subQuery;
        private Query[] scoringQueries; // never null (empty array if there are no valSrcQueries).
        private bool strict = false; // if true, valueSource part of query does not take part in weights normalization.

        public CustomScoreQuery(Query subQuery)
            : this(subQuery, new Query[0])
        {
        }

        public CustomScoreQuery(Query subQuery, Query scoringQuery)
            : this(subQuery, scoringQuery != null ? // don't want an array that contains a single null..
                new Query[] { scoringQuery } : new Query[0])
        {
        }

        public CustomScoreQuery(Query subQuery, params Query[] scoringQueries)
        {
            this.subQuery = subQuery;
            this.scoringQueries = scoringQueries != null ?
                scoringQueries : new Query[0];
            if (subQuery == null) throw new ArgumentException("<subquery> must not be null!");
        }

        public override Query Rewrite(IndexReader reader)
        {
            CustomScoreQuery clone = null;

            Query sq = subQuery.Rewrite(reader);
            if (sq != subQuery)
            {
                clone = (CustomScoreQuery)Clone();
                clone.subQuery = sq;
            }

            for (int i = 0; i < scoringQueries.Length; i++)
            {
                Query v = scoringQueries[i].Rewrite(reader);
                if (v != scoringQueries[i])
                {
                    if (clone == null) clone = (CustomScoreQuery)Clone();
                    clone.scoringQueries[i] = v;
                }
            }

            return (clone == null) ? this : clone;
        }

        public override void ExtractTerms(ISet<Term> terms)
        {
            subQuery.ExtractTerms(terms);
            foreach (Query scoringQuery in scoringQueries)
            {
                scoringQuery.ExtractTerms(terms);
            }
        }

        public override object Clone()
        {
            CustomScoreQuery clone = (CustomScoreQuery)base.Clone();
            clone.subQuery = (Query)subQuery.Clone();
            clone.scoringQueries = new Query[scoringQueries.Length];
            for (int i = 0; i < scoringQueries.Length; i++)
            {
                clone.scoringQueries[i] = (Query)scoringQueries[i].Clone();
            }
            return clone;
        }

        public override string ToString(string field)
        {
            StringBuilder sb = new StringBuilder(Name).Append("(");
            sb.Append(subQuery.ToString(field));
            foreach (Query scoringQuery in scoringQueries)
            {
                sb.Append(", ").Append(scoringQuery.ToString(field));
            }
            sb.Append(")");
            sb.Append(strict ? " STRICT" : "");
            return sb.ToString() + ToStringUtils.Boost(Boost);
        }

        public override bool Equals(object o)
        {
            if (this == o)
                return true;
            if (!base.Equals(o))
                return false;
            if (GetType() != o.GetType())
            {
                return false;
            }
            CustomScoreQuery other = (CustomScoreQuery)o;
            if (this.Boost != other.Boost ||
                !this.subQuery.Equals(other.subQuery) ||
                this.strict != other.strict ||
                this.scoringQueries.Length != other.scoringQueries.Length)
            {
                return false;
            }
            return Arrays.Equals(scoringQueries, other.scoringQueries);
        }

        public override int GetHashCode()
        {
            return (GetType().GetHashCode() + subQuery.GetHashCode() + Arrays.HashCode(scoringQueries))
                ^ Number.FloatToIntBits(Boost) ^ (strict ? 1234 : 4321);
        }

        protected virtual CustomScoreProvider GetCustomScoreProvider(AtomicReaderContext context)
        {
            return new CustomScoreProvider(context);
        }

        private class CustomWeight : Weight
        {
            Weight subQueryWeight;
            Weight[] valSrcWeights;
            bool qStrict;
            float queryWeight;

            private readonly CustomScoreQuery parent;

            public CustomWeight(CustomScoreQuery parent, IndexSearcher searcher)
            {
                this.subQueryWeight = parent.subQuery.CreateWeight(searcher);
                this.valSrcWeights = new Weight[parent.scoringQueries.Length];
                for (int i = 0; i < parent.scoringQueries.Length; i++)
                {
                    this.valSrcWeights[i] = parent.scoringQueries[i].CreateWeight(searcher);
                }
                this.qStrict = parent.strict;
            }

            public override Query Query
            {
                get { return parent; }
            }

            public override float ValueForNormalization
            {
                get
                {
                    float sum = subQueryWeight.ValueForNormalization;
                    foreach (Weight valSrcWeight in valSrcWeights)
                    {
                        if (qStrict)
                        {
                            var ignored = valSrcWeight.ValueForNormalization; // do not include ValueSource part in the query normalization
                        }
                        else
                        {
                            sum += valSrcWeight.ValueForNormalization;
                        }
                    }
                    return sum;
                }
            }

            public override void Normalize(float norm, float topLevelBoost)
            {
                // note we DONT incorporate our boost, nor pass down any topLevelBoost 
                // (e.g. from outer BQ), as there is no guarantee that the CustomScoreProvider's 
                // function obeys the distributive law... it might call sqrt() on the subQuery score
                // or some other arbitrary function other than multiplication.
                // so, instead boosts are applied directly in score()
                subQueryWeight.Normalize(norm, 1f);
                foreach (Weight valSrcWeight in valSrcWeights)
                {
                    if (qStrict)
                    {
                        valSrcWeight.Normalize(1, 1); // do not normalize the ValueSource part
                    }
                    else
                    {
                        valSrcWeight.Normalize(norm, 1f);
                    }
                }
                queryWeight = topLevelBoost * parent.Boost;
            }

            public override Scorer Scorer(AtomicReaderContext context, bool scoreDocsInOrder, bool topScorer, IBits acceptDocs)
            {
                // Pass true for "scoresDocsInOrder", because we
                // require in-order scoring, even if caller does not,
                // since we call advance on the valSrcScorers.  Pass
                // false for "topScorer" because we will not invoke
                // score(Collector) on these scorers:
                Scorer subQueryScorer = subQueryWeight.Scorer(context, true, false, acceptDocs);
                if (subQueryScorer == null)
                {
                    return null;
                }
                Scorer[] valSrcScorers = new Scorer[valSrcWeights.Length];
                for (int i = 0; i < valSrcScorers.Length; i++)
                {
                    valSrcScorers[i] = valSrcWeights[i].Scorer(context, true, topScorer, acceptDocs);
                }
                return new CustomScorer(parent.GetCustomScoreProvider(context), this, queryWeight, subQueryScorer, valSrcScorers);
            }

            public override Explanation Explain(AtomicReaderContext context, int doc)
            {
                Explanation explain = DoExplain(context, doc);
                return explain == null ? new Explanation(0.0f, "no matching docs") : explain;
            }

            private Explanation DoExplain(AtomicReaderContext info, int doc)
            {
                Explanation subQueryExpl = subQueryWeight.Explain(info, doc);
                if (!subQueryExpl.IsMatch)
                {
                    return subQueryExpl;
                }
                // match
                Explanation[] valSrcExpls = new Explanation[valSrcWeights.Length];
                for (int i = 0; i < valSrcWeights.Length; i++)
                {
                    valSrcExpls[i] = valSrcWeights[i].Explain(info, doc);
                }
                Explanation customExp = parent.GetCustomScoreProvider(info).CustomExplain(doc, subQueryExpl, valSrcExpls);
                float sc = parent.Boost * customExp.Value;
                Explanation res = new ComplexExplanation(
                  true, sc, parent.ToString() + ", product of:");
                res.AddDetail(customExp);
                res.AddDetail(new Explanation(parent.Boost, "queryBoost")); // actually using the q boost as q weight (== weight value)
                return res;
            }

            public override bool ScoresDocsOutOfOrder
            {
                get
                {
                    return false;
                }
            }
        }

        private class CustomScorer : Scorer
        {
            private readonly float qWeight;
            private readonly Scorer subQueryScorer;
            private readonly Scorer[] valSrcScorers;
            private readonly CustomScoreProvider provider;
            private readonly float[] vScores; // reused in score() to avoid allocating this array for each doc

            // constructor
            protected internal CustomScorer(CustomScoreProvider provider, CustomWeight w, float qWeight,
                Scorer subQueryScorer, Scorer[] valSrcScorers)
                : base(w)
            {
                this.qWeight = qWeight;
                this.subQueryScorer = subQueryScorer;
                this.valSrcScorers = valSrcScorers;
                this.vScores = new float[valSrcScorers.Length];
                this.provider = provider;
            }

            public override int NextDoc()
            {
                int doc = subQueryScorer.NextDoc();
                if (doc != NO_MORE_DOCS)
                {
                    foreach (Scorer valSrcScorer in valSrcScorers)
                    {
                        valSrcScorer.Advance(doc);
                    }
                }
                return doc;
            }

            public override int DocID
            {
                get { return subQueryScorer.DocID; }
            }

            public override float Score()
            {
                for (int i = 0; i < valSrcScorers.Length; i++)
                {
                    vScores[i] = valSrcScorers[i].Score();
                }
                return qWeight * provider.CustomScore(subQueryScorer.DocID, subQueryScorer.Score(), vScores);
            }

            public override int Freq
            {
                get { return subQueryScorer.Freq; }
            }

            public override ICollection<ChildScorer> Children
            {
                get
                {
                    return new ChildScorer[] {
                        new ChildScorer(subQueryScorer, "CUSTOM")
                    };
                }
            }

            public override int Advance(int target)
            {
                int doc = subQueryScorer.Advance(target);
                if (doc != NO_MORE_DOCS)
                {
                    foreach (Scorer valSrcScorer in valSrcScorers)
                    {
                        valSrcScorer.Advance(doc);
                    }
                }
                return doc;
            }

            public override long Cost
            {
                get { return subQueryScorer.Cost; }
            }
        }

        public override Weight CreateWeight(IndexSearcher searcher)
        {
            return new CustomWeight(this, searcher);
        }

        public virtual bool IsStrict
        {
            get { return strict; }
            set { strict = value; }
        }

        public virtual string Name
        {
            get { return "custom"; }
        }
    }
}
