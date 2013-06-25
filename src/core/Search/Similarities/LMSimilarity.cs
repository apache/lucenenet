using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Search.Similarities
{
    public class LMSimilarity : SimilarityBase
    {
        protected readonly CollectionModel collectionModel;

        public LMSimilarity(CollectionModel collectionModel)
        {
            this.collectionModel = collectionModel;
        }

        public LMSimilarity() : this(new DefaultCollectionModel()) { }

        protected override BasicStats NewStats(string field, float queryBoost)
        {
            return new LMStats(field, queryBoost);
        }

        protected override void FillBasicStats(BasicStats stats, CollectionStatistics collectionStats, TermStatistics termStats)
        {
            base.FillBasicStats(stats, collectionStats, termStats);
            var lmStats = (LMStats)stats;
            lmStats.CollectionProbability = collectionModel.ComputeProbability(stats);
        }

        protected override void Explain(Explanation expl, BasicStats stats, int doc, float freq, float docLen)
        {
            expl.AddDetail(new Explanation(collectionModel.computeProbability(stats), "collection probability"));
        }

        public abstract string GetName();

        public override string ToString()
        {
            var coll = collectionModel.GetName();
            if (coll != null)
            {
                return String.Format("LM {0} - {1}", GetName(), coll);
                //return String.format(Locale.ROOT, "LM %s - %s", getName(), coll);
            }
            else
            {
                return String.Format("LM {0}", GetName());
                //return String.format(Locale.ROOT, "LM %s", getName());
            }
        }

        public class LMStats : BasicStats
        {
            private float collectionProbability;
            public sealed float CollectionProbability { get { return collectionProbability; } set { collectionProbability = value; } }

            public LMStats(String field, float queryBoost) : base(field, queryBoost) { }
        }

        public interface CollectionModel
        {
            public float ComputeProbability(BasicStats stats);
            public string GetName();
        }


        public class DefaultCollectionModel : CollectionModel
        {
            public DefaultCollectionModel() { }

            public override float ComputeProbability(BasicStats stats)
            {
                return (stats.TotalTermFreq + 1F) / (stats.NumberOfFieldTokens + 1F);
            }

            public override string GetName()
            {
                return null;
            }
        }
    }
}
