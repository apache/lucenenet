namespace Lucene.Net.Search
{
    public sealed class BoostAttribute : Util.Attribute, IBoostAttribute
    {
        private float boost = 1.0f;

        public float Boost
        {
            get { return boost; }
            set { boost = value; }
        }

        public override void Clear()
        {
            boost = 1.0f;
        }
        
        public override void CopyTo(Util.Attribute target)
        {
            ((BoostAttribute)target).Boost = boost;
        }
    }
}
