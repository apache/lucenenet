using Lucene.Net.Index;

namespace Lucene.Net.Search.Similarities
{
    public abstract class PerFieldSimilarityWrapper : Similarity
    {
        public override sealed long ComputeNorm(FieldInvertState state)
        {
            return Get(state.Name).ComputeNorm(state);
        }

        public override sealed SimWeight ComputeWeight(float queryBoost, CollectionStatistics collectionStats,
                                                       TermStatistics[] termStats)
        {
            var weight = new PerFieldSimWeight();
            weight.Delegate = Get(collectionStats.Field);
            weight.DelegateWeight = weight.Delegate.ComputeWeight(queryBoost, collectionStats, termStats);
            return weight;
        }

        public override sealed ExactSimScorer GetExactSimScorer(SimWeight weight, AtomicReaderContext context)
        {
            var perFieldWeight = (PerFieldSimWeight) weight;
            return perFieldWeight.Delegate.GetExactSimScorer(perFieldWeight.DelegateWeight, context);
        }

        public override sealed SloppySimScorer GetSloppySimScorer(SimWeight weight, AtomicReaderContext context)
        {
            var perFieldWeight = (PerFieldSimWeight) weight;
            return perFieldWeight.Delegate.GetSloppySimScorer(perFieldWeight.DelegateWeight, context);
        }

        public abstract Similarity Get(string name);

        internal class PerFieldSimWeight : SimWeight
        {
            internal Similarity Delegate;
            internal SimWeight DelegateWeight;

            public override float ValueForNormalization
            {
                get
                {
                    return DelegateWeight.ValueForNormalization;
                }
            }

            public override void Normalize(float queryNorm, float topLevelBoost)
            {
                DelegateWeight.Normalize(queryNorm, topLevelBoost);
            }
        }
    }
}