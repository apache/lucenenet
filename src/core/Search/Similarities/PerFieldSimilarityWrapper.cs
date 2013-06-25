using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Search.Similarities
{
    public abstract class PerFieldSimilarityWrapper : Similarity
    {
        public PerFieldSimilarityWrapper() { }

        public override sealed long ComputeNorm(FieldInvertState state)
        {
            return Get(state.Name).ComputeNorm(state);
        }

        public override sealed SimWeight ComputeWeight(float queryBoost, CollectionStatistics collectionStats, TermStatistics[] termStats)
        {
            var weight = new PerFieldSimWeight();
            weight.Delegate = Get(collectionStats.Field);
            weight.DelegateWeight = weight.Delegate.ComputeWeight(queryBoost, collectionStats, termStats);
            return weight;
        }

        public override sealed ExactSimScorer ExactSimScorer(SimWeight weight, AtomicReaderContext context)
        {
            var perFieldWeight = (PerFieldSimWeight)weight;
            return perFieldWeight.Delegate.ExactSimScorer(perFieldWeight.DelegateWeight, context);
        }

        public override sealed SloppySimScorer SloppySimScorer(SimWeight weight, AtomicReaderContext context)
        {
            var perFieldWeight = (PerFieldSimWeight)weight;
            return perFieldWeight.Delegate.SloppySimScorer(perFieldWeight.delegateWeight, context);
        }

        public abstract Similarity Get(string name);

        internal class PerFieldSimWeight : SimWeight
        {
            Similarity Delegate;
            SimWeight DelegateWeight;

            public override float GetValueForNormalization()
            {
                return DelegateWeight.GetValueForNormalization();
            }

            public override void Normalize(float queryNorm, float topLevelBoost)
            {
                DelegateWeight.Normalize(queryNorm, topLevelBoost);
            }
        }
    }
}
