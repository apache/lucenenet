using System;

namespace Lucene.Net.Search.Similarities
{
    public class BasicStats : Similarity.SimWeight
    {
        private readonly string field;
        protected readonly float queryBoost;
        protected float topLevelBoost;
        protected float totalBoost;

        public BasicStats(String field, float queryBoost)
        {
            this.field = field;
            this.queryBoost = queryBoost;
            totalBoost = queryBoost;
        }

        public string Field
        {
            get { return field; }
        }

        public long NumberOfDocuments { get; set; }
        public long NumberOfFieldTokens { get; set; }
        public float AvgFieldLength { get; set; }
        public long DocFreq { get; set; }
        public long TotalTermFreq { get; set; }

        public float TotalBoost
        {
            get { return totalBoost; }
        }

        protected float RawNormalizationValue
        {
            get { return queryBoost; }
        }

        public override float GetValueForNormalization()
        {
            return RawNormalizationValue*RawNormalizationValue;
        }

        public override void Normalize(float queryNorm, float topLevelBoost)
        {
            this.topLevelBoost = topLevelBoost;
            totalBoost = queryBoost*topLevelBoost;
        }
    }
}