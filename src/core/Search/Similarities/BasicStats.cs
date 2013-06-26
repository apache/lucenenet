using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Search.Similarities
{
    public class BasicStats : Similarity.SimWeight
    {
        private readonly string field;
        public string Field { get { return field; } }

        public long NumberOfDocuments { get; set; }
        public long NumberOfFieldTokens { get; set; }
        public float AvgFieldLength { get; set; }
        public long DocFreq { get; set; }
        public long TotalTermFreq { get; set; }

        protected readonly float queryBoost;
        protected float topLevelBoost;
        protected float totalBoost;
        public float TotalBoost { get { return totalBoost; } }

        public BasicStats(String field, float queryBoost)
        {
            this.field = field;
            this.queryBoost = queryBoost;
            this.totalBoost = queryBoost;
        }

        public override float GetValueForNormalization { get { return RawNormalizationValue * RawNormalizationValue; } }
        protected float RawNormalizationValue { get { return queryBoost; } }

        public override void Normalize(float queryNorm, float topLevelBoost)
        {
            this.topLevelBoost = topLevelBoost;
            totalBoost = queryBoost * topLevelBoost;
        }
    }
}
