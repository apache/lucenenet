using System;

namespace Lucene.Net.Search.Similarities
{
    public class NormalizationZ : Normalization
    {
        internal readonly float z;

        public NormalizationZ() : this(0.30F)
        {
        }

        public NormalizationZ(float z)
        {
            this.z = z;
        }

        public float Z
        {
            get { return z; }
        }

        public override float Tfn(BasicStats stats, float tf, float len)
        {
            return (float) (tf*Math.Pow(stats.AvgFieldLength/len, z));
        }

        public override string ToString()
        {
            return "Z(" + z + ")";
        }
    }
}