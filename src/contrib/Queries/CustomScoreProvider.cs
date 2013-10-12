using Lucene.Net.Index;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Search
{
    public class CustomScoreProvider
    {
        protected readonly AtomicReaderContext context;

        public CustomScoreProvider(AtomicReaderContext context)
        {
            this.context = context;
        }

        public virtual float CustomScore(int doc, float subQueryScore, float[] valSrcScores)
        {
            if (valSrcScores.Length == 1)
            {
                return CustomScore(doc, subQueryScore, valSrcScores[0]);
            }
            if (valSrcScores.Length == 0)
            {
                return CustomScore(doc, subQueryScore, 1);
            }
            float score = subQueryScore;
            foreach (float valSrcScore in valSrcScores)
            {
                score *= valSrcScore;
            }
            return score;
        }

        public virtual float CustomScore(int doc, float subQueryScore, float valSrcScore)
        {
            return subQueryScore * valSrcScore;
        }

        public virtual Explanation CustomExplain(int doc, Explanation subQueryExpl, Explanation[] valSrcExpls)
        {
            if (valSrcExpls.Length == 1)
            {
                return CustomExplain(doc, subQueryExpl, valSrcExpls[0]);
            }
            if (valSrcExpls.Length == 0)
            {
                return subQueryExpl;
            }
            float valSrcScore = 1;
            foreach (Explanation valSrcExpl in valSrcExpls)
            {
                valSrcScore *= valSrcExpl.Value;
            }
            Explanation exp = new Explanation(valSrcScore * subQueryExpl.Value, "custom score: product of:");
            exp.AddDetail(subQueryExpl);
            foreach (Explanation valSrcExpl in valSrcExpls)
            {
                exp.AddDetail(valSrcExpl);
            }
            return exp;
        }

        public virtual Explanation CustomExplain(int doc, Explanation subQueryExpl, Explanation valSrcExpl)
        {
            float valSrcScore = 1;
            if (valSrcExpl != null)
            {
                valSrcScore *= valSrcExpl.Value;
            }
            Explanation exp = new Explanation(valSrcScore * subQueryExpl.Value, "custom score: product of:");
            exp.AddDetail(subQueryExpl);
            exp.AddDetail(valSrcExpl);
            return exp;
        }

    }
}
