using Lucene.Net.Facet.Params;
using Lucene.Net.Facet.Taxonomy;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Facet.Search
{
    public class SumScoreFacetsAggregator : IFacetsAggregator
    {
        private readonly IntsRef ordinals = new IntsRef(32);

        public void Aggregate(FacetsCollector.MatchingDocs matchingDocs, CategoryListParams clp, FacetArrays facetArrays)
        {
            ICategoryListIterator cli = clp.CreateCategoryListIterator(0);
            if (!cli.SetNextReader(matchingDocs.context))
            {
                return;
            }

            int doc = 0;
            int length = matchingDocs.bits.Length;
            float[] scores = facetArrays.GetFloatArray();
            int scoresIdx = 0;
            while (doc < length && (doc = matchingDocs.bits.NextSetBit(doc)) != -1)
            {
                cli.GetOrdinals(doc, ordinals);
                int upto = ordinals.offset + ordinals.length;
                float score = matchingDocs.scores[scoresIdx++];
                for (int i = ordinals.offset; i < upto; i++)
                {
                    scores[ordinals.ints[i]] += score;
                }

                ++doc;
            }
        }

        private float RollupScores(int ordinal, int[] children, int[] siblings, float[] scores)
        {
            float score = 0F;
            while (ordinal != TaxonomyReader.INVALID_ORDINAL)
            {
                float childScore = scores[ordinal];
                childScore += RollupScores(children[ordinal], children, siblings, scores);
                scores[ordinal] = childScore;
                score += childScore;
                ordinal = siblings[ordinal];
            }

            return score;
        }

        public void RollupValues(FacetRequest fr, int ordinal, int[] children, int[] siblings, FacetArrays facetArrays)
        {
            float[] scores = facetArrays.GetFloatArray();
            scores[ordinal] += RollupScores(children[ordinal], children, siblings, scores);
        }

        public bool RequiresDocScores
        {
            get
            {
                return true;
            }
        }
    }
}
