using System;

namespace Lucene.Net.Search.Similarities
{
    public abstract class LMSimilarity : SimilarityBase
    {
        protected readonly ICollectionModel collectionModel;

        protected LMSimilarity(ICollectionModel collectionModel)
        {
            this.collectionModel = collectionModel;
        }

        protected LMSimilarity() : this(new DefaultCollectionModel())
        {
        }

        protected override BasicStats NewStats(string field, float queryBoost)
        {
            return new LMStats(field, queryBoost);
        }

        protected override void FillBasicStats(BasicStats stats, CollectionStatistics collectionStats,
                                               TermStatistics termStats)
        {
            base.FillBasicStats(stats, collectionStats, termStats);
            var lmStats = (LMStats) stats;
            lmStats.CollectionProbability = collectionModel.ComputeProbability(stats);
        }

        protected override void Explain(Explanation expl, BasicStats stats, int doc, float freq, float docLen)
        {
            expl.AddDetail(new Explanation(collectionModel.ComputeProbability(stats), "collection probability"));
        }

        public abstract string GetName();

        public override string ToString()
        {
            string coll = collectionModel.GetName();
            return coll != null ? String.Format("LM {0} - {1}", GetName(), coll) : String.Format("LM {0}", GetName());
        }


        public class DefaultCollectionModel : ICollectionModel
        {
            public float ComputeProbability(BasicStats stats)
            {
                return (stats.TotalTermFreq + 1F)/(stats.NumberOfFieldTokens + 1F);
            }

            public string GetName()
            {
                return null;
            }
        }

        public interface ICollectionModel
        {
            float ComputeProbability(BasicStats stats);
            string GetName();
        }

        public class LMStats : BasicStats
        {
            public LMStats(String field, float queryBoost) : base(field, queryBoost)
            {
            }

            public float CollectionProbability { get; set; }
        }
    }
}